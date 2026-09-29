# E28 — Bericht Phase 0 (Opus, 26.09.2026 ca. 08:30, Worktree e28 = 6324e65d, kein Commit, Arbeitsbaum sauber; Testdatenbank 3ac19fa9)

**Kurzbefund:** Beide Stellen aus N7 sind echte, aber **latente** Fehler: Keines der 14 Referenzprojekte, weder 1048 noch ein Projekt der
Live-Datenbank, erreicht sie. Es gibt in beiden Datenbanken **keine Wärmepumpe im PV-Modus** (`BM_Typ = 'PV'`: 0 Anlagen) und **kein Projekt mit
BHKW und Photovoltaik**. Die Kesselzeilen hinter einem BHKW (1017, 1047) haben keine Überschussstunde. 1018 und 1030 haben Überschussstunden, dort
rechnet der Kessel aber in derselben Speicherstufe wie das BHKW und sieht deshalb den Stromeingang vor dem BHKW. **Die Wirkung auf R20 ist null**,
der Kapitalwert ändert sich nirgends. Stelle 1 ändert, wenn sie greift, den Betrieb der Anlagen. Stelle 2 ist reiner Ausweis.

## (1) Fundstellen und Rechenweg

**Stelle 1: Vorab-Überschuss für den PV-Modus der Wärmepumpe.**
- Fundstellen: `EPOS.Kern/Allgemein/Simulation/SimulationControl.cs:4296-4336` (`PV_Ueberschuss_Vorabberechnen`), Kern `:4320-4326`, Aufruf
  `:1265` in `Speicherstufe_Rechnen`. Die Methode liefert `null`, wenn keine WP-Anlage `BM_Typ = PV` hat oder `tool[4]` keine Photovoltaik ist
  (`:4298-4309`).
- Zeitpunkt: Die Methode läuft beim Start der Speicherstufe. `Rest_Strombedarf_viertelstuendlich` ist dann der Strombedarf nach allen
  Vektorstufen vor der Speicherstufe. Ein BHKW als Vektorstufe davor wird ungeklemmt abgezogen (`:951-953`, `SubVectors(..., false)`). In
  Überschussstunden ist der Rest deshalb negativ.
- Rechnung: `ueberschuss_h = max(0, potenzial_h − bedarf_h)`. Das PV-Potenzial kommt aus einem Probelauf von `simulation_pv`, danach folgt `Init()`.
  `bedarf_h` ist das Stundenmittel des Rests.
- Der Fehler: Ist `bedarf_h < 0`, wird der BHKW-Überschuss `|bedarf_h|` zum PV-Überschuss addiert, auch nachts bei Potenzial 0. Die eigentliche
  PV-Simulation klemmt denselben Fall seit V1 ausdrücklich (`SimulationPV.cs:433-442`: `bedarf = Math.Max(0, bedarfRoh)`, der Rest geht nach
  `BhkwUeberschuss`). Die Vorabrechnung kennt diese Regel nicht.
- Quellen: Der Vorab-Überschuss enthält das PV-Potenzial und, fälschlich, den BHKW-Überschuss einer vorgelagerten BHKW-Vektorstufe. Nicht enthalten
  sind:
  - ein BHKW in derselben Speicherstufe: Es zieht erst nach der Schleife ab (`:907-915`) und fehlt deshalb ganz. Dieselbe Wärmepumpe sähe den
    BHKW-Überschuss also je nach Kaskadenplatz des BHKW oder eben nicht.
  - die Flotte bzw. der Stromspeicher: Er rechnet erst nach der PV (`:612-624`).
- Wohin er fließt (`Kaskadenschleife.cs:791/860-865`):
  - `pvRest` begrenzt die Ladung jeder WP im PV-Modus: `SimulationWaermepumpe.cs:1788-1799` (Ladepotenzial) und `:1871/1902` (Abbuchung).
  - `pvUeberschuss = pvRest > 0` schaltet **für die ganze Schleife** um: die Ladeordnung auf `WS_Ladeprio_PV` (`Ladeordnung.cs:136-145`), jede
    Speicherobergrenze auf `ObergrenzePV` (`SimulationKanaele.cs:1795-1798`; betrifft auch die Ladung von Solar, Kessel und BHKW,
    `Kaskadenschleife.cs:1278-1302`) und den Puffer-Raum des BHKW (`SimulationBHKW.cs:1390-1405`).
  - Keine Wirkung auf den Heizstab, auf die Einspeisung und auf die PV-Simulation selbst. Die PV rechnet danach regulär auf dem Rest nach
    WP-Verbrauch, V1 greift.
- Energiebilanz: bleibt geschlossen. Der WP-Strom kommt in den Rest (`:869-872`) und nimmt den negativen Rest auf. Der Fehler ist also keine
  Doppelzählung, sondern eine **Fehlsteuerung**: Die WP lädt im „PV-Modus" mit BHKW-Strom, nachts gelten PV-Obergrenzen und PV-Ladeprioritäten. Im
  KWK-Split sinkt dann die KWK-Einspeisung zugunsten des Eigenverbrauchs.

**Stelle 2: Kessel-Stufe hinter dem BHKW mit negativem Stromeingang.**
- Fundstellen: Vektorstufe `SimulationControl.cs:922-930` → `Simulation_SPK_Ctrl_Zweikanalig` `:1972-1982`. Diese setzt
  `simulation_spk.Strombedarf_stuendlich = Strombedarf` (Stundenmittel des Rests, ungeklemmt). Zwei weitere Wege derselben Größe:
  - Kessel als Mitglied der Speicherstufe: `:1303` (Kopie des Stufeneingangs, negativ nach einer BHKW-Vektorstufe vor der Schleife),
  - N3-Nachzug `:892-897` (Kessel hinter der WP in der Schleife: Rest nach WP-Strom).
- Verwendung: Der Kessel liest die Reihe **nur als Summe**. `SimulationSPK.cs:1023` (`StrombedarfGesamtKwh = Strombedarf_stuendlich.Sum()`) ist
  die einzige Leseposition im Modul; die Stundenschleife braucht sie nicht, der Stromverbrauch des Elektrokessels ist davon unabhängig. Die Summe
  steht in:
  - `Tab_ErgebnisHeizkessel.Strombedarf` und `.Reststrombedarf`: `SimulationRunner.cs:796-797`, `SimulationErgebnisCtrl.cs:483-484`,
    `HeizkesselReiter.razor:91`, `ErgebnisCtrl.cs:490`,
  - in `aggregate.csv` (`Heizkessel.Strombedarf/Reststrombedarf`).
  Kein Wirtschaftlichkeitsrechner liest diese Spalten. Energiekosten, CO₂ und Strommatrix lesen den Netzbezug bzw. `ReststromMwh`.
- Was bei negativem Eingang passiert: Stunden mit BHKW-Überschuss werden gegen Bedarfsstunden verrechnet. Die Kesselzeile zeigt einen zu kleinen,
  im Extremfall negativen „Strombedarf". Das widerspricht der BHKW-Zeile, die seit E27‑Q4 je Stunde klemmt (`BhkwReststrombedarfMwh`). Rechenwirkung
  hat es nicht, es ist ein **Ausweisfehler**.
- **Nebenbefund N8, dieselbe Art:** Die PV-Zeile `Strombedarf = pvs.Strombedarf.Sum()/4000` (`SimulationRunner.cs:1031`,
  `SimulationErgebnisCtrl.cs:866`) summiert ihren Stufeneingang ebenfalls ungeklemmt. Hinter einem BHKW mit Überschuss sinkt der Wert. Aus den
  Reihen gerechnet ergäbe die E27-Probe 1018 + PV 1040 −27,46 MWh; das ist nicht im Lauf gemessen. Kein Projekt betroffen.

## (2) Messung

| | |
|---|---|
| Grundlage | Referenzlauf auf HEAD 6324e65d mit 15 Projekten (1007…1047 + 1048) gegen R20 |
| Ergebnis | **14/14 PASS, 432/432 CSV byte-gleich** (1048 hat keine R20-Basis) |
| Konfiguration | `Tab_Einstellungen.Tool_1..6`, `Tab_Energieanlagen.BM_Typ` (Testdatenbank und Live-Datenbank nur lesend) |
| Stundenwerte | `strombedarf_viertelstunde.csv` gegen `bhkw_strom.csv` |

| Projekt | Kaskade | Stelle 1: WP im PV-Modus / PV / BHKW davor | Stelle 1 Vorab > 0 bei BHKW-Überschuss | Stelle 2: Kessel hinter BHKW | Stunden Bedarf < BHKW (Viertelstunden) | Kessel-Stufeneingang < 0 |
|---|---|---|---|---|---|---|
| 1007, 1046 | –, Solar, WP, PV, SSP | nein / ja / nein | 0 h, 0 MWh (Vorab = null) | kein Kessel | kein BHKW | – |
| 1008 | WP | nein / nein / nein | 0 (null) | – | kein BHKW | – |
| 1017 | BHKW, Kessel, WP, SSP | nein / nein / ja | 0 (null) | ja (Kessel sieht 635,2 = 672 − 36,8) | 0 h (0), min. Abstand 8,04 kW | 0 h |
| 1018 | BHKW, Kessel (gemeinsame Speicherstufe) | – | 0 (null) | nein, Eingang vor BHKW (Heizkessel.Strombedarf 0) | 3.501 h (14.004), 27,4575 MWh | 0 h |
| 1023, 1039, 1040, 1041, 1042, 1045, 1048 | WP/Kessel (±Solar, PV, SSP) | nein / teils / nein | 0 (null) | kein BHKW | – | 0 h |
| 1024 | WP, Kessel, BHKW | nein / nein / nein | 0 (null) | Kessel vor BHKW | 0 h (0), min. 20,67 kW | 0 h |
| 1030 | BHKW, Kessel (gemeinsame Speicherstufe) | – | 0 (null) | nein, Eingang vor BHKW (Heizkessel.Strombedarf 4.790,09 = BHKW.Strombedarf) | 12 h (48), 0,392 MWh | 0 h |
| 1047 | BHKW, WP, Kessel, SSP | nein / nein / ja | 0 (null) | ja (N3-Nachzug, 640,19) | 0 h (0), min. 8,78 kW | 0 h |

- **Summe: Stelle 1 in 0 Stunden bei 0 MWh, Stelle 2 in 0 Stunden, Kapitalwertwirkung 0 €** in allen 15 Projekten.
- Live-Datenbank (`C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite`, nur lesend): 0 Wärmepumpen im PV-Modus, 0 Projekte mit BHKW und PV, 3 Projekte mit
  Kessel hinter BHKW. Diese drei nicht gerechnet, Stelle 2 wäre dort ohnehin reiner Ausweis.
- **Wie groß der Fehler würde** (gerechnet aus R20-Reihen, kein Lauf): Fiktiv 1018 + PV von 1040 + eine WP im PV-Modus hinter einer
  BHKW-Vektorstufe.

  | | |
  |---|---|
  | Vorab-Überschuss heute | 34,171 MWh |
  | Vorab-Überschuss richtig | 6,7135 MWh (= PV-Potenzial, weil Bedarf ≤ 0) |
  | Falsch als PV gezählt | 27,4575 MWh in 3.501 h (Spitze 14,5 kW) |
  | Davon Stunden ohne jede PV-Erzeugung | 2.111 |

  In diesen 2.111 Stunden stünden `pvUeberschuss`, `ObergrenzePV` und die PV-Ladeprio fälschlich auf „PV". Die Kapitalwertwirkung ist ohne
  konkretes Projekt nicht bezifferbar. Sie kann nicht größer sein als die Überschussmenge mal (Einspeisewert − Eigenverbrauchswert
  KWK/KWKG-Zuschlag) plus die Brennstoffverschiebung Kessel → WP. Das Vorzeichen hängt von den Preisen ab.
- Probe auf einer DB-Kopie: 1047 + PV von 1040 + `BM_Typ = PV` gegen dieselbe Kopie ohne PV-Modus. Die Ergebnisse sind **identisch**, alle
  PV-Erzeugung geht in den Eigenverbrauch (`pv_ueberschuss` 0). Ohne BHKW-Überschuss ist die Vorabrechnung damit wirkungslos, also richtig.
- Eine Probe mit BHKW-Überschuss ist nicht gelungen: Das Strombedarfsprofil von 1047 ließ sich über `Tab_Stromverbraucher` nicht skalieren.
- Kein temporärer Kern-Eingriff. Er war nicht nötig, weil kein Projekt den Pfad erreicht. Die Nachher-Werte sind oben gerechnet.

## (3) Bewertung und Vorschlag

- **Stelle 1: Fehler (latent).** Er widerspricht der V1-Regel („ein negativer Restbedarf ist BHKW-Überschuss, keine PV-Größe",
  `SimulationPV.cs:433`) und dem Namen des Modus. Er wirkt auch uneinheitlich: je nachdem, ob das BHKW als Vektorstufe vor der Speicherstufe steht
  oder Mitglied ist.
  - **Regel:** Den Bedarf je Stunde bei 0 klemmen: `rest = potenzial_h − max(0, bedarf_h)` in `SimulationControl.cs:4325`. Am besten als
    `internal static double[] PvUeberschussVorab(double[] potenzial, double[] bedarf)` für den Theorietest herausziehen.
  - Für `bedarf_h ≥ 0` bleibt das Ergebnis bitgleich.
- **Stelle 2: Ausweisfehler (latent), kein Rechenfehler.**
  - **Regel:** Den Strom-Stufeneingang der Kesselzeile je Stunde bei 0 klemmen, analog E27‑Q4, an allen drei Wegen (`:924`/`:1977`, `:892-897`,
    `:1303`). Dazu `NetzbezugGeklemmt` wiederverwenden: Ohne negativen Wert gibt es dasselbe Array zurück, bitgleich.
  - Die Stundenrechnung des Kessels bleibt unberührt, weil sie die Reihe nicht liest.
- **N8 (PV-Zeile):** gleiche Regel für `pvm.Strombedarf` / `e.StrombedarfMwh`. Frage Q3.
- **Wirkung auf R20: keine CSV wandert** (kein Projekt hat die Konstellation; A/B muss 14/14 byte-gleich zeigen). **Neueinfrierung: nein**, R20
  bleibt. Kein Kapitalwert-Anker wandert, kein Schemaschritt.

## (4) Fragen

- **E28‑Q1 (Stelle 1):**
  - (a) streng PV: den Bedarf bei 0 klemmen, BHKW-Überschuss zählt nie als PV-Überschuss.
  - (b) jeden örtlichen Überschuss bewusst einbeziehen: umbenennen in „Eigenstromüberschuss". Dann müsste auch ein BHKW in der Speicherstufe
    stundengleich einfließen, was ein Umbau der Schleife wäre.
  - (c) nur dokumentieren.
  - **Empfehlung a.**
- **E28‑Q2 (Stelle 2):**
  - (a) Stromeingang der Kesselzeile je Stunde klemmen, alle drei Wege.
  - (b) lassen und als „Nettoeingang" dokumentieren.
  - **Empfehlung a**, einheitlich mit E27‑Q4.
- **E28‑Q3 (N8, PV-Zeile Strombedarf):**
  - (a) mitklemmen.
  - (b) als Restpunkt benennen.
  - **Empfehlung a** (eine Zeile an zwei Stellen, gleiche Hilfsmethode).
- **E28‑Q4 (Basis):** keine Neueinfrierung. A/B gegen R20 als Nachweis, R20 bleibt. **Empfehlung: so.**
- **E28‑Q5 (Protokoll):**
  - (a) kein Hinweis.
  - (b) ein Hinweis, wenn eine WP im PV-Modus hinter einer BHKW-Vektorstufe steht („BHKW-Überschuss zählt nicht als PV-Überschuss").
  - **Empfehlung a**, sonst Ressourcen, Mockup und Papier.

**Aufwand Phase 1: ~2,5 h + Gate.**
- Zwei kleine Kern-Änderungen samt Hilfsmethode, optional N8.
- Tests.
- Referenzlauf 15 Projekte A/B gegen R20 (14/14 byte-gleich), 1048 gleich.
- Keine Papiere (macht der Papieragent).

**Testvorschlag:** neue Klasse `StromStufeneingangKlemmeTests` nach dem Muster `BhkwNetzbezugKlemmeTests`.
1. Theorie `PvUeberschussVorab`:
   - (Pot 10, Bedarf 4 → 6)
   - (10, −5 → 10, nicht 15)
   - (0, −5 → 0, nicht 5)
   - (3, 8 → 0)
   - (0, 0 → 0)
   - ohne negativen Bedarf bitgleich zur alten Formel
2. Theorie Kessel-Stufeneingang: Reihe ohne negativen Wert → dasselbe Array, Summe bitgleich. Mit Überschussstunde → Σ max(0, x), Eingabe
   unberührt.
3. Vorrichtung (Testdatenbank-Kopie): 1030 und 1018 rechnen, `Heizkessel.Strombedarf` bleibt 4.790,09 bzw. 0.
4. 1017/1047: Kesselzeile bitgleich 635,2 / 640,19.
5. Optional eine Projektkopie 1018 + PV 1040 (Kopiere-Muster) für N8: PV-Zeile Strombedarf 0 statt negativ.

Eine Vorrichtung für Stelle 1 mit echter WP im PV-Modus hinter einem BHKW-Überschuss braucht eine skalierbare Strombedarfsquelle; in Phase 1 zu
klären, sonst reicht die Theorie. Testhost-Regel und Schalter `xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2` wie beauftragt.

# E28 — Bericht Phase 1 (Opus, 26.09.2026 ca. 09:00, e28 = eac2378f über 6324e65d; Testdatenbank unverändert 3ac19fa9; Arbeitsbaum sauber)

Entscheide wie freigegeben: E28‑Q1 a, Q2 a, Q3 a, Q4 (keine Neueinfrierung), Q5 a.

**Commits** (Trailer Opus 5.5, Betreffe mit #535):

| Commit | Inhalt |
|---|---|
| `ed8a307e` E28/1 | Stelle 1: neue Methode `SimulationControl.PvUeberschussVorab(potenzial, bedarf)` (`internal static`, ~`:4350-4371`) mit der Regel „Bedarf je Stunde < 0 → 0, Überschuss = max(0, Potenzial − Bedarf)". Aufruf in `PV_Ueberschuss_Vorabberechnen` (`:4331-4333`) statt der Schleife. Ohne negativen Bedarf bitgleich. |
| `3337810b` E28/2 | Stelle 2 und N8: Die Kesselzeile klemmt je Stunde über `NetzbezugGeklemmt` an allen drei Wegen: N3-Nachzug `:894-899`, Mitglied der Speicherstufe `:1305-1309` (Klon des Stufeneingangs, das BHKW bekommt weiter den ungeklemmten Klon), Vektorstufe `Simulation_SPK_Ctrl_Zweikanalig` `:1983-1986`. Dazu die PV-Zeile `SimulationRunner.cs:1031-1033` und `SimulationErgebnisCtrl.cs:866-867`. Ohne negativen Wert bleibt das Array dasselbe, also bitgleich. |
| `eac2378f` E28/3 | neue Klasse `EPOS.Kern.Tests/StromStufeneingangKlemmeTests.cs` mit 13 Fällen, `[Collection("Testdatenbank")]` und `Kulturvorrichtung` (Einzelheiten unten). |

**Die 13 Testfälle:**
- Theorie `PvUeberschussVorab`: (10, 4 → 6), (10, −5 → 10), (0, −5 → 0), (3, 8 → 0), (0, 0 → 0).
- Bitgleichheit zur alten Formel über 8.760 Zufallsstunden samt ±0,0.
- Ein kurzer Bedarfsvektor zählt die fehlenden Stunden als 0.
- Der Kessel-Stromeingang wird je Stunde geklemmt: Summe 17 statt 13,5, die Eingabe bleibt unberührt, ohne Überschuss dasselbe Array.
- Anker `Heizkessel.Strombedarf` (Ergebnismodell = Ergebnisansicht, ≥ 0): 1017 635,2 / 1018 0 / 1030 4.790,09 / 1047 640,19.
- N8: 1018 + PV von 1040 auf der Arbeitskopie. Die Vorbedingung „Stufeneingang negativ" gilt; die PV-Zeile zeigt Strombedarf 0 (vorher rechnerisch −27,46) in Ergebnis und Ergebnisansicht. `BhkwUeberschussGesamtKwh` bleibt 27.457,51.

Eine Vorrichtung für Stelle 1 mit echtem BHKW-Überschuss fehlt bewusst: Dafür müsste eine WP-Anlage samt Gerät, Senke und Puffer in ein BHKW-Projekt mit Überschuss. Außerdem ließ sich das Strombedarfsprofil in Phase 0 nicht über `Tab_Stromverbraucher` skalieren. Die Begründung steht im Klassenkommentar, die Regel hält die Theorie.

**Nachweise (E28/4, kein eigener Commit, nichts zu committen):**

| Prüfung | Ergebnis |
|---|---|
| Release-Build `WP-Plan.Kern.slnf` | 0 Fehler (46 Warnungen, Bestand) |
| Kern-Tests gefiltert (Klemme, PvAusweis, BhkwNetzbezug, `EPOS.Kern.Tests.Simulation*`, StromStufeneingang, ZapfprofilReferenzprojektWache) | 161/161 grün |
| Voller Lauf `WP-Plan.Kern.slnf` mit Schaltern (08:44–08:59) | Kern 8.100/8.101 (1 übersprungen), UI 6.531, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27/28 (1 übersprungen). Zusammen **15.593 grün, 2 übersprungen, 0 rot**; `ZapfprofilReferenzprojektWacheTests` grün |
| Referenzlauf 14 Projekte gegen R20 (e28 = eac2378f) | **14/14 PASS, GESAMT PASS 4.610.207 Werte, 432/432 CSV byte-gleich** |
| 1048 (Lauf mit 15 Projekten) | 32/32 CSV byte-gleich zum Lauf auf 6324e65d |

**Wirkung:** keine Referenz-CSV wandert, keine Neueinfrierung (R20 bleibt), kein Kapitalwert-Anker, kein Schemaschritt. Die Testdatenbank ist nicht angefasst (3ac19fa9; die Proben aus Phase 0 liefen nur auf Kopien im Scratchpad), und `git status` ist sauber.

**Testhost-Regel:** Der erste gefilterte Probelauf der neuen Klasse (08:31) startete, obwohl ein fremder Testhost lief. Das war ein Versehen: nur 13 Fälle, 2 s. Danach lief jeder Lauf über ein Warteskript: warten, bis kein Testhost mehr läuft, dann 5–30 s Zufallspause und erneut prüfen. Der gefilterte Lauf und der volle Lauf sind so abgesichert.

**Aufwand:** ~1 h Arbeit + 25 min Testlauf.

**Abnahmevorschlag A‑E28‑1 (Windows, Testdatenbank):**
1. 1017, 1018, 1030 und 1047 rechnen. Der Kessel-Reiter zeigt Strombedarf 635,20 / 0 / 4.790,09 / 640,19 MWh, unverändert.
2. Projektkopie 1018 mit der PV-Anlage von 1040 rechnen. Der PV-Reiter zeigt Strombedarf 0 (nicht −27,46), die Einspeisung PV 6,60 MWh und die KWK-Einspeisung 27,46 MWh wie bisher.
3. Kern-Lauf gegen R20 grün.

**Kein Logbuch-Eintrag:** Keine Referenzrechnung ändert sich, betroffen ist nur der Ausweis in einer Konstellation, die heute kein Projekt hat.

**Restpunkte:**
- (a) `Strombedarfsdeckung` der PV-Zeile (`SimulationRunner.cs:1035-1036`, `SimulationErgebnisCtrl.cs:858/864`) teilt durch `pvs.Strombedarf_stuendlich.Sum()`, also ungeklemmt. Hinter einem BHKW-Überschuss wäre die Deckung überhöht. Nicht Teil von Q3, benannt.
- (b) Eine Vorrichtung für Stelle 1 mit echter WP im PV-Modus hinter BHKW-Überschuss: nur, wenn ein Referenzprojekt diese Konstellation bekommt.
- (c) Papiere macht der Papieragent: Konzept § 3.6/§ 6.3 Nr. 36 N7 erledigt, Register R‑E28 Q1…Q5, Statuszeile #535.

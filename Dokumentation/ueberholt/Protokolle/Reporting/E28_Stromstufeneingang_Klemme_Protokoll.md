# E28 — Prüfwelle N7: PV-Modus der Wärmepumpe nur auf PV-Überschuss, Strom-Stufeneingang von Kessel- und PV-Zeile geklemmt (Protokoll, 26.09.2026)

Statuszeile #535 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Anlass: der Befund N7 aus E27
(Statusdatei, Nach #521 (g); Register R‑E27, E27‑Q7 „nur melden“) und der Anwenderentscheid vom 26.09.2026 (~08:25),
„Prüfwelle ausführen“; Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 3.6 (Absatz „Netzbezug nie negativ“) und § 6.3 Nr. 36;
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
R‑E28 (neu) und R‑E27 (E27‑Q7); Analysepapier
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
§ 5. Vorgänger: [`E27_BhkwNetzbezug_Klemme_R19_Protokoll.md`](E27_BhkwNetzbezug_Klemme_R19_Protokoll.md). Zweig `e28`
von `6324e65d`; Opus 5.5 im Worktree `.claude/worktrees/e28`: `ed8a307e` (E28/1), `3337810b` (E28/2), `eac2378f`
(E28/3); vier Dateien, +237/−12. Merge `6695caec` („Merge e28: Prüfwelle N7 — PV-Modus nur auf PV-Überschuss,
Strom-Stufeneingang von Kessel- und PV-Zeile geklemmt (#535)“) auf `pm26` über `origin` = `45c35a94` (#534).
**Kein Schemaschritt, keine Neueinfrierung** — die Referenzbasis `2026-09-26_R20_Zapfprofil` bleibt, die Testdatenbank
ist unverändert (`3ac19fa9`, Schemastand 148).

## Befund vor der Welle (Phase 0, Worktree `e28` = `6324e65d`, ohne Kern-Eingriff)

Die Zeilennummern sind die vor E28.

1. **Beide Stellen sind echte, aber latente Fehler.** Keines der 14 Referenzprojekte, weder das Prüfprojekt 1048 noch
   ein Projekt der Live-Datenbank erreicht sie: In beiden Datenbanken gibt es **keine Wärmepumpe im PV-Modus**
   (`BM_Typ = 'PV'`: 0 Anlagen) und **kein Projekt mit BHKW und Photovoltaik**. Die Kessel hinter einem BHKW (1017,
   1047) haben keine Überschussstunde; 1018 und 1030 haben Überschussstunden, rechnen den Kessel aber in derselben
   Speicherstufe wie das BHKW, er sieht dort den Stromeingang vor dem BHKW.
2. **Stelle 1 — Vorab-Überschuss für den PV-Modus der Wärmepumpe** (`SimulationControl.cs:4296-4336`,
   `PV_Ueberschuss_Vorabberechnen`, Kern `:4320-4326`, Aufruf `:1265` in `Speicherstufe_Rechnen`; `null`, wenn keine
   WP im PV-Modus oder `tool[4]` keine Photovoltaik ist, `:4298-4309`). Die Methode läuft beim Start der
   Speicherstufe; `Rest_Strombedarf_viertelstuendlich` ist dann der Strombedarf nach allen Vektorstufen davor, eine
   BHKW-Vektorstufe zieht ungeklemmt ab (`:951-953`, `SubVectors(..., false)`), in Überschussstunden ist der Rest
   negativ. Gerechnet wurde `ueberschuss_h = max(0, potenzial_h − bedarf_h)` mit dem PV-Potenzial eines Probelaufs
   von `simulation_pv`: Bei `bedarf_h < 0` kam der BHKW-Überschuss zum PV-Überschuss, auch nachts bei Potenzial 0.
   Die PV-Simulation selbst klemmt denselben Fall seit V1 (`SimulationPV.cs:433-442`: `bedarf = Math.Max(0,
   bedarfRoh)`, der Rest geht nach `BhkwUeberschuss`). Nicht enthalten waren ein BHKW in derselben Speicherstufe (es
   zieht erst nach der Schleife ab, `:907-915`) und die Flotte (`:612-624`) — dieselbe Wärmepumpe sah den
   BHKW-Überschuss also je nach Kaskadenplatz des BHKW.
3. **Wohin der Vorab-Überschuss fließt** (`Kaskadenschleife.cs:791/860-865`): `pvRest` begrenzt die Ladung jeder WP
   im PV-Modus (`SimulationWaermepumpe.cs:1788-1799`, Abbuchung `:1871/1902`); `pvUeberschuss = pvRest > 0` schaltet
   für die ganze Schleife die Ladeordnung auf `WS_Ladeprio_PV` (`Ladeordnung.cs:136-145`), jede Speicherobergrenze
   auf `ObergrenzePV` (`SimulationKanaele.cs:1795-1798`; auch für Solar, Kessel und BHKW,
   `Kaskadenschleife.cs:1278-1302`) und den Puffer-Raum des BHKW (`SimulationBHKW.cs:1390-1405`). Keine Wirkung auf
   Heizstab, Einspeisung und PV-Simulation. Die Energiebilanz bleibt geschlossen (der WP-Strom nimmt den negativen
   Rest auf, `:869-872`): **eine Fehlsteuerung, keine Doppelzählung** — die WP lädt im „PV-Modus“ mit BHKW-Strom,
   nachts gelten PV-Obergrenzen und PV-Ladeprioritäten, im KWK-Split sinkt die Einspeisung zugunsten des
   Eigenverbrauchs.
4. **Stelle 2 — Kesselzeile mit negativem Stromeingang** an drei Wegen: Vektorstufe `:922-930` →
   `Simulation_SPK_Ctrl_Zweikanalig` `:1972-1982` (`simulation_spk.Strombedarf_stuendlich = Strombedarf`,
   ungeklemmt), Kessel als Mitglied der Speicherstufe `:1303` (Kopie des Stufeneingangs), Nachzug hinter der
   Wärmepumpe `:892-897`. Der Kessel liest die Reihe nur als Summe (`SimulationSPK.cs:1023`,
   `StrombedarfGesamtKwh`), für `Tab_ErgebnisHeizkessel.Strombedarf`/`.Reststrombedarf` (`SimulationRunner.cs:796-797`,
   `SimulationErgebnisCtrl.cs:483-484`, `HeizkesselReiter.razor:91`, `ErgebnisCtrl.cs:490`) und `aggregate.csv`. Kein
   Wirtschaftlichkeitsrechner liest diese Spalten: **ein Ausweisfehler**, er widerspricht der BHKW-Zeile, die seit
   E27‑Q4 je Stunde klemmt.
5. **Nebenbefund N8, dieselbe Art:** die PV-Zeile `Strombedarf = pvs.Strombedarf.Sum()/4000` (`SimulationRunner.cs:1031`,
   `SimulationErgebnisCtrl.cs:866`) summiert ihren Stufeneingang ungeklemmt; aus den Reihen gerechnet ergäbe die
   E27-Probe 1018 + PV von 1040 −27,46 MWh.
6. **Kein Kern-Eingriff:** nicht nötig, weil kein Projekt den Pfad erreicht; die Nachher-Werte sind gerechnet. Eine
   Probe 1047 + PV von 1040 + `BM_Typ = PV` auf einer DB-Kopie ergab identische Ergebnisse (ohne BHKW-Überschuss ist die
   Vorabrechnung wirkungslos, also richtig); eine Probe mit BHKW-Überschuss gelang nicht (das Strombedarfsprofil von
   1047 ließ sich über `Tab_Stromverbraucher` nicht skalieren).

## Messung (Phase 0)

Referenzlauf auf `6324e65d` mit 15 Projekten (1007…1047 und 1048) gegen R20: 14/14 PASS, 432/432 CSV byte-gleich (1048
ohne R20-Basis). Stundenwerte aus `strombedarf_viertelstunde.csv` gegen `bhkw_strom.csv`.

| Projekt | Kaskade | Stelle 1: WP im PV-Modus / PV / BHKW davor | Stelle 1 wirksam | Stelle 2: Kessel hinter BHKW | Stunden Bedarf < BHKW | Kessel-Stufeneingang < 0 |
|---|---|---|---|---|---|---|
| 1007, 1046 | –, Solar, WP, PV, SSP | nein / ja / nein | 0 h (Vorab = null) | kein Kessel | kein BHKW | – |
| 1008 | WP | nein / nein / nein | 0 (null) | – | kein BHKW | – |
| 1017 | BHKW, Kessel, WP, SSP | nein / nein / ja | 0 (null) | ja (635,2 = 672 − 36,8) | 0 h, min. Abstand 8,04 kW | 0 h |
| 1018 | BHKW, Kessel (gemeinsame Speicherstufe) | – | 0 (null) | nein, Eingang vor BHKW (0) | 3.501 h (14.004 Viertelstunden), 27,4575 MWh | 0 h |
| 1023, 1039, 1040, 1041, 1042, 1045, 1048 | WP/Kessel (±Solar, PV, SSP) | nein / teils / nein | 0 (null) | kein BHKW | – | 0 h |
| 1024 | WP, Kessel, BHKW | nein / nein / nein | 0 (null) | Kessel vor BHKW | 0 h, min. 20,67 kW | 0 h |
| 1030 | BHKW, Kessel (gemeinsame Speicherstufe) | – | 0 (null) | nein, Eingang vor BHKW (4.790,09 = BHKW.Strombedarf) | 12 h (48), 0,392 MWh | 0 h |
| 1047 | BHKW, WP, Kessel, SSP | nein / nein / ja | 0 (null) | ja (Nachzug, 640,19) | 0 h, min. 8,78 kW | 0 h |

**Summe: Stelle 1 in 0 Stunden bei 0 MWh, Stelle 2 in 0 Stunden, Kapitalwertwirkung 0 €** in allen 15 Projekten.
Live-Datenbank (`C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite`, nur lesend): 0 Wärmepumpen im PV-Modus, 0 Projekte mit BHKW
und PV, 3 Projekte mit Kessel hinter BHKW (nicht gerechnet, Stelle 2 wäre dort reiner Ausweis).

**Wie groß der Fehler würde** (gerechnet aus R20-Reihen, kein Lauf; fiktiv 1018 + PV von 1040 + eine WP im PV-Modus
hinter der BHKW-Vektorstufe):

| Größe | Wert |
|---|---|
| Vorab-Überschuss vor E28 | 34,171 MWh |
| Vorab-Überschuss richtig (= PV-Potenzial, weil Bedarf ≤ 0) | 6,7135 MWh |
| fälschlich als PV gezählt | 27,4575 MWh in 3.501 h (Spitze 14,5 kW) |
| davon Stunden ohne jede PV-Erzeugung | 2.111 |

Die Kapitalwertwirkung ist ohne konkretes Projekt nicht bezifferbar; sie ist begrenzt durch die Überschussmenge mal
(Einspeisewert − Eigenverbrauchswert KWK/KWKG-Zuschlag) plus die Brennstoffverschiebung Kessel → WP, das Vorzeichen hängt
von den Preisen ab.

## Fragen aus der Welle

Den Anlass hat der Anwender am 26.09.2026 (~08:25) entschieden, „Prüfwelle ausführen“; die Fragen E28‑Q1 bis Q5 stellt
der Phase‑0-Bericht, entschieden hat sie der Orchestrator am 26.09.2026 (09:10) mit der Baufreigabe, nach Empfehlung;
gebaut ist jeweils der Entscheid (→ Register R‑E28).

| Frage | Gegenstand | Entscheid |
|---|---|---|
| **E28‑Q1** Stelle 1 | (a) streng PV: den Bedarf bei 0 klemmen, BHKW-Überschuss zählt nie als PV-Überschuss; (b) jeden örtlichen Überschuss einbeziehen („Eigenstromüberschuss“, Umbau der Schleife); (c) nur dokumentieren | a — `PvUeberschussVorab` |
| **E28‑Q2** Stelle 2 | (a) Stromeingang der Kesselzeile je Stunde klemmen, alle drei Wege; (b) lassen und als „Nettoeingang“ dokumentieren | a — einheitlich mit E27‑Q4 |
| **E28‑Q3** N8 | (a) PV-Zeile Strombedarf mitklemmen; (b) als Restpunkt benennen | a |
| **E28‑Q4** Basis | keine Neueinfrierung, A/B gegen R20 als Nachweis | so — R20 bleibt |
| **E28‑Q5** Protokoll | (a) kein Hinweis; (b) Hinweis bei WP im PV-Modus hinter einer BHKW-Vektorstufe | a |

## Gebaut

- **E28/1 — Stelle 1** (`ed8a307e`, eine Datei, +25/−6; E28‑Q1 a): neue Methode
  `SimulationControl.PvUeberschussVorab(double[] potenzial, double[] bedarf)` (`internal static`, `:4359`) mit der
  Regel „Bedarf je Stunde < 0 → 0, Überschuss = max(0, Potenzial − Bedarf)“; Aufruf in `PV_Ueberschuss_Vorabberechnen`
  `:4331-4333` statt der Schleife. Ohne negativen Bedarf bitgleich.
- **E28/2 — Stelle 2 und N8** (`3337810b`, drei Dateien, +18/−6; E28‑Q2 a, Q3 a): die Kesselzeile klemmt ihren
  Stromeingang je Stunde über `NetzbezugGeklemmt` an allen drei Wegen — Nachzug hinter der Wärmepumpe `:894-899`,
  Mitglied der Speicherstufe `:1305-1309` (Klon des Stufeneingangs; das BHKW bekommt weiter den ungeklemmten Klon),
  Vektorstufe `Simulation_SPK_Ctrl_Zweikanalig` `:1983-1986` —, dazu die PV-Zeile `SimulationRunner.cs:1031-1033` und
  `SimulationErgebnisCtrl.cs:866-867`. Ohne negativen Wert bleibt das Array dasselbe, also bitgleich. Die
  Stundenrechnung des Kessels liest die Reihe nicht und bleibt unberührt.
- **E28/3 — Tests** (`eac2378f`, +194): neue Klasse `EPOS.Kern.Tests/StromStufeneingangKlemmeTests.cs` mit 13 Fällen,
  `[Collection("Testdatenbank")]` und `Kulturvorrichtung`:
  - Theorie `PvUeberschussVorab`: (10, 4 → 6), (10, −5 → 10), (0, −5 → 0), (3, 8 → 0), (0, 0 → 0);
  - Bitgleichheit zur alten Formel über 8.760 Zufallsstunden samt ±0,0;
  - ein kurzer Bedarfsvektor zählt die fehlenden Stunden als 0;
  - Kessel-Stromeingang je Stunde geklemmt: Summe 17 statt 13,5, Eingabe unberührt, ohne Überschuss dasselbe Array;
  - Anker `Heizkessel.Strombedarf` (Ergebnismodell = Ergebnisansicht, ≥ 0): 1017 635,2 / 1018 0 / 1030 4.790,09 /
    1047 640,19;
  - N8: 1018 + PV von 1040 auf der Arbeitskopie — Vorbedingung „Stufeneingang negativ“ gilt, die PV-Zeile zeigt
    Strombedarf 0 (vorher rechnerisch −27,46 MWh) in Ergebnis und Ergebnisansicht, `BhkwUeberschussGesamtKwh` bleibt
    27.457,51.

  Eine Vorrichtung für Stelle 1 mit echtem BHKW-Überschuss fehlt bewusst: Sie bräuchte eine WP-Anlage samt Gerät,
  Senke und Puffer in einem BHKW-Projekt mit Überschuss; die Begründung steht im Klassenkommentar, die Regel hält die
  Theorie.

## Nachweise

- **Build:** Release `WP-Plan.Kern.slnf` 0 Fehler (46 Warnungen, Bestand).
- **Kern-Tests gefiltert** (Klemme, PvAusweis, BhkwNetzbezug, `EPOS.Kern.Tests.Simulation*`, StromStufeneingang,
  ZapfprofilReferenzprojektWache): 161/161 grün.
- **Voller Lauf** `WP-Plan.Kern.slnf` mit den xUnit-Schaltern (08:44–08:59): 15.593 grün, 2 übersprungen, 0 rot (Kern
  8.100/8.101, UI 6.531, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27/28); `ZapfprofilReferenzprojektWacheTests`
  grün.
- **Referenzlauf** 14 Projekte gegen R20 (`eac2378f`): 14/14 PASS, GESAMT PASS 4.610.207 Werte, 432/432 CSV byte-gleich;
  1048 (Lauf mit 15 Projekten) 32/32 CSV byte-gleich zum Lauf auf `6324e65d`.
- **Wirkung:** keine Referenz-CSV wandert, keine Neueinfrierung (R20 bleibt), kein Kapitalwert-Anker, kein
  Schemaschritt; Testdatenbank nicht angefasst (`3ac19fa9`; die Proben der Phase 0 liefen auf Kopien im Scratchpad).
- **Merge** `6695caec` auf `pm26` über `45c35a94`.
- **Gate:** Gate #535 auf `6695caec` (26.09.2026, Kern-Filter Release 0 Fehler, ChartProben 161 Bild-Hashes gleich mit der Messlatte, Tests mit Schaltern: KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (+1 übersprungen), EPOS.UI.Tests 6.531, EPOS.Kern.Tests 8.100 (+1 übersprungen); Dokumentationswachen 31/31 auf dem Papierstand `5826f97d`)
- **CI:** Push `ebf01a90`: alle drei Läufe grün — Kern `main` 36226256793 (21:57 min), Windows `main` 36226256796 (35:16 min; zugleich der Windows-Nachweis für #534), Kern `ios_migration_september` 36226254329 (17:23 min)

## Abweichungen und Befunde

1. **Testhost-Regel einmal verletzt:** der erste gefilterte Probelauf der neuen Klasse (08:31) startete neben einem
   fremden Testhost — 13 Fälle, 2 s; danach lief jeder Lauf über ein Warteskript (warten, bis kein Testhost läuft,
   5–30 s Zufallspause, erneut prüfen).
2. **Keine Vorrichtung für Stelle 1** mit echtem BHKW-Überschuss (siehe „Gebaut“).
3. **Aufwand:** ~1 h Bau + 25 min Testlauf (Schätzung der Phase 0: ~2,5 h + Gate).

## Abnahme am Gerät (A‑E28‑1, Windows, Testdatenbank)

1. 1017, 1018, 1030 und 1047 rechnen: Der Kessel-Reiter zeigt Strombedarf 635,20 / 0 / 4.790,09 / 640,19 MWh,
   unverändert (Sichtabnahme an 1030: 4.790,09).
2. Projektkopie 1018 mit der PV-Anlage von 1040 rechnen: Der PV-Reiter zeigt Strombedarf 0 (nicht −27,46), die
   PV-Einspeisung 6,60 MWh und die KWK-Einspeisung 27,46 MWh wie bisher.
3. Der Kern-Lauf ist grün gegen R20.

**Hinweis:** Kein Projekt führt eine Wärmepumpe im PV-Modus. Die Abnahme der Stelle 1 (PV-Modus nur auf PV-Überschuss)
greift erst mit einem solchen Projekt; bis dahin hält die Theorie in `StromStufeneingangKlemmeTests` die Regel.

## Konzeptvermerk (E28‑Q1…Q3)

In Konzept § 3.6, Absatz „Netzbezug nie negativ“, eingearbeitet: Der PV-Modus der Wärmepumpe reagiert nur auf
PV-Überschuss — ein BHKW-Überschuss zählt nie als PV-Überschuss —, und der Strom-Stufeneingang der Kessel- und der
PV-Zeile wird je Stunde bei 0 geklemmt, einheitlich mit der BHKW-Zeile (E28, Entscheide E28‑Q1…Q3).

## Logbuch

Kein Logbuchsatz: Für die bestehenden Projekte ändert sich nichts sichtbar — keine Referenzrechnung ändert sich,
betroffen ist allein eine Konstellation, die heute kein Projekt hat. Kein Wiki-Fachtext geändert.

## Papiere mit der Statuszeile

Statusdatei (#535, Nach #535); dieses Protokoll und der Eintrag im Dokumentations-Index (Reporting 142 → 143);
Register (Kopf, Familientafel, neue Familie R‑E28, R‑E27 Q7 „geprüft und gebaut in E28 (#535)“); Konzept (§ 3.6
Absatz „Netzbezug nie negativ“, § 6.1 Zeile E28, § 6.3 Nr. 36); Analysepapier § 5 Zeile E28. Im selben Papierschritt,
aber nicht Teil von E28: der Papier-Teil der Sichtprüfung § 6.3 Nr. 21 (Betriebskosten von 1030; Konzept § 6.2 und
§ 6.3 Nr. 21, Register R‑Rest; Statusdatei Nach #535 (h)). Kein Mockup (kein Dialog), kein Wiki-Fachtext.

## Offen

- **Strombedarfsdeckung der PV-Zeile** (`SimulationRunner.cs:1035-1036`, `SimulationErgebnisCtrl.cs:858/864`): teilt
  durch den ungeklemmten Strombedarf, hinter einem BHKW-Überschuss wäre die Deckung zu hoch — nicht Teil von E28‑Q3,
  zur Prüfung mit E29.
- **Vorrichtung für Stelle 1** mit echter WP im PV-Modus hinter einem BHKW-Überschuss: erst, wenn ein Referenzprojekt
  diese Konstellation bekommt.
- Die **Abnahme am Gerät** A‑E28‑1.
- **Gate** und **CI** (Nachweis oben).

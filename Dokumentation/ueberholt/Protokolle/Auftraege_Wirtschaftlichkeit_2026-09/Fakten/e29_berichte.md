# E29 — Bericht Phase 0 (Opus, 26.09.2026, Worktree e29 = 5826f97d, kein Commit, Arbeitsbaum sauber; kein Test- und kein Referenzlauf nötig, Zahlen aus R20 und den Berichten E26–E28)

**Kurzbefund:** Alle drei Punkte sind reiner Ausweis. Kein Kernwert der Wirtschaftlichkeit ändert sich, keine CSV der R20 wandert, und bei der Umstellung
mit Rückfall bleibt auch jeder ChartProben-Hash gleich. Die „BHKW-Einspeisung" gibt es heute schon genau, nämlich stündlich im KWK-Split der
Strommatrix. Die Ergebnisansicht kennt sie nicht, der Zeitreihensatz nur mit PV oder Flotte. Dazu kommen zwei neue Befunde: **N9** (der Stromgang der
Ergebnisansicht stapelt als „Heizkessel" den Stufeneingang statt des Kesselstroms, 1030: +4.790 MWh) und der Restpunkt E28 (a) (Deckung der PV-Zeile).

## (1) E27‑Q3 b — BHKW-Einspeisung

**Was die Größe ist.** Sie ist der KWK-Split der Strommatrix, stündlich (`StromMatrix.cs:227-238`):
`Einspeisung_h = max(0, BHKW_h − min(BHKW_h, max(0, STROMBEDARF_GESAMT_h − PV_GENUTZT_h)))`, Jahressumme `KwkEinspeisungGesamtMWh` (`:65/:237`).
- **Klemme und Kaskade:** Sie wirken schon richtig. `STROMBEDARF_GESAMT` ist der Rest nach der Kaskade plus der BHKW-Strom (`SimulationControl.cs:566-572`).
  Spätere Verbraucher derselben Viertelstunde, also WP, Heizstab, E-Kessel und Kälte, stecken damit schon drin. Die Klemme am Laufende
  (`:630-637`, E27) setzt nur den Netzbezug auf 0; die Einspeisung steht allein hier.
- **Takt:** stündlich. BHKW-Strom und Gesamtbedarf liegen als Stundenmittel vor (`ZeitreihenExtraktor.cs:36`).
- **Bestehende Teilreihe:** `ZeitreihenSatz.BHKW_UEBERSCHUSS` (`BerichtsDaten.cs:470`) wird nur gefüllt
  - mit PV aus `SimulationPV.BhkwUeberschuss` (`SimulationPV.cs:442`, `ZeitreihenExtraktor.cs:84-86`, Schwelle 0,5 kWh). Das ist mathematisch
    dieselbe Größe: Ist der Bedarf vor der PV ≤ 0, dann ist PV_GENUTZT = 0.
  - mit Flotte aus `BhkwNetzeinspeisungKw` (`:98`, nach der Batterie, Beschriftung „BHKW-Einspeisung" `:103`).
  - Ohne PV und ohne Flotte (1018, 1030) fehlt sie.
- **Unterschied zum Stufenüberschuss:** Der BHKW-Reiter kennt nur Stufengrößen. `StrombedarfMwh` ist der Stufeneingang, `ReststrombedarfMwh` ist
  Σ max(0, Bedarf_h − Strom_h) (`SimulationErgebnisCtrl.cs:731-745`, `SimulationControl.cs:4464`). Der Stufenüberschuss Σ max(0, Strom_h − Bedarf_h)
  weicht von der Einspeisung ab, sobald ein späterer Verbraucher den Überschuss aufnimmt (Frage Q1).
- **Werte (R20/E27):**

  | Projekt | BHKW-Einspeisung | Stufenüberschuss |
  |---|---|---|
  | 1018 | 27,4575 MWh (Strombedarf 0, BHKW 27,46) | gleich |
  | 1030 | 0,392 MWh (12 h) | gleich |
  | 1017, 1024, 1047 | 0 (keine Überschussstunde) | gleich |

**Vorschlag.**
- **Hilfsmethode:** `SimulationControl.BhkwEinspeisungStuendlich(double[] bhkwStrom, double[] bedarfGesamt, double[] pvGenutzt)` (`internal static`,
  neben `BhkwReststrombedarfMwh`). Sie steht wörtlich mit der Stundenformel der Strommatrix. Die Strommatrix selbst bleibt unberührt, damit der
  Kapitalwert gar nicht erst berührt wird; die Gleichheit hält ein Test.
- **Diagnosereihe:** die bestehende Konstante `BHKW_UEBERSCHUSS` wiederverwenden (Q2).
  - `ZeitreihenExtraktor` füllt sie zusätzlich im Fall „BHKW, ohne PV, ohne Flotte" aus der Hilfsmethode.
  - Einheit kWh je Stunde, dieselbe Schwelle > 0,5 kWh.
  - Die Zweige mit PV und mit Flotte bleiben bitgleich.
  - Den Doc-Kommentar der Konstante auf „BHKW-Einspeisung (KWK-Split; mit Flotte die Flottenbilanz)" nachziehen.
  - Leser: nur Excel (`ExcelBerichtGenerator.cs:1839`, heute nur im Flottenzweig) und `SzenarioMengen.ENERGIEREIHEN` (skaliert nur). **Kein
    Kapitalwert-Leser**: StromMatrix, KostenEmissionRechner und KennzahlenKatalog lesen sie nicht.
- **BHKW-Reiter:**
  - neues Feld `BhkwErgebnis.EinspeisungMwh` (`SimulationErgebnisCtrl.cs:693ff`), gerechnet in `Bhkw()` aus
    `Strombedarf_Verbraucher_viertelstuendlich` (Stundenmittel), `bh.stromproduktion` und `simulation_pv.Stromproduktion` (falls PV); mit
    Flotte `BhkwNetzeinspeisungKwh/1000` (Q3).
  - Zeile in `BhkwReiter.razor:89-93` **nach „Stromproduktion"** (analog „Wärmeüberschuss" direkt nach der Wärmeproduktion), vor der Deckung.
    Der Reststrombedarf bleibt die betonte Abschlusszeile.
  - Tooltip als `title` am `dt` (Muster `SIMERG_TIP_FLAECHE_GESCHAETZT`, `PhotovoltaikReiter.razor:210`). `Wertzeile` bekommt dafür einen
    optionalen Parameter `hinweis`; ohne ihn bleibt das Markup unverändert.
- **Texte:**

  | Schlüssel | de | en |
  |---|---|---|
  | `SIMERG_LBL_BHKW_EINSPEISUNG` | „Stromeinspeisung:" | „Electricity feed-in:" |
  | `SIMERG_TIP_BHKW_EINSPEISUNG` | „Je Stunde der BHKW-Strom, den die Verbraucher des Anschlusses nach der PV-Eigennutzung nicht abnehmen: Σ max(0, BHKW − max(0, Strombedarf aller Verbraucher − PV-Eigenverbrauch)). Dieselbe Menge wie die KWK-Einspeisung der Wirtschaftlichkeit; mit Speicherflotte die BHKW-Einspeisung der Flottenbilanz." | „Per hour, the CHP electricity not taken by the site's consumers after PV self-consumption: Σ max(0, CHP − max(0, demand of all consumers − PV self-consumption)). Same quantity as the CHP feed-in of the economic analysis; with a storage fleet, the CHP feed-in of the fleet balance." |

  Dazu den Designer nachziehen; die Wache prüft ihn.
- **Berichte:** Word bildet den Reiter nicht ab (`BausteineVergleich.cs:409` zeigt nur Wärme und Strom je BHKW). Die Strommatrix-Tabelle in Word und
  Excel führt die KWK-Einspeisung schon (`BausteineWirtschaftlichkeit.cs:1030`, `ExcelBerichtGenerator.cs:999`), Word bleibt also unverändert.
  - Excel-Monatsblock: optional eine Spalte „BHKW-Einspeisung" auch im Zweig ohne Flotte (`:1844-1847`), wenn die Reihe da ist (Q6).
  - Dann wandert die Kopfzeile der Messlatte `EPOS.Kern.Tests/Messlatten/Bericht_Excel_1030.txt:69`, weil 1030 0,392 MWh Überschuss hat.
    Neu einfrieren, begründet.
- **Mockup:** keines. Es kommt nur eine Zeile in einem bestehenden Reiter dazu, kein Dialog, und es gibt kein Reiter-Mockup.

## (2) E26‑Q6 — Strombilanz-Diagramm und Excel-Spalte „Strombedarf"

**Fundstellen:**
- `ChartRenderer.cs:455-476` (`StrombilanzMonateModell`): Die Linie ist `STROMBEDARF`, also nur der Projektbedarf.
  - Gestapelt: PV-Eigenverbrauch, BHKW-Strom, Netzbezug.
  - Einzige Nebenreihe ist der Name „Einspeisung" (`:767-769`).
  - Leser: Word `BausteineVergleich.cs:92`, `ChartProben` (`Program.cs:182-185`, `Program.GruppeC.cs:75-76`), `ChartRendererGruppeCTests`,
    `WordBerichtSvgWacheTests:355`.
- Excel `ExcelBerichtGenerator.cs:1830` (Monatsblock, Spalte „Strombedarf").

**Vorschlag (Q7 a / Q8 a):** In beiden Fällen `z.Hole(STROMBEDARF_GESAMT) ?? z.Hole(STROMBEDARF)`. Die Beschriftung bleibt „Strombedarf"; es ist
jetzt der Strombedarf des Anschlusses.
- Wirkung bei WP-Projekten: 1040 8,000 → 27,427 MWh/a, 1026 8,000 → 31,351, 1042 8,000 → 41,345 (E26).
- Ohne Speicher und ohne BHKW-Überschuss schließt der Stapel damit auf die Linie (PV-Eigen + BHKW + Netz = Gesamt).
- Es bleiben Abweichungen:
  - Speicherentladung: Der Stapel liegt um die Entladung unter der Linie.
  - BHKW-Überschuss: Der Stapel liegt um die BHKW-Einspeisung über der Linie, siehe Q7 b.

**ChartProben:** Die Proben zeichnen aus `SyntheticherSatz()` (`Program.cs:2417ff`) **ohne** `STROMBEDARF_GESAMT`. Der Rückfall greift, **kein Hash
wandert** (`strombilanz_monate.png` bleibt `6d916351…` in `messlatte_windows.sha256`, 161 Zeilen). **Eine neue Messlatte ist nicht nötig.**
- Sie wird nötig bei einer neuen Beschriftung (Q7 c), bei einer neuen ChartProbe oder wenn die Probe `STROMBEDARF_GESAMT` bekommt: Das Gate vergleicht
  per `diff` die ganze Liste.
- Den neuen Weg deshalb über einen Modelltest belegen, nicht über eine Probe.

**Berichts-Messlatten:**
- Word listet Bilder nur mit Größe (`Bericht_Word_1030.txt:37-38`).
- Excel listet Textzellen, keine Zahlen (`Bericht_Excel_1030.txt:69`).
- Beide bleiben also unverändert.

## (3) N6 — Übersicht „Strombedarf mit Eigenverbrauch" mit Kältestrom

**Heute:** `SimulationErgebnisCtrl.cs:199-202` rechnet Projekt + WP + Heizstab + Kessel(E-Kessel `StromverbrauchSpkMwh`), ohne Kältestrom. Leser:
- Nenner des Stromrings (`SimulationErgebnisHuelle.Bilder.cs:379`)
- Stromdeckung und Legenden-Summenzeile „Strombedarf 100 %" (`Anzeige.cs:162/192`, `UebersichtReiter.razor:388/448-451`)
- Anteile der Stromtabelle (`Anzeige.cs:369`)
- die leise Zeile „davon Eigenverbrauch der Wärmeerzeuger" (`UebersichtReiter.razor:402-407/616-627`, `SIMUEB_EIGENVERBRAUCH`)

**Folge:** Mit Kälte der Stufenrechnung enthält der Ring-Rest `ReststromMwh` den Kältestrom (`SimulationControl.Kaelte.cs:297-300`), der Nenner aber
nicht. Die Deckung ist dann zu hoch, Rest und Deckung können über 100 % gehen.

**Quelle:** `sim.Kaeltestrom_Stufenrechnung_stuendlich` (`SimulationControl.Kaelte.cs:57/297`, null ohne Kälte). Das ist der Kältestrom **der
Stufenrechnung**. Die Anlagen mit eigenem Zähler (E34) laufen daneben und stehen nicht im Netzbezug. Genau diese Menge steckt auch in
`STROMBEDARF_GESAMT` (Test `PvAusweisStromMatrixTests.cs:146-151`).

**Vorschlag (Q9 a):**
- neues Feld `UebersichtKennzahlen.KaeltestromStufeMwh = Σ/1000` (0 ohne Kälte), als **vierter Summand am Ende** (Projekt, WP, Heizstab, Kessel,
  Kälte). Ohne Kälte kommt + 0,0 dazu, das ist bitgleich.
- Eigenverbrauchszeile: dieselbe Reihenfolge, „Kältestrom x" mit dem vorhandenen `SIMUEB_LBL_KAELTESTROM` („Kältestrom"/„Cooling electricity"),
  nur mit Kälte.
- Satz mit Kälte: neuer Schlüssel `SIMUEB_EIGENVERBRAUCH_MIT_KAELTE`, de „davon Eigenverbrauch der Wärme- und Kälteerzeuger: {0} MWh/a", en
  „of which own consumption of the heating and cooling generators: {0} MWh/a". Ohne Kälte bleibt der alte Text, der bUnit-Test
  `UebersichtReiterTests:255` bleibt grün.
- Summenzeile: Die Legenden-Summe „Strombedarf" liest `D.StrombedarfMwh` und zieht von selbst nach. Eine eigene Summenzeile der Eigenverbräuche
  gibt es nicht und braucht es nicht.
- Kein Referenzprojekt rechnet Kälte (Kühlbetrieb aus), die Übersicht ist an allen 14 Projekten bitgleich.

## (4) Referenzlauf R20

`Referenzlauf/Ergebnisexport.cs` liest nur DB-Ergebniszeilen (`SELECT *`, `:531ff`), Vektoren (`:140-230`) und Emissionsskalare. Es liest weder
`ZeitreihenSatz` noch `StromMatrix` noch `SimulationErgebnisCtrl`. Damit:

| Punkt | Wirkung auf R20 |
|---|---|
| Q3 b (Zeitreihensatz + Reiter, nicht persistiert) | keine CSV |
| Q6 | keine CSV |
| N6 | keine CSV |
| N9 | keine CSV |
| PV-Deckung (Q10) | ändert `Tab_ErgebnisPhotovoltaik.Strombedarfsdeckung` = `aggregate.csv Photovoltaik.Strombedarfsdeckung`. Keines der PV-Projekte (1007, 1040, 1041, 1042, 1045, 1046) hat einen negativen PV-Stufeneingang (E28); `NetzbezugGeklemmt` gibt dann dasselbe Array zurück, die Summe ist **bitgleich**. |

**Erwartung: 14/14 PASS, 432/432 CSV byte-gleich, keine Neueinfrierung.**
- Ausnahme wäre Q5 c: eine Diagnosereihe als Vektor-CSV (`bhkw_einspeisung.csv` für 1017/1018/1024/1030/1047 plus `Vektor.….Summe` in aggregate).
  Das hieße fünf neue Dateien und R21 mit Einfrierregel. Nicht empfohlen.

## (5) Restpunkt E28 (a) und Nebenbefunde

- **PV-Deckungsgrad:**
  - Fundstellen: `SimulationRunner.cs:1036-1037`, `SimulationErgebnisCtrl.cs:858/864`. Beide teilen `Stromproduktion` (genutzt) durch
    Σ `Strombedarf_stuendlich`, ungeklemmt.
  - Die PV-Schleife klemmt je Stunde (`SimulationPV.cs:433-442`). Hinter einem BHKW-Überschuss mindern negative Stunden den Nenner, die Deckung
    wird überhöht und kann über 100 % gehen.
  - Kein Projekt ist betroffen (keine Konstellation BHKW + PV). In der Probe 1018 + PV 1040 ist die Summe ≤ 0, die Deckung dort 0 wie heute.
  - **Vorschlag Q10 a:** Nenner `NetzbezugGeklemmt(Strombedarf_stuendlich).Sum()`, stündlich wie die Schleife. Damit ist die Deckung ≤ 100 %
    gesichert, an beiden Stellen gleich.
- **N9 (neu, Stromgang der Ergebnisansicht):**
  - Fundstellen: `SimulationErgebnisHuelle.Bilder.cs:932-933` (Stapel „Heizkessel"), `:950-955` (Kontrolllinie „Summe Stromverbrauch") und
    `Wege.cs:316-318` (CSV) lesen `simulation_spk.Strombedarf_stuendlich`. Das ist der **Strom-Stufeneingang** des Kessels (`SimulationControl.cs:898/1308/1986`),
    nicht der Stromverbrauch des E-Kessels (`Stromverbrauch_stuendlich`, `SimulationSPK.cs:48/426`).

  | Projekt | Stapel heute | richtig |
  |---|---|---|
  | 1017 | 635,2 MWh | 20,12 |
  | 1024 | 409,31 | 47,67 |
  | 1030 | 4.790,09 | 0 |
  | 1047 | 640,19 | 1,0 |

  - Die Summenlinie ist entsprechend fast verdoppelt. Die Kälte fehlt dort ebenfalls. Reiner Ausweis (Q11).
- **N10:** Übersicht-Stromring, Stromtabelle (`Anzeige.cs:384`, `Bilder.cs:331-337`) und Word-Deckungstorte (`BausteineVergleich.cs:225`) zählen beim
  BHKW die ganze Produktion samt Einspeisung. Bei 1018 gibt das Deckung > 100 %. `BHKW.Strombedarfsdeckung` (`SimulationRunner.cs:624`, persistiert,
  aggregate) teilt durch den Projekt-Strombedarf, nicht durch den Gesamtbedarf. Das zu ändern bewegte R20. Nur benennen (Q12).
- **N11:** Im Strombilanz-Diagramm steht „Netzeinspeisung gesamt" (Flotte) im **Stapel**. Nebenreihe ist nur der Name „Einspeisung"
  (`ChartRenderer.cs:467-469/768`). Nur benennen.

## (6) Fragen

- **E29‑Q1 (Definition):**
  - (a) KWK-Split, stündlich, gleich `KwkEinspeisungGesamtMWh`.
  - (b) Stufenüberschuss Σ max(0, Strom_h − Stufeneingang_h). Er schlösse die Zeilenrechnung Bedarf − Produktion + Überschuss = Rest.
  - An den Referenzen sind beide gleich. **Empfehlung a**: Name und Zahl decken sich dann mit der Wirtschaftlichkeit.
- **E29‑Q2 (Reihenkonstante):**
  - (a) `BHKW_UEBERSCHUSS` wiederverwenden und im Fall ohne PV und ohne Flotte zusätzlich füllen.
  - (b) neue Konstante `BHKW_EINSPEISUNG`.
  - **Empfehlung a**: keine zweite Reihe für dieselbe Größe; die PV- und Flottenzweige bleiben bitgleich.
- **E29‑Q3 (Flotte im Reiter):**
  - (a) mit Flotte die `BhkwNetzeinspeisungKwh` der Flottenbilanz, so wie die Reihe es schon tut.
  - (b) immer der KWK-Split.
  - **Empfehlung a**, im Tooltip benannt. Kein Referenzprojekt hat BHKW und Flotte.
- **E29‑Q4 (Zeile):**
  - (a) immer zeigen (auch 0,00), nach „Stromproduktion", mit Tooltip.
  - (b) nur bei > 0.
  - **Empfehlung a**: stabile Zeilenordnung wie beim Wärmeüberschuss.
- **E29‑Q5 (weitere Orte der Diagnosereihe):**
  - (a) nur Zeitreihensatz (Bericht, Excel, Szenarien) und Reiter.
  - (b) zusätzlich eine wählbare Reihe im Stromgang samt CSV.
  - (c) zusätzlich ein Vektor-CSV im Referenzlauf, das hieße R21.
  - **Empfehlung a**; b als Folgepunkt.
- **E29‑Q6 (Excel ohne Flotte):**
  - (a) die Spalte „BHKW-Einspeisung" hinter „Einspeisung", wenn die Reihe da ist; die Messlatte `Bericht_Excel_1030` wird begründet neu
    eingefroren (Kopfzeile).
  - (b) keine Spalte.
  - **Empfehlung a**.
- **E29‑Q7 (Strombilanz-Diagramm):**
  - (a) Linie auf `STROMBEDARF_GESAMT` mit Rückfall; Beschriftung und Stapel bleiben, kein Hash wandert.
  - (b) zusätzlich den BHKW-Stapel in Eigenanteil und Einspeisung teilen (Nebenbalken „Einspeisung" = PV + BHKW).
  - (c) Beschriftung „Strombedarf gesamt"; dann wandern 1–2 Hashes, neue Messlatte nötig.
  - **Empfehlung a.**
- **E29‑Q8 (Excel „Strombedarf"):**
  - (a) Quelle umstellen, Beschriftung bleibt.
  - (b) zusätzlich die Spalte „davon Lastgang"; dann wandert die Messlatte Excel 1030.
  - **Empfehlung a.**
- **E29‑Q9 (N6):**
  - (a) Kältestrom der Stufenrechnung als vierter Summand und in der Eigenverbrauchszeile.
  - (b) auch der Kältestrom mit eigenem Zähler. Das bräche Deckung + Netzbezug = Bedarf, weil `ReststromMwh` ihn nicht führt.
  - **Empfehlung a.**
- **E29‑Q10 (PV-Deckung):**
  - (a) Nenner je Stunde bei 0 klemmen, an beiden Stellen.
  - (b) viertelstündlich wie die Zeile „Strombedarf".
  - (c) lassen.
  - **Empfehlung a**; R20 bitgleich.
- **E29‑Q11 (N9 Stromgang):**
  - (a) in E29 mitziehen: „Heizkessel" = `Stromverbrauch_stuendlich` in Bild, Summenlinie und CSV.
  - (b) wie a, dazu eine Reihe „Kältestrom".
  - (c) eigene Welle.
  - **Empfehlung a** (drei Zeilen, reiner Ausweis, grob sichtbar falsch).
- **E29‑Q12 (N10/N11):** nur benennen. **Empfehlung: so.**

**Aufwand Phase 1:** ~6 h + Gate.

| Teil | Aufwand |
|---|---|
| E29/1 Q3 b | ~2 h |
| E29/2 Q6 | ~0,5 h |
| E29/3 N6 | ~1 h |
| Q10 und N9 | ~0,5 h |
| E29/4 Tests | ~1,5 h |
| Voll-Lauf und Referenzlauf | ~1 h |

Kein Schema, Testdatenbank unverändert, Papiere nicht.

**Testvorschlag:**
1. **Theorie `BhkwEinspeisungStuendlich`:**

   | BHKW | Bedarf | PV | Ergebnis |
   |---|---|---|---|
   | 10 | 4 | 0 | 6 |
   | 10 | 4 | 3 | 9 |
   | 10 | 15 | 0 | 0 |
   | 0 | 5 | 0 | 0 |
   | 10 | 4 | 6 | 10 |

   Dazu: Ein kurzer Vektor zählt die fehlenden Stunden als 0, und über 8.760 Zufallsstunden gilt
   Σ/1000 = `StromMatrix.Baue(…).KwkEinspeisungGesamtMWh` (1e-9).
2. **Vorrichtung (Testdatenbank, `[Collection("Testdatenbank")]`, Kulturvorrichtung):**
   - Reiter `EinspeisungMwh`: 1018 27,4575 (4), 1030 0,392 (6), 1017/1024/1047 0. Jeweils gleich dem KWK-Split des Laufs.
   - `BHKW_UEBERSCHUSS` vorhanden bei 1018/1030, fehlt bei 1017.
   - Kopie 1018 + PV 1040 (Muster E28): Reihe stündlich = `SimulationPV.BhkwUeberschuss` (< 1e-9).
3. **bUnit `ErzeugerReiterTests.Bhkw_gliedert_…` (:757):** Die Stromliste wird „Strombedarf:, Stromproduktion:, Stromeinspeisung:,
   Strombedarfsdeckung:, Reststrombedarf:". Das `title` trägt die Formel; die englische Beschriftung wird geprüft.
4. **Modelltest Strombilanz:** Ein Satz mit `STROMBEDARF` und `STROMBEDARF_GESAMT` → die Linie ist die Monatssumme der Gesamtreihe. Ohne
   Gesamtreihe ist das Modell gleich wie heute. Excel: Die Spalte „Strombedarf" trägt die Gesamtreihe, und gegebenenfalls ist die Spalte
   „BHKW-Einspeisung" da.
5. **N6:**
   - `SimulationErgebnisCtrlTests:152` um den Kälte-Summanden erweitern.
   - Kälte-Vorrichtung nach `KaeltestromAbrechnungTests` (1045-Kopie): `StrombedarfMitEigenverbrauchMwh` = Σ `Strombedarf_Verbraucher_viertelstuendlich`/4000
     (1e-6) und größer als die alte Summe um den Kältestrom.
   - bUnit: Die Eigenverbrauchszeile nennt „Kältestrom" und den Satz mit Kälte.
6. **Q10:** Theorie mit negativen Stunden, Deckung ≤ 100 %. Anker 1040 bitgleich.
7. **N9:** Die Hüllenprobe 1017 zeigt als Stromgang-„Heizkessel" die Summe 20,12 MWh, nicht 635,2.
8. **Nachweise:**
   - Referenzlauf 14/14 gegen R20 byte-gleich
   - ChartProben: 161 Hashes gleich mit der Messlatte (JA)
   - Voll-Lauf mit Schaltern, Testhost-Regel

# E29 — Bericht Phase 1 (Opus, 26.09.2026 ca. 10:10, e29 = ed838081 über 5826f97d; Statusnummer #536; Testdatenbank unverändert 3ac19fa9; Arbeitsbaum sauber)

Entscheide wie freigegeben: E29‑Q1 a … Q11 a, Q12 nur benennen.

**Commits** (Trailer Opus 5.5, Betreffe mit #536):

| Commit | Inhalt |
|---|---|
| `71d74522` E29/1 | Q3 b: `SimulationControl.BhkwEinspeisungStuendlich` (`:4492`, Stundenformel des KWK-Splits wörtlich) und `BhkwEinspeisungDesLaufs` (`:4519`); `ZeitreihenExtraktor.cs:108-121` füllt `BHKW_UEBERSCHUSS` auch ohne PV und ohne Flotte (Schwelle 0,5 kWh; PV- und Flottenzweig unverändert); Doc der Konstante `BerichtsDaten.cs:470`; `BhkwErgebnis.EinspeisungMwh` (`SimulationErgebnisCtrl.cs:741/781`, `BhkwEinspeisungMwh` `:805`, mit Flotte `BhkwNetzeinspeisungKwh`); `BhkwReiter.razor:94` Zeile nach „Stromproduktion", `Wertzeile(…, hinweis)` → `title` am `dt` (`:318`, ohne Hinweis kein Attribut); Ressourcen + Designer (`designer_neu.py`) |
| `656718a1` E29/2 | Q6 a: Excel-Monatsblock ohne Flotte mit Spalte „BHKW-Einspeisung" (`ExcelBerichtGenerator.cs:1855-1858`); Messlatte `Bericht_Excel_1030.txt` neu (siehe unten) |
| `39e540c3` E29/3 | E26‑Q6 (Q7 a/Q8 a): `ChartRenderer.StrombilanzMonateModell` Linie `STROMBEDARF_GESAMT ?? STROMBEDARF` (`:457-463`), Excel „Strombedarf" dieselbe Quelle (`ExcelBerichtGenerator.cs:1831-1837`); Beschriftung und Stapel unverändert |
| `30572adb` E29/4 | N6 (Q9 a): `UebersichtKennzahlen.KaeltestromStufeMwh` (Σ `Kaeltestrom_Stufenrechnung_stuendlich`/1000) als vierter Summand am Ende (`SimulationErgebnisCtrl.cs:211-216`); `UebersichtReiter.razor:405/630` Satz mit Kälte und „Kältestrom x" hinter dem Kessel; Ressource + Designer |
| `83418db8` E29/5 | Q10 a: PV-Deckungsgrad-Nenner je Stunde geklemmt (`SimulationRunner.cs:1037-1043`, `SimulationErgebnisCtrl.cs:909`); Q11 a / N9: `SimulationErgebnisHuelle.StromverbrauchKessel` (`Bilder.cs:919`) für Stapel „Heizkessel" (`:943`), Summenlinie (`:964`) und CSV (`Wege.cs:320`) |
| `80b29c7f` E29/6 | Tests (unten); `ExcelBerichtGenerator.MonatsBlock` internal |
| `ed838081` E29/6 Nachschliff | UTF-8-BOM der neuen Testklasse (Wache `QuelltextKodierungWacheTests` im Voll-Lauf rot, danach grün) |

**Texte:**

| Schlüssel | de | en |
|---|---|---|
| `SIMERG_LBL_BHKW_EINSPEISUNG` | Stromeinspeisung: | Electricity feed-in: |
| `SIMERG_TIP_BHKW_EINSPEISUNG` | Je Stunde der BHKW-Strom, den die Verbraucher des Anschlusses nach der PV-Eigennutzung nicht abnehmen: Σ max(0, BHKW − max(0, Strombedarf aller Verbraucher − PV-Eigenverbrauch)). Dieselbe Menge wie die KWK-Einspeisung der Wirtschaftlichkeit; mit Speicherflotte die BHKW-Einspeisung der Flottenbilanz. | Per hour, the CHP electricity not taken by the site's consumers after PV self-consumption: Σ max(0, CHP − max(0, demand of all consumers − PV self-consumption)). Same quantity as the CHP feed-in of the economic analysis; with a storage fleet, the CHP feed-in of the fleet balance. |
| `SIMUEB_EIGENVERBRAUCH_MIT_KAELTE` | davon Eigenverbrauch der Wärme- und Kälteerzeuger: {0} MWh/a | of which own consumption of the heating and cooling generators: {0} MWh/a |
| (vorhanden) `SIMUEB_LBL_KAELTESTROM` | Kältestrom | Cooling electricity |
| Excel-Kopf (Bericht, deutsch fest) | BHKW-Einspeisung | — |

**Messlatte `EPOS.Kern.Tests/Messlatten/Bericht_Excel_1030.txt` (begründet neu eingefroren, E29‑Q6 a):**
- alt: `## Blatt 4 „Stamm" · 25 Z. × 7 Sp. · …` und `Z13: A=Monat | B=Wärmebedarf | C=BHKW-Wärme | D=Spitzenkessel | E=Strombedarf | F=BHKW-Strom | G=Netzbezug`
- neu: `## Blatt 4 „Stamm" · 25 Z. × 8 Sp. · …` und `Z13: A=Monat | B=Wärmebedarf | C=BHKW-Wärme | D=Spitzenkessel | E=Strombedarf | F=BHKW-Strom | G=BHKW-Einspeisung | H=Netzbezug`
- Grund: 1030 hat 0,392 MWh BHKW-Einspeisung (12 h); die Reihe steht jetzt auch ohne PV/Flotte, der Monatsblock führt sie als Spalte. Sonst keine Zeile anders (2 Zeilen im Diff). Word-Messlatten und Excel „Gruppe" unverändert.

**Tests:**
- neu `EPOS.Kern.Tests/BhkwEinspeisungAusweisTests.cs` (21 Fälle, `[Collection("Testdatenbank")]`, `Kulturvorrichtung`):
  - Theorie Stundenformel (10/4/0 → 6, 10/4/3 → 9, 10/15/0 → 0, 0/5/0 → 0, 10/4/6 → 10); kurzer Bedarfsvektor = Eigenstrom; 8.760 Zufallsstunden Σ = `StromMatrix.KwkEinspeisungGesamtMWh` (1e-9).
  - Vorrichtung Reiter = Reihe = KWK-Split: 1018 27,4575, 1030 0,392, 1017/1024/1047 0 (Reihe fehlt).
  - 1018 + PV 1040: Stundenformel = `SimulationPV.BhkwUeberschuss` (< 1e-9), Reiter 27,4575, PV-Deckung ∈ [0; 100], Ergebnis = Ansicht; 1040 PV-Deckung bitgleich zur alten Formel; Klemme des Nenners.
  - Strombilanz-Linie = Gesamtbedarf (PNG-Gleichheit; Rückfall ungleich); Excel-Monatsblock (Kopf, Januar 2,232 / 0,372 MWh; ohne Gesamtreihe 0,744).
  - N9: 1017 20,12 statt 635,2, 1030 0 statt 4.790,09.
  - N6 an 1045 mit Kühlung: Nenner = vier Glieder + Kältestrom = Σ `Strombedarf_Verbraucher`/4000 (3 Stellen); mit eigenem Zähler Kältestrom 0; ohne Kälte (1040) bitgleich.
- `ErzeugerReiterTests`: Stromliste „Strombedarf:, Stromproduktion:, Stromeinspeisung:, Strombedarfsdeckung:, Reststrombedarf:"; neu `Bhkw_zeigt_die_Stromeinspeisung_mit_Formel_im_Tooltip` (genau ein `dt[title]`).
- `UebersichtReiterTests`: Bestandssatz ohne Kälte; neu `Mit_Kaelte_nennt_die_Eigenverbrauchszeile_den_Kaeltestrom`.

**Nachweise (E29/7):**

| Prüfung | Ergebnis |
|---|---|
| Release-Build `WP-Plan.Kern.slnf` | 0 Fehler |
| ChartProben (174 Bilder, 0 Verstöße) | 161 Hashes, `diff` gegen `C:\Waermeplan\.claude\gate\messlatte_windows.sha256` **leer** |
| Voller Lauf `WP-Plan.Kern.slnf` mit Schaltern (09:54–10:02) | Kern 8.120/8.122 (1 übersprungen, 1 rot: BOM der neuen Datei → `ed838081`), UI 6.533, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27/28 (1 übersprungen) |
| Nachlauf nach BOM-Fix | `QuelltextKodierungWache`, `BhkwEinspeisungAusweisTests`, Dokumentationswachen: 58/58 grün → Kern 8.121/8.122, 1 übersprungen, 0 rot |
| Referenzlauf 15 Projekte (e29 = ed838081), `vergleich` gegen R20 | **14/14 PASS, 432/432 CSV byte-gleich**; GESAMT FAIL allein wegen 1048 „nur im Vergleichslauf" (keine R20-Basis) |
| 1048 | 32/32 CSV byte-gleich zum Lauf auf 5826f97d (temporärer Worktree `b29`, danach entfernt) |
| Testdatenbank | unverändert 3ac19fa9 |

Testhost-Regel: jeder Lauf über das Warteskript (warten, 5–30 s Zufallspause, erneut prüfen); ein fremder Testhost lief 09:25–09:42.

**Wirkung:** keine R20-CSV, kein Kapitalwert, kein Schema, keine ChartProben-Hashes. Sichtbar: BHKW-Reiter (neue Zeile); Übersicht nur mit Kälte; Stromgang „Heizkessel"/Summenlinie/CSV bei Projekten mit Kessel (1017 −615 MWh, 1024 −362 MWh, 1030 −4.790 MWh, 1047 −639 MWh); Strombilanz-Linie und Excel „Strombedarf" bei Projekten mit WP-/Kessel-/Kältestrom (1040 8,0 → 27,4 MWh); Excel-Spalte „BHKW-Einspeisung" bei 1018/1030.

**Aufwand:** ~1 h Bau und Tests, ~45 min Läufe.

**Abnahmevorschlag A‑E29‑1 (Windows, Testdatenbank):**
1. 1018 rechnen: BHKW-Reiter zeigt „Stromeinspeisung: 27,46 MWh/a" nach „Stromproduktion", Tooltip mit Formel; 1030: 0,39; 1017: 0,00.
2. 1017 Stromgang: „Heizkessel" ≈ 20 MWh/a, Summenlinie ohne den Stufeneingang; CSV-Spalte Heizkessel ebenso.
3. Bericht 1030 (Word + Excel): Excel-Monatsblock mit Spalte „BHKW-Einspeisung"; 1040: Strombilanz-Linie und Excel „Strombedarf" ≈ 27,4 statt 8,0 MWh/a.
4. 1045 mit Kühlung (wie E34): Übersicht-Stromspalte „davon Eigenverbrauch der Wärme- und Kälteerzeuger: … · Kältestrom x", Deckung plausibel.
5. Englische Oberfläche: „Electricity feed-in:".
6. Kern-Lauf gegen R20 grün.

**Restpunkte (benannt, kein Code, E29‑Q12):**
- **N10:** Übersicht-Stromring (`SimulationErgebnisHuelle.Bilder.cs:336`), Stromtabelle (`Anzeige.cs:381`) und Word-Deckungstorte (`BausteineVergleich.cs:223-226`) zählen beim BHKW die ganze Produktion samt Einspeisung als Deckung — Größe = BHKW-Einspeisung (1018 27,46 MWh bei Bedarf 0, 1030 0,392 MWh). `BHKW.Strombedarfsdeckung` (`SimulationRunner.cs:624`, persistiert, aggregate) teilt durch den Projekt-Strombedarf statt den Gesamtbedarf (R20: 1017 5,48 %, 1024 26,22 %, 1030 9,02 %, 1047 5,34 %) — Änderung bewegte R20.
- **N11:** Das Strombilanz-Diagramm stapelt „Netzeinspeisung gesamt" der Flotte in der Deckung (`ChartRenderer.cs:473-475`; Nebenbalken nur für den Namen „Einspeisung" `:773`) — Größe 1046: 0,895 MWh/a (`Flotte.NetzeinspeisungKwh` 894,9).
- Papiere (Konzept § 3.6, Register R‑E27 Q3 b, R‑E26 Q6/N6, R‑E28 Restpunkt (a), neue Familie R‑E29, Statuszeile #536) macht der Papieragent. Logbuch-Vorschlag: „Der BHKW-Reiter weist die Stromeinspeisung des BHKW aus; Strombilanz und Excel-Monatswerte messen den Strombedarf aller Verbraucher; der Stromgang zeigt den Stromverbrauch des Heizkessels."

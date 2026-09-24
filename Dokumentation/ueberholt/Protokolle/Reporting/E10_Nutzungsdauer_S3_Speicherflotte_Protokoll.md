# E10 — Nutzungsdauer S3 und Speicherflotte: Sätze je Technik, Speicherflotte an der Nutzungsdauertabelle, Kennzeichnung A8 (Protokoll, 24.09.2026)

Statuszeile #463 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Etappe E10 des Analysepapiers
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
(§ 5 Zeile E10: „Instandsetzung/Wartung je Technik aus den vorhandenen Spalten, Gerätekataloge; Speicherflotte an
`Tab_Nutzungsdauer` mit Neueinfrieren der Basis; geräteeigene Spalten kennzeichnen (A8)"); Nutzungsdauer-Konzept
[`Konzept_Nutzungsdauer_AfA_EPOS-Plan.md`](../../../aktuell/Konzept_Nutzungsdauer_AfA_EPOS-Plan.md) (Stufe S3, ND‑Q6,
ND‑Q7); Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 2.13 (3) (U39), § 3.4 und § 6.3 Nr. 9h und 19; Mockup `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`,
Kategorien 1 und 2 und Anhangzeile U39; Basis und Einfrierregeln in
[`Referenzlaeufe/LIESMICH.md`](../../../../Referenzlaeufe/LIESMICH.md). Entscheide: A7 (R‑A, 20.09.2026: Speicherflotte
mit ND‑S3, eigener Auftrag mit Neueinfrieren), A8 (R‑A, 20.09.2026: geräteeigene Spalten nicht jetzt abkündigen, nur
kennzeichnen), ND‑Q4, ND‑Q6 und ND‑Q7 (R‑ND, 14.09.2026) im
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md),
die sieben Fragen dieser Welle unter R‑E10. Anlass: der Anwender, „Fahre fort" (24.09.2026) — Etappe E10, Nutzungsdauer
Stufe S3 und Speicherflotte. Einen eigenen Entscheid zu ND‑S3 („S3 nur nach Entscheid", Nutzungsdauer-Konzept § 3) gab
es nicht; die Welle baut die Empfehlungen und legt sie als E10‑Q1 bis E10‑Q7 vor. Vorgänger:
[`E9b_Szenarioabdeckung_Dialoge_Protokoll.md`](E9b_Szenarioabdeckung_Dialoge_Protokoll.md). Zweig `e10` von `48d8836d`
(`origin` nach dem Push #462, Schemastand 119), zwei Phasen; Phase 1: `62795858` (E10/1), `bdd4aba9` (E10/2), `056ccd0d`
(E10/3), `71489e2d` (E10/4), `b5f9f249` (E10/5), `375dc0fa` (E10/6), `9d8999f3` (E10/7), `511a6647` (E10/8); Phase 2:
`2c41c642` (E10/9, Umbau), Merge `f9eb2615` (`origin` = `502fea3c`, Dialog Design #466), `b8cb7287` (E10/10,
LIESMICH). Merge `94521f2e` über `origin` = `502fea3c`; der Baum gleicht `b8cb7287`. Opus 5.5 im Worktree
`.claude/worktrees/e10`. **Schemaschritt 120** (die Sätze der Nutzungsdauertabelle, reines DML); **ohne Zutun keine
Rechenwirkung auf die Projektwirtschaftlichkeit**; die Flottenstudie rechnet den Restwert je Einheit linear; **keine
neue Basis** — der Referenzlauf ist byte-gleich zu R13.

## Befund vor der Welle

- **Die Satzspalten lagen, ungezeigt und ungelesen:** `Instandsetzung_Prozent` und `Wartung_Prozent` stehen seit S1 in
  `Tab_Nutzungsdauer` (ND‑Q6: „Anzeige ab S3"); gelesen und geschrieben hat sie nur die Verwaltung, kein Rechenweg; die
  Saat ließ sie leer, der Dialog zeigte sie nicht.
- **Die „Konstanten" der Betriebskosten** sind die Sätze und Empfehlungsbereiche der Betriebsvorlagen-Saat
  (`SchemaKatalog.Schritt39_Vorlagen`, Positionen „Instandhaltung …" mit „% der Investition"); `BetriebskostenCtrl.Betrag`
  bekommt den Satz als Parameter und kennt keine Technik.
- **Die Speicherflotte** führte Ersatz und Restwert über zwei Felder je Einheit im JSON des Flottenstands
  (`ErsatzintervallJahre`, `RestwertEuro` in `Tab_SpeicherAuslegung`); der Restwert war ein fester, eingegebener Betrag,
  Intervall 0 hieß „kein Ersatz". 1046: zwei Einheiten, Intervall 10 a, Restwert 500 bzw. 300 € (gemessen #452).
- **Die geräteeigenen Nutzungsdauer-Spalten** (`Tab_BHKW.Nutzungsdauer`, INTEGER, 3 von 6 gepflegt, Stamm 44 von 79;
  `Tab_Heizkessel.Nutzungsdauer`, REAL, 1 von 22, Stamm 0 von 63) standen ohne Kennzeichen im Katalog und hatten keinen
  Leser in der Wirtschaftlichkeit; `Tab_StromspeicherVariante.Nutzungsdauer` rechnet in Speicherwirtschaftlichkeit und
  Peak-Shaving.
- **`Referenzlaeufe/LIESMICH.md`** führte die Nachträge der Basis bis 115 und 119; 116 bis 118 (E9a) fehlten.

## Gebaut — Phase 1 (E10/1 bis E10/8)

- **Die Sätze im Dialog „Nutzungsdauern (AfA)" (E10/1, `62795858`).** Zwei Spalten mehr — „Instandsetzung [%/a]" und
  „Wartung [%/a]", Zahlenfelder 0 bis 100 mit zwei Nachkommastellen, in der Tabelle und in der Neuzeile —, darüber eine
  leise Zeile (`ND_SAETZE_HINWEIS`) mit der Quelle VDI 2067 Blatt 1, Tabelle A2; das Windows-Fenster 1040 → 1240 px.
  `NutzungsdauerCtrl` lädt und schreibt die Sätze, „Auslieferungswerte wiederherstellen" setzt sie auf die Saat zurück.
  KI-Sicht und Feldkarte `neue_instandsetzung`, `neue_wartung` und die Spalten `instandsetzung`, `wartung`; Maskenwache
  `NutzungsdauerDialog` 8 → 12.
- **Schemaschritt 120 sät die Sätze (E10/2, `bdd4aba9`).** `SCHRITT_120_NUTZUNGSDAUER_SAETZE`, reines DML an
  `Tab_Nutzungsdauer`: Die leeren Satzzellen der Standardzeilen bekommen die Mitte des Empfehlungsbereichs derselben
  Position der Betriebsvorlagen; gesetzt wird nur, was leer ist, der Schritt ist wiederholbar (Saat-Tafel unten). Eine
  Quelle (`NutzungsdauerSaetze`) für Migration, `Werkzeuge/Testdatenbankschema`, die Nachzieh-Liste der Testvorrichtung
  und den Nachweis; Zielversion 119 → 120; Testdatenbank LFS `6259b348…` → `52c4729d…` (67.784.704 Byte, integrity ok).
  **Warum ein Schritt statt einer Nachsaat beim Start:** Die iOS-Schale migriert nicht, sie nimmt die Seed-Kopie — so
  bleibt es bei einer Quelle für Migration, Werkzeug und Testdatenbank. Der Auftrag hatte keinen Schritt geplant und
  für den Bedarfsfall 120 genannt; `origin` stand bei 119, die Zapfprofil-Stufe Z4 nimmt 121.
- **Vorbelegung (E10/3, `056ccd0d`).** Die Zuordnung Position → Technik und Satzart steht in `NutzungsdauerSaetze` (über
  den Positionsschlüssel `DbWerte.VDI_POS_*`, Abweichung 1), die Regel einmal in `NutzungsdauerSatzCtrl.WirksamerSatz`:
  Ein gepflegter Satz bleibt; eine Position „Instandhaltung …"/„Wartung …" mit „% der Investition" ohne Satz bekommt
  den Satz der Tabelle ihrer Technik (die Zeile der Positionsart, sonst die Standardzeile). Die Vorlagenübernahme
  schreibt ihn in eine neue Position, deren Vorlage keinen Satz trägt (`KostenVorlagenUebernahmeCtrl.SatzOderTabelle`);
  auf der Betriebsseite der Kostenverwaltung heißt der vierte Rasterknopf „Sätze vorbelegen…" — dieselbe Bedienung wie
  „Nutzungsdauern vorbelegen…" mit eigenen Texten (`ND_SAETZE_VORBELEGEN_*`, Abweichung 2): leere Sätze füllen, bei
  belegten Positionen die Rückfrage mit ihrer Anzahl, die Zahl in der Statuszeile. Unter dem Satzfeld steht die
  Herkunftszeile „2 % · Satz aus Nutzungsdauertabelle: Heizkessel · Wärmeerzeuger (Instandsetzung)"
  (`ND_SATZ_HERLEITUNG`).
- **Rechenweg (E10/4, `71489e2d`) — mit E10/9 zurückgebaut.** Gebaut war „gepflegter Satz vor Tabelle vor 0", ein
  erfasster Betrag schlug die Tabelle (I‑2, Abweichung 3); Herleitung und Formelmappe nannten die Herkunft;
  Nachweisfassung 9 → 10. Den Rückbau und seinen Grund nennt der nächste Abschnitt.
- **Gerätekataloge (E10/5, `b5f9f249`; ND‑Q7 b, E10‑Q2 a).** Ein **neuer** Kesselkatalogeintrag mit der Wartungseinheit
  „%/a" und ohne Betrag übernimmt den Wartungssatz der Standardzeile Heizkessel
  (`HeizkesselStammCtrl.WartungAusNutzungsdauertabelle`, einmalige Kopie); bestehende Einträge, „€/a" und „€/kWh"
  bleiben unberührt. Die Auslieferung führt keinen Wartungssatz — die Vorbelegung greift, sobald der Anwender einen
  pflegt. Das BHKW führt seine Wartung fest in €/kWh el (`Wartungskosten_kwhel`) und bekommt keine Vorbelegung; die
  Asymmetrie (Konzept § 6.3 Nr. 19) ist dokumentiert, nicht behoben (E10‑Q6 a).
- **Speicherflotte (E10/6, `375dc0fa`; A7, E10‑Q3 a).** Engine: `FlottenWirtschaftlichkeit.LinearerRestwert` — der
  Restwert je Einheit ist `Betrag der letzten Beschaffung × Restdauer ÷ n` auf der Ersatzkette der Flotte (fällig ist
  jedes Jahr 1…T, dessen Nummer durch n teilbar ist, auch das letzte; fällt ein Ersatz ins letzte Jahr, steht sein voller
  Betrag als Restwert daneben); `Bewerte` meldet den Restwert nominal (`RestwertEuro` des Ergebnisses); der zusätzliche
  Restwert der Studie rechnet weiter. Das Feld `RestwertEuro` der Einheit ist Altfeld — gespeichert, nicht gelesen, im
  Flotten-Editor als „Restwert der Einheit (Gerätedaten):" mit der Zeile `FLOTTE_ED_RESTWERT_ALTFELD` gekennzeichnet, in
  der KI-Sicht ebenso; `FlottenSimulator.PruefeEinheit` prüft es nicht mehr. Kern: Eine Einheit ohne eigenes Intervall
  nimmt die Nutzungsdauer der Standardzeile „Stromspeicher · Batterie" (10 a;
  `SpeicherFlottenStudieCtrl.ErsatzintervallVorgabeJahre`, eingesetzt nur im Studienlauf, beschafft in
  `SpeicherAuslegungCtrl.QuellenBeschaffen`); der Editor nennt die Vorgabe in einer Hinweiszeile
  (`FLOTTE_ED_ERSATZINTERVALL_*`).
- **Kennzeichnung A8 (E10/7, `9d8999f3`; E10‑Q4 a).** Die Nutzungsdauer von Heizkessel und BHKW heißt in Katalog und
  Aufklapper „Alle Daten anzeigen" „Nutzungsdauer (Gerätedaten):" (`HZKK_LBL_NUTZUNGSDAUER`, `BHKWK_LBL_NUTZUNGSDAUER`)
  mit dem Tooltip „Gerätedaten — nicht rechenwirksam; maßgeblich ist die Nutzungsdauertabelle (Nutzungsdauern (AfA))"
  (`KBROW_ND_GERAETEDATEN_HINWEIS`, über den neuen Parameter `Titel` an Zahlen-, Ganzzahl- und Textfeld), mit KI-Vermerk
  (`KI_DLG_KBROW_NUTZUNGSDAUER_ERL`) und Vermerk in der Parameterverwendung; kein Schemaschritt (H), keine Spalte
  entfernt. Der Halbsatz „die Speichervariante sollte die Positionsarten 20/21 lesen" ist nicht gebaut —
  `StromspeicherVarianteModel` blieb laut Auftrag unverändert.
- **Tests (E10/8, `511a6647`).** Kern neu: `NutzungsdauerS3Tests` (16 — Saat aus den Konstanten, Zuordnung, Schritt 120
  samt Wiederholbarkeit, Werkzeug-Wache, Speichern und Wiederherstellen, Hülle, Vorrang, Herkunftszeile,
  Vorlagenübernahme an 1006, „Sätze vorbelegen" an 1018, Formelmappe, Kessel in %/a), `SpeicherFlottenNutzungsdauerTests`
  (9 — linearer Restwert, Altfeld rechnet nicht, Rundung und Vorrang des Intervalls, Vorgabe aus der Batteriezeile, 1046
  vorher/nachher), `NutzungsdauerKennzeichnungTests` (4 Methoden, 7 Fälle — Beschriftung, Vermerk, KI-Feldkarte,
  Parameterverwendung, de/en); ergänzt `StromspeicherAuslegungCtrlTests` (+1, der Studienlauf setzt 10 a ein), angepasst
  `ErgebnisansichtTests` (Nachweisfassung 10). bUnit: `KatalogfelderVermerkTests` (3) neu, dazu der Nutzungsdauer-Dialog
  (+2), der Kostendialog (+2), `VorlagenZeile` (+1) und der Flotten-Editor (+1). Engine: `FlottenWirtschaftlichkeitTests`
  erwartet −390 statt −350 — der feste Restwert 40 der Einheit rechnet nicht mehr, der Restwert der Studie (60) bleibt.
- **Ressourcen** (de und en): **24 neu** — Dialog `ND_SP_INSTANDSETZUNG`, `ND_SP_WARTUNG`, `ND_SAETZE_HINWEIS`;
  Assistent acht `KI_DLG_NUD_*` (Name und Erläuterung von Instandsetzung und Wartung in Tabelle und Neuzeile); Knopf
  `ND_SAETZE_VORBELEGEN_BTN`, `_STATUS`, `_FRAGE`, `_KEINE`; Herkunft `ND_SATZ_HERLEITUNG`, `ND_SATZ_HERKUNFT`,
  `ND_SATZART_INSTANDSETZUNG`, `ND_SATZART_WARTUNG`; Flotten-Editor `FLOTTE_ED_ERSATZINTERVALL_VORGABE`, `_TABELLE`,
  `_OHNE_TABELLE`, `FLOTTE_ED_RESTWERT_ALTFELD`; Katalog `KBROW_ND_GERAETEDATEN_HINWEIS`. **6 geändert:**
  `FLOTTE_ED_RESTWERT_EINHEIT`, `KI_DLG_FLE_RESTWERT_ERL`, `KI_DLG_FLE_ERSATZINTERVALL_ERL`, `HZKK_LBL_NUTZUNGSDAUER`,
  `BHKWK_LBL_NUTZUNGSDAUER`, `KI_DLG_KBROW_NUTZUNGSDAUER_ERL`. Mit E10/9 sind fünf der neuen Texte angepasst
  (`ND_SAETZE_HINWEIS`, `KI_DLG_NUD_INST_ERL`, `KI_DLG_NUD_WART_ERL`, `ND_SAETZE_VORBELEGEN_KEINE`,
  `ND_SAETZE_VORBELEGEN_FRAGE`). Je Sprache 9.044 Einträge (9.020 von `origin` + 24); Designer neu erzeugt und
  wiederholbar.

## Der Umbau E10/9 — die Sätze wirken nur über die ausdrückliche Vorbelegung

Der Phase‑1-Bericht nannte die Vorab-Zahlen des Rechenwegs aus E10/4: Mit dem Tabellensatz zur Rechenzeit bekäme **1030
ohne Zutun +37.200 €/a** Betriebskosten — 17.700 € für die Anlage 14920 (6 % auf 295.000 €), **noch einmal 17.700 € für
die Anlage 14921**, die keine eigene Investition führt und deren Basis deshalb nach der Stufung H4a (Anlage →
Komponente → Projekt) auf die ganze BHKW-Komponente zurückfällt, und 1.800 € für den Kessel (2 % auf 90.000 €);
daneben stehen in 1030 die Sammelposten „BHKW" 18.000 €/a und „Heizkessel" 2.000 €/a. 1018 bekäme +5.097,66 €/a, 1026
eine Instandhaltung auf fremder Basis. Das widerspricht **ND‑Q4** („nichts ändert eine gerechnete Wirtschaftlichkeit
ohne Zutun", R‑ND). Die Anweisung der Sitzung: **Umbau als E10/9** — die Tabellensätze wirken nur über die ausdrückliche
Vorbelegung, der Rechenweg bleibt, wie er vor E10 war, die Anker bleiben; E10‑Q1 wird neu gefasst.

- **Rechenweg:** `LiesBetriebskostenTopfe` und die Beträge der Nachweisliste rechnen wieder genau wie vor E10 — mit dem
  gepflegten Satz, sonst dem erfassten Betrag, sonst 0.
- **Herkunft:** Die Nachweisliste nennt „Satz aus Nutzungsdauertabelle" nur noch als Ausweis, wenn der gepflegte Satz
  genau dem der Tabelle gleicht (`NutzungsdauerSatzCtrl.AusTabelle`); Herleitung in Wort- und Tabellenbericht und
  Formelmappe (Stufe 3) hängen „· Satz aus Nutzungsdauertabelle" an; die Nachweisfassung 10 bleibt
  (`KostenPositionNachweis.SatzHerkunft`).
- **`WirksamerSatz`** ist die Kernfunktion für Vorbelegung und Anzeige, der Betrags-Parameter entfällt.
- **Die Herkunftszeile im Dialog** steht nur bei einem Satz gleich dem der Tabelle; ein leeres Feld rechnet mit nichts
  und trägt keine Zeile.
- **„Sätze vorbelegen…"** schreibt den Satz erst mit „Speichern"; eine Zeile mit erfasstem Betrag, aber ohne Satz zählt
  als belegt und löst die Rückfrage aus.
- **Vorlagenübernahme:** Die vom Anwender ausgelöste Übernahme schreibt den Tabellensatz. **Abweichung:** Die
  automatische Pflichtanlage des Wizards (`PflichtpositionenSicherstellen`) schreibt keinen — sie läuft beim Speichern
  der Erzeuger ohne Zutun und wäre derselbe ungewollte Eingriff.
- **Texte** de/en angepasst (die fünf oben); **Tests:** `NutzungsdauerS3Tests` angepasst (die Tabelle rechnet nicht
  selbst; Vorbelegen und Speichern rechnet; die Pflichtanlage schreibt keinen Satz; Herkunft nur bei Gleichheit),
  `BetriebskostenBasisTests` wieder auf dem Stand vor E10.

**E10‑Q1 neu gefasst:** (a) gebaut — die Sätze wirken nur, wenn sie ausdrücklich in die Position geschrieben werden;
(b) zuerst gebaut, verworfen — der implizite Rückfall zur Rechenzeit (1030 ohne Zutun +37.200 €/a, Kapitalwert
−630.612 €, davon 17.700 €/a doppelt). Im Phase‑1-Bericht standen die Lesarten umgekehrt (a = Rechenwirkung,
b = nur Vorbelegung).

## Saat-Tafel (Schritt 120)

Gesät ist die Mitte des Empfehlungsbereichs der Betriebsvorlagen (`SchemaKatalog.Schritt39_Vorlagen`), nur an den
Standardzeilen, nur Instandsetzung (E10‑Q7, Lesart a):

| Technik (Standardzeile) | Instandsetzung | Quelle: Vorlage, Position, Bereich |
|---|---|---|
| Heizkessel · Wärmeerzeuger | 2,0 % | Heizkessel, „Instandhaltung Heizkessel", 1,5–2,5 |
| BHKW · Modul | 6,0 % | BHKW, „Instandhaltung BHKW", 3–9 |
| Wärmezentrale · Rohrleitungen | 2,0 % | Wärmezentrale, „Instandhaltung Wärmezentrale", 1,8–2,2 |
| Stromeinspeisung · Netzanschluss | 2,0 % | Stromeinspeisung, „Instandhaltung Stromeinspeisung", 1,8–2,2 |
| Bauliche Anlagen | 1,25 % | Bauliche Anlagen, „Instandhaltung bauliche Anlagen", 1,0–1,5 |

Wärmepumpe, Photovoltaik, Solarthermie, Stromspeicher und Pufferspeicher bleiben leer — ihre Vorlagen führen keinen
Bereich; Wartung bleibt überall leer — keine Vorlage führt sie in % der Investition. Normwerte werden nicht erfunden
(ND‑Q8).

## Fragen aus der Welle

Der Phase‑1-Bericht stellt sechs Fragen und eine siebte, die der Bau erzwingt; E10‑Q1 ist nach dem Umbau neu gefasst,
E10‑Q5 hat sich erledigt. **Sechs sind beim Anwender offen**; gebaut ist jeweils Lesart a. Sie stehen im
Entscheidungsregister als **R‑E10**.

| Frage | Lesarten | Empfehlung | Stand |
|---|---|---|---|
| **E10‑Q1** Umfang S3 | (a) die Sätze der Tabelle wirken nur über die ausdrückliche Vorbelegung — die vom Anwender ausgelöste Vorlagenübernahme oder „Sätze vorbelegen…" mit „Speichern"; der Rechenweg liest den gepflegten Satz; ohne Zutun ändert sich keine gerechnete Wirtschaftlichkeit (ND‑Q4); (b) leere Positionen rechnen zur Rechenzeit mit dem Tabellensatz — 1030 ohne Zutun +37.200 €/a, Kapitalwert −630.612 €, davon 17.700 €/a doppelt | a | offen; gebaut ist a (E10/9) |
| **E10‑Q2** Gerätekataloge | (a) die Katalogwerte bleiben, nur neue Einträge werden vorbelegt (Kessel in %/a; das BHKW nicht); (b) die Tabelle überschreibt die Katalogwerte | a | offen; gebaut ist a |
| **E10‑Q3** Restwert der Speicherflotte | (a) linear aus der Nutzungsdauer (Ersatzintervall), der feste Restwert der Einheit ist Altfeld; (b) der feste Restwert behält Vorrang, linear nur als Vorgabe — 1046: a +3.432,79 € im Kapitalwert der Studie, b ±0 | a | offen; gebaut ist a |
| **E10‑Q4** A8 | (a) nur kennzeichnen; (b) Schemaschritt (H) entfernt die Spalten | a | offen; gebaut ist a |
| **E10‑Q5** Basis | (a) eine neue Basis R14 nach der ganzen Welle; (b) je Teil eine | a | **erledigt** — der Referenzlauf ist byte-gleich zu R13, eine R14 entfällt; Nachtrag in `Referenzlaeufe/LIESMICH.md` |
| **E10‑Q6** Asymmetrie Wartung BHKW/Kessel (Konzept § 6.3 Nr. 19) | (a) dokumentieren; (b) die Kessel-Einheit auch für das BHKW | a | offen; gebaut ist a |
| **E10‑Q7** Saatwert aus dem Empfehlungsbereich (vom Bau erzwungen) | (a) Mitte; (b) Untergrenze; (c) Obergrenze; (d) keine Saat, nur die Spanne zeigen | a | offen; gebaut ist a (Schritt 120) |

**Mit dieser Welle erledigt** (unter dem Vorbehalt der Entscheide und der Abnahme A‑E10‑1): A7 (R‑A) — die Flotte hängt
an der Tabelle (Intervall-Vorgabe, linearer Restwert); das Neueinfrieren entfällt, weil die Basis keine
Flottenwirtschaftlichkeit führt, die dritte Einfrierregel ist ergänzt; A8 (R‑A) — gekennzeichnet, der Halbsatz zur
Speichervariante bleibt offen; ND‑Q6 (R‑ND) — die Satzspalten sind sichtbar, gesät und in der Vorbelegung gelesen;
ND‑Q7 (R‑ND) — (b) geprüft: nur neue Kesseleinträge in %/a werden vorbelegt; Konzept § 6.3 Nr. 9h — beide Stücke; Mockup
U39 — Stück 1 für BHKW und Kessel (die Speichervariante bleibt, weil sie rechnet), Stück 2 ganz; § 6.3 Nr. 19 —
dokumentiert.

## A/B-Nachweis

**Teil A — „Sätze vorbelegen…" auf einer Arbeitskopie** (Harness des Bauagenten). Ohne Knopf ist alles unverändert:
1030 rechnet −31.141.242,71 €, den für #440 dokumentierten frischen Wert.

| Projekt | vorbelegte Zeilen | Betriebskosten p. a. ohne → mit Knopf | Kapitalwert Erwartet ohne → mit Knopf (Δ) | Δ Günstig / Ungünstig |
|---|---|---|---|---|
| 1018 | 4 — BHKW 6 %, Wärmezentrale 2 %, bauliche Anlagen 1,25 %, Stromeinspeisung 2 % auf 45.312,50 € | 0,00 → 5.097,66 € | nicht rechenbar* | — |
| 1030 | 3 — Kessel 2 % auf 90.000 €, BHKW 14920 und 14921 je 6 % auf 295.000 € | 20.000,00 → 57.200,00 € | −31.141.242,71 → −31.771.854,72 € (−630.612,02 €) | −572.367,27 / −687.883,88 € |
| 1026 | 1 — Kessel 2 % auf 6.775,50 € | 0,00 → 135,51 € | nicht rechenbar* | — |

\* 1018 und 1026 fehlen vorher wie nachher Energieträger-Zuordnungen, deshalb kein Kapitalwert. 1019 bleibt unverändert.

**Datenbefunde (nicht behoben, für den Anwender):** **1030 zählt nach dem Vorbelegen doppelt** — die Instandhaltung der
Anlage 14921 fällt mangels eigener Investition nach der Stufung H4a auf die Summe der ganzen BHKW-Komponente
(295.000 €) zurück, die Instandhaltung des BHKW steht damit zweimal da, und die Sammelposten „BHKW" 18.000 €/a und
„Heizkessel" 2.000 €/a stehen neben den Einzelpositionen; **1026 rechnet auf fremder Basis** — der Kessel trägt die
Investition 0, seine Instandhaltung fällt auf die Projektsumme aus Solarthermie und Puffer (6.775,50 €) zurück. Beides
ist Datenlage der Testdatenbank, nicht die Regel des Rechenwegs; vor „Sätze vorbelegen…" gehören die Investition je
Anlage und die Sammelposten bereinigt.

**Teil B — Speicherflotte 1046** (Studie mit den EPOS-Reihen des Projekts, 20 Jahre, 3 %, Investition 22.150 €, Ersatz
in Jahr 10 und Jahr 20 je 7.000 €):

| Größe | vorher (fester Restwert, zugleich Lesart b) | nachher (linear, E10‑Q3 a) | Δ |
|---|---|---|---|
| Restwert nominal | 800,00 € | 7.000,00 € | +6.200,00 € |
| Kapitalwert | −38.487,24 € | −35.054,45 € | +3.432,79 € |

Mit Zahlungsstrom 0 (die Vorab-Zahl des Phase‑1-Berichts) dieselbe Differenz: −30.791,45 → −27.358,66 €. **Anker:**
`WirtschaftlichkeitAnkerTests` unverändert grün, kein Anker neu gesetzt.

## Referenzlauf und Basis

`vergleich` gegen `2026-09-23_R13_Kuehlung`: **13/13 PASS**, 4.145.687 Werte, 387 von 387 CSV byte-gleich (nur
`protokoll.txt` weicht ab, wie immer). Die Basis führt keine Wirtschaftlichkeitsgrößen, ein Satz der Tabelle rechnet
erst nach der Vorbelegung, und der Projektlauf rechnet keine Flottenwirtschaftlichkeit — `aggregate.csv` von 1046 trägt
nur die Physik der Flotte. **Keine R14** (E10‑Q5 erledigt); der Gate-Pfad bleibt `Referenzlaeufe/2026-09-23_R13_Kuehlung`.
**E10/10 (`b8cb7287`)** in `Referenzlaeufe/LIESMICH.md`: der Kopf der Basis „Nachträge 114 bis 120"; der fehlende
Nachtrag zu den Schritten 116–118 (E9a, 18 leere Spalten, LFS `e9748b7f…`); der Nachtrag „E10 (#463)" — Schritt 120 mit
den fünf Sätzen und LFS `52c4729d…`, ergebnisneutral, der Flottenanschluss ohne Wirkung auf die Referenz, 13/13
byte-gleich, daher keine R14; die dritte Einfrierregel ist um den Absatz „Anschluss an die Nutzungsdauertabelle"
ergänzt — zur Regel gehören jetzt Ersatzintervall und Ersatzkosten der Einheiten von `@Projektflotte` und, sobald eine
Einheit ohne eigenes Intervall rechnet, die Nutzungsdauer der Zeile „Stromspeicher · Batterie"; den Nachweis führt
`SpeicherFlottenNutzungsdauerTests`.

## Phase 2 und der Merge

- **E10/9 (`2c41c642`)** — der Umbau oben.
- **Merge `f9eb2615`** holt `origin` = `502fea3c` (Dialog Design #466: Import-Reste nach Stufe 5); die Ressourcen liefen
  ohne Konflikt zusammen, der Designer blieb gleich und ist wiederholbar erzeugbar; `origin` stand weiter bei
  Zielversion 119, Schritt 120 bleibt bei E10.
- **E10/10 (`b8cb7287`)** — der LIESMICH-Nachtrag oben.
- **Tests auf `b8cb7287`:** Kern-Filter und Windows-Schale 0 Fehler; gefiltert Kern 229, UI 604 und SpeicherEngine 386
  Fälle grün; voller Lauf `WP-Plan.Kern.slnf` **12.580 bestanden, 0 Fehler, 1 übersprungen** (EPOS.Kern.Tests 5.733,
  EPOS.UI.Tests 5.892, KiKern.Tests 542, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27 und 1 übersprungen);
  Maskenwache und die Dokumentations- und Ordnungswachen grün (33 Fälle); SQL-Prüfer 1.766 Texte, 0 Fundstellen;
  Designer wiederholbar (zweiter Lauf +0); vor jedem `dotnet test` die `testhost`-Probe (einmal lief ein fremder
  Prozess, nach 60 s war er beendet).
- **Merge `94521f2e`** über `origin` = `502fea3c`: 61 Dateien, +3.336/−87; der Baum gleicht `b8cb7287`.

## Abweichungen und Befunde

1. **Zuordnung über den Positionsnamen:** Eine Kostenart „Instandsetzung/Wartung" gibt es nicht; zugeordnet wird über
   den Positionsschlüssel (`DbWerte.VDI_POS_*`). Eine technikfremde Position wie „Instandhaltung Wärmezentrale" in der
   BHKW-Vorlage nimmt den Satz der Technik aus ihrem Namen.
2. **Ein Knopf, zwei Seiten:** Auf der Betriebsseite heißt derselbe Rasterknopf „Sätze vorbelegen…"; „Nutzungsdauern
   vorbelegen…" auf der Investitionsseite nennt die Sätze nicht mit.
3. **I‑2 am Knopf:** Eine Position mit erfasstem Betrag, aber ohne Satz zählt als belegt — sie bekommt den Satz erst
   nach der Rückfrage; ein Betrag bleibt ein Betrag.
4. **Speicherflotte:** Der zusätzliche Restwert der Studie rechnet weiter, nur das Feld je Einheit ist Altfeld; das
   Tabellenintervall gilt nur im Studienlauf (der Projektlauf rechnet keine Flottenwirtschaftlichkeit); ein übernommener
   Stand speichert das eingesetzte Intervall als Zahl; Intervall 0 heißt nicht mehr „kein Ersatz", sondern
   „Nutzungsdauer der Tabelle"; Einheiten ohne eigene Kosten: Ersatz kostet 0, einen Restwert gibt es nur bei einer
   Laufzeit unter der Nutzungsdauer; die Flotte ersetzt auch im letzten Jahr, der Restwert steht dann in voller Höhe
   daneben — netto wie bei den Positionen, die im Jahr T nicht mehr ersetzt werden.
5. **A8:** Die KI-Feldkarte trägt den Vermerk, bleibt aber setzbar wie der Katalog; der Tooltip läuft über den neuen
   Parameter `Titel`; der Halbsatz „Speichervariante liest 20/21" ist nicht gebaut.
6. **Pflichtanlage ohne Satz (E10/9):** Die automatische Pflichtanlage des Wizards schreibt keinen Satz.
7. **Schemaschritt 120** statt keines (Begründung oben) und **keine R14** statt der geplanten (Basis byte-gleich).

**Befund zur Intervall-Vorgabe der Flotte:** Anders als die Sätze wirkt sie zur Rechenzeit — so verlangt es der Auftrag
(Punkt 6, A7): Eine Einheit mit Intervall 0 rechnete ohne Ersatz und Restwert, jetzt mit 10 a und linearem Restwert.
Das betrifft die Flottenstudie, nicht die Projektwirtschaftlichkeit; 1046 trägt eigene Intervalle. Der Vorschlag der
Messung #452 (Konzept § 6.3 Nr. 9h: „für Speichervariante und Flottenintervall die Tabelle nur als Vorgabe neuer
Einträge") ist damit für das Flottenintervall nicht gebaut — mitzuentscheiden mit E10‑Q3.

## Zahlen und Abnahme

- **Phase 1** (Worktree `e10`): Kern-Filter und Windows-Schale 0 Fehler, SQL-Prüfer 1.767 Texte, 0 Fundstellen; Tests
  geschrieben und gebaut, kein `dotnet test`, kein Referenzlauf.
- **Phase 2** (auf `b8cb7287`): siehe „Phase 2 und der Merge".
- **Gate auf `94521f2e`** (Worktree `pm10`, Log `GATE463.log`): Kern-Filter 0 Fehler; ChartProben 146 Bilder, alle grün,
  Hashes gleich der Windows-Messlatte (146/146; E10 bringt kein Bild); voller Lauf **12.580 bestanden / 0 Fehler /
  1 übersprungen** (EPOS.Kern.Tests 5.733, EPOS.UI.Tests 5.892, KiKern.Tests 542, SpeicherEngine.Tests 386,
  SpeicherPlanung.Tests 27 und 1 übersprungen); Dokumentationswachen 26/26. Referenzlauf 13/13 gegen R13 byte-gleich und
  SQL-Prüfer 1.766/0 aus dem Agentenlauf auf dem byte-gleichen Baum `b8cb7287`.
- **Schema:** Zielversion 120; Testdatenbank LFS `52c4729d…` (67.784.704 Byte, Schemastand 120; vorher `6259b348…`,
  Stand 119); der nächste freie Schritt ist 121 (vorgesehen für die Zapfprofil-Stufe Z4).
- **Maskenwache:** `NutzungsdauerDialog` 8 → 12, die Summe über 72 Masken 539 → 543.

## Abnahme am Gerät (A‑E10‑1, Windows und iPad)

1. **Administration › Kosten › Nutzungsdauern (AfA)…:** die Spalten „Instandsetzung [%/a]" und „Wartung [%/a]" in
   Tabelle und Neuzeile, darüber die leise Zeile mit VDI 2067 Blatt 1, Tabelle A2; an den Standardzeilen Heizkessel 2,
   BHKW 6, Wärmezentrale 2, Stromeinspeisung 2 und Bauliche Anlagen 1,25, Wartung leer; „Auslieferungswerte
   wiederherstellen" setzt die Sätze mit zurück.
2. **Kostenverwaltung › Betriebskosten auf einer Projektkopie** (etwa 1030): Der vierte Rasterknopf heißt „Sätze
   vorbelegen…"; die Statuszeile nennt die Zahl, bei belegten Positionen kommt die Rückfrage mit ihrer Anzahl;
   geschrieben wird erst mit „Speichern".
3. **Herkunftszeile:** unter dem Satzfeld „6 % · Satz aus Nutzungsdauertabelle: BHKW · Modul (Instandsetzung)" — nur bei
   einem Satz gleich dem der Tabelle; ein eigener Satz trägt keine Zeile.
4. **Kapitalwert vorher/nachher:** Die Kopie von 1030 rechnet ohne Knopf −31.141.242,71 €, nach „Sätze vorbelegen…" und
   „Speichern" −31.771.854,72 €; Wort- und Tabellenbericht nennen in der Herleitung „· Satz aus Nutzungsdauertabelle";
   das Original bleibt unverändert.
5. **Vorlagenübernahme:** „Aus Vorlage übernehmen…" legt eine Position „Instandhaltung …" mit dem Tabellensatz an; eine
   im Wizard neu angelegte Anlage trägt ihre Pflichtpositionen ohne Satz.
6. **Flotten-Editor (Stromspeicher-Auslegung):** unter „Ersatz und Restwert" die Zeile zur Ersatzintervall-Vorgabe
   („Ersatzintervall 0: Es gilt die Nutzungsdauer der Nutzungsdauertabelle für Stromspeicher (10 Jahre) …"), die
   Beschriftung „Restwert der Einheit (Gerätedaten):" mit der Altfeld-Zeile; die Studie von 1046 setzt den Restwert
   linear an (nominal 7.000 € statt 800 €).
7. **Kataloge Heizkessel und BHKW** (auch „Alle Daten anzeigen"): „Nutzungsdauer (Gerätedaten):" mit dem Tooltip; ein
   neuer Kesseleintrag mit der Wartungseinheit „%/a" übernimmt einen in der Tabelle gepflegten Wartungssatz.

## Logbuch

Mit #463 stehen im Update-Papier (Version 1.2.0.4, Sammel-Upload 26.09.2026) vier Sätze, zwei je Stichwort. Der Satz zu
den Betriebskostenpositionen ist nach dem Umbau angepasst („bekommen den Satz … über ‚Sätze vorbelegen…'" statt „rechnen
mit dem Satz der Nutzungsdauertabelle"), der Satz zu den Katalogen sagt „maßgeblich ist" statt „gerechnet wird mit" —
wie der Tooltip, denn auch die Nutzungsdauer rechnet aus der Position, die Tabelle belegt sie vor:

- *nutzungsdauern:* „Der Dialog Nutzungsdauern (AfA) zeigt je Technik die Sätze für Instandsetzung und Wartung in % der
  Investition je Jahr (VDI 2067 Blatt 1, Tabelle A2)."
- *kosten:* „Betriebskostenpositionen ‚Instandhaltung …' und ‚Wartung …' mit ‚% der Investition' bekommen den Satz der
  Nutzungsdauertabelle über ‚Sätze vorbelegen…' oder die Übernahme einer Kostenvorlage; die Herkunft steht am Satz."
- *kosten:* „Die Speicherflotte rechnet den Restwert je Einheit linear aus ihrer Nutzungsdauer; ohne eigenes
  Ersatzintervall gilt die Nutzungsdauer der Nutzungsdauertabelle."
- *nutzungsdauern:* „Die Nutzungsdauer in den Katalogen von Heizkessel und BHKW ist als Gerätedaten gekennzeichnet;
  maßgeblich ist die Nutzungsdauertabelle."

## Befunde nebenbei

- **CI-Nachweise zu #462** (Push `48d8836d`): Kern-Lauf `ios_migration_september` 35956782471, Kern-Lauf `main`
  35956786642 und Windows-Lauf `main` 35956786551 — alle grün; nachgetragen in Nach #462 (h). Der CI-Nachweis zu #463
  steht aus — der Push folgt nach dem Gate.
- **Nutzungsdauer-Konzept:** Der Kopf nannte Codestand `41764ab0` und Zielversion 113 — mit den Papieren zu #463 auf
  `94521f2e` und 120 nachgezogen, § 3 und § 6 mit „S3 umgesetzt #463". **Nach `ueberholt/` wandert es noch nicht**
  (Nach #264: „sobald S3 abgeschlossen ist"): S3 ist gebaut, aber seine Fragen — darunter der Umfang von S3 selbst
  (E10‑Q1) und der Saatwert (E10‑Q7) — sind beim Anwender offen, und der Halbsatz aus A8 zur Speichervariante steht aus;
  die Verschiebung gehört zu den Entscheid-Papieren zu E10.
- **`Referenzlaeufe/LIESMICH.md`:** die Nachträge 116 bis 120 und die Einfrierregel sind mit E10/10 erledigt (oben).
- **Wiki-Quelle Kosten:** Die ±-Knöpfe der Trägerkarte (aus #462) fehlten im Punkt `traegerkarte`, der
  Nutzungsdauern-Dialog kannte die Sätze nicht — mit den Papieren zu #463 nachgezogen, ebenso die Kennzeichnung in der
  Quelle Gerätekataloge (Tabuwort-Regex 0 Treffer).
- **Analysepapier § 6:** Der Schritt (H) — geräteeigene Nutzungsdauer entfernen — bleibt optional und ungebaut (A8,
  E10‑Q4); 120 ist die Nachsaat der Sätze aus E10 (ohne Buchstaben), 121 ist für die Zapfprofil-Stufe Z4 vorgesehen.
- **Befund P5 des Analysepapiers:** der Rest (`Dialogkopf`, `SpeichernLeiste` mit Aktionsknöpfen, Spaltenfilter)
  unverändert offen; E10 berührt ihn nicht.
- **Einfrierregel in `CLAUDE.md`:** Die Kurzfassung nennt den Flottenstand `@Projektflotte` des Projekts 1046 samt
  Projektzeilen; die Ergänzung um die Nutzungsdauer der Zeile „Stromspeicher · Batterie" steht nur in
  `Referenzlaeufe/LIESMICH.md`, wo die Regel maßgeblich ist — ein Nachtrag in der Kurzfassung wäre eine Zeile.

## Offen

- **Die sechs Fragen** E10‑Q1 bis E10‑Q4, E10‑Q6 und E10‑Q7 beim Anwender (Empfehlung jeweils a, gebaut jeweils a);
  offen außerdem E7c3‑Q1 bis E7c3‑Q8, E8c‑Q1 und E8c‑Q2, E9a‑Q1 bis E9a‑Q7 und E9b‑Q1 bis E9b‑Q5.
- **Abnahme am Gerät** A‑E10‑1 (sieben Schritte oben).
- **Datenpflege 1030 und 1026**, bevor dort Sätze vorbelegt werden (Datenbefunde oben).
- **Halbsatz aus A8** — die Speichervariante sollte die Positionsarten 20/21 lesen; ein eigener Auftrag, falls gewünscht.
- **Nächste Etappe: E12** (Wiki-Runden, Sonnet; Sammel-Upload 28.09.2026) — E11 entfällt; damit ist der Etappenplan
  E0–E12 des Analysepapiers bis auf E12 abgearbeitet.
- **Push** nach der Regel des Anwenders ohne Rückfrage aus dem Hilfszweig des Merges (`94521f2e` und die Papiere) auf
  `ios_migration_september` und `main`; der CI-Nachweis kommt mit der nächsten Papierwelle.
- **Papiere mit der Statuszeile:** Register (Kopf, Familientafel, A7, A8, R‑ND mit ND‑Q4, ND‑Q6 und ND‑Q7, Nr. 20, neue
  Familie R‑E10, EZ‑9, EZ‑10), Konzept (Kopf, § 2.1, § 2.8, § 2.13 (3), § 3.4, § 6.1, § 6.2, § 6.3 Nr. 9h und 19, § 6.5,
  § 7 und Anhang), Nutzungsdauer-Konzept (Kopf, § 1.5, § 1.6, § 2.2, § 2.4, § 2.5, § 3 samt „Offen aus S2", § 5, § 6)
  und seine Indexzeile, Protokoll der Entscheidwege (Kopf, § 0.5, Kopf von § 8, § 8.23, § 8.24), Analysepapier (Kopf,
  Nachtrag, § 0 Punkt 5, § 2.4, § 3.4 D4, § 4 A7/A8, § 5 mit der Zeile E10, dem Stand der Etappen und der Begründung,
  § 6), Mockup (Kategorien 1 bis 3 — in Kategorie 3 heißt der vierte Knopf der Betriebsseite jetzt „Sätze vorbelegen…" —,
  Ressourcentafeln der Kategorien 1 und 2, Anhang U39, Stand-Absatz), die Logbuch-Sätze, die Wiki-Quellen Kosten und
  Gerätekataloge und der Index Reporting.

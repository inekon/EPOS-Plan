# E16 — Bericht Phase 1 (Opus, 24.09.2026, Worktree e16 ab 87dd9fd4)

Commits (29 Dateien, +1878/−22): 68a4915a E16/1 Schemaschritt 128; 7572c0cf E16/2 Kern; b8ff3931 E16/3 Dialog; c5ac83f7 E16/4 Ausweis
(Formelmappe); 43776f61 E16/5 Tests. Builds 0 Fehler (Kern-Filter, WindowsFormsApplication1 x64, Werkzeug, EPOS.Referenzlauf); kein
`dotnet test` in Phase 1.

Schemaschritt 128 (origin bei 3c2f1752 auf Zielversion 127): Zahl nur in `WiederholperiodeSchema.SCHRITT` (`EPOS.Kern/Allgemein/Update/
WiederholperiodeSchema.cs`); `SchemaStand.Zielversion`, `SchemaMigration.SCHRITT_WIEDERHOLPERIODE` (Eintrag nach 127, Methode
`Schritt_Wiederholperiode`), Werkzeug `Werkzeuge/Testdatenbankschema/Program.cs` und `EPOS.Kern.Tests/TestDatenbank.cs` leiten sich ab;
außerdem im Betreff/Rumpf von E16/1 und nach Phase 2 als `SchemaVersion` der Testdatenbank. Umnummerierung auf 129: eine Konstante,
Commit-Nachricht, Werkzeug erneut ziehen. Spalte `Wiederholperiode_a` (INTEGER, nullbar, keine Vorgabe) an `Tab_ProjektWerte` und
`Tab_KostenVorlagePosition`; leer/0/1 = jährlich.

Schlüssel (8, de/en; Designer 9.998 → 10.006, wiederholbar): `WIRT_BK_ALLE_N_JAHRE` („alle {0} Jahre ab Jahr {1}"), `WDH_LBL_PERIODE`,
`WDH_EINHEIT`, `WDH_INFO`, `KI_DLG_VOP_WDH_NAME`, `KI_DLG_VOP_WDH_ERL`, `WIRT_FM_MJ_WDH_PB`, `WIRT_FM_MJ_WDH_PE`.

Kern: `KapitalwertRechner` Klasse `Wiederholposten`, Regel `ZahltImJahr`, `Rechne(... wiederholt)` optional (ohne Liste alter Weg),
`Zahlungsbild.Wiederholt` Ausweis; `WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe` legt n ≥ 2 in `BetriebsTopfe.Wiederholt` statt in die
Summen, neu `WiederholtErstesJahr`/`ErstesJahr`, `Gesamt`/`ProjektEingabe.Wiederholt`/`BaueEingabe`/Ohne-KWKG-Kopie führen die Liste,
`RechneBild` (Sensitivität) skaliert p_E-Anteil mit Energiefaktor, investitionsgekoppelten Anteil additiv, `BetriebskostenJahr` zählt
Startjahr ≤ 1, `LiesBetriebskostenPositionen` trägt die Periode; `KostenPositionNachweis.Wiederholperiode`, Nachweisumschlag Fassung
10 → 11; `WirtschaftlichkeitZeilen.HerleitungZeile` „alle n Jahre ab Jahr X"; neuer Controller `EPOS.Kern/Controller/Wiederholperiode.cs`.
Dialog: `VorlagenPositionDialog` Ganzzahlfeld „Zahlung alle: [n] Jahre" (1…99) mit Herleitung, nur Betriebsseite und nur mit Spalte;
`VorlagenPositionErgebnis`/`VorlagenPositionKiSicht`; `KostenKomponenteHuelle.EditorGaben/EditorFertig`; mitgeführt in `KostenVorlagenCtrl`,
`KostenVorlagenUebernahmeCtrl` (Übernahme, Projektkopie), `KostenProjektPositionenCtrl` (Kategorie 2); KI-Feld `wiederholperiode`
(`KiDialoge`/`KiDialogTexte`). Ausweis: `ExcelFormelmappe.Mehrjahrestabelle` je Topf Hilfsspalte `IF(AND(Jahr>=s,MOD(Jahr-s,n)=0),Betrag,0)`,
Basisspalte jährlicher Rest, Betriebszelle `(Basis+Wiederholt)*(1+p)^(Jahr-1)`, gegengerechnet, ohne Periode keine Spalte.

Prüfungen: Werkzeug-Probelauf auf Scratchpad-Kopie (2 Spalten, Stand 128), SQL-Prüfer 1.823/0; Tests `WiederholperiodeTests` (17 Fakten/
Theorien, 16 InlineData), 5 bUnit in `VorlagenPositionDialogTests`; angepasst `ErgebnisansichtTests` (Fassung 11), Maskenwache 8 → 9,
KI-Dialogkatalog „acht" → „neun"; Wache „Testdatenbank auf dem Stand" rot bis Phase 2.

Fragen (gebaut a): E16‑Q1 Zahlungsjahre s, s+n, … ≤ T (`ZahltImJahr`); E16‑Q2 nur Betriebspositionen (Investition = Ersatzkette); E16‑Q3
Betriebskosten p. a. = Jahr-1-Zahl (Periode mit Startjahr ≤ 1 zählt, später nicht; Gliederungsprobe geht auf). **E16‑Q4 neu (Abweichung 8):**
`SpeicherAuslegungCtrl.Modulkosten` liest eine Betriebsposition mit Periode weiter als jährlich — a so lassen (Randfall), b Periode dort
mitrechnen.

Abweichungen: (1) Perioden-Positionen als eigene Liste, nicht als Feld der (Betrag, Startjahr)-Paare — bestehende Ausdrücke unverändert;
(2) Beschriftung „Zahlung alle:" + Einheit „Jahre"; (3) Obergrenze 99; (4) Vorlageneditor-Herleitung auf Jahr 1 (Vorlage ohne Startjahr);
(5) Umschlag 10 → 11; (6) Stufe 3 bleibt Menge × Satz je Zahlung, Periode dort in der Herleitungsspalte; (7) `LiesBetriebskosten(out abJahr)`
(alte Sicht vor FX3) kennt Perioden nicht (Kommentar); (8) siehe Q4; (9) Hinweis „Startjahre gesetzt" auch bei Perioden-Position mit
Startjahr ≥ 2.

Erledigt-Gründe: Register V‑G3 „erledigt mit E16: Wiederholperiode je Betriebsposition (`Wiederholperiode_a`, Schemaschritt 128),
Zahlungen in s, s+n, … ≤ T nach DIN EN 17463 6.3.1; Zeileneditor, Vorlagenübernahme, Kapitalwert, Berichte, Formelmappe; ergebnisneutral
ohne Pflege"; Konzept § 2.11.2 V‑G3 teilweise → umgesetzt (alle vier Zeitpunktarten: Periode 0, jährlich, alle n Jahre, einmalig in k);
§ 2.13 (3) Ergänzung Startjahr = Beginn der Folge, Nutzungsdauer/Ersatz/Restwert unberührt. Logbuchsatz (`kosten`): „Betriebskosten-
positionen lassen sich im Zeileneditor mit ‚Zahlung alle n Jahre' führen (z. B. Dichtheitsprüfung alle 2 Jahre); die Wirtschaftlichkeit
rechnet sie nur in ihren Zahlungsjahren." Abnahme A‑E16‑1: (1) Stift einer Wartungsposition, „Zahlung alle: 2 Jahre", Herleitung „alle 2
Jahre ab Jahr X"; (2) Kapitalwert steigt um den Barwert der entfallenen Jahre, p. a. bleibt Jahr 1; (3) Berichte „alle 2 Jahre ab Jahr 1",
keine Gliederungswarnung, Formelmappe Hilfsspalte MOD; (4) zurück auf 1 = alter Kapitalwert.

Phase 2 (geplant): Nachzug origin 3c2f1752, Testdatenbank 127 → 128 (E16/6, LFS), gefilterte Tests, voller Lauf, A/B 1030 (Zeile
101600098 Wartung Kessel 2.000 €) auf Arbeitskopie, Referenzlauf 13/13 R14.

# E16 — Bericht Phase 2 (Opus, 24.09.2026 abends, e16 = 7c8f4fc4; Merge #484 = ae7b0ed0 auf pm18 ab c778ab12)

Commits: cb06a7c3 Merge origin 3c2f1752 (ohne Konflikt); de49dec9 E16/6 Testdatenbank 127 → 128 (LFS f4a6ee8b, überholt); 6ddb0132 Merge
origin c778ab12 (enthält AK1 W3 01408e8b, Schritt 128 Heizkreis `ErgebnisGebaeudeSchema.SCHRITT_HEIZKREIS`; Konflikte SchemaStand.cs,
SchemaMigration.cs [drei Blöcke], Werkzeuge/Testdatenbankschema/Program.cs, EPOS.Kern.Tests/TestDatenbank.cs, Testdatenbank → Fassung von
origin 81209c50; überall 128 vor Wiederholperiode); 37538993 E16/7 `WiederholperiodeSchema.SCHRITT` 128 → 129 (einzige Stelle; Kommentare
„nach 128 (Heizkreis)"; Werkzeug-Wache prüft 128 vor 129 und SCHRITT > SCHRITT_HEIZKREIS); 7c8f4fc4 E16/8 Testdatenbank 128 → 129 vom
Werkzeug aus der origin-Fassung. Die Zahl 129: nur `WiederholperiodeSchema.SCHRITT`, Betreff/Rumpf E16/7 und E16/8, `SchemaVersion` der
Testdatenbank; Betreff E16/1 nennt noch 128 (Geschichte).

Testdatenbank Endstand: LFS-SHA-256 4c546a7c05137b9e45549d3d0dd171150f6f74734327bafaf61ad5405ff8345a, 67.796.992 Byte (Größe unverändert),
Schemastand 129, 133 Tabellen (132 STRICT + sqlite_sequence), 210 Indizes, 14 Sichten, integrity_check ok, foreign_key_check leer; Werkzeug
2 Spalten, 0 Tabellen, Schritt 128 fand nichts offen; keine Zeile pflegt `Wiederholperiode_a`.

Tests Endstand: gefiltert Kern 240/240 (Wiederholperiode, Ergebnisansicht, Gliederung, Risiko, Wirkungen, Katalogreparatur, Heizkreis,
Migrationsketten, Erstbereitstellung, Formelmappe, Blattstruktur), UI 239/239 (Zeileneditor, Maskenwache, KI-Dialogkatalog); voller Lauf
EPOS.Kern.Tests 6.111 / 0 Fehler, EPOS.UI.Tests 6.017 / 0, KiKern.Tests 549, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27 + 1
übersprungen; Auslieferungsvorlage.Tests 30/30. Zwischenstand auf 128 ebenfalls grün (Kern 196/196, WiederholperiodeTests 31/31, UI 239/239;
voll Kern 6.107, UI 6.017, 549, 386, 27+1, Auslieferungsvorlage 30/30). Testhost-Regel eingehalten (zweimal gewartet).

A/B-Nachweis 1030 (Arbeitskopie, Zeile 101600098 Wartung Kessel 2.000 €/a auf n = 2; Handrechnung Σ 2000·(1+p_B)^(t−1)/(1+i)^t, t = 2, 4, … 20):
| Szenario | T | i | p_B | KW vorher | KW nachher | Δ Lauf | Handrechnung |
| Erwartet | 20 | 3 % | 1,5 % | −21.895.377,28 | −21.878.549,68 | +16.827,59 | 16.827,59 |
| Günstig | 20 | 2 % | 0,5 % | −21.981.462,76 | −21.964.493,60 | +16.969,16 | 16.969,16 |
| Ungünstig | 20 | 4 % | 2,5 % | −21.840.407,19 | −21.823.718,84 | +16.688,35 | 16.688,35 |
Abweichung Lauf/Handrechnung < 1e‑9 €; „vorher Erwartet" = gepinnter Anker −21.895.377,28; Betriebskosten p. a. bleiben 20.000 €/a (Q3 a);
Prüfprogramm war eine vorübergehende Testklasse, gelöscht, nicht committet.

Referenzlauf 13/13 gegen 2026-09-24_R14_Kaelteerzeuger PASS, 4.207.049 Werte, 394/394 CSV byte-gleich. SQL-Prüfer 1.824 Texte / 0
Fundstellen (332 dynamisch, 1.492 in Ordnung). Designer 10.006, +0.

Offen: E16‑Q4 (Modulkosten der Speicherauslegung, a gebaut); Hinweis „Startjahre gesetzt" auch bei Perioden-Position mit Startjahr ≥ 2
(Befund, bleibt); `LiesBetriebskosten(out abJahr)` alte Sicht (Kommentar). Nicht angefasst: LIESMICH-Nachtrag 128 (AK1 W3) und 129 (E16),
Statuszeile. Schrittnummer in Phase 1 = 128 ist überall als 129 zu lesen.

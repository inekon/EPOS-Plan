# E9a — Vollständige Szenarioabdeckung, Teil a: Schemaschritte 116 bis 118, der Kern liest die Paare (Protokoll, 24.09.2026)

Statuszeile #461 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Etappe E9 (Teil a — V‑E) des
Analysepapiers
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
(§ 5 Zeile E9, § 6 Schritte B, C und D); Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 2.11.4 (V‑E), § 2.11.5 (Tafel und Regeln) und § 2.11.7 (Hinweistext); Szenarienkonzept
[`Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md`](../../../aktuell/Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md)
§ 2, § 4 und § 11.1; Mockup `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`, Kategorie 8 (Zone „Was ist
angenommen?", Ressourcentafel) und Anhangzeilen U15 und U27. Entscheide: V‑G5 (R‑V, 31.08.2026: vollständige
Abdeckung), V‑4 (R‑V, 18.09.2026: nach der Darstellungsetappe, bis dahin mit Hinweistext) und A5 (R‑A, 20.09.2026: ohne
Degradation) im
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md),
die sieben Fragen dieser Welle unter R‑E9a. Anlass: der Anwender, „Fahre fort" (24.09.2026) — Etappe E9 in zwei
Wellen: E9a (dieser Teil: Schema, Kern, Tests, A/B-Nachweis — ohne Dialoge, der Hinweistext bleibt) und E9b (der
±-Knopf an drei Orten, die Zeilen 8 und 9 der Szenariotafel, der Hinweistext entfällt, der Ausweis „n von m").
Vorgänger: [`E8c_Bemessungstexte_Startjahr_Protokoll.md`](E8c_Bemessungstexte_Startjahr_Protokoll.md). Zweig `e9` von
`46023235` (`origin`, Schemastand 114, Basis R13), zwei Phasen und zwei Nachzüge; Phase 1: `7f481f6a` (E9a/1),
`12d3f0a6` (E9a/2), `ac7f3b90` (E9a/3), `399a5a18` (E9a/4), `1626b5ff` (E9a/5), `d2ef1862` (E9a/6), `a066b2fd`
(E9a/7), `0da92072` (E9a/8); Phase 2: Merge `bdd06e6e` (`origin` = `a1df2dbe`); zweiter Nachzug: Merge `debb3a5c`
(`origin` = `ea6f8608`, Zapfprofil Z3 mit Schritt 115) und `49fd15fa` (E9a/9). Merge `62613292` auf dem Hilfszweig
`pm8` über `origin` = `8793591b` (27 Dateien, +3.590/−89; der Baum gleicht `49fd15fa` bis auf die drei
Markdown-Dateien des Z3-Nachtrags). Opus 5.5 im Worktree `.claude/worktrees/e9`. **Rechenwirksam je Pflege,
ergebnisneutral ohne Pflege** — `SchemaStand.Zielversion` = 118; Erwartet rechnet bitgleich, Anker und Referenzlauf
unverändert.

## Befund vor der Welle

- **Die Szenarien variierten nur den W5‑B‑9-Satz** (Szenarienkonzept § 2): Günstig und Ungünstig ersetzen Zins und
  die drei Preissteigerungen (`WirtschaftlichkeitParameter.FuerSzenario`) und wirken in der Eingabe auf Investition,
  Erträge und Nutzungsdauer ungepflegter Positionen; „Alles Weitere (Betrachtungszeitraum, Einspeisevergütung, KWKG,
  Steuern, Bilanzierung) bleibt unverändert" (Kommentar an `FuerSzenario`). Betrachtungszeitraum, Trägerpreise,
  Erlössätze und die Mengen der Simulation waren in allen drei Szenarien gleich; das sagt der Hinweistext
  `WIRT_SZEN_HINWEIS` unter der Annahmentafel (V‑4, gebaut #434).
- **Die Tabellen führten keine Paare dafür** (Faktenblatt E9, `PRAGMA table_info` der Testdatenbank):
  `Tab_ProjektWirtschaftlichkeit` trug die 14 Szenariospalten der Schritte 71 und 72 — keine für den
  Betrachtungszeitraum (`Szen_*_Dauer` ist die Nutzungsdaueränderung), keine Menge, keine Erlössätze;
  `energy_project_settings` nur `custom_price_work`, `custom_price_base` und `custom_price_power`;
  `Tab_ProjektPhotovoltaik` `DvEntgelt` und `PpaPreis` ohne Paar.
- **Die Papiere:** Konzept § 2.11.5 führte Energiepreise, Erlössätze, den Zeitraum der Rahmenzeile und die Mengen als
  „neu"; Analysepapier § 6 plante die Schritte B, C und D (reines DDL, Doppelpflicht bei B und D); der Mockup-Anhang
  führte U15 offen. Vergabe vom 24.09.2026: 116, 117 und 118 — 114 hatte die Kühlung KU2 genommen, 115 war der
  Zapfprofil-Stufe Z3 zugesagt.

## Gebaut — Phase 1 (E9a/1 bis E9a/8)

- **Schritt 116 = B, Szenariorahmen (E9a/1, `7f481f6a`).** `SCHRITT_116_SZENARIO_RAHMEN`
  (`SchemaKatalog.Schritt116_Szenariorahmen`): `Szen_Best_Zeitraum` und `Szen_Worst_Zeitraum` (ganze Jahre) und
  `Szen_Best_Menge` und `Szen_Worst_Menge` (Prozent) an `Tab_ProjektWirtschaftlichkeit`, nullbar, reines DDL nach dem
  Muster von Schritt 71; eigene Spaltennamen, nicht `Szen_*_Dauer`. Doppelpflicht: die CREATE-TABLE-Texte und
  `SpalteSicher` der `WirtschaftlichkeitCtrl`, dazu die Nachziehliste der Testdatenbank-Vorrichtung und das Werkzeug
  `Testdatenbankschema`.
- **Schritt 117 = C, Trägerpreise best/worst (E9a/2, `12d3f0a6`).** `SCHRITT_117_TRAEGERPREIS_SZENARIO`:
  `custom_price_work_best`/`_worst`, `custom_price_base_best`/`_worst` und `custom_price_power_best`/`_worst` an
  `energy_project_settings`, nullbar. Keine Doppelpflicht — der Kern legt die Tabelle nirgends selbst an;
  `VariantenCtrl` kopiert die sechs Spalten beim Anlegen einer Variante mit.
- **Schritt 118 = D, Erlössätze best/worst (E9a/3, `ac7f3b90`).** `SCHRITT_118_ERLOESSATZ_SZENARIO`:
  `Einspeiseverguetung_Best`/`_Worst` und `Einspeiseverguetung_KWK_Best`/`_Worst` an `Tab_ProjektWirtschaftlichkeit`
  (Doppelpflicht), `DvEntgelt_Best`/`_Worst` und `PpaPreis_Best`/`_Worst` an `Tab_ProjektPhotovoltaik` (Doppelpflicht
  PPV). Nicht in D: `PpaSpotAufschlag`, `MarktwertJahresmittel`, `MarktwertEntwicklung` (E9a‑Q1, Lesart a).
  `SchemaStand.Zielversion` 118.
- **Testdatenbank (E9a/4, `399a5a18`).** Mit dem Werkzeug von 114 auf 118 gezogen: LFS `8a3bebaf…` (67.751.936 Byte) →
  `64383984…` (67.756.032 Byte), die 18 Spalten leer; ein zweiter Lauf ändert den Inhalt nicht. Der Stand ist mit dem
  zweiten Nachzug überholt (E9a/9, unten).
- **Daten (E9a/5, `1626b5ff`).** `SzenarioSatz` um `Zeitraum` (ganze Jahre), `Menge` (%), `Einspeiseverguetung` und
  `EinspeiseverguetungKwk` (€/kWh) — Laden und Speichern in der `WirtschaftlichkeitCtrl`; `NurVorgaben` und die
  Pflege-Erkennung der Ergebnisseite zählen sie mit; die Trägerkarte (`EnergietraegerPreisCtrl`, `TraegerpreisSzenario`
  mit sechs Feldern), die PV-Karte (`ProjektPhotovoltaikModel`, vier Felder). **Die Regel:** leer oder 0 heißt „wie
  Erwartet", gepflegt ist ein Wert erst, wenn er sich um mehr als 1e−9 vom Erwartungswert unterscheidet
  (`SzenarioSatz.Gepflegt`, dieselbe Regel für Trägerpreise und PV-Erlössätze); **keine Vorgaben** (E9a‑Q5, Lesart a) —
  anders als die sieben Größen des W5‑B‑9-Satzes, deren leeres Feld der Vorgabe folgt.
- **Der Kern liest die Paare (E9a/6, `d2ef1862`)** — an je einer Stelle, Tafel im nächsten Abschnitt.
- **Berichte (E9a/7, `a066b2fd`).** Die Nachweiszeile je Szenario (`SzenarioSatz.Nachweis`: die Szenariozeile der
  Seite, die Herleitungszeile des Parameterdialogs, die Annahmenzeilen in Wort- und Tabellenbericht) nennt jetzt immer
  den Betrachtungszeitraum („T = 20 a") und die Einspeisevergütung, die Einspeisevergütung KWK, wo Projekt oder
  Szenario eine führt, die Mengenänderung nur, wenn sie gepflegt ist; die gepflegten Trägerpreise stehen je Stand als
  eigene Zeile „Trägerpreise dieses Szenarios: …" (`TraegerpreisSzenario.Nachweiszeile`). Die Annahmentafel der Seite
  führt den Betrachtungszeitraum je Szenario mit dem wirksamen Wert (Herkunft „gepflegt", wo ein Szenario einen eigenen
  trägt), die Zeilen Mengenänderung, Einspeisevergütung PV und Einspeisevergütung KWK nur bei Pflege. Formelmappe
  Stufe 0: Der Parameterblock verschiebt das Blatt um 15 statt 12 Zeilen (Kopf, elf Parameterzeilen, Hinweis, Grenze,
  Leerzeile) — neu Mengenänderung, Einspeisevergütung PV und KWK je Szenario, der Betrachtungszeitraum je Szenario in
  den Spalten Günstig und Ungünstig, dazu je gepflegtem Trägerpreis eine Zeile je Stand, Träger und Preisart. Verlauf
  (Seite, Excel-Verlaufsblock, Wortbericht) und Zahlungsgliederung laufen je Szenario über dessen Zeitraum
  (`BerechneVerlaufSzenarienJeZeitraum`, `KapitalwertVerlaufHuelle`). Der Hinweistext `WIRT_SZEN_HINWEIS` bleibt bis
  E9b; die Anhang-E-Checkliste ist unverändert.
- **Tests (E9a/8, `0da92072`).** `SzenarioParameterTests` 22 neue Fälle (die Klasse zählt damit 39): Die Schritte 116
  bis 118 führen 18 Spalten; die Repo-Testdatenbank trägt sie auf Stand 118 (Werkzeug-Wache, liest die Datei
  schreibgeschützt); ohne Pflege rechnen die neuen Größen wie Erwartet; die 1e−9-Regel; `FuerSzenario` trägt Zeitraum
  und Einspeisevergütungen; der Mengenfaktor 1 + Menge/100, nie negativ; `SzenarioMengen` skaliert Mengen und
  Lastgang, nicht die Leistungen; Nachweiszeile und Parameterblock; DV-Entgelt und PPA-Preis je Szenario in der
  Vergütungsreihe; der Speicherweg über drei Controller (Rahmen und Erlössätze, Trägerpreise, PV-Erlössätze); auf
  Projekt 1030 der Zeitraum je Szenario Zahl für Zahl wie ein Lauf mit diesem T, der Verlauf je Szenario über seinen
  Zeitraum, die Menge ±10 % verschiebt die Energiekosten symmetrisch um die Festbeträge; Arbeits- und Grundpreis
  ersetzen den Preis als Ganzes, der Leistungspreis wirkt ohne Staffel und bleibt neben der Staffel ohne Wirkung;
  Einspeisevergütung PV und KWK auf einem Prüfstand; das Rollenmodell. `BerichtBlattstrukturWacheTests`: Die
  Stufe‑0-Prüfung kennt die drei neuen Zeilen.
- **Ressourcen** (de und en): **17 neu** — `WIRT_SZ_*` 8 (die Kohärenzzeilen `LEISTUNGSPREIS_OHNE_WIRKUNG`,
  `ROLLEN_STROMPREIS`, `ROLLEN_EINSPEISUNG`, `PV_DIALOG_EINSPEISUNG`, die Nachweiszeile `TRAEGERPREISE` mit `TP_ARBEIT`,
  `TP_GRUND`, `TP_LEISTUNG`), `WIRT_FM_PARAM_*` 6 (`MENGE`, `VERGUETUNG`, `VERGUETUNG_KWK`, `TP_ARBEIT`, `TP_GRUND`,
  `TP_LEISTUNG`), `WIRT_ANN_*` 3 (`MENGE`, `VERGUETUNG`, `VERGUETUNG_KWK`); keiner geändert oder gestrichen; Designer neu
  erzeugt und wiederholbar; SQL-Prüfer 1.752 Texte, 0 Fundstellen.

## Die Stellen, die je Szenario lesen

Erwartet bekommt in jedem Fall dieselbe Referenz wie vorher — ohne Pflege rechnet jedes Szenario bitgleich.

| Größe (Schritt) | Die eine Stelle | Wirkung | Frage |
|---|---|---|---|
| Betrachtungszeitraum (116) | `WirtschaftlichkeitParameter.FuerSzenario` — die Kopie trägt T_s | Horizont, Restwert am Ende von T_s, Ersatzbeschaffungen innerhalb T_s, die PV-, KWKG- und CO₂-Reihen — wie ein Erwartet-Lauf mit diesem Zeitraum; der Verlauf über `BerechneVerlaufSzenarienJeZeitraum` (Excel-Verlaufsblock, Wortbericht, `KapitalwertVerlaufHuelle`) | E9a‑Q4 a |
| Mengenänderung (116) | `SzenarioMengen.Variante` in `WirtschaftlichkeitCtrl.Szenariodaten`; ein zweiter Aufruf in `EndenergieAufloeser` für die nach Endenergie bemessenen Betriebskosten | f = 1 + Menge/100 auf eine Kopie des Ergebnisbaums und der Stundenreihen — Erzeugung von Strom und Wärme, Einspeisung, Bezug, Brennstoffeinsatz, Hilfsenergie, Vollbenutzungsstunden, Lastgänge samt Bezugsspitze —, bevor Preise, Sätze und Kontingente sie treffen; Leistungen, Prozente, Gerätedaten, Festbeträge und gesetzliche Sätze bleiben, Deckel greifen nach dem Faktor; ohne Pflege keine Kopie | E9a‑Q2 a |
| Arbeits-, Grund- und Leistungspreis je Träger (117) | `TraegerpreisSzenario.Wirksam`, angewandt in `KostenEmissionRechner.LadeTraeger` (Energiekosten, Anlagenzeilen, Parameterblock) und `WirtschaftlichkeitCtrl.StromArbeitspreisEurJeKwh` (PV-Mehrbezug, vermiedener Bezug) | Der Szenariopreis ersetzt den wirksamen Erwartet-Preis als Ganzes (nach der Rückfallkette Projektwert → Preisstand → Katalog), die Preisanteile der Zerlegung bleiben prozentual; eine gepflegte Staffel oder Saisonreihe gilt weiter, der Szenario-Leistungspreis bleibt dann ohne Wirkung (Kohärenzzeile) | E9a‑Q3 a |
| Einspeisevergütung PV und KWK (118) | `FuerSzenario` | der Einspeiseerlös der Eingabe | E9a‑Q1 a |
| DV-Entgelt und PPA-Preis (118) | `ProjektPhotovoltaikCtrl.FuerSzenario` in der PV-Vergütungsrechnung | die Vergütungsreihe des PV-Dialogs | E9a‑Q1 a |
| Tarif-Rollenmodell | die Rollenpreise des Tarifsatzes bleiben in allen Szenarien Erwartet | Szenario-Strompreis und Szenario-Einspeisevergütung ohne Wirkung, je eine Kohärenzzeile; ebenso die flache Einspeisevergütung PV neben einem aktiven PV-Vergütungsdialog | E9a‑Q7 a |

Die Kohärenzzeilen sind Hinweise ohne Rechenwirkung (gerechnet ist richtig, nur ohne den gepflegten Wert); sie stehen
wie die übrigen Kohärenzzeilen auf der Ergebnisseite und in beiden Berichten.

## Lücke 115 und Schrittvergabe

- **Vergabe:** 114 Kühlung KU2 (KU‑S3, `SCHRITT_114_KUEHLUNG_ERZEUGER`, Commit `24074b3a`), 115 Zapfprofil-Stufe Z3 (T2,
  `SCHRITT_115_ZAPFKATEGORIEN`, `Tab_TwwZapfkategorie_STAMM`, #453), 116 bis 118 die Schritte B, C und D dieser Welle;
  der nächste freie Schritt ist 119.
- **Phase 1 lag vor 115** (`origin` trug 114). Mechanisch verkraftet: Die Migration überspringt Schritte mit einer
  Nummer ≤ Stand, Werkzeug und Nachziehliste prüfen jede Spalte einzeln und setzen die Marke erst am Ende; eine Kopie
  ging von 114 auf 118. Verletzt war bis dahin die Regel „lückenlos aufsteigend", und das Risiko war echt: Eine
  Anwender-Datenbank, die ein Build mit 116 bis 118, aber ohne 115 auf 118 hebt, führt 115 nie mehr aus. Deshalb kam
  der Merge erst nach Z3.
- **Der zweite Nachzug `debb3a5c`** holt `origin` = `ea6f8608` (Z3 mit Schritt 115). Konflikte: `SchemaStand.cs`
  (Dokublock 115 vor 116 bis 118, der Satz „115 bleibt frei" entfällt; Zielversion 118), `SchemaMigration.cs`
  (Konstante, Schrittliste und Methode 115 vor 116 bis 118; der Dokukommentar von `SCHRITT_115_ZAPFKATEGORIEN` bekommt
  sein fehlendes `<summary>`), `Werkzeuge/Testdatenbankschema/Program.cs` (Nachziehliste 115, dann 116 bis 118; der
  Satz zur Lücke entfällt), `EPOS.Kern.Tests/TestDatenbank.cs` (`SchemaNachziehen` ebenso), beide `.resx` (beide Seiten,
  die Naht `</data>` geschlossen; je Sprache 8.848, keine doppelt, de/en deckungsgleich, jeder Wert von `origin`
  unverändert; Designer neu erzeugt) und der LFS-Zeiger (die Fassung von `origin`, Stand 115). `SchemaKatalog.cs` hatte
  `origin` nicht berührt (115 steht in `TwwSchema.cs`).

## Testdatenbank

- **Endstand `49fd15fa` (E9a/9):** die Fassung von Z3 (LFS `fbc30835…`, 67.780.608 Byte, Schemastand 115) mit dem
  Werkzeug auf 118 gezogen — erster Lauf: 115 vorhanden (keine Tabelle angelegt), die 18 Spalten der Schritte 116 bis
  118 angelegt, Marke 118; LFS `e9748b7f…`, 67.784.704 Byte. Zweiter Lauf: 0 Spalten, 0 Tabellen, Inhalt gleich (131
  Tabellen, 0 Unterschiede). Gegen 115 unterscheiden sich nur die drei Tabellen um die 18 leeren Spalten und die
  Schemaversion 115 → 118; `integrity_check` ok, `foreign_key_check` leer; der Katalog der Zapfkategorien (28 Zeilen)
  ist erhalten.
- **Kein Referenzprojekt trägt eine Pflege** — alle 18 Spalten sind leer, der Referenzlauf bleibt byte-gleich.

## Phase 2 und die Nachzüge

- **Merge `bdd06e6e`** holt `origin` = `a1df2dbe` (E8c #460 samt Papieren, Dialog Design #458 Stufe 2). Konflikte nur
  in beiden `.resx` (beide Seiten hängen am Dateiende an; beide behalten, die Naht `</data>` geschlossen): je Sprache
  8.770 (8.753 + 17), keine doppelt, de/en deckungsgleich, BOM und CRLF; der Designer neu erzeugt, unverändert gegen
  den Mergestand; die Codedateien automatisch zusammengeführt; der LFS-Zeiger `64383984…` bleibt. Kern-Filter,
  Windows-Schale und `EPOS.Referenzlauf` 0 Fehler.
- **Tests auf `bdd06e6e`** (erst gestartet, als kein fremder Testprozess lief): voller Lauf `WP-Plan.Kern.slnf`
  0 Fehler, **12.177 bestanden, 1 übersprungen** (EPOS.Kern 5.503, EPOS.UI 5.737, KiKern 524, SpeicherEngine 386,
  SpeicherPlanung 27 und 1 übersprungen); gefiltert (`SzenarioParameterTests`, `BerichtBlattstrukturWacheTests`) 52/52,
  die 22 neuen 22/22 — die Datenbankfälle liefen gegen die Testdatenbank (74 bis 290 ms je Fall); Referenzlauf 13/13
  PASS gegen `2026-09-23_R13_Kuehlung`, 4.145.687 Werte in der Toleranz, 387/387 CSV byte-gleich; Zellvergleich unten.
- **Zweiter Nachzug** (`debb3a5c`, `49fd15fa`, oben): gefilterter Lauf 153/153 (`SzenarioParameterTests` 39, die
  Werkzeug-Wache, Tww/Zapfkategorien, Erstbereitstellung), Referenzlauf 13/13 gegen R13, SQL-Prüfer 1.763 Texte,
  0 Fundstellen.
- **Merge `62613292`** auf dem Hilfszweig `pm8` über `origin` = `8793591b` (Z3 samt Papier-Nachtrag zu #453): 27 Dateien,
  +3.590/−89.

## Fragen aus der Welle

Der Phase‑1-Bericht nennt sieben Fragen. **Alle sieben sind beim Anwender offen**; gebaut ist jeweils Lesart a, bei
E9a‑Q6 ist nichts zu bauen. Sie stehen im Entscheidungsregister als **R‑E9a**.

| Frage | Lesarten | Empfehlung | Stand |
|---|---|---|---|
| **E9a‑Q1** Umfang von Schritt D | (a) Einspeisevergütung, Einspeisevergütung KWK, DV-Entgelt, PPA-Preis; (b) zusätzlich der Spot-Aufschlag des PPA und die beiden Marktwertfelder (Marktwert-Override `MarktwertJahresmittel`, `MarktwertEntwicklung`) | a | offen; gebaut ist a |
| **E9a‑Q2** Mengenfaktor | (a) alle kWh-Mengen, Stundenreihen und die Bezugsspitze — Leistungen, Prozente, Gerätedaten, Festbeträge und gesetzliche Sätze bleiben, Deckel greifen nach dem Faktor; (b) nur Erzeugung und Einspeisung | a | offen; gebaut ist a |
| **E9a‑Q3** Trägerpreise | (a) Der Szenariopreis ersetzt den Preis als Ganzes; eine gepflegte Staffel oder Saisonreihe gilt weiter, dazu eine Kohärenzzeile; (b) nur ein Aufschlag auf den Energieanteil | a | offen; gebaut ist a |
| **E9a‑Q4** Zeitraum | (a) wirkt auf Horizont, Restwert und Ersatzbeschaffungen; (b) nur auf den Horizont | a | offen; gebaut ist a |
| **E9a‑Q5** Vorgaben | (a) Die neuen Größen haben keine Vorgabe — leer heißt „wie Erwartet"; (b) Vorgaben wie bei den sieben Größen des W5‑B‑9-Satzes | a | offen; gebaut ist a |
| **E9a‑Q6** Risiko (V‑G7) und n-jährliche Zeitpunkte (V‑G3) | (a) nicht Teil von E9, nur ein Vermerk im Register; (b) in E9b | a | offen; vermerkt im Register (R‑V) |
| **E9a‑Q7** Tarif-Rollenmodell | Befund: Im Rollenmodell ist der Erwartet-Strompreis der Reststromtarif, die Einspeisung bewertet der Einspeisetarif; ein Szenario-Strompreis kürzt sich heraus, eine Szenario-Einspeisevergütung wirkt nicht, Gaspreise und Menge wirken weiter — (a) die Rollenpreise bleiben in allen Szenarien Erwartet, eine Kohärenzzeile nennt beides, ebenso eine flache Einspeisevergütung neben einem aktiven PV-Vergütungsdialog; (b) eigene Best/Worst-Preise am Tarifsatz (bräuchte einen neuen Schemaschritt); (c) der Szenario-Strompreis als Faktor auf die Rollenpreise — nicht empfohlen, weil das die Wirkung verdeckt | a | offen; gebaut ist a |

**Mit dieser Welle teilweise erledigt:** V‑G5 (R‑V) im Kern — die Pflege über die Dialoge kommt mit E9b; V‑4 (R‑V) —
der Hinweistext stimmt nur noch ohne Pflege und entfällt mit E9b; A5 (R‑A) — V‑E ist ohne Degradation gebaut; Mockup
U15 teilweise (Kern).

## A/B-Nachweis

Kapitalwerte in € auf einer Arbeitskopie der Testdatenbank (Schemastand 118), frisch simuliert mit Zeitreihen; je Größe
genau eine Pflege (SQL auf der Kopie), vorher und nachher gerechnet. **Erwartet ist in allen neun Fällen bitgleich**,
und nach dem Zurücksetzen jeder Pflege kommen die Ausgangszahlen exakt wieder. Die Differenzen sind symmetrisch bzw.
proportional, wie gebaut.

| Größe | Projekt | Pflege Günstig / Ungünstig | KW Erwartet (vorher = nachher) | KW Günstig vorher → nachher (Δ) | KW Ungünstig vorher → nachher (Δ) | Zeile je Jahr Günstig / Ungünstig |
|---|---|---|---|---|---|---|
| Zeitraum (B) | 1030 | T 25 a / 15 a (Erwartet 20 a) | −31.141.243 | −31.309.742 → −38.224.910 (−6.915.168) | −31.005.298 → −23.801.792 (+7.203.506) | Restwert-Barwert 169.045 → 141.856 / 158.501 → 224.645 |
| Menge (B) | 1030 | −10 % / +10 % | −31.141.243 | −31.309.742 → −28.253.736 (+3.056.006) | −31.005.298 → −34.007.774 (−3.002.477) | Energiekosten 1.624.616 → 1.462.515 / 1.624.616 → 1.786.718 |
| Arbeitspreis (C) | 1030 | Erdgas 0,74 / 0,94 €/Nm³ (Erwartet 0,84) | −31.141.243 | −31.309.742 → −30.177.956 (+1.131.785) | −31.005.298 → −32.117.259 (−1.111.961) | Energiekosten 1.624.616 → 1.561.334 / 1.624.616 → 1.687.898 |
| Grundpreis (C) | 1030 | Strom 2.000 / 2.800 €/a (Erwartet 2.400) | −31.141.243 | −31.309.742 → −31.302.588 (+7.154) | −31.005.298 → −31.012.326 (−7.029) | Energiekosten 1.624.616 → 1.624.216 / 1.624.616 → 1.625.016 |
| Leistungspreis (C) | 1030 | Strom 60 / 100 €/(kW·a) (Erwartet 80, Ausgangslage) | −33.993.113 | −34.187.033 → −33.467.710 (+719.323) | −33.832.191 → −34.538.914 (−706.723) | Energiekosten 1.785.496 → 1.745.276 / 1.785.496 → 1.825.716 (∓ 20 €/(kW·a) × 2.011 kW Bezugsspitze) |
| Einspeisevergütung KWK (D) | 1030 | 0,07 / 0,03 €/kWh (Erwartet 0,05, Ausgangslage) | −31.140.951 | −31.309.389 → −31.309.248 (+141) | −31.005.058 → −31.005.154 (−96) | Einspeiseerlös KWK 22 → 30 / 18 → 11 |
| Einspeisevergütung PV (D) | 1040\* | 0,10 / 0,06 €/kWh (Erwartet 0,08, Ausgangslage) | −215.178 | −210.726 → −209.909 (+817) | −219.621 → −220.176 (−555) | Einspeiseerlös PV 200 → 250 / 163 → 123 |
| DV-Entgelt (D) | 1040\* | 0,20 / 0,60 ct/kWh (Erwartet 0,40, Marktprämie) | −215.280 | −210.848 → −210.767 (+82) | −219.704 → −219.759 (−56) | Einspeiseerlös PV 192 → 197 / 157 → 153 |
| PPA-Preis (D) | 1040\* | 8,0 / 5,5 ct/kWh (Erwartet 7,0, sonstige Direktvermarktung) | −215.516 | −211.134 → −210.726 (+408) | −219.898 → −220.315 (−416) | Einspeiseerlös PV 175 → 200 / 143 → 112 |

**Zur Tafel.** „Ausgangslage" heißt: Der Erwartet-Wert ist auf der Kopie erst gesetzt, damit die Größe überhaupt
rechnet (1030 führt keinen Strom-Leistungspreis und keine Einspeisevergütung KWK). \* **Warum 1040 mit Ausgangslage:**
Kein PV-Projekt der Testdatenbank rechnet Energiekosten — 1026, 1028, 1040 und 1045 haben keinen Stromträger, 1007
hat Brennstoff ohne Träger, ebenso 1018, das einzige Projekt mit nennenswerter KWK-Einspeisung (27,5 MWh). Auf der
Kopie hat 1040 deshalb die Trägerpreise von 1030 bekommen (Gas und Strom; Energiekosten 10.643 €/a), für DV-Entgelt und
PPA-Preis dazu eine Zeile im PV-Vergütungsdialog. 1040 hat nur 2,27 MWh PV-Überschuss, 1030 nur 0,39 MWh
KWK-Einspeisung; deshalb sind die Beträge in diesen Zeilen klein. **Zum Zeitraum:** 1030 ist kostendominiert —
der längere Zeitraum senkt den Kapitalwert (Günstig −38,2 Mio. €), der kürzere hebt ihn (Ungünstig −23,8 Mio. €).
**Zur Menge:** Das Vorzeichen ist für 1030 Günstig −10 % / Ungünstig +10 % gewählt, weil dort die Kosten überwiegen.

## Nachweis „Erwartet bitgleich", Referenzlauf und Zellvergleich

- **Erwartet:** in allen A/B-Fällen bitgleich; die Ankertests (`WirtschaftlichkeitAnkerTests`) im vollen Lauf
  unverändert grün; Referenzlauf 13/13 PASS gegen `2026-09-23_R13_Kuehlung`, 4.145.687 Werte in der Toleranz, 387/387
  CSV byte-gleich — keine Pflege in den Referenzprojekten, keine Simulationsgröße berührt.
- **Zellvergleich** (Messprogramm des E8b-Berichts) über **15 Prüfgruppen** — die 13 aus E8b, dazu hybarten und
  hybluecke aus E8c —, der Stand vor E9a (`a1df2dbe`) gegen den gemergten Stand `bdd06e6e`, beide auf derselben
  Testdatenbank: je Gruppe **3 Zeilen eingeschoben** (Mengenänderung, Einspeisevergütung PV, Einspeisevergütung KWK),
  keine Zeile verloren; geändert **genau 2 Textzellen je Gruppe** — die Annahmenzeilen Günstig und Ungünstig, die jetzt
  „T = 20 a" und die Einspeisevergütung nennen; keine Zahl weicht ab, die Formelzahl ist gleich. **Wertfassung =
  Formelfassung:** Excel 16 rechnet nach voller Neuberechnung in allen 15 Gruppen jede Formelzelle auf ihren
  gespeicherten Wert; ClosedXML-Nachrechnung „abweichend 0" (nicht nachrechenbar wie bekannt nur `NPV`, `IRR` und die
  Zellen, die darauf zeigen); OpenXML-Prüfung 0 Fehler; die Gliederung des Wortberichts unverändert.

## Abweichungen und Befunde

- **Formelmappe Stufe 1 und 2 rechnen nur Erwartet** (so seit E8b‑Q1): „T je Szenario" wirkt in der Mappe im
  Parameterblock und im Verlaufsblock (Jahre jenseits von T_s bleiben leer); Kapitalwert und Annuität je Szenario gibt
  es in der Mappe nicht als Formel.
- **Szenariopreis ohne Erwartet-Preis:** Günstig und Ungünstig rechnen dann, Erwartet zeigt die Datenlücke — das soll
  der Dialog in E9b kennzeichnen (E9b‑Q4).
- **Menge und Ertragsänderung multiplizieren sich** auf den Erlöszeilen (+10 % und +10 % ergeben +21 %) — die Folge
  aus E9a‑Q2 a; die Ertragsänderung bleibt eigenständig.
- **0 in der Datenbank:** Der Speicherweg der Wirtschaftlichkeit schreibt eine 0 als 0 und liest sie als leer;
  Trägerkarte und PV-Karte schreiben NULL. Rechnerisch folgenlos.
- **Zeitreihen auch bei Szenario-Leistungspreis:** Ein Strom-Leistungspreis allein im Szenario lässt den Sammler die
  Zeitreihen holen — richtig, kostet aber Laufzeit.
- **Der Hinweistext** `WIRT_SZEN_HINWEIS` stimmt nur noch ohne Pflege; E9b nimmt ihn heraus.
- **„Günstig = länger" ist nicht allgemein günstig:** Bei einem kostendominierten Projekt senkt ein längerer Zeitraum den
  Kapitalwert (A/B 1030) — das soll der Dialog in E9b sagen.
- **Die Vorgaben sind unsortiert:** Schon ohne Pflege liegt der Kapitalwert von Günstig unter Erwartet (−31,31 Mio. gegen
  −31,14 Mio. €), weil der niedrigere Zins die Kosten höher abzinst — aus W5‑B‑9, nicht aus E9a.
- **Lücke in der Testdatenbank:** Es fehlt ein PV-Projekt mit vollständigen Preisen — eine eigene Datenaufgabe.
- **Sichtbar ohne Pflege:** Szenariozeile, Herleitungszeile des Parameterdialogs und Annahmenzeilen nennen jetzt
  „T = … a" und die Einspeisevergütung; der Parameterblock der Mappe hat drei Zeilen mehr. Sonst ändert sich ohne
  Pflege nichts.
- **Der Probe-Merge in Phase 1** (`git merge-tree` gegen 39 Commits auf `origin`) zeigte Konflikte nur in den `.resx` —
  so kam es beim Nachzug `bdd06e6e`.

## Zahlen und Abnahme

- **Phase 1** (Worktree `e9`): Kern-Filter und Windows-Schale 0 Fehler; Designer unverändert und wiederholbar;
  SQL-Prüfer 1.752 Texte, 0 Fundstellen — kein `dotnet test`, kein Referenzlauf.
- **Phase 2** (auf `bdd06e6e`): voller Lauf 0 Fehler, **12.177 bestanden, 1 übersprungen**; gefiltert 52/52; Referenzlauf
  13/13 PASS gegen R13; Zellvergleich 15 Prüfgruppen; A/B-Tafel neun Größen.
- **Zweiter Nachzug** (auf `49fd15fa`): gefiltert 153/153, Referenzlauf 13/13 gegen R13, SQL-Prüfer 1.763 Texte,
  0 Fundstellen.
- **Gate auf `62613292`** (Worktree `pm8`, 03:19–03:23), grün: Kern-Filter (Release) 0 Fehler; ChartProben 146 Bilder,
  alle grün, Hashes gleich der Windows-Messlatte (146/146; E9a bringt kein Bild); voller Lauf 0 Fehler, **12.308
  bestanden, 1 übersprungen** (EPOS.Kern 5.607, EPOS.UI 5.764, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27 und
  1 übersprungen); Dokumentationswachen 26/26. Referenzlauf 13/13 gegen R13 und SQL-Prüfer 1.763/0 aus dem Agentenlauf
  auf `49fd15fa` — derselbe Code, verschieden nur die drei Markdown-Dateien des Z3-Nachtrags.
- **Tests** (neu oder erweitert): `SzenarioParameterTests` (+22, jetzt 39), `BerichtBlattstrukturWacheTests` (Stufe 0 mit
  den drei neuen Zeilen).
- **Schema:** Schritte 116, 117 und 118, `SchemaStand.Zielversion` = 118; Testdatenbank LFS `e9748b7f…` (Stand 118).

## Abnahme am Gerät (A‑E9a‑1, Windows und iPad) — ohne Dialog

(1) Auf einer Kopie der Datenbank für das Stammprojekt 1030 `Szen_Worst_Zeitraum` = 15 setzen (SQL; eine Eingabestelle
gibt es bis E9b nicht) und die Wirtschaftlichkeit berechnen: Die Szenariozeile von Ungünstig nennt „T = 15 a", die
Annahmentafel führt in der Zeile Betrachtungszeitraum für Ungünstig 15 a mit der Herkunft „gepflegt"; der Kapitalwert
von Ungünstig ändert sich (A/B: −31.005.298 → −23.801.792 €), Erwartet nicht; die Linie Ungünstig des Verlaufs endet
nach 15 Jahren. (2) Wortbericht: Die Annahmenzeile Ungünstig nennt „T = 15 a". (3) Tabellenbericht: Der Block
„Parameter der Rechnung (je Szenario)" hat die Zeilen Mengenänderung, Einspeisevergütung PV und KWK (15 Zeilen samt
Kopf, Hinweis und Grenze), die Spalte Ungünstig trägt T = 15. (4) Den Wert wieder leeren (NULL): die Ausgangszahlen
kommen exakt wieder. (5) Ohne jede Pflege: Szenariozeile und Annahmenzeilen nennen „T = 20 a" (bei 1030) und die
Einspeisevergütung, sonst ist alles wie vorher. (6) Englisch.

## Logbuch

Mit #461 steht im Update-Papier ein Satz zum Kern (Version 1.2.0.4, Stichwort `szenarien`), ohne Versprechen zu den
Dialogen: „Führt ein Projekt für die Szenarien Günstig und Ungünstig eigene Werte für Betrachtungszeitraum, Mengen,
Energieträgerpreise oder Erlössätze, rechnet jedes Szenario mit seinen Werten; ohne sie gilt der Wert von Erwartet."

**Entwürfe aus dem E9a-Bericht, erst mit E9b zu veröffentlichen** (Versionsnummer dann beim Anwender erfragen):

- *szenarien:* „Die Szenarien Günstig und Ungünstig können einen eigenen Betrachtungszeitraum, eine Mengenänderung,
  eigene Energieträgerpreise sowie eigene Einspeisevergütungen, DV-Entgelte und PPA-Preise führen."
- *wirtschaftlichkeit:* „Bericht und Formelmappe nennen je Szenario Betrachtungszeitraum, Mengenänderung,
  Einspeisevergütungen und gepflegte Energieträgerpreise."

## Befunde nebenbei

- **Szenarienkonzept:** Die Kopfzeile nannte „Zielversion 113, neue ab 114" — mit den Papieren zu #461 auf 118 und E9a
  berichtigt; ebenso § 2.4 (`FuerSzenario` ersetzt jetzt auch Zeitraum und Einspeisevergütungen) und § 4 (die
  Aufzählung der Nachweiszeile).
- **Linux-Messlatte der ChartProben:** `Proben/ChartProben/Messlatte_2026-09-20.sha256` im Repository hat 91 Zeilen, die
  Windows-Messlatte des Gates 146 — Nachtrag beim nächsten Linux-Lauf.
- **CI-Nachweise** der Pushes vor dieser Welle: zu `3986c7ae` (Papiere zu #460) Kern-Lauf `main` 35938295339 und
  Windows-Lauf `main` 35938295404 grün, der Kern-Lauf `ios_migration_september` abgebrochen — überholt durch den Push
  `a1df2dbe` (#458 Stufe 2 über #460), dessen Lauf 35938837705 grün ist; zu `ea6f8608` (Zapfprofil Z3, #453) Kern-Lauf
  35941477369 grün. Der CI-Nachweis zu #461 steht noch aus — der Push folgt nach dem Gate.
- **Testregel:** Den Einzellauf der 22 neuen Tests (etwa 3 s) hat der Agent um 02:50 gestartet, während ein fremder
  Testprozess der Zapfprofil-Sitzung lief (`Auslieferungsvorlage.Tests`, gestartet 02:50:11) — Prüfung und Lauf standen
  in einem Aufruf. Gemeldet; die Ergebnisse waren grün, danach prüfte der Agent vor jedem Lauf getrennt und wartete.

## Offen

- **Die sieben Fragen** E9a‑Q1 bis E9a‑Q7 beim Anwender (Empfehlung jeweils a).
- **Abnahme am Gerät** A‑E9a‑1 (sechs Punkte oben).
- **Nächste Welle: E9b** (voraussichtlich #462; Auftrag `E9b_Auftrag_2026-09-24.md`) — der ±-Knopf an drei Orten
  (Trägerkarte je Preis, Einspeisevergütungen im Parameterdialog, DV-Entgelt und PPA-Preis im PV-Vergütungsdialog; ein
  verallgemeinerter `CaseEingabeDialog`), die Zeilen 8 (Betrachtungszeitraum) und 9 (Mengenänderung) der Szenariotafel,
  KI-Sicht, Formularkarten und Maskenwache; der Hinweistext `WIRT_SZEN_HINWEIS` entfällt, an seiner Stelle der Ausweis
  „n von m Parametern szenariert"; kein Schemaschritt.
- **Push** nach der Regel des Anwenders ohne Rückfrage aus dem Hilfszweig `pm8` (`62613292` und die Papiere) auf
  `ios_migration_september` und `main`; der CI-Nachweis kommt mit der nächsten Papierwelle.
- **Papiere mit der Statuszeile:** Register (Kopf, Familientafel, A5, R‑V mit V‑4, V‑G2, V‑G5 und dem Vermerk zu V‑G7 und
  V‑G3, neue Familie R‑E9a, EZ‑9, EZ‑10), Konzept (Kopf, § 2.11.4, § 2.11.5 Tafel und Regeln, § 2.11.7, § 6.1, § 6.2, § 7
  und Anhang), Szenarienkonzept (Kopf, § 2.4, § 4, § 11, § 11.1), Protokoll der Entscheidwege (§ 8.19, § 8.20, Kopf von
  § 8), Analysepapier (Kopf, Nachtrag, § 0, § 5 mit der Zeile E9, § 6 mit den Schritten 116 bis 118), Mockup (Zone „Was
  ist angenommen?", Ressourcentafel der Kategorie 8, Anhang U15 und U27, Stand-Absatz), Logbuch-Satz und die
  Wiki-Quelle der Seite Wirtschaftlichkeit, Index Reporting.

**Entscheide 24.09.2026:** alle nach Empfehlung, siehe Register R‑E9a

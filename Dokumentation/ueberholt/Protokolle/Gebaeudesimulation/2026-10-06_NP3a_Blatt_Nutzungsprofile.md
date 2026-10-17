# Protokoll NP3a — Blatt „Nutzungsprofile“ (06.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle NP3a der Stufe NP, `1b65fc8c`, `2471fae2`, `8f737ef5` (Zweig `np3a-nutzungsprofile-blatt` auf `b53d4957`), Merge `ee06f829`, Gate-Befunde `b7741400`, Papierpflege `5d9a8188`.
**Entscheide:** E90 (frei definierbare Nutzungsprofile), E91 (Q38–Q47), E92 (die Sitzung baut die ganze Stufe NP).

## 1 Auftrag

Das Blatt „Nutzungsprofile“ als Razor-Komponente mit Hülle und Zugang aus dem Gebäudeeditor (Konzept Nutzungsprofile, Stufe NP3, erster Teil).

## 2 Vorgehen

Ein Zweig `np3a-nutzungsprofile-blatt` mit drei Commits, Merge `ee06f829` ohne Konflikt (`.resx` je 15 246 Einträge, keine Doppel, Designer gleich). Das Gate `gate_haupt.sh NP3a` lief auf dem Merge; die Befunde wurden in `b7741400` behoben. Die Papiere (E90–E92 im Register der Entscheide, Ausschlusskataloge auf Normwerte verengt in acht Papieren, Konditionierungsprofile 5.7 freier Text, Datenaustausch 16, Leitkonzept N1.71) pflegte `5d9a8188`.

## 3 Ergebnis

Blatt `EPOS.UI/Dialoge/Bedarf/RaumnutzungBlatt.razor`, DTOs `RaumnutzungDaten.cs`, Texte `RaumnutzungTexte.cs` (87 Ressourcenschlüssel `RNP_*` in beiden Sprachen), Hülle `EPOS.UI.Daten/Bedarf/RaumnutzungHuelle.cs`, Knopf „Nutzungsprofile verwalten…“ im Gebäudeeditor über `GebaeudeKatalogHuelle`/`GebaeudeAdminHuelle`, CSS `.epos-raumnutzung*`. Jede Handlung schreibt sofort wie die Vorlagenverwaltung, es gibt keine Schlussleiste. Kein eigener KI-Maskenname; Eintrag in der Abdeckungswache mit 31 Eingabestellen. Tests `RaumnutzungBlattTests` 12, `RaumnutzungHuelleTests` 8.

## 4 Festlegungen

Kein Schemaschritt, keine neue Basis (R38 bleibt). Die Festlegungen der Stufe stehen im Konzept Nutzungsprofile.

## 5 Nachweise

Gate auf `ee06f829`: Kern-Filter rc=0; ChartProben grün, 211 Hashes gleich mit `Messlatte_2026-10-05.sha256`; KiKern 549/549, SpeicherEngine 397/397, SpeicherPlanung 27/28 (1 übersprungen), EPOS.UI.Tests 7566/7568 (2 rot), EPOS.Kern.Tests 11227/11231 (1 rot, 3 übersprungen).

Rot waren `FormularrasterTests.Ausserhalb_des_Rasters_bleibt_ein_Feld_unveraendert` (Block `.epos-raumnutzung*` stand hinter dem Formularraster-Block), `ParametersatzTests.Jeder_Parametersatz_einer_Huelle_trifft_die_Parameter_seiner_Komponente` und `SonderlistenVerwaltungTests.Die_Parametersaetze_treffen_die_Parameter_ihrer_Komponenten` (`GebaeudeAdminHuelle` gab `Raumnutzung`/`RaumnutzungTexte` an `GebaeudeAdminDialog`, der sie nicht führt; die Gebäudeverwaltung hätte beim Öffnen geworfen). Behoben in `b7741400`: zwei Zeilen der Hülle entfallen, der Katalogeditor bekommt beide über `GebaeudeKatalogHuelle.Grundgaben`, der CSS-Block steht vor dem Formularraster-Block. Nachtest auf `b7741400`: EPOS.UI.Tests 7568/7568, Kern-Auswahl (GebaeudeKatalogverweis, SonderlistenVerwaltung, GebaeudeHuellen, NachtzeitOberflaeche, Raumnutzung) 90/90.

Dokumentationswachen 35/35; Referenzlauf 21/21 gegen R38: GESAMT PASS (6 872 111 Werte), CSV byte-gleich 646/646; gestörter Lauf (`--stoerung ulp`) PASS; Windows-Schale rc=0; Designer 15 247 Einträge unverändert, wiederholbar; SqlDialektPruefer 2 432 Texte, 0 Fundstellen; Werkzeugtests Formularkarte 124, Auslieferungsvorlage 49, Gebaeudevergleich 24, ZapfprofilValidierung 39; BOM in Markdown: zwei Bestandsfunde seit 26.09. (`Werkzeuge/Formularkarte/LIESMICH.md`, `EPOS.iOS/CLAUDE.md`, nicht aus dieser Welle); Konfliktmarker keine. Die Statuszeile ist #745 der Statusdatei.

## 6 Offenes

Sichtabnahme des Blatts unter Windows beim Anwender, CI-Kennung nach dem Push. NP3b in Arbeit (Übernahme in Gebäudeeditor und Zonendialog mit Rückfrage und Herleitung, Fläche und lichte Höhe, `KonditionierungNutzung` als freier Text, Zugang aus dem Zonenbaum, KI-Feldkarten); NP2 und NP4 offen. Kein Wiki-Upload und kein Logbuch-Satz vor NP4 (Konzept Hilfesystem 13.3).

Lehre für die Agentenaufträge: Die Abnahme einer Hülle schließt `ParametersatzTests` und `SonderlistenVerwaltungTests` ein; neue CSS-Regeln stehen vor dem Formularraster-Block.

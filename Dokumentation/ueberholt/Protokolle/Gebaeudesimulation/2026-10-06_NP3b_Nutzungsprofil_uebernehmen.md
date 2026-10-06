# Protokoll NP3b — „Nutzungsprofil übernehmen…“ (06.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle NP3b der Stufe NP, `fcc28bcc`, `cf937e40`, `6e6d3050`, `1d48661a`, `854d8b68`, `6ba1b4f3`, Gate-Fix `b7741400` über Merge `63a5d4b9`, Merge `74d2337a` (konfliktfrei).
**Entscheide:** E90 (frei definierbare Nutzungsprofile), E91 (Q38–Q47), E92 (die Sitzung baut die ganze Stufe NP); Festlegungen NP-F10 und NP-F15 des Konzepts Nutzungsprofile.

## 1 Auftrag

Die Übernahme eines Nutzungsprofils in Gebäudeeditor und Zonendialog mit Rückfrage und Herleitung, Fläche und lichte Höhe des Ziels, die Nutzung als freier Text und der Zugang zum Blatt aus dem Zonenbaum des Imports (Konzept Nutzungsprofile, Stufe NP3, zweiter Teil).

## 2 Vorgehen

Sechs Commits NP3b-1 bis NP3b-6 auf einem Zweig, Merge `74d2337a` ohne Konflikt; der Gate-Fix `b7741400` kam über den Merge `63a5d4b9` herein. Das Gate `gate_haupt.sh NP3b` lief auf `74d2337a`.

## 3 Ergebnis

1. **Übernahme.** Ein Baustein `EPOS.UI/Dialoge/Bedarf/NutzungsprofilUebernahme.razor` steht im Reiter Konditionierung von Gebäudeeditor, Katalogbau und Verwaltung und in `ZonenKonditionierung` (Zonendialog). Die Profile erscheinen nach Kategorie gruppiert mit Kurzform; leere Profile sind „ohne Werte“ und haben keine Vorschau. Je Größe zeigt der Baustein eine Wochenvorschau, dazu eine Nennwertzeile mit der Herleitung aus einem Probelauf am Arbeitsstand. Die Rückfrage folgt P12 (Vorgabe „Nein“, sobald ein Kalender ersetzt wird). Das Ergebnis ist ein rücknehmbarer Schritt im Arbeitsstand und wird mit OK geschrieben. Kern: `RaumnutzungCtrl.ProfilAnwenden` (reine Anwendung, auch vom Datenbankweg genutzt), `Kalendernutzung` lesend. Die Zone trägt `Nutzungsprofil`, die Kopfzeile des Zonendialogs nennt es. Befund mit roter Probe: Der OK-Weg schrieb an Kalender aus einem Profil keine Nutzung, weil `StandSchreiben` den Profilnamen aus der Herkunft nimmt.
2. **Fläche und lichte Höhe** des Ziels gehen an die Übernahme (Zone: eigene Höhe, leer die des Gebäudes; Datenbankweg über `RaumnutzungCtrl.Zielmasse`). Rote Probe: Außenluft in m³/(h·m²) ohne Höhe wird benannt nicht gesetzt (NP-F10).
3. **Freie Nutzung (NP-F15).** Die Aufzählung `KonditionierungNutzung` ist entfernt. „Als Vorlage speichern…“ fragt die Nutzung als Text (≤ 120 Zeichen) mit Vorschlägen aus dem Katalog (`Suchauswahl`); die vier alten Kennungen zeigt die Anzeige weiter übersetzt (`Nutzungsanzeige`), Bestandswerte bleiben unverändert.
4. **Zonenbaum des Imports.** Der Knopf „Nutzungsprofile…“ öffnet das Blatt als Überlagerung (Esc schließt zuerst sie), danach wird neu zugeordnet; Gaben nur mit offenem Projekt.
5. **Assistent.** Die KI-Sicht der Zone führt das Nur-Lese-Feld `nutzungsprofil` (beide Sprachen); die Eingabebilanz der Kalenderkarte steigt von 4 auf 5.
6. **Nachbesserung.** Textbündel und Importgaben sind nach den Wachen geordnet; die Verwaltung führt `RaumnutzungTexte` als Parameter.

Neue Tests: `NutzungsprofilUebernahmeTests` 7, `RaumnutzungUebernahmeTests` 5.

## 4 Festlegungen

Kein Schemaschritt, keine neue Basis (R38 bleibt). Anwenderentscheid 06.10.2026: Der Einzonenweg der Projektdatei bekommt kein Profil automatisch; zugewiesen wird über „Nutzungsprofil übernehmen…“ im Gebäudeeditor, der Importdialog zeigt den Vorschlag aus der DIN-Nummer ohne Vorbelegung (NP-F16, umgesetzt in NP2b).

## 5 Nachweise

Gate auf `74d2337a`, alles grün: Kern-Filter rc=0; ChartProben grün, 211 Hashes gleich mit `Messlatte_2026-10-05.sha256`; KiKern 549/549, SpeicherEngine 397/397, SpeicherPlanung 27/28 (1 übersprungen), EPOS.UI.Tests 7575/7575, EPOS.Kern.Tests 11233/11236 (3 übersprungen); Dokumentationswachen 35/35; Referenzlauf 21/21 gegen R38: GESAMT PASS (6 872 111 Werte), CSV byte-gleich 646/646; gestörter Lauf PASS; Windows-Schale rc=0; Designer 15 274 Einträge unverändert, wiederholbar; SqlDialektPruefer 2 434 Texte, 0 Fundstellen; Werkzeugtests Formularkarte 124, Auslieferungsvorlage 49, Gebaeudevergleich 24, ZapfprofilValidierung 39; BOM in Markdown nur die zwei Bestandsfunde (seit 26.09.); Konfliktmarker keine. Die Statuszeile ist #748 der Statusdatei.

## 6 Offenes

Sichtabnahme unter Windows beim Anwender (Übernahme im Gebäudeeditor und Zonendialog, Rückfrage, Nennwertzeile, freie Nutzung beim Speichern als Vorlage), CI-Kennung nach dem Push. NP3c (KI-Feldkarte des Blatts, bunit-Probe des Zonenbaum-Knopfs, Herkunft Profil gegen gleichnamige Vorlage) und NP2a (Zuordnung und Import im Kern) sind gebaut und werden gemeinsam gegatet; NP2b (Zonenbaum gruppiert mit Herleitung, Hinweis „(nicht im Katalog)“, Einzonenweg) und NP4 offen. Kein Wiki-Upload und kein Logbuch-Satz vor NP4 (Konzept Hilfesystem 13.3).

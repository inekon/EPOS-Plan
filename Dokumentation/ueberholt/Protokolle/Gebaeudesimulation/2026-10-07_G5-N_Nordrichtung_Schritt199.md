# Protokoll G5-N — Nordrichtung abfragen und nachträglich ändern, Schemaschritt 199 `NordrichtungSchema` (07.10.2026)

**Sitzung:** IFC / Gebäudeimport, Statuszeile **#809**. Commits Kern: `fd362170c`, `c29b52856`; Schema: `fc8740334`, `a8004cee2`, `fc734fe2f`; Oberfläche: `2419f3de2`, `8bb3bed9d`, `aa20f70db`, `f175d90f0`, `e66fea08a`, `6f61bfabd`.
**Entscheid:** E101; Abstimmung [G5 IFC](../../../aktuell/Gebaeudesimulation/2026-10-07_Abstimmung_G5_IFC.md), Abschnitt 7.

## 1 Anlass

Keine der echten Dateien trägt eine Nordrichtung (IFC `TrueNorth`, gbXML `CADModelAzimuth`); der Import nahm still „Planoberseite = Nord“ an, und die Himmelsrichtung der Bauteile blieb ohne Rückfrage und ohne spätere Korrektur. Die Nordrichtung wird deshalb beim Import abgefragt und lässt sich am importierten Gebäude nachträglich ändern. Kein Rechenweg.

## 2 Gebaut

### Kern

- **Vorgabe:** `GebaeudeImportProfil.NordwinkelVorgabeGrad` geht über `IfcLeser` an `IfcAbbildBauer.NordwinkelVorgabe` und ersetzt die Drehung aus der Datei; es wird genau einmal gedreht. gbXML: `CADModelAzimuth` wird nie still angewandt, die Drehung folgt erst nach der Eingabe über `GbxmlLeser.NordwinkelVorgeben`.
- **Umrechnung:** Planoberseite α zu Nordwinkel = (360° − α) mod 360°. `GebaeudeImportAblauf.NordwinkelVorgeben` liest aus dem gehaltenen Dateipuffer neu.
- **Meldungen:** `IMP_IFC_PROT_KEIN_NORDEN`, `IMP_GBXML_PROT_KEIN_NORDEN`, `IMP_GBXML_PROT_NORDDREHUNG` (nach N3) und `IMP_GEB_PROT_NORD_VORGABE`.
- **Nachträgliche Drehung:** `GebaeudeImportCtrl.Ausrichtung.cs` mit `LesenAusrichtung` und `AusrichtungAendern` in einem `DbVorgang`: Azimut − (neu − alt), normiert; Azimut NULL bleibt, die Neigung bleibt; ohne Importquelle wird benannt abgelehnt. Raumgrundrisse liegen in Modellkoordinaten und folgen beim Lesen.

### Schema 199

- `EPOS.Kern/Allgemein/Update/NordrichtungSchema.cs`, `SCHRITT = Ak3Schema.SCHRITT + 1` = 199. Neue Spalte `Tab_Importquelle.Nordwinkel_Herkunft` TEXT mit `CHECK IN ('ANNAHME','DATEI','EINGABE')`; die Wertliste kommt aus dem Enum `Nordwinkelherkunft` (`NordwinkelherkunftWerte`). Nachfüllen: `CASE WHEN Nordwinkel_Grad IS NULL THEN 'ANNAHME' ELSE 'DATEI' END`; wiederholbar.
- **Neulesen:** `GebaeudeNeulesen.GespeicherterWinkelGilt` — EINGABE: gespeicherter Winkel; DATEI: frischer Dateiwert; ANNAHME: Dateiwert, falls vorhanden, sonst Annahme.
- **Kopierwege:** Duplikat und Projektpaket kopieren generisch und tragen die Herkunft (mit Tests).
- **Testdatenbank** 198 auf 199: 89 698 304 Byte, LFS-SHA-256 vorher `b4d6a95a…0f61`, nachher `352e8edc…b8d4`; `Tab_Importquelle` ohne Zeilen, `integrity_check` ok. Die Basis `2026-10-07_R41_Erdreichpruefung` bleibt (Nachtrag in [`Referenzlaeufe/LIESMICH.md`](../../../../Referenzlaeufe/LIESMICH.md)).
- **Nebenbefund:** Das Werkzeug hielt Schritt 198 (`Ak3Schema.Vollstaendig`) auf einer Datenbank mit Stand 198 für offen und baute die Sicht `Abfrage_Projektgebaeude` identisch neu — Hinweis an die Sitzung Gebäudesimulation.

### Oberfläche

- **Baustein** `EPOS.UI/Bausteine/Nordrichtungswahl.razor`: Gradfeld 0 ≤ α < 360 (Komma und Punkt, 0,1°), Schnellwahl N bis NW, SVG-Nordpfeil-Vorschau, Zeile mit Dateiwert und Herkunft, Vorschlag mit „Übernehmen“, Warnstil, Sperre.
- **Zuordnungsdialog:** Abschnitt „Nordrichtung“ in `EPOS.UI/Dialoge/Import/GebaeudeImportDialog.razor`.
- **Gebäudedialog:** Abschnitt „Ausrichtung“ in `EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor`; Rückfrage mit Bauteilzahl (`GebaeudeImportCtrl.DrehbareBauteile`) und Drehwinkel, „Nein“ vorbelegt.
- Nur neue CSS-Klassen `epos-nordwahl*`, `epos-gebaeude-ausrichtung` und eine Regel für `forced-colors`; Dialogkopf, Schlussleiste, Raster, Katalogliste, Warnbanner und Hausblatt unberührt (keine Proben nötig).

## 3 Prüfung

- `NordrichtungTests` (Kern, mit Theorie Neulesen je Herkunft), `NordrichtungSchemaTests` 14, `NordrichtungswahlTests` (bunit) 15, `GebaeudeImportNordrichtungHuelleTests` 2.
- Filterläufe: UI 743/743; Kern 957/957 (Oberfläche) bzw. 1 558 bestanden, 4 übersprungen (Schema); Auslieferungsvorlage 61/61; SQL-Dialekt-Prüfer 2 504 Texte, 0 Fundstellen; Windows-Schale 0 Fehler; Referenzlauf der acht CI-Projekte gegen R41 PASS. Kein Rechenweg geändert, keine Einfrierregel berührt.

## 4 Gate 809

⟨GATE809⟩

## 5 Offen

- **Sichtabnahme unter Windows:** Nordpfeil-Vorschau (Größe, Drehung, hell/dunkel, `forced-colors`); Warnstil im Zuordnungsdialog mit und ohne gbXML-Vorschlag; Ladezustand und Esc beim Neulesen; Gebäudedialog mit echtem IFC-Import (Rückfrage, Drehung, Herkunft „eingegeben“, Sperre ohne Import); Schnellwahl-Kürzel auf Englisch.
- **G5-3** mit Farbmodus „Befund“.
- Logbuch-Eintrag (Version beim Anwender erfragen) und Wiki-Upload gebündelt.

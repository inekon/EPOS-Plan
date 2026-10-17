# Folgeangebote „Nach #968“ (16.10.2026)

## Anlass

Der Block „Nach #968“ der Statusdatei nannte acht Folgeangebote (a) bis (h). Sieben davon sind gebaut und vorintegriert (Zweig `vorintegration-nach968`); (c) die Katalogliste ist mit #1145 erledigt.

## Umsetzung und Abnahme

- **Pufferauslegung (e), Status #1140:** `PufferAuslegungCtrl.KombiSchichtung` setzt beim Kombipuffer die Zonenanteile und legt die Heizungsentnahme unter die Brauchwasserzone; Wiki-Quelle „Pufferspeicher auslegen“. Abnahme: Kern 379 und UI 249 (gefiltert), Referenzlauf 37/37.
- **Simulationsablauf-Hilfe (h), Status #1141:** 178 Umlaute statt ae/oe/ue, 3 Anker in `Simulationsablauf.wiki`.
- **Oberfläche (a/d/f), Status #1142:** 40 Ressourcenschlüssel je Sprache (Stufen der Anlagenkopplung, Spaltenköpfe, `PSP_HINWEIS_SCHICHTEN`), `Vdi6007Rechenweg.cs`, Tests; die EER-Spalte der Kältemaschinenliste erscheint erst ab 720 px. Der Designer lief ohne Abweichung.
- **Diagramme (b/g/a), Status #1143:** Dauerlinie nach dem Gesamtbedarf mit gestapelten Erzeugern, Bivalenzmarken in Worten mit Kollisionsprüfung, Überlappungen, Silbentrennung der Brücke. 68 Bilder geändert, 1 neu, 2 Gegenproben; 260 Bilder, 0 Verstöße; `Messlatte_2026-10-16.sha256` (226 Zeilen) ersetzt die vom 14.10. und steht vollständig in der Hashliste.

## Gate

Volles Gate (`gate_linux.sh`) auf dem Stand vor R57: Kern 14 515 bestanden, 5 Rot; UI 9 619 bestanden, 1 Rot; KiKern 553, SpeicherEngine 397, SpeicherPlanung 27; Referenzlauf 37/37 (CSV 1 205/1 205). Die sechs Rotstellen waren eigene Folgen der Diagrammänderung und sind im Commit `bf53c0cc5` behoben: zwei Bausteinvorlagen (`Berichtsvorlage_Bausteine.dotx`, `_en.dotx`) neu erzeugt, Legendentest „9 °C“. Danach Rest-Gate auf dem Stand nach R57: UI 9 622/0, Referenzlauf 38/38 gegen R57 (CSV 1 233/1 233), SQL 3 045 Texte 0 Fundstellen, Konfliktmarker keine. Nach dem Push von „Dialoge und Korrekturen“ (`105df8e61`): Builds 0 Fehler, ChartProben 0 Verstöße, Messlatte unverändert, UI-Filter 494/0.

## (c) Katalogliste bei 1160 px (Status #1145)

- **Befund:** Bei 1160 px Dialogbreite (Fenster 1400 × 820, Wiki-Bilder) schnitt die Kopfleiste des Katalogs Suchfeld und Schaltflächen ab; durch die längeren Spaltenköpfe aus (d) wurde es enger.
- **Lösung:** `epos-ui.css`, Block `@media (min-width: 1280px) and (min-height: 800px)`: die Kopfleiste bricht um (Katalogverwaltung und Übernahme in der zweiten Zeile). Gefundene Spalten der Kältemaschinenliste bei 1160 px: Hersteller, Bezeichner, Nennkälteleistung, EER, Rückkühlart, Herkunft, Art (Typ ausgeblendet).
- **Sollwert:** In `rollbereichprobe.mjs` darf die Katalog-Kopfleiste im Normalmaß zwei Zeilen und höchstens 93 px hoch sein, weil der Umbruch sie bewusst zweizeilig macht; die Kompaktstufe behält ihr Maß.
- **Proben:** Kopfleistenprobe 38/38, Gegenprobe 48 Verstöße; Raster- und Fensterprobe grün mit Gegenproben; Katalogprobe 21/70 wie vorher (darunter N08b, N22b bei 400 × 624, schon vorher rot); Rollbereich rot nur in der Kompaktstufe 1024 × 700/768; UI-Tests 1 805/0 (gefiltert).
- **Offen in der Kompaktstufe:** KA-E-27 (Kältemaschine „Spalten…“ über „Katalogverwaltung…“), Satzfläche der pv-/solarganglinie, gekürzter Suchplatzhalter der Projektauswahl.

## Nachtrag Kompaktstufe (17.10.2026, Status #1167)

Die drei offenen Punkte der Kompaktstufe sind erledigt: KA-E-27 und der Satzkopf der pv-/solarganglinie durch die Sitzung „Anlagendialog“ (`ad70aa9fb`), der gekürzte Suchplatzhalter mit Weg A+B1 (Anwenderentscheid 17.10.2026). In der Katalog-Kopfleiste gibt unter 1280 × 800 zuerst das Suchfeld bis 11.25rem nach, dann die Überschrift bis 3rem, zuletzt die Knöpfe mit Auslassung bis 5.5rem (Pfeil bleibt, title nennt den vollen Text). Platzhalter bei 1024 × 700 und 1024 × 768 178/178 px statt 110/176 px; Kopfleistenprobe mit Kompaktmessung 76/76 grün, Gegenprobe rot; Rollbereichprobe 2 030 Zustände ohne Verstoß.

## Offen

Wiki-Upload der Seite „Pufferspeicher auslegen“, zweier Grundlagenseiten und zweier Diagrammbilder mit dem Sammel-Upload (Entwurf „Nachtrag zu Version 1.2.0.11“ in `Dokumentation/aktuell/Wiki_Update_2026-09-26.md`).

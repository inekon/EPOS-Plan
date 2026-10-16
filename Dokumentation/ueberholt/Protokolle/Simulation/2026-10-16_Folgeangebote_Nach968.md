# Folgeangebote „Nach #968“ (16.10.2026)

## Anlass

Der Block „Nach #968“ der Statusdatei nannte acht Folgeangebote (a) bis (h). Sieben davon sind gebaut und vorintegriert (Zweig `vorintegration-nach968`); (c) die Katalogliste bleibt in Arbeit.

## Umsetzung und Abnahme

- **Pufferauslegung (e), Status #1140:** `PufferAuslegungCtrl.KombiSchichtung` setzt beim Kombipuffer die Zonenanteile und legt die Heizungsentnahme unter die Brauchwasserzone; Wiki-Quelle „Pufferspeicher auslegen“. Abnahme: Kern 379 und UI 249 (gefiltert), Referenzlauf 37/37.
- **Simulationsablauf-Hilfe (h), Status #1141:** 178 Umlaute statt ae/oe/ue, 3 Anker in `Simulationsablauf.wiki`.
- **Oberfläche (a/d/f), Status #1142:** 40 Ressourcenschlüssel je Sprache (Stufen der Anlagenkopplung, Spaltenköpfe, `PSP_HINWEIS_SCHICHTEN`), `Vdi6007Rechenweg.cs`, Tests; die EER-Spalte der Kältemaschinenliste erscheint erst ab 720 px. Der Designer lief ohne Abweichung.
- **Diagramme (b/g/a), Status #1143:** Dauerlinie nach dem Gesamtbedarf mit gestapelten Erzeugern, Bivalenzmarken in Worten mit Kollisionsprüfung, Überlappungen, Silbentrennung der Brücke. 68 Bilder geändert, 1 neu, 2 Gegenproben; 260 Bilder, 0 Verstöße; `Messlatte_2026-10-16.sha256` (226 Zeilen) ersetzt die vom 14.10. und steht vollständig in der Hashliste.

## Gate

Volles Gate (`gate_linux.sh`) auf dem Stand vor R57: Kern 14 515 bestanden, 5 Rot; UI 9 619 bestanden, 1 Rot; KiKern 553, SpeicherEngine 397, SpeicherPlanung 27; Referenzlauf 37/37 (CSV 1 205/1 205). Die sechs Rotstellen waren eigene Folgen der Diagrammänderung und sind im Commit `bf53c0cc5` behoben: zwei Bausteinvorlagen (`Berichtsvorlage_Bausteine.dotx`, `_en.dotx`) neu erzeugt, Legendentest „9 °C“. Danach Rest-Gate auf dem Stand nach R57: UI 9 622/0, Referenzlauf 38/38 gegen R57 (CSV 1 233/1 233), SQL 3 045 Texte 0 Fundstellen, Konfliktmarker keine. Nach dem Push von „Dialoge und Korrekturen“ (`105df8e61`): Builds 0 Fehler, ChartProben 0 Verstöße, Messlatte unverändert, UI-Filter 494/0.

## Offen

(c) Katalogliste; Wiki-Upload der Seite „Pufferspeicher auslegen“, zweier Grundlagenseiten und zweier Diagrammbilder mit dem Sammel-Upload (Entwurf „Nachtrag zu Version 1.2.0.11“ in `Dokumentation/aktuell/Wiki_Update_2026-09-26.md`).

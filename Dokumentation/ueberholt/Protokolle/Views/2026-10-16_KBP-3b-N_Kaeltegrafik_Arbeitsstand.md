# KBP-3b-N — Kältegrafik: Arbeitsstand, Kalenderwoche, Tagesarten

Sitzung Dialoge und Korrekturen (Übersicht Berichte & Kosten), 16.10.2026, Zweig `kbp3bn`. Fortsetzung von
[`KBP-2 bis 3b`](2026-10-16_KBP-2_bis_3b_Kaeltebedarfsprofile.md); Statuszeile #1107.

## Stand

- `ProfilQuelle.Arbeitsstaende` (Monatswerte, Wochenwerte, Kalender) ersetzt in der Kette Kopfsatz, Typprofil und Kalender; der Referenzlauf setzt sie nie.
- Blatt Woche: Kalenderwoche nach DIN EN ISO 8601 im Gemeinjahr-Raster, Vorgabe die Woche der Spitze; Tagesarten als Hintergrundstreifen `--epos-kalb-*` mit Legende.
- Fensterprobe: Fall `kaeltegrafik` und `kaelteprofile` grün mit roter Gegenprobe; die zwei Gegenproben „Fuß mit negativem Rand über dem Nachlauf“ im Fall `dubletten` bestehen auf dem Container schon vor der Änderung.

## Gate

Merge mit `origin/ios_migration_september` (`283fe7a3d`) ohne Konflikt. Referenzlauf 37/37 PASS gegen R55, CSV byte-gleich 1 205/1 205; EPOS.UI.Tests 9 575/0, KiKern 553/0, SpeicherEngine 397/0, SpeicherPlanung 27/0 (1 übersprungen), SQL-Prüfer 3 002 Texte ohne Fundstelle; gefilterte Kern-Tests 2 892/0; Kern-Mappe und Windows-Schale 0 Fehler.

# Protokoll BK-9 — Vorschauzählung der Teilkataloge

Auftrag BK-9, Nachzug zu [BK-8c](2026-10-16_BK-8c_Wiederimport_Vergleich.md) nach der Windows-Prüfung des
Bauteilkatalog-Imports. Arbeitsort Worktree `zaehl`, Zweig `zaehl`, Basis `61ee18040`. Kein Schemaschritt; Schreibweg,
Abschnittsprüfung und U-Wert bleiben unverändert.

## Befund (nur Zählungen)

- Vorschau „Aufbauten mit Teilkatalog“: 7 913; nach der Übernahme in der Datenbank: 7 906.
- Die Differenz von 7 sind genau die 7 verworfenen Aufbauten („inhomogen: Abschnitt ohne Flächenanteil“).

## Ursache

`BauteilkatalogImport.Planen` bildete die Teilkatalog-Zählungen (`AufbautenMitTeilkatalog`,
`AufbautenMitMehrerenTeilkatalogen`, `AufbautenOhneTeilkatalog`, die Zahl `Teilkataloge`) samt den beiden Protokollzeilen
„Teilkataloge: …“ und „Teilkataloge in Katalog.csv: …“ direkt nach dem Einlesen der Aufbauten, also vor
`AbschnittePruefen`, das inhomogene Aufbauten samt Abschnitten und Schichten aus dem Plan nimmt.

Die Baustoff-Zählungen (`BaustoffeMitTeilkatalog`, `BaustoffeMitMehrerenTeilkatalogen`, `BaustoffeOhneTeilkatalog`) waren
nicht betroffen: Baustoffe werden nur beim Einlesen abgelehnt (ohne Kennung, Kennung doppelt, ohne Namen, λ-Prüfung) und
dann gar nicht erst in den Plan aufgenommen; nach dem Einlesen entfernt kein Schritt einen Baustoff. Die Zahl der
Teilkataloge konnte dagegen zu hoch sein, wenn ein Teilkatalog nur an verworfenen Aufbauten hing.

## Lösung

Der Zählblock samt Protokollzeilen steht jetzt unmittelbar nach `AbschnittePruefen`. Alle Teilkatalog-Zählungen der
Vorschau beziehen sich damit auf die Sätze, die übernommen werden; Vorschau und Übernahme stimmen überein.

## Tests

- Neu in `EPOS.Kern.Tests/BauteilkatalogImportTests`:
  `Die_Vorschau_zaehlt_Teilkataloge_erst_nach_dem_Verwerfen_inhomogener_Aufbauten` — synthetischer Export mit
  `Katalog_Zuordnung.csv`, der Aufbau A5 (Teilkatalog B) hat zwei Abschnitte ohne Flächenanteil und wird verworfen;
  Vorschau 2 Aufbauten mit Teilkatalog (vorher 3), nach der Übernahme 2 Aufbauten mit Teilkatalog in der Quellenangabe.
  Gegenprobe ohne Änderung rot (erwartet 2, gezählt 3).
- `EPOS.Kern.Tests` mit Filter Bauteilkatalog/Teilkatalog/Bauteilaufbau/InhomogeneSchichten: 113 von 113 grün;
  `EPOS.UI.Tests` mit Filter Bauteilkatalog: 9 von 9 grün; `WP-Plan.Kern.slnf` baut mit 0 Fehlern.
- Kein Referenzlauf: geändert ist allein die Zählung der Importvorschau, nicht der Rechenweg der Simulation.

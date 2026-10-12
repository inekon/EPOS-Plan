# Protokoll BK-1 — Editoren ohne Schema (Baustoff, Bauteilaufbau, Bauteil)

Welle BK-1 aus dem [Entwurf Bauteilkatalog](../../../aktuell/Gebaeudesimulation/2026-10-12_Entwurf_Bauteilkatalog_Hottgenroth.md)
(Abschnitte 1.4, 2.2, 6a und 7). Ohne Schemaschritt, Rechenweg unberührt, kein SQL.
Ausgangsstand `953cffc86`, Ergebnis-Commit `12da0b4bd`.

## Befund

- Baustoffliste: c_p hatte den Rang „bei Platz“ und fiel im schmalen Fenster weg.
- Baustoff-Editor: Gruppe war ein freies Textfeld; keine Bänder je Gruppe, nur die harten Bänder des Kerns.
- Aufbau-Editor: R_si und R_se standen nur in der Herleitungszeile, R_T gar nicht; die Richtung der Schichtfolge stand
  nur als `aria-label` des Rasters, nicht sichtbar.
- Bauteildialog: keine Wärmekapazität neben dem U-Wert.
- Die Testdatenbank führt elf Stoffgruppen (Abdichtungen, Beton, Bodenbeläge, Dämmstoffe, Erdreich, Estriche, Holz und
  Holzwerkstoffe, Mauerwerk, Metalle und Glas, Platten, Putze und Mörtel); der Saat-Grundstock nutzt zehn davon.

## Ergebnis

1. **Kern `Baustoffgruppen`** (`EPOS.Kern/Allgemein/Katalog/Baustoffgruppen.cs`): die elf Gruppen plus „Sonstige“ in
   fester Reihenfolge, je Gruppe ein Band für λ, ρ und c_p (DIN 4108-4 / DIN EN ISO 10456 mit Rand, jedes Band
   innerhalb der harten Bänder); „Sonstige“ ohne Band. `Hinweise`/`Hinweis` nennen jeden Wert außerhalb seines Bandes
   und eine freie Gruppe; nie eine Sperre, `BaustoffCtrl.Pruefen` bleibt unverändert. Anzeigenamen je Sprache über
   `BST_GRUPPE_*`, gespeichert bleibt der deutsche Gruppentext.
2. **Baustoffkatalog:** c_p hat den Rang „immer“ (wie λ), ρ bleibt „bei Platz“. Die Gruppe ist ein `Auswahlfeld` der
   festen Gruppen mit leerer Zeile „(keine Gruppe)“; ein Altsatz mit freiem Text erscheint als eigener Eintrag
   „… (freie Angabe)“ samt Hinweis und ist unverändert speicherbar. Der Bandhinweis steht als leise
   `Herleitungszeile` unter den Kenndaten. Stammblattkopf und Lesemodus zeigen den übersetzten Gruppennamen.
3. **Bauteilaufbau:** `BauteilaufbauKennwerte.RT_M2KW` (R_si + R + R_se, aus dem vorhandenen Kernweg) über Hülle und
   DTO in den Summenfuß; der Fuß zeigt R_si, R, R_se, R_T | U, C, T_BT. Über dem Schichtraster steht die Zeile
   „Schichtfolge innen → außen: Schicht 1 liegt raumseitig, die letzte Schicht außen.“
4. **Bauteildialog:** Lesewert „Wärmekapazität C“ neben dem U-Wert (nicht bei Fenstern), aus dem Summenfuß der Hülle;
   ohne Aufbau „aus der Bauweise“, bei unvollständigem Aufbau „–“.
5. **KI-Feldkarte:** Feld `gruppe` des Baustoffkatalogs ist `Wahl` (Einträge `GruppeWahl` der Sicht aus dem Kern),
   Erläuterung in beiden Sprachen nachgezogen. Zählwache unverändert (Textfeld durch Auswahlfeld ersetzt, der neue
   Lesewert ist `NurLesen`).
6. **Naht BK-2:** Kommentar an den Spaltenlisten von `Katalogfilterprofil.FuerBaustoff` und `FuerBauteilaufbau` —
   Klasse, Herkunftsmarke „Import“ und das Kennzeichen „für die Gebäudesimulation meist nicht geeignet“ treten dort
   hinzu; keine Spalte in BK-1.

22 neue Ressourcenschlüssel je Sprache, `Resource.Designer.cs` neu geschrieben (wiederholbar).

## Dateien

- Kern: `Allgemein/Katalog/Baustoffgruppen.cs` (neu), `Allgemein/Katalog/Katalogfilterprofil.cs`,
  `Model/BauteilaufbauKennwerte.cs`, `Allgemein/KI/Dialoge/KiDialoge.cs`, `MyResource/Resource(.en-US).resx`,
  `MyResource/Resource.Designer.cs`
- Hülle: `EPOS.UI.Daten/Bedarf/BauteilaufbauHuelle.cs`
- Oberfläche: `Dialoge/Bedarf/BaustoffKatalogDialog.razor`, `BaustoffKatalogTexte.cs`, `BaustoffKatalogKiSicht.cs`,
  `BauteilschichtenFelder.razor`, `BauteilschichtenTexte.cs`, `BauteilaufbauDaten.cs`, `BauteilDialog.razor`,
  `GebaeudeZonenTexte.cs`, `wwwroot/epos-ui.css` (Regel `.epos-bauteilschicht-richtung`)
- Tests: `EPOS.Kern.Tests/BaustoffgruppenTests.cs` (neu, 8 Fälle), `BauteilaufbauVerwaltungTests.cs` (+1),
  `EPOS.UI.Tests/Dialoge/BaustoffKatalogDialogTests.cs` (+5, ein Fall auf die Auswahl umgestellt),
  `BauteilaufbauDialogTests.cs` (+1, Summenfuß-Fall erweitert), `BauteilDialogTests.cs` (+1)

## Nachweis

| Prüfung | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler |
| Windows-Schale `-p:EnableWindowsTargeting=true` | 0 Fehler |
| `EPOS.Kern.Tests`, Filter Baustoff, Bauteilaufbau, Bauteilschicht, Bauteil, Steckbrief, Ki, Wache, Ressource, Stilblatt, Katalogfilterprofil, Spaltenraenge | 1 518 bestanden, 1 übersprungen, 0 rot |
| `EPOS.UI.Tests`, derselbe Filter | 1 024 bestanden, 0 rot |
| `Proben/Rasterprobe/rasterprobe.mjs` | alle 29 Fälle erfüllen die Sollwerte (RC 0) |
| `Proben/Rasterprobe/katalogprobe.mjs` (Information) | RC 1, „8 von 66 Fällen mit Überlagerung“; die Probe führt keinen Baustofffall, die Fälle betreffen Kessel, BHKW, Wärmepumpe und Klimadaten — nicht dieser Welle zuzuordnen, gegen den Ausgangsstand nicht gegengeprüft |
| Referenzlauf | nicht nötig, Rechenweg unberührt |
| SQL-Dialekt-Prüfer | nicht nötig, keine SQL-Anweisung neu oder geändert |

## Punkte der Windows-Sichtabnahme

1. Administration › Gebäude › Baustoffe im schmalen Fenster (unter 900 px): Spalte „Wärmekapazität“ bleibt stehen,
   „Rohdichte“ weicht.
2. Stammblatt eines eigenen Satzes: Gruppe als Klappliste mit „(keine Gruppe)“ und den zwölf Gruppen; Wechsel zählt
   als ein geändertes Feld.
3. λ eines Dämmstoffs auf 0,8 setzen: leise Hinweiszeile unter den Kenndaten, Speichern geht.
4. Satz mit freier Gruppe (z. B. per Duplizieren und Altsatz): Eintrag „… (freie Angabe)“ und Hinweis.
5. Bauteilaufbauten: Richtungszeile über dem Raster; Summenfuß in zwei Reihen (R_si, R, R_se, R_T | U, C, T_BT),
   Umbruch und Lesbarkeit im schmalen Fenster.
6. Bauteildialog (Gebäudeeditor): „Wärmekapazität C“ neben dem U-Wert; ohne Aufbau „aus der Bauweise“, nach Wahl eines
   Aufbaus der Wert in kJ/(m²K); bei Fenster kein Feld.
7. Dasselbe in Englisch (Gruppennamen, Hinweis, Richtung, Summenfuß).

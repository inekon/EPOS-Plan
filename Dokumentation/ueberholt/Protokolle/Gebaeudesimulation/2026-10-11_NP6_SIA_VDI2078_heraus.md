# Protokoll NP6 — Kategorien SIA 2024 und VDI 2078 entfallen, Wohnprofile der DIN

**11.10.2026 · Sitzung Gebäudesimulation · Stufe NP6 der Nutzungsprofile (E129).** Grundlage:
[Konzept Nutzungsprofile](../../../aktuell/Konzept_Nutzungsprofile_EPOS-Plan.md) 5.2, 5.3, NP-F4; Schemaschritt 219
hängt an 218 (`KaelteArtSchema`, KWN-0).

## Entscheid

E129 (Anwender, 11.10.2026): „nehme sia und vdi 2078 heraus“ und „die DIN 18599-10 Profile für Wohnen fehlen in der
Auswahl“. Begründung: Die beiden Kategorien waren leer und schreibgeschützt, ein Anwender konnte in sie kein Profil
eintragen; VDI 2078 definiert keine Nutzungsprofile je Raumtyp, sie verweist für innere Lasten auf DIN V 18599-10
(Befund K, Abschnitt 3); eigene Werte bleiben über eigene Kategorien möglich.

## Schemaschritt 219 (`RaumnutzungNormkategorienSchema`)

- Ausgelieferte Kategorien (`ReadOnly = 1`) der Art `SIA_2024` oder `VDI_2078`: ohne Profil gelöscht; mit Profil Art
  `EIGEN`, `ReadOnly = 0`, im Migrationsprotokoll benannt — Profile und Zuordnungen bleiben.
- Kategorie DIN (`DIN_V_18599_10`, ReadOnly): „Wohngebäude – Einfamilienhaus“ und „Wohngebäude – Mehrfamilienhaus“,
  Nummer NULL, ohne Kennwert, Zeilenbild und Stunden, `ReadOnly = 1`, Beschreibung „Nutzungsrandbedingungen für
  Wohngebäude nach DIN/TS 18599-10:2025-10, Tabelle 5; ohne Nummer in der Norm“; angelegt nur, wenn die Kategorie kein
  Profil gleichen Namens trägt (ohne Unterschied der Schreibung). Namen in den Konstanten `WOHNEN_EFH`, `WOHNEN_MFH`
  (Bestätigung durch den Anwender steht aus, Q47).
- Kein Tabellenneubau: Der `CHECK` auf `Art` behält beide Werte als Bestand. Schritte 189, 190 und die Saat unverändert;
  `RaumnutzungSchema.OffeneSaat` zählt die entfallenen Kategorien nicht mehr.
- Leser: `SchemaMigration`, `Werkzeuge/Testdatenbankschema` (Testdatenbank 218 → 219), Testvorrichtung,
  `SchemaStand.Zielversion` 219, `Paketanhebung` (Art Katalog — ein Paket führt keinen Katalog der Nutzungsprofile).
- Zuordnung `DIN_NUMMER` 70/71 bleibt auf dem EPOS-Muster Wohnen (NP-F13).

## Oberfläche, Hülle, Werkzeug

- `RaumnutzungArt.Sia2024`/`Vdi2078`, die Texte `ArtSia`/`ArtVdi` und die Ressourcenschlüssel `RNP_LBL_ART_SIA`,
  `RNP_LBL_ART_VDI` (beide Sprachen, Designer neu erzeugt) entfallen; `ARTEN_NORM` führt nur noch DIN.
- Sortierung: Profile ohne Nummer stehen in jeder Auswahl am Ende ihrer Kategorie (`RaumnutzungCtrl`, `Nummer IS NULL`
  zuletzt) — Blatt „Nutzungsprofile“, Zonenbaum (`optgroup`), „Nutzungsprofil übernehmen…“ übernehmen diese Ordnung.
- Auslieferungsvorlage: Prüfbericht nennt `RaumnutzungNormkategorienSchema.Offen()`; Katalogpaket und Katalogfassung
  unberührt (der Katalog der Nutzungsprofile ist nicht Teil des Pakets).

## Nachweis

Kern-Filter und Windows-Schale bauen mit 0 Fehlern; Tests mit Filter grün (Zahlen im Bericht der Sitzung); SQL-Dialekt-
Prüfer ohne Fundstelle. Rechenweg unberührt, kein Referenzlauf.

## Logbuch-Entwurf

- Im Katalog der Nutzungsprofile entfallen die leeren Kategorien „SIA 2024“ und „VDI 2078“; Werte aus weiteren Normen
  führen Sie in eigenen Kategorien.
- Die Kategorie „DIN/TS 18599-10“ enthält die Wohngebäude (Einfamilienhaus, Mehrfamilienhaus) ohne Werte.

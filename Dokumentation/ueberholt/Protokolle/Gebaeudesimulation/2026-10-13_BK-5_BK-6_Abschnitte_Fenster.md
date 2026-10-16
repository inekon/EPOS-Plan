# Protokoll BK-5/BK-6 — Abschnitte inhomogener Schichten und Fenster am Aufbau

Wellen BK-5 und BK-6 aus dem [Entwurf Bauteilkatalog](../../../aktuell/Gebaeudesimulation/2026-10-12_Entwurf_Bauteilkatalog_Hottgenroth.md)
(Abschnitte 6a und 7; Entscheid E132 F7), Punkt 10 der
[Übergabe](../../../aktuell/Gebaeudesimulation/2026-10-12_Uebergabe_Gebaeudesimulation_Kontowechsel.md). Arbeitsort Worktree
`bk5`, Zweig `bk5`. Schemaschritt `BauteilkatalogAbschnittSchema`, angemeldet als **229**.

## Befund

- Die Tabellen der Abschnitte stehen seit Schritt 221 (`BauteilkatalogSchema`): `Tab_Bauteilabschnitt(_STAMM)` mit Nummer,
  Flächenanteil 0…1, Text und Quellkennung, an der Schicht der Verweis `ID_Abschnitt` (Kaskade). Der Flächenanteil gehört dem
  Abschnitt (Streifen), nicht jeder seiner Schichten — eine zweite Spalte `Flaechenanteil` an der Schicht entfällt.
- Die Fensterfelder `Ug`, `Uf`, `g_Wert`, `Rahmenanteil` stehen am Aufbau seit Schritt 221. Es fehlte allein ψ des Glasrands
  für U_w nach DIN EN ISO 10077-1; `Psi_Gesamt/System/Geometrie` sind ψ des Einbaus, nicht des Glasrands.
- Der Paketweg ließ den Abschnittsverweis der Schicht als Nebenspalte leer; `Tab_Bauteilabschnitt_STAMM` stand benannt in den
  Ausnahmen des Registers.
- Der Merge von origin in `bk5` hatte den Schritt der Schale (`Schritt_BauteilkatalogAbschnitt` in `SchemaMigration.cs`) mit
  den Schritten 225 und 227 verschränkt; der Kern-Filter sieht die Datei nicht, erst der Bau der Windows-Schale meldete drei
  Syntaxfehler.

## Ergebnis

- **Schemaschritt** `BauteilkatalogAbschnittSchema`: `Psi_Glasrand REAL` (NULL oder ≥ 0) an `Tab_Bauteilaufbau(_STAMM)`;
  wiederholbar, kein DML, ergebnisneutral. Vier Leser: `SchemaMigration` (Schale), `Werkzeuge/Testdatenbankschema`, die
  Testvorrichtung, die Paketanhebung (Art Ddl). Die Klasse hängt vorläufig an `BrennstoffpreiseTraegerkarteSchema.SCHRITT + 1`
  (227 + 1 = 228); beim Merge hängt sie an 228 `GeraetekostenEntfallSchema` und wird 229.
- **Rechenweg** `InhomogeneSchichten`: R_T als Mittel aus oberem und unterem Grenzwert nach DIN EN ISO 6946, 6.7, Ersatzschichten
  für die Reduktion; `Fensterkennwerte.Uw` nach DIN EN ISO 10077-1 (Glasrandlänge genähert). Ein Aufbau ohne Abschnitt nimmt
  den Weg unverändert.
- **Prüfung:** gleiche Abschnitte je Ebene, jeder Anteil > 0, Summe 1 ± 0,01 — in den Kennwerten (Grund statt Zahl) und im
  Speicherweg (`BauteilaufbauCtrl.EingabePruefen`).
- **Projektkopie** `CopyFromStamm` nimmt die Abschnitte mit (Nummer → neue ID); die Schichten der Kopie zeigen auf die
  Abschnitte der Kopie.
- **Katalogpaket:** `Tab_Bauteilabschnitt_STAMM` reist als Kind des Aufbaus vor den Schichten; die Schicht trägt `ID_Abschnitt`
  als Nummer des Abschnitts unter demselben Aufbau (`Katalogverweis.Eigentuemerspalte`). Ein Aufbau ohne Abschnitt behält
  seine Prüfsumme (`Katalogkind.NurMitZeilen`); die Ausnahme im Register entfällt (Ausnahmen 16 → 15).
- **Import** (`EPOS.Kern/Import/Bauteilkatalog/`): Aufbauten mit Abschnitten werden als Ebenen gelesen statt ausgelassen;
  durchgehende Schicht neben Abschnitten, ungleiche Schichtzahl, Abschnitt ohne Anteil oder eine Summe außerhalb 1 ± 0,01
  verwerfen den Aufbau benannt. Die Fensterdatei liefert ψ des Glasrands.
- **Oberfläche:** Schichtenraster mit Abschnittsnummer, Flächenanteil und Ebenensumme; Summenfuß mit R'_T, R''_T und dem Mittel
  aus dem Kern; Fensterteil (U_g, g, Rahmenanteil, U_f, ψ_g, U_w) im Aufbaueditor nur bei Fenster und Vorhangfassade; der
  Bauteildialog übernimmt g, Rahmenanteil und U_w beim Zuweisen eines Fensteraufbaus.
- **Schale:** der Schritt aus der origin-Fassung plus den drei Stücken des Schritts neu zusammengesetzt; Bau 0 Fehler.

## Nachweis

- Kern-Filter Release 0 Fehler; Windows-Schale (Linux, `EnableWindowsTargeting`) 0 Fehler.
- `EPOS.Kern.Tests` Filter Bauteil|Baustoff|Schema|Katalogpaket|Katalogabgleich|Auslieferung|Ids|Dokumentation|Repository
  1 510 / 0 rot (1 übersprungen), `EPOS.UI.Tests` Filter Bauteil|Baustoff|KiMaskenabdeckung|VorlagenfeldStilblatt 174 / 0 rot.
- Neue Tests: Grenzwerte gegen Handrechnung, Gegenprobe ohne Abschnitte, Anteilsumme ≠ 1 in Kennwerten und Speicherprüfung
  abgelehnt, U_w samt Fenster ohne Schichten, Projektkopie mit Abschnitten (gleiche Kennwerte), Paketweg der Abschnitte samt
  Prüfsummen-Gegenprobe, Import-Gegenprobe der Anteilsumme, drei Dialogtests (Abschnitt daneben, Grenzwerte im Fuß, Fensterteil).
- SQL-Prüfer 2 983 Texte, 0 Fundstellen; Designer wiederholbar (+0).
- Testdatenbank im Worktree auf 228 gehoben (vorläufige Nummer, nach dem Umhängen 229): `Psi_Glasrand` an beiden
  Aufbautabellen, leer; `Tab_Bauteilaufbau_STAMM` 9 Sätze, `Tab_Bauteilschicht_STAMM` 31 Zeilen, davon 0 mit `ID_Abschnitt`;
  `Tab_Bauteilabschnitt(_STAMM)` je 0; kein Fensteraufbau mit Fensterwerten. Nicht committet.
- Kein Referenzprojekt führt einen Aufbau, Abschnitte oder Fensterwerte (`Kein_Referenzprojekt_fuehrt_Abschnitte_oder_Fensterwerte`);
  der Rechenweg der Referenzprojekte ist nicht berührt, der Referenzlauf bleibt byte-gleich (Lauf durch die Orchestrierung).

## Offen

- Umhängen der Kette an 228 `GeraetekostenEntfallSchema` und Commit der gehobenen Testdatenbank (Orchestrierung beim Merge).
- Der Assistent meldet die neuen Eingaben (fünf Fensterwerte, Abschnitt und Flächenanteil) noch nicht an; die Maskenwache
  führt sie mit Grund unter den Eingabestellen.
- BK-7 (Teilkatalognamen aus `Katalog_Inhalt.csv`, Hilfeschlüssel des Importdialogs), Wiki-Quellen.

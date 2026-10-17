# Protokoll KU3-4d — Kältestromabrechnung, Stempeltrigger, Kältespeicher in Bericht und Navigator (04.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle KU3-4d (E67, E68), ein Opus-Auftrag, Commits `8b9558d`, `212dc29`, `7449229`, `73797b4`, `d3786e2`, Merge `bf0c0ac`, Testdatenbank 184 `d8b186b`. Statuszeile #721.
**Entscheid:** keiner neu beim Anwender. Schemaschritt 184, Basis R35 unverändert, Referenzlauf byte-gleich.

## 1 Auftrag

Die Kältemaschine (KU3-4a) im Kältestrom abrechnen wie ein Wärmepumpen-Modul, den Kostenstempel an `Tab_Energieanlagen` auf die Kältemaschine ausdehnen, die Projektkopie teilbar machen und den Kältespeicher (KU3-5) in Bericht und Navigator sichtbar machen.

## 2 Vorgehen

Ein Opus-Auftrag in fünf Commits (Schema 184, Abrechnung und Lauf, Projektkopie, Bericht und Navigator, Tests); Merge nach `bf0c0ac`. Die Testdatenbank wurde von der Orchestrierung beim Merge mit KU3-4d und MZ-Rest in zwei Commits auf 184 und 185 gehoben (Statuszeile #722).

## 3 Ergebnis

- **Schritt 184 `KaeltestromabrechnungSchema`:** `Tab_ErgebnisKaeltemaschine` bekommt `Kaeltestrom_Netzbezug_MWh` (REAL), `Kuehl_ID_Carrier` (INTEGER), `Kuehl_EigenerZaehler` (INTEGER, CHECK 0/1) und `Stromspitze_kW` (REAL). Der Trigger `trg_Kostenstempel_Tab_Energieanlagen_U` wird neu angelegt, sobald seine Spaltenliste `Kaeltemaschine_Anzahl` und `ID_Kaeltemaschine` noch nicht enthält; an `Tab_Kaeltemaschine` liegt kein Trigger (wie an `Tab_WP`). Keine Saat, ergebnisneutral. Registriert in `SchemaMigration`, im Werkzeug Testdatenbankschema, in der Testvorrichtung und in der Paketanhebung (Ddl).
- **Abrechnung:** die Kältemaschine rechnet wie ein Wärmepumpen-Modul und steht in `Kaeltestromabrechnung.Quellen` nach den Modulen; der Kühlträger zählt nur > 0; anteilig oder eigener Zähler mit Grund- und Leistungspreis; Schlüssel `-1-i`. `SimulationRunner` schreibt Netzbezug, Träger, Zähler und Stundenspitze, `ErgebnisCtrl` nur bei vorhandenen Spalten. Ohne Zeitreihen nimmt `KostenEmissionRechner` die gespeicherte Jahresspitze. `EndenergieAufloeser` nimmt den Träger aus der Ergebniszeile (Lauf nach 184), sonst aus der Anlagenzeile. `SzenarioMengen` skaliert Mengen, Netzbezug und Spitze.
- **Projektkopie:** zwei Anlagen aus demselben Katalogsatz teilen eine Kopie in `Tab_Kaeltemaschine`; Vorlauf und Hilfsstrom je Gerät an der Kopie, Anzahl je Anlagenzeile; die Kopie wird erst ohne Nutzer gelöscht.
- **Bericht und Navigator:** neue Tafel `tabelle.kaeltespeicher` (Kapazität, Ladung, Entladung, Wärmeeintrag, Vollzyklen) mit Abschnitt „Kältespeicher“ in der Projektbeschreibung (entfällt ohne Kältespeicher); Speichertemperaturen mit Zusatz „(Kältespeicher)“; die Kälteerzeuger-Tafel zeigt Netzbezug und Träger der Kältemaschine („—“ vor 184); Navigator, Ergebnisanzeige und Füllstandsexport lesen `SpeicherSamtKaelte()`; Meldungen je Anlagenzeile. Vorlagenfeldkatalog v12, Bausteinvorlagen `Berichtsvorlage_Bausteine(.dotx/_en.dotx)` neu erzeugt.
- **Tests:** `KaeltemaschineAbrechnungTests` (neu).

## 4 Entscheide der Orchestrierung

- Beim Merge mit MZ-Rest: Konflikte in sechs Registrierungsdateien gelöst, beide Schritte registriert (184 vor 185).

## 5 Nachweise

Kern-Filter 0 Fehler; Kern-Tests Auftragsfilter 1 918/1 929 grün, nach Nachbesserung alle grün; UI 1 053/1 053; Auslieferungstests 47/47 mit Datenbank 184; SQL-Dialekt 2 345 Texte, 0 Fundstellen; Schale 0 Fehler; Referenzlauf der 8 CI-Projekte PASS gegen R35, 262 Dateien byte-gleich. Gate 722 (Stand `2481d13`): Zahlen in Statuszeile #722.

## 6 Offenes

- KU3-4b: Referenzprojekt mit Kältemaschine und Kältespeicher, Einfrierregel, Basis R36, Wiki Kühlung, Hilfeanker, Logbuch.
- Die Schemastand-Wache war im breiten Lauf nicht reihenfest; nicht untersucht.
- Gate-Zahlen und CI-Kennung nachtragen.

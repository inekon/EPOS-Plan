# Protokoll KU3-1 — Kältemaschine: Schema und Katalog (04.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle KU3-1 (E67, E68), zwei Opus-Aufträge (88 + 114 Aufrufe), Commits `2ad1fb2`, `7197ba9`, `c4ee69a`, `3a3db7d`, `02de20e`, `7a728d4`, `ec5bfaf`, `24c40cc`, Merge `be33936`. Statuszeile #713.
**Entscheid:** keiner neu beim Anwender; ein Entscheid der Orchestrierung (Abschnitt 4). Schemaschritt 182, Basis unverändert, Referenzlauf nicht betroffen.

## 1 Auftrag

Die Kältemaschine als eigener Erzeugertyp anlegen, soweit sie Schema, Katalog und Verwaltung betrifft (Kühlkonzept 5.3, Plan Abschnitt 6): Tabellen, Auslieferungssaat, Katalogregister, Verwaltungsdialog. Der Rechenweg folgt in KU3-2.

## 2 Vorgehen

Zwei Opus-Aufträge: Schema, Register und Kern; danach Oberfläche, Hülle, Menü, KI-Maske und Ressourcen. Ein konfliktfreier Merge; Statuszeile und Papiere aus der Orchestrierung.

## 3 Ergebnis

- **Schema 182** `KaeltemaschineSchema` (hängt an 181): `Tab_Kaeltemaschine_STAMM` (17 Spalten, u. a. `Nennkaelteleistung_kW` > 0, `Nenn_EER` > 0, `Rueckkuehlart` mit CHECK, `Mindestteillast_Prozent` 0–100, `Hilfsstrom_Rueckkuehlung_kW` >= 0 mit NULL = im EER enthalten), `Tab_Kaeltemaschine` (Projektkopie, `ID_Projekt` Kaskade, `ID_Stamm` SET NULL), `Tab_Kenndaten_Kaeltemaschine(_STAMM)` (UNIQUE über das Temperaturpaar, Kaskade); alle STRICT. `SchemaStand.Zielversion` 182.
- **Saat:** drei neutrale Geräte mit `ReadOnly = 1` und je sechs Kennlinienpunkten (Rückkühlung 25/35/45 °C, Kaltwasser 6/12 °C): 50 kW luftgekühlt (R32, EER 3,0, 25 %), 200 kW wassergekühlt mit Trockenkühler (R513A, EER 4,0, 20 %, 6 kW), 500 kW wassergekühlt mit Nasskühler (R1234ze, EER 4,3, 15 %, 10 kW).
- **Register:** `Katalogfassung` Stufe 3 (Kürzel `KM`, Kennlinie als Kind), `KatalogRegistry` `KAELTEMASCHINE`; Duplizieren, Export/Import (`ID_Stamm` reist nicht, Nachtrag über den Bezeichner), Katalogkopie, -abgleich, -paket über das Register; Auslieferungsvorlage STRICT-Zahl 163 → 167.
- **Kern:** `KaeltemaschineModel`, `KaeltemaschineKenndatenModel`, `KaeltemaschineStammCtrl`, `KaeltemaschineCtrl.AusKatalogUebernehmen`, `DbWerte.ERZEUGER_KAELTEMASCHINE` und `KOSTEN_KOMPONENTE_KAELTEMASCHINE`.
- **Oberfläche:** `KaeltemaschineKatalogDialog.razor` (Gerüst der Baustoffverwaltung; Stammblatt mit zwölf Kenndaten, Kennlinie als `Zeilenraster`, Auslieferungssätze gesperrt), Hülle `KaeltemaschineKatalogHuelle`, Menüpunkt `MenuItem_Kaeltemaschinen`, Seitenschlüssel `KAELTEMASCHINE_KATALOG`, `Anlagenart.Kaeltemaschine` im Katalogfilterprofil (neun Arten), KI-Maske mit 17 Feldern, 63 + 7 Ressourcen in beiden Sprachen.
- **Zähler nachgezogen:** Menüband 67 Punkte/53 Handlungen, Katalogeinstieg 4/13, Spaltenränge 15, KI-Katalog 91, Katalogfilterstand 9, Registry 26, Stammtabellen 46.
- **Tests:** `KaeltemaschineSchemaTests` (8), `KaeltemaschineKatalogDialogTests` (13), `KaeltemaschineKatalogHuelleTests` (2); Kern-Filter 1 816 und 283/283, UI 1 420/1 420 und 640/640, breiter 3 004/3 004; Auslieferungsvorlage mit Kopie 182 46/47; SQL-Dialekt 2 318/0; Schale 0 Fehler. Gate-Zahlen trägt die Orchestrierung in der Statuszeile nach.

## 4 Entscheide der Orchestrierung

- Die Zeile in `Tab_KostenKomponente` entsteht erst mit der Wirtschaftlichkeit in KU3-4; der Wert `KOSTEN_KOMPONENTE_KAELTEMASCHINE` steht schon.
- `ParameterVerwendung.AlleArten` führt die Kältemaschine erst mit dem Rechenweg (KU3-2); Hilfeanker, Wiki und `KiChatKontext` folgen mit KU3-4.

## 5 Offenes

- **Testdatenbank:** nicht committet; die Repo-Datei steht auf 180, weil die Datenbank 181 (AK1z) beim Anwender liegt. Nachzug 181 → 182 mit `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite` (wiederholbar, sät die Geräte).
- **iOS:** `IosProjektQuelle.cs` hat eine neue Methode nach dem Muster der Baustoffe, nur gelesen geprüft; Bau nur auf macOS, kein iOS-Lauf ohne Freigabe.
- **Beim Anwender:** Testdatenbank 181 pushen, danach 182 aus dem Bundle; Sichtabnahme der Verwaltung unter Windows.
- **Folgewellen:** KU3-2 Rechenweg (läuft), KU3-3 Kühlung je Zone, KU3-4 Erzeugerdialog, Bericht, Referenz, KU3-5 Kältespeicher.

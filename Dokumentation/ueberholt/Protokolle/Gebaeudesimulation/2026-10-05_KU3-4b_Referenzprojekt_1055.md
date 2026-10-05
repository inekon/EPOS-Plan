# Protokoll KU3-4b — Referenzprojekt 1055 Kältemaschine mit Kältespeicher (05.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle KU3-4b (E67, E68), ein Opus-Auftrag und ein Nachzug, Commits `1fc3f30`, `821dafd`, `54b6df7`, `4df54a5`, `c754255`, `6ebcf5b`, Merge `7b79b22`, Hinweistext `2e287c6`. Statuszeile #727.
**Entscheid:** keiner neu beim Anwender. Kein Schemaschritt; neue Einfrierregel „gesäte Kältemaschinendaten“, Basis R36 (eingefroren in einem parallelen Auftrag).

## 1 Auftrag

Ein Referenzprojekt, das die Kältemaschine (KU3-1 bis KU3-4d) und den Kältespeicher (KU3-5) im Regressionsnetz hält: Saat auf dem Kopierweg des Programms, Einfrierregel, Wache, Zählnachzüge der Testklassen, Basis R36.

## 2 Vorgehen

Ein Opus-Auftrag in sechs Commits: Saatskript `Referenzlaeufe/Skripte/referenzprojekt_1055_kaeltemaschine.py` (`1fc3f30`), Testdatenbank (`821dafd`), Papiere mit Einfrierregel in `CLAUDE.md`, `Referenzlaeufe/LIESMICH.md` und Kühlkonzept 10.x (`54b6df7`), zehn Testklassen zählen 1055 mit (`4df54a5`), Wache `KaeltemaschineReferenzprojektWacheTests` (`c754255`), `BaualtersklassenSchemaTests` (`6ebcf5b`). Beim Merge `7b79b22` wurde die Testdatenbank auf der Fassung der BHKW-Sitzung neu gesät (87 089 152 Byte, SHA-256 `fc67a865…`). Nachzug: der Hinweis zum Netzbezug des Kältestroms nennt die Kältemaschine mit eigenem Text (`2e287c6`).

## 3 Ergebnis

- **Kopie 1017 → 1055** auf dem Kopierweg des Programms; die Wärmepumpe rechnet ohne Kühlbetrieb (Kühlbetrieb 0), die Kälte deckt allein die Kältemaschine.
- **Kältemaschine** in `Tab_Kaeltemaschine` „Kältemaschine 20 kW mit Trockenkühler“: Katalogsatz 2 auf ein Zehntel skaliert — 20 kW, EER 4,0, Rückkühlart `TROCKENKUEHLER`, Mindestteillast 20 %, Hilfsstrom Rückkühlung 0,6 kW, Kaltwasser-Vorlauf ≥ 5 °C, Hilfsstromanteil 0,05; Kennlinie 6 Punkte (Rückkühlung 25/35/45 °C × Kaltwasser 6/12 °C). Anlagenzeile Typ 13, Anzahl 1, `Kuehl_ID_Carrier` 58 „Elektrische Energie 2“, `Kuehl_EigenerZaehler` 1.
- **Kältespeicher** „Kaltwasserspeicher“: 2 000 l, 6/12 °C, Bereitschaftsverlust 1 kWh/24 h, Schwellen 10/95 %, Reserve 10 %, eine Schicht; Anlagenzeile Typ 12.
- **Kaskade** wie 1017 (BHKW, Heizkessel, Wärmepumpe, –, –, Stromspeicher); die Kältemaschine hat keinen Kaskadenplatz.
- **Gebäude 10661:** Kühlung aktiv, Sollwert 24 °C, Kühlleistung höchstens 15 kW.
- **Einfrierregel** „gesäte Kältemaschinendaten“ in `CLAUDE.md` und `Referenzlaeufe/LIESMICH.md`; Kühlkonzept 10.x „So gebaut (KU3-4b)“.
- **Wache** `KaeltemaschineReferenzprojektWacheTests` (3 Tests).
- **Zählnachzüge:** Gebäude 34 → 35, Katalogverweise 30 → 31, Elektrokessel 1018352, Preisbasis kWh 10 → 12, kg 2 → 3, `Tab_Kenndaten_Kuehlung` 20 → 30, Baualtersklasse E 2 → 3.
- **Hinweistext:** Der Hinweis zum Netzbezug des Kältestroms mit abweichendem Kühlträger nannte die Kältemaschine „Wärmepumpe ‚Kältemaschine 20 kW‘“; neuer Schlüssel `SIMENG_KAELTE_KM_KUEHLTRAEGER_MENGE` (de, en-US), Auswahl in `SimulationControl.KuehltraegerMengeHinweis`; Rechenweg unverändert.

## 4 Entscheide der Orchestrierung

- Beim Merge die Testdatenbank auf der Fassung der BHKW-Sitzung neu gesät statt binär zusammengeführt.
- Die freie Kühlung bleibt ohne Regressionsnetz aus 1055 (siehe Offenes), statt die Saat dafür zu verbiegen.

## 5 Nachweise

- **Lauf 9 Projekte:** die 8 CI-Projekte PASS und byte-gleich gegen R35.
- **1055:** Kältebedarf 4,083 MWh, Spitze 14,99 kW, 619 Stunden; Kälte der Maschine 4,187 MWh, Deckung 100 %; Kältestrom 1,256 MWh (Hilfsstrom 0,181 MWh), EER-Jahreswert 3,25, 60 Kühltage; Netzbezug 1,26 MWh über eigenen Zähler; freie Kühlung 0 h; Kältespeicher 13,92 kWh Kapazität, Ladung 2,522 MWh, Entladung 2,417 MWh, Verluste 0,105 MWh, 181,2 Vollzyklen; 166 Stunden unter Mindestteillast getaktet, 1 Stunde außerhalb der Kennlinie.
- **Tests:** angepasste Klassen, Wache und Link-Wache 122/122, nach Korrektur 35/35; ganzes `EPOS.Kern.Tests` 10 865 grün, 4 rot (3 davon fremd und im Hauptzweig behoben: Zielversion 185, ausführliche Vorlage), 2 übersprungen. Nachzug Hinweistext: Kern-Filter 0 Fehler, `KaeltemaschineAbrechnungTests` und `KaeltemaschineRechenwegTests` 18/18.
- **SQL-Dialekt:** 2 346 Texte, 0 Fundstellen.
- **Basis R36:** eingefroren mit 1055 in einem parallelen Auftrag; Gate 726 trägt die Orchestrierung nach.

## 6 Offenes

- Freie Kühlung hat aus 1055 kein Regressionsnetz: Der Trockenkühler kommt bei Kaltwasser 6 °C nie 3 K unter die Rückkühltemperatur (Hinweis; KU3-6 nach AK2, E75).
- Wiki-Upload der Kühlungsseite steht aus; Logbuch-Version beim Anwender erfragen.
- Sichtabnahme des Kältemaschinen-Dialogs unter Windows.
- Gate-Zahlen und CI-Kennung nachtragen.

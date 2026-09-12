# Protokoll Etappe KD1 — Kostenvorlagen-Datenmodell (Migrationsschritte 38/39)

Stand: 25.08.2026 · Branch `kostenformulare` · Konzept: `Konzept_Kostendialoge_EPOS-Plan.md` Rev. 1.2, § 4/§ 14

## 1. Umfang

| Baustein | Inhalt |
|---|---|
| `SchemaKatalog.cs` | `TAB_KOSTENVORLAGE`/`TAB_KOSTENVORLAGEPOSITION` + Spaltenkonstanten (`SPALTE_KV_*`, `SPALTE_KVP_*`, `SPALTE_PW_VORLAGEID`, `SPALTE_PW_STARTJAHR`, `SPALTE_EC_PRICE_POWER`, `SPALTE_EC_PRICE_POWER_MODUS`), DDL-Konstanten (CREATE/Index/Löschweitergabe `FK_KostenVorlagePos`), `Schritt38_Spalten`, Seed-Klassen `VorlagenPositionSeed`/`KostenVorlagenSeed`, `Schritt39_Vorlagen` (20 Vorlagen, 117 Positionen), `VORLAGE_NAME_STANDARD` |
| `SchemaMigration.cs` | `SCHRITT_38_KOSTENVORLAGEN` (Strukturen), `SCHRITT_39_KOSTENVORLAGEN_SEED` (Auslieferungsvorlagen), `ZIEL_VERSION` 37 → 39, Helfer `ParamOderNull` (typisierter DBNull-Parameter) |
| `DbWerte.cs` | 11 neue Bemessungs-Persistenzwerte (`JAHRESBETRAG`, `EUR_PRO_KWH_THERMISCH/_ELEKTRISCH`, `EUR_PRO_KW_LEISTUNG/_HEIZLEISTUNG/_ELEKTRISCH`, `EUR_PRO_KWP`, `EUR_PRO_KWH_KAPAZITAET`, `EUR_PRO_M2_KOLLEKTOR`, `PROZENT_ERZEUGERKOSTEN`, `PROZENT_STROMKOSTEN`), `LEISTUNGSPREIS_MODUS_JAHR/_MONAT`, sechs Bestands-Techniknamen `KOSTEN_KOMPONENTE_*` (an der Produktiv-DB nachgemessen) |

Seed-Regeln: `IstStandard = ReadOnly = TRUE`, Variantenname „Standard"; Sätze, Beträge und
Nutzungsdauern NULL (Struktur ohne erfundene Preise, § 4.3); Empfehlungsbereiche nur aus den
K5-Katalogdaten (BHKW 3,0–9,0 · Wärmezentrale 1,8–2,2 · Bauliche Anlagen 1,0–1,5 ·
Stromeinspeisung 1,8–2,2 · Personal 1,0–4,0 · Verwaltung 0,8–2,0 %); Kostenarten nach VDI 2067
(kapital-/betriebs-/bedarfsgebunden/sonstige — Hilfsenergie ist bedarfsgebunden).
**FK3 umgesetzt:** die Folien-Zeilen „Brennstoffkosten" (Kessel) und „Stromkosten (Verdichter)"
(WP) werden bewusst NICHT gesät.

## 2. Läufe (Testkopie der Produktiv-DB vom 25.08.2026 20:54, 151.949.312 Bytes)

Testaufbau: eigener Runner (`scratchpad\kd1runner`, .NET 8) lädt
`WindowsFormsApplication1.dll` aus dem x64-Debug-Build, setzt `Properties.Settings.DBPath`
**nur im Prozess** auf den Kopie-Ordner und ruft `SchemaMigration.Ausfuehren` — die
Produktiv-DB bleibt unberührt. Stolperstein für Nachahmer: die im Runner-Ausgabeordner
gelandete Fassaden-`System.Data.OleDb.dll` muss durch `runtimes\win\lib\net8.0\` ersetzt
werden, sonst „not supported on this platform".

| Lauf | Ausgang | Ergebnis |
|---|---|---|
| 1 (Stand 37) | Schritt 38: beide Tabellen + Index + FK angelegt; `Tab_ProjektWerte` 2 Spalten neu; `energy_carrier` 1 neu, 1 bereits vorhanden. Schritt 39: **20 Vorlagen, 117 Positionen** | OK, Schemastand 39 |
| 2 (Stand 39) | 38/39 „bereits erledigt" | OK |
| 3 (Marker von Hand auf 37 zurückgesetzt) | 38: alles „bereits vorhanden", 0 Spalten neu; 39: **0 angelegt, 20 bereits vorhanden, 0 Positionen** | OK — Idempotenz unabhängig vom Marker belegt |

## 3. Datenproben (Lauf-1-Kopie)

- 20 Köpfe mit `IstStandard = ReadOnly = TRUE` und Name „Standard"; Positionszahlen je
  Komponente/Kategorie: BHKW 8/11 · Heizkessel 7/8 · Wärmepumpe 7/9 · Solarthermie 7/9 ·
  Photovoltaik 8/9 · Pufferspeicher 2/9 · Stromspeicher 2/3 · Wärmezentrale 4/2 ·
  Bauliche Anlagen 6/2 · Stromeinspeisung 2/2 (Σ 117).
- Stichprobe BHKW/Betrieb deckt sich zeilengenau mit Folie 19 (ohne Energiekosten-Zeile),
  inkl. Kostenarten und Empfehlungsbereichen.
- NULL-Regel: 0 Positionen mit gesetztem Satz/Betrag/Nutzungsdauer; FK3: 0 Treffer auf
  „Brennstoffkosten"/„Stromkosten"; neue Spalten in `Tab_ProjektWerte`/`energy_carrier`
  durchgängig NULL.

## 4. Befunde

1. **`energy_carrier.price_power` existiert bereits** (Bestandsspalte ohne Leser) — Schritt 38
   ergänzt nur `price_power_modus`. Konzept § 7.1/KD1 entsprechend berichtigt.
2. **Umsatzsteuersatz existiert bereits**: `GESETZ_UMSATZSTEUER_REGELSATZ` (Klasse
   UMSATZSTEUER, 19,0 % ab 2007, GESICHERT, seit Etappe E1 „nur hinterlegt, noch ohne
   Rechenwirkung"). Kein neuer Seed; der KD2-Dialogfuß liest diesen Schlüssel. Konzept KL5
   berichtigt.
3. Ergebnisneutralität: repoweit keine Leser von `VorlageID`/`StartJahr`/`price_power_modus`
   außerhalb der Migration (grep-Beleg 25.08.2026).

## 5. Offen / Übergabe

- **Produktiv-DB**: Schritte 38/39 laufen dort beim nächsten Programmstart automatisch.
  Vorher datierte Sicherung nach `DB-Backup\` (Hausregel; die Testkopie ersetzt die
  Sicherung nicht).
- Abnahmekriterium „Referenzläufe byte-identisch" ist durch Strukturerweiterung + belegte
  Leserfreiheit abgedeckt; ein voller Referenz-Simulationslauf steht mit der
  KD-Gesamtabnahme an.
- Weiter mit **KD2** (Komponenten-Kostendialog, Designer-fähig nach Ä6).

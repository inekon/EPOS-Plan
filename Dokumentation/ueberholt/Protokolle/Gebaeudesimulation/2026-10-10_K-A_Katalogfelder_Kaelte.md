# Protokoll K-A — Katalogfelder der Kälteerzeuger (Schemaschritt 211)

**10.10.2026** · Sitzung Gebäudesimulation · Zweig `gs-ka` · Grundlage: Stufe K-A der
[Konzeptprüfung Kälteanlagen](../../../aktuell/Kälteanlagen/2026-10-10_Konzeptpruefung_Kaelteanlagen_Katalog_Import.md)
und Anwenderentscheid E118 (Feld Geräteart; Kältemittel, GWP, Füllmenge und Direktverdampfer als Stammdaten
zugelassen; Herstellernamen nur bei Anwenderimporten, nicht in Auslieferung und Wiki).

## Schema

`KaelteKatalogfelderSchema` (211 = `KaeltemaschineTeillastSchema.SCHRITT + 1`), je fünf Spalten an
`Tab_Kaeltemaschine_STAMM` und `Tab_Kaeltemaschine`, beide Tabellen bleiben `STRICT`:

| Spalte | Typ und Prüfklausel | leer heißt |
|---|---|---|
| `Geraeteart` | TEXT IN (`KWS_LUFT`, `KWS_WASSER`, `KWS_FREIKUEHLUNG`, `SPLIT`, `MULTISPLIT`, `VRF`, `ABSORPTION`) | Rückfüllregel |
| `Kaeltemittel_GWP` | REAL ≥ 0 | keine Angabe |
| `Kaeltemittel_Fuellmenge_kg` | REAL > 0 | keine Angabe |
| `Saisonkennzahl_Art` | TEXT IN (`SEER`, `ETA_S_C`) | keine Angabe |
| `Saisonkennzahl` | REAL > 0 | keine Angabe |

`Kaeltemittel` und `Firma` standen schon. Die Wertemenge kennt Split, Multisplit und VRF (K-D, K-E) und Absorption
(K-G), damit die Folgestufen keinen Tabellenneubau für eine erweiterte Prüfklausel brauchen.

**Nullbar statt NOT NULL.** Wie jede Wertespalte des Bestands (`Rueckkuehlart`, `Teillast_Weg`,
`Verdichterregelung`, `Kennfeld_Randweg`) ist `Geraeteart` nullbar mit CHECK: SQLite legt eine NOT-NULL-Spalte per
`ALTER TABLE` nur mit DEFAULT an, und eine feste Vorgabe ordnete jede wassergekühlte Maschine still falsch ein; ein
Neubau beider STRICT-Tabellen samt Fremdschlüsseln stünde in keinem Verhältnis. Die Lücke schließt die Rückfüllregel
an drei Stellen: im Schritt (jede Zeile wird belegt), im Schreibweg (`KaeltemaschineStammCtrl.KatalogfelderSchreiben`
schreibt nie NULL) und im Leseweg (ein NULL aus einem älteren Paket liest sich nach der Regel).

## Rückfüllregel

`KaelteKatalogfelderSchema.GeraeteartAusRueckkuehlart`: Alle Sätze des Bestands sind Kaltwassersätze (Kennfeld
Rückkühl- × Kaltwassertemperatur). Rückkühlart `LUFT` → `KWS_LUFT`; `WASSER`, `TROCKENKUEHLER`, `NASSKUEHLER` →
`KWS_WASSER` (der Verflüssiger ist wassergekühlt, das Rückkühlwerk gehört nach E33/K8 zur Maschine); leer →
`KWS_LUFT`, weil der Rechenweg eine leere Rückkühlart als Luft rechnet. Freikühlung, Split, Multisplit, VRF und
Absorption entstehen nie aus der Regel. Der Schritt bildet die Prüfsumme jedes Katalogsatzes neu, dessen gespeicherte
Summe vor der Rückfüllung stimmte; ein geänderter Satz bleibt als geändert erkennbar.

Testdatenbank 210 → 211 mit `Werkzeuge/Testdatenbankschema`: **37 Katalogsätze** (12 `KWS_LUFT`, 25 `KWS_WASSER`:
13 Nasskühler, 8 Trockenkühler, 4 Wasser), **3 Projektkopien** (1055, 1059, 1063, je Trockenkühler → `KWS_WASSER`),
**37 Prüfsummen** neu; die übrigen vier Spalten leer, keine Auslieferungszeile mit Firma. Der Zellvergleich gegen
den Stand 210 zeigt nur `Tab_Applikation.SchemaVersion` und `Tab_Kaeltemaschine_STAMM`; `quick_check` ok,
`foreign_key_check` leer, SQL-Dialektprüfer 0 Fundstellen (2 720 Texte). 95 596 544 Byte, LFS-SHA-256
`ee2ba997bc37518e96a53391a6e0f23d45c80c9a58e9e5c5107e81841f0b76ce`.

## Kern, Import, Oberfläche

- `KaeltemaschineModel` um die fünf Felder; `KaeltemaschineStammCtrl`: Lesen, `KatalogfelderSchreiben` (nur wenn
  die Spalten stehen), `KatalogfelderPruefen` (Wertemenge, `KWS_LUFT` nur mit Luft, `KWS_WASSER` nie mit Luft, GWP
  0…30 000, Füllmenge über 0 bis 10 000 kg, SEER 1…20 bzw. ηs,c 50…800 %, Art und Wert nur zusammen),
  `GeraeteartText`. Die Projektkopie (`KaeltemaschineCtrl.AusKatalogUebernehmen`) nimmt die Felder über denselben
  Schreibweg mit. Katalogfassung (Register „KM“) über `KaeltemaschineSchema.Fachspalten`.
- Katalogliste: Spalte Geräteart (filterbar, Rang „bei Platz“ nach der Herkunft, vor dem Typ); `ParameterVerwendung`
  führt die fünf Spalten als Dialogfelder ohne Rechenweg.
- Import: Die CSV-Vorlage (`Quellen/Kaeltemaschine_Kennfeldvorlage.csv`) liest `Geräteart`, `GWP`, `Füllmenge`,
  `SEER` oder `ETA_S_C` optional; eine fehlende Geräteart folgt der Regel, eine unverträgliche Angabe gibt einen
  Hinweis und verwirft die Felder. Copper-Sätze und die eingebauten Typkennfelder bekommen die Geräteart aus
  `condenser_type`.
- Katalogdialog: fünf Felder hinter den zwölf Grundfeldern der Gruppe Kenndaten (Geräteart und Art der Kennzahl als
  Auswahl), Lesemodus und Vergleich als Text, KI-Sicht und KI-Feldkarte; Ressourcen in beiden Sprachen.

## Tests und Nachweis

- Neu: `KaelteKatalogfelderSchemaTests` (Nummer, Spalten, CHECK, Rückfüllregel, Testkopie, Rundreise samt
  Prüfsummen, unstimmige Prüfsumme), `KaelteKatalogfelderTests` (Prüfung, Rundlauf Katalog/Projektkopie, Schreibweg,
  Katalogliste, CSV, Copper), bunit `KaeltemaschineKatalogKatalogfelderTests`.
- Nachgezogen: Spaltenzahlen 27 → 32 und 25 → 30, Profil mit acht Spalten, Spaltenstufen (Geräteart 1 040 px,
  Typ 1 120 px), der Test des Schritts 210, `ParameterVerwendung`.
- Gefilterte Läufe grün: `EPOS.Kern.Tests` (Kältemaschine, Katalogfassung, Paketanhebung, Parameterverwendung,
  KI-Dialoge, Dokumentationswache), `EPOS.UI.Tests` (Kältemaschine, KI-Dialogkatalog, Katalogliste). Kern-Filter
  und Windows-Schale bauen mit 0 Fehlern.
- Referenzlauf (`EPOS.Referenzlauf`, vorher gebaut) der Projekte 1017, 1055, 1059, 1063 und 1064 auf der
  Testdatenbank 211 gegen `2026-10-10_R51_FreieKuehlung`: fünfmal PASS, **169 von 169 Dateien byte-gleich** — kein
  Rechenweg liest die Spalten.

## Offen

- Filter nach Geräteart in der Kälte-Kachel (Oberflächenspalte von K-A im Stufenplan) — mit Stufe 5 der
  Katalogauswahl.
- `Proben/Rasterprobe` nicht gezogen (keine Änderung an `Katalogliste` oder Raster, nur eine Profilspalte).

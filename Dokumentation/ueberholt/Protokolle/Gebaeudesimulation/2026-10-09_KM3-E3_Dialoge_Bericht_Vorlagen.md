# Protokoll KM3-E3 — Dialoge, Kennzahlen, Tafel, Vorlagen-Katalogfassung 18 (09./10.10.2026)

**Sitzung:** Gebäudesimulation, Statuszeile **#876**. Commits E3-a `1a8187f6e`, `5f1caee61`, `d1e446ef0`, `935f9fb65`; E3-b `3b691a1ab`, `462c56f69`, `4fb0155b9`, `bf11a4044`; E3-c `176aa8526`; Merge `bc584f434`. **Entscheid:** E114. Konzepte: [`Konzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md`](../../../aktuell/Kälteanlagen/Konzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md) und [`Umsetzungskonzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md`](../../../aktuell/Kälteanlagen/Umsetzungskonzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md).

## 1 Auftrag und Entscheidlage

- **E114** (Anwender, 09.10.2026): KM3-Q1 bis Q11 nach Empfehlung a; E3 bringt die Dialoggruppe, die Lesewerte, die Kennzahlen, die Tafel und die Vorlagen der Katalogfassung 18.

## 2 Wellen

| Welle | Commits | Ergebnis |
|---|---|---|
| E3-a | `1a8187f6e`, `5f1caee61`, `d1e446ef0`, `935f9fb65` | Gruppe „Teillast und Takten“ mit acht Feldern im Katalogdialog der Kältemaschine, Lesezeile, Kurvenbild, Schnellwahlen „Kurve aus Typkennfeld“ und „Auf Datenblatt skalieren“, Auskunft „Teillastpunkte prüfen“ (A–D), `KaeltemaschineTeillastDialogrechnung`, Hülle, KI 8 Felder, 54 Ressourcen |
| E3-b | `3b691a1ab`, `462c56f69`, `4fb0155b9`, `bf11a4044` | Lesewerte im Anlagendialog, Folgeschaltungshinweis, Kachelzeile im Kältereiter, Kennzahlen `kaelte.km.taktstrom`, `starts`, `teillastanteil`, `lastgrad`, `jaz_verdichter` (Seit 18), Tafel `stand.tabelle.km_teillast` in Word und Excel, Katalogfassung 18, Messlatte Vorlagenfeldkatalog v18, Export-Zeilen `Kaeltemaschine;<Bezeichner>`, KI-Sicht `km_teillast`, 33 Ressourcen, Meldungsschwelle 0,3 |
| E3-c | `176aa8526` | zehn Berichtsvorlagen auf Katalogfassung 18 neu gebaut |
| Merge | `bc584f434` | E3-a, E3-b, E3-c in den Stand von E1/E2 |

## 3 Dateien

- Kern: `KaeltemaschineTeillastDialogrechnung`, Kennzahlen und Tafel des Berichts, Vorlagenfeldkatalog, `ParameterVerwendung` (Kältemaschine mit Kostenvorlage und den acht Teillastspalten auf Simulation), Ressourcen (beide Sprachen), Designer.
- Oberfläche und Hülle: Katalogdialog und Anlagendialog der Kältemaschine, Kachelzeile im Kältereiter, KI-Simulationsmaske.
- Vorlagen: `WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/` (zehn Dateien, Fassung 18).

## 4 Proben

| Probe | Ergebnis |
|---|---|
| Merge-Stand | keine Konfliktmarker; resx 16 768/16 768 Schlüssel, keine Dubletten; Designer unverändert |
| Bau | Kern-Filter 0 Fehler; Windows-Schale 0 Fehler |
| SQL-Dialekt | 2708 Texte, 0 Fundstellen |
| `EPOS.Kern.Tests` (Filter E3) | 2538/2538 |
| `EPOS.UI.Tests` (Filter E3) | 1343/1343 |
| ChartProben | 254 Bilder, alle grün |
| Referenzlauf gegen R48 | 28/28 `PASS`, alle Projektdateien byte-gleich; R48 bleibt unverändert |

## 5 Festlegungen

Die Auskunft „Teillastpunkte prüfen“ kommt ohne Vorgabewerte und ohne Normtafel aus; der Lastgrad bezieht sich auf die Nennkälteleistung; „Auf Datenblatt skalieren“ legt den skalierten Satz über „Neu…“ an; der Jahres-EER des Verdichters rechnet ohne Hilfsstrom und mit abgezogener freier Kühlung; die Tafel hat elf Zeilen; kein Bild (KM3-Q9 a).

## 6 Offen

E4 (Wiki, Logbuch).

## 7 Gate und CI

**Gate:** @GATE@

**CI:** läuft (Vermerk folgt).

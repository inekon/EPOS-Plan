# Wirtschaftlichkeit und Kosten — Mockups und Rechenwege

**Stand 02.09.2026** · Konzeptstand: `../Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`
(führende Fassung) · `SchemaMigration.ZIEL_VERSION` = 61

Dieser Ordner enthält die **visuellen Entwürfe** der Kostendialoge und zu jeder Kostenkategorie den
**dokumentierten Rechenweg** — Formel, Rechtsgrundlage, Codestelle und eine durchgerechnete
Zahlenprobe an einem einzigen Beispielprojekt. Er ist die ausgelagerte Detailfassung von § 2.12 des
konsolidierten Konzepts.

> **Entwurf, keine Umsetzung.** Alle Dialoge sind Vorschläge zur Abnahme (Arbeitsregel des
> Anwenders: erst Konzept, dann Code). Umgesetzt ist bislang nur die Pflichtpositionen-Etappe H1.
> Maßgeblich bei Widerspruch ist das konsolidierte Konzept.

## Struktur

```
Wirtschaftlichkeit_Kosten/
├── LIESMICH.md                          diese Datei — Einstieg und Lesereihenfolge
├── Beispielprojekt.md                   die eine Zahlenquelle: Eingangsgrößen, Mengenbilanz, Preise
├── Mockups/
│   └── Dialog_Formel_Zahlenprobe.html   alle acht Kategorien als Seite; lokal im Browser öffnen
└── Rechenweg/
    ├── 01_Investitionskosten_BHKW.md    Drei-Runden-Kaskade, Zuschussklemme
    ├── 02_Betriebskosten_BHKW.md        Pflichtpositionen, Hilfsenergie an der Endenergie
    ├── 03_Kosten_Photovoltaik.md        kWp-Mengenkette, Ersatz und Restwert, Degradation
    ├── 04_Energiekosten.md              Preisbestandteile, Aufschläge, BEHG-Reihe, Emissionsfaktoren
    ├── 05_Verguetungen_BHKW.md          KWKG-Mischsatz, Mengentafel, Jahresreihe, Energie- und Stromsteuer
    ├── 06_Verguetungen_PV.md            anzulegender Wert, Marktprämie, § 51/51a, 60-%-Kappung
    ├── 07_Erloesrubrik.md               Block A zahlungswirksam, Block B Ausweis, vermiedene Kosten
    └── 08_Wirtschaftlichkeit_Nutzungsdauer.md   Kapitalwert nach DIN EN 17463, Höfingen-Gegenprobe
```

Die Mockup-Seite ist zugleich als Artifact veröffentlicht:
[Dialog, Formel, Zahlenprobe](https://claude.ai/code/artifact/739d3cca-3b6c-4e2b-af8a-d1a7f73ddc9f).
Die HTML-Datei hier ist die Quelle; ein Redeploy erfolgt über den Artifact-Link (`url`), damit die
Adresse stabil bleibt.

## Lesereihenfolge

1. **`Beispielprojekt.md`** — ohne die Mengenbilanz sind die Zahlen der Rechenwege nicht prüfbar.
2. **Die Mockup-Seite** — jede Kategorie ist dort dreigeteilt: Dialog · Berechnungsgrundlage ·
   Berechnungserläuterung.
3. **`Rechenweg/05` und `06`** — die beiden Vergütungsseiten sind der Schwerpunkt des Auftrags.
4. Die übrigen Rechenwege in Nummernfolge; `08` schließt mit dem Kapitalwert.

## Aufbau jeder Rechenweg-Datei

| Abschnitt | Inhalt |
|---|---|
| Kopf | Dialog, Mockup-Anker, Rechtsgrundlagen, Codestellen |
| Was der Dialog zeigt | Felder, Gruppen, Herleitungszeilen, Warnbänder — was der Anwender sieht und warum |
| Berechnungsgrundlage | die Formeln in der Fassung der Formelkarte (Konzept § 3), mit Norm und Codestelle |
| Berechnungserläuterung | Schritttabelle am Beispielprojekt: Rechnung → Ergebnis → Anmerkung |
| Befunde und offene Punkte | was vor der Umsetzung zu entscheiden ist, mit Nummer aus dem Konzept |

## Die Dialogform der Komponentenkosten (abgenommen 02.09.2026)

Kopfband (`#0F1F3D`) mit Titel „Kosten der Komponente — ‹Anlage›" und Zusatz „Investition · netto"
· Reiter **Investition / Betrieb / Ertrag-Bonus** · Raster mit **Position · Kostenart · Bemessung ·
Satz · Menge (mit Herleitungszeile in Monospace) · Betrag · Runde** · Summenzeile mit Brutto,
Zuschuss und I₀ · Warnband amber für Fachhinweise · Infozeile für die Mengenreihenfolge · Fußleiste
mit Statuszeile, „Aus Vorlage übernehmen…", „+ Position", „Speichern".

Für **Photovoltaik** dieselbe Form mit eigenen Anordnungen: Spalte *Nutzungsdauer* im
Investitionsraster, Herleitung der kWp-Menge aus Modulanzahl × Modulleistung, Gruppe *Ersatz und
Restwert* mit Barwerten, Kennzahl €/kWp, Betriebsseite ohne Endenergie-Bemessung, Gruppe *Ertrag und
Degradation* — siehe `Rechenweg/03`.

## Herkunft der Zahlen

Belegzahlen des Bestands sind in Mockup und Rechenwegen gekennzeichnet: Kaskadenprobe Projekt 1042
(Delta +20.927,61 €), Mischsatz 300 kW (5,5667 ct/kWh), AW 300 kWp (6,04 ct/kWh),
Höfingen-Kapitalwert (65.259 €). Alle übrigen Werte sind Annahmen des Beispielprojekts und als
solche in `Beispielprojekt.md` aufgeführt.

## Verwandte Dokumente

- `../Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` — Gesamtkonzept, § 3 Rechenwege
- `../Grundlagen_KWKG_Energiesteuer_Stromsteuer.md` — Rechtsstand mit Quellen
- `../KONTEXT_Kosten_Energie_Wirtschaftlichkeit.md` — Datenwelten und Festlegungen
- `../Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md` — Etappenkonzept (Historie bis B4)

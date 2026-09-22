# Wirtschaftlichkeit und Kosten — Rechenwege und Zahlenprobe

Führende Fassung: `Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` in diesem Ordner — der
**gültige Stand**. Zwei Schwesterpapiere gehören dazu: das
[`Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md`](Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
führt jeden Entscheid des Feldes (Frage, Wortlaut, Datum und Weg, Ort der Regel, Umsetzungsstand) und
ist die eine Stelle, an der neue Entscheide eingetragen werden; das
[Protokoll der Entscheidwege](../../ueberholt/Protokolle/Reporting/Konzept_Wirtschaftlichkeit_Entscheidwege_Protokoll.md)
führt die Geschichte des Konzepts — nie Regelquelle.

Dieser Ordner enthält das **Funktionsbild** der Kostendialoge und der Ergebnisseite sowie zu jeder
Kostenkategorie den **dokumentierten Rechenweg** — Formel, Rechtsgrundlage, Codestelle und eine
durchgerechnete Zahlenprobe an einem einzigen Beispielprojekt. Er ist die ausgelagerte Detailfassung
von § 2.12 des konsolidierten Konzepts; maßgeblich bei Widerspruch ist das Konzept.

Das Mockup `../Mockups/Dialog_Formel_Zahlenprobe.html` beschreibt den Zustand **nach** der
Umsetzung. **Der Anwender hat es am 22.09.2026 abgenommen**; es ist die verbindliche Zielvorgabe
der Etappen E4 ff. des Analysepapiers. Was davon noch nicht gebaut ist, steht gesammelt in seinem
Anhang „Umsetzungsstand" — und nur dort.

## Struktur

```
Wirtschaftlichkeit_Kosten/
├── LIESMICH.md                          diese Datei — Einstieg und Lesereihenfolge
├── Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md   das Konzept: der gültige Stand
├── Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md  alle Entscheide; hier werden neue eingetragen
├── Beispielprojekt.md                   die eine Zahlenquelle: Eingangsgrößen, Mengenbilanz, Preise
├── 2026-09-19_Pruefung_Mockups_Wirtschaftlichkeit.md   Prüfung der Mockups: Befund und Änderungsplan
├── 2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md   Analyse des Konzepts für die Umsetzung: Stand, Lücken, Entscheide, Etappen
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

Die Mockups dieses Themas liegen mit allen übrigen unter `../Mockups/`: das konsolidierte
`Dialog_Formel_Zahlenprobe.html` — alle acht Kategorien mit Dialog, Berechnungsgrundlage,
Berechnungserläuterung, Ergebnisseite und den beiden Anhängen — und
`Ergebnis_Bandbreite_Herkunft.html`; beide lokal im Browser öffnen.

Die Mockup-Seite ist zugleich als Artifact veröffentlicht:
[Dialog, Formel, Zahlenprobe, Ergebnis](https://claude.ai/artifact/1WeFMXrpxrCSu1jUtvTRCw)
(neu veröffentlicht am 22.09.2026 aus dieser Datei, mit Systemschrift-Kette statt Google-Schrift —
Entscheid Q22). **Die Repo-Datei führt:** Die HTML-Datei hier ist die Quelle; ein Redeploy erfolgt
über den Artifact-Link (`url`), damit die Adresse stabil bleibt. Das frühere Artifact `739d3cca…`
gehört einem anderen Konto und ist abgelöst.

## Lesereihenfolge

1. **`Beispielprojekt.md`** — ohne die Mengenbilanz sind die Zahlen der Rechenwege nicht prüfbar.
2. **Die Mockup-Seite** — jede Kategorie ist dort vierfach dargestellt: Dialog ·
   Berechnungsgrundlage · Berechnungserläuterung · Beschriftungen mit Ressourcenschlüsseln, je mit
   einer Abnahmezeile. Kategorie 8 trägt die Ergebnisseite. Wer vor der Umsetzung liest, beginnt
   mit dem Anhang „Umsetzungsstand".
3. **`Rechenweg/05` und `06`** — die beiden Vergütungsseiten sind der Schwerpunkt des Auftrags.
4. Die übrigen Rechenwege in Nummernfolge; `08` schließt mit dem Kapitalwert.
5. **`2026-09-19_Pruefung_Mockups_Wirtschaftlichkeit.md`** — was an den Mockups und den Papieren
   nicht zusammenpasst, mit Änderungsplan (§ 3) und den Entscheidfragen Q1–Q25 (§ 4).
6. **`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`** — wer umsetzen will, liest
   zuletzt dieses: Stand gegen Code, Lücken, die Entscheide A1–A20 und der Etappenplan E0–E12.

Quer zu dieser Reihenfolge steht das **`Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md`**: Wer
wissen will, was entschieden ist und wo die Regel steht, oder wer einen neuen Entscheid einträgt, liest
dort. Wie ein Entscheid zustande kam, steht im Protokoll der Entscheidwege.

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
· **Optionsgruppe „Betriebskosten / Investitionskosten"** und **zwei Reiter** „Kosten
Invest/Betrieb" (`KDLG_TAB_KOSTEN`) und „Ertrag/Bonus" (`KDLG_TAB_ERTRAG`) · Raster mit
**Aktionen · Position · Bemessung · Satz · Betrag netto [€] · Nutzungsdauer [a] · Worst/Best** —
Kostenart und Runde stehen im Zeileneditor bzw. in der Herleitungszeile unter dem Betrag ·
Summenzeile mit Brutto, Zuschuss und I₀ · Warnband amber für Fachhinweise · Infozeile für die
Mengenreihenfolge · **vier Rasterknöpfe** „+ Position hinzufügen", „Aus Vorlage übernehmen…",
„Positionskatalog…", „Nutzungsdauern vorbelegen…" und darunter die Leiste „Abbrechen · Speichern ·
OK" mit Statuszeile.

Die Spalte *Nutzungsdauer* und die Gruppe *Ersatz und Restwert* stehen **technikneutral** im
Investitionsraster jeder Komponente — Ersatz und Restwert hängen an der einzelnen Position, nicht an
der Technik. Für **Photovoltaik** kommen eigene Anordnungen hinzu: Herleitung der kWp-Menge aus
Modulanzahl × Modulleistung, Kennzahl €/kWp, Betriebsseite ohne Endenergie-Bemessung, Gruppe *Ertrag
und Degradation* — siehe `Rechenweg/03`.

## Herkunft der Zahlen

Jede Zahl des Mockups trägt eine von drei Klassen: **Beispielzahl** (aus `Beispielprojekt.md` oder
daraus abgeleitet), **Beleg** (aus dem Datenbestand oder einer fremden Mappe nachgemessen:
Kaskadenprobe Projekt 1042 mit Delta +20.927,61 €, Mischsatz 300 kW mit 5,5667 / 2,4167 ct/kWh,
anzulegender Wert 300 kWp mit 6,04 ct/kWh, Aufschlagsmessung Projekt 1030, Höfingen-Kapitalwert
65.259 €) und **abgeleitet** (allein für die Darstellung gebildet, etwa die interpolierten
Zwischenwerte der beiden äußeren Verlaufskurven). Das Register steht im Anhang „Herkunft der Zahlen"
des Mockups.

Das Beispielprojekt rechnet durchgängig mit **einer** BHKW-Größe (300 kW, I₀ 234.772,40 €) und
**einer** Jahr-1-Konvention (Kalenderjahr der Inbetriebnahme, hier 2026).

## Verwandte Dokumente

- `Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` — Gesamtkonzept, § 3 Rechenwege
- `Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md` — die Entscheide des Feldes, je Familie eine Tafel
- `../../ueberholt/Protokolle/Reporting/Konzept_Wirtschaftlichkeit_Entscheidwege_Protokoll.md` — Protokoll der Entscheidwege (Geschichte, keine Regelquelle)
- `../Grundlagen_KWKG_Energiesteuer_Stromsteuer.md` — Rechtsstand mit Quellen
- `../../ueberholt/KONTEXT_Kosten_Energie_Wirtschaftlichkeit.md` — Datenwelten und Festlegungen (Geschichte, keine Regelquelle)
- `../../ueberholt/Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md` — Etappenkonzept bis B4 (Geschichte, keine Regelquelle)

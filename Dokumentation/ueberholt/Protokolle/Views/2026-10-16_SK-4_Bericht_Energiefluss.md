# SK-4 — Energieflussbild im Word-Bericht

Welle SK, Schritt SK-4, 16.10.2026, Zweig `sk4` (von `sk3`, Kopf `de315c615`, dazu `origin/ios_migration_september`
hineingemerged). Konzept:
[`2026-10-16_Konzept_Energieflussbild_Sankey.md`](../../../aktuell/Dialoge/2026-10-16_Konzept_Energieflussbild_Sankey.md)
(Abschnitte 5 und 8). Vorgänger: [`2026-10-16_SK-3_Oberflaeche_Energiefluss.md`](2026-10-16_SK-3_Oberflaeche_Energiefluss.md).
Anwenderentscheide: SK-4 „gleich mit Bericht“, SK-Q10 Bild im Baustein „Ergebnisse je Variante“, SK-Q12 im Bericht
tragen die Knoten ihre Jahressumme (MWh/a), Bänder ohne Zahl, SK-Q14 Legende im Bild, SK-Q17 der Bericht zeigt immer
die Kette (Variante A, `ENERGIEFLUSS_BERICHT`; kein Parameter für „Stufen“).

## 1. Umfang

- **Sammeln** (`BerichtsDatenSammler`, Schritt 6d): je Stand `VariantenDaten.Energieflussbilanz =
  Energiefluss.Aus(v, BerichtTexte.Englisch)` nach Kosten- und Trägerrechnung; Fehler → `null`. Die Schreiber lesen
  nur `BerichtsDaten`.
- **Modellfabrik** (`Berichtsbilder.Ergebnis.cs`): `EnergieflussbilanzDes(v, englisch)` nimmt die gesammelte Bilanz
  in passender Sprache, sonst baut sie sie aus dem Ergebnisbaum (ohne Datenbank, etwa für eine Vorlage in anderer
  Sprache); `Energiefluss(v, mass, englisch)` → `EnergieflussAbbildung.Bild` →
  `ChartRenderer.EnergieflussModell(…, EnergieflussMass(mass), knotenwerte: true)`; `null` ohne Fluss.
  `EnergieflussMass`: ohne Maß `ENERGIEFLUSS_BERICHT`, eine fehlende Seite im Seitenverhältnis der Berichtsfassung —
  keine festen Zahlen, die neue Gestaltung der Parallelsitzung greift ohne Nacharbeit.
- **Katalog v25** (`Vorlagenfeldkatalog.Bilder.cs`): `stand.bild.energiefluss` (Kontext Stand, `Vorlagenbedarf.Keiner`,
  Ausgabe nur Word, `_bildgroessen` = `FAKTOR_BREIT`/`Bildmass.MIN_BREITE`) und Schalter `hat.bild.energiefluss`, beide
  `FASSUNG_ENERGIEFLUSS = 25`; `KATALOGFASSUNG = 25`; Kapitel „Ergebnisse je Variante“ deckt Bild und Schalter
  (`_kapitelDerTabellenUndBilder`). Ohne Ergebnis der Grund `BV_GRUND_KEIN_ERGEBNIS`, ohne Fluss der Grund der Bilanz.
  Messlatte `EPOS.Kern.Tests/Messlatten/Vorlagenfeldkatalog_v25.txt` (v24 + zwei Schlüssel), `VorlagenfeldkatalogWacheTests`
  auf 25, die Kapitelwache nimmt das Energieflussbild zu den Bildern der Fassung 4.
- **Bausteinweg** (`ErgebnisseBaustein.SchreibeWord`): nach der Kennzahlentafel, vor den Ganglinien
  `ZeichneEnergiefluss` — Bild in 620 px Anzeigebreite, Höhe aus dem Seitenverhältnis des Modells, darunter die
  Beschriftung `EF_BER_BESCHRIFTUNG`; ohne Fluss statt des Bildes der Hinweis `EF_BER_OHNE_BILD` („Kein
  Energieflussbild — {Grund}“). Excel bekommt kein Diagramm (Ausgabe nur Word, der Prüfer meldet „nur Word“).
- **Vorlagen:** `Werkzeuge/Berichtsvorlage alle` — zehn Vorlagen neu (Standard, Beispiel, Kurzbericht de/en,
  Ausführlich de/en, Bausteine de/en, Excel ausführlich de/en), OpenXmlValidator 0 Fehler, zweiter Lauf byte-gleich;
  inhaltlich `EPOS.Katalogfassung` = 25 und die Schnellbausteine der Bausteinvorlagen.
- **Oberfläche:** Marke `stand.bild.energiefluss` (Stufe ähnlich, Hinweis `VF_ORT_ENERGIEFLUSS`) an beiden
  `DiagrammSvg` der Ansicht „Fluss“, Ortszeile in `Vorlagenfeldorte` (Berichte › Übersicht, `bild.energiefluss`);
  die SK-3-Ausnahme in `VorlagenfeldAbdeckungWacheTests` ist entfernt.
- **Ressourcen** beider Sprachen: `VF_STAND__BILD__ENERGIEFLUSS`, `VF_HAT__BILD__ENERGIEFLUSS`, `EF_BER_BESCHRIFTUNG`,
  `EF_BER_OHNE_BILD`, `VF_ORT_ENERGIEFLUSS`; `Resource.Designer.cs` neu erzeugt.
- **Wiki-Quelle** `Projekte/Wiki/Programm Dokumentation - Berichtsvorlagen.wiki`: Absatz „Energiefluss“ (Anker
  `energiefluss`).
- **Merge** `origin/ios_migration_september`: Konflikt nur in `Resource.resx`/`Resource.en-US.resx` (beide Seiten,
  elf Dubletten entfernt, je 19 112 Schlüssel, gleicher Bestand).

## 2. Abnahme

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.
- `EPOS.Kern.Tests`, Filter `Vorlage|Bericht|Baustein|Word|Energiefluss|Wache`: 1 501 Tests; im ersten Lauf 7 rot —
  allein die fünf Berichtsmesslatten (`Bericht_Word_1030`, `_Valeri`, `_Vorlage`, `Bericht_Word_Gruppe`, `_Vorlage`),
  jede um genau die erwarteten Zeilen (Bild 620 × 310 px samt Beschriftung bzw. der Satz „Kein Energieflussbild — …“
  der synthetischen Gruppe ohne Flüsse); aus dem Testausgabeordner übernommen, danach `BerichtWertesatzTests` und
  `BerichtVorlagenMesslatteTests` 17/17 grün, also 1 501/1 501.
- Neu `BerichtEnergieflussTests` (4 Fälle): Katalog v25 nur Word ohne Zeitreihen, Kapitel deckt Bild und Schalter,
  Ressourcen beider Sprachen; Maßregel; ohne Fluss der benannte Satz (Stand ohne Flüsse, Stand ohne Ergebnis);
  Gruppe 1019 (Stamm und zwei Varianten) — Bilanz je Stand gesammelt, Bausteinweg ohne Datenbankzugriff mit drei
  Bildern (PNG + SVG, „MWh/a“ an den Knoten, Seitenverhältnis der Berichtsfassung) und drei Beschriftungen, Platzhalter
  im Block `je stand` mit Schalter an, OpenXmlValidator 0 Fehler.
- `EPOS.UI.Tests`, Filter `Bericht|Vorlage|Uebersicht`: 643/643.
- `Werkzeuge/Auslieferungsvorlage.Tests`: 63/63 (`Werkzeuge/Berichtsvorlage` hat kein eigenes Testprojekt; seine Wachen
  `BerichtsvorlageDateiWacheTests`, `AuslieferungsvorlagenWacheTests` liegen im Kern-Filter und sind grün).
- Sichtprobe: Word-Bericht der Gruppe 1019 (Baustein „Ergebnisse je Variante“) außerhalb des Repositoriums erzeugt,
  das Bild steht je Stand unter der Kennzahlentafel.
- Die Berichtsmesslatten nennen das Anzeigemaß (620 × 310 px aus `ENERGIEFLUSS_BERICHT` 1 240 × 620); ändert die
  Parallelsitzung das Maß, ändern sich diese fünf Zeilen mit.

## 3. Offen und Hinweise

- Die Bildgestaltung (Spuren, Legende, Maß ≈ 1 180 × 430) baut die Parallelsitzung auf `sk2c` um; SK-4 nutzt nur die
  öffentliche Kette und `ENERGIEFLUSS_BERICHT`. Nach deren Merge ist die Sichtprobe des Berichts zu wiederholen.
- Die Kapitelwache führt das Energieflussbild als einzigen Bildschlüssel nach Fassung 4 im Kapitel „Ergebnisse“
  (`MitKapitel` in `VorlagenfeldkatalogWacheTests`).

## 4. Logbuch-Entwurf (Versionsnummer beim Anwender erfragen)

- Der Word-Bericht zeigt je Variante das Energieflussbild unter den Kennzahlen; für eigene Vorlagen gibt es den
  Platzhalter `stand.bild.energiefluss`.

# SK-1 — Flussmodell des Energieflussbilds im Kern

Welle SK, Schritt SK-1, 16.10.2026, Zweig `sk1` (von `origin/ios_migration_september`, Zweig `sk` mit Konzept und
Mockup gemergt). Konzept:
[`2026-10-16_Konzept_Energieflussbild_Sankey.md`](../../../aktuell/Dialoge/2026-10-16_Konzept_Energieflussbild_Sankey.md)
(Abschnitte 1, 2, 3.3, 6, 8, 10). Anwenderentscheide 16.10.2026: ein Bild mit Strom, Wärme und Kälte, dritte Stellung
„Fluss“, eine gewählte Version, Wortbericht in derselben Welle, SK-Q1 bis SK-Q11 nach Empfehlung, SK-Q12 Werte am
Bildschirm nur im Zeigertext (keine Wertetabelle), im Bericht Knoten mit Jahressumme.

## Umfang

| Datei | Inhalt |
|---|---|
| `EPOS.Kern/Allgemein/Bericht/Energiefluss.cs` | Domänenmodell (`EnergieflussModell`, `EnergieflussKnoten`, `EnergieflussFluss`, Aufzählungen `EnergieflussKnotenArt`, `EnergieflussForm`, `EnergieflussHerkunft`) und der Flussbau `Energiefluss.Aus(VariantenDaten, bool, string)` bzw. `Aus(ErgebnisModel, Trägernachweise, bool, string, int)` |
| `EPOS.Kern/Controller/EnergieflussCtrl.cs` | `Lade(idProjekt, istStamm, englisch, versionsname)` über `EnergievergleichCtrl.Lade` — gespeicherter Ergebnisbaum, kein Lauf, keine neue SQL-Anweisung |
| `EPOS.Kern/MyResource/Resource(.en-US).resx`, `Resource.Designer.cs` | 97 Schlüssel `EF_*` (Knotennamen `EF_KN_*`, Kurznamen `EF_KK_*`, Zeiger-, Formel-, Hinweis-, Grund-, KI- und Herkunftstexte), Designer mit `designer_neu.py schreiben` (wiederholbar) |
| `EPOS.Kern.Tests/EnergieflussTests.cs` | 24 Fälle an synthetischen Ergebnisbäumen |
| `EPOS.Kern.Tests/EnergieflussReferenzprojektWacheTests.cs` | 4 Fälle: 1018, 1046, 1058, 1063 auf der Testdatenbank |

Nicht berührt: Renderer, Farbrollen, Oberfläche, Bericht, Schema, Rechenweg, Referenzbasis.

## Regeln des Flussbaus

- **Einheit MWh/a.** Die kWh der Puffer-, Kälte- und Stromspeicherzeilen rechnet allein `Energiefluss.AusKwh` um, über
  `Energieeinheit.MWh.AusKWh`.
- **Spalten** 0 Quellen, 1 Wandler I, 2 Strom, 3 Wandler II, 4 Wärme/Kälte, 5 Speicher, 6 Senken; Verlust- und
  Füllstandsknoten in der Lücke (Spalte des Eigentümers + 0,5). Reihenfolge nach Tabelle 2.2.
- **Bilanz** je Wandler, Bus und Speicher: `|Zufluss − Abfluss| ≤ max(0,05 MWh; 0,5 % des Zuflusses)`. Am Stromknoten
  wird ein positiver Rest bis zum BHKW-Strom (plus Toleranz) zum Band „Einspeisung BHKW“ (Herkunft *bilanziert*) — auch
  unterhalb der Toleranz, weil die Einspeisung eine echte Menge ist. Sonst ein Band „nicht zugeordnet“ (bei negativem Rest
  aus der Quellenspalte) und eine Hinweiszeile.
- **Wandler** schließen durch abgeleitete Bänder: Umweltwärme bzw. Umwelt (Kälte) = Abfluss − Strom; Verlust =
  Zufluss − Abfluss, nie negativ; Brennwertkessel je Modul `max(0, Wärme − Brennstoff)` als Kondensationswärme.
  Elektrokessel (Brennstoffwort „Strom“) hängt am Strom mit `Heizkessel.Stromverbrauch`.
- **Speicher** als Zwischenknoten: Ladung, Entladung je Kanal, Verlustband samt Füllstandsänderung; ein eigenes
  Füllstandsband ab 1 % der Ladung. Stromspeicher entladen an „Strombedarf“; mit Speicher ist die PV-Einspeisung
  `Einspeisung_Mit` der Speicherzeile (mit BHKW höchstens der PV-Überschuss). Quellspeicher (`Verwendung = Quelle`) stehen
  nicht im Bild.
- **Kältestrom mit eigenem Zähler** (abweichender Kühlträger und `Kuehl_EigenerZaehler`, wie
  `Kaeltekaskade.NebenDerStufenrechnung`) kommt aus der Quelle „Kältestrom (eigener Zähler)“, nicht über den Stromknoten;
  freie Kühlung (Wärmepumpe und Kältemaschine) steht als eigener Wandler.
- **Ungedeckt** (Wärme anteilig nach Kanalbedarf, Kälte ganz) ist ein Band aus der Quellenspalte in die Senke; der
  Senkenknoten trägt den ganzen Bedarf. Der Solarüberschuss steht nur im Zeigertext der Solarthermie.
- **Bündelung:** die kleinsten Brennstoffträger, Split-Zeilen bzw. Pufferspeicher, die zusammen unter 1 % des Busses
  bleiben, werden zu „weitere (n)“, wenn es mehr als drei sind; die Namen mit Menge stehen im Zeigertext (keine
  Wertetabelle, SK-Q12).
- **Zeigertexte:** Band „Von → Nach: x MWh/a (y %)“ (Anteil am Ziel bei Wandler, Bus, Speicher und Bedarf, sonst an der
  Quelle) mit „· bilanziert …“ bzw. „· abgeleitet: Formel“; Knoten „Name: Zufluss x · Abfluss y MWh/a“, Quellen und
  Senken „Name: x MWh/a“. Marken `fluss:<von>:<nach>`, `knoten:<id>`. Zahlen unter 10 mit zwei, sonst einer
  Nachkommastelle, Kultur nach Sprache.
- **KI-Flussliste** `EnergieflussModell.KiText()`: Kopf „[fluss] Energiefluss — Version“, je Band eine Zeile mit Herkunft,
  danach die Hinweise; ohne Bild der Grund.

## Bilanz der vier Referenzprojekte (Testdatenbank, eigener Lauf)

| Projekt | Größe | Flussmodell MWh/a | Konzept 1.2 |
|---|---|---:|---:|
| 1018 | Wärme Zufluss | 53,890 | 53,890 |
| 1018 | Puffer Ladung / Entladung / Verlust + ΔSOC | 38,949 / 37,781 / 1,168 | 38,949 / 37,781 / 1,169 |
| 1018 | Einspeisung BHKW (bilanziert) | 18,070 | 18,070 |
| 1046 | Wärme Zufluss · Heizstab · Wärmepumpe · Kessel | 71,410 · 37,810 · 25,090 · 8,510 | 71,410 |
| 1046 | Strom Zufluss | 69,110 | 69,110 |
| 1046 | Stromspeicher Ladung / Entladung / Verlust / Füllstand | 1,208 / 1,045 / 0,148 / 0,014 | 1,208 / 1,045 / 0,148 / 0,014 |
| 1046 | Einspeisung PV · nicht zugeordnet (Hinweis) | 0,903 · 0,435 | 0,903 · 0,44 |
| 1058 | Wärme Zufluss · Umweltwärme (= Erdreichentzug) | 72,030 · 43,090 | 72,030 · 43,090 |
| 1058 | Puffer Ladung / Entladung / Verlust + ΔSOC | 72,024 / 70,126 / 1,899 | 72,024 / 70,125 / 1,898 |
| 1058 | Kälte | 3,810 | 3,810 |
| 1058 | Einspeisung BHKW (bilanziert) · `Einspeisung_Mit` der Speicherzeile | 3,464 · 3,459 | 3,608 |
| 1063 | Wärme Zufluss · ungedeckt | 75,320 · 0,060 | 75,320 · 0,06 |
| 1063 | Kälte Zufluss · Kältespeicher Ladung / Entladung / Verlust | 4,150 · 2,478 / 2,376 / 0,102 | 4,153 · 2,478 / 2,376 / 0,102 |
| 1063 | Einspeisung BHKW (bilanziert) · `Einspeisung_Mit` der Speicherzeile | 11,771 · 11,768 | 10,815 |

Alle geprüften Knoten schließen; 1046 trägt als einziges Projekt das Band „nicht zugeordnet“ (Eigenverbrauch des
Speichersystems, nicht im Ergebnisbaum). **Abweichung von Tabelle 1.2:** Bei 1058 und 1063 lässt die Tabelle den
Stromspeicher (Verlust 0,186 bzw. 0,480, Füllstand −0,010) aus, bei 1063 zählt sie den Kältestrom des eigenen Zählers
(1,43) zum Stromknoten. Der Flussbau rechnet beides wie der Lauf; seine bilanzierte Einspeisung trifft die Einspeisung der
Speicherzeile auf 0,005 MWh/a, die Wache hält sie dagegen.

## Abnahme

- `dotnet build EPOS.Kern.Tests -c Release`: 0 Fehler.
- `dotnet test EPOS.Kern.Tests --filter "FullyQualifiedName~Energiefluss|FullyQualifiedName~Wache"`: 584 von 584 grün (davon 28 Energiefluss: 24 synthetisch, 4 Referenzprojekte).
- Keine SQL-Anweisung neu oder geändert (SQL-Dialekt-Prüfer nicht nötig); kein Schemaschritt; Referenzlauf unberührt.

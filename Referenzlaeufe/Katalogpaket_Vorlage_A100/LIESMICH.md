# Paketvorlage für die Nichtwohn-Nutzungsarten nach DIN EN 12831-3 Beiblatt A100

**Zweck (Anwenderentscheid ZU24).** Die Katalogtypen des Beiblatts A100 — Hotels, Krankenhäuser,
Sportstätten, Schulen, Bürogebäude und die übrigen Nichtwohnnutzungen — kommen **nicht** aus dem
Repositorium. Ihre Zahlen stehen in einer kostenpflichtigen Norm und dürfen hier nie liegen (Kapitel 6
des [Umsetzungskonzepts Zapfprofilgenerator](../../Dokumentation/aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md)).
Der Anwender führt sie als **eigenes Katalogpaket außerhalb des Repositoriums** und spielt es über den
Katalogimport ein. Dieser Ordner ist die **Vorlage** dafür: die vier Dateien des Importformats mit
vollständigen Kopfzeilen und je einer Beispielzeile aus **Platzhaltern**.

**Keine Normzahl.** Jede Zahl dieser Vorlage ist ein Platzhalter: 0, 1, 2, 3, 4, 5, 0,5, 10, 20, 30,
60 sowie der gleichverteilte Tagesgang (1/24 je Stunde) und die gleichverteilte Woche (1/7 je Tag).
Die Bedarfswerte 10 / 20 / 30 kWh je Einheit und Tag liegen bewusst außerhalb jeder plausiblen Spanne
— eine unausgefüllte Vorlage fällt damit sofort auf. Der Wächter
`EPOS.Kern.Tests/TwwKatalogimportTests.Die_Paketvorlage_A100_traegt_nur_Platzhalterzahlen` hält die
Dateien gegen diese Liste.

**Die gefüllte Datei gehört nie ins Repositorium.** Sie wird außerhalb aufbewahrt (beim Anwender, im
Auslieferungspaket), und `Referenzlaeufe/Normzahlen/` ist für den Lauf der Auslieferungsvorlage
ausdrücklich gesperrt.

## Die vier Dateien

| Datei | Zeilen der Vorlage | Inhalt |
|---|---|---|
| `Tab_TwwTagesgangsatz_STAMM.csv` | 1 | ein Tagesgangsatz: `ID` (Schlüssel des Pakets), `Bezeichner`, `Katalogversion` |
| `Tab_TwwTagesgang_STAMM.csv` | 4 | die vier Tagtypen des Satzes (1 Werktag, 2 Samstag, 3 Sonn-/Feiertag, 4 Ruhetag), je 24 Stundenanteile mit Summe 1, dazu Quelle, Ausgabe, Version, Herkunftsart |
| `Tab_TwwNutzungsart_STAMM.csv` | 1 | die Nutzungsart: Bezugsart, Bedarf niedrig/mittel/hoch, Bezugstemperaturen, Bilanzgrenze, Kalenderart, Ferienfaktor, zwölf Monatsfaktoren, sieben Wochenanteile, drei Provenienzgruppen (Bedarf, Jahresgang, Wochengang) und der Verweis `ID_Tagesgangsatz` auf den Satz |
| `Tab_TwwZapfkategorie_STAMM.csv` | 2 | ein **Vorgabesatz** der Gruppe `Nichtwohnen` ohne `ID_Nutzungsart`: je Kategorie Volumenstrom, Dauer, Anteil, Streuung, Kappung |

## Schritt für Schritt

1. **Den Ordner kopieren** — außerhalb des Repositoriums, etwa nach
   `…\EPOS-Katalogpakete\A100\`. In diesem Ordner hier wird nichts ausgefüllt.
2. **Je Typ eine Zeile** in `Tab_TwwNutzungsart_STAMM.csv` anlegen; `ID` fortlaufend, `Bezeichner`
   aus der Liste unten (oder ein eigener Name), `Katalogversion` für alle Zeilen desselben Pakets
   gleich (etwa `A100-1`).
3. **Je Typ einen Tagesgangsatz**: eine Zeile in `Tab_TwwTagesgangsatz_STAMM.csv` und vier Zeilen in
   `Tab_TwwTagesgang_STAMM.csv` (Tagtyp 1 bis 4). Mehrere Nutzungsarten dürfen denselben Satz teilen
   — dann zeigt ihr `ID_Tagesgangsatz` auf dieselbe `ID`.
4. **Die Werte aus dem eigenen Normexemplar eintragen.** Bedarf in kWh je Einheit und Tag bei den
   Bezugstemperaturen der Zeile (`Bezug_Zapftemperatur`, `Bezug_Kaltwasser`); Wertemengen: `Bezugsart`
   1 Personen, 2 Wohneinheiten, 3 Betten, 4 Duschplätze, 5 Sitzplätze, 6 Beschäftigte, 7 Fläche;
   `Bilanzgrenze` 1 Zapfstelle, 2 mit Verteilung, 3 mit Speicher; `Kalenderart` 1 Wohnen,
   2 Arbeitstage, 3 Schulferien, 4 Betrieb, 5 Auslastungsgang.
5. **Die Summenregeln halten:** jeder Tagesgang Summe 1, die sieben Wochenanteile Summe 1, die zwölf
   Monatsfaktoren im Mittel 1, die Anteile eines Kategoriensatzes Summe 1 (Toleranz 1e-9, Zahlen mit
   Dezimal**punkt**).
6. **Provenienz füllen:** `Quelle` (je Wertgruppe, etwa „DIN EN 12831-3 Beiblatt A100, Tabelle NA.…"),
   `Ausgabe` (Ausgabestand, darf leer bleiben), `Version` (Stand des Pakets) und `Herkunftsart` — für
   ein Normpaket des Anwenders `IMPORT`.
7. **Importieren:** in EPOS-Plan **Administration → Brauchwasser → Katalog-Import…**, dann den Ordner
   (oder eine Datei darin oder ein ZIP-Archiv) wählen. Der Bericht nennt je Nutzungsart „angelegt",
   „übersprungen" oder „abgelehnt" samt Grund.

**Die Kalenderart entscheidet über den Kategoriensatz.** Kalenderart 1 heißt Wohnen, jede andere
Nichtwohnen. Ein Vorgabesatz ohne `ID_Nutzungsart` bindet nur an Nutzungsarten **seiner Gruppe**
(Steuerspalte `Gruppe`: `Wohnen` oder `Nichtwohnen`); fehlt der Satz der Gruppe, ist die Nutzungsart
benannt abgelehnt. Eigene Kategorien einer einzelnen Nutzungsart tragen deren `ID_Nutzungsart` und
gehen einem Vorgabesatz vor.

**Ablehnungsgründe.** Das Paket wird als Ganzes abgelehnt (und nichts geändert), wenn eine Spalte
unbekannt ist, eine Pflichtspalte fehlt, eine Zeile eine andere Feldzahl als die Kopfzeile hat, eine
Zahl keine ist (Komma statt Punkt), eine `ID` doppelt vorkommt oder ein Tagtyp eines Satzes doppelt
steht; ohne `Tab_TwwNutzungsart_STAMM.csv` ebenfalls. **Nur die einzelne Nutzungsart** wird abgelehnt,
wenn ihr eine Pflichtangabe oder ein Provenienzfeld fehlt, ein Wert außerhalb seiner Wertemenge liegt,
eine Summenregel verletzt ist, ihr Tagesgangsatz unvollständig ist oder der Vorgabesatz ihrer Gruppe
fehlt. Führt der Katalog denselben Bezeichner in derselben Katalogversion, wird bei gleichem Inhalt
übersprungen und bei abweichendem als „Bezeichner (Import n)" angelegt — vorhandene Zeilen ändert der
Import nie.

**Der Weg über die Auslieferungsvorlage** (`Werkzeuge/Auslieferungsvorlage --katalogpaket <ordner>`)
verlangt dieselben Dateien mit `Status = AUSLIEFERUNG` und `ReadOnly = 1` in jeder Zeile und eine
andere Herkunftsart als `IMPORT` — die Prüfung der Vorlage weist einen Normimport ab. Diese Vorlage
hier ist auf den **Katalogimport beim Anwender** eingestellt.

## Empfohlene Typnamen (nur Namen, keine Werte)

Aus der Auswertung
[`Grundlagen_3_DIN-EN-12831-3_A1_A100_Auswertung.md`](../../Dokumentation/aktuell/Grundlagen_3_DIN-EN-12831-3_A1_A100_Auswertung.md):

- Hotel nach Größe und Sterneklasse: „Hotel, 1 Stern", „Hotel, 2 Sterne", „Hotel, 3 Sterne",
  „Hotel, 4 Sterne", „Hotel, 5 Sterne" — jeweils mit und ohne Wäscherei; „Einfaches Hotel",
  „Mittelklasse-Hotel", „Luxusklasse-Hotel"; „Messehotel"
- „Hotelküche", „Gastronomie", „Großküche, Kantine"
- Krankenhaus nach Bettenzahl: „Krankenhaus, kleine Bettenzahl", „Krankenhaus, mittlere Bettenzahl",
  „Krankenhaus, große Bettenzahl"; „Klinikum Funktionsgebäude"
- „Schule ohne Duschen", „Schule mit Duschen"
- „Sportstätte mit Duschen", „Schwimmbad"
- „Bürogebäude"
- „Werkstatt, Industriewerk mit Wasch- und Duschgelegenheiten"
- „Kaserne", „Justizvollzugsanstalt, Zellentrakt"
- „Studentenwohnheim", „Seniorenwohnheim" (der Katalog führt dafür schon abgeleitete Typen nach
  VDI 6002 — ein A100-Typ tritt neben sie, nicht an ihre Stelle)

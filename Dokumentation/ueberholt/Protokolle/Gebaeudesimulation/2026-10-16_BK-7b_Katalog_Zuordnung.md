# Protokoll BK-7b — Teilkataloge aus `Katalog_Zuordnung.csv`

Nachzug zu [BK-7](2026-10-16_BK-7_Teilkataloge_Exportordner.md), Anwenderentscheid vom 16.10.2026. Arbeitsort Worktree
`bk7b`, Zweig `bk7b`, Basis `85f7272fa`. Kein Schemaschritt.

## Befund

- BK-7 sucht die Teilkataloge eines Satzes über `Katalog.csv` und `Katalog_Inhalt.csv`. Am echten Export trifft das 0 Sätze:
  `Katalog_Inhalt.csv` ist nur der Baum des Katalogs, keine der gelesenen Dateien verbindet Baum und Satz.
- Die Verknüpfung steht in einer eigenen Tabelle, die der lokale Export künftig als `Katalog_Zuordnung.csv` liefert.
  Zählungen am echten Export (nur Zahlen): 193 952 Zeilen, 37 500 treffen einen Satz (37 499 über `BasisUId`, 408 über
  `DimensionUId`); Baustoffe 10 436 von 10 719 mit Teilkatalog, davon 45 mit genau zwei verschiedenen, sonst genau einer;
  Aufbauten 8 392 von 8 427, je genau einer; bei 5 Zeilen weicht `CatalogUId` vom Teilkatalog des Inhaltsknotens ab.
- BK-7 speichert die Teilkataloge nicht in einer eigenen Spalte, sondern als Text in der Quelle des Satzes
  (`…, Teilkatalog <Name>` bzw. `…, Teilkataloge <A> / <B>`); eine Liste ist damit schon möglich. Ein Schemaschritt ist
  nicht nötig.

## Vertrag der Datei

- Format wie die übrigen Dateien des Exports: UTF-8 ohne BOM, Trennzeichen `;`, CRLF, Dezimalpunkt, leere Kennung = leeres
  Feld. Kopf genau `UId;CatalogContentUId;CatalogUId;BasisUId;DimensionUId;StateType;SortNum`, keine Namensspalten.
- `CatalogUId` → Teilkatalog (`Katalog.csv.UId`, Name aus der Namensspalte wie in BK-7); `CatalogContentUId` → Knoten in
  `Katalog_Inhalt.csv`.
- Satzseite: `BasisUId` trifft beim Baustoff die Basiszeile (`Basis_Auszug.csv.UId`, deren `MaterialUId` auf den Baustoff
  zeigt), beim Aufbau dessen `BasisUId`; `DimensionUId` trifft die Kennung `UId` eines Aufbaus.

## Entscheide

1. **Vorrang:** Liegt `Katalog_Zuordnung.csv` im gelesenen Ordner oder in dessen Unterordner `Export`, gilt sie; der Weg über
   `Katalog_Inhalt.csv` entfällt dann. Fehlt sie, ist sie unlesbar, fehlt eine der Spalten `CatalogUId`, `BasisUId`,
   `DimensionUId` oder ist ein Datensatz kaputt, gilt der BK-7-Weg; das Protokoll nennt den Grund
   („Datei fehlt, Teilkataloge aus Katalog_Inhalt.csv: …“, „… Spalte fehlt (…), Teilkataloge aus Katalog_Inhalt.csv“).
2. **Treffer:** Gesucht werden `BasisUId` und `DimensionUId` gegen dieselben Ziele wie in BK-7 (Basiszeile → Baustoff über
   `MaterialUId`, `BasisUId` des Aufbaus → Aufbau, `UId` → Satz). `GId` und `VariantRoot` werden nicht herangezogen: Der
   Vertrag nennt sie nicht, und eine Variantenwurzel meint mehrere Sätze.
3. **Maßgeblich ist `CatalogUId`**, auch wenn der Inhaltsknoten im Baum unter einem anderen Teilkatalog hängt. Die
   Abweichungen zählt das Protokoll („CatalogUId weicht vom Teilkatalog des Inhaltsknotens ab, CatalogUId gilt: n“).
4. **Zwei Teilkataloge:** Die Quelle nennt beide (`Teilkataloge A / B`), wie bei der zusammengeführten Dublette aus BK-7 —
   die Zuordnung ist in beiden Teilkatalogen gleich gültig, eine Auswahl nach `SortNum` hätte eine Angabe des Exports
   verworfen. Die Vorschau zählt diese Sätze.
5. **Ohne Treffer** bleibt ein Satz ohne Teilkatalog; Zeilen mit Satz, aber ohne benannten Teilkatalog (Kennung fehlt in
   `Katalog.csv` oder ohne Namen) zählt das Protokoll.
6. **Vorschau:** Teilkataloge der Quelle (`Katalog.csv` mit Kennung und Name), Weg der Zuordnung (Datei), Baustoffe und
   Aufbauten mit Teilkatalog, mit zwei oder mehr und ohne Teilkatalog — im Protokoll und in der Zeile „Teilkataloge“ des
   Dialogs (`BKI_TEILKATALOGE`, beide Sprachen); der Hinweis des Dialogs (`BKI_HINWEIS`) nennt die neue Datei.

## Ergebnis

- `BauteilkatalogQuelle`: Datei `Katalog_Zuordnung.csv`, defensiv gelesen, auch aus dem Unterordner `Export`.
- `BauteilkatalogTeilkataloge.Zuordnen`: Weg über `Katalog_Zuordnung.csv` vor dem BK-7-Weg; der Baum von
  `Katalog_Inhalt.csv` dient der Abweichungsprobe. Rückgabe mit Zahl der Teilkataloge der Quelle und Weg.
- `BauteilkatalogVorschau`: `TeilkatalogeInQuelle`, `TeilkatalogeAusZuordnung`, `Baustoffe-`/`AufbautenMitMehrerenTeilkatalogen`,
  `Baustoffe-`/`AufbautenOhneTeilkatalog`; Hülle `BauteilkatalogImportHuelle` setzt sie in die Vorschauzeile.
- Rechenweg unberührt: Die Teilkataloge stehen nur im Text der Quelle; kein Referenzlauf nötig.

## Tests

`EPOS.Kern.Tests/BauteilkatalogImportTests`, synthetische Quelle mit neutralen Namen, vier neue Fälle:

- Vorrang der Zuordnung: Baustoff über Basiszeile (Inhaltsknoten in anderem Teilkatalog — CatalogUId gilt, Abweichung 1),
  Aufbauten über `BasisUId` und über `DimensionUId`, Baustoff mit zwei Teilkatalogen, Zeile ohne Treffer, zwei Zeilen ohne
  benannten Teilkatalog; Satz nur in `Katalog_Inhalt.csv` bleibt ohne Teilkatalog; alle Vorschauzählungen.
- Rückfall: Datei fehlt, Spalte `CatalogUId` fehlt, kaputter Datensatz → BK-7-Weg mit Hinweis; Datei ohne Treffer → benannt
  ohne Teilkatalog.
- Unterordner `Export`: ganzer Export im Unterordner; nur die Zuordnung im Unterordner.
- Wiederimport gegen die Testdatenbank: zweiter Lauf ohne neue Sätze, Zeilenzahlen und Quellen unverändert.

## Abnahme

- `WP-Plan.Kern.slnf` (Release): 0 Fehler. Windows-Schale (`-p:EnableWindowsTargeting=true`): 0 Fehler.
- `EPOS.Kern.Tests` mit Filter `Bauteilkatalog`, `DokumentationLinkWache`, `WikiProduktdatenWache`,
  `RepositoryOrdnungWache`, `KiMasken`, `HelpMapping`, `KiBereich`, `KiDialog`: 241 von 241 grün (darunter 70 der
  Bauteilkatalog-Klassen, davon 4 neu).
- `EPOS.UI.Tests` mit Filter `BauteilkatalogImport`, `KiMasken`, `Hilfe`: 758 von 758 grün.
- Kein SQL berührt, SQL-Dialekt-Prüfer entfällt. Ressourcendesigner geschrieben, zweiter Lauf +0 (wiederholbar).
- Kein Referenzlauf: Der Rechenweg liest die Quelle nicht.

## Vorschlag Logbuch

„Der Bauteilkatalog-Import liest die Teilkataloge aus der Datei Katalog_Zuordnung.csv des Exports und zeigt in der Vorschau, wie viele Baustoffe und Aufbauten einen, mehrere oder keinen Teilkatalog tragen.“

## Offen

- Erster Lauf mit dem echten Export samt `Katalog_Zuordnung.csv` beim Anwender; erwartete Vorschau: Baustoffe 10 436 mit
  Teilkatalog (45 mit zwei), Aufbauten 8 392, Abweichungen 5 (Zahlen vor Dublettenzusammenführung und Ablehnungen).
- Wiki-Upload und Logbuch-Eintrag mit dem nächsten Sammel-Upload.

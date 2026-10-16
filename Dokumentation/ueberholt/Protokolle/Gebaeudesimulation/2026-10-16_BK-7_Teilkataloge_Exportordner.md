# Protokoll BK-7 — Teilkataloge in der Quelle, Unterordner `Export`, Hilfeschlüssel des Importdialogs

Welle BK-7 aus dem [Entwurf Bauteilkatalog](../../../aktuell/Gebaeudesimulation/2026-10-12_Entwurf_Bauteilkatalog_Hottgenroth.md)
(Abschnitte 2.3 Regel 12 und 7), Punkt 4 der
[Übergabe](../../../aktuell/Gebaeudesimulation/2026-10-12_Uebergabe_Gebaeudesimulation_Kontowechsel.md). Arbeitsort Worktree
`bk7`, Zweig `bk7`, Basis `836407033` (Runde r6). Kein Schemaschritt.

## Ausgangslage

- Die Quelle eines eingelesenen Satzes nannte nur Präfix und Datenstand; eine in der Quelle zusammengeführte Dublette nannte
  keinen ihrer Teilkataloge (offener Punkt aus dem Protokoll BK-3).
- `Katalog.csv` und `Katalog_Inhalt.csv` liegen im Export, ihre Spaltennamen führt das Feldinventar nicht; der Strukturbericht
  nennt nur den Baum (Elternknoten über einen Verweis) und die Zeilenzahlen.
- Der Dialog verlangte `Baustoffe.csv` im gewählten Ordner; der Export des Anwenderskripts legt die Dateien in einen
  Unterordner `Export`.
- Der Dialog trug den Hilfeschlüssel des Baustoffkatalogs.

## Ergebnis

- **Teilkataloge** (`EPOS.Kern/Allgemein/Import/Bauteilkatalog/BauteilkatalogTeilkataloge.cs`): Beide Dateien sind Kann-Dateien
  und werden defensiv gelesen — fehlt eine, ist sie unlesbar, hat eine Zeile die falsche Feldzahl, fehlt in `Katalog.csv` die
  Kennung `UId` oder eine Namensspalte (`ShortDesc`, `Name`, `Description`, `Caption`, `Title`, `Bezeichnung`, `LongDesc`, die
  erste vorhandene gilt) oder trifft keine Zeile einen Satz, steht ein benannter Hinweis im Protokoll und der Import läuft
  ohne Teilkatalognamen weiter. Die Zuordnung hängt an keiner Verweisspalte: Jede Zeile von `Katalog_Inhalt.csv` wird nach
  Kennungen durchsucht; eine Kennung aus `Katalog.csv` ist ihr Teilkatalog, eine Kennung eines Baustoffs, Aufbaus oder einer
  Basiszeile (`Basis_Auszug.csv` bzw. `BasisUId` des Aufbaus) ihr Inhalt, eine Kennung einer anderen Zeile ihr Elternknoten
  (bis 32 Stufen, nächster Knoten mit Teilkatalog gilt).
- **Quelle:** „Hottgenroth-Katalog, Datenstand JJJJ-MM, Teilkatalog X“, bei mehreren „…, Teilkataloge X / Y“; eine in der
  Quelle zusammengeführte Dublette nennt alle Teilkataloge ihrer Sätze. Höchstens 400 Zeichen (`QUELLE_MAX_ZEICHEN`),
  überzählige Namen als „u. a.“. Am Katalogsatz (Ergänzen) hängt der Import die Quelle des Satzes samt Teilkatalog an.
- **Unterordner `Export`:** Liegt `Baustoffe.csv` nicht im gewählten Ordner, aber in dessen Unterordner `Export`, liest der Kern
  diesen; Hinweis im Protokoll, die Vorschau nennt den gelesenen Ordner (Zeile `epos-bki-ordner`) und die Teilkataloge
  (Zeile `epos-bki-teilkataloge`). Texte `BKI_ORDNER`, `BKI_ORDNER_EXPORT`, `BKI_TEILKATALOGE` neu, `BKI_HINWEIS` ergänzt,
  beide Sprachen, Designer nachgezogen.
- **Hilfe:** eigener Schlüssel `BauteilkatalogImport.btn_Help` → „Baustoffe und Bauteilaufbauten#bauteilkatalog-einlesen“
  (`help_mapping.txt`); der Bereich des Assistenten folgt dem vorhandenen Präfix „Bauteil“ (Gebäude). Der Dialog hat außer
  der Dateiwahl kein Eingabefeld — nach `KiMaskenabdeckungWacheTests` keine Maske mit Eingabe —, deshalb kein Eintrag in
  `KiMaskennamen`, `KiDialoge.Katalog` und `KiMaskenanmeldung`; der InfoKnopf führt den Assistenten.
- **Wiki-Quelle** „Programm Dokumentation - Baustoffe und Bauteilaufbauten“, Abschnitt „Bauteilkatalog einlesen“: Unterordner
  `Export`, die zwei Kann-Dateien und der Teilkatalog in der Quelle; Gegenlesemuster ohne Treffer, keine Hersteller- oder
  Produktnamen. Logbuch-Vorschlag (mit dem nächsten Sammel-Upload): „Bauteilkatalog einlesen nennt den Teilkatalog in der
  Quelle und liest auch den Unterordner Export.“

## Tests

- `EPOS.Kern.Tests/BauteilkatalogImportTests`: fünf neue Fälle mit erfundenen Namen („Teilkatalog A“, „Teilkatalog B“) und
  erfundenen Verweisspalten — Zuordnung direkt, über Basiszeile, über Elternknoten und über die Dublette; fehlende Datei,
  fehlende Namensspalte, kaputte Zeile, kein Treffer; Höchstlänge; Unterordner `Export`; Übernehmen in die Testdatenbank samt
  Anhängen am Katalogsatz.
- `EPOS.UI.Tests/Dialoge/BauteilkatalogImportDialogTests`: Ordner- und Teilkatalogzeile nur, wenn vorhanden; Hilfeschlüssel
  und Hinweistext.

## Abnahme

- `WP-Plan.Kern.slnf` (Release): 0 Fehler. Windows-Schale (`-p:EnableWindowsTargeting=true`, `--no-incremental`): 0 Fehler.
- `EPOS.Kern.Tests` mit Filter `Bauteilkatalog`, `KiMasken`, `HelpMapping`, `KiBereich`, `KiDialog`,
  `WikiProduktdatenWache`, `DokumentationLinkWache`: 222 von 222 grün.
- `EPOS.UI.Tests` mit Filter `BauteilkatalogImport`, `KiMasken`, `InfoKnopf`, `Hilfe`, `KiKnopf`, `Menueband`: 698 von 698 grün.
- SQL-Dialekt-Prüfer: 3 009 SQL-Texte, 0 Fundstellen. Ressourcendesigner wiederholbar (+0).

## Offen

- Welche Ebene des Katalogbaums der Anwender als „Teilkatalog“ liest, zeigt erst der erste Lauf mit dem echten Export: Der
  Leser nimmt den Knoten aus `Katalog.csv`, auf den die Zeile oder ihr nächster Elternknoten verweist. Verweist der Export
  umgekehrt vom Elternknoten auf die Kinder, bleibt die Quelle ohne Teilkatalog (Hinweis „ohne Teilkatalog“ im Protokoll).
- Wiki-Upload und Logbuch-Eintrag mit dem nächsten Sammel-Upload.

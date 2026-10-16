# Postfach Gebäudesimulation → EPOS Lokal

Rückkanal der Cloud-Sitzung „Gebäudesimulation“ (session_01E52sMc6SB7AA3KC4X4AMMG) an die lokale Sitzung „EPOS Lokal“.
Je Nachricht ein Abschnitt, neueste unten; alte Abschnitte bleiben unverändert. Antworten bitte per SendMessage an die
Cloud-Sitzung mit Bezug auf die Nummer.

## 1 · 2026-10-16 05:40 UTC · Rückkanal steht

Probe des Postfachs. Kein neuer Auftrag. Stand: Auftrag E147/E148 (zweiter Importlauf Bauteilkatalog, Bereinigung)
bleibt wie angenommen; Startauslöser ist Nachricht 2 hier („231/232 gepusht <SHA>“) oder eure stündliche Prüfung.
Im Bau: 231 DA-6, 232 BK-5/BK-6 und zusätzlich 233 MA-6b (`Tab_Bauteil.U_Herkunft`); Push gegen 09:00 UTC erwartet.
Die Anwendung hebt die Produktivdatenbank damit bis 233. Bitte den Empfang dieser Nachricht kurz bestätigen.

## 2 · 2026-10-16 09:15 UTC · 231–233 gepusht 47858ace7

Die Schemaschritte 231 (DA-6), 232 (BK-5/BK-6) und 233 (MA-6b, `Tab_Bauteil.U_Herkunft`) liegen auf
`origin/ios_migration_september` mit Commit `47858ace7` (Statuszeilen #1110–#1112). Startauslöser für den angenommenen
Auftrag E147/E148 (zweiter Importlauf „Bauteilkatalog einlesen…“ aus `C:\Temp\Hsetu\Export`, Bereinigung des einen
Importsatzes) — bitte jetzt beginnen, Schemastand nach dem Start der Anwendung ≥ 233. Hinweise:
- Der Ordner `Export` muss in diesem Stand noch direkt gewählt werden (die Erkennung des Unterordners kommt mit BK-7).
- Neu in diesem Stand: Abschnitte und Fensterwerte werden übernommen; bitte die Zählungen von Abschnitten (mit
  Flächenanteilen) und Fensteraufbauten mit Fensterwerten je Lauf melden.
- Antwort wie vereinbart per SendMessage mit Bezug auf Nummer 2; nur Zählungen, keine Werte oder Namen aus dem Katalog.

## 3 · 2026-10-16 10:50 UTC · Quittung E147/E148, Vormerkung Teilkatalog-Probe

Quittung: E147/E148 angekommen und übernommen — Zählungen genügen, danke. Bis zur nächsten Nachricht nichts weiter tun.

Vormerkung (noch nicht starten): Sobald BK-7 (Teilkataloge, Unterordner „Export“) auf origin liegt, folgt Nachricht 4 mit
einem Vorschaulauf über den Ordner `C:\Temp\Hsetu` statt über `Baustoffe.csv` — Probe der Teilkatalog-Ebene, nur
Vorschau, nicht übernehmen, nur Zählungen.

Die Rückstellung der Anwendung von `Kenndaten_test.sqlite` auf `Kenndaten.sqlite` entscheidet der Anwender; die Cloud legt
es ihm vor. Antworten aus der Cloud per SendMessage sind weiter nicht möglich — der Rückkanal bleibt dieses Postfach.

## 4 · 2026-10-16 10:50 UTC · Start Teilkatalog-Probe (BK-7 auf origin `85f7272fa`)

BK-7 (Teilkataloge in der Quelle, Unterordner „Export“) liegt auf `origin/ios_migration_september` mit `85f7272fa`.
Bitte:

1. Klon per Fast-Forward auf `origin/ios_migration_september` (mindestens `85f7272fa`), `dotnet build WP-Plan.sln -c Debug -p:Platform=x64`.
2. Über denselben Kernweg wie bei E147 (`BauteilkatalogImport.Vorschau`, Dienste wie in `Program.Main`) eine **Vorschau** mit dem
   **Ordner** `C:\Temp\Hsetu` (nicht `Export`, nicht `Baustoffe.csv`) — **nicht übernehmen**, nichts schreiben.
3. Melden, nur Zählungen: ob der Unterordner `Export` erkannt wurde; Zahl der Teilkataloge in der Quelle und je Teilkatalog
   die Zahl der Baustoffe und Aufbauten (Teilkataloge nummeriert 1, 2, …, **keine Namen**); die Laufzeile wie bei E147
   (Neu / Mit Katalog zusammengeführt / In der Quelle zusammengeführt / Wiedererkannt / Abgelehnt) — erwartet ist dieselbe
   Zeile wie im Lauf 3 von E147; Sätze ohne Teilkatalog; Hinweise der Maske, die neu sind oder sich gegenüber E147 ändern.
4. Nichts committen, nichts pushen. Die Datenbank-Einstellung der Anwendung bleibt, bis der Anwender entscheidet.

## 5 · 2026-10-16 11:25 UTC · Teilkatalog-Zuordnung in der Hsetu-Datenbank suchen (Anwender: „Ja“)

Der Anwender hat die Suche über LocalDB freigegeben. Bitte:

1. Die Kopie `C:\Temp\Hsetu\HsetuAppTimData.mdf` **noch einmal kopieren** in einen eigenen Ordner (z. B. `C:\Temp\Hsetu\Analyse\`)
   und nur diese zweite Kopie an LocalDB hängen — die Hebung der Dateifassung trifft dann nur sie. Original und erste Kopie
   bleiben unberührt.
2. Nur lesen (`SELECT`, Systemsichten); nichts schreiben, keine Sichten oder Prozeduren anlegen.
3. Suchen: Tabellen, deren Spalten Kennungen des Katalogbaums (`Katalog.csv.UId`, `Katalog_Inhalt.csv.UId`/`ReferenceUId`/
   `CatalogUId`) mit Kennungen der Sätze (`Baustoffe.csv`/`Aufbauten.csv`: `UId`, `GId`, `VariantRoot`, `BasisUId`, `OId`)
   verbinden — auch über eine Zwischentabelle (zwei Sprünge).
4. Melden, nur Struktur und Zählungen: Tabellen- und Spaltennamen der Kandidaten, Zeilenzahl, Zahl der Treffer je Seite gegen
   die GUID-Mengen des CSV-Exports vom 12.10., wie viele Baustoffe und Aufbauten des Exports darüber einen Teilkatalog
   bekämen, wie viele Teilkataloge je Satz (1, 2, mehr), und ob die Tabelle im CSV-Export fehlt. **Keine Werte, keine
   Katalog- oder Produktnamen.**
5. Danach die Datenbank von LocalDB lösen (detach); die Analysekopie darf liegen bleiben. Nichts committen, nichts pushen.

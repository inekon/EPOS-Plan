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

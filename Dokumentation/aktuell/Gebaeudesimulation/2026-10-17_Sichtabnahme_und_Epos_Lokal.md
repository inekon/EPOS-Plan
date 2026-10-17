# Sichtabnahme unter Windows, Auftrag an Epos Lokal und Sportheim-Datei — Stand 17.10.2026

Dieses Papier setzt die Anwenderentscheide [E163 und E164](../Status_Gebaeudesimulation_VDI6007.md) vom 17.10.2026 um
(Wortlaut: „1. und 2. nächste Woche, 3 bis 6: setze um“). Es enthält die Prüfliste der gemeinsamen Sichtabnahme unter Windows
(Punkt 3, E146), den Auftrag an Epos Lokal für die Übernahme der 7 Aufbauten (Punkt 4) und die Handlung des Anwenders für die
Sportheim-Datei (Punkt 5). Punkt 6 (Verfeinerungen des Imports aus der Projektdatei) läuft als Welle IV in einer anderen Arbeit;
Versionsnummer und Weg des Wiki-Sammel-Uploads (Punkte 1 und 2) entscheidet der Anwender in der Woche ab 19.10.2026 (E163).
Übergabestand: [Übergabepapier](2026-10-12_Uebergabe_Gebaeudesimulation_Kontowechsel.md), Abschnitte 3 und 5.

## 1. Prüfliste Sichtabnahme Windows

Voraussetzung: Anwendung auf dem Stand von `origin/ios_migration_september` (mindestens `26c01427c`), ein Projekt mit einem
importierten Gebäude und die IFC-Datei samt Projektdatei zum Importieren. Die Klickwege folgen den Oberflächentexten und den
Wiki-Quellen (`Projekte/Wiki/Programm Dokumentation - Gebäude.wiki`, `… - Gebäudeimport.wiki`,
`… - Baustoffe und Bauteilaufbauten.wiki`). Die letzte Spalte füllt der Anwender mit ok, nicht ok oder einer Bemerkung.

### 1.1 Ampelleiste, Klimaregion und Wochenvorschau (A-N5-1 bis A-N5-4, E146)

| Nr. | Ort (Klickweg) | Prüfung | erwartet | ok/nicht ok/Bemerkung |
|---|---|---|---|---|
| 1 | Gebäudedialog („Eingabe der Gebäudedaten“) › Zeile im Projekt wählen › „Modell prüfen…“ | Das Blatt „Modell prüfen“ öffnet für die gewählte Zeile. | Je Prüfgruppe eine Zeile mit Ampel (grün, gelb oder rot) und Meldungstext. | |
| 2 | „Modell prüfen…“ › Gruppe 4 › „Klimaregion wählen…“ (A-N5-1) | Die Liste der Klimaregionen zeigt Kurzform und Einträge wie die Kopfleiste. | Die Region des Projekts ist vorbelegt. | |
| 3 | Dieselbe Liste › andere Region wählen › OK (A-N5-2) | Die Meldung erscheint, die Kopfleiste der Startseite wechselt, das Ampelblatt prüft neu. | Meldung „Die Klimaregion „…“ ist für das Projekt gespeichert.“; neue Region in der Kopfleiste; Ampelblatt neu geprüft. | |
| 4 | Gebäudeeditor › Reiter „Zonen“ › Leiste Gruppe 4, Region wechseln (A-N5-3) | Bildunterschrift und Teppichbild der Konditionierung nennen das Raster der neuen Region. | Die Wochenvorschau steht sofort richtig, ohne Neuöffnen. | |
| 5 | „Modell prüfen…“ › Gruppe 3 übernehmen (A-N5-4) | Die Wochenvorschau der Konditionierung zeigt den neu gelesenen Stand. | Ohne weitere Handlung steht der übernommene Stand in der Vorschau. | |
| 6 | „Modell prüfen…“ › Zeile einer Gruppe › „Ansicht…“ | Die Gebäudeansicht öffnet im Farbmodus der Gruppe, betroffene Bauteile und Zonen sind hervorgehoben. | Bei einem Gebäude ohne Importdatei ist der Knopf gesperrt, der Kurztext nennt den Grund. | |

### 1.2 Fensterteil, Abschnitte und Gebäudedialoge

| Nr. | Ort (Klickweg) | Prüfung | erwartet | ok/nicht ok/Bemerkung |
|---|---|---|---|---|
| 7 | Gebäudeeditor › Bauteil vom Typ Fenster › Dialog des Bauteils › Fensteraufbau wählen (BK-6) | Die Fensterwerte werden aus dem Fensteraufbau übernommen. | g-Wert, Rahmenanteil und U_w stehen im Bauteil; Hinweis „Aus dem Fensteraufbau übernommen: g-Wert, Rahmenanteil und U_w = … W/(m²K).“; Felder bleiben von Hand änderbar. | |
| 8 | Bauteildialog › Zeile „Herkunft des U-Werts“ (E152) | Die Zeile ist nur lesbar und nennt die Herkunft. | Eine von: von Hand, Katalog, Datei (IFC), Datei (gbXML), Datei (Projektdatei), Vorgabe; ohne Herkunft ein Strich. | |
| 9 | Bauteildialog › U-Wert oder Aufbau ändern | Die Zeile „Herkunft des U-Werts“ zeigt sofort die Herkunft, die das Bauteil beim OK erhält. | Bei Eingabe von Hand „von Hand“, bei gewähltem Katalogaufbau „Katalog“. | |
| 10 | Bauteildialog › „Absorptionsgrad solar“ und „Emissionsgrad“ (DA-8, #1135) | Beide Felder nehmen Werte von 0 bis 1 an und lassen sich leeren. | Leer gilt der Wert des Aufbaus, sonst die Vorgabe 0,6 bzw. 0,9; Werte außerhalb 0 bis 1 werden abgewiesen. | |
| 11 | Administration › Gebäude › Bauteilaufbauten (Baustoffe und Bauteilaufbauten) › Aufbau mit inhomogener Schicht | Abschnitte und Flächenanteile stehen je Schicht; der Summenfuß zeigt R′T und R″T. | Mehrere Abschnitte nebeneinander, Flächenanteile ergeben 100 %; Summenfuß mit oberem und unterem Grenzwert statt eines Einzelwerts. | |
| 12 | Gebäudedialog › „Bearbeiten…“ › Reiter „Zonen“ › „Nutzung und Kalender…“ | Das Blatt zeigt Profil, Kalender, Sollwerte, Ferien und Heizperiode aller Zonen. | Eine Handlung gilt für alle gewählten Zonen; die Ampel der Gruppe „Nutzung und Konditionierung“ steht im Blatt. | |
| 13 | Gebäudeansicht › Farbmodus (fünf Knöpfe) | Alle fünf Farbmodi sind sichtbar: Zonen, Randbedingung, Aufbau, Vollständigkeit, Nutzung. | Beim Wechsel eines Modus setzen sich die Schalter der Legende zurück, die Wahl der Bauteilgruppen bleibt. | |
| 14 | Gebäudeansicht › Farbmodus „Vollständigkeit“ (E145) | Bauteile sind nach dem gefärbt, was ihnen fehlt. | Körper unlesbar oder ohne U-Wert oder Fläche orange oder rot; die Legende zählt die Bauteile je Stufe; ohne Bauteilvorschlag ist der Modus gesperrt. | |
| 15 | Gebäudeansicht › Legende „Vollständigkeit“ › Spalte „Vorschläge“ | Die Knöpfe „Übernehmen“ und „Verwerfen“ wirken auf genau die Zeile. | Nach „Übernehmen“ ändert sich der Zähler der Zeile; „Verwerfen“ schreibt nichts. | |

### 1.3 Import aus IFC und Projektdatei

| Nr. | Ort (Klickweg) | Prüfung | erwartet | ok/nicht ok/Bemerkung |
|---|---|---|---|---|
| 16 | Gebäudedialog › „Importieren (gbXML, IFC)…“ › Feld „Quelle“ | Die Quellenwahl bietet IFC, gbXML und „Alle Formate (nach Dateiendung)“. | Nach dem Lesen nennt der Abschnitt „Quelle“ Datei, Format, Schema, Größe und Zonenregel. | |
| 17 | Importdialog › Quelle „IFC + Projektdatei“ | Es bleibt zu entscheiden, ob sich der zweite Dateidialog (Projektdatei) von selbst öffnen soll. Frage an den Anwender. | Der Anwender notiert in der Bemerkung: „von selbst öffnen“ oder „wie heute auf Knopfdruck“. | |
| 18 | Importdialog › nach dem Lesen mit Projektdatei › Wahlfeld „Zonierung“ | Das Wahlfeld bietet „DIN-V-18599-Zonen“ und „Simulationszonen“. | Die Wahl ändert die Zonen; ist nur eine Zonierung vorhanden, nennt die Protokollzeile es. | |
| 19 | Importdialog › Zonenliste › Spalte Herkunft | Jede Zone nennt ihre Herkunft. | „aus Projektdatei (DIN-Zonen)“ bzw. „aus Projektdatei (Simulationszonen)“ oder die Zonenregel. | |
| 20 | Importdialog › Liste „Flächen je Zone“ › Filter über der Liste | Die drei Filter „Nur Flächen mit Hinweis“, „Nur Flächen ohne Nachbarfläche“, „Nur Flächen ohne U-Wert und Aufbau“ sind lesbar benannt. | Mehrere Filter gelten zusammen („und“); sie wirken nur auf die Anzeige, die Kurztexte sind verständlich. | |
| 21 | Importdialog › IFC- und Projektdatei mit abweichenden Aufbauten | Die Rückfrage „Welche Aufbauten gelten?“ öffnet. | Der Text nennt den Prozentsatz der Hüllfläche; die Länge des Dialogs passt in das Fenster, ohne unnötig zu rollen. | |
| 22 | Rückfrage „Welche Aufbauten gelten?“ | Drei Knöpfe sind sichtbar und eindeutig beschriftet. | Jeder Knopf führt zur genannten Wirkung; Abbrechen übernimmt nichts. | |
| 23 | Rückfrage „Welche Aufbauten gelten?“ bei schmalem Fenster (Fensterbreite verkleinern) | Die Tabellen der Rückfrage bleiben lesbar. | Kein abgeschnittener Text, kein waagerechtes Rollen der ganzen Seite. | |
| 24 | Importdialog › Einzonen- und Mehrzonenweg mit Kalendern in den Räumen (N7/N8) | Die Zonen übernehmen ihre Kalender auch ohne gespeicherten Zonenplan. | Trägt jeder beheizte Raum denselben Kalender, übernimmt die eine Zone ihn; sonst steht eine Warnung in der Meldungsliste mit Hinweis auf eine Zonenregel. | |

### 1.4 Bauteilkatalog einlesen

| Nr. | Ort (Klickweg) | Prüfung | erwartet | ok/nicht ok/Bemerkung |
|---|---|---|---|---|
| 25 | Administration › Gebäude › „Bauteilkatalog einlesen…“ | Der Dialog arbeitet in zwei Schritten: Datei wählen, Vorschau prüfen. | Gewählt wird „Baustoffe.csv“ des Exports; die übrigen CSV-Dateien liegen daneben. | |
| 26 | Dialog › Vorschau (BK-7, BK-9) | Die Vorschau zählt je Satzart, was neu, zusammengeführt, wiedererkannt oder abgelehnt ist. | Zählungen für Baustoffe, Aufbauten, Schichten und Abschnitte; die Zeile der Teilkataloge nennt Zahl der Teilkataloge und der Sätze mit Teilkatalog. | |
| 27 | Dialog › Vorschauzeile Teilkataloge (BK-9) | Die Zahl der Aufbauten mit Teilkatalog stimmt mit der Datenbank überein. | Keine Abweichung zwischen Vorschau und Datenbank (die Abweichung 7 ist mit BK-10 behoben). | |
| 28 | Dialog › Vorschau, Meldung zu Abschnitten mit Flächenanteil 0 (BK-10) | Abschnitte mit Anteil 0 werden weggelassen, je Aufbau steht eine Protokollzeile. | Die übrigen Anteile ergeben 100 %; der Aufbau wird übernommen. | |
| 29 | Dialog › Abbrechen vor „Übernehmen“ | Nichts wird geschrieben. | Die Zählung im Katalog bleibt unverändert. | |

### 1.5 Rückmeldungen

Hier trägt der Anwender zusammen, was auffiel (Nummer der Zeile, Beobachtung, gegebenenfalls Bildschirmfoto im Ordner des
Anwenders, nicht im Repositorium):

- 
- 
- 

Die Prüfliste hat **29 Zeilen**. Entscheidungsfrage in Zeile 17 beantwortet der Anwender bei der Sichtabnahme (Stand
IFC-Übergabe vom 09.10.2026).

## 2. Auftrag an Epos Lokal: Übernahme der 7 Aufbauten

**Hintergrund.** Bei der Übernahme des Bauteilkatalogs am 16.10.2026 (E159) hat die Datenbank des Anwenders 7 Aufbauten nicht
übernommen: Die Vorschau zählte 7 913 Aufbauten mit Teilkatalog, die Datenbank 7 906 (Statuszeile #1171, BK-9/BK-10). Ursache
waren Abschnitte mit Flächenanteil 0, die die Summe der Anteile verfehlten. Mit BK-10 (E162) werden solche Abschnitte
weggelassen, wenn die übrigen 100 % ergeben, mit einer Protokollzeile je Aufbau. Die 7 Aufbauten kommen damit jetzt durch.

**Vorgehen** (das Vorgehen vom 16.10. gilt weiter, Nachricht 8 im Postfach `Quellen/Postfach/an_EPOS_Lokal.md` des Zweigs
`claude/postfach-epos-lokal`; dort nur lesen):

1. Klon auf den aktuellen Stand von `origin/ios_migration_september` bringen (Fast-Forward) und bauen:
   `dotnet build WP-Plan.sln -c Debug -p:Platform=x64` (bei gesperrtem Ausgabeordner mit `-p:OutDir` außerhalb).
2. Prüfen, dass kein Prozess `EPOS_Plan` läuft; sonst den Anwender bitten, die Anwendung zu beenden. Bis zur Meldung „fertig“
   EPOS-Plan nicht starten.
3. **Sicherung:** `VACUUM INTO` nach `C:\ProgramData\EPOS_PLAN\DB-Backup\Kenndaten_vor_Aufbauuebernahme_<Datum_Uhrzeit>.sqlite`;
   `integrity_check` der Kopie ok; Größe melden.
4. **Schema** auf den Stand der Testdatenbank heben (Schemastand 239; Kernweg wie bei E147: `SchemaMigration.Ausfuehren` mit
   `DataRepository.PfadUeberschreibung`; Schrittnummern je „OK“ melden); `integrity_check` ok.
5. **Vorschau** über den Exportordner des 16.10. (derselbe Ordner samt `Katalog_Zuordnung.csv`), Lizenzablage angebunden, noch
   nichts übernehmen. Erwartet: **0 neu, 7 aktualisiert** (mit BK-11 als Zahl sichtbar), Abschnitte 510, Teilkatalog danach 7 913 / 7 913, Teilkatalog-Abweichung **0**. Die 7 Aufbauten stehen schon in der Datenbank; „Übernehmen“ aktualisiert sie (Teilkatalog, Abschnitte ohne Beitrag fallen weg).
6. **Übernahme** über `BauteilkatalogImport.Uebernehmen` (gleicher Ordner); Dauer und Laufzeile melden.
7. **Konvergenzprobe:** zweite Vorschau, erwartet 0 neu, 0 zusammengeführt, alles wiedererkannt.
8. `PRAGMA integrity_check` und `PRAGMA foreign_key_check`; Zählungen (Baustoffe, Aufbauten, Schichten, Abschnitte, Aufbauten mit
   Abschnitten, Sätze mit Teilkatalog in der Quelle-Angabe).
9. Danach darf der Anwender EPOS-Plan wieder starten. Nichts committen, nichts pushen. Bei einem Fehler in den Schritten 4 bis 7
   die Datenbank aus der Sicherung zurückspielen und melden.

**Rückmeldung:** nur Zählungen, keine Werte, keine Namen, keine CSV-Dateien im Repositorium.

**Einstiegs-Prompt für die lokale Sitzung** (der Anwender fügt ihn in Epos Lokal ein):

```text
Auftrag Gebäudesimulation, 17.10.2026 (E164, Punkt 4): Übernahme der 7 Aufbauten in die produktive Datenbank.
Lies zuerst im Zweig claude/postfach-epos-lokal die Datei Quellen/Postfach/an_EPOS_Lokal.md (nur lesen, nicht auschecken,
nichts darauf schreiben), dort Nachricht 8 (Übernahme vom 16.10.), und die Papiere Dokumentation/aktuell/Gebaeudesimulation/
2026-10-17_Sichtabnahme_und_Epos_Lokal.md (Abschnitt 2) sowie Statuszeile #1171 (BK-9/BK-10, E162).
Ablauf: (1) Klon auf origin/ios_migration_september bringen, dotnet build WP-Plan.sln -c Debug -p:Platform=x64.
(2) Kein Prozess EPOS_Plan darf laufen; sonst den Anwender bitten, die Anwendung zu beenden, und bis "fertig" nicht starten.
(3) Sicherung per VACUUM INTO nach C:\ProgramData\EPOS_PLAN\DB-Backup\Kenndaten_vor_Aufbauuebernahme_<Datum_Uhrzeit>.sqlite,
integrity_check der Kopie, Größe melden. (4) Schema der produktiven Kenndaten.sqlite auf den Stand der Testdatenbank (jetzt 239) heben
(SchemaMigration.Ausfuehren mit DataRepository.PfadUeberschreibung), Schritte je OK melden, integrity_check. (5) Vorschau über
den Exportordner des 16.10. mit Katalog_Zuordnung.csv, Lizenzablage angebunden, noch nichts übernehmen; erwartet 0 neu, 7 aktualisiert
(mit BK-11 als Zahl sichtbar), Abschnitte 510, Teilkatalog danach 7 913 / 7 913, Abweichung 0. (6) Übernahme über BauteilkatalogImport.Uebernehmen, Dauer und Laufzeile melden.
(7) Konvergenzprobe: zweite Vorschau, erwartet 0 neu, 0 zusammengeführt. (8) PRAGMA integrity_check und PRAGMA foreign_key_check,
Zählungen (Baustoffe, Aufbauten, Schichten, Abschnitte, Aufbauten mit Abschnitten, Sätze mit Teilkatalog).
(9) Dann darf der Anwender EPOS-Plan wieder starten. Bei einem Fehler in Schritt 4 bis 7: Datenbank aus der Sicherung
zurückspielen und melden. Nichts committen, nichts pushen. Rückmeldung nur mit Zählungen, keine Werte, keine Namen,
keine CSV ins Repositorium. Antworte auf Deutsch.
```

## 3. Sportheim-Datei (Punkt 5)

Die Datei dient als Prüfstück für die Welle IV (Verfeinerungen des Imports aus der Projektdatei, Zweig `ifc6`). Der Anwender
tut im CAD:

1. Den Kellerwänden am Erdreich ihren Bestandsaufbau zuweisen.
2. Die gezeichneten Dicken auf den Bestand setzen.
3. Das Baujahr auf 1970 setzen.
4. Danach **IFC und Projektdatei** neu exportieren.

**Übergabe.** Per Google-Drive-Link; in der Cloud-Umgebung muss dafür der Host `drive.usercontent.google.com` freigegeben sein.
Alternativ legt Epos Lokal die Dateien unversioniert unter `Quellen/` ab.

## 4. Weitere offene Punkte

- Versionsnummer und Weg des nächsten Wiki-Sammel-Uploads: Entscheid in der Woche ab 19.10.2026 ([E163](../Status_Gebaeudesimulation_VDI6007.md)).
- Welle IV (Verfeinerungen des Imports aus der Projektdatei): in Arbeit in einer anderen Sitzung, nicht Teil dieses Papiers.

# Protokoll Welle P — Gebäudeimport nur aus der Projektdatei (.sqproj), Schemaschritt 200 `ProjektdateiImportSchema` (08.10.2026)

**Sitzung:** IFC / Gebäudeimport, Statuszeile **#815**. Commits: T1 `8fa2e6950` bis `ed6c3b2fb`; T2a `7ea25addc`, `73a4b9452`, `aab6d5346`; T2b `b8833b48f` bis `e12a57a6a`; T2c `9d52dbabf`; T3 `127d0ccf8` bis `4e123dd12`. Integration `integration-g5pk` `796afb72c`, im Hauptbaum `08ea6386e`.
**Entscheid:** Anwenderentscheide vom 07.10.2026; Konzept Datenaustausch 16.1.

## 1 Anlass

Das Gebäude soll sich auch allein aus der Projektdatei (.sqproj) importieren lassen. Entschieden: Die Nordrichtung wird abgefragt wie bei IFC ohne Nordwinkel (`building_data/@Direction` bleibt ungenutzt); Format und Herkunft bekommen den eigenen Wert `SQPROJ`; 3D gleich mit Flächenmodell (daraus der Wunsch „gleicher Viewer“, siehe Welle K).

## 2 Gebaut

### Schema 200

- `ProjektdateiImportSchema` erweitert die Prüfklauseln von `Tab_Importquelle.Format` und den Herkunftsspalten von `Tab_Baustoff(_STAMM)`, `Tab_Bauteilaufbau(_STAMM)`, `Tab_Zone` und `Tab_Bauteil` um `SQPROJ`: 7 Tabellen, umgebaut nach dem Rezept `TwwBezugsartSchema.Neubau`; der Schritt ist wiederholbar.
- Testdatenbank auf 200: 90 492 928 Byte, LFS-oid `f3deb49a72a42b357e92059b38b30bbce82a85985927907e4852c4e890d7ae4d`.

### Leser `SqprojGebaeudeLeser`

- Je Level-3-Hüllfläche ein Bauteil mit den Räumen seiner Bezüge; Öffnungen über `ParentUUID`.
- Randbedingung aus der Beheizung der Nachbarräume, Codes 6 und 7 nie still.
- Flächenherkunft `MENGENSATZ`; kein Nordwinkel (`IMP_SQPROJ_PROT_KEIN_NORDEN`); Ablehnung „Hülle unvollständig“.

### Ablauf

- Strom ohne Puffer; die Projektdatei aus demselben Lauf liefert Zonen, Konditionierung, Aufbauten und Nutzung.
- Zonen der Datei als Vorgabe (X1); Speichern mit `SQPROJ` (beim Weg „IFC + Projektdatei“ bleibt `IFC`); Neulesen; Decke über Außenluft wie im IFC-Weg.

### Dialog

- Auswahlfeld „Quelle“ mit IFC, gbXML, „IFC + Projektdatei“ und „Nur Projektdatei (.sqproj)“.
- Auf dem Weg „Nur Projektdatei“ ist „Projektdatei dazuladen“ ausgeblendet; es gibt die Quellzeile und einen leisen Hinweis bei unbeheizten Räumen.
- Grenze 250 MB unter Windows, 100 MB unter iOS.

## 3 Prüfung

Diagnose einer Anwenderdatei gegen den IFC-Weg derselben Datei: beheizte Fläche, Volumen, Erdreich und Grundfläche 0,00 %; Fenster −0,23 %; Außenwand brutto 0,00 %, netto +0,05 %. Die U-Werte weichen um Faktor 2 bis 3 ab; das ist der Datenstand der Dateien, kein Lesefehler: In der Projektdatei stimmen `UValue`, Aufbau und Schichten an 356 von 356 Flächen überein, die IFC verweist auf andere Aufbauten. Kein Rechenweg, Basis unverändert.

## 4 Gate 814

GATEZAHLEN

## 5 Offen

- **Sichtabnahme unter Windows:** Lage des Auswahlfelds „Quelle“; ob der zweite Dateidialog bei „IFC + Projektdatei“ von selbst aufgehen soll; Sichtbarkeit der Nordannahme; Hinweis bei unbeheizten Räumen.
- Logbuch-Eintrag (Version beim Anwender erfragen) und Wiki-Upload gebündelt.

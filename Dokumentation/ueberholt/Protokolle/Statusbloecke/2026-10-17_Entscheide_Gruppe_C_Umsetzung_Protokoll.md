# Statusblock #1166 — Umsetzung der Anwenderentscheide Gruppe C (17.10.2026)

Der ausführliche Block zur Statuszeile #1166 in
[`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md). Register der Entscheide:
[Entscheidungsregister iOS](../../../aktuell/Entscheidungsregister_iOS_EPOS-Plan.md) § 5.
Sitzung „Dialoge und Korrekturen“ (Kostenbasis), Integrationszweig `integration-c`, Stand `18dbbcaaa`.

## 1 Wellen und Commits

| Welle | Merge | Inhalt |
|---|---|---|
| U1 | `4cfc41bc1` | 40b Ressource `FLOTTE_OPT_KEIN_ERGEBNIS` entfernt (beide Sprachen); 35b Etappenvermerk gestrichen; 15 Beispielvorlage entfernt; 20c drei SQL-Texte der Simulation parametriert |
| U2 | `eefa6fab9` | 8a `hat.*` je Variantengruppe; 8b Deckblatt-Regel verlangt den Berichtstitel; 8c Excel-Diagramme wie Word; 8e Hinweis in der Laufmeldung |
| U2b | `09d4b351b` | 8d Kälte-JAZ und Deckung eine Rechnung, Kältespalten ungerundet gespeichert; 6a Flotteneinspeisung im Nebenbalken; 28d Rasterkarte wächst nach der Fußzeile, Zusatz als Textzeile, ChartProben-Messlatte |
| U3 | `363202635` | 29c Betriebskosten je kW·a; 28b Import legt die Vergütungszeile des Stamms an; 6b Hilfsstrom mit Emission und Stromsteuer |
| U4 | `fd5775ce5` | 10a Wechselrichter „bedingt geeignet (Angaben fehlen)“; 23 KI-Assistent wählt Modul und Wechselrichter im Strang |
| Testdatenbank | `5a0f1ccaf` | 8e Datenbefunde bereinigt (Abschnitt 4) |
| Test | `cb0f99dbf` | Excel-Test zu 6a/8c: Flotteneinspeisung in der Nebenbalken-Gruppe |
| Nachentscheide | `9e83e6078`, `f70afe65c`, `1cbd07eca` | 29c, 8a, 6b (Abschnitt 3) |
| Messlatte | `3d08644b4` | ChartProben-LIESMICH: 277 Bilder, davon 250 PNG |
| Basis | `da0a25075`, `ddaf63505` | Basis R58 eingefroren, R57 archiviert (Abschnitt 5) |
| Abgleich | `282685e40`, `18dbbcaaa` | Merge von origin (Schemaschritt 239 von anderer Sitzung angemeldet) |

## 2 Entscheide und Abweichungen von der Empfehlung

Alle 16 umzusetzenden Punkte des früheren Abschnitts 4.6 der Statusdatei sind umgesetzt. Abweichend von der Empfehlung
hatte der Anwender entschieden: 8c (jetzt angleichen, ohne Sichtprobe), 23 (Übernahmeweg im Strang), 6b (Emission und
Stromsteuer gesondert), 28b (eigene Vergütungszeile), 29c (zunächst nur BHKW, später geändert, Abschnitt 3). Übrige Punkte
nach Empfehlung. Wiki-Wirkung: 8a, 8b, 10a, 6b ja; die übrigen nein.

- **8d:** Dashboard und Katalog rechnen Kälte-JAZ und Deckungsgrad mit einer Rechnung; die Kältesummen werden ungerundet gespeichert (Folge: Basis R58). Die JAZ Kälte gilt für alle Kälteerzeuger.
- **6a:** Die Flotteneinspeisung steht im Nebenbalken der Einspeisung der Strombilanz, im Bericht und im Excel-Bericht.
- **28d:** Die Rasterkarte wächst nach der Fußzeile; der Zusatz steht als Textzeile. ChartProben-Messlatte nachgezogen.
- **28b:** Der Import einer einzeln weitergegebenen Variante legt am Ziel eine eigene Vergütungszeile mit den Werten des Stamms an.
- **10a/23:** Wechselrichter ohne Tracker- oder Stromangaben heißen „bedingt geeignet (Angaben fehlen)“; der KI-Assistent wählt im Strang Modul und Wechselrichter und lehnt eine ungeeignete Wahl mit Grund ab.

## 3 Nachentscheide des Anwenders (17.10.2026)

- **29c geändert (`9e83e6078`):** „alles wie vorher + BHKW“. Die Einschränkung der Leistungsarten im Betriebsraster ist zurückgenommen; Wärmepumpe und BHKW behalten ihre Leistungsarten, das BHKW bietet zusätzlich „je kW elektrisch“. Test: 12 €/kW·a × 50 kW = 600 €/a.
- **8a (`f70afe65c`):** „Stamm zählt mit“ — in jeder Variantengruppe zählt das Stammprojekt bei `hat.*` mit.
- **6b (`1cbd07eca`):** „Kennzahlen nachziehen“ — Vorlagenfeldkatalog Fassung 26 mit vier Kennzahlen `energie.hilfsstrom`, `em.co2_hilfsstrom`, `ko.stromsteuer_hilfsstrom`, `ko.stromsteuer_hilfsstrom_gesamt`; neun Vorlagen neu gebaut.

## 4 Testdatenbank-Bereinigung 8e (`5a0f1ccaf`)

Wirtschaftlichkeitsergebnisse der Projekte 1029 und 1044: `IstStamm` auf 0 gesetzt, mit dem Variantennamen versehen; die
Duplikate von Projekt 1043 (ID 216 bis 218) sind gelöscht. Größe unverändert 109 604 864 Byte, LFS-SHA-256
`c8befb5112f9ee917463ffae7f945f4d56b713a8c92c532b7d9cc0f22edd5db2`. Der genaue SQL-Wortlaut steht im Commit
`5a0f1ccaf` (Werkzeug `Werkzeuge/Testdatenbankschema`); in der Form: `UPDATE` der Ergebniszeilen von 1029/1044 mit
`IstStamm = 0` und gesetztem Variantennamen, `DELETE` der Zeilen mit den IDs 216, 217, 218 für Projekt 1043.

## 5 Basis R58 `2026-10-17_R58_Kaeltesummen`

Eingefroren mit 38 Projekten und Testdatenbank 238 (mit Bereinigung 8e); R57 archiviert. Grund: Die Kältespalten werden
ungerundet gespeichert (8d). Gegen R57 weichen acht Projekte in genau einer Zeile von `aggregate.csv` ab,
`Waermepumpe.Kaelteproduktion_WP`:

| Projekt | R57 | R58 |
|---|---|---|
| 1017 | 4,05 | 4,05051694 |
| 1047 | 3,83 | 3,82939092 |
| 1056 | 3,77 | 3,76627292 |
| 1058 | 3,81 | 3,80818165 |
| 1060 | 3,83 | 3,82873151 |
| 1061 | 3,83 | 3,83047193 |
| 1062 | 3,91 | 3,90538151 |
| 1064 | 4,05 | 4,05051694 |

Innerhalb der Toleranz liegen `Stromverbrauch_Kuehlung` und `Deckung_Kuehlung` bei 1056, 1061 und 1072. Alle übrigen
1 224 CSV sind byte-gleich. Abnahmelauf der 19 CI-Projekte gegen R58: GESAMT PASS.

## 6 Abnahmen

- Kern-Filter und Windows-Schale: 0 Fehler.
- Gefilterte Tests: Kern 1 267 grün, 1 übersprungen; UI 624 grün.
- ChartProben: 277 Bilder, 0 Verstöße. SQL-Dialekt-Prüfer: 0 Fundstellen bei 3 057 Texten.
- Gate (`gate_rest_linux.sh`) auf `18dbbcaaa`: grün (UI 9698/9698, KiKern 553/553, SpeicherEngine 397/397, SpeicherPlanung 27/27, Werkzeugtests grün, Kern-Filter, Windows-Schale, Designer, SQL-Prüfer, BOM, Konfliktmarker ohne Befund; Referenzlauf aller 38 Projekte gegen R58 GESAMT PASS, 1233/1233 CSV byte-gleich)
- CI-Kern-Lauf 39009472495 auf `18dbbcaaa`: rot in Block K (zwei Tests: Zählung der KI-Kennungen nach 8e, gerundet gespeicherter Kältestrom-Netzbezug der Kältemaschine nach 8d; behoben mit `412aac2b4` und `5ca1ee743`, alle 38 Projekte gegen R58 weiter GESAMT PASS und byte-gleich, Basis unverändert); Nachlauf {CI2}

## 7 Offen

- Wiki-Upload mit dem nächsten Sammel-Upload: Seiten Berichtsvorlagen, Ergebnisdarstellung, Stromspeicher, Emissionen, Kosten, Varianten, Photovoltaik, Gerätekataloge; acht Logbuch-Sätze vom 17.10.2026 in `Werkzeuge/WikiUpload/logbuch.wiki` (Status 4.2).
- Version für das Update-Logbuch beim Anwender erfragen.

# Protokoll VW1 — Ausweis der Vorlaufwahl der Wärmepumpe (05.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Wellen VW1a und VW1b (E88), VW1a `bc23f76`, `44ab633` (Datenbank 188), VW1b `7fd9fdc`, `b9371b0`, `f17cd11`.
**Entscheide:** E86 (Q34 Weg 3, berichtigt: Die Kennlinienwahl am Vorlauf ist seit AK1 gebaut), E88 (Q35 Weg 2: Ausweis der Vorlaufwahl im Ergebnis), E89 (Q36: GA entfällt, der Altweg bleibt dauerhaft als wählbarer Rechenweg). Schemaschritt 188 `VorlaufwahlSchema`, Basis R38 `2026-10-05_R38_Vorlaufwahl`.

## 1 Auftrag

Die Wärmepumpe wählt bei gekoppeltem Heizkreis je Stunde die Vorlaufstufe ihrer Kennlinie. Diese Wahl stand bisher allein im Simulationsprotokoll. Sie wird als Ergebnis ausgewiesen: Stunden je Kennlinienstützstelle sowie Stunden darunter und darüber, mit Kennzahlen und Berichtszeilen, als Vorarbeit für die Feldphase (H6). Register und Konzepte zu E86, E88 und E89 waren vorab nachgezogen.

## 2 Vorgehen

Zwei Opus-Agenten nacheinander: VW1a (Schema, Modell, Ergebnisweg, Testdatenbank), VW1b (Kennzahlen, Katalogfassung, Bericht, Wiki, Basis R38). Die Papiere schrieb ein Sonnet-Agent. Gate 738 fährt die Orchestrierung danach.

## 3 Ergebnis

**VW1a (Schema 188, Ergebnis).** `Tab_ErgebnisWaermepumpeModul`: `Vorlaufwahl_Stunden` (TEXT, Paare `Vorlauf:Stunden` mit `;`, nur Stunden innerhalb der Stützstellen, alle Stützstellen), `Vorlauf_Darueber_Stunden`, `Vorlauf_Darunter_Stunden` (INTEGER, NULL oder 0–8760). Registriert in `SchemaMigration` (hängt an 187), `SchemaStand`, `Paketanhebung`, `TestDatenbank`, `Werkzeuge/Testdatenbankschema`. `SimulationWaermepumpe.Kennlinienwahl.Zaehlen/Ausweis` und `VorlaufwahlDesModuls`; `ErgebnisWaermepumpeModulModel` mit drei Feldern; `SimulationRunner` füllt nur bei Kennlinienwahl, sonst NULL; `ErgebnisCtrl` NULL-erhaltend; `Referenzlauf/Ergebnisexport.SpaltenNurMitWert`. Tests `VorlaufwahlSchemaTests` (6), `VorlaufwahlErgebnisTests` (4). Testdatenbank 188: 87 736 320 Byte, OID `69322344…` (direkt aus der Sitzung hochgeladen). Abnahme: 1 232 Tests grün, SQL 2 376/0, Referenzlauf 1017 PASS, 1047 und 1056 nur neue Schlüssel.

**VW1b (Kennzahlen, Bericht, Wiki, Basis).** Kennzahlen `wp.vorlauf.darunter_stunden`, `wp.vorlauf.darueber_stunden` (Gruppe Effizienz, Summe über die Module mit Wert); Katalogfassung 14 (`Vorlagenfeldkatalog_v14.txt`), zehn Vorlagen neu erzeugt, Validator 0 Fehler. Drei Berichtszeilen je Modul im Abschnitt „Heizkreis und Übergabe“ (Modulname, „Vorlaufwahl der Kennlinie: 35 °C 1.549 h, …“, „Stunden außerhalb der Stützstellen: darunter …, darüber …“), englische Texte in `BerichtTexte`, `SzenarioMengen` reicht durch. Test `VorlaufwahlBerichtTests` (4), `VorlagenfeldkatalogWacheTests` 13 → 14. Wiki `Projekte/Wiki/Programm Dokumentation - Wärmepumpe.wiki`, Punkt „Vorlaufwahl ablesen“ (Anker `vorlaufwahl`). Basis R38: 21 Projekte, 646 CSV, 4 271 Skalare; 19 Projekte byte-gleich zu R37, 1047 und 1056 nur je drei neue Zeilen in `aggregate.csv` (1047 `35:1549,45:1782,55:122`, darunter 1 851, darüber 0; 1056 `35:1122,45:1411,55:101`, darunter 1 475, darüber 0). R37 liegt unter `Dokumentation/ueberholt/Referenzbasen/`; `kern.yml`, `ios.yml` und das Regressionsnetz in `CLAUDE.md` stehen auf R38. Abnahme: 1 288 Tests grün, 21/21 PASS gegen R38.

## 4 Festlegungen

Der Ausweis entsteht nur bei Kennlinienwahl; ohne sie bleiben die Spalten NULL. Stunden außerhalb der Stützstellen stehen in den Spalten „darunter“ und „darüber“, nicht in der Paarliste. Der Logbuch-Satz lautet: „Der Bericht weist bei gekoppeltem Heizkreis je Wärmepumpe aus, wie viele Stunden sie jede Vorlaufstufe ihrer Kennlinie gewählt hat und wie viele Stunden der Vorlauf darunter oder darüber lag.“ E89: Die Stufe GA entfällt, die Papiere sind nachgezogen.

## 5 Nachweise

Abnahme der Wellen wie oben (VW1a 1 232, VW1b 1 288 Tests grün, SQL 2 376/0, Referenzlauf 21/21 PASS gegen R38). Gate 738 steht in der Statuszeile #738 der Statusdatei. Messwert für die Feldphase (H6): rund 35 % der Stunden mit Kennlinienwahl liegen unter der untersten Stützstelle 35 °C, keine darüber.

## 6 Offenes

Sichtabnahme unter Windows (Bericht), Logbuch-Version beim Anwender erfragen, Wiki-Upload der Wärmepumpenseite mit dem nächsten Sammel-Upload, CI-Vermerk nach dem Push. Q37 (Zonen-Nutzung, Weg 1–3) liegt beim Anwender. Die Stufe AK3 ruht nach H6 bis zur Feldphase.

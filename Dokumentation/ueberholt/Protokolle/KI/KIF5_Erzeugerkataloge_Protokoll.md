# KI‑F5 — Erzeugerkataloge für den Hilfe-Assistenten (Protokoll, 21.09.2026)

Statuszeile #424 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Konzept
[`Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`](../../../aktuell/Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md)
(Stufe S4, KI‑D‑Q5 und KI‑D‑Q6); Vorgänger [`KIF4_Kosten_Wirtschaftlichkeit_Protokoll.md`](KIF4_Kosten_Wirtschaftlichkeit_Protokoll.md),
[`KIF3_Bedarf_Klima_Protokoll.md`](KIF3_Bedarf_Klima_Protokoll.md), [`KIF1_Erzeugermasken_Protokoll.md`](KIF1_Erzeugermasken_Protokoll.md).
Zweig `ki-f5`, Commit `4cd2a678`; Merge `d7bd24d6`.

## Die Masken

| Katalogschlüssel | Dialog | Bindung | Felder (gesamt / nur lesbar / Wahl) | Haken | Ziel | dialog_oeffnen |
|---|---|---|---|---|---|---|
| `Form_DBBHKW` | BHKW-Katalog | `BhkwKatalogDaten` (Markup-Probe) | 13 / 2 / 1 | Auffrischen, Prüfen, Speichern | `Masken.BhkwAdmin` | Windows ja, iOS benannt abgelehnt |
| `Form_SolarDB` | Solarkollektor-Katalog | `SolarkollektorKatalogDaten` (Markup-Probe) | 13 / 1 / 0 | Auffrischen, Prüfen, Speichern | `Masken.SolarkollektorenAdmin` | Windows ja, iOS benannt abgelehnt |
| `Form_AdminPV` | Modulkatalog, Ausprägung PV-Module | `ModulKatalogKiSicht` | 15 / 1 / 1 | Auffrischen, Prüfen, Speichern | `Masken.PvAdmin` | Windows ja, iOS benannt abgelehnt |
| `Form_AdminStromspeicher` | Modulkatalog, Ausprägung Stromspeicher | `ModulKatalogKiSicht` | 14 / 1 / 0 | Auffrischen, Prüfen, Speichern | `Masken.StromspeicherAdmin` | Windows ja, iOS benannt abgelehnt |
| `Form_AdminWechselrichter` | Modulkatalog, Ausprägung Wechselrichter | `ModulKatalogKiSicht` | 26 / 2 / 0 | Auffrischen, Prüfen, Speichern | `Masken.WechselrichterAdmin` | Windows ja, iOS benannt abgelehnt |

Katalog 51 → 56 Masken, 522 → 603 Felddeklarationen. Auf iOS übersetzt `IosNavigation.Uebersetze`
keinen der fünf Schlüssel, die Wurzel antwortet `false` und `dialog_oeffnen` lehnt benannt ab —
dieselbe Lage wie bei den Katalogeditoren Heizkessel, Pufferspeicher und Wärmepumpe.

## Entscheidungen

Der Modulkatalog ist eine Komponente mit drei Ausprägungen (`ModulKatalogProfil`) und bekommt drei
Katalogschlüssel, nach dem Muster der drei Bedarfsverwaltungen; eigene Editoren für
Wechselrichter- und Stromspeicherkatalog gibt es nicht. Er führt seinen Stand als Liste von
`ModulFeldwert`, deren Umfang das Profil zur Laufzeit bestimmt — deshalb bindet er über die
Sichtklasse `ModulKatalogKiSicht` (je Profilschlüssel eine benannte, typisierte Eigenschaft,
Parsen und Formatieren wie die Maske); ein neuer Wächter hält Katalogeintrag und Profil
gegeneinander (Zahl, Anzeigenamen, gesperrte Felder als `nurLesen`). BHKW- und Kollektorkatalog
binden ihr Daten-Objekt mit Markup-Probe.

Nur lesbar sind der BHKW-Modulname und der Kollektorname (Schlüssel des UPDATE, die Maske sperrt
sie selbst), der BHKW-Gesamtwirkungsgrad (Summe zweier Anteile) sowie Bezeichner und Herkunft im
Modulkatalog. Der BHKW-Speicherweg lehnt am Auslieferungssatz benannt ab; Kollektor und
Modulkatalog prüfen dieselben Pflichtzahlen wie ihr Knopf. Kein Schreibschutz-Haken, weil je Feld
gesperrt wird, nicht je Satz.

Draußen nach KI‑D‑Q5 und Q6: die Katalogliste (nach einem CEC-Import über 20 000 Module) samt
Filterstand und Aufklapper, der Katalogbrowser, die Importmasken, der Kennlinien-Editor der
Wärmepumpe (Tabelle mit eigenem Editor), die Wärmepumpen-Auswahlliste und der Wärmepumpen-Wirt
(Zweispaltenauswahl mit eingebettetem Anlagendialog, der sich selbst anmeldet). `Form_WP` und
`Form_PV` sind vollständig: Die Stammfelder der Wärmepumpe decken alle elf Felder mit vier
Wahl-Feldern ab, die Konfiguration gehört zu `Form_WP_Anlage`; Modell- und Strangfelder der
Photovoltaik sind eingebettete Blöcke ohne eigenes Fenster.

## Zahlen und Abnahme

85 neue Ressourcenschlüssel je Sprache (80 Erläuterungen, 5 Anzeigenamen für die Formelzeichen
h0, k1, k2, Kdir, Kdiff); die übrigen Anzeigenamen sind die bestehenden Beschriftungsschlüssel der
Masken. 16 neue Testfälle (Feldzahlen und Profilabgleich, je drei Zeugen für BHKW- und
Kollektorkatalog, sechs für den Modulkatalog); zwei Wegweiser-Tests auf andere Felder gestellt,
weil `th_leistung` jetzt an zwei Masken steht. Worktree: Kern-Filter 0 Fehler, 10 263 Tests grün
(1 übersprungen); 15 Dateien mit BOM und CRLF geprüft. Gate im Hauptbaum auf `d7bd24d6`: siehe
Statuszeile #424.

## Offen, mit Entscheid des Anwenders

1. `Form_WP`, Feld `modulkosten`: Die Maske zeigt es nur lesbar mit Herleitung; der Katalog führt
   es ohne `nurLesen`, und drei Testdateien nutzen genau dieses Feld als setzbaren Fall.
2. `Form_PV`, Auslegungstemperaturen `TKalt` und `THeiss`: Parameter des Strangbausteins, keine
   Eigenschaft der Erzeugerzeile; deklarieren hieße Umstellung von `Form_PV` auf eine Sichtklasse.
3. `Form_PV`, Überlagerung „Anlagenwerte" (Wechselrichter-Nennleistung und -Wirkungsgrade): eigenes
   Fenster, nach Fachkonzept 11.6 ausdrücklich ausgeschlossen — Kandidat für einen eigenen Schlüssel.
4. `Form_PV`, Feld `modell_erweitert`: auf der Maske ein Auswahlfeld mit zwei Einträgen, im Katalog
   ein Wahrheitswert — Abweichung von KI‑D‑Q6.

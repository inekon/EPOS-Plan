# Wiki-Update 26.09.2026 — Vorbereitung des Sammel-Uploads

Dieses Papier bereitet den gebündelten Wiki-Upload vom 26.09.2026 vor (Regel: Konzept
Hilfesystem 13.3). Es listet die Seiten, deren Repo-Quelle seit dem letzten Upload (Version
1.2.0.0, Auftrag #252) fortgeschrieben wurde, sammelt die dazu entworfenen Logbuch-Sätze
geordnet nach Version und nennt, was zum Stichtag noch offen ist. Quelle aller Angaben ist
[`Status_iOS_Migration.md`](Status_iOS_Migration.md); die Statusdatei selbst ist hier nicht
geändert. Die Versionsnummern sind Vorschläge — beim Anwender zu bestätigen (Regel: Konzept
Hilfesystem 13.3).

## 1 Seiten für den Sammel-Upload

| Wiki-Seite | Repo-Quelle | Was sich geändert hat | Quelle |
|---|---|---|---|
| Programm Dokumentation/Klimadaten | `Projekte/Wiki/Programm Dokumentation - Klimadaten.wiki` | neue Seite: Quellenwahl PVGIS/DWD-Testreferenzjahr, Standort aus dem Dateikopf, Regionsvorschau, durchsuchbare Liste mit Quelle/Bezugsjahr/Szenario, Diagrammzoom | Statuszeilen #367, #368, #369, #371, #382, #396, #404, #413 |
| Programm Dokumentation/Simulationsergebnisse | `Projekte/Wiki/Programm Dokumentation - Simulationsergebnisse.wiki` | Farbwahl der Diagramme in den Einstellungen und die neue Diagrammbedienung (Zoom, Werteleiste, schaltbare Legende); jede Kurve und Fläche trägt ein Farbfeld, die Reiter zeigen die Farben des Berichts | Statuszeilen #403, #411, #413, #418 |
| Programm Dokumentation/Stromspeicher | `Projekte/Wiki/Programm Dokumentation - Stromspeicher.wiki` | Beschreibung der Auslegungsbilder auf die neue Diagrammbedienung nachgezogen; neuer Abschnitt „Mögliche Optimierungen" (Betriebsziele, adaptive Lastspitzenkappung, Auslegung, Verteilung, Grenzen) | Statuszeilen #411, #413, #417 |
| Programm Dokumentation/Hilfe-Assistent | `Projekte/Wiki/Programm Dokumentation - Hilfe-Assistent.wiki` | Liste der Masken, in denen der Assistent mitarbeitet (Erzeugermasken des Projekts, Katalogmasken, Kostenverwaltung, Simulationskonfiguration, Ansichten); wächst mit den Wellen KI‑F1b bis F6 | Statuszeilen #416, #419 |
| Programm Dokumentation/Wirtschaftlichkeit | `Projekte/Wiki/Programm Dokumentation - Wirtschaftlichkeit.wiki` | Menüweg und Durchsuchbarkeit der gesetzlichen Parameter, Kohärenzzeile ergänzt; die Ergebnisseite neu beschrieben — Umschalter „Kennzahlen / ValERI-Bewertung", vier Abschnitte, Empfehlungskarten, Bandbreite mit Spannenbild, Verlauf mit drei Szenarien (Anker `verlauf`), Sensitivität mit Steigung, Hinweistext und Deklarationen, „Bericht erzeugen", „Verlauf nach Excel…", Szenarien Ungünstig/Erwartet/Günstig; die CO₂-Prüfung der Stromsteuerbefreiung brennwertbezogen und die vermiedenen Stromkosten ohne jede Eigenerzeugung mit dem Anteil je Anlage | Statuszeilen #372, #405, #413, #434, #436, #437 |
| Programm Dokumentation/Pufferspeicher | `Projekte/Wiki/Programm Dokumentation - Pufferspeicher.wiki` | Aufzählung der Erzeugerseite: der Aufklapper „Alle Daten anzeigen" mit den Investitionskosten statt des entfallenen Detailfelds | Statuszeile #422 |

Die Liste folgt der zusammenfassenden Aussage der Statuszeile #413: „Die Seiten Klimadaten,
Simulationsergebnisse, Stromspeicher und Wirtschaftlichkeit sind in den Repo-Quellen
fortgeschrieben — Sammel-Upload 28.09.2026“ (das dort genannte Datum ist durch den
vorliegenden Auftrag auf den 26.09.2026 vorgezogen). Eine gesonderte Seite zu Kosten oder zu
Gerätekatalogen nennt keine der ausgewerteten Statuszeilen als upload-bereit.

**Hinweis zum Arbeitsstand:** Die Repo-Quelle der Seite Stromspeicher trägt seit diesem
Auftrag zusätzlich einen neuen Abschnitt „Mögliche Optimierungen“ (Aufgabe A desselben
Auftrags) — dafür gibt es noch keine Statuszeile, weil er in derselben Sitzung entsteht. Vor
dem eigentlichen Hochladen der Seite ist er mitzunehmen.

## 2 Logbuch-Einträge für die Wiki-Seite „Update-Logbuch“

Reihenfolge neueste Version oben. Ein Satz je wesentlicher, sichtbarer Änderung, ohne
Einzelheiten und Begründung (Regel: Konzept Hilfesystem 13.4); Kleinigkeiten sind bereits
ausgefiltert (Statuszeilen mit „Kein Logbuch-Satz“).

### Version 1.2.0.4 — Anwenderentscheid 22.09.2026 („letzte Nummer erhöhen"; das Programm trägt heute 1.2.0.3 in `AssemblyInfo.cs`, die Anhebung gehört zur Auslieferung)

Die Diagramm-Umstellung auf Vektorgrafik (DG-E3) ist mit Statuszeile #413 abgeschlossen. Die
folgenden vier Sätze fassen die Zwischenstände aus #404 und #411 zusammen, die dieselbe
Bedienung an einzelnen Diagrammfamilien beschrieben und mit den programmweiten Sätzen aus
#413 ihren Gegenstand verlieren (Regel 13.4: ein Thema, ein Eintrag).

- Seit 26.09.2026: Den Zeitreihen-Diagrammen wurden ein Zoom auf der Zeitachse, eine
  Werteleiste am Mauszeiger und eine schaltbare Legende mit Farbwahl hinzugefügt. (#411)
- Seit 26.09.2026: Jedes Diagramm im Programm ist eine maßstabsfreie Vektorgrafik; die Werte
  an der Stelle, auf die Sie zeigen, stehen unter dem Bild. (#413)
- Seit 26.09.2026: Diagramme ohne Zeitachse — Deckungsringe, Monatsbilder, Rasterkarten,
  Kurven der Stromspeicher-Auslegung, Jahresprojektion, Kennlinien der Wärmepumpe — nennen den
  Wert des Elements, auf das Sie zeigen oder das Sie antippen. (#413)
- Seit 26.09.2026: Der Zeitausschnitt eines Diagramms wird im Bild selbst aufgezogen; das
  Bild wird dabei nicht mehr neu gerechnet. (#413)
- Seit 26.09.2026: Solarthermie und Photovoltaik zeigen ihre Jahreskurven auf Wunsch als
  Dauerlinie (Schalter „sortiert"). (#415)
- Seit 26.09.2026: Der Hilfe-Assistent kann die Erzeugermasken des Projekts lesen, ausfüllen
  und speichern (Heizkessel, BHKW, Pufferspeicher, Stromspeicher, Solarkollektoren, Wärmepumpe,
  Photovoltaik). (#416)
- Seit 26.09.2026: Der Hilfe-Assistent liest und setzt auch die Masken der
  Simulationskonfiguration (Pufferspeicher im Projekt, Erdreich- und Pufferspeicherquelle,
  Quellprofil, Wärmesenke, Komponentenkonfiguration) und die Einstellwerte des
  Stromspeicher-Reiters der Simulation. (#419)
- Seit 26.09.2026: Der Hilfe-Assistent setzt in den freigegebenen Masken jedes Eingabefeld,
  auch Auswahlfelder über den angezeigten Text; Feldnamen dürfen ungefähr sein. (#420)
- Seit 26.09.2026: Der Hilfe-Assistent liest und setzt auch die Masken des Bedarfs (Gebäude,
  Wohnfläche, Gebäudekatalog, Gebäude- und Bedarfstypen, Bedarfsprofile und -verwaltungen,
  Wärmebedarf extern, Solarganglinie) und die Klimadaten. (#421)
- Seit 26.09.2026: Jeder Legendeneintrag eines Diagramms hat ein Farbfeld; die
  Simulationsreiter zeigen dieselben Farben wie der Bericht, und die Einstellungen führen dafür
  54 Größen. (#418)
- Seit 26.09.2026: Die Dialoge Heizkessel und Pufferspeicher zeigen im Modulblock kein Feld
  „Investitionskosten" mehr; der Preis steht bearbeitbar im Aufklapper „Alle Daten anzeigen". (#422)
- Seit 26.09.2026: Der Hilfe-Assistent liest und setzt auch die Masken der Kosten und der
  Wirtschaftlichkeit (Energieträger, Kostenprofil, Kostenfaktoren, Emissions- und
  Nutzungsdauerkatalog, Kostenvorlagen, Wirtschaftlichkeitsparameter, BHKW-Wirtschaftlichkeit,
  Tarifstruktur, Photovoltaik-Vergütung, gesetzliche Parameter) sowie die Seiten Kosten und
  Wirtschaftlichkeit. (#423)
- Seit 26.09.2026: Der Hilfe-Assistent liest und setzt auch die Katalogmasken der Erzeuger
  (BHKW, Solarkollektor, PV‑Module, Stromspeicher, Wechselrichter). (#424)
- Seit 26.09.2026: Der Hilfe-Assistent liest und setzt auch Peak-Shaving, Speicher-Zeitreihen,
  das Zeitintervall der Stromganglinien-Verwaltung, Projektkopie und Projektvariante, die Seiten
  Übersicht und Bericht sowie alle vier Stationen der Stromspeicher-Auslegung; damit arbeitet er
  in allen Masken mit Einstellwerten mit. (#425)
- Seit 26.09.2026: Der Hilfe-Assistent erreicht in der Photovoltaik auch die Auslegungstemperaturen
  und die Anlagenwerte des Wechselrichters sowie in der Kostenverwaltung die Komponentenwahl und die
  Wahl der Photovoltaik-Vergütung; die Modulkosten der Wärmepumpe zeigt er nur noch an. (#427)
- Seit 26.09.2026: Das Übernehmen eines Standard-Stromprofils im Projekt-Assistenten funktioniert
  wieder; scheitert eine Katalogkopie, meldet der Assistent es benannt. (#426)
- Seit 26.09.2026: Der Hilfe-Assistent öffnet unter Windows auch die Kostenverwaltung, die
  Energieträgerverwaltung, die Nutzungsdauern, die gesetzlichen Parameter, die Klimadaten und die
  Projektvariante, führt zu den Erzeugermasken auf den Reiter „Energieerzeuger" und zu den Blättern
  der Ansicht „Berichte und Kosten"; im Speicher-Zeitreihen-Dialog lässt sich die Intervallkonvention
  „automatisch erkennen" wählen. (#428)
- Seit 26.09.2026: Der Hilfe-Assistent nennt Masken und Felder nach einem Sprachwechsel in der
  neuen Sprache. (#430)
- Seit 26.09.2026: Die Knöpfe „BHKW-Tarif…", „Stromtarif…" und „Tarif…" der Wirtschaftlichkeitsseite
  und des PV-Vergütungsdialogs öffnen die Tarifstruktur als Einblendung in der Seite; der
  PV-Vergütungsdialog aus dem Reiter „Ertrag/Bonus" erscheint ebenfalls als Einblendung statt als
  eigenes Fenster. Die zuletzt bearbeitete Vergleichsgruppe der Übersicht wird nach dem Update einmal
  nicht erinnert. (#431)
- Seit 26.09.2026: Die Erlösrubrik der Wirtschaftlichkeit gliedert innerhalb der Blöcke nach Anlage
  — Blockheizkraftwerk, Photovoltaik, Kessel und „projektweit", je mit Zwischensumme — und weist die
  vermiedenen Stromkosten je Anlage aus. Die Energiesteuer-Entlastung steht als zwei Zeilen (§ 53/§ 53a
  beim Blockheizkraftwerk, § 54 beim Kessel) mit Herleitung; eine Nullzeile nennt den Grund des
  Rechenlaufs. (#432)
- Seit 26.09.2026: Die Wirtschaftlichkeitsseite schaltet im Kopf zwischen den Darstellungen
  „Kennzahlen" und „ValERI-Bewertung" um; die Kennzahlen stehen in den vier Abschnitten „Lohnt es
  sich?", „Wie sicher ist das?", „Woraus entsteht die Zahl?" und „Was ist angenommen?". (#434)
- Seit 26.09.2026: Jede Version bekommt eine Empfehlungskarte mit ihrer Einstufung, und die Bandbreite
  zeigt die Szenarien Ungünstig, Erwartet und Günstig nebeneinander mit ihrer Spanne. (#434)
- Seit 26.09.2026: Die Sensitivitätstafel der Seite nennt je Einflussgröße die Steigung, und unter der
  Annahmentafel steht, was ein Szenario verändert und was nicht. (#434)
- Seit 26.09.2026: Der Knopf „Bericht erzeugen" auf der Wirtschaftlichkeitsseite erzeugt den Bericht für
  die gewählte Vergleichsgruppe. (#434)
- Seit 26.09.2026: Wort- und Tabellenbericht führen die Bandbreite mit Einstufung, den Hinweis zu den
  Szenarien und die Deklarationen nach DIN EN 17463. (#434)
- Seit 26.09.2026: Die Wirtschaftlichkeitsseite zeigt den Kapitalwertverlauf aller drei Szenarien in einem
  Bild — Farbe je Variante, Strichart je Szenario —; der Dialog „Verlauf…" ist nicht mehr vorhanden. (#436)
- Seit 26.09.2026: Der Knopf „Verlauf nach Excel…" speichert den Verlauf als Excel-Mappe. (#436)
- Seit 26.09.2026: Ein Balkenbild zeigt die Bandbreite der Kapitalwertdifferenz je Version, auf der Seite
  und im Wortbericht. (#436)
- Seit 26.09.2026: Wort- und Tabellenbericht führen den Verlauf aller drei Szenarien; ihre Szenarientafeln
  nennen die Szenarien „Ungünstig", „Erwartet" und „Günstig". (#436)
- Seit 26.09.2026: Die Stromsteuerbefreiung nach § 9 Abs. 1 Nr. 3 StromStG prüft den CO₂-Grenzwert von
  270 g/kWh brennwertbezogen; die Herleitung nennt den Wert je Anlage. (#437)
- Seit 26.09.2026: Die vermiedenen Stromkosten im Rollentarif beziehen sich auf den Strombedarf ohne jede
  Eigenerzeugung; Blockheizkraftwerk und Photovoltaik erhalten je ihren Anteil nach dem Eigenverbrauch. (#437)

Keinen Eintrag bekommen die Fehlerbehebungen aus #414 und
#415 (Farbwähler-Wechsel, Klimaregionenliste, Auswahlfeld) — Kleinigkeiten nach Regel 13.4.

*Zurückgestellt gegenüber den Rohentwürfen:* der engere Klimadaten-Satz aus #404 und die
beiden Sätze aus #411 zu „Wärmeproduktion/Stromproduktion“ sowie zu den Bedarfs- und
Quellprofildialogen — sie beschreiben dieselbe Bedienung wie oben, nur an weniger Stellen.

### Version 1.2.0.3

- Seit 26.09.2026: Die Wirtschaftlichkeit vergleicht wahlweise alle Varianten gegen ein
  wählbares Referenzprojekt oder zwei Stände gegeneinander. (#358)
- Seit 26.09.2026: Eine Variante übernimmt die Photovoltaik-Vergütung des Stammprojekts oder
  führt eigene Werte. (#359)
- Seit 26.09.2026: Beim Weitergeben einer Variante reist die Photovoltaik-Vergütung des
  Stammprojekts mit. (#360)
- Seit 26.09.2026: Die Übernahme ins Projekt zeigt unter „Aus Vorlage/Variante“ die Auswahl
  der Kostenverwaltung aus der Administration samt den Positionen der gewählten Variante.
  (#363)
- Seit 26.09.2026: Die Betriebskosten weisen die Bezugsgrößen aus dem Simulationslauf auch
  für gespeicherte Läufe aus, und eine Position ohne Bezugsgröße nennt den Grund samt Abhilfe.
  (#364)
- Seit 26.09.2026: Hilfsenergiekosten nach „% des Endenergiebedarfs“ werden bei Anlagen, die
  selbst Strom beziehen, mit dem Arbeitspreis ihres eigenen Energieträgers bewertet; weicht
  die Bemessung einer Kostenposition von der der Vorlage „Standard“ ab, zeigt der Kostendialog
  den Vorlagenwert mit der Möglichkeit, ihn zu übernehmen. (#366)
- Seit 26.09.2026: Klimaregionen lassen sich aus den PVGIS-Daten oder aus einem
  DWD-Testreferenzjahr anlegen — aus einer eigenen Datei oder aus den offenen Regionaldaten.
  (#367)
- Seit 26.09.2026: Beim Einlesen einer Testreferenzjahr-Datei trägt das Programm den Standort
  aus dem Dateikopf ein; er bleibt änderbar. (#368)
- Seit 26.09.2026: Die Regionaldaten zeigen auf Knopfdruck, welche Region ein Standort trifft,
  bevor eingelesen wird; zu jeder Stunde werden zusätzlich Gegenstrahlung, Luftfeuchte und —
  soweit die Quelle sie führt — der Bedeckungsgrad gespeichert. (#369)
- Seit 26.09.2026: Die Klimadaten-Liste hat Suche, Filter und Sortierung und zeigt Quelle,
  Standort und Importdatum; auf der Übersicht ist die Klimawahl durchsuchbar und nennt
  darunter, woher die verwendeten Klimadaten stammen. (#371)
- Seit 26.09.2026: Das Menü Administration ist neu geordnet: Gebäude und Klimadaten stehen
  vorn, die gesetzlichen Parameter unter Kosten, die Dublettenprüfung unter Daten & Import und
  die Einstellungen am Ende. (#372)
- Seit 26.09.2026: Die Lizenz wird nur noch unter Hilfe → Lizenz verwaltet; der Dialog nennt
  vor der Aktivierung, welche Angaben an den Lizenzserver übertragen werden. (#372)
- Seit 26.09.2026: Die gesetzlichen Parameter lassen sich wie die Gerätekataloge durchsuchen,
  filtern und sortieren. (#372)
- Seit 26.09.2026: Projekte lassen sich zu mehreren auf einmal ausgeben — eine gewählte
  Variante nimmt ihr Stammprojekt mit —, und mehrere Paketdateien lassen sich in einem Zug
  einlesen. (#373)
- Seit 26.09.2026: Die Klimadaten-Liste und die Übersicht nennen zur Quelle auch das
  Bezugsjahr und das Szenario der verwendeten Klimadaten. (#382)
- Seit 26.09.2026: Der Gesamtwirkungsgrad eines BHKW wird im Katalog einheitlich als Faktor
  geführt; Gasverbrauch, Emissionen und Brennstoffkosten der betroffenen Module fällt damit
  richtig aus. (#383)
- Seit 26.09.2026: Der BHKW-Katalog führt elektrischen und thermischen Wirkungsgrad; der
  Gesamtwirkungsgrad ergibt sich daraus. (#392)
- Seit 26.09.2026: Die Farben der Diagramme lassen sich in den Einstellungen je Größe ändern;
  der Bericht nimmt dieselben Farben. (#403)

*Ohne ausformulierten Satz:* Statuszeile #377 zählt zu dieser Version „die Logbuch-Sätze 1–14
(+ #361)“; für den Rasterfußzeilen-Befund aus #361 (Fußzeile der Rasterkarte bei hoher
Zeilenschrift nicht mehr abgeschnitten) liegt in der Statusdatei kein ausformulierter Satz vor
— beim Anwender zu erfragen, ob er einen eigenen Eintrag will oder als Kleinigkeit gilt.
*Zurückgestellt:* der engere Klimadiagramm-Satz aus #396 — er geht in der Sache im Satz zu
#404/#413 (Version 1.2.0.x oben) auf.

## 3 Offene Punkte

**Seiten ohne Wiki-Quelle.** Für folgende Bedienung gibt es keine Repo-Quelle unter
`Projekte/Wiki/` und damit keine Bedienungsseite (Statuszeilen #383, #392, #411, #413):

- Wärmepumpe (Kennlinien), Erdreichquelle, Lastspitzenkappung (eigene Maske) — je ein
  fachlicher Dialog ohne eigene Seite;
- Bedarfsergebnis, Quellprofil, Bedarfstyp, Gebäudetyp, Wärmebedarf extern, Stromganglinie,
  Gebäude — die „sieben Dialoge“ aus Statuszeile #411, deren Bedienung unbeschrieben ist
  (Wiki-Runde nötig: eigene Seiten oder Verweis auf „Die Diagramme bedienen“ der Seite
  Simulationsergebnisse);
- Gerätekataloge — ob eine eigene Seite „Programm Dokumentation/Gerätekataloge“ entsteht, ist
  laut Statuszeile #383 ein offener Anwenderentscheid.

**Was bis zum 26.09.2026 noch dazukommt.**

- Die drei Gerätemeldungen (Statuszeile #415: Klimaregionenliste, Auswahlfeld, sortiert)
  berühren keine Bedienungsseite; der Schalter „sortiert" steht als Logbuch-Satz oben.
- Die Wirtschaftlichkeits-Umsetzung war mit Statuszeile #405 zurückgestellt und ist am
  22.09.2026 wieder aufgenommen (E4 #432, E5 #434, E6 #436, E7 Teil a #437). Die Repo-Quelle
  `Projekte/Wiki/Programm Dokumentation - Wirtschaftlichkeit.wiki` ist mit den Papieren zu #436 auf
  die Ergebnisseite nach E5 und E6 nachgezogen (Umschalter, vier Abschnitte, Empfehlungskarten,
  Bandbreite mit Spannenbild, Verlauf mit drei Szenarien, „Bericht erzeugen", „Verlauf nach Excel…",
  Szenarien „Ungünstig"/„Günstig"; Tabuwort-Regex 0 Treffer), mit den Papieren zu #437 um die
  brennwertbezogene CO₂-Prüfung der Stromsteuerbefreiung und die vermiedenen Stromkosten ohne jede
  Eigenerzeugung (Tabuwort-Regex 0 Treffer); die Logbuch-Sätze zu #432, #434, #436 und #437 stehen oben.
  Der neue Leereintrag „(bitte wählen)" der Anlagenart im BHKW-Dialog (#437) ist eine Kleinigkeit ohne
  eigenen Satz.
- Der KI-Assistent bekommt die Masken mit Einstellwerten in sechs Wellen (Statuszeile #416,
  KI‑D‑Q5); je Welle wächst die Maskenliste der Seite Hilfe-Assistent, und ein Logbuch-Satz
  kommt dazu.
- DG‑E5 (Statuszeile #418) ist gemergt: Die Seite Simulationsergebnisse sagt jetzt „Jede Kurve
  und jede Fläche trägt ein Farbfeld", der Logbuch-Satz steht oben.
- Ältere, mit „Version offen“ oder „Version 1.2.0.1“/„1.2.0.2“ vorbereitete Logbuch-Sätze aus
  Statuszeilen vor #358 sind für dieses Papier nicht erneut geprüft; vor dem Hochladen klären,
  ob sie in einer früheren Runde schon veröffentlicht wurden oder noch offen sind.

# Prüfung der Konzeptfamilie Gebäudesimulation auf Konsistenz und Umsetzbarkeit

**Datum:** 17.09.2026 — **Gegenstand:** alle geltenden Papiere zur Gebäudesimulation nach
VDI 6007 (Leitkonzept, Umsetzungskonzept, Rechenschritte, Softwarearchitektur, Systementwurf,
Kühlkonzept, Anlagenkopplung, Mehrzonenmodell, Datenaustausch, ADR-002/003/004/005/006, Register,
Statusdatei, Befunde A–X) — **Anlass:** Entscheid E26.

**Auftrag im Wortlaut (Anwender, 17.09.2026):** „gehe alle Konzepte (.md) im Zusammenhang mit der
Gebäudesimulation nach VDI 6007 nochmals durch und prüfe auf Konsistenz und Umsetzbarkeit …
Insbesondere … 1. das Altmodell noch als Übergang funktioniert und 2. … das Neumodell (VDI 6007)
das alte später komplett ablösen wird und eigenständig arbeiten muss. Vor allem Kühlung … mit den
Wärmepumpen mit Kühlfunktion …"

**Vorgehen.** Sechs Prüfagenten haben je einen Blickwinkel bearbeitet — ALT (Altweg als Übergang
und Ablösung), VDI (eigenständiger VDI-Rechenweg, Datenfluss und Schema), KUE (Kühlung und
Wärmepumpen mit Kühlfunktion gegen den Code), AKM (Anlagenkopplung, Mehrzonen, IFC/gbXML), QUE
(papierübergreifende Konsistenz von Fakten, Namen, Nummern und Verweisen) und PHY (Physik und
Rechenweg einschließlich Kühlfall). Zu jedem Blickwinkel lief ein Gegenprüfer mit dem
ausdrücklichen Auftrag, jeden einzelnen Befund zu widerlegen; wo Vorschlag und Gegenprüfung
auseinandergehen, geht die Korrektur des Gegenprüfers vor. Alle zwölf Agenten liefen mit dem
Modell `opus`; Zerlegung, Abnahme und Zusammenführung lagen bei Fable 5.1. Jeder Befund ist am
Arbeitsbaum belegt — Papierstelle mit Zeilennummer, Quelltextstelle mit Datei und Zeile.

**Zahlen.**

| Blickwinkel | Befunde | bestätigt | teilweise | widerlegt | Ergänzungen |
|---|---|---|---|---|---|
| ALT — Altweg als Übergang | 19 | 5 | 13 | 1 | 5 |
| VDI — eigenständiger VDI-Weg | 19 | 14 | 5 | 0 | 5 |
| KUE — Kühlung und Wärmepumpen | 21 | 12 | 6 | 3 | 5 |
| AKM — Anlagenkopplung, Mehrzonen, IFC | 24 | 15 | 7 | 2 | 5 |
| QUE — papierübergreifende Konsistenz | 20 | 14 | 6 | 0 | 5 |
| PHY — Physik und Rechenweg | 25 | 18 | 3 | 4 | 4 |
| **Summe** | **128** | **78** | **40** | **10** | **29** |

Zusammen 157 geprüfte Punkte. Aus ihnen sind der Entscheid E26 (Nachtrag N1.31 des Leitkonzepts)
und 31 Festlegungen F-Ü1 bis F-D1 hervorgegangen, dazu fünf neu oder wieder offene Punkte.


## 0 Das Wichtigste in zehn Sätzen

Die Konzeptfamilie ist tragfähig: Kein Befund stellt den eingeschlagenen Weg in Frage, und keiner
verlangt, ein Papier neu zu schreiben. Die Trennung der Rechenwege ist sauber gebaut — eine Weiche
am Eingang, zwei Module, ein modellfreier Vorbereitungsschritt außerhalb beider Module, eine Wache
in beide Richtungen. Mit E23 ist sie jedoch auf „dauerhaft" umgeschrieben worden und hat dabei das
Ausbaurezept, den Auslöser und den Einfrierschritt der Ablösung verloren. Entscheid E26 holt
beides zurück: Der Altweg ist Übergang, die Stufe GA ist wieder die letzte Stufe des Plans — ohne
Termin und in keiner Summe —, Q24 und Q25 sind wieder offen. Der VDI-Weg ist beschreibungsreif;
Physik, Diskretisierung, Klimaweg, Klassenschnitt, Schemaschritte, Wächter und Abnahmeleiter sind
belegt und plattformfrei. Drei seiner Verträge waren widersprüchlich — der Inhalt des
Vorbereitungsschritts, die Zahl der Modulläufe je Verbrauchsgebäude und der Fehlerweg des Moduls
—; alle drei sind jetzt an je einer Stelle festgeschrieben. Das Kühlkonzept ist belastbar, aber
sechs Bestandsstellen, die die Kanalzahl selbst aufbauen, fehlten in seiner Stellenliste, und der
Zugriff auf die Laststufe der Kühlkennlinie fehlte ganz. Anlagenkopplung, Mehrzonenmodell und
IFC/gbXML sind fachlich dicht, doch die Softwarearchitektur kannte die Anlagenkopplung an keiner
Stelle, und die Zonenschemaschritte waren in zwei Papieren doppelt belegt. Der Schemastand war in
sechs Papieren überholt; feste Schrittnummern weichen jetzt überall den Papiernamen M3 und M4. Die
Physik im Kern ist tragfähig, aber der Fensterzweig war nach E14 nicht durchgezogen und der
Kühlfall im Rechenbuch nicht nachgezogen — beides ist mit F-P1 und F-P3 geschlossen.


## 1 Entscheid E26 und was er ändert

Der Entscheid steht im Leitkonzept als Nachtrag N1.31
([`../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md));
dort ist er die verbindliche Fassung. Kurz:

- **E23 bedeutet „jetzt", nicht „auf Dauer".** Der Altweg bleibt als funktionierender Übergang.
  Die Lesart „dauerhaft, nie entfernen" aus N1.28 ist zurückgenommen; alle Sachaussagen von N1.28
  zum Altweg selbst bleiben (eigenes Modul `Altweg/`, byte-gleich verschoben, keine neue Funktion,
  keine Kühllast, keine Anlagenkopplung, keine Zonen, je Gebäude wählbar, Ausweis „Tagesbilanz
  (Bestandsweg)", Kältebedarf 0 mit Hinweis, feste Last in der Anlagenkopplung).
- **Die Stufe GA — Altweg ablösen kehrt zurück**, als letzte Stufe ohne Termin und in keiner
  Summe (5–8 PT). Ihren Umfang führt die Löschliste im Umsetzungskonzept Kapitel 6; jede Stufe,
  die einen Altweg-Sonderfall einführt, trägt ihn im selben Auftrag dort ein (ADR-006).
- **Q24 ist wieder offen** und lautet jetzt: Wann ist der VDI-Weg bewährt genug, dass GA
  beauftragt wird? **Q25** lebt wieder als Umfang der Stufe GA.
- **Eigenständigkeit des VDI-Wegs:** Der Vorbereitungsschritt liefert nur, was ohne Modellauf
  feststeht; Bewohnerzahl und Skalierung entstehen je Modul aus dessen erstem Lauf. Der
  Klimakalender zerfällt in einen gemeinsamen und einen Altweg-Teil; das VDI-Modul bekommt nur den
  gemeinsamen. Die NULL-Vorgabe der Ost- und Westfenster bildet der Vorbereitungsschritt, und GA
  füllt die beiden Spalten einmalig, bevor das Bestandsfeld entfällt.
- **Neu ist die Ausbauprobe:** statisch — außer Weiche, Rückweg-Test und Wache nennt keine Datei
  des Kerns `Altweg/`; als Gate von GA — ein Bau mit umbenanntem Ordner `Altweg/` übersetzt nach
  Entfernen der Weiche, und der Referenzlauf aller Projekte ohne Altweg-Gebäude bleibt byte-gleich.
- **Wortwahl in allen Papieren:** „dauerhaft" wird zu „bis zur Ablösung (Stufe GA, Zeitpunkt
  offen)" oder „für die Dauer des Übergangs"; „eine Stufe GA gibt es nicht" wird zu „Stufe GA,
  Zeitpunkt offen (Q24)". Das Risiko „zwei Rechenwege nebeneinander" gilt bis GA. „Bestandsweg"
  bleibt der Name des Altwegs in Dialog und Bericht. Fassade und Vorbereitungsschritt bleiben über
  die Ablösung hinaus; Weiche, `IGebaeudeRechenweg` und Wache leben bis GA.
- **Referenzbasis:** Referenzprojekt auf dem Altweg und Rückweg-Test bleiben bis GA in jeder Basis;
  GA ist ein eigener Einfrierschritt, ebenso eine erst nach der Verschiebung gefundene,
  ergebniswirksame Fehlerbehebung im Altweg.


## 2 Befunde je Blickwinkel

Spalte **Urteil**: Ergebnis der Gegenprüfung. Spalte **Erledigung**: „umgesetzt nach Festlegung
F-xx" — mit dem Nachziehauftrag erledigt, Regel steht in Kapitel 3; „umgesetzt" — ohne eigene
Festlegung nachgezogen; „widerlegt, nichts zu tun"; „offener Punkt" — im Register geführt;
„Messung/Aufgabe in Stufe G0/G1" — erst dort entscheidbar.

### 2.1 Blickwinkel ALT — Altweg als Übergang, spätere Ablösung, Eigenständigkeit des VDI-Wegs

Die Konzeptfamilie beschreibt die Trennung der Rechenwege technisch sauber und weitgehend
widerspruchsfrei — eine Weiche am Eingang, zwei Module, ein Vorbereitungsschritt außerhalb von
`Altweg/`, eine Wache in beide Richtungen. Der Bruch liegt nicht in der Bauform, sondern in der
Wortwahl: Mit E23 ist der Übergang an rund einem Dutzend Stellen zu einem Dauerzustand
umgeschrieben worden, GA ist aus allen vier Stufentabellen verschwunden, Q24 als „nie" beantwortet
und das einzige zusammenhängende Ausbaurezept für gegenstandslos erklärt worden. Damit fehlten
Ablösekriterium, Einfrierschritt und Löschliste — nichts davon ist verloren, alles steht noch in
N1.1 und N1.25, aber keine geltende Tabelle führte es. Die Eigenständigkeit des VDI-Wegs war an
drei Stellen nicht durchgezogen: Der Klimakalender reicht Altweg-Reihen ins VDI-Modul, die
NULL-Vorgabe der Ost- und Westfenster hängt an einem Altweg-Feld, und die Trennungswache prüft
gerade die Richtung nicht, in der eine neue Kopplung entstehen kann.

| Kennung | Schwere | Urteil | Papier und Stelle | Befund in einem Satz | Erledigung |
|---|---|---|---|---|---|
| ALT-01 | hoch | teilweise | Konzept, N1.28 (Z. 2625–2628, 2657–2661) | N1.28 erklärt den Übergang für beendet und das Risiko zweier Rechenwege zur gewählten Bauform. | umgesetzt (E26/N1.31; Satz gestrichen) |
| ALT-02 | hoch | teilweise | Befund X, Vorspann Kap. 4 (Z. 412) | Das einzige zusammenhängende Ausbaurezept der Stufe GA ist für gegenstandslos erklärt. | umgesetzt (E26/N1.31; Vorspann neu gefasst) |
| ALT-03 | hoch | teilweise | Konzept 6.1 (Z. 1050–1051); Befund X 1.2/4.1 | Die NULL-Vorgabe der Ost- und Westfenster hängt an einer Spalte, die mit GA entfällt. | umgesetzt nach Festlegung F-Ü5 |
| ALT-04 | hoch | bestätigt | Umsetzungskonzept 1.8/1.9 (Z. 679–682, 714–717), 4 (Z. 1673) | Die GB-Basis wird zum dauerhaften zweiten Prüfstand erklärt; Systementwurf 8.4 entscheidet das Gegenteil. | umgesetzt nach Festlegung F-Ü7 |
| ALT-05 | hoch | widerlegt | Umsetzungskonzept 1.5 (Z. 419–428) | Der Vertrag des modellfreien Vorbereitungsschritts stehe in zwei Fassungen da. | widerlegt (N1.25 löst ihn auf); Wortlaut nach F-Ü1 |
| ALT-06 | hoch | bestätigt | Systementwurf 3.1, Bild 2 (Z. 300–320) | Das Sequenzbild legt die Verbrauchs-Rückrechnung vor die Weiche und ruft das Modul nur einmal. | umgesetzt nach Festlegung F-Ü2 |
| ALT-10 | hoch | teilweise | Softwarearchitektur 2.8 (Z. 1125–1141); Systementwurf 0/8.3 | Die Einfrierkette schließt einen vierten Anlass aus, obwohl GA selbst einer ist. | umgesetzt (E26) nach Festlegung F-A1 |
| ALT-11 | hoch | teilweise | Statusdatei 1 (Z. 52); Konzept 13 (Z. 1509) | Mit „Q24 = nie" ist auch das Ablösekriterium entfallen. | offener Punkt Q24 (Empfehlung in N1.31) |
| ALT-07 | mittel | bestätigt | Konzept 6.1 (Z. 1074–1077); Softwarearchitektur 2.4 (Z. 965–970) | Das Konzept schließt den Schemaschritt aus, der Altweg-Spalten und `Gebaeude_Modell` entfernt. | umgesetzt (E26; GA-Schemaschritt) |
| ALT-08 | mittel | teilweise | Umsetzungskonzept 1.1 (Z. 168–171) | Weiche, `IGebaeudeRechenweg` und Trennungswache werden als Dauereinrichtungen erklärt. | umgesetzt (E26; Wortwahl) |
| ALT-09 | mittel | teilweise | Konzept N1.28 (Z. 2648); ADR-006 (Z. 132–133); Konzept 10.4 | Referenzprojekt und Rückweg-Test werden dauerhaft festgeschrieben, A15 im alten Wortlaut. | umgesetzt (E26) nach Festlegung F-Ü7 |
| ALT-12 | mittel | teilweise | Softwarearchitektur 1.7, AR16 (Z. 579, 586) | Die Trennungswache prüft die Richtung nicht, in der eine neue Kopplung entsteht. | umgesetzt nach Festlegung F-Ü4 |
| ALT-13 | mittel | teilweise | Befund X 2.4 (Z. 263–265), 3.2 (Z. 330–341) | Der Wertträger Klimakalender reicht Altweg-Reihen an das VDI-Modul. | umgesetzt nach Festlegung F-Ü3 |
| ALT-14 | mittel | teilweise | Konzept 4.4 (Z. 614–619); Umsetzungskonzept 1.2/1.10; Rechenschritte 1.2 | Die Quelle der Wochenendmaske steht in zwei Fassungen da. | umgesetzt nach F-Ü8; offener Punkt U7 |
| ALT-15 | mittel | bestätigt | ADR-002, Vermerk Z. 4 und Entscheidung 3 (Z. 72–77) | Entscheidung 3 verlangt weiterhin eine Modellwahl für die Flächen- und Bewohnerrechnung. | umgesetzt nach Festlegung F-Ü10 |
| ALT-16 | mittel | teilweise | Umsetzungskonzept 4 (Z. 1682–1684, 1690); Konzept 11 | Das Umsetzungskonzept begründet seinen Mehraufwand gegen eine überholte Konzeptzahl. | umgesetzt nach Festlegung F-S8 |
| ALT-17 | mittel | bestätigt | Statusdatei 2 (Z. 68); Umsetzungskonzept 4 (Z. 1680) | GA ist in allen vier Stufentabellen gelöscht oder mit 0 PT geführt. | umgesetzt (E26; GA-Zeile zurück) |
| ALT-18 | mittel | teilweise | Befund X 3.4 (Z. 382–385); Umsetzungskonzept 1.5 (Z. 407–409) | Ein Altweg-Gebäude ohne Tagesverteilung bricht die ganze Bedarfsrechnung ab. | umgesetzt nach F-Ü6; offener Punkt U17 |
| ALT-19 | niedrig | teilweise | Umsetzungskonzept 1.6 (Z. 546–550) | Für den Auslieferungskatalog gibt es keine Regel zu `Gebaeude_Modell`. | umgesetzt nach Festlegung F-Ü9 |
| ALT-E1 | mittel | Ergänzung | Konzept N1.1 (Z. 1586–1587), N1.25 Punkt 4 und 6 | Der Wortlaut der Ablösung steht noch im Konzept und ist wieder in Kraft zu setzen. | umgesetzt (E26/N1.31) |
| ALT-E2 | niedrig | Ergänzung | Befund X 1.2 (Z. 132–133), 4.3, X1 | Die Hülle schreibt beim Speichern zwei Altweg-Flags, die mit GA entfallen. | umgesetzt (Löschliste der Stufe GA) |
| ALT-E3 | mittel | Ergänzung | Kühlkonzept F-K18 (Z. 234); Anlagenkopplung F-A18 (Z. 233) | Jede neue Stufe legt einen Altweg-Sonderfall an, ohne dass sie irgendwo gesammelt würden. | umgesetzt (ADR-006: Regel zur Löschliste) |
| ALT-E4 | niedrig | Ergänzung | Statusdatei 1 (Z. 26 gegen 52 und 68) | Die Statusdatei widerspricht sich in derselben Tabelle. | umgesetzt (Statusdatei) |
| ALT-E5 | mittel | Ergänzung | Softwarearchitektur 4 (Z. 1909) | Der Rückweg-Test ist mit einem Umfang beschrieben, der nicht durchzuhalten ist. | umgesetzt nach Festlegung F-Ü7 |

### 2.2 Blickwinkel VDI — Eigenständiger VDI-Rechenweg im Kern: Datenfluss, Schema, Umsetzbarkeit

Die Papierfamilie beschreibt den VDI-6007-Weg so genau, dass er im Rechenkern gebaut werden kann:
Physik, Diskretisierung, Klimaweg, Klassenschnitt, Schemaschritte, Wächter und Abnahmeleiter sind
belegt und plattformfrei. Umsetzbar war er trotzdem noch nicht in jedem Punkt: Drei Verträge sagen
in drei Papieren Verschiedenes — der Inhalt des Vorbereitungsschritts, die Zahl der Modulläufe je
Verbrauchsgebäude und der Fehlerweg des Moduls. Die festen Schemaschritt-Nummern sind im Bestand
längst anderweitig vergeben und der in den Papieren genannte Schemastand überholt. Dazu kommen ein
mit den vorhandenen Daten nicht rechenbarer Rückfall für die fehlende Gegenstrahlung, ein
Abnahmefenster aus einer Messung ohne den heute geltenden Abzug, eine Schemaspalte ohne Leser und
die Naht `IGebaeudeRechenweg`, die in keinem Vertrags- oder Nahtkapitel definiert ist.

| Kennung | Schwere | Urteil | Papier und Stelle | Befund in einem Satz | Erledigung |
|---|---|---|---|---|---|
| VDI-01 | hoch | teilweise | Rechenschritte 1.1 (Z. 118–122) | Die Schemaschritt-Nummern sind im Bestand vergeben, der genannte Schemastand ist überholt. | umgesetzt nach Festlegung F-S1 |
| VDI-02 | hoch | bestätigt | Systementwurf 3.1, Bild 2 (Z. 302–307) | Der modellfreie Vorbereitungsschritt kann Bewohnerzahl und Skalierungsfaktor nicht liefern. | umgesetzt nach Festlegung F-Ü1 |
| VDI-03 | hoch | bestätigt | Rechenschritte 8.3 (Z. 956–959) | Ob das VDI-Modul je Verbrauchsgebäude ein- oder zweimal läuft, sagen drei Papiere verschieden. | umgesetzt nach Festlegung F-Ü2 |
| VDI-04 | hoch | bestätigt | Rechenschritte 1.2 (Z. 197) gegen 6/E5 (Z. 652–654) | Der Rückfall für fehlende Gegenstrahlung ist zweimal verschieden und in einer Fassung nicht rechenbar. | umgesetzt nach Festlegung F-S3 |
| VDI-05 | hoch | bestätigt | Konzept 10.4 (Z. 1418–1426) | Das positive Abnahmekriterium stammt aus einer Messung ohne den heute geltenden Abzug. | umgesetzt nach F-S6; Messung in G0 |
| VDI-06 | hoch | bestätigt | Rechenschritte 7.2 (Z. 820–821) | Das Rechenbuch führt die Kühlung noch als informative Kappung ohne vierten Kanal. | umgesetzt nach Festlegung F-P3 |
| VDI-07 | hoch | bestätigt | Umsetzungskonzept 1.8 (Z. 629–634) | Der Referenzlauf-Export liest ein `internal`-Ergebnis aus einer eigenständigen Assembly. | umgesetzt nach Festlegung F-S4 |
| VDI-08 | hoch | bestätigt | Umsetzungskonzept 1.5 (Z. 407–408) | Der Fehlerweg des VDI-Moduls ist gegen den Vertrag V1 widersprüchlich festgelegt. | umgesetzt nach Festlegung F-Ü6 |
| VDI-09 | mittel | teilweise | Rechenschritte 6, Ausgabe Schritt E (Z. 722–725) | Wer die inneren Lasten auf Luft und Oberflächen aufteilt, sagen zwei Papiere verschieden. | umgesetzt nach Festlegung F-P2 |
| VDI-10 | mittel | teilweise | Umsetzungskonzept 1.1 (Z. 154–157) | Der Vorbereitungsschritt liest den vollständigen Altweg-Klimakalender für beide Module. | umgesetzt nach Festlegung F-Ü3 |
| VDI-11 | mittel | teilweise | Umsetzungskonzept 1.6, Codeskelett (Z. 506–518) | Im abgedruckten Schrittkörper fehlt die mit E19 verlangte Spaltenumbenennung. | umgesetzt nach Festlegung F-S2 |
| VDI-12 | mittel | bestätigt | Rechenschritte 10.5 (Z. 1319–1321) | Die Planungsgröße der Rechenzeit stammt aus einer Messung, die der Auslieferungsweg nicht fährt. | umgesetzt nach F-S6; Messung in G0 |
| VDI-13 | mittel | bestätigt | Konzept 4.4 (Z. 616–619, 663–664) | Wochenendkalender und Ferienprüfung stehen in Fassungen, die die Rechenschritte verworfen haben. | umgesetzt nach F-Ü8 und F-P6 |
| VDI-14 | mittel | teilweise | Register, Kapitel 0 (Z. 51–88) | U6, U7 und U9 fehlen in der Liste dessen, was vor G1 zu entscheiden ist. | umgesetzt nach F-S7; offene Punkte U6/U7 |
| VDI-15 | mittel | bestätigt | Umsetzungskonzept 1.7 (Z. 568) | M4 legt eine Spalte `Windgeschwindigkeit` an, die kein Papier liest. | umgesetzt nach Festlegung F-S3 |
| VDI-16 | mittel | bestätigt | Umsetzungskonzept 1.8 (Z. 636–638) | Zwei Temperaturreihen sind im Export zu Energiereihen geworden. | umgesetzt nach Festlegung F-S4 |
| VDI-17 | mittel | bestätigt | Systementwurf 1.1, F7 (Z. 106) | Die Zahl der Gebäudekennzahlen steht in drei Papieren verschieden, die achte ist nirgends definiert. | umgesetzt nach Festlegung F-S4 |
| VDI-18 | mittel | bestätigt | Softwarearchitektur 1.7 (Z. 586) | Die Trennungswache trägt zwei Namen und einen Prüfumfang, der die Regel nicht abdeckt. | umgesetzt nach Festlegung F-Ü4 |
| VDI-19 | niedrig | bestätigt | Konzept 6.2 (Z. 1087–1088) | Das Konzept verlangt zwei gleichlautende Sichtdefinitionen; die Architektur verbietet das. | umgesetzt nach Festlegung F-S2 |
| VDI-E1 | hoch | Ergänzung | Systementwurf 4/Bild 5; Softwarearchitektur 1.5 (Z. 386–394) | `IGebaeudeRechenweg` ist in keinem Vertrags- oder Nahtkapitel definiert. | umgesetzt nach Festlegung F-S5 |
| VDI-E2 | hoch | Ergänzung | Umsetzungskonzept 1.5, Codeskelett (Z. 400–403) | Die Weiche reicht dem VDI-Modul den vollständigen Altweg-Kalender als Parameter herein. | umgesetzt nach Festlegung F-Ü3 |
| VDI-E3 | hoch | Ergänzung | Rechenschritte 8.2 (Z. 894) gegen 8.3 (Z. 947–950) | `JahresheizwaermeMwh` ist weder als Wert vor noch als Wert nach der Skalierung festgelegt. | umgesetzt nach Festlegung F-S4 |
| VDI-E4 | mittel | Ergänzung | Systementwurf 4, Bild 5; Umsetzungskonzept 1.4 (Z. 391) | Die Kältefassade hat keinen definierten Weg an die Kühlreihe. | umgesetzt nach Festlegung F-S4 |
| VDI-E5 | mittel | Ergänzung | Rechenschritte 7.1, Verletzungsmaß (Z. 767–776) | Die Sommerlüftungsregel führt eine fünfte Umschaltbedingung ein, die die Stundenschleife nicht kennt. | umgesetzt nach Festlegung F-P4 |

### 2.3 Blickwinkel KUE — Kühlung und Wärmepumpen mit Kühlfunktion, Konzept gegen Code

Das Kühlkonzept ist für ein Papier dieses Alters ungewöhnlich belastbar — Kanalarchitektur,
Deckungstrennung, Datenmodell, Einfrierkette und Stufung sind schlüssig, und Befund W und das
Gegenlesen haben die gefährlichsten Bestandsfragen bereits geklärt. Die Prüfung fand vor allem
Bestandsstellen, die die Stellenliste des vierten Kanals nicht führt: fünf Module bauen ihren
Stundenzustand selbst über die Kanalzahl auf, der Bivalenzpunkt summiert kanalneutral, die Kacheln
der Ergebnisansicht und das Schemabild sind dreielementig verdrahtet, und die Berichtsschlüssel
sind ein festes Feld aus drei Kanalnamen. Zwei Proben waren mit einer Schärfe begründet, die die
Energieprobe des Bestands gar nicht hat. Auf der Erzeugerseite fehlten der Ort der Kältedeckung im
Ablauf, die Behandlung einer Wärmepumpe, die am Kühltag zugleich Brauchwasser deckt, und der
Zugriff auf die Laststufe der Kühlkennlinie; die Katalogfrage K20 ist inzwischen gebaut und im
Papier nur nicht nachgezogen.

| Kennung | Schwere | Urteil | Papier und Stelle | Befund in einem Satz | Erledigung |
|---|---|---|---|---|---|
| KUE-01 | hoch | bestätigt | Kühlkonzept 5.0.3, 8.2, 12.1 K20, 11.1 | K20 ist im Arbeitsbaum gebaut und geprüft, das Papier führt sie als offene Frage mit Aufwand. | umgesetzt nach Festlegung F-K1 |
| KUE-02 | hoch | bestätigt | Kühlkonzept 4.1 (Z. 497–500), F-K4, 4.2 | Fünf Module bauen ihren Stundenzustand selbst über die Kanalzahl auf, nicht „in einer Zeile". | umgesetzt nach Festlegung F-K2 |
| KUE-03 | hoch | bestätigt | Kühlkonzept 4.2, 4.3 Stellenliste, 13 | Der Bivalenzpunkt summiert kanalneutral und fehlt in Stellen- und Risikoliste. | umgesetzt nach Festlegung F-K2 |
| KUE-04 | hoch | bestätigt | Kühlkonzept F-K5, F-K4, 10.2 | Der Referenzakkumulator der Energieprobe würde die Kältebeiträge kanalneutral mitzählen. | umgesetzt nach Festlegung F-K3 |
| KUE-05 | hoch | teilweise | Kühlkonzept 4.4 Punkt 3 (Z. 648–655), F-K6 | Bedarfsaussage und Deckungsaussage der Kälteprobe liegen an verschiedenen Orten des Laufs. | umgesetzt nach Festlegung F-K3 |
| KUE-06 | hoch | bestätigt | Kühlkonzept F-K8, 5.2 (Z. 973–991), K8a | Die Umschaltregel „je Tag" ist für den Erzeuger formuliert, nicht für den Kanal. | umgesetzt nach Festlegung F-K4 |
| KUE-07 | mittel | bestätigt | Kühlkonzept 5.5 (Z. 1030–1038), 4.4, 7 | Die Erzeugerreihenfolge der Kälteseite hat weder einen Ort im Ablauf noch eine Ablage. | umgesetzt nach Festlegung F-K4 |
| KUE-08 | mittel | bestätigt | Kühlkonzept 5.1 Festlegung 5, K8c | Die Rückkühlung ins Erdreich trifft eine Bestandsprüfung, die nur den Fall „nur Heizen" kennt. | umgesetzt nach Festlegung F-K4 |
| KUE-09 | mittel | bestätigt | Kühlkonzept 5.1 Festlegung 3, 5.5 | Die Quellbilanz eines Pufferspeichers begrenzt die Kälteerzeugung nicht. | umgesetzt nach Festlegung F-K4 |
| KUE-10 | mittel | teilweise | Kühlkonzept 7.4, 6.4, N-K8 | `Kaeltebedarf_Gesamt` ist bei einem Kältekanal wertgleich mit `Waermebedarf_Kuehlung`. | umgesetzt nach Festlegung F-K4 |
| KUE-11 | mittel | teilweise | Kühlkonzept 4.3 Stellenliste, 8.4 | Die Erzeugerübersicht baut ihre Spalten über ein fest dreielementiges Namensfeld. | umgesetzt nach Festlegung F-K2 |
| KUE-12 | mittel | bestätigt | Kühlkonzept 7.3 (Z. 1288–1290), 5.1 Festlegung 1 | Der projektseitige Kennlinienzugriff verliert die Laststufe. | umgesetzt nach Festlegung F-K4 |
| KUE-13 | mittel | teilweise | Kühlkonzept 5.1 Festlegung 1, K8b | „Teillastfaktor nach dem Muster der Heizseite" beschreibt etwas, das es im Bestand nicht gibt. | umgesetzt nach Festlegung F-K4 |
| KUE-14 | mittel | widerlegt | Anlagenkopplung 7.1 (Z. 918–919, 924) | Anlagenkopplung und Kühlkonzept belegten `Kuehl_Vorlauf` angeblich verschieden. | widerlegt, nichts zu tun |
| KUE-16 | mittel | teilweise | Kühlkonzept 4.3 (#20–#22), 7.4 | Drei weitere Stellen, die an der Kanalzahl hängen, fehlen in der Stellenliste. | umgesetzt nach Festlegung F-K2 (Teile b und c) |
| KUE-17 | mittel | bestätigt | Kühlkonzept 6.1 (Z. 1069–1092), F-K9 | Die Naht des Kältestroms in die Stufenrechnung ist nur abstrakt beschrieben. | umgesetzt nach Festlegung F-K4 |
| KUE-18 | mittel | teilweise | Kühlkonzept 7.3 KU-S3, 7.6, 5.2 | `KU-S3` legt eine Spalte für Regeln an, die weder gerechnet noch zugesagt sind. | umgesetzt nach Festlegung F-K4 |
| KUE-21 | mittel | widerlegt | Kühlkonzept 7.1 (Z. 1254–1258), 9.1 | `Maximaleraumtemperatur` trage zwei Bedeutungen und sei mit „dauerhaft" begründet. | widerlegt, nichts zu tun |
| KUE-15 | niedrig | bestätigt | Kühlkonzept 5.0.1, 5.0.4, 5.1, 15 | Mehrere Belegstellen des Kapitels 5.0 treffen den Arbeitsbaum nicht mehr. | umgesetzt nach Festlegung F-K1 |
| KUE-19 | niedrig | bestätigt | Kühlkonzept 8.4 (Z. 1523–1529), Schwesterpapiertabelle | Die Ergebnisübersicht ist zweispaltig entworfen; die Kälte hat dort keinen Platz. | umgesetzt nach Festlegung F-K5 |
| KUE-20 | niedrig | widerlegt | Kühlkonzept N-K7, 11.1 KU2 | Das Rechenzeitmaß beschreibe nur die Bedarfsseite. | widerlegt, nichts zu tun |
| KUE-E1 | mittel | Ergänzung | Kühlkonzept 4.4, 4.3 (#30), 5.5, F-K6 | Die Schärfe der Kälteprobe ist dreimal mit einer Schärfe begründet, die die Energieprobe nicht hat. | umgesetzt nach Festlegung F-K3 |
| KUE-E2 | hoch | Ergänzung | Kühlkonzept 4.3, 8.4 (Z. 1528), 4.7 | Die Berichtszeitreihen bilden ihre Schlüssel über ein fest dreielementiges Feld. | umgesetzt nach Festlegung F-K2 |
| KUE-E3 | hoch | Ergänzung | Kühlkonzept 8.2 Schritt 3, 5.0.1, 8.5, 10.3 | Der Sperrgrund im Projektdialog fragt die Stammtabelle statt der Projekttabelle. | umgesetzt nach Festlegung F-K4 |
| KUE-E4 | mittel | Ergänzung | Kühlkonzept 7.3 KU-S3 (Z. 1279), 5.1, 8.2, 7.5 | `Kuehl_Vorlauf` ist als REAL festgelegt, die damit gewählte Kennlinienspalte ist INTEGER. | umgesetzt nach Festlegung F-K4 |
| KUE-E5 | mittel | Ergänzung | Kühlkonzept 3.3, 3.5, 10.2, F-K3 | Die Zusicherung „nie beide größer null" wird an Kanalreihen geprüft, gilt aber je Zone. | umgesetzt nach Festlegung F-K3 |

### 2.4 Blickwinkel AKM — Anlagenkopplung, Mehrzonenmodell, IFC/gbXML

Die Konzeptfamilie ist für diesen Blickwinkel fachlich dicht und durchgehend belegt — Physik der
Übergabe, Zonenkopplung nach ADR-005 und der IFC/gbXML-Leseweg ohne Geometriekernel sind tragfähig
beschrieben. Die Brüche liegen an den Rändern: Die Softwarearchitektur, die Bausteine, Wächter und
Stufen führt, kennt die Anlagenkopplung an keiner Stelle, und ihr Einfrierplan schließt gerade die
Anlässe aus, die das Anlagenkopplungspapier je Stufe fordert. Der Verteilungsschlüssel von AK2 ist
zirkulär, weil die Verfügbarkeit vor dem ersten Gebäudelauf entsteht. Die Zonenschemaschritte sind
doppelt belegt: Zwei Papiere legen `Tab_Zone`, `Tab_Zonenluftstrom` und `ID_Nachbarzone` in
verschiedene Schritte und Stufen, und die Zonenspalten aus Kühlung und Anlagenkopplung kennt das
Papier nicht, dem `Tab_Zone` gehört. Dazu ein Entscheidtext, der ein Metapaket nennt, das der
zugehörige ADR aus iOS-Gründen ausdrücklich ablehnt.

| Kennung | Schwere | Urteil | Papier und Stelle | Befund in einem Satz | Erledigung |
|---|---|---|---|---|---|
| AKM-01 | hoch | teilweise | Konzept N1.27 (Z. 2579–2613) | Der Entscheidtext E22 bindet AK2 unverändert an die entfallene Stufe GA. | umgesetzt (E26; Vermerke in N1.27) |
| AKM-02 | hoch | bestätigt | Anlagenkopplung 6.2 (Z. 788–812), 10.1 | Der Verteilungsschlüssel von AK2 ist zirkulär: die Verfügbarkeit entsteht vor jedem Gebäudelauf. | umgesetzt nach Festlegung F-A2 |
| AKM-03 | hoch | bestätigt | Softwarearchitektur 1.2, 1.3, 1.7, 5 | Das führende Architekturpapier kennt die Anlagenkopplung an keiner Stelle. | umgesetzt nach Festlegung F-A1 |
| AKM-04 | hoch | bestätigt | Softwarearchitektur 2.8 (Z. 1125–1130, 1139) | Der Einfrierplan schließt die Anlässe aus, die das Anlagenkopplungspapier je Stufe fordert. | umgesetzt nach F-A1 (und E26) |
| AKM-05 | hoch | bestätigt | Mehrzonen 4.4 S-C (Z. 774) gegen Softwarearchitektur 5, G6b | `Tab_Zonenluftstrom` und `ID_Nachbarzone` liegen in zwei Schritten und zwei Stufen. | umgesetzt nach Festlegung F-M1 |
| AKM-06 | hoch | teilweise | Mehrzonen 9 G6a (Z. 1343) gegen Softwarearchitektur 5, G3 | Welche Stufe `Tab_Zone` anlegt, sagen zwei geltende Papiere verschieden. | umgesetzt nach Festlegung F-M1 |
| AKM-07 | hoch | teilweise | Statusdatei Q9/E3 (Z. 30); Konzept N1.7 gegen ADR-003 | Entscheidtext und Statuszeile nennen ein Metapaket, das der ADR ausdrücklich ablehnt. | umgesetzt nach Festlegung F-D1 |
| AKM-08 | hoch | bestätigt | Mehrzonen 2.4, 2.9, 8.1 Probe 10, 9 | Das Gate „bitgleich zur Einzonenrechnung" ist mit zwei zusätzlichen Vorläufen nicht erreichbar. | umgesetzt nach Festlegung F-M2 |
| AKM-09 | hoch | bestätigt | Mehrzonen 4.1 (Z. 656–663) | Der empfohlene Speicherweg „Löschen und Neuanlegen" zerstört die Importzuordnung. | umgesetzt nach Festlegung F-M2 |
| AKM-10 | mittel | bestätigt | Rechenschritte (gesamt, Z. 987) gegen Anlagenkopplung 10.1–10.2 | Das Rechenbuch kennt weder Schritt H noch den vierten Betriebsfall noch dessen Gültigkeitsmaß. | umgesetzt nach Festlegung F-P7 |
| AKM-11 | mittel | bestätigt | Anlagenkopplung 11.4; Mehrzonen 8.3; Softwarearchitektur 2.8 | Zwei Papiere beanspruchen dieselbe Ordnungszahl für zwei verschiedene Einfrierregeln. | umgesetzt nach Festlegung F-M1 |
| AKM-12 | mittel | bestätigt | Anlagenkopplung 7.1 (Z. 919) gegen Kühlkonzept 7.3 | Der Auslegungspunkt der Kühlübergabe ist gebäude- und anlagenseitig zugleich belegt. | umgesetzt nach Festlegung F-K6 |
| AKM-13 | mittel | bestätigt | Anlagenkopplung 8.1 (Z. 979–980) gegen 6.5 und 8.5 | AK-S1 legt alle dreizehn Spalten zusätzlich in `Tab_Zone`, gegen die eigene Festlegung. | umgesetzt nach Festlegung F-A4 |
| AKM-14 | mittel | bestätigt | Anlagenkopplung 8.1; Kühlkonzept 7.1; Mehrzonen 4.4 S-C | Kein Schemaschritt legt die Zonenspalten der Kühlung und der Anlagenkopplung an. | umgesetzt nach Festlegung F-M1 |
| AKM-15 | mittel | widerlegt | Anlagenkopplung 6.4 gegen 5.3 und 10.2 H4 | `Anlagenfahrplan` dürfe `Gebaeude/` nicht nennen und müsse es zugleich. | widerlegt, nichts zu tun |
| AKM-16 | mittel | bestätigt | Anlagenkopplung 6.2, 11.1, 13.2 H-F6 | Die Verteilungsregel übernimmt nur die Proportionalität des Vorbilds, nicht dessen Randfälle. | umgesetzt nach Festlegung F-A2 |
| AKM-17 | mittel | teilweise | Anlagenkopplung 6.3 (Z. 867–874), 14 | Für AK3 ist nur der typische Fall beziffert, nicht der benannte Grenzfall. | umgesetzt nach Festlegung F-A3 |
| AKM-18 | mittel | bestätigt | Anlagenkopplung 6.2 (Z. 814–821), 11.1 | Ein Altweg-Gebäude zehrt an derselben Verfügbarkeit, ohne dass die Folge benannt wäre. | umgesetzt nach Festlegung F-A2 |
| AKM-19 | mittel | bestätigt | ADR-003, Kräfte Punkt 4 (Z. 47–48) | Der ADR wägt für iOS ab, ohne die Paketgröße der Schema-Assemblies zu nennen. | umgesetzt nach F-D1; Messung in G0 |
| AKM-20 | mittel | teilweise | Anlagenkopplung 8 (Z. 968–970) | Die Angabe zum Schemastand ist falsch und in drei Papieren verschieden. | umgesetzt nach Festlegung F-S1 |
| AKM-21 | niedrig | widerlegt | Datenaustausch 11.2 und 10 gegen 7.3 | Die Herkunftsspalte werde unter zwei Namen und in zwei Schreibweisen geführt. | widerlegt, nichts zu tun |
| AKM-22 | niedrig | teilweise | ADR-005, Kräfte Punkt 4 (Z. 40–41) | Die Rechenzeit des ADR ist gegen die Fassung des Mehrzonenkonzepts überholt. | umgesetzt nach Festlegung F-M2 |
| AKM-23 | niedrig | teilweise | Anlagenkopplung 12.3 (Z. 1677) gegen 12.1 und 7.4 | Die Vorbedingung von AK1 steht an drei Stellen verschieden da. | umgesetzt nach Festlegung F-A4 |
| AKM-24 | niedrig | bestätigt | Mehrzonen 9, G6c (Z. 1345) und Summenzeile | Die Summenzeile trägt die Zahlen, die die Stufenzeile ausdrücklich ausnimmt. | umgesetzt nach Festlegung F-S8 |
| AKM-E1 | mittel | Ergänzung | Anlagenkopplung 5.3 (Z. 663–667) gegen 7.2, 7.3, 10.2 | Die abschließende Aufzählung der Begrenzungsgründe wird vom eigenen Papier dreimal überschritten. | umgesetzt nach Festlegung F-A4 |
| AKM-E2 | mittel | Ergänzung | Anlagenkopplung 8.5 (Z. 1118–1121) gegen 8.1 und 12.2 | Der projektweite Schalter steht nur im ER-Bild — ohne Spaltenzeile, Wertebereich und Vorgabe. | umgesetzt nach Festlegung F-A4 |
| AKM-E3 | hoch | Ergänzung | Anlagenkopplung 6.1 (Z. 735, 777–780) gegen F-A8 und 12.1 | AK1 liegt nicht vollständig in `Gebaeude/`: die Kennlinienwahl bleibt auf der Erzeugerseite. | umgesetzt nach Festlegung F-A4 |
| AKM-E4 | mittel | Ergänzung | Anlagenkopplung 6.2 und 6.5 gegen 10.2 H4 und 12.3 | Die Verfügbarkeit wird nur zwischen Gebäuden verteilt, ab G6 hat ein Gebäude Zonen. | umgesetzt nach Festlegung F-A2 |
| AKM-E5 | niedrig | Ergänzung | Softwarearchitektur 2.8, Zustandsbild (Z. 1134) | Das Zustandsbild der Einfrierkette startet von einer Basis, die es nicht mehr gibt. | umgesetzt nach Festlegung F-D1 |

### 2.5 Blickwinkel QUE — Papierübergreifende Konsistenz von Fakten, Namen, Nummern, Verweisen

Die zehn Papiere sind untereinander dicht verzahnt — Papiernamen, Klassen- und Spaltennamen, Kanal-
und Kennzahlbegriffe und die meisten Aufwandssummen rechnen sauber nach. Der größte
zusammenhängende Fehler ist der Schemastand: Vier Papiere nennen drei verschiedene Zielversionen
mit drei verschiedenen Zeilennummern derselben Konstante, und keine trifft den Code; die festen
Schrittnummern sind im Bestand längst anderweitig vergeben. Die Statusdatei widerspricht sich in
derselben Tabelle, führt nur einen Teil der geltenden Stufen und trägt gemessene Zeilenzahlen, die
nicht mehr stimmen. Das Register zählt seine offenen Punkte an drei Stellen vorbei, und die beiden
Indexzeilen beschreiben den Stand vom 15.09.2026. Dazu kommen drei Aufwandszitate gegen überholte
Zahlen und zwei Stellen, an denen die Bezugsfläche des Stundenmodells noch auf `Wohnflaeche` steht.

| Kennung | Schwere | Urteil | Papier und Stelle | Befund in einem Satz | Erledigung |
|---|---|---|---|---|---|
| QUE-01 | hoch | bestätigt | Umsetzungskonzept 1.6, Hinweiskasten (Z. 454) | Vier Papiere nennen drei verschiedene Schemastände, und keiner stimmt mit dem Code. | umgesetzt nach Festlegung F-S1 |
| QUE-02 | hoch | bestätigt | Mehrzonen 4.4 (Z. 774) und 9 (Z. 1343) | Derselbe Papiername S-C ist zweimal verschieden belegt. | umgesetzt nach Festlegung F-M1 |
| QUE-03 | hoch | bestätigt | Konzept 6.1, 2.x, 10.3, 11, 16, N1.17 | „Schemaschritt 77/78" steht an sieben Stellen als feste Nummer, bis in eine Kapitelüberschrift. | umgesetzt nach Festlegung F-S1 |
| QUE-04 | hoch | bestätigt | Mehrzonen 4.2, `Tab_Zone` (Z. 682) | Das Papier, dem `Tab_Zone` gehört, kennt die Spalten der Schwesterpapiere nicht. | umgesetzt nach Festlegung F-M1 |
| QUE-05 | hoch | bestätigt | Konzept 12, Punkt „Datenbank" (Z. 1469) | Die Spaltenzahl des Gebäudespalten-Schritts steht im Konzept zweimal verschieden. | umgesetzt nach Festlegung F-S2 |
| QUE-06 | hoch | bestätigt | Statusdatei 1 (Z. 26, 49) und 2 (Z. 68) | Dieselbe Tabelle sagt an zwei Stellen das Gegenteil über die Stufe GA. | umgesetzt (E26; Statusdatei) |
| QUE-07 | hoch | bestätigt | Softwarearchitektur 2.4 (Z. 964) | Ein zweiter Durchgang der Gebäude-Schemaklasse ist ausgeschlossen, GA braucht ihn. | umgesetzt (E26; GA-Schemaschritt) |
| QUE-08 | mittel | bestätigt | Register, Q26 (Z. 135) | Das Register gibt als „Empfehlung des Papiers" Aufwände an, die das Papier nicht mehr nennt. | umgesetzt nach Festlegung F-S7 |
| QUE-09 | mittel | bestätigt | Umsetzungskonzept 4 (Z. 1690) | Das Umsetzungskonzept begründet seinen Mehraufwand gegen eine Zahl, die im Konzept nicht mehr steht. | umgesetzt nach Festlegung F-S8 |
| QUE-10 | mittel | teilweise | Kühlkonzept 11.1 (Z. 1887) | Das Kühlkonzept zitiert den Aufwand von G1 und G2 mit einer überholten Spanne. | umgesetzt nach Festlegung F-S8 |
| QUE-11 | mittel | teilweise | Umsetzungskonzept 1.9 (Z. 739) | Derselbe Wächter trägt in zwei Papieren zwei Namen und zwei Umfänge. | umgesetzt nach Festlegung F-Ü4 |
| QUE-12 | mittel | teilweise | Statusdatei 2, Stufentabelle (Z. 59–68) | Die Stufentabelle führt nur einen Teil der Stufen, die die Schwesterpapiere als geltend führen. | umgesetzt (Statusdatei) |
| QUE-13 | mittel | bestätigt | Konzept 6.1, Zeile `Innenflaechenfaktor` (Z. 1057) | Die Bezugsfläche des Stundenmodells steht noch auf „Wohnfläche". | umgesetzt nach Festlegung F-S2 |
| QUE-14 | mittel | teilweise | Register, Vorspann (Z. 34), Kapitel 0, 8.1, 8.2 | Die Zählung der offenen Punkte lässt drei Punkte aus, die das Register selbst als offen ausweist. | umgesetzt (Register); neue Punkte D17 und K24 |
| QUE-15 | mittel | bestätigt | Index (Z. 55 und 122) | Die Indexzeilen zu Hauptkonzept und Statusdatei beschreiben den Stand vom 15.09.2026. | umgesetzt (Index) |
| QUE-16 | mittel | bestätigt | Statusdatei 3, Registerzeile (Z. 81) | Die Statuszeile zählt die gestrichenen Punkte anders als das Register selbst. | umgesetzt (Statusdatei) |
| QUE-17 | mittel | teilweise | Konzept 8.2 (Z. 1334) und 11 (Z. 1446) | Der Vergleich der beiden Rechenwege im Bedarfsdialog ist als dauerhafte Funktion festgeschrieben. | umgesetzt (E26; endet mit GA) |
| QUE-18 | mittel | bestätigt | Anlagenkopplung 8.1 (Z. 979) gegen 8.5 (Z. 1119) | Die Spaltenzahl von AK-S1 geht mit dem eigenen ER-Bild nicht zusammen. | umgesetzt nach Festlegung F-A4 |
| QUE-19 | niedrig | teilweise | Softwarearchitektur 6, A11 (Z. 2051) | Die A11-Liste der Papiernamen ist gegen die eigene Schritttabelle unvollständig. | umgesetzt nach Festlegung F-S7 |
| QUE-20 | niedrig | bestätigt | Statusdatei 3 (Z. 80) und E12-Zeile (Z. 38) | Zwei Angaben der Statusdatei sind messbar überholt. | umgesetzt (Statusdatei; Zeilenzahlen entfallen) |
| QUE-E1 | hoch | Ergänzung | Umsetzungskonzept 1.6 (Z. 454 gegen 457–458) | Dasselbe Kapitel nennt den Schemastand vier Zeilen auseinander zweimal verschieden. | umgesetzt nach Festlegung F-S1 |
| QUE-E2 | mittel | Ergänzung | Mehrzonen 4.4, Absatz unter der Schritttabelle (Z. 779) | Die Begründung der späteren Nummernvergabe zementiert die Kollision, die A11 verhindern soll. | umgesetzt nach Festlegung F-S1 |
| QUE-E3 | mittel | Ergänzung | Kühlkonzept Anhang (Z. 2128); Datenaustausch 7.4 (Z. 1121) | Der Schemastand ist in mehr Papieren überholt, als QUE-01 nennt. | umgesetzt nach Festlegung F-S1 |
| QUE-E4 | mittel | Ergänzung | Konzept 5 (Z. 582) und 5.x (Z. 723–725) | Auch Formel und Prüfgrenzen bilden die Bezugsfläche noch aus `Wohnflaeche`. | umgesetzt nach Festlegung F-S2 |
| QUE-E5 | mittel | Ergänzung | Index, Zeilen 64 und 56 | Die Indexzeile des Registers zählt Spannen einschließlich der gestrichenen Punkte. | umgesetzt (Register und Index) |

### 2.6 Blickwinkel PHY — Physik und Rechenweg VDI 6007 einschließlich Kühlfall

Der Rechenweg ist in seinem Kern tragfähig und ungewöhnlich gut belegt — Zweikapazitätennetz mit
voll besetzter Systemmatrix, exakte Diskretisierung, Blockmittel als Prüfgröße, Bisektion als
einzige Iteration, jede benannte Abweichung mit Begründung. Nicht durchgezogen war der Fensterzweig
nach E14: Schritt C, die äquivalente Außentemperatur, die Bezugsflächen, der Fensterwiderstand und
die stationäre Probe rechnen noch mit dem alten Schnitt, und der Wärmebrückenterm hängt masselos
zwischen Außenluft und Luftknoten. Der Kühlfall ist im Rechenbuch nicht nachgezogen: Es kennt weder
die Leistungsgrenze noch den vierten Kanal, prüft den freien Lauf nur nach unten und mittelt Heiz-
und Kühlanteil einer Stunde gegeneinander weg. Der Abschnittsdeckel liefert stillschweigend ein
Teilstundenergebnis, die Sommerlüftung hat keinen Auswertezeitpunkt, und der Vorlauf ist ohne
Abbruchkriterium beziffert. In der Anlagenkopplung sind vier Stellen der Übergaberechnung
angreifbar: Sekantenleitwert im Regelbereich, Rücklauf nach der Begrenzung, Verzweigung ohne
Proportionalband und die durchgehende Einheitenmischung.

| Kennung | Schwere | Urteil | Papier und Stelle | Befund in einem Satz | Erledigung |
|---|---|---|---|---|---|
| PHY-01 | hoch | bestätigt | Rechenschritte 2/A7 (Z. 322–326), 4.1 (Z. 398–404) | Schritt C ist nach E14 nicht nachgezogen: der Fensterzweig fehlt in den Knotenbilanzen. | umgesetzt nach Festlegung F-P1 |
| PHY-02 | hoch | bestätigt | Rechenschritte 6/E7 (Z. 687–692) gegen 2/A7a | Die Formel der äquivalenten Außentemperatur enthält weder Fensterterm noch Fensterfläche. | umgesetzt nach Festlegung F-P1 |
| PHY-03 | hoch | bestätigt | Rechenschritte 7.1 (Z. 754, 790) | Der Abschnittsdeckel liefert stillschweigend ein Teilstundenergebnis. | umgesetzt nach Festlegung F-P3 |
| PHY-04 | hoch | bestätigt | Rechenschritte 7.1 (Z. 767–777); Kühlkonzept 3.1, 10.2 | Zwei der vier Verletzungsmaße tragen nicht: freier Lauf einseitig, Kühlfall ohne Leistungsgrenze. | umgesetzt nach Festlegung F-P3 |
| PHY-05 | hoch | bestätigt | Kühlkonzept 3.3, Vorzeichenregel (Z. 342–350) | Heiz- und Kühlanteil einer Stunde mitteln sich im Blockmittel gegeneinander weg. | umgesetzt nach Festlegung F-P3 |
| PHY-06 | hoch | bestätigt | Anlagenkopplung 10.2, H3/H5 (Z. 1356–1378), 3.3 | Im Proportionalband ist die maßgebliche Steigung nicht die der voll geöffneten Übergabe. | umgesetzt nach Festlegung F-P7 |
| PHY-07 | hoch | bestätigt | Rechenschritte 2/A7a (Z. 333–337) gegen 3/B6 | Der Fensterwiderstand wird aus einem U-Wert gebildet, der die Übergänge schon enthält. | umgesetzt nach Festlegung F-P1 |
| PHY-08 | hoch | bestätigt | Anlagenkopplung 10.2, H2/H4/H6 (Z. 1350–1381) | Der Rücklauf wird vor der Begrenzung gebildet und danach unverändert ausgegeben. | umgesetzt nach Festlegung F-P7 |
| PHY-09 | mittel | teilweise | Rechenschritte 7.2 und 7.1; Konzept 4.5 | Die Kühlung steht noch als informative Kappung ohne Leistungsgrenze und ohne eigenen Kanal. | umgesetzt nach Festlegung F-P3 |
| PHY-10 | mittel | bestätigt | Rechenschritte 10.4, stationärer Grenzfall (Z. 1301–1302) | Der Fensterzweig fehlt in der Sollformel der stationären Rechenprobe. | umgesetzt nach Festlegung F-P1 |
| PHY-11 | mittel | widerlegt | Konzept 4.4, Grundfläche (Z. 632–639) | Der unbeheizte Keller werde mit einem Reduktionsfaktor statt über die Kellertemperatur gerechnet. | widerlegt; Rev.-1-Kapitel nachgezogen nach F-P0 |
| PHY-12 | mittel | bestätigt | Rechenschritte 1.2 und 6/E8 gegen Konzept 4.4 | Zwei Papiere nennen verschiedene Quellen für die Wochenendmaske. | umgesetzt nach F-Ü8; offener Punkt U7 |
| PHY-13 | mittel | bestätigt | Rechenschritte 2/A7 und 4.5; Kühlkonzept 3.4 | Die Sommerlüftungsregel hat weder Auswertezeitpunkt noch Hysterese noch Mindestverweildauer. | umgesetzt nach Festlegung F-P4 |
| PHY-14 | mittel | bestätigt | Rechenschritte 6/E3, 8.1, 11 Zeile 4 | Die Bezugsflächen berufen sich auf eine benannte Abweichung, die mit E14 entfallen ist. | umgesetzt nach Festlegung F-P1 |
| PHY-15 | mittel | teilweise | Rechenschritte 6/E6 (Z. 679–685) | Die vorgeschriebene Aufrufform des Erdreichprofils gibt es im Bestand nicht. | umgesetzt nach F-P5; Codeänderung in G1 |
| PHY-16 | mittel | bestätigt | Konzept 4.6, Vorlauf (Z. 696–700) | Der feste Vorlauf deckt die langsamen Normtesträume nicht ab. | umgesetzt nach F-P5; Abbruchkriterium in G0 |
| PHY-17 | mittel | widerlegt | Konzept 5.3, letzte Zeile (Z. 806–807) | Die Vorzeichenkonvention der Leistungsreferenz widerspreche der Richtlinie. | widerlegt; Rev.-1-Kapitel nachgezogen nach F-P0 |
| PHY-18 | mittel | widerlegt | Konzept 5.2, Validierungstabelle und Fußnote (Z. 762–793) | Die Validierung laufe gegen eine einzelne Reihe mit gelockerter Schwelle. | widerlegt; Rev.-1-Kapitel nachgezogen nach F-P0 |
| PHY-19 | mittel | bestätigt | Konzept 4.4 (Z. 661–664), 4.8 (Z. 726), 3.3 (Z. 385) | Ferien- und Wochenendprüfung stehen in einer Fassung, die die Rechenschritte verworfen haben. | umgesetzt nach Festlegung F-P6 |
| PHY-20 | mittel | teilweise | Rechenschritte 11, Zeile 15 (Z. 1353); 6/E3 | Die Wirkung der flächenproportionalen Verteilung ist weder gemessen noch zur Messung vorgesehen. | umgesetzt nach F-P1; Messung in G0 |
| PHY-21 | mittel | bestätigt | Rechenschritte 10.5 (Z. 1319–1321) gegen 9 und 9.6 | Die Planungsgröße der Rechenzeit stammt aus Läufen ohne Kühlung und ohne Leistungsgrenze. | umgesetzt nach F-S6; Messung in G0 |
| PHY-22 | mittel | bestätigt | Anlagenkopplung 3.7 und 6.1 gegen 10.2 H5 | Die Verzweigung fragt nicht nach dem Proportionalband und rechnet die Bestandsgleichung. | umgesetzt nach Festlegung F-P7 |
| PHY-23 | mittel | bestätigt | Anlagenkopplung 10.2, Verletzungsmaß (Z. 1386–1392) | Das Verletzungsmaß kennt nur einen der beiden Knicke der Reglerkennlinie. | umgesetzt nach Festlegung F-P7 |
| PHY-24 | mittel | bestätigt | Anlagenkopplung 3.1, 3.2, 3.3, 10.2 H2 | Die Übergaberechnung steht durchgehend in kW, der Löser rechnet in W. | umgesetzt nach Festlegung F-P7 |
| PHY-25 | niedrig | widerlegt | Konzept 4.4, äquivalente Außentemperatur (Z. 620–631) | Der langwellige Term sei bewölkungsabhängig statt über den Sichtfaktor beschrieben. | widerlegt; Rev.-1-Kapitel nachgezogen nach F-P0 |
| PHY-E1 | hoch | Ergänzung | Kühlkonzept 7.1 KU-S1; Rechenschritte 7.2; Anlagenkopplung 7.1/7.4 | Die Kühlleistung bekommt keine Aufteilung in einen konvektiven und einen radiativen Anteil. | umgesetzt nach Festlegung F-P3 |
| PHY-E2 | mittel | Ergänzung | Rechenschritte 2/A7, 4.1 Luftbilanz, 11 | Der Wärmebrückenterm hängt nach E14 masselos zwischen Außenluft und Luftknoten. | umgesetzt nach F-P1; Messung in G0 |
| PHY-E3 | niedrig | Ergänzung | Rechenschritte 8.2 (Z. 900) gegen 7.2; Anlagenkopplung 11.1 | `MittlereRaumtemperaturHeizzeit` ist bei idealer Regelung bauartbedingt aussagelos. | umgesetzt nach Festlegung F-S4 |
| PHY-E4 | niedrig | Ergänzung | Rechenschritte 2/A2 und A6, 11 | Die Bezugsfläche des inneren Strahlungsaustauschs ist eine Festlegung ohne Quellenangabe. | umgesetzt nach Festlegung F-P1 |


## 3 Festlegungen

Die folgenden Festlegungen sind am 17.09.2026 aus den Befunden getroffen worden. Sie werden im
[Register](../Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) Kapitel 8 als „Festlegungen aus
der Prüfung vom 17.09.2026" geführt. **Widerspruch bis zur Beauftragung von G1 möglich;
verbindlich wird die Fassung im Register Kapitel 8.** Zahlenwerte aus VDI 6007, VDI 6020 und
VDI 2078 stehen nicht hier, sondern in den Fachpapieren.

### 3.1 Trennung und Übergang

- **F-Ü1 Vertrag des Vorbereitungsschritts** (eine Stelle: Softwarearchitektur 1.3, Baustein
  `GebaeudeVorbereitung`; alle anderen Papiere verweisen): modellfrei sind allein Klimakalender,
  `VerbrauchNeu` je Einheit, `FlaecheAlt = Wohnflaeche_gesamt` (nach E19: `Nutzflaeche_gesamt`
  bleibt `Wohnflaeche_gesamt`), `Flaeche_Nutzer`, `Einheit`, `Jahresnutzungsgrad`. Bewohnerzahl und
  Skalierungsfaktor nach E8 entstehen **je Modul** aus dessen erstem Lauf; die Fassade führt die
  Schleife.
- **F-Ü2 Ein Lauf oder zwei:** `IGebaeudeRechenweg.Rechnen` liefert aus **einem** Aufruf die Reihe
  und den unskalierten Jahreswert `VerbrauchAltKwh`. VDI-Modul: ein Lauf, Skalierung als
  Nachmultiplikation in der Fassade (Rechenschritte 8.3). Altweg: die Fassade ruft ihn im
  Verbrauchsfall **zweimal** wie im Bestand (byte-gleich, Befund X X5). Systementwurf-Sequenzbild
  3.1: Rückrechnung **hinter** die Weiche, zwei Aufrufe desselben Moduls nur im Altweg-Zweig.
- **F-Ü3 Klimakalender:** Der Wertträger hat zwei Teile: `Klimakalender.Gemeinsam` (`WE[365]`,
  `Stundentemperatur[8760]`, `WochentagJan1`, Monatsgrenzen) und `Klimakalender.Altweg` (`Sol_*`,
  `A_Temp`, `TagTyp_W/NW`). Die Weiche reicht dem VDI-Modul nur `Gemeinsam`, dem Altweg beides
  (Umsetzungskonzept 1.1/1.5, Softwarearchitektur 1.3, Systementwurf V1).
- **F-Ü4 Modultrennungswache:** ein Name — Klasse `Modultrennungswache`, Test
  `EPOS.Kern.Tests/ModultrennungswacheTests.cs`. Prüfumfang: (1) keine Datei unter `Gebaeude/`
  nennt einen Bezeichner aus `Altweg/` oder eine Altweg-Datenquelle (`Sol_N/O/S/W`, `A_Temp`,
  `TagTyp_W/NW`, `Tab_DBTagV`, `Abfrage_Tagverteilung`, `Fensterflaeche_Ost_West`, `Typ` als
  Verteilungsschlüssel, `Klimakalender.Altweg`); (2) keine Datei unter `Altweg/` nennt einen
  Bezeichner aus `Gebaeude/`; (3) die Kältefassade nennt `Altweg/` nicht; (4) Ausbauprobe (a).
- **F-Ü5 `Fensterflaeche_Ost_West`:** Die NULL-Vorgabe von `Fensterflaeche_Ost`/`_West` (je die
  Hälfte) bildet der **Vorbereitungsschritt** (Fassade), nicht das VDI-Modul; sie ist eine
  Abhängigkeit vom Bestandsfeld nur für den Übergang. Die Stufe GA füllt beide Spalten einmalig aus
  `Fensterflaeche_Ost_West`, bevor sie die Spalte entfernt (Konzept 6.1, Umsetzungskonzept,
  Befund X 4.1).
- **F-Ü6 Fehlerweg:** Das VDI-Modul wirft keine Ausnahme, legt eine Meldung der Stufe Fehler im
  Protokollkanal ab und gibt `false` zurück; der Lauf endet an derselben Stelle wie heute
  (Umsetzungskonzept 1.5, Systementwurf V1). Ein Altweg-Gebäude ohne Tagesverteilung bricht wie
  heute den ganzen Lauf ab; das wird in Systementwurf 6 (K4) und Umsetzungskonzept 1.5 als Wirkung
  benannt und ist der neue offene Punkt **U17** (Register: Lauf abbrechen wie heute oder Gebäude
  benannt ablehnen; Empfehlung: benannt ablehnen, aber erst mit GA, weil es das Altweg-Verhalten
  ändert).
- **F-Ü7 Rückweg-Test:** Fassung des Systementwurfs 8.4 gilt: kein zweiter Basisordner; die
  Arbeitskopie gegen die GB-Basis nur bis G1+G2; danach ein Referenzprojekt mit
  `Gebaeude_Modell = TAGESBILANZ` in der jeweils aktuellen Basis (Empfehlung zu A15, A15 bleibt
  offen); Umfang: allein dieses Referenzprojekt, nicht alle Gebäude; endet mit GA.
- **F-Ü8 Wochenendmaske (U7):** Empfehlung wird auf den Ortszeit-Kalender gestellt (Konzept 4.4,
  Entscheid Q21): der Vorbereitungsschritt bildet `WE[365]` aus dem Wochentag des 1. Januar des
  Referenzjahres; eine Probe hält sie gegen `Tab_Klimadaten.WE` derselben Region. U7 bleibt offen,
  kommt in Register Kapitel 0 (fällig vor G1); die Begründung „dieselbe Quelle wie der Altweg …
  dauerhaft" entfällt in Rechenschritte E8 und Umsetzungskonzept 1.10. U6 ebenfalls in Kapitel 0
  (fällig vor dem Einfrieren von G1+G2).
- **F-Ü9 Auslieferung:** Werkzeuge/Auslieferungsvorlage weist im Prüfbericht jede Zeile von
  `Tab_Gebaeude_STAMM` mit gesetztem `Gebaeude_Modell` aus; Vorgabe NULL.
- **F-Ü10 ADR-002:** Ergänzungsvermerk auf „mit Ausnahme von Entscheidung 3, Satz 3" einschränken;
  der Satz wird durch den Hinweis auf E20/ADR-006 ersetzt.

### 3.2 VDI-Rechenweg und Schema

- **F-S1 Schemastand:** In allen Papieren keine festen Schrittnummern mehr: „Schemaschritt 77/78" →
  Papiernamen **M3** (Gebäudespalten-Schritt, G1) und **M4** (Klimaspalten, G2); Konzept 6.1 heißt
  „Gebäudespalten-Schritt (Papiername M3) — `Tab_Gebaeude` und `Tab_Gebaeude_STAMM`". Der Zielstand
  wird bei der Beauftragung an `SchemaStand.Zielversion` abgelesen; beim Schreiben (17.09.2026)
  steht er auf **84**, die nächste freie Nummer ist **85**. Betroffen: Konzept (neun Stellen),
  Umsetzungskonzept 1.6 (Kasten und Fließtext), Rechenschritte 1.1, Softwarearchitektur 2.4,
  Anlagenkopplung 8, Kühlkonzept Anhang, Mehrzonen 2.6/4.4, Datenaustausch 7.4.
- **F-S2 Gebäudespalten-Schritt M3:** Schrittkörper in der Reihenfolge aus N1.24 (1. je Tabelle
  `RENAME COLUMN Wohnflaeche → Nutzflaeche`, wenn vorhanden; 2. neue Spalten; 3. Sicht neu);
  `GebaeudeSchema.SQL_VIEW_NEU` ist ab M3 die **einzige** Quelle der Sichtdefinition,
  `sql/schema/002_views.sql` bleibt der eingefrorene Stand 61 (Konzept 6.2, Umsetzungskonzept 1.6).
  Spaltenzahl: zwölf (G1) + drei (G2) = 15 je Tabelle, 30 Einträge — Konzept 12 nachziehen.
  Bezugsfläche: `Nutzflaeche` statt `Wohnflaeche` in Konzept 6.1 (Zeile `Innenflaechenfaktor`),
  5 (Formel A_IW) und 5.x (Prüfgrenzen).
- **F-S3 M4:** Die Spalte `Windgeschwindigkeit` wird **nicht** angelegt (kein Leser; der äußere
  Wärmeübergang bleibt beim festen Vorgabewert); M4 hat damit zwei Spalten — Zahlen in
  Umsetzungskonzept 1.7, Rechenschritte 1.2/1.3 und Konzept 12 nachziehen. Gegenstrahlung NULL:
  Δθ_lw = 0 und α_str,A auf dem Vorgabewert (E5) — die „Schätzung nach Blatt 3" entfällt
  (Rechenschritte 1.2, Umsetzungskonzept 1.7).
- **F-S4 Kennzahlen je Gebäude: acht**, die achte heißt `Ueberhitzungsstunden` [h] = Stunden der
  Nutzungszeit mit θ_op > `Maximaleraumtemperatur` (ab KU1: > `Kuehl_Sollwert`); gleich in
  Rechenschritte 8.2, Umsetzungskonzept 1.4, Systementwurf F7, Mehrzonen M5.
  `JahresheizwaermeMwh` = Summe **nach** der Skalierung; `VerbrauchAltKwh` = unskalierter Wert des
  ersten Laufs, ausdrücklich getrennt. `MittlereRaumtemperaturHeizzeit` wird über die Nutzungszeit
  aller Stunden gebildet. Temperaturreihen im Referenzlauf-Export in °C, nur der Kühlbedarf in kWh.
  `GebaeudeModellErgebnis` bleibt `internal`; `EPOS.Kern.csproj` bekommt `InternalsVisibleTo` für
  `EPOS.Referenzlauf` und `Referenzlauf`. Das Ergebnisobjekt je Gebäude liegt in einem je Lauf
  gehaltenen Träger neben dem Vorbereitungsergebnis; **beide** Fassaden lesen es.
- **F-S5 `IGebaeudeRechenweg`** wird Vertrag **V16** in Systementwurf 4.1 und Bild 5 (Zweck,
  Eingaben, Ausgaben mit Einheit, Fehlerfall) mit den Ausprägungen `Vdi6007Rechenweg` und
  `TagesbilanzRechenweg`; Softwarearchitektur 1.5 nennt die Naht.
- **F-S6 Rechenzeit:** Planungsgröße in G0 neu messen mit Kappung an θ_kuehl, gesetztem Φ_h,max und
  Φ_c,max, getrennt ohne und mit Kühlung (Rechenschritte 10.5, Kühlkonzept N-K7 unverändert).
  Abnahmekriterium (4) in Konzept 10.4 wird in G0 aus der wiederholten Messung (mit R_si-Abzug,
  Fensterabminderung, Hay-Davies, Ortszeit, konvektivem Anteil) neu bestimmt; bis dahin informativ.
- **F-S7 Register Kapitel 0** nimmt U6 und U7 auf; Q26-Aufwand nach Anlagenkopplung 12.2
  (AK0 1–2, AK1 9–13, AK2 11–15, AK3 23–38 PT); A11-Liste der Papiernamen: GB, M2–M4, S-A bis S-G,
  KU-S1 bis KU-S4, AK-S1 bis AK-S3.
- **F-S8 Aufwand:** Umsetzungskonzept Kapitel 4 ist die Quelle der verbindlichen Aufwände; Konzept
  11 und 12 verweisen darauf; Zitat in Umsetzungskonzept auf „G1 10–16 PT, G2 3–5 PT";
  Kühlkonzept 11.1: „16–22 PT"; Mehrzonen 9 Summe „40–62 PT — ohne X1…X3 (D16)".

### 3.3 Physik (Rechenschritte, Kühlkonzept, Anlagenkopplung)

- **F-P0 Rev.-1-Kapitel des Konzepts nachziehen** (4.4 Keller = θ_NR,eq mit `Kellertemperatur`;
  4.4 langwelliger Term = geometrischer Sichtfaktor, Bewölkung nur über E_A; 5.2 Validierung auf
  das Prüfband nach E10 mit den Zahlen aus N1.14/N1.15, Fußnote Messfenster streichen; 5.3
  Vorzeichen: Heizen positiv, Kühlen negativ, allein die AixLib-Reihe von Fall 6 beim Einlesen
  spiegeln).
- **F-P1 Fensterzweig nach E14:** Schritt C bleibt bei fünf Knoten; Wand- und Fensterzweig der
  Außenwandgruppe werden zu einem R_Rest,AW/R_1,AW zusammengefasst (Gl. (27)/(28) mit den
  Klemmfällen (28a)–(28c) der Richtlinie, ohne Zahlen); A7a bildet R_AF mit benanntem Abbruch bei
  R_AF ≤ 0; E7 gewichtet θ_eq über alle Außenflächen einschließlich Fenster (für die Fensterfläche
  ohne kurzwelligen Term); A_AW = A_AW,opak + A_w geht in R_conv,AW, A_rad, Strahlungsverteilung
  und θ_op ein (als Folgeentscheidung zu E14 benennen); die stationäre Probe 10.4 enthält den
  Fensterzweig und verlangt `Aussenbauteile_Strahlung = 0`. Der Wärmebrückenterm bleibt im
  masselosen Zweig; Kapitel 11 führt das als benannte Abweichung mit Messung der Wirkung in G0;
  ebenso A_rad = min(A_AW,opak, A_IW) und die flächenproportionale Verteilung des
  Fenstersolareintrags.
- **F-P2 Interne Lasten:** Aufteilung auf Luft und Oberflächen im **Eingangsbauer** (E3/E4);
  Ausgabe von Schritt E: `ThetaOut`, `ThetaEq`, `PhiRadAW`, `PhiRadIW`, `PhiConv`, `ThetaSoll`,
  `ThetaMax`/`ThetaKuehl`.
- **F-P3 Stundenschritt:** Der Abschnittsdeckel (60) ist ein benannter Fehler (Gebäude, Stunde,
  Zahl der Abschnitte, Fallfolge; kein Teilstundenergebnis) mit Rechenprobe. Freier Lauf wird
  zweiseitig geprüft (θ_soll ≤ θ_air ≤ θ_kuehl); Kühlfall geregelt auf die Leistung. Heiz- und
  Kühlanteil werden je Abschnitt getrennt akkumuliert (akkQ_heiz, akkQ_kuehl) und durch 3 600 s
  geteilt; damit wird „in keiner Stunde beide größer null" eine echte Probe. Schritt F kennt die
  fünf Betriebsfälle des Kühlkonzepts 3.2 (Heizen geregelt, Heizgrenze, Totband, Kühlen geregelt,
  Kühlgrenze) mit θ_kuehl (NULL = `Maximaleraumtemperatur`) und Φ_c,max = 1 000 ·
  `Kuehlleistung_Max`; Rechenschritte 7.2 stellt „informativ, kein vierter Kanal" auf E12/E21 um.
  Die Kühlleistung wirkt in KU1 **rein konvektiv** am Luftknoten; ein Strahlungsanteil der
  Kühlübergabe kommt erst mit AK1.
- **F-P4 Sommerlüftung:** Auswertung **einmal je Stunde am Stundenbeginn** mit θ_air und θ_out der
  Vorstunde, Zustand über die ganze Stunde gehalten, Hysterese 1 K, Mindestverweildauer 1 h; damit
  braucht das Verletzungsmaß keinen fünften Fall. Die Schwelle bleibt 23 °C bis KU1; ab KU1
  θ_kuehl − 3 K (Festlegung, Widerspruch möglich).
- **F-P5 Vorlauf:** Projektläufe 30 Tage fest; Normtests in G0 mit deterministischem
  Abbruchkriterium (Zustandsunterschied zweier Vorlaufwochen < 0,01 K, höchstens 12 Wochen,
  benannter Fehler). Erdreich E6: Überladung von `ErdreichTemperatur.JahresprofilKollektor` mit
  ausdrücklicher Temperaturleitfähigkeit als benannte Codeänderung in G1.
- **F-P6 Ferien und Wochenende:** Konzept 4.4, 4.8, 3.3 auf Rechenschritte E8: 0 und 366 sind
  Aus-Marker, Ferienfahrplan nur bei Ferien > 0,9 und θ_soll,Fer ≥ 1 wirksam, benannte Ablehnung
  nur für Tage außerhalb 1…365 in einem Fahrplan, der aktiv ist.
- **F-P7 Anlagenkopplung, Schritt H:** Rechenschritte bekommen ein Kapitel „Schritt H — Einschub
  der Anlagenkopplung (ab AK1)": vierter Betriebsfall „Übergabe begrenzt" mit Sekantenleitwert,
  zwei Vorarbeiten in Schritt E, Verletzungsmaß zweiseitig je Sättigungszustand (gesättigt: gültig
  solange θ_air ≤ θ_soll − Xp; Regelbereich: θ_soll − Xp ≤ θ_air ≤ θ_soll). Leitwert im
  Regelbereich G = Φ_ue,max/Xp + y·G_H, nur bei y = 1 gilt G = G_H. θ_R und θ_m werden nach der
  Begrenzung mit der gelieferten Leistung neu gebildet (konstanter Massenstrom: θ_R = θ_V − Φ/W_H).
  3.7 und Bild 6.1 verzweigen nach „Φ_verlangt ≤ Φ_ue,max **und** Xp = 0". Schritt H rechnet in W
  und W/K mit Einheitenzeile, Umrechnung einmal im Eingangsbauer.

### 3.4 Kühlung und Wärmepumpen mit Kühlfunktion

- **F-K1 Katalogfilter:** K20 ist durch die Umsetzung erledigt (Spalte „Kühlleistung [kW]"
  filterbar, Schalter „nur mit Kühlfunktion" über `AUSDRUCK_MIT_KUEHLUNG`); 5.0.1, 5.0.3, 8.2 und
  Zeilenbelege am Arbeitsbaum nachmessen; `Konzept_Katalogfilter` auf die Zahlenspalte umstellen.
- **F-K2 Vierter Kanal:** 4.2 bekommt „Fünf Stellen bauen den Stundenzustand selbst auf" und (c)
  Bivalenzpunkt (`RestSumme` über `KANAELE_WAERME`); `rest` wird über `KANAELE_WAERME` gefüllt;
  Zeilen in 4.3. Weitere Zeilen 4.3: `SimulationErgebnisHuelle.Anzeige.cs` (Kacheln; `KANALNAMEN`
  ist fest dreielementig — die Wärmeerzeugertabelle bleibt bei drei Kanälen, die Kälte bekommt eine
  eigene Tabelle), `SchemaModell.cs` (Badges und Kanten namentlich), `BerichtsDaten.KANAL_SCHLUESSEL`
  um `"KUEHLUNG"` erweitern samt Bereichsprüfungen.
- **F-K3 Proben:** Kältebeiträge gehen nicht in den Referenzakkumulator der Energieprobe, sondern
  in `probeKaelte`. Zwei Proben: **Bedarfsprobe Kälte** in `SimulationKaeltebedarf` (Muster
  `Energieprobe`: Verletzungen zählen, größte Abweichung, Meldung der Stufe Fehler einmal je Lauf,
  Lauf gilt als fehlgeschlagen — so ausdrücklich festlegen, der Halbsatz „dieselbe Schärfe wie die
  Energieprobe" entfällt) und **Deckungsprobe Kälte** in
  `SimulationControl.KanalganglinienProbe()`. Die Zusicherung „nie beide größer null" gilt je
  Gebäude bzw. Zone aus `GebaeudeModellErgebnis`, nicht auf Kanalebene.
- **F-K4 Reversible Wärmepumpe:** Die Tagesbetriebsart gilt für den **Heizkanal**; der
  Brauchwasserkanal bleibt am Kühltag bedienbar (Stundenleistung zuerst Brauchwasser, Rest Kälte).
  Kältedeckung läuft in einer eigenen Stundenschleife `Kaeltekaskade` **nach** der Wärmekaskade,
  Reihenfolge = Kaskadenplätze der Wärmeseite, gefiltert auf kühlfähige Erzeuger. Kälteerzeugung
  und Kältestrom in **eigenen** Reihen (`Kaelteproduktion_stuendlich`,
  `Stromverbrauch_Kuehlung_stuendlich`), `WP_Waermeproduktion_stuendlich` und
  `WP_Strombedarf_stuendlich` unberührt; die VDI-4640-Prüfung bleibt „nur Heizen", K8c vertagt.
  Der Kältestrom läuft an der benannten Stelle in `SimulationControl` in die Stufenrechnung.
  Anlagen mit Quellspeicher (`WQ_Typ = Pufferspeicher`) werden in KU2 für Kühlung benannt
  abgelehnt. KU2 rechnet mit der Kennlinie der höchsten Laststufe (`MAX(Last)`), linear bei
  Teilauslastung, EER konstant; der Zugriff `KenndatenKuehlungCtrl` wird um `Last` ergänzt,
  projektseitiger Leser nach dem Muster `SimulationWaermepumpe.ModuleAufbauen`; zwei Prüfungen
  `HatKenndatenStamm`/`HatKenndatenProjekt`; `Kuehl_Vorlauf` als INTEGER; `KU-S3` mit drei Spalten
  (`Kuehlbetrieb`, `Kuehl_Vorlauf`, `Kuehl_Hilfsstromanteil`), `Kuehl_Umschaltung` entfällt;
  `Kaeltebedarf_Gesamt` bleibt mit dem Satz, dass sie heute wertgleich mit `Waermebedarf_Kuehlung`
  ist.
- **F-K5 Ergebnisdialog:** Kälte als dritter Block unter „Wärme | Strom", nur sichtbar bei
  Kältebedarf > 0; `Konzept_Simulationsablauf` in die Schwesterpapiertabelle.
- **F-K6 Kälteseite der Anlagenkopplung:** Auslegungspunkt der Kühlübergabe kommt aus der Anlage
  (`Tab_WP.Kuehl_Vorlauf`, feste Spreizung 5 K) — als vierte benannte Abweichung in
  Anlagenkopplung 7.4, kein gebäudeseitiges Spaltenpaar.

### 3.5 Anlagenkopplung, Mehrzonen, Datenaustausch

- **F-A1 Softwarearchitektur Rev. 4** kennt die Anlagenkopplung: `Waermeuebergabe` als Baustein in
  `Gebaeude/` (AK1), `Anlagenfahrplan` samt Naht `Anlagenverfuegbarkeit` neben den Fassaden (AK2),
  `Stundenrand` um Vorlauf, Übergabekennwerte und Reglerband erweitert, Stufen AK0–AK3 und KU0–KU3
  in Kapitel 5, Einfrieranlässe in 2.8.
- **F-A2 AK2-Verteilung als Zweipass:** Pass 1 unbegrenzter Bedarf je Gebäude (Verfügbarkeit
  unendlich) → Schlüssel je Stunde; Pass 2 mit verteilter Verfügbarkeit. Randfälle nach
  `Kanalsatz.NetzverlusteVerteilen`: Summe ≤ 0 → volle Schranke je Gebäude; Rundungsrest nicht dem
  letzten Gebäude. Zweite Verteilungsstufe innerhalb des Gebäudes auf die Zonen nach demselben
  Schlüssel. Pfadabhängigkeit bei Altweg-Gebäuden benennen.
- **F-A3 Iterationsschranke AK3:** Produkt Zonendurchläufe × Anlagendurchläufe je Stunde ≤ 120;
  darüber gilt die Stunde als nicht konvergiert (benannte Meldung, letzter Stand); Grenzfall
  50 × 50 × 20 in 6.3 ausrechnen.
- **F-A4 Zwei Aufzählungen:** `Verfuegbarkeitsgrund` (Anlagenseite) und `Begrenzungsgrund`
  (Gebäudeseite: KEINE_BEGRENZUNG, UEBERGABE, HEIZLEISTUNG_MAX, VERFUEGBARKEIT, UMSCHALTUNG) im
  ganzen Papier gleich. `Tab_Einstellungen.Anlagenkopplung TEXT(4)`, `CHECK IN
  ('AUS','AK1','AK2','AK3')`, NULL = AUS; AK-S1 = 13 Gebäudespalten (26) + 1 Projektspalte = 27
  Einträge. In `Tab_Zone` allein `Uebergabe_Art`, `Uebergabe_Exponent`, `Uebergabe_Leistung_Nenn`.
  Erzeugerseite von AK1: die Kennlinienwahl der Wärmepumpe nimmt je Stunde den gerechneten Vorlauf
  des versorgten Gebäudes (mehrere Gebäude: bedarfsgewichtetes Mittel) — 6.1 „vollständig in
  `Gebaeude/` bis auf die Kennlinienwahl der Erzeugerseite", +1–2 PT. 12.3 Zeile 5: „Vorbedingung
  von AK1 (Wärmeseite: G1 + G2); die Kälteseite setzt KU1 und KU2 voraus".
- **F-M1 Schemaschritte der Zonen:** `Tab_Zone` entsteht mit **G3** (S-A bis S-C: Baustoffe,
  Bauteilaufbau-Schichten, `Tab_Zone` + `Tab_Bauteil`); `Tab_Zonenluftstrom` und `ID_Nachbarzone`
  in **S-G mit G6b**; G6a legt `Tab_Bauteilaufbau(_STAMM)` an. Die Zonenspaltentabelle in
  Mehrzonen 4.2 bekommt einen Block „Spalten aus KU-S1 und AK-S1, sofern diese Stufen stehen"
  (`Kuehl_Sollwert`, `Kuehlleistung_Max`, `Kuehlung_Aktiv`, `Kuehl_Sollwert_Nacht`;
  `Uebergabe_Art`, `Uebergabe_Exponent`, `Uebergabe_Leistung_Nenn`), und S-C legt sie an, wenn die
  Stufen stehen. Einfrierregeln werden **benannt**, nicht durchgezählt.
- **F-M2 Mehrzonen:** Bei N = 1 entfällt der adiabate Vorlauf und die Konvergenzprobe; Probe 10
  bleibt bitgleich. Schreibweg = Abgleich über die Ids in einer Transaktion (Entfernen → Ändern →
  Anlegen, Muster A6), nicht Löschen und Neuanlegen. ADR-005 Rechenzeit „rund 1,1 bis 2,0 s je
  Gebäude und Jahr" mit Verweis.
- **F-D1 IFC:** Konzept 7.4: Klammer mit dem Metapaket streichen, Tabellenzeile mit dem
  Paketzuschnitt aus ADR-003; ADR-003: fünfte Kraft und Aufgabe „Paketgröße der iOS-App messen
  (mit und ohne Schema-Assemblies, ios-arm64, getrimmt)". Basis-Name im Zustandsbild ohne Nummer:
  „aktuelle Basis nach `Referenzlaeufe/LIESMICH.md`".


## 4 Was nachgezogen wurde

- **[Leitkonzept](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) (Rev. 3):** Nachtrag N1.31
  mit E26, die Stufe GA zurück in Kapitel 11, Schemastand und Papiernamen M3/M4 statt fester
  Schrittnummern, Bezugsfläche `Nutzflaeche`, die vier Rev.-1-Kapitel (Keller, langwelliger Term,
  Validierungsband, Vorzeichen) und die Wortwahl „bis zur Ablösung" an allen Stellen.
- **[Umsetzungskonzept](../Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) (Rev. 4):**
  Stufe GA mit Löschliste und Ausbauprobe in Kapitel 6, Verträge des Vorbereitungsschritts und der
  Weiche nach F-Ü1/F-Ü2/F-Ü3/F-Ü6, Schrittkörper M3 mit Umbenennung, M4 ohne die leserlose Spalte,
  Ergebnisreihen und Kennzahlen nach F-S4, Aufwände als verbindliche Quelle.
- **[Rechenschritte](../Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) (Rev. 2):**
  Fensterzweig nach E14 in den Schritten A, B, C und E, Stundenschritt mit Abschnittsdeckel,
  zweiseitiger Prüfung und getrennten Akkumulatoren, die fünf Betriebsfälle mit Leistungsgrenze,
  Sommerlüftung mit Auswertezeitpunkt, Vorlauf mit Abbruchkriterium und das neue Kapitel
  „Schritt H".
- **[Softwarearchitektur](../Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) (Rev. 4):**
  Anlagenkopplung als Baustein, Naht und Stufen, Modultrennungswache mit einem Namen und dem
  erweiterten Prüfumfang, Ausbauprobe, Einfrierkette mit den Anlässen GA und Altweg-Fehlerbehebung,
  Zustandsbild auf die aktuelle Basis.
- **[Systementwurf](../Systementwurf_Gebaeudesimulation_EPOS-Plan.md) (Rev. 4):** Sequenzbild mit
  der Rückrechnung hinter der Weiche, Vertrag V16 für `IGebaeudeRechenweg`, Fehlerweg und
  Abbruchwirkung in Kapitel 6, Rückweg-Test und Einfrieranlässe in 8.3/8.4.
- **[Kühlkonzept](../Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) (Rev. 4):** K20 als
  erledigt, sechs zusätzliche Stellen des vierten Kanals, zwei getrennte Proben, Kältekaskade mit
  eigenen Reihen, Kennlinienzugriff mit Laststufe, Ergebnisdialog mit drittem Block.
- **[Anlagenkopplung](../Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) (Rev. 2):**
  Zweipass-Verteilung samt Randfällen und Zonenstufe, Iterationsschranke, zwei Aufzählungen,
  Projektschalter mit Spalte und Wertebereich, Kennlinienwahl der Erzeugerseite, Kälteseite nach
  F-K6.
- **[Mehrzonenmodell](../Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) (Rev. 3):** Schemaschritte der
  Zonen nach F-M1, Zonenspalten aus Kühlung und Anlagenkopplung, Einzonenfall ohne Zusatzvorlauf,
  Schreibweg als Abgleich, Summenzeile mit dem Vorbehalt zu X1…X3.
- **[Datenaustausch](../Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) (Rev. 2):** Schemastand und
  Papiernamen, Ablageort der gbXML-Schemakopie als neuer offener Punkt D17.
- **[ADR-002](../ADR-002_Stundenmodell_VDI6007_Einbindung.md):** Ergänzungsvermerk eingeschränkt,
  Entscheidung 3 Satz 3 durch den Hinweis auf E20/ADR-006 ersetzt.
  **[ADR-003](../ADR-003_IFC_xBIM_ohne_Geometriekernel.md):** fünfte Kraft und die Aufgabe, die
  Paketgröße der iOS-App zu messen. **[ADR-005](../ADR-005_Zonenkopplung_Mehrzonenmodell.md):**
  Rechenzeit mit Verweis auf das Mehrzonenkonzept.
  **[ADR-006](../ADR-006_Trennung_Altweg_VDI6007.md):** Regel, dass jede Stufe ihren
  Altweg-Sonderfall im selben Auftrag in die Löschliste einträgt.
- **[Befund X](2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md):** Vorspann von Kapitel 4 neu
  („Grundlage der Stufe GA; ihr Zeitpunkt ist offen (Q24)"), die beiden Schreibstellen der
  Altweg-Flags in der Liste 4.3.
- **[Katalogfilter](../Konzept_Katalogfilter_EPOS-Plan.md):** die drei Belegstellen auf die
  Zahlenspalte „Kühlleistung [kW]" und den Schalter „nur mit Kühlfunktion" umgestellt.
- **[Register](../Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md):** Q24 und Q25 wieder offen,
  U17 neu, D17 und K24 als eigene Punkte, U6 und U7 in Kapitel 0, A15 mit Empfehlung, Q26-Aufwand,
  Kapitel 8 mit den Festlegungen dieser Prüfung, Zählung neu auf 67.
- **[Statusdatei](../Status_Gebaeudesimulation_VDI6007.md):** Zeile zu E26, Q24/Q25 wieder offen,
  Stufentabelle mit GA und allen Stufen der Schwesterpapiere, Registerzeile im Wortlaut des
  Registers, Revisionszeilen aller neun Papiere; die Zeilenzahlen der Papiere entfallen.
- **Index (`Dokumentation/LIESMICH.md`):** Zeilen zu Konzept, Statusdatei, Register, ADR-006,
  Anlagenkopplung, Kühlkonzept, Mehrzonen und Softwarearchitektur auf den Stand vom 17.09.2026;
  neue Indexzeile für dieses Prüfprotokoll.
- **[Word-Kurzfassung](Gebaeudesimulation_VDI6007_Architektur_Design_Rechenweg_2026-09-16.docx):**
  auf den Stand nach E26 gebracht (Stufe GA, Ausbauprobe, Verträge, Kühlung).


## 5 Offene Punkte nach der Prüfung

### 5.1 Neu oder wieder offen

| Punkt | Frage | Fällig |
|---|---|---|
| **Q24** | Wann ist der VDI-Weg bewährt genug, dass die Stufe GA beauftragt wird? Empfehlung: alle Referenz- und Bestandsprojekte einmal auf VDI 6007 gerechnet und je Projekt erklärt; eine Feldphase über mindestens eine Heizperiode ohne offenen Fehler; KU1 und, falls beauftragt, AK1 abgenommen; Ausbauprobe grün. | vor GA |
| **Q25** | Umfang der Stufe GA (Empfehlung unverändert zu N1.25 Punkt 4, siehe Löschliste im Umsetzungskonzept 6). | vor GA |
| **U17** | Ein Altweg-Gebäude ohne Tagesverteilung: Lauf abbrechen wie heute oder Gebäude benannt ablehnen? Empfehlung: benannt ablehnen, aber erst mit GA, weil es das Altweg-Verhalten ändert. | mit GA |
| **D17** | Ablageort der gbXML-Schemakopie (Rest aus Register Kapitel 8, D3). | vor G4 |
| **K24** | Eigene Ergebnisspalte für den Kältestrom (Rest aus Register Kapitel 8, K18a). | vor KU2 |

### 5.2 Widerspruchsfrist der Festlegungen

Die 31 Festlegungen F-Ü1 bis F-D1 aus Kapitel 3 sind getroffen, damit die Papiere in sich stimmen.
**Widerspruch ist bis zur Beauftragung von G1 möglich**; verbindlich ist die Fassung im
[Register](../Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md), Kapitel 8. Besonders zu prüfen
sind die Festlegungen mit fachlicher Wirkung: F-P4 (Schwelle der Sommerlüftung ab KU1), F-K4
(Reihenfolge Brauchwasser vor Kälte, Kennlinie der höchsten Laststufe), F-A3 (Iterationsschranke)
und F-Ü7 (Umfang des Rückweg-Tests).

### 5.3 Aufgaben für Stufe G0

- Rechenzeit neu messen — mit Kappung an θ_kuehl, gesetzten Leistungsgrenzen, getrennt ohne und mit
  Kühlung (F-S6, aus VDI-12 und PHY-21).
- Abnahmefenster des Kriteriums (4) neu bestimmen — Messung aus Konzept 5.5 mit dem heute geltenden
  Rechenweg wiederholen (F-S6, aus VDI-05).
- Abbruchkriterium des Vorlaufs für die Normtests festlegen und messen (F-P5, aus PHY-16).
- Wirkung der flächenproportionalen Verteilung des Fenstersolareintrags messen (A_v = 0 in G1) und
  die Wirkung des masselos hängenden Wärmebrückenterms beziffern (F-P1, aus PHY-20 und PHY-E2).
- Paketgröße der iOS-App messen — mit und ohne Schema-Assemblies, ios-arm64, getrimmt (F-D1, aus
  AKM-19).

### 5.4 Vor G1 fällig

Register Kapitel 0 führt nach der Ergänzung um U6 und U7 **35 Punkte**, die vor der Beauftragung
von G1 zu entscheiden sind, weil sie Schema, Referenzbasis, Datenmodell oder eine Fremdbibliothek
unwiderruflich festlegen. Sie werden hier nicht wiederholt; maßgeblich ist
[das Register](../Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md), Kapitel 0.

### 5.5 Weiteres

- **Q26 — Stufenplan der Anlagenkopplung:** offen; der Aufwand steht jetzt im Register nach
  Anlagenkopplung 12.2 (AK0 1–2, AK1 9–13, AK2 11–15, AK3 23–38 PT). Die Entscheidung, ob und wann
  AK0 bis AK3 beauftragt werden, steht beim Anwender.
- **H6 — vertagt.** Der Punkt bleibt zurückgestellt; er wird erst mit der Beauftragung von AK1
  wieder aufgerufen.

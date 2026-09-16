# Offene Entscheide der Gebäudesimulation — Register mit Erläuterung

**Stand 16.09.2026, nach den Entscheiden E16–E25.**

**Zweck.** Dieses Register ist die **eine Stelle, an der jede offene Frage der Gebäudesimulation
mit ihrer Erläuterung steht** — Frage, Hintergrund, Optionen, Empfehlung des jeweiligen Papiers,
Folge bei Nichtentscheid und der Zeitpunkt, bis zu dem sie beantwortet sein muss. Anlass ist die
Bitte des Anwenders vom 16.09.2026: „erläutere alle offenen Punkte".

**Was dieses Register nicht ist.** Es entscheidet nichts und hält nichts fest. **Entschieden wird
weiterhin im Konzept-Nachtrag und in der Statusdatei**: der ausführliche Entscheid als Nachtrag
N1.x in [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md),
die Zeile je Entscheid in [`Status_Gebaeudesimulation_VDI6007.md`](Status_Gebaeudesimulation_VDI6007.md),
der Architekturentscheid im zugehörigen ADR. Dieses Register **zeigt nur auf sie** und wird beim
Entscheid um den betroffenen Punkt gekürzt.

**Lesehinweis.**

- **Kapitel 0** nennt die Punkte, die **vor dem Start von G0/G1 oder vor der Beauftragung einer
  Stufe** fällig sind — alles, was Schema, Referenzbasis, Datenmodell oder eine Fremdbibliothek
  unwiderruflich festlegt. Wer wenig Zeit hat, liest nur dieses Kapitel.
- **Kapitel 1 bis 6** führen je Papier alle offenen Punkte einzeln aus (Kapitel 1 trägt die neue
  Frage Q26), immer im selben Aufbau; **Kapitel 7** hält den Stand der zwölf Fragen H1 bis H12 der
  Anlagenkopplung fest, die mit E24 entschieden sind:
  Frage, Hintergrund, Optionen, Empfehlung des Papiers, Folge bei Nichtentscheid, Fällig vor.
- **Kapitel 8** nennt die technischen Festlegungen,
  denen nur zu widersprechen ist, **Kapitel 9** den Weg, auf dem ein Entscheid festgehalten wird.
- Zahlen und Empfehlungen stehen im Wortlaut der Papiere. Wo zwei Papiere zu derselben Frage
  Verschiedenes sagen, sind **beide** genannt.
- Die Nummern sind die der Papiere und werden nicht umnummeriert: **Q** Konzept, **U**
  Umsetzungskonzept, **M** Mehrzonenmodell, **D** Datenaustausch, **A** Softwarearchitektur, **H** Anlagenkopplung,
  **K** Kühlung.

**Umfang in Zahlen.** 62 offene Punkte (nach den Entscheiden E16–E25 vom 16.09.2026, die Q10, Q11a, Q24, Q25, H1–H12,
U2, U11, U16, M1, M4, A7, A8, A16, A19 und K1 aus diesem Register genommen und Q26 hinzugefügt haben): 1 im
Konzept (Q26), 13 im Umsetzungskonzept (U1, U3–U10, U12–U15), 12 im Mehrzonenkonzept (M2, M3,
M5–M14), 7 im Datenaustauschkonzept (D1, D2, D4, D5, D6, D11, D16), 15 in der Softwarearchitektur
(A1–A6, A9–A15, A17, A18, davon drei reine Verweise auf U-Nummern) und 14 im Kühlkonzept (K4–K12,
K19 sowie K20–K23); die zwölf Fragen H1–H12 der Anlagenkopplung sind mit E24 entschieden.
ADR-004 und ADR-005 sind angenommen; dazu drei Listen zur Kenntnis.

---

## 0. Was jetzt zu entscheiden ist

Kriterium dieser Liste: Der Punkt legt **Schema, Referenzbasis, Datenmodell oder eine
Fremdbibliothek unwiderruflich** fest, oder er ist für den Anwender in der Oberfläche sichtbar und
lässt sich nachträglich nicht stillschweigend ändern. Alles Übrige steht in den Kapiteln 1 bis 7
und darf mit der Stufe entschieden werden, zu der es gehört.

| Nr. | Frage in einem Satz | Empfehlung in einem Satz | Spätestens vor |
|---|---|---|---|
| **U8** | Werden die Normzahlen als gitignorierte, lokal beizustellende Datei geführt — mit der Folge, dass der Normfallnachweis lokal und nicht in der CI läuft? | Ja; das Ausliefern wäre eine Vervielfältigung, und die Lücke im Gate gehört ins Protokoll. | **G0** |
| **U5** | Werden die zwei Gebäudespalten-Schemaschritte zu **einem** verschmolzen (ein Sichtneubau statt zwei)? | Ja — G1 und G2 werden gemeinsam ausgeliefert; der Klimaschritt bleibt getrennt. | **G1** |
| **U1** | Bekommt der Katalogeditor **einen** Schreibweg statt der heutigen mehreren — eine für den Anwender sichtbare Änderung? | Ja, mit G1; „Speichern unter…" bleibt als nicht schließender Zweitknopf. | **G1** |
| **U3** | Bekommt das `Zahlenfeld` einen `Platzhalter` — ein Eingriff in einen Standardbaustein, den jeder Dialog benutzt? | Ja, rein additiv; zieht die Stilblatt-Tests nach sich. | **G1** |
| **A15** | Was geschieht mit der letzten reinen Bestandsbasis, gegen die der Rückweg-Nachweis läuft? | Keine zweite Basis, sondern ein Referenzprojekt, das dauerhaft auf dem Altweg (Tagesbilanz) steht und in der neuen Basis mit eingefroren wird (E20, E23). | **G1 + G2** |
| **A18** | Bleibt der Klimaweg des Gebäudemodells eine **eigene Klasse** oder fällt er in den Eingangsbauer zurück? | Eigene Klasse behalten, aber ausschließlich vom Eingangsbauer gerufen. | **G1** |
| **A10** | Zieht der Gebäudedialog schon mit G1 nach `EPOS.UI.Daten` oder erst mit G6? | Mit G1 — die Hülle wird ohnehin neu geschnitten, und der Importweg setzt einen plattformfreien Schreibweg voraus. | **G1** |
| **A12** | Wo erscheint der Produktausweis — Wiki-Seite **und** Berichtskopf? | Beides, im Wortlaut von E10, unverändert und ohne Umschreibung. | **G1** (Berichtskopf), G2 (Wiki) |
| **K2** | Vorzeichen: Norm innen, Betrag außen — führt der Kanal positive Kältemengen? | Ja; ein Kanal mit negativen Werten bräche jede Summen- und Deckungsrechnung, und zwar still. | **KU1** |
| **K10** | Bleibt der Kühlbetrieb bis zu einer ausdrücklichen Projekteinstellung aus? | Ja, Vorgabe 0 — nicht um das Einfrieren zu vermeiden, sondern um die zwölf übrigen Referenzprojekte zu schützen. | **KU1** |
| **K11** | Eigener Kühlsollwert und eigene Kühlleistungsgrenze — oder bleibt die vorhandene Maximaltemperatur die einzige Kühleingabe? | Eigener Sollwert und eigene Grenze in KU1, Zeitprofil erst in KU3. | **KU1** |
| **K19** | Bekommt KU2 einen eigenen, kleinen Einfrierschritt? | Ja — sonst bleibt die Kältedeckung im Regressionsnetz unsichtbar. | **KU2** |
| **K20** | Wird die Kennzeichenspalte „kühlt" filterbar — als benannte Ausnahme von einer Hausregel oder über eine eigene Filterart am gemeinsamen Katalogfilter? | Eigene Filterart: rund ein Personentag mehr, dafür bleibt die Regel eine Regel. | **KU2** |
| **K22** | Führt die COP-Spalte der Kühlkennlinie wirklich das Kälteverhältnis — oder in manchen Datensätzen das Wärmeverhältnis? | Vor KU2 an den vorhandenen Kühlkennlinien prüfen und das Ergebnis im Glossar festhalten; nie stillschweigend als EER lesen. | **KU2** |
| **A11** | Werden die Schemaschrittnummern jetzt verbindlich vergeben oder erst bei Beauftragung? | Erst bei Beauftragung; verbindlich sind jetzt Reihenfolge und Inhalt. | **erste Auslieferung** eines Schemaschritts |
| **A1** | Bleibt die Kaskade der Gebäudekinder, obwohl der Gebäude-Schreibweg möglicherweise löscht und neu anlegt? | Kaskade behalten, den Schreibweg vor G3 messen, die Rettung dort einbauen, wo das Löschen steht. | **G3** |
| **A14** | Was trägt den Umschalter Klassenweg → Bauteilweg — die Datenlage oder ein eigener Wert? | Datenlage behalten, aber den Übergang benennen: Rückfrage vor der ersten Zone, Herleitungszeile in beiden Stellungen. | **G3** |
| **A2** | Bleibt das IFC-Paket am Kern, oder zieht der Leser hinter eine Naht in ein eigenes Projekt? | Am Kern bleiben, aber die Naht `IGebaeudeLeser` von Anfang an ziehen. | **G4** |
| **A3** | Welchen **Namen und Ordner** bekommt der Zuordnungsdialog — zwei Papiere nennen verschiedene? | Der formatfreie Name; ein Format im Namen einer Maske, die zwei Formate trägt, ist eine Unwahrheit. | **G4** |
| **A13** | Bekommt die Gebäudetabelle Herkunfts- und Quellkennungsspalten wie Zone, Bauteil, Aufbau und Baustoff? | Nein — Gebäudeherkunft nur in der Importzuordnung; sonst 17 statt 15 neue Spalten und ein zweiter Sichtneubau. | **G4** (Herkunftsschritt) |
| **A17** | Bekommen Gebäudeimport und ‑export einen Maskenschlüssel und eine Menüzeile? | Nein — Überlagerung im Gebäudedialog; hier widersprechen sich zwei geltende Papiere. | **G4** |
| **U10** | Kommt eine Lizenzhinweisseite ins Installationspaket — und dann gleich für **alle** ausgelieferten Fremdanteile? | Ja, mit der ersten IFC-Stufe und für alle; ohne sie ist der IFC-Import nicht auslieferbar. | **G4** |
| **U12** | Woher kommen die Vorgaben je Baualtersklasse? | Eigene Werte aus dem EPOS-Gebäudekatalog ableiten — lizenzfrei und hausgemacht. | **G4** |
| **D1** | Kommt der gbXML-Import **vor** dem IFC-Import? | Vorschlag ja, Entscheid beim Anwender — es hängt allein daran, welche Dateien im Feld ankommen. | **Beauftragung G4c/G4a** |
| **D16** | Erweitert die gbXML-Zonenbildung den Entscheid E7 auf ein zweites Format? | Vorschlag ja; sagt der Anwender nein, bleibt gbXML dauerhaft einzonig. | **Beauftragung G4c** |
| **M9** | Steht die Synonymtabelle in der Auslieferung oder je Projekt? | Auslieferung — die Namen der Autorensysteme wiederholen sich projektübergreifend. | **G6a** (Schema) |
| **M14** | Wird die Projektkopie der Baustoffe gebraucht, oder genügt der Auslieferungskatalog mit der Wertekopie an der Schicht? | Beides behalten — die Wertekopie schützt gerechnete Ergebnisse, die Projektkopie erlaubt eigene Stoffe. | **G6a** (Schema) |
| **A6** | Werden Zonen, Bauteile und Luftströme als **ein** Aggregat geschrieben, und schreibt es durch Abgleich über die Ids statt durch Löschen? | Ein Aggregat, Ändern statt Löschen, alles in einer Transaktion — sonst zerstört jedes gewöhnliche Speichern die Importherkunft. | **G6b** |
| **M2** | Raumseitenmaß oder Bruttomaß beim Import? | Raumseitenmaß durchhalten und im Dialog benennen; das weicht von der Bemaßung des Einzonenmodells ab und ist ein Entscheid. | **G6c** |
| **M10** | Kommt die große Testdatei ins Repositorium — und ist die Lizenz der Fassung mit Raumgrenzen geklärt? | Ja, **aber nur zusammen mit der LFS-Zeile im selben Schritt**; die Lizenz ist nachzufragen. | **G6c** (vor dem ersten Commit der Datei) |
| **D4** | Ergebnisgrößen im Export in kWh mit ausdrücklicher Einheit — oder in Joule mit Hinweis? | kWh mit ausdrücklicher Einheit; **unwiderruflich**, eine spätere Umstellung entwertet alte Exporte. | **vor der ersten Zeile Quelltext (G7c)** |
| **D5** | Deterministische Kennungen in beiden Exportformaten? | Ja, von Anfang an, aus dem Schlüsselpfad der IDs; nachträglich nicht mehr einzuführen. | **vor der ersten Zeile Quelltext (G7a/G7c)** |
| **D2** | Lohnt der gbXML-Export nur mit der zweiten Stufe (synthetische Geometrie)? | Ja — ohne sie ist der Export ein Datenblatt in XML-Form, keine Interoperabilität. | **Beauftragung G7** |
| **D6** | Wer ist das Gegenüber des IFC-Exports — welches Werkzeug, welcher Anwender, welcher Zweck? | Frage an den Anwender; Zwischenweg: die semantische Stufe bauen, die Körperstufe zurückstellen. | **G7e** |
| **D11** | Ist die Rückgabe angereicherter fremder IFC-Dateien vertraglich zulässig? | Vor der Round-Trip-Stufe zu klären, nicht danach; mindestens Beipackzettel, eigene Anwendungskennung, neuer Dateiname, bestätigter Hinweis im Dialog. | **G7d** |

**Was hier nicht steht, ist nicht unwichtig** — es ist nur an seine Stufe gebunden und kann mit
ihr entschieden werden. Die vollständige Erläuterung jedes Punktes steht in den folgenden
Kapiteln.

---

## 1. Konzept Gebäudesimulation VDI 6007 — Q26

Quelle: [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md),
Kapitel 13 und Nachtrag N1.25. Alle 23 ursprünglichen Fragen des Konzepts sind entschieden (E1 bis E19;
zuletzt **Q10** mit E18 und **Q11a** mit E19 am 16.09.2026). Mit **E20** — Trennung der Rechenwege,
der Altweg als getrennter Bestandsweg — waren zwei Fragen entstanden (Q24, Q25 aus Befund X), die
**E23** („der Altweg bleibt", N1.28) erledigt hat; mit **E22** (Anlagenkopplung, N1.27) kam Q26 hinzu. Die Fragen, die aus E15 entstanden
sind, führt das Kühlkonzept als K20 bis K23 (Kapitel 6).

### Q24 — wann endet der Übergang?

**Mit E23 entschieden (16.09.2026): nie.** Der Altweg bleibt dauerhaft als eingefrorener Bestandsweg,
die Stufe GA entfällt; Referenzprojekt und Rückweg-Test bleiben. Konzept N1.28, Statusdatei
Abschnitt 1; hier gekürzt.

### Q25 — Umfang der Stufe GA

**Mit E23 gegenstandslos (16.09.2026).** Es gibt keinen GA-Schemaschritt; `Typ`, Tagesverteilung und
Altweg-Spalten bleiben. Die leserlosen Spalten `WW_Bedarf` und `Waermebedarf` sind ein Aufräumpunkt
ohne Stufe (Kapitel 8). Konzept N1.28; hier gekürzt.

### Q26 — Stufenplan der Anlagenkopplung

- **Frage:** Welche Stufen der Anlagenkopplung werden beauftragt und wann — **AK1** (Heizkreis als
  Randbedingung: Heizkurve, Übergabe, Rücklauf; Einbahnstraße Anlage → Gebäude) nach G2, **AK2**
  (Erzeugerfahrplan als Verfügbarkeit je Stunde, Komfortstunden) nach abgenommenem AK1 und einer
  Feldphase, **AK3** (geschlossener Kreis mit Iteration je Stunde) danach?
- **Hintergrund:** E22 macht die bisher ausgeschlossene Kopplung von Vorlauftemperatur und
  Erzeugerfahrplan an die Raumtemperatur zur benannten EPOS-Erweiterung mit eigenem Papier
  ([Anlagenkopplung](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md)). Das Raummodell bleibt das der Richtlinie, die Normtestbeispiele rechnen
  weiter mit idealer Regelung; jede Stufe ist je Gebäude oder Projekt wählbar mit Vorgabe aus und
  eigenem Einfrierschritt. AK2 und AK3 ändern den Grundsatz „erst Bedarf, dann Deckung" für die
  Gebäude des VDI-Wegs; Gebäude auf dem Altweg, der nach E23 dauerhaft bleibt, gehen als feste Last
  ein.
- **Optionen:**
  - **(a) AK1 nach G2; AK2 nach abgenommenem AK1 und einer Feldphase, AK3 danach** — der Nutzen (Aufheizspitzen,
    Vorlauf für die Wärmepumpen-Kennlinien) kommt früh, das Risiko für die Deckungsrechnung spät.
  - **(b) Nur AK1** — Heizkreis als Randbedingung, kein Fahrplan; Unterdeckung bleibt eine Zahl.
  - **(c) Alles in einem Auftrag nach G3** — ein Einfrierschritt; der frühe Nutzen entfällt.
  - **(d) Gar nicht** — der Ausschluss in Konzept 15 bliebe in voller Breite.
- **Empfehlung des Papiers:** **(a)**; Aufwand grob AK1 8–12 PT, AK2 10–15 PT, AK3 20–35 PT
  zuzüglich Neu-Einfrieren — das Papier beziffert nach.
- **Folge bei Nichtentscheid:** Das Papier bleibt Konzept ohne Stufe; nichts blockiert G0 bis G7.
- **Fällig vor:** **Beauftragung von G2** (für AK1); AK2 und AK3 mit der Abnahme von AK1.
- **Quelle:** [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.27 und 13 (Q26);
  [Anlagenkopplung](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) 12 und 13.

---

## 2. Umsetzungskonzept — U1 bis U16

Quelle: [`Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md),
Kapitel 5 („Fragen mit Empfehlung"; das Papier steht in Rev. 2). Dreizehn der sechzehn Fragen sind
offen; **U2** ist durch E20 überholt, **U11** und **U16** sind mit E18 (16.09.2026) beantwortet — alle
drei hier gekürzt.
Vier von ihnen führt die Softwarearchitektur unter eigener Nummer als Sperrpunkt: **U1 = A4**,
**U2 = A19**, **U3 = A5**, **U5 = A9** — entschieden werden sie unter der U-Nummer, damit nicht
zwei Register zwei Antworten bekommen.

### U1 — ein Schreibweg im Katalogeditor

- **Frage:** Bekommt der Gebäude-Katalogeditor **einen** Schreibweg (OK/Abbrechen) statt der
  heutigen mehreren Aus- und Schreibwege? Das ist eine für den Anwender **sichtbare** Änderung.
- **Hintergrund:** Der Editor trägt heute mehrere nebeneinanderstehende Schreib- und
  Ausstiegswege. Das Konzept verlangt für den VDI-6007-Weg zehn harte Prüfregeln (Q18,
  entschieden). Hängen diese Regeln an mehreren Schreibstellen, ist jede einzeln zu bedienen und
  zu prüfen; zusätzlich macht der Zwischenstand des zweiten Reiters die U·A-Summe zeitweise
  falsch, die der Dialog nach E2 offen ausweisen soll.
- **Optionen:**
  - **(a) Ein Schreibweg (OK/Abbrechen)** — die Prüfregeln stehen an einer Stelle; der Anwender
    muss sich an eine geänderte Bedienung gewöhnen, und die Änderung gehört ins Wiki-Logbuch.
  - **(b) Alles lassen** — keine sichtbare Änderung; zehn Prüfregeln an mehreren Schreibstellen,
    dauerhaft mehrfach zu pflegen und zu testen.
- **Empfehlung des Papiers:** **Ja**, ein Schreibweg. „Speichern unter…" bleibt als **nicht
  schließender Zweitknopf** erhalten, wie es die Hausregel der Oberfläche vorsieht.
- **Folge bei Nichtentscheid:** G1 kann nicht sauber abgenommen werden — die Softwarearchitektur
  führt U1 als A4 ausdrücklich als **Sperrpunkt für G1** („sonst hängen zehn Prüfregeln an drei
  Schreibstellen").
- **Fällig vor:** **G1**.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U1); [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A4) und 3.3.

### U2 — sieben Modellparameterfelder verstecken oder gesperrt zeigen

**Durch E20 überholt (16.09.2026).** Der Gebäudedialog ist in VDI-6007-Struktur aufgebaut, die
Modellparameter sind immer sichtbar und bearbeitbar; Felder, die nur der Altweg liest, stehen in einem
eingeklappten Abschnitt „Tagesbilanz (Bestandsweg)", der nur bei einem Gebäude auf dem Altweg
erscheint (dauerhaft, E23). Festlegung in Kapitel 8; Konzept N1.25, N1.28, ADR-006.

### U3 — `Platzhalter` am Standardbaustein `Zahlenfeld`

- **Frage:** Wird der Standardbaustein `Zahlenfeld` um einen `Platzhalter` ergänzt (für Texte wie
  „Vorgabe 0,3" im leeren Feld)? Das ist ein Eingriff in einen Baustein, den **alle** Dialoge
  benutzen.
- **Hintergrund:** Der dritte Reiter des Gebäudedialogs bekommt rund zehn Felder, die leer
  bleiben dürfen und dann eine Vorgabe bedeuten (NULL = Vorgabe). Ohne Platzhalter muss jedes
  dieser Felder seine Vorgabe in einer eigenen Herleitungszeile nennen — zehn Zeilen statt zehn
  Platzhalter.
- **Optionen:**
  - **(a) Platzhalter ergänzen** — ein `[Parameter] string` und ein `placeholder`-Attribut, rein
    additiv; alle Dialoge erben die Möglichkeit, und die Stilblatt-Tests sind nachzuziehen.
  - **(b) Je Feld eine Herleitungszeile** — kein Eingriff in den Baustein, dafür zehn zusätzliche
    Zeilen im Dialog und eine unruhigere Maske.
- **Empfehlung des Papiers:** **Ja**, rein additiv; zieht die Stilblatt-Tests nach sich.
- **Folge bei Nichtentscheid:** Der dritte Reiter kann nicht gebaut werden, ohne sich für einen
  der beiden Wege zu entscheiden; die Softwarearchitektur führt den Punkt als A5 und sperrt
  damit G1.
- **Fällig vor:** **G1**.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U3) und 2.4; [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A5).

### U4 — Glossarabschnitt „Gebäudehülle und Gebäudemodell" vor den Übersetzungen

- **Frage:** Entsteht ein Abschnitt „13. Gebäudehülle und Gebäudemodell" im
  [`Glossar_Lokalisierung.md`](Glossar_Lokalisierung.md), **bevor** die 63 englischen Werte
  geschrieben werden?
- **Hintergrund:** Das Glossar kennt heute weder Wärmebrücke noch Verschattung, Rahmenanteil,
  Bodenplatte, Randbedingung oder operative Temperatur. Wer die Ressourcen ohne diesen Abschnitt
  schreibt, legt die englischen Begriffe implizit fest — und der nächste Auftrag legt sie anders
  fest.
- **Optionen:**
  - **(a) Glossarabschnitt zuerst** — eine überschaubare Vorarbeit; alle folgenden Masken,
    Meldungen und Berichtstexte greifen auf dieselben Begriffe.
  - **(b) Ohne Abschnitt übersetzen** — spart den Schritt und erzeugt zwei Übersetzungen desselben
    Begriffs, die später zusammenzuführen sind.
- **Empfehlung des Papiers:** **Ja** — ohne den Abschnitt entstehen zwei Übersetzungen desselben
  Begriffs.
- **Folge bei Nichtentscheid:** Die Vorgabe „ohne Abschnitt" greift stillschweigend, sobald der
  erste Auftrag Ressourcen schreibt; die Bereinigung kostet später mehr als der Abschnitt jetzt.
- **Fällig vor:** **G1** (Ressourcen des Gebäudedialogs).
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U4) und 2.9.

### U5 — die zwei Gebäudespalten-Schemaschritte verschmelzen

- **Frage:** Werden die beiden Schemaschritte für die Gebäudespalten (G1-Spalten und G2-Spalten)
  zu **einem** Schritt verschmolzen — 15 Spalten je Tabelle, **ein** Sichtneubau?
- **Hintergrund:** Entscheid **E1** liefert G1 und G2 **gemeinsam** aus. Zwei Sichtneubauten
  hintereinander sind zwei Gelegenheiten, die Sichtdefinitionen auseinanderlaufen zu lassen — und
  der Sichtneubau ist die Stelle, an der der Namensleser hängt. Der Klimaspalten-Schritt
  (`Tab_Solar`) hat eine andere Wirkung, anderen Mitläufercode und ein anderes Risiko.
- **Optionen:**
  - **(a) Verschmelzen** — ein Schritt, ein Sichtneubau, eine Prüfung; der Schritt wird größer und
    ist im Fehlerfall als Ganzes zurückzunehmen.
  - **(b) Getrennt lassen** — zwei kleinere, je für sich prüfbare Schritte; zwei Sichtneubauten
    hintereinander und zwei Gelegenheiten für abweichende Definitionen.
- **Empfehlung des Papiers:** **Ja, verschmelzen** — der `Tab_Solar`-Schritt bleibt **getrennt**.
  Die Softwarearchitektur bestätigt das unter A9 und weist darauf hin, dass die Schrittnummern,
  die das Konzept nennt, anderweitig vergeben sind (siehe A11).
- **Folge bei Nichtentscheid:** Die Vorgabe „zwei Schritte" greift; nach der Auslieferung des
  ersten Schritts ist das Verschmelzen nicht mehr möglich.
- **Fällig vor:** **G1** — vor dem ersten der beiden Schritte.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U5), 1.6 und 1.7; [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6
  (A9) und 2.4.

### U6 — Zeitbezug der Sonnengeometrie: Stundenanfang oder Stundenmitte

- **Frage:** Rechnet das Gebäudemodell die Sonnengeometrie auf den **Stundenanfang** wie der
  Bestand — oder auf die **Stundenmitte** wie Blatt 3 der Richtlinie?
- **Hintergrund:** Der Unterschied sind 7,5° Stundenwinkel und trifft genau **Ost und West**; die
  Ost/West-Trennung ist im Konzept gemessen und einer der Gründe für das Stundenmodell. Der
  Bestandsweg über die vorhandenen Strahlungsspalten bleibt in jedem Fall unberührt — er gehört
  zur Referenzbasis.
- **Optionen:**
  - **(a) Stundenanfang** — gleich wie der Bestand, keine Erklärung nötig, weicht vom Blatt-3-Weg
    ab.
  - **(b) Stundenmitte** — folgt der Richtlinie, verlangt eine benannte Abweichung gegenüber dem
    Bestandsweg im selben Programm.
  - **(c) Messen und dann entscheiden** — die Empfehlung: an der **einen** Stelle im Eingangsbauer
    beide Wege rechnen und die Wirkung auf Ost und West beziffern.
- **Empfehlung des Papiers:** **In G1 messen und dann entscheiden**, an der einen Stelle im
  Eingangsbauer. Die Softwarearchitektur präzisiert dazu unter A18, dass diese eine Stelle die
  eigene Klimaklasse ist, damit die Messung ohne den vollen Eingangsbau möglich bleibt.
- **Folge bei Nichtentscheid:** Es greift, was der erste Auftrag baut. Da die Wahl in die
  einzufrierende Basis eingeht, ist eine spätere Umstellung ein Einfrieranlass.
- **Fällig vor:** **G1** (Abnahme) — die Messung selbst ist Teil von G1.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U6) und 1.4; [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.10 und 5.12;
  [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A18).

### U7 — Wochenendkalender aus der Klimatabelle statt aus dem Referenzjahr

- **Frage:** Nimmt das Stundenmodell den Wochenendkalender aus der Spalte `WE` der Klimatabelle
  (wie der Bestand) oder aus dem Referenzjahr des Solardatencontrollers (wie das Konzept)?
- **Hintergrund:** Die Spalte `WE` stammt aus dem Kalenderjahr der PVGIS-Datenlage. Der im
  Konzept verlangte Test wäre mit einer Preisreihe aus einem anderen Jahr nicht führbar, und beide
  Wege rechnen sonst verschiedene Kalender in demselben Lauf — Nutzungsprofile und Preisreihen
  lägen an verschiedenen Wochentagen.
- **Optionen:**
  - **(a) Aus `WE`** — ein Kalender im ganzen Programm; das Stundenmodell erbt die Datenlage des
    Bestands.
  - **(b) Aus dem Referenzjahr** — folgt dem Konzepttext, erzeugt zwei Kalender und macht den
    verlangten Test unführbar.
- **Empfehlung des Papiers:** **Ja, aus `WE`.** Das Umsetzungskonzept korrigiert damit das Konzept
  ausdrücklich (Kapitel 1.10).
- **Folge bei Nichtentscheid:** Widersprüchliche Vorgaben in zwei geltenden Papieren; der
  bauende Auftrag entscheidet faktisch, und die Wahl geht in die Basis ein.
- **Fällig vor:** **G1**.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U7) und 1.10; [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 4.4.

### U8 — Normzahlen lokal statt im Repositorium

- **Frage:** Werden die Normreferenzwerte als **gitignorierte, lokal beizustellende** Datei
  geführt, deren Testfälle ohne sie schweigen — mit der Folge, dass der Normfallnachweis **lokal**
  und nicht in der CI läuft?
- **Hintergrund:** Entscheid **E6** und der Nachtrag zu Q2 halten fest, dass die Normzahlen nicht
  ausgeliefert werden; ihr Ablegen im Repositorium wäre eine Vervielfältigung. Git LFS ist keine
  Zugriffsbeschränkung, sondern nur eine andere Ablage. Zugleich ist der Normfallnachweis das
  Abnahmekriterium der Stufe G0.
- **Optionen:**
  - **(a) Lokale Datei, schweigende Tests** — kein Normwert im Repositorium; die Normfalltests
    laufen nur dort, wo die Datei beigestellt ist, die CI überspringt sie benannt. Die Lücke im
    Gate gehört ins Protokoll, der Laufauszug (Abweichung je Fall, **ohne** Absolutwerte) in die
    Dokumentation.
  - **(b) Normzahlen ins Repositorium** — vollständiges Gate in der CI, aber eine Vervielfältigung
    der Richtlinienwerte; scheidet nach E6 aus.
  - **(c) Kein Normfallnachweis in Tests** — nur der außerhalb geführte Prototypnachweis; dann ist
    die Abnahme von G0 nicht wiederholbar.
- **Empfehlung des Papiers:** **(a) Ja** — mit Protokolleintrag über die Lücke im Gate.
- **Folge bei Nichtentscheid:** Der erste Auftrag zu G0 muss die Frage selbst beantworten, und
  jede andere Antwort als (a) berührt die Lizenzlage.
- **Fällig vor:** **G0** — es ist die Abnahmebedingung dieser Stufe.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U8) und 1.9; [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.2 und N1.11 (E6).

### U9 — die Grenze von 100 Gebäuden im Bestandsweg

- **Frage:** Wird die feste Grenze von 100 Gebäuden im Bestandsweg behoben, und in welchem
  Schritt?
- **Hintergrund:** Zwei Felder fester Länge begrenzen heute die Gebäudeschleife; oberhalb der
  Grenze bricht der Lauf mit einem Indexfehler ab. Eines der beiden Felder wird nirgends gelesen.
  Die Schleife wird in der Stufe GB (Bestandsbefunde vor G1) ohnehin angefasst.
- **Optionen:**
  - **(a) In GB beheben** — das ungelesene Feld löschen, das andere auf die tatsächliche Zeilenzahl
    dimensionieren; **ergebnisneutral**, also ohne Einfrieranlass.
  - **(b) Später beheben** — ein eigener Auftrag, der dieselbe Schleife ein zweites Mal anfasst.
  - **(c) Lassen** — die Grenze bleibt; ein Projekt mit mehr Gebäuden bricht mit einem Indexfehler
    ab statt mit einer Meldung.
- **Empfehlung des Papiers:** **Ja, in GB**, wo die Schleife ohnehin angefasst wird. Ergebnisneutral.
- **Folge bei Nichtentscheid:** Die Grenze bleibt, und der Fehler tritt beim Anwender als
  Indexfehler auf — genau das Fehlerbild, das die Stufe GB sonst beseitigt.
- **Fällig vor:** **GB**.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U9); [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 11 (Stufe GB).

### U10 — Lizenzhinweisseite im Installationspaket, und zwar für alle Fremdanteile

- **Frage:** Kommt eine Lizenzhinweisseite ins Installationspaket — und dann gleich für **alle**
  ausgelieferten Fremdanteile, nicht nur für das IFC-Paket?
- **Hintergrund:** Die Lizenz des IFC-Pakets ist ein Datei-Copyleft: Ihr Abschnitt 3.1 verlangt,
  dass der Quelltext aller ausgelieferten Dateien dieser Lizenz verfügbar ist und dem **Empfänger**
  mitgeteilt wird; der dauerhafte Verweis auf die Quellen genügt. Ohne diese Seite ist der
  IFC-Import nicht auslieferbar. Die übrigen Fremdanteile sind ohnehin fällig — darunter die
  Bibliothek des Gebäudebetrachters aus E11.
- **Optionen:**
  - **(a) Eine Seite für alle Fremdanteile** — einmal gebaut, danach je Paket eine Zeile; erfüllt
    die Auflage und räumt zugleich die übrigen Anteile auf.
  - **(b) Nur für das IFC-Paket** — kleinster Aufwand, erfüllt die Auflage für dieses Paket; die
    übrigen Anteile bleiben unbelegt und die Seite wird ein zweites Mal angefasst.
- **Empfehlung des Papiers:** **Ja, mit der ersten IFC-Stufe und für alle.**
- **Folge bei Nichtentscheid:** Die Stufe G4 kann gebaut, aber **nicht ausgeliefert** werden.
- **Fällig vor:** **G4** (erste IFC-Stufe, gemeinsam mit dem Setup-Schritt).
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U10) und 3.6; [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 7.4 und N1.7 (E3);
  [ADR-003](ADR-003_IFC_xBIM_ohne_Geometriekernel.md);
  [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 8.1.

### U11 — Größenlimit für IFC-Dateien

**Mit E18 beantwortet (16.09.2026, Empfehlung angenommen): benannt ablehnen, nicht versuchen; die
iOS-Zahl wird in G4 gemessen, nicht geschätzt.** Konzept N1.23, Statusdatei Abschnitt 1; hier gekürzt.

### U12 — Herkunft der Vorgaben je Baualtersklasse

- **Frage:** Woher kommen die Vorgabewerte je Baualtersklasse, die der Import setzt?
- **Hintergrund:** Die naheliegende öffentliche Quelle hat weder eine dauerhafte Kennung noch eine
  Datensatzlizenz; ihre Werte zu übernehmen wäre rechtlich ungeklärt. Die Testdatenbank führt
  dagegen Gebäude je Klasse, aus denen sich eigene Werte ableiten lassen.
- **Optionen:**
  - **(a) Eigene Werte aus dem EPOS-Gebäudekatalog ableiten** — lizenzfrei, hausgemacht, passt zu
    den übrigen EPOS-Vorgaben; die Herleitung ist zu dokumentieren.
  - **(b) Nur die Klassen vorbelegen und den Rest leer lassen** — nichts geraten, dafür mehr
    Handarbeit für den Anwender nach jedem Import.
- **Empfehlung des Papiers:** **(a)** — mit (b) als benannter Rückfallweg.
- **Folge bei Nichtentscheid:** Der Import setzt entweder ungeklärte Fremdwerte oder gar nichts;
  beides ist beim Anwender sichtbar.
- **Fällig vor:** **G4** (Vorgaben je Baualtersklasse sind Bestandteil dieser Stufe).
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U12); [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 13 (Q11) und N1.17 (E13).

### U13 — mehrere Gebäude in einer IFC-Datei

- **Frage:** Legt der Import bei mehreren Gebäuden in einer Datei **eines je Lauf** an (Klappliste)
  — oder alle auf einmal?
- **Hintergrund:** „Alle auf einmal" müsste Katalognamen selbst erzeugen und zöge damit die
  Dublettenlogik des Katalogimports nach sich. Das ist ein eigener Arbeitsschritt mit eigenem
  Fehlerbild.
- **Optionen:**
  - **(a) Eines je Lauf** — der Anwender wählt; der Import bleibt überschaubar und
    nachvollziehbar.
  - **(b) Alle auf einmal** — bequemer bei großen Dateien; erzeugt Namen automatisch und braucht
    die Dublettenbehandlung.
- **Empfehlung des Papiers:** **Eines je Lauf.**
- **Folge bei Nichtentscheid:** Der Umfang der Stufe G4 ist unbestimmt — der Unterschied ist ein
  ganzer Arbeitsschritt.
- **Fällig vor:** **G4**.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U13) und 3.5.

### U14 — Fensterabzug an der Wandfläche

- **Frage:** Wird die Wandfläche beim Import um Fenster und Außentüren **vermindert** — oder
  bleiben die Öffnungen in der Bruttofläche, so wie die Datei sie liefert?
- **Hintergrund:** Das Modell führt Wand und Fenster **getrennt** mit je eigenem U-Wert. Ohne
  Abzug zählt die Öffnungsfläche zweimal, einmal als Wand und einmal als Fenster — der Fehler ist
  systematisch und in der Jahressumme sichtbar.
- **Optionen:**
  - **(a) Abziehen** — physikalisch richtig; wird die Differenz negativ, ist die Nettofläche der
    Datei zu nehmen, sonst eine Warnung und der Wert 0.
  - **(b) Belassen** — nichts zu rechnen; die Hülle wird systematisch zu groß.
- **Empfehlung des Papiers:** **Abziehen**, mit dem beschriebenen Rückfall.
- **Folge bei Nichtentscheid:** Der Import liefert eine zu große Hülle — ein stiller Fehler, der
  im Ergebnis wie ein schlechter Dämmstandard aussieht.
- **Fällig vor:** **G4**.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U14) und 3.4; [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 3.4
  (dieselbe Regel für gbXML).

### U15 — Wärmebrückenwerte und Anschlusslängen beim Import

- **Frage:** Werden die drei Wärmebrücken-Kennwerte und die drei Anschlusslängen beim IFC-Import
  als **Vorgabe je Baualtersklasse** gesetzt oder **leer** gelassen?
- **Hintergrund:** IFC liefert weder das eine noch das andere. Eine geratene Anschlusslänge sähe
  im Dialog aus wie eine gemessene; ein Klassen-Kennwert ist als Klassenwert erkennbar. Beide
  Felder tragen ohnehin eine Herkunftsmarke.
- **Optionen:**
  - **(a) Kennwerte als Vorgabe, Längen leer** — der Anwender sieht, was geschätzt und was
    unbekannt ist.
  - **(b) Beides als Vorgabe** — das Gebäude rechnet sofort durch; die Längen sind erfunden.
  - **(c) Beides leer** — nichts erfunden; der Wärmebrückenanteil fehlt vollständig.
- **Empfehlung des Papiers:** **Kennwerte als Vorgabe, Längen leer**, beide mit Herkunftsmarke
  „Vorgabe" bzw. „Leer".
- **Folge bei Nichtentscheid:** Der bauende Auftrag entscheidet; im ungünstigen Fall trägt eine
  erfundene Länge eine Herkunft, die sie als gemessen ausweist.
- **Fällig vor:** **G4**.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U15) und 3.4.

### U16 — Gerätebau im iOS-Workflow oder einmaliger Nachweis

**Mit E18 beantwortet (16.09.2026, Empfehlung angenommen): einmaliger Nachweis in G4 von Hand,
nach Rückfrage; kein dauerhafter Gerätebau im Workflow.** Konzept N1.23, Statusdatei Abschnitt 1;
hier gekürzt.

---

## 3. Mehrzonenmodell aus IFC — M1 bis M14

Quelle: [`Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md`](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md),
Kapitel 10 (das Papier steht in Rev. 2). Die Stufen sind G6a (Datenmodell und Pflege), G6b
(Zoneneingabe und Rechenweg), G6c (Zonenimport) und G6d (Referenzprojekt und Einfrieren). **M1** und
**M4** sind mit [ADR-005](ADR-005_Zonenkopplung_Mehrzonenmodell.md) am 16.09.2026 entschieden (E17)
und hier gekürzt.

### M1 — Kopplungsweg der Zonen

**Entschieden am 16.09.2026 mit ADR-005 (E17): Weg B — Gauß-Seidel je Stunde über die
Nachbarraum-Randbedingung**, A als Vergleichsrechnung, C für zwei Zonen als Prüforakel; die Wahl
wird gemessen bestätigt (Probe 6, Gate „eine Zone bitgleich"). Konzept N1.22, Statusdatei
Abschnitt 1; hier gekürzt.

### M2 — Raumseitenmaß oder Bruttomaß beim Import

- **Frage:** Wird beim Zonenimport durchgehend das **Raumseitenmaß** verwendet oder das
  **Bruttomaß**?
- **Hintergrund:** Das Einzonenmodell bemisst nach einer anderen Regel; ein Wechsel auf das
  Raumseitenmaß ist deshalb ein Entscheid und keine Selbstverständlichkeit. Der Unterschied
  schlägt auf alle Hüllflächen durch und damit auf die Jahressumme.
- **Optionen:**
  - **(a) Raumseitenmaß, im Dialog benannt** — passt zu den Raumgrenzen, die die Dateien liefern;
    weicht von der Bemaßungsregel des Einzonenmodells ab, und dieser Abstand ist vorher zu
    beziffern.
  - **(b) Bruttomaß** — gleiche Regel wie im Einzonenmodell; die importierten Raumgrenzen müssten
    umgerechnet werden, was ohne Wanddicken nicht zuverlässig geht.
- **Empfehlung des Papiers:** **Raumseitenmaß durchhalten und im Dialog benennen**; eine eigene
  Probe beziffert den Abstand zum Einzonenweg vorher.
- **Folge bei Nichtentscheid:** Einzonen- und Mehrzonenweg desselben Gebäudes rechnen verschieden,
  ohne dass gesagt ist, warum — genau die Art von stillem Unterschied, die das Regressionsnetz
  nicht auffängt.
- **Fällig vor:** **G6c**.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 6.2 und 10 (M2).

### M3 — die Temperaturregel für unbeheizte Nachbarzonen

- **Frage:** Gilt die Temperaturdifferenz-Regel gegenüber Nachbarzonen als **feste Vorgabe** oder
  ist sie **je Trennfläche übersteuerbar**?
- **Hintergrund:** Die Zuordnung einer Trennfläche zur Außen- oder Innenwandgruppe hängt davon ab,
  wie weit die Nachbartemperatur von der eigenen abweicht. Gemessen wird das an den **gerechneten**
  Raumkonditionen eines adiabaten Vorlaufs, nicht an den Sollwerten, und die Zuordnung fällt
  **einmal vor dem Lauf**, nie während des Laufs.
- **Optionen:**
  - **(a) Feste Vorgabe** — eine Regel für alle, keine Bedienung; ungewöhnliche Fälle (Keller,
    Treppenhaus) fallen möglicherweise falsch.
  - **(b) Vorgabe mit Übersteuerung je Trennfläche** — der Anwender kann eingreifen, sieht die
    gemessene Differenz als Beleg, und eine Überschreitung wird nach dem Lauf benannt.
- **Empfehlung des Papiers:** **Vorgabe mit Übersteuerung je Trennfläche**, Anzeige der Differenz
  als Beleg, Messung am adiabaten Vorlauf, Benennung einer Überschreitung nach dem Lauf.
- **Folge bei Nichtentscheid:** Der Zonendialog bekommt ein Bedienelement, das niemand bestellt
  hat — oder die Zuordnung ist in Grenzfällen nicht korrigierbar.
- **Fällig vor:** **G6b**.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 2.2 und 10 (M3).

### M4 — Zonen-Luftaustausch in G6 oder später

**Entschieden am 16.09.2026 mit ADR-005 (E17): in G6b**, als Paare mit der Prüfregel
`CHECK (ID_ZoneA < ID_ZoneB)`, Tabelle im Schemaschritt S-G. Konzept N1.22, Statusdatei
Abschnitt 1; hier gekürzt.

### M5 — unbeheizte Zonen im Bedarfsdialog

- **Frage:** Bekommen unbeheizte Zonen eigene Zeilen im Bedarfsdialog?
- **Hintergrund:** Unbeheizte Zonen tragen keine Heizlast, aber eine **Temperatur** und
  Überhitzungsstunden. Genau das ist der fachliche Gewinn der Mehrzonenrechnung: Ohne eigene Zeile
  bleibt etwa die Kellertemperatur unsichtbar.
- **Optionen:**
  - **(a) Ja, mit eigener Zeile** — die Temperatur wird sichtbar; die Zeile trägt in der
    Energiespalte nichts und muss das erklären.
  - **(b) Nein** — die Liste bleibt kurz und zeigt nur Energie; der fachliche Gewinn bleibt
    verborgen.
- **Empfehlung des Papiers:** **Ja.**
- **Folge bei Nichtentscheid:** Der Umfang des Bedarfsdialogs in G6b ist unbestimmt.
- **Fällig vor:** **G6b**.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 2.5, 2.8 und 10 (M5).

### M6 — Länge des Vorlaufs

- **Frage:** Rechnet der Vorlauf 30 Tage mit Konvergenzprobe — oder fest 90 Tage?
- **Hintergrund:** Der Vorlauf bringt die Speichermassen in einen eingeschwungenen Zustand, bevor
  die Jahresrechnung zählt. Bei leichten Gebäuden ist er nach kurzer Zeit erreicht; eine feste
  lange Dauer kostet dann Rechenzeit ohne Aussage.
- **Optionen:**
  - **(a) 30 Tage mit Probe** — der Vorlauf endet, wenn er eingeschwungen ist, und meldet, wenn
    nicht; braucht eine Konvergenzprüfung.
  - **(b) Fest 90 Tage** — keine Prüfung nötig, dafür dreifache Vorlaufzeit auch dort, wo sie
    nichts ändert.
- **Empfehlung des Papiers:** **30 Tage mit Probe.**
- **Folge bei Nichtentscheid:** Die Rechenzeitplanung der Mehrzonenrechnung steht auf einer
  ungeprüften Annahme; die Wahl geht in die eingefrorenen Ergebnisse ein.
- **Fällig vor:** **G6b**.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 2.9 und 10 (M6).

### M7 — Zonenregel als Vorgabe beim Import

- **Frage:** Welche Zonierungsregel ist beim Import die Vorgabe — Zusammenfassung je Geschoss oder
  stets die gröbste Regel?
- **Hintergrund:** Die Dateien liefern die Zonentopologie in aller Regel nicht mit; sie muss aus
  den Raumgrenzen gebildet werden. Die geschossweise Regel trägt in allen vier gemessenen
  Dateien; fehlen die Grenzen, bleibt zwingend die gröbste Regel.
- **Optionen:**
  - **(a) Geschossweise, mit der gröbsten Regel als Rückfall** — ein brauchbares Ergebnis in den
    gemessenen Fällen, ein definierter Rückfall sonst.
  - **(b) Stets die gröbste Regel** — immer ein Ergebnis, nie eine sinnvolle Zonierung.
- **Empfehlung des Papiers:** **Geschossweise, Rückfall auf die gröbste Regel**, weil die
  geschossweise Regel in allen vier Messdateien trägt.
- **Folge bei Nichtentscheid:** Der Import erzeugt eine nicht festgelegte Zahl von Zonen; die
  Mindestgrößenregel (M8) müsste hinterher aufräumen.
- **Fällig vor:** **G6c**.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 6.1, 6.5 und 10 (M7).

### M8 — Mindestgröße einer Zone

- **Frage:** Gilt eine Mindestgröße je Zone (der größere Wert aus einer Mindestfläche und einem
  Mindestanteil), und was geschieht mit einer zu kleinen Zone?
- **Hintergrund:** Ohne Mindestgröße entstehen aus einer der gemessenen Dateien 78 Zonen — jede
  Abstellkammer eine eigene. Der Zuschlag zur Nachbarzone mit der größten gemeinsamen Grenzfläche
  ist die physikalisch naheliegende Zusammenlegung.
- **Optionen:**
  - **(a) Mindestgröße mit Zuschlag zum größten Nachbarn** — wenige, sinnvolle Zonen; die Regel
    ist zu dokumentieren, damit der Anwender die Zusammenlegung versteht.
  - **(b) Keine Mindestgröße** — jede Raumgrenze wird eine Zone; die Obergrenze (M12) greift
    sofort.
- **Empfehlung des Papiers:** **Ja**, mit Zuschlag zum Nachbarn mit der größten gemeinsamen
  Grenzfläche.
- **Folge bei Nichtentscheid:** Importe erzeugen unbrauchbar viele Zonen; die Rechenzeit steigt,
  ohne dass das Ergebnis besser wird.
- **Fällig vor:** **G6c**.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 6.1 und 10 (M8).

### M9 — Synonymtabelle in der Auslieferung oder je Projekt

- **Frage:** Steht die Synonymtabelle, die Namen der Autorensysteme auf EPOS-Begriffe abbildet, im
  **Auslieferungskatalog** oder **je Projekt**?
- **Hintergrund:** Die Namen der Autorensysteme wiederholen sich projektübergreifend — dieselben
  Bauteil- und Materialbezeichnungen tauchen in jeder Datei desselben Programms wieder auf. Eine
  je Projekt gepflegte Zuordnung wäre in jedem neuen Projekt erneut zu leisten.
- **Optionen:**
  - **(a) Auslieferung** — einmal gepflegt, überall wirksam; je Projekt gepflegte Zuordnungen
    ergänzen sie.
  - **(b) Je Projekt** — keine Auslieferungsdatenpflege; jeder Import beginnt bei null.
- **Empfehlung des Papiers:** **Auslieferung.**
- **Folge bei Nichtentscheid:** Die Tabelle landet dort, wo der bauende Auftrag sie hinlegt; ein
  späterer Umzug ist ein Schemaschritt samt Migration und berührt die Auslieferungsvorlage.
- **Fällig vor:** **G6a** (Schema).
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 6.3, 4.2 und 10 (M9).

### M10 — große Testdatei im Repositorium und ihre Lizenz

- **Frage:** Kommt die 17,6 MB große Importprobe ins Repositorium — und ist die Lizenz der Fassung
  mit Raumgrenzen zu klären?
- **Hintergrund:** Es ist die **einzige** gemessene Datei mit Schichten **und** echten
  Raumgrenzenpaaren, also die einzige, die den Zonenimport vollständig belastet. Sie ist zugleich
  groß genug, um dauerhaft in der Git-Geschichte zu liegen, wenn sie ohne LFS-Eintrag committet
  wird. Für die Fassung mit Raumgrenzen ist die freizügige Lizenz **nicht** belegt — belegt ist
  sie nur für eine andere Ablage desselben Modells.
- **Optionen:**
  - **(a) Ja, mit LFS-Zeile im selben Schritt** — Datei verfügbar, Geschichte sauber; dazu ein
    Vermerk im Abschnitt „Git LFS" der Referenzlauf-Liesmich. Die Lizenz der Fassung mit
    Raumgrenzen ist **vorher nachzufragen**; bis dahin wird nur außerhalb des Repositoriums
    gemessen.
  - **(b) Test holt die Datei zur Laufzeit** — nichts im Repositorium; der Test schweigt ohne die
    Datei, und die CI prüft den Fall nicht.
- **Empfehlung des Papiers:** **(a), aber nur zusammen mit der LFS-Zeile im selben Schritt** —
  ohne sie liegt ein 17,6-MB-Blob dauerhaft in der Geschichte. Lizenz der Fassung mit Raumgrenzen
  **nachfragen**. Alternative ist (b).
- **Folge bei Nichtentscheid:** Entweder fehlt die einzige vollständige Probe — oder sie landet
  ohne LFS-Eintrag unwiderruflich in der Geschichte des Repositoriums.
- **Fällig vor:** **G6c**, genauer: **vor dem ersten Commit der Datei**.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 8.2 und 10 (M10);
  Hausregel zu Git LFS in [`CLAUDE.md`](../../CLAUDE.md) und
  [`Referenzlaeufe/LIESMICH.md`](../../Referenzlaeufe/LIESMICH.md).

### M11 — Referenzprojekt mit Zonen: umstellen oder neu anlegen

- **Frage:** Wird für die Mehrzonenrechnung ein **bestehendes** Referenzprojekt umgestellt — oder
  ein vierzehntes angelegt?
- **Hintergrund:** Die Basis führt heute dreizehn Projekte; jedes zusätzliche verlängert **jeden**
  CI-Lauf dauerhaft. Eine Umstellung bewegt dagegen die Zahlen eines bestehenden Projekts und
  verlangt einen begründeten Einfrierschritt — der für G6d ohnehin vorgesehen ist.
- **Optionen:**
  - **(a) Bestehendes umstellen** — keine zusätzliche Laufzeit; die Zahlen eines Projekts ändern
    sich und sind zu begründen.
  - **(b) Vierzehntes Projekt** — die bestehenden Zahlen bleiben unberührt; jeder Lauf wird
    dauerhaft länger.
- **Empfehlung des Papiers:** **Bestehendes umstellen**, im Einfrierschritt G6d.
- **Folge bei Nichtentscheid:** Der Einfrierschritt von G6d hat keinen Gegenstand, und die
  Mehrzonenrechnung bleibt im Regressionsnetz unsichtbar.
- **Fällig vor:** **G6d**.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 8.3, 9 und 10 (M11).

### M12 — Obergrenze der Zonenzahl je Gebäude

- **Frage:** Gilt eine Obergrenze von 50 Zonen je Gebäude — und wie hart?
- **Hintergrund:** Die Zahl ist eine **Setzung aus der Rechenzeit, kein Messergebnis**; das Papier
  sagt das ausdrücklich und hält sie offen, bis die Laufzeit an einem echten Mehrzonengebäude
  gemessen ist. Der Systementwurf führt denselben Punkt in seiner Wiedervorlage.
- **Optionen:**
  - **(a) 50 als Vorgabe, im Import Warnung mit Rückfrage** — der Import schlägt „auf Geschosse
    zusammenlegen" vor; die **Rechnung** lehnt darüber benannt ab.
  - **(b) Harte Grenze überall** — einfach, aber der Import bricht ab, wo eine Zusammenlegung
    genügt hätte.
  - **(c) Keine Grenze** — die Rechenzeit ist nach oben offen.
- **Empfehlung des Papiers:** **(a)**; die Zahl selbst bleibt offen, bis sie gemessen ist.
- **Folge bei Nichtentscheid:** Der Import kennt keine Schranke, und die zugesagte Rechenzeit ist
  nicht zu halten.
- **Fällig vor:** **G6c** (Import) — die Messung gehört zur Abnahme von G6b.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 2.9, 6.6 und 10 (M12);
  [Systementwurf](Systementwurf_Gebaeudesimulation_EPOS-Plan.md) 11.

### M13 — Umfang der Rekonstruktion für ein Autorensystem ohne Raumgrenzenpaare

- **Frage:** Wie weit soll die Rekonstruktion der Nachbarschaften gehen, wenn ein Autorensystem
  keine Raumgrenzenpaare schreibt?
- **Hintergrund:** Ohne Paarbildung über die Geometrie ließe sich Mehrzonigkeit nur dort anbieten,
  wo die Datei echte Paare enthält. Genau die einzige lizenzfreie **kleine** Referenzdatei käme
  dann nicht mehr in Betracht.
- **Optionen:**
  - **(a) Vollständig** — Paarbildung über die Geometrie; alle gemessenen Dateien sind nutzbar.
  - **(b) Mager** — Mehrzonigkeit nur bei echten Paaren, sonst die gröbste Regel; spart 2–3 PT und
    schließt die kleine Referenzdatei aus.
- **Empfehlung des Papiers:** **Vollständig.**
- **Folge bei Nichtentscheid:** Der Umfang von G6c ist unbestimmt; im magereren Fall fehlt eine
  kleine, lizenzfreie Probe für die CI.
- **Fällig vor:** **G6c**.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 6.2 und 10 (M13).

### M14 — Projektkopie der Baustoffe neben dem Auslieferungskatalog

- **Frage:** Wird eine **Projektkopie** der Baustoffe gebraucht, oder genügt der
  Auslieferungskatalog zusammen mit der Wertekopie an der Schicht?
- **Hintergrund:** Die Wertekopie an der Schicht schützt **gerechnete Ergebnisse**: Ändert jemand
  später einen Katalogwert, bleiben die Zahlen des Projekts, wie sie waren. Die Projektkopie
  erlaubt darüber hinaus **projekteigene Stoffe**, die es im Katalog nicht gibt.
- **Optionen:**
  - **(a) Beides behalten** — Ergebnisse geschützt **und** eigene Stoffe möglich; eine Tabelle
    mehr.
  - **(b) Nur Katalog und Wertekopie** — eine Tabelle weniger; der Weg „eigener Stoff ohne
    Katalogeintrag" entfällt.
- **Empfehlung des Papiers:** **Beides behalten** — „wer die Projektkopie streicht, spart eine
  Tabelle und verliert den Weg ‚eigener Stoff ohne Katalogeintrag'".
- **Folge bei Nichtentscheid:** Der Tabellenbestand von G6a ist unbestimmt; eine später
  nachgezogene Projekttabelle ist ein eigener Schemaschritt samt Kopier- und Transportwegen.
- **Fällig vor:** **G6a** (Schema).
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 3.5, 4.2 und 10 (M14).

---

## 4. Datenaustausch gbXML und IFC — D1, D2, D4, D5, D6, D11, D16

Quelle: [`Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md`](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md),
Kapitel 11.1 („Jetzt zu entscheiden"). Das Papier sagt selbst: **Drei Antworten braucht es, um zur
Beauftragung zu werden** — die Reihenfolge (**D1**), ob der gbXML-Export ohne synthetische
Geometrie lohnt (**D2**) und wer das Gegenüber des IFC-Exports ist (**D6**). Die übrigen vier sind
technische Festlegungen, die **unwiderruflich** sind und deshalb einen Zeitpunkt tragen. Der
zweite Block (11.2) ist zur Kenntnis und steht in Kapitel 8 dieses Registers.
Der Leseweg selbst ist mit [ADR-004](ADR-004_gbXML_LINQ_to_XML.md) am 16.09.2026 entschieden
(E16).

### D1 — Reihenfolge: gbXML-Import vor IFC-Import?

- **Frage:** Wird der gbXML-Import **vor** dem IFC-Import gebaut?
- **Hintergrund:** gbXML ist die kleinere Aufgabe: kein Fremdpaket, keine Lizenzauflage, kein
  Ersatz für einen Geometriekernel. Jede Fläche trägt dort die Fläche samt Ausrichtung und
  Neigung, sodass der Import ohne Geometriekernel auskommt. Das Format liefert die thermische
  Topologie zuverlässiger — alle vier ausgezählten Dateien schreiben sie, während die
  IFC-Raumgrenzen eine selten exportierte Sicht voraussetzen. Beide Wege benutzen dasselbe
  Zuordnungsgerüst. **Dagegen** spricht die Praxislage: Die deutsche Normungsarbeit läuft auf IFC.
- **Optionen:**
  - **(a) gbXML zuerst** — schneller zum ersten funktionierenden Import; das Zuordnungsgerüst
    entsteht am billigeren Format.
  - **(b) IFC zuerst** — die Reihenfolge des Umsetzungskonzepts; passt zur Normungslage, kostet
    Paket, Lizenzarbeit und Geometrieersatz sofort.
- **Empfehlung des Papiers:** **Vorschlag ja, Entscheid beim Anwender.** „Die Frage hängt allein
  daran, **welche Dateien im Feld ankommen** — und das weiß nur der Anwender."
- **Folge bei Nichtentscheid:** Es bleibt bei der Reihenfolge des Umsetzungskonzepts, also
  IFC zuerst.
- **Fällig vor:** **der Beauftragung** von G4c bzw. G4a.
- **Quelle:** [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 10 und 11.1
  (D1).

### D2 — lohnt der gbXML-Export nur mit der zweiten Stufe?

- **Frage:** Lohnt sich der gbXML-Export nur zusammen mit der zweiten Stufe (synthetische
  Geometrie) — oder genügt die erste?
- **Hintergrund:** Ohne synthetische Geometrie ist der Export **ein Datenblatt in XML-Form** —
  legitim als Beleg-, Archiv- und Rundlaufformat, aber keine Interoperabilität: Die untersuchten
  Zielwerkzeuge lesen die geometriearme Form nicht, verlangen Polygonzüge oder reparieren zwar
  Polygone, erfinden aber keine. **Ausnahme:** Ist das benannte Gegenüber eines der beiden
  deutschsprachigen Programme, genügt die erste Stufe — dann ist aber vorher zu **belegen**, dass
  deren Importe eine geometriearme Datei annehmen; belegt ist das nicht.
- **Optionen:**
  - **(a) Beide Stufen zusammen** — der Export ist ein Simulationsmodell; höherer Aufwand.
  - **(b) Nur die erste Stufe** — Beleg- und Archivformat; nur sinnvoll, wenn das Gegenüber es
    annimmt, und das ist zu belegen.
  - **(c) Gar nicht** — kein gbXML-Export.
- **Empfehlung des Papiers:** **Ja** — beide Stufen zusammen oder gar nicht.
- **Folge bei Nichtentscheid:** Die Stufe G7 ist nicht beauftragbar; im ungünstigen Fall entsteht
  ein Export, den kein Zielwerkzeug liest.
- **Fällig vor:** **der Beauftragung** von G7.
- **Quelle:** [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 5.1, 10 und
  11.1 (D2).

### D4 — Ergebnisgrößen in kWh mit ausdrücklicher Einheit

- **Frage:** Werden Ergebnisgrößen im Export in **kWh mit ausdrücklichem Einheitenattribut**
  geschrieben — oder in der Grundeinheit mit einem Hinweis?
- **Hintergrund:** Ohne ausdrückliche Einheitenangabe **behauptet die Datei die Grundeinheit**;
  der Empfänger liest dann Zahlen, die um Größenordnungen falsch sind. Die ehrliche Angabe kostet
  eine einmalige Helferfunktion und eine vollständige Einheitenzuweisung in der Datei.
- **Optionen:**
  - **(a) kWh mit ausdrücklicher Einheit** — der Empfänger sieht, was er liest; einmaliger
    Aufwand.
  - **(b) Grundeinheit mit Hinweis** — kein Umrechnen; der Hinweis wird beim maschinellen Lesen
    nicht gelesen.
- **Empfehlung des Papiers:** **kWh mit explizitem Einheitenattribut.** „**Unwiderruflich** — eine
  spätere Umstellung entwertet alte Exporte."
- **Folge bei Nichtentscheid:** Der erste gebaute Export legt es fest; jede Umstellung danach
  macht alle vorher erzeugten Dateien uneindeutig.
- **Fällig vor:** **der ersten Zeile Quelltext** des IFC-Exports (G7c).
- **Quelle:** [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 6.4 und 11.1
  (D4).

### D5 — deterministische Kennungen in beiden Formaten

- **Frage:** Werden die Kennungen der exportierten Objekte **deterministisch** aus dem
  Schlüsselpfad der IDs gebildet?
- **Hintergrund:** Nur mit gleichbleibenden Kennungen kann der Empfänger zwei Exportstände
  desselben Modells vergleichen, und nur so funktioniert der Rundlauf Export → Import. Kennungen
  aus **Namen** zu bilden bricht, sobald jemand einen Namen ändert — deshalb die Regel „aus den
  IDs, nie aus Namen". Sie deckt sich mit der Hausregel „neue Beziehungen über IDs, nicht über
  Textfelder".
- **Optionen:**
  - **(a) Deterministisch aus den IDs** — Modellvergleich und Rundlauf funktionieren;
    Voraussetzung ist ein festgelegter Schlüsselpfad.
  - **(b) Zufällig je Export** — einfacher; jeder Export ist für den Empfänger ein neues Modell.
- **Empfehlung des Papiers:** **Ja, von Anfang an**, aus dem Schlüsselpfad der IDs, nie aus Namen.
  „Nachträglich nicht mehr einzuführen."
- **Folge bei Nichtentscheid:** Der erste Export erzeugt Kennungen; alles, was danach kommt, kann
  nicht mehr damit verglichen werden.
- **Fällig vor:** **der ersten Zeile Quelltext** der Exporte (G7a und G7c).
- **Quelle:** [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 5.4, 6.4 und
  11.1 (D5).

### D6 — wer ist das Gegenüber des IFC-Exports?

- **Frage:** Welches Werkzeug, welcher Anwender, welcher Zweck ist das Gegenüber des IFC-Exports?
- **Hintergrund:** Das Papier sagt ausdrücklich: „**Diese Frage geht an den Anwender, nicht an die
  Technik.**" Ohne benannten Empfänger ist zwischen einem rein semantischen Export (das
  Betrachterfenster des Empfängers bleibt leer) und einem Export mit schematischen Körpern (der
  Betrachter zeigt etwas, und die Verwechslungsgefahr mit einem echten Gebäudemodell ist hoch)
  nicht sinnvoll zu wählen.
- **Optionen:**
  - **(a) Semantischer Export (G7c)** — Daten und Ergebnisse, keine Körper; der Empfänger sieht im
    Betrachter nichts und weiß, dass es kein Geometriemodell ist.
  - **(b) Zusätzlich schematische Körper (G7e)** — der Betrachter zeigt Quader und Platten; ohne
    deutliche Kennzeichnung hält der Empfänger sie für das Gebäude.
- **Empfehlung des Papiers:** **Zwischenweg: G7c bauen, G7e zurückstellen**, bis ein Empfänger
  benannt ist.
- **Folge bei Nichtentscheid:** Der Zwischenweg greift; G7e bleibt liegen. Das ist die vom Papier
  gewollte Vorgabe — kein Schaden, aber auch kein Fortschritt für die Körperstufe.
- **Fällig vor:** **G7e**; G7c geht auch ohne die Antwort.
- **Quelle:** [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 6.1, 10 und
  11.1 (D6).

### D11 — vertragliche Zulässigkeit der Rückgabe fremder Dateien

- **Frage:** Ist es zulässig, eine **fremde** IFC-Datei anzureichern und zurückzugeben?
- **Hintergrund:** Die einschlägige Austauschsicht sagt ausdrücklich, dass der Empfänger das
  Modell **nicht verändern** soll. Technisch ist der Weg billig — die Zuordnungstabellen stehen
  aus der Importstufe bereits. Rechtlich ist genau das der Grund, ihn zu lassen.
- **Optionen:**
  - **(a) Vorher klären und mit Auflagen bauen** — mindestens Beipackzettel, eigene
    Anwendungskennung, neuer Dateiname und ein Hinweis im Dialog, den der Anwender bestätigt.
  - **(b) Nicht bauen** — der Round-Trip entfällt; die Zuordnungstabellen bleiben trotzdem, weil
    sie auch die Herkunft tragen.
- **Empfehlung des Papiers:** **Vor der Round-Trip-Stufe zu klären, nicht danach** — mit den
  genannten Mindestauflagen.
- **Folge bei Nichtentscheid:** G7d ist nicht abnehmbar; das Papier macht die geklärte
  Vertragsfrage ausdrücklich zur Abnahmebedingung dieser Stufe.
- **Fällig vor:** **G7d**.
- **Quelle:** [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 6.6, 10 und
  11.1 (D11).

### D16 — erweitert die gbXML-Zonenbildung den Entscheid E7?

- **Frage:** Wird der Entscheid **E7** (Mehrzonenmodell über den IFC-Import) auf ein **zweites**
  Format erweitert, sodass auch gbXML Zonen bildet?
- **Hintergrund:** E7 nennt für das Mehrzonenmodell den IFC-Import. Die Stufe G6c führt heute nur
  die IFC-Zonenregeln. Die gbXML-Zonenregeln sind **dieselbe Bauform auf denselben Tabellen** und
  laufen sinnvoll mit G6c mit; der Einzonen-Rückfall für gbXML gehört ohnehin in G4c. Der Zuwachs
  für G6c ist mit dem Entscheid zu beziffern — in den heute genannten 16–26 PT stecken die drei
  Regeln noch **nicht**.
- **Optionen:**
  - **(a) Ja** — gbXML wird mehrzonenfähig; G6c wächst um einen zu beziffernden Betrag.
  - **(b) Nein** — gbXML bleibt **dauerhaft einzonig**, und der Zuwachs von G6c entfällt.
- **Empfehlung des Papiers:** **Vorschlag ja.**
- **Folge bei Nichtentscheid:** Der Umfang von G6c ist unbestimmt, und G4c müsste offenlassen, ob
  es die Zonenregeln des Formats überhaupt vorbereitet.
- **Fällig vor:** **der Beauftragung** von G4c.
- **Quelle:** [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 3.3 und 11.1
  (D16); [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 9 (G6c);
  [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.12 (E7).

---

## 5. Softwarearchitektur — A1 bis A19

Quelle: [`Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md`](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md),
Kapitel 6 („Offene Architekturentscheide"; das Papier steht in Rev. 2). **Neunzehn Fragen, davon
A7 und A8 (E16, E17) sowie A16 und A19 (E20) am 16.09.2026 entschieden und hier gekürzt.** Drei
davon — **A4 = U1**, **A5 = U3**, **A9 = U5** — liegen bereits unter einer Nummer des
Umsetzungskonzepts beim Anwender und stehen dort **nur als Sperrpunkt dieses Papiers**; sie sind
hier als Verweis geführt und werden unter ihrer U-Nummer entschieden (Kapitel 2). Ebenso stellt
das Papier **D1** und **D2** ausdrücklich nicht neu — sie stehen im Datenaustauschkonzept
(Kapitel 4).

Die Spalte „Ohne Entscheid blockiert" des Papiers ist hier die Zeile **Fällig vor**.

### A1 — Kaskade der Gebäudekinder gegen einen löschenden Schreibweg

- **Frage:** Bleibt das kaskadierende Löschen der Gebäudekinder bestehen, obwohl der
  Gebäude-Schreibweg möglicherweise löscht und neu anlegt? Bei der Luftstromtabelle greift die
  Falle **doppelt**, weil sie zwei Eltern hat.
- **Hintergrund:** Löscht der Schreibweg das Gebäude und legt es neu an, nimmt die Kaskade alle
  Kinder mit — Zonen, Bauteile, Luftströme. Ob der Bestandsweg das tatsächlich tut, ist **nicht
  gemessen**; es ist ein begründeter Verdacht, und die Messstelle steht fest.
- **Optionen:**
  - **(a) Kaskade behalten, Schreibweg vor G3 messen** — die Rettung wird dort eingebaut, wo das
    Löschen steht; eine Probe prüft beide Fälle.
  - **(b) Ohne Kaskade, Waisen über ein Werkzeug entfernen** — verlagert die Verantwortung in ein
    Werkzeug, das beim Anwender nie läuft.
  - **(c) Schreibweg auf „Ändern statt Löschen" umbauen** — sauber, aber ein Eingriff in den
    Bestandsweg mitten in der Einfrierkette.
- **Empfehlung des Papiers:** **(a)** — Kaskade behalten, messen, die Rettung an der Löschstelle
  einbauen.
- **Folge bei Nichtentscheid:** G3 ist blockiert; die erste Zone kann nicht angelegt werden, ohne
  zu wissen, ob das nächste Speichern sie löscht.
- **Fällig vor:** **G3**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A1),
  2.2 und 3.3.

### A2 — bleibt das IFC-Paket am Kern?

- **Frage:** Bleibt das IFC-Paket am Rechenkern, oder zieht der Leser hinter eine Schnittstelle in
  ein eigenes Projekt?
- **Hintergrund:** [ADR-003](ADR-003_IFC_xBIM_ohne_Geometriekernel.md) bindet das Paket
  unverändert an den Kern. Das Risiko ist der iOS-Gerätebau (Q10 und U16 sind mit E18 entschieden, Nachweis in G4): Vermisst das
  Trimming dort Typen, muss der Leser umziehen. Mit einer von Anfang an gezogenen Naht kostet
  dieser Umzug **eine Fabrikzeile statt eines Umbaus**.
- **Optionen:**
  - **(a) Am Kern bleiben, Naht von Anfang an ziehen** — heute keine Mehrarbeit außer der
    Schnittstelle; der Ausweg bleibt billig.
  - **(b) Sofort eigenes Projekt** — iOS verlöre den Import von vornherein.
  - **(c) Ohne Naht binden** — spart eine Schnittstelle und macht jeden Umzug zum Umbau.
- **Empfehlung des Papiers:** **(a)** — „Am Kern bleiben (ADR-003), aber `IGebaeudeLeser` **von
  Anfang an** ziehen."
- **Folge bei Nichtentscheid:** Das Papier sagt: blockiert **nichts**; die Messung steht in G4 an.
  Wird die Naht aber nicht gezogen, ist der Ausweg später teuer.
- **Fällig vor:** **G4** (spätestens) — die Naht selbst gehört in den ersten Bauauftrag.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A2) und
  1.5; [ADR-003](ADR-003_IFC_xBIM_ohne_Geometriekernel.md);
  [Systementwurf](Systementwurf_Gebaeudesimulation_EPOS-Plan.md) 11.

### A3 — Name und Ordner des Zuordnungsdialogs

- **Frage:** Wie heißt der Zuordnungsdialog und wo liegt er? **Zwei geltende Papiere nennen
  verschiedene Namen und Ordner.** Dass es **ein** Dialog ist, ist im Datenaustauschkonzept
  entschieden.
- **Hintergrund:** Der Dialog trägt **zwei Formate** (IFC und gbXML) und erbt den Ablauf des
  Katalogimports. Ein Format im Namen einer Maske, die beide trägt, wäre eine Unwahrheit — und der
  Name wandert in Maskenschlüssel, Tests, Hilfe und Wiki, ist also später teuer zu ändern.
- **Optionen:**
  - **(a) Formatfreier Name im Importordner** (Vorschlag der Softwarearchitektur) — trägt beide
    Formate; der Ordner folgt der Sache.
  - **(b) Der formatgebundene Name im Bedarfsordner** (Vorschlag des Datenaustauschkonzepts) —
    behält den dort geführten Namen und führt die Zweiformatigkeit nur im Profil.
- **Empfehlung des Papiers:** **(a)**, der formatfreie Name.
- **Folge bei Nichtentscheid:** Zwei geltende Papiere nennen weiterhin zwei Namen; der bauende
  Auftrag entscheidet, und der andere Name bleibt in einem Papier stehen.
- **Fällig vor:** **G4**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A3) und
  3.4; [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 2.4.

### A4 — ein Schreibweg im Katalogeditor (= U1)

- **Frage und Erläuterung:** siehe **U1** in Kapitel 2.
- **Empfehlung des Papiers:** „**= U1, Empfehlung dort: ja**, mit G1; ‚Speichern unter…' bleibt als
  nicht schließender Zweitknopf."
- **Folge bei Nichtentscheid:** **G1 blockiert** — „sonst hängen zehn Prüfregeln an drei
  Schreibstellen".
- **Fällig vor:** **G1**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A4);
  entschieden wird unter U1.

### A5 — Platzhalter am Zahlenfeld (= U3)

- **Frage und Erläuterung:** siehe **U3** in Kapitel 2.
- **Empfehlung des Papiers:** „**= U3, Empfehlung dort: ja**, rein additiv; zieht `StilblattTests`
  nach sich."
- **Folge bei Nichtentscheid:** **G1 blockiert** — der dritte Reiter bekommt rund zehn
  Vorgabefelder.
- **Fällig vor:** **G1**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A5);
  entschieden wird unter U3.

### A6 — ein Aggregat je Gebäude, und Ändern statt Löschen

- **Frage:** Werden Zonen, Bauteile und Luftströme als **ein Aggregat je Gebäude** geschrieben oder
  je Zone einzeln — und schreibt das Aggregat durch **Löschen und Neuanlegen** oder durch
  **Abgleich über die Ids**?
- **Hintergrund:** Nur mit einem Aggregat bleibt der eine Schreibweg über vier
  Überlagerungsebenen **einer**; und nur mit dem Abgleich über die Ids bleiben die Ids stehen, an
  denen die Importzuordnung (ohne Kaskade) und die Luftstromtabelle (mit Kaskade an **beiden**
  Zonen) hängen. Ein Schreibweg, der löscht und neu anlegt, zerstört bei **jedem** gewöhnlichen
  Speichern die Importherkunft — und damit den Round-Trip.
- **Optionen:**
  - **(a) Ein Aggregat, Ändern statt Löschen** — Abgleich über die Ids in der Reihenfolge
    Entfernen → Ändern → Anlegen, alles in **einer** Transaktion.
  - **(b) Löschen und Neuanlegen je Gebäude** — einfacher zu schreiben, zerstört aber bei jedem
    Speichern die Importherkunft.
  - **(c) Je Zone schreiben** — der Bauteildialog müsste selbst schreiben, und „Abbrechen" auf der
    Zonenebene ließe geschriebene Bauteile stehen.
- **Empfehlung des Papiers:** **(a).**
- **Folge bei Nichtentscheid:** **G6b blockiert.** Wird (b) stillschweigend gebaut, geht die
  Importherkunft beim ersten Speichern verloren, ohne dass ein Test anschlägt.
- **Fällig vor:** **G6b**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A6),
  1.4, 2.7 und 3.3.

### A7 — wird ADR-004 angenommen?

**Angenommen am 16.09.2026 (E16).** Der gbXML-Leseweg ist LINQ to XML mit handgeschriebenem
Modell; G4c hat seinen Leseweg. Konzept N1.21, Statusdatei Abschnitte 1 und 3; hier gekürzt.

### A8 — wird ADR-005 angenommen?

**Angenommen am 16.09.2026 (E17), mit Messpflicht.** Zonenkopplung über die
Nachbarraum-Randbedingung mit Durchlauf je Stunde, Zonen-Luftaustausch als Paare (M1, M4); G6b hat
sein Lösungsschema. Konzept N1.22, Statusdatei Abschnitte 1 und 3; hier gekürzt.

### A9 — zwei Gebäudespalten-Schritte zu einem (= U5)

- **Frage und Erläuterung:** siehe **U5** in Kapitel 2.
- **Zusatz dieses Papiers:** Die Schrittnummern, die das Konzept dafür nennt, sind **anderweitig
  vergeben**; die Papiere führen bis zur Beauftragung Buchstabenkürzel statt Zahlen (siehe A11).
- **Empfehlung des Papiers:** „**= U5, Empfehlung dort: ja** — 15 Spalten je Tabelle, ein
  Sichtneubau; der Klimaschritt bleibt getrennt."
- **Folge bei Nichtentscheid:** Der Schemaschritt der Gebäudespalten ist blockiert.
- **Fällig vor:** **G1**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A9) und
  2.4; entschieden wird unter U5.

### A10 — zieht der Gebäudedialog mit G1 nach `EPOS.UI.Daten`?

- **Frage:** Zieht der Gebäudedialog schon mit G1 in die plattformfreie Hüllenschicht — oder erst
  mit G6?
- **Hintergrund:** Die Hülle wird mit G1 ohnehin neu geschnitten, weil der Dialog auf U·A je
  Bauteil umgebaut wird (E2). Der Importweg setzt einen **plattformfreien Schreibweg** voraus;
  ohne ihn bleibt der Import auf iOS benannt abgelehnt.
- **Optionen:**
  - **(a) Mit G1** — ein Schnitt statt zwei; der Importweg findet den Schreibweg vor.
  - **(b) Erst mit G6** — spart im ersten Schritt Arbeit, verdoppelt sie aber, und der Import
    bleibt auf iOS bis dahin benannt abgelehnt.
- **Empfehlung des Papiers:** **Mit G1.**
- **Folge bei Nichtentscheid:** **G4 blockiert** (plattformfreie Importhülle).
- **Fällig vor:** **G1**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A10) und
  1.4; [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 2.8.

### A11 — Schemaschrittnummern jetzt vergeben oder erst bei Beauftragung?

- **Frage:** Werden die Nummern der Schemaschritte jetzt verbindlich vergeben — oder erst bei
  Beauftragung?
- **Hintergrund:** Der Schemastand steht im Transportmanifest eines Projektpakets. **Jede vergebene
  Nummer entwertet ältere Projektpakete**, weil sie einen Stand ankündigt, den es noch nicht gibt.
  Verbindlich sind schon heute **Reihenfolge und Inhalt** der Schritte; die Papiere führen bis zur
  Beauftragung Buchstabenkürzel.
- **Optionen:**
  - **(a) Erst bei Beauftragung** — die Nummern entstehen in der Reihenfolge, in der wirklich
    ausgeliefert wird; die Papiere führen bis dahin Kürzel.
  - **(b) Jetzt fest vergeben** — gibt allen Papieren feste Zahlen, erzwingt aber genau diese
    Auslieferungsreihenfolge.
- **Empfehlung des Papiers:** **(a) Erst bei Beauftragung.**
- **Folge bei Nichtentscheid:** Das Papier sagt: blockiert **nichts**; die Papiere führen bis dahin
  ihre Kürzel. Wird versehentlich (b) praktiziert, sind die Zahlen in mehreren Papieren
  nachzuziehen.
- **Fällig vor:** **der ersten Auslieferung** eines Schemaschritts der Gebäudesimulation.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A11) und
  2.4; [`ADR-001`](ADR-001_Schema-Ausrollung.md).

### A12 — wo erscheint der Produktausweis?

- **Frage:** Erscheint der Produktausweis (der Satz, mit welchem Rechenkern und in welchem Prüfband
  gerechnet wurde) auf der **Wiki-Seite** und im **Berichtskopf**?
- **Hintergrund:** In den Exportdateien ist der Ausweis mit dem Datenaustauschkonzept bereits
  entschieden, im Mehrzonenfall je Zone. Offen ist die Anzeige im Produkt. Der Ausweis wechselt,
  sobald die Stufe G0 den einen noch offenen Testfall löst — sein Wortlaut ist also nicht ewig,
  aber er ist zu jedem Zeitpunkt eindeutig.
- **Optionen:**
  - **(a) Beides** — wer einen Bericht liest, erfährt ohne Umweg, womit die Zahlen entstanden sind.
  - **(b) Nur im Wiki** — der Leser eines Berichts erführe es nicht.
- **Empfehlung des Papiers:** **Beides**, im Wortlaut von E10, **unverändert und ohne
  Umschreibung**.
- **Folge bei Nichtentscheid:** Berichte gehen ohne Ausweis hinaus; die Wiki-Seite von G2 ist ohne
  ihn unvollständig.
- **Fällig vor:** **G1** (Berichtskopf) und **G2** (Wiki-Seite).
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A12) und
  4.3; [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 6.5;
  [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.15 (E10).

### A13 — Herkunftsspalten an der Gebäudetabelle?

- **Frage:** Bekommt `Tab_Gebaeude` die Spalten für **Herkunft** und **Quellkennung**, wie Zone,
  Bauteil, Aufbau und Baustoff sie tragen?
- **Hintergrund:** Das Gebäude hat einen Katalogzwilling, an dem eine Importherkunft nichts
  bedeutet; die Regel der Spaltengleichheit zwischen Projekt- und Katalogtabelle zwänge die Spalten
  aber dorthin. Jede weitere Gebäudespalte ist außerdem ein weiterer Sichtneubau.
- **Optionen:**
  - **(a) Nein — Gebäudeherkunft nur in der Importzuordnung** — 15 neue Spalten, ein Sichtneubau.
  - **(b) Beide Spalten an beide Tabellen** — dann 17 statt 15 neue Spalten und ein **zweiter**
    Sichtneubau.
- **Empfehlung des Papiers:** **(a) Nein**, Regel „Gebäudeherkunft nur in `Tab_Importzuordnung`".
- **Folge bei Nichtentscheid:** **G4 blockiert** (der Herkunftsschritt); der Spaltenumfang des
  Schemaschritts ist unbestimmt.
- **Fällig vor:** **G4** (Herkunftsschritt).
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A13) und
  2.7; [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 7.3.

### A14 — was trägt den Umschalter Klassenweg → Bauteilweg?

- **Frage:** Woran erkennt der Rechenkern, ob er den Klassenweg oder den Bauteilweg rechnet — an
  der **Datenlage** (leere Zonentabelle heißt Klassenweg) oder an einem eigenen Wert?
- **Hintergrund:** Nach der Datenlagenregel ändert das Anlegen der **ersten** Zone die Zahlen eines
  Projekts, ohne dass jemand einen Schalter umgelegt hätte. Das ist für den Anwender überraschend
  und für die Referenzbasis ein Einfrieranlass genau dort, wo ein Referenzprojekt Zonen bekommt.
- **Optionen:**
  - **(a) Datenlage behalten, den Übergang benennen** — Rückfrage vor der ersten Zone,
    Herleitungszeile am Modellschalter in **beiden** Stellungen; Einfrieranlass nur dort, wo ein
    Referenzprojekt Zonen bekommt.
  - **(b) Dritter Persistenzwert am Modellfeld** — der Weg steht ausdrücklich in der Datenbank,
    kostet aber einen Wert, der beim Löschen der letzten Zone wieder falsch wäre.
- **Empfehlung des Papiers:** **(a).**
- **Folge bei Nichtentscheid:** **G3 blockiert** — der Übernahmeknopf entsteht dort, und ohne den
  Entscheid weiß niemand, was er auslöst.
- **Fällig vor:** **G3**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A14);
  [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 4.3.

### A15 — was geschieht mit der letzten reinen Bestandsbasis?

- **Frage:** Was geschieht mit der letzten Referenzbasis, die noch ohne Stundenmodell entstanden
  ist und gegen die der Nachweis des Rückwegs (Tagesbilanz) läuft?
- **Hintergrund:** Ein Befund will sie aufheben; die Hausregel sagt dagegen: „frühere Basen liegen
  nicht mehr im Repositorium, gerechnet wird ausschließlich gegen die aktuelle Basis". Der
  Systementwurf empfiehlt denselben Ausweg wie die Softwarearchitektur.
- **Optionen:**
  - **(a) Ein Referenzprojekt, das dauerhaft auf dem Altweg steht** und in der **neuen** Basis
    mitgefroren wird — dann prüft jeder Lauf **beide** Wege gegen dieselbe, aktuelle Basis, und die
    neuen Reihen entstehen für dieses Projekt gar nicht erst. Die alte Basis bleibt nur bis zum
    Merge von G1 und G2 und wandert dann mit ihrem Protokoll in die Geschichte.
  - **(b) Zwei Basen dauerhaft** — gegen die Hausregel; jeder Lauf müsste sagen, gegen welche er
    misst.
  - **(c) Ein Dateiausschluss im Vergleich** — löste es technisch, kostet aber einen neuen Schalter
    an einem Werkzeug, an dem die ganze Nachweiskette hängt.
- **Empfehlung des Papiers:** **(a)** — so auch der Systementwurf. **E23 (16.09.2026):** das Projekt
  bleibt dauerhaft auf dem Altweg, der Rückweg-Test bleibt (Konzept N1.28).
- **Folge bei Nichtentscheid:** **G1 + G2 blockiert**: Der Einfrierschritt kann nicht abgenommen
  werden, weil unklar ist, wogegen der Rückweg künftig gemessen wird.
- **Fällig vor:** **G1 + G2** (der gemeinsame Einfrierschritt).
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A15) und
  2.8; [Systementwurf](Systementwurf_Gebaeudesimulation_EPOS-Plan.md) 8.4; Hausregel
  „Regressionsnetz" in [`CLAUDE.md`](../../CLAUDE.md).

### A16 — Zuschnitt des zweiten Verzweigungspunkts

**Durch E20 entschieden (16.09.2026): Es gibt keinen zweiten Verzweigungspunkt mehr.** Der
Tagesbilanz-Weg wird Zeichen für Zeichen in ein eigenes Modul `Altweg/` verschoben, der VDI-Weg ruft
nichts daraus; eine Weiche am Eingang der Gebäudebedarfsrechnung wählt das Modul, und was beide
brauchen (Bewohner, Skalierungsfaktor, Klimareihen), liefert ein modellfreier Vorbereitungsschritt.
Konzept N1.25, [ADR-006](ADR-006_Trennung_Altweg_VDI6007.md).

### A17 — Maskenschlüssel und Menüzeile für Import und Export?

- **Frage:** Bekommen Gebäudeimport und ‑export einen **eigenen Maskenschlüssel und eine
  Menüzeile**? **Zwei geltende Papiere widersprechen sich:** Das Datenaustauschkonzept sagt nein,
  ein Bestandsbefund sagt ja.
- **Hintergrund:** Das Menü ist Daten, und ein Untermenü mit nur einem Punkt ist verboten. Ein
  eigener Maskenschlüssel kostet je Schlüssel mehrere Pflegestellen (Navigation, Hilfe, Wiki,
  Tests). Dagegen steht, dass ein Menüeintrag auch ohne geöffnetes Gebäude ein sichtbarer Einstieg
  wäre.
- **Optionen:**
  - **(a) Nein — Überlagerung im Gebäudedialog**, aufgemacht über Knöpfe in der Katalogleiste, je
    Format ein Profil; spart die Pflegestellen und macht die Untermenü-Regel gegenstandslos.
  - **(b) Zwei flache Menüzeilen auf **einen** Schlüssel mit Argument** (Muster des vorhandenen
    PV-Imports) — sichtbarer Einstieg auch ohne geöffnetes Gebäude.
- **Empfehlung des Papiers:** **(a) Nein.**
- **Folge bei Nichtentscheid:** **G4 blockiert**; zwei geltende Papiere behalten zwei Antworten.
- **Fällig vor:** **G4**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A17) und
  3.1; [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 11.2 (D14).

### A18 — bleibt der Klimaweg eine eigene Klasse?

- **Frage:** Bleibt der Klimaweg des Gebäudemodells eine **eigene Klasse**, oder fällt er in den
  Eingangsbauer zurück?
- **Hintergrund:** Das Umsetzungskonzept sagt: „hier — und nur hier — fällt die Entscheidung über
  den Zeitbezug, die Azimutzuordnung und die Erdreichtemperatur", und U6 sagt „an der einen Stelle
  im Eingangsbauer". Genau diese Entscheidungen muss der Prüfmodus umschalten können; sie ohne den
  ganzen Eingangsbau prüfen zu können, spart in G1 die Messung, die U6 verlangt.
- **Optionen:**
  - **(a) Eigene Klasse behalten**, aber **ausschließlich** vom Eingangsbauer gerufen — „an einer
    Stelle" gilt dann als Aufrufstelle.
  - **(b) In den Eingangsbauer zurückfalten** — hält den Wortlaut buchstäblich, macht die Messung zu
    U6 aber teurer, weil jeder Probefall den vollen Eingang bauen muss.
- **Empfehlung des Papiers:** **(a).**
- **Folge bei Nichtentscheid:** **G1 blockiert** — die Klassenliste des Rechenkerns steht sonst
  nicht fest.
- **Fällig vor:** **G1**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A18) und
  1.3; [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 1.4 und 5 (U6).

### A19 — sieben Modellparameterfelder verstecken oder sperren (= U2)

**Durch E20 überholt (16.09.2026), mit U2.** Der Dialog ist in VDI-6007-Struktur aufgebaut, die
Modellparameter sind immer sichtbar; Altweg-Felder stehen im eingeklappten Abschnitt „Tagesbilanz (Bestandsweg)" eines
Altweg-Gebäudes. Festlegung in Kapitel 8; Konzept N1.25, ADR-006.

---

## 6. Kühlung — K1, K4 bis K12, K19 und K20 bis K23

Quelle: [`Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md`](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md),
Kapitel 12.1 („Jetzt zu entscheiden"). Das Papier entsteht aus Entscheid **E12** (Kühlung als
vierter Kanal) und trägt inzwischen auch **E15** (Wärmepumpen mit Kühlfunktion: Auswahl im Katalog
und Konfiguration je Anlage). Die Stufen sind **KU0** (Papiere), **KU1** (der Kanal, gemeinsam mit
G1 + G2), **KU2** (der Erzeuger) und **KU3** (das Umfeld).

Der zweite Block (12.2) sind technische Festlegungen, denen nur zu widersprechen ist; sie stehen in
Kapitel 8. **K20 bis K23** sind die vier Fragen, die mit **E15** und dem Gegenlesen vom
16.09.2026 hinzugekommen sind; das Papier führt sie in Kapitel 12.1 neben K1–K19. **K1** ist mit **E21** (Simulation Kältebedarf analog zur Wärmeseite, 16.09.2026) entschieden und hier gekürzt.

### K1 — vierter Kanal oder eigene Kältestruktur

**Durch E21 entschieden (16.09.2026): vierter Kanal in der bestehenden Kanalstruktur mit getrennter
Deckungsseite** — die Simulation des Kältebedarfs wird analog zur Simulation des Wärmebedarfs
gebaut (Fassade `SimulationKaeltebedarf`, Kanal `KUEHLUNG`, Kennzahlen, Bedarfsdialog, Deckung,
Bericht mit denselben Mustern). Konzept N1.26, Kühlkonzept Rev. 3, Statusdatei Abschnitt 1; hier gekürzt.

### K4 — Stellung der Kühlung in der Knappheitsreihenfolge

- **Frage:** Wo steht die Kühlung in der Reihenfolge, nach der bei Knappheit gedeckt wird?
- **Hintergrund:** Die Kälteseite hat eigene Erzeuger und eine eigene Deckungswelt; ein Rang in der
  Wärme-Knappheitsreihenfolge steuert für sie **nichts**. Ein Rang, der nichts steuert, darf in der
  Oberfläche nicht aussehen, als täte er es.
- **Optionen:**
  - **(a) Zuletzt, und in der Oberfläche nicht zur Bearbeitung angeboten** — ehrlich; der Anwender
    sieht keinen Regler, der nichts bewirkt.
  - **(b) Zuletzt, aber bearbeitbar** — gleiches Verhalten, aber ein Bedienelement ohne Wirkung.
  - **(c) Eingeordnet wie ein Wärmekanal** — vermischt die Deckungswelten.
- **Empfehlung des Papiers:** **Zuletzt**, und in der Oberfläche nicht zur Bearbeitung angeboten.
- **Folge bei Nichtentscheid:** Der Dialog zeigt entweder einen wirkungslosen Rang oder verschweigt
  ihn ohne Begründung.
- **Fällig vor:** **KU1**.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 4.5 und 12.1 (K4).

### K5 — bleibt Feuchte ausgeschlossen?

- **Frage:** Bleibt die Feuchte ausgeschlossen — also **sensible Kälte ohne Entfeuchtung**?
- **Hintergrund:** Entfeuchtung ist ein eigener Rechenweg mit eigener Datenlage. Ohne sie ist der
  ausgewiesene Kältebedarf in feuchtebelasteten Gebäuden zu klein — und diese Grenze muss an
  **jeder** Zahl stehen: Bericht, Dialog, Wiki und Export. Ein exportierter Kältebedarf ohne diesen
  Hinweis ist in fremder Hand eine falsche Zahl.
- **Optionen:**
  - **(a) Ja, ausgeschlossen, und die Grenze steht an jeder Zahl** — ehrlicher Umfang, überall
    kenntlich.
  - **(b) Entfeuchtung aufnehmen** — vollständiger, aber ein eigener Rechenweg mit eigener
    Datenlage; kein Bestandteil dieses Vorhabens.
- **Empfehlung des Papiers:** **Ja**, und die Grenze steht an jeder Zahl.
- **Folge bei Nichtentscheid:** Es bleibt beim Ausschluss — aber ohne die verlangte Kennzeichnung,
  und dann ist die Zahl missverständlich.
- **Fällig vor:** **KU1** (der Hinweistext gehört zu den Ressourcen dieser Stufe).
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 1.3, 9.2 und 12.1 (K5).

### K6 — Zonen, die gleichzeitig heizen und kühlen

- **Frage:** Wie werden mehrere Zonen auf **einen** Kanalwert geführt, wenn Zonen in derselben
  Stunde heizen und kühlen?
- **Hintergrund:** Eine Saldierung („Heizen minus Kühlen") **erfindet eine Wärmerückgewinnung**,
  die es im Gebäude nicht gibt: In Wirklichkeit laufen zwei Anlagen gegeneinander. Der Fall ist
  real (Südzone kühlt, Nordzone heizt) und tritt erst mit dem Mehrzonenmodell auf.
- **Optionen:**
  - **(a) Nicht saldieren** — beide Kanäle tragen ihren Betrag, und eine Kennzahl weist den Fall
    aus.
  - **(b) Saldieren** — eine einzige Zahl je Stunde; sie behauptet eine Rückgewinnung.
- **Empfehlung des Papiers:** **Nicht saldieren.**
- **Folge bei Nichtentscheid:** Im Mehrzonenfall entsteht eine stille, physikalisch falsche
  Gutschrift.
- **Fällig vor:** **KU1** (die Regel gehört in den Kanal, auch wenn der Fall erst mit Zonen
  auftritt).
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 3.5 und 12.1 (K6).

### K7 — Kältespeicher ja oder nein?

- **Frage:** Bekommt die Kälteseite einen **Kältespeicher**?
- **Hintergrund:** Ein Speicher ohne Rechenweg wäre ein Persistenzwert, der nichts tut — genau das,
  was die Hausregeln vermeiden. Der Speicher gehört sachlich zur Kältemaschine, die ohnehin erst in
  der letzten Stufe entsteht.
- **Optionen:**
  - **(a) Vertagen nach KU3**, gemeinsam mit der Kältemaschine — und bis dahin **kein
    Persistenzwert ohne Rechenweg**.
  - **(b) Jetzt aufnehmen** — 3–5 PT, ein Pufferverwendungswert und ein Eintrag in der Klassenliste;
    ein Speicher, der erst später etwas tut.
- **Empfehlung des Papiers:** **Vertagen nach KU3.**
- **Folge bei Nichtentscheid:** Der Schemaumfang von KU1 ist unbestimmt, und es droht ein Wert ohne
  Rechenweg.
- **Fällig vor:** **KU1** (Schemaumfang) — gebaut würde er ohnehin erst in KU3.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 4.6 und 12.1 (K7).

### K8 — freie Kühlung und Rückkühlung: bauen oder benannt ablehnen?

- **Frage:** Werden freie Kühlung und Rückkühlung gebaut — und wenn ja, als eigene Erzeuger?
- **Hintergrund:** Ohne Rückkühlung ist eine Kältemaschine energetisch unvollständig: Die Abwärme
  muss irgendwohin, und der Aufwand dafür gehört zur Strombilanz. Die Nachtlüftung ist dagegen eine
  **Gebäudemaßnahme** und steht im Rechenweg **vor** der Kühlung; die freie Kühlung über die Quelle
  ist ein **Betriebsfall** der vorhandenen Maschine, kein eigener Erzeuger.
- **Optionen:**
  - **(a) Bauen, aber keine als eigener Erzeuger** — Nachtlüftung in G2, freie Kühlung als
    Betriebsfall, Rückkühlung als Bestandteil der Kältemaschine.
  - **(b) Als eigene Erzeugertypen führen** — mehr Einträge in Erzeugerliste, Deckungsreihenfolge
    und Wirtschaftlichkeit für Dinge, die keine Maschinen sind.
  - **(c) Benannt ablehnen** — die Kältemaschine bleibt energetisch unvollständig.
- **Empfehlung des Papiers:** **Bauen, aber keine als eigener Erzeuger.**
- **Folge bei Nichtentscheid:** Entweder fehlt der Rückkühlaufwand in der Strombilanz, oder die
  Erzeugerliste füllt sich mit Einträgen, die keine Erzeuger sind.
- **Fällig vor:** **KU2** (Rückkühlung) bzw. **KU3** (freie Kühlung); die Nachtlüftung gehört zu
  **G2**.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 3.4, 5.4 und 12.1 (K8).

### K9 — Tarif und Stromträger des Kältestroms

- **Frage:** Trägt der Kältestrom denselben Tarif und denselben Stromträger wie der
  Wärmepumpenstrom?
- **Hintergrund:** Es ist physisch dieselbe Maschine an derselben Steckdose. Ein eigener Tarif wäre
  „eine zweite Wahrheit für dieselbe Steckdose" und müsste in Wirtschaftlichkeit und Emissionen
  getrennt geführt werden.
- **Optionen:**
  - **(a) Ja, derselbe Tarif und Träger** — eine Tarifzeile, eine Emissionszuordnung.
  - **(b) Eigener Tarif** — erlaubt getrennte Betrachtung, verlangt eine zweite Tarifzeile und
    verdoppelt die Pflege.
- **Empfehlung des Papiers:** **Ja.**
- **Folge bei Nichtentscheid:** Die Wirtschaftlichkeitsrechnung der Kälteseite ist nicht
  festgelegt; KU2 ist nicht abnehmbar.
- **Fällig vor:** **KU2**.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 6.1, 6.3 und 12.1 (K9).

### K10 — Kühlbetrieb als ausdrückliche Projekteinstellung

- **Frage:** Bleibt der Kühlbetrieb so lange aus, bis eine **ausdrückliche Projekteinstellung** ihn
  einschaltet?
- **Hintergrund:** Ein Kanal, der in jedem Projekt sofort rechnet, bewegt **alle** Referenzprojekte
  — Kanalsummen, Deckung, Wirtschaftlichkeit. Das Papier stellt ausdrücklich klar: Die Einstellung
  ist **nicht** dazu da, das Einfrieren zu vermeiden (der Einfrierschritt kommt ohnehin), sondern
  um die zwölf übrigen Projekte und jedes Bestandsprojekt des Anwenders zu schützen.
- **Optionen:**
  - **(a) Ja, Vorgabe „aus"** — Bestandsprojekte rechnen unverändert; wer Kühlung will, schaltet
    sie ein.
  - **(b) Immer an** — kein Schalter; jedes Bestandsprojekt ändert beim nächsten Lauf seine Zahlen.
- **Empfehlung des Papiers:** **Ja** — Vorgabe 0.
- **Folge bei Nichtentscheid:** Die Rückwärtsverträglichkeit aller Bestandsprojekte hängt in der
  Luft; der Einfrierschritt von KU1 würde weit mehr bewegen als das eine Referenzprojekt.
- **Fällig vor:** **KU1**.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 7.2, 10.5 und 12.1
  (K10).

### K11 — eigener Kühlsollwert und eigene Leistungsgrenze

- **Frage:** Bekommt das Gebäude einen **eigenen Kühlsollwert** mit Zeitprofil und eine eigene
  **Kühlleistungsgrenze** — oder bleibt die vorhandene Maximaltemperatur die einzige Kühleingabe?
- **Hintergrund:** Die vorhandene Maximaltemperatur ist eine Überhitzungsschwelle, kein Sollwert
  einer Anlage; ohne Leistungsgrenze rechnet das Modell jede Stunde auf den Sollwert herunter, ganz
  gleich, wie groß die Maschine ist. Das Zeitprofil (Nachtwert) ist dagegen eine Verfeinerung.
- **Optionen:**
  - **(a) Eigener Sollwert und eigene Grenze in KU1, Zeitprofil erst in KU3** — der Kanal ist
    brauchbar, der Schemaumfang bleibt klein.
  - **(b) Alles sofort** — vollständiger; mehr Spalten und mehr Dialogfelder in derselben Stufe.
  - **(c) Nur die vorhandene Maximaltemperatur** — kein Schemaschritt; die Kälterechnung kennt
    keine Anlagengrenze.
- **Empfehlung des Papiers:** **(a)** — die übrigen Felder erst bei Bedarf.
- **Folge bei Nichtentscheid:** Schema- und Dialogumfang von KU1 sind unbestimmt.
- **Fällig vor:** **KU1**.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 7.1 und 12.1 (K11).

### K12 — gilt Kühlung auf iOS?

- **Frage:** Gilt die Kühlung auch auf der iOS-Schale?
- **Hintergrund:** Der Rechenkern ist plattformfrei, und es entsteht **kein neuer
  Maskenschlüssel** — die Kühlung erscheint als Gruppe in vorhandenen Dialogen. Damit ist ein
  iOS-Lauf für KU1 und KU2 **nicht begründet**; die Hausregel verlangt für jeden macOS-Läufer
  ohnehin eine Rückfrage, und der Lauf zählt zehnfach.
- **Optionen:**
  - **(a) Ja, ohne eigenen iOS-Lauf für KU1/KU2** — die Kühlung ist auf beiden Plattformen da; der
    Nachweis führt der Kern-Lauf.
  - **(b) Ja, mit iOS-Lauf** — zusätzlicher Nachweis, Rückfragepflicht, zehnfaches Kontingent.
  - **(c) Auf iOS ausblenden** — eine Sonderbehandlung ohne technischen Grund.
- **Empfehlung des Papiers:** **(a) Ja.**
- **Folge bei Nichtentscheid:** Es besteht die Gefahr, dass ein iOS-Lauf ohne Anlass angefordert
  wird — entgegen der Hausregel zum Läufer-Kontingent.
- **Fällig vor:** **KU1**.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 8.6, 10.6 und 12.1
  (K12).

### K19 — eigener Einfrierschritt für KU2

- **Frage:** Wird KU2 mit einem **eigenen, kleinen Einfrierschritt** abgenommen — oder bleibt die
  Kältedeckung im Regressionsnetz unsichtbar?
- **Hintergrund:** Mit KU2 bewegt sich **ein** Projekt: das Referenzprojekt mit Kälteerzeuger. Ohne
  Einfrierschritt wird die Deckung nie gegen eine Basis gerechnet — dieselbe Begründung, mit der
  Q14 ein Referenzprojekt für das Gebäudemodell verlangt hat. Das Papier sieht dafür eine **fünfte
  Einfrierregel „gesäte Kältedaten"** vor, die an beide Orte gehört: in die Liesmich der
  Referenzläufe und in den Abschnitt „Regressionsnetz" der Wurzel-CLAUDE.md.
- **Optionen:**
  - **(a) Eigener Einfrierschritt** — ein Projekt bewegt sich, begründet und geprüft.
  - **(b) Kein eigener Schritt** — die Kältedeckung bleibt ungeprüft; jede spätere Änderung an ihr
    ist nicht messbar.
- **Empfehlung des Papiers:** **Eigener Einfrierschritt.**
- **Folge bei Nichtentscheid:** Die Kältedeckung ist dauerhaft ohne Regressionsnachweis.
- **Fällig vor:** **KU2**.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 10.4, 10.5 und 12.1
  (K19).

### K20 — Filter „nur Wärmepumpen mit Kühlfunktion" in der Katalogliste

- **Frage:** Wird die Kennzeichenspalte „kühlt" der Katalogliste **filterbar** — als **benannte
  Ausnahme** von der Regel, dass Ja/Nein-Spalten nicht filterbar sind, oder über eine **eigene
  Filterart**?
- **Hintergrund:** Der Katalog führt heute eine Ja/Nein-Spalte für die Kühlfunktion, und eine
  einzige Zeile im Filterprofil schließt das Filtern solcher Spalten aus. Das ist **kein Versehen,
  sondern Absicht**: Das Katalogfilter-Konzept begründet es damit, dass ein Eingabefeld für zwei
  Werte ein Bedienelement ohne Gewinn sei — die Sortierung stelle die betroffenen Sätze ohnehin
  zusammen. **Für die Kühlung trägt diese Begründung nicht mehr**, und der Unterschied ist E15: Ab
  KU2 ist die Kühlfähigkeit keine Zusatzangabe, sondern die Vorbedingung dafür, dass die Maschine
  eine Aufgabe des Projekts überhaupt erfüllen kann. Wer eine Kältedeckung plant, will nicht
  sortieren, sondern die Untermenge sehen.
- **Optionen:**
  - **(a) Benannte Ausnahme** — die Spalte wird zugelassen, das Filterfenster zeigt statt eines
    Textfelds zwei Schalter („nur mit Kühlfunktion" / „alle"). Eine Zeile plus ein Zweig im
    Filterfenster; der Rest des Filterwerks bleibt unberührt. **Dagegen:** eine Ausnahme von einer
    begründeten Regel — und die nächste Kennzeichenspalte fragt, warum nicht sie auch.
  - **(b) Eigene Filterart** — eine zusätzliche Spaltenart neben der Ja/Nein-Art; die Regel bleibt
    wörtlich stehen und gilt weiter für die nicht gekennzeichneten Spalten, und jede künftige
    Kennzeichenspalte entscheidet selbst am Spaltenprofil. **Dagegen:** eine neue Spaltenart samt
    Anzeige, Filterfenster, Profiltests und Fortschreibung des Katalogfilter-Konzepts.
- **Empfehlung des Papiers:** **(b) Eigene Filterart** — rund ein Personentag mehr, „dafür bleibt
  die Regel eine Regel und jede künftige Kennzeichenspalte entscheidet am Profil, nicht in einer
  Ausnahmeliste". Eine Kennzeichenspalte ist **grundsätzlich** nicht filterbar, es sei denn, sie
  ist ausdrücklich dafür bestimmt; das Katalogfilter-Konzept bekommt dazu einen Satz, im selben
  Auftrag wie der Filter. Der bestehende Trichter und die Auflage, dass der Unterschied „gefüllt /
  nicht gefüllt" ohne Farbe tragen muss, bleiben unverändert.
- **Folge bei Nichtentscheid:** Die Zusage aus **E15** („die Katalogliste bekommt die Auswahl ‚nur
  mit Kühlfunktion'") ist nicht bedienbar: **15 von 51 Katalogsätzen sind kühlfähig**, und ohne
  Filter sucht der Anwender sie von Hand.
- **Fällig vor:** **KU2**.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 5.0.3 und 12.1 (K20);
  [Katalogfilter-Konzept](Konzept_Katalogfilter_EPOS-Plan.md) 5.6.2;
  [Status](Status_Gebaeudesimulation_VDI6007.md) (E15).

### K21 — Kühl-Vorlauf: Auswahl aus den Stützstellen oder freie Eingabe

- **Frage:** Wird die Kaltwasser-Vorlauftemperatur der Anlage aus den **Stützstellen der
  Kühlkennlinie ausgewählt** — oder **frei eingegeben**, mit Interpolation zwischen zwei Vorläufen?
- **Hintergrund:** Der Wert wählt die Kennlinie, so wie der Heizvorlauf es auf der Wärmeseite tut;
  die Kennlinie ist über Vorlauf, Außentemperatur und Laststufe aufgespannt. **Ohne diesen Wert
  wählt die Rechnung die Kennlinie zufällig** — zwei Vorläufe ergeben zwei verschiedene
  Kälteverhältnisse bei derselben Außentemperatur; das Papier führt diesen Fall unter seinen
  Risiken. In der Testdatenbank stehen zwei Stützstellen. Die Vorgabe ist definiert: leeres Feld
  bedeutet den kleinsten Stützwert der Kennlinie.
- **Optionen:**
  - **(a) Auswahl aus den Stützstellen** — es kann nur ein Wert gewählt werden, den die Kennlinie
    trägt; keine Interpolation über den Vorlauf, und Kühl- und Heizseite teilen dieselbe Regel.
  - **(b) Freie Eingabe mit Interpolation** — vertraut wie andere Zahlenfelder; verlangt eine
    Interpolationsregel zwischen zwei Vorläufen und eine Regel für Werte außerhalb — beides hat die
    Heizseite heute nicht.
- **Empfehlung des Papiers:** **Auswahl.** „Eine Interpolation über den Vorlauf hat die Heizseite
  ebenfalls nicht, und **eine** Regel für beide Seiten ist mehr wert als ein Sonderweg." Dazu eine
  Extrapolationswarnung wie auf der Heizseite und eine Stützstellenprobe je Vorlauf.
- **Folge bei Nichtentscheid:** Dialogumfang und Extrapolationsregel stehen nicht fest, und es ist
  offen, ob Kühl- und Heizseite denselben Kennlinienleser teilen.
- **Fällig vor:** **KU2** (Erzeugerdialog und Kennlinienleser).
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 5.1, 8.2, 10.2, 12.1
  (K21) und 13.

### K22 — führt die COP-Spalte der Kühltabelle wirklich den EER?

- **Frage:** Führt die Spalte `COP` der Kühlkennlinie **wirklich das Kälteverhältnis (EER)** — oder
  in manchen Datensätzen das Wärmeverhältnis eines Heizbetriebs bei Kühlvorlauf?
- **Hintergrund:** Der Herstellerdaten-Import trennt Heiz- und Kühlblock, **die Herstellerangaben
  dahinter sind aber nicht gegengelesen**. Die Spalte heißt im Schema wie das Wärmeverhältnis und
  ist eine eingefrorene Spalte; das Papier legt dazu bereits fest, dass sie **nicht umbenannt**,
  aber in Kern, Dialog und Bericht als EER geführt und beschriftet wird — der eine Fall, in dem
  Spaltenname und Anzeigename bewusst auseinandergehen, und er gehört ins Glossar. Offen ist
  nicht die Beschriftung, sondern die **Datenlage**.
- **Optionen:**
  - **(a) Vor der Erzeugerstufe prüfen** — die vorhandenen Kühlkennlinien durchsehen (sieben
    Katalogsätze, 174 Zeilen) und das Ergebnis im Glossar festhalten; ist die Lage uneinheitlich,
    wird die Größe beim Import **benannt** umgerechnet oder der Satz abgelehnt.
  - **(b) Ungeprüft als EER lesen** — kostet nichts und macht aus einer verwechselten Kennzahl
    einen stillen Faktor in jeder Kältekennzahl.
- **Empfehlung des Papiers:** **(a) Vor KU2 an den vorhandenen Kühlkennlinien prüfen** und das
  Ergebnis im Glossar festhalten — „nie stillschweigend als EER gelesen".
- **Folge bei Nichtentscheid:** Jede Kältekennzahl und jede Wirtschaftlichkeitszahl der Kühlseite
  hängt an einer ungeprüften Annahme; „eine verwechselte Kennzahl ist hier ein Faktor, kein
  Rundungsfehler".
- **Fällig vor:** **KU2**.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 5.1, 6.4 und 12.1
  (K22); [Glossar_Lokalisierung.md](Glossar_Lokalisierung.md).

### K23 — Hilfsstrom je Anlage oder pauschal je Projekt

- **Frage:** Wird der Anteil Hilfsstrom des Kältekreises (Pumpen, Ventilatoren) **je Anlage**
  geführt — oder **pauschal je Projekt** in den Einstellungen?
- **Hintergrund:** Der Kältestrom einer Stunde ist die Kältemenge geteilt durch das
  Kälteverhältnis **zuzüglich** der Hilfsantriebe. Der Anteil hängt an der Hydraulik der Maschine,
  nicht am Projekt: Luft- und Solemaschinen haben verschiedene Hilfsantriebe. Die Vorgabe ist so
  gesetzt, dass ein leeres Feld **keinen** Zuschlag bedeutet — damit keine geratene Zahl entsteht.
- **Optionen:**
  - **(a) Je Anlage** — eine Spalte je Wärmepumpentabelle; wer den Wert kennt, trägt ihn ein, wer
    nicht, bekommt keine erfundene Zahl.
  - **(b) Pauschal je Projekt** — eine Spalte in den Einstellungen; ein Wert für alle Maschinen,
    der für jede gleich ungenau ist und wie eine Kenngröße aussieht.
- **Empfehlung des Papiers:** **Je Anlage** — „er hängt an der Hydraulik der Maschine, nicht am
  Projekt; NULL = kein Zuschlag, damit keine geratene Zahl entsteht".
- **Folge bei Nichtentscheid:** Der Ablageort der Spalte ist offen — und damit auch, ob eine
  Kältemaschine der letzten Stufe später dieselbe Eingabe erbt.
- **Fällig vor:** **KU2**.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 5.0.5, 6.1, 7.3 und
  12.1 (K23).

---

## 7. Anlagenkopplung — H1 bis H12 (entschieden)

Quelle: [`Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md`](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md), Kapitel 13.1. Die zwölf Fragen sind am 16.09.2026 mit **E24**
(„H-Fragen sind entschieden — ok") sämtlich **nach Empfehlung des Papiers** entschieden, H1 mit **E25**
um die Wahl des Bandes ergänzt; die technischen
Festlegungen H-F1 bis H-F12 (13.2) sind damit zur Kenntnis genommen. Der Stufenplan bleibt **Q26**
(Kapitel 1). Die Nummer dieses Kapitels bleibt stehen, damit Verweise gelten; die ausführlichen
Erläuterungen stehen im Papier, hier nur der Entscheid je Frage.

| Nr. | Entscheid (E24, 16.09.2026) | Wirkt in |
|---|---|---|
| **H1** | P-Regler mit Proportionalband, Vorgabe 1 K; `Xp = 0` fällt bitgleich auf die ideale Regelung zurück; **E25:** Band wählbar 0,5 K, 1 K, 2 K oder frei (0 bis 5 K) | AK1 |
| **H2** | Heizkurve in AK1 außentemperaturgeführt; die raumgeführte Korrektur erst in AK3 | AK1 |
| **H3** | Übergabe je Zone ab G6, je Gebäude davor; ein Vorlauf je Gebäude; mehrere Heizkreise benannt abgelehnt | AK1, Wirkung ab G6 |
| **H4** | Profilweg in AK2, der Speicher als Vorrat über die Sperrdauer; echte Kopplung erst AK3 | AK2 |
| **H5** | Komfortschwelle 1,0 K in der Nutzungszeit (Eingabe mit Vorgabe); drei Zahlen: Unterschreitungsstunden, Kelvinstunden, längste Strecke | AK2 |
| **H6** | AK3 wird jetzt nicht zugesagt; der Entscheid fällt nach einer Feldphase von AK1 und AK2 | AK3 |
| **H7** | Die Kopplung wirkt in beiden Läufen der Verhältnisrechnung (E8); die Nennleistung der Übergabe folgt bei NULL der skalierten Auslegungslast; feste Nennleistung wird im Bericht benannt | AK1 |
| **H8** | Wochenprofil als Spalte je Gebäude (Sollwertprofil) bzw. je Anlage (Zeitprogramm), 168 Werte, strenger Parser; keine Profiltabelle | AK1, AK2 |
| **H9** | Kälteseite in AK1, wenn KU2 den Kühl-Vorlauf liefert; sonst benannt vertagt | AK1 |
| **H10** | Auslegungs-Außentemperatur aus der Klimareihe hergeleitet als Vorgabe, ein Feld überschreibt; Herleitung steht im Dialog | AK1 |
| **H11** | Die Dialoggruppe heißt „Wärmeübergabe"; die Wärmesenke behält „Heizkreis" | AK1 |
| **H12** | Leerer `Heizung_Strahlungsanteil` bedeutet künftig „Vorgabe der Übergabeart"; Glossar, Herleitungszeile, Datenbankfall | AK1 |

---

## 8. Zur Kenntnis — technische Festlegungen und Wiedervorlage

Die folgenden Punkte **verlangen keinen Entscheid**. Die Papiere beantworten sie selbst; sie stehen
hier, damit der Anwender **widersprechen** kann, nicht damit er entscheiden muss. Nur die Nummer,
die Festlegung in einem Satz und die Stufe, bis zu der ein Widerspruch noch billig ist — die
Begründung steht im jeweiligen Papier (Datenaustauschkonzept 11.2, Kühlkonzept 12.2, Wiedervorlage
des Systementwurfs).

**Festlegung aus E20 (16.09.2026), zu widersprechen bis zur Beauftragung von G1:** Der Gebäudedialog und
seine Nachbarn (Katalog-, Skalierungs- und Bedarfsdialog) sind in **VDI-6007-Struktur** aufgebaut, die
Modellparameter immer sichtbar und bearbeitbar; Felder, die nur der Altweg liest, stehen in einem
eingeklappten Abschnitt „Tagesbilanz (Bestandsweg)", der nur bei einem Gebäude auf dem Altweg erscheint
(dauerhaft, E23); der Schalter heißt „Rechenweg" mit Vorgabe „VDI 6007" und Wert „Tagesbilanz"; ein
Gebäude auf dem Altweg trägt im Bericht „Tagesbilanz (Bestandsweg)" statt des Produktausweises
(Konzept N1.25, [ADR-006](ADR-006_Trennung_Altweg_VDI6007.md)).

**Aus der Anlagenkopplung (E22, E24):** Die zwölf technischen Festlegungen **H-F1 bis H-F12** stehen im
[Papier](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) Kapitel 13.2 und sind mit E24 zur Kenntnis genommen. Dasselbe Papier
fand das Feld `Nutzungszeit` der Erzeuger ohne einen einzigen Leser im Rechenkern und legt fest, dass
AK2 es nicht wiederbelebt; ob es entfällt, ist ein gewöhnlicher Aufräumpunkt — wie die leserlosen Spalten `WW_Bedarf` und `Waermebedarf` aus Befund X (Q25 ist mit E23 entfallen).

### 8.1 Datenaustausch, technische Festlegungen (11.2)

Fundstelle: [`Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md`](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md),
Kapitel 11.2.

| Nr. | Gegenstand der Festlegung |
|---|---|
| **D3** | Schemakopie und Beispieldateien ohne Lizenz: Schema nicht ausliefern, nur als lokale Kopie im Test; die fremden Beispieldateien nicht ins Repositorium, Prüfdateien selbst erzeugen. **Ein Rest bleibt offen:** wo die Schemakopie liegt — im Testprojekt mit ausgeschriebener Begründung oder außerhalb mit benannt übersprungenem Prüftest; **zu entscheiden, bevor die Kopie committet wird** |
| **D7** | Die Round-Trip-Stufe zurückstellen, nicht streichen; die Persistenz der Zuordnung wird trotzdem in der Importstufe gebaut, weil sie auch die Herkunft trägt |
| **D8** | Je Format ein eigener Herkunftswert, nicht ein gemeinsamer mit dem Format eine Tabelle weiter |
| **D9** | Die Kennungsspalten formatfrei benennen, bevor sie entstehen; den Herkunftswertebereich erweitern und die Länge festlegen |
| **D10** | Ersatzschichtung beim Export mit Kennzeichnung, samt ausdrücklichem Vorbehalt: Sie trifft U-Wert und Gesamtwärmekapazität, **nicht die Lage der Masse im Aufbau** — wörtlich in der Bauteilbeschreibung der Datei und in der Meldung an den Anwender |
| **D12** | Der eine Luftwechselwert des Formats wird auf die Infiltrationsspalte gelegt, die Nutzerlüftungsspalte bleibt leer (= Wert des Gebäudes) |
| **D13** | Die erste gbXML-Zonenregel greift nur, wenn es weniger Zonen als Räume gibt — sonst entstünden aus einer Bürodatei über neunzig EPOS-Zonen |
| **D14** | Die Exporte erscheinen als Überlagerung im Gebäudedialog — kein neuer Menüpunkt, kein neuer Maskenschlüssel; zusätzlich ein Einstieg dort, wo die Ergebnisse liegen. **Siehe A17**, wo diese Festlegung einem Bestandsbefund widerspricht |
| **D15** | Größengrenze für gbXML, benannt abgelehnt statt versucht; **die iOS-Zahl ist zu messen** |

### 8.2 Kühlung, technische Festlegungen (12.2)

Fundstelle: [`Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md`](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md),
Kapitel 12.2.

| Nr. | Gegenstand der Festlegung |
|---|---|
| **K2** | Vorzeichen: Norm innen, Betrag außen — der Kanal führt positive Kältemengen. **Steht zusätzlich in Kapitel 0**, weil die Festlegung unwiderruflich in Persistenz und Kanalrechnung eingeht |
| **K3** | Eine externe Ganglinie darf den Kühlkanal tragen — Kältebedarf ohne Gebäudemodell |
| **K8a** | Umschaltung Heizen ↔ Kühlen **je Tag**, Mindestverweildauer ein Tag |
| **K8b** | **Eine** Teillastlogik für Wärme und Kälte, nicht zwei; die Kühlkennlinie zunächst über die höchste Laststufe — eine bewusste, benannte Vereinfachung |
| **K8c** | Keine Erdreichregeneration durch sommerliche Rückkühlung in der Erzeugerstufe — benannt vertagt |
| **K13** | Die Ergebnisspalten folgen dem Bestandsmuster der Kanalspalten |
| **K14** | Der Parser der Knappheitsreihenfolge wird tolerant, statt die gespeicherten Werte zu migrieren — ergebnisneutral |
| **K15** | **Eine** neue Kennzahlgruppe für die Erzeugergrößen der Kälte; die Kanalgrößen bleiben in der Kanalgruppe |
| **K16** | Die maximale Wärmelast bleibt **unberührt**; die Kälteseite bekommt ihre eigene Spitzen- und Bedarfsgröße. Die zwei Kanallisten bekommen eigene, nicht mit einer vorhandenen Kernklasse kollidierende Namen, und der Deckungsgrad der Kälteseite entsteht in einem **eigenen** Zweig statt als vierter Fall im Bestandszweig — „sonst legt der Kühlkanal jeden Wärmeerzeuger neu aus", und ein Deckungsgrad mit dem Wärmenenner lieferte eine Zahl statt eines Fehlers |
| **K17** | Eigener Reihenname für den Referenzlauf-Export, **bedingt** geschrieben: Der Vergleich kennt einen Schlüsselausschluss, aber keinen Dateiausschluss — eine Datei, die nur im neuen Lauf liegt, ist ein roter Vergleich ohne Schalter dagegen. Die bewusste Abweichung vom Bestandsmuster spart zwölf Reihen voller Nullen |
| **K18** | „—" statt 0 in Dialogen, Kacheln und Bericht; ein Projekt ohne Kühlung zeigt keine Kühlgruppe |
| **K18a** | Der Kältestrom reist zunächst als Skalar in der Kennzahlendatei, nicht als Ergebnisspalte je Anlage — **offen**, bis der Bericht sie verlangt |

### 8.3 Systementwurf, Wiedervorlage (Kapitel 11)

Fundstelle: [`Systementwurf_Gebaeudesimulation_EPOS-Plan.md`](Systementwurf_Gebaeudesimulation_EPOS-Plan.md),
Kapitel 11 („Was später zu überdenken ist"). Jeder Gegenstand trägt dort die Bedingung, an der er
hängt.

| Gegenstand | Hängt an |
|---|---|
| Auslegungsheizlast im Stundenmodell — eine Norm-Auslegungsheizlast ist eine andere Rechnung mit anderen Randbedingungen | einem eigenen Papier |
| Kühlung als eigener Kanal — **durch E12 überholt**, die Folgen regelt das Kühlkonzept | Erzeuger-, Speicher- und Wirtschaftlichkeitsrechnung |
| Die geerbten Textvergleiche im Umfeld des Gebäudemodells — benannter Bestand, **kein Umbauauftrag** | eine Altlastbehebung gehört nicht in einen Einfrierschritt, der dreizehn Projekte bewegt |
| Die zwei Projektbindungen des Gebäudes — wird benannt und dokumentiert; die Regel dazu ist festgeschrieben | — |
| Parallelität bei sehr großen Zonenprojekten | einem gemessenen Fall, der es verlangt |
| Ein eigenes Projekt für die Formatleser | dem iOS-Gerätebau (E18, **A2**) |
| Ob der Bauteilweg den Klassenweg vollständig ablöst | der Datenlage im Feld |
| Die dritte Umrechnungsnaht — benannt und in die Regel aufgenommen statt verschwiegen | der Einheitenregel des Kerns |
| Zwei Festlegungen von ADR-Gewicht ohne ADR: Ergebnisreihen als Datei statt als Tabelle, und ein Zuordnungsgerüst für beide Formate | Beauftragung von G4c bzw. des Ergebnisexports |
| Die Obergrenze von 50 Zonen — eine Setzung aus der Rechenzeit, kein Messergebnis (siehe **M12**) | einer Laufzeitmessung an einem echten Mehrzonengebäude |

---

## 9. Wie ein Entscheid festgehalten wird

**Zwei Orte, immer beide.** Der ausführliche Entscheid kommt als **Nachtrag N1.x** in
[`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
— mit dem Wortlaut des Anwenders, dem Entscheid, den berührten Stellen und, wo nötig, der
Aufhebung des bisherigen Stands; die **eine Zeile** je Entscheid kommt in Abschnitt 1 von
[`Status_Gebaeudesimulation_VDI6007.md`](Status_Gebaeudesimulation_VDI6007.md); eine Zeile offener
Konzeptfragen gibt es dort nicht mehr, seit Q11a mit E19 entschieden ist. Betrifft der Entscheid ein
Architekturpapier, wird dessen Fragentabelle im selben Schritt nachgezogen; betrifft er einen
**ADR**, wechselt dort die Kopfzeile **Status** von „Vorgeschlagen" auf „Angenommen" mit Datum,
und die Papiertabelle der Statusdatei sowie die Indexzeile in
[`Dokumentation/LIESMICH.md`](../LIESMICH.md) folgen.

**Dieses Register wird im selben Schritt gekürzt** — der entschiedene Punkt verschwindet hier und
steht fortan im Nachtrag und in der Statusdatei; ist der letzte Punkt eines Kapitels entschieden,
entfällt das Kapitel. Führt der Entscheid zu einer für den Anwender sichtbaren Funktionsänderung,
wird nach der Hausregel ein Eintrag im Wiki-Update-Logbuch **entworfen** und die Versionsnummer
beim Anwender erfragt; die Veröffentlichung läuft gebündelt.

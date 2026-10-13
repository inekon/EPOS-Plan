# Konzept: Zapfprofil — Abbrüche beseitigen, Benutzerführung und Handhabung verbessern

**Stand: entschieden (13.10.2026), Umsetzung in Wellen.** Abschnitt 1 hält den Befund fest, Abschnitt 2
das Zielbild, Abschnitt 3 die Vorschläge ZB1 bis ZB10, Abschnitt 4 die Entscheidungen ZB‑Q1 bis ZB‑Q5,
Abschnitt 5 die Wellen. Der Anwender hat am 13.10.2026 entschieden: Karten mit Zusammenfassung (ZB‑Q1 A),
Kalender aufklappbar im Zonenblock (ZB‑Q2 A), **beliebig viele** eigene Ferienzeiträume je Zone (ZB‑Q3 B,
mit Schemaschritt) und Katalogimport mit Paketvorlage für die DIN‑4708‑Werte (ZB‑Q4 A); ZB‑Q5 folgt der
Empfehlung (keine Vorbelegung). ZB1 (Abbrüche) und ZB7 sind in Arbeit.

**Anlass:** Anwenderauftrag vom 13.10.2026 mit Bildschirmfoto aus Visual Studio
(`ZapfprofilEingabeException` „Die Nutzungsart der Zone „Zone 1“ steht nicht (mehr) im Katalog — bitte
eine Nutzungsart wählen.“ in `ZapfprofilRechner.Rechnen`):

> Der Bereich Zapfprofil muss überarbeitet werden: 1. Kachel Brauchwasserwärme: Abbrüche im Dialog
> Zapfprofil erzeugen 2. Benutzerführung und Handhabung verbessern

Dazu drei Rückfragen und ein Wunsch des Anwenders am selben Tag, je mit Bildschirmfoto der Stufe
Erweitert: „Was bedeutet Testklasse? Wo wird die Ausstattungsklasse festgelegt?“, „Was bedeutet
Wohnungstyp hinzufügen?“ und „Der Kalender sollte ähnlich des Kalenders im Gebäudedialog sein. Aufruf
separater Dialog oder aufklappbar“.

**Geltungsbereich:** der Dialog `EPOS.UI/Dialoge/Bedarf/ZapfprofilDialog.razor` samt Hülle
`EPOS.UI.Daten/Bedarf/ZapfprofilHuelle*.cs`, die Kernschicht `EPOS.Kern/Allgemein/Zapfprofil/` und der
Katalog der Brauchwasser-Nutzungsarten, soweit er die Zonen speist. Nicht berührt: der Rechenweg
vollständiger Zonen, die Auslegung als Verfahren, die Referenzbasis.

**Grundlagen:** [`Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md`](Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md)
(§2.2 Weiche und Eskalation, §5 Dialog, N9, N13, N35–N38), die Bedienungsseite
`Projekte/Wiki/Programm Dokumentation - Brauchwasser-Zapfprofil.wiki`, der Gebäudedialog mit
Kalenderbedienung (`EPOS.UI/Dialoge/Bedarf/KalenderbedienungAbschnitt.razor`).


## 1 Befund

### 1.1 Abbrüche (B1)

„Zone hinzufügen…“ (`ZapfprofilDialog.ZoneNeu`) legt eine Zone mit `IdNutzungsart = 0` und ohne
Bezugsmenge an und stößt die entprellte Vorschau an. Der Vorschauweg
`ZapfprofilHuelle.Vorschau` → `BedarfsVorschauCtrl.ProjektVorschau` →
`SimulationWaermebedarf.BrauchwasserAusGenerator` → `ZapfprofilCtrl.Rechnen` →
`ZapfprofilRechner.Rechnen` sucht die Nutzungsart, findet keine und **wirft**
`ZapfprofilEingabeException(NutzungsartFehlt)`; dieselbe Methode fängt sie zwei Blöcke tiefer und lässt
die Zone 0 tragen. Ohne Debugger bleibt davon eine gelbe Meldung am Zonenblock; Visual Studio hält mit
„Bei Auslösen unterbrechen“ an **jedem** Wurf an — bei jeder Eingabe, weil die Vorschau je Eingabe
rechnet. Das ist der „Abbruch“.

Dieselbe Bauart steckt in 59 Wurfstellen des Kerns; im Bearbeitungsfluss erreichbar sind unter anderem ein
neuer Wohnungstyp ohne Anzahl (`EINGABE_WOHNUNGSTYP_ANZAHL`), ein Wohnungstyp ohne bestimmbare Belegung
(`BelegungFehlt`), ein halb eingetragener Ferienzeitraum, ein Jahresmesswert ohne Einheit
(`MesswertUngueltig`) und eine Zapftemperatur unter dem Kaltwasser (`TemperaturUngueltig`).
Fachlich ist das richtig — die Zone trägt 0 mit benanntem Grund (Umsetzungskonzept §2.2, N8 (b)) —, nur
der Weg dorthin ist eine Ausnahme statt einer Prüfung.

Die Meldung selbst führt in die Irre: Eine neue Zone hatte nie eine Nutzungsart; „steht nicht (mehr) im
Katalog“ trifft nur eine Zone, deren Nutzungsart wirklich verschwunden ist.

### 1.2 Messung am Dialog

Sichtprobe des echten Dialogs mit der echten Hülle auf einer Kopie der Testdatenbank (Projekt 1045,
Chromium, Fensterbreite 1 500 px):

| Stufe | Höhe der Seite | davon Vorschau (rechte Spalte) |
|---|---|---|
| Einfach | 1 147 px | rund 750 px |
| Erweitert | 5 656 px | rund 1 050 px |
| Experte | 7 706 px | rund 1 050 px |

In den Stufen Erweitert und Experte trägt die linke Spalte alles — Zonenliste, Zonenblock und die
Gebäudewerte —, und die Vorschau rollt nach dem ersten Bildschirm aus dem Blick. Wer einen Wert ändert,
sieht seine Wirkung nicht.

### 1.3 Befunde der Bedienung

| Nr. | Befund | Beleg |
|---|---|---|
| B2 | **Neue Zone ohne Führung.** Nach „Zone hinzufügen…“ steht „Zone 2“ mit leerer Nutzungsart, die Liste zeigt „0,0 · trägt 0“ und der Block eine gelbe Warnung. Der nächste Schritt („Nutzungsart wählen, Bezugsgröße eingeben“) steht nirgends; die Auslassungspunkte versprechen einen Dialog, der nicht kommt. | Sichtprobe „neue Zone“ |
| B3 | **Zone und Gebäude vermischt.** Die Gruppe „Tagesbedarf, Ladeleistung, Zirkulation“ führt den Tagesbedarf der Zone neben Ladeleistung und Zirkulation des Gebäudes; „Fachwerte · Gebäude“ steht in Experte unter der Stochastik am Ende der Zonenspalte. Was für die markierte Zone gilt und was für alle, ist nur am Klammerzusatz „(Gebäude)“ zu erkennen. | Stufen Erweitert, Experte |
| B4 | **Senkrechte Optionsgruppen für zwei bis drei Werte.** Stufe, Bedarfsniveau, Zirkulation ja/nein, Einheit des Messwerts, dreimal auto/manuell und der Rechenweg der Jahresreihe belegen je drei Zeilen à 48 px. | alle Stufen |
| B5 | **Der Dreiklang auto/manuell braucht sieben bis neun Zeilen je Wert:** Überschrift doppelt („Tagesbedarf · Wohnen“, „Tagesbedarf“), Umschalter, „Vorschlag: …“, „Als manuellen Wert übernehmen“, Feld, „Wirkt nur bei „manuell““, „Angesetzt: …“, „Rechenweg: 8,3 P × 1,619 kWh/(P·d) × f_θ 1,021 = …“. | Erweitert, Gruppe Tagesbedarf |
| B6 | **Kalender als sechzehn Einzelfelder.** Vier Ferienzeiträume mit je Beginn Tag, Beginn Monat, Ende Tag, Ende Monat untereinander; das Feld Bundesland ist gesperrt, sein Hinweis `ZPG_GRUND_BUNDESLAND` („Wirkt erst mit einer Kalendertabelle je Bundesland; bis dahin …“) ist überholt — das Feiertagsland des Gebäudes wirkt längst. Der Gebäudedialog führt dieselben Ferien als Liste benannter Zeiträume mit Datumsfeldern, dazu Wochenende, Feiertagsland und ein Jahresraster. | Erweitert; Wunsch des Anwenders |
| B7 | **Wohnungstabelle unverständlich.** Der Anwender fragt, was „Wohnungstyp hinzufügen“ bedeutet. Die Erklärung steht nur in Kürzeln (`ZPG_HINW_WOHNUNGSTABELLE`: „Personen leer = Belegung nach Raumzahl aus dem Katalog, sonst …; Ausstattung leer = Vorgabeklasse“). Dass die Tabelle die Bezugsgröße ersetzt und die Bedarfskennzahl nach DIN 4708 speist, sagt die Maske nicht. | Rückfrage des Anwenders |
| B8 | **DIN‑4708‑Katalogwerte ohne Pflegeweg.** Ausstattungsklassen und Belegung je Raumzahl (`Tab_TwwDin4708Wert_STAMM`) lassen sich im Programm weder bearbeiten noch einspielen: Der Katalogimport kennt sieben andere Tabellen, der freie Paketteil bringt als Normzahlen keine mit, allein `Werkzeuge/Auslieferungsvorlage --katalogpaket` legt sie in eine Neuinstallation. Folge: In jeder echten Installation ist die Klappliste leer, die Belegung nach Raumzahl findet nichts, der Normvergleich nach DIN 4708 ist nicht rechenbar. In der Testdatenbank stehen die erfundenen Klassen „Testklasse A/B“ (Katalogversion `TEST-1`, Herkunft fiktiv) — der Anwender hat sie gesehen und nach ihrer Bedeutung gefragt. | Rückfrage des Anwenders; Code `TwwNutzungsartCtrl.Import`, `TwwKataloge.PAKETREIHENFOLGE` |
| B9 | **Erklärtexte als Dauerzustand.** Fast jedes Feld trägt eine Zeile „Vorgabe · …“ mit Formelzeichen, Normbezug und Fachbegriffen („Bilanzgrenze des Kennwerts“, „f_θ“); in der Stufe Einfach stehen Hinweise, mit denen ein Einsteiger nichts tun kann („Zirkulation ohne Fläche“, „Zone außerhalb der Zirkulation“). | alle Stufen |
| B10 | **Doppelte Wahl der Nutzungsart.** In Erweitert klappt unter der Klappliste der ganze Katalog als Tabelle mit Wahlknöpfen auf (sechs Spalten, eigener Rollbalken). | Erweitert |
| B11 | **Anzeigeeinstellungen im Fachblock.** „Temperatur der Literanzeige“ und „Schwelle der Stundenzählung“ stehen in „Fachwerte · Gebäude“, werden aber nicht gespeichert; seit #986 sind sie Rubrik „Zapfprofil“ im Einstellungsdialog. | Experte |


## 2 Zielbild

- **L1 Kein Wurf im Bearbeitungsfluss.** Unvollständig ist kein Fehler: Eine Zone, der noch etwas
  fehlt, rechnet nicht mit und sagt, was fehlt — ohne Ausnahme, ohne gelbe Warnung.
- **L2 Der nächste Schritt ist sichtbar.** Eine neue Zone führt über Nutzungsart und Bezugsgröße zur
  ersten Zahl; erst dann öffnen sich die weiteren Angaben.
- **L3 Zwei Ebenen, sichtbar getrennt:** *Zone* (Nutzung, Belegung, Kalender, Messwert, Tagesbedarf,
  Fachwerte der Zone) und *Gebäude und Anlage* (Zirkulation, Ladeleistung und Ladefenster, Kaltwasser
  und Speichertemperatur der Auslegung, Jahresreihe).
- **L4 Die Vorschau bleibt im Blick:** Die rechte Spalte haftet beim Rollen.
- **L5 Kompakt ohne kleinere Bedienelemente:** Eine Wahl aus zwei bis drei Werten steht waagerecht; ein
  Wert mit Vorgabe ist **ein** Feld — leer heißt Vorgabe, die Vorgabe steht grau im Feld (Hausregel
  „NULL = Vorgabe“, Umsetzungskonzept §3.1). Berührungsziele bleiben 44 px.
- **L6 Erklärung auf Abruf:** Rechenweg, Herkunft und Normbezug stehen hinter einem Aufklapper oder
  der Hilfepille, die Grundansicht spricht Klartext.
- **L7 Gleiche Dinge sehen gleich aus:** Der Kalender der Zone nimmt die Bausteine des Gebäudedialogs.


## 3 Vorschläge

| Nr. | Vorschlag | Wirkung | Aufwand |
|---|---|---|---|
| **ZB1** | **Abbrüche beseitigen** (beauftragt, in Arbeit): Der Kern prüft je Zone vorab und ohne Ausnahme, was der Rechenweg ablehnen würde — Nutzungsart nicht gewählt oder nicht im Katalog, Tagesgangsatz fehlt, Bezugsmenge fehlt, Wohnungstyp ohne Anzahl oder Belegung, halber Ferienzeitraum, unvollständiger Messwert, ungültige Temperaturen und manuelle Werte. Die Zone trägt 0 mit derselben benannten Ablehnung wie heute; eine Regel je Fall, die Wurfstellen bleiben Netz für Unerwartetes. Zwei Sätze: „Für die Zone „…“ ist noch keine Nutzungsart gewählt.“ und der bisherige für eine verschwundene Nutzungsart. Auslegung und Ladevorschlag ebenso. Wache: Zwischenstände der Bearbeitung ohne einen einzigen Wurf (`FirstChanceException`). | B1 | mittel |
| **ZB2** | **Neue Zone geführt.** Der Knopf heißt „Zone hinzufügen“ (ohne Auslassungspunkte). Die neue Zone steht in der Liste als „unvollständig“ statt „0,0 · trägt 0“, der Fokus springt auf die Nutzungsart, und eine leise Zeile im Block nennt den nächsten Schritt („Nutzungsart wählen“ → „Bezugsgröße eingeben“). Eine unvollständige Zone erzeugt keine Warnung in „Hinweise der Bilanz“. | B2 | klein |
| **ZB3** | **Aufbau in zwei Ebenen.** Unter der Zonenliste der Block der markierten Zone als aufklappbare Karten mit Zusammenfassungszeile (Baustein `Klappgruppe`): *Nutzung* (offen) · *Belegung* · *Kalender* · *Messwert* · *Tagesbedarf* · *Fachwerte der Zone*. Daneben bzw. darunter eine eigene Gruppe **Gebäude und Anlage** mit Zirkulation, Ladeleistung samt Ladefenster, Kaltwasser und Speichertemperatur der Auslegung und Jahresreihe (Stochastik). Die Stufen Einfach · Erweitert · Experte bleiben und bestimmen, welche Karten es gibt; eine zugeklappte Karte zeigt in ihrer Kopfzeile die wirksamen Werte, etwa „Belegung · 24 WE aus der Wohnungstabelle · Speicher · Zirkulation ja“. Die Vorschau haftet rechts (L4). Zwei Anordnungen zur Wahl: ZB‑Q1. | B3, 1.2 | groß |
| **ZB4** | **Kompakte Bedienelemente.** (a) `Optionsgruppe` erhält eine waagerechte Form (wie `Mehrfachauswahl.Waagerecht`) für Stufe, Niveau, ja/nein, Einheit, Rechenweg. (b) Der Dreiklang auto/manuell wird **ein** Feld: leer = auto, der Vorschlag steht grau im Feld, ein eingetippter Wert ist manuell; daneben „angesetzt 13,7 kWh/d“ und ein Aufklapper „Rechenweg“. „Als manuellen Wert übernehmen“ entfällt (der Vorschlag lässt sich übernehmen, indem man ihn tippt; wahlweise ein kleiner Knopf „übernehmen“ neben dem Feld). | B4, B5 | mittel |
| **ZB5** | **Kalender der Zone wie im Gebäudedialog** (Wunsch des Anwenders): eine Karte „Kalender“, zugeklappt mit Zusammenfassung („wie Gebäude „Haus A“ · 3 Ferienzeiträume · Feiertage Baden-Württemberg“ bzw. „Kalender der Nutzungsart · keine Ferien“). Aufgeklappt: Quelle (Nutzungsart oder Gebäude), die Ferien als Liste mit Datumsfeldern „von“ und „bis“ (Baustein `Gemeinjahrdatum` wie im Gebäudedialog), „+ Ferien“ und „Entfernen“, bei gebundenem Gebäude dessen Wochenende, Feiertagsland und Ferien als Text mit dem Vermerk „aus dem Gebäude“, darunter das Jahresraster der Zone (Werktag, Samstag, Sonn- und Feiertag, Ferien). Das gesperrte Feld Bundesland entfällt; das Feiertagsland nennt die Zusammenfassung. Anordnung: ZB‑Q2, Zahl der Zeiträume: ZB‑Q3. | B6 | mittel bis groß |
| **ZB6** | **Wohnungstabelle verständlich.** Kopf „Wohnungen (wahlweise)“ mit einem Satz: „Statt einer Bezugsgröße die Wohnungen nach Typ angeben; die Zone rechnet dann mit ihrer Summe, und die Auslegung bildet daraus die Bedarfskennzahl nach DIN 4708.“ Spaltenköpfe mit Einheit und Hilfe („Raumzahl — Wohn- und Schlafräume, Küche zählt nicht“); unter der Tabelle die Summenzeile „ergibt 24 Wohneinheiten · 52 Personen“. Die Spalte Ausstattungsklasse steht nur, wenn der Katalog Klassen führt; sonst eine leise Zeile mit dem Weg (ZB7). | B7 | klein |
| **ZB7** | **Pflegeweg für die DIN‑4708‑Katalogwerte.** Der Anwender trägt Belegung je Raumzahl und Ausstattungsklassen aus seinem eigenen Normexemplar ein — über den Katalogimport (wahlfreie Datei `Tab_TwwDin4708Wert_STAMM.csv` und eine Paketvorlage mit Platzhaltern wie `Vorlage\Katalogpaket_A100`) oder über einen kleinen Editor im Katalog der Nutzungsarten. Keine Werte im Repositorium, Herkunft `IMPORT` bzw. `EIGENKONSTRUKTION`. Weg: ZB‑Q4. | B8 | mittel |
| **ZB8** | **Texte und Hinweise.** Die Zeilen „Vorgabe · …“ wandern in den Platzhalter des Felds und in die Hilfepille; Formeln nur im Aufklapper Rechenweg; der überholte `ZPG_GRUND_BUNDESLAND` entfällt mit ZB5; in der Stufe Einfach stehen nur Hinweise, die der Anwender dort beheben kann, die übrigen ab Erweitert. Alle Sätze bleiben `ZapfSatz` bzw. Ressource in beiden Sprachen. | B9 | klein bis mittel |
| **ZB9** | **Katalogtabelle aus dem Zonenblock nehmen.** Die Klappliste bleibt (mit Bezugsart im Eintrag); die Tabelle des Katalogs öffnet ein Knopf „Katalog…“ als Blatt, nur lesend, mit „Wählen“. | B10 | klein |
| **ZB10** | **Anzeigeeinstellungen aus dem Fachblock.** Literanzeige und Stundenschwelle stehen nur noch im Einstellungsdialog (Rubrik „Zapfprofil“); im Fachblock ein Verweis. | B11 | klein |

**Was bleibt, wie es ist:** der Rechenweg und seine Ablehnungen, Schreibweg und OK-Prüfung,
Blattwechsel aus dem Bedarfsprofil-Dialog (N35/N36), die Überlagerungen Auslegung, Messdaten und die
Editoren, die Stufen als Begriff, der Zähler „n Werte überschrieben“, die Referenzbasis. Einen
Schemaschritt bringt allein ZB5 mit der Entscheidung ZB‑Q3 B: eine Tabelle der Zonenferien (je Zone
beliebig viele Zeiträume mit Reihenfolge, Beginn und Ende als Tag des Gemeinjahrs, wahlfrei einem Namen),
in die der Schritt die vier vorhandenen Spaltenpaare von `Tab_TwwZone` überträgt; Lesen, Speichern,
Projektkopie, Projekttransfer und der Zapfkalender gehen danach über die Liste. Die Ergebnisse bleiben
gleich, solange eine Zone nicht mehr als vier Zeiträume trägt.


## 4 Entscheidungen

| Nr. | Frage | Möglichkeiten | Empfehlung | Entscheid (13.10.2026) |
|---|---|---|---|---|
| ZB‑Q1 | Anordnung des Zonenblocks (ZB3) | **A** aufklappbare Karten untereinander mit Zusammenfassungszeile · **B** Reiter je Thema (Nutzung · Belegung · Kalender · Messwert · Fachwerte) über dem Block | **A** — die Zusammenfassungszeilen zeigen alle wirksamen Werte auf einen Blick, Reiter verstecken sie; A passt zur Klappgruppe der Konditionierung | **A** |
| ZB‑Q2 | Kalender der Zone (ZB5) | **A** aufklappbare Karte im Zonenblock · **B** eigenes Blatt „Kalender der Zone…“ | **A** — die Zusammenfassung reicht meist; das Jahresraster rollt in seinem Kasten. B nur, wenn das Raster mit voller Breite gebraucht wird | **A** |
| ZB‑Q3 | Zahl der eigenen Ferienzeiträume einer Zone | **A** höchstens vier wie heute (ohne Schemaschritt) · **B** beliebig viele (Schemaschritt, eigene Tabelle) | **A** — eine gebundene Zone erbt ohnehin alle Ferien ihres Gebäudes; vier eigene Zeiträume decken Nichtwohnzonen ohne Gebäude | **B** — beliebig viele, wie im Gebäudedialog |
| ZB‑Q4 | Pflegeweg der DIN‑4708‑Werte (ZB7) | **A** Katalogimport mit wahlfreier Datei und Paketvorlage · **B** Editor im Katalog · **C** beides | **A** — derselbe Weg wie die A100-Typen, kein neuer Editor; ein Editor kann folgen | **A** |
| ZB‑Q5 | Vorbelegung einer neuen Zone | **A** ohne Vorbelegung, Fokus auf die Nutzungsart · **B** Nutzungsart der markierten Zone übernehmen | **A** — eine geerbte Nutzungsart rechnet sofort und unbemerkt falsch; „Duplizieren“ bleibt der Weg für eine gleiche Zone | nach Empfehlung **A** (nicht eigens gefragt) |


## 5 Wellen

| Welle | Inhalt | Abnahme |
|---|---|---|
| W1 | ZB1 | Kern-Filter grün, Wache ohne Wurf, Referenzlauf PASS ohne neue Basis |
| W2 | ZB2, ZB4 (a), ZB6, ZB8, ZB10 — Führung, waagerechte Wahl, Texte | bunit je Zustand, `StilblattTests`, Rasterprobe für die Wohnungstabelle (N13 (u): sie rollt im Block noch quer) |
| W3 | ZB5 Kalender der Zone: Schemaschritt Zonenferien (ZB‑Q3 B), Lesen, Speichern, Projektkopie und -transfer, Zapfkalender über die Liste; Karte mit Ferienliste und Jahresraster | Schemakette und Testdatenbank über das Schemawerkzeug (vorher den Nachbarsitzungen gemeldet), bunit, Kalenderprobe im Browser, Referenzlauf PASS ohne neue Basis |
| W4 | ZB3, ZB4 (b), ZB9 — Aufbau in Karten und Gebäudegruppe, haftende Vorschau, ein Feld je Wert | bunit, Fensterprobe, Sichtprobe der drei Stufen; Höhe der Stufe Erweitert unter der Hälfte von heute |
| W5 | ZB7 Pflegeweg DIN 4708 | Importtests mit erfundenen Werten, Bericht je Tabelle, „Nur prüfen“ |
| W6 | Wiki-Seite „Brauchwasser-Zapfprofil“ und Logbuch | Wache `WikiProduktdatenWacheTests`, Gegenlesen nach Konzept Hilfesystem 13 |

Jede Welle endet mit Statuszeile, Protokoll unter `Dokumentation/ueberholt/Protokolle/Zapfprofilgenerator/`
und Push; die Sichtabnahme unter Windows bleibt beim Anwender.

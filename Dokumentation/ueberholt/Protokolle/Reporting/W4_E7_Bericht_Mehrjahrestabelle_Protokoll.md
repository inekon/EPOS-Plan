# Etappe E7 — Bericht (Word und Excel), Mehrjahrestabelle

**Ausbaustufe W4, Etappe 7. Umgesetzt am 19.08.2026.**
Ausgangsstand `3e1e34b` (E6), Arbeitsbaum sauber. Grundlage der Etappe ist die Ist-Analyse
des Berichtsteils („E7-Vorbefund", 19.08.2026) mit ihren sechs belegten Divergenzen zwischen
Word und Excel und der Liste „gerechnet, aber nicht ausgegeben".

**Ergebniswirkung: keine.** 216 von 216 Simulations-CSV sind byte-identisch zur Basis
`2026-08-19_B5`, und **864 von 864** verglichenen Wirtschaftlichkeitswerten (neun Projekte ×
drei Szenarien × alle Skalarfelder) sind gegen den Vorgängerstand **unverändert**. E7 ist
Ausgabe; die einzige Änderung nahe am Rechenweg ist der Rückgabekanal, und der ist rein
additiv.

---

## 1 Worum es ging

Der Bericht rechnete seit Phase 9 mehr, als er zeigte. Vier benannte Erlösreihen entstanden
jahresscharf, wurden in eine Summe eingerechnet und **verworfen** — vom KWK-Zuschlag überlebte
allein der Wert des ersten Jahres. Elf Betriebskostenpositionen mit Kostenart, Bemessungsart
und Herleitung standen seit E3 in der Datenbank und erschienen nirgends. Die Herkunft der
Steuersätze stand nur im Ergebnisreiter, nicht im Bericht — obwohl der Bericht das Dokument
ist, das den Rechtsstand gegenüber Dritten nachweist. Und dieselbe Kennzahlenliste stand
**dreimal** im Code.

Der Engpass war dabei **strukturell, nicht kosmetisch**: `KapitalwertRechner.Zahlungsbild`
transportierte die Jahresreihen nicht zurück. Eine Mehrjahrestabelle nach Positionen war mit
dem Datenfluss vor E7 nicht baubar — es fehlte kein Formatierer, sondern ein Kanal.

---

## 2 Was jetzt anders ist

### 2.1 Der Rückgabekanal (Punkt 1 der Priorisierung)

`KapitalwertRechner.Zahlungsbild` führt seit E7 sechs zusätzliche Felder: `BetriebJeJahr`,
`EnergieJeJahr`, `BehgJeJahr`, `ErsatzJeJahr`, `EinspeiseerloesJeJahr` und die Liste der
benannten Erlösreihen, wie sie hereingereicht wurde. `VerlaufSerie` trägt das ganze
Zahlungsbild mit (`VerlaufSerie.Bild`); bis E7 überlebte aus `BerechneVerlauf` nur die
kumulierte Summe.

**Der Rechenweg ist Zeichen für Zeichen unverändert.** Der Ausdruck für `ausgaben` behält
insbesondere `(energieJahr + behgJahr)` als **eine** Klammer; die getrennten Reihen daneben
sind Ausweis und gehen **nicht** in die Summe ein. Wären sie es, verschöbe sich das Ergebnis
in der letzten Stelle. Der Kommentar an der Stelle sagt das ausdrücklich, damit die nächste
Überarbeitung die Klammer nicht „aufräumt".

Kein Persistenzproblem: `BerechneVerlauf` baut je Projekt ohnehin eine frische
`ProjektEingabe` samt aller Reihen und warf sie weg. Der Kanal reicht bis `VerlaufSerie`, nicht
in die Datenbank.

### 2.2 Die Mehrjahrestabelle (Punkt 2)

Neu in **Word und Excel**, je Projekt eine Tabelle. Zeilen sind die Jahre 0…T, Spalten die
Positionen des Zahlungsstroms — so herum, weil bei T = 20 einundzwanzig Jahresspalten auf A4
nicht darstellbar sind und weil der Kapitalwert-Verlauf im Excel-Bericht es seit Phase 11
bereits so macht.

Spalten: Investition und Ersatz · Betriebskosten · Energiekosten · CO₂-Abgabe ·
Einspeiseerlös · KWK-Zuschlag · Energiesteuer-Gutschrift · Stromsteuer-Befreiung ·
Stromsteuer-Entlastung · Netto nominal · Barwert · Kumuliert. Eine Spalte ohne einen einzigen
Betrag entfällt — dieselbe Konvention wie bei den Kennzahlzeilen („nie 0-Zeilen").

**Vorzeichen: Ausgaben negativ, Einnahmen positiv.** Dadurch ist die Summe der
Positionsspalten die Spalte „Netto nominal", und die Tabelle prüft sich selbst. Die
Abschlusszeile trägt den Restwert-Barwert im Jahr T und schließt die kumulierte Spalte auf den
Nettobarwert auf; darunter steht die Probe im Klartext.

**Drei Dinge werden erst dadurch sichtbar** — gemessen an Projekt 1030 (siehe Abschnitt 4.2):

1. **Das Auslaufen des KWK-Zuschlags.** 44.265 € im Jahr 1, dann die degressive Vbh-Staffel
   (41.409 · 38.554 · 35.698 …), im Jahr 12 nur noch 18.563 € — und **ab Jahr 13 null**, weil
   das 30.000-Stunden-Kontingent erschöpft ist. Im bisherigen „KWKG-Erlös Jahr 1" war davon
   nichts zu sehen; ein Leser musste annehmen, der Zuschlag laufe zwanzig Jahre.
2. **Die Steuergutschriften verlaufen flach** (21.599 / 28.565 / 61.150 € über alle
   zwanzig Jahre) — das belegt die Aussage aus E4, dass die Sätze auf dem heutigen
   Rechtsstand ab 2026 konstant sind, und die Tabelle trägt jede künftige Novelle ohne Umbau.
3. **Die auseinanderlaufenden Preissteigerungssätze.** Betriebskosten +1,5 %/a
   (20.000 → 26.539 €), Energiekosten +2,0 %/a (1.485.561 → 2.164.182 €). Dazu wird die
   Ersatzbeschaffung im Jahr 15 (−295.000 €) als das sichtbar, was sie ist.

**Vermiedene Kosten und Aufschlagsbetrag bekommen keine eigene Zahlungszeile** (Punkt 3).
Beide stecken bereits in anderen Positionen — die Einsparung in der kleineren Bezugsmenge, der
Aufschlag in den Energiekosten. Sie stehen unter der Tabelle als ausdrücklich beschrifteter
**Nachweisblock** mit dem Satz „Die folgenden Beträge stehen bewusst NICHT in der Tabelle
darüber und dürfen nicht zu ihr addiert werden."

### 2.3 Die Kennzahlentabelle: eine Definition statt dreier (Punkte 7 und 8)

Neu: `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitZeilen.cs` (469 Zeilen). `WirtZeile` trägt
Schlüssel, Titel, Format, Excel-Format, Wert- oder Textzugriff und die Stammspalten-Anzeige;
`WirtschaftlichkeitZeilen.Kennzahlen(menge, tarif)` baut die sichtbare Liste. Word-Baustein,
Excel-Generator und Ergebnisreiter **rendern nur noch**.

Alle Zeilen laufen über `MyResource.Resource.WIRT_ZEILE_*` — keine Ausnahme. Das Schema folgt
dem Vorschlag der Ist-Analyse: `<Fachbegriff><, Qualifizierer>  [<Einheit>]`, Einheit in
eckigen Klammern, Komma statt Halbgeviertstrich, **kein „Jahr 1" im Zeilentitel**. Der
Zeitbezug steht einmal über der Tabelle (`WIRT_ZEILE_JAHR1`) — erst dadurch passt derselbe
Schlüssel in Kennzahlen- **und** Mehrjahrestabelle. Aus „KWKG-Erlös Jahr 1" wird
„KWK-Zuschlag [€/a]", gleichlautend mit dem Reihennamen `WIRT_REIHE_KWKG`, den die
Mehrjahrestabelle daneben stellt.

**Die Titel dürfen nicht mehr durch `BerichtTexte.T()` laufen.** `WordKontext` hat dafür
`MitStilRoh` und eine `Zelle`-Überladung mit `uebersetzen = false` bekommen. Bisher lief
`BerichtTexte.T(z.Label)` auch über bereits lokalisierte Titel; das war harmlos, solange kein
deutscher `MyResource`-Wert im Wörterbuch steht — eine Falle, die jetzt zu ist.

### 2.4 Die vier bisher toten `WIRT_REIHE_*`

Sie waren seit E4 in beiden Sprachen gepflegt und wurden von keinem Codepfad gelesen. Sie sind
jetzt die Spaltenköpfe der Mehrjahrestabelle — genau der Ort, für den sie angelegt wurden.

### 2.5 Der KWK-Zuschlag je Modul (E6, Übergabepunkt 1)

Neu in beiden Ausgaben: eine **Tabelle** mit einer Zeile je BHKW-Modul — Bezeichner,
elektrische Leistung, Vollbenutzungsstunden, beide Sätze, **Herkunft des Satzes** (Anlage oder
Projekt), Jahresdeckel, Kontingent, Förderbeginn, Zuschlag im Jahr 1 und das Jahr, ab dem das
Kontingent erschöpft ist. Bis E7 stand dieselbe Auskunft als Aufzählung in einer Hinweiszeile,
die bei drei Modulen unlesbar wird.

Darunter steht **die Herleitung des angesetzten Satzes nach § 7 KWKG** je Modul, aus derselben
Tranchenrechnung, die auch der Modul-Dialog zeigt. Das ist mehr als Kosmetik: An Projekt 1030
macht sie sichtbar, dass der Katalog für die 250-kW-Anlage auf **Eigenstrom keinen Zuschlag**
vorschlägt (kein Tatbestand des § 6 Abs. 3 erfasst), während der Projektsatz von 4,00 ct/kWh
angesetzt wird — eine Abweichung, die vorher nirgends stand. Und sie zeigt die Tranchen
(„50 kW × 8,00 + 50 kW × 6,00 + 150 kW × 5,00 → Mischsatz 5,80 ct/kWh") statt einer
Leistungsklasse.

Die Daten wandern über `WirtschaftlichkeitErgebnis.KwkgModule` (neue Klasse
`KwkgModulNachweis`). **Nicht persistiert** — wie in E6 festgehalten entsteht die Reihe je
Anlage bei jedem Lauf neu; der Bericht rechnet ohnehin frisch, nur der Rückfallpfad auf den
gespeicherten Stand zeigt den Block nicht.

### 2.6 Betriebskosten nach Kostenarten (Punkt 9)

Neu in beiden Ausgaben, gegliedert nach `Kostenart` (VDI 2067: kapital-, bedarfs-,
betriebsgebunden, sonstige, nicht eingeordnet), je Position mit Gruppe, **Bemessungsart** und
der Herleitung **Menge × Einheitpreis**. Das ist der ausdrückliche Zweck, für den Etappe E3 die
Spalte angelegt hat (E3-Protokoll, Restbefund 3).

Gelesen wird — wie von E3 verlangt — **direkt auf `Tab_ProjektWerte`**, nicht über
`Abfrage_Kostenfaktoren`: Die gespeicherte Access-Abfrage liegt außerhalb des Repos und kennt
die fünf Spalten aus Migrationsschritt 19 nicht. Der Positionsname kommt über `StammID` aus
`Tab_Kostenfaktor`.

**Zwei Leseschleifen, eine Probe.** Die Summenschleife `LiesBetriebskosten` ist der Rechenweg
und wurde in E3 gegen die Referenz gestellt; sie umzubauen, damit sie nebenbei eine Liste
füllt, hieße den Rechenweg für eine Ausgabe anzufassen. `LiesBetriebskostenPositionen` rechnet
mit **denselben Regeln** (`BetriebskostenCtrl.Betrag`, dieselbe Szenarienvorfahrt), liefert
aber nur Beschreibung. Weicht ihre Summe von den angesetzten Betriebskosten ab, **sagt der
Bericht das** (`WIRT_BK_ABWEICHUNG`) — statt zwei Zahlen nebeneinanderzustellen und zu
schweigen.

### 2.7 Die sechs Divergenzen der Ist-Analyse

| # | Befund | Stand nach E7 |
|---|---|---|
| D1 | Tarifnachweis fehlt in Excel | **behoben** — `tarifP.Nachweis(…)` steht jetzt in der Nachweiszeile beider Ausgaben (Punkt 12). |
| D2 | `e.Hinweis` erscheint in Excel nirgends | **behoben** — eigener Block „Nachweise und Hinweise". Darin stehen sämtliche Begründungen aus E2 bis E6: warum eine Gutschrift 0 ist, welcher Aufschlagssatz angesetzt wurde, welche Anlage am Stichtag scheitert (Punkt 4). |
| D3 | Aktualitätswarnung `ErgebnisAktuell` fehlt in Excel | **behoben** — dieselbe Prüfung, derselbe Text (`WIRT_ERGEBNIS_VERALTET`) in beiden Ausgaben. |
| D4 | Excel zeigt alle drei Szenarien voll, Word nur „Erwartet" | **bleibt, bewusst.** Word ist ein Dokument, Excel eine Datenablage; drei volle Kennzahlenblöcke in Word wären drei Seiten mit fast identischen Zahlen. Word führt daneben die schmale Szenarienübersicht. |
| D5 | Stammspalte: Word „—", Excel leer, Reiter „(Referenz)" | **auf zwei Fälle reduziert und begründet.** Word und Reiter zeigen jetzt beide `WIRT_ZEILE_STAMM_REFERENZ` („(Referenz)"); Excel lässt die Zelle **leer**, weil die Wertspalten numerisch bleiben müssen — sonst sind Autofilter und Diagramme des Blattes hinüber. Der Unterschied steht jetzt an **einer** Stelle im Code, statt dreimal zufällig zu entstehen. |
| D6 | `daten.Warnungen` nur in Word | **behoben** — die Warnungen des Berichtslaufs stehen jetzt auch im Excel-Nachweisblock. |

Eine weitere Divergenz ist bei der Umstellung **entstanden und wieder beseitigt** worden: Die
KWKG-Modultabelle hatte in Word zunächst neun, in Excel elf Spalten. Sie führen jetzt beide elf.

### 2.8 Die falsch beschriftete Reststromzeile (Punkt 6)

„Stromkosten Tarif [€/a]" trug im ROLLEN-Modus `r.Reststrom.SummeEur`, also die Kosten **mit**
Anlage — und stand damit direkt neben den vermiedenen Kosten, die sich auf den Bezug **ohne**
Anlage beziehen. Der Titel hängt jetzt am Tarifmodus: `WIRT_ZEILE_STROMKOSTEN_RESTSTROM`
(„Reststromkosten nach Tarif") im Rollenmodell, `WIRT_ZEILE_STROMKOSTEN_BEZUG`
(„Strombezugskosten nach Tarif") im Zonenmodell. Beide Titel sind fachlich richtig; einer für
beide Modelle war es nicht.

### 2.9 Einspeiseerlös aufgeschlüsselt (Punkt 10)

`EinspeiseerloesJahr` verschmolz PV-Überschuss und KWK-Einspeisung zu einer Zahl — zwei Mengen
mit zwei Preisen und zwei Rechtsgrundlagen. Neu sind `EinspeiseerloesPvJahr` und
`EinspeiseerloesKwkJahr` (persistiert über `SpalteSicher`, wie die E4-Ergebnisspalten). Die
Zerlegung erscheint nur, wenn **beide** Anteile vorkommen — bei einem reinen PV- oder reinen
KWK-Projekt wäre sie die Gesamtzeile ein zweites Mal.

Der Split entsteht auf drei Wegen, und die Herkunft ist jeweils benannt: im Flat-Pfad exakt aus
den beiden Produkten; im Zonenmodell über einen eigenen PV-Erlös (`StromMatrix.EinspeiseerloesPv`)
mit dem KWK-Anteil als **Rest**, damit die Summe ohne Rundungsrest stimmt; im Rollenmodell
**mengenproportional**, weil das Modell nur **einen** Einspeisetarif kennt — das ist eine
Annahme und steht als solche im Code.

Der Satz `Einspeiseverguetung_KWK` erscheint im Nachweis. *Anmerkung zur Ist-Analyse:* Sie
führt ihn als „erscheint in keinem Nachweis"; tatsächlich stand er seit E5 in
`WirtschaftlichkeitParameter.Nachweis` und damit in beiden Berichten. Der Befund war insoweit
nicht zutreffend.

### 2.10 `BedarfMWh` in der Strommengen-Matrix (Punkt 11)

Seit E5 gerechnet und persistiert, in beiden Matrixblöcken ungenutzt. Jetzt eine fünfte
Mengenspalte „Bedarf ohne Anlage [MWh]" mit dem Hinweis, dass eine Null in allen Zonen „die
Strombedarfsreihe fehlte im Lauf" bedeutet — nicht „kein Bedarf".

---

## 3 Entscheidungen dieser Etappe

- **Jahre als Zeilen, Positionen als Spalten.** Nicht nur ein Layoutgriff: Bei T = 20 passen
  21 Jahresspalten nicht auf A4, und der vorhandene Kapitalwert-Verlauf im Excel-Bericht macht
  es seit Phase 11 bereits so. Damit brauchen Word und Excel **kein zweites Layout**.
- **Vorzeichen nach Zahlungswirkung.** Ausgaben negativ, Einnahmen positiv. Die Alternative
  („alles positiv, Kosten und Erlöse getrennt lesen") hätte die Selbstprüfung der Tabelle
  gekostet: So ist die Summe der Positionsspalten die Nettospalte, und ein Fehler in einer
  Position fällt sofort auf.
- **Spalten ohne Betrag entfallen, Summenspalten bleiben.** Dieselbe Konvention wie bei den
  Zeilen. Ohne sie hätte ein Projekt ohne BHKW vier leere Erlösspalten getragen.
- **In Word ist die Mehrjahrestabelle schmaler gesetzt** (7 pt statt 9 pt,
  `WordBerichtGenerator.SCHRIFT_TABELLE_SCHMAL`). Mit dreizehn Spalten auf 165 mm bleiben je
  Spalte rund 13 mm; bei 9 pt bräche jeder siebenstellige Betrag um. Die übrigen Tabellen
  bleiben unverändert bei 9 pt.
- **Excel lässt die Stammzelle einer Differenzkennzahl leer, Word schreibt „(Referenz)".**
  Der eine bewusst verbliebene Unterschied (D5). Ein Text in einer Zahlenspalte macht
  Autofilter und Diagramme des Blattes unbrauchbar.
- **Word zeigt jede Zeile der Definition und schreibt „—", Excel lässt vollständig leere
  Zeilen weg.** Auch das ist eine Regel, nicht ein Zufall: Im Dokument ist „—" die Aussage
  „gerechnet, aber nicht bestimmbar"; in der Datenablage wäre eine leere Zeile Ballast.
  Gemessen an Projekt 1024 sind das sechs Zeilen Unterschied — **alle ohne Zahlenwert**.
- **Zwei Schlüssel für die Stromkostenzeile** statt eines neutralen Titels. Ein Titel, der in
  beiden Tarifmodellen passt, hätte entweder gelogen („Stromkosten") oder nichts gesagt.
- **Der Betriebskostenblock rechnet nicht mit, er beschreibt** — und meldet, wenn seine Summe
  von der angesetzten abweicht.
- **Die KWKG-Modulzeilen und die Betriebskostenpositionen werden nicht persistiert.** Sie
  entstehen bei jedem Lauf neu, der Bericht rechnet ohnehin frisch. Persistenz hätte zwei
  neue Ergebnistabellen gekostet, für einen Nutzen, den nur der Rückfallpfad hätte.
- **Die Ergebnisspalten des Einspeise-Splits gehen über `SpalteSicher`**, nicht über einen
  Migrationsschritt — dasselbe Muster, das dieses Modul seit W1 für
  `Tab_ErgebnisWirtschaftlichkeit` geht (zuletzt die vier E4-Spalten). Ein Migrationsschritt
  wäre der dritte Mechanismus für **eine** Tabelle.

---

## 4 Verifikation

### 4.1 Übersicht

| # | Prüfung | Methode | Ergebnis |
|---|---|---|---|
| V1 | Build | `MSBuild WP-Plan.sln -p:Platform=x86`, Ausgabe ausschließlich in den Scratch-Ordner | **0 Fehler, exakt 6 Warnungen** (CS0108 ×2, CS0109 ×2, CS1998, CS4014) — der Bestandssatz, keine siebte |
| V2 | Simulationsergebnisse gegen die Basis | `Referenzlauf.exe vergleich 2026-08-19_B5 …`, feste Projektliste, Arbeitskopie-Mechanismus | **9/9 PASS**, 2 366 177 Werte |
| V3 | Byte-Identität | `cmp` je Datei gegen B5 | **216/216 gleich**, 0 Abweichungen — zweimal gemessen (nach dem ersten Umbau und mit dem Endstand) |
| V4 | Plausibilität | `Referenzlauf.exe pruefen` | **GESAMT plausibel** |
| V5 | **Wirtschaftlichkeit A/B** | Harnisch auf `BerichtsDatenSammler.SammleFuerBericht`, je eigene Wegwerf-DB, 9 Projekte × 3 Szenarien, alle öffentlichen Skalarfelder per Reflexion | **864 von 864 Werten identisch**, 0 Abweichungen. Nur in B: die beiden neuen Felder des Einspeise-Splits |
| V6 | Berichtslauf Word **und** Excel | zwei Projekte (1030 Mehrmodul-BHKW mit KWK-Zuschlag und Steuergutschriften, 1024) auf einer präparierten Wegwerf-Kopie | vier Dateien erzeugt, alle neuen Blöcke gefüllt |
| V7 | **Wertgleichheit Word ↔ Excel, gemessen** | Kennzahlentabelle beider Ausgaben extrahiert und zeilenweise verglichen | Projekt 1030: **15 gemeinsame Zeilen, alle in Label und Wert zeichengleich**; Projekt 1024: **5 von 5**. Nur-in-Word-Zeilen: 3 bzw. 6, **alle ohne Zahlenwert** („—" / „(Referenz)") |
| V8 | Mehrjahrestabelle Word ↔ Excel | dieselbe Methode, 22 Zeilen | **22 von 22 zeichengleich**; einziger Unterschied ist die Darstellung leerer Zellen in der Abschlusszeile („—" gegen leer) |
| V9 | Probe der Mehrjahrestabelle | Abschlusszeile gegen die Kennzahl | −25 902 958 + 111 658 = **−25 791 300** = Nettobarwert über T ✓ |
| V10 | Probe des Betriebskostenblocks | Summe der Positionen gegen `BetriebskostenJahr` | 18 000 + 2 000 = **20 000** = angesetzte Betriebskosten ✓ |
| V11 | **Sprachprobe `en-US`** | derselbe Berichtslauf mit `Program.nLanguage = 1` und `CurrentUICulture = en-US` | Alle neuen Blöcke **englisch**; Zahlen wertgleich, Format nach Kultur (1.485.561 ↔ 1,485,561; 0,282 ↔ 0.282). Keine deutschen Reste in den neuen Blöcken — die verbliebenen deutschen Zeichenketten sind **Datenwerte** (Positionsbezeichnungen, Kostengruppen, Zonennamen) und **Bestandsliterale außerhalb dieser Etappe** |
| V12 | Ressourcen in beiden Sprachen und im Designer | Zählung je Schlüssel | **81 neue Schlüssel** in `Resource.resx`, `Resource.en-US.resx` und `Resource.Designer.cs`, je genau einmal; **7 geänderte Werte** in beiden Sprachen |
| V13 | Kodierung und Zeilenenden | `file`, CR-Zählung gegen Zeilenzahl, Suche nach U+FFFD | 13 Dateien, **alle UTF-8**, **alle durchgehend CRLF**, **0 Ersatzzeichen** |
| V14 | Produktivdatenbank nur gelesen | jeder Lauf auf einer Kopie; Zielprüfung im Harnisch („ABBRUCH, wenn der Pfad auf `%ProgramData%` zeigt") | keine `Kenndaten.laccdb` vor und nach den Läufen; **kein Schreibzugriff** auf die produktive Datei |
| V15 | `bin\` des Repos unberührt | jeder Build ausschließlich mit `-p:OutDir=<Scratch>` | erfüllt |

### 4.2 Der Wirkungsbeleg an Projekt 1030

Für den Berichtslauf wurde auf einer **Wegwerf-Kopie** Projekt 1030 um die Angaben ergänzt, die
E4 und E5 eingeführt haben (produzierendes Gewerbe, § 53a, Nutzungsgrad 85 %, Hocheffizienz und
räumlicher Zusammenhang, Aufschläge an, KWK-Einspeisevergütung 8 ct/kWh). Ohne diese Angaben
sind die Gutschriften vorschriftsgemäß 0 — die neuen Blöcke wären leer geblieben. Die
produktive Datenbank ist davon nicht berührt.

Auszug der Mehrjahrestabelle (Werte in €, Ausgaben negativ):

| Jahr | Invest./Ersatz | Betrieb | Energie | CO₂ | Einspeisung | **KWK-Zuschlag** | Energiest. | Stromst. Befr. | Stromst. Entl. | Netto nominal | Barwert | Kumuliert |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 0 | −410.000 | — | — | — | — | — | — | — | — | −410.000 | −410.000 | −410.000 |
| 1 | — | −20.000 | −1.485.561 | −58.386 | 26.135 | **44.265** | 21.599 | 28.565 | 61.150 | −1.382.234 | −1.341.975 | −1.751.975 |
| 2 | — | −20.300 | −1.515.272 | −59.554 | 26.135 | **41.409** | 21.599 | 28.565 | 61.150 | −1.416.269 | −1.334.969 | −3.086.944 |
| 4 | — | −20.914 | −1.576.489 | −61.960 | 26.135 | **35.698** | 21.599 | 28.565 | 61.150 | −1.486.217 | −1.320.485 | −5.735.231 |
| 12 | — | −23.559 | −1.847.108 | −72.596 | 26.135 | **18.563** | 21.599 | 28.565 | 61.150 | −1.787.252 | −1.253.543 | −15.955.073 |
| **13** | — | −23.912 | −1.884.051 | −74.048 | 26.135 | **—** | 21.599 | 28.565 | 61.150 | −1.844.563 | −1.256.057 | −17.211.131 |
| 15 | **−295.000** | −24.635 | −1.960.166 | −77.039 | 26.135 | — | 21.599 | 28.565 | 61.150 | −2.219.393 | −1.424.544 | −19.881.275 |
| 20 | — | −26.539 | −2.164.182 | −85.058 | 26.135 | — | 21.599 | 28.565 | 61.150 | −2.138.331 | −1.183.942 | −25.902.958 |
| **Restwert (Barwert) in T** | | | | | | | | | | | **111.658** | **−25.791.300** |

Die Nullen ab Jahr 13 sind die Kernaussage. Beide Module erschöpfen ihr 30.000-Stunden-Kontingent
im selben Jahr — die Modultabelle nennt es je Modul in der Spalte „Kontingent erschöpft ab Jahr".

Die Modultabelle desselben Laufs:

| Modul | el. Leistung | Vbh | Satz Eigen | Satz Einsp. | Satz aus | Jahresdeckel | Kontingent | Förderbeginn | Zuschlag Jahr 1 | erschöpft ab |
|---|---|---|---|---|---|---|---|---|---|---|
| BHKW EW M 50 S [K] Erdgas | 50 kW | 7.476 h/a | 4,00 | 8,00 | Projekt | Staffel § 8 | 30.000 h | 2027 | 7.377 € | Jahr 13 |
| Agenitor 306(250kw.el) Gas | 250 kW | 5.385 h/a | 4,00 | 8,00 | Projekt | Staffel § 8 | 30.000 h | 2027 | 36.888 € | Jahr 13 |

Darunter die Herleitung: Für das 50-kW-Modul greift **§ 7 Abs. 3a** (16,00 / 8,00 ct/kWh, die
Sonderregel geht den Leistungsanteilen vor); für das 250-kW-Modul schlägt der Katalog auf
Eigenstrom **0 ct/kWh** vor (kein Tatbestand des § 6 Abs. 3 erfasst) und auf Einspeisung
5,80 ct/kWh nach Tranchen. Angesetzt sind beide Male die Projektsätze 4,00/8,00 — die Tabelle
sagt jetzt, dass sie aus dem **Projekt** stammen, und die Herleitung sagt, was der Katalog
stattdessen vorschlagen würde.

---

## 5 Übergabepunkte der Vorgängeretappen

| Herkunft | Punkt | Stand |
|---|---|---|
| E3, Restbefund 3 | „`Kostenart` hat noch keine Oberfläche. Ihr Zweck ist die Gliederung des Berichts in Etappe E7." | **erledigt** (2.6) |
| E3, Restbefund 6 | „`Abfrage_Kostenfaktoren` kennt die neuen Spalten nicht — direkt auf `Tab_ProjektWerte` lesen." | **beachtet** (2.6) |
| E4, Übergabepunkt 6 | „Der Wirtschaftlichkeitsnachweis mischt jetzt Sprachen … Der Block gehört als Ganzes über `MyResource` gezogen." | **erledigt** (2.3) |
| E4, Übergabepunkt 7 | „`KennzahlenKatalog` führt die Gutschriften nicht." | **bewusst offen** — Punkt 13 der Priorisierung: vermischt zwei Lebenszyklen, Architekturentscheidung, keine Berichtsaufgabe |
| E4, Übergabepunkt 8 | „Die benannten Reihen sind noch nirgends einzeln sichtbar; die Mehrjahrestabelle ist der vorgesehene Ort." | **erledigt** (2.2, 2.4) |
| E5, Übergabepunkt 6 | „Die vermiedenen Kosten sind Ausweis, nicht Zahlungsstrom — die Mehrjahrestabelle muss das kenntlich machen." | **erledigt** (2.2, Nachweisblock; Titelzusatz „(Ausweis)") |
| E5, Übergabepunkt 7 | „Der Aufschlagsbetrag steckt in den Energiekosten … im Bericht sollte es beschriftet werden." | **erledigt** (Titelzusatz „(in Energiekosten enthalten)" und Nachweisblock) |
| E5, Übergabepunkt 8 | „`Tab_ErgebnisStromMatrix` führt jetzt `BedarfMWh`, aber keine Lastbilder." | `BedarfMWh` **erledigt** (2.10); Lastbilder **bewusst offen** (Punkt 14 der Priorisierung) |
| E6, Übergabepunkt 1 | „Die Modulaufzählung im Hinweisfeld ist eine Notlösung … In den Bericht gehört das als Tabelle." | **erledigt** (2.5) |
| E6, Übergabepunkt 2 | „Der KWK-Zuschlag wird weiterhin nur als Jahr-1-Wert persistiert; für eine Mehrjahrestabelle je Modul wäre eine Ergebnistabelle nötig." | **teilweise**: Die Mehrjahrestabelle zeigt den KWK-Zuschlag **als Projektsumme** je Jahr — dafür genügt der Rückgabekanal. Eine Mehrjahrestabelle **je Modul** bleibt offen und bräuchte weiterhin eine Ergebnistabelle |
| E6, Übergabepunkt 3 | „`KWKGVbhElektrisch` bleibt die projektweite, leistungsgewichtete Größe." | **entschärft, nicht behoben**: Die Modultabelle stellt die Vbh **je Anlage** daneben. Die Kennzahlzeile bleibt die projektweite Größe, heißt jetzt aber ausdrücklich „Vollbenutzungsstunden elektrisch, KWKG-Basis" |

---

## 6 Bewusst nicht getan

Die Punkte 13 bis 16 der Priorisierung bleiben nach Auftrag liegen:

13. `KennzahlenKatalog` an die Wirtschaftlichkeit anschließen.
14. Lastbilder persistieren.
15. Gesamtlokalisierung des Berichtsmoduls (82 + 18 Literale, `BerichtTexte` als Ganzes). E7
    stellt die **Kennzahlentabelle vollständig** um und legt die **neuen** Blöcke lokalisiert
    an; der übrige Bestand bleibt unangetastet.
16. `DefaultThreadCurrentUICulture` setzen.

Dazu aus eigener Entscheidung:

- **Der Szenarienumfang in Word bleibt, wie er ist** (Divergenz D4, Begründung in 2.7).
- **Keine Mehrjahrestabelle je Modul** (E6, Übergabepunkt 2) — sie bräuchte eine
  Ergebnistabelle und damit einen Migrationsschritt in einer Ausgabe-Etappe.

---

## 7 Offene Punkte

### Bestandsbefund, ausdrücklich benannt statt nebenbei behoben

1. **Die Meldung „Differenzdiagramm entfällt — für das Stammprojekt konnte keine Zahlungsreihe
   gerechnet werden" ist falsch, wenn es gar keine Varianten gibt.** `SchreibeVerlauf` prüft
   `verlauf.Differenz.Any(s => s.Kumuliert != null)`; bei einer Gruppe ohne Varianten ist
   `Differenz` **leer**, und der Bericht behauptet daraufhin ein Problem beim Stamm, das nicht
   besteht. Der Befund stammt aus Phase 11 und ist von E7 **nicht** verändert worden; er ist im
   Berichtslauf zu Projekt 1030 (Stamm ohne Varianten) sichtbar geworden. Die Berichtigung ist
   eine Zeile — sie gehört aber in einen eigenen Vorgang, weil sie den Verlaufsblock betrifft
   und nicht die Ausgabe, um die es hier ging.

### Für die Abnahme (E8)

2. **Der Ergebnisreiter ist nicht interaktiv geprüft worden.** Er rendert seit E7 dieselbe
   Zeilenliste wie Word und Excel (`WirtschaftlichkeitZeilen.Kennzahlen`), und diese Liste ist
   über beide Berichtsausgaben gemessen. Die reiterspezifischen Zeilen sind die Grid-Befüllung
   und die neue Tarif-Zwischenspeicherung. Ein Klickpfad über den Reiter gehört in die Abnahme.
3. **Die Referenzmenge deckt die Mehrjahrestabelle nur an einem Projekt ab.** Nur 1030 führt
   einen gepflegten KWKG-Satz; im übrigen Bestand steht `KWKG_Bonus` auf 0. Die Steuerangaben
   mussten für den Nachweis eigens gesetzt werden. Für eine dauerhafte Regressionsabdeckung des
   Berichtsteils fehlt ein Referenzprojekt mit gepflegten E4- und E5-Angaben.
4. **Der Nachweisblock erscheint nur, wenn vermiedene Kosten oder ein Aufschlagsbetrag
   vorliegen.** Bei inaktivem Tarif und ausgeschaltetem Aufschlagsschalter — dem Vorgabezustand
   — fehlt er ganz. Das ist gewollt (nie leere Blöcke), heißt aber: Der Hinweis „diese Beträge
   gehören nicht in die Summe" erscheint erst, wenn es die Beträge gibt.
5. **Die mengenproportionale Aufteilung des Einspeiseerlöses im Rollenmodell ist eine
   Annahme** (2.9). Sie ist im Code benannt; exakt wäre sie nur mit getrennten Tarifen für PV
   und KWK, und die kennt das Rollenmodell nicht.
6. **Ein neuer Basis-Freeze steht weiterhin aus** (E8). `2026-08-19_B5` bleibt gültig: 216 von
   216 CSV sind byte-identisch, E7 hat den Rechenkern nicht angefasst.

---

## 8 Was E7 angefasst hat

| Datei | Art |
|---|---|
| `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitZeilen.cs` | **neu** — Zeilendefinition und Mehrjahresbild (469 Zeilen) |
| `Allgemein/Wirtschaftlichkeit/KapitalwertRechner.cs` | Rückgabekanal am `Zahlungsbild`, rein additiv |
| `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitDaten.cs` | `VerlaufSerie.Bild`; vier neue Ergebnisgrößen; `KwkgModulNachweis`, `KostenPositionNachweis` |
| `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` | Einspeise-Split, Modulnachweis, `LiesBetriebskostenPositionen`, zwei Ergebnisspalten |
| `Allgemein/Wirtschaftlichkeit/StromMatrix.cs` | `EinspeiseerloesPv` |
| `Allgemein/Bericht/WordBerichtGenerator.cs` | `MitStilRoh` und die `Zelle`-Überladung (Übersetzung aus, Schriftgröße) |
| `Allgemein/Bericht/Bausteine/BausteineWirtschaftlichkeit.cs` | vier neue Blöcke, Kennzahlentabelle auf die zentrale Definition |
| `Allgemein/Bericht/ExcelBerichtGenerator.cs` | dieselben vier Blöcke, D1/D2/D3/D6, Kennzahlentabelle |
| `Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.cs` | dritte Kopie der Zeilenliste entfernt |
| `MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` | 81 neue, 7 geänderte Schlüssel |
| `Allgemein/Simulation/Lokalisierung_Katalog.md` | Nachtrag zu E7 |

Der Rechenkern (`Allgemein/Simulation/*`, `Allgemein/BhkwPlan.cs`) ist **unberührt**.

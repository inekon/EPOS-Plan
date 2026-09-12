# K5 · Komponentenkatalog, Zuschuss-Positionsart und Gruppierung (HF5)

> Etappe K5 des Konzepts „Aktualisierung der Kosten- und Energieträgerstruktur in EPOS-Plan"
> (`Konzept_Kosten_Energietraeger_EPOS-Plan.md`, § 7 = HF5).
> Ausgangsstand `dbeed7f` (K4-Nachtrag), Schemastand 26.
> Zwei Commits: **K5a** `918f8f5` (Datenmodell und Rechenweg), **K5b** (Oberfläche, dieser Commit).
> Bearbeitet am 20.08.2026.

Kurzfassung: Die drei Erfassungsgruppen aus BHKW-Plan — Wärmezentrale, Bauliche Anlagen,
Stromeinspeisung — stehen als Kostenkomponenten samt Positionskatalog in der Datenbank.
Der Investitionszuschuss ist eine eigene Positionsart, die die Anfangsauszahlung
**einmalig** mindert und in Reiter, Word und Excel als negative Zeile erscheint. Der
Investitionsreiter fasst seine Positionen unter einklappbare Gruppenköpfe mit Zähler und
Summe. Die Empfehlungsbereiche des Konzepts sind **nicht** neu angelegt worden — sie waren
bereits da; die Begründung steht in Abschnitt 4.

---

## 1 Schemabefund — was `Tab_KostenKomponente` und `Tab_Kostenfaktor` wirklich führen

Vor der ersten Zeile Seed-Code gemessen, an einer Scratch-Kopie von
`Referenzlaeufe\Arbeitskopie\Kenndaten.accdb`. Die Bestandsaufnahme (§ 8.2) hatte beide
Spaltenlisten als „unvollständig" markiert; hier ist die vollständige.

| Tabelle | Spalte | Typ | AutoWert? |
|---|---|---|---|
| `Tab_KostenKomponente` | `ID` | LONG | **nein** |
| | `Komponente` | TEXT(255) | — |
| `Tab_Kostenfaktor` | `StammID` | LONG | **nein** |
| | `Bezeichnung` | TEXT(255) | — |
| | `IsMainComponent` | YESNO | — |
| `Tab_KostenGruppenKatalog` | `ID`, `GruppenName` | LONG, TEXT(255) | nein |
| `Tab_KostenKategorie` | `KategorieID`, `KategorieName` | LONG, TEXT(255) | nein |

Bestand: 7 Komponenten (IDs 1–7), 19 Katalogpositionen (7 Haupt = StammID 77–84,
12 Neben = 74–94).

### 1.1 Der Katalog ist FLACH — die wichtigste Feststellung der Etappe

`Tab_Kostenfaktor` führt **keine Spalte, die eine Position an eine Komponente bindet.**
Die Auftragszeile „Kläre dabei, wie Kostenfaktoren einer Komponente zugeordnet sind" hat
damit eine unerwartete Antwort: **gar nicht** — jedenfalls nicht im Katalog.

Die Zuordnung entsteht erst je Projekt, in `Tab_ProjektWerte.KomponentenID`. Belege:

- `KostenPositionCtrl.StammIdNeben` (`Controller\KostenPositionCtrl.cs:170-195`) sucht eine
  Nebenposition **allein über `Bezeichnung` + `IsMainComponent = False`** — ohne jeden
  Komponentenbezug.
- `Form_KostenfaktorItem` (`Views\Kosten\Form_KostenfaktorItem.cs:30-34`) füllt seine
  Auswahlliste mit `select * from Tab_Kostenfaktor where IsMainComponent=false` — der
  Anwender sieht beim Anlegen einer Position **alle** Nebenpositionen, gleich welcher
  Komponente.
- `Form_Kosten.LoadKostenFaktoren` (`:989-990`) verbindet Katalog und Komponente über
  `Abfrage_Kostenfaktoren`, und die joint über `Tab_ProjektWerte`.

**Folge für die Seeds:** Die Gruppierung „Wärmezentrale → BHKW-Einbindung,
Heizungstechnik, Abgasanlage" aus § 7.3 ist ein **Katalogvorschlag**, keine
Fremdschlüsselbeziehung. Sie steht deshalb als Datenstruktur im Code
(`SchemaKatalog.Schritt27_Erfassungsgruppen`) und erzeugt in der Datenbank je Bezeichnung
**eine** Zeile. „Sonstiges" kommt in allen drei Gruppen vor und entsteht darum **einmal**.

### 1.2 Zwei Positionen gab es schon

`Schornstein` (StammID 90) und `Abgasanlage` (91) stehen bereits im Bestandskatalog. Der
Seed prüft auf die Bezeichnung und lässt sie unangetastet — das ist zugleich seine
Idempotenz.

### 1.3 Berichtigung eines Kommentars im Bestand

`KostenPositionCtrl.cs:29` behauptet: „`Tab_Kostenfaktor.StammID` ist ein AutoWert". Das
ist **falsch** (Messung oben). `Form_KostenAdmin.btnNeuKostenfaktor_Click`
(`Views\Kosten\Form_KostenAdmin.cs:61`) rechnet mit `GetMaxID + 1` und hat recht; das
`INSERT` ohne `StammID` in `StammIdNeben` (`:183-186`) schreibt in Wahrheit eine 0. Der
Befund ist in `SchemaKatalog.TAB_KOSTENFAKTOR` festgehalten; **repariert wurde er in
dieser Etappe nicht** — er ist älter als K5 und gehört in eine eigene Runde (Abschnitt 9).

---

## 2 Schrittnummer 27

`SchemaMigration.ZIEL_VERSION` stand auf 26 (Schritt 26 = K3/M-B, Einheiten-Seeds). Die
nächste freie Nummer ist damit **27**, wie erwartet. `ZIEL_VERSION` ist auf 27 gehoben,
der Schritt ist als letzter Eintrag in `SCHRITTE` registriert
(`Allgemein\Update\SchemaMigration.cs`).

Es ist ein reines DML auf zwei **Katalog**tabellen. Keine Projektzeile wird angefasst,
kein DDL, kein Vorgabewert per DDL.

---

## 3 Migrationsschritt 27 — die Seeds

### 3.1 Komponenten (27a)

| Komponente | Herkunft | Technik-Planwert |
|---|---|---|
| Wärmezentrale | neu, BHKW-Plan | nein (Erfassungsgruppe) |
| Bauliche Anlagen | neu, BHKW-Plan | nein |
| Stromeinspeisung | neu, BHKW-Plan | nein |

**Nahwärmenetz ist nicht dabei** (Entscheidung E2 vom 19.08.2026): Verteilnetz,
Hausanschluss und Hausstation entfallen ersatzlos. **Der Pufferspeicher wird in der
Wärmezentrale nicht gedoppelt** (Entscheidung E1) — er bleibt die eigene Komponente 6 mit
Planwert-Anbindung. Das weicht bewusst von der Alt-Bemessung ab, die ihn in die
Wärmezentrale rechnete (Konzept Anhang A(b), Position 5).

### 3.2 Hauptpositionen (27b)

Je neue Komponente **eine** Zeile in `Tab_Kostenfaktor` mit `IsMainComponent = True` und
demselben Wortlaut wie die Komponente. Ohne sie fände `KostenPositionCtrl.StammIdHaupt`
nichts, und `Form_Kosten.EnsureMainComponentExists` bräche wortlos ab (`:1156-1157`:
`if (stammID <= 0) return;`). Dasselbe Muster wie im Bestand — dort heißen
`Tab_KostenKomponente` 1–7 und `Tab_Kostenfaktor` 77–84 paarweise gleich.

### 3.3 Nebenpositionen (27c) — Original-Beschriftungen aus Anhang A(a)

| Erfassungsgruppe | Positionen | im Bestand vorhanden |
|---|---|---|
| Wärmezentrale | BHKW-Einbindung · Heizungstechnik · Abgasanlage · Sonstiges | Abgasanlage (91) |
| Bauliche Anlagen | Heizraum · Schornstein · Bauliche Maßnahmen · Heizöllagerung · Erdgasanschluss · Sonstiges | Schornstein (90) |
| Stromeinspeisung | Stromeinspeisung · Sonstiges | — |

„Sonstiges" steht in jeder Gruppe (Katalogmuster geprüft: die Altmaske führte je Gruppe
drei frei benennbare Zeilen, und der Betriebskostenkatalog hat mit `DbWerte.VDI_POS_SONSTIGE`
sein Gegenstück). Wegen des flachen Katalogs entsteht es **einmal**; weitere freie
Positionen legt `StammIdNeben` beim ersten Bedarf selbst an.

„Stromeinspeisung" steht zweimal in `Tab_Kostenfaktor` — einmal als Haupt-, einmal als
Nebenposition. Das ist kein Konflikt: Beide Lesewege unterscheiden ausdrücklich über
`IsMainComponent`, und die Idempotenzprüfung prüft dieselbe Merkmalskombination.

### 3.4 Die Alt-Mengenlogik braucht keine neue Mechanik

„Heizraum = spez. Kosten €/m³ × Raumbedarf" bildet die vorhandene Bemessung
`Menge × Einheitpreis` ab (`Tab_ProjektWerte.Bemessung/Menge/Einheitpreis`, Schritt 19).
Nichts hinzugefügt.

---

## 4 Empfehlungsbereiche — Zielort-Entscheid: **weder noch, sie waren schon da**

Der Auftrag stellte zwei Möglichkeiten zur Wahl: Spalten `Empfehlung_von`/`Empfehlung_bis`
an `Tab_Kostenfaktor` (so § 7.6) **oder** Konstanten am VDI-Katalog in
`BetriebskostenCtrl`. Der Befund ist eine dritte Antwort:

**Die Konstanten stehen bereits im VDI-Katalog, mit exakt den sieben Wertepaaren des
Konzepts**, und sie werden bereits angezeigt.

| Position | § 7.6 | `BetriebskostenCtrl.Katalog` | Zeile |
|---|---|---|---|
| Instandhaltung BHKW | 3,0–9,0 % | `EmpfehlungVon = 3.0, EmpfehlungBis = 9.0` | `Controller\BetriebskostenCtrl.cs:161` |
| Instandhaltung Heizkessel | 1,5–2,5 % | 1.5 / 2.5 | `:169` |
| Instandhaltung Wärmezentrale | 1,8–2,2 % | 1.8 / 2.2 | `:177` |
| Instandhaltung bauliche Anlagen | 1,0–1,5 % | 1.0 / 1.5 | `:185` |
| Instandhaltung Stromeinspeisung | 1,8–2,2 % | 1.8 / 2.2 | `:193` |
| Personalkosten | 1,0–4,0 % | 1.0 / 4.0 | `:201` |
| Steuern/Versicherung/Verwaltung | 0,8–2,0 % | 0.8 / 2.0 | `:209` |

Die Anzeige gibt es ebenfalls schon: `Form_Betriebskosten.Bezugstext`
(`Views\Kosten\Form_Betriebskosten.cs:438-460`) hängt den Empfehlungsbereich über
`MyResource.Resource.VDI_EMPFEHLUNG` an die Bezugsgrößen-Beschriftung **neben dem
Satz-Feld** — genau die im Auftrag verlangte Stelle. Gebaut wurde beides in Etappe W4/E3.

**Entscheid: keine Datenbankspalten.** Zwei Spalten neben den Konstanten wären eine zweite
Wahrheit über dieselbe Zahl, und zwar die schlechtere: Die Empfehlungsbereiche der
VDI 2067 sind Normwerte und dürfen nicht je Datenbank abweichen. Ein Migrationsschritt,
der sie einsät, lädt genau dazu ein. Der § 7.6 war als „optionaler Teil" gekennzeichnet;
die Option ist eingelöst, ohne dass etwas gebaut werden musste.

**Nachprüfbar so:** Kostenmaske → Reiter Betriebskosten → BHKW →
„Betriebskosten VDI 2067…". Rechts neben jedem Satzfeld steht die Bezugsgröße und
dahinter der Bereich.

### 4.1 Offener Nebenbefund: die Bezugsgröße dieser drei Positionen

`BetriebskostenCtrl.cs:130-141` begründet ausführlich, warum sich „Instandhaltung
Wärmezentrale/bauliche Anlagen/Stromeinspeisung" an der **Gesamt**investition bemessen:
„EPOS-Plan führt keine solchen Gruppen". **Seit Schritt 27 führt es sie.** Die Bezugsgröße
könnte damit auf die jeweilige Komponente verengt werden.

Das ist hier **bewusst nicht** geschehen: Es wäre eine stille Ergebnisänderung an jedem
Bestandsprojekt, das eine dieser Positionen gepflegt hat. Sie gehört entschieden, nicht
nebenbei gemacht (Abschnitt 9).

---

## 5 Zuschuss als Positionsart (§ 7.4, L7)

### 5.1 Persistenzwert

`DbWerte.KOSTENART_ZUSCHUSS = "ZUSCHUSS"` (`Allgemein\DbWerte.cs`).

**Abweichung vom Konzepttext, bewusst:** § 7.4 schreibt `"zuschuss"` klein. Die vier
Bestandswerte der Gruppe (`KAPITALGEBUNDEN`, `BEDARFSGEBUNDEN`, `BETRIEBSGEBUNDEN`,
`SONSTIGE`) sind ASCII und durchgehend groß, und der Kommentarblock darüber
(`DbWerte.cs:205-217`) schreibt das ausdrücklich fest. Ein einzelner kleingeschriebener
Wert wäre die Ausnahme, an der jeder spätere Vergleich stolpert. Länge 8 Zeichen, die
Spalte ist `TEXT(20)` — die Längenprobe aus Etappe E3 ist damit erfüllt.

### 5.2 Der Betrag wird POSITIV erfasst — und warum das kein Erlös ist

`IstErloes` bleibt `false`. Ein Erlös geht über `BetriebskostenCtrl.Betrag` in die
**Jahres**reihe; der Zuschuss ist aber eine einmalige Zahlung im Jahr 0. Das Vorzeichen
entsteht ausschließlich im Rechenweg und in der Anzeige.

### 5.3 Eingabeweg — er musste erst geschaffen werden

Befund: **Für die Kostenart einer Investitionsposition gab es überhaupt keine
Oberfläche.** Migrationsschritt 19b belegt alle Kategorie-1-Zeilen mit `KAPITALGEBUNDEN`
vor, und danach änderte sie nie jemand (die Betriebskosten pflegen ihre Kostenart über
`Form_Betriebskosten`). Repoweite Suche nach `KOSTENART_`/`SPALTE_PW_KOSTENART`: nur
Migration, Rechenkern und Berichte — keine Eingabestelle.

Gewählt: **`Form_CaseEingabe`** (`Views\Kosten\Form_CaseEingabe.cs`). Sie hängt am
„+/−"-Knopf **jeder** Positionszeile und schreibt bereits heute in dasselbe
`KostenPosition`-Objekt, das `Form_Kosten.UpdateSingleRowInDatabase` danach speichert.
Der Schalter entsteht **programmatisch**, der Designer bleibt unberührt (Hausregel aus K4);
das Fenster wächst um die Zeile.

Angeboten wird er nur bei **Investitions-Nebenpositionen**:
- Betriebs-/Energiepositionen scheiden aus — dort hätte die Kostenart keine Rechenwirkung.
- Die Hauptposition scheidet aus — sie ist der Anlagenpreis selbst.

Persistiert wird über `Form_Kosten.KostenartSichern` — ein **zweites** `UPDATE`, nicht die
erweiterte Bestandsanweisung: Die Spalte stammt aus Schritt 19 und fehlt in einer nie
migrierten Datenbank; stünde sie in derselben Anweisung, scheiterte dort auch das
Speichern der Beträge, und zwar still.

### 5.4 Rechenweg

Gelesen wird über die neue Überladung
`WirtschaftlichkeitCtrl.LiesInvestitionen(idProjekt, szenario, out zuschuss)`. Die alte
Zwei-Parameter-Fassung bleibt und ruft sie — `UcBkKosten` und jeder andere Aufrufer
compilieren unverändert weiter.

Zuschusszeilen gehen **nicht** als `InvestPosition` in den `KapitalwertRechner`. Der Grund
ist der Alt-Fehler aus Anhang A(e): Eine Position bekommt über ihre Nutzungsdauer eine
Ersatzbeschaffung und einen Restwert, und die Altanwendung gab dem Zuschuss dafür die
Laufvariable des zuletzt bearbeiteten BHKW-Moduls — eine zufällige Nutzungsdauer.

Der Abzug steht in `KapitalwertRechner.Rechne` **nach** der Positionsschleife:

```
I₀_brutto = Σ Betrag(Position)                       ← Ersatzreihe und Restwert entstehen hieraus
Zuschuss  = min(Σ Zuschusszeilen, I₀_brutto)         ← Klemme
Überhang  = Σ Zuschusszeilen − Zuschuss
I₀        = I₀_brutto − Zuschuss                     ← geht in KW und Barwertreihe[0]
KW        = −I₀ − BarwertAusgaben + BarwertEinnahmen + RestwertBarwert
```

Weil der Abzug nach der Schleife steht, bleiben `ersatzJeJahr` und `restwertT`
**Bruttogrößen** — keine Ersatzbeschaffung und kein Restwert auf Fördergeld. Genau das war
die Vorgabe.

**Klemme und Warnung:** Ein Zuschuss über der Investitionssumme ergäbe ein negatives I₀,
also eine Einzahlung im Jahr 0. Rechnerisch möglich, fachlich eine Fehleingabe. Angesetzt
wird höchstens die Investitionssumme; der Überhang wandert als Hinweis in
`WirtschaftlichkeitErgebnis.Hinweis` (`MyResource.Resource.WIRT_ZUSCHUSS_UEBERHANG`) und
erscheint im Bericht.

**Szenarien:** Der Zuschuss folgt Best/Worst wie jede andere Zeile (0/leer →
Erwartungswert, VALERI-Muster). Eine Förderzusage kann ausfallen oder höher ausfallen —
genau die Art Unsicherheit, für die die Szenarien da sind.

**Sensitivität:** Der Investitionsfaktor ±10 % skaliert den Zuschuss **nicht**. Die Frage
lautet „was, wenn die Anlage 10 % mehr kostet?" — eine bewilligte Zusage über einen festen
Betrag ändert sich dadurch nicht. Ihn mitzuskalieren hieße zu behaupten, der Fördergeber
zahle Kostensteigerungen anteilig mit.

**Novellen-Szenario:** `OhneKwkg` kopiert den Zuschuss mit. Ohne diese Zeile rechnete das
Szenario gegen ein anderes I₀ als die Basis, und die ausgewiesene Differenz enthielte den
Zuschuss statt nur den weggefallenen KWKG-Bonus.

### 5.5 Die Bezugsgröße der Prozent-Betriebskosten — verifiziert und berichtigt

Vorgabe § 7.4: „% der Investitionssumme" rechnet **vor** Zuschussabzug. Die Stelle ist
`BetriebskostenCtrl.InvestSumme` (`Controller\BetriebskostenCtrl.cs`), aufgerufen aus
`LiesBezugsgroessen` für `InvestGesamt`, `InvestBhkw`, `InvestKessel`.

Sie war `SELECT SUM(EingegebenerWert) … WHERE KategorieID = 1` — **ungefiltert**. Ohne
Eingriff wäre das nicht bloß „nicht abgezogen", sondern **falsch herum**: Der Zuschuss
steht als POSITIVER Betrag in `EingegebenerWert` und wäre **addiert** worden. Die
Instandhaltung hätte sich an einer Investitionssumme bemessen, die es nie gab.

Jetzt schließt die Abfrage `Kostenart = 'ZUSCHUSS'` aus; `NULL` und Leerstring bleiben
drin (das sind die Bestandszeilen). Fehlt die Spalte, läuft die alte Abfrage — in einer
solchen Datenbank kann keine Zuschusszeile existieren. **Messbeleg: Abschnitt 7.2 B.**

### 5.6 Ausweis

| Ort | Weg |
|---|---|
| Ergebnisreiter, **Word**, **Excel** | eine neue Zeile in `WirtschaftlichkeitZeilen.Kennzahlen` — die gemeinsame Definition aus Etappe E7 bedient alle drei |
| `UcBkKosten` | Herkunftszeile der Investitionskachel: „davon Zuschuss: −X €" |
| Positionszeile | Einheitenspalte trägt „− €", Bezeichnung dunkelgrün |
| Gruppenkopf und Fußzeilen | Zuschuss geht negativ in die Summe (K5b) |

Die Berichtszeile erscheint **nur**, wenn irgendein Projekt der Vergleichsgruppe einen
Zuschuss führt — dasselbe Muster wie bei der KWKG- und der BEHG-Zeile. Sonst stünde in
jedem Bericht ohne Förderung eine Nullzeile.

**Ergebnisspalte:** `Tab_ErgebnisWirtschaftlichkeit.Zuschuss` (DOUBLE), angelegt über
`SpalteSicher` in `StelleTabellenSicher` — derselbe Weg wie alle Ergebnisspalten seit W1.
Die doppelte Schema-Wahrheit (§ 9.2) wird damit **nicht** um einen dritten Mechanismus
erweitert: Ergebnisspalten führt der Controller, Eingabespalten der Migrationskatalog.
Der INSERT in `Persistiere` wuchs von 41 auf 42 Platzhalter, `LadeErgebnisse` liest die
Spalte mit `?? 0`.

`Investition` bleibt der **Brutto**betrag. Beide Zahlen werden gebraucht: die Bruttosumme
als Bezugsgröße und als Wiedererkennungswert zur Kostenmaske, der Zuschuss als eigener
Ausweis. I₀ ist die Differenz und steckt im Kapitalwert — es wird nicht zusätzlich
abgelegt, damit es dazu keine zweite Wahrheit gibt.

### 5.7 Zwei Blocker, die erst beim Bauen sichtbar wurden

Ohne sie hätte Schritt 27 zwar Katalogzeilen erzeugt, die Komponenten wären aber
**unerreichbar** geblieben.

1. **`Form_Kosten.GetKomponentenID`** war eine reine `switch`-Kette über die sieben
   Bestandsnamen mit `default: return 0`. Für die neuen Gruppen hätte
   `KostenPositionCtrl.SetzeBetrag` sofort abgebrochen (`if (komponentenID <= 0) return 0;`).
   → `default` schlägt jetzt im Katalog nach (`KomponentenIdAusKatalog`, Ergebnis gemerkt).
   Die sieben festen Nummern bleiben stehen: Sie stecken auch in
   `BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL/_BHKW` und in jeder Bestandszeile.

2. **`Form_Kosten.ProjektKomponenten`** bot nur an, was verbaut ist **oder** schon
   Positionen trägt. Die drei Gruppen sind keine Gewerke — es gibt keine Gerätetabelle und
   damit kein „verbaut". In einem frischen Projekt wären sie nie in der Liste erschienen,
   also nie bepreisbar gewesen, also nie in der Liste erschienen.
   → `IstErfassungsgruppe(...)` bietet genau diese drei immer an. Es ist eine
   **Namensprüfung**, keine Katalogabfrage: Eine später von Hand angelegte Komponente ohne
   Gewerk und ohne Positionen bleibt draußen, genau wie bisher.

---

## 6 K5b — Gruppierung im Positionsreiter (§ 7.5)

### 6.1 Weg-Entscheid: die vorhandenen Gruppenköpfe werden zu Klapp-Köpfen

Der Auftrag verlangte „komponentenweise gruppiert unter einklappbaren Gruppenkopfzeilen —
Kopf: Komponentenname, Positionszähler, Gruppensumme". Der Bestand sieht so aus:

- **Links** eine Auswahlliste der Komponenten (`listBox_Erzeuger`).
- **Rechts** die Positionen **genau einer** Komponente
  (`LoadKostenFaktoren`: `WHERE … AND (Komponente = ?)`, `Views\Kosten\Form_Kosten.cs:990`),
  darin bereits Kopfzeilen — aber je **`Tab_ProjektWerte.Gruppe`** (Freitext: „Allgemein",
  „Infrastruktur", „Wartung", „test", …), nicht je Komponente.

Alle Positionen aller Komponenten gleichzeitig zu zeigen, hätte den Erfassungsweg
umgebaut: `btn_Hinzu`, der Lösch-Knopf je Gruppe, „Planwert übernehmen…", die
Hinweiszeile und `EnsureMainComponentExists` hängen sämtlich an
`listBox_Erzeuger.Text`. Die Auflage „Bestandsverhalten muss unverändert funktionieren"
wäre dabei nicht zu halten gewesen.

**Gewählt:** Die Komponente bleibt der Rahmen (Auswahl links), und die vorhandenen
Kopfzeilen werden zu dem, was der Auftrag beschreibt — mit dem **Komponentennamen** vorn:

```
▾  WÄRMEZENTRALE   ·   3 Positionen   ·   42.500,00 €
▸  WÄRMEZENTRALE · INFRASTRUKTUR   ·   2 Positionen   ·   8.100,00 €
```

Der freie Gruppenname kommt nur dazu, wenn er **nicht** die Rückfallgruppe „Allgemein"
ist. Sonst stünde in der Regel „WÄRMEZENTRALE · ALLGEMEIN" da, und das Wort ohne Aussage
wäre das auffälligste im Kopf. Ein Klick auf Kopf oder Beschriftung klappt um; das Zeichen
(▾/▸) sagt den Zustand.

### 6.2 Was beim Einklappen passiert — und was nicht

Ausgeblendet werden die Positionszeilen und die Spaltenüberschrift. Die Zeilen werden nur
**unsichtbar**, nicht entfernt: Jede erfasste Zahl bleibt im Speicher, und `Gesamtkosten`
summiert unverändert über `flp.Controls`. **Ein eingeklappter Block verändert keine einzige
Summe.** Der Zähler nennt weiter den Inhalt der Gruppe, nicht den Bildschirmausschnitt.

Der Aktionsknopf („Planwert übernehmen…" bzw. „Betriebskosten VDI 2067…") saß hinter der
kurzen Beschriftung. Mit Zähler und Summe wird sie länger — er rückt jetzt mit und stößt
nicht an den Lösch-Knopf.

Die Blockverwaltung führt **keine** Zeilenliste mit. Zeilen werden an drei Stellen aus
`flp` entfernt und verworfen (`Zeile_DeleteRequested`, `btnDeleteGroup_Click`, Neuaufbau);
eine zweite Liste zeigte danach auf entsorgte Steuerelemente. Gesucht wird über
`flp.Controls` und das `Tag` — dieselbe Zuordnung, die der Löschbefehl schon benutzt.

### 6.3 Zuschuss negativ, an allen vier Stellen des Reiters

| Stelle | Verhalten |
|---|---|
| Positionszeile | Einheitenspalte „− €", Bezeichnung dunkelgrün |
| Gruppenkopf | Summe rechnet `− Betrag` |
| Fußzeile „Selektion" | ebenso (Live-Werte aus den Controls) |
| Fußzeile „Projekt gesamt" | `LiesKomponentenSummen` summiert roh; der Zuschuss wird über `LiesZuschuss` abgezogen |

Der Abzug bei „Projekt gesamt" steht **hier** und nicht in `LiesKomponentenSummen`: Die
Lesemethode dient auch der Komponententabelle von `UcBkKosten`, und deren Verhalten sollte
diese Etappe nicht mitverändern.

### 6.4 Die Gruppierung gilt auf allen drei Kostenreitern

`UpdateDetailPanel` ist die gemeinsame Aufbaumethode für Investition, Betrieb und Energie;
`flp` wird je Reiter umgehängt. Die Klapp-Köpfe erscheinen deshalb überall. Das ist
gewollt: Ein unterschiedliches Verhalten je Reiter **aus derselben Methode heraus** wäre
die eigentliche Überraschung. Der Zuschuss betrifft ohnehin nur Kategorie 1.

Der **Kategorie-Wächter aus K4** ist respektiert: `Gesamtkosten` steigt bei
`!kat.HasValue` (Reiter „Kostenprofil") vor dem Nachziehen der Köpfe aus,
`EnsureMainComponentExists` und `AddKostenItem` unverändert ebenfalls.

---

## 7 Verifikation

### 7.1 Migrationsschritt 27 an der Scratch-Kopie

Kurzlebiger Einzelprozess gegen eine Kopie von `Referenzlaeufe\Arbeitskopie\Kenndaten.accdb`;
dieselben Anweisungen wie im C#-Schritt.

| Größe | vorher | nachher |
|---|---|---|
| `Tab_KostenKomponente` | 7 | **10** |
| `Tab_Kostenfaktor` gesamt | 19 | **30** |
| davon Hauptpositionen | 7 | **10** |
| davon Nebenpositionen | 12 | **20** |
| `Tab_ProjektWerte` Zeilen | 58 | **58** |
| `SUM(EingegebenerWert)` | 74.705,34 | **74.705,34** |

Erstlauf: 27a = 3 Komponenten, 27b = 3 Hauptpositionen, 27c = **8** Nebenpositionen.
Acht statt zwölf, und das ist die Probe auf den flachen Katalog: 12 Katalogeinträge
− 2 × „Sonstiges" (kommt dreimal vor, entsteht einmal) − „Schornstein" und „Abgasanlage"
(im Bestand) = 8.

Neue Zeilen: Komponenten `8 Wärmezentrale`, `9 Bauliche Anlagen`, `10 Stromeinspeisung`;
Katalog `StammID 95…105`.

**Zweitlauf: 27a = 0, 27b = 0, 27c = 0** — idempotent.
**Bestandsschutz:** `Schornstein` (90) und `Abgasanlage` (91) unverändert.
**Ergebnisneutral:** Projektzeilen und Summe unangetastet — ein leerer Katalogeintrag
rechnet nicht.

### 7.2 Zuschuss-Rechenweg an der Scratch-Kopie

Testprojekt 1022 (größte Investitionssumme im Bestand), Schritt-19-Spalten per Vorsorge
angelegt, eine Zuschusszeile über 25 % der Investitionssumme eingefügt, danach entfernt.

**A) Aufteilung** (`LiesInvestitionen`):

| | |
|---|---|
| Investitionspositionen | 3, Summe **12.751,67 €** |
| Zuschuss | **3.187,92 €** |
| I₀ = Summe − Zuschuss | **9.563,75 €** |
| Investitionssumme gegenüber „ohne Zuschusszeile" | **unverändert** ✓ |

**B) Bezugsgröße** (`BetriebskostenCtrl.InvestSumme`):

| Variante | Ergebnis |
|---|---|
| ALT (ungefiltert) | 15.939,59 € ← der Zuschuss wäre **addiert** worden |
| NEU (K5-Filter) | **12.751,67 €** |
| Sollwert (Investitionssumme vor Zuschuss) | 12.751,67 € ✓ |

Das ist der Beleg für die Konzeptregel „Prozent-Bemessungen rechnen vor Zuschussabzug" —
und zugleich der Beleg, dass ohne den Filter ein Vorzeichenfehler entstanden wäre.

**C) Klemme:** erfasst 19.127,51 €, Investition 12.751,67 € → angesetzt 12.751,67 €,
Überhang 6.375,84 €, **I₀ = 0,00 €** (nie negativ), Hinweis gesetzt.

### 7.3 Eine Falle im Prüfwerkzeug (nicht im Produkt)

Der erste Lauf von B meldete „kein Zuschuss erkannt". Ursache lag im PowerShell-Skript:
**Variablennamen sind in PowerShell case-insensitiv.** Der Akkumulator `$zuschuss = 0.0`
überschrieb die Konstante `$ZUSCHUSS = "ZUSCHUSS"`; verglichen wurde danach gegen `0`.
Ein Gegentest mit getrennten Namen bestätigte, dass sowohl der Textvergleich als auch der
SQL-Filter korrekt arbeiten. **Am Produktcode war nichts zu ändern** — dort sind es
`const`-Felder in C#, wo der Fall nicht auftreten kann. Zweite kleinere Falle derselben
Sitzung: `SC` ist ein PowerShell-Alias für `Set-Content` und schlägt eine gleichnamige
eigene Funktion; die Hilfsfunktionen heißen jetzt `Skalar`/`Ausfuehren`/`Tabelle`.

### 7.4 Build

| Lauf | Ergebnis | Diagnosen |
|---|---|---|
| Baseline (vor K5) | 0 Fehler | 6 Warnungen |
| nach K5a | 0 Fehler | 6 Warnungen |
| nach K5b | 0 Fehler | 6 Warnungen |

Identische Menge in allen drei Läufen — die sechs bekannten Altwarnungen:
`KlimaregionStammCtrl.cs:22/23` (CS0109), `MDIMainForm.cs:348` (CS1998), `:359` (CS4014),
`StromverbraucherStammCtrl.cs:25` (CS0108), `WErzeugerModel.cs:6` (CS0108).
**Keine Diagnose aus einer K5-Datei.**

Nur `WindowsFormsApplication1.csproj`, inkrementell, VS-2022-MSBuild, x86/Debug.

### 7.5 Encoding je angefasster Datei

Vor jedem Eingriff gemessen (UTF-8-Decode + BOM-Prüfung). **Alle angefassten Dateien sind
UTF-8**; keine byte-erhaltende Sonderbehandlung nötig.

| Datei | BOM | vorher/nachher |
|---|---|---|
| `Allgemein\DbWerte.cs` | ja | UTF-8 |
| `Allgemein\Update\SchemaKatalog.cs` | ja | UTF-8 |
| `Allgemein\Update\SchemaMigration.cs` | ja | UTF-8 |
| `Allgemein\Wirtschaftlichkeit\KapitalwertRechner.cs` | nein | UTF-8 |
| `Allgemein\Wirtschaftlichkeit\WirtschaftlichkeitCtrl.cs` | nein | UTF-8 |
| `Allgemein\Wirtschaftlichkeit\WirtschaftlichkeitDaten.cs` | nein | UTF-8 |
| `Allgemein\Wirtschaftlichkeit\WirtschaftlichkeitZeilen.cs` | nein | UTF-8 |
| `Controller\BetriebskostenCtrl.cs` | nein | UTF-8 |
| `Views\Kosten\Form_Kosten.cs` | ja | UTF-8 |
| `Views\Kosten\Form_CaseEingabe.cs` | ja | UTF-8 |
| `Views\Kosten\ucKostenItem.cs` | ja | UTF-8 |
| `Views\BerichteKosten\UcBkKosten.cs` | ja | UTF-8 |
| `MyResource\Resource.resx` / `.en-US.resx` / `.Designer.cs` | ja | UTF-8 |

`Views\Kosten\SectionPanel.cs` (cp1252) wurde **nicht angefasst** — die Gruppenköpfe
entstehen programmatisch im vorhandenen Muster.

**Eine Lehre zu den Ressourcendateien:** Beim ersten additiven Schreiben wurden CRLF nach
LF umgeschrieben (Python `newline=""` schreibt das, was der universelle Lesemodus
geliefert hat). Der Commit-Inhalt blieb korrekt (Git normalisiert; Diff = +15/−0 je Datei),
die Arbeitskopie wurde per `git checkout` zurückgeholt. Für K5b wird **binär** gelesen und
geschrieben, die Zeilenenden werden gezählt und erhalten: CRLF 6.236 / 6.230 / 17.942,
kein einziges nacktes LF.

### 7.6 Konfliktmarker-Sweep

Repoweit auf `<<<<<<<` / `>>>>>>>` in `*.cs`, `*.resx`, `*.md` — **am Anfang und am Ende
der Etappe leer.**

---

## 8 Sichtprüfliste für Philipp

Vorbereitung: Anwendung starten (die Migration läuft beim ersten Start und hebt den
Schemastand auf 27), ein Projekt mit BHKW öffnen, Kostenmaske aufrufen.

1. **Neue Komponenten sind da.** Reiter „Investitionskosten": Die Auswahlliste links führt
   unter den bekannten Gewerken zusätzlich **Wärmezentrale**, **Bauliche Anlagen** und
   **Stromeinspeisung** — auch in einem Projekt, in dem für sie noch nichts erfasst ist.
2. **Positionskatalog.** „Wärmezentrale" anwählen → „+" → die Auswahlliste des Dialogs
   enthält *BHKW-Einbindung*, *Heizungstechnik*, *Abgasanlage*, *Heizraum*, *Schornstein*,
   *Bauliche Maßnahmen*, *Heizöllagerung*, *Erdgasanschluss*, *Stromeinspeisung*,
   *Sonstiges*. (Sie stehen alle in einer Liste — der Katalog ist flach, siehe 1.1.)
   **Kein Verteilnetz, kein Hausanschluss, keine Hausstation** (E2).
3. **Gruppierung sichtbar.** Über den Positionszeilen steht ein dunkelblauer Kopf in der
   Form `▾ WÄRMEZENTRALE · 3 Positionen · 42.500,00 €`. Der Komponentenname steht vorn.
4. **Ein-/Ausklappen.** Klick auf den Kopf (oder seine Beschriftung) blendet die Zeilen und
   die graue Spaltenüberschrift aus, das Zeichen wechselt auf `▸`. Erneuter Klick holt
   alles zurück. **Zähler und Summe bleiben im eingeklappten Kopf stehen.**
   Die beiden Knöpfe auf dem Kopf („Planwert übernehmen…", „−") klappen **nicht** um,
   sondern tun weiter, was sie sollen.
5. **Summen je Gruppe.** Einen Betrag ändern → die Gruppensumme im Kopf zieht sofort nach,
   ebenso beide Fußzeilen.
6. **Zuschuss anlegen.** Eine Nebenposition anlegen (z. B. „Sonstiges"), Betrag **positiv**
   eintragen, dann auf den kleinen „+/−"-Knopf der Zeile: Im Dialog steht unter den vier
   Szenariofeldern das Kästchen **„Diese Position ist ein Zuschuss (BAFA, KfW, …)"** mit
   Erläuterung. Ankreuzen, OK.
7. **Zuschuss negativ.** Nach dem Neuaufbau der Liste: Die Zeile zeigt in der
   Einheitenspalte **„− €"**, die Bezeichnung ist dunkelgrün. **Gruppensumme und beide
   Fußzeilen sind um den Betrag kleiner** geworden, nicht größer.
8. **Rechenweg.** Wirtschaftlichkeit neu rechnen. Im Ergebnisreiter (und in Word/Excel)
   steht direkt unter „Investition I₀" die Zeile **„Zuschuss [€]"** mit **negativem**
   Betrag. Die Zeile „Investition" zeigt weiterhin die **Brutto**summe.
9. **Keine Ersatzbeschaffung, kein Restwert.** Der Zuschuss taucht in der
   Mehrjahrestabelle des Berichts **in keinem Folgejahr** auf und erhöht den Restwert
   nicht — er wirkt genau einmal, im Jahr 0.
10. **Kostenübersicht.** Seite „Berichte & Kosten": Unter der Investitionskachel steht
    statt der gewohnten Herkunftszeile **„davon Zuschuss: −X €"**. Der große Wert bleibt
    die Bruttosumme.
11. **Betriebskosten-Hinweis.** Reiter „Betriebskosten" → BHKW → „Betriebskosten
    VDI 2067…": Rechts neben jedem Satzfeld steht die Bezugsgröße und dahinter
    **„üblich X–Y %"** — BHKW 3,0–9,0; Kessel 1,5–2,5; Wärmezentrale 1,8–2,2; bauliche
    Anlagen 1,0–1,5; Stromeinspeisung 1,8–2,2; Personal 1,0–4,0; Verwaltung 0,8–2,0.
    *(Das gab es schon vor K5 — bitte trotzdem gegenprüfen, es ist die Zusage aus § 7.6.)*
12. **Prozent vor Zuschussabzug.** Dieselbe Maske: Die angezeigte Bezugsgröße
    „Investition gesamt" ist die **Brutto**summe, nicht die um den Zuschuss geminderte.
13. **Reiterverhalten unverändert.** Hinzufügen, Löschen einer Zeile, Löschen einer Gruppe
    („−"), „Planwert übernehmen…", Best/Worst-Eingabe, Wechsel zwischen den vier Reitern —
    alles wie vorher. Auf dem Reiter „Kostenprofil" passiert weiterhin nichts Ungewolltes.
14. **Überzahlung.** Optional: Zuschuss größer als die Investitionssumme eintragen →
    Kapitalwert rechnet mit I₀ = 0, und im Bericht steht der Hinweis, dass der erfasste
    Zuschuss die Investitionssumme übersteigt.

---

## 9 Offene Punkte

1. **Bezugsgröße der drei Instandhaltungspositionen.** Seit Schritt 27 gibt es die
   Komponenten, an denen sich „Instandhaltung Wärmezentrale / bauliche Anlagen /
   Stromeinspeisung" eigentlich bemessen müssten; heute rechnen sie gegen die
   Gesamtinvestition (`BetriebskostenCtrl.cs:130-141`, Bezug `BEZUG_INVEST_GESAMT`).
   Die Umstellung wäre eine **stille Ergebnisänderung an jedem Bestandsprojekt**, das eine
   dieser Positionen gepflegt hat, und braucht deshalb eine Entscheidung — samt der Frage,
   ob Altprojekte umgestellt oder auf der alten Bemessung gehalten werden.
2. **`KostenPositionCtrl.StammIdNeben` schreibt `StammID = 0`.** Das `INSERT` (`:183-186`)
   lässt die Spalte weg im Glauben, sie sei ein AutoWert. Sie ist es nicht (1.3). Beim
   ersten „Lern"-Anlegen einer freien Position entsteht dadurch ein Katalogeintrag mit
   `StammID = 0`; ein zweiter kollidierte. Der Fehler ist **älter als K5** und wurde hier
   nur belegt, nicht behoben — er gehört in eine eigene, kleine Runde mit eigener
   Verifikation (Muster: `Form_KostenAdmin`, `MAX + 1`).
3. **`Abfrage_Kostenfaktoren` kennt die neuen Positionen nur über `Tab_ProjektWerte`.**
   Das ist unverändert und richtig, aber die gespeicherte Abfrage liegt außerhalb des
   Repos. Sollte sie in einer Bestandsinstallation zusätzliche Filter tragen, sind die neuen
   Komponenten dort erst sichtbar, wenn die erste Position erfasst ist. **Sichtprüfung 1
   und 2 der Liste oben decken genau das ab.**
4. **KI-Aktion `kostenposition_setzen`.** Konzept § 9.4 verlangt, die Kostenart
   „Zuschuss" in die Positivliste der `KiPruefung` aufzunehmen. Die Aktion prüft heute
   **keine** Kostenart (repoweite Suche: keine Positivliste dafür in
   `Allgemein\KI\Aktionen\KiAktionenSchreiben.cs`), es war also nichts aufzunehmen.
   Sobald die Aktion die Kostenart annimmt, muss `ZUSCHUSS` mit hinein.
5. **Kein Programmstart, keine Referenzläufe.** Die Etappe ist nicht ergebnisneutral —
   Konzept § 10 weist K5 ausdrücklich als erste Etappe mit gewollter Ergebnisänderung aus.
   Solange kein Projekt eine Zuschusszeile führt, ändert sich rechnerisch nichts (die
   Bezugsgrößen-Abfrage liefert dann dasselbe wie vorher). Ein Referenzlauf-Vergleich
   wurde deshalb **nicht** gefahren; die Abnahme läuft über die Sichtprüfliste.
6. **Die Migration selbst wurde nicht über die Anwendung ausgelöst**, sondern mit
   identischen Anweisungen an der Scratch-Kopie nachgestellt (Abschnitt 7.1). Der erste
   echte Lauf passiert beim nächsten Programmstart auf der Arbeitskopie.

---

## 10 Commits

| Commit | Inhalt |
|---|---|
| `918f8f5` | **K5a** — Migrationsschritt 27, Kostenart `ZUSCHUSS`, Rechenweg, Bezugsgrößen-Filter, Ausweis in Bericht und Kostenübersicht, Eingabeschalter, die zwei Erreichbarkeits-Blocker |
| *dieser Commit* | **K5b** — einklappbare Gruppenköpfe mit Zähler und Summe, Zuschuss negativ in Kopf und Fußzeilen; dieses Protokoll liegt darin |

Kein Push (`main` bleibt ungepusht voraus).

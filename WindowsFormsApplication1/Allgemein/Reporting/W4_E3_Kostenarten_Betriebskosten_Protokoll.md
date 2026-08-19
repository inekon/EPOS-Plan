# W4 · Etappe E3 — Kostenarten, Bemessungsarten und Betriebskosten nach VDI 2067

**Stand: 19.08.2026.** Umsetzung der Etappe **E3** aus
[`Konzept_BHKW_Kosten_Erloese.md`](Konzept_BHKW_Kosten_Erloese.md), Leitentscheidungen
**L5** (Kostenposition erweitern statt eigener Erlöstabelle), **L7** (genau eine
Wartungsbemessung) und **L8** (netto verbindlich, Umsatzsteuer aus dem Katalog).
Ausgangsstand `8cfefb3` (Referenzbasis `2026-08-19_B5`).

**Ergebnis in drei Sätzen.** `Tab_ProjektWerte` trägt ab jetzt Kostenart, Bemessungsart,
Erlöskennzeichen, Menge und Einheitpreis; die vier abgeleiteten Bemessungsarten (% der
Investition, €/h, €/kWh, % der Brennstoffkosten) rechnen aus einer **persistierten**
Herleitung, und Erlöspositionen dürfen negativ sein. Für Bestandsprojekte ändert sich
**nichts**: Migrationsschritt 19b belegt jede vorhandene Zeile mit `BETRAG`, und diese
Bemessungsart ist zeilengleich der Rechenweg vor E3 — belegt durch **9/9 PASS** und
**216/216 byte-identische** Referenz-CSV sowie durch **27/27 identische** Betriebskosten-
und Kapitalwertwerte im A/B-Vergleich. Neu ist die Maske „Betriebskosten VDI 2067" mit den
elf Positionen der Altanwendung in drei Spalten.

---

## 1 Was umgesetzt wurde

| # | Gegenstand | Datei : Zeile |
|---|---|---|
| 1 | Vier Kostenarten nach VDI 2067 als Persistenzwerte | `Allgemein/DbWerte.cs:204-249` (`KOSTENART_*`, ab `:225`) |
| 2 | Fünf Bemessungsarten als Persistenzwerte, mit Längenhinweis | `DbWerte.cs:251-311` (`BEMESSUNG_*`, ab `:274`) |
| 3 | Elf Positionsbezeichnungen und die Gruppe der Reihe | `DbWerte.cs:313-385` (`VDI_POS_*` ab `:332`, `KOSTEN_GRUPPE_BETRIEB_VDI` `:385`) |
| 4 | Spaltennamen und Schritt-19-Katalog | `Allgemein/Update/SchemaKatalog.cs:646-733` (`TAB_PROJEKTWERTE` `:646`, `SPALTE_PW_*` `:656/668/683/692/698`, `Schritt19_Kostenarten` `:725`) |
| 5 | Begründung, warum Schritt 19 **nicht** in `SchemaKatalog.Alle` steht | `SchemaKatalog.cs:801-808` |
| 6 | **Migrationsschritt 19** — Nummer, Begründung, Zielversion 18 → 19 | `Allgemein/Update/SchemaMigration.cs:77` (`ZIEL_VERSION`), `:357-394` (`SCHRITT_19_KOSTENARTEN`) |
| 7 | Registrierung in `SCHRITTE`, Ausführung (19a DDL + 19b DML), Zählwerk, Protokollzeile | `SchemaMigration.cs:731-737`, `:1251-1341`, `:478/484`, `:1002-1013` |
| 8 | Tolerante Schemavorsorge für den Fall ohne Migration | `Controller/KostenPositionCtrl.cs:80-152` (`StelleSpaltenSicher`) |
| 9 | Lese- und Schreibschicht der fünf Spalten | `KostenPositionCtrl.cs:510-543` (`Zusatz`), `:545-573` (`LiesZusatz`), `:575-599` (`LiesZusatzNachId`), `:601-641` (`AusZeile`), `:643-666` (`SetzeBetragMitZusatz`), `:668-675` (`ZahlOderNull`) |
| 10 | **Der Positionskatalog**: elf Positionen mit Bemessungsarten, Bezugsgrößen, Empfehlungsbereichen | `Controller/BetriebskostenCtrl.cs:143-247` (`Katalog`), Typ `:72-141` (`Position`) |
| 11 | **Der eine Rechenweg** je Bemessungsart (reine Funktion, kein DB-Zugriff) | `BetriebskostenCtrl.cs:261-299` (`Betrag`) |
| 12 | Bezugsgrößen eines Projekts, jede einzeln gefangen | `BetriebskostenCtrl.cs:335-389` (`Bezugsgroessen`), `:367-389` (`LiesBezugsgroessen`), `:476-495` (`LiesBrennstoffkosten`) |
| 13 | Lesen und Schreiben der elf Positionen | `BetriebskostenCtrl.cs:497-530` (`Zeile`), `:532-577` (`Lies`), `:579-618` (`Speichere`) |
| 14 | Einheitenzeichen von Satz und Menge | `BetriebskostenCtrl.cs:307-333` |
| 15 | **Die Bemessung wirkt in der Jahresrechnung** | `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:1851-1913` (`LiesBetriebskosten`), Hilfsmittel `:2260-2265` (`Text`) |
| 16 | Negative Beträge für Erlöse in der Eingabe; abgeleitete Beträge gesperrt | `Views/Kosten/ucKostenItem.cs:23-66`, Datenfelder `:216-247` |
| 17 | Vorzeichenregel an EINER Stelle in der Planwertschicht | `Controller/TechnikPlanwertCtrl.cs:396-422` (`Basis` mit `erloes`-Schalter) |
| 18 | Kostenverwaltung: Zusatzangaben laden, Knopf auf dem Betriebskostenreiter | `Views/Kosten/Form_Kosten.cs:676-724` (`LoadKostenFaktoren`), `:380-427` (Knopf), `:1133-1160` (`btnBetriebskostenVdi_Click`) |
| 19 | **Die Maske** „Betriebskosten VDI 2067" (ohne Designer-Datei) | `Views/Kosten/Form_Betriebskosten.cs:1-570` |
| 20 | 43 Ressourcenschlüssel in beiden Sprachen samt Designer und Katalognachtrag | `MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs`, `Allgemein/Simulation/Lokalisierung_Katalog.md` |

**Elf geänderte und zwei neue Quelldateien**, dazu dieses Protokoll (neu),
`W4_Umsetzungsstand.md` und der Nachtrag im Lokalisierungskatalog. **Nichts committet.**

**Die Engine ist unberührt** — `git diff --stat -- 'WindowsFormsApplication1/Allgemein/Simulation/*.cs'`
ist leer; dort änderte sich nur die Dokumentationsdatei `Lokalisierung_Katalog.md`.

---

## 2 Datenmodell — Migrationsschritt 19

`Tab_ProjektWerte` bekommt fünf additive Spalten. Die Tabelle hat **kein**
`_STAMM`-Gegenstück (sie ist eine reine Projekttabelle; ihr Katalog ist
`Tab_Kostenfaktor` mit Bezeichnung und Rolle), die Regel „neue Spalten immer in Projekt-
und `_STAMM`-Tabelle" greift hier also nicht.

| Spalte | Typ | Vorbelegung durch 19b | Bedeutung |
|---|---|---|---|
| `Kostenart` | `TEXT(20)` | aus der Kategorie abgeleitet | VDI-2067-Systematik; **ohne Rechenwirkung**, gliedert die Ausgabe |
| `Bemessung` | **`TEXT(30)`** | `BETRAG` für jede Zeile | wie der Jahresbetrag entsteht — die Spalte, an der die Ergebnisneutralität hängt |
| `IstErloes` | `YESNO` | *(keine — ACE setzt False)* | Erlöskennzeichen; nur dort ist ein negativer Betrag zulässig |
| `Menge` | `DOUBLE` | *(keine — bleibt NULL)* | Bezugsmenge: €, h/a oder kWh/a |
| `Einheitpreis` | `DOUBLE` | *(keine — bleibt NULL)* | Satz: %, €/h oder €/kWh |

### 2.1 Drei ACE-Regeln, nachgemessen statt angenommen

1. **`YESNO` kennt kein NULL.** Access belegt die Spalte bei jeder Bestandszeile
   selbsttätig mit `False`; ein DML-Schritt dafür wäre überflüssig. Nachgemessen:
   `SELECT COUNT(*) … WHERE IstErloes IS NULL` = **0**, `GROUP BY IstErloes` = 67 × `False`.
2. **`DOUBLE` bleibt NULL, und das ist die richtige Aussage.** `Menge` und `Einheitpreis`
   werden bewusst **nicht** vorbelegt: „nicht gepflegt" ist etwas anderes als „gepflegt und
   null". Nachgemessen: 67 von 67 Zeilen NULL in beiden Spalten.
3. **`TEXT` bleibt NULL** und braucht deshalb den eigenen DML-Schritt 19b.

### 2.2 Warum `Bemessung` TEXT(30) statt der beauftragten TEXT(20) ist

Der längste Steuerwert ist `PROZENT_BRENNSTOFFKOSTEN` mit **24 Zeichen**. Bei `TEXT(20)`
scheitert das `UPDATE` der Hilfsenergie-Position — und zwar **still**: `DataRepository.ExecuteSQL`
zeigt den SQL-Fehler als modalen Dialog, der im Stapelbetrieb niemanden erreicht. Im
Reflection-Harnisch ist das als hängender Prozess aufgefallen (die Zeile blieb auf 0 stehen,
während die übrigen fünf korrekt geschrieben wurden). Die Spalte ist deshalb `TEXT(30)`;
`Kostenart` bleibt bei `TEXT(20)`, weil dort `BETRIEBSGEBUNDEN` mit 16 Zeichen der längste
Wert ist. Der Längenhinweis steht bei den Konstanten (`DbWerte.cs:251-311`), damit ein
späterer sechster Wert nicht dieselbe Falle stellt.

### 2.3 Warum die Kostenart der Kategorie folgt und nicht pauschal „kapitalgebunden" lautet

Der Auftrag nennt für Bestandszeilen „`BETRAG`/kapitalgebunden". Ergebnisneutral ist davon
allein **`BETRAG`** — die Kostenart wird von keiner Rechnung gelesen. Eine pauschale
Vorbelegung „kapitalgebunden" wäre für jede Wartungs- und Energieposition sachlich falsch
und müsste im Bericht der Etappe E7 von Hand berichtigt werden. Schritt 19b ordnet deshalb
nach der Kategorie ein, die diese Information bereits trägt:

| `KategorieID` | `Tab_KostenKategorie.KategorieName` | `Kostenart` | Zeilen im Bestand |
|---|---|---|---|
| 1 | Investitionskosten | `KAPITALGEBUNDEN` | 51 |
| 2 | Betriebskosten | `BETRIEBSGEBUNDEN` | 16 |
| 3 | Energiekosten | `BEDARFSGEBUNDEN` | 0 |

**Rechenwirkung: keine.** Die Zuordnung ist eine Umbenennung dessen, was in `KategorieID`
ohnehin steht.

### 2.4 Zwei Wege zur Spalte, wie im Bestand

Wie bei den Schritten 12 und 15 gehört `Tab_ProjektWerte` dem Kostenmodul; die stille
Rückfallebene `WaermequelleClass.SchemaSicherstellen` sichert ausdrücklich nur die
**Eingabespalten der Simulation**. Schritt 19 steht deshalb **nicht** in
`SchemaKatalog.Alle`. Seine eigene, tolerante Vorsorge ist
`KostenPositionCtrl.StelleSpaltenSicher` (`KostenPositionCtrl.cs:80`): eigene
`OleDbConnection` statt `DataRepository.ExecuteSQL` (eine Vorsorge ist kein Bedienschritt
und darf keine MessageBox zeigen), Schema einmal je Programmlauf gelesen, Rückgabewert
`false` = „der Aufrufer muss ohne die Spalten auskommen".

---

## 3 Zuordnung der VDI-Bezugsgrößen auf das EPOS-Plan-Modell

### 3.1 Die Ausgangslage — warum eine Zuordnung nötig ist

Die Altanwendung kennt **Investitionsgruppen** (Heizraum, Schornstein, Abgasanlage,
Öllagerung, Gasanschluss, Heizungstechnik, Einbindung, Puffer, Stromeinspeisung) und
bemisst die Instandhaltung daran. EPOS-Plan führt so etwas nicht. Es kennt

* **sieben Komponenten** (`Tab_KostenKomponente`, IDs 1…7: Wärmepumpe, Heizkessel,
  Photovoltaik, Solarthermie, Stromspeicher, Pufferspeicher, BHKW) und
* den **Freitext** `Tab_ProjektWerte.Gruppe` mit dem „lernenden" Katalog
  `Tab_KostenGruppenKatalog`.

**Die Gruppe trägt als Bezugsgröße nicht.** Belegt am Bestand: `Tab_KostenGruppenKatalog`
führt heute die Namen *Arbeitspreis, Infrastruktur, **test**, Energieerzeuger, Brennstoffe,
Wartung, Allgemein, Investition BHKW-Kaskade, Investition Spitzenkessel, Investition
Pufferspeicher, Wartung BHKW, Wartung Kessel*. Sie entstehen frei bei der Eingabe, heißen
je Projekt anders und sind teils gar keine Investitionsgruppen. Eine Prozentangabe darauf
zu beziehen hieße, die Bezugsgröße dem Zufall der Eingabe zu überlassen.

**Neue Investitionsgruppen wurden nicht erfunden.** Das wäre ein Eingriff ins Datenmodell
ohne Auftrag und mit Folgen für Kostenmaske, Berichte und Migration. Stattdessen bemessen
sich die drei Positionen ohne eigene Komponente an der **Investitionssumme des Projekts** —
und zwar **sichtbar benannt**: In der Spalte „Bezugsgröße und Empfehlung" steht bei ihnen
ausdrücklich „Investitionssumme des Projekts: 410.000,00 €".

### 3.2 Die Zuordnungstabelle

| # | VDI-Position (Altanwendung) | Bemessung | Bezugsgröße Altanwendung | **Bezugsgröße EPOS-Plan** | Quelle im Datenmodell | Empfehlung VDI 2067 |
|---|---|---|---|---|---|---|
| 1 | Vollwartung / Wartung BHKW | **genau eine** von €/kWh el, €/h, % Investition | Stromproduktion · Betriebsstunden · Investition BHKW | Stromerzeugung BHKW · Vbh BHKW · Investition BHKW | `Σ Tab_ErgebnisBHKWModul.Stromproduktion × 1000` · `Σ Tab_ErgebnisBHKWModul.VbhThermisch` · `Σ Tab_ProjektWerte` Kat. 1, KomponentenID 7 | — |
| 2 | Instandhaltung BHKW | % Investition | Investition BHKW | **Investition BHKW** | `Σ Tab_ProjektWerte` Kat. 1, KomponentenID **7** | 3,0–9,0 |
| 3 | Instandhaltung Heizkessel | % Investition | Investition Kessel | **Investition Heizkessel** | `Σ Tab_ProjektWerte` Kat. 1, KomponentenID **2** | 1,5–2,5 |
| 4 | Instandhaltung Wärmezentrale | % Investition | Heizungstechnik + Einbindung + Puffer + Abgasanlage | **Investitionssumme des Projekts** *(sichtbar benannt)* | `Σ Tab_ProjektWerte` Kat. 1, alle Komponenten | 1,8–2,2 |
| 5 | Instandhaltung bauliche Anlagen | % Investition | Heizraum + Schornstein + bauliche Maßnahmen + Öllagerung + Gasanschluss | **Investitionssumme des Projekts** *(sichtbar benannt)* | dito | 1,0–1,5 |
| 6 | Instandhaltung Stromeinspeisung | % Investition | Investition Stromeinspeisung | **Investitionssumme des Projekts** *(sichtbar benannt)* | dito | 1,8–2,2 |
| 7 | Personalkosten | % Investition | Investitionssumme | **Investitionssumme des Projekts** | dito | 1,0–4,0 |
| 8 | Steuern, Versicherung, Verwaltung | % Investition | Investitionssumme | **Investitionssumme des Projekts** | dito | 0,8–2,0 |
| 9 | Hilfsenergiekosten | **% der Brennstoffkosten** | Summe Brennstoffkosten | **Summe Brennstoffkosten** | `KostenEmissionRechner`: `Energiekosten − StromkostenNetz` | — |
| 10 | Reserveleistungskosten | Betrag €/a | — | — | — | — |
| 11 | Sonstige Kosten | Betrag €/a | — | — | — | — |

**Warum elf statt zwölf Zeilen.** Die Altmaske führt die Wartung **zweimal** — „Wartung
BHKW (Betriebsstunden)" und „Wartung BHKW (Erzeugung)" — und ließ die eine die andere
kommentarlos überschreiben (Befund 6). Genau das verbietet L7: Es gilt genau **eine**
Bemessung, sichtbar ausgewählt. Aus zwei Zeilen wird deshalb eine Zeile mit einer
Auswahlliste. Fachlich sind es weiterhin die zwölf Positionen der Altanwendung.

### 3.3 Zugehörigkeit und Bemessungsgrundlage sind zwei verschiedene Dinge

Alle elf Positionen werden mit **`KomponentenID = 7` (BHKW)** und der Gruppe
„Betriebskosten VDI 2067" geschrieben — auch „Instandhaltung Heizkessel", die sich an der
**Kessel**investition bemisst. Zwei Gründe:

1. **Kein Betrag darf unsichtbar werden.** `Form_Kosten.LiesKomponentenSummen`,
   `Gesamtkosten` und die Kachelanzeige auf der Seite „Berichte & Kosten" gruppieren über
   `KomponentenID`; eine Zeile mit `KomponentenID = 0` fiele aus allen Summen der Maske
   heraus — genau der Fehler, der bei der Kategorie 3 „Energiekosten" als offener Punkt
   notiert ist. *(In die Jahresrechnung ginge sie über `LiesBetriebskosten` sehr wohl ein —
   ein sichtbarer Betrag in der Rechnung und ein unsichtbarer in der Maske wäre die
   schlimmste der Möglichkeiten.)*
2. **Die Maske ist der BHKW-Betriebskostendialog** (Konzept, Abschnitt 5). Sie wird über
   den Knopf in der Hauptgruppe „BHKW" auf dem Reiter „Betriebskosten" geöffnet; ihre
   Positionen sind die Betriebskosten der KWK-Anlage.

Welche Größe eine Position trägt, ist deshalb nicht an ihrer Komponente ablesbar, sondern
steht **persistent in `Menge`** und im Dialog im Klartext.

---

## 4 Negative Beträge für Erlöse — die Vorzeichenkette

### 4.1 Die Konvention

**Der gespeicherte Betrag ist immer die Zahlungswirkung in €/a: positiv = Ausgabe,
negativ = Einnahme.** `IstErloes` ist das Kennzeichen, das die Eingabeklemme umdreht und
das Vorzeichen erzwingt.

### 4.2 Die Kette, Glied für Glied geprüft

| Glied | Vorher | Jetzt |
|---|---|---|
| **Eingabe** `ucKostenItem.cs:23-66` | `numBetrag.Minimum` stand (aus dem Designer) auf 0; `Klemme(…)` machte aus jedem negativen Betrag stillschweigend eine 0 — und der `ValueChanged`-Handler schrieb diese 0 sofort in die Datenbank | Bei `IstErloes` gilt `Minimum = −100.000.000`, `Maximum = 0`; für Kostenpositionen bleibt die Klemme unverändert bei ≥ 0. Ein Erlös kann damit **nicht** als Kosten erfasst werden und eine Kostenposition **nicht** als Erlös |
| **Persistenz** `KostenPositionCtrl.cs:643` | `UPDATE … SET EingegebenerWert` | `SetzeBetragMitZusatz` schreibt Betrag **und** Herleitung in EINEM `UPDATE` — beide können nie auseinanderlaufen |
| **Rechenweg** `BetriebskostenCtrl.cs:261` | — | `Betrag(…)` zwingt bei `istErloes` das negative Vorzeichen, gleichgültig mit welchem Vorzeichen Menge und Satz erfasst wurden |
| **Jahresrechnung** `WirtschaftlichkeitCtrl.cs:1851` | `summe += Szenariowert(…)` | ebenso, aber mit erzwungenem Vorzeichen auch für den `BETRAG`-Zweig: `erloes && wert > 0 ? −wert : wert` |
| **Kapitalwert** `KapitalwertRechner.cs:126` | `ausgaben = betriebJahr × (1+p_B)^(t−1)` | **unverändert.** Ein negativer Summenanteil senkt die Ausgabenreihe; der Kapitalwert steigt entsprechend. Vorzeichenrichtig ohne Eingriff in den Rechner |
| **Planwertschicht** `TechnikPlanwertCtrl.cs:415` | `if (betrag <= 0) return;` | aufgeteilt in `if (betrag == 0) return;` (0 = ungepflegt) und `if (!erloes && betrag < 0) return;` (negative **Kosten** sind ein Datenfehler). Der Schalter ist die eine Stelle, an der die Regel steht |

**Zum Schalter in `TechnikPlanwertCtrl.Basis`:** Kein Aufrufer der Etappe E3 setzt ihn —
die elf VDI-Positionen sind sämtlich Kosten, und die Gerätetabellen führen kein Erlösfeld.
Für alle heutigen Aufrufer ist der Guard **verhaltensgleich** zum Altstand (`erloes = false`
⇒ `betrag == 0 || betrag < 0` ⇔ `betrag <= 0`). Er existiert, damit die Erlöszeilen der
Etappen E4 (Steuergutschriften) und E5 (vermiedener Strombezug, Einspeiseerlös) die Regel
nicht an einer zweiten Stelle nachbauen müssen.

---

## 5 Der Dialog „Betriebskosten VDI 2067"

**Ohne Designer-Datei**, wie `Form_PlanwertUebernahme` und `Form_Gesetzesparameter`: Die
Maske ist ein Raster aus elf gleichartigen Zeilen; der WinForms-Designer brächte drei
weitere Dateien ohne Gegenwert. Jedes Steuerelement trägt einen `Name`, damit der
Reflection-Harnisch es findet.

### 5.1 Aufbau

```
Kopfzeile (Hinweis: netto verbindlich, Brutto abgeleitet, Satz hat Vorrang)
+---------------------+-----------+-------+---------------+---------------+------------------------+
| Position            | Bemessung | Satz  | Betrag netto  | Betrag brutto | Bezugsgröße + Empfehlung|
+---------------------+-----------+-------+---------------+---------------+------------------------+
| Vollwartung/Wartung | [Auswahl] |  0,04 |   70.522,87 * |   83.922,22 * | Stromerzeugung BHKW …  |
| Instandhaltung BHKW | % der Inv.|  5,00 |   14.750,00 * |   17.552,50 * | Investition BHKW: … VDI 3,0–9,0 % |
| …                   |           |       |               |               |                        |
+---------------------+-----------+-------+---------------+---------------+------------------------+
Summe netto / Summe brutto (Umsatzsteuersatz aus dem Katalog)
Hinweis: Wartung und Instandhaltung sind zwei EIGENE Positionen; ihre Beträge addieren sich.
Hinweis: „Vollbenutzungsstunden" sind eine Näherung — Wärme geteilt durch Leistung.
[Übernehmen] [Abbrechen]                                        * = abgeleitet, gesperrt
```

### 5.2 Die vier Regeln aus L7 und den Altbefunden

| Regel | Umsetzung | Fundstelle |
|---|---|---|
| **Genau eine Wartungsbemessung**, sichtbar ausgewählt, die anderen gesperrt | Nur die Wartung bekommt eine ComboBox (drei Einträge); alle übrigen Positionen kennen genau eine Bemessung und zeigen sie als Beschriftung. Es gibt also **kein** zweites Feld, das man versehentlich füllen könnte — das stille Überschreiben der Altanwendung (Befund 6) ist konstruktiv ausgeschlossen | `Form_Betriebskosten.cs:238-273` |
| **Instandhaltung BHKW ist eine eigene Position neben der Wartung** | Zwei Zeilen mit zwei Beträgen, und der Fußhinweis sagt wörtlich: „Wartung und Instandhaltung BHKW sind zwei EIGENE Positionen; ihre Beträge addieren sich." Damit ist die Beziehung eindeutig sichtbar statt — wie in der Altanwendung mit ihrem „oder" (Befund 7) — verschleiert | `Katalog` `BetriebskostenCtrl.cs:159-175`, Hinweis `VDI_HINWEIS_INSTANDHALTUNG` |
| **Prozentangabe schlägt Absolutangabe, ohne stilles Leeren** | Sobald ein Satz > 0 steht, wird das Absolutfeld `ReadOnly`, grau hinterlegt und trägt den Hinweis „Durch die Satzangabe ersetzt — der Betrag wird berechnet … Satz auf 0 setzen, um wieder einen festen Betrag einzugeben." Der Wert bleibt sichtbar, er wird nur nicht mehr von Hand geändert | `Form_Betriebskosten.cs:358-416` |
| **Bezugsgrößen einheitlich netto, Umsatzsteuer aus dem Katalog** | Jede Bezugsgröße ist eine Nettogröße (Investitionssummen aus `Tab_ProjektWerte`, Brennstoffkosten aus `KostenEmissionRechner`). Die Bruttospalte ist abgeleitet und gesperrt; der Satz kommt aus `GesetzKatalog.Wert(UMSATZSTEUER_REGELSATZ, aktuelles Jahr)`. Fehlt er, steht dort „Umsatzsteuersatz nicht im Katalog gepflegt — kein Bruttobetrag" statt einer stillschweigend angenommenen 1,19 | `Form_Betriebskosten.cs:76-77`, `:410-414`, `:418-437` |

### 5.3 Die Näherung ist gekennzeichnet

„Je Betriebsstunde" rechnet mit `Tab_ErgebnisBHKWModul.VbhThermisch` — `Wärme / P_therm`.
Der Rechenkern bildet **keine Taktung** ab; ein Modul, das ein Jahr lang halb moduliert
läuft, hat 8.760 Betriebsstunden und 4.380 thermische Vbh. Das steht an **drei** Stellen:

* im Anzeigenamen der Bezugsgröße („Vollbenutzungsstunden BHKW (Näherung)"),
* als eigener Fußhinweis der Maske (`VDI_VBH_NAEHERUNG`),
* an der Konstante selbst (`DbWerte.BEMESSUNG_EUR_PRO_H`).

Damit ist der offene Punkt 6 aus dem E2-Protokoll („Echte Betriebsstunden gibt es im Modell
nicht … das ist zu kennzeichnen, sobald der Dialog entsteht") erledigt.

---

## 6 Rechenweg je Bemessungsart

### 6.1 Die Formel

`BetriebskostenCtrl.Betrag` (`:261`) ist eine **reine Funktion** ohne Datenbankzugriff,
ohne Kultur und ohne Zustand (L9):

| `Bemessung` | Formel | `Menge` | `Einheitpreis` |
|---|---|---|---|
| `BETRAG` | `EingegebenerWert` | — (NULL) | — (NULL) |
| `PROZENT_INVESTITION` | `Menge × Satz / 100` | Bezugsinvestition [€] | Satz [%] |
| `EUR_PRO_H` | `Menge × Satz` | Vollbenutzungsstunden [h/a] | Satz [€/h] |
| `EUR_PRO_KWH` | `Menge × Satz` | Jahresarbeit [kWh/a] | Satz [€/kWh] |
| `PROZENT_BRENNSTOFFKOSTEN` | `Menge × Satz / 100` | Brennstoffkosten [€/a] | Satz [%] |

Drei Festlegungen, die im Zweifel greifen:

* **Leer gilt als `BETRAG`.** Eine nicht migrierte Datenbank und jede Zeile, die eine
  ältere Programmfassung angelegt hat, rechnen damit exakt wie vor E3.
* **Fehlt Menge oder Satz, ist die Position `0`** — nicht „der zuletzt gespeicherte
  Betrag". Sonst bliebe ein Wert stehen, dessen Herleitung niemand mehr nachvollziehen kann.
* **Ein unbekannter Wert in `Bemessung`** wird wie `BETRAG` behandelt, nie stillschweigend
  als 0.

### 6.2 Warum die Bezugsgröße persistiert wird

`Menge` und `Einheitpreis` stehen **in der Zeile**, nicht in einer Ableitung zur Laufzeit.
Das hat drei Folgen, die zusammen den Ausschlag geben:

1. Die Herleitung ist **nachvollziehbar gespeichert** — genau die Forderung aus L5
   („damit die Herleitung ‚0,041 €/kWh × 72.000 kWh' persistent ist statt nur Anzeigetext").
2. `LiesBetriebskosten` rechnet **ohne einen einzigen zusätzlichen Datenbankzugriff**
   dieselbe Zahl wie die Kostenmaske. Es gibt keine zweite Wahrheit.
3. Ändert sich die Bezugsgröße (neuer Simulationslauf, geänderte Investition), ändert sich
   der Betrag **nicht von selbst**. Das ist gewollt: Ein gespeicherter Kostenwert darf sich
   nicht hinter dem Rücken des Anwenders bewegen. Beim nächsten Öffnen des Dialogs steht die
   frische Bezugsgröße in der Spalte „Bezugsgröße", und „Übernehmen" schreibt den neuen
   Betrag — auf ausdrückliche Handlung, wie bei der Planwertübernahme.

Zusätzlich wird der abgeleitete Betrag in `EingegebenerWert` **materialisiert**, damit
Kostenliste, Komponentensummen, Kacheln und Berichte ohne Änderung dieselbe Zahl zeigen.
Auseinanderlaufen können die beiden nicht: Das Betragsfeld einer abgeleiteten Position ist
in `ucKostenItem` gesperrt (`:51-66`).

### 6.3 Szenarien und Preissteigerung

**Szenarien.** `LiesBetriebskosten` behält das VALERI-Muster: Ein **gepflegter** Best- oder
Worst-Case-Betrag (≠ 0 und ≠ Erwartungswert) schlägt die Ableitung; ist keiner gepflegt,
gilt der abgeleitete Erwartungswert in allen drei Szenarien. Für `BETRAG`-Zeilen ist der
Code Zeile für Zeile der alte. Nachgemessen: 27 von 27 Werten (9 Projekte × 3 Szenarien)
identisch zum Vorgängerstand.

**Preissteigerung.** `KapitalwertRechner.Rechne` (`:126`) steigert die **Summe**
`betriebJahr` mit `p_B`; abgeleitete Positionen steigen deshalb genauso wie feste. Der
Rechner wurde nicht angefasst.

> **Ein fachlicher Vorbehalt, der benannt sein will.** Die **Hilfsenergiekosten** sind nach
> VDI 2067 *bedarfsgebunden* — sie folgen der Energiepreisentwicklung, nicht der
> Betriebskostenentwicklung. In EPOS-Plan sind sie eine Kategorie-2-Position und steigen
> deshalb mit `p_B`. Das zu trennen hieße, `KapitalwertRechner.Rechne` um eine zweite
> Betriebskostenreihe zu erweitern — eine Signaturänderung, die über E3 hinausgeht. Sie ist
> als offener Punkt für E4 notiert (Abschnitt 9). Solange beide Preissteigerungen gleich
> gepflegt sind, ist der Unterschied null.

---

## 7 Verifikation

Alle Proben auf **Wegwerf-Kopien** unter `C:\Waermeplan\_e3`; die Produktivdatenbank wurde
ausschließlich lesend kopiert (`Kenndaten.laccdb` vor jedem Zugriff geprüft — nicht
vorhanden) und ist nachweislich unverändert: MD5 `66f4806a…` und Zeitstempel
19.08.2026 02:51 vor **und** nach allen Läufen. Build ausschließlich mit
`-p:OutDir=<Scratch>`, `bin\` unberührt.

### 7.1 Verifikationstabelle

| # | Prüfung | Methode | Ergebnis |
|---|---|---|---|
| **V1** | **Simulationsergebnisse unverändert** | `Referenzlauf.exe vergleich 2026-08-19_B5 <neu>`, feste Liste `1007,1008,1011,1017,1018,1021,1023,1024,1030`, Flag AUS | **9/9 PASS**, 2 366 177 Werte |
| **V2** | **Byte-Identität** | `cmp` je Datei | **216/216 byte-gleich**, 0 Abweichungen, 0 zusätzliche, 0 fehlende Dateien |
| V3 | Migration **18 → 19** | Kopie mit dem HEAD-Stand (`ZIEL_VERSION 18`) auf 18 gebracht, dann mit dem E3-Stand migriert | Schemastand **18 → 19**, Ergebnis ERFOLG |
| V4 | Spalten korrekt angelegt | `GetOleDbSchemaTable` | `Tab_ProjektWerte` 13 → **18** Spalten: `Kostenart` (14, TEXT 20), `Bemessung` (15, TEXT 30), `IstErloes` (16, YESNO), `Menge` (17, DOUBLE), `Einheitpreis` (18, DOUBLE) — alle **angehängt** |
| V5 | **Bestandswerte unversehrt** | Vollständiger Dump aller 13 Altspalten × 67 Zeilen vor und nach der Migration, `diff` | **identisch**, 0 Unterschiede |
| V6 | Vorbelegung 19b | `GROUP BY Bemessung, Kostenart` | 67 × `BETRAG`; 51 × `KAPITALGEBUNDEN` (Kat. 1), 16 × `BETRIEBSGEBUNDEN` (Kat. 2) |
| V7 | ACE-Regeln nachgemessen | `COUNT(*) WHERE … IS NULL` | `IstErloes`: **0** NULL, 67 × `False` (ACE belegt selbst) · `Menge`, `Einheitpreis`: **67** NULL (kein Backfill) |
| V8 | **Doppelstart idempotent** | zweiter Migrationslauf mit `--nokopie` | „Schritt 19 …: bereits erledigt", Stand bleibt 19, Spalten **nicht** doppelt, 0 Zeilen vorbelegt |
| **V9** | **Wirtschaftlichkeitsprobe: Bestandsprojekte unverändert** | Reflection-Harnisch gegen HEAD **und** E3 auf derselben unveränderten Kopie: `LiesBetriebskosten` und `KapitalwertRechner.Rechne` je 9 Projekte × 3 Szenarien | **27/27 identisch**, `diff` leer |
| V10 | Rechenweg je Bemessungsart (reine Funktion) | 11 Proben A1–A11 | alle OK: `BETRAG`, `PROZENT_INVESTITION`, `EUR_PRO_H`, `EUR_PRO_KWH`, `PROZENT_BRENNSTOFFKOSTEN`, leere Bemessung, fehlende Menge, fehlender Satz, drei Erlös-Vorzeichenfälle |
| V11 | Bezugsgrößen an echten Daten (Projekt 1030) | Harnisch B1–B7 | Investition gesamt **410.000,00 €** (295.000 + 90.000 + 25.000), BHKW **295.000,00 €**, Kessel **90.000,00 €**, Strom **1.720.070 kWh/a**, Vbh **12.860,72 h** *(= die im B5-Laufprotokoll ausgewiesene Summe thermisch)*, Brennstoffkosten **355.055,20 €/a** |
| V12 | „nicht ermittelbar" statt 0 | Projekt 1021 (ohne BHKW) | `StromKwh` = **null**, nicht 0 |
| **V13** | **Wirkungsnachweis, je eine Position pro Bemessungsart** | präparierte Kopie, Projekt 1030, nachgerechnet | siehe 7.2 — Summe **123.058,53 €/a** gegen erwartete **123.058,53 €/a** |
| V14 | Rückgelesene Herleitung | Harnisch C4–C7 | Satz 0,0410, Bemessung `EUR_PRO_KWH`, fester Betrag 1.234,00 €, Kesselinstandhaltung an der **Kessel**investition (1.800,00 €) |
| **V15** | **Erlös senkt die Kosten, statt sie zu erhöhen** | Position mit `IstErloes = True`, Betrag −500 €/a | gespeichert und zurückgelesen; `LiesBetriebskosten` **122.558,53** statt 123.058,53 — genau −500 |
| V16 | Eingabeklemme | `ucKostenZeile` headless, drei Fälle | Erlös: Untergrenze negativ, **−500 € bleibt erhalten** (vorher auf 0 geklemmt) · Kostenposition: Klemme unverändert, −500 → 0 · abgeleitete Position: Betragsfeld **gesperrt**, feste Position änderbar |
| V17 | Maske headless, deutsch **und** englisch | `Form_Betriebskosten` per Reflection aufgebaut | 11 Zeilen, **genau eine** Bemessungsauswahl (nur die Wartung), 22 Zahlenfelder, mindestens ein gesperrtes Betragsfeld, Summe netto 103.058,53 €/a, brutto 122.639,65 €/a mit **19,0 %** aus dem Katalog, Titel „Betriebskosten nach VDI 2067" / „Operating costs to VDI 2067" |
| V18 | Keine unerwarteten Dialoge | Wächter-Thread auf `#32770` über den ganzen Lauf | **0** Dialoge |
| V19 | Ressourcen in beiden Sprachen und im Designer | Auszählung | 43 Schlüssel in `Resource.resx`, 43 in `Resource.en-US.resx`, 43 im Designer |
| V20 | `Resource.Designer.cs` und beide `.resx` ohne Dubletten | alle Namen sortiert, `uniq -d` | **0 Dubletten** in allen drei Dateien |
| V21 | **Build** | `MSBuild WP-Plan.sln -t:Rebuild -p:Platform=x86 -p:OutDir=<Scratch>` | **0 Fehler, exakt 6 Bestandswarnungen** (CS0108 ×2, CS0109 ×2, CS4014, CS1998) |
| V22 | **Engine unberührt** | `git diff --stat -- 'WindowsFormsApplication1/Allgemein/Simulation/*.cs'` | **leer** |
| V23 | Kodierung und Zeilenenden | `file` je geänderter Datei, CR-Zählung, Suche nach U+FFFD | alle unverändert (UTF-8 mit und ohne BOM wie zuvor), **alle Zeilen CRLF**, **0 Ersatzzeichen** |
| V24 | Produktivdatenbank nur gelesen | `Kenndaten.laccdb` vor jedem Zugriff geprüft (nicht vorhanden), MD5 vorher/nachher | **unverändert** (`66f4806a3b89074b52344f39d477f151`, 19.08.2026 02:51) |
| V25 | `bin\` des Repos unberührt | Build ausschließlich mit `-p:OutDir=<Scratch>` | **erfüllt** |

**47 Einzelproben im Harnisch, 0 Fehlschläge, 0 unerwartete Dialoge.**

### 7.2 Der Wirkungsnachweis mit Zahlen (V13)

Präparierte Kopie, Projekt 1030 „Referenz BHKW-Kaskade": je eine Position pro
Bemessungsart, dazu die beiden Bestandspositionen (Wartung BHKW 18.000 €/a, Wartung Kessel
2.000 €/a = 20.000 €/a vor der Präparierung).

| Position | Bemessung | Satz | Bezugsgröße | Nachgerechnet | Gespeichert |
|---|---|---|---|---|---|
| Wartung BHKW | `EUR_PRO_KWH` | 0,041 €/kWh | 1.720.070 kWh/a | 0,041 × 1.720.070 = **70.522,87 €/a** | 70.522,87 |
| Instandhaltung BHKW | `PROZENT_INVESTITION` | 5,0 % | 295.000 € (BHKW) | 295.000 × 5/100 = **14.750,00 €/a** | 14.750,00 |
| Instandhaltung Heizkessel | `PROZENT_INVESTITION` | 2,0 % | 90.000 € (Kessel) | 90.000 × 2/100 = **1.800,00 €/a** | 1.800,00 |
| Hilfsenergiekosten | `PROZENT_BRENNSTOFFKOSTEN` | 3,0 % | 355.055,20 €/a | 355.055,20 × 3/100 = **10.651,66 €/a** | 10.651,66 |
| Personalkosten | `PROZENT_INVESTITION` | 1,0 % | 410.000 € (gesamt) | 410.000 × 1/100 = **4.100,00 €/a** | 4.100,00 |
| Reserveleistungskosten | `BETRAG` | — | — | **1.234,00 €/a** | 1.234,00 |
| | | | | **Σ neu 103.058,53** | |

`LiesBetriebskosten(1030, "Erwartet")` liefert danach **123.058,53 €/a** = 20.000,00
(Bestand) + 103.058,53 (neu). Erwartet **123.058,53**. Der Dialog zeigt für dieselben Daten
netto **103.058,53 €/a** und brutto **122.639,65 €/a** (× 1,19 aus dem Katalog).

Die fünfte Bemessungsart `EUR_PRO_H` ist in der reinen Funktion belegt (A3:
0,02 €/h × 6.430,36 h = 128,61 €/a) und über die Bezugsgröße `VbhSumme` = 12.860,72 h an
echten Daten (V11); sie ist in dieser Zahlenprobe nicht zusätzlich belegt, weil je Position
nur **eine** Bemessung gelten darf und die Wartung bereits `EUR_PRO_KWH` trägt.

---

## 8 Ergebniswirkung — ausdrücklich

**Für Bestandsprojekte gibt es keine.** Drei voneinander unabhängige Belege:

1. **Der Referenzlauf ist byte-identisch.** 9/9 PASS über 2 366 177 Werte und 216 von 216
   CSV-Dateien byte-gleich zur Basis `2026-08-19_B5`. Die Engine wurde nicht angefasst, und
   die fünf neuen Spalten stehen in einer Tabelle, die der Rechenkern nicht liest.
2. **Betriebskosten und Kapitalwert sind identisch.** Der A/B-Vergleich des HEAD-Standes
   gegen den E3-Stand auf **derselben** unveränderten Datenbankkopie liefert für 9 Projekte
   × 3 Szenarien 27 von 27 gleiche Werte.
3. **Die Migration lässt den Bestand in Ruhe.** Alle 13 Altspalten aller 67 Kostenpositionen
   sind vor und nach Schritt 19 wertgleich; die Vorbelegung schreibt ausschließlich in die
   neuen, zuvor leeren Spalten und ist beim zweiten Lauf ein No-op.

Der einzige Weg, an dem sich ein Ergebnis ändert, führt über den neuen Dialog: Wer dort eine
Position pflegt, bekommt sie als Betriebskostenzeile — sichtbar in der Kostenverwaltung, in
den Komponentensummen und in der Wirtschaftlichkeit.

---

## 9 Offene Punkte

### Für Etappe E4

1. **Preissteigerung der Hilfsenergie.** Sie ist nach VDI 2067 bedarfsgebunden, steigt bei
   uns aber mit der Betriebskosten-Preissteigerung (Abschnitt 6.3). Eine Trennung braucht
   eine zweite Kostenreihe in `KapitalwertRechner.Rechne`.
2. **`IstErloes` ist gepflegt, aber noch ohne eigene Maske.** Die Spalte, die Klemme, die
   Vorzeichenkette und die Jahresrechnung stehen; erfasst wird ein Erlös bisher als
   Kostenposition mit negativem Betrag. Die Erlöszeilen der Etappen E4 und E5
   (Steuergutschriften, vermiedener Strombezug, Einspeiseerlös) bekommen ihre eigene
   Eingabe.
3. **`Kostenart` hat noch keine Oberfläche.** Sie wird gepflegt (Migration und Dialog
   setzen sie), aber nirgends angezeigt oder gefiltert. Ihr Zweck ist die Gliederung des
   Berichts in Etappe E7.
4. **Der Bezug „Investitionssumme des Projekts" ist grob** für Wärmezentrale, bauliche
   Anlagen und Stromeinspeisung (Abschnitt 3.1). Wenn eine feinere Bemessung gebraucht
   wird, wäre der saubere Weg ein **Kennzeichen an der Kostengruppe** („diese Gruppe ist
   eine Investitionsgruppe im Sinn der VDI 2067") — eine Spalte an
   `Tab_KostenGruppenKatalog` und eine Pflegemaske. Das ist eine eigene Entscheidung, keine
   Nebenwirkung von E3.

### Aus dieser Etappe entstanden

5. **Ein SQL-Fehler in `DataRepository.ExecuteSQL` erscheint als modaler Dialog** und
   blockiert im Stapelbetrieb den ganzen Prozess. Bei der zu schmalen `Bemessung`-Spalte
   hat das den Harnisch zehn Minuten hängen lassen, bevor das Zeitlimit griff. Der Befund
   ist bekannt (Nebenbefund der Kostenübernahme, Abschnitt 6); die Vorsorgen dieser Etappe
   umgehen ihn durch eine eigene `OleDbConnection`, die Schreibwege des Kostenmoduls tun es
   nicht.
6. **`Abfrage_Kostenfaktoren` kennt die neuen Spalten nicht.** Die gespeicherte
   Access-Abfrage liegt außerhalb des Repos; sie zu erweitern erreicht keine
   Bestandsinstallation. `Form_Kosten.LoadKostenFaktoren` holt die fünf Felder deshalb über
   einen **zweiten** Zugriff direkt auf `Tab_ProjektWerte` und führt sie über die ID
   zusammen. Das ist ein Zugriff mehr je Komponentenwechsel — für elf bis zwanzig Zeilen
   unerheblich, aber es ist eine Stelle, an der zwei Lesewege nebeneinanderstehen.
7. **`Form_Kosten.GetKomponentenID` verdrahtet die Komponenten-IDs 1…7 hart**, während
   `UcBkKosten` und `KomponentenUebernahmeCtrl` dieselbe Zuordnung dynamisch aus
   `Tab_KostenKomponente` lesen. `BetriebskostenCtrl` musste sich für einen Weg entscheiden
   und hat die beiden gebrauchten IDs als benannte Konstanten übernommen
   (`KOMPONENTE_HEIZKESSEL = 2`, `KOMPONENTE_BHKW = 7`). Die doppelte Wahrheit besteht
   fort und gehört in einem eigenen Vorgang aufgelöst.
8. **Kein Referenzprojekt pflegt VDI-2067-Betriebskosten.** Die Wirkung dieser Etappe ist
   ausschließlich an präparierten Kopien belegbar — dieselbe Lücke, die schon für die
   BHKW-Kaskade und für `Heizkessel.Quellwaerme` festgehalten ist. Ein Referenzprojekt mit
   gepflegten Bemessungsarten wäre der belastbare Regressionstest.

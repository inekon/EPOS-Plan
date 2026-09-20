# Schemaschritt 99 — der BHKW-Katalog führt beide Wirkungsgrade

**Anwenderentscheid vom 20.09.2026:** „Der Wirkungsgrad sollte sich aus dem elektrischen
und dem thermischen Wirkungsgrad ergeben."

Fortsetzung von [`BW1_BhkwWirkungsgrad_Protokoll.md`](BW1_BhkwWirkungsgrad_Protokoll.md)
(Schritt 98: der Gesamtwirkungsgrad ist ein Faktor). Dieses Papier beschreibt, was Schritt
99 anlegt, aufteilt, prüft und ausdrücklich **nicht** tut.

## 1 Der Befund

Ein BHKW liefert aus einer Brennstoffmenge zweierlei: Strom und Wärme. Der **elektrische**
Wirkungsgrad sagt, welcher Teil des Brennstoffs zu Strom wird, der **thermische**, welcher
zu Wärme; erst beide zusammen ergeben den **Gesamtwirkungsgrad**, mit dem der Rechenweg
arbeitet:

```
Verbrauch = (Wärme + Strom) / Wirkungsgrad        (SimulationBHKW.Auswertung)
```

Gepflegt wurde bis hierher allein der Gesamtwert. Das Datenblatt eines Moduls nennt aber
beide Anteile, und Schritt 98 hat den Gesamtwirkungsgrad genau aus ihnen gewonnen — der
Katalog führte den elektrischen Anteil, ohne ihn so zu nennen. Ab Schritt 99 stehen beide
Anteile als eigene Spalten da; der Gesamtwirkungsgrad ist ihre **Summe**.

## 2 Die Formeln

**Hinrichtung — die eine Wahrheit:**

```
Wirkungsgrad = Wirkungsgrad_el + Wirkungsgrad_th
```

Mehr ist es nicht. `BhkwWirkungsgrad.Gesamt(el, th)` rechnet es; jeder Schreibweg des Kerns
zieht die Spalte `Wirkungsgrad` daraus nach (`GesamtZumSchreiben`), sind beide Anteile
gepflegt. Ohne sie bleibt der Altbestandswert unverändert stehen — der Rechenweg liest ihn
weiter wie zuvor.

**Rückrichtung — ein Vorschlag, keine Behauptung:**

```
Wirkungsgrad_el = ROUND(Wirkungsgrad · Pel    / (Pel + Ptherm), 4)
Wirkungsgrad_th = ROUND(Wirkungsgrad · Ptherm / (Pel + Ptherm), 4)
```

Strom- und Wärmestrom eines Moduls stehen im selben Verhältnis wie `Pel` und `Ptherm`;
die Aufteilung folgt diesem Verhältnis. Sie steht zweimal in derselben Form:
als C#-Rechnung in `BhkwWirkungsgrad.Aufteilen` (Dialog und Controller) und als SQL im
Datenteil des Schrittes (`BhkwWirkungsgradAnteile`).

**Die Probe am Bestand.** Der Katalogsatz „EC-POWER XRGI 15" führt 30,8 kW thermisch,
14,5 kW elektrisch und — seit Schritt 98 — 0,9216 gesamt. Die Aufteilung ergibt

```
0,9216 · 14,5 / 45,3 = 0,2950      0,9216 · 30,8 / 45,3 = 0,6266
```

also **genau den Datenblattwert 0,295**, aus dem Schritt 98 seinen Gesamtwirkungsgrad
gewonnen hat. Die Aufteilung ist damit nicht nur an der Formel belegt, sondern am Bestand.

**Das Band.** Jeder Anteil liegt in **(0; 1)** — ein Anteil von 0 ist keiner, und ein
einzelner Anteil von 1 ließe für den anderen nichts übrig. Die **Summe** liegt in
**(0; 1,05]**; 1,05 lässt das Brennwertgerät zu, dessen Gesamtwirkungsgrad auf den Heizwert
bezogen über 1 liegt. Die Obergrenze kommt aus derselben Quelle wie in Schritt 98
(`BhkwWirkungsgradFaktor.BAND_BIS`) — die Abweisung, die dort am Dialog und am Aufklapper
stand, steht seit BW-2 an **einer** Stelle: `BhkwWirkungsgrad.Pruefen`, drei Regeln, drei
benannte Meldungen in beiden Sprachen (`BHKWW_MSG_EL`, `BHKWW_MSG_TH`, `BHKWW_MSG_SUMME`).

## 3 Der Schritt

**DDL.** Vier Spalten, je zwei an `Tab_BHKW_STAMM` und `Tab_BHKW`: `Wirkungsgrad_el` und
`Wirkungsgrad_th`, `REAL`, nullbar. **Katalog und Projektkopie im selben Schritt** — eine
Spalte nur auf einer Seite wäre beim Kopieren ins Projekt sofort ein Datenverlust. Quelle:
`SchemaKatalog.Schritt99_BhkwWirkungsgradAnteile`.

**DML.** Aufgeteilt wird jede Zeile, deren **beide neue Spalten NULL** sind und die
Gesamtwirkungsgrad, `Pel` und `Ptherm` gepflegt führt. Quelle:
`BhkwWirkungsgradAnteile` — dieselbe Klasse liefert Zählung, Ausweisung, Bestandsaufnahme
und Bericht. **Drei Leser:** der Schemaschritt in `SchemaMigration`
(`Schritt_99_BhkwWirkungsgradAnteile`, Access-Zweig, deshalb nicht im Kern), das Werkzeug
`Werkzeuge/Testdatenbankschema` und der Nachweis in `EPOS.Kern.Tests`.

**Wiederholbar.** Nach dem Lauf trifft die Bedingung keine Zeile mehr; ein zweiter Lauf
lässt alles stehen — auch eine Aufteilung, die der Anwender inzwischen von Hand gepflegt
hat und die dem Verhältnis der Leistungen nicht folgt.

**Nichts wird erfunden.** Fehlt der Gesamtwirkungsgrad, `Pel` oder `Ptherm`, bleiben beide
Spalten NULL, und die Zeile wird mit **Id, Bezeichner und Grund** ausgewiesen — im
Migrationsbericht (`migration_protokoll.txt`) wie in der Ausgabe des Werkzeugs.

## 4 Zählung an der Testdatenbank

| Tabelle | Zeilen | aufgeteilt | ausgewiesen |
|---|---:|---:|---:|
| `Tab_BHKW_STAMM` | 79 | **78** | **1** |
| `Tab_BHKW` | 6 | **6** | **0** |

Die eine ausgewiesene Zeile: `Tab_BHKW_STAMM` Id **157** „SenerTec Dachs 0.8 GenD" —
**kein Gesamtwirkungsgrad gepflegt**. Ihr fehlt die Größe, aus der sich etwas teilen ließe;
sie bleibt leer und wird benannt genannt.

**Die Probe des Schrittes:** In jeder aufgeteilten Zeile beider Tabellen ergeben die zwei
Anteile den Gesamtwirkungsgrad, auf **1e‑4** genau. Die Schranke ist kein Zugeständnis,
sondern die Rechnung: Beide Anteile sind je für sich auf vier Stellen gerundet, und zwei
Rundungen tragen zusammen bis zu 1e‑4. `SummeWeichtAb` hält gegen dieselbe Schranke und
meldet für beide Tabellen **0**.

## 5 Was der Schritt nicht tut

- **Er fasst den Rechenweg nicht an.** `SimulationBHKW` liest die Spalte `Wirkungsgrad`,
  und die ändert der Schritt nicht. **Der Referenzlauf bleibt byte-gleich**, die Basis
  `2026-09-19_R10_BhkwWirkungsgrad` gilt weiter.
- **Er rechnet keinen Gesamtwirkungsgrad neu** — was Schritt 98 hinterlassen hat, steht
  unverändert da.
- **Er erfindet keinen Wert**, wo eine Angabe fehlt.
- **Er nimmt dem Aufklapper „Alle Daten" das Feld nicht weg:** Dort bleibt der
  GESAMTwirkungsgrad eingebbar; wer ihn ändert, verschiebt beide Anteile im Verhältnis der
  Leistungen mit (`BHKWStammCtrl.FelderUebernehmen`, Punkt 5). Drei Zahlen, von denen zwei
  einander widersprechen, gibt es damit an keiner Stelle. — **Ein zweiter Anwenderentscheid
  desselben Tages hat das anders entschieden: Abschnitt 10.**

## 6 Die Felder des BHKW-Katalogs nach BW-2

Grundlage der künftigen Wikiseite „Programm Dokumentation/Gerätekataloge" (eigener
Auftrag). Der **Katalogeditor** („Bearbeiten…") zeigt:

| Feld | Einheit | Bemerkung |
|---|---|---|
| Modulname | — | Schlüssel; im Modus „Bearbeiten" nur lesbar |
| Hersteller | — | |
| Motortyp | — | |
| Beschreibung | — | mehrzeilig |
| Thermische Leistung | kW | |
| Elektrische Leistung | kW | |
| **Elektrischer Wirkungsgrad** | Faktor (0…1) | Eingabe, Hinweis „z. B. 0,30" |
| **Thermischer Wirkungsgrad** | Faktor (0…1) | Eingabe, Hinweis „z. B. 0,60" |
| **Ges. Wirkungsgrad** | Faktor (0…1,05] | **berechnet**, nicht editierbar; Summe der beiden |
| Untere Grenzleistung | % | |
| Energieträger | — | Auswahl aus dem Brennstoffkatalog |
| Vorlauf | °C | ganzzahlig |
| Rücklauf | °C | ganzzahlig |

Der Aufklapper **„Alle Daten anzeigen"** des Projektdialogs führt dazu: Ges. Wirkungsgrad,
Investition je kWel (€/kWel, nur Anzeige), Raumbedarf (m³), Wartungskosten (€/kWhel),
Nutzungsdauer (Jahre), die fünf Kostenposten (€) und die fünf Emissionsfaktoren
(CO₂, SO₂, NOx, CO, Staub, je g/MWh — nur Anzeige). **Mit BW-3 führt er auch die zwei
Anteile, und der Gesamtwirkungsgrad ist dort nur noch Anzeige: Abschnitt 10.2.**

**Masken, die den BHKW-Wirkungsgrad zeigen und unverändert bleiben**, weil sie alle den
GESAMTwert zeigen: der Aufklapper „Alle Daten" (`KatalogBrowserProfil`), die Katalogliste
des Projektdialogs (Spalte η, `Katalogfilterprofil.SpEta`, über
`Katalogfeld.WirkungsgradAlsFaktor`), die Parameterübersicht (`ParameterVerwendung`) und
der Variantenvergleich (`AbweichungsErmittler`). Der Vergleich zeigt ihn **ab BW-2 ohne die
Einheit „%"** — sie war schon vor Schritt 98 falsch (offener Punkt BW1‑O3) — und führt die
zwei Anteile mit.

## 7 Ein Befund am Rande: `const 0.0` als Parameterwert

Der Datenteil zählte zunächst **0 aufzuteilende Zeilen**, obwohl dieselbe Abfrage außerhalb
der Anwendung 78 lieferte, und die Ausweisung nannte **jede** Zeile. Die Ursache steckt
nicht im SQL:

`DbParam` hat zwei Konstruktoren — `(string, object)` und `(string, DbParamTyp)`. Ein
**konstanter Ausdruck mit dem Wert null** geht in C# implizit in jeden Aufzählungstyp über;
`new DbParam("@x", NULLGRENZE)` mit `const double NULLGRENZE = 0.0` traf deshalb die
Überladung mit `DbParamTyp`, und die setzt den **Wert auf `DBNull`**. In SQLite stand dann
`Wirkungsgrad > NULL` — für jede Zeile NULL, also weder wahr noch falsch: Die Zählung fand
nichts, die Ausweisung (`COALESCE(…, 0) = 0`) traf alles, und der Schritt fasste nichts an,
**ohne sich zu beklagen**.

`NULLGRENZE` steht seither als `static readonly` — kein konstanter Ausdruck, also gewinnt
die Überladung mit `object`. Die Stelle trägt die Begründung im Kommentar. Schritt 98 war
nie betroffen: Seine Grenzen sind 0,5, 1,0 und 1,05.

## 8 Nachweise

**Neue Fälle** `EPOS.Kern.Tests/BhkwWirkungsgradAnteileTests` (13): Zielstand 99 und die
vier Spalten; `Gesamt` samt halbem Paar; `Aufteilen` am XRGI 15 gegen den Datenblattwert
und ohne gepflegte Leistungen; `Pruefen` mit den Grenzfällen 0, 1, 1,05 und 1,051 und mit
0,30 + 0,80; `GesamtZumSchreiben`; der Schritt an drei synthetischen Sätzen samt Ausweisung,
Wiederholbarkeit und der von Hand gepflegten Aufteilung; die Arbeitskopie (Stand 99, offen 0,
Summe Zeile für Zeile); der Bestandssatz des XRGI 15; die Bestandsaufnahme; die zwei
Schreibwege (`BHKWStammCtrl.Update` zieht die Summe nach und lässt den Altbestand stehen,
`BHKWCtrl.CopyFromStamm` nimmt beide Anteile ins Projekt mit); der Rückfall des Altbestands.

**Neue Fälle** `EPOS.UI.Tests/Dialoge/BhkwKatalogDialogTests` (+8, 29 gesamt): die
mitlaufende Summe, die leere Summe ohne beide Anteile, der Vorschlag beim Öffnen eines
Altbestandssatzes, das Durchreichen beider Werte beim Speichern, die Abweisung von
0,30 + 0,80 (A‑BW2‑2) und eine Theory über die drei Regeln samt Grenzfällen.

**`ParameterVerwendung`** führt die zwei Spalten — der Wächter verlangt Vollständigkeit
**und** die Spaltenfolge der Tabelle, und ein `ADD COLUMN` hängt hinten an: Die zwei
Einträge stehen deshalb am **Ende** der BHKW-Liste, nach `ReadOnly`.

**Gate beide Kulturen grün:** EPOS.Kern 3 914, EPOS.UI 4 845, KiKern 499, SpeicherEngine
378, SpeicherPlanung 27 (1 übersprungen). Windows-Schale mit
`-p:EnableWindowsTargeting=true` **0 Fehler**. SQL-Dialekt-Prüfer **0 Fundstellen**
(1 546 Texte). ResourceDesigner wiederholbar. Schemawerkzeug-Trockenlauf nach der
Migration: **Stand 99, 0 Spalten anzulegen, 0 Zeilen aufzuteilen**.

**Referenzlauf** 1030, 1007, 1017, 1045, 1046 gegen `2026-09-19_R10_BhkwWirkungsgrad`:
**GESAMT PASS** (1 656 417 Werte) und **alle fünf Projekte byte-gleich** (143 Dateien).
Das ist die Abnahme dieses Auftrags: Er ändert die Pflege, nicht die Rechnung.

## 9 Offene Punkte

- **Die Bestandsdatenbank des Anwenders** bekommt die vier Spalten beim nächsten
  Programmstart und teilt ihren Katalog auf; der Migrationsbericht nennt die Zahlen je
  Tabelle und jede Zeile, die leer bleibt. **Gerechnet wird weiter wie zuvor** — die Spalte
  `Wirkungsgrad` ist unangetastet.
- **Eine Wikiseite zum Gerätekatalog gibt es weiter nicht** (offener Punkt seit BW-1). Die
  Feldliste in Abschnitt 6 ist die Grundlage für die Seite „Programm
  Dokumentation/Gerätekataloge"; der Logbuch-Satz zu **Version 1.2.0.3** steht in der
  Statusdatei unter „Nach #392".
- **Der Aufklapper „Alle Daten" zeigt den Gesamtwirkungsgrad weiterhin als Eingabe.**
  **Erledigt mit BW-3, Abschnitt 10.**

---

## 10 Nachtrag BW-3 — der Aufklapper zeigt den Gesamtwirkungsgrad nur noch

**Anwenderentscheid vom 20.09.2026** zum letzten offenen Punkt aus Abschnitt 9: Im
Aufklapper „Alle Daten anzeigen" wird der Gesamtwirkungsgrad zur **reinen Anzeige** — wie
im Katalogeditor. Eine Eingabe gibt es nur noch für den elektrischen und den thermischen
Wirkungsgrad.

### 10.1 Warum der Verteilungsweg fällt

Der Aufklapper nahm den GESAMTwert entgegen und verteilte ihn über
`BHKWStammCtrl.FelderUebernehmen` im Verhältnis der Leistungen auf die zwei Anteile.
Das war widerspruchsfrei, aber es war die **Rückrichtung**: eine geschätzte Aufteilung,
die eine vom Datenblatt gepflegte überschrieb, sobald jemand die Summe anfasste — auch
versehentlich, denn ein Speicherweg schreibt alle Felder des Blocks. Die Hinrichtung ist
die eine Wahrheit (Abschnitt 2); sie steht jetzt an beiden Masken allein.

### 10.2 Was im Aufklapper steht

| Feld | Art | Bemerkung |
|---|---|---|
| **Elektrischer Wirkungsgrad** | Eingabe | Faktor, Hinweis „(Faktor, z. B. 0,30)" |
| **Thermischer Wirkungsgrad** | Eingabe | Faktor, Hinweis „(Faktor, z. B. 0,60)" |
| **Ges. Wirkungsgrad** | **Anzeige** | Summe der zwei, drei Stellen; nicht beschreibbar |

Die zwei Anteile stehen **vor** der Summe — erst die Eingabe, dann, was daraus folgt.
Das BHKW-Profil führt damit **27** Detailfelder, **24** davon editierbar (bis hierher
25 / 23). Nicht editierbar sind der Bezeichner (Schlüssel des `UPDATE`) und die zwei
abgeleiteten Größen: die Investition je kWel und der Gesamtwirkungsgrad.

**Die Summe läuft mit.** `BhkwWirkungsgrad.GesamtAnzeige(el, th, altbestand)` ist die eine
Regel dafür: die Summe der zwei gepflegten Anteile auf drei Stellen, und nur wo **beide**
fehlen, der gespeicherte Altbestandswert. Ein **halbes Paar** ergibt nichts — leer statt
einer Zahl, die nur einen der zwei Anteile enthielte. Gezogen wird sie an drei Stellen:
beim Aufbau der Anzeige (`BHKWStammCtrl.KatalogsatzAnzeige`), bei jeder Feldänderung im
Browser (`KatalogBrowserDialog.SummeNachziehen`) und — als dieselbe Rechnung — im
Katalogeditor.

### 10.3 Die Signatur des Speicherwegs

`BHKWStammCtrl.AnzeigefelderBhkw` führt **`WirkungsgradEl` und `WirkungsgradTh` statt
`Wirkungsgrad`**. Der Gesamtwert lässt sich damit gar nicht mehr liefern — die Falle ist
nicht abgefangen, sondern nicht mehr baubar. `FelderUebernehmen`:

- prüft die zwei Anteile mit `BhkwWirkungsgrad.Pruefen` — dieselben drei Regeln und
  dieselben drei Meldungen wie im Katalogeditor; geprüft wird der Stand, der **nach** der
  Übernahme dastünde, denn ein leer hereinkommendes Feld lässt den gepflegten Wert stehen;
- setzt sie in den gelesenen Satz;
- zieht die Spalte `Wirkungsgrad` als `GesamtZumSchreiben(el, th, bisher)` nach. Fehlt ein
  Anteil (Altbestand vor Schritt 99), bleibt der Gesamtwert stehen, wie er war, und
  `SimulationBHKW` liest ihn unverändert.

Der Wächter `KatalogAufklapperTests.Der_Datensatz_deckt_genau_die_editierbaren_Felder`
hält die Klammer: 24 editierbare Profilfelder, 24 Parameter des Datensatzes.

### 10.4 Der Wächter gegen die Parameter-Falle

Abschnitt 7 beschreibt den Befund; ab BW-3 hält ihn eine Probe:
`EPOS.Kern.Tests/DbParamNullkonstanteWacheTests` liest jede `.cs` unter `EPOS.Kern`,
`EPOS.UI.Daten`, `WindowsFormsApplication1`, `Werkzeuge`, `EPOS.Referenzlauf` und `KiKern`
(ohne `bin`/`obj`, ohne Kommentarzeilen) und meldet **jeden Aufruf
`new DbParam(<name>, <wert>)` — auch über mehrere Zeilen —, dessen zweites Argument (a) ein
Nullliteral ist (`0`, `0.0`, `0d`, `0f`, `0m`, `0L`, `0x0`, `-0` und ihre Vielfachen an
Nullen) oder (b) der Name einer `const`-Deklaration des Bestands mit dem Wert 0.**

Die Begründung steht im Kopf der Klasse: Roslyn wandelt eine **konstante Null jedes
numerischen Typs** implizit in jeden Aufzählungstyp um — die Sprachnorm nennt dafür nur die
ganzzahligen Typen, Roslyn lässt auch `0.0`, `0f` und `0m` durch. Damit gewinnt
`DbParam(string, DbParamTyp)`, der Parameter bindet `DBNull`, und die SQL-Bedingung ist für
jede Zeile NULL: Sie trifft nichts und meldet nichts. Der XML-Kommentar an `NULLGRENZE`
sagt das jetzt in dieser Schärfe und verweist auf den Wächter.

**Ergebnis auf dem Bestand: 0 Fundstellen** bei 2 491 gelesenen `DbParam`-Aufrufen in
718 Dateien. Drei Gegenproben halten ihn wach: eine synthetische Verletzung in allen zehn
Gestalten (samt mehrzeiligem Aufruf und `const`-Bezeichner) wird erkannt, elf unverfängliche
Schreibweisen werden durchgelassen, die Sammlung der `const`-Nullen findet die bekannten
Namen (`SOC_MIN`, `ALLE`, `ANTEIL_VON`, `SYSTEMVERLUSTE_VORGABE`) und ausdrücklich **nicht**
`NULLGRENZE` — die steht seit dem Befund als `static readonly` da —, und die Zählung der
gelesenen Aufrufe schlägt an, sobald der Leser selbst verunglückt.

### 10.5 Nachweise

**Neue Fälle** `EPOS.Kern.Tests/BhkwWirkungsgradAnteileTests` (+3): der Datensatz nimmt
keinen Gesamtwert mehr entgegen und das Profil führt die zwei Anteile editierbar vor der
Summe; der Speicherweg schreibt die Summe und weist einen Anteil außerhalb des Bandes
benannt ab; ein Altbestandssatz ohne Aufteilung behält seinen Gesamtwirkungsgrad, und die
Anzeige zeigt ihn.
**Neue Fälle** `EPOS.Kern.Tests/DbParamNullkonstanteWacheTests` (4, siehe 10.4).
**Neue Fälle** `EPOS.UI.Tests/Dialoge/KatalogBrowserDialogTests` (+2) — die Maske ist der
Browser, nicht der Katalogeditor: Das Gesamtfeld trägt `readonly`, die zwei Anteile nicht;
0,30 und 0,60 ergeben 0,9, und ein geleerter Anteil lässt die Summe leer.
**Angepasst**, nicht gelöscht: die Feldzahl des BHKW (25 → 27) und die Zahl der
editierbaren Felder (23 → 24), die Spaltenfolge des Profils, der Rundlauf des Aufklappers
(liefert jetzt die zwei Anteile) und die Abweisung des Prozentwerts 29,5, die nun am
elektrischen Anteil hängt.

**Gate beide Kulturen grün** (Stand nach dem Merge von DL-2f und WK-1)**:**
EPOS.Kern 3 921, EPOS.UI 4 855, KiKern 499, SpeicherEngine 378, SpeicherPlanung 27
(1 übersprungen). Windows-Schale mit
`-p:EnableWindowsTargeting=true` **0 Fehler**. SQL-Dialekt-Prüfer **0 Fundstellen**
(1 546 Texte). ResourceDesigner wiederholbar — **kein neuer Ressourcenschlüssel**, die zwei
Beschriftungen und ihre Hinweise stehen seit BW-2 in beiden Sprachen.

**Kein Referenzlauf.** BW-3 fasst weder Rechenweg noch Schema an: Die Spalte `Wirkungsgrad`
steht, wo sie stand, und wird aus denselben zwei Anteilen gebildet wie seit Schritt 99.
Die Basis `2026-09-19_R10_BhkwWirkungsgrad` gilt unverändert.

### 10.6 Offen nach BW-3

- **Windows-Abnahme A-BW3-1:** Administration › Energiesysteme › BHKW › „Alle Daten
  anzeigen" — die zwei Anteile sind Eingaben, „Ges. Wirkungsgrad" ist nicht beschreibbar
  und läuft mit; Speichern schreibt beide Anteile samt Summe in den Katalogsatz.
- **Kein Logbuch-Eintrag:** eine Kleinigkeit im Sinn der Regel (Konzept Hilfesystem 13.4);
  der BW-2-Satz zu Version 1.2.0.3 deckt sie ab.
- **Die Repo-Quelle der Wikiseite „Programm Dokumentation/Gerätekataloge"** ist mit WK-1
  (#393) entstanden. Sie steht zu BW-3 nicht im Widerspruch: Ihr BHKW-Abschnitt nennt den
  Gesamtwirkungsgrad als berechnetes, nicht editierbares Feld, und ihre Aufzählung zum
  Aufklapper führt ihn nicht als Eingabe. Der Upload ins Wiki steht noch aus.
- **Die Feldliste in Abschnitt 6** gilt mit der Änderung aus 10.2.

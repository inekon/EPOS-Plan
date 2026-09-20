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
  einander widersprechen, gibt es damit an keiner Stelle.

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
(CO₂, SO₂, NOx, CO, Staub, je g/MWh — nur Anzeige).

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
- **Der Aufklapper „Alle Daten" zeigt den Gesamtwirkungsgrad weiterhin als Eingabe.** Das
  ist ein Entwurfsentscheid: Er ist der Feldbestand der Tabelle, und ein dort geänderter
  Wert wird sauber auf beide Anteile verteilt. Ob er dort — wie im Katalogeditor — zur
  reinen Anzeige werden soll, ist ein Anwenderentscheid.

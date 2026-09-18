# B-1 — Der Brennstoffverbrauch je Kessel steht in der Modulzeile

**Auftrag #331, 18.09.2026.** Anwenderentscheid zum Befund `B-1` der
Wirtschaftlichkeit: *„Empfehlung — Verbrauch aus dem Lauf nachziehen. Es gibt eine
Größe Verbrauch aus dem Simulationslauf, aus der die Kosten ermittelt werden."*
Damit ist der frühere eigene Entscheid („falsche Projektkonfiguration, Hinweistext
bei 0") ausdrücklich zurückgenommen. Die Messung hatte gezeigt, warum: Die Lücke
hing nicht an der Projektkonfiguration.

---

## 1 Der Befund

**Es gab zwei Größen namens „Verbrauch".**

| Ebene | Tabelle | Zustand |
|---|---|---|
| Anlage, je Brennstoffart | `Tab_ErgebnisHeizkessel.Gasverbrauch` und neun Geschwister | gefüllt, 25 von 27 Zeilen der Testdatenbank ungleich null |
| Modul, je Kessel | `Tab_ErgebnisHeizkesselModul.Verbrauch` | **alle 27 Zeilen 0**, bei durchweg erzeugter Wärme |

Und genau die Modulspalte liest die Kostenkette: `EndenergieAufloeser`,
`KostenEmissionRechner` und `EmissionsBilanzRechner` nehmen `mo.Verbrauch` roh.
Der Kesselbrennstoff fehlte damit in **Energiekosten, CO₂-Bilanz und
BEHG-Abgabe** — und `kostenVollstaendig` blieb dabei `true` (Folgebefund `N1`),
das Ergebnis sah also vollständig aus.

**Ursache.** `SimulationSPK.Kessel_Verbrauch_MWh_Spk` war als einziges seiner vier
Nachbarfelder `private` (`s_waerme_Gas_Spk`, `s_waerme_Oel_Spk`,
`Kessel_Jahresnutzungsgrad_Spk` sind öffentlich). Der `SimulationRunner` konnte den
Wert nicht lesen und setzte in seiner Kessel-Modulschleife weder `Verbrauch` noch
`Waermeproduktion` noch `Brennstoff`.

**Die Lücke hing nicht am Kaskadenplatz.** Die Modulschleife läuft über `spk_list`,
also über die Kessel, die gerechnet haben. Ein voll mitrechnender Kaskadenkessel
bekam dort ebenso wenig einen Verbrauch wie ein stillstehender.

---

## 2 Die Umsetzung

**Das Feld ist öffentlich wie seine Nachbarn**, und die Modulschleife übernimmt alle
drei fehlenden Spalten:

* `Verbrauch` = `Kessel_Verbrauch_MWh_Spk[i]` — Nutzwärme über den Wirkungsgrad plus
  die Bereitschaftsverluste der Stillstandsstunden;
* `Waermeproduktion` = `Waerme_Gas + Waerme_Oel` — dieselbe Summe, auf die sich der
  Jahresnutzungsgrad bezieht;
* `Brennstoff` = das Brennstoffwort aus `SimulationSPK.BrennstoffWort` (Gas, Öl,
  Koks, Kohle, Holz, Strom, Pellets, Rapsöl, Tierische Fette, Sonstige).

Das Wort folgt **Bereich für Bereich derselben Verzweigung**, die den Verbrauch auf
die Anlagenzähler bucht (`Tab_Brennstoff_Stamm.ID_Kategorie`); der Quellkommentar an
beiden Stellen sagt, dass sie zusammen geändert werden. Es ist ein Persistenzwert
und deshalb immer deutsch — wie `mo.Modul`, aus demselben Grund: Die
Referenzlauf-Suite exportiert es als Skalar.

**Modulverbrauch und Anlagensumme sind dieselbe Größe.** An allen dreizehn
Referenzprojekten geprüft: Der Modulwert ist Zahl für Zahl
`Heizkessel.Gasverbrauch` (elf Gaskessel) bzw. 0 bei den beiden Elektrokesseln, die
auf `Heizkessel.Stromverbrauch` buchen.

### 2.1 Der Elektrokessel — die benannte Ausnahme

Ein Kessel mit `Tab_Heizkessel.Brennstoff` = 13 bucht seinen Einsatz in
`Bilanz_und_Nutzungsgrad` auf den **Stromzähler** (`StromverbrauchSpkMwh`) und über
`Stromverbrauch_stuendlich` in die Stundenreihe; von dort steht er im
Reststrombedarf und damit im **Netzbezug**, den die Kostenrechnung eigens bepreist
(mit Grundpreis und Leistungspreis aus der Bezugsspitze).

Ein Verbrauch in seiner Modulzeile stünde derselben Energie ein zweites Mal
gegenüber — als „Brennstoff" seines Trägers. Die Zeile führt deshalb **bewusst 0**,
mit Wärme, Nutzungsgrad, Träger und dem Brennstoffwort „Strom".

Drei Wege standen zur Wahl, zwei fallen aus:

* **Die Strommenge eintragen** — Doppelzählung in Energiekosten, CO₂-Bilanz und
  BEHG. Sie bräuchte an drei Stellen eine Gegenausnahme (`KostenEmissionRechner`,
  `EmissionsBilanzRechner`, `EndenergieAufloeser`) statt an einer.
* **Gar keine Zeile** — der Ergebnisexport führt die Module indexgleich zu
  `spk_list`; eine fehlende Zeile verschöbe jede folgende.
* **0 mit gesetztem Träger und Brennstoffwort** — gewählt. Eine Ausnahme, an einer
  Stelle, im Quellkommentar begründet.

### 2.2 Das BHKW — gemessen und NICHT mitgezogen

Der Auftrag nahm an, die BHKW-Modulspalte sei ebenso leer und die Lücke dort sogar
größer, weil die BHKW-Steuerzeile `modul.Verbrauch` **ohne Rückfall** übernimmt.
**Die Annahme ist falsch, und das ist gemessen:**

`ErgebnisCtrl.Save` füllt `Verbrauch` und `Brennstoff` der BHKW-Modulzeile seit
jeher — aus dem dominanten Brennstoff der Anlagenzeile (`BHKWBrennstoff`), anteilig
nach Wärmeproduktion verteilt. In der eingefrorenen Basis steht das schwarz auf
weiß: Projekt 1030 führt 862,20 + 186,09 = 1.048,29 MWh, und das ist genau
`BHKW.Gasverbrauch`. Die Steuergrößen rechnen dort also schon heute mit einer Menge,
nicht mit 0; **es ändert sich durch B-1 an ihnen nichts.**

Ein zweiter Schreiber im `SimulationRunner` wäre eine zweite Wahrheit über dieselbe
Spalte gewesen — und er hätte andere Zahlen geliefert: `SimulationBHKW` rechnet je
Modul `(Wärme + Strom) / Wirkungsgrad`, der Speicherweg verteilt nur nach Wärme.
Deshalb bleibt der BHKW-Weg unangetastet.

**Als Wissen festgehalten** (kein Auftrag, nicht geändert): Führt eine BHKW-Anlage
Module mit **verschiedenen** Brennstoffen, schreibt der Speicherweg allen Modulen
den dominanten Brennstoff und verteilt dessen Menge über alle — ein Ölmodul bekäme
dann eine Gasmenge. Das ist eine eigene Messung und ein eigener Entscheid wert.

---

## 3 Die Warnung `WIRT_KESSELBRENNSTOFF_FEHLT`

**Sie bleibt stehen** — als Wächter für den Fall, dass die Kette doch einmal reißt,
und für gespeicherte Läufe von vor B-1, die ihre 0 behalten. Sie verstummt von
selbst: gemessen an denselben dreizehn Projekten mit demselben Lauf, einmal mit
geleerter und einmal mit gefüllter Modulspalte —

| | Warnung an |
|---|---|
| Modulspalte leer (Zustand vor B-1) | **elf** von dreizehn Projekten |
| Modulspalte gefüllt | **keinem** |

**Beim Elektrokessel schweigt sie ausdrücklich.** Dort ist die 0 die richtige Zahl
und kein Loch; ohne diese Ausnahme stünde bei jedem solchen Projekt dauerhaft eine
Warnung über einen Brennstoff, den es nicht gibt. Erkannt wird er an
`Tab_Heizkessel.Brennstoff` = 13 — der **Anlagen**angabe, nicht am `carrier_id` der
Ergebniszeile: Der trägt die Auskunft nur, wenn dem Kessel überhaupt ein Träger
zugeordnet ist, und das ist im Bestand oft nicht der Fall.

**Bis zu diesem Auftrag deckte kein Test die Fahne ab.** Jetzt decken sie zwei: einer
löst sie aus (Modulzeile mit Wärme und ohne Verbrauch), einer zeigt ihr Schweigen bei
gefüllter Spalte; ein dritter pinnt die Elektrokessel-Ausnahme, ein vierter, dass sie
der **Anlage** gilt und nicht dem Projekt.

---

## 4 Die Rückfälle — alle vier bleiben, keiner ist tot

| Stelle | Entscheid | Grund |
|---|---|---|
| `HilfsstromRechner.KesselBrennstoffMWh` (Verbrauch, sonst Wärme / Nutzungsgrad) | **bleibt** | Trägt zwei Fälle: gespeicherte Läufe von vor B-1 (sie behalten ihre 0) und den Elektrokessel. Ohne ihn stünde dort eine 0 in der Steuerbemessung |
| `ErgebnisCtrl` — Ableitung der Hilfsenergie | **bleibt** | Dieselbe Methode, ihr erster Zweig nimmt jetzt den gelesenen Wert. Ein zweiter Rechenweg entsteht nicht |
| `ExcelBerichtGenerator` — `Verbrauch > 0 ? … : null` | **bleibt** | Beim Elektrokessel ist die leere Zelle richtig; für Altzeilen ebenso |
| Bericht — Wärmeproduktion aus `Waerme_Gas + Waerme_Oel` | **bleibt** | Gespeicherte Zeilen von vor B-1 führen `Waermeproduktion` = 0 |

Die Kommentare an allen vier Stellen nennen jetzt diesen Zweck, statt zu behaupten,
der Rechenkern setze `Verbrauch` nie.

---

## 5 Der Referenzlauf — nichts eingefroren

Gefahren über **alle dreizehn** Projekte gegen `2026-09-16_R8_Heizkessel_Kaskade`.

**37 Abweichungen von 3.882.737 Werten, alle erklärbar.** Je Projekt genau
`HeizkesselModul[0].Verbrauch`, `.Waermeproduktion` und `.Brennstoff`; bei 1017 und
1024 (Elektrokessel) nur die letzten beiden. **Keine einzige andere Größe, keine
Zeitreihe, keine Anlagensumme.** Es gibt keine unerklärte Abweichung.

Die Gegenprobe des Rechenwegs hält in jedem Projekt:
`Verbrauch = Waermeproduktion / (Jahresnutzungsgrad / 100)` — und der Modulwert ist
gleich `Heizkessel.Gasverbrauch`.

| Projekt | Verbrauch [MWh/a] | Brennstoff |
|---|---|---|
| 1007, 1046 | 15,47 | Gas |
| 1008 | 12,02 | Gas |
| 1017 | 0 | Strom |
| 1018 | 16,76 | Gas |
| 1023 | 78,64 | Gas |
| 1024 | 0 | Strom |
| 1030 | 5.403,10 | Gas |
| 1039 | 225,04 | Gas |
| 1040, 1045 | 16,19 | Gas |
| 1041 | 133,33 | Gas |
| 1042 | 13,81 | Gas |

**Eingefroren wurde nichts, Basispfade sind unverändert.** Der Entscheid über eine
neue Basis liegt beim Anwender.

---

## 6 Die zweite Wirkung — Folgebefund `N1`, entscheidungsbedürftig

Mit dem Brennstoff erreicht der Kessel die Kostenkette. Das hat zwei Gesichter,
beide am selben Lauf gemessen (einmal mit geleerter, einmal mit gefüllter
Modulspalte):

**Wo der Kessel einen Energieträger trägt, wächst die Rechnung um seinen
Brennstoff.** Projekt 1030: Energiekosten 1.176.908 → 1.609.156 €/a, CO₂ 2.691,95 →
3.988,69 t/a (Brennstoffanteil 251,59 → 1.548,33 t). Ebenso 1039, 1040, 1041, 1042,
1045.

**Wo er keinen trägt, greift die Hausregel „keine stillen Teilsummen".** Die Menge
landet in `verbrauchOhneTraeger`, `kostenVollstaendig` kippt, und Energiekosten und
CO₂ bleiben `null` — **mit dem benannten Grund und dem Ausweg**: „Ein Teil des
Brennstoffverbrauchs (X MWh/a) gehört zu keinem Energieträger. Ausweg: den
betroffenen Erzeugern unter ‚Anlagen' einen Energieträger zuordnen." Genau das war
`N1`: Bisher sah das Ergebnis vollständig aus und überging den Kessel.

In der Testdatenbank betrifft das **1007, 1008, 1018, 1023 und 1046**; Projekt 1023
verliert dadurch eine Zahl, die es hatte (62.540,05 € → „—"). Der Lauf meldet die
fehlende Zuordnung derselben Anlagen schon heute als Simulationswarnung — die beiden
Meldungen sagen jetzt dasselbe.

---

## 7 Abnahme

* Kern-Filter `WP-Plan.Kern.slnf`: **0 Fehler, 5 Warnungen** (Bestand).
* Windows-Schale (`EnableWindowsTargeting=true`): **0 Fehler, 5 Warnungen** (Bestand).
* `SqlDialektPruefer`: 1.491 SQL-Texte, **0 Fundstellen**.
* Voller Testlauf: **8.815 grün**, 1 übersprungen, 0 rot — darunter die sieben neuen
  Fälle in `EPOS.Kern.Tests/KesselBrennstoffModulTests`.
* Referenzlauf über dreizehn Projekte gefahren, Vergleich vorgelegt, **nichts
  eingefroren**.

**Nicht getan, bewusst:** kein Neueinfrieren der Basis (Anwenderentscheid steht
aus), keine Änderung am BHKW-Schreibweg, keine Änderung an den Mengen des
`HilfsstromRechner` (die Rückrechnung liefert für den Elektrokessel weiterhin eine
Strommenge als § 54-Bemessung — eine eigene Messung wert), kein Wiki-Eingriff (keine
Fachseite wird durch die Änderung unrichtig), keine Windows-Abnahme.

# LS-1 — Die Lastspitzenkappung als Berechnungsart des Einzelspeichers

> Umsetzung 17.09.2026 auf `w-ls1` (Zweig aus `ios_migration_september`).
> Anlass: Anwenderbefund vom 17.09.2026, Anwenderentscheide `LS-E-1` = (a) und
> `LS-E-3` (Empfehlung). Gate: Kern-Filter, beide Kulturen, SQL-Dialekt-Prüfer und
> der **byte-gleiche** Referenzlauf gegen `2026-09-16_R8_Heizkessel_Kaskade`.

---

## 1 Der Befund

**Der Anwender:** „Leistungspreis bei Stamm und Variante gleich, obwohl der Speicher die
Lastspitze senken müsste."

Seit Auftrag #312 bepreist der `KostenEmissionRechner` die Netzbezugsspitze
(`Arbeit × Arbeitspreis + Grundpreis + Leistungspreis × Bezugsspitze`). Die Spitze entsteht
im Lauf aus `Rest_Strombedarf_viertelstuendlich`. Damit Stamm und Variante sich
unterscheiden, muss der Speicher diese Reihe an der Spitze senken — und genau das konnte er
nicht.

**Der gemessene Zustand (fünf Leser, fünfzehn Gegenprüfer):**

1. `StromspeicherSimCtrl.BaueStrategie` kannte für
   `Tab_StromspeicherVariante.Berechnungsart` **zwei** Werte: `Arbitrage` (nur mit
   Preisreihen und Netzentladung) und sonst `Dauernutzung`. Jeder andere Wert fiel mit dem
   Hinweis `SIMENG_SPEICHER_BERECHNUNGSART` zurück.
2. `DbWerte.SP_BERECHNUNG_PEAKSHAVING` **existierte**, wurde aber nirgends gesetzt und
   nirgends ausgewertet. Die Klapplisten der Oberfläche führten zwei Einträge.
3. Die Lastspitzenkappung gab es **zweimal** — als eigene Maske (`PeakShavingCtrl`,
   `PeakShavingDialog`, nur lesend, CSV) und als Betriebsziel der Flotte
   (`FlottenSimulator`) — und **keine von beiden hatte einen Weg in den Projektlauf**. Die
   Maske rechnete und vergaß ihr Ergebnis.
4. Der Projektlauf zog von `Rest_Strombedarf_viertelstuendlich` nur die **Entladung** ab
   (`SimulationControl:588-598`). Für die Dauernutzung stimmt das — sie lädt ausschließlich
   aus Überschuss, und der steht dort ohnehin nicht mehr. Für eine Kappung mit Netzladung
   stimmt es nicht.

**Kurz:** Ein Einzelspeicher konnte die Spitze nicht kappen, und zwei vorhandene
Kappungen konnten es nicht für ihn tun.

---

## 2 Was gebaut wurde

### 2.1 Schemaschritt 86

`Tab_StromspeicherVariante` bekommt zwei Spalten:

| Spalte | Typ | Bedeutung |
|---|---|---|
| `PeakZiel_kW` | `REAL`, **nullbar** | Zielschwelle P_ziel [kW]. NULL heißt „nicht gepflegt" — und das ist etwas anderes als 0 (die Kappung auf 0 kW). |
| `PeakZiel_Adaptiv` | `INTEGER NOT NULL DEFAULT 0 CHECK (… IN (0,1))` | Die Schwelle zieht sich selbst nach. |

**Eine Quelle, drei Leser** nach dem Muster der Schritte 82/83/85:
`SchemaKatalog.Schritt86_Lastspitzenkappung` → `SchemaMigration.Schritt_86_…` (Schale),
`Werkzeuge/Testdatenbankschema` und `EPOS.Kern.Tests/TestDatenbank`.
`SchemaStand.Zielversion` 85 → 86. Die Testdatenbank ist mit dem Werkzeug gewandelt
(2 Spalten angelegt, VACUUM, Größe unverändert 70 770 688 Byte) und mit aktivem
LFS-Filter committet.

**Ergebnisneutral, belegt:** Im ganzen Bestand führt **keine** Variante die neue
Berechnungsart, keine trägt ein Peak-Ziel und keine das Adaptiv-Flag — drei Zählungen
stehen als Prüffall in `LastspitzenkappungTests`. Der Schritt schreibt kein DML.

### 2.2 Der Ladedeckel — ein Feld für zwei Regeln

`PeakShavingParameter` bekommt `LadedeckelKw` (`double?`, Vorgabe `null`): die **höchste
Netzlast, die das LADEN erzeugen darf**. Bis dahin war die Zielschwelle beides in einem —
oberhalb entladen, unterhalb bis zu ihr laden. Der Einzelspeicher braucht die Trennung
zweimal:

* **Grünstrom → Deckel 0.** Geladen wird nur, solange die Netzlast negativ ist, also allein
  aus Erzeugungsüberschuss (Quellen-Matrix 2.1). Ohne Deckel lüde er aus dem Netz, weil
  jede Last unterhalb der Schwelle Luft zum Laden lässt.
* **Ziel ≥ Referenzspitze → Deckel bei der Referenzspitze** (LS-E-3). Ein zu hoch gewähltes
  Ziel darf die Spitze nicht **anheben**; ohne Deckel erzeugte die Ladung eine neue, höhere
  Spitze.

`null` heißt „kein eigener Deckel" — dann gilt wie bisher die Schwelle selbst. **Maske,
Flotte, Rastersuche und der Excel-Regressionstest setzen das Feld nicht und rechnen Wert
für Wert unverändert**; ein eigener Prüffall hält das fest.

### 2.3 Die Strategie im Projektlauf

`BaueStrategie` bekommt den `SpeicherEingang` mit (die Messlatte steht nur in den Reihen)
und baut für `SP_BERECHNUNG_PEAKSHAVING` eine `PeakShaving` **am Netzanschluss**
(`netzanschlussAuslegung: true`): Steuergröße ist die Netzlast `Last − PV − BHKW`, nicht
der blanke Lastgang. Eine Kappung, die die Erzeugung nicht sieht, entlüde gegen eine
Spitze, die es am Zähler gar nicht gibt.

* **Referenzspitze** = `max(0, Last − PV − BHKW)` über das Jahr
  (`StromspeicherSimCtrl.NetzbezugsspitzeKw`).
* **Ladedeckel** = Graustrom ? `min(Ziel, Referenzspitze)` : 0.
* **LS-E-3:** Liegt ein festes Ziel nicht unter der Referenzspitze, steht das im Protokoll.
  Der Lauf wird deswegen nicht verweigert — er sagt es, und der Deckel sorgt dafür, dass
  die Spitze wenigstens nicht steigt.
* **Ohne Ziel** (NULL oder 0, nicht adaptiv) fällt der Lauf **benannt** auf die
  Dauernutzung zurück (`SIMENG_SPEICHER_PEAK_OHNE_ZIEL`).
* **Immer energetisch:** Der Excel-Kompatibilitätsmodus gehört der Dauernutzung
  (Fachkonzept 5.2); die Parameterseite bietet ihn hier gar nicht erst an.

Eine Protokollzeile nennt danach Ziel, Deckel und beide Spitzen
(`SIMENG_SPEICHER_PEAK_LAUF`).

### 2.4 Die Netzwirkung in `Rest_Strombedarf`

Für diese Berechnungsart liefert der Speicherlauf nicht die Entladung, sondern die
**Netzwirkung**:

```
Netzwirkung[i] = max(0, Netzlast ohne Speicher[i]) − max(0, Netzlast mit Speicher[i])
```

Sie wird **negativ**, wo der Speicher aus dem Netz lädt — und genau das fehlte. `SubVectors`
klemmt das Ergebnis bei 0, ein negativer Summand erhöht den Reststrombedarf also
sachgerecht. Die Reihe steht im `StromspeicherLaufKontext`; ist sie `null`, bleibt alles
wie bisher.

**Die Arbitrage bleibt unverändert — eigener Entscheid.** Ihr Netzladepfad liegt in einer
getrennten Reihe und wird weiterhin **nicht** aufgeschlagen. Die Untererfassung ist damit
gemessen und benannt, aber nicht behoben: Sie zu beheben wäre eine Änderung des Rechenwegs
außerhalb dieses Auftrags, und der Referenzlauf bliebe nicht byte-gleich. Wer sie angeht,
braucht dafür einen eigenen Auftrag und eine neue Referenzbasis.

### 2.5 LS-E-3 auch für die Flotte

* `FlottenPlausibilitaet` beanstandet jetzt **`Ziel ≥ Referenzspitze`** (vorher nur `>`) und
  als **Warnung** statt als Hinweis. Ein Ziel genau auf der Spitze kappt ebensowenig wie
  eines darüber — dieser Fall blieb bis dahin stumm.
* `FlottenSimulator` deckelt das Laden auf `min(Peak-Ziel, Referenzspitze)`; die
  Referenzspitze ist die höchste Netzlast ohne Flotte, Hilfsverbrauch eingerechnet, einmal
  vor der Schleife gebildet. **Liegt das Ziel wie im Regelfall darunter, ist der Deckel
  genau das Ziel** — Projekt 1046 (Peak-Ziel 16 kW, Spitze 19,78 kW) rechnet Bit für Bit
  wie bisher, belegt durch den byte-gleichen Referenzlauf.

### 2.6 Oberfläche

* Die Klappliste „Berechnungsart" des Speicherparameterblocks führt einen **dritten**
  Eintrag „Lastspitzenkappung".
* Darunter erscheinen **nur bei dieser Art** Peak-Ziel [kW] und „Peak-Ziel adaptiv
  nachziehen"; beide schreiben sofort, wie jedes andere Feld des Blocks. Bei „adaptiv" ist
  das Zielfeld gesperrt — die Schwelle zieht sich dann selbst nach.
* Eine **Herleitungszeile** nennt die Netzbezugsspitze ohne Speicher des letzten Laufs
  dieser Sitzung; ohne Lauf steht dort nichts.
* Die Maske „Lastspitzenkappung" bekommt **„In Variante übernehmen"**: Berechnungsart,
  **erreichte** Schwelle (nicht die Eingabe) und Adaptiv-Flag in einem Zug. Führt die
  Variante eine andere Art, wird vorher gefragt. Ohne Projekt gibt es den Knopf nicht.
  Die Maske selbst bleibt, wie sie war.
* Für die Kopfzeile des Simulationsreiters liegt der Textbaustein
  `SpeicherAnzeigeCtrl.EinzelspeicherKontextText` bereit („Einzelspeicher:
  Lastspitzenkappung · Peak-Ziel 80 kW (adaptiv)"); die Zeile selbst setzt der
  Speicherkontext zusammen, der aus der Nachbarwelle kommt.
* 14 neue Ressourcenschlüssel in **beiden** Sprachen; `Resource.Designer.cs` neu erzeugt
  (6 287 → 6 301 Einträge, +5 276 Zeichen, zweiter Lauf +0).

---

## 3 Nachweis

### 3.1 Die Engine (`PeakShavingLadedeckelTests`, 10 Fälle)

Kleine Reihen, dt = 1 h, η = 1, Band 0…100 kWh, P = 50 kW — jeder Schritt von Hand
nachrechenbar.

| Fall | Herleitung | Ergebnis |
|---|---|---|
| Ohne Deckel wie bisher | Last 80/20/80/20, Ziel 50 | Reihe Wert für Wert gleich der Fassung ohne das Feld |
| Kappung auf das Ziel | Last 20/20/20/90, Ziel 60 | 60/60/40/60, neue Spitze 60,00 kW |
| Grünstrom (Deckel 0) | Netzlast −30/10/10/70, Ziel 40 | 0/10/10/40; Ladung 30 kWh = Überschuss, **keine** kWh aus dem Netz |
| Graustrom (Deckel 50) | Netzlast 10/10/10/100, Ziel 50 | 50/50/30/50; kein Intervall über dem Ziel |
| Ziel über der Referenz | Netzlast 10/10/10/55, Ziel 200, Deckel 55 | Spitze bleibt 55,00 kW |
| Adaptiv mit Deckel 0 | Netzlast −30/200 | 0/170; erreichte Schwelle 170,00 kW |

**Gegenproben — sie bleiben als Prüffälle stehen**, statt einmal gefahren und
zurückgebaut zu werden; so misst die Reihe den Unterschied, den der Deckel macht, bei jedem
Lauf mit:

* Deckel ausgehängt (`null`) im **Grünstromfall** → Intervall 2 lädt aus dem Netz auf
  40 kW statt bei 10 kW zu bleiben.
* Deckel ausgehängt bei **Ziel 200 über der Referenzspitze 55** → die Ladung erzeugt
  60,00 kW, eine neue Spitze über der Referenz.

### 3.2 Der Projektlauf (`LastspitzenkappungTests`, 24 Fälle)

Projekt 1026 („Beispiel WP WG 1", eine Speicheranlage 11,04 kW / 10,2 kWh, SoC-Band
10…90 %, Start 100 %), synthetische Stromreihe: 5 kW Grundlast, **eine** Viertelstunde mit
30 kW, keine PV — die Netzlast ist damit die Reihe selbst, die Referenzspitze 30,00 kW.

| Fall | Herleitung | Ergebnis |
|---|---|---|
| Graustrom, Ziel 20 kW | 30 − 20 = 10 kW für eine Viertelstunde = 2,5 kWh; Gerät trägt 11,04 kW und 7,74 kWh | Referenzspitze 30,00 → neue Spitze **20,00 kW**, Deckel 20,00 kW |
| dieselbe Reihe, Netzwirkung | an der Spitze `30 − 20 = +10 kW`; davor voller Speicher, also 0; danach Nachladen | Wirkung negativ nach der Spitze, betraglich ≤ 15 kW (5 kW Grundlast bis 20 kW Deckel), **kein** Intervall über 20 kW |
| Grünstrom, Ziel 20 kW | Deckel 0, kein Überschuss in der Reihe | Ladeenergie **0**, keine negative Netzwirkung, Spitze trotzdem 20,00 kW (Startfüllstand) |
| Ohne Ziel (NULL) | nicht adaptiv | Rückfall auf die Dauernutzung, Hinweis im Protokoll, kein Peak-Ergebnis |
| Ziel 30 = Referenzspitze | LS-E-3 | Warnung im Protokoll, Deckel 30,00 kW, `PNeuMax ≤ PAltMax` |

**Gegenprobe (bleibt als Prüffall stehen):** dieselbe Variante auf `Dauernutzung`
festgenagelt → kein Peak-Ergebnis, keine eigene Netzwirkung, Referenzspitze 0 — genau der
Zustand des Anwenderbefunds.

### 3.3 Ende zu Ende: die Spitze schlägt auf die Energiekosten durch

Projekt 1026, Netzbezug 19,08 MWh/a, Arbeitspreis 0,35 €/kWh → **6 678,00 €** Arbeit in
beiden Läufen. Leistungspreis 60 €/(kW·a), Modus JAHR.

| | Arbeit | Leistungsanteil | Energiekosten |
|---|---|---|---|
| Stamm (Spitze 30 kW) | 6 678,00 € | 30 × 60 = 1 800,00 € | **8 478,00 €** |
| Variante (Spitze 20 kW) | 6 678,00 € | 20 × 60 = 1 200,00 € | **7 878,00 €** |
| **Δ** | 0,00 € | **600,00 €** | **600,00 €** = (30 − 20) × 60 |

`Ertrag_Leistungspreis` bleibt **0** (Entscheid E5-1): Der Ertrag der Kappung steckt im
Reststrombetrag und nirgends sonst.

### 3.4 Oberfläche (`SpeicherParameterPeakZielTests` 8, `PeakShavingVarianteTests` 7)

Dritter Eintrag in der Klappliste; die zwei Felder stehen **nur** bei dieser Art und
erscheinen beim Wechsel sofort; beide schreiben sofort und invariant („62,5" → `62.5`);
bei „adaptiv" ist das Zielfeld gesperrt. Der Knopf „In Variante übernehmen" steht nur mit
Delegat da, schreibt ohne Ergebnis nichts, fragt bei abweichender Berechnungsart und
schreibt erst auf „Ja"; ein „Nein" schreibt nichts; ohne aktive Variante nennt er den
Grund.

### 3.5 Gate

| Prüfung | Ergebnis |
|---|---|
| `WP-Plan.Kern.slnf` Release | 0 Fehler, **0 Warnungen** (Schranke 7) |
| Windows-Schale (`EnableWindowsTargeting=true`) | 0 Fehler, 5 Warnungen (Bestand) |
| `EPOS.Kern.Tests` | 3 207 / 3 207 |
| `EPOS.UI.Tests` | 4 572 / 4 572 |
| `SpeicherEngine.Tests` | 378 / 378 |
| `KiKern.Tests` / `SpeicherPlanung.Tests` | 499 / 499 · 27 / 28 (1 übersprungen) |
| beide Kulturen | Standard **und** `LC_ALL=en_US.UTF-8` grün |
| `SqlDialektPruefer` | 1 474 Texte, **0 Fundstellen** |
| Referenzlauf 1030/1007/1017/1045/1046 | 5 / 5 PASS, **byte-gleich** gegen R8 (`diff -rq` je Projekt ohne Ausgabe) |

---

## 4 Wiki und Logbuch

Fortgeschrieben sind die Repo-Quellen
`Projekte/Wiki/Programm Dokumentation - Stromspeicher.wiki` (neuer Abschnitt
„Berechnungsart der Speichervariante" mit den drei Arten, dem Peak-Ziel, der adaptiven
Schwelle und der Regel „das Ziel muss unter der Bezugsspitze liegen"; neuer Abschnitt
„Maske Lastspitzenkappung" mit „In Variante übernehmen") und
`Programm Dokumentation - Wirtschaftlichkeit.wiki` (ein Satz zur Wirkung auf die
Energiekosten). Beide gegengelesen — kein „seit …", kein Entscheid-, Wellen- oder
Befundkürzel, keine Produktdaten. **Der Upload ist gebündelt und steht aus.**

**Logbuch-Entwurf** (bestehende Version, vom Anwender zu bestätigen), ein Satz je
sichtbarer Änderung:

* Der Stromspeicher kann als Berechnungsart „Lastspitzenkappung" auf ein Peak-Ziel
  gefahren werden, wahlweise mit fester oder mit nachgezogener Schwelle.
* Die Maske „Lastspitzenkappung" übernimmt ihr Ergebnis auf Knopfdruck in die aktive
  Speichervariante des Projekts.

Der Hinweis „Peak-Ziel liegt nicht unter der Bezugsspitze" ist eine Meldung im
Laufprotokoll und bekommt nach Regel 13.4 keinen eigenen Eintrag.

---

## 5 Abnahme auf Windows

| Punkt | Handgriff | Erwartung |
|---|---|---|
| `A-LS1-1` | Anwenderprojekt „Stromspeicher Optimierung", Variante „ein Speicher": Reiter ''Stromspeicher'' → Berechnungsart „Lastspitzenkappung" | Die zwei Felder Peak-Ziel und „adaptiv" erscheinen; die Wahl steht nach einem Reiterwechsel noch da |
| `A-LS1-2` | Peak-Ziel 80 kW eintragen, Simulation rechnen | Das Laufprotokoll nennt Ziel, Ladedeckel und beide Spitzen; die Bezugsspitze der Variante liegt bei 80 kW |
| `A-LS1-3` | Betriebsart auf Grünstrom stellen, erneut rechnen | Das Protokoll nennt Ladedeckel 0; der Speicher lädt nicht aus dem Netz |
| `A-LS1-4` | Peak-Ziel über die Bezugsspitze setzen, rechnen | Die Warnung steht im Protokoll, die Spitze steigt nicht |
| `A-LS1-5` | Peak-Ziel leeren, rechnen | Das Protokoll sagt „kein Peak-Ziel — gerechnet wird die Dauernutzung" |
| `A-LS1-6` | Maske „Lastspitzenkappung" öffnen, rechnen, „In Variante übernehmen" | Rückfrage bei abweichender Art; danach trägt die Variante Art, Ziel und Flag |

Alle sechs in **beiden** Sprachen.

---

## 6 Was offen bleibt

1. **Die Untererfassung der Arbitrage.** Ihre Netzladung wird weiterhin nicht auf
   `Rest_Strombedarf` aufgeschlagen (§ 2.4). Gemessen und benannt, bewusst nicht behoben —
   das wäre eine Änderung des Rechenwegs mit neuer Referenzbasis.
2. **Die Kopfzeile des Simulationsreiters** nimmt den Textbaustein aus § 2.6 erst auf,
   wenn der Speicherkontext der Nachbarwelle zusammengeführt ist.
3. **Der adaptive Modus bleibt ein Greedy.** Er startet bei 0 und kann sich in der
   Anlaufphase nicht aus dem Netz vorladen (die Ladeschranke ist dort 0); das ist die
   dokumentierte Eigenschaft der verifizierten Vorlage. Wer die tatsächlich minimale Spitze
   braucht, nimmt `PeakShaving.MinimaleSchwelleKw` und rechnet mit festem Ziel — genau das
   bietet die eigene Maske an.

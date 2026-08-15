# Paket 5 — Solarthermie + Heizkessel (Umsetzungsprotokoll)

Stand: 14.08.2026 · Grundlage: [`Konzept_Simulation_QuellenSenken.md`](Konzept_Simulation_QuellenSenken.md),
Kapitel 3.4 (Ladepriorität und Ladeobergrenzen), 3.5 (PV-Sonderpriorität), 3.6
(Entladereihenfolge), 6.1 (Transportstruktur), 6.3 (Reihenfolge-Invariante samt Nachtrag
zum Bilanzraum), **6.4 (Solarthermie)**, **6.5, erster Punkt (Heizkessel)**, 6.7
(Kompatibilität der Anzeigen) und Kapitel 9 (Paket-Tabelle, Zeile 5) · Vorarbeit:
[`Paket4_EngineKern_Protokoll.md`](Paket4_EngineKern_Protokoll.md) — Speicher-Registry,
Feature-Flag `Kaskade_Zweikanalig`, Phasen A–G, Bilanzraum aus der Nutzerentscheidung
zu Befund 4b-1.

**Nicht committet.** Keine Designer- oder `.resx`-Datei angefasst; die gesperrten Dateien
(`WizardCtrl`, `WErzeugerModel`, `Form_BHKWEing`, `Form_Heizkessel`, `WizardParent`) sind
unberührt, ebenso `Referenzlaeufe/2026-08-14_B0/lauf_protokoll.md`.

> ## ⚠ Stand nach der Review-Nacharbeit (15.08.2026)
>
> Zwei adversariale Reviews haben diese Umsetzung geprüft. Die elf Befunde **N1–N11**
> sind behoben; **Kapitel 13 ist das führende Kapitel für den heutigen Stand.** Die
> Kapitel 1–12 beschreiben die Umsetzung vor der Nacharbeit; wo sie überholt sind, steht
> ein Verweis auf 13. Drei Befunde waren kritisch (falsches Kessel-Ergebnis, Doppelzählung
> der Deckungsgrade, genullter Kessel-Strombedarf), zwei ernst (stiller Positionswechsel,
> stiller Totalausfall bei defekter Puffer-Senke).
>
> **Nutzerentscheidung 5-1 ist am 15.08.2026 BESTÄTIGT.** Die Zurechnungsregel
> „Vermischung im Speicher" (Momentanmischung, proportionale Verlusttragung, Zurechnung je
> Erzeuger*art*) ist damit **keine Interimsregel mehr**, sondern die gültige Regel — ohne
> Codeänderung, der Stand der Nacharbeit bleibt unverändert. Einzelheiten in Kapitel 10.
> Offen bleibt in diesem Protokoll nur noch **5-2**.

**Das Feature-Flag ist der einzige Schalter.** Mit `Kaskade_Zweikanalig = aus` rechnet der
Altpfad — und zwar nachweislich **byte-identisch**: Alle CSV-Dateien der neun
Referenzprojekte sind Zeichen für Zeichen gleich mit der Basis
`Referenzlaeufe/2026-08-14_B1-Fixes` (Teil F.2). Ein neues Basis-Einfrieren ist deshalb
**nicht** nötig.

---

## 1. Umfang

### Neue Dateien

| Datei | Inhalt |
|---|---|
| `Allgemein/Simulation/Kaskadenschleife.cs` | Die Stundenschleife A–G für ALLE speicherfähigen Erzeuger (Wärmepumpe, Solarthermie, Heizkessel): Phasen A/E (Entladung), C/D (Ladephase über die kaskadenübergreifende Ordnung), G (StundeAbschliessen), dazu `SenkeAbziehen` als eine gemeinsame Implementierung |
| `Allgemein/Simulation/Paket5_SolarKessel_Protokoll.md` | dieses Protokoll |

### Geänderte Dateien

| Datei | Änderung |
|---|---|
| `Allgemein/Simulation/SimulationSolarthermie.cs` | Zweikanaliger Weg: `Vorbereiten_Zweikanalig` (stündliches Potenzial je Kollektorfeld vorab), `Stunde_Start`/`Stunde_Bedarf`/`Zweikanalig_Laden`/`Stunde_Ende`, `Abschluss_Zweikanalig`, `Berechnung_Zweikanalig` (Vektorstufe ohne Speicher); neue Größen `solar_anlagen_ids`, `Speicherladung_stuendlich`, `Speicherladung_gesamt`, `Direktdeckung_gesamt`. **`Berechnung()` (Altpfad) unverändert** |
| `Allgemein/Simulation/SimulationSPK.cs` | Schritte 4/5 in `Bilanz_und_Nutzungsgrad` herausgelöst (beide Wege benutzen sie); zweikanaliger Weg mit `Vorbereiten_Zweikanalig`, `Stunde_Start`, `Stunde_Bedarf`, `Zweikanalig_Laden`, **`Stunde_Abschluss`** (Brennstoffbilanz genau 1× je Stunde und Kessel), `Abschluss_Zweikanalig`, `Berechnung_Zweikanalig`. **`Berechnung()` und `Heizkessel_Simulation()` (Altpfad) unverändert** |
| `Allgemein/Simulation/SimulationWaermepumpe.cs` | `Berechnung_Zweikanalig` in Stundenschritte zerlegt: `Zweikanalig_Start`, `Zweikanalig_StundeStart`, `Zweikanalig_Bedarfsphase` (Phase B), `Zweikanalig_Laden` (ein Ladeauftrag), `Heizstabphase` jetzt `public`, `Zweikanalig_StundeEnde`, `Zweikanalig_Ende`. `Entladephase`/`DurchsatzEntladen`/`EntladeKanal` und der Rumpf von `SenkeAbziehen` sind in die `Kaskadenschleife` gewandert. **Der einkanalige Altpfad ist unberührt** |
| `Allgemein/Simulation/SimulationControl.cs` | `Kaskade_Zweikanalig` entscheidet über die Mitglieder der Speicherstufe und ruft sie an der Kaskadenposition des ersten Mitglieds; neu `Speicherstufe_Rechnen`, `BedarfsreihenfolgeAufbauen`, `ErzeugerMitPufferSenke`, `KaskadeEnthaelt`, `SchleifenstufeNach`, `ModulindexDerAnlage`, `WP_Liste_Laden`, `SPK_Liste_Laden`, `Solar_Liste_Laden`, `Simulation_SPK_Ctrl_Zweikanalig`, `Simulation_Solarthermie_Ctrl_Zweikanalig`; `LadeordnungAufbauen` nimmt Solarthermie- und Kessel-Anlagen auf |
| `Allgemein/Simulation/SimulationKanaele.cs` | `Ladeauftrag.Erzeugerart` (welches Modul den `Modulindex` auflöst); Kommentar an `Modulindex` |
| `Allgemein/Simulation/SimulationRunner.cs` | **Pflicht-Mitkorrektur zu Konzept 6.4**: `Restwaermebedarf` und `Waermebedarfsdeckung` der Solarthermie folgen der DIREKTDECKUNG statt der Gesamtproduktion (Teil 4) |

`SimulationBHKW.cs` ist **nicht** angefasst — das BHKW bleibt am `Uebernehmen`-Anker
(Paket 6).

---

## 2. Die Entwurfsentscheidung: eine gemeinsame Stundenschleife

### Warum die Schleife aus dem Wärmepumpen-Modul heraus musste

Etappe 4b hatte die Stundenschleife A–G in `SimulationWaermepumpe.Berechnung_Zweikanalig`
untergebracht — damals folgerichtig, weil die Wärmepumpe der einzige Erzeuger mit
Senkenauswertung war. Paket 4 hat das ausdrücklich als Abgrenzung notiert (Teil 8,
Abgrenzung 3): *„Erst mit Paket 5/6 muss die Stundenschleife über alle Erzeuger geführt
werden; dafür müssen deren Module stundenweise aufrufbar werden."*

Genau das ist jetzt fällig, und zwar aus drei unabhängigen Gründen:

1. **Ein Speicher, zwei Lader.** Sobald Solarthermie und Wärmepumpe denselben Puffer
   laden, müssen beide in derselben Stundenschleife laufen. Ein Vektormodul, das sein
   ganzes Jahr am Stück rechnet, würde den Speicher bis Stunde 8759 füllen und der
   nächsten Stufe einen Füllstand vom Silvesterabend in ihre Stunde 0 reichen. Das ist
   kein Rundungsproblem, sondern ein Kategoriefehler.
2. **`StundeAbschliessen` genau einmal je Stunde und Speicher** (Konzept 6.3, Phase G).
   Diese Zusage kann nur eine Stelle halten, die alle Stufen kennt.
3. **Projekte ohne Wärmepumpe.** 1017 und 1018 der Referenzmenge fahren BHKW + Kessel.
   Ihr Kessel soll einen Puffer laden können — mit der Schleife im WP-Modul gäbe es
   dafür keinen Rechenweg.

Die Schleife steht deshalb in der neuen Klasse `Kaskadenschleife`. Die Erzeugermodule
liefern **Stundenschritte**; die Ordnung (Kaskadenreihenfolge in Phase B,
Ladeprioritätsordnung in C/D, Entladepriorität in A/E) gehört der Schleife.

### Wer in der Schleife rechnet — und wer nicht

| Stufe | in der Schleife | sonst |
|---|---|---|
| **Wärmepumpe** | immer, wenn sie in der Kaskade steht | — |
| **Solarthermie** | wenn mindestens eine ihrer Anlagen einen Puffer als Haupt- oder Zweitsenke führt — **oder wenn sie zwischen zwei Mitgliedern steht** (Nacharbeit N4, Kapitel 13.4) | **zweikanalige Vektorstufe** an ihrer Kaskadenposition |
| **Heizkessel** | dito | **zweikanalige Vektorstufe** an ihrer Kaskadenposition |
| **BHKW** | nie (Paket 6) | einkanalig auf `Waermekanaele.Summe()`, Rest über `Uebernehmen()` |

**Warum eine Stufe ohne Puffer-Senke draußen bleiben darf:** Sie berührt keinen
Speicher. Ihr Ergebnis hängt allein vom Kanalzustand an ihrer Kaskadenposition ab, und
die Phasen A, C, D, E und G haben für sie keinen Inhalt. Sie in die Schleife zu ziehen
wäre nicht falsch, aber es würde die Bezugsgrößen der Wärmepumpe verschieben (Phase A
läuft vor Phase B) — ohne jeden fachlichen Gewinn. Der Preis ist ein sauberer
Regressionsnachweis: 1017, 1018, 1023 und 1024 rechnen mit gesetztem Flag exakt wie in
Paket 4 (Teil F.3).

**Das Kriterium** ist die SENKENREFERENZ einer Anlage (`WS_ID_Puffer` /
`WS_ID_Puffer2` **mit** Puffer-Ziel) — dieselbe Bedingung, aus der
`Ladeordnung.Ladereihenfolge` ihre Ladeaufträge bildet. Eine Alt-`WS_ID_Puffer`, die
noch steht, während `WS_Ziel` längst wieder auf `Heizkreis` zeigt, löst also **keine**
Speicherstufe aus: Es entstünde kein einziger Ladeauftrag daraus
(`SimulationControl.ErzeugerMitPufferSenke`).

### Die Kaskadenposition der Speicherstufe

Die Speicherstufe rechnet an der Kaskadenposition ihres **ersten** Mitglieds; weitere
Mitglieder werden dort mitgerechnet, in Phase B in ihrer Kaskadenreihenfolge
(`BedarfsreihenfolgeAufbauen`). Ein Erzeuger **ohne** Speicherbeteiligung, der zwischen
zwei Mitgliedern steht, rechnet nach der Speicherstufe.

> **BERICHTIGT durch die Nacharbeit (Befund N4, Kapitel 13.4).** Der Satz „das betrifft
> ausschließlich das BHKW" war **falsch**: Er galt auch für Solarthermie- und
> Kesselstufen ohne Puffer-Senke, und dort ohne jeden Hinweis. Seit der Nacharbeit werden
> genau diese Stufen zu Mitgliedern, sobald sie zwischen zwei Mitgliedern stehen — damit
> stimmt die Aussage jetzt: Es betrifft nur noch das BHKW, und der Fall wird protokolliert:

```
Kaskade: Das BHKW steht in der Kaskade zwischen zwei Erzeugern der Speicherstufe.
Es rechnet bis Paket 6 einkanalig als Vektormodul und deshalb NACH der gesamten
Speicherstufe.
```

**In der Referenzmenge tritt der Fall nicht auf.** Die Kaskadenreihenfolgen sind
(an der Datenbank nachgeprüft):

| Projekt | Tool_1..4 | Speicherstufe | BHKW |
|---|---|---|---|
| 1007, 1011 | Solarthermie → Wärmepumpe | WP (Solar hat keine Puffer-Senke) | — |
| 1008, 1010, 1021 | Wärmepumpe | WP | — |
| 1017, 1018 | BHKW → Heizkessel | keine (kein WP, Kessel ohne Puffer-Senke) | vorne |
| 1023 | Wärmepumpe → Heizkessel | WP | — |
| 1024 | Wärmepumpe → Heizkessel → BHKW | WP | hinten |

---

## 3. Teil A — Solarthermie (Konzept 6.4)

### Der Kappungspunkt

`SimulationSolarthermie.BerechneSolarthermie` rechnet in drei Schritten: spezifische
Leistung, potenzielle Erzeugung, **Bilanzierung**. Der dritte Schritt kappt:

```csharp
double produktion  = Math.Min(potenzielleErzeugung, waermebedarf);
double ueberschuss = Math.Max(0, potenzielleErzeugung - waermebedarf);   // verworfen
```

Entscheidend für die Umsetzung ist, was die ersten beiden Schritte tun: Sie hängen
**ausschließlich** von Wetter, Ausrichtung und Kollektorkennwerten ab — nicht vom
Wärmebedarf und nicht vom Speicherfüllstand. Genau deshalb lässt sich die Solarthermie
überhaupt in eine Stundenschleife einfügen: **Ihr Potenzial steht vorab fest, die
Verwendung entscheidet sich erst in der Stunde.**

`Vorbereiten_Zweikanalig` rechnet das Potenzial je Kollektorfeld für alle 8760 Stunden
vorab — mit denselben Aufrufen und in derselben Reihenfolge wie `Berechnung()`, also mit
denselben Zahlen. In der Stunde gilt dann:

| Phase | Solarthermie |
|---|---|
| **B** | Felder mit Hauptsenke `Heizkreis` decken den Momentanbedarf ihres Kanals (`WS_Typ`; bei „Beides" mit Warmwasservorrang). Ein Feld mit Puffer-Hauptsenke deckt **nichts** — es lädt ausschließlich |
| **C/D** | `Zweikanalig_Laden` je Ladeauftrag: Hauptsenke zuerst, Zweitsenke aus dem Rest (13.5, Variante A). Bilanzraum und Durchsatzbudget unverändert aus Paket 4 |
| Stundenende | was weder gedeckt noch gespeichert wurde, ist **verworfen** und wird als `Ueberschuss` gebucht |

Damit gilt: `Waermeproduktion = Direktdeckung + Speicherladung`, und `Ueberschuss` ist
das, was vor Paket 5 der **gesamte** Überschuss war.

### Die Ladeordnung: Solarthermie mit Vorgaberang 10

Solarthermie-Anlagen kommen jetzt über `SimulationControl.LadeordnungAufbauen` in die
kaskadenübergreifende Ladeordnung. Die Vorgabe-Rangfolge aus Konzept 3.4 wirkt damit
erstmals: Solarthermie **10** vor Wärmepumpe **20** vor BHKW 30 vor Heizkessel **40** —
unabhängig von der Kaskadenposition. Der Nachweis am Lauf steht in Teil F.6.

### Der Doppelzählungs-Freibeweis bleibt

Er ist strukturell, nicht numerisch: Eine Anlage hat genau **eine** Hauptsenke und ist
damit eindeutig in Phase B **oder** in Phase C. Phase C ruft kein `SenkeAbziehen`. Das
gilt für Solarthermie und Kessel wortgleich wie für die Wärmepumpe. Gemessen wird es über
die Stundenbilanz (Teil F.4), die Produktion und Speicherbewegung getrennt zählt.

---

## 4. Teil B — die Pflicht-Mitkorrektur in `SimulationRunner`

Konzept 6.4 nennt sie ausdrücklich: `SimulationRunner` bildet den Restbedarf der
Solarthermie als Differenz aus Stufeneingang und **Gesamtproduktion**. Sobald die
Produktion zusätzlich einen Puffer lädt, wächst sie über den Momentanbedarf hinaus — der
Restbedarf wird **negativ**, die Deckung überschreitet 100 %, und beides landet ungeprüft
in `Tab_ErgebnisSolarthermie` und von dort in Variantenbericht und Wirtschaftlichkeit.

**Die Korrektur** (konsistent zu B0-7, das den Runner bereits auf den Eigenanteil
umgestellt hat):

```csharp
double solarDirekt = st.Waermeproduktion_gesamt - st.Speicherladung_gesamt;
if (solarDirekt < 0) solarDirekt = 0;      // Rundungsschutz

stm.Restwaermebedarf     = (st.Waermebedarf_gesamt - solarDirekt) / 1000.0;
stm.Waermebedarfsdeckung = solarDirekt * 100.0 / st.Waermebedarf_gesamt;
```

**Bezugsgröße ist die Direktdeckung.** Die gespeicherte Wärme deckt Bedarf erst später
und über den Speicher; sie einem Erzeuger zuzurechnen wäre eine Doppelzählung, sobald
zwei Erzeuger denselben Puffer laden (genau der Fall aus Teil F.6). Der volle Ertrag
steht weiterhin in `Waermeproduktion` und getrennt in `Speicherladung_gesamt`.

> **ÜBERHOLT für die `Waermebedarfsdeckung` (Nacharbeit N2, Kapitel 13.2).** Die
> Doppelzählung lässt sich auflösen, statt sie zu vermeiden: Seit der Nacharbeit wird die
> Speicherentladung nach der Interimsregel „Vermischung im Speicher" auf die Lader
> aufgeteilt, und der Deckungsgrad ist
> `(Direktdeckung + zugerechnete Entladung) / Bezugsbedarf`. Der **Restwärmebedarf**
> bleibt unverändert auf der Direktdeckung — er ist die Größe der Kaskadenposition, und
> nur so gilt `Restbedarf ≥ 0` konstruktiv. Dieselbe Regel gilt seit der Nacharbeit auch
> für den **Heizkessel**, für den die Mitkorrektur ganz gefehlt hatte (Befund N1).

Damit gilt konstruktiv: `Restwaermebedarf ≥ 0` (die Direktdeckung kann den Stundenbedarf
nicht übersteigen) und `Deckung ≤ 100 %` — **ohne** Kappung.

**Im Altpfad ist die Korrektur nachweislich wirkungslos:** Dort lädt die Solarthermie
keinen Puffer, `Speicherladung_gesamt` ist exakt `0,0`, die Subtraktion ist die Identität
und die Klemmung greift nie (`Waermeproduktion_gesamt` ist eine Summe nichtnegativer
Werte). Belegt ist das nicht nur durch Lesen, sondern durch den byte-identischen
Regressionslauf (Teil F.2).

---

## 5. Teil C — Heizkessel (Konzept 6.5, erster Punkt)

### Zweikanalig ohne zweiten Schleifendurchlauf

Konzept 6.5 beschreibt die Umstellung als „zweiten Schleifendurchlauf mit erhaltenem
Zwischenzustand". Umgesetzt ist genau das, nur ohne zweiten Durchlauf: `Stunde_Bedarf`
bedient in EINER Stunde erst den einen, dann den anderen Kanal — bei `WS_Typ = Beides`
mit Warmwasservorrang, wie überall in dieser Engine. Der Zwischenzustand liegt in zwei
Feldern je Kessel:

- `_restLeistung[i]` — noch nicht vergebene Nennleistung der Stunde,
- `_kesselStunde[i]` — bereits abgegebene Nutzwärme der Stunde.

Die Lastverteilung selbst ist unverändert: Kessel 0 nimmt `min(Leistung, Bedarf)`,
Kessel 1 den Rest, und so fort. Bei `WS_Typ = Beides` ist die je Stunde abgegebene
Gesamtmenge damit dieselbe wie im Altpfad — nur die Aufteilung auf die Kanäle ist neu.

### Die Bereitschaftsverluste — genau einmal je Stunde und Kessel

Das ist die harte Bedingung aus Konzept 6.5. Sie ist dadurch erfüllt, dass die
Brennstoffbilanz **nicht** in der Bedarfsphase steht, sondern in einer eigenen Methode
`Stunde_Abschluss`, die die `Kaskadenschleife` in **Phase G** aufruft:

```
if (_kesselStunde[i] > 0)  Verbrauch = _kesselStunde[i] / Wirkungsgrad     // Kessel lief
else                       Verbrauch = Bereitschaftsfaktor · Nennleistung  // Kessel stand
```

Erst in Phase G steht fest, ob der Kessel in dieser Stunde gelaufen ist — er kann seine
Wärme in der Bedarfsphase B **oder** in der Ladephase C abgegeben haben. Eine Entscheidung
je Kanal hätte den Stillstandsverlust in einer Stunde zweimal gebucht; der
Jahresnutzungsgrad (Schritt 5) wäre entsprechend gekippt. Der Äquivalenznachweis steht in
Teil F.5.

### Senkenauswertung je Kessel

`Zweikanalig_Laden` lädt den zugeordneten Puffer bis zur Abschaltschwelle. Die Schwelle
kommt aus `Ladeauftrag.ObergrenzeStunde` und ist nach der Auflösungsregel 3.4 bereits
bestimmt: eigene Ladegrenze, sonst `Schwelle_Aus` für die vorrangige und
`Schwelle_Aus_Nachrang` für nachrangige Anlagen. Mit Vorgaberang **40** ist der Kessel der
letzte Lader — wo eine Solar-Reservezone gepflegt ist, lädt er nur bis dorthin.

### Was am Altpfad geändert wurde

Genau eine Sache: Die Schritte 4 und 5 von `Berechnung()` (globale Brennstoffzähler,
Emissionen, Jahresnutzungsgrad) stehen jetzt in `Bilanz_und_Nutzungsgrad(Anzahl)`, weil
der zweikanalige Weg sie Zeile für Zeile gleich braucht. **Die ausgeführten Anweisungen
und ihre Reihenfolge sind unverändert**; `Heizkessel_Simulation` ist zeichengleich.

---

## 6. Teil D — was von Paket 4 unangetastet bleibt

| Zusage aus Paket 4 | Status in Paket 5 |
|---|---|
| **Bilanzraum** (Nutzerentscheidung 4b-1): Ladefähigkeit + min(offener Kanalbedarf, Entnahmefähigkeit); Durchsatzbudget je Kanal nur EINMAL vergeben; Phase E gibt den Durchfluss zuerst zurück | unverändert. `absehbar[2]` liegt jetzt in der `Kaskadenschleife` und wird von **allen** Ladeaufträgen gemeinsam verbraucht — genau die Regel „je Kanal nur einmal", jetzt auch über Erzeugerarten hinweg |
| **Doppelzählungs-Freibeweis**: Phase C ruft kein `SenkeAbziehen` | unverändert, und für Solarthermie und Kessel wortgleich umgesetzt |
| **`StundeAbschliessen` genau 1× je Stunde und Speicher** | unverändert; die Schleife ruft es zentral in Phase G. Instrumentierung `Abschluesse` = 8760 in allen Läufen (Teil F.4) |
| **PV-Ladebudget sequenziell Haupt → Zweit**, Abzug beim tatsächlichen Laden (13.5) | unverändert; `pvRest` wird als `ref` durch beide Ladephasen gereicht. Solarthermie und Kessel binden kein PV-Budget (nur Wärmepumpen kennen `BM_Typ = PV`) |
| **Zeitabhängige Ladeobergrenzen** (3.5): zwei vorsortierte Ladeordnungen | unverändert; die zweite Auflösung läuft über ALLE Einträge des Puffers, also auch über Solarthermie und Kessel |
| **Quellspeicher, Regeneration, Kurzschluss Quelle = Senke** | unverändert; Regeneration weiterhin einmal je Speicher und Stunde im Stundenkopf |

**Nachweis durch Lesen, nicht nur durch Messen:** Der Anweisungsblock der Phase B der
Wärmepumpe ist zeichengleich in die neue Methode gewandert — ein Vergleich beider Fassungen
ohne Einrückung liefert **keine einzige Differenz** (145 Zeilen). Dasselbe gilt für
`DurchsatzEntladen` (24 Zeilen), `EntladeKanal` (32 Zeilen) und den Rumpf von
`SenkeAbziehen`. Nur `Ladephase` ist umgebaut: Aus der Schleife über die Ordnung wurde ein
Aufruf je Auftrag, aus jedem `continue` ein `return 0`.

---

## 7. Teil E — Verifikation

### F.1 Build

```
MSBuild WP-Plan.sln -t:Rebuild -p:Configuration=Debug -p:Platform=x86   ->  0 Fehler
MSBuild Referenzlauf\Referenzlauf.csproj  …                             ->  0 Fehler
```

Warnungen: **dieselben sechs Bestandswarnungen** wie vor der Änderung
(`WErzeugerModel.cs` CS0108, `StromverbraucherStammCtrl.cs` CS0108,
`KlimaregionStammCtrl.cs` 2 × CS0109, `MDIMainForm.cs` CS4014 und CS1998) — **keine neue**,
geprüft über einen vollständigen `-t:Rebuild`.

### F.2 Flag AUS — Regression (Pflicht), byte-identisch

Neun Referenzprojekte auf einer eigenen, vollständig migrierten Kopie
(`C:\Waermeplan\Paket5_Test\DB_Basis`), verglichen gegen die aktuelle Basis
`Referenzlaeufe/2026-08-14_B1-Fixes`:

```
Projekt_1007: PASS (29 Dateien, 324210 Werte)   Projekt_1018: PASS (19 Dateien, 210343 Werte)
Projekt_1008: PASS (21 Dateien, 227847 Werte)   Projekt_1021: PASS (21 Dateien, 227840 Werte)
Projekt_1010: PASS (18 Dateien, 201540 Werte)   Projekt_1023: PASS (25 Dateien, 262917 Werte)
Projekt_1011: PASS (29 Dateien, 324232 Werte)   Projekt_1024: PASS (26 Dateien, 271686 Werte)
Projekt_1017: PASS (20 Dateien, 245378 Werte)

GESAMT: PASS (2295993 Werte innerhalb der Toleranz)
```

**Stärkerer Nachweis als die Toleranzprüfung:** Ein rekursiver Byte-Vergleich der beiden
Ordner meldet **keine einzige abweichende CSV-Datei** — weder in den Aggregaten noch in
einer der Ganglinien. Die Zusage „Flag aus = byte-identisch" ist damit nicht ausgelegt,
sondern gemessen.

`Referenzlauf.exe pruefen` meldet für alle neun Projekte „plausibel".

> Der Lauf wurde nach der letzten Codeänderung (Rundungsschutz in `SimulationRunner`)
> **wiederholt** und ist wieder byte-identisch.

### F.3 Flag AN — derselbe Code-Umbau gegen Paket 4

Verglichen wurde **derselbe Datenbestand mit gesetztem Flag**, einmal mit dem
Paket-4-Stand (`Lauf_An_P4`, aus dem gestashten Arbeitsbaum gebaut) und einmal mit dem
Paket-5-Stand (`Lauf_An`):

| Projekt | Ergebnis | Bewertung |
|---|---|---|
| 1007, 1008, 1010, 1017, 1018, 1021, 1023, 1024 | **PASS** | Der Umbau der Stundenschleife, die zweikanalige Kesselstufe und die zweikanalige Solarstufe sind für diese Projekte **verhaltensneutral** |
| 1011 | **FAIL — ausschließlich `wp_warmwasserbedarf`** | erklärt, siehe unten |

Die 1011-Abweichung im Einzelnen (alle Vektoren des Projekts vermessen):

| Datei | max. Abweichung | Jahressumme (neu − alt) |
|---|---|---|
| `wp_warmwasserbedarf.csv` | 0,855 kWh | **−352,563 kWh** (4.056,45 → 3.703,89) |
| `wp_waermebedarf.csv` | 1,3·10⁻⁴ kWh | −0,0035 kWh |
| `restwaerme.csv`, `wp_restwaerme.csv` | 2,5·10⁻⁴ kWh | +0,0013 kWh |
| `solar_waermebedarf.csv`, `solar_restwaerme.csv` | 7·10⁻⁵ kWh | +0,0006 kWh |
| `aggregate.csv` | genau **ein** Wert: `Vektor.wp_warmwasserbedarf.Summe` | |

**Ursache — und sie ist der Zweck von Paket 5.** Im Paket-4-Stand rechnete die
Solarthermie einkanalig auf `Waermekanaele.Summe()`, und ihr Rest wurde über
`Uebernehmen()` **proportional** auf die Kanäle zurückverteilt. 1011 hat 5.105 MWh
Stufeneingang gegen 4.056 kWh Warmwasser — der Warmwasseranteil liegt bei 0,08 %, also
bekam der WW-Kanal proportional so gut wie nichts ab. Seit Paket 5 deckt die Solarthermie
ihren Kanal nach `WS_Typ`, und das ist „Beides" mit **Warmwasservorrang**: Von den
643,58 kWh Solarertrag landen jetzt 352,56 kWh auf dem Warmwasserkanal. Die Wärmepumpe
sieht entsprechend weniger Warmwasserbedarf.

Das ist derselbe Effekt, den Konzept Kapitel 9 für die Kanalumstellung ankündigt
(„die WW-Deckung in allen Projekten, in denen die WP nicht an erster Kaskadenposition
steht") — nur diesmal ausgelöst von der Solarthermie statt von der Kanalbildung. Alle
übrigen Größen bleiben im Rundungsbereich.

`Referenzlauf.exe pruefen` meldet für alle neun Projekte mit gesetztem Flag „plausibel".

### F.4 Flag AN — Bilanzen, Abschlüsse, Solar-Kennzahlen der Referenzprojekte

Gemessen mit einer eigenen headless-Probe (`Probe5`, rechnet über
`SimulationRunner.Simuliere` und **speichert nicht**) auf `DB_Flag`:

| Projekt | Stundenbilanz `Eingang − Rest == Produktion + Heizstab + Solar + Kessel + Entladung − Ladung`, max. | Summe der Beträge | Speicher | `StundeAbschliessen` |
|---|---|---|---|---|
| 1007 | 1,91·10⁻⁶ kWh | 0,00105 kWh | 0 | — |
| 1011 | 1,18·10⁻⁴ kWh | 0,173 kWh | 0 | — |
| 1023 | 1,53·10⁻⁵ kWh | 0,00784 kWh | 1 | **8760/8760** |
| 1024 | 1,53·10⁻⁵ kWh | 0,00729 kWh | 1 | **8760/8760** |

Speicherbilanz `Ladung − Entladung − Verluste == ΔSOC`:

| Speicher | Q_max | Ladung | Entladung | Verluste | SOC Ende | SOC_Max | Vollzyklen | Bilanzfehler |
|---|---|---|---|---|---|---|---|---|
| 1023 · `1018023` | 13,9200 | 109.993,238 | 109.638,385 | 354,854 | 0,0000 | 13,1409 | 7.901,81 | **−4,6·10⁻⁹** |
| 1024 · `1054164` | 10,4400 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

Der Wert für 1023 deckt sich auf die Stelle mit der Paket-4-Nacharbeit (dort 7.902
Vollzyklen) — die Umstellung hat den Rechenweg der Wärmepumpe nicht angetastet. Der
Speicher in 1024 gehört zur **Zweitsenke eines BHKW** und ruht deshalb bis Paket 6; er
schließt seine Stunde trotzdem 8760-mal ab und steht in der Bilanz, statt still
herauszufallen.

Solarthermie (Konzept 6.4, Prüfpunkt „Restbedarf ≥ 0, Deckung ≤ 100 %"):

| Projekt | Stufeneingang | Produktion (Direkt + Speicher) | verworfen | Restbedarf | Deckung |
|---|---|---|---|---|---|
| 1007 | 56,898 MWh | 0 (kein Kollektorfeld im Projekt) | 0 | 56,898 MWh ≥ 0 | 0 % ≤ 100 |
| 1011 | 5.105,254 MWh | 0,644 MWh (0,644 + 0) | 0 | 5.104,610 MWh ≥ 0 | 0,0126 % ≤ 100 |

### F.5 Heizkessel — Äquivalenznachweis der Bereitschaftsverluste

> **Zahlenherkunft** (Nacharbeit N11): Die Werte dieser Tabelle stammen aus einer
> **Wegwerf-Probe** (`Probe5`, rechnet über `SimulationRunner.Simuliere` und speichert
> nicht), nicht aus der Referenzlauf-Suite. Sie sind mit `Referenzlauf.exe` **nicht**
> reproduzierbar — die Suite exportiert `Kessel_Jahresnutzungsgrad_Spk` und die
> Brennstoffsummen nicht in dieser Auflösung. Reproduktion: Kapitel 12, Schritt 4.

Geprüft wird die Forderung aus Konzept 6.5: *dieselbe Jahressumme je Kessel im ein- und
im zweikanaligen Durchlauf bei gleichem Lastprofil*. Genau das leisten die Projekte 1017,
1018 und 1024 — in ihnen ändert die Kanalumstellung das Lastprofil des Kessels nicht
(er steht jeweils am Ende der Kaskade bzw. vor einem Vektormodul, das ohnehin auf der
Kanalsumme rechnet):

| Projekt | Größe | einkanalig (Flag aus) | zweikanalig (Flag an) |
|---|---|---|---|
| **1017** | Nutzwärme | 17,134517 MWh | **17,134517 MWh** |
| | Brennstoff gesamt | 25,245308 MWh | **25,245308 MWh** |
| | **Bereitschaftsverluste** | **8,110791 MWh** | **8,110791 MWh** |
| | Jahresnutzungsgrad | 67,872086 % | **67,872086 %** |
| **1018** | Nutzwärme / Brennstoff | 34,272457 / 34,272457 MWh | identisch |
| | Jahresnutzungsgrad | 100 % | 100 % |
| **1024** | Nutzwärme | 35,541419 MWh | identisch |
| | Jahresnutzungsgrad | 94 % | 94 % |

**Warum 1017 der aussagekräftige Fall ist:** Dort machen die Stillstandsverluste
8,11 MWh aus, also fast ein Drittel des Brennstoffeinsatzes. Würden sie je Kanal gebucht,
stiege der Verbrauch auf rund 33,36 MWh und der Jahresnutzungsgrad fiele auf etwa
51,4 %. Gemessen sind 25,245308 MWh und 67,872086 % — auf sechs Nachkommastellen
identisch mit dem einkanaligen Lauf.

Projekt **1023** weicht zwischen Flag aus und Flag an ab (Nutzwärme 64,322 → 66,892 MWh,
Nutzungsgrad 84,109 → 84,686 %). Das ist **nicht** die Kesselumstellung, sondern die in
Paket 4 dokumentierte Wirkung der Puffer-Hauptsenke an den Wärmepumpen: Der Stufeneingang
des Kessels steigt von 189,603 auf 192,173 MWh. Der Vergleich Paket 4 gegen Paket 5 mit
gesetztem Flag ist für 1023 **PASS** (F.3) — der Kessel rechnet also gleich, er bekommt
nur eine andere Last.

### F.6 Präparierte Szenarien (die Referenzmenge deckt sie nicht ab)

**Kein Projekt der Referenzmenge trägt eine Puffer-Senke an Solarthermie oder Kessel** —
an der Datenbank nachgeprüft. Die neuen Pfade sind deshalb auf eigenen Kopien präpariert
worden. Grundlage ist jeweils `DB_Flag`; die produktive Datenbank wurde nur gelesen.

#### S1 — Solar-Überschuss: verworfen gegen genutzt

Projekt 1007 (56,9 MWh/a Wärmebedarf) bekommt ein Kollektorfeld mit 400 m²
Aperturfläche (`Tab_Energieanlagen` ID 1099001, Kollektor aus dem Katalog, 500 Module ×
0,8 m², Neigung 35°). Das Feld ist grob überdimensioniert — genau darum zeigt es den
Überschuss.

| Variante | Konfiguration des Felds | Produktion gesamt | davon Direktdeckung | davon Speicherladung | **verworfen** |
|---|---|---|---|---|---|
| **S1a** | Hauptsenke `Heizkreis`, keine Zweitsenke | 7,269 MWh | 7,269 MWh | 0 | **209,463 MWh** |
| **S1c** | dazu Zweitsenke `PufferHeizung` (Puffer 1007007, 600 l, Q_max 13,92 kWh) | 10,162 MWh | 3,305 MWh | **6,857 MWh** | **206,571 MWh** |
| **S1d** | wie S1c, Puffer auf 20.000 l (Q_max 464 kWh) | **27,410 MWh** | 3,054 MWh | **24,356 MWh** | **189,323 MWh** |

**Der Kappungspunkt ist weg.** Zwischen S1a und S1c wandern **2,892 MWh** aus dem Verwurf
in den nutzbaren Ertrag, zwischen S1a und S1d **20,141 MWh** — die Zahl der verworfenen
MWh sinkt um genau denselben Betrag, um den die Produktion steigt. Dass der Rest weiter
verworfen wird, ist keine Grenze der Umsetzung, sondern der Anlage: 400 m² auf einem
Projekt mit 6,5 kW mittlerer Last. Die Abhängigkeit vom Speichervolumen (S1c → S1d) zeigt
zugleich, dass der Weg wirklich über den Speicher läuft.

Bilanzen der Speicher in diesen Läufen:

| Variante | Speicher | Ladung | Entladung | Verluste | SOC Ende | SOC_Max | Vollzyklen | Bilanzfehler | Abschlüsse |
|---|---|---|---|---|---|---|---|---|---|
| S1c | `1007007` (13,92) | 6.856,811 | 6.557,990 | 298,821 | 0 | 13,141 | 492,59 | 1,6·10⁻¹⁰ | 8760/8760 |
| S1d | `1007007` (464,00) | 24.356,266 | 23.868,462 | 487,803 | 0 | 440,717 | 52,49 | −8,3·10⁻¹⁰ | 8760/8760 |

Stundenbilanz der Speicherstufe (inklusive Solaranteil): max **1,91·10⁻⁶ kWh** (S1c) bzw.
**6,84·10⁻⁶ kWh** (S1d). Restwärme des Projekts: 5,685 MWh (S1a) → 5,667 (S1c) → **4,437
MWh** (S1d). `Restwaermebedarf ≥ 0` und `Deckung ≤ 100 %` in allen Varianten.

#### S1b — Hauptsenke Brauchwasser, Zweitsenke Heizung (13.5, Variante A)

Dasselbe Feld, aber Hauptsenke `PufferBrauchwasser` (Puffer 1018015, 778 l, 60/45 °C,
Q_max 13,537) und Zweitsenke `PufferHeizung` (Puffer 1007007, 50/30 °C, Q_max 13,920):

```
PUFFER_1018015  Brauchwasser  Ladung 3.134,224  Entladung 2.639,073  Verluste 494,414  SOC_Ende 0,737  8760/8760
PUFFER_1007007  Heizung       Ladung 7.993,581  Entladung 7.695,158  Verluste 298,423  SOC_Ende 0,000  8760/8760
Solar-Produktion gesamt 11.127,804 kWh = 3.134,224 + 7.993,581  ->  Abweichung 0
```

Die Reihenfolge „Hauptsenke bis zu ihrer Ladeobergrenze zuerst, erst der Rest an die
Zweitsenke" geht **exakt** auf: Kein kWh doppelt, kein kWh verloren. Speicherbilanzfehler
−7,9·10⁻¹¹ und 1,6·10⁻¹⁰ kWh, Stundenbilanz max 2,72·10⁻⁶ kWh.

> **Zur Arithmetik der drei Zahlen** (Nacharbeit N11): Die Summe der ANGEZEIGTEN Werte
> ist 3.134,224 + 7.993,581 = **11.127,805**, im Block steht 11.127,804. Das ist reine
> Anzeigerundung — die vollen Werte sind 3.134,223697 und 7.993,580564, ihre Summe
> 11.127,804261, und genau das ist auch die Produktion. Die Aussage „Abweichung 0" gilt
> in **voller Genauigkeit** (gemessen −5,5·10⁻¹² kWh), nicht auf den gedruckten Stellen.

#### S2 — Heizkessel lädt einen Puffer, **ohne Wärmepumpe im Projekt**

Projekt 1018 (BHKW → Heizkessel, keine WP in der Kaskade): Puffer 1018007 auf
`Verwendung = Heizung`, 70/55 °C (Q_max 10,440); Kessel 10369 auf
`WS_Ziel = PufferHeizung`.

```
PUFFER_1018007  Heizung  Q_max 10,4400
   Ladung 35.046,196   Entladung 34.308,732   Verluste 727,629   SOC_Ende 9,835
   SOC_Max 9,835   Vollzyklen 3.356,92   Bilanzfehler 4,5·10⁻⁹   Abschluesse 8760/8760

Kessel 'Vitocrossal 200 CM2'  Nutzwaerme 35,046 MWh   Jahresnutzungsgrad 100 %
Stufeneingang des Kessels 22,054 MWh (vorher 34,324) — der Puffer deckt in Phase A vor
Restwaerme des Projekts 51,78 kWh -> 15,51 kWh
```

Damit ist dreierlei belegt: Die Stundenschleife läuft **ohne Wärmepumpe**; der Kessel
lädt seinen Puffer bis zur Abschaltschwelle und deckt den Bedarf aus ihm; und
`StundeAbschliessen` läuft auch in diesem Weg genau 8760-mal.

#### S3 — Ladepriorität: Solarthermie (10) vor Wärmepumpe (20) am selben Puffer

Auf S1b aufgesetzt: Wärmepumpe 10353 bekommt ebenfalls `WS_Ziel = PufferHeizung` auf
Puffer 1007007, dessen `Schwelle_Aus_Nachrang` auf 50 % gesetzt wird (Solar-Reservezone).
Damit konkurrieren zwei Erzeuger **verschiedener Kaskadenpositionen** an einem Speicher.

| Variante | Ladepriorität der Solarthermie | Solar-Produktion | verworfen |
|---|---|---|---|
| **S3** | Vorgabe **10** (vorrangig) | **9,027 MWh** | 207,706 MWh |
| **S3b** | manuell **30** (hinter der Wärmepumpe) | 7,174 MWh | 209,559 MWh |

Die Vorgabe-Rangfolge aus Konzept 3.4 verschiebt **1,853 MWh** Solarertrag vom Verwurf in
die Nutzung — bei sonst identischer Konfiguration. Das ist der Nachweis, den Paket 4 unter
Befund **4b-3** offenlassen musste („braucht die Solarthermie-Senke aus Paket 5"): Zwei
konkurrierende Lader an einem Puffer, deren Reihenfolge nicht der Kaskade folgt. Der
Puffer selbst bleibt in beiden Varianten sauber bilanziert (Ladung 27.698,167 bzw.
28.091,898 kWh, Bilanzfehler 2,0·10⁻¹¹ bzw. −3,0·10⁻⁹, Abschlüsse 8760/8760).

### F.7 Kodierung und Diff

| Datei | BOM | Zeilenenden |
|---|---|---|
| `SimulationSolarthermie.cs`, `SimulationSPK.cs`, `SimulationControl.cs`, `SimulationWaermepumpe.cs`, `SimulationKanaele.cs`, `SimulationRunner.cs` | ja (unverändert) | CRLF |
| `Kaskadenschleife.cs` (neu) | ja | CRLF |

`git diff --check` meldet für die geänderten Dateien nichts; im Diff steht **kein**
Ersatzzeichen U+FFFD und kein Mojibake. Der Diff von `SimulationSolarthermie.cs` ist rein
additiv (374 Zeilen hinzu, **0** entfernte Zeilen). Keine Designer- und keine
`.resx`-Datei angefasst; die gesperrten Dateien sind unberührt.

---

## 8. Dokumentierte Ergebnisänderungen mit Flag AN

| # | Änderung | Wirkung | Grundlage |
|---|---|---|---|
| 1 | **Die Solarthermie deckt ihren Kanal nach `WS_Typ`** (bei „Beides" mit Warmwasservorrang) statt proportional über `Uebernehmen()` | 1011: `wp_warmwasserbedarf` 4.056,45 → 3.703,89 kWh. Projekte ohne Solarthermie oder mit Solarertrag 0 unverändert | 6.1 (Ende des Kompatibilitätsankers für die Solarthermie), 3.2 |
| 2 | **Der Heizkessel deckt seinen Kanal nach `WS_Typ`** | in der Referenzmenge **keine** Wirkung: Der Kessel steht dort stets am Ende der Kaskade oder vor einem Vektormodul, das auf der Kanalsumme rechnet. Die je Stunde abgegebene Nutzwärme ist unverändert | 6.5 |
| 3 | **Solarthermie kann Puffer laden** (Haupt- und Zweitsenke); der Überschuss wird nicht mehr verworfen | nur Projekte mit Puffer-Senke an einem Kollektorfeld (in der Referenzmenge keines). Zahlen: S1 in F.6 | 6.4 |
| 4 | **Heizkessel kann Puffer laden**; die Stundenschleife läuft auch ohne Wärmepumpe | nur Projekte mit Puffer-Senke am Kessel (in der Referenzmenge keines). Zahlen: S2 in F.6 | 6.5 |
| 5 | `Tab_ErgebnisSolarthermie.Restwaermebedarf` folgt der **Direktdeckung** | ohne Puffer-Senke der Solarthermie bitgleich der bisherige Wert | 6.4 (Mitkorrektur) |
| 6 | Ein BHKW, das in der Kaskade **zwischen** zwei Erzeugern der Speicherstufe steht, rechnet nach der Speicherstufe | tritt in der Referenzmenge nicht auf; wird protokolliert | Abgrenzung, Paket 6 |
| 7 | `Tab_ErgebnisHeizkessel.Restwaermebedarf` folgt der **Direktdeckung**, `.Waermebedarfsdeckung` und die der Solarthermie dem **Eigenanteil** (Direktdeckung + zugerechnete Speicherentladung) | ohne Puffer-Senke bitgleich der bisherige Wert; in der Referenzmenge **null Abweichung** (13.10) | Nacharbeit N1/N2 |
| 8 | Ein Heizkessel **in** der Speicherstufe deckt in **Phase B**, also VOR dem Heizstab (Altpfad und Vektorstufe: danach) | nur mit Puffer-Senke am Kessel. Am präparierten 1023: Kessel-Nutzwärme 66,89 → 114,78 MWh, Gas 78,99 → 132,02 MWh, Heizstab 87,92 → 47,85 MWh | 6.3 (Heizstab ist Phase F, letzte Instanz) — 13.9 |
| 9 | Eine Solar-/Kesselstufe **zwischen** zwei Mitgliedern wird selbst Mitglied und rechnet an ihrer Kaskadenposition | nur bei ≥ 2 Mitgliedern; in der Referenzmenge keines. Am präparierten 1011: Solarproduktion 0,28 → 0,446 MWh | Nacharbeit N4 |
| 10 | Ein Puffer-**Ziel ohne Puffer-Referenz** gilt als Heizkreis (`Normalisieren`), und eine Puffer-Hauptsenke ohne Ladeauftrag fällt auf den Heizkreis zurück — beides mit Protokollzeile | betrifft nur fehlerhaft konfigurierte Anlagen; wirkt auch in den Dialogen | Nacharbeit N5 |

---

## 9. Bewusste Abgrenzungen

| # | Abgrenzung | Begründung |
|---|---|---|
| 1 | **BHKW bleibt am `Uebernehmen`-Anker** und wertet seine Senken nicht aus | Seine Speicherlogik steckt in drei Fahrweisen-Implementierungen (Konzept 6.5, zweiter Punkt) — das ist Paket 6. Eine migrierte Puffer-Senke am BHKW ruht und wird beim Kontextaufbau protokolliert (Projekt 1024, Anlage 11257) |
| 2 | **Solarthermie und Heizkessel ohne Puffer-Senke bleiben Vektorstufen** an ihrer Kaskadenposition — **außer sie stehen zwischen zwei Mitgliedern** (Nacharbeit N4) | Ohne Speicherbeteiligung haben die Phasen A, C, D, E und G für sie keinen Inhalt. Sie in die Schleife zu ziehen würde die Bezugsgrößen der Wärmepumpe verschieben, ohne etwas zu gewinnen — und den sauberen Regressionsnachweis kosten. **Zwischen** zwei Mitgliedern wiegt das anders: dort wäre der Preis ein stiller Positionswechsel (13.4) |
| 3 | **`Waermebedarf_stuendlich` der Wärmepumpe** ist der Kanalzustand beim EINTRITT in die Speicherstufe | Solange nur die Wärmepumpe in der Stufe rechnet — der Fall der gesamten Referenzmenge —, ist das exakt die Paket-4-Größe. Steht eine Solar- oder Kesselstufe MIT Puffer-Senke davor, enthält der Wert deren Deckung noch. **Die Folge für den Deckungsgrad ist mit N2 beseitigt** (er kommt nicht mehr aus dieser Differenz); die ANZEIGEGRÖSSE `Waermebedarf` selbst bleibt der Stufeneingang. Sauber auflösen lässt sich das erst, wenn jede Stufe ihren eigenen Stufeneingang je Stunde führt — Kandidat für Paket 7/10 |
| 4 | **`waermerestbedarf_stuendlich` der Wärmepumpe** ist der Rest nach der GESAMTEN Speicherstufe | dieselbe Familie wie 3; in der Referenzmenge identisch mit dem Paket-4-Wert |
| 5 | **Solarthermie bindet kein PV-Budget** | Der Betriebsmodus `PV` (`BM_Typ`) existiert nur an Wärmepumpen (Konzept 3.5). Für die Solarthermie wäre er auch sinnlos — sie verbraucht keinen Strom |
| 6 | **Kein neuer Speicherparameter** (Lade-/Entladeleistung je Speicher in kW) | steht als Konzept-Nachtrag zu 3.4 vorgemerkt, Default unbegrenzt; die Engine hält die Stelle offen (`EntladeleistungMax`, `Entnahmefaehigkeit()`). Unverändert offen aus Paket 4 (N.15, Punkt 1) |
| 7 | **`solarthermie_list` bleibt toter Code** | Konzept 6.2 stellt ausdrücklich fest, dass die Liste nie gelesen wird. Die neue `solar_anlagen_ids` tritt daneben, nicht an ihre Stelle — dieselbe Konstruktion wie `bhkw_anlagen_ids` und `spk_anlagen_ids` |

---

## 10. Nutzerentscheidungen

> **Stand 15.08.2026:** 5-1 ist **bestätigt** (siehe unten), **5-2 bleibt offen**.

### 5-1 — Der Deckungsgrad der Solarthermie zeigt die gespeicherte Wärme nicht · **BESTÄTIGT 15.08.2026**

> **Entscheidung des Nutzers vom 15.08.2026: die Zurechnungsregel „Vermischung im
> Speicher" ist bestätigt** — mit allen sechs Teilantworten 5-1a bis 5-1f, wie sie die
> Nacharbeit umgesetzt hat. Sie ist damit **keine Interimsregel mehr**, sondern die
> gültige Regel des Rechenkerns; sie trägt seit Paket 6 vier Erzeugerarten (WP,
> Solarthermie, Heizkessel, BHKW). **Keine Codeänderung** — der Stand der Nacharbeit ist
> der bestätigte Stand. Die beiden nicht umgesetzten Teile bleiben Vormerkungen, keine
> offenen Entscheidungen: die eigene Ergebnisspalte `Speicherladung` (5-1d,
> Schemaänderung) und die Addierbarkeit der Prozentwerte bei nachgelagerter Solarthermie
> (5-1f, Anzeige-Aufgabe von Paket 7).

**Der Befund.** `Tab_ErgebnisSolarthermie.Waermebedarfsdeckung` folgt seit dieser
Änderung der **Direktdeckung**. Lädt ein Kollektorfeld ausschließlich einen Puffer
(Hauptsenke `PufferBrauchwasser` oder `PufferHeizung`), ist die Direktdeckung **null** —
die Anlage meldet 0 % Deckung, obwohl sie im Szenario S1b 11,128 MWh in die Speicher
gefahren hat. Der Ertrag steht vollständig in `Waermeproduktion` und getrennt in
`Speicherladung_gesamt`, aber die Kennzahl, die Bericht und Wirtschaftlichkeit lesen,
zeigt ihn nicht.

**Warum der Default trotzdem so umgesetzt ist.** Die Alternative — die Speicherladung der
Deckung zuzuschlagen — ist eine Doppelzählung, sobald zwei Erzeuger denselben Puffer
laden (Szenario S3): Die Entladung des Speichers ist keinem Erzeuger zuzuordnen, und
beide Anteile zusammen könnten den Gesamtbedarf übersteigen. Die harte Anforderung
„Restbedarf ≥ 0 und Deckung ≤ 100 % **ohne Kappung**" wäre nicht mehr konstruktiv
erfüllt, sondern nur noch zufällig.

**Die Alternativen, wenn die Anzeige das ändern soll:**

| Variante | Regel | Bewertung |
|---|---|---|
| **A (umgesetzt)** | Deckung = Direktdeckung / Gesamtbedarf | doppelzählungsfrei, konstruktiv ≤ 100 %, unterschätzt eine reine Ladeanlage auf 0 % |
| B | Deckung = min(Gesamtproduktion, Gesamtbedarf) / Gesamtbedarf | zeigt den Ertrag, aber die Anteile mehrerer Erzeuger summieren sich über 100 % |
| C | Speicherentladung anteilig auf die Lader aufteilen (nach Ladeanteil je Speicher und Jahr) | fachlich am saubersten, verlangt eine neue Größe je Speicher und Erzeuger und eine Festlegung, wie Speicherverluste zugeordnet werden |
| D | Eigene Kennzahl `Speicherladung` in `Tab_ErgebnisSolarthermie` ausweisen und die Deckung wie in A lassen | kleinster Eingriff; die Größe existiert im Rechenkern bereits (`Speicherladung_gesamt`), es fehlt nur die Spalte und die Anzeige |

**Empfehlung: D jetzt, C mit Paket 7**, wenn die Anzeigen ohnehin auf n Speicher
umgestellt werden (Konzept 13.3).

> ### NACHGETRAGEN — der Stand nach der Review-Nacharbeit (N1/N2)
>
> Die Review hat die Frage weitergedreht und dabei zwei Dinge gezeigt, die den Default
> nicht mehr tragen:
>
> 1. **Die Ungleichbehandlung Solar/Kessel war ein Fehler, kein Default.** Der Kessel
>    hatte die Mitkorrektur gar nicht — er meldete `S_Waerme_spk` (inklusive
>    Speicherladung) als Deckung und lieferte einen NEGATIVEN Restwärmebedarf
>    (Befund N1). Beide Erzeuger folgen jetzt derselben Regel.
> 2. **Variante A ist als Deckungsgrad nicht haltbar**, sobald zwei Erzeuger in einer
>    Speicherstufe rechnen: Die WP-Formel meldete die Lieferung der ganzen Stufe als
>    ihren Eigenanteil, und der zweite Erzeuger seine noch einmal dazu (Befund N2).
>
> **Umgesetzt ist deshalb eine minimale Fassung der Variante C** — die Interimsregel
> „Vermischung im Speicher": Jede Ladung wird ihrem Erzeuger gutgeschrieben, jede
> bedarfsdeckende Entladung anteilig am aktuellen Speicherinhalt aufgeteilt, die
> Bereitschaftsverluste tragen alle Anteile proportional. Damit gilt
>
> ```
> Deckungsgrad = (Direktdeckung + zugerechnete Speicherentladung [+ Heizstab bei der WP])
>                / Bezugsbedarf
> ```
>
> und die Summe der ausgewiesenen Deckungen ist **exakt** die tatsächliche Deckung
> (gemessen: Abweichung ≤ 9·10⁻⁵ Prozentpunkte, Kapitel 13.11). Ein Kollektorfeld, das
> ausschließlich einen Puffer lädt, meldet damit nicht mehr 0 %, sondern im Szenario S1b
> **21,5 %** — die 0-%-Anzeige, die dieser Punkt beklagte, ist verschwunden.
>
> **Was weiterhin zu entscheiden ist:**
>
> | # | Frage | Umgesetzte Interimsantwort |
> |---|---|---|
> | 5-1a | Momentanmischung oder Jahres-Ladeanteil je Speicher? | **Momentanmischung** — sie ist zeitlich richtig (Wärme vom Januar deckt keinen Julibedarf) und braucht keine zweite Buchführung |
> | 5-1b | Wer trägt die Speicherverluste? | **alle Lader proportional** zu ihrem Anteil am Inhalt |
> | 5-1c | Zurechnung je Erzeuger*art* oder je *Anlage*? | **je Erzeugerart** — die Ergebnistabellen (`Tab_Ergebnis*`) sind ohnehin je Art geführt. Eine Zurechnung je Anlage bräuchte neue Spalten |
> | 5-1d | Soll `Speicherladung` als **eigene Spalte** ausgewiesen werden (Variante D)? | **nicht umgesetzt** — sie verlangt eine Schemaänderung an `Tab_ErgebnisSolarthermie`/`…Heizkessel`. Die Größe steht im Rechenkern bereit (`Speicherladung_gesamt` in beiden Modulen) |
> | 5-1e | Soll `Waermeproduktion` des Kessels die Speicherladung enthalten? | **ja, unverändert** — sie ist die Bezugsgröße von Brennstoffverbrauch und Jahresnutzungsgrad. Würde sie um die Ladung gekürzt, widerspräche sie `Gasverbrauch` und `Kessel_Jahresnutzungsgrad_Spk` |
> | 5-1f | Bezugsbedarf der Solar-Deckung ist ihr **Stufeneingang**, bei WP und Kessel der **Projektbedarf** | unverändert übernommen. Steht die Solarthermie nicht an erster Kaskadenposition, sind die Prozentwerte damit nicht direkt addierbar — Kandidat für Paket 7 |
>
> **Alle sechs Interimsantworten sind am 15.08.2026 bestätigt worden** und damit die
> gültigen Regeln. 5-1d und 5-1f bleiben als Vormerkungen für Paket 7 bestehen
> (Ergebnisspalte bzw. Anzeige), nicht als offene Entscheidungen.

### 5-2 — Ein nachgelagerter Erzeuger nimmt dem vorgelagerten Speicher den Durchsatz

**Der Befund.** Das Durchsatzbudget des Bilanzraums (`absehbar`, Nutzerentscheidung zu
4b-1) wird **nach Phase B** festgehalten — also nachdem alle Erzeuger der Speicherstufe
den Momentanbedarf gedeckt haben. Das ist die Reihenfolge-Invariante aus Konzept 6.3
wortgetreu. Solange nur die Wärmepumpe in der Stufe steht (die gesamte Referenzmenge),
hat das keine Wirkung.

Sobald aber ein **Heizkessel mit Puffer-Senke** hinter einer Wärmepumpe mit Puffer-Senke
in derselben Stufe steht, deckt der Kessel in Phase B Bedarf, den der Speicher der
Wärmepumpe im selben Zeitschritt hätte durchreichen können. Das Durchsatzbudget schrumpft
entsprechend, und die Wärmepumpe wird wieder auf ihren Speicherinhalt gedrosselt — genau
der Effekt, den Befund 4b-1 beseitigt hat, nur ausgelöst von der Kaskade statt von der
Kapazität.

**Warum der konzeptkonforme Default umgesetzt ist.** Die naheliegende Gegenmaßnahme —
das Budget schon nach Phase A festzuhalten — bricht die Energieerhaltung: Der Speicher
nähme Wärme auf, die anschließend niemand mehr anfordert, bliebe über `Q_max` stehen und
trüge den Überstand in die nächste Stunde. Die saubere Lösung verlangt, dass ein
speicherbedienter Erzeuger seinen Durchsatz an **seiner** Kaskadenposition abgibt, bevor
der nachgelagerte Erzeuger den Bedarf sieht — also eine Verzahnung der Phasen B und C
nach Kaskadenposition. Das ist ein Eingriff in die Phasenstruktur A–G und damit eine
Konzeptfrage, keine Implementierungsfrage.

**Zu entscheiden vor der Freigabe des Flags für Projekte, in denen ein Kessel MIT
Puffer-Senke hinter einer Wärmepumpe MIT Puffer-Senke steht.** Alle anderen
Konfigurationen sind unberührt.

> ### PRÄZISIERT durch die Nacharbeit (N11)
>
> **Die kleinste auslösende Konfiguration** ist nicht „Kessel mit Puffer-Senke", sondern
> genau: **Kessel mit Hauptsenke Heizkreis UND Zweitsenke Puffer**, hinter einer
> Wärmepumpe mit Puffer-Senke. Mit einer Puffer-**Haupt**senke am Kessel tritt 5-2
> **nicht** auf — dann deckt der Kessel in Phase B nichts (er lädt ausschließlich), und
> das Durchsatzbudget, das nach Phase B festgehalten wird, schrumpft nicht.
>
> **Quantifiziert** am präparierten 1023 (`DB_K_ZWEIT`, Kessel 11205 mit Zweitsenke auf
> Puffer 1018023): Die Ladung der Wärmepumpe fällt von 109.993,2 kWh (ohne Kesselsenke)
> auf 102.381,6 kWh, also **−7,6 MWh**; ihre bedarfsdeckende Entladung entsprechend von
> 109.638,4 auf 101.964,2 kWh. Der Kessel deckt dafür 114,78 statt 66,89 MWh direkt.
> Energetisch ist nichts verloren — es ist eine Verschiebung zwischen den Erzeugern.
>
> **„Tritt in keiner heutigen Konfiguration auf"** ist an der migrierten Datenbank
> bestätigt: Kein Projekt trägt eine Puffer-Senke an einem Heizkessel (Abfrage über
> `Tab_Energieanlagen.WS_ID_Puffer`/`WS_ID_Puffer2` mit `ID_Type = 10`).
>
> Der Punkt bleibt eine **Konzeptfrage** (Verzahnung der Phasen B und C nach
> Kaskadenposition) und ist mit der Nacharbeit ausdrücklich **nicht** entschieden.

---

## 11. Offene Punkte

| # | Punkt | Bewertung |
|---|---|---|
| 1 | **Stufeneingang je Erzeugerstufe** (Abgrenzung 3/4): Mit mehreren Stufen in der Speicherstufe zeigt `wp.Waermebedarf_stuendlich` den Eintritt in die Stufe, nicht die Kaskadenposition der Wärmepumpe | betrifft nur Projekte mit Solar- oder Kesselsenke VOR der Wärmepumpe; in der Referenzmenge keines. Gehört zu Paket 7 (Ergebnis + Anzeigen) |
| 2 | **Lade-/Entladeleistung je Speicher [kW]** | unverändert offen aus Paket 4 (N.15, Punkt 1); Datenmodell, Migration, Dialog fehlen |
| 3 | **Zwei Puffer im selben Kanal** (4b-4) | implementiert, weiterhin nur mit einem Puffer je Kanal gemessen. Abnahmefall Paket 10 |
| 4 | **Isolierter Nachweis der PV-Obergrenze** (4b-3, zweiter Teil) | Die Wirkung der Ladepriorität bei zwei konkurrierenden Ladern ist jetzt am Lauf gezeigt (F.6, S3). Offen bleibt die Trennung von Reihenfolge- und Obergrenzen-Anteil bei gleichzeitigem PV-Überschuss |
| 5 | **`Simulation_BHKW_Ctrl` leert `bhkw_list`, aber nicht `bhkw_list_Namen`** (4b-5) | unverändert offen — Kandidat für B0 oder Paket 6 |
| 6 | **Der Warmwasserkanal braucht einen Brauchwasserpuffer**, sonst deckt ihn Heizstab bzw. Folgeerzeuger | unverändert; die Zweitsenke ist der vorgesehene Weg (F.6, S1b zeigt ihn) |

---

## 12. Reproduktion

```powershell
$msb = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
& $msb C:\Waermeplan\WP_Plan\WP-Plan.sln                       -p:Configuration=Debug -p:Platform=x86
& $msb C:\Waermeplan\WP_Plan\Referenzlauf\Referenzlauf.csproj  -p:Configuration=Debug -p:Platform=x86

$exe   = "C:\Waermeplan\WP_Plan\Referenzlauf\bin\x86\Debug\net8.0-windows\Referenzlauf.exe"
$probe = "<Scratchpad>\Probe5\bin\x86\Debug\net8.0-windows\Probe5.exe"

# 1. Eigene, vollstaendig migrierte Kopie ausserhalb des Repos (produktive DB nur LESEN)
& $exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb C:\Waermeplan\Paket5_Test\DB_Basis

# 2. Regression mit Flag AUS - Pflicht
foreach ($id in 1007,1008,1010,1011,1017,1018,1021,1023,1024) {
    & $exe projekt $id "C:\Waermeplan\Paket5_Test\Lauf_Aus\Projekt_$id" C:\Waermeplan\Paket5_Test\DB_Basis
}
& $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-14_B1-Fixes C:\Waermeplan\Paket5_Test\Lauf_Aus
& $exe pruefen   C:\Waermeplan\Paket5_Test\Lauf_Aus
# zusaetzlich: rekursiver Byte-Vergleich beider Ordner -> keine abweichende CSV

# 3. Flag AN: DB_Flag = Kopie von DB_Basis mit Kaskade_Zweikanalig = True (alle neun),
#    gesetzt per 32-bit-PowerShell + ACE (UPDATE Tab_Einstellungen).
foreach ($id in 1007,1008,1010,1011,1017,1018,1021,1023,1024) {
    & $exe projekt $id "C:\Waermeplan\Paket5_Test\Lauf_An\Projekt_$id" C:\Waermeplan\Paket5_Test\DB_Flag
}
# Gegenprobe gegen den Paket-4-Stand: dieselben Laeufe mit gestashten Aenderungen bauen
# (git stash push -- <die sechs Dateien>, Kaskadenschleife.cs beiseite) -> Lauf_An_P4
& $exe vergleich C:\Waermeplan\Paket5_Test\Lauf_An_P4 C:\Waermeplan\Paket5_Test\Lauf_An

# 4. Bilanzen, Abschluesse, Solar- und Kesselkennzahlen (rechnet, speichert NICHT)
& $probe C:\Waermeplan\Paket5_Test\DB_Flag  1007,1011,1017,1018,1023,1024
& $probe C:\Waermeplan\Paket5_Test\DB_Basis 1017,1023        # einkanalige Gegenprobe

# 5. Praeparierte Szenarien (je eine eigene Kopie, per SQL gesetzt)
& $probe C:\Waermeplan\Paket5_Test\DB_S1a 1007   # Solar 400 m2, kein Puffer
& $probe C:\Waermeplan\Paket5_Test\DB_S1c 1007   # + Zweitsenke PufferHeizung
& $probe C:\Waermeplan\Paket5_Test\DB_S1d 1007   # Puffer 20.000 l
& $probe C:\Waermeplan\Paket5_Test\DB_S1b 1007   # Haupt Brauchwasser + Zweit Heizung
& $probe C:\Waermeplan\Paket5_Test\DB_S2  1018   # Kessel laedt Puffer, KEINE Waermepumpe
& $probe C:\Waermeplan\Paket5_Test\DB_S3  1007   # Solar (10) vor WP (20) am selben Puffer
& $probe C:\Waermeplan\Paket5_Test\DB_S3b 1007   # Solar manuell auf 30 -> hinter der WP
```

**Die produktive `Kenndaten.accdb` wurde ausschließlich gelesen.** Alle Läufe dieses
Pakets liefen unter `C:\Waermeplan\Paket5_Test\` (außerhalb des Repos); vor jedem Zugriff
wurde geprüft, dass keine `Kenndaten.laccdb` neben der Quelle liegt.

---

# 13. Nacharbeit zur Review (15.08.2026) — Befunde N1 bis N11

Zwei unabhängige adversariale Reviews haben die Umsetzung aus den Kapiteln 1–12 geprüft
und elf Befunde erhoben. Dieses Kapitel ist das **führende Kapitel für den heutigen
Stand**: Wo es einer Aussage aus 1–12 widerspricht, gilt es.

| # | Schwere | Kurzfassung | Kapitel |
|---|---|---|---|
| **N1** | KRITISCH | Kessel-Ergebnis ohne Direktdeckungs-Korrektur — negativer Restwärmebedarf, Deckungssumme > 100 % | 13.1 |
| **N2** | KRITISCH | Deckungsgrad-Doppelzählung bei ≥ 2 Mitgliedern der Speicherstufe | 13.2 |
| **N3** | KRITISCH | Kessel-`Strombedarf` genullt (Array-Aliasing) bzw. falscher Bezugspunkt | 13.3 |
| **N4** | ERNST | Stiller Positionswechsel einer Nicht-Mitglied-Stufe zwischen zwei Mitgliedern | 13.4 |
| **N5** | ERNST | Defekte Puffer-Senke ⇒ stiller Totalausfall des Erzeugers | 13.5 |
| **N6** | ERNST | Physik-Kopien statt Extraktion (Kessel-Einlesen, Solar-Potenzial) | 13.6 |
| **N7** | GERING | `Ladeauftrag.BMTyp` über den falschen Modulindex aufgelöst | 13.7 |
| **N8** | GERING | `_anzahlZweikanalig` wird nicht in `Init()` zurückgesetzt | 13.7 |
| **N9** | GERING | Neue Engine-Lesewege über den fehlerschluckenden `RecordSet` | 13.7 |
| **N10** | GERING | Zwei neue `MessageBox.Show` im dialogfreien Engine-Pfad | 13.7 |
| **N11** | DOKU | Protokoll berichtigen und ergänzen | 13.8, 13.9 und die Einschübe in 2, 4, 7, 8, 9, 10 |

**Kein Befund ist offengeblieben.** Was bewusst NICHT entschieden wurde, steht in 13.12.

---

## 13.1 N1 — der Heizkessel bekommt die Mitkorrektur, die er nie hatte

**Der Befund.** `SimulationRunner` füllte `Tab_ErgebnisHeizkessel` aus `S_Waerme_spk` —
der gesamten Nutzwärme des Kessels, seit Paket 5 also **inklusive der Speicherladung**.
Konzept 6.4 verlangt genau dafür eine Mitkorrektur; sie war für die Solarthermie
umgesetzt (Kapitel 4) und für den Kessel schlicht vergessen worden. Die Solar-Begründung
gilt wortgleich für ihn.

**Der Fix** (`SimulationSPK.cs`, `SimulationRunner.cs`):

| Datei:Stelle | Änderung |
|---|---|
| `SimulationSPK.cs:426-448` | neue Größen `Speicherladung_stuendlich`, `Speicherladung_gesamt`, `Speicherentladung_Anteil`, `Fehlertext` |
| `SimulationSPK.cs:634-635` | eine Buchungszeile in `Zweikanalig_Laden` — die geladene Menge wird zusätzlich getrennt geführt |
| `SimulationSPK.cs:766-780` | Reset in `Init()` (nicht nur in `Vorbereiten_Zweikanalig`) |
| `SimulationRunner.cs:342-361` | `Restwaermebedarf` und `Waermebedarfsdeckung` aus der Direktdeckung `S_Waerme_spk − Speicherladung_gesamt/1000`, geklemmt auf ≥ 0 bzw. ≤ 100 % |

`Waermeproduktion` bleibt bewusst die **gesamte Nutzwärme**: Sie ist die Bezugsgröße von
`Gasverbrauch` und `Kessel_Jahresnutzungsgrad_Spk`. Würde sie um die Speicherladung
gekürzt, widerspräche die Ergebniszeile ihrer eigenen Brennstoffbilanz. Das ist zugleich
die Behandlung der Solarthermie (dort steht der volle Ertrag in `Waermeproduktion`), die
Ungleichbehandlung ist damit aufgehoben.

**Messung** (präpariertes 1018, Kessel 10369 mit Hauptsenke `PufferHeizung` auf Puffer
1018007, Flag AN — `P5R\DB_S2`):

| Größe | vorher | nachher |
|---|---|---|
| `Restwaermebedarf` | **−12,992223 MWh** | **22,053973 MWh** (≥ 0) |
| `Waermebedarfsdeckung` | 18,926907 % | 18,528635 % |
| Summe aller Erzeugerdeckungen | **100,391276 %** | 99,993004 % |
| tatsächliche Projektdeckung | 99,991626 % | 99,991626 % |
| `Waermeproduktion` | 35,046196 MWh | 35,046196 MWh (unverändert) |

Der Wert −12,99 MWh deckt sich auf die Stelle mit der Messung der Review.

---

## 13.2 N2 — Eigenanteil statt Stufendifferenz: keine kWh in zwei Deckungen

**Der Befund.** `Waermebedarf_stuendlich` der Wärmepumpe steht auf dem Eintritt in die
**gesamte** Speicherstufe, `waermerestbedarf_stuendlich` auf dem Rest **nach** ihr. Der
Eigenanteil aus B0-7b — die Differenz beider — enthält damit die Lieferung aller anderen
Stufenmitglieder, und die melden ihre Deckung zusätzlich selbst. Das landet als Balken im
100-%-Diagramm (`BausteineVergleich.cs:192-195`, `ProjektvergleichBericht.cs:260-261`).

**Die Lösung: ein echter Eigenanteil je Erzeuger.**

```
Eigenanteil = Direktdeckung (Phase B)
            + zugerechnete bedarfsdeckende Speicherentladung (Phasen A/E)
            + Heizstab            (nur Wärmepumpe — er gehört zu ihr)
```

Die ersten beiden Summanden kennt die Kaskadenschleife: Sie sieht jede Lieferung je
Anlage und Phase.

**Die Zurechnungsregel für die Entladung — „Vermischung im Speicher"** (bei der Nacharbeit
als Interimsregel eingeführt, am 15.08.2026 mit 5-1 bestätigt)**.** Der Speicherinhalt
wird als Mischung geführt: Jede Ladung schreibt ihre Menge dem ladenden Erzeuger gut,
jede bedarfsdeckende Entladung wird nach den Anteilen am **aktuellen** Inhalt
aufgeteilt, und die Bereitschaftsverluste tragen alle Anteile proportional (Angleichung
an den Füllstand nach Phase G).

Warum diese und keine andere:

- Sie rechnet **jede kWh genau einem Erzeuger zu**. Daraus folgt konstruktiv, dass die
  Summe der Eigenanteile die Lieferung der Stufe nie überschreitet.
- Sie braucht **keine neue Konfigurationsgröße** und keine zweite Buchführung.
- Sie ist **zeitlich richtig**: Wärme, die im Januar eingespeichert wurde, deckt keinen
  Julibedarf. Eine Zurechnung nach dem JAHRES-Ladeanteil (die naheliegende Alternative)
  würde genau das tun.
- Bei **genau einem Lader je Speicher** — dem Fall aller neun Referenzprojekte — rechnet
  sie die gesamte Entladung wie bisher der Wärmepumpe zu. Deshalb ändert sich in der
  Referenzmenge **kein einziger Wert** (13.10).

Sie ist die minimale Umsetzung der Variante **C** aus der Nutzerentscheidung 5-1 — am
15.08.2026 **bestätigt** und damit keine Interimsregel mehr (Kapitel 10). Was als
Vormerkung für Paket 7 bleibt, steht dort und in 13.12.

**Der Fix:**

| Datei:Stelle | Änderung |
|---|---|
| `Kaskadenschleife.cs:70-201` | Herkunftsrechnung je Speicher: `Anteil_Laden`, `Anteil_Entladen`, `Anteil_Angleichen`, Zähler je Erzeugerart |
| `Kaskadenschleife.cs:374-392` | die Ladephase bucht die tatsächlich geladene Menge auf den Erzeuger |
| `Kaskadenschleife.cs:200, 310, 458, 490` | Reset je Lauf; jede bedarfsdeckende Entladung (Durchsatz und regulär) wird aufgeteilt; Angleichung nach `StundeAbschliessen` |
| `Kaskadenschleife.cs:336-342` | Ergebnis an die Module: `WP/Solar/Kessel.Speicherentladung_Anteil` |
| `SimulationWaermepumpe.cs:820-843` | neue Größen `Direktdeckung_gesamt`, `Speicherentladung_Anteil`; Reset in `Init()` |
| `SimulationWaermepumpe.cs:1039, 1058` | zwei Zeilen in der Bedarfsphase, die die Direktdeckung mitzählen |
| `SimulationSolarthermie.cs:66-75` | `Speicherentladung_Anteil` (Direktdeckung wird dort schon geführt) |
| `SimulationRunner.cs:189-220` | WP-Deckung aus dem Eigenanteil — **nur im zweikanaligen Weg**; der Altpfad behält die Formel aus B0-7b unverändert |
| `SimulationRunner.cs:342-361 und :421-443` | Kessel- und Solar-Deckung aus dem Eigenanteil (im Altpfad bitgleich der bisherige Ausdruck, weil beide Zusatzgrößen exakt 0 sind) |

**Messung** — Summe der ausgewiesenen Deckungen gegen die tatsächliche Projektdeckung:

| Szenario | Summe vorher | Summe nachher | tatsächlich |
|---|---|---|---|
| 1023, Kessel mit Puffer-**Haupt**senke (`DB_K_HAUPT`) | **85,708835 %** | **67,060910 %** | 67,060998 % |
| 1023, Kessel mit **Zweit**senke (`DB_K_ZWEIT`) | **97,343958 %** | **67,892356 %** | 67,892352 % |
| 1011, Kessel-Zweitsenke + Solar dazwischen (`DB_ORD_MIT`) | **14,930406 %** | **10,156954 %** | 10,156807 % |
| 1018, Kessel mit Puffer-Hauptsenke (`DB_S2`) | **100,391276 %** | 99,993004 % | 99,991626 % |

Die verbleibende Abweichung liegt bei **≤ 1,5·10⁻⁴ Prozentpunkten** — Rundung. Die beiden
Werte 85,71 / 67,06 und 14,93 / 10,16 decken sich auf die Stelle mit den Messungen der
Review.

Wie die Zurechnung bei ZWEI Ladern desselben Speichers arbeitet, zeigt `DB_K_HAUPT`:
Puffer 1018023 nimmt 182.543,8 kWh auf und gibt 182.176,6 kWh bedarfsdeckend ab; davon
werden **109.503,9 kWh der Wärmepumpe** und **72.672,7 kWh dem Kessel** zugerechnet — in
der Summe die volle Entladung, keine kWh doppelt.

---

## 13.3 N3 — der Kessel-Strombedarf: kein Aliasing, richtiger Bezugspunkt

**Der Befund — zwei Fehler übereinander.**

1. `SimulationControl` gab dem WP-Modul das Aufrufer-Array als **Ausgabearray**
   (`WP_Strombedarf_stuendlich = Strombedarf`). `Vorbereiten_Zweikanalig →
   ModuleAufbauen → Init` **nullt** dieses Array — und erst danach klonte der Kessel es.
   Ergebnis: `Tab_ErgebnisHeizkessel.Strombedarf` und `.Reststrombedarf` **exakt 0**.
2. Selbst ohne das Nullen wäre der Bezugspunkt falsch gewesen: Im Altpfad steht der
   Kessel hinter der Wärmepumpe und sieht den Strombedarf **nach** deren Verbrauch.

**Der Fix:**

| Datei:Stelle | Änderung |
|---|---|
| `SimulationControl.cs:746-753` | eigene Kopie `stromStufeneingang` **vor** dem Modulaufbau — das Aliasing ist damit weg |
| `SimulationControl.cs:788-791` | der Kessel bekommt seinen Vektor aus dieser Kopie |
| `SimulationControl.cs:470-487` | nach dem Lauf: steht der Kessel in der Kaskade hinter der Wärmepumpe, wird sein Strombedarf über **dieselbe Vektorkette wie im Altpfad** nachgezogen (`Viertelstunden_zu_Stundenwerte_Mittelwert(Rest_Strombedarf_viertelstuendlich)`, nachdem WP-Strom und Heizstab eingerechnet sind) |
| `SimulationControl.cs:565-587` | neue Hilfsmethode `KesselHinterWaermepumpe()` |

**Messung.** Der aussagekräftige Fall ist ein Projekt mit **nennenswertem Grundbedarf an
Strom** — 1011 hat 5.461,96 MWh, die Wärmepumpe verbraucht 91,10 MWh:

| Lauf | Kessel in der Schleife? | `Heizkessel.Strombedarf` |
|---|---|---|
| vorher (`DB_ORD_MIT`) | ja | **0 MWh** |
| nachher (`DB_ORD_MIT`) | ja | **5.553,0545 MWh** |
| Gegenprobe: Vektorstufe (`DB_ORD_OHNE`) | nein | 5.553,0590 MWh |
| Vor-WP-Stand (Stufeneingang) | — | 5.461,96 MWh |

Der neue Wert ist also weder 0 noch der Vor-WP-Stand, sondern deckt sich bis auf
4,5 kWh mit dem Weg, den der Altpfad geht. Am präparierten 1023 (`DB_K_HAUPT`) sind es
124,554641 MWh = 45,375 (WP) + 79,179690 (Heizstab) bei einem Grundbedarf von 0.

**Nicht** geändert wurde der Umstand, dass der Wert in Projekt 1018 **negativ** ist
(−73,731648 MWh): Dort produziert das BHKW mehr Strom, als das Projekt braucht, und der
Altpfad meldet denselben Wert (Referenzbasis `Projekt_1018/aggregate.csv`:
`Heizkessel.Strombedarf;-73.73`). Das ist ein Bestandsverhalten und kein Paket-5-Effekt.

---

## 13.4 N4 — kein stiller Positionswechsel mehr

**Der Befund.** Die Speicherstufe rechnet an der Kaskadenposition ihres **ersten**
Mitglieds. Eine Solar- oder Kesselstufe ohne Puffer-Senke, die zwischen zwei Mitgliedern
steht, rutschte damit hinter die **gesamte** Stufe — inklusive Nachentladung und
Heizstab. Gemessen an einem präparierten 1011 fiel die Solarproduktion von 0,64 auf
0,28 MWh, ohne jeden Hinweis; die Warnung gab es nur im BHKW-Zweig.

**Der Fix ist strukturell, nicht hinweisend** (Empfehlung der Review, geprüft und
umgesetzt): `SimulationControl.ZwischenstufenAufnehmen()`
(`SimulationControl.cs:611-681`) nimmt Solarthermie und Heizkessel **als Mitglieder
auf, sobald sie zwischen dem ersten und dem letzten Mitglied stehen**. Sie nehmen dann
ohne Puffer-Senke als reine Heizkreis-Lieferanten an Phase B teil — an genau ihrer
Kaskadenposition. Ein Durchlauf genügt: Ein neu aufgenommenes Mitglied liegt selbst
innerhalb des Intervalls, das Intervall wächst also nicht.

**Warum nicht „immer Mitglied, sobald die Schleife läuft"** (die weitergehende Variante
aus der Review): Sie ist machbar, aber unverhältnismäßig, und die Wirkung ist an den
Referenzszenarien nachgemessen:

- **1007/1011** (Solarthermie vor der Wärmepumpe): Die Speicherstufe würde an die
  Position der Solarthermie vorrücken. `wp.Waermebedarf_stuendlich` — der Stufeneingang
  und damit `Tab_ErgebnisWaermepumpe.Waermebedarf` samt Ganglinie — stiege um die
  Solardeckung.
- **1023/1024** (Kessel hinter der Wärmepumpe): Der Kessel deckte dann in Phase B, also
  **vor** Nachentladung und Heizstab. Das ist konzeptkonform (13.9), verschiebt aber in
  1023 rund 48 MWh Nutzwärme vom Heizstab zum Kessel — in Projekten, deren Konfiguration
  sich gar nicht geändert hat.

Beides sind Ergebnisänderungen ohne fachlichen Anlass. Die umgesetzte Fassung greift
**genau dort, wo der Fehler auftritt**, und lässt alles andere in Ruhe: Stufen VOR dem
ersten und NACH dem letzten Mitglied stehen ohnehin an der richtigen Stelle (die Schleife
als Ganzes liegt zwischen ihnen).

**Damit stimmt die Protokollaussage aus Kapitel 2 wieder**: Der Positionswechsel betrifft
jetzt tatsächlich nur noch das BHKW, und dieser eine Fall wird weiterhin protokolliert
(`SimulationControl.cs:530-534`).

**Messung** (präpariertes 1011, Kaskade Wärmepumpe → Solarthermie → Heizkessel; der
Kessel 11218 bekommt eine Zweitsenke auf Puffer 1011007 und wird damit Mitglied —
`P5R\DB_ORD_MIT`):

| Größe | vorher | nachher | Vergleichsfall ohne Kesselsenke (`DB_ORD_OHNE`) |
|---|---|---|---|
| Solarproduktion | **0,28 MWh** | **0,445704 MWh** | 0,643577 MWh |
| Position der Solarstufe | nach der ganzen Stufe | Phase B, Position 2 | Vektorstufe, Position 2 |
| Konsolenmeldung | — | „Die Solarthermie steht zwischen zwei Erzeugern der Speicherstufe …" | — |

Dass 0,446 nicht 0,644 erreicht, ist **kein** Rest des Befunds, sondern die Wirkung des
Speichers: Der vom Kessel geladene Puffer entlädt in **Phase A**, also vor der gesamten
Phase B, und senkt den Stufeneingang der Solarthermie von 4.830,0 auf 4.786,7 MWh. Das
ist die Phasenstruktur A–G aus Konzept 6.3 und gehört zur offenen Konzeptfrage 5-2.

---

## 13.5 N5 — eine defekte Puffer-Senke legt keinen Erzeuger mehr still

**Der Befund.** Eine Anlage mit Puffer-**Haupt**senke deckt in Phase B nichts — sie lädt
ausschließlich. Entsteht aus ihrer Senkenreferenz kein Ladeauftrag, produziert sie das
ganze Jahr **nichts**, und zwar ohne Hinweis. Zwei Wege dorthin, beide an einem
präparierten 1018 gemessen (Kesselproduktion 34,27 → **0 MWh**):

- **(a)** `WS_Ziel = 'PufferHeizung'`, aber `WS_ID_Puffer` ist NULL. Die Anlage wird
  mangels Puffer-ID nicht als ladend erkannt (kein Stufenmitglied), aber `Stunde_Bedarf`
  überspringt sie, weil ihre Hauptsenke nicht der Heizkreis ist.
- **(b)** `WS_ID_Puffer` zeigt auf den Puffer eines **fremden** Projekts. Dann ist die
  Anlage Stufenmitglied, die Speicher-Registry lehnt den Puffer ab, und es entsteht nie
  ein Ladeauftrag.

**Der Fix — zwei Schichten:**

| Datei:Stelle | Änderung |
|---|---|
| `WaermesenkeClass.cs:246-264` | `Normalisieren`: Ein Puffer-**Ziel** ohne Puffer-**Referenz** wird zu `ZIEL_HEIZKREIS`; dasselbe für die Zweitsenke. Ein Ziel ohne Ziel ist kein Ziel — damit ist Weg (a) an der Datenschicht geschlossen, in der Engine **und** in den Dialogen |
| `WaermesenkeClass.cs:451-466` | `SenkenLaden` protokolliert jede so korrigierte Anlage |
| `SimulationControl.cs:812-908` | nach `KontextAufbauen()`: `PufferSenkenOhneAuftragZurueckfallen` prüft **jede** Anlage jeder rechnenden Erzeugerart auf einen Ladeauftrag und setzt sie sonst auf `Senke.Heizkreis` zurück — mit Konsolenmeldung. Das schließt Weg (b) **und dieselbe Lücke bei der Wärmepumpe**, die seit Paket 4 bestand |

Die Zuordnungsobjekte sind dieselben Instanzen, mit denen die Module rechnen
(`senkenzuordnungen` ist die eine Quelle) — die Korrektur wirkt deshalb auch für
Solarthermie und Heizkessel, deren Modulaufbau schon gelaufen ist.

**Messung** (1018 mit Flag AN, Kessel 10369):

| Variante | Kessel-Nutzwärme vorher | nachher | Meldung |
|---|---|---|---|
| (a) `WS_ID_Puffer` NULL (`DB_DEF_C1`) | **0 MWh** | **34,272457 MWh** | „Die Anlage 10369 ist auf PufferHeizung gesetzt, hat aber KEINEN Pufferspeicher zugeordnet (WS_ID_Puffer leer). Sie rechnet deshalb auf den HEIZKREIS." |
| (b) Puffer 1008007 aus Projekt 1008 (`DB_DEF_C2`) | **0 MWh** | **34,272457 MWh** | „Die Anlage 10369 … bekommt in diesem Lauf aber KEINEN Ladeauftrag … Die Anlage deckt deshalb den HEIZKREIS; ohne diesen Rückfall würde sie das ganze Jahr nichts produzieren." |

In beiden Fällen rechnet der Kessel damit exakt wie ohne die defekte Konfiguration
(Referenzbasis Flag AUS: `Waermeproduktion 34,27 MWh`, `Restwaermebedarf 0,05 MWh`).

---

## 13.6 N6 — die Physik steht einmal, nicht zweimal

**Der Befund.** Zwei Blöcke waren aus dem Altpfad **kopiert** statt extrahiert:
`SimulationSPK.Vorbereiten_Zweikanalig` trug die Schritte 1/2 aus `Berechnung()`,
`SimulationSolarthermie.Vorbereiten_Zweikanalig` den Potenzialteil. Term für Term
bitgleich — aber ein künftiger Fix am Altpfad hätte im neuen Weg nicht gewirkt, und die
Regressionssuite (Flag aus) hätte das **nie** gemeldet. Zwei Abweichungen waren bereits
entstanden.

**Der Fix:**

| Datei:Stelle | Neue gemeinsame Methode | benutzt von |
|---|---|---|
| `SimulationSPK.cs:159-243` | `Kesseldaten_Einlesen(HeizkesselCtrl, int, bool mitDialog)` | `Berechnung()` und `Vorbereiten_Zweikanalig()` |
| `SimulationSolarthermie.cs:183-224` | `KlimaregionUndGeoLesen()` | beide Wege |
| `SimulationSolarthermie.cs:226-295` | `Kollektorfelder_Lesen()` — Einlesen der Felder **samt stündlichem Potenzial** | beide Wege |
| `SimulationSolarthermie.cs:328-347` | `Bilanzieren(bedarf, potenzial)` — Schritt 3, der Kappungspunkt; `BerechneSolarthermie` ruft sie jetzt auf | Altpfad |

Der bereits erzeugte `HeizkesselCtrl` wird in die Methode **hineingereicht**, damit auch
seine Erzeugungsstelle im Altpfad bleibt, wo sie war.

**Die beiden bereits divergenten Stellen sind bereinigt:**

- **`rows` gegen `Math.Min(rows, 8760)`** (Solarthermie): vereinheitlicht auf die
  abgesicherte Fassung. Das schließt zugleich einen latenten `IndexOutOfRangeException`
  im Altpfad — mehr als 8760 Klimadatenzeilen hätten ihn zum Absturz gebracht. In allen
  neun Referenzprojekten sind es genau 8760 Zeilen, die Änderung ist dort wirkungslos
  (byte-identischer Regressionslauf).
- **`Anzahl == 0`-Frühausstieg** (Kessel): bleibt im Altpfad, wo er steht. Der
  zweikanalige Weg braucht ihn nicht — bei null Kesseln laufen seine Schleifen leer und
  `Stunde_Bedarf` schreibt die Restwärme unverändert durch. Nachgeprüft und gleichwertig.

**HARTE BEDINGUNG erfüllt:** Der Regressionslauf mit Flag AUS ist nach der Extraktion
**byte-identisch** (13.10). Zusätzlich reproduzieren alle sieben präparierten
Solar-/Kesselszenarien aus F.6 ihre Zahlen **auf die letzte gedruckte Stelle** (13.11).

---

## 13.7 N7 bis N10 — die vier kleinen Befunde

| # | Fix | Stelle |
|---|---|---|
| **N7** | `a.BMTyp = BetriebsmodusDerAnlage(e.ID_Anlage)` statt `BetriebsmodusDesModuls(modulindex)`. Seit Paket 5 ist der Modulindex bei Solarthermie und Kessel ein Index in DEREN Modulliste — aufgelöst wurde er gegen `simulation_wp.Betriebsmodi`. Jetzt dieselbe Prioritätsfunktion wie bei den Obergrenzen zwei Zeilen weiter (Konzept 3.5/6.3) | `SimulationControl.cs:1253-1260` |
| **N8** | `_anzahlZweikanalig = 0;` als erste Anweisung von `SimulationSPK.Init()`. Bricht `Vorbereiten_Zweikanalig` mitten in der Kesselschleife ab, stünde sonst der Vorlaufwert neben einer bereits geleerten `_kesselSenke` | `SimulationSPK.cs:855-862` |
| **N9** | `WP_Liste_Laden`, `SPK_Liste_Laden`, `Solar_Liste_Laden` und die Klimaregion-Abfrage der Solarthermie laufen über `StilleDb` (parametrisiert, Fehler auf die Konsole) statt über `RecordSet`. Der schluckt SQL-Fehler still — die Ursache von B1-F1/B1-F2 —, und eine leere Modulliste sähe aus wie „das Projekt hat keine Anlagen dieser Art"; bei der Klimaregion bliebe der Wert des **Vorlaufs** stehen. Die Abfragen sind Wort für Wort dieselben | `SimulationControl.cs:906-985`, `SimulationSolarthermie.cs:183-224` |
| **N10** | Die beiden `MessageBox.Show` aus `Vorbereiten_Zweikanalig` sind weg. „Kessel nicht im Projekt hinterlegt" geht über den **Fehlerkanal** (`SimulationSPK.Fehlertext` → `SimulationControl.Fehlertext` → `SimulationRunner.Simuliere(out fehler)` → kein Speichern eines unvollständigen Ergebnisses); „mehr als 10 Kessel" meldet auf die Konsole und rechnet weiter — dasselbe Verhalten wie der Altpfad, nur ohne Dialog (Konzept 13.4). Der Altpfad zeigt seine Dialoge unverändert | `SimulationSPK.cs:186-193, 484-492`, `SimulationControl.cs:161-168, 795-799, 995-1000`, `SimulationRunner.cs:102-113` |

**Zur Byte-Neutralität von N9:** Umgestellt wurden ausschließlich Methoden, die **nur im
neuen Pfad** aufgerufen werden — mit einer Ausnahme, der Klimaregion-Abfrage der
Solarthermie, die durch die Extraktion (N6) in beiden Wegen liegt. Der byte-identische
Regressionslauf mit Flag AUS ist der Nachweis, dass sie denselben Wert liefert. Die
Altpfad-Methoden `Simulation_WP_Ctrl`, `Simulation_SPK_Ctrl` und
`Simulation_Solarthermie_Ctrl` sind **unangetastet** bei `RecordSet` geblieben.

---

## 13.8 N11 — was am Protokoll berichtigt wurde

| Stelle | Berichtigung |
|---|---|
| Kapitel 2, „Wer in der Schleife rechnet" | Tabelle um die Zwischenpositions-Regel ergänzt |
| Kapitel 2, „Die Kaskadenposition der Speicherstufe" | Die Aussage „betrifft ausschließlich das BHKW" war **falsch**; sie stimmt erst seit N4 und ist als Berichtigung gekennzeichnet |
| Kapitel 4 | Hinweis, dass die Direktdeckungs-Regel für den **Deckungsgrad** durch N2 überholt ist (für den Restbedarf gilt sie weiter) |
| Kapitel 7, F.5 | **Zahlenherkunft** gekennzeichnet: Wegwerf-Probe, nicht aus der Suite reproduzierbar |
| Kapitel 7, F.6 / S1b | Anzeigerundung erklärt: 3.134,224 + 7.993,581 = 11.127,805 auf den gedruckten Stellen; „Abweichung 0" gilt in voller Genauigkeit (−5,5·10⁻¹² kWh) |
| Kapitel 8 | vier neue dokumentierte Ergebnisänderungen (7–10) |
| Kapitel 9, Abgrenzung 2 und 3 | an N4 bzw. N2 angepasst |
| Kapitel 10, 5-1 | um die N1/N2-Regelung erweitert (einheitliche Direktdeckungs-Basis, Interimsregel der Entladungszurechnung, sechs Teilfragen) |
| Kapitel 10, 5-2 | kleinste auslösende Konfiguration präzisiert, an 1023 quantifiziert (−7,6 MWh WP-Ladung), DB-Bestätigung ergänzt |

---

## 13.9 N11 / R1-4(a) — der Kessel deckt in Phase B, also vor dem Heizstab

**Die Ergebnisänderung, die bisher nicht ausgewiesen war.** Rechnet ein Heizkessel als
**Mitglied** der Speicherstufe, deckt er in **Phase B**. Der Heizstab ist Phase F, also
die letzte Instanz (Konzept 6.3). Im Altpfad — und in der zweikanaligen Vektorstufe —
läuft der Kessel dagegen **nach** der ganzen Wärmepumpenstufe und damit **nach** dem
Heizstab.

Das ist **konzeptkonform** und fachlich richtig: Ein Heizstab ist die teuerste Wärme im
System; ihn vor einem Gaskessel laufen zu lassen, wäre eine Fehlreihenfolge. Es ist aber
**ergebnisrelevant** und gehört deshalb ausgewiesen.

**Messung** (1023, Flag AN; links der Kessel als Vektorstufe, rechts als Mitglied mit
Zweitsenke — `DB_Flag` gegen `P5R\DB_K_ZWEIT`):

| Größe | Kessel als Vektorstufe | Kessel als Stufenmitglied | Differenz |
|---|---|---|---|
| Kessel-Nutzwärme | 66,892400 MWh | 114,781642 MWh | **+47,89 MWh** |
| Brennstoff (Gas) | 78,988570 MWh | 132,016382 MWh | **+53,03 MWh** |
| Heizstab (Strom) | 87,918622 MWh | 47,850794 MWh | **−40,07 MWh** |
| Jahresnutzungsgrad | 84,686176 % | 86,944998 % | +2,26 %-Punkte |

Die Größenordnung deckt sich mit der Messung der Review (+53 MWh Gas, −40 MWh
Heizstab-Strom). Betroffen sind **ausschließlich** Projekte mit einer Puffer-Senke am
Kessel; in der Referenzmenge gibt es keines.

---

## 13.10 Verifikation, Teil 1 — die Regressionsläufe

### 13.10.1 Build

```
MSBuild WP-Plan.sln                      -p:Configuration=Debug -p:Platform=x86  ->  0 Fehler
MSBuild Referenzlauf\Referenzlauf.csproj -p:Configuration=Debug -p:Platform=x86  ->  0 Fehler
```

Warnungen: **dieselben sechs Bestandswarnungen** wie vor der Nacharbeit
(`WErzeugerModel.cs` CS0108, `StromverbraucherStammCtrl.cs` CS0108,
`KlimaregionStammCtrl.cs` 2 × CS0109, `MDIMainForm.cs` CS4014 und CS1998) — **keine
neue**.

### 13.10.2 Flag AUS — byte-identisch, auch nach Extraktion und DB-Umstellung

Das ist der harte Nachweis: Die N6-Extraktion hat gemeinsamen Physikcode verschoben, N9
hat einen in beiden Wegen liegenden Datenbankzugriff von ODBC auf OLE DB umgestellt.
Beides ist wirkungslos geblieben.

```
Projekt_1007: PASS (29 Dateien, 324210 Werte)   Projekt_1018: PASS (19 Dateien, 210343 Werte)
Projekt_1008: PASS (21 Dateien, 227847 Werte)   Projekt_1021: PASS (21 Dateien, 227840 Werte)
Projekt_1010: PASS (18 Dateien, 201540 Werte)   Projekt_1023: PASS (25 Dateien, 262917 Werte)
Projekt_1011: PASS (29 Dateien, 324232 Werte)   Projekt_1024: PASS (26 Dateien, 271686 Werte)
Projekt_1017: PASS (20 Dateien, 245378 Werte)

GESAMT: PASS (2295993 Werte innerhalb der Toleranz)
```

**Stärkerer Nachweis:** Ein MD5-Vergleich aller **208** CSV-Dateien gegen
`Referenzlaeufe/2026-08-14_B1-Fixes` meldet **keine einzige abweichende Datei**.
`Referenzlauf.exe pruefen` meldet für alle neun Projekte „plausibel".

### 13.10.3 Flag AN — die Referenzprojekte bleiben Wert für Wert gleich

Verglichen wurde derselbe Datenbestand (`DB_Flag`, alle neun mit gesetztem Flag), einmal
mit dem Stand **vor** der Nacharbeit (`Lauf_An`) und einmal **danach** (`Lauf_An_N2`):

```
alle neun Projekte: PASS (2296004 Werte innerhalb der Toleranz)
```

Und schärfer als die Toleranzprüfung, über einen Schlüssel-für-Schlüssel-Vergleich aller
`aggregate.csv` **plus** MD5 über jede Ganglinie:

> **Abweichende Einträge insgesamt: 0.**

**Das ist die zentrale Aussage dieser Nacharbeit:** Fünf teils tiefgreifende Korrekturen
— eine neue Zurechnungsrechnung im Speicher, ein anderer Deckungsgradbegriff, ein
korrigierter Strombedarf, eine geänderte Mitgliedschaftsregel, zwei Rückfallebenen — und
in den neun Referenzprojekten ändert sich **kein einziger Wert**. Der Grund ist
strukturell und nicht zufällig:

- Kein Referenzprojekt hat **mehr als ein Mitglied** der Speicherstufe (N2, N4 greifen
  nicht).
- Kein Referenzprojekt hat eine **Puffer-Senke an Solarthermie oder Kessel** (N1, N3
  greifen nicht).
- Mit genau einem Lader je Speicher rechnet die Mischungsregel die gesamte Entladung
  wie bisher der Wärmepumpe zu — der neue Deckungsgradbegriff liefert dieselbe Zahl.

`Referenzlauf.exe pruefen` meldet auch für den Flag-AN-Lauf „plausibel".

---

## 13.11 Verifikation, Teil 2 — Bilanzen und präparierte Szenarien

### Energieerhaltung, Speicherbilanz und `StundeAbschliessen` (Flag AN)

| Lauf | Stundenbilanz max | Summe der Beträge | Speicherbilanzfehler | `StundeAbschliessen` |
|---|---|---|---|---|
| 1023 (`DB_Flag`) | 1,53·10⁻⁵ kWh | 0,00784 kWh | −4,6·10⁻⁹ kWh | **8760/8760** |
| 1024 (`DB_Flag`) | 1,53·10⁻⁵ kWh | 0,00729 kWh | 0 | **8760/8760** |
| 1023 (`DB_K_HAUPT`) | 1,53·10⁻⁵ kWh | 0,00887 kWh | −2,8·10⁻⁹ kWh | **8760/8760** |
| 1023 (`DB_K_ZWEIT`) | 1,53·10⁻⁵ kWh | 0,00776 kWh | −4,5·10⁻⁹ kWh | **8760/8760** |
| 1018 (`DB_S2`) | — (kein WP-Bezug) | — | 4,5·10⁻⁹ kWh | **8760/8760** |
| 1011 (`DB_ORD_MIT`) | 1,20·10⁻⁴ kWh | 0,182 kWh | −1,8·10⁻⁹ kWh | **8760/8760** |

Die Stundenbilanz prüft `Eingang − Rest == Produktion(alle Stufen) + Heizstab +
Entladung − Ladung`. Für 1011 liegen die Absolutwerte zwei Größenordnungen höher
(5.105 MWh Jahresbedarf), die relative Genauigkeit ist dieselbe.

### Die sieben Szenarien aus F.6 — unverändert reproduziert

| Szenario | Kennzahl | F.6 (vor der Nacharbeit) | nach der Nacharbeit |
|---|---|---|---|
| S1a | Produktion / verworfen | 7,269 / 209,463 MWh | 7,269129 / 209,463373 |
| S1c | Produktion / verworfen | 10,162 / 206,571 MWh | 10,161504 / 206,570998 |
| S1d | Produktion / verworfen | 27,410 / 189,323 MWh | 27,409974 / 189,322528 |
| S1b | Ladung Brauchwasser / Heizung | 3.134,224 / 7.993,581 kWh | 3.134,223697 / 7.993,580564 |
| S2 | Kessel-Nutzwärme | 35,046 MWh | 35,046196 |
| S3 | Solarproduktion | 9,027 MWh | 9,026872 |
| S3b | Solarproduktion | 7,174 MWh | 7,173935 |

Alle Speicherbilanzfehler bleiben im Bereich 10⁻¹¹ bis 10⁻⁹ kWh, alle Abschlusszähler
bei 8760/8760.

**Neu sichtbar durch N2:** In S1b, S3 und S3b lädt die Solarthermie ausschließlich
Puffer, ihre Direktdeckung ist 0. Vor der Nacharbeit meldete sie deshalb **0 % Deckung**
(genau der Punkt, den die Nutzerentscheidung 5-1 beklagte); jetzt weist sie ihren
Eigenanteil aus:

| Szenario | Deckung vorher | Deckung nachher | zugerechnete Entladung |
|---|---|---|---|
| S1b | 0 % | **21,481366 %** | 10.334,231 kWh |
| S3 | 0 % | **16,933922 %** | 8.336,706 kWh |
| S3b | 0 % | **13,692688 %** | 6.682,871 kWh |

Der Unterschied zwischen S3 (Solarthermie mit Vorgaberang 10) und S3b (manuell auf 30
gesetzt) bleibt der Nachweis der Ladepriorität aus F.6 — er zeigt sich jetzt zusätzlich
im Deckungsgrad.

### Kodierung und Diff

| Datei | BOM | Zeilenenden | U+FFFD |
|---|---|---|---|
| `SimulationSPK.cs`, `SimulationSolarthermie.cs`, `SimulationWaermepumpe.cs`, `SimulationControl.cs`, `SimulationKanaele.cs`, `SimulationRunner.cs`, `Kaskadenschleife.cs` | ja (unverändert) | CRLF (0 Einzel-LF) | 0 |
| `WaermesenkeClass.cs` | **nein** (unverändert) | LF (wie im Bestand) | 0 |

`git diff --check` meldet nichts; ein Diff **ohne** `autocrlf`-Normalisierung zeigt für
`WaermesenkeClass.cs` genau 43 Einfügungen und **0 Löschungen** — die Zeilenenden der
Datei sind also nicht angefasst worden. Keine Designer- und keine `.resx`-Datei berührt;
die sechs gesperrten Dateien sind unverändert. **Nichts committet.**

---

## 13.12 Was die Nacharbeit bewusst NICHT entschieden hat

| # | Punkt | Status |
|---|---|---|
| 1 | **Zurechnungsregel der Speicherentladung** (Momentanmischung, Verlusttragung, je Erzeugerart) | **erledigt** — als Interimsregel umgesetzt und begründet (13.2), am **15.08.2026 mit Nutzerentscheidung 5-1 bestätigt** und damit die gültige Regel (Kapitel 10) |
| 2 | **`Restwaermebedarf` bei Puffer-Hauptsenke** ist der Stufeneingang, weil die Direktdeckung 0 ist (1023/`DB_K_HAUPT`: `Waermebedarf` 382,13 = `Restwaermebedarf` 382,13 bei 72,68 MWh Produktion). Die Größe ist eine **Kaskadenpositions**-Größe und mit dem Deckungsgrad bewusst nicht deckungsgleich | **erledigt** — die vorgeschlagene Alternative `Waermebedarf − Eigenanteil` (mit Klemmung) ist mit **Nutzerentscheidung 6-4** für Solarthermie, Kessel und BHKW umgesetzt (Paket-6-Protokoll 13.2) und mit **6-5** am 15.08.2026 auch für die Wärmepumpe (Paket-6-Protokoll, Kapitel 14). Alle vier Erzeugerarten folgen derselben Regel |
| 3 | **Bezugsbedarf der Solar-Deckung** ist der Stufeneingang, bei WP und Kessel der Projektbedarf | unverändert übernommen; die Prozentwerte sind damit nur addierbar, wenn die Solarthermie an erster Kaskadenposition steht — Kandidat für Paket 7 |
| 4 | **BHKW** meldet weiter seine Produktion als Deckung | Es rechnet einkanalig über `Uebernehmen` und kennt seinen Eigenanteil nicht. Am präparierten 1018 liegt die Deckungssumme dadurch um **+0,0014 Prozentpunkte** über der tatsächlichen Deckung. Paket 6 |
| 5 | **5-2** (nachgelagerter Erzeuger nimmt dem vorgelagerten Speicher den Durchsatz) | unverändert offene Konzeptfrage, jetzt präzisiert und quantifiziert (Kapitel 10) |
| 6 | **Eigene Ergebnisspalte `Speicherladung`** (Variante D aus 5-1) | nicht umgesetzt — Schemaänderung. Die Größe steht im Rechenkern in beiden Modulen bereit. Mit der Bestätigung von 5-1 (5-1d) ist das eine **Vormerkung für Paket 7**, keine offene Entscheidung mehr |
| 7 | **Stufeneingang je Erzeugerstufe** (Abgrenzung 3) | unverändert offen; die Folge für den Deckungsgrad ist mit N2 beseitigt, die Anzeigegröße `Waermebedarf` bleibt der Stufeneingang |
| 8 | **Der Fehlerkanal aus N10 erreicht die Oberfläche noch nicht.** `Form_Simulation_Detail` ruft `SimulationControl.Do_Simulation` direkt und wertet weder `Sperrgrund` (seit ADR-001) noch das neue `Fehlertext` aus. Im zweikanaligen Weg sieht ein Anwender die Meldung „Der Heizkessel … ist im Projekt nicht hinterlegt" deshalb nicht mehr als Dialog, sondern nur auf der Konsole; der headless-Weg über `SimulationRunner` bekommt sie vollständig | bewusst nicht in dieser Nacharbeit geändert — es ist eine ANZEIGE-Aufgabe (dieselbe, die `Sperrgrund` schon offen hat) und gehört zu Paket 8. Der Altpfad zeigt seinen Dialog unverändert |

---

## 13.13 Reproduktion der Nacharbeit

```powershell
$msb   = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
& $msb C:\Waermeplan\WP_Plan\WP-Plan.sln                      -p:Configuration=Debug -p:Platform=x86
& $msb C:\Waermeplan\WP_Plan\Referenzlauf\Referenzlauf.csproj -p:Configuration=Debug -p:Platform=x86

$exe   = "C:\Waermeplan\WP_Plan\Referenzlauf\bin\x86\Debug\net8.0-windows\Referenzlauf.exe"
$probe = "<Scratchpad>\Probe5\bin\x86\Debug\net8.0-windows\Probe5.exe"

# 1. Flag AUS - Pflicht, muss byte-identisch sein
foreach ($id in 1007,1008,1010,1011,1017,1018,1021,1023,1024) {
    & $exe projekt $id "C:\Waermeplan\Paket5_Test\Lauf_Aus_N2\Projekt_$id" C:\Waermeplan\Paket5_Test\DB_Basis
}
& $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-14_B1-Fixes C:\Waermeplan\Paket5_Test\Lauf_Aus_N2
& $exe pruefen   C:\Waermeplan\Paket5_Test\Lauf_Aus_N2
# zusaetzlich MD5 ueber alle 208 CSV -> keine abweichende Datei

# 2. Flag AN gegen den Stand VOR der Nacharbeit
foreach ($id in 1007,1008,1010,1011,1017,1018,1021,1023,1024) {
    & $exe projekt $id "C:\Waermeplan\Paket5_Test\Lauf_An_N2\Projekt_$id" C:\Waermeplan\Paket5_Test\DB_Flag
}
& $exe vergleich C:\Waermeplan\Paket5_Test\Lauf_An C:\Waermeplan\Paket5_Test\Lauf_An_N2
# zusaetzlich Schluessel-Vergleich der aggregate.csv + MD5 je Ganglinie -> 0 Abweichungen

# 3. Praeparierte Review-Szenarien (Kopien im Scratchpad, produktive DB nur LESEN)
& $probe <Scratchpad>\P5R\DB_S2       1018   # N1/N3: Kessel mit Puffer-HAUPTsenke, ohne WP
& $probe <Scratchpad>\P5R\DB_K_HAUPT  1023   # N2/N3: zwei Lader an einem Speicher
& $probe <Scratchpad>\P5R\DB_K_ZWEIT  1023   # N2, 13.9: Kessel mit Zweitsenke
& $probe <Scratchpad>\P5R\DB_ORD_MIT  1011   # N4:    Solar zwischen zwei Mitgliedern
& $probe <Scratchpad>\P5R\DB_ORD_OHNE 1011   # N4:    Vergleichsfall ohne Kesselsenke
& $probe <Scratchpad>\P5R\DB_DEF_C1   1018   # N5(a): WS_ID_Puffer NULL
& $probe <Scratchpad>\P5R\DB_DEF_C2   1018   # N5(b): Puffer eines fremden Projekts

# 4. Die sieben Szenarien aus F.6 zur Gegenprobe der N6-Extraktion
foreach ($s in "S1a","S1c","S1d","S1b","S3","S3b") { & $probe C:\Waermeplan\Paket5_Test\DB_$s 1007 }
```

`Probe5` rechnet über `SimulationRunner.Simuliere` und **speichert nichts**; seit der
Nacharbeit weist es zusätzlich die Eigenanteile, die Deckungssumme gegen die tatsächliche
Projektdeckung und die Formeln VOR der Nacharbeit zum direkten Vergleich aus. Die
präparierten Datenbanken entstehen als Kopien von `DB_Basis` bzw. `DB_Flag` mit einem
`UPDATE` auf `Tab_Energieanlagen` (Senkenfelder) und `Tab_Pufferspeicher`
(Verwendung, Temperaturen, Schwellen).

**Die produktive `Kenndaten.accdb` wurde auch in der Nacharbeit ausschließlich gelesen.**

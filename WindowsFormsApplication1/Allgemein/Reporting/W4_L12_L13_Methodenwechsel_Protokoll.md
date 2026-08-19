# W4 — Leitentscheidungen L12 und L13: Methodenwechsel 2027 und Bilanzierungskonvention Biomasse

**Umgesetzt am 19.08.2026.** Nacharbeit zu den beiden Befunden **A3** und **A4** der Abnahme E8
([`W4_E8_Abnahme_Protokoll.md`](W4_E8_Abnahme_Protokoll.md)): L12 lag nur als Katalogdatenseite
ohne Leser vor, L13 war gar nicht umgesetzt und bis zur Abnahme nicht einmal als offener Punkt
geführt.

Faktenbasis:
[`Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`](../../../Grundlagen_KWKG_Energiesteuer_Stromsteuer.md),
Abschnitte 7.1, 7.4, 7.5, 7.7, 7.8 und 8. Konzept:
[`Konzept_BHKW_Kosten_Erloese.md`](Konzept_BHKW_Kosten_Erloese.md), Leitentscheidungen L11 bis L13.

Ausgangsstand `3307378`, Arbeitsbaum sauber. **Ergebnisneutral für Bestandsprojekte im
Vorgabezustand** — 216/216 Simulationsdateien byte-gleich gegen die Basis `2026-08-19_B6`,
972/972 Wirtschaftlichkeitswerte identisch gegen `3307378`.

---

## 1 Was vorher galt — und warum niemand es sah

### 1.1 L12: die Stromgutschriftmethode steckt in der Systemgrenze, nicht in einer Zahl

`EmissionsBilanzRechner` stellt die **gekoppelte** Erzeugung einer **getrennten** Referenz
gegenüber. Die Referenz erzeugt dieselbe Brennstoffwärme im Referenzkessel **und denselben
KWK-Strom im Referenz-Kraftwerkspark**. Dieser zweite Summand *ist* die Stromgutschrift: Er
schreibt dem BHKW gut, dass der Strom anderswo nicht erzeugt werden muss.

Genau diese Methode ist zum **01.01.2027 abgeschafft**. Der Verdrängungsstrommix (2,8 bzw.
860 g CO₂-Äq/kWh) entfällt ersatzlos; das Gebäudemodernisierungsgesetz (BGBl. 2026 I Nr. 226)
verweist stattdessen auf DIN EN 15316-4-5:2017-09, Abschnitt 6.2.2.1.6.3. Der Bestand kannte
davon nichts: Etappe E1 hatte die 2027er-Katalogzeilen **ohne Wert** angelegt (ein bewusst
entfallener Satz, unterscheidbar von „nicht gepflegt"), aber **keine Codezeile las sie** — Befund
A3.

### 1.2 L13: die stille Konvention steht im Brennstoffkatalog

Die Konvention war nie im Code — sie steht in den **Katalogwerten**. `Tab_Brennstoff_Stamm` führt

| Träger | Kategorie | CO₂ [g/kWh] | entspricht |
|---|---|---|---|
| Holz | 5 | 20 | GEG/GModG Anlage 9, Holz |
| Pellets | 6 | 20 | dito |
| Biogas | 1 (Bezeichner „Biogas") | 140 | GEG Anlage 9, Biogas bis 2026 |
| Rapsöl | 8 | 210 | GEG Anlage 9, Bioöl |
| Tierische Fette | 9 | 210 | dito |

Das sind durchgehend **reine Vorkettenwerte**. Die dahinterstehende Annahme lautet: **biogenes
Verbrennungs-CO₂ = 0.** Damit gilt heute stillschweigend die Konvention von GEG/GModG,
UBA-Emissionsbilanz und BAFA EEW — nicht die des UBA-CO₂-Rechners, der als einziges der fünf
Regelwerke aus Grundlagen 7.8 **365 g/kWh** ansetzt.

Auf der BEHG-Seite kommt eine zweite stille Annahme dazu. `KostenEmissionRechner` stuft nur die
Kategorien 1 Gas, 2 Öl, 3 Koks, 4 Kohle und 11 Sonstige als abgabepflichtig ein und nimmt „Biogas"
namentlich aus. Rapsöl (8) und Tierische Fette (9) fallen damit **vollständig aus der
BEHG-Rechnung** — obwohl Pflanzenöl und Tierfette in Anlage 2 Teil 4 der EBeV 2030 mit
74,0 t CO₂/TJ (266,4 g/kWh) ausdrücklich **BEHG-Brennstoffe** sind. Der Nullansatz des § 8 EBeV
2030 setzt aber einen **Nachhaltigkeitsnachweis** voraus. Der Bestand rechnet also so, **als läge
dieser Nachweis immer vor** — ohne ihn je zu erfragen.

**Diese beiden Ist-Annahmen sind die Vorgabe geworden.** Sichtbar gemacht, nicht verändert.

---

## 2 Was jetzt gilt

### 2.1 Der eine Schalter (L12)

Neue Klasse [`Allgemein/Wirtschaftlichkeit/BilanzKonvention.cs`](../Wirtschaftlichkeit/BilanzKonvention.cs)
— reine Funktion über DTOs, ohne eigenen Datenbankzugriff (L9). Sie bekommt den geladenen
`GesetzKatalog` und den Parametersatz und löst daraus den Rechenweg auf:

```
Wahl = KATALOG  →  Zeile EF_NACHWEIS_VERDRAENGUNGSSTROMMIX zum Bilanzjahr suchen
                   Zeile führt einen Wert  → STROMGUTSCHRIFT   (Rechtsstand bis 31.12.2026)
                   Zeile führt KEINEN Wert → OHNE_GUTSCHRIFT   (Rechtsstand ab 01.01.2027)
                   keine Zeile gefunden    → STROMGUTSCHRIFT   (Bestand darf nicht kippen)
Wahl ≠ KATALOG  →  die gewählte Methode, im Bericht als Wahl gekennzeichnet
```

**Umgeschaltet wird über dasselbe Gültig-ab-Datum, das der Katalog ohnehin führt** — keine
Jahreszahl im Code, kein zweiter Schalter daneben. Damit lesen erstmals Codezeilen die
2027er-Zeilen, die seit E1 unbenutzt im Katalog stehen.

**Und L11 bleibt strikt gewahrt.** Von der Nachweiszeile wird ausschließlich gelesen, **ob** sie
einen Wert führt. Der Wert 860 g CO₂-Äq/kWh belegt keine Variable dieser Klasse und erreicht keine
Bilanzrechnung. Der einzige Faktor, den L12 in die Bilanz einspeist, ist
`EF_BILANZ_SUBSTITUTION_STROM` — aus der Klasse `EF_BILANZ`, nicht `EF_NACHWEIS`.

Die zweite Verdrängungszeile (`PEF_NACHWEIS_VERDRAENGUNGSSTROMMIX`, Primärenergie) wird **als
Gegenprobe** gelesen, stellt den Schalter aber nicht. Widersprechen sich beide Stichtage, meldet
die Klasse das als Hinweis, statt zu handeln — genau der Fall, den „nicht zwei Schalter, die
auseinanderlaufen können" ausschließen soll. (Eine Primärenergierechnung gibt es in EPOS-Plan
ohnehin nicht; `PEF_NACHWEIS_*` wird nur in der Pflegemaske angezeigt.)

### 2.2 Die drei Rechenwege im `EmissionsBilanzRechner`

| Methode | getrennte Referenz | SO₂ / NOx |
|---|---|---|
| `STROMGUTSCHRIFT` | Referenzkessel **+ Kraftwerkspark** (Brennstoff = KWK-Strom ÷ (1 − Netzverluste) ÷ η) | wie bisher aus dem Park |
| `OHNE_GUTSCHRIFT` | **nur** Referenzkessel | keine Gutschrift |
| `SUBSTITUTION` | Referenzkessel + KWK-Strom × Substitutionsfaktor | **keine** Gutschrift, mit Hinweis |

Bei `SUBSTITUTION` gehen Wirkungsgrad und Netzverluste des Parks **nicht** ein: Der
Substitutionsfaktor ist eine Größe je kWh **Strom**, kein Brennstofffaktor. Für SO₂ und NOx gibt es
keinen belegten Substitutionswert — sie bleiben ohne Gutschrift, und der Bericht sagt das, statt
eine Zahl zu erfinden.

> **Abgrenzung, die im Bericht steht und die man nicht überlesen darf.** Verdrahtet ist der
> **Wegfall der Gutschrift**, nicht das Zuteilungsverfahren der DIN EN 15316-4-5. Der Text dieser
> Norm gehört nicht zur Faktenbasis des Vorhabens; ihn zu erraten wäre schlechter als die
> Lücke zu benennen. Die Grundlagen halten dazu fest: „Einen amtlichen Ersatz speziell für KWK gibt
> es nicht." Wer nach 2027 eine Gutschrift rechnet, trifft eine methodische Wahl — sie ist jetzt
> ein Auswahlparameter und wird als Wahl ausgewiesen.

### 2.3 Zwei getrennte Angaben für Biomasse (L13)

**Konvention** (`Biomasse_Konvention`) entscheidet über die **Klimabilanz**:
`NULLANSATZ` (Vorgabe) zählt nur die Vorkette des Brennstoffkatalogs; `VERBRENNUNG` addiert
`EF_BILANZ_BIOGEN_VERBRENNUNG` (365 g/kWh, UBA-CO₂-Rechner) auf die biogene Brennstoffmenge. Der
Betrag steht als eigenes Feld `EmissionsBilanz.CO2BiogenT` und im Bericht als eigene Zeile — man
sieht, welcher Teil der gekoppelten Emission aus der **Wahl** stammt.

**Nachhaltigkeitsnachweis** (`Biomasse_Nachweis`) entscheidet über die **BEHG-Abgabe**: Fehlt er,
wird die **flüssige** Biomasse (Rapsöl, Tierische Fette) mit dem vollen fossilen Standardwert der
EBeV 2030 (`EF_BILANZ_EBEV_PFLANZENOEL`, 266,4 g/kWh) abgabepflichtig. Feste Biomasse, Biogas und
Klärgas sind keine BEHG-Brennstoffe (Grundlagen 7.7) und bleiben in jedem Fall außen vor.

Die Einstufung „biogen" und „BEHG-biogen" liegt an **einer** Stelle
(`BilanzKonvention.IstBiogen` / `IstBehgBiogen`) und wird von beiden Rechnern benutzt — dieselbe
Bauform, mit der `KostenEmissionRechner` schon die BEHG-Pflichtigkeit bestimmt.

---

## 3 Entscheidungen dieser Umsetzung

### 3.1 Das Bilanzjahr fällt auf **2026** zurück, nicht auf das Systemjahr

Das ist die folgenreichste Entscheidung dieser Nacharbeit, und sie ist eine Abwägung.

Der naheliegende Weg wäre gewesen, den Stichtag aus einer vorhandenen Größe abzuleiten. Es gibt
zwei Kandidaten, und **beide brechen die Ergebnisneutralität**:

- `WirtschaftlichkeitCtrl.Foerderbeginn` fällt ohne gepflegte Inbetriebnahme auf
  `DateTime.Now.Year + 1` zurück — **heute also auf 2027**. Daran den Wegfall des
  Verdrängungsstrommix zu hängen, hätte **jedes** Bestandsprojekt ohne Inbetriebnahmedatum sofort
  auf den neuen Rechtsstand gezogen.
- `KwkgInbetriebnahme` selbst trifft Projekte mit geplanter Inbetriebnahme ab 2027 — davon gibt es
  in der produktiven Datenbank eines (**Projekt 1030, IBN 01.03.2027**). Auch das wäre eine
  Ergebnisänderung im Vorgabezustand.

Ein Rückfall auf `DateTime.Now.Year` scheidet aus einem dritten Grund aus: Grundlagen 7.1 verlangt
ausdrücklich, dass ein 2026 gerechneter Variantenvergleich **2029 dieselben Zahlen liefert**. Ein
Jahr aus der Systemuhr bräche das an jedem Jahreswechsel.

Deshalb: **eigene Projektangabe `Bilanz_Jahr`, NULL/0 = nicht gepflegt, Rückfall auf die feste Zahl
2026** (`BilanzKonvention.BILANZJAHR_RUECKFALL`). Deterministisch, reproduzierbar, ergebnisneutral.

**Der Preis dieser Entscheidung, offen benannt:** Der Methodenwechsel greift **nicht von selbst**,
wenn das Kalenderjahr 2027 anbricht. Er greift, wenn jemand das Bilanzjahr einträgt. Damit ist L12
eine „Auswahl mit Ausweis" — in dieser Ausbaustufe die etablierte Antwort auf Rechtsunsicherheit,
hier aber zugleich eine Antwort auf ein **Reproduzierbarkeits**problem. Wer das anders gewichtet,
ändert eine Konstante und einen Vorgabewert; die Mechanik bleibt.

### 3.2 Der Nachhaltigkeitsnachweis ist TEXT, nicht YESNO — gegen die ACE-Falle

Access belegt eine neue `YESNO`-Spalte in **jeder** Bestandszeile mit `False`. Bei den Schaltern der
Etappen E4 und E5 zeigte das in die gewollte Richtung (kein Nachweis ⇒ keine Gutschrift). Hier
zeigt es **in die falsche**: Ein Feld „Nachweis vorhanden" stünde nach der Migration in jedem
Altprojekt auf NEIN und hätte jedem Projekt mit biogenem Brennstoff eine CO₂-Abgabe aufgebürdet,
die es heute nicht trägt.

Deshalb eine `TEXT(30)`-Spalte mit DML-Vorbelegung `NACHWEIS_JA` und einer toleranten Leseseite:
**nur** der ausdrückliche Wert `NACHWEIS_NEIN` entzieht den Nachweis; leer, NULL und jeder
unbekannte Bestandswert bedeuten JA.

### 3.3 Bewusst nicht getan

- **`VariantenDaten.CO2Gesamt` und `CO2Spezifisch` bleiben unberührt.** Sie werden im
  `BerichtsDatenSammler` gerechnet, bevor irgendein Parametersatz bekannt ist; die Konvention dort
  anzuwenden hätte eine neue Abhängigkeit der Berichtsdatensammlung vom Wirtschaftlichkeitsmodul
  bedeutet. Die Konvention wirkt auf die **Emissionsbilanz** (die Klimabilanz dieser Anwendung) und
  auf die **BEHG-Abgabe**; die Kennzahl „CO₂ gesamt" bleibt die katalogbasierte Größe. Der Bericht
  weist die Konvention an beiden Blöcken aus, sodass kein Leser die beiden verwechseln kann.
  **Nachteil, offen benannt:** Bei gewählter Konvention `VERBRENNUNG` zeigen Kennzahl und
  Emissionsbilanz verschiedene CO₂-Zahlen für dasselbe Projekt.
- **Die Bio-Heizöl-Mischungen (Kategorie 2, „Heizöl Bio 5" bis „Heizöl Bio 20") bleiben außen
  vor.** Ihr biogener Anteil steckt im Katalogfaktor (295 / 280 / 266 / 250 statt 310 g/kWh), das
  Datenmodell führt ihn aber nicht als eigene Größe. Ihn aus dem Namen zu lesen wäre geraten. Der
  Berichtshinweis sagt das ausdrücklich.
- **Der Seedwert des Kraftwerksparks wurde nicht angefasst.** `Tab_Kraftwerkspark` Zeile 1
  („Deutscher Strommix") führt **560 g/kWh** — das ist der **Nachweis**wert der GEG Anlage 9, nicht
  der reale Strommix (2025: 406 g CO₂-Äq/kWh mit Vorkette). Das ist ein L11-Verstoß **in den
  Daten**, kein neuer: Er stammt aus Stufe W3 und steht so in jeder produktiven Datenbank. Ihn zu
  korrigieren hätte jede Bestandsbilanz verändert und gehört zum offenen Punkt 9
  (`Tab_Kraftwerkspark` ohne `Bezugsbasis`). **Neu eingeführt wird durch L12/L13 kein einziger
  Nachweiswert in die Bilanz.**
- **Keine Primärenergierechnung.** `PEF_NACHWEIS_*` hat in EPOS-Plan keinen Leser außer der
  Pflegemaske; der Verdrängungs-PEF 2,8 wird nur als Gegenprobe des Stichtags herangezogen.
- **Keine dauerhaften Tests** (offener Punkt 10 aus E8). `BilanzKonvention` ist bewusst
  datenbankfrei und damit unittestbar gebaut — der Test selbst gehört in denselben Vorgang, der
  auch `SteuerGutschriftRechner`, `StromTarifRechner` und `KwkgSatzRechner` abdeckt.

---

## 4 Datenmodell und Katalog

### 4.1 Migrationsschritt 23 (`SchemaMigration.ZIEL_VERSION` 22 → 23)

Vier additive Spalten an `Tab_ProjektWirtschaftlichkeit` — einer reinen Projekttabelle **ohne
`_STAMM`-Gegenstück**, dieselbe Lage wie bei den Schritten 20 und 21.

| Spalte | Typ | Vorbelegung (23b) | Wirkung im Vorgabezustand |
|---|---|---|---|
| `Bilanz_Jahr` | `LONG` | **keine** — bleibt NULL | Rechtsstand bis 31.12.2026 |
| `Emissions_Methode` | `TEXT(30)` | `KATALOG` | Stromgutschrift wie bisher |
| `Biomasse_Konvention` | `TEXT(30)` | `NULLANSATZ` | Vorkettenwerte wie bisher |
| `Biomasse_Nachweis` | `TEXT(30)` | `NACHWEIS_JA` | BEHG-Abgabe wie bisher |

`TEXT(30)` statt `TEXT(24)`: Der längste Steuerwert hat 15 Zeichen, aber die Lehre aus Schritt 19
(Probe C2) ist teuer bezahlt — ein zu kurzes Feld lässt das UPDATE **still** scheitern.

Dazu die tolerante Vorsorge in `WirtschaftlichkeitCtrl.StelleTabellenSicher` (`SpalteSicher` je
Spalte), damit eine nie migrierte Datenbank nicht an einer fehlenden Spalte scheitert. Die
WERTE-Vorbelegung bleibt allein bei 23b; die Leseseite behandelt leer/NULL überall wie den
Vorgabewert — **gemessen**: siehe V6.

### 4.2 Katalog, Generation 4

| Schlüssel | Klasse | Ab | Wert | Status | Quelle |
|---|---|---|---|---|---|
| `EF_BILANZ_SUBSTITUTION_STROM` | `EF_BILANZ` | 2024 | 685 g CO₂-Äq/kWh | **VORLAEUFIG** | UBA CLIMATE CHANGE 11/2026 — für Photovoltaik hergeleitet |
| `EF_BILANZ_BIOGEN_VERBRENNUNG` | `EF_BILANZ` | 2024 | 365 g/kWh | **VORLAEUFIG** | UBA-CO₂-Rechner, Methodikumstellung März 2024 |

Beide gehen über die generationsweise Nachsaat aus E6 auch in bereits gesäte Datenbanken.
Beide stehen bewusst auf `VORLAEUFIG`: Sie stützen sich auf UBA-Veröffentlichungen, nicht auf eine
Rechtsvorgabe. Nichts davon ist erfunden — beide Zahlen stehen in den Grundlagen, Abschnitte 7.4
und 7.8.

---

## 5 Verifikation

| # | Prüfung | Methode | Ergebnis |
|---|---|---|---|
| V1 | Build | `MSBuild WP-Plan.sln -p:Platform=x86`, Ausgabe ausschließlich in den Scratch-Ordner | **0 Fehler, exakt 6 Warnungen** (CS0108 ×2, CS0109 ×2, CS1998, CS4014) — der Bestandssatz, keine siebte |
| V2 | Referenzlauf gegen die Basis | `Referenzlauf.exe lauf --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030`, danach `vergleich` gegen `2026-08-19_B6` | **9/9 PASS**, 2.366.177 Werte innerhalb der Toleranz |
| V3 | Byte-Identität | `cmp` je Datei gegen B6 | **216/216 gleich**, 0 Abweichungen, 0 zusätzliche und 0 fehlende Dateien |
| V4 | Plausibilität | `Referenzlauf.exe pruefen` | **GESAMT plausibel** |
| V5 | **Wirtschaftlichkeit A/B gegen `3307378`** | Reflexions-Harnisch auf `BerichtsDatenSammler.SammleFuerBericht`, **je eigene Wegwerf-DB**, 9 Projekte × 3 Szenarien, alle öffentlichen Skalarfelder von `WirtschaftlichkeitErgebnis` | **972 von 972 Werten identisch**, 0 Abweichungen |
| V6 | Ergebnisneutralität **ohne** Migration | Der B-Lauf von V5 läuft auf einer Datenbank, in der nur `SpalteSicher` die vier Spalten angelegt hat (alle NULL, keine Vorbelegung) | identisch zu A — die tolerante Leseseite trägt |
| V7 | Migrationsschritt 23 | `Referenzlauf.exe migration` auf einer Wegwerfkopie | Schemastand **22 → 23**, „Schritt 23 … OK", **6 Angaben über drei Spalten vorbelegt** (2 Parametersätze × 3 Spalten), `Bilanz_Jahr` bleibt NULL. Die zwei gemeldeten Nachweisabweichungen sind Bestand (`PufferHeizung ohne WS_ID_Puffer: 2`) und berühren Schritt 23 nicht |
| V8 | **Rundprobe** | Je Projekt vier Wertesätze speichern, mit **neuer** Controller-Instanz neu laden, alle vier Felder vergleichen; Projekt 1024 ohne Parameterzeile (INSERT-Pfad), Projekt 1030 mit (UPDATE-Pfad) | **8/8 GLEICH** |
| V9 | Wirkungsbeleg **L12** | präparierte Wegwerfkopie, Projekt 1030 (Zweimodul-BHKW mit Einspeisung), Kraftwerkspark 1 | Abschnitt 6.1 — **−70,0 % ausgewiesene CO₂-Vermeidung** ab Bilanzjahr 2027 |
| V10 | Wirkungsbeleg **L13** | präparierte Wegwerfkopie, Projekt 1024, BHKW-Träger auf „Tierische Fette" | Abschnitt 6.2 — **Vorzeichenwechsel** der Vermeidung, **+3.964,15 €/a** CO₂-Abgabe ohne Nachweis |
| V11 | **Handrechnungen** | fünf Fälle, jeder gegen die gemessene Ausgabe | Abschnitt 6.3 — **5/5 auf die geführten Nachkommastellen getroffen** |
| V12 | Berichtsausweis Word **und** Excel | Berichtslauf auf der präparierten Kopie (Bilanzjahr 2027), Text aus `document.xml` bzw. `sharedStrings.xml` extrahiert | Ausweis **an beiden Stellen**: in der Parameterzeile des Kapitels und über der Emissionsbilanztabelle, dort zusätzlich der DIN-EN-15316-4-5-Hinweis. Word 51.890 Byte, Excel 15.258 Byte |
| V13 | **Sprachprobe `en-US`** | derselbe Lauf mit `CurrentUICulture = en-US` und `Program.nLanguage = 1` | **Zahlen zeichengleich**; der Ausweis vollständig englisch („Balancing: balance year 2027 · CHP electricity without a displacement credit (GModG from 01/01/2027) …") |
| V14 | Ressourcen in beiden Sprachen und im Designer | Zählung je Schlüssel | **30 neue Schlüssel**, je genau einmal in `Resource.resx`, `Resource.en-US.resx` und `Resource.Designer.cs`; 0 geänderte Werte |
| V15 | Kodierung und Zeilenenden | `file`, CR/LF-Zählung, Suche nach U+FFFD über alle 22 berührten Dateien | BOM und Zeilenenden **je Datei unverändert** (LF bleibt LF, CRLF bleibt CRLF, BOM bleibt BOM), **0 Ersatzzeichen** |
| V16 | Produktivdatenbank nur gelesen | jeder Lauf auf einer Kopie; harte Zielprüfung im Harnisch („ABBRUCH, wenn der Pfad auf `%ProgramData%` zeigt") | keine `Kenndaten.laccdb` vor und nach den Läufen; Zeitstempel der produktiven Datei **unverändert 19.08.2026 17:49:58**, also vor Beginn dieser Arbeit |
| V17 | `bin\` des Repos unberührt | jeder Build ausschließlich mit `-p:OutDir=<Scratch>` | erfüllt |

---

## 6 Die Zahlen

### 6.1 L12 — Projekt 1030 (Zweimodul-BHKW, Einspeisung), Kraftwerkspark „Deutscher Strommix"

Bezugsgrößen: KWK-Strom **1.720,08 MWh/a**, Park 560 g/kWh bei η = 100 % und 0 % Netzverlusten,
Referenzkessel 1.473,432 t CO₂/a.

| Fall | gekoppelt [t/a] | getrennt [t/a] | **Vermeidung [t/a]** | darin Gutschrift KWK-Strom [t/a] |
|---|---|---|---|---|
| **Bilanzjahr 2026** (Vorgabe, `KATALOG`) | 1.061,57 | 2.436,68 | **1.375,11** | 963,24 |
| **Bilanzjahr 2027** (`KATALOG` ⇒ ohne Gutschrift) | 1.061,57 | 1.473,43 | **411,87** | 0 |
| Bilanzjahr 2027, `SUBSTITUTION` (685 g/kWh) | 1.061,57 | 2.651,69 | **1.590,12** | 1.178,25 |
| Bilanzjahr 2027, `STROMGUTSCHRIFT` ausdrücklich | 1.061,57 | 2.436,68 | **1.375,11** | 963,24 |

**Der Methodenwechsel kostet dieses Projekt 963,24 t CO₂/a ausgewiesene Vermeidung — 70,0 %.** Die
gekoppelte Seite bleibt unverändert; es verschwindet allein die Gutschrift. Wer stattdessen den
UBA-Substitutionsfaktor wählt, landet **über** dem alten Stand (+215,01 t, +15,6 %), weil
685 g/kWh über den 560 g/kWh des Parkeintrags liegen.

Der **Kapitalwert ist in allen vier Fällen identisch** (−21.501.274,65 €). Das ist richtig so: Die
Emissionsbilanz ist eine Ausweisgröße und speist keine Zahlungsreihe. L12 ändert die
**Klimaaussage** eines BHKW-Projekts, nicht seine Wirtschaftlichkeit.

Der Schalter greift nachweislich aus dem Katalog: `SchalterJahrVon` steht bei Bilanzjahr 2026 auf
**2020** (die Zeile mit 860 g/kWh) und bei Bilanzjahr 2027 auf **2027** (die Zeile ohne Wert),
`VerdraengungEntfallen` entsprechend auf 0 bzw. 1.

### 6.2 L13 — Projekt 1024 (präpariert: BHKW-Träger „Tierische Fette", 228,93 MWh/a)

Im gesamten Bestand gibt es **kein** Projekt mit biogenem Energieträger — deshalb die Präparation
auf einer Wegwerfkopie. CO₂-Preis 65 €/t, Kraftwerkspark 1.

| Fall | gekoppelt [t/a] | davon biogen [t/a] | Vermeidung [t/a] | CO₂-Abgabe [€/a] | Kapitalwert [€] |
|---|---|---|---|---|---|
| `NULLANSATZ` + Nachweis (**Vorgabe**) | 48,08 | 0 | **+44,89** | **0** | −5.095.306,86 |
| `VERBRENNUNG` + Nachweis | **131,63** | **83,56** | **−38,67** | 0 | −5.095.306,86 |
| `NULLANSATZ` ohne Nachweis | 48,08 | 0 | +44,89 | **3.964,15** | **−5.154.283,43** |
| `VERBRENNUNG` ohne Nachweis | 131,63 | 83,56 | −38,67 | 3.964,15 | (additiv aus beiden) |

**Die Konvention dreht das Vorzeichen der Aussage.** Mit dem Nullansatz vermeidet die Anlage
44,89 t CO₂/a gegenüber der getrennten Erzeugung; mit dem Ansatz des UBA-CO₂-Rechners **emittiert
sie 38,67 t/a mehr** als die Referenz. Dasselbe Projekt, dieselben Mengen, zwei Regelwerke — genau
der Widerspruch, den das Umweltbundesamt selbst benennt und den L13 sichtbar machen soll.

**Der fehlende Nachhaltigkeitsnachweis kostet 3.964,15 €/a**, über 20 Jahre bei 3 % ein Barwert von
**58.976,57 €** — der Kapitalwert verschlechtert sich um 1,16 %. Die Begründung steht im
Ergebnishinweis: „Ohne Nachhaltigkeitsnachweis (§ 8 EBeV 2030): 229 MWh flüssige Biomasse mit
266,4 g CO₂/kWh abgabepflichtig."

### 6.3 Handrechnungen

| # | Größe | Rechnung | erwartet | gemessen |
|---|---|---|---|---|
| H1 | Stromgutschrift 2026 | 1.720,08 MWh × 560 g/kWh ÷ 1000 | 963,2448 t | 963,2448 |
| H2 | Substitutionsgutschrift | 1.720,08 MWh × 685 g/kWh ÷ 1000 | 1.178,2548 t | 1.178,2548 |
| H3 | biogenes Verbrennungs-CO₂ | 228,93 MWh × 365 g/kWh ÷ 1000 | 83,55945 t | 83,55945 |
| H4 | BEHG ohne Nachweis | 228,93 MWh × 266,4 g/kWh ÷ 1000 × 65 €/t | 3.964,15188 € | 3.964,15188 |
| H5 | Barwert dieser Abgabe | 3.964,15188 €/a × (1,03²⁰−1)/(0,03·1,03²⁰) = × 14,877475 | 58.976,57 € | 58.976,57 (Kapitalwertdifferenz) |

Die Gegenprobe zu H3 sitzt in derselben Zeile: 48,0753 t ÷ 210 g/kWh × 1000 = **228,93 MWh** —
dieselbe Menge, die H3 mit 365 g/kWh bewertet. Vorkette und Verbrennung greifen auf dieselbe
Bezugsmenge zu.

---

## 7 Berührte Dateien

| Datei | Änderung |
|---|---|
| `Allgemein/Wirtschaftlichkeit/BilanzKonvention.cs` | **neu** — Auflösung der Regeln, Einstufung biogener Träger, Ausweistext |
| `Allgemein/Wirtschaftlichkeit/EmissionsBilanzRechner.cs` | drei Rechenwege für die Stromgutschrift, biogenes Verbrennungs-CO₂, Konvention am Ergebnis |
| `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` | Spaltenvorsorge, Laden, Speichern, BEHG-Basis ohne Nachhaltigkeitsnachweis, `Bilanzregeln()` |
| `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitDaten.cs` | vier Parameterfelder, drei Felder an `EmissionsBilanz` |
| `Allgemein/Wirtschaftlichkeit/GesetzKatalog.cs` | Generation 4 mit zwei Zeilen |
| `Allgemein/Bericht/KostenEmissionRechner.cs` | Mengen biogener Träger (ohne jede Wertung) |
| `Allgemein/Bericht/BerichtsDaten.cs` | zwei Mengenfelder an `VariantenDaten` |
| `Allgemein/Bericht/Bausteine/BausteineWirtschaftlichkeit.cs` | Ausweis in Parameterzeile und Emissionsbilanzblock |
| `Allgemein/Bericht/ExcelBerichtGenerator.cs` | dasselbe für das Excel-Blatt |
| `Allgemein/Update/SchemaKatalog.cs`, `…/SchemaMigration.cs` | Schritt 23 (DDL + DML), `ZIEL_VERSION` 23 |
| `Allgemein/DbWerte.cs` | sieben Steuerwerte, zwei Katalogschlüssel |
| `Views/Wirtschaftlichkeit/Form_WirtschaftlichkeitParameter.cs` | Eingabeblock „Bilanzierung" |
| `Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.cs` | Ausweis in der Parameterzeile des Reiters |
| `MyResource/Resource.resx`, `…en-US.resx`, `…Designer.cs` | 30 neue Schlüssel |

---

## 8 Übergabepunkte

1. **Das Bilanzjahr ist eine Entscheidung, keine Automatik.** Wer will, dass der Methodenwechsel am
   01.01.2027 von selbst greift, muss den Rückfallwert von 2026 auf das Systemjahr umstellen — und
   nimmt damit in Kauf, dass gespeicherte Projekte über den Jahreswechsel andere Zahlen zeigen
   (Abschnitt 3.1). Beides ist vertretbar; still darf keins von beidem passieren.
2. **Die DIN EN 15316-4-5 ist nicht abgebildet**, nur der Wegfall der Gutschrift. Liegt der
   Normtext vor, ist das Zuteilungsverfahren ein eigener Rechenweg neben den drei heutigen — die
   Auswahl trägt ihn ohne Umbau.
3. **`Tab_Kraftwerkspark` führt mit 560 g/kWh einen Nachweiswert in der realen Bilanz** (offener
   Punkt 9, Befund A5). Solange das so ist, unterschätzt jede Stromgutschrift den heutigen
   Strommix nicht, sondern **überschätzt** ihn um rund 38 % gegenüber 406 g CO₂-Äq/kWh. Das ist
   Bestand, kein Ergebnis dieser Arbeit — aber es steht jetzt unmittelbar neben einer Einstellung,
   die dieselbe Größe betrifft.
4. **Bio-Heizöl-Mischungen** brauchen ein Feld „biogener Anteil" am Energieträger, wenn die
   Konvention auch für sie gelten soll (Abschnitt 3.3).
5. **`BilanzKonvention` ist datenbankfrei** und damit der erste der neuen Rechenwege, der einen
   Unittest ohne Wegwerf-Datenbank tragen kann (offener Punkt 10).
6. **Der Dialogblock ist lokalisiert, seine Nachbarn nicht.** `Form_WirtschaftlichkeitParameter`
   trägt jetzt neben 23 deutschen Literalen einen vollständig über `MyResource` geführten Block.
   Das ist der gewollte Weg für neue Texte (Drei-Schichten-Regel) und macht den Mischzustand
   sichtbarer — offener Punkt 11 bleibt.

# H4a — Rückfall-Bezugsgrößen: % der Investition, je kWh thermisch/elektrisch (Protokoll)

Etappe H4a des Pflichtpositionen-Vorhabens — erster Teil des offenen Punkts **H1-1b**
aus [`H2_Endenergie_Protokoll.md`](H2_Endenergie_Protokoll.md), umgesetzt am
**29.08.2026** auf Branch `Pufferspeicher`; Commit ohne Push (Etappenregel).

Definitionsgrundlage ist das Kostendialoge-Konzept § 5.3: „% der Investition" = Summe
der Investitionskosten der Komponente **vor Zuschussabzug**; „je kWh thermisch" =
erzeugte Wärme, „je kWh elektrisch" = erzeugter bzw. bezogener Strom aus dem
Simulationslauf. **„% der Erzeugerkosten" gehört zum Investitionsraster und bleibt
H4b**, ebenso die sechs Gerätewelt-Arten (kW/kWp/m²/Kapazität).

---

## 1 Umgesetzt

### 1.1 Die Regel: Rückfall statt Frischzwang

Anders als die Endenergie-Arten (H2: Menge **immer** frisch, per Konzept § 4.5) holen
die H4a-Arten ihre Bezugsgröße **nur, wenn keine Menge gepflegt ist** — eine
persistierte Menge behält Vorrang (der Alt-BHKW-Dialog schreibt welche; VALERI-Geist
„gepflegt schlägt abgeleitet"). Damit ist der Bestand konstruktiv unangetastet, und
die H3-Pflichtzeilen (Instandhaltung „% der Investition" — bislang 0 € trotz
gepflegtem Satz) werden erstmals rechenfähig.

### 1.2 Bausteine

| Baustein | Inhalt |
|---|---|
| `BetriebskostenCtrl.InvestSummeFuer` (neu, internal) | „% der Investition" **stufig**: Investitionszeilen an genau dieser Anlage → sonst Komponentensumme (die § 5.3-Regel) → notfalls Projektsumme. Kern ist die vorhandene K5-Abfrage (Zuschuss-Ausschluss, Kostenart-Toleranz) — um einen optionalen Anlagenfilter erweitert (Spaltenprobe Schritt 45), die Alt-Überladung delegiert unverändert |
| `EndenergieAufloeser.WaermeerzeugungKwh` / `StromgroesseKwh` (neu) | Laufmengen anlagenscharf über den Bezeichner, Komponentensumme als Rückfall — dasselbe H2-Verfahren. Wärme: BHKW/Heizkessel/Wärmepumpe/Solarthermie; Strom: BHKW/PV = Erzeugung, Wärmepumpe = Bezug (Stromverbrauch + Heizstab); übrige Komponenten bewusst null |
| `EndenergieAufloeser.FuerProjekt` | liefert jetzt auch **ohne Simulationslauf** eine Instanz — die Investitionsart braucht keinen Lauf; Lauf-Größen bleiben dann null (Festlegungstreu) |
| `WirtschaftlichkeitCtrl` | gemeinsamer Helfer `KomponenteUndAnlage` (H2-Refaktor), Artenweiche `IstRueckfallErmittelbareArt` + `RueckfallMenge`; eingebaut in Summenschleife **und** E7-Positionsliste (muss deren Summe treffen) |

## 2 Nachweise

**Build:** VS-MSBuild x64, OutDir umgeleitet — **grün**, Warnprofil = Altbestand
(2× CS0108, 2× CS0109, 1× CS1998). Erste Etappe auf der umbenannten `EPOS_Plan.dll`.

**Harness `..\dev\h4a\`** (gitignored; lesend gegen Produktiv, schreibend nur gegen
die Scratchpad-Kopie mit den H3-Pflichtzeilen):

| Probe | Ergebnis | Soll |
|---|---|---|
| [1] Bestand (Produktiv): alle 7 Zeilen der drei Arten | **Satz leer, Betrag 0** — der Rückfall ändert am Bestand nichts | strikte Neutralität |
| [2] „% der Investition": Kessel-Pflichtzeile (K2/A14854), Testbasis 50.000 € über den App-Schreibweg angelegt, Satz 2 % | **1.000,00 €/a**, Menge 50.000 in der Positionszeile | 1.000,00 |
| [2] „je kWh thermisch" **anlagenscharf**: Wartung der Haupt-WP (A14817), Satz 0,005 €/kWh | Menge **211.140 kWh** = exakt die SQL-Wärmesumme der Modulzeile „CS6800iAW MB + AW 10 OR-T"; **1.055,70 €/a** | 211,1 MWh × 0,005 |
| [2] Summen | `LiesBetriebskosten` = **2.055,70** = Erwartung; **E7-Probe: Positionssumme == Summenschleife JA** | deckungsgleich |
| [3] Regressionsläufe 1010/1018/1024/1035 | fehlerfrei; 1024 weiter 99,00 €/a (Bestandswert) | unverändert |

## 3 Offen (Fortschreibung H1-1b)

| Nr. | Punkt |
|---|---|
| ~~H4a~~ | ~~Kosten- und Laufwelt~~ — **erledigt mit dieser Etappe** |
| H4b | Gerätewelt-Arten (je kW Leistung/Heizleistung/elektrisch, je kWp, je kWh Kapazität, je m² Kollektorfläche — Leseketten über die Anlagen-Geräteverweise) und „% der Erzeugerkosten" samt Investitionsraster-Lesepunkt |
| H2-1 | Mengen-Ausweis beim Dialog-Speichern („Stand des Laufs vom …") — Dialogetappe |

## 3a Nachtrag 09.09.2026 (W5‑B‑8) — die Bezugsgröße „% der Investition" kommt aus der KASKADE

`InvestSummeFuer` staffelte bis hierher über `SUM(EingegebenerWert)` der Kategorie-1-Zeilen. Seit
dem Anwenderentscheid W5‑B‑8 staffelt es über `InvestKaskade.Summen(projektID, ERWARTET)` —
**dieselbe Stufung Anlage → Komponente → Projekt, dieselbe Reihenfolge, dieselbe K5-Regel
„vor Zuschussabzug"**; nur die Zahl, auf die gestaffelt wird, ist jetzt die vollständige
(satzbasierte Zeilen Menge × Satz und die Prozentzeilen der Investseite zählen mit). Der alte
Weg lebt als `InvestSummeSql` weiter und gilt nur noch, wenn die Kaskade nichts anzubieten hat —
also auf einer Datenbank ohne die Spalten aus Schritt 19.

`RueckfallMenge` (WirtschaftlichkeitCtrl) führt die Kaskade seither als Merker der laufenden
Leseschleife, damit der Rechenweg der Kategorie 1 nicht je Betriebskostenzeile erneut läuft —
dasselbe Muster wie beim Endenergie-Auflöser. Nachweis, Beispiel und Regressionsliste:
`iU9_W5_Blazor_Port_Protokoll.md`, Abschnitt „Anwenderentscheid 09.09.2026 — W5‑B‑8";
Fälle in `EPOS.Kern.Tests/BetriebskostenBasisTests.cs`.

## 3b Nachtrag 10.09.2026 (H4c) — Baugrößen und Energiemengen auch für die Betriebskosten

**Anwenderbefund (10.09.2026).** Kostenverwaltung, Projekt 1050, Komponente Stromspeicher
(Gerät Growatt 100 kW / 129 kWh), Reiter „Kosten Invest/Betrieb": Die Betriebskostenzeile
*„Wartung / Sichtprüfung Speicher"* mit der Bemessung **„je kWh elektrisch"** und einem Satz von
1 €/kWh wies **0 €/a** aus. Erwartet war, dass ein Satz je kW bzw. je kWh mit Leistung bzw.
Kapazität des Speichers in €/a umgerechnet wird — *„analog zu Investitionskosten"* —, dazu die
Bitte: *„Prüfe bei anderen Kostenpositionen ebenfalls."*

**Die Ursache — zwei Lücken, nicht ein Rechenfehler.** Die Betriebsseite holt ihre Mengen seit
H2-1 genauso frisch wie die Investitionsseite (`IstRueckfallErmittelbareArt` →
`RueckfallMenge` → `TechnikPlanwertCtrl.BaugroesseSumme` bzw. `EndenergieAufloeser`); der
Rechenweg war also da. Es fehlten die **Einträge für das Gewerk Stromspeicher**: In der
Gerätewelt-Landkarte führte er nur seine Kapazität und keine Leistung, im Lauf-Auflöser gar
nichts. Beide Wege lieferten `null`, und über den **Anwenderentscheid I-2** („nicht rechenbar →
erfasster Wert gilt") galt der erfasste Betrag — bei einer satzbasierten Zeile ist das die 0.
Deshalb war auch nichts zu sehen: eine 0, die aus einer Regel stammt, sieht aus wie eine 0, die
niemand gepflegt hat.

**Die Regel (statt einer Liste).** Eine Kombination *Bemessungsart ↔ Gewerk* rechnet, wenn das
Gewerk **genau eine** Größe führt, die die Art meint. Daraus folgt beides: dass die Wärmepumpe
„je kW Leistung" ebenso trifft wie „je kW Heizleistung" (sie hat nur eine Nennleistung), und
dass das BHKW bei „je kW Leistung" bewusst ohne Bezugsgröße bleibt (`Pel` **und** `Ptherm` —
welche gemeint ist, sagt erst die qualifizierte Art). Die Zuordnung steht seither an einer
Stelle: `TechnikPlanwertCtrl.Geraetespalte`, gelesen von `BaugroesseSumme` und von der neuen
Frage `KenntBaugroesse`.

### Bemessung × Gewerk → Bezugsgröße/Quelle

„**neu**" = mit H4c ergänzt; „—" = bewusst ohne Bezugsgröße (I-2-Rückfall, der Dialog nennt den
Grund).

| Bemessung | WP (1) | Heizkessel (2) | Photovoltaik (3) | Solarthermie (4) | Stromspeicher (5) | Pufferspeicher (6) | BHKW (7) |
|---|---|---|---|---|---|---|---|
| je kW Heizleistung | `Tab_WP.Nennleistung` | `Tab_Heizkessel.Ptherm` **neu** | — | — | — | — | `Tab_BHKW.Ptherm` **neu** |
| je kW Leistung | `Tab_WP.Nennleistung` **neu** | `Tab_Heizkessel.Ptherm` | — | — | `Tab_Stromspeicher.Leistung` **neu** | — | — (zwei Leistungen) |
| je kW elektrisch | — | — | kWp über `PhotovoltaikCtrl.KwpSumme` **neu** | — | `Tab_Stromspeicher.Leistung` **neu** | — | `Tab_BHKW.Pel` |
| je kWp | — | — | kWp über `PhotovoltaikCtrl.KwpSumme` | — | — | — | — |
| je kWh Kapazität | — | — | — | — | `Tab_Stromspeicher.Energie` | — (ohne Temperaturpaar keine kWh) | — |
| je m² Kollektorfläche | — | — | — | `Aperturflaeche` × `Kollektormodulanzahl` | — | — | — |
| je kWh thermisch | Wärmeproduktion des Laufs | Wärmeproduktion des Laufs | — | Wärmeproduktion des Laufs | — | — | Wärmeproduktion des Laufs |
| je kWh elektrisch | Stromverbrauch + Heizstab | — | Stromproduktion des Laufs | — | `Tab_ErgebnisStromspeicher.Entladung_Gesamt` **neu** | — | Stromproduktion des Laufs |
| je Stunde | Betriebsstunden des Laufs | — | — | — | — | — | `VbhThermisch` (benannte Näherung, FX2) |
| % der Investition | Kaskadensumme | Kaskadensumme | Kaskadensumme | Kaskadensumme | Kaskadensumme | Kaskadensumme | Kaskadensumme |
| % der Endenergiekosten / des Endenergiebedarfs | Strom | Brennstoff | — | — | — | — | Brennstoff |
| je kWh · % der Brennstoff-/Stromkosten | Konserve | Konserve | Konserve | Konserve | Konserve | Konserve | Konserve |

„Kaskadensumme“ = die `InvestKaskade`, gestaffelt Anlage → Komponente → Projekt (W5‑B‑8). „Konserve“ = die Menge ist gepflegte Eingabe und wird bewusst nicht ermittelt (FX2, Befund B-4); dasselbe gilt für „% der Erzeugerkosten“, deren Basis die Hauptposition der Kaskade ist.

**Warum die entladene und nicht die geladene Energie.** Ein Satz „€ je kWh" bemisst sich an der
nutzbaren Arbeit des Speichers; `Ladung_Gesamt` enthält zusätzlich die Verluste (Fachkonzept
Stromspeicher 7.1: Ladung = Entladung + Verluste). Die Speicherzeile geht dabei **nicht** durch
`SummeKwh`: Sie trägt ihre Anlage als Schlüssel (`ID_Energieanlage`) statt als Bezeichner — die
Zuordnung ist also genauer als bei den Erzeugern —, und ihre Energien stehen in **kWh/a**, nicht
in MWh/a.

**Zwei Namen, eine Zahl.** „je kW elektrisch" an der Photovoltaik rechnet über denselben Kern wie
„je kWp" (`PhotovoltaikCtrl.KwpSumme`, Befund I-1) — kWp *ist* die elektrische Leistung. Ebenso
am Speicher: „je kW Leistung" und „je kW elektrisch" treffen dieselbe `Leistung`.

**Der Dialog sagt jetzt, WARUM.** Bleibt eine Zeile ohne Bezugsgröße, trägt sie einen
sprachneutralen Grund (`WirtschaftlichkeitCtrl.BASISGRUND_*`: `GEWERK`, `GERAET`, `LAUF`,
`INVEST`, `KONSERVE`); den Satz baut `KostenKomponenteHuelle.GrundText` (Drei-Schichten-Regel).
Im Werkzeugtipp des Betragsfelds steht dann „Keine Bezugsgröße: kein Simulationslauf. Es gilt der
erfasste Betrag." statt des Satzes einer gerechneten Zeile. Beim **Wechsel der Bemessung** zieht
der Dialog die Bezugsgröße frisch nach (`KostenProjektPositionenCtrl.BasisNachziehen` →
`WirtschaftlichkeitCtrl.FrischeBasis`, lesend, ohne die Konserve anzufassen) — vorher rechnete er
bis zum Speichern mit der Basis der alten Art weiter. Dass der Betrag dem **Satz** sofort folgt,
gilt unverändert seit W5‑B‑7 (`KostenKomponenteHuelle.Nachziehen`).

### Nachweise

| Probe | Ergebnis |
|---|---|
| Sandbox-Bau `K:\imp2\src`, `WP-Plan.sln` x64 Debug | **0 Fehler** (Warnprofil unverändert) |
| `EPOS.Kern.Tests` | **2 268 / 2 268 grün** |
| `EPOS.UI.Tests` | **3 358 / 3 360** — die zwei Ausfälle sind `RasterTests.Dieselbe_Zeilenmenge_wird_nur_einmal_gezaehlt` und `…Eine_stabile_Zeilenmenge_laesst_die_virtualisierte_Liste_zur_Ruhe_kommen` (W13‑B‑6, virtualisierte Liste — von H4c/Ä25 nicht berührt). Sie zählen Zeichenläufe und kippen unter Last, wenn ein zweiter Bau gleichzeitig läuft; allein nachgefahren: `RasterTests` **51 / 51 grün** |

Neue Fälle in `EPOS.Kern.Tests/BetriebskostenBaugroesseTests.cs` (synthetisches Projekt 190001 in
der Arbeitskopie: Speicher 100 kW / 129 kWh, PV 25 × 400 W, WP 12 kW, Solar 8 × 2,5 m², Kessel
40 kW):

| Fall | geprüfte Zahl |
|---|---|
| `Speicher_je_kWh_Kapazitaet_rechnet_mit_der_Kapazitaet` | 129 kWh × 2 €/kWh = **258,00 €/a** |
| `Speicher_je_kW_Leistung_rechnet_mit_der_Speicherleistung` | 100 kW × 3 €/kW = **300,00 €/a** |
| `Speicher_je_kW_elektrisch_trifft_dieselbe_Leistung` | 100 kW (dieselbe Größe wie „je kW Leistung") |
| `Speicher_je_kWh_elektrisch_rechnet_mit_der_entladenen_Jahresenergie` | 50 000 kWh × 0,01 €/kWh = **500,00 €/a** |
| `Speicher_je_kWh_elektrisch_ohne_Lauf_faellt_auf_den_erfassten_Wert` | Menge `null`, Betrag 0, Grund `LAUF` |
| `Photovoltaik_je_kWp_rechnet_mit_der_installierten_Leistung` | 25 × 400 W = 10 kWp × 15 €/kWp = **150,00 €/a** |
| `Photovoltaik_je_kW_elektrisch_trifft_dieselben_kWp` | 10 kWp |
| `Waermepumpe_je_kW_Heizleistung_rechnet_mit_der_Nennleistung` | 12 kW × 25 €/kW = **300,00 €/a** |
| `Solarthermie_je_Quadratmeter_rechnet_mit_der_Aperturflaeche` | 8 × 2,5 m² = 20 m² × 4 €/m² = **80,00 €/a** |
| `Heizkessel_je_kW_Leistung_rechnet_mit_Ptherm` | 40 kW × 5 €/kW = **200,00 €/a** |
| `Heizkessel_je_kW_Heizleistung_trifft_dasselbe_Ptherm` | 40 kW |
| `Unpassende_Kombinationen_bleiben_ohne_Bezugsgroesse` (4 Fälle) | BHKW/„je kW Leistung", Puffer/„je kWh Kapazität", WP/„je kW elektrisch", Solar/„je kWp" → Grund `GEWERK` |
| `Passende_Kombinationen_melden_das_fehlende_Geraet` (7 Fälle) | Grund `GERAET` statt `GEWERK` |
| `Die_Prozentarten_der_Kostenwelt_melden_fehlende_Investitionskosten` (2 Fälle) | „% der Investition“ und „% der Erzeugerkosten“ → Grund `INVEST` |
| `Die_Konservenarten_verweisen_auf_die_Pflege` (3 Fälle) | „je kWh“, „% der Brennstoffkosten“, „% der Stromkosten“ → Grund `KONSERVE` |
| `Absolute_Arten_tragen_keinen_Grund` | Betrag/Jahresbetrag/leer → kein Grund |
| `Der_Dialog_zeigt_Betrag_Basis_und_Grund_derselben_Rechnung` | Dialogzeile = Rechenkern (300,00 €/a, Basis 100 kW), Grund `LAUF` ohne Lauf |
| `Der_Wechsel_der_Bemessung_zieht_die_Bezugsgroesse_nach` | 129 kWh → 100 kW → `null` + Grund `GEWERK` |

**Regression.** Ein Projekt ohne größenbezogene Betriebskostenzeilen ist unberührt: Geändert hat
sich ausschließlich, welche Kombinationen Art↔Gewerk eine Menge FINDEN. Eine Zeile, die schon
vorher eine fand, findet dieselbe; eine absolute Zeile hat keine. Die Fälle der W5‑B‑8-Basis
(`BetriebskostenBasisTests`) und der Investitionskaskade (`InvestKaskadeTests`) laufen unverändert.

**Ausdrückliche Folge.** In Projekten, deren Zeilen eine der sieben neu zugeordneten Kombinationen
tragen, steigt der Betrag von 0 auf Menge × Satz — auf der Betriebs- **und** auf der
Investitionsseite (beide lesen dieselbe Landkarte). Genau das war der Auftrag; der Referenzlauf
ist nicht betroffen, weil er keine Kostenwerte führt.

### Offen

| Nr. | Punkt |
|---|---|
| H4c-1 | **Sichtabnahme** am Windows-Gerät: Projekt 1050, Stromspeicher, „je kWh elektrisch"/„je kW" — Betrag, Werkzeugtipp der Basis, Grundtext ohne Lauf |
| H4c-2 | „je kWh", „% der Brennstoffkosten", „% der Stromkosten" bleiben Konserve (FX2/B-4) — der Dialog sagt es jetzt, ermittelt aber weiter nichts |

## 4 Geänderte Dateien

```
Controller/BetriebskostenCtrl.cs                       InvestSumme + Anlagenfilter,
                                                        InvestSummeFuer, Spaltenprobe
Allgemein/Wirtschaftlichkeit/EndenergieAufloeser.cs    WaermeerzeugungKwh/StromgroesseKwh,
                                                        SummeKwh, Null-Lauf-Toleranz,
                                                        PV-/Solar-Komponentenkonstanten
Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs KomponenteUndAnlage (Refaktor),
                                                        Rueckfall-Weiche in beiden Lesepunkten
```

Harness `..\dev\h4a\` gehört nicht zum Lieferumfang.

**H4c (10.09.2026) — geänderte Dateien**

```
EPOS.Kern/Controller/TechnikPlanwertCtrl.cs                Geraetespalte (Regel statt Liste),
                                                            KenntBaugroesse, IstPvLeistungsart
EPOS.Kern/Allgemein/Wirtschaftlichkeit/EndenergieAufloeser.cs
                                                            KOMPONENTE_STROMSPEICHER,
                                                            SpeicherEntladungKwh
EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs
                                                            BASISGRUND_*, BasisGrund, FrischeBasis
EPOS.Kern/Controller/KostenProjektPositionenCtrl.cs        Zeile.BasisGrund, BasisNachziehen
WindowsFormsApplication1/Views/Kosten/KostenKomponenteHuelle.cs
                                                            GrundText, Werkzeugtipp ohne Basis,
                                                            frische Basis beim Bemessungswechsel
EPOS.Kern.Tests/BetriebskostenBaugroesseTests.cs           neu
```


# Konzept: Energieeinheiten in EPOS-Plan — Inventar, Bewertung kWh gegen MWh, Stufenplan

**Rev. 2 — 07.09.2026 — entschieden und Stufe S1 umgesetzt.** Der Anwender hat Q1 mit
**„Regel festschreiben"** und Q2…Q8 mit **„Empfehlung"** beantwortet (Kennung **W8‑O‑5c**,
Wortlaut in Kapitel 9.1). Damit ist die Einheitenregel Hausregel
(`EPOS.Kern/CLAUDE.md`, Abschnitt „Einheiten"), zwei Wächter halten sie, und **der
Rechenkern ist von der EINHEITENregel nicht umgebaut**: 12 von 12 Referenzprojekten und
312 von 312 CSV blieben byte-gleich zur damaligen Basis `2026-09-06_R3_Straenge`. **S2
entfällt, S3 bleibt bewusst liegen.**

**Nachtrag vom selben Tag:** Die TYPfrage ist getrennt und weiter entschieden worden —
**W8‑O‑5d, „alles in double"** (Kapitel 9.2). Sie ist **umgesetzt** und hat die Basis auf
`2026-09-07_R4_Double` gehoben; elf der zwölf Projekte weichen dort gewollt ab. Der
EINHEITENteil dieses Papiers bleibt davon unberührt.

*Rev. 1 (07.09.2026) war der Prüfbericht, der die Fragen stellte; Kapitel 1 bis 8 stehen
unverändert auf dem Stand von Commit `6839a7a`.*

Auftrag (Anwenderentscheid **W8‑O‑5b**, 07.09.2026, im Wortlaut):

> „Nehme die Umrechnung in den Dialogen vor. Prüfe, ob es nicht sinnvoll ist, die
> gesamten Berechnungen in kWh auszuführen und erst in der Anzeige/Dialogen
> umzurechnen. Prüfe in allen Berechnungen."

Nachtrag desselben Tages:

> „… oder Vereinheitlichen der Berechnung in MWh — je nach Sinnhaftigkeit — aber
> einheitlich."

Der erste Satz ist **umgesetzt** und trägt die Kennung W8‑O‑5b (Kapitel 9). Dieses
Papier beantwortet den zweiten und dritten: Es zählt, wo im Kern Energieeinheiten
umgerechnet werden, benennt die Stellen, an denen zwei Wege verschiedene Einheiten
führen, wägt kWh gegen MWh ab — mit einer gemessenen Gleitkommaprobe statt mit einem
Gefühl —, und legt einen Stufenplan mit Aufwand und Risiko vor. **Entschieden wird
nichts davon hier**; Kapitel 8 stellt die Fragen, Kapitel 9 trägt die Entscheide ein.

---

## 0. Das Ergebnis in fünf Sätzen

1. Der Kern rechnet die **Stundenreihen in kWh** und weist die **Jahressummen in MWh**
   aus — das ist die Regel, und sie steht nirgends geschrieben.
2. Die Umrechnung passiert an **242 Stellen**, verteilt über 43 Dateien; **127** davon
   sind wirklich kWh↔MWh, die übrigen 115 sind Leistung, Preis, Emission, Volumen,
   Vollbenutzungsstunden oder Kommentar.
3. **Genau dort, wo es darauf ankommt, fehlt die Einheit am Namen.** Die sechs
   Erzeuger- und Speicherklassen führen **31** Jahressummen als `…_gesamt` oder
   `…_summe` — und **keine einzige** trägt ihre Einheit im Namen, obwohl einige kWh
   und andere MWh führen. Wer eine davon benutzt, muss den Rechenweg lesen. Genau
   daraus ist W8‑O‑5b entstanden: EIN Feld mit ZWEI Einheiten, je nachdem, wer es
   gefüllt hatte.
4. **Die Gleitkommafrage ist keine Einheitenfrage.** `float` hat in jeder
   Größenordnung denselben RELATIVEN Abstand (2⁻²⁴ ≈ 6·10⁻⁸); kWh und MWh sind
   messbar gleich genau (Kapitel 4.3). Was Genauigkeit bringt, ist `double` — nicht
   die Einheit.
5. **Empfohlen wird MWh als die eine ausgewiesene Einheit und kWh als die eine
   Reihen-Einheit** — also die Regel, die der Bestand ohnehin fast überall befolgt,
   nur ausgesprochen, an den Namen gebunden und von einem Wächter gehalten. Ein
   vollständiger Wechsel des Rechenkerns auf EINE Einheit für alles kostet eine neue
   Referenzbasis R4 und bringt fachlich nichts (Kapitel 5).

---

## 1. Inventar

### 1.1 Was die Zählung des Auftrags ergibt — und was sie übersieht

Der Auftrag nennt **217 Stellen** aus

```
grep -rnE "/ ?1000(\.0)?f?\b|\* ?1000(\.0)?f?\b" EPOS.Kern --include=*.cs
```

Nach dem Commit zu W8‑O‑5b sind es **216** (die doppelte Brauchwasserzeile ist eine
Methode geworden). **Diese Zählung ist eine Untergrenze**: Sie kennt nur EINE
Schreibweise. Vier weitere kommen im Bestand vor und bringen **26** Stellen dazu:

| Schreibweise | Stellen | wo |
|---|---:|---|
| `/ 1000`, `* 1000` (die Zählung des Auftrags) | 216 | überall |
| `/= 1000`, `*= 1000` | 12 | `SimulationSPK` (9), `SimulationBHKW` (2), `SimulationControl` (1) |
| `/ 4000` (Viertelstunden → MWh) | 10 | `SimulationStrombedarf` (4), `SimulationControl` (2), `SimulationRunner` (2), `SimulationErgebnisCtrl` (2) |
| `* 0.001` bzw. `0.001 *` | 3 | `BhkwPlan` (3 — `WattToKw`, `VectorSumme`, `MonatsSumme`) |
| `probe[h] *= 0.001` | 1 | `SimulationWaermebedarf:223` |
| **Summe** | **242** | **43 Dateien** |

Die drei `BhkwPlan`-Stellen sind die schwerwiegendsten der Nachzügler: `MonatsSumme`
und `VectorSumme` sind die Routinen, mit denen **jede** Monatsreihe und **jede**
Jahressumme des Bestands aus einer Stundenreihe entsteht. Wer nur nach `/ 1000` sucht,
findet die zentrale Umrechnung des Programms nicht.

> **Merksatz für den Wächter aus Stufe S1:** Eine Suche nach `1000` reicht nicht. Der
> Wächter muss `/ 1000`, `* 1000`, `/= 1000`, `*= 1000`, `/ 4000`, `0.001` und `1e-3`
> kennen.

### 1.2 Die 242 Stellen nach Art

| Art | Stellen | Bedeutung |
|---|---:|---|
| **E — Energie kWh ↔ MWh** | **127** | die Frage dieses Papiers |
| L — Leistung / Einstrahlung W ↔ kW | 28 | PV-Module (`m_Leistung` [W]), Wechselrichter (`Paco` [W]), Einstrahlung G [W/m²] → G′ [kW/m²], `WattToKw` |
| M — Emission g/kWh, mg/kWh → t bzw. kg | 24 | `EmissionsBilanzRechner` (8), `SimulationSPK` (5), `SimulationBHKW` (5), `KostenEmissionRechner` (4), `EmissionsModelle` (1), `WirtschaftlichkeitCtrl` (1) |
| P — Preis €/kWh, ct/kWh auf eine MWh-Menge | 20 | `WirtschaftlichkeitCtrl` (10), `StromMatrix` (3), `KostenEmissionRechner` (2), `PvKennzahlenRechner` (2), `StromTarifRechner` (2), `EndenergieAufloeser` (1) |
| Z — Vollbenutzungsstunden (MWh·1000 / kW = h) | 8 | `SimulationBHKW` (3), `WirtschaftlichkeitCtrl` (3), `GebaeudeBedarfCtrl` (1), `GanglinienAuswertungCtrl` (1) |
| V — Volumen / Konstante (Liter → m³, 1,16 Wh/(l·K)) | 5 | `SimulationPufferspeicher` (3), `ProjektPuffer` (1), `WaermesenkeClass` (1) |
| K — Kommentar, Dokumentation | 30 | keine Rechnung |

**Nur die 127 E-Stellen stehen zur Debatte.** Die übrigen 115 wären von einer
Vereinheitlichung nicht betroffen: Ein Modul, das 400 W leistet, bleibt 0,4 kW, egal
in welcher Einheit die Jahresarbeit steht.

### 1.3 Die 242 Stellen je Rechenstufe

| Rechenstufe | E | L | P | M | V | Z | K | Σ |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Bedarf und Profile | 23 | 1 | 0 | 0 | 0 | 2 | 4 | 30 |
| Simulation je Erzeuger und Speicher | 11 | 17 | 0 | 10 | 5 | 3 | 12 | 58 |
| Ergebnis-DTO (Anzeige) | 39 | 0 | 0 | 0 | 0 | 0 | 4 | 43 |
| Ergebnistabellen `Tab_Ergebnis*` | 34 | 0 | 0 | 0 | 0 | 0 | 1 | 35 |
| Wirtschaftlichkeit | 14 | 1 | 18 | 1 | 0 | 3 | 0 | 37 |
| Emissionen und Kosten | 4 | 0 | 2 | 13 | 0 | 0 | 3 | 22 |
| Bericht, Kennzahlen, Kataloge | 2 | 9 | 0 | 0 | 0 | 0 | 6 | 17 |
| **Summe** | **127** | **28** | **20** | **24** | **5** | **8** | **30** | **242** |

**Das Bild ist eindeutig: 68 der 127 Energie-Umrechnungen (54 %) stehen in genau ZWEI
Dateien** — `SimulationErgebnisCtrl` (34) und `SimulationRunner` (34). Beide tun
dasselbe: Sie nehmen kWh aus den Rechenobjekten und liefern MWh — die eine an die
Anzeige, die andere in die Datenbank. **Das ist keine verstreute Unordnung, sondern
EINE Naht, die zweimal gebaut wurde.**

### 1.4 Die Einheit je Rechenstufe

| Stufe | Stundenreihen | Jahressummen im Rechenobjekt | Gespeichert | Angezeigt |
|---|---|---|---|---|
| **Profilkataloge** (`Tab_Brauchwasser`, `Tab_Prozesswaerme`, `Tab_Stromverbraucher`) | — | zwölf Monatswerte je Profil in **MWh** | **MWh** | MWh/kWh |
| **Ganglinien** (importierte Zeitreihen) | **kWh** bzw. **kW** je Intervall | **MWh** (`/ 1000`, `/ 4000`) | **kWh** | MWh/kWh |
| **Wärmebedarf** (`SimulationWaermebedarf`) | **kWh**, `float[8760]` (11 Reihen) | **MWh** (`Waermebedarf_Gesamt`, `…_Prozess`, `…_Brauchwasser`, `…_Gebaeude_Gesamt`, `…_Extern_Gesamt`, `…_Netzverluste`) | `Tab_ErgebnisEnergiebedarf` **MWh** | MWh (Vorgabe) oder kWh, `Energieeinheit` |
| Monatsreihen | **MWh** (`BhkwPlan.MonatsSumme` skaliert mit 0,001) | — | — | MWh/kWh |
| **Strombedarf** (`SimulationStrombedarf`) | **kW**, `float[35040]` Viertelstunden | **MWh** (`/ 4000`) | `Tab_ErgebnisEnergiebedarf` **MWh** | MWh/kWh |
| **Wärmepumpe** | **kWh**, 7 Reihen | **kWh** (`WP_Waermeproduktion_gesamt`, `WP_Strombedarf_gesamt`, `Heizstab_gesamt`, `waermerestbedarf_gesamt`) | — | — |
| **Spitzenkessel** (`SimulationSPK`) | **kWh**, 6 Reihen | **gemischt**: `Waermebedarf_gesamt` MWh (`/= 1000` in `:1115`), `Strombedarf_gesamt` **kWh**, `S_Waerme_spk`/`Stromverbrauch_Spk`/`BruttoWaermeSpkErzeugung`/die zehn Brennstoffzähler **MWh**, `Quellwaerme_gesamt`/`Speicherladung_gesamt`/`Speicherentladung_Anteil` **kWh** | — | — |
| **BHKW** | **kWh**, 6 Reihen | **gemischt**: `Waermebedarf_gesamt`, `strombedarf.Sum()`, `Waermeueberschuss`, `Direktdeckung_gesamt`, `Speicherladung_gesamt` **kWh**; `s_waerme_MWh[]`, `s_strom_MWh[]`, `Waermeproduktion_BHKW_MWh`, `Stromproduktion_BHKW_MWh` **MWh** | — | — |
| **Solarthermie** | **kWh**, Reihen | **kWh** (`Waermeproduktion_gesamt`, `Ueberschuss_summe`) | — | — |
| **Photovoltaik** | **kWh** Stunden, **kW** Viertelstunden | **kWh** (`Stromproduktion.Sum()`, `Ueberschuss.Sum()`) | — | — |
| **Pufferspeicher** | **kWh**, 7 Reihen | **kWh** (`Q_max`, `Ladung_gesamt`, …) | `Tab_ErgebnisPufferspeicher` **kWh** | kWh |
| **Stromspeicher** | **kWh** Viertelstunden | **kWh** (`EntladeenergieKwh`) | `Tab_ErgebnisStromspeicher` **kWh** | kWh |
| **`SimulationControl`** | Restreihen **kWh** bzw. **kW** | `Restwaerme`, `Reststrom`: **MWh**, und zwar als **`float`** | `Tab_Ergebnis*` **MWh** | MWh |
| **Ergebnis-DTO** (`SimulationErgebnisDaten`) | — | **MWh**, Suffix `…Mwh` am Namen | — | MWh |
| **`Tab_Ergebnis*` (17 Tabellen)** | — | — | **MWh**, außer den zwei Speichertabellen (kWh) | — |
| **Wirtschaftlichkeit** | — | Eingaben **MWh**, Preise **€/kWh** und **ct/kWh** | `Tab_ErgebnisWirtschaftlichkeit` **€** | € |
| **Emissionen** | — | Mengen **MWh**, Faktoren **g/kWh** bzw. **mg/kWh** | **t** bzw. **kg** | t, kg, g/kWh |
| **Bericht / `KennzahlenKatalog`** | — | — | — | **MWh/a** durchgängig, Emissionskennzahl **g/kWh** |
| **Referenzlauf-CSV** | Vektordateien **kWh** (bzw. kW bei Viertelstunden) | `Vektor.*.Summe` **kWh**, `Sim.Restwaerme`/`Sim.Reststrom` **MWh**, `Ergebnis*.…` **MWh** | — | — |

**Die Regel, die man daraus liest:** *Reihen in kWh, Ausweisungen in MWh, Speicher in
kWh.* Sie gilt für rund 90 % des Bestands. Die Ausnahmen stehen im nächsten Kapitel.

---

## 2. Unstimmigkeiten

Eine Unstimmigkeit ist hier: **zwei Wege führen für dieselbe Größe verschiedene
Einheiten**, oder **eine Umrechnung findet außerhalb der Anzeige statt, wo die Anzeige
sie erwarten würde**.

> **Stand nach Stufe S1 (W8‑O‑5c, 07.09.2026).** Sechs der acht sind erledigt:
>
> | Nr. | Stand | Wodurch |
> |---|---|---|
> | U1 | **behoben** | W8‑O‑5b, Commit `6839a7a` |
> | U2 | **offen, harmlos** | Zwei Rundungen derselben Größe, Abstand jetzt ≤ 1 ULP `double`. Die Berichtigung säße in `SimulationWaermebedarf:327` und damit im Rechenweg. **W8‑O‑5d hat die neue Basis gebracht, den Punkt aber bewusst NICHT mitgenommen** — der Auftrag lautete „nur der Typ, keine Gelegenheitsverbesserung". Der Abstand ist mit `double` um sieben Größenordnungen kleiner geworden; ein eigener Entscheid kann ihn jederzeit schließen |
> | U3 | **entschärft** | Die zwei Teiler bleiben (sie sind beide richtig: Viertelstunden- gegen Stundenraster), aber das Feld heißt jetzt `StrombedarfGesamtMwh` und nennt damit die Einheit, um die es ging. Welches RASTER die Reihe hat, sagt weiterhin `Stuetzstellen` |
> | U4 | **behoben** | S1.3: `SimulationSPK` führt `StrombedarfGesamtKwh` neben `StromverbrauchSpkMwh`, `SWaermeSpkMwh`, `WaermebedarfGesamtMwh`, `QuellwaermeGesamtKwh`, `SpeicherladungGesamtKwh`, `BruttoWaermeSpkErzeugungMwh` — jede Jahressumme mit ihrer Einheit am Namen |
> | U5 | **behoben** | S1.2: Torte und Stromring lesen dieselben `…Mwh`-Felder wie der Wärmering — EINE Konvention statt drei |
> | U6 | **behoben** | S1.2: Die elf Energie-Umrechnungen sind aus der Hülle heraus; Wächter 1 hält die Grenze |
> | U7 | **behoben** | Q8: Der Kommentar an `Strombedarf_Max` nennt kW |
> | U8 | **behoben** | Die Felder heißen `RestwaermeMwh`/`ReststromMwh` und nennen ihre Einheit; seit **W8‑O‑5d** (07.09.2026) sind sie `double` |

### U1 — Brauchwasser: ein Feld, zwei Einheiten *(behoben, W8‑O‑5b)*

`SimulationWaermebedarf.Waermebedarf_Brauchwasser` stand im Lauf in MWh
(`Waermebedarf_berechnen`, Teiler 1000) und in der Vorschau in kWh
(`BedarfsVorschauCtrl`, nackte Summe). Die Ergebnishülle konnte nur EINE Angabe
glauben; sie glaubte kWh und teilte den Wert des Laufs ein zweites Mal —
*Simulation → „Wärmebedarf-Details"* zeigte den Brauchwasserbedarf um den Faktor 1000
zu klein. **Behoben:** beide Wege gehen über
`SimulationWaermebedarf.BrauchwassersummeUebernehmen()`.

### U2 — Prozesswärme: dieselbe Größe, zwei Ausdrücke *(offen, harmlos)*

Genau daneben steht der geheilte Fall aus **W9‑O‑3** — und er ist nur halb geheilt:

| Weg | Ausdruck | Arithmetik |
|---|---|---|
| Lauf (`SimulationWaermebedarf:327`) | `Waermebedarf_Prozess = prozesswerte.Sum() / 1000;` | `float / int` → **float-Division** |
| Vorschau (`ProzesssummeUebernehmen`) | `Waermebedarf_Prozess = Energieeinheit.MWh.AusKWh(prozesswerte.Sum());` | `double / double` → **double-Division** |

Dieselbe Größe, dieselbe Einheit, **zwei Rundungen**. Der Unterschied liegt bei
höchstens 1 ULP von `float` (≈ 6·10⁻⁸ relativ) und fällt in keiner Anzeige auf; er
gehört trotzdem in die Liste, weil er dieselbe Bauart hat wie U1. **Warum er in
W8‑O‑5b nicht mitbehoben wurde:** `Waermebedarf_Prozess` ist — anders als das
Brauchwasserfeld — keine reine Anzeigegröße, und eine Änderung an `:327` müsste gegen
den Referenzlauf gehalten werden. Das ist Stufe S2.

### U3 — Strombedarf: ein Feld, zwei Teiler

`SimulationStrombedarf.Strombedarf_Gebaeude_gesamt` wird gefüllt

* im Lauf (`:122`) mit `prozesswerte.Sum() / 4000` — die Reihe steht in **Viertelstunden**,
* in der Vorschau (`ProfilbedarfUebernehmen`, `:335`) mit `stundenreihe.Sum() / 1000` — die Reihe steht in **Stunden**.

Beides ist richtig, und beides ergibt MWh. Aber welcher Teiler gilt, hängt daran, wer
die Reihe gefüllt hat — dieselbe Falle wie U1, nur diesmal richtig gestellt. **Der
Feldname sagt es nicht**, und `Stuetzstellen` (das Feld, das das Raster nennt) muss man
kennen.

### U4 — `SimulationSPK`: kWh und MWh in derselben Klasse, ohne Namensunterschied

```
public double Strombedarf_gesamt = 0;   // kWh  (Strombedarf_stuendlich.Sum())
public double Stromverbrauch_Spk = 0;   // MWh  (Summe der Kessel-Nutzarbeit)
```

Beide Felder sind `double`, beide heißen nach Strom, beide sind Jahressummen — und sie
stehen in verschiedenen Einheiten. Wer sie addiert, muss das wissen; die zwei Aufrufer
tun es und schreiben

```
SimulationErgebnisCtrl:409   e.ReststrombedarfMwh = spk.Strombedarf_gesamt / 1000.0 + spk.Stromverbrauch_Spk;
SimulationRunner:703         h.Reststrombedarf   = spk.Strombedarf_gesamt / 1000.0 + spk.Stromverbrauch_Spk;
```

Ein halber Teiler in einer Summe ist genau das Bild, das W8‑O‑5b erzeugt hat. Dasselbe
gilt für `S_Waerme_spk` (MWh) neben `Speicherladung_gesamt` (kWh) in
`SimulationRunner:253`.

### U5 — Die Ringdiagramme der Ergebnisseite mischen drei Konventionen

`WindowsFormsApplication1/Views/Simulation/SimulationErgebnisHuelle.Bilder.cs:207‑215`
baut EIN Ringdiagramm aus vier Erzeugern:

```
Wärmepumpe   sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000.0   (kWh, hier geteilt)
Heizstab     sim.simulation_wp.Heizstab_gesamt / 1000.0              (kWh, hier geteilt)
Heizkessel   sim.simulation_spk.S_Waerme_spk                         (schon MWh)
BHKW         sim.simulation_bhkw.Waermeproduktion_BHKW_MWh           (schon MWh)
Rest         sim.Restwaerme                                          (schon MWh, float)
```

**Vier Erzeuger, drei Konventionen, ein Bild.** Es stimmt heute; es stimmt, weil
jemand jede der fünf Größen einzeln nachgesehen hat. **Das ist die eigentliche
Kostenstelle des Bestands** — nicht die Rechenzeit, sondern die Prüfzeit bei jeder
Änderung.

### U6 — Elf Energie-Umrechnungen stehen noch in der Windows-Hülle

Die Regel aus W8‑O‑5 („umgerechnet wird an der Anzeige, und die Übergabe trägt ihre
Einheit") ist an den Bedarfsdialogen umgesetzt, an der Ergebnisseite noch nicht:

| Datei | Stellen | Art |
|---|---:|---|
| `Views/Simulation/SimulationErgebnisHuelle.Anzeige.cs` | 5 | Energie (Strombedarf-Ring, Eigenanteil je Kanal) |
| `Views/Simulation/SimulationErgebnisHuelle.Bilder.cs` | 4 | Energie (zwei Ringdiagramme) |
| `Views/Simulation/SimulationKonfigHuelle.cs` | 3 | Leistung W→kWp (unkritisch) |
| `Views/Varianten/EnergieMengen.cs` | 1 | Energie (MWh → kWh vor der Heizwertdivision) |
| `Views/Photovoltaik/PhotovoltaikHuelle.cs` | 1 | Leistung W→kWp (unkritisch) |
| `EPOS.UI/Dialoge/Wirtschaftlichkeit/PhotovoltaikVerguetungDialog.razor` | 1 | Energie |

**Elf Energie-Umrechnungen jenseits der Kerngrenze.** Sie sind der Rest von U5: Die
Hülle greift an diesen Stellen am DTO vorbei unmittelbar auf `sim.simulation_*` zu.
`SimulationErgebnisCtrl` hat für dieselben Größen längst `…Mwh`-Felder.

### U7 — Ein Kommentar, der die Einheit falsch nennt

`SimulationStrombedarf:178`:

```
Strombedarf_Max = Maximaler_Strombedarf(Strombedarf_viertelStundenwerte); // in kWh
```

Der Wert ist eine **Leistung in kW** — die Anzeige nennt ihn seit dem Anwenderwunsch
W8‑E‑2 richtig „max. Leistung [kW]". Der Kommentar ist seit dem Bestand falsch. Kein
Rechenfehler, aber ein Beleg dafür, dass Kommentare die Einheit nicht halten können.

### U8 — `Restwaerme` und `Reststrom` sind `float`-Jahressummen in MWh

`SimulationControl:175‑176` führt beide als `float`. Sie gehen in `Tab_Ergebnis`, in
die Kennzahlen, in den Bericht und als `Sim.Restwaerme` / `Sim.Reststrom` in die
`aggregate.csv` der Referenzbasis. Eine Jahressumme in `float` trägt sieben
Stellen — bei 6,054 MWh reicht das, bei 6 054 MWh auch, aber der Wert wird danach
gegen eine `double`-Bilanz gehalten. Das ist die Nebenfrage Q5.

---

## 3. Was der Auftrag mit „kWh in den Berechnungen" meinen könnte

Drei Lesarten, die sich sehr unterschiedlich auswirken:

| Lesart | Was sich ändert | Referenzlauf |
|---|---|---|
| **A — Nur die Anzeigegrenze härten.** Reihen bleiben kWh, Ausweisungen bleiben MWh; jede Übergabe an Dialog, Bericht und Datenbank trägt ihre Einheit im Namen oder im DTO, ein Wächter hält es. | ~15 Feldnamen, 11 Hüllen-Stellen, ein Wächter | **unberührt** |
| **B — Eine Recheneinheit im Kern (kWh), MWh nur an der Anzeige.** Alle Jahressummen der Rechenobjekte gehen auf kWh; die Teilung geschieht einmal, in `SimulationErgebnisCtrl` und `SimulationRunner`. | ~60 Felder, 68 Stellen in zwei Dateien, alle Aufrufer | **ändert sich** (Reihenfolge der Divisionen) |
| **C — Eine Recheneinheit im Kern (MWh).** Auch die 59 Stundenreihen gehen auf MWh. | ~60 Felder **und** 59 `float[8760]`, jede Kaskadenzeile, jedes Speichermodell | **ändert sich stark** |

Lesart C ist die einzige, die den Faktor 1000 wirklich aus dem Kern entfernt — und die
einzige, die den Rechenweg selbst anfasst. Kapitel 4 und 5 bewerten sie.

---

## 4. Bewertung: kWh gegen MWh als EINE Recheneinheit

### 4.1 Was für kWh spricht

1. **Die Stundenbasis ist kWh.** Eine Stundenreihe trägt eine Leistung in kW; über
   eine Stunde integriert ist das kWh **ohne Faktor**. Jede Kaskadenzeile, jede
   Speicherbilanz, jede Ladeentscheidung rechnet heute kW gegen kWh — in MWh stünde
   in jeder dieser Zeilen ein 0,001.
2. **Die zwei Speichertabellen sind kWh.** `Tab_ErgebnisPufferspeicher` und
   `Tab_ErgebnisStromspeicher` führen kWh (belegt an
   `ErgebnisPufferspeicherModel` und `StromspeicherSimCtrl:1434‑1440`), und die
   Speicherengine rechnet durchgängig in kWh.
3. **Die importierten Ganglinien sind kWh** (bzw. kW je Intervall) — der einzige
   Bestandsdatenstrom, der schon in der Einheit der Stundenreihe hereinkommt.
4. **Die Referenzlauf-Vektoren sind kWh** — 300 der 312 CSV-Dateien der Basis.

### 4.2 Was für MWh spricht

1. **Der Bericht ist MWh.** `KennzahlenKatalog` führt an **15** Stellen die Einheit
   `MWh/a`, Format `N0` — jede Energiekennzahl des Berichts. Eine Umstellung auf kWh
   würde jede Berichtszeile ändern.
2. **15 der 17 `Tab_Ergebnis*`-Tabellen führen MWh.** Eine Umstellung bräuchte einen
   Migrationsschritt für alle Bestandsergebnisse — oder eine Umrechnung an der
   Schreibgrenze, also genau das, was man vermeiden wollte.
3. **Die Wirtschaftlichkeit rechnet auf MWh-Mengen.** Alle 20 Preisstellen haben die
   Form `mengeMWh * 1000.0 * preisEurKWh`. In kWh fiele der Faktor weg — aber die
   Preise stehen in €/kWh und ct/kWh, weil das die Einheit der Verträge ist. Der
   Faktor verschwindet nicht, er wandert.
4. **Die Emissionsfaktoren sind g/kWh und mg/kWh.** `verbrauchMWh * faktorGJeKWh / 1000`
   ergibt Tonnen. In kWh wäre es `verbrauchKwh * faktorGJeKWh / 1e6` — derselbe
   Faktor, nur größer.
5. **Die eingefrorene Referenzbasis ist MWh in den Skalaren.** Jede Änderung an einer
   Jahressumme ändert `aggregate.csv`.
6. **Die Profilkataloge sind MWh** — und das ist der überraschendste Befund dieses
   Papiers. `Tab_Brauchwasser`, `Tab_Prozesswaerme` und `Tab_Stromverbraucher` führen
   ihre zwölf Monatswerte je Profil in **MWh**; die Formeln (5) der drei
   Berechnungsseiten (`Brauchwasser.wiki`, `Prozesswärme.wiki`, `Strombedarf.wiki`)
   multiplizieren an der Profilgrenze mit 1 000, um auf die Stundenreihe zu kommen.
   Der Bedarf betritt den Kern also in MWh, wird für die Reihe auf kWh gebracht und
   für die Ausweisung wieder auf MWh geteilt. **In einem reinen MWh-Kern (Lesart C)
   fiele dieser Faktor ersatzlos weg** — er ist der einzige, für den das gilt.
7. **Der Anwender denkt in MWh.** Die Vorgabe der Einheitenwahl ist MWh (W8‑O‑5), und
   die Anzeige zeigt zwei Nachkommastellen — 594,30 MWh liest sich, 594 300 kWh nicht.

### 4.3 Die Gleitkommafrage — gemessen, nicht geschätzt

Der Auftrag fragt konkret: *„Der Kern rechnet vielfach in `float` — bringt kWh statt
MWh mehr oder weniger Rundungsfehler (Größenordnung 1e3…1e7 je Jahr)?"*

Die Probe rechnet ein synthetisches Jahresprofil (8 760 Stunden, Jahresgang und
Tagesgang) in drei Jahresmengen und summiert es viermal: als `float`-Reihe in kWh und
in MWh, je einmal mit `float`-Akkumulator (die Bauart von `BhkwPlan.VectorSumme` und
`MonatsSumme`) und einmal mit `Enumerable.Sum` (`double`-Akkumulator, `float`-Ergebnis).
Referenz ist die `double`-Summe.

**Erste Messung — die Auflösung von `float`:**

| Wert | ULP (Abstand zur nächsten `float`-Zahl) | relativ |
|---:|---:|---:|
| 0,001 | 1,164·10⁻¹⁰ | 1,164·10⁻⁷ |
| 1 | 1,192·10⁻⁷ | 1,192·10⁻⁷ |
| 50 | 3,815·10⁻⁶ | 7,629·10⁻⁸ |
| 1 000 | 6,104·10⁻⁵ | 6,104·10⁻⁸ |
| 100 000 | 7,812·10⁻³ | 7,812·10⁻⁸ |
| 1 000 000 | 6,250·10⁻² | 6,250·10⁻⁸ |
| 10 000 000 | 1,000 | 1,000·10⁻⁷ |

**Der RELATIVE Abstand ist in jeder Größenordnung derselbe** (2⁻²⁴ = 5,96·10⁻⁸, je nach
Lage in der Mantisse bis 1,2·10⁻⁷). Das ist der Kern der Antwort: `float` ist ein
GLEITkommaformat — es hat keine feste Nachkommastelle, die man durch einen
Einheitenwechsel gewinnen könnte.

**Zweite Messung — die Jahressumme:**

| Jahresmenge | (a) kWh, `float`-Schleife | (b) MWh, `float`-Schleife | (a) kWh, `Enumerable.Sum` | (b) MWh, `Enumerable.Sum` |
|---:|---:|---:|---:|---:|
| 30 MWh | 1,17·10⁻⁶ | 3,82·10⁻⁷ | 3,6·10⁻¹⁶ | 3,6·10⁻¹⁶ |
| 1 000 MWh | 6,25·10⁻⁷ | 8,55·10⁻⁷ | 8,1·10⁻¹⁶ | 8,1·10⁻¹⁶ |
| 10 000 MWh | 3,00·10⁻⁷ | 9,77·10⁻⁸ | 3,4·10⁻¹⁵ | 3,4·10⁻¹⁵ |

**kWh ist einmal besser, einmal schlechter, einmal gleich — das ist Rauschen, kein
Trend.** Was um **neun Größenordnungen** hilft, ist der `double`-Akkumulator: 10⁻¹⁵
statt 10⁻⁶.

**Dritte Messung — der Mischfall.** Eine kleine Größe auf eine große addiert:

```
kWh:  1 000 000    + 0,05      = 1000000,06   (nichts verloren)
MWh:      1 000    + 0,00005   = 1000,00006   (nichts verloren)
```

Gleich, weil das VERHÄLTNIS gleich ist. Die Einheit verschiebt nichts.

**Vierte Messung — der Fall aus W8‑O‑5b.** Vorschau und Lauf verteilen dieselbe
Brauchwasser-Jahresmenge nach verschiedenen Wochentagskonventionen (F3) auf die 8 760
Stunden. Gemessen: 4 059,700 68 kWh gegen 4 059,700 44 kWh — **1 ULP, 6,0·10⁻⁸
relativ**. In MWh: 4,059 700 68 gegen 4,059 700 44 — **derselbe relative Abstand**.
`double` löst ihn auf, MWh nicht.

> **Antwort auf die Frage des Auftrags: Nein.** kWh bringt gegenüber MWh weder mehr
> noch weniger Rundungsfehler. Die Größenordnung 10³…10⁷ ist für `float` irrelevant,
> weil `float` relativ und nicht absolut rundet. **Die Gleitkommafrage ist eine
> `float`-gegen-`double`-Frage und keine Einheitenfrage** — und sie hat mit Q5 ihren
> eigenen Platz.

**Nebenbefund mit Gewicht:** `BhkwPlan.VectorSumme` und `BhkwPlan.MonatsSumme`
akkumulieren **absichtlich in `float`** — sie bilden das Verhalten der abgelösten
Original-DLL nach (Kommentar an der Methode: „Die DLL akkumuliert in einer
`float`-Speicherzelle … Das wird hier bewusst nachgebildet"). Genau diese beiden
Routinen liefern nach der Messung den Fehler 10⁻⁶ statt 10⁻¹⁵. Das ist der einzige
Ort im Bestand, an dem die Zahlengenauigkeit wirklich verliert — und er hat mit der
Einheit nichts zu tun.

---

## 5. Empfehlung

**Empfohlen wird Lesart A aus Kapitel 3, ausgesprochen als HAUSREGEL, plus die Naht
aus U6:**

> **Die Einheitenregel des Rechenkerns.**
> 1. **Zeitreihen führen kWh** (Viertelstundenreihen kW), immer, ohne Ausnahme.
> 2. **Jahres- und Monatssummen, die den Kern VERLASSEN, führen MWh** — an Anzeige,
>    Bericht, Datenbank und CSV.
> 3. **Jede Größe, die den Kern verlässt, trägt ihre Einheit im NAMEN**
>    (`…Mwh`, `…Kwh`, `…Kw`) oder im DTO (`Energieeinheit` am Wert).
> 4. **Umgerechnet wird an genau ZWEI Nähten:** `SimulationErgebnisCtrl` (Anzeige) und
>    `SimulationRunner` (Datenbank). Außerhalb dieser beiden Dateien und der
>    `Energieeinheit` steht in `EPOS.UI` und in den Windows-Hüllen **kein** Faktor 1000
>    auf einer Energiemenge.
> 5. **Innerhalb eines Rechenobjekts gilt kWh** — auch für Jahressummen. Wo heute MWh
>    steht (`S_Waerme_spk`, `Waermeproduktion_BHKW_MWh`, `Restwaerme`, …), bekommt das
>    Feld ein `Mwh` im Namen und bleibt, bis Stufe S2 es umstellt.

**Die drei stärksten Gründe gegen eine vollständige Vereinheitlichung (Lesart B/C):**

1. **Sie löst das Problem nicht.** Der Fehler aus W8‑O‑5b entstand nicht daran, dass
   es zwei Einheiten gibt, sondern daran, dass ein Feld sie **nicht nannte**. Ein
   Kern, der nur kWh führt, hat immer noch Preise in €/kWh, Emissionsfaktoren in
   g/kWh, Leistungen in W und Volumina in Litern — 115 der 242 Stellen bleiben
   unberührt. Der Namensteil der Regel bringt den Nutzen; die Einheit ist Beiwerk.
2. **Sie kostet die Referenzbasis.** Die Divisionsreihenfolge zu ändern, ändert die
   letzten Bits jeder Jahressumme (Kapitel 6). Eine neue Basis R4 ist ein
   Anwenderentscheid, kein Nebeneffekt — und sie kostet die Aussagekraft aller
   Vergleiche, die heute gegen R3 laufen.
3. **Sie bringt keinen Rechenvorteil.** Kapitel 4.3 belegt: gleiche Genauigkeit. Und
   Rechenzeit auch nicht — 242 Multiplikationen im Jahr sind nichts.

**Die Grenze der Umrechnung** (die Frage des Auftrags): **An der Anzeige, und nur
dort** — Dialoge über `Energieeinheit`, Bericht über die `Einheit`-Spalte des
`KennzahlenKatalog`. **Der CSV-Export der Referenzläufe bleibt, wie er ist** (Q3):
Er ist ein Regressionsnetz, kein Anwenderdokument; seine Einheit muss stabil sein,
nicht schön.

---

## 6. Auswirkung auf den Referenzlauf

### 6.1 Was sich bei Lesart A ändert

**Nichts.** Der Commit zu W8‑O‑5b ist der Beweis: Er ändert einen Anzeigeweg, eine
Kernmethode und zwei Vorschauzeilen — und die vier Projekte 1030/1007/1017/1045 sind
gegen `Referenzlaeufe/2026-09-06_R3_Straenge` **byte-gleich** geblieben
(`diff -rq` ohne Unterschied, Toleranzvergleich `GESAMT: PASS`, 1 121 807 Werte).

### 6.2 Was sich bei Lesart B/C ändern würde

| Betroffen | Wie | Toleranzregel iF15 |
|---|---|---|
| **Die 300 Vektordateien** (Stundenreihen) | Lesart B: **unberührt** (Reihen bleiben kWh). Lesart C: **jede Zeile ändert sich** — die Reihe steht dann in MWh, also um Faktor 1000 kleiner | C: **FAIL** (Faktor 1000 ist keine Toleranz) |
| **`Vektor.*.Summe`** in `aggregate.csv` | Lesart B: unberührt. Lesart C: Faktor 1000 | C: **FAIL** |
| **Die Skalare aus `Tab_Ergebnis*`** | Beide Lesarten: die Division wandert; das Ergebnis ändert sich in den letzten Bits (≈ 10⁻⁷ relativ, siehe 4.3) | **PASS** — 10⁻⁷ liegt weit unter der Regel „rel. 1e‑4 ab Betrag 1, sonst abs. 0,01" |
| **`Sim.Restwaerme`, `Sim.Reststrom`** | `float`-Größen, dieselbe Größenordnung des Abstands | **PASS** |
| **Byte-Gleichheit** | in **beiden** Lesarten **verloren** | — |

**Die entscheidende Unterscheidung:** Der Toleranzvergleich bliebe bei Lesart B auf
PASS — die **Byte-Gleichheit** nicht. Das Regressionsnetz dieses Projekts arbeitet
seit der Basis M1 mit Byte-Gleichheit als Regelfall und meldet jede Abweichung; ein
Umbau, der 312 CSV auf „PASS, aber nicht byte-gleich" setzt, macht die nächste echte
Abweichung unsichtbar.

> **Eine Vereinheitlichung nach Lesart B oder C braucht deshalb eine neue Basis R4
> nach ausdrücklichem Anwenderentscheid** — mit demselben Nachweis, den W6‑O‑7 für R3
> geführt hat: der neue Stand einmal gegen den alten mit Toleranz (muss PASS sein),
> und danach eingefroren.

---

## 7. Stufenplan

### S1 — Die Anzeigegrenze härten *(**ERLEDIGT** am 07.09.2026, Commit `5b80a8e`)*

| Schritt | Stellen | Aufwand | Stand |
|---|---:|---|---|
| S1.1 Brauchwasser: eine Einheit für beide Wege | 4 | — | **erledigt** (W8‑O‑5b, `6839a7a`) |
| S1.2 Die elf Energie-Umrechnungen aus U6 in den Kern ziehen — die `…Mwh`-Felder von `SimulationErgebnisCtrl` benutzen statt am DTO vorbei zu rechnen | 11 | 3 h | **erledigt**: vier neue DTO-Felder (`HeizstabWaermeproduktionMwh`, `StromspeicherEntladungMwh`, `StrombedarfMitEigenverbrauchMwh`, `StromGesamtMwh`) und `SimulationErgebnisCtrl.KanalMwh`; die zwei Hüllenmethoden `StrombedarfGesamt()`/`StromgedecktMwh()` sind gefallen. Die zwei Ringdiagramme (U5) lesen jetzt EINE Konvention. Windows-Abnahme A‑W8‑O5c‑1…4 offen |
| S1.3 Die Jahressummen benennen — Einheit am Namen | **43 Namen, 51 Deklarationen, 412 Fundstellen** | 4 h | **erledigt**: alle 31 `…_gesamt`/`…_summe` der sechs Erzeuger- und Speicherklassen bis auf die 13 begründeten Ausnahmen, dazu `Restwaerme`/`Reststrom` (U8), die 19 Brennstoffzähler und die zwei Ergebnisrecords. Der CSV-Schlüssel `Sim.Restwaerme` bleibt hart verdrahtet (Q7), mit Kommentar an der Exportzeile |
| S1.4 Die zwei Wächter | 1 neue Testdatei, 7 Fälle | 2 h | **erledigt**: `EPOS.Kern.Tests/EinheitenWacheTests.cs` — Faktor 1000 in `EPOS.UI/**` und `WindowsFormsApplication1/Views/**` (5 Ausnahmen, alle Leistung), Einheit am Namen in den sieben Simulationsklassen (13 Ausnahmen). Beide je einmal als rot belegt |
| S1.5 Die Einheitenregel schreiben | — | 1 h | **erledigt**: `EPOS.Kern/CLAUDE.md` Abschnitt „Einheiten: die Regel des Rechenkerns", ein Satz in `EPOS.UI/CLAUDE.md` |
| **Summe S1** | **~430** | **10 h** | **Referenzlauf byte-gleich: 12 von 12 Projekten, 312 von 312 CSV** |

> **Warum aus „~76 Stellen" 430 wurden.** Die Schätzung zählte die Felder, die das Papier
> namentlich nennt. Der Wächter aus S1.4 zieht die Grenze aber nicht bei einer Namensliste,
> sondern bei einer REGEL — und die traf alle 31 Jahressummen der sechs Klassen plus die
> Brennstoffzähler, die als Geschwister im selben Deklarationsblock stehen. Eine Regel, die
> ihre eigene Datei nur zur Hälfte durchsetzt, ist keine Regel; ein halb umbenannter Block
> (`GasverbrauchSpkMwh` neben `Koks_SPK`) liest sich schlechter als der Ausgangszustand.
> Die Umbenennung ist mechanisch, der Übersetzer findet jeden Nutzer, und die 312 CSV sind
> byte-gleich geblieben.

### S2 — Eine Einheit je Rechenstufe im Kern *(**ENTFÄLLT** — Anwenderentscheid Q1/Q4 vom 07.09.2026)*

> Q1 lautet „Regel festschreiben" (Lesart A), Q4 entfällt damit. **S2 wird nicht gebaut.**
> Die Begründung steht in Kapitel 5: Eine Recheneinheit behebt die Ursache nicht — die war
> die ungenannte Einheit, nicht die zweite —, sie bringt keine Genauigkeit (Kapitel 4.3) und
> sie kostet die Byte-Gleichheit von 312 CSV und damit eine neue Basis R4. Die Tabelle bleibt
> als Aufwandsschätzung stehen, falls der Entscheid je zurückgenommen wird.

**Was aus Q5 wurde:** Der Wechsel der AKKUMULATOREN von `float` auf `double` ist die einzige
Maßnahme dieses Papiers, die Genauigkeit bringt (neun Größenordnungen, Kapitel 4.3). Er war
KEIN Teil von S2 und bekam am 07.09.2026 die eigene Kennung **W8‑O‑5d**. Der Anwender hat sie
noch am selben Tag **weiter gefasst als gefragt**: „alles in double, ist kein Nachteil und
systematisch. Summenfunktionen aus Original BHKW-Plan ebenfalls double." Damit ist der TYPTEIL
der Stufe S2 **umfassender erledigt als geplant** — nicht nur die Akkumulatoren, sondern auch
die 59 Stundenreihen (Q6 überholt) und `BhkwPlan.VectorSumme`/`MonatsSumme`, die das Verhalten
der abgelösten Original-DLL absichtlich nachbildeten. Der EINHEITENteil von S2 bleibt
unausgeführt; daran ändert der Typentscheid nichts. Basis: `2026-09-07_R4_Double`, Einzelheiten
in Kapitel 9.2.

*Die folgende Tabelle ist die Schätzung von Rev. 1 und wird nicht ausgeführt:*

Nicht in einem Zug, sondern **je Stufe** mit eigenem Referenzlauf-Diff:

| Schritt | Stellen | Aufwand | Risiko |
|---|---:|---|---|
| S2.1 `SimulationSPK` auf kWh: `Waermebedarf_gesamt`, `S_Waerme_spk`, `Stromverbrauch_Spk`, `BruttoWaermeSpkErzeugung` und die zehn Brennstoffzähler; die vier `/= 1000` in `:1107‑1115` fallen, die Teilung wandert in `SimulationErgebnisCtrl`/`SimulationRunner` | ~25 | **6 h** | **mittel** — die Brennstoffzähler gehen in die Emissions- und Kostenrechnung; jede der 24 M-Stellen ist nachzuprüfen |
| S2.2 `SimulationBHKW` auf kWh: `s_waerme_MWh`, `s_strom_MWh`, `Waermeproduktion_BHKW_MWh`, `Stromproduktion_BHKW_MWh`; die drei Z-Stellen (Vollbenutzungsstunden) rechnen dann ohne `* 1000` | ~15 | **5 h** | **mittel** — dieselbe Emissionskette |
| S2.3 `SimulationControl.Restwaerme`/`Reststrom` auf kWh und auf `double` | ~10 | **3 h** | **mittel** — beide stehen als Skalar in der Referenzbasis |
| S2.4 `SimulationWaermebedarf`/`SimulationStrombedarf`: die acht Jahressummen auf kWh; U2 und U3 fallen dabei weg | ~15 | **4 h** | **hoch** — `Waermebedarf_Gesamt` geht in die Netzverlustrechnung (`:355`, `:366`) zurück in den Rechenweg |
| S2.5 Die zwei Nähte konsolidieren: `SimulationErgebnisCtrl` und `SimulationRunner` bekommen EINE gemeinsame Umrechnung statt 68 einzelner Divisionen | 68 | **6 h** | **niedrig**, wenn S2.1–S2.4 stehen |
| **Summe S2** | **~133** | **24 h** | **mittel bis hoch**; **neue Basis R4 nötig** |

### S3 — Gespeicherte Einheiten *(**BEWUSST NICHT** — Anwenderentscheid Q2 vom 07.09.2026)*

> Q2 lautet „Empfehlung": Die 17 `Tab_Ergebnis*` bleiben, wie sie sind, und umgerechnet wird
> an der SCHREIBGRENZE — also dort, wo es heute schon geschieht (`SimulationRunner`). Ein
> Migrationsschritt über die Ergebnisse aller Anwenderprojekte ist nicht rückrollbar, und der
> Nutzen wäre null: Die Tabelle wird geschrieben und gelesen, nie gerechnet.

15 der 17 `Tab_Ergebnis*`-Tabellen führen MWh, zwei (Puffer- und Stromspeicher) kWh. Ein Wechsel
bräuchte einen Migrationsschritt, der **jede vorhandene Ergebniszeile jedes
Anwenderprojekts** umrechnet — und der Rückweg wäre nur über eine Sicherung möglich.

| Schritt | Stellen | Aufwand | Risiko |
|---|---:|---|---|
| S3.1 Migrationsschritt „Ergebniswerte MWh → kWh" für 15 Tabellen, ~90 Spalten | ~90 | **8 h** | **hoch** — nicht rückrollbar ohne Sicherung, trifft Anwenderbestände |
| S3.2 Ergebnisexport, Bericht und Kennzahlenkatalog nachziehen | ~30 | **4 h** | mittel |
| **Summe S3** | **~120** | **12 h** | **hoch** |

**Empfohlen wird stattdessen:** Die Ergebnistabellen bleiben MWh, und die Umrechnung
steht an der **Schreibgrenze** — also da, wo sie heute steht (`SimulationRunner`). Das
ist Q2.

---

## 8. Fragen an den Anwender

> **Alle acht sind am 07.09.2026 beantwortet** — Q1 mit „Regel festschreiben", Q2…Q8 mit
> „Empfehlung". Die Antworten und was daraus folgte stehen in **Kapitel 9.1**; die Tabelle
> hier bleibt als Begründung der Empfehlungen stehen.

| Nr. | Frage | Empfehlung |
|---|---|---|
| **Q1** | **Eine Recheneinheit im Kern — kWh oder MWh —, oder die Regel „Reihen kWh, Ausweisungen MWh" ausdrücklich festschreiben?** | **Die Regel festschreiben (Lesart A).** Sie ist der Bestand, sie kostet keine Referenzbasis, und sie behebt die Ursache: nicht die zweite Einheit, sondern die ungenannte. Falls doch eine Recheneinheit gewünscht ist: **kWh**, weil die Stundenbasis, die importierten Ganglinien, die zwei Speichertabellen und 300 der 312 Referenz-CSV dort stehen — nicht MWh, obwohl die Profilkataloge und die Ergebnistabellen es nahelegen: Ein MWh-Kern müsste jede Kaskaden- und Speicherzeile mit 0,001 durchsetzen. |
| **Q2** | **Wechseln die 17 `Tab_Ergebnis*`-Tabellen mit, oder wird an der Schreibgrenze umgerechnet?** | **An der Schreibgrenze umrechnen, Tabellen bleiben MWh.** Ein Migrationsschritt über die Ergebnisse aller Anwenderprojekte ist nicht rückrollbar, und der Nutzen ist null: Die Tabelle wird nur geschrieben und gelesen, nie gerechnet. |
| **Q3** | **Wechselt der CSV-Export der Referenzläufe mit?** | **Nein.** Er ist ein Regressionsnetz. Seine Einheit muss über Jahre stabil sein, damit alte Basen vergleichbar bleiben — genau deshalb trägt `Ergebnisexport` seit dem Stromspeicher-Nachzug die alten Dateinamen weiter. |
| **Q4** | **Wenn S2: in welcher Reihenfolge?** | **SPK → BHKW → `SimulationControl` → Bedarf → Nähte**, je Stufe mit eigenem Referenzlauf-Diff und eigenem Commit. Der Bedarf zuletzt, weil `Waermebedarf_Gesamt` als einzige der Größen in den Rechenweg zurückgeht (Netzverluste). |
| **Q5** | **`float` → `double` im Kern als eigene Frage?** | **Ja, und getrennt von der Einheitenfrage.** Die Probe zeigt neun Größenordnungen Unterschied zwischen `float`- und `double`-Akkumulation (10⁻⁶ gegen 10⁻¹⁵), und der 1-ULP-Abstand aus W8‑O‑5b ist ein `float`-Artefakt. **Aber:** `BhkwPlan.VectorSumme`/`MonatsSumme` bilden die Original-DLL absichtlich nach; ein Wechsel ändert jede Monatsreihe und jede Jahressumme und braucht ebenfalls R4. Empfehlung: als eigenes Paket führen, **nicht** in S1 oder S2 mitnehmen. |
| **Q6** | **Sollen die 59 Stundenreihen `float` bleiben oder auf `double` gehen?** | **`float` bleiben.** 59 × 8 760 × 4 Byte = 2 MB; in `double` 4 MB — der Speicher ist nicht das Argument, sondern die Byte-Gleichheit: Jede Reihe steht in der Referenzbasis. Wenn Q5 mit „ja" beantwortet wird, gehören zuerst die AKKUMULATOREN auf `double`, nicht die Reihen. |
| **Q7** | **Soll S1.3 (Umbenennungen) den CSV-Schlüssel `Sim.Restwaerme` mitziehen?** | **Nein.** Der Schlüssel ist der Name in 312 Dateien der Basis; er bleibt hart verdrahtet, das Feld bekommt seinen Einheitennamen. Ein Kommentar an der Exportzeile sagt, warum. |
| **Q8** | **Soll U7 (der falsche Kommentar an `Strombedarf_Max`) sofort korrigiert werden?** | **Ja, im nächsten Commit, der die Datei ohnehin anfasst.** Ein Kommentar ist kein Rechenweg; der Referenzlauf sieht ihn nicht. |

---

## 9. Entscheide

| Kennung | Datum | Inhalt | Stand |
|---|---|---|---|
| **W8‑O‑5** | 04.09.2026 | Die Anzeigeeinheit der Bedarfsansichten ist wählbar: MWh als Vorgabe, kWh wählbar, konsistent in den Ansichten. Die Einheit steht AM WERT (`Energieeinheit`), die Anzeige rechnet um. | **umgesetzt** (`EPOS.Kern/Allgemein/Energieeinheit.cs`) |
| **W9‑O‑3** | 04.09.2026 | Die Prozesssumme der Vorschau geht über die Einheitenklasse in die Einheit, die der Kern führt (MWh). | **umgesetzt** (`SimulationWaermebedarf.ProzesssummeUebernehmen`) |
| **W8‑O‑5b** | 07.09.2026 | „Nehme die Umrechnung in den Dialogen vor." — Die Brauchwassermenge steht auf BEIDEN Wegen in MWh; jede Übergabe an einen Dialog trägt ihre Einheit, der Dialog rechnet über `Energieeinheit` um. | **umgesetzt** in Commit `6839a7a` |
| **W8‑O‑5c** | 07.09.2026 | „Prüfe, ob es nicht sinnvoll ist, die gesamten Berechnungen in kWh auszuführen … oder Vereinheitlichen der Berechnung in MWh — aber einheitlich." | **beantwortet** — Q1 „Regel festschreiben", Q2…Q8 „Empfehlung"; **Stufe S1 umgesetzt in `5b80a8e`** |
| **W8‑O‑5d** | 07.09.2026 | „alles in double, ist kein Nachteil und systematisch. Summenfunktionen aus Original BHKW-Plan ebenfalls double." — **umfassender als Q5 gefragt hatte**: nicht nur die Akkumulatoren, sondern der GANZE Rechenweg samt der 59 Stundenreihen (Q6 damit überholt) und samt `BhkwPlan.VectorSumme`/`MonatsSumme`. | **umgesetzt**, neue Basis `Referenzlaeufe/2026-09-07_R4_Double` |

### 9.1 Die Antworten auf Q1…Q8 (Anwender, 07.09.2026)

| Nr. | Antwort im Wortlaut | Was daraus folgte |
|---|---|---|
| **Q1** | **„Regel festschreiben"** | Lesart A. Zeitreihen kWh, Ausweisungen MWh, die Einheit steht im FELDNAMEN, umgerechnet wird an genau zwei Nähten (`SimulationErgebnisCtrl`, `SimulationRunner`) und sonst nur in der Anzeige über `Energieeinheit`. **Kein** Wechsel der Recheneinheit im Kern, **keine** neue Referenzbasis. Die Regel steht in `EPOS.Kern/CLAUDE.md`, Abschnitt „Einheiten"; zwei Wächter halten sie (S1.4) |
| **Q2** | „Empfehlung" | Die 17 `Tab_Ergebnis*` bleiben, wie sie sind (15 MWh, zwei kWh); umgerechnet wird an der SCHREIBGRENZE, also dort, wo es heute schon geschieht. Kein Migrationsschritt über Anwenderbestände |
| **Q3** | „Empfehlung" | Der CSV-Export der Referenzläufe wechselt **nicht** die Einheit. Er ist ein Regressionsnetz; seine Einheit muss über Jahre stabil bleiben |
| **Q4** | „Empfehlung" | **Entfällt** — ohne S2 gibt es keine Reihenfolge zu bestimmen |
| **Q5** | „Empfehlung" | `float` → `double` der Akkumulatoren ist ein **eigenes späteres Paket** mit eigener Basis R4 und trägt die Kennung **W8‑O‑5d**. In S1 wurde **kein** Typ angefasst. **Diese Empfehlung ist ÜBERHOLT** — der Anwender hat W8‑O‑5d am 07.09.2026 weiter gefasst als hier gefragt: nicht nur die Akkumulatoren, der ganze Rechenweg (siehe 9.2) |
| **Q6** | „Empfehlung" | Die 59 Stundenreihen bleiben `float` — sie stehen in 300 der 312 CSV der Basis. **ÜBERHOLT** — der Anwenderentscheid W8‑O‑5d vom 07.09.2026 stellt sie ausdrücklich auf `double`; die Basis ist neu (`2026-09-07_R4_Double`, siehe 9.2) |
| **Q7** | „Empfehlung" | Die CSV-Schlüssel `Sim.Restwaerme` und `Sim.Reststrom` bleiben **hart verdrahtet**; nur die Felder bekommen ihren Einheitennamen (`RestwaermeMwh`, `ReststromMwh`). Ein Kommentar an der Exportzeile in `Referenzlauf/Ergebnisexport.cs` sagt, warum. Dieselbe Regel gilt für `Puffer.Ladung_gesamt`, `Puffer.Entladung_gesamt` und `Puffer.Verluste_gesamt` |
| **Q8** | „Empfehlung" | U7 ist **sofort** berichtigt: Der Kommentar an `SimulationStrombedarf.Strombedarf_Max` nennt jetzt kW statt kWh |

### 9.2 W8‑O‑5d — der Rechenkern rechnet in `double` (07.09.2026)

**Wortlaut des Anwenders:** „W8‑O‑5d: alles in double, ist kein Nachteil und systematisch.
Summenfunktionen aus Original BHKW-Plan ebenfalls double."

Der Entscheid fällt **weiter aus als die Frage Q5**, die nur nach den Akkumulatoren gefragt
hatte, und er hebt die Empfehlung **Q6** ausdrücklich auf. Umgestellt sind:

* alle Stundenreihen des Laufs (8 760 / 35 040 / 365 / 168 / 12) — die 59 Reihen des
  `ZeitreihenSatz` und der Simulationsklassen,
* alle Akkumulatoren, Zwischenwerte, Felder, Eigenschaften, Parameter und Rückgaben in
  `EPOS.Kern/Allgemein/Simulation/**`, `Ganglinie`, `ProfilBedarf`, `ErdreichTemperatur`,
  `WaermequelleClass`, `Kanalsatz`, `SimulationRunner`, `ZeitreihenExtraktor`,
  `CsvExportClass` und den Controllern, die Reihen liefern oder verbrauchen,
* **`BhkwPlan.cs` vollständig** samt `VectorSumme` und `MonatsSumme`: Die bewusste
  Nachbildung des FPU-Verhaltens der Original-DLL (Zwischenwert in `double`, Ergebnis auf
  `float` zurückgeschrieben) ist aufgegeben,
* die Datenbank- und Dateigrenze: `Convert.ToSingle`/`(float)` beim Lesen wird
  `Convert.ToDouble` — SQLite `REAL` **ist** `double`,
* die Nahtstelle zur `SpeicherEngine`: `RasterAdapter.ZuViertelstundenDouble` nimmt
  `double[]`, `ZuFloat`/`ZuDouble` sind entfallen.

**`float` bleibt an drei Grenzen** (die Tabelle steht in `EPOS.Kern/CLAUDE.md`, Abschnitt
„Typen"): Bildpunkte des `ChartRenderer` (SkiaSharp rechnet in `float`), Einbettungsvektoren
des KI-Wissens und Typprüfungen auf boxed Datenbankwerte. Ein dritter Wächter,
`EPOS.Kern.Tests/DoubleWacheTests`, hält den Rechenweg frei — mit **leerer** Ausnahmeliste.
Im Kern fällt die Zahl der `float`-Fundstellen von **788 in 42 Dateien** auf **148 in 15**.

**Was das am Ergebnis geändert hat.** Die Jahressummen bleiben in allen zwölf Referenzprojekten
innerhalb **3e‑5** relativ; die erste Differenz einer Stundenreihe liegt bei einer
`float`-Stufe (rund 1e‑7). **Elf der zwölf Projekte reißen trotzdem die Toleranz iF15**, weil
drei Schwellen des Modells am letzten Bit entscheiden und ihr Ergebnis über Stunden
weitertragen: die Speicherhysterese `SOC >= Q_max · SchwelleAus` (bistabil), die
Volllast/Modulations-Grenze des BHKW und die drei `int`-Rückgaben in `BhkwPlan` (Borland
`_ftol`). Die Energie bleibt dabei erhalten — in Projekt 1018 sind Bedarf, Produktion, SOC und
Verluste identisch und nur die Aufteilung des Puffers zwischen Umsatz und Durchfluss
verschoben; in Projekt 1024 verschiebt sich die Fahrweise zwischen BHKW (+11,2 %), Wärmepumpe
(−14,4 %) und Kessel (−10,8 %) bei unverändertem Gesamtbedarf. Zahlen, Tabelle je Projekt und
Herleitung stehen im `protokoll.txt` der Basis `Referenzlaeufe/2026-09-07_R4_Double` und in
`Referenzlaeufe/LIESMICH.md`.

**Der Beleg, dass der neue Stand der richtige ist**, steht außerhalb des Referenznetzes: Die
Brauchwasser-Jahressumme in `EPOS.Kern.Tests/BedarfVerwaltungTests` trifft die Katalogmenge
jetzt exakt — **742,9000 kWh statt 742,9008** bei 0,7429 MWh Katalogwert. Die 0,0008 kWh waren
die Rundung der 8 760 `float`-Zellen.

---

## Anhang A — Alle 242 Fundstellen

**Legende:** `E` Energie kWh↔MWh · `L` Leistung/Einstrahlung W↔kW · `P` Preis
€/kWh, ct/kWh auf eine MWh-Menge · `M` Emission g/kWh, mg/kWh → t bzw. kg ·
`V` Volumen/Konstante · `Z` Vollbenutzungsstunden · `K` Kommentar, keine Rechnung.

Erhoben am 07.09.2026 auf dem Stand von Commit `6839a7a`.

### A.1 — `/ 1000` und `* 1000` (216 Stellen)

**`EPOS.Kern/Controller/SimulationErgebnisCtrl.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 146 | E | `u.WpWaermeproduktionMwh = sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000.0;` |
| 147 | E | `u.WpStromverbrauchMwh = sim.simulation_wp.WP_Strombedarf_gesamt / 1000.0;` |
| 149 | E | `u.HeizstabStromverbrauchMwh = sim.simulation_wp.Heizstab_gesamt / 1000.0;` |
| 153 | E | `u.SolarWaermeproduktionMwh = sim.simulation_solarthermie.Waermeproduktion_gesamt / 1000.0;` |
| 154 | E | `u.PvStromproduktionMwh = sim.simulation_pv.Stromproduktion_gesamt / 1000.0;` |
| 194 | E | `return summe / 1000.0;` |
| 230 | K | `/// Die Kernformel aus iU9-W10a lautet <c>Volumen · 1,16 · ΔT / 1000</c> und` |
| 270 | E | `e.StufeneingangMwh = wp.Waermebedarf_gesamt / 1000.0;` |
| 279 | E | `e.StromverbrauchMwh = wp.WP_Strombedarf_gesamt / 1000.0;` |
| 280 | E | `e.HeizstabStromverbrauchMwh = wp.Heizstab_gesamt / 1000.0;` |
| 281 | E | `e.WaermeproduktionMwh = wp.WP_Waermeproduktion_gesamt / 1000.0;` |
| 296 | E | `wp.Modul_WP_Waermeproduktion[i] / 1000.0,` |
| 297 | E | `wp.Modul_WP_Strombedarf[i] / 1000.0,` |
| 298 | E | `wp.Modul_Heizstab[i] / 1000.0,` |
| 345 | K | `/// Die Kernformel aus iU9-W10a lautet <c>Volumen · 1,16 · ΔT / 1000</c> und` |
| 408 | E | `e.StrombedarfMwh = spk.Strombedarf_gesamt / 1000.0;` |
| 409 | E | `e.ReststrombedarfMwh = spk.Strombedarf_gesamt / 1000.0 + spk.Stromverbrauch_Spk;` |
| 424 | E | `e.QuellwaermeMwh = spk.Quellwaerme_gesamt / 1000.0;` |
| 496 | E | `e.StufeneingangMwh = st.Waermebedarf_gesamt / 1000.0;` |
| 498 | K | `// Runner, die beide (Stufeneingang - Eigenanteil) / 1000 rechnen.` |
| 500 | E | `st.Waermebedarf_gesamt, eigenKwh) / 1000.0;` |
| 503 | E | `eigenKwh / 1000.0, wb != null ? wb.Waermebedarf_Gesamt : 0.0);` |
| 505 | E | `e.WaermeproduktionMwh = st.Waermeproduktion_gesamt / 1000.0;` |
| 506 | E | `e.UeberschussMwh = st.Ueberschuss_summe / 1000.0;` |
| 511 | E | `k.Waermeproduktion / 1000.0,` |
| 512 | E | `k.Ueberschuss / 1000.0));` |
| 564 | E | `e.StufeneingangMwh = bh.Waermebedarf_gesamt / 1000.0;` |
| 565 | E | `e.StrombedarfMwh = bh.strombedarf.Sum() / 1000.0;` |
| 575 | E | `e.WaermeueberschussMwh = bh.Waermeueberschuss / 1000.0;` |
| 576 | E | `e.SpeicherladungMwh = bh.Speicherladung_gesamt / 1000.0;` |
| 577 | E | `e.SpeicherdeckungMwh = bh.Speicherentladung_Anteil / 1000.0;` |
| 660 | E | `e.StromproduktionMwh = produktionKwh / 1000.0;` |
| 661 | E | `e.UeberschussMwh = pv.Ueberschuss.Sum() / 1000.0;` |
| 671 | E | `m.Stromproduktion / 1000.0));` |
| 679 | E | `m.Name, g.Anzeigename, g.DcAc, g.ErtragKwh / 1000.0,` |

**`EPOS.Kern/Controller/TechnikPlanwertCtrl.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 643 | K | `/// [W je Modul] × <c>Tab_Energieanlagen.PV_Leistung</c> [Modulanzahl]) / 1000` |
| 864 | E | `double kwh = Zahl(r, "Stromproduktion") * 1000.0;   // MWh/a → kWh/a` |
| 1043 | E | `foreach (DataRow r in w.Rows) kwh += Zahl(r, "Waermeproduktion") * 1000.0;` |

**`EPOS.Kern/Controller/GanglinienAuswertungCtrl.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 53 | Z | `=> SpitzeKw > 0 ? JahresarbeitMwh * 1000.0 / SpitzeKw : (double?)null;` |
| 244 | E | `return summe / 1000.0;` |

**`EPOS.Kern/Controller/GebaeudeBedarfCtrl.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 46 | Z | `=> MaxLastKw > 0 ? HeizwaermeMwh * 1000.0 / MaxLastKw : (double?)null;` |
| 126 | K | `// ZEICHENGLEICH zum Lauf: dort steht "kanalHeizung.Sum() / 1000" - eine` |
| 128 | K | `// "/ 1000.0" waere eine double-Division und ergaebe eine andere neunte` |
| 130 | E | `ergebnis.HeizwaermeMwh = werte.Sum() / 1000;` |

**`EPOS.Kern/Controller/PhotovoltaikCtrl.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 185 | K | `/// Σ (<c>Tab_PV.Leistung</c> [W je Modul] × Modulanzahl) / 1000.` |
| 239 | L | `return (o == null \|\| o == DBNull.Value) ? 0 : Convert.ToDouble(o) / 1000.0;` |
| 265 | K | `/// <see cref="KwpSumme"/> (Summe / 1000); sie steht hier und nicht in der` |
| 277 | L | `return (summeWatt / 1000.0).ToString(KW_ANZEIGE_FORMAT, CultureInfo.CurrentCulture);` |

**`EPOS.Kern/Controller/BedarfsVorschauCtrl.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 155 | E | `sim.Waermebedarf_Prozess = sim.prozesswerte.Sum() / 1000;` |

**`EPOS.Kern/Model/EmissionsModelle.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 68 | M | `return IstMilligramm ? wert / 1000.0 : wert;` |

**`EPOS.Kern/Allgemein/Energieeinheit.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 14 | K | `/// Prüfmeldungen — und dazwischen ein nacktes <c>/ 1000</c>, das nur in EINEM` |
| 78 | E | `return ReferenceEquals(this, KWh) ? kWh : kWh / 1000.0;` |
| 84 | E | `return ReferenceEquals(this, MWh) ? mWh : mWh * 1000.0;` |
| 101 | E | `return ReferenceEquals(this, MWh) ? wert : wert / 1000.0;` |
| 107 | E | `return ReferenceEquals(this, KWh) ? wert : wert * 1000.0;` |

**`EPOS.Kern/Allgemein/Bericht/ChartRenderer.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 2750 | E | `r[m] = s / 1000.0;` |

**`EPOS.Kern/Allgemein/Bericht/KostenEmissionRechner.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 30 | K | `///    Strom 560. t/a = MWh/a × Faktor / 1000.` |
| 177 | E | `double menge = kv.Value * 1000.0 / info.EffHi.Value;   // Abrechnungseinheit` |
| 181 | P | `kosten = kv.Value * 1000.0 * info.PreisArbeit.Value;   // €/kWh direkt` |
| 216 | M | `brennstoffCO2t += kv.Value * wirksam.Value / 1000.0;` |
| 223 | M | `behgCO2t += kv.Value * info.CO2.Value / 1000.0;   // BEHG-Basis (Phase 7/W2)` |
| 243 | P | `stromKosten = netzbezugMWh * 1000.0 * strom.PreisArbeit.Value;` |
| 261 | M | `double netzCO2t = netzbezugMWh * stromCO2 / 1000.0;` |
| 289 | M | `? (double?)(v.CO2Gesamt.Value * 1000.0 / waermeMWh)    // t/a → g/kWh Wärme` |

**`EPOS.Kern/Allgemein/Simulation/PvStrangModell.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 339 | L | `? g.m_P_Standby.Value / 1000.0 : 0.0,` |
| 341 | L | `? g.m_P_Nacht.Value / 1000.0 : 0.0` |

**`EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 228 | E | `Waermebedarf_Gebaeude_Gesamt = kanalHeizung.Sum() / 1000;` |
| 311 | E | `Waermebedarf_Extern_Gesamt += ganglinie.Sum() / 1000;` |
| 327 | E | `Waermebedarf_Prozess = prozesswerte.Sum() / 1000;` |
| 349 | E | `Waermebedarf_Gesamt = Waermebedarf.Sum() / 1000;` |
| 355 | E | `stundl_netzverluste = (Waermebedarf_Gesamt * 1000 * Netzverluste) / (float)876000;` |
| 366 | E | `Waermebedarf_Netzverluste = (double)stundl_netzverluste * 8760 / 1000;` |
| 386 | E | `Waermebedarf_Gesamt = Waermebedarf.Sum() / 1000;` |
| 627 | E | `VerbrauchNeu = item.Z_AuswahlWohnflaeche * item.Jahresnutzungsgrad / 1.1 * 1000;` |
| 631 | E | `VerbrauchNeu = item.Z_AuswahlWohnflaeche * item.Jahresnutzungsgrad * 1000;` |
| 635 | E | `VerbrauchNeu = item.Z_AuswahlWohnflaeche * 1000;` |
| 649 | K | `//                double VerbrauchAlt = (BrauchwasserGeb[index] + HeizwaermebedarfGeb[index])...` |
| 650 | E | `double VerbrauchAlt = HeizwaermebedarfGeb[index] / 1000;` |
| 959 | K | `/// <c>/ 1000</c>, der Bedarfsprofildialog (W9) mit der blanken Summe — also in` |

**`EPOS.Kern/Allgemein/Simulation/SimulationStrombedarf.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 335 | E | `Strombedarf_Gebaeude_gesamt = stundenreihe.Sum() / 1000;` |

**`EPOS.Kern/Allgemein/Simulation/SimulationSolarthermie.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 283 | L | `f.Potenzial[i] = (leistungProQm * f.Flaeche * leitungsverluste) / 1000.0;` |
| 326 | L | `double potenzielleErzeugung = (leistungProQm * flaeche * leitungsverluste) / 1000.0;` |

**`EPOS.Kern/Allgemein/Simulation/SimulationPV.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 485 | K | `/// <c>P_DC[kW] = P_STC[kW] · (G·cosTheta / 1000) · (1 + gamma·(T_Zelle − 25))</c>.` |
| 513 | L | `? pStcKw * (strahlung * cosTheta / 1000.0) * tempFaktor` |
| 514 | L | `: (strahlung * cosTheta * flaeche * (nennWirk * tempFaktor)) / 1000.0;` |
| 540 | L | `return modul.m_Leistung / 1000.0 * anzahlModule;` |
| 569 | L | `double ausFlaeche = modul.m_Laenge * modul.m_Breite * (modul.m_Wirkungsgrad / 100.0) * 1000.0;` |
| 896 | K | `/// — <c>Modul-Nennleistung / 1000 · Modulzahl</c>, in genau dieser Reihenfolge` |
| 898 | K | `/// rechnet <c>Modulzahl · Leistung / 1000</c>, was algebraisch dasselbe und im` |
| 968 | L | `kwp += modulJeStrang[s].LeistungW / 1000.0 * s.Modulzahl;` |
| 1071 | L | `paar.Value.PStcKw += paar.Value.LeistungW / 1000.0 * paar.Key.Modulzahl;` |
| 1203 | L | `double pStcStrang = m.LeistungW / 1000.0 * s.Modulzahl;` |

**`EPOS.Kern/Allgemein/Simulation/VDI4640Pruefung.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 635 | K | `///   4. Konsistenz Leistung × Volllaststunden / 1000 ≈ Energie (±1 kWh/m²)` |
| 688 | K | `// Konsistenz der drei Tabellen: Leistung x Volllaststunden / 1000 = Energie` |
| 692 | L | `double erwartet = A2_LEISTUNG[z, b] * A2_VOLLLASTSTUNDEN[z] / 1000.0;` |

**`EPOS.Kern/Allgemein/Simulation/PvErweitertesModell.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 28 | K | `/// Untergrenze der bezogenen Einstrahlung <c>G' = G_t / 1000</c>, unterhalb derer` |
| 90 | K | `/// <param name="gStrich">G' = Einstrahlung auf die Modulebene / 1000 [kW/m²].</param>` |
| 122 | L | `double gStrich = gTilted / 1000.0;` |

**`EPOS.Kern/Allgemein/Simulation/SimulationPufferspeicher.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 12 | K | `///     Q_max [kWh] = Volumen [l] * 1,16 Wh/(l*K) * (Vorlauf - Rücklauf) / 1000` |
| 415 | V | `Q_max = volumenLiter * 1.16 * deltaT / 1000.0;` |
| 1364 | V | `double vM3 = VolumenLiter / 1000.0;` |
| 1378 | V | `? lambda * quer * n / hoehe / 1000.0` |

**`EPOS.Kern/Allgemein/Simulation/WaermesenkeClass.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 254 | V | `return Gesamtvolumen * 1.16 * (Vorlauf - Ruecklauf) / 1000.0;` |

**`EPOS.Kern/Allgemein/Simulation/SimulationControl.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 771 | E | `Reststrom += (float)simulation_wp.WP_Strombedarf_gesamt / 1000f; // in MWh` |
| 772 | E | `Reststrom += (float)simulation_wp.Heizstab_gesamt / 1000f;       // in MWh` |

**`EPOS.Kern/Allgemein/Simulation/SimulationRunner.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 240 | E | `wp.Heizstab_gesamt) / 1000.0;` |
| 253 | E | `double direkt = spk.S_Waerme_spk - spk.Speicherladung_gesamt / 1000.0;` |
| 255 | E | `return direkt + spk.Speicherentladung_Anteil / 1000.0;` |
| 281 | E | `return (bh.Direktdeckung_gesamt + bh.Speicherentladung_Anteil) / 1000.0;` |
| 361 | E | `w.Waermebedarf = wp.Waermebedarf_gesamt / 1000.0;` |
| 362 | E | `w.Waermeproduktion_WP = wp.WP_Waermeproduktion_gesamt / 1000.0;` |
| 363 | E | `w.Stromverbrauch_WP = wp.WP_Strombedarf_gesamt / 1000.0;` |
| 364 | E | `w.Stromverbrauch_Heizstab = wp.Heizstab_gesamt / 1000.0;` |
| 369 | E | `w.Restwaermebedarf = wp.waermerestbedarf_gesamt / 1000.0;` |
| 373 | K | `// implizit mit 1 K) und ohne /1000, nahm das Volumen zudem aus dem` |
| 488 | E | `mo.Waermeproduktion = wp.Modul_WP_Waermeproduktion[i] / 1000.0;` |
| 489 | E | `mo.Stromverbrauch = wp.Modul_WP_Strombedarf[i] / 1000.0;` |
| 490 | E | `mo.Heizstab = wp.Modul_Heizstab[i] / 1000.0;` |
| 509 | E | `double waermebedarfMWh = bh.Waermebedarf_gesamt / 1000.0;` |
| 510 | E | `double strombedarfMWh = bh.strombedarf.Sum() / 1000.0;` |
| 514 | E | `b.Restwaermebedarf = restwaermeBhkw.Sum() / 1000.0;` |
| 518 | E | `b.Waermeueberschuss = bh.Waermeueberschuss / 1000.0;` |
| 702 | E | `h.Strombedarf = spk.Strombedarf_gesamt / 1000.0;` |
| 703 | E | `h.Reststrombedarf = spk.Strombedarf_gesamt / 1000.0 + spk.Stromverbrauch_Spk;` |
| 730 | E | `h.Quellwaerme = spk.Quellwaerme_gesamt / 1000.0;` |
| 808 | E | `stm.Waermebedarf = st.Waermebedarf_gesamt / 1000.0;` |
| 809 | E | `stm.Waermeproduktion = st.Waermeproduktion_gesamt / 1000.0;` |
| 810 | E | `stm.Restwaermebedarf = (st.Waermebedarf_gesamt - solarEigen) / 1000.0;` |
| 836 | E | `double deckungS = solarEigen / 1000.0 * 100.0` |
| 848 | E | `stm.Ueberschuss = st.Ueberschuss_summe / 1000.0;` |
| 857 | E | `Waermeproduktion = k.Waermeproduktion / 1000.0,` |
| 858 | E | `Ueberschuss = k.Ueberschuss / 1000.0` |
| 869 | E | `pvm.Stromproduktion = pvs.Stromproduktion.Sum() / 1000.0;` |
| 887 | E | `pvm.Ueberschuss = einspKwh / 1000.0;` |
| 890 | E | `pvm.Ueberschuss = pvs.Ueberschuss.Sum() / 1000.0;` |
| 904 | E | `Stromproduktion = p.Stromproduktion / 1000.0` |
| 1026 | E | `mwh[k] = summe / 1000.0;` |
| 1080 | E | `k[i] = eigenanteilKanalKWh[i] / 1000.0 / basisMWh * 100.0;` |

**`EPOS.Kern/Allgemein/Simulation/SimulationBHKW.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 107 | K | `// Auswertung() als Waerme_MWh[i] / Waermeleistung[i] * 1000, ist also eine` |
| 372 | K | `// s_waerme ist am Ende bereits in MWh umgerechnet worden (laut deinem Code-Ende / 1000f),` |
| 374 | Z | `Laufzeiten[zaehler] = (s_waerme_MWh[zaehler] / bhkwWaermeLeistung[zaehler]) * 1000f;` |
| 389 | Z | `(s_strom_MWh[zaehler] / bhkwStromLeistung[zaehler]) * 1000f;` |
| 401 | K | `// Emissionen addieren und von g in kg oder Tonnen skalieren (/1000)` |
| 402 | M | `Em_CO2_BHKW += ModulVerbrauch * bhkwCO2Factor[zaehler] / 1000f;` |
| 403 | M | `Em_SO2_BHKW += ModulVerbrauch * bhkwSO2Factor[zaehler] / 1000f;` |
| 404 | M | `Em_NOX_BHKW += ModulVerbrauch * bhkwNOXFactor[zaehler] / 1000f;` |
| 405 | M | `Em_CO_BHKW += ModulVerbrauch * bhkwCOFactor[zaehler] / 1000f;` |
| 406 | M | `Em_Staub_BHKW += ModulVerbrauch * bhkwStaubFactor[zaehler] / 1000f;` |
| 460 | Z | `VbhElektrischGesamt = (summePelKW > 0) ? (summeStromMWh / summePelKW) * 1000f : 0f;` |

**`EPOS.Kern/Allgemein/Simulation/ErdreichAuswertung.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 466 | L | `a.MaxEntzugW = maxEntzugGesamtKW * anteil * 1000.0;   // kW -> W` |

**`EPOS.Kern/Allgemein/BhkwPlan.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 230 | E | `outJahr[h] = (float)((double)outJahr[h] / sum * monatsverbrauch[m] * 1000.0);` |

**`EPOS.Kern/Allgemein/Wirtschaftlichkeit/StromTarifRechner.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 236 | P | `k.ArbeitEur = mengeMWh * 1000.0 * rolle.ArbeitspreisEurKWh;` |
| 271 | P | `: eingabe.EinspeisungMWh * 1000.0 * einspeisung.ArbeitspreisEurKWh` |

**`EPOS.Kern/Allgemein/Wirtschaftlichkeit/StromMatrix.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 161 | E | `double b = bezug[h] / 1000.0;                     // kWh → MWh` |
| 163 | L | `if (b * 1000.0 > m.MaxBezugKW) m.MaxBezugKW = b * 1000.0;   // kWh/h ≙ kW` |
| 167 | E | `z.EinspeisungPvMWh += pvUeber[h] / 1000.0;` |
| 178 | E | `z.BedarfMWh += bedarfOhneAnlage / 1000.0;` |
| 192 | E | `z.KwkEigenMWh += eigen / 1000.0;` |
| 193 | E | `z.KwkEinspeisungMWh += Math.Max(0, erz - eigen) / 1000.0;` |
| 266 | P | `return z == null ? 0 : z.EinspeisungPvMWh * 1000.0 * preisEurKWh;` |
| 282 | P | `return z == null ? 0 : z.BezugMWh * 1000.0 * preisEurKWh;` |
| 288 | P | `return z == null ? 0 : (z.EinspeisungPvMWh + z.KwkEinspeisungMWh) * 1000.0 * preisEurKWh;` |

**`EPOS.Kern/Allgemein/Wirtschaftlichkeit/PvKennzahlenRechner.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 113 | E | `menge0 += erzeugungMWh * 1000.0;` |
| 114 | E | `mengeD += erzeugungMWh * 1000.0 * ab;` |
| 125 | P | `k.VermiedenerBezugJahr1Eur = evMWh * 1000.0 * strompreisEurKwh.Value;` |
| 138 | P | `double ersparnis = evMWh * 1000.0 * strompreisEurKwh.Value` |

**`EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 1544 | P | `e.Erloes = pvUeberschussMWh * 1000.0 * p.Einspeiseverguetung;` |
| 1567 | P | `e.ErloesKwk = kwkEinspeisungMWh * 1000.0 * p.EinspeiseverguetungKWK.Value;` |
| 1644 | M | `behgBasisT += v.BiogenBehgMengeMWh * efOhneNachweis / 1000.0;   // MWh × g/kWh → t` |
| 1874 | E | `- v.Ergebnis.Photovoltaik.Ueberschuss) * 1000.0);` |
| 1956 | P | `double betrag = netzbezugMWh * 1000.0 * ctKwh / 100.0;` |
| 2333 | P | `bonusVoll = eigenNettoMWh * 1000.0 * (satzEigenProjekt / 100.0)` |
| 2334 | P | `+ einspNettoMWh * 1000.0 * (p.KwkgBonusEinspeisung / 100.0);` |
| 2336 | P | `bonusVoll = stromNettoMWh * 1000.0 * (satzEigenProjekt / 100.0);` |
| 2465 | P | `double bonusVoll = eigenMWh * 1000.0 * (satzEigen / 100.0)` |
| 2466 | P | `+ einspMWh * 1000.0 * (satzEinsp / 100.0);` |
| 2634 | Z | `return a.PelKW > 0 ? stromMWh * 1000.0 / a.PelKW : 0;` |
| 3789 | Z | `? StromFoerderfaehigMWh * 1000.0 / PelFoerderfaehigKW : 0;` |
| 4418 | Z | `return stromMWh * 1000.0 / pelKW;` |
| 4719 | P | `return evMWh * 1000.0 * preis.Value;` |
| 4899 | P | `erg.Gestehungskosten = (-bild.Kapitalwert * a) / (eingabe.WaermeMWh * 1000.0);` |

**`EPOS.Kern/Allgemein/Wirtschaftlichkeit/EndenergieAufloeser.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 224 | E | `bedarfKwh += m.VerbrauchMWh * 1000.0;` |
| 227 | P | `if (preis.HasValue) kosten += m.VerbrauchMWh * 1000.0 * preis.Value;` |
| 258 | E | `bedarfKwh += (m.Stromverbrauch + m.Heizstab) * 1000.0;` |
| 365 | E | `if (m.VerbrauchMWh > 0) kwh += m.VerbrauchMWh * 1000.0;` |

**`EPOS.Kern/Allgemein/Wirtschaftlichkeit/PvErloesRechner.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 290 | E | `double basisKwh = Math.Max(0, einspeisungMWh * 1000.0 - e.KappungsverlustKwh);` |

**`EPOS.Kern/Allgemein/Wirtschaftlichkeit/EmissionsBilanzRechner.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 203 | K | `//   CO₂ [g/kWh]  → t/a  = MWh × Faktor / 1000` |
| 204 | K | `//   SO₂/NOx [mg/kWh] → kg/a = MWh × Faktor / 1000` |
| 205 | E | `if (co2Faktor.HasValue) co2 += verbrauchMWh * co2Faktor.Value / 1000.0; else co2Voll = false;` |
| 206 | E | `if (f.So2.HasValue) so2 += verbrauchMWh * f.So2.Value / 1000.0; else so2Voll = false;` |
| 207 | E | `if (f.Nox.HasValue) nox += verbrauchMWh * f.Nox.Value / 1000.0; else noxVoll = false;` |
| 238 | M | `b.CO2BiogenT = biogenMWh * k.BiogenZuschlagGJeKWh / 1000.0;   // MWh × g/kWh → t` |
| 275 | M | `parkCO2 = brennstoffPark * park.CO2 / 1000.0;                   // t/a` |
| 276 | M | `parkSO2 = brennstoffPark * park.SO2 / 1000.0;                   // kg/a` |
| 277 | M | `parkNOx = brennstoffPark * park.NOx / 1000.0;` |
| 285 | M | `parkCO2 = kwkStrom * k.SubstitutionsfaktorGJeKWh.Value / 1000.0;   // t/a` |
| 297 | M | `b.CO2GetrenntT = (refNoetig ? brennstoffRef * rk.CO2.Value / 1000.0 : 0) + parkCO2;` |
| 301 | M | `b.SO2GetrenntKg = (refNoetig ? brennstoffRef * rk.SO2.Value / 1000.0 : 0) + parkSO2;` |
| 303 | M | `b.NOxGetrenntKg = (refNoetig ? brennstoffRef * rk.NOx.Value / 1000.0 : 0) + parkNOx;` |

**`EPOS.Kern/Allgemein/Wirtschaftlichkeit/SteuerGutschriftRechner.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 597 | E | `return brennstoffMWh * 1000.0 / a.EffHi / 1000.0;` |
| 605 | E | `return brennstoffMWh * 1000.0 / a.EffHi / 1000.0;` |

**`EPOS.Kern/Allgemein/Update/ProjektPuffer.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 113 | K | `/// <c>Volumen · 1,16 · ΔT / 1000</c>.` |
| 134 | V | `return volumenLiter * WH_JE_LITER_KELVIN * deltaK / 1000.0;` |

**`EPOS.Kern/Allgemein/Import/Pan/PanModule.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 72 | K | `public double muISC { get; set; }          // mA/GradC (PVsyst-Konvention; A/K = muISC/1000)` |
| 73 | K | `public double muVocSpec { get; set; }          // mV/GradC (V/K = muVocSpec/1000; relativ in ...` |
| 79 | L | `public double muIscAK => muISC / 1000.0;       // A/K` |
| 80 | L | `public double muVocVK => muVocSpec / 1000.0;   // V/K` |

**`EPOS.Kern/Allgemein/Import/CEC/CecWechselrichter.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 124 | L | `m.m_P_AC_Nenn = Paco > 0 ? (double?)(Paco / 1000.0) : null;   // W -> kW` |

**`EPOS.Kern/Allgemein/Import/CEC/PVModule.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 42 | L | `public double Efficiency   => A_c > 0 ? STC / (A_c * 1000.0) * 100.0 : 0.0;` |

**`EPOS.Kern/Allgemein/Import/StrangPlausibilitaet.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 630 | L | `return s.Modulzahl * modul.m_Leistung / 1000.0;` |

**`EPOS.Kern/Allgemein/Import/ModulImportProfil.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 723 | L | `double kw = g.Paco / 1000.0;` |

**`EPOS.Kern/Allgemein/Import/OND/OndWechselrichter.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 170 | L | `return WechselrichterKennlinie.AusProfil(Kennlinienpunkte, PNomConv * 1000.0);` |

**`EPOS.Kern/Allgemein/Import/WechselrichterPlausibilitaet.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 135 | L | `&& pac * 1000.0 > m.m_Sandia_Pdco.Value)` |
| 137 | L | `Z(pac * 1000.0), Z(m.m_Sandia_Pdco.Value)));` |

**`EPOS.Kern/Allgemein/Import/KatalogImportProfil.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 241 | K | `/// <summary>Vorbelegung der Obergrenze (200 / 1000 / 5 / 100).</summary>` |

**`EPOS.Kern/Allgemein/Import/Stromspeicher/BslibImport.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 141 | L | `LeistungKw = StromspeicherImportSatz.Zahl(Feld(iEntladen)) / 1000.0,` |

### A.2 — Die 26 Stellen der übrigen vier Schreibweisen

**`EPOS.Kern/Allgemein/Simulation/SimulationSPK.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 240 | M | `Em_CO2_SPK /= 1000;` |
| 241 | M | `Em_SO2_SPK /= 1000;` |
| 242 | M | `Em_NOX_SPK /= 1000;` |
| 243 | M | `Em_CO_SPK /= 1000;` |
| 244 | M | `Em_Staub_SPK /= 1000;` |
| 1107 | E | `s_waerme_Gas_Spk[i] /= 1000;` |
| 1108 | E | `s_waerme_Oel_Spk[i] /= 1000;` |
| 1109 | E | `Kessel_Verbrauch_MWh_Spk[i] /= 1000;` |
| 1115 | E | `Waermebedarf_gesamt /= 1000;` |

**`EPOS.Kern/Allgemein/Simulation/SimulationControl.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 509 | E | `var x = Rest_Strombedarf_viertelstuendlich.Sum() / 4000;` |
| 558 | E | `Restwaerme /= 1000f;` |
| 562 | E | `Reststrom = Rest_Strombedarf_viertelstuendlich.Sum() / 4000f;` |

**`EPOS.Kern/Allgemein/Simulation/SimulationBHKW.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 1659 | E | `s_waerme_MWh[j] /= 1000f;` |
| 1660 | E | `s_strom_MWh[j] /= 1000f;` |

**`EPOS.Kern/Allgemein/Simulation/SimulationStrombedarf.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 122 | E | `Strombedarf_Gebaeude_gesamt += prozesswerte.Sum() / 4000;` |
| 181 | E | `Stromganglinie_gesamt = Stromganglinie_gesamt / 4000f; // MWh` |
| 184 | E | `Strombedarf_gesamt = Strombedarf_viertelStundenwerte.Sum() / 4000f; // in MWh` |
| 368 | E | `z[indexMonat] = z[indexMonat] / 4000.0f;` |

**`EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 223 | E | `for (int h = 0; h < 8760; h++) probe[h] *= 0.001;` |

**`EPOS.Kern/Allgemein/Simulation/SimulationRunner.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 891 | E | `pvm.Strombedarf = pvs.Strombedarf.Sum() / 4000.0;` |
| 892 | E | `pvm.Reststrombedarf = sim.Rest_Strombedarf_viertelstuendlich.Sum() / 4000.0;` |

**`EPOS.Kern/Controller/SimulationErgebnisCtrl.cs`**

| Zeile | Art | Ausdruck |
|---|---|---|
| 663 | E | `e.StrombedarfMwh = pv.Strombedarf.Sum() / 4000.0;` |
| 664 | E | `e.ReststrombedarfMwh = sim.Rest_Strombedarf_viertelstuendlich.Sum() / 4000.0;` |

**`EPOS.Kern/Allgemein/BhkwPlan.cs`** — die drei zentralen Routinen

| Zeile | Art | Ausdruck |
|---|---|---|
| 66 | L | `v[i] = (float)((double)v[i] * 0.001);` — `WattToKw` |
| 90 | E | `summe = (float)((double)acc * 0.001);` — `VectorSumme`, **`float`-Akkumulator** |
| 126 | E | `sum[m] = (float)(0.001 * value[d] + sum[m]);` — `MonatsSumme`, **`float`-Akkumulator** |

---

## Anhang B — Die Gleitkommaprobe

Der Quelltext der Probe steht nicht im Repository (sie ist ein Nachweis, kein
Bestandteil des Programms). Sie ist in wenigen Zeilen nachgebaut:

1. Ein Jahresprofil `double[8760]` aus Grundlast, Jahresgang (Kosinus, Maximum im
   Januar) und Tagesgang (Sinus), auf eine vorgegebene Jahresmenge normiert.
2. Dieselbe Reihe zweimal nach `float[]`: einmal in kWh, einmal in MWh (`/ 1000`).
3. Vier Summen: `float`-Akkumulator und `Enumerable.Sum` (`double`-Akkumulator), je
   Einheit; Referenz ist die `double`-Schleife über die Ausgangsreihe.
4. Dazu `MathF.BitIncrement` für die ULP-Tabelle in 4.3.

Die Zahlen in Kapitel 4.3 sind die Ausgabe dieses Programms unter .NET 10 auf Linux
x64, gerechnet am 07.09.2026.

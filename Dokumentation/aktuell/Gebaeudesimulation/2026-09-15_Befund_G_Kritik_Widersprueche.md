# Befund G — Vollständigkeits- und Widerspruchsprüfung der Befunde D bis F (15.09.2026)

**Protokoll.** Befund eines Prüf-Agenten (Modell Opus, Workflow ‚gebaeudemodell-pruefen‘, Stufe Kritik) im Auftrag des Konzepts [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Sitzung vom 15.09.2026. Wortlaut wie geliefert; Zeilenbelege und Zahlen sind im Konzept nach dem Gegenlesen korrigiert, bei Widerspruch gilt das Konzept. Pfade unter `C:\Users\…pos-spike\` bezeichnen die Prüfwerkzeuge außerhalb des Repositoriums.

---

## Befund „Kritik" — Vollständigkeits- und Widerspruchsprüfung der drei Befunde

**Prüfweg (nur lesend):** 24 Datei:Zeile-Belege im Repository nachgeschlagen; die Referenz-CSV der 13 Projekte mit Perl neu summiert und ihre Maxima neu bestimmt; die Rohausgaben beider Prototypen in `C:\Users\Dirk\AppData\Local\Temp\epos-spike\` gegen die Befundtexte gehalten. Die Testdatenbank wurde **nicht** geöffnet. Im Repository nichts angelegt oder geändert (`git status --porcelain` → weiterhin 24 Einträge).

---

## 1. Widersprüche zwischen den Befunden

### W1 — Stunde des Maximums: 0-basiert gegen 1-basiert (betrifft 5 Projekte)

> Daten, F: „1007 | … | 34,991 | **451 (19. Jan, 19 h)**"
> Prototyp 2, Spitzenlast: „1007/1046 | … | heute max kW 34,99 | **452 (19. Jan, 20 h)**"

Nachgemessen in `Referenzlaeufe/2026-09-11_R7_Speicherflotte/Projekt_1007/waermebedarf_gebaeude.csv` (Spalte `Index` beginnt bei 0): Maximum 34 991,243 W bei **Index 451**, also Tag 19, Stunde-des-Tages 19 (0-basiert). Beide Befunde meinen dieselbe Stunde, zählen aber verschieden; Prototyp 2 zählt durchgängig 1-basiert (bestätigt in der Rohdatei `…\Prototyp\out\vergleich_zusammenfassung.csv`, Spalte `heute_max_std` = 452/427/8372/8358/1388 gegen meine gemessenen 451/426/8371/8357/1387). Betroffen: 1007, 1017, 1018, 1023/1024, 1039, 1040–1045, 1046.
**Zusatz, den keiner der beiden sagt:** Beide Uhrzeiten sind **UTC-Etiketten**, weil der Gebäudeweg die Ortszeitkorrektur nicht anwendet (s. L6). Ein Konzeptpapier, das „19 h" oder „20 h" schreibt, behauptet unbelegt eine Ortszeit.

### W2 — Katalogkennzahl von Gebäude 10576: 112,5 gegen 112

> Daten, B: „10576: 54,1 vs. **112,5**"
> Prototyp 2, §3: „Katalog kWh/m²a … **112**" und §7: „trifft die Katalogkennzahl **112** kWh/m²a praktisch exakt"

0,5 kWh/m²a Unterschied, aber der Satz „der Prototyp erreicht 100,7 % der Katalogkennzahl" hängt daran (112,8/112 = 100,7 %; 112,8/112,5 = 100,3 %). Der Wert steht in der DB; er muss vor Übernahme auf eine Nachkommastelle festgezurrt werden.

### W3 — Lizenz: zwei verschiedene Rechteinhaber für dieselben Zahlen

> Prototyp 1, §5.1: „Die **VDI-Referenztabellen** sind auf 0,1 K gerundet" — §5.2: „**Referenztabelle** ganzzahlig in W"
> Prototyp 1, §8.7: „**Referenzzahlen stammen aus AixLib** (BSD 3-Clause, © 2010–2018 RWTH Aachen University, E.ON ERC, EBC)"

Das sind zwei unterschiedliche Quellen mit zwei unterschiedlichen Lizenzlagen (VDI 6007-1 → VDI/Beuth, urheberrechtlich geschützte Normtabellen; AixLib → BSD-3). Derselbe Befund führt beide als Herkunft derselben 72 Prüfpunkte je Testfall. Dazu kommt:

> Prototyp 1, §3: „Formeln aus `PartialVDI6007.mo` / `VDI6007.mo` **verbatim abgerufen**" und „Funktionsquelltext `splitFacVal.mo` abgerufen und verifiziert"
> Prototyp 1, §5.5: „Vom Vorlagenmaterial wurde ausschließlich das Datenmodell … gelesen; **kein Quelltext übernommen**"

§5.5 bezieht sich auf das Z:-Vorlagenmaterial, §3 auf AixLib — der Text lädt zur Verwechslung ein. Das Konzept darf beide Sätze **nicht** zu „kein fremder Code, keine Auflage" verschmelzen. Zur Lizenz des Z:-Vorlagenmaterials sagt **kein** Befund etwas.

### W4 — Zeilenzahlen: Prototyp 1 exakt, Prototyp 2 veraltet

| Datei | Befund | gemessen (`wc -l`) |
|---|---|---|
| `Prototyp\*.cs` (7 Dateien) | 227/128/207/162/116/51/93 = **984** | 227/128/207/162/116/51/93 = **984** ✔ |
| `EposLauf\Epos.cs` | 320 | **334** |
| `EposLauf\Program.cs` | 220 | **290** |
| `EposLauf\Altmodell.cs` | 66 | **68** |

Folge: Prototyp 2s Zeilenbelege sind zum Zeitpunkt der Prüfung nicht mehr deckungsgleich mit dem Stand, den der Text beschreibt (stichprobenartig geprüft: `Epos.cs:307` = `var r = sim.Advance(Lasten(h), Regel(h), 3600.0);` — inhaltlich passend; `Epos.cs:137-140` = Kusuda-Kommentar ✔).

### W5 — Rechenzeit der geregelten Rechnung: Faktor 4–5 auseinander

> Prototyp 1, §6: „Ideale Regelung mit Ereignissuche (TC6, TC7, TC11) | **87–121 ms**" je 8 760 Schritte, „bis zu **60 Bisektionsschritte**"
> Prototyp 2, §1: „**Laufzeit gesamt 2,35 s** … rund 100 Jahresläufe à 9480 Stunden" ⇒ **≈ 23 ms** je geregeltem Jahreslauf

Entweder rechnet der EPOS-Adapter einen billigeren Regelweg (plausibel: `QMin = 0`, kein Umschaltereignis innerhalb der Stunde), oder eine der beiden Messungen ist falsch. Solange das nicht geklärt ist, darf keine der beiden Zahlen ins Konzept.

### W6 — Widerspruch zum bereits vorliegenden Konzeptentwurf

Der Entwurf `Dokumentation/aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md` (827 Zeilen, unversioniert) enthält in §4.8:

> „Reine Skalar- und 2×2-Arithmetik, **keine Iteration**, keine Zufälle … alle dreizehn Referenzprojekte in **deutlich unter einer Sekunde**."

Gegen Prototyp 1 §6 („bis zu 60 Bisektionsschritte") und Prototyp 2 §1 (2,35 s). Beide Aussagen des Entwurfs sind durch die Befunde **widerlegt**, nicht gestützt.

### W7 — Parametername: `Fensterflaeche_Ost_West` gegen `Fensterflaeche_Ost`

Beide Befunde sprechen ausschließlich von `Fensterflaeche_Ost_West`. Das ist der DB-Spaltenname (`sql/schema/001_grundschema.sql:1144` für `Tab_Gebaeude`, `:1201` für `Z_ProjektGebaeude`) — im Code heißt das Feld aber `Fensterflaeche_Ost` (`EPOS.Kern/Model/GebaeudeModel.cs:20`, `EPOS.Kern/Model/ProjektGebaeudeModel.cs:26`) und wird aus der Ost-West-Spalte gefüllt (`EPOS.Kern/Controller/GebaeudeCtrl.cs:66`, `GebaeudeStammCtrl.cs:291`). Der Aufruf `SolareGewinneC(…, Sol_w[Tag], Sol_O[Tag], item.Fensterflaeche_Ost, …)` (`SimulationWaermebedarf.cs:826`) sieht im Quelltext aus, als gäbe es eine Ostfläche — es ist die Summenfläche. Die Namensfalle muss ins Papier, sonst sucht der nächste Leser vergebens.

### W8 — Projekt 1041: die Erklärung im Befund „Daten" ist falsch

> Daten, F, Anmerkung: „1041/1042 haben abweichende Gesamtwerte, **weil Prozess- bzw. Brauchwasserprofile dazukommen**"

Gemessen: `Projekt_1041/aggregate.csv` führt `Waermebedarf_Heizung;124.78` MWh, die Gebäudereihe aber nur 59 354,12 kWh. Die Differenz steht in `Projekt_1041/waermebedarf_extern.csv`: Summe **65 429,82 kWh = 65,43 MWh**; 59,35 + 65,43 = **124,78** ✔. Es ist also eine **externe Ganglinie im Heizkanal**, nicht der Prozessanteil (der steht getrennt mit 30 MWh). Prototyp 2 erwähnt 1041 als Mischfall überhaupt nicht — beide Befunde behandeln 1030 als den einzigen Ganglinienfall. **1041 ist ein Gebäude-plus-Ganglinie-Mischprojekt**, und das ist ein Fall, den ein G1-Konzept beschreiben muss.

---

## 2. Stichprobe der Datei:Zeile-Belege (24 geprüft)

**Exakt getroffen (13):**

| Beleg | Inhalt an der Stelle | Urteil |
|---|---|---|
| `EPOS.Kern/Allgemein/BhkwPlan.cs:316` | `s = s * transmissionsgrad * 100.0;` | ✔ |
| `EPOS.Kern/Allgemein/BhkwPlan.cs:342-362` (Prototyp 2) | Rumpf `SpezWaermeverlusteC` mit 0,83/0,95/0,45 | ✔ |
| `EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:571` | `item.Bewohner = item.Z_AuswahlWohnflaeche / item.Flaeche_Nutzer;` | ✔ |
| `…/SimulationWaermebedarf.cs:601-608` | `if (item.Typ == "Wohngebaeude  VDI 2067")` … `StdWerte(ziel, TagTyp_NW, …)` | ✔ |
| `…/SimulationWaermebedarf.cs:650-651` | `VerbrauchAlt = HeizwaermebedarfGeb[index] / 1000;` / `FlaecheNeu = …` | ✔ |
| `…/SimulationWaermebedarf.cs:689`, `:699`, `:701` | Ferien-Logik, Grenze `<= 365` | ✔ |
| `…/SimulationWaermebedarf.cs:777` | `if ((double)item.Raumsolltemperatur_Wochenende > 5)` | ✔ |
| `EPOS.Kern/Controller/KlimadatenCtrl.cs:37` | `…Tab_Klimadaten WHERE ID_Klimaregion={…} ORDER BY ID` | ✔ |
| `EPOS.Kern/Controller/SolardatenCtrl.cs:156-208` | `ReadOrtszeit` von Signatur bis Rumpfende | ✔ |
| `EPOS.Kern/Allgemein/Import/KlimaImportAblauf.cs:132-136` / `:355` | `NEIGUNG_FASSADE = 90`, `AZ_*`; `TagTyp_W = Diffus > 0,5·Global ? 2 : 1` | ✔ |
| `EPOS.Kern/Allgemein/SolarPVGISCalculator.cs:59/63/69/72/75/77/311` | PVGIS-Feldnamen, `ALBEDO_BODEN = 0.2` | ✔ |
| `EPOS.Kern/Allgemein/Bericht/AbweichungsErmittler.cs:128` | `new Merkmal("Gebäude","Tab_Gebaeude","WW_Bedarf","Warmwasserbedarf","kWh/a",0)` | ✔ |
| `sql/schema/001_grundschema.sql:1392-1401` | `Tab_Klimaregion` mit `Bezeichner`, `Klimazone_DIN4710` — **keine** Spalte `Name` (die hat nur `Tab_Klimaregion_STAMM`, :1403) | ✔ |

**Inhaltlich richtig, Zeile daneben (9):**

| Beleg | zitiert | tatsächlich | Versatz |
|---|---|---|---|
| `BhkwPlan.cs` — `pHzg = (tPrev−tAussen)·L + (tSoll−tPrev)·C − innereGewinne` | :419 | **:418** | +1 |
| `BhkwPlan.cs` — Solarfaktor 4,0 (zweite Stelle, Temperaturfortschreibung) | :431 | **:428** | +3 |
| `BhkwPlan.cs` — `a = 1 − exp(−L/C)` | :428 | **:426** | +2 |
| `BhkwPlan.cs` — Kappung `if (tPrev > maxRaumtemp)` (Daten **und** Prototyp 2) | :432 | **:430** | +2 |
| `BhkwPlan.cs` — `return acc * gesamtflaeche / wohnflaeche` | :434 | **:435** | −1 |
| `BhkwPlan.cs` — Gewichte 0,83/0,95/0,45 bzw. WBVK bzw. Lüftung | :323 / :324 / :325-330 | Doku-Kommentar; **Code :347-351 / :353 / :355-359** | ~25 |
| `BhkwPlan.cs` — Rücksetzen bei `day == 1` (Prototyp 2 §7) | :405 | **:398** | +7 |
| `Gebaeudebauweise.cs` — Rückfallwert `return 50;` | :69 | **:66** (Altlast dokumentiert :55-59) | +3 |
| `KlimaImportAblauf.cs` — `WE` = Sa/So | :356 | **:354** | +2 |
| `SolarPVGISCalculator.cs` — `CalculateHourlyHayDavies` | :421 | **:455** (:421 ist der Doku-Kopf) | −34 |
| `SolarPVGISCalculator.cs` — `group.Max(Sonnenwinkel)` / Mittelwertblock | :501 / :489-500 | **:502** / **:494-501** | 1–5 |
| `Directory.Packages.props` — `Microsoft.Data.Sqlite 10.0.11` | :22 | **:20** (Version stimmt) | +2 |
| `Prototyp\Mat2.cs` — `Step` / `Mean` | :117 / :120 | **:124 / :127** | 7 |
| `Prototyp\Cases.cs` — `PowerSign` | :200 | **:204** (Deklaration :20) | 4 |

**Falsch belegt (1):** `…\Prototyp\out\validierung.csv` als Quelle der 12-zeiligen Validierungstabelle (Prototyp 1 §4). Die Datei enthält **eine** Zeile (Testfall 1). Zeitstempel: der `--all`-Lauf schrieb um 16:02:16 alle `testfall_*.csv` und `vergleich_2..12.csv`; um **16:02:22** überschrieb ein Einzellauf für Testfall 1 `validierung.csv` auf einen Datensatz. **Die Aussage selbst stimmt trotzdem** — ich habe die 72 Prüfpunkte je Fall aus den überlebenden `vergleich_<n>.csv` neu ausgewertet:

| TF | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| max \|Abw\| gemessen | 0,0553 | 0,0516 | 0,0590 | 0,0558 | 0,0579 | 1,4986 | 0,6389 | 0,0504 | 0,1363 | 0,1430 | 1,3762 | 0,0538 |
| Befund §4 | 0,055 | 0,052 | 0,059 | 0,056 | 0,058 | 1,499 | 0,639 | 0,050 | 0,136 | 0,143 | 1,376 | 0,054 |

Alle zwölf Werte, je n = 72 Punkte, decken sich. **Das Ergebnis ist belastbar, die zitierte Sammeldatei ist es nicht** — vor Drucklegung `--all` einmal neu laufen lassen.

**Eigene Nachrechnung der Referenzeinheiten (Daten F, bestätigt):** `Projekt_1007/waermebedarf_gebaeude.csv` → 8 760 Zeilen, Summe **53 071 741,26** (Wh), Max **34 991,243** (W); `waermebedarf.csv` → Summe **57 131,44** (kWh), Max **36,4104** (kW); `aggregate.csv` → `Waermebedarf_Gesamt;57.13`, `Waermelast_Max;36.41`. Ebenso geprüft und exakt: 1008 (54 817 809 Wh), 1017 (62 964 709), 1018 (46 881 443), 1023 (329 796 525), 1039 (445 619 744), 1040 (59 354 116), 1030 (**0,00**), sowie `Waermebedarf_Heizung/Brauchwasser/Prozess` für 1023/1030/1041/1042.

**Nicht prüfbar (DB nicht geöffnet):** sämtliche Feldstatistiken aus Daten A/B/C (NULL-Zählungen, `Bauweise = 50 Wh/K` bei 10576, `Ferienbeginn_1 = 366`, Sol-Jahressummen). Sie bleiben Behauptungen des Befunds „Daten" — für ein Konzeptpapier reicht das, wenn die Datei `…\daten\probe_log.txt` (363 Zeilen, vorhanden) als Beleg mitgeführt wird.

---

## 3. Lücken — was ein Konzept wissen muss

### Von mir aus dem Repository beantwortet

**L1 — Mehrere Gebäude je Projekt.** Beantwortet: `SimulationWaermebedarf.cs:190-214` läuft über `ctrl.rows`, rechnet jedes Gebäude einzeln in denselben Puffer `Waermebedarf_EinGebaeude` (der in `HeizwaermeEinesGebaeudes` genullt wird, `:598`), addiert in `kanalHeizung` **und** in `Waermebedarf_Gebaeude` und merkt sich `MaxP[i]` je Gebäude. Es gibt **keine** Kopplung zwischen den Gebäuden, jedes ist eine eigene Zone mit eigenem Klima. 15 Zeilen in 13 Projekten, Maximum 3 je Projekt (1039).

**L2 — Determinismus: ein konkreter Fehler, den kein Befund nennt.** `_prevRoomTemp` ist ein **statisches** Feld (`BhkwPlan.cs:51`), wird nur bei `day == 1` auf die Nachtabsenkung zurückgesetzt (`:398`) und sonst über alle Aufrufe fortgeschrieben (`:433`). `ResetState()` (`:54`) wird in der gesamten Produktion **nirgends** gerufen — nur in `EPOS.Kern.Tests/BhkwPlanRueckgabeTests.cs:91,109,112`. Folge: Gebäude *i+1* startet seine 15 Vorlauftage (350…364) mit der Endraumtemperatur von Gebäude *i*; das Ergebnis eines Projekts **hängt an der Zeilenreihenfolge** und an dem, was vorher im Prozess gerechnet wurde. Der Referenzlauf ist heute nur reproduzierbar, weil die Reihenfolge stabil ist. Für G1 ist das die Musterfalle, die nicht kopiert werden darf — und zugleich ein Grund, die Referenz mit Vorsicht als Wahrheit zu behandeln.

**L3 — Gebäude mit Wohnfläche 0.** Ungeschützt: `BhkwPlan.cs:435` teilt `acc * gesamtflaeche / wohnflaeche` ohne Nullprüfung → ±Infinity bzw. NaN, kein Abbruch, keine Warnung. Ebenso `SimulationWaermebedarf.cs:571` (`/ item.Flaeche_Nutzer`). Interessant: an einer Stelle **ist** der Fall bedacht — `Gebaeudebauweise.cs:44`: `if (wohnflaeche == 0) return SCHWER;`, mit dem ausdrücklichen Kommentar, der Vorläufer habe hier NaN erzeugt. Der Dialog ist abgesichert, der Rechenweg nicht. **G1 braucht eine harte Prüfung `Wohnflaeche > 0` und `Flaeche_Nutzer > 0`** — dieselbe Klasse Fehler wie der `Bauweise`-Rückfallwert, den Prototyp 2 zu Recht als wichtigsten Einzelbefund nennt.

**L4 — Einheit des Ergebniskanals HEIZUNG.** Drei Einheiten in einem Ordner, gemessen:
| Datei / Größe | Einheit | Beleg |
|---|---|---|
| `waermebedarf_gebaeude.csv` | **W je Stunde** | `Referenzlauf/Ergebnisexport.cs:59` exportiert `wb.Waermebedarf_Gebaeude` — das Feld läuft **nie** durch `WattToKw` |
| `waermebedarf.csv`, `waermebedarf_extern.csv` | **kW je Stunde** | `Ergebnisexport.cs:58,62`; `kanalHeizung` wird bei `SimulationWaermebedarf.cs:221` umgerechnet |
| `aggregate.csv`, `Energiebedarf.Waermebedarf_*` | **MWh** (Last: **kW**) | `SimulationWaermebedarf.cs:228` `kanalHeizung.Sum() / 1000` |

Der **Kanal** HEIZUNG selbst führt also **kW je Stunde**, der exportierte Gebäudevektor W. Beide Befunde haben das richtig (Daten F), aber keiner sagt, dass es zwei verschiedene Felder mit verschiedenen Einheiten sind — das ist die eigentliche Falle für den Anschluss eines zweiten Modells.

**L5 — Umfang des HEIZUNG-Kanals.** Der Kanal trägt nicht nur Gebäude: 1030 = 6 137,56 MWh rein aus Ganglinie, 1041 = 59,35 MWh Gebäude **+** 65,43 MWh Ganglinie (gemessen, s. W8). Ein G1-Gebäudemodell ersetzt also nur einen Summanden des Kanals.

**L6 — Ortszeitregel.** Der Gebäudeweg wendet sie **gar nicht** an: er liest Tagesmittel über `KlimadatenCtrl.cs:37` (roh, `ORDER BY ID` = UTC-Tage). `ReadOrtszeit` wird an genau vier Stellen gerufen: `SimulationPV.cs:254`, `SimulationSolarthermie.cs:239`, `KlimaregionCtrl.cs:281` und `SimulationWaermebedarf.cs:915` — letzteres nur in `Stundentemperatur_aus_DB`, deren Doku-Kommentar (`:903-911`) selbst festhält, die Stundentemperatur speise „den COP der Waermepumpe, die Erdreichrechnung und das Reporting". Die Zwei-Zeitbasen-Aussage des Befunds „Daten" ist damit **im Quelltext dokumentiert bestätigt**. Offen bleibt: in welcher Zeitbasis die 7R2C-Ergebnisreihe abgelegt wird und wie sie sich zu PV und Solarthermie stellt, die schon heute in Ortszeit rechnen.

**L7 — Rechenzeitbudget.** Harte Referenzzahl aus dem Repository: `Referenzlaeufe/2026-09-11_R7_Speicherflotte/protokoll.txt` — „**Dauer: 00:00:04**" für **alle 13 Projekte einschließlich** WP, PV, Solarthermie, Speicher und CSV-Ausgabe. Prototyp 2s 2,35 s für zwölf Projekte × fünf Parametersätze bedeutet ~0,5 s je Satz — **derselben Größenordnung wie der gesamte heutige Lauf**. Das ist tragbar, aber es ist kein „unkritisch" mehr; die Zahl gehört mit dieser Bezugsgröße ins Papier.

**L8 — Nichtwohngebäude.** `Wohngebaeude_Nicht_Wohngebaeude` wird in `EPOS.Kern/Allgemein/` **nirgends** gelesen (grep leer) — das Feld ist heute wirkungslos. Die einzige Weiche ist `Typ == "Wohngebaeude  VDI 2067"` → `TagTyp_W` (120 Werte), sonst `TagTyp_NW` (192 Werte), `SimulationWaermebedarf.cs:601-608`. Und die Typangabe ist nicht vertrauenswürdig: Projekt 1007 heißt laut `protokoll.txt` „**Laurentiuskirche**", trägt aber `Typ = "Wohngebaeude  VDI 2067"`, `Gebaeudeart = Einfamilienhaus`, 340 m². Ein NWG-Nutzungsprofil lässt sich an diesen Daten weder ableiten noch prüfen.

**L9 — Kühlung.** In EPOS gibt es keinen Kühlkanal: `EPOS.Kern/Allgemein/DbWerte.cs:1260/1263/1271` kennt `KANAL_HEIZUNG`, `KANAL_BRAUCHWASSER`, `KANAL_PROZESS` — mehr nicht; in `SimulationWaermebedarf.cs` kommt „kuehl"/„kaelte" kein einziges Mal vor. Der Löser **kann** kühlen (`Prototyp\Solver.cs:89` `ControlIntSurface`, Testfall 11 Kühldecke), Prototyp 2 hat es mit `QMin = 0` abgeschaltet. Die 184–1 425 Überhitzungsstunden je Gebäude haben also heute keinen Empfänger.

**L10 — Verbrauchs-Rückrechnung.** Beide Befunde sagen übereinstimmend, dass alle 15 Zeilen den Flächenweg gehen und `Bewohner_und_Flaeche_berechnen` in diesen Projekten nie betreten wird. Prototyp 2 hat den Faktor **nur auf das Ergebnis** angewandt — gegengerechnet: Gebäude 10577 (74 m², Faktor 1) liefert 13 617,2 kWh, Gebäude 10614 (dieselbe Katalogzeile, Faktor 4,5946) liefert 62 565,6 kWh; 13 617,2 × 4,5946 = 62 570,6, Abweichung **0,008 %**. Der Faktor ist also eine reine Nachmultiplikation und trägt keinerlei Physik — genau der Einwand aus Daten G. **Damit ist die Aussage des Konzeptentwurfs §4.7 („Da das Modell linear ist, bleibt die Verbrauchs-Rückrechnung … mit einer Iteration exakt") nicht gedeckt**: linear ist nur die Nachmultiplikation, nicht das Modell. Mit `QMin = 0` ist das 7R2C schon ohne Leistungsgrenze stückweise linear (Prototyp 2 zählt 2 097 Nullstunden = 24 % des Jahres, plus 184–1 425 Stunden über 24 °C im freien Lauf). Wer die fiktive Fläche variiert, statt das Ergebnis zu strecken, bekommt eine nichtlineare Kennlinie.

### Offene Fragen, die keiner der drei Befunde beantwortet

1. **Ost/West — das billigste fehlende Experiment.** Beide Befunde behaupten, ohne Ost/West-Trennung sei die Morgen-/Abendspitze „nicht darstellbar" (Daten G1; Prototyp 2 §6c). **Niemand hat es gemessen.** `Sol_Ost` und `Sol_West` liegen stündlich getrennt in den 13 `klima_*.json`; der Prototyp müsste nur zwei Varianten rechnen („OW-Fläche komplett Ost" / „komplett West") und die Spanne von Spitzenwert und Spitzenstunde angeben. Ohne diese Zahl ist die Pflichtfeld-Forderung #1 aus Prototyp 2 §8 unbelegt.
2. **Mehrere Zonen je Gebäude** und was „ein Gebäude = eine Zone" bei 3 596 m² (10628) bedeutet.
3. **Was bei `Typ`-Werten passiert, zu denen keine `Abfrage_Tagverteilung` existiert** — heute Abbruch (`HeizwaermeEinesGebaeudes` → `false` → `return`, `:197`); für G1 nicht geregelt.
4. **Übergangsregel Alt/Neu:** Was ein Projekt tut, in dem ein Gebäude auf G1 steht und ein zweites auf dem Tagesmodell — Zeitbasis, Spitzenlastdefinition, Vergleichbarkeit der Reihen.
5. **Wie die Referenzläufe fortgeschrieben werden**, wenn das neue Modell per Definition 7–33 % andere Zahlen liefert (Regressionsschwellen der `Wächter`-Tests).
6. **Abnahmekriterium.** Beide Prototypbefunde sagen richtig, dass die Übereinstimmung mit dem 1R1C kein Gütebeweis wäre — aber keiner nennt ein positives Kriterium: woran wird G1 in EPOS abgenommen, wenn gemessene Verbräuche fehlen?
7. **Lizenzstatus des Z:-Vorlagenmaterials** (s. W3).
8. **`.sqlite-wal` / `.sqlite-shm`**: In `Referenzlaeufe/` liegen seit 15:32/15:33 zwei Beistelldateien zur Testdatenbank (WAL 0 Byte, SHM 32 KB), während die `.sqlite` selbst von 09:23 unverändert ist. Es wurde nichts geschrieben, und beide sind per `.gitignore:410-411` ausgenommen (deshalb weiterhin 24 Einträge in `git status`). Die Formulierung beider Prototypbefunde „im Repository wurde **keine Datei angelegt**" ist trotzdem streng genommen unzutreffend; für das Papier genügt „nichts versioniertes geändert, nichts geschrieben".

---

## 4a. Belastbare Kernaussagen — das darf das Konzept übernehmen

1. Der Referenzlauf umfasst **13 Projekte mit 15 Gebäudezeilen**; Projekt **1030 hat kein Gebäude** (Gebäudereihe exakt 0,00 Wh, selbst nachgerechnet) und prüft nur den Ganglinienweg.
2. `waermebedarf_gebaeude.csv` führt 8 760 Stundenwerte in **Watt**; die Jahressumme von Projekt 1007 beträgt **53 071 741,26 Wh = 53 071,74 kWh** und trifft `Energiebedarf.Waermebedarf_Heizung;53.07` MWh (eigene Nachrechnung).
3. `waermebedarf.csv` und `waermebedarf_extern.csv` stehen in **kW**, `aggregate.csv` in **MWh** bzw. **kW** (1007: Summe 57 131,44 kWh gegen `Waermebedarf_Gesamt;57.13`).
4. Der heutige Gebäudeweg ist ein **1R1C mit Stundenschritt auf Tagesmittel-Klimadaten**: `TaeglHeizlastWG` führt eine einzige Raumtemperatur (`BhkwPlan.cs:426-430`), eine einzige Kapazität `Bauweise` und kappt hart bei `Maximaleraumtemperatur` (`BhkwPlan.cs:430`).
5. Die solaren Gewinne erreichen das Gebäude ausschließlich in den **Stunden 9…14 mit Faktor 4,0** (`BhkwPlan.cs:420` und `:428`); die Tagesenergie bleibt erhalten, die Stundenauflösung aus `Tab_Solar` wird vom Gebäudeweg nicht genutzt.
6. Im Wärmeverlustkoeffizienten stecken **fest verdrahtete Gewichte 0,83 (Wand) / 0,95 (Dach) / 0,45 (Grundfläche) / 0,83 (Wärmebrücken)** (`BhkwPlan.cs:347-353`); sie senken L um **14–26 %** gegenüber den ungewichteten U·A-Werten (Prototyp 2, sieben Gebäude einzeln beziffert).
7. Der Skalierungsfaktor ist eine **reine Nachmultiplikation des Ergebnisses** (`BhkwPlan.cs:435`), Spanne **0,2531 (1018) bis 4,5946 (1007/1046)**; er ändert weder L noch C noch Flächen (eigene Gegenprobe: 0,008 % Abweichung zur Proportionalität).
8. `Bauweise` ist eine **Dreipunktwahl 20/50/100 Wh/(m²K)** mit einem stillen Rückfallwert `50` bei Index außerhalb 0..2 (`Gebaeudebauweise.cs:61-66`, Altlast dort dokumentiert); Gebäude 10576 trägt diesen Rückfallwert, was das Altmodell um 34 % nach unten drückt.
9. **Die DB führt Ost und West als eine Summenfläche** `Fensterflaeche_Ost_West` (`sql/schema/001_grundschema.sql:1144`, im Code als `Fensterflaeche_Ost` geführt), während `Sol_Ost` und `Sol_West` stündlich getrennt vorliegen — der wichtigste Datenmangel für eine Stundenrechnung (Wirkung allerdings noch **unbeziffert**, s. L-Frage 1).
10. **Zwei Zeitbasen im selben Lauf**: Stundenreihe über `SolardatenCtrl.ReadOrtszeit` (`:156-208`, UTC→MEZ/MESZ, ganze Zeilen, Warnung bei ≠ 8 760 Zeilen), Tageskalender roh in UTC (`KlimadatenCtrl.cs:37`) — im Quelltext selbst dokumentiert (`SimulationWaermebedarf.cs:903-911`).
11. Der Löser des Prototyps besteht **alle zwölf VDI-6007-1-Testfälle**, je 72 Prüfpunkte; die Maximalabweichungen (0,050–0,143 K bzw. 0,64–1,50 W) habe ich aus den Rohdateien unabhängig nachgerechnet und bestätigt.
12. Die Diagonalisierung der Systemmatrix (Strukturfehler der Vorlage) versagt quantitativ: **bis 304 K bzw. 11 491 W** Abweichung (`--diag`, Prototyp 1 §5.5).
13. Der Prototyp liegt **+7,3 bis +33,0 %** über dem Altmodell (Sonderfall 1008 +89,4 % wegen des `Bauweise`-Datenfehlers); die Prozentzahlen sind aus den Jahressummen beider Modelle nachrechenbar und stimmen.
14. Auf **Tagesebene** stimmen beide Modelle mit **r = 0,982…0,997** überein; der Bruch sitzt in der Stundenverteilung.
15. Zerlegung der Abweichung: **zwei Drittel Parametrierung** (Gewichte +14…+26 %, F_F·F_S +7…+16 %), **Rest −5 bis −13 % reine Modellstruktur** — der 7R2C ist bei gleichen Randbedingungen sparsamer.
16. **Die RC-Strukturparameter sind für die Jahresenergie irrelevant**: C-Aufteilung 0,2/0,8 bis 0,5/0,5 und Massenknoten in Wandmitte ändern **≤ 0,1 %**; `F_S` dagegen **2,67 % je 0,2** (Empfindlichkeitsprobe 1045).
17. Die Erdreich-Randbedingung ist **kein Detail**: Kusuda gegen den Altmodell-Faktor 0,45 unterscheidet sich um **2,3 bis 14,1 %**, streng nach dem Grundflächenanteil an der Hülle.
18. Die Pauschalvariante „θ_eq = θ_out + 0,6·I/25 − 3 K" zeigt in **allen** Projekten in die falsche Richtung (+3,2 bis +7,1 %) und ist für G1 unbrauchbar.
19. Die Spitzenlast ist mit den heutigen Daten **nicht belastbar**: ohne Leistungsbegrenzung fällt die Jahresspitze in 10 von 12 Projekten auf dieselbe Aufheizstunde; als Tagesmittel schrumpft die Differenz von +43 % auf +18…19 %.
20. `Klimazone_DIN4710` ist in allen 13 Regionen leer; die Spalte existiert (`001_grundschema.sql:1399`), ist aber nicht belegt — ein DIN-4710-Testreferenzjahr lässt sich nicht wählen.
21. Der Referenzlauf **aller 13 Projekte einschließlich Anlagensimulation dauert 4 Sekunden** (`protokoll.txt`) — das ist das Zeitbudget, an dem G1 zu messen ist.
22. Die Referenz stammt selbst aus dem 1R1C; **Übereinstimmung wäre kein Gütebeweis**. Der Gütebeweis des Lösers sind die zwölf Normtestfälle.

## 4b. Das darf das Konzept **nicht** übernehmen

1. **Keine Uhrzeit-Angabe zu einer Spitzenstunde**, bevor 0-basiert/1-basiert und UTC/Ortszeit festgelegt sind (W1). „451" und „452", „19 h" und „20 h" bezeichnen dieselbe Stunde.
2. **Nicht „alle dreizehn Referenzprojekte in deutlich unter einer Sekunde"** (Konzeptentwurf §4.8) — durch Prototyp 2 (2,35 s) und Prototyp 1 (87–121 ms je geregeltem Jahreslauf) widerlegt.
3. **Nicht „keine Iteration"** — die ideale Regelung braucht laut Prototyp 1 §6 bis zu 60 Bisektionsschritte je Ereignis.
4. **Nicht „weil das Modell linear ist, ist die Verbrauchs-Rückrechnung in einer Iteration exakt"** (§4.7) — mit `QMin = 0` ist das Modell schon ohne Leistungsgrenze stückweise linear (24 % Nullstunden gemessen).
5. **Nicht die Zahl 23 ms bzw. 121 ms je Jahreslauf** — die beiden Befunde widersprechen sich um Faktor 4–5 (W5).
6. **Nicht „die Referenzzahlen stammen aus AixLib" und gleichzeitig „aus den VDI-Referenztabellen"** — Herkunft und Rechteinhaber müssen vor Drucklegung geklärt und einmal genannt werden; ebenso, dass Formeln aus `.mo`-Quelltext übernommen wurden (W3).
7. **Nicht „kein fremder Quelltext im Prototyp"** als pauschaler Satz — das gilt für das Z:-Vorlagenmaterial, nicht für die AixLib-Formeln.
8. **Nicht `…\Prototyp\out\validierung.csv` als Beleg der 12-Zeilen-Tabelle** zitieren, solange die Datei nur eine Zeile führt.
9. **Nicht „1041/1042 weichen wegen Prozess- bzw. Brauchwasserprofilen ab"** — bei 1041 sind es 65,43 MWh **externe Ganglinie im Heizkanal** (W8).
10. **Nicht die Katalogkennzahl 112 kWh/m²a für 10576** ohne Klärung gegen die 112,5 des Datenbefunds (W2), und damit auch nicht „trifft praktisch exakt".
11. **Nicht „`Fensterflaeche_Ost`" als DB-Feld** und nicht „`Fensterflaeche_Ost_West`" als Codefeld (W7).
12. **Nicht „ohne Ost/West-Trennung ist die Morgen-/Abendspitze nicht darstellbar"** als gemessene Aussage — sie ist plausibel, aber unbeziffert; als Vermutung kennzeichnen oder die zwei Varianten nachrechnen.
13. **Nicht die BhkwPlan-Zeilennummern 419 / 428 / 431 / 432 / 434 / 323 / 324** — richtig sind 418 / 426 / 428 / 430 / 435 bzw. 347-359 (Abschnitt 2).
14. **Nicht `SolarPVGISCalculator.cs:421` für `CalculateHourlyHayDavies`** — die Methode steht auf **:455**.
15. **Nicht `Gebaeudebauweise.cs:69`** für den Rückfallwert — er steht auf **:66**.
16. **Nicht `Directory.Packages.props:22`** für `Microsoft.Data.Sqlite` — Zeile **:20** (Version 10.0.11 stimmt).
17. **Nicht die Zeilenzahlen des EposLauf-Projekts** (320/220/66) — gemessen 334/290/68.
18. **Nicht „im Repository wurde keine Datei angelegt"** ohne den Zusatz, dass `Kenndaten_Test.sqlite-wal/-shm` als ignorierte Beistelldateien entstanden sind (leer bzw. ohne Schreibvorgang).
19. **Nicht „das Nutzungsprofil ist über `Wohngebaeude_Nicht_Wohngebaeude` gesteuert"** — das Feld wird im Rechenkern nirgends gelesen; gesteuert wird allein über `Typ`.
20. **Keine Aussage über Determinismus des Bestands**, ohne den statischen `_prevRoomTemp` zu nennen (L2) — der heutige Lauf ist reihenfolgeabhängig.
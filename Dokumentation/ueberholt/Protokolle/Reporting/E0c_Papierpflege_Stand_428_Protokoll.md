# E0c — Papierpflege der Wirtschaftlichkeit auf den Stand vom 22.09.2026

**Datum:** 22.09.2026 · **Statuszeile:** #429 in
[`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md) · **Zweig:** `we-papiere`
(eigener Worktree, von `3b71871c` abgezweigt, nicht gepusht)

**Anlass:** Der Anwender hat am 22.09.2026 entschieden, das Mockup
[`Dialog_Formel_Zahlenprobe.html`](../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html)
umzusetzen. Die Konzeptpapiere der Wirtschaftlichkeit standen auf dem Stand vom 19./20.09.2026 und
hätten die Umsetzungswellen auf falsche Voraussetzungen gesetzt: Zielversion 96 statt 100, die
Entscheide A1–A20 und Q1–Q25 als offen geführt, obwohl sie am 20.09.2026 nach Empfehlung gefallen
sind, die Etappen E0, E1 und E2 ohne Standmarke, und vier Schemaschritte (97–100), die der Plan noch
für sich beanspruchte, obwohl sie inzwischen anderweitig vergeben sind.

**Auftrag:** Den gültigen Stand eintragen — Code, Schema, gebaute Etappen und die Entscheide. **Keine
neuen Entscheide treffen**, keine Datei umbenennen oder verschieben, kein Code, kein Build, kein
Test. Jede geänderte Standaussage trägt ihre Quelle (Statusnummer oder Protokoll); was nicht belegbar
war, blieb unverändert und steht in § 4 dieses Protokolls.

**Commits (fünf, in der Reihenfolge):**

| SHA | Betreff | `git diff --stat` |
|---|---|---|
| `9aeb5a2c` | E0c: Konzept Wirtschaftlichkeit auf Stand #428 (Kopf, 6, 7) | 1 Datei, +138 / −53 |
| `09673ec2` | E0c: Analyse- und Pruefpapier – Entscheide 20.09., Etappenstand | 2 Dateien, +255 / −60 |
| `dd0518f2` | E0c: Szenarien/VALERI und Nutzungsdauer nachgezogen | 2 Dateien, +55 / −13 |
| `68456203` | E0c: Rechenwege – Befunde mit Standmarken nachgezogen | 5 Dateien, +10 / −8 |
| `8d1e6556` | E0c: Mockup-Anhang Umsetzungsstand nachgezogen | 1 Datei, +22 / −4 |

Dazu dieses Protokoll und die Indexzeile.

**Zeilenzahlen vorher → nachher:**

| Datei | vorher | nachher |
|---|---|---|
| `aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` | 2 670 | 2 755 |
| `aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md` | 405 | 567 |
| `aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Pruefung_Mockups_Wirtschaftlichkeit.md` | 374 | 407 |
| `aktuell/Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md` | 597 | 620 |
| `aktuell/Konzept_Nutzungsdauer_AfA_EPOS-Plan.md` | 264 | 283 |
| `aktuell/Mockups/Dialog_Formel_Zahlenprobe.html` | 5 066 | 5 084 |

Alle elf berührten Dateien sind UTF-8 **ohne BOM** mit **CRLF** im Arbeitsbaum — vor dem Schreiben mit
`head -c 3 | od -An -tx1` und `file` gemessen, danach gegengeprüft. Am Mockup zusätzlich: `<tr`-Zahl
vorher 494, nachher 494; keine doppelten `id`-Werte; alle Diff-Blöcke liegen ab Zeile 4 528, also
vollständig im Anhang.

---

## 1 Der gemessene Stand — die Grundlage aller Änderungen

Gemessen am Codestand `3b71871c` (Worktree, keine Bau- und Testläufe):

| Größe | Messung | Fundstelle |
|---|---|---|
| `SchemaStand.Zielversion` | **100** | `EPOS.Kern/Allgemein/Update/SchemaStand.cs:341` |
| Schritt 97 | Szenario und Bezugsjahr der Klimaregion (KL‑6) | `SchemaKatalog.Schritt97_KlimaSzenario` |
| Schritt 98 | BHKW-Gesamtwirkungsgrad als Faktor (BW‑1) — reines DML, kein `SchrittNN_`-Name | `SchemaStand.cs:291` |
| Schritt 99 | die zwei Wirkungsgrade des BHKW (BW‑1) | `SchemaKatalog.Schritt99_BhkwWirkungsgradAnteile` |
| Schritt 100 | Vorgabe 0 der Fremdschlüsselspalten (FK‑1) | `SchemaStand.cs:320`, Statuszeile #426 |
| nächster freier Schritt | **101** | abgeleitet |
| Rechenaufruf des Berichts | unverändert in der Windows-Schale | `WindowsFormsApplication1/Allgemein/Bericht/BerichtsDatenSammler.cs:180` und `:421` |
| die vier nahtlosen Hüllen (E3 Schritt 1) | unverändert in `WindowsFormsApplication1/Views/` | `Views/Wirtschaftlichkeit/WirtschaftlichkeitParameterHuelle.cs`, `Views/Kosten/{KostenfaktorKatalogHuelle,VorlagenUebernahmeHuelle,ErtragBonusGaben}.cs` |
| `KostenKomponenteHuelle` (E3 Schritt 5) | unverändert in `Views/Kosten/` | `WindowsFormsApplication1/Views/Kosten/KostenKomponenteHuelle.cs` |
| `IosProjektQuelle.BerichteKostenGaben` | **nicht überschrieben** — die Vorgabe liefert `null` | `EPOS.UI/Dienste/IProjektQuelle.cs:272`; `EPOS.iOS/Dienste/IosProjektQuelle.cs` ohne Treffer |
| Positivliste der Wurzel | `EPOS.UI/Seiten/AppWurzel.razor:1590–1611`, **18 Schlüssel** (vorher `:1431–1445`) | mit #428 um fünf Masken gewachsen |
| Ordner unter `EPOS.UI.Daten/` | `Allgemein`, `Assistent`, `Bedarf`, `Klimadaten`, `Kosten`, `Projekt`, `Pufferspeicher`, `Simulation`, `Strom`, `Stromspeicher` — **kein** `Wirtschaftlichkeit` | |
| Fenster-Adapter aus #428 | `KlimadatenFenster`, `ProjektKopieFenster`, `PeakShavingFenster`, `StromganglinieAdminFenster` | `WindowsFormsApplication1/Views/{Admin,Projekt,Stromspeicher,Stromverbraucher}/` |
| Anker- und Testklassen aus E1 | `WirtschaftlichkeitAnkerTests`, `SteuerGutschriftRechnerTests`, `EegSatzRechnerTests`, `PvErloesRechnerEegTests`, `BerichtBlattstrukturWacheTests`, `WirtZeileFormatWacheTests` | `EPOS.Kern.Tests/` |
| Kohärenzfall R5 | gebaut | `EPOS.Kern/Allgemein/Wirtschaftlichkeit/KohaerenzPruefung.cs:537` (`Co2DoppelansatzBehg`) |
| `WIRT_EMPF_KEINE` | parametriert: „… gegenüber **{0}** wirtschaftlich …" | `EPOS.Kern/MyResource/Resource.resx:16146` |

**Nicht erreichbar:** `Z:\20-Development\Wirtschaftlichkeit\BHKW-WP-Plan_Vorlagen\` und
`Z:\20-Development\BHKWPlan\` gaben beide „No such file or directory". Der Satz zur Zulieferung der
BHKW-Plan-Mappen bleibt deshalb stehen (§ 6.3 Nr. 20 des Konzepts).

---

## 2 Was je Papier geändert wurde

### 2.1 `aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`

| Abschnitt | Änderung | Quelle |
|---|---|---|
| Kopfzeile | Stand 22.09.2026, Codestand `3b71871c`, Zielversion **100**, Schritte 90–100 vergeben, neue ab **101**; neuer Absatz mit den Namen der Schritte 97–100 und dem Satz, dass eine Nummer erst bei der Umsetzung fällt | `SchemaStand.cs`, `SchemaKatalog.cs`, #426 |
| § 2.11.2 (V‑G2) | Degradation: **A5 entschieden 20.09.2026 nach Empfehlung** — der Entscheid „G3 nicht umsetzen" gilt, V‑E ohne Degradation | #405, Auftrag des Anwenders |
| § 2.11.4 | Tafel V‑A…V‑E um die Spalte „Stand im Etappenplan E0–E12" (V‑A = E5, V‑C/V‑D = E8, V‑E = E9); der Absatz „Entscheid A5 offen" wird zum Entscheid | Analysepapier § 5, #405 |
| § 2.13 (5) | Ordnerliste von `EPOS.UI.Daten` nachgemessen; neuer Absatz: **A1 entschieden** (E3 vor der Ergebnisansicht) mit dem Hüllen- und Fenster-Adaptermuster aus #428 | Code, #428, Protokoll KI‑F8 |
| § 2.14/U‑1-Block | „U‑1 bekommt Schritt 103" → „bekommt seine Nummer bei der Umsetzung (nächster freier Schritt: 101)" | `SchemaStand.cs` |
| § 6.1 | drei neue Etappenzeilen: **E0 (#379)**, **E1 (#380)**, **DL‑2e (#390)**; die E2-Zeile trägt jetzt **#405** | #379, #380, #390, #405 |
| § 6.2 | vollständig neu gefasst: die neun Anker aus E1 mit Herkunftsspalte; die **zwei Abweichungen zum bisherigen Konzepttext** als Befund (Kaskade 1042 ±0,00 € statt +20.927,61 €; Kapitalwert 1024 −2.896.359,13 € statt −2.220.322,32 €, Differenz −676.036,81 € zu E7); 1030 im Kapitalwert verankert, Betriebskosten weiter offen; 1007/1017/1045/1046 ohne gebuchten Ergebnisstand | #380, Protokoll E1 |
| § 6.3 | neuer Block „Aus Etappe E1 (#380)" mit **R4 Kaskadenrunde 2**; der E2-Block trägt **#405**; Nr. 20 um A17, `_kap` und die nicht erreichbare Z:-Ablage; Nr. 21 als **A11 entschieden und mit E1 gebaut**; die Punkte **29, 30, 31** ausdrücklich als **nicht** vom Entscheid des 20.09. gedeckt gekennzeichnet | #380, #405, Protokolle E1/E2 |
| § 7 | B8 auf **S‑2 (≡ A3)** und **B‑6** gekürzt — V‑3-Rest und I‑5 sind mit #405 erledigt, beide Reste laufen in E7; Absatz „Wiederaufnahme 22.09.2026"; der Block „Offene Entscheide vor der nächsten Codeetappe" wird zur **Entscheidtafel** A1/A2/A5/A11/A13 mit „entschieden 20.09.2026 nach Empfehlung" und Stand je Entscheid | #405, Protokoll E2, Auftrag |
| Anhang | B8/B9/V‑A…V‑E/§ 2.13/S3 mit Etappe und Statusnummer; U‑Reihe auf U1…U45; **neue Tafel E0–E12** mit Statuszeile je Etappe; Absatz zu W‑E2, DL‑2, KI‑F2…F8 und KI‑F4 (#423) | #379…#428 |

### 2.2 `aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`

| Abschnitt | Änderung | Quelle |
|---|---|---|
| Kopfzeile | Stand 22.09.2026 (Erhebung vom 19.09. fortgeschrieben), Codestand `3b71871c`, Zielversion 100, nächster freier Schritt 101 | Code |
| vor § 0 | Block „Fortschreibung 22.09.2026 (E0c)": entschieden A1–A20 und Q1–Q25; gebaut E0/E1/E2, DL‑2e, KI‑F4; nicht ausgeführt A13; nächste Etappe E3 | #379…#428 |
| § 0 Satz 2 | Standmarke: Kaskadenrunde 2 **umgesetzt #380**, CO₂-Kohärenzzeile **umgesetzt #405**; die drei rechenwirksamen Lücken bleiben (A2, A3, A4, E7) | #380, #405 |
| § 0 Satz 6 | „Der Nachweis trägt nicht" → **erledigt mit E1 (#380)**, mit Nennung der sechs Klassen; offen bleibt die Referenzlauf-Erweiterung (A11) | #380 |
| § 0 Satz 7 | Standmarke: E0 (#379) hat die Papierarbeit gemacht; der **Degradationswiderspruch ist mit A5 aufgelöst**; offen bleibt A13 | #379, Auftrag |
| § 3.2 | neuer Stand-Block zu P1–P7, alles nachgemessen: P1 unverändert, aber **kleiner als geplant** (#380); P2 unverändert; **P3 neu gemessen** (`AppWurzel.razor:1590–1611`, 18 Schlüssel, fünf Masken mit #428 dazu; `BerichteKostenGaben` weiter `null`); P4–P7 unverändert; Abschluss mit dem Umzugsmuster aus #428 | Code, #380, #428 |
| § 4 | Vermerkblock „entschieden 20.09.2026 nach Empfehlung" für A1–A20 mit der ausdrücklichen Bestätigung von A5, A3 und A4; **neue Tafel „Stand je Entscheid"** (20 Zeilen: Umsetzungsstand und Etappe); Schlussabsatz zu den drei nicht gedeckten Punkten | #405, Auftrag, Konzept § 6.3 |
| § 5 | Etappenzellen: E0 **umgesetzt #379** (Rest A13), E1 **umgesetzt #380**, E2 **umgesetzt #405** (als W‑E2), E3 **offen, nächste Etappe**; neuer Block „Stand der Etappen" mit je einem Absatz zu E0, E1, E2 (samt dem, was „Nach #405" offen ließ) und einer **Tafel mit dem Stand aller acht Schritte von E3**; Absatz zur Wiederaufnahme am 22.09.2026 | #379, #380, #405, Code |
| § 6 | von „Schemaschritte ab 97" auf **„Schemaschritte dieser Etappen"** umgestellt: 97–100 sind vergeben, die geplanten Schritte heißen fortan **A–H** und bekommen ihre Nummer bei der Umsetzung (heute ab 101); Spalte „vormals" hält die alten Nummern für Verweise fest | `SchemaStand.cs`, #382, #383, #426 |
| Verweise | A2 → Schritt A, A6 → Schritt E, A9 → Schritt G; E7-Zeile auf A/E/F/G (und um B‑6 sowie den Hi/Ho-Leser ergänzt), E9-Zeile auf B/C/D und „ohne Degradation (A5)"; § 0 Satz 5 auf A–G | § 6 |
| § 7 | Stand-Block: die Berichtigungen sind mit **#379** ausgeführt; was E0c nachgezogen hat; was offen bleibt (help_mapping, Hilfesystem § 13.2, A16, A17); Hinweis, dass Schritt 97/103 in der Tafel die Buchstaben A/G meinen | #379, #405 |

### 2.3 `aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Pruefung_Mockups_Wirtschaftlichkeit.md`

| Abschnitt | Änderung | Quelle |
|---|---|---|
| Kopfzeile | Stand 22.09.2026, Codestand `3b71871c`; Schemastand 94 zur Prüfzeit, heute **100**, nächster freier Schritt 101 | Code |
| § 3 Kopf | Der Stern („erst nach dem Entscheid") bedeutet keine Blockade mehr; Stand-Block zur Lesart der Standmarken | #405 |
| § 3.3 | **13 Zeilen** mit Standmarke versehen — `KohaerenzPruefung` (#405, R5), `EnergietraegerPreiskarte` N4 (#405, Q7), `WirtschaftlichkeitZeilen` (Reihenfolge #405/Q19, Q16 und U6 offen), `BausteineWirtschaftlichkeit` (Bandbreite #405, Spaltengruppe offen), `Resource.resx` (teilweise #405, `WIRT_ENK_ANLAGE` offen), `KostenKomponenteHuelle` Reiter (#405), `ErtragBonus` (#405), `VorlagenZeile` (#405), `KostenKomponenteDialog` (Knopfsichtbarkeit #405, Fußleiste #390, Rest offen), `PhotovoltaikVerguetungDialog` (#405), `BhkwWirtschaftlichkeitDialog` (#405, Vorschau offen), die drei Katalogdialoge (#405), die elf Leisten-Dialoge (zwei mit #390), `help_mapping.txt` (#405), `EPOS.UI.Daten/` (offen, Muster seit #428) | #390, #405, #428 |
| § 4 | Vermerkblock „entschieden 20.09.2026 nach Empfehlung" für Q1–Q25; die vier mit #405 umgesetzten Fragen (Q3, Q7, Q12, Q19), Q16 für E5, Q8 teils mit #390, Q14 in E3; **die sieben Fragen mit Restentscheid** (Q9, Q11, Q15, Q18, Q20, Q22, Q23) benannt | #390, #405, Auftrag |
| § 5 | Vorspann mit der Zuordnung P6≈E4, P7≈E5, P8≈Teil von E3, P9≈E12 und dem **Befund, dass P5 keine Entsprechung im Etappenplan hat**; Stand je Etappe in allen neun Zeilen (P1 #379, P2 20.09.2026, P3 #405, P4 teils #405, P5–P9 offen) | #379, #390, #405 |

### 2.4 `aktuell/Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md`

| Abschnitt | Änderung | Quelle |
|---|---|---|
| Kopf | Stand 22.09.2026, Codestand, Zielversion 100/neue ab 101; A5 als Entscheid genannt | Code, Auftrag |
| § 7.3 G3 | „Entscheid A5 offen" → **„Bestätigt mit Entscheid A5 vom 20.09.2026"**: der Widerspruch ist zugunsten dieses Papiers aufgelöst, V‑E ohne Degradation | Auftrag, Konzept § 2.11.4 |
| § 9.1 | Der Satz „**Nachzuziehen:** Die Ressource `WIRT_EMPF_KEINE` nennt weiterhin das Stammprojekt" ist **gestrichen** — die Ressource trägt den parametrierten Text „gegenüber {0}" | `Resource.resx:16146`, #405 (G9) |
| § 10.3 | „heutige Schemastand (19.09.2026: Zielversion 96, neue ab 97)" → **22.09.2026: 100, neue ab 101** | `SchemaStand.cs` |
| § 11 | neue Tafel: V‑4 → **E5** (Abdeckung selbst E9), K8/V‑1 → **E5** (Verlauf E6), V‑G10 → **E8**; alle drei weiterhin nicht gebaut; Absatz, was mit #405 aus diesem Umkreis erledigt ist (G7, G8, G9) | Analysepapier § 5, #405 |

### 2.5 `aktuell/Konzept_Nutzungsdauer_AfA_EPOS-Plan.md`

| Abschnitt | Änderung | Quelle |
|---|---|---|
| Kopf | Stand 22.09.2026, Codestand, Zielversion 100 (neue ab 101); **ND‑S3 = Etappe E10** | Code, Analysepapier § 5 |
| § 1.5 | „Zielversion 96 (Stand 19.09.2026)" → **Zielversion 100 (Stand 22.09.2026), Schritte 90–100 vergeben, neue ab 101** | `SchemaStand.cs` |
| § 3 | „Offen aus S2" neu gefasst: die Hülle der Kostenverwaltung liegt weiter in der Windows-Schale (nachgemessen), der Umzug ist **E3 Schritt 5** (A1, Q14), das **Fenster-Adaptermuster liegt seit #428 vor**; **A7** (Speicherflotte mit ND‑S3), **A8** (geräteeigene Spalten nicht jetzt, nur kennzeichnen) und **A6** (Kennzeichen je Position, E7) als entschieden 20.09.2026 | Code, #428, Analysepapier § 4 |

### 2.6 Rechenwege (nur „Befunde und offene Punkte"; an Formeln und Zahlen nichts)

| Datei | Änderung | Quelle |
|---|---|---|
| `Rechenweg/01_Investitionskosten_BHKW.md` | I‑2 und I‑3 von ⚠ auf ✔ (erledigt, Konzept § 4); **Runde 2 der Kaskade umgesetzt #380** (R4, `InvestKaskadeTests`); **I‑5 umgesetzt #405** | #380, #405, Konzept § 4 |
| `Rechenweg/02_Betriebskosten_BHKW.md` | **B‑7 umgesetzt #405** (Bezugsmenge aus dem `BemessungKatalog`); B‑6 ausdrücklich offen, mit S‑2 in E7 | #405, Protokoll E2 |
| `Rechenweg/04_Energiekosten.md` | die CO₂-Zeile als **R5 umgesetzt #405** (`Co2DoppelansatzBehg`, `KohaerenzCo2Tests`); neue Zeile **R6 umgesetzt #405**; neue Zeile **R11** (Hi/Ho am CO₂-Grenzwert) als offener Entscheid, der vom Entscheid des 20.09. **nicht** gedeckt ist | #405, Konzept § 6.3 Nr. 29 |
| `Rechenweg/05_Verguetungen_BHKW.md` | K‑1 nennt nicht mehr „Schemaschritt 97", sondern **Schritt A** mit Nummer bei der Umsetzung (ab 101) | `SchemaStand.cs`, Analysepapier § 6 |
| `Rechenweg/06_Verguetungen_PV.md` | V‑G5 mit der Zuordnung **V‑E ≡ E9** und dem Hinweis, dass V‑E nach **A5** ohne Degradation je Position geplant wird (die Ertragsdegradation des Dialogs bleibt unberührt) | Analysepapier § 5, Auftrag |

Die Rechenwege `03`, `07` und `08` blieben unverändert: In ihren Befundtafeln steht nichts, was E1
oder E2 belegbar erledigt hätten.

### 2.7 `aktuell/Mockups/Dialog_Formel_Zahlenprobe.html` — nur der Anhang

| Zeile | vorher → nachher | Quelle |
|---|---|---|
| Kopf der Anhangtafel | neuer Stand-Absatz: alle Q1–Q25 und A1–A20 am 20.09.2026 nach Empfehlung entschieden; **seit dem 19.09.2026 ist kein Punkt dieser Tafel gebaut worden**; nächste Etappe E3 | #379…#428 |
| **U1** | „**Schemaschritt 97** (90–96 vergeben)" → „**ein Schemaschritt** (Nummer bei der Umsetzung: 90–100 sind vergeben, der nächste freie ist **101**)"; Chip bleibt `offen` | `SchemaStand.cs` |
| **U32** | dieselbe Ersetzung („**eigener Schemaschritt** …"); Chip bleibt `offen` | `SchemaStand.cs` |
| **U43** | Chip bleibt `Entscheid ausstehend`; Abnahmespalte hält fest, dass **Q18 am 20.09.2026 nach Empfehlung entschieden** ist und die Empfehlung genau lautete, erst die Anhangzeile anzulegen — die Zeile steht, der Folgeentscheid steht aus | Prüfpapier § 4 Q18 |
| **U44** | ebenso | Prüfpapier § 4 Q18 |

**Kein Chip wurde umgestellt.** Kein Gegenstand einer offenen U-Zeile ist seit dem 19.09.2026 gebaut
worden — der Anhang und die Lieferlisten von #379, #380, #390, #405, #423 und #428 wurden
gegeneinander gelesen und überschneiden sich nicht. U43 und U44 behalten ihren Chip, weil die
Empfehlung zu Q18 selbst einen Folgeentscheid vorsah; die drei Chipzustände der Tafel (`offen`,
`erledigt`, `Entscheid ausstehend`) kennen keine Form „entschieden-offen".

### 2.8 `aktuell/Wirtschaftlichkeit_Kosten/LIESMICH.md` — unverändert

Der Wegweiser nennt keine Standaussage, die heute falsch wäre. Alle zwölf Verweise wurden mit `ls`
geprüft und zeigen auf vorhandene Dateien; die Lesereihenfolge nennt nur Papiere, die es gibt. Der
Hinweis „Die Repo-Datei führt — das Artifact ist nicht neu veröffentlicht" gilt unverändert (Q22 ist
entschieden, aber nicht ausgeführt).

---

## 3 Was ausdrücklich **nicht** geändert wurde

* **Keine neuen Entscheide.** Eingetragen wurde nur, was der Anwender am 20.09.2026 („Entscheidung
  nach Empfehlung", A1–A20 und Q1–Q25) und am 22.09.2026 (Umsetzung des Mockups) gesagt hat.
* **A13 ist nicht ausgeführt.** Der Schnitt des konsolidierten Konzepts in drei Papiere (gültiger
  Stand · Entscheidungsregister · Protokoll der Entscheidwege) ist entschieden, aber eine eigene
  Aufgabe; E0c hat kein Papier geteilt, umbenannt oder nach `ueberholt/` bewegt.
* **Keine Formel, keine Zahl, kein Anker** in den Rechenwegen und im Mockup.
* **Kein Code, kein Schemaschritt, kein Build, kein Test.** Die Dokumentationswachen laufen nach dem
  Merge im Hauptbaum.

---

## 4 Nicht belegbar — unverändert gelassen

1. **Rechenweg 01, Zeile 47: „Delta von genau +20.927,61 €".** Diese Zahl steht in der
   Berechnungserläuterung, nicht in der Befundtafel, und ist von der Regel „nichts an Formeln oder
   Zahlen" gedeckt. Sie widerspricht dem mit #380 gemessenen Anker (Kaskade 1042 = ±0,00 €). Der
   Widerspruch ist im Konzept § 6.2 als Befund festgehalten; die Zahlenprobe selbst gehört in eine
   Etappe mit Rechenblick (E7), nicht in eine Papierpflege.
2. **Rechenweg 02, Befund „⚠ B‑1 Kessel-Endenergie ist strukturell 0".** Das Konzept § 6.1 führt
   B‑1/Kesselbrennstoff als mit **#331** erledigt. Die Zeile stand aber nicht auf der Liste der von
   E1/E2 erledigten Punkte, und E0 hat Rechenweg 02 nicht angefasst; die Korrektur braucht eine
   Gegenprobe am Code (`Verbrauch` der Kessel-Modulspalte), die diese Papierpflege nicht führt.
3. **Rechenweg 03 und Mockup U9 — „Degradation mit Quellenangabe".** Ob A5 diesen Punkt miterledigt,
   ist **nicht** sicher: A5 betrifft `V-G2` (Degradation **je Position** als ValERI-Lücke), während
   U9 und Rechenweg 03 die vorhandene **Ertragsdegradation des PV-Dialogs** und die fehlende
   Quellenangabe dazu meinen. Beide Stellen blieben unverändert.
4. **Prüfpapier § 3.1 (Mockup) und § 3.2 (Papiere).** E0a/E0b haben rund 90 Mockup-Stellen und die
   Papiere nachgezogen, aber keine Zuordnung Zeile → Zeile hinterlassen. Ohne diesen Beleg wurde
   keine dieser Zeilen als erledigt gekennzeichnet; der Stand-Block am Kopf des § 3 sagt das.
5. **Prüfpapier § 5, Etappe P5 „Hausstil Dialoge".** Sie hat **keine Entsprechung** im Etappenplan
   E0–E12. Die Arbeit läuft heute unter der Wellenreihe DL‑2; eine Einordnung wäre ein Entscheid und
   wurde nicht getroffen, sondern als Befund vermerkt.
6. **BHKW-Plan-Mappen (Konzept § 6.3 Nr. 20, A17).** `Z:\20-Development\Wirtschaftlichkeit\BHKW-WP-Plan_Vorlagen\`
   und `Z:\20-Development\BHKWPlan\` waren am 22.09.2026 nicht erreichbar. Fundort und Dateinamen
   konnten deshalb **nicht** eingetragen werden; der Satz „wartet auf Zulieferung" bleibt.
7. **Konzept § 6.3 Nr. 29, 30, 31** (Hi/Ho am CO₂-Grenzwert, leere `KWKG_Anlagenart`,
   `Nachweis_Json` in 0 von 78 Ergebniszeilen). Sie sind erst mit E0 und E2 entstanden und tragen
   keine Empfehlung — der Entscheid „nach Empfehlung" vom 20.09.2026 deckt sie nicht. Sie wurden als
   **offen mit eigenem Entscheidbedarf** gekennzeichnet, nicht entschieden.
8. **Die sieben Prüffragen mit Rest** (Q9 Fehlerfarbe, Q11 HT/NT, Q15 Leistungsanteil, Q18
   Folgeentscheid, Q20 Umfang des Parameterdialogs, Q22 Artifact, Q23 Versionsnummer). Sie sind „nach
   Empfehlung" entschieden, aber die Empfehlung lässt je eine Wahl offen; sie wurden als solche
   benannt, nicht aufgelöst.

---

## 5 Offene Aufgaben nach dieser Papierpflege

| Aufgabe | Einordnung |
|---|---|
| **A13 — Schnitt in drei Papiere** (gültiger Stand · Entscheidungsregister · Protokoll der Entscheidwege) | entschieden 20.09.2026, nicht ausgeführt; eigene Aufgabe vor oder neben E3 |
| **E3 Plattform** — acht Schritte, Stand je Schritt im Analysepapier § 5; Muster aus #428 | nächste Codeetappe |
| Die sieben Prüffragen mit Rest und die drei Punkte des § 6.3 (29/30/31) | je ein Wort des Anwenders, mit E7 bzw. vor E12 |
| Referenzmappe der Zahlenprobe benennen (A17) und die BHKW-Plan-Ablage zuliefern | E11 |
| Kapitalwert-Abweichung 1024 (−676.036,81 €) nachrechnen | E7 |
| Betriebskosten 1030 verankern | mit der nächsten Referenzbasis |
| Einordnung von P5 „Hausstil Dialoge" in den Etappenplan | offen, heute unter DL‑2 |

---

## 6 Abnahme

* `git status --short` nach dem letzten Commit **leer**.
* `git diff --stat 3b71871c`: **11 Dateien, +480 / −138** (ohne dieses Protokoll und die Indexzeile).
* Kein Platzhalter, kein Konfliktmarker (`^<{7}`, `^={7}$`, `^>{7}` repoweit in den geänderten
  Dateien: 0 Treffer).
* Alle neuen relativen Verweise mit `ls` geprüft.
* Byteformat je Datei vor und nach dem Schreiben gemessen: UTF-8 ohne BOM, CRLF im Arbeitsbaum,
  unverändert.
* Mockup: `<tr`-Zahl 494 vorher wie nachher, keine doppelten `id`-Werte, alle Diff-Blöcke ab
  Zeile 4 528 (Anhang).
* Kein Bau, kein Test, kein Referenzlauf — die Dokumentationswachen laufen nach dem Merge im
  Hauptbaum.

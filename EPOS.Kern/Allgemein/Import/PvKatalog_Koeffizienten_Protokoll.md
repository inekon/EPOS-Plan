# PV-Katalog: Temperaturkoeffizienten und T_NOCT — Befund, Ursache, Absicherung, Reparatur: Umsetzungsprotokoll

Stand: 02.09.2026 · Branch `ios_migration` · Bezug:
[`Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md`](../../../Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md)
(Paket A, Erkundungsbefund „Katalog vergiftet"). Build x64 Debug out-of-tree: 0 Fehler. Prüfstand-Probe: 0 Fehlschläge.
Datenbasis: 1:1-Kopie der Produktivdatenbank `C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite`
(MD5 `61a3f1d532b608f59178f3f9a29bb221`, 02.09.2026 22:47), ausschließlich lesend.
**Die Produktivdatenbank wurde nicht verändert.**

## 1. Anlass und Soll

Bei der Paket-A-Erkundung (02.09.2026) fiel auf, dass in `Tab_PV` und `Tab_PV_STAMM` bei den
Modulen der Referenzprojekte (1007, 1011, 1026, 1028, 1029) der Wert von `I_Kurzschluss`
(z. B. 9,014) identisch auch in `T_NOCT`, `alpha_SC` und `beta_OC` steht und das
Jinkosolar-Modul `gamma_PMP = 0` trägt. Physikalisch sinnvoll wären T_NOCT ≈ 42–48 °C,
alpha_SC ≈ +0,002…+0,006 A/K, beta_OC ≈ −0,10…−0,15 V/K, gamma_PMP ≈ −0,30…−0,45 %/K.

Auftrag: (1) Datenlage vollständig messen, (2) Ursache im Code belegen, (3) Import und
Katalogdialog absichern (inkl. PAN-Umrechnung, Plausibilitätsprüfung), (4) Reparaturvorschlag
als Skript — Ausführung auf der Produktivdatenbank nur nach Freigabe —, (5) dieses Protokoll.

## 2. Datenbefund (vollständig, beide Tabellen)

Messskript [`sql/pv_katalog/messung_pv_katalog.py`](../../../sql/pv_katalog/messung_pv_katalog.py)
(read-only, URI `mode=ro`), vollständiger Bericht
[`messung_tab_pv_VORHER.md`](../../../sql/pv_katalog/messung_tab_pv_VORHER.md), CSV-Vollabzüge
beider Tabellen daneben. Klassen je Feld: `=I_Kurzschluss` (Wert ≠ 0 und exakt gleich
`I_Kurzschluss`), `NULL`, `0`, `plausibel` (im Fenster), `unplausibel`. Fenster: alpha_SC
0 < x ≤ 0,05 A/K · beta_OC −0,5 ≤ x < 0 V/K · gamma_PMP −1,0 ≤ x < 0 %/K · T_NOCT 20…60 °C.

**Tab_PV_STAMM (6 Zeilen)**

| ID | Bezeichner | Isc | alpha_SC | beta_OC | gamma_PMP | T_NOCT | L × B |
|---|---|---|---|---|---|---|---|
| 5 | Ablytek 6MN6A270 | 9,34 | **0** | **0** | −0,4509 ✓ | **0** | 1,64 × 0,992 |
| 6 | Ablytek 6MN6A275 | 9,42 | **9,42 = Isc** | **9,42 = Isc** | −0,4509 ✓ | **9,42 = Isc** | 1,64 × 0,992 |
| 7 | Ablytek 6MN6A290 | 9,67 | **9,67 = Isc** | **9,67 = Isc** | −0,4509 ✓ | **9,67 = Isc** | 1,64 × 0,992 |
| 8 | Jinkosolar JKM 260P-60 | 9,014 | **9,014 = Isc** | **9,014 = Isc** | **0** | **9,014 = Isc** | 1,65 × 0,992 |
| 9 | LG Electronics LG 320 N1K-A5 | 10,35 | **NULL** | **NULL** | −0,394 ✓ | **NULL** | 1,686 × 1,016 |
| 21 | Philadelphia Solar PS-M144(HCBF)-530W | 13,6 | 0,00272 ✓ | −0,128904 ✓ | −0,385 ✓ | 45,3 ✓ | 0 × 0 |

**Tab_PV (6 Zeilen, Projektkopien)**

| ID | Projekt | Bezeichner | Isc | alpha_SC | beta_OC | gamma_PMP | T_NOCT |
|---|---|---|---|---|---|---|---|
| 1007005 | 1007 | Ablytek 6MN6A270 | 9,34 | **= Isc** | **= Isc** | −0,4509 ✓ | **= Isc** |
| 1007006 | 1007 | Ablytek 6MN6A275 | 9,42 | **= Isc** | **= Isc** | −0,4509 ✓ | **= Isc** |
| 1011008 | 1011 | Jinkosolar JKM 260P-60 | 9,014 | **= Isc** | **= Isc** | **0** | **= Isc** |
| 1015244 | 1026 | Jinkosolar JKM 260P-60 | 9,014 | **= Isc** | **= Isc** | **0** | **= Isc** |
| 1015245 | 1028 | Jinkosolar JKM 260P-60 | 9,014 | **= Isc** | **= Isc** | **0** | **= Isc** |
| 1015246 | 1029 | Jinkosolar JKM 260P-60 | 9,014 | **= Isc** | **= Isc** | **0** | **= Isc** |

**Summe beider Tabellen (12 Zeilen):**

| Feld | plausibel | = I_Kurzschluss | 0 | NULL | unplausibel |
|---|---|---|---|---|---|
| alpha_SC | 1 | 9 | 1 | 1 | 0 |
| beta_OC | 1 | 9 | 1 | 1 | 0 |
| gamma_PMP | 7 | 0 | 5 | 0 | 0 |
| T_NOCT | 1 | 9 | 1 | 1 | 0 |

**11 von 12 Zeilen reparaturbedürftig** (alle außer Stammzeile 21). Die Isc-Signatur trifft
alpha_SC, beta_OC und T_NOCT stets **gemeinsam** (9 Zeilen) — das ist das Muster einer
einzigen fehlerhaften Zuweisung, kein Spaltenversatz (bei einem Versatz stünden drei
verschiedene Nachbarwerte in den drei Spalten). Auffällig außerdem: Die Projektkopie
`1007005` trägt die Signatur, ihre Stammzeile 5 dagegen Nullen — die Stammzeile wurde
**nach** dem Kopieren noch einmal über den Katalogdialog gespeichert (Modulkosten 468,89
gepflegt). Die Access→SQLite-Migration (S7, 02.09.2026) war für beide Tabellen bitgleich
(Migrationsbericht: je 6/6 Zeilen, Hash identisch) — der Befund stammt aus der Access-Zeit.

## 3. Ursache — belegt im Code und in der Git-Historie

Der **heutige** Speicherweg ist korrekt — die vergifteten Werte stammen aus einer
zehn Tage lang gültigen Codefassung vom März 2026, und zwei weitere, noch offene Lücken
haben die übrigen Anomalien erzeugt.

### 3.1 Hauptursache: Kopierfehler im alten `PhotovoltaikCtrl.Update()` (März 2026)

Commit **`5d8122a`** (17.03.2026, „add"), Datei `Controller\PhotovoltaikCtrl.cs`,
Zeilen 112–129 — das UPDATE auf die damalige Katalogtabelle `Tab_PV`
(Schlüssel `Modulname`, der Vorgänger von `Tab_PV_STAMM.Bezeichner`):

```csharp
I_Kurzschluss = {model.m_I_Kurzschluss},
alpha_SC= {model.m_I_Kurzschluss},
beta_OC= {model.m_I_Kurzschluss},
gamma_PMP = {model.m_Temp_Coeff_Pmax},
T_NOCT == {model.m_I_Kurzschluss},
```

Drei Zuweisungen tragen die falsche Modellvariable — **exakt die gemessene Signatur**
(alpha_SC = beta_OC = T_NOCT = I_Kurzschluss, gamma_PMP korrekt). Der Importdialog
(`Form_CECImport.btnSelect_Click`, Stand `5d8122a`) rief nach `INSERT INTO [Tab_PV] (Modulname)`
genau dieses `Update()`. Im Schnappschuss steht sogar `T_NOCT ==`; da `T_NOCT` in den Daten
den Isc-Wert trägt, lief eine Zwischenfassung mit `=`.

Behoben in **`4e80222`** (27.03.2026): `alpha_SC= {SqlVal(model.m_alpha_SC)}` usw., mit
`SqlVal(0) → NULL`. Die drei Stammzeilen **Ablytek 6MN6A275, 6MN6A290** (CEC-Import) und
**Jinkosolar JKM 260P-60** (PAN-Import, Datei `VDI-3805-Daten\PV\Jinko-Solar_JKM260P-60_Dec2019_CFV.PAN`,
erkennbar an Isc 9,014 / Voc 37,81 / Imp 8,461 / Vmp 30,73) wurden in diesem Zeitfenster
angelegt. Das `gamma_PMP = 0` von Jinkosolar gehört dazu: Die damalige
`InitDatensatzUpdate` kannte noch keinen PAN-Zweig (`model.m_Temp_Coeff_Pmax = pvum.CecModule.gamma_pmp`;
für PAN leer), der PAN-Zweig mit `muPmpReq` kam erst in `4e80222`.

Die Katalogtabelle wurde später (bis `eef4cd9`, 26.07.2026: `PhotovoltaikStammCtrl` neu,
`Tab_PV_STAMM` mit `Bezeichner`) umgebaut; die Werte wanderten unverändert mit. Die
Projektzeilen in `Tab_PV` sind 1:1-Kopien (`PhotovoltaikCtrl.CopyFromStamm`, `Controller\PhotovoltaikCtrl.cs:224–284`,
kopiert alle 15 Fachspalten; Projektduplikate 1026/1028/1029 über `ProjektDuplizierenCtrl`) —
deshalb tragen sie dieselbe Signatur, auch `1007005` (Ablytek 270), obwohl die Stammzeile
inzwischen anders aussieht (siehe 3.2). Access→SQLite (S7, 02.09.2026) war bitgleich
(Migrationsbericht: `Tab_PV` und `Tab_PV_STAMM` je 6/6 Zeilen, Hash identisch) — die
SQLite-Migration hat nichts daran geändert.

### 3.2 Zweite Lücke (offen bis heute): `Form_AdminPV` nullt beim Speichern

`Views\Photovoltaik\Form_AdminPV.cs`, `btn_Speichern_Click` (Zeilen 58–134, Stand vor diesem
Paket): Das Modell wird nur aus den 13 Dialogfeldern befüllt (Zeilen 87–99) — es gibt
**keine Felder** für `alpha_SC`, `beta_OC`, `T_NOCT`. Die drei bleiben auf dem
Konstruktorwert 0 und laufen über `PhotovoltaikStammCtrl.UpdateFrom → CopyFrom`
(`Controller\PhotovoltaikStammCtrl.cs:337–355`) → `Update(int)` (Zeilen 237–264, alle 16 Spalten)
als **0** in die Datenbank. So entstand die Stammzeile **Ablytek 6MN6A270** (ID 5):
Modulkosten 468,89 gepflegt, dafür alpha/beta/T_NOCT = 0 — ihre Projektkopie `1007005`
zeigt noch den Zustand davor (9,34). Dieser Weg besteht seit `c698a1e` (13.03.2026).

### 3.3 Dritte Lücke (offen bis heute): PAN-Import verwirft die Koeffizienten

`Views\Photovoltaik\Form_CECImport.cs`, `InitDatensatzUpdate`, PAN-Zweig (Stand vor diesem
Paket Zeilen 592–599): `m_alpha_SC = 0; //pvum.PanModule.muISC`, `m_beta_OC = 0; //…muVocSpec`,
`m_T_NOCT = 0`. Die **LG-Zeile** (ID 9, Datei `LG_LG320N1K-A5_Dec2019_CFV.PAN`) wurde damit
zwischen `4e80222` und `4cb3c53` angelegt: `SqlVal(0)` → **NULL** in alpha/beta/T_NOCT,
gamma = muPmpReq = −0,394. Die Umrechnung fehlte, weil die PVsyst-Einheiten (muISC in
**mA/°C**, muVocSpec in **mV/°C**) nicht zu den Katalogeinheiten (A/K, V/K) passen; der
Kommentar in `PanModule.cs:56` („A/°C (absolut)") war falsch.

### 3.4 Geprüft und ausgeschlossen

- **Spaltenversatz im CEC-Parser:** `CECDataService.ParseCsv` (`Allgemein\Import\CEC\CECDataService.cs:129–170`)
  ordnet **per Kopfzeilenname** zu (`GetCol("alpha_sc")` …), nicht positional — ein Versatz
  ist dort nicht möglich. Die Online-Quelle (NREL SAM, Kopf am 02.09.2026 geprüft) und die
  Repo-Kopie `VDI-3805-Daten\PV\CEC Modules_UTC.csv` führen `alpha_sc, beta_oc, T_NOCT, gamma_pmp`
  mit den Einheiten `A/K, V/K, C, %/K` — passend zum Katalog. Beleg: die Philadelphia-Zeile
  (ID 21, jüngster CEC-Import über den heutigen Weg) ist korrekt (0,00272 / −0,128904 / 45,3 / −0,385).
- **Aber:** Die zweite Repo-Kopie `VDI-3805-Daten\PV\CEC Modules.csv` ist ein Excel-Export
  (**Semikolon-getrennt, Komma-Dezimal**). Der Parser trennt nur am Komma
  (`SplitCsvLine`, Zeilen 182–196) und greift positional auf `fields[26]` für das Datum zu
  (Zeile 146) — diese Datei war damit **gar nicht** importierbar (Ausnahme je Zeile, Import
  bricht als Ganzes ab), nicht „falsch" importierbar. Latente Lücke, in Abschnitt 4 geschlossen.
- **Migrationsschritt 18** (Katalog-Dublettenbereinigung) löscht nur, schreibt keine Werte.
- **Registry-Vergleich:** `KatalogRegistry` „PV" (`Allgemein\Katalog\KatalogRegistry.cs:163–171`)
  vergleicht `alpha_SC/beta_OC/gamma_PMP/T_NOCT` exakt (Entscheidung 9.1). Folge: Ein erneuter
  Import von Ablytek 275 meldete bisher „Abweichend" (vergiftete Bestandswerte gegen korrekte
  Importwerte) — nach der Reparatur „InhaltsGleich". Kein Codeeingriff nötig.

## 4. Absicherung — Code

Fünf Dateien (alle UTF-8, CRLF; BOM-Zustand je Datei unverändert, vorher/nachher per
Byte-Analyse gemessen), **nicht committet** (Sync-Automatik zieht sie ein). Der Paket-A-Strang
hatte `Form_AdminPV` kurz zuvor selbst erweitert (Commit **`7c622b1`**, PA1c: T_NOCT-Eingabefeld,
Erhalt der geladenen alpha/beta-Werte über `_alphaScGeladen`/`_betaOcGeladen`) — damit ist
Lücke 3.2 dort bereits geschlossen; dieses Paket setzt nur noch die Plausibilitätssperre obenauf.
Zeilenanker am **Endstand** gemessen:

| Datei | Zeile(n) | Inhalt |
|---|---|---|
| **`Allgemein\Import\PvModulPlausibilitaet.cs`** (neu, 206 Z.) | `38`–`63` | Konstanten `NOCT_MIN/MAX` 20/60, `ALPHA_MIN/MAX` 0/0,05, `BETA_MIN/MAX` −0,5/0, `GAMMA_MIN/MAX` −1/0 — **wertgleich** mit `SimulationPV.NOCT_MIN/NOCT_MAX/GAMMA_MIN` (Paket A), aber eigenständig, damit die Eingangsprüfung nicht am Rechenkern hängt |
| | `72`–`83` | `Befund` (`Fehler` sperrt, `Warnungen` = Rückfrage, `Ok`) |
| | `124`–`201` | `Pruefe(PhotovoltaikModel m, bool alphaBetaPflegbar = true)`: **Fehler** = Isc-Signatur (`alpha_SC`/`beta_OC`/`T_NOCT` ≠ 0 und exakt = `I_Kurzschluss`), `gamma_PMP > 0` (Mehrertrag bei Wärme), `gamma_PMP < −1`, `alpha_SC < 0`, `beta_OC > 0`, `T_NOCT` ≠ 0 außerhalb 20…60; **Warnungen** = `alpha_SC > 0,05` („vermutlich %/K"), `beta_OC < −0,5` („vermutlich mV/K"), Nullwerte („nicht vorhanden/nicht gepflegt"). Mit `alphaBetaPflegbar = false` werden die alpha/beta-bezogenen Fehler zu Hinweisen mit dem Zusatz „im Katalogdialog nicht pflegbar - per Neuimport oder Reparaturskript berichtigen" (Zeilen `65`–`66`, `92`–`105`); der T_NOCT-Anteil bleibt Fehler |
| `Allgemein\Import\CEC\CECDataService.cs` | `98`–`110` | Kopfkommentar: beide Formatvarianten, Referenzkopien |
| | `141`–`144` | **Trennzeichen** aus der Kopfzeile (häufigeres von `;` und `,`), `SplitCsvLine(…, sep)` |
| | `152`–`163` | **12 Pflichtspalten** (`name … alpha_sc, beta_oc, gamma_pmp, t_noct`); fehlt eine → `(false, "CEC-Kopfzeile unvollstaendig, fehlende Spalte(n): …")` statt stiller Nullen |
| | `169`–`181`, `204` | `Date` per Kopfzeile (`GetCol("date")`) und `TryParse` — statt `fields[26]`/`Parse`, das bei einer kurzen Zeile den ganzen Import abriss |
| | `218`, `225`, `248`–`250` | `SplitCsvLine(string, char sep = ',')`; Units-Marker auch für `;` |
| `Allgemein\Import\Pan\PanModule.cs` | `56`–`57` | Einheitenkommentare berichtigt: `muISC` **mA/°C**, `muVocSpec` mV/°C |
| | `60`–`64` | `muIscAK => muISC/1000` (A/K), `muVocVK => muVocSpec/1000` (V/K) mit Beleg |
| `Views\Photovoltaik\Form_CECImport.cs` | `614`–`630` | `InitDatensatzUpdate`, PAN-Zweig: `m_alpha_SC = muIscAK`, `m_beta_OC = muVocVK`, `m_Temp_Coeff_Pmax = muPmpReq`, `m_T_NOCT = 0` (nicht im PAN; Simulation → 45 °C). Kommentar warnt ausdrücklich, dass `muVocSpec/Voc/10` der **relative** Koeffizient in %/K wäre und nicht nach `beta_OC` (V/K) gehört |
| | `427`–`431` | `ShowDetail`: PAN zeigt die umgerechneten Werte in `textBox_16/17` statt „-" |
| | `458`–`478` | `btnSelect_Click`: Plausibilität **streng** (`Pruefe(model)`) direkt nach `InitDatensatzUpdate`, **vor** der Dublettenprüfung (die Registry vergleicht die vier Felder exakt mit); Fehler → Meldung, nichts geschrieben; Warnungen → „Trotzdem uebernehmen?" |
| `Views\Photovoltaik\Form_AdminPV.cs` | `192`–`213` | `btn_Speichern_Click`: nach dem Befüllen, vor dem Schreiben `Pruefe(model, alphaBetaPflegbar: false)`; Fehler → Dialog bleibt offen; Warnungen → „Trotzdem speichern?" |

**Warum abgemildert im Katalogdialog:** Sechs Bestandssätze tragen die Isc-Signatur, der
Dialog hat keine Felder für alpha/beta. Eine harte Sperre hätte bis zur Reparatur jedes
Speichern (z. B. Modulkosten) blockiert. Jetzt sperrt nur, was der Anwender dort selbst
berichtigen kann (gamma, T_NOCT); alpha/beta melden sich als Hinweis mit Verweis auf
Neuimport/Reparaturskript.

**Hinweis zur Auftragsformulierung „muVocSpec/Voc → beta_OC":** Das wäre die relative Größe
(PVsyst `muVocPerc`, %/°C). `Tab_PV_STAMM.beta_OC` ist laut CEC-Units-Zeile und den korrekten
Bestandszeilen (Philadelphia −0,128904) in **V/K** geführt; umgesetzt ist deshalb
`muVocSpec/1000`. Probe: Jinko −118,1 mV/°C → −0,1181 V/K, gegenüber CEC-Schwesterzeile −0,1166.

## 5. Nachweis

**Build:** MSBuild VS 18, x64 Debug, out-of-tree (`/p:OutputPath` im Prüfstand, App und
Visual Studio liefen parallel): **0 Fehler**, 39 Warnungen — keine davon in den fünf Dateien
dieses Pakets (WFO1000/NU1510/CS0108/CS0109/WFO0003 aus Fremdbeständen).

**Probe** (Konsolenprojekt gegen die gebaute `EPOS_Plan.dll`, Exit-Code 0 = 0 Fehlschläge):

| Fall | Ergebnis |
|---|---|
| `CECDataService.LoadFromFile` auf `CEC Modules_UTC.csv` (Komma/Punkt) | `success`, 20 740 Module |
| dito auf `CEC Modules.csv` (Semikolon/Komma-Dezimal, **vorher unlesbar**) | `success`, 20 740 Module |
| Querabgleich beider Dateien | gleiche Modulanzahl, je 13 Zahlwerte je Modul bitgleich (Toleranz 1e-9); nur die Namen weichen ab, weil der Excel-Export **alle** Punkte durch Kommas ersetzt hat (`Jinko Solar Co. Ltd` → `Jinko Solar Co, Ltd`) |
| Sechs Zielmodule | alpha/beta/gamma/NOCT/L/B exakt auf den Dateiwerten (Ablytek 270: 0,00486614 / −0,121182 / −0,4509 / 47,4 / 1,64 × 0,992 usw.), `Date` 2024 aus `11/14/2024` |
| `PanDataService.ParsePan` Jinko / LG | muISC 3,40 → **0,0034 A/K**, muVocSpec −118,1 → **−0,1181 V/K**, muPmpReq −0,418 · LG 0,0031 / −0,1102 / −0,394 |
| `Pruefe` (a) Isc-Signatur, streng | 3 Fehler / 2 Hinweise |
| (b) Ablytek-270-Sollwerte | 0 / 0 |
| (c) PAN-Jinko (T_NOCT 0) | 0 Fehler / 1 Hinweis (T_NOCT) |
| (d) Isc-Signatur, `alphaBetaPflegbar: false` | 2 Fehler (beide T_NOCT: Signatur + Fenster) / 4 Hinweise — kein alpha/beta-Fehler mehr |
| (e) wie (d), aber T_NOCT = 45 | 0 Fehler / 4 Hinweise → Satz speicherbar |

**Encoding-Nachweis** je Datei vorher/nachher: BOM-Zustand unverändert, CRLF-Zähler =
LF-Zähler, UTF-8 gültig, 0 × U+FFFD; die neue Klasse ist reines ASCII mit BOM.

Prüfstand-Artefakte (Sitzungs-Scratchpad `V:\ergebnis\`: `build.log`, `probe.log`,
`A_diff.patch`, `encoding.txt`) sind flüchtig; die dauerhaften Nachweise der Datenreparatur
liegen unter `sql\pv_katalog\` (Abschnitt 6).

## 6. Reparaturvorschlag (Skript, Ausführung nur nach Freigabe)

Empfehlung: **gezieltes UPDATE mit Quellenangabe** statt Neuimport. Ein Neuimport über den
Konfliktdialog („Überschreiben") würde nur die Stammzeilen treffen, nicht die sechs
Projektkopien in `Tab_PV`, und bei den PAN-Modulen zusätzlich `Firma`, `Leistung`, `Laenge`,
`Breite` neu schreiben. Das Skript ändert ausschließlich die vier Koeffizientenspalten.

**Artefakte** (im Repo unter `sql\pv_katalog\`, Kopien der Nachweise aus dem Prüfstand):

| Datei | Zweck |
|---|---|
| [`reparatur_pv_katalog.sql`](../../../sql/pv_katalog/reparatur_pv_katalog.sql) | 11 UPDATEs in **einer** Transaktion; je UPDATE ein Kommentar mit Quelle (Datei, Zeilennummer, Spalte bzw. PAN-Schlüssel und Umrechnung) und Vorher-Werten; **Guard** auf ID + Bezeichner + die vier gemessenen Ist-Werte (`IS NULL` bei LG) — greift nur im vermessenen Zustand, zweiter Lauf ändert 0 Zeilen |
| [`reparatur_pv_katalog.py`](../../../sql/pv_katalog/reparatur_pv_katalog.py) | Runner: ohne Schalter **Trockenlauf** (Vorher drucken, UPDATEs mit `changes()`, Nachher drucken, ROLLBACK); `--ausfuehren` legt erst `<db>.vor-pv-reparatur-<Datum>.bak` an, dann COMMIT + Kontrollmessung; verweigert `-wal`/`-shm`-Reste und gesperrte DBs; verweigert die Produktivdatenbank ohne `--produktiv-freigegeben` |
| [`messung_pv_katalog.py`](../../../sql/pv_katalog/messung_pv_katalog.py) | Messung/Kontrolle (Abschnitt 2), `--md` für den Bericht |
| `trockenlauf.log`, `echtlauf.log`, `zweitlauf.log`, `messung_tab_pv_NACHHER_probe.md` | Nachweise vom Prüfstand (Kopie `V:\db\probe_reparatur.sqlite`) |

**Zielwerte je Zeile** (aus den Quelldateien gelesen, nicht abgeschrieben):

| Zeile(n) | alpha_SC [A/K] | beta_OC [V/K] | gamma_PMP [%/K] | T_NOCT [°C] | Quelle |
|---|---|---|---|---|---|
| STAMM 5 · Tab_PV 1007005 (Ablytek 270) | 0,00486614 | −0,121182 | −0,4509 (bleibt) | 47,4 | `CEC Modules_UTC.csv` Zeile 4 |
| STAMM 6 · Tab_PV 1007006 (Ablytek 275) | 0,00490782 | −0,122249 | −0,4509 (bleibt) | 47,4 | dito Zeile 5 |
| STAMM 7 (Ablytek 290) | 0,00503807 | −0,125449 | −0,4509 (bleibt) | 47,4 | dito Zeile 8 |
| STAMM 8 · Tab_PV 1011008, 1015244–46 (Jinkosolar) | 0,0034 | −0,1181 | −0,418 | **0** | PAN `Jinko-Solar_JKM260P-60_Dec2019_CFV.PAN`: muISC 3,40 mA/°C, muVocSpec −118,1 mV/°C, muPmpReq −0,418 %/°C |
| STAMM 9 (LG) | 0,0031 | −0,1102 | −0,394 (bleibt) | **0** (statt NULL) | PAN `LG_LG320N1K-A5_Dec2019_CFV.PAN`: muISC 3,10, muVocSpec −110,2 |
| STAMM 21 (Philadelphia) | — | — | — | — | kein UPDATE; gegen CEC Zeile 10328 verifiziert, alle vier Werte stimmen |

Die Quellenzuordnung ist je Zeile belegt: Bei den Ablytek-Zeilen stimmen Isc/Voc/Imp/Vmp/L/B
mit der CEC-Zeile überein (Leistung = Imp·Vmp), bei Jinkosolar und LG mit den PAN-Dateien
(Isc 9,014 / 10,350 usw.). Quellen werden **nicht gemischt**: Die CEC-Schwesterzeilen
(`Jinko Solar Co. Ltd JKM260P-60`: gamma −0,41, T_NOCT 45,1; `LG Electronics Inc. LG320N1K-A5`:
gamma −0,357) stammen aus einem anderen Prüflabor (Isc 8,98 statt 9,014, Länge 1,614 statt 1,65).
Für Jinkosolar steht `T_NOCT = 45,1` als **auskommentierte Variante** im Skript —
Entscheidung des Fachbereichs. `T_NOCT = 0` (statt NULL) für die PAN-Zeilen, weil der Import
0 schreibt und die Dubletten-Registry exakt vergleicht; die Simulation behandelt beides gleich
(Rückfall 45 °C).

**Prüfstand-Nachweis** (Kopie der Produktivdatenbank, Produktivdatei zu keinem Zeitpunkt geöffnet,
MD5 vorher/nachher `61a3f1d532b608f59178f3f9a29bb221`):

| Lauf | Ergebnis |
|---|---|
| Trockenlauf | 11 UPDATEs × `changes() = 1`, ROLLBACK, Kopie danach bitgleich |
| Echtlauf (`--ausfuehren`) | Sicherung angelegt, 11 × 1 Zeile, COMMIT; Kontrollmessung: alpha/beta/gamma 12/12 plausibel, T_NOCT 6 plausibel + 6 × 0 (PAN-Zeilen); keine Isc-Signatur, keine NULL |
| Zweitlauf | 11 × `changes() = 0` — idempotent |
| Gegenprobe VORHER/NACHHER-CSV | 38 geänderte Koeffizientenfelder, **0** geänderte sonstige Felder |

**Ausführung auf der Produktivdatenbank — nur nach Freigabe**, Ablauf:

```bash
python sql\pv_katalog\reparatur_pv_katalog.py C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite
```

(Trockenlauf; vorher EPOS-Plan auf **allen** Rechnern schließen, damit `-wal`/`-shm`
verschwinden — der Runner bricht sonst ab). Dann mit `--ausfuehren --produktiv-freigegeben`;
die Sicherung legt der Runner selbst an. Danach Kontrolle:

```bash
python sql\pv_katalog\messung_pv_katalog.py C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite --md sql\pv_katalog\messung_tab_pv_NACHHER.md
```

Zweiter Rechner: dieselben Schritte, sofern dessen Katalog dieselben Zeilen trägt (Guards
schützen vor Fehltreffern; ein UPDATE mit `changes() = 0` heißt „Zeile nicht im vermessenen
Zustand" und ist dann von Hand zu prüfen). Alternative für den Ausrollweg wäre ein
SQLite-Migrationsschritt 63 nach dem Muster von Schritt 62 (`SchemaMigration.cs:9831`) mit
denselben Guards — nicht umgesetzt, weil der Auftrag ein Skript nach Freigabe verlangt und
die Migrationsnummern zwischen den Strängen linear vergeben werden.

## 7. Offene Punkte und Hinweise

1. **Freigabe der Datenreparatur** (Abschnitt 6) — bis dahin bleiben 11 Zeilen inhaltlich
   falsch; die Simulation fängt T_NOCT über das 20…60-Fenster ab (Paket A), `alpha_SC`/`beta_OC`
   werden dort derzeit nicht gelesen, `gamma_PMP = 0` (Jinkosolar) rechnet ohne Temperaturkorrektur.
2. **Entscheidung T_NOCT Jinkosolar**: 0 (PAN kennt keinen NOCT, Rückfall 45 °C) oder 45,1 aus der
   CEC-Schwesterzeile (anderes Prüflabor) — Variante steht auskommentiert im Skript.
3. **Sichtabnahme** der Meldungstexte in Import- und Katalogdialog sowie der PAN-Anzeige
   (`textBox_16/17`).
4. **Zweiter Rechner:** Katalog dort messen (`messung_pv_katalog.py`), dann dieselbe Reparatur;
   die Guards machen Fehltreffer sichtbar (`changes() = 0`).
5. **Nebenbefunde außerhalb des Auftrags:** Philadelphia (ID 21) hat `Laenge = Breite = 0`, weil die
   CEC-Zeile Length/Width leer führt — für die Flächenrechnung fehlt die Größe (E1.1-Konsistenzwarnung
   greift). `PanDataService.ParsePan` liest `Rserie`, die PAN-Datei schreibt `RSerie` (Groß-/Kleinschreibung),
   ebenso `Gamma`/`muGamma` statt `Gamma1`/`GammaTh` — die Diodenparameter bleiben 0 (nur für E3 relevant).
   `Form_AdminPV` liest die Katalogzeile weiterhin per String-Konkatenation des Bezeichners.
6. **Katalog-Dubletten nach Reparatur:** Ein erneuter CEC-Import von Ablytek 275/290 meldet dann
   „InhaltsGleich" statt „Abweichend" — gewollt.
7. **Commit:** nichts committet; der Sync-Sweep sammelt die fünf Codedateien, `sql\pv_katalog\` und
   dieses Protokoll ein. Beim nächsten eigenen Commit die Pfade gezielt stagen (fremde Änderungen
   an SolardatenCtrl/Klimadaten/ProjektDelete liegen daneben im Baum).

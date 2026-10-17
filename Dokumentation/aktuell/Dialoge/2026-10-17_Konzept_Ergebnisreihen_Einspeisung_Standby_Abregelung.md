# Kurzkonzept Ergebnisreihen: BHKW-Einspeisung, Standby des Speichersystems, PV-Abregelung (Welle EB)

Sitzung Dialoge und Korrekturen, 17.10.2026. Anwenderentscheide vom 17.10.2026 (Konzept Energieflussbild, Abschnitt 13,
„optionale Ergebnisspalten“):

1. Die **Einspeisung des BHKW** wird als eigene Reihe geführt — als Stundenreihe (Viertelstunde wie die übrigen
   Stromreihen des Laufs) und als Jahressumme im Ergebnis — in den Ergebnisdarstellungen und Grafiken; in den Grafiken als
   Reihe „Stromeinspeisung BHKW“ wählbar (EB-Q4).
2. Der **Standby des Stromspeichers** wird berücksichtigt und aufgenommen (Reihe und Jahressumme im Ergebnis; im
   Energieflussbild als benanntes Band statt „nicht zugeordnet“).
3. Die **PV-Abregelung** wird als Reihe und Jahressumme gespeichert; der Rechenweg bleibt unverändert, es gibt keine neue
   Rechenoption. Im Energieflussbild ist sie Teil der Bilanz (EB-Q1, unten); zuschaltbar ist sie dort, wo Reihen geschaltet
   werden (Zeitreihen-Grafiken).

Schritt **EB-1** (Kern, Schema 239, Basis R59) und Schritt **EB-2** (Oberfläche, Katalogfassung 26) sind umgesetzt.
Protokolle:
[`2026-10-17_EB-1_Ergebnisreihen_Kern.md`](../../ueberholt/Protokolle/Views/2026-10-17_EB-1_Ergebnisreihen_Kern.md),
[`2026-10-17_EB-2_Ergebnisreihen_Oberflaeche.md`](../../ueberholt/Protokolle/Views/2026-10-17_EB-2_Ergebnisreihen_Oberflaeche.md).


## 1. Befund: wo der Lauf die drei Größen rechnet und wo sie bis EB-1 lagen

| Größe | Rechnet der Lauf in | Laufreihe (`ZeitreihenSatz`, nicht persistiert) | Jahressumme vor EB-1 |
|---|---|---|---|
| Einspeisung des BHKW [MWh/a] | Viertelstundenbilanz des Laufs, `SimulationControl.BhkwEinspeisungDesLaufs()`, mit Flotte `Speicherflottennetzbilanz.BhkwNetzeinspeisungKwh`; gebündelt in `SimulationErgebnisCtrl.BhkwEinspeisungMwh(sim)` | `BHKW_UEBERSCHUSS` (`"BHKW_Ueberschuss"`, kWh je Stunde; Reihe nur ab 0,5 kWh/a) | nur `Tab_ErgebnisStromMatrix.KwkEinspMWh`, geschrieben allein von `WirtschaftlichkeitCtrl.Persistiere` (erst eine Wirtschaftlichkeitsrechnung, nicht der Lauf); `ErgebnisBHKWModel` kannte `Stromproduktion` und `Strombedarfsdeckung`, nicht die Einspeisung |
| Standby des Speichersystems [kWh/a] | `SimulationControl.SpeichersystemEigenverbrauchKwh` — Standby aus PV und Netz (`StandbyAufteilen`), im Flottenpfad der Hilfsverbrauch der Einheiten | `SPEICHER_EIGENVERBRAUCH` (kWh je Stunde; nur ab 0,5 kWh/a) | nur der Skalar `Stromspeicher.EigenverbrauchSystemMwh` des Referenzlauf-Exports; nicht im Ergebnisbaum |
| PV-Abregelung an der Einspeisegrenze [MWh/a] | `SimulationPV.EinspeisungAufteilen` je Viertelstunde nach der Speicherladung, mit Flotte `Speicherflottennetzbilanz.PvAbregelungKwh`; Reiterwert `SimulationErgebnisCtrl.PhotovoltaikErgebnis.AbregelungMwh` | `PV_ABREGELUNG` (`"PV_Abregelung"`, kWh je Stunde; nur ab 0,5 kWh/a) | nur der Skalar `Photovoltaik.AbregelungMwh` des Exports; `KennzahlenKatalog.AbregelungMwh` las die Reihe, ohne Reihe 0 (Übersicht lädt keine Reihen) |

**Wo Ergebnisse liegen.** Der Lauf (`SimulationRunner.BaueErgebnis`) baut den `ErgebnisModel`-Baum, `ErgebnisCtrl.Save`
schreibt ihn in `Tab_Ergebnis` und die Detailtabellen `Tab_Ergebnis*` (Skalare, kaufmännisch auf zwei Stellen gerundet),
`ErgebnisCtrl` liest ihn zurück; `EnergievergleichCtrl.Lade` baut daraus die `VariantenDaten` der Übersicht,
`EnergieflussCtrl.Lade` die Energieflussbilanz. **Ergebniszeitreihen werden nicht persistiert** (AP0-Entscheid vom
16.08.2026, Frage 2: Neurechnen ist billiger als Speichern); der `ZeitreihenSatz` entsteht je Lauf in
`ZeitreihenExtraktor.AusLauf` und trägt die drei Reihen bereits. Der Referenzlauf exportiert `SELECT * FROM Tab_Ergebnis*`
nach `aggregate.csv`; `Tab_ErgebnisStromspeicher` liest er nicht, den Standby führt er als handgeschriebenen Skalar.

**Energieflussbild vor EB-1.** Die Einspeisung des BHKW war der Rest am Stromknoten (Herkunft „bilanziert“, SK-Q1);
Standby und Abregelung erschienen als Band „nicht zugeordnet“ (1046: 0,44 MWh/a, 0,6 % des Stroms).


## 2. Speicherentscheidung (EB-1)

**Drei Spalten, Schemaschritt 239 (`ErgebnisreihenSchema`, hängt über `+ 1` an 238 `KlimaRohwerteSchema`):**

| Tabelle | Spalte | Einheit | Quelle im Lauf | Modellfeld |
|---|---|---|---|---|
| `Tab_ErgebnisBHKW` | `Stromeinspeisung` | MWh/a (wie die Zeile) | `SimulationErgebnisCtrl.BhkwEinspeisungMwh(sim)` | `ErgebnisBHKWModel.StromeinspeisungMwh` (`double?`) |
| `Tab_ErgebnisPhotovoltaik` | `Abregelung` | MWh/a (wie die Zeile) | `SimulationErgebnisCtrl.PvAbregelungMwh(sim)` — die eine Formel, die auch der Reiter nimmt | `ErgebnisPhotovoltaikModel.AbregelungMwh` (`double?`) |
| `Tab_ErgebnisStromspeicher` | `Eigenverbrauch_System` | kWh/a (wie die Zeile; eine Zeile je Lauf) | `SimulationControl.SpeichersystemEigenverbrauchKwh` | `ErgebnisStromspeicherModel.EigenverbrauchSystemKwh` (`double?`) |

Alle drei `REAL`, nullbar, `CHECK (x IS NULL OR x >= 0)`, kein DML. **NULL heißt „nicht erhoben“** (Ergebnis vor
Schritt 239): das Energieflussbild fällt dann auf die Bilanzierung zurück, die Kennzahl auf die Laufreihe; 0 heißt
„erhoben, keine Menge“ (etwa Abregelung ohne Einspeisegrenze). `ErgebnisCtrl.Save` schreibt die Spalten nur, wenn
`ErgebnisreihenSchema.Lesbar()` sie kennt (Rückfallebene für eine Datenbank vor dem Schritt).

**Reihen bleiben Laufreihen.** Es gibt weiterhin keine persistierten Ergebniszeitreihen; die drei Reihen stehen im
`ZeitreihenSatz` jedes Laufs unter den Schlüsseln `ZeitreihenSatz.BHKW_UEBERSCHUSS` (Einspeisung des BHKW),
`ZeitreihenSatz.SPEICHER_EIGENVERBRAUCH` (Standby) und `ZeitreihenSatz.PV_ABREGELUNG` (Abregelung), jeweils kWh je Stunde
aus dem Stundenmittel der Viertelstundenreihe; die Viertelstundenreihen selbst liegen am Lauf
(`Speicherflottennetzbilanz.*Kw`, `SimulationPV.EinspeisungAufteilen`, `SpeichersystemStandbyAus*Kw`).

**Warum keine Spalte am Kopf `Tab_Ergebnis`:** Jede Größe gehört zu genau einer Detailzeile (BHKW, Photovoltaik,
Stromspeicher) und erscheint so im Reiter dieser Anlage; der Standby ist eine Systemgröße, die Speichertabelle führt
aber genau eine Zeile je Lauf (die Flotte ist eine Zeile), also ist die Zeile der richtige Ort.

**Rechenweg unverändert.** Der Lauf schreibt nur zusätzlich, was er schon rechnet: `SimulationRunner.BaueErgebnis` belegt
die drei Felder; der einzige Eingriff in bestehenden Code ist, dass `SimulationErgebnisCtrl.Photovoltaik` die Abregelung
über denselben Helfer `PvAbregelungMwh` bezieht, den der Lauf nimmt (eine Formel statt zwei Abschriften).


## 3. Wirkung auf Referenzlauf und Basis

Der Export liest `SELECT *`, deshalb bekommt `aggregate.csv` je BHKW-Projekt den Schlüssel `BHKW.Stromeinspeisung` und je
PV-Projekt `Photovoltaik.Abregelung` (34 neue Einträge in 38 Projekten, alle Abregelungen 0 — kein Referenzprojekt trägt
eine Einspeisegrenze); `Tab_ErgebnisStromspeicher` steht nicht im Export, der Standby bleibt der bisherige Skalar
`Stromspeicher.EigenverbrauchSystemMwh` (1046: 0,438). Der Vergleich R59 gegen R57 meldet genau diese Einträge als
„Eintrag nur im Vergleichslauf“; mit `--ohne BHKW.Stromeinspeisung,Photovoltaik.Abregelung` ist er **PASS (13 025 990
Werte)**, 1 199 der 1 233 CSV sind byte-gleich (die 34 anderen sind die `aggregate.csv` mit den neuen Schlüsseln). Kein
bisheriger Wert hat sich geändert. Die Basis ist als **R59 `2026-10-17_R59_Ergebnisreihen`** neu eingefroren (R58 war im
Statuskopf von der Sitzung Kostenbasis angemeldet, R59 die nächste freie Nummer); R57 liegt mit Protokoll unter
[`Dokumentation/ueberholt/Referenzbasen/`](../../ueberholt/Referenzbasen/LIESMICH.md).

**Keine neue Einfrierregel:** Die drei Spalten sind Ausgaben des Laufs, keine gesäten Eingaben; was sie bestimmt
(Einspeisegrenze, Standby-Verbrauch der Speicher, BHKW-Grenzleistung), steht schon in den Einfrierregeln „gesäte
Stromfelder“ und „gesäte BHKW-Grenzleistungen“.


## 4. Energieflussbild (EB-1, Kern)

- **Einspeisung BHKW:** Band Strom → „Einspeisung BHKW“ aus `StromeinspeisungMwh` (Herkunft „direkt“), der
  Stromknoten wird danach wie jeder andere geprüft; ein Altergebnis ohne Wert bilanziert wie bisher (SK-Q1).
  **Speicherzeile:** Die Ergebniszeile trägt den BHKW-Überschuss des Laufs — dieselbe Menge wie Reihe, Reiter und
  KWK-Split. Ein Stromspeicher ohne Flotte lädt daraus seine `Ladung_BHKW` (der Speicherweg bucht nach dem Überschuss),
  ins Netz geht der Rest: Band = Zeile − `Ladung_BHKW`, Herkunft „abgeleitet“ mit Formeltext (1058: 5,32 − 1,86 = 3,46,
  1063: 16,56 − 4,79). Mit Flotte ist die Zeile schon die Netzeinspeisung der Flotte (`BhkwNetzeinspeisungKwh`), die
  Flottenzeile trägt ebenfalls eine `Ladung_BHKW`; welche Lesart gilt, sagt der Knoten: genommen wird die, mit der er
  schließt (zuerst die Zeile) — kein Flottenkennzeichen im Ergebnisbaum nötig.
- **Standby:** Band Strom → „Standby Speichersystem“ (Form Verlust, Knotenart Verlust, Spalte der Speicher) je
  Speicherzeile mit Wert; der Speicherknoten selbst schließt weiter über Ladung, Entladung, Verlust und Füllstand — der
  Standby ist Verbrauch des Systems am Strombus, nicht Inhalt des Speichers. 1046 schließt damit ohne „nicht zugeordnet“.
- **Abregelung (EB-Q1):** Band Photovoltaik → „abgeregelt“ (Form Verlust) an der PV-Quelle; an den Strombus geht
  Erzeugung minus Abregelung, die Quelle trägt das erzeugbare Potenzial (abgegeben + abgeregelt). Im Bild gilt die
  Kleinverlustregel (SK-Q15): ab 1 % der Erzeugung ein grauer Abzweig, darunter nur im Zeigertext der Quelle. Kein
  Schalter in `EnergieflussAbbildung` oder Renderer; ohne Abregelung (0 oder nicht erhoben) ändert sich kein Bild.
- Ressourcen `EF_KN_STANDBY`, `EF_KK_STANDBY`, `EF_KN_ABREGELUNG`, `EF_KK_ABREGELUNG` in beiden Sprachen; Kennungen
  `Energiefluss.STANDBY` (Präfix `standby:` + Index der Speicherzeile) und `Energiefluss.ABREGELUNG_PV`.


## 5. Schritt EB-2 (Oberfläche) — Schnittstelle und Umfang

Lesewege für EB-2: `VariantenDaten.Ergebnis.BHKW.StromeinspeisungMwh`, `….Photovoltaik.AbregelungMwh`,
`….Stromspeicher[0].EigenverbrauchSystemKwh` (alle `double?`, NULL = nicht erhoben, Einheit im Namen);
Reiterwerte `SimulationErgebnisCtrl.BhkwEinspeisungMwh(sim)`, `PvAbregelungMwh(sim)`, `sim.SpeichersystemEigenverbrauchKwh`;
Reihen `ZeitreihenSatz.BHKW_UEBERSCHUSS`, `SPEICHER_EIGENVERBRAUCH`, `PV_ABREGELUNG`.

| Ort | Umfang EB-2 (umgesetzt) |
|---|---|
| Ergebnistabellen der Reiter BHKW, PV, Stromspeicher | standen schon vor EB-2 im Bestand: BHKW „Stromeinspeisung“ [MWh/a], PV „Abregelung“ [MWh/a, nur mit Wert], Stromspeicher „Eigenverbrauch Speichersystem“ samt „davon aus dem Netz“ [kWh/a, nur mit Standby]; unverändert |
| Zeitreihen-Grafiken | BHKW-Reiter: Reihe „Stromeinspeisung“ war schon wählbar (Bestand, Bericht-Bild nutzt dieselbe Beschriftung — nicht umbenannt). Blatt Stromproduktion (Stromgang): „Stromeinspeisung BHKW“ (EB-Q4) (`BHKW_EINSPEISUNG`, der BHKW-Teil von `NETZEINSPEISUNG`, gestrichelt) und „PV-Abregelung“ (`PV_ABREGELUNG`, nur mit Abregelung) hinter den Netzreihen, **ab Werk aus**, CSV folgt der Wahl; Quelle `SimulationErgebnisCtrl.Netzreihen.BhkwEinspeisungKw`/`PvAbregelungKw`. Betriebsbild Stromspeicher: „Standby Speichersystem“ (nur mit Standby), **ab Werk aus**. PV-Reiter: die Abregelung steht schon als Schicht der aufgeteilten Erzeugung |
| Übersicht „Ergebnisse – Energie“ | Kennzahlen `energie.bhkw_einspeisung` [MWh/a, N1] (Ergebniszeile, Rückfall Strommatrix, sonst „—“), `energie.pv_abregelung` [MWh/a, N1, nur > 0], `energie.speicher_standby` [kWh/a, N0, nur > 0] in der Gruppe Strom; Energieflussbild unverändert (EB-1) |
| CSV-Export | Stromgang: die zwei Reihen als Spalten, nur gewählt; die Reiter-CSV des Stromspeichers hängt mit Standby die Spalte „Standby Speichersystem [kWh]“ als letzte an, die bisherigen Spalten bleiben unverändert (EB-Q5) |
| KI-Sicht / Kennzahlen | die drei Kennzahlen stehen im `KennzahlenKatalog` und damit in der Tabelle, die die KI-Sicht der Übersicht liest; Eingabebilanz unverändert (Standby läuft in der vorhandenen Schalterschleife) |
| Bericht | die Kennzahlentafel führt die drei Zeilen (Katalogfassung 26, Vorlagen neu gebaut); Baustein BHKW und Strommatrix-Tafel unverändert |
| Wiki | Seiten Simulation (Blatt Stromproduktion), Stromspeicher (Betriebsbild), Varianten (Gruppe Strom); BHKW und Photovoltaik ohne Änderung |


## 6. Offene Entscheide EB-Q

| Nr. | Frage | Empfehlung / Entscheid |
|---|---|---|
| **EB-Q1** | Abregelung im Energieflussbild hinter einem Schalter oder als Teil der Bilanz? | **Entschieden 17.10.2026:** kein Schalter. Die Abregelung ist Teil der Bilanz und erscheint als grauer Abzweig „abgeregelt“ an der PV-Quelle nach der Kleinverlustregel (ab 1 % der Erzeugung als Band, darunter im Zeigertext); zuschaltbar (Vorgabe aus) ist sie in den Zeitreihen-Grafiken (EB-2). |
| **EB-Q2** | Einheit der Standby-Zeile im Reiter Stromspeicher: kWh/a wie die Speicherzeile oder die Anzeigeeinheit des Projekts (MWh)? | **Entschieden 17.10.2026 nach Empfehlung:** Anzeigeeinheit an der Anzeigekante, die Spalte bleibt kWh/a. Umsetzung EB-2: Der Kennzahlenblock des Reiters zeigt alle Energiezeilen in kWh/a; die Standby-Zeile folgt ihren Nachbarn, die Übersicht führt sie ebenfalls in kWh/a (ein Speichersystem verbraucht Hunderte kWh, in MWh stünde „0,4“). |
| **EB-Q3** | Soll die Übersicht die Einspeisung BHKW bei einem Altergebnis (NULL) weiter aus der Strommatrix zeigen oder „nicht erhoben“ melden? | **Entschieden 17.10.2026 nach Empfehlung, umgesetzt in EB-2:** Strommatrix als Rückfall (`EnergievergleichCtrl.BhkwEinspeisungRueckfall`), sonst „—“; kein stiller Nullwert. |
| **EB-Q4** | Name der BHKW-Einspeisung: „eingespeister Strom“ oder „Stromeinspeisung“? | **Entschieden 17.10.2026:** überall „Stromeinspeisung“. Im Blatt Stromproduktion heißt die Reihe „Stromeinspeisung BHKW“ (englisch „CHP grid feed-in“); BHKW-Reiter und Bericht bleiben bei „Stromeinspeisung“ |
| **EB-Q5** | Standby in der Reiter-CSV des Stromspeichers? | **Entschieden 17.10.2026:** ja, als letzte Spalte „Standby Speichersystem [kWh]“ je Intervall, nur mit Standby; die bisherigen Spalten bleiben unverändert, der Rückweg eigener Exporte damit auch |

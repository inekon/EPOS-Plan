# Paket V0 — Bestandsfehler: Umsetzungsprotokoll

Stand: 27.08.2026 · Branch `Pufferspeicher` · Bezug:
[`Konzept_Brauchwasser_Heizung_Pufferspeicher.md`](Konzept_Brauchwasser_Heizung_Pufferspeicher.md)
Kapitel 2.3 (V0-Tabelle) und 11.1. Build: `WP-Plan.sln` + `Referenzlauf.csproj` x64 Debug, 0 Fehler.

## 1. Umfang

Neun Bestandsfehler (V0-1 … V0-9) aus der Ist-Analyse vom 26.08.2026, behoben **vor** dem
Konzeptumbau, damit sich ihre Ergebniswirkung nicht mit der Dreikanal-Umstellung mischt.
Der V0-9-Teilpunkt „Handler an `comboBox_Erzeuger`" entfällt planmäßig (der Dialog
`Form_KonfigPufferspeicher` wird mit Paket A1 entfernt).

## 2. Die Fixes

| # | Fix | Stellen |
|---|---|---|
| V0-1 | **Mehrgebäude-Doppelzählung**: neuer Einzelgebäude-Puffer `Waermebedarf_EinGebaeude`, je Iteration genullt; jedes Gebäude geht genau einmal in `Waermebedarf` und `Waermebedarf_Gebaeude` (bleibt Summe aller Gebäude); `MaxP[i]` misst jetzt das einzelne Gebäude. Bei 1 Gebäude bitgleich | `SimulationWaermebedarf.cs:145-154, 187-209` |
| V0-2 | **Stromprofile summieren**: Ergebnisvektor `summe` über alle Profile; `temp` bleibt Rechenpuffer je Profil. Bei 1 Profil bitgleich | `SimulationStrombedarf.cs:168-175, 250-257` |
| V0-3 | **Projektfilter** `AND ID_Projekt=…` an Kopf- und Typprofilabfragen von Brauchwasser **und** Prozesswärme (nur Projektmodus — die `_STAMM`-Tabellen tragen kein `ID_Projekt`); `wochen_waerme` vor jedem Ladevorgang genullt; 0 Treffer → Protokollwarnung + Anteil 0 statt Fremdwert/stehengebliebenem Profil. Fehlendes Typprofil wird übersprungen statt mit Nullprofil gerechnet (`StromWocheToJahr` lieferte daraus NaN) | `SimulationWaermebedarf.cs:768-869, 919-1013` |
| V0-4 | **Prozesswärme-Betriebsarten getrennt** (Weiche wie im Brauchwasserzweig): Projektrechnung → Kopf `Tab_Prozesswaerme` + Typ `Tab_Prozesstyp`; Katalogvorschau → beides `_STAMM` (das Typprofil kam in der Vorschau bisher fälschlich aus der Projektkopie, der Kopf im Projektmodus fälschlich aus dem Katalog). Normierungsnenner `jv` folgt der Quelle | `SimulationWaermebedarf.cs:768-790` |
| V0-5 | **Externe Wärmeganglinien**: eigener genullter Puffer je Ganglinie (Summe statt Überschreiben mit Restbestand), Rasterprüfung 8760/35040 (35040 → Stundenmittel, Muster `WirtschaftlichkeitCtrl`; `Tab_Waermebedarf` hat kein `Zeitinterval` — das Raster ergibt sich aus der Wertzahl), Indexschutz gegen Überlauf, sonstige Wertzahl → Warnung + überspringen | `SimulationWaermebedarf.cs:229-291` |
| V0-6 | **Schaltjahr**: Monatsgrenzen aus `const REFERENZJAHR = 2025` statt `DateTime.Today.Year` — im Schaltjahr ergab sich `mo_ende[11] = 8783` auf `float[8760]` → ab 2028 wäre jeder Lauf mit IndexOutOfRange abgebrochen. Für Nicht-Schaltjahre bitgleich | `Init.cs:10-32` |
| V0-7 | **Detailansicht = Runner**: WP-, Kessel- und Solar-Anzeige rechnen jetzt die Eigenanteils-Formeln des Persistenzpfads (Muster: BHKW-Block): Eigenanteil = Direktdeckung + zugerechnete Speicherentladung (+ Heizstab bei WP), Rest = Stufeneingang − Eigenanteil (≥ 0), Deckung geklemmt 0..100; WP zusätzlich mit Altpfad-Formelsatz des Runners; tote Zwischenvariablen entfernt | `Form_Simulation_Detail.cs:3197-3241, 3368-3399, 3454-3486` |
| V0-8 | **Netzverluste absolut**: auch der Absolut-Zweig weist die aufgeschlagene Jahresmenge in `Waermebedarf_Netzverluste` aus (MWh, wie der Prozent-Zweig) | `SimulationWaermebedarf.cs:328-337` |
| V0-9a | **Katalog-Löschung per STAMM-ID**: `Delete(int)` + `IsReadOnlyStatic(int)`; die Namensfassung löst auf genau eine ID auf und delegiert. `Form_PufferSp` adressiert über eine Parallelliste `_katalogIds` (die ListBox wird aus drei Quellen mit unterschiedlicher Sortierung gefüllt). B0-8-Rückfrage und ReadOnly-Sperren unverändert | `PufferSpStammCtrl.cs:59-74, 184-222`, `Form_PufferSp.cs` |
| V0-9b | **Kennlinienkappung oben protokolliert** (Entscheidung F13): Zähler `Modul_Kappung_Oben[MAX_WP]`, Inkrement nur bei echter Überschreitung (`> t_maxSST`), Meldung einmal je Modul und Lauf (`HinweisEinmal`) an beiden Laufenden (Altpfad `:854`, zweikanalig `:1280`); Rechnung unverändert | `SimulationWaermepumpe.cs:199-214, 301-307, 1672-1745` |

**Neue Ressourcenschlüssel** (je in `Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs`):
`SIMENG_WAERMEGANGLINIE_RASTER_PASST_NICHT`, `SIMENG_PROZESSWAERME_KOPF_FEHLT`,
`SIMENG_PROZESSWAERME_TYPPROFIL_FEHLT`, `SIMENG_BRAUCHWASSER_KOPF_FEHLT`,
`SIMENG_BRAUCHWASSER_TYPPROFIL_FEHLT`, `SIMENG_WP_KAPPUNG_OBEN_HINWEIS` — alle dialogfrei über
`SimulationProtokoll` (Muster „Simulation Warnung:"/„Simulation Hinweis:").

## 3. Verifikation: Vorher/Nachher-Referenzlauf

**Methode (Weg B der Suite):** EINE migrierte Kopie der produktiven `Kenndaten.accdb`
(Quelle vom 26.08.2026 23:39, gezogen am 27.08. ~09:40 — die Anwendung des Anwenders war
geöffnet, `Kenndaten.laccdb` vorhanden; für das Vorher/Nachher-Paar unerheblich, da **beide
Läufe auf derselben Kopie** rechnen) nach `C:\Waermeplan\V0_Test\DB`. Vorher-Lauf mit dem
Merge-Stand (vor den Fixes), Nachher-Lauf mit den Fixes, feste Projektliste
1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, 1030.

**Datenlage der Referenzmenge** (aus der Kopie erhoben): Gebäude je Projekt = 1, außer
**1008 = 2** (1030 ohne Gebäudezeile, rechnet über externen Lastgang); Stromverbraucherprofile:
**1007 = 2, 1008 = 2, 1011 = 3**, übrige ≤ 1; Prozesswärme nur **1011** (Projektkopien in
`Tab_Prozesswaerme`/`Tab_Prozesstyp` mit gesetztem `ID_Projekt` vorhanden); externe
Wärmeganglinien 1011 und 1030 mit exakt 8760 Zeilen (Rasterprüfung neutral).

**Ergebnis — exakt die Vorhersage aus dem Konzept:**

| Projekt | Vergleich | Zuordnung |
|---|---|---|
| 1007 | FAIL (gewollt) | V0-2: `Strombedarf_Gesamt` 12 → 24 MWh (zwei identische Profile — vorher zählte nur eines); PV-Überschuss 0,76 → 0,33 MWh, Reststrom 38,82 → 50,34 MWh |
| 1008 | FAIL (gewollt) | **V0-1**: `Waermebedarf_Gesamt` **98,26 → 54,88 MWh** (Gebäude 1 zählte doppelt), `Waermelast_Max` 68,54 → 37,76 kW, Bivalenzpunkt 8,33 → 4,54 °C, `Min_Spitzenkesselleistung` 51,31 → 20,53 kW, WP-Produktion 81,13 → 53,51 MWh; dazu V0-2: Strombedarf 9 945 → 10 310 MWh |
| 1011 | FAIL (gewollt) | V0-2: `Strombedarf_Gesamt` 5 462 → 6 806 MWh (drei Profile statt nur das letzte; Viertelstundenwerte ≈ ×3). **V0-4 neutral**: Projektkopie ≡ Katalogwerte (unveränderte `CopyFromStamm`-Kopie) — der Fix wirkt erst bei projektseitig editierten Prozessdaten |
| 1017, 1018, 1021, 1023, 1024, 1030 | **PASS** | unverändert — insbesondere die Brauchwasser-Projekte 1023/1024: der Projektfilter (V0-3) liefert für die Referenzmenge dieselben (projektrichtigen) Zeilen wie bisher der Erste-Treffer-Zufall |

Plausibilisierung (`pruefen`): alle neun Projekte OK, **keine NaN/Inf** (das mit V0-2 benannte
NaN-Risiko bei Nullsummen-Profilen trifft die Referenzmenge nicht).

## 4. Offene Punkte / Folgetickets

| # | Punkt | Ziel |
|---|---|---|
| O-1 | **Runner-Inkonsistenz Solar-Deckungsgrad**: `SimulationRunner.cs:680` teilt durch den **Stufeneingang** (`st.Waermebedarf_gesamt`), WP/Kessel/BHKW durch den Projektbedarf. Die Anzeige spiegelt bewusst den Runner (V0-7-Ziel „Dialog = `Tab_Ergebnis`") — die Vereinheitlichung ändert Persistenzwerte und gehört nach K2/E1 | K2/E1 |
| O-2 | `Form_PufferSp_Admin.cs:91` löscht weiter über den Namen (trifft jetzt genau eine ID statt aller Namensvettern; volle Parität bräuchte dieselbe Parallellisten-Behandlung wie `Form_PufferSp`) | Einzelfix |
| O-3 | Untergrenzen-Hinweis der Kennlinie: `extrapolation` ist instanzweit statt je Modul — der Hinweis erscheint nur für das erste auslösende Modul (der neue Obergrenzen-Zähler ist je Modul) | Einzelfix |
| O-4 | Restliche `DateTime.Now.Year`-Stellen außerhalb der Monatsgrenzen-Kette (Chart-Achsen, CSV-Export, Ferientage, WP-Quellprofil-Kalender — letzterer geht in F3/K1 auf): `ChartManager.cs:309/373`, `CsvExportClass.cs:150`, `Form_Simulation_Detail.cs:3975/4007`, `Form_Gebaeude2.cs:64/81-82`, `WaermequelleClass.cs:935` | kosmetisch / K1 |
| O-5 | `MaxP` bleibt `float[100]` und wird nirgends gelesen (reines Schreibfeld; > 100 Gebäude liefen wie bisher in IndexOutOfRange) | Aufräumkandidat K1 |
| O-6 | Datenbefund aus der Migrations-Diagnose der Arbeitskopie: „`PufferHeizung` ohne `WS_ID_Puffer`: **2** (erwartet 0)" — zwei Anlagen mit Puffer-Ziel ohne Puffer-Referenz (Normalisieren behandelt sie zur Laufzeit als Heizkreis mit Protokollwarnung). Bestandsdaten, kein Codefehler | Anwender/S1-Migration |
| O-7 | **Neue Referenzbasis einfrieren**: der Nachher-Stand ist der fachlich richtige; das formale Einfrieren nach LIESMICH-Konvention (frische Kopie bei **geschlossener** Anwendung, Selbstvergleich, Ordner unter `Referenzlaeufe\`) steht aus — sinnvoll gemeinsam mit den **vier neuen Referenzprojekten** aus Konzept 11.1 (Mehrgebäude; zwei Puffer je Kanal; Prozesswärme mit eigenem Puffer; Booster-Kette mit Kombi-Speicher), die Projektdaten des Anwenders brauchen | vor K1 |

Läufe und Kopie liegen unter `C:\Waermeplan\V0_Test\{DB, Vorher, Nachher}` (außerhalb des Repos).

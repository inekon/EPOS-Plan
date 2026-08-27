# Paket S1 — Senkentabelle und Ladephasen je Rang: Umsetzungsprotokoll

Stand: 27.08.2026 · Branch `Pufferspeicher` · Bezug:
[`Konzept_Brauchwasser_Heizung_Pufferspeicher.md`](Konzept_Brauchwasser_Heizung_Pufferspeicher.md)
Kapitel 4.4, 5, 9 (Schritt 50), Entscheidungen **F4** (unbegrenzter Rang), **F5** (sechs
Zielwerte + Klassen-Set), **F11** (BHKW-Fahrweisen-Umbau), **F17** (Regel R-Prozess).
Build x64 Debug: 0 Fehler.

## 1. Umfang

Die zwei festen Senkenplätze je Anlage (`WS_*`/`WS_*2` in `Tab_Energieanlagen`) werden durch
die **geordnete Senkentabelle `Z_AnlageSenke`** abgelöst: beliebig viele Senken je Anlage,
frei sortierbar (Rang 1..n), jede mit eigenen Ladeparametern. Die Engine rechnet die
Direktsenken-Kette und die Ladephasen **je Rang**; die beiden Interimsregeln I1/I2 aus K2
sind ersatzlos abgerissen — Prozesswärme deckt nur noch, wer eine Senkenzeile mit Ziel
`Prozesswaerme` hat, und die Speicherentladung folgt dem echten Klassen-Set. Das BHKW
bewirtschaftet seine Puffersenken als **Auftragsliste** (F11) statt als Haupt-/Zweitsenke.

## 2. Bausteine

| Baustein | Inhalt | Stellen |
|---|---|---|
| **Senkentabelle** | `Z_AnlageSenkeModel` (Rang, Ziel, Bedarfsart, ID_Puffer, Ladeprio, Ladeprio_PV, Ladegrenze in PROZENT, `Anschlusshoehe` als P1-Vorgriff mit −1 = nicht gesetzt) + `Z_AnlageSenkeCtrl` (LesenJeProjekt/JeAnlage, SchreibenJeAnlage, SpalteVorhanden) | NEU `Model/Z_AnlageSenkeModel.cs`, `Controller/Z_AnlageSenkeCtrl.cs` |
| **Schritt 50** | 50a Tabelle + Beziehungen (`FK_AnlageSenke_Anlage` **mit Löschweitergabe** — restriktiv scheiterte der Del+Add-Speicherweg, Begründung bei `SQL_FK_SENKE_ANLAGE`); 50b Slots→Ränge unverändert übernehmen (`WS_Typ`→`Bedarfsart`, Anlagen ohne `WS_Ziel` → Heizkreis/Beides, `Ladeprio_PV` nur Rang 1); 50c `Z_AnlagePufferVerbund.ID_Senke`; **50d Regel R-Prozess** (F17): Projekte mit Prozesswärme — Anlagen mit Heizkreis-Senke der Bedarfsart Beides/Heizung bekommen eine `Prozesswaerme`-Zeile unmittelbar NACH der Heizkreiszeile; `ZIEL_VERSION` 49 → 50 | `SchemaMigration.cs` |
| **Steuerwerte** | `WS_ZIEL_PROZESS = "Prozesswaerme"`, `WS_ZIEL_PUFFER_PROZESS = "PufferProzess"` — die Zielliste ist damit sechswertig (L5) | `DbWerte.cs:1076/:1098` |
| **Senkenliste in der Engine** | `Senkenzeile`/`Senkenliste` (`SimulationKanaele.cs:1136/:1226`), `WaermesenkeClass` lädt die Tabelle mit **Rückfall auf die `WS_*`-Spalten**, solange Schritt 50 nicht gelaufen ist; `Ladeauftrag.Rang` | `SimulationKanaele.cs`, `WaermesenkeClass.cs`, `Ladeordnung.cs:168` |
| **Abriss I1/I2** | `DirektsenkeMaske`: Beides → {B, H} **ohne** Interims-P; `SenkenMaske(Senkenzeile)` ist die EINE Stelle Zeile→Kanäle (Heizkreis → Bedarfsart-Maske, Prozesswaerme → {P}, Puffer-Ziele → null); die Entladung fragt `SimulationPufferspeicher.BedientKanal` (echtes Klassen-Set) | `Kaskadenschleife.cs:183-281` |
| **Ladephasen je Rang** | Direktsenken-Kette in Rangfolge (Direktsenken auch ab Rang 2), Ladephasen je Rang mit eigener Obergrenze/PV-Regel; WP hält die Aufträge je Modul (`_zkAuftraege`) | `Kaskadenschleife.cs`, `SimulationWaermepumpe.cs:1032-1152` |
| **BHKW-Fahrweisen (F11)** | `SimulationBHKW.Auftraege` (Liste statt Haupt/Zweit), Wärmeraum = Kanalbedarf + Σ Ladefähigkeiten **aller** Puffersenken (`_raumJeAuftrag`), Reservierung als Liste je Zielspeicher (N3 je Auftrag), Zuordnung zentral in `Kaskadenschleife.BhkwAuftraegeZuordnen`; Beifang K2-O2: `BedientKanal(int)` statt `IstBrauchwasserkanal` | `SimulationBHKW.cs`, `Kaskadenschleife.cs:1007-1040` |
| **Senkendialog** | `Form_Waermesenke` als geordnete Senkenliste (sechs Ziele inkl. Prozesswärme/Puffer Prozess, Rang-1-Pflicht, Umsortieren); **Ränge 1 und 2 werden auf die `WS_*`-Altspalten gespiegelt** (Altanzeigen und Rückfallpfad lesen weiter von dort; Prozess-Ziele sind der einzige Fall ohne Altspalten-Entsprechung); ohne Tabelle (Migration nicht gelaufen) bleibt es bei der Spiegelung | `Views/Simulation/Form_Waermesenke.cs` |
| **Erzeugerkarten** | Senkenkette als Chips je Karte statt fester Senke/Zweitsenke | `Form_Simulation_Config.Karten.cs` |
| **Speicherweg (Senkenrettung)** | Der Del+Add-Speicherweg löscht Anlagenzeilen — die Löschweitergabe nähme die Senkenlisten mit. `WizardCtrl.SenkenSichern/-Wiederherstellen` rettet sie im Arbeitsspeicher über (ID_Type, Bezeichner) auf die NEUEN Anlagen-IDs, **vor** `GeraeteWaisen.Aufraeumen` (ab Rang 3 ist `Z_AnlageSenke.ID_Puffer` der einzige Verweis auf den Speicher). Die **Puffer-Anlagenzeilen** selbst brauchen keine Rettung: der typlose Löschweg verschont `ID_Type 12` (FR-1, eigenes Protokoll [`FR1_PufferVerlust_Protokoll.md`](FR1_PufferVerlust_Protokoll.md)) | `Controller/WizardCtrl.cs` |
| **Waisenlauf / Duplizieren** | `GeraeteWaisen.Referenzen` zählt `Z_AnlageSenke.ID_Puffer` als vierte Verweisquelle (ohne Projektfilter — die Tabelle führt kein `ID_Projekt`, fehlende Tabelle gilt als leer); `ProjektDuplizierenCtrl`: `Z_AnlageSenke` in KINDER (Filter `ID_Anlage IN (SELECT …)`) und `ID_Senke` in der FK_MAP | `GeraeteWaisen.cs`, `ProjektDuplizierenCtrl.cs` |
| **Aufräumen (K2-Reste)** | K2-O1: `Kanalabzug` nach `Kaskadenschleife.cs:1824` umgezogen; K2-O3: Übergangsabbildung `SimulationWaermebedarf.Kanaele()` gelöscht | `Kaskadenschleife.cs`, `SimulationWaermebedarf.cs` |

## 3. Verifikation

Referenzlauf (Weg A, Arbeitskopie migriert auf Schemastand **50**) gegen Basis
`2026-08-27_K1`:

**9/9 PASS — und 216/216 CSV byte-/MD5-gleich.** Der komplette Umbau ist auf der
Referenzmenge nachweisbar verhaltensneutral, **einschließlich der Migration**: Die Engine
liest auf der Arbeitskopie bereits aus `Z_AnlageSenke` (Schritt 50 gelaufen), nicht mehr aus
den `WS_*`-Slots. Migrationsbeleg der Arbeitskopie: **68** Anlagen mit Rang-1-Senke
übernommen, davon **14** mit Rang-2-Senke; Regel R-Prozess hat **8** Prozesswärme-Zeilen
ergänzt und 1 nachfolgenden Rang hochgeschoben.

Einordnung wie bei K2: In der Referenzmenge führt kein Speicherstufen-Projekt Prozesswärme
(1011 rechnet bis A1 im Altpfad), die BHKW-Projekte (1017, 1018, 1024, 1030) fahren mit 1–2
Aufträgen — genau der Bereich, für den der F11-Umbau Byte-Gleichheit zusagt. Die
dokumentiert ergebnisändernden Anteile von S1 (Prozess-Herauslösung, >2 Senken,
Direktsenken ab Rang 2) werden erst mit entsprechend konfigurierten Projekten real.

**Die vier neuen Anwenderprojekte** (1039 Mehrgebäude, 1040 zwei Puffer je Kanal,
1041 Prozesswärme mit eigenem Puffer, 1042 Booster-Kette mit Kombi-Speicher) rechnen auf dem
migrierten Stand **fehlerfrei durch** (Exit 0, keine `Simulation FEHLER`): 1040/1041 mit
arbeitendem Parallelverbund (2 × 3000 l als ein Vorrat), 1042 mit der Sole-Wasser-WP
CS7800iLW 16 auf Kennlinie. Verbleibende Meldungen sind Anwender-Datenpflege (Puffer ohne
Temperaturpaar → Rückfall-ΔT 10 K) und bekannte Kennlinien-Hinweise. **In die eingefrorene
Referenzbasis aufgenommen werden die vier erst nach A1** — vorher fehlen ihnen noch
Quellkonfiguration (Booster) und der endgültige Rechenweg (Altpfad-Abriss).

**Die Basis `2026-08-27_K1` bleibt unverändert gültig.**

## 4. Offene Punkte / Notizen

| # | Punkt | Ziel |
|---|---|---|
| S1-O1 | Senkenrettung erkennt Anlagen über (ID_Type, Bezeichner) wieder — **wer eine Anlage im Dialog umbenennt, verliert ihre Senken** (Rückfall Heizkreis/Beides); dieselbe dokumentierte Grenze wie bei den Speichervarianten (AP9b) und `CopyFromStamm` | dokumentiert, bleibt |
| S1-O2 | `Ladeprio_PV` vergibt die Migration nur an Rang 1 (die Sonderregel hing konstruktiv an der Hauptsenke); der Dialog kann sie je Senke setzen | dokumentiert, bleibt |
| S1-O3 | `Anschlusshoehe` wird mit Schritt 50 nur angelegt, nicht gelesen | P1 |
| S1-O4 | Warnkriterienkatalog W1–W6 (Ziel ∉ Klassen-Set, Speichertyp ≠ Set, Temperatur-Plausibilität …) fehlt noch — der Dialog verhindert nur Hartes (leeres Set, Rang-1-Pflicht) | S2 |
| S1-O5 | Die `WS_*`-Spiegelung (Ränge 1/2) ist eine bewusste Doppelablage, solange Altpfad und Altanzeigen von dort lesen — Abriss der Spiegelung zusammen mit den Spalten | A1 |
| S1-O6 | Ergebnis-Buchführung je Kanal (`_entladungJeArtKanal`) weiter ohne Persistenz | E1 |
| S1-O7 | K2-O5 (`PSP_FEHLER_VERWENDUNG_PFLICHT` verwaist), K2-O6 (EntladeleistungMax-Zwei-Pass), K2-O8 (Set-Wechsel-Rückfrage) unverändert offen | S2/P1 |
| S1-O8 | Engine-/Dialogtexte des S1-Umbaus teilweise inline deutsch (Nachbarstil) — Ressourcen-Nachzug | Paket L |

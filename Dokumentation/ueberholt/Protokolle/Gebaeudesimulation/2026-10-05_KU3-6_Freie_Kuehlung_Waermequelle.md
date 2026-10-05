# Protokoll KU3-6 — Freie Kühlung über die Wärmequelle der Wärmepumpe (05.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Wellen KU3-6a/6b/6c (E75, Plan [2026-10-05_Plan_KU3-6.md](2026-10-05_Plan_KU3-6.md)), Commits 6a `1b5f1d3`, `076046b` (Datenbank 187), 6c `1e06df1`, `2b82d5d`, Merge `263c6fa`, Hüllen `2f2f88a`, Konzept `c3face6`, 6b `58ab5f3`, `80fbe3d`, `b072a43`, `f64fb7f`, Merge `a1291ae`.
**Entscheid:** E75 (Q28: freie Kühlung über die Wärmequelle als KU3-6 nach AK2). Schemaschritt 187 `FreieKuehlungSoleSchema`. Basis R37 unverändert.

## 1 Auftrag

Eine Sole-Wasser- oder Wasser-Wasser-Wärmepumpe im Kühlbetrieb deckt je Stunde Kälte vor dem Verdichter direkt aus ihrer Wärmequelle, solange Quellentemperatur plus Grädigkeit des Wärmetauschers unter dem Kaltwasser-Vorlauf liegt; Stunden und Kälte werden gezählt (Ergebnis, Bericht, Szenariomengen). Die Rückwirkung auf die Sonde (K8c) bleibt vertagt.

## 2 Vorgehen

Drei Opus-Agenten in eigenen Worktrees: 6a (Schema, Modell, Anlagenpfade, Ergebnisspalten, Testdatenbank) und 6c (Dialog, DTO, KI-Feldkarte, Wachen, Ressourcen, Wiki) parallel ab `1fcdb64`, 6b (Rechenweg, Zähler, Kennzahlen, Vorlagen, Proben) auf dem Merge `263c6fa`. Die Orchestrierung trug die Windows-Hüllen (Anlagendialog, Schreibweg Simulation › Konfiguration, Gabenfabrik mit Sperrgrund) und den Konzeptsatz nach. Gate 735 im eigenen Worktree. Das LFS-Objekt der Datenbank 187 wurde erstmals direkt aus der Cloud-Sitzung hochgeladen (Netzfreigabe `lfs.github.com`).

## 3 Ergebnis

**6a (Schema 187, Modell, Ergebnis).** `Tab_Energieanlagen`: `Kuehl_Frei` (0/1, NOT NULL DEFAULT 0), `Kuehl_Frei_Graedigkeit_K` (REAL, NULL oder 0–20), `Kuehl_Frei_Leistung_kW` (REAL, NULL oder > 0); `Tab_ErgebnisWaermepumpe` und `Tab_ErgebnisWaermepumpeModul`: `FreieKuehlung_MWh`, `FreieKuehlung_Stunden`. Registriert in `SchemaMigration`, `SchemaStand`, `Paketanhebung`, `TestDatenbank`, `Werkzeuge/Testdatenbankschema`. `AnlagenSql` 77 statt 74 Platzhalter mit Variante ohne freie Kühlung; `WErzeugerCtrl.KonfigurationFelder` mit Gruppenschalter `FreieKuehlung`; Duplikat, Transfer, Komponentenübernahme tragen die Spalten; `ErgebnisCtrl` schreibt und liest NULL-erhaltend; `Referenzlauf/Ergebnisexport.SpaltenNurMitWert` hält die Basis. Stempeltrigger unverändert (Betriebsvorgaben, keine Kostenspalten). Testdatenbank 187: 87 736 320 Byte, OID 2673bb0f…. Elf Tests `FreieKuehlungSoleSchemaTests`.

**6b (Rechenweg, Bericht).** `KaelteFestwerte.FREIE_KUEHLUNG_SOLE_GRAEDIGKEIT_K` = 3,0 K. `SimulationControl.FreieKuehlungSoleMoeglich(wpTyp, wqTyp)` lässt nur Sole-Wasser/Wasser-Wasser mit Erdreich, Konstant, Profil oder CSV zu, sonst Warnung `SIMENG_KAELTE_WP_FREI_OHNE_QUELLE` und Schalter ohne Wirkung. `Kaelteerzeuger`: `FreieKuehlungSole`, `FreieKuehlungGraedigkeitK`, `FreieKuehlungLeistungKw`, `KuehlVorlaufC` (gepflegter `Kuehl_Vorlauf`, sonst Vorlauf der Kennlinie); Zähler `StundenFreieKuehlung`/`KaelteFreiKwh` gemeinsam mit der Kältemaschine; Kaskade mit `FreieKuehlungWp_stuendlich`. Stundenregel: freier Anteil = min(Raumlast + Ladewunsch des Kältespeichers, Grenze × Zeitanteil), Strom über Ersatz-EER 15 plus Hilfsstrom, Rest über den Verdichter (Taktverlust nur auf den Verdichteranteil). `SimulationRunner` befüllt Modul und Summe nur bei Wirkung (sonst NULL); Skalare `Kaelte[i].WpFreieKuehlungStunden/-Mwh`. Kennzahlen `kaelte.wp.frei`, `kaelte.wp.frei_stunden` („(sensibel)“), Katalogfassung 12 → 13 (`Vorlagenfeldkatalog_v13.txt`), alle zehn Vorlagen neu erzeugt, zwei Berichtszeilen im Wärmepumpenblock, `SzenarioMengen` skaliert MWh und reicht Stunden durch. Rechenprobe 16 Fälle, Datenbankprobe 2 Fälle (1047 mit Sondenquelle von 1029: 616 Stunden frei, 3,836 MWh frei, Kältestrom 0,732 → 0,268 MWh, Wärmeproduktion bitgleich).

**6c (Oberfläche, Wiki).** `WaermepumpeAnlageDaten` mit `KuehlFrei`, `KuehlFreiGraedigkeitK`, `KuehlFreiLeistungKw` und Gabe `FreiSperrgrund`; Gruppe „Kühlbetrieb“ in `WaermepumpeKonfiguration.razor` mit weich gesperrtem Schalter und zwei Zahlenfeldern (Regel `FreieKuehlungSperrgrundAus`); `SimulationKonfigSeite` reicht den Sperrgrund je Karte; beide KI-Feldkarten um drei Felder; 14 Ressourcenschlüssel de/en; `KiMaskenabdeckungWacheTests` 19 → 22; sechs neue Feldtests. Wiki-Quelle Kühlung: Feldtabelle, Absatz „Freie Kühlung über die Wärmequelle“ (Anker `freie-kuehlung-waermequelle`), Ergebnisse, Grenzen (keine Rückwirkung auf die Sonde). Windows-Hüllen: `WaermepumpeAnlageHuelle` füllt und schreibt die drei Felder, `WaermepumpeKuehlGabenBau.Bauen(idProjekt, wqTyp)` trägt den Sperrgrund mit `WPCtrl.BauartDesGeraets`, `SimulationHuelle` schreibt über den Gruppenschalter.

## 4 Festlegungen

F1 bis F6 des Plans gelten so gebaut; Konzept Kühlung 5.4 „So gebaut (KU3-6)“. Randfall benannt: Quelle Konstant ohne `WQ_Temp` fällt im Lauf auf die Außenluft zurück, der Schalter gilt dennoch als wirksam (Wortlaut F2).

## 5 Nachweise

Gate 735 auf `a1291ae`: Kern 11 136 grün (3 übersprungen); UI 7 550 grün; Referenzlauf 21/21 PASS (6 872 105 Werte), Plattformnachweis PASS gegen R37; Windows-Schale auf Linux 0 Fehler; Designer ohne Abweichung; SQL-Dialekt 0 Fundstellen; Werkzeugtests grün; ChartProben-Messlatte unverändert.

## 6 Offenes

Sichtabnahme unter Windows (Dialog, Bericht), Wiki-Upload der Kühlungsseite mit Logbuch-Satz, CI-Vermerk nach dem Push. K8c (Rückwirkung auf die Sonde) vertagt. Nächste Stufen: AK3 nach Q34 (H6), GA mit Rückfrage Ablösekriterium.

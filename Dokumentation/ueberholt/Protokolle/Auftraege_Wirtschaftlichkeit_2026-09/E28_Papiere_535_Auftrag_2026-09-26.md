# Auftrag Papiere #535 — E28 Prüfwelle N7: PV-Modus der Wärmepumpe nur auf PV-Überschuss, Strom-Stufeneingang von Kessel- und PV-Zeile geklemmt (26.09.2026)

Stand: pm26 = 6695caec (Merge e28 „(#535)" über origin 45c35a94 (#534)). Fakten: `Fakten_2026-09-23/e28_berichte.md` (Phase 0 und Phase 1), Entscheide im
`E28_Auftrag_2026-09-26.md` (Q1 a, Q2 a, Q3 a, Q4 keine Neueinfrierung, Q5 a; 09:10), `Entscheide_2026-09-26_Restpunkte.md`; Sichtprüfung 1030 `p1030_bericht.md`.
CI-Vermerk #534: trägt die Hauptsitzung nach dem CI-Lauf selbst nach (Aufgabe 6 entfällt).

## FAKTEN-BAU
Commits im Worktree e28 (Basis 6324e65d, Trailer Opus): ed8a307e E28/1 — `SimulationControl.PvUeberschussVorab(potenzial, bedarf)` (`internal static`, ~:4350-4371; negativer Bedarf zählt als 0, Überschuss = max(0, Potenzial − Bedarf)), Aufruf in `PV_Ueberschuss_Vorabberechnen` :4331-4333, ohne negativen Bedarf bitgleich; 3337810b E28/2 — Kesselzeile klemmt ihren Stromeingang je Stunde über `NetzbezugGeklemmt` an drei Wegen (Nachzug hinter der Wärmepumpe :894-899, Mitglied der Speicherstufe :1305-1309 [BHKW bekommt weiter den ungeklemmten Eingang], Vektorstufe `Simulation_SPK_Ctrl_Zweikanalig` :1983-1986) und N8 PV-Zeile (`SimulationRunner.cs:1031-1033`, `SimulationErgebnisCtrl.cs:866-867`); eac2378f E28/3 — `EPOS.Kern.Tests/StromStufeneingangKlemmeTests.cs`, 13 Fälle (Theorie PvUeberschussVorab (10,4→6) (10,−5→10) (0,−5→0) (3,8→0) (0,0→0); Bitgleichheit alte/neue Formel über 8.760 Zufallsstunden inkl. ±0,0; Kessel-Klemme je Stunde; Anker Heizkessel.Strombedarf 1017 635,2 / 1018 0 / 1030 4.790,09 / 1047 640,19, Ergebnismodell = Ergebnisansicht; N8: 1018 + PV-Anlage von 1040 → PV-Zeile Strombedarf 0 statt −27,46 MWh, BHKW-Überschuss 27.457,51 kWh bleibt); keine Vorrichtung für Stelle 1 mit echtem BHKW-Überschuss (bräuchte WP samt Gerät, Senke, Puffer in einem BHKW-Projekt — Begründung im Klassenkommentar). Nachweise E28/4: Release-Build 0 Fehler; Kern gefiltert 161/161; voller Lauf slnf mit Schaltern 15.593 grün, 2 übersprungen, 0 rot (Kern 8.100/8.101, UI 6.531, KiKern 549, SE 386, SP 27/28), `ZapfprofilReferenzprojektWacheTests` grün; Referenzlauf 14/14 gegen R20 GESAMT PASS 4.610.207 Werte, 432/432 CSV byte-gleich; 1048 32/32 CSV byte-gleich zu 6324e65d → keine Neueinfrierung, kein neuer Anker. Merge pm26 = 6695caec (über origin 45c35a94 = #534). Testhost-Regel einmal verletzt (Probelauf 08:31, 2 s, danach Warteskript). Aufwand ~1 h + 25 min Läufe. Abnahme A‑E28‑1: (1) 1017/1018/1030/1047 rechnen → Kessel-Reiter Strombedarf 635,20 / 0 / 4.790,09 / 640,19 MWh unverändert; (2) Projektkopie 1018 + PV-Anlage von 1040 → PV-Reiter Strombedarf 0 statt −27,46, PV-Einspeisung 6,60 MWh und KWK-Einspeisung 27,46 MWh wie bisher; (3) Kern-Lauf gegen R20 grün. Kein Logbuchsatz (keine Referenzrechnung ändert sich). Restpunkte: Deckungsgrad der PV-Zeile teilt durch den ungeklemmten Strombedarf (`SimulationRunner.cs:1035-1036`, `SimulationErgebnisCtrl.cs:858/864`; hinter BHKW-Überschuss zu hoch; nicht Teil von Q3, für E29 prüfen); Vorrichtung für Stelle 1 erst mit einem Referenzprojekt WP im PV-Modus hinter BHKW-Überschuss.

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #535 (E28) für EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build, kein Test, kein Zweigwechsel, kein Push, kein Merge, kein Stash.
ARBEITSORT: Worktree `.claude/worktrees/papiere535` (Zweig `papiere535` ab 6695caec); von der Repowurzel `C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiere535`,
nie im Hauptbaum. Commits sofort mit `git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Formregeln wie #521/#518 (Kern-Wellen mit
Protokoll und Register); Edit-Werkzeug, Zeilenenden erhalten (Statusdatei CRLF; Konzept/Register/Protokolle prüfen); Datum mit `date`.

FAKTEN: dieser Auftrag; `e28_berichte.md`; `e27_berichte.md` (N7-Herkunft); `p1030_bericht.md`; Statuszeilen #521 (E27) und #518 (E26) mit Nach-Blöcken und
Protokollen `Dokumentation/ueberholt/Protokolle/Reporting/E27_BhkwNetzbezug_Klemme_R19_Protokoll.md` als Muster; Register R‑E27 (E27‑Q7 „nur melden → E28").

AUFGABEN: (1) Statusdatei: Zeile #535 nach #534 vor `---` (Anlass N7/E27‑Q7, Anwenderentscheid 26.09.2026 „Prüfwelle ausführen"; Befund: latent, kein Projekt betroffen,
R20 byte-gleich, Kapitalwert 0 €; Änderung E28/1–3 mit Fundstellen; Nachweis aus FAKTEN-BAU; **Gate:** NACHTRAG-535-GATE; **CI:** NACHTRAG-535-CI; Logbuchsatz: nein —
kein sichtbares Verhalten für Bestandsprojekte, Begründung in die Zeile) und Block Nach #535 vor Nach #534: (a) Rechenweg beider Stellen in Worten und warum latent,
(b) Regel neu (PV-Modus nur PV-Überschuss; Kessel-/PV-Zeile je Stunde geklemmt, einheitlich mit E27‑Q4), (c) N8 als Nebenbefund erledigt, (d) Testklasse und Anker,
(e) Restpunkte aus dem Baubericht, (f) Abnahme A‑E28‑1 (Windows-Sichtabnahme: Kessel-Reiter Strombedarf bei 1030 unverändert 4.790,09; kein Projekt mit WP im
PV-Modus vorhanden — Hinweis, dass die Abnahme erst mit einem solchen Projekt greift), (g) Nachweis-Platzhalter. (2) Protokoll `Dokumentation/ueberholt/Protokolle/Reporting/E28_Stromstufeneingang_Klemme_Protokoll.md`
nach Muster E27 (Anlass, Befund mit Messwerten aus Phase 0, Entscheide, Bau, Nachweise, Restpunkte) und Eintrag im Protokoll-Index derselben Ebene.
(3) Register: Familie R‑E28 (E28‑Q1…Q5 mit Entscheid 26.09.2026 nach Empfehlung, Orchestrator; Anwenderentscheid „Prüfwelle ausführen") nach Muster R‑E27; in R‑E27 bei
E27‑Q7 „→ geprüft und gebaut in E28 (#535)". (4) Konzept `Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`: § 6.3 Nr. 36 um den Satz ergänzen, dass N7 mit E28 (#535)
geprüft und behoben ist (latent, N8 mit); § 3.6 an der Stelle der Netzbezugsklemme (E27) einen Satz zur Klemme des Strom-Stufeneingangs und zur PV-Modus-Regel; § 6.1 Zeile E28.
Analyse `2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md` § 5: Zeile E28 „umgesetzt #535" nach E27 einfügen (Spalten wie die Nachbarn). (5) Sichtprüfung 1030
(Papier-Teil, Anwenderentscheid „Sichtprüfung", Befunde aus `p1030_bericht.md`): Konzept § 6.2 — den Anker `WirtschaftlichkeitAnkerTests.cs:331` (Betriebskosten 1030 =
20.000 €/a) und die beiden Kapitalwert-Anker (−21.895.377,28 € Lauf 212 `WirtschaftlichkeitAnkerTests.cs:328`; −31.142.971,06 € Berichtsdaten `PvAusweisStromMatrixTests.cs:243`;
Differenz ungeklärt, B8) in die Ankertafel aufnehmen und den Satz „kein Anker" berichtigen; § 6.3 Nr. 21 durchstreichen „— Sichtprüfung 26.09.2026 (P1030): 20.000 €/a plausibel,
1030 ist Regressionsprojekt ohne vollständige VDI‑2067-Positionen; Befunde B3/B5 (Doppelanlage der Wartung, Bemessungsart Hilfsenergie), B4 (Hilfsstrom fehlt), B8 (zwei
Kapitalwert-Anker) offen zum Anwenderentscheid (→ Register R‑Rest)"; veraltete Fundstelle `InvestKaskadeTests.cs:281` → `:359`; Register R‑Rest: Zeile Nr. 21 von „läuft" auf
„Sichtprüfung erledigt, Befunde B3/B4/B5/B8 offen" setzen. (6) entfällt (CI-Vermerk #534 trägt die Hauptsitzung nach). (7) Nichts sonst.
(8) Bericht: Commit-SHAs, `git diff --stat`, Zeilennummern, verbliebene Platzhalter, keine Dateiabzüge.

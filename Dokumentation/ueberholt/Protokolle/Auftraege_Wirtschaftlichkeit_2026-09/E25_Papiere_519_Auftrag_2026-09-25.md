# Auftrag Papiere E25 — Prüfprojekt 1048 „PV mit vollständigen Preisen", Statusnummer #519, kein Schemaschritt, Testdatenbank LFS b68638da (25.09.2026)

Merge 3936003c (pm26 ab origin ba798d8a; e25 = e0f6d847 über a499feb7). Gate/CI: Platzhalter NACHTRAG-519-GATE / NACHTRAG-519-CI. Muster: Statuszeile
#518 und Nach #518, `E26_Papiere_518_Auftrag_2026-09-25.md`. Zusatz: CI-Vermerk des Pushs ba798d8a in den Statuszeilen #514, #515 und #518 nachtragen.

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #519 (E25 — Prüfprojekt 1048 „PV mit Preisen" in der Testdatenbank ohne Referenzrolle: Kopie von 1040 über den
Kopierweg des Programms, Gebäude VDI 6007, 40 Module 10,40 kWp, Stromträger 60 0,30 €/kWh (0,26/0,36) + 120 €/a, Erdgas 63 0,80 €/Nm³ (0,70/0,95) +
150 €/a, Parametersatz Zins 3 %/20 a/2 %/1,5 %, Einspeisevergütung 0,08 (0,10/0,06), Kostenpositionen 1.200 €/kWp, Wartung 150 €/a, Instandhaltung
1 %; Skript `Referenzlaeufe/Skripte/pruefprojekt_1048_pv_preise.cs` wiederholbar; Testdatenbank LFS 19a7b632 → b68638da, 44.537 neue Zeilen, 0
bestehende geändert; Referenzbasis R18 bleibt, 14/14 byte-gleich; Anlass E9a-Befund und E21‑Q9 a, Anwender 25.09.2026 „nehme die Empfehlungen vor:
für Später"; kein Schemaschritt) für EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build, kein Test, kein Zweigwechsel, kein Push, kein Merge,
kein Stash. ARBEITSORT: Worktree `.claude/worktrees/papiere519` (Zweig `papiere519` ab 3936003c); von der Repowurzel `C:\Waermeplan\EPOS-Plan` aus
`cd .claude/worktrees/papiere519`, nie im Hauptbaum. Commits sofort mit `git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5
<noreply@anthropic.com>`. Formregeln wie #518; Änderungen mit dem Edit-Werkzeug, Zeilenenden erhalten; Datum mit `date` prüfen.

FAKTEN (ganz lesen): `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e25_berichte.md` (Phase 0 mit Fragen E25‑Q1…Q10 und Entscheiden,
Zwischenbericht E25/1+2, Phase 1: Commits, Projektaufbau, Zellvergleich, Wirkung von E26 an 1048, Tests, Wachen-Anpassungen, LIESMICH-Text, Abnahme),
`E25_Auftrag_2026-09-25.md`, `e9a_berichte.md` (:150–175, Anlass), `e26_berichte.md` (N1/N3, an 1048 gemessen); Konzept § 6.3 (Restpunkte-Tafel),
Register R‑E21 Q9 als Anlass; `Referenzlaeufe/LIESMICH.md` (Abschnitt Prüfprojekt 1048 und Nachtrag unter Aktuelle Basis, von E25 geschrieben —
prüfen, nicht doppelt ändern). CI-Nachweis für die Statuszeilen #514/#515/#518 (Push ba798d8a): Kern `main` 36175600275 grün, Kern
`ios_migration_september` 36175594535 grün, Windows `main` 36175600304 rot durch einen fremden Zeitmess-Test der Zapfprofil-Sitzung
(`TwwMessreihenCtrlTests.Hunderttausend_Zeilen_brauchen_unter_fuenf_Sekunden`, 10,97 s statt 5 s auf dem Windows-Läufer; Kern 7.437/7.438, UI 6.395;
der Zapfprofil-Sitzung gemeldet) — in allen drei Statuszeilen den Vermerk „steht aus (Beobachtung nach dem Push)" durch diesen Text ersetzen.

AUFGABEN: (1) Statusdatei: Zeile #519 nach #518 vor `---` (Anlass, Projektaufbau mit Werten, Zellvergleich, Wirkung E26 an 1048, Tests 13 Fälle +
Zählungen + Wachen, voller Lauf Kern 7.450/UI 6.395, Auslieferungsvorlage 36/36, SQL 1.927/0, Referenzlauf 14/14 gegen R18, Merge 3936003c, Zweig e25
Commits; **Gate:** NACHTRAG-519-GATE; **CI:** NACHTRAG-519-CI; kein Logbuchsatz (Testdaten) mit Begründung; Anwendersicht: nur die Testdatenbank, kein
Programmverhalten) und Block Nach #519 vor Nach #518: (a) E25‑Q1…Q10 mit Entscheiden (aus Phase 0), (b) Abnahme A‑E25‑1 (sechs Punkte aus dem Bericht),
(c) Nachweis (Zellvergleich, Referenzlauf, Tests, Testhost-Regel einmal verletzt), (d) Befunde („PV: vermiedener Bezug" nur bei aktivem
Vergütungsdialog — so gebaut; Kapitalwert-Anker nach E26 unverändert; N1/N3 an 1048 gemessen; die Stromzeile trägt wegen E24 eine andere ID als auf der
Arbeitskopie), (e) Papiernachzug (LIESMICH-Abschnitte), (f) Logbuch: kein Eintrag, (g) Restpunkte (Vergütungszeile/Tarifstruktur nicht angelegt;
Wiederholung des Skripts nach jeder Neufassung der Testdatenbank), (h) nächste Schritte (E27 läuft, R19; A‑E26‑1 an 1048 prüfbar), (i) Gate/CI-Platzhalter.
(2) Protokoll `Dokumentation/ueberholt/Protokolle/Reporting/E25_Pruefprojekt_1048_PV_Preise_Protokoll.md` (Muster E24/E26 ohne Einfrier-Abschnitt),
Index +1. (3) Konzept § 6.3: Restpunkt zum fehlenden PV-Preisprojekt (E9a-Befund; prüfen, ob eine Nr. ihn führt — sonst neue Nr.) erledigt; § 7
Schrittabsatz E25; Kopf Codestand 3936003c. (4) Register: Familie R‑E25 Q1…Q10, R‑E21 Q9 Umsetzungsstand „gebaut #519", Kopf/Familientafel;
Entscheidwege § 8.x. (5) Analysepapier § 5 Zeile E25 (#519), Kopf Codestand. (6) Kein Mockup, kein Wiki. (7) Bytes, `git diff --stat`, Bericht ohne
Dateiabzüge, verbliebene Platzhalter, Zeilennummern.

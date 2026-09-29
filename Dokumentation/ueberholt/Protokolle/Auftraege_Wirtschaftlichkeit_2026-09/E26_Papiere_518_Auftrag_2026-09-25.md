# Auftrag Papiere E26 — PV-Ausweis N1/N3, Referenzbasis R18, Statusnummer #518, kein Schemaschritt (25.09.2026)

Merge 025a8707 (pm26; e26 = 4653fa06 ab 868afc57). Gate/CI: Platzhalter NACHTRAG-518-GATE / NACHTRAG-518-CI. Muster: Statuszeile #514 und Nach #514
(E24, R17-Einfrierung) und `E24_Papiere_514_Auftrag_2026-09-25.md`. Statusnummer #518 (516/517 sind der Zapfprofil-Sitzung reserviert; beim Push wird
origin geprüft).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #518 (E26 — PV-Ausweis berichtigt: N1 `SimulationRunner.cs:989` speichert als Stromproduktion die Erzeugung der Module
statt des Direktverbrauchs; N3 neue Reihe `STROMBEDARF_GESAMT` (Rest nach der Kaskade + BHKW-Strom) als Bedarf der Strommatrix — WP, Heizstab,
Elektrokessel, Kältestrom der Stufenrechnung sind darin, KWK-Split misst sich daran (E26‑Q3 a); Referenzbasis neu eingefroren als
`Referenzlaeufe/2026-09-25_R18_PvAusweis`, R17 archiviert, einzige Wirkung der Skalar `Photovoltaik.Stromproduktion` in vier aggregate.csv;
Anwenderentscheid 25.09.2026 „Befunde aus E25: Empfehlung/bearbeiten"; kein Schemaschritt, Testdatenbank unverändert 0c2fe21a) für EPOS-Plan. Antworten
auf Deutsch. Nur Papiere, kein Build, kein Test, kein Zweigwechsel, kein Push, kein Merge, kein Stash. ARBEITSORT: Worktree
`.claude/worktrees/papiere518` (Zweig `papiere518` ab 025a8707); von der Repowurzel `C:\Waermeplan\EPOS-Plan` aus `cd .claude/worktrees/papiere518`,
nie im Hauptbaum. Commits sofort mit `git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Formregeln wie #514;
Änderungen mit dem Edit-Werkzeug (Python-Schreibskripte lehnt der Klassifizierer ab), Zeilenenden erhalten; Datum mit `date` prüfen. **Die
Basisnamen-Ersetzungen R17 → R18 hat E26 selbst gemacht** (CLAUDE.md, kern.yml, ios.yml, Dokumentation/LIESMICH.md, Konzept_Gebaeudesimulation,
Systementwurf, Konzept_Wirtschaftlichkeit Kopf/Tabelle/Nr. 21, Analysepapier Kopf/Legende, Basenhistorie; der Merge 025a8707 hat die Köpfe von Konzept
und Analysepapier zusammengeführt) — prüfen (`git grep -n R17_Datenpflege` darf nur noch Geschichte treffen), nicht doppelt ändern.

FAKTEN (ganz lesen): `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e26_berichte.md` (Phase 0: Ursachen, Leser, Zahlen, Fragen mit Entscheiden;
Phase 1: Commits, Zahlen vorher/nachher, Tests, A/B-Tafel R17→R18, Dateien der Einfrierung, Fundstellen, Konzeptvermerk-Satz, Logbuch-Vorschlag, Abnahme,
Restpunkte Q6/N5/N6), `e25_berichte.md` (Anlass N1/N3, Zeilen 40 und 61-67), `E26_Auftrag_2026-09-25.md`; Konzept § 3.6 (Strommatrix, ~2364-2381),
§ 6.4 (Eigenverbrauch), § 6.3 (Restpunkte-Tafel), Register R‑Rest/R‑E24 als Muster; `Referenzlaeufe/LIESMICH.md` (Abschnitt Aktuelle Basis R18, von E26
geschrieben) und `Dokumentation/ueberholt/Referenzbasen/LIESMICH.md` (R17-Eintrag von E26).

AUFGABEN: (1) Statusdatei: Zeile #518 nach #515 vor `---` (Anlass, N1/N3 mit Fundstellen, Zahlen 1040/1026/1042, Kapitalwert unverändert mit Ankern,
Tests 14.632/2/0, ChartProben 174/0, SQL 1.920/0, A/B R17→R18 vier Skalare, Basisname ersetzt, Merge, Zweig e26 Commits bc1d8ad8/4fd6eaa6/3136a267/4653fa06;
**Gate:** NACHTRAG-518-GATE; **CI:** NACHTRAG-518-CI; Logbuchsatz aus dem Bericht als Vorschlag mit „Version beim Anwender erfragen"; Anwendersicht:
Ergebnisansicht/Bericht PV-Stromerzeugung, „PV: vermiedener Bezug" ≥ 0, Rollentarif vermiedene Kosten positiv) und Block Nach #518 vor Nach #515: (a)
E26‑Q1…Q7 mit Entscheiden (Q3 mit dem Hinweis, dass der Kapitalwert fremder Projekte mit BHKW zwischen Haushalts- und Bruttostrom wandern kann — an den
Referenzen unverändert), (b) Abnahme A‑E26‑1, (c) Nachweis (Zahlen, A/B, Determinismus, Referenzlauf 14/14 gegen R18), (d) Befunde (1046 Abregelung 0,
vermiedene Menge < PV + Entladung weil Flotte aus dem Netz lädt; PV-Projekte der Testdatenbank ohne Strompreis → Kapitalwert-Anker an 1024/1030; Testhost-
Regel einmal verletzt, grün), (e) Papiernachzug (Basisname, Archiv R17), (f) Logbuch-Vorschlag, (g) Restpunkte Q6 (Strombilanz-Diagramm ChartRenderer.cs:457-471,
Excel ExcelBerichtGenerator.cs:1834), N5 (1018 negativer Netzbezug, kapitalwertwirksam — Empfehlung: eigene Welle E27), N6 (Übersicht ohne Kältestrom),
(h) nächste Schritte (E25 Prüfprojekt 1048 nach dem Push, dort A‑E26‑1 an 1048 prüfbar), (i) Gate/CI-Platzhalter (CI läuft gegen R18 — kern.yml/ios.yml
geändert). (2) Protokoll `Dokumentation/ueberholt/Protokolle/Reporting/E26_PvAusweis_Strommatrix_R18_Protokoll.md` (Muster E24-Protokoll mit Einfrier-
Abschnitt: Ursachen, Änderung je Datei, Zahlen-Tafeln, A/B, Dateien der Einfrierung, Fundstellen, Restpunkte), Index +1. (3) Konzept: § 3.6 den
Konzeptvermerk-Satz aus dem Bericht einarbeiten (Bedarf ohne Eigenerzeugung = alle Verbraucher, Reihe STROMBEDARF_GESAMT, KWK-Split daran); § 6.4 bzw. die
Stelle, die die PV-Stromproduktion/den Eigenverbrauch des Ausweises beschreibt, auf „Erzeugung der Module; Eigenverbrauch = Erzeugung − Einspeisung"
bringen; § 6.3: neue Nr. (fortlaufend) für E26 als erledigt mit Restpunkten Q6/N5/N6 benannt; Kopfzeile Codestand 025a8707 prüfen; § 7 Schrittabsatz E26.
(4) Register: Familie R‑E26 Q1…Q7 (Entscheide vom Orchestrator nach Empfehlung, Anwender „Empfehlung/bearbeiten"), Kopf/Familientafel; Entscheidwege-
Protokoll § 8.x E26. (5) Analysepapier § 5 Zeile E26 (#518) und Nachtrag (Köpfe sind schon auf R18/E26). (6) Archiv-LIESMICH: R17-Eintrag mit Datum/Grund
prüfen. (7) Kein Mockup; Wiki: kein Fachseiten-Text ändern, nur den Logbuch-Vorschlag in der Statuszeile. (8) Bytes, `git diff --stat`, Bericht ohne
Dateiabzüge, verbliebene Platzhalter, Zeilennummern.

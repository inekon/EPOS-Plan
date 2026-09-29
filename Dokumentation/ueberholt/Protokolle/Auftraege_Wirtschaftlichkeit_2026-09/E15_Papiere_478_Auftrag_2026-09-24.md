# Auftrag Papiere #478 — E15: Risikomodul nach DIN EN 17463 (V‑G7), Schemaschritt 125 (gesichert 24.09.2026)

Merge-SHA, Gate-Zahlen und CI-Nachweise nennt die Startnachricht (Platzhalter NACHTRAG-478-MERGE / NACHTRAG-478-GATE / NACHTRAG-478-CI;
ggf. -MERGE2/-GATE2 bei Nachzug). Muster: `E14_Papiere_477_Auftrag_2026-09-24.md`, Statuszeile #477 und Block Nach #477. Vor dem Schreiben
`grep -n "#47[0-9]\|#48[0-9]"` in der Statusdatei (#480 Dialog Design; #481 Zapfprofil Z4b, #482/#483 Dialog Design laufen; Schemastand nach
#478 = 125; Basis R14_Kaelteerzeuger).

## Wortlaut des Agentenauftrags (model: opus)

Papierpflege zur Statuszeile #478 (E15 — Risikomodul: Zinszuschlag oder Zahlungsstromabzug nach DIN EN 17463 6.5/Anhang F, optional,
Vorgabe aus) für EPOS-Plan. Antworten auf Deutsch. Nur Papiere, kein Build, kein Test, kein Zweigwechsel. ARBEITSORT: Worktree
`.claude/worktrees/papiere478` (Zweig `papiere478` ab NACHTRAG-478-MERGE); von der Repowurzel `C:\Waermeplan\EPOS-Plan` aus
`cd .claude/worktrees/papiere478`, nie im Hauptbaum. Commits sofort mit `git add <pfad>`, Trailer `Co-Authored-By: Claude Opus 5.5
<noreply@anthropic.com>`; kein Push, kein Merge, kein Stash. Formregeln wie #477 (UTF-8 ohne BOM, CRLF, byte-erhaltend; Mockup `<tr` =
`</tr>`; Wiki-Tabu-Regex 0 Treffer; Logbuch Version 1.2.0.4, Wiki `wirtschaftlichkeit`).

FAKTEN: `C:\Waermeplan\.claude\auftraege\Fakten_2026-09-23\e15_berichte.md` (Phase 1: Commits, Schema, Kern, Dialog, Schlüssel, Abweichungen
1–5, Fragen E15‑Q1…Q4, erledigt-Gründe, Logbuchsatz, Abnahme A‑E15‑1) und Phase 2 laut Startnachricht (Merge 8494f244 mit 6d022f6d —
ExcelFormelmappe in E14-Struktur mit Risiko-Block je Szenario —, E15/6 5eaed19c Dialogprobe; 13.001/0/1; Referenzlauf 13/13 R14 394 CSV;
A/B-Tafel: 1030 einzeln Zuschlag 1 %-Pkt = Lauf mit i+1 [Erwartet −31.141.243 → −28.306.379], Abzug 10.000 € × 10 % = −1.000 €/a → Erwartet
−14.877 €, Ungünstig −13.590, Günstig −16.351 (= −1.000 × RBF); 1019 Stamm/Referenz: Abzug 0, Zuschlag wie i+1; 1024 Variante: Abzug −14.877,
Kapitalwertdifferenz gegen Stamm −1.807.372 → −1.822.250; Rückweg exakt; Variante 1023 ohne Kapitalwert = Datenlücke Brennstoff ohne Träger
93,5 MWh/a; Zellvergleich 16 Gruppen gleich E14 + drei Risikogruppen hybrz/hybra/hybrza, Excel 16 abweichend 0 in 19 Mappen, hybrz +3
Formeln, hybra +123 Formeln, Differenz −13.590,33, Annuität −1.000; resx 9.961 je Sprache; SQL 1.807/0), `e14_berichte.md` (Formelmappe),
Konzept § 2.11.2 V‑G7, § 2.11.4 V‑E, Register R‑V (V‑G7). Alles ganz lesen. Normbezug: Anhang F Tabelle F.2 rechnet R_loss als Prozent des
Nettorückflusses — gebaut ist € je Periode (E15‑Q4 c als Lesart), das im Register und Konzept ausdrücklich nennen.

AUFGABEN: (1) Statusdatei: Zeile #478 (Anlass: Anwender 24.09.2026 „V‑G7 Risiko: eigener kleiner Auftrag ausführen") nach der letzten
Zeile vor `---` und Block Nach #478: (a) Fragen E15‑Q1…Q4 (offen, gebaut a; Q4 mit den drei Lesarten), (b) Abnahme A‑E15‑1, (c) Nachweis
(Vorgabe aus → Anker/Referenzlauf unverändert; A/B-Tafel gekürzt), (d) Befunde (Normabweichung R_loss €, Abzug trifft Variante nicht
Referenz, PV-Vergütungsdialog/KostenKomponenteHuelle ohne Zuschlag, gespeicherter Zins i+Δ, Datenlücke 1023, Designer-Nachtrag), (e)
Papiernachzug, (f) Logbuch, (g) nächste Schritte (E17 #479 Schritt 126, E16 #484; Wiki-Upload 26.09.), (h) Nachweis: Gate NACHTRAG-478-GATE,
CI NACHTRAG-478-CI. (2) Protokoll `Dokumentation/ueberholt/Protokolle/Reporting/E15_Risikomodul_Protokoll.md` (Muster E14), Index +1.
(3) Register: V‑G7 „gebaut #478 (Schritt 125)", neue Familie R‑E15 mit Q1…Q4 (offen, Empfehlung a); Konzept § 2.11.2 V‑G7 (fehlt → gebaut),
§ 2.11.4 V‑E (Risiko erledigt, V‑G3 offen → E16), § 2.11.5/§ 2.11.6 (Risikozeilen im Parameterblock, RISIKO-Bestandteil), § 6 Schemaschritt 125
mit Spalten, § 7/Anhang, Kopfzeile Codestand/Schemastand 125; Analysepapier § 6 (Schritt 125 = E15) und § 5; Entscheidwege-Protokoll.
(4) `Referenzlaeufe/LIESMICH.md`: Nachtrag Schritt 125 (Spalten, LFS 6c4c32f9, ergebnisneutral). (5) Mockup: Zone Parameter (Gruppe Risiko),
Annahmentafel/Deklaration 6.5/Checkliste Punkt 6, Ressourcentafel (+29, Stand 9.961), Stand-Absatz. (6) Logbuch (#478, ein Satz), Wiki-Quelle
Wirtschaftlichkeit: Abschnitt `risiko` ausformulieren (Art, Felder, Wirkung, Ausweis; Normbezug 6.5/Anhang F ohne Tabuwörter), Wiki_Update.
(7) Bytes, `<tr`, `git diff --stat`, Bericht ohne Dateiabzüge.

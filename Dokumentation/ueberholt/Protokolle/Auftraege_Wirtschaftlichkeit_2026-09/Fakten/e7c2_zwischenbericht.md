# E7c2 — Zwischenbericht des Bau-Agenten (Opus), 23.09.2026 13:50, Punkte 1–5

Zweig `e7c2` (Worktree `.claude/worktrees/e7c2`, Basis origin 4971556a, Schemastand 105 → Zielversion 109, Lücke 106 bis zum Nachzug).
Commits: e924834d E7c2/1 Schritt E (107) · 66620b70 E7c2/2 Schritt F (108) · 8854ba56 E7c2/3 Schritt G (109) · 657abb4a E7c2/4 S‑2 ·
4354b009 E7c2/5 B‑4 Rest. Kern-Filter und Windows-Schale je 0 Fehler; Designer gezogen. Tests geschrieben, nicht gelaufen; kein
Referenzlauf (Phase 2 steht aus). Testdatenbank nicht committet.

A/B (Messprogramm auf Scratchpad-Kopien, vorher = Basiscode + DB 105, nachher = neuer Code + DB 107/108/109): 13 Basisprojekte
nach jedem Punkt 9.195/9.195 Werte gleich (Anker, frischer Lauf, KWKG-/Ersatzreihen, Energiekosten, Emissionen).
Proben:
- E (1024, WP 15 a, Kessel 25 a): ohne Kennzeichen −2.897.442,20 € · WP „Ersatz nein" −2.895.805,46 · Kessel „Restwert nein"
  −2.897.995,88 · beide nein/nein −2.896.359,13 (= Anker) · „ja" = ohne.
- F: DML 5 Zeilen kWh (Regel), 23 Abrechnungseinheit; Kosten 1030/1024 mit Basis kWh gleich; U32 (Stadtgas ohne kWh-Regel) hält kWh.
- G: 5 Einheiten, 5 Preiseinheiten, 1 Preiszeile (1039) → Nm³; Brennstoff 24 „Sonstige" (m³, kein Gas) bleibt; Vorlagenbau auf
  109-Kopie 0 Auffälligkeiten.
- S‑2 (1030, Prod. Gewerbe, BHKW § 53, Kessel § 54): § 54 Jahr 1 7.987,41 → 0; KW −20.388.846,98 → −20.507.679,50; Kohärenzzeile
  WARNUNG. Gepinnte Zahlenprobe U7 (§ 53a + § 54) jetzt gesperrt: Summe alt 24.088,43 → neu 21.202,71.
- B‑4 (1030, Zeile auf 3 %, Konserve 10.000): Brennstoff 300 → 15.483,29 €/a (Basis 516.109,60), KW −21.900.695,28 →
  −22.169.844,81; Strom → 32.683,35 €/a, KW −22.474.745,07. Basisprojekte: einzige Zeile (1018) ohne Satz → gleich.

Offene Fragen (Lesart gebaut = Empfehlung):
- E7c2‑Q1 S‑2 § 53-Seite: a (gebaut) nur Anlagen mit Stromerzeugung zählen (Kessel mit § 53-Wahl bekommt ohnehin 0 → keine Sperre,
  der alte Fall‑5-Hinweis entfällt dort) / b jede § 53/53a-Wahl wie im alten Fall 5. Empfehlung a.
- E7c2‑Q2 B‑4 Bezugsgröße: a (gebaut) Arbeitskosten wie Weg A (Menge × Arbeitspreis; Strom = Netzbezug × Arbeitspreis) / b
  Gesamtkosten inkl. Grund-/Leistungspreis. Empfehlung a. Hinweis 1030: gespeicherter Lauf vor B‑1, Kessel ohne Verbrauch → Basis
  nimmt den aus der Wärme abgeleiteten Brennstoff (#363, wie Weg A) und liegt über dem Brennstoffanteil der gebuchten Energiekosten.
- E7c2‑Q3 F: Neue Zuordnungen aus Wizard/Katalog/Variantenträger schreiben keine Preisbasis → NULL = Abrechnungseinheit (Vorgabe,
  keine Herleitungszeile); Herleitungszeile nur bei fehlender Spalte. Empfehlung: so lassen.
- E7c2‑Q4 G: Brennstoff 24 „Sonstige" bleibt m³ (Entscheid nennt fünf Gase). Empfehlung: so lassen.
Schlüssel bisher: neu ERK_* (8), ND_TAFEL_ERSATZ_AUS/RESTWERT_AUS, KI_DLG_VOP_ERSATZ/RESTWERT_NAME/ERL, ETV_PREISBASIS_OHNE_SPALTE,
KOH_FALL5_MISCHLAGE_SPERRE, STEUER_ENERGIEST_54_MISCHLAGE; gestrichen KOH_FALL5_MISCHLAGE.

Der Agent arbeitet weiter an den Punkten 6–9 (V‑1/V‑2, E7c1‑Q2 b Kontingent aus KWK-Strom, E7c1‑Q1 Hinweis, E7c1‑Q7 Rest der
Überlagerung/KI-Feldkatalog/Berichtsspalten) und sendet danach den Phase‑1-Bericht. Phase 2 (Tests, Referenzlauf gegen R12, Designer,
SQL-Prüfer) erst nach „Tests freigegeben".

# E14 — Bericht Phase 1 (Opus, 24.09.2026, Worktree e14 ab 105cdb31)

Commits: dd0ec440 E14/1 Stufe 1 je Szenario (Mehrjahrestabellen Günstig/Ungünstig); d29f6496 E14/2 Stufe 2 je Szenario (Kennzahlen,
Zinsfuß, Bandbreite als Formel); 7935316d E14/3 Wertfassung = Formelfassung: Wachfälle; 172971e4 E14/4 Wachen und Ausweis (Anker,
Checkliste Punkt 11, T_s). Build 0 Fehler, Designer wiederholbar (nahm den fehlenden Eintrag `ZPG_EINGABE_STOCHASTIK_EINHEITSTAGE` mit auf).

Bau: Unter den Erwartet-Tabellen je ein Block „Mehrjahresübersicht … – Szenario Günstig/Ungünstig (T = n a)" mit Hinweiszeile und je
Stand eine Tabelle; Eingangswerte aus `BerechneVerlaufSzenarienJeZeitraum`, Parameter über `p.FuerSzenario(s)` (Namen `Zins_i_Guenstig`,
`p_E_Unguenstig` …); Tabellen bis max T, jenseits T_s Schutzformel `IF(A<=Zeitraum_T_s,…,"")`, Restwert am Ende von T_s; Nachweisblock
„vermiedene Kosten" nur bei Erwartet. Stufe 2: Kennzahlblöcke Günstig/Ungünstig mit NPV + Jahr 0 + Restwert-Barwert, PMT, Differenzreihe,
Amortisation; zwei Hilfsspalten (Vorzeichen über Nullwerte ≤ 1E‑6 fortgeschrieben, Zahl der Wechsel — Regel wie `KapitalwertRechner.
Vorzeichenwechsel`); Zinsfuß: 0 Wechsel „kein Zinsfuß bestimmbar", > 1 „nicht eindeutig", sonst `ROUND(IRR(…)*100,2)`; Bandbreitentafel
ΔKW als Zellbezug, Spanne `MAX-MIN`; jede Formel im `Formelregister` gegengerechnet. Entscheidnachtrag: E14‑Q2 a ersetzt E8b‑Q1 a. Wachfälle
`Excel_E14_Kennzahlen_Guenstig_und_Unguenstig_rechnen_in_Formeln` und `Excel_E14_Zeitraum_je_Szenario_mit_Schutzformel` (25/20/15 a);
Prüfgruppe `GruppeMitSaetzen` mit T_Günstig/T_Ungünstig. Ankerzeilen: Titel Günstig P+154 … Abschluss P+261, alles bis P+151 unverändert.
Checkliste Punkt 11 „die Mappe rechnet alle drei Szenarien formelbasiert" (de/en, Test). Parameterblock-Zeile „Betrachtungszeitraum T_s je
Szenario", `WIRT_FM_PARAM_HINWEIS` nennt Namen je Szenario und Tabellenlänge.

Schlüssel neu: `WIRT_FM_MJ_SZENARIO_TITEL`, `WIRT_FM_MJ_SZENARIO_HINWEIS`, `WIRT_FM_MJ_VORZEICHEN`, `WIRT_FM_MJ_WECHSEL`,
`WIRT_FM_IZF_NICHT_EINDEUTIG`; gefasst: `WIRT_FM_PARAM_ZEITRAUM`, `WIRT_FM_PARAM_HINWEIS`, `WIRT_AE_11_STAND`.

Abweichungen/Befunde: (1) Designer-Lücke fremd; (2) „Menge × Preis je Träger" in der Tabelle nicht zerlegt — Energiespalte = Jahr‑1-
Kernwert des Szenariolaufs mit p_E_s, Grund-/Leistungspreise bleiben Werte (§ 2.11.6), Stufe 3 bleibt Erwartet; (3) Q3 a ändert auch
Erwartet: mehrdeutiger Zinsfuß jetzt Text statt Zahl (nur Ausweis); (4) Formeltexte Erwartet: neue Zinsfußformel, zwei Hilfsspalten, bei
ungleichem T Schutzzeilen, Abschlusszeile rückt; (5) Wortbericht: Checkliste Punkt 11 neuer Text.

Fragen (gebaut a): E14‑Q1 Tabellenlänge (a eine Tabelle je Szenario bis max T mit Schutzformeln; b drei Blätter); E14‑Q2 Kennzahltafel
(a Formeln statt Werte, ersetzt E8b‑Q1 a; b Werte + Nebenblock); E14‑Q3 Zinsfuß bei mehreren Vorzeichenwechseln (a Text; b IRR mit Schätzwert).

Erledigt-Gründe: Konzept § 2.11.6 („Kennzahlen Günstig/Ungünstig bleiben Werte", „Formeln rechnen mit Erwartet" überholt); Register
E8b‑Q1 durch E14‑Q2 a abgelöst; E9a-Befund 1 erledigt; Mockup U43 Punkt 11. Logbuchsatz (`bericht`): „Die Formelmappe des Tabellenberichts
rechnet Mehrjahrestabellen, Kennzahlen und Bandbreite für alle drei Szenarien mit sichtbaren Formeln auf den Parametersatz des jeweiligen
Szenarios." Abnahme A‑E14‑1: 1030 rechnen, Tabellenbericht in Excel: Blöcke Günstig/Ungünstig, Kennzahlen als Formeln gleich der Seite;
`Zins_i_Guenstig` ändern → Günstig zieht mit; Zeitraum 25/15 → Tabellen bis Jahr 25, Ungünstig ab Jahr 16 leer; Checkliste Punkt 11.

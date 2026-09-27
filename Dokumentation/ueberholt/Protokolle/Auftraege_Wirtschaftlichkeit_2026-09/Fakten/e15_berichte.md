# E15 — Bericht Phase 1 (Opus, 24.09.2026, Worktree e15 ab 105cdb31)

Commits: d46280f7 E15/1 Schemaschritt 125 (vorläufig; Konstante `SCHRITT_125_RISIKOMODUL`, Zahl an vier Stellen: Konstante, Zielversion,
Werkzeugaufruf, Test `Migration_Werkzeug_und_Testdatenbank_ziehen_den_Schritt_aus_einer_Quelle`; Spaltenliste `SchemaKatalog.
RisikomodulSpalten`, Methode `Schritt_Risikomodul`), e111f7ee E15/2 Testdatenbank 125 (LFS 1d971b1a → 6c4c32f9, 67.792.896 B, 4 Spalten
leer), fe795e10 E15/3 Kern (`RisikoModul.cs` mit `Risikoart`; `FuerSzenario` i + Zuschlag in allen Szenarien; `KapitalwertRechner.Rechne`
Parameter `risikoAbzugJahr`: R_loss × p_loss/100 ab Jahr 1, nicht Jahr 0, nicht Restwert; Zahlungsbild `RisikoJeJahr`/`BarwertRisiko`;
`RisikoModul.AbzugFuerStand`; Ausweis nur bei Pflege: Nachweiszeile, Szenariozeile, Annahmentafel RISIKO, Deklaration 6.5, Anhang-E Punkt 6,
Gliederung Bestandteil RISIKO vor Restwert, Mehrjahrestabelle Spalte RISIKO; Formelmappe Stufe 0 `Zins_Basis`+`Risiko_Zuschlag`→`Zins_i`
bzw. `Risiko_Verlust`/`Risiko_p`/`Risiko_Abzug`, Stufe 1 `=-Risiko_Abzug`), a40c9b3c E15/4 Dialog (Gruppe „Risiko (DIN EN 17463, 6.5)"
zwischen Szenarien und Strom: Klappliste aus/Zinszuschlag/Abzug, drei Zahlenfelder gesperrt je Art, Herleitungszeile; Infoknopf
`Form_WirtschaftlichkeitParameter.btn_Help_Risiko` → `Wirtschaftlichkeit#risiko`, Anker in der Wiki-Quelle; KI-Feldkarte `risiko_art`,
`risiko_zinszuschlag`, `risiko_verlust`, `risiko_wahrscheinlichkeit`; Maskenwache Parameterdialog 30 → 34), a8a82d0c E15/5 Tests
(`RisikoModulTests`: Schema, Werkzeug-Wache, Normierung, aus = bitgleich, Zuschlag = Lauf mit i+1 in allen Szenarien, Abzug ab Jahr 1 nicht
Restwert, Differenzreihe risikobereinigt, trifft Variante nicht Referenz, Verlauf/Gliederung, Speicherweg INSERT 1040/UPDATE 1030, Ausweis,
Formelmappe; fünf bUnit-Fälle). Schema Spalten `Tab_ProjektWirtschaftlichkeit`: `Risiko_Art` TEXT(10) leer/ZINS/ABZUG, `Risiko_Zinszuschlag`
REAL %-Punkte, `Risiko_Verlust` REAL €, `Risiko_Wahrscheinlichkeit` REAL %; Doppelpflicht CREATE + SpalteSicher. 29 Schlüssel neu
(WIRT_RISIKO_*, WIRT_ANN_RISIKO, WIRT_DEKL_RISIKO_*, WIRT_MJ_RISIKO, WIRT_GL_RISIKO, WIRT_AE_6_STAND_RISIKO, WIRT_FM_PARAM_RISIKO_*/ZINS_RISIKO,
WPAR_G_RISIKO, WPAR_RISIKO_*, KI_DLG_WPA_RISIKO_*); Designer nahm ZPG_EINGABE_STOCHASTIK_EINHEITSTAGE mit. SQL-Prüfer 1.805/0.

Abweichungen/Befunde: (1) Norm Anhang F Tabelle F.2 rechnet ded_t = P_t × R_loss[%] × p_loss[%] (R_loss als Prozent des Nettorückflusses);
gebaut wie beauftragt R_loss in € je Periode — Prozentlesart bei EPOS nur auf die Differenzreihe sinnvoll (Nettozahlungen meist negativ).
(2) Wen der Abzug trifft (neu E15‑Q4): gebaut a = jeder Stand außer der Referenz des Laufs, Einzelstand trägt ihn selbst (Abzug auf allen
Ständen kürzte sich in jeder Differenz heraus; Anhang F verlangt ihn bei abweichendem Risiko der Investition); Folge: 1030 ohne Varianten
trägt ihn selbst; Stamm einer Gruppe zeigt Kapitalwert ohne Abzug. (3) PV-Vergütungsdialog und `KostenKomponenteHuelle` bleiben beim Zins
ohne Zuschlag. (4) Ergebniszeile speichert bei Zuschlag den gerechneten Zins i + Δ. (5) Merge-Konflikte erwartet: help_mapping, Wiki
Wirtschaftlichkeit, beide resx, ExcelFormelmappe (E14).

Fragen (gebaut a): E15‑Q1 Risiko gleich in allen Szenarien (b Paare); E15‑Q2 Abzug auf Nettozahlung des Standes als Bestandteil RISIKO
(b nur Erlöse; bei €-Betrag zahlengleich); E15‑Q3 Ausweis Nachweiszeile/Annahmentafel/Parameterblock + Deklaration + Anhang-E Punkt 6
(b eigener Block); **E15‑Q4 neu:** a alle Stände außer Referenz, Einzelstand selbst (gebaut); b alle Stände gleich; c R_loss als Prozent der
Differenzreihe wie Norm F.2.

Erledigt-Gründe: Register V‑G7 gebaut (optional, beide Wege 6.5, Vorgabe aus); Konzept § 2.11.2 V‑G7 „fehlt" → gebaut mit Schemaschritt;
§ 2.11.4 V‑E Risiko erledigt, V‑G3 offen. Logbuchsatz (`wirtschaftlichkeit`): „Im Parameterdialog der Wirtschaftlichkeit kann ein Risiko
nach DIN EN 17463 als Zuschlag auf den Kalkulationszins oder als Abzug je Jahr angesetzt werden." Abnahme A‑E15‑1: (1) Zinszuschlag 1 %-Punkt
→ Kapitalwerte wie i + 1, Nachweiszeile nennt Zuschlag; (2) Abzug 10.000 € × 10 % → Gliederungszeile „Risikoabzug (Anhang F)", Mappe mit
`Risiko_Abzug` und `=-Risiko_Abzug`; (3) aus → alles wie vorher.

# E15 — Risikomodul nach DIN EN 17463: Zinszuschlag oder Zahlungsstromabzug, Schemaschritt 125 (Protokoll, 24.09.2026)

Statuszeile #478 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Auftrag des Anwenders vom
24.09.2026 („V‑G7 Risiko: eigener kleiner Auftrag ausführen"). Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 2.11.2 (Lücke V‑G7), § 2.11.4 (V‑E), § 2.11.5 und § 2.11.6;
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
R‑V (V‑G7), R‑E9a (E9a‑Q6) und R‑E15 (neu); Analysepapier
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
§ 5 und § 6; Mockup `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`, Zonen „Dialog — Parameter", „Was ist
angenommen?", „ValERI-Bewertung — die fünf Blöcke", „Bericht und Ausgabe" und Ressourcentafel der Kategorie 8.
Vorgänger: [`E14_Formelmappe_je_Szenario_Protokoll.md`](E14_Formelmappe_je_Szenario_Protokoll.md). Zweig `e15` von
`105cdb31` (`origin` nach dem Push von #474), Opus 5.5 im Worktree `.claude/worktrees/e15`, zwei Phasen: `d46280f7`
(E15/1), `e111f7ee` (E15/2), `fe795e10` (E15/3), `a40c9b3c` (E15/4), `a8a82d0c` (E15/5) in Phase 1; in Phase 2 die
Zusammenführung `8494f244` mit `origin` = `6d022f6d` (#477 E14 samt Papieren) und `5eaed19c` (E15/6). Erster Merge
`d176b378` („Merge e15: Risikomodul nach DIN EN 17463 - Zinszuschlag oder Zahlungsstromabzug, Schritt 125 (#478)")
über `6d022f6d`, der Baum byte-gleich mit `5eaed19c`; End-Merge `NACHTRAG-478-MERGE2`. Basis
`2026-09-24_R14_Kaelteerzeuger`. **Schemaschritt 125, optional, Vorgabe aus** — ohne Pflege rechnet alles wie vorher,
die Basis bleibt.

## Befund vor der Welle

Die Norm lässt das Risiko einer Maßnahme entweder als Zuschlag auf den Kalkulationszins oder als zusätzliche
Auszahlung je Periode ansetzen (DIN EN 17463, 6.5); Anhang F erläutert den Zahlungsstromabzug `R_loss × p_loss` auf
die Nettozahlung der Perioden t > 0 und bevorzugt ihn. EPOS führte keines von beiden — die Lücke V‑G7 stand im
Konzept (§ 2.11.2) auf „fehlt", die Deklaration nannte „Risikozuschlag nicht angesetzt (6.5 optional)". E9 hat das
Risiko nicht gebaut (E9a‑Q6 a, nur ein Vermerk im Register).

## Gebaut

- **E15/1 — Schemaschritt 125** (`SCHRITT_125_RISIKOMODUL`, Methode `Schritt_Risikomodul`, Spaltenliste
  `SchemaKatalog.RisikomodulSpalten`), reines DDL an `Tab_ProjektWirtschaftlichkeit`, nullbar, ohne Vorgabe:
  `Risiko_Art` TEXT(10) — leer/NULL = kein Risiko, `ZINS` = Zinszuschlag, `ABZUG` = Zahlungsstromabzug —,
  `Risiko_Zinszuschlag` DOUBLE [%-Punkte], `Risiko_Verlust` DOUBLE [€ je Periode, R_loss],
  `Risiko_Wahrscheinlichkeit` DOUBLE [%, p_loss]. Die Zahl 125 steht an vier Stellen aus einer Quelle (Konstante,
  Zielversion, Werkzeugaufruf, Test `Migration_Werkzeug_und_Testdatenbank_ziehen_den_Schritt_aus_einer_Quelle`);
  Doppelpflicht CREATE-Text und `SpalteSicher` in `WirtschaftlichkeitCtrl.StelleTabellenSicher`.
- **E15/2 — Testdatenbank auf 125:** `Werkzeuge/Testdatenbankschema` über die Fassung 124 (LFS `1d971b1a…`): vier
  Spalten angelegt, Marker 125, Größe unverändert 67.792.896 Byte (LFS `6c4c32f9…`); `integrity_check` ok,
  `foreign_key_check` leer; die vier Spalten sind in allen fünf Parameterzeilen leer — kein Referenzprojekt trägt ein
  Risiko.
- **E15/3 — Kern:** `RisikoModul.cs` (mit `Risikoart`) ist die eine Stelle der Regeln. **ZINS:**
  `WirtschaftlichkeitParameter.FuerSzenario` setzt i + Zuschlag in allen drei Szenarien (auch Erwartet, dann als
  Kopie); daraus lesen Kapitalwert, Annuität, Amortisation, Differenzreihe und Zinsfuß, Verlauf, Gliederung und
  Sensitivität. **ABZUG:** `KapitalwertRechner.Rechne` mit dem Parameter `risikoAbzugJahr` mindert die Nettozahlung
  jeder Periode t ≥ 1 um R_loss × p_loss / 100 — nicht im Jahr 0, nicht auf den Restwert; am Zahlungsbild
  `RisikoJeJahr` und `BarwertRisiko`. Wen der Abzug trifft, entscheidet `RisikoModul.AbzugFuerStand`: jeden Stand
  außer der Referenz des Laufs; rechnet die Gruppe nur einen Stand, trägt er ihn selbst (E15‑Q4 a). Ohne Risiko
  eigene Zweige, keine Addition von 0 — bitgleich. **Ausweis nur bei Pflege:** Nachweiszeile (Projekt und je
  Szenario), Szenariozeile, Annahmentafel (Zeile RISIKO), Deklaration 6.5 (`WIRT_DEKL_RISIKO_ANGESETZT*`), Punkt 6
  der Anhang-E-Checkliste, Gliederung des Kapitalwerts (Bestandteil RISIKO „Risikoabzug (Anhang F)" vor dem Restwert),
  Mehrjahrestabelle (Spalte „Risikoabzug"), Brückenbild. **Formelmappe:** Stufe 0 bei ZINS mit `Zins_Basis` und
  `Risiko_Zuschlag`, `Zins_i` als Formel; bei ABZUG `Risiko_Verlust`, `Risiko_p` und `Risiko_Abzug` als Formel;
  Stufe 1 schreibt die Risikospalte als `=-Risiko_Abzug`, Stufe 1 und 2 rechnen mit dem gerechneten Zins.
- **E15/4 — Dialog:** Im Parameterdialog die Gruppe „Risiko (DIN EN 17463, 6.5)" zwischen „Szenarien" und „Strom":
  Klappliste „Art der Risikoberücksichtigung" (aus — kein Risiko angesetzt / Zinszuschlag / Zahlungsstromabzug
  R_loss × p_loss (Anhang F)), die Felder Zinszuschlag [%-Punkte], R_loss [€ je Periode] und p_loss [%] — die Felder
  der nicht gewählten Art gesperrt, nicht ausgeblendet —, eine Herleitungszeile aus `RisikoModul.Herleitung`.
  Infoknopf `Form_WirtschaftlichkeitParameter.btn_Help_Risiko` → `Wirtschaftlichkeit#risiko` (`help_mapping`), der
  Anker steht in der Wiki-Quelle. KI-Feldkarte um `risiko_art` (Wahl), `risiko_zinszuschlag`, `risiko_verlust`,
  `risiko_wahrscheinlichkeit`; Maskenwache des Parameterdialogs 30 → 34 Eingaben.
- **E15/5 — Tests:** `RisikoModulTests` (Kern, Sammlung Testdatenbank): Schema 125 und die eine Quelle, Werkzeug-Wache
  der Repo-Testdatenbank (Stand ≥ 125, Spalten leer), Normierung der Art, Vorgabe aus, p_loss ≤ 100 %, Abzug je Stand
  (Referenz frei, Einzelstand trägt ihn), aus = bitgleich, Zuschlag = Lauf mit i + 1 in allen Szenarien samt
  Differenz, Annuität, Amortisation und Zinsfuß, Abzug nur t ≥ 1 und nicht auf den Restwert, Differenzreihe
  risikobereinigt, Verlauf, Gliederung und Mehrjahrestabelle mit Selbstprüfung, Speicherweg über INSERT (1040) und
  UPDATE (1030), Ausweis nur bei Pflege, Formelmappe (Stufe 0 ohne Risiko unverändert, 0 Abweichungen); fünf
  bUnit-Fälle des Dialogs (Vorgabe aus sperrt, Zuschlag und Abzug geben ihre Felder frei samt Herleitung, Speichern,
  Infoknopf der Gruppe, Assistent setzt Art und R_loss).
- **Phase 2:** Zusammenführung `8494f244` mit `6d022f6d` — Konflikt nur in `ExcelFormelmappe.cs`: die E14-Struktur je
  Szenario hat Vorrang, der Risiko-Block der Stufe 1 steht in jeder Szenariotabelle und schreibt `=-Risiko_Abzug` mit
  dem Namen des Szenarios; `help_mapping`, Wiki-Quelle und beide resx tragen beide Seiten, keine Dublette, de/en
  deckungsgleich. **E15/6** (`5eaed19c`): Die Dialogprobe liest den Schlüssel des Gruppen-Infoknopfs am Baustein.

## Schlüssel

Je Sprache 9.932 → 9.961 Einträge, 29 neu: `WIRT_RISIKO_KURZ_ZINS`, `WIRT_RISIKO_KURZ_ABZUG`, `WIRT_RISIKO_NACHWEIS`,
`WIRT_RISIKO_HERLEITUNG_AUS`, `WIRT_RISIKO_HERLEITUNG_ZINS`, `WIRT_RISIKO_HERLEITUNG_ABZUG`, `WIRT_ANN_RISIKO`,
`WIRT_DEKL_RISIKO_ANGESETZT`, `WIRT_DEKL_RISIKO_ANGESETZT_OHNE_NM`, `WIRT_MJ_RISIKO` („Risikoabzug"), `WIRT_GL_RISIKO`
(„Risikoabzug (Anhang F)"), `WIRT_AE_6_STAND_RISIKO`, `WIRT_FM_PARAM_RISIKO_ZUSCHLAG`, `WIRT_FM_PARAM_ZINS_RISIKO`,
`WIRT_FM_PARAM_RISIKO_VERLUST`, `WIRT_FM_PARAM_RISIKO_P`, `WIRT_FM_PARAM_RISIKO_ABZUG`, `WPAR_G_RISIKO`,
`WPAR_RISIKO_ART`, `WPAR_RISIKO_AUS`, `WPAR_RISIKO_ZINS`, `WPAR_RISIKO_ABZUG`, `WPAR_RISIKO_ZUSCHLAG`,
`WPAR_RISIKO_VERLUST`, `WPAR_RISIKO_P` und die vier Erläuterungen des Assistenten `KI_DLG_WPA_RISIKO_*_ERL`.
Unverändert gilt ohne Pflege `WIRT_DEKL_RISIKO` („Risikozuschlag nicht angesetzt (6.5 optional) …") und
`WIRT_AE_6_STAND`. Designer wiederholbar.

## Fragen aus der Welle

Gebaut ist jeweils Lesart a (die Empfehlung); offen beim Anwender (→ Register R‑E15).

| Frage | Lesarten | Empfehlung |
|---|---|---|
| **E15‑Q1** Risiko je Szenario | (a) ein Risiko, gleich in allen drei Szenarien; (b) Paare je Szenario wie die übrigen Szenariowerte | a |
| **E15‑Q2** Worauf der Abzug wirkt | (a) auf die Nettozahlung des Standes, als eigener Bestandteil RISIKO; (b) nur auf die Erlöse — bei einem €-Betrag zahlengleich | a |
| **E15‑Q3** Ausweis | (a) Nachweiszeile, Annahmentafel und Parameterblock, dazu die Deklaration 6.5 und Punkt 6 der Anhang-E-Checkliste; (b) ein eigener Block „Risiko" | a |
| **E15‑Q4** Wen der Abzug trifft (neu) | (a) jeden Stand außer der Referenz des Laufs, ein Einzelstand trägt ihn selbst; (b) alle Stände gleich — der Abzug kürzt sich dann in jeder Differenz heraus; (c) R_loss als Prozent der Differenzreihe, wie Anhang F Tabelle F.2 | a |

## Abweichungen und Befunde

1. **Normabweichung R_loss in € je Periode:** Anhang F, Tabelle F.2 rechnet ded_t = P_t × R_loss [%] × p_loss [%] —
   R_loss als Prozent des Nettorückflusses. Gebaut ist, wie beauftragt, R_loss als Betrag in € je Periode; die
   Prozentlesart ist bei EPOS nur auf der Differenzreihe sinnvoll, weil die Nettozahlungen eines Standes meist
   negativ sind — sie steht als Lesart c in E15‑Q4.
2. **Der Abzug trifft die Variante, nicht die Referenz** (E15‑Q4 a): Ein Abzug auf allen Ständen kürzte sich in jeder
   Differenz heraus, Anhang F verlangt ihn beim abweichenden Risiko der Investition. Folge: 1030 ohne Varianten trägt
   ihn selbst; der Stamm einer Gruppe zeigt seinen Kapitalwert ohne Abzug.
3. **PV-Vergütungsdialog und `KostenKomponenteHuelle` ohne Zuschlag:** Beide rechnen ihre Vorschau mit dem
   Kalkulationszins ohne Risikozuschlag.
4. **Gespeicherter Zins i + Δ:** Bei ZINS speichert die Ergebniszeile den gerechneten Zins (Kalkulationszins plus
   Zuschlag), nicht den Projektwert.
5. **Datenlücke 1023:** Die Variante 1023 rechnet keinen Kapitalwert — ein Brennstoff ohne Träger (93,5 MWh/a); Datenstand,
   kein Fehler des Moduls.
6. **Designer-Nachtrag fremd:** Der Designer-Lauf nahm wieder den fehlenden Eintrag
   `ZPG_EINGABE_STOCHASTIK_EINHEITSTAGE` des Zapfprofilgenerators mit.

## Nachweis

- **Vorgabe aus → nichts bewegt sich:** `WirtschaftlichkeitAnkerTests` unverändert grün, kein Anker neu gesetzt;
  Referenzlauf 13/13 gegen `2026-09-24_R14_Kaelteerzeuger` PASS, 394/394 CSV byte-gleich, 4.207.049 Werte — die Basis
  bleibt; SQL-Prüfer 1.807 Texte, 0 Fundstellen (Phase 1: 1.805/0).
- **A/B-Tafel** (Phase 2, Beträge in €):

| Fall | Wirkung | Regel |
|---|---|---|
| 1030 einzeln, Zinszuschlag 1 %-Punkt | Kapitalwert Erwartet −31.141.243 → −28.306.379 | gleich dem Lauf mit i + 1 |
| 1030 einzeln, Abzug 10.000 € × 10 % = 1.000 €/a | Kapitalwert Erwartet −14.877, Ungünstig −13.590, Günstig −16.351 | −1.000 × Rentenbarwertfaktor des Szenarios |
| 1019 Stamm (Referenz der Gruppe) | Abzug 0; Zuschlag wie i + 1 | die Referenz trägt keinen Abzug |
| 1024 Variante, Abzug | Kapitalwert −14.877; Kapitalwertdifferenz gegen den Stamm −1.807.372 → −1.822.250 | der Abzug trifft die Variante |
| Rückweg (Art wieder „aus") | exakt die Werte vorher | bitgleich |

  Die Variante 1023 der Gruppe rechnet keinen Kapitalwert (Befund 5).
- **Zellvergleich** der Formelmappe (Messprogramm aus E8b/E14): die 16 Prüfgruppen gleich dem Stand nach E14, dazu drei
  Risikogruppen `hybrz`, `hybra` und `hybrza`; Excel 16 nach voller Neuberechnung abweichend 0 in 19 Mappen; `hybrz`
  +3 Formeln, `hybra` +123 Formeln, dort die Differenz −13.590,33 € und die Annuität −1.000 €.
- **Ressourcen:** je Sprache 9.961, keine Dublette, de/en deckungsgleich; Designer wiederholbar.
- **Phase 1** (Worktree `e15`): Build 0 Fehler, Designer wiederholbar, SQL-Prüfer 1.805/0; kein `dotnet test`.
- **Phase 2** (auf `5eaed19c`, derselbe Baum wie `d176b378`): Builds 0 Fehler; gefiltert Kern 112/112
  (`RisikoModulTests` 25), UI 47/47; voller Lauf **13.001 bestanden / 0 Fehler / 1 übersprungen** (EPOS.Kern.Tests
  6.037, EPOS.UI.Tests 6.001, KiKern.Tests 549, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27 und 1
  übersprungen); Referenzlauf 13/13 gegen R14 (oben); SQL-Prüfer 1.807/0; Designer wiederholbar.
- **Gate auf `d176b378`:** Nachtrag folgt.
- **CI:** steht aus (Push nach dem Gate).

## Abnahme am Gerät (A‑E15‑1, Windows)

1. **Zinszuschlag 1 %-Punkt** im Parameterdialog, Gruppe „Risiko" pflegen, rechnen: Die Kapitalwerte sind die eines
   Laufs mit i + 1 in allen drei Szenarien; die Nachweiszeile nennt den Zuschlag.
2. **Zahlungsstromabzug 10.000 € × 10 %** pflegen, rechnen, Bericht erzeugen: In der Gliederung steht die Zeile
   „Risikoabzug (Anhang F)"; die Formelmappe trägt `Risiko_Abzug` im Parameterblock und `=-Risiko_Abzug` in der
   Risikospalte.
3. **Art wieder „aus"**, rechnen: alles wie vorher.

## Logbuch

Im Update-Papier (Version 1.2.0.4, Sammel-Upload 26.09.2026), Stichwort `wirtschaftlichkeit`: „Im Parameterdialog der
Wirtschaftlichkeit kann ein Risiko nach DIN EN 17463 als Zuschlag auf den Kalkulationszins oder als Abzug je Jahr
angesetzt werden."

## Papiere mit der Statuszeile

Register (Kopf, Familientafel mit R‑V und der neuen Familie R‑E15, R‑V mit der Zeile V‑G7 „gebaut #478 (Schritt
125)" und dem Vermerk, R‑E9a mit E9a‑Q6, R‑E15 mit E15‑Q1…Q4, EZ‑9, EZ‑10), Konzept (Kopf und Schrittabsatz mit
Schritt 125, § 2.11.2 V‑G7, § 2.11.4 V‑E und Fußnote, § 2.11.5 Ausweis im Bericht, § 2.11.6 Stufen 0 und 1, § 6.1,
§ 6.2, § 7 und Anhang), Analysepapier (Kopf, Nachtrag #478, § 5 Zeile E15, Stand der Etappen, § 6 Schritt 125),
Protokoll der Entscheidwege (Kopf, Kopf von § 8, § 8.29, § 8.30), `Referenzlaeufe/LIESMICH.md` (Nachtrag Schemastand
125), Mockup (Zone „Dialog — Parameter" mit der Gruppe Risiko, Annahmentafel, Deklaration 6.5, Punkt 6 der
Checkliste, Ressourcentafel der Kategorie 8, Stand-Absatz), Update-Papier und die Wiki-Quelle Wirtschaftlichkeit
(Abschnitt `risiko`, Deklaration), Index (Reporting 129 → 130).

## Offen

- **Fragen E15‑Q1…Q4** beim Anwender (gebaut jeweils a).
- **Abnahme am Gerät** A‑E15‑1 (drei Schritte oben).
- **Gate auf `d176b378`**, der End-Merge und **CI** (Nachtrag).
- Der **Wiki-Sammel-Upload** am 26.09.2026 (Version 1.2.0.4, freigegeben).

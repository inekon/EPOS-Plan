# E22 — Bericht (Opus, 25.09.2026 ca. 10:20, Worktree e22 ab cbed6dba, e22 = 548d5983; kein Schema, keine Daten, keine resx)

Commits: 0e1c0938 E22/1 fünf Stellen auf `ORDER BY " + Ladeordnung.SqlAnlagenprio(null) + ", ID` (SimulationControl WP_Liste_Laden,
QuellbezuegeAufbauen, SenkenPufferDerAnlagen; WaermesenkeClass SenkenLaden, SenkenlistenLaden), HB1-O1-Kommentare entfernt,
`Ladeordnung.SqlAnlagenprio` Absatz „gilt für Anzeige und Rechenweg"; 53394b73 E22/2 drei Modul-Lader `SPK_Liste_Laden`, `Solar_Liste_Laden`,
`BHKW_Liste_Laden` gleich sortiert (Hinweis „Ohne ORDER BY" beim BHKW ersetzt); Punkt 3 Determinismus ohne Datei (kein Commit, E22/3 nicht
vergeben); 0dd97e05 E22/4 Neueinfrierung R15, R14 ins Archiv, Basisname ersetzt; 548d5983 E22/5 `EPOS.Kern.Tests/AnlagenprioRechenwegTests.cs`
(4 Tests). Testdatenbank unverändert (141, a427aa72).
A/B (13 Projekte, EPOS.Referenzlauf): Punkt 1 allein gegen R14: 12/13 PASS byte-gleich, 1042 FAIL genau 10 Werte in aggregate.csv (übrige 35
Dateien byte-gleich): `WaermepumpeModul[0/1]` .Modul/.Leistung/.Waermeproduktion/.Stromverbrauch/.Betriebsstunden tauschen Plätze — neu Platz 0
Anlage 14817 CS6800iAW Prio 1 (11 kW, 71,45 MWh/a Wärme, 26,29 MWh/a Strom, 5.995,29 h), Platz 1 Anlage 14818 CS7800iLW 16 ohne Prio (15 kW,
20,84/7,06 MWh/a, 2.073,4 h); Werte gleich, Zeitreihen byte-gleich. Punkt 1+2 gegen Punkt 1: 394/394 byte-gleich → **E22‑Q1 = a, drei Lader
mitgenommen** (kein Referenzprojekt mit zwei Kesseln/Kollektorfeldern; 1030-BHKWs 14920 Prio 1/14921 ohne stehen nach Regel wie nach Zeilenfolge).
Determinismus: zwei Läufe des Endstands 13/13 byte-gleich, Vergleich GESAMT PASS 4.207.049 Werte; Einfrierlauf byte-gleich.
Neueinfrierung R15: Ordner `Referenzlaeufe/2026-09-25_R15_Anlagenprio` (394 CSV + protokoll.txt = 395 Dateien, 55.550.109 Byte, 2.249 Skalare;
Git: 393 CSV R100, 1042/aggregate.csv R094); R14 protokoll.txt per `git mv` nach `Dokumentation/ueberholt/Referenzbasen/2026-09-24_R14_Kaelteerzeuger/`,
Rest `git rm -r`; Archiv-LIESMICH: 32 Basen/33 Dateien, Tabellenzeile R14, Abschnitt „Die Basis R14 im Einzelnen", Links; Basenhistorie-Link
umgestellt; `Referenzlaeufe/LIESMICH.md`: Abschnitt „Aktuelle Basis" R15 (Anlass, Modultafel R14/R15, A/B, Begründung, Determinismus,
Einfrierbefehl), „Entfernte Basen" (32, acht Protokolle, Behalteliste), Vorgängernotiz R14, Weg A. **Abweichung vom R13-Muster:** die Nachträge
119–141 bleiben in der LIESMICH mit Einleitungssatz (R15 steht auf genau dieser Testdatenbank; „die Basis bleibt" meint darin R14); nur der
R14-Kopf ins Archiv — hält den Einfügeort für den #505/142-Nachtrag konfliktfrei (**vom Orchestrator bestätigt**). Basisname ersetzt in
`CLAUDE.md:145`, `kern.yml` (6), `ios.yml` (2), `Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md:1909`, `Systementwurf_Gebaeudesimulation_EPOS-Plan.md:179`,
`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` Z. 3/2852/2992 (byte-erhaltend, CRLF); bewusst nicht (Geschichte, Vorbild c57a3b4a):
Konzept_Kuehlung Z. 107/142/2780/2884, Status_Gebaeudesimulation_VDI6007.md, Statuszeilen der Statusdatei, Umsetzungskonzept_Zapfprofilgenerator
Z. 2688/2793.
Tests: `AnlagenprioRechenwegTests` (Quelltext-Wache acht Leser tragen die Regel und kein `ORDER BY Prioritaet`; Gegenprobe; Fakt 1042 14817 vor
14818 in SenkenLaden/SenkenlistenLaden; synthetisch Prio 0 = ungepflegt, Kessel 14854 Prio 1 vor beiden WPs); voller Lauf Kern 6.826+1, UI 6.243,
KiKern 549, SpeicherEngine 386, SpeicherPlanung 27+1, 0 Fehler (GebaeudeRueckwegTests, DokumentationLinkWacheTests grün); SqlDialektPruefer 0;
Referenzlauf Endstand gegen R15 13/13 PASS, 394/394 byte-gleich. Messdaten scratchpad/e22/ (A_p1, B_p12, D1, D2, End, gate.log).
Papiervorschläge: § 6.3 Nr. 18 „erledigt 25.09.2026 (E22): Rechenweg, Hydraulikbild und Erzeugerkarten folgen derselben Regel
`Ladeordnung.SqlAnlagenprio` (gepflegte Priorität zuerst, ungepflegte hinten, Regel ‚99'); acht Rechenweg-Leser umgestellt, Wache
`AnlagenprioRechenwegTests`; neue Basis R15; einzige Wirkung Modulreihenfolge der Wärmepumpen in 1042." Logbuchsatz (Wiki Anlagen/Hydraulik):
„Die Nummerierung der Module im Ergebnis folgt der Anlagenpriorität; eine Anlage ohne Priorität steht hinter den Anlagen mit Priorität — wie
im Hydraulikbild." Abnahme A‑E22‑1 (1042): Modul 1 = WP 14817 Prio 1 (CS6800iAW 11 kW) in Ergebnis, Bericht und Hydraulikbild (Sichtprüfung
beim Anwender; belegt in aggregate.csv von R15). Im Nachtrag zu Schritt 142 (#505) meint „die Basis bleibt" dann R15.

# E22 — Nachtrag Umstellung auf R16 (Opus, 25.09.2026 ca. 12:15, e22 = c6ee0961 auf origin f83ce27d)

Lage: AK1 W5 (Cloud, bcd61fe2) fror `2026-09-25_R15_Anlagenkopplung` ein (14 Projekte, neu 1047, Einfrierregel „gesäte Auslegungsdaten der
Übergabe", R14 entfernt). Commits: 8cb5c69b Merge origin f83ce27d (R15_Anlagenkopplung übernommen; 1.182 rename/rename-Konflikte der R14-CSV →
origin, eigener Ordner R15_Anlagenprio `git rm -r`; Inhaltskonflikte in 9 Dateien [Referenzlaeufe/LIESMICH, CLAUDE.md, kern.yml, ios.yml,
Konzept_Gebaeudesimulation, Systementwurf_Gebaeudesimulation, Konzept_Wirtschaftlichkeit, Archiv-LIESMICH, Basenhistorie] → origin-Fassung;
R14-Protokoll ohne Dublette); c6ee0961 E22/6 Neueinfrierung **`Referenzlaeufe/2026-09-25_R16_Anlagenprio`** (14 Projekte; 432 CSV +
protokoll.txt = 433 Dateien, 60.332.709 Byte, 2.447 Skalare; Git 431 CSV R100, 1042/aggregate.csv R094). E22/4 (0dd97e05, R15_Anlagenprio)
inhaltlich ersetzt. Gegen origin nur E22/1, E22/2, E22/5, E22/6.
A/B gegen R15_Anlagenkopplung (Testdatenbank 142, 1360e2be): 13/14 PASS, 431/432 byte-gleich; einzige Abweichung 1042 aggregate.csv 10 Werte
(WaermepumpeModul[0/1] Index; 14817 CS6800iAW Prio 1 an Platz 0); 1047 PASS byte-gleich (38 Dateien, 403.158 Werte); Determinismus zwei Läufe
14/14 byte-gleich (432/432), GESAMT PASS 4.610.207 Werte; Einfrierlauf byte-gleich. Frühere A/B (Teil 1 gegen R14: 10 Werte 1042; Teil 1+2 gegen
Teil 1: 394/394) steht als Tafel in der LIESMICH.
R15_Anlagenkopplung: protokoll.txt per `git mv` nach `Dokumentation/ueberholt/Referenzbasen/2026-09-25_R15_Anlagenkopplung/`, Rest `git rm -r`;
Archiv-LIESMICH 33 Basen/34 Dateien, Tabellenzeile R15, Abschnitt „Die Basis R15 im Einzelnen" (ganzer bisheriger Abschnitt inkl. Nachtrag #505
und R14-Notiz — Muster von origin bei R14, Nachträge mitgewandert); Referenzlaeufe/LIESMICH: „Aktuelle Basis" R16 (Anlass, Modultafel R15/R16,
A/B drei Zeilen, Begründung, Determinismus, Einfrierbefehl, Vorgängernotiz R15), „Entfernte Basen" 33/neun Protokolle, Behalteliste, Weg A;
kern.yml behält die sechs CI-Projekte von origin inkl. 1047. Namensersetzung R15_Anlagenkopplung → R16_Anlagenprio: CLAUDE.md:146, kern.yml (7),
ios.yml (2), Konzept_Gebaeudesimulation_VDI6007:1909, Systementwurf_Gebaeudesimulation:179, Konzept_Wirtschaftlichkeit Kopf Z. 3, Tabelle Z. 2866,
Nr. 21 (origin hatte dort noch R14), Basenhistorie:7, Archiv-Wegweiser Z. 19; Geschichte unverändert (Konzept_Anlagenkopplung Z. 58/2123,
Status_Gebaeudesimulation Z. 49/138, AK1-Welle5-Protokoll).
Tests: voll Kern 7.090+1, UI 6.243, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27+1, 0 Fehler (AnlagenprioRechenwegTests, GebaeudeRueckwegTests
grün mit R16); SqlDialektPruefer 0; Referenzlauf 14/14 gegen R16 PASS 4.610.207 Werte, 432/432 byte-gleich; Testhost-Regel eingehalten.
**Papiere #503 (R16 statt R15):** Statusdatei Z. ~427/606 (#506/E21 „nächste Neueinfrierung nach R15" → R16), Z. 610 (#498/Mess18 „eigene Basis
R15" = Geschichte, bleibt); Konzept_Wirtschaftlichkeit ~3014 „Neueinfrierung nach R15" → R16; Register Z. ~744/747 (E21‑Q3/Q6) prüfen → R16;
Geschichte bleibt (Status_Gebaeudesimulation Z. 138/141, Konzept_Anlagenkopplung Z. 67, Konzept_Gebaeudesimulation Z. 3671); „R15" in Befund_V,
Gegenlesen*, AR15 meint anderes. Abnahme A‑E22‑1 belegt in aggregate.csv von R16. Laufdaten scratchpad/e22/ (R16a, R16b, End16, gate2.log).

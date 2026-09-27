# E27 — Bericht Phase 0 (Opus, 25.09.2026 ca. 21:00, Worktree e27 = ba798d8a, kein Commit; Nachher-Werte rechnerisch aus R18-Reihen, Vorher-Lauf auf Kopie)

Einzige Quelle des negativen Rests: ungeklemmter BHKW-Abzug `SimulationControl.cs:903-904` (Stundenschleife) und `:942-943` (Vektorstufe); alle
anderen Stufen addieren (WP/Heizstab :861/863, Kessel :891/919, Kälte Kaelte.cs:300). Den negativen Rest brauchen: spätere Verbraucher derselben
Viertelstunde (WP, Heizstab, E-Kessel, Kälte hinter dem BHKW — physikalisch richtig → Klemme an der BHKW-Stufe wäre falsch), PV (:578-581,
`BhkwUeberschuss` V1 :583-593, danach SubVectors korrigiert ≥ 0), Einzelspeicher (:618-624, klemmt; Last aus StromspeicherSimCtrl.cs:526/1198 selbst),
Flotte (Stromspeicher.cs:121-135 ersetzt Rest durch NetzbezugKw ≥ 0). Negativ bleibt der Rest genau bei BHKW ohne PV und ohne gerechneten Speicher
(Tool_6 ohne Anlage = null, klemmt nicht): 1018, 1030; außerhalb 1031 (gespeichert −14,72). **Klemme (Q1 a):** am Laufende `:630-637` nach PV/Speicher
vor `ReststromMwh`, Bedingung `!SpeicherflotteErsetztReststrom`, nur Werte < 0 → 0 in neuem Array (Rest kann auf simulation_Strombedarf zeigen, :521);
Werte ≥ 0 bitgleich; Probe 1018 + PV von 1040 auf Kopie: Rest 0, BhkwUeberschuss 27.457,5 kWh, Einspeisung PV 6,60 MWh → Klemme wirkungslos, Pfad
byte-gleich. Kein eigener Überschussvektor: KWK-Split `StromMatrix.cs:227-238` liefert Einspeisung stundengenau (1018 KwkEinsp 27,4575 = Summe der
negativen Viertelstunden; 1030 0,392). **Wirkung:** 12/14 unverändert (1024 Anker bleiben −2.772.642,2674731 / −2.801.567,7561814 / −2.745.356,0376827).
1018: negative Viertelstunden 14.004 → 0, Stromrestbedarf −27,4575 → 0, Strommatrix-Netzbezug −27,4575 → 0, KWK-Split 0/27,4575 gleich; Rollentarif-
Probe Reststromkosten −8.237,25 → 0, vermiedene Menge 27,46 → 0; Kapitalwert in der Testdatenbank keiner (kein Erdgas-Arbeitspreis) → Doppelgutschrift
dort hypothetisch; CO₂ gesamt 13,06 t/a steigt (Gutschrift über negativen Netzbezug `KostenEmissionRechner.cs:735` entfällt). 1030: 48 negative
Viertelstunden (12 h) → 0, Stromrestbedarf 4.357,7808 → 4.358,1728 MWh, Netzbezug gleich, KWK-Split 431,913/0,392 gleich, KWKG Jahr 1 7.322,63 gleich,
Bezugsspitze 2.011 kW gleich; Flat 0,25 €/kWh Stromkosten Netz 1.091.845 → ~1.091.942,50 €/a; Kapitalwert Erwartet −31.141.242,7087 / Best
−31.309.741,5799 / Worst −31.005.297,5828 → je ~−1.700…−1.800 € (rel. 5,6e-5, unter Toleranz, Anker mit 6 Nachkommastellen rot). 1017/1047 kein
Kapitalwert (Brennstoff ohne Träger). Wandernde Anker: PvAusweisStromMatrixTests.cs:239 (1030), KwkgErsatzwegGewichtetTests ~:82 (−21.895.377,339395),
Co2StromtraegerRueckfallTests.cs:70 (4357,78), EnergiekostenGrundTests.cs:356 (4357,78), ProjektkostenArtenTests.cs:93 (1.089.445,00 → 1.089.542,50).
**Ausweis** negativer Netzbezug bisher: Übersicht Legende (SimulationErgebnisHuelle.Bilder.cs:343, Anzeige.cs:398), BHKW-Reiter Reststrombedarf
(BhkwReiter.razor:93 ← SimulationErgebnisCtrl.cs:743), Kennzahl energie.netzbezug/Autarkie (KennzahlenKatalog.cs:380-381/455), Energiekosten/CO₂
(KostenEmissionRechner.cs:536), EndenergieAufloeser.cs:956, Monatsdiagramm (ChartRenderer.cs:465-466), Excel Monatsspalte (:1853), Strommatrix-Tabelle
(BausteineWirtschaftlichkeit.cs:964-967, Excel :1002-1003, gespeichert WirtschaftlichkeitCtrl.cs:8482); „BHKW-Einspeisung" als Größe nur in der
Wirtschaftlichkeit (KwkEinspeisungGesamtMWh), Ergebnisansicht nur mit PV/Flotte (ZeitreihenExtraktor.cs:84-86/103). **R19:** 4/432 CSV wandern
(1018 aggregate: Sim.Reststrom −27,4575103 → 0, Energiebedarf.Stromrestbedarf −27,46 → 0, BHKW.Reststrombedarf −27,46 → 0 [Q4 a],
Vektor.reststrom_viertelstunde.Summe −109.830,041 → 0; 1018 reststrom_viertelstunde.csv 14.004 Werte → 0; 1030 aggregate Sim.Reststrom 4.357,78079 →
4.358,17279, Stromrestbedarf 4.357,78 → 4.358,17, BHKW.Reststrombedarf gleichlautend, Vektor-Summe 17.431.123,2 → 17.432.691,2; 1030
reststrom_viertelstunde.csv 48 Werte → 0); 1030 in der CI-Liste → Neueinfrierung nötig, Ordner `2026-09-25_R19_BhkwNetzbezug`, ~1,5 h.
**Fragen (Entscheid Orchestrator 25.09.2026 ~21:15 nach Empfehlung; Anwender „nach Empfehlung bauen"):** Q3 a (keine neue Ausweisgröße; b Diagnosereihe +
BHKW-Reiter-Zeile als Restpunkt); Q4 a (BHKW.Reststrombedarf an SimulationRunner.cs:607 und SimulationErgebnisCtrl.cs:743 stündlich klemmen Σ max(0,
Bedarf_h − Strom_h)); Q5 a (Klemme am Laufende, nicht Flotte); Q6 a (gespeicherte Altergebnisse 1018/1031 bleiben, heilen beim nächsten Lauf); Q7 N7
nur melden (Vorab-Überschuss für PV-Modus der WP :4306-4317 zählt BHKW-Überschuss als PV-Überschuss; Kessel-Vektorstufe nach BHKW mit negativem
Stufeneingang :913-915); Q8 CO₂-Anstieg 1018 folgerichtig, messen. Aufwand ~5 h + Gate. Hinweis: Klassifizierer lehnte in Phase 0 einen temporären
Kern-Eingriff ab („Modify Shared Resources"); Nachher-Werte gerechnet. Bau freigegeben.

# E27 — Bericht Phase 1 (Opus, 25.09.2026 ca. 21:45, e27 = a96500eb über ba798d8a; Testdatenbank unverändert 19a7b632 [e27 kennt 1048 noch nicht])

Commits: bba74bca E27/1 Klemme `SimulationControl.cs:633-641` (Aufruf vor ReststromMwh) + neue Methode `NetzbezugGeklemmt` (~:4400; nur Werte < 0 → 0,
nicht bei Speicherflotte; ohne negativen Wert dasselbe Array → bitgleich) + `BhkwReststrombedarfMwh` (Q4 a, je Stunde geklemmt; Aufrufe
`SimulationRunner.cs:607-609`, `SimulationErgebnisCtrl.cs:743-745`); 8aa53a99 E27/1 Nachschliff (ohne Überschussstunde Jahresdifferenz wie vorher →
1017/1024/1047 bitgleich); 13ae6216 E27/3 Tests: neu `BhkwNetzbezugKlemmeTests.cs` 7 Fälle, `PvAusweisStromMatrixTests.cs:237-247` Anker 1030
Erwartet/Best/Worst neu („E27"); a96500eb E27/4 Einfrierung R19. E27/2 Ausweis: nichts nötig (alle Anzeigen lesen ReststromMwh/NETZBEZUG).
Zahlen: 1018 Netzbezug −27,4575 → 0, BHKW.Reststrombedarf −27,46 → 0, negative Viertelstunden 14.004 → 0, KWK-Split 0/27,4575 gleich, Rollentarif 0,30
Reststromkosten −8.237,25 → 0, vermiedene Menge 27,46 → 0, CO₂ gesamt 13,0557 → 25,0008 t/a (+11,9451 = 27,46 MWh × 435 g/kWh, Q8), kein Kapitalwert
(kein Erdgas-Arbeitspreis); 1030 Netzbezug 4.357,7808 → 4.358,1728, KWK-Split 431,913/0,392 gleich, KWKG Jahr 1 7.322,63 gleich, Bezugsspitze 2.011 kW
gleich, Energiekosten Flat 1.624.616,20 → 1.624.713,70 €/a (+97,50), Stromkosten Netz 1.091.845 → 1.091.942,50, CO₂ 4.035,0704 → 4.035,2888 t/a,
Kapitalwert Erwartet −31.141.242,7087 → −31.142.971,0615, Best −31.309.741,5799 → −31.311.485,3390, Worst −31.005.297,5828 → −31.007.010,7983; übrige
zwölf unverändert (1024 −2.772.642,27 / −2.801.567,76 / −2.745.356,04); PV nach BHKW (Kopie 1018 + PV 1040): Rest/Netzbezug-Hashes gleich,
BhkwUeberschuss 27.457,51 kWh, nur BHKW.Reststrombedarf −27,46 → 0. Tests: Voll-Lauf zweimal 14.803 grün / 2 übersprungen / 0 rot (Kern 7.446/7.447,
UI 6.395, KiKern 549, SE 386, SP 27/28); nur 1030-Anker in PvAusweisStromMatrixTests rot → neu; KwkgErsatzwegGewichtet, Co2StromtraegerRueckfall,
EnergiekostenGrund, ProjektkostenArten lesen gespeicherte Ergebnisse → grün; SQL 1.927/0; ChartProben 174/0. A/B R18→R19: 12/14 PASS byte-gleich,
428/432 CSV; 1018 aggregate Sim.Reststrom −27,4575103 → 0, Energiebedarf.Stromrestbedarf −27,46 → 0, BHKW.Reststrombedarf −27,46 → 0,
Vektor.reststrom_viertelstunde.Summe −109.830,041 → 0, reststrom_viertelstunde.csv 14.004 Werte < 0 → 0; 1030 Sim.Reststrom 4.357,78079 → 4.358,17279,
Stromrestbedarf 4.357,78 → 4.358,17, BHKW.Reststrombedarf gleichlautend, Vektorsumme 17.431.123,2 → 17.432.691,2, reststrom_viertelstunde.csv 48 Werte
< 0 → 0 (Toleranz meldet 1018 14.008, 1030 48 Abweichungen); Determinismus 2 Läufe 14/14 byte-gleich, GESAMT PASS 4.610.207; Einfrierlauf byte-gleich,
Kontrolllauf gegen R19 PASS. Einfrierung: neu `Referenzlaeufe/2026-09-25_R19_BhkwNetzbezug/` (432 CSV + protokoll.txt, 2.447 Skalare); R18-Protokoll git
mv nach `Dokumentation/ueberholt/Referenzbasen/2026-09-25_R18_PvAusweis/`, Rest git rm; Archiv-LIESMICH 36 Basen/37 Dateien, Verweis Z. 19, Zeile R18,
Abschnitt „Die Basis R18 im Einzelnen"; Referenzlaeufe/LIESMICH: Aktuelle Basis R19 (Anlass, A/B, Determinismus, Einfrierbefehl), Nachtrag 144 +
Zusammenführung E24 übernommen, Vorgängernotiz R18, Entfernte Basen 36/zwölf Protokolle, Weg A. Basisname R18 → R19 (20 Stellen): CLAUDE.md:146,
kern.yml:264, 268-273, ios.yml:224/228, Dokumentation/LIESMICH.md:251, Konzept_Gebaeudesimulation:1936, Systementwurf:179, Konzept_Wirtschaftlichkeit
:3/:2900/:3035, Analysepapier :8/:581 („seit E27 gilt"), Basenhistorie:7; Geschichte unverändert. Konzeptvermerk (§ 3.6 / § 6.3): „Der Netzbezug ist nie
negativ: Ein BHKW-Überschuss, den keine spätere Stufe (Verbraucher derselben Viertelstunde, Photovoltaik, Stromspeicher) aufnimmt, steht allein im
KWK-Split als Einspeisung; der Reststrom wird am Laufende bei 0 geklemmt, der Reststrombedarf der BHKW-Zeile je Stunde (E27, Entscheide E27‑Q1/Q4)."
Logbuch-Vorschlag: „Ein Stromüberschuss des BHKW mindert den Netzbezug nicht mehr, sondern wird ausschließlich als Einspeisung ausgewiesen." Abnahme
A‑E27‑1: (1) 1018 rechnen: Ergebnisansicht, BHKW-Reiter, Übersicht Netzbezug/Reststrombedarf 0 (nicht −27,46); (2) 1018 mit Strompreis im Rollentarif:
Reststromkosten 0, KWK-Einspeisung 27,46 MWh in der Strommatrix-Tabelle; (3) 1030 Netzbezug 4.358,17 MWh, Kapitalwert Erwartet ≈ −31.142.971 €; (4)
Kern-Lauf gegen R19 grün. Restpunkte: Q3 b eigene Ausweisgröße „BHKW-Einspeisung" (Ressourcen, Mockup, Papier); N7 (Vorab-Überschuss PV-Modus WP
`SimulationControl.cs:4330ff` liest negativen Rest nach BHKW als PV-Überschuss; Kessel-Vektorstufe hinter BHKW negativer Stromeingang :913-915); Q6
Altergebnisse 1018 −14,47 / 1031 −14,72 heilen beim nächsten Lauf. Kein Schemaschritt; Klassifizierer lehnte im Bau nichts ab; Testhost-Regel eingehalten.

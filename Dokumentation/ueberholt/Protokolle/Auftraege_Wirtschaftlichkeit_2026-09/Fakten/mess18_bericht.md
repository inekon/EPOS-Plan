# Messwelle § 6.3 Nr. 18 (HB1-O1, Engine-Sortierung) — Bericht (Opus, 25.09.2026 ca. 08:35, Worktree mess18 ab 6cfdebf0, Commit 6de80d66 „nicht mergen")

Ergebnis: Der Probeumbau der fünf Rechenweg-Sortierungen auf die 99er-Regel (`ORDER BY Ladeordnung.SqlAnlagenprio(null), ID`;
`SimulationControl.cs` WP_Liste_Laden/QuellbezuegeAufbauen/SenkenPufferDerAnlagen, `WaermesenkeClass.cs` SenkenLaden/SenkenlistenLaden) ändert
physikalisch in keinem der 13 Referenzprojekte etwas. Kontrolle Ist: 13/13 PASS, 394 CSV byte-gleich. Probe: 12 Projekte PASS byte-gleich;
nur **1042 FAIL mit 10 Werten in `aggregate.csv`** — die Schlüssel `WaermepumpeModul[0/1].Modul/.Leistung/.Waermeproduktion/.Stromverbrauch/
.Betriebsstunden` tauschen die Plätze (14817 CS6800iAW Prio 1, 11 kW, 71,45 MWh aus 26,29 MWh Strom in 5.995 h; 14818 CS7800iLW ohne Prio,
15 kW, 20,84 MWh aus 7,06 MWh in 2.073 h); Werte gleich, nur Index. Alle Zeitreihen byte-gleich. Reihenfolge ändert sich in 1030 (XRGI 9 ohne
Prio hinter BHKW EW M 50 Prio 1 und Kessel Prio 3), 1040/1041/1045 (Kessel ohne Prio hinter WP Prio 1), 1042 (WP Prio 1 vor WP ohne Prio).
Kennzahlen (fünf frische Läufe je Stand, Kette LadeParameter → KostenEmissionRechner → WirtschaftlichkeitCtrl.Berechne Erwartet) vorher =
nachher: 1030 BHKW 11,99 %/Kessel 88,01 %, Gas 1.241,55 + 5.403,10 MWh, CO₂ 4.035,07 t/a, Energiekosten 1.624.616,20 €/a, Kapitalwert
−31.141.295,95 € (frischer Lauf; Anker −21,9 Mio. € stützt sich auf gebuchten Stand); 1040 WP 75,82 %/Kessel 24,18 %, 19,43/16,19 MWh, 13,89 t;
1041 WP 17,02 %/Kessel 82,98 %, 10,25/149,91 MWh, 43,92 t; 1042 WP 72,92 %/Kessel 17,87 %/Solar 0 %, 33,34/19,32 MWh, 22,62 t; 1045 WP
73,41 %/Kessel 26,59 %, 23,50/22,28 MWh, 17,86 t (1040/1041/1042/1045 ohne Kapitalwert: kein Stromträger). Voller Testlauf mit Probeumbau:
Kern 6.815+1, UI 6.237, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27+1 — **kein Test rot, kein Anker**; GebaeudeRueckwegTests grün (1040
PASS); die CI (1030, 1007, 1017, 1045, 1046) bemerkte den Umbau nicht, nur der lokale 13er-Lauf (1042).

Warum ohne Wirkung: 1040/1041/1045 — Deckungsreihenfolge legt die Kaskade über den Typ fest (Tool_1..4 = WP vor Kessel), Anlagen finden
Senken über Anlagen-ID und Puffer über `Z_AnlageSenke.Ladeprio`; 1030 — BHKW-Module lädt `BHKW_Liste_Laden` (nicht unter den fünf Stellen,
ohne ORDER BY, Zeilenreihenfolge, 14920 Prio 1 zufällig vorn), die fünf Stellen ordnen Senken-/Pufferlisten, Ladeordnung Puffer 1054170
unverändert; 1042 — WPs haben getrennte Senken (14817 Heizkreis + Puffer 1054196; 14818 nur Brauchwasserpuffer 1054202, Quellwärme aus
1054196), Rechenfolge je Modul über `ModulEbenen`, nur Index für Anzeige/Bericht. Gepflegte Priorität wird in der Reihenfolge überall
wirksam (Prio 1 vorn), rechnerisch nirgends; Wirkung erst bei zwei Anlagen gleichen Typs auf derselben Rechenebene und Senke mit von der
ID abweichender Priorität — kein Referenzprojekt hat diese Lage. **Nebenbefund:** `SPK_Liste_Laden`, `Solar_Liste_Laden`, `BHKW_Liste_Laden`
(SimulationControl ab Z. ~1561) laden ohne ORDER BY — `Prioritaet` gilt für Kessel/Solar/BHKW-Module gar nicht; „gepflegt zuerst" überall
bräuchte diese drei Stellen dazu (eigene Messung).

Aufwand echter Umbau + R15: Code 5 Zeilen + fünf HB1-O1-Kommentare weg + Hinweis in `Ladeordnung.SqlAnlagenprio` „nur Anzeige-Leser"
streichen (~15 min); Gate ~6 min; zwei Referenzläufe 13 Projekte (Determinismus) + Vergleich; Neueinfrierung R15 nach LIESMICH-Regel (neuer
Basisordner 394 CSV, Abschnitt Aktuelle Basis mit Anlass/A/B-Tafel/Einfrierbefehl, R14-Protokoll nach ueberholt/Referenzbasen, R14 löschen,
Listen nachführen, Basisname in kern.yml, ios.yml, CLAUDE.md und sieben Papieren, Statuszeile, Protokoll) — zusammen 1,5–2 h, ohne Schema und
Daten. Empfehlung des Agenten: Umbau ist fachlich richtig und risikolos; eigene R15 lohnt für eine Modulnummer kaum → mit der nächsten ohnehin
fälligen Neueinfrierung bündeln und dann auch über die drei unsortierten Modul-Lader entscheiden; Nr. 18 bis dahin offen mit Vermerk
„gemessen 25.09.2026, ohne Rechenwirkung, nur Modulreihenfolge in 1042". Messdaten: scratchpad/mess18/ (ist, probe, kz_ist, kz_probe, Logs).
Aufgeräumt: Mess-Testklasse gelöscht (nie committet), Arbeitskopie weg, -wal/-shm entfernt.

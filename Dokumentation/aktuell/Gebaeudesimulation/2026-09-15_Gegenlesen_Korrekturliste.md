# Gegenlesen des Konzepts — zusammengeführte Korrekturliste (15.09.2026)

**Protokoll.** Ergebnis des Workflows ‚konzept-gegenlesen‘ (vier Gegenleser, Zusammenführung; Modell Opus); alle Befunde sind in Rev. 1 des Konzepts eingearbeitet im Auftrag des Konzepts [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Sitzung vom 15.09.2026. Wortlaut wie geliefert; Zeilenbelege und Zahlen sind im Konzept nach dem Gegenlesen korrigiert, bei Widerspruch gilt das Konzept. Pfade unter `C:\Users\…pos-spike\` bezeichnen die Prüfwerkzeuge außerhalb des Repositoriums.

---

## Zusammengeführte Korrekturliste — `Dokumentation/aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`

Quellen der Einzelbefunde: **[P]** Physik/Norm, **[R]** Repositorium/Hausregeln, **[I]** IFC/Lizenzen, **[L]** Gesamtleser. Widersprüche zwischen den Gegenlesern sind im Befundtext als „**Klärung:**" gekennzeichnet und im Repositorium bzw. in `C:\Users\Dirk\AppData\Local\Temp\epos-spike` nachgerechnet.

---

### Schwere: hoch

**1. `Wohngebaeude_Nicht_Wohngebaeude` wird im Kern sehr wohl gelesen** [R2, L5]
(a) 2.1: „das Feld `Wohngebaeude_Nicht_Wohngebaeude` wird im Kern nirgends gelesen."
(b) Die Spalte ist der Standardfilter der Kataloglese und trennt Wohn-/Nichtwohnbau — nur der *Rechenweg* wertet sie nicht aus; in der Testdatenbank hängt genau ein Gebäude daran (10632 Hotel, `WNW=Nicht Wohngebaeude`).
(c) `EPOS.Kern/Controller/GebaeudeCtrl.cs:23` (`ReadAll(string szFilter = "Wohngebaeude_Nicht_Wohngebaeude='Wohngebaeude'")`), `:108`; `GebaeudeStammCtrl.cs:83-84`; `ProjektGebaeudeCtrl.cs:98`; `…\epos-spike\daten\probe_log.txt:326`.
(d) hoch — sachlich falsch; 2.2 und 7.6 bauen darauf auf, das Feld sei tot.
(e) Ersatz: „die Weiche des **Rechenwegs** ist allein `Typ == "Wohngebaeude  VDI 2067"` (zwei Leerzeichen, `SimulationWaermebedarf.cs:601-608`); das Feld `Wohngebaeude_Nicht_Wohngebaeude` steuert nur die **Katalogauswahl** (`GebaeudeCtrl.cs:23`, `GebaeudeStammCtrl.cs:83-84`, ein Treffer in der Testdatenbank: Gebäude 10632) und geht in keine Rechnung ein."

**2. Die Namensumstellung ersetzt den Neubau der Sicht nicht — Schritt 77 liefe sonst ins Leere** [R1]
(a) 2.2: „Neue Spalten müssen hinter `ID` in die Sicht, oder der Leser wird auf Namen umgestellt (wie `GebaeudeCtrl.MapRowToModel`). Dieses Papier wählt die Umstellung (6.2)."
(b) `Abfrage_Projektgebaeude` ist eine Sicht mit **fester Spaltenliste**; ohne Neubau der Sicht erreicht keine neue `Tab_Gebaeude`-Spalte irgendeinen Leser — auch den Namensleser nicht, dessen `dt.Columns.Contains(...)` dann `false` liefert. Die beiden Wege sind keine Alternativen.
(c) `sql/schema/002_views.sql:89-91` (Spaltenliste endet auf `Tab_Gebaeude.ID`), gelesen über `ProjektGebaeudeCtrl.cs:29` (`SELECT * FROM Abfrage_Projektgebaeude`); in `WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs` gibt es für SQLite kein `DROP VIEW`/`CREATE VIEW`-Muster.
(d) hoch — sachlich falsch; G1 wäre nach Papier abgenommen und das Modell bekäme keine Parameter.
(e) Ersatz: „Neue Spalten erreichen den Leser nur über die Sicht: Schemaschritt 77 muss **beides** tun — (1) `Abfrage_Projektgebaeude` in `sql/schema/002_views.sql` und im Migrationsschritt neu aufbauen (`DROP VIEW` + `CREATE VIEW`, neue Spalten hinter `Tab_Gebaeude.ID`; SQLite kennt kein `ALTER VIEW`), (2) den Leser auf Namenszugriff umstellen (Muster `GebaeudeCtrl.MapRowToModel`), damit künftige Spalten die Zuordnung nicht mehr still verschieben (6.2)."

**3. Kap. 0 verschweigt die Bedingungen, unter denen „alle zwölf" bestanden sind** [P1, P2, L6]
(a) 0.2: „**Der neu geschriebene Löser besteht alle zwölf Normtestfälle** (Abweichung 0,05–0,14 K bzw. 0,6–1,5 W; Prüfschwelle 0,15 K / 1,5 W wie in den AixLib-Validierungen, die Norm nennt 0,1 K / 1 W)."
(b) An der im selben Satz genannten Normschwelle fallen vier der zwölf Fälle durch (TF 6: 1,4986 W; TF 9: 0,1363 K; TF 10: 0,1430 K; TF 11: 4,9183 W); Testfall 11 besteht überhaupt nur mit Nachbildung des AixLib-Messfensters, 9 und 10 mit 0,01 K Reserve. Der Entscheider, der nur Kap. 0 liest, erfährt den einzigen offenen technischen Punkt nicht.
(c) `…\epos-spike\Prototyp\out\validierung.csv` (alle zwölf Zeilen, Spalten `toleranz`/`max_abw`/`bestanden`); Papier 5.2 samt Fußnote 1.
(d) hoch — irreführend für die Beauftragung von G0.
(e) Ersatz: „**Der neu geschriebene Löser besteht alle zwölf Normtestfälle an der AixLib-Prüfschwelle 0,15 K / 1,5 W** (0,05–0,14 K bzw. 0,6–1,5 W). Drei Fälle liegen knapp: Testfall 11 besteht nur mit Nachbildung des 120-s-Messfensters der Referenz (roh 4,9 W), 9 und 10 mit 0,01 K Reserve; an der Normschwelle 0,1 K / 1 W bleiben damit vier Fälle offen (6, 9, 10, 11). Ob die Rundung der Referenztabellen sie erklärt, wird in G0 vor der Übernahme geklärt — das ist der einzige offene technische Punkt des Vorschlags."

**4. Die Textwert-Zählungen in 3.1 stammen aus einer anderen Grundgesamtheit** [L4]
(a) 3.1: „Textwerte: `Typ` = „Wohngebaeude  VDI 2067" (19×), „Wohnblock" (5), „Hotel" (2); `Baualtersklasse` A (17), F (4), H (2), G (2), D (1)"
(b) Alle drei Zählungen summieren auf 26, der Absatz spricht von 15 Gebäudezeilen. Nachgezählt aus der Konsolenprobe: `Typ` 10 / 4 / 1; `Baualtersklasse` A 9, F 3, D 1, G 1, H 1; `Gebaeudeart` EFH 9, gr. MFH 4, MFH 1, Hotel 1.
(c) `…\epos-spike\daten\probe_log.txt`, 15 `GEB`-Zeilen (`grep -c "^GEB\t"` = 15).
(d) hoch — sachlich falsch.
(e) Ersatz: „Textwerte der 15 Zeilen: `Typ` = „Wohngebaeude  VDI 2067" (10×), „Wohnblock" (4), „Hotel" (1); `Baualtersklasse` A (9), F (3), D (1), G (1), H (1); `Gebaeudeart` Einfamilienhaus (9), großes Mehrfamilienhaus (4), Mehrfamilienhaus (1), Hotel (1)."

**5. „0 und 366 = aus" gilt nur für `Ferienbeginn_1`; als `Ferienende_n` sprengt 366 das Feld** [R3]
(a) 3.3: „| `Ferienbeginn_n` / `Ferienende_n` | Tag des Jahres 1–365; 0 und 366 = aus | `SimulationWaermebedarf.cs:701-731` |"
(b) Nur Zeitraum 1 prüft `> 0 && <= 365` — und zwar nur den Beginn; die Zeiträume 2–4 prüfen bloß `> 0`. `F_Absenkung` ist `bool[365]`, jede Schleife mit `Ferienende_n = 366` läuft auf Index 365 und wirft. Zeitraum 1 indiziert zudem ohne `−1` und läuft über den Jahreswechsel, 2–4 mit `−1`.
(c) `EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:59` (`private bool[] F_Absenkung = new bool[365];`), `:700-732`.
(d) hoch — irreführend; 2.2 („Sollwertfahrplan unverändert nutzbar") und 4.4 („Sollwerte genau wie im Bestand") bauen darauf auf.
(e) Ersatz: „| `Ferienbeginn_n` / `Ferienende_n` | Tag des Jahres. **Zeitraum 1** ist der Jahreswechselblock (Beginn…365 **und** 0…Ende, ohne `−1`-Versatz); nur sein Beginn ist mit `≤ 365` geriegelt, `366` heißt dort „aus". **Zeiträume 2–4** laufen `Beginn−1 … Ende` und prüfen nur `> 0`. Ein `Ferienende_n = 366` greift in allen vier Zeiträumen über das Feld `bool[365]` hinaus. Das VDI-6007-Modell bildet den Fahrplan nach Tagesindex 1…365 einheitlich ab und lehnt Werte > 365 benannt ab (4.8) — an dieser Stelle bewusst **nicht** zeichengleich zum Bestand. | `SimulationWaermebedarf.cs:59`, `:700-732` |"

**6. „zwei Drittel" kehrt das Verhältnis der Zerlegung 5.10 um** [P5, L7]
(a) 0.4: „zwei Drittel davon sind versteckte Kalibrierfaktoren 0,83/0,95/0,45 im Bestand und der rohe g-Wert, der reine Struktureffekt beträgt −5 bis −13 %." (gleichlautend Kap. 12)
(b) Nach 5.10 tragen die Parametrierungsanteile +20,7 bis +41,6 Prozentpunkte bei Gesamtabweichungen von +7,3 bis +33,0 % — in **jedem** Projekt mehr als 100 % (1040–1045: 13,9 + 6,8 = 20,7 gegen 7,3); die Modellstruktur zieht gegenläufig herunter.
(c) Papier 5.10, alle sechs Zeilen (additiv stimmig); Gegenprobe `…\Prototyp\out\empfindlichkeit_1045.csv` („Altmodell-Randbedingungen;51421.1;-19.25;-13.37").
(d) hoch — arithmetisch unmöglich.
(e) Ersatz: „Die versteckten Kalibrierfaktoren 0,83/0,95/0,45/0,83 und der rohe g-Wert erklären die Abweichung vollständig und darüber hinaus (+21 bis +42 Prozentpunkte); die Modellstruktur wirkt gegenläufig und senkt den Bedarf bei gleichen Randbedingungen um 5 bis 13 %. Die Abweichung ist also Parametrierung, nicht Physik." (Kap. 12 gleichlautend: „davon mehr als vollständig Parametrierung, gegenläufig 5–13 % Modellstruktur".)

**7. Q7 ist mit den Angaben des Papiers nicht entscheidbar — `Waermelast_Max` ist keine Gebäudegröße** [L1]
(a) 13, Q7: „**`Waermelast_Max` = Spitze als gleitendes Tagesmittel**, die Stundenspitze und das 95-%-Quantil daneben ausgewiesen"
(b) `Waermelast_Max` ist das Maximum des **Summenvektors aller drei Kanäle über das ganze Projekt** und normiert zugleich die Dauerlinie; ein gebäudeweises Tagesmittel lässt sich dort nicht einsetzen, ohne Dauerlinie und Deckungsrechnung von der Anzeige zu entkoppeln.
(c) `EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:401` (`Waermebedarf_Max = Maximaler_Waermebedarf(Waermebedarf)`), `:405`, `:407` (`Normieren(…, Waermebedarf_Max)`), `SimulationRunner.cs:358`, `Bericht/Bausteine/BausteineProjekt.cs:68`.
(d) hoch — die Empfehlung ist so nicht ausführbar.
(e) Ersatz: „**Option ja, NULL = unbegrenzt. `Waermelast_Max` bleibt unverändert das Maximum des Kanalsummenvektors** (`SimulationWaermebedarf.cs:401`), damit Dauerlinie, Deckung und Anzeige eine Basis behalten. Die drei Gebäudekennzahlen (Stundenspitze, gleitendes Tagesmittel, 95-%-Quantil) werden **zusätzlich je Gebäude** ausgewiesen und im Bericht neben `Waermelast_Max` gestellt. Wer die Aufheizspitze nicht auslegen will, setzt `Heizleistung_Max`; Rückrechnung ohne Grenze."

**8. Das positive Abnahmekriterium für G1 ist so nicht erfüllbar** [L3]
(a) 10.4: „(2) der Kern reproduziert die Zahlen des Prototyps aus Kapitel 5 auf den Referenzprojekten bei gleicher Parametrierung innerhalb 1e‑6 relativ"
(b) Der Prototyp lief in UTC-Speicherreihenfolge, mit den isotropen `Sol_*`-Spalten und ohne Absorptionsterm; der Kern soll nach 4.4/Q20/Q21 in Ortszeit und mit Hay-Davies rechnen. 1e‑6 ist nur mit einem Prüfmodus erreichbar, den das Papier nirgends fordert.
(c) Papier 5.4 („Die Fassadenstrahlung kam aus den isotropen `Sol_*`-Spalten — der Hay-Davies-Weg aus 4.4 ist im Prototyp **nicht** gemessen", „Zeitreihen in Speicherreihenfolge (UTC) für **beide** Modelle") gegen 4.4, Q20, Q21.
(d) hoch — das Gate von G1 wäre unerreichbar.
(e) Ersatz: „(2) der Kern reproduziert die Zahlen des Prototyps aus Kapitel 5 innerhalb 1e‑6 relativ **in einem Prüfmodus mit den Randbedingungen des Prototyps (UTC-Reihenfolge, isotrope `Sol_*`-Spalten, kein Absorptionsterm)**; der Unterschied zwischen diesem Prüfmodus und der Auslieferungsparametrierung (Ortszeit, Hay-Davies) wird je Referenzprojekt als Zahl ausgewiesen und begründet."

**9. „43 % über der Spitze … als Tagesmittel nur 18 %" stellt einen Einzelwert einer Gesamtsumme gegenüber** [P6]
(a) 4.5: „sie liegt 43 % über der Spitze des Tagesmodells, als Tagesmittel nur 18 % (5.6)." (gleichlautend 5.6 und Kap. 12)
(b) Nachgerechnet: die Stundenspitze reicht je Projekt von **−10,0 %** (1040–1045) bis **+57,0 %** (1017), über alle zwölf Projekte summiert +28,8 %; +43 % trifft allein 1023/1024 (276,60/193,96). Die +18 % dagegen sind der Summenwert über alle zwölf Projekte (797,2/677,3 = +17,7 %).
(c) `…\Prototyp\out\vergleich_zusammenfassung.csv` (`proto_max_kW`) gegen Papier 3.7 (`Max kW`) und 5.6 (Tagesmittel-Spalten).
(d) hoch — zwei Bezugsgrößen in einem Satz.
(e) Ersatz: „sie liegt je nach Projekt −10 bis +57 % neben der Spitze des Tagesmodells (über alle zwölf Projekte zusammen +29 %); als gleitendes Tagesmittel schrumpft der Abstand auf +18 % (5.6)." (Kap. 12: „Spitzenlast je Projekt −10 bis +57 % (Stunde), in der Summe +29 %, als Tagesmittel +18 %".)

**10. Die CDDL-Bewertung ist juristisch falsch verkürzt** [I1]
(a) 7.4: „**CDDL-1.0 ist Datei-Copyleft:** Einbindung in ein proprietäres Produkt ist zulässig, solange die CDDL-Dateien unverändert bleiben; der Lizenztext wird mit ausgeliefert (Setup: Lizenzhinweise)."
(b) „Unverändert bleiben" ist keine CDDL-Bedingung. §3.1 verlangt die Quelltext-Verfügbarkeit für **jede** ausgelieferte Covered Software — auch für unveränderte — samt Hinweis an den Empfänger; unverändert zu bleiben erspart nur die Offenlegung eigener Modifikationen.
(c) CDDL-1.0 §3.1/§3.4/§3.5/§3.6, https://spdx.org/licenses/CDDL-1.0.html; gleicher Text auf https://docs.xbim.net/license/license.html.
(d) hoch — trägt die Entscheidung Q9.
(e) Ersatz: „**CDDL-1.0 ist Datei-Copyleft.** Die Einbindung in ein proprietäres Produkt ist zulässig (§3.6 Larger Work), die eigene Binärfassung darf unter eigener Lizenz ausgeliefert werden (§3.5). Drei Auflagen bleiben: (1) §3.1 — der Quelltext **aller** ausgelieferten CDDL-Dateien muss unter CDDL verfügbar sein, auch wenn nichts geändert wurde; der dauerhafte Verweis auf github.com/xBimTeam bzw. die NuGet-Quellpakete genügt und ist dem Empfänger mitzuteilen. (2) §3.4 — Copyright-, Patent- und Markenvermerke bleiben stehen, eigene Änderungen an CDDL-Dateien sind zu kennzeichnen. (3) Der Lizenztext wird mit ausgeliefert (Installationspaket: Lizenzhinweise). Daraus die Regel: **xBIM nur als NuGet-Paket einbinden, nie forken.**"

**11. Die Zeile `Xbim.Geometry` nennt eine falsche Lizenz und eine Vorabversion** [I2, I3]
(a) 7.4, Tabelle: „| Xbim.Geometry | 6.3.891 | CDDL-1.0 | net472, net8.0 | **C++/CLI, nur Windows** (`Ijwhost.dll`, `win-x64`) | dito | ja (OCCT) |"
(b) `Xbim.Geometry.Engine.Interop` hängt zwingend an `Xbim.Geometry.Occt` ≥ 7.8.1 unter **LGPL-2.1 mit OCCT-exception-1.0** — genau die Lizenzfamilie, die zwei Zeilen tiefer ausgeschlossen wird; außerdem ist 6.3.891 ein Prerelease (`-netcore`), die letzte stabile Fassung ist 5.1.820 (nur net472), .NET 8 gibt es also stabil gar nicht.
(c) nuspec `xbim.geometry.engine.interop/6.3.891-netcore` (Abhängigkeit `Xbim.Geometry.Occt (7.8.1)`); https://www.nuget.org/packages/Xbim.Geometry.Occt (LGPL 2.1 mit OCCT-Ausnahme); https://api.nuget.org/v3-flatcontainer/xbim.geometry.engine.interop/index.json.
(d) hoch — der Leser entscheidet Q9 auf falscher Lizenzangabe.
(e) Ersatz Tabellenzelle: „| Xbim.Geometry (`Xbim.Geometry.Engine.Interop`) | **6.3.891-netcore (Vorabversion)**, stabil zuletzt 5.1.820 (nur net472) | CDDL-1.0, **zieht `Xbim.Geometry.Occt` 7.8.1 unter LGPL-2.1 + OCCT-exception-1.0 nach** | net472, net8.0 | **C++/CLI, nur Windows** | dito | ja (OCCT) |" — und Ausschlusssatz: „LGPL und GPL scheiden für den Kern aus (IfcOpenShell LGPL-3.0, IFC2SB GPL); **auch `Xbim.Geometry` fällt damit aus dem Kern — nicht nur wegen C++/CLI, sondern weil OCCT unter LGPL-2.1 steht und selbst in der Windows-Schale dynamische Bindung und Austauschbarkeit nachzuweisen wären.**"

---

### Schwere: mittel

**12. Tabelle 5.2 führt Testfall 11 als bestanden, die mitgelieferte Messdatei als durchgefallen** [P1]
(a) 5.2: „| 11 Kühldecke (Senke am IW-Oberflächenknoten) | Φ | 1,376 W | ja¹ |"
(b) Die Rohdatei weist 4,9183 W bei Toleranz 1,5 W und „nein" aus; die 1,376 W stammen aus einem zweiten Lauf, in dem der Löser das 120-s-Messfenster der Referenz nachbildet. Geändert wurde das Abnahmeverfahren, nicht der Löser — das gehört in die Zelle, nicht nur in die Fußnote.
(c) `…\Prototyp\out\validierung.csv`, Zeile `11;Q;W;1.5;4.9183;…;nein;85.0`; Gegenstück `…\Prototyp\out\lauf.log:164` („max |Abw| = 1.376 W -> BESTANDEN").
(d) mittel — die Fußnote nennt den Sachverhalt, die Tabellenzelle widerspricht der Messdatei.
(e) Ersatz: „| 11 Kühldecke (Senke am IW-Oberflächenknoten) | Φ | 4,92 W roh / 1,376 W mit nachgebildetem 120-s-Messfenster | **nein / ja¹** |"

**13. „Falsche Richtung" der Absorptionspauschale ist gegen die einzige unabhängige Referenz des Papiers nicht haltbar** [P3]
(a) 4.4: „Die Pauschale „+0,6·I/25 − 3 K" ist ausdrücklich **nicht** zulässig: sie erhöht den Bedarf in allen Projekten um 3–7 % (5.8), weil −3 K Tag und Nacht wirkt."
(b) Gemessen an `spez_Waermeverbrauch` (5.5) verbessert die Variante die Trefferquote in sechs von sieben Katalogbauten, weil der Prototyp durchweg **unter** der Katalogkennzahl liegt (10614: 86,8 % → 92,9 %; 10643: 85,7 % → 92,8 %). „Falsche Richtung" ist ohne Bezugsgröße mindestens unbelegt; die Ablehnung selbst (keine Physik) trägt auch ohne dieses Argument.
(c) `…\Prototyp\out\vergleich_zusammenfassung.csv`, Spalte `abs_kWh`/`abs_abw_proz` (je Projekt +3,2 … +7,1 %) gegen `proto_kWh_m2a` und Papier 5.5.
(d) mittel — unbelegt/irreführend begründet.
(e) Ersatz: „Die Pauschale „+0,6·I/25 − 3 K" ist keine Physik und wird nicht übernommen (α und ΔE_r getrennt, Himmelsaustausch bewölkungsabhängig). Ihre Wirkung von +3,2 bis +7,1 % (5.8) zeigt zugleich die Größenordnung des in G1 abgeschalteten Strahlungsterms: mit ihm träfe der Prototyp die Katalogkennzahl zu 91–99 % statt zu 86–95 %. Der Schalter `Aussenbauteile_Strahlung` ist deshalb in G1 vorzusehen und in G2 mit der Normformel zu füllen."

**14. „Zehn von zwölf" widerspricht den eigenen Zahlen — es sind elf** [P7, R10, L9]
(a) 4.5: „ohne Leistungsgrenze fällt die Jahresspitze in 10 von 12 Referenzprojekten auf dieselbe Stunde" / 5.6: „In zehn von zwölf Projekten liegt die Prototyp-Spitze auf demselben Index"
(b) Nur Projekt 1018 weicht ab (Index 438); 1007, 1008, 1017, 1023, 1024, 1039, 1040, 1041, 1042, 1045, 1046 = elf Projekte liegen auf Index 1 398.
(c) `…\Prototyp\out\vergleich_zusammenfassung.csv`, Spalte `proto_max_std` (1399 in 14 von 15 Zeilen, 439 nur bei 10632); Papier-Tabelle 5.6.
(d) mittel — Zahl gegen eigenen Beleg.
(e) Ersatz (beide Stellen): „fällt die Jahresspitze in **elf von zwölf** Referenzprojekten auf dieselbe Stunde (Index 1 398); nur Projekt 1018 (München) weicht ab (Index 438)."

**15. Ost/West-Verhältnis: 6,8–8,5 % trifft keinen der beiden Klimaorte** [P9, L11]
(a) 5.12: „(Ost liegt in der Jahressumme 6,8–8,5 % über West, Tagesmaximum Ost bei Index 8, West bei Index 14)."
(b) **Klärung:** [L] nennt nur Stuttgart (+9,3 %), [P] beide Orte. Nachgerechnet über alle dreizehn Klimaregionen gilt genau zweierlei: Stuttgart **+9,30 %** (776 870,3 / 710 762,1) und München **+7,29 %** (761 404,4 / 709 677,1). Die angegebene Spanne schließt keinen der beiden Werte ein; 3.5 nennt für Stuttgart selbst „88,7 gegen 81,1 W/m²" = +9,4 %.
(c) `…\epos-spike\daten\probe_log.txt:22/24` und `:91/93` (`SOLSTAT … Sol_Ost/Sol_West`).
(d) mittel.
(e) Ersatz: „(Ost liegt in der Jahressumme 7,3 % (München) bis 9,3 % (Stuttgart) über West, Tagesmaximum Ost bei Index 8, West bei Index 14)."

**16. „86–101 %" ist nach oben und unten falsch gerundet — und stützt sich auf die fehlerhafte Zeile** [P19, R15, L]
(a) 5.5: „trifft die Katalogkennzahl `spez_Waermeverbrauch` zu **86–101 %**, das Tagesmodell zu 48–88 %." (gleichlautend 0.4 und Q16; 10.4 nennt für dasselbe Kriterium 85–105 %)
(b) Aus 5.5 folgt 85,7 % (10643: 386,6/451) bis 100,3 % (10576: 112,8/112,5). Der einzige Wert über 95 % ist die fehlerhafte Zeile 10576; ohne sie reicht die Spanne nur bis **95,4 %** (10599). Die Zahl des Tagesmodells (48–88 %) stimmt.
(c) Papier 5.5; `…\Prototyp\out\vergleich_zusammenfassung.csv` (`proto_kWh_m2a`), Nachrechnung aller acht Zeilen mit Katalogwert.
(d) mittel.
(e) Ersatz: „trifft die Katalogkennzahl `spez_Waermeverbrauch` zu **86–100 %** (85,7 % bei 10643 bis 100,3 % bei der fehlerhaften Zeile 10576; ohne sie 86–95 %), das Tagesmodell zu 48–88 %." — in 10.4 das Kriterium auf denselben Wortlaut bringen: „(4) Katalogkennzahl zu 85–105 % getroffen, wo sie gefüllt ist" bleibt als Abnahmefenster, wird aber als Fenster benannt, nicht als Messwert.

**17. „Zeitkonstanten … 1–2 Tage" steht weder in 5.7 noch in den Rohdaten** [P12, L17]
(a) 4.6: „Zeitkonstanten schwerer Bauweisen liegen bei 1–2 Tagen (5.7), der Vorlauf ist ausreichend und deterministisch."
(b) 5.7 und die Rohdaten nennen 7,5–24,8 h (Maximum 24,8 h bei 10599). **Klärung:** [L] weist zusätzlich zu Recht darauf hin, dass 4.2 für Testfall 1 eine Zeitkonstante von 264 h nennt — die Begründung muss beide Größen tragen.
(c) Papier 5.7; `…\Prototyp\out\vergleich_zusammenfassung.csv`, Spalte `tau_h`; Papier 4.2 („Zeitkonstanten 264 h und 5,3 h").
(d) mittel.
(e) Ersatz: „Die gemessenen Zeitkonstanten der Referenzgebäude liegen bei 7,5–24,8 h (5.7); 30 Tage sind damit rund das Dreißigfache. Die langsameren Normtesträume (bis 264 h, 4.2) deckt der Vorlauf auf unter 0,1 K ab — der Nachweis dafür gehört in G0."

**18. Fenster und Wärmebrücken fehlen in den Knotenbilanzen** [P15]
(a) 4.2, Tabelle: „| R_ve | θ_out ↔ θ_air | 1 / H_ve, H_ve = n · V · 0,34 Wh/(m³K) |" und fünfte Bilanz „+ (θ_out − θ_air)/R_ve"
(b) Die Zeile darunter fasst Fenster und Wärmebrücken „mit R_ve zusammen", aber der definierte Wert von R_ve ist rein die Lüftung. Wer die Gleichungen umsetzt, baut ein anderes Modell als den Prototyp: `UA_masselos` ist bei 10643/10645 mit 232,7 W/K fast doppelt so groß wie `H_ve` (131,6 W/K).
(c) Papier 4.2 (Widerstandstabelle, Gleichung 5); `…\Prototyp\out\vergleich_zusammenfassung.csv` (Spalten `UA_masselos`, `H_ve`).
(d) mittel.
(e) Ersatz: „| R_ext | θ_out ↔ θ_air | 1 / H_ext, **H_ext = H_ve + U_w·A_w + Σψ·L**, H_ve = n · V · 0,34 Wh/(m³K) — Fenster und Wärmebrücken laufen masselos im selben Zweig |" und in der fünften Bilanz „+ (θ_out − θ_air)/R_ext".

**19. Die Vorschrift 4.4 weicht in zwei Punkten von dem ab, was validiert wurde** [P13, P14]
(a) 4.4: „`θ_eq,k = θ_out + (α·I_k − ε·ΔE_r,k)/h_a` mit α = 0,6, h_a = 25 W/(m²K)" und „Eintrag zu 100 % radiativ auf die Oberflächen."
(b) 5.3 beschreibt dieselben Größen anders: `(T_Himmel − T_Luft)·h_rad/(h_rad + h_a)` und `H_sol·α/(h_rad + h_a)` — mit h_rad = 5 und h_a = 25 stehen sich Nenner 25 und 30 gegenüber (17 % im Strahlungsterm), und h_rad ist in 4.2 als **innerer** Strahlungskoeffizient definiert. Ebenso hält 5.3 fest: „9 % des Fenstersolareintrags konvektiv an die Luft" — so wurde gegen die zwölf Normtestfälle validiert.
(c) Papier 4.2 (Zeile R_rad), 4.4, 5.3; `…\Prototyp\out\lauf.log:11-12` („Strahlungssplit Solar : AW=[0.000000] IW=[1.000000]").
(d) mittel — die Vorschrift für den Kern weicht vom Gütebeweis ab, ohne dass die Abweichung benannt wird.
(e) Ersatz: „`θ_eq,k = θ_out + (α·I_k − F_r·ε·ΔE_r,k)/h_a` mit α = 0,6 und h_a = 25 W/(m²K) als **Summe** aus äußerem Konvektions- und Strahlungsübergang — dieselbe Formel, die 5.3 als `α·H_sol/(h_rad,a + h_conv,a)` schreibt; `h_rad` in 4.2 bezeichnet den **inneren** Austausch und ist umzubenennen." sowie „Eintrag radiativ auf die Oberflächen, **9 % konvektiv an die Luft — wie in den Normtestfällen und im Prototyp (5.3)**; die Verteilung auf AW/IW folgt `splitFacVal`."

**20. Die beiden ISO-13790-Zitate in 4.3 verdecken zwei bewusste Abweichungen** [P16, P17]
(a) 4.3: „R_1,AW = 1 / (h_ms · A_AW,opak) (h_ms = 9,1 W/(m²K), DIN EN ISO 13790 12.2.2)" und „(leicht 72, schwer 180, sehr schwer 360 kJ/(m2K) je m² Wohnfläche; DIN EN ISO 13790 Tab. 12: 80 / 165 / 370 kJ/(m2K) für sehr leicht / mittel / sehr schwer)"
(b) In ISO 13790 hängt h_ms an **einer** wirksamen Speicherfläche A_m = 2,5·A_f; hier wird der Koeffizient auf beide Massepfade gelegt (10643: 492 + 503 = 995 m² statt 503 m²). Und Tab. 12 kennt fünf Klassen (80 / 110 / 165 / 260 / 370); zitiert sind genau die drei, die passen — ausgelassen sind ISO „leicht" (110) und ISO „schwer" (260), also die Namen, die mit den EPOS-Namen übereinstimmen. EPOS „schwer" (180) ist in Wahrheit ISO *mittel*.
(c) Papier 4.3 und 3.3; `EPOS.Kern/Allgemein/Gebaeudebauweise.cs:22-67` (×20/50/100); Flächen 10643 aus `…\daten\probe_log.txt:339`, `UA_opak` 984,04 aus `vergleich_zusammenfassung.csv`.
(d) mittel — unbenannte Normabweichungen an zwei Stellen.
(e) Ersatz: „R_1,AW = 1/(h_ms·A_AW,opak), R_1,IW = 1/(h_ms·A_IW), h_ms = 9,1 W/(m²K). **Abweichung von ISO 13790 bewusst:** dort hängt h_ms an der einen wirksamen Speicherfläche A_m = 2,5·A_f; hier wird der Koeffizient auf beide Massepfade des 7R2C-Netzes angewandt (5.9: Jahresenergie ≤ 0,1 %; für Leistungs- und Kühlgrenze in G3 durch den Bauteilweg abzulösen)." und „(die drei EPOS-Bauarten ergeben 72 / 180 / 360 kJ/(m²K) je m² Wohnfläche. DIN EN ISO 13790 Tab. 12 kennt fünf Klassen — 80 / 110 / 165 / 260 / 370 für sehr leicht / leicht / mittel / schwer / sehr schwer; die EPOS-Namen sind um eine Stufe verschoben: EPOS „leicht" = ISO *sehr leicht*, EPOS „schwer" = ISO *mittel*. Beim Umbenennen der Klappliste ist das zu berücksichtigen.)"

**21. „Die Einfrierregel" gibt es nicht — es gibt drei, und keine deckt Gebäudedaten** [R9]
(a) 5.11: „die Korrektur der Testdatenbank verändert das Referenzergebnis von Projekt 1008 und läuft über die Einfrierregel (Q22)."
(b) Die Basis kennt genau drei benannte Einfrierregeln — Emissionsfaktoren, PV-Modulkoeffizienten, Flottenstand 1046. Gesäte Gebäudedaten fallen durch das Netz, statt es zu benutzen.
(c) `Referenzlaeufe/LIESMICH.md:57`, `:79`, `:100`; `CLAUDE.md`, Abschnitt „Regressionsnetz".
(d) mittel.
(e) Ersatz (5.11, 10.4, Q22 gleichlautend): „…verändert das Referenzergebnis von Projekt 1008. Dafür gibt es heute **keine** Einfrierregel; die drei bestehenden (`Referenzlaeufe/LIESMICH.md:57/79/100`) decken Emissionsfaktoren, PV-Modulkoeffizienten und den Flottenstand 1046. G1 legt deshalb im selben Schritt eine **vierte Einfrierregel „gesäte Gebäudedaten"** an (`Tab_Gebaeude(_STAMM)`: `Bauweise`, U-Werte, Flächen, Sollwerte, `Luftwechselrate`, `Fensterdurchlassgrad`) und trägt sie in `Referenzlaeufe/LIESMICH.md` und in den Abschnitt „Regressionsnetz" der `CLAUDE.md` ein."

**22. Tabelle 6.1: Hausregelverstoß beim Schalter, und die G2-Spalten fehlen ganz** [R7, L13]
(a) 6.1: „| `Aussenbauteile_Strahlung` | INTEGER `CHECK (… IN (0,1))` | θ_eq mit Absorption und Abstrahlung | 0 |"
(b) Die Hausregel schreibt für neue Boolean-Spalten `INTEGER NOT NULL DEFAULT 0 CHECK (…)` vor; „NULL bedeutet 0" ist damit unvereinbar (die NULL-Vorgaberegel gilt Fachwerten, nicht Schaltern). Zudem brauchen Infiltration (0,3 1/h), Nutzerlüftung (0,4 1/h) und die Sommerlüftungsregel aus 4.4 Spalten, die weder in 6.1 noch in Kap. 12 („elf Spalten in zwei Tabellen") vorkommen — G2 ist so nicht beauftragbar.
(c) `Dokumentation/aktuell/BETRIEB_SQLITE.md:299` („gibt ihr `INTEGER NOT NULL DEFAULT 0 CHECK (spalte IN (0,1))` — dann bleibt das so"); Bestandsmuster `sql/schema/001_grundschema.sql`; Papier 4.4 gegen 6.1 und Kap. 12.
(d) mittel.
(e) Ersatz Zeile: „| `Aussenbauteile_Strahlung` | INTEGER `NOT NULL DEFAULT 0 CHECK ("Aussenbauteile_Strahlung" IN (0,1))` | θ_eq mit Absorption und Abstrahlung | — (Schalter, kein Fachwert: hier gilt die Boolean-Regel aus `BETRIEB_SQLITE.md`) |" und neuer Absatz: „**Mit G2 kommen drei Spalten in Schemaschritt 78:** `Luftwechsel_Infiltration` (REAL 1/h, NULL = 0,3), `Luftwechsel_Nutzer` (REAL 1/h, NULL = 0,4), `Sommerlueftung` (INTEGER NOT NULL DEFAULT 0 CHECK (… IN (0,1)))." — Kap. 12 entsprechend: „elf Spalten (G1), drei Spalten (G2), drei Tabellen (G3)".

**23. Zwei der drei Datenbefunde aus 0.6 haben keine Stufe, keinen Aufwand und keinen Platz in der Reihenfolge** [L2]
(a) 0.6: „**Drei Datenbefunde sind unabhängig vom Modell zu beheben:** ein Gebäude der Testdatenbank trägt den stillen Rückfallwert `Bauweise = 50 Wh/K` …, der Rechenweg teilt ungeschützt durch `Wohnflaeche`, und der Raumtemperatur-Zustand des Bestands ist ein statisches Feld"
(b) Nur die `Bauweise`-Korrektur steht in G1 (Kap. 11) und in Kap. 15. Q18 (Warnung im Tagesmodell) und Q23 (`_prevRoomTemp` als Instanzzustand, „eigener, begründeter Einfrierschritt") tauchen in Stufenplan, Aufwand und Reihenfolge nirgends auf — wer Q23 mit „Ja" beantwortet, beauftragt ins Leere.
(c) Papier Kap. 11 und Kap. 15 gegen Q18/Q23; Bestandsbelege bestätigt (`BhkwPlan.cs:51`, `:54`, `:435`; `SimulationWaermebedarf.cs:571`).
(d) mittel.
(e) Ersatz — neue Zeile in Kap. 11 vor G2: „| **GB — Bestandsbefunde** | Warnungen statt stiller NaN im Tagesmodell (Q18), `_prevRoomTemp` als Instanzzustand mit `ResetState` je Gebäude (Q23), Korrektur 10576 (Q22) | Referenzergebnis von 1008 und 1039 ändert sich — eigener, begründeter Einfrierschritt | klein, 1–2 PT |" und in Kap. 15 als Punkt 5 einreihen.

**24. Die AixLib-Lizenzangabe stimmt nicht, und die VDI-Referenzzahlen deckt sie ohnehin nicht** [I4, I5]
(a) 5.1: „`AixLib.ThermalZones.ReducedOrder.Validation.VDI6007.TestCase1…12` (RWTH Aachen, E.ON ERC, EBC; **BSD 3-Clause**)." (gleichlautend 10.1: „(BSD 3-Clause, Copyright-Vermerk im Test)")
(b) AixLib steht unter einem *überarbeiteten* 3-Klausel-BSD mit einem Zusatzabsatz (Rückfluss von Verbesserungen) — nicht unter SPDX `BSD-3-Clause`; GitHub erkennt für das Repositorium keine Standardlizenz. Und die übernommenen **Zahlenwerte** geben Tabellen der VDI 6007 Blatt 1 wieder, an denen der VDI das Urheberrecht hält; die RWTH kann daran keine Rechte einräumen.
(c) `AixLib/UsersGuide/License.mo` („a revised 3 clause BSD license with an ADDED paragraph at the end"), `AixLib/UsersGuide/Copyright.mo`; https://api.github.com/repos/RWTH-EBC/AixLib (`license: null`); Referenzreihen als eingebettete `CombiTimeTable` in `Validation/VDI6007/TestCase*.mo`.
(d) mittel — die Lizenz, die mitgeliefert werden soll, ist nicht die genannte.
(e) Ersatz: „(RWTH Aachen, E.ON ERC, EBC; **überarbeitete 3-Klausel-BSD-Lizenz mit Zusatzabsatz zur Rückgabe von Verbesserungen**, Wortlaut in `AixLib/UsersGuide/License.mo` — nicht identisch mit SPDX `BSD-3-Clause`, deshalb ist der AixLib-Wortlaut selbst mitzuliefern. Der Vermerk lautet: ‚Copyright (c) 2010-2018, RWTH Aachen University, E.ON Energy Research Center, Institute for Energy Efficient Buildings and Indoor Climate.') **Offen bleibt, ob die Zahlenwerte selbst von dieser Lizenz gedeckt sind:** sie geben Tabellen der VDI 6007 Blatt 1 wieder. Vor der Abnahme ist die Richtlinie zu beziehen und die Zitierfähigkeit zu klären, oder die Referenzreihen bleiben interne Prüfdaten und werden nicht ausgeliefert (gehört in Q2/Q9)."

**25. „senken den Verlustkoeffizienten um 14,9–26,4 %" hat den Bezug verdreht** [P8]
(a) 5.10: „Die Gewichte senken den Verlustkoeffizienten der sieben Katalogbauten um 14,9–26,4 % (10614: 234,1 gegen 203,7 W/K; 10632: 4 440 gegen 3 636 W/K)." (gleichlautend 4.3: „sie senken den Verlustkoeffizienten um 14–26 %")
(b) 203,7/234,1 ist eine Senkung um **13,0 %**, 3 636/4 440 um 18,1 %. Die 14,9 bzw. 26,4 % sind die Werte des Protokolls für L_ungewichtet/L_gewichtet − 1, also die *Erhöhung* mit umgekehrtem Bezug; als Senkung gelesen ergibt sich 13,0–20,9 %.
(c) `…\Prototyp\out\lauf_vergleich.txt` („L_proto 234.1 W/K vs L_alt 203.7 W/K ( 14.9 %)", „L_proto 693.3 … L_alt 548.4 ( 26.4 %)"); Summenprobe `UA_opak`+`UA_masselos`+`H_ve` aus `vergleich_zusammenfassung.csv`.
(d) mittel.
(e) Ersatz: „Der ungewichtete Ansatz liegt bei den sieben Katalogbauten 14,9–26,4 % über dem gewichteten (10614: 234,1 gegen 203,7 W/K; 10632: 4 440 gegen 3 636 W/K); umgekehrt gelesen senken die Gewichte den Verlustkoeffizienten um 13,0–20,9 %." (4.3 entsprechend: „sie senken den Verlustkoeffizienten um 13–21 %".)

---

## Anhang A — kleinere Korrekturen, die noch in den Text gehören (je niedrig)

| # | Stelle | Befund und Ersatz |
|---|---|---|
| A1 | 2.1, 2.2, 2.3 | **Belegkorrekturen** (alle geprüft): `(:221)` → `(:222)` (221 ist die Kommentarzeile, `WattToKw` steht auf 222); „`SummenvektorAusKanaelen`, `:439`" → „`SimulationWaermebedarf.SummenvektorAusKanaelen`, `SimulationWaermebedarf.cs:439`" (`SimulationKanaele.cs:439` ist `public const int ANZAHL = 3;`); „`Sonnengeometrie` (`:353-384`)" und „(`CalculateHourlyHayDavies`, `:455-482`)" → beide mit Dateinamen `SolarPVGISCalculator.cs` (`KlimaImportAblauf.cs` hat nur 398 Zeilen); `ProjektGebaeudeCtrl.cs:26-107` → `:26-104` (Methodenende), Zuweisungen `:42-99`. |
| A2 | 2.1, Punkt 4 | Die Namensfalle im Laufweg sitzt in `ProjektGebaeudeCtrl.cs:56` (`row[14]` = Spalte 14 der Sicht = `Fensterflaeche_Ost_West`), nicht in `GebaeudeCtrl.cs:66` (das füllt `GebaeudeModel`, den Katalogweg). Beide Stellen nennen. |
| A3 | 4.3 / 4.8 | „Klemme ≥ 1e‑6 K/W" ist genau der stille Rückfall, den 4.8 verbietet. Ersatz: „**Wird der Ausdruck ≤ 0 — rechnerisch ab einem mittleren U über 4,17 W/(m²K) —, bricht die Rechnung mit benanntem Fehler ab; es gibt keine Klemme.**" (Nachgerechnet bleibt R_Rest für alle Referenzgebäude positiv, Höchstwert U 2,90 bei 10643 → R_Rest = 5,29·10⁻⁴ K/W; die Klemme greift heute nirgends.) |
| A4 | 2.1 Punkt 2 / 4.8 | Die Null-Prüfungs-Liste ist unvollständig: `:651` teilt durch das Rechenergebnis `VerbrauchAlt`, `:653` durch `Flaeche_Nutzer`. In 4.8 ergänzen: „`VerbrauchAlt > 0` vor der Rückrechnung". |
| A5 | 4.4 | „Wochenendtage folgen aus dem Wochentag des 1. Januar" nennt kein Bezugsjahr (sieben mögliche Masken). Ersatz: „…aus dem Wochentag des 1. Januar des **Referenzjahres der Zeitbasis** (`SolardatenCtrl.Referenzjahr(idProjekt)`, `:222`) — ein Test hält die Maske gegen `Tab_Klimadaten.WE` derselben Region (`KlimaImportAblauf.cs:354`)." |
| A6 | 4.4 | „ab Baualtersklasse nach 1995: 0,75" ist nicht ausführbar — 2.2 hält fest „**kein Baujahr als Zahl**", 3.1 zeigt Buchstaben (A, D, F, G, H), und `Baujahr` kommt laut 6.1 erst mit G4. Ersatz: „F_F = 1 − `Rahmenanteil` (Vorgabe 0,3, also 0,7). Eine klassenabhängige Vorgabe setzt eine Tabelle `Baualtersklasse` → Baujahrspanne voraus, die es heute nicht gibt; sie gehört als eigener Punkt in 6.1." |
| A7 | 4.4 | Die 18,8 K sind aus Kap. 3 nicht nachrechenbar (sie sind die Rohamplitude der **Tagesmittel**; aus den Stundenextremen −18,2 … 33,5 °C folgt 25,9 K). Ersatz: „(nicht (max−min)/2 der **Tagesmittel**: das ergäbe 18,8 K und −3,0 °C Erdreich; aus den Stundenextremen sogar 25,9 K)". Beleg `…\Prototyp\out\lauf_vergleich.txt` („Amplitude(Harmonische) 9.315 K (roh max-min/2 18.75 K)"). |
| A8 | 3.1 | „effektiv **sieben verschiedene Katalogbauten**" → **acht**: die 15 Zeilen tragen acht Parametersätze (EFH-A-TS-212 3×, GMH-F-U-130 3×, EFH-A-U-347s 4×, dazu MFH-H-U-112, GMH-D-S-118, Hotel-G-136, EFH-A-U-338, EFH-A-U-451). Beleg `…\daten\probe_log.txt`, `GEB`-Zeilen. |
| A9 | 3.1, 3.6, 5.5, 5.6, 5.10, 5.12 | „1040–1045" legt sechs Projekte nahe; es gibt 1040, 1041, 1042, 1045. Durchgehend durch „**1040/1041/1042/1045**" ersetzen (3.7 schreibt es bereits richtig). |
| A10 | 5.12, Tabelle | Unter einer Spaltenüberschrift stehen drei Rechenweisen: Jahresheizwärme volle Spanne (1023: 2,46 %), Stundenspitze und Tagesmittel-Maximum halbe Spanne gegen 50/50 (tatsächlich 0,48 % und 1,00 %). Ersatz: Kopf „Spanne 100 % Ost gegen 100 % West, bezogen auf den 50/50-Fall" und Werte „≤ 0,48 %" bzw. „≤ 1,0 %". Beleg `…\Prototyp\out\ostwest_projekte.csv` (1023: 277,261 / 275,942 bei 276,601; 172,191 / 170,476 bei 171,333). |
| A11 | 5.6 | Monatsmuster: „im Hochwinter klein (+10 bis +24 %)" gilt für sieben Projekte nicht (1040/41/42/45 +0,8…+3,3 %, 1008 +73…+82 %); im Juli liefert das Tagesmodell bei 1017 und 1023/1024 0 kWh, der relative Vergleich ist dort nicht definiert. Spannen erweitern und den Sonderfall benennen; „In allen Projekten dasselbe Muster" → „Das Muster ist dasselbe, die Höhe nicht." |
| A12 | 5.6 | „2 097 Nullstunden" → **2 092** (nachgezählt aus `…\Prototyp\out\vergleich_1045.csv`, Spalte `Q_proto_W`); die 936 des Tagesmodells stimmen exakt. |
| A13 | 5.6, Stundentabelle | Die fett gesetzten Werte des Tagesmodells (3 573 bei h18, 3 681 bei h20) sind nicht seine Maxima: die Stundensummen erreichen h9 = 3 833 und h19 = 3 875 kWh — beide Stunden fehlen in der Spaltenauswahl, der behauptete Doppelbuckel ist an seinen Spitzen nicht belegt. Spalten 9 und 19 aufnehmen und dort fett setzen. |
| A14 | 4.4 / 3.3 | Der Wechsel 1,2/3,6 = 0,3333 → 0,34 Wh/(m³K) hebt H_ve zusätzlich um 2 %, der Satz nennt als Unterschied nur den Temperaturfaktor. Ergänzen: „der Bestand rechnet mit 0,3333 Wh/(m³K) (`BhkwPlan.cs:357/359`) — der reine Zahlenwechsel hebt H_ve um 2 %." In 2.1 („0,2777") und 3.3 („0,2778") eine Schreibweise wählen: „`…·1,2·n·0,2778` (im Code `0.2777777777777778`, also 1/3,6)". |
| A15 | 0.3 | „Die Datenbank trägt fast alle Eingaben eines VDI-6007-Modells" ist freundlicher als 3.8 („neun Größen sind Annahme mit Vorgabewert, kein Datum"). Ersatz: „Die Datenbank trägt die **Bilanzgrößen** (Flächen, U-Werte, Gesamtkapazität, Nutzung, Sollwerte); neun weitere Größen sind Vorgaben, keine Daten (3.8)." |
| A16 | 10.4 | Die operative Temperatur wird in 4.6 gerechnet und in 8.2 angezeigt, aber nicht exportiert — sie fällt aus dem Regressionsnetz. Ersatz: „Die neuen Reihen `raumtemperatur.csv`, `operative_temperatur.csv` und `kuehlbedarf.csv` exportiert `Ergebnisexport` nur für VDI-6007-Gebäude…" |
| A17 | 7.4 / 7.6 | Die Empfehlung („`Xbim.Ifc4` + `Xbim.IO.MemoryModel`") deckt die in 7.6 versprochene Schemabreite (2x3 / 4 / 4.3) nicht; und der Ausschlusssatz nennt nur LGPL und GPL, obwohl der ara3d-Pfad web-ifc unter **MPL-2.0** (ebenfalls Datei-Copyleft mit Quelltextpflicht) einbindet. Ersatz: „`Xbim.Ifc2x3` + `Xbim.Ifc4` + `Xbim.Ifc4x3` + `Xbim.IO.MemoryModel` (oder das Metapaket `Xbim.Essentials` 6.1.605)" und „…; **MPL-2.0 (web-ifc) ist ebenfalls Datei-Copyleft und damit nur für den optionalen ara3d-Pfad zu prüfen, nicht für den Kern.**" |
| A18 | 7.2 | Zwei Schemaangaben unvollständig: `RefLatitude/RefLongitude` ist `IfcCompoundPlaneAngleMeasure` = **`LIST [3:4] OF INTEGER`** (viertes Glied Millionstel-Sekunden, alle Glieder gleiches Vorzeichen) — „Grad/Minuten/Sekunden-Tupel" führt in den Abbruch; und `IfcZone` erlaubt als `RelatedObjects` laut Schema auch `IfcZone`/`IfcSpatialZone`, Schachtelung ist also zu entschachteln, obwohl der Erläuterungstext „nicht hierarchisch" sagt. |
| A19 | 7.3, 7.6, 7.7 | Drei Quellenangaben ohne Fundstelle: „Automation in Construction 2025" mit 64 %/48 % (weder Autor noch DOI, Werte nicht auffindbar — entweder DOI nachtragen oder die Prozentzahlen streichen); „TABULA/IWU, Zenodo 2025" (Record und Datensatzlizenz eintragen); gbXML „keine auffindbare Lizenzangabe" — das heißt nicht „frei", sondern ungeklärt; vor G5 ist eine schriftliche Nutzungserlaubnis für das XSD einzuholen. Ebenso KIT: die Quelle verlangt die Namensnennung im vorgegebenen Wortlaut bei Veröffentlichungen. |
| A20 | 8.1 / 11 | Der Infobutton der Dialoggruppe kommt mit G1, die Wiki-Seite erst mit G2 — dazwischen führt er ins Leere. Ersatz: „Der Infobutton auf die Wiki-Seite kommt mit G2, zusammen mit der Seite selbst." |
| A21 | 11 / 13 | Die Aufwände werden nicht summiert, obwohl Q15 nach der ersten Beauftragung fragt. Ergänzen: „**Summen: G0+G1 8–14 PT; G0–G2 11–19 PT; G0–G3 19–31 PT; G0–G4 29–56 PT.**" |
| A22 | vor 14 | Es fehlt ein Risikokapitel; Drift 9/10, iOS-Trimming, CDDL-Freigabe, mehrfaches Neu-Einfrieren und die fehlende Validierung an Messwerten stehen verstreut. Kurze Risikotabelle mit Wirkung und Gegenmaßnahme vor die Abgrenzung setzen. |

## Anhang B — Stil und Geschmack (keine sachliche Änderung nötig)

- **B1** Überschrift „Das Ergebnis in acht Sätzen" — es sind acht Punkte aus je zwei bis vier Sätzen. „…in acht Punkten".
- **B2** Anglizismen gegen die Hausregel deutscher Bezeichner: „Gate" (10.4) → „Abnahmesperre", „Setup" (7.4, Q9) → „Installationspaket"; „Repositorium" (Vorspann, 3, 5) und „Repository" (Q12, 14) vereinheitlichen.
- **B3** `Tab_Gebaeude.Rechenmodell` folgt dem zitierten PV-Vorbild nicht (dort `Tab_Energieanlagen.PV_Modell`, Persistenzwert namensgleich mit der Konstante, `DbWerte.cs:2173/2180`). Entweder `Gebaeude_Modell` mit Werten `GEBAEUDE_MODELL_*` wählen oder die Abweichung in 6.4 begründen.
- **B4** 5.10/Q16 nennen dieselbe Katalogtrefferquote dreimal in drei Rundungen; eine Formulierung genügt (siehe Befund 16).

---

**Geprüft und bestätigt:** die Knotenbilanzen 4.2 (alle fünf Gleichungen vorzeichenrichtig), die Eliminationsformel `A = C⁻¹(K·M⁻¹·K − diag(G_1+G_Rest, G_2))` samt nachgerechneten Zeitkonstanten 263,1 h / 5,34 h („264 h und 5,3 h") und dem Verhältnis Nebendiagonale/Diagonale 0,704 („rund 70 %"); R_Rest bleibt für alle Referenzgebäude positiv (10643: 5,29·10⁻⁴ K/W), der R_si-Abzug 0,13 = 1/(2,7+5) passt; die Kusuda-Konstanten (Dämpfung 0,6847, Phasenverzug 22,0 Tage, Erdreich Stuttgart 3,50 … 16,25 °C); F_W = 0,9 und F_r 0,5/1,0 normkonform; C_ges = Bauweise · 3 600 J/Wh und die Umrechnung 20/50/100 Wh/(m²K) → 72/180/360 kJ/(m²K); Tabelle 5.2 gibt alle zwölf Abweichungen der Rohdatei korrekt wieder, ebenso die Reserve 0,007 K bei Testfall 10; Tabelle 5.5 ist durchgängig aus `vergleich_zusammenfassung.csv` reproduzierbar (einschließlich Skalierungsprobe 62 566/13 617 = 4,5947), ebenso 3.4, 3.6, 3.7, die Monats- und Stundensummen, die Empfindlichkeitstabelle 5.9, die Variantenspannen 5.8 je Projekt (+3,2…+7,1 % und −2,3…−14,1 %), die Korrelationen (r Tag 0,982–0,997, r Stunde 0,736–0,925), 5.7 (19,73–21,12 °C, 184–1 425 Stunden > 24 °C, τ 7,5–24,8 h), der Sonderfall 5.11 in allen fünf Zeilen, in 5.12 die Jahresspanne 0,7–2,5 %, Morgen-/Abendsummen und Stunden > 24 °C, sowie 5.13 vollständig (Median 5,09 ms, 9 480 Schritte, 203–826 Umschaltereignisse); die Spanne „7–33 % über dem Tagesmodell" ist **je Projekt** richtig (1007 +17,9, 1017 +33,0, 1018 +33,0, 1023/1024 +25,2, 1039 +19,0, 1040/41/42/45 +7,3; 1008 +89,4 ist der Datenfehler) — der Zusatz „je Projekt" fehlt allerdings in 0.4 und 5.5, wo direkt darunter eine Gebäudetabelle mit −2,1 % steht; „120 bzw. 192 Werte" der Tagverteilung ist durch die Datenprobe gedeckt (120 für `Typ = Wohngebaeude  VDI 2067`, 192 für Wohnblock/Hotel, `daten\probe_log.txt`, `GEBTV`-Zeilen); ferner sämtliche Zeilenbelege in 2.1 zum Rechenweg (`BhkwPlan.cs:51/54/259-289/311-318/342-362/388-436/398/418/420/426-430/433/435`, `SimulationWaermebedarf.cs:128/222/401/405/407/439/566-611/571/601-608/613-656/684-888/748-814/903-911`), `SchemaStand.Zielversion = 76`, `Vergleich.cs:43-44`, `Ergebnisexport.cs:58-67`, `DbWerte.cs:1260-1271` und `:2173/2180`, `Gebaeudebauweise.cs:22-67`, die Schemazitate in `001_grundschema.sql` und `Directory.Packages.props:20`; die Hausregeln der Ablage (Papier in `Dokumentation/aktuell/`, Indexzeile, auflösende Verweise, UTF-8 ohne BOM, durchgehend CRLF) und der Aufbau im Muster der Schwesterpapiere; auf der IFC-Seite `Xbim.Essentials`/`Xbim.Ifc4` 6.1.605 unter CDDL-1.0 mit net10.0, die generierte Entity-Factory und das Trimming-Risiko über `ExpressMetaData`, GeometryGymIFC_Core 26.8.17 MIT, Hypar.IFC4 seit 2023 ungepflegt, IfcOpenShell LGPL-3.0, BIMserver AGPL-3.0, gbXML 8.01 (Januar 2026), Design Transfer View = Entwurf, Space Boundary Addon View = IFC2x3-Anhang, `Pset_SpaceThermalRequirements` in IFC 4.3 entfallen, `YearOfConstruction` als `IfcLabel`, `TrueNorth` optional, `SolarHeatGainTransmittance` = g-Wert, ISO 16739-1:2024 = IFC 4.3.2.

**Anzahl: 11 hoch / 14 mittel / 26 niedrig.**
# Gegenlesen des Konzepts — Blickwinkel Leser und Entscheider (15.09.2026)

**Protokoll.** Einzelbefund des Workflows ‚konzept-gegenlesen‘ (Modell Opus) im Auftrag des Konzepts [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Sitzung vom 15.09.2026. Wortlaut wie geliefert; Zeilenbelege und Zahlen sind im Konzept nach dem Gegenlesen korrigiert, bei Widerspruch gilt das Konzept. Pfade unter `C:\Users\…pos-spike\` bezeichnen die Prüfwerkzeuge außerhalb des Repositoriums.

---

Ich habe das Papier vollständig gelesen und seine Code-, Datei- und Rechenbelege gegen den Zweig `ios_migration_september` geprüft.

**Befunde (wichtigste zuerst)**

**1. Q7 ist mit den Angaben des Papiers nicht entscheidbar — `Waermelast_Max` ist keine Gebäudegröße**
(a) 13, Q7: „**`Waermelast_Max` = Spitze als gleitendes Tagesmittel**, die Stundenspitze und das 95-%-Quantil daneben ausgewiesen"
(b) `Waermelast_Max` ist im Bestand das Maximum des **Summenvektors aller drei Kanäle über das ganze Projekt** und normiert zugleich die Dauerlinie; ein gebäudeweiser Tagesmittelwert kann dort gar nicht eingesetzt werden, ohne die Dauerlinie und die Deckungsrechnung von der Anzeige zu entkoppeln.
(c) `C:\Waermeplan\EPOS-Plan\EPOS.Kern\Allgemein\Simulation\SimulationWaermebedarf.cs:401` (`Waermebedarf_Max = Maximaler_Waermebedarf(Waermebedarf)` über den Kanalsummenvektor), `:405` und `:407` (`Normieren(Waermebedarf_sortiert, Waermebedarf_Max)`, `Normieren(Dauerlinie_nicht_sortiert, …)`), `EPOS.Kern\Allgemein\Simulation\SimulationRunner.cs:358`, `EPOS.Kern\Allgemein\Bericht\Bausteine\BausteineProjekt.cs:68` („Wärmelast max.")
(d) hoch
(e) Ersatz für die Q7-Empfehlung: „**Option ja, NULL = unbegrenzt. `Waermelast_Max` bleibt unverändert das Maximum des Kanalsummenvektors** (`SimulationWaermebedarf.cs:401`), damit Dauerlinie, Deckung und Anzeige eine Basis behalten. Die drei Gebäudekennzahlen (Stundenspitze, gleitendes Tagesmittel, 95-%-Quantil) werden **zusätzlich je Gebäude** ausgewiesen und im Bericht neben `Waermelast_Max` gestellt. Wer die Aufheizspitze nicht auslegen will, setzt `Heizleistung_Max` — das ist der physikalische Weg, nicht die Mittelung der Kennzahl."

**2. Die drei Datenbefunde aus der Kurzfassung haben keine Stufe, keinen Aufwand und keinen Platz in der Reihenfolge**
(a) 0.6: „**Drei Datenbefunde sind unabhängig vom Modell zu beheben:** ein Gebäude der Testdatenbank trägt den stillen Rückfallwert `Bauweise = 50 Wh/K` …, der Rechenweg teilt ungeschützt durch `Wohnflaeche`, und der Raumtemperatur-Zustand des Bestands ist ein statisches Feld"
(b) Von den drei Befunden taucht nur die `Bauweise`-Korrektur in Stufe G1 (Kapitel 11) und in Kapitel 15 auf; die fehlende Nullprüfung und der statische Zustand (Q23, Empfehlung „Ja") kommen in Stufenplan, Aufwand und Reihenfolge nirgends vor — der Entscheider beauftragt sie mit „Ja zu Q23" ins Leere.
(c) Papier Kapitel 11 (Tabelle Stufen, G0–G5) und Kapitel 15 (Punkte 1–4) gegen Q23; Bestandsbelege bestätigt: `EPOS.Kern\Allgemein\BhkwPlan.cs:51` (`private static double _prevRoomTemp`), `:54` (`ResetState`, im Produktionsweg nicht gerufen), `:435` (`return acc * gesamtflaeche / wohnflaeche`), `EPOS.Kern\Allgemein\Simulation\SimulationWaermebedarf.cs:571`
(d) hoch
(e) In Kapitel 11 eine Zeile ergänzen: „**GB — Bestandsbefunde** | Nullprüfung in `TaeglHeizlastWG` und `HeizwaermeEinesGebaeudes` (benannte Fehler statt NaN), `_prevRoomTemp` als Instanzzustand mit `ResetState` je Gebäude, `Bauweise`-Korrektur 10576 | Referenzlauf ändert sich in 1008 und 1039 — eigener, begründeter Einfrierschritt | klein, 1–2 PT"; in Kapitel 15 als Punkt 5 einreihen.

**3. Das positive Abnahmekriterium für G1 ist so nicht erfüllbar**
(a) 10.4: „(2) der Kern reproduziert die Zahlen des Prototyps aus Kapitel 5 auf den Referenzprojekten bei gleicher Parametrierung innerhalb 1e‑6 relativ"
(b) Der Prototyp lief in UTC-Speicherreihenfolge und mit den isotropen `Sol_*`-Spalten, der Kern soll nach 4.4/Q20/Q21 in Ortszeit und mit Hay-Davies rechnen — die 1e‑6-Gleichheit ist nur erreichbar, wenn der Kern einen Vergleichsmodus (UTC + isotrop) behält, den das Papier nirgends fordert.
(c) Papier 5.4: „die Fassadenstrahlung kam aus den isotropen `Sol_*`-Spalten — der Hay-Davies-Weg aus 4.4 ist im Prototyp **nicht** gemessen" und „Zeitreihen in Speicherreihenfolge (UTC) für **beide** Modelle"; dagegen 4.4 (Ortszeit), Q20, Q21
(d) hoch
(e) „(2) der Kern reproduziert die Zahlen des Prototyps aus Kapitel 5 innerhalb 1e‑6 relativ **in einem Prüfmodus mit den Randbedingungen des Prototyps (UTC-Reihenfolge, isotrope `Sol_*`-Spalten, kein Absorptionsterm)**; der Unterschied zwischen diesem Prüfmodus und der Auslieferungsparametrierung (Ortszeit, Hay-Davies) wird je Referenzprojekt als Zahl ausgewiesen und begründet."

**4. Die Textwert-Zählungen in 3.1 passen nicht zu den 15 Gebäudezeilen**
(a) 3.1: „Textwerte: `Typ` = „Wohngebaeude  VDI 2067" (19×), „Wohnblock" (5), „Hotel" (2); `Baualtersklasse` A (17), F (4), H (2), G (2), D (1)"
(b) Alle drei Zählungen summieren auf 26, der Absatz spricht aber von 15 Gebäudezeilen der dreizehn Referenzprojekte — die Zahlen stammen erkennbar aus einer anderen Grundgesamtheit (vermutlich `Tab_Gebaeude` über alle 25 Projekte oder inklusive `_STAMM`).
(c) Papier 3.1 (19+5+2 = 26; 17+4+2+2+1 = 26; 17+5+2+2 = 26) gegen 3.1 („15 Gebäudezeilen") und 3.2 („Vollständigkeit (15 Zeilen)", alle Angaben x/15)
(d) hoch (sachlich falsch)
(e) „Textwerte **in `Tab_Gebaeude` über alle 25 Projekte der Testdatenbank (26 Zeilen)**: `Typ` … . **In den 15 Zeilen der dreizehn Referenzprojekte** verteilen sich die Werte wie folgt: …" — oder die Zählungen auf die 15 Zeilen neu erheben.

**5. „Wird im Kern nirgends gelesen" ist für `Wohngebaeude_Nicht_Wohngebaeude` falsch**
(a) 2.1: „die Weiche ist allein `Typ == "Wohngebaeude  VDI 2067"` (zwei Leerzeichen, `SimulationWaermebedarf.cs:601-608`), das Feld `Wohngebaeude_Nicht_Wohngebaeude` wird im Kern nirgends gelesen."
(b) Das Feld wird im Kern sehr wohl gelesen — es ist der Standardfilter der Kataloglese und trägt die Katalogtrennung Wohn/Nichtwohn; nur der **Rechenweg** wertet es nicht aus.
(c) `EPOS.Kern\Controller\GebaeudeCtrl.cs:23` (`ReadAll(string szFilter = "Wohngebaeude_Nicht_Wohngebaeude='Wohngebaeude'")`), `EPOS.Kern\Controller\GebaeudeStammCtrl.cs:83-84` (`FILTER_WOHNGEBAEUDE`, `FILTER_NICHT_WOHNGEBAEUDE`), `EPOS.Kern\Controller\ProjektGebaeudeCtrl.cs:98`; die Weiche auf `Typ` ist bestätigt (`SimulationWaermebedarf.cs:601`)
(d) hoch (sachlich falsch; ein Leser könnte das Feld für tot halten und beim IFC-Weg 7.6 fallen lassen)
(e) „… die Weiche ist allein `Typ == "Wohngebaeude  VDI 2067"` (zwei Leerzeichen, `SimulationWaermebedarf.cs:601-608`); das Feld `Wohngebaeude_Nicht_Wohngebaeude` steuert nur die **Kataloglese** (`GebaeudeCtrl.cs:23`, `GebaeudeStammCtrl.cs:83-84`) und geht in den Rechenweg nicht ein."

**6. Die Kurzfassung verschweigt den einzigen offenen Punkt des Lösers**
(a) 0.2: „**Der neu geschriebene Löser besteht alle zwölf Normtestfälle** (Abweichung 0,05–0,14 K bzw. 0,6–1,5 W …)"
(b) Nach 5.2 besteht Testfall 11 nur mit Nachbildung des 120-s-Messfensters von AixLib, die Testfälle 6, 9 und 10 nur knapp (0,01 K bzw. 0,001 W Reserve), und der Drift in 9/10 ist ausdrücklich „der empfindlichste Punkt für den Einsatz mit realen Wetterdaten" — in der Kurzfassung, die der Entscheider liest, steht davon nichts.
(c) Papier 5.2 (Fußnote 1: „ohne sie 70 von 72 Punkten"; „die Reserve zur Schwelle ist nur 0,01 K"), Kapitel 11 G0 („Klärung des Drifts in Testfall 9/10")
(d) hoch (irreführend für die Beauftragung von G0)
(e) „**Der neu geschriebene Löser besteht alle zwölf Normtestfälle** (0,05–0,14 K bzw. 0,6–1,5 W). Drei Fälle liegen knapp: Testfall 11 nur mit Nachbildung des AixLib-Messfensters, 9 und 10 mit 0,01 K Reserve. Das Verhalten mit realen Wetterdaten wird in G0 vor der Übernahme geklärt — das ist der einzige offene technische Punkt des Vorschlags."

**7. Die Angabe „zwei Drittel" ist aus 5.10 nicht ableitbar**
(a) 0.4: „Er liegt 7–33 % über dem Tagesmodell; zwei Drittel davon sind versteckte Kalibrierfaktoren 0,83/0,95/0,45 im Bestand und der rohe g-Wert"
(b) Nach 5.10 wirkt die Parametrierung mit **+20,7 bis +41,6 Prozentpunkten** und die Modellstruktur mit −5,1 bis −13,4 — die Parametrierung trägt also nicht zwei Drittel, sondern 61–88 % der Beträge und in jedem Projekt **mehr als 100 %** der Nettoabweichung (1007: +16,1 + 10,9 − 9,1 = +17,9).
(c) Papier 5.10, Tabellenzeilen 1007 bis 1040–1045 (Spalten addieren sich exakt auf die Gesamtabweichung); dieselbe Aussage nochmals in Kapitel 12 („davon zwei Drittel Parametrierung")
(d) mittel
(e) „Er liegt je Projekt 7–33 % über dem Tagesmodell. Die Parametrierung (Gewichte 0,83/0,95/0,45 und roher g-Wert) treibt um +21 bis +42 Prozentpunkte nach oben, die Modellstruktur um 5 bis 13 Prozentpunkte nach unten; der Rest ist die Nettoabweichung. Die Abweichung ist also überwiegend Parametrierung, nicht Physik."

**8. Die Projektspanne „1040–1045" nennt zwei Projekte, die es nicht gibt**
(a) 3.6: „| 1040–1045 | 10645/46/47/51 | 201 | 201 | 1 |"
(b) Die Basis enthält die Projekte 1040, 1041, 1042 und 1045 — 1043 und 1044 gibt es nicht; die Schreibweise „1040–1045" (3.1, 3.6, 5.5, 5.6, 5.10, 5.12) legt sechs Projekte nahe und widerspricht der eigenen Tabelle 3.7.
(c) `C:\Waermeplan\EPOS-Plan\Referenzlaeufe\2026-09-11_R7_Speicherflotte\protokoll.txt:7` („Projekte: 1007, 1008, 1017, 1018, 1023, 1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046") und die Ordnerliste desselben Verzeichnisses; Papier 3.7 nennt korrekt „1040, 1041, 1042, 1045"
(d) mittel
(e) Überall „1040–1045" durch „**1040/1041/1042/1045**" ersetzen.

**9. „Zehn von zwölf" widerspricht der eigenen Tabelle 5.6**
(a) 5.6: „In zehn von zwölf Projekten liegt die Prototyp-Spitze auf demselben Index" (gleichlautend 4.5: „fällt die Jahresspitze in 10 von 12 Referenzprojekten auf dieselbe Stunde")
(b) Die Tabelle darüber zeigt Index 1 398 für 1007, 1046, 1008, 1017, 1023, 1024, 1039, 1040, 1041, 1042 und 1045 — das sind elf der zwölf Projekte mit Gebäude, einzig 1018 liegt bei 438; die Zahl 10 ist nicht nachvollziehbar.
(c) Papier 5.6, Tabelle (Zeilen „1007, 1046", „1008", „1017", „1023, 1024", „1039", „1040–1045" alle mit Index 1 398; „1018" mit 438), abgeglichen mit der Projektliste aus `Referenzlaeufe\2026-09-11_R7_Speicherflotte\protokoll.txt:7`
(d) mittel
(e) „In **elf von zwölf** Projekten liegt die Prototyp-Spitze auf demselben Index 1 398; allein 1018 fällt heraus (Index 438)." — oder benennen, warum 1041 (Mischfall) nicht mitzählt.

**10. „7–33 % über dem Tagesmodell" gilt nicht je Gebäude**
(a) 0.4: „Er liegt 7–33 % über dem Tagesmodell"
(b) Die Spanne gilt je **Projekt**; in der Gebäudetabelle 5.5 liegt Gebäude 10643 mit −2,1 % **unter** dem Tagesmodell und 10576 mit +108,5 % weit darüber — die Kurzfassung sagt nicht, worauf sich die Spanne bezieht.
(c) Papier 5.5, Zeilen „1039 | 10643 | 201 | 77 705 | 79 373 | −2,1 %" und „1008 | 10576 | 800 | 90 216 | 43 267 | **+108,5 %**"
(d) mittel
(e) „Er liegt **je Projekt** 7–33 % über dem Tagesmodell (Projekt 1008 ausgenommen, dort wirkt der Datenfehler 10576); **je Gebäude** reicht die Spanne von −2 % bis +33 %."

**11. Das Ost/West-Verhältnis widerspricht der eigenen Klimaprüfung**
(a) 5.12: „(Ost liegt in der Jahressumme 6,8–8,5 % über West, Tagesmaximum Ost bei Index 8, West bei Index 14)"
(b) Nach 3.5 liegt Ost mit 777 gegen 711 kWh/(m²a) bzw. 88,7 gegen 81,1 W/m² um **9,3 %** über West — die in 5.12 genannte Spanne 6,8–8,5 % schließt diesen Wert nicht ein.
(c) Papier 3.5: „die isotropen Fassadenwerte Süd 935, Ost 777, West 711, Nord 432 kWh/(m²a)" und „(Jahresmittel 88,7 gegen 81,1 W/m²)"
(d) mittel
(e) Entweder die Bezugsgröße nennen („… über West, gemessen an der **Summe der Fensterstrahlung nach F_F·F_S·F_W**, während die rohe Fassadenstrahlung 9,3 % Unterschied zeigt (3.5)") oder die Zahl auf 9,3 % berichtigen.

**12. „Sieben verschiedene Katalogbauten" passt nicht zu den neun Zeilen in 3.4**
(a) 3.1: „**15 Gebäudezeilen**, effektiv **sieben verschiedene Katalogbauten** (1007/1046, 1023/1024/1039 und 1040–1045 teilen Zeilen)"
(b) Die Plausibilitätstabelle 3.4 führt neun unterschiedliche Gebäude-IDs mit acht unterschiedlichen Parametersätzen auf — „sieben" zählt offenbar Projektgruppen, nicht Katalogbauten, und trägt denselben Namen wie eine andere Größe.
(c) Papier 3.4 (Zeilen 10614, 10576, 10577, 10599, 10632, 10628, 10642, 10643, 10645 = neun; 10614 und 10577 sind parametergleich) gegen 3.1
(d) mittel
(e) „**15 Gebäudezeilen** auf **neun verschiedenen Katalogbauten** (davon zwei parametergleich, also acht Parametersätze); die Projekte 1007/1046, 1023/1024 und 1040/1041/1042/1045 teilen sich Gebäudezeilen."

**13. Stufe G2 braucht Datenbankspalten, die das Datenmodell nicht vorsieht**
(a) 11, Zeile G2: „… Vergleich Tagesbilanz/VDI 6007 im Bedarfsdialog, **Sommerlüftungsregel und Infiltration/Nutzerlüftung**, Wiki-Seite"
(b) Infiltration (0,3 1/h), Nutzerlüftung (0,4 1/h) und die Schwellen der Sommerlüftungsregel sind in 4.4 als Vorgaben genannt, kommen aber in Kapitel 6 (Schemaschritt 77) nicht vor und haben keinen eigenen Schemaschritt — G2 ist so nicht beauftragbar.
(c) Papier 4.4 („G2: Trennung Infiltration (Vorgabe 0,3 1/h) und Nutzerlüftung (0,4 1/h) sowie eine **Sommerlüftungsregel** (n auf 2,0 1/h, wenn θ_air > 23 °C und θ_out < θ_air − 2 K)") gegen 6.1 (elf Spalten, keine davon Lüftung) und 12 („elf Spalten in zwei Tabellen (G1); drei Tabellen (G3)")
(d) mittel
(e) In 6.1 ergänzen: „Mit G2 kommen drei weitere Spalten in **Schemaschritt 78**: `Luftwechsel_Infiltration` (REAL 1/h, NULL = 0,3), `Luftwechsel_Nutzer` (REAL 1/h, NULL = 0,4), `Sommerlueftung` (INTEGER 0/1, NULL = 0)." und Kapitel 12 entsprechend („elf Spalten G1, drei Spalten G2").

**14. Die Wochentagsregel des neuen Modells ist unbestimmt**
(a) 4.4: „das Gebäudemodell rechnet in **einer** Zeitbasis (Ortszeit …) und braucht `Tab_Klimadaten` nicht mehr; Wochenendtage folgen aus dem Wochentag des 1. Januar."
(b) Das Papier sagt nicht, welcher Wochentag das ist bzw. woher er kommt; der Bestand leitet `WE` aus dem Datum des TMY-Satzes ab, das neue Modell braucht eine festgeschriebene Regel, sonst verschieben sich Sollwertfahrplan und Vergleichbarkeit stillschweigend.
(c) `EPOS.Kern\Allgemein\Import\KlimaImportAblauf.cs:354` (`t.WE = datum.DayOfWeek == DayOfWeek.Saturday || datum.DayOfWeek == DayOfWeek.Sunday;`), Papier 3.3 („`WE` 0/1") und 3.2 („Wochenend-/Ferien-Sollwerte … 0/15 wirksam")
(d) mittel (unbelegt/unklar)
(e) „… und braucht `Tab_Klimadaten` nicht mehr. Die Wochentage übernimmt das Modell aus **derselben Datumsregel wie der Import** (`KlimaImportAblauf.cs:354`), also aus dem Kalenderjahr des TMY-Satzes; das hält Bestands- und VDI-6007-Weg auf demselben Wochentagsraster. In den Referenzprojekten ist die Wochenendabsenkung ohnehin unwirksam (3.2)."

**15. Die Klemme in 4.3 ist genau der stille Rückfall, den das Papier an anderer Stelle verbietet**
(a) 4.3: „R_Rest,AW = 1 / Σ(U·A)_opak − R_1,AW − R_si / A_AW,opak  (… **Klemme ≥ 1e‑6 K/W**)"
(b) Wird der Ausdruck negativ (sehr hohe U-Werte), rutscht R_Rest auf 1e‑6 und der Massenknoten läuft praktisch auf θ_eq — die Rechnung liefert dann eine Zahl statt einer Meldung, obwohl 4.8 und 0.6 gerade „harte Prüfungen … kein stiller Rückfall" fordern.
(c) Papier 4.8 („Harte Prüfungen vor der Rechnung (benannte Fehler, kein stiller Rückfall)") und 0.6 („der stille Rückfallwert `Bauweise = 50 Wh/K`"); Bestandsbeleg für die Gefahr: `EPOS.Kern\Allgemein\Gebaeudebauweise.cs:66` (`return 50;`)
(d) mittel
(e) „… (R_si = 0,13 m²K/W …). **Wird der Ausdruck ≤ 0 — rechnerisch ab U > 4,2 W/(m²K) —, bricht die Rechnung mit benanntem Fehler ab; es gibt keine Klemme.** Die U-Wert-Prüfung aus 4.8 (0,1–6 W/(m²K)) fängt den Fall bereits vorher ab."

**16. Die Kurzfassung bewertet die Datenlage freundlicher als Kapitel 3**
(a) 0.3: „Die Datenbank trägt fast alle Eingaben eines VDI-6007-Modells"
(b) Nach 3.8 sind neun Größen — Kapazitätsaufteilung, Innenbauteilfläche, R_1 aus U-Wert, Ost/West-Aufteilung, Rahmen-, Verschattungs- und Winkelfaktor, Absorptionsgrad, Randbedingung der Grundfläche, Lüftungstrennung — „eine Annahme mit Vorgabewert, kein Datum"; „fast alle" trägt das nicht.
(c) Papier 3.8: „**Gebäudedaten: ausreichend zum Start, nicht zum Abschluss.**"
(d) mittel
(e) „Die Datenbank trägt die **Bilanzgrößen** eines VDI-6007-Modells (Flächen, U-Werte, Gesamtkapazität, Nutzung, Sollwerte); neun weitere Größen sind Vorgaben, keine Daten (3.8). Die Klimadaten liegen stündlich und lückenlos vor."

**17. Die Begründung des 30-Tage-Vorlaufs stützt sich auf eine Zahl, die im Papier nicht steht**
(a) 4.6: „**Vorlauf:** 30 Tage … Zeitkonstanten schwerer Bauweisen liegen bei 1–2 Tagen (5.7), der Vorlauf ist ausreichend und deterministisch."
(b) 5.7 misst Zeitkonstanten von 7,5–24,8 h, also 0,3–1,0 Tage; zugleich nennt 4.2 für Testfall 1 eine Zeitkonstante von 264 h (11 Tage), bei der 30 Tage weniger als drei Zeitkonstanten wären — der Verweis „(5.7)" belegt die Aussage nicht.
(c) Papier 5.7 („Zeitkonstanten C/H 7,5–24,8 h, bei 10576 0,09 h"), Papier 4.2 („Testfall 1: Zeitkonstanten 264 h und 5,3 h")
(d) mittel
(e) „**Vorlauf:** 30 Tage …, statt der 15 Tage des Bestands. Die gemessenen Zeitkonstanten der Referenzgebäude liegen bei 7,5–24,8 h (5.7); 30 Tage sind damit mehr als das Zwanzigfache und decken auch die langsameren Normtesträume (bis 264 h, 4.2) auf unter 0,1 K ab. Die Zahl wird in G0 an einem Testraum mit 264 h nachgewiesen."

**18. Es fehlt ein Risikoteil**
(a) 14: „## 14. Abgrenzung — was dieses Papier nicht behandelt"
(b) Das Papier hat Abgrenzung (14), Aufwand (11) und Fragen (13), aber kein Kapitel, das die Risiken bündelt — Drift in Testfall 9/10, iOS-Trimming bei xBIM, CDDL-Freigabe, zweifaches Neu-Einfrieren der Referenzbasis und die fehlende Validierung an gemessenen Verbräuchen stehen verstreut, und der Entscheider muss sie sich zusammensuchen.
(c) Papier 5.2 (Drift 9/10), 7.4 („iOS-Risiko ist das Trimming"), 7.4 („falls CDDL nicht freigegeben wird"), 10.4 und Q22/Q23 (Einfrieren), 5.14 („Belastbar gegen die Wirklichkeit validieren ließe sich EPOS erst an gemessenen Verbräuchen")
(d) mittel
(e) Neues Kapitel vor der Abgrenzung: „## 14. Risiken | Risiko | Wirkung | Gegenmaßnahme | Drift Testfall 9/10 (0,01 K Reserve) | Löser unbrauchbar für reale Wetterdaten | in G0 vor der Anbindung klären, sonst kein G1 | CDDL-1.0 nicht freigegeben | IFC-Weg neu | GeometryGymIFC (MIT) als Ausweg (7.4) | iOS-Trimming von xBIM | G4 nicht plattformfrei | TrimmerRootDescriptor, Nachweis früh im iOS-Lauf | Referenzbasis dreimal neu eingefroren (Q14, Q22, Q23) | Regressionsnetz zeitweise blind | jeder Schritt einzeln begründet (Einfrierregel) | keine gemessenen Verbräuche | Modell nur gegen Norm, nicht gegen Wirklichkeit geprüft | positives Kriterium 10.4, Messdaten aus der Praxis nachziehen |"

**19. Die operative Temperatur fällt aus dem Regressionsnetz**
(a) 10.4: „Die neuen Reihen `raumtemperatur.csv` und `kuehlbedarf.csv` exportiert `Ergebnisexport` nur für VDI-6007-Gebäude, damit die Bestandsordner byte-gleich bleiben."
(b) 4.6 erzeugt vier Reihen (`Heizlast`, `Raumtemperatur`, `OperativeTemperatur`, `Kuehlbedarf`) und 8.2 zeigt die operative Temperatur im Dialog, aber der Referenzlauf exportiert sie nicht — eine Größe, die der Anwender sieht, ist damit unbewacht.
(c) Papier 4.6 („`OperativeTemperatur[8760]`") und 8.2 („Raumtemperatur-Jahresverlauf (Luft und operativ …)"); Exportmuster `C:\Waermeplan\EPOS-Plan\Referenzlauf\Ergebnisexport.cs:58-67`
(d) niedrig
(e) „Die neuen Reihen `raumtemperatur.csv`, `operative_temperatur.csv` und `kuehlbedarf.csv` exportiert `Ergebnisexport` nur für VDI-6007-Gebäude …"

**20. Der Infobutton in G1 zeigt auf eine Seite, die es erst in G2 gibt**
(a) 8.1: „… mit Vorgabe-Anzeige („Vorgabe 0,3"), **Infobutton auf die Wiki-Seite**."
(b) Die Dialoggruppe kommt nach Kapitel 11 mit G1, die Wiki-Seite „Gebäudemodell VDI 6007" erst mit G2 — zwischen beiden Stufen führt der Knopf ins Leere.
(c) Papier 11, G1 („Dialoggruppe „Rechenmodell"") gegen 11, G2 („Wiki-Seite") und Kapitel 9 („Wiki: neue Seite „Gebäudemodell VDI 6007"")
(d) niedrig
(e) „… mit Vorgabe-Anzeige („Vorgabe 0,3"). Der Infobutton auf die Wiki-Seite kommt mit G2, zusammen mit der Seite selbst."

**21. Zwei Zeilenbelege zeigen auf die falsche Datei bzw. die Nachbarzeile**
(a) 2.3: „Daneben gibt es `Sonnengeometrie` (`:353-384`) und **Hay-Davies** (`CalculateHourlyHayDavies`, `:455-482`); Perez nicht."
(b) Beide Fundstellen liegen in `SolarPVGISCalculator.cs`, nicht in der zuvor genannten Datei `KlimaImportAblauf.cs`, und der Beleg „`:221`" für die W→kW-Wandlung zeigt auf eine Kommentarzeile statt auf den Aufruf.
(c) `EPOS.Kern\Allgemein\SolarPVGISCalculator.cs:353` (`private static Sonnenstand Sonnengeometrie`), `:455` (`public static double CalculateHourlyHayDavies`); `EPOS.Kern\Allgemein\Simulation\SimulationWaermebedarf.cs:222` (`WPPlan.Core.BhkwPlan.WattToKw(kanalHeizung);`)
(d) niedrig
(e) „Daneben gibt es `SolarPVGISCalculator.Sonnengeometrie` (`SolarPVGISCalculator.cs:353-384`) und **Hay-Davies** (`CalculateHourlyHayDavies`, `SolarPVGISCalculator.cs:455-482`); Perez nicht." sowie in 2.1 „einmal nach kW gebracht (`:222`)".

**22. „In acht Sätzen" sind acht Absätze**
(a) 0: „## 0. Das Ergebnis in acht Sätzen"
(b) Die acht Punkte bestehen aus zwei bis vier Sätzen, mehrere mit eingeklammerten Zahlenreihen — die Überschrift verspricht eine Kürze, die der Abschnitt nicht hält.
(c) Papier 0.4 (vier Sätze, drei Zahlenspannen) und 0.6 (drei Befunde in einem Satzgefüge)
(d) niedrig
(e) „## 0. Das Ergebnis in acht Punkten"

**23. Anglizismen und eine uneinheitliche Schreibung**
(a) 10.4: „**Bestandsprojekte:** unverändert gegen `2026-09-11_R7_Speicherflotte` — das **Gate** von G1."
(b) „Gate" (10.4), „Setup" (7.4, Q9) und der Wechsel zwischen „Repositorium" (Vorspann, 3, 5) und „Repository" (Q12, 14) brechen mit der Hausregel deutscher Bezeichner.
(c) Papier Vorspann („außerhalb des Repositoriums"), Q12 („als Importprobe ins Repository"), 14 („dafür fehlen Daten im Repository"), 7.4 („der Lizenztext wird mit ausgeliefert (Setup: Lizenzhinweise)")
(d) niedrig
(e) „— die **Abnahmesperre** von G1."; „Setup" → „Installationspaket"; durchgehend „Repositorium".

**24. Der Aufwand wird nicht summiert**
(a) 11: „Aufwände sind Größenordnungen für Entwicklung und Nachweis; Agentenarbeit verkürzt die Kalenderzeit, nicht die Prüfzeit."
(b) Der Entscheider muss die Stufen selbst addieren, obwohl Q15 genau nach der ersten Beauftragung (G0 + G1) fragt.
(c) Papier 11 (G0 2–4, G1 6–10, G2 3–5, G3 8–12, G4 10–20 (+5), G5 30–60 + gbXML 10–15 PT) gegen Q15
(d) niedrig
(e) Ergänzen: „**Summen: erste Beauftragung G0+G1 8–14 PT; nutzbares Modell G0–G2 11–19 PT; mit Bauteilkatalog G0–G3 19–31 PT; mit IFC G0–G4 29–56 PT.** Aufwände sind Größenordnungen …"

**Geprüft und bestätigt:** sämtliche Zeilenbelege in 2.1 zum Rechenweg (`BhkwPlan.cs:51/54/311-318/342-362/347-353/355-359/388-436/418/420/426/428/430/433/435`, `SimulationWaermebedarf.cs:128/566/601/571/641/748-814/903-911`), der Faktor 100 der drei Physikfunktionen wird an allen vier Aufrufstellen wieder herausgeteilt, die Weiche auf `Typ == "Wohngebaeude  VDI 2067"` mit zwei Leerzeichen, die Namensfalle `Fensterflaeche_Ost_West` → `Fensterflaeche_Ost` (`001_grundschema.sql:1144`, `ProjektGebaeudeModel.cs:26`, `GebaeudeCtrl.cs:66`, Aufruf `:826`), der Vorlauf über 15 Tage (350–364), der Indexleser `row[0]…row[57]` (`ProjektGebaeudeCtrl.cs:42/99`), die Rückfallwerte in `Gebaeudebauweise.cs:44/66` und die Klassenwerte 20/50/100, Schemastand 76 (`SchemaStand.cs:93`), die Schemazitate `001_grundschema.sql:850-863/1131-1188/2153-2182/2873-2881`, die Referenzlauf-Toleranzen (`Vergleich.cs:43-44`: 1e‑4 / 0,01) und die Exportliste (`Ergebnisexport.cs:58-67`, `waermebedarf_gebaeude.csv` auf :59), `DbWerte.cs:1260-1271` (drei Kanäle, kein Kühlkanal) und `:2173/2180` (PV-Modellwerte), `SimulationPV.cs:700-709` als Muster der Textweiche, `Microsoft.Data.Sqlite` 10.0.11 (`Directory.Packages.props:20`), `GebaeudeBedarfCtrl.Rechnen` auf `:94`, `Kanal`-Konstanten in `SimulationKanaele.cs:429/435`, `SolarPVGISCalculator.cs:100` (PVGIS-TMY) und die Tagesmittelbildung mit `Sonnenwinkel` als Tagesmaximum (`:484-503`), `KlimaImportAblauf.cs:132-136` (Neigung 90°, Azimute 0/−90/180/90) und `:354-355` (`WE`, `TagTyp_W`), `KlimadatenCtrl.cs:37` (`ORDER BY ID`), `SolardatenCtrl.ReadOrtszeit` auf `:156`, die Laufzeit von 4 s für dreizehn Projekte (`Referenzlaeufe\2026-09-11_R7_Speicherflotte\protokoll.txt:8` und `:390`), Projekt 1007 = „Laurentiuskirche" (ebd. Ablaufteil), sowie die Existenz aller verlinkten Dokumente, Dialoge und Hüllen (PV-Konzept, überholtes Wärmebedarfspapier, `Doku_Simulationsergebnis_Darstellung.md`, `ADR-001`, `Glossar_Lokalisierung.md`, `Referenzlaeufe/LIESMICH.md`, `Referenzlaeufe/Importproben/`, `PvErweitertesModell.cs`, `GebaeudeHuelle.cs`, `SchemaKatalog.cs`, `SchemaMigration.cs`); ferner die innere Arithmetik der Tabellen 3.4, 3.6, 3.7, 5.5 und 5.10 (spezifische Bauweise, Skalierungsfaktoren, kWh/m²a, additive Zerlegung), der Stationärwert 0,9 °C in 1.2 und die Umrechnung 20/50/100 Wh/(m²K) → 72/180/360 kJ/(m²K) in 4.3; Füllwörter im klassischen Sinn („eigentlich", „grundsätzlich", „im Wesentlichen") kommen im Papier nicht vor.
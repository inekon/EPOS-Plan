# Konzept: Konditionierungsprofile — Kalender, Voreinstellungen und Aufheizoptimierung

> **Rev. 3 mit den Entscheiden vom 26.09.2026 (E52, E53).** Der Anwender hat die fünf Fragen der Rev. 2 entschieden und
> die Heizperiode festgelegt (E53, Leitkonzept N1.60): Die Nachtauskühlung wirkt bedingt (P9), die neuen Zellen der
> Matrix stehen in einer eigenen Vorgabetabelle (P10), **Vorlagen gibt es je Größe** — je Größe eine Liste
> vorbefüllter Kalender, abweichend von der Empfehlung (P11) —, „Matrix erneut anwenden" ersetzt nur den Matrixbereich
> (P12), und `Kuehl_Sollwert_Nacht` ist die Zelle Kühlen/Nacht (P13). Die **Heizperiode** hat ein Datum für Start und
> Ende; außerhalb steht die Raumheizung auf „aus", der Wärmeerzeuger liefert dann nur Warmwasser und Prozesswärme.
> Rev. 3 fasst 3.5, 5.7, 7.4 und 9.3 neu und schreibt 0 bis 2, 3.2, 3.3, 3.6, 3.7, 4.5, 4.7 bis 4.9, 5, 6, 7, 8 und 10
> bis 12 fort. **Rev. 2** hat nach der Ergänzung des Auftrags am selben Tag Vorgabe-Matrix, Vorlagen und
> Nachtauskühlung eingeführt, **Rev. 1** zwei Entwürfe nach einer Gegenprüfung zusammengeführt (Entscheide E52,
> Leitkonzept N1.59). Nachgetragen in Rev. 3: der Beleg der Sitzung „Dialoge und Korrekturen" (#571) zur heutigen
> Semantik der Sollwerte (3.3, 7.2, 10.4, 12). Entwürfe, Gegenprüfung und Prüfskripte liegen im Arbeitsordner der
> Sitzung, nicht im Repositorium.

> **Fortgeschrieben am 27.09.2026 mit E54** (Leitkonzept N1.62, 9.4): „Als Vorlage speichern" nimmt weder Nennwert
> noch Saison mit — eine Vorlage trägt nur die Nutzungszeilen ihrer Spalte (3.5, 5.7) —, und KP3 bringt die
> Nachtauskühl- und die Sommerlüftungsstunden in Bericht, Export, KI-Sicht und Variantenvergleich (3.7, 8). Der Entwurf
> der zweiten Hälfte von KP1 steht unter [`ueberholt/Entwurf_KP1b_Konditionierungsprofile.md`](../ueberholt/Entwurf_KP1b_Konditionierungsprofile.md).

> **Fortgeschrieben am 29.09.2026:** KP1 ist abgeschlossen — die zweite Hälfte (KP1b) steht im
> [Protokoll](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-29_KP1b_Konditionierung_zweite_Haelfte.md), ihre Festlegungen im Leitkonzept N1.63;
> Schemaschritt 152 (`KP-S1v`) trägt 5.7 samt Fremdschlüssel `ID_Vorlage`, Teilindizes und `Nachtauskuehlstunden_H` (5.4).

> **Fortgeschrieben am 29.09.2026 mit E56** (Leitkonzept N1.65, 9.6): Für KP2 liegt der
> [Entwurf](../ueberholt/2026-09-29_Entwurf_KP2.md) vor; entschieden sind die Saat der 14 Vorlagen samt Feiertagen (3.5), das Folgen
> eines unveränderten angelegten Kalenders (3.3, 7.2), die Altfelder (7.1), der Ort der Vorlagenverwaltung (7.4) und die
> Aufteilung der Gesamtangabe des Luftwechsels (3.1); der Entwurf veranschlagt KP2 mit 19–22 PT (8).

> **Fortgeschrieben am 02.10.2026 — KP2 abgeschlossen** (Leitkonzept N1.66, 9.7): Die elf Wellen der Stufe stehen im
> [Protokoll KP2](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-30_KP2_Konditionierung_Oberflaeche.md), ihre Festlegungen im
> Leitkonzept N1.66. Mit **E57** trägt die Zeile „Vorlage" der Matrix die Abkürzung „alle Größen": eine gleichnamige
> Vorlage in allen Größen nach einer Rückfrage für alle Größen, kein Satzbegriff, P11 bleibt (7.2, 7.4, 9.7, R16);
> nachgezogen sind 8, 10.5 und 12.

> **Fortgeschrieben am 02.10.2026 mit E58** (Leitkonzept N1.67, 9.8): Für KP3 liegt der
> [Entwurf](../ueberholt/2026-10-02_Entwurf_KP3.md) vor; entschieden sind die Kappung bei Quelle
> `Heizleistung_Max` (4.3), der Ausgangswert der Rampe nach gestufter Absenkung (4.1, 4.5), Variantenvergleich und
> Kurzbericht (7.6), Zuschnitt des Referenzprojekts 1051 samt Nachtlüftung (10.2) und der Zeitpunkt des Einfrierens
> (10.1); die Aufheizreserve ρ entscheidet der Anwender nach der Messung in RP1 (4.4). Der Entwurf veranschlagt KP3 mit
> 14–18,25 PT (8); nachgezogen sind 5.4, 6 und 12.

> **Fortgeschrieben am 04.10.2026 mit KP4** (Leitkonzept N1.69): KP3 ist in Rechenweg, Daten, Referenzprojekt und Basis
> gebaut — die Wellen R1 bis R5, D1, D2, O1, RP1 und RP2 (Basis R34) —, die Reserve ist mit E64 Nutzereingabe (leer
> 20 % mit Laufhinweis, Welle EV1); offen sind die Oberflächenwellen O1b, O2, O3 und die Welle A. Die Papiere sind
> nachgezogen: Rechenschritte 7.5 und 7.6, Leitkonzept 4.4, 4.5 und N1.69, Softwarearchitektur; hier der Kopf, 4.4,
> 4.6, 5.4 und 8.

> **Nachgezogen 08.10.2026 — AK3-K (E103, E104) gebaut (Basis R43)** ([Register](Status_Gebaeudesimulation_VDI6007.md),
> [Entwurf AK3-K](../ueberholt/2026-10-07_Entwurf_AK3-K.md)): Das „aus“ der Heiz- und Kühlsollwertkalender samt
> Heiz- und Kühlperiode ist die **Freigabe** der Raumheizung und Raumkühlung je Zone und Tag; die Karte zeigt sie als
> Jahresband im Reiter Konditionierung. Ist nur eine Seite frei, gilt sie; sind beide frei, wählt
> die Tagessumme eines unbegrenzten Probetags; ist keine frei, wird die Zone an dem Tag nicht konditioniert. Kein
> Schemaschritt, keine Kopierwege. Nachgezogen ist 3.1.

> **Fortgeschrieben am 09.10.2026 — Kalenderbedienung (E110)** ([Befund](../ueberholt/2026-10-09_Befund_Kalenderbedienung_Konditionierung.md)
> samt Mockup, 7.8): V2 „Wochenprofile" mit dem Jahresraster aus V1; Stufe 1 (K1a, K1b)
> ohne Schemaschritt, Stufe 2 (K2) mit Schemaschritt 207. Neu sind 7.8 und 9.10, nachgezogen 8 und 10.1.

**Stand:** 06.10.2026. **Fassung:** Rev. 3 mit E54 bis E60 — P1–P8 entschieden (E52), P9–P13 und die Heizperiode
entschieden (E53), zwei Fragen des KP1b-Entwurfs entschieden (E54), die Nutzungszeit der Auslegung bestätigt (E55), fünf
Fragen des KP2-Entwurfs entschieden (E56), die Abkürzung „gleichnamige Vorlage in allen Größen übernehmen" aufgenommen
(E57), die acht Fragen des KP3-Entwurfs entschieden, eine davon als Entscheid nach einer Messung (E58), Aufschlag und
manuelle Aufheizzeit (E59) und die Auslegungsgröße (E60) aufgenommen, beide in 9.9; KP0 bis KP2
sind umgesetzt, KP3 ist in Rechenweg, Daten, Referenzprojekt, Basis und Oberfläche gebaut (O1b, O2 und O3 gebaut, Statuszeilen #779, #780 und die der Welle O3; Berichtsvorlagen in Katalogfassung 16 statt 12; Welle A: Schritt 194 gebaut — verwendeter Aufschlag und bemessene Aufheizzeit im Ergebnis, E99), die
Reserve ist Nutzereingabe (E64), die Papiere, Wiki-Quellen und der Logbuch-Entwurf (10.5) sind mit KP4 nachgezogen.

**Zweck.** Jede Größe der Raumkonditionierung — Heiz- und Kühlsollwert, Lüftung, innere Gewinne aus Geräten und
Personen — bekommt je Zone einen stundengenauen Jahreskalender; im Einzonenmodell ist das Gebäude die Zone, Katalogbauten
tragen dieselben Kalender. Eine Vorgabe-Matrix erzeugt die Kalender in einem Schritt, je Größe belegt eine Vorlage —
ein vorbefüllter Kalender aus einer Auswahlliste — Matrixspalte und Kalender, jeder Kalender bleibt einzeln änderbar;
Heiz- und Kühlperiode begrenzen Heizen und Kühlen auf einen Zeitraum; vor einem Sprung des Heizsollwerts nach oben
steigt der Sollwert über eine **berechnete** Aufheizzeit an. Kapitel 9 trennt die Festlegungen nach Empfehlung
(F1–F22) von den Entscheiden E52 (P1–P8) und E53 (P9–P13, Heizperiode).

**Verhältnis zum Leitkonzept.** Teilkonzept des [Leitkonzepts](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
(Grundlagen dort 4.4–4.6, 5.6, 15, N1.32, N1.37, N1.48, N1.55, N1.56). Die Entscheide stehen dort als **Nachtrag
N1.59 (E52, P1–P8)** und **Nachtrag N1.60 (E53, P9–P13 und Heizperiode)** und je als Zeile in Abschnitt 1 der
[Statusdatei](Status_Gebaeudesimulation_VDI6007.md); das [Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md)
führt die Punkte P1–P13 und P15–P17 (Vorschlagsspanne, Aufschlag, Auslegungsgröße; 9.9) in Kapitel 10 mit
Entscheidvermerk und als offenen Punkt P14 die Aufheizreserve nach der Messung in RP1 (E58 F7). Die Stufen KP0–KP4 führt die
Statusdatei in Abschnitt 2. Was das Papier in Kühlkonzept, Anlagenkopplung und Mehrzonenmodell berührt, nennt 2.3.

**Was dieses Papier nicht tut.** Es übernimmt aus Normen weder Tabellenwerte noch Formeln. VDI 6007 Blatt 1 und 3
behandeln Aufheiz- und Absenkdynamik nicht, VDI 2078 nennt ein Nutzungsprofil nur im Beispiel des Anhangs A, VDI 6020
die Nachtabsenkung nur bei der Auslegungstagberechnung; DIN EN 12831-1, DIN V 18599-2/-10, DIN EN 16798-1,
DIN EN ISO 52016-1 und SIA 2024 liegen lokal nicht vor. Die Aufheizrechnung ist deshalb **aus dem eigenen 2K-Modell
hergeleitet** und an ihm nachgerechnet. Keine Hersteller- und Produktdaten; Nutzungsmuster und Vorlagen sind
EPOS-Vorgaben mit runden Werten; die Testdatenbank ist nur lesend befragt.

**Auftrag des Anwenders (26.09.2026), im Wortlaut:**

> 1. Es ist erforderlich, Profile für die Konditionierung (sowohl einzonen als auch Mehrzonenmodell) vorzunehmen. Alle relevanten grössen (Soll-Temperatur Heizung , Soll-Temperatur Kühlung, Lüftung/Luftwechsel, interne Wärmegewinne durch Geräte/Anlage und durch Personen, ..), müssen dazu ein Kalender erhalten der einfach für den benutzer zu bedienen ist (stunden einstellung möglich).
> 2. Es soll voreinstellungen geben, die schnell den Kalender setzen. Zum Beispiel Solltemperatur Heizung: Heizperiode von ... bis, Nachtabsenkung auf ... von ... bis, Wochenendeabsenkung,)
> 3. Bei einem Temperatursprung - zum beispiel bei der Nachtabsekung auf Tagtemperatu von 17 auf 21°C - gibt es einen großen heizwärmebedarf. Dieser soll vermieden werden indem die Heizleestung durch einen sukkzessiven Anstieg der Soll-Temperatur Raum erhöht wird. Es eine Aufheizzeit vor einer Temperaturänderung geben (Raum-Solltemperatur). Diese Zeit soll ermittelt werden durch eine Berechnung der Aufheizzeit vor dem Temperatur-Sprung. Es soll eine maximale aufheizzeit ermittelt werden a) entweder nidrigste Außentemperatur oder b) niedrigste Außentemperatur abzüglich eine vorgegebenen Temperaturabzug (in K)
> 4. Erstelle dazu eine Konzept

**Entscheid des Anwenders (26.09.2026), im Wortlaut:** „P1, P2: Empfehlung / P3: (b) / P4 bis P8: Empfehlung"

**Ergänzung des Auftrags (26.09.2026), im Wortlaut** — zum Bildschirmfoto der heutigen Gruppe „Raumtemperaturen" (Soll
am Tag 20 °C, Nachtabsenkung auf 18 °C von „Vorgabe 22" h bis „Vorgabe 6" h, Maximalraumtemperatur 24 °C,
Wochenendabsenkung 0 °C, Soll in Ferien 0 °C, darunter „Ferien Anfang"):

> „Konzept für Kalender: VDI 6007. Alle relevanten Parameter haben einen Kalender pro Zone: Raumtemperatur Heizen und Kühlen – separat - (Soll-Temperatur), Luftwechsel. Der Kalender soll mit dem Dialog vorbelegt bzw. geschrieben werden (und kann individuell gesetzt werden: Screenshot 1. Die Spalten sollen für Kühlen und Lüftung erweitert werden (auch Ferien). Mit Kalender wird dieser Dialog bei VDI 6007 nur als Vorgabe für den Kalender relevant und die Tagesprofile übers Jahr daraus zu erstellen. -> Konzept. Kühlen wird ebenfalls relevant. Erstelle ein Konzept für den Kalender und die Benutzerführung. Ziel ist mit möglichst wenig Aufwand Änderungen und Vorbelegungen vornehmen zu können. Es soll Vorlagen geben, die übernommen werden können. Vorlagen sollen auch erstellt werden können. Lüftung: Es soll eine Nachtauskühlung geben – also Luftwechselrate in der Nacht (zeiten zu definieren) ist erhöht (wert vorgebbar)."
>
> „kalender auch mit internen last wie schon vorgegeben"

**Entscheid des Anwenders zu P9–P13 (26.09.2026), im Wortlaut:** „P9: (b) / P10: (b) / P11: Eine Vorlage mit
vorbefülltem Kalender zur Auswahl aus mehreren Kalendern. / P12: unklar / P13: (a) / Heizperiode: Wird vom Benutzer
vorgegeben mit Datum Start und Datum Ende. In dieser Zeit ist der Heizwärmeerzeuger aus." — **per Rückfrage geklärt:**
P11 heißt je Größe eine Liste vorbefüllter Kalender (Wohnen, Büro, Schule, eigene), der Anwender wählt je Größe einen,
Sätze aller Größen gibt es nicht; P12 heißt (a) — nur den Matrixbereich ersetzen, eigene Perioden und Ausnahmetage
bleiben, Rückfrage vorher; die Heizperiode heißt: Innerhalb von Start bis Ende wird geheizt, außerhalb steht die
Raumheizung auf „aus", der Wärmeerzeuger liefert dann nur Warmwasser und Prozesswärme.

---

## 0. Das Ergebnis in zehn Punkten

1. **Fünf Kalender je Zone:** Heizsollwert, Kühlsollwert, Lüftung, Geräte und Anlage, Personen (F1). Heizen und Kühlen
   sind getrennte Kalender, der Heizbetrieb ist der Wert „aus", auch stundenweise (P2); im Einzonenmodell ist das Gebäude
   die Zone, Katalogbauten tragen dieselben Kalender (P3).
2. **Die Vorgabe-Matrix ist der Generator:** Die heutige Gruppe „Raumtemperaturen" wird zur Matrix mit den Spalten
   Heizen, Kühlen, Lüftung, Geräte/Anlage und Personen und den Zeilen Tag, Nacht (Wert, von, bis), Wochenende, Ferien und
   Saison. Aus ihr entstehen die fünf Kalender, die danach einzeln änderbar bleiben; unter VDI 6007 ist die Matrix nur
   Vorgabe, der Tagesbilanz-Weg liest weiter die Felder. Die Saison ist die **Heiz- bzw. Kühlperiode** mit Datum für
   Start und Ende; außerhalb steht Heizen bzw. Kühlen auf „aus" — außerhalb der Heizperiode liefert der
   Wärmeerzeuger nur Warmwasser und Prozesswärme (E53).
3. **Nichts wird migriert, alles wird abgeleitet:** Ohne angelegten Kalender erzeugt der Kern ihn aus der Matrix,
   bitgleich zum heutigen Fahrplan; „Kalender anlegen" ändert keine Reihe. Die heutigen Felder bleiben die Zellen, für
   die sie stehen, neue Zellen stehen in einer Vorgabetabelle (P10); KP1 und KP2 rechnen gegen R22 byte-gleich.
4. **Vorlagen je Größe:** Jede Kalenderkarte trägt eine Auswahlliste vorbefüllter Kalender ihrer Größe — ausgelieferte
   (Wohnen, Büro, Schule; gesperrt) und eigene, projektübergreifend im Katalog; die Wahl belegt Matrixspalte und
   Kalender dieser Größe auf Gebäude, Zone oder Katalogbau, „Als Vorlage speichern" legt aus einer Karte eine eigene
   Vorlage an (P11, abweichend von der Empfehlung).
5. **Nachtauskühlung:** Die Nachtzeile der Lüftung trägt einen erhöhten Luftwechsel mit eigenen Zeiten; er wirkt nur,
   wenn der Raum warm und die Außenluft mindestens 2 K kühler ist — die Bedingung der Sommerlüftung (P9).
6. **Speicherung:** STRICT-Tabellen für Kalender, Perioden, Vorgaben und Vorlagen mit IDs und CHECK; die Woche ist ein
   168-Werte-Text nach H8 mit eigenem strengem Leser (F4).
7. **Die Spitze nach dem Sprung ist ein Leistungsüberschuss, kein Wärmeimpuls:** Eine Rampe verteilt ihn; je besser
   gedämmt, desto größer der relative Überschuss.
8. **Die Aufheizrechnung ist eine geschlossene Stufenformel,** ein Vorab-Fahrplan in einem Lauf (F7); bemessen wird an
   der kältesten Stunde (a) oder 2 K darunter (b), die Aufheizleistung ist `Heizleistung_Max`, sonst 1,2 × die Last der
   kältesten Stunde (P5), gerampt wird täglich nach Bedarf (P6).
9. **Das Zahlenbeispiel zeigt die Wirkungsgrenze:** Das Haus aus Projekt 1045 braucht keine Rampe; eine gedämmte
   Variante rampt an der kältesten Stunde 8 h und senkt ihre Spitze um 15 % (4.5).
10. **Stufen KP0–KP4 mit 35–49 PT** — darin 3–4 PT für die Katalogkalender und 6–7 PT für Matrix, Vorlagen und
    Nachtauskühlung —, KP3b optional 3–5 PT. P1–P8 sind mit E52, P9–P13 und die Heizperiode mit E53 entschieden; vor
    KP1 ist keine Frage mehr offen.

## 1. Auftrag, Einordnung, Befund heute

Der Auftrag hat drei Teile: Kalender je Größe für Einzonen- und Mehrzonengebäude (Punkt 1), Voreinstellungen (Punkt 2)
und eine berechnete Aufheizzeit vor Sollwertsprüngen mit einer höchsten Aufheizzeit nach (a) oder (b) (Punkt 3). Die
Ergänzung vom selben Tag macht den heutigen Dialog zur Vorgabe-Matrix der Kalender je Zone, verlangt Vorlagen und eine
Nachtauskühlung; der Entscheid E53 legt Vorlagen je Größe und die Heizperiode fest. Der Auftrag setzt auf E43 auf
(Nachtzeit je Gebäude, Vorgaben 20/18 °C und 5 W/m², Leitkonzept N1.48), ändert mit P7 die Staffel von K11 (E27),
berührt zwei Ausschlüsse der Schwesterpapiere (2.3) und ist über die Zonenkalender mit G6c und G6d verzahnt (8).

**Befund heute.** Pfade unter `EPOS.Kern/Allgemein/Simulation/Gebaeude/` stehen ohne diesen Vorsatz; Zeilen am Stand
`origin/ios_migration_september` vom 26.09.2026.

| Nr | Befund | Fundstelle |
|---|---|---|
| B1 | Die Gruppe „Raumtemperaturen" in „Alle Daten" führt **Zahlenfelder**: Soll am Tag, Nachtabsenkung mit Beginn und Ende (volle Stunde, Platzhalter „Vorgabe 22/6"), Maximalraumtemperatur, Wochenendabsenkung, Soll in Ferien (0 °C heißt jeweils unwirksam), darunter vier Ferienzeiträume aus Tag und Monat. Kühl-, Lüftungs- und Lastwerte stehen in anderen Gruppen; eine grafische Kalenderdarstellung gibt es nicht | `EPOS.UI/Dialoge/Bedarf/GebaeudeStammblattFelder.razor:183-260, 340`; `EPOS.UI/Dialoge/Bedarf/GebaeudeKatalogDialog.razor:495` |
| B2 | Der Sollwertfahrplan ist eine **Stufenfunktion**: Ferien vor Wochenende vor Tag/Nacht. Das Wochenende gilt ganztags und nur mit Wert > 5 °C, der Merker `Wochenende` wird nicht gelesen; Ferien nur mit `Ferien` > 0,9 und Wert ≥ 1 °C; 0 und 366 heißen „aus", Beginn > Ende heißt Jahreswechsel, ein anderer Tag außerhalb 1…365 ist ein benannter Fehler | `GebaeudeModellEingang.cs:1842-1892`; `GebaeudeFestwerte.cs:128-134` |
| B3 | Der Wochentag kommt aus dem **Wochentagsraster des Projekts** (E115, `Konditionierungdatenweg.Raster`), demselben wie im Zapfkalender: im Regelfall dem der Klimaregion; trägt das Projekt eine Preisreihe mit Jahr, gilt für alle Leser der Kalender dieses Jahres — Raster des echten 1. Januar und echte Feiertagsdaten. Die Ortszeit ist MEZ/MESZ mit eigener Regel für die Umstellstunden | `EPOS.Kern/Allgemein/Simulation/Klimakalender.cs:83-113`; `EPOS.Kern/Controller/SolardatenCtrl.cs:147-153, 241`; `EPOS.Kern/Allgemein/DbWerte.cs:2681`; `GebaeudeModellEingang.cs:1606-1617` |
| B4 | Einziger Zeitprogramm-Baustein ist das **Wochenraster** (7 × 24, streng: eine leere Zelle ist ein Fehler), genutzt für `Sollwertprofil` (AK1) und den Erzeugerfahrplan (AK2). `Sollwertprofil`: 168 Werte mit `;`, höchstens 1 400 Zeichen; der Schreiber setzt zwei Nachkommastellen, der Leser nimmt jede Stellenzahl, aber nur Zahlen. Das Profil gilt nur mit wirksamer Kopplung, Ferien wirken darüber | `EPOS.UI/Bausteine/Wochenraster.razor:1-19`; `EPOS.Kern/Allgemein/Update/AnlagenkopplungSchema.cs:233-242, 291-336`; `GebaeudeModellEingang.cs:920, 1559-1599` |
| B5 | Innere Gewinne sind **ein konstanter Wattwert**, je zur Hälfte konvektiv und radiativ, nach Leitkonzept 3.3 „Leistung des ganzen Katalogbaus, zeitlich konstant". Er entspricht bei rund 150 von 275 Katalogsätzen und 24 von 29 Projektzeilen etwa 70 W je Person (Median 2,3 W/m²). Personen sind keine eigene Größe | `GebaeudeModellEingang.cs:586, 810-811` |
| B6 | Der Luftwechsel ist eine Konstante (Infiltration + Nutzer, Vorgaben 0,3 und 0,4 1/h). Einzige Zeitabhängigkeit ist die **Sommerlüftung**: 2,0 1/h, ein, wenn die Raumluft der Vorstunde über 23 °C (mit wirksamer Kühlung θ_kühl − 3 K) liegt und die Außenluft mehr als 2 K kühler ist, aus mit 1 K Hysterese, ausgewertet am Stundenbeginn | `GebaeudeFestwerte.cs:139-148`; `Sommerlueftungsregel.cs:5-27`; `Vdi6007Rechenweg.cs:270-273` |
| B7 | Der Kühlsollwert ist konstant; leer heißt Kühlung aus (F-K1). `Kuehl_Sollwert_Nacht` steht in `Tab_Gebaeude(_STAMM)` und `Tab_Zone`, wird durchgereicht und nicht gelesen. Geprüft wird θ_kühl ≥ höchster Heizsollwert des Jahres + 1 K | `GebaeudeModellEingang.cs:930, 1626-1645`; `EPOS.Kern/Allgemein/Zonenvorgaben.cs:55`; `EPOS.UI/Dialoge/Bedarf/GebaeudeKatalogDaten.cs:270-275` |
| B8 | `Heizleistung_Max` ist die einzige Leistungsgrenze (leer = unbegrenzt, Zone nach Flächenanteil); der Löser kappt. Gezählt werden die gekappten Stunden **nur mit wirksamer Kopplung** | `GebaeudeModellEingang.cs:170, 407-408`; `Vdi6007Rechenweg.cs:337-342`; `HeizkreisErgebnis.cs:208-209` |
| B9 | Ohne Grenze liegt die Jahresspitze in elf von zwölf Projekten auf der ersten Stunde nach der Nachtabsenkung am kältesten Tag — der ideale Heizer deckt den Sprung in einer Stunde | Leitkonzept 5.6 (Prototyp) |
| B10 | Die Auslegungsaußentemperatur (H10) ist das abgerundete **kälteste Tagesmittel**; die kälteste Stunde ist nirgends geführt. In 12 der 14 Referenzprojekte mit Gebäude liegt sie bei −18,2 °C gegen ein Tagesmittel von −10,95 °C (H10 −11 °C), in 1018 und 1049 bei −9,3 gegen −6,6 °C. Die stationäre Last ist ein Aufruf; die Auslegungsraumtemperatur fällt auf `SollTag` zurück | `GebaeudeModellEingang.cs:1237, 1268-1287, 1512-1528`; `Zonenmodell2K.cs:601` |
| B11 | Der Löser kann **je Stunde**: Heizsollwert NaN = keine Heizung, obere Grenze +∞ = keine Kühlung, Zusatzleitwert ≥ 0. Der Eingang füllt Grenzen und Strahlungsanteil als Skalare; der Vorlauf startet mit dem Sollwert der ersten Vorlaufstunde (NaN bräche ihn); der Zwischenspeicher des freien Falls hält einen einzigen Leitwert | `Stundenrand.cs:36-117`; `Zonenmodell2K.cs:959-966, 1037`; `Vdi6007Rechenweg.cs:283` |
| B12 | Mehrzonen: **Nachtzeit, Ferien und Kühlwerte kommen vom Gebäude** (N1.56 Festlegung 1, E49 A4 (a)); jedes Zonenfeld darf leer sein und heißt dann „Wert des Gebäudes"; die Nutzungszeit der Kennzahlen ist die der ersten beheizten Zone; bis zu 50 Zonen je Gebäude | `GebaeudeModellEingang.cs:391-392`; `Zonenrechnung.cs:212-215`; `GebaeudeModellErgebnis.cs:116, 147`; `EPOS.Kern/Allgemein/GebaeudeZonenregeln.cs:20` |
| B13 | Tag/Monat → Jahrestag rechnet im **laufenden** Kalenderjahr: In einem Schaltjahr (2028) landet jedes Datum ab dem 1. März einen Tag später | `EPOS.Kern/Allgemein/Ferienzeit.cs:44-46` |
| B14 | Weitere Leser der Felder: der Altweg (vier Sollwerte, keine Nachtzeit, kein Profil), das Zapfprofil (Ferien-Vorbelegung), der gbXML-Export (`SollHeizenC` aus dem Tagwert). Der einzige Weg Katalog → Projekt ist `CopyFromStamm`; er setzt den Katalogverweis `ID_Gebaeude_Stamm` der Projektkopie | `EPOS.Kern/Allgemein/Simulation/Altweg/TagesbilanzRechenweg.cs:279-360`; `EPOS.Kern/Controller/ZapfprofilCtrl.Eingang.cs:450-470`; `EPOS.Kern/Allgemein/Export/Gebaeude/GebaeudeExportAblauf.cs:375`; `EPOS.Kern/Controller/GebaeudeStammCtrl.cs:606-615` |
| B15 | Ausgeschlossen sind die vorausschauende Aufheizung („Optimierung der Einschaltzeit") und Nutzungsprofile für Nichtwohngebäude (SIA 2024, DIN V 18599-10) | Anlagenkopplung 1.3, 4.3, 4.4; Leitkonzept 15 |
| B16 | Testdatenbank (Basis R22): 17 Gebäude in 14 Referenzprojekten (1030 rechnet ohne Gebäude), **0 Zonen** in der ganzen Datenbank; alle Referenzgebäude 20/18 °C, Nachtzeit leer (22–6 Uhr), ohne Wochenend- und Ferienfahrplan, `Sollwertprofil`, `Heizleistung_Max` und Kühl-Nachtwert. Im Katalog tragen 49 Sätze aktive Ferien und 51 einen Wochenendwert über 5 °C. Schemastand 150 | `EPOS.Kern/Allgemein/Update/SchemaStand.cs:682`; Abfrage lesend |
| B17 | `Maximaleraumtemperatur` ist allein die **Grenze der Überhitzungsstunden**; der Löser regelt nicht auf sie, ohne wirksame Kühlung läuft das Gebäude frei (E32). Geprüft wird, dass sie über dem Tagsollwert liegt | `GebaeudeModellEingang.cs:122-128, 1795-1799` |

**Folgerung.** Wochentagslogik, strenger Leser, Rasterbaustein und stündliche Randbedingungen liegen bereit. Es fehlen
Datumsbereiche, Stundenreihen für Lüftung und Gewinne, Zonen- und Katalogkalender, eine Vorgabe für Kühlen und Lüftung
über Tag, Nacht, Wochenende und Ferien, Vorlagen, die Größe „kälteste Stunde" und jede Vorwegnahme des Sprungs. Kein
Referenzprojekt nutzt eine Sonderregel aus B2 — die Bitgleichheit braucht Proben über den ganzen Katalog (3.3).

## 2. Anforderungen und Abgrenzung

### 2.1 Anforderungen

| Nr | Anforderung (Auftragspunkt, Entscheid) | Nachweis |
|---|---|---|
| KA1 | Jede der fünf Größen hat je Zone einen Kalender, stundengenau änderbar (1; Ergänzung) | Kernprobe, bunit |
| KA2 | Bedienbar ohne Zahlenkolonnen: Zeitfenster „Tage, von, bis, Wert", Perioden mit Datum, Vorschau als Bild (1) | Sichtabnahme Windows, bunit |
| KA3 | Die Vorgabe-Matrix belegt die fünf Kalender vor oder schreibt sie; danach bleibt jeder Kalender einzeln änderbar (2; Ergänzung) | Kernprobe Generator, bunit |
| KA4 | Vor jedem Sprung des Heizsollwerts nach oben steigt der Sollwert stufenweise; der Zielwert steht zu Beginn der Nutzung. Groß wird nach dem Sprung die **Leistung**, nicht die Wärme: Eine Rampe senkt die Spitze und hebt die Jahresheizwärme leicht (3) | N-AH10 |
| KA5 | Die Aufheizzeit wird je Sprung aus dem Modell der Zone **berechnet**; die höchste gilt bei (a) der niedrigsten Außentemperatur oder (b) dieser abzüglich ΔT_K (3) | N-AH1 bis N-AH5 |
| KA6 | Aufheizzeiten, ihre Grundlage und die Grenzfälle stehen in Ergebnis, Bericht und Export | Datenbankfall, Bericht |
| KA7 | Katalogbauten der Auslieferung tragen eigene Kalender; die Übernahme ins Projekt nimmt sie mit, die Auslieferungsvorlage liefert sie aus (P3) | Datenbankfälle, Vorlagenlauf |
| KA8 | Vorlagen je Größe — eine Auswahlliste vorbefüllter Kalender je Kalenderkarte — lassen sich übernehmen und erstellen, ausgeliefert und eigen, projektübergreifend (Ergänzung, P11) | Datenbankfälle, bunit |
| KA9 | Nachtauskühlung: erhöhter Luftwechsel in einem vorgebbaren Nachtfenster mit vorgebbarem Wert, wirksam unter der Bedingung der Sommerlüftung (Ergänzung, P9) | N-NK1 bis N-NK4 |
| KA10 | Heizperiode mit Datum für Start und Ende: innerhalb wird geheizt, außerhalb steht die Raumheizung auf „aus", der Wärmeerzeuger liefert nur Warmwasser und Prozesswärme; die Kühlperiode gilt entsprechend (2; E53) | Rechenproben und Kaskadenprobe (6) |
| KN1 | **Byte-Gleichheit:** Ohne angelegten Kalender, ohne neue Matrixzelle und mit ausgeschalteter Aufheizoptimierung rechnet jedes Projekt byte-gleich; „Kalender anlegen" ändert keine Reihe | Referenzlauf gegen R22, Wache 3.3 |
| KN2 | **Determinismus:** nur Daten, Klimareihe und Referenzjahr — keine Uhr, kein Zufall, `InvariantCulture` | zwei Läufe, de-DE gegen en-US |
| KN3 | **Rechenzeit:** O(8 760) je Zone und Größe, die Aufheizrechnung unter 1 ms je Zone, **kein Zweitlauf**; E36 bleibt unberührt, eine neue Rechenzeitgrenze ist nicht nötig | Messung im Abnahmelauf |
| KN4 | **Plattform und Daten:** Fachlogik im Kern, keine Datenbank in der Oberfläche, iOS ohne eigenen Code, Berührungsziele ≥ 44 px; STRICT, IDs, Schalter 0/1 mit CHECK, NULL als Vorgabe | Wächter, Schemaprobe, `SqlDialektPruefer` |
| KN5 | **Benannt statt still:** ungültige Eingaben sind benannte Fehler, Grenzfälle benannte Hinweise mit Zähler | Test je Grund |
| KN6 | **Wenig Aufwand:** Je Größe eine Vorlagenwahl und wenige Matrixzellen ergeben einen vollständigen Satz aus fünf Kalendern; Einzelarbeit am Kalender nur bei Bedarf (Ergänzung, P11) | Sichtabnahme mit Schrittzählung |

### 2.2 Abgrenzung

- **K11 und KU3.** Das Zeitprofil der Kühlung kommt in den Kühlkalender (P7), die Kühlspalte der Matrix belegt ihn vor.
  KU3 behält Kältemaschine, freie Kühlung, Kältespeicher, Export, Vorkühlen und **Kühlung je Zone**; einen
  Kühlkalender der Zone gibt es erst mit KU3.
- **Altweg.** Projekt 1040 und jedes Gebäude auf dem Tagesbilanz-Weg lesen weder Kalender noch Matrixzellen noch Rampe,
  dauerhaft (E89, ADR-006); für sie zeigt die Matrix nur die Felder, die der Altweg liest. Dieser Hinweis ist ein
  Altweg-Sonderfall und kommt mit KP2 ins Inventar der Altweg-Bestandteile (Umsetzungskonzept 6.1).
- **Anlagenkopplung.** Der Erzeugerfahrplan (AK2) ist Verfügbarkeit der Anlage — derselbe Rasterbaustein, eine andere
  Größe. Die Heizperiode schaltet keinen Erzeuger ab, sondern die Raumheizung (E53); wer einen Erzeuger außerhalb der
  Heizperiode abschalten will, nutzt dessen Fahrplan. `Sollwertprofil` bleibt Bestandsweg von AK1, ein angelegter
  Heizkalender hat Vorrang; AK1-Gebäude bekommen keine Rampe (F13).
- **Lastgänge, Zapfprofil, Tarife** bleiben eigene Zeitreihen; berührt wird allein die Ferien-Vorbelegung des
  Zapfprofils (5.5). **Normprofile:** keine Tabellen aus DIN V 18599-10 oder SIA 2024; EPOS-Muster für Nichtwohnbauten
  sind ausgelieferte Vorlagen (P4).
- **Import-Zeitpläne:** gbXML `Schedule`, `Occupants` und die IFC-Zeitreihen bleiben ungelesen (Datenaustausch 13).
  **Feuchte:** Personen zählen sensibel; latente Lasten und Entfeuchtung bleiben ausgeschlossen (K5).
- **Regelungstechnik:** Rampe und Nachtauskühlung sind Vorgaben auf deterministischen Klimadaten; der Regler bleibt, was
  er ist. Keine Kalendergrößen sind `Maximaleraumtemperatur`, Leistungsgrenzen, Infiltration und Sonnenschutz.

### 2.3 Berührte Festlegungen der Schwesterpapiere

Anlagenkopplung, Kühlkonzept und Mehrzonenmodell werden mit diesem Papier **nicht** geändert; ihren Nachzug übernimmt
KP0. Leitkonzept 15 ist mit N1.59 nachgezogen.

| Stelle | Heute | Mit E52, E53 und Rev. 3 |
|---|---|---|
| Anlagenkopplung 4.4 | vorausschauende Aufheizung („Optimierung der Einschaltzeit") ausgeschlossen | Auftragspunkt 3 verlangt sie als Vorab-Fahrplan der idealen Regelung; der Ausschluss gilt weiter für den Regler und für AK1-Gebäude bis KP3b |
| Anlagenkopplung 1.3, 4.3; Leitkonzept 15 | Nutzungsprofile für Nichtwohngebäude ausgeschlossen | auf Normprofile verengt (P4); Leitkonzept 15 nachgezogen |
| Kühlkonzept 7.1, 11; Register K11 (E27) | Zeitprofil der Kühlung in KU3 | im Kühlkalender mit KP1 (P7), vorbelegt aus der Kühlspalte der Matrix; K11 trägt im Register den Vermerk |
| E52, P7 (a), Wortlaut „bleibt ungelesen" | `Kuehl_Sollwert_Nacht` füllt nur die Voreinstellung vor | ist mit P13 (a) (E53) die Zelle Kühlen/Nacht der Matrix |
| Leitkonzept N1.56 Festlegung 1 | die Nachtzeit kommt vom Gebäude | eine Zone mit eigener Matrixzeile oder eigenem Kalender hat eigene Zeiten (F2) |
| Leitkonzept N1.55, E49 A4 (a) | Kühlwerte vom Gebäude | bleibt bis KU3 |
| Rechenschritte 7.2, F-P4 (Sommerlüftung) | Regel über den ganzen Tag, 2,0 1/h | bleibt; die Nachtauskühlung nutzt ihre Bedingung im Nachtfenster (P9) |
| Anlagenkopplung 4.3 (H8) | 168 Werte als Text für **einen** Wochenvektor je Gebäude | gilt für die Woche jedes Kalenders; Perioden und Vorgaben stehen in Tabellen (F4) |
| Anlagenkopplung 3.4 Nr. 3, 10.1 (Vorlauf je Stunde) | Vorlauf leer nur jenseits der Heizgrenze | auch in jeder Stunde mit Heizsollwert „aus" — außerhalb der Heizperiode oder stundenweise —, getrennt von der Heizgrenze gezählt (6) |

## 3. Fachliches Modell

### 3.1 Größen und Semantik

| Größe | Werteart | heutige Spalten | ohne Kalender | Eigentümer |
|---|---|---|---|---|
| **Heizsollwert** θ_H | 0…30 °C oder „aus", auch je Stunde | vier Sollwerte, `Nachtabsenkung_Beginn/_Ende`, `Ferien`, `Ferienbeginn/-ende_1…4`; AK1 `Sollwertprofil` | Standardfahrplan (3.3) | Zone (Gebäude), Katalogbau, Vorlage |
| **Kühlsollwert** θ_K | 15…35 °C oder „aus" | `Kuehl_Sollwert`, `Kuehl_Sollwert_Nacht` (P13) | Konstante wie heute; nur mit Kühlbetrieb und `Kuehlung_Aktiv` (E32) | Gebäude, Katalogbau, Vorlage; Zone ab KU3 |
| **Lüftung** n_N | Nutzerlüftung 0…20 1/h; die Infiltration bleibt konstant darunter | `Luftwechsel_Nutzer` | der heutige Wert | Zone (Gebäude), Katalogbau, Vorlage |
| **Geräte und Anlage** Q_G | Anteil 0…100 % eines Nennwerts [W] | `Interne_Waermegewinne` | 100 % | Zone (Gebäude), Katalogbau, Vorlage |
| **Personen** Q_P | Anwesenheit 0…100 % × Nennwert [W] | neu; `Bewohner` schlägt die Personenzahl vor | kein Kalender = 0 W zusätzlich | Zone (Gebäude), Katalogbau, Vorlage |

**Lüftung (F15, fortgeschrieben):** Der Kalender führt die Nutzerlüftung **absolut in 1/h** — die Nachtauskühlung
verlangt Werte über dem Tageswert, und ein Luftwechsel ist auf das Volumen bezogen, eine Zone erbt ihn also sinnvoll.
Stammt der Luftwechsel aus der Gesamtangabe `Luftwechselrate`, verlangt jede Lüftungsvorgabe die getrennte Angabe
(E56: eine Rückfrage teilt die Gesamtangabe auf — Infiltration = min(0,3 1/h; Rate), Nutzerlüftung = der Rest —, der
wirksame Luftwechsel bleibt gleich). **Gewinne (P1):** `Interne_Waermegewinne` ist ein Dauerwert, der
meist die Personen schon enthält (B5). Entschieden ist die Trennung, energieerhaltend: Wer einen Personenkalender anlegt
oder die Personenspalte der Matrix füllt, bekommt als Geräte-Nennwert `Interne_Waermegewinne` minus das Jahresmittel der
Personenwärme; die Karte zeigt die Rechnung und beide Jahresmittel. **Personen:** Nennwert = Personenzahl × **70 W**
(EPOS-Vorgabe, passt zum Katalog), sensibel, je zur Hälfte konvektiv und radiativ; die Personenzahl kommt aus
`Bewohner` (Gebäude und Zone), sonst aus Nutzfläche ÷ `Flaeche_Nutzer`. **„aus"** heißt beim Heizen NaN, beim Kühlen
+∞, bei der Lüftung 0 1/h Nutzerlüftung, bei Anteilen 0.

**„aus“ ist die Freigabe (E104).** Das „aus“ des Heiz- und des Kühlsollwerts — als Grundangabe, in der
Standardwoche, je Periode und als Heiz- bzw. Kühlperiode (außerhalb „aus“, E53) — entscheidet je Zone und Tag, ob
Raumheizung und Raumkühlung **freigegeben** sind: frei ist eine Seite, wenn ihr Sollwert an mindestens einer Stunde
des Tags nicht „aus“ ist (die Kühlung zusätzlich nur mit Kühlbetrieb und `Kuehlung_Aktiv`). Innerhalb dieser Freigabe
wählt die Simulation die Tagesart der Zone ([Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 3.5):
sind beide frei, nach den Tagessummen des Probetags; ist nur eine frei, diese. Am Kühltag steht der Heizsollwert den
ganzen Tag auf „aus“, am Heiztag der Kühlsollwert. Die Kalenderkarte im Reiter Konditionierung zeigt je Zone ein
**Jahresband** „Heizen frei / Kühlen frei / beides / keines“ und die Zahl der Tage, an denen die Tagesart nach Bedarf
entscheidet. Für die Freigabe gibt es keine neue Spalte, keinen Schemaschritt und keine Kopierwege.

### 3.2 Kalendermodell

**Raster und Zeitbasis.** Ein Kalender liefert 8 760 Werte: Tag d = 0…364, Stunde s = 0…23, h = 24·d + s, Gemeinjahr
mit 365 Tagen; die Woche hat 168 Stunden von Montag 00:00 bis Sonntag 23:00. Die Kalenderstunde ist die Uhrstunde der
Ortszeit, in der der Lauf rechnet (MEZ/MESZ, `EPOS.Kern/Allgemein/Simulation/SolarZeitbasis.cs`); an den zwei
Umstelltagen gilt deren Regel, der Kalender führt keine eigene. **Wochentag:** w(d) = (w₀ + d) mod 7, w₀ aus
dem **einen Wochentagsraster des Projekts** (E115, `Konditionierungdatenweg.Raster`): w₀ ist der Wochentag des 1. Januar
im Raster der Klimaregion (`ProfilBedarf.WochentagJan1AusKlimaregion`, aus `Tab_Klimadaten.WE`), mit einer Preisreihe mit
Jahr der Wochentag des echten 1. Januar dieses Jahres — dasselbe Raster, mit dem Zapfkalender und Bedarfsprofile rechnen;
die Wochenendmaske des Gebäudelaufs wird daraus gebildet. Ein Projektgebäude kennt sein Projekt über
`Tab_Gebaeude.ID_Projekt`; auch sein Arbeitsstand rechnet in dessen Raster. Ohne Projekt (Katalog)
oder ohne Klimaregion gilt das Rückfallraster mit dem 1. Januar auf einem Sonntag (`ProfilBedarf.WOCHENTAG_ALTKONVENTION`),
wie im Zapfkalender. Der Kalender speichert kein Jahr: Wechselt die Klimaregion, wandern die Wochentage, die Daten nicht;
Datum ↔ Jahrestag rechnet der Dialog im Gemeinjahr, nie im laufenden Jahr (B13). Die Bedienung nennt im Regelfall kein
Jahr, sondern das Raster („Gemeinjahr, 1. Januar = Donnerstag", Datumsanzeige „Do 15.01.").

**Ebenen und Vorrang**, von schwach nach stark:

```
Ebene 1  Grundangabe     ein Wert oder „aus"
Ebene 2  Standardwoche   168 Zellen (Zahl oder „aus", P2); ist sie angelegt, tritt sie an die Stelle der Grundangabe
Ebene 3  Perioden        Beginn, Ende (Tag 1…365, Beginn > Ende = über den Jahreswechsel) oder eine Feiertagsregel;
                         Rang 1…999 eindeutig; Art ZEITRAUM, FERIEN, FEIERTAG oder BETRIEBSPAUSE; genau eine Angabe:
                         Wert, „aus", eigene Woche oder „wie Wochentag X" (1 = Montag … 7 = Sonntag)

v(h) = Angabe(P*) an (w(d), s)       P* = ranghöchste Periode, die den Tag d enthält
v(h) = Ebene 2 bzw. 1 an (w(d), s)   wenn keine Periode den Tag enthält
```

Perioden gelten **ganze Tage**; je Stunde gewinnt genau eine Quelle, die Vorschau nennt sie („Quelle: Sommerferien").
„Wie Wochentag X" nimmt die Stunden dieses Tags aus der Standardwoche — die Angabe der Feiertage. Die Art ordnet und
benennt, gerechnet wird mit ihr nicht.

**Heiz- und Kühlperiode (E53).** Die Saison einer Spalte ist ein Zeitraum mit Datum für Start und Ende, gespeichert als
Tag 1…365 im Gemeinjahr; Start nach Ende heißt über den Jahreswechsel (1.10.–30.4.). Der Generator bildet daraus
**eine** Periode der Art BETRIEBSPAUSE mit „aus" über die Tage außerhalb — vom Tag nach dem Ende bis zum Tag vor dem
Start, wo nötig über den Jahreswechsel — im Rang über den Perioden der Matrix (3.3); innerhalb gelten Woche, Ferien und
eigene Perioden wie ohne Saison. Wie jede Periode gilt sie ganze Tage: Die Heizperiode beginnt um 00:00 des Starttags
und endet um 24:00 des Endtags. Leer heißt ganzjährig; ein Zeitraum über das ganze Jahr wird als leer gespeichert.

**Feiertage als Regel (F11).** Die neun bundeseinheitlichen Feiertage (Neujahr, Karfreitag, Ostermontag, 1. Mai,
Christi Himmelfahrt, Pfingstmontag, 3. Oktober, 1. und 2. Weihnachtstag) stehen als Regelkennung in der Periode, nicht
als Jahrestag; die acht Landesregeln ebenso (Feiertagsland am Gebäude). **Die Konvention des Gemeinjahrs (E114):** Im
Regelfall ist kein Jahresdatum relevant. Ohne Preisreihe mit Jahr liegen die Regeln im Wochentagsraster des Projekts
(`Gemeinjahrkalender`, E115: ein Raster für Gebäudelauf, Zapfkalender und Bedarfsprofile, im Regelfall das der Klimaregion): Ostersonntag ist der Sonntag des Rasters am nächsten zum Jahrestag 98 (8. April) — ein Gleichstand kann in
einer Woche von sieben Tagen nicht eintreten, sonst gälte der frühere —, Karfreitag liegt 2 Tage davor, Ostermontag 1,
Himmelfahrt 39, Pfingstmontag 50 und Fronleichnam 60 Tage danach; Buß- und Bettag ist der letzte Mittwoch des Rasters vor
dem Jahrestag 327 (23. November). So fallen die beweglichen Feiertage auf ihren Wochentag in derselben Woche, die
Wochenprofile, Standardlastprofile und Zapfkalender lesen. Feste Feiertage stehen auf Tag und Monat. Nur wenn die aktive
Variante eine Preisreihe mit Jahr trägt, gilt der Kalender dieses Jahres (`Gemeinjahrkalender.Kalenderjahr`): das Raster
seines echten 1. Januar und Ostern und Buß- und Bettag auf seinen echten Daten (Tag und Monat im Gemeinjahr) — so liegt
Ostern auf einem Sonntag, Buß- und Bettag auf einem Mittwoch des Rasters, und die Wochenenden des Bedarfs liegen auf denen
der Preisreihe. Alle Leser — Gebäudelauf, Zapfkalender, Bedarfsprofile, Arbeitsstand, Teppich, Nutzungstage,
Profilvorschau, Betriebskalender — lösen den Sonderfall über `Konditionierungdatenweg.Raster` auf; ein Jahr mit fremdem
Raster ist nicht baubar (`Gemeinjahrkalender.Aus` prüft w₀). Die Bedienung nennt dann „Bezugsjahr 2027", volle Daten und
die Wochentage des Jahres. **Ring:** Für Rampe und Vorlauf ist das Jahr ein
Ring; ein Sprung am 1. Januar greift in die Dezemberstunden wie der Vorlauf (Leitkonzept 4.6).

### 3.3 Vorgabe-Matrix und Standardfahrplan — abgeleitet bis angelegt, bitgleich

**Die Matrix ist die Vorgabe, der Generator macht daraus Kalender.** Sie fasst die heutige Gruppe „Raumtemperaturen",
die Kühlwerte, die Lüftung und die Lasten in einer Tabelle. „neu" heißt: Zelle der Vorgabetabelle (5.6, P10); jede andere
Zelle ist die genannte Bestandsspalte.

| Zeile | Heizen (°C oder „aus") | Kühlen (°C oder „aus") | Lüftung (1/h) | Geräte/Anlage | Personen |
|---|---|---|---|---|---|
| Nennwert | — | — | Infiltration `Luftwechsel_Infiltration`, konstant | `Interne_Waermegewinne` [W], nach P1 Gesamtwert − Personenmittel | neu [W]; Vorschlag `Bewohner` × 70 W |
| Tag | `Raumsolltemperatur_Tag` | `Kuehl_Sollwert` | `Luftwechsel_Nutzer` | neu, Anteil (leer = 100 %) | neu, Anteil |
| Nacht: Wert, von, bis | `Raumsolltemperatur_Nachtabsenkung`, `Nachtabsenkung_Beginn/_Ende` (leer = 22–6 Uhr) | `Kuehl_Sollwert_Nacht` (P13); Zeiten neu | neu: Nachtauskühlung, Wert und Zeiten, bedingt (3.7, P9) | neu | neu |
| Wochenende | `Raumsolltemperatur_Wochenende` — absolut, Sa und So ganztägig, wirksam nur über 5 °C | neu | neu | neu | neu |
| Ferien | `Raumsolltemperatur_Ferien` — absolut, ganztägig, ab 1 °C, auf dem VDI-Weg nur mit Merker `Ferien` | neu | neu | neu | neu |
| Saison: Start, Ende | neu: Heizperiode — innerhalb heizen, außerhalb „aus" (E53) | neu: Kühlperiode — innerhalb kühlen, außerhalb „aus" | — | — | — |

Neben der Matrix stehen drei Einzelangaben: die **Ferienzeiträume** (`Ferienbeginn/-ende_1…4`, als Datum im Gemeinjahr;
sie gelten für alle Spalten, der Merker `Ferien` schaltet nur die Heizspalte), die **Maximalraumtemperatur** als Grenze
der Überhitzungsstunden (F21) und der Schalter **Sommerlüftung** (3.7).

**Abbildung der heutigen Felder.** Die Zellen der Heizspalte tragen genau die Bedeutung, die der Kern heute rechnet;
belegt hat sie die Sitzung „Dialoge und Korrekturen" (#571) mit diesen Fundstellen:

- **Wochenende:** `Raumsolltemperatur_Wochenende` ist ein absoluter Wert und wirkt nur über 5 °C
  (`GebaeudeFestwerte.cs:128`, geprüft über `Gebaeudemodellvorgaben.WochenendsollwertWirksam`,
  `Gebaeuderechenweg.cs:176`); dann gilt er Sa und So alle 24 Stunden, die Nacht eingeschlossen (VDI-Weg
  `GebaeudeModellEingang.cs:1892`, Tagesbilanz `TagesbilanzPhysik.cs:194`). Werte bis 5 °C, auch 0, heißen
  „Wochenende wie Werktag".
- **Ferien:** `Raumsolltemperatur_Ferien` wirkt ab 1 °C (`GebaeudeFestwerte.cs:134`); der VDI-Weg verlangt zusätzlich
  den Merker `Ferien` über 0,9 (`GebaeudeModellEingang.cs:1846`; Tagesbilanz `TagesbilanzRechenweg.cs:279`). Ferien
  gelten ganztägig und im Rang vor dem Wochenende (`GebaeudeModellEingang.cs:1891`).
- **Nachtzeit:** Die Nachtzeit des Gebäudes (`Nachtzeit.cs`, Vorgabe 22–6 Uhr) wirkt nur auf dem VDI-Weg; der
  Tagesbilanz-Weg rechnet fest 7–22 Uhr als Tag (`TagesbilanzPhysik.cs:196`) und liest die Matrix nicht (2.2).
- **Merker:** Die Oberfläche setzt `Wochenende` und `Ferien` schon bei einem Wert über 0
  (`GebaeudeArbeitsstand.cs:1539/1541`); die Rechnung liest fürs Wochenende nur den Wert, für die Ferien Merker und
  Wert.
- **Anlagenkopplung:** Ein wirksames Sollwert-Zeitprogramm (`Sollwertprofil`) ersetzt Tag, Nacht und Wochenende; die
  Ferien liegen darüber (`GebaeudeModellEingang.cs:1595`).

Der Standardfahrplan-Generator reproduziert genau das, bitgleich; die Wache unter „Bitgleich" hält es an allen
Schwellen.

**Generator.** Je Spalte entsteht ein Kalender: Die Standardwoche trägt werktags den Tagwert und im Nachtfenster der
Spalte den Nachtwert, Sa und So ganztags den Wochenendwert; Ferien werden Perioden der Art FERIEN (Rang 200 + k), die
Saison eine Periode der Art BETRIEBSPAUSE außerhalb von Start bis Ende mit „aus" (Rang ab 900, 3.2). **Leere Zellen**
heißen: Nacht wie Tag, Wochenende wie Werktag, Ferien wie gewöhnliche Tage, Saison ganzjährig; ein leeres Nachtfenster
ist das der Heizspalte (F19). Für die Heizspalte gelten die heutigen Regeln wörtlich (Wochenende nur über 5 °C, Ferien
nur mit Merker und Wert ≥ 1 °C, 0 und 366 = aus, Nachtzeit mit der strengen Prüfung des Laufs statt des Rückfalls von
`Bestandswoche`, `EPOS.Kern/Allgemein/Gebaeuderechenweg.cs:369-384`); mit wirksamer Kopplung ersetzt `Sollwertprofil`
die Standardwoche der Heizspalte, die Ferien bleiben darüber (`GebaeudeModellEingang.cs:1559-1599`). Die Lasten werden
Anteile × Nennwert; die Nachtzeile der Lüftung trägt die Nachtauskühlung (3.7).

**Heizperiode und Kühlperiode (E53).** Der Anwender gibt die Heizperiode mit Datum für Start und Ende vor; innerhalb
wird geheizt, außerhalb steht die Raumheizung auf „aus" (P2): Der Löser rechnet die Zone dort ohne Heizung, die
Gebäudewärme im Kanal Raumwärme ist 0, und der Wärmeerzeuger liefert nur Warmwasser und Prozesswärme (4.7, 6). Die
Heizperiode schaltet keinen Erzeuger ab — Warmwasser, Prozesswärme und externe Lastgänge laufen weiter; wer einen
Erzeuger außerhalb wirklich abschalten will, nutzt dessen Fahrplan (AK2). Die **Kühlperiode** gilt entsprechend:
Außerhalb steht der Kühlsollwert auf „aus" (+∞), die Zone schwingt nach oben frei, die Überhitzungsstunden zählen gegen
`Maximaleraumtemperatur` wie heute (B17), und die Schwelle von Sommerlüftung und Nachtauskühlung ist die feste 23 °C
(3.6). Heiz- und Kühlperiode dürfen sich überschneiden — dort gilt die stündliche Prüfung θ_K ≥ θ_H + 1 K (F17) — oder
eine Übergangszeit ohne beide lassen. Leer heißt ganzjährig wie heute; eine Zone erbt die Saison je Zelle vom Gebäude
(3.4).

**Abgeleitet bis angelegt.** Jede Größe ist **abgeleitet** (keine Kalenderzeile; der Lauf erzeugt den Kalender aus der
Matrix) oder **angelegt** (Zeilen; die Matrix ist dann nur Vorgabe, die Karte sagt es). „Kalender anlegen" schreibt
den Generator in Zeilen; „Verwerfen" löscht sie und kehrt zur Matrix zurück. **„Matrix erneut anwenden"** auf einen
angelegten, geänderten Kalender ersetzt nach P12 (a) nur den **Matrixbereich** — Standardwoche, Ferien- und
Saisonperioden —; eigene Perioden und Ausnahmetage bleiben. Die Rückfrage kommt vorher und nennt, was ersetzt wird und
was bleibt; dieselbe Regel gilt, wenn eine Vorlage auf einen angelegten Kalender übernommen wird (3.5). **Ein angelegter, nicht von Hand geänderter
Kalender folgt der Matrix (E56):** Solange sein Matrixbereich dem Generator gleicht, übernimmt er jede Änderung der
Matrix ohne Rückfrage, eigene Perioden bleiben; erst nach einer Handänderung gilt P12 mit Knopf und Rückfrage.

**Bitgleich**, weil nur Werte kopiert werden und der Wochentag aus derselben Maske kommt: Sind die neuen Zellen leer —
in allen Bestandsdaten —, liefert der Generator den heutigen Sollwertfahrplan, den konstanten Kühlsollwert, den
konstanten Luftwechsel und die konstanten Gewinne. Die Invariante „Anlegen ändert keine Reihe" halten drei Regeln: (1)
**Rundlauf** — der Schreiber setzt bis zu vier Nachkommastellen, „Anlegen" prüft je Wert Wert → Text → Wert bitgleich
und lehnt sonst benannt ab; (2) **Zonenwerte bleiben** (F2) — legt ein Gebäude seinen Kalender an, während Zonen eigene
Zellen tragen, legt derselbe Schritt deren abgeleitete Kalender mit an; (3) **Energie bleibt** (P1). **Wache:** Der
Generator wird gegen `Sollwertfahrplan` und `SollwertfahrplanMitProfil` gehalten — über **alle Gebäudezeilen der
Testdatenbank** (heute 304: 275 Katalog, 29 Projekt, darunter die 49 Sätze mit aktiven Ferien und die 51 mit
Wochenendwert), über die Grenzfälle (Nachtzeit über Mitternacht, Ferien über den Jahreswechsel, 0 und 366, Ferienmerker
um 0,9, Feriensollwert unter 1 °C, auch mit gesetztem Merker bei einem Wert über 0, Wochenende um und genau 5 °C,
Wochenprofil mit Ferien, Fehlerfälle mit demselben Fehlergrund) und über **alle sieben Wochentage des 1. Januar**.

**Kühlen/Nacht (P13).** `Kuehl_Sollwert_Nacht` ist die Zelle Kühlen/Nacht und wirkt wie jede Zelle über den Generator.
P7 (a) gilt im Kern weiter — das Zeitprofil der Kühlung kommt mit KP1 im Kühlkalender —, nur der Wortlaut „bleibt
ungelesen" entfällt; wo die Spalte heute gefüllt ist, ändert sich der Kühlfahrplan (Testdatenbank: nirgends, R14).

### 3.4 Vererbung Gebäude → Zone, Katalog → Projekt

**Matrix je Zelle (F2):** Jede Zelle der Zonenmatrix darf leer sein und heißt dann „wie das Gebäude" — die
Vorgabenkaskade von heute (`EPOS.Kern/Allgemein/Zonenvorgaben.cs`), jetzt für alle Zellen samt Nachtfenstern; die
Ferienzeiträume kommen vom Gebäude. **Kalender je Größe:** Je Größe gilt die erste Quelle: (1) Kalender der Zone
angelegt; (2) Kalender des Gebäudes angelegt; (3) abgeleitet aus der wirksamen Matrix der Zone. Die Zone erbt **den
ganzen Kalender** oder führt einen eigenen; „vom Gebäude übernehmen und anpassen" legt eine Kopie an; Vererbung einzelner
Perioden gibt es nicht. Anteilskalender multiplizieren den Nennwert der Zone (eigener Wert oder Flächenanteil). Eine
Zone mit eigener Nachtzeile oder eigenem Heizkalender hat eigene Zeiten, Ferien und Feiertage (N1.56 Festlegung 1 gilt
für sie nicht); einen Kühlkalender der Zone gibt es erst mit KU3 (bis dahin E49 A4 (a)); unbeheizte Zonen haben weder
Heiz- noch Kühlkalender (N1.56 Festlegung 2), wohl aber Lüftung, Geräte und Personen. **Nutzungszeit (F16):** Die
Kennzahlen der Nutzungszeit folgen dem Personenkalender (Anwesenheit > 0), wenn einer gilt, sonst der Nachtzeit wie
heute; Rampenstunden zählen nicht. Für das Gebäude zählt eine Stunde, in der eine beheizte Zone in Nutzung ist (Muster
N1.56 Nr. 10). **Aufheizzeit manuell (4.6):** Die Zone erbt den Wert ihres Gebäudes immer; ein eigenes Zonenfeld gibt es
nicht, unbeheizte Zonen rampen nicht.

**Katalogbauten (P3).** Ein Katalogbau (`Tab_Gebaeude_STAMM`) führt Matrix und Kalender nach denselben Regeln, ohne
Zonen. Die Übernahme ins Projekt **kopiert** seine Vorgabezellen und angelegten Kalender samt Perioden an das
Projektgebäude; danach sind Katalog und Projekt unabhängig — der Lauf liest nie den Katalog, und eine spätere Änderung
im Katalog erreicht ein Projekt nur über eine erneute Übernahme. Der ausgelieferte Katalogbau trägt das Schloss
(`ReadOnly`); es sperrt auch Matrix und Kalender.

### 3.5 Vorlagen je Größe und Werkzeuge der Karte

**Wohin die Voreinstellungen gehen.** Die Matrix, die Vorlagen und drei Werkzeuge der Karte übernehmen die
Voreinstellungen der Rev. 1 (F3 ist durch den Auftrag vom 26.09.2026 ersetzt):

| Rev. 1 | Rev. 2 und 3 |
|---|---|
| V1 Standardfahrplan übernehmen | „Kalender anlegen" aus der Matrix |
| V2 Nachtabsenkung, V3 Wochenendabsenkung, V5 Ferien | Matrixzeilen Nacht, Wochenende und Ferien samt Ferienzeiträumen |
| V4 Heizperiode, V10 Kühlperiode | Matrixzeile Saison der Heiz- und der Kühlspalte mit Datum für Start und Ende (F20, E53) |
| V11 Nachtanhebung Kühlung, V12 Betriebszeiten | Nachtzeilen der Kühl-, Lüftungs- und Lastspalten mit eigenen Zeiten (F19) |
| V7 Wohnen, V8 Büro, V9 Schule | ausgelieferte Vorlagen je Größe (P4, P11, F22) |
| V14 von Gebäude, Zone oder Katalogbau übernehmen | „Als Vorlage speichern" und die Auswahlliste der Kalenderkarte |
| V6 Feiertage, V13 Zeitstruktur übernehmen | Werkzeuge der Karte, dazu das Zeitfenster „Tage, von, bis, Wert" |

**Eine Vorlage ist ein vorbefüllter Kalender einer Größe** (P11, abweichend von der Empfehlung). Je Größe — Heizen,
Kühlen, Lüftung, Geräte, Personen — gibt es eine eigene Liste, aus der der Anwender einen Kalender wählt; Sätze aller
fünf Größen gibt es nicht. Eine Vorlage trägt die Nutzungszeilen der Spalte ihrer Größe in der Matrix (Tag, Nacht mit
Zeiten, Wochenende, Ferien), **nicht Nennwert und Saison** — beide gehören dem Objekt und bleiben beim Ziel (E54) —, und, wo sie aus einem angelegten Kalender stammt, dessen Standardwoche und eigene Perioden
samt Feiertagsregeln. **Datierte Ferien trägt sie nicht:** Die Ferienzeiträume gehören dem Gebäude und gelten für alle
Spalten; die Vorlage bringt nur den Wert der Ferienzeile mit. Vorlagen stehen im Katalog, projektübergreifend:
**ausgeliefert** (`ReadOnly`, gesperrt, gesät mit KP2) und **eigen** (erstellt vom Anwender).

**Übernehmen.** Die Auswahlliste der Kalenderkarte zeigt die Vorlagen ihrer Größe, zu jeder die Vorschau an den
Ferienzeiträumen des Ziels. „Übernehmen" wirkt auf Gebäude, Zone oder Katalogbau und nur auf diese Größe: Die Zellen
der Vorlage gehen in die Matrixspalte — eine leere Zelle der Vorlage lässt die des Ziels, wie sie ist —, und der
Kalender der Größe wird angelegt: der Generator mit den Ferienzeiträumen des Ziels, darüber Woche und Perioden der
Vorlage; die Karte vermerkt die Herkunft („aus Vorlage Büro"). Trägt das Ziel schon einen angelegten Kalender dieser
Größe, gilt die Regel von P12: Nach einer Rückfrage wird nur der Matrixbereich ersetzt — Standardwoche, Ferien- und
Saisonperioden —; eigene Perioden und Ausnahmetage des Ziels bleiben, die Perioden der Vorlage kommen dazu, eine
Feiertagsregel nur einmal.

**„Als Vorlage speichern"** legt aus der Karte einer Größe — Matrixspalte und, falls angelegt, Kalender eines
Gebäudes, einer Zone oder eines Katalogbaus — eine eigene Vorlage dieser Größe an, ohne Nennwert und Saison (E54). Eigene Vorlagen lassen sich
umbenennen und löschen, ausgelieferte nur duplizieren. Übernommen ist kopiert: Eine spätere Änderung der Vorlage
erreicht kein Gebäude.

**„Kopieren nach …"** legt aus einer Vorlage — auch einer ausgelieferten — eine eigene Vorlage einer **anderen** Größe
an; die Quelle bleibt, wie sie ist. **Geräte ↔ Personen** reisen direkt: gleiche Einheit (Anteil 0 … 1), Vorgabezeilen,
Standardwoche, Perioden und Feiertagsregeln unverändert. **Heizen → Kühlen** nimmt nur die Zeitstruktur —
Standardwoche, Perioden, Feiertagsregeln, Nacht-, Wochenend- und Ferienzeilen mit ihren Zeiten — und die Aus-Zeiten:
Wo Heizen „aus" ist, ist Kühlen „aus". Jede Zelle, Wochenstunde und Periode, deren Heizsollwert den **Tagwert** der
Vorlage erreicht, bekommt den **Komfortsollwert** (Vorgabe 26 °C); jede mit niedrigerem Heizsollwert — die
**Absenkzeit**: Nacht, Wochenende, Ferien, abgesenkte Stunden der Standardwoche und Perioden — bekommt den
**Absenksollwert** (Vorgabe 28 °C) oder „aus", wie die ausgelieferte Kühlvorlage „Büro" nachts, am Wochenende und in
den Ferien. Der Tagwert ist der Wert der Zeile „Tag"; trägt sie keinen Sollwert, der höchste Heizsollwert der Vorlage
(Zeilen, Grundangabe, Standardwoche, Perioden). Beide Werte sind im Kopierdialog änderbar in den Grenzen der
Kühlspalte; ein Absenksollwert unter dem Komfortsollwert wird benannt abgelehnt — beim Kühlen ist die Absenkung ein
höherer Sollwert. Aus der Heizvorlage „Büro" (Tag 20 °C, sonst 16 °C) wird so eine Kühlvorlage mit 26 °C am Tag und
28 °C bzw. „aus" in allen übrigen Zeiten. Andere Richtungen —
Kühlen → Heizen, alles mit Lüftung — gibt es nicht; still umgerechnet wird nichts. Die Kopie trägt den Namen der Quelle
als Vorschlag, ein Doppelname in der Zielliste wird benannt abgelehnt; die Beschreibung wird übernommen und um die
Herkunft ergänzt („aus Vorlage ‚Büro‘ (Heizen)"), die Nutzung übernommen, `ReadOnly = 0`. Der Inhalt entsteht nur in
der Zielgröße, ohne Nennwert und Saison (E54).

> **Vermerk 05.10.2026 (E90):** Die Nutzung als feste Kennung (Wohnen, Büro, Schule, Sonstige) wird zum Katalog der
> Nutzungsprofile mit Kategorien (EPOS-Muster, DIN V 18599-10, SIA 2024, VDI 2078, eigene) und änderbarer Zuordnung;
> die 14 Vorlagen dieser Tabelle bleiben, die Muster Wohnen, Büro und Schule werden zusätzlich als Profile bitgleich
> abgebildet, Normwerte werden weiterhin nicht ausgeliefert (B15, P4). Ausgearbeitet in
> [Konzept Nutzungsprofile](Konzept_Nutzungsprofile_EPOS-Plan.md) (Stufe NP).

**Ausgelieferte Vorlagen (F22)** — EPOS-Muster mit runden Werten, weder Norm- noch Messwerte. Die Tabelle liest sich
spaltenweise: Jede belegte Zelle ist eine Vorlage in der Liste ihrer Größe, zusammen **14 Vorlagen in fünf Listen** —
die Lüftungsliste führt kein „Wohnen". Leere Zellen bleiben beim Ziel; eine Heiz- oder Kühlperiode tragen die
ausgelieferten Vorlagen nicht, sie hängt vom Ort ab.

| Vorlage | Heizen | Kühlen | Lüftung | Geräte | Personen |
|---|---|---|---|---|---|
| Wohnen | Tag 20 °C, Nacht 18 °C (22–6 Uhr) | Tag 26 °C, Nacht 28 °C | — | 100 % | Tag (7–17 Uhr) 50 %, Nacht 100 %, Wochenende 100 % |
| Büro | Mo–Fr 7–18 Uhr 20 °C, sonst 16 °C, Wochenende und Ferien 16 °C | Tag 26 °C, sonst „aus" | Nacht (18–7 Uhr) und Wochenende 0,1 1/h | Tag 100 %, sonst 10 % | Mo–Fr 8–17 Uhr 100 %, sonst 0 % |
| Schule | Mo–Fr 7–15 Uhr 20 °C, sonst 16 °C, Ferien 16 °C | Tag 26 °C, sonst „aus" | wie Büro | wie Büro | Mo–Fr 8–14 Uhr 100 %, sonst 0 % |

**Saat (E56):** Ausgeliefert wird die vervollständigte Tabelle des [Entwurfs KP2](../ueberholt/2026-09-29_Entwurf_KP2.md)
(Abschnitt 4, 46 Vorgabezeilen): „sonst" heißt Nacht, Wochenende und Ferien; Schule mit eigenen Zeiten (Heizen 7–15,
Personen 8–14 Uhr) statt „wie Büro"; Nachtfenster ausdrücklich. Büro und Schule tragen dazu die neun
bundeseinheitlichen Feiertage „wie Sonntag" als Regeln ohne eigene Woche.

**Werkzeuge der Karte:** das Zeitfenster „Tage, von, bis, Wert" für die Standardwoche, die Feiertage als Regel (F11)
und „Zeitstruktur übernehmen" (Kühlen, Lüftung oder Geräte „wie Heizung" oder „wie Anwesenheit"). Jedes Werkzeug ersetzt
genau seinen Zielbereich; gespeichert werden gewöhnliche Regeln, dazu ein lesbarer Vermerk in `Bemerkung`.

### 3.6 Prüfregeln und Meldungen

- **Streng (H-F10):** genau 168 Zellen je Woche, jede eine Zahl in den Grenzen der Größe oder „aus" (P2); höchstens
  **64 Perioden** je Kalender (EPOS-Wert); Rang eindeutig; Tage 1…365; Feiertagsregel aus der festen Liste. In der
  Matrix: Stunden 0…23, Saisontage 1…365, Lüftung 0…20 1/h, Anteile 0…100 %. Die Grenzen einer Größe stehen an einer
  Stelle des Kerns (`Konditionierungsgroessen.Min/Max`); Zellprüfung, Leser, die Felder der Matrix und der Kalenderkarte,
  der Assistent und die Sollwerte von „Kopieren nach …" nehmen sie. Der Kühlsollwert hat die Plausibilitätsgrenzen
  15…35 °C, dieselben wie der Kühlsollwert des Gebäudes, dessen Wert die Bestandszelle der Kühlspalte trägt.
- **Kühl- über Heizsollwert je Stunde (F17):** θ_K(h) ≥ θ_H(h) + 1 K, wo beide wirken — bei konstantem Kühlsollwert
  genau die heutige Prüfung (`GebaeudeModellEingang.cs:1641`); die Matrix prüft dasselbe je Zeile vor. Die Rampe wird
  **vor** der Prüfung an θ_K(h) − 1 K gekappt, benannt und gezählt; eine Optimierung bricht nie einen Lauf ab.
- **Maximalraumtemperatur (F21):** über dem höchsten Heizsollwert der Nutzungszeit statt über `SollTag`
  (`GebaeudeModellEingang.cs:1797`). Nutzungszeit ist hier und bei den Auslegungswerten die Zeit außerhalb der
  Nachtzeit, nicht die Personenmaske (E55).
- **Sommerlüftung je Stunde:** Schwelle θ_K(h) − 3 K; ist die Kühlung „aus" (+∞), gilt die feste Schwelle 23 °C.
  **Vorlaufstart bei „aus":** Startwert der unbeheizten Zone (Mittel von θ_eq über den Vorlauf, N1.56 Festlegung 7).
- **Auslegungswerte:** Die Auslegungsraumtemperatur der Übergabe fällt auf den höchsten Heizsollwert der Nutzungszeit
  zurück statt auf `SollTag` (`:1237`), die der Kälte auf den niedrigsten wirksamen Kühlsollwert; die
  Auslegungsheizlast nimmt den höchsten Luftwechsel der Nutzungszeit ohne Nachtauskühlung, die Aufheizrechnung den der
  Sprungstunde.
- **Heizperiode (E53):** Liegt die Raumluft einer beheizten Zone außerhalb der Heizperiode in einer Nutzungsstunde
  unter dem Tagwert der Heizspalte, zählt der Lauf die Stunde und nennt die Zahl als Hinweis — die Heizperiode schneidet
  Bedarf ab, den die Raumheizung sonst gedeckt hätte; der Lauf rechnet weiter. Start und Ende gibt es nur zusammen.
- **Meldungen:** `GebaeudeModellFehler.KalenderUngueltig` mit Größe, Periode und Stelle; `SIMENG_KOND_*` im Kern,
  `KOND_MSG_*` im Dialog; Doppelname einer Vorlage benannt abgelehnt.

### 3.7 Nachtauskühlung

**Was sie ist.** Die Nachtzeile der Lüftungsspalte trägt einen **erhöhten Luftwechsel** der Nutzerlüftung in 1/h mit
eigenem Nachtfenster (von–bis, leer = das der Heizspalte); über den Stundenkalender kommt der Wert in genau diese
Stunden. **Bedingt** (P9 (b), E53) wirkt der Überschuss über den Tageswert nur, wenn die Bedingung der Sommerlüftung
erfüllt ist: Raumluft der Vorstunde über der Schwelle (23 °C, mit wirksamer Kühlung — innerhalb der Kühlperiode — θ_K −
3 K) und Außenluft mindestens ΔT kühler (EPOS-Vorgabe 2 K, einstellbar 0–5 K), aus mit 1 K Hysterese, ausgewertet am
Stundenbeginn (Rechenschritte 7.2, F-P4); sonst gilt der Tageswert. Eine unbedingte Nachtauskühlung gibt es nicht (P9):
Sie lüftete auch in Winternächten und höbe die Heizwärme.

**Verhältnis zur Sommerlüftung.** Die Sommerlüftung bleibt eine eigene Regel über den ganzen Tag mit 2,0 1/h (Schalter
`Sommerlueftung`). Wirken beide, gilt der größere Luftwechsel; die Nachtauskühlung ist die Sommerlüftung im
Nachtfenster mit dem Wert des Anwenders.

**Im Kern.** Der Luftwechsel geht mit seinem Jahresminimum in die Ersatzparameter, der Überschuss je Stunde als
Zusatzleitwert (6); die Bedingung entscheidet die Regel am Stundenbeginn wie die Sommerlüftung, je Zone mit deren
eigener Raumluft. Der Zwischenspeicher des freien Falls hält die wenigen Leitwerte (R7).

**Wirkung.** Die Raumluft ist am Morgen kühler: weniger Überhitzungsstunden, mit wirksamer Kühlung weniger Kühlenergie
am Folgetag; die Heizwärme bleibt, solange die Bedingung Winternächte ausschließt. Die Kennzahl
`Nachtauskuehlstunden_H` (Muster `Sommerlueftungsstunden_H`) steht in Ergebnis, Bericht und Export, nur wenn eine
Nachtauskühlung gesetzt ist. Bericht, CSV-Export, KI-Sicht und Variantenvergleich bringt KP3, zusammen mit `Sommerlueftungsstunden_H` (E54).

| Probe | Inhalt | Kriterium |
|---|---|---|
| N-NK1 Byte-Gleichheit | ohne Nachtzeile der Lüftung | byte-gleich |
| N-NK2 Nachtfenster | Reihenprobe mit stets erfüllter Bedingung: Nachtwert genau im Nachtfenster, sonst Tageswert | ohne Ausnahme |
| N-NK3 Bedingt | Schalten wie die Sommerlüftung, nur im Nachtfenster; Winterwoche ohne eine Nachtauskühlungsstunde, Heizwärme unverändert; Sommerwoche mit weniger Überhitzungs- bzw. Kühlstunden | benannte Zähler |
| N-NK4 Zusammenspiel | mit Sommerlüftung gilt der größere Luftwechsel; Zwischenspeicher mit Lüftungskalender | byte-gleich zur Einzelrechnung |

## 4. Aufheizoptimierung

Die Abschnitte 4.1 bis 4.9 beschreiben das Verfahren **Sollwertrampe** (Bestand). Daneben stehen die Verfahren
„Vorheizzeit vorgeben“ und „Vorheizzeit berechnen“ mit Deckel (4.10).

### 4.1 Sprung und Rampe als lineare Treppe

Die Optimierung formt die **fertige** Heizsollwertreihe einer Zone. Ein **Sprung** liegt in der Stunde h_s, wenn
s(h_s − 1) und θ_T = s(h_s) endlich sind und θ_T − s(h_s − 1) > 0,01 K; die **Absenkdauer** D ist die Zahl der
zusammenhängenden Stunden vor h_s mit endlichem Sollwert unter θ_T. Die Rampe beginnt bei θ_N (Ausgangswert, unten),
ΔT = θ_T − θ_N, und belegt mit n Stufen h_s − n + 1 … h_s:

```
s'(h_s − n + j) = max( s(h_s − n + j),  θ_N + ΔT · j/n )      j = 1 … n
t_auf = (n − 1) h ≤ D            n = 1 ist der heutige Sprung — keine Rampe
```

Die **letzte Stufe fällt in die Sprungstunde**: Der Zielwert steht zu Beginn der Nutzung, und n = 1 ändert nichts
(F6). Das `max` senkt nie einen Wert; mehrere Anstiege (17 → 19 → 21 °C) sind je ein Sprung. Linear, weil der Auftrag
einen „sukzessiven Anstieg" verlangt und die Treppe als „+ x K je Stunde" lesbar und geschlossen bemessbar ist.

**Ausgangswert θ_N (E58 F2).** θ_N ist der **kleinste endliche Sollwert im Fenster der Rampe** — den letzten
min(D, t_auf,max + 1) Stunden vor h_s ([Entwurf KP3](../ueberholt/2026-10-02_Entwurf_KP3.md), Festlegung 9). Bei einfacher Nachtabsenkung ist das s(h_s − 1). Liegt
ein Wochenend- oder Ferienwert unter dem Nachtwert, entstehen zwei Anstiege (Mo 0 Uhr 16 → 18 °C, 6 Uhr 18 → 20 °C);
die Massen kommen dann aus dem tieferen Wert, und die Rampe beginnt bei ihm — mit s(h_s − 1) läge die Spitze über
P_auf (Prüfsatz +1,7 %, mit dem kleinsten Sollwert des Fensters n = 10 statt 2).

### 4.2 Die Physik der Spitze

Im 2K-Modell sind Luft und Oberflächen kapazitätslos, Zustand sind die Massen der Außen- und Innenbauteile; die
ideale Regelung hält θ_air = θ_soll, die Leistung ist affin im Zustand (Rechenschritte 4.1, 4.3). Springt der
Sollwert, springen Luft und Oberflächen mit, die Massen nicht. Die Leistung je Kelvin Sprung ist die
**Sprungantwort des geregelten Systems** bei festen Randwerten:

```
g(t) = H_s + r_1·e^(−t/τ_1) + r_2·e^(−t/τ_2)   [W/K]     H_s  stationärer Leitwert (StationaereHeizlastW)
C_k  = r_k·τ_k  Modalkapazitäten [J/K]                    τ_k  Zeitkonstanten des geregelten Falls
C_w  = C_1 + C_2  wirksame Aufheizkapazität               G_0  = H_s + r_1 + r_2  Sprungleitwert;  h = 3 600 s
```

C_w ist die **Überschusswärme je Kelvin** über der neuen stationären Last — das, was eine Rampe verteilen muss. Am
Haus aus Projekt 1045 (`EFH-A-U-347s`, Rechenschritte 9.1/9.2, Prüfmodus-Konvention, konvektive Heizung): τ = 1,02
und 4,54 h, H_s = 971,8 W/K, G_0 = 2 425 W/K, C_1 = 0,24 und C_2 = 5,55 kWh/K, **C_w = 5,78 kWh/K** — weniger als die
Speicherfähigkeit der Bauweise (10,05 kWh/K) und die quasistationäre Speicheränderung (7,54 kWh/K), weil die
Außenmasse nur teilweise mitgeht; mit Strahlungsanteil 0,3 ist C_w = 6,93 kWh/K. Bei −12 °C liegt die Spitze eines
Sprungs 17 → 21 °C ohne Rampe bei 37,04 kW gegen 32,07 kW stationär (+15 %).

### 4.3 Die Stufenformel — Vorab-Fahrplan in einem Lauf

Die Treppe ist eine Summe von n Sprüngen ΔT/n im Stundenabstand; ihre Stundenmittel summieren sich als geometrische
Reihe. Für die Sprungstunde, bei festen Randwerten die größte Stunde der Rampe, gilt aus einem Gleichgewicht bei θ_N
(**Gleichgewichtsform**, der ungünstigste Anfangszustand):

```
Φ̄_n = Φ_stat(θ_T, T_a) + (ΔT / (n·h)) · Σ_k C_k · (1 − e^(−n·h/τ_k))
```

Sie gilt für jeden Strahlungsanteil, bei zusammenfallenden Eigenwerten in der Grenzform wie im Löser (Rechenschritte
5). **Nachgerechnet:** Formel und Stundensimulation stimmen überein (bei 35,3 kW Grenze und −12 °C beide n = 5 mit
35,22 kW im Stundenmittel, auch mit Strahlungsanteil 0,3). Die Absenkform — vorher eingeschwungen bei θ_T, D Stunden auf θ_N — liegt
nie darüber und dient als Probe (N-AH2). **Erste Ordnung als Schranke:** Wegen 1 − e^(−x) ≤ 1 hält
n_F = ⌈C_w·ΔT / (h·(P_auf − Φ_stat))⌉ die Grenze immer — Aufheizzeit = wirksame Kapazität × Hub ÷ Leistungsreserve.
Sie steht in Herleitungszeile und Probenband; bemessen wird mit ihr nicht, denn mit der quasistationären Kapazität
rampt sie am Haus aus 1045 10 h statt 4 h und an milden Tagen 2–3 h ohne Bedarf.

**Augenblick oder Stundenmittel (E58 F1).** Der Löser kappt `Heizleistung_Max` am **Augenblickswert**: Er wählt
den begrenzten Fall, sobald die Leistung am Beginn eines Abschnitts die Grenze übersteigt. Mit Quelle Grenze (4.4)
bemisst deshalb die **Augenblicksform** — die Leistung am Beginn der Sprungstunde, bei festen Randwerten das Maximum
über die Rampe —, mit Zielleistung das Stundenmittel Φ̄_n, denn die Zielleistung ist eine Stundengröße des Erzeugers:

```
Φ̂_n = Φ_stat(θ_T, T_a) + (ΔT / n) · z · (Σ_{m=0…n−1} Φ(h)^m) · v        Quelle Grenze
Φ̄_n = Φ_stat(θ_T, T_a) + (ΔT / (n·h)) · z · Γ(n·h) · v                  Zielleistung (= Gleichgewichtsform oben)
```

z ist die Ausgangszeile, Φ und Γ sind die Matrixfunktionen des geregelten Falls (über `Uebergangsrechner.Bei`), v =
A⁻¹·ΔB ([Entwurf KP3](../ueberholt/2026-10-02_Entwurf_KP3.md), Abschnitt 2). Am Prüfsatz hält die Grenze 35,3 kW mit n = 7 statt 5; die Sprungstunde kappt bei festen
Randwerten nicht.

**Ablauf (F7):**

```
je Zone einmal:  H_s, τ_k, C_k des geregelten Falls zum Strahlungsanteil (Eigenwerte und Residuen)
je Sprung h_s:   T_a = kleinste Außenlufttemperatur in [h_s − n_max, h_s]
                 Φ_stat = StationaereHeizlastW(θ_T, T_a, θ_eq = T_a, Erdreich des Tages, Nachbarn nach 4.7)
                 n = kleinstes n ≥ 1 mit Φ_n ≤ P_auf,   n ≤ min(n_max, D + 1)
                 Φ_n = Φ̂_n bei Quelle Grenze, Φ̄_n bei Zielleistung (E58 F1)
```

Sonne und innere Gewinne bleiben in der Bemessung außen vor; sie verkürzen nur, die Formel irrt zur sicheren Seite.
Die Rechnung ist deterministisch, kostet unter 1 ms je Zone und läuft vor dem Lauf im Eingangsbauer; die Zonenschleife
läuft einmal, **ein Zweitlauf entfällt**. Die **Überlagerung** auf einem Lauf ohne Rampe scheidet aus: Sie ist exakt
nur über durchgehend geregelte Stundenfolgen, bei gesetzter `Heizleistung_Max` rechnet der gekappte Vergleichslauf das
freie System, die Vorhersage wird zu niedrig und n = 1 hielte das Kriterium trivial; dazu kämen zwei bis vier Läufe,
in Mehrzonengebäuden 1–4 s je Gebäude. Überlagerung und Vorausrechnung auf einer Kopie bleiben **Prüforakel**.

**Nachweisband im Lauf (W3).** Im Fenster [h_s − n + 1, h_s + 2] gilt bei Quelle Grenze „Kappungsanteil > 0", bei
Zielleistung „Stundenleistung > 1,01 · P_auf" als Hinweis mit Tageszahl (E58 F1). Dafür liefert `Schritt` den Kappungsanteil von `Heizleistung_Max` auch
im idealen Fall — ein neuer Ausgang, keine geänderte Zahl.

### 4.4 Die Aufheizleistung P_auf

P_auf ist die Leistung, die das Aufheizen höchstens beanspruchen darf — **eine feste Zahl je Gebäude bzw. Zone**,
nicht je Tag. Die erste zutreffende Quelle gilt (P5): (1) **`Heizleistung_Max`**, wenn gesetzt (Zone: eigener Wert
oder Flächenanteil; beim Skalieren nach E8 dieselbe Regel) — die Rampe verhindert dann die Kappung nach dem Sprung;
(2) die **Zielleistung** P_auf = (1 + ρ) · Φ_stat(θ_T,max, T_a,min) mit dem höchsten Heizsollwert der Nutzungszeit und
der **kältesten Stunde** und der **Aufheizreserve ρ**, einer Eingabe im Projekt (1–100 %, gespeichert als Anteil 0 < ρ ≤ 1);
bleibt sie leer, gilt **20 %**, und der Lauf meldet es (E64, unten).

Der Anker an der kältesten Stunde macht (a) immer erreichbar, (b) verlängert die Zeit wie gewollt, und das Tagesmittel
mischt sich nicht in eine Stundenbemessung. Der verworfene Anker H10 hätte P_auf im Referenzklima **unter** die Last der
kältesten Stunde gelegt — bei temperaturproportionaler Last um rund 2 % in (a) und 7 % in (b), weil die kälteste
Stunde 22–23 % mehr Last trägt als das H10-Tagesmittel. Eine Spalte `Aufheizleistung` braucht es nicht, die
Nennleistung der Übergabe entfällt als Quelle, solange AK1-Gebäude ausgenommen sind (F13), und eine je Tag
mitwandernde Grenze widerspräche dem Wortlaut „maximale Aufheizzeit bei niedrigster Außentemperatur".

**Probe KP0 (R2), abgeschlossen 27.09.2026.** Vor KP1 ist ρ an allen 17 Gebäuden der fünfzehn Referenzprojekte
geprüft — Überhöhung der Sprungspitze an der kältesten Stunde, t_auf,max in (a) und (b) für den Standardsprung
(Nachtabsenkung 18 → 20 °C), Tabelle und Herleitung im
[Protokoll](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-27_KP0_Aufheizleistungsprobe.md); 4.5 zeigt, warum
sie nötig ist. **Befund:** An allen 16 gerechneten Gebäuden (Projekt 1040 bleibt auf dem Tagesbilanz-Weg ohne
Aufheizrechnung) hält ρ = 20 % eine positive Reserve — Bemessung (a) braucht nirgends eine Rampe (t_auf,max = 0 h
an jedem Gebäude), Bemessung (b) höchstens drei Stunden, keines der beiden ist unerreichbar; die Überhöhung der
Sprungspitze reicht von 5,1 % bis 18,9 % (Median 13,9 %), am größten am langsamsten Gebäude mit der höchsten
Speicherfähigkeit der Bauweise. **Festlegung nach Empfehlung, Widerspruch bis zur Beauftragung von KP1
möglich: ρ bleibt bei 20 %.** Ein größerer Sprung als der Standardsprung (Wochenend- oder Ferienabsenkung mit
mehr als 2 K) ist von dieser Probe nicht erfasst und hebt die Überhöhung überproportional an; das bleibt eine
Beobachtung für KP1 ff., kein Anlass, den Startwert vorab zu ändern.

**Reserve: Messung und Vorgabe (E58 F7, E64).** Die Probe KP0 rechnete konvektiv; der Kern rechnet jede Gebäudezeile
ohne Angabe mit dem Strahlungsanteil 0,3, und damit liegt die Überhöhung der Sprungspitze um 20–31 % höher. Die Welle RP1
hat deshalb ρ_min aller VDI-Gebäude der Referenzprojekte mit dem echten Kern gemessen (Kriterium W1 = 0 und W3 = 0;
[A/B-Protokoll](../ueberholt/Protokolle/Gebaeudesimulation/2026-10-03_KP3_RP1_AB-Protokoll_1051.md) Abschnitt 8): Das
größte ρ_min liegt bei 8,75 % (1051), die kleinste Reserve mit erreichbarem Bemessungsfall bei 13,75 % (1051, Bemessung
(b)); bei 20 % sind W1 und W3 überall 0. Bemessung (b) ändert P_auf nicht, sie prüft nur die Erreichbarkeit bei
T_a,min − ΔT_K. **Die Reserve ist Nutzereingabe ohne pauschale Programmvorgabe** (E64): Ist `Aufheiz_Reserve` leer, rechnet
der Lauf mit 20 %, meldet einmal je Lauf `SIMENG_AUFH_RESERVE_VORGABE` („Aufheizreserve nicht vorgegeben; es gelten
20 %.") und die Herleitungszeile nennt „Reserve 20 % (Vorgabe)"; eine eingegebene Reserve steht dort ohne Zusatz. Das
Referenzprojekt 1051 rechnet mit leerer Reserve (10.2).

### 4.5 Bemessung (a) und (b)

Bemessung (a)/(b) mit ΔT_K gilt nur für das Verfahren Sollwertrampe; in den Verfahren mit Deckel (4.10) ersetzt die
Jahresbetrachtung den Bemessungsfall.

| Variante | T_a,B | Rolle |
|---|---|---|
| **(a) kälteste Stunde** *(Vorgabe)* | Minimum der Außenluft über die Stunden mit Heizsollwert — ohne Heizperiode alle 8 760 Stunden der Zeitbasis des Laufs (4.7) | Auftrag a) |
| (b) kälteste Stunde − ΔT_K | ΔT_K **EPOS-Vorgabe 2 K**, 0–10 K | Auftrag b); Reserve für Jahre kälter als das Klimajahr |
| kältestes Tagesmittel | `KaeltesterTag` (H10) | nur Anzeige, zum Abgleich mit AK1 |

**Bemessungsfall** ist die Gleichgewichtsform mit dem größten Anstieg ΔT_max = θ_T − θ_N des Heizkalenders bei
T_a,B, θ_N nach 4.1 (E58 F2): t_auf,max = n_max − 1 mit dem kleinsten n_max ≤ 48, für das Φ_n ≤ P_auf (Φ_n nach 4.3,
E58 F1); sonst „nicht erreichbar" (W1). ΔT_K gilt
**nur für die Höchstzeit** (F18). Bei fester P_auf ist die Aufheizzeit am kältesten Punkt am längsten; t_auf,max ist
Bemessungsgröße, Anzeige und Obergrenze der täglichen Zeit, als Wache mit Zähler (erwartet: nie).

**Zahlenbeispiel mit der gewählten Vorgabe** — P_auf = 1,2 × Φ_stat an der kältesten Stunde (Klimaregion von Projekt
1045: −18,2 °C); Sprung 17 → 21 °C nach 8 h Absenkung (D = 8); Stufenformel in der Prüfmodus-Konvention der
Rechenschritte 9 (Außenluft als einzige Randtemperatur, ohne Sonne und Gewinne). Daneben eine gedämmte Variante
desselben Hauses (Restleitwert der Außenwand geviertelt, Leitwert am Luftknoten halbiert):

| Größe | Haus aus 1045 | gedämmte Variante |
|---|---|---|
| H_s, C_w, τ_1/τ_2 | 971,8 W/K, 5,78 kWh/K, 1,02/4,54 h | 402,6 W/K, 8,33 kWh/K, 1,34/4,99 h |
| Φ_stat(21 °C) an der kältesten Stunde → P_auf | 38,1 kW → **45,7 kW** | 15,8 kW → **18,9 kW** |
| (a) −18,2 °C: Spitze ohne Rampe → t_auf,max | 43,0 kW (+13 %) → **0 h** | 22,3 kW (+41 %) → **8 h**, Spitze 18,9 kW (−15 %) |
| (b) −20,2 °C: Spitze ohne Rampe → t_auf,max | 45,0 kW (+12 %) → **0 h** | 23,1 kW (+39 %) → **13 h**, begrenzt auf D = 8 h: 19,7 kW (W2) |
| täglich bei −16 / −14 / −12 / −10 / −8 °C | keine Rampe | 5 / 3 / 2 / 1 / 0 h |
| erste Ordnung in (a), zum Vergleich | 3 h | 10 h |

**Lesart.** Das unsanierte Haus braucht mit dieser Vorgabe **keine** Rampe: Seine Sprungspitze liegt 12–16 % über der
stationären Last, die Reserve beträgt 20 %. Die Optimierung wirkt in gut gedämmten Bauten, wo der relative Überschuss
groß ist, und wo `Heizleistung_Max` knapp gesetzt ist. In der gedämmten Variante füllt die Rampe an der kältesten
Stunde die ganze Nacht, in (b) reicht die Nacht nicht — die Absenkung ist dort weitgehend wirkungslos, und der Hinweis
sagt es.

### 4.6 Täglich, Deckel und Begrenzung

Die Arten „täglich“ und „fest“, der Deckel n ≤ 48 und der Aufschlag gelten nur für das Verfahren Sollwertrampe; der
Leistungsdeckel P_K der Verfahren mit Deckel ist ein anderer Begriff (4.10).

- **„täglich"** *(P6, Vorgabe)*: je Sprung n nach 4.3 mit der kleinsten Außenlufttemperatur im Fenster, ohne ΔT_K,
  begrenzt auf min(t_auf,max + 1, D + 1); an milden Tagen ist n = 1, keine Rampe, keine Mehrwärme. **„fest"** *(P6,
  wählbar)*: jede Rampe n = min(t_auf,max + 1, D + 1) — eine feste Vorhaltezeit, mehr Wärme an milden Tagen.
- **Deckel** n ≤ 48 (EPOS-Wert). **Absenkdauer** n − 1 ≤ D; verlangt die Formel mehr, füllt die Rampe die Absenkung,
  und der Tag zählt als begrenzt (W2).
- **Rundung (F8):** das kleinste haltende n, also Aufrunden; keine Mindestrampe, denn 1 h Vorlauf mit dem Zielwert
  verschöbe den Sprung nur und senkte die Spitze nicht.
- **Individuell je Gebäude:** n folgt je Gebäude und Zone aus eigenen Massen, eigenem Φ_stat und eigener P_auf (4.3,
  4.4); pauschal je Projekt sind nur ρ, die Art und der Aufschlag.
- **„manuell"** *(am Gebäude)*: Ein Gebäude mit einer manuellen Aufheizzeit t_m (`Aufheizzeit_Manuell_H`, 1–47 h) rampt
  vor jedem Sprung mit n = t_m + 1, begrenzt auf D + 1 und den Deckel; t_m ersetzt auch die Fenstergrenze (W = min(D, t_m + 1),
  4.3); ein Gebäude ohne Wert folgt der Art des Projekts.
  Die Bemessung läuft weiter und liefert Vorschlag und Herleitungszeile (4.8, 7.6), entscheidet aber nicht; der Aufschlag
  gilt nicht. Zonen erben den Wert ihres Gebäudes (3.4), unbeheizte Zonen rampen nicht; ein gekoppeltes Einzonengebäude
  bleibt nicht optimiert (W5).
- **Aufschlag** *(Projekt, kalenderbezogen)*: Er verlängert nur Rampen, die ein Sprung des Heizkalenders auslöst und
  die schon eine Rampe sind — ermitteltes n > 1 der Arten täglich und fest: n' = min(48, n + max(A_h, ⌈n · A_% / 100⌉))
  mit dem Aufschlag A_h in Stunden (0–24) und A_% in Prozent (0–100); es gilt der größere der beiden. An Sprüngen ohne
  Rampe (n = 1), an Tagen ohne Sprung und auf die manuelle Aufheizzeit wirkt er nicht; „täglich" behält an milden Tagen
  seine Bedeutung. Danach begrenzt die Absenkdauer wie bei jedem n (n' − 1 ≤ D, W2 zählt mit n'); die Rampe wird mit n'
  nach 4.1 geschrieben, die Sprungstunde nie, gekappt an θ_K − 1 K. t_auf,max bleibt die bemessene Zeit ohne Aufschlag.
  Ohne Aufschlag (beide leer oder 0) rechnet die Rampe bitgleich wie ohne die Felder.

### 4.7 Wechselwirkungen

- **Kappung durch `Heizleistung_Max`:** P_auf ist die Grenze, die Rampe verhindert die Kappung nach dem Sprung.
  Übersteigt schon die stationäre Last die Grenze, ist der Sprung unerreichbar: n = D + 1, Zähler W1.
- **Kühlung (F17):** Die Rampe bleibt zwischen θ_N und θ_T und wird an θ_K(h) − 1 K gekappt (3.6). **Vorkühlen** vor
  einem fallenden Kühlsollwert ist dieselbe Rechnung gespiegelt und kommt mit KU3 (F10, KP3b).
- **Mehrzonen:** Jede Zone hat eigene H_s, τ_k, C_k, P_auf und Rampen. Im Bemessungsfall tragen beheizte Nachbarzonen
  ihren Sollwert vor dem Sprung, unbeheizte ihren Startwert nach N1.56 Festlegung 7; dass Nachbarn mitrampen, senkt die
  wahre Leistung — sicher (N-AH9). Unbeheizte Zonen bekommen keine Rampe.
- **Anlagenkopplung (F13):** Übergabe und P-Regler sind nichtlinear, die Übergabe kappt die Spitze ohnehin; gekoppelte
  Einzonengebäude werden in KP3 **benannt nicht optimiert** (W5). KP3b bringt sie mit der Vorausrechnung und dem
  Ankunftskriterium θ̄_air(h_s) ≥ θ_T − 1 K (Schwelle wie H5). Mehrzonengebäude rechnen nach E49 A4 (a) ideal und
  bekommen die Rampe.
- **„aus" (W4):** Ein Übergang von „aus" auf einen Wert bekommt keine Rampe — der Anfangswert der frei schwingenden
  Zone ist vor dem Lauf unbekannt, ihn zu kennen verlangte einen Zweitlauf (die verworfene Option P2 (c)); der Sprung
  wird gezählt.
- **Heizperiode (E53):** Innerhalb rampt die Optimierung wie ohne Heizperiode, außerhalb gibt es weder Sollwert noch
  Rampe. Der **Beginn der Heizperiode** ist ein Übergang aus „aus": keine Rampe, W4 zählt ihn — einmal im Jahr je Zone;
  die frei schwingende Zone liegt zu dieser Zeit meist nahe am Sollwert, die Spitze bleibt klein. Beginnt die
  Heizperiode in einer Absenkung, zählt die Absenkdauer D des ersten Morgens ab 00:00 des Starttags. Das **Ende** ist
  ein Sprung nach unten und braucht keine Rampe. T_a,B und der Anker von P_auf (4.4, 4.5) nehmen die kälteste Stunde
  **innerhalb** der Heizperiode — ohne Heizperiode die kälteste Stunde des Jahres. Die Kühlperiode berührt die Rampe nur
  über die Kühlkappung (F17): Außerhalb ist θ_K = +∞, die Kappung entfällt.
- **Nachtauskühlung:** Sie wirkt nur bedingt (P9) und lüftet nicht, solange die Raumluft unter der Schwelle liegt — in
  Heiznächten also nicht; mit einer Rampe trifft sie praktisch nicht zusammen.
- **Lüftung, Skalierung:** Der geregelte Fall hängt vom Lüftungsleitwert nicht ab (Rechenschritte 4.5), die
  Modalgrößen also auch nicht; Φ_stat nimmt den Luftwechsel der Sprungstunde. Die Rampe ist Teil der Sollwertreihe und
  entsteht in jedem Lauf der Verhältnisrechnung gleich (E8, H7). Nutzungszeit, Altweg und Normtestfälle bleiben
  unberührt.

### 4.8 Ergebnisse und Hinweise

| Ergebnis | je | Ablage |
|---|---|---|
| t_auf,max mit T_a,B und Variante | Gebäude, Zone | `Aufheizzeit_Max_H`, `Aufheiz_Aussen_C` |
| P_auf samt Quelle (Grenze oder Zielleistung) | Gebäude, Zone | `Aufheiz_Leistung_Kw`, `Aufheiz_Leistungsquelle` |
| Rampentage, Σ Aufheizstunden, längste Rampe | Gebäude, Zone | `Aufheiztage`, `Aufheizstunden_H`, `Aufheizzeit_Laengste_H` |
| begrenzte und unerreichbare Tage | Gebäude, Zone | `Aufheiztage_Begrenzt`, `Aufheiztage_Unerreichbar` |
| Stunden mit Kappung durch `Heizleistung_Max`, auch ohne Kopplung | Gebäude, Zone | `HeizleistungMax_H` |
| Sollwertreihe mit Rampe | Stunde | vorhandene Reihe `Heizsollwert` (`GebaeudeModellErgebnis.cs:70`) |

Die Spalten sind nullbar (NULL = Schalter aus); der Export schreibt die neuen Kennzahlen **nur bei wirksamem Schalter**
(Muster E32). Den Vergleich mit und ohne Rampe liefert eine Projektvariante (P8).

**Art im Ergebnis.** Die Ergebnisspalte `Aufheiz_Art` (Gebäude und Zonen; die Zone erbt die Art ihres Gebäudes) trägt
die wirksame Art `TAEGLICH`, `FEST` oder `MANUELL`; `Aufheiz_Bemessung` behält bei jeder Art die Variante der Bemessung.
Rampt ein Gebäude mit manueller Aufheizzeit, steht `Aufheizzeit_Max_H` = t_m (Gebäude und Zonen), Zustand BEMESSEN;
T_a,B, P_auf und Quelle bleiben die der Bemessung. Der Export nennt `Geb[n].Aufheizart` bei wirksamem Schalter und
`Geb[n].Aufheizzeit_Manuell` nur, wenn der Wert gesetzt ist. Die
**Herleitungszeile** der Bemessung (ohne Jahreslauf) bleibt bei jeder Art stehen; sie ist zugleich der Vorschlag für die
manuelle Aufheizzeit (7.6) und nennt bei gesetztem Wert beide Zeiten. Rampentage, Aufheizstunden, längste Rampe, W2 und
W3 zählen die tatsächlich geschriebenen Rampen, also mit Aufschlag bzw. manuellem Wert.

**Auslegungsgröße der Heizung.** Für die Auslegung gilt nicht das Maximum der idealen Last, sondern
Φ_HL + Φ_RH: die stationäre Auslegungsheizlast Φ_HL plus der **Aufheizzuschlag** Φ_RH = max(0, P_auf − Φ_stat(θ_T,max,
T_a,B)) aus der Bemessung, nach dem Muster des Aufheizzuschlags der DIN EN 12831-1. P_auf enthält die stationäre Last am
Bemessungspunkt schon; der Zuschlag ist bei Quelle Ziel ρ · Φ_stat, bei Quelle Grenze `Heizleistung_Max` − Φ_stat, bei
W1 null; skaliert wird er wie P_auf, am Gebäude mit Zonen als Summe der beheizten Zonen, ein gekoppeltes Gebäude bekommt
keinen. Die Ergebniszeile des Gebäudes trägt `Auslegungsheizlast_Kw` und `Aufheizzuschlag_Kw`. Bedarfsdialog und Bericht
zeigen die Auslegungsgröße mit ihren Teilen und daneben ideale Spitze (`SpitzeKw`), Tagesmittel (`SpitzeTagesmittelKw`)
und P_auf mit Quelle (`Aufheiz_Leistung_Kw`, `Aufheiz_Leistungsquelle`) und sagen, dass die ideale Spitze bei Schalter
aus keine Auslegungsgröße ist. Ein Filter auf der Lastreihe gehört nicht in den Rechenweg
([Konzept Heizlastspitzen](Gebaeudesimulation/2026-10-03_Konzept_Heizlastspitzen_Glaettung.md)); die Rampe senkt die
Spitze geschlossen geregelt.

**Auslegungsheizlast ohne Anlagenkopplung (E97).** Φ_HL ist die stationäre Last am Auslegungspunkt, aus der die
Anlagenkopplung ihre Nennleistung herleitet, und entsteht auch ohne Kopplung — sonst trüge kein optimiertes Gebäude eine
Auslegungsgröße, denn ein gekoppeltes bemisst nicht. Es gilt derselbe Ausdruck mit demselben Auslegungstag und derselben
Auslegungs-Außentemperatur (Feld, sonst das kälteste Tagesmittel abgerundet); jede beheizte Zone steht an ihrer
Auslegungsraumtemperatur (3.6), unbeheizte Nachbarn an der Auslegungs-Außentemperatur. Ohne Kopplung gibt ein Feld außerhalb
seiner Grenzen keine Zahl statt eines Fehlers. Ergebniszeile, Auskunft und Ergebnisexport (`Geb[n].AuslegungsheizlastKw`,
`Geb[n].AufheizzuschlagKw`) tragen beide Teile.

| Hinweis (benannt; Protokoll, Bedarfsdialog, Bericht; der Lauf rechnet weiter) | Kriterium |
|---|---|
| **W1** Aufheizleistung reicht nicht | P_auf ≤ Φ_stat(θ_T,max, T_a,B); dazu die Zahl der Tage mit P_auf ≤ Φ_stat bei T_a des Tages |
| **W2** durch die Absenkdauer begrenzt — „Absenkung weitgehend wirkungslos" | t_auf,max ≥ kürzeste regelmäßige Absenkdauer, dazu die Tage mit n − 1 = D und größerem Bedarf; nur aus der Stufenformel |
| **W3** Nachweisband | im Fenster [h_s − n + 1, h_s + 2] bei Quelle Grenze Kappungsanteil > 0, bei Zielleistung Leistung > 1,01 · P_auf (Tageszahl; E58 F1) |
| **W4** Übergang aus „aus" ohne Rampe | Zahl der Sprünge aus NaN, darunter der Beginn der Heizperiode (4.7) |
| **W5** gekoppeltes Gebäude nicht optimiert | wirksame AK1 im Einzonenweg, bis KP3b; gilt nur für das Verfahren Sollwertrampe — die Verfahren mit Deckel beziehen AK1-Einzonengebäude ein (4.10) |

**Schalter (F9):** Projekteinstellung `Aufheizoptimierung`, **Vorgabe aus** — keine Rampe, keine neue Rechenoperation,
keine neue Kennzahl. Grenzfallprobe (Muster Anlagenkopplung 3.7): Schalter an, P_auf = +∞ — überall n = 1, byte-gleich
zu „aus". In einem Referenzprojekt gesetzt, friert der Schalter die Basis neu ein (10.3).

### 4.9 Nachweise der Aufheizrechnung

Die Kürzel heißen N-AH, weil N-A1 bis N-A9 im Anlagenkopplungskonzept vergeben sind.

| Probe | Inhalt | Kriterium |
|---|---|---|
| N-AH1 Identität | Stufenformel gegen `Zonenmodell2K.Schritt` mit Treppe aus dem Gleichgewicht; Parametersätze aller Gebäude der Testdatenbank und synthetische Sätze bis zu schweren, gut gedämmten Bauten; Strahlungsanteil 0 und 0,3; n = 1…24 | relativ ≤ 1·10⁻⁹ |
| N-AH2 Absenkform | dasselbe nach D Stunden auf θ_N aus einem Gleichgewicht bei θ_T | relativ ≤ 1·10⁻⁹; nie über der Gleichgewichtsform |
| N-AH3 Orakel | Vorausrechnung und Überlagerung auf einem synthetischen Jahr mit Tagesgang, Gewinnen, Lüftungskalender und gesetzter `Heizleistung_Max` | Stundenleistung im Fenster ≤ 1,01 · P_auf, wo W3 nicht anschlägt; W3-Tage decken sich mit dem Orakel |
| N-AH4 Schranke | n_F der ersten Ordnung gegen n der Stufenformel | n_F ≥ n ohne Ausnahme |
| N-AH5 Monotonie | n wächst mit kälterer Luft, größerem ΔT und kleinerer P_auf; (b) ≥ (a) | ohne Ausnahme |
| N-AH6 Unerreichbar | P_auf ≤ Φ_stat | W1, n = D + 1, keine Ausnahme |
| N-AH7 Determinismus | zwei Läufe; de-DE gegen en-US (`Kulturvorrichtung`) | byte-gleich |
| N-AH8 Grenzfall | Schalter an, P_auf = +∞ | byte-gleich zu „aus" |
| N-AH9 Mehrzonen | zwei Zonen mit Trennfläche, Formel gegen die volle Zonenschleife | sicher, Band 1 % |
| N-AH10 Ränder | Sprung am 1. Januar, D < n, Übergang aus „aus", Beginn der Heizperiode, Bemessung innerhalb der Heizperiode, Deckel 48, Kühlkappung an θ_K − 1 K, AK1-Gebäude | benannte Ergebnisse W1–W5 |
| N-AH11 Aufschlag | n' nach 4.6 je Rampe mit n > 1 in täglich und fest, Sprünge mit n = 1 ohne Aufschlag, Deckel 48, W2 mit n', t_auf,max unverändert | ohne Ausnahme; Aufschlag 0/0 und Schalter aus byte-gleich |
| N-AH12 manuell | jeder Sprung n = t_m + 1, Zonen erben, kein Aufschlag, Bemessung unverändert, `Aufheiz_Art` = `MANUELL`, Export nur gesetzt | ohne Ausnahme; Schalter aus byte-gleich |

### 4.10 Vorheizen mit Deckel (Fassung 2, E122/E124/E125)

Die Gruppe „Aufheizen vor Nutzungsbeginn“ bietet bei eingeschalteter Optimierung drei **Verfahren** (E122 F1):

- **Sollwertrampe** — der Bestand nach 4.1 bis 4.9, bitgleich.
- **Vorheizzeit vorgeben** (Option 1) — t_V in Stunden je Projekt, Gebäude oder Zone; der Lauf prüft je Sprung die Ankunft
  und nennt die Tage, an denen t_V nicht reicht, mit der nötigen Zeit.
- **Vorheizzeit berechnen** (Option 2) — t_V ist das **95-%-Quantil** des Bedarfs t_nötig über die erreichbaren
  Kalendersprünge (das kleinste t, das mindestens 95 % von ihnen abdeckt; Kern-Konstante, E125 F19 (b)) und gilt
  **fest an jedem Sprung** (E124 F13 (a): in der Realität wird die Vorheizzeit fest gesetzt); die Tage mit t_nötig über t_V
  nennt der Hinweis mit der nötigen Zeit und der Unterschreitung, das Maximum t_nötig,max bleibt Ergebnisgröße. Ein
  unerreichbarer Sprung geht nicht in t_V ein, er wird gezählt und gemeldet (E125 F18 (b)). Ist t_V länger als die
  Absenkung einer Nacht, wird sie durchgeheizt (1051: t_V ≈ 15 h aus dem Sprung nach dem Wochenende über der
  Werktagsabsenkung von 13 h, ≈ +10 % Heizwärme); der Lauf zählt die Nächte ohne Absenkung und weist die Mehrwärme aus.
  Eine Anwendung „täglich“ wird nicht gebaut.

**Fenster.** In den t_V Stunden vor dem Kalendersprung h_s steht der Sollwert schon auf dem Zielwert θ_T (Kühlkappe an
θ_K − 1 K wie in 4.7); ein Übergang aus „aus“ bekommt kein Fenster (W4).

**Deckel.** Φ_K,max ist das Jahresmaximum der Heizlast ohne Aufheizanteil Φ_ref(h_s) an den Kalendersprüngen (E124
F11 (a)), bestimmt in einem **Vorlauf** ohne Vorheizen. Der Deckel P_K = (1 + x)·Φ_K,max oder Φ_K,max + Δ (Toleranz in %
oder kW, Vorgabe 20 % als Kern-Konstante, Vorbelegung neuer Projekte über `Dienste.Einstellungen`, E124 F14 (a)) gilt
**stündlich** ab h_s bis zum Ende des Sollwertblocks (E122 F8 (a)), nie unter der Heizlast der Stunde: Grenze
max(P_K, Φ_ref(h)), die Floor-Stunden werden gezählt (E124 F12 (a)). Im Fenster gilt P_V = min(P_K, P_verf), mit P_verf
wie P_auf in 4.4. Option 2 bestimmt t_nötig je Sprung mit einer **Vorausschau** auf dem Zustand des Vorlaufs.

**Ankunft.** Die Luft am Beginn von h_s (Augenblick) liegt bei θ_T − ε oder darüber; ε ist die Regelgenauigkeit, Vorgabe
1 K, das Stundenmittel von h_s ist Kennzahl (E124 F15 (a)). An einer Zone mit Heizkreis gilt die Ankunft gegen
min(θ_T, θ_stat) − ε, θ_stat ist die Raumluft, die der Heizkreis am durchgewärmten Bau zur Kalenderstunde hält (E125 F17 (b)).

**Geltung und Kopplung.** t_V gilt je Gebäude oder je Zone (E122 F6). AK1-Einzonengebäude werden einbezogen; ein Schalter
je Projekt (`Aufheiz_Heizkreis_Einbeziehen`) nimmt sie aus (E122 F10, E124 F16 (a)). Eine Sperrzeit der Anlage (Nachtsperre,
Zeitprogramm 0) rechnen Vorausschau und Lauf mit; liegen Fensterstunden ohne verfügbare Wärme in ihr, nennt ein Hinweis die
Tage und die Stunden, in denen vorgeheizt werden kann — das Fenster bleibt, wo es ist (E125 F20 (a)). Bemessung (a)/(b), die Arten
„täglich“/„fest“ der Rampe und der Aufschlag gelten in diesen Verfahren nicht.

Einzelheiten — Begriffe, Physik, Zahlen an 1051, Texte, Schema, Wellen V0–V8: [Entwurf Vorheizrampe](Gebaeudesimulation/2026-10-10_Entwurf_Vorheizrampe.md).

## 5. Datenmodell und Schema

### 5.1 Kalender und Perioden

**`Tab_Konditionierungskalender`** — ein Kalender je Eigentümer und Größe, `STRICT`:

| Spalte | Typ | Regel |
|---|---|---|
| `ID` | INTEGER PRIMARY KEY AUTOINCREMENT | |
| `ID_Gebaeude` | INTEGER | → `Tab_Gebaeude(ID)` ON DELETE CASCADE; am Zonenkalender das Gebäude der Zone |
| `ID_Zone` | INTEGER | → `Tab_Zone(ID)` ON DELETE CASCADE; NULL = kein Zonenkalender |
| `ID_Gebaeude_Stamm` | INTEGER | → `Tab_Gebaeude_STAMM(ID)` ON DELETE CASCADE; Kalender eines Katalogbaus (P3) — der Name folgt dem Katalogverweis, den `Tab_Gebaeude` schon führt (Schritt 121) |
| `ID_Vorlage` | INTEGER | → `Tab_Konditionierungsvorlage_STAMM(ID)` ON DELETE CASCADE; Kalender einer Vorlage, in deren Größe (5.7) |
| `Groesse` | TEXT NOT NULL | `CHECK (Groesse IN ('HEIZSOLL','KUEHLSOLL','LUEFTUNG','GERAETE','PERSONEN'))` |
| `Wert` | REAL | Grundangabe |
| `Aus` | INTEGER NOT NULL DEFAULT 0 | `CHECK (Aus IN (0,1))` |
| `Woche` | TEXT | Standardwoche (5.2); `CHECK (length(Woche) <= 1400)` |
| `Nennwert` | REAL | W bei 100 %; `CHECK (Nennwert IS NULL OR (Groesse IN ('GERAETE','PERSONEN') AND Nennwert >= 0))`; bei Geräten heißt NULL `Interne_Waermegewinne` |
| `Bemerkung` | TEXT | Vermerk eines Werkzeugs; `CHECK (length(Bemerkung) <= 200)` |

**Eigentümerregel:** genau ein Gebäude (mit oder ohne Zone), ein Katalogbau oder eine Vorlage —
`CHECK ((ID_Gebaeude IS NOT NULL) + (ID_Gebaeude_Stamm IS NOT NULL) + (ID_Vorlage IS NOT NULL) = 1 AND (ID_Zone IS NULL
OR ID_Gebaeude IS NOT NULL))`. Genau eines von `Wert`, `Aus = 1`, `Woche`. Eindeutigkeit über vier Teilindizes je
Eigentümerart und Größe (Gebäude ohne Zone, Zone, Katalogbau, Vorlage); Teilindizes gibt es im Schema noch nicht, sie
kommen erst nach der Werkzeugprobe (R4), sonst hält der Controller die Eindeutigkeit mit Datenbankfall.
**Konsistenz:** Am Zonenkalender ist `ID_Gebaeude` das Gebäude der Zone. Das Schloss des Katalogbaus oder der Vorlage
(`ReadOnly`) gilt für seine Kalender; eine eigene Spalte gibt es dafür nicht.

**`Tab_Konditionierungsperiode`** — die Perioden eines Kalenders, `STRICT`:

| Spalte | Typ | Regel |
|---|---|---|
| `ID` | INTEGER PRIMARY KEY AUTOINCREMENT | |
| `ID_Kalender` | INTEGER NOT NULL | → `Tab_Konditionierungskalender(ID)` ON DELETE CASCADE |
| `Rang` | INTEGER NOT NULL | `CHECK (Rang BETWEEN 1 AND 999)`, `UNIQUE (ID_Kalender, Rang)` |
| `Art` | TEXT NOT NULL | `CHECK (Art IN ('ZEITRAUM','FERIEN','FEIERTAG','BETRIEBSPAUSE'))` |
| `Bezeichner` | TEXT NOT NULL | `CHECK (length(Bezeichner) <= 80)` |
| `Beginn`, `Ende` | INTEGER | Tag 1…365; NULL nur mit Feiertagsregel |
| `Feiertagsregel` | TEXT | `CHECK (… IN ('NEUJAHR','KARFREITAG','OSTERMONTAG','ERSTER_MAI','HIMMELFAHRT','PFINGSTMONTAG','EINHEIT','WEIHNACHTEN_1','WEIHNACHTEN_2'))` |
| `Wert`, `Aus`, `Woche` | wie oben | Angabe der Periode |
| `WieWochentag` | INTEGER | `CHECK (WieWochentag BETWEEN 1 AND 7)` |

Genau eine Angabe aus `Wert`, `Aus = 1`, `Woche`, `WieWochentag`; entweder `Beginn` und `Ende` in 1…365 ohne
Feiertagsregel oder eine Feiertagsregel ohne Tage mit `Art = 'FEIERTAG'`. Alle Kennwörter sind Persistenzwerte in
`DbWerte` (ASCII, eingefroren). **Keine Spalte am Gebäude:** Die Zuordnung läuft über den Eigentümer, eine Beziehung über
IDs wie bei `Tab_Zone`; es entsteht weder ein Sichtneubau von `Abfrage_Projektgebaeude` noch eine neue Spalte in
`Tab_Gebaeude_STAMM`, die zwei Fallen aus Anlagenkopplung 8.6 entfallen.

### 5.2 Die Woche als Text

Die Standardwoche ist ein 168-Werte-Text nach H8: Montag 00:00 bis Sonntag 23:00, Trennzeichen `;`, Punkt als
Dezimaltrennzeichen, `InvariantCulture`. Jede Zelle ist eine Zahl mit bis zu **vier Nachkommastellen** oder das
Kennwort `aus` (P2); in den Grenzen der Größen bleiben 168 Werte unter 1 400 Zeichen, sonst benannte Ablehnung. Leser
und Schreiber sind ein **eigener strenger Kalenderwochenleser** (genau 168 Zellen, kein Auffüllen, benannter Fehler
mit Stelle); der Leser von `Sollwertprofil` bleibt, wie er ist. Text für die Woche, Zeilen für die Perioden (F4):
Perioden brauchen Datum, Rang und Art als prüfbare Spalten, die Woche ist ein dichter Wertvektor, für den das Haus mit
H8 die Textform entschieden hat.

### 5.3 Projektschalter und Ergebnisspalten

`Tab_Einstellungen` (KP3): `Aufheizoptimierung` INTEGER NOT NULL DEFAULT 0 `CHECK IN (0,1)`; `Aufheiz_Bemessung` TEXT
`IN ('STUNDE','STUNDE_ABZUG')`, NULL heißt (a); `Aufheiz_Abzug_K` REAL 0…10, NULL heißt 2 K; `Aufheiz_Reserve` REAL
0…1, NULL heißt 0,2; `Aufheiz_Art` TEXT `IN ('TAEGLICH','FEST')`, NULL heißt täglich (P6) — jeweils `CHECK (… IS NULL
OR …)`. Modus und Abzug stehen getrennt, damit NULL nicht zugleich „Variante (a)" bedeutet. `Tab_ErgebnisGebaeude` und
`Tab_ErgebnisZone` bekommen die Spalten aus 4.8 (KP3) und `Nachtauskuehlstunden_H` (KP1), alle nullbar (Muster E30),
`Aufheiz_Leistungsquelle` mit `CHECK (… IN ('GRENZE','ZIEL'))`.

Der **Aufschlag** steht in `Tab_Einstellungen` als `Aufheiz_Aufschlag_H` INTEGER 0…24 und `Aufheiz_Aufschlag_Prozent`
REAL 0…100, NULL heißt jeweils 0; beim Schreiben werden 0 und ein leeres Feld zu NULL. Die **manuelle Aufheizzeit** steht
je Gebäude in `Tab_Gebaeude.Aufheizzeit_Manuell_H` INTEGER 1…47, NULL heißt „Art des Projekts"; der Katalog
(`Tab_Gebaeude_STAMM`) und die Zonen tragen kein Feld. `Tab_ErgebnisGebaeude` und `Tab_ErgebnisZone` bekommen
`Aufheiz_Art` TEXT `IN ('TAEGLICH','FEST','MANUELL')`, das Gebäude dazu `Auslegungsheizlast_Kw` (> 0) und
`Aufheizzuschlag_Kw` (≥ 0); `Tab_ErgebnisZone.Aufheiz_Zustand` kennt zusätzlich `GEKOPPELT`.

### 5.4 Die Schemaschritte

**KP-S1** die Tabellen aus 5.1, 5.6 und 5.7 samt Indizes und `Nachtauskuehlstunden_H` (KP1) — gebaut als Schritte 151
(`KP-S1`: 5.1 und 5.6) und 152 (`KP-S1v`: 5.7, Fremdschlüssel `ID_Vorlage` per Tabellenneubau, acht Teilindizes,
`Nachtauskuehlstunden_H`), **KP-S1b** die Saat der
14 ausgelieferten Vorlagen (KP2, Muster Schritt 149: legt nur an, was unter Größe und Namen fehlt, überschreibt nie),
**KP-S2** die Projektspalten und **KP-S3** die Ergebnisspalten der Aufheizoptimierung (beide KP3). Die Nummern vergibt
die Beauftragung aus `SchemaStand.Zielversion` + 1 — nach dem Entwurf KP3 **Schritt 160** (KP-S2) und **Schritt 161**
(KP-S3) bei Zielversion 159 — und prüft sie **spät gegen `origin`**, unmittelbar vor dem Schemacommit, weil
Nachbarsitzungen die nächsten Nummern belegen können
(ADR-001, Register A11). Je Schritt eine `static class …Schema` mit `SCHRITT`, `Vollstaendig()` und `Alle(bericht)` nach
`NachtzeitSchema` (Schritt 144), wiederholbar, eingehängt in `SchemaMigration`, `Werkzeuge/Testdatenbankschema` (Muster
`Program.cs:1919-1939`) und die Schemakopien; die Testdatenbank wandert im selben Merge mit aktivem LFS-Filter. **Kein
DML an Bestandsdaten** — die einzige Saat sind neue Katalogzeilen der Vorlagen, die kein Lauf liest; der Referenzlauf
bleibt byte-gleich.

**KP-S4** (KP3, Welle R5) trägt die Eingabe- und Ergebnisspalten aus 5.3 per `ADD COLUMN`, ohne Neubau von
`Tab_ErgebnisGebaeude`; weil SQLite einen `CHECK` nicht ändert, baut er allein `Tab_ErgebnisZone` für `GEKOPPELT` neu
(Rezept von Schritt 96) und die Sicht `Abfrage_Projektgebaeude` um die Gebäudespalte. Die Nummer vergibt die Beauftragung wie oben aus `SchemaStand.Zielversion`
+ 1 — gebaut als **Schritt 174** (`AufheizManuellSchema`) — und prüft sie spät gegen `origin`; kein DML an Bestandsdaten.
Die Vorgabe U_g der Bodenplatte (`Erdreich_U_Wirksam`, E65) kam mit **Schritt 180** (`ErdreichVorgabeSchema`).

### 5.5 Kopierwege, Leser und Werkzeuge

| Weg | Wirkung | Maßnahme |
|---|---|---|
| Katalog → Projekt (Übernahme) | Vorgabezellen und angelegte Katalogkalender reisen mit (P3) | `GebaeudeStammCtrl.CopyFromStamm` (`EPOS.Kern/Controller/GebaeudeStammCtrl.cs:606-615`), der einzige Weg Katalog → Projekt, kopiert Vorgaben, Kalender und Perioden in derselben Transaktion; eine erneute Übernahme ersetzt die des Gebäudes nach Rückfrage, die der Zonen bleiben |
| Projekt → Katalog („Speichern unter") | Gebäudezellen und Gebäudekalender reisen mit | Zonenzellen und Zonenkalender bleiben zurück, die Rückfrage nennt sie wie die Zonen |
| Katalogbau duplizieren, sperren, löschen | Vorgaben und Kalender folgen dem Katalogbau | `Katalogkopie.Duplizieren` mit Vorgabe und Kalender als Kindtabellen, die Perioden darunter (`GebaeudeStammCtrl.cs:915`); das Schloss (`SchlossSetzen`, `:924`) sperrt beides; Löschen über die Kaskade |
| Vorlage übernehmen, speichern, löschen (je Größe) | Kopie in beide Richtungen | übernommen ist kopiert, ohne datierte Ferien; Löschen einer Vorlage berührt kein Gebäude; Vorlagen reisen nicht mit Projekt, Variante oder Transfer |
| Projekt duplizieren, Variante | Vorgaben und Kalender reisen mit | **von Hand** in `KINDER` (`EPOS.Kern/Controller/ProjektDuplizierenCtrl.cs:226, 300-330`): über `ID_Gebaeude`, Perioden über den Kalender, `ID_Zone` über die zuerst deklarierte Beziehung (Muster `Tab_Zonenluftstrom`); `ProjektplanKinderWacheTests` hält es |
| Projekttransfer (`.wpx`) | erbt den Plan | Rundlaufprobe; eine ältere Fassung nennt die unbekannten Tabellen im Importbericht |
| Gebäude oder Zone löschen | Kaskade | Datenbankfall |
| Import gbXML, IFC | schreibt die Felder (E43) | unverändert; Zeitpläne der Datei bleiben ungelesen |
| Export gbXML | `SollHeizenC` aus dem Tagwert | mit angelegtem Heizkalender der häufigste Wert der Nutzungsstunden Mo–Fr; Verlustliste `GebaeudeExportVerluste` um „Kalender" |
| Zapfprofil, Ferien-Vorbelegung | liest `Ferienbeginn/-ende` | mit angelegtem Heizkalender dessen Perioden der Art FERIEN; `ZapfprofilReferenzprojektWacheTests` bleibt grün |
| Auslieferungsvorlage | Katalogvorgaben, Katalogkalender, ausgelieferte Vorlagen und die Kalender der Beispielprojekte reisen mit | Die Projektbereinigung erfasst Vorgaben und Kalender über `ID_Gebaeude` als Folgetabellen und lässt Katalog- und Vorlagenzeilen (`ID_Gebaeude` leer) stehen (`Werkzeuge/Auslieferungsvorlage/Projektsicht.cs:24-40, 75-87`); die Katalogbereinigung (`--kataloge readonly`) räumt eigene Vorlagen und entfernte Katalogbauten samt Kaskade; der **Prüfbericht** zählt Vorlagen, Vorgaben, Kalender und Perioden je Eigentümerart; die Werkzeugtests zählen vier STRICT-Tabellen mehr |
| `SqlDialektPruefer`, KI-Wissen | neue Anweisungen, neue Handlungen | Prüfer ziehen; Aktionswissen „Matrix", „Kalender anlegen", „Vorlage übernehmen", „Nachtauskühlung", „Aufheizoptimierung" |

**Leser aus der HottCAD-Projektdatei** (Datenaustauschkonzept 16, E80): Wird zum IFC-Import die Projektdatei
dazugeladen, bildet `SqprojKonditionierung` je Zone aus Tagesganglinie und DIN-V-18599-Nutzungsprofil einen formatfreien
Satz (`Zonenkonditionierung`: Kalender mit Standardwoche und Perioden, Vorgabezellen, je Größe Herkunft und Beleg). Beim
Speichern schreibt `ZonenplanCtrl.ProjektdateiUebernehmen` ihn nach den Vorlagen der Nutzung über die reinen Schritte
dieses Konzepts in denselben Vorgang: Je gelieferter Größe weicht die Kalenderkopie der Vorlage, `ZelleSetzen` trägt die
Zellen des Profils ein (Bestand oder Vorgabetabelle), der Kalender der Ganglinie tritt mit dem Beleg in `Bemerkung` an.
Größen ohne Angabe der Projektdatei behalten die Vorlage. Kein neues Schema. Im Einzonenweg nimmt das Gebäude die
Konditionierung der einen Zone bzw. der Gebäudegruppe der Projektdatei als Gebäudekalender (Eigentümer Gebäude ohne
Zone, 5.1) — `ProjektdateiUebernehmen` ohne Zone, derselbe Weg und Beleg.

**Leser aus der IFC-Datei (`EPOS_*`)** (Datenaustauschkonzept 6.3, 16.3): Der IFC-Export schreibt je Zone die Nutzung,
die Zellen Heizsollwert Tag und Nacht, Kühlsollwert und Luftwechsel der Nutzer in `EPOS_Zone` und je angelegtem Kalender
einen Satz `EPOS_Kalender_<Größe>` (Grundangabe, Standardwoche als Text wie in 5.2, Nennwert, Bemerkung, Perioden als
`Art;Beginn;Ende;Feiertagsregel;Angabe`). Der IFC-Leser nimmt sie je Raum zurück (`AbbildKonditionierung`); der
Zonenplan gibt sie als `Zonenkonditionierung` mit Herkunft „aus IFC-Datei (EPOS)“ an denselben Schreibweg. Rangfolge:
Projektdatei vor IFC-`EPOS_*` vor Vorlage der Nutzung; ein nicht lesbarer Periodentext fällt benannt weg.

Die manuelle Aufheizzeit reist mit „Projekt duplizieren", Variante und `.wpx`, nicht zwischen Katalog und Projekt (kein
Katalogfeld; die Übernahme lässt NULL); die Importe gbXML und IFC schreiben NULL.

**Die alten Spalten bleiben** dauerhaft (F5, E89): Zellen der Matrix, Eingaben von Altweg und Katalog, Ziel des Imports,
Quelle des Exports; bei angelegtem Kalender ruhen sie für diese Größe. `Sollwertprofil` bleibt für AK1 lesbar, die
Gruppe „Wärmeübergabe" bietet „In den Kalender übernehmen" an (KP2).

### 5.6 Die Vorgabetabelle

Nach P10 (b) stehen die **neuen Zellen** der Matrix in einer eigenen Tabelle je Eigentümer, Größe und Zeile — dem
Muster der Kalendertabelle; die heutigen Felder bleiben die Zellen, für die sie stehen (3.3). **Ein Ort je Zelle:** Für
Gebäude, Zone und Katalogbau trägt die Tabelle nur Zellen ohne Bestandsspalte und das „aus" einer Bestandszelle (eine
Zeile mit `Aus = 1` hat Vorrang vor dem Zahlenwert der Spalte); für Vorlagen trägt sie alle Zellen ihrer einen Größe
(5.7). Mischt eine Zeile Bestandsspalte und neue Angaben — Kühlen/Nacht mit `Kuehl_Sollwert_Nacht` als Wert (P13) und
neuen Zeiten —, trägt die Vorgabezeile nur die neuen Angaben. Kein DML.

**`Tab_Konditionierungsvorgabe`** (`STRICT`): `ID`; die vier Eigentümer wie in 5.1 mit derselben Eigentümerregel;
`Groesse` wie in 5.1; `Zeile` TEXT NOT NULL `CHECK (Zeile IN ('NENNWERT','TAG','NACHT','WOCHENENDE','FERIEN','SAISON'))`;
`Wert` REAL; `Aus` INTEGER NOT NULL DEFAULT 0 `CHECK IN (0,1)`; `Von`, `Bis` INTEGER — Stunde 0…23 in der Zeile NACHT,
Tag 1…365 in der Zeile SAISON, sonst NULL (`CHECK` je Zeile); `Bedingt_K` REAL — nur Lüftung/Nacht: ΔT der
Nachtauskühlung 0…5 K, NULL heißt 2 K (P9); in der Zeile SAISON sind `Von` und `Bis` Start und Ende der Heiz- bzw.
Kühlperiode, beide oder keiner (E53). Eindeutig je Eigentümer, Größe und Zeile über Teilindizes wie in 5.1. **Jedes
Feld einer Zeile darf leer sein** und heißt dann „wie die Ebene darüber": Zone → Gebäude → Vorgabe des Programms (3.4).

### 5.7 Die Vorlagen

**`Tab_Konditionierungsvorlage_STAMM`** (`STRICT`, Auslieferungskatalog): `ID`; `Groesse` TEXT NOT NULL wie in 5.1 —
eine Vorlage gehört **genau einer Größe** (P11); `Bezeichner` TEXT NOT NULL `CHECK (length(Bezeichner) BETWEEN 1 AND 80)`,
eindeutig **je Größe** ohne Unterschied von Groß- und Kleinschreibung (`UNIQUE (Groesse, Bezeichner COLLATE NOCASE)`) —
„Büro" darf in jeder der fünf Listen stehen; `Beschreibung` TEXT `CHECK (length(Beschreibung) <= 400)`; `Nutzung` TEXT
`CHECK (Nutzung IS NULL OR length(Nutzung) BETWEEN 1 AND 120)` — `Nutzung` ist freier Text (NP-F15, Schemaschritt 189, [Nutzungsprofile](Konzept_Nutzungsprofile_EPOS-Plan.md)); `ReadOnly` INTEGER NOT NULL DEFAULT 0
`CHECK IN (0,1)`. Der Inhalt einer Vorlage steht mit dem Eigentümer `ID_Vorlage` in Vorgabe-, Kalender- und
Periodentabelle, **nur in ihrer Größe**: die Vorgabezeilen ihrer Spalte, höchstens ein Kalender samt Perioden, keine
Periode der Art FERIEN (3.5); keine Zeilen `NENNWERT` und `SAISON`, keine Saisonperiode und kein `Nennwert` am Kalender (E54); der Controller hält die Größe gleich, ein Datenbankfall prüft es. **Namensregel:**
ausgelieferte Vorlagen tragen neutrale Nutzungsnamen ohne Hersteller- und Produktdaten; ein Doppelname in derselben
Liste wird beim Anlegen, Speichern und Umbenennen benannt abgelehnt. **Löschen und Umbenennen** nur bei `ReadOnly = 0`;
ausgelieferte Vorlagen lassen sich duplizieren. Die Wache `KonditionierungsvorlagenWacheTests` hält die 14
ausgelieferten Vorlagen: `ReadOnly`, eindeutige Namen je Liste, keine Katalognamen von Produkten, jede Zelle in ihren
Grenzen, Inhalt nur in der eigenen Größe.

## 6. Rechenkern und Einbau

| Klasse (Modul `Gebaeude/`, ohne Datenbank) | Aufgabe |
|---|---|
| `Vorgabematrix` (unveränderlich) | die wirksame Matrix eines Eigentümers: Bestandsspalten und Vorgabezeilen nach der Regel „ein Ort je Zelle", Kaskade Zone → Gebäude → Programm |
| `Standardfahrplan` | der Generator aus der Matrix, fünf Spalten (3.3) — zugleich „Kalender anlegen" und „Matrix erneut anwenden" (Matrixbereich, P12) |
| `Konditionierungskalender` (unveränderlich) | Grundangabe, Standardwoche, Perioden; `Auswerten(w₀, Referenzjahr) → double[8760]` |
| `Kalenderregel`, `Feiertage` | eine Periode samt `Enthaelt(d)`; die neun Feiertagsregeln, Osterdatum als Rechenvorschrift |
| `Kalenderwoche`, `Kalenderleser` | strenger Leser und Schreiber der Woche (5.2); Zeilen → Kalender mit `GebaeudeModellFehler.KalenderUngueltig` |
| `Konditionierungseingang` | löst je Zone und Größe die Kette 3.4 auf, liefert fünf Reihen und das Nachtfenster der Nachtauskühlung |
| `Nachtauskuehlung` | die Regel aus 3.7 neben `Sommerlueftungsregel`, gleiche Auswertung am Stundenbeginn |
| `Aufheizantwort` (Methode in `Zonenmodell2K`), `Aufheizoptimierung` | H_s, τ_k, r_k, C_k des geregelten Falls; Sprünge, Bemessungsfall, Stufenformel, P_auf, Rampenreihe, Hinweise W1–W5 |
| `KonditionierungCtrl`, `KonditionierungsvorlageCtrl` (`EPOS.Kern/Controller/`) | Matrix und Kalender je Gebäude oder Katalogbau in **einer** Transaktion, Anlegen, Verwerfen, erneut Anwenden, Kopierwege aus 5.5; Vorlagen je Größe listen, übernehmen (Matrixbereich nach P12), speichern, umbenennen, löschen, Doppelnamen je Liste; Hüllen in `EPOS.UI.Daten/Bedarf/` |

**Einbau in `GebaeudeModellEingang`.** `ThetaSoll` = Heizkalender; nur mit Schalter formt die `Aufheizoptimierung` die
fertige Heizreihe **nach dem Bauen** — im Einzonenweg in `Vdi6007Rechenweg.Rechnen` über `ZonenEingang.Einzeln`, im
Mehrzonenweg am Ende von `ZonenEingang.Bauen` in beiden Aufbauten, denn erst dort stehen die Reihen der Nachbarn;
`Bauen` selbst bleibt ([Entwurf KP3](../ueberholt/2026-10-02_Entwurf_KP3.md), Festlegung 1). `ThetaMax` = Kühlkalender, „aus" = +∞,
`GebaeudeModellErgebnis.KuehlSollwert` wird zur Reihe;
`PhiConv` und `PhiRad*` bekommen je Stunde Q_G(h) + Q_P(h) statt der Konstante (`:810-811`). Der Luftwechsel geht mit
seinem **Jahresminimum** in die Ersatzparameter, der Überschuss (n(h) − n_min) · V · ρc läuft je Stunde als
Zusatzleitwert, nie negativ (`Stundenrand.cs:113-117`); der Überschuss der Nachtauskühlung wirkt nach 3.7, die
Sommerlüftung nach 3.6, der größere gewinnt; der Zwischenspeicher des freien Falls (`Zonenmodell2K.cs:959-966`) wird ein
kleiner, geordneter Speicher je Leitwert (R7). Die Zonen rechnen je Zone, die Zonenschleife läuft **einmal**. `Schritt`
schreibt den Kappungsanteil von `Heizleistung_Max` auch im idealen Fall. Der Lauf liest ausschließlich Projektmatrix und
Projektkalender.

**„aus" und Heizperiode (E53).** Stunden mit Heizsollwert „aus" — außerhalb der Heizperiode oder stundenweise —
rechnet der Löser ohne Heizung (NaN, B11). Die Heizleistung der Zone ist dort 0, die Gebäudewärme im Kanal Raumwärme
ebenso (`EPOS.Kern/Allgemein/Simulation/SimulationKanaele.cs`), und die Erzeugerrechnung deckt in diesen Stunden nur
Warmwasser, Prozesswärme und externe Lastgänge; ein Erzeuger ohne diese Lasten steht still, ohne dass ein Fahrplan ihn
abschaltet. Mit wirksamer Kopplung (AK1) ist der Vorlauf einer solchen Stunde leer wie jenseits der Heizgrenze
(Anlagenkopplung 3.4 Nr. 3, 10.1): Die Übergabe liefert nichts, die Stunde zählt nicht als Heizgrenzstunde
(`HeizkreisErgebnis.HeizgrenzeStundenH`), sondern getrennt. Die Kälteseite gilt gespiegelt: Außerhalb der Kühlperiode
ist `ThetaMax` = +∞, die Kühlübergabe liefert nichts, der Kälteerzeuger bekommt keinen Bedarf.

**Bauvorschrift der Byte-Gleichheit.** Ohne angelegten Kalender, ohne neue Matrixzelle und ohne Schalter nimmt der
Eingang **wörtlich den Bestandszweig**: keine Multiplikation mit 1, kein neues Minimum, kein Umweg über den Kalender,
dieselbe Sommerlüftungsregel, dieselbe konstante `ThetaMax`-Reihe, derselbe Zwischenspeicher, derselbe Vorlaufstart.
Keine Uhr, kein Zufall, `InvariantCulture` in Leser und Schreiber.

**Tests** in `EPOS.Kern.Tests` unter en-US; Tests mit deutschen Texten pinnen de-DE mit der `Kulturvorrichtung`:

- **Matrix und Kalender:** Matrix lesen gleich Bestandsfeldern bei leerer Vorgabetabelle, über alle Gebäudezeilen; „aus"
  einer Bestandszelle; Kaskade Zone → Gebäude je Zelle; Rundlauf und Strenge des Wochenlesers (167 und 169 Werte, „aus",
  Rang doppelt, Tag 0 und 366, unbekannte Feiertagsregel); Vorrang und Quelle je Stunde; Ring; Umstelltage MEZ/MESZ;
  Feiertage mit Referenzjahr 2024 und 2025 (R5); Nachtfenster je Spalte; Saison als eine Periode außerhalb von Start bis
  Ende (3.2).
- **Wache Standardfahrplan** (3.3) über alle Gebäudezeilen der Testdatenbank, die Grenzfälle und alle sieben Wochentage
  des 1. Januar; Anlegen ergibt dieselbe Reihe, auch für Zonen mit eigenen Zellen; „erneut anwenden" ersetzt genau den
  Matrixbereich, eigene Perioden und Ausnahmetage bleiben (P12); Jahresmittel der Gewinne vor und nach dem Füllen der
  Personenspalte gleich (R1).
- **Reihen:** konstanter Luftwechsel und Anteil 1 bitgleich; Zusatzleitwert nie negativ; Kühlprüfung und Kühlkappung der
  Rampe (R6); „aus" in Stunde 8 040, Kühl-„aus" mit Sommerlüftung (R3); **Heizperiode** (R15): außerhalb „aus", Start
  nach Ende über den Jahreswechsel, leer byte-gleich, Kaskadenprobe mit Gebäudewärme 0 außerhalb bei unverändertem
  Warmwasser und Prozesswärme, AK1-Vorlauf leer und getrennt gezählt, Hinweis der Untertemperatur, Kühlperiode
  gespiegelt; **Nachtauskühlung** N-NK1 bis N-NK4; **Aufheizen** N-AH1 bis N-AH10.
- **Datenbankfälle:** KP-S1 zweimal; Eigentümerregel; Löschkaskade an Gebäude, Zone, Katalogbau und Vorlage; Duplikat,
  Variante und `.wpx`-Rundlauf mit Vorgaben und Kalendern; Übernahme Katalog → Projekt, erneute Übernahme, „Speichern
  unter", Katalogkopie und Schloss (R10); Vorlagen je Größe: Liste, Übernahme auf einen angelegten Kalender nach P12,
  speichern, umbenennen, löschen, Doppelname je Liste, Inhalt einer fremden Größe abgelehnt (R13);
  `SqlDialektPruefer`; `Werkzeuge/Auslieferungsvorlage` mit Katalogkalendern und Vorlagen samt Prüfbericht und
  `Werkzeuge/Testdatenbankschema` im Gate (R4).

## 7. Benutzerführung Windows und iOS

### 7.1 Grundsatz: eine Matrix, fünf Kalender, Vorlagen je Größe

Ziel der Ergänzung ist, Änderungen und Vorbelegungen **mit möglichst wenig Aufwand** vorzunehmen. Der Weg hat drei
Stufen, jede nur so weit wie nötig: (1) **Vorlage wählen** — je Größe belegt eine Wahl aus der Liste vorbefüllter
Kalender Matrixspalte und Kalender (P11); (2) **Matrix anpassen** — wenige Zellen ändern Tag, Nacht, Wochenende, Ferien
oder Saison einer Größe; (3) **Kalender einzeln ändern** — nur wenn die Matrix nicht reicht: Zeitfenster, Wochenraster,
Perioden, Feiertage. Der Katalogeditor bekommt **in allen Modi** — Projekt, Katalog neu und Katalog bearbeiten — statt
„Temperaturen und Ferien" den Reiter **„Konditionierung"**: oben die Matrix, darunter je Größe eine Kalenderkarte,
eingeklappt mit einer Zeile Zustand („aus der Matrix", „aus Vorlage Büro" oder „angelegt, 3 eigene Perioden"). Unter VDI
6007 ist die Matrix nur Vorgabe, die Karte sagt es; für Gebäude auf dem Tagesbilanz-Weg zeigt die Matrix nur die Felder
des Altwegs (2.2). **Altfelder (E56):** Wärmegewinne, Infiltration, Nutzerlüftung, Kühlsollwert, Sommerlüftung und
Maximalraumtemperatur stehen nur noch im Reiter „Konditionierung"; der Reiter „Gebäude und Hülle" behält
Luftwechselrate, Kühlung aktiv und Kühlleistung und zeigt eine Herleitungszeile.

### 7.2 Die Matrix

**Ausgangslage (#571).** Der Gebäudedialog führt in der Gruppe „Raumtemperaturen" die Felder „Soll am Wochenende
(ganztägig)" und „Soll in Ferien (ganztägig)" mit dem Platzhalter „keine" für einen unwirksamen Wert und einer
Herleitungszeile. Die Matrix übernimmt diese Namen für die Zellen Wochenende und Ferien der Heizspalte, den
Platzhalter „keine" für ihre leeren Zellen und die Herleitungszeile unter der Matrix; die Zellen bedeuten, was 3.3
unter „Abbildung der heutigen Felder" festhält.

```
┌ Konditionierung ────────────────────────────────────────────────────────────────────────────────────────────┐
│             Heizen °C        Kühlen °C        Lüftung 1/h             Geräte           Personen             │
│ Vorlage     Wohnen           Wohnen           —                       Wohnen           Wohnen               │
│ Nennwert        —               —             Infiltration 0,3       2 100 W          4 × 70 W = 280 W      │
│ Tag          [20,0]          [26,0]           [0,4]                  [100 %]          [ 50 %]               │
│ Nacht        [18,0] 22–6     [28,0] 22–6      [2,0] 22–6 ΔT 2 K      [100 %] 22–6     [100 %] 17–7          │
│ Wochenende   [ — ]           [ — ]            [ — ]                  [ — ]            [100 %]               │
│ Ferien       [16,0]          [aus]            [ — ]                  [ 50 %]          [  0 %]               │
│ Saison       1.10.–30.4.     1.5.–30.9.                                                                     │
│ Ferienzeiträume  24.07.–03.09. · 23.12.–06.01.     Maximalraumtemperatur [24,0] °C     ☐ Sommerlüftung      │
│ [Kalender anlegen]   [Matrix erneut anwenden…]                                                              │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

- **Zellen:** Zahlenfelder mit Einheit; „—" heißt leer (wie Tag bzw. wie Werktag), „aus" ist ein Zustand der Zelle bei
  Heizen und Kühlen (P2). Nachtzeiten stehen in der Zelle als „von–bis", leer = die Zeiten der Heizspalte (F19). Die
  Heizspalte zeigt die heutigen Felder: Ein Wochenendwert bis 5 °C und ein Ferienwert unter 1 °C sind unwirksam und
  erscheinen wie im Dialog als „keine" (in der Skizze „—"). Die Zeile **Vorlage** nennt je Spalte die zuletzt
  übernommene Vorlage, ein Klick öffnet die Auswahlliste der Karte (7.4); ihre Kopfzelle trägt die Liste **„alle
  Größen"**, die eine gleichnamige Vorlage nach einer Rückfrage in allen Größen übernimmt (E57, 7.4); die **Saison**
  nimmt Heiz- und Kühlperiode mit
  Datum für Start und Ende, leer = ganzjährig (E53). Ob eine Zelle in ihrer Bestandsspalte oder in der Vorgabetabelle
  liegt (P10), sieht der Anwender nicht.
- **Lasten:** Nennwert mit Herleitungszeile (P1: Geräte = Gesamtwert − Personenmittel; Personen = Zahl × 70 W) und
  Anteile je Zeile; die Zeile unter der Matrix zeigt die Jahresmittel in W und W/m² neben `Interne_Waermegewinne`.
- **Lüftung:** Nutzerlüftung in 1/h, die Infiltration als feste Zeile; in der Nachtzeile die **Nachtauskühlung** mit
  Wert, Zeiten und ΔT (Vorgabe 2 K); sie wirkt stets bedingt, einen Schalter gibt es nicht (P9).
- **Kühlen:** wirkt nur mit Kühlbetrieb im Projekt und `Kuehlung_Aktiv` (E32); sonst ist die Spalte weich gesperrt,
  mit Grund am Element. Die Nachtzelle ist `Kuehl_Sollwert_Nacht` (P13).
- **Knöpfe:** „Kalender anlegen" schreibt die Kalender aus der Matrix; „Matrix erneut anwenden…" fragt vorher und
  ersetzt nach P12 nur den Matrixbereich — die Rückfrage nennt, was ersetzt wird und was bleibt (eigene Perioden,
  Ausnahmetage). Geschrieben wird mit dem OK des Editors; „Zurücknehmen" nimmt den letzten Schritt des Arbeitsstands
  zurück. Ein angelegter, nicht von Hand geänderter Kalender folgt der Matrix ohne Knopf (E56, 3.3).
- **Schmale Anordnung** unter 900 px: je Größe eine Karte mit den Zeilen untereinander, umschaltbar über fünf Reiter;
  Berührungsziele ≥ 44 px, nichts rollt quer.

### 7.3 Zonen und Katalog

**Zonen:** Im Detailbereich der Zone steht dieselbe Matrix; geerbte Zellen zeigen den Gebäudewert als Platzhalter
„Vorgabe …" (Muster U3), eine eigene Zelle überschreibt ihn, „erben" leert sie wieder. Je Größe trägt die Zone „vom
Gebäude" (gesperrte Anzeige mit Grund) oder „eigener Kalender"; „vom Gebäude übernehmen und anpassen" legt eine Kopie an.
Die Kühlspalte der Zone ist bedienbar seit KU3-3 (Vererbung Kühlkonzept 3.5). **Katalog (P3):** Im Katalogmodus steht
dieselbe Oberfläche ohne Zonen; das Schloss eines ausgelieferten Satzes sperrt Matrix und Karten, „Duplizieren" öffnet
eine bearbeitbare Kopie samt Vorgaben und Kalendern. Die Katalogauswahl zeigt je Satz, ob er Kalender trägt; die
Rückfrage einer erneuten Übernahme nennt, was ersetzt wird und was bleibt.

### 7.4 Vorlagen je Größe

Jede Kalenderkarte trägt im Kopf die **Auswahlliste** ihrer Größe — „Vorlage: [Büro ▾]" mit den ausgelieferten
Vorlagen (Schloss) und den eigenen, zu jeder die Vorschau (Woche und Teppichbild an den Ferienzeiträumen des Ziels) —
und „Übernehmen" (P11). Die Wahl wirkt auf den Arbeitsstand und nur auf diese Größe; trägt das Ziel schon einen
angelegten Kalender, fragt der Dialog vorher und ersetzt nach P12 nur den Matrixbereich (3.5). Die Zeile „Vorlage" der
Matrix nennt je Spalte die zuletzt übernommene Vorlage und öffnet mit einem Klick die Liste der Karte.
**„Als Vorlage speichern…"** steht in derselben Karte und fragt Name, Beschreibung und Nutzung; die Größe ist die der
Karte, ein Doppelname in dieser Liste wird am Feld benannt abgelehnt. **„Vorlagen verwalten"** — aus jeder Kalenderkarte als Blatt im Katalogeditor, auf beiden Plattformen, kein Menüpunkt
(E56) — zeigt die fünf Listen
mit einem Umschalter der Größe; eigene Vorlagen lassen sich dort umbenennen und löschen, ausgelieferte duplizieren;
Löschen fragt nach und nennt, dass kein Gebäude berührt wird. Wer alle fünf Größen nach einem Muster belegen will,
wählt es in der Kopfzelle der Zeile „Vorlage" unter **„alle Größen"** (E57): Die Liste führt jeden Namen, der in
mindestens einer der fünf Listen steht, einmal und in derselben Reihenfolge. **Eine** Rückfrage nennt vor dem Schreiben
je Größe, was geschieht — übernehmen, nach P12 ersetzen und behalten, die Gesamtangabe der Lüftung aufteilen (F5), keine
Vorlage dieses Namens (die Größe bleibt) oder gesperrt mit Grund —, die betroffenen Zonen mit Namen; „Ja" übernimmt in
einem Schritt, den „Zurücknehmen" ganz zurücknimmt. Die Abkürzung führt keinen Satzbegriff ein: Vorlagen bleiben je
Größe (P11), die Übernahme je Karte bleibt, gleiche Namen stehen in jeder Liste an derselben Stelle (Zählfall KN6: 3
statt 11 bis 16 Handgriffe).

**„Kopieren nach …"** (3.5) steht in jeder Zeile der Verwaltung neben „Duplizieren". Eine kleine Überlagerung mit Titel
und Kreuz zeigt nur die erlaubten Ziele der Quelle — Heizen → Kühlen, Geräte ↔ Personen —, den Namen der Quelle als
Vorschlag und, nur bei Heizen → Kühlen, die Felder des Komfortsollwerts (Vorgabe 26 °C) und des Absenksollwerts
(Vorgabe 28 °C, nimmt „aus" wie die Zellen der Matrix), beide in den Grenzen der Kühlspalte. An Kühl- und
Lüftungsvorlagen ist der Knopf weich gesperrt, der Grund steht am Knopf. „Kopieren" schreibt sofort; ein Doppelname
der Zielliste steht am Namensfeld, ein ungültiger Sollwert an seinem Feld, ein Absenksollwert unter dem
Komfortsollwert am Absenkfeld. Danach zeigt die Verwaltung die
Zielliste mit der neuen Vorlage gewählt.

### 7.5 Die Kalenderkarte

Aufgeklappt zeigt jede Karte im Kopf die **Auswahlliste der Vorlagen** ihrer Größe mit „Übernehmen" und „Als Vorlage
speichern…" (7.4), darunter Grundangabe, **Zeitfenster-Werkzeug** (sieben Tagesknöpfe, von, bis, Wert — der Weg ohne
Zellenarbeit auf dem Tablet), das **Wochenraster** (7 × 24, mit dem Zellenzustand „aus" und einer schmalen Anordnung
unter 900 px als 2 × 12 Zellen, damit Zellen 44 px behalten; die Zusätze sind abschaltbar und berühren AK1 und AK2
nicht), die **Periodenliste** (schlichte Tabelle ohne Virtualisierung; ✎ öffnet die Periode in der Karte), die Werkzeuge
Feiertage und Zeitstruktur und die **Vorschau** als `Zeichenmodell` in `DiagrammSvg` — die Woche über
`ChartRenderer.StundenprofilModell` wie heute (`EPOS.UI.Daten/Bedarf/GebaeudeKatalogHuelle.cs:779-784`), das Jahr als
**Teppichbild** (Tage × Stunden, Farbe = Wert, „aus" als eigene Fläche; Berühren nennt die Quelle der Stunde), gehalten
von `Proben/ChartProben`. „Verwerfen" führt zurück zur Matrix.

### 7.6 Aufheizoptimierung, Ergebnis, Bericht

Die Projekteinstellung steht in `EPOS.UI/Seiten/Simulation/SimulationKonfigSeite.razor` neben Kühlbetrieb und
Anlagenkopplung: Schalter, (a)/(b) mit ΔT_K, Aufheizreserve ρ, Art täglich/fest, Aufschlag in Stunden und in Prozent
(4.6) und je Gebäude eine Herleitungszeile
(„t_auf,max 8 h bei −18,2 °C · P_auf 18,9 kW Zielleistung · C_w 8,3 kWh/K"). Ergebnisseite und Bericht führen die
Kennzahlen aus 4.8 und die Nachtauskühlungsstunden (Muster E30), das Bedarfsbild die geformte Sollwertreihe, der Bericht
je Kalender eine Kurzform („Heizen: 20/18 °C, 22–6 Uhr, Heizperiode 1.10.–30.4.") und je Größe den Namen der
übernommenen Vorlage; der Hinweis der Heizperiode (3.6) steht im Protokoll und im Bedarfsdialog.

**Variantenvergleich und Kurzbericht (E58 F3, F4).** Der Variantenvergleich führt Nachtauskühl-, Sommerlüftungs- und
Aufheizwerte zweifach: als **Gebäudetafel je Stand** im Vergleichskapitel (Marke `stand.tabelle.gebaeude` nach
`stand.tabelle.heizkessel`, je Variante jedes Gebäude, ohne Δ) und als **Kennzahlgruppe „Gebäude"** im
Kennzahlenkatalog mit Δ (Höchstwert über die Gebäude, P_auf als Summe; jede neue Kennzahl mit `Kennzahl.Seit`, damit
die Listen v1–v11 bleiben). Für eigene Vorlagen stehen die Einzelfelder `gebaeude.ergebnis.*`; der ausgelieferte
Kurzbericht trägt in beiden Sprachen einen **Aufheizabsatz**. Alle neuen Marken gehören zur Katalogfassung 12, die
Vorlagen werden über `Werkzeuge/Berichtsvorlage` neu gebaut.

**Aufheizzeit manuell im Gebäudedialog.** Der Reiter „Konditionierung" des Gebäudedialogs trägt im Projektmodus eine
Gruppe „Aufheizung" mit dem Feld „Aufheizzeit manuell (h)" (1–47, leer = Art des Projekts) und zwei **Vorschlägen**
daneben: (1) die bemessene Aufheizzeit dieses Gebäudes aus der Herleitungszeile (4.8, ohne Jahreslauf) mit
„Übernehmen"; (2) die **Spanne aus der Zeitkonstante** des Gebäudes, keine feste Tabelle: [t_u; t_o] mit
t_u = max(1, t_auf,max) und t_o = min(47, ⌈τ₂ · ln 10⌉) Stunden (liegt t_u über t_o, gilt [t_o; t_u]; bei
unerreichbarer Bemessung [1; t_o]). τ₂ ist die langsame Zeitkonstante der Aufheizantwort (4.2) mit Strahlungsanteil und
Zusatzleitwert des Bemessungsfalls, am Gebäude mit Zonen das Maximum der beheizten Zonen; nach ln 10 · τ₂ ist der
langsame Modus auf 10 % abgeklungen, länger zu rampen senkt die Spitze kaum noch. Im Zahlenbeispiel aus 4.5 ergibt das
1–11 h für das Haus aus 1045 und 8–12 h für die gedämmte Variante. Die Regel ist ein Vorschlag der Umsetzung (Entwurf
Festlegung 40). Ein Wert außerhalb der Spanne wird mit Hinweis angenommen, nicht gesperrt (weiche Sperre nach den Hausregeln
von `EPOS.UI`); ohne Projektschalter sagt das Feld, dass es erst mit der Aufheizoptimierung wirkt. Zonen zeigen den
geerbten Wert lesend, der Katalogmodus zeigt das Feld nicht. Der Assistent setzt es über ein Katalogfeld in
`GebaeudeKiSicht`; Abweichungsmerkmale „Art", „Aufheizzeit manuell (h)", „Aufschlag (h)" und „Aufschlag (%)".

**Auslegungsgröße in Bedarfsdialog und Bericht.** Die Gruppe „Aufheizung" des Bedarfsdialogs und die Gebäudetafel des
Berichts zeigen je Gebäude die Auslegungsgröße Φ_HL + Φ_RH mit ihren Teilen (4.8) und daneben ideale Spitze, Tagesmittel
und P_auf mit Quelle, mit dem Hinweis, dass die ideale Spitze bei Schalter aus keine Auslegungsgröße ist; beide
Sprachen. Der Bericht liest die Teile aus der Ergebniszeile, der Bedarfsdialog bei Schalter aus aus der Auskunft der
Bemessung.

### 7.7 Plattform, Glossar, Texte, Assistent, Tests

- **Plattform:** Windows und iOS teilen jede Komponente; alles geschieht in der einen Überlagerung des Katalogeditors,
  keine modale Kette (iL5), keine Hover-Bedienung; die iOS-Hülle bleibt unverändert.
- **Glossar vor den Ressourcen** in [Glossar](Glossar_Lokalisierung.md) § 13 (Muster U4, E28) — Vorschläge:
  Konditionierung → conditioning, Vorgabe-Matrix → defaults matrix, Vorlage → template, Standardwoche → standard week,
  Periode → period, Heizperiode → heating period, Kühlperiode → cooling period, Nachtauskühlung → night purge
  ventilation, Aufheizzeit → preheat time, Aufheizleistung → preheat power, Aufheizreserve → preheat reserve,
  Aufheizoptimierung → preheat optimisation, Aufschlag → surcharge, Aufheizzeit manuell → manual preheat time,
  Vorschlag → suggestion.
- **Texte und Assistent:** `KOND_*` in **beiden** `.resx`, danach `Werkzeuge/ResourceDesigner`; jede neue Eingabe
  braucht ein Katalogfeld in `KiDialoge` (Woche und Matrixzeile als Textfeld wie `sollwertprofil`, `KiDialoge.cs:5783`)
  oder einen Grund in `BewusstDraussen`, gezählt in `EINGABESTELLEN`; `KiMaskenabdeckungWacheTests` hält es.
- **Tests:** bunit je Baustein und Dialog (Feldbestand, Rückweg samt `null`, Zustand, Fall ohne Gaben), in Projekt- und
  Katalogmodi, dazu `StilblattTests`, `SchliesskreuzWacheTests`, `UeberlagerungstitelTests`. `Proben/Rasterprobe` nur,
  wenn `Raster`, `Katalogliste` oder die `.epos-raster*`-Regeln berührt werden.

### 7.8 Kalenderbedienung (Fassung 2)

Mit E110 (9.10) bekommt der Reiter „Konditionierung" eine Kalenderbedienung nach dem angenommenen
Mockup [`Mockups/Konditionierung_Kalender.html`](Mockups/Konditionierung_Kalender.html): Grundgerüst ist die Variante V2 „Wochenprofile" des
[Befundpapiers](../ueberholt/2026-10-09_Befund_Kalenderbedienung_Konditionierung.md), ergänzt um das Jahresraster aus V1 als
anklickbare Anzeige. Die Vorgabe-Matrix (7.2) bleibt als Übersicht; ihre Zeilen Wochenende und Ferien werden über die
Schnellfelder bedient. Die Kalenderkarte (7.5) bleibt der Ort der Einzelbearbeitung einer Größe.

**Aufbau.**

- **Wochenprofile** je Größe: die Standardwoche und die Wochen der Zuordnungszeilen, jedes ein 7 × 24-Raster mit
  Pinsel (Wert oder „aus" auf einen Zellbereich), „Montag nach Di–Fr", „Samstag nach Sonntag", „Tag kopieren" (eine
  Tagesspalte auf andere Wochentage, auch in ein anderes Profil) und „Woche kopieren" in ein anderes Profil derselben
  Größe oder einer Größe gleicher Einheit — Heizen ↔ Kühlen (°C), Geräte ↔ Personen (Anteil), die Lüftung (1/h) nur zu
  sich; der Zielwert wird gegen die Grenzen der Zielgröße geprüft (3.6).
- **Zuordnung** als Tabelle „von–bis (Datum des Bezugsjahrs) → Wochenprofil, gilt für": „alle" heißt alle Größen mit
  angelegtem Kalender, eine Auswahl ist je Zeile abwählbar. Die Wirkung einer Zeile ist ein Wochenprofil, „aus", „wie
  Wochentag X" oder ein Wert.
- **Jahresband** über der Zuordnung: die Zeiträume der gewählten Größe als farbige Abschnitte; ein Klick wählt Zeile
  und Profil.
- **Einzeltage** als Liste (Datum, Bezeichnung, Wirkung „wie Sonntag" oder „aus", gilt für) mit „Feiertage laden".
- **Schnellfelder:** Wochenende (Sa + So), Ferien als Liste benannter Datumsbereiche (beliebig viele), Saison von–bis
  je Größe, Feiertage.
- **Jahresraster** (12 × 31) als Anzeige: jeder Tag in der Farbe seiner Quelle (Wochenprofil, Zeitraum, Einzeltag,
  Ferien, Feiertag, Saison „aus"); ein Klick öffnet die Zeile bzw. den Einzeltag, der den Tag bestimmt.
- **Kopieren:** Tag und Woche (oben), **Monat** — alle Zeilen und Einzeltage, die einen Monat berühren, werden auf
  einen anderen Monat übertragen (auf den Quellmonat beschnitten, um den Abstand der Monatsersten verschoben, auf den
  Zielmonat beschnitten); die Kopien bekommen in derselben Folge Ränge über allen vorhandenen Zeilen.
- **„Vorlage übernehmen"** für alle Größen in einem Schritt (Vorlagen je Größe, 7.4, E57).

**Abbildung auf das heutige Modell (Stufe 1, ohne Schemaschritt).** Die Schicht liegt im Kern
(`Kalenderbedienung`, neben `Konditionierungsarbeit`) und arbeitet wie die übrigen Schritte rein über dem Arbeitsstand
(`Konditionierungsarbeitsstand`); geschrieben wird allein im OK-Weg des Editors über die vorhandenen Controller und
Tabellen — keine neue SQL-Anweisung.

| Bedienung | Modell heute |
|---|---|
| Wochenprofil | Standardwoche des Kalenders bzw. die Woche (`Angabe = Woche`) einer eigenen Periode; Name = Bezeichner der Periode |
| Zuordnungszeile | Periode der Art `ZEITRAUM` im Eigenband 310 … 899, je Größe eine Kopie |
| Einzeltag | Periode mit Beginn = Ende (`ZEITRAUM`) oder Feiertagsregel (`FEIERTAG`); „wie Sonntag" = `WieWochentag 7`, „aus" = `aus` |
| „gilt für alle" bzw. Auswahl | synchron gehaltene Kopien in den Kalendern der gewählten Größen, **gekoppelt über Name + Beginn + Ende** (bei Feiertagen Name + Regel); Ändern und Löschen einer gekoppelten Zeile wirkt auf alle Kopien, eine abgewählte Größe verliert ihre Kopie |
| Ferien 1 bis 4 | die Spalten `Ferienbeginn/-ende_1…4` des Gebäudes — sie lesen der Generator (Perioden `FERIEN`, Rang 200 + k), der Tagesbilanz-Weg und der Zapfkalender; nach jeder Änderung wird der Matrixbereich der angelegten Kalender von Gebäude und Zonen erneuert („Matrix erneut", P12) |
| Ferien ab 5 | Perioden `ZEITRAUM` mit dem Bezeichner „Ferien n" am unteren Ende des Eigenbands, mit der Angabe der Ferienperiode des Kalenders — nur in angelegten Kalendern, deren Ferienzeile wirkt; ein abgeleiteter Kalender kennt nur die vier Spalten |
| Wochenende | fest Sa + So (Wochenendzeile der Matrix) — nur lesbar |
| Saison | die Zeile `SAISON` der Matrix je Größe (Periode `BETRIEBSPAUSE`, Rang 900) |
| Feiertage | die neun bundeseinheitlichen Regeln (Rang 100 … 108, unter den Ferien); die Feiertage eines Landes als feste Einzeltage des Bezugsjahrs |

**Abbildung auf das Modell der Stufe 2 (Schemaschritt 207).** Die Schicht hält den gemeinsamen Kalender im Arbeitsstand
nativ (`Konditionierungsstand.Gemeinsam`, `Ferienliste`, `Wochen`); die Größenkalender tragen seine Perioden
ausgebreitet, wie der Lauf sie liest, damit die Werkzeuge der Karte jede Größe gegen ihre Grenzen prüfen (3.6). Der OK-Weg
schreibt die eigenen Kalender ohne die Kopien, den gemeinsamen Kalender als eine Zeile je Periode und die benannten Wochen.

| Bedienung | Modell Stufe 2 |
|---|---|
| Wochenprofil | Standardwoche des Größenkalenders oder eine benannte Woche (`Tab_Konditionierungswoche`: Name je Größe eindeutig, 168 Werte); „Woche kopieren" in ein Profil derselben Größe oder einer Größe gleicher Einheit (Heizen ↔ Kühlen, Geräte ↔ Personen, Lüftung nur zu sich) mit der Grenzprüfung der Zielgröße; „Tag kopieren" auch in ein anderes Profil; eine Woche, auf die eine Zeile verweist, lässt sich nicht löschen (benannte Ablehnung) |
| Zuordnungszeile | EINE Periode des gemeinsamen Kalenders (`Groesse = ALLE`) mit `Gilt_Fuer` (31 = alle, Auswahl = Teilmaske); ein Wochenprofil als Verweis `ID_Woche`; Ändern und Löschen wirken auf die eine Zeile, eine abgewählte Größe verlässt die Maske; eine Größe ohne angelegten Kalender steht in der Maske und wirkt, sobald ihr Kalender angelegt ist |
| Lesebrücke | gekoppelte Kopien je Größe bleiben für Altbestand, den die Migration nicht zusammengeführt hat (Rang belegt), und für die Wirkung „Standardwoche" über mehrere Größen (je Größe eine andere Woche); sie werden wie in Stufe 1 bedient |
| Wochenende | Spalte `Wochenendtage` am Gebäude (Wochenmaske, leer = Sa + So); das Schnellfeld erneuert den Matrixbereich der angelegten Kalender. Es lesen der Generator und der Zapfkalender (ein Tag der Maske oder ein Feiertag ist gekennzeichnet; ein gewöhnlicher Samstag der Maske trägt den Samstags-, jeder andere gekennzeichnete Tag den Sonntagsgang — ein Feiertag unter jeder Maske, E112); **der Tagesbilanz-Weg bleibt bei Samstag und Sonntag (E111)** |
| Feiertagsland | Spalte `Feiertagsland` am Gebäude; das Schnellfeld legt die Regelperioden seiner Landesregeln im gemeinsamen Kalender des Gebäudes an (Maske 31, „wie Sonntag", Rang 109 + Stelle der Regel, also 109 … 116) und entfernt beim Wechsel oder Leeren nur diese (erkannt an Regel und Rang, nie am Text); die neun bundeseinheitlichen Regeln bleiben |
| Ferien | beliebig viele benannte Zeiträume: die ersten vier in `Ferienbeginn/-ende_1…4` (der Trigger spiegelt sie auf Rang 200 … 203; ihre Namen liegen im Bezeichner dieser Spiegelperioden, und jeder Schreibweg der Spalten bewahrt sie über die Hülle `Kalendergemeinschaft.MitFeriennamen`), die weiteren als Ferienperioden des gemeinsamen Kalenders ab Rang 204 ohne Angabe. Der Generator liest die ganze Liste und macht aus jedem Zeitraum eine Periode `FERIEN` auf dessen Rang (200 … 309) mit der Ferienangabe der Größe; der Zapfkalender liest bei angelegtem Heizkalender alle Ferienperioden des gemeinsamen Kalenders, sonst die vier Spalten; **der Tagesbilanz-Weg liest nur die vier Spalten (E111)**. Zeilen „Ferien n" der Stufe 1 im Eigenband werden beim OK-Weg und in der Bestandsform die Periode `FERIEN` ihres Rangs (gleicher Zeitraum, gleiche Angabe) |

**Rangregel** (unverändert, 3.2): Der höhere Rang gewinnt — Feiertagsregeln 100 … 108 und Landesregeln 109 … 116 <
Ferien 200 … 203 und Ferienliste 204 … 309 < eigene Zeilen 310 … 899 < Saison 900. Eine neue Zeile kommt über die ranghöchste eigene, Ferien ab 5 auf den
Rang ihrer Ferienperiode (204 … 309); damit schlägt die zuletzt angelegte Zeile die ältere, und eine Zeile, die die Schicht in
mehreren Größen anlegt, steht in jeder Größe über denselben Zeilen. Ein Einzeltag „wie Sonntag" liegt im Eigenband
über den Ferien — anders als die Feiertagsregel.

**Die zwei Stufen.** **Stufe 1** (K1a Datenschicht, K1b Oberfläche) läuft auf dem heutigen Modell wie oben. **Stufe 2**
(Welle K2, Schemaschritt 207 `KalenderbedienungSchema`) bringt den gemeinsamen Eigentümer „Gebäude, alle Größen" mit
einer Spalte „gilt für" statt der Kopien, benannte wiederverwendbare Wochen (eine Tabelle der Wochen, die Periode
verweist auf sie), das wählbare Wochenende am Gebäude, die Feiertage der sechzehn Länder als Regeln (Prüfregel der
`Feiertagsregel` erweitert, Wahl am Gebäude) und beliebig viele Ferienzeiträume als Perioden des Gebäudes; die vier
Ferienspalten werden dann aus den ersten vier Ferienperioden gespiegelt, solange ein Leser sie braucht. Die Kopplung
über Name + Beginn + Ende wird dabei in den gemeinsamen Eigentümer überführt.

## 8. Stufen und Aufwand

| Stufe | Inhalt | Vorbedingung | Abnahme | Basis | PT |
|---|---|---|---|---|---|
| **KP0** | Dieses Konzept, die Entscheide E52 (N1.59) und E53 (N1.60), der Nachzug der Schwesterpapiere (2.3), die P_auf-Probe (4.4) und das Glossar (13) — abgeschlossen 27.09.2026 | — | Papiere widerspruchsfrei, `DokumentationLinkWacheTests` grün | nein | 1–2 |
| **KP1** | KP-S1 (Kalender, Perioden, Vorgaben, Vorlagen), Vorgabematrix mit Kaskade, Generator mit fünf Spalten, Kalendermodell, Feiertage, Heiz- und Kühlperiode samt Folgen (Kopplung, Hinweis), Vererbung, fünf Reihen, Nachtauskühlung, stündliche Kühlprüfung, Controller für Matrix und Vorlagen je Größe, Kopierwege Katalog ↔ Projekt, KINDER, Auslieferungsvorlage samt Prüfbericht, Werkzeuge | KP0; Schemawellen von G6c gemergt | Kern-Gate, Proben und Datenbankfälle (6), Vorlagenlauf; Referenzlauf **byte-gleich** gegen R22 | nein | 13–18 |
| **KP2** | Reiter „Konditionierung" in allen Modi: Matrix mit schmaler Anordnung, Zonenmatrix, Kalenderkarten mit Zeitfenster, „aus", Periodenliste, Werkzeugen und Teppichbild, Auswahlliste und „Als Vorlage speichern" je Karte, Vorlagenverwaltung mit fünf Listen, Saat der 14 ausgelieferten Vorlagen (KP-S1b), Katalogauswahl, Assistent, Ressourcen; Ferienumrechnung im Gemeinjahr (B13) — abgeschlossen 02.10.2026 in elf Wellen samt der Abkürzung „alle Größen" (E57; [Protokoll KP2](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-30_KP2_Konditionierung_Oberflaeche.md), Festlegungen im Leitkonzept N1.66) | KP1 | bunit, ChartProben, Sichtabnahme Windows; byte-gleich — erfüllt bis auf die Sichtabnahme SA1, die beim Anwender aussteht (Referenzlauf byte-gleich gegen R29) | nein | 14–18; Entwurf: 19–22 (E56) |
| **KP3** | Stufenformel, Nachweisband, Aufheizleistung, Bemessung, KP-S2, KP-S3, Ergebnis, Hinweise, Bericht, Export — samt Nachtauskühl- und Sommerlüftungsstunden (E54); neues Referenzprojekt über die Vorlagen- und Katalogübernahme, Einfrierregel, neue Basis, CI ([Entwurf KP3](../ueberholt/2026-10-02_Entwurf_KP3.md), 9.8, 9.9). **Teilweise:** gebaut sind der Rechenweg (R1–R5 samt Aufschlag und manueller Aufheizzeit, Schritt 174), die Daten (D1, D2: Schritte 160, 161, Kennzahlen, Export, Auskunft), die Projekteinstellung (O1), das Referenzprojekt 1051 mit Wache und ρ-Messung (RP1) und die Basis R34 mit dem Erdreich nach DIN EN ISO 13370 (RP2); E64 mit EV1 (Reserve leer = 20 % mit Laufhinweis); offen sind O1b, O2, O3 und die Welle A ([Protokoll KP3](../ueberholt/Protokolle/Gebaeudesimulation/2026-10-02_KP3_Aufheizoptimierung.md), Festlegungen im Leitkonzept N1.69) | KP2 | N-AH1–N-AH12; alle übrigen Projekte byte-gleich; A/B-Protokoll — erfüllt für die gebauten Wellen | **ja** (R34) | 6–9; Entwurf mit E58: 14–18,25; mit E59/E60: 16,75–21,75 |
| **KP4** | Papiere nachziehen (Rechenschritte mit den Schritten K „Aufheizrampe" und L „Nachtauskühlung", Leitkonzept 4.4, 4.5 und N1.69, Softwarearchitektur, Status, Protokoll), Wiki-Quellen, Logbuch-Entwurf — **Papiere, Wiki-Quellen (10.4) und Logbuch-Entwurf (10.5) nachgezogen 04.10.2026**; Wiki-Upload gebündelt, Version beim Anwender | KP3 | Wiki-Suchmuster aus `CLAUDE.md` leer, Link-Wache grün | nein | 1–2 |
| **K1a** | Kalenderbedienung Stufe 1 (E110, 7.8): Konzept 7.8 und 9.10, plattformfreie Datenschicht `Kalenderbedienung` im Kern (Wochenprofile, gekoppelte Zuordnung, Einzeltage, Schnellfelder, Monat- und Tageskopie, Jahresraster und Jahresband, Vorlage für alle Größen) | KP2 | Kern-Tests der Schicht; Referenzlauf 1051 und 1052 byte-gleich | nein | 1–2 |
| **K1b** | Kalenderbedienung Stufe 1, Oberfläche: Wochenprofile, Zuordnung mit Jahresband, Einzeltage, Schnellfelder, Jahresraster im Reiter „Konditionierung", Hülle und Texte | K1a | bunit, Sichtabnahme; byte-gleich | nein | 2–3 |
| **K2** | Kalenderbedienung Stufe 2: Schemaschritt 207 `KalenderbedienungSchema` (gemeinsamer Eigentümer, benannte Wochen, Wochenende am Gebäude, Länderfeiertage als Regeln, Ferienperioden), Kopierwege, Umstellung der Schicht; S2b: die Leser (Generator mit der Ferienliste ab Rang 204, Zapfkalender mit Wochenende und Ferienliste, wiederholbare Bestandsform) | K1b | Schemaproben, Kopierwege; Referenzlauf byte-gleich | nein | 3–5 |
| **Summe** | | | | | **35–49** (mit den Entwürfen KP2 und KP3: 50,75–65,75) |
| KP3b *(optional)* | AK1-Gebäude über die Vorausrechnung mit Ankunftskriterium; Vorkühlen mit KU3 | KP3; KU3 für die Kälte | wie KP3 | je nach Projekt | 3–5 |

In KP1 stecken 2–3 PT für die Katalogkalender (P3) und 3–4 PT für Vorgabetabelle, Generator mit fünf Spalten, Vorlagen
und Nachtauskühlung; in KP2 1 PT für den Katalog und 3 PT für Matrix- und Vorlagenoberfläche samt Saat — die
Voreinstellungen der Rev. 1 sind darin aufgegangen. **Mit E53 (Rev. 3) bleiben die Spannen:** Die Vorlagen je Größe
sparen in KP1 die Satzlogik und die Größenauswahl, die Folgen der Heizperiode (Vorlauf der Kopplung, Hinweis, Proben)
kosten dort ebenso viel, rund 0,5 PT; in KP2 kosten Auswahlliste und Speichern je Karte, die Verwaltung mit fünf Listen
und die Saat von 14 statt drei Vorlagen rund 0,5 PT mehr, was die Spanne trägt. Dazu je Einfrierschritt rund 0,5 PT.
**Reihenfolge:** P1–P13 sind entschieden (E52, E53); KP0 schließt mit Probe, Glossar und Nachzug. **KP1 folgt den
Schemawellen von G6c** (bis auf das Wiki gebaut), weil beide Zonenkaskade und Schemastand berühren. **G6d**
(Referenzprojekt mit Zonen, eigener Einfrierschritt) und **KP3** frieren je eine Basis ein; liegen sie nah beieinander,
spart ein gemeinsamer Einfrierschritt einen Lauf und deckt die Zonenkalender mit (R8). **KU3 folgt KP1** (P7): KU3
verliert „Kühlsollwert Nacht" und baut „Kühlung je Zone" auf den Zonenkalender. **AK2** profitiert, weil seine
Komfortstunden der Nutzungszeit aus 3.4 folgen (F16).

## 9. Festlegungen F1–F22, Entscheide P1–P13

### 9.1 Festlegungen nach Empfehlung — Widerspruch möglich

Sie verlangen keinen Entscheid; die Entwürfe, der Auftrag oder eine Hausregel beantworten sie. Ein Widerspruch ist bis
zur Beauftragung der genannten Stufe billig.

| Nr | Festlegung | Grund | bis |
|---|---|---|---|
| F1 | Fünf Größen je Zone; Beleuchtung in „Geräte", Heizbetrieb als „aus" (3.1) | Auftragswortlaut samt Ergänzung | KP1 |
| F2 | *Fortgeschrieben (Rev. 2):* Die Zone erbt die Matrix je Zelle (leer = Gebäude) und je Größe den ganzen Kalender oder führt einen eigenen; „übernehmen und anpassen"; Anlegen erhält Zonenwerte (3.3, 3.4) | Vorgabenkaskade von heute; nur so bleibt Anlegen ergebnisneutral | KP1 |
| F3 | *Ersetzt durch den Auftrag vom 26.09.2026:* statt Voreinstellungen im Code Matrix, Vorlagen im Katalog und Werkzeuge der Karte (3.5) | Ergänzung des Auftrags | — |
| F4 | STRICT-Tabellen, die Woche als H8-Text (5.1, 5.2) | `CLAUDE.md` (STRICT, IDs, CHECK); H8 gilt für einen Wochenvektor | KP1 |
| F5 | *Fortgeschrieben (Rev. 2):* Abgeleitet bis angelegt — die Matrix ist der Generator, ohne angelegten Kalender rechnet der Lauf aus ihr; kein DML an Bestandsdaten, alte Spalten bleiben (E89; 3.3, 5.4) | bitgleich durch Bau; Altweg, Import, Export lesen weiter | KP1 |
| F6 | Lineare Treppe, letzte Stufe in der Sprungstunde (4.1) | „sukzessiver Anstieg"; n = 1 ist heute | KP3 |
| F7 | Bemessung mit der geschlossenen Stufenformel in einem Lauf, Nachweisband, Überlagerung und Vorausrechnung nur als Prüforakel (4.3) | exakt bei festen Randwerten, sicher sonst; kein Zweitlauf | KP3 |
| F8 | Rundung auf das kleinste haltende n, keine Mindestrampe (4.6) | eine Rampe von 1 h verschiebt den Sprung nur | KP3 |
| F9 | Schalter je Projekt, Vorgabe aus (4.8) | Byte-Gleichheit aller Bestandsprojekte | KP3 |
| F10 | Vorkühlen später, mit KU3 (4.7) | nicht beauftragt | KP3 |
| F11 | Bundeseinheitliche Feiertage als Werkzeug der Karte, als Regel gespeichert (3.2) | Referenzjahr und Schaltjahr verschieben Jahrestage | KP1 |
| F12 | Wiki gebündelt, Version beim Anwender (10.4) | `CLAUDE.md` | KP4 |
| F13 | AK1-gekoppelte Einzonengebäude in KP3 benannt ausgenommen (4.7) | nichtlinear; die Übergabe kappt ohnehin | KP3 |
| F14 | Neues Referenzprojekt, Bestand unberührt, Einfrierregel „gesäte Konditionierungsdaten" (10.2, 10.3) | Regressionsnetz | KP3 |
| F15 | *Fortgeschrieben (Rev. 2):* Lüftung absolut in 1/h Nutzerlüftung, Infiltration konstant darunter (3.1) | die Nachtauskühlung verlangt Werte über dem Tageswert; 1/h ist volumenbezogen und vererbt sich sinnvoll | KP1 |
| F16 | Nutzungszeit aus dem Personenkalender (3.4) | folgerichtig; Rampenstunden zählen nicht | KP1 |
| F17 | Stündliche Kühlprüfung, Rampe an θ_K(h) − 1 K gekappt (3.6) | eine Optimierung bricht keinen Lauf ab | KP1 |
| F18 | Bemessung (a) als Vorgabe, (b) wählbar, ΔT_K = 2 K, nur für die Höchstzeit (4.5) | Auftragswortlaut; EPOS-Wert | KP3 |
| F19 | Nachtfenster je Spalte der Matrix, leer = das der Heizspalte (3.3) | Nachtauskühlung und Anwesenheit brauchen eigene Zeiten; leer bleibt es ein Eintrag | KP1 |
| F20 | *Mit E53 entschieden (Heizperiode):* Zeile Saison für Heizen und Kühlen — Heiz- und Kühlperiode mit Datum für Start und Ende, innerhalb wirksam, außerhalb „aus", leer = ganzjährig; außerhalb der Heizperiode liefert der Wärmeerzeuger nur Warmwasser und Prozesswärme (3.2, 3.3) | Auftragspunkt 2 („Heizperiode von … bis"); E53 | KP1 |
| F21 | `Maximaleraumtemperatur` bleibt ein Einzelwert neben der Matrix — die Grenze der Überhitzungsstunden, kein Kühlsollwert; geprüft gegen den höchsten Heizsollwert der Nutzungszeit (3.6) | E32; der Kühlsollwert hat die eigene Spalte | KP1 |
| F22 | *Fortgeschrieben (Rev. 3, P11):* Ausgelieferte Vorlagen Wohnen, Büro, Schule als EPOS-Muster je Größe — 14 in fünf Listen —, `ReadOnly`, gesät mit KP2; neutrale Namen ohne Produktdaten, Doppelnamen je Liste benannt abgelehnt (3.5, 5.7) | P4, P11; Namensregel der Kataloge | KP2 |

### 9.2 Entscheide des Anwenders (E52, 26.09.2026)

Wortlaut: **„P1, P2: Empfehlung / P3: (b) / P4 bis P8: Empfehlung"**. Der Entscheid steht als Nachtrag N1.59 im
[Leitkonzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md); Frage, Hintergrund und alle Optionen stehen im
[Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) Kapitel 10.

| Nr | Frage | Entscheid | Folgen |
|---|---|---|---|
| **P1** | Was bedeutet `Interne_Waermegewinne` unter Kalendern? | **(b)**, nach Empfehlung: Geräte und Personen getrennt; beim Anlegen des Personenkalenders wird der Geräte-Nennwert = `Interne_Waermegewinne` − Jahresmittel der Personenwärme, energieerhaltend und sichtbar | keine Doppelzählung der Personenwärme; gilt auch beim Füllen der Personenspalte der Matrix; gilt nicht für die Übernahme eines Nutzungsprofils mit eigenem Personennennwert (E93); Datenbankfall „Jahresmittel vor und nach dem Anlegen gleich" (R1); KP1 |
| **P2** | Soll die Heizung stundenweise „aus" sein können? | **(b)**, nach Empfehlung: Kalenderwoche mit Kennwort „aus" je Zelle, Wochenraster mit Schalter je Zelle, Übergang aus „aus" ohne Rampe (W4) | eigener Kalenderwochenleser (5.2), „aus" auch als Zustand einer Matrixzelle, Vorlaufstart nach 3.6; ein Zweitlauf entfällt; KP1 und KP2 |
| **P3** | Sollen Katalogbauten der Auslieferung Kalender tragen? | **(b), abweichend von der Empfehlung (a):** ja — Eigentümer `ID_Gebaeude_Stamm`, Kopierweg Katalog → Projekt, Auslieferungsvorlage und Prüfbericht | Eigentümerregel und Teilindex (5.1); Kopierwege über `CopyFromStamm`, „Speichern unter", Katalogkopie und Schloss (5.5); Matrix und Karten in allen Katalogmodi (7); Einfrierregel für gesäte Katalogkalender (10.3); Mehraufwand 2–3 PT in KP1 und 1 PT in KP2 (8) |
| **P4** | Nutzungsmuster für Nichtwohnbauten (Büro, Schule) als Voreinstellung? | **(a)**, nach Empfehlung: ja, als EPOS-Muster mit runden Werten | ausgelieferte Vorlagen Büro und Schule (F22); der Ausschluss in Leitkonzept 15 (mit N1.59 nachgezogen) und Anlagenkopplung 1.3 (Nachzug in KP0) ist auf Normprofile verengt; KP2 |
| **P5** | Woran bemisst sich P_auf ohne `Heizleistung_Max`? | **(b)**, nach Empfehlung: (1 + ρ) × stationäre Last an der kältesten Stunde, ρ = 20 % nach der KP0-Probe | (a) ist immer erreichbar; Rampen entstehen vor allem an kalten Tagen und in gut gedämmten Bauten (4.5); KP3 |
| **P6** | Aufheizzeit täglich berechnet oder fest? | **(a)**, nach Empfehlung: täglich, ≤ t_auf,max; **(b)** fest ist wählbar | Spalte `Aufheiz_Art`, NULL = täglich (5.3); an milden Tagen keine Rampe und keine Mehrwärme; KP3 |
| **P7** | Kommt das Zeitprofil der Kühlung (K11) jetzt in den Kalender? | **(a)**, nach Empfehlung: ja; `Kuehl_Sollwert_Nacht` bleibt ungelesen und füllt nur die Voreinstellung vor | E27 ist bei K11 geändert: das Zeitprofil kommt mit KP1 statt mit KU3; der Wortlaut „bleibt ungelesen" ist mit P13 (a) entfallen (E53) |
| **P8** | Wie zeigt das Ergebnis den Vergleich mit und ohne Rampe? | **(a)**, nach Empfehlung: über eine Projektvariante; ein optionaler Vergleichslauf bleibt spätere Wahl | keine Mehrrechenzeit, kein Zweitlauf; KP3 |

### 9.3 Entscheide des Anwenders (E53, 26.09.2026)

Wortlaut: **„P9: (b) / P10: (b) / P11: Eine Vorlage mit vorbefülltem Kalender zur Auswahl aus mehreren Kalendern. /
P12: unklar / P13: (a) / Heizperiode: Wird vom Benutzer vorgegeben mit Datum Start und Datum Ende. In dieser Zeit ist
der Heizwärmeerzeuger aus."** P11, P12 und die Heizperiode sind per Rückfrage geklärt (Spalte „Entscheid"). Der
Entscheid steht als Nachtrag N1.60 im [Leitkonzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md); Frage,
Hintergrund und alle Optionen stehen im [Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) Kapitel 10.

| Nr | Frage | Entscheid | Folgen |
|---|---|---|---|
| **P9** | Wirkt die Nachtauskühlung unbedingt oder nur unter einer Bedingung? | **(b)**, nach Empfehlung: bedingt wie die Sommerlüftung — Raumluft über der Schwelle (23 °C, mit wirksamer Kühlung θ_K − 3 K), Außenluft mindestens ΔT kühler (Vorgabe 2 K), aus mit 1 K Hysterese; sonst gilt der Tageswert | kein Schalter „bedingt", `Bedingt_K` ist allein ΔT (NULL = 2 K, 5.6); keine Nachtauskühlung in Heiznächten, die Heizwärme bleibt (N-NK3, R12); KP1 |
| **P10** | Wo stehen die neuen Zellen der Vorgabe-Matrix? | **(b)**, nach Empfehlung: eigene Tabelle `Tab_Konditionierungsvorgabe` je Eigentümer, Größe und Zeile, auch für Katalogbauten und Vorlagen; die heutigen Felder bleiben ihre Zellen, ein Ort je Zelle, kein DML | kein Sichtneubau, keine neue Gebäudespalte; die Matrix liest aus zwei Quellen (R11); KP-S1 in KP1 |
| **P11** | Ist eine Vorlage ein Satz aller fünf Größen oder gibt es Vorlagen je Größe? | **(b), abweichend von der Empfehlung (a)**, per Rückfrage geklärt: je Größe eine Liste vorbefüllter Kalender (Wohnen, Büro, Schule, eigene); der Anwender wählt je Größe einen, Sätze aller Größen gibt es nicht | eine Vorlage gehört einer Größe (`Groesse`, Name eindeutig je Liste, 5.7); Auswahlliste und „Als Vorlage speichern" je Kalenderkarte (7.4, 7.5); 14 ausgelieferte Vorlagen in fünf Listen (3.5, F22); Aufwand in der Summe gleich (8) |
| **P12** | Was geschieht mit einem angelegten, einzeln geänderten Kalender, wenn die Matrix erneut angewendet wird? | **(a)**, nach Empfehlung, per Rückfrage geklärt (Wortlaut „unklar"): nur den Matrixbereich ersetzen — Standardwoche, Ferien- und Saisonperioden —, eigene Perioden und Ausnahmetage bleiben, Rückfrage vorher | dieselbe Regel beim Übernehmen einer Vorlage auf einen angelegten Kalender (3.5); Probe „erneut anwenden ersetzt genau den Matrixbereich" (6); KP1 und KP2 |
| **P13** | Wird `Kuehl_Sollwert_Nacht` die Zelle Kühlen/Nacht der Matrix? | **(a)**, nach Empfehlung: ja — die Bestandsspalte ist die Zelle und wirkt über den Generator | P7 (a) gilt im Kern weiter, der Wortlaut „bleibt ungelesen" entfällt; wo die Spalte gefüllt ist, ändert sich der Kühlfahrplan (Testdatenbank: nirgends; R14); KP1 |
| **Heizperiode** | Was heißt „Heizperiode von … bis" (Auftragspunkt 2, F20)? | per Rückfrage geklärt: Der Anwender gibt **Start und Ende als Datum** vor; innerhalb wird geheizt, außerhalb steht die Raumheizung auf „aus" (P2), der Wärmeerzeuger liefert dann nur Warmwasser und Prozesswärme; die **Kühlperiode** gilt entsprechend | Saisonzeile der Matrix als eine Periode „aus" im Rang über den Matrixperioden (3.2, 3.3); Löser ohne Heizung, Gebäudewärme im Kanal Raumwärme 0, Vorlauf der Kopplung leer und getrennt gezählt (6); Beginn der Heizperiode ohne Rampe, W4, Bemessung innerhalb (4.7); Hinweis bei Untertemperatur außerhalb (3.6); abgeschaltet wird kein Erzeuger, dafür bleibt dessen Fahrplan (AK2); KP1 |

### 9.4 Entscheide des Anwenders (E54, 27.09.2026)

Zwei Fragen aus der Synthese des [KP1b-Entwurfs](../ueberholt/Entwurf_KP1b_Konditionierungsprofile.md) (Abschnitt 5), per Auswahl entschieden. Der
Entscheid steht als Nachtrag N1.62 im [Leitkonzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md); im Register
standen beide Fragen nicht.

| Frage | Entscheid | Folgen |
|---|---|---|
| Was nimmt „Als Vorlage speichern" an Nennwert (Watt bei 100 %, Infiltration) und Saison (Heiz- bzw. Kühlperiode) mit? | **Weder Nennwert noch Saison — abweichend von der Empfehlung** (Saison ja, Nennwert nein) und von 3.5 in Rev. 3 | eine eigene Vorlage trägt Zeitstruktur und die Werte der Nutzungszeilen (Tag, Nacht mit Zeiten und `Bedingt_K`, Wochenende, Ferienwert); beim Übernehmen bleiben Nennwert und Saison des Ziels (leere Zellen der Vorlage, 3.5); die 14 ausgelieferten Vorlagen tragen ohnehin keines von beiden (F22); 3.5 und 5.7 fortgeschrieben; KP1b (Vorlagen-Controller), KP2 |
| Kommen mit der Nachtauskühlung auch die Sommerlüftungsstunden in Bericht und Export? | **Beide Kennzahlen**, nach Empfehlung | KP3 bringt `Nachtauskuehlstunden_H` und `Sommerlueftungsstunden_H` in Bericht, CSV-Export, KI-Sicht und Variantenvergleich (rund 0,25 PT zusätzlich); 3.7 und 8 fortgeschrieben |

### 9.5 Entscheid des Anwenders (E55, 29.09.2026)

Festlegung N1.63 Nr. 12 der Umsetzung KP1b, zur Bestätigung vorgelegt. Der Entscheid steht als Nachtrag N1.64 im
[Leitkonzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md).

| Frage | Entscheid | Folgen |
|---|---|---|
| Welche Stunden nehmen die Auslegungswerte (Übergabe, Kälte, Heizlast) und die Prüfung F21 als Nutzungszeit — die Nachtzeit oder die Personenmaske (F16)? | **Die Nachtzeit**, nach Empfehlung („bei der Nachtzeit bleiben“) | keine Codeänderung; die Personenmaske bleibt auf die Kennzahlen beschränkt (F16); 3.6 nennt es |

### 9.6 Entscheide des Anwenders (E56, 29.09.2026)

Fünf Fragen aus dem [Entwurf KP2](../ueberholt/2026-09-29_Entwurf_KP2.md) (Abschnitt 6), per Auswahl entschieden, alle nach
Empfehlung. Der Entscheid steht als Nachtrag N1.65 im [Leitkonzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md).

| Frage | Entscheid | Folgen |
|---|---|---|
| F1 Welche Werte tragen die 14 ausgelieferten Vorlagen (ab KP3 eingefroren)? | **Die vervollständigte Tabelle des Entwurfs samt Feiertagen** bei Büro und Schule | 3.5; Schemaschritt der Saat (KP-S1b) mit 46 Vorgabezeilen und den Feiertagsregeln |
| F2 Folgt ein angelegter, nicht von Hand geänderter Kalender der Matrix? | **Ja** — ohne Rückfrage, solange sein Matrixbereich dem Generator gleicht; danach P12 | 3.3, 7.2; Stufe 2 „Matrix anpassen" wirkt auch nach „Vorlage wählen" |
| F3 Wo stehen die Altfelder (Wärmegewinne, Infiltration, Nutzerlüftung, Kühlsollwert, Sommerlüftung, Maximalraumtemperatur)? | **Nur im Reiter „Konditionierung"**, Reiter 1 zeigt eine Herleitungszeile | 7.1; eine Eingabestelle je Wert |
| F4 Wo liegt die Vorlagenverwaltung? | **In jeder Kalenderkarte** als Blatt im Katalogeditor, auf beiden Plattformen | 7.4; kein Menüpunkt, iOS-Hülle unverändert |
| F5 Wie wird die Gesamtangabe `Luftwechselrate` aufgeteilt, sobald die Lüftung eine Vorgabe bekommt? | **Aufteilen, die Summe bleibt:** Infiltration = min(0,3 1/h; Rate), Nutzerlüftung = der Rest, nach einer Rückfrage | 3.1 (F15) |

### 9.7 Entscheid des Anwenders (E57, 30.09.2026)

Die Abkürzung „gleichnamige Vorlage in allen Größen übernehmen" hatte der [Entwurf KP2](../ueberholt/2026-09-29_Entwurf_KP2.md) (Abschnitt 6)
nicht gefragt, sondern an die gezählten Handgriffe gebunden; vorgelegt mit dem Zählfall KN6 der Welle U2, per Auswahl
entschieden. Der Entscheid und die Festlegungen seiner Umsetzung stehen im Nachtrag N1.66 des
[Leitkonzepts](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md).

| Frage | Entscheid | Folgen |
|---|---|---|
| Bekommt die Matrix eine Abkürzung „gleichnamige Vorlage in allen Größen übernehmen"? Gezählt (KN6, „Büro" in allen fünf Größen): 11 Handgriffe breit, 12 an der Gesamtangabe der Lüftung, 16 mit fünf angelegten Kalendern, 15 schmal | **Aufnehmen** (Wortlaut „nehme auf") — ein Eintrag in der Zeile „Vorlage", eine Rückfrage für alle Größen, kein Satzbegriff; P11 bleibt | 7.2, 7.4; umgesetzt mit der Welle U5 (N1.66 Nr. 25–30): 3 Handgriffe in allen vier Fällen; R16 entschärft |

### 9.8 Entscheide des Anwenders (E58, 02.10.2026)

Acht Fragen aus dem [Entwurf KP3](../ueberholt/2026-10-02_Entwurf_KP3.md) (Abschnitt 6), per Auswahl entschieden: F1, F2, F5, F6 und F8 nach Empfehlung, **F3, F4 und
F7 abweichend**. Der Entscheid steht als Nachtrag N1.67 im [Leitkonzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md);
die Festlegungen der Umsetzung (Entwurf Abschnitt 5) folgen mit der Umsetzung als N1.69.

| Frage | Entscheid | Folgen |
|---|---|---|
| F1 Welche Leistung hält die Rampe bei Quelle `Heizleistung_Max`? | **(b)**, nach Empfehlung: Augenblickswert bei Quelle Grenze, Stundenmittel bei Zielleistung | 4.3, 4.5, 4.8 (W3); am Prüfsatz n = 7 statt 5 |
| F2 Ab welchem Sollwert bemisst sich ein Sprung nach gestufter Absenkung? | **(b)**, nach Empfehlung: der kleinste endliche Sollwert im Fenster der Rampe | 4.1, 4.5; bei einfacher Nachtabsenkung unverändert |
| F3 Wo stehen Nachtauskühl-, Sommerlüftungs- und Aufheizwerte im Variantenvergleich? | **(c) beides**, abweichend von der Empfehlung (a): Gebäudetafel je Stand (`stand.tabelle.gebaeude`, ohne Δ) und Kennzahlgruppe „Gebäude" mit Δ (`Kennzahl.Seit`) | 7.6; +0,75 PT in der Welle O3 |
| F4 Kurzbericht und Einzelfelder? | **(b)**, abweichend von der Empfehlung (a): Einzelfelder `gebaeude.ergebnis.*` und ein Aufheizabsatz im Kurzbericht, beide Sprachen | 7.6; +0,25 PT in der Welle O3 |
| F5 Zuschnitt des Referenzprojekts 1051 | **(a)**, nach Empfehlung: „Referenzprojekt Konditionierung", Kopie von 1007, Bau per Probe, „Büro" in allen fünf Größen, `Kuehlung_Aktiv` = 1 bei Kühlbetrieb aus, Nachtauskühlung nach F8, Sommerlüftung an, Ferien 23.12.–6.1. und 1.–14.8., Heizperiode 1.10.–30.4., Bemessung „kälteste Stunde − 2 K", täglich, ρ leer, achtes Projekt in der CI | 10.2 |
| F6 Wann wird eingefroren? | **(a)**, nach Empfehlung: nach Rechenweg, Export und Referenzprojekt (RP2 nach RP1); O1–O3 danach gegen R34 | 10.1 |
| F7 Bleibt die Aufheizreserve ρ bei 20 %? | **(c) Entscheid nach der Messung**, abweichend von der Empfehlung (a): RP1 misst ρ_min aller 17 VDI-Gebäude mit dem echten Kern (A/B-Protokoll); bis dahin 20 % | 4.4; offener Punkt P14 im Register, fällig vor RP2 |
| F8 Lüftungsvorlage „Büro" gegen Nachtauskühlung | **(a)**, nach Empfehlung: das Skript übernimmt „Büro" in allen fünf Größen und überschreibt danach die Lüftungs-Nachtzeile mit der Nachtauskühlung (2,0 1/h, 18–7 Uhr, bedingt); Wochenende und Ferien bleiben 0,1 1/h | 10.2; die ausgelieferte Saat bleibt |

### 9.9 Entscheide des Anwenders (E59, 02.10.2026; E60, 03.10.2026)

**E59** antwortet auf eine Vorgabe des Anwenders, nicht auf eine Frage des Papiers: „Die Rampe soll jeweils individuell
für ein Gebäude ermittelt werden und nicht pauschal. Ein Aufschlag auf diesen Wert könnte sinnvoll sein
(Benutzervorgabe). Außerdem soll es einen manuellen Wert als Eingabe geben — mit plausiblen Vorschlägen." Drei Rückfragen
hat der Anwender beantwortet; im [Entwurf KP3](../ueberholt/2026-10-02_Entwurf_KP3.md) steht die Vorgabe als F9
(Abschnitt 6), ihre Ausgestaltung als Festlegungen 34–40, 42 und 43 (Abschnitt 5). **E60** entscheidet Vorschlag 1 des
[Konzepts Heizlastspitzen](Gebaeudesimulation/2026-10-03_Konzept_Heizlastspitzen_Glaettung.md) (Abschnitt 5), im
Entwurf Festlegung 41. Beide stehen als Nachtrag N1.68 im [Leitkonzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md).
Am 03.10.2026 hat der Anwender die drei Folgefragen P15–P17 des [Registers](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md)
und den Schemaweg entschieden (letzte vier Zeilen).

| Frage | Entscheid | Folgen |
|---|---|---|
| E59 (1) Wo liegt der manuelle Wert? | **je Gebäude**: dritte Art „manuell" am Gebäude, `Tab_Gebaeude.Aufheizzeit_Manuell_H` 1–47 h; Zonen erben, kein Feld am Katalog | 3.4, 4.6, 4.8, 5.3, 5.5, 7.6; Festlegungen 37–39 |
| E59 (2) In welcher Einheit wird der Aufschlag eingegeben? | **Stunden und Prozent, es gilt das Maximum**: `Aufheiz_Aufschlag_H` 0–24 und `Aufheiz_Aufschlag_Prozent` 0–100 am Projekt, n' = min(48, n + max(A_h, ⌈n · A_%/100⌉)) auf jedes ermittelte n, nicht auf den manuellen Wert | 4.6, 5.3, 7.6; Festlegungen 35, 36; Geltungsbereich mit P16 entschieden |
| E59 (3) Wann wird umgesetzt? | **nach D2, vor RP1**: Welle R5 (Rechenweg, Schritt KP-S4, Testdatenbank, Export), O1b (Projekteinstellung), Erweiterung von O2 (Feld mit Vorschlägen: bemessene Zeit und Spanne) und O3 (Abweichungsmerkmale) | 5.4, 8; KP3 16,75–21,75 PT mit E60; die Spanne mit P15 entschieden |
| E59 „individuell" | Der Rechenweg ist es schon — Stufenformel, Bemessung und P_auf je Gebäude und Zone —; pauschal bleiben ρ (P14), die Art und der Aufschlag | 4.6; Festlegung 34 |
| E60 Welche Größe gilt für die Heizungsauslegung? | **Vorschlag 1**: stationäre Auslegungsheizlast plus P_auf aus der KP3-Bemessung; Bedarfsdialog (O2) und Bericht (O3) stellen ideale Spitze, Tagesmittel und P_auf mit Quelle nebeneinander, mit dem Hinweis, dass die ideale Spitze bei Schalter aus keine Auslegungsgröße ist; kein Filter im Rechenweg, die Kennzahl „Spitze als n-h-Mittel" (Vorschlag 2) nicht beauftragt | 4.8, 7.6; Festlegung 41; die Lesart der Summe mit P17 entschieden |
| P16 Trifft der Aufschlag auch Sprünge ohne Rampe? (03.10.2026) | **Nein — kalenderbezogen:** Der Aufschlag gilt nur an Rampen, die ein Sprung des Heizsollwerts auslöst, und verlängert nur Rampen mit n > 1; an Sprüngen ohne Rampe (n = 1) und an Tagen ohne Sprung kein Aufschlag; Begrenzung auf D + 1 und W2 bleiben | 4.6; Festlegung 35 |
| P17 Wie ist die Auslegungsgröße zu lesen? (03.10.2026) | **(b)**: Auslegungsheizlast plus Aufheizzuschlag P_auf − Φ_stat, nach dem Muster Φ_RH der DIN EN 12831-1, mit den Teilen daneben | 4.8, 7.6; Festlegung 41; Ergebnisspalten `Auslegungsheizlast_Kw`, `Aufheizzuschlag_Kw` |
| P15 Woher kommt die Vorschlagsspanne der manuellen Aufheizzeit? (03.10.2026) | **(b)**: aus τ₂ des Gebäudes (langsame Zeitkonstante des Zweikapazitätenmodells), keine feste Bauart-Tabelle; dazu die bemessene Zeit aus der Herleitungszeile | 7.6; Festlegung 40 mit der Regel [max(1, t_auf,max); min(47, ⌈τ₂ · ln 10⌉)] |
| Schemaweg des Schritts KP-S4 (03.10.2026) | Neue Ergebnisspalte `Aufheiz_Art` an Gebäude und Zone per `ADD COLUMN`, `Aufheiz_Bemessung` behält die Variante, kein Neubau von `Tab_ErgebnisGebaeude`; `GEKOPPELT` an der Zone als kleiner Neubau von `Tab_ErgebnisZone` im selben Schritt; Export `Geb[n].Aufheizart`, Abweichungsmerkmal „Art" | 4.8, 5.3, 5.4; Festlegungen 39, 43 |

### 9.10 Entscheide des Anwenders (E110, E111, E112, E114, 09.10.2026; E115, 10.10.2026)

Der Anwender hat am 09.10.2026 das Mockup [`Mockups/Konditionierung_Kalender.html`](Mockups/Konditionierung_Kalender.html) angenommen und damit die
Empfehlungen des [Befundpapiers](../ueberholt/2026-10-09_Befund_Kalenderbedienung_Konditionierung.md) (Abschnitt 4)
entschieden. Ausgestaltung in 7.8, Stufen in 8.

| Frage | Entscheid | Folgen |
|---|---|---|
| Welche Variante? | **V2 „Wochenprofile" als Grundgerüst, ergänzt um das Jahresraster aus V1 als anklickbare Anzeige** | 7.8; Stufe 1 ohne Schemaschritt |
| Gelten Wochenende, Ferien, Feiertage und Ausnahmetage für alle Größen? | **gemeinsam für alle fünf Größen, je Zeile „gilt für" abwählbar** | 7.8; Stufe 1 als gekoppelte Kopien, Stufe 2 gemeinsamer Eigentümer |
| Bleibt die Vorgabe-Matrix? | **als Übersicht**; ihre Zeilen Wochenende und Ferien werden über die Schnellfelder bedient | 7.2, 7.8 |
| Wie viele Ferienzeiträume? | **beliebig viele, als Perioden** | 7.8; die ersten vier spiegeln die Gebäudespalten |
| Feiertage je Bundesland? | **alle 16 Länder als Regeln, Wahl am Gebäude** | Stufe 2 (Schemaschritt 207); in Stufe 1 bundeseinheitlich plus feste Datumsbereiche |
| Liest der Tagesbilanz-Weg das Wochenende des Gebäudes und die Ferienliste? (E111) | **nein** — er bleibt bei Samstag und Sonntag und den vier Ferienspalten `Ferienbeginn/-ende_1…4` | 7.8; der Tagesbilanz-Weg ist eingefroren (`Altweg/`), die Einschränkung ist benannt; Generator und Zapfkalender lesen Wochenende und Ferienliste |
| Zählen Feiertage im Zapfkalender auch mit der Vorgabe Samstag + Sonntag als Sonntag? (E112) | **ja** — Feiertage zählen im Zapfkalender unabhängig von der Wochenendmaske als Sonntag, auch am Samstag | 7.8; die Feiertage kommen aus den Regeln des Kerns (Feiertagsland des gebundenen Gebäudes, ohne Gebäude bundeseinheitlich); Basis R47 |
| Hängt die Lage der beweglichen Feiertage an einem Jahr? (E114) | **nein, im Regelfall ist kein Jahresdatum relevant** — Ostern ist der Sonntag des Wochentagsrasters am nächsten zum 8. April, Buß- und Bettag der letzte Mittwoch vor dem 23. November; nur eine Preisreihe mit Jahr setzt die echten Daten | 3.2; `Gemeinjahrkalender`, `Feiertage.Jahrestag`, `Landesfeiertage.Jahrestage`; Konditionierung, Jahresraster, Teppich, Betriebskalender und Zapfkalender lesen dieselbe Konvention; Basis R48 |
| Mit welchem Wochentagsraster rechnen Gebäudelauf, Zapfkalender und Bedarfsprofile? (E115, 10.10.2026, „Empfehlung") | **mit einem, im Regelfall dem der Klimaregion** — Daten statt Datum; im Sonderfall mit Preisreihenjahr Raster und Feiertage des echten Jahres für alle Leser (`Gemeinjahrkalender.Kalenderjahr`); ohne Projekt oder Klimaregion das Rückfallraster (1. Januar = Sonntag); der Arbeitsstand eines Projektgebäudes rechnet im Raster seines Projekts | 3.2; `Konditionierungdatenweg.Raster` als gemeinsame Auflösung, Wochenendmaske des Gebäudelaufs, Arbeitsstand, Teppich, Nutzungstage, Betriebskalender; Bedienung nennt das Raster statt eines Jahres; `Konditionierungsarbeit.Abdruck` schlüsselt w₀ und Jahr; Basis R50 (1051 und 1052 ändern sich, die übrigen Referenzprojekte bleiben byte-gleich) |

## 10. Nachweise, Abnahme, Einfrierregel, Wiki

### 10.1 Nachweise je Stufe

- **KP1:** Rechenproben und Datenbankfälle nach 6, N-NK1 bis N-NK4, Vorlagenlauf der Auslieferungsvorlage mit
  Katalogkalendern und Vorlagen; **Referenzlauf byte-gleich** gegen die geltende Basis (heute
  `2026-09-26_R22_Solarthermie`, fünfzehn Projekte) — kein Referenzprojekt trägt einen Kalender oder eine neue
  Matrixzelle. **KP2:** bunit und Wachen (7.7), `Proben/ChartProben` mit dem Teppichbild, Sichtabnahme in
  `EPOS_Plan.exe` in Projekt- und Katalogmodi mit Zählung der Schritte „je Größe eine Vorlage wählen, zwei Zellen
  ändern" (KN6); Referenzlauf byte-gleich.
- **KP3:** N-AH1 bis N-AH10; alle Bestandsprojekte byte-gleich (Schalter aus); das neue Referenzprojekt mit
  A/B-Protokoll; neue Basis mit Protokoll in [`Referenzlaeufe/LIESMICH.md`](../../Referenzlaeufe/LIESMICH.md);
  `kern.yml` rechnet das Projekt als achtes mit. Eingefroren wird nach Rechenweg, Export und Referenzprojekt (Welle RP2
nach RP1, E58 F6); Oberfläche und Bericht werden danach byte-gleich gegen die neue Basis abgenommen. Das A/B-Protokoll
trägt ρ_min aller 17 VDI-Gebäude (E58 F7, 4.4). **iOS:** Die Hülle bleibt unverändert, keine
  `Dienste.*`-Schnittstelle wird berührt; der grüne Kern-Lauf ist der Nachweis, ein iOS-Lauf nur auf Zuruf.

- **K1a/K1b/K2 (Kalenderbedienung, E110):** K1a mit Kern-Tests der Schicht `Kalenderbedienung` auf einem Arbeitsstand
  (Wochenprofile, gekoppelte Kopien, Einzeltage, Ferien mit Spiegelung der vier Gebäudespalten, Feiertage, Monatskopie,
  Jahresraster, Vorlage für alle Größen) und dem Lesen von Referenzprojekt 1051 ohne Schreibzugriff; K1b mit bunit und
  Sichtabnahme; K2 mit Schemaproben und Kopierwegen. Jede Welle: Referenzlauf 1051 und 1052 byte-gleich — die
  Einfrierregel 10.3 bleibt unberührt, weil keine gesäte Zeile geändert wird.

### 10.2 Das neue Referenzprojekt

**„Referenzprojekt Konditionierung", Nummer 1051** — eine Kopie von 1007, dem Projekt mit genau einem Gebäude auf dem
VDI-Weg (1030 rechnet ohne Gebäude, 1040 auf dem Altweg; E58 F5). Das Gebäude kommt **über die Katalogübernahme** aus
einem Katalogbau, auf den das Referenzskript die gesäten Vorlagen „Büro" aller fünf Größen übernommen hat — so stehen
Vorlagenweg je Größe und Kopierweg Katalog → Projekt (P3) im Regressionsnetz: angelegte Kalender aller fünf Größen,
Ferienzeiträume (23.12.–6.1. und 1.–14.8.), Feiertage, eine **Heizperiode** 1.10.–30.4. (E53), die
**Nachtauskühlung** (P9), die Sommerlüftung und die Aufheizoptimierung in Variante (b) mit 2 K, täglich, Reserve leer;
`Kuehlung_Aktiv` = 1 bei Kühlbetrieb aus, die Zuordnung mit der Fläche des Baus. **Nachtlüftung (E58 F8):** Die
Vorlage „Büro" senkt die Lüftung nachts auf 0,1 1/h; das Skript übernimmt sie in allen fünf Größen und überschreibt
danach die Nachtzeile der Lüftung mit der Nachtauskühlung — 2,0 1/h von 18 bis 7 Uhr, bedingt —, Wochenende und Ferien
bleiben bei 0,1 1/h, die ausgelieferte Vorlage bleibt unverändert. Weil die Rampe mit der Vorgabe nur in gut
gedämmten Bauten oder mit knapper `Heizleistung_Max` wirkt (4.5) und der Bau von 1007 sie nur an einem Morgen trägt,
wird der Bau **per Probe mit dem echten Kern** gewählt (Strahlungsanteil 0,3; [Entwurf KP3](../ueberholt/2026-10-02_Entwurf_KP3.md), Festlegung 31), so dass die Rampe
an kalten Tagen greift. Die Nummer 1051 ist die nächste freie nach 1050 und wird beim Einfrieren gegen die
Testdatenbank geprüft; das Skript liegt unter `Referenzlaeufe/Skripte/`, die CI rechnet das Projekt als achtes. Zonenkalender deckt das Projekt nur, wenn der Einfrierschritt
mit G6d zusammenfällt (R8).

### 10.3 Einfrierregel „gesäte Konditionierungsdaten"

Mit KP3 kommt in [`CLAUDE.md`](../../CLAUDE.md), Abschnitt „Regressionsnetz", und in `Referenzlaeufe/LIESMICH.md` eine
neunte Einfrierregel: *Wer gesäte Konditionierungsdaten eines Referenzprojekts ändert, friert im selben Schritt die
Basis neu ein.* Betroffen sind die Vorgabe-, Kalender- und Periodenzeilen seiner Gebäude und Zonen samt Nennwerten,
Heiz- und Kühlperiode und Nachtauskühlung, die **gesäten Katalogkalender und Vorlagen**, aus denen sein Referenzskript
übernimmt (`ID_Gebaeude_Stamm`, `ID_Vorlage`), `Aufheizoptimierung` und die Spalten `Aufheiz_*` seiner
`Tab_Einstellungen` (darunter der Aufschlag), `Aufheizzeit_Manuell_H` seiner Gebäude, das Referenzjahr seiner Spotpreisreihe (es verschiebt Wochentage und Feiertage) und das Anlegen
oder Entfernen eines Referenzprojekts mit Kalender oder Aufheizoptimierung.

### 10.4 Wiki-Änderungen (nur Liste; Veröffentlichung gebündelt nach KP3)

| Seite (`Projekte/Wiki/`) | Abschnitt | Änderung |
|---|---|---|
| `Programm Dokumentation - Gebäude.wiki` | Reiter „Temperaturen und Ferien" (Anker `raumtemperaturen`, `nachtzeit`, `ferienzeiten`) und der Abschnitt „Temperaturen und Ferien" aus #571 (Anker `temperaturen-und-ferien`, `wochenendabsenkung`, `ferienabsenkung`) | Reiter „Konditionierung" in Projekt und Gebäudekatalog (Anker `konditionierung`, `matrix`, `kalender`, `vorlagen`): Matrix mit Heiz- und Kühlperiode, Kalenderkarten, Vorlagen je Größe wählen und speichern, Schloss ausgelieferter Sätze; die alten Anker und die aus #571 bleiben im neuen Abschnitt, ebenso die Feldnamen „Soll am Wochenende (ganztägig)" und „Soll in Ferien (ganztägig)" — nachgezogen |
| `Programm Dokumentation - Gebäudemodell VDI 6007.wiki` | Eingaben, Lüftung, Ergebnisse, Grenzen | Sollwerte, Gewinne und Luftwechsel als Kalender aus der Matrix; neue Abschnitte „Heizperiode" (Anker `heizperiode`), „Nachtauskühlung" (Anker `nachtauskuehlung`) und „Aufheizoptimierung" (Anker `aufheizoptimierung`) — nachgezogen |
| `Programm Dokumentation - Mehrzonenmodell.wiki` | Zonen anlegen, Ergebnisse | Zonen erben Matrixzellen und Kalender oder führen eigene; der Satz über Nachtzeit, Ferien und Kühlung vom Gebäude wird ersetzt — nachgezogen |
| `Programm Dokumentation - Kühlung.wiki` | Eingaben im Gebäudedialog | Kühlspalte der Matrix, Kühlperiode, Nachtwert, Kühlkalender — nachgezogen |
| `Programm Dokumentation - Simulation.wiki` | Projekteinstellungen | Schalter Aufheizoptimierung, Variante (a)/(b), Aufheizreserve, Art — nachgezogen |
| `Programm Dokumentation - Simulationsergebnisse.wiki` | Gebäudekennzahlen | Kennzahlen der Aufheizzeit, Nachtauskühlungsstunden, Hinweise W1–W5 und der Hinweis der Heizperiode — nachgezogen |
| `Programm Dokumentation - Gebäudeimport.wiki` | Vorgaben | der Import setzt Matrixfelder, der Kalender entsteht auf Knopfdruck oder aus einer Vorlage — nachgezogen |

Die Seiten beschreiben die Funktion, wie sie ist, ohne Hersteller- und Produktdaten; Beispiele tragen neutrale Namen
mit runden Werten.

### 10.5 Logbuch-Entwurf

Ein Satz, veröffentlicht mit dem gebündelten Upload; die Versionsnummer ist beim Anwender zu erfragen:

> „Gebäude, Zonen und Katalogbauten führen für Heiz- und Kühlsollwert, Lüftung, Geräte und Personen stundengenaue
> Kalender, die eine Vorgabe-Matrix oder je Größe eine Vorlage belegt, mit Heiz- und Kühlperiode und Nachtauskühlung,
> und die Aufheizoptimierung ersetzt den Sollwertsprung nach einer Absenkung durch eine berechnete Aufheizrampe."

Die Sätze der Statuszeilen #690 bis #705 aus der Sitzung Gebäudesimulation, je ein Satz, geordnet; Version vom Anwender.
Wo die Statuszeile keinen Satz führt (#691, #692), ist er hier aus ihrem Titel entworfen und vom Anwender zu prüfen;
#695 bis #697 sind Referenz- und Einfrierschritte ohne sichtbare Änderung und bekommen keinen Eintrag, #690 ist
ein Testdatenbankschritt und nur der Vollständigkeit halber genannt:

| Zeile | Satz |
|---|---|
| #690 (G6d) | „Das Referenzprojekt 1052 mit drei Zonen steht in der Testdatenbank; die Referenzbasis ist unverändert, eingefroren wird mit RP2." *(intern, kein Logbuch-Eintrag nötig)* |
| #691 (IFC-Diagnose, Entwurf) | „Der IFC-Import übernimmt die Beheizungsart des CAD-Exports je Raum, wertet einen U-Wert von 0 oder darunter nicht als U-Wert und lässt eine unbeheizte Zone ohne Flächen weg." |
| #692 (IFC-Vorschläge, Entwurf) | „Der IFC-Import meldet einen Widerspruch zwischen dem Jahr der Datei und dem des Dateinamens, erkennt „getrennt beheizt“ an der Raumtemperatur und rechnet die Wärmekapazität masseloser IFC4-Schichten nach." |
| #693 (Nachzüge) | „Bauteilschichten sind ab 0,5 mm Dicke zulässig; der IFC-Import übernimmt damit Bleche ab 0,5 mm und übergeht nur noch Folien und Anstriche darunter." *(Satz steht laut Statuszeile unter 1.2.0.6)* |
| #705 (EV1) | „Im Gebäudedialog lässt sich der wirksame U-Wert der Bodenplatte als Wert vorgeben; leer rechnet das Programm die Erdreichkorrektur nach DIN EN ISO 13370. Die Aufheizreserve zeigt am leeren Feld die Vorgabe 20 %, und der Lauf meldet als Hinweis, wenn sie gilt." |

Version vom Anwender: offen.

Oberfläche und Bericht der Aufheizoptimierung (Wellen O1b, O2, O3; Statuszeilen #779, #780 und die der Welle O3), je ein Satz, Version vom Anwender:

| Welle | Satz |
|---|---|
| O1b, O2 (Aufschlag, manuell) | „Die Aufheizoptimierung lässt sich um einen Aufschlag in Stunden oder Prozent verlängern, und im Gebäudedialog kann die Aufheizzeit je Gebäude von Hand vorgegeben werden, mit der bemessenen Zeit als Vorschlag.“ |
| O2 (Gruppe „Aufheizung“) | „Der Bedarfsdialog zeigt in der Gruppe „Aufheizung“ die Aufheizzeit, die Aufheizleistung, die Rampentage und die Hinweise des Laufs und weist die Auslegungsgröße der Heizung aus Auslegungsheizlast und Aufheizzuschlag aus.“ |
| O3 (Bericht, Vergleich) | „Bericht und Variantenvergleich weisen die Aufheizung je Gebäude aus: Gebäudetafel, Aufheizabsatz im Kurzbericht, neue Vorlagenfelder und die Kennzahlgruppe „Gebäude“ mit der Abweichung je Kennzahl.“ |

Die Sätze der Oberfläche aus KP2 stehen im [Protokoll KP2](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-30_KP2_Konditionierung_Oberflaeche.md), Abschnitt 7.

## 11. Risiken

| Nr | Risiko | Herkunft | Prüfweg | Stand |
|---|---|---|---|---|
| RA1 | Überlagerung nicht exakt oder unsicher (Kappung, Fallwechsel, Zonen) | Entwurf A | entfällt mit F7; als Orakel N-AH3 mit gesetzter `Heizleistung_Max` | entschärft |
| RA2 | Rechenzeit eines Zweitlaufs (≤ 2,5-fach) | Entwurf A | entfällt mit F7; Messung im KP3-Abnahmelauf | entschärft |
| RA3 | Kollision mit G6c an `Tab_Zone` und bei den Schrittnummern | Entwurf A | Merge nach den Schemawellen von G6c, Nummer unmittelbar vor dem Schemacommit gegen `origin` | offen |
| RB1 | Ersatzmodell erster Ordnung zu grob | Entwurf B | entfällt mit F7; N-AH1 und N-AH4 (leicht, schwer, gedämmt) | entschärft |
| RB2 | „Anlegen" scheitert an Altwerten mit vielen Nachkommastellen | Entwurf B | Schreiber mit vier Stellen; Wache über alle Gebäudezeilen (heute 0 Fälle) | offen |
| RB3 | Nachbarzonen in der Bemessung nur festgelegt, nicht gerechnet | Entwurf B | N-AH9: zwei Zonen mit Trennfläche, Formel gegen Schleife, Band 1 % | offen |
| R1 | Doppelzählung der inneren Gewinne | Gegenprüfung | mit P1 ausgeschlossen; Datenbankfall: Jahresmittel der Gewinne vor und nach „Anlegen" gleich | entschärft |
| R2 | P_auf-Vorgabe wirkungslos oder unerreichbar | Gegenprüfung | Anker nach P5 an der kältesten Stunde; KP0-Probe: Überhöhung und t_auf,max (a)/(b) je Referenzgebäude | offen, durch 4.5 bestätigt |
| R3 | „aus" bricht Leser, Vorlaufstart (NaN) oder Sommerlüftungsschwelle (+∞) | Gegenprüfung | Rechenproben: „aus" in Stunde 8 040, Kühl-„aus" mit Sommerlüftung | offen |
| R4 | Teilindizes, gemischte Eigentümer und neue Tabellen in Werkzeugen und Schemakopien | Gegenprüfung | `SqlDialektPruefer`, `Testdatenbankschema`, Projektsicht, Katalogbereinigung und Tests der Auslieferungsvorlage im KP1-Gate | offen |
| R5 | Wochentags- und Feiertagsversatz beim Wechsel der Preisreihe oder im Schaltjahr | Gegenprüfung | Tests mit Referenzjahr 2024 und 2025; Feiertagsregel im Lauf aufgelöst | offen |
| R6 | Harte Kühlprüfung nach der Rampe bricht den Lauf ab | Gegenprüfung | Rechenprobe mit gestuftem Kühlkalender; Kappung an θ_K − 1 K benannt (F17) | offen |
| R7 | Zwischenspeicher des freien Falls wird bei stündlichem Luftwechsel ständig neu gebaut | Gegenprüfung | Laufzeitprobe mit Lüftungskalender und Nachtauskühlung; byte-gleich ohne | offen |
| R8 | Zonenkalender ohne Referenzabdeckung (0 Zonen in der Testdatenbank) | Gegenprüfung | Zonen im KP3-Referenzprojekt oder gemeinsames Einfrieren mit G6d | offen |
| R9 | Basis und Projektnummer wandern | Gegenprüfung | KP1/KP2 gegen die dann geltende Basis (heute R22); Nummer nach 1049 beim Einfrieren gegen die Testdatenbank | offen |
| R10 | Katalog- und Projektkalender laufen auseinander, oder die Auslieferungsvorlage verliert Katalogkalender | E52 (P3) | der Lauf liest nie den Katalog; Datenbankfälle Übernahme, erneute Übernahme, Katalogkopie, Schloss; Vorlagenlauf, Prüfbericht je Eigentümerart | offen |
| R11 | Die Matrix liest aus zwei Quellen (Bestandsspalten und Vorgabetabelle) und verliert eine Zelle oder liest sie doppelt | Rev. 2 (P10) | Wache „Matrix lesen = Bestandsfelder" über alle Gebäudezeilen; Datenbankfälle „aus" einer Bestandszelle und Kaskade | offen |
| R12 | Die Nachtauskühlung treibt die Heizwärme oder schaltet unruhig | Rev. 2, P9 (E53) | N-NK3: Winterwoche ohne Nachtauskühlungsstunde, Heizwärme unverändert; Hysterese wie die Sommerlüftung | offen |
| R13 | Vorlagen mit Doppelnamen oder Produktnamen, mit Inhalt einer fremden Größe, oder eigene Vorlagen gehen in die Auslieferung | Rev. 2, P11 (E53) | `UNIQUE (Groesse, Bezeichner)` ohne Groß-/Kleinschreibung, Größengleichheit im Controller mit Datenbankfall, `KonditionierungsvorlagenWacheTests`, Katalogbereinigung `--kataloge readonly` im Vorlagenlauf | offen |
| R14 | `Kuehl_Sollwert_Nacht` wird wirksam, wo sie in Anwenderdatenbanken heute gefüllt ist | Rev. 2, P13 (E53) | Testdatenbank: 0 Fälle; der erste Lauf nach KP1 nennt Gebäude mit Kühl-Nachtwert im Protokoll | offen |
| R15 | Die Heizperiode schneidet Heizbedarf ab: Untertemperatur außerhalb bleibt unbemerkt, oder ein Erzeuger ohne Warmwasser und Prozesswärme steht außerhalb still, ohne dass es auffällt | E53 (Heizperiode) | Hinweis mit Zähler (3.6); Kaskadenprobe: Gebäudewärme außerhalb 0, Warmwasser und Prozesswärme unverändert (6) | offen |
| R16 | Vorlagen je Größe kosten fünf Griffe statt einem (KN6) | P11 (E53) | Sichtabnahme mit Schrittzählung; gleiche Namen an derselben Stelle jeder Liste, Zeile „Vorlage" der Matrix (7.2); Zählfall KN6 als bunit-Probe | entschärft mit E57 — 3 Handgriffe über „alle Größen" (7.4) |

## 12. Verweise

**Papiere.** [Leitkonzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) (4.4–4.6, 5.6, 15, N1.32, N1.37, N1.48,
N1.55, N1.56, N1.59–N1.67); [Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) (Kapitel 10, K11);
[Statusdatei](Status_Gebaeudesimulation_VDI6007.md); [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md)
(3.4, 7.1, 11); [Anlagenkopplung](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) (1.3, 3.4, 3.7, 4.3, 4.4, 8.4,
8.6, 9.2, 10.1); [Mehrzonenmodell](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) (2.6, 2.9);
[Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) (4, 5, 7.2, 8.2, 8.3, 9);
[Datenaustausch](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) (13);
[Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) (6.1, Inventar der Altweg-Bestandteile);
[ADR-001](ADR-001_Schema-Ausrollung.md); [ADR-005](ADR-005_Zonenkopplung_Mehrzonenmodell.md);
[ADR-006](ADR-006_Trennung_Altweg_VDI6007.md);
[BETRIEB_SQLITE](BETRIEB_SQLITE.md) § 6; [Konzept Hilfesystem](Konzept_Hilfesystem_Wikidokumentation.md) 13;
[`Referenzlaeufe/LIESMICH.md`](../../Referenzlaeufe/LIESMICH.md); [`EPOS.UI/CLAUDE.md`](../../EPOS.UI/CLAUDE.md);
[Protokoll G6b](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-26_G6b_Mehrzonenrechnung.md);
[Entwurf KP2](../ueberholt/2026-09-29_Entwurf_KP2.md); [Protokoll KP2](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-30_KP2_Konditionierung_Oberflaeche.md);
[Entwurf KP3](../ueberholt/2026-10-02_Entwurf_KP3.md);
[Statusdatei der Migration](Status_iOS_Migration.md) #571 (Beleg der Sitzung „Dialoge und Korrekturen" zur
Semantik der Sollwerte, Feldnamen und Wiki-Anker).

**Entscheide und Registerpunkte.** E8 (Skalierung), E27 mit K11 (Zeitprofil der Kühlung, mit E52 geändert), E28 mit U4
(Glossar vor den Übersetzungen), E30 (Ergebnistabelle), E32 (freier Lauf ohne Kühlung), E36 (Rechenzeit der Kopplung),
E43 (Nachtzeit je Gebäude, Vorgaben), E49 mit A4 (a) (Kühlwerte vom Gebäude), E52 (P1–P8), E53 (P9–P13, Heizperiode),
E54 bis E58 (9.4–9.8),
N1.56 Festlegungen 1, 2, 7 und 10; F-K1 (Kühlsollwert leer = aus), F-P4 (Sommerlüftung); H5, H7, H8, H10 und H-F10 der
Anlagenkopplung; U3 (Platzhalter), U7 (Ortszeit-Kalender); A11 (Schrittnummern bei Beauftragung); K5 (Feuchte).

**Code.** `EPOS.Kern/Allgemein/Simulation/Gebaeude/` (`GebaeudeModellEingang.cs`, `Zonenmodell2K.cs`,
`Stundenrand.cs`, `Vdi6007Rechenweg.cs`, `Zonenrechnung.cs`, `Sommerlueftungsregel.cs`, `GebaeudeFestwerte.cs`,
`GebaeudeModellErgebnis.cs`, `HeizkreisErgebnis.cs`); `EPOS.Kern/Allgemein/Update/AnlagenkopplungSchema.cs`,
`NachtzeitSchema.cs`, `SchemaStand.cs`; `EPOS.Kern/Allgemein/Katalog/Katalogkopie.cs`,
`Auslieferungskennzeichen.cs`; `EPOS.Kern/Allgemein/Simulation/SimulationKanaele.cs`;
`EPOS.Kern/Controller/GebaeudeStammCtrl.cs`, `ProjektDuplizierenCtrl.cs`,
`SolardatenCtrl.cs`, `ZapfprofilCtrl.Eingang.cs`; `EPOS.UI/Bausteine/Wochenraster.razor`;
`EPOS.UI/Dialoge/Bedarf/GebaeudeStammblattFelder.razor`; `EPOS.UI.Daten/Bedarf/GebaeudeKatalogHuelle.cs`;
`EPOS.UI/Seiten/Simulation/SimulationKonfigSeite.razor`; `Werkzeuge/Testdatenbankschema/Program.cs`,
`Werkzeuge/Auslieferungsvorlage/Projektsicht.cs`; übrige Fundstellen in 1 und 3.3.

## 13. Glossar

Begriffe dieses Teilkonzepts, alphabetisch, je ein Satz; die Kapitelangabe führt zur Herleitung. Für Fachbegriffe der
Gebäudehülle und des Stundenmodells gilt [Glossar Lokalisierung](Glossar_Lokalisierung.md) § 13.

| Begriff | Bedeutung |
|---|---|
| **Absenkdauer D** | Die Zahl der zusammenhängenden Stunden vor einem Sprung, in denen der Sollwert endlich und unter dem Zielwert liegt; sie begrenzt die Aufheizzeit nach oben (4.1, 4.6). |
| **Abzug ΔT_K** | Der Temperaturabzug von der kältesten Stunde für die Bemessungsvariante (b); EPOS-Vorgabe 2 K, im Projekt 0–10 K einstellbar, wirkt nur auf die Höchstzeit (4.5). |
| **Aufheizleistung P_auf** | Die Leistung, die das Aufheizen höchstens beanspruchen darf: `Heizleistung_Max`, wenn gesetzt, sonst die Zielleistung (1 + ρ) mal die stationäre Last an der kältesten Stunde (4.4). |
| **Aufheizoptimierung** | Der Projektschalter (Vorgabe aus), der vor jedem Sollwertsprung nach oben eine berechnete Rampe einfügt, damit die Sprungspitze die Aufheizleistung nicht übersteigt (4, F9). |
| **Aufheizzeit t_auf** | Die Dauer der Rampe vor einem Sprung, (n − 1) Stunden bei n Stufen, höchstens die Absenkdauer D (4.1). |
| **Aufheizzeit manuell t_m** | Die je Gebäude eingegebene Aufheizzeit (1–47 h), mit der das Gebäude statt der ermittelten an jedem Sprung rampt; die Zonen erben sie (4.6, 7.6). |
| **Aufheizzuschlag Φ_RH** | Der Teil der Aufheizleistung über der stationären Last am Bemessungspunkt, max(0, P_auf − Φ_stat); er kommt zur Auslegungsheizlast hinzu (4.8). |
| **Aufschlag** | Die Projektvorgabe in Stunden und in Prozent, um die jede Rampe eines Kalendersprungs mit n > 1 länger wird; es gilt der größere Wert, höchstens bis zum Deckel (4.6). |
| **Auslegungsgröße** | Die Leistung, nach der die Heizung ausgelegt wird: die stationäre Auslegungsheizlast plus der Aufheizzuschlag, nicht das Maximum der idealen Last (4.8). |
| **Ausnahmetag** | Ein Tag, den eine eigene Periode (Ferien, Betriebspause, Feiertag oder „wie Wochentag X") statt der Standardwoche bestimmt; er bleibt erhalten, wenn die Matrix erneut angewendet oder eine Vorlage übernommen wird (3.2, P12). |
| **Bemessung (a) und (b)** | Die zwei Varianten der kältesten Außentemperatur für die Höchstzeit: (a) die kälteste Stunde selbst (Vorgabe) oder (b) die kälteste Stunde abzüglich des Abzugs ΔT_K als Reserve für kältere Jahre (4.5). |
| **Deckel** | Die feste Obergrenze von 48 Stufen (Stunden) je Rampe, unabhängig von Absenkdauer und Stufenformel (4.6). |
| **Eigentümer** | Der Träger eines Kalenders oder einer Vorgabezeile: genau ein Gebäude (mit oder ohne Zone), ein Katalogbau oder eine Vorlage, nie mehrere zugleich (Eigentümerregel, 5.6). |
| **Feiertagsregel** | Eine von neun bundeseinheitlichen Feiertagsregeln (Neujahr bis zweiter Weihnachtstag) aus einer festen Liste, die der Lauf gegen das Referenzjahr auf einen Tag im Gemeinjahr auflöst (3.2, F11). |
| **Grundangabe** | Die schwächste Ebene des Kalenders: ein Wert oder „aus", der gilt, solange keine Standardwoche angelegt ist und keine Periode den Tag enthält (3.2, Ebene 1). |
| **Höchstzeit t_max** | Die längste Aufheizzeit, die die Stufenformel im Bemessungsfall noch unter der Aufheizleistung hält; zugleich Anzeige und tägliche Obergrenze der Aufheizzeit (4.5). |
| **Kalender** | Der `Konditionierungskalender`: eine der fünf Größen einer Zone, aus Grundangabe, Standardwoche und Perioden zu 8 760 Stundenwerten ausgewertet (3.2). |
| **Kalenderkarte** | Die Bedienoberfläche eines Kalenders mit Auswahlliste der Vorlagen, Zeitfenster-Werkzeug, Wochenraster, Periodenliste und Vorschau als Wochenprofil und Teppichbild (7.5). |
| **Kappungsanteil** | Der Anteil einer Stunde, in dem `Heizleistung_Max` die Leistung kappt; ein neuer Ausgang des Rechenschritts, der auch ohne Kappung mitgeführt wird (4.3). |
| **Konditionierung** | Die fünf Größen der Raumkonditionierung — Heiz- und Kühlsollwert, Lüftung, innere Gewinne aus Geräten und Personen —, die je Zone einen stundengenauen Kalender erhalten (1, 3.1). |
| **Nachtauskühlung** | Eine erhöhte Nachtlüftung, die nur wirkt, wenn die Raumluft über einer Schwelle liegt und die Außenluft mindestens ΔT kühler ist (Hysterese 1 K) — bedingt wie die Sommerlüftung, nie in Heiznächten (3.7, P9). |
| **Nachweisband** | Das Zeitfenster um jeden Sprung, in dem eine Stundenleistung über 1,01 · P_auf oder ein Kappungsanteil größer 0 als Hinweis mit Tageszahl gilt (4.3, W3). |
| **Periode** | Ein Zeitraum oder Feiertag mit Rang, Art und einer Angabe (Wert, „aus", eigene Woche oder „wie Wochentag X"), der die Standardwoche für die enthaltenen Tage ganz ersetzt (3.2, Ebene 3). |
| **Rampe** | Die lineare Treppe aus n gleich großen Stufen ΔT/n vor einem Sprung, deren letzte Stufe in die Sprungstunde selbst fällt (4.1). |
| **Reserve ρ** | Der Zuschlag auf die stationäre Last an der kältesten Stunde, der die Zielleistung P_auf ergibt; Startwert 20 %, im Projekt einstellbar (4.4, P5). |
| **Saison (Heizperiode, Kühlperiode)** | Die Zeile der Matrix mit Start- und Enddatum, innerhalb der Heiz- bzw. Kühlsollwert wirkt und außerhalb „aus" steht; leer heißt ganzjährig (3.2, 3.3, E53). |
| **Sollwertsprung** | Der Übergang des fertigen Heizsollwerts von einem endlichen Wert auf einen höheren endlichen Wert in einer Stunde, um mehr als 0,01 K (4.1). |
| **Standardfahrplan** | Der Generator, der aus der Vorgabe-Matrix einen Kalender mit fünf Spalten ableitet — hinter „Kalender anlegen" und, auf den Matrixbereich beschränkt, hinter „Matrix erneut anwenden" (3.3). |
| **Standardwoche** | Die zweite Ebene des Kalenders: 168 Zellen (Wert oder „aus") für eine typische Woche, die an die Stelle der Grundangabe tritt, sobald sie angelegt ist (3.2, Ebene 2). |
| **Stufenformel (Gleichgewichtsform)** | Die geschlossene Formel, die die mittlere Leistung einer n-stufigen Rampe aus dem Gleichgewicht beim alten Sollwert berechnet und so die kleinste haltende Stufenzahl in einem Lauf ohne Zweitlauf liefert (4.3). |
| **Vererbung** | Die Kaskade, mit der eine Zone eine Matrixzelle vom Gebäude übernimmt, solange die Zelle leer ist, oder einen eigenen Wert bzw. Kalender führt (3.4, F2). |
| **Vorgabe-Matrix** | Die Tabelle mit den Spalten Heizen, Kühlen, Lüftung, Geräte/Anlage und Personen und den Zeilen Nennwert, Tag, Nacht, Wochenende, Ferien und Saison, aus der der Standardfahrplan einen Kalender ableitet (3.3). |
| **Vorkühlen** | Die auf den fallenden Kühlsollwert gespiegelte Aufheizrechnung; kommt mit KU3 (4.7, F10). |
| **Vorlage** | Ein vorbefüllter Kalender einer der fünf Größen aus einer Auswahlliste (Wohnen, Büro, Schule, eigene); eine Vorlage gehört genau einer Größe (3.5, P11). |
| **Zone** | Die kleinste Einheit mit eigener Konditionierung; im Einzonenmodell ist das Gebäude die Zone, im Mehrzonenmodell erbt jede Zone die Matrix ihres Gebäudes oder führt eigene Kalender (3.4, Mehrzonenmodell 2.6). |

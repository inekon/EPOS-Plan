# Konzept: Konditionierungsprofile — Kalender, Voreinstellungen und Aufheizoptimierung

> **Rev. 1 — erste Fassung mit den Entscheiden vom 26.09.2026 (E52, Leitkonzept N1.59).** Synthese zweier
> unabhängiger Entwürfe (Rechenweg und Physik; Datenmodell, Bedienung und Übernahme des Bestands) nach einer
> Gegenprüfung mit 25 Faktenstichproben und eigenen Nachrechnungen; Entwürfe, Gegenprüfung und Prüfskripte liegen im
> Arbeitsordner der Sitzung, nicht im Repositorium. Die Empfehlungen der Gegenprüfung tragen die Kapitel 3 bis 8. Die
> acht Fragen hat der Anwender am selben Tag entschieden — P1, P2 und P4 bis P8 nach Empfehlung, **P3 abweichend:
> Katalogbauten der Auslieferung tragen eigene Kalender**; Kapitel 9 hält Entscheide und Festlegungen fest, Kapitel 11
> die Risiken.

**Stand:** 26.09.2026. **Fassung:** Rev. 1 — entschieden (E52); die Umsetzung der Stufen KP1–KP4 folgt auf Auftrag.

**Zweck.** Jede Größe der Raumkonditionierung — Heiz- und Kühlsollwert, Lüftung, innere Gewinne aus Geräten und
Personen — bekommt je Gebäude, Zone und Katalogbau einen stundengenauen Jahreskalender; Voreinstellungen setzen ihn in
einem Schritt; vor einem Sprung des Heizsollwerts nach oben steigt der Sollwert über eine **berechnete** Aufheizzeit
an. Entschieden sind die acht Fragen P1–P8 (E52, Kapitel 9.2); achtzehn Festlegungen F1–F18 stehen nach Empfehlung zur
Kenntnis, Widerspruch ist möglich (9.1).

**Verhältnis zum Leitkonzept.** Teilkonzept des [Leitkonzepts](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
(Grundlagen dort 4.4–4.6, 5.6, 15, N1.32, N1.37, N1.48, N1.55, N1.56). Der Entscheid steht dort als **Nachtrag N1.59
(E52)**, als Zeile in Abschnitt 1 der [Statusdatei](Status_Gebaeudesimulation_VDI6007.md) und im
[Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) Kapitel 10 (entschieden, die Festlegungen als Vermerk);
die Stufen KP0–KP4 führt die Statusdatei in Abschnitt 2. Was das Papier in Kühlkonzept, Anlagenkopplung und
Mehrzonenmodell berührt, nennt 2.3.

**Was dieses Papier nicht tut.** Es übernimmt aus Normen weder Tabellenwerte noch Formeln. VDI 6007 Blatt 1 und 3
behandeln Aufheiz- und Absenkdynamik nicht, VDI 2078 nennt ein Nutzungsprofil nur im Beispiel des Anhangs A, VDI 6020
die Nachtabsenkung nur bei der Auslegungstagberechnung; DIN EN 12831-1, DIN V 18599-2/-10, DIN EN 16798-1,
DIN EN ISO 52016-1 und SIA 2024 liegen lokal nicht vor. Die Aufheizrechnung ist deshalb **aus dem eigenen 2K-Modell
hergeleitet** und an ihm nachgerechnet. Keine Hersteller- und Produktdaten; Nutzungsmuster sind EPOS-Vorgaben mit
runden Werten; die Testdatenbank ist nur lesend befragt.

**Auftrag des Anwenders (26.09.2026), im Wortlaut:**

> 1. Es ist erforderlich, Profile für die Konditionierung (sowohl einzonen als auch Mehrzonenmodell) vorzunehmen. Alle relevanten grössen (Soll-Temperatur Heizung , Soll-Temperatur Kühlung, Lüftung/Luftwechsel, interne Wärmegewinne durch Geräte/Anlage und durch Personen, ..), müssen dazu ein Kalender erhalten der einfach für den benutzer zu bedienen ist (stunden einstellung möglich).
> 2. Es soll voreinstellungen geben, die schnell den Kalender setzen. Zum Beispiel Solltemperatur Heizung: Heizperiode von ... bis, Nachtabsenkung auf ... von ... bis, Wochenendeabsenkung,)
> 3. Bei einem Temperatursprung - zum beispiel bei der Nachtabsekung auf Tagtemperatu von 17 auf 21°C - gibt es einen großen heizwärmebedarf. Dieser soll vermieden werden indem die Heizleestung durch einen sukkzessiven Anstieg der Soll-Temperatur Raum erhöht wird. Es eine Aufheizzeit vor einer Temperaturänderung geben (Raum-Solltemperatur). Diese Zeit soll ermittelt werden durch eine Berechnung der Aufheizzeit vor dem Temperatur-Sprung. Es soll eine maximale aufheizzeit ermittelt werden a) entweder nidrigste Außentemperatur oder b) niedrigste Außentemperatur abzüglich eine vorgegebenen Temperaturabzug (in K)
> 4. Erstelle dazu eine Konzept

**Entscheid des Anwenders (26.09.2026), im Wortlaut:** „P1, P2: Empfehlung / P3: (b) / P4 bis P8: Empfehlung"

---

## 0. Das Ergebnis in zehn Punkten

1. **Fünf Größen, je Gebäude, Zone und Katalogbau ein Kalender:** Heizsollwert, Kühlsollwert samt dem Zeitprofil aus
   K11 (P7), Nutzerlüftung, Geräte und Anlage, Personen (F1). Beleuchtung steckt in „Geräte", der Heizbetrieb ist der
   Wert „aus", auch stundenweise (P2); Geräte und Personen stehen getrennt und energieerhaltend (P1), Lüftung, Geräte
   und Personen sind Anteile eines Nennwerts, 100 % ist der Bestand (F15).
2. **Ein Kalender ergibt genau eine Reihe von 8 760 Stunden** aus Grundangabe, Standardwoche 7 × 24 und Perioden mit
   Datum und Rang — im Gemeinjahr, in der Ortszeit des Laufs, mit dem Wochentag aus dem Referenzjahr wie heute;
   Feiertage sind gespeicherte Regeln, keine Jahrestage (F11).
3. **Nichts wird migriert, alles wird abgeleitet:** Ohne angelegten Kalender bildet der Kern aus den heutigen Feldern
   den Standardfahrplan, bitgleich; „Kalender anlegen" ändert keine Reihe, auch nicht die der Zonen. Kein DML, die
   alten Spalten bleiben bis GA (F5); KP1 und KP2 rechnen gegen die Basis R22 (fünfzehn Projekte) byte-gleich.
4. **Zwei STRICT-Tabellen** mit IDs und CHECK; Eigentümer ist ein Gebäude, eine Zone oder ein Katalogbau (P3), die
   Woche ein 168-Werte-Text nach H8 mit eigenem strengem Leser (F4). Katalogkalender reisen bei der Übernahme ins
   Projekt mit und gehören zur Auslieferung.
5. **Voreinstellungen sind Generatoren im Kern** mit festem Zielbereich, Vorschau und einem Schritt zurück (F3);
   Nutzungsmuster für Büro und Schule gehören dazu (P4).
6. **Die Spitze nach dem Sprung ist ein Leistungsüberschuss, kein Wärmeimpuls:** Er klingt mit den Zeitkonstanten der
   Masse ab; eine Rampe verteilt ihn. Je besser gedämmt, desto größer der relative Überschuss.
7. **Die Aufheizrechnung ist eine geschlossene Stufenformel** aus zwei Zeitkonstanten und zwei Modalkapazitäten der
   Zone — ein Vorab-Fahrplan in einem Lauf, unter 1 ms je Zone (F7); die erste Ordnung ist nur obere Schranke,
   Überlagerung und Vorausrechnung sind Prüforakel, ein Nachweisband meldet, wo die Annahmen nicht tragen.
8. **Bemessen wird an der kältesten Stunde** (a) oder ΔT_K = 2 K darunter (b), nur für die höchste Aufheizzeit (F18);
   die Aufheizleistung ist `Heizleistung_Max`, sonst 1,2 × die stationäre Last der kältesten Stunde (P5, ρ nach der
   KP0-Probe). Täglich wird nur so lange gerampt, wie der Tag verlangt, „fest" ist wählbar (P6); höchstens 48 h und
   nie über die Absenkung hinaus.
9. **Das Zahlenbeispiel zeigt die Wirkungsgrenze:** Das Haus aus Projekt 1045 braucht mit dieser Vorgabe keine Rampe;
   eine gedämmte Variante rampt an der kältesten Stunde 8 h und senkt ihre Spitze um 15 % (4.5).
10. **Stufen KP0–KP4 mit 29–42 PT,** davon 3–4 PT für die Katalogkalender (P3), KP3b optional 3–5 PT; eine neue Basis
    entsteht erst mit KP3 und einem neuen Referenzprojekt. Mit E52 ist vor KP1 keine Frage mehr offen.

## 1. Auftrag, Einordnung, Befund heute

Der Auftrag hat drei Teile: Kalender je Größe für Einzonen- und Mehrzonengebäude (Punkt 1), Voreinstellungen (Punkt 2)
und eine berechnete Aufheizzeit vor Sollwertsprüngen mit einer höchsten Aufheizzeit nach (a) oder (b) (Punkt 3). Er
setzt auf E43 auf (Nachtzeit je Gebäude, Vorgaben 20/18 °C und 5 W/m², Leitkonzept N1.48), ändert mit P7 die Staffel
von K11 (E27), berührt zwei Ausschlüsse der Schwesterpapiere (2.3) und ist über die Zonenkalender mit G6c und G6d
verzahnt (8).

**Befund heute.** Pfade unter `EPOS.Kern/Allgemein/Simulation/Gebaeude/` stehen ohne diesen Vorsatz; Zeilen am Stand
`origin/ios_migration_september` vom 26.09.2026.

| Nr | Befund | Fundstelle |
|---|---|---|
| B1 | Sollwerte, Nachtzeit (volle Stunde) und vier Ferienzeiträume aus Tag und Monat sind **Zahlenfelder** in der Gruppe „Alle Daten"; eine grafische Kalenderdarstellung gibt es nicht | `EPOS.UI/Dialoge/Bedarf/GebaeudeStammblattFelder.razor:183-260` |
| B2 | Der Sollwertfahrplan ist eine **Stufenfunktion**: Ferien vor Wochenende vor Tag/Nacht. Das Wochenende gilt ganztags und nur mit Wert > 5 °C, der Merker `Wochenende` wird nicht gelesen; Ferien nur mit `Ferien` > 0,9 und Wert ≥ 1 °C; 0 und 366 heißen „aus", Beginn > Ende heißt Jahreswechsel, ein anderer Tag außerhalb 1…365 ist ein benannter Fehler | `GebaeudeModellEingang.cs:1842-1892`; `GebaeudeFestwerte.cs:128-134` |
| B3 | Der Wochentag kommt aus der Wochenendmaske des **Ortszeit-Kalenders** (U7); Referenzjahr ist das Jahr der Spotpreisreihe, sonst 2025 — ein Wechsel der Preisreihe verschiebt alle Wochenmuster. Die Ortszeit ist MEZ/MESZ mit eigener Regel für die Umstellstunden | `EPOS.Kern/Allgemein/Simulation/Klimakalender.cs:83-113`; `EPOS.Kern/Controller/SolardatenCtrl.cs:147-153, 241`; `EPOS.Kern/Allgemein/DbWerte.cs:2681`; `GebaeudeModellEingang.cs:1606-1617` |
| B4 | Einziger Zeitprogramm-Baustein ist das **Wochenraster** (7 × 24, streng: eine leere Zelle ist ein Fehler), genutzt für `Sollwertprofil` (AK1) und den Erzeugerfahrplan (AK2). `Sollwertprofil`: 168 Werte mit `;`, höchstens 1 400 Zeichen; der Schreiber setzt zwei Nachkommastellen, der Leser nimmt jede Stellenzahl, aber nur Zahlen. Das Profil gilt nur mit wirksamer Kopplung, Ferien wirken darüber | `EPOS.UI/Bausteine/Wochenraster.razor:1-19`; `EPOS.Kern/Allgemein/Update/AnlagenkopplungSchema.cs:233-242, 291-336`; `GebaeudeModellEingang.cs:920, 1559-1599` |
| B5 | Innere Gewinne sind **ein konstanter Wattwert**, je zur Hälfte konvektiv und radiativ, nach Leitkonzept 3.3 „Leistung des ganzen Katalogbaus, zeitlich konstant". Er entspricht bei rund 150 von 275 Katalogsätzen und 24 von 29 Projektzeilen etwa 70 W je Person (Median 2,3 W/m²). Personen sind keine eigene Größe | `GebaeudeModellEingang.cs:586, 810-811` |
| B6 | Der Luftwechsel ist eine Konstante (Infiltration + Nutzer, Vorgaben 0,3 und 0,4 1/h). Einzige Zeitabhängigkeit ist die Sommerlüftung: 2,0 1/h als Zusatzleitwert ab 23 °C, mit wirksamer Kühlung ab θ_kühl − 3 K | `GebaeudeFestwerte.cs:139-142`; `Sommerlueftungsregel.cs:7, 24`; `Vdi6007Rechenweg.cs:270-273` |
| B7 | Der Kühlsollwert ist konstant. `Kuehl_Sollwert_Nacht` steht in `Tab_Gebaeude(_STAMM)` und `Tab_Zone`, wird durchgereicht und nicht gelesen (K11: Zeitprofil in KU3). Geprüft wird θ_kühl ≥ höchster Heizsollwert des Jahres + 1 K | `GebaeudeModellEingang.cs:930, 1641`; `EPOS.Kern/Allgemein/Zonenvorgaben.cs:55`; `EPOS.UI/Dialoge/Bedarf/GebaeudeKatalogDaten.cs:270-275` |
| B8 | `Heizleistung_Max` ist die einzige Leistungsgrenze (leer = unbegrenzt, Zone nach Flächenanteil); der Löser kappt. Gezählt werden die gekappten Stunden **nur mit wirksamer Kopplung** | `GebaeudeModellEingang.cs:170, 407-408`; `Vdi6007Rechenweg.cs:337-342`; `HeizkreisErgebnis.cs:208-209` |
| B9 | Ohne Grenze liegt die Jahresspitze in elf von zwölf Projekten auf der ersten Stunde nach der Nachtabsenkung am kältesten Tag — der ideale Heizer deckt den Sprung in einer Stunde | Leitkonzept 5.6 (Prototyp) |
| B10 | Die Auslegungsaußentemperatur (H10) ist das abgerundete **kälteste Tagesmittel**; die kälteste Stunde ist nirgends geführt. In 12 der 14 Referenzprojekte mit Gebäude liegt sie bei −18,2 °C gegen ein Tagesmittel von −10,95 °C (H10 −11 °C), in 1018 und 1049 bei −9,3 gegen −6,6 °C. Die stationäre Last ist ein Aufruf; die Auslegungsraumtemperatur fällt auf `SollTag` zurück | `GebaeudeModellEingang.cs:1237, 1268-1287, 1512-1528`; `Zonenmodell2K.cs:601` |
| B11 | Der Löser kann **je Stunde**: Heizsollwert NaN = keine Heizung, obere Grenze +∞ = keine Kühlung, Zusatzleitwert ≥ 0. Der Eingang füllt Grenzen und Strahlungsanteil als Skalare; der Vorlauf startet mit dem Sollwert der ersten Vorlaufstunde (NaN bräche ihn); der Zwischenspeicher des freien Falls hält einen einzigen Leitwert | `Stundenrand.cs:36-117`; `Zonenmodell2K.cs:959-966, 1037`; `Vdi6007Rechenweg.cs:283` |
| B12 | Mehrzonen: **Nachtzeit, Ferien und Kühlwerte kommen vom Gebäude** (N1.56 Festlegung 1, E49 A4 (a)); die Nutzungszeit der Kennzahlen ist die der ersten beheizten Zone; bis zu 50 Zonen je Gebäude | `GebaeudeModellEingang.cs:391-392`; `Zonenrechnung.cs:212-215`; `GebaeudeModellErgebnis.cs:116, 147`; `EPOS.Kern/Allgemein/GebaeudeZonenregeln.cs:20` |
| B13 | Tag/Monat → Jahrestag rechnet im **laufenden** Kalenderjahr: In einem Schaltjahr (2028) landet jedes Datum ab dem 1. März einen Tag später | `EPOS.Kern/Allgemein/Ferienzeit.cs:44-46` |
| B14 | Weitere Leser der Felder: der Altweg (vier Sollwerte, keine Nachtzeit, kein Profil), das Zapfprofil (Ferien-Vorbelegung), der gbXML-Export (`SollHeizenC` aus dem Tagwert). Der einzige Weg Katalog → Projekt ist `CopyFromStamm`; er setzt den Katalogverweis `ID_Gebaeude_Stamm` der Projektkopie | `EPOS.Kern/Allgemein/Simulation/Altweg/TagesbilanzRechenweg.cs:279-360`; `EPOS.Kern/Controller/ZapfprofilCtrl.Eingang.cs:450-470`; `EPOS.Kern/Allgemein/Export/Gebaeude/GebaeudeExportAblauf.cs:375`; `EPOS.Kern/Controller/GebaeudeStammCtrl.cs:606-615` |
| B15 | Ausgeschlossen sind die vorausschauende Aufheizung („Optimierung der Einschaltzeit") und Nutzungsprofile für Nichtwohngebäude (SIA 2024, DIN V 18599-10) | Anlagenkopplung 1.3, 4.3, 4.4; Leitkonzept 15 |
| B16 | Testdatenbank (Basis R22): 17 Gebäude in 14 Referenzprojekten (1030 rechnet ohne Gebäude), **0 Zonen** in der ganzen Datenbank; alle Referenzgebäude 20/18 °C, Nachtzeit leer (22–6 Uhr), ohne Wochenend- und Ferienfahrplan, `Sollwertprofil`, `Heizleistung_Max` und Kühl-Nachtwert. Im Katalog tragen 49 Sätze aktive Ferien und 51 einen Wochenendwert über 5 °C. Schemastand 150 | `EPOS.Kern/Allgemein/Update/SchemaStand.cs:682`; Abfrage lesend |

**Folgerung.** Wochentagslogik, strenger Leser, Rasterbaustein und stündliche Randbedingungen liegen bereit. Es fehlen
Datumsbereiche, Stundenreihen für Lüftung und Gewinne, der Zonen- und der Katalogkalender, die Größe „kälteste Stunde"
und jede Vorwegnahme des Sprungs. Kein Referenzprojekt nutzt eine Sonderregel aus B2 — die Bitgleichheit braucht
Proben über den ganzen Katalog (3.3).

## 2. Anforderungen und Abgrenzung

### 2.1 Anforderungen

| Nr | Anforderung (Auftragspunkt, Entscheid) | Nachweis |
|---|---|---|
| KA1 | Jede der fünf Größen hat je Gebäude und je Zone einen Kalender, stundengenau änderbar (1) | Kernprobe, bunit |
| KA2 | Bedienbar ohne Zahlenkolonnen: Zeitfenster „Tage, von, bis, Wert", Perioden mit Datum, Vorschau als Bild (1) | Sichtabnahme Windows, bunit |
| KA3 | Voreinstellungen setzen den Kalender aus wenigen Parametern und zeigen die Wirkung vor dem Übernehmen (2) | Kernprobe je Voreinstellung |
| KA4 | Vor jedem Sprung des Heizsollwerts nach oben steigt der Sollwert stufenweise; der Zielwert steht zu Beginn der Nutzung. Groß wird nach dem Sprung die **Leistung**, nicht die Wärme: Eine Rampe senkt die Spitze und hebt die Jahresheizwärme leicht (3) | N-AH10 |
| KA5 | Die Aufheizzeit wird je Sprung aus dem Modell der Zone **berechnet**; die höchste gilt bei (a) der niedrigsten Außentemperatur oder (b) dieser abzüglich ΔT_K (3) | N-AH1 bis N-AH5 |
| KA6 | Aufheizzeiten, ihre Grundlage und die Grenzfälle stehen in Ergebnis, Bericht und Export | Datenbankfall, Bericht |
| KA7 | Katalogbauten der Auslieferung tragen eigene Kalender; die Übernahme ins Projekt nimmt sie mit, die Auslieferungsvorlage liefert sie aus (P3) | Datenbankfälle, Vorlagenlauf |
| KN1 | **Byte-Gleichheit:** Ohne angelegten Kalender und mit ausgeschalteter Aufheizoptimierung rechnet jedes Projekt byte-gleich; „Kalender anlegen" ändert keine Reihe | Referenzlauf gegen R22, Wache 3.3 |
| KN2 | **Determinismus:** nur Daten, Klimareihe und Referenzjahr — keine Uhr, kein Zufall, `InvariantCulture` | zwei Läufe, de-DE gegen en-US |
| KN3 | **Rechenzeit:** O(8 760) je Zone und Größe, die Aufheizrechnung unter 1 ms je Zone, **kein Zweitlauf**; E36 bleibt unberührt, eine neue Rechenzeitgrenze ist nicht nötig | Messung im Abnahmelauf |
| KN4 | **Plattform und Daten:** Fachlogik im Kern, keine Datenbank in der Oberfläche, iOS ohne eigenen Code, Berührungsziele ≥ 44 px; STRICT, IDs, Schalter 0/1 mit CHECK, NULL als Vorgabe, kein DML | Wächter, Schemaprobe, `SqlDialektPruefer` |
| KN5 | **Benannt statt still:** ungültige Eingaben sind benannte Fehler, Grenzfälle der Aufheizrechnung benannte Hinweise mit Zähler | Test je Grund |

### 2.2 Abgrenzung

- **K11 und KU3.** Das Zeitprofil der Kühlung (Nachtwert) kommt in den Kühlkalender (P7). KU3 behält Kältemaschine,
  freie Kühlung, Kältespeicher, Export und **Kühlung je Zone**; einen Kühlkalender der Zone gibt es erst mit KU3.
- **Altweg.** Projekt 1040 und jedes Gebäude auf dem Tagesbilanz-Weg lesen weder Kalender noch Rampe, bis GA
  (ADR-006); der Hinweis der Karte dazu ist ein Altweg-Sonderfall und kommt mit KP2 in die Löschliste von GA.
- **Anlagenkopplung.** Der Erzeugerfahrplan (AK2) ist Verfügbarkeit der Anlage — derselbe Rasterbaustein, eine andere
  Größe. `Sollwertprofil` bleibt Bestandsweg von AK1, ein angelegter Heizkalender hat Vorrang; AK1-Gebäude bekommen
  keine Rampe (F13).
- **Lastgänge, Zapfprofil, Tarife** bleiben eigene Zeitreihen; berührt wird allein die Ferien-Vorbelegung des
  Zapfprofils (5.5). **Normprofile:** keine Tabellen aus DIN V 18599-10 oder SIA 2024; EPOS-Nutzungsmuster für
  Nichtwohnbauten sind Voreinstellungen (P4).
- **Import-Zeitpläne:** gbXML `Schedule`, `Occupants` und die IFC-Zeitreihen bleiben ungelesen (Datenaustausch 13).
  **Feuchte:** Personen zählen sensibel; latente Lasten und Entfeuchtung bleiben ausgeschlossen (K5).
- **Regelungstechnik:** Die Rampe ist ein Vorab-Fahrplan auf deterministischen Klimadaten, der Regler bleibt, was er
  ist. Keine Kalendergrößen sind `Maximaleraumtemperatur`, Leistungsgrenzen, Infiltration und Sonnenschutz.

### 2.3 Berührte Festlegungen der Schwesterpapiere

Anlagenkopplung, Kühlkonzept und Mehrzonenmodell werden mit diesem Papier **nicht** geändert; ihren Nachzug auf E52
übernimmt KP0. Leitkonzept 15 ist mit N1.59 nachgezogen.

| Stelle | Heute | Mit E52 |
|---|---|---|
| Anlagenkopplung 4.4 | vorausschauende Aufheizung („Optimierung der Einschaltzeit") ausgeschlossen | Auftragspunkt 3 verlangt sie als Vorab-Fahrplan der idealen Regelung; der Ausschluss gilt weiter für den Regler und für AK1-Gebäude bis KP3b |
| Anlagenkopplung 1.3, 4.3; Leitkonzept 15 | Nutzungsprofile für Nichtwohngebäude ausgeschlossen | auf Normprofile verengt (P4); Leitkonzept 15 nachgezogen |
| Kühlkonzept 7.1, 11; Register K11 (E27) | Zeitprofil der Kühlung in KU3 | im Kühlkalender mit KP1 (P7), KU3 ohne „Kühlsollwert Nacht"; K11 trägt im Register den Vermerk |
| Leitkonzept N1.56 Festlegung 1 | die Nachtzeit kommt vom Gebäude | eine Zone mit eigenem Heizkalender hat eigene Nacht, Ferien und Feiertage (F2) |
| Leitkonzept N1.55, E49 A4 (a) | Kühlwerte vom Gebäude | bleibt bis KU3 |
| Anlagenkopplung 4.3 (H8) | 168 Werte als Text für **einen** Wochenvektor je Gebäude | gilt für die Woche jedes Kalenders; Perioden stehen in einer Tabelle (F4) |

## 3. Fachliches Modell

### 3.1 Größen und Semantik

| Größe | Werteart | heutige Spalten | ohne Kalender | Eigentümer |
|---|---|---|---|---|
| **Heizsollwert** θ_H | 0…30 °C oder „aus", auch je Stunde | vier Sollwerte, `Nachtabsenkung_Beginn/_Ende`, `Ferien`, `Ferienbeginn/-ende_1…4`; AK1 `Sollwertprofil` | Standardfahrplan (3.3) | Gebäude, Zone, Katalogbau |
| **Kühlsollwert** θ_K | °C in den heutigen Grenzen oder „aus" | `Kuehl_Sollwert`; `Kuehl_Sollwert_Nacht` füllt nur V11 vor (P7) | Konstante wie heute; nur mit Kühlbetrieb und `Kuehlung_Aktiv` (E32) | Gebäude, Katalogbau; Zone ab KU3 |
| **Nutzerlüftung** n_N | Anteil 0…100 % von `Luftwechsel_Nutzer` | `Luftwechsel_Nutzer`; Infiltration bleibt konstant | 100 % | Gebäude, Zone, Katalogbau |
| **Geräte und Anlage** Q_G | Anteil 0…100 % eines Nennwerts [W] | `Interne_Waermegewinne` | 100 % | Gebäude, Zone, Katalogbau |
| **Personen** Q_P | Anwesenheit 0…100 % × Nennwert [W] | neu; `Bewohner` schlägt die Personenzahl vor | kein Kalender = 0 W zusätzlich | Gebäude, Zone, Katalogbau |

**Lüftung (F15):** Der Kalender wirkt allein auf den Nutzeranteil; die Anzeige zeigt den Luftwechsel absolut in 1/h;
stammt er aus der Gesamtangabe `Luftwechselrate`, verlangt ein Lüftungskalender die getrennte Angabe (Vorschlag: die
heutigen Vorgaben 0,3 und 0,4 1/h). **Gewinne (P1):** `Interne_Waermegewinne` ist ein Dauerwert, der meist die
Personen schon enthält (B5). Entschieden ist die Trennung, energieerhaltend: Wer einen Personenkalender anlegt, bekommt
als Geräte-Nennwert `Interne_Waermegewinne` minus das Jahresmittel der Personenwärme; die Karte zeigt die Rechnung und
beide Jahresmittel. Ein Nennwert ist der Wert bei 100 %; jede spätere Formung ändert das Jahresmittel sichtbar.
**Personen:** Nennwert = Personenzahl × **70 W** (EPOS-Vorgabe, passt zum Katalog), sensibel, je zur Hälfte konvektiv
und radiativ; die Personenzahl kommt aus `Bewohner` (Gebäude und Zone), sonst aus Nutzfläche ÷ `Flaeche_Nutzer`.
**„aus"** heißt beim Heizen NaN, beim Kühlen +∞, bei Anteilen 0.

### 3.2 Kalendermodell

**Raster und Zeitbasis.** Ein Kalender liefert 8 760 Werte: Tag d = 0…364, Stunde s = 0…23, h = 24·d + s, Gemeinjahr
mit 365 Tagen; die Woche hat 168 Stunden von Montag 00:00 bis Sonntag 23:00. Die Kalenderstunde ist die Uhrstunde der
Ortszeit, in der der Lauf rechnet (MEZ/MESZ, `EPOS.Kern/Allgemein/Simulation/SolarZeitbasis.cs`); an den zwei
Umstelltagen gilt deren Regel, der Kalender führt keine eigene. **Wochentag:** w(d) = (w₀ + d) mod 7, w₀ aus
`WochentagDesErstenTags` der Wochenendmaske des Ortszeit-Kalenders wie heute (U7). Der Kalender speichert kein Jahr:
Wechselt das Referenzjahr, wandern die Wochentage, die Daten nicht; Datum ↔ Jahrestag rechnet der Dialog im Gemeinjahr,
nie im laufenden Jahr (B13).

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
benennt, gerechnet wird mit ihr nicht. Der heutige Vorrang bleibt darstellbar: Tag/Nacht sind die Werktagszeilen der
Standardwoche, das Wochenende ihre Sa/So-Zeilen, die Ferien Perioden darüber.

**Feiertage als Regel (F11).** Die neun bundeseinheitlichen Feiertage (Neujahr, Karfreitag, Ostermontag, 1. Mai,
Christi Himmelfahrt, Pfingstmontag, 3. Oktober, 1. und 2. Weihnachtstag) stehen als Regelkennung in der Periode, nicht
als Jahrestag. Der Lauf löst sie gegen das Referenzjahr auf — das Osterdatum als Rechenvorschrift — und bildet Tag und
Monat im Gemeinjahr ab; Länderfeiertage sind gewöhnliche Perioden. **Ring:** Für Rampe und Vorlauf ist das Jahr ein
Ring; ein Sprung am 1. Januar greift in die Dezemberstunden wie der Vorlauf (Leitkonzept 4.6).

### 3.3 Standardfahrplan — abgeleitet bis angelegt, bitgleich

Jede Größe ist **abgeleitet** (keine Zeile; der Kern bildet den Standardfahrplan aus den aktuellen Feldern) oder
**angelegt** (Zeilen; die Felder ruhen für diese Größe, die Karte sagt es). „Kalender anlegen" schreibt den
Standardfahrplan mit **demselben Generator**, den der Lauf im abgeleiteten Fall nutzt; „Verwerfen" löscht die Zeilen.
Für Katalogbauten gilt dasselbe mit den Feldern ihrer Katalogzeile.

| Heutiges Feld | Im Standardfahrplan | Regel wie heute |
|---|---|---|
| `Raumsolltemperatur_Tag`, `_Nachtabsenkung`, `Nachtabsenkung_Beginn/_Ende` | Standardwoche: Nutzungszeit Tagwert, Nachtzeit Nachtwert | `Nachtzeit` mit der strengen Prüfung des Laufs (`NachtzeitUngueltig`), nicht mit dem Rückfall von `Bestandswoche` (`EPOS.Kern/Allgemein/Gebaeuderechenweg.cs:369-384`) |
| `Raumsolltemperatur_Wochenende` | Sa- und So-Zeilen ganztags, nur mit Wert > 5 °C; Merker `Wochenende` unbeachtet | `GebaeudeModellEingang.cs:1882-1887` |
| `Ferien`, `Raumsolltemperatur_Ferien`, `Ferienbeginn/-ende_1…4` | nur mit `Ferien` > 0,9 und Wert ≥ 1 °C: je Zeitraum k ohne 0/366 eine Periode Art FERIEN, Rang 200 + k; ein Tag außerhalb 1…365 ist derselbe benannte Fehler | `:1842-1872` |
| `Sollwertprofil` (nur mit wirksamer Kopplung) | ersetzt die Standardwoche; die Ferienperioden bleiben darüber | `:1559-1599` |
| `Kuehl_Sollwert` | Grundangabe; `Kuehl_Sollwert_Nacht` bleibt ungelesen und füllt nur V11 vor (P7) | `:930` |
| Lüftung, Geräte; Personen | Grundangabe 100 %; Personen ohne Kalender, 0 W zusätzlich | — |

**Bitgleich**, weil nur Werte kopiert werden und der Wochentag aus derselben Maske kommt. Die Invariante „Anlegen
ändert keine Reihe" halten drei Regeln: (1) **Rundlauf** — der Schreiber setzt bis zu vier Nachkommastellen, „Anlegen"
prüft je Wert Wert → Text → Wert bitgleich und lehnt sonst benannt ab (in der Testdatenbank kein Fall); (2)
**Zonenwerte bleiben** (F2) — legt ein Gebäude seinen Heizkalender an, während Zonen eigene Sollwertfelder tragen,
legt derselbe Schritt deren abgeleitete Kalender mit an; (3) **Energie bleibt** (P1). **Wache:** Der Generator wird
gegen `Sollwertfahrplan` und `SollwertfahrplanMitProfil` gehalten — über **alle Gebäudezeilen der Testdatenbank**
(heute 304: 275 Katalog, 29 Projekt, darunter die 49 Sätze mit aktiven Ferien und die 51 mit Wochenendwert), über die
Grenzfälle (Nachtzeit über Mitternacht, Ferien über den Jahreswechsel, 0 und 366, Ferienmerker um 0,9,
Feriensollwert unter 1 °C, Wochenende um 5 °C, Wochenprofil mit Ferien, Fehlerfälle mit demselben Fehlergrund) und
über **alle sieben Wochentage des 1. Januar**.

### 3.4 Vererbung Gebäude → Zone, Katalog → Projekt

Je Größe gilt die erste Quelle: (1) Kalender der Zone angelegt; (2) Kalender des Gebäudes angelegt; (3) abgeleitet —
mit den wirksamen Werten der Zone aus der Vorgabenkaskade (`EPOS.Kern/Allgemein/Zonenvorgaben.cs`) und Nachtzeit und
Ferien des Gebäudes, also wie heute. Die Zone erbt **den ganzen Kalender** oder führt einen eigenen (F2); „vom
Gebäude übernehmen und anpassen" legt eine Kopie an; Vererbung einzelner Perioden gibt es nicht. Anteilskalender
multiplizieren den Nennwert der Zone (eigener Wert oder Flächenanteil). Eine Zone mit eigenem Heizkalender hat eigene
Nacht, Ferien und Feiertage (N1.56 Festlegung 1 gilt für sie nicht); einen Kühlkalender der Zone gibt es erst mit KU3
(bis dahin E49 A4 (a)); unbeheizte Zonen haben weder Heiz- noch Kühlkalender (N1.56 Festlegung 2), wohl aber Lüftung,
Geräte und Personen. **Nutzungszeit (F16):** Die Kennzahlen der Nutzungszeit (mittlere Raumtemperatur, Überhitzungs-
und Komfortstunden) folgen dem Personenkalender (Anwesenheit > 0), wenn einer gilt, sonst der Nachtzeit wie heute;
Rampenstunden zählen nicht. Für das Gebäude zählt eine Stunde, in der eine beheizte Zone in Nutzung ist (Muster N1.56
Nr. 10).

**Katalogbauten (P3).** Ein Katalogbau (`Tab_Gebaeude_STAMM`) führt Kalender nach denselben Regeln, ohne Zonen. Die
Übernahme ins Projekt **kopiert** seine angelegten Kalender samt Perioden an das Projektgebäude; danach sind Katalog-
und Projektkalender unabhängig — der Lauf liest nie den Katalog, und eine spätere Änderung im Katalog erreicht ein
Projekt nur über eine erneute Übernahme. Ein abgeleiteter Katalogkalender kommt abgeleitet an. Der ausgelieferte
Katalogbau trägt das Schloss (`ReadOnly`); es sperrt auch seine Kalender.

### 3.5 Voreinstellungen mit Zielbereich

Eine Voreinstellung ist eine Funktion *Parameter → Regeln* mit festem **Zielbereich**: Sie ersetzt genau diesen
Bereich und lässt alles andere stehen (F3); gespeichert wird allein ein lesbarer Vermerk in `Bemerkung`
(„Nachtabsenkung 18 °C, 22–6 Uhr"), den keine Rechnung liest. Die Parameter sind EPOS-Vorgaben mit runden Werten,
vorbelegt mit den Werten des Gebäudes; Generatoren stehen als Code im Kern, Namen in Ressourcen. Im Katalog wirken sie
wie im Projekt.

| Nr | Voreinstellung | Größen | Parameter (EPOS-Vorgabe) | Zielbereich |
|---|---|---|---|---|
| V1 | Standardfahrplan übernehmen | Heizen, Kühlen | die heutigen Felder | ganzer Kalender (= „Anlegen") |
| V2 | Nachtabsenkung | Heizen | 18 °C, 22 bis 6 Uhr, Mo–So | Zellen der Standardwoche in diesen Stunden und Tagen |
| V3 | Wochenendabsenkung | Heizen | 18 °C, Sa–So ganztags oder Fr ab / Mo bis Uhrzeit | Zellen Sa–So samt Randstunden |
| V4 | Heizperiode | Heizen | 1.10. bis 30.4.; außerhalb „aus" oder ein Wert | Periode Art BETRIEBSPAUSE, Rang ab 900 (schlägt alles) |
| V5 | Ferien, Betriebsferien | alle | bis vier Zeiträume; Wert, „aus" oder „wie Sonntag" | Perioden Art FERIEN |
| V6 | Feiertage | alle | die neun bundeseinheitlichen Feiertage als Regel, „wie Sonntag" | Perioden Art FEIERTAG |
| V7 | Nutzungsmuster Wohnen | Heizen, Personen, Geräte | 20/18 °C, Nacht 22–6 Uhr; Anwesenheit Mo–Fr 17–7 Uhr 100 %, 7–17 Uhr 50 %, Sa–So 100 %; Geräte 100 % | Standardwochen |
| V8 | Nutzungsmuster Büro (P4) | Heizen, Personen, Geräte, Lüftung | Mo–Fr 7–18 Uhr 20 °C, sonst 16 °C; Anwesenheit Mo–Fr 8–17 Uhr 100 %, sonst 0 %; Geräte und Lüftung Mo–Fr 7–18 Uhr 100 %, sonst 10 % | Standardwochen |
| V9 | Nutzungsmuster Schule (P4) | wie V8 | Mo–Fr 7–15 Uhr 20 °C, sonst 16 °C; Anwesenheit Mo–Fr 8–14 Uhr 100 % | Standardwochen |
| V10 | Kühlperiode (P7) | Kühlen | 1.5. bis 30.9.; außerhalb „aus" | Periode Art BETRIEBSPAUSE |
| V11 | Nachtanhebung Kühlung (P7) | Kühlen | `Kuehl_Sollwert_Nacht`, sonst 28 °C; 22 bis 6 Uhr | Zellen der Standardwoche |
| V12 | Betriebszeiten | Lüftung und Geräte in einem Schritt | Mo–Fr 7–18 Uhr 100 %, sonst 20 % | Standardwochen |
| V13 | Zeitstruktur übernehmen | Kühlen, Lüftung, Geräte | Quelle „wie Heizung" oder „wie Anwesenheit"; oberer Zustand → Wert A, unterer → Wert B, Schwelle = Mitte aus kleinstem und größtem Wert der Quellwoche | Standardwoche |
| V14 | Von Gebäude, Zone oder Katalogbau übernehmen | alle | Quellkalender | ganzer Kalender |

### 3.6 Prüfregeln und Meldungen

- **Streng (H-F10):** genau 168 Zellen je Woche, jede eine Zahl in den Grenzen der Größe oder „aus" (P2); höchstens
  **64 Perioden** je Kalender (EPOS-Wert); Rang eindeutig; Tage 1…365; Feiertagsregel aus der festen Liste.
- **Kühl- über Heizsollwert je Stunde (F17):** θ_K(h) ≥ θ_H(h) + 1 K, wo beide wirken — bei konstantem Kühlsollwert
  genau die heutige Prüfung (`GebaeudeModellEingang.cs:1641`). Die Rampe wird **vor** der Prüfung an θ_K(h) − 1 K
  gekappt, benannt und gezählt; eine Optimierung bricht nie einen Lauf ab.
- **Sommerlüftung je Stunde:** Schwelle θ_K(h) − 3 K; ist die Kühlung „aus" (+∞), gilt die feste Schwelle 23 °C; der
  Zusatzleitwert ist (2,0 1/h − n(h))⁺ · V · ρc. **Vorlaufstart bei „aus":** Startwert der unbeheizten Zone (Mittel
  von θ_eq über den Vorlauf, N1.56 Festlegung 7), benannt.
- **Auslegungswerte:** Die Auslegungsraumtemperatur der Übergabe fällt auf den höchsten Heizsollwert der Nutzungszeit
  zurück statt auf `SollTag` (`:1237`), die der Kälte auf den niedrigsten wirksamen Kühlsollwert; die
  Auslegungsheizlast nimmt den höchsten Luftwechsel der Nutzungszeit, die Aufheizrechnung den der Sprungstunde.
- **Meldungen:** `GebaeudeModellFehler.KalenderUngueltig` mit Größe, Periode und Stelle; `SIMENG_KOND_*` im Kern,
  `KOND_MSG_*` im Dialog.

## 4. Aufheizoptimierung

### 4.1 Sprung und Rampe als lineare Treppe

Die Optimierung formt die **fertige** Heizsollwertreihe einer Zone. Ein **Sprung** liegt in der Stunde h_s, wenn
θ_N = s(h_s − 1) und θ_T = s(h_s) endlich sind und ΔT = θ_T − θ_N > 0,01 K; die **Absenkdauer** D ist die Zahl der
zusammenhängenden Stunden vor h_s mit endlichem Sollwert unter θ_T. Die Rampe mit n Stufen belegt h_s − n + 1 … h_s:

```
s'(h_s − n + j) = max( s(h_s − n + j),  θ_N + ΔT · j/n )      j = 1 … n
t_auf = (n − 1) h ≤ D            n = 1 ist der heutige Sprung — keine Rampe
```

Die **letzte Stufe fällt in die Sprungstunde**: Der Zielwert steht zu Beginn der Nutzung, und n = 1 ändert nichts
(F6). Das `max` senkt nie einen Wert; mehrere Anstiege (17 → 19 → 21 °C) sind je ein Sprung. Linear, weil der Auftrag
einen „sukzessiven Anstieg" verlangt und die Treppe als „+ x K je Stunde" lesbar und geschlossen bemessbar ist.

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
35,22 kW, auch mit Strahlungsanteil 0,3). Die Absenkform — vorher eingeschwungen bei θ_T, D Stunden auf θ_N — liegt
nie darüber und dient als Probe (N-AH2). **Erste Ordnung als Schranke:** Wegen 1 − e^(−x) ≤ 1 hält
n_F = ⌈C_w·ΔT / (h·(P_auf − Φ_stat))⌉ die Grenze immer — Aufheizzeit = wirksame Kapazität × Hub ÷ Leistungsreserve.
Sie steht in Herleitungszeile und Probenband; bemessen wird mit ihr nicht, denn mit der quasistationären Kapazität
rampt sie am Haus aus 1045 10 h statt 4 h und an milden Tagen 2–3 h ohne Bedarf.

**Ablauf (F7):**

```
je Zone einmal:  H_s, τ_k, C_k des geregelten Falls zum Strahlungsanteil (Eigenwerte und Residuen)
je Sprung h_s:   T_a = kleinste Außenlufttemperatur in [h_s − n_max, h_s]
                 Φ_stat = StationaereHeizlastW(θ_T, T_a, θ_eq = T_a, Erdreich des Tages, Nachbarn nach 4.7)
                 n = kleinstes n ≥ 1 mit Φ̄_n ≤ P_auf,   n ≤ min(n_max, D + 1)
```

Sonne und innere Gewinne bleiben in der Bemessung außen vor; sie verkürzen nur, die Formel irrt zur sicheren Seite.
Die Rechnung ist deterministisch, kostet unter 1 ms je Zone und läuft vor dem Lauf im Eingangsbauer; die Zonenschleife
läuft einmal, **ein Zweitlauf entfällt**. Die **Überlagerung** auf einem Lauf ohne Rampe scheidet aus: Sie ist exakt
nur über durchgehend geregelte Stundenfolgen, bei gesetzter `Heizleistung_Max` rechnet der gekappte Vergleichslauf das
freie System, die Vorhersage wird zu niedrig und n = 1 hielte das Kriterium trivial; dazu kämen zwei bis vier Läufe,
in Mehrzonengebäuden 1–4 s je Gebäude. Überlagerung und Vorausrechnung auf einer Kopie bleiben **Prüforakel**.

**Nachweisband im Lauf (W3).** Im Fenster [h_s − n + 1, h_s + 2] gilt „Stundenleistung > 1,01 · P_auf **oder**
Kappungsanteil > 0" als Hinweis mit Tageszahl. Dafür liefert `Schritt` den Kappungsanteil von `Heizleistung_Max` auch
im idealen Fall — ein neuer Ausgang, keine geänderte Zahl.

### 4.4 Die Aufheizleistung P_auf

P_auf ist die Leistung, die das Aufheizen höchstens beanspruchen darf — **eine feste Zahl je Gebäude bzw. Zone**,
nicht je Tag. Die erste zutreffende Quelle gilt (P5): (1) **`Heizleistung_Max`**, wenn gesetzt (Zone: eigener Wert
oder Flächenanteil; beim Skalieren nach E8 dieselbe Regel) — die Rampe verhindert dann die Kappung nach dem Sprung;
(2) die **Zielleistung** P_auf = (1 + ρ) · Φ_stat(θ_T,max, T_a,min) mit dem höchsten Heizsollwert der Nutzungszeit und
der **kältesten Stunde**, **ρ = 20 %** als Startwert, im Projekt einstellbar (0–100 %).

Der Anker an der kältesten Stunde macht (a) immer erreichbar, (b) verlängert die Zeit wie gewollt, und das Tagesmittel
mischt sich nicht in eine Stundenbemessung. Der verworfene Anker H10 hätte P_auf im Referenzklima **unter** die Last der
kältesten Stunde gelegt — bei temperaturproportionaler Last um rund 2 % in (a) und 7 % in (b), weil die kälteste
Stunde 22–23 % mehr Last trägt als das H10-Tagesmittel. Eine Spalte `Aufheizleistung` braucht es nicht, die
Nennleistung der Übergabe entfällt als Quelle, solange AK1-Gebäude ausgenommen sind (F13), und eine je Tag
mitwandernde Grenze widerspräche dem Wortlaut „maximale Aufheizzeit bei niedrigster Außentemperatur". **KP0-Probe
(R2):** Vor KP1 wird ρ an den 17 Referenzgebäuden geprüft — Überhöhung der Sprungspitze an der kältesten Stunde,
t_auf,max in (a) und (b), Tabelle im Protokoll; 4.5 zeigt, warum sie nötig ist.

### 4.5 Bemessung (a) und (b)

| Variante | T_a,B | Rolle |
|---|---|---|
| **(a) kälteste Stunde** *(Vorgabe)* | Minimum der Außenluft über die 8 760 Stunden der Zeitbasis des Laufs | Auftrag a) |
| (b) kälteste Stunde − ΔT_K | ΔT_K **EPOS-Vorgabe 2 K**, 0–10 K | Auftrag b); Reserve für Jahre kälter als das Klimajahr |
| kältestes Tagesmittel | `KaeltesterTag` (H10) | nur Anzeige, zum Abgleich mit AK1 |

**Bemessungsfall** ist die Gleichgewichtsform mit dem größten Anstieg ΔT_max des Heizkalenders bei T_a,B:
t_auf,max = n_max − 1 mit dem kleinsten n_max ≤ 48, für das Φ̄_n ≤ P_auf; sonst „nicht erreichbar" (W1). ΔT_K gilt
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

- **„täglich"** *(P6, Vorgabe)*: je Sprung n nach 4.3 mit der kleinsten Außenlufttemperatur im Fenster, ohne ΔT_K,
  begrenzt auf min(t_auf,max + 1, D + 1); an milden Tagen ist n = 1, keine Rampe, keine Mehrwärme. **„fest"** *(P6,
  wählbar)*: jede Rampe n = min(t_auf,max + 1, D + 1) — eine feste Vorhaltezeit, mehr Wärme an milden Tagen.
- **Deckel** n ≤ 48 (EPOS-Wert). **Absenkdauer** n − 1 ≤ D; verlangt die Formel mehr, füllt die Rampe die Absenkung,
  und der Tag zählt als begrenzt (W2).
- **Rundung (F8):** das kleinste haltende n, also Aufrunden; keine Mindestrampe, denn 1 h Vorlauf mit dem Zielwert
  verschöbe den Sprung nur und senkte die Spitze nicht.

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

| Hinweis (benannt; Protokoll, Bedarfsdialog, Bericht; der Lauf rechnet weiter) | Kriterium |
|---|---|
| **W1** Aufheizleistung reicht nicht | P_auf ≤ Φ_stat(θ_T,max, T_a,B); dazu die Zahl der Tage mit P_auf ≤ Φ_stat bei T_a des Tages |
| **W2** durch die Absenkdauer begrenzt — „Absenkung weitgehend wirkungslos" | t_auf,max ≥ kürzeste regelmäßige Absenkdauer, dazu die Tage mit n − 1 = D und größerem Bedarf; nur aus der Stufenformel |
| **W3** Nachweisband | im Fenster [h_s − n + 1, h_s + 2] Leistung > 1,01 · P_auf oder Kappungsanteil > 0 (Tageszahl) |
| **W4** Übergang aus „aus" ohne Rampe | Zahl der Sprünge aus NaN |
| **W5** gekoppeltes Gebäude nicht optimiert | wirksame AK1 im Einzonenweg, bis KP3b |

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
| N-AH10 Ränder | Sprung am 1. Januar, D < n, Übergang aus „aus", Deckel 48, Kühlkappung an θ_K − 1 K, AK1-Gebäude | benannte Ergebnisse W1–W5 |

## 5. Datenmodell und Schema

### 5.1 Die zwei Tabellen

**`Tab_Konditionierungskalender`** — ein Kalender je Eigentümer und Größe, `STRICT`:

| Spalte | Typ | Regel |
|---|---|---|
| `ID` | INTEGER PRIMARY KEY AUTOINCREMENT | |
| `ID_Gebaeude` | INTEGER | → `Tab_Gebaeude(ID)` ON DELETE CASCADE; am Zonenkalender das Gebäude der Zone |
| `ID_Zone` | INTEGER | → `Tab_Zone(ID)` ON DELETE CASCADE; NULL = kein Zonenkalender |
| `ID_Gebaeude_Stamm` | INTEGER | → `Tab_Gebaeude_STAMM(ID)` ON DELETE CASCADE; Kalender eines Katalogbaus (P3) — der Name folgt dem Katalogverweis, den `Tab_Gebaeude` schon führt (Schritt 121) |
| `Groesse` | TEXT NOT NULL | `CHECK (Groesse IN ('HEIZSOLL','KUEHLSOLL','LUEFTUNG','GERAETE','PERSONEN'))` |
| `Wert` | REAL | Grundangabe |
| `Aus` | INTEGER NOT NULL DEFAULT 0 | `CHECK (Aus IN (0,1))` |
| `Woche` | TEXT | Standardwoche (5.2); `CHECK (length(Woche) <= 1400)` |
| `Nennwert` | REAL | W bei 100 %; `CHECK (Nennwert IS NULL OR (Groesse IN ('GERAETE','PERSONEN') AND Nennwert >= 0))`; bei Geräten heißt NULL `Interne_Waermegewinne` |
| `Bemerkung` | TEXT | Vermerk der Voreinstellung; `CHECK (length(Bemerkung) <= 200)` |

**Eigentümerregel:** genau ein Gebäude oder genau ein Katalogbau —
`CHECK ((ID_Gebaeude IS NOT NULL AND ID_Gebaeude_Stamm IS NULL) OR (ID_Gebaeude IS NULL AND ID_Zone IS NULL AND
ID_Gebaeude_Stamm IS NOT NULL))`; Katalogbauten haben keine Zonen. Genau eines von `Wert`, `Aus = 1`, `Woche`
(`CHECK ((Wert IS NOT NULL) + (Aus = 1) + (Woche IS NOT NULL) = 1)`). Eindeutigkeit über drei Teilindizes —
`UNIQUE (ID_Gebaeude, Groesse) WHERE ID_Gebaeude IS NOT NULL AND ID_Zone IS NULL`, `UNIQUE (ID_Zone, Groesse) WHERE
ID_Zone IS NOT NULL` und `UNIQUE (ID_Gebaeude_Stamm, Groesse) WHERE ID_Gebaeude_Stamm IS NOT NULL`, weil ein
gewöhnliches `UNIQUE` wegen NULL doppelte Kalender zuließe; Teilindizes gibt es im Schema noch nicht, sie kommen erst
nach der Werkzeugprobe (R4), sonst hält der Controller die Eindeutigkeit mit Datenbankfall. **Konsistenz:** Am
Zonenkalender ist `ID_Gebaeude` das Gebäude der Zone — Controllerregel und Datenbankfall. Das Schloss des Katalogbaus
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
`DbWerte` (ASCII, eingefroren). **Keine Spalte am Gebäude:** Die Zuordnung läuft über den Eigentümer, eine Beziehung
über IDs wie bei `Tab_Zone`; es entsteht weder ein Sichtneubau von `Abfrage_Projektgebaeude` noch eine neue Spalte in
`Tab_Gebaeude_STAMM`, die zwei Fallen aus Anlagenkopplung 8.6 entfallen. Katalog- und Projektkalender stehen in
derselben Tabelle, unterschieden allein durch den Eigentümer.

### 5.2 Die Woche als Text

Die Standardwoche ist ein 168-Werte-Text nach H8: Montag 00:00 bis Sonntag 23:00, Trennzeichen `;`, Punkt als
Dezimaltrennzeichen, `InvariantCulture`. Jede Zelle ist eine Zahl mit bis zu **vier Nachkommastellen** oder das
Kennwort `aus` (P2); in den Grenzen der Größen bleiben 168 Werte unter 1 400 Zeichen, sonst benannte Ablehnung. Leser
und Schreiber sind ein **eigener strenger Kalenderwochenleser** (genau 168 Zellen, kein Auffüllen, benannter Fehler
mit Stelle); der Leser von `Sollwertprofil` bleibt, wie er ist. Text für die Woche, Zeilen für die Perioden (F4):
Perioden brauchen Datum, Rang und Art als prüfbare Spalten, die Woche ist ein dichter Wertvektor, für den das Haus mit
H8 die Textform entschieden hat; Wochenraster, Leser und Vorschau aus AK1 lassen sich weiter nutzen.

### 5.3 Projektschalter und Ergebnisspalten (KP3)

`Tab_Einstellungen`: `Aufheizoptimierung` INTEGER NOT NULL DEFAULT 0 `CHECK IN (0,1)`; `Aufheiz_Bemessung` TEXT
`IN ('STUNDE','STUNDE_ABZUG')`, NULL heißt (a); `Aufheiz_Abzug_K` REAL 0…10, NULL heißt 2 K; `Aufheiz_Reserve` REAL
0…1, NULL heißt 0,2; `Aufheiz_Art` TEXT `IN ('TAEGLICH','FEST')`, NULL heißt täglich (P6) — jeweils `CHECK (… IS NULL
OR …)`. Modus und Abzug stehen getrennt, damit NULL nicht zugleich „Variante (a)" bedeutet. `Tab_ErgebnisGebaeude` und
`Tab_ErgebnisZone` bekommen die Spalten aus 4.8, alle nullbar (Muster E30), `Aufheiz_Leistungsquelle` mit
`CHECK (… IN ('GRENZE','ZIEL'))`.

### 5.4 Die Schemaschritte

**KP-S1** die zwei Tabellen mit allen drei Eigentümern samt Indizes (KP1), **KP-S2** die Projektspalten und **KP-S3**
die Ergebnisspalten (beide KP3). Die Nummern vergibt die Beauftragung aus `SchemaStand.Zielversion` + 1 — bei
Abfassung **ab 151** (Zielversion 150) — und prüft sie **spät gegen `origin`**, unmittelbar vor dem Schemacommit, weil
die Wellen von G6c die nächsten Nummern belegen können (ADR-001, Register A11). Je Schritt eine `static class …Schema`
mit `SCHRITT`, `Vollstaendig()` und `Alle(bericht)` nach `NachtzeitSchema` (Schritt 144), wiederholbar, eingehängt in
`SchemaMigration`, `Werkzeuge/Testdatenbankschema` (Muster `Program.cs:1919-1939`) und die Schemakopien; die
Testdatenbank wandert im selben Merge mit aktivem LFS-Filter. **Kein DML** — Katalogkalender entstehen erst durch
Anlegen oder durch eine spätere, eigens begründete Saat; der Referenzlauf bleibt byte-gleich.

### 5.5 Kopierwege, Leser und Werkzeuge

| Weg | Wirkung | Maßnahme |
|---|---|---|
| Katalog → Projekt (Übernahme) | angelegte Katalogkalender reisen mit (P3) | `GebaeudeStammCtrl.CopyFromStamm` (`EPOS.Kern/Controller/GebaeudeStammCtrl.cs:606-615`), der einzige Weg Katalog → Projekt, kopiert Kalender und Perioden in derselben Transaktion an das neue Projektgebäude; eine erneute Übernahme ersetzt dessen Gebäudekalender nach Rückfrage, Zonenkalender bleiben |
| Projekt → Katalog („Speichern unter") | Gebäudekalender reisen mit | Zonenkalender bleiben zurück, die Rückfrage nennt sie wie die Zonen |
| Katalogbau duplizieren, sperren, löschen | Kalender folgen dem Katalogbau | `Katalogkopie.Duplizieren` mit dem Kalender als Kindtabelle und den Perioden darunter (`GebaeudeStammCtrl.cs:915`); das Schloss (`SchlossSetzen`, `:924`) sperrt auch die Kalender; Löschen über die Kaskade |
| Projekt duplizieren, Variante | Kalender reisen mit | **von Hand** in `KINDER` (`EPOS.Kern/Controller/ProjektDuplizierenCtrl.cs:226, 300-330`): Kalender über `ID_Gebaeude`, Perioden über den Kalender, `ID_Zone` über die zuerst deklarierte Beziehung (Muster `Tab_Zonenluftstrom`); `ProjektplanKinderWacheTests` hält es |
| Projekttransfer (`.wpx`) | erbt den Plan | Rundlaufprobe; eine ältere Fassung nennt die unbekannte Tabelle im Importbericht |
| Gebäude oder Zone löschen | Kaskade | Datenbankfall |
| Import gbXML, IFC | schreibt die Felder (E43) | unverändert; Zeitpläne der Datei bleiben ungelesen |
| Export gbXML | `SollHeizenC` aus dem Tagwert | mit angelegtem Heizkalender der häufigste Wert der Nutzungsstunden Mo–Fr; Verlustliste `GebaeudeExportVerluste` um „Kalender" |
| Zapfprofil, Ferien-Vorbelegung | liest `Ferienbeginn/-ende` | mit angelegtem Heizkalender dessen Perioden der Art FERIEN; `ZapfprofilReferenzprojektWacheTests` bleibt grün |
| Auslieferungsvorlage | Katalogkalender der ausgelieferten Katalogbauten und Kalender der Beispielprojekte reisen mit | Die Projektbereinigung erfasst die Kalender über `ID_Gebaeude` als Folgetabelle und lässt Katalogkalender (`ID_Gebaeude` leer) stehen (`Werkzeuge/Auslieferungsvorlage/Projektsicht.cs:24-40, 75-87`); die Katalogbereinigung (`--kataloge readonly`) räumt die Kalender entfernter Katalogbauten über die Kaskade; der **Prüfbericht** zählt Kalender und Perioden je Eigentümerart; die Werkzeugtests zählen zwei STRICT-Tabellen mehr |
| `SqlDialektPruefer`, KI-Wissen | neue Anweisungen, neue Handlungen | Prüfer ziehen; Aktionswissen „Kalender anlegen", „Voreinstellung", „Aufheizoptimierung" |

**Die alten Spalten bleiben** bis GA (F5): Parameter des Standardfahrplans, Eingaben von Altweg und Katalog, Ziel des
Imports, Quelle des Exports; bei angelegtem Kalender ruhen sie für diese Größe. `Sollwertprofil` bleibt für AK1
lesbar, die Gruppe „Wärmeübergabe" bietet „In den Kalender übernehmen" an (KP2); `Kuehl_Sollwert_Nacht` bleibt
ungelesen (P7).

## 6. Rechenkern und Einbau

| Klasse (Modul `Gebaeude/`, ohne Datenbank) | Aufgabe |
|---|---|
| `Konditionierungskalender` (unveränderlich) | Grundangabe, Standardwoche, Perioden; `Auswerten(w₀, Referenzjahr) → double[8760]` |
| `Kalenderregel`, `Feiertage` | eine Periode samt `Enthaelt(d)`; die neun Feiertagsregeln, Osterdatum als Rechenvorschrift |
| `Kalenderwoche`, `Kalenderleser` | strenger Leser und Schreiber der Woche (5.2); Zeilen → Kalender mit `GebaeudeModellFehler.KalenderUngueltig` |
| `Standardfahrplan` | der abgeleitete Kalender (3.3) — zugleich Generator von „Anlegen" |
| `Konditionierungseingang` | löst je Zone und Größe die Kette 3.4 auf, liefert fünf Reihen |
| `Voreinstellung` | Records und Generatoren V1–V14; rein, deterministisch |
| `Aufheizantwort` (Methode in `Zonenmodell2K`) | H_s, τ_k, r_k, C_k des geregelten Falls zum Strahlungsanteil |
| `Aufheizoptimierung` | Sprünge, Bemessungsfall, Stufenformel, P_auf, Rampenreihe, Hinweise W1–W5 |
| `KonditionierungCtrl` (`EPOS.Kern/Controller/`) | Lesen und Schreiben je Gebäude oder Katalogbau in **einer** Transaktion, Anlegen, Verwerfen, Eigentümer- und Konsistenzregel, Kopie für die Wege aus 5.5; Hülle `KonditionierungHuelle` in `EPOS.UI.Daten/Bedarf/` |

**Einbau in `GebaeudeModellEingang`.** `ThetaSoll` = Heizkalender, danach nur mit Schalter die `Aufheizoptimierung`
(nach `:920`); `ThetaMax` = Kühlkalender, „aus" = +∞ (`:930`), `GebaeudeModellErgebnis.KuehlSollwert` wird zur Reihe;
`PhiConv` und `PhiRad*` bekommen je Stunde Q_G(h) + Q_P(h) statt der Konstante (`:810-811`). Der Luftwechsel geht mit
seinem **Jahresminimum** in die Ersatzparameter, der Überschuss (n(h) − n_min) · V · ρc läuft je Stunde als
Zusatzleitwert, nie negativ (`Stundenrand.cs:113-117`), dazu die Sommerlüftung nach 3.6; der Zwischenspeicher des
freien Falls (`Zonenmodell2K.cs:959-966`) wird ein kleiner, geordneter Speicher je Leitwert (R7). Die Zonen rechnen je
Zone, die Zonenschleife läuft **einmal**. `Schritt` schreibt den Kappungsanteil von `Heizleistung_Max` auch im
idealen Fall — ein neuer Ausgang, keine geänderte Zahl. Der Lauf liest ausschließlich Projektkalender.

**Bauvorschrift der Byte-Gleichheit.** Ohne angelegten Kalender, mit Anteil 1 und ohne Schalter nimmt der Eingang
**wörtlich den Bestandszweig**: keine Multiplikation mit 1, kein neues Minimum, kein Umweg über den Kalender, dieselbe
Sommerlüftungsregel, dieselbe konstante `ThetaMax`-Reihe, derselbe Zwischenspeicher, derselbe Vorlaufstart. Keine
Uhr, kein Zufall, `InvariantCulture` in Leser und Schreiber.

**Tests** in `EPOS.Kern.Tests` unter en-US; Tests mit deutschen Texten pinnen de-DE mit der `Kulturvorrichtung`:

- **Kalender:** Rundlauf und Strenge des Wochenlesers (167 und 169 Werte, „aus", Rang doppelt, Tag 0 und 366,
  unbekannte Feiertagsregel); Vorrang und Quelle je Stunde; Ring; Umstelltage MEZ/MESZ; Feiertage mit Referenzjahr
  2024 und 2025 (R5); je Voreinstellung eine erwartete Wochenreihe.
- **Wache Standardfahrplan** (3.3) über alle Gebäudezeilen der Testdatenbank, die Grenzfälle und alle sieben
  Wochentage des 1. Januar; Anlegen ergibt dieselbe Reihe, auch für Zonen mit eigenen Sollwerten; Jahresmittel der
  Gewinne vor und nach dem Anlegen des Personenkalenders gleich (R1); Vererbung; Nutzungszeit.
- **Reihen:** Anteil 1 und konstanter Luftwechsel bitgleich; Zusatzleitwert nie negativ; Kühlprüfung und Kühlkappung
  der Rampe (R6); „aus" in Stunde 8 040, Kühl-„aus" mit Sommerlüftung (R3); **Aufheizen** N-AH1 bis N-AH10.
- **Datenbankfälle:** KP-S1 zweimal; Eigentümerregel; Löschkaskade an Gebäude, Zone und Katalogbau; Duplikat,
  Variante und `.wpx`-Rundlauf mit Gebäude- und Zonenkalendern; Übernahme Katalog → Projekt mit Kalendern, erneute
  Übernahme, „Speichern unter", Katalogkopie und Schloss (R10); Konsistenzregel; `SqlDialektPruefer`;
  `Werkzeuge/Auslieferungsvorlage` mit Katalogkalendern samt Prüfbericht und `Werkzeuge/Testdatenbankschema` im Gate
  (R4).

## 7. Oberfläche Windows und iOS

Der Katalogeditor bekommt **in allen Modi** — Projekt, Katalog neu und Katalog bearbeiten — statt „Temperaturen und
Ferien" den Reiter **„Konditionierung"** mit einer Karte je Größe (P3). Im Katalog sperrt das Schloss eines
ausgelieferten Satzes auch seine Karten, mit Grund am Element; „Duplizieren" öffnet eine bearbeitbare Kopie samt
Kalendern. Zonenkarten gibt es nur im Projekt. Der Detailblock des Gebäudedialogs zeigt je Größe eine Zeile
(„Heizsollwert: Kalender, 3 Perioden").

```
┌ Heizsollwert ───────────────────────────────────────────── [Voreinstellung ▾] (i) ┐
│ Quelle  (•) aus den Feldern: Tag 20 °C · Nacht 18 °C · 22–6 Uhr   ( ) Kalender     │
│         [Kalender anlegen]  leise: „übernimmt diesen Fahrplan unverändert"         │
│ Grundangabe  ( ) Wert [    ] °C   ( ) aus   (•) Standardwoche                      │
│ Zeitfenster  [Mo][Di][Mi][Do][Fr][Sa][So] von [ 6] bis [22] [20,0] °C  [Setzen]    │
│ Woche        [Wochenraster 7 × 24 — Zeile setzen · kopieren · „aus"]               │
│ Perioden     ▲▼ Pause     Heizpause     01.05.–30.09.   aus           ✎ 🗑        │
│              ▲▼ Feiertag  Ostermontag   (Regel)         wie Sonntag   ✎ 🗑        │
│ Vorschau     [Jahr | Woche ▾]  ‹DiagrammSvg›   Quelle der Stunde: …                │
│ Kennwerte    Jahresmittel 18,7 °C · Stunden „aus" 3 672 · Sprünge 261              │
└────────────────────────────────────────────────────────────────────────────────────┘
```

- **Karten:** Lüftung, Geräte und Personen zeigen zusätzlich den Nennwert mit Herleitungszeile und das Jahresmittel in
  W und W/m² neben dem Wert von `Interne_Waermegewinne` (P1); „Verwerfen" führt zurück zu den Feldern.
- **Woche:** Das **Wochenraster** bleibt der Baustein der Stundeneinstellung und bekommt drei abschaltbare Zusätze,
  die AK1 und AK2 nicht berühren: das **Zeitfenster-Werkzeug** (sieben Tagesknöpfe, von, bis, Wert — der Weg ohne
  Zellenarbeit auf dem Tablet), den Zellenzustand **„aus"** (P2) und eine **schmale Anordnung** unter 900 px — ein Tag
  als 2 × 12 Zellen, damit Zellen 44 px behalten und nichts quer rollt (24 Zellen zu 44 px brauchen 1 056 px).
- **Perioden und Vorschau:** Die Periodenliste ist eine schlichte Tabelle ohne Virtualisierung; ✎ öffnet die Periode
  **in der Karte**. Die Vorschau ist ein `Zeichenmodell` in `DiagrammSvg`: die Woche über
  `ChartRenderer.StundenprofilModell` wie heute (`EPOS.UI.Daten/Bedarf/GebaeudeKatalogHuelle.cs:779-784`), das Jahr
  als neues **Teppichbild** (Tage × Stunden, Farbe = Wert, „aus" als eigene Fläche), gehalten von `Proben/ChartProben`.
- **Voreinstellungen** klappen in der Karte auf: Auswahl, Parameter mit den Werten des Gebäudes, vorab die Wirkung
  („ersetzt: 2 Ferienperioden · bleibt: Heizpause") und die Vorschau vorher und nachher. „Übernehmen" wirkt auf den
  **Arbeitsstand**, geschrieben wird mit dem OK des Editors; „Zurücknehmen" nimmt den letzten Schritt zurück.
- **Zonen:** Im Reiter „Zonen" trägt jede Zone je Größe „vom Gebäude" (gesperrte Anzeige mit Grund am Element) oder
  „eigener Kalender"; „vom Gebäude übernehmen und anpassen" legt eine Kopie an. Der Kühlkalender der Zone ist bis KU3
  weich gesperrt, mit Grund am Element.
- **Übernahme aus dem Katalog:** Die Katalogauswahl zeigt je Satz, ob er Kalender trägt; die Rückfrage einer erneuten
  Übernahme nennt die Gebäudekalender, die ersetzt werden, und die Zonenkalender, die bleiben.
- **Aufheizoptimierung** in `EPOS.UI/Seiten/Simulation/SimulationKonfigSeite.razor` neben Kühlbetrieb und
  Anlagenkopplung: Schalter, (a)/(b) mit ΔT_K, Aufheizreserve ρ, Art täglich/fest und je Gebäude eine
  Herleitungszeile („t_auf,max 8 h bei −18,2 °C · P_auf 18,9 kW Zielleistung · C_w 8,3 kWh/K"). Ergebnisseite und
  Bericht führen die Kennzahlen aus 4.8 (Muster E30), das Bedarfsbild die geformte Sollwertreihe, der Bericht je
  Kalender eine Kurzform („Heizen: 20/18 °C, 22–6 Uhr, Heizpause 1.5.–30.9.").
- **Plattform:** Windows und iOS teilen jede Komponente; alles geschieht in der einen Überlagerung des
  Katalogeditors, keine modale Kette (iL5), Berührungsziele ≥ 44 px, keine Hover-Bedienung; die iOS-Hülle bleibt
  unverändert.
- **Glossar vor den Ressourcen:** Die neuen Begriffe kommen vor den englischen Texten in
  [Glossar](Glossar_Lokalisierung.md) § 13 (Muster U4, E28) — Vorschläge: Konditionierung → conditioning,
  Standardwoche → standard week, Periode → period, Voreinstellung → preset, Aufheizzeit → preheat time,
  Aufheizleistung → preheat power, Aufheizreserve → preheat reserve, Aufheizoptimierung → preheat optimisation.
- **Texte und Assistent:** `KOND_*` in **beiden** `.resx`, danach `Werkzeuge/ResourceDesigner`; jede neue Eingabe
  braucht ein Katalogfeld in `KiDialoge` (die Woche als ein Textfeld wie `sollwertprofil`, `KiDialoge.cs:5783`) oder
  einen Grund in `BewusstDraussen`, gezählt in `EINGABESTELLEN`; `KiMaskenabdeckungWacheTests` hält es.
- **Tests:** bunit je Baustein und Dialog (Feldbestand, Rückweg samt `null`, Zustand, Fall ohne Gaben), in den
  Projekt- und den Katalogmodi, dazu `StilblattTests`, `SchliesskreuzWacheTests`, `UeberlagerungstitelTests`.
  `Proben/Rasterprobe` nur, wenn `Raster`, `Katalogliste` oder die `.epos-raster*`-Regeln berührt werden.

## 8. Stufen und Aufwand

| Stufe | Inhalt | Vorbedingung | Abnahme | Basis | PT |
|---|---|---|---|---|---|
| **KP0** | Dieses Konzept und der Entscheid E52 (N1.59) — erledigt; offen: Nachzug der Schwesterpapiere (2.3), P_auf-Probe (4.4), Glossar § 13 | — | Papiere widerspruchsfrei, `DokumentationLinkWacheTests` grün | nein | 1–2 |
| **KP1** | KP-S1 mit drei Eigentümern, Kalendermodell, Leser, Standardfahrplan, Feiertage, Vererbung, fünf Reihen, stündliche Kühlprüfung, Controller, KINDER, Werkzeuge; dazu die Katalogkalender (P3): Kopierwege Katalog ↔ Projekt, Katalogkopie und Schloss, Auslieferungsvorlage samt Prüfbericht | KP0; Schemawellen von G6c gemergt | Kern-Gate, Proben und Datenbankfälle (6), Vorlagenlauf; Referenzlauf **byte-gleich** gegen R22 | nein | 10–14, davon 2–3 für P3 |
| **KP2** | Reiter und Karte in allen Modi samt Schloss, Zeitfenster, „aus", schmale Anordnung, Periodenliste, Voreinstellungen, Teppichbild, Zonen, Katalogauswahl, Assistent, Ressourcen; Ferienumrechnung im Gemeinjahr (B13) | KP1 | bunit, ChartProben, Sichtabnahme Windows; byte-gleich | nein | 11–15, davon 1 für P3 |
| **KP3** | Stufenformel, Nachweisband, Aufheizleistung, Bemessung, KP-S2, KP-S3, Ergebnis, Hinweise, Bericht, Export; neues Referenzprojekt über die Katalogübernahme, Einfrierregel, neue Basis, CI | KP2 | N-AH1–N-AH10; alle übrigen Projekte byte-gleich; A/B-Protokoll | **ja** | 6–9 |
| **KP4** | Papiere nachziehen (Rechenschritte mit neuem Schritt „Aufheizrampe", Leitkonzept 4.4, Softwarearchitektur, Status, Protokoll), Wiki-Quellen, Logbuch-Entwurf | KP3 | Wiki-Suchmuster aus `CLAUDE.md` leer, Link-Wache grün | nein | 1–2 |
| **Summe** | | | | | **29–42** |
| KP3b *(optional)* | AK1-Gebäude über die Vorausrechnung mit Ankunftskriterium; Vorkühlen mit KU3 | KP3; KU3 für die Kälte | wie KP3 | je nach Projekt | 3–5 |

Dazu je Einfrierschritt rund 0,5 PT. **Reihenfolge:** Die acht Fragen sind mit E52 entschieden; KP0 schließt mit
Probe, Glossar und Nachzug und läuft vor KP1. **KP1 folgt den Schemawellen von G6c** (in Arbeit, nächste Welle D),
weil beide Zonenkaskade und Schemastand berühren. **G6d** (Referenzprojekt mit Zonen, eigener Einfrierschritt) und
**KP3** frieren je eine Basis ein; liegen sie nah beieinander, spart ein gemeinsamer Einfrierschritt einen Lauf und
deckt die Zonenkalender mit (R8). **KU3 folgt KP1** (P7): KU3 verliert „Kühlsollwert Nacht" und baut „Kühlung je Zone"
auf den Zonenkalender. **AK2** profitiert, weil seine Komfortstunden der Nutzungszeit aus 3.4 folgen (F16).

## 9. Festlegungen F1–F18 und Entscheide P1–P8

### 9.1 Festlegungen nach Empfehlung — Widerspruch möglich

Sie verlangen keinen Entscheid; beide Entwürfe oder eine Hausregel beantworten sie. Ein Widerspruch ist bis zur
Beauftragung der genannten Stufe billig.

| Nr | Festlegung | Grund | bis |
|---|---|---|---|
| F1 | Fünf Größen; Beleuchtung in „Geräte", Heizbetrieb als „aus" (3.1) | Auftragswortlaut | KP1 |
| F2 | Die Zone erbt je Größe den ganzen Kalender oder führt einen eigenen; „übernehmen und anpassen"; Anlegen erhält Zonenwerte (3.3, 3.4) | beide Entwürfe; nur so bleibt Anlegen ergebnisneutral | KP1 |
| F3 | Voreinstellungen als Erzeuger im Kern, kein `_STAMM`-Katalog eigener Vorlagen (3.5) | beide Entwürfe; kein Kopierweg, keine Auslieferungspflege | KP2 |
| F4 | Zwei STRICT-Tabellen, die Woche als H8-Text (5.1, 5.2) | `CLAUDE.md` (STRICT, IDs, CHECK); H8 gilt für einen Wochenvektor | KP1 |
| F5 | Abgeleitet bis angelegt, kein DML, alte Spalten bis GA (3.3, 5.5) | bitgleich durch Bau; Altweg, Import, Export lesen weiter | KP1 |
| F6 | Lineare Treppe, letzte Stufe in der Sprungstunde (4.1) | „sukzessiver Anstieg"; n = 1 ist heute | KP3 |
| F7 | Bemessung mit der geschlossenen Stufenformel in einem Lauf, Nachweisband, Überlagerung und Vorausrechnung nur als Prüforakel (4.3) | exakt bei festen Randwerten, sicher sonst; kein Zweitlauf | KP3 |
| F8 | Rundung auf das kleinste haltende n, keine Mindestrampe (4.6) | eine Rampe von 1 h verschiebt den Sprung nur | KP3 |
| F9 | Schalter je Projekt, Vorgabe aus (4.8) | Byte-Gleichheit aller Bestandsprojekte | KP3 |
| F10 | Vorkühlen später, mit KU3 (4.7) | nicht beauftragt | KP3 |
| F11 | Bundeseinheitliche Feiertage als Voreinstellung, als Regel gespeichert (3.2) | Referenzjahr und Schaltjahr verschieben Jahrestage | KP1 |
| F12 | Wiki gebündelt, Version beim Anwender (10.4) | `CLAUDE.md` | KP4 |
| F13 | AK1-gekoppelte Einzonengebäude in KP3 benannt ausgenommen (4.7) | nichtlinear; die Übergabe kappt ohnehin | KP3 |
| F14 | Neues Referenzprojekt, Bestand unberührt, Einfrierregel „gesäte Konditionierungsdaten" (10.2, 10.3) | Regressionsnetz | KP3 |
| F15 | Lüftung als Anteil der Nutzerlüftung, Anzeige absolut (3.1) | 100 % ist exakt der Bestand | KP1 |
| F16 | Nutzungszeit aus dem Personenkalender (3.4) | folgerichtig; Rampenstunden zählen nicht | KP1 |
| F17 | Stündliche Kühlprüfung, Rampe an θ_K(h) − 1 K gekappt (3.6) | eine Optimierung bricht keinen Lauf ab | KP1 |
| F18 | Bemessung (a) als Vorgabe, (b) wählbar, ΔT_K = 2 K, nur für die Höchstzeit (4.5) | Auftragswortlaut; EPOS-Wert | KP3 |

### 9.2 Entscheide des Anwenders (E52, 26.09.2026)

Wortlaut: **„P1, P2: Empfehlung / P3: (b) / P4 bis P8: Empfehlung"**. Der Entscheid steht als Nachtrag N1.59 im
[Leitkonzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md); Frage, Hintergrund und alle Optionen stehen im
[Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) Kapitel 10.

| Nr | Frage | Entscheid | Folgen |
|---|---|---|---|
| **P1** | Was bedeutet `Interne_Waermegewinne` unter Kalendern? | **(b)**, nach Empfehlung: Geräte und Personen getrennt; beim Anlegen des Personenkalenders wird der Geräte-Nennwert = `Interne_Waermegewinne` − Jahresmittel der Personenwärme, energieerhaltend und sichtbar | keine Doppelzählung der Personenwärme; verworfen sind die Form mit Mittel 100 % (a) und die zusätzliche Personenwärme (c); Datenbankfall „Jahresmittel vor und nach dem Anlegen gleich" (R1); KP1 |
| **P2** | Soll die Heizung stundenweise „aus" sein können? | **(b)**, nach Empfehlung: Kalenderwoche mit Kennwort „aus" je Zelle, Wochenraster mit Schalter je Zelle, Übergang aus „aus" ohne Rampe (W4) | eigener Kalenderwochenleser (5.2), Vorlaufstart nach 3.6; die Rampe aus der frei schwingenden Temperatur (c) und damit ein Zweitlauf entfallen; KP1 und KP2 |
| **P3** | Sollen Katalogbauten der Auslieferung Kalender tragen? | **(b), abweichend von der Empfehlung (a):** ja — Eigentümer `ID_Gebaeude_Stamm`, Kopierweg Katalog → Projekt, Auslieferungsvorlage und Prüfbericht | dritter Eigentümer mit Eigentümerregel und Teilindex (5.1); Kopierwege über `CopyFromStamm`, „Speichern unter", Katalogkopie und Schloss (5.5); Karten in allen Katalogmodi (7); neues Referenzprojekt über die Katalogübernahme und Einfrierregel für gesäte Katalogkalender (10.2, 10.3); Mehraufwand 2–3 PT in KP1 und 1 PT in KP2 (8) |
| **P4** | Nutzungsmuster für Nichtwohnbauten (Büro, Schule) als Voreinstellung? | **(a)**, nach Empfehlung: ja, als EPOS-Muster mit runden Werten (V8, V9) | der Ausschluss in Leitkonzept 15 (mit N1.59 nachgezogen) und Anlagenkopplung 1.3 (Nachzug in KP0) ist auf Normprofile verengt; KP2 |
| **P5** | Woran bemisst sich P_auf ohne `Heizleistung_Max`? | **(b)**, nach Empfehlung: (1 + ρ) × stationäre Last an der kältesten Stunde, ρ = 20 % nach der KP0-Probe | (a) ist immer erreichbar; Rampen entstehen vor allem an kalten Tagen und in gut gedämmten Bauten (4.5); verworfen sind der Anker H10 und die Pflichtangabe; KP3 |
| **P6** | Aufheizzeit täglich berechnet oder fest? | **(a)**, nach Empfehlung: täglich, ≤ t_auf,max; **(b)** fest ist wählbar | Spalte `Aufheiz_Art`, NULL = täglich (5.3); an milden Tagen keine Rampe und keine Mehrwärme; KP3 |
| **P7** | Kommt das Zeitprofil der Kühlung (K11) jetzt in den Kalender? | **(a)**, nach Empfehlung: ja; `Kuehl_Sollwert_Nacht` bleibt ungelesen und füllt nur V11 vor | E27 ist bei K11 geändert: das Zeitprofil kommt mit KP1 statt mit KU3, KU3 behält „Kühlung je Zone"; eine zweite Wahrheit (b) entfällt; Register K11 mit Vermerk |
| **P8** | Wie zeigt das Ergebnis den Vergleich mit und ohne Rampe? | **(a)**, nach Empfehlung: über eine Projektvariante; ein optionaler Vergleichslauf bleibt spätere Wahl | keine Mehrrechenzeit, kein Zweitlauf; KP3 |

## 10. Nachweise, Abnahme, Einfrierregel, Wiki

### 10.1 Nachweise je Stufe

- **KP1:** Rechenproben und Datenbankfälle nach 6, Vorlagenlauf der Auslieferungsvorlage mit Katalogkalendern;
  **Referenzlauf byte-gleich** gegen die geltende Basis (heute `2026-09-26_R22_Solarthermie`, fünfzehn Projekte) —
  kein Referenzprojekt trägt einen Kalender. **KP2:** bunit und Wachen (7), `Proben/ChartProben` mit dem Teppichbild,
  Sichtabnahme in `EPOS_Plan.exe` in Projekt- und Katalogmodi; Referenzlauf byte-gleich.
- **KP3:** N-AH1 bis N-AH10; alle Bestandsprojekte byte-gleich (Schalter aus); das neue Referenzprojekt mit
  A/B-Protokoll; neue Basis mit Protokoll in [`Referenzlaeufe/LIESMICH.md`](../../Referenzlaeufe/LIESMICH.md);
  `kern.yml` rechnet das Projekt als achtes mit. **iOS:** Die Hülle bleibt unverändert, keine
  `Dienste.*`-Schnittstelle wird berührt; der grüne Kern-Lauf ist der Nachweis, ein iOS-Lauf nur auf Zuruf.

### 10.2 Das neue Referenzprojekt

Kopie eines Projekts mit genau einem Gebäude auf dem VDI-Weg (Vorschlag 1007; 1030 rechnet ohne Gebäude, 1040 auf dem
Altweg). Das Gebäude kommt **über die Katalogübernahme** aus einem Katalogbau mit gesäten Kalendern, damit der
Kopierweg Katalog → Projekt (P3) im Regressionsnetz steht: angelegter Heizkalender (Nachtabsenkung, Wochenende, Ferien
V5, Feiertage V6), Personen-, Geräte- und Lüftungskalender (V7 oder V8); dazu Aufheizoptimierung in Variante (b) mit
2 K. Weil die Rampe mit der Vorgabe nur in gut gedämmten Bauten oder mit knapper `Heizleistung_Max` wirkt (4.5), wählt
die KP0-Probe den Bau so, dass die Rampe an kalten Tagen greift. Die Nummer ist die nächste freie nach 1049 und wird
beim Einfrieren gegen die Testdatenbank geprüft; das Skript liegt unter `Referenzlaeufe/Skripte/`. Zonenkalender deckt
das Projekt nur, wenn der Einfrierschritt mit G6d zusammenfällt (R8).

### 10.3 Einfrierregel „gesäte Konditionierungsdaten"

Mit KP3 kommt in [`CLAUDE.md`](../../CLAUDE.md), Abschnitt „Regressionsnetz", und in `Referenzlaeufe/LIESMICH.md`
eine neunte Einfrierregel: *Wer gesäte Konditionierungsdaten eines Referenzprojekts ändert, friert im selben Schritt
die Basis neu ein.* Betroffen sind die Kalender- und Periodenzeilen seiner Gebäude und Zonen samt Nennwerten, die
**gesäten Katalogkalender** der Katalogbauten, aus denen sein Referenzskript die Gebäude übernimmt
(`ID_Gebaeude_Stamm`), `Aufheizoptimierung` und die Spalten `Aufheiz_*` seiner `Tab_Einstellungen`, das Referenzjahr
seiner Spotpreisreihe (es verschiebt Wochentage und Feiertage) und das Anlegen oder Entfernen eines Referenzprojekts
mit Kalender oder Aufheizoptimierung.

### 10.4 Wiki-Änderungen (nur Liste; Veröffentlichung gebündelt nach KP3)

| Seite (`Projekte/Wiki/`) | Abschnitt | Änderung |
|---|---|---|
| `Programm Dokumentation - Gebäude.wiki` | Reiter „Temperaturen und Ferien" (Anker `raumtemperaturen`, `nachtzeit`, `ferienzeiten`) | Reiter „Konditionierung" in Projekt und Gebäudekatalog (Anker `konditionierung`, `kalender`, `voreinstellungen`), Schloss ausgelieferter Sätze, Kalender bei der Übernahme; die alten Anker bleiben im neuen Abschnitt |
| `Programm Dokumentation - Gebäudemodell VDI 6007.wiki` | Eingaben, Lüftung, Ergebnisse, Grenzen | Sollwerte, Gewinne und Luftwechsel als Kalender; neuer Abschnitt „Aufheizoptimierung" (Anker `aufheizoptimierung`) |
| `Programm Dokumentation - Mehrzonenmodell.wiki` | Zonen anlegen, Ergebnisse | Zonen erben Kalender oder führen eigene; der Satz über Nachtzeit, Ferien und Kühlung vom Gebäude wird ersetzt |
| `Programm Dokumentation - Kühlung.wiki` | Eingaben im Gebäudedialog | Kühlsollwert als Kalender, Kühlperiode, Nachtanhebung |
| `Programm Dokumentation - Simulation.wiki` | Projekteinstellungen | Schalter Aufheizoptimierung, Variante (a)/(b), Aufheizreserve, Art |
| `Programm Dokumentation - Simulationsergebnisse.wiki` | Gebäudekennzahlen | Kennzahlen der Aufheizzeit, Hinweise W1–W5 |
| `Programm Dokumentation - Gebäudeimport.wiki` | Vorgaben | der Import setzt Werte, der Kalender entsteht auf Knopfdruck |

Die Seiten beschreiben die Funktion, wie sie ist, ohne Hersteller- und Produktdaten; Beispiele tragen neutrale Namen
mit runden Werten.

### 10.5 Logbuch-Entwurf

Ein Satz, veröffentlicht mit dem gebündelten Upload; die Versionsnummer ist beim Anwender zu erfragen:

> „Gebäude, Zonen und Katalogbauten führen für Heiz- und Kühlsollwert, Lüftung, Geräte und Personen einen
> stundengenauen Kalender mit Voreinstellungen, und die Aufheizoptimierung ersetzt den Sollwertsprung nach einer
> Absenkung durch eine berechnete Aufheizrampe."

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
| R4 | Teilindizes, gemischter Eigentümer und neue Tabellen in Werkzeugen und Schemakopien | Gegenprüfung | `SqlDialektPruefer`, `Testdatenbankschema`, Projektsicht, Katalogbereinigung und Tests der Auslieferungsvorlage im KP1-Gate | offen |
| R5 | Wochentags- und Feiertagsversatz beim Wechsel der Preisreihe oder im Schaltjahr | Gegenprüfung | Tests mit Referenzjahr 2024 und 2025; Feiertagsregel im Lauf aufgelöst | offen |
| R6 | Harte Kühlprüfung nach der Rampe bricht den Lauf ab | Gegenprüfung | Rechenprobe mit gestuftem Kühlkalender; Kappung an θ_K − 1 K benannt (F17) | offen |
| R7 | Zwischenspeicher des freien Falls wird bei stündlichem Luftwechsel ständig neu gebaut | Gegenprüfung | Laufzeitprobe mit Lüftungskalender; byte-gleich ohne | offen |
| R8 | Zonenkalender ohne Referenzabdeckung (0 Zonen in der Testdatenbank) | Gegenprüfung | Zonen im KP3-Referenzprojekt oder gemeinsames Einfrieren mit G6d | offen |
| R9 | Basis und Projektnummer wandern | Gegenprüfung | KP1/KP2 gegen die dann geltende Basis (heute R22); Nummer nach 1049 beim Einfrieren gegen die Testdatenbank | offen |
| R10 | Katalog- und Projektkalender laufen auseinander, oder die Auslieferungsvorlage verliert Katalogkalender | E52 (P3) | der Lauf liest nie den Katalog; Datenbankfälle Übernahme, erneute Übernahme, Katalogkopie, Schloss; Vorlagenlauf mit Katalogkalendern, Prüfbericht je Eigentümerart | offen |

## 12. Verweise

**Papiere.** [Leitkonzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) (4.4–4.6, 5.6, 15, N1.32, N1.37, N1.48,
N1.55, N1.56, N1.59); [Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) (Kapitel 10, K11);
[Statusdatei](Status_Gebaeudesimulation_VDI6007.md); [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md)
(3.4, 7.1, 11); [Anlagenkopplung](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) (1.3, 3.7, 4.3, 4.4, 8.4,
8.6, 9.2); [Mehrzonenmodell](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) (2.6, 2.9);
[Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) (4, 5, 8.2, 8.3, 9);
[Datenaustausch](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) (13);
[Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) (6, Löschliste GA);
[ADR-001](ADR-001_Schema-Ausrollung.md); [ADR-005](ADR-005_Zonenkopplung_Mehrzonenmodell.md);
[ADR-006](ADR-006_Trennung_Altweg_VDI6007.md); [Glossar](Glossar_Lokalisierung.md) § 13;
[BETRIEB_SQLITE](BETRIEB_SQLITE.md) § 6; [Konzept Hilfesystem](Konzept_Hilfesystem_Wikidokumentation.md) 13;
[`Referenzlaeufe/LIESMICH.md`](../../Referenzlaeufe/LIESMICH.md); [`EPOS.UI/CLAUDE.md`](../../EPOS.UI/CLAUDE.md);
[Protokoll G6b](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-26_G6b_Mehrzonenrechnung.md).

**Entscheide und Registerpunkte.** E8 (Skalierung), E27 mit K11 (Zeitprofil der Kühlung, mit E52 geändert), E28 mit
U4 (Glossar vor den Übersetzungen), E30 (Ergebnistabelle), E32 (freier Lauf ohne Kühlung), E36 (Rechenzeit der
Kopplung), E43 (Nachtzeit je Gebäude, Vorgaben), E49 mit A4 (a) (Kühlwerte vom Gebäude), E52 (P1–P8), N1.56
Festlegungen 1, 2, 7 und 10; H5, H7, H8, H10 und H-F10 der Anlagenkopplung; U7 (Ortszeit-Kalender); A11
(Schrittnummern bei Beauftragung); K5 (Feuchte).

**Code.** `EPOS.Kern/Allgemein/Simulation/Gebaeude/` (`GebaeudeModellEingang.cs`, `Zonenmodell2K.cs`,
`Stundenrand.cs`, `Vdi6007Rechenweg.cs`, `Zonenrechnung.cs`, `Sommerlueftungsregel.cs`, `GebaeudeFestwerte.cs`,
`GebaeudeModellErgebnis.cs`, `HeizkreisErgebnis.cs`); `EPOS.Kern/Allgemein/Update/AnlagenkopplungSchema.cs`,
`NachtzeitSchema.cs`, `SchemaStand.cs`; `EPOS.Kern/Allgemein/Katalog/Katalogkopie.cs`,
`Auslieferungskennzeichen.cs`; `EPOS.Kern/Controller/GebaeudeStammCtrl.cs`, `ProjektDuplizierenCtrl.cs`,
`SolardatenCtrl.cs`, `ZapfprofilCtrl.Eingang.cs`; `EPOS.UI/Bausteine/Wochenraster.razor`;
`EPOS.UI.Daten/Bedarf/GebaeudeKatalogHuelle.cs`; `EPOS.UI/Seiten/Simulation/SimulationKonfigSeite.razor`;
`Werkzeuge/Testdatenbankschema/Program.cs`, `Werkzeuge/Auslieferungsvorlage/Projektsicht.cs`; übrige Fundstellen in 1.

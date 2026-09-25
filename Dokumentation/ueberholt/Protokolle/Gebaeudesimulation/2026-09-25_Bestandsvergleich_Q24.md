# Protokoll: Bestandsvergleich alt/neu — Bedingung (1) des Ablösekriteriums Q24 (25.09.2026)

**Auftrag:** Bedingung (1) des Ablösekriteriums der Stufe GA (Q24, E27) für die Bestandsprojekte:
jedes Gebäude der Arbeitsdatenbank einmal auf dem Tagesbilanz-Weg („alt") und auf dem VDI-6007-Weg
(„neu") rechnen und die Abweichung je Gebäude und je Projekt erklären. **Anwenderentscheide vom
25.09.2026:** F1 = Gebäude und Projektwirkung; F2 = Quelle allein die Arbeitsdatenbank dieses
Rechners, Aufnahme nur bei geschlossenem Programm; F3 = der Anwender migriert die Arbeitsdatenbank
selbst mit einem aktuellen Build (die Migration der Kopie über den Windows-Referenzlauf bleibt
benannter Ersatzweg); F4 = Datenfehler korrigiert der Anwender, Bedingung (1) gilt als erfüllt,
wenn keine rote Zeile bleibt und der Anwender die Erklärungen der gelben Zeilen bestätigt.
Maßgeblich: [Umsetzungskonzept](../../../aktuell/Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
Kapitel 6.1 (Löschliste), [Leitkonzept](../../../aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
5.5, 5.9 bis 5.11 und 10.4, [Werkzeug](../../../../Werkzeuge/Gebaeudevergleich/LIESMICH.md).

Dieses Protokoll ist **namenlos und ohne Projekt- und Gebäudekennungen des Bestands**; es stützt sich
allein auf die zwei Zusammenfassungen des Werkzeugs. Die Einzelzeilen liegen beim Anwender außerhalb
des Repositoriums.

## 1 Das Werkzeug

| Teil | Stand |
|---|---|
| Ort | `Werkzeuge/Gebaeudevergleich` mit Testprojekt `Werkzeuge/Gebaeudevergleich.Tests`, eigene Projektmappe, nicht in `WP-Plan.sln` und nicht im Kern-Filter; CI-Schritt „Gebaeudevergleich-Tests" in `kern.yml` (nur ubuntu); fällt mit GA (Löschliste 6.1, „Tests und Nachweise") |
| Unterbefehle | `aufnahme` (immutable-Verbindung, `VACUUM INTO`, SHA-256 der Quelle davor und danach, Bestand der Nebendateien, Prozesssperre für die Produktivquelle), `vergleich` (je Gebäude zwei Aufrufe `GebaeudeBedarfCtrl.Rechnen` mit erzwungenem Weg, Kennzahlen, Merkmale, Ursachenregeln, Ampel), `variante` (Kopie mit einheitlichem `Gebaeude_Modell` für die Projektwirkung) |
| Sicherheit | Schreibnaht auf „nein", nie die Werkzeugfreigabe; eigene SQL nur `VACUUM INTO ?`, `PRAGMA integrity_check`, `UPDATE Tab_Gebaeude SET Gebaeude_Modell = ?` (nur auf der Variantenkopie); Ausgabe nie im Repository und nie unter `%ProgramData%\EPOS_PLAN`; ohne `--mit-namen` wird jeder Projekt-, Kunden-, Bearbeiter- und Gebäudename durch die ID ersetzt, die Zusammenfassung trägt weder Namen noch IDs |
| Ursachenregeln | U-E8, U-E8Z, U-NN, U-MW, U-KAT, U-SP, U-NG, U-SOL, U-BW, U-IL, U-ZO, U-AK, U-KU, U-TB; Band E8 = 0,1 % (Messung an Projekt 1009 der Testdatenbank: 0,000 %); Ampel „Fehler" bei gescheitertem Weg oder U-BW („Datenfehler"), „erklärt" bei U-E8 im Band oder Flächenangabe mit Δ Jahr 5–50 % ohne Katalogwert bzw. mit Katalogtreffer 90–115 % |
| Commits | `823142b3`, `25d0d398`, `f63a4a16`, `a554a6ca`, `06db710c`, zusammengeführt mit `f8a0d1d7` |
| Proben | T1–T15, 24 Fälle: Parität gegen die Basis (15 Gebäude auf dem VDI-Weg, 1040 auf dem Tagesbilanz-Weg, Kälte von 1017 und 1047), R12-Anker 1045 (+27,95 %, erklärt), Band E8, Fehlerfang Bauweise 50, Schreibort, Aufnahme, Schemastand, kein Schreiben, byte-gleiche Wiederholung, Namensfreiheit, Regeln an den Rändern, Schreibsperre, Variante |
| Gate | auf dem Stand des Anwender-Builds (`1b55e45a`, Zielversion 142): Proben 24/24, Referenzlauf der vierzehn Projekte gegen R16 PASS und byte-gleich; davor der Kern-Filter grün |

## 2 Durchführung

| Schritt | Ergebnis |
|---|---|
| Migration | vom Anwender mit dem Build auf `1b55e45a` (Schemastand 142, eigene Sicherung des Programms); der Ersatzweg über den Windows-Referenzlauf blieb ungenutzt |
| Erste Aufnahme | Exitcode 0, Programm geschlossen, keine Nebendateien, Quelle byte-gleich (SHA-256 davor und danach gleich), rund 0,9 s im Zugriff, `integrity_check` der Kopie ok |
| Erster Vergleich | Exitcode 1: 24 erklärt, 1 zu prüfen, **2 rot** — zwei Gebäude derselben Bauform mit Bauweise 50 Wh/K bei 304 m² Nutzfläche (0,16 Wh/(m²K), unter der Grenze 5; der stille Rückfallwert aus Leitkonzept 5.11), eines mit Flächen-, eines mit Verbrauchsangabe; der VDI-Weg lehnt beide benannt ab (`BauweiseUnplausibel`) |
| Korrektur | auf ausdrücklichen Auftrag des Anwenders: Bauweise beider Projektkopien auf 15 200 Wh/K (50 Wh/(m²K), wie in der Testdatenbank nach Q22), vorher Sicherung per `VACUUM INTO`, eine Transaktion über genau zwei Zellen, `quick_check` ok. Zwischen den Aufnahmen hat der Anwender außerdem einen Katalogsatz geändert und in einem Projekt ein Gebäude getauscht — beides gehört zum Bestand |
| Zweite Aufnahme | Exitcode 0, keine Nebendateien, Quelle byte-gleich, rund 0,8 s im Zugriff, Schemastand 142 |
| Zweiter Vergleich | **Exitcode 0: 25 erklärt, 2 zu prüfen, 0 rot**; die zwei korrigierten Gebäude sind erklärt (Flächenangabe +35,5 % mit Katalogtreffer 99,0 %; Verbrauchsangabe Δ 0,000 % nach U-E8) |
| Projektwirkung | zwei Varianten (alle Gebäude alt bzw. neu), je ein ganzer Lauf mit `EPOS.Referenzlauf` über die 24 Projekte mit Gebäude, Vergleich über die Aggregate |

Die Namensprüfung über alle Ausgaben des Werkzeugs fand in beiden Durchgängen keinen Namenswert der
Momentaufnahme; die zwei Zusammenfassungen tragen keine Projekt-, Gebäude- oder Klimaregionskennung.

## 3 Ergebnis je Gebäude (zweiter Durchgang)

| Größe | Anzahl |
|---|---:|
| Projekte gesamt | 33 |
| gerechnet | 24 |
| übersprungen | 9 (8 ohne Gebäude, 1 ohne Gebäude und mit unbekannter Klimaregion) |
| Gebäude auf beiden Wegen gerechnet | 27 |
| Gebäude mit Fehler | 0 |
| Ampel erklärt / zu prüfen / Fehler | 25 / 2 / 0 |

**Jahresabweichung Δ = neu / alt − 1** (Minimum / Q1 / Median / Q3 / Maximum):

| Angabe | n | Verteilung [%] | Bänder < 0 / 0–5 / 5–15 / 15–30 / 30–50 / > 50 |
|---|---:|---|---|
| Fläche, je Gebäude | 26 | 24,9 / 27,9 / 30,5 / 36,4 / 45,6 | 0 / 0 / 0 / 13 / 13 / 0 |
| Verbrauch, je Gebäude | 1 | 0,0 | 0 / 1 / 0 / 0 / 0 / 0 |
| Fläche, je Bauform-Gruppe (Median) | 10 | 24,9 / 30,4 / 35,0 / 37,6 / 45,6 | 0 / 0 / 0 / 3 / 7 / 0 |
| Verbrauch, je Bauform-Gruppe | 1 | 0,0 | 0 / 1 / 0 / 0 / 0 / 0 |

**Katalogtreffer** (Flächenangabe mit Katalogwert > 0, 19 Gebäude): neu 67,9 bis 109,3 %, davon 17 im
Band 90–115 %; alt 50,4 bis 87,6 %. Ohne Katalogwert 7, nicht bestimmbar (Verbrauchsangabe) 1.
**Verbrauchstreffer:** alt und neu je 100,00 %.

**Regeln:** U-MW 26, U-KAT 17, U-NG 16, U-SP 6, U-SOL 2, U-IL 2, U-E8 1; U-BW, U-E8Z, U-NN, U-ZO,
U-AK, U-KU und U-TB je 0 (im Bestand rechnet kein Projekt Kälte, keines ist gekoppelt, kein Gebäude
hat eine Zone, keines steht auf dem Tagesbilanz-Weg).

## 4 Die zwei gelben Zeilen — vom Anwender bestätigt

Beide liegen im Band des Modellwechsels; gelb sind sie allein, weil der Katalogtreffer des neuen Wegs
unter 90 % liegt. In beiden Fällen liegen **beide Wege** unter dem Katalogwert, und das Verhältnis
neu zu alt ist das aller anderen Gebäude — die Ursache liegt im Katalogwert, nicht im Rechenweg.

| | Gebäude A | Gebäude B |
|---|---|---|
| Bau | kleines Mehrfamilienhaus, Katalogwert 117 kWh/(m²a); die Projektkopie gleicht ihrem Katalogsatz in allen Zahlenspalten | großes, sehr kompaktes Mehrfamilienhaus (A/NF 1,08), Katalogwert 71,4 kWh/(m²a); das in einem Projekt getauschte Gebäude |
| Hülle | Transmission 1,04 W/(m²K) je m² Nutzfläche, innere Gewinne 2,93 W/m² | Transmission 0,49 W/(m²K) je m² Nutzfläche, innere Gewinne 1,96 W/m² |
| Δ Jahr | +34,5 % (U-MW) | +38,0 % (U-MW; U-SP: Spitze +81 %, Aufheizspitze bei 2 K Absenkung) |
| Katalogtreffer alt / neu | 50,4 % / 67,9 % | 58,1 % / 80,1 % |
| VDI-Weg | mittlere Raumtemperatur der Heizzeit 23,2 °C, 1 840 h über 24 °C, 4 270 Heizstunden | 22,1 °C, 1 386 h über 24 °C, 4 009 Heizstunden |
| Empfindlichkeit (eigene Kopie) | innere Gewinne 2,07 W/m²: 71,5 %; U-Werte × 1,26: 82,3 %; beides: 86,1 %; Bauweise 20 Wh/(m²K): 69,4 %; Δ Jahr bleibt 26,7 bis 34,5 % | innere Gewinne halbiert: 87,5 %; ohne innere Gewinne: 95,2 % (dann „erklärt"); Δ Jahr bleibt 35,4 bis 38,0 % |
| Erklärung | Der Katalogsatz widerspricht sich selbst: Sein Kennwert passt nicht zu seinen U-Werten. | Gewinnnutzung bei gut gedämmtem Bau: Innere und solare Gewinne decken einen großen Teil der geringen Verluste, das Stundenmodell nutzt sie voll; der Katalogwert ist konservativ. |
| Anwender | **bestätigt (25.09.2026)**; die Korrektur des Katalogsatzes nimmt der Anwender selbst vor (offen) | **bestätigt (25.09.2026)** |

**Hinweis zu Gebäude B:** Die Projektkopie trägt `WW_Bedarf = 0`, und im Katalogsatz ist `WW_Bedarf`
beim Speichern verloren gegangen. Beides wird als eigener Fehler untersucht (Gebäudedialog und
Kopierweg verlieren `WW_Bedarf`, gegebenenfalls weitere Spalten). Auf den Vergleich der Raumwärme hat
das keine Wirkung; in der Projektwirkung fehlt dem betroffenen Projekt der Warmwasseranteil dieses
Gebäudes.

## 5 Projektwirkung (zweiter Durchgang)

24 Projekte, alle auf beiden Wegen mit Heizwärme (Minimum / Median / Maximum):

| Größe | Wert |
|---|---|
| Δ Heizwärme je Projekt | 0,0 / 30,5 / 45,6 % |
| Δ Wärmelast (Spitze des Heizkanals, keine Normheizlast) | −34,2 / 18,1 / 80,8 % |
| Restwärme über alle Projekte | 477,6 → 873,5 MWh; Projekte mit Restwärme > 1 MWh: 10 → 10 |
| Deckung Wärmepumpe (18 Projekte) | −11,5 / −2,8 / +2,7 Prozentpunkte |
| Deckung Kessel (19 Projekte) | −18,4 / 0,0 / +8,1 Prozentpunkte |
| Deckung BHKW (6 Projekte) | −8,3 / +3,0 / +18,4 Prozentpunkte |
| Deckung Solarthermie (5 Projekte) | −1,3 / 0,0 / 0,0 Prozentpunkte |

Erklärt: Die Projektwirkung folgt der Heizwärme. Der Zuwachs der Restwärme entsteht fast ganz in vier
Projekten mit demselben großen Mehrfamilienhaus, deren Erzeuger schon auf dem alten Weg nicht
reichen (Restwärme dort +77 bis +118 MWh, Deckung der Wärmepumpe −4,6 bis −11,5 Prozentpunkte). Wo
ein BHKW vor dem Kessel steht, übernimmt es den Mehrbedarf (bis +18,4 Prozentpunkte); wo die Spitze
stark steigt, wächst der Kesselanteil (bis +8,1). Das Projekt mit Verbrauchsangabe behält seine
Jahreswärme (Δ 0,0 %), seine Spitze sinkt (−34,2 %, anders verteilt über den Tag), die Restwärme
fällt. Ein Projekt mit einem externen Heizlastgang steigt nur um 13,3 %; sein absoluter Zuwachs
gleicht dem seines Gebäudes. Bei Gebäude B fehlt der Warmwasseranteil (Abschnitt 4).

## 6 Stand und Offenes

- **Bedingung (1) des Ablösekriteriums ist für den Bestand dieses Rechners erfüllt**
  (Momentaufnahme vom 25.09.2026, Schemastand 142): keine rote Zeile, 25 Gebäude vom Werkzeug
  erklärt, die zwei gelben vom Anwender bestätigt. Zusammen mit den Referenzprojekten (Basen R12 bis
  R16, 1040 nach A15 ausgenommen) sind alle Projekte gerechnet und erklärt.
- Offen beim Anwender: die Korrektur des Katalogsatzes von Gebäude A. Der Verlust von `WW_Bedarf`
  (Abschnitt 4) ist behoben, siehe Nachtrag. Andere Rechner des Anwenders kommen mit demselben Werkzeug dran.
- **Schemastand bei einer Wiederholung:** `vergleich` und `variante` verlangen die Zielversion des
  eigenen Stands. Seit dem Schemaschritt 143 braucht ein Werkzeug vom aktuellen Kopf eine
  Arbeitsdatenbank auf 143 — also erst ein Programmstart mit einem Build dieses Stands, dann die
  Aufnahme.
- Bedingungen (2) Feldphase und (4) Ausbauprobe bleiben offen; (3) ist erfüllt.

## Nachtrag: Verlust von `WW_Bedarf` behoben

Ursache war die Altregel `Stand.WwBedarf = 0` in `GebaeudeArbeitsstand.Ableiten()` (OK-Weg des
Gebäudedialogs, eingeführt mit G1 W5, verbreitert mit #465 und G3-D2). Behoben mit `bb876a21`
samt Rundlauf-Test aller Spalten (`GebaeudeRundlaufTests`) und `b9c26b88` (ein nicht berührter
Heizkurven-Vorschlag fällt beim Abschalten des Heizkreises weg); gepusht als `fc0b7e5c`, CI grün im
Lauf `36164923229`. In der Arbeitsdatenbank waren genau drei Zellen betroffen (der Katalogsatz der
Bauform mit korrigierter Bauweise: `WW_Bedarf`; die Projektkopie von Gebäude B: `WW_Bedarf` und
`Heizkurve_Aktiv`); der
Anwender hat die Reparatur beauftragt, sie ist mit Sicherung vorher durchgeführt. Der Vergleich der
Raumwärme bleibt davon unberührt.

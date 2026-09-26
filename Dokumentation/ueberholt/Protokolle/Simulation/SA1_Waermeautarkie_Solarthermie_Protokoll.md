# Auftrag SA-1 — Wärme-Autarkie bei Solarthermie im Ergebnisreiter

Stand: 26.09.2026 · Zweig `ios_migration_september` · Anlass: Anwender 26.09.2026 „Die
Simulation soll im Falle Solarthermie analog PV für die Wärme eine Autarkie erstellen; diese
ist gegenwärtig leer."

**Kein Schemaschritt, kein Eingriff in den Rechenweg der Simulation.** Kein Referenzprojekt
führt Solarthermie in der Kaskade; der Referenzlauf bleibt innerhalb der Toleranz (Kapitel 5).

## 1. Befund — warum der Reiter leer war

Der Reiter „Autarkie Analyse" der Simulationsseite kannte nur die Stromseite: Monatsstapel
„Energie-Bedarf & Deckung" aus PV-Direktverbrauch, Batterie und Netzbezug. Ein Projekt mit
Solarthermie und ohne PV bekam ein leeres Strombild; für die Wärme gab es weder eine
Aggregation noch ein Bild. Die Ergebnisreihen, die eine Wärme-Autarkie tragen, lagen im Lauf
bereits vor (Wärmebedarf der Wärmekanäle, Direktdeckung und Speicheranteil der Solarthermie),
wurden aber nirgends zu Monaten zusammengefasst.

Nebenbefund an der Solarthermie-Kachel (Deckung, thermischer Nutzungsgrad, CO₂-Anteil): Sie
rechnete die Deckung als Minimum aus Solarertrag und Restbedarf an der Kaskadenposition; die
Speicherladung zählte als Deckung, und der Nenner war nicht der ganze Wärmebedarf. Kachel und
neues Bild hätten verschiedene Zahlen gezeigt.

## 2. Reihen

`EPOS.Kern/Allgemein/Simulation/SolarWaermeMonate.cs` summiert je Kalendermonat (365 Tage,
kein Schaltjahr):

| Reihe | Herkunft |
|---|---|
| Wärmebedarf | Summe der Wärmekanäle des Laufs |
| Solarthermie (direkt) | Direktdeckung der Solarthermie |
| Solarthermie (Speicher) | Speicheranteil der Solarthermie (nur, wenn vorhanden) |
| Deckungslücke | Bedarf minus beide Solaranteile, nach unten auf 0 geklemmt |

Dazu der solare Deckungsanteil für Jahr und Monat. Die Jahressumme der beiden Solaranteile
ist die Wärmedeckung Solar der Übersicht.

## 3. Umsetzung

- **Kern:** `SolarWaermeMonate` (Aggregation), `EPOS.Kern/Allgemein/Bericht/WaermeAutarkieBild.cs`
  (Monatsstapel „Wärmebedarf & Deckung [kWh]"), ChartProbe `waerme_autarkie_monate`.
- **Oberfläche:** Reiter „Autarkie Analyse" mit Bild `simerg-waermemonate`; Reihen
  „Solarthermie (direkt)", „Solarthermie (Speicher)" (nur mit Speicheranteil) und
  „Deckungslücke (Kessel/übrige Erzeuger)" in Grau. Sichtbarkeit in `AutarkieDaten`: nur
  Solarthermie → nur das Wärmebild, PV und Solarthermie → beide Bilder untereinander, nur PV
  → das Strombild. Hülle `SimulationErgebnisHuelle.Bilder.cs`.
- **Ressourcen:** vier Schlüssel in beiden Sprachen, Designer nachgezogen;
  `CHART_ACHSE_ENERGIEBEDARF_DECKUNG` ohne „(kWh)" — die doppelte Einheit im Titel ist weg.
- **Bericht:** kein Berichtsbild; Ausnahme in `VorlagenfeldAbdeckungWacheTests` (kein
  Katalogschlüssel, nicht im Bericht).

## 4. Kachelangleichung

Die Solarthermie-Kachel liest dieselben Reihen wie das Bild: Deckung = (direkt + Speicher) /
ganzer Wärmebedarf; Nutzungsgrad und CO₂-Anteil auf derselben Grundlage. „Nicht benötigt"
erscheint nur noch ohne Wärmebedarf. Die Kachelzahlen ändern sich damit für Projekte mit
Solarthermie — vom Anwender zu bestätigen (Kapitel 7).

## 5. Tests und Gate

- `EPOS.Kern.Tests/SolarWaermeMonateTests` (synthetische Reihen und Lauf des Projekts 1026),
  vier bunit-Fälle der Sichtbarkeit in `EPOS.UI.Tests/Seiten/GangUndErgebnisReiterTests`.
- Gate auf `78039320e`: Kern-Filter Release 0 Fehler; voller Lauf 0 Fehler
  (15 965 erfolgreich, 2 übersprungen — EPOS.Kern.Tests 8 318, EPOS.UI.Tests 6 685, KiKern.Tests
  549, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27); ChartProben 220 Bilder, 0 Verstöße;
  Referenzlauf der sechs CI-Projekte (1030, 1007, 1017, 1045, 1046, 1047) gegen
  `2026-09-26_R21_BhkwDeckung`: **PASS**, 2 208 587 Werte innerhalb der Toleranz, alle CSV
  byte-gleich (außer `protokoll.txt`); Windows-Schale 0 Fehler; Designer unverändert (12 500
  Einträge, +0, CRLF).

## 6. Analyse: Vorlauf und Rücklauf der Solarthermie (ohne Codeänderung)

**Befund:** Die Felder Vorlauf- und Rücklauftemperatur der Kollektorfelder wirken nicht auf
den Ertrag.

- `SimulationSolarthermie.cs:246` setzt die Speichertemperatur fest auf `tStorage = 50`, die
  Leitungsverluste fest auf 0,92;
- `Kollektorfelder_Lesen` liest Vorlauf und Rücklauf gar nicht;
- **Nebenwirkung:** Solarthermie steht in `ProjektPuffer.WAERMEERZEUGER_TYPEN` und zieht
  damit die Systemvorgabe des Puffers MIN(VL)/MAX(RL) herunter bzw. herauf;
- die Werte kommen als Vorbelegung aus `Tab_Solarkollektoren_STAMM` (W6-E-4).

**Drei Wege (Anwenderentscheid):**

1. **Wirksam machen:** mittlere Kollektortemperatur T_m aus der Senke bzw. (VL + RL) / 2 mit
   Rückfall 50 °C; ½–1 Tag; keine Einfrierfolge, weil kein Referenzprojekt Solarthermie führt.
2. **Entfernen:** die zwei Katalogspalten samt Feldern streichen.
3. **Entkoppeln:** Solarthermie aus der Systemvorgabe des Puffers nehmen, Beschriftung und
   Kommentar korrigieren (Felder als Auslegungsangabe ohne Rechenwirkung).

**Nebenbefund:** Die Aperturfläche im `SolarkollektorenDialog.razor` (Zeile 498,
`Gesamtflaeche`) wird ungerundet angezeigt („58,1999999999").

## 7. Offen

- Monatsdeckung in Prozent wird noch nicht angezeigt;
- Kachel „Speichernutzen" nur für PV;
- Strom-Monate rechnen mit 730 h je Monat, die Wärme-Monate mit Kalendermonaten;
- Farbe der Deckungslücke grau statt rot — Anwenderentscheid;
- geänderte Kachelzahlen der Solarthermie vom Anwender bestätigen lassen;
- Solarthermie Vorlauf/Rücklauf: Weg 1, 2 oder 3 — Anwenderentscheid;
- Rundung der Aperturfläche im Solarkollektoren-Dialog;
- `Werkzeuge/ResourceDesigner/designer_neu.py` schreibt LF statt CRLF — Werkzeug nachbessern.

## 8. Nachtrag #552: Vorlauf und Rücklauf entfernt (Schemaschritt 150)

Statuszeile #552 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md). Anwenderentscheid
26.09.2026 zu Abschnitt 6: **Weg 2** („Katalogspalten VL/RL entfernen — keine Funktion“).

- **Schemaschritt 150** (`f19b246d1`, Quelle `SolarkollektorTemperaturen`): `Vorlauf` und `Ruecklauf` an
  `Tab_Solarkollektoren_STAMM` und `Tab_Solarkollektoren` per `DROP COLUMN`, kein DML, kein Tabellenneubau; beide
  Tabellen bleiben STRICT. Dieselbe Quelle bedient Migration, Werkzeug `Testdatenbankschema` und Testvorrichtung.
- **Katalog** (`5e6402ecf`): Editor, Katalogbrowser-Profil, Aufklapper-Speicherweg, Hüller, Modell, Stamm- und
  Projekt-Controller (Insert, Update, Import-Update, Kopie ins Projekt), Importvergleich, `KatalogRegistry`,
  `ParameterVerwendung` und KI-Feldtafeln ohne die Spalten; sechs Ressourcenschlüssel entfernt. Wiki-Quelle
  Gerätekataloge ohne die Zeile (`2603dc3a8`).
- **Über den Wortlaut hinaus** (einzeln zurücknehmbar): (1) der Kollektordialog der Anlage führt die Felder nicht mehr
  und schreibt sie nicht; (2) `AnlagenTemperaturen.AusStammsatz`/`AusGeraetekopie` ohne Solarzweig (Kessel und BHKW
  bleiben), neue Auskunft `FuehrtTemperaturpaar`; (3) die Systemvorgabe eines neuen Puffers läuft über
  `ProjektPuffer.SYSTEMVORGABE_TYPEN` ohne Solarthermie (`WAERMEERZEUGER_TYPEN` unverändert); (4) Erzeugerkarte und
  Hydraulikbild lesen ein stehengebliebenes Solar-Paar nicht mehr — kein Chip, keine W3-Warnung.
- **Nebenbefund behoben:** die Aperturfläche im Kollektordialog steht auf „0.##“ in der Kultur des Anwenders.
- **Testdatenbank:** nach dem Merge mit origin (#546) auf deren Fassung `979fe89c` (Stand 149) inkrementell
  nachgezogen (`8857c5be1`) → `6ce7ddfa…`, 70 684 672 Byte, Stand 150; Zellvergleich über 10 895 378 Zellen, einzige
  Abweichungen `SchemaVersion` und die vier Spalten (Werte 0, einmal NULL); `integrity_check` ok,
  `foreign_key_check` leer, zweiter Lauf 0/0. Keine Einfrierregel berührt, keine Neueinfrierung; Referenzlauf der
  sechs CI-Projekte gegen R21 PASS, alle CSV byte-gleich.
- Tests: `SolarkollektorTemperaturenTests` (Schritt auf Vorzustand, übrige Zellen gleich, wiederholbar; Systemvorgabe;
  Karte und Hydraulikbild; Werkzeug-Wache), bunit ohne die Felder, Rundung de/en.
- **Anwenderentscheid 26.09.2026 („#552: bestätigt“):** die Punkte (1)–(4) bleiben, nichts wird zurückgenommen.
- **Offen:** die alte Pendelspeicher-Migration übergeht ein Solar-Paar der Anlagenzeile — prüfen.

## 9. Nachtrag #554: Monatsdeckung in Prozent und Speichernutzen Wärme

Statuszeile #554 (`74e8229a7`), erledigt Abschnitt 7, Punkte 1 und 2. Unter dem Monatsstapel „Wärmebedarf &
Deckung“ steht die solare Deckung je Kalendermonat als Herleitungszeile (`SolarWaermeMonate.Deckungszeile`, Monat ohne
Bedarf „–“). Die Ergebniskachel nennt mit Solarthermie den solaren Speicheranteil („Speichernutzen Wärme: … kWh/Jahr“),
mit PV und Solarthermie beide Zeilen; Sichtbarkeit wie die Bilder. Ressourcen `SIM_ANZEIGE_SPEICHERNUTZEN_WAERME` und
`SIM_ANZEIGE_WAERME_DECKUNG_MONATE` in beiden Sprachen. Die Definition der Solarthermie-Kachel (Deckung am ganzen
Wärmebedarf, direkt oder über den Speicher) und die graue Deckungslücke stehen im Kommentar und in der Wiki-Quelle
Simulation. Kein Rechenweg geändert. **Offen:** die Testdatenbank führt kein Referenzprojekt mit deckender
Solarthermie — Vorschlag eines solchen Projekts; `designer_neu.py` und LF (Abschnitt 7) bleibt offen.

## 10. Nachtrag #557: Kollektorertrag und Puffer-Kopplung

Statuszeile #557 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md). Anlass: Rückfrage des
Anwenders zum Projekt 1067 „Test: Wärmeganglinie+ST“ — der Reiter Solarthermie wies einen hohen Kollektorertrag, die
Übersicht aber eine Wärmebedarfsdeckung von nur 7,99 % aus. Analyse ohne Codeänderung am Rechenkern; die Tafel des
Reiters ist umgestellt (Abschnitt 10.6).

### 10.1 Konfiguration

- Wärmebedarf aus einer Ganglinie (Kirche), 65,43 MWh/a, Bedarf nur in der Heizzeit (Juni bis August null).
- Solarthermie: 116,4 m² Flachkollektor; Kaskade Solarthermie → Heizkessel 22 kW; Spitzenlast rund 25 kW.
- Pufferspeicher 3000 l ohne eigenes Temperaturpaar (Vorlauf/Rücklauf leer), `Schwelle_Aus_Nachrang` leer.

### 10.2 Monatsbilanz (MWh)

| Monat | Bedarf | Kollektor brutto | davon genutzt | Überschuss | Kessel | ungedeckt |
|---|---:|---:|---:|---:|---:|---:|
| Jan | 12,08 | 0,87 | 0,48 | 0,39 | 10,44 | 1,27 |
| Feb | 10,86 | 1,45 | 0,68 | 0,77 | 8,93 | 1,31 |
| Mär | 9,41 | 4,23 | 1,42 | 2,81 | 7,43 | 0,64 |
| Apr | 5,91 | 4,53 | 1,01 | 3,52 | 4,80 | 0,19 |
| Mai | 1,63 | 7,54 | 0,39 | 7,15 | 1,33 | 0,01 |
| Jun | 0 | 9,22 | 0,04 | 9,18 | 0,06 | 0 |
| Jul | 0 | 10,05 | 0,04 | 10,01 | 0,06 | 0 |
| Aug | 0 | 10,75 | 0,04 | 10,71 | 0,06 | 0 |
| Sep | 1,41 | 4,45 | 0,10 | 4,35 | 1,40 | 0 |
| Okt | 4,21 | 3,41 | 0,58 | 2,83 | 3,67 | 0,06 |
| Nov | 7,94 | 0,57 | 0,24 | 0,33 | 7,25 | 0,53 |
| Dez | 11,98 | 0,70 | 0,42 | 0,28 | 10,18 | 1,44 |
| **Jahr** | **65,43** | **57,76** | **5,44** | **52,33** | **55,61** | **5,45** |

Die Jahressummen der Spalten sind aus den gerundeten Monatswerten gebildet (brutto aus den Monaten 57,77). Genutzt wird
auch in den Sommermonaten ohne Bedarf ein kleiner Rest (0,04 MWh je Monat) — er geht in den Puffer und dessen Verluste;
die Wärmebedarfsdeckung von 7,99 % bezieht sich auf den Bedarf.

**Bruttoertrag geprüft:** 57,76 MWh (496 kWh/m²) — unabhängig aus Einstrahlung und Kollektorkennlinie nachgerechnet
55,7 bis 59,4 MWh. Der Kollektorertrag ist richtig.

### 10.3 Übergangstag 24.04.

Kollektor brutto 423 kWh, davon genutzt 14,2 kWh, Überschuss 409 kWh, Wärmebedarf des Tages 29,8 kWh. Der Puffer
steht bei 32,9 kWh Inhalt bei einer Kapazität von 34,8 kWh — er ist voll, bevor die Sonne kommt.

### 10.4 Obergrenzen der Deckung

Was die Solarthermie bei idealer Speicherung höchstens decken könnte (Minimum aus Ertrag und Bedarf, je Zeitfenster
aufsummiert):

| Ausgleich über | stündlich | täglich | wöchentlich | monatlich |
|---|---:|---:|---:|---:|
| gedeckt (MWh/a) | 5,07 | 11,28 | 16,58 | 18,79 |
| Anteil am Bedarf | 7,7 % | 17,2 % | 25,3 % | 28,7 % |

42,3 MWh des Bruttoertrags fallen in Stunden ohne Wärmebedarf. Ein idealer Tagesspeicher käme auf höchstens 17 %,
ein Monatsspeicher auf 29 %.

### 10.5 Ursache und Varianten

Zwei Gründe, beide aus den Eingaben, kein Kernfehler:

1. **Bedarf und Ertrag liegen gegeneinander** (Abschnitt 10.2 und 10.4): der Bedarf fällt in die Heizzeit, der Ertrag
   in den Sommer.
2. **Der Puffer ist klein gerechnet und vom Kessel belegt.** Ohne Temperaturpaar rechnet der Puffer mit dem Rückfall
   ΔT = 10 K, bei 3000 l also 34,8 kWh (`EPOS.Kern/Allgemein/Simulation/SimulationPufferspeicher.cs:116`). Mit leerer
   `Schwelle_Aus_Nachrang` lädt der nachrangige Kessel den Puffer bis zur Abschaltschwelle von 95 %
   (`EPOS.Kern/Allgemein/Simulation/Ladeordnung.cs:522` und `:548`, `EPOS.Kern/Allgemein/Simulation/WaermesenkeClass.cs:741`).
   Die Solarthermie findet den Puffer voll und kommt nur in Höhe des Momentanbedarfs durch
   (`EPOS.Kern/Allgemein/Simulation/SimulationSolarthermie.cs:545` ff.).

Varianten auf einer Kopie des Projekts:

| Variante | Wärmebedarfsdeckung |
|---|---:|
| wie eingegeben | 7,99 % |
| Puffer Vorlauf/Rücklauf 55/30 °C | 8,14 % |
| `Schwelle_Aus_Nachrang` 30 % | 10,28 % |
| beides | 13,34 % |

**Einordnung:** Die Simulation rechnet richtig; die Deckung ist eine Folge der Bedarfsganglinie und der
Puffer-Kopplung. Die Grenze eines Tagesspeichers (17 %) erreicht auch die beste Variante nicht ganz.

**Empfehlung an den Anwender (Projekt 1067):** Vorlauf und Rücklauf am Puffer eintragen, die Nachrang-Schwelle auf
etwa 30 % setzen und das Kollektorfeld an der Winterlast statt am Jahresbedarf auslegen.

### 10.6 Umstellung der Tafel

Die Tafel des Reiters Solarthermie (`EPOS.UI/Seiten/Simulation/SolarthermieReiter.razor`) zeigt: Wärmebedarf,
Kollektorertrag brutto, davon genutzt, Überschuss, Restwärmebedarf, Wärmebedarfsdeckung. Der Bruttowert ist die
Summe der zwei gerundeten Teile, so stimmt die Tafel in sich. Ressourcen `SIMERG_LBL_SOLAR_BRUTTO` und
`SIMERG_LBL_SOLAR_GENUTZT` (de/en) neu, `SIMERG_LBL_GESAMTLEISTUNG_MODULE` entfernt; Tests in
`EPOS.UI.Tests` (`ErzeugerReiterTests`); Absatz in der Wiki-Quelle Simulationsergebnisse. Kern und DTO unverändert.
Logbuch-Satz unter Version 1.2.0.5: „Der Reiter Solarthermie zeigt den Kollektorertrag brutto mit den Teilen genutzt
und Überschuss.“

### 10.7 Offen

- **Warnkriterium** „Solarthermie lädt einen Puffer, den ein nachrangiger Erzeuger voll hält“ (½ Tag) —
  Anwenderentscheid.
- **Standard der Nachrang-Schwelle** unter der Abschaltschwelle, wenn Solarthermie am Puffer hängt (Rechenwegänderung,
  kein Referenzprojekt betroffen; ½–1 Tag) — Anwenderentscheid.
- Spalte „Wärmeproduktion“ der Modultabelle: genutzt oder brutto ausweisen?
- Schreibung „Überschuß“ in der Oberfläche gemeinsam mit der Photovoltaik vereinheitlichen.
- **Kessel-Nebenbefund**, nicht untersucht: Gasverbrauch bleibt bei rund 66–67 MWh, obwohl die Kesselwärme in den
  Varianten von 55,6 auf 51,5 MWh sinkt (Nutzungsgrad 0,84 → 0,78); der Kessel mit 22 kW bei rund 25 kW Spitze lässt
  5,45 MWh ungedeckt.

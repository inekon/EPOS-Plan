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

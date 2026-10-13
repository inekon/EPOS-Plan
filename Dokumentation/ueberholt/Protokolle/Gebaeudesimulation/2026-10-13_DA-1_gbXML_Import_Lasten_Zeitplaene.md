# Protokoll DA-1 — gbXML-Import von Lasten und Zeitplänen

Welle DA-1 aus dem [Konzept Datenaustausch der Simulationsdaten](../../../aktuell/Gebaeudesimulation/2026-10-12_Konzept_Datenaustausch_Simulationsdaten.md)
(Abschnitte 2, 3.1, 3.3, 4.1, 7). Grundlage ist Entscheid E138 (Anwender 13.10.2026, „nach Empfehlung“): F2 Datei vor Profil,
das Profil füllt nur fehlende Größen, Abweichungen als Hinweis; F3 nicht abbildbare Zeitpläne verdichten, den Rest benannt
verwerfen, die Datei nicht ablehnen; F5 Rundlauf je DA-Welle (der Rundlauf Export → Import folgt mit DA-2).

## Befund

Der gbXML-Leser las je Raum `PeopleNumber`, `LightPowerPerArea`, `EquipPowerPerArea`, `AirChangesPerHour` und an der Zone
`DesignHeatT`/`DesignCoolT`. `GebaeudeAggregation` fasste die Lasten zum Gebäudemittel zusammen. Zeitpläne (`Schedule`-Kette),
`PeopleHeatGain`, Außenluft und der Luftwechsel der Zone blieben ungelesen. Für die Konditionierung je Zone gab es schon einen
formatfreien Schreibweg: `Zonenkonditionierung` → `ZonenplanCtrl.ProjektdateiUebernehmen`, mit Profil zuerst und Datei danach.
Ihn nutzen die HottCAD-Projektdatei und die IFC-Sätze `EPOS_*`.

## Erweiterungspunkt (Schritt 1 von DA-1)

Die Schemakopie 6.01 liegt nicht im Repositorium (`Referenzlaeufe/Schemakopien/` trägt nur die `LIESMICH.md`), und das Netz ist
gesperrt. Die Tabelle gibt das bekannte Schema 6.01 aus dem Gedächtnis wieder. **Jede Zeile ist eine Annahme**, bis Probe 3 lokal
mit der Kopie läuft.

| Element / Attribut | Ort | Inhalt, Einheit | im Import |
|---|---|---|---|
| `PeopleNumber` (`unit` = `NumberOfPeople`, `SquareMPerPerson`, `SquareFtPerPerson`) | `Space` | Personen bzw. Fläche je Person | gelesen (Bestand) |
| `PeopleHeatGain` (`unit` = `W`, `KW`, `BtuPerHour`; `heatGainType` = `Total`, `Sensible`, `Latent`) | `Space` | Wärme je Person | neu |
| `LightPowerPerArea`, `EquipPowerPerArea` (`WattPerSquareMeter`, `WattPerSquareFoot`) | `Space` | Leistung je Fläche | gelesen (Bestand), jetzt je Zone |
| `AirChangesPerHour` | `Space` | Infiltration [1/h] | Bestand (Gebäudemittel) |
| `AirChangesPerHour` | `Zone` | Nutzerlüftung [1/h] | neu |
| `OAFlowPerPerson` (`CubicMPerHr`, `CubicMPerMin`, `CubicMPerSec`, `LPerSec`, `LPerMin`, `CFM`) | `Space`, sonst `Zone` | Außenluft je Person | neu |
| `OAFlowPerArea` (`CubicMPerHrSquareM`, `LPerSecPerSquareM`, `CFMPerSquareFoot` und Schreibvarianten) | `Space`, sonst `Zone` | Außenluft je Fläche | neu |
| `DesignHeatT`, `DesignCoolT` | `Zone` | Sollwerte | Bestand, jetzt auch Zelle TAG |
| `conditionType` | `Space` | beheizt/unbeheizt | Bestand |
| `peopleScheduleIdRef`, `equipmentScheduleIdRef`, `lightScheduleIdRef` | `Space` | Verweis auf `Schedule` | neu |
| `heatSchedIdRef`, `coolSchedIdRef` | `Zone` | Verweis auf `Schedule` (Typ `Temp`) | neu |
| `airChangesSchedIdRef` | `Zone` | Verweis auf den Lüftungsplan (Name nach Konzept 3.1, ungeprüft) | neu |
| `Schedule/@type` (`scheduleTypeEnum`: `Temp`, `Fraction`, `OnOff`, `ResetTemp`, `ActivityLevel` …) | Wurzel | Plantyp | neu; abgebildet `Temp`, `Fraction`, `OnOff` |
| `YearSchedule` mit `BeginDate`, `EndDate`, `WeekScheduleId/@weekScheduleIdRef`, `Name` | `Schedule` | Jahresabschnitt | neu |
| `WeekSchedule/Day` mit `dayType`, `dayScheduleIdRef` | Wurzel | Tagtyp → Tagesplan | neu |
| `dayType` (`Weekday`, `Weekend`, `Mon` … `Sun`, `Holiday`, `WeekendOrHoliday`, `All`, `HeatingDesignDay`, `CoolingDesignDay`) | `Day` | Tagtyp | neu; Auslegungstage übergangen |
| `DaySchedule/ScheduleValue` | Wurzel | Werte des Tages (24 oder Vielfaches) | neu |

**Befund Erweiterungspunkt:** Nach dem bekannten Aufbau von 6.01 haben `Space`, `Zone` und die Zeitplanelemente keinen
Platzhalter `xs:any` oder `xs:anyAttribute`. Eine Fremderweiterung im Namensraum `urn:epos-plan:gbxml:1` (Weg 1 aus Konzept 3.3)
wäre damit nicht schemagültig. Als Rückfall für DA-2 gilt Weg 2: ein Block `EPOS:` mit Schlüssel=Wert in `Description` bzw.
`DocumentHistory`. Der Auftrag nennt `Name`/`CADObjectId` als Träger; Konzept 3.3 schließt beide aus. Der Widerspruch ist
benannt, gebaut ist nichts. Mit der Schemakopie ist der Befund vor DA-2 zu bestätigen.

## Ergebnis

- **Leser** (`EPOS.Kern/Allgemein/Import/Gbxml/GbxmlZeitplan.cs`, `GbxmlLeser.cs`): Er liest die `Schedule`-Kette vollständig in
  `GbxmlZeitplan` (Typ, Name, Jahresabschnitte mit Beginn und Ende im Gemeinjahr, je Abschnitt `GbxmlWochenplan` mit dem Tagesbild
  je Tagtyp, wie geschrieben). Damit kommt ein exportierter `Schedule` mit Perioden, Wochentagen und Stundenwerten ohne Verlust
  zurück (F5). Temperaturpläne stehen in °C (Annahme: Einheit aus `gbXML/@temperatureUnit`). Je Raum entsteht `GbxmlRaumlasten`
  (`AbbildRaum.GbxmlLasten`): Wärme je Person, Außenluft je Person und je Fläche, Luftwechsel der Zone und die sechs Pläne. Neue
  Einheiten stehen in `GbxmlEinheiten` (`Leistung`, `Volumenstrom`, `VolumenstromJeFlaeche`).
- **Abbildung** (`GbxmlKonditionierung.cs`): Sie bildet je Zone eine `Zonenkonditionierung` mit Herkunft
  `Konditionierungsherkunft.GbxmlDatei`. Der Zonenplan setzt sie, wo keine `EPOS_*`-Sätze vorliegen (Mehrzonenweg). Im
  Einzonenweg liefert `GebaeudeImportAblauf.Gebaeudekonditionierung` sie ohne Projektdatei als Gebäudekalender. Geschrieben wird
  über den Bestandsweg: Profil zuerst, dann die Datei je Größe (F2). Neu ist `Groessenkonditionierung.NennwertDatei`: Ein Nennwert
  ohne Plan setzt sich auf den Kalender der Profilkopie, dessen Zeitplan bleibt; ohne Profilkalender gilt „immer 1“.
- **Befund im Protokoll**: je Zone die Zeile `ZONE_LASTEN` (Lastwerte, Zeitpläne, verdichtet, verworfen), dazu die Zeilen der
  Verdichtung, der Verwürfe und der Profilabweichung. Sie stehen in den Meldungen des Bauteilvorschlags, also im bestehenden
  Importprotokoll. Die Hülle ist unberührt.
- **Rechenweg, Schema, Referenzprojekte**: unberührt, kein Schemaschritt, kein SQL.

### Abbildung Inventar → gbXML → EPOS

| Nr | Inventar | gbXML | EPOS | Regel |
|---|---|---|---|---|
| B6 | Heizsollwert Tag | `DesignHeatT` | Zelle HEIZSOLL/TAG (Zone: `Raumsolltemperatur_Tag` wie bisher) | flächengewichtet, 0,1 K |
| B7, B8 | Nacht, Wochenende, Ferien | `heatSchedIdRef` → `Temp` | Kalender HEIZSOLL (Woche, Perioden) | stündlich, ohne Verdichtung auf Zeilen |
| B9 | Kühlsollwert Tag, Nacht | `DesignCoolT`, `coolSchedIdRef` | Zelle KUEHLSOLL/TAG, Kalender KUEHLSOLL | wie B6/B7 |
| B13 | Nutzerluftwechsel | `Zone/AirChangesPerHour`, `airChangesSchedIdRef` | Zelle LUEFTUNG/TAG [1/h], Kalender LUEFTUNG | Anteil × Nennluftwechsel |
| B14 | Außenluft je Person und Fläche | `OAFlowPerPerson`, `OAFlowPerArea` | Zelle LUEFTUNG/TAG (Σ m³/h ÷ Volumen) | nur ohne Luftwechsel der Zone; kein Feld je Person (Hinweis) |
| B15 | Belegung | `PeopleNumber` | Nennwert PERSONEN | Zahl, sonst Fläche ÷ Fläche je Person |
| B16 | Wärme je Person | `PeopleHeatGain` | Nennwert PERSONEN = Σ Zahl × Wärme | ohne Angabe 70 W; latent verworfen |
| B17 | Geräte | `EquipPowerPerArea`, `equipmentScheduleIdRef` | Nennwert GERAETE, Anteil | Leistung × Fläche |
| B18 | Licht | `LightPowerPerArea`, `lightScheduleIdRef` | in GERAETE addiert, mit eigenem Plan gewichtet | wie B17 |
| C1, C2 | Kalender Heiz-, Kühlsoll | `Schedule` `Temp` | `HEIZSOLL`, `KUEHLSOLL` | Standardwoche und Perioden |
| C3 | Kalender Lüftung | `Schedule` `Fraction`/`OnOff` | `LUEFTUNG` | Anteil × Nennluftwechsel |
| C4, C5 | Kalender Geräte, Personen | `Schedule` `Fraction`/`OnOff` | `GERAETE`, `PERSONEN` mit Nennwert | nennwertgewichteter Anteil |
| C6 | Feiertage | `dayType` `Holiday`/`WeekendOrHoliday` | neun Feiertagsregeln mit eigener Woche | Datum aus der Regel des Kerns |
| C7 | Betriebspause, Zeitraum | `YearSchedule`-Folge | Perioden `BETRIEBSPAUSE` (alles aus), `FERIEN` (Name), `ZEITRAUM` | Rang ab 310 |
| A10 | Ferien | `YearSchedule` mit Ferienname | Periode `FERIEN` | Name enthält Ferien, Urlaub, Holiday oder Vacation |

**Zählung je Inventargruppe** (gelesen / abgebildet / verdichtet / verworfen):

- **B (22):** 10 gelesen (B6, B7, B8, B9, B13, B14, B15, B16, B17, B18); 10 abgebildet. Verdichtet werden B14 (die Angabe je
  Person geht in den Luftwechsel über) und B18 (Licht geht in die Geräte). Verworfen wird nur der latente Anteil von B16.
- **C (10):** 6 gelesen (C1–C7 ohne C6-Regel); alle 6 abgebildet. Verdichtet wird C6: Feiertage sind die neun Regeln des Kerns,
  die Datei kennt kein Datum. Verworfen werden Auslegungstage und Typen außerhalb `Temp`/`Fraction`/`OnOff`. C8, C9 und C10 haben
  kein gbXML-Element.
- **A (10):** A10 über C7 abgebildet; A8 ist nicht als Feld übernommen (die Wochenendmaske folgt den Tagtypen nur im Kalender).

### Verdichtungsregeln (F3)

1. **Zeitschritt:** Ein Tagesplan mit 24·k Werten wird auf Stundenmittel verdichtet (`PLAN_ZEITSCHRITT`). Eine andere Zahl
   verwirft den Tagesplan (`PLAN_STUNDEN`). Ohne Tagesbild für einen Wochentag verwirft der Leser den Plan (`PLAN_WOCHE_LUECKE`).
2. **Mehrere Räume einer Zone:** Alle Abschnittsgrenzen aller Pläne kommen auf eine gemeinsame Teilung. Je Teilstück gilt die
   gewichtete Woche: Anteile nach Nennwert, Temperaturen nach Fläche. Ein Raum ohne Plan zählt mit Anteil 1
   (`PLAN_TEILWEISE`). Verschiedene Pläne heißen `VERDICHTET_RAEUME`.
3. **Standardwoche und Perioden:** Die Woche mit den meisten Tagen wird Standardwoche, die übrigen Teilstücke werden Perioden.
   Tage ohne Abschnitt nehmen den längsten Abschnitt (`TAGE_OHNE_ABSCHNITT`). Mehr Perioden als 64 (55 neben den Feiertagen):
   Die kürzesten fallen weg (`PLAN_PERIODEN`).
4. **Feiertage:** Das Tagesbild `Holiday` des Hauptabschnitts wird zur Woche der neun Feiertagsregeln (Rang 100–108, unter den
   Perioden). Ein Plan ohne eigenes Bild nimmt seinen Sonntag (`FEIERTAG_SONNTAG`).
5. **Grenzen der Größe:** Ein Heizsollwert unter 0 °C wird „aus“, ein Kühlsollwert über 35 °C ebenso. Alle übrigen Werte werden
   auf die Grenzen der Kalendertabellen begrenzt, also Heizen 0–30 °C, Kühlen 15–35 °C, Lüftung 0–20 1/h und Anteile 0–1
   (`WERTE_BEGRENZT`).
6. **Typ passt nicht zur Größe** (z. B. `Temp` an Personen): Der Plan wird verworfen, es gilt das Profil (`PLAN_ART`).

### Datei vor Profil (F2)

Was die Datei liefert, ersetzt je Größe die Kopie des Profils. Fehlt in der Datei Wert und Plan einer Größe, bleibt die Größe beim
Profil. Ohne Lasten in der Datei (nur `DesignHeatT`/`DesignCoolT`) entsteht keine Konditionierung, der Profilweg von GD-5/NP
bleibt unverändert. Weicht der Nennwert der Personen oder Geräte um mehr als 5 % vom Profil ab, schreibt das Protokoll
`PROFIL_ABWEICHUNG`; es gilt die Datei.

## Dateien

- neu: `EPOS.Kern/Allgemein/Import/Gbxml/GbxmlZeitplan.cs`, `GbxmlRaumlasten.cs`, `GbxmlKonditionierung.cs`,
  `EPOS.Kern.Tests/GbxmlLastenZeitplaeneTests.cs`
- geändert: `GbxmlLeser.cs`, `GbxmlEinheiten.cs`, `Gebaeude/GebaeudeAbbild.cs`, `Gebaeude/Zonenplan.cs`,
  `Gebaeude/GebaeudeBauteilvorschlag.cs`, `Gebaeude/GebaeudeImportAblauf.cs`, `Sqproj/SqprojKonditionierung.cs`,
  `Controller/ZonenplanCtrl.cs`, beide `Resource*.resx`, `Resource.Designer.cs`
- Ressourcen: 20 neue Schlüssel je Sprache (`IMP_GBXML_KOND_*` 2, `IMP_GBXML_PROT_*` 18)

## Nachweis

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.
- `dotnet test EPOS.Kern.Tests --filter "Gbxml|GebaeudeImport"`: grün, darin 11 neue Tests (`GbxmlLastenZeitplaeneTests` 10,
  `GbxmlLastenZonenplanTests` 1). Die Probe hat zwei Räume, Temperatur-, Anteils- und An/Aus-Pläne, zwei Jahresabschnitte, eine
  Ferienwoche, ein Feiertagsbild, einen Tagesplan mit 48 Werten, einen Plan `ActivityLevel`, einen Tagesplan mit 23 Werten und
  einen Verweis ins Leere.
- Designer wiederholbar (`designer_neu.py`: unverändert, zweiter Lauf +0). SQL nicht berührt.

## Offen

- **Schemakopie 6.01**: Erweiterungspunkt, Einheitenlisten (`powerUnitEnum`, `flowUnitEnum`, Fläche je Volumenstrom), der Name
  `airChangesSchedIdRef` und die Tagtypen sind gegen die Kopie zu prüfen. Annahmen sind im Code als solche vermerkt.
- **Schemabedarf (nicht angemeldet):** Außenluft je Person an der Zone. Ein nullbares Feld `Tab_Zone.Aussenluft_Person`
  [m³/(h·P)] würde B14 ohne Umrechnung tragen; heute geht der Wert in `Luftwechsel_Nutzer` über. Die Lichtleistung je Zone
  (B18) bräuchte ein Feld oder einen eigenen Kalender, um im Rundlauf getrennt von den Geräten zurückzukommen. Ein Feld
  `Tab_Zone.Personen_Waerme` würde B16 ohne den Weg über den Nennwert halten.
- **`Bewohner` je Zone** wird nicht gesetzt; die Personen wirken über den Nennwert des Kalenders `PERSONEN`.
- **Einzonenweg:** Die Zähler des Gebäudekalenders stehen nicht im Protokoll, weil der Weg die Konditionierung erst beim
  Schreiben bildet.
- **Matrixprofil ohne Kalender:** Ein Nennwert ohne Plan bekommt dort den Kalender „immer 1“, nicht die Zeilen des Profils.
- **Der Rundlauf Export → Import** folgt mit DA-2 (F5). Das Abbild hält dafür Abschnitte, Tagtypen und Stundenwerte.

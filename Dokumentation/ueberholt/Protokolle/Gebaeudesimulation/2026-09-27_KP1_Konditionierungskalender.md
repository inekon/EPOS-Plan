# Protokoll KP1 (erste Hälfte) — Konditionierungskalender im Rechenkern

**Datum** 27.09.2026 · **Zweig** `kp1-konditionierung` (Worktree `kp1`, aufgesetzt auf
`origin/ios_migration_september`) · **Stufe** KP1 der
[Konditionierungsprofile](../../../aktuell/Konzept_Konditionierungsprofile_EPOS-Plan.md), erste Hälfte
(KP1a) · **Entscheide** E52 (N1.59) und E53 (N1.60) · **Festlegungen der Umsetzung**
[N1.61](../../../aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)

Fünf Wellen, je mit grünem Build und Proben committet; ein volles Gate am Ende.

## 1. Was gebaut ist

### Welle 1 — Schemaschritt KP-S1 (Nummer 151)

`EPOS.Kern/Allgemein/Update/KonditionierungSchema.cs` trägt Nummer, DDL, `Lesbar`, `Vollstaendig` und
`Ausfuehren` (ADR-001 Option C: EINE Quelle für Migration, `Werkzeuge/Testdatenbankschema`,
Testvorrichtung und Nachweis). Drei `STRICT`-Tabellen samt neun Indizes, **reines DDL, kein DML** (F5):

| Tabelle | Spalten | Regeln |
|---|---|---|
| `Tab_Konditionierungskalender` | 11 | Eigentümerregel als `CHECK` (genau ein Gebäude mit oder ohne Zone, ein Katalogbau oder eine Vorlage; Zone nur mit Gebäude), genau eine Angabe aus `Wert`, `Aus = 1`, `Woche`; `Nennwert` nur bei `GERAETE` und `PERSONEN` und ≥ 0 |
| `Tab_Konditionierungsperiode` | 12 | `Rang` 1…999 je Kalender `UNIQUE`, vier Arten, entweder Datum 1…365 ohne Regel oder eine der neun Feiertagsregeln ohne Datum mit `Art = 'FEIERTAG'`; genau eine Angabe aus `Wert`, `Aus`, `Woche`, `WieWochentag` |
| `Tab_Konditionierungsvorgabe` | 12 | die neuen Zellen der Matrix je Eigentümer, Größe und Zeile (P10 b); `Von`/`Bis` sind Stunde 0…23 in der Zeile `NACHT`, Tag 1…365 in `SAISON` — dort beide oder keiner (E53) — und sonst NULL; `Bedingt_K` nur an Lüftung/Nacht (P9) |

Die Kennwörter (fünf Größen, vier Arten, neun Feiertagsregeln, sechs Zeilen, `aus`) stehen als
Persistenzwerte in `DbWerte`, die Tabellennamen in `SchemaKatalog`. `SchemaStand.Zielversion` steht auf
151; `SchemaMigration` der Windows-Schale, das Werkzeug und `EPOS.Kern.Tests/TestDatenbank` bedienen sich
aus derselben Quelle. Die Testdatenbank ist nachgezogen (Schemastand **151**).

### Welle 2 — Kalendermodell und Standardfahrplan-Generator

Zwölf Klassen in `EPOS.Kern/Allgemein/Simulation/Gebaeude/Konditionierung/`, alle plattformfrei, **ohne
Datenbank**, ohne Uhr, ohne Zufall, `InvariantCulture`:

`Konditionierungsgroesse(n)` (Grenzen, Kennwörter, Bedeutung von „aus"), `Kalenderwoche` (der **eigene**
strenge Leser und Schreiber der 168-Zellen-Woche nach H8 mit Kennwort `aus`, vier Nachkommastellen und
dem Rundlauf Wert → Text → Wert bitgleich — der Leser von `Sollwertprofil` bleibt unberührt),
`Feiertage` (die neun Regeln, Osterdatum als Rechenvorschrift nach Meeus/Jones/Butcher, Abbildung
Tag/Monat → Jahrestag im Gemeinjahr), `Kalenderangabe` und `Kalenderregel` (genau eine Angabe je Ebene;
Perioden mit Rang, Art, Datum oder Regel, Jahreswechsel, `WieWochentag`), `Konditionierungskalender`
(drei Ebenen und `Auswerten(w₀, Referenzjahr) → double[8760]`, dazu `Quelle(tag)` für die Vorschau und
`TraegtAus()`), `Kalenderzeilen` und `Kalenderleser` (die DTO der drei Tabellen, der strenge Leser mit
**benanntem Befund** statt Ausnahme, Schreiber und Rundlaufprüfung), `Matrixeingang` und
`Vorgabematrix` (die wirksame Matrix aus Bestandsspalten und Vorgabezeilen nach „ein Ort je Zelle",
Kaskade Zone → Gebäude je Zelle, F2), `Standardfahrplan` (der Generator — zugleich abgeleiteter Fahrplan
und „Kalender anlegen"), `Konditionierungseingang` (die Brücke Gebäudezeile → Matrix, die erste Quelle
je Größe und die Bauvorschrift der Byte-Gleichheit).

`GebaeudeModellEingang.Bestandsfahrplan` ist seither die **eine** Stelle, an der zwischen
`Sollwertfahrplan` und `SollwertfahrplanMitProfil` entschieden wird; `Bauen` ruft sie, und die Wache ruft
sie ebenfalls.

### Welle 3 — Die fünf Reihen im Stundenmodell

`Konditionierungssatz` trägt je Größe **einen** Kalender (erste Quelle Zone → Gebäude → abgeleitet) samt
w₀ und Referenzjahr; `Konditionierungdatenweg` ist die **einzige** Stelle des Moduls mit
Datenbankzugriff (über `DataRepository` mit `?`-Parametern) und liefert `null`, wo nichts steht.

Im Eingangsbauer:

- **`ThetaSoll`** aus dem Heizkalender; „aus" ist NaN, und `Stundenrand.MitHeizung` ist dort `false` —
  der Löser rechnet die Stunde ohne Heizung, die Heizleistung der Zone ist 0 und der Kanal Raumwärme
  ebenso (E53). Kein Erzeuger wird abgeschaltet; die Zahl der Stunden steht als Hinweis im Protokoll.
- **`ThetaMax`** aus dem Kühlkalender (P7 a, P13 a); „aus" ist +∞ (E32). Dann tritt die stündliche
  Prüfung θ_K(h) ≥ θ_H(h) + 1 K an die Stelle der Prüfung gegen den höchsten Sollwert (F17).
- **Lüftung:** das **Jahresminimum** geht in R_ext, der Überschuss je Stunde als masseloser
  Zusatzleitwert, **nie negativ** (F15); der größere aus Sommerlüftung und Kalender gewinnt, und die
  Sommerlüftung wird nach dem neuen Minimum neu gebildet.
- **Geräte und Personen:** `PhiConv` und `PhiRad*` bekommen je Stunde Q_G(h) + Q_P(h) statt der
  Konstante (P1 b).
- Die **Zonenschleife** bekommt die Konditionierung als Naht `Func<long?, Konditionierungssatz>`;
  `ZonenEingang` und `Zonenrechnung` geben sie durch, `Vdi6007Rechenweg` füllt sie.

Neu: `GebaeudeModellFehler.KalenderUngueltig` und vier Ressourcenschlüssel `SIMENG_KOND_*` in beiden
`.resx` (Designer neu erzeugt).

### Welle 4 — `KonditionierungCtrl`

Lesen (`Vorgaben`, `Kalender`), `Anlegen`, `Schreiben`, `Verwerfen`, `ErneutAnwenden` (P12 a) und
`Vorgabe` — jeder Schreibweg in **einer** Transaktion; dazu `PersonenNennwertVorschlag`,
`PersonenJahresmittelW` und `GeraeteNennwertNachPersonen` für „Energie bleibt" (P1 b). Der
`Eigner`-Satz trägt die Eigentümerregel als Code, Wort für Wort wie der `CHECK`. Die Oberfläche kommt
mit KP2.

### Welle 5 — Proben des Kalendermodells

60 datenbankfreie Fälle zu Leser, Schreiber, Kompilierung, Feiertagen, Grenzen, Zeilenleser, Vererbung
und Eigentümerregel.

## 2. Schemaschritte

| Nummer | Name | Inhalt | Ergebnisneutral |
|---|---|---|---|
| **151** | KP-S1 (`KonditionierungSchema`) | drei `STRICT`-Tabellen, neun Indizes, reines DDL | ja — die Tabellen entstehen leer, kein Referenzprojekt trägt eine Zeile |

Die Nummer ist unmittelbar vor dem Schemacommit gegen `origin` geprüft (dort Zielversion 150) und beim
Merge vor dem Gate erneut.

## 3. Festlegungen der Umsetzung

Sie stehen nummeriert als Nachtrag
[N1.61](../../../aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) im Leitkonzept (sechzehn
Punkte, Muster N1.57). Die zwei mit der weitesten Folge:

1. **`ID_Vorlage` ohne Fremdschlüssel** — die Spalte steht schon in beiden Eigentümerregeln, damit KP1b
   keine Tabelle neu bauen muss; SQLite kann einem `CHECK` keine Spalte nachtragen.
2. **Der Saisonzweig des `CHECK` ist NULL-frei gebaut** — einen `CHECK` mit Ergebnis NULL lässt SQLite
   durch; ohne die ausdrückliche Prüfung käme ein Saisonstart ohne Ende hindurch.

## 4. Befunde der Umsetzung

| Befund | Ursache | Behebung |
|---|---|---|
| Ein Saisonstart ohne Ende kam durch den `CHECK` | SQLite lässt einen `CHECK` passieren, dessen Ergebnis NULL ist | Der Zweig ist NULL-frei gebaut (zweimal `IS NOT NULL`); die drei Tabellen wurden neu angelegt, sie waren leer |
| `SPALTENZAHL_VORGABE` stand auf 11 | Zählfehler beim Abfassen der DDL | auf 12 berichtigt, die Probe hält sie |
| Die Bereichsprüfung einer Vorgabezelle zog die Größengrenzen auf **jede** Zeile | Die Zeile `NENNWERT` trägt bei den Lasten einen Wattwert, nicht den Anteil 0…1 | Die Prüfung gilt je Zeile (Wattwert ≥ 0, Infiltration in den Lüftungsgrenzen, `SAISON` ohne Wertprüfung) |
| Die Wache las nur 203 statt 2 128 Vergleiche | `Tab_Gebaeude_STAMM` führt keine Spalte `Gebaeudename`; die Abfrage scheiterte und der Leser brach ab | Die Spalte ist aus der Abfrage genommen (sie gehört nicht zum Fahrplan), und der Leser überspringt eine Tabelle statt abzubrechen |
| `last_insert_rowid()` gab 0 | `DataRepository` arbeitet je Aufruf auf einer eigenen Verbindung, die Funktion gilt je Verbindung | In der Probe `MAX(ID)` statt `last_insert_rowid()` |
| **Ein dupliziertes Projekt hätte seine Kalender verloren** (`ProjektplanKinderWacheTests`) | Kalender und Vorgabezeile tragen je **zwei** Fremdschlüssel auf Plantabellen (`ID_Gebaeude`, `ID_Zone`); die Auto-Erkennung von `ProjektDuplizierenCtrl.ErmittlePlan` filtert über die **erste** Spalte — über `ID_Zone` fiele jeder Gebäudekalender weg, weil `ID_Zone` dort NULL ist | Die drei Tabellen stehen **von Hand** in `KINDER` (Konzept 5.5): Kalender und Vorgaben über `ID_Gebaeude` (die Eigentümerregel hält fest, dass am Zonenkalender `ID_Gebaeude` das Gebäude der Zone ist — ein Filter nimmt beide mit), die Perioden dreistufig über den Kalender. Katalog- und Vorlagenzeilen reisen nicht mit dem Projekt |
| Schritt 151 nannte seine Paketwirkung nicht (`ProjektpaketAnhebungTests`) | `Paketanhebung.STUFEN` muss jeden Schritt bis zum Zielstand führen | Eintrag `new Stufe(151, Art.Ddl, "Konditionierungskalender, Perioden und Vorgabezellen")` |
| `SolarkollektorTemperaturenTests` behauptete, Schritt 150 sei der Zielstand | Der Zielstand steht jetzt auf 151 | Der Fall prüft die Kette lückenlos weiter (150 + 1 = `KonditionierungSchema.SCHRITT` = `SchemaStand.Zielversion`) und ist umbenannt |

## 5. Nachweise

| Nachweis | Ergebnis |
|---|---|
| **Wache „Standardfahrplan bitgleich"** (`KonditionierungStandardfahrplanWacheTests`) | **bitgleich** (`DoubleToInt64Bits`) über **alle 304 Gebäudezeilen** der Testdatenbank (29 `Tab_Gebaeude`, 275 `Tab_Gebaeude_STAMM`) und **alle sieben Wochentage** des 1. Januar = 2 128 Vergleiche je 8 760 Stunden; dazu die Schwellen (Wochenende 4,9999/5/5,0001/0/16 °C, Ferienmerker 0,9/0,9001, Feriensollwert 0,9999/1,0/0,5 mit Merker), jede gültige Nachtzeit, jeder Ferienzeitraum samt Jahreswechsel und „0/366 = aus", vier Zeiträume zugleich, AK1 mit Wochenprofil und Ferien darüber, Fehlerbilder mit **demselben Grund** wie der Lauf |
| `KonditionierungReihenTests` | Byte-Gleichheit ohne und mit leerem Satz über alle Reihen, Heizperiode über den Jahreswechsel (153 Tage aus, Grenzen ganze Tage), Kühlkalender mit „aus", stündliche Kühlprüfung, Lüftungsminimum und Überschuss, Geräte- und Personenreihen, Determinismus |
| `KonditionierungKalendermodellTests` | 60 Fälle: Leser, Schreiber, Kompilierung, Feiertage, Grenzen, Zeilenleser, Vererbung, Eigentümerregel |
| `KonditionierungCtrlTests` | Schemaschritt zweimal, drei Tabellen `STRICT`, Anlegen/Verwerfen/Kaskade, Katalogbau (der Lauf liest ihn **nicht**), „erneut anwenden" ersetzt genau den Matrixbereich, Vorgabezeilen, Energieerhaltung P1 |
| **Referenzlauf** fünfzehn Projekte gegen `2026-09-26_R23_KesselBereitschaft` | **GESAMT PASS** (4 899 525 Werte) **und byte-gleich** — 460 von 461 Dateien byte-identisch, allein `protokoll.txt` weicht ab (nur Information) |
| Kern-Filter `WP-Plan.Kern.slnf` (Release) | 0 Fehler |
| Windows-Schale (`Debug`, x64) | 0 Fehler |
| `SqlDialektPruefer` | 0 Fundstellen (2 040 SQL-Texte) |
| Testgate `WP-Plan.Kern.slnf` | `KiKern.Tests` 549, `SpeicherEngine.Tests` 386, `SpeicherPlanung.Tests` 27 (1 übersprungen, vorbestehend), `EPOS.UI.Tests` **6 841** — alle grün. **`EPOS.Kern.Tests` ist nach der Behebung der drei Wächterbefunde nicht mehr vollständig durchgelaufen:** Der Lauf wurde von der Arbeitsumgebung wegen Speicherknappheit abgebrochen, nicht wegen eines Fehlers. Belegt ist der Stand aus dem **ersten** vollen Lauf (8 797 von 8 801 grün, genau die drei Befunde unten) und aus zwei **gezielten** Läufen nach der Behebung: 632 Wächterfälle (`…Wache`, `…Paketanhebung`, `…Schema`, `…Projektplan`, `…Projektpaket`, `ProjektDuplizieren`) und 10 Fälle `SolarkollektorTemperaturen`, alle grün, dazu die 120 Konditionierungsfälle. **Der vollständige Lauf von `EPOS.Kern.Tests` ist damit der einzige offene Punkt der Abnahme.** |

## 6. Offen — die zweite Hälfte (KP1b)

- Die Vorlagentabelle `Tab_Konditionierungsvorlage_STAMM` samt Perioden und Wochen, ihre Saat der
  vierzehn ausgelieferten Vorlagen (KP-S1b, gesät mit KP2) und der Fremdschlüssel auf `ID_Vorlage`.
- Die **Nachtauskühlung** (3.7, P9) samt `Nachtauskuehlstunden_H`.
- Die Kopierwege Katalog → Projekt (`CopyFromStamm`), „Speichern unter", Katalogkopie und Schloss,
  `KINDER` für Duplikat und Variante, der `.wpx`-Rundlauf.
- `Werkzeuge/Auslieferungsvorlage` samt Prüfbericht und die Werkzeuge der Karte.
- Der Hinweis auf **Untertemperatur** außerhalb der Heizperiode (er braucht die gelöste Raumluft), die
  Auslegungswerte aus 3.6 und die Nutzungszeit aus dem Personenkalender (F16).

**Die Schnittstelle für KP2** (Oberfläche) ist damit gesetzt: `KonditionierungCtrl` mit `Eigner`,
`Vorgabematrix`/`Matrixspalte`/`Matrixzelle` als DTO der Matrix, `Konditionierungskalender` samt
`Quelle(tag)` für die Vorschau und `Kalenderwoche` für das Wochenraster; die Hüllen kommen nach
`EPOS.UI.Daten/Bedarf/`.

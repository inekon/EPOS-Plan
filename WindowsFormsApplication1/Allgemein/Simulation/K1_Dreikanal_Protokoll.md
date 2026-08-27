# Paket K1 — Dreikanal-Bedarf: Umsetzungsprotokoll

Stand: 27.08.2026 · Branch `Pufferspeicher` · Bezug:
[`Konzept_Brauchwasser_Heizung_Pufferspeicher.md`](Konzept_Brauchwasser_Heizung_Pufferspeicher.md)
Kapitel 4 und 9 (Schritt 48), Entscheidungen **F2** (Netzverluste anteilig), **F3** (Kalender
vereinheitlicht), **F18** (Ganglinien-Kanalzuordnung). Build x64 Debug: 0 Fehler.

## 1. Umfang

Die Bedarfsermittlung führt Heizwärme, Brauchwasser und Prozesswärme jetzt als **drei getrennte
Kanäle** von der Quelle bis zur Kanalbildung — ohne Residuum. Die Kaskade selbst bleibt in K1
unverändert zweikanalig; eine dokumentierte **Übergangsabbildung** (`Kanaele()`: Heiz = Heizung +
Prozess, WW = Brauchwasser) versorgt sie, bis Paket K2 auf drei Kanäle umstellt.

## 2. Bausteine

| Baustein | Inhalt | Stellen |
|---|---|---|
| **Kanalsatz** | `Kanal` (HEIZUNG/BRAUCHWASSER/PROZESS, `AusText`/`Name`) und `Kanalsatz` (drei `float[8760]`, `Summe()`, `Clone()`, `NetzverlusteVerteilen()`, `ErhaltungOk()`, Debug-`Selbsttest`) neben dem unveränderten `Waermekanaele` | `SimulationKanaele.cs:391-777` |
| **Kalender im Rechenkern** | `StromWocheToJahr` mit neuer Überladung `wochentagJan1` (Montag = 0); Phase 1 als Modulo-Kachelung — für Wochentag 6 (Sonntag) beweisbar die exakte Altsequenz; 5-Parameter-Fassung delegiert mit 6 | `BhkwPlan.cs:180-247` |
| **Gemeinsame Profilroutine** | `ProfilBedarf` ersetzt die drei Kopien des Algorithmus „12 Monatswerte × 168-h-Woche → 8760" für Brauchwasser, Prozesswärme und Strom; zwei Quellmodi (Projektrechnung mit Pflichtfilter `ID_Projekt` / Katalogvorschau `_STAMM`); Datenzugriff über **`DataRepository` mit `?`-Parametern** statt RecordSet-String-SQL; V0-Fehlerpfade + zwei neue NaN-Wächter (Monatssumme 0, Nullprofil) | NEU `Allgemein/Simulation/ProfilBedarf.cs` (545 Z.) |
| **Kanalbildung** | Gebäude → Heizkanal; externe Ganglinien je `Kanal`-Spalte in ihren Kanal; Brauchwasser-/Prozessprofile in ihre Kanäle; **Netzverluste je Stunde anteilig** (F2, Heizung als exakte Differenz, Stunde ohne Bedarf → Heizung); Summenvektor `Waermebedarf` = abgeleitete Kanalsumme; `KanaeleDrei()` als K2-Andockpunkt; Kappungsfälle/`Kanal_Kappungen` ersatzlos entfallen | `SimulationWaermebedarf.cs:129-620` |
| **Energieprobe** | je Stunde Kanalsumme gegen unabhängig (double) akkumulierte Gesamtsumme, Toleranz 1-ULP-Klasse × 5 Rundungsschritte; Verletzung → Protokollwarnung einmal je Lauf; dazu Debug-Selbsttest im Kanalsatz | `SimulationWaermebedarf.cs` (`Energieprobe`), `SimulationKanaele.cs` |
| **Strombedarf** | auf `ProfilBedarf` umgestellt; Kalender aus der Wärmerechnung durchgereicht (`SimulationRunner.cs:152-157`, `Form_Simulation_Detail.cs:2976-2981`), sonst eigene Klimadaten-Lesung mit Cache; V0-3-Fehlerpfade nachgezogen | `SimulationStrombedarf.cs` |
| **Schritt 48** | `Z_ProjektWaermebedarf.Kanal` TEXT(50), DML-Vorbelegung `Heizung`; `ZIEL_VERSION` 47 → 48; bewusst **ohne** SchemaKatalog-Rückfallebene (Muster der Schritte 45–47: DDL inline, alle Leser spaltentolerant) | `SchemaMigration.cs`, `SchemaKatalog.cs` (Namenskonstanten) |
| **Kanal-Spalte durchgängig** | Modellfeld `Kanal` (Default Heizung, NULL-tolerant), Controller mit Spaltenvorsorge + `KanaeleNachladen` über alle vier Aufrufwege; Schreibweg `Add_WaermebedarfExtern` erweitert; Löschpfad sichert Kanäle vor DELETE+INSERT | `Z_ProjektWaermebedarfModel.cs`, `Z_ProjektGebGanglinieCtrl.cs`, `WizardCtrl.cs`, `WaermebedarfExternKontextMenuCtrl.cs` |
| **Oberfläche (F18)** | Kanal-ComboBox je markierter Zuordnung im Ganglinien-Dialog, programmatisch (CP1252-Datei byte-sicher bearbeitet); Steuerwert↔Anzeige über `DbWerte.KANAL_*` + Ressourcen. **Mitfix:** `btn_Hinzu` legte bisher mehrfach dasselbe Modellobjekt in die Liste — jetzt ein eigenes je Zuordnung | `Views/Wärmebedarf/Form_Waermebedarf.cs` |

Neue `DbWerte`: `KANAL_HEIZUNG = "Heizung"`, `KANAL_BRAUCHWASSER = "Brauchwasser"`,
`KANAL_PROZESS = "Prozesswaerme"` (bewusst umlautfrei). 13 neue Ressourcenschlüssel (de + en +
Designer): `KANAL_LABEL`, `KANAL_*_ANZEIGE` (4), `SIMENG_ENERGIEPROBE_KANAELE`,
`SIMENG_KALENDER_WOCHENENDE_UNBESTIMMT`, `SIMENG_PRAEFIX_BRAUCHWASSER`,
`SIMENG_PRAEFIX_PROZESSWAERME`, `SIMENG_PROFIL_MONATSSUMME_NULL`,
`SIMENG_PROFIL_WOCHENPROFIL_NULL`, `SIMENG_STROMPROFIL_KOPF_FEHLT`,
`SIMENG_STROMPROFIL_TYP_UNDEFINIERT`, `SIMENG_STROMPROFIL_TYPPROFIL_FEHLT`.

## 3. Bewusste Verhaltensänderungen

1. **F2** — Netzverluste anteilig auf die Kanäle; `brauchwasserwerte`/`prozesswerte` tragen ihren
   Anteil mit (wirkt auf die WW-Deckung der WP). `Waermebedarf_Brauchwasser/_Prozess` und die
   Monatswerte bleiben der reine Profilanteil.
2. **F3** — alle Profil-Bedarfe kacheln am Klimadaten-Kalender (produktiv: 1. Januar =
   Donnerstag → Verschiebung um drei Tage). Monats- und Jahresmengen unverändert; die
   Katalogvorschau bleibt bei der Sonntag-Konvention (projektlos).
3. **Strom erbt die V0-3-Fehlerpfade**: fehlender Kopf/fehlendes Profil → Warnung + überspringen
   statt stillem Weiterrechnen mit dem vorigen Profil.
4. Neue NaN-Wächter melden und überspringen statt NaN zu erzeugen.

## 4. Verifikation

Referenzlauf (Weg A, Arbeitskopie migriert auf Schemastand **48**) gegen Basis `2026-08-27_V0`:

| Ergebnis | Projekte | Zuordnung |
|---|---|---|
| **PASS** | 1018, 1030 | einzige Projekte ohne Stromprofil-, Brauchwasser- und Prozessanteil — K1 ist dort verhaltensneutral |
| FAIL (gewollt) | 1007, 1008, 1011, 1017, 1021, 1024 | F3-Kalenderverschiebung der Stromprofile (alle sechs führen Stromverbraucherprofile); 1007/1011/1024 zusätzlich Brauchwasser (F3 + F2) |
| FAIL (gewollt) | 1023 | kein Stromprofil (`Strombedarf_Gesamt` 0) — reine Brauchwasser-Wirkung: F3-Verschiebung + F2-Netzverlustanteil im WW-Kanal (sichtbar u. a. am Heizstab) |

**Jahressummen-Invariante: bestanden** — `Waermebedarf_Gesamt` und `Strombedarf_Gesamt` sind in
allen neun Projekten exakt unverändert; es ändern sich ausschließlich zeitliche Verteilung und
Kanalzuordnung. **Energieprobe: 0 Verletzungen** über 9 × 8760 Stunden. `pruefen`: alle Projekte
plausibel, keine NaN/Inf. Laufprotokoll: 12 bekannte Bestandswarnungen (Energieträger-Zuordnung
`ID_Carrier` leer aus den Kostendialog-Paketen; Rückfall-ΔT zweier Puffer ohne Temperaturpaar),
0 Fehler.

**Neue Basis:** `Referenzlaeufe\2026-08-27_K1` (Selbstvergleich zweiter Lauf **216/216
byte-/MD5-gleich**); `2026-08-27_V0` rückt zu den früheren Ständen.

## 5. Offene Punkte / Notizen

| # | Punkt | Ziel |
|---|---|---|
| K1-O1 | `Tab_Stromverbraucher` bekommt weiterhin **keinen** `ID_Projekt`-Filter (in `ProfilQuelle.Strom` kommentiert) — ob der Stromzweig dieselbe Namens-Mehrdeutigkeit hat wie Brauchwasser/Prozess (V0-3), ist eine offene Datenfrage | Datenprüfung, ggf. Einzelfix |
| K1-O2 | Energieprobe-Toleranz ist 1-ULP-Klasse × **5 Rundungsschritte** (`ERHALTUNG_SCHRITTE_SUMME`) — begründet durch drei float-Speicherungen + zwei Additionen gegen die double-Referenz; enger stellen hieße Fehlalarme ohne Rechenfehler | dokumentiert, bleibt |
| K1-O3 | `Waermebedarf` und `Kanaele().Summe()` können je Stunde ≤ 1 ULP auseinanderliegen (Klammerung `H+B+P` vs. `(H+P)+B`) — vor K1 galt Bitgleichheit der Zweikanalsumme | entfällt mit K2 (Kaskade rechnet dann direkt dreikanalig) |
| K1-O4 | Bestandswarnungen des Laufs: 10 × fehlende Energieträger-Zuordnung (`ID_Carrier` leer — Kostendialog-Bestand), 2 × Puffer ohne Temperaturpaar (Rückfall-ΔT 10 K; wird mit Schritt 51/A1-Temperaturübernahme gegenstandslos, sofern die Alt-Zuordnung ein Paar trägt) | Anwender-Datenpflege / A1 |
| K1-O5 | Die Übergangsabbildung `Kanaele()` (Heiz = Heizung + Prozess) ist der definierte K2-Abrisspunkt; `KanaeleDrei()` steht bereit | K2 |
| K1-O6 | Restliche `DateTime.Now.Year`-Stellen aus V0-O4 unverändert (Chart-Achsen, CSV-Export, Ferientage, WP-Quellprofil-Wochengang — letzterer wird mit Q1/Tagesprofil obsolet) | Q1 / kosmetisch |

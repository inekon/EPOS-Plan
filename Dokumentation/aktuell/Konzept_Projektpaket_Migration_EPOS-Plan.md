# Konzept: Ältere Projektpakete beim Import anheben

Ein Projektpaket (`.wpx`) eines älteren Programmstands muss sich immer einspielen lassen. Dieses
Papier beschreibt, wie der Import ein Paket vom Schemastand seiner Quelle auf den Zielstand
dieses Programms hebt, und löst die offene Frage TF4 des
[Konzepts Projekttransfer](Konzept_Projekttransfer_EPOS-Plan.md) ab („ältere Pakete in neuere
Datenbank“). Anlass: Ein Paket mit Schemastand 93 wurde an einem Rechner mit Stand 150
abgelehnt.

## 1 Befund

**Paketformat.** Ein Paket ist ein ZIP (`ProjektExportImportCtrl.ExportEines`): `manifest.json`
mit `format = wp-projekt`, `formatVersion = 2`, `schemaVersion` (der Migrationsstand der
Quelle, `SchemaStand.Zielversion`), Quellprojekt, Tabellenliste mit Primärschlüssel;
`data/<Tabelle>.json` je Projekttabelle des Transferplans (`SELECT *` mit Projektfilter, Zeilen
als Spalte → Wert — also das Spaltenbild der Quelle); `projects/<i>/data/` je Variante;
`catalogs/` (Kataloge mit natürlichem Schlüssel: Träger, Umrechnungen, Brennstoff, Typen,
Kostenkomponenten, Zapfprofil-Kataloge), `fill/` (Katalogzeilen, die über die Original-Id
aufgefüllt werden), `catalogChildren/`, `pvstamm/`. Es ist **keine SQLite-Datei** und trägt
**kein DDL**.

**Import.** Der Import ist spaltentolerant: Er fügt die Schnittmenge aus Paketspalten und
Zielspalten ein; eine neue Zielspalte bekommt ihre Vorgabe, eine entfallene Paketspalte wird
verworfen, eine fehlende Zieltabelle übersprungen. Abgelehnt wurde das Paket an einer Sperre:
`EPOS.Kern/Controller/ProjektExportImportCtrl.cs:561` (Einzelimport, Text mit „Bitte beide
Rechner auf denselben Programmstand bringen“) und `EPOS.Kern/Controller/ProjektTransferSammel.cs:574`
(Sammellauf). Begründung am Ort (B2, Konzept Projekttransfer T2): „die Datenmigrationen laufen
datenbankweit genau einmal, ein Paket mit anderem Stand schleuste still Altdaten ein“. Das trifft
zu — aber nur für die Schritte, die **Werte umrechnen**. Neue und entfallene Spalten fängt die
Schnittmenge schon ab.

**Migrationsapparat.** `SchemaMigration` liegt in der Windows-Schale
(`WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs`), arbeitet über die globale
Zugriffsschicht (`DataRepository`) auf der Anwenderdatenbank, setzt Kataloge, Sichten,
Fremdschlüssel und `Tab_Applikation` voraus und fängt erst bei Stand 61 an (Freeze). Die
Datenteile der Schritte liegen dagegen im Kern (`EPOS.Kern/Allgemein/Update/*`) als
**SQL-Texte mit `?`-Parametern auf einer Tabelle** (`BhkwWirkungsgradFaktor.SqlUmrechnen(t)`,
`GebaeudeSchema.UmbenennungSql(t)`, `PreisbasisUebernahme.SQL_KWH` …) oder als reine Funktion
(`BaualtersklassenSchema.Umschluesseln`).

**Schritte 94 bis 150 (57) nach Art ihrer Wirkung auf ein Paket:**

| Art | Zahl | Schritte |
|---|---|---|
| nur DDL (Spalte, Tabelle, Sicht, Index, Spaltenabbau) — die Schnittmenge des Imports genügt | 31 | 95, 97, 105, 107–111, 114, 116–119, 122, 123, 125, 128, 129, 131, 133–140, 144, 145, 147, 150 |
| nur Katalog oder Saat (`*_STAMM`, Vorlagen, globale Tabellen) — das Ziel führt sie schon | 12 | 94, 103, 115, 120, 126, 130, 132, 141–143, 146, 149 |
| vom Import schon abgedeckt | 4 | 96 (Umschlüsselung, Waisen), 100 (verwaiste Verweise werden leer), 121 (Katalogverweis über den Namen), 124 (Zapfprofil-Laufangaben benannt) |
| **rechnet Projektwerte um — braucht eine Paketumformung** | **10** | 98 BHKW-Wirkungsgrad Prozent → Faktor, 99 Aufteilung el/th, 101 `Wohnflaeche` → `Nutzflaeche`, 102 leere KWKG-Anlagenart → NULL, 104 Zeitzonentarif abgelöst, 106 fremde Ergebnisverweise, 112 Preisbasis, 113 Preiszeilen der Gase m³ → Nm³, 127 Freitext → Wirkung, 148 Baualtersklassen umgeschlüsselt |

Schritt 150 (Vorlauf/Rücklauf am Kollektor entfernt) braucht also keine Umformung: Die Werte
haben am Ziel keine Spalte mehr, und kein Rechenweg las sie.

## 2 Wege

**A — Paket als SQLite im Quellschema, die Migration darauf.** Trägt nicht: Das Paket enthält
kein Schema, das Schema eines alten Stands lässt sich nicht nachbauen, und die Migration braucht
Kataloge, Sichten, Fremdschlüssel und den Marker der Anwenderdatenbank, läuft über die globale
Zugriffsschicht und liegt in der Windows-Schale (iOS hätte sie nicht). Ein neues Paketformat
mit eingebettetem DDL löste nur künftige Pakete, nicht die vorhandenen.

**B — Zeilen umformen, mit den Anweisungen der Schritte.** Der Import liest die Zeilen wie
heute, legt je Projektbaum eine **Arbeitsdatenbank** im Speicher an (je Pakettabelle eine
Tabelle mit den Paketspalten, dazu die mitgereisten Kataloge als Nachschlagetabellen) und fährt
dort für jeden Schritt zwischen Paketstand und Zielstand mit Paketwirkung **dieselbe Anweisung**
des Kern-Bausteins nach, den auch die Migration ruft. Danach gehen die Zeilen zurück in den
unveränderten Importweg; unveränderte Zellen behalten ihren JSON-Wert. Kein zweites Regelwerk:
Die Paketstufe nennt je Schritt nur, **welche** Anweisung auf **welche** Tabelle wirkt.

**C — Staging in einer Kopie der Anwenderdatenbank.** Verworfen: Die Kopie steht auf dem
Zielstand; ein Paket im Altstand ließe sich darin ebenso wenig migrieren.

**Bewertung.** A: Aufwand groß (Migration in den Kern, Umleitung der Zugriffsschicht, neues
Paketformat), Risiko hoch, hilft vorhandenen Paketen nicht. B: Aufwand mittel (ein Register, eine
Arbeitsdatenbank, zehn Stufen), Risiko gering (die Umformung läuft vor der Transaktion, die
Datenbank sieht nur das Ergebnis), Wartung ein Eintrag je neuem Schritt. **Empfehlung: B.**

## 3 Umsetzung (Weg B)

- **Register** `EPOS.Kern/Allgemein/Update/Paketanhebung.cs`: je Schemaschritt von der unteren
  Grenze bis `SchemaStand.Zielversion` genau ein Eintrag — Art (DDL, Katalog, Import, Umformung),
  Kurztext und bei Umformung die Aktion auf der Arbeitsdatenbank. Die Wache
  `ProjektpaketAnhebungTests` verlangt einen Eintrag für jede Nummer: Wer einen Schritt anlegt, muss
  seine Paketwirkung benennen.
- **Arbeitsdatenbank** `Paketarbeitsdatenbank` (Kern, `Microsoft.Data.Sqlite`, `:memory:`),
  Anweisungen über `SqliteDatenzugriff.ErzeugeKommando` wie überall. Neue Tabellen einer Stufe
  (Schritt 127: `Tab_ProjektWirkung`) reisen als neue Pakettabelle weiter.
- **Import:** Stand = Ziel oder 0 (Altpaket vor T2): wie heute. Stand < Ziel: anheben, dann
  importieren. Stand > Ziel: benannt abgelehnt („neuer als dieses Programm — bitte das Programm
  aktualisieren“). Scheitert eine Stufe, bricht der Import mit Schrittnummer ab, bevor die
  Transaktion beginnt — die Datenbank bleibt unberührt.

## 4 Bedienung

- Vorschau im Dialog: „Das Paket (Stand 93) wird beim Import auf Stand 150 gehoben: 57 Schritte,
  davon 10 mit Umformung der Projektdaten.“ In der Paketliste des Sammellaufs steht der Stand mit
  „wird gehoben“; ein neueres Paket steht dort als nicht einspielbar.
- Das Importergebnis nennt die Anhebung und je umformender Stufe eine Zeile („Schritt 148: 1
  Gebäude umgeschlüsselt …“, unklare Fälle benannt). Die Sicherungskopie vor dem Import bleibt.

## 5 Grenzen

- **Untere Grenze:** Das Register beginnt mit Stufe 93 (Grenze 92). Ein Paket mit Stand 1 bis 91
  wird wie ein Altpaket tolerant eingespielt, die Stufen ab 93 laufen, und das Ergebnis nennt,
  dass Umformungen bis Stand 92 nicht nachgefahren wurden. Die Schritte 62 bis 92 enthalten
  weitere Umformungen (79 Heizstab je Anlage, 83/84 Strompreis und Vergütung, 89/91 KWKG) —
  Stufe 2 dieses Konzepts. Kein Paket wird abgelehnt, weil es zu alt ist.
- **Katalogverweise** auf Sätze, die es am Ziel nicht gibt, folgen der Regel des Imports:
  Natural-Key-Kataloge reisen mit, Gebäude- und Wärmepumpenkatalog werden über den Namen neu
  gefunden (sonst leer), Auffüllkataloge über die Original-Id.
- **Schritt 104 im Paket:** Zonensätze werden entfernt und mit Zonentarif gerechnete Ergebnisse
  verworfen wie in der Datenbank. Die Leistungspreis-Staffel eines rechnenden Zonensatzes wird
  nicht an den Stromträger geschrieben (dazu braucht die Migration die Trägerzuordnung der
  Datenbank), sondern im Ergebnis mit ihren Werten genannt.
- Kataloge des Pakets (`catalogs/`, `fill/`) werden nicht umgeformt: Am Ziel gilt der Katalog des
  Ziels; nur ein dort fehlender Satz reist im Altstand mit.

## 6 Prüfweg

Alte Pakete gibt es nicht im Repository. `ProjektpaketAnhebungTests` exportiert deshalb aus der
Testdatenbank und baut das Paket auf den Stand 93 zurück: `schemaVersion` = 93, `Nutzflaeche` →
`Wohnflaeche`, Spalten jüngerer Schritte entfernt, BHKW-Wirkungsgrad in Prozent, Baualtersklasse
im alten Schlüssel, `Vorlauf`/`Ruecklauf` am Kollektor, Freitext statt Wirkung. Nach dem Import
stimmen die Werte mit der Quelle überein. Dazu: ein Paket auf Zielstand bleibt unverändert, ein
Paket neuer als der Zielstand wird mit klarem Text abgelehnt, eine scheiternde Stufe lässt die
Datenbank unberührt.

## 7 Aufwand

| Stufe | Inhalt | Umfang |
|---|---|---|
| 1 | Register 93–150, Arbeitsdatenbank, zehn Umformungen, Importweg, Dialog, Tests, Wiki | eine Sitzung |
| 2 | Register 62–92 mit den Umformungen dieser Schritte, untere Grenze 61 | eine Sitzung |
| laufend | je neuem Schemaschritt ein Registereintrag (die Wache erzwingt ihn) | Minuten |

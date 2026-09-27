# Konzept: Ältere Projektpakete beim Import anheben

Ein Projektpaket (`.wpx`) eines älteren Programmstands muss sich immer einspielen lassen. Dieses
Papier beschreibt, wie der Import ein Paket vom Schemastand seiner Quelle auf den Zielstand
dieses Programms hebt, und löst die offene Frage TF4 des
[Konzepts Projekttransfer](../aktuell/Konzept_Projekttransfer_EPOS-Plan.md) ab („ältere Pakete in neuere
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

**Schritte 62 bis 92 (31) nach Art ihrer Wirkung auf ein Paket** (Stufe 2; die Migration beginnt
bei Stand 61, darunter liegt die Access-Zeit):

| Art | Zahl | Schritte |
|---|---|---|
| nur DDL | 16 | 63–66, 70–74, 81, 82, 85, 86, 88, 91, 92 |
| nur Katalog oder Saat | 5 | 62 (Klimadaten-Waisen der `*_STAMM`), 68 (Hersteller des Stromspeicherkatalogs), 75 (Nutzungsdauertabelle; die Verweisspalte der Projektkosten bleibt leer), 77 und 78 (Kostenvorlage) |
| vom Import schon abgedeckt | 1 | 80 (Katalogverweis der Wärmepumpen-Projektkopie, am Ziel über den Bezeichner) |
| **rechnet Projektwerte um** | **9** | 67 leere BHKW-Leistungsuntergrenze → 30 %, 69 verdorbene PV-Modulkoeffizienten der Projektkopie, 76 ein Trägersatz je Projekt und Träger, 79 Heizstab vom Projekt an jede Wärmepumpe, 83 Strompreis-Anteile zerlegen den Arbeitspreis, 84 Einspeisevergütung von der Trägerkarte in die Parameter, 87 eine aktive Speichervariante je Projekt, 89 KWK-Zuschlag an die Anlage, 90 Nullzeilen der Erfassungsgruppen |

Der Gesetzeskatalog von Schritt 87 und die Katalogteile von 68, 69 und 75 gehören dem Ziel; die
Spaltenabbauten von 79, 85, 90 und 91 erledigt die Schnittmenge des Imports, nachdem die
Umformungen die Werte umgesetzt haben.

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
Arbeitsdatenbank, neunzehn Umformungen), Risiko gering (die Umformung läuft vor der Transaktion, die
Datenbank sieht nur das Ergebnis), Wartung ein Eintrag je neuem Schritt. **Empfehlung: B.**

## 3 Umsetzung (Weg B)

- **Register** `EPOS.Kern/Allgemein/Update/Paketanhebung.cs`: je Schemaschritt von der unteren
  Grenze bis `SchemaStand.Zielversion` genau ein Eintrag — Art (DDL, Katalog, Import, Umformung),
  Kurztext und bei Umformung die Aktion auf der Arbeitsdatenbank. Die Wache
  `ProjektpaketAnhebungTests` verlangt einen Eintrag für jede Nummer: Wer einen Schritt anlegt, muss
  seine Paketwirkung benennen.
- **Arbeitsdatenbank** `Paketarbeitsdatenbank` (Kern, `Microsoft.Data.Sqlite`, `:memory:`),
  Anweisungen über `SqliteDatenzugriff.ErzeugeKommando` wie überall. Neue Tabellen einer Stufe
  (Schritt 127: `Tab_ProjektWirkung`; Schritt 84: der Parametersatz der Wirtschaftlichkeit, wenn
  das Paket keinen trägt) reisen als neue Pakettabelle weiter. Eine leere Zelle, die das Paket
  nicht trug (Spalte oder Zeile einer Stufe), reist nicht: Am Ziel gilt die Vorgabe der Spalte,
  auch bei `NOT NULL`.
- **Zeilenweise Bausteine** (Schritte 83 und 84): `StrompreisZerlegung.Falten` und
  `VerguetungUmzug.Umziehen` entscheiden je Zeile und schreiben mehrere Anweisungen. Sie laufen
  über `Umformzugriff` — an der Datenbank für die Migration, an der Arbeitsdatenbank für das Paket;
  es bleibt ein Regelwerk.
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

- **Untere Grenze:** Das Register beginnt mit Stufe 62 (Grenze 61, der erste Stand der
  SQLite-Migration). Ein Paket mit Stand 1 bis 60 wird tolerant eingespielt, alle Stufen ab 62
  laufen, und das Ergebnis nennt, dass Umformungen bis Stand 61 nicht nachgefahren wurden. Kein
  Paket wird abgelehnt, weil es zu alt ist.
- **Schritt 69 im Paket:** Repariert wird aus der Wertequelle der Migration (Auslieferungsmodule,
  CEC-Liste am Herstellerdatenpfad) und aus dem Stammsatz, wenn das Paket ihn mitführt; ein
  verdorbener Wert ohne Treffer wird leer wie in der Datenbank.
- **Schritt 90 im Paket:** Die Erfassungsgruppe und die Hauptkomponente werden in den
  mitgereisten Kostenkatalogen nachgeschlagen (`catalogs/Tab_KostenKomponente`,
  `fill/Tab_Kostenfaktor`); fehlen sie im Paket, bleibt die Nullzeile stehen — sie trägt keinen
  Wert.
- **Schritte 83 und 84 im Paket:** Die Trägerart (`pricing_model`) kommt aus dem mitgereisten
  Trägerkatalog, der wirksame Arbeitspreis aus den Preiszeilen des Projekts; die Detailzeile
  nennt die Zahl der gefalteten Trägersätze bzw. der umgezogenen Projekte.
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

Für die Stufen 62 bis 92 bauen vier Fälle das Paket auf Stand 61 zurück, sodass jede Stufe bis zum
Zielstand läuft: **Anlagen** (Leistungsgrenze leer, Heizstab am Projekt, zwei aktive
Speichervarianten, KWK-Zuschlag am Projekt — 67, 79, 87, 89), **Strompreis** (doppelter
Trägersatz, Aufschlagsmodus mit aktivem Anteil, Vergütung an der Karte — 76, 83, 84), **Kosten**
(Nullzeile einer Erfassungsgruppe — 90) und **PV** (Koeffizient als Kopie des Kurzschlussstroms,
T_NOCT außerhalb des Fensters — 69). Nach dem Import stehen die Werte da, die die Migration an
der Datenbank erzeugt hätte.

## 7 Stufen

| Stufe | Inhalt | Stand |
|---|---|---|
| 1 | Register 93–150, Arbeitsdatenbank, zehn Umformungen, Importweg, Dialog, Tests, Wiki | umgesetzt |
| 2 | Register 62–92 mit neun Umformungen, untere Grenze 61, `Umformzugriff` für 83 und 84 | umgesetzt |
| laufend | je neuem Schemaschritt ein Registereintrag (die Wache erzwingt ihn) | Regel im Register `Paketanhebung` |

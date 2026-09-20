# Datenmodell und Schema der Wirtschaftlichkeit — Ist gegen Soll, Schemabedarf, doppelte Wahrheiten

Stand 19.09.2026 · Zweig `ios_migration_september` · `SchemaStand.Zielversion = 94` (`EPOS.Kern/Allgemein/Update/SchemaStand.cs:265`) · gemessen an der Kopie von `Referenzlaeufe/Kenndaten_Test.sqlite` (70,8 MB, `Tab_Applikation.SchemaVersion = 94`).

## 0 Ergebnis in fünf Sätzen

Das Ist-Schema ist gesünder als das Konzept glauben macht: alle 20 geprüften Wirtschaftlichkeitstabellen sind `STRICT` (119 von 120 Tabellen der Datei, die eine Ausnahme ist `sqlite_sequence`), tragen CHECK-Bedingungen und sind auf die Namen des Konzepts abgebildet — die einzige echte Namensabweichung ist, dass `Tab_ProjektWirtschaftlichkeit` bereits sechs der acht in § 2.11.5 als „neu" geführten Rahmen-Szenariospalten seit Schritt 71 besitzt. Der zweite Migrationsmechanismus lebt und ist größer als die „vier Tabellen" des § 6.5: `WirtschaftlichkeitCtrl.StelleTabellenSicher` legt **fünf** Tabellen per `CREATE TABLE` an — **ohne `STRICT`, ohne Fremdschlüssel** — und rüstet 55 Spalten per `SpalteSicher` nach; er verstößt damit gegen ADR-001 (er ist wörtlich die dort verworfene Option B) und gegen die STRICT-Hausregel der `CLAUDE.md`, ist aber im Normalbetrieb wirkungslos, weil `Erstbereitstellung` eine vollständige Vorlage kopiert. Drei Konzeptaussagen sind durch Messung widerlegt: § 5 „Schemaschritt 62 vergeben (U-1)" stimmt nicht (62 ist `Schritt_62_KlimaWaisen`, die fünf Gase führen in `Tab_Brennstoff_Stamm.Einheit` unverändert `m³`), § 2.13 (3) Punkte 2 und 3 sind mit #357 erledigt, und die Ersatz/Restwert-Felder der Speicherflotte sind keine Spalten, sondern JSON-Felder im Flottenstand. Für die offenen Punkte schlage ich acht Schemaschritte 95–102 vor, von denen sieben reines, ergebnisneutrales DDL sind und **nur einer** — der Anschluss der Speicherflotte — die Einfrierregel der Referenzbasis berührt.

---

## 1 Ist-Schema der Wirtschaftlichkeitstabellen

Gemessen an der Kopie, `sqlite_master.sql` und `PRAGMA table_info` / `PRAGMA foreign_key_list`. Alle Typen sind SQLite-`STRICT`-Typen (`INTEGER`, `REAL`, `TEXT`); `!` = `NOT NULL`, `=x` = `DEFAULT x`.

### 1.1 Übersicht

| Tabelle | Spalten | STRICT | CHECK | FK | Zeilen |
|---|---:|---|---:|---:|---:|
| `Tab_ProjektWirtschaftlichkeit` | 45 | ja | 10 | 1 | 5 |
| `Tab_ErgebnisWirtschaftlichkeit` | 53 | ja | 3 | **0** | 78 |
| `Tab_Energieanlagen` | 80 | ja | 19 | 13 | 130 |
| `Tab_ProjektWerte` | 24 | ja | 4 | 5 | 164 |
| `Tab_Kostenfaktor` | 3 | ja | 1 | 0 | 75 |
| `Tab_KostenVorlage` | 8 | ja | 3 | **0** | 20 |
| `Tab_KostenVorlagePosition` | 15 | ja | 4 | 2 | 120 |
| `Tab_Nutzungsdauer` | 11 | ja | 2 | 1 | 28 |
| `Tab_Gesetzesparameter` | 8 | ja | 5 | 0 | 226 |
| `energy_carrier` | 19 | ja | 7 | 0 | 27 |
| `energy_price` | 11 | ja | 1 | 2 | 34 |
| `energy_project_settings` | 40 | ja | 15 | 2 | 28 |
| `Tab_ProjektPhotovoltaik` | 22 | ja | 8 | **0** | 0 |
| `Tab_StromspeicherVariante` | 23 | ja | 11 | 1 | 13 |
| `Tab_ErgebnisWirtSensitivitaet` (`TAB_SENS`) | 7 | ja | 1 | **0** | 12 |
| `Tab_ProjektTarif` (`TAB_TARIF`) | 55 | ja | 4 | 1 | 0 |
| `Tab_ErgebnisStromMatrix` (`TAB_MATRIX`) | 10 | ja | 1 | **0** | 8 |
| `Tab_BHKW` | 27 | ja | **0** | 0 | 6 |
| `Tab_Heizkessel` | 23 | ja | 2 | 0 | 22 |
| `energy_conversion` | 8 | ja | 5 | 1 | 65 |
| `Tab_Brennstoff_Stamm` | 16 | ja | 1 | 1 | 25 |

Die Namen `TAB_PARAMETER`/`TAB_ERGEBNIS`/`TAB_SENS`/`TAB_TARIF`/`TAB_MATRIX` stehen in `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:69–73`; `TAB_PARAMETER` ist `Tab_ProjektWirtschaftlichkeit`, es gibt **keine** eigene Parametertabelle.

### 1.2 Die Rahmenzeile `Tab_ProjektWirtschaftlichkeit` (45 Spalten)

Rahmen: `ID`, `ID_Projekt` (FK → `Tab_Projekt.ID`, `ON DELETE CASCADE`, UNIQUE-Index `UQ_ProjWirtProj`), `Zinssatz`, `Betrachtungszeitraum` (INTEGER), `Preissteigerung_Energie`, `Preissteigerung_Betrieb`, `Preissteigerung_Investition`, `Einspeiseverguetung`, `Einspeiseverguetung_KWK`, `CO2_Preis`, `GeaendertAm`.
Steuer/Bilanz: `Unternehmensart`, `Raeumlicher_Zusammenhang`, `Hocheffizienz_Nachweis`, `Jahresnutzungsgrad`, `Energiesteuer_Wahl`, `Aufteilung_Methode`, `Stromst_Befreiung_Modus`, `Bilanz_Jahr`, `Emissions_Methode`, `Biomasse_Konvention`, `Biomasse_Nachweis`.
KWKG (vier verbliebene): `KWKG_Abschlag_Negativ`, `KWKG_Pauschalmodus`, `KWKG_Stichtag`, `KWKG_Inbetriebnahme` — **genau die vier, die § 6.5 nennt; die elf Altspalten sind fort** (Schritte 90/91, `KwkgProjektaltspalten.cs`).
Referenz/Kraftwerkspark: `ID_Referenzprojekt` (Schritt 92, nullbar), `ID_Kraftwerkspark`, `RefKessel_Wirkungsgrad`, `RefKessel_ID_Brennstoff`.
Szenarien (Schritte 71/72): `Szen_Best_Zins`, `Szen_Best_Preis_E`, `Szen_Best_Preis_B`, `Szen_Best_Invest`, `Szen_Best_Ertrag`, `Szen_Best_Dauer`, `Szen_Best_Preis_I` und die sechs `Szen_Worst_*`-Gegenstücke, dazu `Nicht_Monetaer` (TEXT, Freitext).

**Befund R-1 bestätigt:** fünf Zeilen für fünf Stammprojekte (1017, 1019, 1023, 1024, 1030), eine je Stammprojekt, durch den UNIQUE-Index erzwungen. `ID_Referenzprojekt` ist in allen fünf NULL — der Schritt 92 ist wie dokumentiert ergebnisneutral gelaufen.

### 1.3 Abweichungen zu den Namen und Feldern des Konzepts

| Konzeptstelle | Konzept sagt | Ist-Schema | Bewertung |
|---|---|---|---|
| § 1.2 ① Positionswelt | „Kostenart · Bemessung · Satz · Menge · Betrag · IstErloes · Nutzungsdauer · StartJahr · IstPflicht" | `Kostenart`, `Bemessung`, `Einheitpreis` (nicht „Satz"), `Menge`, `EingegebenerWert` (nicht „Betrag"), `IstErloes`, `Nutzungsdauer`, `StartJahr`, `IstPflicht` | **Namensabweichung**: „Satz" = `Einheitpreis`, „Betrag" = `EingegebenerWert`. Das Konzept führt Fachbegriffe, nicht Spaltennamen — für die Umsetzung ist eine Nennung der echten Namen nötig |
| § 1.2 ① | „je Projekt, Komponente und ANLAGE" | `ProjektID`, `KomponentenID`, `ID_Anlage`, zusätzlich `ID_AnlageGeraet`, `StammID`, `VorlageID`, `Gruppe`, `NutzungsdauerID` | vollständig, zwei ungenannte Zusatzschlüssel |
| § 1.2 ③ | `energy_carrier · energy_price · energy_project_settings` | alle drei vorhanden | deckungsgleich |
| § 1.2 ④ | „Schluessel · Klasse · JahrVon · Wert · Einheit · Status · Quelle" | exakt diese sieben plus `ID` | deckungsgleich |
| § 1.2 Schluss | „seit Schema 61 die Wirtschaftlichkeitsspalten an `Tab_Energieanlagen` (KWKG_*, `Energiesteuer_Wahl`, `Aufteilung_Methode`, `Hilfsenergie_Anteil`)" | alle vorhanden; KWKG_* sind **neun** Spalten (`KWKG_Stichtag`, `_Inbetriebnahme`, `_Anlagenart`, `_Eigenstromfall`, `_Satz_Einspeisung`, `_Satz_Eigen`, `_Vbh_Kontingent`, `_Vbh_Jahresdeckel`, `_Kostenanteil`) | Zählung im Konzept fehlt; `_Kostenanteil` kam erst mit Schritt 89/91 hinzu |
| § 2.11.5 Zeile „Rahmen" | „je Größe `_Best`/`_Worst` (8 Spalten)" — **neu** | `Szen_Best/Worst_Zins`, `…_Preis_E`, `…_Preis_B` existieren seit Schritt 71 → **6 von 8 sind da**; es fehlt allein `Betrachtungszeitraum` Best/Worst | **Konzept überholt.** Zusätzlich lauert eine Namensfalle: `Szen_*_Dauer` heißt **Nutzungsdaueränderung** (Szenarienkonzept § 2.1, Z. 68), nicht Betrachtungszeitraum |
| § 2.11.5 Zeile „Energiepreise" | `custom_price_work_best/_worst` — neu | in `energy_project_settings` **nicht vorhanden**; Bestand: `custom_price_work`, `custom_price_base`, `custom_price_power` | Bedarf bestätigt |
| § 2.11.5 Zeile „Erlössätze" | „je Feld ein Best/Worst-Paar an derselben Tabelle" | weder an `Tab_ProjektWirtschaftlichkeit` (`Einspeiseverguetung`, `Einspeiseverguetung_KWK`) noch an `Tab_ProjektPhotovoltaik` (`DvEntgelt`, `PpaPreis`, `PpaSpotAufschlag`, `MarktwertJahresmittel`) | Bedarf bestätigt |
| § 2.11.5 Zeile „Mengen" | „ein Mengenfaktor [%] je Szenario an der Rahmenzeile" | nicht vorhanden | Bedarf bestätigt |
| § 2.13 (3) Nr. 4 | geräteeigene Nutzungsdauer in `Tab_BHKW`, `Tab_Heizkessel`, `Tab_StromspeicherVariante` | alle drei tragen `Nutzungsdauer` (BHKW `INTEGER`, Kessel `REAL`, Variante `REAL`); **`Tab_Energieanlagen` trägt keine** | bestätigt; Pflegestand: BHKW 3 von 6, Kessel 1 von 22, Variante 13 von 13 |
| § 2.13 (3) Nr. 5 | Speicherflotte „führt ihren Ersatz über ein handgepflegtes `ErsatzintervallJahre` und einen `RestwertEuro`" | **keine Spalten**: `SpeicherEngine/FlottenModel.cs:541` und `:544` sind JSON-Felder des Flottenstands, der über `JsonSerializer` in `Tab_SpeicherAuslegung` liegt (`SpeicherFlottenStudieCtrl.cs:7,177,712`) | **Konzept ungenau** — und dadurch einfrierrelevant, siehe § 3.6 |
| § 2.15 / § 2.16 | Schritte 92/93 | `Tab_ProjektWirtschaftlichkeit.ID_Referenzprojekt` (INTEGER, nullbar) und `Tab_ProjektPhotovoltaik.Uebernahme_Stamm` (INTEGER) beide vorhanden | **bestätigt** |
| § 3.9 / § 6.3 Nr. 17 | Nachweisumschlag `Nachweis_Json`, „SpalteSicher, ohne Schemaschritt" | `Tab_ErgebnisWirtschaftlichkeit.Nachweis_Json TEXT` vorhanden, angelegt in `WirtschaftlichkeitCtrl.cs:527` als `LONGTEXT` → `TEXT`; in `SchemaMigration` kein Schritt | **bestätigt**; in der Testdatenbank ist sie in **0 von 78** Zeilen gefüllt |
| § 6.5 „Zwei Migrationsmechanismen — vier Tabellen" | vier | **fünf** `CREATE TABLE` plus eine fremde Tabelle über `SpalteSicher` (`Tab_ProjektPhotovoltaik`) | **Konzept unterzählt**, siehe § 2 |

**Nicht gefunden, obwohl das Konzept sie benennt:** keine Spalte `Satz`, kein `BestCase`/`WorstCase` in dieser Schreibweise (die Spalten heißen `Bestcase`/`Worstcase` samt `Bestcase_Nutzungsdauer`/`Worstcase_Nutzungsdauer`), keine Nutzungsdauerspalte an `Tab_Energieanlagen`.

---

## 2 Zwei Migrationsmechanismen (§ 6.5)

### 2.1 Was der Selbst-DDL führt

`WirtschaftlichkeitCtrl.StelleTabellenSicher` (`WirtschaftlichkeitCtrl.cs:260–580`):

| Art | Ziel | Zahl | Beleg |
|---|---|---:|---|
| `Ddl(CREATE TABLE IF NOT EXISTS …)` | `Tab_ProjektWirtschaftlichkeit` | 1 | `:273` |
| | `Tab_ErgebnisWirtschaftlichkeit` | 1 | `:307` |
| | `Tab_ErgebnisWirtSensitivitaet` | 1 | `:341` |
| | `Tab_ProjektTarif` | 1 | `:355` |
| | `Tab_ErgebnisStromMatrix` | 1 | `:375` |
| `Ddl(CREATE UNIQUE INDEX)` | `UQ_ProjWirtProj`, `UQ_ProjTarifProj` | 2 | `:299`, `:367` |
| `SpalteSicher` | `Tab_ErgebnisWirtschaftlichkeit` | 30 | `:390–527` |
| | `Tab_ProjektWirtschaftlichkeit` | 15 + 1 Liste | `:395–465`, `:565` |
| | `Tab_ErgebnisStromMatrix` | 1 | `:469` |
| | `Tab_ProjektPhotovoltaik` (fremde Tabelle) | Liste `Schritt93_VerguetungJeVariante` | `:574` |

Zusammen **5 Tabellen mit eigenem CREATE, 55 `SpalteSicher`-Aufrufe, 6 berührte Tabellen**. Die Übersetzung der Access-Typangaben nach SQLite steht in `EPOS.Kern/Allgemein/Simulation/StilleDb.cs:325` (`AlterTableAddColumn`) und `SqliteSpaltenTyp` (`:259–290`): `YESNO` → `INTEGER NOT NULL DEFAULT 0 CHECK (x IN (0,1))`, `YESNO_NULL` → `INTEGER CHECK (x IN (0,1))`, `DOUBLE`/`REAL` → `REAL`, `LONG`/`INTEGER` → `INTEGER`, `LONGTEXT`/`MEMO` → `TEXT` ohne Längenprüfung.

### 2.2 Was `SchemaMigration` führt

`WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs`. Einstieg `Ausfuehren`, `ZIEL_VERSION = SchemaStand.Zielversion` (`:108`), `FREEZE_VERSION = 61` — **die Schritte 1–61 stehen nicht im Programm** (`:10–26`), sie sind der Access-Freeze-Stand. Die Schritte 62–94 sind registriert (`:4173` ff., 90–94 bei `:4579–4652`). `Tab_ProjektWirtschaftlichkeit` bekommt ihre Spalten in den Schritten 20, 21, 28, 71, 72, 83, 84, 88, 90, 91, 92; `Tab_ProjektTarif` in 32 ff.

### 2.3 Überschneidung, Regelverstoß, Umbaukosten

**Sie überschneiden sich vollständig.** Jede Spalte, die ein Schritt an `Tab_ProjektWirtschaftlichkeit`, `Tab_ErgebnisWirtschaftlichkeit`, `Tab_ErgebnisStromMatrix` oder `Tab_ProjektPhotovoltaik` anlegt, muss ein zweites Mal als `SpalteSicher` eingetragen werden — der Quelltext sagt das selbst an fünf Stellen („DER ZWEITE DDL-ORT", `:61`, `:70`; „doppelte Schema-Wahrheit dieses Moduls", `:81`), und der Konzepttext § 2.15 (Z. 569) schreibt es als Pflicht fest: „wie bei jeder Spalte dieser Tabelle an **beide** DDL-Orte". Die Gegenrichtung stimmt ebenfalls: `KwkgProjektaltspalten` (Schritt 91) musste das `SpalteSicher` für `KWKG_Kostenanteil` **entfernen**, sonst hätte der nächste Programmstart die gelöschte Spalte wieder angelegt (`WirtschaftlichkeitCtrl.cs:84–85` des Kommentarblocks). Das ist die eigentliche Gefahr: Der Selbst-DDL kann Migrationsschritte **rückgängig machen**.

**Verstöße:**

| Regel | Fundstelle | Verstoß |
|---|---|---|
| ADR-001 „Schemaänderungen laufen als nummerierte Schritte über `SchemaMigration`" (`CLAUDE.md:76–78`) | ADR-001 „Entscheidung", Z. 63–88 | **ja.** Der Selbst-DDL ist wörtlich die dort verworfene **Option A/B**-Bauart: lazy, prozessweit einmalig, ohne Versionsmarker, ohne Fehlermeldung. ADR-001 Z. 141–144 nennt genau das als Contra: „kein definierter Zeitpunkt, an dem ein Anwender über einen Fehlschlag informiert würde" |
| „Fachtabellen sind `STRICT`" (`CLAUDE.md:75`) | `WirtschaftlichkeitCtrl.cs:273–389` | **ja.** Keiner der fünf `CREATE TABLE` trägt `STRICT`. In der Testdatenbank sind alle fünf Tabellen `STRICT` — also stammen sie dort aus der Migration bzw. der Vorlage; der CREATE-Pfad würde sie **non-STRICT** neu anlegen |
| „Neue Beziehungen über IDs" / Fremdschlüssel | `:273–389` | **ja.** Kein `CREATE` setzt einen Fremdschlüssel. Gemessen: `Tab_ErgebnisWirtschaftlichkeit` hat 0 FK, obwohl `:310` `ID_Ergebnis` als „FK auf `Tab_Ergebnis.ID`" kommentiert; `Tab_ErgebnisWirtSensitivitaet` und `Tab_ErgebnisStromMatrix` haben 0 FK. Die vorhandene FK auf `Tab_ProjektWirtschaftlichkeit.ID_Projekt` stammt aus der Migration, nicht aus dem CREATE |
| „Boolean-Spalten … mit `CHECK (spalte IN (0,1))`" (`CLAUDE.md:81`) | `StilleDb.cs:263`, `:272` | **nein.** `SpalteSicher` hält die Regel ein; der `CREATE` von `Tab_ProjektWirtschaftlichkeit` schreibt `KWKG_Pauschalmodus … CHECK (… IN (0,1))` und `IstStamm` ebenso |
| stille `catch { }` um jeden Block | `:275`, `:305`, `:339`, `:352`, `:373` und `SpalteSicher` `:604` | Fehler bleiben unsichtbar — das ist bewusst („Verhaltenstreue", `:612–619`), widerspricht aber ADR-001 „Fehler werden gesammelt und einmal gemeldet" |

**Praktische Wirkung heute: gering.** Der `CREATE`-Pfad läuft nur, wenn `TabelleVorhanden` falsch meldet. Im Normalbetrieb kopiert `EPOS.Kern/Allgemein/Datenbank/Erstbereitstellung.cs` eine vollständige Vorlage (`CLAUDE.md:70–71`), die alle fünf Tabellen `STRICT` mitbringt; die Migration prüft danach nur noch den Marker. Der Selbst-DDL ist also eine **latente**, keine wirksame Verletzung — mit der aktiven Nebenwirkung aus dem `SpalteSicher`-Teil (Wiederanlage gelöschter Spalten).

**Kosten eines Umbaus auf einen Mechanismus.** Der saubere Weg ist, `StelleTabellenSicher` zu entkernen: die fünf `CREATE` streichen (sie sind im Normalbetrieb tot), die 55 `SpalteSicher` streichen und die Schritte als Pflichtvorbedingung behandeln. Aufwand und Risiko:

- **Die fünf CREATE:** je ein Schemaschritt „Tabelle anlegen, falls sie fehlt, STRICT, mit FK" — oder, billiger und ehrlicher, ersatzloses Streichen mit einer klaren Startmeldung, wenn eine Tabelle fehlt. Eine Datei ohne diese Tabellen ist keine EPOS-Plan-Datenbank; `Erstbereitstellung` prüft das bereits über `Tab_Applikation` (`:101–102`).
- **Die 55 `SpalteSicher`:** entfallen, sobald verlässlich feststeht, dass der Marker die Spalten deckt. Genau das leistet `SchemaMigration` seit dem Freeze-Stand; eine nie migrierte Datei kann nicht mehr auftauchen, weil `EposSqliteMigrator` den Weg von Access nach SQLite hält (`SchemaMigration.cs:20–25`).
- **Der harte Teil ist die Rückfallebene** `SchemaKatalog.Alle`. Das Szenarienkonzept begründet zweimal wortgleich (§ 3, Z. 161–167; § 10.3, Z. 457–461), warum die `Tab_ProjektWirtschaftlichkeit`-Spalten dort **nicht** geführt werden: „die tolerante Vorsorge steht unmittelbar vor dem Zugriff in `WirtschaftlichkeitCtrl.StelleTabellenSicher`". Wer den Selbst-DDL abschaltet, muss diese Begründung kassieren und die Spalten in `SchemaKatalog.Alle` aufnehmen — sonst fällt beides weg.
- Geschätzte Größenordnung: ein eigener Auftrag, kein Nebenschritt. Betroffen sind drei Dateien (`WirtschaftlichkeitCtrl.cs`, `SchemaKatalog.cs`, `SchemaMigration.cs`), die Wachen in `EPOS.Kern.Tests` und die beiden Konzeptbegründungen.

**Verlangt das Konzept den Umbau? Nein.** § 6.5 führt die Doppelung als benannte, begründete Doppelung mit der Auflage „neue Spalten gehören an beide Stellen" — das ist eine Pflegeregel, kein Umbauauftrag. Auch § 2.15 (Z. 569) schreibt die Doppelpflege fort statt sie zu beenden. Die Auflösung ist also **Empfehlung dieses Berichts**, nicht Konzeptvorgabe.

---

## 3 Schemabedarf der offenen Konzeptpunkte

Vorbemerkung zu zwei Spalten der Tabellen unten:

- **Ergebnisneutral bis zur ersten Pflege:** „ja" heißt, der Schritt legt nur Spalten an, ohne DML und ohne DDL-DEFAULT, und NULL/0 bedeutet „wie bisher". Das ist das Muster der Schritte 70–72 und 92.
- **Einfrierregel betroffen:** Die Einfrierregeln der `CLAUDE.md:146–155` sind abschließend — Emissionsfaktoren, PV-Modulkoeffizienten, Flottenstand `@Projektflotte` des Projekts 1046. Der Referenzlauf rechnet Simulationen, keine Wirtschaftlichkeit (Szenarienkonzept § 3, Z. 168–169), deshalb ist er bei Wirtschaftlichkeitsspalten grundsätzlich unberührt.

### 3.1 K-1 — Stromkennzahl und Abwärmeabfuhr (§ 3.6, Z. 1839–1862)

| | |
|---|---|
| Tabelle | `Tab_Energieanlagen` |
| Spalten | `KWKG_Abwaermeabfuhr` INTEGER NOT NULL DEFAULT 0 CHECK (… IN (0,1)) · `KWKG_Stromkennzahl` REAL, nullbar (NULL = kein Wert, Vorschlag aus P_el/P_th der Gerätezeile) |
| DDL/DML | reines DDL, kein DML |
| ergebnisneutral | **ja** — 0 ist der heutige Fall 1 (gesamte Nettostromerzeugung ist KWK-Strom) |
| Einfrierregel | nein |
| Testdatenbank / Vorlage / Erstbereitstellung | Testdatenbank zieht über `Werkzeuge/Testdatenbankschema` nach; Auslieferungsvorlage unberührt (`Projektsicht` entdeckt Tabellen über `ID_Projekt`/`ProjektID`, nicht über Spaltenlisten — `Projektsicht.cs:61,75`); Erstbereitstellung unberührt |
| Schrittnummer | **95** |

Das Konzept nennt an dieser Stelle „nächster freier Schemaschritt ist **92**" (Z. 1851) — überholt, 92–94 sind vergeben.

### 3.2 Szenariospalten § 2.11.5

Drei getrennte Schritte, weil sie drei Tabellen treffen. **Alle drei: reines DDL, kein DML, ergebnisneutral ja, Einfrierregel nein, Vorlage/Erstbereitstellung unberührt.**

| Schritt | Tabelle | Spalten | Anmerkung |
|---:|---|---|---|
| **96** | `Tab_ProjektWirtschaftlichkeit` | `Szen_Best_Zeitraum` REAL nullbar · `Szen_Worst_Zeitraum` REAL nullbar · `Szen_Best_Menge` REAL nullbar · `Szen_Worst_Menge` REAL nullbar | Damit ist der „Rahmen" des § 2.11.5 komplett: Zins/p_E/p_B stehen seit Schritt 71. **Namensvorsicht:** nicht `Szen_*_Dauer` nehmen — das ist die Nutzungsdaueränderung. `Szen_*_Menge` ist der Mengenfaktor [%] |
| **97** | `energy_project_settings` | `custom_price_work_best/_worst` · `custom_price_base_best/_worst` · `custom_price_power_best/_worst`, je REAL nullbar | sechs Spalten; NULL = wie `custom_price_*` |
| **98** | `Tab_ProjektWirtschaftlichkeit` + `Tab_ProjektPhotovoltaik` | PW: `Einspeiseverguetung_Best/_Worst`, `Einspeiseverguetung_KWK_Best/_Worst` · PPV: `DvEntgelt_Best/_Worst`, `PpaPreis_Best/_Worst` | Erlössätze. Die PV-Felder gehören an `Tab_ProjektPhotovoltaik`, weil § 2.16 die Vergütung je Variante führt (`Uebernahme_Stamm`) |

Für 97 und 98 gilt der zweite DDL-Ort **nicht** (`energy_project_settings` und `Tab_ProjektPhotovoltaik` gehören nicht dem `WirtschaftlichkeitCtrl`) — außer `Tab_ProjektPhotovoltaik`, die er über `Schritt93_VerguetungJeVariante` bereits anfasst (`:574`); dort ist der Nachtrag Pflicht. Für 96 ist er Pflicht.

### 3.3 Kennzeichen Ersatz / Restwert (§ 2.13 (3) Nr. 1)

| | |
|---|---|
| Tabelle | `Tab_ProjektWerte` und `Tab_KostenVorlagePosition` (Muster wie `NutzungsdauerID`, das an beiden steht) |
| Spalten | `ErsatzFuehren` INTEGER **nullbar** CHECK (… IN (0,1)) · `RestwertAnsetzen` INTEGER **nullbar** CHECK (… IN (0,1)) |
| NULL-Semantik | **NULL = wie bisher** (beides an, sobald eine Nutzungsdauer da ist). Ein `NOT NULL DEFAULT 1` wäre hier falsch: es nähme der Nullsemantik die Aussage, genau wie Schritt 71 es begründet (`SchemaKatalog.cs:4067–4071`) |
| DDL/DML | reines DDL |
| ergebnisneutral | **ja** |
| Einfrierregel | nein |
| Testdatenbank | ja (Spalten) · Vorlage: die 120 Auslieferungspositionen bleiben NULL · Erstbereitstellung: unberührt |
| Schrittnummer | **99** |

**Alternative „je Technik":** die beiden Kennzeichen an `Tab_Nutzungsdauer` statt an der Position. Billiger (28 statt 284 Zeilen), aber der Konzepttext sagt „je Position **oder** je Technik" — das ist ein Anwenderentscheid, kein technischer. Empfehlung: an der Position, weil die Positionsart über `NutzungsdauerID` ohnehin die Technikzeile erreicht und die Position der feinere Ort ist.

### 3.4 Pflegeort Positionsart (`NutzungsdauerID`) — **erledigt, Konzept überholt**

§ 2.13 (3) Nr. 3 sagt: „`NutzungsdauerID` wird ausschließlich bei der Vorlagenübernahme gesetzt, kein Dialog lässt sie wählen." Gemessen und widerlegt:

- Der Zeileneditor führt die Positionsart: `WindowsFormsApplication1/Views/Kosten/KostenKomponenteHuelle.cs:1153–1159` reicht `Positionsarten` und `PositionsartId = b.Position.NutzungsdauerId` an den Dialog.
- Der Knopf existiert: Ressource `Nutzungsdauern vorbelegen…` (`EPOS.Kern/MyResource/Resource.resx:19195`), Dialogparameter `VorbelegenText` (`EPOS.UI/Dialoge/Kosten/KostenKomponenteDialog.razor:595`), Umsetzung `:994` „U8 (Stufe S2, Anwenderentscheid ND‑Q4 (b))", Windows-Hülle `KostenKomponenteHuelle.cs:254,562`, Test `EPOS.UI.Tests/Dialoge/KostenKomponenteDialogTests.cs:1778`.
- Das Nutzungsdauer-Konzept führt Stufe S2 selbst als **umgesetzt** (§ 6, Z. 261), Statuszeile **#357** bestätigt es.

**Kein Schemabedarf.** Dasselbe gilt für Nr. 2 (Nachpflege des Bestands). Gemessene Folge der späten Umsetzung: `Tab_ProjektWerte.NutzungsdauerID` ist in **0 von 164** Zeilen gesetzt, in `Tab_KostenVorlagePosition` dagegen in **31 von 120** — der Bestand ist noch nicht nachgepflegt, das Werkzeug dafür ist aber gebaut.

### 3.5 Geräteeigene Nutzungsdauer-Spalten (§ 2.13 (3) Nr. 4) — abkündigen?

| Spalte | Typ | gefüllt | Leser in der Wirtschaftlichkeit |
|---|---|---|---|
| `Tab_BHKW.Nutzungsdauer` | INTEGER | 3 von 6 | keiner |
| `Tab_Heizkessel.Nutzungsdauer` | REAL | 1 von 22 | keiner |
| `Tab_StromspeicherVariante.Nutzungsdauer` | REAL | 13 von 13 | **die Speicherwirtschaftlichkeit**, nicht der Kapitalwertrechner |

**Empfehlung: nicht abkündigen, sondern kennzeichnen.** Die ersten beiden sind Katalogangaben der Gerätezeile und schaden nicht, solange kein Rechner sie liest; ein Löschschritt wäre Aufwand ohne Ertrag und träfe die Gerätedialoge. Die dritte hat einen echten Leser. Der belastbare Schritt ist der umgekehrte: `Tab_Nutzungsdauer` führt bereits die Positionsarten „Batterie" (ID 20, 10 a) und „Wechselrichter / Leistungselektronik" (ID 21, 12 a) für Komponente 5 — die Speichervariante sollte sie lesen, statt eine eigene Zahl zu führen. Das ist **kein Schemaschritt, sondern Rechenweg**.

### 3.6 Anschluss der Speicherflotte (§ 2.13 (3) Nr. 5) — **der einzige einfrierrelevante Punkt**

| | |
|---|---|
| Ablage | **keine Spalten.** `ErsatzintervallJahre` (int) und `RestwertEuro` (double) sind Felder von `SpeicherEngine/FlottenModel.cs:541,544`, der Flottenstand liegt als JSON in `Tab_SpeicherAuslegung` (`EPOS.Kern/Controller/SpeicherFlottenStudieCtrl.cs:7,177,712`) |
| DDL/DML | **kein DDL** — der Anschluss an `Tab_Nutzungsdauer` ist eine Änderung am Flottenmodell und an seinem JSON |
| ergebnisneutral | **nein**, sobald die Ableitung greift: `FlottenWirtschaftlichkeit.cs:54–55` bucht Ersatz in jedem Jahr, dessen Nummer durch `ErsatzintervallJahre` teilbar ist |
| **Einfrierregel** | **JA.** `CLAUDE.md:155`: „der Flottenstand `@Projektflotte` des Projekts 1046 in `Tab_SpeicherAuslegung` und dessen Projektzeilen". Wer den Flottenstand anfasst, friert die Basis im selben Schritt neu ein und begründet es in `Referenzlaeufe/LIESMICH.md` |
| Testdatenbank / Vorlage | Testdatenbank ja (Projekt 1046) · Vorlage: `Tab_SpeicherAuslegung` ist projektbezogen, wird also geleert |
| Schrittnummer | **keine** — eigener Auftrag mit Neueinfrieren, nicht in die Schrittkette 95–102 einreihen |

### 3.7 `SteuerErgebnis`-Trennung (§ 2.13, Z. 373; § 6.3 Nr. 9a, Z. 2261)

Nicht persistiert. `SteuerErgebnis.EnergiesteuerEur` ist eine Rechengröße; die Ergebnistabelle führt eine einzige Spalte `EnergiesteuerErloes` REAL. Das Mockup-Papier hat die Trennung bereits als Programmänderung an `SteuerGutschriftRechner.cs` eingeplant („`SteuerErgebnis` mit zwei Beträgen (§ 53/53a und § 54), zwei Rubrikzeilen", § 3.3, Schwere hoch).

**Schemabedarf nur, wenn der Ausweis persistiert werden soll.** Dann: `Tab_ErgebnisWirtschaftlichkeit` + `EnergiesteuerErloes_53` und `EnergiesteuerErloes_54` REAL nullbar, Altspalte als Summe stehen lassen. **Empfehlung: nicht persistieren** — die Trennung gehört in den Nachweisumschlag `Nachweis_Json`, der genau dafür da ist (§ 6.3 Nr. 17) und heute in 0 von 78 Zeilen gefüllt ist. Kein Schritt.

### 3.8 Anlagenbezug der Erlöszeilen (§ 2.13 (4))

Persistiert oder Umschlag? **Umschlag.** Die Erlöszeilen entstehen im Rechenweg (`SteuerGutschriftRechner.cs`, `WirtschaftlichkeitZeilen.cs`) und werden nicht je Zeile gespeichert; die Ergebnistabelle führt Summen (`EinspeiseerloesPV`, `EinspeiseerloesKWK`, `VermiedenArbeit`, `VermiedenLeistung`, `VermiedenGesamt`, `AufschlagBetrag`). Eine Persistierung je Anlage wäre eine neue Ergebnisdetailtabelle — deutlich mehr als der Konzepttext verlangt („Die Zeilen des Katalogs brauchen einen Anlagenbezug (Schlüssel je Anlage wie bei den Energiekosten-Unterzeilen)").

**Kein Schemaschritt.** Der Anlagenbezug ist ein Feld der Zeilendefinition im Kern plus eine Herkunftszeile im `Nachweis_Json`; das Muster steht schon (§ 2.16, Z. 1280: „die Herkunft reist im Nachweisumschlag der Ergebniszeile, **keine neue Ergebnisspalte**"). Gemessene Randbedingung: die Aufteilung je Anlage braucht Eigenverbrauchsmengen aus `Tab_ErgebnisStromMatrix` — die Tabelle führt `KwkEigenMWh`/`EinspPvMWh` nur **je Zone**, nicht je Anlage (10 Spalten, 8 Zeilen). Ob das reicht, ist vor der Umsetzung zu prüfen.

### 3.9 `ID_Umrechnung`-Altlast der Preisbasis („Nach #341")

Der offene Punkt steht in `Dokumentation/aktuell/Status_iOS_Migration.md:400`: „`UR-1` Schritt 2 — die Preisbasis-Kennung ist eine Altlast … Sauber wäre eine eigene, kleine Spalte (Kartenzustand statt Regelkennung); das ist ein Schemaschritt und war nicht beauftragt."

| | |
|---|---|
| Tabelle | `energy_project_settings` |
| Spalten | `Preisbasis` TEXT nullbar (Wert: die gewählte Einheit, z. B. `kWh` oder die Abrechnungseinheit; NULL = Abrechnungseinheit) |
| DML | einmalig: wo `ID_Umrechnung` auf eine aktive Regel `Abrechnungseinheit → kWh` zeigt, `Preisbasis := 'kWh'`. Danach `ID_Umrechnung` stehen lassen (die Regeln prüfen weiter die Einheitenkette) oder in einem Folgeschritt abräumen |
| ergebnisneutral | **ja** — gespeichert ist ohnehin der Basispreis je Abrechnungseinheit; es geht nur die Eingabegewohnheit verloren bzw. wieder ein |
| Einfrierregel | nein |
| Gemessen | 28 von 28 Zeilen tragen einen Wert; **genau eine** steht auf `-1` (der beschriebene Fehlschlag), die übrigen auf gültigen Regel-IDs (29, 35, 36, 40, 51, 52, 53, 60) |
| Schrittnummer | **100** |

### 3.10 Einheitenbruch U-1 (§ 5, Z. 2178) — **Schritt 62 ist NICHT gelaufen**

Das Konzept schreibt in der Kopfzeile (Z. 3) „Schemaschritt 62 vergeben (U-1)" und in § 5 „als **Schemaschritt 62** (Muster Schritt 26a)". Gemessen:

| Prüfung | Ergebnis | Beleg |
|---|---|---|
| Was ist Schritt 62? | `Schritt_62_KlimaWaisen` — zwei `DELETE` auf Klimadaten, nichts mit Einheiten | `SchemaMigration.cs:4173`, `:5025` |
| Tragen die fünf Gase `Nm³` in `Tab_Brennstoff_Stamm.Einheit`? | **nein.** Brennstoff 1 Stadtgas, 2 Erdgas LL, 3 Erdgas E, 14 Biogas, 25 Wasserstoff: `Einheit = m³`, `PreisEinheit = €/m3` | Messung an der Kopie |
| Trägt `energy_carrier.billing_unit` `Nm³`? | **ja**, 8 von 27 Trägern — das ist Schritt **26a**, gelaufen im Access-Freeze-Bereich | `SchemaMigration.cs:737–742`; Messung |
| Bricht es noch in Projektdaten durch? | **ja**: `energy_price.arbeitspreis_unit` führt 19 × `Nm³`, aber **1 × `m³`** | Messung |

**Der Schritt steht also aus.** Neuer Vorschlag:

| | |
|---|---|
| Tabelle | `Tab_Brennstoff_Stamm` (+ `energy_price` als Aufräumen) |
| DDL/DML | **reines DML**, kein DDL: `Einheit` `m³`→`Nm³` und `PreisEinheit` `€/m3`→`€/Nm³` bei den fünf Gasen; `energy_price.arbeitspreis_unit` `m³`→`Nm³` (1 Zeile) |
| ergebnisneutral | **ja** — der Stammtext ist Anzeige und Ableitungsschlüssel, kein Rechenwert; `Hi`/`Hs` sind seit jeher Normwerte (`SchemaMigration.cs:751`) |
| Einfrierregel | **nein**, aber prüfen: `Tab_Brennstoff_Stamm` steht in der Einfrierliste nur mit `CO2/SO2/NOx/Staub` (`CLAUDE.md:151`) — `Einheit`/`PreisEinheit` sind nicht genannt |
| Testdatenbank / Vorlage | Testdatenbank ja (die 5 Zeilen) · Vorlage: `Tab_Brennstoff_Stamm` ist ein `ReadOnly`-Katalog und bleibt stehen (`Vorlagenbau.cs:156`), also muss der Schritt vor dem Vorlagenbau laufen |
| Schrittnummer | **101** |

**Randfrage: gibt es das Einheitenbruch-Konzept inzwischen in `aktuell/`?** **Nein.** `Konzept_Einheitenbruch_Energietraeger_EPOS-Plan.md` liegt in `Dokumentation/ueberholt/`, nicht in `aktuell/`. Es ist also gemergt worden (§ 5 Z. 2189–2190 „nur auf Zweig `claude/lucid-cori-a9a425` … noch nicht gemergt" ist überholt), aber gleich als überholt abgelegt — sein Bezugsstand ist Access mit `SchemaVersion 61`. Die gleichnamige Datei `Dokumentation/aktuell/Konzept_Einheiten_EPOS-Plan.md` ist **ein anderes Papier** (kWh gegen MWh, Rev. 2 vom 07.09.2026, Stufe S1 umgesetzt) und behandelt U-1 nicht.

### 3.11 Abkündigung geräteeigener Spalten — optionaler Schritt 102

Falls der Anwender die zweite Wahrheit wirklich beenden will: `Tab_BHKW.Nutzungsdauer` und `Tab_Heizkessel.Nutzungsdauer` entfernen (Muster `StrompreisAltspalten`, das fünf Spalten ohne Leser ersatzlos gezogen hat, `StrompreisAltspalten.cs:14–40`). `Tab_StromspeicherVariante.Nutzungsdauer` **nicht** — sie hat einen Leser. Ergebnisneutral ja, Einfrierregel nein, kein DML. Empfehlung: zurückstellen, siehe § 3.5.

---

## 4 Doppelte Wahrheiten (§ 6.5) — Stand heute

| Zeile des Konzepts | Gemessener Stand | Beleg |
|---|---|---|
| **Stromsteuersatz Katalog gegen `const`** | **besteht fort, aber entschärft.** Katalog: `STROMST_REGELSATZ` 2026 = 20,5 EUR/MWh, Status GESICHERT (gemessen, 226 Zeilen in `Tab_Gesetzesparameter`); Konstante: `StrompreisZerlegungModel.STROMSTEUER_REGELFALL = 2.050` ct/kWh. **Wertgleich.** Der Rechenweg liest den Katalog, nicht die Konstante (`StrompreisZerlegungModel.cs:71–72`, `SteuerGutschriftRechner.cs:685`); die Konstante ist Anzeigevorgabe und Summenglied (`:103`, `:144`). **Die „Wache" ist keine Kopplung:** `StrompreisZerlegungTests.cs:40,94` prüft Modell gegen Konstante, nicht Konstante gegen Katalog — eine gepflegte Novelle erreicht die Konstante weiterhin nicht. Das Konzept beschreibt das korrekt | `GesetzKatalog.cs:1136`; `StrompreisZerlegungModel.cs:78` |
| **„Energieintensiv" an drei Orten** | unverändert wie beschrieben: Unternehmensart (`Tab_ProjektWirtschaftlichkeit.Unternehmensart` TEXT), Schnellwahl im Trägerdialog, Katalogsatz. Nicht weiter geprüft (Oberflächenfrage, vom Mockup-Papier § 3.3 abgedeckt) | — |
| **BHKW-Einspeisevergütung an vier Orten** | **nur noch drei.** Die vier waren: (1) `Tab_ProjektWirtschaftlichkeit.Einspeiseverguetung_KWK`, (2) `Tab_ProjektTarif.Einsp_Arbeit` (aktiver Tarif, hat Vorrang), (3) `Tab_Energieanlagen.KWKG_Satz_Einspeisung` je Anlage, (4) `energy_project_settings.Verguetung_BHKW`. **(4) ist fort** — Schritt 84 hat sie nach `Tab_ProjektWirtschaftlichkeit` umgezogen („dort steht seither die **eine** Wahrheit"), Schritt 85 hat die Altspalte entfernt; in der Testdatenbank existiert sie nicht mehr. Gelesen werden (1), (2) und (3) | `StrompreisAltspalten.cs:23–27`; `VerguetungUmzug.cs:14–43`; Messung `energy_project_settings` 40 Spalten ohne `Verguetung_BHKW` |
| **Zwei Migrationsmechanismen** | besteht, **größer als beschrieben**: fünf Tabellen mit CREATE statt „vier", 55 `SpalteSicher`, sechs berührte Tabellen | § 2 dieses Berichts |
| **Zwei Lesewege auf die Kostenposition** | **besteht.** Die gespeicherte Access-Abfrage existiert weiter — als SQLite-**Sicht** `Abfrage_Kostenfaktoren` (gemessen, in `sqlite_master` als `view`). Sie ist kein Access-Artefakt mehr: `Microsoft.ACE`/`OleDb` sind aus `EPOS.Kern` verschwunden (die Treffer sind ausschließlich erklärende Kommentare, z. B. `DataRepository.cs:30` „weder im Quelltext noch als PackageReference"). Die Sicht ist aktiv genug, dass `ProjektWerteLoeschschutz` sie beim Tabellenumbau eigens behandeln muss (`:65–70`) und eine Wache sie hält (`EPOS.Kern.Tests/ProjektWerteSchemaWacheTests.cs:34`). Der direkte Zugriff bleibt der Normalfall (`KostenPositionCtrl.cs:656` „Eigene Abfrage statt Erweiterung von `Abfrage_Kostenfaktoren`") | `WirtschaftlichkeitCtrl.cs:6179`; `SchemaKatalog.cs:3222` |
| **Komponenten-IDs hart verdrahtet (`Form_Kosten` gegen `UcBkKosten`)** | **beide Klassen gibt es im Produkt nicht mehr.** `Form_Kosten` existiert nur noch als Prüfmuster in `Werkzeuge/Formularkarte.Tests/Pruefmuster/Kosten/Form_Kosten.Auszug.cs`; `UcBkKosten` gar nicht. **Der Razor-Stand:** die Kostenseite liegt in `EPOS.UI/Dialoge/Kosten/` (u. a. `KostenKomponenteDialog.razor`, `VorlagenZeile.razor`, `KostenfaktorKatalogDialog.razor`) mit den Windows-Hüllen unter `WindowsFormsApplication1/Views/Kosten/` und `…/BerichteKosten/`. Die Unterscheidung „hart verdrahtet gegen dynamisch" ist damit im Kern gelandet: `KostenVorlagenCtrl.IstErfassungsgruppe` (`:112`), gelesen von `KostenSeiteGaben.cs:493,504`. **Die Konzeptzeile ist gegenstandslos und zu streichen** | — |
| **Vorrang Projekt vor Katalog in drei Implementierungen** | **zwei sind nachweisbar, die dritte ist die Sicht.** `KostenEmissionRechner.cs:1126` „Vorrangkette: Projektwert → PREISHISTORIE zum Stichtag → Katalogwert → null", dazu `:1181`. In `StromPreisCtrl.cs` habe ich keine gleichlautende Kette gefunden (nur `FIXPREIS_RUECKFALL_CT_KWH`, `:130`) — die dritte ist die im Konzept genannte Abfrage, also `Abfrage_Kostenfaktoren`. **Teilbefund, nicht abschließend** | — |
| ~~Kennzahlenliste dreifach~~ | aufgelöst, wie das Konzept sagt | — |

---

## 5 Fremdschlüssel und Löschschutz

### 5.1 Was `Tab_ProjektWerte` trägt

Fünf Fremdschlüssel, gemessen:

| Spalte | Ziel | ON DELETE |
|---|---|---|
| `ProjektID` | `Tab_Projekt.ID` | CASCADE |
| `KomponentenID` | `Tab_KostenKomponente.ID` | NO ACTION |
| `Gruppe` | `Tab_KostenGruppenKatalog.GruppenName` | NO ACTION |
| `StammID` | `Tab_Kostenfaktor.StammID` | **RESTRICT** |
| `NutzungsdauerID` | `Tab_Nutzungsdauer.ID` | NO ACTION |

**Auf Anlage und Vorlage trägt sie keine.** `ID_Anlage`, `ID_AnlageGeraet` und `VorlageID` stehen ohne Fremdschlüssel da. Der Löschschutz des Schritts 79 (`ProjektWerteLoeschschutz.cs`) betrifft ausschließlich `StammID` → `Tab_Kostenfaktor`: Die alte Beziehung trug `ON DELETE CASCADE`, ein Löschen im Kostenfaktor-Katalog hätte Projektpositionen stillschweigend mitgenommen; RESTRICT statt NO ACTION wurde gewählt, „damit die Ablehnung an dem DELETE hängt, das sie auslöst" (`:38–41`).

### 5.2 Was beim Löschen einer Anlage mit Positionen passiert

**Nichts auf der Datenbankseite** — mangels Fremdschlüssel auf `ID_Anlage` bleibt die Position stehen und trägt eine tote Anlagen-ID. Genau dafür ist die gelbe Zeile des § 2.14 da: „Ihre Anlage ist gelöscht oder der Verweis leer — sie **hatte** eine", gerendert als „{0} — ohne Anlagenzuordnung" mit Papierkorb. Die Unterscheidung gegen die Erfassungsgruppe trifft `KostenVorlagenCtrl.IstErfassungsgruppe` im Kern (`:112`), die Statuszeile meldet nur die gelben (`KostenSeiteGaben.cs:504`). Das ist eine bewusste Entscheidung: Ein CASCADE würde erfasste Kosten vernichten, ein RESTRICT das Löschen der Anlage blockieren. **Für die Umsetzung heißt das: keinen Fremdschlüssel auf `ID_Anlage` nachrüsten**, ohne den Entscheid neu zu stellen.

### 5.3 Waisenzählung in der Testdatenbank

| Frage | Gemessen |
|---|---|
| Positionen mit `ID_Anlage`, zu der es keine `Tab_Energieanlagen`-Zeile gibt | **0** — keine echten Waisen |
| Positionen ohne Anlage (`ID_Anlage` NULL oder 0) | **10** von 164: 8 in Gruppe „Allgemein" (Projekte 1040, 1041, 1043, 1044; sechs davon mit Betrag 3.775,00 / 3.000,50 €), 2 ohne Gruppe (Projekt 1007, Betrag 0) |
| Anlagen ohne Träger (`ID_Carrier` NULL oder 0) | **113 von 130** — erwartbar, nur brennstoffführende Anlagen brauchen einen |
| Anlagen mit `ID_Carrier`, den es in `energy_carrier` nicht gibt | **0** |
| Energieanlagen mit gefüllten `KWKG_*` | **9**; davon **7 ohne BHKW-Modul** (`ID_BHKW = 0`) |
| — davon mit echten Sätzen | nur **2** (IDs 14920, 14921, Projekt 1030, je 8,0/4,0 €/MWh), und beide **haben** ein Modul |
| — die übrigen 7 | tragen `KWKG_Anlagenart = ''` (Leerzeichenkette, nicht NULL) bei Projekten 1032 und 1043, sonst alles 0 |
| Hauptkomponentenzeilen (`Tab_Kostenfaktor.IsMainComponent = 1`) in `Tab_ProjektWerte` | **63**; davon 25 mit Gruppe „Allgemein" und Wert 0 — das sind die Altzeilen, die Schritt 90 **bewusst stehen gelassen** hat, weil ihre Gruppe anderswo Positionen mit Wert führt (Bedingung in `KostenErfassungsgruppenAltzeilen.cs:173–189`, vierte Bedingung `NOT EXISTS … m.ProjektID = …`) |
| Kat-1-Positionen ohne Nutzungsdauer | **95 von 101**; mit Betrag ≠ 0 und ohne Dauer: **27** |

**Zwei Befunde zum Mitnehmen.** Erstens: die 7 Anlagen mit `KWKG_Anlagenart = ''` sind ein Unsauberkeitsbefund, kein Rechenfehler — in einer STRICT-Tabelle ist die leere Zeichenkette ein Wert und unterscheidet sich von NULL; ein Aufräum-DML wäre ergebnisneutral. Zweitens: das Konzept nennt in § 2.13 (3) „103 von 109 Investitionspositionen ohne Dauer" — gemessen sind es **95 von 101**. Die Zahl im Konzept stammt aus einem älteren Stand der Testdatenbank und ist zu berichtigen.

---

## 6 Zusammenfassung für die Umsetzung

### 6.1 Schemaschritte ab 95

| Nr. | Inhalt | Tabelle(n) | Art | Abhängigkeit | Rechenwirkung | Nachweis |
|---:|---|---|---|---|---|---|
| **95** | K-1: `KWKG_Abwaermeabfuhr`, `KWKG_Stromkennzahl` | `Tab_Energieanlagen` | DDL | keine | keine bis zur Pflege; danach sinkt der Zuschlag von Anlagen mit Notkühler | Referenzlauf byte-gleich; **A-B-Nachweis** für Fall 2 an Projekt 1030 (einzige Anlage mit KWKG-Sätzen) |
| **96** | Szenariorahmen: `Szen_Best/Worst_Zeitraum`, `Szen_Best/Worst_Menge` | `Tab_ProjektWirtschaftlichkeit` | DDL, **beide DDL-Orte** | keine | keine (NULL = wie Erwartet) | Referenzlauf byte-gleich |
| **97** | Trägerpreise best/worst (6 Spalten) | `energy_project_settings` | DDL | keine | keine | Referenzlauf byte-gleich |
| **98** | Erlössätze best/worst (8 Spalten) | `Tab_ProjektWirtschaftlichkeit`, `Tab_ProjektPhotovoltaik` | DDL, für PPV **beide DDL-Orte** | nach 96 (gemeinsame Pflegeoberfläche) | keine | Referenzlauf byte-gleich |
| **99** | Ersatz/Restwert je Position: `ErsatzFuehren`, `RestwertAnsetzen` | `Tab_ProjektWerte`, `Tab_KostenVorlagePosition` | DDL | keine | keine (NULL = wie bisher) | Referenzlauf byte-gleich; **A-B-Nachweis** an einem Projekt mit T > Nutzungsdauer |
| **100** | Preisbasis als eigene Spalte statt `ID_Umrechnung` | `energy_project_settings` | DDL + einmaliges DML | keine | keine (Basispreis bleibt) | Referenzlauf byte-gleich |
| **101** | U-1: Stammtext der fünf Gase auf `Nm³` | `Tab_Brennstoff_Stamm`, `energy_price` | **reines DML** | vor dem Vorlagenbau | keine (Anzeige und Ableitungsschlüssel) | Referenzlauf byte-gleich; **Gegenprobe** auf die Identitätsregel-Ableitung (soll 25 von 25 statt 16 von 25 liefern) |
| *(102)* | *optional:* geräteeigene Nutzungsdauer abkündigen | `Tab_BHKW`, `Tab_Heizkessel` | DDL (Entfernen) | nach Entscheid | keine (kein Leser) | Referenzlauf byte-gleich |
| *(ohne Nr.)* | Speicherflotte an `Tab_Nutzungsdauer` | `Tab_SpeicherAuslegung` (JSON) | kein DDL | eigener Auftrag | **ja** | **Basis neu einfrieren** (`CLAUDE.md:155`), Begründung in `Referenzlaeufe/LIESMICH.md` |

**Reihenfolge-Begründung.** 95 zuerst, weil es der einzige Punkt mit einem abgenommenen Anwenderentscheid ist (18.09.2026) und die Anlagenwahrheit der KWKG-Spalten fortschreibt. 96–98 danach als zusammenhängende Szenariowelle (sie teilen Oberfläche und Nullsemantik). 99 und 100 sind unabhängig und können parallel laufen. 101 sollte vor dem nächsten Vorlagenbau liegen, weil `Tab_Brennstoff_Stamm` ein `ReadOnly`-Katalog ist und in der Auslieferungsvorlage stehen bleibt.

**Für alle Schritte an `Tab_ProjektWirtschaftlichkeit`, `Tab_ErgebnisWirtschaftlichkeit`, `Tab_ErgebnisStromMatrix` und `Tab_ProjektPhotovoltaik` gilt die Doppelpflicht**: Schritt in `SchemaMigration` **und** `SpalteSicher` in `WirtschaftlichkeitCtrl.StelleTabellenSicher`, solange der zweite Mechanismus lebt. Für `energy_project_settings`, `Tab_ProjektWerte`, `Tab_KostenVorlagePosition`, `Tab_Energieanlagen` und `Tab_Brennstoff_Stamm` gilt sie nicht.

**Testdatenbank, Vorlage, Erstbereitstellung — die gute Nachricht:** keiner der Schritte braucht eine Sonderbehandlung. `Werkzeuge/Testdatenbankschema/Program.cs` zieht die Datei auf `SchemaStand.Zielversion` (`:11`, `:903`); `Werkzeuge/Auslieferungsvorlage` entdeckt projektbezogene Tabellen dynamisch über die Spaltennamen `ID_Projekt`/`ProjektID` und die transitive Hülle darunter (`Projektsicht.cs:61,75,82`), nicht über eine gepflegte Liste — **neue Spalten sind ihm gleichgültig, nur eine neue projektbezogene Tabelle müsste er finden**, und das täte er automatisch. `Erstbereitstellung.cs` prüft nur `Tab_Applikation` und den Schemastand (`:101–113`) und akzeptiert ausdrücklich eine ältere Vorlage.

### 6.2 Konzeptstellen, die zu berichtigen sind

Das Mockup-Befundpapier vom 19.09.2026 deckt in § 3.2 bereits die Kopfzeile, § 3.6 (K-1), § 5 (U-1), § 6.3 Nr. 9b und § 2.13 (3) ab. **Nicht darin enthalten und hier neu:**

| Stelle (Zeile) | Alt | Neu |
|---|---|---|
| § 1.2, Z. 84–85 | „Kostenart · Bemessung · **Satz** · Menge · **Betrag** · IstErloes …" | Spaltennamen nennen: „… `Einheitpreis` … `EingegebenerWert` …"; `ID_AnlageGeraet`, `StammID`, `VorlageID`, `NutzungsdauerID` ergänzen |
| § 1.2, Z. 96–99 | „die Wirtschaftlichkeitsspalten an `Tab_Energieanlagen` (KWKG_*, …)" | „… die **neun** `KWKG_*`-Spalten …" und `ID_Carrier` ergänzen |
| § 2.11.5, Z. 712 Zeile „Rahmen" | „je Größe `_Best`/`_Worst` (8 Spalten) … **neu**" | „**6 von 8 vorhanden** seit Schritt 71 (`Szen_Best/Worst_Zins`, `_Preis_E`, `_Preis_B`); neu sind allein Best/Worst des **Betrachtungszeitraums**. Namensvorsicht: `Szen_*_Dauer` ist die Nutzungsdaueränderung" |
| § 2.13 (3), Z. 928 | „in der Testdatenbank tragen **103 von 109** Investitionspositionen keine Dauer" | „**95 von 101**; 27 davon tragen einen Betrag" (Messung 19.09.2026) |
| § 2.13 (3) Nr. 5, Z. 966–968 | „die ihren Ersatz über ein handgepflegtes `ErsatzintervallJahre` und einen `RestwertEuro` führt" | „… über die gleichnamigen **Felder des Flottenstands** (JSON in `Tab_SpeicherAuslegung`), nicht über Spalten — der Anschluss berührt deshalb die **Einfrierregel** des Projekts 1046" |
| § 3.6, Z. 1851 | „nächster freier Schemaschritt ist **92** (90 ist BK1a, 91 ist BK1b)" | „nächster freier Schemaschritt ist **95** (92 Vergleichsprojekt, 93 Vergütung je Variante, 94 Hilfsstrom-Bemessung)" |
| § 5, Z. 2178 und Kopfzeile Z. 3 | „als **Schemaschritt 62** … / Schemaschritt 62 vergeben (U-1)" | „**Schritt 62 ist anderweitig vergeben** (`Schritt_62_KlimaWaisen`); U-1 steht aus und bekommt Schritt **101**. Gemessen 19.09.2026: die fünf Gase führen in `Tab_Brennstoff_Stamm.Einheit` unverändert `m³`; `energy_carrier.billing_unit` steht dagegen seit Schritt 26a auf `Nm³`" |
| § 5, Z. 2189–2190 | „`Konzept_Einheitenbruch_Energietraeger_EPOS-Plan.md` — liegt derzeit **nur** auf Zweig `claude/lucid-cori-a9a425` …, noch nicht gemergt" | „… liegt in `Dokumentation/ueberholt/`; sein Bezugsstand ist Access mit `SchemaVersion 61`. Nicht zu verwechseln mit `Dokumentation/aktuell/Konzept_Einheiten_EPOS-Plan.md` (kWh gegen MWh)" |
| § 6.5, Zeile „Zwei Migrationsmechanismen" | „**vier Tabellen**; neue Spalten gehören an beide Stellen" | „**fünf** Tabellen mit eigenem `CREATE TABLE` (`Tab_ProjektWirtschaftlichkeit`, `Tab_ErgebnisWirtschaftlichkeit`, `Tab_ErgebnisWirtSensitivitaet`, `Tab_ProjektTarif`, `Tab_ErgebnisStromMatrix`), dazu 55 `SpalteSicher` auf sechs Tabellen — die CREATE tragen weder `STRICT` noch Fremdschlüssel (ADR-001 Option B)" |
| § 6.5, Zeile „BHKW-Einspeisevergütung an vier Orten" | „vier Orte … drei Felder zu viel" | „**drei Orte**: aktiver Tarif (`Tab_ProjektTarif.Einsp_Arbeit`), Projektparameter (`Einspeiseverguetung_KWK`), Anlage (`KWKG_Satz_Einspeisung`). Der vierte (`energy_project_settings.Verguetung_BHKW`) ist mit Schritt 84/85 entfallen" |
| § 6.5, Zeile „Komponenten-IDs hart verdrahtet … `Form_Kosten` gegen `UcBkKosten`" | wie steht | **streichen** — beide Klassen gibt es nicht mehr; die Unterscheidung liegt im Kern (`KostenVorlagenCtrl.IstErfassungsgruppe`), die Oberfläche in `EPOS.UI/Dialoge/Kosten/` |
| § 6.5, Zeile „Zwei Lesewege … gespeicherte Access-Abfrage" | „Access-Abfrage" | „gespeicherte **Sicht** `Abfrage_Kostenfaktoren` — kein Access-Artefakt mehr, `OleDb`/`ACE` sind aus `EPOS.Kern` verschwunden; die Sicht lebt in SQLite und wird beim Tabellenumbau eigens behandelt (`ProjektWerteLoeschschutz`)" |
| § 6.5, Zeile „Stromsteuersatz" | „eine **Wache** hält beide zusammen" | „eine Wache prüft Modell gegen Konstante (`StrompreisZerlegungTests`), **nicht** Konstante gegen Katalog — gekoppelt ist nichts, und eine Wache dafür fehlt" |
| § 6.3 Nr. 14 | „Reduzierter Stromsteuersatz bleibt Konstante bis zur Katalog-Nachpflege" | Der Katalog führt `STROMST_REDUZIERT_SATZ` 2026 = 0,5 EUR/MWh, Status GESICHERT (gemessen) — die Nachpflege **ist erfolgt**; zu prüfen bleibt nur, ob der Rechenweg sie liest |
| § 6.3 (neu) | — | Zwei neue offene Punkte: **(25)** 7 Energieanlagen führen `KWKG_Anlagenart = ''` statt NULL (Projekte 1032, 1043) · **(26)** `Nachweis_Json` ist in 0 von 78 Ergebniszeilen gefüllt, obwohl § 6.3 Nr. 17 sie als erledigt führt — zu klären, ob die Zeilen älter als B7P sind |

---

## Nicht geprüft

- **Kein Bau, kein Test, kein Referenzlauf.** Alle Aussagen zur Rechenwirkung sind aus Quelltext und Konzept abgeleitet, nicht gemessen. Die Einstufung „ergebnisneutral" der vorgeschlagenen Schritte 95–102 ist begründet, aber unbelegt.
- **`SchemaMigration.cs` nur punktuell gelesen** (Kopf, Schrittregistrierung 90–94, Schritt 62, Treffer zu `Tab_ProjektWirtschaftlichkeit`/`Tab_ProjektTarif`). Ich habe **nicht** Schritt für Schritt geprüft, welche Spalte jeder der Schritte 62–94 anlegt; die Aussage „sie überschneiden sich vollständig" stützt sich auf die Selbstauskunft des Quelltextes und fünf Stichproben.
- **Die dritte Implementierung des Vorrangs „Projekt vor Katalog"** habe ich nicht positiv identifiziert. In `StromPreisCtrl.cs` fand sich keine gleichlautende Kette; ob die dritte wirklich `Abfrage_Kostenfaktoren` ist, bleibt Vermutung.
- **„Energieintensiv an drei Orten"** habe ich nicht nachgemessen — die Zeile ist eine Oberflächenfrage und vom Mockup-Papier abgedeckt.
- **`Tab_SpeicherAuslegung`** habe ich nicht in der Datenbank gelesen; die Aussage zur JSON-Ablage stammt aus `SpeicherFlottenStudieCtrl.cs` und `FlottenModel.cs`. Ob `ErsatzintervallJahre` im Flottenstand des Projekts 1046 tatsächlich gepflegt ist, ist ungeprüft.
- **`Tab_ProjektPhotovoltaik` ist in der Testdatenbank leer** (0 Zeilen). Alle Aussagen zu PV-Vergütungsspalten sind Schemaaussagen, keine Datenaussagen.
- **`Tab_ProjektTarif` ist leer** (0 Zeilen) — der Vorrang „aktiver Tarif schlägt Parameterwert" ist an der Testdatenbank nicht nachweisbar.
- **`EPOS.Kern.Tests` nur überflogen.** Ich habe die Wachenamen erhoben, aber keine Testkörper gelesen außer den drei zitierten Zeilen. Ob es eine Wache gibt, die „alle Fachtabellen sind STRICT" prüft, habe ich **nicht** festgestellt — `Migration74Tests.cs:13` nennt die Zahl nur in einem Kommentar.
- **`Werkzeuge/Auslieferungsvorlage`** habe ich auf die Entdeckungslogik hin gelesen, nicht auf die Bereinigungsregeln je Tabelle; die Aussage „keine Sonderbehandlung nötig" gilt für Spalten, nicht zwingend für neue Tabellen.
- **Der Einheitenbruch selbst** (welche 9 von 25 Brennstoffen `-1` liefern) ist nicht nachgerechnet; geprüft ist nur der Stammtext der fünf im Konzept genannten Gase.

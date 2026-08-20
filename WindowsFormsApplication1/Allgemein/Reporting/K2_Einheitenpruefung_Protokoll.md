# K2 · Einheiten-Konsistenz der Energieträger — HF2, Migrationsschritt M-A

**Stand: 19.08.2026.** Umsetzung der Etappe **K2** aus
[`Konzept_Kosten_Energietraeger_EPOS-Plan.md`](../../../Konzept_Kosten_Energietraeger_EPOS-Plan.md)
(§ 2 L2/L3/L4, § 4 HF2, § 9, § 10). Ausgangsstand `743d81a`. Vorgängeretappe:
[`K1_Aufraeumung_Protokoll.md`](K1_Aufraeumung_Protokoll.md).

**Ergebnis in drei Sätzen.** Die Umrechnungsregel hat ab jetzt einen **Namen** und einen
**Schalter**: Migrationsschritt **25** legt `energy_conversion.faktor_name` und
`energy_conversion.aktiv` an, belegt sie ergebnisneutral vor und legt die Tabelle
notfalls selbst an — sie ist die einzige des Vorhabens, die bisher weder ein
Migrationsschritt noch ein Controller anlegte. Die neue Klasse
`Controller/EnergieEinheitenPruefung.cs` beantwortet die Frage aus **L2** („erreicht
dieser Träger kWh?") für den Katalog und für ein einzelnes Projekt, und der
Wirtschaftlichkeitslauf meldet ihre Befunde als nicht blockierende Protokollwarnung.
**Kein Bestandswert wurde angefasst** — weder `factor` noch `from_unit`/`to_unit`, und
keine Einheit wurde in `Nm³` umbenannt; das ist M-B in Etappe K3.

---

## 1 Was geändert wurde

| # | Gegenstand | Datei : Zeile |
|---|---|---|
| 1 | Zwei Persistenzwerte für den Faktornamen (Drei-Schichten-Regel, Ablage analog `PREISREIHE_*`) | `Allgemein/DbWerte.cs:1047-1083` — `UMRECHNUNG_NAME_STANDARD` (`:1067`), `UMRECHNUNG_NAME_Z_FAKTOR` (`:1082`) |
| 2 | Zwei Tabellennamen ergänzt | `Allgemein/Update/SchemaKatalog.cs:61-62` (`ENERGY_CONVERSION`, `ENERGY_CARRIER`) |
| 3 | Spaltenkonstanten und Schrittauswahl für M-A | `SchemaKatalog.cs:1209-1282` — `SPALTE_EC_FAKTOR_NAME` (`:1223`, TEXT(50)), `SPALTE_EC_AKTIV` (`:1242`, YESNO), `Schritt25_Einheitenkonsistenz` (`:1278-1282`) |
| 4 | Begründung, warum der Schritt **nicht** in `Alle` steht (Rückfallebene) | `SchemaKatalog.cs:1400-1409` |
| 5 | Zielversion 24 → **25** | `SchemaMigration.cs:77` |
| 6 | Schrittnummer samt vollständiger Begründung | `SchemaMigration.cs:614-672` (Doku ab `:615`) |
| 7 | Registrierung im Schrittregister (hinter Schritt 24) | `SchemaMigration.cs:1133-1145` (Kommentar ab `:1133`) |
| 8 | Zählwerk `DatenUmrechnungAktiv` / `DatenUmrechnungBenannt` + Rücksetzung | `SchemaMigration.cs:904-916`, Rücksetzung `:1194-1195` |
| 9 | Der Schritt selbst (25a/25b/25c) | `SchemaMigration.cs:2071-2322` — `CARRIER_GAS` (`:2088`), `SQL_CREATE_ENERGY_CONVERSION` (`:2097`), `Schritt_25_Einheitenkonsistenz` (`:2129`), `FaktornameVorbelegen` (`:2216`), `GasBrennstoffListe` (`:2290`), `SpalteVorhanden` (`:2317`) |
| 10 | Der Prüfer | `Controller/EnergieEinheitenPruefung.cs` (neu, 571 Zeilen) — `EinheitenBefund` (`:22`), `PruefeKatalog` (`:136`), `PruefeProjekt` (`:169`) |
| 11 | Nicht blockierende Protokollwarnung im Wirtschaftlichkeitslauf | `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:45` (Cache `_einheitenGeprueft`), `:503` (Aufruf am Ende von `LadeParameter`), `:507-558` (`MeldeEinheitenBefunde`, Signatur `:537`) |
| 12 | Dieses Protokoll | `Allgemein/Reporting/K2_Einheitenpruefung_Protokoll.md` (neu) |

**Vier geänderte und zwei neue Dateien.** In den vier Bestandsdateien 529 eingefügte
und 1 geänderte Zeile (die Zielversion), dazu 1.038 Zeilen in den beiden neuen —
zusammen 1.567 Einfügungen. Keine gelöschte Datei, kein `DROP`, keine Datenänderung an
Bestandswerten.

---

## 2 Drei Korrekturen am Auftrag — jeweils am Bestand verifiziert

Der Arbeitsauftrag und das Konzept gehen an drei Stellen von einer Lage aus, die der
Code nicht bestätigt. Alle drei sind gemessen, nicht vermutet.

### 2.1 Der Schritt heißt 25, nicht 21

Der Auftrag nannte „letzter Schritt = 20". Tatsächlich stand `ZIEL_VERSION` bei
**24**: Die Nummern 21 bis 23 gehören den Etappen E5, E6 und L12/L13, die 24 den
Katalogdubletten. Das ist dieselbe Lage, die `SCHRITT_24_KATALOG_DUBLETTEN` schon
zweimal ans Ende gerückt hat (Kommentar `SchemaMigration.cs:380-388`: „Warum 24 und
nicht 19"). Der neue Schritt bekommt deshalb die **25**. Zwei Schritte mit derselben
Nummer würden den Versionsmarker unbrauchbar machen — er hält genau eine Zahl fest,
und der jeweils andere Schritt gälte als erledigt, ohne je gelaufen zu sein.

### 2.2 Der Gas-Erkennungswert heißt `GASEOUS_FUEL`, nicht `GAS`

Konzept § 4.1 und der Auftrag nennen `pricing_model = 'GAS'`. **Diesen Wert gibt es
nicht.** Die Katalogtabelle `pricing_model` der Arbeitskopie führt genau sechs Codes:

```
ANIMAL_FAT · ELECTRICITY · GASEOUS_FUEL · HEAT · LIQUID_FUEL · SOLID_FUEL
```

`Gas` ist der **`group_code`**, nicht das Preismodell — und über den zu gehen wäre
zusätzlich falsch: **Wasserstoff** (Träger 68) führt den eigenen Gruppencode
`Wasserstoff` bei `pricing_model = GASEOUS_FUEL`. Wer nach `group_code = 'Gas'` filtert,
lässt ausgerechnet den gasförmigsten Träger ohne z-Faktor. Gewählt ist deshalb
`pricing_model = 'GASEOUS_FUEL'` (`SchemaMigration.cs:2088`, Muster der vorhandenen
Konstante `CARRIER_STROM = "ELECTRICITY"`).

Gegenprobe an der Arbeitskopie: 5 der 59 Regeln gehören zu einem gasförmigen Träger
(Brennstoffe 1 Stadtgas, 2 Erdgas LL, 3 Erdgas E, 14 Biogas, 25 Wasserstoff), 54 nicht.

### 2.3 `energy_conversion` wird von der Migration **nicht** angelegt

Bestätigt: Kein Migrationsschritt und kein Controller legt die Tabelle an. Sie stammt
aus der ausgelieferten `Kenndaten.accdb` bzw. aus der Handmigration
(`migration.manuell.sql:503-507`, „energy_conversion: global, Quelle gewinnt komplett").
Ohne Vorsorge hätte `SpaltenAnlegen` auf einer Datenbank ohne diese Herkunft nur
„Tabelle nicht lesbar" gemeldet und den Schritt scheitern lassen — **dauerhaft**, denn
der Versionsmarker bliebe stehen. Teil **25a** legt sie deshalb selbst an, mit exakt dem
Spaltensatz des Handskripts (`ID, id_brennstoff, from_unit, to_unit, factor,
user_edited`); die zwei Neuspalten kommen über den regulären Katalogweg 25b, damit es
für sie genau eine Wahrheit gibt. Eine so entstandene Tabelle ist **leer** — die Seeds
sind M-B in Etappe K3.

---

## 3 Migrationsschritt 25 im Einzelnen

| Teil | Was | Idempotenz |
|---|---|---|
| **25a** | `CREATE TABLE energy_conversion (…)` | `Ddl()` wertet „existiert bereits" als Erfolg. Gemessen: ACE liefert dabei **SQLState 3010** — genau die Nummer, die `IstBereitsVorhanden` (`SchemaMigration.cs:5045-5069`) prüft. Der deutsche Meldungstext lautet „Tabelle … ist bereits vorhanden" und würde die Textprüfung auf „existiert bereits" **nicht** treffen; die Nummernprüfung greift davor. |
| **25b** | `faktor_name` TEXT(50), `aktiv` YESNO aus dem Katalog; danach `UPDATE … SET aktiv = TRUE` | Das UPDATE läuft **nur, wenn `aktiv` in eben diesem Lauf entstanden ist** (`SpalteVorhanden` VOR `SpaltenAnlegen`). Begründung unten. |
| **25c** | `faktor_name = 'z-Faktor'` für Gasträger, danach `= 'Umrechnungsfaktor'` für den Rest | Beide UPDATEs mit `IS NULL OR = ''` — dieselbe Bedingung wie 19b bis 23b. Zweiter Lauf: 0 Zeilen (gemessen). |

**Kein DDL-DEFAULT** (Hausregel `SchemaKatalog.cs:466-476`). Beide Vorbelegungen setzt
der DML-Teil.

**Warum `aktiv` nur einmal vorbelegt wird.** `ADD COLUMN … YESNO` belegt in Access jede
Bestandszeile mit `False` — jede vorhandene Umrechnungsregel stünde damit schlagartig
auf „aus". Dieselbe Falle wie bei `Extrapolation_erlaubt` (Schritt 7) und
`Aufschlag_Anwenden` (Schritt 12d). Anders als dort lässt sie sich hier **nicht** über
eine WHERE-Klausel absichern: `YESNO` kennt in Access kein NULL, „nie gesetzt" und
„bewusst abgeschaltet" sind nach dem ersten Lauf ununterscheidbar. Ein pauschales UPDATE
bei jedem Lauf würde die erste vom Anwender abgeschaltete Regel wieder einschalten.
Deshalb der Migrations-Anker nach dem Muster `WirtschaftlichkeitCtrl.SpalteSicher`
(„liefert true, wenn die Spalte JETZT neu angelegt wurde").

**Warum 25c aus zwei Anweisungen besteht und die Reihenfolge trägt.** Der zweite UPDATE
greift alles, was danach noch ohne Namen dasteht. Liefe er zuerst, bekämen auch die
Gasregeln „Umrechnungsfaktor", und der erste fände wegen seiner eigenen
`IS NULL OR = ''`-Bedingung keine Zeile mehr.

### 3.1 Eine ACE-Falle, gefunden im Trockentest

Die naheliegende Fassung des z-Faktor-UPDATE war

```sql
UPDATE [energy_conversion] SET [faktor_name] = ?
 WHERE [id_brennstoff] IN (SELECT DISTINCT [ID_Brennstoff] FROM [energy_carrier]
                            WHERE [pricing_model] = ?)
   AND ([faktor_name] IS NULL OR [faktor_name] = '')
```

Sie trifft gegen die Arbeitskopie **null Zeilen** — ohne Fehler, ohne Warnung. Dieselbe
Bedingung liefert:

| Fassung | Betroffene Zeilen |
|---|---|
| `SELECT COUNT(*)` mit Parameter in der Unterabfrage | **5** ✔ |
| `UPDATE` mit **Literal** in der Unterabfrage | **5** ✔ |
| `UPDATE` mit **zwei Parametern**, einer davon in der Unterabfrage | **0** ✘ |

ACE bindet Parameter innerhalb der Unterabfrage eines UPDATE nicht in Textreihenfolge.
Das stille „0 Zeilen" wäre hier besonders heimtückisch gewesen: Der Schritt hätte als
erfolgreich gegolten, der Marker wäre auf 25 gerückt, und die fünf Gasregeln hätten
anschließend vom zweiten UPDATE den **Standardnamen** bekommen — ein falscher
Persistenzwert, den kein späterer Lauf mehr korrigiert (`IS NULL OR = ''` findet sie
nicht mehr).

Umgesetzte Fassung (`FaktornameVorbelegen`, `SchemaMigration.cs:2216-2288`): erst die
Brennstoffnummern mit einer **parametrisierten Abfrage** holen (dort bindet ACE
korrekt), dann ein UPDATE mit **ganzzahliger IN-Liste**. Die Werte durchlaufen `Zahl()`
und sind `int`, bevor sie in den SQL-Text gehen — eine Einschleusung ist damit
ausgeschlossen. Gegenprobe nach der Korrektur: **5 z-Faktor, 54 Umrechnungsfaktor,
zweiter Lauf 0/0.**

---

## 4 Der Prüfer `EnergieEinheitenPruefung`

**Fachregel (L2, Konzept § 4.2).** Je aktivem Träger:

1. `billing_unit = kWh` → erfüllt (Identität, Stufe 0);
2. sonst eine **aktive** Regel `billing_unit → kWh` mit `factor > 0` (Stufe 1);
3. sonst eine zweistufige Kette `billing_unit → X → kWh`, beide aktiv und `> 0`
   (Stufe 2);
4. sonst Befund `KWH_UNERREICHBAR`.

Zusätzlich, wenn die Kette steht (Stufe ≥ 1) und weder `hi_kwh_per_unit` noch
`hs_kwh_per_unit` gepflegt ist: Befund `HEIZWERT_FEHLT`. Bei kWh-Trägern (Stufe 0) ist
die Frage gegenstandslos — dort **ist** die Menge schon die Energie.

**Mehr als zwei Stufen prüft er nicht.** Konzept § 4.2 nennt ausdrücklich
„Kettenauflösung max. 2 Stufen"; eine unbegrenzte Suche wäre auf einem frei editierbaren
Einheiten-Textfeld eine Zyklensuche ohne fachlichen Gewinn. Regeln, die auf sich selbst
zeigen (`m³ → m³` — im Bestand die Regel jedes Gasträgers), werden als **Zwischen**stufe
übersprungen; als erste Stufe schickten sie den Prüfer über dieselbe Einheit im Kreis.

**Problemcodes statt Ressourcen.** `EinheitenBefund` trägt beides: einen sprachneutralen
`Code` (`MIGRATION_AUSSTEHEND`, `KWH_UNERREICHBAR`, `HEIZWERT_FEHLT`) und einen deutschen
`Klartext` mit den konkreten Einheiten des Falls. MyResource ist bewusst **nicht**
eingebunden — der Prüfer ist UI-frei und wird in K2 nur von einem Protokollkanal
gelesen; der Code ist der Anker, an dem die Dialogfassung in K3 ihre lokalisierten Texte
aufhängt.

**Eigene Verbindung statt `DataRepository.GetDataTable`.** Der Verbindungsstring kommt
aus `DataRepository.GetConnectionString()` — die eine Wahrheit über den Datenbankpfad —,
die Abfragen laufen aber auf einer eigenen `OleDbConnection`. Begründung wörtlich wie
bei `SchemaMigration` (`:28-33`): `DataRepository` zeigt bei Fehlern **außerhalb** des
Engine-Modus eine `MessageBox` (`DataRepository.cs:131-155`). Genau das darf hier nicht
passieren — der Prüfer läuft im Bestand auf Datenbanken **vor** Schritt 25, in denen die
Spalte `aktiv` schlicht fehlt, und eine fehlende Spalte ist für ihn ein Befund, kein
Dialog. Das Schema wird deshalb auch nicht über eine Probeabfrage ermittelt, sondern
über `GetOleDbSchemaTable` — dasselbe Vorgehen wie `WirtschaftlichkeitCtrl.SpalteSicher`.

**Robust gegen fehlendes Schema.** Fehlt die Tabelle oder eine der zwei Spalten, liefert
der Prüfer genau **einen** Befund `MIGRATION_AUSSTEHEND` und sonst nichts — keine
Ausnahme nach außen, keine Teilaussage über Träger, die er nicht beurteilen kann. Eine
gänzlich unlesbare Datenbank ergibt eine **leere** Liste: Das wäre eine Aussage über den
Prüfer, nicht über die Träger.

### 4.1 Trägermenge je Projekt

`PruefeProjekt` prüft die **Vereinigung** zweier Mengen — dieselbe Doppelquelle, mit der
auch die Wirtschaftlichkeit arbeitet:

- `energy_project_settings.ID_Energieträger` — die im Kostendialog gepflegten Träger,
  Grundlage von `Abfrage_Energietraeger_Effektiv`;
- `Tab_Energieanlagen.ID_Carrier` — der Träger, den eine Anlage tatsächlich fährt.

Beide decken sich im Bestand **nicht**: Es gibt gepflegte Träger ohne Anlage und Anlagen
ohne gepflegten Träger (die BHKW-Anlage des Projekts 1017 führt gar keinen — Befund aus
Etappe E2, `WirtschaftlichkeitCtrl.cs:2845-2847`). Wer nur eine der beiden Mengen prüfte,
übersähe genau die Lücken, um die es geht. Scheitert der zweite Zweig, weil eine alte
Datenbank `ID_Carrier` noch nicht führt (vor Schritt 8), fällt die Abfrage auf die
gepflegte Trägermenge zurück — eine kleinere, aber richtige Aussage.

**Projektüberschreibungen.** `custom_hi`/`custom_hs` schlagen den Katalogheizwert, sobald
sie `> 0` sind (Vorrangregel „Projektwert vor Katalogwert" wie in
`WirtschaftlichkeitCtrl.Traeger`). `ID_Umrechnung` benennt die im Dialog **gewählte**
Regel; ihre `to_unit` ist dann die Einheit, in der das Projekt rechnet, und die Kette
beginnt dort statt bei `billing_unit` (Leseseite:
`ucFuelSettings.GetTargetUnitByConversionId`). Zeigt der Verweis ins Leere, auf eine
abgeschaltete Regel oder auf eine mit Faktor 0, gilt wieder `billing_unit` — der Prüfer
erfindet keine Zieleinheit.

Gegenprobe an zwei echten Projekten der Arbeitskopie (UNION-Abfrage einzeln
ausgeführt): Projekt **1011** liefert 2 Träger (Heizöl L mit `ID_Umrechnung = 35` →
Zieleinheit `kg`; Erdgas E mit `40` → `m³`), Projekt **1017** liefert 3 (zwei
Stromträger, Tierische Fette). Die Abfrage läuft in ACE fehlerfrei — einschließlich der
Umlautspalte `ID_Energieträger`, der `NULL`-Literale im zweiten UNION-Zweig und der zwei
Parameter.

---

## 5 Protokollanbindung im Wirtschaftlichkeitslauf

`WirtschaftlichkeitCtrl.LadeParameter` ruft am Ende `MeldeEinheitenBefunde(idStamm)`
(`:503`). Der Aufruf ist **nicht blockierend**: keine MessageBox, kein Abbruch, kein
veränderter Rückgabewert.

**Warum nicht blockierend.** Ein Träger, der kWh nicht erreicht, ist ein Mangel der
Stammdaten. Ihn mitten im Wirtschaftlichkeitslauf zur Fehlerlage zu erklären hieße, ein
gespeichertes Projekt unbenutzbar zu machen, das gestern noch gerechnet hat. Die
blockierende Prüfung gehört an die Stelle, an der die Daten **entstehen** — beim
Speichern in `ucFuelSettings`, Etappe K3 (so auch Konzept § 4.2).

**Gewählter Kanal: `SimulationProtokoll.Aktuell.WarnungEinmal`.** Das ist der eine nicht
blockierende Meldekanal dieser Anwendung — prozessweit erreichbar, nie `null`, im
unbeaufsichtigten Lauf dialogfrei und ausdrücklich ergebnisneutral („Diese Klasse rechnet
nichts. Sie sammelt Text.", `SimulationProtokoll.cs:69`). Er ist kein Simulationsmonopol:
`DataRepository` meldet über `FehlerMelden` in denselben Weg. Die Stufe **Warnung**
trifft die Lage nach der Definition der Klasse selbst — „gerechnet wurde, aber mit einer
Ersatzannahme": Bei fehlender Regelkette greift die Mengenrechnung unmittelbar auf
`eff_hi` zurück.

**Kein Einfluss auf die Referenzläufe — nachgeprüft.** `Referenzlauf/Protokoll.cs:67`
zählt Warnungen über das Konsolen-Token `"Simulation Warnung:"`. Die Suite ruft
`LadeParameter` jedoch **nirgends** auf (`grep` über `Referenzlauf/*.cs`: kein Treffer
auf `LadeParameter` oder `WirtschaftlichkeitCtrl` — sie rechnet Simulationen, keine
Wirtschaftlichkeit). Diese Meldungen können dort also weder auftauchen noch eine Zählung
verschieben.

**Zwei Sicherungen gegen Mehrfachmeldung und Kosten.** `LadeParameter` wird je Sitzung
vielfach gerufen (Parameterdialog, Reiter, Verlaufsfenster, Bericht, KI-Leseaktion). Der
Cache `_einheitenGeprueft` (`:45`) begrenzt die **Prüfung** auf einmal je Projekt und
Ctrl-Leben — dieselbe Begründung wie beim benachbarten `_refKesselCache` („Review 11:
LadeParameter wird oft gerufen"); `WarnungEinmal` begrenzt zusätzlich die **Meldung** je
Schlüssel `K2/EINHEITEN/<Projekt>/<Träger>/<Code>`.

---

## 6 Befundliste — Plausibilitätslauf gegen `Referenzlaeufe\Arbeitskopie\Kenndaten.accdb`

Read-only nachgestellt per PowerShell (x64, `Microsoft.ACE.OLEDB.12.0`) gegen eine
**Scratch-Kopie**; die Arbeitskopie selbst wurde nur gelesen, nie geöffnet-und-geändert.
Geprüft wurde der Katalogstand **vor** Schritt 25, also mit der Annahme „alle Regeln
aktiv" (die Spalte `aktiv` existiert dort noch nicht) — das ist genau die Regelmenge, die
der Prüfer nach der Migration vorfindet.

**21 aktive Träger, 59 Regeln, 17 Befunde.** Alle 17 sind `KWH_UNERREICHBAR`; **kein**
`HEIZWERT_FEHLT` (jeder Träger führt einen `hi_kwh_per_unit > 0`).

| # | Träger | `billing_unit` | Befund |
|---|---|---|---|
| 49 | Biogas | `m³` | keine Regel `m³ → kWh`, auch nicht zweistufig |
| 52 | Erdgas LL | `m³` | dito |
| 61 | Biogas 2 | `m³` | dito |
| 63 | Erdgas E | `m³` | dito |
| 64 | Stadtgas | `m³` | dito |
| 65 | Test | `m³` | dito |
| 66 | Biogas Variante | `m³` | dito |
| 68 | Wasserstoff | `m³` | dito |
| 53 | Heizöl Bio 10 | `L` | keine Regel `L → kWh` |
| 56 | Heizöl EL | `L` | dito |
| 57 | Heizöl Bio 15 | `L` | dito |
| 62 | Heizöl L | `L` | dito |
| 67 | Heizöl S | `L` | dito |
| 70 | Heizöl L Variante | `L` | dito |
| 71 | Heizöl L var | `L` | dito |
| 59 | Koks | `kg` | keine Regel `kg → kWh` |
| 69 | Tierische Fette | `kg` | dito |

**Ohne Befund** bleiben die vier Träger mit `billing_unit = kWh`: 51 Fernwärme, 54 Strom
Variante, 58 Elektrische Energie 2, 60 Elektrische Energie — bei ihnen ist die Bedingung
aus L2 durch Identität erfüllt.

**Das ist die erwartete Lücke, nicht ein Fehler des Prüfers.** Jeder der 17 Träger führt
Regeln — aber nur solche, die INNERHALB der Mengenwelt bleiben (`L → kg`, `L → m3`,
`kg → t`, `m³ → m³`). Eine Regel nach `kWh` gibt es für keinen einzigen. Genau diese
Seeds legt **M-B in Etappe K3** an; danach muss `PruefeKatalog()` null Befunde liefern —
so das Abnahmekriterium der Etappe K3.

Nebenbefund für K3: Die Regeln `L → m3`, `kg → t`, `kg → m3`, `kg → rm`, `kg → SRM` und
`kWh → MWh` tragen durchweg **Faktor 0**. Der Prüfer siebt sie schon beim Lesen aus (nur
`factor > 0` zählt); als Umrechnungsregeln sind sie unbrauchbar und wären in K3 entweder
zu füllen oder abzuschalten — was ab jetzt möglich ist, ohne sie zu löschen (L3).

---

## 7 Ergebnisneutralität — die Belegkette

K2 ist laut Konzept § 10 **zwingend ergebnisneutral**. Drei Nachweise:

**a) Kein Bestandswert wird geändert.** Schritt 25 fasst `factor`, `from_unit`,
`to_unit` und `user_edited` nicht an, legt keine Zeile an und löscht keine. Gegenprobe
am Trockentest: 59 Zeilen und `SUM(factor) = 36,0242189` vor **wie nach** dem
vollständigen Lauf, einschließlich Wiederholungslauf.

**b) Kein Rechenpfad liest die neuen Spalten.** Alle Leser von `energy_conversion`
arbeiten mit **ausgeschriebener** Spaltenliste, nie mit `SELECT *`:

| Stelle | Abfrage |
|---|---|
| `ucFuelSettings.cs:477` (`GetConversions`) | `SELECT id_brennstoff, from_unit, to_unit, factor FROM ENERGY_CONVERSION WHERE …` |
| `ucFuelSettings.cs:528` (`GetTargetUnitByConversionId`) | `select to_unit from energy_conversion where id=…` |
| `ucFuelSettings.cs:539` (`GetConvID`) | `SELECT ID FROM ENERGY_CONVERSION WHERE …` |
| `WizardCtrl.cs:1061` | `SELECT ID FROM ENERGY_CONVERSION WHERE …` |

Zwei zusätzliche Spalten sind für sie folgenlos; die Leseseite musste **nicht** angefasst
werden. `Form_Kosten.GetAllCarriers` (`:1290`) verwendet zwar `ec.*` — das ist aber
`energy_carrier`, nicht `energy_conversion`, und diese Tabelle bleibt unverändert. Die
Mengen- und Kostenrechnung geht ohnehin über `Abfrage_Energietraeger_Effektiv`, die den
Namen und den Schalter nicht kennt.

**c) Der einzige neue Leser rechnet nichts.** `EnergieEinheitenPruefung` liest und
meldet; sie schreibt nirgends. Ihr Ergebnis erreicht keinen Rechenweg und keine
persistierte Spalte — insbesondere **nicht** `Tab_ErgebnisWirtschaftlichkeit.HinweisText`,
was gespeicherte Altrechnungen textlich verändert hätte. Es geht ausschließlich in den
Protokollkanal.

**Nicht erledigt und ausdrücklich nicht Teil von K2:** Ein tatsächlicher
Referenzlauf-Vergleich (B5/B6) und der Duplizieren-Smoke stehen aus — wie schon in K1
(dortiger offener Punkt 1) waren sie nicht Teil des Arbeitsauftrags. Fachlich ist keine
Abweichung zu erwarten; die Begründung steht in a) bis c).

---

## 8 Encoding-Befund je angefasster Datei

Jede Datei wurde **vor** dem Schreiben gemessen und danach gegengeprüft. Alle vier
Bestandsdateien sind gültiges UTF-8 — die cp1252-Falle des Baums (93 von 372 `.cs`) traf
keine von ihnen, das Edit-Werkzeug war damit zulässig.

| Datei | Befund vorher | Ergebnis nachher |
|---|---|---|
| `Allgemein/DbWerte.cs` | UTF-8 **mit** BOM, CRLF, 87.957 Byte | 90.262 Byte, BOM erhalten, 1.500 CRLF, **0** nackte LF |
| `Allgemein/Update/SchemaKatalog.cs` | UTF-8 **mit** BOM, CRLF, 85.504 Byte | 91.296 Byte, BOM erhalten, 1.423 CRLF, **0** nackte LF |
| `Allgemein/Update/SchemaMigration.cs` | UTF-8 **mit** BOM, CRLF, 263.878 Byte | 279.804 Byte, BOM erhalten, 5.074 CRLF, **0** nackte LF |
| `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` | UTF-8 **ohne** BOM, CRLF, 224.136 Byte (md5 `c3444e0b…`) | 227.877 Byte, weiterhin **ohne** BOM, 3.933 CRLF, **0** nackte LF |
| `Controller/EnergieEinheitenPruefung.cs` | neu | UTF-8 **mit** BOM, CRLF — wie die Nachbardateien im Ordner (`StromAufschlagCtrl.cs`, `StromPreisCtrl.cs`) |
| `Allgemein/Reporting/K2_Einheitenpruefung_Protokoll.md` | neu | UTF-8 **ohne** BOM, CRLF — nach `.editorconfig` `[*.md]`, wie das K1-Protokoll |

Der BOM-Zustand ist in beiden Richtungen erhalten: `WirtschaftlichkeitCtrl.cs` hat
weiterhin **keinen**, die drei anderen weiterhin **einen**. Keine Datei hat nackte LF
bekommen.

---

## 9 Build — Baseline gegen Ende

Wie in K1: `dotnet build` bricht am Hauptprojekt grundsätzlich mit **MSB4803** ab
(zwei `COMReference`-Einträge, Excel-Interop und VBIDE). Gebaut wurde mit der
.NET-Framework-Fassung von MSBuild:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    WindowsFormsApplication1\WindowsFormsApplication1.csproj -t:Rebuild -p:Configuration=Debug -p:Platform=x86
```

| Lauf | Ergebnis | Fehler | Warnungen |
|---|---|---|---|
| **Baseline** (`743d81a`, vor K2) | erfolgreich | **0** | **6** |
| **Ende** (nach K2) | erfolgreich | **0** | **6** |

Die Baseline wurde gegen `743d81a` **neu gemessen** und nicht aus K1 übernommen — der
Baum hat sich seither erheblich bewegt (Lizenzdialog, KI-Registerauswahl, L12/L13,
Katalogdubletten). Sie stimmt zufällig mit dem K1-Endstand überein.

Die sechs Warnungen sind zeilengleich dieselben und stammen sämtlich aus unberührten
Dateien: `Model/WErzeugerModel.cs(6,20)` CS0108,
`Controller/KlimaregionStammCtrl.cs(22,24)` und `(23,48)` CS0109,
`Controller/StromverbraucherStammCtrl.cs(25,44)` CS0108, `MDIMainForm.cs(348,28)` CS1998
sowie `(359,17)` CS4014. **Keine neue Warnung aus einer K2-Datei**, keine verschwundene.

Ein `if (MAX_STUFEN < 2) return -1;` im ersten Entwurf des Prüfers hätte CS0162
(„unerreichbarer Code") ausgelöst, weil der Vergleich zweier Konstanten zur Übersetzzeit
entschieden wird — die Zeile ist entfallen, die Zweistufigkeit steht jetzt im Kommentar.

**Konfliktmarker-Sweep.** Siehe Abschnitt 11.

---

## 10 Zurückgestellt: die KI-Leseaktion `energietraeger_pruefen`

Konzept § 4.4 und § 9 Punkt 4 sehen für HF2 zusätzlich eine **KI-Leseaktion**
`energietraeger_pruefen` im Bestandsmuster `Allgemein\KI\Aktionen\` vor, die die
Befundliste des Prüfers in den Chat gibt.

**Sie ist in K2 bewusst nicht umgesetzt.** Grund: Kollisionsvermeidung. Das
Aktionsregister und die umgebenden `Ki*`-Bereiche (`KiHarnisch\`, `KiKern\`,
`Allgemein\KI\`) sind parallele Arbeit an derselben Datei-Menge; eine neue Aktion hätte
dieselben Registrierungsstellen angefasst, an denen die KI-Etappe gerade arbeitet.
Zwei Sitzungen im selben Register erzeugen genau die Konfliktmarker, gegen die die
Hausregel (Konzept § 9 Punkt 5) den Sweep vorschreibt. Der fachliche Teil — der Prüfer
selbst — ist fertig und öffentlich aufrufbar; die Aktion ist danach ein Adapter von
wenigen Zeilen.

**ToDo für die Nacharbeit.** Neue Leseaktion im Bestandsmuster von
`Allgemein\KI\Aktionen\`, die

- `EnergieEinheitenPruefung.PruefeKatalog()` bzw. `PruefeProjekt(idProjekt)` ruft,
- die Befunde als Liste `(Träger, Code, Klartext)` zurückgibt,
- in der Positivliste der `KiPruefung` als **lesend** geführt wird (kein Schreibrecht),

sinnvollerweise gemeinsam mit dem zweiten offenen KI-Punkt des Konzepts (§ 9 Punkt 4:
neue Kostenart `zuschuss` in die Positivliste, gehört zu HF5/K5).

---

## 11 Abschluss und offene Punkte

**Konfliktmarker-Sweep.** `*.cs`, `*.md` und `*.resx` unterhalb von
`WindowsFormsApplication1\`, `Referenzlauf\`, `SpeicherEngine\` und den
Lösungswurzel-Dokumenten auf `<<<<<<<` geprüft, ohne `mit_Puffer_KI_Lösungsversuch\`,
`Tempkib2\`, `WindowsFormsApplication1 - Kopie\`, `.claude\`, `bin`/`obj`:
**872 Dateien geprüft, kein echter Treffer.** Die vier Fundstellen sind sämtlich der in
Backticks gesetzte Prosa-Verweis auf eben diesen Sweep —
`Konzept_Kosten_Energietraeger_EPOS-Plan.md:268` und `:283`,
`K1_Aufraeumung_Protokoll.md:106` und dieses Protokoll selbst.

**Offene Punkte.**

1. **KI-Leseaktion `energietraeger_pruefen`** — zurückgestellt, siehe Abschnitt 10.
2. **Referenzlauf-Vergleich und Duplizieren-Smoke** stehen aus (Abschnitt 7); wie in K1
   nicht Teil des Auftrags.
3. **Der Dialogausbau bleibt K3.** `ucFuelSettings` zeigt Name und Schalter noch nicht;
   bis dahin sind beide Spalten reine Datenhaltung. Ebenso die **blockierende** Prüfung
   beim Speichern (Konzept § 4.3) und die Anzeige „effektiv: 1 ⟨Einheit⟩ = X kWh".
4. **Die Nm³-Umbenennung ist M-B (K3)**, nicht K2 — `billing_unit` der Gasträger steht
   weiterhin auf `m³`.
5. **Faktor-0-Regeln** (Abschnitt 6, Nebenbefund) sind in K3 zu füllen oder
   abzuschalten.
6. **`ProjektDuplizierenCtrl.cs:48`** führt `energy_conversion` in der Ausschlussliste
   der projektbezogenen Tabellen. Das bleibt richtig — die Tabelle ist global, und die
   zwei neuen Spalten ändern daran nichts.
7. **`migration.manuell.sql:505`** kopiert `energy_conversion` weiterhin mit dem
   Spaltensatz **ohne** `faktor_name`/`aktiv`. Das ist beabsichtigt und korrekt: Das
   Handskript zieht Daten aus einer **Alt**-Datenbank, die die Spalten nicht kennt;
   Schritt 25 legt sie danach in der Zieldatenbank an und belegt sie vor. Eine Erwähnung
   im Skriptkommentar wäre trotzdem eine Freundlichkeit für den nächsten Leser —
   Einzeiler für K3 oder K6.

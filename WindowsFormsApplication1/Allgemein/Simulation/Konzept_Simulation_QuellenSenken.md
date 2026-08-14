# Konzept: Quellen und Senken von Wärmeerzeugern, Pufferspeicher und Brauchwasser-Stundenbilanz

**Fassung 12** · Stand 14.08.2026 · Status: abgestimmt (E1–E13), am Code, an der produktiven Datenbank **und an VDI 4640 Blatt 1 + Blatt 2** verifiziert, Entwurf zur Umsetzung · **Umsetzungsstand im Code: 0 %**
Bezug: `Konzept_TWW-Zapfprofile_WP-Plan_1.md` (Ausbaupfad Brauchwasser; liegt doppelt
vor — **maßgeblich ist die Fassung in der Repo-Wurzel**, die Kopie unter
`Allgemein/Waermespeicher/` ist inhaltsgleich, Stand 29.07.2026),
`Konzept_Variantenbericht.md`, `Konzept_Wirtschaftlichkeit.md`,
**[`ADR-001_Schema-Ausrollung.md`](ADR-001_Schema-Ausrollung.md)** (angenommen
14.08.2026 — ersetzt die Kapitel 5.6 und 13.2)
Codebasis: verifiziert am Stand 14.08.2026 — 49 Einzelaussagen der Fassung 11 durch
fünf unabhängige Prüfläufe kontrolliert, jede Abweichung adversarial gegengeprüft;
42 bestätigt, 7 in dieser Fassung korrigiert

### Fassungshistorie

| Fassung | Inhalt | Entscheidungen |
|---|---|---|
| 1 | Erstentwurf nach Codeanalyse: Quellen-/Senken-Zuordnung je Anlage, Pufferspeicher-Pflicht, Erdreichmodell, zweikanalige Brauchwasserbilanz | E1–E6 |
| 2 | Nach drei Prüfläufen gegen den Quellcode: vier sachliche Korrekturen (siehe unten), Vorab-Paket Bestandsfehler, neu kalkulierter Aufwand | + E7–E9 |
| 3 | Ladevorrang bei mehreren Erzeugern an einem Puffer ausgearbeitet (vormals offener Punkt O1); Ladephase aus der Bedarfskaskade herausgelöst | + E10 |
| 4 | PV-Sonderfall und Entladereihenfolge entschieden (vormals O1a/O1b), beide vorbelegt und übersteuerbar; Korrektur einer widersprüchlichen Formulierung aus Fassung 3 | + E11 |
| 5 | Umsetzungsdetails O3–O9 ausgearbeitet und entschieden (neues Kapitel 13): Erdreichmodell gegen VDI 4640 plausibilisiert, DB-Pfad vereinheitlicht, Anzeigen für n Puffer, Engine-Fehlerkanal statt MessageBox, PV-Budgetaufteilung, durchgängige Lokalisierung als eigenes Paket | + E12 |
| 6 | Datenbankschema gegen `migration.manuell.sql` geprüft | — |
| 7 | Schemaprüfung zurückgestuft, weil das Migrationsskript nicht der maßgebliche Stand ist | — |
| 8 | Schema an `Kenndaten.accdb` verifiziert (13.7), alle fünf Punkte geklärt. `Rücklauf` trägt den Umlaut (B0-4 ohne Ergebniswirkung); Löschweitergaben **existieren**; `ID_PUFFER` hat **keine** Beziehung; `Tab_Pufferspeicher` hängt **nicht** an der Projekt-Kaskade | — |
| 9 | Erdreichmodell auf VDI 4640 Blatt 1 umgestellt (13.1): Bodentyp-Katalog aus Tabelle 1 der Norm (λ und ρ·c_p, daraus a abgeleitet); Sondenformel um den normbegründeten 20-m-Abzug korrigiert; neutrale Zone 10–20 m bestätigt | — |
| 10 | Normgrundlage festgelegt: Der Entwurf 2021-12 von Blatt 1 ist die verbindliche Basis | + E13 |
| 11 | **VDI 4640 Blatt 2:2019 eingearbeitet** (13.1): Die Plausibilitätsprüfung nutzt jetzt die Auslegungstabellen der Norm — Kollektoren nach Klimazone (15 Zonen, DIN 4710) und Bodenart (Tab. A2), Sonden nach Wärmeleitfähigkeit, Sondenzahl und Volllaststunden (Tab. B2). Die bisherigen Faustwerte stammten aus der Ausgabe 2001 und sind überholt | — |
| **12** | **Codeverifikation 14.08.2026 eingearbeitet.** Schema-Ausrollung und Datenmigration auf **ADR-001** umgestellt (5.6 und 13.2 ersetzt; `UpdateDatabaseFromScript` und das extern erzeugte Migrationsskript endgültig verworfen — das externe DB-Migrationswerkzeug ist grundsätzlich nicht Teil der Anwendungsarchitektur). Sieben bestätigte Abweichungen korrigiert: MessageBox-Inventar (13.4, acht statt sechs Stellen plus DataRepository-Pfad), Ressourcenlage (13.6/11: de-DE-Variante und en-US-Formularsatelliten existieren), CREATE-TABLE-Vorbild vorhanden (6.6), Übergabeweg der Pufferliste (2.2), `Form_Quellprofil`-`.resx` (2.1), ID-Stabilität differenziert (2.3/5.2), Kapazitätsanzeige präzisiert (13.3). Neu: dritter Puffer-Speicherpfad über `Form_Start` (2.3), `wp_quellspeicher`-Integration (6.2), B0-7 erweitert, **B0-11** ergänzt, Layoutherleitung 4.1 korrigiert, Befundliste Kapitel 8 erweitert. Aufwand neu: **64–81,5 PT** | — |

> **Die vier sachlichen Korrekturen aus der Codeverifikation (Fassung 2).**
>
> 1. **Pufferspeicher sind bereits Anlagen.** `WizardItemClass.PUFFER_TYP = 12`;
>    der Projektbaum legt Puffer als Zeile in `Tab_Energieanlagen` an
>    (`WizardCtrl.cs:162-166, 214`). Es existieren damit **drei** konkurrierende
>    Repräsentationen eines Projekt-Puffers. Kapitel 2.3 und 5 sind daraufhin neu
>    geschrieben; der Fremdschlüssel zeigt auf `Tab_Pufferspeicher.ID`
>    (E7, Begründung 5.2).
> 2. **`solarthermie_list` ist toter Code** — die in Fassung 1 geforderte Umstellung
>    auf Anlagen-IDs entfällt (Kapitel 6.2).
> 3. **`Tab_ErgebnisPufferspeicher` kann nicht über `StelleEnergieSpaltenSicher()`
>    entstehen** — das Muster kann nur `ADD COLUMN`. Neuer Weg in Kapitel 6.6.
> 4. **Der Aufwand war um Faktor ~2,5 zu niedrig angesetzt.** Kapitel 9 ist neu
>    kalkuliert; Kapitel 8 (Vorab-Paket Bestandsfehler) und Kapitel 11
>    (Prüfprotokoll) sind daraufhin entstanden.
>
> **Korrektur in Fassung 4 gegenüber Fassung 3:** Der dortige Vorschlag zu O1b lautete
> „Entladung in umgekehrter Ladepriorität" mit der Begründung, der Vorrangspeicher
> solle frei bleiben. Beides zusammen ist widersprüchlich — frei bleibt der vorrangig
> geladene Speicher nur, wenn er **zuerst** entladen wird. Die Regel gilt daher in
> *gleicher* Richtung wie die Ladepriorität (Begründung in 3.6).

> **Einordnung.** Dieses Konzept ist Bestandteil von EPOS-Plan und beschreibt die
> Erweiterung der Simulationskonfiguration um eine explizite **Quellen-/Senken-Zuordnung
> je Wärmeerzeuger-Anlage**, die Verbesserung der **Pufferspeicher-Auswahl**
> (Trennung Heizung/Brauchwasser, Pflicht zur vorherigen Anlage im Projekt) sowie die
> **stundenwertbasierte Führung des Brauchwasserbedarfs** als eigenen Bedarfskanal
> neben dem Heizwärmebedarf.

---

## 1. Getroffene Entscheidungen

| # | Punkt | Entscheidung |
|---|---|---|
| E1 | Zuordnungsebene | Quelle und Senke werden **je einzelner Anlage** zugeordnet (je Zeile in `Tab_Energieanlagen`), nicht je Erzeugertyp |
| E2 | Anzahl Senken | **Hauptsenke + optionale Zweitsenke** je Anlage. Die Hauptsenke ist Pflicht; die Zweitsenke dient ausschließlich der Überschussverwertung (Solarthermie: primär Puffer Brauchwasser, Überschuss in Puffer Heizung) |
| E3 | Pufferspeicher-Pflicht | Ein Pufferspeicher ist **nur dann Pflicht, wenn eine Quelle oder Senke vom Typ Pufferspeicher gewählt wurde** — dann muss er zuvor **im Projekt angelegt** sein (kein implizites Kopieren aus den Stammdaten mehr). Ohne Puffer-Quelle/-Senke ist kein Pufferspeicher erforderlich |
| E4 | Quellentyp Erdreich | **Vereinfachtes Erdreichmodell**: Erdreichtemperatur aus den Klimadaten der Projekt-Klimaregion (gedämpfter, phasenverschobener Jahresgang, Parameter Tiefe/Bodentyp). Monatsprofil/CSV bleiben als Alternativen |
| E5 | Umsetzungsumfang | **Paketweise Auslieferung** (Kapitel 9), jedes Paket einzeln verifizierbar. TWW-Zapfprofile bleiben eigenes Konzept |
| E6 | Brauchwasser-Bilanz | Der Brauchwasserbedarf wird als **eigener Stundenwerte-Kanal** (`float[8760]`) geführt; die gesamte Erzeuger-Kaskade rechnet **zweikanalig** (Heizung / Brauchwasser) statt auf einem Summenvektor |
| **E7** | **FK-Ziel Pufferspeicher** | Der Fremdschlüssel zeigt auf **`Tab_Pufferspeicher.ID`** (Projektkopie). Die heutige Dedup-Regel je (`Bezeichner`, `ID_Projekt`) wird aufgehoben, damit zwei baugleiche Speicher im Projekt unterscheidbar sind. Das Speichermuster der Anlagen (`DELETE all + INSERT`) bleibt unangetastet |
| **E8** | **Umfang Erzeuger** | **Alle vier Wärmeerzeuger** (WP, Solarthermie, Heizkessel, BHKW) werden in derselben Stufe auf Quelle/Senke umgestellt. Beim BHKW schließt das die Ablösung des skalaren Pendelspeichers durch `SimulationPufferspeicher` ein |
| **E9** | **Bestandsfehler** | Die bei der Prüfung gefundenen Bestandsfehler (Kapitel 8) werden als **eigenes Vorab-Paket B0** ausgeliefert, jeder Fix mit dokumentierter Ergebnisänderung — vor dem eigentlichen Umbau |
| **E10** | **Ladevorrang am Puffer** | Laden mehrere Erzeuger denselben Speicher, entscheidet eine **Ladepriorität je Anlage und Senke** — mit Vorgabe **Solarthermie → Wärmepumpe → BHKW → Heizkessel** (nach Grenzkosten) und manueller Übersteuerung. Ergänzend eine **Ladeobergrenze** in zwei Stufen: eine zweite Abschaltschwelle am Puffer für nachrangige Erzeuger (Solar-Reservezone) und optional eine eigene Obergrenze je Anlage. Beide Grenzen sind per Default verhaltensneutral (3.4) |
| **E11** | **PV-Sonderfall und Entladereihenfolge** | Beide Regeln werden **vorbelegt und sind übersteuerbar**: (a) Eine Wärmepumpe im Betriebsmodus `PV` bleibt gegenüber Solarthermie **nachrangig**; über eine eigene PV-Ladepriorität je Anlage lässt sich das umkehren (3.5). (b) Bedienen mehrere Puffer denselben Kanal, wird **in gleicher Reihenfolge wie die Ladepriorität entladen** — der Speicher mit der günstigsten Nachladung zuerst; je Speicher übersteuerbar (3.6) |
| **E12** | **Umsetzungsdetails (Kapitel 13)** | Erdreichmodell wird gegen **VDI 4640** plausibilisiert, Bodentyp-Katalog mit belegten Kennwerten (13.1). Schema und Datenmigration laufen über die versionierte **`SchemaMigration`** nach ADR-001, ausschließlich über den DB-Pfad von `DataRepository` (13.2). Anzeigen werden auf **n Pufferspeicher** umgestellt statt auf einen Alias (13.3). MessageBoxen in der Engine werden durch einen **Protokoll-/Fehlerkanal** nach dem Muster `SimuliereUndSpeichere(id, out fehler)` ersetzt (13.4). Das PV-Ladebudget geht **erst an die Hauptsenke, dann an die Zweitsenke** (13.5). Der Simulationsbereich wird **durchgängig lokalisiert** (13.6, eigenes Paket) |
| **E13** | **Normgrundlage Erdreichmodell** | Verbindliche Basis ist **VDI 4640 Blatt 1, Entwurf 2021-12** (Tabelle 1). Auf den Weißdruck wird **nicht** gewartet — die Umsetzung beginnt mit den Entwurfswerten. Ergebnisse und Dokumentation weisen den Entwurfsstand aus; erscheint der Weißdruck später, ist nur Tabelle 1 gegenzuprüfen (13.1) |

---

## 2. Ausgangslage im Code (verifizierte Ist-Analyse)

### 2.1 Was bereits existiert

Die Grundmechanik für Quelle/Senke ist für **Wärmepumpen** gebaut und dient als Vorlage:

- **`Tab_Energieanlagen`** trägt (per `WaermequelleClass.SchemaSicherstellen()`,
  `:81-123`, zur Laufzeit angelegte) Spalten: `Prioritaet`, `WQ_Typ`, `WQ_Temp`,
  `WQ_Monatswerte`, `WQ_Wochenwerte`, `WQ_CSV`, `WQ_Puffer`, `WQ_Spreizung`,
  `WQ_Regeneration`, `WQ_Unbegrenzt`, `WS_Typ`, `BM_Typ`.
- **Quellentypen** (`WaermequelleClass.TypWerte`, `:54-57`): `Aussenluft`, `Konstant`,
  `Pufferspeicher`, `Profil` (12 Monats- + 168 Wochenwerte), `CSV` (8760 Stundenwerte).
  Eingabedialoge `Form_Quellprofil` und `Form_QuellePufferspeicher` existieren,
  beide **programmatisch ohne Designer**. `Form_QuellePufferspeicher` hat auch keine
  `.resx`; zu `Form_Quellprofil` existiert eine inhaltlich leere, wirkungslose
  `Form_Quellprofil.resx` (VS-Gerüst ohne Ressourceneinträge, per SDK-Glob als
  EmbeddedResource eingebunden — der Code-Kommentar `:20-21` „keine .resx" ist
  falsch; Datei bei Gelegenheit entfernen).
- **`WS_Typ`** (`Beides`/`Warmwasser`/`Heizung`) ist eine **Bedarfsart**, keine
  hydraulische Senke; ausgewertet ausschließlich in `SimulationWaermepumpe`
  (`SenkeAbziehen`, `:618-639`, mit Warmwasservorrang bei `Beides`).
- **`SimulationPufferspeicher`** ist ein generisches Energiebilanzmodell
  (Q_max = Volumen·1,16·ΔT/1000, Hysterese, füllstandsanteilige Verluste,
  Regeneration) und bereits mehrfach instanzierbar.
- **`Form_Simulation_Config`**: programmatisch aufgebaute Übersichts-ListView
  (`:188-232`), Bearbeitung per Doppelklick — heute nur für WP-Zeilen aktiv (`istWP`,
  `:253`, `Tag`-Zuweisung nur im WP-Zweig `:278`).

### 2.2 Heutige Grenzen, die dieses Konzept aufhebt

1. **Senke nur als Bedarfsart** — „Heizkreis / Pufferspeicher Heizung /
   Pufferspeicher Brauchwasser" existiert nicht; kein Bezug auf einen konkreten Speicher.
2. **Puffer-Zuordnung je Erzeuger-Typ statt je Anlage:** `Z_ProjektPufferSp.Erzeuger`
   ist ein Gewerk-**String**; `SimulationControl.cs:74-110` wertet nur den **ersten**
   Eintrag mit `Erzeuger == "Wärmepumpe"` aus (`break` in `:108`, der auch dann greift,
   wenn der Speicherdatensatz fehlt). Zuordnungen zu Kessel/BHKW/Solarthermie sind
   heute **wirkungslos**, obwohl der Dialog sie anbietet.
3. **Genau ein Speicher projektweit**, keine Trennung Heizung/Brauchwasser.
4. **Puffer-Auswahl aus den Stammdaten:** `Form_QuellePufferspeicher.cs:177-179`
   listet `Tab_Pufferspeicher_STAMM` direkt; `Form_Simulation_Config.cs:1473-1477`
   füllt die Rubrik-ComboBox aus derselben Quelle, und erst `btn_Hinzu_Click`
   (`:1617-1621`) übergibt `Form_KonfigPufferspeicher` die Liste — **gefiltert** über
   `AktivePufferSp()`, die volle Stammliste nur als Fallback ohne aktive
   Puffer-Checkbox. `Z_ProjektPufferSpCtrl.Insert()`
   (`:45-49`) kopiert beim Speichern implizit ins Projekt. Referenzierung per
   **Bezeichner-String**, nicht per ID.
5. **Quellentyp „Erdreich" fehlt.** Luft-Wasser-WP wird per Literalvergleich
   `Tab_WP.Typ == "Luft-Wasser"` erkannt (`WaermequelleClass.cs:247`, `:318`).
   Volltextsuche nach `erdreich|erdsonde|kusuda|geotherm|bodentemp`: **null Treffer**.
6. **Ein Bedarfsvektor:** `Do_Simulation` reicht `Eingang`/`Ausgang` (`float[8760]`)
   seriell durch die Kaskade (`:124-187`). Der Brauchwasseranteil
   (`brauchwasserwerte[8760]`) wird stündlich ermittelt, aber sofort in den
   Gesamtvektor addiert (`SimulationWaermebedarf.cs:239-245`) — nur die WP kennt ihn
   danach noch, und zwar **ungekürzt** statt als Rest nach vorgeschalteten Erzeugern
   (`SimulationControl.cs:236-238`). Kessel, BHKW und Solarthermie sehen ihn nicht.
   Verschärfend kappt die WP den vollen WW-Vektor stündlich auf den Restbedarf
   (`SimulationWaermepumpe.cs:238-243`) — steht sie **nicht** an erster
   Kaskadenposition, wird der gesamte Rest als Warmwasser klassifiziert
   (`rest_heiz = 0`) und WP-Module mit `WS_Typ='Heizung'` bleiben systematisch aus.
7. **Solarthermie-Überschuss wird verworfen** (`SimulationSolarthermie.cs:167-169`:
   Kappung am Momentanbedarf, `Ueberschuss[]` nur gezählt) — größter fachlicher Hebel.
8. **BHKW-Pendelspeicher** ist ein Skalar (`SimulationControl.cs:321`:
   `Volumen · 20000/860`) mit hartkodierten Regelschwellen 30/10/20 %
   (`SimulationBHKW.cs:425, 440, 457`) — kein `SimulationPufferspeicher`.
   Zusätzlich verwirft `SimulationControl.cs:327` den vom BHKW selbst berechneten,
   speicherbewussten `waermerestbedarf` und bildet den Rest als schlichte
   Vektordifferenz — geladene Speicherenergie gilt damit als sofort bedarfsdeckend.

### 2.3 Der Pufferspeicher hat heute drei Repräsentationen *(neu in Fassung 2)*

| Ebene | Tabelle | Entsteht durch | Bedeutung |
|---|---|---|---|
| **Anlage** | `Tab_Energieanlagen`, `ID_Type = 12` (`PUFFER_TYP`) | Projektbaum-Kontextmenü → `PufferSpKontextMenuCtrl.cs:133-134` **oder** Startseite → `Form_Start.cs:1580-1605` (`pBox_Pufferspeicher_Click`) — beide typgefiltert über `WizardCtrl.Add_WP_Waermeerzeuger`; nur der `Form_Start`-Pfad überträgt vollständige Modelle | „Der Anwender hat einen Puffer im Projekt angelegt" |
| **Gerätedaten** | `Tab_Pufferspeicher` (`ID_Projekt`) | `PufferSpCtrl.CopyFromStamm` (`:151-197`) — aufgerufen aus `WizardCtrl.cs:164` **und** implizit aus `Z_ProjektPufferSpCtrl.Insert` (`:47-49`) | Projektkopie der Stammdaten |
| **Zuordnung** | `Z_ProjektPufferSp` | `Form_Simulation_Config.btn_Speichern_Click` (`:1535-1555`) | Gewerk ↔ Speicher, Vor-/Rücklauf, Schwellen |

Daraus folgen die zentralen Inkonsistenzen:

- **Ein Puffer kann simuliert werden, ohne je „angelegt" worden zu sein** — die
  Simulationskonfiguration erzeugt `Tab_Pufferspeicher` + `Z_ProjektPufferSp` ohne
  Anlagenzeile. Genau das unterbindet E3.
- **Umgekehrt** erzeugt das Kontextmenü Anlage + Gerätedaten, aber keine Zuordnung —
  der Puffer ist „angelegt", rechnet aber nicht mit.
- **`Tab_Energieanlagen` mit `ID_Type = 12` wird von der Simulation nie gelesen**
  (Volltextsuche `PUFFER_TYP`: nur `WizardItemClass`, `WizardCtrl`,
  `PufferSpKontextMenuCtrl`, `Form_PufferSp`). Für Puffer-Zeilen bleiben `Vorlauf`,
  `Rücklauf`, `Volumen` konstant 0, weil die erzeugenden Stellen nur vier Felder
  setzen (`Form_PufferSp.cs:98-104`, `PufferSpKontextMenuCtrl.cs:115-119`) —
  Ausnahme ist der `Form_Start`-Pfad (`:1594`), der als einziger die vollständigen
  Modelle aus `werzctrl.items` übergibt.
- **`Form_PufferSp.cs:101` schreibt die STAMM-ID in `ID_PUFFER`**, obwohl dort laut
  `PufferSpCtrl.cs:148-150` die Projekt-ID stehen muss. Repariert wird das
  nachträglich in `WizardCtrl.cs:164-165` über den Bezeichner — schlägt die
  Auflösung fehl, überlebt die falsche ID stillschweigend.
- **`Tab_Energieanlagen.ID` ist bei Puffern instabil** *(präzisiert in Fassung 12)*:
  Der Wizard-Pfad (`WizardParent.cs:661-664`) löscht **alle** Anlagen des Projekts
  und fügt sie neu ein; Kontextmenü und Startseite
  (`PufferSpKontextMenuCtrl.cs:133-134`, `Form_Start.cs:1603`) nutzen die
  **typgefilterte** Überladung (`WizardCtrl.cs:32-36`, `AND ID_Type = ?`) und
  erneuern nur die Puffer-Zeilen. Die ID ist ein Access-AutoWert — nach jedem
  Speichern haben mindestens die **Puffer**-Anlagen neue IDs, über den Wizard alle.
  Das ist der Grund für E7.

---

## 3. Zielbild: das Quellen-/Senken-Modell

### 3.1 Begriffe

Jede Wärmeerzeuger-**Anlage** (Zeile in `Tab_Energieanlagen`: WP, Heizkessel, BHKW,
Solarthermie) erhält:

- **genau eine Hauptsenke** (Pflicht, E1/E2):
  - `HEIZKREIS` — direkte Deckung des Momentanbedarfs (Verhalten wie heute),
  - `PUFFER_HEIZUNG` — die Anlage lädt einen Projekt-Puffer mit Verwendung „Heizung",
  - `PUFFER_BRAUCHWASSER` — dito mit Verwendung „Brauchwasser";
- **optional eine Zweitsenke** (E2), ausschließlich zur Verwertung von Überschuss
  bzw. verbleibendem Ladepotenzial, **nie** zur Deckung von Pflichtbedarf;
- **Wärmepumpen zusätzlich genau eine Wärmequelle**:
  - Typ **Luft-Wasser**: fest `Luft` (Außentemperatur der Klimaregion, nicht änderbar),
  - Typ **Sole/Wasser**: `Erdreich` *(neu, E4)*, `Konstante Temperatur`,
    `Temperaturprofil` (Monat/Tag → Jahr), `Stundenprofil CSV`, `Pufferspeicher`
    (Heizung **oder** Brauchwasser — Abwärmenutzung, kaskadierte WP).

Die bestehende Bedarfsart `WS_Typ` bleibt als **Feinsteuerung für die Hauptsenke
`HEIZKREIS`** erhalten (Default `Beides` mit WW-Vorrang). Bei Puffer-Senken ist der
Kanal durch die **Verwendung des Puffers** bestimmt; `WS_Typ` wird dort ignoriert.

### 3.2 Kanalmodell (E6)

```
Kanal HEIZUNG      [8760] = Waermebedarf − brauchwasserwerte   (elementweise, ≥ 0)
Kanal BRAUCHWASSER [8760] = brauchwasserwerte

Gesamtwärmebedarf  [8760] = HEIZUNG + BRAUCHWASSER   (identisch mit heute)
```

Der Heizkanal wird bewusst als **Residuum** gebildet. Begründung: `Waermebedarf`
enthält die Netzverluste, die in `SimulationWaermebedarf.cs:261` **nach** Addition der
Brauchwasserwerte (`:245`) aufgeschlagen werden. Das Residuum trägt sie damit
vollständig — das entspricht exakt der heutigen impliziten Zuordnung und ist die
einzige altverhaltenserhaltende Variante. **Damit ist der frühere offene Punkt O2
entschieden: Netzverluste vollständig auf den Heizkanal.**

Jede Anlage bedient gemäß Senke den passenden Kanal; Pufferspeicher entladen in „ihren"
Kanal. Die Ergebnisgrößen bleiben kompatibel, werden aber erstmals **exakt**, weil der
WW-Restbedarf durch die Kaskade mitgeführt wird statt am WP-Modul aus dem Summenvektor
rekonstruiert zu werden (`SimulationWaermepumpe.cs:240-244`).

### 3.3 Pufferspeicher im Projekt (E3/E7)

Ein Pufferspeicher, der als Quelle **oder** Senke dienen soll, muss zuvor als
**Projekt-Pufferspeicher** angelegt worden sein. Maßgeblich ist die Zeile in
`Tab_Pufferspeicher` (E7); sie entsteht künftig **ausschließlich explizit** über die
Puffer-Verwaltung (4.3) oder den Projektbaum — das implizite `CopyFromStamm` in
`Z_ProjektPufferSpCtrl.Insert` entfällt mit der Ablösung dieser Tabelle (5.4).

Jeder Projekt-Puffer erhält die Eigenschaft **Verwendung** = `Heizung` |
`Brauchwasser` sowie seine Betriebsparameter (Vor-/Rücklauf → Q_max,
Ein-/Abschaltschwelle). Mehrere Anlagen dürfen denselben Puffer laden (n:1) — ihre
Reihenfolge regelt die Ladepriorität (3.4).
Ist keine Puffer-Quelle/-Senke gewählt, ist **kein** Pufferspeicher erforderlich.

### 3.4 Ladepriorität und Ladeobergrenzen (E10)

#### Drei Prioritätsbegriffe, klar getrennt

Die Anwendung kennt bereits zwei Reihenfolgen; die Ladepriorität ist die dritte und
bewusst **unabhängig** von den anderen:

| Begriff | Speicherort | Was er regelt |
|---|---|---|
| Kaskadenposition | `Tab_Einstellungen.Tool_1..4` | Welcher Erzeuger**typ** den *Bedarf* zuerst deckt |
| Anlagenpriorität | `Tab_Energieanlagen.Prioritaet` | Reihenfolge der Anlagen eines Typs untereinander |
| **Ladepriorität** *(neu)* | `Tab_Energieanlagen.WS_Ladeprio` / `WS_Ladeprio2` | Welcher Erzeuger einen *Puffer* zuerst lädt |

Die Trennung ist der fachliche Kern: Solarthermie steht in der Bedarfskaskade
typischerweise hinten — beim Speicherladen soll sie aber zuerst zum Zug kommen.

#### Vorgabe-Rangfolge

Ohne eigene Eingabe (`WS_Ladeprio = 0`) gilt eine Rangfolge nach den Grenzkosten der
erzeugten Wärme:

| Rang | Erzeuger | Begründung |
|---|---|---|
| 10 | **Solarthermie** | Grenzkosten ≈ 0. Nicht genutzte Einstrahlung ist unwiederbringlich verloren — anders als PV-Strom, der eingespeist werden kann. Deshalb auch dann vorrangig, wenn eine Wärmepumpe im PV-Modus fährt |
| 20 | **Wärmepumpe** | Grenzkosten = Strompreis / JAZ; profitiert zusätzlich von niedrigen Speichertemperaturen (besserer COP), lädt also günstiger in einen entleerten Speicher |
| 30 | **BHKW** | Wärme ist Koppelprodukt; die Bewertung hängt von der Stromgutschrift ab. Bei stromgeführter Fahrweise mit hoher Vergütung ist ein Vorzug vor der Wärmepumpe sinnvoll — dafür die manuelle Übersteuerung |
| 40 | **Heizkessel** | höchste Grenzkosten, soll nur nachheizen |

Manuelle Werte 1–99 übersteuern die Vorgabe. **Bei Gleichstand** entscheidet die
Kaskadenposition, dann `Tab_Energieanlagen.Prioritaet`, dann die Anlagen-ID — die
Reihenfolge ist damit immer deterministisch und nie von der Datenbankreihenfolge
abhängig.

#### Ladeobergrenzen: zwei Stufen, per Default neutral

Die reine Reihenfolge löst den Zeitkonflikt nicht: Lädt die Wärmepumpe morgens den
Puffer bis zur Abschaltschwelle, findet die Sonne mittags keinen Platz mehr. Dagegen
gibt es zwei Stellschrauben, die zusammen alle drei diskutierten Verhaltensweisen
abdecken:

| Stellschraube | Ort | Wirkung |
|---|---|---|
| **`Schwelle_Aus_Nachrang`** | je Pufferspeicher | Obergrenze für **alle nachrangigen** Erzeuger dieses Puffers (z. B. 70 %). Der Bereich darüber bleibt der vorrangigen Quelle vorbehalten — die klassische Solar-Reservezone |
| **`WS_Ladegrenze`** / `WS_Ladegrenze2` | je Anlage und Senke | Eigene Obergrenze in % des Speichers. Überschreibt die Puffer-Regel für genau diese Anlage |

Auflösungsregel je Ladevorgang:

```
Obergrenze =  WS_Ladegrenze          , wenn gesetzt (> 0)
           =  Schwelle_Aus           , wenn die Anlage die vorrangige an diesem Puffer ist
           =  Schwelle_Aus_Nachrang  , sonst
Ladefähigkeit = Q_max · Obergrenze − SOC
```

Vorrangig ist die Anlage mit der kleinsten Ladeprioritätszahl an diesem Puffer.

**Default-Verhalten:** `Schwelle_Aus_Nachrang` wird bei der Migration und bei
Neuanlage auf `Schwelle_Aus` gesetzt, `WS_Ladegrenze` auf 0 (nicht gesetzt). Damit
wirkt zunächst **nur die Reihenfolge**, ohne Reservezone — verhaltensneutral zum
Bestand. Wer eine Solarreserve will, setzt eine Zahl am Puffer; wer je Erzeuger
feinsteuern will, nutzt die Anlagengrenze. Alle drei Ausprägungen sind damit ohne
Codeänderung erreichbar.

#### Pflege und Anzeige

Gepflegt werden Ladepriorität und Ladegrenze im Senkendialog der jeweiligen Anlage
(4.2) — dort, wo die Zuordnung entsteht. Der Pufferspeicher-Dialog (4.3) zeigt
ergänzend **alle ladenden Anlagen in ihrer wirksamen Reihenfolge** samt Obergrenze,
damit der Konflikt dort sichtbar wird, wo er entsteht. Die Anzeige ist die
maßgebliche Kontrollinstanz; sie wird aus denselben Daten berechnet, die die
Simulation verwendet.

### 3.5 Sonderfall Wärmepumpe im PV-Modus (E11a)

Fährt eine Wärmepumpe im Betriebsmodus `PV` (`BM_Typ = "PV"`, heute ausgewertet in
`SimulationWaermepumpe.cs:433-445`), lädt sie den Speicher aus PV-Überschussstrom.
Die naheliegende Frage ist, ob sie damit vor die Solarthermie rücken sollte.

**Vorbelegung: nein — die Wärmepumpe bleibt nachrangig.** Begründung: Nicht genutzte
Solarwärme ist unwiederbringlich verloren, nicht genutzter PV-Strom dagegen
einspeisbar. Verdrängt die Wärmepumpe die Solarthermie aus dem Speicher, entsteht ein
echter Ertragsverlust; im umgekehrten Fall entsteht Einspeisevergütung statt
Eigenverbrauch. Der Saldo spricht in aller Regel für den Solarvorrang.

**Übersteuerbar** über eine eigene PV-Ladepriorität je Anlage und Senke
(`WS_Ladeprio_PV`, 5.3). Sie greift **ausschließlich in Stunden mit PV-Überschuss**;
in allen übrigen Stunden gilt die reguläre Ladepriorität. Damit ist die Priorität
erstmals zeitabhängig — technisch ein Zusatzfeld in der Prioritätsauflösung, die je
Stunde ohnehin ausgewertet wird.

```
Ladeprio(Anlage, Stunde) =  WS_Ladeprio_PV   , wenn gesetzt (> 0)
                                               UND Betriebsmodus = PV
                                               UND PV-Überschuss in dieser Stunde
                         =  WS_Ladeprio      , sonst  (0 → Vorgabe nach 3.4)
```

Default `WS_Ladeprio_PV = 0` → keine Sonderregel, Verhalten wie in 3.4. Wer die
Wärmepumpe bei PV-Überschuss vorziehen will, trägt einen Wert kleiner als die
Solar-Priorität ein (z. B. 5). Die Regel ist bewusst nicht auf Wärmepumpen
beschränkt — sie funktioniert für jede Anlage mit PV-gekoppeltem Betriebsmodus.

### 3.6 Entladereihenfolge bei mehreren Puffern je Kanal (E11b)

Bedienen mehrere Speicher denselben Kanal (z. B. zwei Heizungspuffer), muss die
Entladung eine Reihenfolge haben.

**Vorbelegung: in gleicher Richtung wie die Ladepriorität** — der Speicher, dessen
Nachladung am günstigsten ist, wird zuerst geleert. Ein überwiegend solar geladener
Speicher wird also **zuerst** entladen, damit er wieder aufnahmefähig ist, wenn die
Sonne kommt. Bliebe er voll, könnte die Solarthermie nicht einspeisen und ihr Ertrag
wäre verloren — dieselbe Logik, die schon der Ladepriorität (3.4) und der
Reservezone zugrunde liegt.

*(Anmerkung: Fassung 3 schlug hier „umgekehrte Ladepriorität" vor. Das war ein
Formulierungsfehler — die dort mitgegebene Begründung „damit der Vorrangspeicher frei
bleibt" verlangt genau die hier festgelegte Richtung.)*

Der Automatikwert eines Speichers leitet sich aus der besten Ladepriorität ab, die an
ihm anliegt:

```
Entladeprio(Puffer) =  Entladeprio            , wenn gesetzt (> 0)
                    =  min( Ladeprio aller an diesem Puffer ladenden Anlagen ) , sonst
```

Kleinere Zahl bedeutet früher entladen. Ein nur vom Kessel geladener Speicher (40)
wird damit nach dem solar geladenen (10) herangezogen. Bei Gleichstand entscheidet
die Puffer-ID — deterministisch.

**Übersteuerbar** über die Spalte `Entladeprio` je Pufferspeicher (5.1), gepflegt im
Pufferdialog (4.3). Damit lässt sich auch die entgegengesetzte Strategie einstellen
(hochwertige Wärme schonen und zuletzt nutzen), falls ein Projekt das verlangt.

Die Regel betrifft ausschließlich Puffer **desselben Kanals**; Heizungs- und
Brauchwasserspeicher bedienen getrennte Kanäle und konkurrieren nicht.

---

## 4. Anwendersicht (Konfiguration unter „Simulation")

### 4.1 Erweiterte Erzeuger-Übersicht in `Form_Simulation_Config`

```
┌─ Übersicht Wärmeerzeuger ────────────────────────────────────────────────────────────┐
│ Prio │ Erzeuger     │ Anlage        │ Wärmequelle (*)   │ Wärmesenke (*)  │ Zweitsenke │
├──────┼──────────────┼───────────────┼───────────────────┼─────────────────┼────────────┤
│ 1    │ Wärmepumpe   │ Vitocal 300   │ Erdreich (1,5 m)  │ Puffer Heizung  │ –          │
│ 1    │ Wärmepumpe   │ Vitocal 350   │ Konstant 10 °C    │ Puffer Brauchw. │ –          │
│ 2    │ Heizkessel   │ Vitola 200    │ –                 │ Heizkreis       │ –          │
│ 3    │ Solarthermie │ Vitosol 200-F │ –                 │ Puffer Brauchw. │ Puffer Hzg.│
│ 4    │ BHKW         │ Energator G   │ –                 │ Puffer Heizung  │ Heizkreis  │
├──────┴──────────────┴───────────────┴───────────────────┴─────────────────┴────────────┤
│ Pufferspeicher im Projekt:  PS 800 (Heizung, 800 l) · WW 500 (Brauchwasser, 500 l)     │
│ [Pufferspeicher anlegen…]  [Bearbeiten…]  [Entfernen]                                  │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

**Layoutzwang (Prüfbefund, Herleitung korrigiert in Fassung 12):** Die Übersicht wird
dynamisch bemessen — ihre Höhe folgt der Position der Pufferspeicher-Rubrik
(`groupBox_Uebersicht.Size = new Size(…, groupBox_PufferSp.Top − 109 − 10)`,
`Form_Simulation_Config.cs:194`; ListView vierseitig verankert, `:208-210`; die Rubrik
selbst verschiebt das Layout um `VERSCHIEBUNG = 105`, `:1051`). Das ergibt Platz für
3–4 Zeilen und 8 Spalten. Mit zwei zusätzlichen Spalten und Zeilen für **alle**
Erzeuger reicht das nicht. **Der Umbau der Übersicht und der Entfall der
Pufferspeicher-Rubrik (4.4) sind deshalb nicht getrennt planbar** — der Bereich der
Rubrik geht an die Übersicht, und weil die Höhe an `groupBox_PufferSp.Top` hängt,
wächst die Übersicht erst mit deren Entfall.

**Zwingende Vorarbeit:** Die Spaltenindizes 3/4/5/6/7 stehen an drei Stellen doppelt
(Tooltip-`switch` `:571-606`, Doppelklick-Dispatcher `:637-661`, Reihenfolge der
`Columns.Add` `:212-219`). Vor dem Einfügen neuer Spalten sind **Konstanten**
einzuführen (`COL_PRIO`, `COL_QUELLE`, `COL_SENKE`, …), sonst entstehen stille
Fehlbedienungen. Ebenso ist das `else`-Fallback `int spalte = 4;` (`:630`) auf eine
Whitelist umzustellen — sobald jede Zeile ein `Tag` trägt, öffnet sonst ein
Doppelklick auf die Bezeichnerspalte den Wärmequellen-Dialog. Ein **zweiter** Satz
hartkodierter Indizes hängt an der Zuordnungstabelle `listView1` (`:392-427` gegen
die positionale Spaltenanlage `:81-85`) — er entfällt mit der Rubrik (Etappe B in
4.4), ist bis dahin aber bei jeder Spaltenänderung mitzudenken.

Änderungen im Einzelnen:

- `AktualisiereErzeugerUebersicht` (`:239-293`): `istWP`-Filter (`:253`) entfällt;
  `zeile.Tag = anlagen[a]` wandert aus dem WP-Zweig (`:278`) heraus.
- `AnlagenImProjekt` (`:300-336`): SQL um die neuen Spalten erweitern, `AnlagenInfo`
  (`:43-53`) entsprechend.
- `BetriebsmodusBearbeiten` (`:446-546`): Texte sind WP-spezifisch — für die übrigen
  Erzeuger sperren oder umtexten.
- Der Layoutcode wandert in eine eigene `partial class`-Datei
  (`Form_Simulation_Config.Uebersicht.cs`); die Datei hat bereits 1704 Zeilen.

### 4.2 Senkendialog `Form_Waermesenke` (neu)

```
┌─ Wärmesenke — Vitocal 300 ─────────────────────────────────────┐
│  Hauptsenke:                                                   │
│   (•) Heizkreis (direkt)                                       │
│        Bedarfsart: [Beides ▾]  (nur bei Heizkreis)             │
│   ( ) Pufferspeicher Heizung      [PS 800 (800 l)  ▾]          │
│   ( ) Pufferspeicher Brauchwasser [WW 500 (500 l)  ▾]          │
│                                                                │
│     Ladepriorität: [nach Vorgabe (20) ▾]   Lädt als 2. von 3   │
│     Ladeobergrenze: [ ] eigene Vorgabe [ 70 ] % des Speichers  │
│     Bei PV-Überschuss: [unverändert ▾]  (nur Betriebsmodus PV) │
│                                                                │
│  ☐ Zweitsenke (nur Überschuss/Ladepotenzial):                  │
│      [Pufferspeicher Heizung ▾]  [PS 800 (800 l) ▾]            │
│      Ladepriorität: [nach Vorgabe ▾]                           │
│                                                                │
│  ⓘ Für Puffer-Senken muss der Speicher im Projekt              │
│    angelegt sein.            [Pufferspeicher anlegen…]         │
│                                         [Abbrechen] [OK]       │
└────────────────────────────────────────────────────────────────┘
```

Umsetzung nach dem verifizierten Bestandsmuster: **komplett programmatisch, kein
Designer, keine `.resx`**, Klasse erbt direkt von `Form`, Datenübergabe über
öffentliche Felder, Validierung im `btnOk_Click` mit `DialogResult = DialogResult.None`
(Vorbild `Form_QuellePufferspeicher.cs:18, 20, 251-285`). Die Puffer-Dropdowns listen
**nur Projekt-Puffer passender Verwendung**; existiert keiner, blockiert OK mit Hinweis
und Absprung.

*Hinweis zur Lokalisierung:* Der Bestand ist hier durchgängig deutsch-hartkodiert —
weder `Form_QuellePufferspeicher` noch `Form_Quellprofil` noch die Übersichtsspalten
und Tooltips in `Form_Simulation_Config` nutzen `MyResource.Resource`. Die neuen
Dialoge werden von Anfang an gegen den Ressourcenkatalog gebaut, der in **Paket 9**
entsteht (13.6) — alle sichtbaren Texte über `MyResource.Resource.SIM_*`, keine
Literale. Das kostet beim Neubau nichts und erspart eine spätere Nacharbeit.

### 4.3 Pufferspeicher-Verwaltung (Projektebene)

Einstieg aus der Konfiguration und aus dem Projektbaum. Der Dialog nutzt
`Form_PufferSp_Admin` als Katalogbrowser und `PufferSpCtrl.CopyFromStamm` als
**explizite** Übernahme:

1. Katalogauswahl aus `Tab_Pufferspeicher_STAMM` (inkl. VDI-3805-Importe) **oder**
   freie Eingabe (Bezeichner, Volumen, Bereitschaftsverluste).
2. Pflichtfeld **Verwendung**: `Heizung` | `Brauchwasser`.
3. Betriebsparameter: Vorlauf/Rücklauf [°C] (Vorbelegung aus
   `Abfrage_Erzeuger_Vorlauftemperaturen`/`…Ruecklauftemperaturen`),
   Ein-/Abschaltschwelle [%] (Default 10/95) sowie **Abschaltschwelle für
   nachrangige Erzeuger** [%] (Default = Abschaltschwelle, also keine Reserve, 3.4).
4. Speichern → Zeile in `Tab_Pufferspeicher` mit den neuen Spalten (5.1).
   **Mehrfachanlage desselben Katalogtyps ist zulässig** (E7, 5.2).

Der Dialog zeigt zusätzlich die **Ladereihenfolge dieses Speichers** als Kontrolle:

```
┌─ Pufferspeicher „PS 800" (Heizung, 800 l) ──────────────────────┐
│  Vorlauf [ 55 ] °C   Rücklauf [ 35 ] °C   →  Q_max 18,6 kWh     │
│  Einschaltschwelle [ 10 ] %   Abschaltschwelle [ 95 ] %         │
│  Abschaltschwelle nachrangig [ 70 ] %  (Reserve für Vorrang)     │
│                                                                 │
│  Ladereihenfolge (aus den Erzeugerzuordnungen):                 │
│    1.  Vitosol 200-F   Solarthermie   Prio 10   bis 95 %        │
│    2.  Vitocal 300     Wärmepumpe     Prio 20   bis 70 %        │
│    3.  Energator G     BHKW           Prio 30   bis 70 %        │
│                                                                 │
│  Entladepriorität: [automatisch (10) ▾]                         │
│    ⓘ Wird als 1. von 2 Heizungsspeichern entladen               │
│                                     [Abbrechen] [OK]            │
└─────────────────────────────────────────────────────────────────┘
```

`Form_PufferSp_Bearbeiten` arbeitet heute ausschließlich gegen
`Tab_Pufferspeicher_STAMM` (`:45`) und liest positionsbasiert `row[2]…row[6]`
(`:53-57`) — der Projektmodus ist ein **Neubau**, kein Feldzusatz, und die
Ordinal-Lesung ist dabei auf Namenszugriff umzustellen.

### 4.4 Entfall der Zuordnungs-Rubrik

Die programmatisch erzeugte Rubrik „Pufferspeicher:" (`InitPufferspeicherRubrik`,
`:1049-1129`) mit `comboBox_Puffer1/2`, die Zuordnungstabelle `listView1` und
`Form_KonfigPufferspeicher` entfallen. Betroffen sind rund 350 Zeilen über zehn
Methoden (`_zuordnungen`, `AktivePufferSp`, `RefreshZuordnungAnzeige`,
`ZugeordnetePufferSp`, `SpeicherregelungBearbeiten`, `btn_Hinzu`, `btn_Loeschen`,
Teile von `btn_Speichern_Click`).

**Vorgehen in zwei Etappen** (Empfehlung aus der Prüfung, da es keine automatisierten
Tests gibt): Etappe A setzt die Rubrik nur auf `Visible = false` — wie es
`checkBox_PufferSp` (`:1054`) bereits vormacht — und schreibt `_zuordnungen` weiter
mit, während die neue Struktur parallel gepflegt wird. Etappe B entfernt den Code,
nachdem die Migration in Realprojekten bestätigt ist.

### 4.5 Quellendialog Erdreich (neu, E4)

```
┌─ Wärmequelle Erdreich — Vitocal 300 ───────────────────────────────┐
│  Quellsystem:  (•) Erdkollektor   Verlegetiefe [1,5] m             │
│                                   Fläche       [ 250] m²           │
│                ( ) Erdsonde       Länge je Sonde [ 90] m           │
│                                   Anzahl Sonden  [  2]             │
│  Bodentyp:     [Sand, feucht ▾]        (Katalog VDI 4640 Bl. 1)    │
│  Klimazone:    [ 6 — Nordwestdeutschl. ▾]  (DIN 4710, aus Region)  │
│                                                                    │
│  Vorschau: Jahresgang der Quelltemperatur (Chart)                  │
│  min 4,2 °C (Feb) · max 14,8 °C (Aug) · Mittel 9,6 °C              │
│                                                                    │
│  Auslegungsprüfung nach VDI 4640 Bl. 2 (nach der Simulation):      │
│   Entzugsleistung   6 480 W / 250 m² = 25,9 W/m²   Grenze 16 ⚠     │
│   Entzugsenergie          8 900 kWh/a =  35,6 kWh/m²  Grenze 31 ⚠  │
│   ⓘ Klimazone 6, Bodenart Sand: Kollektor ist zu klein bemessen.   │
│     Empfohlener Rohrabstand 0,3…0,4 m                              │
│                                               [Abbrechen] [OK]     │
└────────────────────────────────────────────────────────────────────┘
```

```
T_Boden(z, t) = T_m − A · e^(−z/d) · cos( 2π·(t − t_min)/8760 − z/d ),   d = √(2a/ω)

T_m    Jahresmittel der Außentemperatur
A      Amplitude — aus Regression eines Jahres-Sinus (MathNet.Numerics ist verfügbar)
       oder aus den 12 Monatsmitteln; NICHT aus Extrema der Stundenwerte
       (diese überschätzen die Amplitude erheblich)
z      Tiefe [m] (Kollektor: Verlegetiefe, Default 1,5 m)
a      Temperaturleitfaehigkeit des Untergrunds je Bodentyp (Formelzeichen nach
       VDI 4640 Bl. 1); Katalog mit Normkennwerten in 13.1
t_min  Phasenlage des Temperaturminimums
```

Für **Erdsonden** wird diese Formel nicht angewendet: Ab etwa 10 m Tiefe ist der
Jahresgang praktisch abgeklungen. Die Sonde erhält eine konstante Quelltemperatur
nach VDI 4640; Herleitung, Zahlenwerte und die Plausibilisierung des gesamten
Modells stehen in **Kapitel 13.1**.

**Datenlage verifiziert:** Der 8760er-Außentemperaturvektor liegt bereits vor —
`SimulationWaermebedarf.Stundentemperatur` (`:52`, gefüllt `:563-577` aus
`Tab_Solar.Temperatur WHERE ID_Klimaregion=…`) und wird über `SimulationControl.cs:112`
→ `simulation_wp.Temperatur` bis in `WaermequelleClass.Quelltemperatur(…, aussentemp)`
(`:244`) durchgereicht. Der Erdreich-Fall braucht damit **keinen eigenen DB-Zugriff**
und ist isoliert testbar. Für die Dialog-Vorschau wird der Vektor einmal beim Öffnen
geladen und gecacht (8760 Zeilen über `RecordSet` ≈ 0,3–1 s), nicht bei jeder
Parameteränderung. Chart nach Muster `Form_Quellprofil.cs:330-363`, aber mit
`SeriesChartType.FastLine`.

Entzugsleistung und Regeneration werden nicht modelliert (bewusste Vereinfachung,
im Ergebnis dokumentiert); wer Quellerschöpfung abbilden will, nutzt den Quellentyp
`Pufferspeicher` mit Regeneration.

Die **Auslegungsprüfung** im unteren Dialogbereich braucht Simulationsergebnisse
(maximale Entzugsleistung, Jahresentzugsarbeit, Volllaststunden) und bleibt deshalb
leer, solange kein Lauf vorliegt. Sie wird nach jedem Simulationslauf aktualisiert
und zusätzlich im Ergebnisbereich ausgewiesen — dort erreicht sie den Anwender auch
dann, wenn er den Quellendialog nicht mehr öffnet. Details zu den Grenzwerten
in 13.1.

### 4.6 Validierung beim Speichern (E3)

| Prüfung | Verhalten bei Verstoß |
|---|---|
| Hauptsenke gesetzt (jede Anlage) | Default `HEIZKREIS` wird gesetzt — erfüllt „Wärmeerzeuger soll immer eine Wärmesenke haben" |
| Senke `PUFFER_*` → Projekt-Puffer existiert, Verwendung passt | Speichern blockiert; Meldung mit Anlagen-/Puffername + Absprung „Pufferspeicher anlegen…" |
| Quelle `Pufferspeicher` → dito | dito |
| Zweitsenke ≠ Hauptsenke | Speichern blockiert |
| Puffer als Quelle **und** Senke derselben Anlage | Speichern blockiert (Kurzschluss) |
| Puffer wird geladen, aber sein Kanal hat keinen Bedarf | Warnung (kein Blocker) |

Beim Umbau mitzunehmen: `btn_Speichern_Click:1551-1552` nutzt `Int32.Parse` ohne
`TryParse` — leere Vorlaufwerte werfen eine unbehandelte `FormatException`.

---

## 5. Datenmodell

### 5.1 Erweiterung `Tab_Pufferspeicher` (Projektkopie)

| Spalte | Typ | Bedeutung |
|---|---|---|
| `Verwendung` | TEXT(50) | `Heizung` \| `Brauchwasser` (Pflicht bei Neuanlage; Migration 5.5) |
| `Vorlauf` | LONG | Bezugsvorlauf [°C] → Q_max |
| `Ruecklauf` | LONG | Bezugsrücklauf [°C] |
| `Schwelle_Ein` | DOUBLE | Einschaltschwelle Nachladung [%], Default 10 |
| `Schwelle_Aus` | DOUBLE | Abschaltschwelle [%], Default 95 |
| `Schwelle_Aus_Nachrang` | DOUBLE | Abschaltschwelle für **nachrangige** Erzeuger [%] (3.4). Default = `Schwelle_Aus` → keine Reservezone, verhaltensneutral |
| `Entladeprio` | LONG | Entladereihenfolge unter den Puffern desselben Kanals (3.6). 0 = automatisch aus der besten Ladepriorität dieses Speichers, 1–99 = manuell |

Die Betriebsparameter wandern damit von der Zuordnung an den **Speicher selbst** —
ein Puffer hat genau einen Betriebszustand, unabhängig davon, wie viele Anlagen ihn
laden. Zusätzlich zu ergänzen: `PufferSpCtrl.CopyFromStamm` (`:174-176`) hat eine
feste Spaltenliste und muss die neuen Felder mitführen.

### 5.2 Begründung des FK-Ziels (E7)

Gewählt: **`Tab_Pufferspeicher.ID`**.

| Kriterium | `Tab_Pufferspeicher.ID` (gewählt) | `Tab_Energieanlagen.ID` (verworfen) |
|---|---|---|
| ID-Stabilität | stabil: Vergabe `GetMaxID+1` (`PufferSpCtrl.cs:171`), kein aufgerufener Löschpfad | **instabil**: AutoWert, `DELETE + INSERT` bei jedem Speichern — typgefiltert über Kontextmenü/`Form_Start` (`WizardCtrl.cs:32-36`), projektweit über den Wizard (`WizardParent.cs:661-664`) |
| „im Projekt angelegt" ausdrückbar | ja, sobald das implizite `CopyFromStamm` entfällt (5.4) | ja, direkt |
| Mehrere baugleiche Puffer | erst nach Aufhebung der Dedup-Regel (siehe unten) | ja, ohne Änderung |
| Aufwand | gering | +3–5 PT: `Add_WP_Waermeerzeuger` müsste auf ID-erhaltendes UPSERT umgestellt werden — das betrifft **alle** Gewerke und den Wizard |

**Notwendige Begleitänderung:** `PufferSpCtrl.GetProjektId(Bezeichner, ID_Projekt)`
(`:132-139`) erlaubt heute nur **eine** Zeile je (Bezeichner, Projekt). Die
Dedup-Prüfung entfällt; die Auflösung läuft künftig über die ID, nicht über den
Bezeichner. `CopyFromStamm` legt bei jedem expliziten Aufruf eine neue Projektzeile an.

**Konsistenzregel:** Beim Anlegen eines Puffers über die Verwaltung (4.3) wird
zusätzlich die Anlagenzeile in `Tab_Energieanlagen` (`ID_Type = 12`) geschrieben,
damit der Projektbaum den Puffer weiterhin zeigt. Beim Entfernen wird beides
zusammen entfernt (behebt zugleich die fehlende Löschkaskade, Kapitel 8/B0-6).

### 5.3 Erweiterung `Tab_Energieanlagen`

Neue Spalten über `WaermequelleClass.SchemaSicherstellen()` (bestehender Mechanismus):

| Spalte | Typ | Bedeutung |
|---|---|---|
| `WS_Ziel` | TEXT(50) | Hauptsenke: `Heizkreis` \| `PufferHeizung` \| `PufferBrauchwasser` (Default `Heizkreis`) |
| `WS_ID_Puffer` | LONG | FK → `Tab_Pufferspeicher.ID`, wenn `WS_Ziel = Puffer*` |
| `WS_Ladeprio` | LONG | Ladepriorität der Hauptsenke (3.4). 0 = nach Vorgabe (Solar 10 / WP 20 / BHKW 30 / Kessel 40), 1–99 = manuell |
| `WS_Ladegrenze` | DOUBLE | Eigene Ladeobergrenze [%] der Hauptsenke; 0 = nicht gesetzt, dann gilt die Puffer-Regel |
| `WS_Ladeprio_PV` | LONG | Abweichende Ladepriorität in Stunden mit PV-Überschuss (3.5); 0 = keine Sonderregel |
| `WS_Ziel2` | TEXT(50) | Zweitsenke (leer = keine) |
| `WS_ID_Puffer2` | LONG | FK für die Zweitsenke |
| `WS_Ladeprio2` | LONG | Ladepriorität der Zweitsenke |
| `WS_Ladegrenze2` | DOUBLE | Ladeobergrenze [%] der Zweitsenke |
| `WQ_ID_Puffer` | LONG | FK für Quelle `Pufferspeicher` — **ersetzt** die Bezeichner-Referenz `WQ_Puffer` (bleibt als Altspalte lesbar) |
| `WQ_Tiefe` | DOUBLE | Erdreich: Verlegetiefe [m] bzw. Sondenlänge [m] |
| `WQ_Flaeche` | DOUBLE | Erdreich: Kollektorfläche [m²] — für die Auslegungsprüfung (13.1) |
| `WQ_Anzahl` | LONG | Erdreich: Anzahl Sonden — Eingangsgröße der Tabelle B2 |
| `WQ_Bodentyp` | TEXT(50) | Erdreich: Katalogschlüssel |
| `WQ_Quellsystem` | TEXT(50) | `Kollektor` \| `Sonde` |

`WaermequelleClass` erhält `TYP_ERDREICH = "Erdreich"`. **Wichtig:** `TypAnzeige`
(`:45-52`) und `TypWerte` (`:54-57`) sind indexgekoppelt
(`Form_Simulation_Config.cs:880-882`, `:898`) — neue Werte **anhängen**, nicht
einfügen. Zusätzlich sind die UI-Dispatcher `WaermequelleAnzeige()` (`:339`) und
`WqCombo_SelectedIndexChanged()` (`:894`) in `Form_Simulation_Config.cs` zu erweitern.

**Beziehungen anlegen:** Die drei neuen ID-Spalten erhalten eine erzwungene Beziehung
auf `Tab_Pufferspeicher.ID` mit Aktualisierungs- und Löschweitergabe — Vorbild ist
`Z_ProjektPufferSp.ID_Pufferspeicher`, das genau so definiert ist. Hintergrund: Die
vorhandene Spalte `ID_PUFFER` ist als **einzige** Komponentenreferenz in
`Tab_Energieanlagen` ohne Beziehung (13.7); genau deshalb kann dort unbemerkt eine
STAMM-ID stehen. Bei dieser Gelegenheit ist `ID_PUFFER` nachzurüsten — nach
Bereinigung der Altwerte — und die fehlende Beziehung
`Tab_Projekt.ID → Tab_Pufferspeicher.ID_Projekt` zu ergänzen (B0-6).

**Pflicht bei ID-Spalten:** `ProjektDuplizierenCtrl` versetzt ID-Spalten beim
Variantenanlegen um den Offset der Zieltabelle und entscheidet das über echte
Access-FKs **oder** die handgepflegte `FK_MAP` (`:71-79`). `WS_ID_Puffer`,
`WS_ID_Puffer2` und `WQ_ID_Puffer` müssen dort eingetragen werden — sonst zeigen die
Puffer-Referenzen einer Variante auf die Speicher des **Quellprojekts**. Das ist ein
stiller Datenfehler, der erst im Ergebnis auffällt. (Der bisherige `WQ_Puffer` als
TEXT hatte dieses Problem nicht — der Wechsel von Name auf ID ist nicht kostenlos.)

### 5.4 `Z_ProjektPufferSp` wird abgelöst

Die Zuordnung liegt künftig als FK an der Anlage (5.3), Verwendung und
Betriebsparameter am Puffer (5.1). `Z_ProjektPufferSp` wird nach der Migration nicht
mehr geschrieben; die Tabelle bleibt für Alt-Datenbanken lesbar. Das beseitigt vier
Altlasten: Text-Referenz über `Erzeuger`-Literale, `break` nach dem ersten Treffer,
`Schwelle_*` außerhalb des Models — und den Schwellen-Reset (B0-1).

Zu beachten: `WaermequelleClass.cs:270-279` nutzt `Z_ProjektPufferSp.Vorlauf/Ruecklauf`
als Altdaten-Fallback für die Quell-Puffertemperatur; dieser Pfad wird auf die neuen
Puffer-Spalten umgestellt.

### 5.5 Datenmigration (Schritt 5 der `SchemaMigration`, einmalig je Datenbank)

Die Migration läuft als **Schritt 5 der `SchemaMigration`** (ADR-001) genau einmal je
Datenbank über alle Projekte. Die Run-once-Garantie kommt vom
`SchemaVersion`-Marker — **nicht** aus einer Heuristik über den Datenbestand, die
Anwenderentscheidungen (z. B. ein bewusst zurückgestelltes `WS_Ziel='Heizkreis'`)
bei jedem Start überschreiben würde.

| Altbestand | Übernahme |
|---|---|
| `Z_ProjektPufferSp` mit `Erzeuger='Wärmepumpe'`, erster Eintrag nach `Prioritaet` | Projekt-Puffer erhält `Verwendung='Heizung'`, `Vorlauf`/`Ruecklauf`/`Schwelle_*` aus der Zuordnung; **alle** WP-Anlagen: `WS_Ziel='PufferHeizung'`, `WS_ID_Puffer` = Puffer-ID (entspricht dem heutigen Verhalten) |
| `Z_ProjektPufferSp` mit anderem `Erzeuger` | keine Übernahme (war wirkungslos); Protokollhinweis |
| `WS_Typ` vorhanden, kein Puffer | `WS_Ziel='Heizkreis'`, `WS_Typ` bleibt Bedarfsart |
| `WQ_Typ='Pufferspeicher'` mit `WQ_Puffer` (Bezeichner) | Projekt-Puffer gleichen Bezeichners → `WQ_ID_Puffer` setzen; sonst Hinweis „Quell-Puffer im Projekt anlegen" |
| `Tab_Pufferspeicher`-Zeilen ohne Anlagenzeile | Anlagenzeile (`ID_Type=12`) nachtragen, damit der Puffer im Projektbaum erscheint |
| Ladepriorität und Ladeobergrenzen (3.4) | `WS_Ladeprio*` = 0 (Vorgabe greift), `WS_Ladegrenze*` = 0 (nicht gesetzt), `Schwelle_Aus_Nachrang` = `Schwelle_Aus` → verhaltensneutral zum Bestand |
| PV-Sonderregel und Entladereihenfolge (3.5/3.6) | `WS_Ladeprio_PV` = 0 (keine Sonderregel), `Entladeprio` = 0 (automatisch) → Vorbelegung greift, keine Altdaten betroffen |
| Alle übrigen Anlagen ohne `WS_Ziel` | `WS_Ziel='Heizkreis'` |
| BHKW mit `Tab_Einstellungen.Pendelspeicher > 0` | Projekt-Puffer „BHKW-Pendelspeicher" (Verwendung `Heizung`) anlegen, Volumen aus dem Parameter, `WS_Ziel='PufferHeizung'` an der BHKW-Anlage |

### 5.6 Schema-Ausrollung — entschieden in ADR-001

**Die Ausrollung ist in [`ADR-001_Schema-Ausrollung.md`](ADR-001_Schema-Ausrollung.md)
festgelegt** (angenommen 14.08.2026): eine **versionierte In-Code-Migration**
(`Allgemein/Update/SchemaMigration.cs`) mit Schemamarker
`Tab_Applikation.SchemaVersion`, ausgeführt einmalig beim Programmstart in
`Program.Main`, mit Sammelfehlerbericht und Startblockade des Simulationsbereichs
bei Fehlschlag. Sie deckt alle drei Änderungsklassen ab: additives DDL (**24 neue
Spalten in vier Tabellen**, Kapitel 12), strukturelles DDL
(`Tab_ErgebnisPufferspeicher`, Index, fünf Beziehungen — 6.6 und 5.3) und die
einmalige Datenmigration (5.5).

Der frühere Weg über `UpdateDatabaseFromScript` und eine ausgelieferte Skriptdatei
ist **endgültig verworfen** — die Klasse wurde absichtlich gelöscht (Ordner
`Allgemein/Update/` neu belegt), und das externe DB-Migrationswerkzeug
(`DB_Migration`, dynamisch erzeugtes `migration*.sql`) ist grundsätzlich nicht Teil
der Anwendungsarchitektur.

`SchemaSicherstellen()` bleibt als Rückfallebene bestehen — bewährt, idempotent,
läuft automatisch bei Öffnen der Konfiguration und bei jedem Simulationsstart
(`Form_Simulation_Config.cs:1447`, `SimulationControl.cs:66`, `:221`) — und ruft
künftig **denselben Spaltenkatalog** wie die Migration auf (eine Quelle,
ADR-Aufgabe 4).

Zwei Einschränkungen des Mechanismus sind dabei zu beheben:
- Die private Überladung (`WaermequelleClass.cs:160-176`) hat `Tab_Energieanlagen`
  **hartkodiert** (`:167`); für `Tab_Pufferspeicher` ist die öffentliche Überladung
  (`:130-158`) zu nutzen, die je Spalte eine eigene `OleDbConnection` öffnet — besser
  eine dritte Überladung mit übergebener Connection ergänzen.
- **Fehler werden verschluckt** (`catch { Console.WriteLine(…) }`, `:119-122`,
  `:154-157`, `:172-175`). In einer `WinExe` ohne Konsole ist das spurloses Scheitern:
  Schlägt `ALTER TABLE` fehl (Datei schreibgeschützt, DB exklusiv geöffnet), liest
  `WertLesen` stumm `null` und der Anwender sieht Defaults statt seiner Eingaben. Bei
  **24 neuen Spalten** steigt das Risiko spürbar → **Rückgabewert `bool` + einmalige
  Sammelmeldung** — im Migrationskontext übernimmt das der Fehlerkanal der
  `SchemaMigration` (ADR-001).

---

## 6. Simulations-Engine

### 6.1 Transportstruktur statt Signaturflut

Leitidee: **kein neuer Datentyp in den Erzeugermodulen**, sondern eine
Transportklasse in `SimulationControl`.

```csharp
// Allgemein/Simulation/SimulationKanaele.cs (neu)
public class Waermekanaele
{
    public float[] Heiz = new float[8760];
    public float[] WW   = new float[8760];
    public float[] Summe();                    // für einkanalige Rechenwege
    public void    Uebernehmen(float[] restSumme, float[] vorherHeiz, float[] vorherWW);
    public Waermekanaele Clone();
}

public enum Senke { Heizkreis, PufferHeizung, PufferBrauchwasser }

public class Senkenzuordnung                   // je Tab_Energieanlagen.ID
{
    public int    AnlagenID;
    public Senke  Haupt = Senke.Heizkreis;
    public int    IDPufferHaupt;               // 0 = keiner
    public Senke? Zweit;
    public int    IDPufferZweit;
    public string WSTyp = "Beides";            // nur bei Haupt == Heizkreis
}
```

`Uebernehmen` verteilt eine einkanalig ermittelte Restsumme proportional zum
Kanalanteil der jeweiligen Stunde zurück — der Kompatibilitätsanker für Rechenwege,
die (noch) einkanalig arbeiten.

### 6.2 Speicher-Registry

`SimulationControl.cs:74-110` wird ersetzt:

```csharp
public Dictionary<int, SimulationPufferspeicher> speicher;   // je Tab_Pufferspeicher.ID
public SimulationPufferspeicher puffer_wp => ErsterHeizpuffer();   // Alias, siehe 6.7
```

Init je Projekt-Puffer aus `Tab_Pufferspeicher` (Volumen, Vorlauf, Rücklauf, Verluste,
Schwellen, Verwendung). `SimulationPufferspeicher` erhält die Felder `Verwendung` und
`LaedtGerade` — Letzteres ersetzt den heute **modulübergreifenden** `bool _speicherLaden`
in `SimulationWaermepumpe.cs:76`, der bei mehreren Speichern nicht mehr tragfähig ist.

**Anlagen-IDs:** `wp_list` enthält bereits Anlagen-IDs (`SimulationControl.cs:229`).
Bei Solarthermie liegt die Anlagen-ID über `ctrl.items[n].ID` vor
(`SimulationSolarthermie.cs:62-66`, `WErzeugerCtrl.cs:136`) — **`solarthermie_list`
ist toter Code und wird nie gelesen; die in Fassung 1 geforderte Umstellung entfällt.**
Umzustellen sind `bhkw_list` (enthält `ID_BHKW`, `SimulationControl.cs:311`) und
`spk_list` (enthält **Bezeichner-Strings**, `:341`, konsumiert in
`SimulationSPK.cs:106`). Beim Kessel ist zusätzlich zu beachten, dass `spk_list[i]`
zugleich der Modulname im Ergebnis ist (`SimulationRunner.cs:284`) — es braucht eine
parallele Namensliste, wie beim BHKW mit `bhkw_list_Namen` (`:312`) bereits vorhanden.

**Bestehende Quellspeicher-Liste** *(neu in Fassung 12)*: Quellseitig existiert
bereits `List<SimulationPufferspeicher> wp_quellspeicher` je WP-Modul
(`SimulationWaermepumpe.cs:49`, befüllt über `WaermequelleClass.Quellspeicher()`).
Die Registry muss diese Instanzen **übernehmen oder ablösen** — sonst entstehen zwei
parallele Speicherverwaltungen mit getrennter Bilanz.

### 6.3 Reihenfolge-Invariante der Kaskade

```
je Stunde h:
  A) Vorabentladung:  alle Speicher decken Bedarf in ihrem Kanal (Hysterese),
                      Puffer sortiert nach Entladepriorität (3.6)
  B) Bedarfsdeckung:  Kaskade tool[0..3], je Anlage nach Prioritaet
                      — nur Anlagen mit Hauptsenke HEIZKREIS
                      → SenkeAbziehen(WS_Typ, …)
  C) Speicherladung:  alle Anlagen mit Hauptsenke PUFFER_*,
                      sortiert nach Ladepriorität der Stunde (KASKADENÜBERGREIFEND,
                      3.4 / PV-Sonderfall 3.5)
                      → Speicher.Laden(…)   [KEIN SenkeAbziehen]
  D) Zweitsenken:     nur aus verbleibendem Ladepotenzial, ebenfalls nach Ladeprio
  E) Nachentladung:   Speicher decken den noch offenen Bedarf im eigenen Kanal,
                      wieder nach Entladepriorität
  F) Heizstab auf den dann verbleibenden Kanalrest
  G) StundeAbschliessen() je Speicher                        — genau einmal
```

**Der architektonisch wesentliche Punkt (neu in Fassung 3):** Die Ladephase C läuft
**außerhalb** der Bedarfskaskade. Die Ladepriorität kann der Kaskadenreihenfolge
widersprechen — Solarthermie in Slot 3 lädt vor einer Wärmepumpe in Slot 1 —, und das
ist genau der Zweck (3.4). Da jede Anlage **genau eine** Hauptsenke hat, ist sie
eindeutig entweder in Phase B oder in Phase C; nur die Zweitsenke (D) überlappt.
Damit bleibt die Zuordnung „eine Anlage, eine Wärmemenge, ein Ziel" erhalten und es
entsteht keine Doppelzählung.

Entladen wird **zweimal** je Stunde (A und E) — das entspricht der heutigen WP-Logik
(`SimulationWaermepumpe.cs:254-273` bzw. `:513-517`) und ist fachlich richtig: Vorab
deckt der Speicher, was er kann; nach der Produktion greift er noch einmal für den
Rest, den kein Erzeuger direkt gedeckt hat. Ohne die Nachentladung E entstünde bei
reinen Ladespeichersystemen eine künstliche Unterdeckung von einer Stunde.

`StundeAbschliessen()` läuft dagegen **genau einmal** je Speicher und Stunde, zentral
in Phase G. Heute ruft die WP es teils innerhalb der Modulschleife (`:410`, `:502`) —
bei mehreren Modulen an demselben Quellspeicher werden die Verluste dadurch mehrfach
gezählt.

**Die zentrale Falle: keine Doppelzählung.** Eine Anlage mit Puffer-Senke darf
`SenkeAbziehen` **nicht** mehr aufrufen (`SimulationWaermepumpe.cs:470`, `:489`) —
sonst wird dieselbe kWh einmal als Bedarfsdeckung und einmal als Speicherinhalt
gezählt. Der Deckungsgrad in `Form_Simulation_Detail.cs:1223-1226` würde das durch
`if (deckung > 100) deckung = 100` **kaschieren**; ein Testfall ohne diese Kappung
ist Pflicht.

Ebenfalls anzupassen (Prüfbefund, in Fassung 1 nicht adressiert):
- `SenkeAbziehen` bei der Speicherentladung (`:267`) ist hart auf `SENKE_BEIDES`
  gesetzt — der Kanal muss künftig aus `Pufferspeicher.Verwendung` folgen, sonst
  deckt ein Brauchwasserpuffer Heizbedarf.
- `verfuegbar` (`:401-412`) bekommt einen dritten Fall: bei Puffer-Senke ist es die
  **Ladefähigkeit** `Q_max · Obergrenze − SOC` mit der Obergrenze nach der
  Auflösungsregel aus 3.4 (eigene Ladegrenze → sonst `Schwelle_Aus` für die
  vorrangige, `Schwelle_Aus_Nachrang` für nachrangige Anlagen), kein Bedarf. Die
  Abbruchbedingung „kein Bedarf → Modul aus" wird zu „kein Bedarf **und** kein
  Ladepotenzial".
- **Betriebsart „Alternativbetrieb"** (`:346-356`) und die Bivalenzpunkt-Erfassung
  (`:520-523`) vergleichen gegen den aggregierten `Rest_waerme`; im Speicherbetrieb
  ist das nicht mehr die maßgebliche Bezugsgröße.
- **Quellspeicher-Bilanz** (`:378-394`, `:496-503`) rechnet über die Differenz eines
  modulübergreifenden Stundenakkumulators; die Speicherladung (`:560-576`) entnimmt
  der Quelle heute **keine** Wärme — die Quellbilanz ist bereits unvollständig und
  wird mit mehreren Puffern deutlicher falsch.
- **Betriebsmodus `PV`** (`:433-445`) verbraucht `pvRest` sequenziell über die
  Module; mit Zweitsenke muss dasselbe Budget auf zwei Senken aufgeteilt werden —
  Reihenfolge festlegen. Dieselbe Stelle liefert zugleich das Kriterium
  „PV-Überschuss in dieser Stunde" für die zeitabhängige Ladepriorität (3.5).

### 6.4 Solarthermie

Der Kappungspunkt ist eine Zeile (`SimulationSolarthermie.cs:167-169`); der Überschuss
ist bereits isoliert. Ein `Speicher.Laden(ueberschuss, i)` im Aufrufer (`:119-124`)
genügt — **fachlich der größte Hebel bei kleinstem Codeeinsatz.**

**Zwingende Mitkorrektur:** `SimulationRunner.cs:301` rechnet
`Restwaermebedarf = (Waermebedarf_gesamt − Waermeproduktion_gesamt)/1000`. Sobald Solar
zusätzlich einen Puffer lädt, wächst die Produktion über den Momentanbedarf hinaus —
der Restbedarf wird **negativ** und die Deckung überschreitet 100 % ohne Kappung.
Beides landet ungeprüft in `Tab_ErgebnisSolarthermie` und damit in Variantenbericht
und Wirtschaftlichkeit.

### 6.5 Heizkessel und BHKW (E8)

- **`SimulationSPK`**: `Heizkessel_Simulation` (`:223-284`) iteriert über einen
  Bedarfsvektor. Zweikanalig heißt: zweiter Schleifendurchlauf mit erhaltenem
  Zwischenzustand — **die Bereitschaftsverluste dürfen nur einmal je Stunde und
  Kessel anfallen** (`:275`), sonst verdoppeln sie sich und der Jahresnutzungsgrad
  (`:192`) wird falsch. Zusätzlich Senkenauswertung je Kessel (Puffer laden bis
  Abschaltschwelle).
- **`SimulationBHKW`**: drei parallele Fahrweisen-Implementierungen
  (`BhkwSimulationWaermegefuehrt`, `SimulationStromgefuehrt`, `SimulationOhneEinspeisung`,
  aufgerufen `:155-178`), jede mit eigener Speicherlogik. Der skalare
  `kapazitaetPendelspeicher` wird durch einen zugeordneten `SimulationPufferspeicher`
  ersetzt; die hartkodierten Schwellen 30/10/20 % (`:425`, `:440`, `:457`) werden zu
  Speicherparametern. **Dabei ist der Bilanzfehler in `SimulationControl.cs:327`
  mitzubeheben** (der BHKW-eigene `waermerestbedarf` aus `SimulationBHKW.cs:502` wird
  heute verworfen). Das verändert **sämtliche BHKW-Ergebnisse** — der Grund, warum
  dieses Paket eine eigene Verifikation gegen Referenzprojekte braucht.

### 6.6 Ergebnis-Persistenz

`Tab_ErgebnisWaermepumpe.Kapazitaet_Pufferspeicher` wird künftig aus
`SimulationPufferspeicher.Q_max` gefüllt statt aus dem Legacy-Ausdruck
`Volumen · 1,16` (`SimulationRunner.cs:138`) — der heute schon der Anzeige in
`Form_Simulation_Detail.cs:1242` widerspricht.

Neu: **`Tab_ErgebnisPufferspeicher`** je Simulationslauf und Puffer —
`ID, ID_Ergebnis, ID_Pufferspeicher, Bezeichner, Verwendung, Q_max, Ladung_gesamt,
Entladung_gesamt, Verluste_gesamt, SOC_Ende, SOC_Mittel, SOC_Max, Vollzyklen`.

**Korrektur gegenüber Fassung 1, präzisiert in Fassung 12:** Das Muster
`StelleEnergieSpaltenSicher()` / `StelleBHKWSpaltenSicher()` /
`StelleModulSpaltenSicher()` (`ErgebnisCtrl.cs:690, :716, :745` — alle drei laufen
bei **jedem** `Save` mit leerem `catch`, kein `_schemaGeprueft`-Flag) kann
**ausschließlich `ALTER TABLE … ADD COLUMN`**. Ein `CREATE TABLE` existiert im
Repository dagegen durchaus — an neun Stellen in vier Dateien, u. a. über den
`Ddl()`-Helfer in `WirtschaftlichkeitCtrl.cs:72-190`, in `BerichtCtrl.cs:150` und
`VariantenCtrl.cs:245`; nur `ErgebnisCtrl` selbst hat keins. **Dieses erprobte
In-Code-Muster ist das Vorbild für `StellePufferTabelleSicher()`.**
Entscheidend ist außerdem: `ErgebnisCtrl.Save` (`:66`) räumt den Vorgängerlauf per
`DELETE ID_Projekt FROM Tab_Ergebnis WHERE ID_Projekt = ?` ab (`:85`, Access-Syntax)
und verlässt sich laut Kommentar
(`:83-84`) auf die **Löschweitergabe im Access-Schema**. Diese existiert tatsächlich —
alle sechs Detailtabellen hängen mit `DEL-CASCADE` an `Tab_Ergebnis.ID`, die
Modultabellen wiederum an ihren Kopftabellen (an der Datenbank verifiziert, 13.7).
Eine zur Laufzeit ohne Constraint erzeugte Tabelle hätte diese Beziehung jedoch
**nicht** — und da IDs über `MAX(ID)+1` **wiederverwendet** werden, würden Waisenzeilen
später auf fremde Läufe zeigen und stillschweigend falsche Speicherbilanzen in Bericht
und Wirtschaftlichkeit erzeugen. Die neue Tabelle bekommt deshalb dieselbe Beziehung
wie ihre Geschwister.

Angelegt wird sie in **Schritt 3 der `SchemaMigration`** (ADR-001):

```
CREATE TABLE Tab_ErgebnisPufferspeicher (ID LONG NOT NULL PRIMARY KEY,
    ID_Ergebnis LONG, ID_Pufferspeicher LONG, Bezeichner TEXT(255),
    Verwendung TEXT(50), Q_max DOUBLE, Ladung_gesamt DOUBLE, Entladung_gesamt DOUBLE,
    Verluste_gesamt DOUBLE, SOC_Ende DOUBLE, SOC_Mittel DOUBLE, SOC_Max DOUBLE,
    Vollzyklen DOUBLE)
CREATE INDEX idx_ErgPuffer ON Tab_ErgebnisPufferspeicher (ID_Ergebnis)
ALTER TABLE Tab_ErgebnisPufferspeicher ADD CONSTRAINT FK_ErgPuffer
    FOREIGN KEY (ID_Ergebnis) REFERENCES Tab_Ergebnis (ID) ON DELETE CASCADE
```

Zusätzlich in `ErgebnisCtrl` eine `StellePufferTabelleSicher()` als Fallback für
Datenbanken, deren Migration noch nicht gelaufen ist (nach dem `Ddl()`-Vorbild,
s. o.) — mit `CREATE TABLE`, FK-Constraint
**und** einem defensiven expliziten Delete in `Save`, falls die Constraint fehlt.
`Vollzyklen = Ladung_gesamt / Q_max` mit Division-durch-Null-Absicherung;
`SOC_Mittel`/`SOC_Max` erfordern eine Auswertung von `SOC_stuendlich`, die es noch
nicht gibt.

### 6.7 Kompatibilität der Anzeigen

`puffer_wp` bleibt als Alias auf den ersten Heizungs-Puffer erhalten, damit
`NavigatorWaerme.cs:116-118, 146, 159`, `Form_Simulation_Detail.cs:292-298` (CSV-Export)
und `:1241-1244` unverändert funktionieren. **Aber:** Sobald ein Projekt zwei Puffer
hat, zeigen diese Stellen stillschweigend nur einen — das ist in der UI kenntlich zu
machen („Puffer 1 von 2") und im Paket 7 aufzulösen.

### 6.8 Anschluss TWW-Konzept

Der WW-Kanal ist der saubere Andockpunkt für
`Konzept_TWW-Zapfprofile_WP-Plan_1.md`: Die dortige Zapfprofil-Engine ersetzt nur die
**Befüllung** von `Rest_WW` (`zapfwerte[8760]` + getrennter Zirkulationskanal) über
die dort beschriebene Weiche mit Default „alt". An Kaskade, Senken und Speichern
ändert sich dadurch nichts.

---

## 7. Technische Struktur

| Datei | Art | Inhalt |
|---|---|---|
| `Allgemein/Simulation/SimulationKanaele.cs` | neu | `Waermekanaele`, `Senke`, `Senkenzuordnung` (6.1) |
| `Allgemein/Simulation/ErdreichTemperatur.cs` | neu | Kusuda-Berechnung, Bodentyp-Katalog (α), Ableitung T_m/A/t_min per Regression |
| `Allgemein/Simulation/WaermequelleClass.cs` | ändern | `TYP_ERDREICH`, neue Spalten in `SchemaSicherstellen` (+ Fehlerrückgabe), Erdreich-Fall in `Quelltemperatur()`, `WQ_ID_Puffer` statt Bezeichner in `Quellspeicher()` (Projekt- statt Stammtabelle), Altdaten-Fallback `:270-279` umstellen |
| `Allgemein/Simulation/SimulationControl.cs` | ändern | Speicher-Registry (6.2), zweikanalige Kaskade und aus der Kaskade gelöste Ladephase (6.3), Ladeprioritäts-Auflösung (3.4), Anlagen-IDs für Kessel/BHKW, Bilanzfehler `:327` |
| `Allgemein/Simulation/SimulationPufferspeicher.cs` | ändern | Felder `Verwendung`, `LaedtGerade`; SOC-Kennzahlen |
| `Allgemein/Simulation/SimulationWaermepumpe.cs` | ändern | Senke je Modul, Kanäle von außen, Hysterese am Speicherobjekt, Zweitsenke, Reihenfolge-Invariante |
| `Allgemein/Simulation/SimulationSolarthermie.cs` | ändern | Überschuss → Puffer (6.4) |
| `Allgemein/Simulation/SimulationSPK.cs` | ändern | Zweikanaligkeit, Senkenauswertung, Bereitschaftsverluste einmal je Stunde |
| `Allgemein/Simulation/SimulationBHKW.cs` | ändern | Pendelspeicher → `SimulationPufferspeicher` in allen drei Fahrweisen |
| `Allgemein/Simulation/SimulationWaermebedarf.cs` | ändern | Heiz- und WW-Kanal getrennt bereitstellen (Summenfelder bleiben) |
| `Allgemein/Simulation/SimulationRunner.cs` | ändern | `Kapazitaet_Pufferspeicher` aus `Q_max`, Solar-Restbedarf (6.4), Puffer-Ergebnisse |
| `Controller/ErgebnisCtrl.cs` | ändern | `TAB_PUFFER`, `StellePufferTabelleSicher()`, Save/Load-Block |
| `Model/ErgebnisModel.cs` | ändern | `List<ErgebnisPufferspeicherModel>` |
| `Views/Simulation/Form_Simulation_Config.cs` | ändern | Spaltenkonstanten, Übersicht für alle Erzeuger, Validierung 4.6, Rubrik-Entfall, `TryParse` |
| `Views/Simulation/Form_Simulation_Config.Uebersicht.cs` | neu | ausgelagerter Layout-/Übersichtscode (`partial class`) |
| `Views/Simulation/Form_Waermesenke.cs` | neu | Senkendialog 4.2 |
| `Views/Simulation/Form_QuelleErdreich.cs` | neu | Erdreichdialog 4.5 mit Vorschau-Chart |
| `Views/Simulation/Form_QuellePufferspeicher.cs` | ändern | Liste aus `Tab_Pufferspeicher` (Projekt, Verwendungsfilter), FK statt Bezeichner |
| `Views/Pufferspeicher/Form_PufferSp_Bearbeiten.cs` | ändern | **Projektmodus ist Neubau** (heute nur STAMM, `:45`), Ordinal-Lesung `:53-57` auf Namenszugriff |
| `Controller/PufferSpCtrl.cs` | ändern | neue Spalten, `ReadAllProjekt(idProjekt, verwendung)`, Dedup-Regel aufheben (5.2), `CopyFromStamm`-Spaltenliste |
| `Controller/ProjektDuplizierenCtrl.cs` | ändern | `FK_MAP` um `WS_ID_Puffer`, `WS_ID_Puffer2`, `WQ_ID_Puffer` (5.3) |
| `Allgemein/Update/SchemaMigration.cs` | neu | Versionierte Migration nach ADR-001: Schrittregister, `SchemaVersion`-Marker, Sammelfehlerbericht; enthält die Datenmigration 5.5 als Schritt 5 |
| `Controller/ApplikationCtrl.cs` | ändern | `SchemaVersion` in `Tab_Applikation` lesen/schreiben |
| `Program.cs` | ändern | Startsequenz: `SchemaMigration.Ausfuehren()` vor dem MDI-Start, Startblockade des Simulationsbereichs bei Fehlschlag |

**Projekteinbindung:** Die `.csproj` ist SDK-Style ohne
`EnableDefaultCompileItems=false` — neue `.cs`-Dateien werden **automatisch**
kompiliert. Namespace zwingend `WindowsFormsApplication1` (flach, unabhängig vom
Ordner). Build nur x86. Neue Dateien **UTF-8 mit BOM** speichern — im Bestand gibt es
Dateien mit kaputten Umlauten (z. B. `Form_PufferSp_Bearbeiten.cs:44`).

---

## 8. Vorab-Paket B0: Bestandsfehler (E9)

Diese Fehler existieren unabhängig vom Umbau, wirken sich teils **heute schon** auf
Ergebnisse aus und werden vorab einzeln ausgeliefert — jeder mit dokumentierter
Ergebnisänderung. Nur so ist später zuordenbar, ob eine Abweichung vom Umbau oder von
einem Bugfix stammt.

| # | Fehler | Fundstelle | Wirkung |
|---|---|---|---|
| B0-1 | **Speicherregelung wird bei jedem Speichern zurückgesetzt.** `btn_Speichern_Click` löscht alle Zuordnungen (`:1531`) und legt sie neu an; `Z_ProjektPufferSpCtrl.Insert()` (`:63-68`) schreibt `Schwelle_Ein`/`Schwelle_Aus` **nicht** mit | `Form_Simulation_Config.cs:1531`, `Z_ProjektPufferSpCtrl.cs:63-68` | Eingestellte Hysterese fällt still auf 10/95 % zurück |
| B0-2 | **Array-Aliasing im Heizkessel.** Bei null Kesseln bindet `Restwaerme = Waermebedarf` dasselbe Objekt; `Init()` löscht es beim nächsten Lauf | `SimulationSPK.cs:101`, `:353`; `SimulationControl.cs:124` | Zweiter Lauf in derselben Sitzung kann den Projekt-Wärmebedarf auf 0 setzen |
| B0-3 | **Kesselsuche ohne Projektfilter.** `SELECT … WHERE Bezeichner='…'` ohne `ID_Projekt` | `SimulationSPK.cs:106` | Bei gleichem Kesselnamen in mehreren Projekten falsche Leistung/Brennstoff/Emissionen |
| B0-4 | **Spaltenname `Rücklauf` vs. `Ruecklauf`.** Die Spalte heißt `Rücklauf` **mit** Umlaut (an der DB verifiziert, 13.7) — `ReadAllFilter` und `WizardCtrl` sind korrekt, `WErzeugerCtrl.Insert`/`Update`/`ReadSingle` nicht | `WErzeugerCtrl.cs:24`, `:81`, `:191` | **Keine Ergebniswirkung** — die drei Methoden werden für Energieanlagen nicht aufgerufen. Aufräumarbeit, um die stille Fehlerquelle zu beseitigen |
| B0-5 | **Heizstab-Ganglinie überschreibt statt zu addieren** (`=` statt `+=`), während `Heizstab_gesamt` korrekt addiert | `SimulationWaermepumpe.cs:532`, `:539` | Bei ≥2 WP-Modulen inkonsistente Ganglinie; geht über `SimulationControl.cs:143-144` in den Strombedarf ein |
| B0-6 | **Keine Löschkaskade beim Puffer**, zwei Ursachen: (a) Löschen im Projektbaum entfernt nur die Anlagenzeile; (b) `Tab_Pufferspeicher` hängt als einzige Projektkopie **nicht** an der Löschweitergabe von `Tab_Projekt` (13.7) | `PufferSpKontextMenuCtrl.cs:82-100`, `WizardCtrl.cs:38-42`; Schema | Gelöschter Puffer rechnet weiter; beim Projektlöschen bleiben Waisen. Fix: Aufräumen im Code **und** Beziehung `Tab_Projekt.ID → Tab_Pufferspeicher.ID_Projekt` nachtragen |
| B0-7 | **Restbedarfsformel und Deckungsgrad widersprechen der Ganglinie** *(erweitert in Fassung 12)*. (a) `Waermebedarf − Produktion − Heizstab` statt der Summe aus `waermerestbedarf_gesamt` — dabei wird mit `Stromverbrauch_Heizstab` eine **Strommenge** von einer Wärmemenge abgezogen; Speicherverluste fehlen in der Bilanz. (b) Der **Deckungsgrad** wird doppelt und widersprüchlich gerechnet: `SimulationRunner.cs:148-153` produktionsbasiert (landet in `Tab_Ergebnis` → Bericht/Wirtschaftlichkeit), `Form_Simulation_Detail.cs:1223-1227` restbedarfsbasiert — genau die Formel, die der dortige Kommentar als „mit Pufferspeicher ungenau" verwirft; beide kappen bei 100 % | `SimulationRunner.cs:137, :148-153` gegen `SimulationWaermepumpe.cs:603-604` und `Form_Simulation_Detail.cs:1223-1227` | Gespeichertes Ergebnis und Anzeige widersprechen sich; mit mehreren Speichern wird die Abweichung auffällig |
| B0-8 | **`Form_PufferSp` löscht aus den Stammdaten.** Der Löschbutton im Projektdialog ruft `PufferSpStammCtrl.Delete` | `Form_PufferSp.cs:229` (`pufferspctrl` ist `PufferSpStammCtrl`, `:12`) | Katalogdatensatz verschwindet global |
| B0-9 | **Lokalisierter Anzeigename direkt im SQL.** `Abfrage_Erzeuger_Vorlauftemperaturen where Typ='<Anzeigename>'` | `Form_KonfigPufferspeicher.cs:43`, `:50` | In englischer Oberfläche findet die Abfrage nichts → Vor-/Rücklauf bleiben leer. Zusätzlich SQL-Injection-anfällig (13.6) |
| B0-10 | **Anzeigetext als Filter-Steuerwert.** Volumenfilter vergleicht `comboBox_Volumen.Text` gegen deutsche Literale; ohne Treffer bleibt der Volumenteil des Prädikats leer | `Form_PufferSp.cs:184-189`, `Form_PufferSp_Admin.cs:60-65` | Der erste Filterteil ist nie leer — ohne Volumen-Treffer entsteht `… where <filter> and  order by …` (`Form_PufferSp.cs:198`, `Form_PufferSp_Admin.cs:74`) → **SQL-Syntaxfehler zur Laufzeit** (13.6) |
| B0-11 | **Anzeigename landet als DB-Steuerwert** *(neu in Fassung 12)*. Beim Speichern der Zuordnungen sucht das Rückwärts-Mapping den lokalisierten Anzeigetext in der LanguageItem-Liste; ohne Treffer wird der **Anzeigename** als `Erzeuger` in `Z_ProjektPufferSp` geschrieben (`match?.DbValue ?? z[0]`) — die Leseseite sucht hart nach `Erzeuger='Wärmepumpe'` | `Form_Simulation_Config.cs:1548-1549` | Sprachwechsel zwischen Anlegen und Lesen macht die Zuordnung stillschweigend wirkungslos; gleiche Familie wie B0-9/B0-10, wird mit 5.4 obsolet, wirkt aber heute (13.6) |

Weitere Befunde ohne unmittelbare Ergebniswirkung, die bei Berührung mitzunehmen sind
*(erweitert in Fassung 12)*:

- `PufferSpCtrl.Delete()`/`Update()` sind funktionsunfähig (keine `Connection`, `:27`).
- `WizardParent.LoadWEFromDB` liest `ID_PUFFER` nicht (`:511-560`) — nur dadurch geht
  die Puffer-Referenz beim Wizard-Speichern verloren; der Kontextmenü-Pfad
  (`PufferSpKontextMenuCtrl.cs:117`) übernimmt sie.
- `WizardItemClass.PUFFER_ITEM = 13` ist out-of-range gegenüber `MenueCtrl` (`:29-41`),
  der Wizard-Zweig in `Form_PufferSp` ist unerreichbar.
- Das stille ID-Reparaturmuster aus 2.3 (`if (idX > 0)`) verschluckt
  `CopyFromStamm`-Fehler in **allen** Gewerken von `Add_WP_Waermeerzeuger`
  (WP `:144-145`, BHKW `:149-150`, Kessel `:154-155`, Stromspeicher `:159-160`,
  PV `:169-170`, Solar `:174-175`), nicht nur beim Puffer.
- **`KlimaregionCtrl`:** `Add()`, `Update()` und `Delete()` schreiben auf
  Spaltennamen (`Name`, `Längengrad`, `Breitengrad`, `Beschreibung`), die die
  produktiv genutzte Leseseite (`Bezeichner`, `Longitude`, `Latitude`, `Details`)
  nicht kennt — und widersprechen sich untereinander; `Update()`/`Delete()` laufen
  zur Laufzeit zwangsläufig auf OleDb-Fehler. **Vor der Erweiterung von
  `Tab_Klimaregion` in Paket 3 (13.1) zu reparieren.**
- `SimulationSPK.cs:140/:210` übergibt `Max_Waermebedarf` by-value — das Feld
  (`:27`) bleibt immer 0 (anders als `ref double GasSpitze`).
- `SimulationSPK.cs:163` weist `Stromverbrauch_stuendlich` die **Referenz** der
  Kesselganglinie zu (Aliasing wie B0-2) — bei Brennstoffart 13 zeigen Strom- und
  Wärmeganglinie auf dieselbe Instanz.
- `SimulationControl.cs:211` überschreibt `Reststrom` am Ende — die Additionen
  `:114`/`:138-139` sind wirkungslos (toter Code).
- `SimulationWaermepumpe.cs:296-299`: Deckt der Puffer die Stunde vollständig,
  verlässt `break` die Modulschleife — Quellspeicher erhalten in diesen Stunden
  weder Regeneration noch `StundeAbschliessen`, ihre SOC-Ganglinie bleibt 0
  (verschärft die Quellbilanz-Punkte in 6.3).

---

## 9. Umsetzungspakete und Aufwand

Die Schätzung aus Fassung 1 (13–17 PT) war um Faktor ~2,5 zu niedrig; mit den
Entscheidungen der Fassungen 4 und 5 ist der Umfang zusätzlich gewachsen. Ursachen,
nach Gewicht: die **durchgängige Lokalisierung** als eigenes Paket (E12, 17,5 PT),
der Umbau der **WP-Kernschleife** (~640 Zeilen mit voller Bilanzwirkung), die
Einbeziehung von **Kessel und BHKW** in dieselbe Stufe (E8), der **Layoutzwang** in
der Konfiguration (4.1) und die fehlende **Regressionsinfrastruktur** (keine
automatisierten Tests, keine Referenzläufe — Solution und Versionskontrolle sind
dagegen vorhanden, siehe 13.7).

| # | Paket | Inhalt | Aufwand |
|---|---|---|---|
| **B0** | Bestandsfehler | Kapitel 8 (inkl. B0-11 und erweitertem B0-7), einzeln ausgeliefert | **1,5–2,5 PT** |
| **B1** | Referenzlauf-Suite | 5–8 Realprojekte, Ergebnisvektoren als CSV eingefroren, automatisierter Toleranzvergleich. **Ohne dieses Paket ist keine der folgenden Änderungen verifizierbar** | **2 PT** |
| **1** | Schema + Migration | **`SchemaMigration`-Mechanismus mit `SchemaVersion`-Marker und Startsequenz (ADR-001)**, 5.1/5.3, Spaltenkatalog aus `SchemaSicherstellen` herauslösen und härten (5.6), Datenmigration 5.5, `FK_MAP`, Dedup-Aufhebung, Ordinal-Leser absichern | **5,5–8 PT** |
| **2** | Konfigurations-UI | Spaltenkonstanten, Layout-Neuverteilung, Übersicht für alle Erzeuger, `Form_Waermesenke`, Projekt-Puffer-Verwaltung, Rubrik-Entfall (Etappe A), Validierung | **6–9 PT** |
| **3** | Erdreichmodell | `ErdreichTemperatur`, Dialog, Vorschau-Chart, Bodentyp-Katalog nach VDI 4640 Bl. 1 Tab. 1, Sondenformel, Plausibilitätsprüfung mit den Auslegungstabellen A2/B2 aus Bl. 2 inkl. Klimazonen-Zuordnung (13.1); **Vorarbeit: `KlimaregionCtrl`-Reparatur (Kapitel 8)** | **4,5–5,5 PT** |
| **4** | Engine-Kern | Registry, `Waermekanaele`, zweikanalige Kaskade, aus der Kaskade gelöste Ladephase mit Prioritätsauflösung (3.4), WP-Umstellung, Reihenfolge-Invariante | **9–12 PT** |
| **5** | Solarthermie + Heizkessel | Überschuss → Puffer inkl. Fix `SimulationRunner.cs:301-304`; Kessel zweikanalig + Senken | **4–6 PT** |
| **6** | BHKW | Pendelspeicher-Ablösung in drei Fahrweisen, Bilanzfehler `SimulationControl.cs:327` | **6–9 PT** |
| **7** | Ergebnis + Anzeigen | `Tab_ErgebnisPufferspeicher` (6.6), Navigator/Detail/CSV auf n Puffer nach 13.3 | **3–4 PT** |
| **8** | Engine-Protokoll | Protokoll-/Fehlerkanal statt MessageBox (13.4, acht Stellen), Einstellung `Extrapolation_erlaubt`, Anbindung an `SimuliereUndSpeichere(… out fehler)`; **DataRepository-Fehlerpfad ohne MessageBox im Engine-Kontext** | **2–2,5 PT** |
| **9** | Lokalisierung | Simulationsbereich durchgängig zweisprachig, Teilpakete L0–L8 nach 13.6, inkl. Behebung B0-9/B0-10 | **17,5 PT** |
| **10** | Abnahme | Realprojekt-Tests (nur Heizkreis / WP+WW-Puffer / Solar-Zweitsenke / BHKW / zwei Puffer je Kanal / WP im PV-Modus mit Solar und Zweitsenke / Migration Altprojekte / Projekt ohne Puffer), Energieerhaltungstest, Sprachvergleich DE/EN, Rubrik-Entfall Etappe B | **4–6 PT** |
| | **Summe** *(Fassung 12: +3–3,5 PT für ADR-001-Mechanismus und neue Befunde)* | | **64–81,5 PT** |

**Feature-Flag empfohlen** (~0,5 PT, in Paket 4 enthalten): eine Projekteinstellung
`Kaskade_Zweikanalig`, Default aus. Altprojekte rechnen weiter auf dem alten Pfad,
die Umstellung erfolgt projektweise. Das ist die einzige belastbare Rückfallebene.

**Empfohlene Reihenfolge**

```
B0  Bestandsfehler          ← zuerst, einzeln, jeder mit dokumentierter Wirkung
B1  Referenzlauf-Suite      ← ohne sie ist nichts danach verifizierbar
 3  Erdreichmodell          ← additiv, risikoarm, früh sichtbarer Nutzen
 1  Schema + Migration
 7  Ergebnis + Anzeigen     ← liefert die Messgrößen für alles Folgende
 2  Konfigurations-UI
 4  Engine-Kern
 5  Solarthermie + Kessel
 6  BHKW
 8  Engine-Protokoll        ← nach dem Engine-Umbau, sammelt dessen Meldungen
 9  Lokalisierung           ← zuletzt, wenn alle Texte final sind
10  Abnahme
```

Paket 9 bewusst am Ende: Jede vorgezogene Lokalisierung müsste bei jeder späteren
UI-Änderung nachgeführt werden. Die einzige Ausnahme sind die beiden Bestandsfehler
B0-9 und B0-10 — sie gehören inhaltlich zur Lokalisierung, wirken aber schon heute
und werden deshalb in B0 vorgezogen.

### Zur Erwartung an „unveränderte Ergebnisse"

Bei Default `WS_Ziel = Heizkreis` und ohne konfigurierten Puffer ist die Kanaltrennung
**mathematisch verlustfrei umkehrbar** (3.2). Bitgenaue Gleichheit ist dennoch nicht
zusagbar: Sobald der Heizkanal als eigener `float[8760]` geführt statt als `double`
aus `Rest_waerme − rest_ww` berechnet wird (`SimulationWaermepumpe.cs:244`), ändern
sich Rundungen in der letzten Stelle; über 8760 Stunden akkumuliert das auf 1e-4…1e-3
relativ. **Realistische Zusage: auf zwei Nachkommastellen identisch** — sofern die
Bugfixes aus B0 separat ausgeliefert sind.

Zwangsläufig ändern sich außerdem: die WW-Deckung in allen Projekten, in denen die WP
**nicht** an erster Kaskadenposition steht (heute überschätzt, weil
`Warmwasserbedarf_stuendlich` den vollen statt den Rest-WW-Bedarf erhält); die
Heizstab-Ganglinie bei ≥2 Modulen (B0-5); Kesselergebnisse bei Bezeichnerkollision
(B0-3); `Kapazitaet_Pufferspeicher`; sämtliche BHKW-Ergebnisse (Paket 6).

---

## 10. Risiken und offene Punkte

| # | Punkt | Status/Maßnahme |
|---|---|---|
| ~~O1~~ | ~~Ladevorrang bei mehreren Ladern an einem Puffer~~ | **entschieden** (E10, Kapitel 3.4): Ladepriorität je Anlage und Senke, Vorgabe Solar → WP → BHKW → Kessel, manuell übersteuerbar; Ladeobergrenzen in zwei Stufen, per Default verhaltensneutral |
| ~~O1a~~ | ~~WP im PV-Modus gegenüber Solarthermie~~ | **entschieden** (E11a, Kapitel 3.5): bleibt nachrangig, über `WS_Ladeprio_PV` je Anlage umkehrbar. Im Abnahmetest mit einem Realprojekt gegenrechnen |
| ~~O1b~~ | ~~Entladereihenfolge bei mehreren Puffern je Kanal~~ | **entschieden** (E11b, Kapitel 3.6): gleiche Richtung wie die Ladepriorität, je Speicher über `Entladeprio` übersteuerbar. Die widersprüchliche Formulierung aus Fassung 3 ist korrigiert |
| ~~O2~~ | ~~Netzverluste-Kanalzuordnung~~ | **entschieden** (3.2): vollständig auf den Heizkanal — einzige altverhaltenserhaltende Variante |
| ~~O3~~ | ~~Zweitsenke × Betriebsmodus `PV`~~ | **entschieden** (13.5): sequenziell, Hauptsenke bis zu ihrer Ladeobergrenze zuerst, erst der Rest an die Zweitsenke. Testfall in Paket 10 (Abnahme) |
| ~~O4~~ | ~~`MessageBox`-Aufrufe blockieren Headless-Läufe~~ | **entschieden** (13.4): Protokoll-/Fehlerkanal nach dem Muster `SimuliereUndSpeichere(… out fehler)` aus `Konzept_Variantenbericht.md` (E10 und Kap. 3.4 dort); Extrapolationsrückfrage wird zur Vorab-Einstellung. Eigenes Paket 8 |
| ~~O5~~ | ~~Erdreichmodell ohne Validierungsreferenz~~ | **erledigt** (13.1): gegen VDI 4640 plausibilisiert, Bodentyp-Katalog mit belegten Kennwerten, Phasenverschiebung exakt reproduziert, Entzugsleistung als Warnung. Normtext-Verifikation vor Auslieferung bleibt offen |
| ~~O6~~ | ~~Abweichender DB-Pfad in `UpdateDatabaseFromScript`~~ | **gegenstandslos** (ADR-001, angenommen 14.08.2026): Die Klasse ist endgültig verworfen; die `SchemaMigration` läuft ausschließlich über `DataRepository` (13.2) |
| ~~O7~~ | ~~Anzeigen zeigen bei mehreren Puffern nur einen~~ | **entschieden** (13.3): Chart-Serie, CSV-Spalten und Detailanzeige je Puffer; Übergangshinweis „Speicher 1 von n" bis zur Umstellung in Paket 7 |
| ~~O8~~ | ~~Reales Access-Schema unbestätigt~~ | **erledigt** (13.7): alle fünf Punkte an `Kenndaten.accdb` verifiziert. `Rücklauf` mit Umlaut (B0-4 ohne Ergebniswirkung), `Tab_Einstellungen`-Reihenfolge bestätigt, Löschweitergaben **vorhanden**, beide Erzeuger-Abfragen ausgelesen, keine Spaltenkollision. Zwei neue Befunde: `ID_PUFFER` ohne Beziehung, `Tab_Pufferspeicher` ohne Projekt-Kaskade — beide in B0-6 bzw. 5.3 aufgenommen |
| ~~O9~~ | ~~Lokalisierung faktisch aufgegeben~~ | **entschieden** (13.6): Simulationsbereich wird durchgängig zweisprachig, eigenes Paket 9 (17,5 PT). 287 Codestellen, 156 eindeutige Texte; DB-Werte bleiben deutsch (Drei-Schichten-Regel) |
| O10 | Keine automatisierten Tests; jede Verifikation ist ein manueller Lauf unter Windows/x86 mit ACE 32-bit | Paket B1 ist damit nicht optional. *(`WP-Plan.sln` und `.git` existieren; `CLAUDE.md` ist inzwischen korrigiert, siehe 13.7. Versionskontrolle als Rollback-Pfad ist vorhanden.)* |
| ~~O11~~ | ~~VDI 4640 nicht im Normtext verfügbar~~ | **erledigt**: Blatt 1 (Entwurf 2021-12, E13) und Blatt 2:2019-06 liegen vor und sind eingearbeitet — Untergrundkennwerte aus Bl. 1 Tab. 1, Auslegungstabellen aus Bl. 2 Anhang A/B (13.1). Offen bleibt nur die Klärung, ob die vorhandenen EPOS-Klimaregionen den 15 DIN-4710-Zonen entsprechen |

---

## 11. Prüfprotokoll der Codeverifikation (12.08.2026, nachgeprüft 14.08.2026)

Drei unabhängige Prüfläufe gegen den Quellcode. Ergebnis: **13 von 14 geprüften
Aussagen aus Fassung 1 bestätigt**, vier sachliche Korrekturen, acht neue
Bestandsfehler (Kapitel 8).

**Nachprüfung zur Fassung 12 (14.08.2026):** 49 Einzelaussagen der Fassung 11 durch
fünf unabhängige Prüfläufe kontrolliert, jede gemeldete Abweichung adversarial
gegengeprüft — **42 bestätigt, 7 korrigiert** (Details in der Fassungshistorie); ein
Fehlalarm des Erstprüfers wurde in der Gegenprüfung verworfen (B0-7 war korrekt
referenziert). Der Umsetzungsstand des Konzepts im Code betrug zum Prüfzeitpunkt
**0 %**; alle zehn (jetzt elf) B0-Bestandsfehler waren unverändert vorhanden.

**Bestätigt:** Spaltenliste in `SchemaSicherstellen`; Quellentypen und fehlendes
„Erdreich"; `break` nach dem ersten WP-Eintrag; `Quellspeicher()` aus STAMM per
Bezeichner mit Start „voll"; stündliche Brauchwasserberechnung und ungekürzte
Weitergabe an die WP; verworfener Solar-Überschuss; BHKW-Pendelspeicherformel und
Schwellen; die `Rücklauf`/`Ruecklauf`-Inkonsistenz im Code (welche
Seite defekt ist, hängt am realen Spaltennamen — 13.7); Heizstab-Zuweisung; implizites `CopyFromStamm`;
Nichtkollision der geplanten `Tab_Pufferspeicher`-Spalten; `WS_Typ`-Werte und
WW-Vorrang; `ID_PUFFER` ungenutzt und mit STAMM-ID befüllt; Klimadatenbasis für das
Erdreichmodell.

**Korrigiert in Fassung 2:** (1) Pufferspeicher-Datenmodell mit drei Repräsentationen
und instabilen Anlagen-IDs → E7, Kapitel 2.3/5.2. (2) `solarthermie_list` ist toter
Code → Kapitel 6.2. (3) `Tab_ErgebnisPufferspeicher` nicht über das
`ADD COLUMN`-Muster → Kapitel 6.6. (4) Aufwand → Kapitel 9.

**Ergänzt in den Fassungen 3 und 4** (fachliche Festlegungen, nicht aus dem Code
ableitbar, aber an vorhandenen Codestellen verankert): Ladepriorität und
Ladeobergrenzen (3.4) mit der aus der Kaskade gelösten Ladephase (6.3); der
PV-Sonderfall (3.5), dessen Kriterium „PV-Überschuss in dieser Stunde" aus der
bestehenden Auswertung in `SimulationWaermepumpe.cs:433-445` stammt; die
Entladereihenfolge bei mehreren Puffern je Kanal (3.6). Alle drei sind per Default
verhaltensneutral zum Bestand und über die Spalten `WS_Ladeprio*`, `WS_Ladegrenze*`,
`WS_Ladeprio_PV`, `Schwelle_Aus_Nachrang` und `Entladeprio` übersteuerbar.

**Nachgetragen in Fassung 5, korrigiert in Fassung 12:** Die Lokalisierungsanalyse
(13.6) hat den Bestand ausgezählt (287 Codestellen, 156 eindeutige Texte, nur 7
vorhandene Ressourcenschlüssel) und zwei weitere Bestandsfehler aufgedeckt (B0-9,
B0-10 in Kapitel 8). Die Satelliten-Lage stellte sich in Fassung 12 anders dar als
in Fassung 5 angenommen: Es gibt **auch** `MyResource\Resource.de-DE.resx` (redundant
zur neutralen Datei), und die englischen Satelliten von `Form_Simulation_Config` und
`Form_KonfigPufferspeicher` **existieren bereits** — unvollständig, mit echten
Übersetzungen (Details und Bereinigung in 13.6). Beide Formulare stehen auf
`Localizable = true`.

**Normquellen (Fassungen 9–11):** Das Erdreichmodell ist gegen **VDI 4640 Blatt 1**
(Entwurf 2021-12, Tabelle 1: Wärmeleitfähigkeit und volumenbezogene Wärmekapazität)
und **VDI 4640 Blatt 2:2019-06** (Anhang A Tabelle A1/A2 für Kollektoren, Anhang B
Tabelle B1/B2 für Sonden) verifiziert. Zwei Modellaussagen sind dabei unabhängig
bestätigt worden: die neutrale Zone bei 10–20 m und die ungestörte
Untergrundtemperatur von 11 °C, die Blatt 2 seinen Sondentabellen zugrunde legt.
Zwei Annahmen mussten korrigiert werden: der Bodentyp-Katalog (bodenkundliche
Oberbodenwerte statt geologischer Untergrundkennwerte) und die Entzugsleistungs-
Richtwerte (Faustwerte der Ausgabe 2001 statt der klimazonenabhängigen
Auslegungstabellen von 2019).

**Alle Prüfungen abgeschlossen.** Code, Datenbankschema und Normgrundlagen sind
verifiziert; offen ist allein die Frage, ob die vorhandenen EPOS-Klimaregionen den
15 DIN-4710-Zonen entsprechen (13.1).

---

## 12. Zusammenfassung der neuen Datenfelder

Konsolidierte Übersicht aller in diesem Konzept eingeführten Spalten — als Vorlage
für die `SchemaMigration` (ADR-001) und die Abnahme.

**`Tab_Energieanlagen`** (je Wärmeerzeuger-Anlage, über `SchemaSicherstellen`):

| Spalte | Typ | Default | Kapitel |
|---|---|---|---|
| `WS_Ziel` | TEXT(50) | `Heizkreis` | 5.3 |
| `WS_ID_Puffer` | LONG | 0 | 5.3 |
| `WS_Ladeprio` | LONG | 0 (Vorgabe nach Erzeugertyp) | 3.4 |
| `WS_Ladegrenze` | DOUBLE | 0 (Puffer-Regel gilt) | 3.4 |
| `WS_Ladeprio_PV` | LONG | 0 (keine Sonderregel) | 3.5 |
| `WS_Ziel2` | TEXT(50) | leer | 5.3 |
| `WS_ID_Puffer2` | LONG | 0 | 5.3 |
| `WS_Ladeprio2` | LONG | 0 | 3.4 |
| `WS_Ladegrenze2` | DOUBLE | 0 | 3.4 |
| `WQ_ID_Puffer` | LONG | 0 | 5.3 |
| `WQ_Tiefe` | DOUBLE | 0 | 4.5 (Sonde: Länge) |
| `WQ_Flaeche` | DOUBLE | 0 | 13.1 (Kollektorfläche) |
| `WQ_Anzahl` | LONG | 1 | 13.1 (Anzahl Sonden) |
| `WQ_Bodentyp` | TEXT(50) | leer | 4.5 / 13.1 |
| `WQ_Quellsystem` | TEXT(50) | leer | 4.5 |

**`Tab_Pufferspeicher`** (je Projekt-Speicher):

| Spalte | Typ | Default | Kapitel |
|---|---|---|---|
| `Verwendung` | TEXT(50) | Pflichteingabe | 5.1 |
| `Vorlauf` | LONG | aus Erzeuger-Abfrage | 5.1 |
| `Ruecklauf` | LONG | aus Erzeuger-Abfrage | 5.1 |
| `Schwelle_Ein` | DOUBLE | 10 | 5.1 |
| `Schwelle_Aus` | DOUBLE | 95 | 5.1 |
| `Schwelle_Aus_Nachrang` | DOUBLE | = `Schwelle_Aus` | 3.4 |
| `Entladeprio` | LONG | 0 (automatisch) | 3.6 |

**`Tab_Klimaregion`** (je Klimaregion, einmalig zu pflegen):

| Spalte | Typ | Default | Kapitel |
|---|---|---|---|
| `Klimazone_DIN4710` | LONG | 0 (unbestimmt) | 13.1 — Zone 1–15 für die Kollektor-Auslegungstabelle |

**`Tab_Einstellungen`** (je Projekt, nur anhängen — positionsbasiertes Lesen!):

| Spalte | Typ | Default | Kapitel |
|---|---|---|---|
| `Extrapolation_erlaubt` | YESNO | nein | 13.4 |

**`Tab_ErgebnisPufferspeicher`** (neue Tabelle, je Simulationslauf und Speicher):
`ID, ID_Ergebnis, ID_Pufferspeicher, Bezeichner, Verwendung, Q_max, Ladung_gesamt,
Entladung_gesamt, Verluste_gesamt, SOC_Ende, SOC_Mittel, SOC_Max, Vollzyklen` (6.6).

**Nicht vergessen:** `WS_ID_Puffer`, `WS_ID_Puffer2` und `WQ_ID_Puffer` müssen in die
`FK_MAP` von `ProjektDuplizierenCtrl` (`:71-79`) eingetragen werden, sonst zeigen
Varianten auf die Speicher des Quellprojekts (5.3).

---

## 13. Ausarbeitung der Umsetzungsdetails (E12)

Dieses Kapitel löst die zuvor als O3–O9 offenen Punkte auf.

### 13.1 Erdreichmodell: Plausibilisierung gegen VDI 4640 (vormals O5)

#### Bodentyp-Katalog nach VDI 4640 Blatt 1

**Quelle: VDI 4640 Blatt 1 (Entwurf 2021-12), Tabelle 1** „Beispiele für
Wärmeleitfähigkeit und volumenbezogene spezifische Wärmekapazität des Untergrunds".
Die Norm führt je Gesteinstyp einen **empfohlenen Rechenwert für λ** und einen
Bereich für ρ·c_p — genau die beiden Größen, aus denen die Temperaturleitfähigkeit
folgt:

```
a = λ / (ρ · c_p)          [m²/s]        (Formelzeichen a nach VDI 4640 Blatt 1)
d = √(2a / ω)              [m]           Dämpfungstiefe, ω = 2π/8760 h⁻¹
Amplitude(z) = A · e^(−z/d)
```

Der Katalog führt λ und ρ·c_p als Eingangsgrößen und leitet a daraus ab — so bleibt
der Normbezug nachvollziehbar und die Werte sind bei einer Normfortschreibung
einzeln pflegbar.

| Katalogschlüssel `WQ_Bodentyp` | Untergrund | λ [W/(m·K)] | ρ·c_p [MJ/(m³·K)] | a [mm²/s] | d [m] | Amplitude 1,5 m | 4 m | 10 m |
|---|---|---|---|---|---|---|---|---|
| `TON_TROCKEN` | Ton/Schluff, trocken | 0,5 | 1,55 | 0,32 | 1,80 | 43 % | 11 % | 0,4 % |
| `TON_NASS` | Ton/Schluff, wassergesättigt | 1,8 | 2,40 | 0,75 | 2,74 | 58 % | 23 % | 2,6 % |
| `SAND_TROCKEN` | Sand, trocken | 0,4 | 1,45 | 0,28 | 1,66 | 41 % | 9 % | 0,2 % |
| **`SAND_FEUCHT`** *(Default)* | Sand, feucht | 1,4 | 1,90 | 0,74 | 2,72 | 58 % | 23 % | 2,5 % |
| `SAND_NASS` | Sand, wassergesättigt | 2,4 | 2,50 | 0,96 | 3,10 | 62 % | 28 % | 4,0 % |
| `KIES_TROCKEN` | Kies/Steine, trocken | 0,4 | 1,45 | 0,28 | 1,66 | 41 % | 9 % | 0,2 % |
| `KIES_NASS` | Kies/Steine, wassergesättigt | 1,8 | 2,40 | 0,75 | 2,74 | 58 % | 23 % | 2,6 % |
| `MERGEL_LEHM` | Geschiebemergel/-lehm | 2,4 | 2,00 | 1,20 | 3,47 | 65 % | 32 % | 5,6 % |
| `TONSTEIN` | Ton-/Schluffstein | 2,2 | 2,25 | 0,98 | 3,13 | 62 % | 28 % | 4,1 % |
| `SANDSTEIN` | Sandstein | 2,8 | 2,20 | 1,27 | 3,57 | 66 % | 33 % | 6,1 % |
| `KALKSTEIN` | Kalkstein | 2,7 | 2,25 | 1,20 | 3,47 | 65 % | 32 % | 5,6 % |
| `GRANIT` | Granit | 3,2 | 2,55 | 1,25 | 3,55 | 66 % | 32 % | 6,0 % |
| `GNEIS` | Gneis | 2,9 | 2,10 | 1,38 | 3,72 | 67 % | 34 % | 6,8 % |

λ ist der **empfohlene Rechenwert** der Norm, ρ·c_p der Mittelwert des dort
angegebenen Bereichs. Weitere Gesteinstypen (Dolomit, Basalt, Marmor, Quarzit,
Torf u. a.) stehen in Tabelle 1 und lassen sich nach demselben Muster ergänzen; der
Startumfang deckt die in Deutschland üblichen Untergründe ab.

> **Abweichung gegenüber Fassung 5–8 dieses Konzepts:** Der frühere Katalog stützte
> sich auf bodenkundliche Labormesswerte (Themenheft „Thermische Eigenschaften von
> Böden", LGB Rheinland-Pfalz). Diese beschreiben **landwirtschaftliche Oberböden**,
> die Norm dagegen den **geologischen Untergrund**. Beim Lehm unterscheiden sich die
> Werte um Faktor 2 (0,59 gegenüber 1,20 mm²/s). Maßgeblich ist der Normwert; die
> bodenkundlichen Werte bleiben als Hinweis erwähnenswert, weil ein Flachkollektor in
> 1,0–1,5 m tatsächlich im Oberboden liegt und dort träger reagiert, als die
> Normwerte nahelegen.

#### Validierung an unabhängigen Referenzangaben

| Referenzaussage | Modellergebnis | Bewertung |
|---|---|---|
| VDI 4640 Bl. 1, Abschn. 4.1: neutrale Zone „in etwa 10 m bis 20 m Tiefe" | bei 10 m verbleiben 0,2–6,8 % der Oberflächenamplitude, bei 20 m < 0,5 % | ✔ bestätigt |
| VDI 4640 Bl. 1, Abschn. 4.1: mittlere Lufttemperatur in Deutschland „etwa 9,5 °C" | Modellansatz T_m = Jahresmittel der Klimadaten | ✔ konsistent |
| Fachliteratur: Phasenverschiebung ≈ 6 Monate in 6,4 m (α = 4,17·10⁻⁷ m²/s) | mit d = 2,05 m → 182 Tage = 6,0 Monate | ✔ exakt reproduziert |
| Ungestörte Erdreichtemperatur ≈ 11 °C | T_m 9,5 °C + Oberflächenoffset ≈ 1,5 K | ✔ Größenordnung bestätigt |

Die exakte Reproduktion der Phasenverschiebung prüft Dämpfungstiefe und Phasenterm
gemeinsam; die Quelle ist nicht in die Modellbildung eingeflossen.

#### Erdsonden — konstante Quelltemperatur

Ab der neutralen Zone (10–20 m nach VDI 4640 Blatt 1) ist der Jahresgang
abgeklungen. Für `WQ_Quellsystem = Sonde` entfällt die Kusuda-Formel:

```
T_Sonde = T_m + ΔT_Oberflaeche + grad_geo · max(0, Sondenlänge/2 − 20 m)

T_m             Jahresmittel der Außentemperatur (aus den Klimadaten, D ≈ 9,5 °C)
ΔT_Oberflaeche  Oberflächenoffset, Default 1,5 K
grad_geo        geothermischer Gradient, Default 0,03 K/m
```

**Der Abzug von 20 m ist normbegründet:** Nach VDI 4640 Blatt 1, Abschnitt 4.1,
stammt die Energie oberflächennaher Anlagen bis zur neutralen Zone „fast
ausschließlich aus Sonneneinstrahlung und Sickerwasser, sodass man den Einfluss des
geothermischen Wärmestroms vernachlässigen kann. Erst ab einer Teufe zwischen 20 m
und 100 m kann man von einem zunehmenden Anteil des geothermischen Wärmeflusses
sprechen." Die Wärmestromdichte aus dem Erdinneren beträgt nur 0,05–0,12 W/m²
gegenüber bis zu 1000 W/m² Sonnenstrahlung.

Beispiele: 50-m-Sonde → 9,5 + 1,5 + 0,03·5 = **11,15 °C**; 100-m-Sonde → 9,5 + 1,5 +
0,03·30 = **11,9 °C**. Beides deckt sich mit dem Richtwert von rund 11 °C für die
ungestörte Erdreichtemperatur in Deutschland. (Die frühere Formel ohne den 20-m-Abzug
lieferte für die 100-m-Sonde 12,5 °C und überschätzte den geothermischen Beitrag.)

#### Plausibilitätsprüfung nach VDI 4640 Blatt 2 (Anhänge A und B)

**Blatt 2:2019-06 liegt vor.** Die Norm arbeitet nicht mehr mit den pauschalen
Faustwerten der Ausgabe 2001, sondern mit Auslegungstabellen, die aus
Simulationsrechnungen stammen. Die bisher im Konzept genannten Spannen
(„20–40 W/m²", „20–100 W/m") sind damit **überholt** und werden ersetzt.

**Kollektoren — Anhang A, Tabelle A2.** Maximalwerte je **Klimazone** (15 Zonen nach
**DIN 4710**, Bild A1) und **Bodenart**. Einzuhalten sind **beide** Werte, Leistung
*und* Jahresenergie:

| Klimazone | Sand | Lehm | Schluff | Sandiger Ton | Volllaststunden |
|---|---|---|---|---|---|
| 1 | 28 / 46 | 34 / 56 | 36 / 59 | 39 / 64 | 1650 |
| 2 | 21 / 37 | 29 / 52 | 29 / 52 | 31 / 55 | 1800 |
| 3 | 25 / 41 | 32 / 52 | 35 / 57 | 38 / 62 | 1650 |
| 4 | 23 / 34 | 30 / 45 | 33 / 49 | 36 / 54 | 1500 |
| 5 | 29 / 49 | 37 / 62 | 38 / 64 | 41 / 69 | 1700 |
| 6 | 16 / 31 | 26 / 50 | 28 / 54 | 30 / 58 | 1950 |
| 7 | 25 / 40 | 32 / 51 | 33 / 52 | 37 / 59 | 1600 |
| 8 | 12 / 24 | 23 / 46 | 25 / 50 | 26 / 52 | 2000 |
| 9 | 17 / 29 | 26 / 45 | 29 / 50 | 32 / 56 | 1750 |
| 10 | 13 / 23 | 23 / 41 | 26 / 46 | 28 / 50 | 1800 |
| 11 | 5 / 12 | 9 / 21 | 12 / 28 | 13 / 31 | 2400 |
| 12 | 30 / 40 | 37 / 49 | 39 / 52 | 42 / 56 | 1350 |
| 13 | 16 / 28 | 25 / 45 | 27 / 48 | 29 / 52 | 1800 |
| 14 | 14 / 25 | 25 / 46 | 27 / 49 | 28 / 51 | 1850 |
| 15 | 14 / 24 | 25 / 43 | 26 / 45 | 29 / 50 | 1750 |

Format: **Entzugsleistung [W/m²] / Entzugsenergie [kWh/(m²·a)]**. Die Bandbreite
reicht von 5 W/m² (Zone 11, Sand — Höhenlagen ab 1000 m, laut Norm „aus
wirtschaftlichen Gründen nicht zu empfehlen") bis 42 W/m² (Zone 12, sandiger Ton).
Die pauschale Spanne „20–40" trifft also nur die mittleren Zonen.

Randbedingungen der Tabelle: PE-Rohr 32 × 3,0 mm, turbulente Durchströmung,
Heizgrenztemperatur 12 °C, Normaußentemperatur nach DIN EN 12831 Beiblatt 1. Bei
laminarer Strömung sinken Entzugsleistung um ~10 % und Rohrabstand um ~20 %.
Tabelle A2 nennt zusätzlich den optimalen **Rohrabstand** (0,2…0,65 m je Bodenart) —
eine sinnvolle Zusatzausgabe im Dialog. Für Kapillarrohrmatten gilt Tabelle A3 mit
eigenen Werten.

Bodenarten und ihre Kennwerte nach **Tabelle A1**:

| Bodenart | Wassergehalt [Vol.-%] | λ [W/(m·K)] |
|---|---|---|
| Sand | < 10 | 1,2 |
| Lehm | 25…31 | 1,5 |
| Schluff | 35…40 | 1,5 |
| Sandiger Ton | 35…40 | 1,8 |

**Sonden — Anhang B, Tabellen B2 bis B7.** Die spezifische Entzugsleistung hängt ab
von Wärmeleitfähigkeit des Untergrunds, Anzahl der Sonden (Reihe, Abstand 6 m) und
Jahresvolllaststunden. Auszug aus **Tabelle B2** („nur Heizen", Austritt −5 °C bei
Spitzenlast), Werte in W/m:

| Volllaststunden | Sonden | λ = 1,0 | 2,0 | 3,0 | 4,0 W/(m·K) |
|---|---|---|---|---|---|
| 1200 h/a | 1 | 37,5 | 52,0 | 61,5 | 68,3 |
| | 5 | 29,7 | 43,4 | 53,4 | 60,8 |
| 1800 h/a | 1 | 28,6 | 43,0 | 53,0 | 60,4 |
| | 5 | 21,6 | 33,9 | 43,6 | 51,3 |
| 2400 h/a | 1 | 23,7 | 37,4 | 47,3 | 55,0 |
| | 4 | 18,0 | 29,5 | 38,5 | 46,0 |

Randbedingungen: Doppel-U-Sonde 32 × 3,0, Verfüllung λ = 0,8 W/(m·K), Bohrloch
150 mm, turbulente Strömung. Für laminare Strömung gilt Korrekturfaktor 0,79…0,85
(Tabelle B1). **Die Norm setzt eine mittlere ungestörte Untergrundtemperatur von
11 °C über die Sondenlänge an** — das bestätigt den Sondenansatz oben unabhängig.

#### Was die Umstellung für EPOS-Plan bedeutet

Die Prüfung wird präziser, braucht aber vier Eingangsgrößen:

| Größe | Herkunft |
|---|---|
| **Klimazone 1–15** (DIN 4710) | neue Spalte `Tab_Klimaregion.Klimazone_DIN4710` — die Zone ist eine Eigenschaft der Region, nicht des Projekts, und wird damit **einmal je Region gepflegt** statt je Projekt. Vorbelegung über die Zonenkarte (Bild A1 der Norm) anhand der Regionskoordinaten; im Erdreichdialog überschreibbar. **Vor der Umsetzung zu klären, ob die vorhandenen EPOS-Klimaregionen bereits den DIN-4710-Zonen entsprechen** — dann ist die Zuordnung eine reine Datenpflege |
| **Bodenart** (4 Klassen aus Tab. A1) | Zuordnungsspalte im Bodentyp-Katalog (13.1): Jeder der 13 Untergrundtypen aus Blatt 1 wird auf die nächstliegende der vier Blatt-2-Bodenarten abgebildet |
| **Jahresvolllaststunden** | **liegt bereits vor** — die Simulation rechnet sie ohnehin (`Betriebsstunden_Gesamt` je WP) |
| **Kollektorfläche / Sondenlänge und -anzahl** | Eingabe im Erdreichdialog (4.5): `WQ_Flaeche`, `WQ_Tiefe`, `WQ_Anzahl` |

**Vorschlag zur Stufung**, weil der Datenumfang gewachsen ist:

- **Stufe 1 (im Paket 3 enthalten):** Tabelle A2 und Tabelle B2 als Katalog im Code,
  Warnung bei Überschreitung von Leistung **oder** Jahresenergie. Deckt den
  Standardfall „Heizen ohne Trinkwarmwasser" ab. Zusätzlicher Aufwand gegenüber der
  bisherigen Planung: **+1 PT** (Katalogpflege 15 × 4 Werte plus B2, Klimazonen-Zuordnung).
- **Stufe 2 (später):** Tabellen B3–B7 für die übrigen Betriebsfälle
  (mit Trinkwassererwärmung, mit Kühlung, andere Austrittstemperaturen), Tabelle A3
  für Kapillarrohrmatten, Rohrabstands-Empfehlung im Dialog. **+1–2 PT**.

Die zweite Warnbedingung — Quelltemperatur minus Spreizung soll 0 °C nicht dauerhaft
unterschreiten — bleibt unverändert bestehen.

> **Normstand (E13).** Grundlage ist **VDI 4640 Blatt 1, Entwurf 2021-12**
> (Gründruck, Einspruchsfrist war 2022-05-31); der gültige Weißdruck ist die Ausgabe
> 2010-06. **Entscheidung: Es wird mit dem Entwurf 2021-12 gearbeitet** — er ist der
> fachlich aktuelle Stand, und die Untergrundkennwerte der Tabelle 1 sind
> Erfahrungswerte aus Fachliteratur und Untersuchungsvorhaben, die sich durch eine
> Überführung in den Weißdruck erfahrungsgemäß nicht wesentlich ändern. Auf den
> Weißdruck wird nicht gewartet.
>
> Zwei Pflichten bleiben: Ergebnisse und Programmdokumentation weisen den
> **Entwurfsstand** aus ("nach VDI 4640 Blatt 1, Entwurf 2021-12") — dasselbe
> Vorgehen wie im TWW-Konzept für den A100-Entwurf. Und erscheint der Weißdruck
> später, ist ausschließlich Tabelle 1 gegenzuprüfen; da der Katalog λ und ρ·c_p
> getrennt führt, ist eine Aktualisierung eine reine Datenpflege ohne Codeänderung.
>
> **Blatt 2:2019-06 liegt vollständig vor** (Weißdruck) und ist mit den Anhängen A und B
> eingearbeitet; die Berichtigung 2020-04 betrifft nur Rohrmaterialien und ist ohne Belang
> für die Auslegungstabellen.

### 13.2 Datenbankpfad und Ausrollmechanismus (vormals O6) — ersetzt durch ADR-001

**Gegenstandslos geworden** *(Fassung 12)*: Der hier ursprünglich behandelte
Pfadkonflikt betraf `UpdateDatabaseFromScript.GetDBPath()` (Registry
`HKCU\…\ODBC.INI\TEST`) gegenüber `DataRepository.GetDBPath()`. Mit
[ADR-001](ADR-001_Schema-Ausrollung.md) (angenommen 14.08.2026) ist die Klasse
endgültig verworfen; Schema und Datenmigration laufen als versionierte
In-Code-Migration (`SchemaMigration`) **ausschließlich über `DataRepository`** — es
gibt nur noch einen DB-Pfad. Drei Detailanforderungen aus der früheren Fassung
bleiben sinngemäß erhalten und wandern in den ADR-Mechanismus:

1. Aufruf beim Programmstart nach der Lizenz-/Versionsprüfung in `Program.cs`, vor
   dem Öffnen des Hauptfensters.
2. Das Migrationsprotokoll weist den tatsächlich verwendeten DB-Pfad als erste Zeile
   aus, damit im Supportfall sofort erkennbar ist, welche Datei bearbeitet wurde.
3. Die Fehlerbehandlung unterscheidet „Spalte/Objekt existiert bereits"
   (unkritisch, protokolliert) vom echten Fehler (Marker bleibt stehen,
   Startblockade) über den OLE-DB-Fehlercode.

### 13.3 Anzeigen für mehrere Pufferspeicher (vormals O7)

Der Alias `puffer_wp` (6.7) ist eine Übergangslösung. Drei Stellen zeigen bei
mehreren Puffern stillschweigend nur einen; sie werden in Paket 7 umgestellt:

| Stelle | Heute | Künftig |
|---|---|---|
| `NavigatorWaerme.cs:116-118, 146, 159` | eine Chartserie „Speicherfüllstand" aus `sim.puffer_wp.SOC_stuendlich`; Checkbox aktiv nur wenn `puffer_wp != null` (auch CSV-Spalte `:89-90` und Y-Skalierung `:126` hängen an derselben Serie) | **eine Serie je Puffer**, benannt nach Speicher und Verwendung. Statt einer Checkbox eine kleine Auswahlliste der Puffer. **Wichtig:** `Series.Name` muss ein technischer Schlüssel werden (`PUFFER_<ID>`), der Anzeigetext geht in `LegendText` — sonst kollidiert die Umstellung mit der Lokalisierung (13.6) |
| `Form_Simulation_Detail.cs:292-298` | CSV-Export mit drei fest verdrahteten Pufferspalten (Ladung, Entladung, SOC) für den einen `puffer_wp`, ohne Speicherbezeichner im Kopf | **drei Spalten je Puffer** (SOC, Ladung, Entladung), Spaltenkopf mit Speicherbezeichner. Die Kopfzeile bleibt deutsch (Exportformat, 13.6) |
| `Form_Simulation_Detail.cs:1241-1244` | `textBox_Pufferspeicher` zeigt `Q_max` + Bezeichner des einen `puffer_wp`; nur **ohne** Zuordnung greift der Legacy-Ausdruck `Volumen · 1,16`. Der eigentliche Fehler liegt im Runner (`:138`): `Volumen · 1,16` ohne ΔT und ohne /1000, Volumen aus dem **WP-Datensatz** statt aus dem zugeordneten Puffer — der gespeicherte Wert widerspricht der Anzeige (6.6) | **kleine Ergebnistabelle** je Puffer: Bezeichner, Verwendung, Q_max, Ladung, Entladung, Verluste, Vollzyklen, SOC-Ende. Speist sich direkt aus `Tab_ErgebnisPufferspeicher` (6.6) und ist damit identisch mit Bericht und Wirtschaftlichkeit |

Ergänzend: Solange die Umstellung nicht abgeschlossen ist, weist die
Detailansicht bei mehr als einem Speicher sichtbar darauf hin („Speicher 1 von 2");
ein stiller Teilausweis ist die schlechteste Variante.

### 13.4 Engine ohne MessageBox — Protokoll- und Fehlerkanal (vormals O4)

**Ausgangslage.** `Konzept_Variantenbericht.md` setzt in Entscheidung E10 und
Kapitel 3.4 voraus, dass die Simulation **headless** läuft: Vor der Berichtserstellung
prüft der Dialog die Ergebnis-Zeitstempel je Projekt und bietet an, fehlende oder
veraltete Läufe automatisch nachzuholen — über

```csharp
new SimulationRunner().SimuliereUndSpeichere(id, out string fehler);
```

mit einer frischen Instanz je Projekt. Genau dieses `out fehler`-Muster ist die
Vorlage: Die Engine gibt Fehler **zurück**, statt sie anzuzeigen.

Heute brechen **mindestens acht** Stellen diese Zusage *(Fassung 12: zwei mehr als
bisher gezählt)*, weil sie eine `MessageBox` öffnen und damit
einen unbeaufsichtigten Lauf blockieren:

| Stelle | Anlass | Künftige Behandlung |
|---|---|---|
| `SimulationWaermepumpe.cs:189-191` | keine Kennlinie zur gewählten Vorlauftemperatur | Fehler → Lauf für diese Anlage abbrechen, Protokolleintrag |
| `SimulationWaermepumpe.cs:679-686` | **Rückfrage**, ob unterhalb der niedrigsten Stützstelle extrapoliert werden darf | siehe unten |
| `SimulationWaermebedarf.cs:167` | Gebäudetyp nicht definiert | Warnung, Standardprofil verwenden |
| `SimulationWaermebedarf.cs:642`, `:749` | Prozess-/Brauchwassertyp nicht definiert (enthält zudem den Tippfehler „DerTyp") | Warnung, Anteil = 0 |
| `Z_ProjektPufferSpCtrl.cs:52` | Pufferspeicher weder im Projekt noch im Stamm gefunden | entfällt mit der Ablösung der Tabelle (5.4) |
| `SimulationStrombedarf.cs:68`, `:207` *(neu in Fassung 12)* | Fehler bei der Stromprofil-Berechnung (Sammel-`catch`); im Headless-Pfad erreichbar über `SimulationRunner.cs:75` | Fehler → Protokolleintrag, Lauf abbrechen |

**Dazu kommt der DB-Fehlerpfad** *(neu in Fassung 12)*: `DataRepository` zeigt bei
jedem Datenbankfehler selbst eine `MessageBox` (`:44, :67, :94, :123, :155, :226`),
und die Simulationsklassen greifen durchgängig darüber zu — die Kommentare in
`WaermequelleClass.cs:77` und `:181` halten genau das fest. Der Protokollkanal löst
den Headless-Lauf deshalb nur, wenn dieser Pfad mitbehandelt wird: ein Engine-Modus,
der statt des Dialogs eine Exception bzw. einen Fehlercode liefert, den
`SimulationProtokoll` einsammelt.

**Lösung.** Eine schlanke Protokollklasse, die alle Module befüllen:

```csharp
// Allgemein/Simulation/SimulationProtokoll.cs (neu)
public class SimulationProtokoll
{
    public List<string> Hinweise  { get; }
    public List<string> Warnungen { get; }
    public List<string> Fehler    { get; }
    public bool IstFehlerfrei => Fehler.Count == 0;
    public string AlsText(bool nurFehlerUndWarnungen = false);
}
```

- `SimulationControl.Protokoll` wird vor dem Lauf erzeugt und an alle Module
  durchgereicht; jeder Eintrag nennt Projekt, Anlage und Stunde, sofern zutreffend.
- **Interaktiver Aufruf** (`Form_Simulation_Detail`): Nach dem Lauf wird das Protokoll
  gezeigt — nur wenn es Einträge gibt, und in einem sammelnden Dialog statt in
  n Einzelmeldungen. Das ist zugleich eine spürbare Verbesserung: Heute kann eine
  Simulation dutzende Meldungen nacheinander zeigen.
- **Headless-Aufruf** (`SimulationRunner`): `SimuliereUndSpeichere(id, out fehler)`
  füllt `fehler` aus `Protokoll.AlsText(nurFehlerUndWarnungen: true)`. Der
  Variantenbericht kann die Meldungen damit unverändert in seiner Hinweisliste
  ausgeben (dortiges Kapitel 3.4).
- Das Protokoll wird zusätzlich im Berichtskapitel „Datengrundlage & Methodik"
  ausgewiesen — dort ist ohnehin der Platz für Vorbehalte zum Simulationsstand.

**Sonderfall Extrapolation.** Die Rückfrage in `SimulationWaermepumpe.cs:679-686`
ist die einzige echte Interaktion. Sie wird zu einer Vorab-Einstellung:
neue Spalte `Extrapolation_erlaubt` (YESNO) in `Tab_Einstellungen`, Default **nein**.
Bei `nein` wird auf die unterste Stützstelle gekappt und eine Warnung protokolliert;
bei `ja` wird wie bisher extrapoliert, ebenfalls mit Protokolleintrag. Die Einstellung
gehört in den Parameterbereich der Wärmepumpe in `Form_Simulation_Detail`.

### 13.5 PV-Ladebudget und Zweitsenke (vormals O3)

**Problem.** `SimulationWaermepumpe.cs:433-445` verteilt das PV-Überschussbudget
`pvRest` sequenziell über die Module. Mit einer Zweitsenke (E2) muss dasselbe Budget
zusätzlich auf zwei Ziele aufgeteilt werden; die Reihenfolge entscheidet über das
Ergebnis.

**Drei denkbare Regeln:**

| Variante | Verhalten | Bewertung |
|---|---|---|
| **A — sequenziell, Hauptsenke zuerst** | Hauptsenke wird bis zu ihrer Ladeobergrenze (3.4) bedient, erst der Rest geht an die Zweitsenke | deterministisch, entspricht der Definition der Zweitsenke als reiner Überschussverwertung; keine zusätzlichen Parameter |
| B — proportional zur freien Ladefähigkeit | Budget wird im Verhältnis der freien Kapazitäten aufgeteilt | verteilt Wärme auf zwei Speicher, senkt aber die erreichte Temperatur in beiden; schwerer nachvollziehbar |
| C — nach Ladepriorität der jeweiligen Senke | Die Ladeprioritäten (3.4) entscheiden auch innerhalb einer Anlage | konsistent mit der Prioritätslogik, kann aber dazu führen, dass die *Zweit*senke vor der *Haupt*senke bedient wird — begrifflich widersprüchlich |

**Empfehlung und Festlegung: Variante A.** Sie ist die einzige, die zur Definition
aus E2 passt („Zweitsenke ausschließlich zur Verwertung von Überschuss bzw.
verbleibendem Ladepotenzial, nie für Pflichtbedarf") und die ohne neue Parameter
auskommt. Konkret je Stunde und Anlage:

```
budget = Ladepotenzial der Anlage (bei Modus PV: begrenzt durch pvRest)
menge1 = min(budget, Ladefähigkeit Hauptsenke)     → Hauptsenke laden
budget -= menge1
if (Zweitsenke gesetzt && budget > 0)
    menge2 = min(budget, Ladefähigkeit Zweitsenke) → Zweitsenke laden
pvRest -= (menge1 + menge2) / COP_mittel
```

Die modulübergreifende Reihenfolge bleibt wie heute: Die Module verbrauchen das
PV-Budget in ihrer Anlagenpriorität (`Tab_Energieanlagen.Prioritaet`). Damit ist die
Aufteilung vollständig deterministisch und ohne zusätzliche Eingabe erklärbar.

Testfall für die Abnahme: eine WP im PV-Modus mit Hauptsenke Puffer Brauchwasser und
Zweitsenke Puffer Heizung, an einem Tag mit begrenztem PV-Überschuss — der
Brauchwasserspeicher muss zuerst bis zu seiner Obergrenze geladen werden.

### 13.6 Durchgängige Lokalisierung des Simulationsbereichs (vormals O9)

**Entscheidung:** Der Simulationsbereich wird vollständig zweisprachig gemacht — nicht
nur die neuen Dialoge. Grundlage ist eine Auszählung des Bestands.

#### Befund

- **287 hartkodierte, benutzersichtbare deutsche Zeichenketten** in 16 Dateien,
  daraus **156 eindeutige Texte** (hohe Dublettenquote: „Heizkessel" 12×,
  „Solarthermie" 11×, „Abbrechen" 6×). Ein zentraler Katalog spart rund ein Drittel
  des Übersetzungsaufwands.
- Verteilung: 122 Beschriftungen, 78 Dialog-/MessageBox-Fragmente in 47 Aufrufen,
  34 angezeigte Datenwerte, 29 Chart-/CSV-Beschriftungen, 12 Spaltenköpfe,
  12 Tooltips.
- **`MyResource.Resource` wird bisher an genau 7 Schlüsseln verwendet**, alle in
  `Form_Simulation_Config.cs` (`KONFIG_BHKW`, `…_HEIZKESSEL`, `…_SOLARTHERMIE`,
  `…_WAERMEPUMPE`, `…_PHOTOVOLTAIK`, `…_STROMSPEICHER`, `…_GESAMTSYSTEM`). Es gibt
  also **kein Gerüst zum Erweitern, sondern eines aufzubauen** — das vorhandene
  `LanguageItem`-Muster (DisplayName / DbValue) ist dabei die richtige Idee.
- *(korrigiert in Fassung 12)* Im Ressourcenkatalog existieren
  `MyResource\Resource.resx` (neutral = deutsch), `Resource.en-US.resx` **und —
  entgegen früherer Fassungen — auch `Resource.de-DE.resx`** (21 Einträge,
  inhaltlich redundant zur neutralen Datei), dazu eine 0-Byte-Datei
  `Resource.en-US.Designer.cs`. **Bereinigung vorab:** de-DE-Satellit löschen
  (Deutsch ist die Fallback-Kultur) und die 0-Byte-Datei entfernen — danach gilt
  wieder: **jeder Schlüssel ist an genau zwei Stellen zu pflegen.**
- *(korrigiert in Fassung 12)* `Form_Simulation_Config` und
  `Form_KonfigPufferspeicher` stehen bereits auf `Localizable = true`, und die
  englischen Satelliten **existieren** mit echten Übersetzungen — aber
  unvollständig: `Form_Simulation_Config.en-US.resx` deckt 65 von 298 Einträgen der
  neutralen `.resx` ab, `Form_KonfigPufferspeicher.en-US.resx` 10 von 98. Die
  de-DE-Satelliten beider Formulare sind Restbestände
  (`Form_Simulation_Config.de-DE.resx` enthält nur zwei Layout-Einträge, die
  Position/Größe eines Buttons kulturabhängig überschreiben) und werden bereinigt.

#### Die zentrale Regel: drei Schichten

| Schicht | Sprache | Beispiel |
|---|---|---|
| **Persistenz** — Werte in der Access-DB, SQL-Literale | **immer deutsch, eingefroren** | `Erzeuger='Wärmepumpe'`, `WS_Typ='Beides'`, `WQ_Typ='Aussenluft'`, `Tab_WP.Typ='Luft-Wasser'` |
| **Schlüssel** — Chart-Serien, ComboBox-Steuerwerte, Filter-Tokens | **sprachneutral, ASCII** | `PUFFER_12`, `VOL_GT_1000` |
| **Anzeige** | **lokalisiert über `.resx`** | `MyResource.Resource.SIM_WQ_AUSSENLUFT` |

Diese Regel gehört in `CLAUDE.md`, damit sie in künftigen Arbeitssitzungen erhalten
bleibt.

**Warum die DB-Werte deutsch bleiben müssen:** Die Engine vergleicht sie direkt.
`SimulationControl.cs:79` (`if (…Erzeuger != "Wärmepumpe") continue;`) und
`WaermequelleClass.cs:271-275` (`… AND Erzeuger='Wärmepumpe'`) würden bei
lokalisierten Werten **stillschweigend falsche Ergebnisse** liefern — ohne
Fehlermeldung. Dasselbe gilt für `Form_Simulation_Detail.cs:441-481` und die
`switch`-Zweige in `Form_Simulation_Config.cs:305-311`. Zusätzlich lägen in
Bestandsdatenbanken deutsche Werte, deren Lokalisierung eine Datenmigration
erzwingen würde.

#### Drei Bestandsfehler derselben Ursache

Alle drei entstehen dadurch, dass ein **Anzeigetext als Steuerwert** dient — und alle
werden im Zuge der Lokalisierung behoben (Ergänzung zum Vorab-Paket B0):

| # | Fehler | Fundstelle | Wirkung |
|---|---|---|---|
| B0-9 | `Form_KonfigPufferspeicher` setzt den **lokalisierten** Erzeugernamen direkt in die SQL-Abfrage `Abfrage_Erzeuger_Vorlauftemperaturen` ein | `Form_KonfigPufferspeicher.cs:43`, `:50` | In englischer Oberfläche findet die Abfrage nichts; Vor-/Rücklauf bleiben leer. Zusätzlich Stringkonkatenation statt Parameter |
| B0-10 | Volumenfilter vergleicht `comboBox_Volumen.Text` gegen deutsche Literale; ohne Treffer bleibt der Volumenteil des Prädikats leer | `Form_PufferSp.cs:184-189`, `Form_PufferSp_Admin.cs:60-65` | Erzeugt `… where <filter> and  order by …` (`:198` bzw. `:74`) → **SQL-Syntaxfehler zur Laufzeit** |
| B0-11 | Rückwärts-Mapping Anzeigetext → DB-Wert beim Speichern der Zuordnungen; ohne Treffer wird der **Anzeigename** als `Erzeuger` geschrieben (`match?.DbValue ?? z[0]`) | `Form_Simulation_Config.cs:1548-1549` | Sprachwechsel zwischen Anlegen und Lesen macht die Zuordnung stillschweigend wirkungslos |

#### Vorgehen

| Paket | Inhalt | PT |
|---|---|---|
| L0 | Encoding vereinheitlichen (mehrere Dateien sind ISO-8859-1 ohne BOM, u. a. `Form_PufferSp_Bearbeiten.cs`, `Form_PufferSp_einlesen.cs`) → UTF-8 mit BOM, `.editorconfig`; Konstantenklasse `DbWerte.cs` für die 62 verstreuten DB-Wert-Literale; Glossar DE→EN für ca. 40 Fachbegriffe | 2,0 |
| L1 | Vorhandene en-US-Satelliten von `Form_Simulation_Config` und `Form_KonfigPufferspeicher` über den Designer **vervollständigen** (Abdeckung heute 65/298 bzw. 10/98); de-DE-Satelliten und `MyResource`-Redundanz bereinigen (kein Coderisiko) | 0,5 |
| L2 | Ressourcenkatalog: 156 Schlüssel in beiden `.resx`, Namensschema `SIM_*`, `SIMQ_*`, `PSP_*`, `SIMENG_*`, `CHART_*` | 2,0 |
| L3 | Programmatische Dialoge `Form_QuellePufferspeicher`, `Form_Quellprofil` (71 Texte); Monats-/Wochentagsnamen über `CultureInfo` statt eigener Arrays | 1,5 |
| L4 | `Form_Simulation_Config.cs` (86 Texte); dabei die **drei duplizierten `LanguageItem`-Listen** zusammenführen (sie haben heute unterschiedlichen Inhalt: 4 vs. 5 Einträge) und `_zuordnungen` intern auf DbValue statt DisplayName umstellen | 3,0 |
| L5 | Behebung B0-9, B0-10 und B0-11 plus 17 MessageBoxen in den Pufferspeicher-Dialogen | 1,5 |
| L6 | `NavigatorWaerme`: Chart-Serien auf technische Schlüssel + `LegendText`, 30 Lookups nachziehen, Designer auf `Localizable`; `WaermequelleClass.TypAnzeige` und `CSV_FORMAT_HINWEIS` (dabei `const` → `static readonly`, da Konstanten keine Ressourcen referenzieren können) | 2,0 |
| L7 | Regressionstest beide Sprachen — **entscheidend: identische Simulationsergebnisse in DE und EN** | 2,0 |
| L8 | Build-Prüfung gegen neue Hardcodings, `CLAUDE.md`-Ergänzung | 0,5 |
| | **Summe** (inkl. 20 % Puffer für Layout- und Übersetzungsschleifen) | **≈ 17,5 PT** |

Feste Pixel-Geometrie in den programmatischen Dialogen ist das Hauptrisiko: Englische
Texte sind teils länger, Labelgrößen brauchen Reserve. Designer-`.resx` werden
ausschließlich über den WinForms-Designer gepflegt — eine `Localizable`-Ressource
trägt je Kultur auch Position und Größe, Handedits verschieben Steuerelemente.

**Nicht Teil dieses Pakets:** das Setzen von `CurrentCulture` (heute wird nur
`CurrentUICulture` gesetzt). Das würde Zahlenformatierung und -parsing im gesamten
Programm ändern — eigenes Vorhaben mit eigenem Regressionsrisiko. Im
Simulationsbereich werden lediglich die fünf hartkodierten Dezimalkomma-Vorgaben
(z. B. `"10,0"` in `Form_QuellePufferspeicher.cs:100`) kulturneutral gemacht, weil sie
auf einem englischen Windows heute schon Parse-Fehler erzeugen.

*(Randnotiz, Fassung 12: `Program.cs:45/:48` öffnet den Sprach-Registry-Schlüssel mit
`@"Software\\wp-plan"` — im Verbatim-String ist das ein **literaler
Doppel-Backslash** im Schlüsselnamen; Konzept und CLAUDE.md schreiben
`Software\wp-plan`. Beim Lokalisierungspaket nicht stillschweigend „korrigieren":
Bestandsinstallationen tragen ihre Spracheinstellung unter genau diesem Schlüssel —
eine Bereinigung braucht eine Übernahme des Altwerts.)*

### 13.7 Verifikation des Access-Schemas (vormals O8)

**Status: verifiziert am 12.08.2026** gegen die produktive Datenbank
`Kenndaten.accdb` (92 MB, Stand 11.08.2026) — ausgelesen mit `mdbtools`
(`mdb-schema`, `mdb-queries`, `mdb-export` inkl. der Systemtabelle
`MSysRelationships`). Das zuvor herangezogene `migration.manuell.sql` ist **nicht**
der maßgebliche Stand und wurde verworfen. Alle fünf Punkte sind geklärt; zwei
Befunde korrigieren frühere Annahmen dieses Konzepts.

#### ✔ 1. `Rücklauf` — mit Umlaut

```
Tab_Energieanlagen: … [Vorlauf] Long Integer, [Rücklauf] Long Integer, …
```

| Zugriffspfad | Schreibweise | Bewertung |
|---|---|---|
| `WErzeugerCtrl.ReadAllFilter` (`:146`) — von der Simulation genutzt | `Rücklauf` | ✔ korrekt |
| `WizardCtrl.cs:181` (INSERT) | `Rücklauf` | ✔ korrekt |
| `WErzeugerCtrl.Insert` (`:81`), `Update` (`:24`), `ReadSingle` (`:191`) | `Ruecklauf` | ✘ defekt |

**Die Simulation rechnet mit korrekten Rücklaufwerten.** B0-4 bleibt im Vorab-Paket,
aber als Aufräumarbeit **ohne Ergebniswirkung** — die drei defekten Methoden werden
für Energieanlagen nicht aufgerufen. Auch die Abfrage
`Abfrage_Erzeuger_Ruecklauftemperaturen` verwendet `Max(Tab_Energieanlagen.Rücklauf)`
und ist damit stimmig.

> **Für die Migration (5.5):** Die Schreibweise ist tabellenübergreifend
> uneinheitlich — `Tab_Energieanlagen` hat `Rücklauf` **mit** Umlaut,
> `Z_ProjektPufferSp` hat `Ruecklauf` **ohne**. Beim Übertragen der Werte an den
> Puffer sind beide zu bedienen.

#### ✔ 2. `Tab_Einstellungen` — 23 Spalten, Reihenfolge bestätigt

```
ID, ID_Projekt, BHKW_Grenzleistung, Netzverluste, NetzverlusteEinheit, WP_Heizstab,
Kessel_Betriebsbereitschaft, Tool_1 … Tool_6, Ladefuellstand_Min, Ladefuellstand_Max,
Ladeleistung_Max, Ladefuellstand_Min_Auswahl, Ladefuellstand_Max_Auswahl,
Ladeleistung_Max_Auswahl, Ladeschwellwert, Betriebsart, Leistungsgrenze, Pendelspeicher
```

Exakt die Reihenfolge, die `KonfigurationCtrl.cs:44-66` über `row[0]…row[22]`
erwartet. **`Extrapolation_erlaubt` (13.4) darf ausschließlich am Ende angehängt
werden** — `ReadSingle` prüft weder Namen noch Länge, eine eingefügte Spalte
verschöbe stillschweigend die `Tool_*`-Zuordnung und damit die Erzeugerreihenfolge
der Simulation.

#### ✔ 3. Löschweitergaben — vorhanden, entgegen der früheren Vermutung

Die Datenbank führt **72 Beziehungen**, davon 68 mit Löschweitergabe. Für die
Ergebnisse gilt durchgängig `DEL-CASCADE`:

```
Tab_Ergebnis.ID → Tab_ErgebnisEnergiebedarf / …Waermepumpe / …BHKW /
                  …Heizkessel / …Solarthermie / …Photovoltaik      [DEL-CASCADE]
Tab_ErgebnisWaermepumpe.ID → Tab_ErgebnisWaermepumpeModul.ID_ErgebnisWaermepumpe
                                                                   [DEL-CASCADE]
   (analog für BHKW-, Heizkessel-, Solarthermie- und Photovoltaik-Module)
```

**Der Kommentar in `ErgebnisCtrl.cs:66-67` ist also korrekt** — das `DELETE FROM
Tab_Ergebnis` räumt die Detailtabellen tatsächlich mit ab. Die frühere Vermutung
„keine durchgängigen Kaskaden" ist widerlegt.

**Konsequenz für 6.6:** `Tab_ErgebnisPufferspeicher` wird mit derselben Beziehung
angelegt (`FOREIGN KEY (ID_Ergebnis) REFERENCES Tab_Ergebnis(ID) ON DELETE CASCADE`)
und fügt sich damit nahtlos ein. Das zusätzlich vorgesehene explizite Delete in
`Save` bleibt als Absicherung für Datenbanken, in denen das Schema-Skript nicht
gelaufen ist — es ist jetzt aber nachrangig, nicht mehr zwingend.

#### ✔ 4. Die beiden Erzeuger-Abfragen

```sql
Abfrage_Erzeuger_Vorlauftemperaturen:
  SELECT Tab_Energieanlagen.ID_Projekt, Tab_Typ_Energieanlagen.Bezeichner,
         Min(Tab_Energieanlagen.Vorlauf)
  FROM [Tab_Energieanlagen], [Tab_Typ_Energieanlagen] …

Abfrage_Erzeuger_Ruecklauftemperaturen:
  … Max(Tab_Energieanlagen.Rücklauf) …
```

Sie liefern je Projekt und Erzeugertyp den **kleinsten Vorlauf** bzw. **größten
Rücklauf** — die konservative Auslegung für einen gemeinsamen Speicher.

> **Nachtrag B0-Review (14.08.2026):** Die Wiedergabe oben ist unvollständig — die
> realen Definitionen enden auf ein hartkodiertes
> `HAVING (((Tab_Energieanlagen.ID_Projekt)=8))` und liefern damit für **jedes**
> Projekt 0 Zeilen; der äußere Filter ist wirkungslos. B0-9 umgeht die gespeicherten
> Abfragen deshalb per direktem SQL (`Form_KonfigPufferspeicher`). Sollen die
> Abfragen selbst erhalten bleiben, ist das `HAVING` zu entfernen — Schemaänderung,
> zurückgestellt. Der
Typbezug läuft über **`Tab_Typ_Energieanlagen.Bezeichner`**, also über deutsche
Datenwerte:

| ID | Bezeichner | entspricht `WizardItemClass` |
|---|---|---|
| 1 | Wärmepumpe | `WP_TYP` |
| 2 | Solarthermie | `SOLAR_TYP` |
| 3 | Photovoltaik | `PV_TYP` |
| 4 | Batteriespeicher | `SP_TYP` |
| 10 | Heizkessel | `KESSEL_TYP` |
| 11 | BHKW | `BHKW_TYP` |
| 12 | Pufferspeicher | `PUFFER_TYP` |

Damit ist **B0-9 vollständig belegt**: `Form_KonfigPufferspeicher.cs:43` übergibt den
*lokalisierten Anzeigenamen* an eine Abfrage, die den *deutschen DB-Bezeichner*
erwartet. In englischer Oberfläche findet sie nichts. Zugleich bestätigt die Tabelle
die Drei-Schichten-Regel aus 13.6: Diese Bezeichner sind Persistenzwerte und bleiben
deutsch.

#### ✔ 5. Pufferspeicher-Tabellen — keine Kollision

```
Tab_Pufferspeicher:       ID, ID_Projekt, Bezeichner, Hersteller, Speichertyp,
                          Bereitschaftsverluste, Gesamtvolumen, Investitionskosten
Tab_Pufferspeicher_STAMM: … + ReadOnly
Z_ProjektPufferSp:        ID, ID_Projekt, ID_Pufferspeicher, Pufferspeicher, Erzeuger,
                          Vorlauf, Ruecklauf, Prioritaet, Schwelle_Ein, Schwelle_Aus
Tab_Energieanlagen:       … + Prioritaet, WQ_Typ, WQ_Temp, WQ_Monatswerte, WQ_CSV,
                          WQ_Wochenwerte, WQ_Puffer, WQ_Spreizung, WQ_Regeneration,
                          WQ_Unbegrenzt, WS_Typ, BM_Typ
```

Alle in 5.1 und 5.3 geplanten Spalten sind frei. `Schwelle_Ein`/`Schwelle_Aus` und
die vollständige `WQ_*`/`WS_*`/`BM_*`-Familie sind bereits vorhanden —
`SchemaSicherstellen` ist also gelaufen und funktioniert wie beschrieben. Das
positionsbasierte `row[2]…row[6]` in `Form_PufferSp_Bearbeiten` trifft Hersteller,
Speichertyp, Bereitschaftsverluste, Gesamtvolumen, Investitionskosten — es passt.

#### ⚠ Zwei neue Befunde mit Konzeptwirkung

**(a) `Tab_Energieanlagen.ID_PUFFER` hat keine Beziehung.** Alle anderen
Komponentenverweise sind gesichert — `ID_WP`, `ID_BHKW`, `ID_Kessel`, `ID_Solar`,
`ID_PV`, `ID_SP` jeweils mit `UPD+DEL-CASCADE` —, **`ID_PUFFER` als einziger nicht**.
Das erklärt, warum `Form_PufferSp.cs:101` unbemerkt die STAMM-ID hineinschreiben
kann (2.3): Access lehnt es nicht ab, weil keine Integritätsregel greift. Der
Kommentar in `PufferSpCtrl.cs:148-150` („Beziehung verweist auf die Projekt-Tabelle")
beschreibt einen Sollzustand, keinen Ist-Zustand.

→ **Für E7:** Die neuen Spalten `WS_ID_Puffer`, `WS_ID_Puffer2` und `WQ_ID_Puffer`
werden **mit** erzwungener Beziehung auf `Tab_Pufferspeicher.ID` angelegt (Vorbild:
`Z_ProjektPufferSp.ID_Pufferspeicher`, dort `UPD+DEL-CASCADE`). Bei dieser
Gelegenheit sollte auch `ID_PUFFER` nachgerüstet werden — nach Bereinigung der
Altwerte, die auf STAMM-IDs zeigen.

**(b) `Tab_Pufferspeicher` hängt nicht an der Projekt-Kaskade.** An `Tab_Projekt.ID`
hängen mit Löschweitergabe: `Tab_Einstellungen`, `Tab_Energieanlagen`,
`Tab_ProjektWerte`, `Tab_Brennstoff_Projekt`, `Tab_Klimaregion`, alle `Z_Projekt*`
sowie `energy_price` und `energy_project_settings` — **`Tab_Pufferspeicher` fehlt**,
obwohl es eine `ID_Projekt`-Spalte führt.

→ Beim Löschen eines Projekts bleiben die Puffer-Projektkopien als Waisen zurück.
Das ergänzt **B0-6** um eine zweite Ursache und ist mit derselben Maßnahme zu
beheben: Beziehung `Tab_Projekt.ID → Tab_Pufferspeicher.ID_Projekt` mit
Löschweitergabe nachtragen. Da E7 `Tab_Pufferspeicher` zur führenden Ebene macht,
ist das nicht optional.

#### Nebenbefund: `CLAUDE.md` war in zwei Punkten veraltet *(erledigt, Fassung 12)*

Im Repository existieren sowohl **`WP-Plan.sln`** als auch ein **`.git`-Verzeichnis**;
die früher gegenteiligen Aussagen in `CLAUDE.md` sind inzwischen korrigiert (Stand
14.08.2026). Für dieses Konzept bleibt die Erleichterung: Versionskontrolle ist
vorhanden, der in 13.6 geforderte Rollback-Pfad für die Lokalisierung existiert.

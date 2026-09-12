# Paket 4 — Engine-Kern (Umsetzungsprotokoll)

Stand: 14.08.2026 · Grundlage: [`Konzept_Simulation_QuellenSenken.md`](Konzept_Simulation_QuellenSenken.md),
Kapitel 6.1 (Transportstruktur), 6.2 (Speicher-Registry, mit dem Zusatz der Fassung 12),
6.7 (Alias `puffer_wp`), 7 (Zeile `WaermequelleClass`) und Kapitel 9 („Feature-Flag
empfohlen") · Vorarbeiten:
[`Paket1_SchemaMigration_Protokoll.md`](Paket1_SchemaMigration_Protokoll.md) (Schrittregister
und Versionsmarker), [`Paket2_KonfigUI_Protokoll.md`](Paket2_KonfigUI_Protokoll.md)
(`WaermesenkeClass`, `Ladeordnung`, `StilleDb`, Fußzeile der Konfiguration),
[`Paket7_Ergebnis_Anzeigen_Protokoll.md`](Paket7_Ergebnis_Anzeigen_Protokoll.md)
(Rolle, `ID_Anlage`, `Schluessel` am `SimulationPufferspeicher`).

**Nicht committet.** Keine Designer- oder `.resx`-Datei angefasst; die gesperrten Dateien
(`WizardCtrl`, `WErzeugerModel`, `Form_BHKWEing`, `WizardParent`, `Form_Heizkessel*`,
`RecordSet`) sind unberührt.

> **Aufbau dieses Protokolls.** Etappe 4a (Infrastruktur) und Etappe 4b (zweikanalige
> Kaskade) sind der Stand, den die Review gesehen hat. Was daraus wurde, steht im Kapitel
> **„Review-Nacharbeit (14.08.2026)"** am Ende — einschließlich der Nutzerentscheidung zu
> Befund 4b-1. Korrekturen an einzelnen Aussagen der Etappen stehen als Einschub an Ort und
> Stelle; die dort gezeigten Messwerte bleiben als Beleg des damaligen Stands erhalten.

---

# Etappe 4a — Infrastruktur (verhaltensneutral)

Etappe 4a legt die Bausteine des Engine-Umbaus an, **ohne einen Rechenweg zu ändern**.
Die einzige zulässige und im Folgenden belegte Ergebnisabweichung ist die neue
ID-Semantik des Quellspeichers (Teil C). Die eigentliche zweikanalige Kaskade mit
herausgelöster Ladephase folgt in Etappe 4b hinter dem Feature-Flag.

## 1. Umfang

### Neue Dateien

| Datei | Inhalt |
|---|---|
| `Allgemein/Simulation/SimulationKanaele.cs` | `Waermekanaele` (Heiz/WW je `float[8760]`, `Summe()`, `Uebernehmen()`, `Clone()`), `enum Senke`, `Senkenzuordnung` — Code-Skizze aus Konzept 6.1 übernommen; DEBUG-Selbsttest nach dem Muster von `ErdreichTemperatur` |
| `Allgemein/Simulation/Paket4_EngineKern_Protokoll.md` | dieses Protokoll |

### Geänderte Dateien

| Datei | Änderung |
|---|---|
| `Allgemein/Update/SchemaKatalog.cs` | `SPALTE_KASKADE_ZWEIKANALIG`, neue Gruppe `Schritt6_FeatureFlag` (`Tab_Einstellungen.Kaskade_Zweikanalig YESNO`), in `Alle` aufgenommen |
| `Allgemein/Update/SchemaMigration.cs` | `ZIEL_VERSION` 5 → 6, `SCHRITT_6_FEATUREFLAG`, Schritt 6 im Register und als `Schritt_6_FeatureFlag` |
| `Model/KonfigurationModel.cs` | Feld `Kaskade_Zweikanalig` (Vorbelegung `false`) |
| `Controller/KonfigurationCtrl.cs` | `ReadSingle` füllt das Feld **namensbasiert**; neue statische `KaskadeZweikanaligLesen` / `KaskadeZweikanaligSchreiben` (dialogfrei über `StilleDb`) |
| `Views/Simulation/Form_Simulation_Config.Uebersicht.cs` | `InitKaskadeSchalter()`, `AktualisiereKaskadeSchalter()`, `checkBox_KaskadeZweikanalig_CheckedChanged` — programmatische Checkbox „Zweikanalige Kaskade (Vorschau)" rechts in der Fußzeile, mit Mouseover-Hinweis |
| `Views/Simulation/Form_Simulation_Config.cs` | `SetControls` belegt den Schalter vor |
| `Allgemein/Simulation/SimulationPufferspeicher.cs` | `VERWENDUNG_BRAUCHWASSER`, `SchwelleAusNachrang`, `Entladeprio`, `ID_Projekt`, `LaedtGerade`, `ImRechenpfad`; `Reset()` setzt `LaedtGerade` |
| `Allgemein/Simulation/SimulationControl.cs` | `speicherRegistry` + Aufnahmereihenfolge, `SpeicherRegistryAufbauen()`, `SpeicherAufnehmen()`, `ErsterHeizpuffer()`, `QuellspeicherUebernehmen()`, `ReferenzierteSenkenPuffer()`; `puffer_wp` wird Eigenschaft (Alias); `senkenzuordnungen`; `KaskadeZweikanalig`; `bhkw_anlagen_ids` / `spk_anlagen_ids` befüllt |
| `Allgemein/Simulation/WaermequelleClass.cs` | `Quellspeicher()` löst über `WQ_ID_Puffer` auf die Projektkopie auf (dreistufige Kette, neue Hilfsmethode `QuellspeicherZeile`), setzt `ID_Projekt` |
| `Allgemein/Simulation/WaermesenkeClass.cs` | `SenkenLaden(idProjekt)` — alle Senkenzuordnungen eines Projekts in Kaskadenreihenfolge |
| `Allgemein/Simulation/SimulationSPK.cs` | Feld `spk_anlagen_ids` |
| `Allgemein/Simulation/SimulationBHKW.cs` | Feld `bhkw_anlagen_ids` |
| `../Referenzlauf/Migrationslauf.cs` | Schema-Nachweis für `Tab_Einstellungen` verschärft: prüft die Ordinalkette row[0..22] **positionsweise** und beide angehängten Spalten |

`SimulationWaermepumpe.cs` ist **nicht** angefasst — siehe Teil C, Entscheidung 2.

## 2. Teil A — `Waermekanaele`, `Senke`, `Senkenzuordnung`

Die Code-Skizze aus Konzept 6.1 ist wortgetreu übernommen (Feldnamen, Methodennamen,
Vorbelegungen). Ergänzt wurden nur Dinge, die die Skizze offenlässt:

**`Summe()` liefert einen neuen Vektor.** Ein zurückgegebenes internes Array wäre in
diesem Rechenkern eine Aliasing-Falle: Die Module überschreiben ihre Eingangsvektoren
in-place (B0-2), ein solcher Schreibzugriff hätte stillschweigend den Heizkanal verändert.

**`Uebernehmen()` — die vier festgelegten Randfälle:**

| Fall | Regel | Begründung |
|---|---|---|
| Kanalanteil unbestimmt (`vorherHeiz + vorherWW == 0`) | Rest vollständig auf den **Heizkanal** | Es gibt kein Verhältnis; dieselbe Regel, mit der Konzept 3.2 (O2) die Netzverluste zuordnet. Der WW-Kanal hatte in dieser Stunde nachweislich keinen Bedarf |
| Restsumme 0 | beide Kanäle 0 | fällt aus der Formel, steht nur da, weil es die häufigste Stunde ist |
| Rundungsrest | `WW` proportional, `Heiz` als **Differenz** | damit gilt `Heiz + WW == Restsumme` bitgleich; die Energieerhaltung hängt nicht daran, wie sich zwei getrennt gerundete Produkte addieren |
| negative Restsumme | wird verteilt, **nicht** geklemmt | ein negativer Rest ist ein Bilanzfehler des Aufrufers (Konzept 6.4 beschreibt genau so einen); Klemmen würde ihn verstecken |

**Selbsttest** (`Waermekanaele.Selbsttest()`, in `#if DEBUG`): acht zugesicherte Punkte,
darunter die Erhaltung über ein volles Jahr mit gemischtem Testfall (reine Heiz-, reine
WW-, gemischte und bedarfsfreie Stunden). Ergebnis siehe Teil F.

`Senkenzuordnung` bekommt zusätzlich die Abbildung `SenkeAusZiel` / `ZielAusSenke` auf die
Textwerte der Spalte `WS_Ziel`. Unbekanntes, Leeres und `null` werden zu
`Senke.Heizkreis` — dieselbe Regel wie in `WaermesenkeClass.Normalisieren` (Konzept 4.6,
erste Zeile der Tabelle). Damit gibt es keine zweite Auslegung der DB-Werte.

## 3. Teil B — Feature-Flag `Kaskade_Zweikanalig`

**Schema.** Schritt 6 der `SchemaMigration` hängt `Kaskade_Zweikanalig` (`YESNO`) an
`Tab_Einstellungen` an; `ZIEL_VERSION` steht auf 6. Ein eigener Schritt ist nötig, damit
eine bereits auf Stand 5 stehende Datenbank nur diese eine Spalte nachzieht und weder die
Schemaschritte noch die Datenmigration (Schritt 5, das einzige DML des Vorhabens)
wiederholt. `ADD COLUMN … YESNO` belegt bestehende Zeilen in Access mit `False` — der
Default „aus" braucht keine eigene Klausel, und ein Ja/Nein-Feld kennt kein NULL.

**Leseseite.** `KonfigurationCtrl.ReadSingle` liest die Spalte **namensbasiert**
(`dt.Columns.Contains(...)`), nicht als `row[24]`. Die Ordinalkette row[0]…row[22] bleibt
Zeichen für Zeichen unverändert; sie ist die brüchigste Stelle des Datenzugriffs, und jede
weitere Position hätte sie nur länger gemacht. Fehlt die Spalte (Datenbank noch nicht auf
Stand 6), bleibt es bei „aus". Der Wert wird in **beiden** Zweigen gesetzt — ein
wiederverwendetes Model dürfte sonst den Stand des zuvor gelesenen Projekts behalten.

**Schreibseite.** Ein eigenes, zielgenaues `UPDATE`
(`KonfigurationCtrl.KaskadeZweikanaligSchreiben`) statt einer Erweiterung von `Update()`.
Grund: Dessen Spaltenliste und die von `Insert()` sind an dieselbe Ordinalkette gekoppelt;
auf einer Datenbank ohne Schemastand 6 hätte ein erweitertes `UPDATE` das Speichern der
**gesamten** Konfiguration scheitern lassen — wegen eines Vorschauschalters.

**Oberfläche.** Programmatische Checkbox „Zweikanalige Kaskade (Vorschau)" am rechten Ende
der Fußzeile von `Form_Simulation_Config`, neben „Pufferspeicher anlegen / verwalten…".
Sie schreibt sofort (nicht erst beim „Speichern", siehe oben) und meldet das Ergebnis über
`ShowStatus`. Schlägt das Schreiben fehl — kein Einstellungssatz oder Spalte fehlt —,
springt der Haken zurück, damit die Anzeige nicht mehr behauptet, als die Datenbank
hergibt. Der Mouseover-Hinweis sagt ausdrücklich, dass die zweikanalige Rechnung erst mit
Etappe 4b kommt und der Schalter bis dahin nichts am Ergebnis ändert.

**Engine.** `SimulationControl.Do_Simulation` liest das Flag in das Feld
`KaskadeZweikanalig` und schreibt bei gesetztem Flag eine Zeile auf die Konsole —
**verzweigt aber nicht**. Belegt ist das nicht nur durch Lesen des Codes, sondern durch
einen zweiten Regressionslauf mit Flag „an" in allen neun Referenzprojekten (Teil F.4).

## 4. Teil C — Speicher-Registry

### Aufbau

`SimulationControl.speicherRegistry` (`Dictionary<int, SimulationPufferspeicher>`,
Schlüssel `Tab_Pufferspeicher.ID`) wird zu Beginn jedes Laufs aufgebaut. Die Reihenfolge
ist Teil des Vertrags:

1. **Senkenspeicher der Wärmepumpe** aus der Alt-Zuordnung `Z_ProjektPufferSp` — der Block
   ist gegenüber Paket 3 unverändert, nur das Ziel der Zuweisung ist eine lokale Variable.
   Konzept 6.7 verlangt genau das: „die heutige Initialisierung bleibt die Quelle der
   Parameter". Er steht an erster Stelle.
2. **Alle übrigen als Senke referenzierten Projekt-Puffer** (`WS_ID_Puffer`,
   `WS_ID_Puffer2` der Anlagen in Kaskadenreihenfolge, danach die Puffer der übrigen
   Zuordnungszeilen), mit den Betriebsparametern der Projektkopie: Volumen,
   Vorlauf/Rücklauf → `Q_max`, Bereitschaftsverluste, Schwellen inklusive
   `Schwelle_Aus_Nachrang`, `Entladeprio`, Verwendung über
   `WaermesenkeClass.WirksameVerwendung`.
3. **Quellspeicher** der WP-Module — sie tragen sich nach dem Modulaufbau selbst ein
   (`QuellspeicherUebernehmen`, siehe unten).

Aufgenommen werden nur **referenzierte** Puffer. Projekt 1023 der Referenzmenge zeigt,
warum: Es trägt über 80 Puffer-Kopien aus wiederholtem „Projekt duplizieren", von denen
genau einer benutzt wird.

`SimulationPufferspeicher` bekommt dafür `SchwelleAusNachrang`, `Entladeprio`,
`ID_Projekt` und `LaedtGerade`. Letzteres ersetzt ab 4b den heute **modulübergreifenden**
`bool _speicherLaden` in `SimulationWaermepumpe` (Konzept 6.2) — ein Hysteresezustand kann
nicht für zwei Speicher zugleich gelten. In 4a wird das Feld gesetzt, aber von keinem
Rechenpfad gelesen.

### Entscheidung 1: `puffer_wp` als Alias — und warum er eine Einschränkung braucht

`puffer_wp` ist jetzt eine Eigenschaft und liefert `ErsterHeizpuffer()`: den ersten
Registry-Eintrag in Aufnahmereihenfolge mit `Verwendung = "Heizung"`, **der im Rechenpfad
steht** (`SimulationPufferspeicher.ImRechenpfad`). In Etappe 4a trägt dieses Flag genau
der unter 1. aufgebaute Speicher sowie jeder Quellspeicher eines WP-Moduls.

Ohne die Einschränkung wäre Etappe 4a **nicht** verhaltensneutral. Die Registry enthält
seit der Datenmigration 5.5 auch Puffer, die heute niemand rechnet. Zwei Fälle aus der
Referenzmenge selbst:

| Projekt | Registry-Eintrag | heute | ohne `ImRechenpfad` |
|---|---|---|---|
| 1007 | `1007007` aus einer **Solarthermie**-Zuordnung, Verwendung „Heizung" | `puffer_wp = null`, `PufferWP_vorhanden = False` | `puffer_wp` zeigt darauf → die Wärmepumpe rechnete plötzlich mit Puffer |
| 1011 | `1011007` aus einer **Gesamtsystem**-Zuordnung | `puffer_wp = null` | dito |
| 1018 | `1018007` aus einer **BHKW**-Zuordnung | `puffer_wp = null` | dito |

Aus „kein Puffer" würde still ein „Puffer mit Q_max" — mit voller Wirkung auf
`Kapazitaet_Pufferspeicher`, auf die WP-Rechnung und auf alle Anzeigen. Ab Etappe 4b
rechnen alle Registry-Speicher mit; dann verliert das Flag seine Sonderrolle und die
Reihenfolge folgt der Entladepriorität (Konzept 3.6).

### Entscheidung 2: Quellspeicher — Übernahme statt geteilter Instanz

`WaermequelleClass.Quellspeicher()` löst den Speicher jetzt dreistufig auf
(Konzept 7, Zeile `WaermequelleClass`):

1. `WQ_ID_Puffer` → Zeile in `Tab_Pufferspeicher` (**Projektkopie**); Migrationsregel R3
   hat die Spalte aus dem Bezeichner aufgelöst,
2. `WQ_Puffer` (Bezeichner) in der Projektkopie, kleinste ID — deterministisch wie
   `WaermesenkeClass.QuellPufferDerAnlage`,
3. `WQ_Puffer` im Katalog `_STAMM` — der bisherige Weg, jetzt Rückfallebene für
   Altbestand ohne Projektkopie.

Die **Instanzen** wandern anschließend in die Registry: `SimulationControl` ruft direkt
nach `simulation_wp.Berechnung()` die Methode `QuellspeicherUebernehmen()` auf und nimmt
die Objekte aus `simulation_wp.Quellspeicher` unverändert (keine Kopien) auf. Damit ist
die vom Konzept 6.2 (Fassung 12) geforderte Ablösung der parallelen Liste
`wp_quellspeicher` erfüllt: Was das Modul rechnet, ist danach dasselbe Objekt, das in der
Registry steht — belegt durch die Registry-Probe (Teil F.5, „in Registry: True").

**Bewusst nicht umgesetzt: die geteilte Instanz bei mehreren Modulen am selben
Quellpuffer.** Nutzen zwei Module denselben Speicher, behält in 4a jedes Modul seine
eigene Instanz und nur die erste kommt in die Registry (eine Konsolenzeile weist darauf
hin). Ein Zusammenlegen wäre kein Aufräumen, sondern eine Ergebnisänderung: Die Entladung
beider Module liefe gegen einen gemeinsamen Füllstand, und `StundeAbschliessen` müsste
zugleich von „mehrfach" auf „genau einmal je Stunde" umgestellt werden (Konzept 6.3, die
Mehrfach-Falle). Beides gehört in Etappe 4b, wo es messbar ist. **Prüfung an den
Referenzprojekten:** Nur 1021 hat überhaupt einen Quellspeicher, und dort nutzt ihn genau
ein Modul (Anlage 10361; die zweite Wärmepumpe 10360 hat keine Puffer-Quelle). Der Fall
tritt in der Referenzmenge also nicht auf.

**Warum die Registry die Quellspeicher nicht vorab aus der Speicherzeile aufbaut:** Ihre
nutzbare Kapazität folgt nicht dem Temperaturpaar des Puffers, sondern der Spreizung
`WQ_Spreizung` der **Anlage** (dazu `WQ_Regeneration`). Ein vorab aus `Tab_Pufferspeicher`
gebautes Objekt trüge ein falsches `Q_max`. Deshalb die Übernahme nach dem Modulaufbau
statt einer zweiten Konstruktion — das ist die einzige Stelle, an der die Umsetzung von der
wörtlichen Aufgabenstellung („Aufbau … aus … `WQ_ID_Puffer`") abweicht; das Ergebnis ist
dasselbe, nur der Zeitpunkt liegt im Lauf statt davor.

### Dokumentierte ID-Semantik-Abweichung (Projekt 1021)

`ID_Pufferspeicher` des Quellspeichers zeigt nicht mehr auf die Katalogzeile, sondern auf
die Projektkopie. Der Wert landet in `Tab_ErgebnisPufferspeicher.ID_Pufferspeicher` und im
Serienschlüssel der Anzeigen.

```
Projekt 1021, aggregate.csv:  Pufferspeicher[0].ID_Pufferspeicher   8  →  1018014
```

Alle Rechengrößen bleiben gleich — an der Datenbank nachgeprüft:

| | STAMM (ID 8) | Projektkopie (ID 1018014) |
|---|---|---|
| Bezeichner | allSTOR exclusiv VPS 800/3-7 | allSTOR exclusiv VPS 800/3-7 |
| Gesamtvolumen | 778 l | 778 l |
| Bereitschaftsverluste | 2,4 kWh/24h | 2,4 kWh/24h |
| daraus `Q_max` (Spreizung 5 K) | 4,5124 kWh | 4,5124 kWh |

Damit ändert sich in der Regression **ausschließlich** dieser eine Wert; `Bezeichner`,
`Verwendung`, `Q_max`, alle Summen, alle Kennzahlen und alle drei Ganglinien
(`quellspeicher_10361_*.csv`) sind byte-genau gleich. Wären die Werte auseinandergegangen,
wäre die Umstellung nicht ausgeliefert worden, sondern der Katalog-Fallback geblieben.

Fachlich ist die Umstellung nötig, weil die Katalogzeile projektweit geteilt ist: Zwei
Projekte mit demselben Speichertyp zeigten bisher auf dieselbe ID — Ergebniszeilen und
Anzeigen konnten sie nicht auseinanderhalten, und der Kurzschluss-Test „derselbe Speicher
als Quelle **und** Senke" (Konzept 4.6) hätte nie greifen können, weil die Senke immer
eine Projekt-ID trägt.

### Was ausdrücklich unverändert bleibt

`AlleSpeicher()` liefert weiterhin genau die Speicher, die auch rechnen — **nicht** den
vollen Registry-Inhalt. Sonst kämen Speicher in die Ergebniszeilen, die in diesem Lauf
nichts getan haben, und jede Regressionsbasis wäre hinfällig. Die Zusammenführung gehört
zu 4b.

## 5. Teil D — Anlagen-IDs und Senkenzuordnungen

`SimulationControl` befüllt zusätzlich `simulation_bhkw.bhkw_anlagen_ids` und
`simulation_spk.spk_anlagen_ids` mit `Tab_Energieanlagen.ID`, **parallel** zu den
bestehenden Listen und indexgleich mit ihnen. Beide Bestandslisten bleiben, wie sie sind:

- `bhkw_list` trägt die `ID_BHKW`, also die **Katalogzeile**. Zwei BHKW desselben Typs im
  Projekt wären darüber nicht unterscheidbar — Senke, Ladepriorität und Speicherzuordnung
  hängen aber an der Anlage.
- `spk_list` trägt **Bezeichner**, und dieser Bezeichner ist zugleich der Modulname der
  Ergebniszeile (`SimulationRunner`, Modulauflistung Heizkessel) sowie der Suchschlüssel
  der Kesseldaten. Eine Umstellung auf IDs hätte die Modulnamen aller Kesselergebnisse
  verändert — deshalb die parallele Liste nach dem Muster `bhkw_list_Namen`.

`WaermesenkeClass.SenkenLaden(idProjekt)` liefert die Senkenzuordnungen aller
Wärmeerzeuger eines Projekts in Kaskadenreihenfolge als `List<Senkenzuordnung>` — eine
Abfrage für das ganze Projekt statt einer je Anlage, normalisiert über denselben Weg
(`AusDatenzeile`), damit es keine zweite Auslegung der Felder gibt.
`SimulationControl.Do_Simulation` füllt damit das Feld `senkenzuordnungen`; **kein
Rechenpfad wertet es aus.**

## 6. Teil E — Bewusste Abweichungen

| # | Abweichung | Begründung |
|---|---|---|
| 1 | Quellspeicher kommen **nach** dem Modulaufbau in die Registry, nicht vorab aus `WQ_ID_Puffer` | ihr `Q_max` folgt der Anlagen-Spreizung, nicht dem Temperaturpaar der Speicherzeile (Teil C, Entscheidung 2) |
| 2 | Mehrere Module am selben Quellpuffer teilen die Instanz **noch nicht** | Zusammenlegen wäre eine Ergebnisänderung und verlangt zugleich die Umstellung von `StundeAbschliessen` (Konzept 6.3) — Etappe 4b. In der Referenzmenge tritt der Fall nicht auf |
| 3 | `puffer_wp` liefert nur Registry-Einträge mit `ImRechenpfad` | ohne die Einschränkung wäre 4a in drei der neun Referenzprojekte nicht verhaltensneutral (Teil C, Entscheidung 1) |
| 4 | `Quellspeicher()` baut auch dann einen Speicher, wenn `WQ_Puffer` leer ist, sofern `WQ_ID_Puffer` gesetzt ist | der Fremdschlüssel ist die neue führende Referenz; bisher brach die Methode am leeren Bezeichner ab |
| 5 | `SimulationPufferspeicher.Verwendung` trägt jetzt drei Werte („Heizung", „Brauchwasser", „Quelle") | die ersten beiden sind die KANÄLE der Projektkopie, der dritte die ROLLE. Für Anzeige, Serienschlüssel und Vollzyklen-Bezug zählt allein „ist es ein Quellspeicher" — alles andere ist ein Senkenspeicher |
| 6 | `Referenzlauf/Migrationslauf.cs` mitgeändert | der Schema-Nachweis prüfte bisher nur, dass `Extrapolation_erlaubt` die **letzte** Spalte ist; mit einer zweiten angehängten Spalte hätte er „ACHTUNG: nicht am Ende!" gemeldet, ohne einen Fehler zu zählen. Jetzt prüft er die Ordinalkette row[0..22] positionsweise — der stärkere Nachweis |

## 7. Teil F — Verifikation

### F.1 Build

```
MSBuild ..\WP-Plan.sln -p:Configuration=Debug -p:Platform=x86     →  0 Fehler
MSBuild ..\WP-Plan.sln -p:Configuration=Release -p:Platform=x86   →  0 Fehler
```

Warnungen: **dieselben sechs Bestandswarnungen** wie vor der Änderung
(`WErzeugerModel.cs` CS0108, `StromverbraucherStammCtrl.cs` CS0108,
`KlimaregionStammCtrl.cs` 2 × CS0109, `MDIMainForm.cs` CS4014 und CS1998) — keine neue.

Gegenprobe zur `#if DEBUG`-Klammer: Die Zeichenkette `Selbsttest` kommt im
Debug-Assembly vor, im Release-Assembly **nicht** (`grep -a -c`: 1 / 0; im Metadaten-Heap
teilen sich die drei `Selbsttest()`-Methoden einen Eintrag).

### F.2 Migration

Quelle: `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` — **nur gelesen**, Schemastand 0
(die Spalte `SchemaVersion` existiert dort nicht). Ziel:
`C:\Waermeplan\Paket4a_Test\DB_Basis` (außerhalb des Repos).

```
Referenzlauf.exe migration <produktiv> C:\Waermeplan\Paket4a_Test\DB_Basis
  Schemastand vorher: 0   →   Schemastand nachher: 6
  Schritt 6  Feature-Flag Kaskade_Zweikanalig …: OK
             Tab_Einstellungen: 1 Spalten angelegt, 0 bereits vorhanden
  Abweichungen im Schema-Nachweis: 0            (Exit-Code 0)

Referenzlauf.exe migration … --nokopie      (No-op-Zweitlauf)
  Schritte 1-6: "bereits erledigt", Schemastand 6 → 6
  Abweichungen im Schema-Nachweis: 0            (Exit-Code 0)
```

Schema-Nachweis für `Tab_Einstellungen` (25 Spalten):

```
row[0..22] unveraendert (KonfigurationCtrl.ReadSingle liest positionsbasiert)
Extrapolation_erlaubt an Position 23  (angehaengt)
Kaskade_Zweikanalig   an Position 24  (angehaengt)
```

Der positionsweise Vergleich prüft alle 23 Namen der Ordinalkette einzeln
(`ID`, `ID_Projekt`, … `Pendelspeicher`) — nicht nur, dass die neuen Spalten hinten
liegen. Zusätzlich headless nachgewiesen: nach dem Schreiben des Flags liefert
`ReadSingle` weiterhin `Tool_1` und `Pendelspeicher` korrekt (Teil F.4).

### F.3 Regression (Pflicht)

Neun Referenzprojekte, gerechnet im Modus `projekt` auf `DB_Basis`, verglichen gegen
`Referenzlaeufe/2026-08-14_Paket7`:

```
Projekt_1007: PASS (29 Dateien, 324210 Werte)
Projekt_1008: PASS (21 Dateien, 227847 Werte)
Projekt_1010: PASS (18 Dateien, 201540 Werte)
Projekt_1011: PASS (29 Dateien, 324232 Werte)
Projekt_1017: PASS (20 Dateien, 245378 Werte)
Projekt_1018: PASS (19 Dateien, 210343 Werte)
Projekt_1021: FAIL (21 Dateien, 227840 Werte, 1 Abweichungen)
    aggregate.csv [Pufferspeicher[0].ID_Pufferspeicher]: ref=8 neu=1018014
Projekt_1023: PASS (25 Dateien, 262917 Werte)
Projekt_1024: PASS (22 Dateien, 236616 Werte)
```

**2.260.923 verglichene Werte, genau eine Abweichung** — die in Teil C dokumentierte
ID-Semantik. Alle Vektoren (8760- und 35040-Raster), alle Skalare, alle Speicherkennzahlen
und der `Bezeichner` derselben Ergebniszeile sind unverändert.
`Referenzlauf.exe pruefen` meldet für alle neun Projekte „plausibel".

> **Zur Referenzbasis:** `2026-08-14_Paket7` war zu diesem Zeitpunkt die gültige Basis.
> Mit der Review-Nacharbeit ist sie durch `2026-08-14_Paket4` abgelöst worden (N.14); die
> hier gezeigte 1021-Abweichung ist dort eingefroren, zusammen mit den beiden
> B0-13-Folgegrößen.

### F.4 Flag-Nachweis

Auf einer zweiten Kopie (`DB_Flag`) wurde `Kaskade_Zweikanalig` für **alle neun** Projekte
über `KonfigurationCtrl.KaskadeZweikanaligSchreiben` auf „an" gesetzt und zurückgelesen —
über `KaskadeZweikanaligLesen` **und** über `ReadSingle` in das Model-Feld:

```
Projekt 1008: schreiben=True  Lesen(still)=True  ReadSingle->model=True
              Ordinalkette unberuehrt: Pendelspeicher=0 Tool_1=Wärmepumpe
```

Derselbe Vergleich mit Flag „an" liefert **exakt dasselbe Ergebnis** wie mit Flag „aus"
(acht PASS, dieselbe eine 1021-Abweichung). Der Schalter ist in Etappe 4a nachweislich
wirkungslos.

### F.5 Selbsttest und Registry-Probe

`Waermekanaele.Selbsttest()` (Debug-Build, eigenes Konsolenprojekt im Scratchpad mit
Assembly-Referenz auf `WindowsFormsApplication1.dll`):

```
1. Summe(): elementweise = OK, eigener Vektor = OK
2. Erhaltung Heiz + WW == Restsumme (bitgleich): OK
3. Proportional 30/10 bei Rest 8 -> Heiz 6 / WW 2   OK
4. Kanalanteil 0 -> Heiz 5 / WW 0   OK
5. Restsumme 0 bei vorhandenem Bedarf -> Heiz 0 / WW 0   OK
6. Restsumme -4 bei 3/1 -> Heiz -3 / WW -1   OK
7. Clone(): Werte gleich = OK, Vektoren getrennt = OK
8. Senkenzuordnung Vorbelegung und Ziel-Abbildung: OK
ERGEBNIS: alle Pruefungen bestanden.
```

Registry-Probe, headless über `SimulationRunner.Simuliere` (rechnet, **speichert nicht**)
auf einer eigenen Kopie:

| Projekt | Registry | `puffer_wp` | `AlleSpeicher()` | Anlagen-IDs |
|---|---|---|---|---|
| 1008 | 2: `1008007` „Vitocell 140-E 600 Liter" Heizung Q_max 6,9600 **ImRechenpfad**; `1008008` Heizung Q_max 9,0248 (Heizkessel-Zuordnung, nicht im Rechenpfad) | ID 1008007, Q_max **6,9600** wie bisher, identisch mit der Registry-Instanz | 1 (unverändert) | — |
| 1021 | 1: `1018014` „allSTOR exclusiv VPS 800/3-7" **Quelle** Q_max 4,5124, Anlage 10361, ImRechenpfad | null (wie bisher) | 1 — `QUELLE_10361`, **dieselbe Instanz** wie in der Registry | — |
| 1007 | 1: `1007007` Heizung, **nicht** im Rechenpfad (Solarthermie-Zuordnung) | null (wie bisher) | 0 | — |
| 1011 / 1018 | 1: `1011007` bzw. `1018007`, nicht im Rechenpfad | null (wie bisher) | 0 | 1018: `bhkw_list`=2 / `bhkw_anlagen_ids=[10370,10371]`, `spk_anlagen_ids=[10369]` |
| 1017 | 0 | null | 0 | `bhkw_anlagen_ids=[10260]`, `spk_list=[eloBLOCK VE 28]` / `spk_anlagen_ids=[10259]` |
| 1023 | 1: `1018023` Heizung Q_max 13,9200, ImRechenpfad | ID 1018023, Q_max **13,9200** wie bisher | 1 | `spk_anlagen_ids=[11205]` |
| 1010 / 1024 | 0 | null | 0 | 1024: `bhkw_anlagen_ids=[11237]` |

`senkenzuordnungen` wird in allen Projekten gefüllt (1008: drei Anlagen, davon zwei mit
`PufferHeizung` auf Puffer 1008007; 1021: zwei Anlagen auf `Heizkreis [Beides]`).

### F.6 Kodierung und Diff

Jede geänderte Datei behält ihre Kodierung: BOM bleibt, wo BOM war
(`SimulationControl`, `SimulationSPK`, `SimulationBHKW`, `SchemaKatalog`,
`SchemaMigration`, `KonfigurationCtrl`, `KonfigurationModel`,
`Form_Simulation_Config.cs`, `Migrationslauf`), und bleibt aus, wo keins war
(`SimulationPufferspeicher`, `WaermequelleClass`, `WaermesenkeClass`,
`Form_Simulation_Config.Uebersicht.cs`). Zeilenenden durchgängig CRLF; die neue Datei
`SimulationKanaele.cs` ist UTF-8 **mit** BOM und CRLF (Konzept Kapitel 7).
`git diff --check` meldet für die geänderten Dateien nichts, `git diff --stat` zeigt
ausschließlich echte Änderungen (kein Zeilenende-Rauschen). Keine Designer- und keine
`.resx`-Datei angefasst; die sechs gesperrten Dateien sind unberührt.

## 8. Offene Punkte für Etappe 4b

| # | Punkt | Grundlage |
|---|---|---|
| 1 | **Zweikanalige Kaskade** hinter dem Flag: `Waermekanaele` durch die Kaskade führen, `Uebernehmen()` als Kompatibilitätsanker der einkanaligen Rechenwege | 6.1, 3.2 |
| 2 | **Ladephase aus der Kaskade lösen** (Phasen A–G der Reihenfolge-Invariante), Ladeprioritäts-Auflösung nach 3.4, Entladereihenfolge nach 3.6 | 6.3 |
| 3 | `LaedtGerade` **auswerten** und den modulübergreifenden `_speicherLaden` in `SimulationWaermepumpe` (`:106`) ablösen | 6.2 |
| 4 | **Geteilte Instanz** am Quellpuffer bei mehreren Modulen, zusammen mit `StundeAbschliessen()` genau einmal je Stunde und Speicher | 6.3, Entscheidung 2 |
| 5 | `ImRechenpfad` **entfernen**, sobald alle Registry-Speicher rechnen; `ErsterHeizpuffer()` auf die Entladepriorität umstellen | 6.7, 3.6 |
| 6 | `AlleSpeicher()` mit der Registry **zusammenführen** — erst wenn die Registry rechnet, sonst kippt die Regressionsbasis | 6.6 |
| 7 | `senkenzuordnungen`, `bhkw_anlagen_ids`, `spk_anlagen_ids` **konsumieren** (Senkenauswertung je Kessel/BHKW) | 6.5, Pakete 5/6 |
| 8 | `SenkeAbziehen` bei Puffer-Senke unterlassen; `verfuegbar` um den Fall „Ladefähigkeit" erweitern | 6.3 |

### Nebenbefund (nicht in 4a behoben)

`SimulationControl.Simulation_BHKW_Ctrl` leert `bhkw_list`, aber **nicht**
`bhkw_list_Namen`. Bei einem zweiten Lauf in derselben Sitzung wächst die Namensliste
weiter, und `SimulationRunner` greift mit dem Index von `bhkw_list` darauf zu — die
Modulnamen der BHKW-Ergebnisse verschieben sich. Dieselbe Familie wie B0-2 (Aliasing) und
B0-5. Die neue Liste `bhkw_anlagen_ids` wird korrekt geleert; der Bestandsfehler bleibt
unangetastet, weil seine Behebung eine Ergebnisänderung wäre — Kandidat für B0 oder
Paket 6.

## 9. Reproduktion

```powershell
# 1. Build (Release ist die Gegenprobe zur #if-DEBUG-Klammer)
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    C:\Waermeplan\WP_Plan\WP-Plan.sln -p:Configuration=Debug -p:Platform=x86
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    C:\Waermeplan\WP_Plan\Referenzlauf\Referenzlauf.csproj -p:Configuration=Debug -p:Platform=x86

$exe = "C:\Waermeplan\WP_Plan\Referenzlauf\bin\x86\Debug\net8.0-windows\Referenzlauf.exe"

# 2. Eigene, vollstaendig migrierte Kopie ausserhalb des Repos (0 -> 6)
& $exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb C:\Waermeplan\Paket4a_Test\DB_Basis
& $exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb C:\Waermeplan\Paket4a_Test\DB_Basis --nokopie

# 3. Regression
foreach ($id in 1007,1008,1010,1011,1017,1018,1021,1023,1024) {
    & $exe projekt $id "C:\Waermeplan\Paket4a_Test\Lauf\Projekt_$id" C:\Waermeplan\Paket4a_Test\DB_Basis
}
& $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-14_Paket7 C:\Waermeplan\Paket4a_Test\Lauf
& $exe pruefen   C:\Waermeplan\Paket4a_Test\Lauf

# 4. Selbsttest und Registry-Probe: Konsolenprojekte mit Assembly-Referenz auf
#    bin\x86\Debug\net8.0-windows\WindowsFormsApplication1.dll (zwingend Debug),
#    Aufrufe Waermekanaele.Selbsttest() bzw. SimulationRunner.Simuliere(id, out fehler)
#    mit umgebogenem DB-Pfad (Settings.DBPath per Reflection) und DialogWaechter.
```

**Die produktive `Kenndaten.accdb` wird ausschließlich gelesen.** Alle Läufe dieser Etappe
liefen auf Kopien unter `C:\Waermeplan\Paket4a_Test\` (außerhalb des Repos):
`DB_Basis` (Regression), `DB_Flag` (Flag-Nachweis), `DB_Probe`/`DB_Probe2` (Schema- und
Registry-Proben).

---

# Etappe 4b — Zweikanalige Kaskade mit herausgelöster Ladephase

Etappe 4b füllt das Feature-Flag aus 4a mit Rechnung: Ist `Kaskade_Zweikanalig` am Projekt
gesetzt, läuft die Stundenschleife nach der Reihenfolge-Invariante A–G aus Konzept 6.3 auf
den beiden Bedarfskanälen (3.2), mit der aus der Kaskade **gelösten** Ladephase und der
Prioritätsauflösung nach 3.4/3.5. Ist es nicht gesetzt, rechnet der Altpfad — **unverändert
und wertgenau nachgewiesen**.

## 1. Umfang

### Geänderte Dateien

| Datei | Änderung |
|---|---|
| `Allgemein/Simulation/SimulationWaermebedarf.cs` | `Kanaele()` — Heiz- und WW-Kanal nach Konzept 3.2, dazu die Kappungszähler `Kanal_Kappungen` / `Kanal_Kappung_kWh`. Summenfelder und alle bestehenden Vektoren unberührt |
| `Allgemein/Simulation/SimulationKanaele.cs` | `Ladeauftrag` (eine Anlage lädt einen Speicher) und `Kaskadenkontext` (Registry, Entladeordnung je Kanal, vorsortierte Ladeordnung, Senke je Modul, Hinweiskanal) |
| `Allgemein/Simulation/Ladeordnung.cs` | `SortierenNachLadeprio(liste, prio)` — die Ordnungsregel 3.4 als EINE Implementierung für Anzeige und Engine; die bisherige private `Sortieren` ruft sie mit der gespeicherten Priorität auf |
| `Allgemein/Simulation/SimulationPufferspeicher.cs` | `IstQuelle`, `IstBrauchwasserkanal`, `Ladefaehigkeit(obergrenze)`, `HystereseFortschreiben()`, Instrumentierung `Abschluesse` |
| `Allgemein/Simulation/SimulationWaermepumpe.cs` | `ModuleAufbauen()` + `Berechnung_Stundenschleife()` (Altpfad, nur die Methodengrenze ist neu); NEU: `Vorbereiten_Zweikanalig()`, `QuellspeicherZusammenfuehren()`, `Berechnung_Zweikanalig()`, `Verfuegbar()`, `LadepotenzialBestimmen()`, `Entladephase()`/`EntladeKanal()`, `Ladephase()`, `Heizstabphase()`, `AbbruchAufraeumen()`, Lesezugriff `Betriebsmodi` |
| `Allgemein/Simulation/SimulationControl.cs` | Verzweigung in `Do_Simulation`; NEU: `Kaskade_Zweikanalig()`, `RestAufKanaeleZurueck()`, `Simulation_WP_Ctrl_Zweikanalig()`, `RegistryFuerZweikanaligOeffnen()`, `KontextAufbauen()`, `RegistrySpeicher()`, `EntladeordnungAufbauen()`, `LadeordnungAufbauen()`; `AlleSpeicher()` mit Registry zusammengeführt |

Keine neue Datei. Keine Designer- oder `.resx`-Datei angefasst; die gesperrten Dateien
(`WizardCtrl`, `WErzeugerModel`, `Form_BHKWEing`, `WizardParent`, `Form_Heizkessel*`,
`RecordSet`) sind unberührt.

## 2. Teil A — die beiden Kanäle (Konzept 3.2)

`SimulationWaermebedarf.Kanaele()` liefert ein neues `Waermekanaele`-Objekt:

```
Kanal BRAUCHWASSER [8760] = brauchwasserwerte
Kanal HEIZUNG      [8760] = Waermebedarf − brauchwasserwerte   (elementweise, ≥ 0)
```

Der Heizkanal ist bewusst das **Residuum nach dem Netzverlust-Aufschlag**: `NetzverlusteC`
addiert den konstanten Stundenbetrag auf `Waermebedarf`, und zwar erst **nach** der Addition
der Brauchwasserwerte. Das Residuum trägt die Netzverluste damit vollständig — die
Entscheidung O2 aus Konzept 3.2, und die einzige altverhaltenserhaltende Variante.

**Kappungsfälle**, beide dokumentiert und gezählt:

| Fall | Regel | Erwartung |
|---|---|---|
| negativer Brauchwasserwert | auf 0 gesetzt | tritt nicht auf; schützt den WW-Kanal vor einer „negativen Deckung" |
| Brauchwasser über Gesamtbedarf | Heizkanal 0, WW-Kanal auf den Gesamtbedarf begrenzt — die SUMME bleibt exakt der Gesamtbedarf | rechnerisch unmöglich (`Waermebedarf` enthält die Brauchwasserwerte, alle weiteren Summanden sind nichtnegativ); übrig bleibt der `float`-Rundungsfall |

Gemessen über alle neun Referenzprojekte: **0 Kappungen**, Probe `Heiz + WW == Waermebedarf`
mit einer maximalen Abweichung von 6,1·10⁻⁵ kWh (1011, reine `float`-Rundung).

> **Korrektur (Review-Nacharbeit 14.08.2026) — was die beiden Zahlen wirklich aussagen.**
>
> **Der Kappungszähler ist ein Zustand des letzten `Kanaele()`-Aufrufs**, kein Laufzähler:
> `Kanal_Kappungen` und `Kanal_Kappung_kWh` werden zu Beginn jedes Aufrufs auf 0 gesetzt.
> Die Engine ruft die Methode einmal je Lauf, die Probe ruft sie ein zweites Mal (mit
> demselben Eingangsvektor, also demselben Ergebnis). „0 Kappungen über alle neun
> Referenzprojekte" heißt deshalb genau: In keinem der neun Projekte lag ein
> Brauchwasserwert über dem Gesamtbedarf. Es heißt **nicht**, dass ein Zähler über mehrere
> Läufe hinweg mitgeführt würde.
>
> **Die Kanalprobe prüft die Arithmetik, nicht die Kaskade.** `Heiz + WW == Waermebedarf`
> ist die Konstruktionsregel von `Kanaele()` selbst (`heiz = gesamt − ww`) — die Probe kann
> also nur zeigen, dass die Rückaddition in `float` aufgeht, und genau das tut sie: Die
> Abweichung ist der Rundungsrest der Differenzbildung (≤ 1 ulp, dieselbe Familie wie bei
> `Uebernehmen`; gemessen 0 in 1008, 1,7·10⁻⁶ in 1007, 7,6·10⁻⁶ in 1023). Eine
> **unabhängige** Aussage über die Kanalführung liefert sie nicht — die liefern die
> Stundenbilanz der WP-Stufe und die Speicherbilanzen.

## 3. Teil B — die Stundenschleife A–G

Die Phasen stehen in `SimulationWaermepumpe.Berechnung_Zweikanalig` und laufen je Stunde in
genau dieser Reihenfolge:

| Phase | Umsetzung |
|---|---|
| **A Vorabentladung** | `Entladephase(vorab: true)` über `Kaskadenkontext.EntladenHeizung` / `…Brauchwasser`, sortiert nach `Ladeordnung.Entladereihenfolge` (3.6). Hysterese über `SimulationPufferspeicher.LaedtGerade` (`HystereseFortschreiben`), fortgeschrieben für JEDEN Speicher — auch ohne Bedarf. Abgezogen wird über `SenkeAbziehen` mit dem **Kanal des Puffers**, nicht mehr hart mit `SENKE_BEIDES` |
| **B Bedarfsdeckung** | Modulschleife in Anlagenpriorität; Deckung nur für Module mit Hauptsenke `HEIZKREIS`, `SenkeAbziehen(WS_Typ)` auf den echten Kanälen. Kennlinie, Betriebsarten, Sperrzeiten und Quellbegrenzung wie im Bestand |
| **C Speicherladung** | `Ladephase(zweitsenken: false)` über die **kaskadenübergreifend** vorsortierte Liste. Ladefähigkeit `Q_max · Obergrenze − SOC`, Obergrenze aus `Ladeordnung.ObergrenzenAufloesen`. `Speicher.Laden`, **kein `SenkeAbziehen`**. Quellentnahme auch beim Laden; die Quelle begrenzt die Ladung — **seit der Nacharbeit: BILANZRAUM statt Ladefähigkeit, siehe N.1** |
| **D Zweitsenken** | dieselbe Methode mit `zweitsenken: true`, aus dem verbleibenden `ladeRest` desselben Moduls — damit ist das PV-Budget sequenziell Haupt → Zweit (13.5, Variante A) ohne zweiten Parameter |
| **E Nachentladung** | `Entladephase(vorab: false)`, ohne Hysteresegatter — genau wie die heutige Entladung vor Heizstab und Folge-Erzeuger |
| **F Heizstab** | `Heizstabphase` auf den aggregierten Kanalrest mit der Modulgrenze `Tab_WP.Heizung`; Aufteilung auf die Kanäle über `SENKE_BEIDES` (Warmwasservorrang), Additionslogik aus B0-5 |
| **G Abschluss** | `StundeAbschliessen()` **genau einmal** je Registry-Speicher, Quellspeicher eingeschlossen; davor die Abschaltprüfung der Hysterese (vor den Bereitschaftsverlusten, wie im Altpfad) |

**Zeitabhängige Ladepriorität (3.5).** Die Ladeordnung wird je Lauf **zweimal** vorsortiert —
einmal mit der gespeicherten Ladepriorität, einmal mit `Ladeordnung.WirksameLadeprioPV`.
Je Stunde wird die passende Liste gewählt; Kriterium ist der PV-Überschuss der Stunde aus
`PV_Ueberschuss_stuendlich` **vor** seinem Verbrauch. Das spart 8760 Sortierungen und liefert
dasselbe Ergebnis.

**Die Doppelzählungsfalle (6.3)** ist strukturell ausgeschlossen: Eine Anlage hat genau eine
Hauptsenke und ist damit eindeutig in Phase B **oder** in Phase C. Phase C ruft kein
`SenkeAbziehen`. Der Nachweis steht in Teil F.4.

## 4. Teil C — WP-Modul: eigene Methodenvariante statt Umbau in-place

**Abgewogen und begründet** (der Kommentarblock steht so auch im Quelltext):

- **Umbau in-place** hätte einen Rechenweg erhalten, aber rund zwanzig Verzweigungen in den
  Bestandscode getragen (Senke je Modul, Ladefähigkeit statt Bedarf, Entladen nach Kanal,
  `StundeAbschliessen` zentral). Der Altpfad wäre danach nicht mehr durch **Lesen** als
  unverändert nachweisbar gewesen, sondern nur noch durch Messen. Bei einem Feature-Flag,
  dessen einziger Zweck die Rückfallebene ist, ist das die falsche Reihenfolge.
- **Eigene Methode** kostet eine zweite Stundenschleife (~200 Zeilen). Geteilt statt kopiert
  werden Modulaufbau (`ModuleAufbauen`), Kennlinienauswertung (`berechne_wptherm`) und
  `SenkeAbziehen`; gedoppelt ist ausschließlich die Ablaufsteuerung — und genau die ist in
  beiden Fassungen verschieden.

Damit `Berechnung()` und `Berechnung_Zweikanalig()` denselben Modulaufbau benutzen, ist der
Aufbaublock in `ModuleAufbauen()` und der Rest in `Berechnung_Stundenschleife()` gewandert.
**Die ausgeführten Anweisungen und ihre Reihenfolge sind unverändert** — der Diff des Altpfads
besteht aus genau fünf entfernten Zeilen (die Deklarationen von `rs`, `wp` und `biv`, die
mitgewandert sind); die Stundenschleife selbst ist zeichengleich. Die Regression (F.2) belegt
es wertgenau.

**Die geforderten WP-Anpassungen im neuen Pfad:**

| Punkt (6.3) | Umsetzung |
|---|---|
| Senke je Modul aus `WaermesenkeClass.SenkenLaden` | `Kaskadenkontext.SenkeJeModul`, indexgleich mit der Modulliste; fehlt eine Zeile, gilt die Vorbelegung Heizkreis/Beides |
| `verfuegbar` dritter Fall | `Verfuegbar()` — WW-Kanal, Heizkanal (bzw. beides) und, neu, `Ladefaehigkeit(Obergrenze)` der Hauptsenke. **Seit der Nacharbeit: `Bilanzraum(Obergrenze, offener Kanalbedarf)` — N.1** |
| Abbruch „kein Bedarf **und** kein Ladepotenzial" | `if (verfuegbar <= 0 && !kannLaden) continue;` — ein Modul mit Zweitsenke läuft damit auch in Stunden ohne Bedarf |
| Alternativbetrieb und Bivalenzpunkt gegen die kanalgerechte Bezugsgröße | Alternativbetrieb vergleicht gegen `verfuegbar` statt gegen den aggregierten `Rest_waerme`; der Bivalenzpunkt wird nach Phase E auf den Kanalrest gezogen — dieselbe Stelle im Ablauf wie heute. **Seit der Nacharbeit vergleicht der Alternativbetrieb gegen den offenen KANALBEDARF (`AlternativBezug`), nicht gegen den Bilanzraum — N.2** |
| `SenkeAbziehen` bei Speicherentladung nach **Verwendung des Puffers** | `EntladeKanal` übergibt `SENKE_WARMWASSER` bzw. `SENKE_HEIZUNG`; ein Brauchwasserspeicher kann keinen Heizbedarf mehr decken |
| Hysterese über `LaedtGerade` statt `_speicherLaden` | `HystereseFortschreiben()` am Speicher; das modulübergreifende Feld `_speicherLaden` wird im neuen Pfad nicht mehr gelesen (im Altpfad bleibt es unverändert) |

## 5. Teil D — Registry, geteilte Quellspeicher-Instanz, `AlleSpeicher()`

**Alle Registry-Speicher rechnen** (offener Punkt 5 aus 4a): `RegistryFuerZweikanaligOeffnen()`
setzt `ImRechenpfad` auf allen Einträgen und zieht `Schwelle_Aus_Nachrang` und `Entladeprio`
aus der Projektkopie nach — die Alt-Zuordnung `Z_ProjektPufferSp` kennt diese beiden Spalten
nicht. Ein-/Abschaltschwelle bleiben ausdrücklich, wie die Registry sie aufgebaut hat.
Das **Feld** `ImRechenpfad` bleibt bestehen, weil der Altpfad es weiter braucht; im neuen Weg
tragen es alle, damit ist seine Sonderrolle gegenstandslos, ohne die Rückfallebene anzutasten.

**Geteilte Instanz am Quellpuffer** (offener Punkt 4 aus 4a): `QuellspeicherZusammenfuehren()`
führt mehrfach benutzte Quellspeicher auf die Instanz des ersten Moduls zusammen; damit laufen
beide Module gegen einen Füllstand, und Phase G zieht die Bereitschaftsverluste genau einmal.
Der Fall wird protokolliert. In der Referenzmenge tritt er nicht auf (nur 1021 hat überhaupt
einen Quellspeicher, genutzt von genau einem Modul); belegt ist die Zusammenführung deshalb am
präparierten Szenario in F.6.

**`AlleSpeicher()` zusammengeführt** (offener Punkt 6 aus 4a): Mit gesetztem Flag liefert die
Methode `RegistrySpeicher()` — dieselben Objekte, die die Stundenschleife bewegt hat. Ergebnis,
Navigator und CSV speisen sich damit aus einer Quelle. **Ohne Flag ist die Methode Zeile für
Zeile die alte**; das Paket-7-Verhalten bleibt unangetastet.

## 6. Teil E — Verifikation

### F.1 Build

```
MSBuild ..\WP-Plan.sln -p:Configuration=Debug -p:Platform=x86   →  0 Fehler
```

Warnungen: **dieselben sechs Bestandswarnungen** wie vor der Änderung (`WErzeugerModel.cs`
CS0108, `StromverbraucherStammCtrl.cs` CS0108, `KlimaregionStammCtrl.cs` 2 × CS0109,
`MDIMainForm.cs` CS4014 und CS1998) — **keine neue**, geprüft über einen vollständigen
`-t:Rebuild`.

> **Die Messwerte dieses Kapitels beschreiben den Stand VOR der Review-Nacharbeit vom
> 14.08.2026.** Sie bleiben als Beleg der damaligen Aussagen stehen — insbesondere für
> Befund 4b-1, der sich genau an ihnen zeigte. **Maßgeblich sind die Zahlen im Kapitel
> „Review-Nacharbeit"** am Ende dieses Protokolls; wo sich eine Aussage inhaltlich geändert
> hat, steht die Korrektur an Ort und Stelle.

### F.2 Flag AUS — Regression (Pflicht)

Neun Referenzprojekte auf einer eigenen Kopie (`C:\Waermeplan\Paket4b_Test\DB_Basis`,
identisch mit der abgenommenen 4a-Kopie), verglichen gegen `Referenzlaeufe/2026-08-14_Paket7`:

```
Projekt_1007: PASS (29 Dateien, 324210 Werte)
Projekt_1008: PASS (21 Dateien, 227847 Werte)
Projekt_1010: PASS (18 Dateien, 201540 Werte)
Projekt_1011: PASS (29 Dateien, 324232 Werte)
Projekt_1017: PASS (20 Dateien, 245378 Werte)
Projekt_1018: PASS (19 Dateien, 210343 Werte)
Projekt_1021: FAIL (21 Dateien, 227840 Werte, 1 Abweichungen)
    aggregate.csv [Pufferspeicher[0].ID_Pufferspeicher]: ref=8 neu=1018014
Projekt_1023: PASS (25 Dateien, 262917 Werte)
Projekt_1024: PASS (22 Dateien, 236616 Werte)
```

**2.260.923 verglichene Werte, genau eine Abweichung** — die in Etappe 4a dokumentierte
ID-Semantik des Quellspeichers. Die Aufteilung von `Berechnung()` in zwei Methoden ist damit
wertgenau als neutral belegt.

### F.3 Flag AN — Äquivalenzklasse „nur Heizkreis"

Geprüfte Konfiguration (`Tab_Energieanlagen.WS_Ziel` je Projekt): **1010, 1017, 1018, 1021,
1024 und zusätzlich 1007 und 1011** haben ausschließlich `Heizkreis`-Senken; 1008 und 1023
tragen `PufferHeizung` und gehören damit nicht in diese Klasse. Verglichen wurde je Projekt
**derselbe Code mit Flag aus gegen Flag an**:

| Projekt | Konfiguration | max. Abweichung | erklärt? |
|---|---|---|---|
| 1010 | 1 WP, kein Puffer | **0** (bitgleich) | — |
| 1017 | BHKW + Kessel, kein Puffer | **0** (bitgleich) | — |
| 1018 | 2 BHKW + Kessel, kein Puffer | **0** (bitgleich) | — |
| 1021 | 2 WP, Quellspeicher, kein Senkenpuffer | **0** (bitgleich) | — |
| 1024 | 2 WP + Kessel + BHKW | 1,6·10⁻⁵ kWh je Stundenwert, 0,001 kWh auf der Jahressumme (1,4·10⁻⁴ relativ) | ja — `float`-Rundung durch die Kanalführung (Konzept Kapitel 9) |
| 1007 | WP + Kessel + Solar + PV, **Puffer aus Solar-Zuordnung** | Ganglinien ≤ 2·10⁻⁶ kWh; zusätzlich `Kapazitaet_Pufferspeicher` 0 → 6,96 und 21 neue Kennzahlen | ja — der Projekt-Puffer rechnet ab 4b mit (Teil 7, Änderung 2) |
| 1011 | 3 WP + 2 Kessel + Solar + PV, **Puffer aus Gesamtsystem-Zuordnung** | Ganglinien ≤ 1,3·10⁻⁴ kWh; `wp_warmwasserbedarf` −159,6 kWh (4059,70 → 3900,09) | ja — beides erklärt (Teil 7, Änderungen 1 und 2) |

**Die Zusage aus Kapitel 9 ist eingehalten:** Alle Energie- und Stromganglinien stimmen auf
zwei Nachkommastellen überein; die größte Abweichung eines Stundenwerts liegt bei
1,6·10⁻⁵ kWh. Vier von sieben Projekten sind bitgleich.

Die beiden erklärten Abweichungen im Einzelnen:

1. **`wp_warmwasserbedarf` in 1011.** Im Altpfad bekommt das WP-Modul den vollen
   `brauchwasserwerte`-Vektor, unabhängig davon, was vorgelagerte Erzeuger schon gedeckt
   haben. Im zweikanaligen Weg bekommt es den **WW-Kanal an seiner Kaskadenposition**. 1011
   rechnet Solarthermie auf Position 2 und die Wärmepumpe auf Position 4; die Solarthermie
   produziert 515,81 kWh, wovon `Uebernehmen()` 159,61 kWh anteilig dem WW-Kanal zuordnet.
   4059,70 − 159,61 = 3900,09 — genau die gemessene Differenz. Das ist der in Kapitel 9
   ausdrücklich angekündigte Fall „WW-Deckung, wenn die WP nicht an erster Kaskadenposition
   steht (heute überschätzt)". Gegenprobe 1007: dort produziert die Solarthermie 0 kWh, und
   der Vektor `wp_warmwasserbedarf` bleibt **byte-genau** gleich.

   > **Präzisierung (Review-Nacharbeit):** „exakt gleich" gilt für **diesen Vektor**, nicht
   > für das ganze Projekt 1007. Dort bleiben die drei Restwärme-/Heizstab-Summen in den
   > letzten `float`-Stellen verschieden (`Sim.Restwaerme` 6,04914188 gegen 6,04914236 —
   > 8·10⁻⁹ relativ), weil die Kanalführung in `float` rechnet statt einen Summenvektor zu
   > differenzieren. Das ist die in Kapitel 9 angekündigte Rundung und liegt vier
   > Größenordnungen unter der Vergleichstoleranz.
2. **Projekt-Puffer ohne Erzeugersenke.** 1007 und 1011 tragen je einen Projekt-Puffer aus
   einer Alt-Zuordnung (Solarthermie bzw. „Gesamtsystem"). Ab 4b steht er in der Registry und
   im Rechenpfad: Er erscheint in `Tab_ErgebnisPufferspeicher`, liefert `puffer_wp` und damit
   `Kapazitaet_Pufferspeicher`. **Gerechnet hat er nichts** — Ladung, Entladung, Verluste und
   SOC sind über das ganze Jahr 0, weil ihn in 4b niemand lädt. Die Wärmebilanz ist unberührt.

### F.4 Flag AN — Puffer-Szenarien 1008 und 1023

Beide Projekte tragen `WS_Ziel = PufferHeizung` an ihren Wärmepumpen (Migration aus
`Z_ProjektPufferSp`). Gemessen mit einer eigenen headless-Probe auf `DB_Flag`:

**Energieerhaltung je Stunde** — geprüft wird über alle 8760 Stunden
`Eingang − Rest == Produktion + Heizstab + Entladung − Ladung`:

| Projekt | max. Abweichung | Summe der Beträge über das Jahr |
|---|---|---|
| 1008 | 9,5·10⁻⁷ kWh (Stunde 7158) | 0,0016 kWh |
| 1023 | 1,5·10⁻⁵ kWh (Stunde 404) | 0,0065 kWh |

**Jahresbilanz der Speicher** — `Ladung − Entladung − Verluste == ΔSOC`:

| Speicher | Q_max | Ladung | Entladung | Verluste | SOC Ende | Bilanzfehler |
|---|---|---|---|---|---|---|
| 1008 · `1008007` | 6,9600 | 37.338,918 | 37.064,876 | 274,042 | 0,0000 | 1,9·10⁻⁹ |
| 1008 · `1008008` | 9,0248 | 0 | 0 | 0 | 0 | 0 |
| 1023 · `1018023` | 13,9200 | 71.017,940 | 70.719,997 | 297,943 | 0,0000 | 1,2·10⁻⁸ |

**Deckungsgrad OHNE Kappung** (Pflichttest 6.3), restbedarfsbasiert — die Größe, die
`BaueErgebnis` und die Detailansicht persistieren:

| Projekt | Deckung restbedarfsbasiert | Deckung produktionsbasiert |
|---|---|---|
| 1008 | **37,7203 %** | 37,9992 % |
| 1023 | **42,1313 %** | 42,1313 % / prod. 18,2224 % |

Beide liegen ohne jede Kappung unter 100 %. Die Kappung `if (deckung > 100) deckung = 100`
verdeckt in 4b nichts.

> **Korrektur (Review-Nacharbeit) — dieser Test allein trägt die Aussage nicht.**
>
> Die persistierte, **restbedarfsbasierte** Deckung ist
> `(Eingang − Restbedarf) / Gesamtbedarf`. Der Restbedarf wird in jeder Stunde bei 0
> geklemmt, also ist der Zähler nie größer als der Nenner: **Diese Größe KANN
> konstruktionsbedingt nie über 100 % steigen** — auch dann nicht, wenn dieselbe kWh doppelt
> gezählt würde. Sie ist damit kein Nachweis der Doppelzählungsfreiheit, sondern nur die
> Bestätigung, dass die 100-%-Kappung im Ergebnis nicht greift.
>
> **Tragend sind zwei andere Proben**, und beide sind erfüllt:
>
> 1. die **Stundenbilanz** `Eingang − Rest == Produktion + Heizstab + Entladung − Ladung`
>    über alle 8760 Stunden — sie zählt Produktion und Speicherbewegung getrennt und würde
>    jede doppelt gebuchte kWh als Bilanzfehler zeigen;
> 2. die **produktionsbasierte Gegenprobe** `Produktion / Gesamtbedarf`, die sehr wohl über
>    100 % gehen kann. Sie tut es im präparierten Quellspeicher-Szenario (F.6: 106,4456 %)
>    und ist dort vollständig durch Speicherverluste und Restinhalt erklärt.

**`StundeAbschliessen`-Zählung:** instrumentiert über `SimulationPufferspeicher.Abschluesse` —
**genau 8760** je Speicher in allen geprüften Läufen, Senken- wie Quellspeicher.

**Vergleich neu gegen alt** (dieselbe Datenbank, nur das Flag unterscheidet sich):

| Größe [kWh bzw. MWh] | 1008 alt | 1008 neu | 1023 alt | 1023 neu |
|---|---|---|---|---|
| WP-Wärmeproduktion | 81.125,9 | **37.338,9** | 138.151,1 | **71.017,9** |
| WP-Strom | 18.990,5 | 7.892,0 | 53.739,3 | 27.706,1 |
| Heizstab | 0 | 0 | 62.223,2 | **93.478,2** |
| Speicherladung | 22.260,3 | 37.338,9 | 13.244,5 | 71.017,9 |
| Speicherentladung | 21.837,9 | 37.064,9 | 12.997,3 | 70.720,0 |
| Speicherverluste | 415,6 | 274,0 | 247,2 | 297,9 |
| Restwärme gesamt (MWh) | 17,56 | **61,20** | 125,28 | **149,22** |
| Kesselproduktion (MWh) | — | — | 64,32 | 76,31 |
| WP-Deckung (persistiert, %) | 82,13 | 37,72 | 51,35 | 42,13 |

**Erklärung der Differenz.** Sie ist keine Rundung, sondern die Semantik der Puffer-Senke:

- Im **Altpfad** deckt die Wärmepumpe den Momentanbedarf **direkt** und lädt nur ihren
  Überschuss in den Speicher. Der Speicher ist ein Zusatz.
- Im **neuen Pfad** ist eine Anlage mit `WS_Ziel = PufferHeizung` ausschließlich Lader
  (Phase C); der Bedarf wird ausschließlich aus dem Speicher gedeckt (Phasen A und E). Damit
  begrenzt die **nutzbare Speicherkapazität den Stundendurchsatz** der Wärmepumpe: 1008 hat
  6,96 kWh, 1023 hat 13,92 kWh nutzbaren Inhalt. Genau das zeigen die Zahlen — 1023 lädt
  71.017,9 kWh in 5102 Vollzyklen, also im Mittel knapp ein Zyklus je Betriebsstunde.

  > **Korrektur (Review-Nacharbeit) zur Rolle von `SOC_Max`.** Der Satz „SOC_Max erreicht
  > mit 13,14 kWh die Abschaltschwelle 0,95 · Q_max" stand hier als Beleg für die
  > Drosselung — das ist er nicht. `SOC_Max = Q_max · Obergrenze` ist die **Ladeobergrenze
  > aus Konzept 3.4** und stellt sich immer ein, sobald der Speicher einmal vollgeladen
  > wird; sie sagt über den DURCHSATZ nichts aus. Der Durchsatz steht in den
  > **Vollzyklen**: 5102 Zyklen in 8760 Stunden hießen damals „höchstens ein Speicherinhalt
  > je Stunde" — das war die Drosselung. Nach der Umstellung auf den Bilanzraum stehen dort
  > 7902 (1023) bzw. 11.378 Zyklen (1008), also **mehr als ein Inhalt je Stunde**, während
  > `SOC_Max` unverändert bei 0,95 · Q_max liegt. Genau diese Trennung — Zielfüllstand
  > gegen Stundendurchsatz — ist der Kern der Nutzerentscheidung zu 4b-1.
- Die fehlende Wärme übernehmen die nachgelagerten Erzeuger: in 1023 der Heizstab
  (+31,3 MWh) und der Kessel (+12,0 MWh), in 1008 bleibt sie als Restwärme stehen (das
  Projekt hat den Kessel auf keiner Kaskadenposition).

Das ist die konzepttreue Umsetzung von 6.3 — **und zugleich ein Befund für die Review**,
siehe Teil 9, Befund 4b-1.

### F.5 Präpariertes Szenario — Hauptsenke Brauchwasser, Zweitsenke Heizung

Auf einer eigenen Kopie (`DB_Szenario`) präpariert, Projekt 1023 (60.000 kWh/a
Brauchwasserbedarf): Puffer `1018024` auf `Verwendung = Brauchwasser`, 60/45 °C; Anlage 11203
auf `WS_Ziel = PufferBrauchwasser` (Puffer 1018024) **und** `WS_Ziel2 = PufferHeizung`
(Puffer 1018023). Anlage 11204 bleibt auf `PufferHeizung`.

```
PUFFER_1018023  Heizung       Q_max 13,9200  Ladung 66.673,44  Entladung 66.375,62  Verluste 297,82  SOC_Ende  0,0000
PUFFER_1018024  Brauchwasser  Q_max 13,5372  Ladung 50.766,04  Entladung 50.135,14  Verluste 618,14  SOC_Ende 12,7653
Modul 11203 (Haupt BW + Zweit Heizung): Wärme 61.700,27
Modul 11204 (Haupt Heizung):            Wärme 55.739,21
```

Die Rechnung geht **exakt** auf:

- 61.700,27 − 50.766,04 = **10.934,23** kWh gehen als Überschuss in die Zweitsenke,
- 55.739,21 + 10.934,23 = **66.673,44** kWh = Ladung des Heizungspuffers. Kein kWh doppelt.
- Der WW-Kanal wird aus dem Brauchwasserspeicher gedeckt (50.135,14 kWh von 60.000 kWh
  Jahresbedarf); der Heizkanal berührt ihn nicht.
- Stundenbilanz max. 1,5·10⁻⁵ kWh, Speicherbilanzen 9,3·10⁻⁹ und 3,9·10⁻⁹ kWh,
  `StundeAbschliessen` 8760/8760, Deckungsgrad ohne Kappung **48,2001 %**.

Damit ist die Reihenfolge aus 13.5 („Hauptsenke bis zu ihrer Ladeobergrenze zuerst, erst der
Rest an die Zweitsenke") am Zahlenbeispiel belegt.

### F.6 Quellspeicher (1021) mit Flag an

**Referenzstand 1021, Flag an gegen Flag aus: bitgleich** (siehe F.3). Die Quellbilanz:

```
QUELLE_10361  ID 1018014  Q_max 4,5124  Ladung 0  Entladung 4,2637  Verluste 0,2487
              SOC_Ende 0,0000  Bilanzfehler 8,9·10⁻¹⁶  StundeAbschliessen-Aufrufe 8760
```

Da 1021 keine Puffer-**Senke** hat, kann es die neue Quellentnahme beim Laden nicht zeigen.
Dafür ein präpariertes Szenario auf `DB_Szenario`: Anlage 10361 bekommt
`WS_Ziel = PufferHeizung` auf den Projekt-Puffer 1018013 (13,92 kWh) und
`WQ_Regeneration = 8` kW an ihrem Quellspeicher.

```
PUFFER_1018013 Heizung Q_max 13,9200 Ladung 12.238,958 Entladung 11.497,679 Verluste 728,138 SOC_Ende 13,1409
QUELLE_10361   Quelle  Q_max  4,5124 Ladung  8.036,547 Entladung  7.323,714 Verluste 713,698 SOC_Ende  3,6477
Modul 10361: Wärme 12.238,958  Strom 4.915,244      Modul 10360: Wärme 2,378  Strom 0,984
```

- **Quellentnahme auch beim Laden:** Modul 10361 erzeugt seine gesamte Wärme als LADUNG.
  Verdampferwärme = 12.238,958 − 4.915,244 = **7.323,714 kWh** = Entladung des Quellspeichers,
  auf sechs Stellen genau. Im Altpfad entnimmt die Speicherladung der Quelle **nichts**
  (Prüfbefund 6.3); jetzt tut sie es, und die Quelle begrenzt die Ladung.
- **Speicherbilanzen:** 6,9·10⁻¹⁰ (Senke) und 1,7·10⁻⁹ kWh (Quelle).
- **`StundeAbschliessen` 8760/8760** für beide Speicher — instrumentiert nachgewiesen.
- **Energieerhaltung der Stunde:** max. 4,8·10⁻⁷ kWh.
- **Deckungsgrad ohne Kappung: 100,0000 %** (restbedarfsbasiert). Produktionsbasiert stehen
  106,4459 % — und genau das ist die Probe auf die Doppelzählungsfrage: Produktion
  12.241,336 = Bedarf 11.500,057 + Speicherverluste 728,138 + Restinhalt 13,141. Die
  Überdeckung ist vollständig durch Verluste und Speicherinhalt erklärt, nicht durch doppelt
  gezählte kWh. Die persistierte, restbedarfsbasierte Größe bleibt bei exakt 100 %.

### F.7 PV-Ladebudget und zeitabhängige Priorität

In der Referenzmenge kombiniert **kein** Projekt `BM_Typ = PV` mit einer Puffer-Senke; der
Pfad wurde deshalb auf `DB_Szenario` präpariert (Projekt 1007: Anlage 10353 auf
`BM_Typ = PV`, `WS_Ziel = PufferHeizung` auf Puffer 1007007, `WS_Ladeprio_PV = 5`).

```
PUFFER_1007007  Ladung 3.071,893  Entladung 2.852,755  Verluste 219,137  SOC_Ende 0  Bilanzfehler 8,1·10⁻¹¹
Modul 10353: Wärme 3.071,893  Strom 494,830  (JAZ 6,21, Laufzeit 873 h)
Stundenbilanz max. 1,9·10⁻⁶ kWh, StundeAbschliessen 8760
```

Die Wärmepumpe lädt **nur** in Stunden mit PV-Überschuss und nur bis zu dessen Budget
(sichtbar an der hohen JAZ: sie läuft ausschließlich in Stunden mit PV-Ertrag, also bei
höheren Außentemperaturen). Die PV-Liste `LadenMitPV` wird in genau diesen Stunden benutzt.
Eine echte **Umsortierung** durch `WS_Ladeprio_PV` lässt sich damit noch nicht zeigen — dafür
braucht es zwei konkurrierende Lader an einem Puffer und eine Solarthermie mit Senke, also
Paket 5. Der Testfall gehört in die Abnahme (Paket 10).

### F.8 Kodierung und Diff

Jede geänderte Datei behält ihre Kodierung und ihre Zeilenenden:

```
SimulationControl.cs         BOM  CRLF          SimulationPufferspeicher.cs  kein BOM  LF
SimulationWaermepumpe.cs     BOM  CRLF          Ladeordnung.cs               kein BOM  LF
SimulationWaermebedarf.cs    BOM  CRLF
SimulationKanaele.cs         BOM  CRLF
```

`git diff --check` meldet für die in 4b geänderten Dateien nichts. Der Diff des WP-Altpfads
besteht aus **fünf entfernten Zeilen** (mitgewanderte Deklarationen); die einkanalige
Stundenschleife und die einkanalige Kaskadenschleife in `SimulationControl` sind
zeichengleich. Keine Designer- und keine `.resx`-Datei angefasst, gesperrte Dateien unberührt.
`Referenzlauf.exe pruefen` meldet für alle neun Projekte mit Flag an „plausibel".

## 7. Dokumentierte Ergebnisänderungen mit Flag AN

| # | Änderung | Wirkung | Grundlage |
|---|---|---|---|
| 1 | `Warmwasserbedarf_stuendlich` ist der WW-Kanal an der Kaskadenposition der WP, nicht mehr der volle Brauchwasservektor | 1011: −159,6 kWh; Projekte mit WP an Position 1 unverändert | Kapitel 9 („WW-Deckung … heute überschätzt") |
| 2 | ~~Alle Registry-Speicher rechnen mit — auch solche ohne ladenden Erzeuger~~ **ENTFÄLLT mit der Nacharbeit (N.6):** Es rechnen nur Speicher mit Senken- oder Quellreferenz | vorher 1007/1011: `Kapazitaet_Pufferspeicher` 0 → 6,96, je 21 neue Kennzahlen, drei neue CSV-Ganglinien. **Jetzt: keine Änderung**, 1007 ist wieder PASS | 6.7 |
| 3 | Eine Anlage mit Puffer-Hauptsenke deckt **keinen** Bedarf mehr direkt, sondern lädt — gedeckt wird aus dem Speicher | 1008: WP-Produktion 81,13 → 79,19 MWh; 1023: 138,15 → 109,99 MWh, davon 28,16 MWh der Warmwasserkanal, den ein **Heizungs**puffer nicht bedienen kann (N.1). Vor der Nacharbeit: −54 % / −49 % | 3.1, 6.3 (Phasen B/C) |
| 3a | **Die Speicherladung ist nicht mehr auf den Speicherinhalt gedrosselt** (Bilanzraum, Nutzerentscheidung zu 4b-1) | Vollzyklen 1008: 3.198 → 11.378, 1023: 951 → 7.902; der Füllstand liegt innerhalb der Stunde über `Q_max` und ist nach Phase E wieder darunter | 3.4/6.3, Nachträge |
| 3b | **Die Regeneration der Quellspeicher ist aus der Modulschleife in den Stundenkopf gewandert** und wird genau einmal je Speicher und Stunde gutgeschrieben | Im Altpfad läuft sie je Modul innerhalb der Schleife — bei zwei Modulen an einem Quellspeicher (nach der Zusammenführung dieselbe Instanz) wäre sie doppelt gutgeschrieben worden. In der Referenzmenge tritt der Fall nicht auf (1021 hat genau ein Modul am Quellspeicher, Ergebnis bitgleich); mit mehreren Modulen ändert sich die Quellbilanz | 6.3 (Phase G, „genau einmal") |
| 4 | Die Speicherladung entnimmt der Wärmequelle jetzt Wärme | nur Projekte mit Puffer-Quelle **und** Puffer-Senke (in der Referenzmenge keines) | 6.3, Prüfbefund |
| 5 | `StundeAbschliessen` genau einmal je Speicher und Stunde | nur bei mehreren Modulen am selben Quellspeicher (in der Referenzmenge keines) | 6.3 |
| 6 | Die Speicherladung wird jetzt auch **je Modul** gebucht (`Modul_WP_Waermeproduktion`, `…_Strombedarf`, `…_Laufzeit`) | Modulsummen der Ergebnistabelle stimmen mit den Gesamtwerten überein; im Altpfad taten sie das bei Speicherladung nicht | Konsistenz der Ergebnispersistenz |
| 7 | Rundung: Kanalführung in `float` statt Differenzbildung aus dem Summenvektor | ≤ 1,6·10⁻⁵ kWh je Stundenwert | Kapitel 9 |

## 8. Bewusste Abgrenzungen

| # | Abgrenzung | Begründung |
|---|---|---|
| 1 | **Kessel, BHKW und Solarthermie rechnen einkanalig** auf `Waermekanaele.Summe()`; ihr Rest wird über `Uebernehmen()` proportional zurückverteilt | Kompatibilitätsanker 6.1; ihre Zweikanaligkeit und ihre Senkenauswertung sind Paket 5 und 6 |
| 2 | **BHKW-Anlagen mit migriertem `WS_Ziel = PufferHeizung` ruhen**: Sie laden nicht, sondern rechnen wie Heizkreis-Anlagen | Ihre Speicherlogik steckt in drei Fahrweisen-Implementierungen (6.5) — der Umbau ist Paket 6. Der Fall wird beim Kontextaufbau **protokolliert** („Etappe 4b wertet nur Wärmepumpen-Senken aus") |
| 3 | Die Phasen A, C, D, E und G laufen an der **Kaskadenposition der Wärmepumpe**, nicht vor der gesamten Kaskade | Solange nur die WP Speicher bedient, ist das identisch — kein anderer Erzeuger lädt oder entlädt. Erst mit Paket 5/6 muss die Stundenschleife über alle Erzeuger geführt werden; dafür müssen deren Module stundenweise aufrufbar werden |
| 4 | Ein Projekt **ohne** Wärmepumpe öffnet die Registry nicht; seine Speicher rechnen auch mit Flag an nicht | In 4b gäbe es niemanden, der sie lädt. `AlleSpeicher()` bleibt leer, das Ergebnis entspricht dem Altpfad |
| 5 | `MODUS_LEISTUNG` bleibt bei Puffer-Hauptsenke wirkungslos (wie `MODUS_LAUFZEIT`) | Bei einer Speicher-Hauptsenke IST die Ladung der Auftrag, nicht der Überschuss über einen Bedarf hinaus. „Auf den Bedarf modulieren" hätte die Anlage stillgelegt; die Ladefähigkeit begrenzt ohnehin |
| 6 | `ImRechenpfad` bleibt als Feld erhalten | Der Altpfad braucht es weiter (4a, Entscheidung 1). Im neuen Weg tragen es alle Einträge |
| 7 | `_speicherLaden` in `SimulationWaermepumpe` bleibt stehen | Es ist der Hysteresezustand des Altpfads. Entfernt wird es, wenn der einkanalige Weg entfällt |

## 9. Offene Punkte und Befunde

| # | Punkt | Bewertung |
|---|---|---|
> **Stand nach der Review-Nacharbeit vom 14.08.2026:** 4b-1 ist **entschieden und
> umgesetzt** (Bilanzraum, siehe unten), 4b-2 löst sich damit, 4b-3 ist um die
> zeitabhängige Auflösung der Ladeobergrenzen ergänzt, 4b-5 hat eine Parallelsitzung
> behoben. Offen bleibt 4b-4. Die Tabelle bleibt als Befundlage der Review stehen.

| **4b-1** | **Die nutzbare Speicherkapazität begrenzt den Stundendurchsatz einer Anlage mit Puffer-Hauptsenke.** Ladefähigkeit ist `Q_max · Obergrenze − SOC`; die Entladung derselben Stunde (Phase E) kommt erst danach. Ein 600-l-Speicher mit 20 K Spreizung lässt so höchstens ~13 kWh/h durch, während der Momentanbedarf ein Vielfaches betragen kann (1023: WP-Produktion halbiert, Heizstab +31,3 MWh) | Die Umsetzung folgt 6.3 wörtlich. Fachlich ist ein Pufferspeicher aber eine **hydraulische Weiche**: Er wird geladen, während die Last aus ihm entnimmt — der Durchsatz ist nicht auf den Inhalt begrenzt. Vor der Freigabe des Flags für Puffer-Projekte ist zu entscheiden, ob die Ladefähigkeit die im selben Zeitschritt absehbare Entnahme berücksichtigen soll (oder ob ein Bypass „Direktdeckung bis zum Bedarf" ins Konzept gehört). **Konzeptfrage, keine Implementierungsfrage** |
| **4b-2** | Zwei Wärmepumpen an DEMSELBEN Puffer: Die vorrangige füllt ihn allein, die zweite bleibt aus (1008: Modul 10133 mit 0 kWh) | Direkte Folge von 4b-1 und der Ladeordnung. Löst sich mit 4b-1 |
| **4b-3** | Umsortierung durch `WS_Ladeprio_PV` (3.5) ist implementiert und über die zwei vorsortierten Listen nachvollziehbar, aber noch nicht an einem Lauf mit zwei konkurrierenden Ladern gezeigt | Braucht die Solarthermie-Senke aus Paket 5. Testfall für die Abnahme (Paket 10) |
| **4b-4** | Zwei Puffer im selben Kanal (Entladereihenfolge 3.6) sind implementiert, aber nur mit einem Puffer je Kanal getestet | Die Reihenfolge kommt aus `Ladeordnung.Entladereihenfolge` und ist dort geprüft. Abnahmefall aus Kapitel 9, Paket 10 |
| **4b-5** | `SimulationControl.Simulation_BHKW_Ctrl` leert `bhkw_list`, aber nicht `bhkw_list_Namen` (Nebenbefund aus 4a) | unverändert offen — Kandidat für B0 oder Paket 6 |

## 10. Folgearbeit (nach der Review)

> **Referenzbasis — erledigt mit der Nacharbeit.** `Referenzlaeufe/2026-08-14_Paket4` ist
> seit dem 14.08.2026 die gültige Basis (neun Projekte, **Flag AUS**); `LIESMICH.md` ist
> umgestellt, der Selbstvergleich meldet PASS. Eingefroren sind die drei bekannten
> 1021-Abweichungen (ID-Semantik des Quellspeichers, zwei B0-13-Folgegrößen) — siehe N.14.
>
> Der **Basiswechsel des Flags** (Default „an", projektweise Umstellung der Bestandsprojekte)
> steht weiterhin aus. Die Entscheidung zu Befund 4b-1 ist getroffen und umgesetzt (N.1);
> vor der Umstellung eines Bestandsprojekts ist zu prüfen, ob sein Warmwasserkanal einen
> Speicherweg hat (N.15, Punkt 6).

## 11. Reproduktion

```powershell
# 1. Build
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    C:\Waermeplan\WP_Plan\WP-Plan.sln -p:Configuration=Debug -p:Platform=x86
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    C:\Waermeplan\WP_Plan\Referenzlauf\Referenzlauf.csproj -p:Configuration=Debug -p:Platform=x86

$exe = "C:\Waermeplan\WP_Plan\Referenzlauf\bin\x86\Debug\net8.0-windows\Referenzlauf.exe"

# 2. Eigene Kopien ausserhalb des Repos (DB_Basis = Flag aus, DB_Flag = Flag an,
#    DB_Szenario = praeparierte Faelle), jeweils aus der migrierten 4a-Kopie
#    C:\Waermeplan\Paket4a_Test\DB_Basis erzeugt.

# 3. Regression mit Flag AUS
foreach ($id in 1007,1008,1010,1011,1017,1018,1021,1023,1024) {
    & $exe projekt $id "C:\Waermeplan\Paket4b_Test\Lauf_Aus\Projekt_$id" C:\Waermeplan\Paket4b_Test\DB_Basis
}
& $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-14_Paket7 C:\Waermeplan\Paket4b_Test\Lauf_Aus

# 4. Flag setzen und denselben Satz mit Flag AN rechnen (Probe4b: Konsolenprojekt im
#    Scratchpad mit Assembly-Referenz auf WindowsFormsApplication1.dll und Referenzlauf.dll)
Probe4b.exe C:\Waermeplan\Paket4b_Test\DB_Flag 1007,1008,1010,1011,1017,1018,1021,1023,1024 flagAn
foreach ($id in 1007,1008,1010,1011,1017,1018,1021,1023,1024) {
    & $exe projekt $id "C:\Waermeplan\Paket4b_Test\Lauf_An\Projekt_$id" C:\Waermeplan\Paket4b_Test\DB_Flag
}
& $exe vergleich C:\Waermeplan\Paket4b_Test\Lauf_Aus C:\Waermeplan\Paket4b_Test\Lauf_An
& $exe pruefen   C:\Waermeplan\Paket4b_Test\Lauf_An

# 5. Bilanzen, Deckungsgrade und StundeAbschliessen-Zaehlung (rechnet, speichert NICHT)
Probe4b.exe C:\Waermeplan\Paket4b_Test\DB_Flag     1008,1023 probe
Probe4b.exe C:\Waermeplan\Paket4b_Test\DB_Szenario 1023,1021,1007 probe
```

**Die produktive `Kenndaten.accdb` wurde auch in dieser Etappe ausschließlich gelesen** — und
zwar nur mittelbar über die bereits vorhandene, migrierte Kopie aus Etappe 4a. Alle Läufe
liefen unter `C:\Waermeplan\Paket4b_Test\` (außerhalb des Repos).

---

# Review-Nacharbeit (14.08.2026)

Grundlage: die konsolidierten Befunde der beiden Review-Teile (Kaskade/Physik und
Infrastruktur) und die **Nutzerentscheidung zu Befund 4b-1**. Alle Läufe dieser Nacharbeit
liefen auf eigenen Kopien unter `C:\Waermeplan\Paket4_Nacharbeit\` — außerhalb des Repos;
die produktive `Kenndaten.accdb` wurde nur gelesen.

## N.1 Befund 4b-1 — der Puffer ist eine hydraulische Weiche

### Die Entscheidung

Ein Pufferspeicher darf die Anlage **nicht drosseln**. Umgesetzt wird das über den
**Bilanzraum** und nicht über einen Bypass „Direktdeckung bis zum Bedarf": Der Bypass hätte
die Phasenstruktur A–G aufgeweicht und den Freibeweis gegen Doppelzählung zerstört (eine
Anlage wäre wieder gleichzeitig in Phase B und C gewesen). Der Bilanzraum lässt beides
unangetastet:

```
Ladefähigkeit_h = (Q_max · Obergrenze − SOC)                           [SOC-Zielwert, 3.4]
                + min(offener Kanalbedarf des Pufferkanals,
                      Entnahmefähigkeit des Speichers)                 [Durchsatz]
```

**Kein `SenkeAbziehen` in Phase C. Phase E entlädt wie gehabt.** Der Konzept-Nachtrag steht
in `Konzept_Simulation_QuellenSenken.md` an 3.4 (die Formel ist ein SOC-Zielwert, der
Stundendurchsatz ist davon getrennt; optionale Lade-/Entladeleistung je Speicher als
späterer Parameter vorgemerkt, Default unbegrenzt) und an 6.3 (Umsetzung, die drei
zwingenden Regeln, PV-Budget, zeitabhängige Obergrenzen).

### Die Umsetzung

| Stelle | Änderung |
|---|---|
| `SimulationPufferspeicher.Bilanzraum(obergrenze, offenerKanalbedarf)` | die Formel; `Entnahmefaehigkeit()` liefert `EntladeleistungMax` bzw. **unbegrenzt** (der vorgemerkte Parameter) |
| `SimulationPufferspeicher.Laden(menge, stunde, durchlass)` | Überladung: die Aufnahme darf die freie Kapazität um `durchlass` übersteigen. Der Altpfad ruft weiter die zweistellige Form (`durchlass = 0`) — dort ändert sich kein Byte |
| `SimulationWaermepumpe.Verfuegbar` | dritter Fall = Bilanzraum statt reiner Ladefähigkeit; `Ladefaehig` (Abbruchbedingung) ebenso |
| `SimulationWaermepumpe.Berechnung_Zweikanalig` | Durchsatzbudget je Kanal (`absehbar[2]`), festgehalten **nach** Phase B und über die Phasen C und D **nur einmal** vergeben |
| `SimulationWaermepumpe.Entladephase` | Phase E gibt den Durchfluss (`SOC > Q_max`) **zuerst** zurück, vor der regulären Entladereihenfolge |
| `SimulationPufferspeicher.StundeAbschliessen` | Verlustanteil auf 1 geklemmt — der Füllstand kann innerhalb der Stunde über `Q_max` liegen. Ohne Durchlass greift die Klemmung nie (Altpfad bitgleich) |

Warum das Durchsatzbudget je Kanal nur einmal vergeben wird und warum Phase E den
Durchfluss zuerst zurückgibt: Sonst reichten zwei Speicher desselben Kanals dieselbe
absehbare Entnahme doppelt durch, bzw. ein anderer Speicher deckte den Bedarf und der
durchflossene bliebe über `Q_max` stehen. Bei einem Speicher je Kanal — dem heute
geprüften Fall — sind beide Regeln wirkungslos.

### Die Zahlen

Projekt **1023** (zwei Wärmepumpen auf einem 600-l-Heizungspuffer, Kessel dahinter):

| Größe | Altpfad (Flag aus) | 4b **vor** der Nacharbeit | 4b **nach** der Nacharbeit |
|---|---|---|---|
| WP-Wärmeproduktion [MWh] | 138,15 | **71,02** | **109,99** |
| Heizstab [MWh] | 62,22 | 93,48 | 87,92 |
| Speicherladung [kWh] | 13.244,5 | 71.017,9 | 109.993,2 |
| Vollzyklen | 951 | 5.102 | **7.902** |
| `SOC_Max` [kWh] | 13,83 | 13,14 | 13,14 |
| Restwärme nach der WP-Stufe [MWh] | 189,60 | — | 192,17 |
| Restwärme gesamt [MWh] | 125,28 | 149,22 | **125,28** |

Projekt **1008** (zwei Wärmepumpen auf einem 600-l-Puffer, **kein** Kessel dahinter):

| Größe | Altpfad | vor der Nacharbeit | nach der Nacharbeit |
|---|---|---|---|
| WP-Wärmeproduktion [MWh] | 81,13 | **37,34** | **79,19** |
| davon Modul 10133 [kWh] | > 0 | **0** (Befund 4b-2) | **8.568,2** |
| Vollzyklen | 3.198 | — | **11.378** |
| Restwärme [MWh] | 17,56 | 61,20 | 19,63 |

**Die Drosselung ist weg.** Die Vollzyklen sagen es am deutlichsten: 11.378 Zyklen in 8760
Stunden sind im Mittel **1,3 Speicherinhalte je Stunde** — mit der alten Ladefähigkeit war
höchstens einer möglich. `SOC_Max` bleibt dabei unverändert bei `0,95 · Q_max`: Der
Zielfüllstand ist eben nicht der Durchsatz.

**Befund 4b-2 löst sich mit:** In 1008 produziert das zweite Modul wieder (8.568,2 kWh statt 0).

### Die verbleibende Differenz zum Altpfad — erklärt

**1023: −28,16 MWh WP-Produktion** gegenüber dem Altpfad. Sie ist vollständig aufgeteilt:

```
  +25,70 MWh  Heizstab
  + 2,57 MWh  mehr Restbedarf an den nachgelagerten Kessel (189,60 -> 192,17)
  − 0,11 MWh  höhere Speicherverluste (247,2 -> 354,9 kWh)
  ------------------------------------------------------------------
   28,16 MWh  = die gesamte Differenz, auf zwei Nachkommastellen
```

Der Grund ist **nicht** mehr die Kapazität, sondern der **Kanal**: 1023 hat 60.000 kWh/a
Brauchwasserbedarf, und seine beiden Wärmepumpen laden einen Puffer mit
`Verwendung = Heizung`. Ein Heizungspuffer deckt keinen Warmwasserbedarf (Konzept 3.2/6.3,
dieselbe Regel, die einen Brauchwasserspeicher vom Heizkreis fernhält) — im Altpfad tat die
Wärmepumpe das über `SENKE_BEIDES` mit Warmwasservorrang. Den WW-Kanal übernehmen jetzt
Heizstab und Kessel. **Die Restwärme des Gesamtsystems ist dieselbe** (125,28 MWh in beiden
Fassungen); es verschiebt sich nur, welcher Erzeuger deckt. Fachlich ist das eine
Konfigurationsaussage über das Projekt: Wer Warmwasser über den Speicher decken will,
braucht einen Brauchwasserpuffer als Zweitsenke — genau der Fall, den N.6 zeigt.

**1008: −1,94 MWh.** Dort gibt es keinen Warmwasserbedarf und keinen nachgelagerten Kessel;
die Differenz besteht aus den +0,14 MWh zusätzlichen Speicherverlusten (der Durchsatz ist
3,6-mal so hoch) und 1,8 MWh, die als Restwärme stehen bleiben, weil die gesamte Wärme jetzt
durch den Speicher läuft und dessen Ladeobergrenze (95 %) die Zwischenspeicherung begrenzt.
2,4 % bei einer grundlegend anderen Betriebsweise — und ohne Kessel wandert jede nicht
gedeckte kWh unmittelbar in die Restwärme (Randfall „kein Folgeerzeuger").

### 20-fach-Volumen-Gegenprobe (Teil A)

Auf einer eigenen Kopie (`DB_Vol20`) wurde das Speichervolumen beider Projekte
verzwanzigfacht (600 l → 12.000 l, `Q_max` 13,92 → 278,40 bzw. 6,96 → 139,20 kWh):

| Projekt | WP-Produktion bei 600 l | bei 12.000 l | Änderung |
|---|---|---|---|
| 1023 | 109.993,2 kWh | 111.713,5 kWh | **+1,6 %** |
| 1008 | 79.191,3 kWh | 87.249,8 kWh | +10,2 % |

**Das Ergebnis hängt nicht mehr am Volumen.** Die verbleibende Verbesserung bei 1008 ist der
echte, physikalische Nutzen eines größeren Speichers — er verschiebt Überschusswärme aus
schwachen in starke Stunden: Die Restwärme sinkt von 19,63 auf 11,68 MWh, `SOC_Max` erreicht
132,2 von 139,2 kWh, der Speicher wird also wirklich benutzt. 1023 kann diesen Nutzen kaum
noch heben, weil dort der Heizstab ohnehin einspringt. Vor der Nacharbeit skalierte die
Produktion dagegen unmittelbar mit dem Volumen — das war der Befund.

## N.2 Alternativbetrieb bei Puffer-Hauptsenke (Pflicht-Fix 1)

`SimulationWaermepumpe.AlternativBezug()` vergleicht jetzt gegen den **offenen Kanalbedarf**
des Puffers, nicht gegen dessen Bilanzraum oder Ladefähigkeit. Begründung im Quelltext und
im Konzept-Nachtrag zu 6.3: Die Betriebsart fragt „trägt die Wärmepumpe die Last allein?".
Hinge sie am Füllstand, legte ein voller Puffer die Anlage bei milden Temperaturen still und
ein leerer ließe sie bei Frost laufen. In der Referenzmenge trägt keine Anlage mit
Puffer-Senke `Bivalenter_Betrieb = Alternativbetrieb`; die Änderung ist dort wirkungslos und
durch die unveränderten Vergleichsergebnisse belegt.

## N.3 PV-Ladebudget nach 13.5 (Pflicht-Fix 2)

`LadepotenzialBestimmen` **verbraucht das Budget nicht mehr**; `pvRest` geht nur noch als
Obergrenze ein. Abgezogen wird in der Ladephase die **tatsächlich geladene** Menge
(`ladung / COP`), über die Phasen C und D hinweg — damit ist die Aufteilung Haupt- vor
Zweitsenke weiterhin sequenziell aus demselben Budget.

**Nachweis (`DB_PV`, Projekt 1007, präpariert):** zwei Wärmepumpen-Module, beide im
Betriebsmodus `PV`. Modul 10353 hat **keine** Puffer-Senke (Heizkreis) und kann sein
Ladepotenzial also nie unterbringen; Modul 11256 lädt den Puffer 1007007.

```
Modul 10353 (PV, Heizkreis):   Waerme 22.872,977  Strom 5.351,902   Laufzeit 4829,6 h
Modul 11256 (PV, Puffer):      Ladung  2.739,727  Strom   451,483   Laufzeit  816,6 h
```

**Gegenprobe:** Wird Modul 10353 auf `Laufzeit` gestellt — es bindet dann überhaupt kein
PV-Budget —, lädt Modul 11256 **exakt dieselben 2.739,7267 kWh mit denselben 451,483 kWh
Strom**. Das ist der Punkt: Das nicht untergebrachte Potenzial des vorangehenden Moduls
kostet den nächsten nichts mehr. Mit der alten Regel wäre 11256 fast leer ausgegangen —
Modul 10353 zieht bei voller Leistung rund 1,1 kW, und so viel PV-Überschuss steht in den
wenigsten Stunden bereit.

## N.4 Zeitabhängige Auflösung der Ladeobergrenzen (Pflicht-Fix 3)

`Ladeordnung.ObergrenzenAufloesen` hat eine öffentliche Überladung mit **freier
Prioritätsfunktion** bekommen; `SimulationControl.LadeordnungAufbauen` löst je Puffer
**zweimal** auf — einmal mit der gespeicherten Ladeprio, einmal mit
`WirksameLadeprioPV` — und legt beide Werte am `Ladeauftrag` ab (`Obergrenze`,
`ObergrenzePV`). Die Engine wählt je Stunde mit `ObergrenzeStunde(pvUeberschuss)`. Damit
bestimmen **dieselbe Funktion und dieselben Daten** die Reihenfolge und den Vorrang — vorher
folgte die Reihenfolge der PV-Priorität, die Obergrenze aber der gespeicherten. Der Vorrang
wird jetzt über das **Minimum** der Prioritäten bestimmt statt über den ersten Listenplatz,
weil die Liste nach einer anderen Priorität sortiert sein kann als der, nach der aufgelöst wird.

**Nachweis (`DB_PVPrio`, Projekt 1007):** zwei Module am selben Puffer, Reservezone
`Schwelle_Aus_Nachrang = 50 %` gegen `Schwelle_Aus = 95 %`. Modul 10353 (Laufzeit) hat
Ladeprio 10, Modul 11256 (PV) hat Ladeprio 20 und `WS_Ladeprio_PV = 5`.

| | Ladung Modul 11256 (PV) | Ladung Modul 10353 | Ladung gesamt |
|---|---|---|---|
| `WS_Ladeprio_PV = 5` | **2.288,3 kWh** (697,7 h) | 22.861,1 kWh | 25.149,4 kWh |
| `WS_Ladeprio_PV = 0`, sonst identisch | 919,9 kWh (334,3 h) | 24.223,1 kWh | 25.143,0 kWh |

Die PV-Sonderpriorität verschiebt also 1,37 MWh Ladung vom netzgeführten auf das
PV-geführte Modul, bei praktisch gleicher Gesamtladung — genau der Zweck von 3.5. Der
**isolierte** Nachweis, dass davon der Obergrenzen-Anteil kommt (und nicht nur die
Reihenfolge), braucht weiterhin den Abnahmefall aus Paket 10 mit zwei konkurrierenden Ladern
und einer Solarthermie-Senke (offener Punkt 4b-3).

## N.5 Kurzschluss Quelle = Senke (Pflicht-Fix 4)

Zeigt `WQ_ID_Puffer` auf einen Speicher, der im selben Projekt bereits als **Senke** in der
Registry steht, ist der Registry-Schlüssel belegt: Die Quell-Instanz fiel bisher **still**
aus Phase G und aus der Ergebnispersistenz, obwohl das Modul aus ihr entnimmt.
`QuellspeicherUebernehmen` führt sie jetzt als **Zusatzspeicher** (`_zusatzSpeicher`,
ohne Registry-Schlüssel) und protokolliert den Fall.

**Nachweis (`DB_Kurzschluss`, Projekt 1021):** Anlage 10361 bekommt `WS_Ziel = PufferHeizung`
auf **denselben** Puffer 1018014, der ihre Wärmequelle ist.

```
Speicher-Registry: Puffer 1018014 ist QUELLE der Anlage 10361 und steht zugleich als SENKE
in der Registry (Kurzschluss, Konzept 4.6). Die Quell-Instanz rechnet mit und wird
bilanziert, aber die Konfiguration ist zu prüfen.

Registry: 1 Eintrag, AlleSpeicher(): 2
  PUFFER_1018014  Senke   Q_max 9,0248   Abschluesse 8760   Bilanzfehler 3,0e-14
  QUELLE_10361    Quelle  Q_max 4,5124   Abschluesse 8760   Bilanzfehler 0
```

Beide Instanzen schließen ihre Stunde **genau einmal** ab und stehen in der Bilanz. Dass sie
verschiedene `Q_max` tragen (9,0248 aus dem Temperaturpaar der Speicherzeile gegen 4,5124 aus
der Spreizung `WQ_Spreizung` der Anlage), ist zugleich der Grund, warum sie **nicht**
zusammengelegt werden dürfen. Der Lauf ist fachlich sinnlos — die Wärmepumpe lädt den
Speicher, aus dem sie entnimmt, und kommt über 7,5 kWh im Jahr nicht hinaus —, aber er ist
**sichtbar** sinnlos statt still falsch. Konzept 4.6 blockiert die Konfiguration beim
Speichern; Altdaten können sie tragen.

## N.6 `ImRechenpfad` enger gefasst (Pflicht-Fix 5)

`RegistryFuerZweikanaligOeffnen` öffnet den Rechenpfad nicht mehr für **alle**
Registry-Einträge, sondern nur für Speicher mit **Senkenreferenz einer Anlage**
(`WS_ID_Puffer`, `WS_ID_Puffer2` — dieselben Referenzen, aus denen `Ladeordnung` die
Ladeaufträge bildet) oder **Quellreferenz** (`WQ_ID_Puffer`). Ein Puffer, der nur über die
Alt-Zuordnung `Z_ProjektPufferSp` am Projekt hängt, kann in diesem Rechenweg von niemandem
geladen werden — er erschien mit lauter Nullen in `Tab_ErgebnisPufferspeicher` und meldete
über `puffer_wp` eine Speicherkapazität, die kein Erzeuger benutzt.

**Nachweis:** Projekt **1007** ist mit gesetztem Flag wieder **PASS** gegen den
Flag-aus-Lauf (vorher: `Kapazitaet_Pufferspeicher` 0 → 6,96, 21 neue Kennzahlen, drei neue
CSV-Ganglinien). Es bleiben ausschließlich vier `float`-Rundungen in den letzten Stellen
(`Sim.Restwaerme` 6,04914188 → 6,04914236). Dasselbe gilt für **1011** und **1018**: In 1011
bleibt nur noch der dokumentierte WW-Kanal-Effekt (`wp_warmwasserbedarf` 4.059,70 → 3.900,09),
1018 ist bitgleich. Damit ist auch die Folge aus Teil A Punkt 7 (`Kapazitaet_Pufferspeicher`)
erledigt, und die **dokumentierte Ergebnisänderung Nr. 2 aus Teil 7 entfällt**.

> **Zu 1018:** Das Projekt steht in der Äquivalenzklasse, weil seine Erzeuger sämtlich
> `WS_Ziel = Heizkreis` tragen (ein Kessel, zwei BHKW; nachgeprüft an der Datenbank). Der
> Registry-Eintrag 1018007 stammt aus einer Alt-Zuordnung ohne Senkenreferenz und rechnet
> deshalb nicht mit. Die frühere Begründung „BHKW-Senken ruhen in 4b" trifft auf 1018 gar
> nicht zu — sie gilt für Projekte, in denen ein BHKW eine migrierte Puffer-Senke trägt.

## N.7 Temperatur-Vorrangkette im Registry-Block 2 (Pflicht-Fix 6)

Fehlt der Projektkopie das Temperaturpaar, galt bisher unmittelbar der 10-K-Notnagel aus
`SimulationPufferspeicher.Init`. Jetzt gilt — wie in Block 1 und wie in Paket 1d — zuerst die
**Zuordnungszeile** aus `Z_ProjektPufferSp` (`ZuordnungsTemperaturen`, Suche über Puffer-ID,
danach über den Bezeichner).

**Nachweis:** Puffer 1007007 trägt in der Projektkopie kein Temperaturpaar. Im Lauf steht
jetzt

```
Speicher-Registry: Puffer 1007007 (Vitocell 140-E 600 Liter) hat kein Temperaturpaar in der
Projektkopie - es gilt die Zuordnungszeile (50/30 °C).
```

und `Q_max` ist 13,9200 kWh (600 l · 1,16 · 20 K / 1000) statt 6,9600 kWh nach dem 10-K-Default
— derselbe Wert, mit dem der Altpfad diesen Speicher rechnet.

## N.8 Oberflächentexte und XML-Kommentare (Pflicht-Fix 7)

Der Mouseover-Hinweis am Schalter „Zweikanalige Kaskade (Vorschau)" sagt jetzt, dass
zweikanalig gerechnet wird, dass Anlagen mit Puffer-Senke diesen laden statt den Bedarf zu
decken, dass sich die Ergebnisse **ändern** und wo die Änderungen stehen (dieses Protokoll,
Teil 7). Die Statusmeldung nach dem Umschalten ebenso. Nachgezogen sind außerdem die
XML-Kommentare, die noch den Stand der Etappe 4a beschrieben: `KaskadeZweikanalig`,
`speicherRegistry`, `senkenzuordnungen`, `ErsterHeizpuffer`, `SpeicherRegistryAufbauen`,
`QuellspeicherUebernehmen` (`SimulationControl`), `LaedtGerade`, `ImRechenpfad` und der
Registry-Feldblock (`SimulationPufferspeicher`), die Klassenköpfe von `Waermekanaele` und
`Senkenzuordnung`, `WaermesenkeClass.SenkenLaden`, `KonfigurationModel.Kaskade_Zweikanalig`
sowie die beiden „gefüllt, aber ungenutzt"-Kommentare in `SimulationBHKW` und `SimulationSPK`.

## N.9 `Uebernehmen`: „bitgleich" war zu stark (Pflicht-Fix 8)

Die Zusicherung lautet jetzt **„bis auf höchstens ein ulp (≤ 1,2·10⁻⁷ relativ)"**. Die
Differenz `rest − ww` wird in `float` gebildet und dabei gerundet, wenn ihr exaktes Ergebnis
nicht auf das Raster des Exponenten fällt; die Rückaddition liegt dann ein ulp neben
`restSumme`. Der Selbsttest ist um genau diesen Fall erweitert (Punkt **2b**, Wertemuster aus
der Review: `rest = 207393100`, vorher `5,9786716 / 0,7120331`) und prüft die neue Grenze;
Punkt 2a prüft weiterhin die Bitgleichheit im normalen Wertebereich über alle 8760 Stunden.

## N.10 Ladeprioritäten neuer Anlagenzeilen (Pflicht-Fix 9)

`WS_Ladeprio*`, `WS_Ladeprio_PV` und `WS_Ladegrenze*` sind **keine Fremdschlüssel**: 0 heißt
„nach Vorgabe" bzw. „nicht gesetzt" (Konzept 3.4). Migrationsregel R5 setzt das für den
Bestand — genau einmal je Datenbank. Danach angelegte Anlagen trugen wieder NULL, und der
Schema-Nachweis des Referenzlaufs meldete „Anlagen ohne Ladeprio-Vorgabe: **2**".

Die beiden Zeilen sind **nicht** von der Migration erzeugt worden, sondern über die
Oberfläche: ein Heizkessel und ein BHKW in Projekt 1024. Der erzeugende Pfad ist
`WizardCtrl` — eine der für diese Paketarbeit **gesperrten** Dateien. Deshalb drei Ebenen:

1. `ProjektPuffer.SQL_ANLAGENZEILE_INSERT` führt die fünf Spalten jetzt mit 0 (damit auch die
   Migrationsregeln R4/R6 und die Puffer-Verwaltung im Projekt),
2. `WErzeugerCtrl.Insert` ebenso,
3. `WaermesenkeClass.VorbelegungNachziehen(idProjekt)` zieht am **Engine-Einstieg** nach, was
   ein gesperrter Pfad offen gelassen hat — dieselbe Anweisung wie R5, dialogfrei über
   `StilleDb`, rechnerisch neutral (`StilleDb.Zahl(NULL)` ist 0).

**Nachweis** auf einer frisch migrierten Kopie (`DB_Neu`):

```
vor dem Lauf:   Anlagen ohne Ladeprio-Vorgabe: 2
Lauf Projekt 1024:  "Ladeprioritäten: 8 Feld(er) ohne Vorgabe auf 0 gesetzt"
nach dem Lauf:  Anlagen ohne Ladeprio-Vorgabe: 0
Schema-Nachweis (Referenzlauf.exe migration --nokopie):
  PufferHeizung ohne WS_ID_Puffer:    0   (erwartet 0)
  ID-Spalten mit 0 statt NULL:        0   (erwartet 0)
  Anlagen ohne Ladeprio-Vorgabe:      0   (erwartet 0)
  Projekt-Puffer ohne Anlagenzeile:   0   (erwartet 0)
  Abweichungen im Schema-Nachweis:    0
```

Dass der Nachweis wieder 0 meldet, ist reine Konsistenz — die Engine behandelt NULL und 0
gleich, und die Regression bestätigt es (PASS gegen die neue Basis, obwohl der Lauf die
Vorbelegung nachzieht).

## N.11 Kleinkram (Pflicht-Fix 10)

- **`else` vor der Altschleife geklammert.** Die einkanalige Kaskade stand ohne Block hinter
  dem `else`; jetzt mit Klammern und passend eingerückt. Rein syntaktisch — `git diff -w`
  zeigt für die Schleife keine Änderung.
- **Ungenutzte `Ladeauftrag`-Felder gestrichen:** `ID_Type`, `LadeprioPV`, `Kaskadenposition`
  und `Anlagenprioritaet` wurden gesetzt, aber nie gelesen (die Sortierung liest sie aus dem
  `LadeEintrag`). `Ladeprio` und `AnlagenID` bleiben — sie stehen in `ToString()` und damit im
  Protokollkanal. Neu ist `ObergrenzePV` (N.4).
- **Toter Obergrenzen-Rückfallzweig entfernt:** `e.Obergrenze > 0 ? … : sp.SchwelleAus` — nach
  `ObergrenzenAufloesen` ist die Obergrenze immer > 0 (eigene Ladegrenze, sonst
  `Schwelle_Aus`/`Schwelle_Aus_Nachrang`, beide mit Vorgabe 95 %). Der Zweig war unerreichbar
  und hätte zwei verschiedene Parameterquellen vermischt (Puffer-Zeile gegen Registry-Objekt).
- **Regeneration der Quellspeicher als Ergebnisänderung dokumentiert** — siehe Teil 7,
  Nachtrag unten.

## N.12 Bestandsfehler B0-13 (Zusatzauftrag aus der Parallelsitzungs-Koordination)

Eine Parallelsitzung hat auf ihrem Branch (Commit `ae7b705`) den Befund behoben, dass
`WP_Laufzeit`/`Modul_WP_Laufzeit` im **Volllast-Zweig** der Modulschleife auch dann eine volle
Stunde zählen, wenn `result[PTHERM] = 0` ist (Sperrzeit, begrenzte Quelle, Alternativbetrieb) —
der Teillast-Zweig hatte den Guard längst. Derselbe Guard steht jetzt in **beiden** Pfaden
unserer Fassung, in exakt derselben Form (`if (result[PTHERM] > 0) { … }`), damit der spätere
Merge konfliktfrei ist und Flag-an/Flag-aus für 1021 äquivalent bleiben.

**Ergebniswirkung: nur Projekt 1021, zwei laufzeitbasierte Skalare** —
`WaermepumpeModul[0].Betriebsstunden` 6692,41 → 4,41 und `Waermepumpe.Vollbenutzungsstunden`
3846,66 → 502,66. Beide Zahlen decken sich mit der Messung der Parallelsitzung. Sie sind in
der neuen Referenzbasis eingefroren.

## N.13 Verifikation

### Build

```
MSBuild WP-Plan.sln -p:Configuration=Debug -p:Platform=x86   ->  0 Fehler
MSBuild Referenzlauf\Referenzlauf.csproj  …                  ->  0 Fehler
```

Warnungen: dieselben sechs Bestandswarnungen (`WErzeugerModel` CS0108,
`StromverbraucherStammCtrl` CS0108, `KlimaregionStammCtrl` 2 × CS0109, `MDIMainForm` CS4014
und CS1998) — **keine neue**.

### Flag AUS — Regression (Pflicht)

Neun Referenzprojekte auf `C:\Waermeplan\Paket4_Nacharbeit\DB_Basis`, verglichen gegen die
bisherige Basis `2026-08-14_Paket7`:

```
Projekt_1007: PASS      Projekt_1011: PASS      Projekt_1021: FAIL (3 Abweichungen)
Projekt_1008: PASS      Projekt_1017: PASS          aggregate.csv [Pufferspeicher[0].ID_Pufferspeicher]: 8 -> 1018014
Projekt_1010: PASS      Projekt_1018: PASS          aggregate.csv [WaermepumpeModul[0].Betriebsstunden]: 6692,41 -> 4,41
                        Projekt_1023: PASS          aggregate.csv [Waermepumpe.Vollbenutzungsstunden]: 3846,66 -> 502,66
                        Projekt_1024: PASS
```

**2.260.923 verglichene Werte, genau drei Abweichungen** — die ID-Semantik des Quellspeichers
aus Etappe 4a und die beiden B0-13-Folgegrößen. `pruefen` meldet für alle neun „plausibel".

### Flag AN — Äquivalenzklasse und Puffer-Projekte

Derselbe Code, nur das Flag unterscheidet sich (`DB_Basis` gegen `DB_Flag`):

| Projekt | Ergebnis | Erklärung |
|---|---|---|
| 1010, 1017, 1018, 1021, 1024 | **PASS** | keine Puffer-Senke; 1021 bitgleich trotz Quellspeicher |
| 1007 | **PASS** | neu — der Puffer ohne Senkenreferenz rechnet nicht mehr mit (N.6). Es bleiben vier `float`-Rundungen ≤ 8·10⁻⁹ relativ |
| 1011 | erwartet abweichend | ausschließlich `wp_warmwasserbedarf` (4.059,70 → 3.900,09 kWh): der WW-Kanal an der Kaskadenposition der WP (Teil 7, Änderung 1) |
| 1008, 1023 | erwartet abweichend | die Puffer-Projekte, Zahlen und Erklärung in N.1 |

### Energieerhaltung, Abschlüsse, Deckungsgrad (Flag AN)

| | 1008 | 1023 |
|---|---|---|
| Stundenbilanz `Eingang − Rest == Produktion + Heizstab + Entladung − Ladung`, max. | 3,8·10⁻⁶ kWh | 1,5·10⁻⁵ kWh |
| Summe der Beträge über das Jahr | 0,0043 kWh | 0,0085 kWh |
| Speicherbilanz `Ladung − Entladung − Verluste == ΔSOC` | 1,5·10⁻⁸ | 4,6·10⁻⁹ |
| `StundeAbschliessen` je Speicher | **8760/8760** | **8760/8760** |
| Deckungsgrad ohne Kappung, restbedarfsbasiert | 80,0237 % | 50,6908 % |
| dieselbe Größe produktionsbasiert | 80,5916 % | 28,2230 % |
| Kanalprobe / Kappungen | 0 kWh / 0 | 7,6·10⁻⁶ kWh / 0 |

### Brauchwasser-Hauptsenke mit Zweitsenke Heizung (erneut)

Präpariert auf `DB_Szenario` (Projekt 1023, 60.000 kWh/a Brauchwasser): Puffer 1018024 auf
`Verwendung = Brauchwasser`, Anlage 11203 mit Hauptsenke Brauchwasser und Zweitsenke Heizung,
Anlage 11204 unverändert auf Heizung.

```
PUFFER_1018023  Heizung       Q_max 13,9200  Ladung 82.157,58  Entladung 81.817,53  Verluste 340,05
PUFFER_1018024  Brauchwasser  Q_max 13,5372  Ladung 54.448,08  Entladung 53.801,35  Verluste 633,97
Modul 11203 (Haupt BW + Zweit Heizung): Wärme 80.729,89
Modul 11204 (Haupt Heizung):            Wärme 55.875,77
```

Die Reihenfolge aus 13.5 geht weiterhin **exakt** auf:

- 80.729,89 − 54.448,08 = **26.281,81** kWh Überschuss in die Zweitsenke,
- 55.875,77 + 26.281,81 = **82.157,58** kWh = Ladung des Heizungspuffers. Kein kWh doppelt.
- Stundenbilanz max. 1,5·10⁻⁵ kWh, Speicherbilanzen 6,9·10⁻⁹ und 4,3·10⁻⁹ kWh,
  `StundeAbschliessen` 8760/8760 für beide, Deckungsgrad ohne Kappung 51,4366 %.

Gegenüber der Messung vor der Nacharbeit sind die Umsätze deutlich höher (66.673 → 82.158 bzw.
50.766 → 54.448 kWh) — dieselbe Ursache wie in N.1: Der Durchsatz hängt nicht mehr am Inhalt.

### Quellspeicher (1021) und Quellentnahme beim Laden

Referenzstand 1021 mit Flag an ist **bitgleich** zum Flag-aus-Lauf. Im präparierten Szenario
(Anlage 10361 lädt Puffer 1018013, Quellspeicher mit `WQ_Regeneration = 8` kW):

```
PUFFER_1018013 Heizung Q_max 13,9200 Ladung 12.238,958 Entladung 11.497,679 Verluste 728,138
QUELLE_10361   Quelle  Q_max  4,5124 Ladung  8.036,547 Entladung  7.323,714 Verluste 713,698
Modul 10361: Wärme 12.238,958  Strom 4.915,244   ->  Verdampferwärme 7.323,714 = Entladung
Stundenbilanz max. 4,8·10⁻⁷ kWh, StundeAbschliessen 8760/8760
Deckungsgrad ohne Kappung: restbedarfsbasiert 100,0000 %, produktionsbasiert 106,4459 %
```

Die produktionsbasierte Überdeckung ist die tragende Doppelzählungsprobe (siehe die Korrektur
in F.4): Produktion 12.241,336 = Bedarf 11.500,057 + Speicherverluste 728,138 + Restinhalt
13,141 — vollständig erklärt, keine doppelt gezählte kWh.

## N.14 Neue Referenzbasis

**`Referenzlaeufe/2026-08-14_Paket4/`** ist die neue Basis: neun Projekte, **Flag AUS**, mit
dem endgültigen Binärstand gerechnet. `LIESMICH.md` ist umgestellt; der Selbstvergleich der
Basis gegen einen frischen Lauf desselben Stands meldet **PASS (2.260.923 Werte)**.

Die Basis bleibt bewusst der **Altpfad**: Er ist die Rückfallebene, bis die Bestandsprojekte
projektweise umgestellt werden. Ein Lauf mit gesetztem Flag ist kein Regressionsfall gegen
diese Basis, sondern wird gegen den Flag-aus-Lauf desselben Codes verglichen.

Eingefroren sind damit die drei 1021-Änderungen (ID-Semantik des Quellspeichers, zwei
B0-13-Folgegrößen); Begründung in `2026-08-14_Paket4/lauf_protokoll.md`.

## N.15 Was offen bleibt

| # | Punkt | Bewertung |
|---|---|---|
| 1 | **Lade-/Entladeleistung je Speicher [kW]** — im Konzept-Nachtrag zu 3.4 vorgemerkt, Default unbegrenzt. Zu tun: Datenmodell, Migration, Dialog, Registry-Aufbau; die Engine hält die Stelle offen (`EntladeleistungMax`, `Entnahmefaehigkeit()`) | fachliche Erweiterung, kein Fehler |
| 2 | **4b-4: zwei Puffer im selben Kanal** sind implementiert (Entladereihenfolge 3.6, Durchsatzbudget je Kanal, Durchsatzrückgabe zuerst), aber weiterhin nur mit einem Puffer je Kanal gemessen | Abnahmefall Paket 10 |
| 3 | **4b-3: isolierter Nachweis der PV-Obergrenze** — die Wirkung von `WS_Ladeprio_PV` ist am Lauf gezeigt (N.4), der Anteil der Obergrenze daran nicht getrennt | Abnahmefall Paket 10 |
| 4 | **`ErsterHeizpuffer` folgt der Aufnahmereihenfolge, nicht der Entladepriorität** (Konzept 3.6). Bei mehreren Heizungspuffern zeigt `puffer_wp` — und damit `Kapazitaet_Pufferspeicher`, der Navigator und die Berichts-SOC-Reihe — auf den zuerst aufgenommenen | in der Referenzmenge unerheblich (nie mehr als ein rechnender Heizungspuffer); gehört zu Paket 7/9, wenn die Anzeigen n Speicher tragen |
| 5 | **Kessel, BHKW und Solarthermie rechnen einkanalig** und werten ihre Senken nicht aus; eine migrierte Puffer-Senke an ihnen ruht und wird protokolliert | Paket 5/6, unverändert |
| 6 | **Der Warmwasserkanal hat ohne Brauchwasserpuffer keinen Speicherweg.** Wo eine Anlage nur einen Heizungspuffer lädt (1023), deckt der Heizstab bzw. der Folgeerzeuger das Warmwasser | Konfigurationsfrage, siehe N.1; die Zweitsenke ist der vorgesehene Weg |

## N.16 Folgegrößen und Konsumenten der geänderten Werte

Wer mit gesetztem Flag rechnet, bekommt nicht nur andere Ganglinien — mehrere abgeleitete
Größen und Anzeigen hängen daran:

| Größe / Konsument | Wirkung |
|---|---|
| `Waermepumpe.Vollbenutzungsstunden`, `WaermepumpeModul[i].Betriebsstunden` | folgen `WP_Laufzeit`; im neuen Weg zählt auch die Ladung Laufzeit, und B0-13 hat die Zählung im Volllast-Zweig korrigiert |
| `Bivalenzpunkt` | wird nach Phase E auf den Kanalrest gezogen — dieselbe Stelle im Ablauf wie im Altpfad, aber ein anderer Rest |
| `Min_Spitzenkessel` und die Auslegungsgrößen der Folge-Erzeuger | folgen dem Restbedarf nach der WP-Stufe (1023: 189,60 → 192,17 MWh) |
| `Kapazitaet_Pufferspeicher` | kommt aus `puffer_wp`; mit dem engeren `ImRechenpfad` (N.6) wieder wie im Altpfad |
| `ErdreichAuswertung` / VDI-4640-Prüfung | rechnet die Entzugsarbeit **inklusive Speicherladung** aus der WP-Produktion; die Ladung ist im neuen Weg ein Vielfaches, die Auslegungsprüfung fällt entsprechend schärfer aus |
| Berichtsmodul und Navigator | konsumieren die SOC-Reihe und die Speicherkennzahlen (`SOC_Mittel`, `SOC_Max`, `Vollzyklen`) — alle drei ändern sich mit dem Durchsatz, `Vollzyklen` um Größenordnungen |

## N.17 Geänderte Dateien der Nacharbeit

| Datei | Änderung |
|---|---|
| `Allgemein/Simulation/SimulationPufferspeicher.cs` | `Bilanzraum`, `Entnahmefaehigkeit`, `EntladeleistungMax`, `Laden(…, durchlass)`, Verlustklemmung, Kommentare |
| `Allgemein/Simulation/SimulationWaermepumpe.cs` | Bilanzraum in `Verfuegbar`/`Ladefaehig`, `AlternativBezug`, `Kanalbedarf`, Durchsatzbudget, `DurchsatzEntladen`, PV-Budget in der Ladephase, B0-13 in **beiden** Pfaden |
| `Allgemein/Simulation/SimulationControl.cs` | `else` geklammert, `SenkenPufferDerAnlagen`, engeres `ImRechenpfad`, `ZuordnungsTemperaturen`, `_zusatzSpeicher` (Kurzschluss), zweite Obergrenzen-Auflösung, `BetriebsmodusDesModuls`/`…DerAnlage`, `VorbelegungNachziehen`-Aufruf, Kommentare |
| `Allgemein/Simulation/Ladeordnung.cs` | öffentliche `ObergrenzenAufloesen(…, prio)` mit Minimum-Bestimmung |
| `Allgemein/Simulation/SimulationKanaele.cs` | `Ladeauftrag.ObergrenzePV`/`ObergrenzeStunde`, ungenutzte Felder entfernt, ulp-Zusicherung, Selbsttest 2b |
| `Allgemein/Simulation/WaermesenkeClass.cs` | `VorbelegungNachziehen`, Kommentar zu `SenkenLaden` |
| `Allgemein/Update/ProjektPuffer.cs` | Anlagenzeile mit `WS_Ladeprio*`/`WS_Ladegrenze*` = 0 |
| `Controller/WErzeugerCtrl.cs` | dieselben fünf Spalten im INSERT |
| `Views/Simulation/Form_Simulation_Config.Uebersicht.cs` | Mouseover- und Statustext, XML-Kommentar |
| `Model/KonfigurationModel.cs`, `Allgemein/Simulation/SimulationBHKW.cs`, `…/SimulationSPK.cs` | XML-Kommentare der 4a-Stände |
| `Allgemein/Simulation/Konzept_Simulation_QuellenSenken.md` | Nachträge an 3.4 und 6.3 |
| `Referenzlaeufe/LIESMICH.md`, `Referenzlaeufe/2026-08-14_Paket4/` | neue Basis |

Keine Designer- und keine `.resx`-Datei angefasst. Die gesperrten Dateien (`WizardCtrl`,
`WErzeugerModel`, `Form_BHKWEing`, `WizardParent`, `Form_Heizkessel*`, `RecordSet`) sind
unberührt — auch dort, wo eine von ihnen die Ursache war (N.10).

## N.18 Reproduktion der Nacharbeit

```powershell
$exe   = "C:\Waermeplan\WP_Plan\Referenzlauf\bin\x86\Debug\net8.0-windows\Referenzlauf.exe"
$probe = "<Scratchpad>\Probe4b\bin\x86\Debug\net8.0-windows\Probe4b.exe"

# Regression (Flag aus) gegen die neue Basis
foreach ($id in 1007,1008,1010,1011,1017,1018,1021,1023,1024) {
    & $exe projekt $id "C:\Waermeplan\Paket4_Nacharbeit\Lauf_Aus\Projekt_$id" `
                       C:\Waermeplan\Paket4_Nacharbeit\DB_Basis
}
& $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-14_Paket4 `
                 C:\Waermeplan\Paket4_Nacharbeit\Lauf_Aus

# Flag an: derselbe Satz auf DB_Flag, danach Flag aus gegen Flag an
& $exe vergleich C:\Waermeplan\Paket4_Nacharbeit\Lauf_Aus C:\Waermeplan\Paket4_Nacharbeit\Lauf_An

# Bilanzen, Deckungsgrade, StundeAbschliessen (rechnet, speichert NICHT)
& $probe C:\Waermeplan\Paket4_Nacharbeit\DB_Flag        1008,1023      probe
& $probe C:\Waermeplan\Paket4_Nacharbeit\DB_Vol20       1008,1023      probe   # 20-fach-Volumen
& $probe C:\Waermeplan\Paket4_Nacharbeit\DB_Szenario    1023,1021,1007 probe   # BW + Zweitsenke, Quelle
& $probe C:\Waermeplan\Paket4_Nacharbeit\DB_PV          1007           probe   # PV-Budget
& $probe C:\Waermeplan\Paket4_Nacharbeit\DB_PVPrio      1007           probe   # PV-Sonderprioritaet
& $probe C:\Waermeplan\Paket4_Nacharbeit\DB_Kurzschluss 1021           probe   # Quelle = Senke

# Schema-Nachweis (Ladeprio-Vorbelegung)
& $exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb C:\Waermeplan\Paket4_Nacharbeit\DB_Neu --nokopie
```

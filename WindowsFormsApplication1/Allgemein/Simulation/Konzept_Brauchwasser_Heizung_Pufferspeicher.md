# Konzept: Brauchwasser, Heizung und Pufferspeicher — Dreikanalbilanz, Schichtspeicher, Booster-Wärmepumpe

**Fassung 2** · Stand 27.08.2026 · Branch `Pufferspeicher` · Status: **weitgehend entschieden** —
offen sind nur noch **F11** (BHKW-Senkenzahl) und **F13** (Kennlinienrand); zu beiden steht die
Erläuterung in Kapitel 12, die Entscheidung wird nachgereicht.

| Fassung | Inhalt |
|---|---|
| 1 | Erstfassung nach Ist-Analyse (9 Prüfläufe) und 3 adversarialen Reviews; Entscheidungen F1 (Altpfad entfällt), F7 (Schichtmodell N = 1…10, Default 1), F17 (R-Prozess automatisch) und Merge-Strategie (`kostenformulare` landet zuerst in `main`) |
| **2** | Entscheidungen F2–F6, F8–F10, F12, F14–F18 eingearbeitet: Netzverluste **anteilig** auf die Kanäle (F2), **Kalender vereinheitlicht** (F3), Senkentabelle mit unbegrenztem Rang (F4), **F5-Alternative** — sechs Senkenziele bleiben, Pufferklassen werden ein **Klassen-Set** aus drei Nutzungs-Flags (Kombi = {Heizung, Brauchwasser}), Warnkriterienkatalog statt pauschalem Hinweis (F6), Herkunftsrechnung je Speicher (F8), Booster als Anzeigeregel (F9), Knappheitsreihenfolge projektweit übersteuerbar über neue `Tab_Einstellungen`-Spalte (F10), Quellprofile in die DB (F12), F14–F16/F18 wie empfohlen. **Neu:** Heizkessel-Quellpuffer ausdrücklich verankert (8.4). Migrationsblock jetzt **45–51** |

**Bezug:** [`Konzept_Simulation_QuellenSenken.md`](Konzept_Simulation_QuellenSenken.md) (Fassung 12 —
inzwischen **umgesetzt** in den Paketen B0, 1–9, D1–D5b, K3, BHKW-Regulär und Parallelverbund; die
dortige Kopfzeile „Umsetzungsstand 0 %" ist überholt), [`Konzept_KonfigUI_Hydraulik.md`](Konzept_KonfigUI_Hydraulik.md),
[`KONTEXT_Brauchwassertypen_VDI6002.md`](../../../KONTEXT_Brauchwassertypen_VDI6002.md),
`Konzept_TWW-Zapfprofile_WP-Plan_1.md` (Repo-Wurzel).

**Prüfmethodik:** Ist-Analyse durch **neun unabhängige Prüfläufe** am Code-Stand 26.08.2026
(Engine-Kern, Pufferspeicher, Quellen/Senken-Klassen, Wärmepumpe, übrige Erzeugermodule,
Bedarfsermittlung, Konfigurationsdialog, Datenmodell/Migration, Bilanzdefekte), jede Kernaussage mit
Datei:Zeile belegt. Der Entwurf wurde anschließend durch **drei unabhängige adversariale Reviews**
(Fakten, Vollständigkeit, Architektur) am Code gegengeprüft; alle bestätigten Befunde sind in dieser
Fassung eingearbeitet. Dieses Konzept ist die Fortschreibung des Quellen/Senken-Konzepts — es wiederholt
dessen Inhalte nicht, sondern beschreibt den **nächsten Umbau** auf dem umgesetzten Stand.

> **Einordnung.** Bestandteil von EPOS-Plan; betrachtet Quellen und Senken der Wärmeerzeuger sowie
> Pufferspeicher. Die bisherige Umsetzung wird gezielt überarbeitet mit vier Zielen:
> **(Z1)** Vereinfachung von Code und Prinzip, **(Z2)** mehr Flexibilität für Erweiterungen,
> **(Z3)** Korrektur der Bilanzierung — Heizwärme, Brauchwasser und Prozesswärme als drei getrennte
> Kanäle, **(Z4)** Wärmepumpe mit Quelle Pufferspeicher („Booster"-Wärmepumpe) als vollwertige,
> physikalisch plausible Konstellation. Dazu kommen: wählbare Senken je Erzeuger (Heizkreis,
> Prozesswärme, Puffer Heizung/Brauchwasser/Prozess, Kombi-Speicher), mehrere parallel genutzte
> Pufferspeicher, drei Pufferklassen mit freier Zuordnung (Hinweis statt Sperre) und ein
> **Schichtspeichermodell** samt Konfiguration. Der Dialog „Simulation Konfiguration" behält sein
> Design (Schema- und Listen-/Kartendarstellung) und wird nur angepasst, wo erforderlich.

---

## 1. Auftrag und Ziele

| # | Ziel | Messlatte |
|---|---|---|
| Z1 | **Vereinfachung** von Code und Prinzip | Der einkanalige Altpfad und seine Doppelstrukturen entfallen; ein Rechenweg, eine Wahrheit je Kennzahl |
| Z2 | **Flexibilität** für Erweiterungen | Kanäle indiziert statt boolesch verdrahtet; Senken als Liste statt Spaltenpaar; Erzeugermodule mit einheitlicher Stundenschnittstelle |
| Z3 | **Korrekte Bilanzierung** | Heizwärme, Brauchwasser, Prozesswärme als drei getrennte `float[8760]`-Kanäle von der Bedarfsermittlung bis in Ergebnis und Bericht; Energieprobe je Stunde |
| Z4 | **Booster-Wärmepumpe** | WP mit Quelle Pufferspeicher rechnet gegen die tatsächliche Speichertemperatur (Schichtmodell) und ist im Schema als Booster erkennbar |

Dazu die Detailanforderungen aus dem Auftrag: wählbare Senken je Erzeuger (mindestens eine, wahlweise
mehrere), sechs Senkentypen (Heizkreis, Prozesswärme, Pufferspeicher Heizung / Brauchwasser /
Prozesswärme, Kombi-Speicher), mehrere Pufferspeicher parallel, drei Pufferklassen mit freier
Zuordnung und Hinweis, Schichtspeichermodell mit Konfiguration, WP-Quellen je Bauart (Luft-Wasser:
Luft; Sole/Wasser: Erdreich, Konstanttemperatur, Temperaturprofil aus Profileingabe **Monat oder
Tag**, Stundenprofil-Import 8760, Pufferspeicher), spätere Erweiterung auf Priorisierung der Be- und
Entladung sowie auf mehrere Heizkreise.

---

## 2. Ausgangslage: Ist-Stand nach Umsetzung der Fassung 12

### 2.1 Was funktioniert und Grundlage bleibt

Der zweikanalige Rechenweg ist umgesetzt und in der Referenzlauf-Suite (neun Projekte) abgenommen:

- **Stundenschleife mit Reihenfolge-Invariante A–G** (`Kaskadenschleife.cs:477-626`): Vorabentladung,
  Bedarfsdeckung in Kaskadenreihenfolge, Ladephasen Haupt-/Zweitsenken kaskadenübergreifend nach
  Ladepriorität, Nachentladung (Durchsatz zuerst), Heizstab, `StundeAbschliessen` genau einmal je
  Speicher. Die Phasen B/C/D laufen je **Rechenebene** aufsteigend — die Booster-/Kessel-Kaskade
  („WP 1 → Puffer 1 → WP 2 → Puffer 2") fällt daraus ohne Sonderfall heraus; Ringe brechen den Lauf
  mit sprechender Meldung ab (`Kaskadenschleife.cs:877-910`).
- **Speicher-Registry** je `Tab_Pufferspeicher.ID` mit Entladereihenfolge je Kanal, zwei vorsortierten
  Ladeordnungen (mit/ohne PV-Überschuss), Durchsatz-/Umsatztrennung („hydraulische Weiche"),
  BHKW-Reservierung und -Notreserve, Parallelverbund (`SimulationControl.cs:2302-2592`,
  `Ladeordnung.cs`, `SimulationPufferspeicher.cs`).
- **Herkunftsrechnung** („Vermischung im Speicher", `Kaskadenschleife.cs:142-306`): Entladung wird den
  ladenden Erzeugerarten anteilig am Speicherinhalt zugerechnet — Grundlage der Eigenanteils-Bilanz
  im `SimulationRunner`.
- **Senken- und Quellenmodell** an `Tab_Energieanlagen`: Hauptsenke + optionale Zweitsenke
  (`WS_Ziel`, `WS_ID_Puffer`, `WS_Ziel2`, `WS_ID_Puffer2`, Ladeprioritäten und -grenzen), Quellen
  `Aussenluft | Konstant | Erdreich | Profil | CSV | Pufferspeicher` (`WaermequelleClass.cs:47-52`)
  plus der Leerwert `WQ_TYP_OHNE` („keine gesonderte Wärmequelle", Etappe D5b, `:54-55`).
  Der Kombispeicher (Verwendung „Kombi") bedient Heizung und Warmwasser aus einem Vorrat.
  Die Kombination „Hauptsenke Heizkreis + Puffer-Zweitsenke" (direkt decken, Rest laden) ist heute
  bereits der Regelfall.
- **Konfigurationsdialog** `Form_Simulation_Config` mit Karten- und Schemaansicht, gemeinsamem
  Auswahlschlüssel und denselben Editoren aus beiden Ansichten — dieses Design bleibt (Z-Vorgabe 8).
- **SchemaMigration** nach ADR-001: versionierte In-Code-Migration, Marker
  `Tab_Applikation.SchemaVersion` (Zielstand heute **37**), Einzelanhebung je Schritt, Abbruch beim
  ersten Fehler. Neue Schemaänderungen dieses Konzepts docken hier an.

### 2.2 Strukturelle Grenzen, die dieses Konzept aufhebt

1. **Nur zwei Kanäle.** `Waermekanaele` führt `Heiz` und `WW` (`SimulationKanaele.cs:31/34`).
   **Prozesswärme wird vor der Kanalbildung in den Summenvektor addiert**
   (`SimulationWaermebedarf.cs:244`) und der Heizkanal als Residuum `Gesamt − Brauchwasser` gebildet
   (`:349-376`) — Prozesswärme und Netzverluste sind ab da unumkehrbar „Heizwärme". Folgen: ein
   Erzeuger mit `WS_Typ='Heizung'` deckt Prozesswärme wie Raumheizung; ein Heizungspuffer speichert
   Prozesswärme; es gibt keine Senke, kein Pufferziel und keine Ergebnisgröße für Prozesswärme.
2. **Höchstens zwei Senken je Anlage**, die zweite zwingend ein Puffer
   (`SimulationKanaele.cs:419-448`, `WaermesenkeClass.cs:328`). Der Spaltenkatalog verwirft weitere
   `WS_*3…n`-Spaltenreihen ausdrücklich (`SchemaKatalog.cs:79`).
3. **Ein-Zonen-Speicher.** `SimulationPufferspeicher` hat genau eine Zustandsgröße `SOC` [kWh] gegen
   `Q_max = V·1,16·(VL−RL)/1000`; der Klassenkopf schließt Schichtung und Leistungsbegrenzung
   ausdrücklich aus (`SimulationPufferspeicher.cs:5-16`). Alle Regelgrößen sind SOC-Anteile.
4. **Zwei vollständige Rechenwege.** Der einkanalige Altpfad (Modulschleife auf einem Summenvektor)
   existiert parallel zur Speicherstufe; die Weiche hat drei Oder-Glieder (Projektflag
   `Kaskade_Zweikanalig`, BHKW im Projekt, Parallelverbund — `SimulationControl.cs:419-434`). Im
   Altpfad gelten fort: die Brauchwasser-Kappung der WP (C6, `SimulationWaermepumpe.cs:429-440`),
   doppelt bilanzierte Quellspeicher, entfallende Quellregeneration, `WS_Ziel`-Blindheit und das
   Restbedarfs-Aliasing. **Fast jeder heute offene Bilanzdefekt ist ein Altpfad-Defekt.**
5. **Harte Zuordnungsprüfung statt Hinweis.** `WaermesenkeClass.PufferPasst` lehnt einen Puffer ab,
   dessen Verwendung nicht exakt zum Senkenziel passt (`WaermesenkeClass.cs:1005-1031`);
   `PasstZuFilter` blendet unpassende Puffer aus den Auswahllisten aus.
6. **Booster nur energetisch.** Die Quelltemperatur eines Quellpuffers ist eine Jahreskonstante
   (`WQ_Temp` oder `(VL+RL)/2`, `WaermequelleClass.cs:603`) ohne Rückkopplung an den
   Speicherzustand; Kennlinienkappung oberhalb der obersten Stützstelle erfolgt still
   (`SimulationWaermepumpe.cs:1670`).
7. **Doppelte Persistenz-Altlasten.** Die Alt-Zuordnung `Z_ProjektPufferSp` (Textschlüssel!) wird
   über die Brücke `WpSenkeSpiegeln` weiter mitgeschrieben (`WaermesenkeClass.cs:1338`) und ist im
   Altpfad die einzige Quelle des WP-Senkenspeichers; `Form_KonfigPufferspeicher` ist die zweite,
   konkurrierende Ablage der Betriebstemperaturen.
8. **Anzeige-/Ergebnislücken bei mehreren Puffern.** `Kapazitaet_Pufferspeicher` und die
   Berichts-Zeitreihe `PUFFER_SOC` hängen am Alias `puffer_wp` = erster Heizungspuffer in
   Aufnahmereihenfolge (`SimulationRunner.cs:254`, `ZeitreihenExtraktor.cs:69-70`); der Fall „zwei
   Puffer je Kanal" ist implementiert, aber in keinem Referenzprojekt belegt (Befunde S-1/S-2).

### 2.3 Bestandsfehler mit Ergebniswirkung (→ Vorab-Paket V0)

Bei der Ist-Analyse bestätigt bzw. neu gefunden — sie verändern Ergebnisse und sind **vor** dem
Umbau als eigenes Paket zu beheben, damit sich ihre Wirkung nicht mit der Konzeptumstellung mischt
(dasselbe Vorgehen wie B0 in Fassung 12):

| # | Befund | Fundstelle | Wirkung |
|---|---|---|---|
| V0-1 | **Mehrgebäude-Doppelzählung**: `Waermebedarf_Gebaeude` wird nur einmal genullt, `BhkwPlan.StdWerte` addiert, der kumulierte Vektor geht je Gebäude erneut ein — bei N Gebäuden `Σ (N−i+1)·G_i` | `SimulationWaermebedarf.cs:116/181/188`, `BhkwPlan.cs:233-243` | Jahreswärmebedarf systematisch überhöht ab dem 2. Gebäude (produktiv: Projekt 1008 mit 2 Gebäuden). Von der Referenzsuite unentdeckt, **obwohl 1008 Referenzprojekt ist** — die fehlerhafte Rechnung ist dort selbst eingefroren. Vor dem Fix: Gebäudezahl je Referenzprojekt aus der DB belegen |
| V0-2 | **Mehrere Stromprofile: nur das letzte zählt** — Aufsummierung auskommentiert | `SimulationStrombedarf.cs:240-245` | Strombedarf zu niedrig (produktiv: 1006/1007/1008/1032 je 2, 1011 drei Profile) |
| V0-3 | **Profilzugriffe ohne Projektfilter**: Brauchwasser-Monatswerte/-Stundenprofil und Prozess-Wochenprofil werden per Bezeichner/Typname ohne `ID_Projekt` nachgeschlagen | `SimulationWaermebedarf.cs:813/852/739` | Projekte rechnen mit fremden Werten (produktiv: 45× „Haushalt-3" mit zwei verschiedenen Monatssätzen) |
| V0-4 | **Prozesswärme-Monatswerte aus dem STAMM-Katalog** statt aus der Projektkopie | `SimulationWaermebedarf.cs:699` | Monatsverteilung und Typbezug stammen aus dem Katalog; nur die Projekt-Jahressumme wirkt (Skalierung `pjv/jv`, `:704-724`). Achtung: der Fix ändert mit der Quelltabelle auch den Normierungsnenner `jv` — Ergebnisänderung je Referenzprojekt vorher/nachher dokumentieren. Gegenrichtung mitfixen: die Katalog-**Vorschau** liest das Prozess-Typprofil heute aus der Projektkopie statt aus `Tab_Prozesstyp_STAMM` (`:739`) |
| V0-5 | **Externe Wärmeganglinien**: kein 8760-Rastercheck (Stromzweig hat ihn), kein Reset zwischen Ganglinien, Überlauf > 8760 wirft ungefangen | `SimulationWaermebedarf.cs:211-230` | Doppelzählung von Reststunden bzw. Laufabbruch |
| V0-6 | **Schaltjahr-Absturz**: Monatsgrenzen aus `DateTime.Today.Year` → `mo_ende[11] = 8783` auf `float[8760]` | `Init.cs:14-25`, `BhkwPlan.cs:125` | Ab 2028 bricht jeder Lauf mit IndexOutOfRange ab — sicher eintretend |
| V0-7 | **Detailansicht-Deckungsgrade** von WP, Kessel und Solarthermie rechnen noch die alten, vom Runner verworfenen Formeln (Restbedarf kann negativ werden, Zähler enthält Speicherladung); nur das BHKW ist nachgezogen | `Form_Simulation_Detail.cs:3202/3343/3404` gegen `SimulationRunner.cs:264-351` | Dialog und `Tab_Ergebnis` zeigen verschiedene Zahlen, sobald ein Puffer im Spiel ist |
| V0-8 | **Netzverluste bei absoluter Einheit** werden addiert, aber als 0 ausgewiesen | `SimulationWaermebedarf.cs:261-266` | Bilanzausweis falsch |
| V0-9 | Restpunkte: Katalog-Löschung per `WHERE Bezeichner` statt ID (`PufferSpStammCtrl.cs:177`); stille Kennlinienkappung oben ohne Protokoll (`SimulationWaermepumpe.cs:1670`). Der B0-9-Rest (fehlender Handler an `comboBox_Erzeuger`, `Form_KonfigPufferspeicher.cs:65`) wird **nicht** mehr repariert — der Dialog entfällt mit Paket A1 | | Einzelfixes |

V0-1 bis V0-5 betreffen die **Eingangsdaten der Kanäle** — ohne sie ist jede Dreikanalbilanz auf
Sand gebaut. Referenzprojekte für Mehrgebäude und Mehrfachprofile sind Teil des Pakets (11.1).

---

## 3. Leitentscheidungen

Die Architektur dieses Konzepts hängt an acht Leitentscheidungen. Sie sind hier mit Empfehlung
ausgearbeitet; die zugehörigen Rückfragen stehen in Kapitel 12.

| # | Entscheidung | Empfehlung | Rückfrage |
|---|---|---|---|
| L1 | **Der einkanalige Altpfad entfällt ersatzlos.** Die (auf drei Kanäle erweiterte) Stundenschleife wird der einzige Rechenweg; das Flag `Kaskade_Zweikanalig` und die Weiche entfallen, die Migration setzt Bestandsprojekte um | umsetzen | F1 |
| L2 | **Kanäle werden indiziert**, nicht boolesch: `KANAL_HEIZUNG=0, KANAL_BRAUCHWASSER=1, KANAL_PROZESS=2`, `KANAL_ANZAHL=3`. Alle Kanalstrukturen (Restbedarf, Entladeordnung, Durchsatzbudget, `SenkeAbziehen`) laufen über den Index — der **Rechenkern** ist damit auf mehrere Heizkreise vorbereitet; Persistenz und Oberfläche kanalbezogener Parameter (z. B. `Entnahme_*`, `T_Nutz_*`) blieben bei diesem Ausbau umzustellen (dann Kindtabelle `Z_PufferKanal` statt Spaltenreihe) | umsetzen | — |
| L3 | **Kein Residuum mehr**: jeder Kanal wird direkt aus seiner Quelle gebildet (Heizung = Gebäude + externe Lastgänge; Brauchwasser = Brauchwasserprofile; Prozess = Prozessprofile; Netzverluste **anteilig je Stunde** auf die Kanäle, F2 ✔). Invariante: Kanalsumme = bisheriger Gesamtbedarf | umsetzen | F2 ✔ |
| L4 | **Senken als Zuordnungstabelle** `Z_AnlageSenke` (n Senken je Anlage mit Rang) statt der Spaltenpaare `WS_*`/`WS_*2` | umsetzen | F4 |
| L5 | **Senkenziele bleiben ausdifferenziert** (F5-Alternative, entschieden 27.08.2026): `Heizkreis`, `Prozesswaerme`, `PufferHeizung`, `PufferBrauchwasser`, **`PufferProzess` (neu)**, `PufferKombi`. Das Puffer-Ziel benennt den Zweck der Ladung (Auswahl, Anzeige, Schema); maßgeblich für die **Entladung** ist das Klassen-Set des Puffers (L6). Weicht das Ziel vom Set des gewählten Puffers ab, greift ein Warnkriterium (F6) | umsetzen | F5 ✔ |
| L6 | **Pufferklassen als Klassen-Set** (F5-Alternative): `Tab_Pufferspeicher.Verwendung` wird durch drei unabhängige Flags `Nutzung_Heizung`, `Nutzung_Brauchwasser`, `Nutzung_Prozess` abgelöst (DML: Heizung → {H}, Brauchwasser → {B}, Kombi → {H, B}); damit sind auch Kombinationen wie {Heizung, Prozess} möglich, „Kombi" ist nur noch der Anzeigename des Sets {H, B}. Die Zuordnungsprüfung wird von der Sperre auf einen **Warnkriterienkatalog** umgestellt (6.2); hart gesperrt bleiben nur Kurzschluss (Quelle = eigene Senke) und Ring | umsetzen | F5 ✔, F6 ✔ |
| L7 | **Schichtspeichermodell als Erweiterung des bestehenden Speicherkerns**, nicht als Ersatz: `SimulationPufferspeicher` erhält N Schichten (Default **N = 1** = exakt heutiges Verhalten). **SOC bleibt die führende Zustandsgröße** samt Durchsatz-/Weichen-Mechanik; die Schichtebene verteilt den Speicherinhalt `min(SOC, Q_max)` auf N Temperaturen und liefert daraus Nutzbarkeitsgrenzen und Quelltemperaturen (7.3). Kaskadenschleife und Ladeordnung bleiben unverändert | umsetzen | F7, F8 |
| L8 | **Booster-Wärmepumpe wird temperaturgekoppelt**: die Quelltemperatur folgt dem Speicherzustand (Schichttemperatur an der Entnahmehöhe); die Konstellation wird im Schema als „Booster" benannt; Kennlinienkappung wird protokolliert | umsetzen | F9 |

**Warum L1 zuerst.** Die Ist-Analyse belegt: nahezu alle offenen Bilanzdefekte (C6-Kappung,
Quellspeicher-Doppelbilanz, entfallende Regeneration, `WS_Ziel`-Blindheit, Aliasing) leben nur noch
im Altpfad, und jede Erweiterung müsste dreifach gedacht werden (Altpfad ist mit dem BHKW-Zwang
ohnehin keine vollständige Rückfallebene mehr). Der Rückbau entfernt je Erzeuger bis zu drei
Rechenfassungen, die Weiche, `KonfigurationCtrl.KaskadeNotwendig`, die Alt-Zuordnung
`Z_ProjektPufferSp` samt Spiegelbrücke und den Dialog `Form_KonfigPufferspeicher` — der größte
Einzelhebel für Z1. Der Preis: Bestandsprojekte ohne Flag ändern ihr Ergebnis; deshalb Referenzbasis
neu einfrieren (Kapitel 11) und Ergebnisänderungen im Migrationshinweis dokumentieren.

---

## 4. Zielbild Kanalmodell (Z3)

### 4.1 Drei Kanäle, indiziert

```csharp
// Allgemein/Simulation/SimulationKanaele.cs — Umbau
public static class Kanal
{
    public const int HEIZUNG = 0;
    public const int BRAUCHWASSER = 1;
    public const int PROZESS = 2;
    public const int ANZAHL = 3;
}

public class Kanalsatz            // ersetzt Waermekanaele
{
    public readonly float[][] Bedarf = new float[Kanal.ANZAHL][];   // je [8760]
    public float[] Summe();                                          // eigener Vektor (Aliasing-Regel B0-2)
    public Kanalsatz Clone();
}
```

Alle heute booleschen Kanalstellen werden auf den Index umgestellt:

| Heute | Künftig |
|---|---|
| `SenkeAbziehen(senke, menge, ref rest_ww, ref rest_heiz)` (`Kaskadenschleife.cs:1263`) | `SenkeAbziehen(kanalmaske, menge, double[] rest)` — Abzug in Maskenreihenfolge (4.3) |
| `double[2] absehbar` Durchsatzbudget (`Kaskadenschleife.cs:475`) | `double[Kanal.ANZAHL]` |
| `EntladenHeizung` / `EntladenBrauchwasser` (`SimulationKanaele.cs:636/639`) | `Entladeordnung[Kanal.ANZAHL]` (Liste je Kanal) |
| `BedientKanal(bool brauchwasser)` (`SimulationPufferspeicher.cs:641`) | `BedientKanal(int kanal)` aus der Klassenmenge des Speichers |
| `ref rest_heiz, ref rest_ww` in allen Modulsignaturen | `double[] rest` (ein Parameter, indiziert) |
| `_entladungJeArtStunde = double[ART_ANZAHL]` (`Kaskadenschleife.cs:198`) | `double[ART_ANZAHL, Kanal.ANZAHL]` — Zurechnung je Art **und** Kanal |
| `Direktdeckung_gesamt`, `Speicherentladung_Anteil`, `Heizstab_gesamt` als Skalare je Modul | `double[Kanal.ANZAHL]` je Modul — Voraussetzung für Deckungsgrade je Kanal (4.4) |
| Herkunftsrechnung `Anteil_Laden/_Entladen/_Umbuchen` (eindimensional nach Art) | kanalindizierte Buchung der Entladung (die Inhaltsanteile selbst bleiben je Speicher, 7.6) |

`Waermekanaele.Uebernehmen` (der Kompatibilitätsanker ohne produktiven Aufrufer,
`SimulationKanaele.cs:101-120`) wird auf `Kanalsatz` verallgemeinert (proportionale Verteilung über
n Kanäle, Erhaltungszusage unverändert) und behält seinen Debug-Selbsttest — er bleibt die
spezifizierte Kanalarithmetik für künftige einkanalige Zulieferstufen.

### 4.2 Kanalbildung ohne Residuum (L3)

`SimulationWaermebedarf` führt die drei Vektoren getrennt bis zum Schluss:

```
HEIZUNG      = Gebäudewärme (V0-1 saniert) + externe Lastgänge (V0-5 saniert, Kanal wählbar → F18)
BRAUCHWASSER = brauchwasserwerte                                  (Projektfilter, V0-3 saniert)
PROZESS      = prozesswerte                                       (Projektkopie, V0-3/V0-4 saniert)
danach:  Netzverluste je Stunde ANTEILIG auf alle drei Kanäle (F2 ✔)
```

Externe Wärmeganglinien erhalten eine Kanalzuordnung (`Z_ProjektWaermebedarf.Kanal`, Vorbelegung
`Heizung` = altverhaltenserhaltend, F18) — eine importierte Ganglinie kann fachlich ebenso
Prozess- oder Brauchwasserlast sein; heute trägt sie keinerlei Kennzeichnung.

Die Addition `VectorenAddieren(prozesswerte, Waermebedarf)` (`:244`) und die Residuum-Bildung in
`Kanaele()` (`:349-376`) entfallen. Der bisherige Summenvektor `Waermebedarf` wird zur abgeleiteten
Größe `Summe()` — jede Stelle, die heute den Gesamtbedarf liest (Dauerlinie, Maximum, Anzeigen),
bekommt ihn unverändert. **Invariante (Energieprobe):** je Stunde gilt
`HEIZUNG + BRAUCHWASSER + PROZESS == bisheriger Gesamtbedarf` — nach V0 **bis auf die
float-Rundung der Additionsreihenfolge** (Toleranz 1-ULP-Klasse, derselbe Maßstab wie im
bestehenden `Waermekanaele.Selbsttest`; float-Addition ist nicht assoziativ, „bitgleich" wurde für
genau diese Zusage schon einmal zurückgenommen). Abgesichert als Debug-Selbsttest und als
Protokollprobe im Lauf (11.3). Beide heutigen Kappungsfälle der Residuum-Bildung (`ww < 0` und
„Brauchwasser > Gesamtbedarf", `:360-368`) sind konstruktiv unmöglich geworden und entfallen
ersatzlos — jeder Kanal entsteht aus eigenen, nichtnegativen Quellen.

**Netzverluste (F2 ✔ entschieden 27.08.2026: anteilig):** Die Netzverluste werden je Stunde
**proportional zu den Kanalbedarfen** dieser Stunde auf die drei Kanäle verteilt; ist der
Gesamtbedarf einer Stunde 0, gehen sie vollständig auf den Heizkanal (dieselbe Randfallregel wie
in `Waermekanaele.Uebernehmen`). Das ersetzt die Altverhaltens-Entscheidung O2 („vollständig auf
Heizung") bewusst — eine dokumentierte Ergebnisänderung für **alle** Bestandsprojekte mit
Brauchwasser- oder Prozessanteil (11.2). Der bisherige konstante Aufschlag auf alle 8760 Stunden
bleibt in Stufe 1 unverändert (nur die Kanalzuordnung ändert sich).

**Gemeinsame Profilroutine.** Die drei fast identischen Kopien des Profilalgorithmus (Prozess
`:667`, Brauchwasser `:773`, Strom `SimulationStrombedarf.cs:161`) werden zu **einer** Routine
„12 Monatswerte × 168-h-Wochenprofil → 8760" mit einheitlichen Fehlerpfaden zusammengezogen.
Die Routine kennt **zwei Quellmodi als expliziten Aufrufparameter**: *Projektrechnung* (Projektkopie,
Pflichtfilter `ID_Projekt`) und *Katalogvorschau* (`_STAMM`-Tabellen, die kein `ID_Projekt` tragen —
heutige Aufrufer: Admin-Dialoge und Vorschaupfade, `SimulationWaermebedarf.cs:804-809`); die
bisherige Ableitung aus `list != null` entfällt. Einheitliche Fehlerpfade: kein Treffer →
Protokollwarnung statt stiller 0, Monatssumme 0 → Warnung statt NaN (`BhkwPlan.cs:193`),
Klassenfelder-Zwischenspeicher (`monats_waerme`, `wochen_waerme`, `temp`) entfallen. Die Bedarfsrechnung wechselt dabei von `RecordSet`-String-SQL auf
`DataRepository` mit `?`-Parametern (Projektvorgabe).

**Kalender (F3 ✔ entschieden 27.08.2026: vereinheitlichen):** Die drei Kalenderkonventionen
(Profilpfad „1. Januar = Sonntag" `BhkwPlan.cs:180`; Gebäudepfad `Tab_Klimadaten.WE`;
WP-Quellprofil „nächstes Nicht-Schaltjahr, Montag = 0" `WaermequelleClass.cs:934-938`) werden mit
der gemeinsamen Profilroutine auf **eine** Konvention gezogen: führend ist der Klimadaten-Kalender
(`Tab_Klimadaten.WE` bzw. der daraus abgeleitete Wochentag des 1. Januar) — die Profilkachelung
startet mit dem tatsächlichen Wochentag statt fest mit Sonntag. Energiewirkungsfrei
(Monatsnormierung bleibt), aber jede Stundenganglinie der Profil-Bedarfe verschiebt sich —
dokumentierte Ergebnisänderung (11.2), umgesetzt in Paket K1.

### 4.3 Abzugsregel und Kanal-Rangfolge

`SenkeAbziehen` bleibt die eine Kanalregel für alle Erzeuger, Heizstab und Speicherentladung.
Statt der drei Fälle `Warmwasser | Heizung | Beides` arbeitet sie mit einer **Kanalmaske** je
Direktsenke (5.2). Bei mehrelementiger Maske gilt eine feste **Knappheitsreihenfolge**; Vorbelegung:

```
BRAUCHWASSER  →  PROZESS  →  HEIZUNG
```

Warmwasser-Vorrang wie heute (Komfortkriterium), Prozess vor Heizung (Produktionsausfall wiegt
schwerer als Raumkomfort). Dieselbe Reihenfolge gilt für die Entladung eines Speichers, dessen
Klassen-Set mehrere Kanäle umfasst (heutige Kombi-Regel K-1, verallgemeinert). Mit dem Klassen-Set
(L6/F5) ist auch die Prozess-Position ab Paket K2 real wirksam (z. B. Set {Heizung, Prozess}).
**Projektweit übersteuerbar (F10 ✔ entschieden 27.08.2026):** neue Spalte
`Tab_Einstellungen.Kanal_Knappheitsreihenfolge` (TEXT, sprachneutraler Steuerwert, Default
`BRAUCHWASSER;PROZESS;HEIZUNG`, Schritt 46) — unter Beachtung der Ordinal-Lesekette von
`Tab_Einstellungen` nur mit zielgenauem UPDATE (Kapitel 9).

### 4.4 Bedarfsarten und Ergebnis je Kanal

- Die **Bedarfsart** der Direktsenke Heizkreis behält ihre drei Werte `Beides | Warmwasser |
  Heizung` — ihr Ort wandert aber in die Senkenzeile (`Z_AnlageSenke.Bedarfsart`, 5.1); die Spalte
  `Tab_Energieanlagen.WS_Typ` wird Lese-Altlast. Die neue Direktsenke `Prozesswaerme` deckt den
  Prozesskanal. Ein vierter Bedarfsart-Wert ist damit **nicht** nötig — eine Wahrheit je Frage.
- **Bestandsprojekte mit Prozesswärme** verlieren durch die Herauslösung ihre bisherige (implizite)
  Prozessdeckung — die Migration muss sie ersetzen: **Regel R-Prozess** (5.1/F17): führt das
  Projekt Prozesswärme, erhält jede Anlage mit Direktsenke Heizkreis und Bedarfsart `Beides` oder
  `Heizung` unmittelbar nach ihrer Heizkreiszeile eine zusätzliche Senkenzeile
  `Ziel='Prozesswaerme'`. Ohne diese Regel bliebe der Prozesskanal in jedem Bestandsprojekt
  ungedeckt — weit über die beabsichtigte, dokumentierte Ergebnisänderung hinaus.
- **Ergebnis-Persistenz je Kanal** (heute fehlt jede Trennung, `ErgebnisModel.cs:57-65`):
  `Tab_ErgebnisEnergiebedarf` erhält `Waermebedarf_Heizung/_Brauchwasser/_Prozess`; je Erzeuger
  kommen `Deckung_Heizung/_Brauchwasser/_Prozess` (Eigenanteils-Logik des Runners, je Kanal) hinzu;
  `Tab_ErgebnisPufferspeicher` erhält die Kanalaufteilung der Entladung sowie die
  Durchsatzsummen (heute nicht persistiert). Voraussetzung ist die kanalindizierte Buchführung in
  der Engine (4.1, letzte drei Tabellenzeilen) — sie gehört zu Paket K2, nicht zu E1. Der Bericht
  weist die drei Bedarfe und Deckungsgrade getrennt aus; der `ZeitreihenExtraktor` liefert die
  Kanalganglinien.

---

## 5. Zielbild Senkenmodell (Z2, Auftragspunkt 4)

### 5.1 Zuordnungstabelle statt Spaltenpaar (L4, L5)

```
Z_AnlageSenke
  ID              AUTOWERT      Primärschlüssel
  ID_Anlage       LONG          FK → Tab_Energieanlagen.ID
  Rang            LONG          1..n — Reihenfolge der Senken dieser Anlage
  Ziel            TEXT(50)      'Heizkreis' | 'Prozesswaerme' | 'PufferHeizung' | 'PufferBrauchwasser'
                                | 'PufferProzess' (neu) | 'PufferKombi'   (DbWerte, deutsch, eingefroren)
  Bedarfsart      TEXT(50)      nur bei Ziel='Heizkreis': 'Beides' | 'Warmwasser' | 'Heizung'
  ID_Puffer       LONG NULL     nur bei Puffer-Zielen: FK → Tab_Pufferspeicher.ID
  Ladeprio        LONG          0 = Vorgabe (Ladeordnung)
  Ladeprio_PV     LONG          0 = keine PV-Sonderpriorität
  Ladegrenze      DOUBLE        0 = nicht gesetzt; in PROZENT — dieselbe Einheit wie WS_Ladegrenze,
                                Schwelle_Aus und Schwelle_Aus_Nachrang (die Umrechnung /100 bleibt
                                wie heute beim Bau des Ladeauftrags, SimulationControl.cs:2106-2111)
  Anschlusshoehe  DOUBLE NULL   Einspeisehöhe 0..1 am Schichtspeicher (7.4); NULL = Standard oben
```

**Invariante:** Rang 1 ist Pflicht. Der Dialog verweigert das Entfernen der letzten Zeile; findet
die Engine keine Zeile, rechnet sie `Heizkreis/Beides` mit Protokollwarnung (heutige
Normalisierungsregel, `WaermesenkeClass.cs:320-326`).

Die sechs geforderten Senkentypen sind damit direkt die sechs `Ziel`-Werte (F5-Alternative, L5):
`Heizkreis` und `Prozesswaerme` als Direktsenken, die vier Puffer-Ziele als Ladeziele. Das
Puffer-Ziel benennt den **Zweck der Ladung** (Auswahlfilter, Chip, Schema-Kante); welche Kanäle
der Speicher **entlädt**, bestimmt allein sein Klassen-Set (6.1) — weicht das Set des gewählten
Puffers vom Ziel ab, greift ein Warnkriterium (6.2). **Migration (Schritt 47):**

- je Anlage wird `WS_Ziel`/… als Rang 1 und `WS_Ziel2`/… als Rang 2 übernommen — die
  `Ziel`-Textwerte bleiben unverändert (F5-Alternative: keine Wertablösung), `WS_ID_Puffer*`
  wandert mit; `WS_Typ` wandert als `Bedarfsart` in die Rang-1-Zeile; Anlagen ganz ohne `WS_Ziel`
  erhalten eine Rang-1-Zeile `Heizkreis/Beides` (Invariante oben);
- `Ladeprio_PV`: Rang 1 erbt `WS_Ladeprio_PV`, alle höheren Ränge erhalten 0 — eine Spalte
  `WS_Ladeprio_PV2` existiert nicht, die PV-Sonderregel hängt heute konstruktiv an der Hauptsenke
  (`Ladeordnung.cs:270-273`); das ist exakt das Bestandsverhalten;
- **Regel R-Prozess** (4.4/F17): zusätzliche `Prozesswaerme`-Zeilen für Bestandsprojekte mit
  Prozesswärme;
- die Altspalten bleiben als stillgelegte Lese-Altlast stehen (Muster `WQ_Puffer` → `WQ_ID_Puffer`).

Der Parallelverbund `Z_AnlagePufferVerbund` erhält die Spalte `ID_Senke` (FK → `Z_AnlageSenke.ID`),
damit ein Verbund künftig an jeder Puffersenke möglich ist, nicht nur an der ersten.
**Projektkopie:** `Z_AnlageSenke` hat kein `ID_Projekt` — beim Duplizieren greift weder `FK_MAP`
noch der `ID_Projekt`-Standardweg. Nötig sind ein `KINDER`-Eintrag
(`ID_Anlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ?)`) **und** die Aufnahme von
`ID_Anlage`/`ID_Puffer` in die `FK_MAP` (`ProjektDuplizierenCtrl.cs:117-128, :265-282`). Derselbe
Eintrag fehlt heute bereits für `Z_AnlagePufferVerbund` — als Prüf-/Fixpunkt in Paket S1 aufnehmen;
„Projekt mit mehreren Senken duplizieren" wird Abnahmekriterium von S1. Die offenen
`FK_MAP`-Einträge für `WS_ID_Puffer`, `WS_ID_Puffer2`, `WQ_ID_Puffer` (Paket-3-Restpunkt) werden
miterledigt.

### 5.2 Verteilungsregel: eine kWh, genau ein Ziel

Die Reihenfolge-Invariante der Stunde bleibt; die Phasen C/D verallgemeinern sich zu **Ladephasen
je Rang**:

```
B  Bedarfsdeckung:  je Anlage deckt die Kette ihrer DIREKTsenken in Rangfolge den
                    Momentanbedarf (SenkeAbziehen mit Kanalmaske der Senke)
C… Ladephasen:      Rang für Rang: alle Puffersenken des Rangs r aller Anlagen,
                    kaskadenübergreifend sortiert nach Ladeordnung (heutiges C = Rang 1,
                    heutiges D = Rang 2); Budget ist das verbleibende Erzeugungspotenzial
```

Doppelzählungsfreiheit heißt künftig nicht mehr „eine Anlage ist entweder in B oder in C", sondern:
**die Produktion einer Stunde wird sequenziell über die Senkenliste verteilt; jede kWh geht genau
einmal entweder durch `SenkeAbziehen` (Direktsenke) oder durch `Speicher.Laden` (Puffersenke).**
Der Standardfall „direkt decken + Rest in den Puffer laden" ist **heute bereits konfigurierbar**
(Hauptsenke Heizkreis + Puffer-Zweitsenke — der Regelfall) und bleibt bei der Migration erhalten.
**Neu** sind: mehr als zwei Senken je Anlage, die freie Reihenfolge, und Direktsenken ab Rang 2 —
heute muss jede Zweitsenke ein Puffer sein (`WaermesenkeClass.cs:328-335`), „Puffer zuerst, Rest
direkt" ist also nicht abbildbar. Das Durchsatzbudget je Kanal funktioniert unverändert, nur
indiziert.

**Einschränkung BHKW:** Die drei Fahrweisen schalten alle Motoren gemeinsam gegen einen Wärmeraum
zu (`SimulationBHKW.cs:931-992`); eine Senke je *Modul* wäre neue Physik (Paket-6-Entscheidung).
Zusätzlich ist die BHKW-Stufe hart auf **zwei Senkenplätze** gebaut: `Auftrag_Haupt`/`Auftrag_Zweit`,
genau ein Reservierungsfeld, `ZweitsenkenRaum()` nur aus dem zweiten Auftrag
(`SimulationBHKW.cs:831-834, :1088, :1112-1116`; `Kaskadenschleife.cs:654-681` füllt nur zwei
Slots). Es bleibt deshalb bei **höchstens zwei Senken je BHKW-Stufe (Rang 1/2)** — der Senkendialog
erzwingt das für BHKW-Anlagen (F11); eine Verallgemeinerung (Wärmeraum = Σ Ladefähigkeit aller
Puffersenken, Reservierung als Liste je Zielspeicher) ist als Ausbaustufe vorgemerkt.

### 5.3 Senkendialog

`Form_Waermesenke` wechselt von vier Radiobuttons + Zweitsenken-GroupBox auf eine **geordnete
Senkenliste** (Hinzufügen/Entfernen/Rang ändern; je Zeile Ziel, ggf. Puffer, Bedarfsart,
Ladepriorität/-grenze). Das Grunddesign des Konfigurationsdialogs (Karten + Schema) bleibt; die
Chips der Erzeugerkarte zeigen künftig die Senkenkette („→ Heizkreis · → Puffer P1 · → Puffer P2").

---

## 6. Pufferspeicher: Klassen, freie Zuordnung, Bewirtschaftung (Auftragspunkte 4 und 5)

### 6.1 Drei Klassen als Klassen-Set (L6, F5-Alternative — entschieden 27.08.2026)

`Tab_Pufferspeicher.Verwendung` wird durch **drei unabhängige Ja/Nein-Flags** abgelöst:
`Nutzung_Heizung`, `Nutzung_Brauchwasser`, `Nutzung_Prozess` (Schritt 46, DML-Migration:
`Heizung` → {H}, `Brauchwasser` → {B}, `Kombi` → {H, B}; die Spalte `Verwendung` bleibt
Lese-Altlast). Damit sind **beliebige Kombinationen** möglich — auch {Heizung, Prozess} oder
{H, B, P}; „Kombi" ist nur noch der Anzeigename des Sets {H, B}, kein eigener Persistenzwert mehr.
Die Laufzeitrolle `Quelle` bleibt unverändert (leeres Set). `BedientKanal(int kanal)` liest das
Flag; ein Speicher steht in den Entladeordnungen **aller** Kanäle seines Sets, die Reihenfolge je
Stunde regelt die Knappheitsregel 4.3. Mindestens ein Flag muss gesetzt sein (Dialog erzwingt das;
leeres Set wäre ein Speicher, den niemand entlädt).

### 6.2 Freie Zuordnung mit Warnkriterienkatalog (F6 ✔ entschieden 27.08.2026)

Grundsatz: Zuordnungen sind frei, aber **nur sinnvolle Konstellationen bleiben unkommentiert** —
ein definierter Kriterienkatalog prüft beim Speichern (Dialogwarnung mit Begründung, nicht
blockierend) **und** beim Laufstart (Protokollwarnung). `PufferPasst`/`PasstZuFilter` entfallen in
ihrer sperrenden Form; die Puffer-Auswahl zeigt alle Projekt-Puffer, gruppiert nach Klassen-Set.

**Warnkriterien (W1–W6):**

| # | Kriterium | Begründung |
|---|---|---|
| W1 | Puffer-Ziel der Senke ∉ Klassen-Set des gewählten Puffers (z. B. `PufferProzess` auf einen {H, B}-Speicher) | Ladung mit Zweck, den der Speicher nie entlädt |
| W2 | `Speichertyp` des Katalogs passt nicht zum Klassen-Set (z. B. Brauchwasserspeicher-Bauform als reiner Heizpuffer) | Bauform-/Nutzungswiderspruch — der vom Auftrag genannte Fall „Warmwasserpuffer für Heizung, wenn der Benutzer das wünscht" |
| W3 | Erzeuger-Vorlauf < `VL_eff` des Ziel-Puffers bzw. < `T_Nutz` des Zielkanals | Erzeuger kann den Speicher nie auf Solltemperatur laden — mit dem Schichtmodell erstmals prüfbar |
| W4 | `T_Nutz_BW` > `VL_eff` | Kanal wäre dauerhaft abgeschaltet; wird zusätzlich zur Warnung auf `VL_eff` geklemmt (7.2) |
| W5 | Quellpuffer ohne einen einzigen Lader (Booster-Kette ins Leere; heute nur Registry-Randfall) | Quelle liefe nach Startfüllung leer |
| W6 | `Schichten_Anzahl` > 1 am Leitspeicher eines Parallelverbunds | unzulässig — wird abgewiesen, nicht nur gewarnt (6.3) |

**Hart gesperrt bleiben**: Kurzschluss (Quellpuffer = eigene Senke), Ring in der Kaskadenkette,
leeres Klassen-Set und W6 — physikalisch bzw. konstruktiv begründet; Kurzschluss und Ring sind
heute schon Dialog- und Engine-Guards.

### 6.3 Mehrere Puffer parallel

Die Mechanik (Registry, Entladereihenfolge je Kanal nach `Entladeprio`, Ladeordnung, Durchsatzbudget
einmal je Kanal und Stunde) existiert und bleibt. Zu ergänzen:

- **Nachweis**: ein Referenzprojekt „zwei Puffer je Kanal" (Befund S-2) — Teil von Paket V0/11.1.
- **Anzeige/Bericht**: der Alias `puffer_wp` wird abgelöst. `Kapazitaet_Pufferspeicher` wird zur
  Summe der Senkenspeicher-Kapazitäten des Laufs; Zeitreihen (`PUFFER_SOC`, CSV-Export) laufen je
  Speicher über den bestehenden Schlüssel `PUFFER_<ID>` (Befund S-1).
- **Parallelverbund** bleibt für baugleiche Behälter, die bewusst als EIN Vorrat rechnen sollen;
  echte parallele Bewirtschaftung übernehmen mehrere eigenständige Puffer mit Entladeprio. Beides
  zusammen an einer Senke ist zulässig (Verbund = ein Rechenspeicher in der Menge). Verbund und
  Schichtung schließen sich je Rechenspeicher aus (F8): ein Verbund-Leitspeicher rechnet stets mit
  N = 1 — sein `Q_max` ist die **aufsummierte** Kapazität aller Mitglieder
  (`SimulationControl.cs:2596-2673`), eine aus dem Leitspeicher-Volumen abgeleitete Schichtebene
  wäre falsch. `Schichten_Anzahl > 1` am Leitspeicher eines Verbunds wird beim Speichern abgewiesen
  (Guard in Paket P1).
- **Lade-/Entladeleistung je Speicher** (`EntladeleistungMax`, seit Paket 4 vorgemerkt): wird mit
  dem Schichtmodell-Paket als Spalten `Ladeleistung_Max`/`Entladeleistung_Max` [kW] eingeführt,
  Default 0 = unbegrenzt (verhaltensneutral).

---

## 7. Schichtspeichermodell (L7)

### 7.1 Modellwahl

**Mehrzonen-Modell mit idealer Einschichtung und vertikalem Ausgleich** — N übereinanderliegende
Schichten gleichen Volumens, oben Schicht 1. Es ist der fachliche Mittelweg zwischen dem heutigen
Ein-Zonen-Modell und CFD-artigen Ansätzen und in Stundensimulationen Standard (Typ „multi-node").
Kern der Architektur: **das Schichtmodell lebt vollständig innerhalb von `SimulationPufferspeicher`**;
die Kaskadenschleife, die Ladeordnung, die Herkunftsrechnung und alle Erzeugermodule sehen weiter
dieselbe Energie-Schnittstelle (SOC, Q_max, Laden, Entladen, Ladefähigkeit, Bilanzraum, Hysterese).

### 7.2 Zustand und Parameter

| Größe | Bedeutung | Herkunft |
|---|---|---|
| `N` | Schichtenzahl 1…10, Default **1** | `Tab_Pufferspeicher.Schichten_Anzahl` |
| `T[i]` | Temperatur je Schicht [°C], Start = `RL_eff` | Laufzeitzustand |
| `VL_eff`, `RL_eff` | wirksames Temperaturpaar: das gepflegte Paar `Vorlauf`/`Ruecklauf`; fehlt es (ΔT ≤ 0), gilt `RL_eff = RL` und `VL_eff = RL_eff + RueckfallDeltaT` (10 K generisch, 20 K BHKW-Pendelspeicher — dieselbe Spreizung, die heute in `Q_max` einfließt, `SimulationPufferspeicher.cs:376-394`, `SimulationControl.cs:1655`). `SimulationPufferspeicher` nimmt beide als **neue Laufzeitfelder** auf — heute verrechnet `Init` die Temperaturen sofort zu `Q_max` und verwirft sie | vorhandene Spalten `Vorlauf`/`Ruecklauf` + Rückfallregel |
| `H`, `D` | Höhe/Durchmesser für Schichtflächen; ersatzweise H/D-Verhältnis (Default 2,5) aus dem Volumen | neue Spalte `Hoehe` (NULL = aus H/D-Default) |
| `LambdaEff` | effektive vertikale Wärmeleitfähigkeit inkl. Wandleitung [W/(m·K)], Default 1,5 | neue Spalte `Lambda_Eff` |
| Verluste | wie heute `Bereitschaftsverluste` [kWh/24h], verteilt auf die Schichten nach Manteloberfläche und gewichtet mit `(T[i] − RL_eff)/(VL_eff − RL_eff)` | vorhandene Spalte |
| `T_Nutz[kanal]` | Mindest-Nutztemperatur je Kanal [°C]; **Spalten-Default NULL = RL_eff (verhaltensneutral)**; der Dialog schlägt 55 °C vor, sobald der Anwender N > 1 wählt; Werte > VL_eff werden beim Laufstart auf VL_eff geklemmt mit Protokollwarnung — sonst wäre der Kanal still komplett abgeschaltet | neue Spalte `T_Nutz_BW` (zunächst nur Brauchwasser, F7) |
| Anschlusshöhen | Einspeisehöhe je Senkenzeile (5.1), Entnahmehöhe je Kanal, Quell-Entnahmehöhe je WP-Quelle; alle 0..1, Default: Laden oben, Entnahme oben, Rücklauf unten, Quelle oben | `Z_AnlageSenke.Anschlusshoehe`, `Tab_Pufferspeicher.Entnahme_*`, `Tab_Energieanlagen.WQ_Anschlusshoehe` |

### 7.3 SOC bleibt führend — die Schichtebene als zweite Zustandsebene

Der Verhaltensneutralitäts-Anker: **die Energiearithmetik des Bestands bleibt wörtlich bestehen.**
`SOC` [kWh] bleibt die führende Zustandsgröße samt der gesamten Durchsatz-/Weichen-Mechanik —
Laden mit Durchlass (`SOC` darf innerstündlich über `Q_max` liegen,
`SimulationPufferspeicher.cs:467-493`), Zerlegung Umsatz/Durchfluss (Befund N6), `Reserviert`,
`Bilanzraum`, `Q_max` unverändert. Die Schichtebene ist eine **zweite Zustandsebene**, die
ausschließlich den *Speicherinhalt* `A = min(SOC, Q_max)` auf N Temperaturen verteilt:

```
Σ_i  V/N · 1,16 · (T[i] − RL_eff) / 1000  ==  min(SOC, Q_max)     [Schicht-Invariante]
T[i] ∈ [RL_eff, VL_eff]
```

Der **Überhang `B = max(0, SOC − Q_max)`** (Durchfluss derselben Stunde) wird bewusst **nicht** in
Schichten geführt — er ist hydraulisch durchströmende Wärme, folgt weiter der bestehenden
N6-Buchung (Durchsatz-Ladung/-Entladung/-Verluste zuerst) und zählt für die Entnahmefähigkeit
stets als nutzbar. Ohne diese Trennung wäre der Durchlass-Mechanismus im Schichtmodell nicht
darstellbar (eine bei `VL_eff` gekappte Temperatursumme kann nie über `Q_max` liegen) — und mit
ihr bleibt er es wörtlich.

Alle Hüllgrößen (SchwelleEin/Aus/AusNachrang/Reserve, Ladefähigkeit, Bilanzraum, Hysterese)
rechnen unverändert auf dem SOC. **Vollzyklen bleiben umsatzbasiert** (`Ladung_gesamt` bzw. bei
Quellspeichern `Entladung_gesamt` gegen `Q_max`, Befund N6, `SimulationPufferspeicher.cs:587-589`)
— vom Schichtmodell nur mittelbar betroffen, über die unveränderte Umsatz-/Durchfluss-Aufteilung.

**Für N = 1 ist das Modell per Konstruktion byte-gleich**: Laden, Entladen, Verluste und
Kennzahlen laufen ausschließlich über die unveränderte SOC-Arithmetik; die eine Schichttemperatur
ist eine reine Umrechnung `T = RL_eff + A/Q_max · (VL_eff − RL_eff)` ohne Rückwirkung
(Wärmeleitung und Inversionsmischung haben bei einer Schicht keine Wirkung). Der Nachweis
(byte-vergleichender Referenzlauf N = 1 gegen den Stand vor dem Paket, **einschließlich eines
Projekts mit ungepflegtem Temperaturpaar/Rückfall-ΔT**) ist Abnahmekriterium des
Schichtmodell-Pakets.

### 7.4 Stundenablauf im Speicher

Reihenfolge je Stunde (innerhalb der bestehenden Phasen, die Phasenfolge A–G ändert sich nicht).
Grundsatz: **je Vorgang bucht zuerst die unveränderte SOC-Arithmetik; die Schichtebene vollzieht
nur den Umsatzanteil (Änderung von `A = min(SOC, Q_max)`) nach.**

1. **Beladung** (aus Phasen C…): SOC-Buchung wie heute, einschließlich Durchlass. Auf der
   Schichtebene wird der Umsatzanteil ideal eingeschichtet: von der **Einspeisehöhe abwärts**
   werden die Schichten nacheinander auf `VL_eff` gehoben („Temperaturband wandert nach unten");
   Schichten oberhalb der Einspeisehöhe bleiben unberührt.
2. **Entnahme** (Phasen A/E): SOC-Buchung wie heute; der Durchfluss verlässt den Speicher zuerst
   (N6). Auf der Schichtebene gilt **ideale Verdrängung am Anschluss**: die entnommene Masse
   verlässt den Speicher an der Entnahmehöhe des Kanals; zugänglich sind die Schichten **von der
   Entnahmehöhe abwärts** — sie rücken nacheinander an den Anschluss nach, die unterste Schicht
   fällt zuerst auf `RL_eff` zurück (Rücklauf tritt unten ein). Schichten **oberhalb** der
   Entnahmehöhe bleiben unberührt. Die **Entladefähigkeit je Kanal** ist
   `Durchfluss B + Σ Energie der zugänglichen Schichten mit T[i] ≥ T_Nutz[kanal]`
   (bei Default `T_Nutz = RL_eff` und Entnahmehöhe oben: gesamter SOC — verhaltensneutral).
   Verfügbarkeitsbemessung und Zustandsupdate sind damit derselbe Vorgang.
3. **Vertikaler Ausgleich** (in `StundeAbschliessen`): Wärmeleitung zwischen Nachbarschichten
   `ΔQ_i = k · (T[i+1] − T[i])` mit `k = LambdaEff · A_quer / (H/N)` [W/K] · 1 h; der Austausch ist
   je Paar auf 25 % der Temperaturdifferenz gekappt — unbedingt stabil und monoton, keine
   Sub-Schritte nötig. Anschließend **Inversionsmischung**: ist eine untere Schicht wärmer als die
   darüber, werden beide volumengewichtet gemischt (Auftrieb).
4. **Verluste** (in `StundeAbschliessen`): SOC-Buchung wie heute (füllstandsanteilig, gekappt,
   Durchfluss trägt zuerst — N6-Regel unverändert); die Schichtebene verteilt den Schichtanteil
   des Verlusts `(T[i] − RL_eff)`-proportional — bei N = 1 identisch mit der heutigen
   füllstandsanteiligen Rechnung.

Ergebnisgrößen zusätzlich zum Bestand: Ganglinien `T_oben`, `T_unten` am Objekt (CSV-Export je
Speicher), Kennzahlen `T_oben_Mittel`, `T_oben_Min` in `Tab_ErgebnisPufferspeicher`. Eine
Ergebniszeile **je Schicht** wird nicht persistiert (Volumen; F7).

### 7.5 Kombi-Speicher im Schichtmodell

Der Kombispeicher wird physikalisch das, was er ist: **Brauchwasser-Bereitschaftszone oben,
Heizzone darunter** — Entnahmehöhe Brauchwasser oben (1,0), Entnahmehöhe Heizung Mitte
(Default 0,5). Mit der Verdrängungsregel aus 7.4 Punkt 2 greift die Heizentnahme nur auf die
Schichten **unterhalb** ihrer Entnahmehöhe zu — die BW-Bereitschaftszone oben bleibt von der
Heizung unangetastet; genau das ist die physikalische Aufwertung gegenüber dem heutigen
Ein-Vorrat-Kombi. `T_Nutz_BW` wird vom Dialog mit 55 °C vorgeschlagen, sobald N > 1 gewählt wird
(Spalten-Default bleibt RL_eff, 7.2). Die Knappheitsregel 4.3 ersetzt die heutige K-1-Regel.
Bei N = 1 verhält er sich exakt wie heute (ein Vorrat, beide Kanäle).

### 7.6 Was bewusst NICHT geändert wird

- **Herkunftsrechnung bleibt je Speicher**, nicht je Schicht (F8): die Zurechnung „Vermischung im
  Speicher" (Anteile am Gesamtinhalt) ist eine Bilanzkonvention für die Wirtschaftlichkeit, keine
  Physik; eine Schicht-Herkunft wäre Scheingenauigkeit und bräche die bestätigte Anwenderentscheidung
  vom 15.08.2026.
- **Hysterese, Reserve, Ladeordnung** bleiben SOC-basiert — das Schichtmodell verfeinert die
  Physik *unterhalb* der Regelung, nicht die Regelung selbst.
- **Quellspeicher** (Erdsonden-Ersatz mit `WQ_Spreizung`, Start voll) bleiben Ein-Zonen-Modelle;
  Schichtung gilt für Senken- und Kombi-Speicher. Der *geteilte* Puffer der Booster-Konstellation
  ist ein Senkenspeicher und trägt Schichtung — genau dort wirkt sie auf die Quelltemperatur (8.2).

---

## 8. Wärmequellen: Wärmepumpe, Heizkessel, Booster (Z4)

### 8.1 Quellen je Bauart

Die sechs Quellentypen existieren vollständig (`Aussenluft`, `Konstant`, `Erdreich`, `Profil`,
`CSV`, `Pufferspeicher`). Anpassungen:

1. **Bauart-Bindung sichtbar machen**: bei Luft-Wasser-WP erscheint die Quelle „Luft" als fester,
   nicht änderbarer Eintrag auf Karte und Schema (statt der heutigen Abbruchmeldung
   `Form_Simulation_Config.Uebersicht.cs:749`); bei Sole/Wasser ist die Liste wählbar. Die Engine
   erzwingt die Bindung (heute nur stiller Kurzschluss `WaermequelleClass.cs:585`).
2. **Temperaturprofil „Monat oder Tag"**: Die Profileingabe erhält zwei gleichwertige Betriebsarten —
   **12 Monatswerte** (vorhanden) **oder 365 Tageswerte** (neu, `Form_Quellprofil` erweitert; Ablage
   nach dem Kopf/Daten-Muster, Punkt 3). Der heutige additive Wochengang (168 Werte) bleibt als
   Option der Monatsvariante erhalten (Bestandsdaten), wird bei der Tagesvariante aber nicht
   angeboten — ein Jahr ergibt sich aus 365 Tageswerten direkt und **kalenderunabhängig**
   (Tag i → Tag i; kein Wochentagsbezug). Randnotiz Kalender: der bestehende Wochengang der
   Quellprofile nutzt eine **dritte** Kalenderkonvention (Wochentag des 1. Januar aus dem nächsten
   Nicht-Schaltjahr, Montag = 0 — `WaermequelleClass.cs:934-938`), neben „1. Januar = Sonntag" des
   Bedarfspfads (`BhkwPlan.cs:180`) und `Tab_Klimadaten.WE` des Gebäudepfads (→ F3).
3. **Stundenprofil in die Datenbank**: `WQ_CSV` speichert heute nur den Dateipfad — Projektweitergabe
   verliert die Quelle, der Rückfall auf Außentemperatur ist still (`WaermequelleClass.cs:655`).
   Neu: Import legt die 8760 Werte als Kopf/Daten-Paar `Tab_Quellprofil`/`Tab_QuellprofilDaten` ab
   (Muster `Tab_Stromganglinie`); `WQ_CSV` bleibt als Lese-Altlast mit einmaliger Übernahme beim
   ersten Öffnen. Gleiches Muster löst mittelfristig die delimitierten Strings
   `WQ_Monatswerte`/`WQ_Wochenwerte` ab (F12).
4. **Indexkopplung entschärfen**: die Quellenauswahl wird von `SelectedIndex` auf den
   sprachneutralen Typ-Schlüssel umgestellt (`TypWerte`-Werte als Tag der Einträge) — die stille
   Datenzerstörungsfalle „Liste umsortiert → Bestandsprojekte zeigen auf falsche Quelle" entfällt.

### 8.2 Booster-Wärmepumpe (L8)

Die Konstellation „WP 2 zieht aus dem Puffer, den WP 1 lädt, und lädt den Brauchwasserpuffer"
rechnet heute bereits über die Rechenebenen. Sie wird in drei Schritten vollwertig:

1. **Temperaturkopplung** — ein Schnittstellenwechsel, kein Wertetausch: `Quelltemperatur()` ist
   heute eine statische Methode, die **vor** der Stundenschleife ein komplettes Jahresprofil
   `float[8760]` liefert und je Modul genau einmal beim Aufbau gerufen wird
   (`WaermequelleClass.cs:582`, `SimulationWaermepumpe.cs:330`, gelesen `:518/:1062`). Für den
   geteilten (Senken-/Kombi-)Puffer wird daraus eine **Stundenabfrage mit Rückkopplung**:
   die Quelltemperatur wird je Stunde **genau einmal** gebildet — unmittelbar vor Phase B der
   Rechenebene der beziehenden Anlage — und gilt für Bedarfs- **und** Ladephase derselben Stunde
   (sonst wäre das Ergebnis nicht reproduzierbar spezifiziert, denn der SOC des Puffers ändert
   sich innerhalb der Stunde mehrfach). Geliefert wird die **Schichttemperatur an der
   Quell-Entnahmehöhe** (`WQ_Anschlusshoehe`, Default oben); ohne Schichtmodell (N = 1) die
   Ein-Zonen-Ersatztemperatur `RL_eff + A/Q_max · (VL_eff − RL_eff)`. Der COP folgt damit dem
   Speicherzustand — die eigentliche physikalische Aufwertung des Boosters. Die Anzeige-Ganglinie
   `Quelltemperaturen` (`SimulationWaermepumpe.cs:128`) wird dadurch vom Eingangs- zum
   **Laufergebnis**. Geltungsbereich: die Kopplung gilt für den **geteilten** Puffer;
   eigenständige Quellspeicher (Erdsonden-Ersatz mit `WQ_Spreizung`, `Init(V, Spreizung, 0, …)`,
   Start voll) behalten die heutige statische Quelltemperatur — ihre VL/RL-Ersatzwerte (Spreizung/0)
   sind keine Speichertemperaturen, eine Zustandsformel wäre dort Scheinphysik. `WQ_Temp`
   übersteuert als manuelle Konstante weiterhin (Bestandsverhalten). Die Wirkungslosigkeit von
   `WQ_Spreizung`/`WQ_Regeneration` beim geteilten Puffer wird im Dialog ausgeblendet statt still
   ignoriert.
2. **Kennlinienrand protokollieren**: Quelltemperaturen oberhalb der obersten Stützstelle sind beim
   Booster der Normalfall — die heutige stille Kappung (`SimulationWaermepumpe.cs:1670`) erhält
   eine Protokollwarnung mit Stundenzahl; Extrapolation bleibt verboten (F13).
3. **Benennung**: Karte und Schema kennzeichnen eine WP mit Quellpuffer, deren Zielpuffer die Klasse
   `Brauchwasser` oder `Kombi` trägt, als **„Booster"** (Badge + Schemabeschriftung). Kein eigener
   Anlagentyp, keine neue Persistenz — eine Anzeigeregel (F9).

### 8.3 Aufräumarbeiten WP-Modul

Mit dem Altpfad-Rückbau (L1) entfallen die zweite Stundenschleife und die bekannten
Altpfad-Defekte. Zusätzlich: toter Code raus (`potenzialTherm/El`, `WP_Betriebsart`,
Heapsort-Reste, `Rest_Speicher`-Gruppe), `MAX_WP`-Abbruch bekommt einen Fehlertext, Division durch
COP erhält einen Kenndaten-Plausibilitätsguard (COP ≤ 0 → Abbruch mit Meldung statt NaN).

### 8.4 Heizkessel mit Puffer-Quelle (analog Wärmepumpe)

Der Heizkessel kann — wie die Wärmepumpe — einen **Pufferspeicher als Wärmequelle** haben. Die
Mechanik existiert bereits: `QuellenwahlMoeglich` schaltet die Quellenwahl für WP **und**
Heizkessel frei, wobei der Kessel die Typen `""` (Systemrücklauf) und `Pufferspeicher` erhält
(`WaermequelleClass.cs:118, :133`); der Kessel-Quellbezug läuft über dieselben Rechenebenen der
Kaskadenschleife (Kessel-Kaskade, Etappe D5a), und `Form_QuellePufferspeicher` blendet für den
Kessel die Verdampfer-Parameter aus. Das Konzept **verankert diesen Pfad ausdrücklich als
vollwertige Konstellation** und baut ihn in zwei Punkten aus:

1. **Temperaturkopplung analog 8.2**: Der Kessel-Quellanteil rechnet heute mit einer festen
   Vorlauf-/Rücklauf-Spreizung (`SimulationSPK.cs:452-460`). Mit dem Schichtmodell liefert der
   geteilte Quellpuffer stattdessen die **Schichttemperatur an der Quell-Entnahmehöhe**
   (`WQ_Anschlusshoehe`, derselbe Lesezeitpunkt wie in 8.2: je Stunde genau einmal, vor Phase B
   der Rechenebene); ohne Schichtmodell (N = 1) die Ein-Zonen-Ersatztemperatur. Der Quellanteil
   des Kessels folgt damit dem Speicherzustand statt einer Jahreskonstante.
2. **Gleichbehandlung in Dialog und Schema**: Die Quellen-Chips und die Schema-Kette
   („… → Puffer → Kessel → …") zeigen den Kessel-Quellbezug wie den der WP; die Warnkriterien W1–W5
   (6.2) und die harten Guards (Kurzschluss, Ring) gelten unverändert. Weitere Quellentypen
   (Luft, Erdreich, Profile) bleiben dem Kessel bewusst verschlossen — er hat keinen Verdampfer.

Umsetzung in Paket **B1** (gemeinsam mit der WP-Temperaturkopplung — es ist dieselbe
Schnittstelle) und Paket **S2** (Anzeige).

---

## 9. Datenmodell und Migration

**Voraussetzung: Merge des Branches `kostenformulare` — vor Umsetzungsbeginn.** Der Branch
`kostenformulare` (Kostendialoge KD1–KD6, PV-Wirtschaftlichkeit, Energieträger; Stand 26.08.2026:
41 Commits vor `main`, 103 Dateien, +21 317 Zeilen) berührt Kerndateien dieses Konzepts —
`SimulationControl.cs`, `SimulationRunner.cs`, `SimulationPV.cs`, `SchemaMigration.cs`,
`SchemaKatalog.cs`, `DbWerte.cs`, `Form_Simulation_Config.cs`, `ZeitreihenExtraktor.cs`,
`KostenEmissionRechner.cs`, die Wirtschaftlichkeits-Klassen und beide `Resource`-Kataloge.
Der Probe-Merge nach `Pufferspeicher` (Stand 27.08.2026, `git merge-tree`) ist **konfliktfrei**,
weil `Pufferspeicher` noch auf dem `main`-Stand steht — jede Umsetzungswoche ohne Merge erhöht das
Konfliktrisiko in genau diesen Dateien. **Entscheidend:** `kostenformulare` führt die
`SchemaMigration` bereits bis **`ZIEL_VERSION = 44`** fort (u. a. `SCHRITT_38_KOSTENVORLAGEN` …
`SCHRITT_41_PROJEKTPHOTOVOLTAIK`). Vorgehen: `kostenformulare` **zuerst** nach `Pufferspeicher`
mergen bzw. dessen Landung in `main` abwarten, erst dann mit Paket V0 beginnen.
**Entschieden 27.08.2026:** Es wird die **Landung von `kostenformulare` in `main` abgewartet**;
danach wird `main` nach `Pufferspeicher` gemergt und die Umsetzung beginnt auf dem gemeinsamen
Stand. Bis dahin keine Code-Pakete auf `Pufferspeicher`.

Die Schritte der `SchemaMigration` tragen **ganzzahlige Nummern**, und jeder erfolgreiche Schritt
hebt den Marker `Tab_Applikation.SchemaVersion` einzeln an (`SchemaMigration.cs:1825-1837, :2400`;
ADR-001) — Buchstaben-Teilschritte gibt es nicht. Für dieses Konzept wird der **Nummernblock
45–51** reserviert (38–44 sind durch `kostenformulare` vergeben, siehe oben); jeder Schritt gehört
zu genau **einem** Auslieferungspaket und läuft unmittelbar mit ihm aus (Schema vor Code desselben
Pakets, wie in den Paketen 1–9). Die Nummern folgen der Auslieferungsreihenfolge aus Kapitel 13.
Verhaltensneutrale DML-Vorbelegungen wie bisher.

| Schritt | Paket | Inhalt |
|---|---|---|
| 45 | K1 | `Z_ProjektWaermebedarf.Kanal` (Kanalzuordnung externer Wärmeganglinien, Vorbelegung `Heizung`, F18) |
| 46 | K2 | `Tab_Pufferspeicher`: Klassen-Set-Flags `Nutzung_Heizung`, `Nutzung_Brauchwasser`, `Nutzung_Prozess` mit DML-Migration aus `Verwendung` (Heizung → {H}, Brauchwasser → {B}, Kombi → {H, B}; `Verwendung` wird Lese-Altlast) · `Tab_Einstellungen.Kanal_Knappheitsreihenfolge` (TEXT, Default `BRAUCHWASSER;PROZESS;HEIZUNG`, zielgenaues UPDATE wegen Ordinal-Lesekette) |
| 47 | S1 | `Z_AnlageSenke` anlegen und migrieren (5.1: Slots → Ränge, Ziel-Werte unverändert + neu `PufferProzess`, `WS_Typ` → `Bedarfsart`, Rang-1-Pflicht, `Ladeprio_PV`-Regel, **R-Prozess**), FK-Beziehungen ohne Löschweitergabe, `Z_AnlagePufferVerbund.ID_Senke`, `KINDER`-/`FK_MAP`-Einträge (5.1), `ReferenzenAufPuffer`/`ReferenzenLoesen` nachziehen |
| 48 | A1 | Altpfad-Stilllegung: **zuerst DML-Übernahme der Betriebstemperaturen** — für jeden Puffer ohne vollständiges Paar in `Tab_Pufferspeicher` Vorlauf/Rücklauf aus der zugehörigen `Z_ProjektPufferSp`-Zeile übernehmen (exakt die Vorrangkette aus `SimulationControl.cs:2494-2519`; betroffene Puffer im Migrationshinweis auflisten — sonst fielen sie nach der Stilllegung still auf ΔT = 10 K zurück); dann `Kaskade_Zweikanalig` in Bestandsdaten auf WAHR setzen und aus der Weiche nehmen; `Z_ProjektPufferSp` stilllegen (Brücke `WpSenkeSpiegeln` entfällt) |
| 49 | E1 | Ergebnis-Spalten je Kanal (4.4): `Tab_ErgebnisEnergiebedarf`, Erzeuger-Ergebnistabellen, `Tab_ErgebnisPufferspeicher` (+ `ID_Anlage` für Quellspeicherzeilen, + Durchsatzsummen, + `T_oben_*`) |
| 50 | P1 | `Tab_Pufferspeicher`: neue Spalten `Schichten_Anzahl` (Default 1), `Hoehe`, `Lambda_Eff`, `T_Nutz_BW` (Default NULL = RL_eff), `Entnahme_Heizung`, `Entnahme_BW`, `Entnahme_Prozess`, `Ladeleistung_Max`, `Entladeleistung_Max` (Defaults verhaltensneutral) |
| 51 | Q1 | `Tab_Energieanlagen.WQ_Anschlusshoehe`; `Tab_Quellprofil`/`Tab_QuellprofilDaten` (8.1); Tagesprofil-Ablage |

Regeln aus dem Bestand, die weitergelten: neue Steuerwerte deutsch und eingefroren in `DbWerte`
(Drei-Schichten-Regel), neue Beziehungen über IDs (nie Textfelder), TEXT-Feldbreiten gegen die
stille Access-UPDATE-Falle prüfen, `Tab_Einstellungen` nur mit zielgenauen UPDATEs erweitern
(Ordinal-Lesekette), Access-Feldgrenze (255 Spalten je Tabelle) — genau deshalb Zuordnungstabelle
statt Spaltenreihen. Die Ganglinien-Ablage in der DB ist gegen die 2-GB-Grenze zu bemessen
(8760 Zeilen je Profil; Muster `Tab_StromganglinieDaten` als Vergleichsmaßstab).

---

## 10. Oberfläche (Design bleibt, Anpassung wo erforderlich)

| Stelle | Anpassung |
|---|---|
| `Form_Simulation_Config` (Karten + Schema) | **Design unverändert.** Erzeugerkarte: Senken-Chips als Kette (5.3), Booster-Badge (8.2), Quelle „Luft" fest bei Luft-Wasser (8.1), Kessel-Quellkette (8.4). SpeicherKarte: Klassen-Set-Badges (H/B/P statt eines Verwendungs-Badges), Schicht-Badge („4 Schichten"), T_oben in der Detailansicht |
| `SchemaAnsicht`/`SchemaModell` | dritter Abnehmerknoten **Prozesswärme** in der Abnehmerspalte (eigene Kantenfarbe), `BedientKanal`-Ableitung indiziert; feste Spaltenbreiten durch inhaltsabhängige Breitenrechnung ersetzen |
| `Form_Waermesenke` | Umbau auf geordnete Senkenliste (5.3); Pufferauswahl ungefiltert, nach Klassen-Set gruppiert, mit Warnkriterien W1–W5 (6.2) |
| `Form_PufferSp_Projekt` | Klassen-Set-Flags (drei Häkchen) statt der Verwendungs-ComboBox; neue Gruppe „Schichtung" (Anzahl, Höhe, λ_eff, T_Nutz BW, Entnahmehöhen — nur sichtbar bei N > 1); Lade-/Entladeleistung |
| `Form_Quellprofil` | Betriebsart Monat/Tag (8.1) |
| `Form_KonfigPufferspeicher` | **entfällt** mit Schritt 48 / Paket A1 (Alt-Zuordnung stillgelegt) |
| Ergebnisanzeigen / `NavigatorWaerme` / Bericht | drei Bedarfs-/Deckungsgrößen; Puffer-Kennzahlen ohne `puffer_wp`-Alias (6.3); Detailansicht-Formeln = Runner-Formeln (V0-7) |

Lokalisierung: alle neuen sichtbaren Texte über `MyResource.Resource.*` in beiden Sprachen,
Steuerwerte in `DbWerte`, Nachtrag in `Lokalisierung_Katalog.md` — die im Simulationsbereich
geltende Drei-Schichten-Regel unverändert.

---

## 11. Verifikation und Referenzbasis

### 11.1 Referenzbasis neu einfrieren — vor allem anderen

Aktuelle Basis ist `2026-08-19_B6` (neun Projekte, 216 CSV). Sie trägt den kommenden Umbau nicht:
Die fünf Nicht-BHKW-Projekte sind mit Flag AUS eingefroren, die vier BHKW-Projekte (1017, 1018,
1024, 1030) rechnen seit Paket BHKW-Regulär immer über die Speicherstufe (Flag-AN-Läufe existieren
nur als A/B-Vergleiche in K3/D5a/D5b, nicht als Basis); die in `Referenzlaeufe\LIESMICH.md`
geführte Liste „Was diese Basis nicht absichert" nennt genau die hier relevanten Lücken, und der
jüngste x64-Lauf (2026-08-22) fuhr bereits einen abweichenden Projektsatz (1012/1026 statt
1008/1018). Zudem enthält die Basis die fehlerhafte Mehrgebäude-Rechnung von 1008 als Sollwert
(V0-1). **Neue Basis:** die neun Bestandsprojekte — Projektsatz zuvor festlegen — **plus** vier
neue Referenzprojekte (Mehrgebäude; zwei Puffer je Kanal; Prozesswärme mit eigenem Puffer;
Booster-Kette mit Kombi-Speicher), alle über die Speicherstufe.

### 11.2 Byte-Vergleich, wo er trägt; Toleranzvergleich, wo nicht

Verhaltensneutral nachzuweisen (byte-gleich): Schichtmodell mit N = 1 (einschließlich
Rückfall-ΔT-Projekt, 7.3); Senkentabellen-Migration; Klassen-Set-Migration (`Verwendung` → Flags);
`T_Nutz = RL_eff`. Bewusst ergebnisändernd (dokumentierter Vorher/Nachher-Vergleich je
Referenzprojekt): V0-Fixes, Dreikanal-Herauslösung der Prozesswärme (mit Migrationsregel
R-Prozess), **Netzverlust-Umverteilung auf die Kanäle** (F2), **Kalender-Vereinheitlichung** (F3),
Altpfad-Stilllegung, Booster-/Kessel-Temperaturkopplung.

### 11.3 Energieprobe je Stunde

Als bleibende Selbstprüfung (heute nur im BHKW): Summe der Kanalabzüge + Speicherinhaltänderung +
Verluste = Summe der Erzeugung, je Stunde, Toleranz 1-ULP-Klasse; im Debug-Build als Selbsttest
(Muster `Waermekanaele.Selbsttest`), im Release als Protokollprobe am Laufende. Dazu die
Schicht-Invarianten aus 7.3: `Σ Schichtenergie == min(SOC, Q_max)`, `T` monoton nach
Inversionsmischung, `RL_eff ≤ T[i] ≤ VL_eff`.

### 11.4 Regressionsfallen aus der Ist-Analyse

Ausdrücklich in die Prüfliste: Vollzyklen bei Durchreichbetrieb, WP-Restbedarf Skalar vs. Ganglinie
(bewusst verschieden — bleibt und wird dokumentiert), `SolarCalculator.lastCosTheta` (statisches
Feld) vor jeder Parallelisierung.

---

## 12. Rückfragen an den Produktverantwortlichen

„✔" = entschieden (mit Datum), „◉" = Empfehlung bei noch offener Frage. Stand Fassung 2 sind
**alle Fragen außer F11 und F13 entschieden**; zu beiden steht unten die Erläuterung.

**F1 — Altpfad abschaffen?** ✔ **Entschieden 27.08.2026: Ja** (L1). Konsequenz: Bestandsprojekte
ohne Flag ändern ihr Ergebnis; Referenzbasis wird neu eingefroren, Ergebnisänderungen je
Referenzprojekt dokumentiert.

**F2 — Netzverluste im Dreikanalmodell?** ✔ **Entschieden 27.08.2026: sofort anteilig** — je
Stunde proportional zu den Kanalbedarfen, bei Gesamtbedarf 0 auf den Heizkanal (4.2). Bewusste,
dokumentierte Ergebnisänderung für alle Bestandsprojekte mit Brauchwasser-/Prozessanteil (11.2).

**F3 — Kalender vereinheitlichen?** ✔ **Entschieden 27.08.2026: Ja, vereinheitlichen** — alle
drei Konventionen (Bedarfs-Profilpfad „1. Januar = Sonntag", Gebäudepfad `Tab_Klimadaten.WE`,
WP-Quellprofil „nächstes Nicht-Schaltjahr") werden in Paket K1 auf den Klimadaten-Kalender
gezogen (4.2). Energiewirkungsfrei, aber jede Profil-Stundenganglinie verschiebt sich —
dokumentierte Ergebnisänderung (11.2). Das neue 365-Tage-Quellprofil (8.1) ist kalenderunabhängig.

**F4 — Senkenanzahl?** ✔ **Entschieden 27.08.2026: Zuordnungstabelle mit unbegrenztem Rang**
(praktisch 1–4, UI bietet Hinzufügen bis 4; Ausnahme BHKW → F11).

**F5 — Ziel-Vereinfachung und Kombi?** ✔ **Entschieden 27.08.2026: Alternative** — die sechs
Zielwerte bleiben (+ neu `PufferProzess`), und die Pufferklassen werden ein **Klassen-Set** aus
drei Nutzungs-Flags (`Kombi` = {Heizung, Brauchwasser}, beliebige Kombinationen wie
{Heizung, Prozess} möglich). Eingearbeitet in L5/L6, 5.1, 6.1; Migration Schritt 46/47.

**F6 — Freie Zuordnung: Reichweite des Hinweises?** ✔ **Entschieden 27.08.2026: Konstellationen
nur, wenn sinnvoll — Ausschlusskriterien mit Warnung.** Umgesetzt als Warnkriterienkatalog W1–W6
(6.2): definierte unplausible Konstellationen erzeugen Dialog- und Protokollwarnung; hart gesperrt
bleiben Kurzschluss, Ring, leeres Klassen-Set und Schichtung am Verbund-Leitspeicher.

**F7 — Schichtmodell-Tiefe?** ✔ **Entschieden 27.08.2026: wie vorgeschlagen** — N je Speicher
konfigurierbar 1…10, Default 1; nur Senken-/Kombi-Speicher; `T_Nutz` zunächst nur für
Brauchwasser; keine Schicht-Persistenz je Stunde.

**F8 — Schichtmodell-Randfragen?** ✔ **Entschieden 27.08.2026: wie empfohlen** — die
Herkunftsrechnung bleibt je Speicher; Verbund und Schichtung schließen sich je Rechenspeicher aus;
Default N = 1 hält den Bestand stabil, Ergebnisänderung durch N > 1 ist eine bewusste
Anwenderentscheidung je Speicher.

**F9 — Booster als Anzeigeregel oder eigener Typ?** ✔ **Entschieden 27.08.2026: Anzeigeregel** +
Validierung (kein neuer Anlagentyp, kein neues Schema).

**F10 — Kanal-Knappheitsregel?** ✔ **Entschieden 27.08.2026: projektweite Übersteuerung über
eine neue `Tab_Einstellungen`-Spalte** `Kanal_Knappheitsreihenfolge` (Default
`BRAUCHWASSER;PROZESS;HEIZUNG`, Schritt 46; Details 4.3). Mit dem Klassen-Set (F5) ist auch die
Prozess-Position ab K2 wirksam.

**F11 — BHKW-Senkenzahl? — ERLÄUTERUNG, Entscheidung offen.** Das BHKW ist der einzige Erzeuger,
dessen Motoren **gemeinsam je Stufe** zugeschaltet werden: Die drei Fahrweisen entscheiden je
Stunde „wie viele Motoren laufen?" gegen **einen** Wärmeraum (Kanalbedarf + Ladefähigkeit genau
einer Puffersenke, `SimulationBHKW.cs:931-992`). Der Code hat dafür exakt **zwei Senkenplätze**
(`Auftrag_Haupt`/`Auftrag_Zweit`), **ein** Reservierungsfeld (`_reservierterSpeicher`, sichert den
in Phase B verplanten Wärmeraum gegen vorrangige Lader) und `ZweitsenkenRaum()` nur aus dem
zweiten Auftrag (`:831-834, :1088, :1112-1116`). **Konsequenz von „mehr als zwei Senken":** die
Zuschaltung müsste gegen die Summe aller Puffersenken bemessen und die Reservierung zur Liste je
Zielspeicher werden — ein Umbau der Fahrweisenlogik, den Paket 6 als „neue Physik" ausdrücklich
abgelehnt hat. Ohne Umbau wäre die Zuschaltung ab Rang 3 systematisch zu klein und die
Reservierung schützte nur einen Speicher (genau der Verwurf-Fehler N3, gemessen 12,06 MWh).
**Empfehlung:** höchstens zwei Senken je BHKW-Stufe (Rang 1/2), vom Senkendialog erzwungen;
Verallgemeinerung als Ausbaustufe. **Alternative:** Fahrweisen-Umbau jetzt (≈ +3–4 PT in S1,
Ergebnisänderung aller BHKW-Projekte mit Mehrfachsenken).

**F12 — Quellprofile in die DB?** ✔ **Entschieden 27.08.2026: Ja** — 8760er-Profile als
Kopf/Daten-Tabellen, Tagesprofil = 365 Tageswerte als neue Profil-Betriebsart; `WQ_CSV`-Pfad nur
noch Import-Quelle.

**F13 — Kennlinienrand? — ERLÄUTERUNG, Entscheidung offen.** Die WP-Kennlinien stammen aus
Herstellerdaten (Tab_Kenndaten, VDI-3805-Import) und haben Stützstellen typischerweise nur bis
~20 °C Quelltemperatur — eine Booster-WP zieht aber aus einem 30–50 °C warmen Puffer, liegt also
**regelmäßig oberhalb der obersten Stützstelle**. Zwei Umgangsweisen: **(a) Kappung** (Empfehlung):
oberhalb der Tabelle gilt der COP der obersten Stützstelle — konservativ (der echte COP wäre
besser), erfindet keine Herstellerdaten, wird künftig protokolliert statt still
(`SimulationWaermepumpe.cs:1670`); der Booster wird dadurch tendenziell etwas zu schlecht
bewertet. **(b) Extrapolation**: lineare Fortschreibung der Kennlinie über die Tabelle hinaus —
bildet den Temperaturvorteil ab, extrapoliert aber jenseits der Herstellerangaben (Risiko
systematischer Überschätzung von COP und JAZ, keine Datengrundlage). **Dritter Weg** (ergänzend zu
a, kein Ersatz): Hochtemperatur-Kennfelder importieren, wo der Hersteller sie liefert — dann
erübrigt sich die Frage für diese Geräte. **Empfehlung: (a) Kappung + Protokollwarnung.**

**F14 — Detailansicht angleichen?** ✔ **Entschieden 27.08.2026: Ja** (V0-7): Anzeigeformeln =
Runner-Formeln; sichtbare Änderung der angezeigten Deckungsgrade in Bestandsprojekten mit Puffer
wird im Migrationshinweis genannt.

**F15 — V0 als eigenes Vorab-Paket?** ✔ **Entschieden 27.08.2026: Ja**, mit eigenem Referenzlauf
vor/nach je Fix — Ergebnisänderungen (Mehrgebäude ↓, Stromprofile ↑) nicht mit der
Konzeptumstellung vermischen.

**F16 — Prozesswärme-Temperaturniveau?** ✔ **Entschieden 27.08.2026: wie empfohlen** — Stufe 1:
Prozesskanal rein energetisch (wie Heizung/BW heute), Prozess-Puffer mit eigenem VL/RL-Paar
(vorhandene Spalten). Stufe 2 (vorgemerkt): VL/RL je Prozess-Eintrag in `Z_Projekt_Prozesswaerme`
mit Wirkung auf WP-Kennfeld und Erzeuger-Eignung.

**F17 — Migrationsregel R-Prozess bestätigt?** ✔ **Entschieden 27.08.2026: Ja, automatisch.**
Bei der Migration (Schritt 47) erhält jede Anlage mit Direktsenke Heizkreis und Bedarfsart
`Beides` oder `Heizung` eine zusätzliche Senkenzeile `Ziel='Prozesswaerme'` **nach** ihrer
Heizkreiszeile (4.4); die Rangfolge „Heizung vor Prozess je Anlage" ist damit festgelegt.

**F18 — Kanalzuordnung externer Wärmeganglinien?** ✔ **Entschieden 27.08.2026: Ja** — neue Spalte
`Z_ProjektWaermebedarf.Kanal` (Schritt 45), Vorbelegung `Heizung` (altverhaltenserhaltend); der
Anwender kann eine importierte Ganglinie damit als Brauchwasser- oder Prozesslast deklarieren.

---

## 13. Umsetzungspakete und Aufwand

Reihenfolge ist Abhängigkeitsreihenfolge; jedes Paket einzeln lieferbar und verifizierbar.

| Paket | Inhalt | Kapitel / Schritt | Aufwand (PT) |
|---|---|---|---|
| **V0** | Bestandsfehler + Referenzbasis: V0-1…V0-9, vier neue Referenzprojekte, Basis neu einfrieren | 2.3, 11.1 | 6–8 |
| **K1** | Dreikanal-Bedarf: `Kanalsatz`, Kanalbildung ohne Residuum, **Netzverluste anteilig** (F2), **Kalender-Vereinheitlichung** (F3), gemeinsame Profilroutine (zwei Quellmodi), `DataRepository`-Umstellung der Bedarfsrechnung, Ganglinien-Kanalzuordnung, Energieprobe | 4 · Schritt 45 | 7–9 |
| **K2** | Kaskade dreikanalig: `SenkeAbziehen` mit Maske, Entladeordnungen/Durchsatzbudget indiziert, **kanalindizierte Deckungs-/Zurechnungsbuchführung** (4.1/4.4), **Klassen-Set-Flags + Knappheitsreihenfolge-Spalte** (6.1, F10) | 4.3, 6.1 · Schritt 46 | 7–9 |
| **S1** | Senkentabelle `Z_AnlageSenke` + Migration (inkl. R-Prozess) + Ladephasen je Rang + Senkendialog-Umbau + Projektkopie (`KINDER`/`FK_MAP`) | 5 · Schritt 47 | 7–9 |
| **S2** | Freie Zuordnung mit Warnkriterien W1–W5; Schema-/Kartenanpassungen (Prozessknoten, Senkenketten-Chips, Kessel-Quellkette) | 6.2, 10 | 4–5 |
| **A1** | Altpfad-Rückbau: Weiche, Flag, WP-Altschleife, SPK/Solar-Altwege, Temperaturübernahme + `Z_ProjektPufferSp`-Stilllegung, `Form_KonfigPufferspeicher` entfällt, toter Code (8.3) | 3/L1 · Schritt 48 | 6–8 |
| **E1** | Ergebnis/Bericht je Kanal (Persistenz + Anzeige; Engine-Buchführung kommt aus K2); `puffer_wp`-Ablösung; `Tab_ErgebnisPufferspeicher`-Erweiterung | 4.4, 6.3 · Schritt 49 | 4–5 |
| **P1** | Schichtmodell-Kern: Schema (Schritt 50), N Schichten als zweite Zustandsebene, Ausgleich/Verluste/Inversion, N=1-Byte-Nachweis (inkl. Rückfall-ΔT-Projekt), Lade-/Entladeleistung, Verbund-Guard (W6) | 7 · Schritt 50 | 8–10 |
| **P2** | Schicht-Konfiguration UI: `Form_PufferSp_Projekt`, SpeicherKarte, Ergebnis-Kennzahlen, Kombi-Zonen | 7.5, 10 | 3–4 |
| **B1** | Booster: Temperaturkopplung WP **und Heizkessel** (Schnittstellenwechsel `Quelltemperatur` → Stundenabfrage, 8.2/8.4), Kennlinien-Protokoll, Badge/Schema, WP-Guards | 8.2–8.4 | 6–8 |
| **Q1** | Quellen-Ausbau: Bauart-Bindung, Tagesprofil, Profile in DB, Schlüssel- statt Indexkopplung | 8.1 · Schritt 51 | 4–6 |
| **L** | Lokalisierung + Dokumentation (Katalog-Nachträge, Migrationshinweis, Fassung 3 dieses Konzepts) | 10 | 2–3 |
| | **Summe** | | **64–84** |

Meilenstein-Schnitte: nach **A1** ist Z1 im Kern erreicht (ein Rechenweg); nach **E1** ist Z3
durchgängig (Bedarf → Kaskade → Ergebnis → Bericht); nach **B1** ist Z4 erreicht. P1 (mit seinem
eigenen Schema-Schritt 50) und P2 sind ab K2 unabhängig von S2/A1/E1 parallelisierbar; P2 setzt P1
voraus.

---

## 14. Risiken

| Risiko | Begegnung |
|---|---|
| **Ergebnisänderungen an Bestandsprojekten** (V0-Fixes, Prozess-Herauslösung, Altpfad-Stilllegung, Detailansicht-Angleich) | getrennte Pakete mit je eigenem Vorher/Nachher-Referenzlauf; Migrationshinweis mit Zahlenbeispielen; keine Vermischung von Fix- und Umbauwirkung |
| **Dreikanal-Umbau berührt jede Modulsignatur** — kein additiver Eingriff | ein Schnitt (K1/K2) statt Etappen-Flags; die Signaturen werden ohnehin angefasst → dabei einheitliche `Stunde_*`-Schnittstelle über alle vier Module herstellen (Z2) |
| **Byte-Regression entfällt als Kriterium**, sobald Kanäle und Schichtmodell rechnen | zweistufiges Kriterium (11.2): byte-gleich wo verhaltensneutral zugesagt, sonst dokumentierter Toleranzvergleich; Energieprobe als bleibende Wache |
| **93/372 `.cs`-Dateien nicht UTF-8**; Kernedateien (SimulationControl, SimulationWaermepumpe, SimulationWaermebedarf, WaermequelleClass, WaermesenkeClass, SchemaMigration) betroffen | byte-sicherer Patchweg (CP1252-Rezept), Diff-Review je Datei |
| **Grep-Fallen**: `..\WindowsFormsApplication1 - Kopie`, `..\mit_Puffer_KI_Lösungsversuch` und `.claude\worktrees\*` enthalten Vollkopien | Suchpfade strikt auf das Anwendungsprojekt begrenzen (bestehende Projektregel, gilt verschärft) |
| **Migrations-Schrittnummern / Parallelbranch `kostenformulare`** | Block 38–44 ist auf `kostenformulare` bereits vergeben (`ZIEL_VERSION = 44`); dieses Konzept reserviert **45–51** (Kapitel 9). Entschieden: Landung von `kostenformulare` in `main` abwarten, dann `main` nach `Pufferspeicher` mergen — der Probe-Merge ist heute konfliktfrei, beide Arbeiten berühren aber dieselben Kerndateien (SimulationControl, SimulationRunner, SchemaMigration, DbWerte, Form_Simulation_Config, Resource) |
| **Schichtmodell-Numerik** (Stabilität, Monotonie) | gekappter Austausch (7.4) ist unbedingt stabil; Invarianten-Selbsttest; N=1-Byte-Nachweis als Anker |
| **Access-Grenzen** (255 Spalten, 2 GB, 32 Indizes) | Zuordnungs- und Kindtabellen statt Spaltenreihen; Ganglinien-Volumen bemessen (9) |
| **Referenzbasis veraltet schon wieder** während der Umsetzung | Basis wird je Paket fortgeschrieben (Lauf-Protokolle unter `Referenzlaeufe\`), nicht erst am Ende |

---

## 15. Zusammenfassung der Schema-Änderungen

**Neue Tabellen:** `Z_AnlageSenke` (5.1) · `Tab_Quellprofil` + `Tab_QuellprofilDaten` (8.1).

**Neue Spalten:** `Tab_Pufferspeicher`: `Nutzung_Heizung`, `Nutzung_Brauchwasser`,
`Nutzung_Prozess` (Klassen-Set, Schritt 46), `Schichten_Anzahl`, `Hoehe`, `Lambda_Eff`,
`T_Nutz_BW`, `Entnahme_Heizung`, `Entnahme_BW`, `Entnahme_Prozess`, `Ladeleistung_Max`,
`Entladeleistung_Max` · `Tab_Einstellungen`: `Kanal_Knappheitsreihenfolge` (F10) ·
`Tab_Energieanlagen`: `WQ_Anschlusshoehe` · `Z_AnlagePufferVerbund`: `ID_Senke` ·
`Z_ProjektWaermebedarf`: `Kanal` (F18) ·
Ergebnistabellen: Kanalspalten (4.4), `Tab_ErgebnisPufferspeicher.ID_Anlage` + Durchsatz- und
Temperatur-Kennzahlen.

**Neue Steuerwerte (`DbWerte`, deutsch, eingefroren):** `WS_ZIEL_PROZESS = "Prozesswaerme"`,
`WS_ZIEL_PUFFER_PROZESS = "PufferProzess"` — die übrigen fünf Zielwerte bleiben unverändert
(F5-Alternative). Die Knappheitsreihenfolge nutzt sprachneutrale ASCII-Schlüssel
(`BRAUCHWASSER;PROZESS;HEIZUNG`).

**Stillgelegt (Lese-Altlast nach Migration):** `Tab_Einstellungen.Kaskade_Zweikanalig`
(nach Schritt 48) · `Z_ProjektPufferSp` (nach DML-Temperaturübernahme, Schritt 48) ·
`Tab_Pufferspeicher.Verwendung` (nach Klassen-Set-Migration, Schritt 46) ·
`Tab_Energieanlagen`: `WS_Ziel`, `WS_ID_Puffer`, `WS_Typ`, `WS_Ladeprio`, `WS_Ladegrenze`,
`WS_Ladeprio_PV` sowie `WS_Ziel2`, `WS_ID_Puffer2`, `WS_Ladeprio2`, `WS_Ladegrenze2`
(ein `WS_Ladeprio_PV2` existiert nicht — die PV-Sonderregel hing an der Hauptsenke) ·
`WQ_CSV` (nur noch Importquelle) · `WQ_Puffer` (Bezeichner-Altweg, war bereits abgelöst).

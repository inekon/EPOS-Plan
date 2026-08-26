# Konzept: Brauchwasser, Heizung und Pufferspeicher — Dreikanalbilanz, Schichtspeicher, Booster-Wärmepumpe

**Fassung 1** · Stand 26.08.2026 · Branch `Pufferspeicher` · Status: **Entwurf zur Abstimmung** — die
Rückfragen in Kapitel 12 sind vor Umsetzungsbeginn zu entscheiden; zu jeder steht eine Empfehlung.

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
| L3 | **Kein Residuum mehr**: jeder Kanal wird direkt aus seiner Quelle gebildet (Heizung = Gebäude + externe Lastgänge + Netzverluste; Brauchwasser = Brauchwasserprofile; Prozess = Prozessprofile). Invariante: Kanalsumme = bisheriger Gesamtbedarf | umsetzen | F2 (Netzverluste) |
| L4 | **Senken als Zuordnungstabelle** `Z_AnlageSenke` (n Senken je Anlage mit Rang) statt der Spaltenpaare `WS_*`/`WS_*2` | umsetzen | F4 |
| L5 | **Senkenziel wird vereinfacht** auf drei Werte: `Heizkreis`, `Prozesswaerme`, `Puffer` (+ Puffer-ID). Die Kanalzugehörigkeit einer Puffersenke folgt **allein aus der Klasse des Puffers** — die heutige Doppelwahrheit (Ziel-Klasse *und* Puffer-Verwendung) entfällt, und „freie Zuordnung" ist konstruktiv kein Widerspruch mehr. Die sechs geforderten Senkentypen erscheinen unverändert in der Oberfläche (Puffer-Auswahl gruppiert nach Klasse) | umsetzen | F5 |
| L6 | **Pufferklassen**: `Verwendung` erhält den vierten Wahlwert `Prozess`; `Kombi` bleibt der feste Wert „Heizung + Brauchwasser". Die Zuordnungsprüfung wird von Sperre auf **Hinweis** umgestellt (Dialogwarnung + Protokollwarnung); hart gesperrt bleiben nur Kurzschluss (Quelle = eigene Senke) und Ring | umsetzen | F6 |
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
             + Netzverluste (Regel F2)
BRAUCHWASSER = brauchwasserwerte                                  (Projektfilter, V0-3 saniert)
PROZESS      = prozesswerte                                       (Projektkopie, V0-3/V0-4 saniert)
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

**Netzverluste (F2):** Vorbelegung bleibt altverhaltenserhaltend **vollständig auf dem Heizkanal**
(Entscheidung O2 der Fassung 12). Die Zuordnung wird aber eine explizite, dokumentierte Stelle mit
einem Erweiterungspunkt (proportionale Verteilung je Stunde bzw. je Kanal parametrierbar) statt
eines Konstruktionsnebeneffekts.

**Gemeinsame Profilroutine.** Die drei fast identischen Kopien des Profilalgorithmus (Prozess
`:667`, Brauchwasser `:773`, Strom `SimulationStrombedarf.cs:161`) werden zu **einer** Routine
„12 Monatswerte × 168-h-Wochenprofil → 8760" mit einheitlichen Fehlerpfaden zusammengezogen.
Die Routine kennt **zwei Quellmodi als expliziten Aufrufparameter**: *Projektrechnung* (Projektkopie,
Pflichtfilter `ID_Projekt`) und *Katalogvorschau* (`_STAMM`-Tabellen, die kein `ID_Projekt` tragen —
heutige Aufrufer: Admin-Dialoge und Vorschaupfade, `SimulationWaermebedarf.cs:804-809`); die
bisherige Ableitung aus `list != null` entfällt. Einheitliche Fehlerpfade: kein Treffer →
Protokollwarnung statt stiller 0, Monatssumme 0 → Warnung statt NaN (`BhkwPlan.cs:193`),
Klassenfelder-Zwischenspeicher (`monats_waerme`, `wochen_waerme`, `temp`) entfallen. Die Bedarfsrechnung wechselt dabei von `RecordSet`-String-SQL auf
`DataRepository` mit `?`-Parametern (Projektvorgabe). Der Kalenderbruch (Profilpfad: 1. Januar =
Sonntag, `BhkwPlan.cs:180`; Gebäudepfad: `Tab_Klimadaten.WE`) wird als Rückfrage F3 behandelt.

### 4.3 Abzugsregel und Kanal-Rangfolge

`SenkeAbziehen` bleibt die eine Kanalregel für alle Erzeuger, Heizstab und Speicherentladung.
Statt der drei Fälle `Warmwasser | Heizung | Beides` arbeitet sie mit einer **Kanalmaske** je
Direktsenke (5.2). Bei mehrelementiger Maske gilt eine feste **Knappheitsreihenfolge**; Vorbelegung:

```
BRAUCHWASSER  →  PROZESS  →  HEIZUNG
```

Warmwasser-Vorrang wie heute (Komfortkriterium), Prozess vor Heizung (Produktionsausfall wiegt
schwerer als Raumkomfort). Dieselbe Reihenfolge gilt für die Entladung eines Kombi-Speichers
(heutige Regel K-1, verallgemeinert). **Wirksamkeit in Stufe 1:** mehrelementige Masken entstehen
nur aus `Bedarfsart='Beides'` ({Brauchwasser, Heizung}) und aus dem Kombi-Speicher — die
Prozess-Position wird erst mit dem freien Klassen-Set (6.1/F5) auswertbar und ist bis dahin eine
vorgehaltene Festlegung. Die Reihenfolge ist deshalb zunächst **fest verdrahtet**; eine
projektweite Übersteuerung ist Ausbaustufe (F10).

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
  Ziel            TEXT(50)      'Heizkreis' | 'Prozesswaerme' | 'Puffer'     (DbWerte, deutsch, eingefroren)
  Bedarfsart      TEXT(50)      nur bei Ziel='Heizkreis': 'Beides' | 'Warmwasser' | 'Heizung'
  ID_Puffer       LONG NULL     nur bei Ziel='Puffer': FK → Tab_Pufferspeicher.ID
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

Die sechs geforderten Senkentypen bilden sich so ab: `Heizkreis` und `Prozesswaerme` sind
Direktsenken; `Puffer Heizung / Brauchwasser / Prozess / Kombi` ist `Ziel='Puffer'` + ein Puffer
der jeweiligen Klasse. Die Oberfläche zeigt weiterhin sechs Einträge (Pufferliste nach Klasse
gruppiert), gespeichert wird die vereinfachte Form. **Migration (Schritt 46):**

- je Anlage wird `WS_Ziel`/… als Rang 1 und `WS_Ziel2`/… als Rang 2 übernommen
  (`PufferHeizung|PufferBrauchwasser|PufferKombi` → `Puffer` + vorhandene `WS_ID_Puffer*`);
  `WS_Typ` wandert als `Bedarfsart` in die Rang-1-Zeile; Anlagen ganz ohne `WS_Ziel` erhalten eine
  Rang-1-Zeile `Heizkreis/Beides` (Invariante oben);
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

### 6.1 Drei Klassen + Kombi (L6)

`Tab_Pufferspeicher.Verwendung`: `Heizung | Brauchwasser | Prozess | Kombi` (+ Laufzeitrolle
`Quelle`). `Kombi` bleibt fest „Heizung + Brauchwasser"; ein frei wählbares Klassen-Set (z. B.
„Heizung + Prozess") ist als Ausbaustufe vorgemerkt und wird durch die indizierte Kanalmenge
(`BedientKanal(int)` über eine Klassenmenge) vorbereitet, aber nicht jetzt gebaut (F5).

### 6.2 Freie Zuordnung mit Hinweis

- `PufferPasst`/`PasstZuFilter` entfallen in ihrer sperrenden Form — die Puffer-Auswahl zeigt
  **alle** Projekt-Puffer, gruppiert nach Klasse.
- Ein **Hinweis** (Dialog, nicht blockierend, plus Protokollwarnung im Lauf) erscheint, wenn
  (a) der `Speichertyp` des Katalogs (z. B. Brauchwasserspeicher) nicht zur gewählten `Verwendung`
  passt, oder (b) die Temperaturlage unplausibel ist (Erzeuger-Vorlauf < Puffer-Solltemperatur —
  mit dem Schichtmodell erstmals prüfbar).
- **Hart gesperrt bleiben**: Kurzschluss (Quellpuffer = eigene Senke) und Ring in der
  Kaskadenkette — beide physikalisch begründet, beide heute schon Dialog- und Engine-Guards.
- Da die Kanalzugehörigkeit allein aus der Puffer-Klasse folgt (L5), gibt es den Fall „Puffer wird
  gegen seine Klasse entladen" nicht mehr: Wer einen Brauchwasserpuffer für Heizung nutzen will,
  stellt seine `Verwendung` auf `Heizung` (oder `Kombi`) — genau dort sitzt der Hinweis.

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

## 8. Wärmepumpe: Quellen und Booster (Z4)

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
mergen (bzw. dessen Landung in `main` abwarten), erst dann mit Paket V0 beginnen.

Die Schritte der `SchemaMigration` tragen **ganzzahlige Nummern**, und jeder erfolgreiche Schritt
hebt den Marker `Tab_Applikation.SchemaVersion` einzeln an (`SchemaMigration.cs:1825-1837, :2400`;
ADR-001) — Buchstaben-Teilschritte gibt es nicht. Für dieses Konzept wird der **Nummernblock
45–50** reserviert (38–44 sind durch `kostenformulare` vergeben, siehe oben); jeder Schritt gehört
zu genau **einem** Auslieferungspaket und läuft unmittelbar mit ihm aus (Schema vor Code desselben
Pakets, wie in den Paketen 1–9). Die Nummern folgen der Auslieferungsreihenfolge aus Kapitel 13.
Verhaltensneutrale DML-Vorbelegungen wie bisher.

| Schritt | Paket | Inhalt |
|---|---|---|
| 45 | K1 | `Z_ProjektWaermebedarf.Kanal` (Kanalzuordnung externer Wärmeganglinien, Vorbelegung `Heizung`, F18) |
| 46 | S1 | `Z_AnlageSenke` anlegen und migrieren (5.1: Slots → Ränge, `WS_Typ` → `Bedarfsart`, Rang-1-Pflicht, `Ladeprio_PV`-Regel, **R-Prozess**), FK-Beziehungen ohne Löschweitergabe, `Z_AnlagePufferVerbund.ID_Senke`, `KINDER`-/`FK_MAP`-Einträge (5.1), `ReferenzenAufPuffer`/`ReferenzenLoesen` nachziehen |
| 47 | A1 | Altpfad-Stilllegung: **zuerst DML-Übernahme der Betriebstemperaturen** — für jeden Puffer ohne vollständiges Paar in `Tab_Pufferspeicher` Vorlauf/Rücklauf aus der zugehörigen `Z_ProjektPufferSp`-Zeile übernehmen (exakt die Vorrangkette aus `SimulationControl.cs:2494-2519`; betroffene Puffer im Migrationshinweis auflisten — sonst fielen sie nach der Stilllegung still auf ΔT = 10 K zurück); dann `Kaskade_Zweikanalig` in Bestandsdaten auf WAHR setzen und aus der Weiche nehmen; `Z_ProjektPufferSp` stilllegen (Brücke `WpSenkeSpiegeln` entfällt) |
| 48 | E1 | Ergebnis-Spalten je Kanal (4.4): `Tab_ErgebnisEnergiebedarf`, Erzeuger-Ergebnistabellen, `Tab_ErgebnisPufferspeicher` (+ `ID_Anlage` für Quellspeicherzeilen, + Durchsatzsummen, + `T_oben_*`) |
| 49 | P1 | `Tab_Pufferspeicher`: neue Spalten `Schichten_Anzahl` (Default 1), `Hoehe`, `Lambda_Eff`, `T_Nutz_BW` (Default NULL = RL_eff), `Entnahme_Heizung`, `Entnahme_BW`, `Entnahme_Prozess`, `Ladeleistung_Max`, `Entladeleistung_Max` (Defaults verhaltensneutral). — Der neue `Verwendung`-Wert `Prozess` (Paket K2) braucht **keinen** Migrationsschritt: die Spalte ist TEXT, der Wert kommt nur über `DbWerte` und die Auswahlliste hinzu |
| 50 | Q1 | `Tab_Energieanlagen.WQ_Anschlusshoehe`; `Tab_Quellprofil`/`Tab_QuellprofilDaten` (8.1); Tagesprofil-Ablage |

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
| `Form_Simulation_Config` (Karten + Schema) | **Design unverändert.** Erzeugerkarte: Senken-Chips als Kette (5.3), Booster-Badge (8.2), Quelle „Luft" fest bei Luft-Wasser (8.1). SpeicherKarte: Klassen-Badge um `Prozess` erweitert, Schicht-Badge („4 Schichten"), T_oben in der Detailansicht |
| `SchemaAnsicht`/`SchemaModell` | dritter Abnehmerknoten **Prozesswärme** in der Abnehmerspalte (eigene Kantenfarbe), `BedientKanal`-Ableitung indiziert; feste Spaltenbreiten durch inhaltsabhängige Breitenrechnung ersetzen |
| `Form_Waermesenke` | Umbau auf geordnete Senkenliste (5.3); Pufferauswahl ungefiltert, nach Klasse gruppiert, mit Hinweislogik (6.2) |
| `Form_PufferSp_Projekt` | Verwendung um `Prozess`; neue Gruppe „Schichtung" (Anzahl, Höhe, λ_eff, T_Nutz BW, Entnahmehöhen — nur sichtbar bei N > 1); Lade-/Entladeleistung |
| `Form_Quellprofil` | Betriebsart Monat/Tag (8.1) |
| `Form_KonfigPufferspeicher` | **entfällt** mit Schritt 47 / Paket A1 (Alt-Zuordnung stillgelegt) |
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
Rückfall-ΔT-Projekt, 7.3); Senkentabellen-Migration; Ziel-Vereinfachung; `T_Nutz = RL_eff`.
Bewusst ergebnisändernd (dokumentierter Vorher/Nachher-Vergleich je Referenzprojekt): V0-Fixes,
Dreikanal-Herauslösung der Prozesswärme (mit Migrationsregel R-Prozess), Altpfad-Stilllegung,
Booster-Temperaturkopplung.

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

Jede Frage mit Empfehlung; „◉" = Empfehlung. Die Antworten werden als Entscheidungen E1… in
Fassung 2 eingearbeitet.

**F1 — Altpfad abschaffen?** ◉ Ja (L1). Konsequenz: Bestandsprojekte ohne Flag ändern ihr
Ergebnis; Referenzbasis wird neu eingefroren, Ergebnisänderungen je Referenzprojekt dokumentiert.
Alternative: Altpfad behalten → jede Neuerung dreifach, Dreikanal im Altpfad nicht darstellbar.

**F2 — Netzverluste im Dreikanalmodell?** ◉ Vorbelegung unverändert 100 % Heizkanal
(altverhaltenserhaltend), aber als explizite, parametrierbare Zuordnung (Erweiterung: anteilig je
Stundenbedarf; nur auf Stunden mit Bedarf). Alternative: sofort anteilig → stille Ergebnisänderung
aller Bestandsprojekte.

**F3 — Kalender vereinheitlichen?** Es laufen **drei** Kalenderkonventionen nebeneinander:
Bedarfs-Profilpfad (1. Januar = Sonntag, `BhkwPlan.cs:180`), Gebäudepfad (`Tab_Klimadaten.WE`)
und WP-Quellprofil (Wochentag des 1. Januar aus dem nächsten Nicht-Schaltjahr, Montag = 0,
`WaermequelleClass.cs:934-938`). ◉ In dieser Stufe **nicht** anfassen (reine Verteilungsänderung
ohne Energiewirkung, aber jede Stundenganglinie ändert sich); als eigener Punkt hinter das
TWW-Zapfprofile-Konzept. Das neue 365-Tage-Quellprofil (8.1) ist kalenderunabhängig und von der
Frage nicht betroffen.

**F4 — Senkenanzahl?** ◉ Zuordnungstabelle mit unbegrenztem Rang (praktisch 1–4), UI bietet
Hinzufügen bis 4. Alternative fester dritter Slot: erneut Spaltenreihen, vom Spaltenkatalog
bereits verworfen.

**F5 — Ziel-Vereinfachung und Kombi?** ◉ `Ziel ∈ {Heizkreis, Prozesswaerme, Puffer}`, Klasse nur
am Puffer; `Kombi` bleibt fester Wert. Alternative (sechs Zielwerte behalten + Klassen-Set am
Puffer): mehr Kombinatorik, zweite Wahrheit bleibt.

**F6 — Freie Zuordnung: Reichweite des Hinweises?** ◉ Hinweis im Dialog (nicht blockierend, mit
Begründungstext) **und** Protokollwarnung im Lauf; hart bleiben Kurzschluss und Ring. Zu bestätigen:
darf wirklich jede Konstellation gerechnet werden (z. B. 35-°C-Erzeuger lädt 60-°C-BW-Puffer — mit
Schichtmodell sichtbar wirkungslos, ohne still schönfärbend)?

**F7 — Schichtmodell-Tiefe?** ◉ N je Speicher konfigurierbar 1…10, Default 1; nur Senken-/
Kombi-Speicher; `T_Nutz` zunächst nur für Brauchwasser; keine Schicht-Persistenz je Stunde.
Alternativen: festes N (weniger flexibel), Zwei-Zonen-Modell (kann Booster-Temperatur nicht
liefern), Persistenz je Schicht (Datenvolumen).

**F8 — Schichtmodell-Randfragen?** ◉ Herkunftsrechnung bleibt je Speicher; Verbund und Schichtung
schließen sich je Rechenspeicher aus; Ergebnisänderung durch N > 1 ist eine bewusste
Anwenderentscheidung je Speicher (Default N = 1 hält Bestand stabil).

**F9 — Booster als Anzeigeregel oder eigener Typ?** ◉ Anzeigeregel + Validierung (kein neuer
Anlagentyp, kein neues Schema). Alternative eigener Betriebsmodus „Booster": mehr Persistenz ohne
Rechenmehrwert.

**F10 — Kanal-Knappheitsregel?** ◉ Brauchwasser → Prozess → Heizung, zunächst **fest verdrahtet**
(eine projektweite Übersteuerung wäre eine neue `Tab_Einstellungen`-Spalte und ist Ausbaustufe).
In Stufe 1 ist nur die Position Brauchwasser↔Heizung wirksam (4.3); die Prozess-Position wird mit
dem freien Klassen-Set (F5) scharf. Zu bestätigen, ob Prozess vor Heizung dem Anwenderbild
entspricht.

**F11 — BHKW-Einschränkung akzeptiert?** ◉ **Höchstens zwei Senken je BHKW-Stufe (Rang 1/2)**,
vom Senkendialog erzwungen — die Fahrweisen-Zuschaltung und die Reservierung sind hart auf zwei
Senkenplätze gebaut (5.2); keine Senke je Modul (Paket-6-Entscheidung „keine neue Physik" bleibt).
Verallgemeinerung auf n Puffersenken je Stufe als Ausbaustufe vorgemerkt.

**F12 — Quellprofile in die DB?** ◉ Ja: 8760er-Profile als Kopf/Daten-Tabellen, Tagesprofil = 365
Tageswerte als neue Profil-Betriebsart; `WQ_CSV`-Pfad nur noch Import-Quelle. Zu bestätigen:
„Monat **oder** Tag" heißt 12 Monatswerte oder 365 Tageswerte (nicht 24-h-Gang)?

**F13 — Kennlinienrand?** ◉ Kappung + Protokollwarnung (keine Extrapolation). Alternative
Extrapolation: erfindet Herstellerdaten.

**F14 — Detailansicht angleichen?** ◉ Ja (V0-7): Anzeigeformeln = Runner-Formeln; sichtbare
Änderung der angezeigten Deckungsgrade in Bestandsprojekten mit Puffer wird im Migrationshinweis
genannt.

**F15 — V0 als eigenes Vorab-Paket?** ◉ Ja, mit eigenem Referenzlauf vor/nach je Fix —
Ergebnisänderungen (Mehrgebäude ↓, Stromprofile ↑) nicht mit der Konzeptumstellung vermischen.

**F16 — Prozesswärme-Temperaturniveau?** ◉ Stufe 1: Prozesskanal rein energetisch (wie Heizung/BW
heute), Prozess-Puffer mit eigenem VL/RL-Paar (vorhandene Spalten). Stufe 2 (vorgemerkt): VL/RL je
Prozess-Eintrag in `Z_Projekt_Prozesswaerme` mit Wirkung auf WP-Kennfeld und Erzeuger-Eignung.
Ohne Temperaturniveau bleibt die Trennung eine Mengenbilanz — für Bericht und Speicherführung
ausreichend, für Exergie-Aussagen nicht.

**F17 — Migrationsregel R-Prozess bestätigt?** Bestandsprojekte mit Prozesswärme decken den
Prozessbedarf heute implizit über den Heizkanal. ◉ Bei der Migration (Schritt 46) erhält jede
Anlage mit Direktsenke Heizkreis und Bedarfsart `Beides` oder `Heizung` eine zusätzliche
Senkenzeile `Ziel='Prozesswaerme'` **nach** ihrer Heizkreiszeile (4.4) — die Rangfolge
„Heizung vor Prozess je Anlage" ist damit festgelegt und hier zu bestätigen. Alternative
(keine Regel): der Prozesskanal bliebe in allen Bestandsprojekten ungedeckt — nicht vertretbar.

**F18 — Kanalzuordnung externer Wärmeganglinien?** ◉ Neue Spalte `Z_ProjektWaermebedarf.Kanal`
(Schritt 45), Vorbelegung `Heizung` (altverhaltenserhaltend); der Anwender kann eine importierte
Ganglinie damit als Brauchwasser- oder Prozesslast deklarieren. Alternative: pauschal Heizung
(heutiges Verhalten) — dann bleibt eine importierte Prozessganglinie falsch klassifiziert.

---

## 13. Umsetzungspakete und Aufwand

Reihenfolge ist Abhängigkeitsreihenfolge; jedes Paket einzeln lieferbar und verifizierbar.

| Paket | Inhalt | Kapitel / Schritt | Aufwand (PT) |
|---|---|---|---|
| **V0** | Bestandsfehler + Referenzbasis: V0-1…V0-9, vier neue Referenzprojekte, Basis neu einfrieren | 2.3, 11.1 | 6–8 |
| **K1** | Dreikanal-Bedarf: `Kanalsatz`, Kanalbildung ohne Residuum, gemeinsame Profilroutine (zwei Quellmodi), `DataRepository`-Umstellung der Bedarfsrechnung, Ganglinien-Kanalzuordnung, Energieprobe | 4 · Schritt 45 | 6–8 |
| **K2** | Kaskade dreikanalig: `SenkeAbziehen` mit Maske, Entladeordnungen/Durchsatzbudget indiziert, **kanalindizierte Deckungs-/Zurechnungsbuchführung** (4.1/4.4), Pufferklasse `Prozess`, Knappheitsregel | 4.3, 6.1 | 6–8 |
| **S1** | Senkentabelle `Z_AnlageSenke` + Migration (inkl. R-Prozess) + Ladephasen je Rang + Senkendialog-Umbau + Projektkopie (`KINDER`/`FK_MAP`) | 5 · Schritt 46 | 7–9 |
| **S2** | Freie Zuordnung mit Hinweis; Schema-/Kartenanpassungen (Prozessknoten, Senkenketten-Chips) | 6.2, 10 | 4–5 |
| **A1** | Altpfad-Rückbau: Weiche, Flag, WP-Altschleife, SPK/Solar-Altwege, Temperaturübernahme + `Z_ProjektPufferSp`-Stilllegung, `Form_KonfigPufferspeicher` entfällt, toter Code (8.3) | 3/L1 · Schritt 47 | 6–8 |
| **E1** | Ergebnis/Bericht je Kanal (Persistenz + Anzeige; Engine-Buchführung kommt aus K2); `puffer_wp`-Ablösung; `Tab_ErgebnisPufferspeicher`-Erweiterung | 4.4, 6.3 · Schritt 48 | 4–5 |
| **P1** | Schichtmodell-Kern: Schema (Schritt 49), N Schichten als zweite Zustandsebene, Ausgleich/Verluste/Inversion, N=1-Byte-Nachweis (inkl. Rückfall-ΔT-Projekt), Lade-/Entladeleistung, Verbund-Guard | 7 · Schritt 49 | 8–10 |
| **P2** | Schicht-Konfiguration UI: `Form_PufferSp_Projekt`, SpeicherKarte, Ergebnis-Kennzahlen, Kombi-Zonen | 7.5, 10 | 3–4 |
| **B1** | Booster: Temperaturkopplung (Schnittstellenwechsel `Quelltemperatur` → Stundenabfrage), Kennlinien-Protokoll, Badge/Schema, WP-Guards | 8.2, 8.3 | 5–7 |
| **Q1** | Quellen-Ausbau: Bauart-Bindung, Tagesprofil, Profile in DB, Schlüssel- statt Indexkopplung | 8.1 · Schritt 50 | 4–6 |
| **L** | Lokalisierung + Dokumentation (Katalog-Nachträge, Migrationshinweis, Fassung 2 dieses Konzepts) | 10 | 2–3 |
| | **Summe** | | **61–81** |

Meilenstein-Schnitte: nach **A1** ist Z1 im Kern erreicht (ein Rechenweg); nach **E1** ist Z3
durchgängig (Bedarf → Kaskade → Ergebnis → Bericht); nach **B1** ist Z4 erreicht. P1 (mit seinem
eigenen Schema-Schritt 49) und P2 sind ab K2 unabhängig von S2/A1/E1 parallelisierbar; P2 setzt P1
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
| **Migrations-Schrittnummern / Parallelbranch `kostenformulare`** | Block 38–44 ist auf `kostenformulare` bereits vergeben (`ZIEL_VERSION = 44`); dieses Konzept reserviert **45–50** (Kapitel 9). `kostenformulare` **vor Umsetzungsbeginn** nach `Pufferspeicher` mergen — der Probe-Merge ist heute konfliktfrei, beide Arbeiten berühren aber dieselben Kerndateien (SimulationControl, SimulationRunner, SchemaMigration, DbWerte, Form_Simulation_Config, Resource) |
| **Schichtmodell-Numerik** (Stabilität, Monotonie) | gekappter Austausch (7.4) ist unbedingt stabil; Invarianten-Selbsttest; N=1-Byte-Nachweis als Anker |
| **Access-Grenzen** (255 Spalten, 2 GB, 32 Indizes) | Zuordnungs- und Kindtabellen statt Spaltenreihen; Ganglinien-Volumen bemessen (9) |
| **Referenzbasis veraltet schon wieder** während der Umsetzung | Basis wird je Paket fortgeschrieben (Lauf-Protokolle unter `Referenzlaeufe\`), nicht erst am Ende |

---

## 15. Zusammenfassung der Schema-Änderungen

**Neue Tabellen:** `Z_AnlageSenke` (5.1) · `Tab_Quellprofil` + `Tab_QuellprofilDaten` (8.1).

**Neue Spalten:** `Tab_Pufferspeicher`: `Schichten_Anzahl`, `Hoehe`, `Lambda_Eff`, `T_Nutz_BW`,
`Entnahme_Heizung`, `Entnahme_BW`, `Entnahme_Prozess`, `Ladeleistung_Max`, `Entladeleistung_Max` ·
`Tab_Energieanlagen`: `WQ_Anschlusshoehe` · `Z_AnlagePufferVerbund`: `ID_Senke` ·
`Z_ProjektWaermebedarf`: `Kanal` (F18) ·
Ergebnistabellen: Kanalspalten (4.4), `Tab_ErgebnisPufferspeicher.ID_Anlage` + Durchsatz- und
Temperatur-Kennzahlen.

**Neue Steuerwerte (`DbWerte`, deutsch, eingefroren):** `WS_ZIEL_PROZESS = "Prozesswaerme"`,
`WS_ZIEL_PUFFER = "Puffer"` (Ablösung der drei Puffer-Zielwerte, Migration),
`PSP_VERWENDUNG_PROZESS = "Prozess"`.

**Stillgelegt (Lese-Altlast nach Migration):** `Tab_Einstellungen.Kaskade_Zweikanalig`
(nach Schritt 47) · `Z_ProjektPufferSp` (nach DML-Temperaturübernahme, Schritt 47) ·
`Tab_Energieanlagen`: `WS_Ziel`, `WS_ID_Puffer`, `WS_Typ`, `WS_Ladeprio`, `WS_Ladegrenze`,
`WS_Ladeprio_PV` sowie `WS_Ziel2`, `WS_ID_Puffer2`, `WS_Ladeprio2`, `WS_Ladegrenze2`
(ein `WS_Ladeprio_PV2` existiert nicht — die PV-Sonderregel hing an der Hauptsenke) ·
`WQ_CSV` (nur noch Importquelle) · `WQ_Puffer` (Bezeichner-Altweg, war bereits abgelöst).

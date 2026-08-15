# Paket 6 — BHKW (Umsetzungsprotokoll)

> **Stand nach der Nacharbeit vom 15.08.2026.** Zwei adversariale Reviews haben
> dreizehn Befunde (N1–N13) ergeben; sie sind behoben, gemessen und in
> **[Kapitel 13](#13-nacharbeit-zu-den-review-befunden-n1n13)** dokumentiert. Kapitel 1
> bis 12 beschreiben den Entwurf; wo die Nacharbeit eine Aussage berichtigt hat, steht
> das an Ort und Stelle. Die geänderten Dateien sind von fünf auf **neun** gewachsen
> (Teil 13.1).

Stand: 15.08.2026 · Grundlage: [`Konzept_Simulation_QuellenSenken.md`](Konzept_Simulation_QuellenSenken.md),
Kapitel 3.4 (Ladepriorität und Ladeobergrenzen), 3.5 (PV-Sonderpriorität), 3.6
(Entladereihenfolge), 6.1 (Transportstruktur), 6.3 (Reihenfolge-Invariante samt Nachtrag
zum Bilanzraum), **6.5, zweiter Punkt (BHKW)**, 6.7 (Kompatibilität der Anzeigen), 13.5
(PV-Ladebudget und Zweitsenke) und Kapitel 9 (Paket-Tabelle, Zeile 6) · Vorarbeit:
[`Paket4_EngineKern_Protokoll.md`](Paket4_EngineKern_Protokoll.md) und
[`Paket5_SolarKessel_Protokoll.md`](Paket5_SolarKessel_Protokoll.md) — Speicher-Registry,
Feature-Flag `Kaskade_Zweikanalig`, Kaskadenschleife mit den Phasen A–G, Bilanzraum,
Herkunftsrechnung je Speicher (Nacharbeit N2).

**Nicht committet.** Keine Designer- oder `.resx`-Datei angefasst; die gesperrten Dateien
(`WizardCtrl`, `WErzeugerModel`, `Form_BHKWEing`, `Form_Heizkessel`, `WizardParent`) sind
unberührt, ebenso `Referenzlaeufe/2026-08-14_B0/lauf_protokoll.md`, `DB-Backup/` und die
untracked Ordner `Referenzlaeufe/2026-08-14_Paket1_Migration/` und
`…_Paket3_Review/`.

**Das Feature-Flag ist der einzige Schalter.** Mit `Kaskade_Zweikanalig = aus` rechnet der
Altpfad **byte-identisch**: Alle 208 CSV-Dateien der neun Referenzprojekte sind Zeichen für
Zeichen gleich mit der Basis `Referenzlaeufe/2026-08-14_B1-Fixes` (Teil 7.2). Ein neues
Basis-Einfrieren ist deshalb **nicht** nötig.

---

## 1. Umfang

### Geänderte Dateien

| Datei | Änderung |
|---|---|
| `Allgemein/Simulation/SimulationBHKW.cs` | **Extraktion** der gemeinsamen Physik (`Kennzahlen_Zuruecksetzen`, `Moduldaten_Einlesen`, `Auswertung`, `Motorlauf_Waermegefuehrt`, `Motorlauf_Stromgefuehrt`, `Motorlauf_OhneEinspeisung`, `Speicherabrechnung`) und der **zweikanalige Weg**: `Init`, `Vorbereiten_Zweikanalig`, `Stunde_Start`, `Stunde_Bedarf`, `Zweikanalig_Laden`, `Stunde_Ende`, `Fahrweise_Stunde`, `Abschluss_Zweikanalig`, `Berechnung_Zweikanalig`; neue Größen `Direktdeckung_gesamt`, `Speicherladung_stuendlich/_gesamt`, `Speicherentladung_Anteil`, `Ueberschuss_stuendlich`, `Waermebedarf_gesamt`, `Fehlertext`, `Auftrag_Haupt/_Zweit`. **`Berechnung()` und die drei Fahrweisen-Methoden rechnen im Altpfad anweisungsgleich weiter** (Nachweis 7.1) |
| `Allgemein/Simulation/Kaskadenschleife.cs` | BHKW als viertes Schleifenmitglied: Feld `BHKW`, `MitBHKW`, Phase-B-Zweig, Ladephasen-Zweig, `Stunde_Start`/`Stunde_Ende`, `Abschluss_Zweikanalig`; Herkunftsrechnung um `ART_BHKW` erweitert (`ART_ANZAHL` 3 → 4); neue `BhkwAuftraegeZuordnen()` |
| `Allgemein/Simulation/SimulationControl.cs` | `_bhkwInSchleife` samt `BHKWInSpeicherstufe`; Aufnahmekriterium (Puffer-Senke **oder** Pendelspeicher); BHKW in `IstSchleifenstufe`, `BedarfsreihenfolgeAufbauen`, `ZwischenstufenAufnehmen`, `ModulindexDerAnlage`, `PufferSenkenOhneAuftragZurueckfallen`; Modulaufbau in `Speicherstufe_Rechnen`; neue `BHKW_Liste_Laden()`, `Simulation_BHKW_Ctrl_Zweikanalig()`, `BhkwErsatzspeicherAufnehmen()`, `LadeauftragEinsortieren()`; **Entfall des `Uebernehmen`-Ankers und des Warnzweigs „BHKW zwischen zwei Mitgliedern"** |
| `Allgemein/Simulation/SimulationRunner.cs` | `Tab_ErgebnisBHKW.Restwaermebedarf` und `.Waermebedarfsdeckung` aus dem **Eigenanteil** — nur im zweikanaligen Weg (Teil 4, seit der Nacharbeit N1 auch der Restbedarf) |
| `Controller/PufferSpCtrl.cs` | `PendelspeicherId` von `private` auf `public` (die Engine braucht die Puffer-ID für den Ersatzspeicher) |

**In der Nacharbeit dazugekommen** (Kapitel 13):

| Datei | Änderung |
|---|---|
| `Allgemein/Simulation/SimulationPufferspeicher.cs` | ΔT-Rückfall parametrierbar samt `RueckfallDeltaT` (N2), Reservierung der Ladefähigkeit (N3), Trennung von **Speicherumsatz und Durchfluss** in `Laden`/`Entladen`/`StundeAbschliessen` (N6) |
| `Allgemein/Simulation/SimulationSPK.cs` | Stufeneingang in `Stunde_Start` statt `Stunde_Bedarf` — vor Phase A (N1) |
| `Allgemein/Simulation/SimulationSolarthermie.cs` | dito (N1) |
| `Allgemein/Simulation/SimulationKanaele.cs` | Begründung, warum `Waermekanaele.Uebernehmen` trotz fehlendem Aufrufer bleibt (N10) |

### Neue Dateien

| Datei | Inhalt |
|---|---|
| `Allgemein/Simulation/Paket6_BHKW_Protokoll.md` | dieses Protokoll |

Der Rechenkern hat damit **keine Erzeugerart mehr am Kompatibilitätsanker**
`Waermekanaele.Uebernehmen` — im zweikanaligen Weg rechnen Wärmepumpe, Solarthermie,
Heizkessel und BHKW alle auf den beiden Kanälen.

---

## 2. Die Ausgangslage — drei Befunde, die den Entwurf bestimmt haben

### 2.1 Die hartkodierten Schwellen 30/10/20 % sind **unerreichbar**

Konzept 2.2 (Punkt 8) und 6.5 nennen sie als hartkodierte Regelschwellen der
wärmegeführten Fahrweise. Sie stehen im Code — aber in totem Code:

```csharp
// Winterbetrieb
//if (stunde < 3600 || stunde > 5760)
if (stunde < 8760)                       // stunde ∈ [0…8759]  ->  IMMER wahr
{ … }
else if (stdTag > 10 && stdTag < 22)     // Sommerbetrieb: enthält 30 % und 10 %
{ … }
else if (speicher - restWaerme < kapazitaetPendelspeicher * 0.2f && …)   // 20 %
{ … }
```

Die auskommentierte Zeile darüber zeigt die ursprüngliche Absicht (Sommer =
Stunden 3600…5760). Seit sie durch `stunde < 8760` ersetzt wurde, läuft **ausschließlich
der Winterzweig**; Sommerbetrieb und beide Notschaltungen sind seit jeher unerreichbar.
Damit gilt: Die drei Prozentwerte haben **nie** ein Ergebnis beeinflusst, und ihre
Ablösung ist ergebnisneutral.

### 2.2 Kein Referenzprojekt hat heute einen Pendelspeicher

An der migrierten Datenbank nachgeprüft: `Puffer 'BHKW-Pendelspeicher': 0` — in keinem
der neun Projekte existiert die Zeile, `PufferSpCtrl.PendelspeicherVolumenLiter` liefert
überall 0 und damit `kapazitaetPendelspeicher = 0`. Der gesamte Speicherzweig der drei
Fahrweisen ist in der Referenzmenge **inaktiv**; die wärmegeführte Fahrweise arbeitet dort
als reine Bedarfsdeckung.

Folge für die Verifikation: Die Speicher- **und** die Bilanzfehler-Wirkung sind an der
Referenzmenge nicht messbar. Beides wird an präparierten Datenbanken gezeigt (Teil 7.5
und 7.6).

### 2.3 Die drei Fahrweisen schalten ihre Motoren **gemeinsam** zu

Alle drei Implementierungen entscheiden je Stunde über `restWaerme + restSpeicher`
(bzw. `restStrom`) und iterieren dabei über **alle** Module gegen **einen** gemeinsamen
Speicher. Eine Senke je Modul würde diese Zuschaltlogik auseinanderreißen — das wäre neue
Physik. Der Entwurf hält deshalb an einer Senke je BHKW-**Stufe** fest (Abgrenzung 3 in
Kapitel 8).

---

## 3. Entwurfsentscheidungen

### 3.1 Die Auflösungskette des BHKW-Speichers

```
1. Puffer-Senke der BHKW-Anlage   (WS_Ziel/WS_ID_Puffer bzw. WS_Ziel2/WS_ID_Puffer2)
      -> Registry-Speicher, Ladeauftrag über Ladeordnung (Vorgaberang 30)
      -> derselbe Weg wie Wärmepumpe, Solarthermie und Heizkessel
2. KEINE Puffer-Senke, aber ein Pendelspeichervolumen (Puffer-Zeile 'BHKW-Pendelspeicher')
      -> ERSATZSPEICHER: dieselbe Zeile wird als SimulationPufferspeicher aufgenommen
         und bekommt einen Ladeauftrag als ZWEITSENKE
3. Weder noch
      -> kein Speicher; das BHKW deckt nur den Momentanbedarf
```

**Stufe 1 ist der Regelfall.** Migrationsregel R6 (Konzept 5.5) legt zum Pendelspeicher
*immer* auch die Senke an — und zwar als **HAUPTsenke**: `ProjektPuffer.SQL_BHKW_AUF_PUFFER`
schreibt `WS_Ziel = 'PufferHeizung'` auf **alle** BHKW-Anlagen des Projekts, und
`PufferSpCtrl.SetPendelspeicherVolumenLiter` ruft genau diese Anweisung auf. Stufe 2
entsteht nur, wenn die Senke nachträglich zurückgenommen wurde, während die Puffer-Zeile
stehen blieb.

**Warum Stufe 2 überhaupt existiert.** Ohne sie verlöre ein solches Projekt beim Setzen
des Flags stillschweigend seinen Speicher — genau die Sorte stiller Verhaltensänderung,
gegen die die Paket-5-Nacharbeit (Befund N5) angetreten ist. Der Ersatzspeicher hält das
Verhalten fachlich gleich („das BHKW arbeitet mit seinem Pendelspeicher") und stellt es
zugleich auf die **eine** Speicherphysik um: Hysterese, Bereitschaftsverluste, Kapazität
aus der ΔT-Spreizung, Entladung in den Phasen A/E, `StundeAbschliessen` in Phase G,
Herkunftsrechnung und Ergebnispersistenz.

**Warum der ERSATZ-Pendelspeicher als ZWEITsenke aufgenommen wird.**

> **Berichtigt in der Nacharbeit (Befund N12c).** Die frühere Begründung — „als
> Hauptsenke würde das BHKW nichts mehr direkt decken, eine Verhaltensänderung ohne
> Anlass" — **trug nicht**: Der von R6 geschriebene Regelfall *ist* die Hauptsenke, und
> genau so rechnet Stufe 1 seit Paket 6. Die Zweitsenken-Rolle betrifft ausschließlich
> **Stufe 2**, also den Ausnahmefall ohne Senkenreferenz.

Für Stufe 2 ist die Zweitsenke die richtige Rolle, weil dort die Senkenzeile fehlt: Ohne
`WS_Ziel` gilt Hauptsenke Heizkreis (Konzept 4.6), das BHKW deckt also zuerst den
Momentanbedarf, und der Pendelspeicher nimmt den Überhang auf — die Definition der
Zweitsenke aus Konzept E2.

**Beide Wege sind energetisch identisch**, gemessen an 1018 mit 1000-l-Pendelspeicher
(60/40 °C, Teil 13.6):

| Größe | Stufe 1 (HAUPTsenke, R6-Regelfall) | Stufe 2 (Ersatzspeicher als Zweitsenke) |
|---|---|---|
| BHKW-Produktion | 155,888 MWh | 155,888 MWh |
| Kessel-Nutzwärme | 29,229 MWh | 29,229 MWh |
| Restwärme des Projekts | 0,05178 MWh | 0,05178 MWh |
| Deckungssumme / tatsächlich | 99,972036 % / 99,972036 % | 99,972036 % / 99,972036 % |
| Direktdeckung / Speicherladung | 0 / 155,885 MWh | 99,165 / 56,719 MWh |
| Speicherumsatz (ohne Durchfluss) | 82.796 kWh | 56.719 kWh |
| Durchfluss | 73.089 kWh | 0 kWh |
| Vollzyklen | 3.568,80 | 2.444,81 |

Unterschiedlich sind also **nur der Speicherumsatz und die Ausweisgrößen** — energetisch
liefern beide Wege dieselbe Rechnung. Als Hauptsenke ist der Speicher eine hydraulische
Weiche (die gesamte Produktion läuft hindurch), als Zweitsenke ein Vorrat.

Die Umsetzung steht in `SimulationControl.BhkwErsatzspeicherAufnehmen`. Der Fall wird
protokolliert:

```
BHKW-Pendelspeicher: Keine Puffer-Senke am BHKW - der Speicher „BHKW-Pendelspeicher"
(1000 l, 60/40 °C, Q_max 23,2 kWh) rechnet als ZWEITSENKE mit. Der skalare
Pendelspeicher des Altpfads ist damit abgelöst.
```

### 3.2 Wie die Fahrweisen in die Phasenstruktur A–G passen

Der Kern des Umbaus ist eine einzige Beobachtung: Der Ausdruck
`restWaerme + restSpeicher`, mit dem alle drei Fahrweisen ihre Motoren zuschalten, **ist**
der Bilanzraum aus der Paket-4-Nutzerentscheidung zu Befund 4b-1 —

```
Bilanzraum = (Q_max · Obergrenze − SOC)                    [SOC-Zielwert, Konzept 3.4]
           + min(offener Kanalbedarf, Entnahmefähigkeit)    [Durchsatz]
```

— nur ohne die Trennung in Zielfüllstand und Durchsatz. Der Altcode hat die hydraulische
Weiche also schon immer richtig gesehen; Paket 6 macht die beiden Summanden sichtbar und
ersetzt den skalaren Speicher durch das Speicherobjekt.

Daraus folgt die Zuordnung:

| Hauptsenke der Stufe | Phase B | Phase C/D |
|---|---|---|
| **Heizkreis** (mit oder ohne Zweitsenke) | Die Fahrweise läuft: Wärmeraum = offener Kanalbedarf nach `WS_Typ` **+** Ladefähigkeit der Zweitsenke. Was den Bedarf deckt, geht über `SenkeAbziehen`; der Rest bleibt bis Phase D stehen | Phase D lagert den Rest in die Zweitsenke ein |
| **Puffer** | deckt **nichts** (Doppelzählungs-Freibeweis) | Phase C: Die Fahrweise läuft gegen den Bilanzraum des Hauptsenken-Speichers; alles Erzeugte wird eingelagert, ein Rest geht in Phase D an die Zweitsenke |

Technisch führt `SimulationBHKW.Fahrweise_Stunde` den Speicher als **skalaren Spiegel**
des `SimulationPufferspeicher`: `speicher` startet bei 0, `kapazitaet` ist der Wärmeraum.
Der Zuwachs von `speicher` ist damit genau die einzulagernde Menge, der Rückgang von
`restWaerme` die Direktdeckung. Gerechnet wird mit **denselben** Methoden wie im Altpfad
(`Motorlauf_Waermegefuehrt`, `…Stromgefuehrt`, `…OhneEinspeisung`) — es gibt keine zweite
Physik (Paket-5-Lehre N6).

Was am Stundenende weder gedeckt noch gespeichert ist, wird als **Wärmeüberschuss**
gebucht. Damit hat `Waermeueberschuss` erstmals in allen drei Fahrweisen dieselbe
Bedeutung; im Altpfad kannte sie nur die stromgeführte Fahrweise (Überlauf des
Pendelspeichers), die wärmegeführte trug dort den toten Solar-Überschuss und die Fahrweise
ohne Einspeisung setzte sie gar nicht.

### 3.3 Schwellen-Mapping 30/10/20 % → Speicherparameter

Die drei Prozentwerte sind **Mindestfüllstände**: „es müssen immer 10 % (bzw. 20 %) im
Speicher sein", und im Sommerzweig „läuft an, wenn der Füllstand unter 30 % fällt". In der
neuen Speicherphysik ist genau das die **Einschaltschwelle der Hysterese**:

| Altwert | Fundstelle (HEAD) | Semantik | Neuer Parameter |
|---|---|---|---|
| **30 %** | `SimulationBHKW.cs:442` (Sommerzweig) | Nachladen beginnt unterhalb dieses Füllstands | `Tab_Pufferspeicher.Schwelle_Ein` |
| **10 %** | `SimulationBHKW.cs:457` (Notschaltung Sommer) | Mindestfüllstand | `Tab_Pufferspeicher.Schwelle_Ein` — **Default ist 10 %**, also wertgleich |
| **20 %** | `SimulationBHKW.cs:474` (Notschaltung Nacht) | Mindestfüllstand im Nachtfenster | `Tab_Pufferspeicher.Schwelle_Ein` |
| — | — | Abschalten bei „voll" | `Tab_Pufferspeicher.Schwelle_Aus` (Default 95 %) |
| — | — | Reservezone für vorrangige Erzeuger | `Tab_Pufferspeicher.Schwelle_Aus_Nachrang` (Default = `Schwelle_Aus`) |

Die drei Werte werden also auf **einen** Parameter abgebildet. Sie galten in einander
ausschließenden Betriebsfenstern (Sommertag, Sommernacht, Winternacht), die es in der
Phasenstruktur A–G nicht mehr gibt — dort entscheidet die Hysterese am Speicher, nicht die
Uhrzeit.

> **Berichtigt in der Nacharbeit (Befund N12a).** Die frühere Formulierung „das ist kein
> Informationsverlust … die Abbildung ist wertgleich" war **falsch** und ist gestrichen.
> Richtig ist:
>
> * Der **Altzweig ist toter Code** (2.1). `Schwelle_Ein` wirkt im neuen Weg auf die
>   Hysterese der **Phase A** — also darauf, wann ein Speicher wieder entlädt —, während
>   die drei Prozentwerte im Altcode Zuschaltbedingungen der Motoren in bestimmten
>   Tageszeitfenstern waren.
> * **Ergebnisneutral ist die Ablösung trotzdem**, aber aus einem anderen Grund: Die drei
>   Zweige sind seit der Änderung `if (stunde < 3600 || stunde > 5760)` → `if (stunde < 8760)`
>   unerreichbar, ihre Schwellen haben also nie ein Ergebnis beeinflusst.
> * **Wertgleich ist die Semantik NICHT.** Wer die alten Fenster zurückhaben will, braucht
>   eigene Parameter — siehe Nutzerentscheidung 6-2, Variante B.

Die Spalten existieren seit Paket 1/B0-1 an `Tab_Pufferspeicher`
(`Schwelle_Ein`, `Schwelle_Aus`, `Schwelle_Aus_Nachrang`, `Entladeprio` — an der
Datenbank nachgeprüft) und werden von `WaermesenkeClass.PufferLesen` gelesen.

### 3.4 ΔT-Spreizung: die Kapazität hängt jetzt an den Temperaturen

**Alte Formel** (`SimulationControl.Simulation_BHKW_Ctrl`, unverändert im Altpfad):

```
kapazitaetPendelspeicher = Volumen[l] · 20 / 860   [kWh]
```

Das ist `Volumen · 1,16279 Wh/(l·K) · 20 K / 1000` — eine **implizit fest verdrahtete
Spreizung von 20 K** und eine spezifische Wärme von 1,16279 statt der im Rechenkern sonst
verwendeten 1,16 Wh/(l·K).

**Neue Formel** (`SimulationPufferspeicher.Init`, dieselbe wie für alle anderen Speicher):

```
Q_max = Volumen[l] · 1,16 · (Vorlauf − Rücklauf) / 1000   [kWh]
        Fallback ΔT = 10 K, wenn kein Temperaturpaar gepflegt ist
```

Gegenüberstellung für einen 1000-l-Pendelspeicher:

| Fall | ΔT | Q_max neu | Q_max alt | Abweichung |
|---|---|---|---|---|
| Temperaturpaar 60/40 °C (Systemvorgabe) | 20 K | **23,20 kWh** | 23,2558 kWh | **−0,24 %** (nur die Konstante 1,16 gegen 1,16279) |
| Temperaturpaar 70/55 °C | 15 K | 17,40 kWh | 23,2558 kWh | −25,2 % |
| Temperaturpaar 55/45 °C | 10 K | 11,60 kWh | 23,2558 kWh | −50,1 % |
| **kein Paar gepflegt** (Engine-Rückfall) | 10 K | 11,60 kWh | 23,2558 kWh | −50,1 % |

Dazu kommt die **Abschaltschwelle**: Der neue Speicher lädt nur bis
`Q_max · Schwelle_Aus` (Default 95 %), der alte bis 100 %. Bei 20 K sind das
22,04 kWh gegen 23,2558 kWh, also **−5,2 %** nutzbarer Zielfüllstand. Gemessen:
`SOC_Max 22,040001 kWh` (Teil 7.5).

**Ergebniswirkung, gemessen an Projekt 1018 mit 1000-l-Pendelspeicher** (Teil 7.5):

| ΔT des Pendelspeichers | Q_max | Speicherumsatz | BHKW-Produktion | Kessel-Nutzwärme |
|---|---|---|---|---|
| 20 K (60/40) | 23,20 kWh | 56,719 MWh | 155,888 MWh | 29,229 MWh |
| 10 K (kein Paar) | 11,60 kWh | 37,474 MWh | 153,543 MWh | 31,574 MWh |

Die halbierte Kapazität kostet 2,34 MWh BHKW-Wärme und verlagert sie auf den Kessel. Das
ist die fachlich richtige Richtung — ein kleinerer Puffer taktet das BHKW häufiger ab —
und es ist die Größe, die Paket 1 ausdrücklich für Paket 6 vorgemerkt hatte
(`Paket1_SchemaMigration_Protokoll.md`, Zeile „feste 20 K, VL/RL gehen nicht ein —
Ändern würde die Referenzergebnisse verschieben. Gehört zu Paket 6").

**Empfehlung für den Betrieb:** Am Pendelspeicher ein Temperaturpaar pflegen. Ohne Paar
greift der 10-K-Rückfall der Engine und halbiert die Kapazität gegenüber der alten
Annahme. `PufferSpCtrl.SetPendelspeicherVolumenLiter` belegt Vor-/Rücklauf beim Anlegen
aus den Systemvorgaben des Projekts (Paket 1, Etappe 4) — sind dort keine
Erzeugertemperaturen gepflegt, bleiben beide Spalten NULL.

### 3.5 Der Bilanzfehler — Fix nur im neuen Pfad

`SimulationControl.Simulation_BHKW_Ctrl` bildet den Rest als Vektordifferenz

```csharp
float[] restwaerme = SubVectors(Waermebedarf, simulation_bhkw.waermeproduktion);
```

und verwirft damit den vom BHKW selbst geführten `waermerestbedarf`. Der Unterschied ist
genau die Speicherbewegung der Stunde:

```
waermerestbedarf_h = Bedarf_h − Produktion_h + Ladung_h − Entladung_h
```

Über das Jahr summiert sich `Σ(Ladung − Entladung)` auf den Endfüllstand, ist also klein.
**Stundenweise** aber weicht der Wert um bis zur vollen Speicherkapazität ab — und weil
`SubVectors` negative Stundenwerte auf 0 klemmt, ist die Jahressumme des Kessel-Eingangs
zusätzlich **überhöht**. Der Folgeerzeuger bekommt damit ein falsches Lastprofil.

**Im neuen Pfad ist der Fehler strukturell verschwunden:** Das BHKW schreibt seinen Rest
unmittelbar in die Kanäle; es gibt keine Vektordifferenz mehr.

**Im Altpfad besteht er weiter** — bewusst, nach Entscheidung des Orchestrators: Die
Flag-Disziplin ist die einzige Rückfallebene, und ein Altpfad-Fix wäre ein eigenes
B0-artiges Paket mit eigenem Basis-Einfrieren. Wirkung im Altpfad, gemessen (Teil 7.6):

| Projekt (präpariert, 1000-l-Pendelspeicher) | Kessel-Eingang Altpfad | tatsächlicher Rest | Kessel-Nutzwärme Altpfad → neu |
|---|---|---|---|
| 1017, wärmegeführt | — | — | **11,807 → 2,740 MWh** (−9,07 MWh Gas) |
| 1018, wärmegeführt | 34,322 MWh | 29,281 MWh | **34,270 → 29,229 MWh** (−5,04 MWh Gas) |
| 1017, ohne Einspeisung | — | — | **9,600 → 0 MWh** |
| 1017, stromgeführt | — | — | 4,419 → 2,740 MWh |

Der Altpfad lässt also **Kesselwärme entstehen, die niemand angefordert hat** — bis zu
9,07 MWh in einem 63-MWh-Projekt (14 %). Ohne Pendelspeicher (der heutige Zustand aller
neun Referenzprojekte) ist der Fehler exakt 0 (gemessen: `max |rest − (bedarf−prod)| =
0 kWh` in 1017, `3,8·10⁻⁶ kWh` in 1018).

> **Vorlage für eine Nutzerentscheidung:** Soll der Fehler auch im Altpfad behoben werden,
> ist das ein B0-artiges Vorab-Paket mit eigenem Basis-Einfrieren — die Ergebnisse aller
> BHKW-Projekte mit Pendelspeicher ändern sich dadurch. Solange kein Projekt einen
> Pendelspeicher trägt (heute: keines), ist der Fehler latent.

### 3.6 Der Deckungsgrad des BHKW

Bisher meldete das BHKW seine **Produktion** als Bedarfsdeckung
(`SimulationRunner`, `Waermeproduktion_BHKW_MWh · 100 / Waermebedarf_Gesamt`) — offener
Punkt 4 der Paket-5-Nacharbeit (13.12). Das ist eine Doppelzählung, sobald Wärme in einen
Speicher geht oder verworfen wird.

Seit Paket 6 gilt im zweikanaligen Weg dieselbe Regel wie für Wärmepumpe, Solarthermie und
Heizkessel:

```
Eigenanteil = Direktdeckung (Phase B) + zugerechnete Speicherentladung (Phasen A/E)
Restwärmebedarf = Stufeneingang − Direktdeckung        (geklemmt auf ≥ 0)
```

Die Zurechnung der Entladung folgt der Interimsregel „Vermischung im Speicher" aus der
Paket-5-Nacharbeit (N2); dafür ist die Herkunftsrechnung der `Kaskadenschleife` um die
vierte Erzeugerart erweitert worden.

**Gemessene Wirkung** (Summe aller ausgewiesenen Erzeugerdeckungen gegen die tatsächliche
Projektdeckung):

| Szenario | Summe vorher | Summe nachher | tatsächlich |
|---|---|---|---|
| 1017 wärmegeführt, 1000-l-Pendelspeicher | **114,508 %** | **100,000001 %** | 100 % |
| 1017 stromgeführt, 1000-l-Pendelspeicher | **271,601 %** | **100,000001 %** | 100 % |
| 1017 ohne Einspeisung, 1000-l-Pendelspeicher | **115,297 %** | **100,000000 %** | 100 % |
| 1018 wärmegeführt, 1000-l-Pendelspeicher | **102,826 %** | **99,972036 %** | 99,972036 % |
| 1024 (Referenzprojekt, BHKW-Zweitsenke) | 88,315261 %¹ | **88,162164 %** | 88,162172 % |

„Summe vorher" ist jeweils **derselbe Lauf**, nur mit der alten produktionsbasierten
BHKW-Formel gerechnet — die Zeilen sind also direkt vergleichbar. Die 271 % im
stromgeführten Fall entstehen, weil dort 106,2 MWh Koppelwärme verworfen werden und
trotzdem als Deckung galten.

¹ 36,645548 (WP) + 10,492563 (Kessel) + **41,177150** (BHKW, Produktion) = 88,315261 %
gegen 36,645548 + 10,492563 + **41,024054** (BHKW, Eigenanteil) = 88,162164 %.

### 3.7 Entfall des Zwischenstufen-Sonderfalls

Paket 5 protokollierte: *„Das BHKW steht in der Kaskade zwischen zwei Erzeugern der
Speicherstufe. Es rechnet bis Paket 6 einkanalig als Vektormodul und deshalb NACH der
gesamten Speicherstufe."* Da das BHKW jetzt stundenweise rechnen kann, ist der Zweig
**unerreichbar geworden und entfernt**. An seine Stelle tritt derselbe strukturelle Fix,
den Befund N4 für Solarthermie und Heizkessel gebracht hat: `ZwischenstufenAufnehmen`
nimmt auch ein BHKW als Mitglied auf, sobald es zwischen dem ersten und dem letzten
Mitglied steht, und protokolliert das.

Damit gibt es im zweikanaligen Weg **keinen stillen Positionswechsel mehr, für keine der
vier Erzeugerarten**.

---

## 4. Die Mitkorrektur in `SimulationRunner`

| Größe | Altpfad (unverändert) | zweikanaliger Weg |
|---|---|---|
| `Waermeproduktion` | `Waermeproduktion_BHKW_MWh` | unverändert — sie ist die Bezugsgröße von Brennstoffverbrauch und Emissionen |
| `Waermebedarf` | Kanalstand an der Kaskadenposition | **Stufeneingang VOR Phase A** (Nacharbeit N1; vorher: nach Phase A) |
| `Restwaermebedarf` | `Σ SubVectors(Bedarf, Produktion)` | **`Stufeneingang − Eigenanteil`**, geklemmt auf ≥ 0 (Nacharbeit N1; vorher: `− Direktdeckung`) |
| `Waermebedarfsdeckung` | `Produktion / Projektbedarf` | `(Direktdeckung + zugerechnete Entladung) / Projektbedarf`, geklemmt auf ≤ 100 % |
| `Waermeueberschuss` | wie bisher, zusätzlich auf den Laufanfang genullt (N9) | jetzt in allen drei Fahrweisen gefüllt |

> **Nacharbeit N1/N12d.** Die beiden mit **fett** markierten Zeilen sind das Ergebnis der
> Nacharbeit. Die ursprüngliche Fassung hatte `Waermebedarf` gar nicht als geänderte Größe
> ausgewiesen, obwohl sie es war (gemessen an 1017 mit Pendelspeicher: 62,91 → 27,80 MWh,
> −56 %), und band `Restwaermebedarf` an die Direktdeckung — die bei Puffer-HAUPTsenke
> konstruktiv 0 ist. Beides ist in Teil 13.2 vollständig beschrieben.

Der Altpfad-Zweig ist durch `if (sim.KaskadeZweikanalig)` geschützt und wird dort nicht
betreten; `Direktdeckung_gesamt` und `Speicherentladung_Anteil` sind im Altpfad exakt 0.

**Nicht geändert wurde die Anzeige** in `Form_Simulation_Detail` (`:1749` bildet den
BHKW-Restbedarf weiterhin als Vektordifferenz, `:1756` die Deckung aus der Produktion).
Das ist dieselbe Abgrenzung, die Paket 5 für den Fehlerkanal gezogen hat (13.12, Punkt 8):
eine ANZEIGE-Aufgabe, die zu Paket 7 gehört. Siehe offener Punkt 3 in Kapitel 9.

---

## 5. Wer in der Schleife rechnet

| Stufe | in der Stundenschleife | sonst |
|---|---|---|
| **Wärmepumpe** | immer, wenn sie in der Kaskade steht | — |
| **Solarthermie** | mit Puffer-Senke — oder zwischen zwei Mitgliedern | zweikanalige Vektorstufe |
| **Heizkessel** | dito | zweikanalige Vektorstufe |
| **BHKW** *(neu)* | mit Puffer-Senke **oder Pendelspeicher** — oder zwischen zwei Mitgliedern | zweikanalige Vektorstufe |

Das BHKW-Kriterium ist bewusst weiter als bei Solarthermie und Kessel: Der Pendelspeicher
ist ein Speicher, auch wenn keine Senkenreferenz auf ihn zeigt.

Kaskadenreihenfolgen und Mitgliedschaften der Referenzmenge (an der Datenbank geprüft):

| Projekt | Tool_1..4 | Speicherstufe mit Flag AN | Änderung gegenüber Paket 5 |
|---|---|---|---|
| 1007, 1011 | Solarthermie → Wärmepumpe | WP | — |
| 1008, 1010, 1021 | Wärmepumpe | WP | — |
| 1017, 1018 | BHKW → Heizkessel | **keine** (BHKW ohne Speicher → Vektorstufe) | — |
| 1023 | Wärmepumpe → Heizkessel | WP | — |
| 1024 | Wärmepumpe → Heizkessel → BHKW | **WP + Heizkessel + BHKW** | BHKW wird Mitglied (Zweitsenke), der Kessel dazwischen ebenfalls (N4-Regel) |

---

## 6. Fahrweisen — was sich je Fahrweise ändert

| Fahrweise | Was die Fahrweise bestimmt | Speicherinteraktion neu |
|---|---|---|
| **wärmegeführt** (`modeBHKW = 0`) | Zuschaltung nach `restWaerme + restSpeicher`; Modulation bis `Pth · GrenzL` | `restSpeicher` = Ladefähigkeit bzw. Bilanzraum des zugeordneten Speichers |
| **stromgeführt** (`modeBHKW = 1`) | Zuschaltung nach dem STROMBEDARF; Wärme ist Koppelprodukt | Der Speicher geht **nicht** in die Zuschaltung ein (richtig: eine stromgeführte Maschine richtet sich nicht nach dem Füllstand). Überschusswärme geht in die Ladephase C/D; was dort nicht hineinpasst, ist Wärmeüberschuss — dieselbe Regel wie im Altpfad, nur über das Speicherobjekt |
| **ohne Einspeisung** (`modeBHKW = 2`) | zwei Zuschaltschleifen: erst wärme-, dann stromseitig, beide gegen `restWaerme + restSpeicher` **und** den Strombedarf | wie wärmegeführt |

**Die Referenzmenge deckt nur die wärmegeführte Fahrweise ab** (1017, 1018 und 1024 haben
alle `Betriebsart = 0` bzw. NULL). Die beiden übrigen sind an präparierten Datenbanken
geprüft (Teil 7.6).

---

## 7. Verifikation

### 7.1 Byte-Neutralität der Extraktion (Paket-5-Lehre N6)

Sechs Blöcke sind aus `Berechnung()` und den drei Fahrweisen **herausgelöst**, nicht
kopiert. Nachweis durch Lesen: Vergleich der Anweisungszeilen (ohne Leerzeichen,
Kommentare und Blockklammern) gegen `git show HEAD`:

| Extrahierte Methode | Anweisungszeilen alt / neu | Ergebnis |
|---|---|---|
| `Kennzahlen_Zuruecksetzen()` | 16 / 16 | **identisch** |
| `Moduldaten_Einlesen(anzahl)` | 19 / 19 | **identisch** (nur `anzahlBhkw` → Parameter `anzahl`) |
| `Auswertung(anzahl)` | 30 / 30 | **identisch** (dito) |
| `Motorlauf_Waermegefuehrt(...)` | 85 / 85 | **identisch** |
| `Motorlauf_Stromgefuehrt(...)` | 16 / 16 | **identisch** |
| `Motorlauf_OhneEinspeisung(...)` | 75 / 75 | **identisch** |
| `Speicherabrechnung(...)` | 10 / 10 | **identisch** (stand zweimal im Bestand — wärmegeführt und ohne Einspeisung — und steht jetzt einmal) |

Bewegt wurden ausschließlich drei Deklarationen (`restSpeicher`, `wLeistung`, `sLeistung`)
in die extrahierte Methode hinein; ihre erste Zuweisung ist dabei zur Initialisierung
geworden. Gemessener Nachweis: der byte-identische Regressionslauf (7.2), der **vor** dem
Anbau des zweikanaligen Wegs zusätzlich einzeln geführt wurde.

### 7.2 Flag AUS — Regression (Pflicht), byte-identisch

Neun Referenzprojekte auf einer eigenen, vollständig migrierten Kopie
(`C:\Waermeplan\Paket6_Test\DB_Basis`), verglichen gegen `Referenzlaeufe/2026-08-14_B1-Fixes`:

```
Projekt_1007: PASS (29 Dateien, 324210 Werte)   Projekt_1018: PASS (19 Dateien, 210343 Werte)
Projekt_1008: PASS (21 Dateien, 227847 Werte)   Projekt_1021: PASS (21 Dateien, 227840 Werte)
Projekt_1010: PASS (18 Dateien, 201540 Werte)   Projekt_1023: PASS (25 Dateien, 262917 Werte)
Projekt_1011: PASS (29 Dateien, 324232 Werte)   Projekt_1024: PASS (26 Dateien, 271686 Werte)
Projekt_1017: PASS (20 Dateien, 245378 Werte)

GESAMT: PASS (2295993 Werte innerhalb der Toleranz)
```

**Stärkerer Nachweis als die Toleranzprüfung:** MD5 über alle **208** CSV-Dateien —
**keine einzige abweichende Datei**. `Referenzlauf.exe pruefen` meldet für alle neun
Projekte „plausibel".

### 7.3 Flag AN — gegen den Paket-5-Stand

Derselbe Datenbestand (`DB_Flag`, alle neun mit gesetztem Flag), einmal mit dem
Paket-5-Stand (aus `git checkout` des HEAD gebaut) und einmal mit dem Paket-6-Stand:

| Projekt | Ergebnis | Bewertung |
|---|---|---|
| 1007, 1008, 1010, 1011, 1021, 1023 | **PASS** | **Nicht-BHKW-Projekte unverändert** (Prüfpunkt 3 der Aufgabe) |
| 1017, 1018 | **PASS** | BHKW ohne Speicher: die zweikanalige Vektorstufe liefert dieselben Zahlen wie der `Uebernehmen`-Anker — beide Projekte haben keinen Brauchwasserbedarf, der WW-Kanal ist durchgehend 0 |
| 1024 | **FAIL — gewollt** | Das BHKW nutzt erstmals seine migrierte Zweitsenke; Einzelaufstellung in Kapitel 8 |

### 7.4 Flag AN — Referenzprojekte: Bilanzen, Abschlüsse, Deckungssummen

Gemessen mit einer eigenen headless-Probe (`Probe6`, rechnet über
`SimulationRunner.Simuliere` und **speichert nicht**):

| Projekt | Stundenbilanz Projekt, max | Summe der Beträge | Speicher | `StundeAbschliessen` | Deckungssumme / tatsächlich |
|---|---|---|---|---|---|
| 1017 | **0 kWh** | 0 kWh | 0 | — | 100,000001 % / 100 % |
| 1018 | 3,815·10⁻⁶ kWh | 0,00328 kWh | 0 | — | 99,972036 % / 99,972036 % |
| 1024 | 1,526·10⁻⁵ kWh | 0,00793 kWh | 1 | **8760/8760** | 88,162164 % / 88,162172 % |

Die Stundenbilanz prüft
`Bedarf − Rest == Produktion(alle Erzeuger) + Heizstab + Entladung − Ladung − verworfene BHKW-Wärme`
auf dem PROJEKTbedarf (nicht auf einem Stufeneingang: die Phase-A-Entladung liegt vor der
Bedarfsphase und würde dort doppelt zählen).

Speicherbilanz `Ladung − Entladung − Verluste == ΔSOC`:

| Lauf | Speicher | Q_max | Ladung | Entladung | Verluste | SOC Ende | SOC_Max | Vollzyklen | Bilanzfehler |
|---|---|---|---|---|---|---|---|---|---|
| 1024 | `1054164` (Brauchwasser) | 10,4400 | 32.528,037 | 31.936,644 | 581,558 | 9,8349 | 9,8349 | 3.115,71 | **−6,5·10⁻¹⁰** |

Der Speicher in 1024 ruhte in Paket 5 („gehört zur Zweitsenke eines BHKW") — er arbeitet
jetzt.

### 7.5 Präparierte Szenarien — Ersatz-Pendelspeicher und ΔT

Alle Szenarien auf eigenen Kopien von `DB_Basis`; die produktive `Kenndaten.accdb` wurde
ausschließlich gelesen.

**P1 — 1018 mit 1000-l-Pendelspeicher, 60/40 °C (ΔT 20 K), wärmegeführt**

| Größe | Flag AUS (Altpfad) | Flag AN (Ersatzspeicher) |
|---|---|---|
| Speicherkapazität | 23,2558 kWh (`Liter · 20/860`) | **23,20 kWh** (`Liter · 1,16 · 20/1000`) |
| Speicherumsatz | nicht ausgewiesen | Ladung **56.719,495** = Entladung **56.719,495** kWh, Verluste 0, SOC_Ende 0, SOC_Max **22,040001** (= 95 % von Q_max), Vollzyklen 2.444,81, **Bilanzfehler 7,3·10⁻¹²**, **Abschlüsse 8760/8760** |
| BHKW-Produktion | 156,129 MWh | 155,888 MWh (−0,15 %) |
| davon Direktdeckung / Speicherladung | — | 99,165 / 56,719 MWh |
| Kessel-Nutzwärme (= Gas) | **34,270 MWh** | **29,229 MWh** |
| Kessel-Stufeneingang | 34,322 MWh | 29,281 MWh |
| Restwärme des Projekts | 0,05178 MWh | 0,05178 MWh |
| Summe der Deckungen | **102,826283 %** | **99,972036 %** (tatsächlich 99,972036 %) |
| Bilanzfehler-Probe `max \|rest − (Bedarf−Produktion)\|` | **23,26 kWh** (= die volle Speicherkapazität), Summe der Beträge **10.560 kWh** | — (es gibt keine Vektordifferenz mehr) |
| Stundenbilanz Projekt | — | max **5,7·10⁻⁶ kWh**, Summe 0,00472 kWh |

**P2 — dasselbe ohne Temperaturpaar am Pendelspeicher (Engine-Rückfall ΔT 10 K)**

| Größe | ΔT 20 K | ΔT 10 K |
|---|---|---|
| Q_max | 23,20 kWh | **11,60 kWh** |
| Speicherumsatz | 56,719 MWh | 37,474 MWh |
| SOC_Max | 22,040001 | 11,020000 |
| BHKW-Produktion | 155,888 MWh | 153,543 MWh |
| Kessel-Nutzwärme | 29,229 MWh | 31,574 MWh |
| Bilanzfehler Speicher | 7,3·10⁻¹² | 7,3·10⁻¹² |
| Abschlüsse | 8760/8760 | 8760/8760 |
| Stundenbilanz Projekt, max | 5,7·10⁻⁶ kWh | 5,2·10⁻⁶ kWh |
| Deckungssumme / tatsächlich | 99,972036 % / 99,972036 % | 99,972036 % / 99,972036 % |

**P3 — 1018 mit Puffer-HAUPTSENKE am BHKW** (`WS_Ziel = PufferHeizung` an beiden
BHKW-Anlagen auf Puffer 1018007, 70/55 °C, Q_max 10,44 kWh): Das BHKW deckt in Phase B
**nichts** und lädt ausschließlich; die Deckung läuft vollständig über die Entladung.

```
BHKW   Produktion 153,863 MWh   Direktdeckung 0   Speicherladung 153,859 MWh
       zugerechnete Entladung 153,263 MWh  ->  Eigenanteil 153,263 MWh
Kessel Direktdeckung 31,852 MWh
PUFFER_1018007  Heizung  Q_max 10,44
   Ladung 153.859,307   Entladung 153.262,509   Verluste 596,798   SOC_Ende 0
   SOC_Max 9,834875   Vollzyklen 14.737,48   Bilanzfehler 2,3·10⁻⁹   Abschluesse 8760/8760
Summe der Deckungen 99,972036 %  gegen tatsaechlich 99,972036 %
Stundenbilanz Projekt: max 4,9·10⁻⁶ kWh, Summe 0,00183 kWh
Restwaerme des Projekts 0,05178 MWh (unveraendert)
```

Der sehr hohe Vollzyklenwert ist die Folge eines 10-kWh-Puffers an 21 kW thermischer
Leistung — er zeigt, dass der Bilanzraum (Durchsatz) korrekt arbeitet: Der Speicher ist
hier eine hydraulische Weiche und kein Vorrat. Der Doppelzählungs-Freibeweis greift
sichtbar: Direktdeckung 0, Eigenanteil ausschließlich aus der zugerechneten Entladung.

### 7.6 Präparierte Szenarien — alle drei Fahrweisen

Projekt **1017** (BHKW → Heizkessel, 63 MWh Wärme- und 665 MWh Strombedarf), jeweils mit
1000-l-Pendelspeicher (60/40 °C). 1018 eignet sich für die stromgeführten Fahrweisen
nicht: Es hat praktisch keinen Strombedarf, dort produziert das BHKW in `modeBHKW = 1`
und `2` gar nichts (nachgemessen: 0 MWh in beiden Pfaden — ein gültiger, aber
nichtssagender Fall).

| Fahrweise | Größe | Flag AUS | Flag AN |
|---|---|---|---|
| **0 — wärmegeführt** | BHKW-Wärme / -Strom | 60,228 / 31,699 MWh | 60,190 / 31,679 MWh |
| | Betriebsstunden | 3.169,90 | 3.167,88 |
| | Direktdeckung / Speicherladung | — | 23,795 / 36,395 MWh |
| | Kessel-Nutzwärme | **11,807 MWh** | **2,740 MWh** |
| | Speicher | — | Ladung 36.394,613, Entladung 36.372,573, SOC_Ende 22,04, Vollzyklen 1.568,73, **Bilanzfehler 0**, **8760/8760** |
| | Deckungssumme | **114,508 %** | **100,000001 %** |
| | Stundenbilanz | — | max **1,9·10⁻⁶ kWh** |
| **1 — stromgeführt** | BHKW-Wärme / -Strom | 166,440 / 87,600 MWh | 166,440 / 87,600 MWh (**identisch** — die Zuschaltung folgt dem Strom) |
| | Betriebsstunden | 8.760 | 8.760 |
| | Wärmeüberschuss (verworfen) | 106,212 MWh | 106,250 MWh |
| | Direktdeckung / Speicherladung | — | 22,542 / 37,648 MWh |
| | Kessel-Nutzwärme | 4,419 MWh | 2,740 MWh |
| | Speicher | — | Ladung 37.647,594, Entladung 37.625,554, Vollzyklen 1.622,74, **Bilanzfehler 8,7·10⁻¹³**, **8760/8760** |
| | Deckungssumme | **271,601 %** | **100,000001 %** |
| | Stundenbilanz | — | max **1,9·10⁻⁶ kWh** |
| **2 — ohne Einspeisung** | BHKW-Wärme / -Strom | 62,931 / 33,122 MWh | 62,930 / 33,121 MWh |
| | Betriebsstunden | 3.312,18 | 3.312,11 |
| | Direktdeckung / Speicherladung | — | 33,353 / 29,577 MWh |
| | Kessel-Nutzwärme | **9,600 MWh** | **0 MWh** |
| | Speicher | — | Ladung 29.576,794, Entladung 29.554,754, Vollzyklen 1.274,86, **Bilanzfehler 0**, **8760/8760** |
| | Deckungssumme | **115,297 %** | **100,000000 %** |
| | Stundenbilanz | — | max **1,9·10⁻⁶ kWh** |

Der Bilanzfehler des Altpfads, direkt gemessen als
`max |waermerestbedarf − (Bedarf − Produktion)|` über alle 8760 Stunden:

| Fahrweise (Flag AUS, 1017) | max je Stunde | Summe der Beträge |
|---|---|---|
| 0 — wärmegeführt | 15,20 kWh | 18.230 kWh |
| 1 — stromgeführt | 19,00 kWh | 109.700 kWh |
| 2 — ohne Einspeisung | **23,26 kWh** (= volle Kapazität) | 19.220 kWh |

Ohne Pendelspeicher — dem heutigen Zustand aller neun Referenzprojekte — ist dieselbe
Größe exakt **0 kWh** (1017) bzw. **3,8·10⁻⁶ kWh** (1018, reine `float`-Rundung).

**Erklärung jeder Abweichung:**

1. **BHKW-Wärme fast gleich** (−0,06 % bis −0,15 % in Fahrweise 0 und 2, exakt gleich in
   Fahrweise 1): Der Speicher ist um 0,24 % kleiner (Konstante 1,16 gegen 1,16279) und die
   Abschaltschwelle kappt bei 95 %. Beides verkleinert den Wärmeraum leicht; die
   Zuschaltentscheidung ändert sich dadurch nur in wenigen Grenzstunden. In der
   stromgeführten Fahrweise geht der Speicher gar nicht in die Zuschaltung ein — dort ist
   die Produktion **bitgleich**.
2. **Kessel-Nutzwärme deutlich kleiner**: der Bilanzfehler (3.5). Im Altpfad bekommt der
   Kessel ein Lastprofil, in dem die vom BHKW eingespeicherte Wärme bereits als gedeckt
   gilt und die negativen Stundenreste weggeklemmt sind. Der Kessel deckt dadurch Bedarf,
   den es nicht gibt.
3. **Restwärme des Projekts unverändert** (0 MWh in 1017, 0,05178 MWh in 1018): Die
   Verschiebung ist eine zwischen den Erzeugern, keine Energieänderung.
4. **Deckungssumme**: 3.6.
5. **Wärmeüberschuss stromgeführt +0,038 MWh**: Der neue Speicher nimmt 95 % statt 100 %
   auf; der Überhang wird verworfen. Fachlich dieselbe Größe wie im Altpfad.

### 7.7 Kodierung und Diff

| Datei | BOM | Zeilenenden | U+FFFD im Diff |
|---|---|---|---|
| `SimulationBHKW.cs`, `SimulationControl.cs`, `Kaskadenschleife.cs`, `SimulationRunner.cs`, `PufferSpCtrl.cs` | ja (unverändert) | CRLF, 100 % der Zeilen | **0** |

> **Berichtigt in der Nacharbeit (Befund N12b).** „`git diff --check` meldet nichts" war zu
> pauschal. Richtig ist: Der Befehl meldet **zwei** Treffer (`trailing whitespace`), und
> beide stammen aus der **gesperrten** Nutzerdatei `Views/BHKW/Form_BHKWEing.cs`
> (Zeilen 395 und 407) — sie war beim Beginn von Paket 6 bereits geändert und wurde nicht
> angefasst. Die von Paket 6 geänderten Engine-Dateien sind sauber.
>
> Der Stand nach der Nacharbeit steht in Teil 13.11; er umfasst neun statt fünf Dateien,
> alle mit unveränderter Kodierung.

Keine Designer- und keine `.resx`-Datei angefasst; die gesperrten Dateien sind unverändert.
**Nichts committet.**

Umfang des Diffs (Stand Paket 6 vor der Nacharbeit): 1.116 Einfügungen, 166 Löschungen
über fünf Dateien.

### 7.8 Build

```
MSBuild WP-Plan.sln                      -t:Rebuild -p:Configuration=Debug -p:Platform=x86  ->  0 Fehler
MSBuild Referenzlauf\Referenzlauf.csproj -t:Rebuild -p:Configuration=Debug -p:Platform=x86  ->  0 Fehler
```

Warnungen: **dieselben sechs Bestandswarnungen** (`WErzeugerModel.cs` CS0108,
`StromverbraucherStammCtrl.cs` CS0108, `KlimaregionStammCtrl.cs` 2 × CS0109,
`MDIMainForm.cs` CS4014 und CS1998) — **keine neue**, geprüft über einen vollständigen
`-t:Rebuild`.

---

## 8. Dokumentierte Ergebnisänderungen mit Flag AN

| # | Änderung | Wirkung | Grundlage |
|---|---|---|---|
| 1 | **Das BHKW deckt seinen Kanal nach `WS_Typ`** statt proportional über `Uebernehmen()` | in der Referenzmenge **keine**: 1017 und 1018 haben keinen Brauchwasserbedarf (WW-Kanal durchgehend 0), in 1024 steht das BHKW hinter WP und Kessel | 6.1 (Ende des Kompatibilitätsankers), 3.2 |
| 2 | **Das BHKW kann Puffer laden** (Haupt- und Zweitsenke, Vorgaberang 30) | 1024: der Brauchwasserpuffer 1054164 arbeitet erstmals — 32,53 MWh Umsatz | 6.5, 3.4 |
| 3 | **Der Pendelspeicher ist durch `SimulationPufferspeicher` abgelöst** | nur Projekte mit Pendelspeicher (in der Referenzmenge keines). Zahlen: 7.5/7.6 | 6.5, zweiter Punkt |
| 4 | **Kapazität aus der ΔT-Spreizung** statt fest 20 K, Abschaltschwelle 95 % | dito; Gegenüberstellung in 3.4 | 5.1, Paket-1-Vormerkung |
| 5 | **Bilanzfehler `SimulationControl` behoben** — der Rest ist der tatsächliche Rest | nur mit Speicher: bis −9,07 MWh Kesselwärme in 1017 | 6.5, 2.2 Punkt 8 |
| 6 | `Tab_ErgebnisBHKW.Restwaermebedarf` **und** `.Waermebedarfsdeckung` folgen dem **Eigenanteil** | ohne Speicher praktisch bitgleich (1017: 72,7626 → 72,762644 %, Differenz aus `float`- gegen `double`-Summation); mit Speicher siehe 13.2 | 6.4-Muster, Paket-5-Nacharbeit N1/N2, **Paket-6-Nacharbeit N1** |
| 7 | **`Waermeueberschuss` in allen drei Fahrweisen** dieselbe Größe (produziert, weder gedeckt noch gespeichert) | wärmegeführt und ohne Einspeisung meldeten bisher 0 | 6.5 |
| 10 | **`Tab_ErgebnisBHKW.Waermebedarf`, `Tab_ErgebnisHeizkessel.Waermebedarf` und die Solarthermie melden den Stufeneingang VOR Phase A** | 1024: BHKW 200,08 → 389,73 MWh, Kessel 240,97 → 389,73 MWh; damit dieselbe Bezugsgröße wie die Wärmepumpe | Nacharbeit N1 (13.2) |
| 11 | **`Tab_ErgebnisPufferspeicher.Ladung_gesamt/Entladung_gesamt/Vollzyklen` messen den Speicherumsatz OHNE Durchfluss** | 1008: 79.191 → 33.994 kWh, Vollzyklen 11.378,06 → 4.884,24; 1023: 109.993 → 70.872 kWh, Vollzyklen 7.901,81 → 5.091,37 | Nacharbeit N6 (13.7) |
| 12 | **Ganglinie `bhkw_restwaerme` entsteht an der BHKW-Position** statt als Projektrest nach Phase F | 1024: Jahressumme 46.135,57 → 229.846,79 kWh | Nacharbeit N4 (13.5) |
| 13 | **Der Ersatz-Pendelspeicher ohne Temperaturpaar rechnet mit ΔT = 20 K** statt 10 K, und die Zuordnungszeile `Z_ProjektPufferSp` wird ausgewertet | 1018 präpariert: Q_max 11,60 → 23,20 kWh bzw. 17,40 kWh über die Z-Zeile | Nacharbeit N2 (13.3) |
| 14 | **Das BHKW reserviert den Speicherraum, gegen den es in Phase B zugeschaltet hat** | präpariertes 1024: 19,02 MWh Wärmeverwurf → 0; −5,70 MWh Netzstrom | Nacharbeit N3 (13.4) |
| 15 | **Der Bilanzraum der BHKW-Zweitsenke enthält den Durchsatzterm**, wenn das BHKW letzte Bedarfsstufe ist und die Kanäle getrennt sind | präpariertes 1024: BHKW-Wärme 157,50 → 169,06 MWh, Heizstab 37,12 → 25,83 MWh, Netzstrom −16,57 MWh | Nacharbeit N5 (13.6) |
| 8 | Ein BHKW **zwischen zwei Mitgliedern** wird selbst Mitglied statt nach der Stufe zu rechnen | tritt in der Referenzmenge nicht auf; in 1024 wird dadurch der Heizkessel zum Zwischenmitglied (Änderung 9) | Nacharbeit N4, jetzt für alle vier Arten |
| 9 | **Projekt 1024**: BHKW und (dadurch) Heizkessel werden Stufenmitglieder | siehe Tabelle unten | 13.9, N4 |

### Projekt 1024 im Einzelnen (Flag AN, Paket 5 → Paket 6)

| Größe | Paket 5 | Paket 6 | Erklärung |
|---|---|---|---|
| `Pufferspeicher[0]` Ladung / Entladung | 0 / 0 | **32.528,04 / 31.936,64 kWh** | Die BHKW-Zweitsenke arbeitet |
| BHKW-Wärmeproduktion | 106,92 MWh | **160,48 MWh** | Das BHKW lädt zusätzlich den Brauchwasserpuffer |
| BHKW-Stromproduktion | 48,81 MWh | **73,26 MWh** | Koppelprodukt |
| BHKW-Betriebsstunden | 2.324,26 | 3.488,69 | dito |
| Heizstab | 62,81 MWh | **26,00 MWh** | Der Kessel deckt jetzt in Phase B, also **vor** dem Heizstab (13.9) |
| Kessel-Nutzwärme | 35,54 MWh | 40,89 MWh | dito |
| WP-Wärmeproduktion | 137,28 MWh | 116,82 MWh | Der Brauchwasserpuffer entlädt in **Phase A**, vor der Bedarfsphase der WP — sie hat weniger zu tun |
| `Waermepumpe.Restwaermebedarf` | 189,64 MWh | 46,14 MWh | `waermerestbedarf_stuendlich` ist der Rest nach der **gesamten** Stufe, die jetzt drei Mitglieder hat (Paket-5-Abgrenzung 4) |
| **Restwärme des Projekts** | 47,184 MWh | **46,136 MWh** | −1,05 MWh: der Speicher verwertet BHKW-Wärme, die vorher fehlte |
| **Reststrom** | 468,02 MWh | **405,96 MWh** | −62,06 MWh: mehr BHKW-Strom, weniger Heizstab |
| Deckungssumme / tatsächlich | — | 88,162164 % / 88,162172 % | keine Doppelzählung |

Alle Änderungen sind Folgen genau **einer** Konfiguration, die seit der Migration in der
Datenbank steht (Anlage 11257, `WS_Ziel2 = PufferBrauchwasser`, Puffer 1054164) und die
Paket 5 ausdrücklich als „ruht bis Paket 6" ausgewiesen hatte.

---

## 9. Bewusste Abgrenzungen und offene Punkte

| # | Punkt | Bewertung |
|---|---|---|
| 1 | **Der Bilanzfehler besteht im Altpfad weiter** | Entscheidung des Orchestrators; Wirkung quantifiziert in 3.5. Ein Altpfad-Fix wäre ein eigenes B0-artiges Paket |
| 2 | **Eine Senke je BHKW-Stufe**, nicht je Anlage | Die Fahrweisen schalten alle Module gemeinsam zu (2.3). Maßgeblich ist die Senke der ersten Anlage mit Puffer-Hauptsenke, sonst die der ersten Anlage; Abweichungen werden protokolliert. Eine Senke je Modul verlangte einen Umbau der Zuschaltlogik — neue Physik |
| 3 | **`Form_Simulation_Detail` zeigt weiter die Altformeln** für BHKW-Restbedarf (`:1749`) und -Deckung (`:1756`) | ANZEIGE-Aufgabe, gehört zu Paket 7 — dieselbe Abgrenzung wie beim Fehlerkanal (Paket-5-Nacharbeit 13.12, Punkt 8). Die gespeicherten Ergebnisse (`Tab_ErgebnisBHKW`) sind korrigiert |
| 4 | **Ein stromgeführtes BHKW hinter einer Wärmepumpe** in derselben Speicherstufe sieht den Strombedarf des STUFENEINGANGS, nicht den nach WP-Verbrauch und Heizstab | Innerhalb einer Stundenschleife gibt es die Vektorreihenfolge des Altpfads nicht mehr; der Heizstab derselben Stunde steht erst in Phase F fest. In der Referenzmenge ohne Wirkung (alle BHKW-Projekte fahren wärmegeführt; 1017/1018 haben keine WP in der Kaskade). Verwandt mit Befund N3 der Paket-5-Nacharbeit, dort für den ausgewiesenen Kessel-Strombedarf gelöst |
| 5 | **`bhkw_list` bleibt die Katalogliste** (`ID_BHKW`), `bhkw_anlagen_ids` die Anlagenliste | wie bei Kessel und Solarthermie; die Ergebnis-Modulnamen kommen weiterhin aus `bhkw_list_Namen` |
| 6 | **Kein neuer Speicherparameter** (Lade-/Entladeleistung je Speicher in kW) | unverändert offen aus Paket 4/5; `EntladeleistungMax` hält die Stelle |
| 7 | **Offene Konzeptfrage 5-2** (nachgelagerter Erzeuger nimmt dem vorgelagerten Speicher den Durchsatz) | unverändert offen. Mit dem BHKW als viertem Mitglied kann sie jetzt auch dort auftreten |
| 8 | **Zurechnungsregel der Speicherentladung** („Vermischung im Speicher", Momentanmischung, Zurechnung je Erzeuger*art*) | unverändert die Interimsregel aus der Paket-5-Nacharbeit; sie trägt jetzt vier statt drei Arten. Bestätigung offen — Nutzerentscheidung 5-1 |

### Bestandsbefunde, die dabei aufgefallen sind (nicht behoben)

| # | Befund | Wirkung |
|---|---|---|
| B-1 | **`SimulationBHKW` hat keine `Init()` im Altpfad.** `SimulationStromgefuehrt` nullt `s_waerme_MWh`/`s_strom_MWh` **nicht** (die beiden anderen Fahrweisen tun es) | Ein zweiter Lauf auf derselben Instanz — im Programm über `Form_Simulation_Detail` möglich — addiert in der stromgeführten Fahrweise auf die Vorwerte auf. Im Referenzlauf unsichtbar (je Projekt ein eigener Prozess). Der zweikanalige Weg setzt über `Init()` alles zurück |
| B-2 | **`bhkwGrenzleistungAllgemein /= 100` mutiert ein öffentliches Feld** | Zwei `Berechnung()`-Aufrufe auf derselben Instanz teilen zweimal. `SimulationControl` setzt den Wert vor jedem Lauf neu, deshalb heute ohne Wirkung |
| B-3 | **Keine Obergrenze für die Modulzahl im Altpfad.** Die Felder sind fest `[10]`; ab dem 11. BHKW läuft `Simulation_BHKW_Ctrl` in eine `IndexOutOfRangeException` | Wie B0-12 beim Kessel. Der zweikanalige Weg begrenzt auf `MAX_BHKW = 10` und meldet das dialogfrei auf die Konsole |
| B-4 | **Sommerbetrieb und beide Notschaltungen der wärmegeführten Fahrweise sind unerreichbar** (2.1) | Kein Ergebnis betroffen — aber die Absicht des Vorgängercodes (Sommer-/Winterfenster) ist damit seit Langem außer Kraft. Ob sie zurückkehren soll, ist eine fachliche Frage; siehe Nutzerentscheidung 6-2 |
| B-5 | **`SimulationSSP.Berechnung` ist ein Rumpf** (in der Nacharbeit aufgefallen, 13.10 c): Die Schleife über alle 35.040 Viertelstunden setzt `Stromgespeichert[i] = 0` und tut sonst nichts; die aus `Tab_Stromspeicher` gelesene Kapazität wird nicht benutzt | Ein Batteriespeicher rechnet in KEINEM Projekt und in KEINEM der beiden Rechenwege. Die Wechselwirkung mit einem stromgeführten BHKW ist deshalb strukturell null. Kein Paket-6-Effekt; gehört in ein eigenes Paket |

---

## 10. Offene Nutzerentscheidungen

### 6-1 — Soll der Bilanzfehler auch im Altpfad behoben werden?

**Der Befund.** `SimulationControl.Simulation_BHKW_Ctrl` verwirft den speicherbewussten
`waermerestbedarf` des BHKW und bildet den Rest als geklemmte Vektordifferenz. Gemessen
(7.6): bis zu **9,07 MWh** Kesselwärme und -brennstoff, die kein Bedarf angefordert hat.

**Umgesetzter Default:** Fix nur im neuen Pfad — der Altpfad bleibt byte-identisch.

| Variante | Wirkung | Preis |
|---|---|---|
| **A (umgesetzt)** | Altpfad unverändert, Flag-Disziplin als Rückfallebene | Der Fehler wirkt weiter, sobald ein Projekt einen Pendelspeicher bekommt |
| B | Altpfad mitkorrigieren | Neues Basis-Einfrieren nötig; sämtliche BHKW-Ergebnisse mit Pendelspeicher ändern sich. Eigenes B0-artiges Paket |

Solange **kein** Projekt der Datenbank einen Pendelspeicher trägt (heute: keines), ist der
Fehler latent und Variante A ohne praktische Folge.

### 6-2 — Sollen Sommerbetrieb und Notschaltungen zurückkehren?

**Der Befund.** Die wärmegeführte Fahrweise hat drei Betriebszweige, von denen zwei seit
der Änderung `if (stunde < 3600 || stunde > 5760)` → `if (stunde < 8760)` unerreichbar
sind (2.1). Mit ihnen sind auch die Schwellen 30/10/20 % außer Kraft.

**Umgesetzter Default:** Der tote Code bleibt unverändert stehen (Altpfad byte-identisch),
und der neue Weg bildet die Mindestfüllstände über die Hysterese des Speichers ab (3.3).

| Variante | Bewertung |
|---|---|
| **A (umgesetzt)** | Der Speicher regelt über `Schwelle_Ein`/`Schwelle_Aus` — jahreszeitunabhängig, projektweise einstellbar, konsistent mit allen anderen Erzeugern |
| B | Sommer-/Winterfenster wiederbeleben (Stunden 3600…5760) und die drei Schwellen als eigene Parameter einführen | verlangt drei neue Spalten und eine fachliche Klärung, warum ein BHKW im Sommer anders regeln soll als ein Kessel. **Wäre eine echte Ergebnisänderung in beiden Pfaden** |

### 6-3 — Verwendung des BHKW-Pendelspeichers

Der Ersatz-Pendelspeicher übernimmt `Verwendung` aus der Puffer-Zeile; Migrationsregel R6
legt sie als `Heizung` an. Ein BHKW mit reinem Brauchwasserbedarf müsste die Verwendung
von Hand umstellen. **Umgesetzt: die Zeile entscheidet** — keine Sonderregel für den
Pendelspeicher.

### 6-4 — Bezugsgröße des Restwärmebedarfs · **ENTSCHIEDEN in der Nacharbeit**

**Der Befund.** `Restwaermebedarf` war eine **Kaskadenpositions**-Größe: Stufeneingang
minus **Direktdeckung**. Bei Puffer-Hauptsenke ist die Direktdeckung konstruktiv 0
(Doppelzählungs-Freibeweis) — der Erzeuger meldete dann 100 % seines Stufeneingangs als
Restbedarf und gleichzeitig eine Deckung von 84 %. Am R6-Regelfall gemessen (1018 mit
Pendelspeicher als Hauptsenke): **141,45 MWh Restwärmebedarf bei 84 % Deckung.**

**Entscheidung des Orchestrators, einheitlich für Solarthermie, Heizkessel und BHKW:**

```
Waermebedarf         = Stufeneingang VOR Phase A          (altpfadkonform)
Restwaermebedarf     = Stufeneingang − EIGENANTEIL        (geklemmt >= 0)
Eigenanteil          = Direktdeckung + zugerechnete Speicherentladung
Waermebedarfsdeckung = Eigenanteil / Projektbedarf        (unverändert)
```

Damit sind Restbedarf und Deckung zwei Seiten derselben Rechnung, und der Wert bleibt
konstruktiv ≥ 0: Direktdeckung und zugerechnete Entladung stammen beide aus demselben
Stufeneingang. Gemessen am R6-Regelfall: **29,281 MWh statt 141,45 MWh.**

| Variante | Bewertung |
|---|---|
| **A (umgesetzt)** | `Stufeneingang − Eigenanteil`. Restbedarf und Deckung passen zusammen; die Größe ist bei jeder Senkenkonfiguration erklärbar |
| B | `Stufeneingang − Direktdeckung` (Stand vor der Nacharbeit) | Bei Puffer-Hauptsenke — dem Regelfall migrierter Datenbanken — meldet der Erzeuger seinen vollen Eingang als Rest |
| C | Rest NACH der ganzen Speicherstufe (das tut die Wärmepumpe) | Mit mehreren Mitgliedern melden alle denselben Wert; der Bezug zum einzelnen Erzeuger geht verloren |

Damit ist auch der offene Punkt der Paket-5-Nacharbeit („Bezugsgröße bei
Puffer-Hauptsenke") erledigt. **Offen bleibt die Wärmepumpe** — sie meldet weiterhin
Variante C; siehe Nutzerentscheidung 6-5.

### 6-5 — Der Restwärmebedarf der WÄRMEPUMPE (in der Nacharbeit neu aufgetaucht)

**Der Befund.** Nach der Umstellung aus 6-4 melden in Projekt 1024 alle drei Mitglieder
denselben `Waermebedarf` (389,73 MWh = Stufeneingang vor Phase A), aber:

| Erzeuger | Eigenanteil | `Restwaermebedarf` | Formel |
|---|---|---|---|
| Wärmepumpe | 142,82 MWh | **46,14 MWh** | Rest nach der GANZEN Stufe (inkl. Heizstab) |
| Heizkessel | 40,89 MWh | 348,84 MWh | Stufeneingang − Eigenanteil |
| BHKW | 159,88 MWh | 229,85 MWh | Stufeneingang − Eigenanteil |

Die Wärmepumpe bildet ihren Restbedarf in `Zweikanalig_StundeEnde` aus dem Kanalstand
nach Phase F. Mit **einem** Mitglied — dem Fall aller acht übrigen Referenzprojekte — ist
das dieselbe Zahl wie `Stufeneingang − Eigenanteil`; erst ab zwei Mitgliedern laufen die
Größen auseinander. Nach der Regel aus 6-4 müsste sie **246,91 MWh** melden.

**Nicht umgesetzt** — der Auftrag der Nacharbeit nannte ausdrücklich Solarthermie,
Heizkessel und BHKW. Eine Änderung an der Wärmepumpe hätte zusätzlich
`waermerestbedarf_stuendlich` betroffen und damit `Min_Spitzenkesselleistung`, die
Ganglinie `wp_restwaerme.csv` und die Altpfad-Formel der Deckung.

| Variante | Wirkung | Preis |
|---|---|---|
| **A (Stand heute)** | WP behält Variante C | Bei mehr als einem Stufenmitglied meldet die WP eine andere Größe als Kessel und BHKW — in der Referenzmenge betrifft das nur 1024 |
| B | WP auf `Stufeneingang − Eigenanteil` umstellen (nur der Skalar in `SimulationRunner`) | 1024: 46,14 → 246,91 MWh. Ganglinie und `Min_Spitzenkesselleistung` blieben unberührt — dann weichen Skalar und Ganglinie voneinander ab (das war Befund N4 beim BHKW) |
| C | WP vollständig umstellen (Skalar **und** Ganglinie) | zusätzlich `wp_restwaerme.csv` und `Min_Spitzenkesselleistung`; eigenes Paket wert |

---

## 11. Reproduktion

```powershell
$msb   = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
& $msb C:\Waermeplan\WP_Plan\WP-Plan.sln                      -p:Configuration=Debug -p:Platform=x86
& $msb C:\Waermeplan\WP_Plan\Referenzlauf\Referenzlauf.csproj -p:Configuration=Debug -p:Platform=x86

$exe   = "C:\Waermeplan\WP_Plan\Referenzlauf\bin\x86\Debug\net8.0-windows\Referenzlauf.exe"
$probe = "<Scratchpad>\Probe6\bin\x86\Debug\net8.0-windows\Probe6.exe"

# 1. Eigene, vollstaendig migrierte Kopie ausserhalb des Repos (produktive DB nur LESEN)
& $exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb C:\Waermeplan\Paket6_Test\DB_Basis

# 2. Regression mit Flag AUS - Pflicht, muss byte-identisch sein
foreach ($id in 1007,1008,1010,1011,1017,1018,1021,1023,1024) {
    & $exe projekt $id "C:\Waermeplan\Paket6_Test\Final_Aus\Projekt_$id" C:\Waermeplan\Paket6_Test\DB_Basis
}
& $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-14_B1-Fixes C:\Waermeplan\Paket6_Test\Final_Aus
& $exe pruefen   C:\Waermeplan\Paket6_Test\Final_Aus
# zusaetzlich MD5 ueber alle 208 CSV -> keine abweichende Datei

# 3. Flag AN: DB_Flag = Kopie von DB_Basis mit Kaskade_Zweikanalig = True (alle neun),
#    gesetzt per 32-bit-PowerShell + ACE.
foreach ($id in 1007,1008,1010,1011,1017,1018,1021,1023,1024) {
    & $exe projekt $id "C:\Waermeplan\Paket6_Test\Final_An\Projekt_$id" C:\Waermeplan\Paket6_Test\DB_Flag
}
# Gegenprobe gegen den Paket-5-Stand: die fuenf geaenderten Dateien sichern,
# 'git checkout --' darauf, neu bauen, Lauf_An_P5 rechnen, Dateien zurueckkopieren.
& $exe vergleich C:\Waermeplan\Paket6_Test\Lauf_An_P5 C:\Waermeplan\Paket6_Test\Final_An

# 4. Bilanzen, Abschluesse, Eigenanteile (rechnet, speichert NICHT)
& $probe C:\Waermeplan\Paket6_Test\DB_Flag 1017,1018,1024

# 5. Praeparierte Szenarien (je eine eigene Kopie von DB_Basis, per SQL gesetzt)
& $probe C:\Waermeplan\Paket6_Test\DB_PS20_AUS   1018   # Altpfad mit Pendelspeicher
& $probe C:\Waermeplan\Paket6_Test\DB_PS20_AN    1018   # Ersatzspeicher, dT 20 K
& $probe C:\Waermeplan\Paket6_Test\DB_PSNULL_AN  1018   # Ersatzspeicher ohne Temperaturpaar
& $probe C:\Waermeplan\Paket6_Test\DB_HS_AN      1018   # BHKW mit Puffer-HAUPTsenke
foreach ($m in 0,1,2) {
    & $probe "C:\Waermeplan\Paket6_Test\DB_17M${m}_AUS" 1017
    & $probe "C:\Waermeplan\Paket6_Test\DB_17M${m}_AN"  1017
}
```

`Probe6` rechnet über `SimulationRunner.Simuliere` und **speichert nichts**; es weist die
Eigenanteile, die Deckungssumme gegen die tatsächliche Projektdeckung, die
Speicherbilanzen, die Abschlusszähler, die Stundenbilanz des Projekts und die
Bilanzfehler-Probe aus. Die präparierten Datenbanken entstehen als Kopien von `DB_Basis`
mit einem `INSERT` in `Tab_Pufferspeicher` (Pendelspeicher) bzw. `UPDATE` auf
`Tab_Energieanlagen` (Senkenfelder) und `Tab_Einstellungen` (`Betriebsart`,
`Kaskade_Zweikanalig`).

**Die produktive `Kenndaten.accdb` wurde ausschließlich gelesen.** Vor dem Kopieren wurde
geprüft, dass keine `Kenndaten.laccdb` daneben liegt; alle Läufe liefen unter
`C:\Waermeplan\Paket6_Test\` außerhalb des Repos.

---

## 12. Kapitel 13 im Überblick

Die Nacharbeit zu den Review-Befunden N1–N13 steht in Kapitel 13. Sie hat **vier weitere
Dateien** berührt, **acht weitere** Ergebnisänderungen mit Flag AN erzeugt (Kapitel 8,
Zeilen 10–15) und vier Aussagen dieses Protokolls berichtigt (3.1, 3.3, Kapitel 4, 7.7).

---

## 13. Nacharbeit zu den Review-Befunden N1–N13

Zwei adversariale Reviews des Paket-6-Stands haben dreizehn Befunde ergeben. Dieses
Kapitel beschreibt je Befund den Fix, die Messung und die verbliebenen offenen Punkte.
Alle Zahlen stammen aus Läufen vom 15.08.2026 auf eigenen Datenbankkopien; die produktive
`Kenndaten.accdb` wurde ausschließlich gelesen.

### 13.1 Übersicht

| # | Schwere | Befund | Fix in |
|---|---|---|---|
| N1 | ERNST | Ergebnisgrößen der Speicherstufen-Module uneinheitlich | `SimulationBHKW`, `SimulationSPK`, `SimulationSolarthermie`, `Kaskadenschleife`, `SimulationRunner` |
| N2 | ERNST | Ersatzspeicher-Temperaturkette und stiller ΔT-Rückfall | `SimulationControl`, `SimulationPufferspeicher` |
| N3 | ERNST | Motorlast gegen veraltete Ladefähigkeit | `SimulationBHKW`, `SimulationPufferspeicher`, `Kaskadenschleife` |
| N4 | gering | Skalar und Ganglinie des Restwärmebedarfs uneinheitlich | `SimulationBHKW`, `Kaskadenschleife` |
| N5 | gering | BHKW-Zweitsenke ohne Durchsatzterm | `SimulationBHKW`, `Kaskadenschleife` |
| N6 | gering | Durchsatz statt Umsatz in `Tab_ErgebnisPufferspeicher` | `SimulationPufferspeicher` |
| N7 | gering | Ersatzspeicher nicht in Entladeordnung einsortiert | `SimulationControl` |
| N8 | gering | Toter Fehlerkanal, write-only-Größen | `SimulationBHKW`, `SimulationControl`, `SimulationRunner` |
| N9 | gering | Zustandsrest `Waermeueberschuss` | `SimulationBHKW` |
| N10 | gering | Toter Code | `SimulationControl`, `SimulationKanaele` |
| N11 | Doku | Exklusivitätsverlust des Pendelspeichers | dieses Kapitel (13.12) |
| N12 | Doku | Vier falsche oder unvollständige Aussagen | Kapitel 3.1, 3.3, 4, 7.7 |
| N13 | Test | Vier verfehlte Konstellationen | dieses Kapitel (13.10) |

### 13.2 N1 — Einheitliche Ergebnisgrößen der Speicherstufe

**Der Befund.** Im R6-Regelfall (Pendelspeicher als Puffer-HAUPTsenke — genau das, was
`ProjektPuffer.SQL_BHKW_AUF_PUFFER` und die Migration schreiben) meldete
`Tab_ErgebnisBHKW.Restwaermebedarf` **141,45 MWh** — 100 % des Stufeneingangs — bei
gleichzeitig **84 %** ausgewiesener Deckung. Ursache: Der Restbedarf zog nur die
DIREKTDECKUNG ab, und die ist bei Puffer-Hauptsenke konstruktiv 0. Zweitens war
`Tab_ErgebnisBHKW.Waermebedarf` im neuen Pfad der Stufeneingang **nach** der
Vorabentladung (Phase A) — gemessen an 1017 mit Pendelspeicher 62,91 → 27,80 MWh
(−56 %), und nirgends dokumentiert.

**Der Fix** (Orchestrator-Entscheidung, siehe Nutzerentscheidung 6-4), einheitlich für
Solarthermie, Heizkessel und BHKW:

| Größe | neu |
|---|---|
| `Waermebedarf` | Stufeneingang **vor Phase A** — dieselbe Bezugsgröße, die die Wärmepumpe seit Etappe 4b führt (`Zweikanalig_Start` nimmt die Kanäle vor der Schleife) |
| `Restwaermebedarf` | `Stufeneingang − Eigenanteil`, geklemmt ≥ 0 |
| `Waermebedarfsdeckung` | `Eigenanteil / Projektbedarf` (unverändert) |

Umgesetzt durch Verlagerung des Stufeneingangs von `Stunde_Bedarf` nach `Stunde_Start`
(`SimulationBHKW.cs:1261`, `SimulationSPK.cs:537`, `SimulationSolarthermie.cs:460`) — die
Kaskadenschleife ruft `Stunde_Start` vor Phase A und übergibt den Kanalstand
(`Kaskadenschleife.cs:288-291`). In der Vektorstufe gibt es keine Phase A; dort liefert der
Aufruf denselben Wert wie bisher. Die Bezugsgröße des Restbedarfs ändern
`SimulationRunner.cs:308` (BHKW), `:410` (Heizkessel) und `:500` (Solarthermie).

**Messung am R6-Regelfall** (1018, Pendelspeicher 1000 l / 60/40 °C als Hauptsenke an
beiden BHKW-Anlagen, Flag AN):

| Größe | vorher | nachher |
|---|---|---|
| `Tab_ErgebnisBHKW.Waermebedarf` | Eingang NACH Phase A | **185,166 MWh** (= Projektbedarf, VOR Phase A) |
| `Tab_ErgebnisBHKW.Restwaermebedarf` | **141,45 MWh** | **29,281 MWh** |
| `Tab_ErgebnisBHKW.Waermebedarfsdeckung` | 84,19 % | 84,19 % |
| Eigenanteil (Direktdeckung 0 + Entladung) | 155,885 MWh | 155,885 MWh |
| Deckungssumme / tatsächlich | 99,972036 % / 99,972036 % | 99,972036 % / 99,972036 % |

Die Erwartung des Auftrags („~29 statt 141 MWh") ist damit exakt getroffen.

**Wirkung auf die Referenzmenge** (Flag AN): nur Projekt 1024, weil es als einziges mehr
als ein Stufenmitglied hat.

| Größe | vorher | nachher |
|---|---|---|
| `BHKW.Waermebedarf` | 200,08 MWh | 389,73 MWh |
| `BHKW.Restwaermebedarf` | 72,13 MWh | 229,85 MWh |
| `Heizkessel.Waermebedarf` | 240,97 MWh | 389,73 MWh |
| `Heizkessel.Restwaermebedarf` | 200,08 MWh | 348,84 MWh |

Alle drei Mitglieder melden jetzt denselben Stufeneingang (389,73 MWh) — die
Speicherstufe ist EINE Stufe mit EINEM Eingang, und jeder Erzeuger weist seinen Anteil
daran aus. **Energetisch ändert sich nichts:** Produktion, Speicherumsatz, Restwärme und
Reststrom des Projekts sind unverändert (13.9).

**Offen geblieben:** die Wärmepumpe — siehe Nutzerentscheidung 6-5.

### 13.3 N2 — Temperaturkette und ΔT-Rückfall des Ersatzspeichers

**Der Befund.** `BhkwErsatzspeicherAufnehmen` wertete nur `PufferInfo.Vorlauf/Ruecklauf`
aus und fiel sonst auf den generischen 10-K-Notnagel zurück. Der Registry-Weg wertet
zusätzlich die Zeile `Z_ProjektPufferSp` aus — **derselbe Puffer bekam je nach Weg ein
anderes Q_max**. Und jeder Rückfall geschah stillschweigend.

**Der Fix**, dreiteilig:

1. **Temperaturkette auch im Ersatzspeicher-Weg** (`SimulationControl.cs:1194`). Die
   Zuordnungszeilen werden beim Registry-Aufbau ohnehin gelesen und stehen jetzt im Feld
   `_pspZuordnungen` (`:96`) — kein zweiter Datenbankzugriff.
2. **Rückfall 20 K für den PENDELSPEICHER** (`SimulationControl.cs:1221`,
   `SimulationPufferspeicher.Init` mit neuem Parameter `rueckfallDeltaT`). Die Altformel
   `Liter · 20 / 860` hatte 20 K fest verdrahtet; 20 K ist damit der **wertgleiche**
   Ersatz (1,16 gegen 1,16279 Wh/(l·K) — Abweichung 0,24 %), während die generischen
   10 K die Kapazität ohne fachlichen Grund halbieren. Für alle anderen Puffer bleibt es
   bei 10 K.
3. **Jeder Rückfall wird gemeldet** — `SimulationPufferspeicher.RueckfallDeltaT` hält den
   verwendeten Wert fest, `SimulationControl.RueckfallMelden` schreibt ihn auf die
   Konsole, an allen drei `Init`-Stellen (Registry-Block 1 und 2, Ersatzspeicher). Das
   Modell selbst bleibt dialog- und ausgabefrei.

**Messung** (1018 mit 1000-l-Pendelspeicher OHNE Puffer-Senke, also Ersatzspeicher-Weg):

| Fall | Q_max vorher | Q_max nachher | BHKW-Wärme | Kessel-Nutzwärme |
|---|---|---|---|---|
| kein Temperaturpaar, keine Z-Zeile | 11,60 kWh (10 K) | **23,20 kWh (20 K)** | 153,543 → 155,888 MWh | 31,574 → 29,229 MWh |
| kein Paar, aber Z-Zeile 70/55 °C | 11,60 kWh (10 K) | **17,40 kWh (15 K)** | → 154,753 MWh | → 30,364 MWh |
| Temperaturpaar 60/40 °C am Puffer | 23,20 kWh | 23,20 kWh (unverändert) | 155,888 MWh | 29,229 MWh |

Beide Rückfälle erzeugen jetzt eine Protokollzeile, zum Beispiel:

```
Speicher-Registry: Puffer 1054165 (BHKW-Pendelspeicher) hat KEIN Temperaturpaar - es gilt
der Rückfall ΔT = 20 K, nutzbare Kapazität Q_max 23,2 kWh. Ein gepflegtes Vorlauf-/
Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
```

und im Kettenfall:

```
BHKW-Pendelspeicher: Puffer 1054165 (BHKW-Pendelspeicher) hat kein Temperaturpaar in der
Projektkopie - es gilt die Zuordnungszeile (70/55 °C).
BHKW-Pendelspeicher: Keine Puffer-Senke am BHKW - der Speicher „BHKW-Pendelspeicher"
(1000 l, 70/55 °C, Q_max 17,4 kWh, Entladeprio 0, Obergrenze 95 % / mit PV 95 %) rechnet
als ZWEITSENKE mit.
```

Die Kapazitätstabelle in 3.4 gilt damit für den Pendelspeicher in einem Punkt nicht mehr:
Die Zeile „kein Paar gepflegt (Engine-Rückfall) | 10 K | 11,60 kWh | −50,1 %" ist für ihn
durch „20 K | 23,20 kWh | −0,24 %" ersetzt. Für alle anderen Puffer bleibt sie gültig.

### 13.4 N3 — Reservierung des Speicherraums

**Der Befund.** `Stunde_Bedarf` bemisst den Wärmeraum der Motoren an der Ladefähigkeit
der Zweitsenke — in **Phase B**. Eingelagert wird erst in **Phase D**. Dazwischen laufen
die Ladeaufträge der Erzeuger mit besserer Ladepriorität (Solarthermie 10, Wärmepumpe 20
gegen BHKW 30); sie können den Raum aufbrauchen, gegen den das BHKW gerade zugeschaltet
hat. Das BHKW hat dann bereits produziert, die Wärme wird verworfen, der Brennstoff ist
verbraucht.

**Der Fix** (minimal-invasiv, wie beauftragt): `SimulationPufferspeicher.Reserviert`
(`:258`), abgezogen in `Ladefaehigkeit` (`:578`). Das BHKW reserviert in Phase B **genau
die Menge, die es einlagern will** — nicht den ganzen Wärmeraum
(`SimulationBHKW.cs:1336`) — und gibt sie unmittelbar vor dem eigenen Ladevorgang wieder
frei (`SimulationBHKW.cs:1426`). Die Kaskadenschleife setzt alle Reservierungen zu Beginn
jeder Stunde zurück (`Kaskadenschleife.cs:286`), damit eine nicht eingelöste Reservierung
sich nicht weiterschleppt.

**Warum das weder den Bilanzraum noch den Doppelzählungs-Freibeweis verletzt:** Es wird
nichts zusätzlich geladen und nichts zusätzlich gedeckt — nur die **Reihenfolge der
Vergabe** wird festgehalten. `StundeAbschliessen` läuft unverändert genau einmal
(gemessen 8760/8760), und der Speicherumsatz ist in beiden Fassungen identisch.

**Messung** an einem präparierten 1024 (beide Wärmepumpen bekommen denselben
Brauchwasserpuffer 1054164 als Zweitsenke wie das BHKW; Ladeprio WP 20 < BHKW 30):

| Größe | vorher | nachher |
|---|---|---|
| **BHKW-Wärmeüberschuss (verworfen)** | **19,024 MWh** | **0,000001 MWh** |
| BHKW-Speicherladung | 11,908 MWh | 30,931 MWh |
| BHKW-Eigenanteil / Deckung | 139,526 MWh / 35,801 % | 158,266 MWh / 40,609 % |
| WP zugerechnete Entladung | 20,485 MWh | 1,746 MWh |
| **Reststrom des Projekts** | **412,908 MWh** | **407,205 MWh** (−5,703) |
| BHKW-Produktion | 158,816 MWh | 158,816 MWh (unverändert) |
| Speicher Ladung / Entladung | 32.747,63 / 32.131,81 kWh | 32.747,63 / 32.131,81 kWh (unverändert) |
| Restwärme des Projekts | 46,101 MWh | 46,101 MWh (unverändert) |
| Deckungssumme / tatsächlich | 88,170965 % / 88,170971 % | 88,170965 % / 88,170971 % |
| `StundeAbschliessen` | 8760/8760 | 8760/8760 |
| Speicher-Bilanzfehler | −2,0·10⁻⁹ | −1,0·10⁻⁹ |

Die 19,02 MWh, die vorher als Wärmeüberschuss verpufften, decken jetzt Bedarf; die
Wärmepumpe muss dafür 19,02 MWh weniger produzieren und spart 5,70 MWh Strom. **Der
Brennstoffeinsatz ist identisch** — es ist reiner Gewinn aus der Vermeidung von Verwurf.

**Verbleibender Rest:** Die Reservierung sichert die **Ladefähigkeit** des Speichers, nicht
das **Durchsatzbudget** desselben Kanals. Ein Erzeuger mit besserer Ladepriorität kann dem
BHKW in Phase C/D weiterhin den Durchsatz wegnehmen. Das berührt die offene Konzeptfrage
5-2 und bleibt dort vermerkt.

### 13.5 N4 — Skalar und Ganglinie des BHKW-Restwärmebedarfs

**Der Befund.** `Stunde_Ende` wird nach Phase F gerufen und schrieb den **PROJEKTrest**
in die Ganglinie `waermerestbedarf` — also den Stand nach dem Heizstab der Wärmepumpe und
nach den Beiträgen aller anderen Erzeuger. Der Skalar in `Tab_ErgebnisBHKW` entstand
dagegen an der BHKW-Position. Differenz in 1024: **72,13 gegen 46,14 MWh** — genau der
Heizstab (26,00 MWh).

**Der Fix.** Beide Größen bilden jetzt dieselbe Rechnung ab:

```
Rest = Stufeneingang − Direktdeckung − zugerechnete Speicherentladung
```

die Ganglinie stundenweise (`SimulationBHKW.Stunde_Ende`, `:1500`), der Skalar als
Jahressumme (`SimulationRunner`). Damit die Ganglinie den Stundenwert der Zurechnung
bekommt, führt die Kaskadenschleife die Herkunftsrechnung zusätzlich je Stunde
(`_entladungJeArtStunde`, `Kaskadenschleife.cs:141`) und übergibt sie beim Stundenende.

**Der Ausdruck ist konstruktiv nicht negativ:** Direktdeckung und Entladung einer Stunde
stammen beide aus dem Stufeneingang derselben Stunde und können ihn zusammen nicht
überschreiten.

**Die Projektrest-Übergabe ist davon getrennt** — sie läuft über die Kanäle
(`Waermekanaele`) beziehungsweise über `SimulationControl.Rest_Waermebedarf_stuendlich`
und war nie diese Ganglinie. Der Bilanzfehler-Fix aus 3.5 bleibt unberührt.

**Messung** (Differenz Skalar − Ganglinie, alle Flag-AN-Läufe):

| Lauf | Skalar | Ganglinie | Differenz |
|---|---|---|---|
| 1017 (ohne Speicher) | 17,134517 MWh | 17,134517 MWh | −3,6·10⁻¹⁵ |
| 1018 (ohne Speicher) | 34,324241 MWh | 34,324241 MWh | 1,4·10⁻¹⁴ |
| 1024 (Referenz) | 229,846793 MWh | 229,846792 MWh | 1,5·10⁻⁷ |
| 1018 R6-Regelfall | 29,281166 MWh | 29,281166 MWh | 6,8·10⁻⁸ |
| 1018 Ersatzspeicher 20 K | 29,281169 MWh | 29,281169 MWh | 5,6·10⁻⁸ |

Die Restdifferenzen sind die `float`-Ganglinie gegen die `double`-Jahressumme.

### 13.6 N5 — Durchsatzterm im Bilanzraum der BHKW-Zweitsenke

**Der Befund.** Der Wärmeraum der Zweitsenke war nur die Ladefähigkeit — ohne den
Durchsatzterm `+ min(offener Kanalbedarf, Entnahmefähigkeit)` des Bilanzraums 4b-1, den
der Heizkessel in Phase D längst hat. Folge: Ein BHKW mit Heizungs-Typ und
Brauchwasserpuffer als Zweitsenke läuft bei vollem Puffer nicht an, obwohl der Puffer
durchreichen könnte.

**Der Fix** steht in `SimulationBHKW.ZweitsenkenRaum` (`:1374`) — mit **zwei
Einschränkungen**, beide aus dem Grundsatz „es entsteht keine Wärme, die niemand
angefordert hat":

1. **Kanalüberschneidung.** Deckt das BHKW mit seinem `WS_Typ` denselben Kanal, den der
   Speicher bedient, ist dessen offener Bedarf bereits als Direktdeckung verplant. Der
   Durchsatzterm ist dann 0.
2. **Nur als LETZTE Stufe der Bedarfsreihenfolge.** Der Durchsatz der Ladephase bemisst
   sich am Budget `absehbar`, und das steht erst nach der **gesamten** Phase B fest. Nur
   wenn das BHKW dort zuletzt kommt, ist der Kanalstand, den es sieht, genau dieses
   Budget. Die Kaskadenschleife setzt dafür `BHKW.LetzteBedarfsstufe`
   (`Kaskadenschleife.cs:262`).

Die zweite Einschränkung ist **gemessen und nicht vorsorglich**: Ohne sie produzierte ein
präpariertes 1024 mit dem BHKW an Kaskadenposition 1 **+9,14 MWh, davon 8,87 MWh
verworfen** — die Schätzung lag nachweislich daneben, weil Wärmepumpe und Kessel den
Warmwasserkanal nach dem BHKW noch weiter abdeckten.

**Messung** an einem präparierten 1024 (BHKW als letzte Bedarfsstufe, alle Erzeuger auf
`WS_Typ = Heizung`, Brauchwasserpuffer 1054164 als BHKW-Zweitsenke — der Warmwasserkanal
bleibt also offen und der Puffer kann durchreichen):

| Größe | vorher | nachher |
|---|---|---|
| BHKW-Wärmeproduktion | 157,500 MWh | **169,062 MWh** (+11,56) |
| BHKW-Stromproduktion | 71,900 MWh | **77,180 MWh** (+5,28) |
| BHKW-Speicherladung | 34,685 MWh | 46,272 MWh |
| BHKW-Eigenanteil | 157,155 MWh | **168,489 MWh** |
| **BHKW-Wärmeüberschuss (verworfen)** | **0 MWh** | **0 MWh** |
| WP-Heizstab | 37,118 MWh | **25,826 MWh** (−11,29) |
| **Reststrom des Projekts** | 415,555 MWh | **398,984 MWh** (−16,57) |
| Restwärme des Projekts | 46,087 MWh | 46,044 MWh |
| Deckungssumme / tatsächlich | 88,174624 % / 88,174630 % | 88,185551 % / 88,185553 % |
| Speicher-Bilanzfehler / Abschlüsse | −7,6·10⁻¹² / 8760 | −2,7·10⁻⁹ / 8760 |

Der Durchsatzterm bringt das BHKW am vollen Puffer zum Anlaufen, **ohne dass etwas
verworfen wird**: Die zusätzliche Wärme geht durch den Speicher an den Warmwasserkanal,
der Heizstab schrumpft um 11,29 MWh und der Netzstrombezug um 16,57 MWh.

Zur Gegenprobe: Dasselbe Projekt mit dem BHKW an Kaskadenposition 1 ist nach der
Einschränkung **unverändert** gegenüber dem Stand ohne Durchsatzterm.

### 13.7 N6 — Durchsatz gegen Umsatz in `Tab_ErgebnisPufferspeicher`

**Der Befund.** Bei Puffer-Hauptsenke läuft die gesamte Produktion als
`Laden(…, durchlass)` durch den Speicher. Am R6-Regelfall gemessen: `Ladung_gesamt`
155.884 kWh und **6.719 Vollzyklen** an einem Speicher mit Q_max 23,2 kWh — die Kennzahl
maß den Durchsatz, nicht die Speicherbeanspruchung.

**Der Fix** zerlegt den Füllstand in `SimulationPufferspeicher`:

```
A = min(SOC, Q_max)          SPEICHERINHALT
B = max(0, SOC − Q_max)      Durchfluss dieser Stunde
```

`Laden` füllt zuerst A, dann B; `Entladen` entnimmt zuerst B, dann A; die
Bereitschaftsverluste treffen ebenfalls zuerst B. `Ladung_gesamt`, `Entladung_gesamt`,
`Verluste_gesamt` und die zugehörigen Ganglinien führen ab jetzt **nur noch A**, die
neuen Größen `Durchsatz_Ladung_gesamt`, `Durchsatz_Entladung_gesamt`,
`Durchsatz_Verluste_gesamt` und ihre Ganglinien nur noch B. `Vollzyklen` folgt damit
automatisch dem echten Umsatz.

**Beide Bilanzen gehen für sich exakt auf:**

```
Ladung_gesamt           − Entladung_gesamt           − Verluste_gesamt           = A
Durchsatz_Ladung_gesamt − Durchsatz_Entladung_gesamt − Durchsatz_Verluste_gesamt = B
```

und ihre Summe ist der bisherige Gesamtausdruck. **Ohne Durchlass — jeder Aufruf des
Altpfads — ist B durchgehend 0**, die Durchsatzgrößen bleiben exakt 0,0 und die drei
Altgrößen sind bitgleich die bisherigen (bestätigt durch die 208/208 MD5-Gleichheit,
13.9).

**Nicht persistiert:** `Tab_ErgebnisPufferspeicher` hat keine Spalte für den Durchsatz.
Eine Schemaänderung gehört nicht in diese Nacharbeit; die Größe steht am Objekt und geht
in die Protokollmeldungen ein. **Vorgemerkte Erweiterung** — sie gehört zu Paket 7
(Anzeigen) oder in ein eigenes Schema-Paket.

**Messung** (Flag AN, Referenzmenge — die einzigen Projekte mit Durchfluss):

| Projekt | Größe | vorher | nachher |
|---|---|---|---|
| 1008 | `Ladung_gesamt` | 79.191,28 kWh | **33.994,30 kWh** |
| 1008 | `Entladung_gesamt` | 78.633,18 kWh | **33.436,20 kWh** |
| 1008 | `Vollzyklen` | 11.378,06 | **4.884,24** |
| 1008 | Durchfluss (neu ausgewiesen) | — | 45.196,98 kWh, ein = aus |
| 1023 | `Ladung_gesamt` | 109.993,24 kWh | **70.871,89 kWh** |
| 1023 | `Entladung_gesamt` | 109.638,38 kWh | **70.517,03 kWh** |
| 1023 | `Vollzyklen` | 7.901,81 | **5.091,37** |
| 1023 | Durchfluss (neu ausgewiesen) | — | 39.121,35 kWh, ein = aus |
| 1024 | alle Größen | unverändert | unverändert (kein Durchfluss) |

`Ladung_gesamt − Entladung_gesamt` ist in beiden Projekten unverändert (1008: 558,1067;
1023: 354,853) — der Durchfluss hebt sich exakt auf, und die Energiebilanz
`Ladung − Entladung − Verluste = ΔSOC` geht mit den neuen Größen weiter auf
(1008: −3,3·10⁻¹⁰, 1023: −5,1·10⁻⁹).

Am R6-Regelfall: `Vollzyklen` **6.719 → 3.568,80** bei 82.796 kWh Speicherumsatz und
73.089 kWh Durchfluss (brutto 155.885 kWh).

### 13.8 N7 bis N10 — die übrigen Codebefunde

**N7 — Ersatzspeicher in Entlade- und Ladeordnung.** Der Ersatz-Pendelspeicher wurde per
`Add` ans Ende der Entladereihenfolge gehängt und bekam `Obergrenze`/`ObergrenzePV` fest
auf `SchwelleAus`. Beides ist angeglichen: `EntladeordnungEinsortieren`
(`SimulationControl.cs:1277`) setzt ihn über `Ladeordnung.Entladereihenfolge` an seinen
Platz nach `Entladeprio` (Konzept 3.6) und meldet den Ausnahmefall;
`ObergrenzenFuerErsatzspeicher` (`SimulationControl.cs:1318`) löst die Ladeobergrenzen
über `Ladeordnung.Ladereihenfolge` und `Ladeordnung.ObergrenzenAufloesen` auf — beide
Ausprägungen, mit und ohne PV-Überschuss (Konzept 3.4/3.5). Ohne Ladeeintrag bleibt es
bei `SchwelleAus`; das ist derselbe Wert, den die Auflösung der vorrangigen Anlage
zuweisen würde. Die aufgelösten Werte stehen in der Protokollzeile des Ersatzspeichers.

**N8 — Fehlerkanal und write-only-Größen.** `SimulationBHKW.Fehlertext` wurde nie belegt,
`Vorbereiten_Zweikanalig` lieferte immer `true` — die Auswertung in `SimulationControl`
war unerreichbar. Behoben:

* **Mehr als `MAX_BHKW` Module** setzen jetzt `Fehlertext` und liefern `false`
  (`SimulationBHKW.cs:1189`). **Bewusste Abweichung vom Heizkessel:** Der kürzt und
  rechnet weiter, weil sein Altpfad genau das tut (MessageBox + erste `MAX_SPK`) — das
  Verhalten bleibt dort dasselbe. Das BHKW hat diese Vorlage nicht: Sein Altpfad läuft ab
  dem 11. Modul in eine `IndexOutOfRangeException` (Bestandsbefund B-3). Es gibt also
  kein Verhalten zu erhalten, und ein stillschweigend gekürztes Ergebnis sähe plausibel
  aus, wäre aber falsch.
* **Pendelspeichervolumen ohne Puffer-Zeile** setzt `SimulationControl.Fehlertext` und
  `m_bError` (`:1161`), ebenso eine Puffer-Zeile, die zu einem anderen Projekt gehört
  (`:1174`). Beide Fälle sind über die Oberfläche nicht herstellbar — `PendelspeicherVolumenLiter`
  und `PendelspeicherId` lesen dieselbe Zeile —, sie sind Wächter gegen inkonsistente
  Datenbestände.

Die Kette ist **end-to-end geprüft**: Ein präpariertes 1017 mit **elf** BHKW-Anlagen
liefert

```
BHKW: Im Projekt sind 11 BHKW hinterlegt, die Simulation unterstützt maximal 10. Der Lauf
wurde abgebrochen, damit kein Ergebnis ohne die übrigen Module entsteht.
[…] FEHLER: Projekt 1017: BHKW: Im Projekt sind 11 BHKW hinterlegt, […]
```

mit `false` aus `SimuliereUndSpeichere(out fehler)` und Exitcode 3 im Referenzlauf —
**es wird kein Ergebnis gespeichert.**

Die fünf write-only-Größen sind angebunden:

| Größe | Anbindung |
|---|---|
| `Waermebedarf_gesamt` | Bezugsgröße von `Tab_ErgebnisBHKW.Waermebedarf` im zweikanaligen Weg (`double`-Summe statt 8760 `float`-Additionen) |
| `Speicherladung_gesamt`, `Speicherladung_stuendlich`, `Ueberschuss_stuendlich` | **Energieprobe** `SimulationBHKW.Energieprobe()`: `Produktion = Direktdeckung + Speicherladung + Überschuss`, über die Jahressumme UND über jede Stunde. Genau das, was der Kopfkommentar von `Ueberschuss_stuendlich` seit jeher verspricht. Eine Verletzung wäre ein Buchungsfehler in der Phasenstruktur und wird dialogfrei gemeldet. In **keinem** der geprüften Läufe hat die Probe angeschlagen |
| `Fehlertext` | siehe oben |

**N9 — Zustandsrest `Waermeueberschuss`.** Die Größe wurde nirgends auf den Laufanfang
gesetzt; nur die wärmegeführte Fahrweise überschrieb sie zufällig. Auf einer
wiederverwendeten Instanz — im Programm über `Form_Simulation_Detail` der Normalfall —
meldete ein Folgelauf den Überschuss seines Vorlaufs. Behoben durch
`Waermeueberschuss = 0f` in `Kennzahlen_Zuruecksetzen()` (`SimulationBHKW.cs:219`), dem
ersten Schritt **beider** Rechenwege.

**Nachweislich byte-neutral:** Das Feld ist mit `0f` initialisiert; beim ERSTEN Lauf einer
Instanz — dem Fall jedes Referenzlaufs (ein Prozess je Projekt) — setzt die Zeile 0 auf 0.
Bestätigt durch die 208/208 MD5-Gleichheit.

Bewusst **nicht** über den vorgeschlagenen Guard `if (sim.KaskadeZweikanalig)` im
`SimulationRunner` gelöst: Der hätte den Wert im Altpfad still auf 0 gezwungen und damit
den Überschuss der **stromgeführten** Fahrweise aus `Tab_ErgebnisBHKW` entfernt — eine
echte Altpfad-Regression. Der Reset behebt die Ursache statt des Symptoms und wirkt in
beiden Wegen und in beiden Richtungen.

**N10 — toter Code.** `SimulationControl.RestAufKanaeleZurueck` und
`SimulationControl.SchleifenstufeNach` sind **entfernt**; an ihrer Stelle steht je eine
Kommentarzeile, die sagt, was dort stand und warum es weg ist.

`Waermekanaele.Uebernehmen` **bleibt**, begründet (die zweite vom Auftrag angebotene
Variante):

1. Sie ist die in Konzept 6.1 **spezifizierte** Kanalarithmetik, nicht ein zufällig
   entstandener Helfer. Jede künftige einkanalige Stufe braucht genau diese Regel.
2. Ihre Zusage ist die einzige, die im Selbsttest festgenagelt ist — exakte Erhaltung im
   Normalbereich, höchstens ein ULP im Extremfall. Mit der Methode fielen **sechs der
   acht** Testfälle weg.

Der Unterschied zu den beiden entfernten Methoden ist genau das: Die waren private
Hilfsmittel ohne Zusage und ohne Test. Methodenkopf und Selbsttest-Kopf sind entsprechend
ergänzt; beide sagen jetzt ausdrücklich, dass es seit Paket 6 keinen produktiven Aufrufer
mehr gibt.

### 13.9 Verifikation nach der Nacharbeit

**1. Flag AUS — Regression (Pflicht), byte-identisch.** Neun Referenzprojekte gegen
`Referenzlaeufe/2026-08-14_B1-Fixes`:

```
Projekt_1007: PASS (29 Dateien, 324210 Werte)   Projekt_1018: PASS (19 Dateien, 210343 Werte)
Projekt_1008: PASS (21 Dateien, 227847 Werte)   Projekt_1021: PASS (21 Dateien, 227840 Werte)
Projekt_1010: PASS (18 Dateien, 201540 Werte)   Projekt_1023: PASS (25 Dateien, 262917 Werte)
Projekt_1011: PASS (29 Dateien, 324232 Werte)   Projekt_1024: PASS (26 Dateien, 271686 Werte)
Projekt_1017: PASS (20 Dateien, 245378 Werte)

GESAMT: PASS (2295993 Werte innerhalb der Toleranz)
MD5 über alle 208 CSV-Dateien: 0 abweichend
Referenzlauf.exe pruefen: GESAMT plausibel
```

**2. Flag AN — Änderungen gegenüber dem Paket-6-Stand vor der Nacharbeit.** Verglichen
wurden die `aggregate.csv` aller neun Projekte, Größe für Größe. **Vollständige Liste:**

| Projekt | Größe | vorher | nachher | Grund |
|---|---|---|---|---|
| 1007, 1010, 1011, 1017, 1018, 1021 | — | — | — | **keine einzige Abweichung** |
| 1008 | `Puffer.Ladung_gesamt` | 79.191,2835 | 33.994,3028 | N6 |
| 1008 | `Puffer.Entladung_gesamt` | 78.633,1768 | 33.436,1961 | N6 |
| 1008 | `Puffer.Vollzyklen` | 11.378,058 | 4.884,23891 | N6 |
| 1008 | `Pufferspeicher[0].Ladung_gesamt` | 79.191,28 | 33.994,30 | N6 |
| 1008 | `Pufferspeicher[0].Entladung_gesamt` | 78.633,18 | 33.436,20 | N6 |
| 1008 | `Pufferspeicher[0].Vollzyklen` | 11.378,06 | 4.884,24 | N6 |
| 1008 | `Vektor.puffer_ladung.Summe` | 79.191,2837 | 33.994,3031 | N6 |
| 1008 | `Vektor.puffer_entladung.Summe` | 78.633,1764 | 33.436,196 | N6 |
| 1023 | `Puffer.Ladung_gesamt` | 109.993,238 | 70.871,8871 | N6 |
| 1023 | `Puffer.Entladung_gesamt` | 109.638,385 | 70.517,0334 | N6 |
| 1023 | `Puffer.Vollzyklen` | 7.901,8131 | 5.091,3712 | N6 |
| 1023 | `Pufferspeicher[0].Ladung_gesamt` | 109.993,24 | 70.871,89 | N6 |
| 1023 | `Pufferspeicher[0].Entladung_gesamt` | 109.638,38 | 70.517,03 | N6 |
| 1023 | `Pufferspeicher[0].Vollzyklen` | 7.901,81 | 5.091,37 | N6 |
| 1023 | `Vektor.puffer_ladung.Summe` | 109.993,238 | 70.871,8875 | N6 |
| 1023 | `Vektor.puffer_entladung.Summe` | 109.638,385 | 70.517,0338 | N6 |
| 1024 | `BHKW.Waermebedarf` | 200,08 | 389,73 | N1 |
| 1024 | `BHKW.Restwaermebedarf` | 72,13 | 229,85 | N1 |
| 1024 | `Heizkessel.Waermebedarf` | 240,97 | 389,73 | N1 |
| 1024 | `Heizkessel.Restwaermebedarf` | 200,08 | 348,84 | N1 |
| 1024 | `Vektor.kessel_waermebedarf.Summe` | 240.971,641 | 389.729,716 | N1 |
| 1024 | `Vektor.bhkw_waermebedarf.Summe` | 200.079,008 | 389.729,716 | N1 |
| 1024 | `Vektor.bhkw_restwaerme.Summe` | 46.135,5736 | 229.846,792 | N4 |

**Das sind 23 Abweichungen in drei Projekten, alle durch N1, N4 oder N6 begründet.**
Keine Energiegröße ist darunter: Produktion, Verbrauch, Emissionen, Restwärme, Reststrom,
`SOC_Ende`, `SOC_Max`, `SOC_Mittel` und `Verluste_gesamt` sind in allen neun Projekten
unverändert. `Referenzlauf.exe pruefen` meldet „GESAMT plausibel".

**3. Energieerhaltung, Abschlüsse, Deckungssummen** (headless-Probe `Probe6`, rechnet über
`SimulationRunner.Simuliere` und speichert nicht):

| Projekt | Stundenbilanz Projekt, max | `StundeAbschliessen` | Speicher-Bilanzfehler | Deckungssumme / tatsächlich |
|---|---|---|---|---|
| 1007 | 1,9·10⁻⁶ kWh | — | — | 89,368498 % / 89,368505 % |
| 1008 | 5,7·10⁻⁶ kWh | 8760/8760 | −3,3·10⁻¹⁰ | 80,023657 % / 80,023624 % |
| 1010 | **0 kWh** | — | — | 100 % / 100 % |
| 1011 | 1,4·10⁻⁴ kWh | — | — | 5,404140 % / 5,403767 % |
| 1017 | **0 kWh** | — | — | 100,000001 % / 100 % |
| 1018 | 3,8·10⁻⁶ kWh | — | — | 99,972036 % / 99,972036 % |
| 1021 | **0 kWh** | 8760/8760 | Quellspeicher¹ | 100 % / 100 % |
| 1023 | 1,9·10⁻⁵ kWh | 8760/8760 | −5,1·10⁻⁹ | 67,854567 % / 67,854566 % |
| 1024 | 1,5·10⁻⁵ kWh | 8760/8760 | −6,5·10⁻¹⁰ | 88,162164 % / 88,162172 % |

¹ Ein QUELLspeicher startet gefüllt (`SOC = Q_max`); der Ausdruck
`Ladung − Entladung − Verluste − SOC` trägt deshalb die Erstfüllung (−4,512 kWh). Das ist
Bestandsverhalten und keine Bilanzverletzung — die Durchsatzgrößen sind dort exakt 0.

Die Stundenbilanz prüft
`Bedarf − Rest == Produktion(alle Erzeuger) + Heizstab + Entladung − Ladung − verworfene BHKW-Wärme`
auf dem PROJEKTbedarf; seit N6 gehen dort die **physikalischen** Flüsse ein, also
Speicherumsatz **plus** Durchfluss.

**4. Build.**

```
MSBuild WP-Plan.sln                      -t:Rebuild -p:Configuration=Debug -p:Platform=x86  ->  0 Fehler
MSBuild Referenzlauf\Referenzlauf.csproj -t:Rebuild -p:Configuration=Debug -p:Platform=x86  ->  0 Fehler
```

Warnungen: **dieselben sechs Bestandswarnungen** (`WErzeugerModel.cs` CS0108,
`StromverbraucherStammCtrl.cs` CS0108, `KlimaregionStammCtrl.cs` 2 × CS0109,
`MDIMainForm.cs` CS4014 und CS1998) — keine neue.

### 13.10 N13 — die vier Pflichtläufe

**(a) R6-Regelfall: 1018 mit Pendelspeicher als Puffer-HAUPTsenke.** Präpariert exakt so,
wie `PufferSpCtrl.SetPendelspeicherVolumenLiter` es schreibt: Puffer-Zeile
„BHKW-Pendelspeicher" (1000 l, 60/40 °C) plus `WS_Ziel = 'PufferHeizung'` und
`WS_ID_Puffer` auf **alle** BHKW-Anlagen des Projekts (`SQL_BHKW_AUF_PUFFER`).

| Größe | Flag AUS | Flag AN |
|---|---|---|
| BHKW-Produktion / Strom | 156,129 / 76,177 MWh | 155,888 / 76,065 MWh |
| Betriebsstunden | 6.383,66 | 6.368,52 |
| Direktdeckung / Speicherladung | — | **0** / 155,885 MWh |
| Kessel-Nutzwärme (= Gas) | 34,270 MWh | **29,229 MWh** |
| `Tab_ErgebnisBHKW.Waermebedarf` | 185,166 MWh | 185,166 MWh |
| `Tab_ErgebnisBHKW.Restwaermebedarf` | 185,166 MWh (Altformel) | **29,281 MWh** (vor der Nacharbeit: 141,45) |
| Restwärme des Projekts | 0,05178 MWh | 0,05178 MWh |
| Deckungssumme / tatsächlich | **102,826283 %** / 99,972036 % | **99,972036 %** / 99,972036 % |
| Speicher `PUFFER_1054165` | — | Q_max 23,20; **Umsatz 82.796,196**, **Durchfluss 73.088,639** (ein = aus), brutto 155.884,835; Verluste 0; SOC_Ende 0; SOC_Max 22,040001; **Vollzyklen 3.568,80** (vor N6: 6.719); Bilanzfehler −2,7·10⁻⁹; **Abschlüsse 8760/8760** |
| Stundenbilanz Projekt | — | max 4,8·10⁻⁶ kWh, Summe 0,000366 kWh |

Der Doppelzählungs-Freibeweis greift sichtbar: Direktdeckung 0, Eigenanteil ausschließlich
aus der zugerechneten Entladung. N1, N2, N4 und N6 sind an diesem einen Lauf gemeinsam
nachgewiesen.

**(b) Zwei BHKW-Anlagen mit VERSCHIEDENEN Senken.** 1018, Anlage 10370 auf den
Pendelspeicher als Hauptsenke, Anlage 10371 auf Heizkreis.

```
BHKW: Die Anlage 10371 hat eine andere Wärmesenke als die führende Anlage 10370. Die
Fahrweisen schalten alle Module gemeinsam zu; für die gesamte BHKW-Stufe gilt deshalb die
Senke der führenden Anlage.
```

| Größe | Wert |
|---|---|
| Modul 0 `EC_Power_15kw.el Gas` | 125,914 MWh Wärme / 62,543 MWh Strom / 4.114,83 h |
| Modul 1 `EC_Power_6kw.el FL` | 29,974 MWh Wärme / 13,522 MWh Strom / 2.253,69 h |
| Direktdeckung / Speicherladung | 0 / 155,885 MWh |
| Kessel-Nutzwärme | 29,229 MWh |
| Deckungssumme / tatsächlich | 99,972036 % / 99,972036 % |
| Speicher | Umsatz 82.796,196, Durchfluss 73.088,639, Vollzyklen 3.568,80, Bilanzfehler −2,7·10⁻⁹, **8760/8760** |

**Messergebnis:** Die Zahlen sind identisch mit (a) — beide Module laufen und laden
gemeinsam den Speicher der führenden Anlage. Die Abgrenzung „eine Senke je BHKW-Stufe"
(Kapitel 9, Punkt 2) ist damit **gemessen** und nicht nur protokolliert.

**(c) Stromgeführtes BHKW mit Batteriespeicher.** Zwei Läufe:

*1017* (BHKW → Heizkessel → … → **Stromspeicher**, `Betriebsart = 1`, Pendelspeicher
1000 l als Hauptsenke):

| Größe | Flag AUS | Flag AN |
|---|---|---|
| BHKW-Wärme / -Strom | 166,440 / 87,600 MWh | 166,440 / 87,600 MWh (**identisch** — die Zuschaltung folgt dem Strom) |
| Betriebsstunden | 8.760 | 8.760 |
| Wärmeüberschuss (verworfen) | 106,21 MWh | 106,25 MWh |
| Kessel-Nutzwärme | 4,419 MWh | **2,740 MWh** |
| `Tab_ErgebnisBHKW.Waermebedarf` | 62,91 MWh | 62,91 MWh (altpfadkonform) |
| `Tab_ErgebnisBHKW.Restwaermebedarf` | 4,42 MWh | **2,74 MWh** |
| `Tab_ErgebnisBHKW.Waermebedarfsdeckung` | **264,58 %** | **95,64 %** |
| `Sim.bSimulationSSP` | True | True |
| Reststrom | 588,820 MWh | **587,141 MWh** |
| Speicher | — | Q_max 23,20; Umsatz 54.637,590; Durchfluss 5.552,216; Vollzyklen 2.355,07; Bilanzfehler −4,3·10⁻¹¹; **8760/8760** |
| Deckungssumme / tatsächlich | 271,601 % / 100 % | 100,000001 % / 100 % |

*1007* (Photovoltaik **und** Stromspeicher in der Kaskade, um ein BHKW ergänzt,
`Betriebsart = 1`):

| Größe | Flag AUS | Flag AN |
|---|---|---|
| BHKW-Wärme / -Strom | 22,30 / 11,74 MWh | 22,30 / 11,74 MWh |
| Betriebsstunden | 1.173,5 | 1.173,5 |
| Wärmeüberschuss | 5,19 MWh | 6,44 MWh |
| WP-Wärmeproduktion | 17,89 MWh | 20,06 MWh |
| PV-Eigenverbrauch / -Überschuss | 2,19 / 3,87 MWh | 2,23 / 3,83 MWh |
| Reststrom | 21,689 MWh | **20,491 MWh** |
| Restwärme | 3,39641 MWh | 3,39473 MWh |
| Speicher (Pendelspeicher) | — | Q_max 23,20; Umsatz 15.823,56; Vollzyklen 682,05 |
| `ssp_gespeichert` (Batterie) | **0** | **0** |

**Bestandsbefund B-5 (neu):** `SimulationSSP.Berechnung` ist ein **Rumpf** — die Schleife
über alle 35.040 Viertelstunden setzt `Stromgespeichert[i] = 0` und tut sonst nichts. Die
Speicherkapazität wird aus `Tab_Stromspeicher` gelesen und dann nicht benutzt. Die
Wechselwirkung zwischen einem stromgeführten BHKW und einer Batterie ist deshalb
**strukturell null**, in beiden Rechenwegen gleichermaßen. Das ist kein Paket-6-Effekt und
war vor der Nacharbeit genauso; es gehört in ein eigenes Paket.

**(d) BHKW mit `WS_Typ = Warmwasser` gegen den Brauchwasserkanal.** 1024, Anlage 11257 auf
Hauptsenke Heizkreis mit `WS_Typ = 'Warmwasser'` (die Zweitsenke — der Brauchwasserpuffer
1054164 — bleibt).

| Größe | 1024 unverändert | 1024 mit `WS_Typ = Warmwasser` |
|---|---|---|
| BHKW-Produktion / Strom | 160,480 / 73,259 MWh | **44,449 / 20,291 MWh** |
| Betriebsstunden | 3.488,69 | 966,27 |
| Direktdeckung / Speicherladung | 127,946 / 32,528 MWh | **0** / 44,449 MWh |
| BHKW-Eigenanteil / Deckung | 159,883 MWh / 41,024 % | 43,765 MWh / 11,230 % |
| WP-Heizstab | 25,997 MWh | 51,769 MWh |
| Restwärme des Projekts | 46,136 MWh | 136,566 MWh |
| Deckungssumme / tatsächlich | 88,162164 % / 88,162172 % | 64,958914 % / 64,958883 % |
| Speicher | Vollzyklen 3.115,71, Bilanzfehler −6,5·10⁻¹⁰, 8760/8760 | Vollzyklen 4.257,53, Bilanzfehler −1,5·10⁻⁹, **8760/8760** |
| Stundenbilanz | max 1,5·10⁻⁵ kWh | max 1,3·10⁻⁵ kWh |

Das BHKW steht an Kaskadenposition 3 und sieht den Warmwasserkanal erst, nachdem die
Wärmepumpe ihn (mit Warmwasservorrang) bereits gedeckt hat — die **Direktdeckung ist
deshalb 0**, und die Maschine läuft nur noch für den Puffer. Das ist die richtige
Kanalregel: Ein Erzeuger mit `WS_Typ = Warmwasser` rührt den Heizkanal nicht an. Der
Anstieg des Heizstabs um 25,77 MWh ist die Kehrseite. Energieerhaltung, Abschlusszähler
und Deckungssumme bleiben in Ordnung.

### 13.11 Kodierung, Diff und Umfang nach der Nacharbeit

| Datei | BOM | Zeilenenden | U+FFFD im Diff |
|---|---|---|---|
| `SimulationBHKW.cs` | ja | CRLF 1690/1690 | 0 |
| `SimulationControl.cs` | ja | CRLF 2486/2486 | 0 |
| `Kaskadenschleife.cs` | ja | CRLF 636/636 | 0 |
| `SimulationRunner.cs` | ja | CRLF 592/592 | 0 |
| `SimulationSPK.cs` | ja | CRLF 840/840 | 0 |
| `SimulationSolarthermie.cs` | ja | CRLF 652/652 | 0 |
| `SimulationKanaele.cs` | ja | CRLF 608/608 | 0 |
| **`SimulationPufferspeicher.cs`** | **nein — wie im Bestand** | CRLF 690/690 | 0 |
| `PufferSpCtrl.cs` | ja | CRLF 982/982 | 0 |

`SimulationPufferspeicher.cs` ist die einzige Datei des Rechenkerns **ohne** BOM; dieser
Zustand ist erhalten geblieben. (Beim Bearbeiten war die Datei zwischenzeitlich auf reines
LF gefallen — das ist erkannt und vor dem Abschluss zurückgesetzt worden.)

`git diff --check` meldet **zwei** Treffer, beide in der gesperrten Nutzerdatei
`Views/BHKW/Form_BHKWEing.cs` (Zeilen 395 und 407, `trailing whitespace`). Die neun von
Paket 6 geänderten Dateien sind sauber.

Umfang des Gesamtdiffs (Paket 6 einschließlich Nacharbeit) über die neun Dateien:
**1.884 Einfügungen, 226 Löschungen**. Keine Designer- und keine `.resx`-Datei angefasst;
die gesperrten Dateien sind unverändert. **Nichts committet.**

### 13.12 N11 — Exklusivitätsverlust des Pendelspeichers (Ergebnisänderung)

**Der Wirkmechanismus.** Im Altpfad war der Pendelspeicher ein **skalarer, exklusiver**
Vorrat des BHKW: `kapazitaetPendelspeicher` existierte nur innerhalb von
`SimulationBHKW`, wurde nur von den drei Fahrweisen gefüllt und nur von ihnen geleert.
Kein anderer Erzeuger und kein anderer Verbraucher kam an ihn heran; was das BHKW
hineinschob, kam ausschließlich seinem eigenen Restbedarf zugute, und zwar an seiner
Kaskadenposition.

Im zweikanaligen Weg ist er ein `SimulationPufferspeicher` wie jeder andere und rechnet in
den Phasen A/E der gemeinsamen Stundenschleife:

* Er **entlädt in Phase A** — vor der Bedarfsphase **aller** Erzeuger. Seine Wärme deckt
  damit Bedarf, der sonst an eine Wärmepumpe oder einen Kessel gegangen wäre.
* Er **entlädt in seinen Kanal**, nicht in „den Bedarf des BHKW": Ein Pendelspeicher mit
  `Verwendung = Heizung` bedient den Heizkanal, gleich welcher Erzeuger dahinter steht.
* Er **kann von anderen Erzeugern geladen werden**, sobald deren Senkenreferenz auf ihn
  zeigt — und seine Entladung wird dann nach der Herkunftsrechnung auf die Erzeugerarten
  aufgeteilt.

**Orchestrator-Entscheidung: Die Exklusivität wird NICHT wiederhergestellt.** Die
einheitliche Speicherphysik ist der Zweck des Umbaus; ein Speicher mit Sonderrechten wäre
eine zweite Physik und damit genau das, was Konzept 6.2 und die Paket-5-Lehre N6
ausschließen. Wer den Pendelspeicher exklusiv halten will, erreicht das über die
Konfiguration — indem er ihn keinem anderen Erzeuger als Senke zuweist.

**Wirkung:** In der Referenzmenge **keine** — kein Projekt trägt einen Pendelspeicher
(2.2). Der Fall ist nur konstruierbar; die Größenordnung zeigt 13.4: Wenn zwei Erzeuger
denselben Speicher bedienen, verschiebt sich die zugerechnete Entladung zwischen ihnen
(dort 20,485 → 1,746 MWh zugunsten des BHKW).

### 13.13 Offene Punkte nach der Nacharbeit

| # | Punkt | Stand |
|---|---|---|
| 1 | **Bilanzfehler im Altpfad** | unverändert offen — Nutzerentscheidung 6-1 |
| 2 | **Eine Senke je BHKW-Stufe** | unverändert; jetzt gemessen statt nur protokolliert (13.10 b) |
| 3 | **`Form_Simulation_Detail` zeigt die Altformeln** | unverändert — Anzeige-Aufgabe, Paket 7. Mit N1/N4 weichen Anzeige und gespeichertes Ergebnis jetzt deutlicher voneinander ab |
| 4 | **Stromgeführtes BHKW sieht den Strombedarf des Stufeneingangs** | unverändert offen |
| 5 | **Kein Lade-/Entladeleistungs-Parameter je Speicher** | unverändert offen |
| 6 | **Konzeptfrage 5-2** (nachgelagerter Erzeuger nimmt den Durchsatz) | unverändert offen; N3 sichert die Ladefähigkeit, **nicht** das Durchsatzbudget (13.4) |
| 7 | **Zurechnungsregel der Speicherentladung** | unverändert Interimsregel — Nutzerentscheidung 5-1 |
| 8 | **Restwärmebedarf der WÄRMEPUMPE** | **NEU** — Nutzerentscheidung 6-5 |
| 9 | **Durchsatz nicht persistiert** | **NEU** — `Tab_ErgebnisPufferspeicher` braucht dafür eine Spalte; vorgemerkte Erweiterung (13.7) |
| 10 | **`SimulationSSP` ist ein Rumpf** | **NEU** — Bestandsbefund B-5 (13.10 c) |

### 13.14 Reproduktion der Nacharbeit

```powershell
$msb   = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
& $msb C:\Waermeplan\WP_Plan\WP-Plan.sln                      -t:Rebuild -p:Configuration=Debug -p:Platform=x86
& $msb C:\Waermeplan\WP_Plan\Referenzlauf\Referenzlauf.csproj -t:Rebuild -p:Configuration=Debug -p:Platform=x86

$exe   = "C:\Waermeplan\WP_Plan\Referenzlauf\bin\x86\Debug\net8.0-windows\Referenzlauf.exe"
$probe = "<Scratchpad>\Probe6\bin\x86\Debug\net8.0-windows\Probe6.exe"

# 1. Flag AUS - Pflicht, muss byte-identisch sein
foreach ($id in 1007,1008,1010,1011,1017,1018,1021,1023,1024) {
    & $exe projekt $id "C:\Waermeplan\Paket6_Test\NA_Final_Aus\Projekt_$id" C:\Waermeplan\Paket6_Test\DB_Basis
}
& $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-14_B1-Fixes C:\Waermeplan\Paket6_Test\NA_Final_Aus
& $exe pruefen   C:\Waermeplan\Paket6_Test\NA_Final_Aus
# zusaetzlich MD5 ueber alle 208 CSV -> 0 abweichend

# 2. Flag AN gegen den Stand vor der Nacharbeit (aggregate.csv Groesse fuer Groesse)
foreach ($id in 1007,1008,1010,1011,1017,1018,1021,1023,1024) {
    & $exe projekt $id "C:\Waermeplan\Paket6_Test\NA_An2\Projekt_$id" C:\Waermeplan\Paket6_Test\DB_Flag
}

# 3. Bilanzen, Abschluesse, Eigenanteile, Durchsatz (rechnet, speichert NICHT)
& $probe C:\Waermeplan\Paket6_Test\DB_Flag 1007,1008,1010,1011,1017,1018,1021,1023,1024

# 4. Szenarien der Nacharbeit (je eine eigene Kopie von DB_Basis, per SQL gesetzt)
& $probe C:\Waermeplan\Paket6_Test\NA_R6_AN     1018   # N13a  R6-Regelfall, Hauptsenke
& $probe C:\Waermeplan\Paket6_Test\NA_R6_AUS    1018   # dito, Altpfad
& $probe C:\Waermeplan\Paket6_Test\NA_ES20_AN   1018   # N2b   Rueckfall 20 K
& $probe C:\Waermeplan\Paket6_Test\NA_ESZ_AN    1018   # N2a   Z_ProjektPufferSp 70/55
& $probe C:\Waermeplan\Paket6_Test\NA_2BHKW_AN  1018   # N13b  zwei BHKW, zwei Senken
& $probe C:\Waermeplan\Paket6_Test\NA_SSP_AN    1017   # N13c  stromgefuehrt + Batterie
& $probe C:\Waermeplan\Paket6_Test\NA_WW_AN     1024   # N13d  WS_Typ = Warmwasser
& $probe C:\Waermeplan\Paket6_Test\NA_N3_AN     1024   # N3    Wettbewerb um den Speicherraum
& $probe C:\Waermeplan\Paket6_Test\NA_N5C_AN    1024   # N5    Durchsatzterm, BHKW letzte Stufe
& $exe   projekt 1017 <out> C:\Waermeplan\Paket6_Test\NA_MAX_AN    # N8   elf BHKW -> Abbruch
& $exe   projekt 1007 <out> C:\Waermeplan\Paket6_Test\NA_SSPPV_AN  # N13c PV + Batterie + BHKW
```

Die Vorher/Nachher-Messungen zu N3 und N5 entstanden über zwei **temporäre** Schalter in
`SimulationBHKW`, die den jeweiligen Fix abschalteten. Sie sind nach der Messung wieder
entfernt worden; der ausgelieferte Stand enthält sie nicht — nachgewiesen durch den
abschließenden `-t:Rebuild` und die Wiederholung der Flag-AUS-Regression (208/208 MD5).

**Die produktive `Kenndaten.accdb` wurde ausschließlich gelesen.** Vor dem Kopieren wurde
geprüft, dass keine `Kenndaten.laccdb` daneben liegt; alle Läufe liefen unter
`C:\Waermeplan\Paket6_Test\` außerhalb des Repos.

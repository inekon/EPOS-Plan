# Paket 8 — Engine-Protokoll: Protokoll- und Fehlerkanal statt MessageBox

**Stand 15.08.2026 · Status: umgesetzt, review-nachgearbeitet und verifiziert · Ergebnisneutral
nachgewiesen (9/9 Projekte, Flag aus UND Flag an, byte-/MD5-identisch)**

> **Nacharbeit der Review-Befunde N1–N14: siehe [Kapitel 10](#10-nacharbeit-der-review-befunde-n1n14).**
> Die Kapitel 1–9 beschreiben die Umsetzung; wo die Nacharbeit eine Aussage berichtigt hat, steht das
> an Ort und Stelle vermerkt.

Bezug: [`Konzept_Simulation_QuellenSenken.md`](Konzept_Simulation_QuellenSenken.md) Kapitel 13.4
(Entscheidung O4) und Kapitel 9 (Paket-Tabelle, Zeile 8) ·
[`../Reporting/Konzept_Variantenbericht.md`](../Reporting/Konzept_Variantenbericht.md) E10/3.4
(Muster `SimuliereUndSpeichere(… out fehler)`) ·
Vorarbeiten: [`Paket5_SolarKessel_Protokoll.md`](Paket5_SolarKessel_Protokoll.md) N10,
[`Paket6_BHKW_Protokoll.md`](Paket6_BHKW_Protokoll.md) N8

---

## 1. Auftrag und Abgrenzung

Paket 8 ist **Infrastruktur**. Es verändert **keinen Rechenweg** — weder mit gesetztem noch mit
abgewähltem Feature-Flag `Kaskade_Zweikanalig`. Was es verändert:

| | vorher | nachher |
|---|---|---|
| Meldeweg der Engine | MessageBox mitten im Rechenlauf | Protokoll-/Fehlerkanal, Anzeige durch den Aufrufer |
| Extrapolation der WP-Kennlinie | Rückfrage je Lauf (Ja/Nein) | Vorab-Einstellung `Extrapolation_erlaubt`, Default WAHR |
| Grenzfall „es wurde extrapoliert" | stumm (nach der ersten Antwort) | Protokollzeile, in Oberfläche und Lauf-Protokoll sichtbar |
| Abgebrochener Lauf in `Form_Simulation_Detail` | Ergebnisfelder aus Nullwerten, speicherbar | Meldung, Felder unberührt, Speichern gesperrt |
| DB-Fehler im Rechenpfad | MessageBox aus `DataRepository` | Protokolleintrag, Lauf läuft dialogfrei zu Ende |

Der bereits vorhandene **einstufige** Kanal aus Paket 5 (N10: „Kessel nicht hinterlegt") und Paket 6
(N8: `> MAX_BHKW`, Pendelspeicher ohne Puffer-Zeile) — `Fehlertext` am Modul →
`SimulationControl.Fehlertext` → `SimulationRunner.Simuliere(out fehler)` — wird verallgemeinert:
zu einem **zweistufigen** Kanal mit Fehlern (Abbruch) und Warnungen/Hinweisen (Lauf läuft weiter).

---

## 2. Bestandsaufnahme der Dialogstellen

### 2.1 Zahlenbilanz

Konzept 13.4 (Fassung 12) nennt **acht** MessageBox-Stellen in der Engine plus den
`DataRepository`-Pfad. Am heutigen Stand (nach B0, Paket 5 und Paket 6) ergibt die Nachzählung:

| | Konzept 13.4 | heutiger Stand | Bemerkung |
|---|---|---|---|
| Engine-Ordner `Allgemein/Simulation/` | 7 | **9** | +2 aus B0-12 (`SimulationSPK`, Kesselzahl); eine davon schon halb umgestellt (`mitDialog`) |
| `Z_ProjektPufferSpCtrl.Insert` | 1 | 1 | **nicht mehr aus dem Engine-Kontext erreichbar** (Nachweis 2.3) |
| `DataRepository` | 6 | 6 | unverändert |
| **datenbanknahe Controller im Rechenpfad** | — | **3** | im Konzept nicht erfasst; `ErgebnisCtrl` 2×, `PufferSpCtrl` 1× (Fund dieses Pakets) |
| **Summe** | 14 | **19** | |

Davon **19 umgestellt, 0 offen** (Begründung je Stelle in 2.2).

### 2.2 Stelle → Art → neue Behandlung

**A · Engine-Module (`Allgemein/Simulation/`)**

| # | Stelle (Zeile vorher) | Art | Neue Behandlung | Verhaltensänderung |
|---|---|---|---|---|
| 1 | `SimulationWaermepumpe.cs:256` — keine Kennlinie zum gewählten Vorlauf | Fehlermeldung + Abbruch | `Fehlertext` am Modul → `SimulationControl.Fehlertext` → Runner/UI; Fehler-Kanal | Abbruch unverändert; **neu**: der headless-Lauf speichert kein Ergebnis mehr |
| 2 | `SimulationWaermepumpe.cs:1461` — „Temperatur unter Minimum Kennlinie" | **Rückfrage** | Vorab-Einstellung `Extrapolation_erlaubt` (Kapitel 4); erlaubt → Hinweis-Kanal, verboten → Fehler-Kanal | keine, solange die Einstellung auf WAHR steht (= bisherige Antwort) |
| 3 | `SimulationWaermebedarf.cs:167` — Tagesverteilungstyp nicht gefunden | Fehlermeldung + `return` | Warnungs-Kanal, `return` unverändert | keine |
| 4 | `SimulationWaermebedarf.cs:721` — Prozesstyp nicht definiert („DerTyp") | Fehlermeldung + `return` | Warnungs-Kanal, `return` unverändert; Tippfehler behoben | keine |
| 5 | `SimulationWaermebedarf.cs:828` — Brauchwassertyp nicht definiert | Fehlermeldung + `return` | wie 4 | keine |
| 6 | `SimulationStrombedarf.cs:68` — Stromprofile nicht berechenbar | Fehlermeldung + `return` | neuer `Fehlertext` an der Klasse; Runner und `Form_Simulation_Detail.Energiebedarf` brechen ab | **ja, im Fehlerfall**: bisher rechnete der Lauf mit leerem Stromprofil weiter |
| 7 | `SimulationStrombedarf.cs:207` — Sammel-`catch` der Profilrechnung | Fehlermeldung | Fehler-Kanal; Rückgabe `null` unverändert (mündet in 6) | siehe 6 |
| 8 | `SimulationSPK.cs:126` — mehr als `MAX_SPK` Kessel (B0-12, Altpfad) | Hinweis | Warnungs-Kanal, Kappung unverändert | keine |
| 9 | `SimulationSPK.cs:189` — Kessel im Projekt nicht hinterlegt (B0-3) | Fehlermeldung + Abbruch | Parameter `mitDialog` **entfallen**; beide Rechenwege melden über `Fehlertext` | **ja, im Fehlerfall**: der Altpfad rechnete mit genullter Restwärme weiter und speicherte das |

Zusätzlich auf den Hinweis-Kanal aufgesetzt (waren nie Dialoge, aber nur auf der Konsole und damit
im Programm unsichtbar): `SimulationControl` — Migrationssperre, Ladeprioritäts-Vorbelegung, alle
`Kaskadenkontext.Hinweise` (ΔT-Rückfall aus Paket 6, Senken-Rückfall aus Paket 5 N5,
Zwischenstufen-Abgrenzungen).

**B · `DataRepository` — der Fehlerpfad im Engine-Kontext**

| # | Stelle | Art | Neue Behandlung |
|---|---|---|---|
| 10–15 | `GetDataTable`, `ExecuteSQL`, `ExecuteNonQuery`, `ExecuteInsertAndGetId`, `ExecuteScalar`, `DeleteWithDependencies` | Fehlermeldung | **eine** Entscheidungsstelle `DataRepository.FehlerMelden`: im Engine-Modus Protokolleintrag + Konsole, sonst MessageBox mit **unverändertem Wortlaut** |

**C · Datenbanknahe Controller im Rechenpfad** *(im Konzept nicht erfasst — Fund dieses Pakets)*

| # | Stelle | Art | Warum sie hierhergehört |
|---|---|---|---|
| 16 | `ErgebnisCtrl.Save:487` | Fehlermeldung | **letzte Station des headless-Laufs**; eine MessageBox hätte den Lauf noch NACH der vollständigen Rechnung blockiert |
| 17 | `ErgebnisCtrl.Delete(int):65` | Fehlermeldung | **Sicherheitsnetz** — siehe Berichtigung unten |
| 18 | `PufferSpCtrl.CopyFromStamm:193` | Fehlermeldung | heute nur aus der Oberfläche erreichbar — als Sicherheitsnetz mit umgestellt |
| 19 | `Z_ProjektPufferSpCtrl.Insert:71` | Fehlermeldung | Konzept 13.4 führt sie als Engine-Stelle; **das ist sie nicht mehr** (2.3) — trotzdem über dieselbe Entscheidungsstelle |

> **Berichtigt in der Nacharbeit (Befund N14a).** Stelle 17 stand hier mit der Begründung „wird aus
> `Save` gerufen". Das ist falsch: `ErgebnisCtrl.Save` löscht den Vorgängerlauf **inline in seiner
> eigenen Transaktion** (`DELETE ID_Projekt FROM Tab_Ergebnis …`), `Delete(int idProjekt)` hat im
> Anwendungsprojekt derzeit **gar keinen Aufrufer**. Die Stelle ist damit dasselbe wie 18 und 19: ein
> Sicherheitsnetz für den Fall, dass der Weg je in den Rechenpfad gerät. An der Umstellung ändert
> das nichts, nur an ihrer Einordnung.

Alle vier laufen über `DataRepository.FehlerMelden`. **Außerhalb des Engine-Modus ist ihr Verhalten
Zeile für Zeile das bisherige.**

### 2.3 Nachweis: `Z_ProjektPufferSpCtrl` ist kein Engine-Pfad mehr

Volltextsuche im Anwendungsprojekt (ohne die Alt-Vollkopien und `.claude/worktrees`):

```
Views/Simulation/Form_Simulation_Config.cs:229, 873, 926, 937, 971   → UI
Allgemein/Simulation/SimulationControl.cs:96, 1928, 2098, 2188        → ausschließlich ReadAll
```

Die Simulation **liest** `Z_ProjektPufferSp` (Temperaturen der Alt-Zuordnung, Konzept 6.2), sie
schreibt dort nicht. `Insert` — und damit die MessageBox — kommt nur aus
`Form_Simulation_Config.btn_Speichern_Click`. Die Konzeptangabe stammt aus der Zeit vor Paket 2.

---

## 3. Kanal-Architektur

### 3.1 Die Klasse

`Allgemein/Simulation/SimulationProtokoll.cs` (neu, ~230 Zeilen) — genau die Form, die Konzept 13.4
vorzeichnet:

```csharp
public sealed class SimulationProtokoll
{
    public IList<string> Hinweise  { get; }
    public IList<string> Warnungen { get; }
    public IList<string> Fehler    { get; }
    public bool IstFehlerfrei      { get; }

    public void Hinweis(string text);
    public void HinweisEinmal(string schluessel, string text);   // gegen 8760-fache Meldungen
    public void Warnung(string text);
    public void WarnungEinmal(string schluessel, string text);
    public void Fehlermeldung(string text);                      // heißt so, weil `Fehler` die Liste ist

    public string AlsText(bool nurFehlerUndWarnungen = false);
    public string HinweistextFuerAnzeige();                      // Warnungen + Hinweise, für die UI

    public static SimulationProtokoll Aktuell { get; }
    public static SimulationProtokoll NeuStarten();
}
```

**Drei Stufen statt zwei.** Das Konzept nennt Hinweise, Warnungen und Fehler; der Auftrag zu Paket 8
fasst die ersten beiden als „Hinweise/Warnungen" zusammen. Umgesetzt sind alle drei Listen — die
Trennung kostet nichts und trägt die fachliche Unterscheidung: **Warnung** = gerechnet, aber mit
Ersatzannahme; **Hinweis** = vollwertig gerechnet, Randbedingung erwähnenswert.

### 3.2 Warum ambient (`Aktuell`) und nicht durchgereicht

Konzept 13.4 schreibt „`SimulationControl.Protokoll` … an alle Module durchgereicht". Umgesetzt ist
ein prozessweiter Kanal mit `SimulationControl.Protokoll` als Zeiger darauf. Begründung:

1. **Die meldenden Stellen liegen zu tief.** `berechne_wptherm` sitzt fünf Ebenen unter
   `Do_Simulation`, in der Stundenschleife. Ein durchgereichter Parameter hätte rund vierzig
   Signaturen berührt — also genau den Rechenpfad, den ein Infrastrukturpaket nicht anfassen darf.
2. **Zwei Melder kennen `SimulationControl` gar nicht.** `SimulationWaermebedarf` und
   `SimulationStrombedarf` laufen **vor** der Kaskade und werden von Formular bzw. Runner gerufen.
3. **Eindeutigkeit ist gegeben** — solange höchstens ein Lauf gleichzeitig rechnet.

> **Berichtigt in der Nacharbeit (Befund N7).** Punkt 3 stand hier als „die Anwendung ist einläufig
> (ein MDI-Thread)". Das stimmt nicht: Der **Berichtspfad rechnet auf einem ThreadPool-Thread** —
> `BerichtsDatenSammler.Sammle` läuft in `Task.Run`, gerufen aus `Form_Bericht`,
> `Form_Wirtschaftlichkeit` und `Form_WirtschaftlichkeitVerlauf`. Die Eindeutigkeit trägt eine
> andere, bis dahin unausgesprochene Invariante:
>
> **MODALITÄTS-INVARIANTE.** Alle drei genannten Formulare werden ausschließlich über
> `ShowDialog()` geöffnet (aus `Form_Variantentest` bzw. aus `Form_Wirtschaftlichkeit`). Solange
> einer davon offen ist, kann der MDI-Thread keinen zweiten Lauf starten — der Simulationsknopf der
> Detailansicht ist nicht erreichbar. Dazu kommt die Prozessgrenze der Referenzlauf-Suite: Sie
> rechnet **jedes Projekt in einem eigenen Kindprozess**.
>
> Dieselbe Invariante trägt `DataRepository._stillTiefe` und `_stilleFehler` (Engine-Modus, 3.6).
> **Wer eines dieser Formulare je nicht-modal öffnet, bricht sie** — dann gehören Kanal und
> Engine-Modus threadgebunden. Die Invariante ist jetzt in beiden Klassen im Kopfkommentar benannt.
> Die Härtung selbst (`[ThreadStatic]`/`AsyncLocal`) ist **vorgemerkt, nicht umgesetzt**: Sie trägt
> nur, wenn ALLE Lese-/Schreibpaare threadrein sind, und das sind sie heute nicht —
> `Form_Simulation_Detail` schreibt über die Engine und liest `Aktuell` anschließend auf dem
> UI-Thread, der Berichtspfad schreibt und liest auf dem Worker. Eine halbe Umstellung wäre
> schlechter als die benannte Invariante (Kapitel 9, Punkt 8).

Die Listen sind trotzdem `lock`-geschützt — der `DialogWaechter` der Suite läuft in einem
Hintergrundthread, und die Kosten sind null.

### 3.3 Wer den Kanal öffnet

| Einstiegspunkt | Aufruf | Zeitpunkt |
|---|---|---|
| `SimulationRunner.Simuliere` | `SimulationProtokoll.NeuStarten()` | **vor** der Bedarfsrechnung |
| `Form_Simulation_Detail.btn_Simulation_Click` | dito | **vor** `Energiebedarf(...)` |
| `SimulationControl.Do_Simulation` | `Protokoll = SimulationProtokoll.Aktuell` | dockt nur an — ein eigener Kanal hier hätte die Meldungen der Bedarfsrechnung verworfen |

### 3.4 Konsole bleibt

Jeder Eintrag geht zusätzlich über `Console.WriteLine("Simulation <Art>: …")`. Die Lauf-Protokolle
der Referenzlauf-Suite lesen die Konsolenausgabe der Kindprozesse mit — genau darüber ist die
Extrapolations-Protokollzeile in Kapitel 7.3 nachgewiesen. Die vorhandenen `Console.WriteLine` der
Engine sind auf den Kanal **aufgesetzt** (nicht zusätzlich), es gibt also keine Doppelausgabe.

### 3.5 Signaturen bleiben kompatibel

```csharp
var runner = new SimulationRunner();
int id = runner.SimuliereUndSpeichere(idProjekt, out string fehler);   // unverändert
//   fehler            = Abbruchgrund + Warnungen  (AlsText(nurFehlerUndWarnungen: true))
//   runner.Protokoll  = zusätzlich die Hinweise   (neue, optionale Abfrage)
```

Kein bestehender Aufrufer musste angepasst werden. `Protokoll` überlebt den Aufruf, damit der
Variantenbericht (Konzept dort 3.4) die Hinweise in seine Hinweisliste übernehmen kann.

### 3.6 `DataRepository` im Engine-Modus

```csharp
using (DataRepository.EngineModus())        // zählend, verschachtelbar
{
    …rechnen…
    foreach (string m in DataRepository.StilleFehlerAbholen())
        Protokoll.Warnung("Datenbankzugriff während des Laufs: " + m);
}
```

Gesetzt an drei Stellen: `SimulationRunner.Simuliere` (ganzer Lauf),
`SimulationRunner.SimuliereUndSpeichere` (zusätzlich um **Ergebnisaufbau und** `ErgebnisCtrl.Save` —
berichtigt, Befund N4), `SimulationControl.Do_Simulation` (innere Absicherung, falls die Engine ohne
Runner gerufen wird). **`Form_Simulation_Detail` setzt ihn nicht** — der Lauf aus der Detailansicht
steht auf dem UI-Thread, dort ist der Dialog die richtige Meldung, und die innere Absicherung in
`Do_Simulation` deckt den Rechenteil trotzdem ab (berichtigt, Befund N14c).

Der Schalter **zählt**, weil sich diese Bereiche schachteln; die Freigabe steht in `Dispose`, läuft
also auch bei einer Ausnahme.

**Kappung:** höchstens **50 Meldungen je Abholung**, plus eine Zeile „… weitere Meldungen
unterdrückt" beim Überlauf (Befund N12a). Abgeholt wird bis zu dreimal je Lauf
(`Do_Simulation`, `Simuliere`, `SimuliereUndSpeichere`) — im Grenzfall stehen also bis zu
**3 × 51 = 153** Zeilen im Kanal, nicht 50 (berichtigt, Befund N14b).

**Für jede andere Nutzung von `DataRepository` ändert sich nichts** — ohne gesetzten Schalter kommt
die MessageBox wie bisher, mit demselben Wortlaut.

---

## 4. Einstellung `Extrapolation_erlaubt`

### 4.1 Verhalten

```
erlaubt (WAHR, Vorbelegung)  → es wird extrapoliert wie bisher, Zeile für Zeile derselbe
                               Rechenweg; EINMAL je Anlage ein Hinweis im Protokoll
verboten (FALSCH)            → Abbruch über den Fehlerkanal mit sprechendem Text
                               (Anlage, Untergrenze der Kennlinie, Abhilfe)
```

Der Ausgang „verboten" ist derselbe Programmpfad wie das bisherige „Nein" (`result[STATUS] = 0`) —
nur der Meldeweg ist neu.

### 4.2 Default WAHR — bewusste Abweichung vom Konzept

Konzept 13.4 schreibt „Default **nein**. Bei nein wird auf die unterste Stützstelle gekappt". Das
ist **nicht** umgesetzt, aus zwei Gründen:

1. **Ergebnisneutralität.** Der Referenzlauf B1-Fixes weist für **fünf von neun** Projekten die
   Rückfrage aus, jedes Mal mit der Antwort „Ja". Ein Default „nein" hätte diese fünf Projekte
   entweder abbrechen lassen oder anders gerechnet. Paket 8 ist Infrastruktur.
2. **Die Kappung wäre eine Rechenänderung.** „Auf die unterste Stützstelle kappen" liefert andere
   COP- und Leistungswerte als die lineare Verlängerung. Das gehört in ein Paket mit eigener
   Ergebnisbewertung, nicht in den Protokollumbau.

**Dokumentierte Alternativen für die Nutzerentscheidung** (Kapitel 9, Punkt 1): Default umdrehen ·
Kappung als dritte Stufe ergänzen · beides.

### 4.3 Wo sie gelesen und geschrieben wird

| Ort | Weg |
|---|---|
| `KonfigurationCtrl.ReadSingle` | **namensbasiert** über `SchemaKatalog.SPALTE_EXTRAPOLATION_ERLAUBT`; Ordinalkette `row[0..22]` unangetastet |
| `KonfigurationCtrl.ExtrapolationErlaubtLesen(id)` | dialogfrei über `StilleDb`, für die Oberfläche |
| `KonfigurationCtrl.ExtrapolationErlaubtSchreiben(id, wert)` | eigenes, zielgenaues UPDATE — nicht in `Update()` |
| `KonfigurationCtrl.Insert` | nachgestelltes `ExtrapolationErlaubtSchreiben(id, true)` |
| `SimulationControl.Do_Simulation` | `simulation_wp.Extrapolation_Erlaubt = ctrl_konfig?.model?.Extrapolation_erlaubt ?? true` |

**Fehlende Spalte, fehlende Zeile und NULL liefern gleichermaßen `true`.** Das ist der Unterschied
zum Muster `Kaskade_Zweikanalig` (dort ist die Datenlücke „aus"): Ein „verboten" darf nur aus einem
ausdrücklich gesetzten FALSE kommen, nie aus einem nicht migrierten Bestand.

Warum `Insert` die Spalte **nicht** in seine Spaltenliste bekommt: Die Liste hängt an der
Ordinalkette von `ReadSingle`; auf einer Datenbank ohne Schemastand 7 würde ein erweitertes INSERT
das Anlegen der **gesamten** Konfiguration scheitern lassen. Ohne die nachgestellte Zeile stünde
jedes **neue** Projekt auf FALSCH (Access belegt eine angehängte YESNO-Spalte mit False) und damit
auf anderem Verhalten als der migrierte Bestand.

### 4.4 Oberfläche

`Form_Simulation_Config`, Fußzeile rechts, eine Zeile unter dem Kaskadenschalter:

```
☑ Extrapolation der WP-Kennlinie erlauben
```

Aufbau **exakt nach dem Muster der Paket-2-Flag-Checkbox** (`InitKaskadeSchalter`): programmatisch
im Code-Behind, kein Designer, keine `.resx`, Position aus dem Nachbarsteuerelement abgeleitet,
`ToolTip` über `_uebersichtTip`, Schreiben sofort bei `CheckedChanged`, Rücknahme des Hakens bei
misslungenem Schreiben, Statuszeile über `ShowStatus`. Vorbelegung aus
`AktualisiereExtrapolationSchalter()`, gerufen aus `SetControls` neben
`AktualisiereKaskadeSchalter()`.

---

## 5. Migrationsschritt 7

### 5.1 Ausgangslage: die Spalte gab es schon

`Extrapolation_erlaubt` steht seit Paket 1 in `SchemaKatalog.Schritt2_Speicher` und ist damit in
jeder gepflegten Datenbank vorhanden — **mit dem Wert `False`**, denn `ALTER TABLE … ADD COLUMN …
YESNO` belegt bestehende Zeilen in Access so (ein Ja/Nein-Feld kennt kein NULL).

Genau das ist der Grund für einen eigenen Schritt: Nicht die Spalte fehlt, sondern ihre
**Vorbelegung** widerspricht dem bisherigen Verhalten.

### 5.2 Aufbau — exakt nach Muster Schritt 6

| Bestandteil | Schritt 6 (`Kaskade_Zweikanalig`) | Schritt 7 (`Extrapolation_erlaubt`) |
|---|---|---|
| Katalogeintrag | `SchemaKatalog.Schritt6_FeatureFlag` | `SchemaKatalog.Schritt7_Extrapolation` |
| Namenskonstante | `SPALTE_KASKADE_ZWEIKANALIG` | `SPALTE_EXTRAPOLATION_ERLAUBT` |
| Nummer | `SCHRITT_6_FEATUREFLAG = 6` | `SCHRITT_7_EXTRAPOLATION = 7` |
| Registrierung | `SCHRITTE`-Array | dito |
| DDL | `SpaltenAnlegen(...)`, idempotent, Duplikat-Erkennung über `IstBereitsVorhanden` (Jet-SQLStates) | dito — auf gepflegten Datenbanken ein No-op |
| DML | — | `UPDATE Tab_Einstellungen SET Extrapolation_erlaubt = TRUE` |
| Zählwerk | — | `SchemaMigration.DatenExtrapolationVorbelegt` |
| Leseseite | namensbasiert in `KonfigurationCtrl` | dito |
| Ordinalkette `row[0..22]` | unangetastet | unangetastet |

`ZIEL_VERSION` 6 → **7**.

`Schritt7_Extrapolation` ist bewusst **nicht** in `SchemaKatalog.Alle` aufgenommen: Die Spalte steht
dort bereits über `Schritt2_Speicher`, ein zweiter Eintrag wäre die Überschneidung, die der
Kommentar an `Alle` ausschließt. Die stille Rückfallebene
(`WaermequelleClass.SchemaSicherstellen`) deckt die Spalte damit unverändert über Schritt 2 ab.

### 5.3 Einmaligkeit

Der Schritt läuft genau einmal je Datenbank (Marker 6 → 7). Ein später vom Anwender gesetztes „nein"
wird dadurch **nicht** überschrieben — nachgewiesen in 7.5. Neu angelegte Einstellungssätze belegt
`KonfigurationCtrl.Insert` selbst vor.

Die eingefrorenen Referenzbasen unter `Referenzlaeufe/` sind unberührt: Migriert wird ausschließlich
die Arbeitskopie, die die Suite vor jedem Lauf frisch aus der produktiven Datenbank zieht.

---

## 6. UI-Anbindung `Form_Simulation_Detail`

### 6.1 Der geschlossene offene Punkt

Paket 5 (N10) hält fest: „`Form_Simulation_Detail` wertet weder `Sperrgrund` noch `Fehlertext` aus —
im zweikanaligen Weg entfällt dort heute ein bisheriger Dialog stumm." Ein abgebrochener Lauf sah
danach aus wie ein Ergebnis aus Nullwerten **und ließ sich speichern**. Das ist behoben.

### 6.2 Was das Formular jetzt tut

Nach `sim.Do_Simulation(...)`:

```csharp
if (LaufAbgebrochen()) return;      //  Sperrgrund ODER Fehlertext
Endergebniss_Simulation();
FuelleUebersicht();
LaufmeldungenAnzeigen();            //  Warnungen + Hinweise, nicht-modal
```

| Kanal | Anzeige | Begründung |
|---|---|---|
| **Fehler** (`Sperrgrund`, `Fehlertext`) | MessageBox „Simulation abgebrochen", ergänzt um die Warnungen desselben Laufs; `btn_ErgebnisSpeichern.Enabled = false`; Ergebnisfelder bleiben unberührt | Ein Dialog **in der Oberfläche** ist legitim — mitten in der Kaskade war er es nie. Kein Ergebnis darf entstehen, dieselbe Regel wie headless. |
| **Warnungen + Hinweise** | `label_Laufmeldungen` in der Fußzeile: „*n* Hinweise zum Lauf (anklicken)", voller Text als Mouseover, Klick öffnet einen **sammelnden** Dialog | nicht-modal, hält niemanden auf; sammelnd statt *n* Einzelmeldungen — genau die Verbesserung aus 13.4 |

Zusätzlich bricht `Energiebedarf(...)` jetzt bei `simulation_Strombedarf.Fehlertext` ab (Stelle 6 der
Tabelle) — auch das ein Dialog, der aus der Engine in die Oberfläche gewandert ist.

### 6.3 Minimal-invasiv

`label_Laufmeldungen` entsteht **programmatisch** und richtet seine Position an `btn_Simulation` aus
(Fußzeilenstreifen, `x = btn_Simulation.Right + 16`; dort liegen zwischen `btn_Simulation` und
`btn_ErgebnisSpeichern` rund 490 px frei). Weder `Form_Simulation_Detail.Designer.cs` noch eine der
drei `.resx` (387 KB / 33 KB / 6 KB) sind angefasst — dasselbe Vorgehen wie bei `listView_SimPuffer`
(Paket 7) und `label_Erdreich` (Paket 3). Kein Layout-Umbau.

---

## 7. Verifikation

Alle Läufe headless über die Referenzlauf-Suite (`Referenzlauf.exe`), x86, gegen eine Arbeitskopie
der produktiven Datenbank. **Die produktive `Kenndaten.accdb` wurde ausschließlich gelesen**
(kein `Kenndaten.laccdb` vorhanden, Prüfung vor Beginn).

### 7.1 Regression, Flag AUS — **PASS**

Neun Projekte gegen die eingefrorene Basis `Referenzlaeufe/2026-08-14_B1-Fixes`.
Die Arbeitskopie durchläuft dabei die Migration **bis Schritt 7**.

```
Referenzlauf.exe lauf --ziel Referenzlaeufe/2026-08-15_Paket8_FlagAus_v2
                      --projekte 1007,1008,1010,1011,1017,1018,1021,1023,1024
Referenzlauf.exe vergleich Referenzlaeufe/2026-08-14_B1-Fixes
                           Referenzlaeufe/2026-08-15_Paket8_FlagAus_v2
```

| Projekt | Dateien | Werte | Status |
|---|---|---|---|
| 1007 | 29 | 324 210 | PASS |
| 1008 | 21 | 227 847 | PASS |
| 1010 | 18 | 201 540 | PASS |
| 1011 | 29 | 324 232 | PASS |
| 1017 | 20 | 245 378 | PASS |
| 1018 | 19 | 210 343 | PASS |
| 1021 | 21 | 227 840 | PASS |
| 1023 | 25 | 262 917 | PASS |
| 1024 | 26 | 271 686 | PASS |
| **gesamt** | **208** | **2 295 993** | **PASS** |

**Byte-/MD5-Gegenprobe:** 208 CSV-Dateien, **0 Abweichungen**. Nicht nur innerhalb der Toleranz —
bitgleich.

### 7.2 Flag AN gegen den HEAD-Stand — **PASS**

A/B auf **derselben** Datenbankkopie mit `Kaskade_Zweikanalig = TRUE` für alle neun Projekte:

* **HEAD** = Stand vor Paket 8, aus `git archive HEAD` in ein Wegwerf-Verzeichnis exportiert und dort
  gebaut (der Haupt-Checkout blieb unangetastet).
* **NEU** = Stand mit Paket 8.

| Projekt | Werte | Status |
|---|---|---|
| 1007 / 1008 / 1010 / 1011 | 324 210 / 227 847 / 201 540 / 324 232 | PASS |
| 1017 / 1018 / 1021 | 245 378 / 210 343 / 227 840 | PASS |
| 1023 / 1024 | 262 917 / 271 697 | PASS |
| **gesamt** | **2 296 004** | **PASS** |

**Byte-Gegenprobe `diff -r`: 0 abweichende Dateien.**

### 7.3 Dialogfreiheit — **PASS**

| | B1-Fixes (vorher) | Paket 8 |
|---|---|---|
| Abschnitt „Automatisch beantwortete Dialoge" im Lauf-Protokoll | vorhanden, **5 Einträge** | **nicht vorhanden** |
| Warnungen im Lauf-Protokoll | 5 | **0** |
| Extrapolations-Protokollzeilen | 0 (stumm) | **5** |
| Projekte mit Dialog bzw. Protokollzeile | 1007, 1008, 1011, 1023, 1024 | **1007, 1008, 1011, 1023, 1024** |

Dieselben fünf Projekte, punktgenau. Beispielzeile:

```
Simulation Hinweis: Wärmepumpe 'CS7800iLW 12': Die Quelltemperatur unterschreitet die
untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung
„Extrapolation der Kennlinie erlauben“).
```

Auch im **A/B mit Flag an** (7.2) meldete der HEAD-Stand 5 automatisch beantwortete Dialoge, der
Paket-8-Stand **0** — bei bitgleichem Ergebnis. Das ist der direkte Beleg, dass die Umstellung von
der Dialogantwort auf die Einstellung ergebnisneutral ist.

### 7.4 Fehlerkanal Ende zu Ende — **PASS**

Präparierte Kopie, `Extrapolation_erlaubt = FALSE` für Projekt 1007, Lauf im Modus `projekt`:

```
Simulation FEHLER: Wärmepumpe: Die Quelltemperatur unterschreitet die untere Stützstelle
der Kennlinie der Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S' (-15,0 °C). Die
Projekteinstellung „Extrapolation der Kennlinie erlauben“ ist abgewählt, deshalb wurde die
Simulation abgebrochen. Entweder die Kennlinie um tiefere Stützstellen ergänzen oder die
Einstellung setzen.
```

| Kriterium | Erwartung | Messung |
|---|---|---|
| Exitcode | ≠ 0 | **3** |
| Fehlertext beim Aufrufer (`out fehler`) | sprechend | ja (Wortlaut oben, vom Runner durchgereicht) |
| Ergebnis gespeichert | nein | `Tab_Ergebnis` für 1007 **unverändert** — auf einer frischen Kopie sind das **0 Zeilen** (berichtigt, Befund N14e: die produktive Datenbank führt für 1007 keine Ergebniszeile; Ergebnisse gibt es dort nur für 1010, 1011, 1019, 1023, 1024, 1025) |
| CSV-Ausgabe | keine | **0 Dateien** |
| Dialog | keiner | keiner (kein Eintrag des `DialogWaechter`) |

**UI-Anzeige des Fehlertexts — Code-Beleg** (kein Klicktest nötig):
`Form_Simulation_Detail.btn_Simulation_Click` → `LaufAbgebrochen()` liest
`sim.Sperrgrund` bzw. `sim.Fehlertext`, zeigt `MessageBox.Show(text, "Simulation abgebrochen", …)`,
setzt `btn_ErgebnisSpeichern.Enabled = false` und kehrt **vor** `Endergebniss_Simulation()` zurück.
Die Kette dorthin: `SimulationWaermepumpe.Fehlertext` → `SimulationControl.Fehlertext`
(`Simulation_WP_Ctrl` im Altpfad, `Speicherstufe_Rechnen` im zweikanaligen Weg).

### 7.5 Migrationsschritt 7 — **PASS**

| Fall | Erwartung | Messung |
|---|---|---|
| Frische Kopie einer V6-Datenbank | Migration auf 7 | `SchemaVersion vorher: 6` → `nachher: 7 (Zielstand 7)`, `Ergebnis: ERFOLG` |
| Schritt 7 ausgeführt | ja | `Schritt 7 Vorbelegung Extrapolation_erlaubt …: OK` |
| Spalte vorhanden, Default WAHR | ja | `14 Einstellungssätze auf WAHR vorbelegt` |
| Spalte außerhalb der Ordinalkette | Position ≥ 23 | `row[0..22] unveraendert` · `Extrapolation_erlaubt an Position 23 (angehaengt)` |
| Zweiter Lauf | „bereits erledigt" | `Schritt 7 …: bereits erledigt`, Version bleibt 7 |
| V7-Datenbank | keine Aktion | dito, `MigrationOk=True` |
| Anwender-FALSCH überlebt | ja | 1007 vor/nach zweitem Migrationslauf: `False`; 1008 unverändert `True` |

Der Schema-Nachweis meldet **1 Abweichung** (`Anlagen ohne Ladeprio-Vorgabe: 1`). **Gegenprobe mit
dem HEAD-Stand auf derselben Quelle: dieselbe 1 Abweichung.** Es ist eine Datenspur paralleler
Arbeit an der produktiven Datenbank (eine nach Migrationsschritt 5 angelegte Anlage trägt wieder
NULL) — genau der Fall, den `WaermesenkeClass.VorbelegungNachziehen` bei jedem Simulationsstart
abfängt. **Nicht von Paket 8 verursacht.**

### 7.6 Build

```
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    WP-Plan.sln -p:Configuration=Debug -p:Platform=x86
```

**0 Fehler, exakt 6 Warnungen** — dieselben sechs Bestandswarnungen wie vor dem Paket
(`WErzeugerModel` CS0108, `KlimaregionStammCtrl` 2× CS0109, `StromverbraucherStammCtrl` CS0108,
`MDIMainForm` CS4014 und CS1998). `Referenzlauf/Referenzlauf.csproj` baut ebenfalls fehlerfrei.

---

## 8. Geänderte und neue Dateien

**Neu**

| Datei | Inhalt |
|---|---|
| `Allgemein/Simulation/SimulationProtokoll.cs` | der Kanal (Kapitel 3) |
| `Allgemein/Simulation/Paket8_EngineProtokoll_Protokoll.md` | dieses Dokument |

**Geändert**

| Datei | Änderung |
|---|---|
| `Allgemein/DataRepository.cs` | Engine-Modus (`EngineModus()`, `StilleFehlerAbholen()`, `EngineModusAktiv`), 6 MessageBoxen → `FehlerMelden` |
| `Allgemein/Simulation/SimulationControl.cs` | `Protokoll`; `Do_Simulation` im Engine-Modus; `Extrapolation_Erlaubt` an das WP-Modul; WP- und Kessel-`Fehlertext` in beiden Rechenwegen abgeholt; Konsolen-Meldungen auf den Kanal |
| `Allgemein/Simulation/SimulationRunner.cs` | `Protokoll`; Engine-Modus um Lauf **und** Speichern; `out fehler` aus dem Kanal ergänzt; Abbruch bei `SimulationStrombedarf.Fehlertext` |
| `Allgemein/Simulation/SimulationWaermepumpe.cs` | `Extrapolation_Erlaubt`, `Fehlertext`; beide MessageBoxen entfallen (Stellen 1 und 2) |
| `Allgemein/Simulation/SimulationWaermebedarf.cs` | 3 MessageBoxen → Warnungs-Kanal; Tippfehler „DerTyp" behoben |
| `Allgemein/Simulation/SimulationStrombedarf.cs` | `Fehlertext`; 2 MessageBoxen → Fehler-Kanal |
| `Allgemein/Simulation/SimulationSPK.cs` | Parameter `mitDialog` entfallen; MAX_SPK-Meldung → Warnungs-Kanal |
| `Allgemein/Update/SchemaKatalog.cs` | `SPALTE_EXTRAPOLATION_ERLAUBT`, `Schritt7_Extrapolation` |
| `Allgemein/Update/SchemaMigration.cs` | `ZIEL_VERSION = 7`, `SCHRITT_7_EXTRAPOLATION`, `Schritt_7_ExtrapolationVorbelegung`, Zählwerk |
| `Controller/KonfigurationCtrl.cs` | namensbasiertes Lesen; `ExtrapolationErlaubtLesen/Schreiben`; Vorbelegung nach `Insert` |
| `Controller/ErgebnisCtrl.cs` | 2 MessageBoxen → `DataRepository.FehlerMelden` |
| `Controller/PufferSpCtrl.cs` | 1 MessageBox → `DataRepository.FehlerMelden` (Sicherheitsnetz) |
| `Controller/Z_ProjektPufferSpCtrl.cs` | 1 MessageBox → `DataRepository.FehlerMelden` (Sicherheitsnetz) |
| `Model/KonfigurationModel.cs` | Feld `Extrapolation_erlaubt`, Vorbelegung `true` |
| `Views/Simulation/Form_Simulation_Config.cs` | `AktualisiereExtrapolationSchalter()` in `SetControls` |
| `Views/Simulation/Form_Simulation_Config.Uebersicht.cs` | Checkbox samt Vorbelegung und Schreibweg |
| `Views/Simulation/Form_Simulation_Detail.cs` | `LaufAbgebrochen()`, `LaufmeldungenAnzeigen()`, Meldungszeile; Abbruch in `Energiebedarf` |

**Zusätzlich in der Nacharbeit geändert** (Kapitel 10)

| Datei | Änderung |
|---|---|
| `Allgemein/Bericht/BerichtsDatenSammler.cs` | N2: `SimuliereUndSpeichere` statt eigenem Save · N5: `LaufmeldungenUebernehmen` in `daten.Warnungen` |
| `Views/Varianten/Form_Variantentest.cs` | N5: Laufmeldungen in der Sammelmeldung |
| `Allgemein/Simulation/SimulationProtokoll.cs` | N6: `FehlertextFuerAnzeige` · N7: Threading-Kommentar berichtigt |
| `Allgemein/Simulation/SimulationBHKW.cs` | N9: Fehlerkanal statt blanker Konsolenzeile |
| `Controller/HeizkesselCtrl.cs`, `BHKWCtrl.cs`, `PhotovoltaikCtrl.cs`, `SolarkollektorenCtrl.cs`, `StromspeicherCtrl.cs` | N10: je 1 MessageBox → `DataRepository.FehlerMelden` |
| `Referenzlauf/Protokoll.cs` | N13b: Warnungszähler kennt die neue Wortwahl |
| `Referenzlaeufe/LIESMICH.md` | N8: Weg-B-Falle · Abschnitt „Dialoge der Engine" auf den Stand nach Paket 8 gebracht |

Die übrigen Dateien der Tabelle darüber sind in der Nacharbeit ebenfalls angefasst worden
(N1/N3/N4/N6/N7/N8/N9/N11/N12/N13); die Einzelheiten stehen in 10.1–10.3.

**Nicht angefasst:** Designer- und `.resx`-Dateien; die eingefrorenen Referenzbasen; die gesperrten
Dateien des Auftrags. Am `Referenzlauf/` wurde in der Nacharbeit **eine** Zeile geändert (N13b) —
der `DialogWaechter` bleibt unverändert als Sicherheitsnetz bestehen und hat nichts mehr zu drücken.

---

## 9. Offene Punkte und Nutzerentscheidungen

**1 · Default von `Extrapolation_erlaubt` (Konzeptabweichung, entschieden nach Auftrag)**
Umgesetzt: **WAHR**. Konzept 13.4 nennt „nein" plus Kappung auf die unterste Stützstelle.
Alternativen, falls gewünscht:
(a) Default auf FALSCH drehen — dann brechen die fünf betroffenen Referenzprojekte ab, bis der
Anwender die Einstellung setzt; reine Migrationsänderung (`Schritt_7_…` auf `FALSE`).
(b) **Kappung** als drittes Verhalten ergänzen (extrapolieren / kappen / abbrechen) — das ist eine
**Rechenänderung** mit eigener Ergebnisbewertung und gehört in ein eigenes Paket.
(c) Einstellung projektübergreifend statt je Projekt.

**2 · `Extrapolation_erlaubt` sitzt je Projekt, nicht je Wärmepumpe.**
Das folgt der Ablage in `Tab_Einstellungen` aus Konzept 13.4. Bei mehreren Wärmepumpen mit sehr
unterschiedlichen Kennlinien wäre eine Einstellung je Anlage (`Tab_Energieanlagen`) genauer. Kein
Handlungsdruck: Der Protokolleintrag nennt die betroffene Anlage namentlich.

**3 · Die Einstellung gehört laut Konzept „in den Parameterbereich der Wärmepumpe in
`Form_Simulation_Detail`".**
Sie sitzt stattdessen in der Fußzeile von `Form_Simulation_Config`, neben dem Kaskadenschalter.
Begründung: Dort steht bereits die einzige vergleichbare Projekteinstellung, das Muster ist erprobt,
und der Parameterbereich der Detailansicht ist Designer-/`.resx`-gebunden — ein Eingriff dort wäre
nicht minimal-invasiv. Verschiebbar, sobald Paket 9 den Bereich ohnehin anfasst.

**4 · Zwei Fehlerpfade verhalten sich jetzt anders (Stellen 6 und 9 der Tabelle 2.2).**
Bricht die Strombedarfsrechnung ab oder ist ein Kessel im Projekt nicht hinterlegt, wird **kein**
Ergebnis mehr gespeichert. Bisher rechnete der Lauf mit leerem Stromprofil bzw. genullter Restwärme
weiter und speicherte das Resultat. Das ist die vom Konzept geforderte Behandlung („Fehler → Lauf
abbrechen") und in keinem der neun Referenzprojekte erreichbar — dort tritt keiner der beiden Fälle
auf. **Ergebnisneutral im Erfolgsfall, absichtlich anders im Fehlerfall.**

**5 · Der `DialogWaechter` der Referenzlauf-Suite bleibt bestehen.**
Er hat nach dieser Umstellung nichts mehr zu drücken (nachgewiesen in 7.3) und ist damit reines
Sicherheitsnetz. Er sollte **nicht** entfernt werden: Er ist die Messsonde, mit der sich jede künftig
neu eingeschleppte MessageBox im Rechenpfad sofort im Lauf-Protokoll zeigt.

**6 · Der Variantenbericht liest die Hinweise noch nicht.**
`SimulationRunner.Protokoll` steht bereit (3.5) und `out fehler` trägt Fehler und Warnungen. Die
Übernahme in die Hinweisliste des Berichts und in das Berichtskapitel „Datengrundlage & Methodik"
(Konzept 13.4, letzter Punkt) gehört zum Berichtsmodul und ist hier nicht umgesetzt.

**7 · Nicht umgestellte Dialoge — bewusst.**
`Form_Simulation_Detail` und `Form_Simulation_Config` zeigen weiterhin MessageBoxen (Konfiguration
fehlt, Klimaregion fehlt, Netzverluste > 100 %, Migrationssperre, Abbruchmeldung aus 6.2). Das ist
Oberfläche, kein Engine-Kontext — dort ist der Dialog richtig.

**8 · Threadreiner Kanal — vorgemerkte Härtung (Nacharbeit, Befund N7).**
`SimulationProtokoll.Aktuell` und `DataRepository._stillTiefe` sind prozessweit und tragen über die
**Modalitäts-Invariante** aus 3.2. Die Umstellung auf `[ThreadStatic]` bzw. `AsyncLocal` ist
vorgemerkt, aber **nicht** umgesetzt: Sie trägt nur, wenn alle Lese-/Schreibpaare threadrein sind,
und heute liest `Form_Simulation_Detail` den Kanal auf dem UI-Thread nach einem synchronen Lauf,
während der Berichtspfad auf einem Worker schreibt UND liest. Auslöser für die Umsetzung wäre jede
Änderung, die eines der drei Berichtsformulare nicht-modal öffnet oder zwei Läufe nebenläufig
zulässt.

**9 · Konzept 13.4 „Standardprofil verwenden" ist NICHT umgesetzt (Nacharbeit, Befund N14d).**
Konzept 13.4 nennt für den fehlenden **Tagesverteilungstyp** (`SimulationWaermebedarf.cs:167`) als
Behandlung „Warnung + Standardprofil verwenden". Umgesetzt ist **Warnung + `return`** — also der
unveränderte Abbruch der Bedarfsrechnung an genau dieser Stelle, nur mit sichtbarer Meldung statt
MessageBox.

*Warum die Abweichung:* Ein Ersatzprofil ist eine **Rechenänderung**. Es lieferte eine
Stundenverteilung, wo bisher gar nichts gerechnet wurde, und damit andere Zahlen in jedem
betroffenen Projekt. Paket 8 ist Infrastruktur und muss byte-identisch bleiben.

*Warum die Meldung als „Warnung" und nicht als „Fehler" eingestuft ist:* Der Lauf **bricht nicht
ab**. Er rechnet weiter — mit einem Gebäude, dessen Bedarf fehlt. Das ist genau die Definition der
Warnstufe („gerechnet, aber mit einer Ersatzannahme"; die Ersatzannahme ist hier „Bedarf 0"). Ein
Fehler wäre der Abbruch, und den gibt es an dieser Stelle bewusst nicht — das wäre ebenfalls eine
Verhaltensänderung. Der Meldungstext sagt deshalb ausdrücklich, dass das Ergebnis unvollständig ist.
Der Nachweis, dass die Warnung den Anwender jetzt erreicht, steht in 10.4 (Szenario d).

*Zu entscheiden:* Ersatzprofil (Konzeptvariante), harter Abbruch, oder Beibehalten. Alle drei sind
Rechen- bzw. Verhaltensänderungen und gehören in ein eigenes Paket mit Ergebnisbewertung.
---

## 10. Nacharbeit der Review-Befunde N1–N14

**Stand 15.08.2026 · 14 Befunde, alle behandelt · ergebnisneutral nachgewiesen (10.4)**

Die Review zu Paket 8 hat zwei kritische, fünf ernste und sieben geringe Befunde gemeldet. Dieses
Kapitel hält je Befund fest, was geändert wurde und warum. Aussagen der Kapitel 1–9, die dabei
berichtigt wurden, sind dort als Blockzitat vermerkt (Befunde N7 und N14a–N14e).

### 10.1 Kritische Befunde

#### N1 — „Ergebnis speichern": Zustandsmaschine statt Einbahnstraße

**Fund.** Zwei entgegengesetzte Schäden in derselben Schaltfläche:

* Der neue Strombedarf-Abbruch in `btn_Simulation_Click` kehrte **vor** `LaufAbgebrochen()` zurück.
  `btn_ErgebnisSpeichern` blieb aktiv, `SpeichereErgebnis()` prüfte nichts — und
  `ErgebnisCtrl.Save` **löscht zuerst**. Ein Klick hätte das gültige Bestandsergebnis des Projekts
  durch einen Nullsatz ersetzt. Dasselbe galt direkt nach dem Öffnen des Formulars: Die
  Schaltfläche ist im Designer aktiviert, die Simulationsobjekte sind leer.
* Umgekehrt setzte `LaufAbgebrochen()` `Enabled = false`, und **nirgends im Repo** stand je wieder
  ein `true`. Ein korrigierter Erfolgslauf im selben Fenster ließ sich nicht mehr speichern.

**Fix** (`Views/Simulation/Form_Simulation_Detail.cs`) — eine Zustandsmaschine mit einem Merkmal:

| Stelle | Wirkung |
|---|---|
| Feld `_ergebnisGueltig` (Vorbelegung `false`) | trägt den Zustand zusätzlich zu `Enabled`, weil die Schaltfläche im Designer aktiviert ist |
| `ErgebnisUngueltig()` / `ErgebnisGueltig()` | die beiden einzigen Übergänge — Merkmal und `Enabled` können nicht auseinanderlaufen |
| `btn_Simulation_Click`, erste Zeile | `ErgebnisUngueltig()` — **vor** jeder Prüfung, damit jeder Frühausstieg (Migrationssperre, fehlende Konfiguration, Netzverluste, Klimaregion, Strombedarf-Abbruch, Kaskaden-Abbruch) gesperrt zurücklässt |
| nach `FuelleUebersicht()` | `ErgebnisGueltig()` — der einzige Weg zurück |
| `SimulationBlockiert()`, `LaufAbgebrochen()` | benutzen `ErgebnisUngueltig()` statt eigener Zuweisung |
| `SpeichereErgebnis()`, Kopf | Frühausstieg mit Meldung, wenn `!_ergebnisGueltig` — **der eigentliche Schutz**, denn der gesperrte Knopf allein trägt nicht |

Nachweis: 10.4, Szenario (a) — mit Beleg, dass `Tab_Ergebnis` den Speicherversuch nach einem
Abbruch unverändert übersteht.

#### N2 — Berichtspfad: Speichern ohne Engine-Modus auf dem ThreadPool-Thread

**Fund.** `BerichtsDatenSammler.SammleProjekt` rief nach `runner.Simuliere(...)` selbst
`SimulationRunner.BaueErgebnis` und `ergCtrl.Save(...)` — beides **außerhalb** des dialogfreien
Modus. Dieser Pfad läuft in `Task.Run` (`Form_Bericht.cs:407`, `Form_Wirtschaftlichkeit.cs:422`,
`Form_WirtschaftlichkeitVerlauf.cs:219`): Ein Datenbankfehler hätte eine MessageBox auf dem
Worker-Thread geöffnet, und der Fortschrittsbalken wäre eingefroren.

**Fix** (`Allgemein/Bericht/BerichtsDatenSammler.cs`) — der bevorzugte Weg: Umstellung auf
`runner.SimuliereUndSpeichere(...)`. Der klammert Ergebnisaufbau und Speichern korrekt (mit N4) und
liefert das Protokoll gleich mit.

Damit das **Verhalten unverändert** bleibt, hat `SimulationRunner` eine neue, schreibgeschützte
Eigenschaft `LaufOk` bekommen: `SimuliereUndSpeichere` liefert `-1` sowohl für „nicht gerechnet" als
auch für „nicht gespeichert", der Sammler braucht die Unterscheidung aber — aus einem gerechneten,
nur nicht gespeicherten Lauf lassen sich die Stundenreihen für die Ganglinien weiterhin abholen.
Genau so verhielt sich der bisherige Code.

**Nicht geändert:** Die Semantik von `out fehler` (nur im Misserfolgsfall belegt). Das ist Absicht;
die Meldungen eines erfolgreichen Laufs stehen im Protokoll (N5) und sind in Kapitel 3.5
klargestellt.

### 10.2 Ernste Befunde

| Befund | Datei · Stelle | Fix |
|---|---|---|
| **N3** — `extrapolation` nie zurückgesetzt | `Allgemein/Simulation/SimulationWaermepumpe.cs`, `ModuleAufbauen()` | `extrapolation = false;` in den Rücksetzblock neben `Fehlertext = "";`. Ab dem zweiten Lauf derselben Instanz — im MDI-Fenster der Normalfall — war `Extrapolation_Erlaubt` sonst wirkungslos: kein Abbruch bei Verbot, kein Hinweis bei Erlaubnis. **Ergebnisneutral:** Das Merkmal steuert ausschließlich Meldung und Verbotsprüfung; die lineare Verlängerung der Kennlinie läuft in jedem Fall — der Zweig hinter `if (!extrapolation)` endet vor der Rechnung. Nachweis 10.4 (b) |
| **N4** — `BaueErgebnis` außerhalb des Engine-Modus | `Allgemein/Simulation/SimulationRunner.cs`, `SimuliereUndSpeichere` | Das `using (DataRepository.EngineModus())` umschließt jetzt auch `BaueErgebnis`. Der beginnt mit `ProjektCtrl.ReadSingle` und liest danach Anlagen- und Speicherzeilen — jeder Datenbankfehler darin hätte im headless-Lauf einen Dialog geöffnet. Im Berichtspfad durch N2 miterledigt |
| **N5** — Warnungen verschwinden im erfolgreichen headless-Lauf | `Views/Varianten/Form_Variantentest.cs`, `Allgemein/Bericht/BerichtsDatenSammler.cs` | `Form_Variantentest.btnSimulieren_Click` hält jetzt die Runner-Instanz und hängt `runner.Protokoll.HinweistextFuerAnzeige()` je Projekt eingerückt an die Sammelmeldung. `BerichtsDatenSammler` übernimmt Warnungen **und** Hinweise jedes Laufs in `daten.Warnungen`, mit Projektbezug (`LaufmeldungenUebernehmen`) — bei einem Variantenbericht laufen bis zu einem Dutzend Simulationen, eine Meldung ohne Zuordnung wäre wertlos |
| **N6** — Strombedarf-Abbruch: Diagnose erreichte die UI nicht | `SimulationStrombedarf.cs`, `SimulationProtokoll.cs`, `Form_Simulation_Detail.cs` | Drei Teile: (1) Der Sammel-`catch` nennt jetzt das **betroffene Stromprofil** (mitgeführt in `aktuellesProfil`) — die häufigste Ursache ist eine `InvalidCastException` aus `(double)rs.Read(...)` bei einem leeren Monats- oder Wochenfeld. (2) Neu: `SimulationProtokoll.FehlertextFuerAnzeige(ausserdem)` — die Fehlerliste als Fließtext, ohne Doppelung zum bereits angezeigten Abbruchgrund. (3) `Energiebedarf(...)` und `LaufAbgebrochen()` zeigen diese Fehlerzeilen im Abbruchdialog; der Frühausstieg aus `Energiebedarf` zeigt zusätzlich die Fußzeilenmeldungen |
| **N7** — Threading-Prämisse falsch | `SimulationProtokoll.cs`, `DataRepository.cs`, Kapitel 3.2 | Kommentare berichtigt, die **Modalitäts-Invariante** in beiden Klassen und im Protokoll benannt; `[ThreadStatic]`/`AsyncLocal` als vorgemerkte Härtung dokumentiert (Kapitel 9, Punkt 8) |

### 10.3 Geringe Befunde

| Befund | Fix |
|---|---|
| **N8** — Referenzlauf-„Weg B"-Falle | `Controller/KonfigurationCtrl.cs`: Der namensbasierte Leser behandelt den **nie vorbelegten** Zustand. Solange `Tab_Applikation.SchemaVersion < 7` steht, gilt ein gespeichertes `False` als Datenlücke (Access-Vorbelegung einer angehängten YESNO-Spalte) und nicht als Anwenderwille — gelesen wird „erlaubt". Ab Schemastand 7 zählt der gespeicherte Wert. **Warum der Leser und nicht die Rückfallebene:** `WaermequelleClass.SchemaSicherstellen` läuft erst in `Do_Simulation`, also **nach** dem Lesen der Konfiguration im `SimulationRunner` — ein dort nachgezogenes `UPDATE` hätte den laufenden Lauf nicht mehr erreicht. Der Leser wirkt sofort und schreibt nichts in eine fremde Datenbank. Der erreichte Schemastand wird gemerkt: auf gepflegten Datenbanken genau ein Marker-Lesevorgang je Programmlauf. Dokumentiert in `Referenzlaeufe/LIESMICH.md` (Weg B, Schritt 1). Nachweis 10.4 (c), beidseitig |
| **N9** — Fehlerstellen ohne Kanaleintrag | `SimulationBHKW.cs` (> `MAX_BHKW`) und `SimulationControl.cs` 2× (Pendelspeicher ohne Volumen bzw. ohne Puffer-Zeile): `Console.WriteLine(Fehlertext)` → `SimulationProtokoll.Fehlermeldung(Fehlertext)`. Die Konsolenzeile bleibt (sie steckt im Kanal), die Meldung steht jetzt zusätzlich im Lauf-Protokoll und in der Sammelanzeige |
| **N10** — Geschwister-Dialoge einheitlich | `HeizkesselCtrl.cs:156`, `BHKWCtrl.cs:226`, `PhotovoltaikCtrl.cs:211`, `SolarkollektorenCtrl.cs:191`, `StromspeicherCtrl.cs:204` auf `DataRepository.FehlerMelden` umgestellt — wie `PufferSpCtrl.cs:193` in Paket 8. `HeizkesselCtrl` und `BHKWCtrl` werden von Engine-Modulen benutzt, die übrigen drei folgen der Konsistenz. **`BHKWCtrl.cs` ist Windows-1252-kodiert** und wurde deshalb byteweise mit dieser Kodierung bearbeitet (Gegenprobe: die Umlaute der Bestandszeilen 14/15 unverändert) |
| **N11** — `Fehlertext`-Überschreiben | `SimulationControl.cs`: neue Hilfsmethode `FehlertextAufnehmen(text)` — sammelt statt zu überschreiben (Anhängen mit Zeilenumbruch, Doppelungen weggelassen). Verwendet an allen vier Stellen: zweikanaliger WP-Aufbau, Stundenschleife, WP-Altpfad, Kessel-Altpfad. Vorher überschrieben zwei davon bedingungslos — im Altpfad läuft nach der Wärmepumpe noch der Kessel, dessen Meldung verdrängte die der WP. **Ergebnisneutral:** `Fehlertext` ist reiner Meldetext; ob er belegt ist, entscheidet über den Abbruch des Speicherns, und belegt ist er in genau denselben Fällen wie zuvor |
| **N12a** — Meldungs-Kappung stumm | `DataRepository.FehlerMelden`: beim Überlauf über `MAX_STILLE_FEHLER` **einmalig** „… weitere Meldungen unterdrückt (Grenze 50 je Abholung)". Eine abgeschnittene Liste las sich sonst wie eine vollständige |
| **N12b** — Warnungen doppelt | `Form_Simulation_Detail.LaufAbgebrochen()`: Der Dialog zeigt jetzt **Abbruchgrund + Fehler** (N6), die **Warnungen nur noch in der Fußzeile**. Vorher standen sie in beidem |
| **N13a** — Position der neuen Checkbox | `Form_Simulation_Config.Uebersicht.cs`: neue Methode `ExtrapolationSchalterPlatzieren()`, aufgerufen **nach** `Controls.Add` (ein AutoSize-Steuerelement kennt seine Höhe erst dann). Zwei Korrekturen: **Null-Schutz** für `checkBox_KaskadeZweikanalig` mit demselben Rückfall wie `InitKaskadeSchalter`, und eine **kollisionsfreie** Rechnung. Die Kollision war real: Verwalten-Knopf bis y ≈ 476, Speichern/OK ab y ≈ 490 — der Schalter saß bei y 476…493 und lag damit auf der Oberkante von `btn_Speichern`. Statt einer festen Zahl wird der Bedarf gerechnet und das Formular bei Bedarf um genau die fehlenden Pixel höher, mit nachgezogener Knopfzeile — dasselbe Vorgehen wie in `InitPufferspeicherRubrik` (die drei Elemente sind ohne Verankerung, Bestand). Punkt für den UI-Sichttest: 10.5 |
| **N13b** — Zähler der Suite | `Referenzlauf/Protokoll.cs`, `AusKindprozess`: zählt jetzt `WARNUNG:` **und** `Simulation Warnung:`. Hinweise werden bewusst nicht gezählt — sie melden einen vollwertig gerechneten Grenzfall, den es in jedem bisherigen Referenzlauf gab. Fehler traf der bisherige Vergleich schon (`Simulation FEHLER:` enthält `FEHLER:`). Eingefrorene Basen bleiben unberührt: Gezählt wird beim Lesen der Kindprozessausgabe, nicht beim Vergleich |
| **N13c** — Zeilenenden | `Form_Simulation_Config.cs` und `Form_Simulation_Config.Uebersicht.cs` lagen vollständig als LF vor und sind auf CRLF normalisiert. Inhalt unverändert (Zeichenzahl ohne CR vorher = nachher; `git diff` weist für `Form_Simulation_Config.cs` weiterhin genau die drei Paket-8-Zeilen aus). BOM je Datei erhalten: `Form_Simulation_Config.cs` mit, `…Uebersicht.cs` ohne |
| **N14** — Doku | (a) Kapitel 2.2 Stelle 17 · (b) Kapitel 3.6 Kappung · (c) `DataRepository`-Kopfkommentar · (d) Kapitel 9 Punkt 9 · (e) Kapitel 7.4 Zeilenzahl — alle an Ort und Stelle berichtigt, jeweils als Blockzitat mit Befundnummer |

### 10.4 Verifikation der Nacharbeit

Alle Läufe x86, headless, ausschließlich auf **Kopien**. Die produktive `Kenndaten.accdb` wurde nur
gelesen (keine `Kenndaten.laccdb` vor Beginn, keine danach; der einzige Direktzugriff lief mit
`Mode=Read`).

#### 1 · Regression Flag AUS — **PASS**

Neun Projekte gegen die eingefrorene Basis `Referenzlaeufe/2026-08-14_B1-Fixes`, Arbeitskopie über
`lauf` (mit Migration bis Schritt 7).

```
Referenzlauf.exe lauf --ziel Referenzlaeufe/2026-08-15_Paket8_Nacharbeit_FlagAus
                      --projekte 1007,1008,1010,1011,1017,1018,1021,1023,1024
Referenzlauf.exe vergleich Referenzlaeufe/2026-08-14_B1-Fixes
                           Referenzlaeufe/2026-08-15_Paket8_Nacharbeit_FlagAus
```

| Projekt | Werte | Status |
|---|---|---|
| 1007 / 1008 / 1010 | 324 210 / 227 847 / 201 540 | PASS |
| 1011 / 1017 / 1018 | 324 232 / 245 378 / 210 343 | PASS |
| 1021 / 1023 / 1024 | 227 840 / 262 917 / 271 686 | PASS |
| **gesamt** | **2 295 993** | **9/9 PASS** |

**Byte-/MD5-Gegenprobe: 208 CSV-Dateien, 0 Abweichungen.**

#### 2 · Flag AN, A/B gegen den Stand **vor Paket 8** — **PASS**

Auf **einer** Datenbankkopie außerhalb des Repos (`migration`-Modus, Schemastand 7), für alle neun
Projekte `Kaskade_Zweikanalig = TRUE` gesetzt, dann jedes Projekt zweimal im Modus `projekt`
gerechnet: einmal mit dem Nacharbeitsstand, einmal mit dem aus `git archive HEAD` exportierten und
dort gebauten Stand vor Paket 8.

| Projekt | Werte | Status |
|---|---|---|
| 1007 / 1008 / 1010 / 1011 | 324 210 / 227 847 / 201 540 / 324 232 | PASS |
| 1017 / 1018 / 1021 | 245 378 / 210 343 / 227 840 | PASS |
| 1023 / 1024 | 262 917 / 271 697 | PASS |
| **gesamt** | **2 296 004** | **9/9 PASS** |

**Byte-/MD5-Gegenprobe: 208 CSV-Dateien, 0 Abweichungen.**

Das ist der schärfere Nachweis als „gegen den Vor-Nacharbeits-Stand": Kapitel 7.2 hat bereits
gezeigt, dass Paket 8 mit gesetztem Flag bitgleich zum HEAD-Stand rechnet. Rechnet die Nacharbeit
ebenfalls bitgleich zum HEAD-Stand, ist sie es auch zum Vor-Nacharbeits-Stand — und diese Kette ist
jederzeit reproduzierbar, weil HEAD im Git steht.

Nebenbefund: Der Nacharbeitsstand meldete in diesen Läufen **0 Dialogzeilen**, der HEAD-Stand
brauchte den Dialogwächter — bei bitgleichem Ergebnis (dieselbe Aussage wie 7.3).

#### 3 · E2E-Szenarien — **4/4 PASS**

Getrieben über einen Wegwerf-Testtreiber (eigenes Konsolenprojekt außerhalb des Repos, mit
`ProjectReference` auf die Anwendung; DB-Pfad per Reflection auf die Kopie umgebogen und
nachgeprüft — Muster `DbUmgebung`). Der Treiber ist Verifikationswerkzeug und **kein** Bestandteil
des Repos.

**(a) Abbruchlauf → Erfolgslauf im selben Fenster** (Befund N1). `Form_Simulation_Detail` wird
instanziiert und `btn_Simulation_Click` zweimal aufgerufen — erst mit `Extrapolation_erlaubt =
FALSCH` (Abbruch), dann mit WAHR (Erfolg). Ein Dialogwächter bestätigt die Meldungen.

```
Startzustand     : _ergebnisGueltig=False  btn.Enabled=True
nach Abbruchlauf : _ergebnisGueltig=False  btn.Enabled=False
Tab_Ergebnis vor  Speicherversuch : ID=174 / 15.08.2026 10:09:52
Tab_Ergebnis nach Speicherversuch : ID=174 / 15.08.2026 10:09:52     <- unberuehrt
nach Erfolgslauf : _ergebnisGueltig=True   btn.Enabled=True
[Dialog] 'Simulation abgebrochen' / Die Quelltemperatur unterschreitet die untere Stützstelle …
[Dialog] 'Ergebnis speichern'    / Es liegt kein vollständiges Simulationsergebnis vor. …
```

Damit sind beide Hälften von N1 belegt: Der Speicherversuch nach dem Abbruch lässt das
Bestandsergebnis in Ruhe (vorher hätte er es durch einen Nullsatz ersetzt), und der korrigierte
Erfolgslauf gibt die Schaltfläche im **selben** Fenster wieder frei (vorher blieb sie für immer
gesperrt). Der Startzustand zeigt zugleich, warum der Frühausstieg in `SpeichereErgebnis()` nötig
ist: `Enabled` steht aus dem Designer auf `True`.

**(b) `Extrapolation_erlaubt = FALSCH` wirkt auch im zweiten Lauf** (Befund N3). Zwei Aufrufe von
`Simuliere` auf **demselben** `SimulationRunner` — und damit derselben `SimulationControl` und
demselben WP-Modul:

```
Extrapolation_erlaubt laut DB: False
Lauf 1: ok=False   Simulation FEHLER: Wärmepumpe: … Kennlinie … (-15,0 °C) … abgebrochen
Lauf 2: ok=False   Simulation FEHLER: … (identisch)
ERGEBNIS: Abbrueche = 2 von 2 -> PASS
```

**Gegenprobe** (ohne den alten Stand neu bauen zu müssen): Lauf 1 mit **erlaubter** Extrapolation
setzt das interne Merkmal, Lauf 2 auf derselben Instanz mit **verbotener**:

```
Lauf 1 (erlaubt) : ok=True    Hinweise=1   (… Es wird extrapoliert …)
Lauf 2 (verboten): ok=False   (… abgebrochen …)
ERGEBNIS: PASS
```

Genau dieser zweite Lauf lief vor der Nacharbeit durch: Das stehengebliebene `extrapolation = true`
übersprang den Block mit der Verbotsprüfung vollständig.

**(c) Weg B — unmigrierte Kopie** (Befund N8), beidseitig geprüft, `Referenzlauf projekt 1007`:

| Kopie | `SchemaVersion` | `Extrapolation_erlaubt` | Erwartung | Messung |
|---|---|---|---|---|
| nicht migriert | 6 | FALSE (Access-Vorbelegung) | Lauf geht durch | `Simulation Hinweis: … Es wird extrapoliert`, 29 CSV-Dateien, **Exitcode 0** |
| migriert | 7 | FALSE (Anwenderwille) | Lauf bricht ab | `Simulation FEHLER: … abgewählt … abgebrochen`, **Exitcode 3** |

Beide auf derselben Datenlage. Vor der Nacharbeit brach **beides** ab. Die Gegenprobe belegt
zugleich, dass 7.4 unverändert gilt: Ein ausdrücklich gesetztes „nein" schlägt weiterhin durch.

**(d) Warnfall headless** (Befund N5). Auf einer Kopie, in der die Tagesverteilungstypen nicht mehr
auffindbar sind (`Tab_DBTagV.Bezeichner` umbenannt), Projekt 1010:

```
ok=True   out fehler=(leer)
Warnungen im Kanal: 1
   WARNUNG  Wärmebedarf: Zum Tagesverteilungstyp „Wohngebaeude  VDI 2067“ sind keine Daten
            hinterlegt. Die Bedarfsrechnung wurde an dieser Stelle abgebrochen; das Ergebnis
            ist unvollständig.
--- HinweistextFuerAnzeige() (genau das zeigt Form_Variantentest) ---
• Wärmebedarf: Zum Tagesverteilungstyp „Wohngebaeude  VDI 2067“ sind keine Daten hinterlegt. …
ERGEBNIS: PASS
```

`out fehler` bleibt leer — die Semantik ist unverändert. Die Warnung erreicht den Anwender jetzt
über die Sammelmeldung von `Form_Variantentest` (und über `daten.Warnungen` im Berichtspfad), wo
vorher nur „ok" stand.

#### 4 · Build

```
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    WP-Plan.sln -p:Configuration=Debug -p:Platform=x86
```

**0 Fehler, exakt 6 Warnungen** — dieselben sechs Bestandswarnungen wie vor der Nacharbeit
(`WErzeugerModel` CS0108, `KlimaregionStammCtrl` 2× CS0109, `StromverbraucherStammCtrl` CS0108,
`MDIMainForm` CS4014 und CS1998). `Referenzlauf/Referenzlauf.csproj` baut fehlerfrei.

### 10.5 Offene Punkte der Nacharbeit

**1 · UI-Sichttest steht aus.** Zwei Punkte sind nur rechnerisch bzw. headless belegt und gehören
einmal am laufenden Programm angesehen:

* **`Form_Simulation_Config`, Fußzeile** (N13a): Sitzt der Schalter „Extrapolation der WP-Kennlinie
  erlauben" eine Zeile unter dem Kaskadenschalter, ohne `btn_Speichern`, `btn_OK` oder `lblStatus`
  zu berühren? Die Höhenkorrektur greift nur, wenn sie gebraucht wird — auf dem gemessenen Stand
  fehlten 8 px. Auch mit verbreitertem Formular prüfen (die Übersicht wächst bis 1169 px
  Clientbreite).
* **`Form_Simulation_Detail`, Fußzeile**: Meldungszeile „*n* Hinweise zum Lauf (anklicken)" neben
  `btn_Simulation`, Mouseover und Klickdialog; und der Abbruchdialog, der seit N12b **keine**
  Warnungen mehr doppelt zeigt.

**2 · Threadreiner Kanal** — siehe Kapitel 9, Punkt 8.

**3 · Konzept 13.4 „Standardprofil verwenden"** — siehe Kapitel 9, Punkt 9.

**4 · Die Referenzbasis bleibt `2026-08-14_B1-Fixes`.** Der Lauf
`2026-08-15_Paket8_Nacharbeit_FlagAus` ist ein Prüflauf, kein neuer Basisstand — er ist bitgleich
zur Basis, es gibt nichts neu zu setzen.

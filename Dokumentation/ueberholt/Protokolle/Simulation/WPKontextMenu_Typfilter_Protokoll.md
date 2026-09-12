# Stiller Verlust aller übrigen Gewerke beim Bearbeiten einer Wärmepumpe

Stand 17.08.2026, Codebasis `41e7bfd`. Nicht committet.

Das Kontextmenü der Wärmepumpen-Liste im Projektbaum löschte beim Speichern **alle**
Anlagenzeilen des Projekts und schrieb nur die Wärmepumpen zurück. PV, Kessel, BHKW,
Solarkollektoren, Pufferspeicher und Stromspeicher waren damit nach jedem
„Bearbeiten → OK" weg — ohne Meldung, ohne Rückfrage und ohne einen zweiten Pfad, der sie
wieder anlegt.

---

## 1. Befund

`WindowsFormsApplication1\Controller\WPKontextMenuCtrl.cs:147-230`, Handler
`ContextMenuItemBearbeiten_Click`.

Die Liste, die zurückgeschrieben wird, ist von vornherein **auf zwei Typen gefiltert**:

| Zeile | Anweisung | Wirkung |
|---|---|---|
| 160 | `werzctrl.ReadAllFilter("ID_Projekt=… and (ID_Type=1 Or ID_Type=7)")` | liest **nur** WP-Zeilen |
| 163-182 | `for … list_alle.Add(werzctrl.items[i])` | `list_alle` führt **nur** WP-Zeilen |
| 216 (vorher) | `wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt)` | löscht **alle** Anlagenzeilen des Projekts |
| 217 (vorher) | `wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, list_alle)` | schreibt **nur** die WP-Zeilen zurück |

Die einargumentige Überladung (`WizardCtrl.cs:28-34`) setzt
`DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ?` ab — ohne Typfilter. Der Rundumschlag
ist an dieser Stelle also nicht die Absicht, sondern die falsche Überladung: `list_alle`
heißt „alle **dieses Menüs**", nicht „alle Anlagen des Projekts".

Betroffen war ausschließlich der Bearbeiten-Zweig. `ContextMenuItemNeu_Click` (Zeile 232 ff.)
ruft `Add_WP_Waermeerzeuger` ohne vorheriges Löschen, `ContextMenuItemLoeschen_Click`
(Zeile 127 ff.) geht über `Del_Projekt_ID_Waermeerzeuger` und trifft genau eine Zeile.

## 2. Warum kein anderer Pfad die Zeilen wieder anlegt

* **`Program.mainfrm.SetWPControl` (Zeile 228)** ist reiner Lese-/UI-Refresh:
  `FormMain.cs:241-270` öffnet ein `RecordSet` auf `Tab_Energieanlagen`, leert
  `listView_WP` und füllt es neu. Kein `INSERT`, kein `UPDATE` — und ohnehin nur über
  `ID_Type IN (1,7)`, also blind für die gelöschten Gewerke.
* **`ProjektCtrl.Update` (Zeile 226)** schreibt das Änderungsdatum des Projektkopfs.
* Das `DELETE` läuft **ohne Transaktion**; ein Fehlschlag des nachfolgenden `INSERT` wäre
  nicht rückabwickelbar. Eine Sicherung der Fremdtyp-Zeilen gibt es an keiner Stelle.

Der Verlust ist damit endgültig, sobald der Dialog mit OK geschlossen wurde.

## 3. Der Fix

`WPKontextMenuCtrl.cs:216-221` — der ungefilterte Aufruf wird durch zwei typgefilterte
ersetzt (`WizardCtrl.cs:36-42`, `DELETE … WHERE ID_Projekt = ? AND ID_Type = ?`):

```csharp
// Nur die beiden WP-Typen loeschen (Plan- und Referenzliste dieses Menues):
// list_alle fuehrt ausschliesslich WP-Zeilen, ein Loeschen ohne Typfilter
// wuerde alle uebrigen Gewerke des Projekts unwiederbringlich mitnehmen.
wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, WizardItemClass.WP_TYP);
wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, WizardItemClass.REF_WP_TYP);
wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, list_alle);
```

Zwei Aufrufe, weil dieses Kontextmenü **zwei** Listen bedient (Plan- und Referenzliste) und
`Del_Projekt_Waermeerzeuger` je Aufruf genau einen Typ nimmt. Das ist exakt das
**Geschwister-Muster** aller übrigen Kontextmenüs, die je nur einen Typ führen und deshalb
mit einem Aufruf auskommen:

`BHKWKontextMenuCtrl.cs:134` · `HeizkesselKontextMenuCtrl.cs:140` ·
`PufferSpKontextMenuCtrl.cs:139` · `PVKontextMenuCtrl.cs:130` ·
`SolarKontextMenuCtrl.cs:134` · `StromspeicherKontextMenuCtrl.cs:222` und `:265`

Der Bearbeiten-Zweig des Wärmepumpen-Menüs war der einzige Ausreißer.

## 4. Wechselwirkung mit AP9b (Rettung der Speicher-Variantenparameter)

`Del_Projekt_Waermeerzeuger` ruft in **beiden** Überladungen `SpVariantenSichern`. Die
typgefilterte Fassung übergibt den Typ, und `SpVariantenSichern` (`WizardCtrl.cs:446-453`)
kehrt für jeden Typ außer `SP_TYP`/`REF_SP_TYP` sofort um:

```csharp
if (nType != TYP_ALLE &&
    nType != WizardItemClass.SP_TYP && nType != WizardItemClass.REF_SP_TYP) return;
```

`Del(id, 1)` und `Del(id, 7)` fassen die Rettungsmechanik also **nicht** an — kein
Sicherungsstand, kein Wiederherstellen, `m_SpVariantenSicherung` bleibt `null`. Das ist im
Trockenlauf auch sichtbar: Szenario A (ungefiltert, `TYP_ALLE`) protokolliert die
AP9b-Zeilen „Speichervarianten-Rettung: … kommt im Projekt … mehrfach vor", Szenario B gibt
sie nicht aus.

Umgekehrt zeigt genau das den zweiten Teil des Schadens: Im ungefilterten Weg griff die
Rettung zwar, lief aber ins Leere, weil die zurückgeschriebene Liste keine Speicheranlage
enthält, an die die Parameter zurückkonnten. Gemessen an den drei Testprojekten:
`Tab_StromspeicherVariante` **10 → 2 Zeilen** in Szenario A (Löschweitergabe
`FK_SpVariante_Anlage`), **10 → 10** in Szenario B.

## 5. Verifikation

**Build.** `MSBuild WindowsFormsApplication1.csproj -restore -t:Rebuild
-p:Configuration=Debug -p:Platform=x86` → **0 Fehler, 6 Warnungen**. Es sind ausschließlich
die Bestandswarnungen: `WErzeugerModel` CS0108, `StromverbraucherStammCtrl` CS0108,
`KlimaregionStammCtrl` 2× CS0109, `MDIMainForm` CS4014 und CS1998. **Keine neue Warnung.**

**Trockenlauf.** Wegwerf-Konsolenhost mit `ProjectReference` auf die App
(Projekteigenschaften 1:1 aus `Referenzlauf\Referenzlauf.csproj`), Zugriff auf die internen
`WErzeugerCtrl`/`WizardCtrl` per Reflection, alle App-Aufrufe in
`DataRepository.EngineModus()`. Snapshots über eine **eigene** `OleDbConnection`
(`SELECT * FROM Tab_Energieanlagen`, alle 57 Spalten, keyed nach `ID`), nicht über App-Code.
Nachgebildet wird „OK ohne Änderungen": Liste lesen, löschen, unverändert zurückschreiben.

Die produktive `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` (89 MB) wurde **einmal lesend**
kopiert; die zweite Arbeitskopie entstand aus der ersten. Vor jedem Lauf die doppelte
Schutzprüfung aus `Referenzlauf\DbUmgebung.cs` (`GetDBPath()` muss in den Arbeitskopie-Ordner
zeigen und darf keine bekannte Produktiv-Ablage sein). Ausgangsstand: **80 Zeilen** in
`Tab_Energieanlagen`, 13 Projekte mit WP- **und** Fremdtyp-Zeilen. Testprojekte: **1023,
1011, 1007**. Kein Projekt der Datenbank führt Typ-7-Zeilen (Referenzliste durchgängig leer).

**Szenario A — alte Sequenz, Messbeleg des Befunds.** `ReadAllFilter(ID_Type IN (1,7))` →
`Del_Projekt_Waermeerzeuger(id)` → `Add_WP_Waermeerzeuger(id, liste)`:

| Projekt | Typ 1 WP | Typ 2 Solar | Typ 3 PV | Typ 4 Stromsp. | Typ 10 Kessel | Typ 12 Puffer | Summe |
|---|---|---|---|---|---|---|---|
| 1023 | 2 → 2 | – | – | – | 1 → **0** | 4 → **0** | 7 → **2** |
| 1011 | 3 → 3 | 2 → **0** | 2 → **0** | 4 → **0** | 2 → **0** | 1 → **0** | 14 → **3** |
| 1007 | 1 → 1 | – | 2 → **0** | 4 → **0** | 1 → **0** | 3 → **0** | 11 → **1** |

Nicht-WP-Zeilen der drei Projekte: **26 → 0**. Über die ganze Datenbank
`Tab_Energieanlagen` **80 → 54**, `Tab_StromspeicherVariante` **10 → 2**. Der Befund ist
damit gemessen, nicht nur gelesen.

**Szenario B — neue Sequenz, Fix-Validierung.** `Del(id,1)` → `Del(id,7)` →
`Add(id, liste)`, alle drei Rückgaben `true`:

| Assertion | 1023 | 1011 | 1007 |
|---|---|---|---|
| (a) Nicht-WP-Zeilen: gleiche `ID`, alle Spalten identisch | 5 Zeilen / **285 Feldwerte** / 0 Abw. | 11 / **627** / 0 | 10 / **570** / 0 |
| (b) WP-Zeilen: Anzahl gleich, Zuordnung `Bezeichner\|ID_Type`, alle Spalten außer `ID` | 2 / **112** / 0 | 3 / **168** / 0 | 1 / **56** / 0 |
| (c) Zeilen aller anderen Projekte: Anzahl und `ID` unverändert | 73 | 66 | 69 |
| (d) `Tab_StromspeicherVariante` unverändert | 10 Zeilen / 210 Feldwerte | 10 / 210 | 10 / 210 |

Aus (a) und (b) zusammen **1 818 Feldwerte** DBNull-sicher verglichen, aus (d) weitere
**630** — **0 Abweichungen**. Kein `ID_WP`
wich ab — `CopyFromStamm` findet die vorhandene Projektkopie wieder. Die `ID` der WP-Zeilen
ändert sich erwartungsgemäß bei allen 6 Zeilen (AutoWert; Löschen + Neuanlegen ist der
Speicherweg **aller** Erzeuger, siehe `Roundtrip_Erzeuger_Protokoll.md`). Summen je Projekt
vorher = nachher: 7 → 7, 14 → 14, 11 → 11; Datenbank gesamt **80 → 80**,
`Tab_StromspeicherVariante` **10 → 10**. Exitcode 0.

Anders als erwartet ist `Tab_StromspeicherVariante` in der produktiven Datenbank
**vorhanden** (10 Zeilen) — Assertion (d) ist also wirklich geprüft und kein Leerlauf.

## 6. Restrisiken

* **`ID_Type`-Überschreibung in Zeile 176.** `werzctrl.items[i].ID_Type =
  werzctrl.items[0].ID_Type;` weist jeder gefundenen Zeile den Typ der **ersten** gelesenen
  Zeile zu, unabhängig von `i`. Führt ein Projekt Plan- **und** Referenz-Wärmepumpen, kippt
  eine Referenz-WP damit auf Typ 1 (oder umgekehrt). Das ist ein **separater Altbefund** und
  **nicht Teil dieses Fixes**; er blieb im Trockenlauf ohne Wirkung, weil keine einzige
  Zeile der Datenbank `ID_Type = 7` trägt.
* **Typ 7 ist unbelegt.** `Del(id, 7)` ist auf dem heutigen Datenbestand ein Leerlauf. Der
  Aufruf ist trotzdem nötig — `ReadAllFilter` liest Typ 7 mit, und ohne das zweite `DELETE`
  entstünden Dubletten, sobald eine Referenzliste befüllt wird. Ungetestet ist damit nur der
  Fall „Projekt mit Typ-7-Zeilen".
* **Verlustfreiheit des Roundtrips** hängt weiter daran, dass `SQL_ANLAGE_INSERT` alle 56
  schreibbaren Spalten führt (Paket-Nachweis in `Roundtrip_Erzeuger_Protokoll.md`). Der
  Trockenlauf bestätigt das für die WP-Zeilen erneut, prüft es aber nicht neu ab.
* **Der Trockenlauf simuliert „OK ohne Änderungen".** Der `WPCtrl`-Merge (Zeilen 167-177) und
  die Rückschreibung der Dialogfelder (Zeilen 190-210) sind bewusst nicht nachgebildet —
  gemessen ist die Del/Add-Sequenz, nicht der Dialog.
* **Doppelte Bezeichner.** Szenario A förderte die AP9b-Meldung „BYD B-Box HVM 11.0 kommt im
  Projekt 1011/1007 mehrfach vor" zutage. Das betrifft die Variantenzuordnung über den
  Bezeichner, nicht diesen Fix — die typgefilterten Aufrufe rühren die Speicheranlagen gar
  nicht erst an.

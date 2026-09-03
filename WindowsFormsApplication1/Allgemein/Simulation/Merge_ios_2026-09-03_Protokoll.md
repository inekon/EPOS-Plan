# Merge `origin/ios_migration` → `ios_migration` (03.09.2026)

Zusammenführung des lokalen Standes (13 Commits: Paket A und B des PV-Ertragsmodells,
Projektdialoge, FS1) mit den 81 Remote-Commits des **Umzugs nach `EPOS.Kern` / `EPOS.UI`**.

* **Merge-Base:** `d46e200`
* **Lokal vorher:** `b9c566f` (Vor-Merge-Commit, siehe unten)
* **Remote:** `430a864`
* **Merge-Commit:** `e428092` — „Merge origin/ios_migration (Umzug EPOS.Kern/EPOS.UI):
  Paket A/B PV-Ertragsmodell, Projektdialoge, FS1 nachgezogen"
* **Sicherungsreferenz:** Branch `sicherung/vor-merge-2026-09-03` auf `b9c566f`
* **Nachweisanker:** Branch `merge/ios-2026-09-03`
* Kein Push.

Vor dem Merge wurde der Arbeitsbaum in **einem** Commit `b9c566f` festgehalten
(„Vor-Merge-Stand 03.09.2026"): PV-Katalog-Koeffizienten (CEC-/PAN-Import, `Form_AdminPV`,
`Form_CECImport`, `PvModulPlausibilitaet`), das FS1-Protokoll und die Konzeptkorrektur N2.5
(Degradationsfaktor Jahr 20: **0,9092**, nicht 0,9088). Das Reparaturskript unter
`sql/pv_katalog/` ist mitgeführt, aber **nicht ausgeführt**.

---

## 1. Was der Umzug mit unseren Dateien gemacht hat

Die Umbenennungserkennung von Git hat den Großteil selbst getragen: Unsere Paket-A/B-Hunks
sind in den **neuen** Pfaden gelandet, ohne dass ein Konflikt entstand — `SimulationPV.cs`,
`PvErloesRechner.cs`, `AbweichungsErmittler.cs`, `SolarPVGISCalculator.cs`,
`WirtschaftlichkeitCtrl.cs`, `KatalogRegistry.cs`, die Modelle und `SchemaKatalog.cs`.

**Von Hand verschoben** (Git sah dort keine Ordnerumbenennung, weil die `.md`-Protokolle in
`WindowsFormsApplication1/Allgemein/Simulation/` geblieben sind und nur die `.cs` gewandert
sind):

| Datei | vorher | jetzt |
|---|---|---|
| `SolarZeitbasis.cs` | `WindowsFormsApplication1/Allgemein/Simulation/` | `EPOS.Kern/Allgemein/Simulation/` |
| `PvErweitertesModell.cs` | `WindowsFormsApplication1/Allgemein/Simulation/` | `EPOS.Kern/Allgemein/Simulation/` |

Beide werden von `SimulationPV` im Kern gelesen; der Kern kennt das Anwendungsprojekt nicht,
sie hätten dort also den Build gebrochen.

**Von Git als „file location"-Konflikt gemeldet und an den neuen Ort übernommen** (der
ganze Ordner `Allgemein/Import` ist gewandert):

| Datei | jetzt |
|---|---|
| `PvModulPlausibilitaet.cs` | `EPOS.Kern/Allgemein/Import/` |
| `PvKatalog_Koeffizienten_Protokoll.md` | `EPOS.Kern/Allgemein/Import/` |

**Unverändert am Platz geblieben:** `Views/Photovoltaik/Form_PVModell.cs` (die Masken sind
nicht gewandert), `Form_ProjektDelete.*`, sämtliche `.md`-Protokolle unter
`WindowsFormsApplication1/Allgemein/Simulation/` — und dieses hier.

**Namensräume:** Remote hat beim Umzug **keinen** Namensraum geändert; auch die Dateien in
`EPOS.Kern` stehen weiter in `namespace WindowsFormsApplication1`. Unsere Dateien führten
denselben Namensraum, es war also nichts anzugleichen. **Projektdateien:** beide Projekte
nehmen per SDK-Globbing auf; es war kein `Compile`-Eintrag zu setzen und es entstand keine
Dublette.

---

## 2. Konflikte und Entscheidungen

Zehn Konfliktdateien, dreizehn Hunks.

### 2.1 `EPOS.Kern/Controller/PhotovoltaikCtrl.cs` (1)

Remote hat mit **iU6** `OleDbParameter` durch den providerfreien `DbParam` ersetzt (auf
Linux/macOS wirft `new OleDbParameter(...)` schon im Konstruktor). Unser Paket-B-Parameter
für `Technologie` stand noch in der alten Bauform.
**Entscheidung:** Remote-Bauform übernommen, unseren Parameter in `DbParam` nachgezogen —
samt Kommentar „E2.3: leer bleibt NULL". Die Spalte `Technologie` selbst war im SQL bereits
konfliktfrei zusammengeführt.

### 2.2 `EPOS.Kern/Controller/PhotovoltaikStammCtrl.cs` (3)

Dreimal dasselbe Bild: Remote auf `DbParam`, unser `Tec()`-Parameter fehlte.
**Entscheidung:** `Tec()` gibt jetzt `DbParam` zurück, der Aufruf steht in allen drei
Anweisungen (INSERT, UPDATE, UpdateImport) an **derselben Stelle wie zuvor**.
Gegengeprüft: 19/19, 18/18 und 15/15 Spalten zu Platzhaltern zu Parametern — die Bindung
läuft über die POSITION, eine Verschiebung wäre stumm falsch.

### 2.3 `EPOS.Kern/Controller/ProjektPhotovoltaikCtrl.cs` (1)

Wie 2.1. **Entscheidung:** Remote-Bauform, `D("@deg", m.Degradation)` an alter Stelle wieder
eingesetzt (`D` liefert bereits `DbParam`). `ParameterInsert` reicht auf `Parameter` durch —
eine Stelle genügt.

### 2.4 `EPOS.Kern/Controller/SolardatenCtrl.cs` (1)

Remote hat `using System.Data.OleDb` und `using System.Windows.Forms` entfernt (der Kern
darf beides nicht mehr nennen), wir hatten dazwischen `using System.Globalization` ergänzt.
**Entscheidung:** Nur `System.Globalization` behalten. **Folgefund:** Unser
`ReadOrtszeit` benutzte noch einen `OleDbParameter` — auf `DbParam` umgestellt. Ohne das
hätte `EnableWindowsTargeting=false` den Kern-Build gebrochen.

### 2.5 `EPOS.Kern/MyResource/Resource.resx` und `Resource.en-US.resx` (je 1)

Beide Seiten haben am selben Ende neue Schlüssel angehängt.
**Entscheidung:** Vereinigung, unsere zuerst. **Achtung, Stolperstelle:** Das schließende
`</data>` unseres letzten Blocks lag im gemeinsamen Rumpf hinter dem Konflikt — die reine
Aneinanderreihung ergab ungültiges XML (MSB3103). Ein `</data>` an der Naht behebt es; der
Build ist der Wächter, der es gefunden hat. Danach je **2 796** `data`-Elemente in beiden
Sprachen, keine doppelten Schlüssel.
Unsere Schlüssel sind vollständig: **7** (4 × `PV_ANLAGE_*`, 3 × `PV_MODUL_*TNOCT`) aus PA1c,
**29** `PVM_*` und **2** `SIM_KARTE_PV_MODELL_*` aus PB1d — in de **und** en, und die
Designer-Eigenschaften dazu (`Resource.Designer.cs` war konfliktfrei). Die `PDLG_*`-,
`PA_*`- und `WZP_*`-Schlüssel der Projektdialoge haben absichtlich **keine**
Designer-Eigenschaft; sie werden über `ResourceManager.GetString` gelesen.

### 2.6 `WindowsFormsApplication1/Controller/WizardCtrl.cs` (2)

Der inhaltlich größte Konflikt. Remote hat mit **iU3** `SQL_ANLAGE_INSERT` und
`AnlagenParameter` in die neue Klasse `EPOS.Kern/Controller/AnlagenSql.cs` gezogen;
`WizardCtrl` **leitet nur noch weiter**. Unsere sieben Paket-A/B-Spalten standen in der
alten, jetzt gelöschten Fassung.

**Entscheidung:** Remote-Weiterleitung übernommen und unsere sieben Spalten **in
`AnlagenSql`** nachgezogen — Spaltenliste, Platzhalter und Parameter:

```
PV_WrWirkungsgrad, PV_Systemverluste,                    (Paket A, Schritt 62)
PV_Modell, PV_WrNennleistungKw, PV_WrEta10/50/100        (Paket B, Schritt 63)
```

Gegengeprüft: **63 Spalten = 63 Platzhalter = 63 Parameter.** Das ist zugleich die
Bedingung dafür, dass `WizardCtrl.Fachspalten()` weiter stimmt: Die Rettungsmenge ist als
**Komplement dieser Anweisung** definiert, es gibt keine zweite Liste. Weil die sieben
Spalten jetzt in `AnlagenSql` stehen, verlassen sie die Rettungsmenge automatisch — genau
wie vorher. Die ausführliche Begründung (Paket A/B, „nicht vollständig, mit Absicht",
FS1-Rettung) ist von `WizardCtrl` nach `AnlagenSql` mitgewandert, dorthin, wo die Anweisung
jetzt steht.

> **Nebenbefund:** Der Remote-Kommentar sprach von „alle 57 Spalten", tatsächlich waren es
> 56 — die Abweichung bestand schon vor dem Merge. Der Text nennt jetzt die **gezählte**
> Zahl 63.

**Folgefund im selben Build:** Unser FS1-Block (`FachspaltenSichern` /
`FachspaltenWiederherstellen`) benutzte an vier Stellen `OleDbParameter`, dessen `using`
Remote entfernt hat. Auf `DbParam` umgestellt.

### 2.7 `WindowsFormsApplication1/Controller/MenueCtrl.cs` (3)

Der einzige echte **Entwurfskonflikt**. Remote hat mit **iU5** jede Maskenkonstruktion aus
dem Controller entfernt (`Dienste.Navigation.OeffneMaske(Masken.X, …)`, `Dienste.Dialog`
statt `MessageBox`); unser Auftrag vom 02.09. hat den Löschdialog auf **Mehrfachauswahl**
umgebaut, mit direktem `new Form_ProjektDelete()`, `MessageBox` und einer Liste als
Ergebnis. Beide Absichten stehen quer zueinander — und Remotes `WinFormsNavigation` las an
`Form_ProjektDelete` noch `frm.ID_Projekt` / `frm.szProjekt`, die es seit unserem Umbau
nicht mehr gibt (ein Bruch, der ohne Zutun entstanden wäre).

**Entscheidung: beides erhalten, nicht eines opfern.** Neue Nutzlastklasse
`EPOS.Kern/Allgemein/Dienste/Projektloeschwahl.cs` — nach derselben Bauform wie
`Projektwahl`, aber mit `List<ProjektModel> ZuLoeschen` und `SicherungGewuenscht`.
`WinFormsNavigation` füllt sie (`LoeschwahlUebernehmen`), `MenueCtrl.ProjektDelete` ruft
`Dienste.Navigation.OeffneMaske(Masken.ProjektDelete, wahl)` und behält unsere Schleife über
die Liste. Damit kennt der Controller die Maske nicht (iU5) **und** die Mehrfachauswahl
bleibt (Auftrag 02.09.).

Weitere Entscheidungen in derselben Datei:

* **Remotes Einzelrückfrage entfällt.** Sie fragte „Projekt '{Name}' wirklich löschen?" für
  genau ein Projekt. Unser Dialog fragt vor dem Löschen mit der **vollständigen Liste**
  zurück; die alte Rückfrage wäre eine zweite, schwächere Abfrage über denselben Vorgang.
* `MessageBox.Show(...)` der Erfolgsmeldung → `Dienste.Dialog.Warnung` bzw. `.Meldung`.
* `DatenbankSichern`: `MessageBox` mit `YesNo`/`Warning`/`Button2` →
  `Dienste.Dialog.Frage(..., warnend: true, vorgabeNein: true)` — dieselbe Aussage,
  dieselbe Vorbelegung.
* `AktuellesProjektZuruecksetzen`: `OleDbParameter` → `DbParam`.
* Der Ressourcenhelfer `Form_ProjektDelete.TPd` bleibt als **eine** Wahrheit für die
  `PDLG_*`-Texte; er ist ein reiner `ResourceManager`-Zugriff, keine Maskenbedienung.

### 2.8 Migrationsstand

`SchemaMigration.cs` ist remote in der Anwendung geblieben und wurde konfliktfrei
zusammengeführt. Nachgeprüft: `ZIEL_VERSION = 63`, `FREEZE_VERSION_ACCESS = 61`,
`SCHRITTE_SQLITE` führt **62** und **63**. Remote hatte keinen Schritt jenseits 61 — es gab
keine Nummernkollision.

---

## 3. Nachweis

### 3.1 Build

| Stand | Ergebnis |
|---|---|
| Merge (Worktree `P:\wt`, x64) | **0 Fehler** |
| Merge (`git archive` → `P:\merge\src`, x64) | **0 Fehler** |
| `origin/ios_migration` pur (`P:\merge\theirs`, x64) | **0 Fehler** |

**Warnungsprofil identisch** zwischen Merge und reinem `origin/ios_migration`:
WFO1000 28, NU1510 4, CS0109 2, CS0108 2, WFO0003 1, CA2255 1. **Aus unseren Dateien kommt
keine einzige neue Warnung.**

### 3.2 Referenzlauf — der Dreifach-Nachweis

Werkzeug `Referenzlauf/` (baut nach dem Umzug unverändert: es referenziert die Anwendung,
und die referenziert jetzt `EPOS.Kern`), 14 Projekte, Quelle `P:\pa0\Quelle\Kenndaten.sqlite`
(MD5 `47bcefaca0f18d2180ba37786c6cb6b3`), je frische Arbeitskopie.

| Vergleich | Ergebnis |
|---|---|
| **MERGE gegen PB1** (`2026-09-03_M1_nach-Merge` ↔ `2026-09-03_PB1_nach-PaketB`) | **355/355 bitgleich**, 0 ungleich, keine Datei nur auf einer Seite |
| **THEIRS gegen PA0** (`origin/ios_migration` pur ↔ `2026-09-02_PA0_vor-PaketA`) | **355/355 bitgleich** |
| Toleranzvergleich PB1 → M1 | **14/14 PASS**, 3 882 476 Werte |
| `pruefen` auf M1 | **plausibel**, dieselben drei Bestandshinweise |

**Einordnungstabelle (Datei | PB1=MERGE? | THEIRS=PA0? | Einordnung): leer.**
Es gibt **keine** Abweichung MERGE ≠ PB1, also nichts einzuordnen. Der zweite Vergleich
zeigt zugleich, warum: Die 81 Remote-Commits — der ganze Umzug, `DbParam` statt
`OleDbParameter`, `AnlagenSql`, `Dienste`/`Navigation` — haben **keinen einzigen gerechneten
Wert** verschoben. Beide Achsen sind exakt, nicht nur innerhalb der Toleranz.

### 3.3 Harness Paket A und B (gegen den Merge-Build)

| Probe | Ergebnis |
|---|---|
| `rein` — Zeitbasis (PA1) | **18 PASS, 0 FAIL** |
| `rein` — Modell (PB1): Huld, Hay-Davies, Kennlinie, Clipping, Degradation, Technologie | **58 PASS, 0 FAIL** |
| `zeitbasis` an der DB-Kopie (14 Klimaregionen) | **115 PASS, 0 FAIL** |
| `migration` 61 → 62 → 63 auf frischer Kopie, Zweitlauf idempotent, kein DML | **24 PASS, 0 FAIL** |
| INEKON „Schulung 01", Prüfstand `kd1runner` Modus `pv6` | **28 PASS, 0 FAIL** (I3/I4 ±1 %) |

Geprüfte Kernaussagen unverändert: Huld η_rel(1, 0) = 1 **exakt**, Hay-Davies bei DNI = 0
identisch zum isotropen Modell (22 716 Sonnenstunden, max. Abweichung 0), Clipping-Verlust
exakt Σ max(0, P_DC·η − P_AC,nenn), Energiebilanz geschlossen, Degradation Jahr 20 bei
0,5 %/a = **0,909156** (die korrigierte Konzeptangabe 0,9092).

Die Harness-Vorlagen brauchten genau **drei** Anpassungen an die neue Struktur — alle drei
sind Adressierung, keine Fachänderung:

1. Zusätzliche Referenz auf `EPOS.Kern.dll` (die Namensräume sind gleich geblieben).
2. Der DB-Pfad wird nicht mehr per Reflexion auf `Properties.Settings` gesetzt (die sind mit
   dem Kern gewandert), sondern über den dafür vorgesehenen Haken
   `DataRepository.PfadUeberschreibung` — der die Einstellungen des Anwenders gar nicht erst
   berührt.
3. `SolardatenCtrl` wird über `typeof(DataRepository).Assembly` gesucht statt über
   `typeof(SchemaMigration).Assembly` — der eine Typ ist im Kern, der andere in der Anwendung.

Die produktive Datenbank `C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite` blieb unberührt
(Zeitstempel 02.09.2026 22:07:36, vorher wie nachher).

---

## 4. Offene Punkte

* **Sichtabnahme am Programm** steht aus — insbesondere der umgebaute Löschdialog über den
  neuen Weg `Dienste.Navigation` → `Projektloeschwahl` (Mehrfachauswahl, Sicherungskopie,
  Erfolgs- und Fehlermeldung) und die PV-Masken `Form_PV`, `Form_PVModell`, `Form_AdminPV`.
* **Kein Push.** `sicherung/vor-merge-2026-09-03` und `merge/ios-2026-09-03` sind lokale
  Anker und können nach der Abnahme entfallen.
* Der Merge zieht den **Schemastand 63** in den Umzugsstrang: `.wpx`-Pakete mit Stand 62
  werden abgewiesen — systemimmanent wie bei jedem Migrationsschritt.
* `sql/pv_katalog/` (Reparaturskript Isc-Signatur) ist weiterhin **nur mitgeführt**, nicht
  ausgeführt; die Freigabe steht aus.
* Aus dem Bestand unverändert offen: Bestätigung der Degradation „vermiedener Bezug",
  Katalogpflege Technologie/T_NOCT, E3 zurückgestellt.

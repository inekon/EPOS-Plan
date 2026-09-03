# Merge 4 `origin/ios_migration` → `ios_migration` (03.09.2026, früher Nachmittag)

Vierte Zusammenführung desselben Strangs am selben Tag. Merge 1 holte den **Umzug** nach
`EPOS.Kern` / `EPOS.UI`, Merge 2 iU9/iU10 und das SQL-Dialekt-Audit, Merge 3 die **Welle 1**
des iU9-Blazor-Ports. Merge 4 holt die **Welle 0** desselben Vorhabens — und die ist von
anderer Art: Sie portiert nichts, sie **legt still**. Nach dem Anwenderentscheid **iF29**
verschwinden neun Altmasken ersatzlos, die Kostenstatics aus `Form_Kosten` sind vorher in
den Kern gerettet.

* **Merge-Base:** `908926a` (der Remote-Elter von Merge 3 — unser Strang ist seither nicht
  neu verzweigt)
* **Lokal vorher:** `83498dc` (20 Commits: Paket A/B, Projektdialoge, FS1, die Nachweise der
  Merges 1–3)
* **Remote:** `b0d3d86` (`bb0474c`…`b0d3d86`, **acht** Commits, **90 Dateien, +860 / −10 877**)
* **Merge-Commit:** `f6acb04` — „Merge 4 origin/ios_migration (b0d3d86): Paket A/B,
  Projektdialoge, FS1 nachgezogen"
* **Sicherungsreferenz:** Branch `sicherung/vor-merge4-2026-09-03` auf `83498dc`
* **Nachweisanker:** Branch `merge4/ios-2026-09-03`
* Kein Push.

Der Arbeitsbaum war vor dem Merge sauber; es gab nichts vorab festzuhalten.

---

## 1. Was Welle 0 stilllegt — und warum das die günstigste Sorte Remote-Änderung ist

Acht Commits, in vier Schritten:

| Commit | Inhalt |
|---|---|
| `bb0474c` | **W0.1** — Kostenstatics aus `Form_Kosten` in den Kern retten: neu `EPOS.Kern/Controller/KostenSummenCtrl.cs` und `EPOS.Kern/Model/EnergietraegerModel.cs` |
| `16b106a` | **W0.2** — neun Altmasken stilllegen (Anwenderentscheid iF29) |
| `b9974dc` | `EPOS.iOS`: `Microsoft.Maui.Controls` ausdrücklich referenzieren (CI-Nachzug) |
| `43452a7` | **W0.3** — Formularkarte-Tests und Befundliste auf den Stand nach der Stilllegung |
| `5b53f61` | **W0.4** — Doku nachziehen |
| `4a64dfd` | Merge iU9 W0 auf `ios_migration` |
| `3ac5e02` | `EPOS.iOS`: Verweise auf den gelöschten Kosteneditor berichtigt (W0‑O1) |
| `b0d3d86` | `CLAUDE.md`: iOS-Job pauschal freigegeben (Anwender) |

**Gelöscht (25 Dateien, neun Masken):** `Form_Kosten` (+ `.Designer.cs`, `.resx`),
`ucKostenItem` (+ `.Designer.cs`, `.resx`), `Form_Betriebskosten`, `Form_Bericht`,
`Form_Simulation_Kurz` (+ Designer und **drei** Ressourcendateien),
`Form_Variantentest` (+ Designer, resx), `Form_KwkgModule` (+ Designer, resx),
`Form_Wirtschaftlichkeit`, dazu die Karteileichen `ChartManagerNeu.cs` und
`Form_Simulation_Detail - Kopie.cs`. Das erklärt die −10 877 Zeilen bei nur +860.

**Umbenannt statt gelöscht:** `Form_KostenfaktorItem` (`.cs`, `.Designer.cs`, `.resx`) wandert
nach `Werkzeuge/Formularkarte.Tests/Pruefmuster/Kosten/` — sie lebt nur noch als **Prüfmuster**
der Formularkarte weiter, nicht mehr als Maske der Anwendung.

**Neu (zwei Dateien):** `EPOS.Kern/Controller/KostenSummenCtrl.cs` (`internal static class`
mit `KATEGORIE_INVESTITION`, `KATEGORIE_BETRIEB`, `GetAllCarriers`, `LiesKomponentenSummen`,
`LiesAnlagenSummen`) und `EPOS.Kern/Model/EnergietraegerModel.cs`.

**Keine der neun gelöschten Masken stand in unserer Änderungsmenge.** Es ging also nichts
von uns mit unter. Wo Remote unsere Dateien anfasst, tut er es fast nur im Fließtext: Von
den 90 Dateien liegen **sechs** in beiden Änderungsmengen, und in fünf davon ändert er
ausschließlich Doku-Kommentare, die auf die nun gelöschten Masken verwiesen.

---

## 2. Berührungsfläche und Auflösung

Unsere 20 Commits ändern **67** Dateien (ohne `Referenzlaeufe/`), die acht Remote-Commits
**90**. **Sechs Dateien liegen in beiden Mengen — und alle sechs gingen ohne Konflikt
zusammen.**

| Datei | was Remote dort tut | Auflösung |
|---|---|---|
| `EPOS.Kern/Allgemein/Update/SchemaKatalog.cs` | eine Doku-Zeile (`Form_Kosten.LoadKostenFaktoren` → „die Kostenmasken") | automatisch |
| `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` | BOM ergänzt + eine Doku-Zeile (`Form_Wirtschaftlichkeit` → `UcWirtschaftlichkeit`) | automatisch |
| `WindowsFormsApplication1/Allgemein/KI/HilfeKontext.cs` | BOM ergänzt + **sieben** Einträge der stillgelegten Masken entfernt | automatisch |
| `WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs` | 13 Doku-Zeilen | automatisch |
| `WindowsFormsApplication1/Controller/WizardCtrl.cs` | vier Doku-Zeilen | automatisch |
| `WindowsFormsApplication1/Views/Wirtschaftlichkeit/Form_PhotovoltaikVerguetung.cs` | **echte Codeänderung**: `Form_Kosten.*` → `KostenSummenCtrl.*` an drei Stellen | automatisch |

**Es gab keinen Konfliktmarker aufzulösen.** `P:\merge4\aufloesungen\` ist deshalb leer —
das ist der Befund, nicht ein Versäumnis. Die Markersuche über den ganzen Baum
(`^<<<<<<<`, `^=======`, `^>>>>>>>` in `.cs`, `.resx`, `.razor`, `.md`, `.csproj`, `.sln`,
`.txt`, `.css`) hat genau **einen** Treffer: dieselbe Trennlinie aus Gleichheitszeichen in
`VDI-3805-Daten/KWK-Daten/2G Energy/_Anfrage_2G_avus_Datenblaetter.txt` wie bei Merge 3 — im
Bestand, von keiner Seite berührt. `MERGE_HEAD` stand vor dem Commit auf `b0d3d86`; der
Merge-Commit trägt beide Eltern (`83498dc`, `b0d3d86`).

Der **Umfang** ist gegengemessen: `git diff --cached HEAD` vor dem Commit nennt
**90 Dateien, +860 / −10 877** — Zeichen für Zeichen die Zahlen von
`git diff 908926a origin/ios_migration`.

### 2.1 Der schärfere Nachweis: das Merge-Delta ist gleich dem Remote-Delta

Bei Merge 3 wurde je Datei gezählt, ob jede hinzugefügte Zeile ankam und jede gelöschte
verschwand. Hier geht es strenger, weil beide Seiten dieselben sechs Dateien anfassen:

> Für **jede** der sechs Dateien ist `git diff 83498dc <Merge>` **zeilengleich** mit
> `git diff 908926a b0d3d86` (Prüfsumme über die geänderten Zeilen, sechs von sechs
> **GLEICH**).

Das ist beides auf einmal: Der Merge hat an diesen Dateien **genau** das getan, was Remote
getan hat — und **nichts** an unserer Seite. Es kann weder eine unserer Zeilen verloren
gegangen sein noch eine von Remotes Zeilen fehlen.

Drei scheinbar „zurückgebliebene" gelöschte Zeilen aus der Merge‑3‑Zählweise sind
Teilstring-Artefakte und wurden einzeln aufgelöst:

* `using System;` (zweimal) — Remote hat nur das **BOM** ergänzt; die Datei trägt jetzt
  `EF BB BF` und danach dieselbe Zeile. Beide Dateien nachgemessen.
* `/// <c>KostenPositionCtrl.StammIdHaupt</c> nichts, und` — Remotes Ersatzzeile ist die
  alte Zeile **plus** „die Vorsorge der"; die alte ist ihr Präfix.
* Die drei `ZIEL_VERSION`-Zeilen in `SchemaMigration.cs` — sie stehen **zweimal** in der
  Datei: einmal im eingefrorenen **Access**-Zweig (dort haben *wir* auf
  `FREEZE_VERSION_ACCESS` umgestellt, Zeilen 3854/3969/4221) und einmal im **SQLite**-Zweig
  (unverändert `ZIEL_VERSION`, Zeilen 4346/4422/4423).

### 2.2 Die ausdrücklich verlangte Nummernprobe: **keine Kollision**

Vor der Auflösung geprüft, ob Welle 0 eigene Schritte 62/63 oder eine geänderte
`ZIEL_VERSION` mitbringt — das hätte mit unseren Nummern kollidiert und wäre ein Fall zum
Anhalten gewesen. Der Befund am reinen Remote-Stand `b0d3d86`:

```
130:        public const int ZIEL_VERSION = 61;
4139:        private static readonly Schritt[] SCHRITTE_SQLITE =
4142:            // new Schritt(SCHRITT_62_..., "Kurzbeschreibung", …   ← nur der Platzhalter-Kommentar
```

Remote steht **weiter auf 61** und führt **keinen** SQLite-Schritt. Seine 13 Zeilen an dieser
Datei sind ausnahmslos Doku. **Keine Kollision** — der Konflikt war normal aufzulösen, und
der Merge-Stand trägt unverändert:

| Konstante | Merge-Stand |
|---|---|
| `ZIEL_VERSION` | **63** (Zeile 147) |
| `FREEZE_VERSION_ACCESS` | **61** (Zeile 169) |
| `SCHRITT_62_PV_ANLAGENPARAMETER` | 62 (Zeile 2316) |
| `SCHRITT_63_PV_MODELLWAHL` | 63 (Zeile 2352) |
| `SCHRITTE_SQLITE` | enthält beide Schritte, 62 und 63 |

### 2.3 Die eine echte Codeänderung an einer unserer Dateien: `Form_PhotovoltaikVerguetung`

Weil `Form_Kosten` gelöscht ist, mussten drei Aufrufe umgehängt werden:

| Stelle im Merge-Stand | vorher | nachher |
|---|---|---|
| Zeile 160 | `Form_Kosten.KATEGORIE_INVESTITION` | `KostenSummenCtrl.KATEGORIE_INVESTITION` |
| Zeile 161 | `Form_Kosten.KATEGORIE_BETRIEB` | `KostenSummenCtrl.KATEGORIE_BETRIEB` |
| Zeile 381 | `Form_Kosten.LiesKomponentenSummen(…)` | `KostenSummenCtrl.LiesKomponentenSummen(…)` |

Die Zielkonstanten sind im Kern **wertgleich** definiert
(`KATEGORIE_INVESTITION = DbWerte.KOSTEN_KATEGORIE_INVESTITION`, ebenso Betrieb) — die
Umhängung ist ein Ortswechsel, keine Fachänderung.

**Unser Degradationsfeld (Stufe E2.4, Commit `74f9acf`) sitzt in derselben Datei und ist
unversehrt:** 18 Fundstellen `numDegradation` / `DegradationsfeldAnlegen` /
`PVM_DEGRADATION`. Unsere Hunks liegen an den Altzeilen 47–56, 61–67, 134–140, 227–237 und
350–356, Remotes an 99–103 und 314–317 — sie berühren einander nicht.

### 2.4 Die übrigen genannten Andockungen

* **`WirtschaftlichkeitCtrl`** — die Erlösreihe `PV_VERGUETUNG` steht unverändert
  (Zeile 1889, gespeist aus `PvErloesRechner.Rechne`); Remote hat nur BOM und einen
  Doku-Satz angefasst. `PvErloesRechner.cs` liegt gar nicht in Remotes Änderungsmenge.
* **`WizardCtrl`** — unser FS1-Block und die PV-Spalten in `AnlagenSql` stehen unverändert;
  Remotes vier Zeilen sind `<summary>`-Text.
* **`HilfeKontext`** — unsere PV-Einträge (u. a. `Form_PVModell` aus Paket B) bleiben,
  Remotes sieben Einträge der stillgelegten Masken (`Form_Bericht`, `Form_Kosten`,
  `Form_KostenfaktorItem`, `Form_Simulation_Kurz`, `Form_Variantentest`, `Form_KwkgModule`,
  `Form_Wirtschaftlichkeit`) fallen weg.

### 2.5 Ressourcen — unverändert, und trotzdem gezählt

Welle 0 fasst `Resource.resx` / `Resource.en-US.resx` / `Resource.Designer.cs` **nicht** an —
die drei Dateien stehen nicht in ihren 90. Die Zählung wurde dennoch wiederholt und trifft
Merge 3 auf die Stelle:

* **XML wohlgeformt**, je **2 944** `data`-Elemente in de **und** en, **2 944 eindeutige
  Namen** in beiden.
* **de und en führen dieselbe Schlüsselmenge** (Differenzmenge beidseitig leer).
* **Unsere Schlüssel** vollzählig: `PVW_` 63, `PVM_` 29, `PDLG_` 13, `PV_ANLAGE_` 4,
  `PV_MODUL_` 3, `WZP_` 5, `PA_` 2.
* `Resource.Designer.cs`: **2 819** Eigenschaften, **alle eindeutig** — kein CS0102.

---

## 3. Nachweis

### 3.1 Build — dreimal, mit demselben Werkzeug

MSBuild aus VS 18 Community, `-restore /p:Platform=x64 /p:Configuration=Debug`, `WP-Plan.sln`.

| Stand | Ergebnis |
|---|---|
| Merge, Worktree `P:\wt4` | **0 Fehler**, 32 Warnungen |
| Merge, `git archive f6acb04` → `P:\merge4\src` | **0 Fehler**, 32 Warnungen |
| `origin/ios_migration` pur (`b0d3d86`), `git archive` → `P:\merge4\theirs` | **0 Fehler**, 32 Warnungen |

**Warnungsprofil in allen drei Läufen identisch:** WFO1000 22, NU1510 4, CS0108 2, CS0109 2,
WFO0003 1, CA2255 1. Schärfer: Die **30** datei- und zeilengenauen Meldungen sind in allen
drei Läufen Zeile für Zeile dieselben (Vergleich nach Pfadnormierung: **0 Unterschiede**).
**Aus unseren Dateien kommt keine einzige neue Warnung.**

**Der Unterschied zu Merge 3 (34 Warnungen / 29 Meldungen) ist restlos erklärt** und
stammt allein aus der Stilllegung:

| Meldung | Merge 3 | Merge 4 | Grund |
|---|---|---|---|
| `UcBericht.cs(65,21)` WFO1000 „AlsDialog" | vorhanden | **weg** | `Form_Bericht` gelöscht, die Eigenschaft `AlsDialog` mit ihr |
| `UcWirtschaftlichkeit.cs(80,21)` WFO1000 „AlsDialog" | vorhanden | **weg** | `Form_Wirtschaftlichkeit` gelöscht, dito |
| `UcBkKosten.cs(1304/1310,27)` WFO1000 | 1304 / 1310 | **1305 / 1311** | W0 hat eine Zeile darüber eingefügt — dieselbe Meldung, um eins verschoben |

Beide Streichungen sind am Merge-Stand gegengeprüft: `AlsDialog` kommt in `UcBericht.cs` und
`UcWirtschaftlichkeit.cs` nicht mehr vor.

### 3.2 Referenzlauf — der Dreifach-Nachweis

Werkzeug `Referenzlauf/`, 14 Projekte
(1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, 1026, 1028, 1029, 1030, 1039, 1043),
Quelle `P:\pa0\Quelle\Kenndaten.sqlite` (MD5 `47bcefaca0f18d2180ba37786c6cb6b3`, vor dem Lauf
gemessen), je **frische** Arbeitskopie; die Arbeitskopie des Merge-Laufs migriert 61 → 63,
die des THEIRS-Laufs bleibt auf 61 (Remote steht weiter auf `ZIEL_VERSION = 61`).

| Vergleich | Ergebnis |
|---|---|
| **MERGE (M4) gegen M3** (`2026-09-03_M4_nach-Merge4` ↔ `2026-09-03_M3_nach-Merge3`) | **355/355 bitgleich** (MD5), 0 ungleich, keine Datei nur auf einer Seite |
| **THEIRS (`b0d3d86`) gegen THEIRS (`908926a`)** (`P:\merge4\theirs_lauf` ↔ `P:\merge3\theirs_lauf`) | **355/355 bitgleich** |
| Toleranzvergleich M3 → M4 | **14/14 PASS**, 3 882 476 Werte |
| `pruefen` auf M4 | **plausibel**, dieselben Bestandshinweise wie M1/M2/M3 |

> Die 356. Datei je Ordner ist das `lauf_protokoll.md` des Laufs selbst (Zeitstempel und
> Pfade); sie zählt in keiner der vier Fassungen zum Nutzdatenbestand.

**Einordnungstabelle (Datei | M3 = MERGE? | THEIRS abweichend? | Einordnung): leer.**
Es gibt **keine** Abweichung MERGE ≠ M3, also nichts einzuordnen.

Warum das hier nicht überrascht und trotzdem gemessen gehört: Welle 0 **löscht** Oberfläche.
Der einzige Code, der den Rechenweg berührt, ist die Rettung der Kostenstatics nach
`KostenSummenCtrl` — und die trägt `LiesKomponentenSummen` / `LiesAnlagenSummen` /
`GetAllCarriers` unverändert weiter. Der zweite Vergleich belegt das ohne unsere Pakete: der
reine Remote-Stand rechnet byte-für-byte wie vor der Stilllegung. Und weil zugleich
MERGE = M3 exakt gilt, hat auch die Zusammenführung nichts verschoben. **Beide Achsen sind
exakt.**

Der Lauf selbst: 14 von 14 erfolgreich, 26 Warnungen, 0 Fehler — dieselben Zahlen wie M1,
M2 und M3.

### 3.3 Harness Paket A und B (gegen den Merge-Build)

| Probe | Ergebnis |
|---|---|
| `rein` — Zeitbasis (PA1) | **18 PASS, 0 FAIL** |
| `rein` — Modell (PB1): Huld, Hay-Davies, Kennlinie, Clipping, Degradation, Technologie | **58 PASS, 0 FAIL** |
| `zeitbasis` an der DB-Kopie (14 Klimaregionen) | **115 PASS, 0 FAIL** |
| `migration` 61 → 62 → 63 auf frischer Kopie, Zweitlauf idempotent, kein DML | **24 PASS, 0 FAIL** |
| INEKON „Schulung 01", Prüfstand `kd1runner` Modus `pv6` | **28 PASS, 0 FAIL** (I3 −0,76 %, I4 −0,47 %) |

Zusammen **243 PASS, 0 FAIL** — Probe für Probe dieselben Zahlen wie bei den Merges 1–3,
samt der INEKON-Abstände auf zwei Nachkommastellen.

### 3.4 Die beiden ausdrücklich verlangten Einzelproben

Weil Welle 0 `Form_PhotovoltaikVerguetung` anfasst, wurden die zwei Aussagen wieder
**headless am Merge-Build** nachgemessen:

* **Degradation.** `PvErloesRechner.DegradationsFaktor(0.5, 20)` liefert **0.909156**, also
  die Konzeptangabe **0,9092**, und ist bitgenau gleich `(1 − 0,005)^19`. Probe `g3` in
  `ModellProbe.Degradation()`, Aufruf **direkt** — kein Umweg über eine Maske. Dazu
  unverändert grün: `g1` (d = 0 → Faktor 1), `g2` (Jahr 1 immer 1, Exponent t−1), `g4`
  (monoton fallend), `g5` (negative Eingabe ignoriert), `g6` (d = 100 %/a → Jahr 2 ist 0).
* **PV-Wirtschaftlichkeit.** `pv6` gegen die INEKON-Referenz „Schulung 01" trifft weiterhin
  **I3 −0,76 %** (91 867 gegen 92 568) und **I4 −0,47 %** (−23 087 gegen −22 979), beide
  innerhalb ±1 %; `I5` bestätigt den Rohabstand als reine Konventionsdifferenz (< 4 %).
  Ebenfalls grün: der ganze Rückladepfad P0–P5.

Die Harness-Vorlagen mussten **gar nicht** angepasst werden; es genügte, in
`mrunner.csproj`, `harness/Program.cs`, `kd1runner.csproj` und `kd1runner/Program.cs` den
Pfad `P:\merge3\src` auf `P:\merge4\src` zu ziehen (byte-erhaltend, alle vier ohne BOM).

> **Fallstrick, unverändert gültig.** Der Modus `pv6` **migriert nicht**; er setzt nur
> `DataRepository.PfadUeberschreibung`. Auf einer Kopie im Schemastand **61** meldet er
> „no such column: Degradation" und liefert 24 PASS / 4 FAIL — Folgefehler eines
> fehlgeschlagenen `Speichern`, **kein** Befund am Code. Hier wurde deshalb wieder die von
> der Migrationsprobe auf **63** gebrachte Kopie verwendet.

Die produktive Datenbank `C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite` blieb unberührt
(Zeitstempel **02.09.2026 22:07:36**, vorher wie nachher gemessen).

### 3.5 Übernahme in den Hauptbaum

Der Hauptbaum stand unverändert auf `83498dc` und war sauber; die Übernahme lief deshalb als
**`git merge --ff-only merge4/ios-2026-09-03`** — ein reines Vorspulen, kein zweiter
Merge-Commit. Der Worktree `P:\wt4` ist entfernt, der Branch bleibt als Anker.
`core.longpaths` war nur für `git worktree add`, den Merge **und den Merge-Commit** auf dem
subst-Pfad gesetzt und ist wieder entfernt (`git config --get core.longpaths` liefert nichts).

> **Fallstrick aus Merge 1–3, wieder bestätigt:** `core.longpaths=true` reicht nicht, wenn
> man es nur für `git worktree add` setzt. Der anschließende `git merge` liest den Baum
> erneut ein (`read-tree`) und scheitert sonst an `VDI-3805-Daten/…` mit
> „Filename too long" — mit einem `fatal: read-tree failed`, das wie ein Merge-Fehler
> aussieht, aber keiner ist.

**Der Hauptbaum-Build nach `bin\x64\Debug` ist bewusst NICHT gelaufen.** Visual Studio war
während des ganzen Vorgangs offen (`WP-Plan — Form_Klimadaten.cs [Entwurf]*`, also mit
**ungespeicherten Designer-Änderungen**). Der Merge-Stand ist dreifach gebaut und
warnungsgleich; der vierte Build hätte nichts Neues gezeigt, aber mit dem offenen Designer
kollidieren können. **Nachzuholen, sobald VS geschlossen ist** — siehe offene Punkte.

---

## 4. Offene Punkte

* **Hauptbaum-Build `bin\x64\Debug` nachholen**, sobald Visual Studio geschlossen ist. Der
  Nachweis hängt nicht daran (drei Builds, 0 Fehler, zeilengleiches Warnungsprofil), die
  lauffähigen Binärdateien im Hauptbaum aber schon.
* **Sichtabnahme am Programm** steht aus. **Neu hinzugekommen:** die neun stillgelegten
  Masken. Anders als bei Welle 1 gibt es hier **keine** Nachfolgemaske abzunehmen — abzunehmen
  ist, dass die **Wege dorthin** verschwunden sind und kein Menüpunkt, kein Knopf und kein
  Hilfeverweis ins Leere zeigt. Betroffen: Kosteneditor (`Form_Kosten`, `ucKostenItem`),
  Betriebskostenpflege, Berichtsmaske (`Form_Bericht` → `UcBericht`), Kurzsimulation,
  Variantentest, KWKG-Module, Wirtschaftlichkeitsmaske (`Form_Wirtschaftlichkeit` →
  `UcWirtschaftlichkeit`). Remote führt seine eigene Befundliste in
  [`Werkzeuge/Formularkarte/Erreichbarkeit_2026-09-03.md`](../../../Werkzeuge/Formularkarte/Erreichbarkeit_2026-09-03.md).
* **Unsere eigene Zusatzprobe zu W0:** Der **PV-Vergütungsdialog** liest seine
  Investitions- und Betriebssummen jetzt aus `KostenSummenCtrl` statt aus `Form_Kosten`.
  Rechnerisch ist es derselbe Weg (2.3), gesehen hat es noch niemand: Dialog öffnen und
  nachsehen, dass die PV-Komponentensummen und der Strompreisvorschlag weiter erscheinen.
* **Aus Merge 3 unverändert offen:** die sieben Dialoge der Welle 1 mit ihrer Abnahmeliste
  (`iU9_W1_Blazor_Port_Protokoll.md`), die Zusatzprobe „PV-Erlösreihe im neuen
  Kapitalwert-Verlauf", W1‑O1 … W1‑O7 des Remote-Strangs.
* **Aus Merge 2 unverändert offen:** die Blazor-Dialoge aus iU9 (Heizkessel/BHKW →
  „Energieträger-Variante"), der Dialog BHKW-Wirtschaftlichkeit (B5), FX2–FX5; die vier vom
  Remote selbst benannten Pfade (Kosten → Position mit **neuer** Gruppe; Preisreihen auf
  einer DB **ohne** `Tab_PreisreiheDaten`; Klimadaten → Region löschen; Wärmepumpe-Test →
  „Kühlung"). **Aus Merge 1:** Löschdialog über `Dienste.Navigation` → `Projektloeschwahl`,
  die PV-Masken `Form_PV`, `Form_PVModell`, `Form_AdminPV`.
* **Vom Remote selbst als offen gekennzeichnet:** W0‑O1 (die `EPOS.iOS`-Verweise auf den
  gelöschten Kosteneditor) ist mit `3ac5e02` bereits berichtigt; die Formularkarte meldet
  nach der Stilllegung `nein`/`verwaist` auf 0. 105 von 111 Masken bleiben offen.
* **Nebenbefund am Prüfwerkzeug, unverändert:** `kd1runner` **ohne** Modusargument stirbt
  seit dem Umzug an einer `NullReferenceException` (er sucht
  `WindowsFormsApplication1.Properties.Settings` per Reflexion in der Assembly von
  `SchemaMigration`; die Einstellungen sind mit dem Kern gewandert). Der Modus `pv6` ist
  nicht betroffen. Reparatur auf Zuruf.
* **Kein Push.** `sicherung/vor-merge4-2026-09-03` und `merge4/ios-2026-09-03` sind lokale
  Anker und können nach der Abnahme entfallen; die Anker der Merges 1–3 stehen ebenfalls
  noch. Der lokale Branch steht damit **21 Commits** vor `origin/ios_migration`.
* Schemastand bleibt **63**; `.wpx`-Pakete mit Stand 62 werden abgewiesen — systemimmanent.
* `sql/pv_katalog/` (Reparaturskript Isc-Signatur) ist weiterhin **nur mitgeführt**, nicht
  ausgeführt; die Freigabe steht aus.
* Aus dem Bestand unverändert offen: Bestätigung der Degradation „vermiedener Bezug",
  Katalogpflege Technologie/T_NOCT, E3 zurückgestellt.

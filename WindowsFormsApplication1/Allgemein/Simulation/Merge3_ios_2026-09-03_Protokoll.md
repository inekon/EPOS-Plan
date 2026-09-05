# Merge 3 `origin/ios_migration` → `ios_migration` (03.09.2026, mittags)

Dritte Zusammenführung desselben Strangs am selben Tag. Merge 1 holte den **Umzug** nach
`EPOS.Kern` / `EPOS.UI`, Merge 2 iU9/iU10 und das SQL-Dialekt-Audit; Merge 3 holt die
**Welle 1 des iU9-Blazor-Ports**: **elf Remote-Commits**, die **sieben WinForms-Masken** der
Kosten- und Wirtschaftlichkeitsseite durch **sechs Razor-Komponenten** ersetzen und die
WinForms-Fassungen im selben Schritt löschen.

* **Merge-Base:** `71cde0c` (der Remote-Elter von Merge 2 — unser Strang ist seither nicht
  neu verzweigt)
* **Lokal vorher:** `533eb7b` (18 Commits: Paket A/B, Projektdialoge, FS1, Merge-1- und
  Merge-2-Nachweis)
* **Remote:** `908926a` (`0d92c89`…`908926a` plus `9e4fa37`, 59 Dateien, +5 477 / −2 538)
* **Merge-Commit:** `359b1cd` — „Merge 3 origin/ios_migration (iU9 Welle 1 Blazor-Dialoge):
  Paket A/B, Projektdialoge, FS1 nachgezogen"
* **Sicherungsreferenz:** Branch `sicherung/vor-merge3-2026-09-03` auf `533eb7b`
* **Nachweisanker:** Branch `merge3/ios-2026-09-03`
* Kein Push.

Der Arbeitsbaum war vor dem Merge sauber; es gab nichts vorab festzuhalten.

---

## 1. Was Welle 1 portiert hat — und wo unsere Andockungen liegen

Sieben Masken, sechs Komponenten (`Form_VariantenName` und `Form_KostenItemNeu` gehen in
**einer** Namensabfrage auf). Die Tabelle nennt zu jedem Port, **welche unserer
Andockungen** an derselben Stelle sitzt.

| # | gelöschte Maske | neue Komponente | Aufrufer nach dem Umbau | unsere Andockung dort |
|---|---|---|---|---|
| W1.1 | `Form_VorlagenPosition` | `EPOS.UI/Dialoge/Kosten/VorlagenPositionDialog.razor` | `Form_KostenKomponente.cs` (`Zeile_EditorAngefordert`) | keine |
| W1.2 | `Form_VariantenName` + `Form_KostenItemNeu` | `EPOS.UI/Dialoge/Allgemein/NamensDialog.razor` | `Form_KostenKomponente.cs`, `Form_Energietraeger.cs` | keine |
| W1.3 | `Form_CaseEingabe` | `EPOS.UI/Dialoge/Kosten/CaseEingabeDialog.razor` | `Form_KostenKomponente.cs`, `ucKostenItem.cs` | keine |
| W1.4 | `Form_VorlagenUebernahme` | `EPOS.UI/Dialoge/Kosten/VorlagenUebernahmeDialog.razor` | `Form_KostenKomponente.cs` | keine |
| W1.5 | `Form_KostenAdmin` | `EPOS.UI/Dialoge/Kosten/KostenfaktorKatalogDialog.razor` | `Form_KostenKomponente.cs` | keine |
| W1.6 | `Form_WirtschaftlichkeitVerlauf` | `EPOS.UI/Dialoge/Wirtschaftlichkeit/KapitalwertVerlaufDialog.razor` | **`UcWirtschaftlichkeit.cs:491`** (`btnVerlauf_Click`) | **derselbe Steuerbaustein** trägt unseren PV-Knopf (Zeile 236/238) — siehe 1.1 |

Dazu ein neuer Baustein (`EPOS.UI/Bausteine/Optionsgruppe.razor`), ein neuer Kern-Controller
(`EPOS.Kern/Controller/KostenfaktorCtrl.cs` — die drei SQL-Anweisungen aus `Form_KostenAdmin`
zeichengleich mit `DbParam`), drei Hüllen, ein Windows-Helfer (`NamensDialogHuelle`), der
Ressourcen-Sammelnachtrag (43 Schlüssel de **und** en) und die Formularkarte-Tests.

**Keine der sieben gelöschten Masken stand in unserer Änderungsmenge.** Es ging also nichts
von uns mit unter.

### 1.1 Die eine Stelle, an der sich beide Seiten begegnen: `UcWirtschaftlichkeit`

Remote hat in dieser Datei **neun Zeilen** geändert — den Aufruf des Verlaufsdialogs
(`new Form_WirtschaftlichkeitVerlauf(...)` → `KapitalwertVerlaufHuelle.Oeffnen(...)`).
Unser PV-Knopf (`btnPhotovoltaik`, ETAPPE P5) liegt in derselben Datei, aber **250 Zeilen
davor** und stammt aus einem Commit, der schon in `71cde0c` steckt; unsere 18 Commits fassen
die Datei **nicht** an. Git nimmt darum schlicht die Remote-Fassung — und die enthält den
PV-Knopf unverändert. Nachgemessen im Merge-Stand: **8 Vorkommen `btnPhotovoltaik`**,
`new Form_PhotovoltaikVerguetung()` in Zeile 238.

**Fachlich wichtiger als die Textstelle:** Der neue Verlaufsdialog rechnet über **denselben
Weg** wie sein Vorläufer — `new BerichtsDatenSammler().Sammle(...)`, dann
`WirtschaftlichkeitCtrl.BerechneVerlauf`. Unsere Erlösreihe `PV_VERGUETUNG`
(`WirtschaftlichkeitCtrl.cs:1889`, gespeist aus `PvErloesRechner.Rechne`) sitzt **innerhalb**
dieses Weges. Weder die alte Maske noch die neue Komponente nennt PV an irgendeiner Stelle
selbst — der Port ist gegenüber unserer Andockung **neutral**. `WirtschaftlichkeitCtrl.cs`
und `PvErloesRechner.cs` hat Welle 1 nicht angefasst.

### 1.2 Die beiden anderen genannten Andockungen

* **Degradationsfeld in `Form_PhotovoltaikVerguetung`** (Stufe E2.4): Die Maske steht nicht
  in Remotes Änderungsmenge. Im Merge-Stand unverändert vorhanden — `DegradationsfeldAnlegen()`,
  `numDegradation`, Text über `PVM_DEGRADATION`, Hilfetext über `PVM_DEGRADATION_TIP`.
* **`KostenProjektPositionenCtrl.AnkerNachziehen`** — der Doppel-Fix, den Merge 2 mit
  **unserer** Fassung aufgelöst hat: **Welle 1 fasst die Datei nicht an**
  (`git diff HEAD` auf diese Datei ist leer). Unsere Fassung steht unverändert, samt beider
  Herleitungen — `SELECT 1` im EXISTS, Hinweis auf Schritt 47 im eingefrorenen Access-Zweig
  und Remotes zweiter Fundort („schon das ANLEGEN eines Heizkessels löste ihn aus").

---

## 2. Berührungsfläche und Auflösung

Unsere 18 Commits ändern **66** Dateien (ohne `Referenzlaeufe/`), die elf Remote-Commits
**59**. **Vier Dateien liegen in beiden Mengen — und alle vier gingen ohne Konflikt
zusammen.**

| Datei | Auflösung |
|---|---|
| `EPOS.Kern/MyResource/Resource.resx` | automatisch, Vereinigung geprüft |
| `EPOS.Kern/MyResource/Resource.en-US.resx` | automatisch, Vereinigung geprüft |
| `EPOS.Kern/MyResource/Resource.Designer.cs` | automatisch, auf Dubletten geprüft |
| `WindowsFormsApplication1/Allgemein/KI/HilfeKontext.cs` | automatisch (unsere PV-Einträge bleiben, Remotes sieben Einträge der gelöschten Masken fallen weg) |

**Es gab keinen Konfliktmarker aufzulösen.** `P:\merge3\aufloesungen\` ist deshalb leer —
das ist der Befund, nicht ein Versäumnis. Die Markersuche über den ganzen Baum
(`^<<<<<<<`, `^=======`, `^>>>>>>>` in `.cs`, `.resx`, `.razor`, `.md`, `.csproj`, `.sln`,
`.txt`, `.css`) hat genau **einen** Treffer: eine Zeile aus Gleichheitszeichen in
`VDI-3805-Daten/KWK-Daten/2G Energy/_Anfrage_2G_avus_Datenblaetter.txt` — eine Trennlinie im
Bestand, von keiner Seite berührt. `MERGE_HEAD` stand vor dem Commit auf `908926a`; der
Merge-Commit trägt beide Eltern (`533eb7b`, `908926a`).

Der **Umfang** der Zusammenführung ist gegengemessen: `git diff --cached HEAD` vor dem Commit
nennt **59 Dateien, +5 477 / −2 538** — Zeichen für Zeichen die Zahlen von
`git diff 71cde0c origin/ios_migration`. Unsere Seite ist also unangetastet geblieben, und
Remotes Seite ist vollständig angekommen.

### 2.1 Was automatisch zusammenging — und wie es nachgewiesen ist

Für jede der vier Dateien wurde gegengeprüft, dass **beide** Seiten wirklich angekommen sind:
jede in `71cde0c..533eb7b` bzw. `71cde0c..origin/ios_migration` **hinzugefügte** Zeile steht
im Merge, jede **gelöschte** ist verschwunden.

| Datei | unsere +/− | Remote +/− | fehlend / zurückgeblieben |
|---|---|---|---|
| `Resource.resx` | 125 / 0 | 87 / 0 | 0 / 0 |
| `Resource.en-US.resx` | 125 / 0 | 87 / 0 | 0 / 0 |
| `Resource.Designer.cs` | 124 / 0 | 135 / 0 | 0 / 0 |
| `HilfeKontext.cs` | 4 / 0 | 0 / 7 | 0 / 0 |

### 2.2 Ressourcen — die Vereinigung ist gezählt, nicht geglaubt

Wie bei Merge 2 legte Git beide Blöcke ohne Konflikt zusammen; die Merge-1-Stolperstelle
(fehlendes `</data>` an der Naht) konnte nicht auftreten. Trotzdem geprüft, in **beiden**
Sprachen:

* **XML wohlgeformt** (`[xml]`-Ladeprobe): ja, je **2 944** `data`-Elemente.
* **Keine Dublette:** 2 944 Elemente, 2 944 eindeutige Namen — in de **und** en.
* **de und en führen dieselbe Schlüsselmenge** (Differenzmenge beidseitig leer).
* **Remotes 43 neue Schlüssel** (`VPOS_*` 7, `NAMD_*` 1, `KCASE_*` 8, `KUEB_*` 5, `KFAK_*` 11,
  `WVERL_*` 11) sind **alle 43 in beiden Sprachen** vorhanden.
* **Unsere Schlüssel** sind vollzählig: `PV_ANLAGE_*` 4, `PV_MODUL_*` 3, `PVM_*` 29,
  `SIM_KARTE_PV_MODELL_*` 2, `PVW_*` 63, `PDLG_*` 13, `PA_*` 2, `WZP_*` 5.

Die Zahl geht auch in der Bilanz auf: Merge 2 stand bei **2 901**, Remote legte **43** nach —
**2 944**. `Resource.Designer.cs` führt **2 819** Eigenschaften, **alle eindeutig**; die von
Remote von Hand ergänzten Einträge erzeugen also kein CS0102.

---

## 3. Nachweis

### 3.1 Build — viermal, mit demselben Werkzeug

MSBuild aus VS 18 Community, `-restore /p:Platform=x64`, `WP-Plan.sln`.

| Stand | Ergebnis |
|---|---|
| Merge, Worktree `P:\wt3` | **0 Fehler**, 34 Warnungen |
| Merge, `git archive HEAD` → `P:\merge3\src` | **0 Fehler**, 34 Warnungen |
| `origin/ios_migration` pur (`908926a`), `git archive` → `P:\merge3\theirs` | **0 Fehler**, 34 Warnungen |
| Hauptbaum nach der Übernahme (`bin\x64\Debug`) | **0 Fehler**, 34 Warnungen |

**Warnungsprofil in allen vier Läufen identisch:** WFO1000 24, NU1510 4, CS0109 2, CS0108 2,
WFO0003 1, CA2255 1. Schärfer: Die **29** datei- und zeilengenauen Meldungen von Merge,
reinem `origin/ios_migration` und Hauptbaum sind Zeile für Zeile dieselben — und
**zeilengleich zu Merge 2**. Welle 1 hat also weder eine Warnung hinzugefügt noch eine
beseitigt, und **aus unseren Dateien kommt keine einzige neue Warnung**.

> Bemerkenswert bei 59 geänderten Dateien und sieben gelöschten Masken: Die gelöschten
> WinForms-Masken trugen keine der 29 Meldungen. Die WFO1000-Zählung bleibt bei 24.

### 3.2 Referenzlauf — der Dreifach-Nachweis

Werkzeug `Referenzlauf/`, 14 Projekte
(1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, 1026, 1028, 1029, 1030, 1039, 1043),
Quelle `P:\pa0\Quelle\Kenndaten.sqlite` (MD5 `47bcefaca0f18d2180ba37786c6cb6b3`, vor dem Lauf
gemessen), je **frische** Arbeitskopie; die Arbeitskopie des Merge-Laufs migriert 61 → 63,
die des THEIRS-Laufs bleibt auf 61 (Remote steht weiter auf `ZIEL_VERSION = 61`).

| Vergleich | Ergebnis |
|---|---|
| **MERGE (M3) gegen M2** (`2026-09-03_M3_nach-Merge3` ↔ `2026-09-03_M2_nach-Merge2`) | **355/355 bitgleich** (MD5), 0 ungleich, keine Datei nur auf einer Seite |
| **THEIRS (`908926a`) gegen THEIRS (`71cde0c`)** (`P:\merge3\theirs_lauf` ↔ `P:\merge2\theirs_lauf`) | **355/355 bitgleich** |
| Toleranzvergleich M2 → M3 | **14/14 PASS**, 3 882 476 Werte |
| `pruefen` auf M3 | **plausibel**, dieselben Bestandshinweise wie M1/M2 |

**Einordnungstabelle (Datei | M2=MERGE? | THEIRS abweichend? | Einordnung): leer.**
Es gibt **keine** Abweichung MERGE ≠ M2, also nichts einzuordnen.

Der zweite Vergleich sagt hier, warum das kein Zufall ist: Welle 1 hat **Oberfläche**
ausgetauscht — Masken gegen Razor-Komponenten —, und der einzige neue **Kern**-Baustein
(`KostenfaktorCtrl`) trägt die drei SQL-Anweisungen aus `Form_KostenAdmin` zeichengleich
weiter. Der reine Remote-Stand rechnet deshalb byte-für-byte wie vor der Welle. Und weil
zugleich MERGE = M2 exakt gilt, hat auch die Zusammenführung nichts verschoben. **Beide
Achsen sind exakt.**

Der Lauf selbst: 14 von 14 erfolgreich, 26 Warnungen, 0 Fehler — dieselben Zahlen wie M1
und M2.

### 3.3 Harness Paket A und B (gegen den Merge-Build)

| Probe | Ergebnis |
|---|---|
| `rein` — Zeitbasis (PA1) | **18 PASS, 0 FAIL** |
| `rein` — Modell (PB1): Huld, Hay-Davies, Kennlinie, Clipping, Degradation, Technologie | **58 PASS, 0 FAIL** |
| `zeitbasis` an der DB-Kopie (14 Klimaregionen) | **115 PASS, 0 FAIL** |
| `migration` 61 → 62 → 63 auf frischer Kopie, Zweitlauf idempotent, kein DML | **24 PASS, 0 FAIL** |
| INEKON „Schulung 01", Prüfstand `kd1runner` Modus `pv6` | **28 PASS, 0 FAIL** (I3 −0,76 %, I4 −0,47 %) |

Zusammen **243 PASS, 0 FAIL** — Probe für Probe dieselben Zahlen wie bei Merge 1 und 2, samt
der INEKON-Abstände auf zwei Nachkommastellen.

### 3.4 Die beiden ausdrücklich verlangten Einzelproben

Weil Welle 1 die Wirtschaftlichkeits-Dialoge portiert, wurden zwei Aussagen **headless am
Merge-Build** eigens nachgemessen:

* **Degradation.** `PvErloesRechner.DegradationsFaktor(0.5, 20)` liefert **0.909156**, also
  die Konzeptangabe **0,9092**, und ist bitgenau gleich `(1 − 0,005)^19` (Abweichung
  < 1e-15). Die Probe steht als `g3` in `ModellProbe.Degradation()` und ruft die Methode
  **direkt** auf — kein Umweg über eine Maske. Dazu `g1` (d = 0 → Faktor 1 in jedem Jahr,
  exakt), `g2` (Jahr 1 immer 1, Exponent t−1), `g4` (monoton fallend), `g5` (negative
  Eingabe ignoriert), `g6` (d = 100 %/a → Jahr 2 ist 0).
* **PV-Wirtschaftlichkeit.** `pv6` gegen die INEKON-Referenz „Schulung 01" trifft weiterhin
  **I3 −0,76 %** (Überschuss, 91 867 gegen 92 568) und **I4 −0,47 %** (Volleinspeisung,
  −23 087 gegen −22 979), beide innerhalb ±1 %. Die Rohabstände (−3,44 % / −2,76 %) sind wie
  gehabt reine Konventionsdifferenz (I5 < 4 %). Ebenfalls grün: der ganze Rückladepfad
  P0–P5, der den PV-Block über den DB-Roundtrip in Reiter und Bericht bringt.

Die Harness-Vorlagen mussten **gar nicht** angepasst werden; es genügte, die drei
Merge-1-Anpassungen (Referenz auf `EPOS.Kern.dll`, `DataRepository.PfadUeberschreibung`,
`SolardatenCtrl` über `typeof(DataRepository).Assembly`) mitzunehmen und den Pfad
`P:\merge2\src` auf `P:\merge3\src` zu ziehen.

> **Fallstrick, unverändert gültig.** Der Modus `pv6` **migriert nicht**; er setzt nur
> `DataRepository.PfadUeberschreibung`. Auf einer Kopie im Schemastand **61** meldet er
> „no such column: Degradation" und liefert 24 PASS / 4 FAIL — Folgefehler eines
> fehlgeschlagenen `Speichern`, **kein** Befund am Code. Hier wurde deshalb die von der
> Migrationsprobe auf **63** gebrachte Kopie verwendet.

Die produktive Datenbank `C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite` blieb unberührt
(Zeitstempel **02.09.2026 22:07:36**, vorher wie nachher gemessen).

### 3.5 Übernahme in den Hauptbaum

Der Hauptbaum stand unverändert auf `533eb7b` und war sauber; die Übernahme lief deshalb als
**`git merge --ff-only merge3/ios-2026-09-03`** — ein reines Vorspulen, kein zweiter
Merge-Commit. Der Worktree `P:\wt3` ist entfernt, der Branch bleibt als Anker.
`core.longpaths` war nur für `git worktree add` **und den Merge selbst** auf dem
subst-Pfad gesetzt und ist wieder entfernt.

> **Nachtrag zum Fallstrick aus Merge 1/2:** `core.longpaths=true` reicht nicht, wenn man es
> nur für `git worktree add` setzt. Der anschließende `git merge` liest den Baum erneut ein
> (`read-tree`) und scheitert sonst an `VDI-3805-Daten/…` mit „Filename too long" — mit
> einem `fatal: read-tree failed`, das wie ein Merge-Fehler aussieht, aber keiner ist. Der
> Schalter muss bis **nach** dem Merge stehen bleiben.

---

## 4. Offene Punkte

* **Sichtabnahme am Programm** steht aus. **Neu hinzugekommen:** die sieben Dialoge der
  Welle 1. Remote führt dafür in
  [`iU9_W1_Blazor_Port_Protokoll.md`](../Reporting/iU9_W1_Blazor_Port_Protokoll.md) eine
  eigene Abnahmeliste (neun Oberflächenpunkte × sechs Dialoge, dazu die fachlichen Proben
  F‑1 bis F‑7). Weg dorthin: **Menü → Kostenvorlagen → Kostenverwaltung** für W1.1–W1.5,
  **Berichte & Kosten → Wirtschaftlichkeit → Verlauf** für W1.6, **Menü → Energieträger →
  „Neu…"** für den zweiten Aufrufer der Namensabfrage.
* **Unsere eigene Zusatzprobe zu W1.6:** Im neuen Kapitalwert-Verlauf muss die
  **PV-Erlösreihe** weiter im Ergebnis stehen — also: PV-Vergütungsdialog aktivieren,
  simulieren, Verlauf öffnen und nachsehen, dass die Kurve dieselbe ist wie vor dem Port.
  Rechnerisch ist der Weg identisch (siehe 1.1); gesehen hat es noch niemand.
* **Aus Merge 2 unverändert offen:** die Blazor-Dialoge aus iU9 (Heizkessel/BHKW →
  „Energieträger-Variante"), der Dialog BHKW-Wirtschaftlichkeit (B5), FX2–FX5; die vier vom
  Remote selbst benannten Pfade (Kosten → Position mit **neuer** Gruppe; Preisreihen auf
  einer DB **ohne** `Tab_PreisreiheDaten`; Klimadaten → Region löschen; Wärmepumpe-Test →
  „Kühlung"). **Aus Merge 1:** Löschdialog über `Dienste.Navigation` → `Projektloeschwahl`,
  die PV-Masken `Form_PV`, `Form_PVModell`, `Form_AdminPV`.
* **Vom Remote selbst als offen gekennzeichnet** (Welle 1, W1‑O1 … W1‑O7): der ersatzlos
  entfallene Zuschuss-Schalter im Projektraster (A‑6), das Offenbleiben des
  Übernahme-Dialogs (A‑10), die fehlende Fortschrittsanzeige im Verlaufsdialog (A‑17),
  `SelectAll()` in der Namensabfrage (A‑3), das Löschen gleichnamiger Kostenfaktoren über
  die `StammID` (A‑15), die Persistenzwerte als Anzeigetext in der Szenarioliste,
  `NamensDialogHuelle` vor Welle 2. Diese Punkte gehören dem Remote-Strang; sie sind hier
  nur genannt, damit sie bei der Abnahme nicht als unser Befund missverstanden werden.
* **Nebenbefund am Prüfwerkzeug, unverändert:** `kd1runner` **ohne** Modusargument stirbt
  seit dem Umzug an einer `NullReferenceException` (er sucht
  `WindowsFormsApplication1.Properties.Settings` per Reflexion in der Assembly von
  `SchemaMigration`; die Einstellungen sind mit dem Kern gewandert). Der Modus `pv6` ist
  nicht betroffen. Reparatur auf Zuruf.
* **Kein Push.** `sicherung/vor-merge3-2026-09-03` und `merge3/ios-2026-09-03` sind lokale
  Anker und können nach der Abnahme entfallen; die Anker der Merges 1 und 2 stehen ebenfalls
  noch.
* Schemastand bleibt **63**; `.wpx`-Pakete mit Stand 62 werden abgewiesen — systemimmanent.
* `sql/pv_katalog/` (Reparaturskript Isc-Signatur) ist weiterhin **nur mitgeführt**, nicht
  ausgeführt; die Freigabe steht aus.
* Aus dem Bestand unverändert offen: Bestätigung der Degradation „vermiedener Bezug",
  Katalogpflege Technologie/T_NOCT, E3 zurückgestellt.

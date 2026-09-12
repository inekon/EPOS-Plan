# iU9 Welle 16c — Das Hauptfenster: MDIMainForm, das Menü, der Rahmen — Portprotokoll

> Teilwelle **W16c** des Pakets iU9 (Welle 16 = der Rahmen K5 in drei Teilwellen)
> und die **letzte** der drei. Grundlage: `iU9_W16_Vermessung.md` (1 907 Zeilen,
> Stand `4101740`) und die Arbeitsanweisung `iU9_W16_Arbeitsanweisung.md`,
> Abschnitt „W16c — Das Hauptfenster".
> Basis: `c8fbd77` (Statusblock W16b).
> **Mit dieser Teilwelle endet die Mischphase** (Umsetzungskonzept M9).

---

## 0 — Was die Teilwelle getan hat

**Das Hauptfenster ist Razor, und `WindowsFormsApplication1` führt keine
Fachmaske mehr.**

| Bauteil | vorher | nachher |
|---|---|---|
| `MDIMainForm.cs` | **873** Z. | **129** Z. — die Hülle |
| `MDIMainForm.Designer.cs` | 493 Z. (45 `ToolStripMenuItem`, 6 `ToolStripSeparator`) | **gelöscht**, eingefroren im Prüfmuster |
| `MDIMainForm.resx` / `.de-DE` / `.en-US` | 1 729 / 1 086 / 1 185 Z. | **gelöscht**, eingefroren im Prüfmuster |
| `Controller/MenueCtrl.cs` | 347 Z., 26 öffentliche Methoden | **257** Z., **6** Methoden |
| `Dienste/WinFormsNavigation.cs` | 269 Z. | **256** Z. (`MitOk`, `WahlUebernehmen` gefallen) |
| `Allgemein/Blazor/BlazorDialogForm.cs` | 386 Z. (mit `DpiInsel`) | **301** Z. |

Neu in `EPOS.UI`: der Baustein **`Menueband.razor`** (300 Z.) mit
**`Menuepunkt.cs`** (115 Z.) und der **erzeugten** **`Menuetabelle.cs`**
(54 Punkte; seit W16c‑E‑2 **55**), die Seite **`Hauptfenster.razor`** und die
elf Menübildchen unter `wwwroot/bilder/menue/`. Erweitert: **`Seitenschluessel`**
(86 → 319 Z., die EINE Schlüsseltabelle beider Plattformen) und **`AppWurzel`**
(354 → 473 Z., die gemeinsame Wurzel).
Neu in der Anwendung: **`Views/Hauptformular/HauptfensterHuelle.cs`** (309 Z.).

**`WindowsFormsApplication1` führt seither EINE Maske** — `Form_HelpPopup`, die
Hilfe-Sprechblase ohne Fachinhalt (bleibt bis iU11, Entscheid W15b‑E‑2) — und
die Hülle `MDIMainForm` **ohne Designer**. Der einzige bewusst gebliebene
Übergang nach WinForms ist die `Sprungbruecke` mit **einem** Zweig
(`Form_SpeicherOptimierung`, iF22).

---

## 1 — Commits

| # | Commit | Inhalt |
|---|---|---|
| W16c.5a | `915e0a7` | `MDIMainForm.Designer.cs` und die drei `.resx` als Prüfmuster eingefroren (E‑9) — **vor jeder Änderung** |
| W16c.0 | `f93a50f` | **K7 / E‑1 / E‑2**: `Seitenschluessel` wird die EINE Schlüsseltabelle beider Plattformen |
| W16c.1 | `9e1f76b` | Der Baustein `Menueband` und die **erzeugte** Menütabelle (R‑W16‑8), Nachweis **N4** |
| W16c.2 | `0ff1ef4` | Die Seite `Hauptfenster` (S2) und `AppWurzel` als gemeinsame Wurzel (E‑1) |
| W16c.3/.5 | `3409c80` | **Der Rückbau** auf die Hülle; die Zeugen und Schwellen (N1/N2), der zurückgeholte Maskenschlüssel-Zeuge |
| W16c.4 | `ddfab40` | **E‑6 / iF21**: Per Monitor V2, die `DpiInsel` fällt |
| W16c.6 | `52e1e46` | Die Fensterhilfe (W16b‑O‑4), `HilfeKontext`, `help_mapping.txt`, vier `CLAUDE.md`, Nachweise, M9 |
| W16c.7 | `03c5947` | **W16c‑E‑2**: Die zwei Sprachpunkte wandern in ein Untermenü „Sprache" |
| W16c.8 | `74e0cc1` | **W16c‑E‑3**: „Varianten und Bericht…" wechselt die ANSICHT |
| **E‑10** | `7ed320b` | **Nach dem Merge:** `MDIMainForm` → `Hauptfensterrahmen` (Klasse, Datei, `Program.rahmen`, `Erreichbarkeit.Wurzelmasken`, `HilfeKontext`, die zwei Prüfmuster-Auszüge, die Kommentare) |
| **B‑12** | `73b6e58` | **Der Startabsturz:** `Hauptfenster` bekommt den `[Parameter] Zustand`, den `BlazorSeite<T>` jedem Parametersatz beilegt; dazu die Reflexionswache in `BlazorSeite` und fünf bunit-Fälle, die über den Weg der Hülle rendern |
| **B‑13** | `3d1a0ca` | **Die Untermenüs klappten nicht auf:** `@onfocusout` fällt, der Offen-Zustand wird ein PFAD über alle drei Ebenen, die Schließfläche fängt den Klick daneben, die Tastatur bekommt → ← ↑ ↓ in der offenen Klappe |
| **B‑13** | `e467635` | Die **14 Wachen** dazu in `MenuebandTests` und `HauptfensterTests` — beide über `AddMultipleAttributes` mit der echten `Menuetabelle` |

> **W16c.3 und W16c.5 sind EIN Commit** — dieselbe Begründung wie bei
> W16b.3/.5 im Protokoll der Vorwelle: Der Rückbau löscht
> `MDIMainForm.Designer.cs`, und im selben Augenblick verlieren sechs Testanker
> der Formularkarte ihren Gegenstand (Großschreibungs-Zeuge, `Masken >= 2`,
> `Lokalisierte >= 1`, die Übersichtszeile, der Wurzelnamensraum und
> `Erreichbar(Ja) >= 2`). Dazwischen läge ein Stand, der übersetzt und rot ist.
> Der REST von W16c.5 — das Einfrieren — steht als eigener Commit **davor**, wie
> die Anweisung es verlangt.

---

## 2 — Was vor der Teilwelle nachgemessen wurde

| Maß | Sollwert der Anweisung | Gemessen (Basis `c8fbd77`) |
|---|---|---|
| Stapellauf `--alle WindowsFormsApplication1` | 2 Masken / 3 Designer, 2 / 0 / 0 / 0 | **erfüllt** (1 lokalisiert; 45 `ToolStripMenuItem`, 6 `ToolStripSeparator`) |
| `dotnet test WP-Plan.Kern.slnf` | 3 968 | **3 968** (450 / 337 / 2 160 / 1 021) |
| `dotnet test Werkzeuge/Formularkarte.Tests` | 121 | **121** |
| Build-Warnungen | 6 | **6** |
| `ChartProben` | 32 | **32** |
| SQL-Texte | 1 200 | **1 200**, 0 Fundstellen |

**`git grep` vor dem Rückbau** (Auflage der Anweisung): Die acht `Init*`, der
`MenuStrip` und **jeder** der 34 `MenuItem_*`-Handler haben außerhalb von
`MDIMainForm.cs` **keine** Codestelle — nur Kommentare in vier Hüllen. Von den
26 `MenueCtrl`-Methoden haben nach dem Rückbau noch **sechs** einen Aufrufer
(`StartseiteHuelle` vier, `AssistentHuelle` einen, die neue
`HauptfensterHuelle` vier — mit Überschneidung). `MDIMainForm.PRODUKTNAME` hat
**einen** Leser außerhalb (`LizenzVerwaltungHuelle.cs:71`) und bleibt deshalb
als Konstante an der Hülle stehen.

---

## 3 — Feldkartenabgleich `MDIMainForm` (1 Kartenzeile)

Die Feldkarte hilft beim Hauptfenster **nicht** — das hat schon § 11.4 der
Vermessung festgehalten: Das Werkzeug kennt `MenuStrip`/`ToolStripMenuItem` als
„prüfen" und listet sie nicht als Felder. Neu gezogen (jetzt gegen das
eingefrorene Prüfmuster) liefert sie unverändert:

| Angabe | Wert |
|---|---|
| Zeilen der Karte | **1** (`label_OnlineDoku`, die Ladeanzeige) |
| Steuerelemente | `ToolStripMenuItem` **45**, `ToolStripSeparator` **6**, `Label` 1, `MenuStrip` 1 |
| Titel de / en | „WP‑Plan" / — (Befund W16‑B22: der alte Produktname; `InitMarke` überschrieb ihn zur Laufzeit) |
| Lokalisiert | ja (`ApplyResources`) |
| Fensterereignisse | `Load -> MDIMainForm_Load` |

**Der Abgleich der Welle ist deshalb die MENÜTABELLE**, nicht die Feldkarte —
und sie ist erzeugt, nicht abgetippt (§ 4).

---

## 4 — Die Menütabelle: erzeugt, nicht abgetippt (R‑W16‑8)

Das Skript der Teilwelle heißt **`w16c_menue.py`**. Es liest

* `MDIMainForm.Designer.cs` — Baum (`DropDownItems.AddRange`), Reihenfolge,
  Trenner, `Click`-Handler, `Image`-Zuweisungen;
* `MDIMainForm.resx` / `.de-DE.resx` / `.en-US.resx` — die Beschriftungen

und schreibt daraus `EPOS.UI/Bausteine/Menuetabelle.cs` sowie die **46 neuen
`MENU_*`-Schlüssel** in `Resource.resx` und `Resource.en-US.resx`. Die neun
programmatischen Punkte der acht `Init*`-Methoden stehen als Datentabelle im
Skript, samt ihrer Einhängestelle („unmittelbar unter `Einstellungen`",
„unmittelbar unter `Stromspeicher`") — und mit ihren **vorhandenen**
`MyResource`-Schlüsseln.

| Gruppe | Zahl | Bemerkung |
|---|---|---|
| aus dem Designer | **45** | `MENU_*`, mechanisch aus dem Steuerelementnamen (`MenuItem_zuletztGeöffnet` → `MENU_ZULETZT_GEOEFFNET`) |
| aus den acht `Init*` | **9** | wörtlich die alten Schlüssel: `PEAK_MENUE`, `KDLG_MENUE_VORLAGEN`, `KDLG_MENUE_ENERGIETRAEGER`, `GESETZ_MENUE`, `ADM_DUBLETTEN_MENUE`, `KI_MENUE_ASSISTENT`, `MENU_VARIANTE_SPEICHERN`, `MENU_VARIANTEN_BERICHT` — dazu **ein** neuer (`MENU_LIZENZ_VERWALTUNG`, im Bestand ein Code-Literal) |
| Trennstriche | **8** | 6 aus dem Designer, 2 aus `BaueVariantenMenue` und `InitKiHilfe` |
| Bilder | **11** | 9 aus dem Designer, 2 aus `InitGesetzeMenue`/`InitLizenzMenue` — dieselben PNG unter `EPOS.UI/wwwroot/bilder/menue/` (zusammen 30 KB) |
| **nachgetragen (W16c‑E‑2)** | **1** | der Kopf **„Sprache"** (`MENU_SPRACHE`, en „Language"). Er hat **keine Designer-Herkunft** — er ist der Anwenderentscheid vom 04.09.2026; die Tabelle sagt das im Kopfkommentar, denn das Erzeugerskript liegt nicht im Repository |
| **nachgetragen (W16c‑E‑6)** | **1 − 2** | die Unterrubrik **„Profile & Lastgänge"** (`MENU_PROFILE_LASTGAENGE`, en „Profiles & load curves") kommt hinzu, ebenfalls **ohne Designer-Herkunft**; dafür fallen `MenuItem_PC_Bearbeiten` und `MenuItem_ST_Bearbeiten` weg — die einzigen Kinder ihrer Untermenüs (06.09.2026, Abschnitt am Ende dieses Protokolls) |

Damit führte die Tabelle beim Abschluss der Teilwelle **55 Punkte** (54 des
Bestands + 1) in **vier Köpfen** der obersten Ebene — Projekt, Administration,
Hilfe, Sprache. Von den 55 **handelten 42** (unverändert: der neue Kopf handelt
nicht) und **13 klappten auf** (12 + „Sprache"); Trennstriche und Bilder sind
unverändert 8 und 11. Der Menüpunkt „Varianten und Bericht…" meldet weiterhin
`BERICHTE_KOSTEN` — was sich mit W16c‑E‑3 geändert hat, ist nicht die Tabelle,
sondern was die Hülle damit tut (§ 6).

> **Nachtrag 06.09.2026 (W16c‑E‑6).** Die Umordnung des Kopfes
> „Administration" hat aus den 55 Punkten **54** gemacht und aus den 13
> aufklappenden **12**: Eine Unterrubrik kommt hinzu, zwei Untermenüs mit
> nur einem Punkt „Bearbeiten" fallen weg. **Die 42 handelnden Punkte sind
> geblieben** — das ist die Zahl, an der die Vollzähligkeit hängt. Der
> Abschnitt am Ende dieses Protokolls sagt es im Einzelnen.

Ein zweites kleines Skript, **`w16c_texte.py`**, legt die vier Texte des
Kopfbands und der „Über"-Meldung an (`HAUPT_CLAIM`, `HAUPT_VERSION`,
`HAUPT_UEBER_TITEL`, `HAUPT_UEBER_HAUS`); ein drittes, **`w16c_designer.py`**,
trägt die 50 neuen Schlüssel in `Resource.Designer.cs` nach — Visual Studio
erzeugt die Datei sonst selbst, auf Linux gibt es den Designer nicht.

---

## 5 — Die Angleichungen (A‑1 … A‑9)

| # | Was | Warum |
|---|---|---|
| **A‑1** | **Das Menü hat DREI Ebenen und klappt per Klick, nicht per Überfahren.** Ein `MenuStrip` öffnet ein Untermenü, sobald die Maus darüber steht; das Menüband öffnet es beim Klick und schließt es bei Esc, bei der Wahl und beim Fokusverlust | Ein Aufklappen beim Überfahren ist in einer WebView nicht zuverlässig nachzubauen (kein `Capture`, kein Verzögerungsverhalten des Systems) — und es ist auf Touch (iL5) falsch. Die Tastatur bedient beides gleich: ← → wandern, ↓ öffnet, Esc schließt |
| **A‑2** | **„Daten && Import" heißt jetzt „Daten & Import"** — ebenso „Wärmebedarf & Heizung", „Strombedarf & Speicher", „Klimadaten & Umgebung" | WinForms verdoppelt `&` für das Tastenkürzel; in Razor gibt es diese Verdopplung nicht. Dieselbe Angleichung wie bei „Berichte && Kosten" der Startseite (W16b) |
| **A‑3** | **Die Ladeanzeige `label_OnlineDoku` entfällt ersatzlos.** Sie stand zentriert im Fenster, während `HelpCatalog.LoadAllAsync` anlief | Der Aufruf wird **nicht** abgewartet (`_ =`); die Anzeige stand deshalb zwischen zwei Zeilen und war nie zu lesen. Die WebView bringt ihre eigene Themafläche mit, bevor sie zeichnet |
| **A‑4** | **Die Titelzeile heißt von Anfang an „EPOS-Plan".** `MDIMainForm.de-DE.resx` trug „WP‑Plan", und `InitMarke` überschrieb es zur Laufzeit (Befund W16‑B22) | Der Widerspruch fällt mit dem Designer weg — die Hülle setzt `Text = PRODUKTNAME` im Konstruktor |
| **A‑5** | **Die „Über"-Meldung läuft über `Dienste.Dialog`.** Ihr Titel war „Über " + PRODUKTNAME (deutsches Literal), ihre Schlusszeile „INEKON - Intelligente Energiekonzepte" ebenso | Beide stehen jetzt im Katalog (`HAUPT_UEBER_TITEL` mit `{0}`, `HAUPT_UEBER_HAUS`). Aussehen und Wortlaut sind unverändert; die **einzige** `MessageBox` des Hauptfensters ist damit weg |
| **A‑6** | **Der Browserstart läuft über `Dienste.Datei.AdresseOeffnen`** statt über ein unmittelbares `Process.Start` | Die letzte solche Zeile des Hauptfensters. `MitSystemOeffnen` taugte nicht — es prüft `File.Exists` und liefert für eine Adresse immer `false` (Befund W16c‑B8). Der Fehlerfall bleibt folgenlos, wie beim Vorläufer |
| **A‑7** | **Die 21 einzeiligen `MenueCtrl`-Methoden entfallen.** Ein Menüpunkt nennt jetzt seinen Maskenschlüssel selbst, und `HauptfensterHuelle.Weg` reicht ihn an `Dienste.Navigation` | Eine Methode, die nichts tut, als einen Schlüssel weiterzugeben, ist nach dem Umbau eine Zwischenstufe ohne Aufgabe. Die sechs zusammengesetzten Abläufe bleiben |
| **A‑8** | **Die Fensterhilfe sitzt im Kopfband** (`Hauptfenster.btn_Help`, Ziel unverändert „Programmablauf") | Sie war `Form_Start.btn_Help` und lag dort oberhalb des gesamten Reiterwerks — die Hilfe zum **Fenster**, nicht zur Seite (Befund W16b‑B5, offener Punkt W16b‑O‑4) |
| **A‑9** | **Die sieben stillen `Console.WriteLine` der `Init*`-Methoden entfallen.** Sie meldeten „Menü konnte nicht eingebunden werden" auf eine Konsole, die es im Fensterbetrieb nicht gibt | In einer Tabelle gibt es kein Einhängen, das scheitern könnte (Befund W16‑B33) |

---

## 6 — Anwenderfragen

> **Nachtrag 04.09.2026.** Der Anwender hat entschieden: **E‑1, E‑2, E‑6,
> E‑8a und E‑9 sind bestätigt**, **W16c‑E‑1 ist ok**, und **W16c‑E‑2** und
> **W16c‑E‑3** sind zugunsten der jeweils anderen Form entschieden und
> umgesetzt (Commits `03c5947` und `74e0cc1`). Offen bleibt **W16a‑E‑1 /
> W16b‑O‑5**.
>
> **Nachtrag 04.09.2026, zweiter Durchgang.** **E‑10 ist entschieden und
> umgesetzt** (Commit `7ed320b`): Die Hülle heißt **`Hauptfensterrahmen`** —
> nicht `Hauptfenster` (die Razor-Seite) und nicht `HauptfensterHuelle` (deren
> Blazor-Hülle). Damit ist **W16c‑O‑1 erledigt**.

| # | Frage | Stand |
|---|---|---|
| **E‑1** | Wird `AppWurzel` die gemeinsame Wurzel? | **Vorläufig ja, umgesetzt.** Eine Wurzel, zwei Schalen: Die Zustandsmaschine kennt die Ansichten, `Kopfleiste` (`RenderFragment`) trägt je Plattform die Schale — unter Windows das Menüband samt Markenkopf, auf iOS **nichts**. `Startansicht` ist die Ansicht beim Aufmachen und zugleich das Ziel des Rückwegs; damit ist `ZurueckZurListe` auf beiden Plattformen derselbe Programmtext. **Bestätigt** (Anwender, 04.09.2026) |
| **E‑2** | `Masken` und `Seitenschluessel` zusammenlegen? | **Vorläufig ja, umgesetzt.** `Seitenschluessel` führt 34 Werte: die sieben der iOS-Wurzel, `STARTSEITE`, `BERICHTE_KOSTEN`/`VARIANTEN`, die 25 Maskenschlüssel und die 19 Wege des Hauptfensters. **Die übernommenen sind VERWEISE** (`= WindowsFormsApplication1.Masken.X`), keine Abschriften — `INavigation.OeffneMaske` bleibt unverändert gültig, `Masken` bleibt im Kern die Quelle. Die Richtung ist die einzig mögliche: EPOS.UI kennt EPOS.Kern, nicht umgekehrt. **Bestätigt** (Anwender, 04.09.2026) |
| **E‑6** | iF21 (Per Monitor V2, `DpiInsel` weg) in W16c? | **Vorläufig ja, umgesetzt.** `app.manifest` `dpiAware=true/pm` + `dpiAwareness=PerMonitorV2`, `Program.Main` `HighDpiMode.PerMonitorV2`, die `DpiInsel` und die zwei `ShowDialog`-Überladungen gelöscht. **Auf Linux ist nur der Bau prüfbar** — die Abnahme bei 100/125/150 % steht in § 9 und in `Umsetzung_iU9_Nachweise.md` § 12.1. **Bestätigt** (Anwender, 04.09.2026) |
| **E‑8a** | Formularkarte behalten, Bestandstests aufs Prüfmuster? | **Umgesetzt.** Sechs Anker sind umgehängt, einer ist **zurückgeholt** (§ 8). **Bestätigt** (Anwender, 04.09.2026) |
| **E‑9** | Wohin mit den Zeugen? | **Umgesetzt.** `MDIMainForm.Designer.cs` und seine drei `.resx` (3 493 Z.) sind **vor** dem Rückbau eingefroren; der Großschreibungs-Zeuge und der Wurzelnamensraum-Zeuge hängen seither dort, und der Prüfmusterbaum trägt jetzt auch einen **Sprungtabellen-Auszug**. **Bestätigt** (Anwender, 04.09.2026) |
| **E‑10** | `MDIMainForm` umbenennen? | **Anwenderentscheid 04.09.2026: `Hauptfensterrahmen` — umgesetzt** (Commit `7ed320b`). **Nicht `Hauptfenster`** (so heißt die Razor-SEITE in `EPOS.UI.Seiten`) und **nicht `HauptfensterHuelle`** (so heißt die Blazor-Hülle dieser Seite): Der RAHMEN ist das Fenster mit `Application.Run`, dem `BlazorWebView`, F1 und dem Sprachwechsel. Angefasst: die Klasse und ihre Datei (`WindowsFormsApplication1/MDIMainForm.cs` → `Views/Hauptformular/Hauptfensterrahmen.cs`, `git mv`, neben `HauptfensterHuelle.cs`; Namensraum unverändert), `Program.mdifrm` → `Program.rahmen` (kein Aufrufer außerhalb von `Program`), `Erreichbarkeit.Wurzelmasken`, `HilfeKontext.BEREICH_JE_TYP` (der Schlüssel ist der Typname des offenen Fensters und zieht mit um, das Ziel `B_HAUPTFENSTER` bleibt), die **zwei** Prüfmuster-Auszüge (beide `git mv`), `Stapel.Uebersicht`, `DesignerLeser`, `Formularkarte/LIESMICH.md` und die Kommentare in den Hüllen, in `EPOS.UI` und in `EPOS.UI.Tests`. **`help_mapping.txt` war nicht zu ändern**: Es führt seit W16c.6 keinen Schlüssel dieses Fensters mehr — die Fensterhilfe heißt `Hauptfenster.btn_Help` und sitzt im Kopfband der Razor-Seite. **Belassen** (Geschichte, kein lebender Bezeichner): die eingefrorenen `MDIMainForm.Designer.cs` und die drei `.resx` im Prüfmuster und jedes Zitat, das eine gelöschte DATEI oder Quellzeile nennt |
| **W16a‑E‑1 / W16b‑O‑5** | Wird der Assistent eine freie Ansicht? | **W16c hat es nicht getan** (Auflage der Anweisung). Er bleibt eine modale `BlazorDialogForm`; `Seitenschluessel.ProjektNeu`/`…Bearbeiten` gehen über `HauptfensterHuelle.Weg` in dieselbe Hülle wie bisher — samt dem Nachzug des Projektkontexts („Befund 3"). **Entschieden 04.09.2026: ja — in iU11 zusammen mit der Transaktion W16a‑O‑1.** |
| **W16b‑E‑1 / W16b‑E‑2** | (aus W16b) | **Nicht angefasst** |
| **W16c‑E‑1 (neu)** | **Das Menü klappt beim KLICK auf, nicht beim Überfahren** (A‑1) | Der `MenuStrip` öffnete ein Untermenü, sobald die Maus darüber stand — und schloss das vorige. Wer das Menü mit der Maus „durchfährt", muss jetzt jeden Kopf anklicken. Die Tastaturbedienung ist dieselbe geblieben (← → ↓ Pos1 Ende Esc). **ok** (Anwender, 04.09.2026) |
| **W16c‑E‑2** | Die zwei Sprachpunkte als eigene Köpfe neben „Hilfe" — oder ein Untermenü „Sprache"? | **Anwenderentscheid 04.09.2026: Untermenü „Sprache" — umgesetzt** (Commit `03c5947`). Der Bestand führte fünf Köpfe (`menuToolbar.Items` = Projekt, Administration, Hilfe, Deutsch, Englisch); W16c hatte das wörtlich übernommen, weil die Welle nichts umbauen sollte, was sie nur umzieht. Jetzt steht **ganz rechts, wo „Deutsch" stand, der Kopf „Sprache"** (`MENU_SPRACHE`, en „Language"); er klappt nur auf, die zwei Punkte sind seine Untereinträge und behalten Name, Bild (`germany`/`usa`) und Seitenschlüssel — `help_mapping.txt` und `HauptfensterHuelle.Weg` greifen unverändert, `Application.Restart()` bleibt. Zahlen: **55** Punkte, **4** Köpfe, **13** aufklappende, 42 handelnde, 8 Trenner, 11 Bilder |
| **W16c‑E‑3** | Holt „Varianten und Bericht…" den sechsten Reiter der Startseite nach vorn — oder wechselt er die Ansicht? | **Anwenderentscheid 04.09.2026: Ansichtswechsel — umgesetzt** (Commit `74e0cc1`). Bis dahin wörtlich der Bestand (`MenuItem_VariantenBericht_Click` → `StartseiteHuelle.Aktuelle.ZeigeBerichteKosten`), und `BERICHTE_KOSTEN` war allein der **iOS**-Weg. Jetzt ist es der Weg **beider** Plattformen: Der Fall in `HauptfensterHuelle.Weg` ist gefallen — der Schlüssel steht in `Ansichten`, nicht in `Masken`, also meldet `MaskeOeffnen` false und `Hauptfenster.Springe` lässt die `AppWurzel` auf die Ansicht wechseln. **Das sechste Reiterblatt bleibt bestehen** (dieselbe Komponente, dieselbe `BerichteKostenHuelle`); nur der Menüweg führt in die Ansicht. Der Rückweg geht über `ZurueckZurListe` auf die `Startansicht` — dafür hat `BerichteKostenSeite` einen `Geschlossen`-Rückruf und den Knopf `BK_BTN_ZURUECK` bekommen, den es **ohne** Rückruf (also im Reiterblatt) nicht gibt |
| **W16c‑E‑4 (neu, 05.09.2026)** | **Der Kopf „Sprache" steht rechtsbündig** — Anwenderwunsch 05.09.2026 („Sprache sollte oben rechts sein") | **Umgesetzt.** Im Bestand sassen die zwei Sprachpunkte als letzte Einträge des `MenuStrip` rechtsbündig am Rand; der Kopf „Sprache" aus **W16c‑E‑2** hat ihre Stelle geerbt, stand seither aber links neben „Hilfe". Verschoben wird **nur die Optik**: `Menuepunkt` führt das Kennzeichen `RechtsBuendig`, das Band hängt daran die Klasse `epos-menueband-punkt--rechts` (`margin-left: auto`), und der Punkt bleibt an SEINER Stelle im Markup. Damit sind Tastaturreihenfolge (← → über die vier Köpfe, **Ende = „Sprache"**), Sprachausgabe und der Nachweis **N4** unverändert — ein umsortiertes Markup hätte beides verschoben. Die Zeile in der erzeugten `Menuetabelle` ist dieselbe, die schon W16c‑E‑2 von Hand nachgetragen hat (sie hat keine Designer-Herkunft); sie bekommt das Kennzeichen als benannten Ctor-Parameter, weil Objekt- und Sammelinitialisierung sich nicht mischen lassen |
| **W16c‑E‑5 (neu, 05.09.2026)** | **Farbgebung des Menübands und des Kopfbands an die WinForms-Fassung vor W16 anlehnen** — Anwenderwunsch 05.09.2026 („Design und Farbgebung kann verbessert werden, angelehnt an winforms Version vor-W16"); die Startseite trägt denselben Wunsch als **W16b‑E‑5** | **Umgesetzt — ausschließlich im Stilblatt.** Fünf Werte des Vorläufers standen bis hierher **nur als Rückfall IN der Regel** (`var(--epos-marke, #005aa0)`) und damit an so vielen Stellen, wie sie benutzt wurden; sie sind jetzt Token in `:root` — `--epos-marke`, `--epos-marke-untertitel`, `--epos-marke-trennlinie`, `--epos-menue-flaeche`, `--epos-flaeche-hell`. Angeglichen sind dabei vier Dinge: das **Menüband** trägt wieder das AliceBlue des `menuToolbar` (#f0f8ff statt #f0f6fc) und seine **vier Köpfe** die 12-pt-Schrift des Bestands (16 px statt 13); die **Trennlinie** unter Menüband und Kopfband ist das kühle #dee3e8 aus `InitMarke` statt des warmen Hausbeiges #d9d7cf; der **Produktname** steht in 19 px (Segoe UI Semibold 14 pt) statt in Kacheltitelgröße; **Gattung und Claim** stehen in 11 px (8,25 pt) und im kühlen #70777e. **Die Menühöhe bleibt bei 44 px** — Berührungsziel (Hausregel M2/iL4), denn dieselbe Wurzel trägt auf dem iPad ein Menü, das mit dem Finger bedient wird; der Bestand maß 29 px. **Die Versionsfarbe ist NICHT übernommen** (150,156,162 trägt auf Weiß nur 2,77:1); die Version wird nur kleiner und behält das leise Hausgrau. Die Tabelle unten nennt jeden Wert mit Fundstelle |


### Die Farb- und Schrifttabelle zu W16c‑E‑5 (Vorbild → Umsetzung)

> Erhoben aus `84d7c16`: `MDIMainForm.Designer.cs` (`menuToolbar`), `MDIMainForm.resx`
> (Schriften der Köpfe) und `MDIMainForm.InitMarke` (:200–285) — das Kopfband war
> **programmatisch** gebaut, damit Designer und `.resx` unberührt blieben, und steht
> deshalb vollständig im Programmtext. „bis `830c903`" ist der Stand, den der Anwender
> am 05.09.2026 gesehen hat.

| Element | Vorbild (Wert, Fundstelle) | bis `830c903` | jetzt |
|---|---|---|---|
| **Menüleiste, Fläche** | `menuToolbar.BackColor = Color.AliceBlue` = #f0f8ff (Designer :90) | Rückfall #f0f6fc, kein Token | `--epos-menue-flaeche` #f0f8ff |
| **Menüköpfe, Schrift** | Segoe UI **12 pt** = 16 px (`.resx`: `Projekte`, `Administration`, `Help`, `Deutsch`, `Englisch`) | 13 px (Dialogschrift) | 16 px — nur die Köpfe; die Zeilen der aufgeschlagenen Ebene bleiben bei 13 px |
| Menüleiste, Höhe | 29 px, Polster 7,2,0,2 (`.resx`) | 44 px Berührungsziel | **unverändert** — Hausregel M2/iL4 |
| Menüleiste, Linie unten | (der `MenuStrip` zog keine; die Linie kam vom Kopfband darunter) | `--epos-rahmen-leise` #d9d7cf | `--epos-marke-trennlinie` #dee3e8 |
| Auswahl im Menü | `ToolStripProfessionalRenderer` (helles Blau) | Rückfall #d9e8f7 | `--epos-menue-marke` #d9e8f7 (jetzt Token) |
| **Kopfband, Fläche** | `Panel { Height = 52, BackColor = White }` (`InitMarke` :215–219) | Rückfall #ffffff | `--epos-flaeche-hell` #ffffff |
| **Kopfband, Linie unten** | `Pen(Color.FromArgb(222, 227, 232))` (:224) | `--epos-rahmen-leise` #d9d7cf | `--epos-marke-trennlinie` #dee3e8 |
| Akzentbalken links | `FillRectangle(0,90,160)`, 4 px breit, von y=10 bis Höhe−22 (:227) | 4 px, Rückfall #005aa0 | `--epos-marke` #005aa0 (Token; die senkrechte Aussparung bleibt weg — ein `border-left` läuft durch, und der Balken ist die Aussage, nicht seine Länge) |
| **Produktname** | Segoe UI Semibold **14 pt** fett ≈ 19 px, ForeColor 0,90,160 (:235–236) | 16 px (`--epos-schriftgroesse-kartentitel`) | 19 px, `--epos-marke` |
| **Gattung + Claim** | Segoe UI **8,25 pt** = 11 px, ForeColor 112,119,126 (:244–245) | 13 px, `--epos-text-leise` #5f5e5a | 11 px, `--epos-marke-untertitel` #70777e (4,54:1 auf Weiß) |
| Version | Segoe UI 8,25 pt, ForeColor 150,156,162 (:267–268) | 13 px, `--epos-text-leise` | 11 px, `--epos-text-leise` — **Farbe nicht übernommen**: 150,156,162 trägt auf Weiß nur 2,77:1 |
| Kopfband, Polster | `Padding(16, 6, 0, 0)` links, `Padding(0, 20, 18, 0)` rechts (:255, :269) | 6/18/8/18 | unverändert — ohne feste Pixelkoordinaten, das Band ist ein Flexkasten |

Die Werte stehen als Fall in `EPOS.UI.Tests/Seiten/StartseiteAnmutungTests.cs`
(`Die_Werte_des_Hauptfensters_stehen_in_root`,
`Das_Kopfband_des_Hauptfensters_traegt_die_Masse_von_InitMarke`,
`Das_Menueband_ist_AliceBlue_und_traegt_die_Kopfschrift_des_Bestands`) — dieselbe Wache,
die auch W16b‑E‑5 trägt.

---

## 7 — Befunde

| # | Befund | Folge |
|---|---|---|
| **W16c‑B1** | **`Masken.PvImport` fehlte seit W13.0k in `DiensteTests.Navigationsschluessel_sind_sprachneutrales_ASCII`** — der einzige der 25 Schlüssel ohne Zeugen | Der Abgleich beider Klassen (K7) hat die Lücke gefunden; sie ist geschlossen |
| **W16c‑B2** | **45 ist die DESIGNER-Zahl, nicht die Menüzahl.** Der Designer führt 45 `ToolStripMenuItem` und 6 `ToolStripSeparator`; die acht `Init*`-Methoden hängen **neun** weitere Punkte und **zwei** Trenner ein | Das laufende Programm hatte immer **54** Punkte und **8** Trenner. Die Tabelle führt sie gleichrangig — der Grund für das programmatische Einhängen („damit Designer und `.resx` unberührt bleiben") entfällt mit dem Designer |
| **W16c‑B3** | **Es sind VIER Leichen in den `.resx`, nicht zwei.** W16‑B26 nennt `MenuItem_Update` und `MenuItem_PV_Import`; dazu kommen `MenuItem_Kosten` und `kostenAdminToolStripMenuItem` im neutralen `.resx` | Das sind die Alteinträge „Kosten" und „Kosten Admin", die Änderung 7 der Kostendialoge entfernt hat. Alle vier sind ersatzlos liegengeblieben und fallen mit dem Designer |
| **W16c‑B4** | **SIEBEN Menüpunkte hatten keine englische Beschriftung, nicht vier.** W16‑B26 nennt `Administration` und `MenuItem_Version` (beide wortgleich), `menuToolbar` und `$this` (keine Menüpunkte) sowie `MenuItem_ExportImport` und `MenuItem_Dokumentation` (in beiden Satelliten leer). Nachgemessen fehlen außerdem `MenuItem_Einstellungen`, `MenuItem_KostenVerwaltung` und `MenuItem_PV_Import_CEC` — sie standen **nur** im neutralen `.resx` | Ergänzt: Administration/Administration, Version/Version, Export/Import (wortgleich), Dokumentation/Documentation, Einstellungen/Settings, Kosten/Costs, „Import Photovoltaik CEC/Pan"/„Import photovoltaics CEC/Pan" |
| **W16c‑B5** | **`MenuItem_PV_Import_PAN` gibt es als Steuerelement NICHT** — nur seinen Handler (`MDIMainForm.cs:809`). Dasselbe gilt für `MenuItem_PV_Import_Click` (`:757`) | Befund W16‑B24 bestätigt und erledigt: Beide Handler sind mit dem Rückbau gefallen; im Menübaum stand ohnehin nur `MenuItem_PV_Import_CEC` |
| **W16c‑B6** | **Der KI-Assistent ist eine Zeile der Tabelle.** `InitKiHilfe` suchte sein Hilfemenü über den **Anzeigetext** (`StartsWith("Hilfe"/"Help")`) | Befund W16‑B23 eingelöst — die einzige Stelle des Bestands, an der ein Anzeigetext als Schlüssel diente. Jetzt: Ziel `KI_ASSISTENT`, Kürzel `F1` |
| **W16c‑B7** | **Die Sollwerte von N1 gehen nicht auf.** Die Anweisung nennt `Masken == 0` und `Erreichbar(Ja) == 0`; nachgemessen sind es **1 und 1** | Die Sollwerte der Vermessung sind vom Stand **vor W15b** gerechnet, als `Form_HelpPopup` noch als umzustellend galt; mit Entscheid W15b‑E‑2 bleibt sie bis iU11. Geprüft wird deshalb die STARKE Form: genau eine Maske, und zwar diese (mit Namen) |
| **W16c‑B8** | **`Dienste.Datei.MitSystemOeffnen` taugt nicht für eine Adresse** — es prüft `File.Exists` und liefert für `https://…` immer `false` | Neues Glied `AdresseOeffnen(string)` mit Standardumsetzung `false`; die Windows-Fassung trägt den Rumpf des alten Menühandlers. Auf iOS bleibt es bei `false` (kein Menü, kein Aufrufer) — nachzuziehen mit iU11 |
| **W16c‑B9** | **`Program.cs` brauchte KEINE Bereinigung.** Nach dem Rückbau ist keines der vier Felder verwaist: `mdifrm` trägt `Application.Run`, `projektkontext` ist `Dienste.Projekt`, `menuectrl` liest `AssistentHuelle`, `wizardctrl` die Startseiten- und die Hauptfensterhülle | Die Anweisung nennt „`Program` bereinigen"; es gab nichts zu bereinigen — `startfrm` und `mainfrm` sind schon mit W16b gefallen |
| **W16c‑B10** | **`AppWurzel.ZurueckZurListe` räumte `_simErgebnis` nicht ab** (die übrigen fünf Zwischenspeicher schon) | Beim Umbau erledigt. Wirkung im Bestand: keine — die Ansicht wird über `_ansicht` gewählt; der Satz blieb bloß am Leben |
| **W16c‑B11** (04.09.2026, bei W16c‑E‑3 gefunden) | **`IProjektQuelle` stand im Windows-Dienstverzeichnis GAR NICHT** — `BlazorDienste.Erzeugen` trug allein `IHilfeDienst`. `AppWurzel` fordert die Quelle seit W16c.2 per `@inject` an und wird seither in **jedem** Windows-Start gezeichnet; ein fehlendes `@inject`-Ziel wirft beim Aufbau der Komponente | `BlazorDienste` trägt jetzt `KeineProjekte` ein. Unter Windows liefert die Quelle bewusst nichts — jede Ansicht bekommt ihren Parametersatz von der Hülle —, aber eingetragen muss sie sein. Auf Linux ist das nicht nachweisbar (die Windows-Abnahme steht als W16c‑O‑2 aus); der Befund kam beim Nachrechnen des Weges für W16c‑E‑3 heraus |

| **W16c‑B12** (04.09.2026, beim ersten Start durch den Anwender) | **`Hauptfenster` fehlte der `[Parameter] Zustand` — die Anwendung startete nicht.** `BlazorSeite<T>` trägt den `SeitenZustand` **jedem** Parametersatz nach (`BlazorSeite.cs:93‑96`): Wer keinen mitgibt, bekommt einen frischen, und der Schlüssel `"Zustand"` steht danach in jedem Satz. Bis W16b war die Wurzel der Hülle `BlazorSeite<Startseite>`, und die Startseite **führt** den Parameter; seit W16c.2 ist es `BlazorSeite<Hauptfenster>` (`Hauptfensterrahmen.cs:122`) — und `Hauptfenster.razor` hatte ihn nicht. Blazor wirft dafür beim **ersten Zeichnen** `InvalidOperationException: Object of type 'EPOS.UI.Seiten.Hauptfenster' does not have a property matching the name 'Zustand'`; weil das im Verteiler geschieht, reicht die WinForms-Nachrichtenschleife es als **`TargetInvocationException` an `Program.Main:332`** (`Application.Run`) weiter — die Aufrufliste zeigt nur `[Externer Code]` | Behoben in `73b6e58`: `Hauptfenster` trägt den Parameter und reicht ihn an `AppWurzel`, die sich an `SeitenZustand.Geaendert` hängt und neu zeichnet (der Weg, den `BlazorSeite.ProjektSetzen` beschreibt). `AppWurzel.MitZustand()` legt ihn nur einem Parametersatz bei, der **keinen** führt: Unter Windows behalten `StartseiteHuelle` und `BerichteKostenHuelle` ihren eigenen — über ihn meldet `ProjektKontextCtrl.Gewechselt` den Projektwechsel (**Abnahmepunkt 9**), und zwei Zustände für dieselbe Ansicht wären zwei Wahrheiten |
| **W16c‑B12a** — warum 4 031 grüne Tests ihn nicht sahen | **Die bunit-Fälle rendern ohne die Hülle.** `Render<Hauptfenster>(p => p.Add(x => x.Weg, …))` setzt **getippte** Parameter; ein Schlüssel ohne `[Parameter]` kann dabei gar nicht entstehen. Der Weg der Hülle ist ein **Wörterbuch** auf die Wurzelkomponente (`RootComponents.Add<T>("#app", parameter)`), und nur dort fällt ein unbekannter Schlüssel auf. Dieselbe Lücke hatte schon **W16c‑B11** (`IProjektQuelle` fehlte im Dienstverzeichnis) — beide Male war das Fehlende etwas, das **nur die Hülle** beisteuert | **Zwei Wachen.** (1) `BlazorSeite<T>` prüft im Konstruktor per Reflexion, ob `T` einen beschreibbaren `[Parameter]` `Zustand` führt, der einen `SeitenZustand` aufnimmt, und wirft sonst „BlazorSeite verlangt einen Parameter Zustand" **mit dem Typnamen** — lesbar statt verpackt. (2) `HauptfensterTests.AusHuelle()` rendert über `AddMultipleAttributes`, also genau wie die Hülle; fünf Fälle hängen daran, darunter die `Theory` `Jede_Seite_einer_Seitenhuelle_traegt_den_Parameter_Zustand` über `Hauptfenster`, `Startseite` und `BerichteKostenSeite` |
| **W16c‑B13** (05.09.2026, Windows-Abnahme, Bildschirmfoto „iOS_Migration_Probleme" S. 3) | **Die Untermenüs des Menübands ließen sich nicht aufklappen.** *Beobachtung:* „Administration" öffnet auf Klick und zeigt seine elf Einträge — aber die sieben mit `▸` („Wärmebedarf & Heizung", „Strombedarf & Speicher", „Energiesysteme", „Klimadaten & Umgebung", „Daten & Import", „Kosten", „Gebäude") taten beim Klick nichts; das Menü schloss sich, es passierte sonst nichts. *Ursache:* Am `<nav>` des Bandes hing **`@onfocusout`**. `focusout` **blast nach oben** und feuert AUCH, wenn der Fokus **innerhalb** des Bandes wandert — der Zeigerdruck auf eine Untermenüzeile nimmt dem Kopfknopf den Fokus, das Ereignis steigt zum `<nav>` auf, `Schliessen()` räumt die Klappe weg, und weil die gedrückte Zeile damit **aus dem DOM** ist, kommt beim Loslassen **gar kein `click`** mehr an. `FocusEventArgs` kennt kein `relatedTarget`, ein sicheres „ist der neue Fokus noch im Band?" gibt es also nicht; auf dem iPad setzt eine Berührung überhaupt keinen Fokus, dort schloss dasselbe Band nie. Dazu **zwei Nebenursachen**: Die zweite Ebene lag in einem flachen `HashSet` über **Namen** neben einem Feld `_offen` für die oberste — zwei Geschwister konnten gleichzeitig offen sein —, und die Tastatur kannte weder `→` noch `←` noch ein Wandern in der offenen Klappe; die dritte Ebene war auch mit ihr nicht erreichbar (der Kopfkommentar versprach `↑ ↓` seit W16c.1, der Code hatte es nie) | Behoben in `3d1a0ca`. **(1)** Der Offen-Zustand ist ein **PFAD** (`_pfad`): `_pfad[0]` der aufgeklappte Kopf, `_pfad[1]` der Punkt darin, `_pfad[2]` der der dritten Ebene. `Offen(p, ebene)` fragt, ob `p` auf **seiner** Ebene im Pfad steht; weil eine Ebene nur EINEN Eintrag trägt, schließen Geschwister einander aus, und mit einem Kopf fällt sein ganzes Untermenü. **(2)** `@onfocusout` ist gestrichen; solange ein Menü offen ist, steht statt dessen eine **Schließfläche** — ein durchsichtiger Deckel über der ganzen Ansicht (`position: fixed; inset: 0`), der den Klick oder Tipp NEBEN das Menü fängt. Kein Fokus, kein JavaScript, auf Maus und Finger dasselbe. Drei z-Ebenen halten das zusammen: Band 41, Klappe 40, Deckel 39 — sonst finge der Deckel den Klick auf den Menüpunkt selbst ab. **(3)** Die Tastatur führt einen Zeiger `_zeile` durch die tiefste offene Klappe: `↓` öffnet und wandert, `↑` wandert zurück, `→` öffnet das Untermenü des Zeigers, `←` schließt genau diese Ebene, `Esc` und `Tab` schließen alles. Nur der Zeiger steht in der Klappe im Tabulatorzyklus (roving `tabindex`), `OnAfterRenderAsync` zieht den Fokus nach — damit wählt Enter/Leertaste **ohne eigenen Handler**, es ist ein `<button>`, der den Fokus wirklich hat. **(4)** Jedes Kind einer Klappe steht in einer eigenen `OpenRegion`: Der durchlaufende Zähler als Folgenummer verschob beim Aufgehen einer tieferen Klappe alle folgenden Nummern, und Blazor baute den Rest der Liste neu auf — samt Fokus |
| **W16c‑B13a** — warum 2 317 grüne Tests ihn nicht sahen, und die zwei Fallen beim Beheben | **bunit feuert nur das Ereignis, das der Fall nennt.** `cut.Find("#menue-…").Click()` löst `click` aus und **keinen Fokuswechsel** — der Fall `Ein_Untermenue_der_dritten_Ebene_klappt_seitlich_auf` stand seit W16c.1 grün, während im Browser genau dieser Weg tot war. Es ist dieselbe Art Lücke wie **W6‑B‑1** (das Stilblatt) und **W16c‑B12** (der Parametersatz): Was nur die Umgebung beisteuert — hier die Fokusmechanik des Browsers —, sieht ein bunit-Fall nicht. Beim Beheben kamen zwei Fallen dazu: ein **bedingter** `AddElementReferenceCapture` bricht Blazors Abgleich mit `NotImplementedException: Unexpected frame type during RemoveOldFrame` (die Verweise liegen deshalb für **alle** Zeilen in einer Tabelle über `Menuepunkt.Name`), und ein Suchmuster mit `\n` im Stilblatt fällt auf dem Windows-Läufer ins Leere (CRLF in der Arbeitskopie — dieselbe Ursache wie `d3abd94`) | **14 Wachen** in `e467635`. `MenuebandTests` bekommt einen eigenen Weg **`AusHuelle`** — EIN Wörterbuch über `AddMultipleAttributes` mit der **echten** `Menuetabelle`, so wie `HauptfensterTests` es seit `B12a` tut; ein zurechtgelegter Baum hätte den Befund nicht getragen, er hängt an ihren drei Ebenen. Elf Fälle im Baustein (Aufklappen der zweiten Ebene mit allen sechs Punkten im DOM, das Zuklappen, der gegenseitige Ausschluss zweier Geschwister, der Kopfwechsel, der dreistufige Weg bis `PvAdmin`, die Schließfläche, **die Gegenprobe zur Ursache** — `Assert.Throws<MissingEventHandlerException>(() => cut.Find(".epos-menueband").FocusOut())`, zwei Tastaturwege, der Tabulator, die drei z-Ebenen im Stilblatt) und drei im Fenster (derselbe Menüweg über den Parametersatz der Hülle, der Punkt der dritten Ebene im EINEN Handler, der Klick daneben) |

---

## 8 — Die Zeugen der Formularkarte (E‑8a / E‑9, Nachweise N1 und N2)

Der Rückbau nimmt der Formularkarte **sechs** Anker; alle sechs sind im selben
Commit umgehängt, und **einer ist zurückgeholt**.

| Anker | vorher | nachher |
|---|---|---|
| `FindetAlleDesignerDateien…` — Großschreibungs-Zeuge | `MDIMainForm.Designer.cs` im Bestand | `Form_HelpPopup.Designer.cs` im Bestand **und** `MDIMainForm.Designer.cs` im Prüfmuster |
| dieselbe — `dateien.Count >= 5` | 5 | **4** (2 unter `WindowsFormsApplication1`, 2 erzeugte des Kerns) |
| `JedeMaskeLiefertEineKarte` — **N1** | `Masken >= 2` | **`Masken == 1`** und `Form_HelpPopup` mit Namen |
| `DieHaelfteDerMaskenIstLokalisiert` — **N2** | `Lokalisierte >= 1` im Bestand | **`== 0`** im Bestand, **`>= 1` im Prüfmuster** |
| `DieUebersichtNenntZahlenUndMasken` | „MDIMainForm" | „Form_HelpPopup" |
| `MaskenAusserhalbEinesFachordners…` | `Kartenbau` über `WindowsFormsApplication1/MDIMainForm.Designer.cs` | beide Zweige von `DesignerLeser.Fachbereich` unmittelbar (`Properties/` und der Projektordner an der `.csproj`) **plus** der Gegenbeweis am Prüfmuster |
| `DerStapellaufZaehlt…` — **N1** | `Erreichbar(Ja) >= 2`, „\| MDIMainForm \| ja \|" | **`== 1`**, „\| Form_HelpPopup \| ja \|" |
| **`DieSprungtabelleLoestDieMaskenschluesselAuf`** | **gestrichen mit W16b.1** | **ZURÜCK** — gegen das Prüfmuster |

**Offener Punkt W16b‑O‑1 ist erledigt.** Der Graph löst einen Maskenschlüssel
nicht über einen Aufruf auf, sondern über eine besondere Klasse: Er erkennt
`WinFormsNavigation` am Namen, liest den `switch` in `OeffneMaske` und ordnet
jedem `case Masken.X:` die Masken zu, die dieser Zweig anfasst
(`Erreichbarkeit.cs:651–694`). Diese Mechanik war nach W16b nicht mehr prüfbar —
es gab keinen Schlüssel mehr mit einer WinForms-Maske dahinter. Das Prüfmuster
führt jetzt einen Auszug:

```
Pruefmuster/Hauptformular/WinFormsNavigation.Auszug.cs                (ein case Masken.PufferSpAdmin)
Pruefmuster/Hauptformular/Hauptfensterrahmen.Sprungtabelle.Auszug.cs  (die Wurzel, die den Schluessel nennt)
        ↓
Pruefmuster/Pufferspeicher/Form_PufferSp_Admin                        („ja")
```

> Die zwei Wurzeldateien des Prüfmusters hießen bis zum 04.09.2026
> `MDIMainForm.Sprungtabelle.Auszug.cs` bzw.
> `Pruefmuster/Pufferspeicher/MDIMainForm.Auszug.cs`; sie sind mit **E‑10**
> umbenannt, weil `Erreichbarkeit.Wurzelmasken` den KLASSENnamen führt. Die
> eingefrorenen **Designer- und `.resx`-Zeugen behalten ihren alten Namen** —
> sie sind das Abbild des Bestands, nicht lebender Quelltext.

Der Fall prüft die **Öffnerliste**, nicht den Pfad: Die Maske hat im Muster zwei
Öffner — den unmittelbaren Weg (der Anker des „unklar"-Falls) und den Weg über
den Schlüssel —, und der Graph nennt den kürzeren als Pfad.

**Risiko R‑W16‑10 ist eingelöst.** `Form_HelpPopup` meldet unverändert „ja".
`MDIMainForm` bleibt als **Klasse** die Wurzel des Graphen — er liest Quelltext,
nicht Designer —, ist aber keine **Maske** mehr. `Program.Main` musste nicht zur
dritten Wurzel werden.

---

## 9 — Gate

| Prüfung | Sollwert | Ergebnis |
|---|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64` | 0 Fehler, 6 Warnungen | **0 / 6** (Vollneubau) |
| `dotnet test WP-Plan.Kern.slnf -c Release` | Basis 3 968 + neue | **4 002** (KiKern 450, SpeicherEngine 337, EPOS.Kern.Tests 1 021, EPOS.UI.Tests **2 194**) — **+34** |
| dieselben Tests unter `LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8` | gleich | **grün** |
| `dotnet test Werkzeuge/Formularkarte.Tests -c Release` | 121 | **122**, auch unter `en_US` (+1: der zurückgeholte Maskenschlüssel-Zeuge) |
| Stapellauf `--alle WindowsFormsApplication1` | **1 Maske / 2 Designer** | **1 / 2**, 0 lokalisiert, **1 ja / 0 nein / 0 verwaist / 0 unklar** |
| `SqlDialektPruefer` | 0 Fundstellen | **0 von 1 200**; **`WindowsFormsApplication1` hat null Inline-SQL** (Befund W16‑B34 hält) |
| `ChartProben` | 32 unverändert | **32 Bilder, 0 Verstöße** |
| Referenzlauf 1030 / 1007 / 1017 gegen `2026-08-30_B3-Kaskade` | byte-gleich | **PASS, 815 043 Werte** (1007: 324 219, 1017: 254 154, 1030: 236 670); `diff -rq` **byte-gleich in allen drei** |
| Wächter `Program.*` im Kern | leer | **leer** |
| Wächter `System.Windows.Forms`/`MessageBox.`/`Registry.`/`ProtectedData`/`OleDb`/`SpecialFolder` im Kern | leer | **leer** (nur die dokumentierten Kommentare und die eine begründete `SpecialFolder`-Stelle in `DataRepository`) |
| `git grep` auf jeden gefallenen Bezeichner | nur Kommentare, Protokolle und das eingefrorene Prüfmuster | **erfüllt** |

**Nach dem Merge von `origin/ios_migration`** (`97b048c` — der zweiundzwanzigste
iOS-Lauf auf dem Stand nach W16b, die Nachweisliste und das Konzept) ist das
ganze Gate ein zweites Mal gelaufen: Build **0 / 6** (Vollneubau), **4 002**
grün und ebenso unter `en_US`, Formularkarte **122**, Stapellauf **1 / 2** mit
0 lokalisiert und 1 / 0 / 0 / 0, SQL **0 von 1 200**, ChartProben **32**,
Referenzlauf **byte-gleich in allen drei Projekten** (815 043 Werte), beide
Wächter leer. **Der Merge lief ohne Konflikt** — die andere Seite hat nur
`Umsetzung_iU10_Nachweise.md` und `Umsetzungskonzept_iOS_EPOS-Plan.md`
angefasst, keine Quelldatei.

**Gate der zwei Anwenderentscheide vom 04.09.2026** (Commits `03c5947` und
`74e0cc1`, Basis `555ef11`):

| Prüfung | Sollwert | Ergebnis |
|---|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64` | 0 Fehler, 6 Warnungen | **0 / 6** (Vollneubau, beide Male) |
| `dotnet test WP-Plan.Kern.slnf -c Release` | Basis 4 002 + neue | **4 006** nach W16c‑E‑2 (+4), **4 012** nach W16c‑E‑3 (+6) — nur `EPOS.UI.Tests` (2 194 → **2 204**) |
| dieselben Tests unter `LANG=en_US.UTF-8` | gleich | **grün** |
| `dotnet test Werkzeuge/Formularkarte.Tests -c Release` | 122 | **122**, unverändert (keine Maske berührt) |
| Wächter `Program.*` / WinForms im Kern | leer | **leer** (am Kern nur vier `MyResource`-Einträge: `MENU_SPRACHE`, `BK_BTN_ZURUECK`) |
| `ChartProben`, `SqlDialektPruefer`, Referenzlauf | nicht berührt | **kein Rechenweg, kein SQL, kein Bild angefasst** — der Referenzlauf läuft in der Orchestrierung |

**Der Nachweis N4 hat neue Zahlen** (§ 4): 55 Punkte, 4 Köpfe, 13
aufklappende, 42 handelnde. Der Fall „Ein Sprachpunkt der obersten Ebene
meldet unmittelbar" heißt jetzt „Ein Sprachpunkt im Untermenü Sprache meldet
beim Klick"; dazu kamen der Aufbau des Kopfes, der zweite Sprachpunkt, die
Pfeiltasten über **vier** Köpfe mit öffnendem „Sprache" und — für W16c‑E‑3 —
der Ansichtswechsel samt Rückweg an `Hauptfenster` und `AppWurzel` sowie der
Rückwegknopf mit und ohne Rückruf an `BerichteKostenSeite`.

**Gate der Behebung von W16c‑B12** (Commit `73b6e58`, Basis `01463d1`):

| Prüfung | Sollwert | Ergebnis |
|---|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental` | 0 Fehler, 6 Warnungen | **0 / 6** (Vollneubau) |
| `dotnet test EPOS.UI.Tests -c Release` | Basis 2 222 + 5 neue | **2 227** (+5: vier Fälle über `AusHuelle` und eine `Theory` mit drei Zeilen) |
| dieselben Tests unter `LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8` | gleich | **grün** |
| `dotnet test EPOS.Kern.Tests -c Release` | unverändert | **1 024 grün** (der Kern ist nicht berührt) |
| **Gegenprobe**: ein zusätzlicher unbekannter Schlüssel im Parametersatz | die vier `AusHuelle`-Fälle rot | **rot, mit genau dem gemeldeten Wortlaut** (`does not have a property matching the name …`) — der neue Weg prüft wirklich die Schlüssel |
| `ChartProben`, `SqlDialektPruefer`, Referenzlauf | nicht berührt | **kein Rechenweg, kein SQL, kein Bild angefasst** |

**Der Referenzlauf ist byte-gleich, und das war zu erwarten:** Der Rechenkern
ist nicht angefasst. Der Berührungspunkt dieser Teilwelle ist der **Startweg** —
Erststart, Lizenz, `LizenzManager.NachpruefungImHintergrund()` —, und der ist
inhaltlich unverändert: dieselben Aufrufe in derselben Reihenfolge, nur an einer
anderen Stelle (die Nachprüfung stand am Ende von `InitLizenzMenue`, jetzt am
Ende von `BeimLaden`).

---

## 10 — Windows-Abnahme (Vollabnahme N1–N10)

| # | Was | Erwartung |
|---|---|---|
| **0** | **START** — die Anwendung aus Visual Studio (Debug, x64) und als Installation starten | Das Hauptfenster steht: Menüband, Kopfband, Startseite. **Dieser Punkt bleibt offen, bis ihn ein Windows-Gerät zeigt** — er ist auf Linux grundsätzlich nicht nachweisbar (kein WebView2, keine WinForms-Nachrichtenschleife), und genau hier ist die Teilwelle am 04.09.2026 gescheitert (**Befund W16c‑B12**: `TargetInvocationException` an `Program.Main:332`). Die zwei Wachen dagegen — die Reflexionsprüfung in `BlazorSeite<T>` und die fünf bunit-Fälle über den Weg der Hülle — greifen ab jetzt, ersetzen den Punkt aber nicht |
| 1 | **Erststart** auf einem Rechner ohne Datenbank | Unverändert: Erststartdialog, Lizenzvereinbarung, dann das Hauptfenster. Beide Dialoge laufen besitzerlos VOR dem Fenster (W15c) und sind vom Rückbau nicht berührt |
| 2 | **Lizenz** — Zustände, Karenz, „Lizenz…" im Menü Administration | Unverändert; die stille Nachprüfung läuft beim Start weiter im Hintergrund |
| 3 | **Alle 55 Menüpunkte durchklicken** | **VIER Köpfe** (W16c‑E‑2): Projekt 8 (+2 Trenner-Gruppen), Administration 11 mit sieben Untermenüs, Hilfe 4 und ganz rechts **„Sprache" mit genau zwei Einträgen** (Deutsch, Englisch — mit ihren Fahnen). Jeder Punkt führt in denselben Dialog wie vorher |
| 3a | **Wo „Sprache“ steht** (W16c‑E‑4) | Die drei Köpfe Projekt, Administration und Hilfe stehen links beieinander; **„Sprache“ sitzt am RECHTEN Rand der Leiste**, mit Abstand zu ihnen — so wie im Bestand „Deutsch“/„Englisch“. Beim Verkleinern des Fensters rutscht er mit dem Rand nach links, springt aber nicht in die Reihe der drei |
| 4 | **Die drei Ebenen**: Administration → Energiesysteme → Photovoltaik → Bearbeiten | Das Untermenü der dritten Ebene klappt seitlich eingerückt auf |
| **4a** | **Befund W16c‑B13 (05.09.2026) — die Untermenüs mit der MAUS.** „Administration" anklicken, dann **„Wärmebedarf & Heizung ▸"** anklicken; danach **„Strombedarf & Speicher ▸"**; dann irgendwo **neben** das Menü klicken | Der erste Klick klappt sechs Einträge eingerückt darunter auf (Brauchwasser, Heizkessel, Prozesswärme, Pufferspeicher, Wärmebedarf extern, Wärmepumpe) — **„Administration" bleibt dabei offen**. Der zweite schließt das erste Untermenü und öffnet an seiner Stelle das zweite: **nie zwei nebeneinander**. Ein zweiter Klick auf denselben Punkt klappt ihn wieder zu. Ein Klick auf einen **Blattpunkt** („Einstellungen", „Brauchwasser") führt in den Dialog und schließt das ganze Menü. Der Klick daneben schließt es **ohne** etwas auszulösen — auch ein Klick auf eine Kachel der Startseite, der schließt dann nur das Menü |
| 5 | **Tastatur im Menü**: ← → ↓ Pos1 Ende Esc | Wandern über die **vier** Köpfe, Öffnen (auch von „Sprache"), Schließen. Ende springt auf „Sprache", nicht mehr auf „Englisch". Tab verlässt die Leiste nach EINEM Druck |
| **5a** | **Befund W16c‑B13 — die Untermenüs mit der TASTATUR.** Mit `→` auf „Administration", `↓` hinein, `↓ ↓` bis „Energiesysteme", `→`, `→`, dann `←`, `←`, dann `Esc` | `↓` öffnet das Menü und stellt den Balken auf den **ersten** Eintrag; `↓ ↑` wandern darin und laufen am Ende um. `→` öffnet das Untermenü des markierten Punktes und springt auf dessen ersten Eintrag — so bis in die **dritte** Ebene („Photovoltaik" → „Bearbeiten…"). `←` schließt **genau eine** Ebene und stellt den Balken zurück auf ihren Kopf, nicht das ganze Menü. `Enter` oder Leertaste auf einem Blattpunkt führt in den Dialog. `Esc` und `Tab` schließen alles |
| 6 | **F1** irgendwo im Fenster | Der KI-Assistent geht auf — auch wenn der Fokus in der WebView steht |
| 7 | **Kopfband** | „EPOS-Plan", darunter „Energieplanungs-Software · Energie · Planung · Optimierung · Simulation", rechts der Fragezeichenknopf (Ziel „Programmablauf") und „Version x.y.z" |
| 7a | **Die Farbgebung von Menüband und Kopfband** (W16c‑E‑5) | Das Menüband ist **hellblau** (AliceBlue) und seine vier Köpfe stehen in derselben Schriftgröße wie die Reiter der Startseite — nicht in Dialogschrift. Der Produktname „EPOS-Plan" ist **deutlich größer** als die Zeile darunter, Gattung, Claim und Version stehen **klein und grau**. Die Haarlinien unter Menüband und Kopfband sind **kühl blaugrau**, nicht beige |
| 8 | **21 Kacheln der Startseite**, Projektwechsel im Kopfband, Klimaregion | Das Menüband steht darüber und verschwindet nie. **Seit dem 05.09.2026 (W16b‑E‑3 / W16b‑E‑4):** Jede Kachel trägt ihr Sinnbild, der Klimakasten steht links und der Projektkasten rechts, und die Zeile „Energieplanungs-Software“ steht nur noch EINMAL — im Kopfband des Fensters |
| 9 | **Assistent** über Menü „Projekt → Neu…" und „→ Bearbeiten…" | Modal wie bisher; danach zeigt die Startseite das neue Projekt (Nachzug des Kontexts) |
| 10 | **Simulation**: „Simulation Konfiguration…" und die Ergebniskachel | Konfiguration als freie Ansicht, Ergebnis als Überlagerung — **mit dem Menüband darüber** |
| 11 | **Bericht**: Menü „Projekt → Varianten und Bericht…" | **W16c‑E‑3:** Die ANSICHT wechselt auf „Berichte & Kosten" (Seite „Übersicht"), die Startseite ist abgelöst, das Menüband steht darüber. Links oben in der Kopfzeile steht **„◀ Zurück"** und führt zur Startseite zurück. Der **sechste Reiter der Startseite zeigt dieselbe Seite** und dort **ohne** Rückwegknopf — beide Wege müssen denselben Stamm und dieselbe Markierung zeigen |
| 12 | **Sprachwechsel** auf Englisch und zurück — jetzt über „Sprache → Englisch" | Alle 55 Menüpunkte englisch, der Kopf heißt **„Language"**, Kopfband englisch („Energy planning software · Energy · Planning · Optimisation · Simulation"), „Über EPOS-Plan" → „About EPOS-Plan". Das Programm startet neu |
| 13 | **DPI 100 / 125 / 150 %** — Menüband, Kopfband, Startseite, ein modaler Dialog | **Scharf** (Per Monitor V2). Das ist der Unterschied zu allen Wellen davor |
| 14 | **DPI: `Form_HelpPopup`** (Fragezeichenknopf → Sprechblase) und **`Form_SpeicherOptimierung`** (aus der Ergebnisseite) | Die zwei letzten WinForms-Fenster. **Hier könnte eine echte Abweichung auftreten** — sie sind die einzigen Masken, die von der DPI-Umstellung betroffen sind |
| 15 | **Zwei Monitore mit verschiedener Skalierung**, Fenster hinüberziehen | Der eigentliche Gewinn von „Per Monitor V2" — unter DpiUnaware gar nicht möglich |
| 16 | **Setup** auf einem Rechner ohne WebView2 | Die Laufzeit wird nachinstalliert; ohne sie meldet `Program.Main` die Bezugsquelle und beendet (W15c) |

---

## 11 — Was iU11 erbt

| Was | Zustand |
|---|---|
| **`Form_HelpPopup`** | die **letzte** Designer-Maske (Entscheid W15b‑E‑2). Sie fällt mit `HelpCatalog`/`HelpExtender`; ihr Ersatz `IHilfeDienst` steht mit Windows- und iOS-Fassung |
| **`Sprungbruecke`** | **ein** Zweig: `Sprungziel.SpeicherOptimierung` → `Form_SpeicherOptimierung` (iF22, der einzige Ort mit ScottPlot). Sie ist der Beweis, dass die Mischphase EINEN Übergang behält |
| **`Hauptfensterrahmen`** | die Hülle, 129 Zeilen, ohne Designer, seit dem 04.09.2026 unter diesem Namen in `Views/Hauptformular/` (E‑10, Commit `7ed320b`; vorher `MDIMainForm` in der Projektwurzel). Der Rahmen trägt `Application.Run`, den `BlazorWebView`, F1 und den Sprachwechsel — **er bleibt Windows**, auch wenn alles darin Razor ist |
| **W16b‑O‑3** | `IosProjektKontext` liest die Klimazone anders als der Kern (Befund W16b‑B2) — auf `ProjektKontextCtrl` zu ziehen |
| **`IProjektQuelle` auf iOS** | drei Glieder laufen dort in die Standardumsetzung: `StartseiteGaben`, `BerichteKostenGaben` (beide `null`) und `IDateiDienst.AdresseOeffnen` (`false`). `AppWurzel` sagt es im Banner, statt leer zu bleiben |
| **`Erreichbarkeit`** | `Wurzelmasken` führt `Hauptfensterrahmen` (die Hülle, keine Maske; seit E‑10, vorher `MDIMainForm`), `Wurzelklasse` `Program`. Fällt `Form_HelpPopup`, ist der Graph leer — dann verliert auch `DerBestandFuehrtKeineUngeklaerteMaskeMehr` seinen Gegenstand |
| **`WFO1000`** | steht bei 0; die Herabstufung in der `.editorconfig` kann mit `Form_HelpPopup` entfallen |
| **W16b‑O‑2** | `ProjektTransferDialogTests.Schliessen_meldet_ob_ein_Import_gelungen_ist` ist flatterhaft — in dieser Teilwelle **nicht** aufgetreten (vier Gesamtläufe, davon einer unter `en_US`) |

---

## 12 — Offene Punkte

| # | Punkt |
|---|---|
| ~~**W16c‑O‑1**~~ | ~~Die Umbenennung `MDIMainForm` (E‑10) steht aus~~ — **erledigt** (04.09.2026, Commit `7ed320b`): Die Klasse heißt `Hauptfensterrahmen` und liegt in `Views/Hauptformular/`, `Program.mdifrm` heißt `rahmen`, `Erreichbarkeit.Wurzelmasken`, `HilfeKontext.BEREICH_JE_TYP` und die zwei Prüfmuster-Auszüge sind nachgezogen. `help_mapping.txt` war nicht betroffen (kein Schlüssel dieses Fensters mehr, seit W16c.6 heißt die Fensterhilfe `Hauptfenster.btn_Help`). Gate: 0 Fehler / 6 Warnungen, 122 Formularkarte-Fälle, 1 ja / 0 nein / 0 verwaist / 0 unklar |
| **W16c‑O‑2** | **Die DPI-Abnahme steht aus.** Auf Linux ist nur der Bau prüfbar; die 16 Punkte in § 10 (besonders 13–15) brauchen ein Windows-Gerät. `Form_HelpPopup` und `Form_SpeicherOptimierung` sind die zwei Kandidaten für eine echte Abweichung |
| ~~**W16c‑O‑3**~~ | ~~Der Menüpunkt „Deutsch"/„Englisch" bleibt ein Kopf erster Ebene~~ — **erledigt** (04.09.2026, Commit `03c5947`): Der Anwender hat **W16c‑E‑2** zugunsten des Untermenüs entschieden. „Sprache" ist ein Kopf, die zwei Punkte hängen darunter; N4 führt seither 55 Punkte in vier Köpfen. **Nachtrag 05.09.2026:** Der Kopf steht seit **W16c‑E‑4** auch wieder rechtsbündig — dort, wo im Bestand die zwei Sprachpunkte sassen |
| **W16c‑O‑6** (neu) | **Die Windows-Abnahme der zwei Entscheide steht aus.** Auf Linux ist beides nur als bunit-Fall geprüft: dass „Sprache" aufklappt und der Klick meldet (W16c‑E‑2), und dass „Varianten und Bericht…" die Ansicht wechselt und „◀ Zurück" zurückführt (W16c‑E‑3). Was ein Windows-Gerät zeigen muss, steht als Punkt 3, 5, 11 und 12 in § 10 — dazu **Befund W16c‑B11**: dass die Anwendung überhaupt startet, ist auf Linux nicht nachweisbar |
| **W16c‑O‑7** (neu, 04.09.2026) | **Der Abnahmepunkt „Start" (§ 10, Punkt 0) bleibt offen.** `W16c‑B12` hat gezeigt, was W16c‑O‑6 nur vermutet hatte: Der Start ist der einzige Punkt, den **jeder** Fehler der Hülle trifft, und der einzige, den Linux gar nicht prüfen kann. Zweimal hintereinander fehlte etwas, das **nur die Hülle** beisteuert — das Dienstverzeichnis (`B11`) und der Parametersatz (`B12`). Die zwei Wachen aus `B12a` decken künftig die zweite Art ab; für die erste gibt es keine, weil `@inject` erst zur Laufzeit auflöst. **Erste Handlung nach jedem Rückbau an der Hülle: starten.** |
| **W16c‑O‑4** | **`Seitenschluessel` führt 34 Werte in einer Klasse** — Ansichten, Masken und Wege nebeneinander. Das ist gewollt (das Menüband kennt genau eine Schlüsselart, N4 prüft an einem Ort), aber die Klasse ist mit 319 Zeilen die größte Konstantenklasse des Hauses. Wenn iU11 sie teilt, dann entlang „Ansicht / Maske / Weg" — und mit einem gemeinsamen `Alle` |
| **W16c‑O‑5** | **Das Menüband hat keine Freischaltung nach Projektzustand.** Der Bestand hatte sie auch nicht (`WinFormsNavigation.MenueAktualisieren` ist seit iU5 leer, mit Begründung); die Reitersperre der Startseite trägt das. Wer sie je will, hat mit der Tabelle jetzt den Ort dafür |

---

## Anwenderwunsch W16c‑E‑6 (06.09.2026) — Administration-Menü umgeordnet

> **Zur Kennung.** Der Auftrag nannte diesen Wunsch „W16c‑E‑5". Diese Kennung
> war schon vergeben: **W16c‑E‑5** ist seit dem 05.09.2026 die Farbgebung von
> Menüband und Kopfband (§ 6, umgesetzt in `04d5ac6`, mit eigenem Statusblock
> im Umsetzungskonzept). Zwei Entscheide unter einer Kennung machten das
> Register unbrauchbar; der Wunsch läuft deshalb als **W16c‑E‑6**, der nächsten
> freien Nummer. Wer nach „W16c‑E‑5 / Administration" sucht, findet ihn hier.

**Wortlaut des Anwenders (06.09.2026):** „Administration: Verschiebe BHKW von
Energiesystem in ‚Wärmebedarf & Heizung'. Verschiebe Solarkollektoren von
Energiesystem in ‚Wärmebedarf & Heizung'. Verschiebe Pufferspeicher von
‚Wärmebedarf & Heizung' in Energiesystem. Erstelle in ‚Wärmebedarf & Heizung'
Unterrubrik ‚Profile & Lastgänge'; verschiebe in diese Rubrik: ‚Wärmebedarf
Lastgang', ‚Prozesswärme', ‚Solarthermieganglinie' (aus Menü Energiesystem)."

**Der Ordnungsgedanke dahinter** — er steht hier, weil er die zwei
Entscheidungen unten trägt: Die Rubrik „Wärmebedarf & Heizung" sammelt danach
die **Wärmeerzeuger** (was Wärme macht), „Energiesysteme" die **Anlagen, die
Strom erzeugen oder Wärme puffern**, und die neue Unterrubrik die
**Zeitreihen** (was einen Verlauf über das Jahr beschreibt, kein Gerät).

### Vorher / Nachher — nur der Kopf „Administration"

```
VORHER — 55 Punkte, 13 aufklappende      NACHHER — 54 Punkte, 12 aufklappende

Administration                           Administration
├─ Wärmebedarf & Heizung ▸               ├─ Wärmebedarf & Heizung ▸
│  ├─ Brauchwasser                       │  ├─ Brauchwasser
│  ├─ Kessel                             │  ├─ Kessel
│  ├─ Prozesswärme              [3]      │  ├─ Wärmepumpe
│  ├─ Pufferspeicher            [2]      │  ├─ BHKW                       [1]
│  ├─ Wärmebedarf Lastgang      [3]      │  ├─ Solarkollektoren        [1][4]
│  └─ Wärmepumpe                         │  └─ Profile & Lastgänge ▸      [3]
├─ Strombedarf & Speicher ▸              │     ├─ Wärmebedarf Lastgang
│  └─ (4 Punkte, unverändert)            │     ├─ Prozesswärme
├─ Energiesysteme ▸                      │     └─ Solarthermieganglinie
│  ├─ Photovoltaik ▸            [4]      ├─ Strombedarf & Speicher ▸
│  │  └─ Bearbeiten                      │  └─ (4 Punkte, unverändert)
│  ├─ Solarkollektoren ▸     [1][4]      ├─ Energiesysteme ▸
│  │  └─ Bearbeiten                      │  ├─ Photovoltaik               [4]
│  ├─ Solarthermieganglinie     [3]      │  └─ Pufferspeicher             [2]
│  └─ BHKW                      [1]      ├─ Klimadaten & Umgebung ▸
├─ Klimadaten & Umgebung ▸               ├─ Daten & Import ▸
├─ Daten & Import ▸                      ├─ Kostenverwaltung ▸
├─ Kostenverwaltung ▸                    ├─ Gebäude ▸
├─ Gebäude ▸                             ├─ Einstellungen
├─ Einstellungen                         ├─ Gesetzliche Parameter
├─ Gesetzliche Parameter                 ├─ Katalogdubletten
├─ Katalogdubletten                      └─ Lizenzverwaltung
└─ Lizenzverwaltung

[1] von „Energiesysteme" nach „Wärmebedarf & Heizung"
[2] von „Wärmebedarf & Heizung" nach „Energiesysteme"
[3] in die NEUE Unterrubrik „Profile & Lastgänge"
[4] Untermenü mit einem einzigen Punkt „Bearbeiten" aufgelöst
```

Die acht Rubriken darunter (Klimadaten & Umgebung bis Lizenzverwaltung) und die
drei anderen Köpfe (Projekt, Hilfe, Sprache) sind **unberührt**.

### Die Zahlen

| | vorher | nachher | warum |
|---|---|---|---|
| Punkte (ohne Trenner) | 55 | **54** | +1 Unterrubrik, −2 aufgelöste „Bearbeiten"-Punkte |
| davon **handelnd** | 42 | **42** | **unverändert** — kein Ziel entfallen, keines hinzugekommen |
| davon aufklappend | 13 | **12** | +1 Unterrubrik, −2 aufgelöste Untermenüs |
| Trennstriche | 8 | 8 | unberührt |
| Bilder | 11 | 11 | unberührt — sie hängen an den Rubrikköpfen, nicht am Inhalt |
| Ziele unter „Administration" | 28 | **28** | dieselbe Menge, andere Stelle im Baum |
| Menütiefe | 3 Ebenen | 3 Ebenen | der dreistufige Weg heißt nur anders (s. u.) |

**Die 42 ist die Zahl, an der die Vollzähligkeit hängt.** Punkte, die nur
aufklappen, sind Wegweiser; ein Ziel ist ein Weg in die Anwendung. Deshalb
prüft der Nachweis N4 seit W16c‑E‑6 nicht nur Zahlen, sondern die **Menge der
28 Ziele** unter „Administration" und den Weg jedes verschobenen Punktes.

### Zwei Entscheidungen zu den Ein-Punkt-Untermenüs

**Beide aufgelöst.** „Photovoltaik ▸ Bearbeiten" und „Solarkollektoren ▸
Bearbeiten" führten je **genau einen** Punkt, und dieser Punkt hieß
„Bearbeiten" — ein Wort, das nichts sagt, was der Vater nicht schon sagt. Der
Klick darauf war reine Wegzoll: aufklappen, um das Einzige zu wählen, was da
steht. `MenuItem_PV` und `MenuItem_Solarkollektoren` tragen jetzt selbst das
Ziel ihres früheren Kindes (`Seitenschluessel.PvAdmin` bzw.
`…SolarkollektorenAdmin`), und `MenuItem_PC_Bearbeiten` /
`MenuItem_ST_Bearbeiten` fallen weg.

Für **Solarkollektoren** kam der Anlass aus dem Wunsch selbst: Der Punkt zieht
nach „Wärmebedarf & Heizung" und steht dort zwischen lauter unmittelbar
handelnden Geschwistern (Brauchwasser, Kessel, Wärmepumpe, BHKW). Ein einzelnes
„▸ Bearbeiten" mitten darin wäre ein Bruch in der Reihe.

Für **Photovoltaik** war die Lage nach der Umordnung dieselbe: „Energiesysteme"
führt nur noch zwei Punkte, und **Pufferspeicher** handelt unmittelbar. Bliebe
Photovoltaik aufklappend, hätte eine Rubrik mit zwei Zeilen zwei verschiedene
Bedienweisen. Der Anwender hat für Solarkollektoren gefragt, „ob das Untermenü
mit nur einem Punkt noch sinnvoll ist" — die Antwort gilt für beide gleich.

**Was das kostet:** die zwei Textschlüssel `MENU_PC_BEARBEITEN` und
`MENU_ST_BEARBEITEN` werden vom Menü nicht mehr gelesen. Sie bleiben im
Katalog stehen (Löschen brächte keinen Gewinn und einen Designer-Lauf mehr);
die Tabelle sagt im Kopfkommentar, dass sie verwaist sind.

**Was das NICHT kostet:** die Menütiefe. Bis W16c‑E‑6 war „Administration ▸
Energiesysteme ▸ Photovoltaik ▸ Bearbeiten" der **einzige** dreistufige Weg des
Bestands — an ihm hing der Nachweis, dass `epos-menueband-klappe--tief` über
drei Ebenen trägt (Befund W16c‑B13). Diesen Weg gibt es nicht mehr; an seine
Stelle tritt **„Administration ▸ Wärmebedarf & Heizung ▸ Profile & Lastgänge ▸
Wärmebedarf Lastgang"**. Die zwei Fälle, die den alten Weg gingen
(`MenuebandTests.Ein_Punkt_der_dritten_Ebene_meldet_und_schliesst_das_ganze_Band`
und `HauptfensterTests.Ein_Punkt_der_dritten_Ebene_landet_im_selben_Handler`),
gehen jetzt den neuen — der Befund bleibt bewacht.

### Die neue Unterrubrik

| | |
|---|---|
| Name | `MenuItem_ProfileLastgaenge` — die Namenskonvention der Rubriken; anders als der Kopf „Sprache" (der auf der obersten Ebene steht) |
| Textschlüssel | `MENU_PROFILE_LASTGAENGE`, de **„Profile & Lastgänge"**, en **„Profiles & load curves"** |
| Ziel | **keines** — sie klappt nur auf |
| Bild | **keines** — es gibt kein PNG unter `wwwroot/bilder/menue/`, das sie meinte; dieselbe Lage wie beim Kopf „Sprache" |
| Herkunft | **keine Designer-Herkunft.** Sie ist nach „Sprache" die zweite solche Zeile und steht deshalb im Kopfkommentar der `Menuetabelle` |
| Reihenfolge darin | Wärmebedarf Lastgang, Prozesswärme, Solarthermieganglinie — wörtlich die des Wunsches |

Das Kaufmannsund steht **einfach** da („Profile & Lastgänge"), nicht verdoppelt
— dieselbe Angleichung **A‑2**, die schon „Daten & Import" und „Wärmebedarf &
Heizung" betrifft: WinForms verdoppelt `&` für das Tastenkürzel, Razor nicht.

### Was sich NICHT geändert hat

* **Namen, Seitenschlüssel, Bilder und Kürzel** aller verschobenen Punkte —
  es wandert die Zuordnung, nicht die Kennung. `HauptfensterHuelle.Weg` und die
  Maskenschlüssel des Kerns sind unberührt.
* **`help_mapping.txt`** — die Datei nennt **keinen** `MenuItem_*`-Anker
  (nachgesehen: 0 Treffer). Der Menüumbau berührt die Hilfe nicht.
* **`Start/Kachelbilder.cs` und die Startseite** — die 21 Kacheln und die sechs
  Reiter sind nicht Gegenstand dieses Wunsches. Nur das Menüband ist umgeordnet.
* **`Menueband.razor`** — kein Zeichen Programmtext. Die Rekursion in
  `Untermenue(eltern, ebene)` trägt jede Tiefe; die Umordnung ist reine
  Datenänderung in `Menuetabelle.cs`. Das ist die Probe darauf, dass „das Menü
  ist Daten" trägt.

### Nachweis

| Was | Ergebnis |
|---|---|
| `Werkzeuge/ResourceDesigner/designer_neu.py` | `Eintraege: 4876 (vorher 4876); Bloecke gleich 4876, abweichend 0, neu 0` — der eine neue Schlüssel steht im Designer, von Hand ergänzt wurde nichts |
| Bau `EPOS.Kern` + `EPOS.UI` + `EPOS.UI.Tests` | 0 Fehler, 7 Warnungen (alle vorbestehend) |
| `dotnet test` unter `LANG=de_DE.UTF-8` | **2692 / 2692 grün** (vorher 2683) |
| `dotnet test` unter `LANG=en_US.UTF-8` | **2692 / 2692 grün** (vorher 2683) |
| neue Fälle | **9** in `MenuebandTests` (Abschnitt „Anwenderentscheid W16c‑E‑6") |
| geänderte Fälle | 5 — die zwei Zählfälle, der Fall über die sechs Punkte der Rubrik und die zwei dreistufigen Wege (einer je Testklasse) |

Die neun neuen Fälle prüfen: die **sechs Punkte** von „Wärmebedarf & Heizung"
in ihrer Reihenfolge; die Unterrubrik mit **genau drei** Punkten in der
gewünschten Reihenfolge, ohne Ziel und ohne Bild; „Energiesysteme" mit
Photovoltaik und Pufferspeicher, **beide handelnd**; dass
`MenuItem_PC_Bearbeiten` und `MenuItem_ST_Bearbeiten` **nicht mehr vorkommen**
und ihre Väter das Ziel geerbt haben; dass **kein** verschobener Punkt noch in
seiner alten Rubrik steht (die Gegenprobe — der Eindeutigkeitsfall über die
Namen liest den Baum flach und fände ein Doppel nicht); dass **jeder**
verschobene Punkt seinen Seitenschlüssel, sein Bild und sein Kürzel behält;
dass die **Menge der 28 Ziele** unter „Administration" dieselbe ist wie vorher;
dass die neue Rubrik in **beiden Sprachen** beschriftet ist; und am gezeichneten
Band, dass der Weg über drei Ebenen bis zu den drei Zeitreihen führt und der
Pufferspeicher drüben bei den Energiesystemen steht.

> **Zum flatterhaften Fall.** Der erste Gesamtlauf unter `de_DE` fiel mit
> `ProjektTransferDialogTests.Schliessen_meldet_ob_ein_Import_gelungen_ist`
> rot aus; der Fall läuft allein und in den drei folgenden Gesamtläufen grün.
> Das ist **W16b‑O‑2** (§ 11) und hat mit dem Menü nichts zu tun.

### Abnahmepunkte A‑W16c‑E‑5 (Windows)

Auf Linux ist nur der bunit-Fall geprüft. Was ein Windows-Gerät zeigen muss:

| # | Punkt | Erwartung |
|---|---|---|
| **A‑W16c‑E‑5‑1** | Administration ▸ **Wärmebedarf & Heizung** | Sechs Zeilen in dieser Reihenfolge: Brauchwasser, Kessel, Wärmepumpe, BHKW, Solarkollektoren, **Profile & Lastgänge ▸**. Nur die letzte trägt den Pfeil |
| **A‑W16c‑E‑5‑2** | … ▸ **Profile & Lastgänge** | Klappt seitlich auf und zeigt drei Zeilen: Wärmebedarf Lastgang, Prozesswärme, Solarthermieganglinie. Jede öffnet ihre Maske |
| **A‑W16c‑E‑5‑3** | Administration ▸ **Energiesysteme** | Zwei Zeilen: **Photovoltaik** und **Pufferspeicher**, beide **ohne** Pfeil. Ein Klick auf Photovoltaik öffnet unmittelbar die PV-Verwaltung (kein „Bearbeiten" mehr), ein Klick auf Pufferspeicher die Pufferspeicher-Verwaltung |
| **A‑W16c‑E‑5‑4** | **BHKW und Solarkollektoren** | Beide stehen unter „Wärmebedarf & Heizung" und öffnen dieselben Masken wie vorher unter „Energiesysteme" |
| **A‑W16c‑E‑5‑5** | **Tastaturweg** | Mit ← → auf „Administration", ↓ öffnet den Kopf, → öffnet „Wärmebedarf & Heizung", **5 × ↓** steht auf „Profile & Lastgänge", → öffnet sie, Enter auf „Wärmebedarf Lastgang" öffnet die Maske. Zweimal ← führt Ebene um Ebene zurück, Esc schließt alles |
| **A‑W16c‑E‑5‑6** | **Englisch** | Unter „Language ▸ English" heißt die Unterrubrik **„Profiles & load curves"**, die Rubriken „Heat requirement & heating" und „Energy systems" wie bisher |
| **A‑W16c‑E‑5‑7** | **Die dritte Ebene am Bildschirmrand** | „Profile & Lastgänge" ist die **letzte** Zeile ihrer Klappe und öffnet weiter rechts als der bisherige dreistufige Weg. Zu prüfen ist, dass die Klappe der dritten Ebene nicht am rechten Fensterrand abgeschnitten wird — auch bei 150 % Skalierung und schmalem Fenster |

Die Punkte tragen die Kennung **A‑W16c‑E‑5**, wie im Auftrag verlangt; der
Entscheid selbst läuft aus dem oben genannten Grund als **W16c‑E‑6**.

### Berührte Dateien

| Datei | Was |
|---|---|
| `EPOS.UI/Bausteine/Menuetabelle.cs` | der Kopf „Administration" umgeordnet; Kopfkommentar um den Absatz zu W16c‑E‑6 und um „die Datei ist seit W16c die Quelle" erweitert; Zahlen im Klassenkommentar (54 / 42 / 12) |
| `EPOS.Kern/MyResource/Resource.resx`, `.en-US.resx` | **ein** neuer Schlüssel `MENU_PROFILE_LASTGAENGE` |
| `EPOS.Kern/MyResource/Resource.Designer.cs` | per `designer_neu.py schreiben` erzeugt |
| `EPOS.UI.Tests/Bausteine/MenuebandTests.cs` | 9 neue Fälle, 4 nachgezogen |
| `EPOS.UI.Tests/Seiten/HauptfensterTests.cs` | 1 Fall nachgezogen (der dreistufige Weg), 1 Kommentar |
| `EPOS.UI/Bausteine/Menueband.razor`, `EPOS.UI/Seiten/Hauptfenster.razor`, `EPOS.UI/Seiten/Seitenschluessel.cs`, `WindowsFormsApplication1/Views/Hauptformular/Hauptfensterrahmen.cs` | nur die Zahl 55 → 54 im Kommentar |
| `CLAUDE.md`, `EPOS.UI/CLAUDE.md`, `WindowsFormsApplication1/CLAUDE.md`, `EPOS.iOS/CLAUDE.md` | Zahl und Entscheid nachgetragen |
| dieses Protokoll | § 4 (Zahlen, Nachtrag) und dieser Abschnitt |

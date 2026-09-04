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

Damit führt die Tabelle **55 Punkte** (54 des Bestands + 1) in **vier Köpfen**
der obersten Ebene — Projekt, Administration, Hilfe, Sprache. Von den 55
**handeln 42** (unverändert: der neue Kopf handelt nicht) und **13 klappen auf**
(12 + „Sprache"); Trennstriche und Bilder sind unverändert 8 und 11. Der
Menüpunkt „Varianten und Bericht…" meldet weiterhin `BERICHTE_KOSTEN` — was
sich mit W16c‑E‑3 geändert hat, ist nicht die Tabelle, sondern was die Hülle
damit tut (§ 6).

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
| **W16a‑E‑1 / W16b‑O‑5** | Wird der Assistent eine freie Ansicht? | **W16c hat es nicht getan** (Auflage der Anweisung). Er bleibt eine modale `BlazorDialogForm`; `Seitenschluessel.ProjektNeu`/`…Bearbeiten` gehen über `HauptfensterHuelle.Weg` in dieselbe Hülle wie bisher — samt dem Nachzug des Projektkontexts („Befund 3"). **Anwenderfrage bleibt offen** |
| **W16b‑E‑1 / W16b‑E‑2** | (aus W16b) | **Nicht angefasst** |
| **W16c‑E‑1 (neu)** | **Das Menü klappt beim KLICK auf, nicht beim Überfahren** (A‑1) | Der `MenuStrip` öffnete ein Untermenü, sobald die Maus darüber stand — und schloss das vorige. Wer das Menü mit der Maus „durchfährt", muss jetzt jeden Kopf anklicken. Die Tastaturbedienung ist dieselbe geblieben (← → ↓ Pos1 Ende Esc). **ok** (Anwender, 04.09.2026) |
| **W16c‑E‑2** | Die zwei Sprachpunkte als eigene Köpfe neben „Hilfe" — oder ein Untermenü „Sprache"? | **Anwenderentscheid 04.09.2026: Untermenü „Sprache" — umgesetzt** (Commit `03c5947`). Der Bestand führte fünf Köpfe (`menuToolbar.Items` = Projekt, Administration, Hilfe, Deutsch, Englisch); W16c hatte das wörtlich übernommen, weil die Welle nichts umbauen sollte, was sie nur umzieht. Jetzt steht **ganz rechts, wo „Deutsch" stand, der Kopf „Sprache"** (`MENU_SPRACHE`, en „Language"); er klappt nur auf, die zwei Punkte sind seine Untereinträge und behalten Name, Bild (`germany`/`usa`) und Seitenschlüssel — `help_mapping.txt` und `HauptfensterHuelle.Weg` greifen unverändert, `Application.Restart()` bleibt. Zahlen: **55** Punkte, **4** Köpfe, **13** aufklappende, 42 handelnde, 8 Trenner, 11 Bilder |
| **W16c‑E‑3** | Holt „Varianten und Bericht…" den sechsten Reiter der Startseite nach vorn — oder wechselt er die Ansicht? | **Anwenderentscheid 04.09.2026: Ansichtswechsel — umgesetzt** (Commit `74e0cc1`). Bis dahin wörtlich der Bestand (`MenuItem_VariantenBericht_Click` → `StartseiteHuelle.Aktuelle.ZeigeBerichteKosten`), und `BERICHTE_KOSTEN` war allein der **iOS**-Weg. Jetzt ist es der Weg **beider** Plattformen: Der Fall in `HauptfensterHuelle.Weg` ist gefallen — der Schlüssel steht in `Ansichten`, nicht in `Masken`, also meldet `MaskeOeffnen` false und `Hauptfenster.Springe` lässt die `AppWurzel` auf die Ansicht wechseln. **Das sechste Reiterblatt bleibt bestehen** (dieselbe Komponente, dieselbe `BerichteKostenHuelle`); nur der Menüweg führt in die Ansicht. Der Rückweg geht über `ZurueckZurListe` auf die `Startansicht` — dafür hat `BerichteKostenSeite` einen `Geschlossen`-Rückruf und den Knopf `BK_BTN_ZURUECK` bekommen, den es **ohne** Rückruf (also im Reiterblatt) nicht gibt |

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
| 1 | **Erststart** auf einem Rechner ohne Datenbank | Unverändert: Erststartdialog, Lizenzvereinbarung, dann das Hauptfenster. Beide Dialoge laufen besitzerlos VOR dem Fenster (W15c) und sind vom Rückbau nicht berührt |
| 2 | **Lizenz** — Zustände, Karenz, „Lizenz…" im Menü Administration | Unverändert; die stille Nachprüfung läuft beim Start weiter im Hintergrund |
| 3 | **Alle 55 Menüpunkte durchklicken** | **VIER Köpfe** (W16c‑E‑2): Projekt 8 (+2 Trenner-Gruppen), Administration 11 mit sieben Untermenüs, Hilfe 4 und ganz rechts **„Sprache" mit genau zwei Einträgen** (Deutsch, Englisch — mit ihren Fahnen). Jeder Punkt führt in denselben Dialog wie vorher |
| 4 | **Die drei Ebenen**: Administration → Energiesysteme → Photovoltaik → Bearbeiten | Das Untermenü der dritten Ebene klappt seitlich eingerückt auf |
| 5 | **Tastatur im Menü**: ← → ↓ Pos1 Ende Esc | Wandern über die **vier** Köpfe, Öffnen (auch von „Sprache"), Schließen. Ende springt auf „Sprache", nicht mehr auf „Englisch". Tab verlässt die Leiste nach EINEM Druck |
| 6 | **F1** irgendwo im Fenster | Der KI-Assistent geht auf — auch wenn der Fokus in der WebView steht |
| 7 | **Kopfband** | „EPOS-Plan", darunter „Energieplanungs-Software · Energie · Planung · Optimierung · Simulation", rechts der Fragezeichenknopf (Ziel „Programmablauf") und „Version x.y.z" |
| 8 | **21 Kacheln der Startseite**, Projektwechsel im Kopfband, Klimaregion | Unverändert gegenüber W16b — das Menüband steht darüber und verschwindet nie |
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
| ~~**W16c‑O‑3**~~ | ~~Der Menüpunkt „Deutsch"/„Englisch" bleibt ein Kopf erster Ebene~~ — **erledigt** (04.09.2026, Commit `03c5947`): Der Anwender hat **W16c‑E‑2** zugunsten des Untermenüs entschieden. „Sprache" ist ein Kopf, die zwei Punkte hängen darunter; N4 führt seither 55 Punkte in vier Köpfen |
| **W16c‑O‑6** (neu) | **Die Windows-Abnahme der zwei Entscheide steht aus.** Auf Linux ist beides nur als bunit-Fall geprüft: dass „Sprache" aufklappt und der Klick meldet (W16c‑E‑2), und dass „Varianten und Bericht…" die Ansicht wechselt und „◀ Zurück" zurückführt (W16c‑E‑3). Was ein Windows-Gerät zeigen muss, steht als Punkt 3, 5, 11 und 12 in § 10 — dazu **Befund W16c‑B11**: dass die Anwendung überhaupt startet, ist auf Linux nicht nachweisbar |
| **W16c‑O‑4** | **`Seitenschluessel` führt 34 Werte in einer Klasse** — Ansichten, Masken und Wege nebeneinander. Das ist gewollt (das Menüband kennt genau eine Schlüsselart, N4 prüft an einem Ort), aber die Klasse ist mit 319 Zeilen die größte Konstantenklasse des Hauses. Wenn iU11 sie teilt, dann entlang „Ansicht / Maske / Weg" — und mit einem gemeinsamen `Alle` |
| **W16c‑O‑5** | **Das Menüband hat keine Freischaltung nach Projektzustand.** Der Bestand hatte sie auch nicht (`WinFormsNavigation.MenueAktualisieren` ist seit iU5 leer, mit Begründung); die Reitersperre der Startseite trägt das. Wer sie je will, hat mit der Tabelle jetzt den Ort dafür |

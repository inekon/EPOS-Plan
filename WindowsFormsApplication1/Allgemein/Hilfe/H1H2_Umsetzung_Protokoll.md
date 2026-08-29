# H1 + H2 — Umsetzungsprotokoll

**Stand:** 29.08.2026
**Grundlage:** [`Konzept_Hilfesystem_Wikidokumentation.md`](../../../Konzept_Hilfesystem_Wikidokumentation.md),
3. Fassung, Abschnitte 4 (A1–A6), 8 (H1/H2) und 11 (Fallstricke).
**Umfang:** Paket **H1** (Katalog auf MediaWiki: A1, A2, A4, A6) und **H2** (Zuordnung neu: A3).
H3 (die 23 Wiki-Seiten), H4 (`WikiWissen`), H5 (Riegel/Einwilligung) sind **nicht** Teil dieser
Umsetzung.

> **Reihenfolge-Zwang beachtet, aber offen:** Der Konzeptabschnitt 8 verlangt H3 **vor** der
> Auslieferung von H1+H2. Die Rubrik ist am 29.08.2026 noch leer (Nachweis in § 11) — dieser
> Codestand ist damit **fertig, aber noch nicht auslieferbar**.

---

## 1. Kodierungsbehandlung je Datei

Vor jeder Änderung strikt als UTF-8 gelesen (`UTF8Encoding(false, true)` in `try/catch`) und auf
`U+FFFD` geprüft. **Keine** der berührten Dateien ist CP1252 — das CP1252-Rezept kam nirgends zum
Einsatz. Der BOM-Zustand ist je Datei unverändert geblieben.

| Datei | vorher | nachher | Werkzeug |
|---|---|---|---|
| `Allgemein\Hilfe\HelpCatalog.cs` | UTF-8 +BOM | UTF-8 +BOM | Edit |
| `Allgemein\Hilfe\HilfeAutomatik.cs` | UTF-8 +BOM | UTF-8 +BOM | Edit |
| `Allgemein\Hilfe\DokuUebersetzung.cs` | *(neu)* | UTF-8 ohne BOM, **reines ASCII** (geprüft) | Write |
| `Allgemein\Hilfe\help_mapping.txt` | UTF-8 +BOM, CRLF | UTF-8 +BOM, CRLF | Write + PowerShell-Nachlauf |
| `Allgemein\Hilfe\help_cache.json` | UTF-8 ohne BOM, CRLF | UTF-8 ohne BOM, CRLF | PowerShell (erzeugt) |
| `Allgemein\KI\Aktionen\KiAktionenDialog.cs` | UTF-8 +BOM | UTF-8 +BOM | Edit |
| `Allgemein\KI\HilfeWissen.cs` | UTF-8 **ohne** BOM | UTF-8 ohne BOM | Edit, Umlaute per Rückleseprobe bestätigt |
| `Program.cs` | UTF-8 +BOM | UTF-8 +BOM | Edit |
| `MDIMainForm.cs` | UTF-8 +BOM | UTF-8 +BOM | Edit |
| `Views\Help\Form_HelpPopup.cs` | UTF-8 +BOM | UTF-8 +BOM | Edit |
| `Views\Admin\Form_AdminSettings.cs` | UTF-8 +BOM | UTF-8 +BOM | Edit |
| `MyResource\Resource.resx` | UTF-8 +BOM, CRLF | UTF-8 +BOM, CRLF | PowerShell (Einfügung vor `</root>`) |
| `MyResource\Resource.en-US.resx` | UTF-8 +BOM, CRLF | UTF-8 +BOM, CRLF | PowerShell (dito) |
| `MyResource\Resource.Designer.cs` | UTF-8 +BOM | UTF-8 +BOM | **von Visual Studio selbst regeneriert**, siehe § 8 |

Schlussprobe über alle 14 Dateien: strikt UTF-8 lesbar, kein `U+FFFD`.

---

## 2. Aufgabe 1 — Klasse `WordPressHelpCatalog` → `WikiHelpCatalog`

Reine Umbenennung, Verhalten identisch. Dateiname `HelpCatalog.cs` bleibt.

| Datei | vorher | nachher |
|---|---|---|
| `Allgemein\Hilfe\HelpCatalog.cs` | `:33` Klassenkopf, `:73` ctor, `:536` `typeof(...)`, `:618` Feld, `:622` ctor `HelpExtender` | `:45` Klassenkopf (mit neuem Klassenkommentar), `:92` ctor, `:639` `typeof(...)`, `:721` Feld, `:725` ctor |
| `Allgemein\Hilfe\HilfeAutomatik.cs` | `:54`, `:81` | `:54`, `:81` |
| `Allgemein\KI\Aktionen\KiAktionenDialog.cs` | `:142` (Doku), `:167` (`andockpunkt`), `:704` | `:142`, `:167`, `:704` |
| `Program.cs` | `:25` Eigenschaft, `:142`/`:148` | `:32` Eigenschaft, `:160` |

**Grep-Beweis** (ohne `bin/`, `obj/`): `WordPressHelpCatalog` kommt im Quellbestand **genau einmal**
vor — als Historienhinweis im Klassenkommentar `HelpCatalog.cs:40` („Hiess bis H1
`WordPressHelpCatalog`"). Kein Bezeichner, kein Aufruf.

---

## 3. Aufgabe 2 — Lader auf MediaWiki (A1)

**Vorher** `HelpCatalog.cs:355`:
```
{_baseUrl}/rest.php/v1/{Properties.Settings.Default.WordPressPrefix}?per_page=100&page={N}&_fields=slug,link,title
```
→ WordPress-Form auf MediaWiki-Host, HTTP 404, Rückfall auf den Startbestand.

**Nachher** `HelpCatalog.cs:414` (in `LoadAllCoreAsync`, ab `:382`):
```
{_baseUrl}/api.php?action=query&list=allpages&apprefix=Programm%20Dokumentation%2F&aplimit=500&format=json
```
`apprefix` wird über `Uri.EscapeDataString(RubrikPraefix)` kodiert (`RubrikPraefix` = Konstante
`"Programm Dokumentation/"`, `:88`). Fortsetzung über `continue.apcontinue`, angehängt als
`&apcontinue={Uri.EscapeDataString(...)}`, Schleife bis kein `continue` mehr gemeldet wird;
zusätzlich Abbruch, wenn ein Server denselben `apcontinue`-Wert wiederholt.

Je Seite entsteht der `HelpEntry` in `EintragAusTitel` (`:553`) über `SeitenUrl` (`:576`):

* `Tooltip` = Kurzname (Titelteil hinter `Programm Dokumentation/`)
* `Url` = `{_baseUrl}/wiki/{Titel}`, dabei **Leerzeichen → `_` VOR** der Kodierung, `'/'` bleibt
  unkodiert (Titel wird an `/` zerlegt, jedes Stück einzeln `EscapeDataString`)
* `Slug` = Kurzname

Titel ohne `Programm Dokumentation/`-Präfix und der leere Kurzname (die Rubrikseite selbst) liefern
`null` und fallen heraus.

**Beibehalten:** 10-s-`CancellationTokenSource` je Abruf, stiller `catch`, Schlüsselung über den
normalisierten Pfad (F7), Rangfolge Online → `%APPDATA%`-Sicherung → eingebetteter Startbestand,
Schreiben der Sicherung nach erfolgreichem Onlinelauf.

**Ergänzt (klein, bewusst):** Läuft der Abruf durch, meldet die Rubrik aber **null** Unterseiten,
steht das jetzt als `Debug`-Warnung im Ausgabefenster (`:492 ff.`) — vorher wäre der
H3-nicht-fertig-Zustand vollkommen unsichtbar geblieben. Die Rangfolge selbst ist unverändert
(die Bedingung `onlineLoadSuccessful && tempCache.Count > 0` stand schon vorher so da).

Weiterer Nebeneffekt: der Konstruktor schneidet einen abschließenden `/` der Basis-URL ab
(`_baseUrl = (baseUrl ?? "").TrimEnd('/')`), damit ein im Admin-Dialog eingetragenes
`https://wiki.epos-plan.de/` keine doppelten Schrägstriche erzeugt.

`Properties.Settings.Default.WordPressPrefix` wird **nirgends mehr gelesen** — siehe § 9.

---

## 4. Aufgabe 3 — Basis-URL aus den Settings (A2)

**Vorher** `Program.cs:148`:
`HelpCatalog = new WordPressHelpCatalog("https://wiki.epos-plan.de");// (Properties.Settings.Default.WordPressUrl); …`

**Nachher** `Program.cs:155–160`:
```csharp
string dokuBasis = Properties.Settings.Default.WordPressUrl;
if (string.IsNullOrWhiteSpace(dokuBasis)) dokuBasis = WIKI_STANDARD;
HelpCatalog = new WikiHelpCatalog(dokuBasis);
```
Neu daneben `Program.cs:29`:
`public const string WIKI_STANDARD = "https://wiki.epos-plan.de";` — Not-Rückfall bei leerem
Einstellwert; die Werksvorgabe in `app.config` trägt bereits dieselbe URL. Damit steuert **ein**
Einstellwert Katalog **und** Menüpunkt Dokumentation, und `txt_OnlineDokuUrl` im Admin-Dialog wirkt
wieder auf beides. Der Settings-**Schlüssel** heißt weiterhin `WordPressUrl` (Entscheid 7.3: eine
Umbenennung verwürfe gespeicherte Anwenderwerte in der `user.config`).

---

## 5. Aufgabe 4 — Anker-Durchlass (A3)

Mapping-Ziele dürfen `Ziel#anker` lauten. Der Anker wird **vor** der Katalogauflösung abgetrennt und
beim Öffnen wieder angehängt.

| Stelle | vorher | nachher |
|---|---|---|
| `WikiHelpCatalog.Aufloesen` | `:122`, entschied direkt Pfad/Slug | `:141`, schneidet zuerst bei `'#'` ab (der Pfadweg tat das schon in `PfadNormalisieren`, dem Slug-Weg fehlte es) |
| `HelpExtender.ZielAufloesen` | `:1033–1055`, `string ZielAufloesen(string)` | `:1159` Kurzform (delegiert) + `:1165` `ZielAufloesen(string, out string anker)`; die `de\|en`-Zerlegung bleibt, jede Hälfte läuft durch `AnkerAbtrennen` (`:1216`) |
| Übergabe ans Popup | `EintragHolen` gab den Katalogeintrag direkt zurück | `EintragHolen` gibt `MitAnker(entry, anker)` zurück (`:1235`) — **Kopie** des `HelpEntry`, damit der gemeinsam genutzte Katalog nicht je Formular verschmutzt wird |

Gewählte Stelle: `HelpEntry`-Kopie in `EintragHolen`. `Form_HelpPopup` und `Process.Start` bleiben
dadurch unverändert — der Anker steckt bereits in `entry.Url`.

**Mapping-Parser geprüft, keine Korrektur nötig:** `ZuordnungenAnwenden` wertet `'#'` weiterhin nur
am Zeilenanfang als Kommentar (`line.StartsWith("#")` nach `Trim('\uFEFF', ' ', '\t')`) — ein `#`
mitten in der Zeile geht unversehrt in das Ziel. Damit ist der Anker-Durchlass ohne Parseränderung
tragfähig.

---

## 6. Aufgabe 5 — EN-Übersetzungs-Wrapper (A6 / Entscheid 7.1a)

**Neu:** `Allgemein\Hilfe\DokuUebersetzung.cs` (Namespace `WindowsFormsApplication1`,
`internal static class`).

`internal static string FuerAnzeige(string url)`:
* `Program.nLanguage == 0` → Original
* Host ≠ `wiki.epos-plan.de` (Vergleich `OrdinalIgnoreCase`) → Original
* sonst Host umbauen: **erst** `-` → `--`, **dann** `.` → `-`, dann `.translate.goog`
  (`wiki.epos-plan.de` → `wiki-epos--plan-de.translate.goog`)
* Query `_x_tr_sl=de&_x_tr_tl=en&_x_tr_hl=en` mit `?` bzw. `&` angehängt
* `#anker` bleibt **hinter** der Query
* jeder Fehler (unparsbare URL, Ausnahme) → Original-URL, plus `Debug`-Zeile. Nie ein toter Link.

**Angewandt:**

| Stelle | nachher |
|---|---|
| `Views\Help\Form_HelpPopup.cs:213` | `string _anzeigeUrl = DokuUebersetzung.FuerAnzeige(_targetUrl);` vor `Process.Start` |
| `MDIMainForm.cs:830` | `_targetUrl = DokuUebersetzung.FuerAnzeige(_targetUrl);` in `MenuItem_Dokumentation_Click`, nach dem Settings-/Fallback-Zweig |

Nachweis gegen die verifizierte Referenz-URL: siehe § 11, Abschnitt 5 des Prüflaufs.

---

## 7. Aufgabe 6 — Streuverweise (A4)

| Stelle | vorher | nachher |
|---|---|---|
| `MDIMainForm.cs:808` | `DOKU_URL = "https://epos-plan.de/epos-plan/epos-plan-dokumetation/"` (Tippfehler im Original) | `:811` `public const string DOKU_URL = Program.WIKI_STANDARD;` — bleibt reiner Not-Fallback, führend ist der Settings-Wert |
| `Allgemein\KI\HilfeWissen.cs:173` | Klartext `https://epos-plan.de/epos-plan/epos-plan-dokumetation/` im Abschnitt „Dokumentation und Lizenz" | `:172–176` Wiki-URL `https://wiki.epos-plan.de`, dazu ein Satz zur Rubrik „Programm Dokumentation" und zum Bezug der Info-Schaltflächen |

Das LinkLabel „Online-Dokumentation öffnen" in `Form_KiChat.cs:249–264` folgt `DOKU_URL` und ist
damit automatisch nachgezogen (kein Codeeingriff). Lizenz-, AGB-, Impressums- und Portal-URLs auf
`epos-plan.de` blieben unberührt (bestätigte Ausnahme in Entscheid 7.3).

---

## 8. Aufgabe 7 — Popup-Linktext lokalisiert (A4)

**Vorbedingung geprüft:** Grep in `MyResource\Resource.resx` nach `HILFE`, `KAPITEL`, `DOKU`,
`POPUP` — es gab **keinen** passenden Schlüssel (nur `KI_KNOPF_HILFE`, `KI_MENUE_ASSISTENT_HILFE`,
`KI_HILFEBETRIEB_*`, alle mit anderer Bedeutung).

**Vorher** `Form_HelpPopup.cs:82–84`:
```csharp
linkLabel_Doku.Text = angeheftet
    ? $"Kapitel: {titel}\r\n➔ Hier klicken für Online-Doku\r\n(Esc oder Klick daneben schließt)"
    : $"Kapitel: {titel}\r\n➔ Hier klicken für Online-Doku";
```

**Nachher** `Form_HelpPopup.cs:83–92`: drei Bausteine aus `MyResource.Resource.*`, zusammengesetzt
mit `\r\n`; der Pfeil `➔` bleibt als sprachneutrales Zeichen im Code.

**Neue Schlüssel** (beide `.resx`):

| Schlüssel | de | en |
|---|---|---|
| `HILFE_POPUP_KAPITEL` | `Kapitel: {0}` | `Chapter: {0}` |
| `HILFE_POPUP_LINK` | `Online-Dokumentation öffnen` | `Open online documentation` |
| `HILFE_POPUP_ESC` | `(Esc oder Klick daneben schließt)` | `(Esc or a click elsewhere closes this)` |

> **Abweichung von der Vorgabe (bewusst, dokumentiert):** Beauftragt waren **zwei** Schlüssel. Der
> Auftrag lautete zugleich, die Zeilen 82–84 vollständig auf `MyResource.Resource.*` umzustellen —
> die dritte Zeile ist der Esc-Hinweis. Ohne `HILFE_POPUP_ESC` wäre ein deutscher Anzeigetext im
> Code stehen geblieben, also weiterhin ein Verstoß gegen die Drei-Schichten-Regel. Der dritte
> Schlüssel ist deshalb ergänzt.

**Einfügeort in den `.resx`:** ans Dateiende vor `</root>`. Der Bestand ist **nicht** alphabetisch
sortiert, sondern thematisch gruppiert und wird angehängt (2641 Einträge, letzte Gruppe
`SIM_*`) — eine alphabetische Einordnung wäre in dieser Datei willkürlich und für den ResX-Leser
ohne Bedeutung. Die drei neuen Schlüssel stehen untereinander alphabetisch.

### 8.1 `Resource.Designer.cs` — Fallstrick eingetreten und aufgelöst

Vorgesehen war die Hand-Ergänzung der drei Eigenschaften. **Visual Studio ist zuvorgekommen:**
rund eine Sekunde nach dem Schreiben der `.resx` hatte es `MyResource\Resource.Designer.cs` von
selbst regeneriert und die drei Eigenschaften bereits alphabetisch korrekt eingeordnet
(`:3821 HILFE_POPUP_ESC`, `:3830 HILFE_POPUP_KAPITEL`, `:3839 HILFE_POPUP_LINK` — zwischen
`GESETZ_TITEL` und `IMP_KONFLIKT_ABBRECHEN`). Der beabsichtigte Hand-Edit schlug daraufhin mit
„File has been modified since read" fehl und wurde **nicht** wiederholt.

**Damit ist der bekannte Fallstrick (`CLAUDE.md`, „Visual Studio regeneriert
`MyResource/Resource.Designer.cs` selbst") in diesem Fall bereits erledigt.** Hätte die
Hand-Einfügung stattgefunden, stünden jetzt Duplikate (CS0102) in der Datei. **Auflösung, falls es
doch noch dazu kommt: die generierte Fassung behalten, die Hand-Einfügung entfernen.** Wer diesen
Stand ohne laufendes Visual Studio nachbaut, muss die drei Eigenschaften ggf. selbst ergänzen —
der Build ist der Prüfstein.

---

## 9. Aufgabe 8 — `WordPressPrefix` stillgelegt (Entscheid 7.3)

`Views\Admin\Form_AdminSettings.cs`:

| vorher | nachher |
|---|---|
| `:147` `Properties.Settings.Default.WordPressPrefix = txt_WPPrefix.Text;` (Speichern) | entfernt, Kommentar an der Stelle |
| `:215` `txt_WPPrefix.Text = …WordPressPrefix;` (Laden) | entfernt |
| `:237` `txt_WPPrefix.Text = …WordPressPrefix;` (Standardwerte) | entfernt |
| — | `:224–225` im `Form_AdminSettings_Load`: `lbl_WPPrefix.Visible = false; txt_WPPrefix.Visible = false;` |

Die Designer-Datei wurde **nicht** angefasst — Steuerelement und Beschriftung existieren weiter,
sind aber nicht mehr sichtbar (Prüfliste 9, Punkt 15: „Feld nicht mehr im Admin-Dialog").

**Grep-Beweis** (`*.cs`, ohne `bin/`, `obj/`): `WordPressPrefix` erscheint nur noch
* `Properties\Settings.Designer.cs:53/55/58` — die generierte Settings-Eigenschaft selbst
  (Speicherplatz, kein Leser),
* `Allgemein\Hilfe\HelpCatalog.cs:406` und `Views\Admin\Form_AdminSettings.cs:147/244` —
  **Kommentare**.

**Kein Codepfad liest oder schreibt die Einstellung mehr.**

---

## 10. Aufgabe 9 + 10 — Zuordnung und Startbestand neu (A3 / H2)

### 10.1 `Allgemein\Hilfe\help_mapping.txt`

Komplett neu geschrieben. **Die linken Seiten (`Formname.Controlpfad`) sind byte-identisch zum
Vorzustand** — Beweis: beide Fassungen entkommentiert, linke Seiten extrahiert, getrimmt, sortiert,
`diff` leer (26 = 26). Rechte Seite je Zeile: **ein** Ziel, kein `|` mehr.

| Programmstelle | Ziel (Kurzname) |
|---|---|
| `Form_Start.btn_Help` | Programmablauf |
| `Form_Start.btn_Help_Kurzanleitung` | Kurzanleitung |
| `Form_Start.btn_Help_Waermebedarf` | Wärmebedarf |
| `Form_Start.btn_Help_Strombedarf` | Strombedarf |
| `Form_Klimadaten.btn_Help` | Klimadaten |
| `Form_Kosten.btn_Help` | Kosten |
| `Form_WP.btn_Help` | Wärmepumpe |
| `Form_Heizkessel.btn_Help` | Heizkessel |
| `Form_BHKWEing.btn_Help` | BHKW |
| `Form_SolarKollektoren.btn_Help` | Solarthermie |
| `Form_PV.btn_Help` | Photovoltaik |
| `Form_PufferSp.btn_Help` | Pufferspeicher |
| `Form_Stromspeicher.btn_Help` | Stromspeicher |
| `Form_Waermebedarf.btn_Help` | Wärmebedarf |
| `Form_Gebaeude.btn_Help` | Gebäude |
| `Form_Brauchwasser.btn_Help` | Brauchwasser |
| `Form_Prozesswaerme.btn_Help` | Prozesswärme |
| `Form_Stromverbraucher.btn_Help` | Stromverbraucher |
| `Form_Simulation_Config.btn_Help` | Simulation |
| `UcWirtschaftlichkeit.btn_Help` | Wirtschaftlichkeit |
| `Form_Variantentest.btn_Help` | Varianten |
| `Form_ProjektSpeichernUnter.btn_Help` | Projektverwaltung |
| `Form_ImportKonflikte.btn_Help` | Projektverwaltung |
| `WizardParent.btn_Help` | Kurzanleitung |
| `Form_AdminSettings.btn_Help` | Einstellungen |
| `Form_KiChat.btn_Help` | Hilfe-Assistent |

26 Zeilen → 23 verschiedene Unterseiten. Der Kommentarkopf ist auf das neue Zielformat
umgeschrieben (Kurzname, optional `#anker`), erklärt den Wegfall der EN-Hälfte, den Abgleich über
die Kleinschreibform und die Kommentarregel („`#` nur am Zeilenanfang").

**Kodierung des Parsers geprüft:** Beide Ladewege lesen ausdrücklich UTF-8 —
`File.ReadAllLines(filePath, Encoding.UTF8)` für die Datei neben der EXE und
`new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true)` für die
eingebettete Ressource; zusätzlich trimmt der Parser je Zeile `'\uFEFF'`. Die Datei ist deshalb als
**UTF-8 mit BOM, CRLF** gespeichert — wie im Vorzustand, jetzt aber mit Umlauten in den *Zielen*
statt nur in den Kommentaren.

### 10.2 `Allgemein\Hilfe\help_cache.json`

Ist-Schema übernommen: JSON-Objekt, Schlüssel = normalisierter Link-Pfad, Wert =
`{ "Tooltip", "Url", "Slug" }`, eine Zeile je Eintrag, CRLF, UTF-8 **ohne** BOM, Nicht-ASCII als
`\uXXXX` — letzteres bewusst, weil die `.csproj` genau das als Eigenschaft dieser Datei dokumentiert
(„Reines ASCII (Umlaute als `\uXXXX`), damit keine Kodierung daran scheitern kann"); ASCII ist eine
Teilmenge von UTF-8, die Vorgabe ist damit erfüllt.

Der Bestand wurde **generiert**, nicht getippt: dieselbe Vorschrift wie im Code
(`Uri.EscapeDataString` je Titelstück, Leerzeichen → `_` vorher; Schlüssel über die Nachbildung von
`PfadNormalisieren`). 116 alte `epos-plan.de`-Einträge → **23** Wiki-Einträge.

Beispielzeile:
```
"/wiki/programm_dokumentation/w\u00e4rmebedarf/": { "Tooltip": "W\u00e4rmebedarf", "Url": "https://wiki.epos-plan.de/wiki/Programm_Dokumentation/W%C3%A4rmebedarf", "Slug": "W\u00e4rmebedarf" }
```

Die `EmbeddedResource`/`LogicalName`-Einträge in `WindowsFormsApplication1.csproj:171–194` sind
**unverändert**.

### 10.3 Slug-Normalisierung: eine Vorschrift für beide Seiten

Neu `WikiHelpCatalog.SlugNormalisieren` (`HelpCatalog.cs:256`) = `Trim()` + `ToLowerInvariant()`,
dieselbe Kleinschreibung wie `PfadNormalisieren`. Umlaute bleiben Umlaute („Wärmebedarf" →
„wärmebedarf", **nicht** „waermebedarf").

Angewandt **auf beiden Seiten des Abgleichs**:
* Katalogseite: `EintragAufnehmen` legt den Kurznamen normalisiert in `_slugAufPfad` ab,
* Mapping-Seite: `UeberSlug` normalisiert die Angabe vor dem Nachschlagen.

Nachweis im Prüflauf (§ 11, Abschnitt 3): „Wärmebedarf", „wärmebedarf", „WÄRMEBEDARF" und
„&nbsp;&nbsp;Wärmebedarf&nbsp;&nbsp;" treffen dieselbe Seite; der abgelegte Slug ist
`wärmebedarf`.

---

## 11. Aufgabe 11 — Build- und Prüfnachweis

### 11.1 Build

```
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
  C:\Waermeplan\WP_Plan\WP-Plan.sln -p:Configuration=Debug -p:Platform=x64 `
  -p:OutDir=C:\Waermeplan\WP_Plan\dev\build_h1\
```

**Ergebnis: 0 Fehler.** Ausgabe (gekürzt):
```
SpeicherEngine -> …\dev\build_h1\SpeicherEngine.dll
KiKern         -> …\dev\build_h1\KiKern.dll
WindowsFormsApplication1 -> …\dev\build_h1\WindowsFormsApplication1.dll
SpeicherEngine.Tests / KiKern.Tests -> …
```

**Warnungen unverändert zum Vorzustand — 5 Stück, alle in nicht berührtem Code:**

| Warnung | Stelle |
|---|---|
| CS0109 ×2 | `Controller\KlimaregionStammCtrl.cs:22,24` / `:23,48` |
| CS0108 | `Controller\StromverbraucherStammCtrl.cs:25,44` |
| CS0108 | `Model\WErzeugerModel.cs:6,20` |
| CS1998 | `MDIMainForm.cs:489,28` (`MDIMainForm_Load`) |

Die einzige Warnung in einer geänderten Datei ist CS1998 in `MDIMainForm.cs:489`. Gegenprobe:
`git show HEAD:…/MDIMainForm.cs | sed -n '485,492p'` ist zeichengleich mit dem Arbeitsstand; der
erste Diff-Hunk beginnt bei `@@ -807`. Die Warnung ist Altbestand.

### 11.2 Funktionsprüfung (Wegwerf-Harnesse unter `..\dev\`)

`C:\Waermeplan\WP_Plan\dev\h1probe\` (gitignored, **kein** `.cs` unterhalb von
`WindowsFormsApplication1`): kleines net8-Konsolenprojekt gegen die gebaute
`WindowsFormsApplication1.dll`, `internal`-Mitglieder über Reflexion.
**33 Prüfungen, alle grün** (`ALLES GRUEN`, ExitCode 0):

1. **Startbestand** — eingebettete `help_cache.json` lädt, **23 Seiten**.
2. **Zuordnung** — alle **26** Mapping-Ziele lösen im Katalog auf (26/26); `Tooltip` =
   „Wärmebedarf", `Url` = `…/Programm_Dokumentation/W%C3%A4rmebedarf`.
3. **Slug-Normalisierung** — siehe § 10.3.
4. **Anker** — `Pufferspeicher#ladung` löst auf; `ZielAufloesen` liefert Ziel `Pufferspeicher` und
   Anker `ladung`; `MitAnker` ergibt `…/Pufferspeicher#ladung`; der Katalogeintrag selbst bleibt
   ankerfrei (Kopie).
4b. **Titel → HelpEntry** — Kurzname korrekt; Leerzeichen → `_` vor der Kodierung
   („Kessel und Spitzenlast" → `Kessel_und_Spitzenlast`); Rubrikseite selbst, fremde Rubrik und
   leerer Kurzname fallen heraus.
5. **Übersetzungs-Proxy** — DE unverändert;
   EN = `https://wiki-epos--plan-de.translate.goog/wiki/Pufferspeicher?_x_tr_sl=de&_x_tr_tl=en&_x_tr_hl=en`
   (**identisch mit der am 29.08.2026 verifizierten Referenz-URL**); Anker bleibt hinter der Query;
   vorhandene Query wird mit `&` verlängert; fremder Host und unparsbare Eingabe → Original.
6. **Ressourcen** — beide Sprachen liefern die drei neuen Schlüssel.

### 11.3 Live-Probe gegen `wiki.epos-plan.de`

| Aufruf | Ergebnis |
|---|---|
| `api.php?action=query&list=allpages&apprefix=Programm%20Dokumentation%2F&aplimit=500&format=json` | **HTTP 200**, `query.allpages` = **0 Seiten**, kein `continue` |
| dasselbe mit `apprefix=Grundlagen%2F` | HTTP 200, 15 Seiten, Elemente tragen `title` (z. B. `Grundlagen/BHKW`) |
| dito mit `aplimit=3` | 3 Seiten + `continue.apcontinue = Grundlagen/Kessel_und_Spitzenlast`; Fortsetzungsaufruf mit `&apcontinue=…` liefert die nächsten 3 |

**Damit ist bewiesen:** der 404-Zustand ist beseitigt, die Antwortstruktur entspricht genau dem,
was der neue Lader auswertet, und die Fortsetzung über `apcontinue` funktioniert. **Die Rubrik
„Programm Dokumentation" ist aber noch leer — Paket H3 steht aus** (siehe § 12).

---

## 12. Bekannte Nacharbeit und offene Punkte

1. **H3 zuerst ausliefern.** Solange die Rubrik leer ist, liefert der Onlinelauf nichts; es greift
   die `%APPDATA%`-Sicherung, ersatzweise der neue eingebettete Startbestand. Auf einem
   Entwicklerrechner mit alter Sicherung öffnen die Buttons daher weiterhin die
   **WordPress-Seiten** — die Sicherung wird erst vom ersten *erfolgreichen* (nicht-leeren)
   Onlinelauf überschrieben. Wer das sofort sehen will, löscht
   `%APPDATA%\WP-Plan\help_cache.json`. Die neue `Debug`-Warnung macht den Zustand sichtbar.
2. **`Resource.Designer.cs`** — von Visual Studio bereits regeneriert (§ 8.1). Erscheint dennoch
   einmal CS0102: die generierte Fassung behalten, die Hand-Einfügung entfernen.
3. **`txt_WPPrefix` / `lbl_WPPrefix` restlos entfernen** — spätere Aufgabe im WinForms-Designer
   (`Views\Admin\Form_AdminSettings.Designer.cs:30/31/119/120/179–194/493/494`). Bis dahin sind die
   Steuerelemente nur unsichtbar geschaltet. Beim Entfernen fällt auch die dann tote
   Settings-Eigenschaft `WordPressPrefix` (`Properties\Settings.settings:11`,
   `Settings.Designer.cs:53`, `app.config:20`) an — sie wurde hier bewusst **nicht** angefasst.
4. **Feld `lbl_WPPrefix.Text`** trägt noch den Text „WordPress API-Präfix …". Da das Label
   unsichtbar ist, spielt das keine Rolle; es verschwindet mit Punkt 3.
5. **Nicht Teil von H1/H2, laut Konzept später:** Popup-Kurzbeschreibungen (A5/7.6, H6),
   `WikiWissen` und die Quellen-Links im Chat (H4), Einwilligungs-Zusatzsatz (H5),
   Vertragstabelle auf der Rubrikseite und Bereichs-Zuordnung (H3/Teil C).
6. **Prüfliste 9 des Konzepts** ist damit für die Punkte 1–5 und 14 erst nach H3 abfahrbar;
   Punkt 15 (`WordPressPrefix`) ist mit § 9 erledigt.

---

## 13. Nicht angefasst

Auf Weisung außerhalb des Umfangs und nachweislich unberührt: `Allgemein\Update\SchemaMigration.cs`,
`Allgemein\DbWerte.cs`, sämtlicher Emissions-/CO₂-Bezug, beide `CLAUDE.md`, das Konzeptdokument
selbst. Erscheinen diese Dateien im `git status` als geändert, stammen die Änderungen aus der
parallel laufenden Sitzung. Es wurde **kein** Git-Schreibkommando ausgeführt.

---

## 14. Nachtrag 29.08.2026 — Fix „graue Buttons"

**Befund aus dem Programmlauf:** Nach dem Start der neuen EXE (14:42) waren praktisch
**alle** Info-Schaltflächen (`btn_Help*`) ausgegraut — obwohl der neue MediaWiki-Lader
nachweislich funktionierte (`%APPDATA%\EPOS-Plan\help_cache.json`, 14:49, 23 Wiki-Einträge)
und die h1probe „26/26 Mapping-Ziele lösen auf" meldete.

### 14.1 Ursache

**Nicht** im Code von H1/H2, sondern in einer **Restdatei im Ausgabeordner**:

```
WindowsFormsApplication1\bin\x64\Debug\net8.0-windows\help_mapping.txt   464 Byte, 28.08.2026 17:16
WindowsFormsApplication1\bin\x64\Release\net8.0-windows\help_mapping.txt 464 Byte, 28.08.2026 17:16   (identischer SHA-256)
```

Diese Datei stammt aus der Zeit, als die Zuordnung noch mitkopiert wurde. Seit sie
`EmbeddedResource` ist (`WindowsFormsApplication1.csproj:164–176`, Kommentar: „deshalb wird sie
bewusst NICHT in den Ausgabeordner kopiert"), **überschreibt kein Build sie mehr** — sie blieb
als Leiche liegen und gewann zur Laufzeit gegen die eingebettete Fassung.

Sie enthält 9 Zeilen aus einem sehr frühen Stand mit **WordPress-Slugs**:

```
Form_Kosten.btn_Help=kostenrechnung
Form_Start.btn_Help=Programmfunktionen
Form_Klimadaten.btn_Help=klimadaten
Form_Start.btn_Help_Waermebedarf=waermebedarfsrechnung
Form_Start.btn_Help_Kurzanleitung=epos-plan-kurzanleitung
Form_Start.btn_Help_Strombedarf=strombedarf
```

Die Wirkung war zweistufig und deckt den Befund vollständig:

1. `HelpExtender.ZuordnungLaden` (vorher `HelpCatalog.cs:945–997`) las die Datei neben der EXE und
   gab sie **als Ersatz** zurück — die eingebettete 26-Zeilen-Fassung wurde gar nicht erst
   geöffnet. Für 20 der 26 Programmstellen existierte damit **keine Zeile**, und
   `InfobuttonsOhneZuordnungAbschalten` (vorher `:1007`, jetzt `:1061`) schaltete sie sofort ab
   („hat keine Zeile in help_mapping.txt").
2. Von den 6 verbliebenen Zeilen trafen im neuen Wiki-Katalog nur `klimadaten` und `strombedarf`
   (die Kleinschreibform der Kurznamen „Klimadaten"/„Strombedarf"). `kostenrechnung`,
   `Programmfunktionen`, `waermebedarfsrechnung` und `epos-plan-kurzanleitung` lösten nicht auf →
   `ZuordnungenPruefen` (vorher `:1063`, jetzt `:1117`) schaltete auch diese ab.

**Ergebnis: 24 von 26 Info-Buttons grau.** Aktiv blieben allein `Form_Klimadaten.btn_Help` und
`Form_Start.btn_Help_Strombedarf` — zwei Zufallstreffer.

Die im Auftrag genannten Verdächtigen sind entlastet: `ZielAufloesen` mit Anker-`out`-Parameter,
`Aufloesen`, die `HelpEntry`-Kopie in `EintragHolen`, der Registrierungsweg über
`WikiHelpCatalog`/`HilfeAutomatik` und der BOM in `help_mapping.txt` verhalten sich alle korrekt
(Phase B/B2/C des Beweises unten).

### 14.2 Fix

**Datei `Allgemein\Hilfe\HelpCatalog.cs`, `HelpExtender.ZuordnungLaden`** — aus *Ersatz* wird
*Auflage*:

| | vorher (`:945–997`, eine Methode) | nachher (`:971` `ZuordnungLaden`, `:995` `ZuordnungNebenExeLaden`, `:1019` `ZuordnungEingebettetLaden`) |
|---|---|---|
| Ablauf | Datei neben der EXE gefunden → **return**; die eingebettete Fassung wurde nie gelesen | `ZuordnungEingebettetLaden()` **immer**, danach `ZuordnungNebenExeLaden()`; beide Listen werden aneinandergehängt, die Datei-Zeilen **hinten** |
| Vorrang | Datei ersetzt alles | Datei übersteuert je Zeile — `ZuordnungenAnwenden` wendet jede passende Zeile an, `SetHelpKey` überschreibt den Schlüssel, also gewinnt die zuletzt gelesene Zeile |
| Fehlende Zeilen | verschwinden ersatzlos → Button grau | bleiben in Kraft |
| Protokoll | eine Zeile je Quelle | zusätzlich eine Zeile „N eingebettete Zeilen, darüber M Zeilen aus der Datei neben der EXE" |

Der Zweck von F2 („Zuordnungen ohne Neubau korrigieren") bleibt unverändert erhalten; verloren
geht nur die Möglichkeit, eine Zuordnung durch **Weglassen** einer Zeile zu entfernen — siehe
§ 14.5.

**Umgebung:** Beide Restdateien wurden entfernt (vorher gesichert). Ohne diesen Schritt blieben
auf dem Entwicklerrechner die vier Buttons grau, die die Restdatei ausdrücklich falsch zuordnet
(`Form_Start.btn_Help`, `…_Kurzanleitung`, `…_Waermebedarf`, `Form_Kosten.btn_Help`).

### 14.3 Beweis — `..\dev\h2probe_buttons\` (gitignored)

Wegwerf-Harnesse gegen die gebaute Assembly (`..\dev\build_fix\`), das die **echte Verkabelung**
fährt statt der Katalogebene: 23 reale `Form`-Objekte mit den 26 realen `btn_Help*`-Schaltflächen,
registriert über `HelpExtender.RegisterBaum(form, form.Name)` — genau der eine Aufruf, den
`HilfeAutomatik.WurzelErfassen` im Programm macht —, anschließend die Frage `button.Enabled`.
Der Katalog wird offline aus dem eingebetteten Startbestand belegt (23 Seiten, inhaltsgleich mit
der AppData-Sicherung vom 29.08. 14:49) und über das Feld `_geladen` auf `IsLoaded = true`
gesetzt. Zwei Phasen im selben Prozess, dazwischen wird `HelpExtender._mappingZeilen` geleert.

| Phase | Lage | vor dem Fix | nach dem Fix |
|---|---|---|---|
| **A** | die 464-Byte-Restdatei liegt neben der EXE | **2 von 26 aktiv** (24 grau) — der Befund, deterministisch nachgestellt | **22 von 26 aktiv**; grau bleiben genau die 4, die die Restdatei selbst falsch zuordnet |
| **B** | keine Datei neben der EXE → eingebettete Fassung | 26/26 aktiv | 26/26 aktiv |
| **B2** | wie B, `Program.nLanguage = 1` (EN-Rückfall auf das eine Ziel) | 26/26 aktiv | 26/26 aktiv |
| **C** | Klickziel über `EintragHolen` | grün | grün |

Phase C im Einzelnen: ohne Anker
`…/Programm_Dokumentation/Pufferspeicher`; mit Anker
`…/Programm_Dokumentation/Pufferspeicher#ladung`; der Katalogeintrag bleibt ankerfrei (Kopie);
EN-Proxy
`https://wiki-epos--plan-de.translate.goog/wiki/Programm_Dokumentation/Pufferspeicher?_x_tr_sl=de&_x_tr_tl=en&_x_tr_hl=en#ladung`
(Anker hinter der Query); DE unverändert.

Vor dem Fix: `1 FEHLER`, ExitCode 1 (Phase A). Nach dem Fix: `ALLES GRUEN`, ExitCode 0.
Die bestehende `..\dev\h1probe\` läuft gegen dieselbe Assembly weiterhin mit **33 Prüfungen grün**,
darunter „alle Ziele im Katalog → 26/26".

### 14.4 Warum die ursprüngliche Prüfung das nicht sah

`h1probe` misst die **Katalogebene**: `katalog.Contains(ziel)` für die 26 Ziele, als
Zeichenketten-Feld im Prüfprogramm. Sie liest `help_mapping.txt` nie, ruft `ZuordnungLaden` nie
auf und kennt keinen `Control`. Genau die zwei Glieder der Kette, an denen es hing — **welche
Zuordnungsdatei gewinnt** und **welcher Button daraufhin `Enabled` ist** —, lagen außerhalb ihres
Messbereichs. Deshalb konnte sie „26/26" melden, während im Programm 24 von 26 Schaltflächen grau
waren. `h2probe_buttons` schließt diese Lücke.

### 14.5 Offene Konzeptfrage

Die Datei neben der EXE kann einen Info-Button jetzt nicht mehr durch **Weglassen** seiner Zeile
abschalten — nur noch durch Umschreiben auf ein Ziel, das der Katalog nicht kennt. Falls das
gezielte Abschalten je gebraucht wird, wäre eine ausdrückliche Marke nötig (etwa
`Form_X.btn_Help = -`). Bewusst nicht umgesetzt: Der Fall ist bisher nirgends verlangt, und die
stillschweigende Löschwirkung war genau der Fehler.

Ebenfalls offen (Betrieb, kein Code): Ein Installer, der `help_mapping.txt` je neben die EXE legt,
löst dasselbe Verhalten aus — die Datei muss dort dauerhaft **fehlen**, sonst friert sie die
Zuordnungen ein, die sie nennt.

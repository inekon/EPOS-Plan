# B5b — Port des Dialogs „BHKW-Wirtschaftlichkeit" nach Blazor (Umsetzungsprotokoll)

**Stand 03.09.2026 · Integrations-Worktree `merge_fx_b5_ios`, Basis `e0e247c` (Merge FX/B5 auf den
iU8-Stand `4aa6b15`) · nicht committet**

Anschlussprotokoll zu [`B5_BhkwWirtschaftlichkeit_Dialog_Protokoll.md`](B5_BhkwWirtschaftlichkeit_Dialog_Protokoll.md).
Was dort steht, gilt fachlich unverändert weiter; hier steht **nur, was der Port geändert hat**.

---

## 1. Entscheid und Auftrag

**Anwenderentscheid 03.09.2026:** Der in Etappe B5 als WinForms-Code-only gebaute Dialog
`Views/Wirtschaftlichkeit/Form_BhkwWirtschaftlichkeit.cs` wird gemäß der **iU8-Arbeitsregel**
(„jeder neue und jeder ohnehin anzufassende Dialog entsteht in `EPOS.UI`, die Anwendung liefert nur
die Hülle") als Razor-Komponente neu gebaut und die WinForms-Maske im **selben Schritt** gelöscht
(**Regel M1** — keine zweite Fassung derselben Maske).

Der Dialog ist damit nach `Form_Kosten_Auswahl` (Stichtag iZ5, iU8-9) die **zweite** Maske von
EPOS-Plan, die in `EPOS.UI` lebt — und die erste mit einer Tabelle, einem nicht schließenden
Speichern-Knopf und einer Datenseite von der Größe eines Fachdialogs.

**Feste Entscheide unverändert übernommen:** K1 kein Deckungsfeld · K2 Basis „% des
Endenergiebedarfs" ausdrücklich benannt · **K3 = a** (Modusfeld sichtbar, deaktiviert, Vermerk
„ab B6") · K4 Brennstoffspalte · K5 Jahresnutzungsgrad bleibt Projektgröße · K6 Hilfsenergieanteil
nur am BHKW pflegbar (+ Kesselhinweis) · K7 elf Spalten · K8 = c Andockung · Anlagentabelle mit
7 Spalten inkl. „Projekt" · Kohärenz-/Warnzeilen aus den Kern-Prüfquellen (eine Wahrheit) ·
Mengenkette aus `KwkgModulNachweis` · **Vorschau = gebuchter Stand (BW8, keine Zweitrechnung)**.

---

## 2. Bauweise

### 2.1 Die Komponente — `EPOS.UI/Dialoge/Wirtschaftlichkeit/`

| Datei | Zeilen | Inhalt |
|---|---|---|
| `BhkwWirtschaftlichkeitDialog.razor` | 916 | die Maske: acht Abschnitte, Feldbindung, Zustandslogik, Warn-/Kohärenz-/Herleitungszeilen, Speichern- und Schließweg |
| `BhkwWirtschaftlichkeitTexte.cs` | 145 | die Beschriftungen, **einmal** aus dem Ressourcenkatalog geholt |
| `BhkwWirtschaftlichkeitDaten.cs` | 227 | `BhwTexte` (Textzugriff, Kultur, Kurzdatum), `Steuerwahl` + `BhkwWahlen` (die sechs Auswahllisten), `BhkwSprung`, `BhkwWirtschaftlichkeitErgebnis` |

Ordner- und Namenskonvention nach dem Vorbild `Dialoge/Kosten/EnergietraegerVarianteDialog.razor`
(iU8-8b): `@namespace EPOS.UI.Dialoge.<Fachbereich>`, Wurzel-`div` mit `tabindex="-1"`, `@ref` und
`@onkeydown`, Kopfzeile mit Titel und `<InfoKnopf>`, Abschluss mit `<SpeichernLeiste>`.

**Bausteine nach der Zielkomponenten-Tabelle** (`Werkzeuge/Formularkarte/LIESMICH.md`):

| WinForms (gelöscht) | EPOS.UI |
|---|---|
| `ListView` (Anlagentabelle) | `Raster` (QuickGrid) mit `TemplateColumn` je Spalte |
| `NumericUpDown` | `Zahlenfeld` (Min/Max/Nachkommastellen wie im Bestand) |
| `ComboBox` | `Auswahlfeld` |
| `DateTimePicker` mit `ShowCheckBox` | `Datumsfeld` (leeres Feld = Haken aus) |
| `CheckBox` | `Schalter` |
| `GroupBox` | `Gruppenkopf` |
| Firebrick-Label (Warnzeile) | `Warnbanner` |
| DimGray-Label (Herleitung, Mengenkette, Vorschau) | `Herleitungszeile` |
| „keine Auffälligkeit" | `Kohaerenzzeile` (Zustand Ok) |
| `MessageBox` beim Speicherfehler | `Warnbanner` (Stufe Fehler) |
| `SpeichernLeiste` (WinForms) + „Schließen" | `SpeichernLeiste` (Baustein) |
| `InfoKnopf.Anbringen` | `<InfoKnopf Schluessel="…" />` |

**Zwei Bausteine haben je einen Parameter dazubekommen** — additiv, Vorgabewert = bisheriges
Verhalten, jeder mit eigenem Test:

* `Standards/Auswahlfeld.razor` → `Aktiv` (Vorbild: `Schalter.Aktiv`). Gebraucht für **K3**: Das
  Modusfeld § 9 Abs. 1 Nr. 3 wird gezeigt, aber gesperrt.
* `Bausteine/SpeichernLeiste.razor` → `MitAbbrechen`. Eine Maske, die schon beim Speichern
  schreibt, kennt kein Verwerfen mehr; sie hat nur „Speichern" und „Schließen".

### 2.2 Die Datenseite — in der Hülle, nicht in der Komponente

Hausregel `EPOS.UI/CLAUDE.md`: **keine Datenbank in einer Komponente.** Alles kommt als
`[Parameter]` herein:

| Parameter | Quelle in der Hülle |
|---|---|
| `Anlagen` (`IList<KwkgAnlagenAngabe>`) | `KwkgAnlagenCtrl.LadeGruppe(idStamm, stammName)` |
| `Parameter` (`WirtschaftlichkeitParameter`) | `WirtschaftlichkeitCtrl.LadeParameter(idStamm)` |
| `HatHeizkessel` | `WirtschaftlichkeitCtrl.ErzeugerDerGruppe(idStamm).Heizkessel` |
| `StammName` | `ProjektCtrl.ReadSingle` |
| `Doppelpflege` | `KohaerenzPruefung.Pruefe(idStamm, null)` — `internal` zum Kern, deshalb in der Hülle |
| `Katalog` (`Func<string,int,GesetzParameter>`) | `GesetzKatalog.WertMitHerkunft` |
| `ErgebnisseAusLauf` | durchgereicht von `UcWirtschaftlichkeit._ergebnisse` |
| `ErgebnisseLaden` (`Func<…>`) | `WirtschaftlichkeitCtrl.LadeErgebnisse` |
| `Speichern` (`Func<int>`) | `KwkgAnlagenCtrl.Speichere(g, true)` ×n + `SpeichereParameter` |
| `Geschlossen` (`EventCallback`) | `BlazorDialogForm.Schliessen` |

**Der Katalog als Delegat, nicht als Objekt** — dieselbe Übergabe, die `KwkgSatzRechner` selbst
verlangt (Leitentscheidung L9: „Der Katalog wird als Delegat hereingereicht, damit dieselbe Rechnung
im Dialog, in der Wirtschaftlichkeit und in einer Probe verwendbar ist"). Damit rechnet die
Komponente mit **dem einen** Katalog und bleibt trotzdem datenbankfrei.

**`Speichern` ist ein `Func<int>` und kein `EventCallback`** — begründete Abweichung vom Vorbild:
Die Speichernleiste braucht eine Antwort („gespeichert um 12:03" oder „nicht gespeichert"), ein
`EventCallback` liefert keine. Der Rückgabewert ist die Zahl der gescheiterten Sätze; daraus wird
dieselbe Meldung wie bisher (`BHW_MSG_FEHLER`), nur als Warnbanner statt als MessageBox.

**Die Komponente schreibt in die übergebenen Objekte zurück** (`KwkgAnlagenAngabe`,
`WirtschaftlichkeitParameter`) — wortgleich zum Bestandsverhalten `UebernimmFelder` /
`ProjektwerteSpeichern`. Die Hülle speichert danach dieselben Zeilen; eine Abbildungsschicht
dazwischen gäbe es nur, um sie zweimal zu pflegen.

### 2.3 Die Hülle — `Views/Wirtschaftlichkeit/BhkwWirtschaftlichkeitHuelle.cs` (198 Zeilen)

Vorbild `Form_Kosten.CreateNewEnergyCarrier` (iU8-9): Parameterwörterbuch bauen,
`Geschlossen`-Rückruf auf `BlazorDialogForm.Schliessen` legen, `ShowDialog()` auswerten.
`BlazorDialogForm<BhkwWirtschaftlichkeitDialog>`, Innenmaß **914 × (Arbeitsbereich − 90)** — die
Breite ist das Hausmaß § 5, gescrollt wird innerhalb der Komponente.

`Oeffnen(besitzer, idStamm, ergebnisseAusLauf)` liefert `true`, wenn mindestens einmal gespeichert
wurde — genau das, was `Form_BhkwWirtschaftlichkeit.Gespeichert` lieferte. Für
`UcWirtschaftlichkeit` ändert sich damit **eine Zeile**; Rückmeldung und Nachlauf bleiben.

### 2.4 Der Sprungknopf — die Designfrage

Die Stromsteuergruppe trägt zwei Sprünge in `Form_Tarifstruktur`: „Strombezug…"
(`TarifSicht.Strombezug`, Feldkarte 4.5) und „BHKW-Tarif…" (`TarifSicht.Bhkw`, Abweichung A-3 aus
B5 — ohne ihn wäre die Differenzmethoden-Sicht über keine Oberfläche mehr erreichbar).

**Befund: Das Haus hat (Stand iU8) kein Muster, mit dem ein Blazor-Dialog ein zweites MODALES
WinForms-Fenster über sich öffnet.** Geprüft wurden `Allgemein/Blazor/` (nur
`BlazorDialogForm<T>`, `DpiInsel`, `BlazorDienste` — kein Registrierungspunkt je Dialog) und die
einzige vorhandene Brücke `IHilfeDienst` / `WindowsHilfeDienst`: Sie zeigt ein **modeloses**
Hilfefenster, nicht einen modalen Dialog, und ist als *der* Dienst der Bibliothek registriert, nicht
als Muster für beliebige Fenster.

**Gewählte Lösung (die im Auftrag benannte Rückfallvariante):** Der Sprung läuft **nachgelagert**.
Die Komponente meldet den Wunsch im Ergebnis (`BhkwSprung.BhkwTarif` / `.Strombezug`), die Hülle
schließt den Dialog, öffnet `Form_Tarifstruktur` und **bringt den Dialog danach mit frisch
geladenen Daten zurück** (Schleife in `Oeffnen`). Eine leise Zeile unter den Knöpfen sagt es vorher:

> „Der Sprung schließt diesen Dialog und öffnet ihn danach wieder — bitte vorher speichern."

**Designfrage B5b-O1 — ENTSCHIEDEN (Anwender, 03.09.2026): Variante a.** Der nachgelagerte
Sprung (Dialog schließt, Zielfenster öffnet, Dialog kehrt mit frischen Daten zurück, Hinweiszeile
kündigt es an) ist die beschlossene Bauform — **kein** `IFensterDienst`. Begründung: funktional
sauber, kein neues Brückenmuster auf der frischen Blazor-Schiene; ein Fensterdienst lohnt erst,
wenn mehrere portierte Masken ihn brauchen. Wiedervorlage ausdrücklich bei der **nächsten
iU9-Maske mit Sprungziel** (Kandidat laut B5b-O7: `Form_KwkgModule`-Nachfolger) — dann mit der
Erfahrung aus dem Praxisbetrieb dieser Lösung.

---

## 3. Feldkarten-Abgleich (Soll = `b5_feldkarte.md` § 1)

Der Abgleich ist **als Test ausgeführt** (`EPOS.UI.Tests/Dialoge/BhkwWirtschaftlichkeitDialogTests.cs`),
nicht als einmalige Messung: Jede Gruppe hat einen Test, der Feldzahl **und** Beschriftungen gegen
die Feldkarte prüft. Fällt ein Feld weg, wird der Test rot.

| Gruppe | Soll (Feldkarte) | Ist (Komponente) | Deckung |
|---|---|---|---|
| **1 Anlagen** | Tabelle 1.1–1.6 (6 Spalten) + 3 Warnzeilen | `Raster` mit **8** Spalten: Wahl · Projekt · Anlage · P_el [kW] · Brennstoff · Stichtag · Inbetriebnahme · Anlagenart; 3 `Warnbanner` | **100 %** + „Projekt" (A-1, aus B5) + Wahlspalte (**A-6**, siehe § 4) |
| **1b Angaben der Anlage** | 11 Felder (1.7–1.17) | Datum 2 · Auswahl 4 · Zahl 5 = **11**, Reihenfolge und Beschriftungen wortgleich | **100 %** |
| **2 KWK-Zuschlag** | 11 Felder (2.1–2.11) + Herleitung + Knopf | Zahl 6 · Auswahl 2 · Schalter 1 · Datum 2 = **11**, 2 Herleitungszeilen, Knopf „Vorschlag in die Satzfelder übernehmen" | **100 %** |
| **3 Energiesteuer** | 3 Felder (3.1–3.3) + Herleitung | Auswahl 2 · Zahl 1 = **3**, Herleitung aus `SteuerHerkunft` | **100 %** |
| **4 Stromsteuer** | 4 Felder (4.1–4.4) + Sprungknopf (4.5) | Auswahl 2 · Schalter 2 = **4**, **2** Sprungknöpfe | **100 %** + „BHKW-Tarif…" (A-3, aus B5) |
| **— Kohärenzprüfung** | (laut Feldkarte in 3 und 4) | eigener Abschnitt, Zeilen aus `KohaerenzHinweis` | A-2 (aus B5, unverändert) |
| **5 Hilfsstrom** | 5.1 (= 1.17) · **5.2 gestrichen** · 5.3–5.6 Mengenkette · 5.7 Doppelpflege | Basiserläuterung, Mengenkette (2 Zeilen), Doppelpflege-Warnbanner, Kesselhinweis | **100 %**, K1 negativ belegt |
| **6 Vorschau** | 5 Zeilen (Zuschlag · Energiesteuer · Stromsteuer · Einspeiseerlös · Vermiedene) + Prüfhinweise | 5 Zeilen + Standzeile; Prüfhinweise stehen im Kohärenzabschnitt | **100 %** (A-4 aus B5: gebuchter Stand statt live) |

**Kein Feld der Feldkarte fehlt.** Die drei Abweichungen A-1 bis A-4 stammen aus B5 und sind dort
begründet; neu ist allein A-5/A-6 (§ 4).

---

## 4. Abweichungen dieses Ports (mit Begründung)

| # | Abweichung | Begründung |
|---|---|---|
| **A-5** | Die Feldgruppe „Angaben der gewählten Anlage" ist ohne gewählte Zeile **leer** statt gesperrt sichtbar | WinForms konnte elf Felder anzeigen und mit `FelderAktiv(false)` sperren. Im Blazor-Layout wäre eine graue Feldwand ohne Bezug eine Behauptung; an ihre Stelle tritt ein Satz („Keine Anlage gewählt."). Der Zustand ist derselbe, nur ehrlicher gezeigt. Neuer Schlüssel `BHW_A_OHNE_WAHL` |
| **A-6** | Die Anlagentabelle hat eine zusätzliche **Wahlspalte** (○ / ●) | Eine `ListView` markiert die gewählte Zeile selbst; ein `Raster` (QuickGrid) kennt keine Zeilenmarkierung. Der Wahlknopf je Zeile ist der kleinste Ersatz, ist mit der Tastatur erreichbar und meldet sich einer Sprachausgabe über `aria-pressed`. Neuer Schlüssel `BHW_SP_WAHL` |
| **A-7** | **Enter** ist nicht belegt (nur **Esc** schließt) | Im Anlegedialog war Enter das Bestätigen. In einer Maske mit dreißig Feldern und einem nicht schließenden Speichern-Knopf wäre ein versehentliches Enter kein Bestätigen, sondern ein Zufall |
| **A-8** | Eine Fehleingabe **färbt** das Zahlenfeld, statt am `NumericUpDown` geklemmt zu werden | Hausregel `EPOS.UI/CLAUDE.md`: „Eine Fehleingabe färbt das Feld (`epos-fehleingabe`), sie meldet nicht." Der gespeicherte Wert bleibt derselbe, weil ein ungültiger Text gar nicht erst gemeldet wird |
| **A-9** | Der Speicherfehler erscheint als **Warnbanner** statt als MessageBox | `EPOS.UI` kennt keine MessageBox. Der Text ist unverändert (`BHW_MSG_FEHLER`); der Titel `BHW_MSG_FEHLER_TITEL` wird nicht mehr gebraucht (§ 5) |
| **A-10** | Die acht Abschnitte stehen **untereinander** statt in zwei Spalten | Das Hausmaß § 5 (914 × 662, zwei Spalten ab x = 464) ist ein Pixelraster; die Blazor-Fassung fließt und scrollt. Die Reihenfolge der Felder ist unverändert |

---

## 5. Texte — 92 wiederverwendet, 3 neu, 1 verwaist

**Zugriffsmuster.** Die 98 Schlüssel `BHW_*` stehen seit B5 in `EPOS.Kern/MyResource/Resource.resx`
und `Resource.en-US.resx` — aber **nicht** in der erzeugten `Resource.Designer.cs`. Stark
typisierte Eigenschaften gibt es für sie also nicht; `@Resource.BHW_TITEL` übersetzt nicht. Die
Komponente holt sie deshalb über `Resource.ResourceManager.GetString(schluessel, Resource.Culture)`
— genau der Aufruf, den die erzeugten Eigenschaften intern machen, mit demselben Katalog, denselben
Satellitendateien und dem deutschen Rückfall aus Konzept § 6.4. Aufgelöst wird **einmal je Dialog**
in `BhkwWirtschaftlichkeitTexte`, nicht bei jedem Zeichnen.

> **Anmerkung für iU9:** Sobald Visual Studio die Designer-Datei einmal neu erzeugt, existieren die
> Eigenschaften, und der Zugriff kann auf `@Resource.BHW_TITEL` umgestellt werden — die Schlüssel
> ändern sich dabei nicht. `EPOS.Kern/MyResource` wurde in diesem Paket **nicht angefasst**.

**Neue Schlüssel — 3, alle mit deutschem Rückfall im Code, keine `.resx` angefasst:**

| Schlüssel | de (Rückfall) | en (Vorschlag) | Grund |
|---|---|---|---|
| `BHW_SP_WAHL` | Wahl | Select | Kopf der Wahlspalte (A-6) |
| `BHW_A_OHNE_WAHL` | Keine Anlage gewählt. | No unit selected. | leere Feldgruppe (A-5) |
| `BHW_S_SPRUNG_HINWEIS` | Der Sprung schließt diesen Dialog und öffnet ihn danach wieder — bitte vorher speichern. | Following this link closes the dialog and reopens it afterwards — please save first. | nachgelagerter Sprung (§ 2.4) |

**Verwaist — 1:** `BHW_MSG_FEHLER_TITEL` („Fehler") war der Titel der MessageBox; ein Warnbanner
hat keinen. Der Eintrag bleibt in beiden `.resx` stehen (nicht angefasst) und kann beim nächsten
Sammelnachtrag entfallen. Die übrigen fünf nicht von der Komponente benutzten `BHW_*`-Schlüssel
sind weiterhin in Gebrauch: `BHW_KNOPF` und `BHW_MELD_GESPEICHERT` in `UcWirtschaftlichkeit`,
`BHW_PARAM_GRUPPE` / `BHW_PARAM_KNOPF` / `BHW_PARAM_VERWEIS` in `Form_WirtschaftlichkeitParameter`.

---

## 6. WinForms-Seite

* **`Views/Wirtschaftlichkeit/Form_BhkwWirtschaftlichkeit.cs` GELÖSCHT** (1 519 Zeilen, Regel M1).
* `UcWirtschaftlichkeit.btnBhkwWirtschaftlichkeit_Click` ruft
  `BhkwWirtschaftlichkeitHuelle.Oeffnen(Besitzer, _idStamm, _ergebnisse)`. Beschriftung, Lage und
  Sichtbarkeit von `btnBhkwTarif` sind **unverändert** („BHKW-Wirtschaftlichkeit…", x = 182,
  y = 494, sichtbar an `_erzeuger.Bhkw`).
* `Form_WirtschaftlichkeitParameter.btnBhkwWirtschaftlichkeit_Click` (BW9-Sprungknopf) ruft
  dieselbe Hülle ohne durchgereichten Lauf — wie zuvor der einparametrige Konstruktor.
  **BW9 selbst ist unberührt** (§ 7.4).
* Zwei Kommentare, die auf die gelöschte Klasse zeigten, sind auf die Komponente umgeschrieben.

---

## 7. Nachweise

Alle Läufe im Integrations-Worktree, Harness `dev/b5` (gitignored) gegen **frische Kopien** der
Produktivdatenbank im Scratchpad; die Produktivdatenbank wurde nur gelesen (Schutzriegel in
`Program.Main`).

### 7.1 Build

| Probe | Ergebnis |
|---|---|
| `dotnet build WP-Plan.sln -c Debug -p:Platform=x64 -t:Rebuild` | **0 Fehler, 38 Warnungen** — dasselbe Warnungsbild wie die Basis |
| Warnungscodes | 28 × WFO1000, 4 × NU1510, 2 × CS0108, 2 × CS0109, 1 × WFO0003, 1 × CA2255 — **kein neuer Code** |
| Warnungen aus den neuen/geänderten Dateien | **0** (Sweep über das Buildprotokoll nach `BhkwWirtschaftlichkeit`, `Auswahlfeld`, `SpeichernLeiste`) |
| `EPOS.UI` allein | 0 Fehler, 0 eigene Warnungen |

### 7.2 Tests

```
dotnet test WP-Plan.Kern.slnf -c Debug
  KiKern.Tests          450/450
  SpeicherEngine.Tests  337/337
  EPOS.Kern.Tests        35/35
  EPOS.UI.Tests         106/106     (Bestand 64 + NEU 42)
  ------------------------------------------------
  gesamt                928/928     0 rot
```

Die 42 neuen Tests: **40** im Feldkartennetz `BhkwWirtschaftlichkeitDialogTests`, je **1** für die
beiden neuen Baustein-Parameter (`Auswahlfeld.Aktiv`, `SpeichernLeiste.MitAbbrechen`).

Geprüft wird darin — neben dem Feldbestand je Gruppe (§ 3):

* **K1** — kein Vorkommen von „Deckung" im gesamten Markup (Negativbeleg);
* **K3** — Modusfeld vorhanden, `disabled`, zwei Einträge, Auswahl „Ausweis (nicht im
  Kapitalwert)", Vermerk „ab B6 — bis dahin gilt fest „Ausweis" (nicht im Kapitalwert).";
* **K6** — `AnteilPflegbar(11/10/1)` = wahr/falsch/falsch, `AnteilHinweis(11/10/1)` =
  falsch/wahr/falsch; Anteilsfeld sichtbar bei gewählter Zeile; Kesselhinweis nur mit
  `HatHeizkessel`;
* **Warnzeilen** am präparierten Stand (600 kW / 2 500 kW / Heizöl + IBN 2025), wörtlich:
  `Ausschreibung nach § 8a KWKG: Grosses BHKW, Sehr grosses BHKW über 500 kW.` ·
  `Stromsteuerbefreiung § 9 Abs. 1 Nr. 3 entfällt: Sehr grosses BHKW über 2.000 kW.` ·
  `Heizöl-Ausschluss ab Inbetriebnahme 2025: Sehr grosses BHKW.`
  — und dass ohne Katalog die Rückfallgrenzen 500 / 2 000 gelten;
* **Kohärenz** — steuerliche Zeilen im eigenen Abschnitt, Doppelpflege **genau einmal** in Gruppe 5
  (sie kommt aus zwei Quellen und wird über den Text entdoppelt);
* **Mengenkette** — `Stromerzeugung brutto 373,780 MWh/a − Hilfsstrom 0,000 MWh/a =
  Nettostromerzeugung 373,780 MWh/a` / `davon Eigenverbrauch 373,780 MWh/a, Einspeisung 0,000 MWh/a`;
* **Vorschau** — fünf Jahr-1-Zeilen plus Standzeile; Befreiung + Entlastung stehen in **einer**
  Zeile (Bestandsverhalten);
* **Nullsemantik** — `0` im Satzfeld ⇒ `null` in der Anlagenzeile, `0` im Hilfsenergieanteil ⇒
  `0,0` (BF4);
* **Speichernleiste** — Speichern erst nach einer Änderung anklickbar, gelungenes Speichern meldet
  sich in der Statuszeile und schließt **nicht**, gescheitertes zeigt Warnbanner + Fehlerstatus;
* **Schließen und Sprung** — Ergebnis mit `BhkwSprung.Keiner` bzw. `.Strombezug` / `.BhkwTarif`,
  Esc schließt, `Gespeichert` wird im Ergebnis mitgeführt.

### 7.3 K7 — Schreibprobe über den Blazor-Datenpfad (`dev/b5 b5b 1030`, frische DB-Kopie)

Geschrieben wurde über **`BhkwWirtschaftlichkeitHuelle.Speichern(...)`** — die Methode, auf die der
`Speichern`-Rückruf der Komponente zeigt.

| Probe | Ergebnis |
|---|---|
| Leseweg + K4 | 2 Anlagen: `BHKW EW M 50 S [K] Erdgas` (Pel 50, Carrier 63, Brennstoff **Erdgas E**, Heizöl False) · `EC-POWER XRGI 9` (Pel 9, Erdgas E) |
| Ausgangsstand Zeile 14920 | alle elf Spalten `NULL` |
| `Huelle.Speichern(...)` | gescheiterte Sätze **0**; Zeile danach: `03/17/2026 00:00:00 \| 05/04/2027 00:00:00 \| MODERNISIERT \| NR2_KUNDENANLAGE \| 5.57 \| 3.25 \| 30000 \| 3500 \| PARAGRAF_53A \| ENERGETISCH \| 3.5` — **alle elf Spalten kommen an** |
| Idempotenz | zweites Speichern: 0 gescheitert, Rücklesung **bitgleich** („Drift: KEINE") |
| Rückweg (Nullsemantik) | leer/leer/0 und `SatzEinspCt = null` → `NULL \| 3.25 \| … \| NULL \| NULL \| 0` |
| Leseweg zurück | `Wahl='' Methode='' Anteil=0 Stichtag=2026-03-17` |

Der Bestandsaufrufer `Speichere(g)` (acht Spalten) ist unverändert und in B5 § 6.1 belegt.

### 7.4 BW9 unberührt (`dev/b5 bw9 1030`)

| Größe | B5-Protokoll | jetzt |
|---|---|---|
| Steuerelemente | 102, davon 68 sichtbar / 34 unsichtbar | **102 / 68 / 34** |
| „BHKW — KWKG 2025" (y = 285) | AUSGEBLENDET | **AUSGEBLENDET** |
| „BHKW — Energie- und Stromsteuer" (y = 676) | AUSGEBLENDET | **AUSGEBLENDET** |
| Ersatzgruppe y = 285 | sichtbar | **sichtbar** |
| „Brennstoff — BEHG …" | y = 402 | **y = 402** |
| Knopf „⚙ Werte je BHKW-Modul …" | AUSGEBLENDET | **AUSGEBLENDET** |
| Knopf „⚙ BHKW-Wirtschaftlichkeit …" | sichtbar | **sichtbar** |
| Verweiszeile | unverändert | unverändert |

### 7.5 Anker-Lauf (`dev/b5 anker`) — 6/6 zeichengleich

```
Betrieb 1024 = 99,0000                     (Soll 99,00)
Invest  1018 = 45.312,5000                 (Soll 45.312,50)
Invest  1024 = 12.001,0000                 (Soll 12.001,00)
Invest  1042 = 13.000,0000                 (Soll 13.000,00)
KW 1024      = -2219863.761540025          (Soll -2.219.863,7615)
KW 1030      = -21875243.675724894         (Soll -21.875.243,6757)
```

### 7.6 Hüllen-Probe (`dev/b5 b5b`, headless)

```
[B5b-1] Form_BhkwWirtschaftlichkeit: NICHT vorhanden (richtig)
        EPOS.UI-Komponente BhkwWirtschaftlichkeitDialog: vorhanden (EPOS.UI.dll)
[B5b-2] BhkwWirtschaftlichkeitHuelle: vorhanden
          Boolean Oeffnen(IWin32Window besitzer, Int32 idStamm, List`1 ergebnisseAusLauf)
        UcWirtschaftlichkeit.btnBhkwWirtschaftlichkeit_Click(Object sender, EventArgs e)
        Feld btnBhkwTarif: Text='BHKW-Wirtschaftlichkeit…', x=182 y=494
[B5b-4] Konstruktion OHNE Ausnahme. Typ=BlazorDialogForm`1[BhkwWirtschaftlichkeitDialog]
        Titel='BHKW-Wirtschaftlichkeit' ClientSize=914x662 Border=FixedDialog
        Kind: Microsoft.AspNetCore.Components.WebView.WindowsForms.BlazorWebView Dock=Fill
```

Weiter reicht headless nichts: Ob der Inhalt erscheint, entscheidet die WebView2-Laufzeit. Die
Tafel **„Wenn der Dialog leer bleibt"** in [`../../../Umsetzung_iU8_Nachweise.md`](../../../Umsetzung_iU8_Nachweise.md)
nennt die fünf Ursachen und ihre Prüfung.

### 7.7 Veröffentlichung

`dotnet publish WindowsFormsApplication1 -c Debug -p:Platform=x64` → im Ausgabeordner liegen
`wwwroot\index.html`, `wwwroot\EPOS_Plan.styles.css`, `wwwroot\_content\EPOS.UI\`
(`epos-ui.css`, `help_icon.png`), `wwwroot\_framework\blazor.webview.js` und `EPOS.UI.dll`.
Neue statische Web-Anteile bringt B5b nicht mit.

### 7.8 Repoweit

| Probe | Ergebnis |
|---|---|
| `git grep Form_BhkwWirtschaftlichkeit` (ohne `dev/`) | **keine Codestelle mehr** — nur Konzeptdokumente, dieses und das B5-Protokoll sowie zwei erklärende Kommentare, die auf die Komponente verweisen |
| Marker-Sweep (`^<<<<<<<`, `^>>>>>>>`, `^=======`) über `*.cs *.razor *.md *.resx` | **0** |
| Kodierung der neuen Dateien | UTF-8 **mit BOM**, CRLF (`.editorconfig`) |
| Produktivdatenbank | nur gelesen |

`git status --porcelain` (nicht committet):

```
 M EPOS.UI.Tests/Bausteine/SpeichernLeisteTests.cs
 M EPOS.UI.Tests/Standards/FelderTests.cs
 M EPOS.UI/Bausteine/SpeichernLeiste.razor
 M EPOS.UI/Standards/Auswahlfeld.razor
D  WindowsFormsApplication1/Views/Wirtschaftlichkeit/Form_BhkwWirtschaftlichkeit.cs
 M WindowsFormsApplication1/Views/Wirtschaftlichkeit/Form_WirtschaftlichkeitParameter.cs
 M WindowsFormsApplication1/Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.cs
?? EPOS.UI.Tests/Dialoge/BhkwWirtschaftlichkeitDialogTests.cs
?? EPOS.UI/Dialoge/Wirtschaftlichkeit/
?? WindowsFormsApplication1/Views/Wirtschaftlichkeit/BhkwWirtschaftlichkeitHuelle.cs
?? WindowsFormsApplication1/Allgemein/Reporting/B5b_Blazor_Port_Protokoll.md
```

---

## 8. Grenzen

* **WebView2 ist Laufzeitvoraussetzung.** Fehlt sie, startet die Anwendung, aber dieser Dialog
  bleibt leer (beige Fläche). Prüfung und Abhilfe: `Umsetzung_iU8_Nachweise.md`, Kopf und Tafel
  „Wenn der Dialog leer bleibt". Das Setup installiert sie still nach (`Setup/EPOS-Plan.iss`).
* **Die DPI-Insel** (`BlazorDialogForm.ShowDialog` → `PER_MONITOR_AWARE_V2`) greift erst ab
  Windows 10 (1803). Davor ist der Inhalt bitmapskaliert — ein Schönheitsfehler, kein Fehlschlag.
* **Der Infoknopf hat noch kein Ziel.** `help_mapping.txt` führt keine Zeile
  `Form_BhkwWirtschaftlichkeit.btn_Help` — das war in B5 schon so und ist mit dem Port unverändert.
  Der Knopf bleibt sichtbar und folgenlos, bis die Zeile ergänzt wird (**B5b-O2**).
* **Kein Layout-Nachweis ohne Bildschirm.** Alles, was hier gemessen wurde, ist Struktur und
  Verhalten. Wie die Maske aussieht, entscheidet die Sichtabnahme (§ 9).

---

## 9. Abnahmeliste Windows (iZ5) für DIESEN Dialog

Zum Abhaken am laufenden Programm — Wirtschaftlichkeit → **„BHKW-Wirtschaftlichkeit…"**:

*Grundfunktion*

- [ ] Der Dialog öffnet mittig über dem Elternfenster, feste Größe, ohne Minimier-, Maximier- und
      Taskleistenknopf
- [ ] **Kein weißes Aufblitzen** beim Öffnen (die Hülle steht auf `#f5f4ef`)
- [ ] Die Anlagentabelle zeigt **alle** BHKW der Vergleichsgruppe (Stamm **und** Varianten) mit
      Projekt, Bezeichner, P_el, Brennstoff, Stichtag, Inbetriebnahme, Anlagenart
- [ ] Ein Klick auf den Wahlknopf einer Zeile füllt die Gruppe „Angaben der gewählten Anlage" mit
      **deren** Werten; die vorherige Zeile behält ihre Eingaben
- [ ] **Speichern** schließt den Dialog **nicht** und meldet sich in der Statuszeile mit Uhrzeit
- [ ] Nach dem Speichern ist der Speichern-Knopf wieder gesperrt, bis etwas geändert wird
- [ ] **Schließen** kehrt zur Wirtschaftlichkeitsseite zurück; nach einem Speichern erscheint dort
      „BHKW-Wirtschaftlichkeit gespeichert — bitte neu berechnen."
- [ ] Ein zweiter Aufruf zeigt die gespeicherten Werte wieder

*Die festen Entscheide*

- [ ] **K3:** „Modus § 9 Abs. 1 Nr. 3" ist sichtbar, **grau/nicht bedienbar**, steht auf „Ausweis
      (nicht im Kapitalwert)", darunter der Vermerk „ab B6 …"
- [ ] **K1:** Es gibt **kein** Feld „Deckung je Modul"
- [ ] **K6:** Der Hilfsenergieanteil steht bei der gewählten BHKW-Anlage; führt die Gruppe einen
      Heizkessel, erscheint in „Hilfsstrom" der Kesselhinweis
- [ ] **K7:** Nach dem Speichern stehen an der Anlage auch Energiesteuerwahl, Aufteilungsmethode
      und Hilfsenergieanteil (nachzusehen im Modul- oder Datenblick)

*Sprungknöpfe (§ 2.4 — die Designfrage)*

- [ ] „Strombezug…" schließt den Dialog, öffnet die Tarifstruktur (Einkaufsseite) und bringt den
      Dialog danach zurück
- [ ] „BHKW-Tarif…" ebenso mit der BHKW-Sicht (Differenzmethode)
- [ ] Der Hinweis unter den Knöpfen ist verständlich — **oder** der Anwender entscheidet sich für
      ein anderes Muster (B5b-O1)

*Tastatur und Finger (M2)*

- [ ] **Esc** schließt; **Tab** wandert durch Tabelle, Felder und Leiste und bleibt im Dialog
- [ ] Der Erstfokus liegt auf dem Dialog
- [ ] Auf einem Touchgerät sind Wahlknöpfe, Felder und Leiste sicher zu treffen (44 px)
- [ ] Die Auswahllisten öffnen die fingerfreundliche Edge-Liste

*Sprache und Darstellung*

- [ ] **Deutsch:** Titel „BHKW-Wirtschaftlichkeit — \<Stammprojekt\>", Gruppentitel und
      Beschriftungen wortgleich zur WinForms-Fassung
- [ ] **Englisch** (`HKCU\Software\wp-plan\Language` = 1, Neustart): die 92 übernommenen
      `BHW_*`-Texte erscheinen englisch; die **drei neuen** Schlüssel (§ 5) stehen bis zum
      nächsten resx-Nachtrag deutsch da — das ist der bekannte Rückfall, kein Fehler
- [ ] **Hochkontrast-Design:** alle Texte lesbar, Warnbanner als Warnung erkennbar
- [ ] **125 % und 150 %:** der Inhalt ist **scharf** (DPI-Insel), das Fenster passt zum Elternfenster
- [ ] Die lange Maske **scrollt innerhalb** des Dialogs; die Speichernleiste bleibt erreichbar

---

## 10. Offene Punkte

| # | Punkt |
|---|---|
| **B5b-O1** | ~~Designfrage Sprungbrücke~~ **ENTSCHIEDEN 03.09.2026: Variante a** — der nachgelagerte Sprung bleibt (§ 2); kein `IFensterDienst`. Wiedervorlage bei der nächsten iU9-Maske mit Sprungziel |
| **B5b-O2** | `help_mapping.txt` hat keine Zeile `Form_BhkwWirtschaftlichkeit.btn_Help` — der Infoknopf bleibt folgenlos (Bestand aus B5). Ein Wort des Anwenders, welche Wikiseite er zeigen soll, genügt |
| **B5b-O3** | Die drei neuen Schlüssel `BHW_SP_WAHL`, `BHW_A_OHNE_WAHL`, `BHW_S_SPRUNG_HINWEIS` in den nächsten resx-Sammelnachtrag (de + en); `BHW_MSG_FEHLER_TITEL` kann dabei entfallen |
| **B5b-O4** | Sobald Visual Studio `Resource.Designer.cs` neu erzeugt: Textzugriff von `ResourceManager.GetString` auf `@Resource.BHW_*` umstellen (§ 5) |
| **B5b-O5** | A-2 aus B5 bleibt offen: Soll `KohaerenzHinweis` ein ASCII-Artmerkmal bekommen, damit die Zeilen auf „Energiesteuer" und „Stromsteuer" verteilt werden können? Gehört zu B6 (Herleitungstafel) |
| **B5b-O6** | A-4 aus B5 bleibt offen: Live-Vorschau erst, wenn B6 den Rechenweg headless anstoßbar macht |
| **B5b-O7** | `Form_KwkgModule` ist über die Oberfläche weiterhin unerreichbar (BW9) und noch WinForms. Sie ist der nächste Kandidat für iU9 — ihre Felder stehen vollständig in diesem Dialog |

---

## 11. Geänderte und neue Dateien

```
NEU
  EPOS.UI/Dialoge/Wirtschaftlichkeit/BhkwWirtschaftlichkeitDialog.razor      916 Zeilen
  EPOS.UI/Dialoge/Wirtschaftlichkeit/BhkwWirtschaftlichkeitTexte.cs          145
  EPOS.UI/Dialoge/Wirtschaftlichkeit/BhkwWirtschaftlichkeitDaten.cs          227
  EPOS.UI.Tests/Dialoge/BhkwWirtschaftlichkeitDialogTests.cs                 853  (40 Tests)
  WindowsFormsApplication1/Views/Wirtschaftlichkeit/
      BhkwWirtschaftlichkeitHuelle.cs                                        198
  WindowsFormsApplication1/Allgemein/Reporting/B5b_Blazor_Port_Protokoll.md  dieses Protokoll

GEÄNDERT
  EPOS.UI/Standards/Auswahlfeld.razor            + Parameter Aktiv
  EPOS.UI/Bausteine/SpeichernLeiste.razor        + Parameter MitAbbrechen
  EPOS.UI.Tests/Standards/FelderTests.cs         + 1 Test
  EPOS.UI.Tests/Bausteine/SpeichernLeisteTests.cs + 1 Test
  .../Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.cs           Handler auf die Hülle
  .../Views/Wirtschaftlichkeit/Form_WirtschaftlichkeitParameter.cs BW9-Sprungknopf auf die Hülle

GELÖSCHT
  WindowsFormsApplication1/Views/Wirtschaftlichkeit/
      Form_BhkwWirtschaftlichkeit.cs                                       1.519 Zeilen (Regel M1)

HARNESS (gitignored)
  dev/b5/Program.cs                              + Schritt „b5b"
```

# Statusblöcke des Umsetzungskonzepts iOS (bis 12.09.2026)

**Herkunft:** ausgeschnitten am 12.09.2026 aus
[`../../../aktuell/Umsetzungskonzept_iOS_EPOS-Plan.md`](../../../aktuell/Umsetzungskonzept_iOS_EPOS-Plan.md)
(Rev. 2.2). Das Konzept enthält seither nur noch das Konzept; der fortgeschriebene Stand steht in
[`../../../aktuell/Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md).

**Diese Datei wird nicht fortgeschrieben.** Sie ist die wortgetreue Ablage der Blöcke, so wie sie im
Konzept standen — Reihenfolge wie im Quelldokument, je Block eine Überschrift.

**Zu den Commit-Kennungen:** Sämtliche in den Blöcken genannten Kennungen stammen von **vor** der
Geschichtsumschreibung vom 12.09.2026 und zeigen im heutigen Zweig auf nichts mehr. Die Zuordnung
alt → neu steht in
[`../../Geschichte/commit-map_2026-09-12.txt`](../../Geschichte/commit-map_2026-09-12.txt).
Die in den Blöcken genannten Basis-Kennungen waren ohnehin die **Entwicklungsbasen** der jeweiligen
Tranche, nicht ihre Elternteile auf dem Zweig.

**Relative Verweise** in den Blöcken sind beim Umzug auf diesen Ort umgerechnet worden; Anker und
Adressen im Netz stehen unverändert.

---

## 4.0 Gesamtübersicht — Stand 03.09.2026

### 4.0 Gesamtübersicht — Stand 03.09.2026

Der Stand aller Pakete auf einen Blick. **„Hier erreicht" heißt: auf Linux gebaut, getestet und
gegen die Referenzbasis gefahren** — die Spalte ganz rechts nennt, was dafür ein Windows braucht.

| Paket | Stand | Commits auf `ios_migration` | auf Windows offen |
|---|---|---|---|
| **iU0** Klärung, Sicherung, Rückbau | ✔ erledigt 02.09. | `c3a8233`, `1ab062d` | Anwender trägt Entscheide und Termine ein |
| **iU1** .NET 10, Windows, CI | ✔ erledigt 02.09. | `c3a8233`..`ce2dc9e`, P1.11 `0c83dba` | Referenzlauf **332/332** (iZ1), Proben 16/16, Excel-Import, Setup |
| **iU2** Mac und Apple-Konto | ⏳ nicht begonnen | — | alles (Hardware, Xcode, Konto) |
| **iU3** Machbarkeits-Spike | ✔ **bestanden** 02.09. (iZ3) | `13cedbb`..`db9f00f`, `edefbef`, `e3bd586` | — (auf Linux **und** arm64-macOS byte-gleich) |
| **iU4** `EPOS.Kern` herauslösen | ✔ hier erreicht 03.09. | `4a0a4e2`..`18f515f` | Vollreferenzlauf **332/332** (iZ4), VS 2026 öffnet 12 Projekte |
| **iU5** Statics kappen, Dienste | ✔ hier erreicht 03.09. (iZ5a) | `35be81f`..`c477523`; zweiter Umzug `a546af9`..`a9e5c16`, Doku `f95fc34` | Bedienprobe: Bericht, Katalogimport, Lizenzaktivierung, KI-Chat, 12 Gewerke, Sprachumschaltung |
| **iU6** Datenzugriff plattformfrei | ✔ hier erreicht 03.09. | `22fb7eb`..`300a354` | Erststart-Migration aus `.accdb`, Solar-/Pufferspeicherdialoge, die 36 `RecordSet`-Views |
| **iU7** Charts und Berichte | ✔ hier erreicht 03.09. | `c6b32eb`..`f84932b`, `6604c05`..`0759b37`, `0af6421` | `Referenzlauf.exe bildvergleich` alt/neu (Vorbedingung für iF23)  Zoom seit 05.09.2026 über den Baustein `Diagramm` (Nachtrag im W11b-Block) |
| **iU8** `EPOS.UI`, erster Dialog | ✔ **iZ5 hier erreicht** 03.09. | A `8574911`..`8f5a28e`, `45a21dc`, `f5fb05c` · B `4369fdb`..`eafbc1f`, `eff82aa`, `e3d1e5b` · C `479fcf9`..`0af7ca7`, `4aa6b15` | Dialogabnahme (Maus/Finger, de/en, Hochkontrast, 125 %/150 %, Enter/Esc), Setup mit und ohne WebView2, VS-2026-Designer unter dem Razor-SDK. Anwenderwunsch **iU8‑E‑1** (05.09.2026, `ddf4d00`): Fachdialoge öffnen im Anteil des Arbeitsbereichs (85 % × 90 %, Deckel 92 %; `EPOS.UI/Dienste/Fenstermass.cs`), fünf kleine Masken als `Dialogart.Klein`; Abnahme je 100/125/150 % offen; **iU8‑E‑2** (05.09.2026, `6ab0b9f`): hausweite Formularregel — Baustein `Formularraster`/`Formulargruppe`, Beschriftung neben dem Feld, kurze Zahlenfelder mit Einheit, `auto-fill`-Spalten; acht Dialoge umgestellt, Rest als #91 in drei Paketen — Restumstellung #91 in drei Paketen abgeschlossen (P1 Erzeuger W6/W7, P2 Kosten W1–W5, P3 Bedarf/Simulation/Projekt W8–W16a: 41 weitere Dateien, alle `epos-feldpaar` gefallen; iU8‑O‑1 am 06.09. geschlossen (`e6bc2fd`, `PufferSpProjektDialog` im Raster) `PufferSpProjektDialog`) |
| **iU9** Masken in Wellen | ✔ **W0 bis W16 umgesetzt, M9 abgeschlossen** 04.09. | `ab3aea8` | **1** Designer-Maske offen (`Form_HelpPopup`, bis iU11; 2 nach W16b, 7 nach W16a, 11 nach W15c, 12 nach W15b, 13 nach W15a, 17 nach W14c, 21 nach W14a, 28 nach W14b, 32 nach W13, 38 nach W12, 43 nach W11b, 49 nach W10b, 50 nach W10a, 55 nach W9, 63 nach W8, 73 nach W7, 81 nach W6, 88 nach W5, 91 nach W4, 98 nach W3, 102 nach W2, 105 nach W0); Stilllegung nach iF29 abgeschlossen, Sprungbrücke steht, `ChartRenderer` um Kostenprofil, Kennlinien und die drei Bedarfsbilder erweitert, seit W5 die erste **Seite** (`BlazorSeite`, Reiter „Berichte & Kosten"), seit W6–W9 **alle elf Kacheln des Startbilds** (sieben Erzeuger, vier Bedarfe) und **zehn der dreizehn Assistentenseiten** als Razor-Komponenten; `WPCtrl`, `BedarfStammCtrl`, `TypProfilCtrl`, `Ferienzeit`, die Projektlisten der Bedarfsgewerke und das Suchmuster im Kern; seit W10a die sieben Quell-, Senken- und Pufferdialoge der Simulationskonfiguration mit dem Baustein `Bildkarte` und dem zweireihigen `Jahresgang`, seit W10b die **Simulationskonfiguration als Seite** mit Kartenspalten, SVG-Schema (`SchemaModell`/`SchemaLayout` im Kern) und drei Überlagerungsebenen in einer WebView; seit W11a die Ergebnisrechnung der Detailansicht als DTOs im Kern (`SimulationErgebnisCtrl`), der **nebenläufige Simulationslauf** (`SimulationLaufCtrl`, `Do_Simulation` mit Fortschritt und Abbruch), sieben Ergebnisbilder im Renderer (30 Proben) und der Baustein `Fortschritt`; seit W11b die **Ergebnisseite der Simulation** (`SimulationErgebnisSeite`, zehn Blätter, Autarkie, Ganglinien-Navigatoren, Variantenvergleich als Überlagerung) — `Form_Simulation_Detail` mit 7 766 Zeilen ist gelöscht; seit W12 die **AP5-Importkette als ein Kern-Ablauf** (`GanglinienImportAblauf` mit zwölf bitgleichen Proben), `StromganglinieDialog`, `StromganglinieAdminDialog`, `PeakShavingDialog` (nebenläufig) und der gemeinsame `ImportKonflikteDialog`; seit W13 die **Katalog-Importe** — `KatalogImportDialog` mit vier Ausprägungen (`KatalogImportProfil`/`KatalogImportAblauf` im Kern, transaktional), `WaermebedarfAdminDialog`, `PvModulImportDialog` (CEC/PAN), die Mehrfachmarkierung im `Raster` und zwanzig eingefrorene Importproben; die `ImportKonflikteHuelle` und die Sprungbrücke `WaermebedarfExternAdmin` sind gefallen; seit W14b die **Bedarfs-Admin** — `BedarfAdminDialog` mit drei Ausprägungen über `BedarfsArt` (`BedarfsVorschauCtrl` im Kern) und `SolarganglinieAdminDialog` (Sprungziel → Überlagerung), `ToolsClass` gefallen; dazu der Anwenderentscheid **Energieeinheit MWh/kWh wählbar** (W8‑O‑5/W9‑O‑3); seit W14a die **Erzeuger-Admin** — `KatalogBrowserDialog` mit vier Ausprägungen (`KatalogBrowserProfil`), `PufferSpKatalogDialog` (der vierte Katalogeditor), `ModulKatalogDialog` (PV, Stromspeicher), die Heizkessel-Brennstoffkette im Kern berichtigt, die letzten fünf ablösbaren Sprungziele → Überlagerungen, `SpeichernLeiste`/`KiAufrufKnopf`/`PufferSpFilter` gefallen; **der Erreichbarkeitsbefund steht auf 0 nein / 0 verwaist / 0 unklar**; seit W14c die **Verwaltung** — `GesetzeskatalogDialog` (Zeilendialog als Überlagerung), `KatalogDublettenDialog` mit dem Baustein `Baumansicht`, `EinstellungenDialog` (`EinstellungenCtrl` im Kern), `KlimadatenDialog` (`KlimaregionStammCtrl` und `KlimaImportAblauf` im Kern, zwei Klimabilder im Renderer → 32 Proben); die **letzten zwei ablösbaren Sprungziele** → Überlagerungen, `ChartManager` (die MS-Chart-Bindung) und `RoundedPanel` gefallen, **WFO1000 6 → 0**, Warnungen der Mappe 12 → 6; **Anwenderentscheide W14c E‑3/E‑5/E‑6/E‑7 vom 04.09.2026 umgesetzt** (`a0e6707`: Komponente wieder `KlimadatenDialog`, feste Pfade ohne Ordnerwähler nur lesend, **Altbereinigung der Klimadaten-Waisen als Schema-Schritt 62** — `ZIEL_VERSION` 62, neu `FREEZE_VERSION` 61 —, keine Ortsliste in der Auslieferung); seit W15a das **Projekt** — Baustein `ProjektListe` (vier Projektlisten des Bestands werden eine), `ProjektWahlDialog` (Öffnen und Löschen), `ProjektKopieDialog`, `ProjektTransferDialog` (`ProjektExportImportCtrl` im Kern, `SchemaStand.Zielversion`; **der Projektimport war seit der SQLite-Umstellung kaputt, B55 — von den Proben gefunden und behoben**), `ProjektKopfSeite` (die erste Assistentenseite als Razor, über `BlazorAssistentSeite`); `ProjektAuswahl` (uc) bleibt bis W16; seit W15b **Hilfe und KI** — `KiChatService` (1 751 Z.) im Kern hinter der Naht `IKiAusfuehrung`, die Bausteine `Gespraechsverlauf` (Bausteinlücke 17) und `KiKnopf`, `Warnbanner.Verfaellt`, `TextAnzeige`, `KiHinweisDialog`, `KiEinstellungenDialog`, `KiChatDialog` in vier Kindern (kein Streaming, kein Markdown, Schlüssel nie durchgereicht, Riegel vor dem `Modellkanal`); `Form_HelpPopup` (E‑2, fällt in iU11) und `Form_Hinweis` (E‑1b, fällt mit W16) bleiben bewusst; seit W15c **Lizenz und Erststart** — `LizenzVerwaltungDialog`, `ErststartDialog` (besitzerlose Hülle mit vier Zusätzen an `BlazorDialogForm`) und `LizenzDialog` (drei Reiterblätter, Zustimmungsmodus, Browserdruck), im Kern `LizenzManager.Bewerten`, `LizenzCtrl`, `LizenzTextCtrl`, `ZustimmungCtrl`; **die ersten Lizenztests überhaupt** (+79 Kern-, +67 bunit-Fälle); **E‑8 Weg 2: `Program.Main` prüft die WebView2-Laufzeit und endet mit Meldung, wenn sie fehlt**; iF30 (Lesemodus-Durchsetzung) nach W16; seit W16a **der Assistent** — `KomponentenBestandCtrl` im Kern mit **Nachweis N6** (Bitmaske bitgleich für alle 13 Referenzprojekte), `AssistentCtrl` und `WizardCtrl` im Kern, Baustein `Assistent`, Seite `AssistentSeite` (13 Seiten in Bestandsreihenfolge), `KomponentenauswahlDialog`, `Kachel.Zustand`; `WizardParent`, `Wizard_Komponenten`, `Wizard_Stromlastgang`, `ProjektAuswahl` (uc) und `BlazorAssistentSeite` gefallen (26 Dateien); `Views/Wizard` und `Views/Projekt` führen keine Designer-Maske mehr; seit W16b **die Startseite** — Seite `Startseite` (`EPOS.UI/Seiten/Start/`, Kopfband, sechs Reiter mit 21 Kacheln in fünf Reiterkomponenten, Reiter 6 = `BerichteKostenSeite`), im Kern `ProjektKontextCtrl` (**Nachweis N7**), `StartseiteCtrl` und `BedarfsZustand`, `StartseiteHuelle` im `MDIMainForm_Load`, `Dienste.Projekt` über den Kern; **E‑5** (Simulationskonfiguration als freie Ansicht, Ergebnis als `Ueberlagerung` — **R‑W10b‑1 und R‑W11‑1 eingelöst**), **E‑7** (`FormMain`, `Form_StromTest`, `StromTestClass` und zwölf `*KontextMenuCtrl` ohne Nachfolge gefallen), **E‑9** (`Form_Start`-Designer als Prüfmuster eingefroren); `Form_Start` (+`.bak`), `AktionsKarte`, `Form_Hinweis`, `FormStartProjektKontext` gefallen — **34 Dateien, 13 019 gegen 5 549 Zeilen**; `Program.startfrm` weg; `WindowsFormsApplication1` führt noch `MDIMainForm` und `Form_HelpPopup` und **null Inline-SQL** (B34); seit W16c **das Hauptfenster** — Baustein `Menueband` mit der aus dem Designer erzeugten `Menuetabelle` (54 Punkte, **Nachweis N4**), Seite `Hauptfenster` hinter `HauptfensterHuelle`, `Seitenschluessel` als die eine Schlüsseltabelle beider Plattformen (K7), `AppWurzel` als gemeinsame Wurzel (E‑1); `MDIMainForm` auf die Hülle zurückgebaut (873 → 129 Zeilen, Designer und drei `.resx` als Prüfmuster, E‑9) und in `Hauptfensterrahmen` umbenannt (E‑10), **Per Monitor V2 statt `DpiInsel` (E‑6, iF21)**, Zeugen und Schwellen der Formularkarte auf N1/N2; **die Mischphase (M9) ist zu Ende** — `WindowsFormsApplication1` führt eine Designer-Maske (`Form_HelpPopup`, bis iU11), null Inline-SQL und die `Sprungbruecke` mit einem Zweig (iF22) |
| **iU10** iOS-Hülle `EPOS.iOS` | ✔ hier erreicht 03.09., seither je Welle im CI geprüft | `ios.yml`-Läufe 15–22 grün (außer 18), zuletzt 33898599945 auf `c8fbd77` | Gerätebefunde (iU13), siehe `Umsetzung_iU10_Nachweise.md` |
| **iU11**–**iU13** | ⏳ nicht begonnen | — | — |

**Die Reihenfolge auf dem Zweig ist nicht die Reihenfolge der Planung.** iU5 bis iU8 sind in
eigenen Worktrees entstanden und per Cherry-Pick übernommen worden; die SHAs sind dabei neu
vergeben worden. Auf `ios_migration` folgen nach `18f515f` (iU4-8): iU8-1…5/8a/5b → iU7-1…4 →
iU6 → iU8-5c, iU8-12 → iU7-5…8 → iU5-T0…T5 → iU8-6…13 → iU5-U1…U5 mit iU7-9. Die in den
Statusblöcken genannten Basis-SHAs sind die **Entwicklungsbasen**, nicht die Elternteile auf dem
Zweig. Für die Nachweise ist das ohne Belang: Jede Tranche ist einzeln gebaut, getestet und
gegen `2026-08-30_B3-Kaskade` gefahren worden.

**Die drei Zahlen, an denen die ganze Kette hängt** (Linux, SDK 10.0.400, Stand `f95fc34`):

| Messung | Wert | Verlauf |
|---|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64` | **0 Fehler, 34 Warnungen** | 123 (iU4/iU5) → 36 (iU6, die 87 CA1416 fallen weg) → 34 (iU8-9, zwei WFO1000 mit dem gelöschten Formular) |
| `dotnet test WP-Plan.Kern.slnf -c Release` | **886** | 787 (Stand iU1) + 9 (iU4-6) + 9 (iU6-T4) + 14 (`DiensteTests`, iU5-T0…T5) + 3 (Renderer, iU7-8) + 64 (`EPOS.UI.Tests`, iU8-5…8a) = **886**. Die Zwischensummen der Statusblöcke (796, 805, 810, 872) sind die Messungen der jeweiligen Entwicklungsbasis |
| Referenzlauf 1030/1007/1017 gegen `2026-08-30_B3-Kaskade` | **GESAMT PASS, byte-gleich** | nach **jeder** Tranche, 815 043 Werte |

## Statusblock iU1 — umgesetzt 02.09.2026

> **Umgesetzt 02.09.2026 auf Branch `ios_migration`, Commits `c3a8233`..`0ddc417`,
> P1.8 `dab063a`, P1.10 `ce2dc9e`, P1.11 `0c83dba`; Nachweis hier geführt, CI Kern + Windows grün,
> Nachweis Windows-Ausführung offen (iZ1).**
> Die Abnahmeliste je Commit steht in
> [`Umsetzung_iU0_iU1_Nachweise.md`](../../Umsetzung_iU0_iU1_Nachweise.md).

## Statusblock iU3 — Stand 02.09.2026 (Machbarkeits-Spike bestanden)

> **Status 02.09.2026 — bestanden.** Umgesetzt als `EPOS.Kern` (91 verlinkte Dateien, `net10.0`
> ohne WinForms) + `EPOS.Referenzlauf`, Commits `13cedbb`…`db9f00f`. Projekt 1030 auf Linux x64
> gegen `2026-08-30_B3-Kaskade`: **PASS, 22 Dateien byte-identisch.** Vorbedingungen, die das
> Konzept nicht kannte: `DbParam` statt `OleDbParameter` (Test B) und der Kern in einer Assembly
> ohne `UseWindowsForms` (Test A) — beides erledigt. Der Spike lief ohne Mac; die ARM64-Frage (iF15)
> hat der `macos-latest`-Schritt der CI beantwortet: **auf Apple Silicon (`macos-26-arm64`) ebenfalls PASS
> und byte-gleich** (Lauf `edefbef`). Einzelheiten: Entscheidungsregister § 2.2/2.3.

## Statusblock iU4 — Stand 03.09.2026

> **Status 03.09.2026 — hier erreicht, Windows-Nachweis offen.** Sieben Commits
> (`4a0a4e2` iU4-1 … `616dff4` iU4-7) auf `origin/ios_migration` = `9fe9c71`.
>
> **Umfang:** `EPOS.Kern` führt **168 Dateien**, die seit iU4-5 physisch unter
> `EPOS.Kern/` liegen (`git mv`, Ordnerstruktur `Allgemein/…`, `Controller/`, `Model/`,
> `MyResource/`, `Properties/` erhalten). Die Anwendung übersetzt sie nicht mehr, sondern
> referenziert das Projekt; sie schrumpft von 585 auf 417 `.cs`. Der Weg dorthin ging
> über iU4-4: Erst wurde der volle Umfang **verlinkt** und übersetzt — der
> Portabilitätsbeweis vor dem Umzug, Wächter `EnableWindowsTargeting=false`.
>
> **Gekappte Kanten (iU4-1 bis iU4-3):** `Sprache` und `ZahlText` lösen die letzten
> `Program`-Statics des Kernpfads ab (`Program` leitet weiter); acht Schema-Konstanten
> ziehen von `SchemaMigration` nach `SchemaStand`; `KomponentenUebernahmeCtrl` ruft
> `AnlagenSql` direkt statt über `WizardCtrl`; sieben `MessageBox.Show` werden zu
> `Meldung.*`; der Geräte-Aufräumlauf läuft über den Haken
> `WErzeugerCtrl.GeraetewaisenAufraeumen`. Die beiden `partial`-Aufteilungen über die
> künftige Assemblygrenze hinweg sind aufgelöst: `WizardItemClass` bekommt mit
> `WizardSeite` einen abgeleiteten Oberflächentyp, die `FillComboBox`-Hälften werden
> Erweiterungsmethoden in `ControllerListen`. Fünf `*Ctrl.WinForms.cs` entfallen
> ersatzlos (0 Aufrufer).
>
> **`InternalsVisibleTo`** auf `EPOS_Plan` und `EPOS.Kern.Tests` — etliche Controller und
> Modelle sind ohne Zugriffsangabe deklariert und damit `internal`; die Alternative wäre
> eine breite Sichtbarkeitsänderung am Bestand ohne fachlichen Grund gewesen.
>
> **Nachweis hier:** `dotnet build WP-Plan.sln -c Release -p:Platform=x64` → 0 Fehler,
> **123 Warnungen** (EPOS.Kern 89, App 34) — dieselbe Summe wie vor der Etappe.
> `dotnet test WP-Plan.Kern.slnf` → **796** (787 + 9 neue). Referenzlauf **1030, 1007 und
> 1017** gegen `2026-08-30_B3-Kaskade`: **GESAMT PASS, alle drei byte-gleich.**
>
> **Offen nach iU4:** `Program.*`-Statics und `MessageBox` in `Views/` (iU5), `IDatenzugriff`
> (iU6), die AUSGABE-Hälfte des Berichts samt `ChartRenderer` (iU7), `EPOS.UI` (iU8), die
> Maskenwellen mit den 432 `OleDbParameter`-Altaufrufen und WFO1000 (iU9), `IEinstellungen`
> und `ILizenzAblage` (iU11). In der Anwendung bleiben mit Absicht: `SchemaMigration` samt
> Access-Zweig, `SchemaModell`, `GeraeteWaisen`, `ErststartMigration`,
> `AnlagenEindeutigkeit`, `Katalog/`, `Import/` (außer `AnsiEncoding`), `WizardCtrl`, die
> `*KontextMenuCtrl`, `MenueCtrl`, die Stamm-Controller mit `MessageBox`, `KI/`, `Hilfe/`,
> `GrafikTools/`, `Export/`, `Lizenz/` und `WPCtrl`.

## Abnahmestand iU4 (§ 4, Absatz „Abnahme (iZ4)“)

**Hier erreicht** (Linux, drei Projekte byte-gleich); der Windows-Durchlauf 332/332 steht aus.

## Statusblock iU5 — Stand 03.09.2026

> **Status 03.09.2026 — Abnahmekriterium erreicht, Windows-Bedienprobe offen.** Sechs
> Commits (`35be81f` iU5-T0 … `9235a92` iU5-T5) auf der Basis `18f515f`.
>
> **Das Abnahmekriterium ist maschinell erfüllt:**
> ```bash
> git grep -nE '\bProgram\.[A-Za-z]' -- 'EPOS.Kern/*.cs' \
>     'WindowsFormsApplication1/Allgemein/*.cs' \
>     'WindowsFormsApplication1/Controller/*.cs' \
>     'WindowsFormsApplication1/Model/*.cs' | grep -vP ':\s*(///|//|\*)'
> # → 0 Treffer  (Basis: 53)
> ```
>
> **Der Halter statt eines Containers.** `EPOS.Kern/Allgemein/Dienste/` führt neun
> Schnittstellen mit Standardfassungen und den statischen Halter `Dienste`; die
> Windows-Fassungen liegen in `WindowsFormsApplication1/Dienste/` und werden in
> `Program.Main` **vor** `DataRepository.DatenbankVorhanden()` eingelegt. Ein
> DI-Container ist im Bestand fremd — acht austauschbare Haken (`Meldung`,
> `KiTexte.Lieferant`, `KiEinwilligung.Nachfragen`, `KiAusfuehrer.*`,
> `AnlagenEindeutigkeit.Frage`, `SimulationControl.Speicherlauf` …) tragen ihn bereits;
> von den 22 rufenden Klassen sind etliche rein statisch. Begründung im
> Entscheidungsregister § 2.6.
>
> **`Meldung` bleibt** und zeigt seit T0 selbst auf `Dienste.Dialog`. `Program.Main`
> belegt deshalb nur noch `Dienste.*`. Beabsichtigter Nebeneffekt: Die Hinweisdialoge
> des Kerns tragen unter Windows wieder das **Informationssymbol**, das sie bis iU3-2
> hatten.
>
> **Tranchen und Zahlen.**
>
> | Tranche | Commit | Dateien | Fundstellen | Warnungen | Tests | Referenzlauf |
> |---|---|---|---|---|---|---|
> | T0 Halter, Schnittstellen, Adapter | `35be81f` | 33 neu + 4 | — | 123 / 89 | 809 | PASS |
> | T1 Dialoge | `4118ed0` | 14 | 33 `MessageBox.Show` | 123 / 89 | 809 | PASS |
> | T2 Pfade und Dateien | `8add154` | 13 | 12 `SpecialFolder`, 2 Dateidialoge, 1 `Process.Start` | 123 / 89 | 809 | PASS |
> | T3 Einstellungen und Lizenz | `d477a77` | 11 | ~30 Registry, 10 DPAPI, 3 `Settings` | 123 / 89 | 809 | PASS |
> | T4 Sprache | `b9fecf0` | 5 | 5 `nLanguage` | 123 / 89 | 809 | PASS |
> | T5 Navigation und Kontext | `9235a92` | 21 | 32 `Set*Control`, 25 `ShowDialog`, 9 `startfrm` | 123 / 89 | 810 | PASS |
>
> Warnungen „App / Kern"; die Summe **123** ist unverändert die Basis von iU4.
> Referenzlauf jeweils **1030, 1007, 1017** gegen `2026-08-30_B3-Kaskade`:
> **GESAMT PASS, 815.043 Werte**, `diff -rq` nur `protokoll.txt` zusätzlich.
>
> **Rückbau (M4).** `System.Management` als Paketreferenz entfernt (vorher bestätigt:
> null Treffer auf `System.Management`, `ManagementObject`, `ManagementClass`, `Win32_*`
> im ganzen Repo — die Geräte-ID lief nie über WMI). `Program.StartLocalWebServer` /
> `StopLocalWebServer` samt fester Pfadangabe `C:\WPFake` gelöscht (34 Zeilen, einziger
> Aufruf seit jeher auskommentiert). Die Dublette
> `RegPfad = @"Software\EPOS_PLAN\Variantentest"` auf eine Konstante gelegt.
>
> **Ausnahmen, die bewusst stehen bleiben.**
>
> | Fundstelle | Warum |
> |---|---|
> | `DataRepository.cs` — `Properties.Settings.DBPath/DBName`, `CommonApplicationData` | tabu in iU5, gehört zu iU6 |
> | `ErststartMigration.cs` — `Properties.Settings.Save()` | Access-Zweig, bleibt mit der Erststart-Migration in der Anwendung |
> | `HelpExtender` in `Hilfe/HelpCatalog.cs` — `new Form_HelpPopup()` | Oberflächenbaustein wie `HilfeAutomatik`/`InfoKnopf`, geht mit iU9 |
> | 12 `*KontextMenuCtrl` — 44 `new Form_X` / `ShowDialog` | **siehe unten** |
> | `EPOS.Kern/Allgemein/Dienste/StandardPfade.cs` — `SpecialFolder` | die Standardfassung von `IPfade` selbst; genau dort gehört es hin |
> | `SimulationControl`/`SimulationKanaele` (`speicherRegistry`), `SchemaMigration` (`KatalogRegistry`) | Wortteil, kein Registry-Zugriff — der Wächterausdruck führt kein `\b` |
>
> **Warum die Bearbeitungsdialoge der Kontextmenüs bleiben.** Sie sind keine
> `Program`-Bindung, sondern eine Maske-zu-Maske-Kopplung: Der Controller füllt
> `frm.list_werzmodel` mit typisierten Modellen, setzt `frm.m_nType`/`m_ID_Projekt`,
> ruft `frm.SetControls(…)`, zeigt und liest die Liste zurück. Ein Schlüssel plus
> `object[]` bildete das nur ab, indem der halbe Controller in `WinFormsNavigation`
> zöge. Diese Klassen sind ohnehin **Oberflächenbausteine** — sie führen `ListView`,
> `ContextMenuStrip` und `MouseEventHandler` und können nie in den plattformfreien Kern;
> sie wandern mit ihren Masken in iU9. `MenueCtrl` dagegen ist vollständig umgestellt und
> braucht kein `using System.Windows.Forms` mehr.
>
> **Windows-Prüfpunkte für die Abnahme am Gerät.** Registry-Werte werden unverändert
> gelesen (`Language`, `GeminiApiKey`, `KiZaehler`, `KiHinweisBestaetigt`,
> `CsvExportPfad`, `LizenzAnker`, `LizenzZugestimmt`); die Lizenz bleibt aktiviert und
> der KI-Schlüssel lesbar (DPAPI-Geltungsbereiche unverändert `LocalMachine` bzw.
> `CurrentUser`); Umschalten de↔en wirkt nach Neustart wie bisher; alle zwölf Gewerke
> öffnen und speichern über das Kontextmenü; die vier Ja/Nein-Rückfragen antworten in
> beide Richtungen richtig — die Projektlöschung mit Fokus auf „Nein"; alle 19
> Stammdaten- und Einlesemasken aus dem Menü; CSV-Export schlägt den zuletzt benutzten
> Ordner vor; Hilfe-Zwischenspeicher unter `%APPDATA%\EPOS-Plan` und Lizenz unter
> `%APPDATA%\wp-plan` bleiben liegen.
>
> **Offen nach iU5:** der Windows-Vollreferenzlauf 332/332, die Eingabehelfer
> `Program.Zahl*`/`Ganzzahl*` mit ihren Masken (iU9) und die genannten Ausnahmen.
>
> **iU5-Abschluss: der zweite Umzug (03.09.2026).** Fünf Commits (`a546af9` iU5-U1 …
> `a9e5c16` iU5-U5) auf `e3d1e5b`, dem letzten Commit von iU8. Nachdem iU5 den Kernkandidaten die Umgebung
> abgenommen hatte, ist die Frage „was kann noch mit?" nicht mehr geschätzt, sondern
> **gemessen worden**: Jede Datei unter `Allgemein/` und `Controller/` wurde nach
> `EPOS.Kern/` verschoben und der Kernbau mit `EnableWindowsTargeting=false` als Wächter
> laufen gelassen; was er ablehnte, ging unverändert zurück. **74 von 136 Dateien sind
> mitgegangen** — der Kern wächst von **194 auf 268** `.cs`-Dateien; unter
> `WindowsFormsApplication1/Allgemein/` und `/Controller/` bleiben **62**. (Die 136 sind 84 + 49
> aus `c477523` plus die drei Hüllendateien, die iU8-6/iU8-7 dazwischen angelegt haben —
> `Blazor/BlazorDialogForm.cs`, `Blazor/BlazorDienste.cs`, `Hilfe/WindowsHilfeDienst.cs`. Der
> zweite Umzug lief auf `e3d1e5b`, also **hinter** dem ganzen iU8-Strang B.)
>
> | Tranche | Commit | Verschoben | Zurück (Grund) |
> |---|---|---|---|
> | iU5-U1 | `a546af9` | **20** — `Lizenz/` (4), `Export/` (1), `Import/` (12), `Katalog/` (3) | keine |
> | iU5-U2 | `5cb807c` | **11** von 25 aus `KI/` — Wissen, Semantik, Texte, Schutzstufen, Einwilligung | **14**: `KiDialogZugriff`, `KiAusfuehrer`, `HilfeKontext`, `KiAufrufKnopf` (lebende `Control`/`Form`); `KiAktionenDialog` (+ `HelpEntry`); `KiAktionenProjekt`/`-Schreiben` (`OleDbException`); `KiChatService`, `KiAktionenSitzung`, `KiAktionen` (+ `KiHilfe`), `-Energie`, `-Uebernahme`, `-Wirtschaft`, `KiAktionenLastgang` (`GanglinienEintrag`) |
> | iU5-U3 | `82807f4` | **9** — die Bericht-AUSGABE: Word, Excel, `Bausteine/` (4), `IBerichtsBaustein`, `BerichtsKonfiguration`, `ZeitreihenExtraktor` | **2**: `ChartRendererGdi` (GDI+, von vornherein ausgenommen), `BerichtsDatenSammler` (`EnergieMengen` aus `Views/Varianten/`) |
> | iU5-U4 | `c67fe36` | **29** von 47 Controllern — sieben Stamm-, vierzehn Projekt-, fünf Zuordnungs-Controller, `BerichtCtrl`, `SpotpreisImportCtrl`, `SpotpreisLeser` | **18**: die 12 `*KontextMenuCtrl`, `WPCtrl` (+ `.WinForms.cs`, `partial`), `KlimaregionStammCtrl` (`ComboBox`/`ListBox`), `WizardCtrl`/`MenueCtrl` (`WizardParent`), `EnergietraegerKatalogCtrl` (`EnergyCarrier`), `PeakShavingCtrl` (`OleDbException`), `ProjektExportImportCtrl` (`SchemaMigration`) |
> | iU5-U5 | `a9e5c16` | **5** — `ToolsClass`, `FileDlgClass`, `Hilfe/DokuUebersetzung`, `Update/AnlagenEindeutigkeit`, `chart_test` | **3**: `StromTestClass` (`WPCtrl`), `IAssistentRahmen` (`WizardSeite`), `Simulation/SchemaModell` (`Form_Waermesenke`) |
>
> **Ein einziger inhaltlicher Eingriff.** `Bausteine/BausteineStandard.cs` las die
> Programmfassung über `System.Windows.Forms.Application.ProductVersion`; an ihre Stelle tritt
> `DeckblattBaustein.ProduktFassung()` mit derselben Reihenfolge (informelle Fassung des
> Einstiegs-Assemblies → `FileVersionInfo.ProductVersion` → `"1.0.0.0"`). Weil die Anwendung
> `GenerateAssemblyInfo=false` setzt und nur `AssemblyFileVersion 1.1.0.0` deklariert, zeigt das
> Deckblatt unter Windows unverändert `1.1.0.0`. Dazu **17 tote `using`-Zeilen** entfernt
> (`System.Windows.Forms` 15×, `Microsoft.Win32` 2×) — in keiner dieser Dateien wurde ein Typ
> aus dem Namensraum benutzt; ohne `EnableWindowsTargeting` fiel das nie auf.
>
> **Neu im Kern**: `BouncyCastle.Cryptography`, `ClosedXML`, `DocumentFormat.OpenXml`,
> `SixLabors.Fonts`, `Microsoft.ML.OnnxRuntime`, `Microsoft.ML.Tokenizers` und eine
> `ProjectReference` auf `KiKern` — alle plattformfrei. `Mscc.GenerativeAI` und
> `Microsoft.Extensions.Http/Logging` wurden **nicht** gebraucht: Die Namen stehen im Bestand
> nur in Kommentaren von `KiChatService`, und der bleibt in der Anwendung.
>
> **Die Schnittkante, die dabei sichtbar wurde:** Was der KI-Assistent **weiß**, ist Kern; was
> er **bedient**, hängt an lebenden WinForms-Controls und bleibt bei der Oberfläche, bis iU8 die
> Masken ablöst. Dasselbe Muster bei den Controllern: Rechnen und Speichern gehen mit, Listen-
> und Kontextmenü-Bedienung bleibt.
>
> **Nachweis je Tranche:** Kern 0 Fehler (Warnungen 2 → 3, die dritte ist CS0108 aus
> `StromverbraucherStammCtrl` und mitgewandert), Lösung x64 0 Fehler / **34 Warnungen**
> unverändert, **886 Tests** grün, Referenzlauf 1030/1007/1017 GESAMT PASS mit leerem
> `diff -rq`, ChartProben 9 Bilder ohne Verstoß, `ZugriffsschichtProben` und
> `Referenzlauf` (x64) 0 Fehler, beide Wächter 0 Treffer.
>
> *(Die Commit-Bodys der fünf Tranchen nennen 36 Warnungen — sie sind im Worktree ohne
> iU8 gemessen worden. Auf `ios_migration` liegt der zweite Umzug hinter `92380ea`, das
> zwei WFO1000 mit `Form_Kosten_Auswahl` gelöscht hat; nachgemessen sind es dort **34**.)*
>
> **Offen nach dem zweiten Umzug:** die Windows-Bedienprobe (Berichte, Katalogimport,
> Lizenzaktivierung, KI-Chat) und die 59 Dateien, die erst mit `Views/` bzw. mit dem
> Access-Zweig gehen können.
>
> **Befund iU5‑O‑1 (Windows-CI 06.09.2026, Lauf 34018913888 auf `002c937`), behoben in `8256ba4`:** Acht Testklassen
> tauschten die prozessweiten `Dienste.*` in drei nebeneinander laufenden xunit-Sammlungen; xunit trennt nur INNERHALB
> einer Sammlung. Während der Tausch von `DiensteTests` stand, meldete ein Datenbanktest über
> `DataRepository.FehlerMelden` in denselben `Dienste.Dialog` — die Mitschrift bekam „Fehler beim Laden der Daten:
> Type…" statt „mit Titel", der nächste Lauf war grün. Jetzt tragen **alle** Tauscher `[Collection("Testdatenbank")]`
> (die eine serielle Sammlung, ohne `ICollectionFixture` also kostenlos), der Name „Dienste" ist weg, und der Wächter
> `EPOS.Kern.Tests/DiensteSammlungTests` hält die Regel über die Quelldateien fest — samt Gegenproben und Hausregel in
> `EPOS.Kern/CLAUDE.md`. Der fremde Melder war ein **Testfehler**: `TestDatenbank.SpalteSicherstellen` fragte mit
> eigenem `PRAGMA table_info(…)`; dessen Spalte `dflt_value` meldet für NULL den Typnamen „BLOB", die Tabelle wurde als
> `Byte[]` gebaut und beim ersten Vorgabewert gesprengt — die Existenzprüfung fand nie eine Spalte, das `ADD COLUMN`
> lief immer, 1 440 stille Fehlmeldungen je Lauf. Die Auskunft holt jetzt der Kern (`DataRepository.SpalteVorhanden`).
> 1 348 statt 1 344 grün, fünf Läufe hintereinander stabil, 59 s statt 70 s; Rechenweg unberührt. Der dabei sichtbar
> gewordene Kern-Befund steht als **iU6‑O‑1** im iU6-Block.

## Abnahmestand iU5 (§ 4, Absatz „Abnahme“)

**Hier erreicht** (Linux, drei Projekte byte-gleich, Wächter 0 Treffer); die Bedienprobe auf
Windows und der Vollreferenzlauf 332/332 stehen aus.

## Statusblock iU6 — Stand 03.09.2026

> **Status 03.09.2026 — hier erreicht, Windows-Nachweis offen.** Sechs Commits
> (`22fb7eb` iU6-T1 … `2387abf` iU6-T5) auf `origin/ios_migration` = `18f515f`.
>
> **Das Ergebnis in einem Satz: `EPOS.Kern` nennt `System.Data.OleDb` nicht mehr — weder
> im Quelltext noch als `PackageReference` —, und `CA1416` ist von 87 auf 0 gefallen.**
> Der Datenzugriff liegt hinter `IDatenzugriff`; `DataRepository` bleibt die Fassade
> davor und ist für die rund 160 Aufruferdateien unverändert.
>
> | Tranche | Commit | Inhalt | CA1416 |
> |---|---|---|---|
> | iU6-T1 | `22fb7eb` | `RecordSet.DBCommand` **ersatzlos gestrichen** (iR8) | 87 → 78 |
> | iU6-T2 | `582844c` | toter OleDb-Code in `SolarkollektorenCtrl`, `PufferSpCtrl`; Access-Zweig aus `ApplikationCtrl` in die Anwendung | 78 → **0** |
> | iU6-T3a | `35de91d` | Masken-Sweep: `OleDbParameter` → `DbParam` in 46 Views | 0 |
> | iU6-T3b | `fe28cb2` | Brücke aus dem Kern; `System.Data.OleDb` aus `EPOS.Kern.csproj` | 0 |
> | iU6-T4 | `7780df6` | `IDatenzugriff` + `SqliteDatenzugriff`; `DataRepository` wird Fassade | 0 |
> | iU6-T5 | `2387abf` | `bundle_green` für iOS vorbereitet (greift erst mit Multi-Targeting) | 0 |
>
> **iR8 war eine Streichung, kein Umbau.** Die Vermessung fand repositoryweit **null**
> Zugriffe auf `RecordSet.DBCommand` außerhalb von `RecordSet.cs`. Das Kommando wurde
> seit iU3 nur noch lazy im Getter angelegt und blieb damit immer `null`; `MerkeSql()`
> schrieb in ein Objekt, das es nie gab, `Parameter()` lieferte ausnahmslos `null`. Ein
> Ersatztyp wäre eine Fassade für null Nutzer gewesen — und hätte den falschen Eindruck
> erweckt, `RecordSet` trage Parameter. Es gibt deshalb **keinen `DbBefehl`**.
>
> **Dasselbe Bild in zwei Controllern.** `SolarkollektorenCtrl.Update()`,
> `PufferSpCtrl.Delete(string)` und `PufferSpCtrl.Update()` füllten ein `DBCommand`, das
> nie eine Verbindung bekam, und riefen darauf `ExecuteNonQuery()` — auf Windows also
> eine `InvalidOperationException` im `catch` und ein stilles `return false`. Alle drei
> hatten **0 Aufrufer** (erschöpfende Instanzlisten in den Commit-Bodys); geschrieben
> wird über die OleDb-freien `*StammCtrl`. Zusammen waren das 71 der 87 Warnungen.
>
> **`EPOS.Daten` entsteht nicht.** Die Planung sah dafür ein eigenes Projekt vor. Der
> Vertrag ist ein Interface und eine Klasse — ein drittes Projekt hätte den Kern von
> seiner eigenen Zugriffsschicht getrennt, ohne dass ein zweiter Anbieter in Sicht wäre
> (§ 1.5, Präzisierung zu iL2: es gibt **einen** Dialekt). `IDatenzugriff` und
> `SqliteDatenzugriff` liegen daher in `EPOS.Kern/Allgemein/`.
>
> **Der Masken-Sweep kam vor die Streichung.** Umgekehrt wäre der Zwischenstand nicht
> übersetzbar gewesen: Die Views hängen an genau dem impliziten Operator und an
> `DbParam.Von()`, die T3b entfernt. Das Skript hat 434 `OleDbParameter`-Vorkommen
> ersetzt, 54 `DbParam.Von(…)`-Klammern aufgelöst, 39 `OleDbType` auf `DbParamTyp`
> gehoben, 36 Objektinitialisierer `{ Value = }` auf `{ Wert = }` gezogen und 38
> `using System.Data.OleDb;` entfernt — 46 Dateien, keine von Hand.
>
> **Was Windows-seitig offen ist.** Der Referenzlauf deckt den Rechenpfad ab, nicht die
> Bedienung. Offen sind deshalb: die **Erststart-Migration aus einem `.accdb`-Bestand**
> (einziger verbliebener Nutzer der Brücke — `SchemaMigration` und `GeraeteWaisen` binden
> über `DbParamOleDb.Nach`, den Schemamarker liest und schreibt `SchemaVersionAccess`);
> die **Solar- und Pufferspeicher-Dialoge**, die sich „unverändert" verhalten müssen; die
> **36 Views mit `RecordSet`** (FormMain 13, Form_Start 10, Form_PV 6, Form_Gebäude 6,
> Form_WP 4, dazu `Form_DBBHKW.cs:436/450` mit den `DbVorgang`-Überladungen); und die
> Sweep-Dateien mit den meisten Stellen — `Form_Kosten.cs` (83), `ucFuelSettings.cs`
> (80), `Form_BHKWEing.cs` (50), `Form_Heizkessel.cs` (46),
> `Form_Heizkessel_einlesen.cs` (20). Die Liste ist je Commit abhakbar in
> [`Umsetzung_iU0_iU1_Nachweise.md`](../../Umsetzung_iU0_iU1_Nachweise.md).
>
> **Nachweis hier:** `dotnet build WP-Plan.sln -c Release -p:Platform=x64` → 0 Fehler,
> **36 Warnungen** (vorher 123; die 87 CA1416 sind weg). `EPOS.Kern` allein: 0 Fehler,
> **2 Warnungen** (CA2255, CS0108 — beide aus dem Bestand). `dotnet test
> WP-Plan.Kern.slnf` → **805** (796 + 9 neue). Referenzlauf **1030, 1007, 1017** gegen
> `2026-08-30_B3-Kaskade`: **GESAMT PASS (815.043 Werte), alle drei byte-gleich** — nach
> **jeder** Tranche. `Proben/ZugriffsschichtProben` übersetzt fehlerfrei.
> `dotnet list EPOS.Kern package | grep -c OleDb` → **0**.
>
> **Anwenderentscheid iU6‑O‑1 vom 06.09.2026 (Empfehlung), umgesetzt in `b80e5aa`:** `SqliteDatenzugriff.LadeTabelle`
> legt den Spaltentyp nicht mehr fest, wenn die Deklaration auf `Byte[]` führt **und** die erste Zeile `IsDBNull` ist —
> die Spalte entsteht dann als `object`. Der Befund kam aus iU5‑O‑1: Für eine Spalte ohne deklarierten Typ (PRAGMA-Ergebnis,
> Ausdruck) meldet `Microsoft.Data.Sqlite` bei NULL in der ersten Zeile „BLOB", die Tabelle wurde `Byte[]` und der Ladevorgang
> starb an der ersten belegten Zeile; 72 der 118 Tabellen der Testdatenbank tragen das Muster in `pragma table_info`. Die
> Probe (Microsoft.Data.Sqlite 10.0.11) zeigt, warum das auch für echte BLOB-Spalten gilt: Deklaration und Speicherklasse
> melden bei NULL beide „BLOB"/`Byte[]` und sind nicht trennbar, `GetValue` liefert BLOB-Werte aber auch aus der
> `object`-Spalte unverändert als `Byte[]`. Belegt: fünf neue Fälle in `EPOS.Kern.Tests/SpaltentypTests` (ohne die Änderung
> fallen genau die drei zweideutigen), `EPOS.Kern.Tests` 1 484 grün, SQL-Prüfer 0 Fundstellen, Referenzlauf 1030/1007/1017
> **byte-gleich**, Gate grün. **iU6‑O‑1 ist damit geschlossen.**

## Abnahmestand iU6 (§ 4, Absatz „Abnahme“)

**Hier erreicht** (CA1416 87 → 0, `dotnet list EPOS.Kern package | grep -c OleDb` = 0, drei Projekte
byte-gleich); die Windows-Punkte stehen im Statusblock.

## Statusblock iU7 — Stand 03.09.2026

> **Status 03.09.2026 — Renderer und Ausgabe sind im Kern.** Fünf
> weitere Commits (`6604c05` iU7-5 … `0af6421` iU7-9) auf der Basis `300a354`, aufbauend auf
> iU7-1…iU7-4 (`c6b32eb`…`f84932b`). Die Ausgabe selbst ist im zweiten Umzug gewandert
> (iU5-U3, `82807f4`); iU7-9 hat den letzten Rest — die Systemdialoge der Berichtsansicht —
> auf `Dienste.Datei` gelegt.
>
> | Tranche | Commit | Inhalt |
> |---|---|---|
> | iU7-5 | `6604c05` | **`ChartRenderer.cs` von `WindowsFormsApplication1` nach `EPOS.Kern/Allgemein/Bericht/`** — verschoben, nicht verlinkt. `SkiaSharp` im `EPOS.Kern.csproj`, die nativen Bibliotheken **bedingt über `IsOSPlatform`** (Linux, macOS, Win32). Namespace bleibt `WindowsFormsApplication1`, alle Aufrufer der Anwendung und `Referenzlauf/Bildvergleich.cs` übersetzen unverändert |
> | iU7-6 | `6737dd4` | **`Proben/ChartProben` hängt am Kern** statt an Ersatzklassen: `ProjectReference` auf `EPOS.Kern` statt `Compile Include`; `ZeitreihenSatzStub.cs` und `BerichtTexteStub.cs` gelöscht. Die Probe misst jetzt die echten `ZeitreihenSatz`/`VerlaufSerie`/`BerichtTexte` |
> | iU7-7 | `dc97916` | **ChartProben in `kern.yml`** — nach dem Test-Schritt, auf ubuntu **und** macos; die neun PNG als Artefakt `chartproben-<os>` (14 Tage) |
> | iU7-8 | `0759b37` | **Drei Renderer-Tests in `EPOS.Kern.Tests`**: `TagesMittel`/`MonatsSummenMWh` exakt, `Kuchen` liefert PNG in 960×600, `BalkenHorizontal` zweimal byte-gleich. **869 → 872 Tests** |
> | iU7-9 | `0af6421` | **Berichtsausgabe über `Dienste.Datei`**: `Views/Bericht/UcBericht.cs` (Ordnerwahl, Speicherziel, zweimal Öffnen) und `Views/Varianten/Form_Variantentest.cs` (Speicherziel, Öffnen) rufen `OrdnerWaehlen`, `DateiSpeichern` und `MitSystemOeffnen` statt `FolderBrowserDialog`, `SaveFileDialog` und `Process.Start`. `WordBerichtGenerator.FindeVorlage()` brauchte nichts — sie sucht schon über `AppDomain.CurrentDomain.BaseDirectory`. Eine bewusste Abweichung: die Dialoge bekommen kein Besitzerfenster mehr, weil `IDateiDienst` keines kennt |
>
> **Der Renderer war die Eintrittskarte, der Rest kam mit iU5-U3 und iU7-9.** Im Kern liegt
> jetzt die **Zeichnung** *und* die **Ausgabe** — `WordBerichtGenerator`,
> `ExcelBerichtGenerator`, `Bausteine/`, `BerichtsKonfiguration`, `ZeitreihenExtraktor`,
> `IBerichtsBaustein` (verschoben im zweiten Umzug, siehe iU5-Statusblock). In der Anwendung
> geblieben ist nur `BerichtsDatenSammler`, weil er `EnergieMengen` aus `Views/Varianten/` ruft;
> `ChartRendererGdi` (Gegenpart des Windows-Bildvergleichs aus iU7-1) ist mit iF23 am
> 03.09.2026 gelöscht.
>
> **Damit steht die Vorlage für iF16.** Der Kern liefert PNG-Bytes, die Oberfläche zeigt
> sie an — genau der Weg, den `EPOS.UI/Standards/ChartBild` (iU8-4) schon annimmt. Ein
> Chart-Stack für Bericht *und* Bildschirm, ohne SkiaSharp-Komponente in der WebView
> (iR3).
>
> **Nachweis:** `dotnet build WP-Plan.sln -c Release -p:Platform=x64` → 0 Fehler,
> **36 Warnungen** (unverändert); `EPOS.Kern` allein 0 Fehler, **2 Warnungen** (nach
> iU5-U4 drei — die dritte ist mit `StromverbraucherStammCtrl` mitgewandert).
> `dotnet test WP-Plan.Kern.slnf -c Release` → **872** (869 + 3).
> `dotnet run --project Proben/ChartProben -c Release` → *9 Bilder geprueft, 0
> Verstoesse*; alle neun PNG **byte-gleich** zum Stand vor dem Umzug (Schrift auf dem
> Prüfsystem: Liberation Sans). Referenzlauf **1030, 1007, 1017** gegen
> `2026-08-30_B3-Kaskade`: **GESAMT PASS** (815 043 Werte), `diff -rq` meldet für alle
> drei Projekte **keinen** Unterschied.

## Abnahmestand iU7 (§ 4, Absatz „Abnahme“)

**Hier erreicht, soweit ohne Windows möglich** (ChartProben 9/9, drei Renderer-Tests,
`kern.yml` auf ubuntu und macos).

## Statusblock iU8 — Stand 03.09.2026

> **Status 03.09.2026 — iZ5 hier erreicht, Windows-Abnahme offen.** Drei Stränge, **neunzehn
> Commits**: Strang A (8) auf der Basis `18f515f`, Strang B (7) auf `c477523`, Strang C (4) auf
> `f5fb05c`. **Ein vollständiger Dialog von EPOS-Plan lebt in plattformfreiem Code**:
> `Form_Kosten` öffnet „Energieträger anlegen" als Razor-Komponente; die WinForms-Fassung ist
> gelöscht.
>
> **Strang A — `EPOS.UI`, die Bibliothek**
>
> | Tranche | Commit | Inhalt |
> |---|---|---|
> | iU8-1 | `8574911` | Paketgruppe „Blazor Hybrid (iU8)" in `Directory.Packages.props`: Components.Web/QuickGrid 10.0.11, WebView.WindowsForms 10.0.100, bunit 2.9.0, CodeAnalysis.CSharp 5.9.0 |
> | iU8-2 | `a1b4df6` | `EPOS.UI` als Razor-Klassenbibliothek, `net10.0`, `EnableWindowsTargeting=false` — derselbe Wächter wie im Kern |
> | iU8-3 | `bbb7d42` | Thema aus `KartenStil.cs` als CSS-Variablen; die sieben Bausteine (Gruppenkopf, Warnbanner, SpeichernLeiste, InfoKnopf, Kachel, Herleitungs- und Kohärenzzeile) |
> | iU8-4 | `f690466` | Standardfelder: Zahl, Ganzzahl, Text, Auswahl, Datum, Schalter, Raster (QuickGrid), ChartBild |
> | iU8-5 / 5b / 5c | `cace2db`, `45a21dc`, `f5fb05c` | `EPOS.UI.Tests` mit bunit; Aufnahme in `WP-Plan.sln` und `WP-Plan.Kern.slnf`; UI-Kultur der Tests auf `de-DE` gepinnt |
> | iU8-8a | `8f5a28e` | `EnergietraegerVarianteDialog.razor` — der erste Dialog als Komponente, datenbankfrei |
>
> **Strang B — die Windows-Hülle und der Stichtag**
>
> | Tranche | Commit | Inhalt |
> |---|---|---|
> | iU8-6 | `4369fdb` | **`WindowsFormsApplication1.csproj` auf `Microsoft.NET.Sdk.Razor`**, Projektreferenz auf `EPOS.UI`, `wwwroot/index.html`, `Allgemein/Blazor/BlazorDialogForm.cs` + `BlazorDienste.cs` |
> | iU8-7 | `b12e910` | Hilfe-Brücke: `HelpExtender.ZielFuer(schluessel)` und `Allgemein/Hilfe/WindowsHilfeDienst.cs` |
> | iU8-8b | `1e2a44c` | Sieben Ressourcenschlüssel (`KAUSW_*`, `ALLG_BTN_*`) und `EPOS.Kern/Controller/EnergietraegerVarianteCtrl.cs` |
> | iU8-9 | `92380ea` | **iZ5** — `Form_Kosten` öffnet die Komponente; `Form_Kosten_Auswahl.cs/.Designer.cs/.resx` **gelöscht** (M1) |
> | iU8-10 | `eafbc1f` | WebView2 als zweite Setup-Voraussetzung (`.iss`, `build-setup.ps1`, Setup-Konzept 5.5) |
>
> Dazu in Strang B `eff82aa` (iU8-13, Doku und Windows-Nachweisliste) und `e3d1e5b` (iU8-10b,
> `.gitignore` für den WebView2-Bootstrapper in der Repowurzel).
>
> **Strang C — der Formular-Generator** (`479fcf9` iU8-12a … `0af7ca7` iU8-12d, dazu
> `4aa6b15` iU8-12e): `Werkzeuge/Formularkarte`, Roslyn-Leser, `resx`-Leser mit
> Label-Zeilenregel, Razor-Skelett, Stapellauf über alle Designer-Dateien; mit iU8-12e das
> **Prüfmuster** statt der lebenden Maske.
>
> **Der Razor-SDK ist keine Kosmetik, sondern die einzige Möglichkeit.** Die Gegenprobe mit
> dem einfachen `Microsoft.NET.Sdk` übersetzt fehlerfrei, liefert im
> Veröffentlichungsordner aber **kein `wwwroot`** — weder `index.html` noch `_content` noch
> `_framework/blazor.webview.js`. Der Dialog bliebe beim Anwender leer. Die Umstellung
> kostet **keine** neue Warnung (Codes vor und nach identisch).
>
> **Drei Korrekturen an diesem Konzept**, gemessen statt geschätzt:
>
> | Stelle | Bisher | Befund 03.09.2026 |
> |---|---|---|
> | Zahl der Designer-Dateien (iU8, Formular-Generator) | „**118**" bzw. 79/74/21 aus der Vorvermessung | **123 Dateien, 120 Masken, 63 davon lokalisiert** — die Vorvermessung suchte nur `*.Designer.cs`; der Bestand schreibt auch `*.designer.cs` (`Form_BHKWEing.designer.cs`). Nach dem Löschen von `Form_Kosten_Auswahl` sind es 122/119 |
> | Name der Scoped-CSS-Datei | `EPOS.UI.styles.css` erwartet | **`EPOS_Plan.styles.css`** — das Bündel folgt dem **Host**-Assembly, nicht der Bibliothek, und liegt in `wwwroot\`, nicht neben der EXE |
> | „ClosedXML-Standardschrift für Nicht-Windows setzen" (iU7-Tabelle) | als offene Aufgabe geführt | **nicht nötig.** iU7-4 (`f84932b`) hat nachgemessen: ClosedXML 0.105.1 bringt Carlito eingebettet mit; eine erzwungene Systemschrift machte die Spaltenbreiten schlechter. Gesetzt wird nur noch, wenn eine Messprobe fehlschlägt |
>
> **Das Raster „Label x28 / Control x270" gibt es nicht.** `Point(28,` und `Point(270,`
> kommen in je einer Datei vor. Tragfähig ist die Zeilenregel: das nächste Label **links in
> derselben Zeile** (|Δy| ≤ 8 px) — sie trägt den Stapellauf über alle Masken (iU8-12b).
>
> **Nachweis (Linux, SDK 10.0.400):** `dotnet build WP-Plan.sln -c Release -p:Platform=x64
> --no-incremental` → 0 Fehler, **34 Warnungen** (vorher 36; die beiden entfallenen WFO1000
> gehören dem gelöschten Formular), **keine neuen Warnungscodes**.
> `dotnet test WP-Plan.Kern.slnf -c Release` → **886** (KiKern 450, SpeicherEngine 337,
> EPOS.UI 64, EPOS.Kern 35). Referenzlauf **1030, 1007, 1017** gegen
> `2026-08-30_B3-Kaskade`: **GESAMT PASS** (815 043 Werte), `diff -rq` nur `protokoll.txt`.
> `dotnet run --project Proben/ChartProben -c Release` grün. `dotnet publish -r win-x64
> --self-contained` enthält `wwwroot/index.html`, `wwwroot/EPOS_Plan.styles.css`,
> `wwwroot/_content/EPOS.UI/{epos-ui.css,help_icon.png}`,
> `wwwroot/_content/…QuickGrid/QuickGrid.razor.js`, `wwwroot/_framework/blazor.webview.js`,
> `EPOS.UI.dll`, `Microsoft.Web.WebView2.{Core,WinForms}.dll` und
> `runtimes/win-x64/native/WebView2Loader.dll`.
>
> **Was noch offen ist.**
>
> 1. **Die Windows-Abnahme von iZ5** — Maus *und* Finger (M2), deutsch *und* englisch,
>    Hochkontrast, 125 %/150 % DPI (greift die DPI-Insel?), Enter/Esc, Infoknopf,
>    WebView2-Profilordner, Setup in der Windows-Sandbox ohne WebView2, VS-2026-Designer
>    unter dem Razor-SDK. Die Punkte stehen einzeln in
>    [`Umsetzung_iU8_Nachweise.md`](../../Umsetzung_iU8_Nachweise.md).
> 2. ~~**Der Stapellauf des Generators liest den gelöschten Dialog.**~~ **Erledigt mit
>    iU8-12e (`4aa6b15`).** 22 der 100 Tests hingen an `Form_Kosten_Auswahl.Designer.cs` bzw.
>    an `new Form_Kosten_Auswahl` in `Form_Kosten.cs` und scheiterten seit iU8-9. Gelöst
>    wurde das nicht durch eine andere Probemaske allein, sondern durch die Trennung von
>    Werkzeugprüfung und Bestandsprüfung: Der letzte Stand der gelöschten Maske liegt
>    **eingefroren** unter `Formularkarte.Tests/Pruefmuster/Kosten/` (Designer, `.cs`, `.resx`
>    und der Aufrufer-Auszug aus `Form_Kosten.cs`, wortgleich aus `92380ea^`), wird **nie
>    übersetzt** und vom Stapellauf **übergangen** wie `bin` und `obj`; die `StapelTests`
>    hängen jetzt an der lebenden `Form_Kosten_VarAuswahl`. **101 Tests, alle grün.**
>    Nachgemessen nach iZ5: **122 Designer-Dateien, 119 Masken** im ganzen Repo, davon
>    **117 unter `Views/`**.
> 3. **WebView2-Verteilung online oder offline** — Bootstrapper (heute), Standalone-Installer
>    oder Fixed Version. Anwenderentscheidung, offen als S10 im Setup-Konzept.
>
> **iU9-1 (vorgezogen) — der Öffner des ersten Dialogs war unerreichbar.** Die Windows-Abnahme
> vom 03.09.2026 hat gezeigt, dass iU8-9 die falsche Maske umgestellt hat: `Form_Kosten` ist seit
> **KD6a kein Einstieg mehr** (`UcBkKosten.btnVerwaltung_Click` öffnet `Form_KostenKomponente`,
> `Form_Start.cs:2175` entfernt `btn_Kosten` per `EntferneAltknopf`). Der erste Blazor-Dialog war
> damit in der Oberfläche nicht zu erreichen — nicht falsch gebaut, nur an der toten Maske
> angeschlossen. Dieselbe Funktion lebte in der zeichengleichen Schwester
> `Views/Kosten/Form_Kosten_VarAuswahl` mit zwei erreichbaren Aufrufern:
> `Form_Heizkessel.CreateNewEnergyCarrier` (Knopf „◀", `btn_Kessel_Hinzu`) und dem Gegenstück in
> `Form_BHKWEing` (`btn_Hinzu`). Beide öffnen seit **iU9-1** dieselbe Razor-Komponente über
> `BlazorDialogForm`; die Schwester ist gelöscht (M1), damit gibt es die drei Abfragen des
> Dialogs nur noch einmal — in `EnergietraegerVarianteCtrl`. `Form_Kosten.CreateNewEnergyCarrier`
> bleibt unverändert stehen (die Maske ist tot, aber nicht gelöscht — das entscheidet der
> Anwender) und trägt den Befund als Kommentar. **Nachweis:** Build 0 Fehler / **30** Warnungen
> (34 minus die vier WFO1000 der gelöschten Maske), **928** Tests, Formularkarte **101/101**,
> Referenzlauf 1030/1007/1017 **GESAMT PASS**. **Die Lehre** steht im Entscheidungsregister
> § 2.8: Die Wahl der ersten Maske muss die **Erreichbarkeit des Öffners** prüfen, nicht nur
> Größe und Feldzahl.

## Abnahmestand iU8 (§ 4, Absatz „Abnahme (iZ5)“)

**Hier erreicht, soweit ohne Windows möglich** — die WinForms-Fassung ist gelöscht, 64 bunit-Tests
grün, der Veröffentlichungsordner trägt `wwwroot` vollständig. Die Abnahme mit Maus und Finger
steht aus und ist die eigentliche Aufgabe von
[`Umsetzung_iU8_Nachweise.md`](../../Umsetzung_iU8_Nachweise.md).

## Statusblock iU9 — Teilwelle 16c umgesetzt, Welle 16 und Meilenstein M9 abgeschlossen (04.09.2026, Basis c8fbd77 nach W16b, zusammengeführt mit 97b048c nach dem zweiundzwanzigsten iOS-Lauf)

> **Statusblock iU9 — Teilwelle 16c umgesetzt, Welle 16 und Meilenstein M9 abgeschlossen (04.09.2026, Basis `c8fbd77` nach W16b, zusammengeführt mit `97b048c` nach dem zweiundzwanzigsten iOS-Lauf)**
>
> **Das Hauptfenster ist Razor, und `WindowsFormsApplication1` führt keine Fachmaske mehr — die Mischphase (M9) ist zu
> Ende.** `MDIMainForm.cs` 873 → **129** Zeilen (die Hülle: `Form` + `Application.Run`, eine `BlazorWebView` mit
> `Hauptfenster.razor`, Besitzer der `BlazorDialogForm<T>`, F1, `Application.Restart()` beim Sprachwechsel,
> `LizenzManager.NachpruefungImHintergrund()`), `MDIMainForm.Designer.cs` (493 Z., 45 `ToolStripMenuItem`) und die drei
> `.resx` (4 000 Z.) **vor dem Rückbau** als Prüfmuster `Pruefmuster/Hauptformular/` eingefroren (E‑9), die acht `Init*`
> und 34 `MenuItem_*`-Handler gefallen, `MenueCtrl` 347 → 257 Z. (26 → 6 Methoden), `WinFormsNavigation` 269 → 256,
> `BlazorDialogForm` 386 → 301 (die `DpiInsel` samt zwei `ShowDialog`-Überladungen weg). Neu in `EPOS.UI`: Baustein
> **`Menueband`** (`Menuepunkt`, **`Menuetabelle` per Skript aus dem Designer erzeugt, nicht abgetippt** — R‑W16‑8; **54**
> Punkte und 8 Trenner, nicht 45: die acht `Init*` hängten neun Punkte programmatisch ein, B2; vier `.resx`-Leichen und
> sieben fehlende englische Beschriftungen bereinigt, B3/B4; die toten Handler `MenuItem_PV_Import_PAN`/`MenuItem_PV_Import`
> gefallen, W16‑B24; der KI-Assistent eine Tabellenzeile statt einer Suche über den Anzeigetext, W16‑B23), Seite
> **`Hauptfenster`** (Menüband + Kopfband PRODUKTNAME/GATTUNG/CLAIM/Version + Inhaltsfläche, `Springe(schluessel)` als
> einziger Handler) hinter **`HauptfensterHuelle`**, **`Seitenschluessel` als die eine Schlüsseltabelle beider Plattformen**
> (K7, E‑1/E‑2: 34 Werte, die übernommenen `Masken`-Werte sind Verweise, `INavigation.OeffneMaske` bleibt; `Masken.PvImport`
> fehlte seit W13 im ASCII-Zeugen, B1) und **`AppWurzel` als gemeinsame Wurzel** (eine Wurzel, zwei Schalen: `Kopfleiste` als
> `RenderFragment` trägt unter Windows das Menüband, auf iOS nichts; `Startansicht` ist die Ansicht beim Aufmachen und das
> Ziel des Rückwegs). **E‑6/iF21 eingelöst:** `app.manifest` Per Monitor V2, `Program.Main` `HighDpiMode.PerMonitorV2` — der
> Gerätebefund bei 100/125/150 % steht aus (`Umsetzung_iU9_Nachweise.md` § 12.1). Fensterhilfe im Kopfband
> (`Hauptfenster.btn_Help`, W16b‑O‑4), `HilfeKontext`, `help_mapping.txt`, vier `CLAUDE.md` und
> `Konzept_iOS-Portierung_EPOS-Plan.md` (M9 ✔) nachgezogen. Sieben Sachcommits, Protokoll, Merge und Gate-Nachtrag
> (`915e0a7` … `54b7c96`), auf `ios_migration` als `ab3aea8`; **`WindowsFormsApplication1` führt noch EINE Designer-Maske**
> (`Form_HelpPopup`, bis iU11, W15b‑E‑2), die Hülle `MDIMainForm` ohne Designer, **null Inline-SQL** und die `Sprungbruecke`
> mit einem Zweig (`Form_SpeicherOptimierung`, iF22). `EPOS.iOS` ist unberührt — die drei neuen Schnittstellenglieder
> (`StartseiteGaben`, `BerichteKostenGaben`, `AdresseOeffnen`) haben Standardumsetzungen, die `AppWurzel` sagt es im Banner.
>
> **Neun Angleichungen** (A‑1…A‑9: das Menü klappt beim Klick auf, nicht beim Überfahren; `&&` → `&` in vier Menütexten;
> die nie lesbare Ladeanzeige `label_OnlineDoku` entfällt; der Titel ist von Anfang an „EPOS-Plan", W16‑B22; „Über" über
> `Dienste.Dialog`; Browserstart über `Dienste.Datei.AdresseOeffnen` statt `Process.Start`, B8; die 21 einzeiligen
> `MenueCtrl`-Methoden entfallen; die Fensterhilfe sitzt im Kopfband; sieben stille `Console.WriteLine` entfallen) und die
> Befunde W16c‑B1…B10, darunter: **die N1-Sollwerte „0/0" gehen nicht auf** — sie sind vom Stand vor W15b gerechnet,
> geprüft wird die starke Form „genau eine Maske, und zwar `Form_HelpPopup`" (B7); `Program.cs` brauchte keine
> Bereinigung (B9); `AppWurzel.ZurueckZurListe` räumte `_simErgebnis` nicht ab (B10, behoben). **R‑W16‑10 eingelöst**
> (`Form_HelpPopup` meldet „ja", `MDIMainForm` bleibt als Klasse Wurzel des Graphen, ist aber keine Maske mehr),
> **W16b‑O‑1 erledigt** (der Maskenschlüssel-Zeuge ist über einen Sprungtabellen-Auszug im Prüfmuster zurück).
> **Anwenderentscheide 04.09.2026:** E‑1, E‑2, E‑6, E‑8a, E‑9 bestätigt; W16c‑E‑1 (das Menü klappt beim Klick auf)
> bestätigt; **W16c‑E‑2: Untermenü „Sprache"** und **W16c‑E‑3: Ansichtswechsel** umgesetzt (`a9797d1`: die zwei Sprachpunkte
> sind Untereinträge des Kopfes „Sprache", N4 jetzt 55 Punkte / 8 Trenner / 4 Köpfe, W16c‑O‑3 erledigt; „Varianten und
> Bericht…" wechselt die Ansicht der `AppWurzel` auf `BERICHTE_KOSTEN` wie auf iOS, Windows liefert die
> `BerichteKostenGaben` aus derselben Hülle wie das sechste Reiterblatt, Rückweg über `ZurueckZurListe`; dabei Befund
> W16c‑B11: `IProjektQuelle` fehlte im Windows-Dienstverzeichnis, `KeineProjekte` eingetragen — Abnahmepunkt W16c‑O‑6;
> Gate auf dem gemergten Stand: 0 Fehler / 6 Warnungen, **4 012** Tests auch unter `en_US`, Formularkarte 122, Referenzlauf
> byte-gleich). **E‑10 entschieden 04.09.2026: `MDIMainForm` → `Hauptfensterrahmen`** (umgesetzt `c7f989b`, W16c‑O‑1 erledigt;
> nicht `Hauptfenster`, das ist die Razor-Seite); **W16a‑E‑1/W16b‑O‑5 entschieden 04.09.2026: der Assistent wird in
> iU11 zusammen mit der Transaktion W16a‑O‑1 eine freie Ansicht der `AppWurzel`**, bis dahin modal; **W16b‑E‑1 und
> W16b‑E‑2 bestätigt 04.09.2026**; **iF30 entschieden 04.09.2026** (streng über die Schreibnaht im Kern, eigene Welle
> nach der Windows-Abnahme, siehe Register). **Was iU11 erbt:** `Form_HelpPopup` (fällt mit
> `HelpCatalog`/`HelpExtender`, Ersatz `IHilfeDienst` steht), die `Sprungbruecke` mit einem Zweig, E‑10, W16b‑O‑3 erledigt
> (`bd0592a`, eine Wahrheit im Kern), die drei iOS-Standardumsetzungen, die DPI-Abnahme (W16c‑O‑2),
> `Seitenschluessel` mit 34 Werten in einer Klasse (W16c‑O‑4, Teilung entlang Ansicht/Maske/Weg), keine Menüfreischaltung
> nach Projektzustand wie im Bestand (W16c‑O‑5); die `WFO1000`-Herabstufung kann mit `Form_HelpPopup` entfallen.
>
> **Nachweise** (auf dem gemergten Stand `ab3aea8`, Linux): Build → 0 Fehler, **6** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **4 002** grün (3 968 nach W16b; N4 `MenuebandTests` 15 Fälle — 54 Punkte, 8 Trenner,
> 5 Köpfe, jeder Klick ein bekannter `Seitenschluessel`, jede Beschriftung de und en aus `MyResource`, 11 Bilder; dazu
> `HauptfensterTests`, `AppWurzelTests`, `SeitenschluesselTests`), **identisch unter `LC_ALL=en_US.UTF-8`** · Formularkarte
> **122** grün (+1: der zurückgeholte Maskenschlüssel-Zeuge; **N1** `Masken == 1` = `Form_HelpPopup` mit Namen,
> `Erreichbar(Ja) == 1`; **N2** das Prüfmuster liefert weiter Karten, Skelette und Erreichbarkeit) · Stapellauf **1** Maske /
> 2 Designer (**Sollwert exakt getroffen**), 0 lokalisiert, **1 erreichbar / 0 nein / 0 verwaist / 0 unklar** · SQL-Prüfer
> 1 200 Texte, 0 Fundstellen · ChartProben 32 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS, byte-gleich**
> (815 043 Werte; der Startweg Erststart → Lizenz → `NachpruefungImHintergrund` ist inhaltlich unverändert) · beide
> Wächter leer.
>
> **Protokoll** mit der Feldkarte (eine Kartenzeile), der erzeugten Menütabelle (§ 4), den neun Angleichungen, den
> Befunden, den Zeugen der Formularkarte (§ 8), der Löschliste mit `git grep`-Nachweis und der **Vollabnahme N1–N10 in
> sechzehn Punkten**: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W16c_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme 04.09.2026, erster Befund W16c‑B12 (Startabsturz):** `BlazorSeite<T>` gibt jedem Parametersatz
> einen `SeitenZustand` unter dem Schlüssel `Zustand` mit; seit W16c ist die Wurzel `BlazorSeite<Hauptfenster>`, und
> `Hauptfenster.razor` führte den Parameter nicht — Blazor warf beim ersten Rendern, verpackt als
> `TargetInvocationException` bei `Application.Run`. Behoben in `1429712`: `Hauptfenster` und `AppWurzel` nehmen den
> Zustand (die Hüllen der Startseite und der Berichte behalten ihre eigenen), `BlazorSeite` prüft per Reflexion und
> meldet lesbar, `HauptfensterTests` rendern über `AddMultipleAttributes` wie die Hülle (Gegenprobe mit unbekanntem
> Schlüssel rot); die bunit-Tests sahen es nicht, weil sie mit getippten Parametern rendern (B12a, Parallele zu B11).
> Abnahmepunkt 0 „Start" bleibt bis zum Gerät offen (W16c‑O‑7). **Windows-Abnahme
> steht aus** — Erststart mit Lizenz, 54 Menüpunkte in drei Ebenen mit Tastatur und F1, Kopfband, 21 Kacheln, Assistent,
> Simulation, Bericht, Sprachwechsel, **DPI 100/125/150 %** mit `Form_HelpPopup` und `Form_SpeicherOptimierung` als
> Kandidaten für eine echte Abweichung, Monitorwechsel, Setup mit WebView2. Der vierundzwanzigste iOS-Lauf
> (33904433007) auf diesem Stand ist grün — erstmals mit `Hauptfenster`, `Menueband` und der `AppWurzel` als gemeinsamer
> Wurzel (N9; Lauf 23 war eine abgebrochene Dublette).
> **Windows-Abnahme 05.09.2026, W16c‑E‑4 („Sprache sollte oben rechts sein") umgesetzt (`4bcd981`):** der Kopf
> „Sprache" steht am **rechten Rand** des Menübands — `Menuepunkt.RechtsBuendig` aus der erzeugten `Menuetabelle`,
> das `Menueband` hängt `epos-menueband-punkt--rechts` (`margin-left: auto`) an. **Nur die Optik wandert**:
> Markup-Reihenfolge, Tastaturweg (Ende = „Sprache"), Sprachausgabe und N4 bleiben unverändert; fünf bunit-Fälle.
> Der Abnahmepunkt 3a „Wo ‚Sprache' steht" ist neu in der Liste, die Sichtprüfung bleibt beim Anwender.
> **Anwenderwunsch W16c‑E‑5 vom 05.09.2026 (Farbgebung wie vor W16), umgesetzt (`04d5ac6`):** Menüband und
> Kopfband folgen `menuToolbar` und `MDIMainForm.InitMarke` — AliceBlue #f0f8ff, vier Köpfe in 16 px, kühle
> Trennlinie #dee3e8, Produktname 19 px, Gattung und Claim 11 px in #70777e. Fünf Werte, die bisher nur als
> Rückfall in der Regel standen (`--epos-marke`, `--epos-marke-untertitel`, `--epos-marke-trennlinie`,
> `--epos-menue-flaeche`, `--epos-flaeche-hell`), sind jetzt Token in `:root`; die Menühöhe bleibt beim
> Berührungsziel 44 px, die Versionsfarbe des Bestands ist wegen 2,77:1 nicht übernommen. Abnahmepunkt 7a.
> **Windows-Abnahme 05.09.2026 (PDF des Anwenders, S. 3), Befund W16c‑B13 (`78f32a7`):** die verschachtelten
> Untermenüs des Menübands („Administration → Wärmebedarf & Heizung ▸ …") ließen sich nicht aufklappen. Drei
> Ursachen: `@onfocusout` am `<nav>` schloss die Klappe schon beim Zeigerdruck, weil `focusout` auch bei einem
> Fokuswechsel **im Band selbst** feuert und die gedrückte Zeile beim Loslassen aus dem DOM war (kein `click` mehr;
> `FocusEventArgs` kennt kein `relatedTarget`, auf dem iPad setzt eine Berührung gar keinen Fokus); die zweite
> Ebene führte keinen eigenen Offen-Zustand (ein Feld für oben, ein flaches Namens-Set darunter, kein Ausschluss
> unter Geschwistern); und die Tastatur kannte weder → noch ← noch ein Wandern in der offenen Klappe. Behoben: der
> Offen-Zustand ist ein **Pfad über alle drei Ebenen**, eine Schließfläche (`position: fixed; inset: 0`) ersetzt
> `focusout`, `→ ← ↑ ↓` wandern in der Klappe mit rovendem `tabindex`, `OpenRegion` je Kind und Verweisfang je
> Zeile (ein bedingter `AddElementReferenceCapture` brach Blazors Abgleich). 14 hüllengleiche Wachen mit der
> echten `Menuetabelle`, darunter die Gegenprobe zur Ursache; Abnahmepunkte 4a (Maus/Berührung) und 5a
> (Tastatur); drei Hausregeln in `EPOS.UI/CLAUDE.md`.
>
> **Anwenderwunsch W16c‑E‑6 vom 06.09.2026 („Administration: Verschiebe BHKW von Energiesystem in ‚Wärmebedarf &
> Heizung' …"), umgesetzt in `14bc16c`:** BHKW und Solarkollektoren wandern nach „Wärmebedarf & Heizung", Pufferspeicher
> nach „Energiesysteme", und die drei Zeitreihen stehen in der neuen Unterrubrik „Profile & Lastgänge"
> (`MENU_PROFILE_LASTGAENGE`, en „Profiles & load curves") — Wärmebedarf Lastgang, Prozesswärme, Solarthermieganglinie.
> Die zwei Untermenüs mit dem einzigen Punkt „Bearbeiten" sind aufgelöst: `MenuItem_PV` und
> `MenuItem_Solarkollektoren` tragen jetzt selbst das Ziel ihres früheren Kindes. 55 → 54 Punkte, 13 → 12
> aufklappende, 42 handelnde unverändert — kein Ziel entfallen, keines hinzugekommen, die Menge der 28 Ziele unter
> „Administration" ist dieselbe. Geändert wurde die `Menuetabelle`, kein Zeichen im `Menueband`; der bisher einzige
> dreistufige Weg (PV ▸ Bearbeiten) ist durch „Profile & Lastgänge" ersetzt, der Wächter über W16c‑B13 bleibt. Neun
> neue bunit-Fälle, 2 704 in beiden Kulturen. Zur Kennung: `W16c‑E‑5` war seit dem 05.09.2026 die Farbgebung
> (`04d5ac6`), deshalb E‑6; die Abnahmepunkte im W16c-Protokoll heißen A‑W16c‑E‑5‑1 … ‑7.
>
> **W16c‑E‑7 (Anwenderentscheid 07.09.2026: „Mache zwei Untermenüs Photovoltaik 1. PV Module 2. Wechselrichter", dazu
> die Richtigstellung „der Import steht nicht unter Energiesysteme sondern vdi3805"), umgesetzt in `5a378d7`/`4ff7e32`,
> zusammengeführt in `28da824`:** Das Paar Modul/Wechselrichter steht an ZWEI Stellen des Kopfs „Administration" — als
> Katalog unter „Energiesysteme", als Import unter „Daten & Import" — und beide führen es jetzt unter einem eigenen Knoten
> **„Photovoltaik"** (`MenuItem_PV_Gruppe`, `MenuItem_PV_Import_Gruppe`, EIN Textschlüssel `MENU_PHOTOVOLTAIK`). Ziel,
> Argument und Kennung der vier Punkte sind unverändert; neu sind zwei Beschriftungen, weil der Knoten den Begriff schon
> trägt: „Photovoltaik Module" → **„PV Module"** (`MENU_PV_MODULE`), „Import Photovoltaik CEC/Pan" → **„PV Module (CEC,
> PAN)…"**, beides in beiden Sprachen. Die Regel aus W16c‑E‑6 („kein Untermenü mit nur einem Punkt") bleibt gewahrt: je
> zwei. **Die vierte Menüstufe musste nicht gebaut werden** — der Offen-Zustand des Bandes ist seit W16c‑B13 ein
> tiefenunabhängiger Pfad, `Untermenue` ruft sich mit `ebene + 1` selbst, `.epos-menueband-klappe--tief` gilt ab der dritten
> Ebene; `Menueband.razor` und das Stilblatt sind unverändert, nachgewiesen mit Maus, Tastatur (→ öffnet, ← schließt genau
> eine Ebene) und über die Hülle. Menüzahlen **58 Punkte / 44 handelnd / 14 aufklappend** (vorher 56/44/12); die vier
> `CLAUDE.md` standen noch auf 54/42 aus der Zeit vor W6‑E‑2 und sind mitgezogen. **Nebenbefund W16c‑O‑7:** „Klimadaten"
> ist das einzige verbliebene Ein-Punkt-Untermenü des Bestands — unangetastet, im Wächter namentlich festgehalten;
> Anwenderfrage, ob es nach der W16c‑E‑6-Regel aufgelöst wird. Nachweis: elf neue bunit-Fälle, Kern 1644 / UI 3020 grün,
> Designer „abweichend 0", SQL 0, Gate grün, Referenzlauf 1030/1007/1017/1045 byte-gleich. Abnahme auf Windows:
> A‑W16c‑E7‑1…10 (beide Knoten, vier Ziele, Tastaturweg, Schließfläche, englische Texte, 58 Punkte ohne Leerweg).
>
> **W16c‑O‑7 (Anwenderentscheid 07.09.2026: „ja"), umgesetzt in `4cfa018`, zusammengeführt in `e88cdab`:** Das letzte
> Ein-Punkt-Untermenü ist aufgelöst. Der Knoten „Klimadaten" führte als einziges Kind einen Punkt derselben Beschriftung —
> dieselbe Lage, die W16c‑E‑6 bei den zwei „Bearbeiten"-Untermenüs beseitigt hat. Der Punkt steht jetzt an der Stelle des
> Knotens unmittelbar im Kopf „Administration" und trägt dessen Bild `Menu4`; Name, `MENU_KLIMADATEN` und
> `Seitenschluessel.Klimadaten` unverändert, `MENU_KLIMA` bleibt ungelesen im Katalog. Zahlen 57 Punkte / 44 handelnd /
> 13 aufklappend (nach dem Stromspeicher-Menüpunkt aus W13‑E‑2 S1 im selben Merge: 58 / 45 / 13). Die Regel „kein
> Untermenü mit nur einem Punkt" gilt damit **ohne Ausnahme** — `MenuebandTests` hält die Liste der Einzelgänger leer
> statt einen namentlich zu führen; zwei neue Fälle belegen Lage, Bild und den eingesparten Klick am gezeichneten Band.
> Abnahme auf Windows: A‑W16c‑O7‑1…4 (Zeile mit Sinnbild zwischen „Energiesysteme" und „Daten & Import", Tastaturweg ohne
> →, F1-Hilfe, englisch „Climate data").

## Statusblock iU9 — Teilwelle 16b umgesetzt (04.09.2026, Basis 84d7c16 nach W16a, zusammengeführt mit d4a7632 nach dem einundzwanzigsten iOS-Lauf)

> **Statusblock iU9 — Teilwelle 16b umgesetzt (04.09.2026, Basis `84d7c16` nach W16a, zusammengeführt mit `d4a7632` nach dem einundzwanzigsten iOS-Lauf)**
>
> **Die Wurzel der Anwendung aus Anwendersicht ist Razor, und der Altzweig ist weg — 34 Dateien, 13 019 gelöschte gegen
> 5 549 neue Zeilen:** `Form_Start` (2 339 Z. `.cs` + 1 864 `.bak`, 1 381 Designer, 4 900 `.resx`) → Seite **`Startseite`**
> (`EPOS.UI/Seiten/Start/`: Kopfband mit Projektauswahl, Statuszeichen und Klimafeld, sechs `Reiter` mit 21 Kacheln in den
> fünf Reiterkomponenten `ProjektReiter`, `WaermebedarfReiter`, `StrombedarfReiter`, `ErzeugerReiter`, `SimulationReiter`,
> Reiter 6 = `BerichteKostenSeite` aus W5; Kachelzustand aus `KomponentenBestandCtrl`, Reitersperre und die drei
> `Form_Hinweis`-Aufrufe über `Warnbanner.Verfaellt`) hinter **`StartseiteHuelle`** (`BlazorSeite<Startseite>` im
> `MDIMainForm_Load`); `FormMain` mit `Form_StromTest`, `StromTestClass` und den zwölf `*KontextMenuCtrl` (E‑7, Altzweig
> K6‑a, 6 682 Z.) **ohne Nachfolge**; `AktionsKarte` → `Kachel`; `Form_Hinweis` → `Warnbanner.Verfaellt`;
> `FormStartProjektKontext` → **`ProjektKontextCtrl`** im Kern (K2, **Nachweis N7 zuerst**: der Wechsel zieht Id, Name und
> Klimazone zugleich nach, ein unbekannter Name lässt den Kontext stehen, `Uebernehmen` schreibt „zuletzt geöffnet",
> `Setzen` nicht), dazu **`StartseiteCtrl`** (K4, die vier SQL mit `DbParam`) und **`BedarfsZustand`** (E‑5: die zwei
> Bedarfsobjekte gehören dem Projekt, nicht mehr einem Fenster). **E‑5 umgesetzt:** die Simulationskonfiguration löst
> die Startseite in derselben WebView ab, das Ergebnis liegt als `Ueberlagerung` darüber, die zwei modalen Hüllen sind
> gefallen — **R‑W10b‑1 und R‑W11‑1 damit eingelöst** (in den Blöcken W10b/W11b nachgetragen).
> **Nachtrag 11.09.2026, Auftrag #207 (SIM‑Q1):** Die zwei Wirte der Startseite sind seither ihrerseits gefallen — die
> Simulation ist EINE freie Ansicht `SIMULATION` mit Ablaufleiste (① Konfiguration · ② Lauf · ③ Ergebnis), und
> die Startseite meldet nur noch den Weg dorthin. E‑5 bleibt als Entscheid gültig („kein zweites Fenster"); was sich
> ändert, ist der Wirt, nicht das Ziel. `Dienste.Projekt` läuft
> über `ProjektKontextCtrl`, `Program.startfrm` gibt es nicht mehr, `IProjektQuelle.Startkacheln(int)` (K6) mit
> Standardumsetzung. 78 neue Texte de/en, darunter erstmals englisch die drei Literale aus dem Code (B1).
> `Form_Start.Designer.cs` und drei `.resx` als Prüfmuster `Pruefmuster/Hauptformular/` eingefroren (E‑9), alle elf
> Typzeugen hängen am Prüfmuster. Sieben Sachcommits, Protokoll, Merge und Gate-Nachtrag (`b10cfc1` … `666fe4f`), auf
> `ios_migration` als `ff60252`; **`WindowsFormsApplication1` führt noch zwei Masken** (`MDIMainForm`, `Form_HelpPopup`) **und
> null Inline-SQL** (B34 eingelöst).
>
> **Zehn Angleichungen** (A‑1…A‑10: 13 `Paint`-Handler → CSS, drei Bindemuster → ein `@onclick` je Kachel,
> `UpdateWizardSymbole` → `KomponentenBestandCtrl`, der Hinweis der Reitersperre steht vorher statt nach dem Klickversuch,
> `Form_Hinweis` → Banner 3 s, fünf `MessageBox` des Klimaspeicherwegs → ein Banner, die Statusfarbe der Solar-Radiobuttons
> entfällt, `IProjektKontext.Vorhanden = true` wie auf iOS, „Öffnen…"/„zuletzt geöffnet" setzen das Projekt aktiv statt
> ein Detailformular zu zeigen, gerechnete Rechtsbündigkeit → CSS) und die Befunde W16b‑B1…B8, darunter:
> **`IosProjektKontext` liest die Klimazone anders** (Stammname statt Projektkopie) — der Kern übernimmt den Windows-Weg,
> **W16b‑O‑3 entschieden 04.09.2026 („iOS-Lösung"): die Messung zeigte, dass die iOS-Abfrage den falschen
> Schlüsselraum las — `ID_Klimaregion` ist die Id der Projektkopie, der Stammname war auf iOS immer leer; umgesetzt als
> EINE Wahrheit im Kern (`StartseiteCtrl.ProjektKlimazone` liest die Projektkopie, die Stammabfrage fällt),
> `IosProjektKontext` läuft über `ProjektKontextCtrl`, N7 15 Fälle, `bd0592a`** (B2); `ProjektTransferDialogTests` flatterhaft, nicht von dieser Welle (B7,
> W16b‑O‑2); die Stapellauf-Sollzahl „1/2" der Anweisung ist die von nach W16c, gemessen 2 Masken / 3 Designer (B8).
> **Anwenderfragen:** **E‑7 umgesetzt** — verloren gehen die Gewerksübersicht in Listenform und das Drag & Drop
> zwischen den zwölf Listen; an ihrer Stelle dieselben zwölf Gewerke als Kacheln mit Statuspunkt, jede führt in
> denselben Dialog wie das Kontextmenü; **nicht ersetzt** ist das Verschieben eines Katalogeintrags per Maus; **E‑5
> umgesetzt**; E‑1/E‑2 vorbereitet (K7 ist W16c); E‑9 umgesetzt; **W16a‑E‑1 bleibt offen** (der Assistent bleibt modal,
> an ihm hängt ein Schreibweg — technisch wäre die freie Ansicht jetzt möglich, W16b‑O‑5); **neu W16b‑E‑1** (der Reiter
> „Simulation" springt ohne Klimaregion sichtbar auf Reiter 1 zurück, die Meldung steht als Banner oben statt als
> `MessageBox` — bestätigt 04.09.2026) und **W16b‑E‑2** (der Reiter „Berichte & Kosten" wird von Anfang an gehalten, ein
> Ladevorgang mehr beim ersten Variantenwechsel — bestätigt 04.09.2026). **Was W16c erbt:** `MDIMainForm` nur an vier Stellen angefasst
> (`MDIMainForm_Load`, `MenuItem_Neu`/`_ProjektBearbeiten` → `projektkontext.Setzen`, `_AlsVariante` → `Dienste.Projekt`,
> `_VariantenBericht` → `StartseiteHuelle.Aktuelle`), Menü, `Init*`, Kopfband, F1 und Sprachwechsel unberührt; der
> „ja"-Zeuge steht an `MDIMainForm` und ist beim Rückbau umzuhängen, der Maskenschlüssel-Zeuge ist gestrichen (W16b‑O‑1,
> rückholbar über einen Sprungtabellen-Auszug im Prüfmuster); `Erreichbarkeit.Wurzelmasken` = nur `MDIMainForm`
> (W16‑B3 erledigt); `AppWurzel` unberührt, `Seitenschluessel.STARTSEITE` ist K7; `Form_Start.btn_Help` (die Fensterhilfe)
> wandert ans Hauptfenster (W16b‑O‑4).
>
> **Nachweise** (auf dem gemergten Stand `ff60252`, Linux): Build → 0 Fehler, **6** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **3 968** grün (3 923 nach W16a; N7 `ProjektKontextCtrlTests` 12 Fälle, N3
> `StartseiteTests` 21 Fälle — sechs Reiter, 21 Kacheln 5/4/3/7/2, Kachelzustand aus der Bitmaske, Reitersperre,
> Projektwechsel über `SeitenZustand`, die E‑5-Ansichten), **identisch unter `LC_ALL=en_US.UTF-8`** · Formularkarte
> **121** grün (T1 gestrichen) · Stapellauf **2** Masken / 3 Designer (B8), **2 erreichbar / 0 nein / 0 verwaist /
> 0 unklar** · SQL-Prüfer 1 200 Texte, 0 Fundstellen · ChartProben 32 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017
> **PASS, byte-gleich** (815 043 Werte) · **Referenzlauf mit Projektwechsel** (§ 16.3, R‑W16‑4): 1030→1007 und 1007→1030
> byte-gleich zur Basis und untereinander, dazu der Kern-Fall 1030→1007→1030 · beide Wächter leer.
>
> **Protokoll** mit dem Feldkartenabgleich der 108 Kartenzeilen, den zehn Angleichungen, den Befunden, der Löschliste mit
> `git grep`-Nachweis und **sechzehn Abnahmepunkten**:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W16b_Blazor_Port_Protokoll.md`. **Windows-Abnahme steht aus**,
> darunter alle 21 Kacheln, der Projektwechsel im Kopfband, die Konfiguration als Ansicht, das Ergebnis als Überlagerung
> und **DPI 100/125/150 %** (die Seite sitzt bis iF21/W16c in der DpiUnaware-`MDIMainForm`, R‑W16‑2 — die Abnahme hält
> fest, wie unscharf es ist). Der zweiundzwanzigste iOS-Lauf (33898599945) auf diesem Stand ist grün — erstmals mit der
> Razor-Startseite und `IProjektQuelle.Startkacheln` im gemeinsamen `EPOS.UI` (N9; iOS setzt die Startkacheln noch nicht
> um, der Zweig in der `AppWurzel` kommt mit K7 in W16c).
> **Windows-Abnahme 05.09.2026 (erstes Bildschirmfoto der gestylten Startseite — „Icons fehlen … Design ähnlich
> WinForms"), W16b‑E‑3 und W16b‑E‑4 umgesetzt (`4bcd981`):** die **21 Kachelbilder und Symbole** von `Form_Start`
> wandern per `git mv` unverändert nach `EPOS.UI/wwwroot/bilder/start` (Zuordnung Kachel → Datei in
> `Seiten/Start/Kachelbilder.cs`, der Ausschnitt 84 × 84 im Stilblatt über `object-fit`, aus `Properties/Resources`
> samt drei Leichen ausgetragen; `PHeizkessel.jpg` lag nur eingebettet in `Form_Start.resx`, Bytes nachgemessen;
> die Konfigurationskachel hatte im Vorläufer kein Bild und bekommt `PSchnellSim.jpg`, das einzige Kachelbild ohne eigene Kachel). Die
> **Gattungszeile** der Startseite steht nur noch **ohne Kopfleiste** (`Startseite.KopfbandZeigen`, von der
> `AppWurzel` nach dem Parametersatz gesetzt: Windows nennt die Gattung schon im Markenkopf, iOS behält die Zeile).
> Die **Anordnung** folgt `Form_Start.Designer.cs` ohne feste Pixelkoordinaten: Kopfband über den zwei Kästen, Klima
> links und Projekt rechts (Seh- und Tabreihenfolge), Statuszeichen vor der Beschriftung, Globus im Klimakasten,
> Schriftgrade des Markenbands, der Reiter und der Überschriften, **drei Kachelspalten** (Mindestbreite 404, das
> Raster läuft schmal nicht mehr über), Sinnbild links vom Titel, Erläuterung darunter, Zurück/Weiter 132 px fett.
> Nicht angeglichen: der Bildknopf „Speichern" bleibt beschriftet, der Infoknopf beim Haus-Token 28 px, keine
> feste Kachelhöhe. 18 neue bunit-Fälle; die Sichtprüfung (Abnahmepunkte 1, 2a, 2b) bleibt beim Anwender.
> **Anwenderwunsch W16b‑E‑5 vom 05.09.2026 („Design und Farbgebung … angelehnt an winforms Version vor‑W16"),
> umgesetzt (`04d5ac6`):** die Startseite trägt wieder die Anmutung von `Form_Start` (erhoben aus Designer,
> drei `.resx` und den zwei `*_Paint` des Standes `84d7c16`) — Reiterleiste auf eigenem Grund mit **gefüllter
> aktiver Zunge** in #005aa0 und weißer Schrift, weißes Reiterblatt mit kühlem Rahmen, die zwei Kopfkästen in
> #b4becd mit 8 px Rundung und 16‑px‑Beschriftungen, jede Erläuterung halbfett in DimGray, der
> Zusammenfassungskasten auf #f9fafc, die Knöpfe in LightGray. Sieben neue Token `--epos-start-*` gelten **nur**
> für `Seiten/Start`, damit der gemeinsame Farbsatz der Dialoge nicht kippt; drei Farben des Vorläufers sind
> wegen des Hauskontrasts 4,5:1 bewusst in derselben Familie ersetzt (weiß auf #6876df bei 16 px, 128,128,255 auf
> #f9fafc, die Versionsfarbe 150,156,162). Nur Stilblatt, kein Markup, keine feste Pixelkoordinate; Wache
> `StartseiteAnmutungTests` (30 Fälle: Token, tragende Regeln, Kontrast nach WCAG 2.1 aus den Stilblattwerten
> nachgerechnet); Abnahmepunkte 2c–2g für die Sichtprüfung. Nicht angeglichen: Statusanstrich der ganzen
> Kachel (A‑1), Kachelbeschriftung mittig (W16b‑E‑3), 29‑px‑Menühöhe und 65‑px‑Kopfkästen (Berührungsziel
> 44 px, M2/iL4), 1,5‑px‑Rahmen.
> **Windows-Abnahme 05.09.2026, Befund W16b‑B‑1 („die Dialoge auf der Startseite sind leer"), `f889e9e`:** Das
> modale Fenster zeigte die Hüllenfläche #F5F4EF, die zweite WebView2 zeichnete nichts. Als Ursache **ausgeschlossen**:
> Parametersatz (61 Hüllenstellen und 13 Assistentenseiten gegen die `[Parameter]` geprüft, 0 Treffer), Ausnahme
> beim ersten Zeichnen (alle 21 Kachelziele rendern ohne Gaben), Stilblatt (die Dialogwurzeln stehen vor Z. 1386 und
> waren nie abgeschaltet), Faden, WebView2-Laufzeit. Übrig bleibt der **Weg selbst**: Seit Startseite und Hauptfenster
> Razor sind (W16b.2/W16c.2), öffnet jeder Kachelklick und Menüpunkt ein modales Fenster mit einer ZWEITEN WebView2
> aus dem `WebMessageReceived`-Rückruf der ersten heraus — genau das, was `Sprungbruecke` seit W2.2 als Risiko R2
> ausschließt; die Belege vom 04.09. (W4‑B‑1, W5‑B‑1) hingen noch an WinForms-Klicks. `Blazorsprung` lässt das
> Ereignis zu Ende laufen und fährt den Sprung eine Nachricht später (`BeginInvoke`, Wiedereintrittssperre) — an den
> **zwei** Verteilern `StartseiteHuelle.Kachelweg` und `HauptfensterHuelle.Weg`, nicht in 21/55 Aufrufern; der
> synchrone Vertrag von `Weg` bleibt. `WebViewWache` hängt sich an `CoreWebView2InitializationCompleted` und
> `BlazorWebViewInitialized` (der WinForms-`BlazorWebView` 10.0.100 führt kein `UnhandledException`) und zeigt nach
> 10 s einen markierbaren Text statt der leeren Fläche; `Parametersatzwache` in beiden Hüllen. Dauerhafte Wachen auf
> Linux: `ParametersatzTests` (liest die Hüllenquellen, Reflexion über `EPOS.UI`), `StartkachelDialogeTests`
> (21 Ziele + Assistent mit Hüllensatz, Gegenprobe), `StartseiteTests` (+2). **Ursache wahrscheinlich, nicht
> bewiesen** — die Abnahme am Gerät (Protokoll W16b § 12.3, A1–A7) entscheidet; bleibt eine Fläche leer, steht nach
> 10 s der Grund darin. **Befund W16b‑B‑2 (Reiter gesperrt):** die Sperre ist vorbildgetreu (`Form_Start_Load`), und
> Projektname im Kopfband und gesperrte Reiter schließen einander aus (beides hängt an `ProjektId()`); Erklärung ist
> die Farbe (**W16b‑B‑2b**: frei #5f5e5a gegen gesperrt #888780, das Vorbild zeichnete frei schwarz) — behoben mit
> W16b‑E‑5 (`1a72cd5`), Wache in `StartseiteTests`. Offene Frage ans Gerät: stand das Banner „Bitte zuerst ein
> Projekt auswählen!" über der Leiste? **Antwort des Anwenders: ja** — Befund W16b‑B‑2 damit geschlossen (kein
> Projekt offen, Sperre und Banner vorbildgetreu).
> **Anwenderwunsch W16b‑E‑6 vom 05.09.2026 („ja, oder anderen Hinweis geben der elegant ist"), umgesetzt
> (`2981c1a`):** Das dauerhafte Warnbanner der Reitersperre ist gefallen. An seine Stelle treten eine **leise
> Einstiegszeile** im Reiter „Projekt" (mit dem ⚠ des Kopfbands, verschwindet mit dem offenen Projekt — Name und ✔
> stehen darüber im Kopfband, wie bei der Gattungszeile W16b‑E‑4), der Grund als **Tooltip** am nun **weich**
> gesperrten Reiterknopf (`Reiterblatt.Sperrgrund` → `aria-disabled` statt `disabled`, weil ein `disabled`-Knopf
> keine Zeigerereignisse annimmt und keinen Tooltip zeigt; neues Ereignis `Reiter.Verweigert`, die Pfeiltasten
> überspringen beide Bauarten) und das bisherige Banner **flüchtig für drei Sekunden nach dem Versuch** — auf eine
> gesperrte Zunge wie über „Weiter ▶", den Weg der Tastatur; das ist `tabControl_Wizard_Selecting` samt der
> Lebensdauer von `Form_Hinweis`, nur ohne Wegklicken. Sperre und Farbgebung (W16b‑E‑5) bleiben. Zwei Texte
> `START_EINSTIEG` und `START_SPERRE_TIPP` in beiden Sprachen, ortsneutral („oben"/„unten") für die Schale ohne
> Kopfleiste. Wachen `ReiterTests` +2, `StartseiteTests` +5; Abnahmepunkte 1/1a–1c/3/14/16 im Protokoll. Der
> Windows-CI-Lauf 128 auf `e65d3a9` fiel an **einem** Test: der Suchhelfer `Stilblock` in `StartseiteTests` las
> das Stilblatt ohne Zeilenenden-Angleichung, und auf dem Windows-Läufer liegt es nach `text=auto` mit CRLF —
> ein zweizeiliger Selektor traf nicht mehr. Angeglichen an `StartseiteAnmutungTests`/`StilblattTests`
> (`\r\n` → `\n`), Gegenprobe mit CRLF-Stilblatt grün; Kern-Lauf 133 war grün.
>
> **Anwenderwunsch W16b‑E‑7 vom 05.09.2026 („Kacheln sollten ähnlich wie zuvor angeordnet sein – sind jetzt zu
> groß"; Bildschirmfoto: zwei Kacheln je Zeile, jede rund die halbe Fensterbreite), umgesetzt in `436dfbc`:** Ursache
> war die **Mindest**breite 404 px an einem Raster mit `1fr`-Spalten — bei 150 % Skalierung misst das Reiterblatt
> eines Full-HD-Schirms rund 1 200 CSS‑px, die drei Kacheln brauchen 1 228; also blieben zwei Spalten, und `1fr`
> verteilte die ganze Breite auf sie, während Sinnbild (84 px) und Titel (16 px) blieben. `Kachelraster` führt
> deshalb eine **`Hoechstbreite`** (0 = dehnend wie bisher): drei feste Spalten von höchstens `--epos-kachel-max`,
> linksbündig, `gap: 6px 8px` (die Fugen des Designers aus `Form_Start.resx`: x = 18/422/834, y = 134/325),
> dazu `grid-auto-rows: minmax(185px, auto)` als Zeilenhöhe des Vorläufers (Kachel 404 × 185); auf schmalem Schirm
> zwei Spalten (< 1 150 px) und eine (< 720 px). Alle fünf Reiter mit ihren 21 Kacheln setzen `Hoechstbreite="404"`,
> die Kennzahlreihen der Kosten- und der Wirtschaftlichkeitsseite dehnen unverändert; Farben, Schriften und die
> sieben `--epos-start-*`-Token aus W16b‑E‑5 sind unberührt. Wachen: `StartseiteTests.Jeder_Reiter_stellt_seine_
> Kacheln_im_Vorbildmass` (alle fünf Reiter) und drei Fälle in `StartseiteAnmutungTests` am Stilblatt; der Fall
> `Das_Kachelraster_nimmt_die_Kachelbreite_des_Vorlaeufers` aus W16b‑E‑3 entfällt, weil er genau die Mindestbreite
> festschrieb, die den Befund verursacht hat. Ein erster Agentenlauf zu diesem Wunsch wurde um 16:45 UTC durch eine
> Unterbrechung abgebrochen; sein Teilstand (`FestesMass`, `auto-fill`, geschrumpfte Seitenränder) ist geprüft und
> verworfen — er hätte beim Anwender weiterhin zwei Spalten ergeben.
>
> **Nachtrag zu W13‑B‑1 (`4fd8cc7`):** § 12 ist um die **Fehlerschranke** ergänzt — die dritte Wache neben
> `Parametersatzwache` (falscher Schlüssel vor dem ersten Zeichnen) und `WebViewWache` (WebView2 kommt nicht hoch):
> `Fehlerschranke.razor` + `Wurzel<T>`, gemountet von `BlazorDialogForm`, `BlazorSeite` und `EPOS.iOS/HauptSeite`;
> der Parametersatz geht unverändert durch, die Wachen prüfen weiter gegen `T`. Regel (c) ist erweitert: aus „kein
> `ShowDialog` aus einem Blazor-Ereignis" wird **„kein modales Systemfenster im WebView-Rückruf"** — mit zwei
> Werkzeugen, `Blazorsprung` ohne Rückgabewert und `Blazornachlauf` mit.
>
> **W16b‑O‑2 erledigt (06.09.2026, `48a5547`):** Der flatterhafte Fall
> `ProjektTransferDialogTests.Schliessen_meldet_ob_ein_Import_gelungen_ist` (Windows-CI rot auf `cb8379e`) war kein
> Fehler des Dialogs, sondern zwei Wettläufe im Test: gewartet wurde auf das Kennzeichen der Attrappe, das im
> `Task.Run` des Dialogs schon beim Betreten des Imports fällt — geschlossen wurde also mitten im Lauf, und ein
> laufender Import meldet nichts; dazu kehrt bunits synchrones `Click()` zurück, ohne auf den Ereignisbehandler zu
> warten. Der Test wartet jetzt auf den gezeichneten Abschluss und fasst die Ergebnisprüfungen in `WaitForAssertion`;
> alle 28 Wartestellen der bunit-Tests sind durchgesehen, nur der Transferdialog hat einen eigenen `Task.Run`.
> Nachweis: altes Muster unter künstlicher Last 12 von 15 rot, neues 0 von 15; der Fall 30-mal grün in de und
> 30-mal in en, `EPOS.UI.Tests` 2 721 grün. Kein Produktcode geändert.

## Statusblock iU9 — Teilwelle 16a umgesetzt (04.09.2026, Basis 975ead5 = Tag vor-W16, zusammengeführt mit 3c7e0d6 nach den W15c-Entscheiden)

> **Statusblock iU9 — Teilwelle 16a umgesetzt (04.09.2026, Basis `975ead5` = Tag `vor-W16`, zusammengeführt mit `3c7e0d6` nach den W15c-Entscheiden)**
>
> **Der ganze Projektassistent bis auf seine Daten ist verschwunden — vier Masken, 1 694 Zeilen `.cs`, 988 Designer,
> 2 `MessageBox`, 26 Dateien:** `Wizard_Stromlastgang` (keine neue Komponente — die Assistentenseite 6 ist der
> `StromganglinieDialog` aus W12, W12‑O‑3), `Wizard_Komponenten` → `KomponentenauswahlDialog` (13 Kacheln über
> `Kachelraster`, die Rückfrage beim Abwählen einer belegten Komponente wortgleich mit `VorgabeNein`), `WizardParent` →
> Baustein **`Assistent`** (`Seiten` mit Titel/Inhalt/`Aktiv`, `NaechsteAktive(richtung)` statt `Next`/`Back`, „Weiter"
> wird auf der letzten aktiven Seite „Speichern") und Seite **`AssistentSeite`** (13 Seiten als `RenderFragment` in
> Bestandsreihenfolge, linkes Band nur in Betriebsart BEARBEITEN auf Schritt 0 mit der Razor-Projektliste aus W15a) hinter
> `AssistentHuelle` mit Gaben und Delegaten, `ProjektAuswahl` (uc) → der Baustein `ProjektListe` (die iZ5-Ausnahme aus
> W15a ist eingelöst). Dazu gelöscht: `WizardSeite`, `AssistentSeiten`, die zwei `IAssistent*Seite`, `IAssistentRahmen`
> und `BlazorAssistentSeite` (kein WinForms-Rahmen mehr). Im Kern: **`KomponentenBestandCtrl`** (unverändert verschoben,
> **Nachweis N6 zuerst**: `Bitmaske(id)` gegen den eingefrorenen `Form_Start.status`-Wert für **alle 13**
> Referenzprojekte — keine Abweichung, E‑3 damit erzwungen statt behauptet), **`AssistentCtrl`** (die sechs `Load*FromDB`,
> `SpeichernAusfuehren` beide Zweige mit der bitgleichen Reihenfolge der 21 Controlleraufrufe, Seitenschaltung) und
> `WizardCtrl` gleich mit (Befund W16a‑B2: seine einzige WinForms-Kante war ein totes Feld), `Kachel.Zustand`/`Aktiv`
> (B7), `IProjektQuelle.AssistentGaben(betriebsart, id)` mit Standardumsetzung. Sechs Sachcommits, Protokoll, Merge und
> Gate-Nachtrag (`d10b7b9` … `654bd66`), auf `ios_migration` als `81052cc`; `Views/Wizard` und `Views/Projekt` führen keine
> Designer-Maske mehr.
>
> **Acht Angleichungen** (A‑1…A‑8) und die Befunde W16a‑B1…B8, darunter: `WizardCtrl` in den Kern (B2), auch `LoadZGeb`
> ließ sein `RecordSet` offen (B3, behoben), K5 gegenstandslos (B5), **`AktionsKarte` fällt nicht mit W16a** — sechs
> Instanzen in `Form_Start`, sie geht mit W16b (B6). **Anwenderfragen:** E‑3 belegt (N6), **E‑4 halb** — die eine
> Meldung statt 17 stiller `return` ist da (`AssistentErgebnis` nennt den fehlgeschlagenen Schritt, der Assistent bleibt
> stehen), die **Transaktion nicht**: sie verlangte den Umbau aller 23 Schreibmethoden von `WizardCtrl` auf einen
> hereingereichten `DbVorgang`, was R‑W16‑6 ohne Windows-Feldvergleich untersagt — offen; E‑9 für den
> Kleinschreibungs-Zeugen umgesetzt (`Wizard_Komponenten` als Prüfmuster `Pruefmuster/Wizard/`); **neu W16a‑E‑1** (der
> Assistent bleibt unter Windows modal, Begründung wie R‑W10b‑1 — mit W16b/W16c könnte er eine freie Ansicht in derselben
> WebView werden: soll er? — **entschieden 04.09.2026: ja, in iU11 mit der Transaktion W16a‑O‑1**) und **W16a‑E‑2** (der NEU-Zweig schließt bei einem `Add_Projekt`-Fehlschlag nicht mehr
> kommentarlos, die Eingaben bleiben erhalten — bestätigen?). **Was W16b erbt:** der Rückweg der Hülle an
> `Program.startfrm.HinweisProjektGeoeffnet()` wird ein Rückruf an die Razor-Startseite, `IosProjektQuelle` setzt
> `AssistentGaben` noch nicht um, `Form_Start.UpdateWizardSymbole` ist ersatzlos zu löschen (N6 belegt die Gleichheit),
> `AktionsKarte` (3) und `Form_Hinweis` (3) fallen dort.
>
> **Nachweise** (auf dem gemergten Stand `81052cc`, Linux): Build → 0 Fehler, **6** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **3 923** grün (3 833 nach W15c; N6 16 Fälle, N5 `AssistentTests` 28, R‑W16‑6
> `AssistentCtrlTests` 26 — Bearbeiten- und Neu-Lauf lassen Zählstand, Bitmaske, Anlagenbezeichner und Kopffelder gleich),
> **identisch unter `LC_ALL=en_US.UTF-8`** · Formularkarte **123** grün (ein Zeuge ins Prüfmuster umgezogen) · Stapellauf
> **7** Masken / 8 Designer (**Sollwert exakt getroffen**), **7 erreichbar / 0 nein / 0 verwaist / 0 unklar** · SQL-Prüfer
> 1 234 Texte, 0 Fundstellen · ChartProben 32 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS, byte-gleich**
> (815 043 Werte) · beide Wächter leer.
>
> **Protokoll** mit den sechzehn geprüften Annahmen, dem Feldkartenabgleich, N5/N6, dem zweiten R‑W16‑6-Nachweis, acht
> Abweichungen, den Befunden, der Löschliste mit `git grep`-Nachweis und **elf Abnahmepunkten**:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W16a_Blazor_Port_Protokoll.md`. **Windows-Abnahme steht aus**,
> darunter der `projekt`-CSV-Vergleich eines neu angelegten und eines bearbeiteten Projekts Feld für Feld gegen den Stand
> `vor-W16` (R‑W16‑6, nur auf Windows). Der einundzwanzigste iOS-Lauf (33890882150) auf diesem Stand ist grün — erstmals
> mit dem Assistenten hinter `AssistentGaben` in der `AppWurzel` (N9; iOS setzt die Gaben noch nicht um, W16a‑O‑4).
> **Windows-Abnahme 05.09.2026 (PDF S. 6), zwei Befunde (`974c198`, Protokoll § 12):** **W16a‑B‑1** — die Weiche
> „Profil/Ganglinie" stand als eigener Kasten unter der Solarthermiekarte des Erzeugerreiters; sie sitzt jetzt in
> deren Rahmen, weil nur noch diese Kachel den Wirt bekommt und der den Kartenrahmen trägt (über das Markup ginge es
> nicht — eine Kachel ist ein `<button>`, der keine Optionsfelder enthalten darf), Klickziel und Tastaturweg
> unverändert. **W16a‑B‑2** — der Parametersatz einer Assistentenseite wird beim Betreten geholt und nicht mehr bei
> jedem Neuzeichnen (Herleitung im W9-Protokoll § 12.1, Befund W9‑B‑1).
>
> **Zum Anwenderwunsch W15a‑E‑1 vom 05.09.2026 (`325a275`), gemeldet an dieser Maske (Assistent, Seite 0, linkes
> Band):** Der Projektname bricht jetzt um, statt abgeschnitten zu werden; Varianten stehen eingerückt unter ihrem
> Stamm und tragen darunter leise „Variante von ‹Stamm›" — eine Artspalte hat in 280 px keinen Platz. `AssistentSeite`
> reicht dafür zwei Texte durch, `AssistentHuelle` füllt sie aus `PRJ_LIST_ART_VARIANTE`/`PRJ_LIST_VARIANTE_VON`.
> Herleitung im W15a-Protokoll § 14, hier § 13, Abnahmepunkt A‑W16a‑E‑1.
>
> **Formularraster, Paket P3 (iU8‑E‑2, 05.09.2026, `d3fccf1`):** `ProjektKopfSeite`: die sechs Felder zweispaltig im Raster
> (`epos-projektkopf-raster` gefallen), Pflichtsternchen, Legende und der Hinweis bei leerem/vergebenem Namen aus der
> Rechner-2-Linie (Merge 5) bleiben zwischen Feldern und Beschreibung — deshalb steht die Beschreibung als breites Feld
> unter dem Raster. Paket P3 gesamt: 14 Dateien, 168 Felder, 43 Raster, kein `Formulargruppe` nötig (jede Gruppe des
> Vorbilds ist schon ein `Gruppenkopf`), 15 `epos-feldpaar` und drei Zweispalter samt Stilblattregeln gefallen, eine
> Zeile CSS neu (Herleitungszeile spannt über alle Spalten); UI 2 595 (+14), Formularkarte 122.
>
> **W16a‑O‑1 erledigt — die Transaktion des Speicherwegs (11.09.2026, `ecce4cd1`), zusammengeführt in `d8014c1f`.**
> Die zweite Hälfte von Entscheid **E‑4** ist eingelöst: Die **23 Schreibmethoden** von `WizardCtrl` nehmen einen
> `DbVorgang` entgegen, `AssistentCtrl.Speichern` öffnet ihn **einmal** über den ganzen Lauf, schreibt ihn nur bei
> Erfolg fest und rollt bei jedem Fehlschlag zurück — beim gescheiterten Schritt wie bei der geworfenen Ausnahme.
> **Ein halb geschriebenes Projekt kann es nicht mehr geben**; das `AssistentErgebnis` nennt den Schritt unverändert
> weiter. **Die Reihenfolge der Schreibschritte ist Zeichen für Zeichen dieselbe geblieben** — der Diff von
> `WizardCtrl` entfernt genau die 23 alten Signaturen und sonst keine Zeile. Der eigentliche Befund steckte darunter:
> Ein bloß durchgereichter Vorgang hätte die zwölf Katalogcontroller (`CopyFromStamm`, `ApplyGanglinieToProjekt`,
> `KostenProjektPositionenCtrl`, `GeraeteWaisen`) nicht erreicht — sie holen sich je Anweisung eine EIGENE Verbindung,
> liefen also an der Transaktion vorbei und hingen unter WAL an deren Schreibsperre. Deshalb meldet jede
> Schreibmethode den Vorgang am FADEN an (neu `EPOS.Kern/Allgemein/Vorgangsklammer.cs`, `[ThreadStatic]` statt
> `AsyncLocal`, weil eine `SqliteConnection` nicht auf zwei Fäden darf); die Zugriffsschicht leiht sich an ihrer
> einen Stelle dessen Verbindung **samt Transaktion**, und ein `DataRepository.Vorgang()` unter der Klammer wird zum
> SAVEPOINT‑Unterpunkt statt zu einer zweiten Verbindung. **Ohne angemeldete Klammer ist die Zugriffsschicht
> unverändert** — eine eigene Verbindung je Anweisung, ohne Transaktion —, und alle Bestandsaufrufer (Startseite,
> Kontextmenüs, Tests) rufen weiter ohne Vorgang. Zwei neue Kern‑Prüffälle (`AssistentCtrlTests`): ein erzwungener
> Fehlschlag in **Schritt 5 von 14** lässt **24 projektgebundene Tabellen** Zeile für Zeile unverändert, und der
> erfolgreiche Lauf schreibt dasselbe wie die bisherige Schreibfolge — bis hin zu den vergebenen Ids; die Gegenprobe
> mit ausgehängter Klammer fällt rot aus. Der Bedienweg bleibt **modal**; die freie Ansicht ist W16a‑E‑1 (#62b).
> Gate grün: 0 Fehler / 6 eindeutige Warnungen, Kern **2 341** (+2), UI 3 403, SpeicherEngine 347, KiKern 469 — auch
> unter `en_US` —, Formularkarte 122, SQL 0 von 1 312, ChartProben 44, Referenzlauf 1030/1007/1017/1045 **4 × PASS
> und byte-gleich** gegen `2026-09-07_R6_PvKoeffizienten`, kein Schema.
>
> **Offen: Risiko R‑W16‑6 ist damit NICHT eingelöst** (→ **geschlossen 11.09.2026, Anwenderentscheid: „alter Schreibweg ist nicht mehr relevant"**)**.** Der Umbau des Schreibwegs verlangt den Feld-für-Feld-Vergleich
> am Windows-Gerät, und der läuft nur dort — je einmal für ein über den Assistenten NEU angelegtes und ein
> BEARBEITETES Projekt, mit dem Stand vor und nach dieser Welle (`Referenzlauf.exe projekt <id> <ordner>`, dann
> `vergleich`). Auf Linux belegen nur die zwei Kern-Prüffälle den Rückzug. **Abnahme auf Windows:
> A‑W16a‑O1‑1…8** — die zwei Assistentenwege, der `projekt`-Vergleich (‑3, **er schließt R‑W16‑6**), das erzwungene
> Scheitern mit unverändertem Projekt (‑4), der Wiederholungslauf ohne Dubletten (‑5), die Bestandswege der
> Startseite (‑6), der Waisen-Aufräumlauf ohne „database is locked" (‑7) und die englische Fehlermeldung (‑8).
> Zwei Restpunkte für den Anwender: **W16a‑O‑1‑R1** (`StelleSpaltenSicher` merkt sich das Anlegen einer Spalte
> statisch — ein Rückzug nimmt die Spalte zurück, der gemerkte Wert bleibt bis zum Programmstart; trifft nur
> Datenbanken, deren Migration diese Spalten noch nicht angelegt hat) und **W16a‑O‑1‑R2** (`WIZ_SPEICHERN_FEHLER`
> endet auf „die bereits geschriebenen Angaben bleiben stehen." — das stimmt jetzt nicht mehr; der Satz ist
> bewusst unangetastet geblieben, seine Neufassung in de und en ist eine Anwenderentscheidung).
>
> **W16a‑E‑1 / W16b‑O‑5 erledigt — der Projektassistent ist eine freie Ansicht (11.09.2026, `f2a1edd4`),
> zusammengeführt in `b3c9fdce`.** **Die letzte Fachseite verlässt ihre modale Hülle.**
> `BlazorDialogForm<AssistentSeite>` und jedes `DialogResult` des Assistenten sind gelöscht; die drei Schlüssel
> `ASSISTENT`, `PROJEKT_NEU` und `PROJEKT_BEARBEITEN` führen auf denselben Zweig der `AppWurzel`, die Betriebsart
> sagt der Schlüssel oder das Argument. Damit ist der Entscheid vom 04.09.2026 eingelöst — und zwar erst jetzt,
> weil er an **W16a‑O‑1** hing: Ohne Transaktion hinterließe ein seitlicher Ausstieg ein halb geschriebenes Projekt.
> **Die zwei Gründe für die Modalität sind einzeln abgelöst**: Die Meldung „Daten gespeichert" steht dort, wo die
> Antwort bekannt ist — in `AssistentHuelle`, hinter dem gelungenen Speicherlauf, als **Kurzhinweis der Startseite**
> statt als `MessageBox` —, und der Projektkontext wird unmittelbar nach dem Schreiben über
> `ProjektKontextCtrl.Gewechselt` nachgezogen, wie bei jedem anderen Projektwechsel; der Unterschied „zuletzt
> geöffnet merken" (Kachel) gegen „nur setzen" (Menü) bleibt erhalten.
>
> **Anwenderentscheid 62b‑E‑1 (11.09.2026)**: Wer den Assistenten mit ungespeicherten Eingaben verlässt, bekommt
> drei Wege in einer `Ueberlagerung` — **Speichern** (derselbe Weg wie der Knopf, samt Prüfung und der Transaktion
> aus W16a‑O‑1; **scheitert er, bleibt der Assistent stehen** und der Wechsel findet nicht statt), **Verwerfen**
> (Wechsel ohne Schreiben), **Bleiben** (Abbruch, auch über Esc). **Ohne Änderung gibt es keine Rückfrage**, und
> „geändert" leitet `AssistentCtrl.HatAenderungen` aus zwei **Zustandsabdrücken** ab — Projektkopf (acht Felder)
> und die sechs Fachlisten samt den dreizehn Seitenschaltern —, nicht aus einem Ereigniszähler, der auch bei einem
> Fokuswechsel hochzählte. Die Frage gilt für **jeden** Ausgang: Ansichtswechsel, „Abbrechen", „Projekt öffnen",
> **Programmschluss** und Sprachwechsel-Neustart; **Bearbeiten hat keinen Sonderweg**. Sie liegt in der
> Razor-Schicht und damit auf **beiden** Plattformen; für Windows reichen `INavigationsZiel.VerlassenFraglich`
> (synchron) und `.DarfVerlassen()` sie bis in `Hauptfensterrahmen.FormClosing`. Beide Mitglieder haben
> **Standardfassungen** — kein Implementierer bricht, und `IosNavigation` ruft die Schnittstelle ohnehin nur.
> Neu: `AssistentVerlassen`, fünf Textschlüssel in de und en (`designer_neu.py` gezogen, 5 379 → 5 381,
> wiederholbar). **Der Speicherweg aus W16a‑O‑1 ist unangetastet.**
>
> Gate grün: 0 Fehler / 6 eindeutige Warnungen, Kern **2 351**, UI **3 422** (+19), SpeicherEngine 347, KiKern 469,
> Formularkarte 122, SQL 0 von 1 312, ChartProben 44, Referenzlauf 1030/1007/1017/1045 **4 × PASS und byte-gleich**
> gegen `2026-09-07_R6_PvKoeffizienten`, kein Schema. **Auf Linux nicht prüfbar und deshalb nicht behauptet**: dass
> wirklich kein zweites Fenster entsteht, die Anmutung der freien Ansicht, `FormClosing`/`Application.Restart` und
> das Zusammenspiel mit der WebView2 — geprüft ist die **Schaltlogik**. **Abnahme auf Windows: A‑W16a‑E1‑1…13**
> (die zwei Menüwege und die zwei Startkacheln, Speichern neu und bestehend, „Abbrechen" mit und ohne Eingaben,
> Ansichtswechsel mit Zielansicht nach dem Speichern, scheiterndes Speichern in der Rückfrage, „Projekt öffnen",
> Programmschluss, Sprachwechsel, Anmutung bei 100/125/150 %). **Offen bleiben R‑W16‑6** (der Feldvergleich am
> Gerät, gemeinsam mit A‑W16a‑O1‑3; → **geschlossen 11.09.2026 durch Anwenderentscheid, siehe Eintrag im Welle‑11b‑Block**) **und `IosProjektQuelle.AssistentGaben`** (W16a‑O‑4): Auf dem iPad meldet die
> Wurzel weiter „Der Projektassistent steht auf diesem Gerät noch nicht zur Verfügung."; sobald die iOS-Hülle den
> Parametersatz liefert, bekommt sie die Rückfrage ohne weitere Arbeit mit — sie braucht dafür nur den Delegaten
> `HatAenderungen` in ihren Gaben. Zwei Auslegungen zur Bestätigung: „Abbrechen" geht durch dieselbe Rückfrage
> (strenge Lesart von „verlässt den Assistenten"), und ein Menüpunkt, der nur einen **Dialog** öffnet statt die
> Ansicht zu wechseln, fragt nicht.

## Statusblock iU9 — Welle 15c umgesetzt (04.09.2026, Basis f71853b nach W15b, zusammengeführt mit 5a73fd6 nach den W15b-Entscheiden)

> **Statusblock iU9 — Welle 15c umgesetzt (04.09.2026, Basis `f71853b` nach W15b, zusammengeführt mit `5a73fd6` nach den W15b-Entscheiden)**
>
> **Drei Masken — 1 588 Zeilen `.cs`, 246 Designer, 119 `.resx`, 16 `MessageBox` und drei Meldungen im Startweg — sind
> drei Razor-Komponenten, sechs Kern- und Hüllenvorarbeiten und vier Hüllenwege:** `LizenzVerwaltungDialog`
> (`EPOS.UI/Dialoge/Lizenz/`, Zustand als Zeichenkette, Aktivieren/Lesen/Trial/Freigeben/Auffrischen als Delegaten, das
> Schlüsselfeld nach Erfolg leer — S4; **auch als Überlagerung im Lizenzdialog**, A‑4), `ErststartDialog` (unbestimmter
> `Fortschritt` ohne Abbruch, Protokoll als `Textfeld`, **besitzerlose Hülle** mit den vier neuen Zusätzen an
> `BlazorDialogForm<T>` — `ImTaskbar`, `AufBildschirmMittig`, `SchliessenGesperrt`, `Mindestmass` — und dem Rückkanal
> `LaufAktiv`; für die 40 bestehenden Aufrufer ändert sich nichts) und `LizenzDialog` (drei `Reiterblatt`
> Lizenzvereinbarung / Rechtliche Hinweise / Komponenten, Zustimmungsmodus als Parameter, „Drucken" über den Browserdruck,
> „Speichern unter…" als Text, Verweise nur aus dem eigenen Ressourcentext und nie als `MarkupString`) hinter zwei
> Hüllenwegen (Menü Hilfe und der besitzerlose `ZustimmungSicherstellen`-Weg aus `Program.cs`). **Kein neuer Baustein**
> (B12). Im Kern: **`LizenzManager.Bewerten`** — die reine Zustandsrechnung aus `Pruefe()` herausgezogen, `Pruefe()` bleibt
> Fassade (E‑10, Verhalten unverändert) —, `LizenzCtrl` mit `LizenzGaben`, `EmailGueltig` und `LicDateiLesen` (liest nur,
> prüft nicht — S3), `LizenzTextCtrl` (die Online-Quelle ist **eine Zeile**, heute bitgleich die AGB-Seite — E‑17),
> `ZustimmungCtrl` (`catch → true` wortgleich, E‑15), `StatusText`/`TypText` über neun `MyResource`-Schlüssel;
> `ErststartCtrl` bleibt in der Windows-Anwendung, weil `ErststartMigration` OleDb mitbringt (Wächter). **Der
> Wellennachweis ist eine Erstanlage:** bis hierher prüfte kein einziger Test den 659-Zeilen-Lizenzkern (B1) — jetzt
> **+79 Kern-Fälle** (`LizenzZustandTests` 19: Ränder Kulanz und Karenz ±1 Tag, Uhrtoleranz, Laufzeit sticht Leine,
> Schreibrecht je Zustand — **kein Fall fasst die Ablage an**; `LizenzTokenTests` 10 mit einem im Test erzeugten
> Schlüsselpaar, **kein Server-Token im Repository**; `LizenzTexteTests` 12; `LizenzCtrlTests` 21; `LizenzTextCtrlTests` 17)
> und **+67 bunit-Fälle** (28 / 26 / 13), alle vor der ersten Maske. **E‑8, Weg 2:** `Program.Main` prüft nach der
> Sprachwahl und vor dem ersten besitzerlosen Dialog die WebView2-Laufzeit; fehlt sie, erscheint eine native `MessageBox`
> mit der Bezugsquelle (zweisprachig, Wortlaut nach dem Setup) und das Programm endet — keine WinForms-Rückfallmasken;
> die Zusage in `Umsetzung_iU8_Nachweise.md` („startet, nur der Dialog bleibt leer") ist berichtigt. **E‑7:** die
> 27 Rechtstexte stehen **deutsch in beiden Sprachzweigen** mit dem Zusatz „Binding version in German." (A‑9);
> **maschinell umgezogen und zurückverglichen: 26 von 27 zeichengleich, einer berichtigt** (O‑1 — .NET 10, SQLite und
> WebView2 statt .NET 8 und ACE). Zwölf Sachcommits, Protokoll, Merge und Gate-Nachtrag (`bb805d3` … `7cb03d1`), auf
> `ios_migration` als `2369f52`; 63 Texte des Lizenzdialogs zweisprachig.
>
> **Neun Angleichungen** (A‑1…A‑9): Suchleiste und A+/A− entfallen (die WebView zoomt selbst), keine RTF-Anzeige
> (`.rtf`/`.docx` zeigen denselben Hinweistext), Browserdruck statt `PrintDocument` (der einzige Nutzer des Bestands
> fällt), Verwaltung als Überlagerung, keine geratenen Verweise, Speichern als Text, Online-Fassung wird abgewartet statt
> aus `async void` geschrieben, `Mindestmass` statt einer verdeckten `MinimumSize`, der englische Zusatz einmal statt
> 27-mal. **Befunde** B1…B28 eingetreten — außer B26 (der Typzeuge stand seit W14a/W14c schon auf „Bestand ODER
> Prüfmuster", `GroupBox` liegt im eingefrorenen Muster); neu **B29** (E‑6 gegenstandslos: der „ja"-Zeuge liegt seit
> W14c auf `MDIMainForm`, der Wurzel) und **B30** (der Erststart überschreibt seinen Zustandstext mit der Schlussmeldung —
> bitgleich, mit Zeugen). **Anwenderfragen:** E‑8 Weg 2 (oben), E‑7 (oben — eine andere Entscheidung kostet 27 Werte im
> englischen Zweig), **E‑9 → iF30** (Register: Lesemodus-Durchsetzung nach W16 — `DarfSchreiben()` hat genau einen Leser),
> E‑2 (Druckknopf bleibt), E‑4 (kein iOS-Einstieg in die Lizenzverwaltung, iU11), E‑12 (Suchleiste entfallen), E‑17
> (Vertragsendpunkt `epos/v1/vertrag` später — eine Zeile). **Entschieden am 04.09.2026 (Empfehlungen angenommen):**
> W15c‑O‑1 — der Vertragsendpunkt löst die AGB-Seite ab, sobald der Lizenzserver 1.4.0 im Betrieb ist (eine Zeile und ihr
> Zeuge); W15c‑O‑2 — das `LizenzTexte`-Bündel für die zwei großen Komponenten **ist umgesetzt** (04.09.2026, `2281ece`:
> gemessen 18 bzw. 29 Einzelparameter, nicht 25/20, werden **einer**; `LizenzDialog` 449 → 349 und `LizenzVerwaltungDialog`
> 451 → 347 Zeilen; `LizenzTexte` füllt sich selbst aus `MyResource` in de und en, ein leerer Katalogeintrag bleibt leer (E‑7);
> die 54 Dialogfälle bleiben, ein neuer Fall prüft die Selbstfüllung aller Texte; Regel in `EPOS.UI/CLAUDE.md`: ab etwa
> zehn Anzeigetexten ein Bündel, `*Texte` = Beschriftungen, `*Gaben` = Zustand). **Offen:** W15c‑O‑3
> (Textsuche im Vertragstext), W15c‑O‑4 (Lizenzeinstieg auf iOS, iU11), W15c‑O‑5 (`Form_HelpPopup` fällt weder mit W15c
> noch mit W16).
>
> **Nachweise** (auf dem gemergten Stand `2369f52`, Linux): Build → 0 Fehler, **6** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **3 833** grün (3 687 nach den W15b-Entscheiden), **identisch unter
> `LC_ALL=en_US.UTF-8`** · Formularkarte **124** grün · Stapellauf **11** Masken (12 − 1; zwei der drei Masken waren
> Code-Formen ohne Designer), 14 Designer, **11 erreichbar / 0 nein / 0 verwaist / 0 unklar**, Lokalisierungszähler
> unverändert 7 · SQL-Prüfer 1 235 Texte, 0 Fundstellen · ChartProben 32 Bilder, 0 Verstöße · Referenzlauf
> 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte) · beide Wächter leer, S1/S2-`git diff` leer.
>
> **Protokoll** mit Feldkartenabgleich (zwei Karten von Hand), den 146 neuen Fällen, neun Abweichungen, den Befunden
> B1…B30, der Sicherheit S1…S4, den vier Hüllenzusätzen und **22 Abnahmepunkten**:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W15c_Blazor_Port_Protokoll.md`. **Windows-Abnahme steht aus**, und
> zwei Punkte sind nur dort führbar: **der Erststart auf einem echten `.accdb`-Bestand** mit Fehlschlag-Variante
> (Rückfallstand `git show 3ae6847:WindowsFormsApplication1/Views/Admin/Form_Erststart.cs`) und **die Windows-Sandbox
> ohne WebView2** (Meldung und Programmende statt leerer Dialoge); dazu Aktivieren/Lesen/Trial/Freigeben, die Zustimmung
> beim Erststart, Drucken, Speichern unter, de/en, 125 %. Der zwanzigste iOS-Lauf (33883210632) auf diesem Stand ist grün.
>
> **Rückweg-Anker für W16 (R‑W16‑12): `975ead5`** — der Statusblock-Commit dieser Welle, der letzte Stand vor W16a. Der
> Git-Tag `vor-W16` ließ sich aus der CI-Umgebung nicht pushen (der Push-Zugang der Umgebung erlaubt nur den Zweig
> `ios_migration`, Tag-Refs werden mit HTTP 403 abgewiesen); **der Anwender hat ihn am 04.09.2026 vom Arbeitsplatz aus
> gesetzt** — `refs/tags/vor-W16` zeigt auf `975ead5`.
>
> **Befund W15c‑B‑1 und Anwenderwunsch W15c‑E‑1 vom 05.09.2026 (Bildschirmfoto Hilfe → Lizenz: „Die Darstellung
> kann verbessert werden. Wozu gibt es ‚Datei wählen'? löschen"), umgesetzt in `ee7214f`:** **W15c‑B‑1** — Jede
> Zeile begann links vom sichtbaren Rand. Ursache war die Wurzelregel selbst: Die drei Wurzeln der Welle 15c
> (`.epos-lizenz`, `.epos-lizverw`, `.epos-erststart`) hängen als einzige nicht unter `.epos-dialog` und trugen
> deshalb weder Seitenrand noch `overflow-x` — das erste Zeichen stand bei x = 0 an der Kante der WebView, und bei
> 125–150 % schnitt sie es weg. Rand und Waagerecht-Sperre stehen jetzt an einer Stelle für alle drei
> (`overflow-x: clip`, nicht `hidden`). **W15c‑E‑1** — Der Vertragstext stand in einem `<textarea>` mit
> Größenanfasser; er ist jetzt eine Leseansicht: Kopf mit dem `InfoKnopf` (er stand hinter „Schließen"), je Karte
> derselbe gerollte Lesebereich mit Absätzen und den „§ …"-Zeilen als Überschriften, leise Fußzeile, rechts die
> Knopfleiste. Am Wortlaut ändert sich nichts, neu ist allein `LizenzDialog.Vertragsabschnitte`. **„Datei wählen…"
> ist gefallen:** `LizenzHuelle.DateiWaehlen` war der Rest der RichTextBox des Vorläufers (Filter `*.rtf;*.docx;*.pdf`)
> und ersetzte den lesbaren Vertragstext durch den Zeiger auf eine Datei, die die WebView seit E‑1 nicht anzeigt;
> der Weg zur `.lic` bleibt allein „Lizenz aktivieren…", eine Vertragsdatei findet `LizenzTextCtrl.DateiSuchen`
> weiterhin selbst. 15 neue bunit-Fälle (UI 2 497). Offen als **W15c‑O‑6**: Die vier KI-Wurzeln der Welle 15b haben
> dieselbe Form (kein Seitenrand, kein `overflow-x`) — sie gehören in die laufende Überarbeitung des
> Hilfe-Assistenten (W15b‑E‑3).
>
> **Welle iF30 — Lesemodus streng (Anwenderentscheid 04.09.2026), umgesetzt 06.09.2026 in `8daf051`:**
> `LizenzManager.DarfSchreiben()` hatte seit iU5‑U1 genau einen Leser (`KiAusfuehrer.Schreibrecht`, W15c‑B7) — der
> Lesemodus stand im Konzept, war sichtbar und prüfbar, aber nicht durchgesetzt. Er wird es jetzt an **einer** Stelle:
> `SqliteDatenzugriff.ErzeugeKommando` baut jede Anweisung des Kerns (die sechs Zugriffsmethoden, `DbVorgang`,
> `RecordSet`, `StilleDb` und die sechs Eigenverbindungen laufen dort hindurch; die zwei Kommandos daneben — PRAGMA und
> `last_insert_rowid()` — schreiben nicht), und dort steht `Schreibnaht.Pruefe(sql)`. Lesen bleibt frei (`SELECT`,
> `PRAGMA`, `EXPLAIN`, `VALUES` — die Liste ist die der Leser, nicht die der Schreiber); ein Schreibversuch wirft eine
> eigene `LesemodusException` mit fertigem Satz statt eines SQLite-Fehlers. **Fünf Ausnahmen** stehen als
> `Schreibnaht.Freigabe(grund)` an ihrer Stelle im Quelltext, nie über den SQL-Text: Schema- und Erststart-Migration,
> Schemamarker, Programmzustand (`Tab_Applikation` — ohne ihn ließe sich im Lesemodus kein Projekt mehr öffnen) und
> die Sicherung (`VACUUM INTO` ist ein Export). Die im Entscheid genannten Ausnahmen Lizenzaktivierung und
> Einstellungen brauchen keine: Beide berühren die Datenbank nicht. Der Simulationslauf fragt vor dem Start
> (`SimulationLaufCtrl.Vorpruefen`); Ansehen, Berichte und Export sind unberührt. **Die Falle der Welle** sind die
> Werkzeuge: Referenzlauf (Linux und Windows), iOS-Prüfmodus, Schemawerkzeug und Testvorrichtung laufen ohne Lizenz
> und schreiben — jedes hebt die Sperre mit einer benannten Zeile `Schreibnaht.WerkzeugFreigabe(grund)`; `ChartProben`
> braucht keine. Sichtbar ist der Zustand als Banner in der `AppWurzel`: der Lesemodus dauerhaft (der eine Zustand,
> den W16b‑E‑6 für ein Dauerbanner gelten lässt), die Warnstufen 30/14/7 als verfallender Hinweis. Nachweise: Kern
> 1 230 → 1 302, bunit +12, Referenzlauf 1030/1007/1017 byte-gleich zu `2026-09-05_R2_Zeitbasis`, ChartProben grün,
> SQL-Prüfer 0 Fundstellen, beide Wächter leer, Windows-Bau x64 fehlerfrei. Protokoll
> `iF30_Lesemodus_Protokoll.md`; offene Punkte iF30‑O‑1 (ist `Tab_Applikation` zu Recht Ausnahme?), iF30‑O‑2
> (Warnstufen je Programmstart statt täglich), iF30‑O‑3 (Banner läuft erst beim nächsten Start nach), iF30‑O‑4
> (`PRAGMA` bei den Lesern), iF30‑O‑5 (Access-Zweig der Erststart-Migration geht an der Naht vorbei).
>
> **Anwenderentscheide iF30‑O‑1…5 vom 06.09.2026, umgesetzt in `a3bd169`:** Vier Punkte bestätigen den gebauten Stand,
> einer ändert ihn. **iF30‑O‑2 („einmal täglich reicht")** ist gebaut: Die drei Warnstufen 30/14/7 erscheinen seither
> **einmal je Kalendertag** statt bei jedem Programmstart. Der neue `EPOS.Kern/Allgemein/Lizenz/LizenzWarnungMerker`
> hält in `Dienste.Einstellungen` unter `LizenzWarnungGezeigt` einen Vermerk `yyyy-MM-dd|stufe`; eine dringendere Stufe
> (30 → 14 → 7) zeigt auch am selben Tag noch einmal, ein unlesbarer Vermerk und eine werfende Ablage zeigen immer.
> Entschieden wird im **Kern** (`LizenzLage.MitTagesmerker` → neues Feld `LizenzLage.WarnungZeigen`, `Bilden` bleibt
> rein), die `AppWurzel` fragt nur das Feld. **Der Lesemodus ist keine Warnstufe und bleibt bei jedem Start sichtbar** —
> ebenso Kulanzfenster und fällige Nachprüfung, die den Merker gar nicht anfassen. **iF30‑O‑1** (`Tab_Applikation`
> bleibt Ausnahme A‑4), **iF30‑O‑3** (Banner bis zum nächsten Start) und **iF30‑O‑4** (`PRAGMA` gilt als Lesen) sind
> bestätigt; **iF30‑O‑5** ist mit „Access-DB nicht mehr relevant" geschlossen — **kein Rückbau beauftragt**, der
> Access-Zweig bleibt, wie er ist. Nachweise: Kern und UI 0 Fehler/keine neue Warnung, `EPOS.Kern.Tests` 1 348 →
> 1 372 grün, `EPOS.UI.Tests` 2 724 → 2 728 grün, Designer-Prüfung „abweichend 0", beide Kern-Wächter leer, Gate grün,
> Referenzlauf byte-gleich. Protokoll: § 5.1, § 6-Nachtrag und § 8 in `iF30_Lesemodus_Protokoll.md`; Abnahme A‑iF30‑11.
>
> **#157 Erststart ohne Altbestand (Befund des Wiki-Agenten #155, 09.09.2026, umgesetzt in `18f6b11`, zusammengeführt in
> `ed2b8b6`).** Fehlen im Datenbankordner `Kenndaten.sqlite` UND `Kenndaten.accdb` — die Lage jeder Neuinstallation auf
> einem frischen Rechner —, **startet das Programm nicht**: `DataRepository.DatenbankVorhanden()` ist falsch
> (`SqliteDatenzugriff` öffnet `Mode=ReadOnly` und legt nichts an), `ErststartMigration.Pruefe` meldet `BeidesFehlt`, und
> `Program.cs:392–400` endet mit der Meldung `START_DB_FEHLT`; eine leere Datenbank entsteht nicht, und das vom Setup nach
> `{app}\Vorlage` gelegte `Kenndaten.accdb` liest kein Pfad im Quelltext (die Erstkopie war ein Vorschlag des
> Setup-Konzepts § 6.2, nie gebaut). Belegt durch `Proben/ErststartProben` (9/9, ohne Windows, nicht in `WP-Plan.sln`).
> Richtiggestellt: `UebernahmeText` und `AceFehlt` in `Setup/EPOS-Plan.iss` (de+en) samt drei Kommentaren — die Aussage
> „eine Datenbank je Windows-Konto im Benutzerprofil" kannte der Code nie; `BETRIEB_SQLITE.md` § 1.1 neu. Anwenderrahmen
> vom selben Tag: Access wurde nie produktiv verwendet, der Übernahmeweg ist Hauswerkzeug, kein Kundenweg; die
> Anwenderdokumentation (Wiki, Website) nennt Access nicht mehr. **Offen: Entscheid #157‑E‑1** — W1 `.accdb`-Vorlage +
> Übernahme auf jedem neuen Rechner, W2 `.sqlite`-Vorlage (Übernahmeweg bleibt), **W3 `.sqlite`-Vorlage und Access-Weg
> samt Engine aus Setup und Erststart (empfohlen; deckungsgleich mit der iOS-Schale, die die Seed-Kopie schon so
> fährt)** —, dazu #157‑E‑2 (Erzeugung der ausgelieferten Vorlage, mit oder ohne Beispielprojekte) und #157‑E‑3
> (Deinstallations-Rückfrage zeigt auf `{localappdata}\EPOS_PLAN`, das nichts anlegt). Gate: Kern 2194 / UI 3362
> grün, kein Rechenweg berührt.
>
> **#161 Deinstallations-Rückfrage (#157‑E‑3, Anwenderentscheid 09.09.2026 „Empfehlung", umgesetzt in `ae6d8c7`,
> zusammengeführt in `e92589e`).** `CurUninstallStepChanged` fragte nach `{localappdata}\EPOS_PLAN` — einem Ordner, den
> nichts anlegt (Vorschlag des Setup-Konzepts § 6.2, nie umgesetzt); die Rückfrage erschien de facto nie. Seither zielt
> sie auf `{commonappdata}\EPOS_PLAN` (Datenbank samt `-wal`/`-shm` und `DB-Backup`), erscheint nur, wenn der Ordner
> existiert, Voreinstellung „Nein" (`MB_DEFBUTTON2`); der Text de/en sagt, dass der Ordner ALLEN Windows-Konten des Rechners
> gehört, dass es keinen Rückweg gibt und was NICHT gelöscht wird (die zwei `WP-Plan`-Datenverzeichnisse, die
> Registrierungseinstellungen — die der alte Text als gelöscht versprach, ohne dass je Code dahinterstand). Ein
> fehlgeschlagenes `DelTree` meldet sich jetzt (`DatenLoeschenFehlgeschlagen`, de/en) statt still zu bleiben. Nebenbefund
> im Kommentar belegt: `{}`-Blockkommentare verschachteln in Pascal nicht — Ordnerkonstanten stehen dort ohne Klammern.
> Doku: `Setup/Konzept_Setup_InnoSetup_EPOS-Plan.md` 2.4/6.3, `BETRIEB_SQLITE.md` § 8. Nur Setup-Skript und Doku, kein
> gebauter Code; der `ISCC`-Lauf auf Windows steht aus. Der Absatz „Deinstallation" in Wiki und Website wird mit W3 (#162)
> nachgezogen.
>
> **#160 Auslieferungsvorlage als Werkzeug (#157‑E‑2, Anwenderentscheid 09.09.2026 „Empfehlung", umgesetzt in `f763f60`,
> zusammengeführt in `1764653`).** `Werkzeuge/Auslieferungsvorlage` (eigene Projektmappe, nicht in `WP-Plan.sln`) erzeugt aus
> einer produktiven `Kenndaten.sqlite` reproduzierbar die bereinigte Auslieferungsdatenbank: Arbeitskopie über
> `Datenbanksicherung.KopieAnlegen` (Quelle bleibt byte-gleich), alle Projektdaten fallen — die **73 Tabellen mit
> Projektbezug leitet das Werkzeug aus dem Schema der geöffneten Datei ab**, nicht aus einer gepflegten Liste (die
> Handliste des Reduzierungsskripts kannte `Tab_Wechselrichter`/`Z_AnlageStrang` nicht) —, `Tab_Applikation` verliert
> Projektname (dort stand ein Kundenname), Beschreibung, Icon, `ID_Projekt`; Beispielprojekte kommen als `.wpx`-Pakete
> über `ProjektExportImportCtrl` (`--beispiele`, Beleg: Beispielkonzept § 6.2/E6), dann `VACUUM`, Prüflauf
> (`integrity_check`, `foreign_key_check`, Schemastand, STRICT-Zahl, Datenschutzwächter: keine Zeile in einer Projekttabelle
> außerhalb der Beispiele, keine Lizenz-/KI-Tabelle, keine Pfadangabe) und `<ziel>.bericht.txt`. Das Werkzeug schreibt im
> Repository nur nach `Setup/Vorlage/` (Rückgabe 3 sonst; `.gitignore` deckt `*.sqlite` dort ab); Rückgabecodes 0/2/3/4/5
> mit Grund auf stderr, bei ≠ 0 keine Zieldatei. Lauf gegen die Testdatenbank: 1 173 224 → 34 Projektzeilen, 66,8 → 23,7 MB,
> Schemastand 72, 117 STRICT. 17 Proben (`Werkzeuge/Auslieferungsvorlage.Tests`, starten das Werkzeug als Programm), seit
> diesem Stand als Schritt in `kern.yml` (nur ubuntu). Kern: zwei `InternalsVisibleTo`-Zeilen. **Befund #160‑F‑1, offen:**
> Die Regel des Setup-Konzepts § 6.1 Schritt 3 („in `*_STAMM` bleibt nur `ReadOnly = TRUE`") würde 22 von 28 Katalogtabellen
> leeren (419 722 → 101 Zeilen; `Tab_Kenndaten_STAMM` 1 960 Kennfelder, `Tab_Klimadaten_STAMM`, `Tab_Solar_STAMM` über die
> Kaskade), weil `ReadOnly` im Code ein Schreibschutz der Oberfläche ist, keine Auslieferungsmarke — der Katalogwächter
> bricht mit Code 4 ab, bis der Anwender entscheidet (`--kataloge alle` als ausdrücklicher Weg daran vorbei).
> Setup-Verdrahtung (`build-setup.ps1`, `VorlageDb`) folgt mit W3 (#162).
>
> **#162 W3 — Erststart aus der Auslieferungsvorlage (#157‑E‑1, Anwenderentscheid 09.09.2026 „Empfehlung", umgesetzt in
> `ba30d28`, zusammengeführt in `307bcba`).** Eine Neuinstallation startet wieder: `Program.DatenbankBereitstellen()` ruft den
> neuen Kernbaustein `EPOS.Kern/Allgemein/Datenbank/Erstbereitstellung.cs`, der `{app}\Vorlage\Kenndaten.sqlite` in den
> Datenordner kopiert und danach `PRAGMA integrity_check` UND `Tab_Applikation.SchemaVersion` prüft — nie überschreibend,
> nie halb (eine misslungene Kopie wird entfernt), eine ältere Vorlage hebt `SchemaMigration.Ausfuehren` beim selben Start
> an; fehlt die Vorlage, nennt `START_VORLAGE_FEHLT` den erwarteten Ort. Gefunden wird sie über die neue `IPfade`-Eigenschaft
> `Auslieferungsvorlage` (Aufstieg von `AppContext.BaseDirectory` wie `Herstellerdaten`; `IosPfade` erbt — kein iOS-Adapter
> nötig, die iOS-Schale behält ihren Seed-Weg aus dem Paket). **Der Access-Weg ist gefallen:** `ErststartMigration` (434 Z.),
> `ErststartCtrl` (74), `ErststartHuelle` (145), `ErststartDialog.razor` (167) mit 13 bunit-Fällen, 15 Ressourcenschlüssel
> je Sprache, die `EposSqliteMigrator.Kern`-Referenz der Anwendung, Fall 16 der `ZugriffsschichtProben`; im Setup 34 von 43
> Access-Fundstellen (ACE-Redist, vier Pascal-Funktionen, drei Meldungen, die Übernahme-Seite samt
> `InitializeWizard`/`ShouldSkipPage`), `VorlageDb` → `Vorlage\Kenndaten.sqlite` mit Abbruch, wenn sie fehlt.
> `build-setup.ps1` erzeugt die Vorlage vor jedem ISCC-Lauf über `Werkzeuge/Auslieferungsvorlage` (#160; Quelle aus
> `-Quelldatenbank` oder `EPOS_VORLAGE_QUELLE`, ohne Angabe Abbruch — kein Rückgriff auf die Arbeitsdatenbank). Bleiben als
> Hauswerkzeug: `EposSqliteMigrator`, `SchemaMigration.HebeAltbestand`, `SchemaVersionAccess`, `DbParamOleDb`. Doku:
> `BETRIEB_SQLITE.md` § 1 neu (§ 1.1 „Übernahme = Hauswerkzeug"), Wurzel-`CLAUDE.md`, drei Projekt-`CLAUDE.md`, Setup-Konzept
> § 6 (6.1–6.4). `Proben/ErststartProben` 9 → 21 Prüfungen. Gate nach dem Merge: Kern 2225 / UI 3358 grün (−13 bunit-Fälle
> des gefallenen Dialogs, +20 `ErstbereitstellungTests`), SQL-Dialektprüfer 0, beide Kern-Wächter leer, Referenzlauf
> byte-gleich. **Offen:** `ISCC`-Lauf und `build-setup.ps1` auf Windows (hier weder Inno Setup noch PowerShell), der erste
> gemeinsame Lauf mit #160, und der Kommentar in `EPOS.iOS/Datenbankbereitstellung.cs:15`, der noch
> `ErststartMigration.Pruefe` nennt (iOS hier nicht baubar).
>
> **#164 Build-Skript am Anwenderort (Anwenderhinweis 10.09.2026 „Inno Setup unter `C:\Waermeplan\WP_Plan\Setup`",
> umgesetzt in `243dca2`, zusammengeführt in `151dcea`).** `build-setup.ps1` suchte `ISCC.exe` nur unter `%ProgramFiles%`
> und in der Registry; seither Parameter `-Iscc` (Datei oder Ordner), Umgebungsvariable `EPOS_ISCC` und die Suche neben
> dem Skript (`ISCC.exe`, `Inno Setup 6\ISCC.exe`, Unterordner „Inno Setup*"), die Fehlermeldung nennt alle Kandidaten.
> Dazu `-Kataloge readonly|alle` → `--kataloge` an `Werkzeuge/Auslieferungsvorlage` (Rückgabe 4 = Katalogwächter → Hinweis
> „bis #160‑E‑1 mit `-Kataloge alle`"). Laufanleitung Windows im Setup-Konzept § 8.1 (sechs Schritte, Rückmeldung:
> Konsolenausgabe, Prüfbericht, ISCC-Meldungen). Nur Skript und Konzept, kein Gate; BOM/LF unverändert; der PowerShell-
> Lauf auf Windows steht aus.

## Statusblock iU9 — Welle 15b umgesetzt (04.09.2026, Basis c11f13d nach W15a, zusammengeführt mit 08cbc2a nach den W15a-Entscheiden)

> **Statusblock iU9 — Welle 15b umgesetzt (04.09.2026, Basis `c11f13d` nach W15a, zusammengeführt mit `08cbc2a` nach den W15a-Entscheiden)**
>
> **Vier Masken — 2 243 Zeilen `.cs`, 191 Designer, die eine `MessageBox` der Welle — sind fünf Razor-Komponenten,
> zwei Bausteine, ein Nachtrag und zwei Hüllen:** `TextAnzeige` (`EPOS.UI/Dialoge/Hilfe/`, Überlagerung), `KiHinweisDialog`
> mit `KiHinweisHuelle` (die Einwilligung aus `Program.cs` läuft jetzt **asynchron** über `KiEinwilligung.Nachfragen`, alle
> drei Aufrufer in einem Schritt), `KiEinstellungenDialog` (Schlüssel nur als Vorbelegung, `type="password"`, nie
> durchgereicht — S‑1/S‑2) und **`KiChatDialog` in vier Kindern** (Rahmen, `KiBestaetigungBlock` als Fußbereich des Verlaufs,
> `KiWerkzeugliste` mit der Kulturregel, `KiEingabezeile` mit Enter/Shift+Enter; keine `.razor` über 400 Zeilen), dahinter
> `KiChatHuelle` **nicht-modal mit Besitzer** (E‑6, holt ein offenes Fenster nach vorn). Neu: der Baustein
> **`Gespraechsverlauf`** (Bausteinlücke 17 — zehn Rollen, kein Streaming, kein Markdown, keine Link-Erkennung, Autoscroll nur
> unten, nichts in `localStorage`; 29 Fälle), der Baustein **`KiKnopf`** (der KI-Einstieg aus einer Maske, über
> `Seitenschluessel.KiAssistent` ohne `Masken.*`-Zwilling — E‑10) und `Warnbanner.Verfaellt`. Im Kern: **`KiChatService`
> (1 751 Z.) per `git mv`** — Befund B31: kein reines Verschieben, der Dienst ruft zehnmal `KiAusfuehrer`, deshalb die Naht
> `IKiAusfuehrung`/`KiAusfuehrungsweg` mit stiller Standardfassung (Bauart `Dienste.*`, **der Einwilligungsriegel bleibt
> davor** — S‑4), `Kurzbeschreibung.Umbrechen`, `KiAusfuehrer.AufOberflaeche` statt `Control`-Anker (E‑8), `KiChatKontext`
> (Positivliste der 24 Bereiche, Ermittlung in der Hülle — E‑9), `KiVerlaufstexte` (zwei getrennte Listen Anzeige/Prompt).
> **Zwei Masken bleiben bewusst:** `Form_HelpPopup` (E‑2, ihr Ersatz `IHilfeDienst` steht auf beiden Plattformen; fällt
> mit `HelpCatalog` in iU11) und `Form_Hinweis` (E‑1b, drei Aufrufer in `Form_Start`; fällt mit W16 — der Nachfolger
> `Warnbanner.Verfaellt` ist gebaut und geprüft). 16 Sachcommits, ein Merge und ein Gate-Nachtrag (`ab25d75` … `34047de`),
> auf `ios_migration` als `fa9d17f`; 21 Textschlüssel de/en (419 `KI_*` beidseitig), acht CSS-Variablen.
>
> **Die neun Zeugen entstanden vor den Masken** (T‑1…T‑9, 129 Fälle; **kein Netz in einem einzigen Fall**, Modellaufrufe
> nur über den Prüfkanal `Modellkanal`): Einwilligungsriegel P‑1 (ohne `Nachfragen` kein Modellaufruf, Abschalter,
> Fassung 1 < 2), Werkzeugrunde und die vier Ausgänge der Bestätigung P‑2/P‑3. **Zehn Angleichungen** (A‑1…A‑10): keine
> Maßparameter, Bestätigungsblock unten im Verlauf, keine Positionsrechnung, kein `DetectUrls`, Autoscroll nur unten, die
> eine MessageBox wird ein Warnbanner, die 400‑ms-Sperruhr und der Flackerschutz entfallen. **Befunde** B1…B30 eingetreten,
> vier neu (B31 Naht, B32 20 statt 17 Texte, B33, B34 `Schalter` hält seinen Zustand selbst — `@key` im Chat).
> **Anwenderfragen:** E‑1b und E‑6 wie vorläufig entschieden, E‑8/E‑9/E‑10 umgesetzt; **entschieden am 04.09.2026
> (Empfehlungen angenommen, `13835f2`/`aaaacce`, gemerged als `4775213`):** W15b‑O‑1 — der Schnitt der Naht
> `IKiAusfuehrung` ist bestätigt; die iOS-Hülle nutzt denselben Kern und läuft bis zu ihrer eigenen Fassung (O‑4) auf der
> stillen Standardfassung `KeineAusfuehrung` (fragen und suchen ja, ausführen nein); W15b‑O‑2 — der Tooltip der
> Semantikzeile ist als `title` zurück (wortgleich der alte Schlüssel `KI_SEMANTIK_HERKUNFT`, zwei Zeugen); **offen:** W15b‑O‑3 (`Standards/Schalter`-Rücksetzer, 20 Nutzer, eigene Welle), W15b‑O‑4
> (iOS bedient `KiAssistentGaben` noch nicht, Handprobe in iU11), W15b‑O‑5 (`Form_HelpPopup` ist die einzige Maske, die
> weder mit W15c noch mit W16 fällt).
>
> **Nachweise** (auf dem gemergten Stand `fa9d17f`, Linux): Build → 0 Fehler, **6** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **3 685** grün (3 524 nach den W15a-Entscheiden), **identisch unter
> `LC_ALL=en_US.UTF-8`** · Formularkarte **124** grün · Stapellauf **12** Masken (13 − 1; drei der vier Masken waren
> Code-Formen ohne Designer), 15 Designer, **12 erreichbar / 0 nein / 0 verwaist / 0 unklar** · SQL-Prüfer 1 235 Texte,
> 0 Fundstellen · ChartProben 32 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte) ·
> beide Wächter leer.
>
> **Protokoll** mit Feldkartenabgleich (drei Karten von Hand), den neun Zeugen, zehn Abweichungen, den Befunden B1…B34,
> der Sicherheit S‑1…S‑4 und 17 Abnahmepunkten: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W15b_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: Menü und F1, zweites Öffnen holt nach vorn, eine Frage mit Modell einmal echt, Nur-suchen
> ohne Modell, Werkzeugliste mit Kulturregel, die vier Ausgänge der Bestätigung, Rechtshinweis aus dem Chat und beim
> Erststart, „Modell neu erkennen" über Abbrechen hinweg, Kopieren, de/en, Esc je Ebene; **bekannter Schönheitsfehler
> (Punkt 16):** die DPI-Insel greift nur im modalen Lauf, der nicht-modale Chat ist ab 125 % bitmapskaliert (iF21, W16c).
> Der achtzehnte iOS-Lauf (33876284942) auf diesem Stand war **rot** — CS0103 im iOS-Hilfedienst aus W15b.0g, den nur der
> macOS-Läufer übersetzt; behoben in `f0e23a4`, der neunzehnte Lauf (33878903371) darauf ist grün.
>
> **Windows-Abnahme 05.09.2026 am Hilfe-Assistenten — Befunde W15b‑B‑1, W15b‑B‑2 und Wünsche W15b‑E‑3, W15b‑E‑4
> (Bildschirmfotos „Hilfe-Assistent", „Aktionen von Hand ausführen", HTTP 401), umgesetzt in `28082e8`:** **W15b‑B‑1** —
> „Einstellungen…" öffnete ein leeres Fenster, dann stürzte die Anwendung ab: `KiChatHuelle.Gaben.cs:208` gab
> `Task.FromResult(KiEinstellungenHuelle.Oeffnen(_fenster))` heraus, ein zweites modales `BlazorDialogForm` mit
> einer zweiten WebView2 synchron im `WebMessageReceived`-Rückruf der ersten — dieselbe Lage wie W16b‑B‑1 und
> W13‑B‑1 (Risiko R2); dieselbe Zeile für den Rechtshinweis, dritte Fundstelle `KiHinweisHuelle.Einhaengen`. Die
> Einstellungen und der Hinweis erscheinen seither als **Überlagerung** derselben WebView (Entscheid E‑5; beide Hüllen
> hatten ihren Parametersatz seit W15b.3/.4 dafür getrennt), die Delegatenwege laufen über `Blazornachlauf`; drei
> gleichartige Fundstellen in Bericht (`BerichtSeiteGaben.cs`), Einstellungen (`EinstellungenHuelle.cs`) und
> Ergebnisseite (`SimulationErgebnisHuelle.Wege.cs`) sind mitbehoben, und die neue Wache `HuellenwegTests` hält
> alle 63 Hüllendateien darauf, dass kein modales Systemfenster synchron aus einem Blazor-Ereignis kommt.
> **W15b‑B‑2** — die Frage endete mit dem rohen Anbietertext „HTTP 401 … Expected OAuth 2 access token": Neu
> `KiKern/KiDienstfehler.cs` mit `KiDienstAusnahme` — Anwendersatz in den Verlauf, Rohtext ins Protokoll
> („Protokoll anzeigen"); `SendenAsync` weist eine Anfrage **ohne** Schlüssel ab, bevor sie hinausgeht, und wertet den
> Rückgabewert von `TryAddWithoutValidation` aus. Der **doppelte Block** im Verlauf kam aus der Eingabezeile:
> `@onkeydown:preventDefault` wird beim Zeichnen ausgewertet, der Browser trug den Zeilenumbruch nach, `oninput`
> schrieb die Frage ins geleerte Feld zurück. Die Schlüsselablage selbst (`%APPDATA%\wp-plan\ki-schluessel.dat`,
> DPAPI) ist unverändert; steht künftig „Kein API-Schlüssel hinterlegt", ist der Speicher leer, bei „(401)" nimmt
> der Dienst einen vorhandenen Schlüssel nicht an. **W15b‑E‑3** — `.epos-kichat` hing an einer offenen Höhenkette,
> der Gesprächsverlauf war nicht zu sehen; er füllt jetzt die Höhe, die Begrüßung steht mit einem Satz und einer
> Klappe im Kopf, der Zähler genau einmal, die drei Knöpfe in einer Reihe (`KiChatAnmutungTests`). **W15b‑E‑4** —
> `KiAktion` führt Titel und Beispiel (zweisprachig, alle 24 Aktionen); die Werkzeugliste zeigt Klartext statt
> Bezeichner, gruppiert nach lesend/ändernd, mit Suchfeld, Kennzeichen, Beispiel und beschrifteten Pflichtfeldern,
> der Andockpunkt ist nur noch Kurztext, und das eingebaute Hilfewissen trägt den Abschnitt „Aktionen des
> Assistenten". UI 2 571 (+25), Kern 1 165 (+76), KiKern 469 (+19). Protokoll W15b, 21 Abnahmepunkte.
>
> **H13 Teil B — Hilferubrik „Berechnung", Erzeuger und Speicher (Anwenderwunsch 06.09.2026 „Erweiterung Hilfe: Erläutere in
> der Hilfe jeweils die Berechnungswege … die Details der Berechnung sollten in einer separaten Hilferubrik auf der Wiki sein …
> aufrufbar aus den allgemeinen Erklärungen mit Bezügen"), umgesetzt in `845f898`:** Sieben Seiten der neuen Rubrik
> `Programm Dokumentation/Berechnung/` liegen als MediaWiki-Markup im Rechenkern (`EPOS.Kern/Allgemein/Hilfe/Berechnung/*.wiki`,
> 1 512 Zeilen, über ein csproj-Glob eingebettet) — Heizkessel, BHKW, Wärmepumpe, Pufferspeicher, Solarthermie, Photovoltaik,
> Stromspeicher, jede mit denselben sechs Abschnitten (Was berechnet wird, Eingangsgrößen, Rechenweg, Grenzen und Annahmen,
> Ergebnisse, Bezüge). Jede Zahl ist aus dem Rechenkern belegt, und jede Seite sagt unter „Grenzen und Annahmen", was der Kern
> NICHT tut: die drei Befunde aus W14a‑E‑8, die vier fest verdrahteten Annahmen der Solarthermie (T_Speicher 50 °C,
> Leitungsverluste 0,92) und der Punkt, dass die Solarthermie-Ganglinien vom Lauf gar nicht gelesen werden. Die PV-Seite trägt
> den Abschnitt „Wechselrichter" mit den zwei Optionen des Entscheids W6‑E‑3, Option 2 als Ausblick „in Umsetzung, Stand
> 06.09.2026". Zehn neue Zuordnungszeilen (`<Form>.Berechnung` → `Berechnung/<Seite>`) führen aus zehn Razor-Dialogen dorthin;
> der Fensterknopf oben rechts bleibt daneben; die Knöpfe stehen in der eigenen Klasse `.epos-berechnungshilfe`, weil die
> Knopfleiste eine Aufzählung von Aktionen ist. 53 neue Testfälle halten Aufbau, Zuordnung, Einbettung und Knopf. Die Wiki-Seiten
> legt der Anwender aus den Dateien an (fünf Handgriffe im Protokoll H13b); bis dahin schaltet der Katalog die zehn Knöpfe ab.
> Offen: O‑H13b‑3 (Anker je Abschnitt), Anwenderfrage, ob der Berechnungsknopf auch in die Katalogeditoren soll. Teil A
> (Katalog, Lader, KI-Wissen, Startseite, Bezüge, Bedarfsseiten) folgt. Nachweis: Kern/UI 0 Fehler, Gate grün, Referenzlauf
> byte-gleich. Protokoll `WindowsFormsApplication1/Allgemein/Hilfe/H13b_Berechnungshilfe_Erzeuger_Protokoll.md`.
>
> **H13 Teil A — Hilferubrik „Berechnung": Katalog, Lader, KI-Wissen, Startseite, Bezüge und die Seiten des Ablaufs und des
> Bedarfs, umgesetzt in `404c394`:** Der Hilfekatalog kennt die Unterrubrik `Programm Dokumentation/Berechnung/`: der Onlineabruf
> holt Seiten zweiter Stufe von selbst (`apprefix` ist ein reiner Zeichenkettenpräfix, gemessen), `BerechnungsRueckfallErgaenzen`
> hält die Rubrik eng gefasst am Leben, solange das Wiki sie noch nicht führt — nach F6 hätte ein erfolgreicher Abruf den
> Startbestand sonst vollständig ersetzt und alle Knöpfe stumm geschaltet —, und `PfadNormalisieren` ebnet Leerzeichen zu
> Unterstrichen (`Wärmequelle Erdreich`); `help_cache.json` 32 → 46 Einträge. Im Kern liest `BerechnungsHilfe` die eingebetteten
> `.wiki`-Seiten (Kopfblock, Markup, Klartext), und `HilfeWissen` hängt je Seite einen Wissensabschnitt an — der Assistent kennt
> jeden Rechenweg ohne Netz. Sechs Seiten (Simulationsablauf, Wärmebedarf, Brauchwasser, Prozesswärme, Strombedarf, Wärmequelle
> Erdreich), acht Infoknöpfe in sechs Dialogen, dazu `_Index.wiki` (Rubrikstartseite mit allen 13 Seiten) und `_Bezuege.wiki`
> (18 kopierfertige Abschnitte „Berechnung" für die allgemeinen Seiten) als Arbeitsvorlagen für den Anwender. Jede Zahl gegen den
> Rechenkern belegt, Fundstellen im Kopfblock; die Brauchwasserseite nennt W8‑O‑5b als Grenze (Anwenderentscheid offen).
> Offen: H13‑O‑1 (die sieben Beschreibungen der Teil-B-Seiten im Startbestand an den Satz „Was berechnet wird" angleichen).
> Fünf Handgriffe des Anwenders im Wiki stehen in § 9 des Protokolls `H13_Berechnungshilfe_Protokoll.md`; bis dahin sind die
> Knöpfe über den Startbestand wirksam. Nachweis: Kern/UI 0 Fehler, Gate grün, Referenzlauf byte-gleich.
>
> **H13 Fassung 2 — Formelzeichen, Parameter und mathematische Schreibweise (Anwenderwunsch 06.09.2026: „Definiere in der
> hochgeladenen Dokumentation die Definition der Parameter und Variablen. Stell wenn möglich die Formeln in mathematischer
> Schreibweise dar."), umgesetzt in `d0c17a1` (Teil A), `75866fa` (Teil B) und `25c08e2` (Zusammenführung):** Alle 13 Seiten
> der Rubrik tragen zwischen Eingangsgrößen und Rechenweg den Abschnitt **„Formelzeichen und Parameter"** mit zwei Tabellen
> (Parameter: Symbol, Bedeutung, Einheit, Herkunft — Variablen: Symbol, Bedeutung, Einheit, berechnet in; 208 Parameter- und
> 255 Variablenzeilen) und **205 nummerierte Anzeige-Gleichungen in Unicode-Notation** (Malpunkt, Σ, Δ, √, griechische
> Buchstaben, Indizes als `<sub>`/`<sup>`). Grund für Unicode statt LaTeX: Das Wiki hat **keine Math-Erweiterung** (gemessen
> 06.09.2026 über `siteinfo` — 25 Erweiterungen, Math nicht darunter), `<math>` erschiene dem Leser als Klartext. Die
> Schreibweise steht als Abschnitt auf der Rubrikstartseite (gemeinsame Zeichentabelle aller Seiten, Semikolon als
> Argumenttrenner von min/max). `BerechnungsHilfe.Klartext` setzt Indizes für den Assistenten in `P_AC,nenn`/`T^2` um und
> löst `&nbsp;` auf. Neun Unstimmigkeiten der Fassung 1 gegen den Rechenkern berichtigt (u. a. Viertelstundenwerte sind kW,
> Klimaeinstrahlung W/m² im Tagesmittel, Ost und West teilen eine Fensterfläche, Nutzungsgrad des Kessels brennstoffbezogen,
> Vollzyklen des Puffers rollenabhängig, Entladeleistung als Stundenbudget). Die vier Wächter prüfen sieben Abschnitte in
> Reihenfolge, das Verbot von `<math>`/LaTeX, lückenlose Gleichungsnummern, beide Tabellen und die Kodierung nach der
> Einbettung — seit der Zusammenführung für **alle 13 Seiten** (H13‑O‑5 geschlossen; Bauform angeglichen: Unterüberschriften
> Parameter/Variablen und Formelnummer hinter `&nbsp;&nbsp;` auf allen Seiten). Vorschau-Probe 13/13 über die
> Wiki-Schnittstelle. Nachweis: Kern 1 558 / UI 2 921 grün, Gate grün, Referenzlauf byte-gleich. **Wiki:** Fassung 2 mit
> `--ueberschreiben` hochgeladen (06.09.2026, 19:29 UTC); die Rubrikseite „Programm Dokumentation" trägt nach Anwenderwunsch vom
> 06.09.2026 die sieben Unterrubriken voran und die Zuordnung der 100 Aufrufstellen gruppiert (Revision 446, Zeilen
> wortgleich, Anker unverändert). **Neu vom Anwender (06.09.2026, nach Sichtung der Fassung 1 im Wiki):** echte
> mathematische Darstellung wie LaTeX (Summen- und Integralzeichen, Brüche, Indizes) und die Definitionen der Zeichen
> unmittelbar unter jeder Formel (Beispiel: SKZ = P_el / P_therm; P_el: elektrische Leistung des BHKW …) — offen als
> **H13‑O‑6** (Math-Erweiterung auf dem Wiki-Server, native MathML-Wiedergabe ohne Zusatzdienst; Anwenderentscheid, Ersatzweg
> Formelbilder) und **H13‑O‑7** (Legende je Gleichung als Fassung 3, unabhängig von H13‑O‑6).
>
> **H13 Fassung 3 — LaTeX-Formeln und eine Legende unter jeder Gleichung (Anwenderwunsch 06.09.2026 nach Sichtung der
> Fassung 1 im Wiki: „stelle die Berechnungsdokumentation mit mathematischen Zeichen … wie LaTeX dar. Die Definitionen der
> Parameter/Variablen … sollte unter der verwendeten Formel beschrieben werden" und „mathe erweiterung soll für die Formeln
> installiert werden auf wiki"), umgesetzt in `7da5c69` (Teil A), `ef85efb` (Teil B), `cb97a24` und `88201ac`
> (Zusammenführung):** Der Anwender hat die **Math-Erweiterung** auf `wiki.epos-plan.de` installiert (gemessen 06.09.2026:
> `siteinfo` führt „Math", 26 Erweiterungen; native MathML-Wiedergabe ohne Zusatzdienst). Alle 13 Seiten der Rubrik tragen
> ihre **205 Anzeige-Gleichungen als LaTeX in `<math>\displaystyle …</math>`** mit der laufenden Nummer und **darunter die
> Legende — 781 Zeilen, je Zeichen eine** (Ergebnisgröße zuerst, Bedeutung, Einheit in eckigen Klammern, Konstanten mit
> Wert); das Beispiel des Anwenders steht wörtlich als BHKW (1): SKZ = P_el / P_th mit drei Legendezeilen. Die Symbolspalte
> beider Tabellen und die Zeichen des Fließtextes sind ebenfalls `<math>`; `<big>` ist verschwunden. Der Riegel der
> Fassung 2 kehrt sich um: nicht mehr „kein Backslash", sondern **nur der vereinbarte Befehlsvorrat** (WikiTexVC-sicher;
> `\lvert`/`\rvert` kennt WikiTexVC nicht — Befund der Wiki-Probe, in Erdreich (18), Pufferspeicher (18) und Photovoltaik
> (3) durch `\left| … \right|` ersetzt); Fallunterscheidungen als `cases`, Komma statt Semikolon in min/max, Umlaute in
> Indizes als ASCII-Umschrift. Die Schreibweise auf der Rubrikstartseite ist neu gefasst. `BerechnungsHilfe.LatexKlartext`
> setzt `<math>` für den Assistenten in lesbare Zeichen um (Bruch, Summe mit Limits, Index/Hochzahl, cases, griechisch;
> sechs Fälle). Die vier Wächter prüfen seit `88201ac` für **alle 13 Seiten**: `<math>`-Gleichung mit Nummer, lückenlose
> Nummern, Legende unter jeder Gleichung, kein `<big>`, kein Fremdbefehl, beide Tabellen, sieben Abschnitte, Fassung im
> Kopfblock, `<math>` und ein tragender Befehl nach der Einbettung. Nachweise: lokale Probe (`latexprobe.py`, latex2mathml)
> **2 024 Formeln / 0 Fehler**; Wiki-Probe über `action=parse` gegen die installierte Erweiterung **14/14 Seiten ohne
> Parserfehler**; Kern 1 590 / UI 2 973 grün, Gate grün, Referenzlauf byte-gleich (der Rechenweg ist unberührt). **Wiki:**
> Fassung 3 mit `--ueberschreiben` hochgeladen (06.09.2026, 20:33 UTC). H13‑O‑6 und H13‑O‑7 geschlossen.
>
> **O‑H13b‑5, H13‑O‑1, O‑H13b‑3 (Anwenderentscheid 07.09.2026: „Empfehlung"), umgesetzt in `b652bba`/`7172ef9`/`285e4c1`,
> zusammengeführt in `89e2d6a`:** **O‑H13b‑5:** Der Knopf „Berechnung" steht auch in den **acht Katalogverwaltungen**
> (Heizkessel, BHKW, Solarkollektoren, Pufferspeicher über `KatalogBrowserDialog`; Stromspeicher, PV Module, Wechselrichter
> über `ModulKatalogDialog`; Wärmepumpen-Stamm über `WaermepumpeStammDialog`); Schlüssel und Zielseite stehen im Profil im
> Kern neben `HilfeSchluessel`, damit Windows und iOS denselben Weg haben; zwei belegte Abweichungen (Wechselrichter → Seite
> Photovoltaik, Abschnitt; `Form_WP_Stamm.Berechnung`, weil der kürzere Name dem Anlagendialog gehört); Zuordnung 10 → 18
> Zeilen, `BerechnungsknopfTests` liest zusätzlich die Katalogprofile. **H13‑O‑1:** Die sieben Erzeugerseiten sprechen im
> Startbestand (`help_cache.json`, der Text des Infoknopfs ohne Netz) den Anfang ihres Abschnitts „Was berechnet wird" —
> so viele ganze Sätze, wie in 240 Zeichen passen (Heizkessel 228, Wärmepumpe 51 …); Wächter gegen Drift mit Gegenprobe an
> der Satzgrenze; die sechs Teil-A-Seiten bleiben außen vor (neuer Punkt **H13‑O‑7**, „Wärmequelle Erdreich" endet im
> ersten Absatz mit Doppelpunkt vor einer Aufzählung). **O‑H13b‑3:** **92 Sprungmarken** auf allen 13 Rechenwegseiten
> (`was, eingang, zeichen, rechenweg, grenzen, ergebnisse, bezuege`, Photovoltaik dazu `wechselrichter`) — die Form ist
> gegen das Wiki belegt: `Vorlage:Anker` existiert (pageid 33), `MediaWiki:Common.css` stellt sie auf null Pixel, die
> Rubrik „Grundlagen" benutzt sie eine Zeile unter der Überschrift; dieselbe Messung bestätigt MediaWiki 1.46.0 mit
> Math-Erweiterung (H13‑F3‑1 erledigt). 26 Knopf-Zuordnungen zielen auf `#rechenweg` (Ausnahme Wechselrichterkatalog →
> `#wechselrichter`), Fensterknöpfe und F1 bleiben ohne Anker, der Assistent sieht keine Marke (`AlsKlartext`/`Saeubern`
> vor dem Tabellengerüst); `action=parse`-Probe aller 14 Seiten: 92 Anker, 0 Formelfehler (H13‑F3‑3 erledigt). Nachweis:
> Kern 1790 / UI 3102 grün, Formularkarte 122, SQL 0, Gate grün, Referenzlauf byte-gleich. Abnahme auf Windows:
> A‑H13b‑14…24, A‑H13‑19…21. **Upload 07.09.2026:** alle 14 Seiten der Rubrik (13 Rechenwegseiten und die
> Rubrikstartseite) mit dem Bot-Zugang überschrieben (Zusammenfassung „Stand 07.09.2026, Sprungmarken je Abschnitt"),
> die 18 Bezüge und die Rubrikzeile waren schon da; Nachprobe über `action=parse`: Anker gerendert (`class="epos-anker"`,
> `id="rechenweg"` vorhanden), 0 Parserfehler, `<math>` unverändert (Wärmepumpe 155, Photovoltaik 324).
>
> **#158 Sicherungspunkt SQLite-fest (Befund des Wiki-Agenten #155, 09.09.2026, umgesetzt in `ad42a78`, zusammengeführt in
> `e772799`).** `KiSicherungspunkt` kopierte vor der ersten ändernden Aktion einer Sitzung nur die Hauptdatei — unter SQLite
> im WAL-Modus fehlten der Kopie die Änderungen aus der `-wal`, und die Prüfung auf eine `.laccdb`-Sperrdatei konnte nie
> mehr treffen. Seither gibt es EINE Sicherungswahrheit im Kern: `Datenbanksicherung.KopieAnlegen(quellpfad, zielordner,
> praefix)` (`EPOS.Kern/Allgemein/Datenbank/`) zieht die Kopie über `VACUUM INTO` auf einer geöffneten Verbindung — der Weg,
> den `BETRIEB_SQLITE.md` § 3.2 nennt und den die iOS-Schale (`Datenbankbereitstellung.SicherungAnlegen`) schon fuhr —, mit
> `Schreibnaht.GRUND_SICHERUNG`, damit Sicherungen auch im Lesemodus erlaubt bleiben; Ergebnis ist eine in sich
> geschlossene Datei ohne `-wal`/`-shm`. `KiSicherungspunkt` (Ordner `DB-Backup`, Zeitstempelmuster, Sperre bei Fehlschlag
> unverändert) und `MenueCtrl.DatenbankKopieAnlegen` (Projekte löschen, Projektimport) nutzen den Helfer; `File.Copy`
> ist in beiden Dateien weg (Wächter als Testfall). `KI_SICH_GEOEFFNET` samt Ressourcenschlüssel entfällt — ein Zwischenstand
> der Kopie ist mit `VACUUM INTO` strukturell ausgeschlossen, ein `SQLITE_BUSY` läuft über den vorhandenen Fehlerpfad
> `KI_SICH_FEHLGESCHLAGEN`; `KiSicherungspunkt.Hinweis` bleibt für `KiAusfuehrer` erhalten und liefert `""`. Nebenwirkung:
> Der Zeitstempel der Projektverwaltungs-Kopie trägt seither Bindestriche (`Kenndaten_<zweck>_JJJJ-MM-TT_hhmmss.sqlite`).
> Tests: `DatenbanksicherungTests` (6, u. a. Kopie enthält nur in der `-wal` stehende Änderungen, keine Begleitdateien,
> unbeschreibbares Ziel sperrt) und `KiSicherungspunktTests` (3); Hausregel in `EPOS.Kern/CLAUDE.md`, Satz in
> `BETRIEB_SQLITE.md` § 3.2. Gate: Kern 2205 / UI 3372 grün, SQL-Dialektprüfer 0, Referenzlauf byte-gleich (kein Rechenweg
> berührt); rot allein durch die Warnungsschranke — die siebte eindeutige Warnung `CS8602` in
> `EPOS.UI.Tests/Standards/RasterTests.cs:335` kam mit `97a7fec` (Opus-Sitzung, W13‑B‑6) und ist nicht Teil von #158;
> sie ist in der Katalogliste-Linie zu beheben, damit die Schranke von sechs wieder trägt.

## Statusblock iU9 — Welle 15a umgesetzt (04.09.2026, Basis f7e2758 nach W14c, zusammengeführt mit 8651b0d nach den W14c-Entscheiden)

> **Statusblock iU9 — Welle 15a umgesetzt (04.09.2026, Basis `f7e2758` nach W14c, zusammengeführt mit `8651b0d` nach den W14c-Entscheiden)**
>
> **Sechs Bauteile — 1 254 Zeilen `.cs`, 683 Designer, fünf Formen und ein UserControl — sind ein Baustein, drei Dialoge,
> eine Assistentenseite und vier Hüllen:** der Baustein **`ProjektListe`** (`EPOS.UI/Bausteine/`; der Bestand führte **vier
> Projektlisten nebeneinander**, die fünfte lag fertig als iOS-Seite — „Eine Projektauswahl für alle" aus
> `Konzept_Projektdialoge_Vereinheitlichung.md` ist eingelöst, `Seiten/Projektliste` baut darauf und ihre fünf Tests sind
> unverändert grün), `ProjektWahlDialog` (`EPOS.UI/Dialoge/Projekt/`, Zweck Öffnen oder Löschen — zwei Masken in einer
> Komponente), `ProjektKopieDialog` („Speichern unter", Duplizierlauf mit Fortschritt und **Abbruch über
> `CancellationToken` mit Rollback**), `ProjektTransferDialog` (Export/Import, erstmals englisch) und `ProjektKopfSeite`
> (`EPOS.UI/Seiten/Assistent/`, die erste Assistentenseite als Razor über `BlazorAssistentSeite`, Weg (a) ohne Umbau am
> Rahmen). **`ProjektAuswahl` (uc) bleibt bis W16** — bewusste iZ5-Ausnahme, weil `WizardParent` es hostet; nur die Hüllform
> fällt. Im Kern: **`ProjektExportImportCtrl` (1 278 Z.) per `git mv`** — die einzige Kante war `SchemaMigration.ZIEL_VERSION`,
> jetzt `SchemaStand.Zielversion` (62) —, `ProjektAngaben`, `ProjektCtrl.IdVonName`/`NamenListe`/`LoeschenMitVorarbeiten`/
> `Kopf`, `ProjektDuplizierenCtrl.PruefeNamen`/`VerwaltungsfelderSetzen`, `KlimaregionStammCtrl.IdVonName`/
> `NameZuProjektregion`, `IProjektQuelle.TransferDaten()` mit Standardumsetzung; 83 Textschlüssel de/en. **Die Proben
> entstanden zuerst — und fanden Befund B55: der Projektimport war seit der SQLite-Umstellung kaputt** (benannte
> Platzhalter `@id`/`@k0`/`@c0` im SQL-Text, die Zugriffsschicht bindet nach Position; jeder Import brach mit „Must add values
> for the following parameters"). Vier Stellen auf `?`, P1–P5 danach grün, vor und nach dem Umzug. Elf Sachcommits, ein Merge
> und ein Gate-Nachtrag (`7d8c93a` … `b612775`), auf `ios_migration` als `e759eaf`.
>
> **Zwölf Angleichungen** (A‑1…A‑12): kein Emoji auf „Abbrechen", Duplizieren abbrechbar, Fenster wächst nicht, **die
> Dublettenprüfung wird richtig** (`PruefeNamen` statt Präfixsuche — „Muster" neben „Musterprojekt" wird angenommen),
> Doppelklick markiert nur, Löschdialog mit Esc, Transferdialog übersetzt, Datum folgt der Programmsprache, Sicherung und
> Importbericht über Delegaten (Windows-Vorgabe unverändert), eine Projektliste mit Suche auch in „Löschen"; **A‑12 gilt
> nicht für „Export"** — der Transferdialog behält sein Auswahlfeld (Platz unter der Variantenliste; W15a‑O‑2). **B56 widerlegt
> B25:** „Projekt → Öffnen…" ist im MDI-Menü vorhanden und verdrahtet, E‑6 ist gegenstandslos. **Anwenderfragen
> entschieden:** E‑1 nein, E‑2 ja, E‑3 nur markieren, E‑4 Programmsprache, E‑5 „Löschen" ja / „Export" nein. **Offen:**
> W15a‑O‑1 (P6, Referenzlauf auf ein importiertes Projekt, nicht gelaufen — Ersatz Abnahmepunkt 4). **Entschieden am
> 04.09.2026:** W15a‑O‑2 (Empfehlung angenommen — der Transferdialog behält sein Auswahlfeld, keine volle Projektliste im
> Export) und W15a‑O‑3 („Projektname darf nicht gleich sein, daher löschen. Rückfragen in diesem Fall": Namen sind über den
> eindeutigen Index `Projektname` eindeutig, das Löschen über den Namen bleibt; trifft ein Name mehrere Projekte, fragt das
> Programm mit Vorgabe „Nein" nach statt still beide zu löschen — umgesetzt in `ba806b7`, gemerged als `fe07e82`:
> `LoeschStand.Mehrdeutig` mit Anzahl in `ProjektCtrl.LoeschenMitVorarbeiten`, zweite `Rueckfrage` im `ProjektWahlDialog`,
> sieben Tests, darunter zwei auf einer Arbeitskopie ohne den Index). **W15a‑O‑4 (entschieden 04.09.2026, Empfehlung
> angenommen):** `VariantenCtrl.LoescheVariante` rief `ProjektCtrl.Delete(name)` direkt — jetzt dieselbe Vorprüfung
> (`LoeschBefund` statt `bool`, `Mehrdeutig` mit Anzahl) und dieselbe zweite Rückfrage in der `UebersichtSeite`, sechs
> Tests; umgesetzt in `5104ea3`, gemerged als `1c49f38`.
> **Testanker:** der Maskenschlüssel-Zeuge steht jetzt auf `FormMain`/`Masken.ProjektDetail`, zwei W16-Aufträge (T1, T2)
> stehen in den Tests und im Protokoll.
>
> **Nachweise** (auf dem gemergten Stand `e759eaf`, Linux): Build → 0 Fehler, **6** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **3 511** grün (3 436 nach den W14c-Entscheiden; P1–P5, P7–P9, 14 Fälle `ProjektListe`,
> 45 Fälle der drei Dialoge und der Seite), **identisch unter `LC_ALL=en_US.UTF-8`** · Formularkarte **124** grün ·
> Stapellauf **13** Masken (17 − 4; `Form_ProjektExportImport` war eine Code-Form ohne Designer), 14 Designer,
> **13 erreichbar / 0 nein / 0 verwaist / 0 unklar** · SQL-Prüfer 1 234 Texte, 0 Fundstellen · ChartProben 32 Bilder,
> 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte) · beide Wächter leer.
>
> **Protokoll** mit Feldkartenabgleich je Maske (Transfermaske von Hand), den Proben, zwölf Abweichungen, den Befunden
> B1…B56 und der Windows-Abnahme: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W15a_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: Projektwechsel über alle vier Wege mit offenen Blazor-Seiten, Löschen mit Kaskade,
> Speichern unter (Fortschritt, Abbrechen, Dublette „Muster" neben „Musterprojekt"), **Export→Import-Rundreise mit
> Variantenpaket und Sicherung samt Kennzahlenvergleich** (der Import war bis hierher unbenutzbar), Assistent vor und
> zurück, de/en, 125 %. Der siebzehnte iOS-Lauf (33867643966) auf diesem Stand ist grün.
> **Windows-Abnahme 05.09.2026 (PDF S. 4), Befund W15a‑B‑1 (`974c198`, Protokoll § 13):** in „Speichern unter"
> lag die Spalte „Geändert" hinter dem waagerechten Rollbalken — der Umbruch der zwei Spalten kam erst bei 780 px,
> und die Hausregel `white-space: nowrap` trieb die Tabelle über die Spaltenbreite. Der Umbruch kommt jetzt bei
> 1 100 px (Liste über die volle Breite, Formular darunter), Name und Kunde brechen um, das Datum bleibt einzeilig
> mit fester Breite und kulturabhängigem Kurzformat. `ProjektWahlDialog` war nicht betroffen, `ProjektTransferDialog`
> führt keine Projektliste (W15a‑O‑2).
>
> **Anwenderwunsch W15a‑E‑1 vom 05.09.2026 (zwei Bildschirmfotos: „Projekt öffnen: Es sollte wie zuvor kenntlich
> sein, welches Variantenprojekte sind"), umgesetzt in `325a275`:** Als eigene Spalte gab es die Variante im Vorbild
> nie — weder `ProjektAuswahl` (418 Z.) noch `Form_ProjektAuswahl` (99 Z.) führten das Wort; kenntlich war sie **am
> Namen** (`VariantenCtrl.AnlegenAusStamm` bildet „‹Stamm› - ‹Bezeichner›", `Form_Start.FuelleVariantenCombo` zeigte
> genau das, die Ordnung kam aus `VariantenCtrl.LadeGruppe`: Stamm zuerst, dann Varianten `ORDER BY Variantenname`).
> Das trug nicht mehr, weil das Assistentenband 280 px breit ist und ausgerechnet der Teil abgeschnitten wurde, der
> die Variante ausmacht. Jetzt trägt `ProjektKopfZeile` Stamm-Id, Bezeichner und Stammnamen (`IstVariante`) aus
> **einer** Abfrage mit zwei LEFT JOINs in `ProjektCtrl.NamenListe` (fehlt `Tab_Variante`, läuft die alte Abfrage —
> ein LEFT JOIN auf eine fehlende Tabelle hätte die ganze Liste geleert); der Baustein `ProjektListe` gruppiert
> Stamm → Varianten nach Bezeichner wie `LadeGruppe` (auch unter Datumssortierung, Stamm-Ausfall und Ringketten
> abgesichert), zeigt die Spalte „Art" **nur, wenn die Liste eine Variante führt**, sonst die leise Zeile „Variante
> von …" mit Einrückung, und die Suche greift über den Bezeichner. Aufrufer: `ProjektWahlDialog`, `ProjektKopieDialog`,
> `AssistentSeite` und die drei Hüllen, vier neue Textschlüssel de/en; `Startseite.Varianten` war schon gekennzeichnet
> und bleibt, `ProjektTransferDialog` führt keine `ProjektListe`. Dabei ist der Befund **W15a‑B‑1** erst wirklich
> behoben: Die Umbruchregel stand im Blatt und **wirkte nicht** — `.epos-raster td` (0,1,1) schlug
> `.epos-projektliste-name` (0,1,0); sie trägt jetzt den Tabellenselektor davor. Elf neue bunit-Fälle, ein Kern-Fall
> gegen `Tab_Variante`; Protokoll § 14, Abnahmepunkt A‑W15a‑E‑1.
>
> **Formularraster, Paket P3 (iU8‑E‑2, 05.09.2026, `d3fccf1`):** `ProjektTransferDialog` (drei Raster, einspaltig) und `ProjektKopieDialog` (die vier Felder
> rechts neben der Liste, Beschriftung daneben).

## Statusblock iU9 — Welle 14c umgesetzt (04.09.2026, Basis 4e77221 nach W14a/W14b, zusammengeführt mit 809fe41)

> **Statusblock iU9 — Welle 14c umgesetzt (04.09.2026, Basis `4e77221` nach W14a/W14b, zusammengeführt mit `809fe41`)**
>
> **Fünf Masken — 2 198 Zeilen `.cs`, 1 425 Designer, 26 `MessageBox` + 2 indirekte — sind fünf Razor-Komponenten in
> vier Fenstern:** `GesetzeskatalogDialog` mit `GesetzeskatalogZeileDialog` als Überlagerung (`EPOS.UI/Dialoge/Admin/`;
> aus dem Kostendialog und dem Wirtschaftlichkeits-Parameterdialog erscheint der Katalog jetzt IM Dialog, E‑1),
> `KatalogDublettenDialog` über dem neuen Baustein **`Baumansicht`** (der einzige `TreeView` des Bestands; 14 bunit-Fälle:
> Rollen und Ebenen, Dreieck klappt ohne zu wählen, ein `tabindex`, die vier Pfeiltasten, Auswahl überlebt den Neuaufbau),
> `EinstellungenDialog` (`EinstellungenCtrl` im Kern über `Dienste.Pfade`/`Dienste.Einstellungen`, vier Rubriken als
> `Reiter`) und `KlimadatenDialog` (seit E‑3 wieder so benannt; `KlimaregionStammCtrl` in den Kern gezogen, `KlimaImportAblauf` mit Abbruch, die zwei
> Klimabilder im Renderer → **32 Proben**). **Vier der fünf Fachteile lagen schon im Kern** (`GesetzKatalog`,
> `DublettenPruefung`/`KatalogBereinigung`/`KatalogRegistry`, `SolarPVGISCalculator`) — die Vorarbeit war Zuschnitt, kein
> neuer Rechenweg; der Nachweis (`KatalogpflegeTests`, 104 Fälle über eine Arbeitskopie) entstand vor der ersten Maske,
> weil es für acht Kern-Klassen **keinen einzigen Test** gab (B62). Mit der Welle fallen die **letzten zwei ablösbaren
> `Sprungziel`-Zweige** (`Sprungbruecke` führt nur noch `SpeicherOptimierung`, bis W16 — R‑W14c‑11), `ChartManager`
> (560 Z., **die MS-Chart-Bindung endet**), `RoundedPanel` und **alle sechs WFO1000** der Mappe (Warnungen 12 → 6, Rest
> Altbestand). Neun Sachcommits, ein Merge und ein Gate-Nachtrag (`8ee59d7` … `c1b049e`), auf `ios_migration` als
> `f7e2758`; 80 Textschlüssel de/en für zwei nie lokalisierte Masken.
>
> **17 Angleichungen, zwei hingenommene Abweichungen** (A‑1…A‑17): `Rueckfrage.VorgabeNein` für sechs Löschfragen,
> der Klimaimport lässt sich abbrechen, Klimaregion löschen fragt **und räumt die 8 760 + 365 Datenzeilen ab** (der alte
> Weg ließ Waisen), die Dublettenprüfung des Imports fragt die Datenbank statt der Präfixsuche, **ein** PVGIS-Abruf statt
> vier (kein gespeichertes Byte ändert sich), „Standardwerte" setzt den Datenbanknamen ins richtige Feld (B53, der einzige
> Rechenfehler), Dublettenscan im Hintergrund mit Fortschritt; hingenommen: Legende in den Klimabildern, kein Mausrad-Zoom.
> Sechs Befunde wörtlich trotz Befund (B3, B5, B8, B16, B30, B39). **Anwenderfragen (entschieden am 04.09.2026, umgesetzt in
> `e86eff6`/`766f349`/`1fbffd2`/`24c8912`, gemerged als `a0e6707`):** E‑3 (Klimaregion = die deutschen Regionen der
> Klimazonenkarte, Klimadaten = der weltweite TMY-Download: die Komponente heißt wieder `KlimadatenDialog`, der Menütext
> bleibt „Klimadaten"), E‑5 (ohne Ordnerwähler sind die fünf Pfade fest und **nur lesend**, Hinweistext de/en — die
> iOS-Sandbox), E‑6 („Altbereinigung ausführen": **Schema-Schritt 62** räumt Waisen in `Tab_Solar_STAMM` und
> `Tab_Klimadaten_STAMM` ab, `ZIEL_VERSION` 62 und neu `FREEZE_VERSION` 61, weil Freeze- und Zielstand bis dahin dieselbe
> Konstante waren; auf `Kenndaten_Test.sqlite` ein Leerlauf — 32 Regionen × 8 760 und × 365 exakt; Projektpakete mit
> Schemastand 61 werden nach Regel B2 abgewiesen; die Datenblöcke tragen ohnehin `ON DELETE CASCADE`, der Schritt ist ein
> Netz für Altbestände), E‑7 (keine Ortsliste in der Auslieferung; Katalognamen scheiden als Vorschlag aus, weil der
> Ortsname zugleich der Regionsname wird und A‑9 vergebene Namen abweist — Variante (c) bleibt), E‑8 (Hinweis:
> ohne WebView2-Laufzeit bleiben die letzten vier Admin-Masken leer). **Testanker:** `Form_Klimadaten` als Prüfmuster
> `Pruefmuster/Klimadaten/` (fünf Anker und der `Chart`-Typzeuge), drei Anker auf `MDIMainForm` (fällt als letzte, W16);
> Schwellen 20 / 17 / 11 / 17. Abweichung von der Vermessung benannt: der Test zählt 20 Designer-Dateien repoweit
> (18 + 2 generierte des Kerns).
>
> **Nachweise** (auf dem gemergten Stand `f7e2758`, Linux): Build → 0 Fehler, **6** Warnungen (12 nach W14a) ·
> `dotnet test WP-Plan.Kern.slnf` → **3 430** grün (3 227 nach W14a), **identisch unter `LC_ALL=en_US.UTF-8`** ·
> Formularkarte **124** grün · Stapellauf **17** Masken (21 − 4; `Form_KatalogDubletten` war eine Code-Form ohne
> Designer), 18 Designer, 17 erreichbar, **0 nein / 0 verwaist / 0 unklar** · SQL-Prüfer 1 232 Texte, 0 Fundstellen ·
> ChartProben **32** Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte) · beide
> Wächter leer.
>
> **Protokoll** mit Feldkartenabgleich je Maske, der WFO1000-Bilanz, 17 Abweichungen, den Befunden B1…B64, der Zählung
> zu E‑6 und 13 Abnahmepunkten: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W14c_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: Katalog aus beiden Razor-Aufrufern mit Esc-Ebenen, Klimaimport einmal echt gegen PVGIS
> vorher/nachher zahlengleich, Abbrechen, die zwei Bilder gegen den Bestand, Löschen mit Kaskade (`SELECT COUNT(*)`),
> Dublettenscan mit Fortschritt und Baum per Tastatur, Einstellungen speichern/zurücksetzen, KI-Schalter mit
> Maschinenriegel, Reihenfolge des Administrationsmenüs (B63), de/en, 125 %, fehlende Ortsliste. Der sechzehnte
> iOS-Lauf (33861268537) auf diesem Stand ist grün.
>
>
> **Anwenderwunsch W14c‑E‑9 vom 05.09.2026 (Admin-Dialoge an die Bildschirmgröße) mit Befund W14c‑B‑16, umgesetzt in
> `ddf4d00`:** Die drei Hüllklassen `epos-klimaregion-*` des `KlimadatenDialog` standen nie im Stilblatt — die
> „zwei Spalten" lagen deshalb untereinander; ersetzt durch den `Katalograhmen` (Liste links, Diagramme und Import
> rechts). Der `GesetzeskatalogDialog` gibt seiner einen Liste die volle Höhe (`epos-katalog-fuellend`, wie die
> ListView 916×424 in `Form_Gesetzesparameter`), der Zeileneditor bleibt Überlagerung; Dubletten und Einstellungen
> gewinnen allein durch das größere Fenster. Protokoll ergänzt.
>
> **#169 (10.09.2026) — `KlimadatenDialogTests` deterministisch (W16b‑O‑2).** Im Gate zu #167 fiel
> `Der_Fortschritt_meldet_die_Schritte_und_laesst_sich_abbrechen` (1 von 3 360): Zeile 357 prüfte den Abbruchzähler
> unmittelbar nach dem synchronen `Click()`; der Abbruch läuft in `Bausteine/Fortschritt.razor` über
> `EventCallback.InvokeAsync` auf dem Renderer-Dispatcher, bunits `Click()` wartet darauf nicht (derselbe Wettlauf wie in
> `ProjektTransferDialogTests`, „5 von 12 Läufen"). Allein und in Wiederholungen grün, im vollen Lauf mit zwei Threads
> nicht. Fix (`8a7ff0a`, nur Testcode): zehn Sofort-Asserts nach `.Click()`/`.Input()` derselben Klasse (Zähler, Markup,
> Instanzzustand) auf `WaitForAssertion`/`WaitForState` (10 s) umgestellt; Abwesenheitsprüfungen bleiben. Nachweis: Klasse
> zehnmal 16/16, Projektlauf 3 360 grün. Gate auf `a86d30e`: Kern 2 277 grün, UI 3 360 grün, Referenzlauf byte-gleich; Warnungsschranke
> weiter allein durch die vorbestehende CS8602 (`RasterTests.cs:335`) gerissen.

## Statusblock iU9 — Welle 14a umgesetzt (04.09.2026, Basis 01c9933 nach W13, zusammengeführt mit c9855b1 nach W14b)

> **Statusblock iU9 — Welle 14a umgesetzt (04.09.2026, Basis `01c9933` nach W13, zusammengeführt mit `c9855b1` nach W14b)**
>
> **Sieben Masken — 2 387 Zeilen `.cs`, 2 369 Designer, 39 `MessageBox` + 32 indirekte — sind drei Razor-Komponenten:**
> `KatalogBrowserDialog` (`EPOS.UI/Dialoge/Erzeuger/`) mit **vier Ausprägungen** über `KatalogBrowserProfil` (Heizkessel,
> BHKW, Solarkollektoren, Pufferspeicher mit `NurLesen`) — vier Masken waren Behälter um Editoren, die seit W6/W7 Razor
> sind —, `PufferSpKatalogDialog` (**der fehlende vierte Katalogeditor**, als Überlagerung im Browser) und
> `ModulKatalogDialog` mit zwei Ausprägungen (Stromspeicher als Vorbild, Photovoltaik bekommt dessen gepflegte Bauart).
> Im Kern: `KatalogZeilen`/`KatalogsatzAnzeige`/`SpeichernAus` je Katalog, die Speichertyp-Abbildung, `ModulKatalogProfil`,
> `DbWerte.SP_TYP_LITHIUM_IONEN`, `StromspeicherModel.C_VER_VORGABE` — und **`HeizkesselStammCtrl.Filtern` berichtigt**
> (W14‑B2: der Kern trug die Brennstoffkette der mit W6.3 gelöschten Maske; Fernwärme, Sonstige Energieträger und
> Wasserstoff filterten in **beiden** Heizkesseldialogen nicht — Vorher/Nachher-Zählung je Gruppe im Protokoll). Mit der
> Welle fallen die **letzten fünf ablösbaren `Sprungziel`-Zweige** (ihre Aufrufer sind Razor → Überlagerungen),
> `Views/Pufferspeicher/PufferSpFilter.cs`, `Allgemein/SpeichernLeiste.cs` und `Allgemein/KI/KiAufrufKnopf.cs` (E‑10:
> der KI-Einstieg aus einer Maske kommt mit W15b zurück). **Der Erreichbarkeitsbefund steht erstmals auf 0 nein /
> 0 verwaist / 0 unklar** — `Form_PufferSp_Bearbeiten` und `Form_SolarKollektorenAdmin` sind als Prüfmuster eingefroren
> (der „unklar"-Zeuge und der `DataGridView`-Typzeuge). Elf Sachcommits und ein Merge (`5fdbb4b` … `e5f387c`), auf
> `ios_migration` als `4e77221`. Der Nachweis (50 eingefrorene Fälle `KatalogVerwaltungTests`) entstand vor der ersten
> Maske; 97 Textschlüssel de/en für zwei nie lokalisierte Masken.
>
> **16 Abweichungen** (A‑1…A‑16), u. a.: „OK" liefert OK, ein Löschtext mit Namen, die achte BHKW-Leistungsstufe trifft
> (79 statt 8 Treffer vorher), `Exists`-Vorabtest überall, Löschen mit Rückfrage auch bei PV, der echte Löschgrund statt
> „Projektzuordnung", keine modale Prüfung beim Feldverlassen, der Kontextmenüweg des Stromspeichers öffnet den Katalog,
> Hersteller- und Speicherlisten sortiert. **Anwenderfragen:** E‑2 (Aperturfläche im Feld „Kollektorfläche" — wörtlich),
> E‑9 (je zwei Menüpunkte für Heizkessel und Pufferspeicher — beide behalten), **E‑11 neu** (die Solarkollektoren-Verwaltung
> hat zwei Flächenfelder, „Kollektorfläche" wird nirgends gefüllt — Modulfläche zeigen oder Feld streichen?).
>
> **Nachweise** (auf dem gemergten Stand `4e77221`, Linux): Build → 0 Fehler, **12** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **3 227** grün (3 087 nach W14b), **identisch unter `LC_ALL=en_US.UTF-8`** ·
> Formularkarte **124** grün · Stapellauf **21** Masken (28 − 7), 21 erreichbar, **0 nein / 0 verwaist / 0 unklar** ·
> SQL-Prüfer 1 232 Texte, 0 Fundstellen · ChartProben 30 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017
> **PASS, byte-gleich** (815 043 Werte).
>
> **Protokoll** mit Feldkartenabgleich je Ausprägung, der Brennstoffzählung, 16 Abweichungen, den Befunden B1…B79 und
> 13 Abnahmepunkten: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W14a_Blazor_Port_Protokoll.md`. **Windows-Abnahme
> steht aus**: die Brennstoffkette in beiden Heizkesseldialogen, die achte BHKW-Stufe, Löschrückfrage PV, `NurLesen` des
> Pufferspeicherbrowsers, Kontextmenüweg Stromspeicher, die Überlagerungen statt Sprüngen, de/en, 125 %. Der fünfzehnte
> iOS-Lauf (33852944072) auf diesem Stand ist grün.
> **Zum Anwenderentscheid #76 vom 05.09.2026 geprüft und nicht betroffen (`b6fd863`):** der
> `KatalogBrowserDialog` (vier Ausprägungen) führt eine Liste plus Detailblock, kein Projekt↔DB-Paar; die vier
> Projektdialoge, die er als Sprungziel bedient, sind über Welle 6 auf `Zweispaltenauswahl` umgestellt.
>
>
> **Anwenderwunsch W14a‑E‑6 vom 05.09.2026 (Bildschirmfoto „Administration Solarkollektoren": Fenster klein, Liste
> und Eingabe untereinander mit Seitenrollbalken, Kopfzeile „Name | Name"), umgesetzt in `ddf4d00`:**
> `KatalogBrowserDialog` (vier Ausprägungen) und `ModulKatalogDialog` (zwei) stellen Liste und Eingabe wieder
> nebeneinander wie ihre sechs Vorbilder (`Form_Heizkessel_Admin` 726×383 bis `Form_BHKWAdmin` 856×517) — über den
> neuen Baustein `EPOS.UI/Bausteine/Katalograhmen.razor` (Liste links mit Filter, Detailblock rechts, Umbruch
> untereinander unter 900 CSS‑px = `--epos-zweispalten-umbruch`, die Liste rollt in sich; die Höchsthöhe aus W9‑B‑2
> fällt nur im Rahmen). Die Hülle `BlazorDialogForm` öffnet jeden `Fachdialog` im Anteil des Arbeitsbereichs
> (85 % × 90 %, gedeckelt auf 92 %; Rechnung plattformfrei in `EPOS.UI/Dienste/Fenstermass.cs`, `FenstermassTests`
> 11 Fälle); fünf kleine Masken tragen `Dialogart.Klein` und bleiben, wie sie waren. Die Wahlspalte heißt wieder
> „Wahl" — Ursache waren die zwei Hüllen (`KatalogBrowserHuelle` gab `SpalteName`, `ModulKatalogHuelle`
> `Listenbeschriftung`), beide lesen jetzt `KFAK_SP_WAHL`. Die Katalogwurzel rollt, statt die Schlussleiste
> abzuschneiden. 34 neue bunit-Fälle (`KatalograhmenTests`, `KatalogdialogTests`, `FenstermassTests`); Protokoll
> ergänzt.
>
> **Anwenderwunsch iU8‑E‑2 / W14a‑E‑7 vom 05.09.2026 (Bildschirmfoto „Administration Photovoltaik Module":
> „Verbessere die Darstellung der Dialoge, insbesondere der Parameter auf der rechten Seite: kompakter,
> übersichtlicher"), umgesetzt in `6ab0b9f`:** Im `Katalograhmen` aus W14a‑E‑6 nahm rechts jedes Feld die volle Breite,
> die Beschriftung stand darüber, die Zahlenfelder waren so breit wie die Textfelder und die Einheit stand am rechten
> Rand des Blocks — der Block war doppelt so hoch wie der Dialog und rollte. Gemessen an `Form_AdminPV.resx`
> (607 × 489) tat das Vorbild es anders: Beschriftungsspalte 178 px, Zahlenfeld 62 px, Einheit 4 px dahinter. Die
> Antwort ist eine **hausweite Regel und kein Sonderfall**: der Baustein `EPOS.UI/Bausteine/Formularraster.razor`
> (Beschriftung neben dem Feld in einer 12-rem-Spalte, `repeat(auto-fill, minmax(--epos-formularspalte, 1fr))`
> nach der Breite des Rasters — die rechte Spalte eines Katalograhmens liegt damit genauso richtig wie ein
> freistehender Dialog —, `Einspaltig` als benannter Rückweg) mit `Formulargruppe.razor` als leiser
> Zwischenüberschrift (`display: contents`, damit die Felder direkte Rasterkinder bleiben), dazu zwei Klassen, mit
> denen ein Feld seine Länge selbst meldet (`Zahlenfeld`/`Ganzzahlfeld` kurz mit der Einheit unmittelbar dahinter,
> mehrzeiliges `Textfeld` breit); unter 900 CSS‑px fällt die Beschriftung wieder über das Feld, `--epos-touchziel`
> bleibt die Mindesthöhe, und die Regel greift nur innerhalb von `.epos-formularraster` — ein Dialog hängt seinen
> vorhandenen Feldlauf hinein, mehr nicht. Acht Dialoge tragen die neue Form (`ModulKatalogDialog`,
> `KatalogBrowserDialog` ×4, `PufferSpKatalogDialog`, `BedarfAdminDialog`, `WaermebedarfAdminDialog`, dazu die
> Stichproben `HeizkesselDialog`, `GebaeudeDialog`, `EinstellungenDialog` — je eine Zeile). Die Bestandsaufnahme
> aller 92 Dateien mit 624 Feldbausteinen (41 Klasse A: reines Einhängen; 43 Klasse B: Handarbeit) und der
> Vorschlag für drei Restpakete stehen im Protokoll W14a; die Restumstellung läuft als Aufgabe #91 (Pakete P1
> Erzeuger W6/W7/W14a, P2 Kosten W1–W5, P3 Bedarf/Simulation/Projekt W8–W16a). `FormularrasterTests` 14 Fälle
> (darunter einer, der jede Selektorzeile des Blocks auf `.epos-formularraster` prüft), UI 2 546, Formularkarte
> 122.
>
> **Anwenderwunsch W14a‑E‑8 vom 06.09.2026 („Für alle Menüs mit Anlagendaten: Erstelle einen Bearbeiten-Dialog zusätzlich
> im Bearbeiten-Menü (optionale Anzeige) …"), umgesetzt in `e7c5f63`:** Unter dem Bearbeiten-Formular jeder der sieben
> Anlagenverwaltungen steht seither ein Aufklapper „Alle Parameter und ihre Verwendung anzeigen" — zugeklappt die
> Vorgabe, aufgeklappt je Katalogspalte eine Zeile mit Anzeigetext, Wert samt Einheit („–" bei NULL) und der
> Kennzeichnung, wofür der Wert gebraucht wird. Die Antwort darauf ist eine Fachaussage über den Rechenweg und liegt
> deshalb im Kern: `ParameterVerwendung` nennt für jede der 128 Spalten der sieben Stammtabellen ihre Stufe — 56
> Simulation, 33 Wirtschaftlichkeit, 37 Bericht, 36 nur Anzeige, 5 ohne jeden Leser — und belegt jede Einstufung mit
> Datei und Zeile; die Beschriftungen holt sie über dieselben Ressourcenschlüssel wie `KatalogBrowserProfil` und
> `ModulKatalogProfil`. Drei Befunde stehen damit schwarz auf weiß: Die fünf Emissionsspalten des Heizkessels werden
> gepflegt, aber nicht gerechnet (der Lauf holt die Faktoren aus `Tab_Brennstoff_Stamm` — beim BHKW ist es
> umgekehrt), fünf Maße der Wärmepumpe aus dem VDI‑3805-Import hat kein Leser, und `Investition_kwel` des BHKW ist
> seit dem Entscheid vom 22.08.2026 eine abgeleitete Dublette. **Die Prüfung, um die der Anwender gebeten hat
> („→ prüfe"), ergibt genau eine Lücke:** `Tab_WP_STAMM.Modulkosten` geht in die Kostenplanung, ist in der
> Verwaltung aber nicht zu pflegen (Entscheid Ä19 der Welle 7) — kein Versehen, sondern ein Widerspruch zwischen zwei
> Entscheiden, offen als **W14a‑O‑1** mit Vorschlag (nur lesend mit Herleitungszeile zeigen). Der Baustein
> `Parameteruebersicht` steht einmal in `EPOS.UI` und bedient alle sieben Ausprägungen in drei Wirten; er nimmt den
> Aufklappknopf aus W6‑E‑1, trägt die Kennzeichnung als Text und nicht nur als Farbe, und seine Tabelle steht in
> `.epos-raster-huelle`. Nachweis: 42 Kern-Fälle gegen `pragma table_info` der Testdatenbank (keine vergessene, keine
> erfundene Spalte, Belegpflicht für jede gerechnete) und 17 bunit-Fälle; Kern 1 344, UI 2 721 grün in beiden
> Sprachen, SQL-Prüfer 0 Fundstellen, Rechenweg unberührt. Acht Abnahmepunkte A‑W14a‑E‑8 im W14a-Protokoll.
>
> **Anwenderentscheid W14a‑O‑1 vom 06.09.2026 (Empfehlung), umgesetzt in `60db6ff`:** `Tab_WP_STAMM.Modulkosten` steht
> im Wärmepumpen-Stammdialog wieder da — als **Lesewert** im Formularraster mit „€" dahinter und einer Herleitungszeile
> „aus dem Herstellerimport; Gerätekosten werden in der Kostenverwaltung gepflegt", bei fehlendem Wert „–" und „kein
> Planwert aus dem Herstellerimport". **Ä19 bleibt gewahrt:** kein Eingabefeld, kein Schreibweg, kein gesperrtes Feld;
> der Wert wird wie bisher nur durchgereicht, Rechenweg und SQL sind unberührt. Die Einheit ist ein Betrag **je Gerät**
> (`TechnikPlanwertCtrl`, Basis „Modulpreis" unverändert — anders als PV und Stromspeicher), Beschriftung und Ziffern
> sind wortgleich zum Aufklapper der Parameterübersicht. **Dabei aufgefallen und offen als W14a‑O‑2:** Kein Importweg
> schreibt diese Spalte (`KatalogImportSatz.NachStamm` setzt sie nie, `UpdateImport` lässt sie stehen) — für ein neu
> importiertes Gerät bleibt sie dauerhaft 0, und der beschlossene Wortlaut „aus dem Herstellerimport" beschreibt einen
> Weg, den es nicht gibt; drei Wege mit Empfehlung (a: Wortlaut „aus dem Datenbestand") stehen im W14a-Protokoll,
> Anwenderentscheid. Nachweis: UI 2 731, Kern 1 344 grün in beiden Sprachen (Basis des Agenten), Designer „abweichend
> 0", SQL-Prüfer 0 Fundstellen, Gate grün, Referenzlauf byte-gleich; Abnahmepunkt A‑W14a‑E‑8‑9.
> **Nachtrag W14a‑O‑2, entschieden 06.09.2026 (Weg a, Empfehlung):** Die Herleitungszeile sagt seither „aus dem
> Datenbestand; Gerätekosten werden in der Kostenverwaltung gepflegt" (en „from the stored catalogue; …"), der Leerhinweis
> „kein Planwert im Datenbestand"; Ä19 unverändert, Weg (b) nicht beauftragt. 33 Dialogfälle grün in de und en.
>
> **W14a‑E‑8‑B3 (Anwenderentscheid 07.09.2026: „Entweder Investitionskosten als Summe/Gesamt oder Investitionskosten auf
> kW elektrisch × Kosten pro kW elektrisch — umgerechnet je nach Eingabe"), umgesetzt in `e109cca`, zusammengeführt in
> `a6230e6`:** Der BHKW-Katalogeditor nimmt die Investition auf drei Wegen entgegen — Gesamtsumme [€], Wert je kW
> elektrisch [€/kW], fünf Einzelposten; die zuletzt geänderte Eingabe führt, das jeweils andere Feld folgt. Die
> Umrechnung steht im Kern und nur dort (`BHKWKosten`: `Gesamt`, `JeKWel`/`JeKWelEingabe`, `GesamtAusJeKWel`,
> `ModulAusGesamt`, `Nebenposten`, `NebenpostenUeberschreiten`; Cent für Eurobeträge, 1 €/kW für die Anzeige).
> **Der Ausgleich läuft immer über `Kosten_Modul`**, die vier Nebenposten bleiben stehen — `TechnikPlanwertCtrl`
> rechnet unverändert mit den fünf Posten, kein sechster Betrag, Referenzlauf byte-gleich. `Investition_kwel` bleibt die
> Ableitung und wird vor jedem Schreibvorgang nachgezogen: aus der Dublette (Befund B3) ist die Anzeige eines Eingabewegs
> geworden. Herleitungszeile mit vier Zuständen (führend mit Rechnung „50 000 € / 40 kW = 1 250 €/kW", unbestimmt bei
> P_el = 0 → Feld gesperrt, Abweichung, gedeckelt bei Nebenposten > Gesamt → Modul 0,00, das getippte Feld springt nicht
> zurück); die drei Funktionsparameter `Summe`/`JeKWelBestimmbar`/`JeKWel` des Dialogs sind entfallen, er ruft den Kern
> unmittelbar. **B2 („Empfehlung"): keine Programmarbeit** — die fünf Maßspalten der Wärmepumpe bleiben als
> Referenzdaten, gekennzeichnet „ohne Leser". Nachweis: 16 neue Kern-Fälle (`BhkwKostenTests`), 25 bunit-Fälle,
> Kern 1724 / UI 3064 grün, Designer „abweichend 0", SQL 0, Gate grün, Referenzlauf 1030/1007/1017/1045 byte-gleich.
> Doku: `Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md` § 4.7, W14a-Protokoll B3 geschlossen, `BHKW.wiki`. Abnahme auf
> Windows: A‑W14a‑E8‑B3‑1…8.
>
> **W14a‑E‑8‑B1 und W11a‑O‑2 (Anwenderentscheid 07.09.2026: „Es soll der gepflegte CO₂-Wert herangezogen werden — der
> an dem Energieträger hängt, gilt generell für alle Erzeuger"), umgesetzt in `582a803`, zusammengeführt in `47ffe37`:**
> Kessel und BHKW lasen zwei eigene Quellen (Brennstofftabelle bzw. fünf Gerätespalten in g/MWh), die Emissionsbilanz
> der Wirtschaftlichkeit eine dritte (Emissionskatalog des Energieträgers). Beide Erzeuger lesen jetzt den
> Emissionskatalog über den neuen Kern-Dienst `Emissionsquelle` — Kette Projekt → Katalog → Stamm → Carrier wie
> Konzept § 3, im Berechnungsmodus des Projekts (CO₂ oder CO₂e), mit einem fünften Glied `BRENNSTOFF` für Anlagen ohne
> `ID_Carrier` (Protokollhinweis im Lauf). Die zehn Gerätespalten von Kessel und BHKW sind „nur Anzeige"
> (`ParameterVerwendung`, Herleitungszeile in beiden Katalogeditoren). **W11a‑O‑2 (#45) geschlossen:** Die
> Autarkie-Kachel nimmt Netzstrom- und Wärmeträger des Projekts statt der Literale 0,42/0,20 kg/kWh; der
> Netzstrom-Rückfall 435 g/kWh steht einmal (`Emissionsquelle.StromTraeger`), auch für `KostenEmissionRechner`.
> **B2 („Empfehlung"): keine Programmarbeit.** Zwei Lücken benannt statt gefüllt: keine Emissionsart „CO" (CO seither
> auch beim BHKW 0, Saatvorschlag § 9.9) und Staub im Auslieferungsstand abgewählt (Rückfall `Tab_Brennstoff_Stamm.Staub`
> außerhalb der Zeilenliste, damit die CO₂e-Summe nicht kippt). **Wirkung:** BHKW-CO₂ ändert sich erheblich (1030:
> 0 → 251,59 t/a, 1024: 45,65 → 70,76 t/a, 1017: 0 → 21,62 t/a), Kessel nirgends (Projektübersteuerung 240 g/kWh
> zahlengleich zum Stammwert). **Harter Befund, der den Auftrag umkehrt:** Die Emissionsgrößen der Simulation
> (`Em_CO2_SPK`, `Em_CO2_BHKW` …) haben repo-weit keinen Leser außerhalb ihrer Klasse, keine `Tab_Ergebnis*`-Spalte
> und keine Referenz-CSV trägt eine Emissionsgröße — **Referenzlauf 12/12 byte-gleich gegen R3 (3 313 002 Werte
> PASS), R3 bleibt die Basis; eine R4 wäre eine Kopie ohne Aussage** (offener Punkt § 9.8: `Em_CO2_*` in den Export).
> Der Nachweis hängt an `EmissionsquelleTests` (10 Fälle: Kette, Staub- und Brennstoff-Rückfall, Modus CO₂e mit
> CH₄-Saat 240 → 269,8 g/kWh, Gegenprobe Gerätespalten 999 999 → Ergebnis unverändert, beide Autarkie-Seiten).
> Nachweis: Kern 1744 / UI 3064 grün, Designer „abweichend 0", SQL 0, Gate grün. Doku: Emissionsarten-Konzept § 8
> Entscheide, W14a-Protokoll B1–B3, `Heizkessel.wiki`/`BHKW.wiki`, `EPOS.Kern/CLAUDE.md`, `Referenzlaeufe/LIESMICH.md`.
> Abnahme auf Windows: A‑W14a‑E8‑1…7 — darunter A‑W14a‑E8‑5 (die CO₂-Ersparnis der Autarkie-Kachel bewegt sich mit
> dem Katalogfaktor des Stromträgers) und A‑W14a‑E8‑6 (Modus CO₂-Äquivalent hebt Kessel- und BHKW-Faktor).
>
> **W14a‑E‑9 (Anwenderwunsch 07.09.2026: „Die Dialoge unter Administration → Energiesysteme / Wärmebedarf & Heizung /
> Strombedarf & Speicher sollen eine Auswahl/Suche erhalten, um die wichtigen Parameter der Komponenten bei der Auswahl
> eingrenzen zu können. Gebe einen Vorschlag. Evtl. auch anderes Design der Dialogbox."), Konzept und Mockup in `36c0c27`,
> zusammengeführt in `0748d94` — nichts umgesetzt:** Befund über die 15 Menüpunkte der drei Köpfe (14 Katalogverwaltungen,
> eine Rechenmaske): **zehn von vierzehn** Verwaltungen haben keinen Filter, **zwölf von vierzehn** Listen zeigen nur den
> Namen, die drei größten Kataloge (PV 20 743 Module, Stromspeicher 6 658, Wechselrichter 2 343 nach Import) haben den
> schwächsten Filter; der einzige vollständige Filter sitzt in der Wärmepumpen-Überlagerung statt über der Stammliste; der
> PV-Katalog hat im Projektdialog einen Herstellerfilter, in der Verwaltung nicht. Drei Datenbefunde: Brennwert-Kennzeichen
> bei 6 von 63 Kesseln gepflegt (46 Beschreibungen nennen es), `Wirkungsgrad_Gas` in zwei Einheiten, `Tab_Stromspeicher_STAMM`
> ohne `Firma`. **Vorschlag:** EIN Baustein `Filterleiste` mit einem `Katalogfilterprofil` je Katalog im Kern — der vierte
> Zwilling zu `KatalogImportProfil`/`KatalogBrowserProfil`/`ModulKatalogProfil`, damit Import, Verwaltung und Projektauswahl
> eine Mechanik haben (Klapplisten, Suche mit `*`-Mustern, zwei Zahlenbereiche, „Weitere Filter", Chips, Trefferzähler,
> Sortierung über Spaltenköpfe, Zustand je Sitzung) — und ein Dialogbild in **drei Zonen** (Filterleiste über die Breite,
> Liste mit drei bis fünf sortierbaren Parameterspalten links und Detail rechts, Aktionsleiste unten; Umbruch bei 900 px;
> im Projektdialog steht die Leiste in der Katalogspalte der `Zweispaltenauswahl`). Abgelehnt mit Begründung: Kartenansicht,
> Facettenleiste links, Spaltenkopf-Filter, „nur mehr Spalten". Empfohlene Filterzeile je Katalog in Kapitel 4 (z. B.
> Wärmepumpe: Hersteller · Quelle · Auslegung, Nennleistung · max. Vorlauf, Spalte COP A2/W35). Papier
> `Konzept_Katalogfilter_EPOS-Plan.md` (772 Zeilen), Mockup `Mockups/Katalogfilter_Vorschlag.html` (drei Reiter mit echten
> Namen und gemessenen Trefferzahlen 15/63, 7/51, 15/20 749). Stufenplan S1 (Baustein, Profile, beide Verwaltungskomponenten,
> 12–16 h) → S2 (elf Projektdialoge, Assistent, „im Projekt verwendet", 8–12 h) → S3 (Bedarfs-/Zeitreihenkataloge, Vergleich,
> Importmasken, 8–10 h), Referenzlauf in allen Stufen unberührt. **Offen: Q1…Q12** (Parameter je Katalog; Zustand je
> Sitzung; Vergleich in S3; Zonenmodell; Bereiche ergänzen Stufen; Speicher bis 5 000 Zeilen, darüber SQL; Spalte `Firma`
> als Schemaschritt; Brennwert-Daten berichtigen; Wirkungsgrad > 2 als Prozent; Leiste auch für Importmasken in S3.4;
> Gebäude/Klima/Kosten nicht; Spalte „im Projekt verwendet" in S2.3).
>
> **W14a‑E‑8‑B1, Nachzug (Anwenderwunsch 07.09.2026: „Emissionspunkte § 9.8 und § 9.9: betrachte das Konzept und mache
> einen Vorschlag zur Umsetzung im jetzigen Zustand"), Vorschlag in `2ae5658`, zusammengeführt in `1a1fbbb` — keine
> Umsetzung:** Das Emissionskonzept bekommt **Kapitel 11** (487 Z.) mit dem gemessenen Ist-Stand — die zehn `Em_*`-Felder
> haben repo-weit EINEN Leser (`EmissionsquelleTests`), keine der 17 `Tab_Ergebnis*` führt eine Emissionsspalte,
> `aggregate.csv` heute 1 722 Skalare über zwölf Projekte —, dem Vorschlag für § 9.8 (**zehn Skalare** `Em.Kessel.*`/
> `Em.Bhkw.*` mit Einheit im Namen, +70 Schlüssel = +4,1 %, zehn von zwölf Projekten mit Werten ≠ 0, 1007 und 1008 ohne;
> ein neuer Schlüssel ist im Vergleich ein FAIL, also neue Basis oder `--ohne`) und für § 9.9 (Schritt **68** nach Muster
> 67 zugeschnitten). **Zwei Befunde schärfen die offenen Punkte:** Weder GEMIS 5.2 noch die UBA-Liste v2.1 führt **CO**
> (gemessen über alle 15 bzw. 22 Blätter) — Schritt 68 hätte keine Zahl zu säen; und ein Datenschritt allein reichte
> nicht, weil `EmissionsFaktorSatz` kein Feld `Co` hat und die Zuordnung Art → Feld eine feste if/else-Kette über vier
> Kürzel ist (`EmissionsFaktorLader.cs:194‑208`). Die vorhergesagten Exportwerte sind gegen B1 belegt (1030 BHKW
> 251,585 t/a, 1024 70,761, 1017 21,624). **Empfehlungen: § 9.8 als Z1** — dem laufenden `double`-Auftrag W8‑O‑5d
> mitgeben, bevor R4 einfriert (ein Basiswechsel statt zwei, Zahlen sofort in `double`-Genauigkeit); **§ 9.9 warten**,
> bis eine Quelle mit CO-Faktoren je Energieträger vorliegt. **Sieben Anwenderfragen offen:** `Em‑9.8‑Q1…Q4`,
> `Em‑9.9‑Q1…Q3`, je mit Empfehlung in § 11.5. Geändert ist nur das Emissionskonzept; kein Code, keine Migration,
> Referenzlauf und CI unberührt.
>
> **W14a‑E‑10 (Anwenderentscheid 07.09.2026: „Q1 bis Q12: ändere den Katalogfilter — nur Suche, Hersteller, Brennstoff etc.
> sollte an Spalte mit Sortieren und Suchen erfolgen. Das Schema des Dialogs sollte immer gleich aussehen (Wärmepumpe
> ähnlich wie PV-Module und Heizkessel)"), Konzept Rev. 2 und Mockup in `79c2cad`, zusammengeführt in `04acbbb` — nichts
> umgesetzt:** Die Empfehlung **Q4** (Zonenmodell mit Filterleiste, Klapplisten, Bereichsfeldern, Chips) ist abgelehnt,
> die in Rev. 1 verworfene Variante **A4** gewählt. Neues **Kapitel 5.6 „Das Spaltenmodell"**: Zone A trägt nur noch EIN
> Suchfeld über alle Spalten und die immer sichtbare Filter-/Trefferzeile; jede Spalte hat einen Sortierpfeil (auf → ab →
> aus) und einen Trichter mit Popover aus Spaltenname, einem Feld „enthält…" und „Filter löschen"; gefilterte Spalten
> tragen den gefüllten Trichter. Die drei Einwände gegen A4 sind entkräftet: `ColumnOptions`/`Sortable`/`Class`/
> `ShowColumnOptionsAsync` in **QuickGrid 10.0.11** (Fundstellen in 5.6.7), der Optionsknopf ist ein `<button>`,
> `QuickGrid.razor.js` schließt bei Esc und rückt das Popover ins Raster. Gefiltert wird VOR dem Raster (der Controller
> reicht eine eingeschränkte `IQueryable`), weil der `@key`-Fix W6‑B‑2 sonst beim Sprung 20 749 → 15 die alte Liste
> zeigte. **Stufenplan neu:** S1 Spaltenfilter + `Zahlenausdruck` + sieben Profile + Parameterspalten aus sieben
> Controllern + die acht Verwaltungsdialoge (10–14 h) → S2 die sieben Projektdialoge samt Wärmepumpe, deren elf
> Bedienelemente in die Spalten fallen (6–9 h) → S3 Bedarf/Zeitreihen/Vergleich/Import (7–9 h); Summe 23–32 statt
> 28–38 h. **Q5 ersetzt:** die sechs festen Leistungs-/Volumenstufen entfallen; **Q7** (Spalte `Firma` im
> Stromspeicher-Stamm) wird dringlicher. Mockup bedienbar (Trichter, Filter löschen, echtes Sortieren), bei 1 366 px ohne
> waagerechtes Rollen geprüft, Trefferzahlen gemessen 15/63, 7/51, 15/20 749. **Neu offen:** W14a‑E‑10‑Q1 (ein Feld je
> Zahlenspalte mit `>10`/`<60`/`10..60`/`=15`; Empfehlung ja), O‑5 (Katalogliste der `Zweispaltenauswahl` nur halbe
> Breite), O‑6 (ein neu aufgebautes Raster schließt das Popover — vor S1.2 zu entscheiden).
>
> **W14a‑E‑10 Rev. 3 (zweite Rückmeldung des Anwenders 07.09.2026: „Die Filter sollten über den Spaltennamen sitzen …
> und nicht separat, außer ‚Suche' über alle Felder. Liste wie zuvor über ganze Breite, sonst zu schmale Liste."),
> Konzept Rev. 3 und Mockup in `ee7f025`, zusammengeführt in `0b1b4c7` — nichts umgesetzt:** Trichter und Sortierzeichen
> stehen **im Spaltenkopf neben dem Namen** (Vorbild: der Tabellenkopf der Projektanwendung des Anwenders), die
> **Filterzeile fällt** — ein gesetzter Filter ist am gefüllten Trichter erkennbar (gefüllt gegen Umriss, nicht nur
> Farbe), die Trefferzahl steht rechts in der Suchzeile, ein Rücksetzer erscheint nur bei gesetztem Filter. Die **Liste
> läuft über die ganze Breite**, der Eingabeblock darunter im `Formularraster`: sechs (Heizkessel), sieben (PV-Module)
> bzw. neun (Wärmepumpen-Katalog) Parameterspalten statt fünf; gefallen sind nur Spalten mit gemessen leeren Daten
> (Vor-/Rücklauf 0 von 63, Investition 4 von 63, WP-Bauart 45 von 51 leer). Gemessen mit Chromium bei 1 366 und 1 920 px
> (sechs PNG): keine Seite und **keine Liste** rollt waagerecht — **O‑5 gegenstandslos**. Neu: **W14a‑E‑10‑Q2**
> (Projektliste und Katalog untereinander statt nebeneinander, Änderung an #76; Empfehlung „untereinander", vor S2.6 zu
> entscheiden) und **O‑7** (Dialoghöhe 1 152–1 228 px, die Liste ist auf elf Zeilen begrenzt und rollt in sich, der
> Eingabeblock beginnt bei 674 bzw. 825 px). Aufwand: S1 10–14 h unverändert, S2 7–10 h (+1 h Umbau der
> `Zweispaltenauswahl`), Summe S1–S3 24–33 h.
>
> **Em‑9.8 / Em‑9.9 (Anwenderentscheid 07.09.2026: „Sieben Fragen: Empfehlung. 9.8 und 9.9: Empfehlung"), umgesetzt in
> `880a9de`, Basis in `3d60d7e`, zusammengeführt in `9aa038f`:** `Referenzlauf/Ergebnisexport.cs` schreibt die **zehn
> Emissionsgrößen** als Skalare `Em.Kessel.*` / `Em.Bhkw.*` — Einheit im Namen (`Co2T` t/a, `So2Kg`/`NoxKg`/`CoKg`/
> `StaubKg` kg/a), **mit** CO, obwohl es strukturell 0 ist (ein späterer Trägerwert ändert dann eine Zahl statt einen
> Schlüssel hinzuzufügen), und nur, wenn die Stufe gelaufen ist (1007 und 1008 bekommen keinen). +70 Schlüssel wie in
> § 11.2.3 vorhergesagt (1 722 → 1 792); der iOS-Prüfmodus meldet für 1030 künftig 160 statt 150 Skalare. **Q1 = Z1**
> auf der Basis R5 statt R4 (R4 war beim Entscheid schon gepusht) — es bleibt bei einem Basiswechsel. **§ 9 Punkt 9 ist
> geschlossen als „bewusst nicht":** Schritt 68 wird gebaut, sobald eine Quelle mit CO-Faktoren je Energieträger
> vorliegt (weder UBA v2.1 noch GEMIS 5.2 führt CO), keine leere Art, keine Saat aus den Gerätespalten. **Neue
> Hausregel (Q4)** in `LIESMICH.md` und der Wurzel-`CLAUDE.md`: Wer einen gesäten Emissionsfaktor der Testdatenbank
> ändert, friert die Basis im selben Schritt neu ein; Vorlagen bleiben frei. Konzept fortgeschrieben: § 8 trägt die
> sieben Entscheide, § 9 Punkt 8 und 9 sind geschlossen, § 11.5 die Antworten. `EmissionsquelleTests` 10 → 12 (alle
> zehn Größen für 1030, dazu ein Wächter, der jeden Schlüssel im Quelltext an SEIN Feld gebunden findet). Abnahme auf
> Windows: A‑Em‑98‑1…4 (1030 zeigt die zehn Werte, 1007 keinen; Bericht und Kacheln unverändert; sieben Arten).
>
> **W14a‑E‑10 Stufe S1 (Anwenderentscheid 07.09.2026: „Katalogfilter: Empfehlung jeweils ja" — S1 starten, Q1 = ja,
> Q2 = ja), umgesetzt in `78b0f1e`/`ce43d2a`/`f842465`/`4d52b3b`/`7479d83`, zusammengeführt in `9d5fabf` — der
> Katalogfilter sitzt an der Spalte.** Die **acht Verwaltungsdialoge** (Heizkessel, BHKW, Solarkollektoren,
> Pufferspeicher, PV-Module, Wechselrichter, Stromspeicher, Wärmepumpe) tragen dasselbe Schema: EINE Suchzeile mit
> Trefferzahl, die Liste über die ganze Breite mit 5 bis 9 Parameterspalten, Filter und Sortierung im Spaltenkopf, der
> Eingabeblock darunter. **Kern:** `Katalogfilterprofil` (acht Ausprägungen), `Katalogfilter` mit `Katalogfilterstand`,
> **`Zahlenausdruck`** (Q1: ein Feld je Zahlenspalte versteht `>10`, `>=10`, `<60`, `<=60`, `=15`, `10..60`, `15`) und
> `Katalogfeld`; acht `…StammCtrl.Katalogfilterzeilen()` liefern Anzeige- und Filterwerte statt `ID, Bezeichner` und
> rechnen σ, C‑Rate, Modulfläche, VL min/max und COP A2/W35 einmal mit. **Oberfläche:** Bausteine `Spaltenfilter`
> (QuickGrid `ColumnOptions`, Trichter gefüllt gegen Umriss, Kennzeichenspalten nur sortierbar) und `Katalogliste`;
> der `Katalograhmen` steht **untereinander**, die Liste mit Höhengrenze 1,3 × `--epos-listenhoehe` (elf Zeilen).
> Entfallen: `KatalogFilterArt`, `HatHerstellerfilter`, die vier Klapplisten, `BrowserZeile`/`ModulZeile`;
> `LEISTUNG_SQL`/`VOLUMEN_SQL` bleiben für die Projektdialoge bis S2. **O‑6 entschieden und gemessen:** Ein Neuaufbau
> des Rasters (`@key`-Fix W6‑B‑2) schließt ein QuickGrid-eigenes Popover — das Feld wirkt deshalb bei Enter oder
> Verlassen, nicht beim Tippen; Esc schließt ohne zu übernehmen. **O‑7 bleibt offen** (Dialoghöhe im bunit-Markup nicht
> messbar, misst der Anwender). Trefferzahlen gegen die Testdatenbank decken sich exakt mit Anhang A des Konzepts
> (Heizkessel „Gas" 52, `10..60` 54, `>=0,95` 32 → 15 von 63; Wärmepumpe 7 von 51); der M3-Fall 20 749 → 15 ist als
> bunit-Probe unter `Virtualisiert` belegt. Nachweis: Kern 1891 / UI 3157 grün, SpeicherEngine 337, KiKern 469,
> Formularkarte 122, ChartProben 44, SQL 0, Designer 0 (49 neue Schlüssel), Gate grün, Referenzlauf byte-gleich gegen R5.
> **Offen (Anwender): W14a‑E‑10‑Q7** — die Spalte `Firma` für `Tab_Stromspeicher_STAMM`: das Bezeichnerpräfix trägt in
> der Testdatenbank 0 von 5 (handgepflegte Altsätze), in beiden Importwegen dagegen jeden Satz; der Schemaschritt (68,
> `Zielversion` 67 → 68, Testdatenbank ändert sich) wurde bewusst nicht in dieser Welle genommen — Empfehlung: mit S2.
> Abnahme auf Windows: A‑W14a‑E10‑1…13. S2 (die sieben Projektdialoge samt Wärmepumpe untereinander, Q2) ist der
> nächste Schritt.
>
> **W14a‑E‑10 Stufe S2 (Anwenderentscheid 07.09.2026: „S2 starten, Q7 mit aufnehmen"), umgesetzt in `11e1316` (Q7) ·
> `9fc285e` (S2.6) · `84bd1a0` (S2.5/S2.3) · `e3c4a51` (S2.1) · `5370b5a` (S2.2) · `df07e18` (S2.4) · `f6faeef` (Doku),
> zusammengeführt in `8dee370` — die Projektdialoge, der Assistent und der Filterstand.** Die **sieben Projektdialoge**
> stehen auf derselben `Katalogliste` wie die acht Verwaltungsdialoge aus S1 — fünfzehn Dialoge, zehn Komponenten, keine
> zweite Fassung. Der Kern des Entscheids ist die **Wärmepumpe**: ihre elf Bedienelemente sind neun Spalten geworden
> (Hersteller, Modell, Quelle, P_N, VL min, VL max, Zuheizung, Kühlen, COP); vier fallen mit gemessener Begründung weg
> (Bauart 45 von 51 leer, Auslegung = „Kühlen" Satz für Satz, Regelung und Aufstellung in den Kenndaten), und der
> alte Elf-Kriterien-Filter bleibt als Gegenprobe im Test (beide 7 von 51). **Q2 = ja:** die `Zweispaltenauswahl` steht
> **untereinander** (Projektliste oben, Übernahmeleiste als Textzeile ▲/▼, Katalog unten über die ganze Breite; elf
> Wirte, keiner brauchte eine Änderung; die Medienabfrage 900 px und das Pfeilpaar ◀▶ sind gefallen). **Q12 = ja:** „im
> Projekt verwendet" aus der lebenden Projektliste statt aus einer Zählabfrage (die Dialoge schreiben erst beim OK
> zurück; ein Durchlauf, keine neue SQL; Konzept 9.1). **S2.5:** der `Katalogfilterstand` lebt je Katalog über die
> Sitzung im `Katalogfilterregister` des Kerns, gemeinsam für Verwaltung und Projektdialog, mit Sortierspalte und
> ‑richtung. **Q7 = ja:** Schemaschritt **68** (`StromspeicherFirmaNachtrag`) gibt `Tab_Stromspeicher_STAMM` und
> `Tab_Stromspeicher` die Spalte `Firma`, Nachtrag aus dem Bezeichnerpräfix idempotent, beide Importwege schreiben sie,
> das Präfix bleibt Rückfall, Editorfeld „Firma"; Testdatenbank eingespielt (Schemastand 68, STRICT 117, die fünf
> Altsätze bleiben leer), `Zielversion` 67 → 68. Nebenbei fiel der Knopf „Modul-Katalog…" des `WaermepumpeStammDialog`
> (zeigte dieselbe Liste, iZ5). Nachweis: Kern 1925 / UI 3194 grün, SpeicherEngine 337, KiKern 469, Formularkarte 122,
> ChartProben 44, SQL 0, Designer 0, Gate grün, Referenzlauf byte-gleich gegen R5. Neu offen: **O‑8** (die
> Verwendungsspalte sagt nichts über andere Projekte) und **O‑9** (vier Wärmepumpenmerkmale nur noch in der
> Detailansicht); O‑7 bleibt. Abnahme auf Windows: A‑W14a‑E10‑S2‑1…12 — **Update-Hinweis: Schemastand 68, ein `.wpx`
> auf Stand 67 wird abgewiesen.** Nächster Schritt: Stufe S3 (Bedarf, Zeitreihen, Vergleich, Importmasken).
>
> **W14a‑E‑10 Stufe S3 (Anwenderentscheid 07.09.2026: „S3 starten"), umgesetzt in `92854e1` (S3.1) · `1b02bce` (S3.2) ·
> `df34bc5` (S3.3) · `2186ab9` (S3.4) · `daf86c0` (Doku), zusammengeführt in `19f3f39` — Bedarf, Zeitreihen, Vergleich,
> Importmasken.** **S3.1:** `Katalogfilterprofil.FuerBedarf` für Brauchwasser (16), Prozesswärme (32) und
> Stromverbraucher (41) — Bezeichner · Typ · Jahressumme [MWh] · Beschreibung · Auslieferung aus EINER Abfrage
> (`BedarfStammCtrl.Katalogfilterzeilen`; die Monatswerte stehen in MWh, kein Faktor 1000); `BedarfAdminDialog` und
> `BedarfsProfileDialog` auf der `Katalogliste`, „Standard Stromprofil" führt in denselben Dialog. **S3.2:**
> `Katalogfilterprofil.FuerZeitreihe`; `GanglinienAuswertungCtrl.Kennzahlen` liefert Jahresarbeit und Spitze je Katalog
> aus EINER `GROUP BY`-Abfrage — mit Fensterfunktion (`ROW_NUMBER() OVER (PARTITION BY …)`, Eimer auf 8 760 Stunden),
> weil die naive Abfrage für eine Viertelstundenreihe 4 590 kW Spitze meldete, die Grafik desselben Dialogs aber 1 513,5 kW
> (Stundenmittel) — eine Wahrheit; Preis: Stromganglinie 191 ms (78 840 Wertzeilen), Wärmebedarf 85 ms, Solarganglinie
> 21 ms, konstant in der Zeilenzahl. **S3.3, Q3 = ja:** der Vergleich lebt im Baustein `Katalogliste` und gilt damit in
> allen Wirten — Strg-/Umschalt-Klick auf den Wahlknopf markiert bis zu drei Zeilen (kein blanker Klick, keine zweite
> Spalte), „Vergleichen (n markiert)" öffnet eine breite `Ueberlagerung` mit einer Zeile je Parameter aus
> `ParameterUebersichtCtrl.Werte` (Bedarf: `BedarfStammCtrl.Vergleichszeilen` mit zwölf Monatswerten; Zeitreihen: die
> Profilspalten), Abweichungen mit Wort UND Farbe; die vierte Markierung fällt mit Hinweis. **S3.4, Q10 = ja:**
> `KatalogImportDialog` (fünf Ausprägungen) und `ModulImportDialog` (zwei) auf der `Katalogliste` in der Betriebsart
> `Mehrfach` (Kontrollkästchen, Doppelklick); die Wirte filtern weiter selbst mit `Katalogfilter.Anwenden` auf demselben
> Filterstand, damit Zeilenmarkierung und Statuszeile bei ihrer Regel bleiben. Erhalten: Mehrfachwahl W6‑E‑5 samt
> gesammelter Wahl, Statuszeile, Virtualisierung ab 120, `@key`-Fix W6‑B‑2, Übernehmen mit Vorprüfung, „Hersteller" ohne
> Doppelpunkt. **Damit tragen 21 Dialoge in 16 Komponenten die eine Katalogliste; die Filtermechanik gibt es im Haus
> genau einmal.** Bewusst geändert: die Filtervorbelegung der Zahlenleisten der Importmasken entfällt (**O‑10**, Rückweg
> benannt), die Importsuche läuft über alle Spalten (**O‑11**), Altdaten in `KatalogImportProfil`/`ModulImportProfil`
> bleiben bis zur Abnahme (**O‑12**). Nachweis: Kern 2033 / UI 3239 grün (de und en), SpeicherEngine 337, KiKern 469,
> Formularkarte 122, SQL 0, Designer 0, ChartProben 44, Gate grün, Referenzlauf 1030/1007/1017/1045 byte-gleich gegen
> `2026-09-07_R6_PvKoeffizienten` (der Zweig hat #150 vor S3.4 aufgenommen), kein Schema. Abnahme auf Windows:
> **A‑W14a‑E10‑S3‑1…10**. Nebenbefund am Werkzeug: `designer_neu.py schreiben` hängt je Lauf eine Leerzeile an
> (nicht idempotent) — eigene Aufgabe.
>
> **O‑13 erledigt (07.09.2026, `1cf0e1e`) und ResourceDesigner wiederholbar (`ff6a724`), zusammengeführt in `bd83486`.**
> Der in der Windows-Sandbox flatterhafte Fall `KataloglisteTests.Zwanzigtausend_Zeilen_werden_zu_fuenfzehn_und_das_Raster_zeigt_sie`
> (3 216 von 3 217, M7-Nachweis) war ein Wettlauf im Test, kein Fehler der `Katalogliste` — die dritte Fundstelle der
> Regel aus W16b‑O‑2 / W6‑B‑2‑O‑1: Der `@key`-Fix W6‑B‑2 schlüsselt das Raster mit `(Virtualisiert, Zeilenzahl)`, beim
> Übergang 20 749 → 15 wechseln beide Teile, Blazor baut das QuickGrid neu (mit `OnAfterRenderAsync` und asynchronem
> Datenabruf), und bunits synchrones Ereignis wird dahinter eingereiht — der Übergang, für den es den Fall gibt, lässt ihn
> flattern. Behoben nur am Test: drei wartende Helfer (`Filter` auf das gezeichnete Popover, `Gezeichnet` auf
> Trefferzeile UND Körperzeilen, `Sortiert` auf Pfeil und Reihenfolge), alle Wartestellen der Klasse nachgezogen;
> `SpaltenfilterTests` bleibt (unter 120 Zeilen, kein Neuaufbau). Messung: unter Last 1 rot in 560 Läufen, mit belegter
> Warteschlange 15/15 rot → 0/15; der Fall 30/30 grün unter Last in `de` und `en_US.UTF-8`. **ResourceDesigner:**
> `designer_neu.py schreiben` hängte je Lauf neun Zeichen an `Resource.Designer.cs` an — `rstrip('\n')` ließ die acht
> Leerzeichen der Trennzeile stehen, `block()` schrieb sie ein zweites Mal; 34 angesammelte Leerzeilen einmalig
> begradigt (1 823 894 → 1 823 588 Byte, 34 Löschungen, kein Schlüssel berührt), Lauf 2 und 3 ändern 0 Byte, jeder
> Aufruf prüft die Wiederholbarkeit selbst („+0; unveraendert"). Nachweis: Kern 2033 / UI 3239 grün (de und en),
> Warnungen 6, Gate grün, Referenzlauf byte-gleich gegen R6. Abnahme auf Windows: den Fall 10× unter Last laufen
> lassen (10/10 grün), `designer_neu.py schreiben` zweimal — `git status` bleibt sauber.
>
> **O‑12 erledigt (07.09.2026, `7c196c5` + `69ef1cb`), zusammengeführt in `8960ed3`.** Die zwei Importprofile führen
> keine Daten mehr, die keine Maske liest: gefallen sind aus `KatalogImportProfil` `FilterBezeichnung`, `FilterVon`,
> `FilterBis`, `FilterMaximum` und `HerstellerFilter` samt ihren Setzern in allen fünf Ausprägungen, aus
> `KatalogFilterbereich` seine vier Leistenteile, aus `ModulImportProfil` `FilterHersteller`, `FilterTechnologie`,
> `FilterSuche`, `SuchePlatzhalter`, `TextAlle` und `Zahlenfilter` samt dem Hilfstyp `ImportZahlenfilter`, dazu
> `ImportZeile.Hersteller`/`.Technologie` und fünf tote Fälle in `Texte.Zu` — **−146 Zeilen**. **Die Aufzählung in O‑12
> war eine Verdachtsliste:** gemessen wurde jedes Feld einzeln (Lesegraph über `*.cs` und `*.razor`, Tabelle im
> Protokoll `iU9_W15a_Blazor_Port_Protokoll.md`, Abschnitt „W14a‑E‑10‑O‑12"), und **`Zweitfilter` lebt** —
> `KatalogImportDialog.razor:629` liest seine Nachkommastellen für die zweite Zahlenspalte des Stromspeicherimports
> (W13‑E‑2); ebenso bleiben `FilterNachkommastellen`, `FilterSpaltentitel`/`-einheit` (sie bauen Kopf und Einheit der
> Zahlenspalte in `BaueListenprofil`) und `KatalogImportAblauf.Anzeigeindex`, dessen Prüffall die einzige Zusicherung
> gegen eine echte VDI-Datei hält. **Kein Prüffall ist entfallen, keiner dazugekommen** — fünf beschnitten, zwei
> umbenannt. Gate grün: 0 Fehler / 6 eindeutige Warnungen, Kern 2 033 / UI 3 239 / SpeicherEngine 337 / KiKern 469
> grün in `de` und `en_US`, Formularkarte 122, SQL 0 Fundstellen (1 297 Texte), ChartProben 44, Referenzlauf
> 1030/1007/1017/1045 **4 × PASS und byte-gleich** gegen `2026-09-07_R6_PvKoeffizienten`, kein Schema. Kein Verhalten
> ändert sich — Abnahme auf Windows: **A‑W14a‑E10‑O12‑1** (die sieben Importmasken verhalten sich unverändert; beim
> Stromspeicher zusätzlich: „Energie [kWh]" und „Leistung [kW]" mit **einer** Nachkommastelle und Zahlenausdruck
> `10..60`).
>
> **W14a‑E‑10‑O‑14 (Anwenderentscheid 09.09.2026 „toten Code entfernen — nur wenn sinnvoll"), umgesetzt in `1a04aa9`,
> zusammengeführt in `393b519`.** Der tote Filterweg `KatalogImportAblauf.Anzeigeindex(von, bis, suchtext)` — der Rumpf des
> alten `FuelleListe` der VDI-Importe (Zahlenbereich plus Suchtext über Name und Firma, Welle W13) — hatte seit der
> Katalogfilter-Stufe S3.4 keinen Aufrufer im Produktcode mehr; beide Importmasken filtern über `Katalogfilter.Anwenden`.
> Einziger Nutzer war der Testfall `DerFilterVerbindetZahlenbereichUndSuchtext`, der die mit **O‑10** gestrichene
> Vorbelegung „0 bis 1 000 Liter" prüfte. Methode und Testfall sind entfernt, die Abschnitte umnummeriert; die gleichnamigen
> privaten Helfer `Anzeigeindex(Katalogfilterzeile)` der zwei Wirte sind etwas anderes und bleiben, ebenso
> `KatalogImportSatz.Filterwert` (Hüllen, Daten, Tests) und `VdiAuswahlFilter.Passt` (`Katalogfilter` ruft die
> `params`-Form selbst — eine zweite Überladung gab es nie). Suche und Zahlenausdruck bleiben belegt durch
> `ZahlenausdruckTests.Bereich`, `KatalogspaltenfilterTests.Suche_ist_ODER_ueber_Spalten_und_UND_ueber_Begriffe` und den
> Wirt-Test `KatalogImportDialogTests.Der_Zahlenfilter_und_der_Suchtext_wirken_zusammen`. Nachweis: Kern 2194 / UI 3362
> grün, Referenzlauf 1030/1007/1017/1045 byte-gleich gegen R6 (Importpfad, kein Rechenweg); das Gate war bis auf EINEN
> vorbestehenden Befund grün: Der SQL-Dialektprüfer meldet fünf Fundstellen, weil die Testdatenbank auf Schemastand 69
> steht, `SchemaStand.Zielversion` aber seit den Schritten 70–72 (Wirtschaftlichkeit, `Ausleg_T_*`) auf 72 — schon auf
> `a4bc691` vor diesem Merge, nicht Teil von O‑14; die Testdatenbank ist auf Stand 72 einzuspielen (Muster #150). Konzept
> Katalogfilter Kapitel 9 (Entscheidzeile) und 10 (O‑14, Nachtrag an O‑12).

## Statusblock iU9 — Welle 14b umgesetzt (04.09.2026, Basis 01c9933 nach W13, zusammengeführt mit 34cc691; parallel zu W14a)

> **Statusblock iU9 — Welle 14b umgesetzt (04.09.2026, Basis `01c9933` nach W13, zusammengeführt mit `34cc691`; parallel zu W14a)**
>
> **Vier Masken — 670 Zeilen `.cs`, 937 Designer, 11 `MessageBox` — sind zwei Razor-Komponenten:** `BedarfAdminDialog`
> (`EPOS.UI/Dialoge/Bedarf/`) mit **drei Ausprägungen** über `BedarfsArt` (Brauchwasser, Prozesswärme, Stromverbraucher —
> die drei Drillinge waren bis auf die Bezeichner zeichengleich; fünf ihrer sieben Knöpfe riefen schon die Razor-Dialoge
> aus W8) und `SolarganglinieAdminDialog` (`EPOS.UI/Dialoge/Solarthermie/`, Zwilling der Ganglinien-Verwaltungen aus W12
> und W13, Einlesen über `GanglinienTextDatei.Lies(pfad, mitKopfzeile: true)` mit `Fortschritt`). Im Kern:
> `BedarfStammCtrl.Bezeichner`/`Kopf`/`Loeschen`, **`BedarfsVorschauCtrl`** (die drei Vorrechnungen als ein Weg),
> `SolarganglinieStammCtrl.Exists`/`HatProjektzuordnung` (die Präfixsuche `FindString` war die einzige Dublettenprüfung,
> B70). Mit der Welle fallen `EPOS.Kern/Allgemein/ToolsClass.cs` (letzter Nutzer), das Sprungziel `SolarganglinieAdmin`
> (`SolarganglinieDialog` zeigt die Verwaltung als Überlagerung) und der Kleinschreibungs-Zeuge der Formularkarte wandert
> auf `WizardParent.designer.cs`. **Elf Sachcommits und zwei Merges** (`2a53d36` … `8b855ce`), auf `ios_migration`
> als `c9855b1`. Der Nachweis (27 Kern-Fälle + Probe `solarganglinie_8760.txt`) entstand vor der ersten Maske.
>
> **Sieben Abweichungen** (A‑1…A‑7): Leerprüfung vor dem Löschen auch beim Brauchwasser, ein Löschsatz mit Platzhalter
> statt dreier Schreibweisen, Fehlschlag und ReadOnly-Sperre als Warnbanner, Rückfrage vor dem Löschen der Solarganglinie,
> der Ganglinienordner sichtbar (stand auf `Visible = False`, B79), „OK" liefert OK. **Zwei neue Befunde:** B78 — der
> Knopf „Ergebnisse" stand in allen drei Drillingen im Code, aber in keinem Designer: er war seit jeher tot, die
> Anwenderfragen E‑7/E‑8 der Vermessung sind damit gegenstandslos; B79 (s. o.). **E‑6 (B49, Brauchwasser ohne Teiler)
> ist durch den Anwenderentscheid W8‑O‑5/W9‑O‑3 erledigt** — `Energieeinheit`/`BedarfEinheitWahl` im Kern, MWh als
> Vorgabe, kWh wählbar, konsistent in allen Bedarfsansichten; die Prozesswärme im W9-Weg rechnet ebenfalls über die
> Einheitenklasse (`SimulationWaermebedarf.ProzesssummeUebernehmen`, W9‑O‑3b). Offen: **W14b‑O‑1** (Jahressumme in
> drei Formaten — Anwenderfrage, Empfehlung `F2`), W14b‑O‑2 (`Rechenstand` des W9-Wegs und `BedarfsVorschauCtrl`
> zusammenführen), W14b‑O‑3 (gleichnamige Datei im Ablageordner wird weiterverwendet), **W8‑O‑5b** (Simulation →
> „Wärmebedarf-Details" teilt ein bereits in MWh stehendes Brauchwasser ein zweites Mal — Anwenderentscheid).
>
> **Nachweise** (auf dem gemergten Stand `c9855b1`, Linux): Build → 0 Fehler, **12** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **3 087** grün (3 012 vor dem Merge), **identisch unter `LC_ALL=en_US.UTF-8`** ·
> Formularkarte **123** grün · Stapellauf **28** Masken (32 − 4), 27 erreichbar, 0 × „nein", 1 „unklar" (fällt mit
> W14a) · SQL-Prüfer 1 240 Texte, 0 Fundstellen · ChartProben 30 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017
> **PASS, byte-gleich** (815 043 Werte).
>
> **Protokoll** mit Feldkartenabgleich je Ausprägung, sieben Abweichungen, den Befunden B48…B79 und 25 Abnahmepunkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W14b_Blazor_Port_Protokoll.md`. **Windows-Abnahme steht aus**:
> die sieben Knöpfe je Ausprägung, die drei Jahressummen-Formate, Löschen leer/mit Rückfrage/schreibgeschützt, „Grafik"
> als Überlagerung mit Einheitenwahl, Solarganglinie (Ordner, Kopie, Einlesen mit Kopfzeile, Projektzuordnungssperre,
> Überlagerung aus dem Projektdialog), de/en, 125 %. Der fünfzehnte iOS-Lauf folgt nach dem Merge von W14a.
>
>
> **Anwenderwunsch W14b‑E‑9 vom 05.09.2026 (Admin-Dialoge an die Bildschirmgröße), umgesetzt in `ddf4d00`:**
> `SolarganglinieAdminDialog` steht nebeneinander wie `Form_Solarganglinie_Admin` (681×344, Liste links nach
> Hausanordnung); `BedarfAdminDialog` (drei Ausprägungen) und `WaermebedarfAdminDialog` bleiben gestapelt wie ihre
> Vorbilder (`Form_Stromverbraucher_Admin` 542×489, `Form_AdminWaermeeinlesen` 676×433), nehmen aber die Höhe des
> größeren Fensters (`Katalograhmen Gestapelt`). Protokoll ergänzt.

## Statusblock iU9 — Welle 13 umgesetzt (04.09.2026, Basis 08c489a nach W12, zusammengeführt mit 4101740)

> **Statusblock iU9 — Welle 13 umgesetzt (04.09.2026, Basis `08c489a` nach W12, zusammengeführt mit `4101740`)**
>
> **Sechs Masken — 2 396 Zeilen `.cs`, 2 621 Designer, 32 `MessageBox` — sind drei Razor-Komponenten:**
> `KatalogImportDialog` (`EPOS.UI/Dialoge/Import/`) mit **vier Ausprägungen** für die VDI-3805-Blätter Heizkessel,
> Pufferspeicher, Solarkollektoren und Wärmepumpen (`KatalogImportProfil` als Satz je Katalog, `KatalogImportAblauf`
> als EIN Kern-Ablauf Lesen → Vorprüfen → Konfliktdialog → Ausführen, transaktional), `WaermebedarfAdminDialog`
> (Zwilling der Stromganglinien-Verwaltung aus W12, `GanglinienTextDatei.Lies(pfad, mitKopfzeile)` bereits mit dem
> Kopfzeilenschalter für W14b) und `PvModulImportDialog` (CEC-Katalog und `.pan`-Dateien, erstmals lokalisiert). Die
> eine Bausteinlücke — **Mehrfachmarkierung im `Raster`** — ist gebaut. Mit der Welle fallen die
> `ImportKonflikteHuelle` aus W12 (alle Aufrufer sind Razor) und die Sprungbrücke `WaermebedarfExternAdmin`
> (`WaermebedarfExternDialog` zeigt die Verwaltung als Überlagerung). **Alle sechs Masken sind im selben Commit wie
> ihr Nachfolger gelöscht** (Regel M1, ohne Nachzügler). Acht Sachcommits und ein Merge (`0711916` … `a59cbd5`),
> auf `ios_migration` als `01c9933`.
>
> **Der Nachweis der Welle sind die Importproben.** Für die fünf Parser, `DublettenPruefung` und `VdiAuswahlFilter`
> gab es keinen Test (W13‑B1); **zwanzig Probendateien** unter `Referenzlaeufe/Importproben/` (188 KB, CP1252 und
> CRLF per `.gitattributes` eingefroren) mit aus dem Bestand eingefrorenen Erwartungswerten entstanden **vor** der
> ersten Maske — Vaillant/Buderus-Heizkessel mit Wirkungsgrad-Rückfall, Pufferspeicher mit dem fehlenden zehnten Block
> (B23, wörtlich behalten), Solar mit allen vier Bauarten, Hoval-Wärmepumpen mit Voll-/Teillast-Trennung, 50
> CEC-Module, vier `.pan`, 8 760 Wärmebedarfswerte mit drei Gegenproben. 27 Abweichungen (A‑1…A‑27) mit je einem
> Windows-Abnahmepunkt, u. a.: Solar bekommt Dublettenprüfung und Konfliktdialog, alle vier Importe schreiben
> transaktional, die Übernahme liest aus den Detailfeldern, Wärmepumpen-Ordner `VDI_Waermepumpe` mit Rückfall.
> Drei neue Befunde: B56 (Wärmebedarf-Beschriftung nannte Komma, der Parser liest invariant), B57 (Trina-PAN ohne
> `Bifacial`-Schlüssel), B58 (`CEC Modules.csv` ist eine Semikolon-Fassung — unlesbar für den Dienst, die Probe stammt
> aus `_UTC`).
>
> **Nachweise** (auf dem gemergten Stand `01c9933`, Linux): Build → 0 Fehler, **12** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **2 972** grün (2 807 nach W12), **identisch unter `LC_ALL=en_US.UTF-8`** ·
> Formularkarte **123** grün · Stapellauf **32** Masken (38 − 6) · SQL-Prüfer 1 241 Texte, 0 Fundstellen ·
> ChartProben 30 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte).
>
> **Protokoll** mit Feldkartenabgleich je Ausprägung, den Importproben, 27 Abweichungen und sieben offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W13_Blazor_Port_Protokoll.md`. **Anwenderfragen:** W13‑O‑2 (PAN
> ohne Temperaturkoeffizienten, B44), W13‑O‑3 (zwei Leistungsbegriffe, B40), W13‑O‑4 (fehlende Nachlaufblöcke, B23),
> W13‑O‑5 (Kühlleistung an Zuheizung gekoppelt, B32), W13‑O‑6 (zwei PV-Menüpunkte, eine Maske). **Windows-Abnahme
> steht aus** (§ 10 des Protokolls, zwölf Punkte). Der vierzehnte iOS-Lauf (33844935661) auf diesem Stand ist grün.
>
> **Windows-Abnahme 05.09.2026, Befund W13‑B‑1 („Admin: vdi3805 Datei import: Absturz bei Datei laden, teilweise
> Absturz auch bei Dateiauswahl-Dialog"), behoben in `4fd8cc7`:** Zwei Ursachen. Der modale Dateiwähler lief
> **synchron im `WebMessageReceived`-Rückruf** derselben WebView2 — `KatalogImportHuelle.DateiWaehlen` gab
> `Task.FromResult(Dienste.Datei.DateiOeffnen(…))` heraus, ein schon erfüllter Task, der `OpenFileDialog` pumpte seine
> Nachrichtenschleife also in der WebView, die gerade zeichnet (wortgleich das Muster von W16b‑B‑1, eine Ebene tiefer;
> elf Hüllen hatten dieselbe Zeile, ob es gutgeht, hing an der Zeitlage — daher das „teilweise"). Und eine Ausnahme
> aus einem Blazor-Ereignis hatte **kein Netz** — der WinForms-`BlazorWebView` 10.0.100 führt kein
> `UnhandledException`. Behebung: `IDateiDienst`/`IDialogDienst` führen wartbare Zwillinge mit Standardfassung
> (`DateiOeffnenAsync`, `DateiSpeichernAsync`, `OrdnerWaehlenAsync`, `MeldungAsync`, `WarnungAsync`, `FrageAsync`);
> die Windows-Fassungen posten sie über `Allgemein/Blazor/Blazornachlauf.cs` — der Bruder von `Blazorsprung` für den
> Fall **mit** Rückgabewert — eine Nachricht später; `Dateiwahl.razor` und `KatalogImportDialog` brauchten keine Zeile,
> sie warteten von jeher. Dazu die **Fehlerschranke** (`EPOS.UI/Bausteine/Fehlerschranke.razor` auf `ErrorBoundaryBase`,
> `Wurzel<T>`), die alle drei Hüllen und die iOS-Seite statt `T` mounten. Der Kern ist als Ursache ausgeschlossen: neun
> neue Fälle fahren alle vier Ausprägungen gegen sechs Bauarten kaputter Dateien, `Lesen` macht daraus eine
> `IMP_KAT_PROT_LESEFEHLER`-Meldung. Auf iOS war derselbe Befund ein anderer Fehler: `IosDateiDienst.AufDemHauptfaden`
> lieferte vom Hauptfaden `default`, der Wähler ging nie auf — mit den `…Async`-Fassungen behoben. Protokoll § 13,
> Abnahmepunkte B1–B7 (Wähler geht auf, kaputte Datei → Warnbanner, Fehlerkasten mit rotem Rand statt Absturz).
>
> **Formularraster, Paket P3 (iU8‑E‑2, 05.09.2026, `d3fccf1`):** `PvModulImportDialog` (Klasse B, 21 von 28 Feldern in drei Rastern — die drei
> Detailreiter sind Formularblöcke, die zwei Filterleisten `epos-pvimport-filter` über dem Modulgitter nicht;
> `epos-pvimport-details` samt Regel gefallen). Nachtrag im W13-Protokoll, obwohl die Datei in der P3-Pakettabelle
> stand.
>
> **W13‑E‑2 (Anwenderwunsch 07.09.2026: „es gibt keinen Datenimport für Stromspeicher … folgende Quellen prüfen"),
> Stufe 0 umgesetzt in `c4cf58f`, zusammengeführt in `8235092`:** Vier Quellen mit Zugriffsbeleg geprüft. Nur die **CEC
> Energy Storage System List** ist ein Geräteverzeichnis (XLSX, 1 338 305 Byte, 6 654 Geräte, 130 Hersteller, Stand
> 21.08.2026; kWh/kW zu 100 % belegt, η_RT bei 16,6 %; keine Kosten, Degradation, Zyklen, Standby). **bslib** (HTW Berlin,
> MIT + CC BY 4.0) liefert vier vermessene Systeme und als einzige den Standby, ihre √η-Konvention ist die von
> `SpeicherParameter`. **NREL SAM** führt keine Speicherliste, sondern vier generische Zellchemien, **TUM simses** Messreihen
> einer Zelle und zwei Zellmodelle — beide ohne Gerätebezug, nicht importierbar. Gebaut: Kern-Zerleger `CecSpeicherImport`
> (XLSX über ClosedXML und CSV), `BslibImport`, `StromspeicherImportSatz` (`NachModell`, geteilte Umrechnungen), vier
> Zellchemie-Werte in `DbWerte`, zwei echte Proben (`stromspeicher_cec_ess_23.csv`, `stromspeicher_bslib_7.csv`), 28 Fälle;
> die volle CEC-Mappe läuft durch (6 654 Sätze, keiner ohne Leistung, kein η außerhalb (0…1]). **Keine Maske, kein
> Menüpunkt, kein Migrationsschritt** — Rechenweg unberührt. `Konzept_Stromspeicherimport_EPOS-Plan.md`: Prüfbericht,
> Abbildung auf `Tab_Stromspeicher_STAMM`, Stufenplan S1–S3, Mockup, acht Fragen Q1…Q8 mit Empfehlung; die wichtigste ist
> **Q2**: Die CEC-Nutzungsbedingungen untersagen die kommerzielle Nutzung, und den NREL/BSD-3-Umweg der Modul- und
> Wechselrichterlisten gibt es für Speicher nicht — Empfehlung: nicht mitliefern, sondern Abrufknopf nach dem Muster von
> `CecWechselrichterDienst`, bslib ausliefern. Stufe S1 (siebte Ausprägung des `KatalogImportDialog`, Menüpunkt
> „Stromspeicher…" unter Daten & Import ▸ nach W16c‑E‑7) folgt nach dem Anwenderentscheid. Nachweis: Kern 1672 / UI 3020
> grün, SQL 0, Gate grün, Referenzlauf 1030/1007/1017/1045 byte-gleich.
>
> **W13‑E‑2 Stufe S1 (Anwenderentscheid 07.09.2026: Q1…Q8 = „Empfehlung"), umgesetzt in `d3cd643`, zusammengeführt in
> `e88cdab`:** Der einzige Katalog ohne Einlesepunkt hat einen. `KatalogImportArt.Stromspeicher` ist die **fünfte
> Ausprägung** des `KatalogImportDialog` und die erste ohne VDI 3805: drei Quellknöpfe statt des Dateiwählers („CEC-Liste
> abrufen", „CEC-Datei laden" XLSX/CSV, „bslib laden"), sieben Listenspalten, zwei Zahlenbereiche (kWh und kW),
> Herstellerklappliste, Herleitungszeile zu den fehlenden Kosten (Q3: leer, in der Verwaltung pflegen); dorthin ist auch
> der Stufenhinweis der Wärmepumpe gewandert (`Hinweis` im Profil statt eines zweiten Markup-Zweigs). Mehrfachwahl,
> Doppelklick und Konfliktdialog kommen unverändert aus dem Wirt (W6‑E‑5). **Der Quellschlüssel wählt den Zerleger, nicht
> die Dateiendung** — `bslib_database.csv` und eine ausgeleitete CEC-CSV sehen gleich aus (`Lesen`-Delegat mit
> Quellschlüssel). Die CEC-Liste wird **nicht** mitgeliefert (Q2, Nutzungsbedingungen); `CecSpeicherDienst` holt sie nach
> dem Muster von `CecWechselrichterDienst` — Bytes statt Text (XLSX), zerlegt nichts, `HttpMessageHandler` und Ablageort
> einlegbar, Zwischenspeicher unter `%LocalAppData%`, ohne Netz Warnbanner bzw. Rückgriff auf den Zwischenspeicher.
> **bslib wird ausgeliefert** (`VDI-3805-Daten/Stromspeicher/bslib_database.csv`, CC BY 4.0, 2 729 Byte byte-gleich zur
> Importprobe und zu PyPI-, FZJ- und HTW-Quelle, `LIESMICH_bslib.md`); `Setup/EPOS-Plan.iss` bleibt unverändert, weil die
> Komponente `herstellerdaten` rekursiv liefert. Übernahme über `StromspeicherStammCtrl.ImportUebernehmen`/`UpdateImport`
> (Q4 Standby = max(voll, leer) je AC+DC, Q5 Bezeichner „Hersteller: Modell", Q6 keine Zyklen-Vorbelegung; Überschreiben
> frischt Kapazität, Leistung, η und Standby auf und lässt von Hand gepflegte Kosten, Degradation und Zyklen stehen).
> Menüpunkt „Stromspeicher (CEC, bslib)…" unter Administration ▸ Daten & Import hinter dem Knoten „Photovoltaik" (58 Punkte,
> 45 handelnd — der erste neue Weg seit W6‑E‑2), F1 auf den Abschnitt „Datenimport" der `Stromspeicher.wiki`. Kein
> Migrationsschritt, Rechenweg unberührt. Nachweis: 23 neue Kern-Fälle (`StromspeicherUebernahmeTests`, Transaktion mit
> Rollback), 11 bunit-Fälle, Kern 1707 / UI 3057 grün, Designer „abweichend 0", SQL 0, Gate grün, Referenzlauf
> 1030/1007/1017/1045 byte-gleich. Q7 (Standby im Rechenweg, Stufe S3) bleibt als spätere Stufe im Konzept. Abnahme auf
> Windows: A‑W13‑E2‑1…15 — darunter der Netzabruf mit und ohne Verbindung (A‑W13‑E2‑5…7), Abbruch während des Abrufs,
> bslib ohne Dateiwähler nach Installation mit der Komponente „Herstellerdaten" (vier Sätze, drei übergangene Zeilen).

## Statusblock iU9 — Welle 12 umgesetzt (04.09.2026, Basis 73a4338 nach W11b, zusammengeführt mit fe22915)

> **Statusblock iU9 — Welle 12 umgesetzt (04.09.2026, Basis `73a4338` nach W11b, zusammengeführt mit `fe22915`)**
>
> **Sechs Masken — 2 134 Zeilen `.cs`, 1 409 Designer, 10 `MessageBox` + 13 indirekte — sind sechs
> Razor-Komponenten:** `GanglinieProtokollDialog`, `GanglinieImportOptionenDialog`,
> `ImportKonflikteDialog`, `StromganglinieAdminDialog`, `StromganglinieDialog` und
> `PeakShavingDialog`. **Der rote Faden ist die AP5-Importkette**, die zweimal wörtlich im Bestand
> stand (mit Ablage in der Stammdatenverwaltung, ohne in der Lastspitzenkappung) und jetzt EIN
> Kern-Ablauf `GanglinienImportAblauf` mit drei Rückrufen ist; die drei Zwischenmasken erscheinen
> als `Ueberlagerung` desselben Fensters, jeder Rückruf wartet auf eine `TaskCompletionSource`.
> **`ImportKonflikteDialog` ist Blatt vor Host MIT Hülle** (Entscheid § 8.3 der Vermessung): Vier
> seiner fünf Aufrufer bleiben bis W13 WinForms, und die `Sprungbruecke` kann keine Nutzlast
> zurückgeben — die Hülle kostet 80 Zeilen und lebt eine Welle. **Acht Dateien in den Kern:**
> `GanglinienImportAblauf`, `GanglinienOptionenModell`, `GanglinienProtokollText`,
> `ImportKonfliktModell`, `PeakShavingCtrl` (Umzug), `PeakShavingKennzahlenBlock`,
> `PeakShavingEingaben`, `PeakShavingBild`. **Bilanz 80 Dateien, +9 742 / −5 422 Zeilen** (ohne die
> 3,4 MB Probendateien). Sechzehn Sachcommits und ein Merge (`72dd8ba` … `34e2095`), auf `ios_migration` als `08c489a`.
>
> **Der Nachweis der Welle ist der bitgleiche Ganglinien-Import.** Dafür gab es KEINEN Test
> (Befund W12‑B14); die zwölf Proben — Trennzeichen `;`/`,`/Tab/einspaltig × Dezimaltrenner ×
> Kopfzeile × 8 760/35 040/525 600 × Schaltjahr × beide Sommerzeitfälle × `.xlsx` — entstehen
> deshalb ZUERST, mit aus dem Bestand eingefrorenen Erwartungswerten. Sie laufen danach durch den
> neuen Kern-Ablauf und liefern dieselben Zahlen. **Befund W12‑B27 dabei gefunden und behoben:**
> Der Excel-Zweig war überhaupt nicht benutzbar (drei Leseschleifen liefen um eine Zeile über das
> Feld hinaus, jeder `.xlsx`-Import endete in `IMPORT_PROT_LESEFEHLER`) — damit ist der offene
> Nachweispunkt `Umsetzung_iU0_iU1_Nachweise.md:136` erklärt und abgehakt.
>
> **Zwei Entscheidungen:** (1) **Kein neuer Renderer** für das Vorher/Nachher-Bild —
> `ChartRenderer.ErzeugerStapel` trägt seit W11a eine Sekundärachse und rechnet die
> Jahresstundenmarken über die Reihenlänge um; die ChartProben bleiben bei 30. (2) **Der Anker des
> Erreichbarkeitstests hängt jetzt an `Form_AdminSettings`** (`MDIMainForm → MenuItem_Einstellungen`):
> Von den zwölf Masken mit einem Pfad ab `Form_Start` fällt keine erst in W13/W14 (Befund W12‑B26),
> der Test kann seine Form „über die Startseite" nicht behalten. **Der Rechenlauf der
> Lastspitzenkappung läuft nebenher** (`Task.Run` + `Fortschritt`, Befund W12‑B22) — die dritte
> nebenläufige Rechnung der Anwendung.
>
> **Nachweise** (auf dem gemergten Stand, Linux): Build → 0 Fehler, **12** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **2 807** grün (2 614 nach W11b), **identisch unter
> `LC_ALL=en_US.UTF-8`** · Formularkarte **123** grün · Stapellauf **38** Masken (43 − 5),
> 37 erreichbar, 0 × „nein" · SQL-Prüfer 1 231 Texte, 0 Fundstellen · ChartProben 30 Bilder,
> 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte) ·
> **Import-Proben byte-gleich**.
>
> **Protokoll** mit Feldkartenabgleich, 19 Abweichungen (A‑1…A‑19), den wörtlich übernommenen
> Befunden und fünf offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W12_Blazor_Port_Protokoll.md`.
> **Anwenderfrage W12‑O‑1:** Befund B5 — derselbe Katalogeintrag lässt sich einem Projekt beliebig
> oft zuordnen (heute wie früher). Soll das so bleiben? **Windows-Abnahme steht aus**: Verwaltung
> mit Einlesen (CSV `;`/`,`, `.xlsx`) samt Optionen/Protokoll/Konflikt, Startbild → Strom-Messdaten
> mit ◀/▶ und „Bearbeiten…", Lastspitzenkappung mit Balken, minimaler Schwelle, drei Reitern und
> CSV — auch ohne geöffnetes Projekt —, die vier W13-Importmasken über die Konflikthülle, de/en,
> 125 %, Esc je Ebene.
> **Anwenderentscheid #76 vom 05.09.2026 („#76: Empfehlung"), umgesetzt in `b6fd863`:** `StromganglinieDialog` steht im Baustein `Zweispaltenauswahl`; die zwei
> Glyphen-Parameter samt `STROMGL_BTN_HINZUFUEGEN`/`_ENTFERNEN` sind ohne Nutzer und gefallen.
>
> **Windows-Abnahme 05.09.2026, Befund W12‑B‑1 (Bildschirmfoto „Standard Stromprofil": „Beschriftung der Buttons
> nicht zur Umrandung passen" — die vier Katalogknöpfe unter der Datenbankliste liefen über ihren Rahmen bzw. waren
> abgeschnitten), behoben in `e56ac6d`:** Ursache waren zwei Zeilen im Hausblatt, nicht der Baustein
> `Zweispaltenauswahl`: `.epos-leiste` war ein `display: flex` ohne `flex-wrap`, `.epos-knopf` hatte neben
> `min-width: 88px` die Vorgabe `flex-shrink: 1` — in der halb so breiten rechten Spalte schrumpften die vier Knöpfe
> auf ihre 88 px, während „Stromverbraucher" als unteilbares Wort breiter blieb. Behoben an **einer** Stelle für das
> ganze Haus, ohne einen Dialog anzufassen: `flex-wrap: wrap` an der Leiste, `flex: 0 1 auto` mit `white-space:
> normal` und `overflow-wrap: break-word` am Knopf, `padding: 4px 12px`; **kein** `overflow: hidden`, denn
> Abschneiden wäre derselbe Fehler in still; einzeilige Knöpfe („OK", „Abbrechen", Fußleiste) bleiben in Höhe und
> Breite, wie sie waren. Mit erledigt sind die Katalogleisten aller elf Projekt↔DB-Dialoge (vier Knöpfe bei
> `BedarfsProfileDialog` und `GebaeudeDialog`, drei bei BHKW/Heizkessel/Solarkollektoren, zwei bei
> Pufferspeicher/Photovoltaik/Wärmebedarf extern) und die der Katalogverwaltungen am `Katalograhmen`. Drei neue
> Wachen in `ZweispaltenauswahlTests` (14 → 17: Markup, Bestand, Stilregel), Gegenprobe mit zurückgedrehtem Blatt
> rot. Protokoll W12, Abnahmepunkte A‑W12‑B‑1.
>
> **Anwenderwunsch W12‑E‑1 vom 05.09.2026 (Bildschirmfoto „Stromganglinien": „csv-Datei Stromlastgang importieren
> (mit Info zum Format) fehlt. Ebenfalls fehlt löschen und Speichern unter"), umgesetzt in `43f0581`:** Das Vorbild
> `Form_Stromganglinie` (678 × 345) hatte keinen Import, kein Löschen und kein Speichern unter — der Wunsch ist eine
> echte Erweiterung; „Datei Einlesen…" und „Ganglinie Löschen" lagen eine Maske weiter in `Form_Stromganglinie_Admin`,
> „Speichern unter" gab es im ganzen Bestand nicht (der Eintrag „… - Kopie" der Testdatenbank ist ein zweiter Import
> unter anderem Dateinamen). Der Dialog trägt unter der Katalogliste jetzt **vier** Knöpfe statt einem: „CSV-Datei
> importieren…", „Speichern unter…", „Löschen", „Bearbeiten…". Der Import ist kein zweiter Weg, sondern derselbe: Die
> Kette liegt seit W12.0d im Kern (`GanglinienImportAblauf`), ihre Oberflächenseite steht jetzt im Baustein
> `GanglinienImportLauf.razor` (drei Überlagerungen, `Starten(pfad, raster)`), den auch die Verwaltung
> `StromganglinieAdminDialog` einhängt (422 → 303 Zeilen) — die Überlagerungen gibt es einmal statt zweimal. Der
> Formathinweis nennt sichtbar, was die Kette annimmt (8 760 bzw. 35 040 Werte, vier Feldtrennzeichen oder einspaltig,
> erkannte Kopfzeile, Komma oder Punkt, kW oder kWh je Intervall, Bezeichner = Dateiname ohne Erweiterung) und steht
> als Kurztext am Infoknopf. Löschen prüft zwei Sperren vor der Rückfrage und meldet beide Gründe — Projektzuordnung
> (`StromganglinieStammCtrl.HatProjektzuordnung`, Muster W14b) und `ReadOnly` (Grund als `title` am Knopf).
> „Speichern unter" ist die Kopie unter neuem Namen (`KopiereStamm`: Kopf und Werte in einer Transaktion, `ORDER BY
> ID`, immer `ReadOnly = false`, Vorschlag „‹Name› - Kopie", Dublettenprüfung vor dem Einfügen in Maske und Kern).
> Nebenbefund behoben: `ReadAll` warf `ReadOnly` weg — die Verwaltungshülle fragte je Zeile nach (N+1), der
> Projektdialog konnte einen Auslieferungssatz nicht erkennen. Kern 1 086 (+10 `StromganglinieKatalogTests`), UI 2 509
> (+16), SQL-Prüfer 1 204 / 0. Protokoll W12, zehn Abnahmepunkte.
>
> **Formularraster, Paket P3 (iU8‑E‑2, 05.09.2026, `d3fccf1`):** `PeakShavingDialog` (19 Felder, vier Raster, sieben `epos-feldpaar` gefallen; „minimale
> Schwelle ermitteln" steht neben der Zielschwelle, Ergebnisreiter bleibt Tabelle) und
> `GanglinieImportOptionenDialog` (Klasse B: die acht Formatlisten im Raster, das Vorschaugitter nicht).
> `StromganglinieDialog`/`StromganglinieAdminDialog` unangetastet (W12‑E‑1/E‑2).
>
> **Anwenderwunsch W12‑E‑2 vom 05.09.2026 („stelle die importierte Stromganglinie als Grafik dar (wie bisher, zoombar,
> umschaltbar auf sortiert)"), umgesetzt in `dbdcdf1`:** Ein „bisher" gab es nicht: Weder `Form_Stromganglinie_Admin`
> noch `Form_Stromganglinie` noch `Wizard_Stromlastgang` trug je ein Chart, und das einzige Chart der
> Stromverbrauchermasken (`Form_ErgStromverbraucher`) zeichnete Monatssäulen. Übernommen ist deshalb das einzige
> Vorbild, das der Bestand kennt — das Bild B1 des Bedarfsreiters (`ChartRenderer.GanglinieNormiert`) — in der
> Anordnung des `GebaeudeBedarfDialog` (W9.8). Sobald links oder rechts eine Zeile markiert ist, steht unter den zwei
> Spalten der neue Baustein `GanglinienGrafik`: drei Kennzahlen (Jahresarbeit, Spitze als 100‑%-Linie,
> Vollbenutzungsstunden), der Schalter „sortiert", die Einheitenwahl MWh/kWh (W8‑O‑5) und das Bild im Baustein
> `Diagramm` mit Bild- und Datenzoom. Die Zahlen liefert `StromganglinieAuswertungCtrl` im Kern aus derselben Wertspalte
> wie der Lauf (Katalog und Projektkopie); 35 040 Viertelstundenwerte gehen durch
> `SimulationControl.Viertelstunden_zu_Stundenwerte_Mittelwert` — kein zweiter Rechenweg, kein neues Renderer-Bild.
> Den Platz gibt der Formathinweis her: sichtbar ist eine Zeile (`STROMGL_HINWEIS_FORMAT_KURZ`), der volle Wortlaut
> hängt am Infoknopf. Eingefroren gegen die Testdatenbank: `Lastgang_Strom_NestleLB` 4 790,086 MWh / 2 070,00 kW /
> 2 314,05 h/a; `test` (35 040 → 8 760) 4 788,929 MWh / 1 310,75 kW / 3 653,58 h/a. Tests: `StromganglinieDialogTests`
> 30 → 41, `StromganglinieAuswertungTests` 7 neu; ChartProben und SQL-Prüfer unverändert grün. Zehn Abnahmepunkte
> A‑W12‑E‑2 im W12-Protokoll.

## Statusblock iU9 — Welle 11b umgesetzt (04.09.2026, Basis 81a04ec nach W11a, zusammengeführt mit 604d1f6)

> **Statusblock iU9 — Welle 11b umgesetzt (04.09.2026, Basis `81a04ec` nach W11a, zusammengeführt mit `604d1f6`)**
>
> **Nachtrag Windows-Abnahme V3 (07.09.2026) — der Photovoltaik-Reiter, W11b‑B‑6 bis B‑10, umgesetzt in
> `159f3f8` (Merge 7 `e6803a6`).** „Die PV-Simulation scheint nicht zu funktionieren": Reiter und Kurve zeigten den
> GENUTZTEN Anteil (`Stromproduktion` = min(Erzeugung, Bedarf), ohne Strombedarf 0), Überschuss und Modultabelle die
> Erzeugung — der Port war wörtlich (`:4551`, `:4574`), die Beschriftung nie. **B‑6** DTO trägt Erzeugung
> (`Stromproduktion_Theoretisch`) und genutzten Anteil getrennt, `BildPv` zeichnet die Erzeugung; **B‑7** Einheit
> W/m² statt kW (`MaxEinstrahlungWm2`); **B‑8** Fläche eines CEC-Moduls ohne Katalogmaße aus P_STC/η geschätzt und
> als `≈` gekennzeichnet (`SimulationPV.FlaecheZurAnzeige`, nur Anzeige); **B‑9** kein eigener Seitentitel unter dem
> Titel der Überlagerung, Hilfeknopf bleibt rechts; **B‑10** Diagramm auf drei Viertel der Zeile
> (`min(--epos-diagramm-breit, 75%)`) — auf 1280 × 800 bei 150 % füllte es den sichtbaren Reiter. Nachweis:
> `ErzeugerReiterTests` +3, `PvModulparameterTests` +3, `SimulationErgebnisCtrlTests` erweitert, Referenzlauf
> unberührt (Protokoll W11b, Abschnitt „Windows-Abnahme V3").
>
> Der zweite Lauf der Welle 11: **`Form_Simulation_Detail` (7 766 Zeilen + 3 082 Designer), `DashboardForm`,
> `NavigatorUebersicht`, `NavigatorStrom`, `NavigatorWaerme` und `Form_SpeicherVariantenVergleich` → eine
> Razor-Seite `SimulationErgebnisSeite`** (`EPOS.UI/Seiten/Simulation/`) mit **zehn** Blättern (R3 „Simulation“
> war nur der Behälter der Menüliste, A‑1), dem Ergebnis-Blatt mit Autarkie-Analyse, den Ganglinien-Navigatoren
> Wärme/Strom und dem Variantenvergleich als Überlagerung; `TabNavigationManager`, `TabListMapper`,
> `DonutChartDrawer`/`Kacheln` gelöscht (`ChartManager` bleibt für Klimadaten und Peak-Shaving, A‑12).
> **Hosting-Entscheid R‑W11‑1:** Seite mit `SeitenZustand` (iOS erreicht sie über `AppWurzel`), auf Windows
> bis W16 in der modalen Dialoghülle, weil die Bedarfsobjekte der Startmaske gehören (**eingelöst mit W16b, E‑5,
> 04.09.2026:** das Ergebnis ist eine `Ueberlagerung` der Razor-Startseite, die Bedarfsobjekte gehören dem Projekt,
> `BedarfsZustand`; der Automatikstart beim Öffnen bleibt). Die Hülle fährt
> `SimulationLaufCtrl.Laufen` in `Task.Run` mit `Fortschritt` und Abbrechen; der Automatikstart beim Öffnen
> bleibt, Endlage Übersicht. **Sprungbrücke `SpeicherOptimierung`** (bleibt WinForms, iF22) mit Rückgabe
> `AuslegungUebernommen`; `SimulationKonfigSeite` (W10b) als Überlagerung — `SeitenZustand` wird **nicht**
> doppelt gebraucht. **Bilanz 78 Dateien, +11 159 / −27 103 Zeilen.** Vierzehn Sachcommits und ein Merge
> (`5ac1703` … `2c47cf0`), auf `ios_migration` als `73a4338`.
>
> **Der Ertrag ist eine WebView für das ganze Simulationsergebnis** — drei Navigationen und ~130
> Laufzeit-Steuerelemente sind ein `Reiter`; die 17 Zeichenflächen laufen über die sieben W11a-Bilder.
> **Anwenderentscheid W11a‑O‑1 umgesetzt (A‑19):** „Wärme gesamt“ ist die Summe der **Deckung** je Erzeuger,
> die Restwärme ist **eine** Zahl (`sim.Restwaerme`) und kann rechnerisch nicht negativ werden — 1030
> 6 137,56 − 6 137,56 = 0,00, 1007 6,04, 1017 0,00; Bedarf − Deckung trifft die Bilanzgröße in allen drei
> Projekten. Zehn Befunde in W11b behoben (u. a. zwei Fülllogiken für `chart2`, Heizstab in beiden
> Zweigen derselbe Anteil, Stromgang mit Sortiertumschalter, BHKW-Strom eigene Farbe, die elf Reitertitel
> erstmals englisch), 19 entfallen mit den Masken. Offen: W11b‑O‑1 (14 stille `Console.WriteLine`),
> O‑2 (17 Flächen ohne Foto des Bestands — Sichtabnahme am Gerät), O‑3 (erstes Blatt des Ergebnis-Reiters
> doppelt zur Übersicht?), O‑4 (`Form_SpeicherOptimierung` modal über der WebView), O‑5 (`IosProjektQuelle`
> liefert den Satz noch nicht).
>
> **Nachweise** (auf dem gemergten Stand `73a4338`, Linux): Build → 0 Fehler, **12** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **2 614** grün (2 502 nach W11a), **identisch unter `LC_ALL=en_US.UTF-8`** ·
> Formularkarte **123** grün · Stapellauf **43** Masken (49 − 6), 42 erreichbar, 0 × „nein“ · SQL-Prüfer
> 1 233 Texte, 0 Fundstellen · ChartProben 30 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS,
> byte-gleich** (815 043 Werte).
>
> **Protokoll** mit Feldkartenabgleich je Reiter, 19 Abweichungen (A‑1…A‑19), 17 Windows-Abnahmewegen und
> sechs offenen Punkten: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W11b_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: Automatikstart mit Balken und bedienbarem Fenster, Abbrechen, Endlage
> Übersicht, die 17 Flächen gegen ein Foto des Bestands, Konfiguration als Überlagerung mit Rücksprung,
> Variantenvergleich mit echtem Fortschritt, Optimierung modal über der WebView, die sechs CSV-Exporte, de/en,
> 125 %. Der zwölfte iOS-Lauf (33832613617) auf diesem Stand ist grün.
> **Windows-Abnahme 05.09.2026 („Allgemein bei Charts: das Zoomen funktioniert nicht"; PDF S. 8–9), drei
> Diagrammbefunde behoben (`db35a7b`, Protokoll § 10a):** **A‑1** nimmt den in A‑7 aufgegebenen Zoom zurück (Risiko
> R‑W11‑5 geschlossen): Jedes Renderer-Bild steht im neuen Baustein **`Diagramm`** (`Diagramm.razor` +
> `epos-diagramm.js`, über `ChartBild` für alle **34** Bilder, auch Kuchen, Ringe und Kennlinien) — Mausrad zoomt
> um den Zeiger, Ziehen verschiebt, Doppelklick/Taste 0 zurück, +/−, Pinch, Stufenanzeige „×2,5", Knopf „1:1",
> ganz im Browser ohne Neuzeichnen; **Datenzoom** für die Jahresganglinien (Bedarf, Wärmegang, Stromgang) über
> ein Rechteck (Umschalt+Ziehen oder Knopf „Bereich") → der Kern zeichnet mit einem `Achsenfenster` neu, die
> x-Achse trägt die wirklichen Jahresstunden in runden Schritten; ohne Fenster zeichnet jedes Bild byte-genau
> wie zuvor (iU7-Renderer unverändert PNG, `ChartProben` prüft jetzt 34 Bilder und 2 Gegenproben, die 30
> Bestandsbilder byte-gleich). JS über `import()` wie `epos-verlauf.js`, die `index.html` beider Wirte
> unverändert; WKWebView-Punkte (`wheel`+`ctrlKey`, `gesturestart` unterdrückt, `touch-action: none`) sind
> vorbereitet. **W11b‑B‑2** stellt die Diagramme der Ergebnisseite über die volle Rasterbreite (eine Regel
> `.epos-simerg-diagrammzeile` statt acht Sonderfälle, `min-height` 280 px). **W11b‑B‑3**: die Streuwolke B4
> „Leistung über Außentemperatur" hatte richtige Serien und Achsentitel, aber fünf gleichmäßig verteilte Marken
> (−18,2 … −5,3 … 7,7) — jetzt runde Teilung (1/2/2,5/5 × 10^k), aufgerundete Bereiche, Ränder für Legende und
> Titel. Abnahmepunkte 18–24; Punkt 22 (iPad-Pinch, Seite zoomt nicht mit) bleibt bis zur Geräteprüfung offen.
> Bewusste Vereinfachungen: die Null bleibt unten, B1 nimmt nur den Zeitausschnitt, „1:1" verwirft Bild- und
> Datenzoom, Doppelklick nur den Bildzoom.
>
> **Formularraster, Paket P3 (iU8‑E‑2, 05.09.2026, `d3fccf1`):** `ParameterReiter` der Simulationsseite (Klasse B, 23 Felder, neun Raster, alle einspaltig —
> unter jeder Zahl steht ihre Entsprechung; `Daten.Unterblaetter` und Grafikreiter bleiben).
>
> **W11b‑B‑4 (Windows-Abnahme V2, PDF 07.09.2026: „Die Charts sind optisch zu groß"), behoben in `a7c5052`, zusammengeführt
> in `e8f2bb3`:** Seit W11b‑B‑2 füllte jedes Renderer-Bild die Breite seines Rahmens; auf 1 920 px stand der 960 × 600
> gezeichnete Kuchen der Übersicht auf 1 850 × 1 156 Bildpunkten. Jetzt gilt EINE Maßregel im Stilblatt: Kein Renderer-Bild
> wird über die Breite hinaus gedehnt, in der es gezeichnet wurde — `--epos-diagramm-breit: 1240px` an `.epos-diagramm`,
> `--epos-diagramm-rund: 560px` an `.epos-diagramm--rund`; die 560 sind gerechnet (kleinste Schrift ≥ 11 px: Kuchen 19 px
> auf 960 → 556, Ring 16 px auf 720 → 495, der größere Wert für beide). `ChartBild` bekommt `Rund="true"`, `Diagramm` eine
> `Zusatzklasse`; die Bilder bleiben 960 bzw. 720 px breit (ChartProben unverändert), der Zoom bleibt, die Kuchenzeile
> steht weiter über beide Spalten. Wachen: `ChartBildTests` (4), `UebersichtReiterTests` (2). Nachweis: Kern 1762 / UI
> 3081 grün, Gate grün, Referenzlauf byte-gleich. Abnahme: A‑W11b‑B4‑1…5 (1 920 und 1 366 px ohne Rollen, Zoom, Ganglinien
> der übrigen Reiter höchstens 1 240 px, zwei Ringe je höchstens 560 px).
>
> **W11b‑B‑5/B‑6 (Windows-Abnahme V2, PDF 07.09.2026: „Auslegung optimieren — Texte überschneiden sich, Dialog stürzt nach
> kurzer Zeit ab"), behoben in `c9acb5b`, zusammengeführt in `76fafe5`:** **Die letzte WinForms-Fachmaske ist gefallen.**
> `Form_SpeicherOptimierung` (1 325 Z., iF22) hatte zwei Ursachen, beide mit Fundstelle belegt: Die **Überschneidung** war
> Koordinatenarbeit — `lbl_Aktuell` (x 14…414) und der Zielfunktionstext (x 310…1140) lagen beide auf y = 108, letzterer
> ragte 8 px aus seiner GroupBox. Der **Absturz** war ScottPlot: Jeder Lauf hängte über `Plot.Add.ColorBar` eine weitere
> Farbskala an denselben Plot, `Plot.Clear()` räumt aber nur Plottables, keine Panels — die Zeichenfläche schrumpfte je Lauf
> um 78 Bildpunkte und war **ab dem achten Lauf null** (an der Bibliothek nachgemessen: 6 → 13 Panels, DataRect 514 → 0);
> ein nicht endlicher Rasterwert (`c_pow = 0`) beendete sie zusätzlich im `OnPaint`, also außerhalb jedes `try/catch`.
> Neu sind `EPOS.Kern/Controller/SpeicherOptimierungCtrl` (Eingabeprüfung, `IProgress`/`CancellationToken`, DTO, CSV,
> Fortschritt GEDROSSELT: höchstens jeder 10. Punkt, höchstens alle 100 ms statt 400 Meldungen je Sekunde; Stützstellen
> 2…50, ≤ 5 000 Rasterpunkte), zwei `ChartRenderer`-Bilder (`Optimierungsraster` 860×560, `Schnittkurve` 720×460; nicht
> endliche Werte fallen im Renderer weg) und die Überlagerung `EPOS.UI/Dialoge/Strom/SpeicherOptimierungDialog.razor` der
> Ergebnisseite mit der Erklärung als `Herleitungszeile` UNTER der Formulargruppe. Die Rechnung ist unverändert — 34
> Kern-Fälle rechnen `dJ = E_a,äq − I·a(i_z,N)` an jedem Rasterpunkt nach; die bunit-Probe „fünf Läufe hintereinander"
> hält den Absturzfall maschinell. Damit fällt **das letzte Sprungziel**: `Sprungziel` ist leer (Registerstelle bleibt),
> `Sprungbruecke.cs` gelöscht, `ScottPlot.WinForms` aus dem Paketstand entfernt; `WindowsFormsApplication1` führt nur noch
> `Form_HelpPopup` und den `Hauptfensterrahmen` — **iF22 erledigt**. Nachweis: Release x64 0 Fehler, Kern 1824 / UI 3126
> grün, ChartProben **38 Bilder + 6 Gegenproben** (das Gate baut ChartProben seither mit, weil es nicht in `WP-Plan.sln`
> steht und mit `--no-build` einen alten Stand meldete), Formularkarte 122 (eine Maske), SQL 0, Referenzlauf byte-gleich.
> Abnahme auf Windows: A‑W11b‑B5‑1…11 (Überlagerung im selben Fenster; sechs Felder ohne Überdeckung bei 125/150 %; Lauf mit
> Fortschritt und Sperre; Abbrechen ohne halbes Bild; Rand-Hinweis; Eingabefehler sperrt den Start; Zoom in beiden Bildern;
> „Bestpunkt übernehmen" mit Rückfrage; CSV in deutschem Excel; **kein Absturz nach fünf Läufen**, Bilder gleich groß; ✕/Esc).
>
> **#170 (11.09.2026) — Mehrspeicherkonzept vom Windows-Rechner auf `ios_migration_september` integriert.** Der Sync `4c3b521`
> (162 Dateien, +48 617 Zeilen) brachte die Spezifikation „Simulation mehrerer Stromspeicher" v1.2
> (`Projekte/Spezifikation_Stromspeicher_Optimierung.md`, 15 Kapitel), den Python-Referenzkern `Projekte/Speichersimulation/code/`
> und die fertige C#-Umsetzung: `SpeicherEngine/Flotten*.cs` (Modell, Simulator, Wirtschaftlichkeit, Rainflow, Optimierer), das
> neue Projekt `SpeicherPlanung` (Google.OrTools 9.15.6755/SCIP hinter `IFlottenPlaner`, referenziert nur von der
> WinForms-Anwendung), acht neue Kern-Dateien (u. a. `SpeicherFlottenStudieCtrl`, `SpeicherFlottenProjektCtrl`,
> `SpeicherAuslegungCtrl`, `SpeicherZeitreihenImport`, `SpeicherFlottenCsvImport`), fünf Razor-Dialoge unter
> `EPOS.UI/Dialoge/Strom/`, den Reiter `StromspeicherReiter` und **Schemaschritt 73** (`Tab_SpeicherAuslegung`); die
> Testdatenbank steht auf 73 (119 Tabellen, 117 STRICT, 70 025 216 Byte), die Basis R6 bleibt. Anwenderentscheide: Zweig
> `ios_migration_september` als Integrationszweig, „docx behalten", „Wurzel bleibt Heimat", „.work behalten".
> Integration in vier Schritten: Merge von `ios_migration` (#62a, #166–#169, #168) mit einem Konflikt in
> `SpeicherParameterBlockTests` (`c51e839`); **#170b** Doku-Einordnung (`CLAUDE.md`, `EPOS.Kern/CLAUDE.md`, `BETRIEB_SQLITE.md`
> § 3, `LIESMICH`, `kern.yml`, Nachweisdokument; `8d5099c`); **#170a** acht Testklassen auf `EposBunitContext` (der rote
> Rainflow-Fall war Kulturabhängigkeit), fünf Dubletten zu Verweisdateien, lokale Pfade ersetzt (`11bccf8`); **#171** 291 ×
> CS1591 durch echte Dokumentation der Flotten-Verträge, `ModuleInitializer` (CA2255, AOT-Risiko) durch
> `StromspeicherzweigEinhaengen()` am Laufeinstieg ersetzt, zwei Null-Warnungen (`656e3a0`). Gate auf `656e3a0`: Kern 2 418 grün,
> UI 3 453 grün, SpeicherEngine 382, SpeicherPlanung 27 + 1 übersprungen, 5 eindeutige Warnungen, SQL-Prüfer 0 von 1 319, ChartProben 44,
> Referenzlauf 1030/1007/1017/1045 byte-gleich gegen R6, **OR-Tools baut und rechnet auf ubuntu**. Offen: #170c (Verhalten
> ohne Planer auf iOS), #172 (tote Links in `LIESMICH`), #173 (xUnit-Analysewarnungen), Zusammenführung mit `ios_migration`
> nach #62b.
> **Offene Punkte des Mehrspeicherkonzepts (aus „Ehrliche Grenzen", Doku_Mehrspeicher_Konzept_und_Umsetzung.md, 11.09.2026)** —
> zur Aufnahme ins Register § 8 des Umsetzungskonzepts bei der Zusammenführung von `ios_migration_september`:
> - **SP‑O‑1 Oracle-Jahreslauf:** 35 040 Intervalle mit Horizont 192 / Neuplanung 96 brechen nach > 60 s ab (Test übersprungen); Laufzeit und Dialogtauglichkeit unbelegt. Entscheid: Grenze für den Dialog (Kandidatenzahl × Horizont) oder Hintergrundlauf mit Fortschritt.
> - **SP‑O‑2 Rainflow ohne Alterungswirkung:** Miner-Schaden wird ausgewiesen, aber Kapazität/Leistung altern im Lauf nicht; kein Kalender-, Temperatur- oder Ersatzmodell.
> - **SP‑O‑3 Solver nur Windows:** Google.OrTools/SCIP hängt an `SpeicherPlanung` (WinForms). iOS und jede andere Schale ohne registrierten `IFlottenPlaner` haben nur die reaktiven Ziele PvGreedy/PeakShaving; PvPlanung/Arbitrage/MultiUse müssen dort als „nicht verfügbar" erscheinen (#170c prüft). → **eingelöst 11.09.2026 (#170c: `FlottenPlanerLage`, Sperre im Betriebseditor); Gerätebeleg auf dem iPad am 11.09.2026 durch den Anwender geführt.**
> - **SP‑O‑4 Optimalität:** MILP-Optimum gilt je Horizont; rollierender Jahreslauf und endliches Größenraster sind nicht global optimal — im Ergebnis so benennen (steht im Dialog).
> - **SP‑O‑5 Tarife:** keine Monatspeaks, Tarifstaffeln, Steuern, mehrere Abrechnungsperioden; eine Abrechnungsperiode je Variante.
> - **SP‑O‑6 Mehrjahresalterung:** Projektjahre und SoC-Mitnahme umgesetzt; Degradation, Ausfall, Reparatur fehlen (spätere Zustandsmodelle).
> - **SP‑O‑7 Wärmekopplung:** BHKW-Fahrplan und elektrische Zusatzlast sind Eingaben; keine gemeinsame Wärme/Strom-MILP (bewusst, Kap. 13.5).
> - **SP‑O‑8 Referenzlauf:** Linux-Gate 11.09.2026 byte-gleich für 1030/1007/1017/1045; die vier Projekte führen keine Flotte — ein Referenzprojekt MIT Flotte (Basis R7) fehlt, sonst ist der Flottenpfad ohne Regressionsnetz. → **eingelöst 11.09.2026 (#174: Basis R7, Projekt 1046).**
> - **SP‑O‑9 Repository:** `.work/` (73-MB-Datenbankkopie) und fünf docx auf Anwenderwunsch im Zweig belassen (11.09.2026); Klongröße + ~80 MB.
> - **SP‑O‑10 Arbeitslose Flotte (Anwenderrückmeldung 11.09.2026, #179):** Peak-Ziel fest 50 kW vorbelegt (`SpeicherFlottenStudieCtrl.cs:49/:64`), Netzladung per Vorgabe verboten, Start-SoC = Minimum — die Flotte lädt und entlädt im ganzen Jahr nicht, „Mit Flotte" = „Ohne Speicher", und keine Diagnose sagt es; Behebung Paket P1 des Konzepts `Projekte/Konzept_Stromspeicher_Dialoge_EPOS-Plan.md`, wartet auf SD‑Q3/Q4/Q5 → **entschieden 11.09.2026 (Empfehlung), Paket P1 = #183.**
> - **SP‑O‑11 Darstellung (Anwenderrückmeldung 11.09.2026, #179):** Flottendialog als Überlagerung mit vier Reitern und Ergebnistabelle („Fenster in Fenster"), Diagramm ohne Reihenwahl/Dauerlinie/Datenzoom entgegen Hausregel `Doku_Simulationsergebnis_Darstellung.md` § 5, keine Größen-Sicht trotz Rastersuche; Zielbild: freie Ansicht `STROMSPEICHER_AUSLEGUNG` der `AppWurzel` mit Ablaufleiste, Ergebnis als eigener Schritt, Rasterkarte und Schnittkurve für die Flotte (Pakete P2–P4), Mockup `Projekte/Mockup_Stromspeicher_Ansicht_2026-09-11.html`; wartet auf SD‑Q1/Q6/Q7 → **entschieden 11.09.2026 (Empfehlung), Paket P2 = #184, P3/P4 folgen.**
> - **SP‑O‑12 Zwei Speicherpfade:** Einzelspeicher (regressionsgeprüft, zwölf Projekte) und Flotte (1046) rechnen getrennt; Zusammenführung erst nach SD‑Q2 → **11.09.2026: nicht jetzt (Empfehlung), bleibt offen.**
>
> **#170c/#172/#173 (11.09.2026) — Nacharbeiten der Integration.** **#170c (SP‑O‑3 eingelöst, `4f3cf8e`):** Drei der fünf
> Flottenziele planen und brauchen einen `IFlottenPlaner`, den nur die WinForms-Anwendung registriert (`Program.cs`);
> `EPOS.iOS/MauiProgram` und die Linux-Prüfstände setzen keinen, und die Wahl eines planenden Ziels endete erst im Lauf mit
> einer `InvalidOperationException`. Jetzt: Auskunft `EPOS.Kern/Controller/FlottenPlanerLage.cs` (`Verfuegbar`, `Grund`,
> `IstPlanend`, `ZielMoeglich`; Fabrik gesetzt UND liefert einen Planer), `Auswahlfeld` mit gesperrten Einträgen samt Grund,
> Warn- bzw. Hinweisbanner im gemeinsamen Baustein `SpeicherFlottenBetriebEditor` (Dialog und Reiter), Vergleichsknopf
> gesperrt, solange Ziel oder Auslegungsraster planen; vier Ressourcenschlüssel de/en. Mit gesetzter Fabrik ist nichts
> gesperrt — Windows unverändert; `EPOS.iOS` und `SpeicherPlanung` unberührt, Gerätebeleg im Simulator offen (kein iOS-Lauf,
> Regel vom 09.09.2026). **#172:** fünf historische Links in `Referenzlaeufe/LIESMICH.md` auf gelöschte Basisordner entlinkt,
> `EPOS.Kern/CLAUDE.md` `Model/` (54). **#173:** 14 Codestellen in sieben Testdateien auf die xUnit-Idiome, 28 Analysewarnungen
> → 0; das Gate zählt Warnungen seither mit `[A-Za-z]+[0-9]+`. Gate auf `ed83c66`: Kern 2 432 grün, UI 3 468 grün, SpeicherEngine 382,
> SpeicherPlanung 27 + 1 übersprungen, 5 eindeutige Warnungen, SQL 0 von 1 319, ChartProben 44, Referenzlauf byte-gleich.
>
> **#174 (11.09.2026, SP‑O‑8 eingelöst, `088a6a1`, Merge `65c767f`) — Prüfprojekt 1046 und Referenzbasis R7.** Keines der zwölf
> Referenzprojekte betrat den Flottenpfad des Projektlaufs (`SpeicherFlottenProjektCtrl` → `SpeicherEngine/FlottenSimulator`);
> eine stille Änderung an Verteilung, Reserve, Richtungswirkungsgrad oder Netzbilanz wäre nur im Prüfstand aufgefallen. Jetzt:
> **1046 „Prüfprojekt Speicherflotte"**, Tiefkopie von 1007 (höchste Bezugsspitze 19,776 kW der drei Projekte mit Strombedarf
> und PV, führt schon Einzelspeicher — dasselbe Projekt rechnet mit Flotte aus den Einzelpfad, mit Flotte an den Flottenpfad),
> zwei Einheiten 24 kWh an 10/12 kW und 16 kWh an 6/7 kW mit getrennten Richtungswirkungsgraden, SoC-Bändern, Reserve und
> Hilfsverbrauch, Ziel `PeakShaving` gegen 16 kW, Verteilung `Kaskade`, kein Solver (planende Ziele brauchen OR-Tools, das der
> plattformfreie Referenzlauf bewusst nicht bindet). Die Größe ist am Peak-Ziel gemessen, nicht geraten: längste Exkursion über
> T aus `reststrom_viertelstunde.csv` 101,5 kWh (T=12), 33,5 kWh (T=16), 8,5 kWh (T=18) bei 34,4 kWh nutzbar — T=16 hält 20
> Überschreitungen und damit den Freigabeweg der Peak-Reserve im Netz; die Auftragsempfehlung 190 kWh an 80 kW wäre vor
> 19,8 kW in jedem Intervall unbegrenzt gewesen. Wirkung: Bezugsspitze 19,7762 → **16,7428 kW** (−15,3 %), Netzbezug
> 50 538,7 → 51 611,0 kWh (+2,1 %), Intervalle über 16 kW 2 864 → 20; A entlädt 1 761×/lädt 877×, B 1 177×/734×, „nur B
> entlädt" 1 152× (Kaskade), beide SoC-Bänder voll ausgefahren, gleichzeitiges Laden und Entladen 0×. `Referenzlauf/Ergebnisexport.cs`
> führt neu vier Flotten-Ganglinien je Einheit und 42 Skalare `Flotte.*` (Netzbilanz, Referenz ohne Speicher `Flotte.Ref.*`,
> je Einheit Energie, Vollzyklen, Miner-Schaden, Rainflow-Zyklen), alle nur, wenn die Flotte gerechnet hat. Basis
> `Referenzlaeufe/2026-09-11_R7_Speicherflotte`: **13 Projekte, 345 CSV, 1 937 Skalare**, die zwölf alten **byte-gleich zu R6**,
> zweiter Lauf 13/13 byte-gleich, Toleranzvergleich 13/13 PASS (3 777 497 Werte); Projekt wiederholbar aus
> `Referenzlaeufe/Skripte/pruefprojekt_1046_speicherflotte.py` (Testdatenbank 25 Projekte, Schemastand 73, 117 STRICT).
> `kern.yml` rechnet 1030/1007/1017/1045/**1046** gegen R7, `ios.yml` nur den Basispfad (kein iOS-Lauf ausgelöst), dritte
> Einfrierregel in `CLAUDE.md` und `LIESMICH.md`. Gate auf `65c767f`: Kern 2437, UI 3487, SpeicherEngine 382, SpeicherPlanung
> 27 + 1 übersprungen, 5 eindeutige Warnungen, SQL 0 von 1 319, ChartProben 44, Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#178 (11.09.2026, `cc90fc9`, Merge `e7d6426`) — Schemaschritt 74: `Tab_SpeicherAuslegung` STRICT.** Befund aus dem Nachweis zu
> iOS-Lauf 41: Die Tabelle aus Schritt 73 war die einzige Fachtabelle der Testdatenbank ohne STRICT (119 Tabellen, 117 STRICT).
> Schritt 74 legt sie in einer Transaktion als STRICT neu an (CREATE neu, INSERT SELECT, DROP, RENAME, Index; wiederholbar,
> `PRAGMA defer_foreign_keys`); die eine Quelle `SpeicherAuslegungCtrl.SQL_TABELLE` trägt STRICT für Migration, stille
> Selbstanlage und `Werkzeuge/Testdatenbankschema`; Schritt 73 bleibt unverändert. `SchemaStand.Zielversion = 74`, Testdatenbank
> 119 Tabellen / **118 STRICT** / 25 Projekte, Dateigröße unverändert, die Zeile `@Projektflotte` (1046) byte-gleich (SHA-256);
> Gegenprobe „Tabelle ohne STRICT" leer. `Migration74Tests` 7 Fälle, Auslieferungsvorlage-Probe auf 118, Doku (LIESMICH, BETRIEB_SQLITE
> § 6.5, CLAUDE.md, EPOS.Kern/CLAUDE.md, ios.yml-Kommentar, Nachweisdokument, Spezifikation 14.4) nachgezogen. Referenzlauf 13/13
> byte-gleich gegen R7, kein iOS-Lauf (Regel vom 09.09.2026; das STRICT-Gate liest die Erwartung aus der Seed-Datenbank). Gate auf
> `e7d6426`: Kern 2444, UI 3487, SpeicherEngine 382, SpeicherPlanung 27 + 1 übersprungen, 5 eindeutige Warnungen, SQL 0 von 1 326,
> ChartProben 44, Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#185 (11.09.2026, `deb2922`, Merge `1ba2f98`) — Projektlauf mit aktivierter Flotte bricht nicht mehr an fehlenden
> Kostensätzen ab.** Anwenderbefund (Projekt „Stromspeicher mit Wärmepumpe"): `InvalidOperationException` „Im Dialog fehlen die
> Investitionskoeffizienten" aus `SpeicherAuslegungCtrl.KostenAufloesen` über `SpeicherFlottenProjektCtrl.Rechnen` in
> `SimulationControl.SpeicherlaufAusfuehren`. Drei Ursachen belegt (Wegwerfprobe gegen `28fd7a6`): Der Dialogstand `@Aktuell`
> erreicht den Projektlauf mit `Investitionsquelle = Dialog`, aber ohne Sätze (`Vorbelegung` setzt `BetriebVorhanden = false`);
> `KostenAufloesen` verlangte die Sätze auch für Einheiten mit eigenen Kosten (`Konfiguration` überspringt sie); und der
> Projektlauf braucht gar keine Sätze — sie erreichen nur `FlottenWirtschaftlichkeit`, Jahreskonten entstehen im Projektlauf
> nicht. Jetzt: Modus `KostenPflicht` an `Vorbereiten`/`AusQuellenVorbereiten` (eine Methode, kein Duplikat), Kennzeichen
> `SpeicherKostensaetze.NichtBewertbar`, `FlottenProjektPruefung` mit `Pruefe(projektId)` (Probleme blockieren, Hinweise
> nicht; fehlende Sätze sind ein Hinweis), `Aktivieren` schreibt den vollständigen Stand `@Projektflotte`, der Abbruch in
> `SpeicherlaufAusfuehren` bleibt gewollt (ein Projekt mit aktivierter Flotte rechnet nie still ohne sie), der Text nennt den
> Ausweg; 14 Ressourcen `FLOTTE_MSG_*` de/en. Die Ausnahme erreicht den Anwender über `SimulationErgebnisHuelle.Laufen` als
> Rückmeldung auf der Ergebnisseite, nicht als unbehandelte Ausnahme. 13 neue Fälle (`SpeicherFlottenProjektKostenTests`,
> `FlottenPlanerLageTests`). Gate auf `1ba2f98`: Kern 2483, UI 3490, SpeicherEngine 382, SpeicherPlanung 27 + 1 übersprungen,
> 5 eindeutige Warnungen, SQL 0 von 1 326, ChartProben 44, Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#183 (11.09.2026, `710b4c3`, Merge `14ecdde`) — Stromspeicher-Dialoge Paket P1 (SD‑Q3/Q4/Q5, Empfehlung).**
> `SpeicherEngine`: `FlottenDiagnose` am Studienergebnis (je Einheit und Flotte: Intervalle mit Entlade-/Ladeanforderung,
> Ladedeckel 0 durch Peak-Regel bzw. Netzladeverbot, Entladeanforderung ohne Energie, Lade-/Entladeenergie, `Arbeitslos`,
> benannte Gründe) — nicht im Referenzexport, kein Rechenwert ändert sich. Kern: `FlottenPeakZiel` (Vorschlag
> `H₀ = max(Referenzspitze − Σ Entladeleistung, max Tagesminima)` mit Herleitung, zweistufiger Rückfall statt fest 50 kW;
> `PeakZielBestimmen` per Bisektion ≤ 12 Läufe mit `IProgress`/`CancellationToken`, nur reaktive Ziele), `FlottenPlausibilitaet`
> (fünf Kennungen: Peak-Ziel unter Tagesminimum ohne Netzladung, über Referenzspitze, Betriebskosten < 0,1 % der Investition,
> Start-SoC auf Minimum, Flotte arbeitslos; `Pruefhinweise` im Ergebnis), `FlottenVorgaben.NetzladungFuer` (PeakShaving →
> erlaubt, nur beim Anlegen; serialisierte Vorgabe bleibt `false`, der Stand 1046 trägt `NetzladungErlaubt = true`
> ausdrücklich). Spezifikation 5.1 um „N dauerhaft über H" ergänzt (Fassung 1.3). 32 neue Fälle. Die UI-Verdrahtung
> (Diagnosebanner, Knopf „Peak-Ziel bestimmen") kommt mit P3.
>
> **#186 (11.09.2026, `c08906f`, Merge `97e17a6`) — Abnahmeliste: Kostenverwaltung und Gruppenkopf.** Der Spaltenkopf
> „Nutzungs-⏎dauer [a]" trug einen Zeilenumbruch aus WinForms-Zeiten, der in HTML zu einem Leerzeichen kollabierte, dazu
> `nowrap` und 90/60-px-Spuren; die Einheit des Zahlenfelds quoll aus der Satzspalte in „Betrag netto" (Hausbefund W6‑B‑4,
> jetzt auch im Zeilenraster); der Gruppenkopf-Balken hatte keinen Innenabstand (140 von 155 Einsätzen ohne Symbol). Jetzt:
> Umbruch aus beiden Ressourcenwerten, Kopfzellen brechen um, `min-width: 0` im Zeilenraster, Gruppenkopf als Raster mit fester
> Symbolspur und `padding-inline` — gleicher Textbeginn mit und ohne Symbol. Zwei Stilregel-Wachen, ein bunit-Test gegen den
> echten Ressourcenwert; `Resource.Designer.cs` neu erzeugt (Kommentarvorschau, −13 Zeichen). Gate auf `4bf5fce`: Kern 2483,
> UI 3490, SpeicherEngine 388, SpeicherPlanung 27 + 1 übersprungen, 5 eindeutige Warnungen, SQL 0 von 1 326, ChartProben 44,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#190 (11.09.2026, `ef374b9`, Merge `7270204`, Anwenderentscheid HK‑E‑1a) — Erzeuger ohne Kaskadenplatz.** Abnahmeliste,
> Projekt „PV mit Heizkessel": Spitzenkessel 0,00 MWh, Restwärmebedarf 11,55 MWh, Deckung 0 %. Ursache: Ein Wärmeerzeuger rechnet
> nur mit Platz in `Tab_Einstellungen.Tool_1..4` (`SimulationControl.cs:861–871`), den einzig der Knopf „+ aufnehmen" der
> Simulationskonfiguration vergibt (`Kaskade.Aufnehmen`, ein Aufrufer); die verfügbaren Karten waren dort ausgeblendet, und
> niemand warnte. Vier der dreizehn Referenzprojekte tragen dieselbe Lücke (1007 und 1046 Heizkessel, 1008 Heizkessel, 1017
> Wärmepumpe). **HK‑E‑1a (Anwender, 11.09.2026): melden, nicht automatisch aufnehmen** — ein automatisches Aufnehmen hätte drei
> CI-Projekte und damit die Basis R7 geändert (R8), und ein Platz ist eine gespeicherte Anwendereinstellung. Jetzt:
> `SimulationLaufCtrl.ErzeugerOhneKaskadenplatz` (Kennungen `LAUF_W_ERZEUGER_OHNE_KASKADENPLATZ` und `…_STROMPLATZ` für
> PV/Speicher auf `Tool_5/6`, `Warnbefund.Steuerwert`), Protokollwarnung im Lauf hinter `WarnkriterienMelden` (nicht im
> Referenzexport), Übersichtszeile „(nicht in der Kaskade)", Konfigurationsseite mit Hinweisleiste „n Erzeuger sind angelegt,
> aber nicht in der Simulation" und Knopf „einblenden"; verfügbare Karten MIT Anlage beim ersten Blick sichtbar,
> Katalog-Platzhalter bleiben verborgen (`ErzeugerZeile.HatAnlage`). Sechs Ressourcen de/en; zehn neue Fälle; Referenzlauf
> 13/13 byte-gleich gegen R7. Gate auf `7270204`: Kern 2507, UI 3512, SpeicherEngine 388, SpeicherPlanung 27 + 1 übersprungen,
> 5 eindeutige Warnungen, SQL 0 von 1 327, ChartProben 44, Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#184 (11.09.2026, `2a77bd4`, Merge `12db7da`) — Stromspeicher-Dialoge Paket P2 (SD‑Q6/Q7, Empfehlung).**
> `SpeicherFlottenErgebnisAnsicht` neu nach Konzept 2.2/2.3: Hinweise als Warnbanner, Zeile „Berechnete Betriebsführung" mit
> Knopf „Betriebsführung ändern" (der Dialog wechselt auf den Reiter), vier Kennzahlkacheln, Vergleichstabelle „Ohne Speicher ·
> Mit Flotte · Δ" mit Vorzeichenregel je Kennzahl (`FlottenVergleichszeile.NegativIstBesser`) und einklappbaren Nullzeilen,
> Jahresprojektion als Bild (`ChartRenderer.Jahresprojektion`, Balken je Jahr, Linie kumuliert, Ersatzjahre markiert) mit
> aufklappbarer Tabelle, Speicher-Kennzahlen je Einheit, CSV-Export. Diagramme nach Hausregel § 5: über jedem Bild „sortiert",
> ein Schalter je Reihe (Netz ohne/mit, Peak-Ziel, Speicher gesamt und je Einheit, Ladezustand je Einheit als zweite Achse),
> Datenzoom, Zeitraum Jahr/Woche/Tag mit Navigator — `SpeicherFlottenAnzeigeCtrl.Bilder(ergebnis, startTag, tage, speicher,
> reihen, sortiert, netzbereich, socbereich)` reicht `ladezustand`/`sortiert`/`fenster` an den unveränderten Renderer durch,
> ohne Datenbank; Bildschlüssel-Zwischenspeicher statt Stapel. **349 Ressourcen `FLOTTE_*` de/en** — die fünf Flottenkomponenten
> und drei Textbündel sind erstmals lokalisiert. ChartProben 44 → 46 (39 Bilder, 7 Gegenproben). 37 neue Fälle; Referenzlauf
> 1030/1046 byte-gleich. Kein Diagnosebanner (P3), Kandidatentabelle nur lokalisiert (P4). Beim Merge: Ressourcenkonflikte am
> Dateiende mit #185/#190 durch Vereinigung gelöst, Designer neu erzeugt. Gate auf `12db7da`: Kern 2507, UI 3512, SpeicherEngine 388,
> SpeicherPlanung 27 + 1 übersprungen, 5 eindeutige Warnungen, SQL 0 von 1 327, ChartProben 46, Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#188 und #189 (11.09.2026, `4d62c31` und `de20c6a`, Merge `3c5ff0b`) — zwei Punkte der Abnahmeliste `iOS_Migration_Probleme`.**
> **#188 Schema kürzt lange Bezeichner:** Ein Knotentitel, der breiter ist als seine Spalte, lief im Hydraulikschema über den
> Kasten hinaus. Jetzt kürzt `SchemaLayout.TitelKuerzen` im Kern auf die verfügbare Breite (Spaltenbreite abzüglich beider
> Knotenränder und des Rangversatzes der Kaskade, gemessen über `ZEICHEN_BREITE`) mit „…"; `Knotenflaeche.TitelAnzeige` trägt
> die Anzeigefassung neben dem vollen `Titel`, `Schema.razor` zeichnet sie, Tooltip und `aria-label` führen weiter den vollen
> Namen; die Windows-Hülle (`SimulationKonfigHuelle.SchemaAbbilden`) reicht das Feld durch. Acht neue Fälle (sechs Kern, zwei
> bunit). **#189 `SettingsEinstellungen` prüft den Schlüssel vor dem Zugriff:** `AusSettings` fragte den Wertindexer von
> `Properties.Settings` und fing die `SettingsPropertyNotFoundException` als Steuerfluss; jetzt prüft der Sammlungsindexer
> (`Properties[schluessel] == null → null`), der Fangblock bleibt für eine verdorbene `user.config`. Ein Kern-Test belegt das
> Verhalten beider Indexer am plattformfreien `Properties.Settings`-Typ. Referenzlauf 1030 byte-gleich (reine Anzeige und
> Einstellungspfad). Gate auf `3c5ff0b`: Kern 2532, UI 3541, SpeicherEngine 388, SpeicherPlanung 27 + 1 übersprungen,
> 5 eindeutige Warnungen, SQL 0 von 1 342, ChartProben 46, Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#187 (11.09.2026, `10a394a`, Merge `275479e`) — Abnahmeliste: doppelter Dialogtitel.** Die Überlagerung „Position bearbeiten"
> trug den Titel zweimal — einmal im Kopf der `Ueberlagerung`, einmal in der eingebetteten Komponente, beide aus demselben
> Ressourcenschlüssel (`VPOS_TITEL`, `KFAK_TITEL`, `KCASE_TITEL`). Eine Sichtung aller 102 Überlagerungen des Bestands (87 mit
> Titel) fand **22** solche Dopplungen: die vier Kostendialoge in `KostenKomponenteDialog`, elf `NamensDialog`-Einbettungen der
> Katalog- und Bedarfsdialoge, die Editor-Überlagerungen von `BhkwDialog` und `HeizkesselDialog`, `KlimazonenkarteDialog` in
> `QuelleErdreichDialog` und `PufferSpProjektDialog` in `QuellePufferspeicherDialog`/`WaermesenkeDialog`. Alle folgen jetzt der
> **Hausregel „Ein Titel, eine Stelle"** (W11b‑B‑9 als Regel in `EPOS.UI/CLAUDE.md`): Trägt die Überlagerung einen Titel, zeigt
> die Komponente keinen eigenen — Bauart a) `TitelText` bleibt leer (Regelfall, in den Hüllen `KostenKomponenteHuelle`,
> `KostenfaktorKatalogHuelle`, Helfer `OhneTitel`), Bauart b) `[Parameter] bool TitelAnzeigen = true`, wo `TitelText` noch etwas
> anderes speist (Bildbeschreibung der Karte, Gruppenkopf des Bestandsblocks). Eine geteilte `Gaben()`-Methode, die auch ein
> eigenständiges Fenster bedient, bleibt unverändert. **Wache** `EPOS.UI.Tests/UeberlagerungstitelTests.cs` (fünf Fälle, zwei
> Bauarten: Markup mit identischem Bezeichner; Hülle mit gleichem Schlüssel in `.Titel` und `TitelText` derselben Methode) mit
> Gegenprobe am eingefrorenen Vorher-Stand; drei bunit-Fälle. Restfall: die dritte Einbettung von `PufferSpProjektDialog` in
> `SimulationKonfigSeite.razor` (Abgrenzung zu #190) → **#194** (`5c44276`, Merge `94bff2f`): `TitelAnzeigen="false"` an der
> dritten Einbettung; die Wache prüft seither an jeder Einbettung mit Überlagerungstitel den Parameter selbst, auch ohne
> `TitelText=` an der Stelle, und findet den alten Stand rot; bunit-Fall in `SimulationKonfigSeiteTests`. Gate auf `275479e`: Kern 2532, UI 3541, SpeicherEngine 388,
> SpeicherPlanung 27 + 1 übersprungen, 5 eindeutige Warnungen, SQL 0 von 1 342, ChartProben 46, Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#193 (11.09.2026, `e0c81b0`, Merge `8b2bfc8`) — Stromspeicher-Dialoge Paket P4 (Konzept 2.5, Entscheid 11.09.2026): Größen-Sicht
> der Flotte.** Die Rastersuche des Flottenoptimierers lieferte Kandidaten nur mit Kapazität, Leistung, Kapitalwert und
> Zulässigkeit — ein arbeitsloser Kandidat war von einem arbeitenden nicht zu unterscheiden, und eine Größen-Sicht gab es für die
> Flotte nicht (Befund SP‑O‑11). Jetzt trägt `FlottenKandidatZusammenfassung` je Kandidat Durchsatz, Vollzyklen, Bezugsspitze,
> Betriebsersparnis, `Arbeitslos` (aus der Diagnose P1), C-Rate, Rasterindex und die Einheiten (`FlottenKandidatEinheit`), gefüllt
> aus dem Kandidatenlauf, den der `FlottenOptimierer` ohnehin rechnet. Der Kern liefert die Anzeigedaten ohne Datenbank
> (`SpeicherFlottenAnzeigeCtrl.Groessen`: `Rasterdaten` Summe oder je Einheit, Löcher als NaN; `Schnittdaten` über der Kapazität
> bei fester C-Rate und `SchnittdatenLeistung` bei fester Kapazität; `Rasterbild`/`Schnittbild`; Kandidatenprofil und -zeilen
> im Katalogfilter-Muster). `ChartRenderer.Optimierungsraster` bekommt zwei optionale Parameter — Schraffur der unzulässigen
> Zellen und eine Fußzeile mit dem SP‑O‑4-Hinweis („endliches Raster") —, die 39 Bestandsbilder bleiben byte-gleich. Der
> Baustein `SpeicherFlottenGroessenAnsicht` (Parameter `Ergebnis`, `Einheiten`, Ereignis `KandidatUebernehmen`) zeigt Rasterkarte
> mit Einheitenwahl, die zwei Schnittkurven mit Schiebern (C-Rate bzw. Kapazität, Vorbelegung am Optimum) und die sortier- und
> filterbare Kandidatentabelle mit Optimum-Zeile, Arbeitslos-Kennzeichen und „Kandidat übernehmen"; 51 Ressourcen
> `FLOTTE_GROESSEN_*` de/en. **Noch nirgends eingehängt** — die Ansicht `STROMSPEICHER_AUSLEGUNG` aus P3 (#192) bindet ihn
> nach beiden Merges ein. ChartProben 46 → **49** (41 Bilder, 8 Gegenproben); 42 neue Fälle (6 Engine, 19 Kern, 17 bunit).
> Gate auf `8b2bfc8`: Kern 2532, UI 3541, SpeicherEngine 394, SpeicherPlanung 27 + 1 übersprungen, 5 eindeutige Warnungen,
> SQL 0 von 1 342, ChartProben 49, Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#195 (11.09.2026, `7e3434b`, Merge `b17af66`) — SQL-Dialektprüfer: Namensauflösung in drei Stufen.** Das Gate auf `8b2bfc8`
> meldete eine Fundstelle in `KomponentenUebernahmeCtrl.cs:344` — `DELETE FROM [<div class="epos-dialog-kopf">…]`: Die
> Schleifenvariable `kind` (`foreach (string kind in plan.Kindtabellen)`) wurde über die gleichnamige Fixture-Konstante
> `const string kind` der neuen Wache `UeberlagerungstitelTests` (#187) aufgelöst, weil der Kurzname damit in genau einer Klasse
> vorkam. Zwei Ursachen im Werkzeug: `_lokale_namen` kannte nur `string x = …`/`var x = …`, nicht `foreach`, Parameter
> (`out`/`ref`/`in`/`params`/`this`), Deklarationen ohne Zuweisung und Dekonstruktionen; und der Konstantenkatalog kam aus ALLEN
> `.cs` des Repos statt aus dem Prüfbereich (`WURZELN`). Jetzt löst `_konstante` in drei Stufen auf — eigene Klasse geht immer
> vor; erst ohne eigenen Treffer sperrt ein lokaler Name; sonst der Kurzname einer fremden Klasse (Hausregel W6.7) — und der Katalog
> stammt aus `dateien_im_bereich`. Nebenbefund: Sechs Klassen (`Z_ProjGebCtrl`, `Z_ProjektGebGanglinieCtrl`, `StromspeicherSimCtrl`,
> `Z_ProjektBrauchwasserCtrl`, `Z_ProjektProzesswaermeCtrl`, `Z_ProjektStromverbraucherCtrl`) führen eine klasseneigene
> `const string sql` neben einem Parameter oder einer Variablen `sql`; die alte Reihenfolge „lokal vor eigener Klasse" hatte deren
> 15 Konstantenbezüge seit jeher stillschweigend übersprungen — sie werden jetzt geprüft: **1 342 SQL-Texte, 0 Fundstellen,
> 215 dynamisch, 1 127 in Ordnung** (vorher 1 327/1/214/1 112; per CSV-Vergleich keine andere Klassifikation geändert).
> Selbsttest 35 Anweisungen (drei neue Tokenfälle: foreach-Name, Katalogbereich, eigene Klasse vor lokalem Namen); LIESMICH.
> Gate auf `b17af66`: Kern 2532, UI 3541, 5 eindeutige Warnungen, SQL 0 von 1 342, ChartProben 49, Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#192 (11.09.2026, `0088941`, Merge `ef55097`) — Stromspeicher-Dialoge Paket P3 (SD‑Q1/Q3/Q5, Empfehlung): die Ansicht
> `STROMSPEICHER_AUSLEGUNG`.** Der Flottendialog war eine Überlagerung mit vier Reitern („Fenster in Fenster", SP‑O‑11). Jetzt
> ist die Stromspeicher-Auslegung eine **freie Ansicht der `AppWurzel`** (`EPOS.UI/Seiten/Strom/StromspeicherAuslegungSeite.razor`,
> Muster #62b): Kopfzeile mit Projekt/Variante und Rückweg über `Dienste.Navigation`, **Ablaufleiste** 1 Speicher · 2 Daten &
> Kosten · 3 Betriebsführung · 4 Berechnen · 5 Ergebnis (Schritt 5 erst nach einem Lauf, „Eingaben geändert" markiert ihn
> veraltet), **Modus Flotte/Einzelspeicher** als zwei Modi EINER Ansicht (SD‑Q1) — der Einzelspeicher bringt seine Blätter
> Suchraum, Betrieb und Ergebnis mit Rasterkarte/Schnittkurve mit, rechnerisch bleiben die Pfade getrennt (SD‑Q2). Die Blätter
> sind die bestehenden Editoren (`SpeicherFlottenEditor`, `SpeicherAuslegungEditor`, `SpeicherFlottenBetriebEditor`,
> `SpeicherFlottenErgebnisAnsicht`); Überlagerungen nur noch für CSV-Spaltenzuordnung, Prognosen und die Rückfrage beim
> Verlassen. **`SpeicherFlottenDialog` und `SpeicherOptimierungDialog` fallen** (iZ5); der Stromspeicher-Reiter der Ergebnisseite
> wechselt die Ansicht (W16c‑E‑3). **Datenbank- und Rechenweg im Kern:** `StromspeicherAuslegungCtrl` (653 Zeilen — Vorgaben,
> Profile speichern, Flotte/Einzel vorbereiten und rechnen, Aktivieren/Deaktivieren, CSV, Vorprüfung, Peak-Ziel, eigener
> Simulationslauf); `SimulationErgebnisHuelle.Flotte.cs` fällt, `.Optimierung.cs` schrumpft 400 → 190 Zeilen, die neue
> `StromspeicherAuslegungHuelle` behält Dateiwähler, `Task.Run`, Abbruch und Fensterbesitz. Die Ergebnisseite meldet ihre gerechnete
> `SimulationControl` VOR dem Ansichtswechsel am Controller an (`Anmelden`, Einmal-Übergabe); ohne Anmeldung bietet die Seite den
> eigenen Lauf über den Baustein `Fortschritt` an (W11a) — nie zwei Läufe nebeneinander. **Diagnosebanner** (`FlottenDiagnosebanner`)
> über den Kacheln bei `Diagnose.Arbeitslos` mit Gründen, Prüfhinweisen und Abhilfeknöpfen (Peak-Ziel bestimmen, Netzladung
> erlauben, zu Schritt 3); **Peak-Ziel-Block** (`PeakZielBlock`) mit Herleitung aus `FlottenPeakZiel.Vorschlag`, „übernehmen" und
> „Peak-Ziel bestimmen…" nebenläufig mit Fortschritt und Abbruch, Ergebnis als Rückfrage; **Vorprüfung** vor „Berechnen" als Banner
> ohne Blockieren; **Netzladung je Ziel** beim Zielwechsel (SD‑Q5); Start-SoC-Hinweis (SD‑Q4). 46 Ressourcen de/en am Ende der
> resx; Formularkarte 0/0/0; Windows-Hülle baut mit `EnableWindowsTargeting`. **Offen:** die Marke `@* P4:
> SpeicherFlottenGroessenAnsicht (#193) *@` in Schritt 5 (Einbindung → #196); kein Menüpunkt (Empfehlung des Agenten: erst nach der
> Windows-Abnahme, weil die Seite ohne angemeldeten Lauf erst selbst rechnen müsste); unter Windows landet der Rückweg auf der
> Startseite, weil das Ergebnis dort eine Überlagerung ist; `EPOS.iOS/wwwroot/index.html` bindet `epos-flotte.css` nicht ein
> (Bestand seit P2, → #196, iOS-Lauf nur auf Anwenderwunsch); Windows-Abnahme durch den Anwender. Merge-Konflikte mit #193 an
> fünf Dateienden (resx beidseitig angefügt, CSS, zwei Doku-Dateien) durch Vereinigung gelöst — im CSS hatte Git den
> gemeinsamen Blockschluss der P3-Regeln als Suffix gewertet, zwei schließende Klammern nachgesetzt (82/82). Gate auf `ef55097`:
> Kern 2540, UI 3559, 5 eindeutige Warnungen, SQL 0 von 1 342, ChartProben 49, Referenzlauf 5/5 byte-gleich gegen R7.
>
> **R‑W16‑6 geschlossen (Anwenderentscheid 11.09.2026: „alter Schreibweg ist nicht mehr relevant").** Das Risiko aus
> Teilwelle 16a — der neue Schreibweg des Projektassistenten (`AssistentCtrl` mit Transaktion) könnte ein Projekt mit anderen
> Feldwerten ablegen als der gefallene WinForms-Assistent — verlangte den Feld-für-Feld-Vergleich am Windows-Gerät gegen den
> Stand `vor-W16` (`975ead5`), Abnahmepunkt A‑W16a‑O1‑3. Der Anwender hat entschieden, dass der alte Schreibweg nicht mehr
> maßgeblich ist: Der Assistent von W16a ist die einzige Fassung, seine zwei Kern-Prüffälle (Rückzug bei Fehlschlag) und die
> bunit-Fälle der Ansicht sind der Nachweis, ein Vergleich gegen den gelöschten Weg entfällt. Damit ist auch A‑W16a‑O1‑3
> gegenstandslos; die übrigen Punkte A‑W16a‑O1‑1/2/4…8 bleiben Teil der Windows-Abnahme. Der Git-Tag `vor-W16` bleibt zur
> Geschichte stehen.
>
> **#196 (11.09.2026, `47bdc8a`, Merge `bd9dbac`) — Größen-Sicht (P4) in der Ansicht (P3), iOS-Stilblatt.** Im Ergebnis-Schritt
> der Ansicht `STROMSPEICHER_AUSLEGUNG` steht jetzt an der Marke aus P3 der Baustein `SpeicherFlottenGroessenAnsicht`,
> sobald der Lauf ein Rastersuchergebnis trägt (`FlottenAuslegungErgebnis` im `SpeicherFlottenErgebnis` — Bedingung ist das
> Ergebnis, nicht der Schalter, damit die Karte nicht verschwindet, wenn der Schalter nach dem Lauf umgelegt wird); die alte
> einfache Kandidatentabelle in `SpeicherFlottenErgebnisAnsicht` fällt, Empfehlung und CSV-Export des Variantenvergleichs
> wanderten mit. **„Kandidat übernehmen"** läuft über eine Stelle der Seite: `SpeicherFlottenAnzeigeCtrl.KandidatKonfiguration`
> gibt für den besten Kandidaten die `BesteKonfiguration` des Optimierers zurück (dieselbe Wahrheit wie „Beste Flotte
> übernehmen") und bildet jeden anderen aus `FlottenKandidatEinheit` und dem Arbeitsstand zurück; danach Schritt 1, Banner,
> Schritt 5 veraltet, bei ungespeicherten Eingaben die Dreifachfrage (62b‑E‑1). **iOS:** `EPOS.iOS/wwwroot/index.html` bindet
> `epos-flotte.css` ein (seit P2 fehlend) — die eine Hüllenzeile, für die der Anwender den iOS-Lauf 42 freigegeben hat
> („iOS-Lauf 42 nach #196 starten"). 17 neue Fälle (8 bunit Seite, 6 Kern, 3 Ergänzungen), drei Ressourcen. Gate auf `bd9dbac`:
> Kern 2546, UI 3569, 5 eindeutige Warnungen, SQL 0 von 1 342, ChartProben 49, Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#199 (11.09.2026, `a3c33aa`, Merge `e975cf9`) — Assistent im Dialog, Stufe S1 (Anwenderentscheid 11.09.2026: Wege 1 + 2;
> Konzept `Projekte/Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`, #198).** Der KI-Knopf-Baustein aus W15b war nirgends
> eingebaut, der Kontexthaken des Kerns wurde in keinem Produktcode gesetzt, auf iOS kannte der Assistent keinen Bereich. Jetzt:
> **`InfoKnopf.MitAssistent`** (Vorgabe wahr) zeichnet den `KiKnopf` neben dem Info-Knopf — alle Dialoge und Überlagerungen mit
> Info-Knopf in einem Schritt; Lizenz- und Lizenzverwaltungsdialog setzen ihn ab. Sichtbar ist er, sobald die Lizenz KI erlaubt
> (`KiVerfuegbarkeit.Moeglich`); ohne Einrichtung führt er in die Einstellungen (KI‑D‑Q1). **Ein Öffnungsweg** `KiAssistentWeg.Oeffnen`
> → `KiChatKontext.AufrufMelden(KiAufrufkontext {Bereich, Dialogname, Hilfeschluessel, Frage, Kennung})` →
> `Dienste.Navigation.OeffneMaske(Masken.KiAssistent, kontext)`: Windows fängt den Schlüssel in `WinFormsNavigation` und öffnet die
> nicht-modale `KiChatHuelle` mit Kontext (ein schon offenes Fenster bekommt ihn über `KiChatSteuerung.Kontext` nachgereicht);
> die `AppWurzel` merkt die Herkunft, legt den Kontext über `IProjektQuelle.KiAssistentGaben` und kehrt dorthin zurück (Muster
> #62b — die Wurzel stellt die Ansicht wieder her, nicht den inneren Zustand einer Komponente). **Bereich aus dem
> Hilfeschlüssel:** `KiChatKontext.BereichFuerHilfeschluessel` mit 69 Maskenpräfixen, längstes Präfix gewinnt; ein Wächter hält
> die Tabelle gegen alle 201 Schlüssel der `help_mapping.txt` (0 unbekannt); `HilfeKontext` der Windows-Hülle ist zweiter
> Lieferant desselben Hakens. **„erklären lassen":** `Warnbanner.Kennung` zeigt den Link, der den Assistenten mit vorbelegter,
> nicht abgeschickter Frage öffnet (`KI_FRAGE_<Kennung>`, sonst `KI_FRAGE_ALLGEMEIN` mit Bannertext); gesetzt am
> `FlottenDiagnosebanner` (Kopf `FLOTTE_ARBEITSLOS` und je Prüfhinweis) und am Vorprüfungsbanner der Stromspeicher-Ansicht.
> **Aktionswissen:** 15 Abschnitte in `HilfeWissen.Aktionswissen()` (fünf `FLOTTE_*`, zwei `LAUF_W_ERZEUGER_OHNE_*`, acht
> `PV_STRANG_P1…P8`) mit Bedeutung, Ursache, Abhilfe, Wiki-Verweis; `Suchen` gibt der gemeldeten Kennung den Vorrang, ohne
> `KiChatService` anzufassen. Kontextzeile des Chats nennt Bereich, Dialogname und Kennung. 22 Ressourcen de/en; 69 neue Fälle
> (34 Kern, 35 bunit; die Tauscher von `Dienste.Navigation` in der seriellen Sammlung `KiDialogweg`). **Offen:** Laufwarnungen und
> Strangampel laufen nicht über `Warnbanner` und tragen noch keinen Link (Aktionswissen liegt bereit); Weg 1 belegt keine Frage
> vor. Wiki-Absatz „Der Assistent aus einem Dialog heraus" als Textvorschlag im Bericht (Upload mit #203). Gate auf `e975cf9`:
> Kern 2580, UI 3604, 5 eindeutige Warnungen, SQL 0 von 1 342, ChartProben 49, Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#203 (11.09.2026, `1a5fedb`, Merge `52f7fce`, Berichtigung `b8e59ae`; Wiki-Upload 11.09.2026 15:40 UTC, Revisionen 537–539) —
> Wiki-Dokumentation Stromspeicher nachgezogen (Anwenderfrage 11.09.2026 „ist die dokumentation auf der wiki zum stromspeicher
> aktualisiert?").** Befund: beide Stromspeicherseiten des Wikis kannten die Flotte nicht, und die Rechenwegseite war **im Wiki
> neuer als im Repository** (Erweiterung vom 10.09. Kosten/Zeitreihen, zwei Bearbeitungen vom 11.09. „Grenzen und Annahmen", 79 Zeilen)
> — ein Upload aus dem Quellbaum hätte sie stillschweigend überschrieben. Deshalb zuerst der Live-Stand in
> `EPOS.Kern/Allgemein/Hilfe/Berechnung/Stromspeicher.wiki` übernommen, dann **Fassung 4 „Speicherflotte"** darauf gesetzt
> (1 055 Zeilen; Hauptteil `{{Anker|flotte}}` mit Gleichungen 26–47 als `<math>` mit Legende: Physik je Einheit, fünf Betriebsziele,
> drei Verteilungen, Planer nur auf Windows nach SP‑O‑3, Rainflow und Reserven, Kapitalwert, Rastersuche und Kandidaten, Diagnose
> und Peak-Ziel, Größen-Sicht; 42 `Flotte.*`-Skalare und die Ganglinien je Einheit des Referenzexports benannt); die drei H13-Wächter
> (`BerechnungsHilfeTests`, `BerechnungshilfeTests`, `BerechnungsknopfTests`) führen die Fassung je Seite. **Bedienungsseite neu**
> auf die Ansicht `STROMSPEICHER_AUSLEGUNG` (271 Zeilen, Anker `auslegung-ansicht/-start/-ablauf/-speicher/-daten/-betrieb/-rechnen/
> -ergebnis/-diagnose/-groessen/-verlassen/-ios`), dazu der Absatz **„Der Assistent aus einem Dialog heraus"** auf der Seite
> Hilfe-Assistent (#199). Beide Bedienungsseiten haben seither eine **Repo-Quelle unter `Projekte/Wiki/`** — Hausregel im
> Hilfesystem-Konzept: zuerst im Repository ändern, Live-Stand vor dem Hochladen vergleichen, Anker und Kategorie bleiben, Upload
> durch die Orchestrierung. `help_mapping.txt`: `Form_SpeicherOptimierung.btn_Help` springt auf `Stromspeicher#auslegung-ansicht`
> statt an den Seitenanfang. Nachprobe des Uploads: alle drei Seiten byte-gleich zur Quelle zurückgelesen, 474 Formeln gerendert,
> 0 Parse-Fehler, Kategorien unverändert. Drei Textstellen, die der Upload vom 10.09. verdorben hatte (zwei Fettsätze über den
> Zeilenumbruch, Protokollzeile ohne „Berechnungsart"), in `b8e59ae` berichtigt; Kern 2 580 und UI 3 604 grün auf diesem Stand
> (reine Doku und Wächter, kein Gate nötig).
>
> **#200 (11.09.2026, `1e6cd38`, Merge `24a8b71`) — Assistent im Dialog, Stufe S2 (Weg 4 „Feldzustand mitgeben"; KI‑D‑Q2/Q3
> umgesetzt).** Die **Maskenbrücke** `EPOS.Kern/Allgemein/KI/KiMaskenbruecke.cs` (531 Z.) führt je offenem Dialog seine Feldliste:
> `KiFeldzugang` mit `Lesen` UND `Setzen` (Setzen angelegt und geprüft, noch von keinem Produktionsweg gerufen — das ist S3),
> `KiFeldwert`, `KiDialogdaten`, Protokollsenke; An-/Abmelden idempotent, thread-sicher. Der **Dialogkatalog** hängt nicht mehr an
> WinForms-Controlnamen: `KiDialogFeld` trägt einen `KiEigenschaftspfad` auf das Daten-Objekt der Razor-Komponente, `KiDialoge.cs`
> ist neu geschrieben (`KiMaskennamen`), ein Wächter prüft jeden Pfad per Reflexion. **Fünf Masken** melden sich über den
> Anmeldehelfer `EPOS.UI/Dienste/KiMaskenanmeldung.cs` (drei Zeilen je Dialog) an: Heizkessel 15 Felder, PV 3, Pufferspeicher 1,
> Wärmepumpe 1 — Feldumfang bewusst unverändert (Fachkonzept 11.6) — und neu die **Stromspeicher-Ansicht** über das flache Sichtmodell
> `StromspeicherKiSicht.cs` (16 Felder, 5 setzbar, 11 abgeleitet: Diagnose, Ergebnis der letzten Bewertung). **Einwilligungsstufe
> „Dialogdaten"** in `KiEinwilligung` (eigener Merker, Fassung, Datum, Zurücknehmen; Text in den KI-Einstellungen). **Chat:** Schalter
> „Feldwerte mitsenden" (nur bei angemeldeter Maske; ohne Einwilligung aus und gesperrt mit Grund), Vorschau zeigt den Feldblock
> wörtlich; `KiChatService` bekam genau EINEN optionalen Parameter `KiDialogdaten` (Block hinter dem Bereich, vor den Hilfeabschnitten),
> der Function-Calling-Vertrag ist unberührt, `dialog_lesen` liefert aus der Brücke. Die Komponente kennt die Brücke nicht — sie bekommt
> `Feldwerte`/`FeldwerteEinwilligen`/`FeldwerteGesperrt` als Delegaten (§ 15.3), beide Hüllen legen sie aus denselben zwei Kernstellen.
> Knöpfe behalten ihren `Controlpfad` und melden „nicht bedienbar" (Formularaktionen sind S3). Nebenbei: zwei `Schalter` der
> Eingabezeile trugen denselben `@key` (Blazor-Abbruch „More than one sibling has the same key value") — behoben. **51 Ressourcen** de/en.
> 40 Dateien, +4 599/−310; **+27 Kern-, +57 bunit-, +5 KiKern-Fälle** (`KiMaskenbrueckeTests`, `KiDialogdatenEinwilligungTests`,
> `KiDialogkatalogTests`, `KiFeldwerteTests`). **Offen für S3 (#201):** Setzen über die Brücke, Knöpfe, `feld_setzen`/`formular_ausfuellen`
> laufen noch über `KiDialogZugriff` und lehnen ab; iOS hat weiter keinen `Fragen`-Delegaten (`IProjektQuelle.KiAssistentGaben` leer, iU11).
> Gate sept16 auf `24a8b71`: Kern 2 607, UI 3 661, KiKern 474, 5 eindeutige Warnungen, SQL 0 von 1 342, ChartProben 49,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#210 (11.09.2026, `0697dd6`, Merge `99e5f3f`) — Speicherflotte im Projektlauf: zwei Einheiten bleiben zwei.** Anwenderbefund aus zwei
> Bildschirmfotos: „Kennzahlen je Speicher" und die Reihenwahl der Diagramme führten EINE Einheit, ebenso der Eingabestand `@Aktuell` —
> das Projekt hat zwei Speicheranlagen. **Ursache** (`SpeicherFlottenStudieCtrl.Vorbelegung`, vorher Z. 61–70): Die erste Flotte eines Projekts
> entstand aus `StromspeicherSimCtrl.LeseParameter(projektId)`, und der liefert per Bauart EINEN Satz — die Anlagenzeile der aktiven
> Speichervariante (AP9b), im Rückfall die kapazitätsgewichtete Summe. Für den Einzelspeicherlauf richtig, für die Flotte ein stiller Verlust;
> weil der Projektlauf den gespeicherten Stand `@Projektflotte` rechnet, fehlte die zweite Anlage danach überall. Die zwei anderen Hypothesen
> (Zusammenfall gleicher `AnlageId`/Namen; veraltetes Ergebnis ohne Banner) sind mit Tests ausgeschlossen. **Fix:** `StromspeicherSimCtrl.Speicheranlagen(int)`
> (alle `SP_TYP`-Anlagen in Anlagenreihenfolge, `REF_SP_TYP` bleibt draußen) und `SpeicherFlottenStudieCtrl.EinheitenAusProjektanlagen` — je Anlage ein
> eigener `LeseParameter(projektId, anlageId)`-Satz (Gerätedaten aus der Anlage, SoC-Band aus deren Variantenzeile), Name = Anlagenbezeichner, `AnlageId`
> gesetzt, Rückfall auf den Sammelsatz nur ohne Anlagensatz; gespeicherte Stände werden nie überschrieben. Der Stromspeicher-Reiter zeigt je Einheit die
> Spalte **„Herkunft"** („Projektanlage ‹Id›" oder „nur im Eingabestand"). 6 Kern-Fälle (`SpeicherFlottenAnlagenEinheitenTests`, drei davon vor dem Fix
> rot) und 4 bunit-Fälle. **Offen (#210‑O‑1, Anwenderentscheid):** Das Schema unterscheidet eine gleichzeitig betriebene Anlage nicht von einer
> Vergleichsalternative — beides ist eine `SP_TYP`-Zeile; der Fix nimmt alle als Einheiten (sichtbarer statt stiller Fehler, überzählige löscht der
> Anwender in Schritt 1). Die Referenzprojekte 1007/1046 führen je vier `SP_TYP`-Anlagen und bekämen beim ersten Öffnen der Auslegung vier
> Vorbelegungs-Einheiten; regressionsrelevant ist das nicht (1046 rechnet seinen Stand `@Projektflotte`). Gate sept17 auf `99e5f3f`: Kern 2 613, UI 3 665,
> Engine 394, KiKern 474, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 49, Referenzlauf 5/5 byte-gleich gegen R7 (Agent: 13/13).
>
> **#206 (11.09.2026, `8a382b6` + `7cecc6e`, Merge `4fcb6a1`) — Stromspeicher-Auslegung, Paket P5: ein Modus statt zwei (Anwenderentscheid
> SD‑E‑8: „Es ist nicht sinnvoll, einen Unterschied zwischen Einzelspeicher und Flotte zu machen … die bisherigen Berechnungsarten für
> Einzelspeicher sind nicht mehr nötig").** Der Modus-Umschalter und `AuslegungModus` sind gefallen; die Ansicht `STROMSPEICHER_AUSLEGUNG`
> rechnet immer die Flotte. **Ein Einzelspeicher ist eine Flotte mit einer Einheit:** Ohne gespeicherten Stand belegt der Kern je
> Speicheranlage des Projekts eine Einheit vor (Vorbelegung aus #210 — Kapazität, Leistungen, Wirkungsgrade und SoC-Band aus der Anlage,
> Name = Anlagenbezeichner). Die fünf Betriebsziele, Peak-Ziel, Diagnose, Vorprüfung und Größen-Sicht gelten für jede Einheitenzahl; die
> **Verteilung erscheint erst ab zwei Einheiten** (`SpeicherFlottenBetriebEditor.VerteilungZeigen`, bei einer Einheit eine Erklärzeile).
> Der **Leistungspreis** ist eine Eingabe in Schritt 2 (`LeistungspreisBlock`, derselbe Wert wie L_P des alten Einzelspeichers, mit Quelle);
> **Rückschreiben in die Projektanlage** (Kapazität/Leistung über `UebernehmeAuslegung`, mit Rückfrage) steht in Schritt 5 für die Einheit
> mit Anlagenbezug — damit bekommt auch der klassische Projektlauf ohne aktivierte Flotte die optimierte Größe. Schritt 4 heißt „Bewerten".
> **Gelöscht:** `EinzelspeicherSuchraum/-Betrieb/-Ergebnis.razor` und der Einzel-Prüfstand (1 538 Z., 41 Fälle), der Suchraum-Teil des
> `SpeicherAuslegungEditor`, `EinzelVorbereiten`/`EinzelRechnen`/`Betriebsbild`/`RasterCsv` in `StromspeicherAuslegungCtrl` und Hülle
> (nur, was keinen Aufrufer mehr hatte). **Geblieben mit Grund:** `SpeicherOptimierer`/`OptimiererStrategie`/`SpeicherOptimierungCtrl` — Aufrufer
> sind die KI-Aktion `speicher_optimieren` (`StromspeicherSimCtrl:688`), `SpeicherAuslegungCtrl.Vorbelegung` und das Bericht-`SpeicherBetriebsbild`;
> `Strategien()` hat nur noch einen Testaufrufer. Ressourcen −38 (17 der Ansicht, 21 verwaiste `OPT_*`), +3, 5 894 Schlüssel je Sprache.
> 18 neue bunit-Fälle (Wache „kein `AuslegungModus` in EPOS.UI"), 2 Kern-Fälle gegen Projekt 1017. **Wiki:** Bedienungsseite mit neuem Anker
> `auslegung-anlage`, Rechenwegseite **Fassung 5** (Abschnitt `auslegung-kosten-zeitreihen` als Rechenweg des Einzelspeicher-Optimierers
> gekennzeichnet — nicht mehr in der Ansicht, weiter im Bericht und Assistenten), Wächter nachgezogen; Upload durch die Orchestrierung.
> **Offen:** Der zweite Wirt des Betriebseditors (`StromspeicherReiter`) zeigt die Verteilung weiter auch bei einer Einheit (Anwenderfrage);
> Stufenplan-Zeile „Feinraster" heißt jetzt P6; Windows-Abnahme steht aus. SD‑Q1 revidiert, SD‑Q2 bleibt (Projektlaufpfade unverändert).
> Gate sept18 auf `4fcb6a1`: Kern 2 615, UI 3 638, Engine 394, KiKern 474, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 49,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#201 (11.09.2026, `d1bfb56`, Zwischenmerge `d2889cd` mit #206/#210, Merge `9122812`) — Assistent im Dialog, Stufe S3 (Weg 5 „Assistent soll
> steuern", KI‑D‑Q3/Q4).** Aktionsregister und Ausführung sind aus der Windows-Hülle in den Kern gezogen: zehn Dateien per `git mv` nach
> `EPOS.Kern/Allgemein/KI/Aktionen/` bzw. `KiAusfuehrung.cs` (1 040 Z.). `KiAusfuehrung` ist eine **Instanz** (Einläufigkeit, Laufmarke,
> Sitzungsgedächtnis und Register gehören einem Ausführer; ein Prüfling legt sich einen frischen an); die `AppWurzel` legt die Kern-Ausführung
> ein, solange `KeineAusfuehrung` steht — **iOS und die Razor-Dialoge haben dasselbe Register** (29 Aktionen: 24 Bestand, neu `dialog_oeffnen`
> Stufe 1, `dialog_speichern` Stufe 2 mit Sicherungspunkt, `simulation_rechnen`, `peak_ziel_bestimmen`, `flotte_bewerten` Stufe 3;
> `KiRiegel.HoechsteStufe` → `Rechnen`). **`feld_setzen`** geht über die Maskenbrücke (#200) in den offenen Dialog: `KiMaskenhaken`
> (Auffrischen, Prüfen, Speichern, Schreibschutz, Rechenwege) je Maske, `KiFeldwandler` (Werttyp), `KiMaskenziele`, `KiFeldhilfe` als Naht
> zum Feld-Hilfetext, `KiLaufumgebung` trägt Fortschritt und Abbruchmarke bis in die Aktion; Ablehnungen benannt (abgeleitetes Feld,
> Typfehler, Schreibschutz, ohne Bestätigung, Auffrischen). Setzbar: Heizkessel 15/15, Photovoltaik 3/3, Pufferspeicher 1/1, Wärmepumpe 1/1
> (einzige Maske mit `Schreibgeschuetzt`, weil nur ihr Daten-Objekt `NurLesen` führt), Stromspeicher-Auslegung 5/16. **In der Hülle blieb**
> `KiAusfuehrungWindows` (121 Z.: Halter der Instanz, `Form.ActiveForm.Modal`, Hilfetext über `WikiHelpCatalog`); `KiDialogZugriff` (567 Z.)
> und `KiAusfuehrungAdapter` sind gelöscht, der Rückfall über `Application.OpenForms` ersatzlos. 53 Ressourcen `KI_AKTION_*`/`KI_FELD_*` de/en.
> Tests: `KiRegisterS3Tests` (663 Z.), `KiFeldSetzenTests` (je Maske ein Fall), `KiMaskenhakenTests`. **Doku:** Konzept Dialogintegration (S3),
> `Konzept_KI-Assistent_Aufgabensteuerung.md` (+101), drei CLAUDE.md, Wiki „Hilfe-Assistent" (+25, Upload durch die Orchestrierung).
> **Abweichungen/offen:** `speicher_optimieren` war nie registriert (nur im Aktionskatalog des Konzepts; sein Platz nach SD‑E‑8 ist
> `flotte_bewerten`); `dialog_aktion_ausfuehren` bleibt und lehnt benannt ab (ein Razor-Dialog hat keinen Knopf von außen);
> `dialog_oeffnen("StromspeicherAuslegung")` lehnt unter Windows benannt ab (kein Schlüssel der WinForms-Navigation, braucht einen gerechneten
> Lauf; iOS wechselt die `AppWurzel`); der **Fortschrittsbalken im Chat** wird noch nicht gefüttert (die Hülle reicht `CancellationToken.None`,
> die Senke `KiAusfuehrung.Fortschritt` bleibt unbelegt — ein Hüllenschritt). Zwischenmerge mit #206/#210: zwei resx-Konflikte (reine Anhänge),
> `flotte_bewerten` ruft `Starten()` = seit #206 der Flottenweg.
> Gate sept19 auf `9122812`: Kern 2643, UI 3654, Engine 394, KiKern 474, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 49,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#207 (11.09.2026, `4da303d`, Merge `e608457`) — Simulationsablauf Stufe S1 (Anwenderentscheid „SIM‑Q1 bis Q6: Empfehlung").** Die Simulation
> ist eine **freie Ansicht `SIMULATION`** der `AppWurzel` (`EPOS.UI/Seiten/Simulation/SimulationSeite.razor`, 429 Z.) mit Ablaufleiste
> **① Konfiguration · ② Simulation starten ▶ (Knopf, kein Blatt) · ③ Ergebnis**; sie bettet `SimulationKonfigSeite` und
> `SimulationErgebnisSeite` ein. ② ist gesperrt, solange die Konfiguration ungespeichert ist (genau die drei Kaskadenwege) oder die
> Sperre aus ADR‑001 (`SchemaMigration.SimulationGesperrt`) steht — der Grund steht am Knopf und darunter; ③ öffnet, sobald ein Lauf gerechnet
> ist (nicht „gültig", sonst sperrte der Nachzug der Auslegung genau den Schritt, in den der Rückweg führt); `Automatikstart` ist in der
> Ansicht aus (SIM‑Q1). **Gefallen:** aus der `Startseite` die Ergebnis-`Ueberlagerung`, die Konfig-Einbettung und ihre zwei Gaben (die
> Kachel ruft `Dienste.Navigation.OeffneMaske(Masken.Simulation, marke)`); aus der Ergebnisseite die Fußknöpfe „Konfiguration …"/„Beenden"
> und die Konfig-`Ueberlagerung`; aus der Konfigurationsseite „Beenden". **Rückwegstapel** `List<(Schluessel, Marke)>` in der `AppWurzel`
> (höchstens 3, geleert bei `STARTSEITE`/`PROJEKTLISTE`; `Zeige(ziel, marke, merken)` legt ab, `Zurueck()` holt und überspringt tote Ziele)
> ersetzt `_auslegungRueckweg` (#192) und `_kiRueckweg` (#199) — die Auslegung kehrt in ③ auf den Reiter „Stromspeicher" zurück, der
> das Banner „Flotte geändert" zeigt (`SimulationErgebnisHuelle.LaufGerechnet`). **Hülle:** `SimulationHuelle` (121 Z.) hält je Projekt
> Konfig- und Ergebnishülle und liefert **ein** Wörterbuch `{Dienste, ProjektText}`; `HauptfensterHuelle.Gaben()["SimulationGaben"]`,
> `IProjektQuelle.SimulationGaben` (Standard `null` — die iOS-Quelle liefert noch nichts, S2 = #208). **Menü:** „Simulation…" neben
> „Varianten und Bericht…" (SIM‑Q3), 58 → **59** Punkte, 46 handelnd; Marken `schritt=…;blatt=…`. Ressourcen +14 `SIM_ANSICHT_*`/`MENU_SIMULATION`,
> −3 verwaiste. Tests +33: `SimulationSeiteTests` (477 Z.), `StartseiteSimulationwegTests`, Wache `SimulationOhneUeberlagerungTests` („keine
> Simulationsseite in einer `Ueberlagerung`", Gegenprobe am Bestand vor #207). **Offen:** „Simulation starten ▶" bleibt auch in der Fußleiste
> von ③ (Zwilling von ②, trägt die Seite auf iOS ohne Leiste); ein nur gespeichertes Ergebnis früherer Sitzungen öffnet ③ nicht (kein Lesepfad
> im Bestand); Wiki-Bedienungsseite „Simulation" (Textvorschlag des Agenten) durch die Orchestrierung; Windows-Abnahme steht aus.
> Gate sept20 auf `e608457`: Kern 2643, UI 3687, Engine 394, KiKern 474, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 49,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#213 (11.09.2026, `dcbe300`, Merge `33d2afb`) — Stromspeicher-Reiter: Leistungsverteilung erst ab zwei Einheiten (Anwenderentscheid
> „Verteilung im Stromspeicher-Reiter erst ab zwei Einheiten: Empfehlung", der zweite Wirt von SD‑E‑8).** `StromspeicherReiter.razor` setzt
> `VerteilungZeigen` des `SpeicherFlottenBetriebEditor` aus der Einheitenzahl, wortgleich zur Auslegungsansicht; der Editor selbst kennt die
> Einheitenzahl nicht (sein Wert ist `FlottenSimulationOptionen`), deshalb bleibt der Parameter und beide Wirte entscheiden gleich. Zwei
> bunit-Fälle in `StromspeicherReiterTests` (eine Einheit → Erklärzeile, zwei → Klappliste). Konzept 4.1 („Offen: zweiter Wirt" → erledigt) und
> `Doku_Mehrspeicher_Konzept_und_Umsetzung.md` nachgezogen; der Satz „Offen: zweiter Wirt" im Statusblock #206 oben bleibt als Geschichte stehen.
> Gate sept21 auf `33d2afb`: Kern 2643, UI 3689, Engine 394, KiKern 474, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 49,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#211 (11.09.2026, `c6417bc`, Merge `8387deb`) — tote Markdown-Links und Schreibschutz-Kennzeichen für `feld_setzen` (Restpunkt #201,
> Anwenderentscheid „Schreibschutz: Empfehlung mit #211 starten").** Teil A: die zwölf toten relativen Links (Befund #209) in
> `Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md` (10), `Konzept_DB-Migration_SQLite_EPOS-Plan.md` (1) und
> `Doku_Mehrspeicher_Konzept_und_Umsetzung.md` (1) sind 0 — neun Ziele auf ihren heutigen Ort unter `EPOS.Kern/` (per `git log --follow`), drei
> entlinkt mit Klammerzusatz (`SpeicherFlottenDialog.razor` gefallen mit #192, `GebäudeKontextMenuCtrl.cs` mit W16b, `Kenndaten.cs` mit W7).
> Teil B: `NurLesen` nach dem Muster der Wärmepumpe an `HeizkesselKatalogDaten`, `ErzeugerZeile` (Photovoltaik) und `PufferSpKatalogDaten`, befüllt
> in den Hüllen aus `m_bReadOnly` bzw. `IsReadOnlyStatic` der Stamm-Controller; die drei Masken melden `Schreibgeschuetzt` in `KiHaken()`, der
> Ablehnungsweg (`KI_FELD_SATZ_GESCHUETZT`) war vorhanden — der Assistent lehnt einen Auslieferungssatz jetzt VOR der Bestätigung ab statt erst
> beim Speichern; der Speicherweg der Controller ist unverändert. Drei Fälle in `KiFeldSetzenTests`, `KiMaskenhakenTests` erweitert; Konzept
> Dialogintegration 3.4/KI‑D‑Q3 „erledigt mit #211". Offen: `ErzeugerZeile.NurLesen` trägt die Bedeutung „Gerät schreibgeschützt" (Windows-Abnahme).
> Gate sept22 auf `8387deb`: Kern 2643, UI 3692, Engine 394, KiKern 474, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 49,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#217 (11.09.2026, `6fade45`, Merge `e536a29`) — „Projekt löschen" (Kachel) stürzte ab.** `ProjektWahlDialog.MehrereFrage` formatierte den
> Ressourcentext `PDLG_RUECKFRAGE` mit EINEM Argument; der Text stammt aus dem alten WinForms-Dialog (Merge 5, 02.09.2026) und trägt ZWEI Platzhalter
> (Anzahl, Namensliste) samt „Fortfahren?" — `FormatException` bei jedem Löschen, in bunit grün, weil die Razor-Vorgabe nur `{0}` hatte. Fix: der Dialog
> baut die Namensliste (zwölf Namen, „… und n weitere", Variantenkennzeichen) und formatiert mit beiden Argumenten; die Razor-Vorgabe hat dieselbe
> Struktur wie die Ressource. Alle sieben Format-Parameter des Dialogs gegen ihre Ressourcen geprüft (nur dieser wich ab). Zwei bunit-Fälle mit dem
> echten Ressourcentext (de/en, 3 und 14 Projekte) und die Wache `ProjektWahlPlatzhalterWacheTests` (höchste Platzhalternummer der Ressource = Vorgabe).
>
> **#214 (11.09.2026, `0a29d31`, Merge `42f51e4`) — Fortschrittsbalken und Abbrechen bei Rechenaktionen des Assistenten (Restpunkt #201,
> Anwenderentscheid „Empfehlung starten").** Befund: Auf dem Bedienfaden wären Balken und Abbruchknopf eine leere Zusage — `AufOberflaeche` führte den
> Lauf inline, der Renderer zeichnete nicht, der Klick kam erst nach dem Lauf an. Lange Aktionen (`AusfuehrenLang`) laufen deshalb in
> `KiAusfuehrung.ImHintergrund` (`Task.Run`), die 19 kurzen bleiben auf dem Oberflächenfaden. `KiChatDialog.Lauf.cs` (258 Z.) belegt die Senke, zeigt
> Balken, Schritttext und „Abbrechen" und sperrt Senden/Aktionsknöpfe während einer Aktion. Abbruch erreicht `simulation_rechnen` über einen
> `Phasenmelder` in `SimulationRunner`/`SimulationControl` (Prüfung zwischen den fünf Phasen, `OperationCanceledException` → `KiErgebnis.Abgebrochen`,
> nichts gespeichert), `peak_ziel_bestimmen` und `flotte_bewerten` über die `KiLaufKlammer` der Auslegungsansicht (dieselbe Abbruchquelle wie der Knopf
> „Berechnen"); die Hülle reicht die Abbruchmarke an alle vier Wege statt `CancellationToken.None`. 9 Ressourcen, `KiLaufumgebungTests` (14),
> Kernfälle mit Gegenprobe lang/kurz. **Offen:** Windows-Abnahme (Lage des Balkens, spürbarer Abbruch); die Werkzeugliste löst die drei
> Rechenaktionen weiterhin nicht aus (Bestand #201, bestätigungspflichtig); Wiki „Hilfe-Assistent" Absatz „Rechnen dauert" in der Quelle, Upload durch die Orchestrierung.
> Gate sept23 auf `42f51e4`: Kern 2645, UI 3711, Engine 394, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 49,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#215 (11.09.2026, `4bd5c8c`, Merge `74a3bb1`) — Paket P7: adaptive Lastspitzenkappung, die kausale Ratsche (Spezifikation 5.1.1 Fassung 1.4,
> Anwenderentscheid „PS‑Q1 bis Q4, Empfehlung").** Befund des Anwenders: Bei festem Peak-Ziel entlud die Flotte nach einer verfehlten Spitze
> an jeder kleineren weiter und stand leer, wenn die große kam. Jetzt ist H ein Zustand: `PeakZielAdaptiv` in `FlottenSimulationOptionen`
> (Vorgabe NEUER Stände über `FlottenVorgaben.PeakZielAdaptivFuer`, gespeicherte Stände bleiben fest), je Intervall D_t aus `Grenzen(…, release: true)`,
> Nachzug `H = N − D` bei `N − D > H`, danach die Regel aus 5.1 mit H an allen sieben Lesestellen des Simulators; `FlottenIntervallErgebnis.PeakZielKw`
> (Treppe), `FlottenSimulationErgebnis.ErreichtesPeakZielKw` (H_end), `FlottenDiagnose.IntervalleSchwelleNachgezogen`. Ohne Ratsche ist der Rechenweg
> Bit für Bit der bisherige. Bisektion rechnet ausdrücklich fest und heißt „mit Vorausschau erreichbar"; Anzeige-Controller zeichnet die Treppe und
> nennt „kausal erreicht". Ansicht: Schritt 3 Wahl „adaptiv (kausal) | fest" mit „Startwert" (Grundlast vorbelegt), Schritt 5 zwei Zeilen, Reiter zeigt
> den Modus, KI-Sicht 17. Feld `peak_ziel_adaptiv`, 15 Ressourcen. **Prüfstand:** `FlottenPeakRatscheTests` (14) mit dem Port des Excel-Makros auf
> einem synthetischen Lastgang (7 Tage, Grundlast 60 kW, Spitzen 250–400 kW, ein 740-kW-Block): fest 740 kW (arbeitslos, SP‑O‑10), adaptiv
> **540 kW** (H-Treppe 60 → 250 → 340 → 540, drei Nachzüge), Vorausschau-Optimum M* 340 kW, Wert der Vorausschau 200 kW; Port und Simulator auf
> 1e‑6 gleich in Jahresspitze, H-Treppe, Netz- und SoC-Ganglinie; adaptiv mit H₀ = M* = fest. `PeakRatscheAnsichtTests` (11), zehn Kernfälle.
> **Doku:** Spezifikation 5.1.1 „umgesetzt", Konzept P7, `Doku_Mehrspeicher` (+70), Wiki-Rechenweg **Fassung 6** (Anker `peak-ratsche`, Gl. 48–50),
> Bedienungsseite Schritt 3/5 — Upload durch die Orchestrierung. **Offen:** Windows-Abnahme; S‑D (Kurzfristprognose) nach PS‑Q4 erst nach gemessenem
> H_end − M*; Beobachtung des Agenten: `ModulImportDialogTests.Der_Herstellerfilter_…` fiel in einem von drei Gesamtläufen (Verdacht prozessweites
> `Katalogfilterregister`, gehört zu #212). Kein iOS-Lauf (trifft die Hülle nicht).
> Gate sept24 auf `74a3bb1`: Kern 2655, UI 3722, Engine 408, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 49,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#216 (11.09.2026, `5ef1433`, Merge `bcd3725`) — Windows-Abnahme der Simulationsansicht (#207, drei Bildschirmfotos; Anwenderentscheid SIM‑E‑1).**
> Die Kachel der Startseite heißt „Simulation starten" und startet den Lauf (`SimulationMarke.SCHRITT_LAUF`; bei Sperre Rückfall auf ① mit Grund).
> Der Kopf ist eine rechtsbündige Werkzeugleiste (kompakte Ablaufleiste, „Ergebnis speichern" nur in ③, i/KI, „← zurück"); die Fußleiste des
> Ergebnisses ist gefallen. Der Reiter „Parameter" ist entfallen (`ParameterReiter.razor` gelöscht): Die Netzverluste — sie werden gelesen, Weg
> `KonfigurationCtrl` → `SimulationLaufCtrl.cs:72/118` → `SimulationWaermebedarf.cs:344-374` — stehen in ① im Abschnitt „Wärmebedarf" (Vorgabe 0 %,
> wirkt nur bei Wärmebedarf); BHKW-Betriebsart und untere Leistungsgrenze, Heizstab und Betriebsbereitschaft sitzen als Parameterbereich an der
> ERSTEN Karte ihrer Art (`ErzeugerKachel.Parameterbereich`); der Einzelspeicherblock steht im Reiter „Stromspeicher" unter „Einzelanlage
> (klassischer Projektlauf)" und nur ohne aktivierte Flotte. Alle fünf Schreibwege bleiben (`SimulationParameterDienste` aus
> `SimulationErgebnisHuelle.ParameterGaben()`); das Ergebnis macht mit der Übersicht auf. **Nebenbefund behoben:** `SimulationKonfigHuelle.Speichern`
> schrieb die ganze `Tab_Einstellungen`-Zeile aus dem Arbeitsstand und hätte die fünf Laufparameter zurückgesetzt — `LaufparameterNachlesen()` liest
> sie vor dem Schreiben nach. Fünf Ressourcen (`SIMKONF_GRP_WAERMEBEDARF`, `SIMKONF_HRL_NETZVERLUSTE`, `SIMKONF_GRP_LAUFPARAMETER`,
> `SP_GRP_EINZELANLAGE`, `START_K_DETAILSIM_T`), `SIMERG_TAB_PARAMETER` entfernt; 13 neue bunit-Fälle, 11 der `ParameterReiterTests` gefallen;
> Konzept Simulationsablauf Abschnitt 7. **Offen:** Windows-Abnahme; iOS liefert `SimulationGaben` weiter `null` (#208).
>
> **#218 (11.09.2026, `b9dc087`, Merge `bec51ec`) — Hilfe-Pille mit EPOS-Marke (Anwenderentscheid „KI-Knopf: Variante C + Variante D").**
> `InfoKnopf.razor` zeichnet die Pille `.epos-hilfepille`: links das „i" als Inline-SVG (statt `help_icon.png`), rechts bei sichtbarem Assistenten
> die nachgezeichnete EPOS-Plan-Marke (drei Felder PV-Blau/Grün/Orange, weiße Mitte, blauer Blitz; Token `--epos-ki-marke-*`). `KiKnopf.razor`
> bleibt als Ring für Wirte ohne Info-Knopf und zeichnet dieselbe Marke; beide tragen `Aktiv` (Vorgabe `false`, füllt Feld bzw. Ring mit
> `--epos-marke`) — noch von keinem Wirt gesetzt. Die Selektoren `.epos-infoknopf`/`.epos-kiknopf` bleiben (über 40 Dialogproben), dazu
> `epos-hilfepille__feld[--aktiv]`. Textbeschriftung „KI" entfällt: `KI_KNOPF_HILFE` und `KI_KNOPF_DIALOG` ohne Leser entfernt,
> `KI_KNOPF_DIALOG_TOOLTIP` bleibt. Fünf neue bunit-Fälle; Wiki-Quelle Hilfe-Assistent um den Satz zur Marke ergänzt.
> **Offen:** Windows-Abnahme der Farben (bunit prüft nur Struktur); `Aktiv` setzen, sobald eine Ansicht `KI_ASSISTENT` steht.
> Gate sept25 auf `bec51ec`: Kern 2 655, UI 3 729, Engine 408, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 49,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#212 (11.09.2026, `8935ad6`, Merge `5b54010`) — Stromspeicher-Import „blinkt" bei 6 654 Zeilen (Anwenderbefund, Neustart nach Abbruch;
> Erweiterung „auch bei anderen Imports prüfen").** Headless über alle SECHS Importwirte (vier VDI 3805, Wechselrichter CEC/OND, Stromspeicher
> CEC/bslib) mit je 6 654 synthetischen Zeilen gemessen: keine neue Datenquelle je Zeichenlauf, Behälter richtig — die Ursache sind drei
> Kosten je Zeichenlauf, alle an der GEMEINSAMEN Stelle behoben: (1) `Katalogliste.Neuberechnen` filterte und sortierte den ganzen Katalog
> bei jedem Zeichenlauf (105 ms sortiert, 194 ms mit Suche → 4 ms; Abzug `EtwasGeaendert` über Zeilenliste samt Anzahl, Profil, Filterstand,
> Suche, Sortierung, Spaltenausdrücke); (2) `KatalogImportDialog.Anzeigeindex` linear und dreifache zeilenweise Abfrage des Alle-Schalters
> (445 → 15 ms; Modulimport `_gewaehlte.Contains` 539 → 7 ms per Mengenspiegel); (3) `Raster.Zeilenhoehe` 44 statt 53 px, `Virtualize`
> forderte bei jeder Sichtbarkeitsmeldung neu an. QuickGrid lädt hinter `Task.Delay(100)` und zeichnet solange Platzhalter (`loading`) —
> das Blinken. Statuszeile nennt die Satzzahl („6.654 von 6.654 Einträgen geladen. · 1 gewählt"); die grüne Sammelmeldung des Fotos war
> korrekt. Wache mit eigenen Zählern in der Komponente (bunit `RenderCount` zählt den Unterbaum), parametrisiert über die Ausprägungen;
> 14 neue Fälle, UI-Suite 20 s statt 51 s. Konzept Stromspeicherimport (Befund/Fix), `EPOS.UI/CLAUDE.md` (Katalogliste: stabile
> Items-Referenz, Zeilenmaß). **Offen:** Windows-Abnahme (Blinken weg? Zeilenmaß 53 px trifft?); der Abzug bemerkt keinen Zeilentausch
> bei gleicher Anzahl in derselben Listeninstanz (tut heute kein Wirt).
> Gate sept26 auf `5b54010`: Kern 2 655, UI 3 743, Engine 408, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 49,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#219 (11.09.2026, `76d2586`, Merge `2e02c95`) — Hilfe-Assistent: Eingabe ohne Tastatur (KI‑D‑B‑1) und toter Verweis „Online-Dokumentation"
> (KI‑D‑B‑2); Anwenderbefunde mit Bildschirmfoto.** KI‑D‑B‑1: Der Pillenweg aus einer Ansicht (`InfoKnopf` → `KiAssistentWeg` → `OeffneMaske`)
> öffnete die nicht-modale `KiChatHuelle` mit ihrer zweiten WebView2 SYNCHRON im WebMessage-Rückruf der ersten WebView2
> (`WinFormsNavigation.cs:195`, ohne `Blazorsprung`; die Lage von W16b‑B‑1/W13‑B‑1/W15b‑B‑1), und hinter `Show(besitzer)` stand keine
> Fokusübergabe — der Menüweg lief seit W16b verzögert, aber ebenso ohne Fokus. Die Sperrhypothese (`Gesperrt`) ist mit bunit widerlegt.
> Fix: Fall `KiAssistent` über `Blazorsprung.Verzoegert`; `BlazorDialogForm.TastaturUebergeben()`/`Fokussieren()` nach `Show` und beim
> Nach-vorn-Holen; `KiEingabezeile` mit `autofocus` und `FocusAsync` nach dem ersten Zeichnen. KI‑D‑B‑2: `KiChatHuelle.Gaben.cs:460` rief
> `Dienste.Datei.MitSystemOeffnen` für eine https-Adresse, `WindowsDateiDienst.cs:107` prüft `File.Exists` und gab still `false` — jeder
> Verweis des Chats (Fußleiste, Wikitreffer, Modellantworten) und der Rückfall des i-Knopfs (`WindowsHilfeDienst.cs:82`) waren tot; Fix
> `Dienste.Datei.AdresseOeffnen` („eine Adresse ist keine Datei", seit iU9‑W16c.3 im Vertrag). Alle 16 Bedienelemente des Dialogs geprüft
> (Tabelle im Bericht; die übrigen grün, „Verlauf kopieren" geht über die Hülle, nicht über `navigator.clipboard`). 19 bunit-Fälle
> `KiChatBedienungTests`, 10 Quelltextwachen `KiChatOeffnerTests` (je mit Gegenprobe), `WindowsFormsApplication1/CLAUDE.md` (Blazorsprung
> dritter Verteiler, Tastaturübergabe, Adresse ≠ Datei), KI-Konzept Kapitel 8. **Offen (nur am Gerät):** Tastatur auf beiden Öffnungswegen
> und beim zweiten Öffnen; Browserstart des Verweises; falls der Fokus ausbleibt, `CoreWebView2Controller.MoveFocus` als nächster Schritt;
> `Blazorsprung._angefordert` ist prozessweit ein Riegel; iOS erbt `autofocus` ungeprüft (kein iOS-Lauf, Regel).
> Gate sept27 auf `2e02c95`: Kern 2 655, UI 3 772, Engine 408, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 49,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#208 (11.09.2026, `2f62ca4` + `81d8f2f`, Merge `c989745`) — Simulationsablauf Stufe S2: iOS erreicht die Simulation; Anwenderentscheid
> #208‑E‑1 = A: das elfte Projekt `EPOS.UI.Daten`.** Die sieben Simulationshüllen (5 612 Zeilen, sechs davon Windows) ziehen als 18 Dateien in ein
> plattformfreies Projekt, das Kern UND Oberfläche sieht (`EnableWindowsTargeting=false`; der Kern darf EPOS.UI nicht kennen, EPOS.UI keine
> Datenbank, eine Hülle baut die DTO der Razor-Seiten und lädt). `SimulationAnsichtQuelle` liefert dasselbe Wörterbuch aus `idProjekt`, Projektname
> und Bedarfszustand; zwei benannte Nähte (`SimulationPlattformwege`: Wärmepumpen-Assistent auf iOS benannt abgelehnt; `Katalogwege`: Pufferkatalog-
> Knopf entfällt ohne Delegat); `Ladeordnung.Kreisziffer` und `KiChatKontext.BereichMelder` in den Kern; die Windows-Hülle bleibt ein Adapter
> (`SimulationHuelle.cs`, 64 Z.). iOS: `IosProjektQuelle.SimulationGaben`, Kachel „Simulation" in der Projektliste; **#202** erledigt
> (`DisplayAlertAsync`/`DisplayActionSheetAsync`, `IsBusy`, zwei Null-Hinweise im Referenzlauf). `WP-Plan.Kern.slnf`/`WP-Plan.sln` führen elf Projekte;
> `SqlDialektPruefer` prüft die neue Wurzel mit. Merge-Konflikt `SimulationHuelle.cs` (#216 vs. Umzug) zugunsten des Adapters mit dem
> #216-Parameterweg. 11 neue Kernfälle (`SimulationAnsichtQuelleTests`, Bild-Determinismus, Quelltextwache „keine Windows-Berührung"), 4 bunit
> (`SimulationAufIosTests`); `EinheitenWacheTests`/`ParametersatzTests` lesen das neue Projekt mit. **Offen:** iOS-Lauf 43 (freigegeben, folgt);
> `Blazorsprung`/Hüllen der übrigen 26 Ordner unter `Views/` bleiben Windows (iU11).
> Gate sept28 auf `c989745`: Kern 2 666, UI 3 776, Engine 408, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 49,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **iOS-Lauf 43 (34644279531 auf `d976451`, 11.09.2026, grün, 10 min 49 s) und #223 (`5370fef`, Merge `eb23302`).** Der erste Lauf mit
> `EPOS.UI.Daten` und der Simulationsansicht auf iOS: Bau 0 Fehler, STRICT 118, Projekte 25, Prüfmodus 1030 PASS (236 680 Werte) und
> byte-gleich gegen R7; Nachweisabsatz „Dreiundvierzigster Lauf". Der iOS-Bau meldet 6 Warnungen statt 11 — vier des Kerns, eine des SDK
> und **eine der Hülle**: die #202-Zeile `string ordner = Path.GetDirectoryName(datei)` in `Referenzlauf/Protokoll.cs:104` ist unter
> `Nullable=enable` ein `CS8600`; die Schranke „iOS-Warnungen = 0" ist um eine verfehlt. **#223** setzt `string?` in einer eng begrenzten
> `#nullable enable`/`restore`-Klammer (Referenzlauf.csproj und EPOS.Referenzlauf.csproj verlinken die Datei mit `Nullable=disable`, ein nacktes
> `string?` wäre dort `CS8632`); Beleg ohne Mac: `EPOS.Referenzlauf` mit `-p:Nullable=enable` vorher 1, nachher 0 Fundstellen, ohne Schalter 0/0;
> `Ergebnisexport.cs` warnungsfrei. Die Schranke wird mit dem nächsten freigegebenen iOS-Lauf geführt (kein eigener Lauf, CI-Regel).
> Gate sept29 auf `eb23302`: Kern 2 666, UI 3 776, Engine 408, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 49,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#222 (11.09.2026, `34a6dbc`, Merge `d84af29`) — Simulationsergebnis: EINE Übersicht als Dashboard Wärme | Strom (Anwenderentscheid SIM‑E‑3,
> Mockup `simulation-uebersicht.html`).** Der Hauptreiter „Übersicht" trägt je Spalte Kennzahlenzeile (Bedarf, Deckung, Rest), Ring mit
> HTML-Legende und Erzeugertabelle mit rechtsbündigen Zahlenköpfen, Einheit im Kopf, gedimmten und ausblendbaren Nullzeilen, Summen- und
> Restzeile; das Blatt „Übersicht" im Ergebnisreiter fällt (Autarkie und zwei Produktionscharts bleiben). Ringe: der ungedeckte Rest ist immer ein
> graues Segment, 0 % ein grauer Vollring mit Hinweis; Legende aus dem Bild heraus; keine Zoomleiste an Ringen (`ChartBild Rund` setzt `OhneZoom`,
> Ausnahme W8‑E‑2 in `EPOS.UI/CLAUDE.md`); Hinweisband kompakt. **Befund im Renderer:** `SKPath.ArcTo` zeichnet bei 360° nichts — ein Ring oder
> Kuchen mit einem einzigen Segment blieb weiß (deshalb der leere Kreis im Foto); `ChartRenderer.Kreissegment` zeichnet ab 360° einen Kreis,
> Wächter `ErgebnisbilderTests` und ChartProbe `ring_null_prozent` (fiel vor dem Fix rot). Abweichungen vom Mockup mit Grund: Weg nach ① nur
> Text (`SimulationSeite` gehört #221; Nachrüstung danach), Stromtabelle Erzeugung/Anteil (Eigenverbrauch und Einspeisung je Erzeuger führt kein
> DTO), Restzeile Wärme ohne Kanalaufteilung, Zusatzzeile „davon Eigenverbrauch der Wärmeerzeuger". 22 Ressourcen, 3 verwaiste entfernt,
> Designer wiederholbar; 29 neue Fälle; Konzept Simulationsablauf Abschnitt 8; Wiki-Quelle Simulation (Upload durch die Orchestrierung).
> **Offen:** Windows-Abnahme; Sprung aus dem 0‑%-Hinweis nach ① nach #221. (`BildKuchen()` ohne Aufrufer ist mit **#240** erledigt — Pfad, Bildschlüssel und Testzeile entfernt, der Renderer bleibt für den Variantenbericht.)
> Gate sept30 auf `d84af29`: Kern 2 670, UI 3 779, Engine 408, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 53,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#221 (11.09.2026, `73078dc`, Merge `c311384`) — EINE Hilfe-Pille je Bildschirm (Anwenderentscheid KI‑D‑E‑1).** `AppWurzel` führt den
> `Hilfekontext` (Schlüssel, Ansicht, Schritt, Reiter) und reicht einen `Hilfekontextmelder` als festen `CascadingValue` nach unten; jede Ansicht
> mit eigener Pille meldet in `OnAfterRender` (die Senke vergleicht) und in `Dispose` `null`; nach oben geht `HilfekontextGeaendert`, woran
> `Hauptfenster` Schlüssel, Name und `Aktiv` seiner EINEN Kopfband-Pille bindet; `KiChatKontext.AufrufGeaendert` meldet das Schließen des
> Windows-Chatfensters in diese WebView. Der `CascadingValue` `HilfePilleImKopfband` (im Hauptfenster `true`, auf iOS nicht gesetzt) lässt
> die Ansichten ihre eigene Pille weg. Die Simulationsansicht meldet 18 Felder an der Maskenbrücke an (`KiMaskennamen.SIMULATION`, Wirt
> `SimulationSeite`): Schritt und Reiter, Kaskade und nicht aufgenommene Erzeuger, fünf Laufparameter lesbar UND setzbar über
> `SimulationParameterDienste`, neun Ergebniskennzahlen aus `SimulationErgebnisHuelle.LetzterStand` (dieselbe DTO, kein zweiter Ladeweg).
> Kontextzeile `KI_KONTEXT_STELLE` („{0} · {1}"), Startfragen je Bereich; 41 Ressourcen je Sprache; Konzept KI-Assistent 3.1/6/7/8,
> `EPOS.UI/CLAUDE.md`, Wiki-Quelle Hilfe-Assistent (Upload durch die Orchestrierung). `HilfePilleTests` läuft in der seriellen Sammlung
> `KiDialogweg`, weil `Navigationsziel.Aktuell` und `KiChatKontext.Aufruf` prozessweiter Zustand sind. **Offen:** Windows-Abnahme am Gerät
> (Pille springt mit dem Schritt um, rechtes Feld leuchtet bei offenem Chat, Kontextzeile „Simulation · 3 Ergebnis · Stromspeicher");
> eine gemeinsame serielle Sammlung für alle AppWurzel-Wirte (Nachzug); `Aktiv` auf iOS nicht sichtbar, weil die Ansicht `KI_ASSISTENT`
> die Pille der abgelösten Ansicht verdrängt. Merge: die Ressourcendateien beider Aufträge (#222, #221) vereinigt, Designer neu erzeugt.
> Gate sept31 auf `c311384` ROT: `KiSimulationMaskeTests.Schritt_3_…` erwartete „420,5" und bekam „420.5" — die Maskenbrücke formatiert mit der
> Prozesskultur, die Klasse pinnte sie nicht (im Worktree des Agenten hing der Fall an der Laufreihenfolge); Fix `91885de` hängt die
> `Kulturvorrichtung` ein. Gate sept32 auf `91885de`: Kern 2 673, UI 3 812, Engine 408, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 53,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#225 (11.09.2026, `a71aa03` + `49cbb02`, Merge `9b30cfd`) — Ablaufleiste der Stromspeicher-Auslegung bündig; toter Anzahl-Schritt fällt
> (zwei Anwenderwünsche vom Bildschirmfoto).** Die Regel `.epos-ablaufleiste-aktion { margin-inline-start: auto }` schob Rechenknopf und
> hintere Stationen nach rechts; der neue, unabhängige Modifikator `Buendig` (`epos-ablaufleiste--buendig`) hebt NUR diese Marge auf, die
> Simulationsseite behält `Kompakt` (#216) — belegt durch `AblaufleisteBuendigTests` (beide Wirte). Im Kasten „Anzahl" des Größenbereichs
> stand ein deaktiviertes Schrittfeld ohne Modellbindung (`FlottenAuslegungsachse` kennt nur `AnzahlVon`/`AnzahlBis`, der Optimierer zählt in
> Einerschritten); es fällt, eine Erklärzeile `FLOTTE_ED_ANZAHL_HINWEIS` (de/en) tritt an seine Stelle. Konzept Stromspeicher-Dialoge Register
> SP‑O‑13. Gate sept33 auf `9b30cfd`: Kern 2 673, UI 3 814, Engine 408, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben
> 53, Referenzlauf 5/5 byte-gleich gegen R7. Am selben Abend kam mit dem Sync `78bcf88` die Excel-Mappe V7 (71 MB) ins Repository.
>
> **#220 (11.09.2026, `989569e`, Merge `97dc344`) — Startseiten-Reiter „Simulation": die Kachel rechnet an Ort und Stelle, rechts das Ergebnis
> (Anwenderentscheid SIM‑E‑2, Option 1).** `SimulationLaufsteuerung` ist die EINE Wahrheit für Schritt ② der Ansicht und die Kachel des Reiters
> (Sperrgrund in der Reihenfolge fremder Lauf → rote Vorprüfung → ungespeicherte Konfiguration, Fortschritt, Abbrechen, Start); der Lauf
> selbst bleibt bei der `SimulationErgebnisSeite` (Zusätze `FortschrittZeigen`, `Anteil`, `Fortschrittstext`, `AbbruchMoeglich`,
> `LaufAbbrechen()`). Die `SimulationLaufsperre` liegt je Projekt in der `SimulationAnsichtQuelle` und geht über `SimulationAnsichtDienste`
> in jeden Parametersatz — Ansicht und Reiter sperren sich gegenseitig (`SIM_LAUF_ANDERSWO`). Dienste aus einer Quelle:
> `AppWurzel.SimulationGabenHolen()` bündelt Hüllen-Delegat und `Quelle.SimulationGaben` und reicht sie an die Startseite (iOS-Weg
> unverändert). Rückwegmarke mit Wirtkennung `wirt=START;schritt=3;blatt=…` (`SimulationMarke.WIRT_START`); der Reiter meldet seinen
> Hilfekontext „Startseite · Simulation · ⟨Blatt⟩" ohne eigene Pille (#221). 14 neue bunit-Fälle; Konzept Simulationsablauf Abschnitt 9;
> Wiki-Quelle Simulation (Abschnitt „Der Reiter Simulation der Startseite", Anker `startreiter`; Upload durch die Orchestrierung).
> **Offen:** Windows-Abnahme (Zweispaltigkeit ab 1 100 px, Kachelbreite, Höhe der Ergebnisseite im Reiter); der Reiter „Simulation" ist
> unter Windows der einzige Startseiten-Reiter ohne eigene Info-Pille (sie steht im Kopfband) — sichtbare Ungleichheit, vom Anwender
> abzunehmen. Merge-Konflikt nur in `EPOS.UI/CLAUDE.md` (Absätze #225 und #220 vereinigt). Gate sept34 auf dem Arbeitsbaum des Merges
> `97dc344` (die Kopfzeile des Protokolls nennt noch `72b90b1`, weil der Lauf vor dem Merge-Commit startete): Kern 2 673, UI 3 828,
> Engine 408, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 53, Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#227 (11.09.2026, `4690f57`, Merge `49c2e6f`) — Hilfe-Assistent: Startzeile „Aktuellen Dialog erklären" mit Kontext (Anwenderhinweis
> zum Bildschirmfoto mit zwei Pillen).** Bisher landete die vorbereitete Frage `KI_FRAGE_*` nur als Vorbelegung im Eingabefeld; der Assistent
> zeigte nicht, dass er den Bildschirm kennt. Jetzt steht beim Öffnen aus einer Pille über dem Eingabefeld eine Startzeile: links die
> Kontextzeile (Ansicht · Schritt · Reiter bzw. Dialogname), rechts „Aktuellen Dialog erklären" (schickt die vorbereitete Frage, sonst eine
> allgemeine mit dem Bildschirmnamen) und „Was kann ich hier tun?"; sie verschwindet mit der ersten Nachricht, fehlt beim Menüweg
> (kein Hilfeschlüssel) und ist ohne Einrichtung weich gesperrt (`aria-disabled`, KI‑D‑Q1). `KiKontextangabe` trägt den Hilfeschlüssel
> bis zur Komponente (Windows-Hülle und `AppWurzel`); die Pille nennt im Tooltip den Bildschirm („Simulation vom Hilfe-Assistenten
> erklären lassen"). 7 Ressourcen de/en, 11 neue bunit-Fälle; Konzept KI-Assistent 3.1; Wiki Hilfe-Assistent Revision 553.
> Der Zweig entstand noch auf der alten Linie (Basis 9b30cfd) und wurde vor dem Merge auf die bereinigte Historie umgesetzt (Basis a5f7ff7).
> **Historie:** Am Abend kam mit einem Sync (78bcf88/0be2b0c) die Excel-Mappe V7 (71 MB) in den Zweig; nach Anwenderentscheid
> („Historie umschreiben", Freigabe für `git rebase`/`push --force-with-lease`) wurde der Zweig ab b516f22 ohne diese zwei Commits neu
> aufgebaut (Baum identisch, alle Commits signiert, Merges erhalten; `.gitignore` sperrt `*.xlsm`/`*.xlsb`).
> Gate sept35 auf `49c2e6f`: Kern 2 673, UI 3 839, Engine 408, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 53,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#226 (11.09.2026, `8c3a901`, Merge `e0c9c6a`) — Größen-Sicht: Rasterkarte und Schnitte folgen der Größenkopplung (Anwenderbefund, zwei
> Bildschirmfotos).** Ursache belegt: `SpeicherFlottenAnzeigeCtrl.Groessen.cs:133` baute die Spaltenachse IMMER aus der C-Rate — bei Kopplung
> „Kapazität und Leistung" (13 × 13 Kandidaten, 20…500/40) sind das 137 krumme C-Raten, 1 781 Stellen mit mindestens 1 612 Löchern; und
> `ChartRenderer.Rasterfarbe` gab jedem `NaN` die Minimumfarbe — daher das rote Feld (Gegenprobe: ohne den Fix waren Loch und Minimum
> byte-gleich). Jetzt führt `FlottenAuslegungErgebnis.Achsenmodus` (aus der ersten aktiven Suchachse) die eine Quelle; `FlottenRasterdaten`
> trägt Modus, Zeilen- und Spaltenwerte samt Größenart; die Schnitte heißen `SchnittdatenBeiSpalte`/`BeiZeile` (Achse je Kopplung unmittelbar,
> `P = E·C` oder `E = P/C`); Titel, Achsen-, Schieber- und `alt`-Texte kommen je Kopplung aus dem Kern (zehn Ressourcen de/en); Löcher hellgrau
> (`C_RASTER_LOCH`), Schraffur und SP‑O‑4-Fußzeile unverändert. 17 neue Fälle (Kern +9, UI +4, Engine +4); ChartProben 55 (+Rasterbild
> Kapazität × Leistung, +Gegenprobe Loch ≠ Minimum); Konzept 2.5 und Register SP‑O‑14 (beim Merge von SP‑O‑13 umnummeriert, #225 trägt
> SP‑O‑13); Wiki-Quelle Stromspeicher (Upload durch die Orchestrierung). **Offen:** Windows-Abnahme; ein einmaliger, nicht reproduzierter
> UI-Testfall in einem von vier Läufen des Agenten (Name nicht erfasst) — bleibt beobachtet.
> Gate sept36 auf `e0c9c6a`: Kern 2 682, UI 3 843, Engine 412, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 55,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#229 (11.09.2026, `7edbc29`, Merge `e53eba8`) — die EPOS-Marke als Programmsymbol (Anwenderwunsch „nehme das EPOS-ICON als
> Programm-Symbol").** Befund: `WindowsFormsApplication1.csproj` führte kein `ApplicationIcon`, die Exe zeigte in Taskleiste, Alt‑Tab und
> Explorer das Standardsymbol; die im Setup nur optional verdrahtete `Setup/EPOS-Plan.ico` lag weder im Arbeitsbaum noch je in der
> Git-Geschichte. Jetzt: `WindowsFormsApplication1/Resources/EPOS-Plan.ico` (19 054 Byte, sieben Stufen 16…256 px, aus der Marke des
> Bausteins `InfoKnopf` — drei Felder, weißer Kern, Blitz — erzeugt; alle Stufen PNG-komprimiert), `ApplicationIcon` im csproj,
> `Programmsymbol.Anwenden(Form)` lädt das Symbol aus der laufenden Exe (eine Quelle) für `Hauptfensterrahmen`, `BlazorDialogForm` (und
> damit `KiChatHuelle`) und `Form_HelpPopup`; `SetupIconFile` zeigt auf dieselbe Datei, `[Icons]`/`UninstallDisplayIcon` erben es über die
> Exe. `EPOS.iOS/Resources/AppIcon/appicon.svg` trägt statt des „EP"-Platzhalters (Kopfkommentar „vorläufig bis iU13") dieselbe Marke —
> trifft die Hülle, **kein iOS-Lauf ausgelöst** (Regel vom 09.09.2026); Nachweis mit dem nächsten freigegebenen Lauf. Wache
> `ProgrammsymbolWacheTests` (3 Fälle: `ApplicationIcon` gesetzt, ICO-Kopf und Stufen 16/32/48, Setup und Anwendung nennen dieselbe
> Datei). Der Zweig entstand noch auf der alten Linie und wurde vor dem Merge auf `d5c8c98` umgesetzt. **Offen:** Windows-Abnahme
> (Taskleiste, Alt‑Tab, Explorer, Fensterköpfe, Installer-Symbol; sollten PNG-komprimierte Kleinstufen irgendwo weiß bleiben, BMP-Stufen
> für 16…48 px nachziehen); Windows-CI-Lauf auf dem Push beobachten (erster Build mit `ApplicationIcon`).
> Gate sept37 auf `e53eba8`: Kern 2 685, UI 3 843, Engine 412, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 55,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#228 (11.09.2026, `785dc96`, Merge `e462a84`) — Hilfe-Assistent öffnet wieder aus Hauptmenü und F1 (Anwenderbefund, Konzept KI‑D‑B‑3).**
> Zwei getrennte Ursachen, beide in der Windows-Hülle: (1) Der Menüpunkt läuft seit #219 ZWEIMAL durch `Blazorsprung` — `HauptfensterHuelle.Weg`
> verzögert jeden Maskenschlüssel (`Masken.KiAssistent` ist einer, der eigene `case` dort ist auf dem Menüweg tot), und `WinFormsNavigation`
> verzögert im geposteten Sprung noch einmal; der Riegel `_angefordert` fiel aber erst im `finally` am ENDE des Sprungs, also traf der innere
> Ruf den Riegel des äußeren und kehrte stumm zurück (der Klassenkopf versprach seit W16b den Beginn). (2) F1 hing an `KeyPreview`/`KeyDown` —
> das wirkt nur für Tasten im `WndProc` eines WinForms-Steuerelements, und seit W16c sitzt der Tastaturzeiger im nativen Browserfenster der
> WebView2; F1 war seit W16c nie am Gerät geprüft. Jetzt: der Riegel fällt als Erstes in `Ausfuehren` (ein innerer Sprung reiht sich regulär
> ein), `RiegelSteht()` protokolliert jedes Abweisen und lässt einen verwaisten Riegel nach 5 s verfallen (eine `BeginInvoke`-Nachricht läuft
> nie, wenn ihr Wirtsfenster vorher fällt), `Wirtsfenster()` fällt von `Form.ActiveForm` auf den `Hauptfensterrahmen` zurück — EINE Ermittlung
> für Nachrichtenschlange und Fensterbesitzer; F1 über `ProcessCmdKey` (erreicht die Taste auch aus der WebView2); `KiChatHuelle` setzt
> `_offene` erst nach gelungenem Bau und hängt bei einem Fehlschlag wieder aus (vorher blieb eine Hülle ohne Fenster stehen, die nie ein
> `FormClosed` meldete — jedes weitere Öffnen „holte sie nach vorn"). Keine Zeile in Kern oder Oberfläche; sechs Quelltextwachen
> `KiChatOeffnerTests` (Riegelreihenfolge mit Gegenprobe, Protokoll und Verfall, Wirtsfenster-Rückfall, F1 über `ProcessCmdKey`, Lebenszyklus);
> Konzept Kapitel 8 KI‑D‑B‑3 mit Abnahmeliste; `WindowsFormsApplication1/CLAUDE.md` Regeln (f) und (g). Der Zweig entstand auf der alten Linie
> und wurde vor dem Merge auf `d5c8c98` umgesetzt; Konflikt in `Hauptfensterrahmen.cs` mit #229 (Programmsymbol-Aufruf bleibt, `KeyPreview`
> fällt). **Offen:** Windows-Abnahme — Menü Hilfe → KI-Assistent zweimal hintereinander, F1 mit dem Zeiger in der WebView2, Pille aus Ansicht und
> Dialog, jeweils nach vorherigem Öffnen und Schließen.
> Gate sept38 auf `e462a84`: Kern 2 685, UI 3 849, Engine 412, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 55,
> Referenzlauf 5/5 byte-gleich gegen R7.
>
> **#230 (11.09.2026, `16dbbb2`, Merge `b8855a4`) — Windows-CI rot seit Lauf 262: dreizehn Kern-Testfälle prüfen deutsche Texte, der
> Läufer ist en-US.** Befund der Orchestrierung beim Beobachten des #229-Pushs: `windows.yml` (`build-test`, `windows-latest`) war seit
> Lauf 262 (`0ec96e3`, 12:09 UTC, Statusblöcke #185/#183/#186) rot, letzter grüner Lauf 261 (`b35c88c`); Lauf 315 auf `e19ef8c`: 12 von
> 2 673 Kern-Fällen rot, alle übrigen Projekte grün. Die Fälle halten deutsche Texte gegen `Contains`/`Equal` („Betriebskostenkoeffizienten",
> „Ja", „Speicherflotte", „nicht bewertbar"), der en-US-Läufer liefert die englischen Satellitentexte — die Ressourcen folgen
> `CurrentUICulture`, nicht `CurrentCulture`. Linux (Gate, `kern.yml`) läuft invariant, die neutrale `.resx` ist deutsch, deshalb dort grün
> — das Gate hatte an dieser Stelle einen blinden Fleck. Reproduktion auf Linux mit `LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8` (ICU, keine
> glibc-Locale nötig): dieselben zwölf Fälle plus ein dreizehnter, nirgends gemeldeter
> (`SpeicherAuslegungRechnungTests.Fehlende_gewaehlte_Kostenmodul_Kategorie_ist_ein_Fehler`). Fix nach dem Hausmuster #167/#168: neue
> gemeinsame `EPOS.Kern.Tests/Kulturvorrichtung.cs` (baugleich zur UI-Fassung, ohne bunit; pinnt vier Werte auf `de-DE`, stellt einzeln
> zurück) in fünf Klassen als `IDisposable`-Feld, `KiMaskenbrueckeTests` tauscht seinen `CurrentCulture`-Handstand dagegen, der
> dreizehnte Fall pinnt lokal; Produktcode unverändert, Texte bleiben deutsch. Wächter bewusst NICHT erweitert: die Regel „Klasse
> referenziert `Resource.`" hätte drei der fünf Klassen nicht gefunden (ihre Texte kommen aus Kern-Ausnahmen). **Gate seit sept39:**
> die Kern-/UI-Tests laufen zusätzlich unter en-US (`LANG`/`LC_ALL`), damit der Windows-Läufer nicht mehr die einzige Stelle ist, die
> kulturabhängige Asserts findet. **Nebenbefund #231** (Anwenderentscheid ausstehend, wie #168): die prozessweite
> `DefaultThreadCurrent(UI)Culture`-Pinnung in über 50 Testdateien schlägt bei PARALLELER Sammlungsausführung (xunit-Vorgabe, so läuft
> die CI) auf Worker-Threads paralleler Kern-Rechnungen fremder Tests durch — unter en-US in 2 von 11 Läufen je ein Fall rot
> (`SpeicherOptimierungCtrlTests`, `PeakShavingBildTests`); das Gate sieht es nicht (Sammlungen dort nicht parallel).
> `EPOS.Kern/CLAUDE.md` trägt beides. **#230b (`5dedd3c`, Merge `c0be2cf`):** der erste en-US-Lauf des Gates (sept39) fand sofort drei
> UI-Klassen ohne Vorrichtung (`KostenKomponenteDialogTests`, `SimulationKonfigSeiteTests`, `SpeicherParameterBlockTests`; 174 von 197
> UI-Klassen pinnen nicht und liefen unter en-US nur dank des #168-Bestands grün) — gepinnt; Befund dabei: `SpeicherParameterBlockTests`
> erbte die Pinnung aus `EposBunitContext` und fiel trotzdem, weil C# die Feldinitialisierer der abgeleiteten Klasse VOR dem
> Basiskonstruktor auswertet — ein Ressourcenwert im Feldinitialisierer fror auf Englisch ein; jetzt eigenes, zuerst deklariertes
> `Kulturvorrichtung`-Feld, Wert erst im Konstruktorrumpf, `base.Dispose` vor `_kultur.Dispose`. `EPOS.UI/CLAUDE.md` Regelpunkt.
> **Windows-CI danach:** Lauf 320 auf `b663060` GRÜN — der erste grüne Windows-Lauf seit Lauf 261 (Kern 2 685, UI 3 893, Engine 425).
> Lauf 319 auf `cf65e71` (derselbe Programmstand, nur der Statusblock-Commit davor) fiel mit GENAU EINEM Fall
> (`SpeicherOptimierungCtrlTests.Die_Kennzahlen_tragen_die_Zahlen_des_Bestpunkts`, „Sequence contains no matching element": die Kennzahl wird
> über ihre deutsche Bezeichnung gesucht, ein Worker-Thread rechnete englisch) — das ist der Nebenbefund #231 in freier Wildbahn, 1 von
> 2 685, auf identischem Programmstand einmal rot und einmal grün. **Offen:** Anwenderentscheid #231 (Empfehlung: `windows.yml`/`kern.yml`
> mit `-- xUnit.ParallelizeTestCollections=false` wie das Gate, danach Kern-Texte mit expliziter Kultur als eigene Welle).
> Gate sept41 auf `c0be2cf`: Kern 2 685, UI 3 893, Engine 425, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 57,
> Referenzlauf 5/5 byte-gleich gegen R7; en-US-Lauf aller fünf Testprojekte grün.
>
> **#224 (11.09.2026, `b4c4063`, Merge `4d46cb1`) — Stromspeicher-Auslegung: Station „4 Optimierung" als Seite, Feinraster, Kasten
> „Bestes Ergebnis", Editor neu geordnet (Anwenderentscheid SD‑E‑9 = Empfehlung, Option A; SD‑Q10 Feinraster nur auf der Größenachse,
> SD‑Q11 Ziel Kapitalwert + jährliche Ersparnis, SD‑Q12 Jahresprojektion nach Schritt 2; Darstellung nach Konzept 7.8).** Ablaufleiste
> `1 Speicher · 2 Daten & Kosten · 3 Betriebsführung · 4 Optimierung · 5 Ergebnis` als Stufenleiste (nummerierte Kreise, aktiv/erledigt/
> kommend, „veraltet" als Pille; die Simulationsansicht bekommt dieselben Kreise, Lage nach #216 unverändert), der Rechenknopf steht im
> Blatt 4 (`Ablaufleiste MitAktion="false"`). Gewandert: Schalter „Größen optimieren" und Suchbereiche je Einheit von Schritt 1 in die
> Suchraumtabelle der Station 4 (`OptimierungBlock.razor`, 397 Z.); die Größen-Sicht von Schritt 5 unter den Kasten „Bestes Ergebnis"
> (Kapitalwert, jährliche Ersparnis, Kapazität · C‑Rate · Leistung, Anzahl, geprüfte/zulässige Kandidaten, Rechendauer, Marke Grob/Fein);
> „Netz und Planung" von Schritt 1 nach 3 (`SpeicherFlottenNetzBlock`); die Jahresprojektion mit drei Erklärzeilen (Ausgleichswert,
> Restwert der Studie, Kandidatengrenze) nach Schritt 2 (`SpeicherFlottenWirtschaftBlock`); die Vorprüfung als kompakte `Hinweiszeilen`
> (ab zwei aufklappbar). Der Editor schrumpft 675 → 540 Zeilen; vier Blätter schreiben dieselbe `FlottenStudieKonfiguration`,
> `FlotteGeschrieben()` setzt dem Editor eine frische Kopie. **Feinraster (Phase 2, `FlottenOptimierer`):** Fenster
> `[max(von, E*−Δ), min(bis, E*+Δ)]` um die Größe des Grob-Optimums, Mindestbreite 1 kWh, Schrittweite Δ/9, nur die ERSTE aktive
> Suchachse, übrige Achsen/Stückzahl/Ziel beim Grob-Optimum, Gewinn nur bei STRIKT besserem Kapitalwert (Gleichstand → Grob bleibt);
> `Kandidatenzahl` ist die eine Zählregel für Lauf und Kandidatenzeile, `MaximaleKandidaten` zählt Grob + Obergrenze Fein und weist vor
> Phase 1 ab; Phasenmarke am Kandidaten, Abbruch und Fortschritt über beide Phasen; vorbelegt an, serialisiert. `Zahlenfeld` zeigt
> höchstens vier Nachkommastellen (`0.####`, Parameter `Nachkommastellen`), gespeicherter Wert unverändert. Sieben begründete
> Abweichungen vom Mockup in Konzept 7.9 (u. a. nur erste Achse verfeinert; Kandidatenzeile nennt für Phase 2 eine Obergrenze; Grob/Fein
> nur in der Schnittkurve unterscheidbar, `C_FEINRASTER`; keine geschätzte Rechendauer; „Netz und Planung" in 3 statt 1). 37 neue Fälle
> (Engine +13 `FlottenFeinrasterTests`, UI +24); ChartProben 57 Bilder / 13 Gegenproben (+`flottenschnitt_feinraster`,
> +`flottenschnitt_feinpunkte_wirken`); ~50 Ressourcen; Konzept Kap. 5 (P6/P8), Register SP‑O‑15/16, 7.9; Rechenweg-Wiki Stromspeicher
> (Feinraster, Gleichungen 47–49, Folgegleichungen nachgezogen) und Benutzer-Wiki Stromspeicher (Schritte 1–5 neu; Upload durch die
> Orchestrierung); `Doku_Mehrspeicher_Konzept_und_Umsetzung.md`; drei CLAUDE.md. Nebenbei: doppelte Tabellenzeilen in
> `EPOS.UI/CLAUDE.md` aus einem früheren Merge bereinigt. Der Agentencommit trug zunächst einen abweichenden Trailer und wurde vor dem
> Merge auf die Sitzungsvorgabe umgeschrieben. **Offen:** Windows-Abnahme (Stufenleiste, Suchraumtabelle, Kasten bei 125 % DPI);
> kein iOS-Lauf (trifft die Hülle nicht), optischer Beleg auf dem iPad fehlt.
> Gate sept41 auf `c0be2cf` (Stand nach #230b): Kern 2 685, UI 3 893, Engine 425, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 57,
> Referenzlauf 5/5 byte-gleich gegen R7; en-US-Lauf grün.
>
> **#233 (12.09.2026, `c4857b5`, Merge `2f57bcc`) — Startseiten-Reiter „Simulation" als Bedienblock (Anwenderrückmeldung „das Layout ist nicht
> gut/stimmt nicht — Größe, Lesbarkeit", Bildschirmfoto).** Befund: links drei Elemente in drei Breiten (Projektzusammenfassung ~600 px mit
> blauen Werten, grauer Knopf „Simulation Konfiguration…" ~355 px, eine 190 px schmale Startseiten-Kachel „Simulation starten" mit 84-px-Sinnbild
> und dreizeilig umgebrochenem Untertitel), rechts eine große leere Fläche „Ergebnis" mit aktivem „Ergebnis speichern" und einer einsamen
> Hinweiszeile — das Kachelraster der Drei-Spalten-Reiter (W16b‑E‑7) passt in einer schmalen Spalte nicht. Jetzt: linker Bedienblock fester
> Breite (360 px) mit kompakter Zusammenfassung (Werte halbfett in Textfarbe statt Markenblau, Kontrast 6,8 → 13,4 : 1), Hauptknopf
> „Simulation starten ▶" in Blockbreite mit grauer Erklärzeile, Zweitknopf „Simulation Konfiguration…" in Blockbreite (Text und Sinnbild
> bleiben: Ressourcen liegen im Kern, W16b‑E‑3), darunter Fortschritt/Abbrechen/Sperrgrund; rechts „Ergebnis speichern" ohne Ergebnis
> gesperrt (Tooltip = Hinweistext), Leerzustand als ruhige Karte mit dem Kachelbild; unter 1 100 px untereinander. Gefallen: das Kachelraster
> im Reiter, drei Kachelhelfer, das Warnbanner am Spaltenfuß, die 1fr:2fr-Aufteilung; geblieben: alle Kachelschlüssel/-bilder, Ressourcen,
> Wege, Sperrprüfung, Lauf, Marke — keine Hüllenänderung, kein iOS-Lauf. Elf neue bunit-Fälle (5 Bedienung, 6 Stilblatt); Regel in
> `EPOS.UI/CLAUDE.md` („Kachelraster nur in Drei-Spalten-Reitern, im Zweispalten-Reiter ein Bedienblock"); Konzept Simulationsablauf 9.8;
> Wiki-Quelle Simulation (Upload durch die Orchestrierung). **Offen:** Windows-Sichtprüfung des Blocks.
> Gate sept42 auf `2f57bcc`: Kern 2 685, UI 3 904, Engine 425, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 57,
> Referenzlauf 5/5 byte-gleich gegen R7; en-US-Lauf grün.
>
> **#231/#232/#232b (12.09.2026, Anwenderentscheid „#231: Empfehlung umsetzen") — Race der prozessweiten Kulturpinnung: CI ohne parallele
> Sammlungen, Kulturweitergabe an die Worker, UI-Kultur auf dem Testfaden.** **Teil a (#231a, `e491b5a`, Merge `a63d230`, direkt gepusht —
> CI-Konfiguration und Doku, das Gate prüft Workflows nicht):** `windows.yml` und `kern.yml` fahren die Kern-slnf mit
> `-- xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2` wie das Gate; Nachweis Windows-Lauf 322 (6 min 29 s, nicht langsamer
> als parallel) und Kern-Lauf 377 grün; `KiKern.Tests` hatte `DisableTestParallelization` schon. **Teil b (#232, `1472b6a`, Merge `6085a09`):**
> `SpeicherEngine/Kulturweitergabe.cs` (264 Z.; Abhängigkeitsrichtung `EPOS.Kern → SpeicherEngine`, deshalb dort, eine Klasse für alle) —
> `Erfassen`, `For`/`ForEach`, `Starten`/`StartenAsync`, `Kulturstand.Halter` setzt beide Werte auf dem Arbeitsfaden und stellt zurück, kein
> `AsyncLocal`, kein Griff an `DefaultThreadCurrent*`; zwölf Stellen umgestellt (das `Parallel.For` der Rastersuche, drei `Task.Run` im KI-Teil
> des Kerns, acht in den Hüllen von `EPOS.UI.Daten`), `FlottenOptimierer` rechnet sequenziell (nichts zu tun), die zwei `Task.Run` in
> `EPOS.UI` bleiben außerhalb (Bedienfaden der WebView, begründet im Wächterkopf); Wächter `ParallelitaetWacheTests` (6 Fälle: keine nackte
> Parallelität in Kern, Engine, KiKern, EPOS.UI.Daten); Belegtest `KulturweitergabeTests` (9 Fälle) mit Testdoppel-`TaskScheduler` auf drei
> en-US-Fäden und Aufrufer `de-AT`: über die Hülle deutsch, mit nacktem `Parallel.For` englisch; Referenzlauf 1030/1046 byte-gleich.
> **Messbefund, der #230 korrigiert:** ein AUSDRÜCKLICH auf dem Faden gesetzter Wert fließt seit .NET Core über den `ExecutionContext` in
> `Task.Run`, `Parallel.For` und neue Threads mit — „Aufrufer gepinnt, Worker nicht" stimmte so nicht; die echte Lücke war der Aufrufer, der
> selbst nichts setzt und seine UI-Kultur aus dem veränderlichen Vorgabewert bezieht. Genau das waren die wandernden Fälle aus Lauf 319:
> `SpeicherOptimierungCtrlTests` und `SpeicherOptimierungLastspitzeTests` pinnten nur `CurrentCulture`, `PeakShavingBildTests` nichts.
> **Rest (#232b, `aa668a4`):** die drei Klassen pinnen `CurrentCulture` und `CurrentUICulture` THREADGEBUNDEN (kein `DefaultThreadCurrent*`,
> nicht die #230-Vorrichtung, die prozessweit setzt); Messung: EPOS.Kern.Tests parallel unter en-US 10/10 grün (2 700 Fälle) — vorher 2 von 10 rot.
> `EPOS.Kern/CLAUDE.md` trägt Regel und Messung, `Doku_Mehrspeicher_Konzept_und_Umsetzung.md` den Satz zur Rastersuche.
> **Windows-Abnahme #229:** Anwender 12.09.2026 „Programmsymbol ok" (nach Neubau; Taskleiste, Fenster, Explorer).
> Gate sept43 auf `d5a3a29`: Kern 2 700, UI 3 904, Engine 425, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 57,
> Referenzlauf 5/5 byte-gleich gegen R7; en-US-Lauf grün.
>
> **#234 (12.09.2026, Anwenderrückmeldung mit zwei Bildschirmfotos „Ergebnis › Wärme Produktion Chart": „Die Grafik ist zu Beginn leer …
> Einheiten X-Achse fehlen. Nur bei Pufferspeicher zweite Achse, nicht für Wärmelast") — Vorbelegung, Sitzungsgedächtnis,
> Achseneinheiten, zweite Achse nur für den Speicherinhalt (`8411b60`, Merge `f6e0b58`).** Befund: `WaermegangReiter` startete ohne gewählte
> Reihe und ohne Bedarfslinie (Leerhinweis des Renderers), `Reiterblatt` baut die Komponente je Blattwechsel neu (`@if (Sichtbar)`), so
> dass jede Anwenderwahl verfiel; `ChartRenderer.XAchse`/`XAchseFenster` zeichneten Marken ohne Achsentitel; `BildWaermegang` legte den
> Wärmebedarf [kW] auf die ZWEITE Achse („Wärmelast") und die Speicherfüllstände [kWh] auf die Primärachse — verkehrt herum, denn
> Bedarf und Produktion sind dieselbe Größe. **Umsetzung (`8411b60`, Agent Opus; 16 Dateien, +663/−90):** `Ganglinienstand`/`Ganglinienregister` in `EPOS.UI/Seiten/Simulation/` (Bedarfsart,
> sortiert, Bedarfslinie, Reihen als SCHLÜSSEL; ein Stand je Reiter, prozessweit unter Schloss, Bauart S2.5 „Filterstand je Katalog"; Testisolation
> über den Parameter `Gedaechtnis`, kein Datenzoom, keine Dauerpersistenz); `WaermegangReiter` belegt beim ersten Aufbau alle Erzeuger, alle
> Speicher und die Bedarfslinie vor, `StromgangReiter` behält „nur Gesamt" und bekommt dasselbe Gedächtnis. `ChartRenderer`: Helfer
> `XAchsentitel` mittig bei `rc.Bottom + 30` aus `XAchse` und `XAchseFenster` (wirkt auf ErzeugerStapel, GanglinieNormiert, Speicherbetrieb,
> gezoomten Jahresverlauf); `zweiteAchse` → `zweiteAchsen` (Liste, gemeinsame Nice-Obergrenze, Reihenfarbe bei einer, DimGray bei mehreren);
> Zusatzfund: der y2-Titel stand starr bei `rc.Right − 40` und wurde abgeschnitten, rückt jetzt nach links. `BildWaermegang`: Wärmebedarf als Linie
> auf der Primärachse „Leistung [kW]", Speicherfüllstände auf der zweiten Achse „Speicherinhalt [kWh]", nur mit gewähltem Speicher. Ressourcen:
> `CHART_ACHSE_JAHRESSTUNDEN` → „Jahresstunden [h]" und `CHART_ACHSE_LEISTUNG` → „Leistung [kW]" umgewidmet (ohne bzw. nur kW-Nutzer), neu
> `CHART_ACHSE_SPEICHERINHALT_KWH`, `CHART_ACHSE_LEISTUNG_SPEICHERINHALT` gelöscht (ohne Nutzer), `CHART_ACHSE_WAERMELAST` bleibt (vier Nutzer).
> bunit +5 (`GangUndErgebnisReiterTests`), ChartProben 57/12 → 59/13 (`erzeugerstapel_zwei_speicher`, Gegenprobe `erzeugerstapel_zweite_achse`).
> Wiki `Programm Dokumentation/Simulation` Revision 560 (Absatz zum Blatt Wärmeproduktion; Revision 558 war eine Handbearbeitung des Anwenders,
> die Revision 559 überschrieb — 560 stellt sie wieder her, die Repo-Quelle ist angeglichen, Regel im Hilfesystem-Konzept).
> **Offen:** bei vielen Reihen bricht die Legende des `ErzeugerStapel` in eine zweite Zeile und liegt auf dem y-Titel (kein Regress; Behebung
> nach dem Muster W11b‑B‑28 aus `Speicherbetrieb`); der `Jahresverlauf` zeichnet seine Monatsnamen selbst und trägt in der Vollansicht keinen
> Achsentitel (bewusst); Windows-Abnahme der drei Diagramme.
> Gate sept44 auf `f6e0b58`: Kern 2 700, UI 3 909, Engine 425, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 59
> Bilder / 13 Gegenproben, Referenzlauf 5/5 byte-gleich gegen R7 (kein Rechenweg berührt); en-US-Lauf grün.
>
> **#235 (12.09.2026, Anwenderrückmeldung mit Bildschirmfoto „Stromspeicher Einlesen": „die Auswahlliste flackert bei großen Datenlisten
> immer noch") — Restfall #212, diesmal im echten Browser gemessen und behoben (`64d82c6`, Merge `347c92e`, Agent Opus; 18 Dateien, +1 385/−43).**
> #212 hatte drei Zeichenkosten beseitigt, aber nur in bunit gemessen — ohne Layout, ohne JavaScript, ohne die Sichtbarkeitsmelder von
> `Virtualize`. **Messung** (Blazor-Server-Wirt mit `EPOS.UI`, Playwright 1.56 auf Chromium headless, 6 654 Sätze, echtes
> Stromspeicher-Listenprofil, neun Fälle: 1 300 × 900, 1 300 × 700, Zoom 1,25, Ladeweg „CEC-Liste abrufen", 20 746 Zeilen, Filtern, 119 Zeilen,
> Zeichentakt 10/s, Gegenprobe): die echte Zeile war **48,2 px** statt der von #212 gerechneten 53 (QuickGrids eigenes Stilblatt gewinnt über
> die Zellenpolsterung: 1,6 px statt 4 px), die Platzhalterzeile **21,9 px**. `Virtualize` misst nicht, es rechnet mit `ItemSize` — die zwei
> Abstandshalter kamen auf verschiedene Anfangszeilen und schoben das Fenster **alle 33 ms** gegeneinander (Sichtbarkeitsmelder 366–374 in
> 3 s), jeder Sprung stellte QuickGrids Datenanforderung hinter der 100‑ms‑Entprellung neu an: nach Rollen um 2 000 px **16 Platzhalter,
> dauerhaft**; das Bildschirmfoto der Probe glich dem des Anwenders. **Verworfen mit Messwert:** Rollbehälter (`clientHeight` 418, richtig),
> sticky `thead` (vorderer Halter `isIntersecting: false`), Klasse `loading` (0 Umschaltungen in allen neun Fällen — das „Blinken" aus #212 war
> nie die Klasse), `@key`-Wechsel beim Laden/Filtern, Fortschrittsmeldungen/Zeichentakt, Behältermaß beim ersten Messen.
> **Fix an EINER Stelle:** dieselbe Zahl geht als `ItemSize` UND als `--epos-rasterzeile` an die Hülle (`Raster.Hoehenstil`), das Stilblatt gibt sie
> beiden Zeilenarten (echte Zeile und Platzhalter je **53,0 px**). Danach: Sichtbarkeitsmelder in 3 s **4**, nach Rollen 0 Platzhalter / 16 echte
> Zeilen binnen 115–139 ms, Abstandshalter stehen. Virtualisierung bleibt vollständig, **kein Rückfall** auf Seitenblättern. Die Probe ist
> dauerhaft: `Proben/Rasterprobe/` (Skript 485 Z., minimaler Wirt, LIESMICH mit Messwerten; nicht in sln/slnf, keine CI; Rückgabe 0/1/2),
> Werkzeugzeile in der Wurzel-`CLAUDE.md`; `RasterTests` +3, #212-Wache in `KataloglisteTests` berichtigt; Konzept Stromspeicherimport
> „Befund #235", `EPOS.UI/CLAUDE.md`. **Offen:** Windows-Abnahme unter WebView2 mit 125 % DPI (WebView2 reicht sie als `zoomFactor`
> weiter, Fall D nähert das nur an); der ganze `KatalogImportDialog` ist nicht gehostet; Nebenbefund: die Hausregel `.epos-raster th, td
> { padding: 4px 8px }` wirkt in keinem QuickGrid-Raster (QuickGrids Regel ist spezifischer) — eigener Auftrag, weil sie jede Liste änderte.
> Gate sept45 auf `347c92e` (gelaufen auf dem byte-gleichen Baum des ersten Merges `038d63b`; Welle und Merge wurden danach mit identischem
> Baum nachsigniert, weil der Agentencommit aus dem Worktree unsigniert war): Kern 2 700, UI 3 912, Engine 425, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 59,
> Referenzlauf 5/5 byte-gleich gegen R7 (kein Rechenweg berührt); en-US-Lauf grün.
>
> **#237 (12.09.2026, Anwenderwunsch mit Bildschirmfoto „Als Variante speichern": „Ermögliche eine Variante aus einem bestehenden Projekt
> anzulegen (Auswahl Checkbox im Dialog) … default ist Variantenbezeichner = Projektname des ausgewählten zu übernehmenden Projektes")
> — umgesetzt (`47f1337`, Merge `8169757`, Agent Opus; 13 Dateien, +1 386/−113).** Der Dialog „Als Variante speichern" bekommt das
> Kontrollkästchen „Inhalt aus einem bestehenden Projekt übernehmen"; angehakt erscheint darunter die Projektliste (Baustein `ProjektListe`,
> ohne Vorauswahl, begrenzte Höhe), die Wahl belegt den Bezeichner mit dem Projektnamen der Quelle vor — ein von Hand getippter Bezeichner
> bleibt stehen (Von-Hand-Merker) —, und unter dem Feld steht laufend der Zielname aus DERSELBEN Kernregel (`VariantenCtrl.Zielname`:
> `<Stamm> - <Bezeichner>`, bei Kollision Zählnummer). **Kern:** `AnlegenAusStamm(idStamm, stammName, bezeichner, idQuelle)` — der Stamm gibt
> Name und `Tab_Variante.ID_ProjektRef`, die Quelle den Inhalt (Tiefkopie über `ProjektDuplizierenCtrl`, Energieeinstellungen aus der Quelle);
> die alte Signatur ruft die neue mit Quelle = Stamm und ist Zeile für Zeile unverändert. Beleg: Quelle 1007 gegen Stamm 1030 an drei
> sichtbaren Stellen (`Tab_PV` 2/0, `Tab_Energieanlagen` 11/4, `energy_project_settings` 0/2) — die Variante trägt jeweils die Zahl der
> Quelle; die Gegenprobe zeigt für die alte Signatur die Zahlen des Stamms. **Oberfläche:** neuer plattformfreier Dialog
> `EPOS.UI/Dialoge/Projekt/ProjektVarianteDialog.razor` (+ `ProjektVarianteDaten`, Titelregel W11b‑B‑9, keine Datenbank), Hülle
> `EPOS.UI.Daten/Projekt/ProjektVarianteHuelle.cs` (Stamm bestimmen, Zeilen, Zielnamensregel, Anlegen mit Rückruf zum Nachziehen der
> Startseite); `AlsVarianteHuelle` ist ein dünner Adapter (`BlazorDialogForm<ProjektVarianteDialog>`), und — wichtig — der Weg des
> Bildschirmfotos (Auswahlfeld „Projekt:" im Kopfband) lief bis dahin NICHT über diese Hülle, sondern über `StartseiteHuelle.VarianteAnlegenJetzt`
> mit eigener `NamensDialogHuelle`-Abfrage; beide Wege öffnen jetzt dasselbe Fenster. Der generische `NamensDialog` ist unverändert. Vier
> Ressourcenschlüssel de/en (`VAR_DLG_QUELLE_HAKEN`, `VAR_DLG_HINWEIS_QUELLE`, `VAR_DLG_ZIELNAME`, `VAR_MSG_QUELLE_WAEHLEN`); der Hinweistext
> ohne Haken bleibt wortgleich. Tests: Kern +8 (`ProjektpflegeTests`, Tiefkopie/Gegenprobe/Zähler/Umbenennen/Hülle), UI +18 bunit
> (`ProjektVarianteDialogTests`), beide auch unter en-US grün; SQL-Prüfer 0 von 1 343. **Unverändert:** der zweite Anlegeweg im Reiter
> „Berichte & Kosten" und die KI-Aktion `variante_anlegen` (alte Signatur, Stamm = Quelle); `KiAktionenSchreiben.NeuerProjektname` bleibt ein
> eigener Spiegel der Namensregel (offener Kleinpunkt). Wiki: Absatz „Als Variante speichern" in Anwendersprache; iOS nicht betroffen
> (der Weg ist auf iOS nicht belegt), kein iOS-Lauf.
> Gate sept46 auf `8169757`: Kern 2 708, UI 3 930, Engine 425, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 59,
> Referenzlauf 5/5 byte-gleich gegen R7 (kein Rechenweg berührt); en-US-Lauf grün.
>
> **#236 (12.09.2026, Anwenderrückmeldung mit Bildschirmfoto Startseiten-Reiter „Simulation" › Übersicht, Projekt „Stromspeicher Optimierung -
> ein Speicher": „Der Strombedarf wird in der Übersicht (Simulation) nicht korrekt dargestellt … eingelesener Strombedarf") — Ursache belegt und
> behoben (`3b0539f`, Merge `7f85837`, Agent Opus; 20 Dateien, +1 373/−71).** Links nannte die Projektzusammenfassung 2 850,20 MWh/a, rechts
> stand „Strombedarf 0,00 MWh/a", „Deckung 0,0 %", die Marke „kein Stromerzeuger im Projekt" und „Ohne Bedarf lässt sich keine Deckung
> ausweisen", „Ergebnis speichern" gesperrt. **Der Kern rechnet richtig** — Referenzprojekt 1030 (nur Ganglinie, keine Verbraucherprofile)
> liefert 4 790,09 MWh, und die linke Spalte nutzt dieselbe `SimulationStrombedarf.Berechnung`. **Die Übersicht zeichnete ein Nullobjekt:**
> `SimulationErgebnisHuelle.Zusammentragen` stieg bei ungültigem Ergebnis aus, BEVOR `d.Uebersicht` gebaut war, und
> `SimulationErgebnisDaten.Uebersicht` war mit `new UebersichtDaten()` vorbelegt, das der `UebersichtReiter` wie ein Ergebnis zeichnete.
> Ungültig wird ein gerechnetes Ergebnis an drei Stellen der Stromspeicher-Auslegung (Einstellungen speichern, Flotte rechnen, Rückruf aus der
> Ansicht), während `LaufGerechnet` wahr bleibt und der Startreiter die Spalte deshalb montiert — genau der Weg des Anwenders, der aus der
> Auslegung dieses Projekts kam. Headless-Beleg VOR der Änderung (Projekt 1030): ohne Lauf `bedarfStrom 0,00 / uebStrom 0,00`; nach Lauf
> `4 790,09 / 4 790,09`, Deckung 9,0 %; nach `OptimierungEinstellungenSpeichern` `gueltig=false, bedarfStrom 4 790,09, uebStrom 0,00,
> erzeugerDa=false` — das Bildschirmfoto. **Zweite Reproduktion** mit einem Speicher-Projekt (aus 1030 abgeleitet: Kaskadenplätze leer,
> Ganglinie behalten, eine Einheit aus dem Flottenstand von 1046): der Lauf geht durch, danach 4 790,09 — kein Fehler im Rechenweg, reine
> Anzeige. **Fix:** `enum ErgebnisZustand` (NichtGerechnet, Gueltig, Veraltet, Abgebrochen) mit `Zustandsgrund` statt Bool
> (`ErgebnisGueltig` bleibt als Ableitung), `ZustandSetzen`/`Abbruch(grund)` an den acht Frühausstiegen des Laufs, die drei Ungültig-Setzer
> nennen ihren Anlass („Veraltet"), `BedarfSicherstellen` rechnet den Bedarf einmal je Hülle über denselben Weg wie der Lauf, `Uebersicht`
> ist nullbar ohne Vorbelegung; der `UebersichtReiter` zeigt ohne gültiges Ergebnis die zwei Bedarfszahlen und EINE ruhige Leerkarte mit
> Grund (kein Ring, keine Deckung, keine Tabelle, keine Marke), der Startreiter eine leise Zustandszeile, „Ergebnis speichern" nennt den
> Zustand im `title`; neun Ressourcenschlüssel `SIMERG_ZUSTAND_*` de/en, drei Stilregeln. **Nebenbefund mitbehoben:** die Ergebnisseite
> meldete `StandGeaendert` nie beim Aufbau — nach dem Rückweg aus der Auslegung blieb „Ergebnis speichern" auch bei gültigem Ergebnis
> gesperrt, bis etwas anderes den Wirt neu zeichnete (eine Zeile in `OnAfterRenderAsync`). Tests: Kern +4 (`SimulationUebersichtZustandTests`,
> die drei Zustände und das Speicher-Projekt), UI +12 (`UebersichtReiterTests` +5 mit Wache über ein vorbelegtes DTO, `SimulationErgebnisSeiteTests` +3,
> `StartreiterSimulationTests` +4), alle auch unter en-US grün; SQL-Prüfer 0 von 1 343. Doku: Abschnitt 10 „Befund #236" im Konzept
> Simulationsablauf, Hausregel in `EPOS.UI/CLAUDE.md` („kein vorbelegtes DTO als Ergebnis zeichnen"), eine Zeile in der Wiki-Quelle
> Simulation (Revision 562). Kein Rechenweg berührt; iOS nutzt dieselbe Hülle, kein iOS-Lauf.
> Gate sept47 auf `7f85837`: Kern 2 712, UI 3 942, Engine 425, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 59,
> Referenzlauf 5/5 byte-gleich gegen R7; en-US-Lauf grün.
>
> **#238 (12.09.2026, Anwenderrückmeldung mit Bildschirmfoto Berichte & Kosten › Übersicht: „Im Name in der Auswahl Variante ist der
> Variantenname doppelt enthalten … Der Name sollte so wie im Dropdown (Projekt und Variantenauswahl) sein", Zusatz „korrigiere dabei auch
> die Überlappung") — behoben (`5dc2321`, Merge `7c61416`, Agent Sonnet; 4 Dateien, +137/−29).** Das Auswahlfeld „Variante:" baute seinen
> Eintrag als „Bezeichner — Projektname" (`UebersichtSeite.Eintragstext`, Erbe der zwei Tabellenspalten bis W5‑E‑1), und der Projektname einer
> Variante ist seit jeher `<Stamm> - <Bezeichner>` — daher „ein Speicher — Stromspeicher Optimierung - ein Speicher". Seither steht dort der
> Projektname wie im Kopfband-Auswahlfeld „Projekt:" (`StartseiteCtrl.VariantenEintrag.Name`): Stamm „Stromspeicher Optimierung" zuerst,
> Varianten „Stromspeicher Optimierung - ein Speicher"; Rückfall auf den Bezeichner nur bei leerem Projektnamen; Ids, Reihenfolge,
> Markierung, Tabelle, Umbenennen und Löschen unverändert. **Überlappung:** die Regel `.epos-variantenzeile > .epos-feld:first-child`
> deckelte das Auswahlfeld auf 34 rem (die ~550 px des Bildschirmfotos) — der Eintrag wurde abgeschnitten, und die aufgeklappte Liste eines
> `<select>` ist so breit wie ihr längster Eintrag und ragte über das Bezeichnerfeld; die Obergrenze fällt (`flex: 1 1 24rem`), das
> Bezeichnerfeld bekommt `min-width: 10rem`, der Umbruch bleibt das vorhandene `flex-wrap` der Zeile. Tests: UI +3 (Anwenderbeispiel
> Stamm + zwei Varianten, Rückfall, Umbruch-/Höchstbreiten-Wache), `UebersichtSeiteTests` 37/37 auch unter en-US. Wiki: Punkt „Variante" auf
> der Seite „Varianten" neu gefasst (Revision 563). Nebenbefund des Agenten: ein einmaliger Ausreißer in `ModulImportDialogTests` im
> parallelen Vollauf, isoliert und im sauberen Vollauf grün — beobachten (Muster W16b‑O‑2).
> Gate sept48 auf `7c61416`: Kern 2 712, UI 3 945, Engine 425, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 343, ChartProben 59,
> Referenzlauf 5/5 byte-gleich gegen R7; en-US-Lauf grün.
>
> **#239 (12.09.2026, Anwenderrückmeldung mit Bildschirmfoto Stromspeicher-Auslegung › „1 Speicher": „Stromspeicher hinzufügen geht nicht
> für Speicher aus der Datenbank - nur für duplizierung des vorhandenen … Bei mehreren angelegten Stromspeichern wird nur einer angezeigt,
> nach löschen steht er nicht mehr zur Auswahl") — Ursache belegt und behoben (`0dd09acd`, Merge `eba098cb`, Agent Opus; 16 Dateien,
> +1 945/−4).** Drei Befunde mit einer Wurzel: „+ Speicher hinzufügen" legte nur eine generische Einheit an (100 kWh, 50/50 kW), es gab
> keinen Weg zum Speicherkatalog und keinen zu den Speicheranlagen des Projekts; die Vorbelegung je `SP_TYP`-Anlage
> (`SpeicherFlottenStudieCtrl.Vorbelegung`, #210) greift nur, solange kein Stand `@Aktuell` gespeichert ist — den schreibt die Ansicht an
> vier Stellen (Einstellungen speichern, Flotte rechnen, Rückruf) —, danach war die Einheitenliste eingefroren: eine später angelegte
> Speicheranlage erschien nie, eine entfernte kam nie zurück. **Fix im Kern an EINER Stelle:** `EinheitAusProjektanlage(projekt, anlage)`
> geht DENSELBEN Weg wie die Vorbelegung (`LeseParameter` + `Einheit`), `Projektanlagenkandidaten(projekt)` baut jeden Kandidaten aus genau
> dieser Einheit, `EinheitAusKatalog(id)` bildet den Katalogsatz ab (Name, Energie → Kapazität, Leistung → Lade- und Entladeleistung,
> `eta_ch = eta_dis = sqrt(eta_RT)` wörtlich wie `SpeicherParameter` im Projektlauf, Rückfall 0,90; Modulkosten, Leistungskosten,
> Investition fix, Standby W → kW; NICHT abgebildet, weil Annahme statt Zuordnung: Verschleißkosten je Nennkapazität und Zyklus,
> zugesicherte Zyklen ohne Entladetiefe, Degradation). **Editor:** der Knopf öffnet eine Überlagerung mit drei Quellen — Speicheranlage des
> Projekts (vertretene Zeilen gesperrt), Speicherkatalog (`Katalogliste` mit demselben Profil wie der Projekt-Stromspeicherdialog,
> Doppelklick übernimmt), leere Einheit; darüber die Nachzugszeile „n Speicheranlagen des Projekts sind nicht in der Flotte: …" mit
> „Aufnehmen" — sie erscheint auch nach dem Entfernen wieder. Ohne die fünf neuen Dienste (`StromspeicherAuslegungDienste`, Hülle
> `DiensteSatz`) legt der Knopf wie bisher sofort eine leere Einheit an. Gespeicherter Stand und Vorbelegung bleiben unangetastet
> (SP‑O‑8; Kern-Fall `Ein_gespeicherter_Stand_bleibt_unveraendert`). 19 Ressourcenschlüssel de/en; Tests Kern +10
> (`SpeicherFlottenQuellenTests`), UI +11 (`Dialoge/SpeicherFlottenQuellenTests`), beide auch unter en-US grün; SQL-Prüfer 0 von 1 344.
> Doku: Konzept Stromspeicher-Dialoge Abschnitt 1.8 mit der Abbildungstabelle Katalog → Einheit und Stufenplanzeile P9, ein Satz in
> `EPOS.UI/CLAUDE.md`, Wiki-Bedienungsseite Stromspeicher Schritt 1 neu (Revision 564; der Satz „als Vorlage dient ein Satz aus dem
> Speicherkatalog" beschrieb bis dahin etwas, das es nicht gab). Kein Rechenweg berührt, kein iOS-Lauf.
> Gate sept49 auf `eba098cb`: Kern 2 722, UI 3 956, Engine 425, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 344, ChartProben 59,
> Referenzlauf 5/5 byte-gleich gegen R7; en-US-Lauf grün.
>
> **#240 (12.09.2026, Anwender: „Führe aus: Kleinpunkte ohne Auftrag") — sieben Kleinpunkte aus #234, #235, #237, #238 und zwei ältere
> Restpunkte in EINEM Commit (`bf32eb61`, Merge `4f30192a`, Agent Opus; 28 Dateien, +914/−196), kein Rechenweg berührt.** (1) Die KI-Aktion
> `variante_anlegen` nimmt ein optionales `quellprojekt` (Überladung `AnlegenAusStamm` mit Quelle aus #237), holt den Zielnamen aus
> `VariantenCtrl.Zielname` statt aus dem gelöschten Spiegel `NeuerProjektname` und lehnt Quelle = Stamm ab; vier Schlüssel de/en, Kern +4
> (`KiRegisterS3Tests`). (2) Berichte & Kosten › Übersicht öffnet denselben Variantendialog wie das Kopfband (`VarianteAnlegenOeffnen`,
> `AlsVarianteHuelle.Zeige` über `Blazorsprung` mit dem Stamm der Seite, Liste lädt nach Erfolg neu; das Bezeichnerfeld gehört nur noch dem
> Umbenennen), UI +3. (3) Die Hausregel `.epos-raster th, td { padding: 4px 8px }` verlor gegen QuickGrids `.quickgrid[theme=default] >
> tbody > tr > td` (0,2,3 gegen 0,1,1) — jetzt `table.epos-raster.quickgrid > tbody > tr > td` ohne `!important`; Rasterprobe vorher →
> nachher: virtualisierte Fälle 53,0 → 53,0 px (`ItemSize` 53 bleibt, die natürliche Zeilenhöhe ist jetzt genau dieses Maß), Fall ohne
> gesetztes Maß 48,2 → 53,0 px, Gegenprobe weiter rot, 9/9; nicht virtualisierte Raster werden 4,8 px je Zeile höher (gewollt). (4) Der
> `ErzeugerStapel` beginnt seine Zeichenfläche unter der GEMESSENEN Legendenhöhe — bei neun Reihen lag die mehrzeilige Legende auf dem
> y-Titel (offen aus #234); ein Bild mit einer Legendenzeile bleibt byte-gleich; ChartProben 59 → **61 Bilder, 13 → 14 Gegenproben** (die
> Gegenprobe misst den Versatz an den Bildpunkten). (5) `Bilder.UebersichtKuchen`/`BildKuchen()` hatte seit #222 keinen Anforderer mehr —
> Pfad, Bildschlüssel und Testzeile entfernt, `ChartRenderer.Kuchen` bleibt für den Variantenbericht. (6) Die zwei `Task.Run` des
> `ProjektTransferDialog` laufen über `SpeicherEngine.Kulturweitergabe`; der Wächter `ParallelitaetWacheTests` liest jetzt auch `EPOS.UI`
> und `*.razor`, Trefferliste leer. (7) `ModulImportDialogTests.Der_Herstellerfilter_…`: drei Sofort-Asserts hinter dem Neubau des Rasters
> (`@key`) standen ohne Warten — jetzt `WaitForAssertion`; Beleg 20 Läufe der Klasse hintereinander grün. Tests Kern +4, UI +2 (Saldo aus
> Umbau und Wegfall), KiKern 488; SQL-Prüfer 0; `WP-Plan.sln` x64 baut. Doku: Werkzeugzeile ChartProben in der Wurzel-`CLAUDE.md`,
> `EPOS.UI/CLAUDE.md` (Raster-Spezifität), `Proben/Rasterprobe/LIESMICH.md`, offener Punkt `BildKuchen()` im Block #222 geschlossen.
> Gate sept50 auf `4f30192a`: Kern 2 726, UI 3 958, Engine 425, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 344, ChartProben 61,
> Referenzlauf 5/5 byte-gleich gegen R7; en-US-Lauf grün.
>
> **#241 (12.09.2026, Anwender: „Verschiebe alle .MD Dateien in ein Verzeichnis und strukturiere nach aktuell und überholt. Sie sollen
> nach wie vor für Claude genutzt werden") — umgesetzt (`c71a1dae`, Merge `a598b564`, Agent Opus; 310 Dateien, +970/−4 310).** Die 303
> versionierten Markdown-Dateien liegen seither unter `Dokumentation/`: **49** in `aktuell/` (gültige Arbeitsgrundlagen, flach;
> `Wirtschaftlichkeit_Kosten/` als mitgezogener Ordner), **43** in `ueberholt/` (abgeschlossen, ersetzt, nur noch Geschichte) und **172**
> Protokolle in `ueberholt/Protokolle/` mit ihrer bisherigen Unterstruktur (Reporting 82, Simulation 61, sql 10, Hilfe 6, KI 5, Views 4,
> Update 2, Bericht 1, EPOS.Kern_Import 1); **8** byte-gleiche Dubletten entfernt (Wurzelkopie des konsolidierten Wirtschaftlichkeits-
> Konzepts, `vdi3805_importer.md` zweimal, fünf Kopien unter `Allgemein/Waermespeicher/`); **31** bleiben am Ort — die fünf `CLAUDE.md`
> (Claude Code lädt sie dort; das ist die Bedingung „nach wie vor für Claude"), `README.md` (jetzt drei Sätze statt „Test"), acht
> Werkzeug- und Daten-LIESMICH, das Beispiele-Gerüst (Anwender 12.09.: „ist wichtig"), das Referenzpaket `Projekte/Speichersimulation/`.
> Index `Dokumentation/LIESMICH.md` mit Regel (aktuell = Arbeitsgrundlage, ueberholt = nur Geschichte, nie Regelquelle) und Pflegeregel
> (neues Konzept nach `aktuell/`, Ersetztes per `git mv` nach `ueberholt/` mit Indexzeile; die Statusblöcke leben hier weiter, die
> Wiki-Vermerke in `aktuell/Konzept_Hilfesystem_Wikidokumentation.md`); Zweifelsfälle mit Grund im Bericht entschieden (u. a.
> `Entscheidungsregister_iOS`, `Konzept_Stromspeicher` und `Konzept_Projektbeispiele_Dokumentation` aktuell; `Konzept_Hilfesystem_Infobutton`,
> `Projekt-ExportImport`, `Konzept_Projektdialoge_Vereinheitlichung`, die drei Access-Papiere ueberholt). **311 Links in 104 Dateien**
> nachgezogen, dazu die fünf CLAUDE.md, README, drei Workflow-Kommentare, `build-beispiele.ps1`, die Stummel des Referenzpakets und elf
> pfadtragende Kommentare; tote Altlinks 21 → 12 (die zwölf zeigen auf entfernte Referenzbasen und mit iU9‑W13/W14a gelöschte Masken und
> stehen als benannte Ausnahmen in der Wache). Wurzel-`CLAUDE.md`: Abschnitt „Dokumentation" statt „Grundlagen- und Konzeptdokumente",
> „Compact instructions" auf die neuen Pfade. **Wache** `DokumentationLinkWacheTests` (7 Fälle): jeder relative Link löst auf, der Index
> nennt jede Datei, in der Wurzel liegen nur `CLAUDE.md` und `README.md`, keine `*_Protokoll.md` mehr unter `WindowsFormsApplication1/Allgemein/`.
> Kein Rechenweg, keine Ressource, kein SQL berührt. Orchestrierung: die Statusblock-Skripte schreiben seither in
> `Dokumentation/aktuell/Umsetzungskonzept_iOS_EPOS-Plan.md`.
> Gate sept51 auf `a598b564`: Kern 2 733, UI 3 958, Engine 425, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 344, ChartProben 61,
> Referenzlauf 5/5 byte-gleich gegen R7; en-US-Lauf grün.
>
> **#242 (12.09.2026, Anwender: „Räume alte nicht mehr genutzte Läufe/Verzeichnisse auf", „Lösche .work", „EPOS-Plan_Beispiele_Geruest
> ist wichtig", Fernzweige „vorerst nicht") — umgesetzt (`f6464b88`, Merge `d2e15bb1`, Agent Sonnet; 42 Dateien, +430/−3 286).** Entfernt,
> jede Stelle mit `git grep` belegt (31 Dateien, 70,2 MB): `.work/` — Einmal-Prüfprogramm `RealFleetHarness`, Prüfbericht und die
> 70-MB-Kopie der Produktivdatenbank vom 11.09. (30 Projekte mit Kundennamen); das ist die **Rücknahme von SP‑O‑9** („.work behalten",
> 11.09.2026) durch den Anwender, `.work/` steht seither in der `.gitignore`, die zwei Verweise (Fehleranalyse des Referenzpakets,
> Mehrspeicher-Doku) tragen „nicht mehr im Repository; Stand 4c3b5210 in der Git-Geschichte" (seit AUF‑Q1 am 12.09.2026 auch dort nicht
> mehr; mit #244 umformuliert) —; `DB-Backup/` (16 Git-LFS-Zeiger auf
> Access-Sicherungen, per `.gitignore` seit dem 02.09. ausgeschlossen, Objekte lagen nie auf dem Server); vier `.bak`-Kopien unter
> `WindowsFormsApplication1`; `sqlite-probe/` (Spike vor der SQLite-Umstellung, drei Links in Index und `BETRIEB_SQLITE` umformuliert);
> vier `Lizenzserver/*.original-2026-08-19`; `Reporting_Geruest.zip`. Verschoben: der Hydraulik-Entwurf nach `Mockups/`, die README des
> nicht mehr versionierten Scrapers nach `Dokumentation/ueberholt/`. **Dauerhaft:** Wache `RepositoryOrdnungWacheTests` (7 Fälle: keine
> `*.bak`/`*.orig`/`*.original-*`/`*.accdb`/`*.laccdb`, kein `.work/`, kein `DB-Backup/`, `*.sqlite` nur die Testdatenbank), das Konzept
> `Dokumentation/aktuell/Konzept_Repository_Aufraeumen_EPOS-Plan.md` (Regel, Inventar je Verzeichnis, Stufen 0–3, Entscheide SP‑O‑9
> zurückgenommen / AUF‑E‑1 Beispiele-Gerüst bleibt / AUF‑E‑2 Fernzweige vorerst nicht / AUF‑Q2 LFS und AUF‑Q3 Quellen entschieden
> → #243 / AUF‑Q4 gegenstandslos, `retention-days: 14` stand schon / AUF‑Q1 Geschichte umschreiben offen) und der Abschnitt „Aufräumen"
> in der Wurzel-`CLAUDE.md`. Das Sitzungs-Scratchpad der Orchestrierung war zuvor von 3,4 GB auf 220 MB bereinigt worden. Die
> Datenbankkopie bleibt in der Git-Geschichte, bis der Anwender AUF‑Q1 entscheidet. Kern-Tests 2 733 → 2 740; kein Rechenweg berührt.
> **Gate sept52 auf `d2e15bb1`: ROT — allein durch die neue Wache selbst.** `Sqlite_Dateien_nur_auf_der_Weissliste` lief das Dateisystem
> und traf die ignorierte `Referenzlaeufe/Arbeitskopie/Kenndaten.sqlite`, die der Referenzlauf des Gates anlegt, und die
> `.claude/worktrees/…` laufender Agenten; alles Übrige war grün (Kern 2 739 von 2 740, UI 3 958, en-US dieselbe eine Stelle, 5/5
> byte-gleich). Fix `69bc9777` in #243 (Wache über `git ls-files -z`) — deshalb wurde #242 nicht einzeln gepusht, sondern mit #243.
> Erster grüner Nachweis: Gate sept53 auf `f41e5307` (#242 und #243 zusammen): Kern 2743, UI 3958, Engine 425, KiKern 488, 5
> eindeutige Warnungen, SQL 0 von 1 344, ChartProben 61, Referenzlauf 5/5 byte-gleich gegen R7; en-US-Lauf grün.
>
> **#243 (12.09.2026, Anwenderentscheide AUF‑Q2 „Nehme VDI-Archive und Testdatenbanken in git-lfs" und AUF‑Q3 „Setze Empfehlung um")
> — umgesetzt (`69bc9777`, `5743f218`, `42121e08`, Merge `f41e5307`, Agent Opus; 95 Dateien, +795/−744 665 — die Zeilen der VDI-Textkataloge
> weichen ihren Drei-Zeilen-Zeigern).** **Git LFS ab jetzt, ohne Geschichtsumschreibung:** die `.gitattributes` führt vier Regeln
> (`Referenzlaeufe/Kenndaten_Test.sqlite`, `VDI-3805-Daten/**/*.zip|*.vdi|*.VDI`), die vier Access-Zeilen sind gefallen;
> `git lfs ls-files` = **69** (eine Testdatenbank, 68 Archive; 172 045 379 Byte = 164,1 MiB), die Arbeitsdateien byte-gleich (Testdatenbank
> sha256 `449a8652…6626a9d7` vorher = nachher), die alten Blobs bleiben in der Geschichte (AUF‑Q1 offen). Bewusst NICHT in LFS: CSV (auch
> `bslib_database.csv`, byteweise gegen ihre Importprobe geprüft), PAN, OND, PDF, XLSX, die Referenzbasis R7 und
> `Referenzlaeufe/Importproben/**` — klein oder Gegenstand von Byte-Vergleichen, und jeder LFS-Abruf kostet Bandbreite. **Workflows**
> (Checkout überall ohne `lfs: true`): `kern.yml` und `windows.yml/build-test` mit „LFS-Zwischenlager" (`actions/cache@v4` auf `.git/lfs`,
> Schlüssel aus dem Zeiger) und „Testdatenbank aus LFS" (gezielter Pull; Prüfung: größer als 1 MB und beginnt nicht mit
> `version https://git-lfs`); `windows.yml/installer` zieht ungefiltert und prüft die Testdatenbank UND jede Datei unter
> `VDI-3805-Daten`, weil das Setup sie einpackt; `ios.yml` zieht die Seed-Datenbank gezielt vor dem Bau — **nur geändert, nicht ausgelöst**
> (Regel vom 09.09.2026), der nächste freigegebene iOS-Lauf ist der Nachweis. **Zeiger-Schutz** an den drei Öffnungsstellen:
> `EPOS.Kern.Tests/TestDatenbank.cs` (`LfsZeigerProbe`, benannter Abbruch), `Referenzlauf/DbUmgebung.cs` (verlinkt, gilt für
> `Referenzlauf` UND `EPOS.Referenzlauf`; Protokollzeile, Rückgabe 2), `Werkzeuge/Auslieferungsvorlage/Argumente.cs` (Rückgabe 2).
> **Ordnungs-Wache** (`69bc9777`, der Fix zum roten Gate sept52): Dateiliste aus `git ls-files -z` (UTF-8 — ein VDI-Pfad trägt ein „ä",
> ohne `-z` fiele er aus jeder Zählung) statt Dateisystem, der Rückfall nimmt `.claude`, `TestResults` und
> `Referenzlaeufe/Arbeitskopie` aus; 7 → **10 Fälle** (Gegenprobe des synthetischen Baums, vier LFS-Regeln, Testdatenbank kein Zeiger);
> belegt 10/10 mit angelegter Arbeitskopie und 10/10 mit `PATH` ohne git. **Fremdquellen (AUF‑Q3):** `git mv BHKWPlan → Quellen/BHKWPlan`,
> `PV-Konzept_PV-Now → Quellen/PV-Now`, `VALERI → Quellen/VALERI` (15 Dateien; Verweise in drei Konzepten und im Aufräumkonzept; reine
> Dateinamen und fremde Ablageorte wie `Z:\…\BHKWPlan` unangetastet). Doku: `Referenzlaeufe/LIESMICH.md` Abschnitt „Git LFS",
> Wurzel-`CLAUDE.md` (Absatz im Abschnitt „Aufräumen"), Setup-Konzept (Installer-Job zieht ungefiltert), Aufräumkonzept Stufe 3
> „umgesetzt", AUF‑Q2/Q3 entschieden, AUF‑Q4 gegenstandslos. **Für den Anwender:** einmal `git lfs install` auf dem Windows-Rechner vor
> dem nächsten Pull — sonst kommen Zeigerdateien an, und Wache, Tests und Werkzeuge sagen es mit Namen; GitHub gibt frei 1 GB
> LFS-Speicher und 1 GB Bandbreite je Monat, jede neue Fassung der Testdatenbank kostet 68 MB Speicher. Orchestrierung: der Push lädt
> die 68 Objekte (165 MB) über den pre-push-Haken hoch (`git lfs push --dry-run` vorher: 68 Objekte, Batch-Endpunkt antwortet 200);
> scheitert der Upload, bricht der Push ab. Kern-Tests 2 740 → 2 743; kein Rechenweg, keine Ressource, kein SQL berührt.
> Gate sept53 auf `f41e5307`: Kern 2743, UI 3958, Engine 425, KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 344, ChartProben 61,
> Referenzlauf 5/5 byte-gleich gegen R7; en-US-Lauf grün.
>
> **#244 (12.09.2026, Anwenderentscheid AUF‑Q1 „ausführen", Umfang „Variante 2: mit Access-Altbeständen") — umgesetzt; die Git-Geschichte
> ist umgeschrieben.** Vorbereitung (Agent Opus, Merge alt `bdff8c15`, neu `ddc42aca`): die Protokolle der 24 entfernten Referenzbasen
> liegen byte-gleich unter `Dokumentation/ueberholt/Referenzbasen/` (25 Dateien, 604 KB), jede Aussage „liegt in der Git-Geschichte
> (Stand 1e71d30)" in CLAUDE.md, `Referenzlaeufe/LIESMICH.md` und den Konzepten ist berichtigt, das Aufräumkonzept führt Stufe 4; Gate sept54
> darauf grün (Kern 2 743, UI 3 958, 5/5 byte-gleich, en-US grün), CI Kern 389 / Windows 334 grün. **Messung und Probe** (Agent Opus,
> Spiegelklon, `git filter-repo` 2.47): versioniert waren jemals **38** Basisordner (24 aus SYNC‑Q1 plus 14 ältere), 156 MiB eindeutige
> Blobs; `.work/` 70 MiB; 13 Fassungen der Testdatenbank (804 MiB roh, 12 ohne Zeiger); 67 VDI-Archive ohne Zeiger (96 MiB); dazu außerhalb
> des Auftrags die Access-Altbestände der Frühgeschichte (42 Fassungen `Kenndaten.accdb`, 13 `.bak`, `AccessDatabaseEngine.exe`; 1 159 MiB
> roh, 53 MiB im Pack) — der Anwender entschied für die erweiterte Variante. **Umschreiben:** frischer Spiegel, ein filter-repo-Lauf
> (41 Verzeichnisse und 41 Access-Pfade per `--invert-paths`, 79 historische Blobs von Testdatenbank und VDI-Archiven per
> `--strip-blobs-with-ids`; die 68 aktuellen LFS-Objekte bleiben, 11 ältere Testdatenbank-Fassungen sind endgültig weg), Verifikation
> vollständig PASS: Baum des Arbeitszweigs vor = nach, jeder andere Zweig und der Tag nur Löschungen in den entfernten Pfaden, Autor/Datum/
> Committer aller 2 145 Commits unverändert, Nachrichten nur um ersetzte Kennungen, fsck sauber, 69 LFS-Zeiger; 18 Commits wurden leer und
> fielen (Testdatenbank-Nachzüge, ein Basen-Commit, 15 „add"-Commits der Access-Zeit). **Pack 554 → 358 MiB (−35 %)**; die 61 verwaisten
> LFS-Zeiger auf Access-Objekte (4,9 GB referenziert) sind aus der Geschichte. **Push:** ein Gesamt-Push mit Tag wurde von der Umgebung mit
> HTTP 403 abgewiesen — sie lässt Force-Pushes auf Zweige zu, aber keine Tag-Updates und keine Ref-Löschungen; deshalb alle **13 Zweige**
> in drei Pushes mit `--force-with-lease` je Ref (Lease gegen den Stand vor dem Umschreiben, kein Sync dazwischen), Tag `vor-W16` bleibt
> beim Anwender (neu `83c9508c`), ebenso das Löschen der zwei Probezweige `tmp-umschreibung-probe` und `tmp-probe-leer`. **Folgen:** alle
> Commit-Kennungen ändern sich (Karte alt → neu mit 2149 Zeilen unter `Dokumentation/ueberholt/Geschichte/commit-map_2026-09-12.txt`;
> jede Kennung in Statusblöcken vor diesem Block ist eine alte), die 1 355 Signaturen der alten Commits entfallen, jeder Rechner klont neu,
> aus einem alten Klon wird nie wieder gepusht. GitHub gibt den Speicher erst nach eigener Bereinigung frei; `refs/pull/1/*` halten die alte
> Geschichte bis dahin. Kein Rechenweg, keine Ressource, kein SQL berührt; Gate nicht nötig (Baum identisch).

## Statusblock iU9 — Welle 11a umgesetzt (04.09.2026, Basis 427fd59 nach W10a, zusammengeführt mit a398c9a nach W10b)

> **Statusblock iU9 — Welle 11a umgesetzt (04.09.2026, Basis `427fd59` nach W10a, zusammengeführt mit `a398c9a` nach W10b)**
>
> Die Welle 11 des Wellenplans (Simulationsergebnis: `Form_Simulation_Detail` mit elf Reitern,
> Dashboard, drei Navigatoren, Variantenvergleich — 11 031 Zeilen, 17 Zeichenflächen) läuft in zwei
> Läufen. **W11a** verlegt alles, was ohne Oberfläche geht, in den Kern und hängt die WinForms-Masken
> schon daran — **ohne eine Maske zu löschen**; W11b baut danach die Ergebnisseite in einem Schritt.
> Vermessung `iU9_W11_Vermessung.md` (1 734 Zeilen, 50 Befunde), Arbeitsanweisung
> `iU9_W11a_Arbeitsanweisung.md`. Acht Sachcommits und zwei Merges:
>
> | Commit | Inhalt |
> |---|---|
> | `d0b64b9` `fd1e750` | **W11a.1/2** `ErgebnisPraesenz` (public) und `Ganglinie` (Dauerlinie) im Kern; **elf** inline-SQL-Stellen der Welle als Controller-Methoden (`KonfigurationCtrl.LiesProjekt`, `HeizkesselStammCtrl.BrennstoffartenJeProjekt`, `WErzeugerCtrl.AnlagenJeTyp`, `StromspeicherStammCtrl.KapazitaetJeProjekt` …) |
> | `a2665db` `5ebecdf` | **W11a.3/5** `SimulationErgebnisCtrl` — die Reiterzahlen als **sieben DTOs**, die vier Eigenanteil-Rechnungen mit dem `SimulationRunner` geteilt (**eine Wahrheit**), `SpeicherKennzahlenBlock` (39 Zeilen); `SpeicherAnzeigeCtrl` (vier Kopien der Anzeigetexte → eine), CO₂-Faktoren in `EmissionsVorgaben`, Speicherkapazität über Controller |
> | `88fceb5` | **W11a.4** `SimulationLaufCtrl` (Vorprüfen, Bedarf, Bestücken, Laufen, Abbruchgrund, Speichern); `SimulationControl.Do_Simulation` mit `IProgress<LaufFortschritt>` (fünf Phasen) und `CancellationToken` — **die Detailansicht rechnet nebenläufig**, mit Balken und Abbrechen, statt das Fenster einzufrieren (W11‑B48); kein Lesevorgang musste vorgezogen werden (R‑W10a‑2 gilt) |
> | `52f76ae` `35a8d76` | **W11a.6/7** sieben Ergebnisbilder im `ChartRenderer` — `GanglinieNormiert`, `ErzeugerStapel` (mit zweiter Achse; trägt sechs der siebzehn Flächen), `Streuwolke`, `Ring`, `MonatsStapel`, `Temperaturverlauf` — **16 → 30 Proben**; Baustein **`Fortschritt`** |
> | `b8dfd01` `9f00c91` `c3c75c5` `8c9ecbe` | Merge W6–W10a-Nachweise; Protokoll und drei CLAUDE.md; Merge W10b (sieben Konflikte, u. a. beide Wellen hatten `LiesProjekt` — eine Fassung); Merge auf `ios_migration` |
>
> **Der Ertrag ist der Zahlenabzug.** 95 Kennzahlen je Projekt vor und nach dem Umbau verglichen:
> **92 unverändert**, drei geändert und begründet — die Restwärme rechnet jetzt wie der
> `SimulationRunner` (BHKW mitgezählt: Projekt 1030 Gesamtwärme 5 403 → 6 139 MWh, Restwärme
> 734 → −1,76 MWh; W11‑B35), der PV-Deckungsgrad ohne Strombedarf ist 0 statt `NaN` (B22).
> **Entscheid für den Anwender (W11a‑O‑1):** Restwärme auf ≥ 0 klemmen? Dazu W11a‑O‑2 (CO₂-Faktoren
> 0,42/0,20 wörtlich, `EmissionsVorgaben` hatte kein Gegenstück), O‑3 (Zusammenführung der vier
> Berichtsbilder mit den neuen), O‑5 (`KonfigurationCtrl` liest zwei Modelle — Netzverluste faktisch 0 %,
> Referenzstand wörtlich). Nebenbefund behoben: `TestDatenbank` kopierte 77 MB je Testfall.
>
> **Nachweise** (auf dem gemergten Stand `8c9ecbe`, Linux): Build → 0 Fehler, **12** Warnungen ·
> `dotnet test WP-Plan.Kern.slnf` → **2 502** grün (2 379 nach W10b), **identisch unter
> `LC_ALL=en_US.UTF-8`** · Formularkarte **123** grün · Stapellauf **49** Masken (unverändert, keine
> Maske gelöscht) · SQL-Prüfer 1 233 Texte, 0 Fundstellen · **ChartProben 30 Bilder**, 0 Verstöße ·
> Referenzlauf 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte) — nach jedem Teilschritt geprüft.
>
> **Protokoll**: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W11a_Kern_Protokoll.md` (Zahlenabzug
> je DTO, Fadenprüfung, 30 Proben, § 11 Merge-Nachtrag). **Windows-Abnahme steht aus** (acht Punkte):
> nebenläufiger Start mit Balken, Abbrechen, Reiterlage nach dem Automatikstart, die drei geänderten
> Zahlen, 39 Kennzahlzeilen mit Ampelfarben, Variantenvergleich/Optimierung, Autarkie-Kachel, beide Sprachen.

## Statusblock iU9 — Welle 10b umgesetzt (04.09.2026, Basis 427fd59 nach W10a, zusammengeführt mit cd849f8)

> **Statusblock iU9 — Welle 10b umgesetzt (04.09.2026, Basis `427fd59` nach W10a, zusammengeführt mit `cd849f8`)**
>
> Der zweite Lauf der Welle 10: **`Form_Simulation_Config` mit ihren vier Teildateien
> (4 558 Zeilen), den drei Steuerelement-Klassen `ErzeugerKarte`/`SpeicherKarte`/`SchemaAnsicht`
> (2 121 Zeilen) und dem Zeichenmodell → eine Razor-Seite `SimulationKonfigSeite`** mit den
> Bausteinen **`Schema`** (das Hydraulikbild als SVG), **`ErzeugerKachel`** und **`SpeicherKachel`**;
> `SchemaModell` unverändert in den Kern, dazu `SchemaLayout` (die Anordnung headless prüfbar) und
> `Kaskade` (Platzlogik der `Tool_1…6`). Die sieben W10a-Dialoge hängen als Überlagerungen an der
> Seite; ihre Hüllen verlieren den Fensterweg. **Hosting-Entscheid R‑W10b‑1:** die Komponente ist
> eine Seite (`SeitenZustand`, Eintrag für `AppWurzel`), auf Windows bis W16 in der modalen
> Dialoghülle, weil beide Aufrufer (Startbild, Detailansicht) die modale Rückkehr brauchen.
> **Eingelöst mit W16b (E‑5, 04.09.2026):** die Konfiguration löst die Razor-Startseite in derselben WebView ab, die
> modale Hülle ist gefallen; die zwei Bedarfsobjekte gehören dem Projekt (`BedarfsZustand`), nicht mehr einem Fenster.
> Arbeitsanweisung `iU9_W10b_Arbeitsanweisung.md`. Acht Sachcommits und ein Merge:
>
> | Commit | Inhalt |
> |---|---|
> | `2e75393` `93bd88f` | **W10b.0a/b** `SchemaModell` in den Kern, `SchemaLayout` neu; fünf inline-SQL in vier Controller, `Kaskade` und Quellenwahl im Kern, `KonfigurationCtrl.LiesProjekt` |
> | `cac6eb4` `4caee3f` | **W10b.0c/d** Baustein `Schema` (Knoten, Bézier-Kanten, Kaskadenband, Legende, Klick/Doppelklick, Tastatur), Bausteine `ErzeugerKachel`/`SpeicherKachel` (Chips mit sechs Stilen und sechs Zielen, Schwellenband als Inline-SVG) |
> | `dd132ff` | **W10b.1** die Seite: zwei Kartenspalten, Umschalter Liste/Schema mit erhaltener Auswahl, zwei eigene Überlagerungsebenen (Betriebsmodus, WP-Priorität, Quellenwahl, Wärmesenke, Pufferverwaltung; Quelle Pufferspeicher, Quellprofil, Erdreich), Fußzeile mit Sofortschaltern — vier Teildateien und drei Controls gelöscht |
> | `d75908c` `6bea64e` `a91ba2a` | **W10b.2–4** Befund W10b‑B42 (`DatenzugriffTests` ohne Sammlungsmarke riss DB-Tests mit), Formularkarte, Protokoll, vier CLAUDE.md, iOS-Einstieg (`IProjektQuelle.SimulationKonfigGaben` mit Standardumsetzung) |
>
> **Der Ertrag ist eine WebView für die ganze Konfiguration.** Drei Navigationen und ~9 000 Zeilen
> WinForms sind eine Seite mit **einem** `Neuladen()` (statt neun Auffrischungsstellen, W10‑B40);
> die Kette Seite → Quelle/Senke → Pufferverwaltung → Klimazonenkarte läuft in **einem** Fenster,
> Esc schließt je Ebene; `SeitenZustand` wird genau einmal gebraucht. Alle Befunde W10‑B33…B40
> erledigt, dazu W10b‑B41 (`listErzeuger` ohne Leser) und B42. Ein Entscheid für den Anwender:
> soll „Speichern“ erst nach einer Änderung aktiv werden (W10b‑O‑3)? Das Schema ist ohne
> Bildvergleich portiert (W10b‑O‑1) — Sichtabnahme gegen ein Foto des Bestands.
>
> **Nachweise** (auf dem gemergten Stand `a91ba2a`, Linux): Build → 0 Fehler, **12** Warnungen
> (17 nach W10a; fünf WFO1000 der gelöschten Karten) · `dotnet test WP-Plan.Kern.slnf` → **2 379**
> grün (2 284 nach W10a), **identisch unter `LC_ALL=en_US.UTF-8`** · Formularkarte **123** grün ·
> Stapellauf **49** Masken (50 − 1), 48 erreichbar, 0 × „nein“ · SQL-Prüfer 1 239 Texte,
> 0 Fundstellen · ChartProben 16 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS,
> byte-gleich** (815 043 Werte) · `dotnet publish` mit `wwwroot` samt neuer CSS.
>
> **Protokoll** mit 13 Abweichungen (A‑1…A‑13), 16 Windows-Abnahmewegen und sieben offenen
> Punkten: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W10b_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: eine WebView für alles, Chipfolge je Anlagentyp, Schema gegen das
> Foto des Bestands, synchrone Auswahl in beiden Ansichten, drei Überlagerungsebenen mit Esc je
> Ebene, Rücksprung aus `Form_Simulation_Detail`, de/en. Der elfte iOS-Lauf (33826084944) auf
> diesem Stand ist grün.
>
> **Formularraster, Paket P3 (iU8‑E‑2, 05.09.2026, `d3fccf1`):** `QuelleErdreichDialog` („Standort" einspaltig, jeder Hinweis unter seinem Feld; Vorschau
> und Auslegungsprüfung bleiben am Diagramm), `QuellprofilDialog` (die 24 Stundenwerte zu zweit je Zeile; Werteseite
> bleibt Tabelle), `WaermesenkeDialog` (Klasse B: beide Blöcke einspaltig, weil jeder Schalter das Feld unter sich
> freigibt; die Senkenliste bleibt Zeilenraster).
>
> **Anwenderbefund W10b‑B‑1 vom 05.09.2026 („Simulation → Simulation-Konfiguration → Ansicht Schema: Die Pfeile sind
> teilweise zu dick und falsch dargestellt"), behoben in `4e39d4a`:** Drei Sachen, drei Ursachen, keine im Rechenweg.
> Die Farbregel des Stilblatts setzte neben dem Strich auch die **Füllung** und schlug dabei das `fill="none"` am
> Element — der offene Bézierbogen wurde als Fläche ausgemalt (die „riesige blaue Fläche mit gezacktem Rand"); und
> `markerUnits="strokeWidth"` machte aus der 9×7-Spitze bei 1,8 px Strich 16×13 px. Die Füllung gehört jetzt allein
> der Pfeilspitze, die Spitze misst feste 10×8 Nutzereinheiten, die Strichstärke steht gedeckelt im Kern (2 px,
> hervorgehoben 3, Grenzen 2–6) als Attribut am Element — eine Breite aus Leistung oder Volumen gab es nie und wird
> bewusst nicht eingeführt. Die Wegführung in `SchemaLayout` ist von Bézierbögen auf **Spaltenbahnen** umgestellt:
> waagerecht aus dem Kasten, senkrecht in der Gasse, waagerecht in den Zielkasten; jede Leitung mit eigener Senkrechte
> (`GassenBelegen`), übersprungene Spalten auf einer kastenfreien Bahn gequert (`FreieBahn`), mehrere Ansätze an
> derselben Kastenseite über die Kastenhöhe verteilt (`AnkerVerteilen`) — keine Leitung kreuzt mehr einen Kasten, für
> sechs echte Projekte Strecke für Strecke nachgeprüft. Der Widerspruch „Quelle: Puffer 3000Ltr · Kaskade" gegen
> „Keine Kaskade im Projekt" entstand, weil der Satz am leeren Kaskadenband hing und das Band nur bei einem Lader auf
> Rang 1 entstand — Projekt 1042 lädt seinen Quellpuffer auf Rang 2. `SchemaModell.HatKaskade` trägt die Tatsache
> seither selbst, die Kettensuche geht über alle Ränge, es gilt `HatKaskade ⇔ Band`. Nebenbefund: 1030 „B3-Kaskade"
> trägt trotz Namens keinen Quellpuffer; die echten Kaskadenprojekte der Testdatenbank sind 1042, 1043, 1044.
> Nachweise: `SchemaLayoutTests` 19, `SchemaModellTests` neu 10 gegen die Testdatenbank, `SchemaTests` 16; Kern 1 190
> und UI 2 648 grün in beiden Kulturen, Kern-Wächter leer, kein Referenzlauf nötig (das Schema rechnet nichts). Sechs
> Abnahmepunkte im W10b-Protokoll.

## Statusblock iU9 — Welle 10a umgesetzt (03.09.2026, Basis 04fc474 nach W9, zusammengeführt mit b6a72b0)

> **Statusblock iU9 — Welle 10a umgesetzt (03.09.2026, Basis `04fc474` nach W9, zusammengeführt mit `b6a72b0`)**
>
> Die Welle 10 des Wellenplans (Simulationskonfiguration, 12 361 Zeilen) ist die größte des Pakets
> und läuft deshalb in zwei Läufen. **W10a** portiert die **sieben Dialoge**, die aus
> `Form_Simulation_Config` heraus geöffnet werden: Betriebsmodus, Klimazonenkarte, Quelle Erdreich,
> Pufferverwaltung, Quelle Pufferspeicher, Quellprofil und Wärmesenke → **sieben Razor-Komponenten**
> in `EPOS.UI/Dialoge/Simulation/`, jede WinForms-Fassung gelöscht (Regel M1), 7 803 Zeilen
> Oberflächencode, 30 `MessageBox`. `Form_Simulation_Config` bleibt bis W10b WinForms und ruft die
> Dialoge über Hüllen. Arbeitsanweisung `iU9_W10a_Arbeitsanweisung.md`, Vermessung
> `iU9_W10_Vermessung.md` (1 887 Zeilen, 40 Befunde). Fünfzehn Sachcommits und ein Merge:
>
> | Commit | Inhalt |
> |---|---|
> | `352f349` `cbfccb1` `7aae643` | **W10a.0a–c** `SenkeAnzeige`/`IstPufferZiel` in den Kern (sonst bräche der Bau beim Löschen der Senkenmaske); Kapazitätsformel, Katalog-SQL, Sondenmeter, Ergebniszuordnung und Profilparser im Kern; Sprungziel `PufferSpAdminNurLesen` |
> | `ef513e6` `53240c4` `6fe8656` | **W10a.0d–f** `ChartRenderer.Jahresgang` (zweireihig, Monatsachse) mit Probe; Baustein **`Bildkarte`** (PNG mit SVG-Klickflächen) samt `KlimazonenPfade` (15 Zonen, zur Bauzeit erzeugt) und dem Kartenbild unter `EPOS.UI/wwwroot/bilder/`; `WertAbfrage` |
> | `18ac6e1` `b34a6d3` `033d0b9` | **W10a.1–3** `BetriebsmodusDialog`, `KlimazonenkarteDialog` (Überlagerung), `QuelleErdreichDialog` (Kollektor/Sonde, VDI-4640-Prüfung, **asynchroner** Simulationslauf aus dem Dialog) |
> | `781e463` `a6d15e5` `82aad99` `97ff674` | **W10a.4–7** `PufferSpProjektDialog` (Klassen-Set, Schwellen, Schichtung, Ladereihenfolge — das Blatt aller drei Absprünge), `QuellePufferspeicherDialog`, `QuellprofilDialog` (virtualisiertes 8 760-Zeilen-Raster), `WaermesenkeDialog` (Senkenliste mit Rang, Parallelverbund, Ladeverhalten) |
> | `630a56b` `e69df40` `427fd59` | **W10a.8–11** Ressourcen, Formularkarte, Protokoll, drei CLAUDE.md, Nachweise auf dem gemergten Stand |
>
> **Der Ertrag ist die Klickkarte, die zum ersten Mal funktioniert.** Die WinForms-Klimazonenkarte
> konnte ihre ausgelieferte SVG **nie** lesen (W10a‑B41: der Parser erwartete den Pfadbefehl getrennt
> von der ersten Koordinate, `float.Parse("M315.30")` warf, ein leerer `catch` verschluckte es) — die
> Maske zeigte immer nur ihre Ladefehlerzeile. Die Blazor-Fassung stellt die Zonenwahl per Klick her.
> Zwei Proben haben die Bauweise bestimmt: `SimulationRunner.Simuliere` läuft in `Task.Run` fehlerfrei
> gegen die Testdatenbank (R‑W10a‑2, deshalb rechnet der Erdreichdialog asynchron mit Wartezustand),
> und das Kartenbild misst 1,29 MiB (R‑W10a‑3, nicht verkleinert). 18 Befunde behoben, 8 wörtlich
> übernommen und als Entscheid für den Anwender notiert (W10a‑O‑1…O‑7), dazu ein nicht
> reproduzierter Einzelausfall der Testsuite unter `en_US` (W10a‑O‑8, Frist der `WaitForAssertion`).
>
> **Nachweise** (auf dem gemergten Stand `427fd59`, Linux): Build → 0 Fehler, **17** Warnungen
> (20 nach W9; drei WFO1000 gingen mit den Designern) · `dotnet test WP-Plan.Kern.slnf` → **2 284**
> grün (2 066 nach W9), **identisch unter `LC_ALL=en_US.UTF-8`** · Formularkarte **123** grün ·
> Stapellauf **50** Masken (55 − 5; Quellprofil und Wärmesenke hatten keinen Designer), 49 erreichbar,
> 0 × „nein“ · SQL-Prüfer 1 240 Texte, 0 Fundstellen · **ChartProben 16 Bilder**, 0 Verstöße ·
> Referenzlauf 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte) · `dotnet publish` mit `wwwroot`
> samt Kartenbild.
>
> **Protokoll** mit Feldkartenabgleich (7 Masken), 17 Abweichungen (A‑1…A‑17), 20 Windows-Abnahmewegen
> und acht offenen Punkten: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W10a_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: das Kartenbild in der veröffentlichten `wwwroot`, der asynchrone
> Simulationslauf bei einem großen Projekt, die Pufferverwaltung ohne Abbrechen, der Kesselzweig, der
> die WP-Vorgaben unberührt lässt, die englische Oberfläche mit unübersetzten Steuerwerten.
>
> **Nachzug iU8‑O‑1 (06.09.2026), umgesetzt in `e6bc2fd`:** Der letzte offene Dialog des Anwenderwunsches iU8‑E‑2 steht im
> Raster. Alle drei Parameterblöcke von `PufferSpProjektDialog` (Eigenschaften, Schichtung und Leistungsgrenzen,
> Ladereihenfolge) liegen jetzt im `Formularraster`; sechs `Formulargruppe`n gliedern die 21 Felder — Volumen und
> Verluste, Temperaturen, Schwellen, Leistungsgrenzen und, erst ab zwei Schichten, Schichtmodell und Entnahmehöhen.
> Kein Raster ist einspaltig: Die Felder tragen hier keine Reihenfolge, sie gehören paarweise zusammen
> (Vorlauf/Rücklauf, Ein‑/Abschaltschwelle, Lade‑/Entladeleistung) — das unterscheidet ihn von `WaermesenkeDialog` und
> `QuelleErdreichDialog` aus P3. Die beiden `Zeilenraster` (Bestand, Ladereihenfolge) bleiben draußen; eine Liste ist
> kein Formularblock. Kein Feld verloren, keines umbenannt, keines verschoben (2 Auswahl‑, 1 Text‑,
> 1 Mehrfachauswahl‑, 4 Ganzzahl‑, 13 Zahlenfelder vorher wie nachher); die eine Herleitungszeile weniger ist der Kopf
> der Entnahmehöhen, der jetzt der Titel seiner Gruppe ist. Eine Selektorzeile im Stilblatt, fünf Ressourcenschlüssel
> de/en, `Resource.Designer.cs` mit `Werkzeuge/ResourceDesigner` erzeugt (4 870 → 4 875). `EPOS.UI.Tests`
> 2 679 → 2 683, grün unter de und en. **iU8‑O‑1 ist geschlossen.** Fünf Abnahmepunkte A‑iU8‑O‑1 im W10a-Protokoll.
>
> **iU8‑E‑3 (Anwenderwunsch, Windows-Abnahme V2 07.09.2026: „Zur besseren optischen Abgrenzung sollten Listen einen Rahmen
> haben"), umgesetzt in `ebc512c`, zusammengeführt in `e8f2bb3`:** Der Rahmen steht an denselben zwei Stellen wie die
> Höchsthöhe aus W9‑B‑2 — `.epos-raster-huelle` und `.epos-zeilenraster` tragen `border: 1px solid var(--epos-rahmen)` und
> `border-radius: var(--epos-ecke)`, der stehende Spaltenkopf ist innen mit derselben Linie abgesetzt. Damit rahmen alle
> Listen des Hauses: Raster/QuickGrid (auch `--frei`, `--hoch`), ProjektListe, die handgeschriebenen Projekt/DB-Tabellen,
> die Katalogverwaltungen, das Positionsraster; die Pfeilspalte der Zweispaltenauswahl rahmt nicht mit (Regel und Markup
> geprüft). Farbe bewusst `--epos-rahmen` (#b4b2a9), nicht `--epos-rahmen-leise`, weil jenes schon die Zeilentrennlinie
> trägt — ein Außenrand in derselben Farbe begrenzte nichts; es ist die Farbe jedes Eingabefelds. Nicht gerahmt: die
> Wertetabellen der Ergebnisreiter (Berichtsblöcke). iOS lädt dasselbe Stilblatt. Wachen: `ListenrahmenTests` (14 Fälle),
> `StilblattTests` grün. Abnahme: A‑iU8‑E3‑1…6.

## Statusblock iU9 — Welle 9 umgesetzt (03.09.2026, Basis 8995d3e nach W8, zusammengeführt mit 1cf3dbf)

> **Statusblock iU9 — Welle 9 umgesetzt (03.09.2026, Basis `8995d3e` nach W8, zusammengeführt mit `1cf3dbf`)**
>
> Die Welle stammt aus dem Wellenplan iU9, Abschnitt C Zeile W9, Arbeitsanweisung
> `iU9_W9_Arbeitsanweisung.md` (Scratchpad der Sitzung): **acht Masken der Bedarfskacheln des
> Startbilds → fünf Razor-Komponenten** in `EPOS.UI/Dialoge/Bedarf/` — Gebäudekatalog auf zwei
> Reitern (`Form_Gebaeude1` + `Form_Gebaeude2`), Gebäudeverwaltung mit Admin-Modus, Wohnflächendialog,
> externer Wärmebedarf mit Kanalwahl und **ein** Dialog `BedarfsProfileDialog` für die Drillinge
> Prozesswärme, Stromverbraucher und Brauchwasser; jede WinForms-Fassung gelöscht (Regel M1),
> 3 289 Zeilen Oberflächencode, 42 `MessageBox`. Mit den Assistentenseiten 2 bis 5 laufen **zehn der
> dreizehn Assistentenseiten** als Razor-Komponenten; alle elf Kacheln des Startbilds sind Blazor.
> Fünfzehn Sachcommits und ein Merge:
>
> | Commit | Inhalt |
> |---|---|
> | `b24e5da` | **W9.0a** Assistentenseiten mit beliebigem Listentyp (`IAssistentListenSeite<T>`, vier Listentypen statt nur `WErzeugerModel`) |
> | `53aa3f1` `af4dba5` `fd97501` `01701c6` `04ce2ba` | **W9.0b–f** Katalogfilter, Baualtersklassen und Bauart-Ableitung der Gebäudemasken im Kern; **`Ferienzeit`** (Jahrestag ↔ Tag/Monat, vier Prüfregeln); die fünf Projektlisten der Bedarfsgewerke als Controller-Methoden statt inline-JOINs in Startbild und Kontextmenüs; **ein** Suchmuster im Haus (Wildcard-Filter aus W7 verallgemeinert); Sprungziel `WaermebedarfExternAdmin` |
> | `e384853` `7f59398` `f960151` | **W9.3/W9.1/W9.2** `GebaeudeWohnflaecheDialog`, `GebaeudeKatalogDialog` (zwei Reiter, drei Modi, 78 Felder), `GebaeudeDialog` (Host mit Wohn-/Nichtwohn-Umschalter, vier Filterzweigen, Wildcard-Suche, Admin-Modus, Assistentenseite 2) |
> | `ae1c097` `3c89b6c` `59d984d` | **W9.4/W9.5** `WaermebedarfExternDialog` (Kanal je Zuordnung, Assistentenseite 3), `BedarfsProfileDialog` (Ausprägung `BedarfsArt`, Simulation in der Hülle, W8-Blätter als Überlagerungen, Assistentenseiten 4 und 5), Brauchwasser-Überlagerung im Gebäudekatalog |
> | `ecbbdc0` `6c174e3` `d04a056` | **W9.6–W9.8** 207 Textschlüssel de/en, Formularkarte-Tests (Anker auf `Form_Stromganglinie` und `Form_PufferSp_Bearbeiten` umgehängt), Protokoll, drei CLAUDE.md, STAND.md |
> | `04fc474` | Merge `origin/ios_migration` (Statusblock W8, de-DE-Festlegung der Kurvennamen-Tests) |
>
> **Der Ertrag sind die Projektlisten im Kern und die generische Assistentenseite.** Fünf
> inline-JOINs, die in Startbild, Kontextmenü und Gebäudekatalog je dreimal wortgleich standen,
> sind fünf Controller-Methoden; die Assistentenschnittstelle aus W6 trägt jetzt jeden Listentyp.
> **Vier Befunde behoben:** die Checkbox „Dezentrale Warmwasserbereitung“ wurde gezeigt und nie
> gespeichert (W9‑B3, stiller Datenverlust, A‑2); zwei ungesicherte `Double.Parse` (B4); in
> englischer Oberfläche lief der Verwendungsfilter ins Leere, weil der Steuerwert übersetzt wurde
> (B8); „Überschreiben“ traf nach Umbenennen keine Zeile (B9). **Drei wörtlich übernommen, Entscheid
> beim Anwender** (W9‑O‑1…O‑3): der Filterzweig ohne Verwendung, die Bauweise am Index der
> Gebäudeart-Liste, kWh gegen MWh in derselben Meldung. Dazu W9‑O‑4 (darf „Überschreiben“
> umbenennen?), W9‑O‑5 (Admin-Modus des Katalogeditors hat keinen Aufrufer) und W9‑O‑7
> (Speicherbedarf von zehn WebViews im Assistenten).
>
> **Windows-Läufer seit W8 mit en-US:** Drei Kern-Tests der Welle 8 verglichen deutsche
> Ressourcentexte und fielen auf dem Windows-Läufer (Lauf 33801244655); seit `1cf3dbf` legt jeder
> Texttest die Oberflächensprache fest, und die Wellen laufen zusätzlich unter `en_US` durch.
>
> **Nachweise** (auf dem gemergten Stand `04fc474`, Linux): Build → 0 Fehler, **20**
> Warnungen · `dotnet test WP-Plan.Kern.slnf` → **2 066** grün (1 906 nach W8; +98 bunit,
> +62 Kern), **identisch unter `LC_ALL=en_US.UTF-8`** · Formularkarte **123** grün · Stapellauf
> **55** Masken (63 − 8), 54 erreichbar, 0 × „nein“ · SQL-Prüfer 1 241 Texte, 0 Fundstellen ·
> ChartProben 15 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017 **PASS, byte-gleich**
> (815 043 Werte) · `dotnet publish` mit vollständigem `wwwroot`.
>
> **Protokoll** mit Feldkartenabgleich (8 Masken, Drillinge je Ausprägung), 15 Abweichungen
> (A‑1…A‑15), Windows-Abnahmeliste mit 14 Aufrufwegen und sieben offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W9_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: die vier Filterkombinationen und die Wildcard-Suche, „Ändern“
> schreibt vier Werte zurück, der Katalogeditor über zwei Reiter mit Ferienregeln, „Brauchwasser…“
> als Überlagerung, Gebäudeverwaltung ohne Projektteil, Kanalwahl beim externen Wärmebedarf, die
> drei Bedarfsblätter mit Simulation → Ergebnis → DB ändern/neu → Typ ändern, **Assistent Seiten 2–5
> mit Speichermessung** (zehn WebViews), de/en, 125 %, Finger/Maus, Esc/Tab.
> **Windows-Abnahme 05.09.2026 (PDF des Anwenders, S. 4–5), Befunde W9‑B‑4 (Prozesswärme) und W9‑B‑5
> (Standardlastprofil), behoben in `490c48e`:** „Simulation bringt Ergebnis 0 (monatlicher Verlauf), Grafik
> bleibt leer". **Nicht** die Einheit — der Regressionsverdacht auf W8‑O‑5/W9‑O‑3b ist ausgeschlossen
> (`Energieeinheit.MWh.AusMWh` ist die Identität, kein DTO-Feld vertauscht) — sondern die **Namensauflösung**: die
> Zuordnungen des Projekts tragen die Namen der **Projektkopien** (`Tab_*.Bezeichner`, in der Testdatenbank acht
> mit Zusatz „(P‹Projekt›)"), die Vorschau des Bedarfsprofil-Dialogs schlug sie aber ausschließlich im
> `_STAMM`-Katalog nach (Modus aus `list == null ? Projektrechnung : Katalogvorschau`, so seit V0‑4, kein
> W8/W9-Regress) und übergeht Unbekanntes still — zwölf Nullmonate. `ProfilBedarf.Vorschaumodus(namen, idProjekt)`
> hält die Regel einmal fest: ohne Liste Projektrechnung, mit Liste ohne Projekt Katalogvorschau, mit Liste **und**
> Projekt die neue **Projektvorschau** — Katalog zuerst (jede heute richtige Zahl bleibt zeichengleich), Projektkopie
> als Rückfall mit Kopf und Typprofil. Messung Projekt 1017: Vorschau 0 → 672 000,4 kWh. Zeuge zuerst rot, fünf
> Wachen in `BedarfsProfilVorschauTests`, Referenzlauf byte-gleich (alle elf rechenbaren Basisprojekte). **Offener
> Anwenderentscheid W9‑O‑3c:** eine im Projekt geänderte Kopie wird in der Vorschau weiter mit der Katalogverteilung
> gezeigt (Brauchwasser 1007: Januar 1,900 statt 0,552 MWh bei gleicher Jahressumme); Kopie zuerst brächte Vorschau
> und Lauf zur Deckung, ändert aber angezeigte Zahlen — bewusst nicht nebenbei entschieden.
> **Windows-Abnahme 05.09.2026 (PDF S. 1–2), drei Befunde behoben (`974c198`, Protokoll § 12):** **W9‑B‑1** — das
> im Projekt gespeicherte Gebäude stand unmarkiert in der Liste, weil `AssistentSeite` den Parametersatz der
> stehenden Seite bei **jedem Neuzeichnen** neu zog (die Hüllen bauen dabei eine neue Anzeigeliste — der lebenden
> Komponente wurde die Liste unter den Füßen getauscht) und `GebaeudeDialog` seine Markierung an der
> Objektgleichheit festmachte; der Seiteninhalt wird jetzt beim **Betreten** geholt und gemerkt (wie
> `WizardParent.Next/Back`), die Markierung läuft über die `IdZ` und wird bei einem Listenwechsel nachgezogen.
> **W9‑B‑2** — „Liste zu lang": jede Rasterliste steht seither in einem festen Rahmen mit Rollbalken
> (`--epos-listenhoehe` 22 rem an `.epos-raster-huelle`, stehender Spaltenkopf, Parameter `Begrenzt`, Rückweg
> `--frei`); Anwenderregel, in `EPOS.UI/CLAUDE.md` festgehalten, gilt für Gebäude, Wärmebedarf extern,
> Bedarfsprofile, Gebäudetyp, Stromganglinien, Solarkollektoren, Wärmepumpe, die vier Katalogverwaltungen und die
> Projektdialoge. **W9‑B‑3** — die zwei Richtungsknöpfe des Gebäude-Dialogs tragen Beschriftung und Kurztext in
> beiden Sprachen, das Zeichen zeigt in die Wanderrichtung; das Anordnungsschema selbst ist der Anwenderentscheid
> **#76 („Empfehlung": altes Schema nebeneinander, Umbruch auf schmalem Schirm)** und folgt als eigene Welle, die
> zwei Geschwisterdialoge stehen als W9‑O‑8 offen. 21 bunit-Wachen; Abnahmepunkte A‑W9‑B‑1…B‑3.
> **W9‑O‑3c entschieden (05.09.2026, „W9‑O‑3c: Empfehlung", `ab60806`):** Die Projektvorschau des
> Bedarfsprofil-Dialogs liest die **Projektkopie zuerst** und den `_STAMM`-Katalog nur noch als Rückfall für die
> noch nicht gespeicherte Zeile — Vorschau und Lauf zeigen überall dieselben Zahlen (Brauchwasser 1007: Januar
> 1,900 → **0,552 MWh** bei unveränderter Jahressumme 4 059,7 kWh; Prozesswärme 1041 und Stromverbraucher 1024
> zeichengleich). Projektrechnung und Katalogvorschau unberührt; die eingefrorene Wache „Katalog bleibt erste
> Quelle" ist auf den Entscheid umgestellt, sieben Fälle in `BedarfsProfilVorschauTests`, Referenzlauf über alle
> elf rechenbaren Projekte byte-gleich. Abnahmepunkt A‑W9‑O‑3c.
> **Anwenderentscheid #76 vom 05.09.2026 („#76: Empfehlung"), umgesetzt in `b6fd863`:** `GebaeudeDialog`, `WaermebedarfExternDialog` und
> `BedarfsProfileDialog` stehen wieder **nebeneinander wie im BHKW-PLAN** (Vorbild `Form_Gebaeude` 252/63/436 px:
> Filterblock rechts über der Katalogliste, Detailblock unter dem Paar, Kanalklappliste links bei der Projektzeile)
> und brechen erst unter 900 px untereinander um; die Listen bleiben höhenbegrenzt (W9‑B‑2). Das Pfeilzeichen ist aus
> `GEB_BTN_UEBERNEHMEN`/`_ENTFERNEN` entfernt und hängt jetzt an der Anordnung; „übernehmen" zeigt wie im Vorbild
> zur Projektliste (◀). **W9‑O‑8 damit geschlossen.** Wache `ZweispaltenauswahlTests` (14) mit Medienabfrage gegen
> Token, drei Anordnungsfälle, Selektoren in zwölf Testklassen nachgezogen.
>
> **Anwenderwunsch W9‑E‑2 vom 05.09.2026 (zwei Bildschirmfotos: „der Wärmebedarf vom Gebäude sollte aus diesem
> Dialog (mit Button Simulation) aufgerufen werden können – analog wie aus dem Simulationsbereich", ohne
> Brauchwasser und ohne Gesamt), umgesetzt als **W9.8** in `7811b5d`:** Der Gebäudedialog zeigt über den neuen
> Knopf „Simulation…" (neben „Ändern", frei bei markiertem Projektgebäude, nicht in der Katalogverwaltung) den
> Wärmebedarf **eines** Gebäudes als vierte Überlagerung — Heizung allein, ohne Brauchwasser und ohne Gesamtsumme:
> Wärmebedarf [MWh/kWh wählbar, W8‑O‑5], maximale Wärmelast [kW], Vollbenutzungsstunden, die Jahresganglinie als
> `ChartRenderer.GanglinieNormiert` (dasselbe Bild B1 wie auf der Ergebnisseite, nur mit einer Reihe) im Baustein
> `Diagramm` mit „sortiert", Bild- und Datenzoom, dazu die Monatsübersicht. Ein Vorbild gab es nicht — `Form_Gebaeude`
> trug nur `btn_Aendern`, `Form_Simulation_Kurz` (iF29) rechnete den ganzen Lauf; **neu ist die Auskunft, nicht die
> Rechnung.** Gerechnet wird im Kern (`EPOS.Kern/Controller/GebaeudeBedarfCtrl`) mit **denselben** Methoden wie der
> Lauf: `SimulationWaermebedarf.KlimakalenderLesen` und `…HeizwaermeEinesGebaeudes` sind Anweisung für Anweisung aus
> `Waermebedarf_berechnen` herausgezogen, die Schleife des Laufs ruft sie; Schlüssel ist `Z_ProjektGebaeude.ID`
> (eine neue Abfrage `SELECT ID FROM Tab_Gebaeude WHERE ID_ProjektGebaeude = ?`), die Jahressumme rechnet wie der
> Lauf in float. Der Referenzlauf bleibt byte-gleich, bei einem Ein-Gebäude-Projekt (1007) ist die Zahl des Dialogs
> bitgleich zu `Waermebedarf_Gebaeude_Gesamt`. Ohne Zahl (ungespeicherte Zeile, Projekt ohne Klimaregion) meldet der
> Dialog. 13 Kern- und 20 bunit-Fälle (eine Wache: kein Brauchwasser, kein „Gesamt"), acht Texte de/en; auf iOS ist
> der Dialog nur als Assistentenseite erreichbar, `BedarfGaben` ist dort mitzudenken, wenn der Assistent in iU11
> verdrahtet wird. Protokoll W9.8.
>
> **Nachtrag zu W9‑B‑4/B‑5 (`99033ce`, W8‑B‑3):** Der heute Vormittag berichtigte Rechenstand war nicht die Ursache
> des Nullwerts im Ergebnisdialog; die Namensauflösung der Projektkopien arbeitet korrekt — der Profilbedarf ging
> erst danach verloren, in der Abschrift der Vorschaurechnung in `BedarfsProfileHuelle`, die mit W8‑B‑3 ersatzlos
> entfällt (`BedarfsVorschauCtrl.ProjektVorschau` im Kern).
>
> **Formularraster, Paket P3 (iU8‑E‑2, 05.09.2026, `d3fccf1`):** `GebaeudeKatalogDialog` (41 Felder, neun Raster in beiden Reitern; „Wohnfläche" kurz mit
> „m²", die Ferientage als Tag | Monat nebeneinander — acht `epos-feldpaar` gefallen) und `GebaeudeWohnflaecheDialog`
> (zwei Raster).
>
> **Anwenderwunsch W9‑E‑3 vom 05.09.2026 („Gestalte den Dialog bei Wärmebedarf → Daten importieren analog zum Import
> des Strombedarf → Messdaten importieren (mit grafischer Darstellung etc. wie kürzlich vorgenommen)"), umgesetzt in
> `4d64626`:** Der Dialog „Wärmebedarf Extern" folgt seither `StromganglinieDialog` nach W12‑E‑1/W12‑E‑2. Unter der
> Katalogliste stehen vier Knöpfe — „CSV-Datei importieren…", „Speichern unter…", „DB Ganglinie löschen",
> „Einlesen/Bearbeiten.." —, darunter der einzeilige Formathinweis mit dem vollen Wortlaut am Infoknopf
> (`WBX_HINWEIS_FORMAT`/`…_KURZ`, de/en), und unter den zwei Spalten die Grafik der markierten Ganglinie (Kennzahlen,
> „sortiert", Einheitenwahl, Bild B1 mit Bild- und Datenzoom). Der Befund der Umsetzung ist eine Doppelung: Der
> Wärmebedarf führte eine zweite, engere Importkette neben der AP5-Kette des Stroms (eine Textzeile je Wert,
> Dezimaltrenner Punkt, kein Trennzeichen, keine Einheitenwahl, kein Protokoll, nur 8 760 Werte). Sie ist ersatzlos
> entfallen: `GanglinienImportAblauf` bekommt mit `GanglinienZiel` eine Ausprägung als Daten (Muster
> `KatalogImportProfil`), und vier Masken hängen denselben Baustein `GanglinienImportLauf` ein — der Wärmeimport kann
> seither Excel, Kopfzeilen, Trennzeichen, kWh je Intervall und Viertelstundenwerte. Den Rechenweg der Kennzahlen gibt
> es nur einmal: `StromganglinieAuswertungCtrl` ist zu `GanglinienAuswertungCtrl` mit `GanglinienQuelle`
> verallgemeinert, dieselbe Verdichtung wie im Lauf; die Bausteine `GanglinienGrafik` und `GanglinienImportLauf` liegen
> jetzt unter `Bausteine/`. Drei Befunde fielen dabei: Der Dialog kannte das Auslieferungskennzeichen nicht (er holte
> nur eine Namensliste), die ReadOnly-Meldung der Wärmeverwaltung sprach von der „Stromganglinie"
> (`WBAD_MSG_SCHREIBGESCHUETZT`), und Überschreiben wechselte die Kopf-Id (`ErsetzeGanglinie` behält sie). Eine
> Falle, die es beim Strom nicht gibt: `Z_ProjektWaermebedarf.ID_Ganglinie` zeigt auf die Projektkopie, eine eben
> aufgenommene Zeile trägt die Stamm-Id — der Dialog gibt die Id nur bei `IdZ > 0` weiter, sonst Rückfall über den
> Bezeichner. Hausregel aus dem Umzug: Ein Baustein, der Komponenten eines anderen Namensraums zeichnet, braucht das
> `@using` im Kopf — sonst hält der Razor-Übersetzer sie stumm für HTML-Elemente. Der Kanal bleibt, wie er war, und
> steht im `Formularraster`. Eingefroren: `Wärmebedarf_Laurentiuskirche` 65,430 MWh / 47,649 kW / 1 373,16 h/a.
> Nachweise: Kern 1 230 und UI 2 679 grün (auch en‑US), Windows-Bau 0 Fehler, SQL-Prüfer 0 Fundstellen, ChartProben
> 40/0, Kern-Wächter leer; Referenzlauf nicht nötig — gelesen wird nur. Vierzehn Abnahmepunkte A‑W9‑E‑3 im W9-Protokoll.
>
> **Anwenderentscheid W9‑O‑9 vom 06.09.2026 („Knopftexte im Dialog „Wärmebedarf Extern" wortgleich zum Stromdialog"),
> umgesetzt in `9b594e6`:** Die Katalogleiste des Dialogs liest seither in **allen vier** Knöpfen die Stromschlüssel —
> `STROMGL_BTN_BEARBEITEN` („Bearbeiten…" / „Edit…") und `STROMGL_BTN_LOESCHEN` („Löschen" / „Delete") lösen die
> wärmespezifischen Texte „Einlesen/Bearbeiten.." und „DB Ganglinie löschen" ab, die bei W9‑E‑3 stehen geblieben waren.
> Die Vorgabe der Komponente **ist** damit der Stromschlüssel: Die zwei Überschreibungen der Windows-Hülle entfallen,
> und die zwei nur dort gelesenen Schlüssel `WBX_BTN_BEARBEITEN`/`WBX_BTN_LOESCHEN` sind aus beiden `.resx` gelöscht
> (`Resource.Designer.cs` mit `Werkzeuge/ResourceDesigner` neu erzeugt, „abweichend 0"). Mit demselben Schritt trägt
> die **Rückfrage vor dem Löschen den Dialogtitel** statt des Knopftextes — genau wie `StromganglinieDialog`; „Löschen"
> allein wäre als Überschrift nichtssagend. Nachweise: UI 2 724 grün (39 im Dialog, davon zwei neue Zeugen in de **und**
> en), Kern 1 344 grün, Windows-Bau 0 Fehler, Gate grün, Referenzlauf byte-gleich. Ein Abnahmepunkt A‑W9‑E‑3 ergänzt
> (Nr. 15), vier berichtigt; sieben Abnahmepunkte A‑W9‑O‑9 in der Sitzungsmeldung.
> **Nachtrag W9‑O‑9b (Anwender, 06.09.2026, nach der Abnahme):** Die Überschrift der Katalogspalte heißt
> „Wärmebedarf einlesen" (en „Read in heat requirement") statt „Wärmebedarf aus DB"; die Razor-Vorgabe liest seither
> `WBX_LBL_KATALOG` wie die Knöpfe ihre Schlüssel, der Windows-Rückfalltext ist nachgezogen.

## Statusblock iU9 — Welle 8 umgesetzt (03.09.2026, Basis e5114e1 nach W7, zusammengeführt mit e74136e)

> **Statusblock iU9 — Welle 8 umgesetzt (03.09.2026, Basis `e5114e1` nach W7, zusammengeführt mit `e74136e`)**
>
> Die Welle stammt aus dem Wellenplan iU9, Abschnitt C Zeilen W8a/W8b, Arbeitsanweisung
> `iU9_W8_Arbeitsanweisung.md` (Scratchpad der Sitzung): **zehn Masken der Bedarfstypen → vier
> Razor-Komponenten** in `EPOS.UI/Dialoge/Bedarf/` — die drei Ergebnismasken, die drei
> Stammkopfmasken und die drei Typprofilmasken der Drillinge Prozesswärme, Stromverbraucher und
> Brauchwasser werden **je eine** Komponente mit der Ausprägung `BedarfsArt`, dazu der Gebäudetyp;
> jede WinForms-Fassung gelöscht (Regel M1), 41 `MessageBox`. Die vier Komponenten sind die
> Blätter, die Welle 9 (Bedarfsmasken vom Startbild) als Überlagerungen einhängt. Elf Sachcommits
> und ein Merge:
>
> | Commit | Inhalt |
> |---|---|
> | `e9d7ad6` `fec0a20` `b1d8a4b` | **W8.0a/b/d** `ProzesswaermeStammCtrl` auf den Schnitt seiner Zwillinge; `BedarfsArt`, `BedarfStammCtrl` und `TypProfilCtrl` im Kern (eine Datenseite für drei Kataloge); `TagVCtrl` trägt die Gebäudetyp-Verwaltung |
> | `c046c07` | **W8.0c** drei Bedarfsbilder im `ChartRenderer` (**Monatssäulen**, **Stundenprofil**, **Jahresverlauf**) mit drei Proben |
> | `34e69ff` `1e9c8fc` `6b65f2e` `2119e18` | **W8.1–W8.4** `BedarfErgebnisDialog` (eingefrorenes Rechenobjekt als DTO), `TypStammDialog`, `TypProfilDialog` (Tag kopieren/einfügen wirkt jetzt, Befund B1), `GebaeudetypDialog` — zehn Masken gelöscht |
> | `cbb358e` `04dd413` `51c806d` | **W8.5–W8.7** 143 Textschlüssel de/en, Formularkarte-Tests, Protokoll, drei CLAUDE.md, STAND.md |
> | `8995d3e` | Merge `origin/ios_migration` (Statusblock W7) |
>
> **Der Ertrag ist die eine Datenseite für drei Kataloge.** Drei Zwillingsdialoge je Blatt mit je
> eigenem Aufbaucode sind eine Komponente mit Ausprägung, die Schreibwege laufen in **einer**
> Transaktion (A‑9), die drei Charts sind drei Renderer-Bilder mit Proben. Zwei Befunde des
> Bestands sind behoben (Tag kopieren/einfügen ohne Wirkung, „Novmember“), einer bleibt als
> **Frage an den Anwender**: `Form_Brauchwasser_Admin` öffnet die **Prozess**-Ansicht des
> Ergebnisdialogs (W8‑O‑3, wörtlich übernommen), und im Brauchwasser-Ergebnis steht ein Teiler
> 1000, den die beiden Zwillinge nicht haben — **eine der Anzeigen ist um den Faktor 1000
> daneben** (W8‑O‑5).
>
> **Nachweise** (auf dem gemergten Stand `8995d3e`, Linux): Build → 0 Fehler, **20**
> Warnungen · `dotnet test WP-Plan.Kern.slnf` → **1 906** grün (1 820 nach W7; +66 bunit,
> +20 Kern) · Formularkarte **123** grün · Stapellauf **63** Masken (73 − 10), 61 erreichbar,
> 0 × „nein“ · SQL-Prüfer 1 254 Texte, 0 Fundstellen · **ChartProben 15 Bilder**, 0 Verstöße
> · Referenzlauf 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte) · `dotnet publish` mit
> vollständigem `wwwroot`.
>
> **Protokoll** mit Feldkartenabgleich je Ausprägung, 14 Abweichungen (A‑1…A‑14),
> Windows-Abnahmeliste mit 13 Punkten und sieben offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W8_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: Startreiter und Sicht des Brauchwasser-Ergebnisdialogs,
> „Überschreiben“ nach „Speichern unter“, Tagwechsel im Typprofil verwirft nicht übernommene
> Eingaben, stiller Kurvenübertrag im Gebäudetyp, fünf bzw. acht Kurvennamen nach Kurvenzahl,
> de/en, 125 %, Finger/Maus, Esc/Enter je Dialog.
> **Windows-Abnahme 05.09.2026, Befund W8‑B‑1 (`490c48e`):** Ergebnisdialog, Einheitenwahl und ChartRenderer
> sind **entlastet** — die Nullreihe hinter „Prozesswärme/Standardlastprofil bringt 0, Grafik leer" entstand in der
> Vorschau der Welle 9 (W9‑B‑4/B‑5, Namensauflösung der Projektkopien); die leeren Achsen 0–5 sind das korrekte
> Bild einer Nullreihe (`MonatsSaeulen` mit `maxWert = 0`). Renderer unverändert, ChartProben 32/0.
>
> **Anwenderwunsch W8‑E‑1 und Befund W8‑B‑2 vom 05.09.2026 (Bildschirmfotos „Typ in DB ändern…" aus dem Standard-
> Stromprofil, „eigenes Lastprofil" im Assistenten, WinForms-Vorbild „Stromverbrauchertyp Stundenverteilung"),
> umgesetzt in `01dae1c`:** **W8‑E‑1 — Stundenverteilung in der Anordnung des Vorbilds.** Der Anwender wollte den Dialog
> „so wie zuvor"; die Razor-Fassung stapelte Typen, Wochentage und die 24 Stundenwerte untereinander und war dreimal
> so hoch wie `Form_EingStromTyp` (607 × 544). Zurückgeholt ist die Anordnung des Designers — Typliste links,
> Beschreibung rechts, Reiter darunter; im Wochenblatt drei Spalten zu acht Stundenwerten mit der Nummer vor dem
> Feld, rechts die Wochentagsliste mit „Tag kopieren"/„Tag einfügen", unten „Änderungen Übernehmen" samt Diskette,
> und die Fußleiste in der Reihenfolge Speichern unter | Speichern in DB | Löschen | Neu | Schließen. Übernommen ist
> die Anordnung, nicht das Pixelmaß: `--epos-touchziel` gilt weiter (acht statt neun Typzeilen im Rahmen), die
> Dreiteilung macht das Stilblatt (`grid-auto-flow: column`), im Markup laufen die Felder weiter 1…24, damit der
> Tabulatorweg bleibt. **W8‑B‑2 — ein belegter Typname meldet sich, statt zu werfen.** „Neu" mit vorhandenem Namen
> lief bis ins `INSERT` und endete in einem modalen „SQLite Error 19"-Kasten aus einem Blazor-Ereignis heraus
> (`TypProfilCtrl.Anlegen` prüfte nicht, der Wurf lief über `SqliteDatenzugriff` → `DataRepository.FehlerMelden` →
> `Dienste.Dialog`, Regel A‑8). `TypProfilCtrl.TypExists` prüft jetzt vorher, `Neu`/`SpeichernUnter` geben
> `TypAnlageErgebnis` statt `bool`, die Namensabfrage bleibt offen mit dem Warnbanner „Ein Typ mit diesem Namen ist
> schon vorhanden" (`BPRO_MSG_NAME_BELEGT`, de/en). Beide Aufrufwege — Überlagerung und Assistentenseite — zeigen
> dieselbe Komponente aus einem Parametersatz, alle drei Ausprägungen (Strom, Prozesswärme, Brauchwasser). Zwei
> Kern-Wachen, elf bunit-Fälle; Protokoll „Windows-Abnahme 05.09.2026 — Stundenverteilung".
>
> **Anwenderwunsch W8‑E‑2 und Befund W8‑B‑3 vom 05.09.2026 (Bildschirmfoto „monatlicher Verlauf…" aus dem
> Standard-Stromprofil: „max. Strombedarf 3,72 kW, Gesamter Strombedarf 0, Stromganglinie 0, Strombedarf Gebäude
> 0"), umgesetzt in `99033ce`:** Der Ergebnisdialog des Strombedarfs zeigte „Gesamter Strombedarf 0" und
> „Strombedarf Gebäude 0" neben einem gerechneten Spitzenwert (**W8‑B‑3**). Ursache war eine zweite, von Hand
> nachgezogene Fassung der Vorschaurechnung in `BedarfsProfileHuelle.Rechenstand`, aus der die Zeile
> herausgefallen war, die `Strombedarf_Gebaeude_gesamt` belegt — dieselbe Klasse Fehler wie W9‑B‑4/B‑5, nur eine
> Ebene weiter; weder Einheit noch Projektkopie waren beteiligt. Die sechs Zuweisungen stehen jetzt einmal im Kern
> (`SimulationStrombedarf.ProfilbedarfUebernehmen`, Zwilling von `ProzesssummeUebernehmen`), und die Projektvorschau
> ist als `BedarfsVorschauCtrl.ProjektVorschau` aus der Hülle in den Kern gezogen; Katalog- und Projektvorschau
> nehmen dieselbe Fassung, die Hülle hält den Stand nur noch. Zum Wunsch **W8‑E‑2** gliedert `Kennzahlart` das
> Blatt in drei Kategorien: die LEISTUNG („max. Leistung", vormals „max. Strombedarf") in einem eigenen Block und
> außerhalb der Summe, darunter die Posten, am Fuß abgesetzt die Summe; „Strombedarf Gebäude" heißt jetzt
> „Strombedarf aus Profil" und trägt den gerechneten Wert (8 000 kWh/a bei kWh, 8,00 bei MWh). Der Grafikreiter
> bekommt mit `BedarfGangGrafik` die Stufen Jahr | Woche | Tag samt Ringnavigator — ohne neues Renderer-Bild, über
> einen optionalen `Achsenfenster`-Parameter an `ChartRenderer.Jahresverlauf` (`jahresverlauf_bedarf` bleibt
> byte-gleich; ChartProben jetzt 36 Bilder + 4 Gegenproben). Die Wärmeausprägung ist konsistent mitgezogen und sieht
> ihren Grafikreiter unverändert. Kern 1 077, UI 2 491, Referenzlauf byte-gleich. Beim Merge mit W8‑E‑1 (`4724774`)
> fehlten zwei schließende Klammern aus der Konfliktauflösung; dabei fiel eine seit `91bac96` verwaiste
> Konfliktmarke vor dem Schema-Block auf, die als ungültiger Selektor die erste Regel des Blocks verschluckte —
> beide behoben, Klammerbilanz 739/739.
>
> **Formularraster, Paket P3 (iU8‑E‑2, 05.09.2026, `d3fccf1`):** `BedarfsProfileDialog` (Block „Jahresverbrauch": zwei Felder in einer Zeile,
> „Übernehmen" darunter) und `TypStammDialog` (die zwölf Monatswerte in zwei Spalten zu sechs statt zwölf voller
> Zeilen) hängen im Raster; `TypProfilDialog` und `BedarfErgebnisDialog` bleiben, wie sie heute abgenommen wurden.
>
> **W8‑O‑5b (Anwenderentscheid 07.09.2026: „Nehme die Umrechnung in den Dialogen vor"), umgesetzt in `6839a7a`,
> zusammengeführt in `0a4a207`:** `SimulationWaermebedarf.Waermebedarf_Brauchwasser` stand im Lauf in MWh (Formel (6),
> `:336`) und in der Vorschau in kWh (`BedarfsVorschauCtrl:131/:164`); die Ergebnishülle nahm immer kWh an und teilte
> den Wert des Laufs ein zweites Mal — **Simulation → „Wärmebedarf-Details" zeigte den Brauchwasserbedarf um Faktor
> 1 000 zu klein.** Beide Wege gehen jetzt über `BrauchwassersummeUebernehmen()` (Bauform `ProzesssummeUebernehmen`
> aus W9‑O‑3), die Hülle nennt für jede Energiekennzahl MWh, umgerechnet wird nur noch in der Anzeige über
> `Energieeinheit`; die Vorschauzahl bleibt unverändert, die des Laufs wird richtig. Drei neue Fälle (Kern: Vorschau
> und Lauf weisen für Projekt 1007 dieselbe Menge aus — auf 1e‑6, denn beide Wege verteilen die Monatsmenge nach
> verschiedenen Wochentagskonventionen und die Summe von 8 760 `float`-Werten landet auf benachbarten Stufen, gemessen
> 1 ULP; zwei bunit), `Brauchwasser.wiki` ohne die offene Unstimmigkeit. Nachweis: Kern 1708 / UI 3059 grün, SQL 0,
> Gate grün, Referenzlauf 1030/1007/1017/1045 byte-gleich (das Feld ist reine Anzeigegröße, die Ergebnisspalte kommt
> aus dem Kanalvektor). Abnahme auf Windows: A‑W8‑O5b‑1…6.
>
> **W8‑O‑5c (Anwenderauftrag 07.09.2026: „Prüfe, ob es nicht sinnvoll ist, die gesamten Berechnungen in kWh
> auszuführen und erst in der Anzeige umzurechnen … oder Vereinheitlichen in MWh — je nach Sinnhaftigkeit, aber
> einheitlich"), Prüfbericht in `ca34870`:** `Konzept_Einheiten_EPOS-Plan.md` — Inventar von **242 Umrechnungsstellen
> in 43 Dateien** (die 217 der Grobzählung sind eine Untergrenze; vier weitere Schreibweisen, darunter
> `BhkwPlan.MonatsSumme`/`VectorSumme`), je Stelle klassifiziert mit Datei:Zeile: 127 Energie (68 davon in
> `SimulationErgebnisCtrl` und `SimulationRunner`), 28 Leistung, 24 Emission, 20 Preis, 8 Vollbenutzungsstunden,
> 5 Volumen, 30 Kommentare; acht Unstimmigkeiten U1–U8; Einheit je Rechenstufe; gemessene Gleitkommaprobe (`float`
> rundet relativ, kWh 1,17e‑6 gegen MWh 3,82e‑7 bei 30 MWh — die Einheitenfrage und die `float`/`double`-Frage sind
> zwei Fragen, neun Größenordnungen bringt der `double`-Akkumulator). **Empfehlung: keine einzige Recheneinheit,
> sondern die Regel des Bestands festschreiben** — Zeitreihen kWh, Ausweisungen MWh, die Einheit am Namen, umgerechnet
> an genau zwei Nähten; falls doch eine Einheit gewünscht: kWh (Stundenbasis, Ganglinien, 300 der 312 Referenz-CSV;
> Gegenbefund: die Profilkataloge speichern MWh). Stufenplan S1 (~76 Stellen, Anzeigegrenze härten, byte-gleich; S1.1
> in `6839a7a` erledigt), S2 (~133, Kern je Stufe, neue Basis R4 nötig), S3 (~120, Ergebnistabellen — Empfehlung:
> bewusst nicht). **Offen: Q1…Q8** (Q1 Regel festschreiben statt Einheit wechseln; Q2 Ergebnistabellen bleiben; Q3
> CSV-Export bleibt; Q4 Reihenfolge S2; Q5 `float` → `double` als eigenes Paket; Q6 Stundenreihen nicht; Q7
> CSV-Schlüssel bleibt; Q8 falscher Kommentar an `Strombedarf_Max` sofort).
>
> **W8‑O‑5c Stufe S1 (Anwenderentscheid 07.09.2026: Q1 „Regel festschreiben", Q2–Q8 „Empfehlung"), umgesetzt in
> `5b80a8e`/`3c738e2`, zusammengeführt in `edde16c`:** Die Einheitenregel des Bestands ist festgeschrieben — Zeitreihen
> kWh, Ausweisungen MWh, **die Einheit steht im Feldnamen**, umgerechnet wird an genau zwei Nähten
> (`SimulationErgebnisCtrl`, `SimulationRunner`) und sonst nur in der Anzeige über `Energieeinheit`; kein Wechsel der
> Recheneinheit, keine Migration, keine neue Basis. Umgesetzt: die elf Energie-Umrechnungen der Ergebnisseite sind aus der
> Windows-Hülle in den Kern gewandert (U6), die zwei Ringdiagramme führen eine Konvention statt drei (U5), **43 Namen /
> 51 Deklarationen / 412 Fundstellen** tragen ihre Einheit (U4/U8: `Restwaerme`→`RestwaermeMwh`,
> `Stromverbrauch_Spk`→`StromverbrauchSpkMwh`, `WP_Waermeproduktion_gesamt`→`WpWaermeproduktionGesamtKwh`, …; bewusst
> nicht: `Speicherentladung_Anteil` überall kWh, `ErgebnisModel.Waermeueberschuss` als DB-Modell in MWh), der Kommentar an
> `Strombedarf_Max` nennt kW (U7); sechs der acht Unstimmigkeiten sind erledigt, der CSV-Schlüssel `Sim.Restwaerme` bleibt
> (Q7, Kommentar an der Exportzeile). **Zwei Wächter** in `EinheitenWacheTests` (7 Fälle): kein Faktor 1000 auf einer
> Energiemenge in `EPOS.UI` und `Views` (sieben Schreibweisen, fünf Ausnahmen — alle Leistung, mit Gegenprobe, dass jede
> noch existiert) und Einheit am Namen jeder Jahressumme der sieben Simulationsklassen (13 begründete Ausnahmen:
> Leistungsspitzen, Vollbenutzungsstunden, acht Puffer-Schlüssel der eingefrorenen Basis); beide je einmal als rot belegt.
> Hausregel in `EPOS.Kern/CLAUDE.md`, ein Satz in `EPOS.UI/CLAUDE.md`. Nachweis: Kern 1753 / UI 3064 grün, 0 eindeutige
> Warnungen, Designer „abweichend 0", SQL 0, Gate grün, Referenzlauf **12 von 12 Projekten, 312 von 312 CSV byte-gleich**.
> Konzept Kapitel 7 und 9: S1 erledigt, S2 entfällt, S3 bleibt bewusst liegen; **W8‑O‑5d** (Anwenderentscheid 07.09.2026:
> „alles in double, ist kein Nachteil und systematisch; Summenfunktionen aus Original BHKW-Plan ebenfalls double") folgt
> als eigenes Paket mit neuer Basis R4, nach der Speicheroptimierung (W11b‑B‑5). Abnahme auf Windows: A‑W8‑O5c‑1…6 (Ringe
> und Torte der Übersicht, Eigenanteilsraster, Brennstoffmengen, PV-Vergütung — alle Zahlen unverändert).
>
> **W8‑O‑5d (Anwenderentscheid 07.09.2026: „alles in double, ist kein Nachteil und systematisch. Summenfunktionen aus
> Original BHKW-Plan ebenfalls double"), umgesetzt in `6272845`/`1b67538`/`0d21993`, zusammengeführt in `7bc2b15`:**
> **Der Rechenkern rechnet in `double`.** Umgestellt sind alle Stundenreihen, Akkumulatoren, Zwischenwerte und Rückgaben
> der Simulation, `BhkwPlan.cs` vollständig (die Nachbildung des FPU-Verhaltens der Original-DLL ist aufgegeben), die
> Datenbankgrenze (`Convert.ToSingle` → `ToDouble`; SQLite `REAL` IST `double`) und die Naht zur `SpeicherEngine`
> (`RasterAdapter` nimmt `double[]`, `ZuFloat`/`ZuDouble` entfallen). Die Empfehlung **Q6** des Einheitenkonzepts ist
> damit überholt, der Typteil der Stufe S2 umfassender erledigt als geplant. `float` bleibt an drei belegten Grenzen
> (SkiaSharp-Bildpunkte, KI-Einbettungen, Typprüfungen auf Datenbankwerte) — **147 statt 788 Fundstellen**, im Ordner
> `Simulation/` keine; ein dritter Wächter `DoubleWacheTests` mit leerer Ausnahmeliste hält den Rechenweg frei.
> **Neue Basis `Referenzlaeufe/2026-09-07_R4_Double`** (zwölf Projekte, 312 CSV), R3 rückt in die Geschichte; `kern.yml`,
> `ios.yml`, beide `CLAUDE.md`, `LIESMICH.md`, das Einheitenkonzept und das Gate der Orchestrierung sind nachgezogen.
> **Elf der zwölf Projekte weichen gewollt von R3 ab** (nur 1030 PASS unter iF15): Die Eingangsgrößen ändern sich nur im
> letzten `float`-Bit (Jahressummen des Wärmebedarfs innerhalb 3e‑5), aber drei Schwellen des Modells entscheiden am
> letzten Bit — die Speicherhysterese `SOC ≥ Q_max · SchwelleAus` gegen einen Stand, den `Ladefaehigkeit` genau auf diese
> Grenze fährt (bistabil, Projekt 1018: Umsatz gegen Durchfluss verschoben, Bedarf/Produktion/SOC/Verluste identisch), die
> Volllast/Modulations-Grenze in `Motorlauf_Waermegefuehrt` (1024: Fahrweise **+11,2 % BHKW / −14,4 % WP / −10,8 % Kessel**
> bei gleichem Gesamtbedarf) und die drei `int`-Rückgaben in `BhkwPlan` (1041: Tagesheizlast um eine Einheit). Der neue
> Stand ist der richtige — die Brauchwasser-Jahressumme trifft die Katalogmenge jetzt exakt (742,9000 statt 742,9008 kWh).
> Determinismus 12/12 byte-gleich, Laufzeit 3 s statt 4 s. **Nebenbefund behoben:** der Fortschrittsmelder der
> Speicheroptimierung (W11b‑B‑5) gab außerhalb des Schlosses weiter, zwei Fäden überholten sich — Weitergabe jetzt unter
> dem Schloss. **Offen (Anwender):** W8‑O‑5d‑Q1 Zahlenrand an den drei Schwellen (fachliche Änderung, Empfehlung: ja,
> `1e‑9` wie `SchichtTemperatur`), W8‑O‑5d‑Q2 die `int`-Abschneidung in `BhkwPlan` (Treue zur DLL oder Genauigkeit).
> Nachweis: Kern 1828 / UI 3126 grün, SpeicherEngine 337, KiKern 469, Formularkarte 122, ChartProben 44, SQL 0, Gate grün
> gegen R4 byte-gleich. Abnahme auf Windows: A‑W8‑O5d‑1…8.
>
> **W8‑O‑5d‑Q1/Q2 (Anwenderentscheid 07.09.2026: „W8‑O‑5d‑Q2: keine Treue zur alten DLL, Empfehlung. W8‑O‑5d‑Q1:
> Empfehlung"), umgesetzt in `8cbbff6`, Basis in `3d60d7e`, zusammengeführt in `9aa038f`:** **Q1** — die zwei
> Betriebsschwellen, die bei der `double`-Umstellung am letzten Bit entschieden, tragen einen **benannten Zahlenrand**
> (`Allgemein/Simulation/Rechenrand.cs`: `1e‑9 + 1e‑12 · |Schwelle|`, absolut UND relativ, weil die Schwellen kWh über
> mehrere Größenordnungen tragen; vier Größenordnungen unter der Vergleichstoleranz der Suite). Angewandt an
> `HystereseFortschreiben` (Abschaltschwelle; die Einschaltschwelle bleibt bewusst ohne Rand, auf sie steuert kein
> Rechenweg den Füllstand) und an beiden Stufen von `Motorlauf_Waermegefuehrt`. **Q2** — `TaeglHeizlastWG`,
> `SolareGewinneC` und `SpezWaermeverlusteC` geben `double` zurück, das Borland-`_ftol` fällt; der Aufrufer zieht mit
> (dort war `/ 100` eine GANZZAHLIGE Division — zwei Abschneidungen hintereinander). **Neue Basis
> `Referenzlaeufe/2026-09-07_R5_Zahlenrand`** (zwölf Projekte, 312 CSV, 1 792 statt 1 722 Skalare), R4 rückt in die
> Geschichte; `kern.yml`, `ios.yml`, beide `CLAUDE.md`, `LIESMICH.md` und das Gate der Orchestrierung sind nachgezogen.
> **Elf der zwölf Projekte weichen gewollt von R4 ab, Ursache ist Q2:** Der spezifische Wärmeverlustkoeffizient landete
> auf **ganzen W/K** (1007: 194,5722 → 194, −0,29 %; 1041: 811,0302 → 811) und die Tagesheizlast auf ganzen
> Wattstunden — in R4 sind 257 von 365 Tagessummen in 1041 exakt ganzzahlig, in R5 keine. Die Jahressumme des
> Gebäudewärmebedarfs verschiebt sich um **−1,15e‑3 … +4,43e‑3** (je kleiner das Gebäude, desto mehr), an einem milden
> Tag um bis zu +13,2 % (1041, 2. Mai; Verstärker ist die Verzweigung „Sollwert < Vortemperatur → Stunde zählt nicht"
> des instationären Modells). **Gegenbeweis: 1030** (kein Gebäudebedarf) ist in 21 von 22 Dateien byte-gleich zu R4.
> Die zwei R4-Verschiebungen bleiben und werden eindeutig statt zufällig: 1024 BHKW 179 470 → 179 519 kWh (+2,7e‑4),
> 1018 Umsatz/Durchfluss auf dem R4-Stand, weil der Rand `Q_max · SchwelleAus` sicher als erreicht liest. Determinismus
> 12/12 byte-gleich, 3 313 072 Werte PASS. Nachweise: `RechenrandTests` (6, je Schwelle Grenze und ein ulp darunter,
> Bistabilität, Gegenproben), `BhkwPlanRueckgabeTests` (5); Kern 1841 / UI 3126 grün, SpeicherEngine 337, KiKern 469,
> Formularkarte 122, ChartProben 44, SQL 0, Gate grün gegen R5 byte-gleich. **Notiert, nicht geändert (neue Fragen
> W8‑O‑5d‑Q3/Q4):** neun Vergleiche derselben Bauart in `Motorlauf_Stromgefuehrt`/`_OhneEinspeisung` und
> `EntnahmeObergrenze` tragen den Rand nicht — die Entscheide nannten sie nicht; Empfehlung: nachziehen, Referenzlauf
> bleibt dann voraussichtlich byte-gleich (die zwölf Projekte fahren wärmegeführt). Abnahme auf Windows: A‑W8‑O5d‑Q‑1…6.
>
> **W8‑O‑5d‑Q3/Q4 (Anwenderentscheid 07.09.2026: „Empfehlung"), umgesetzt in `db67c24`, zusammengeführt in `6eb18cc` — der
> Zahlenrand an ALLEN Betriebsschwellen:** Q3 gibt den **14** übrigen Vergleichen der BHKW-Fahrweisen
> `Rechenrand.SchwelleErreicht` (zwei in `Motorlauf_Stromgefuehrt`, zwölf in `Motorlauf_OhneEinspeisung` — die Liste
> des Entscheids nannte neun; eine Stelle fehlte, drei tragen je zwei Vergleiche), in EINER Leserichtung: Schwelle ist
> die Maschinengröße, Wert der Rest; die vier Vorzeichentests `restWaerme < 0` bleiben, sie sind keine Schwelle. Sechs
> der 14 standen schon geschlossen (`<=`); bei den acht strengen fällt die Gleichheit auf die Seite des größeren
> Betriebszustands — an den Volllastgrenzen ergebnisgleich, an drei Modulationsgrenzen von „Motor aus" auf
> „Mindestlast", die Seite, auf der die übrigen ohnehin lagen. Q4 stellt die Reservemarke `Q_max · SchwelleReserve` in
> `EntnahmeObergrenze` spiegelbildlich zur Abschaltschwelle (Wert und Schwelle vertauscht, die Reserve wird von oben
> erreicht); der frühe `double.MaxValue` ohne Reserve bleibt davor. **Nachweis:** `RechenrandFahrweisenTests` (13 Fälle:
> Grenze und ein ulp darunter, Gegenproben, Entladeprobe über fünf Stunden), Kern 1854 / UI 3126 grün; alle 21 Projekte der
> Testdatenbank fahren wärmegeführt (`Betriebsart` 0 oder NULL), deshalb sind die Proben synthetisch — und deshalb ist der
> Referenzlauf **12/12 byte-gleich zu R5** (312 CSV, `diff -rq` leer), Toleranzvergleich PASS (3 313 072 Werte), **keine
> neue Basis**, Gate grün. **Doku:** `BHKW.wiki` (Zahlenrand für alle drei Fahrweisen, stromgeführte Tabelle mit den
> Vergleichszeichen des Codes), `Pufferspeicher.wiki` (Mindestfüllstand erstmals im Rechenweg, Schritt 3; Schritt 4
> nennt die zwei angesteuerten Marken), `EPOS.Kern/CLAUDE.md` (Tabelle aller 17 Stellen, Kriterium „Rand an jede Marke,
> die eine Rechnung ansteuert"). **Notiert, nicht geändert:** `Ladefaehigkeit(obergrenzeAnteil > 0)` (Kennzeichen, keine
> Energieschwelle) und die uneinheitliche Strenge des Paars `P_el·x_min` gegen `restStrom` im Bestand (der Rand ebnet sie
> am Gleichheitspunkt ein). Abnahme auf Windows: A‑W8‑O5d‑Q34‑1…5. Die drei seit R5 geänderten Wiki-Seiten (Wärmebedarf, BHKW,
> Pufferspeicher) wurden am 07.09.2026 mit allen 14 Berechnungsseiten neu hochgeladen (Nachprobe per `action=parse`:
> je 21 Anker, 0 Parserfehler, Zahlenrand und Mindestfüllstand sichtbar).

## Statusblock iU9 — Welle 7 umgesetzt (03.09.2026, Basis 198506f nach W6, zusammengeführt mit 98ebe81)

> **Statusblock iU9 — Welle 7 umgesetzt (03.09.2026, Basis `198506f` nach W6, zusammengeführt mit `98ebe81`)**
>
> Die Welle stammt aus dem Wellenplan iU9, Abschnitt C Zeile W7, Arbeitsanweisung
> `iU9_W7_Arbeitsanweisung.md` (Scratchpad der Sitzung): **acht Masken der Gewerke
> Wärmepumpe (5) und Solarthermie (3) → acht Razor-Komponenten** in
> `EPOS.UI/Dialoge/Waermepumpe/` und `EPOS.UI/Dialoge/Solarthermie/`, jede WinForms-Fassung
> gelöscht (Regel M1) — 3 065 Zeilen Oberflächencode, 43 `MessageBox`. Zwei davon sind die
> Assistentenseiten 7 und 8; damit laufen **sechs der dreizehn Assistentenseiten** als
> Razor-Komponenten. Sechzehn Sachcommits:
>
> | Commit | Inhalt |
> |---|---|
> | `2cd898a` | **W7.0a** `WPCtrl` (Projektgeräte `Tab_WP`) aus der Anwendung in den Kern; `FillListBox` entfällt |
> | `0872196` `7fc9419` `0d1e6e4` `7da4f33` `808837b` | **W7.0b–f** Katalogzeile und Katalogfilter im Kern (die Filterlogik des Wärmepumpen-Katalogs, testbar ohne Oberfläche), **`ChartRenderer.Kennlinien`** mit zwei Proben (COP und Leistung über der Außentemperatur, eine Reihe je Vorlauf), `KenndatenCtrl.Abgleichen` (Kennlinien-Rückschreiben in **einer** Transaktion statt RowState-Schleife), sieben Datenwege in Kern-Controller, Sprungziel `SolarganglinieAdmin` |
> | `29a1bf3` `555e770` | **W7.1/W7.2** die Blätter `WaermepumpenKatalogDialog` (Filterleiste mit Wildcard-Suche) und `KennlinienEditorDialog` (Stützstellen je Vorlauf) — beide nur als Überlagerung |
> | `5e71c49` | **W7.6** `SolarkollektorKatalogDialog` |
> | `b30f6bd` `b98cf35` `a371328` | **W7.3–W7.5** `WaermepumpeStammDialog` (zwei Kennlinienbilder, Wärme/Kühlung), `WaermepumpeAnlageDialog` (47 Felder, Bivalenzlogik, Kostenzeile) und `WaermepumpenDialog` (Host mit **vier Ebenen** Überlagerung: Verwaltung → Anlage → Stammdialog → Kennlinien-Editor) |
> | `0ad0a59` `3655bce` | **W7.7/W7.8** `SolarkollektorenDialog` (Assistentenseite 8) und `SolarganglinieDialog` |
> | `35188f7` `0077533` `e5114e1` | **W7.9–W7.11** 157 Textschlüssel de/en, Formularkarte-Tests (Prüfmuster `Wizard_WPItem`, Sprungtabellen-Test auf `Form_AdminStromspeicher`), Protokoll, drei CLAUDE.md, STAND.md |
>
> **Der Ertrag ist die Kennlinie im Kern.** Vier WinForms-Charts mit je eigenem
> Aufbaucode sind **eine** Renderer-Methode mit zwei Proben; Wärme und Kühlung
> (`Tab_Kenndaten_STAMM`, `Tab_Kenndaten_Kuehlung_STAMM` mit `MAX(Last)`) laufen über
> dieselbe Datenseite. Der Projektgeräte-Controller `WPCtrl` liegt jetzt im Kern — bis W7 der
> letzte Erzeuger-Controller in der Anwendung.
>
> **Ein echter Befund (W7‑O‑4, behoben):** „Bearbeiten" im Kontextmenü der WP-Liste schrieb
> `Regelung = Leistungsstufen`, und `Leistungsstufen` wird im ganzen Bestand nie gesetzt —
> jedes Bearbeiten aus dem Kontextmenü **löschte die Leistungsstufen** des Geräts. Dazu
> zwei Entscheide für den Anwender: die Baujahrliste (2024 doppelt, 2022 fehlte; mit A‑15
> lückenlos) und die nie greifende Vorlauf-/Rücklaufprüfung der Solarkollektoren (W7‑O‑5).
>
> **Nachweise** (auf dem gemergten Stand `e5114e1`, Linux): Build → 0 Fehler, **20**
> Warnungen · `dotnet test WP-Plan.Kern.slnf` → **1 820** grün (1 636 nach W6; +155 bunit,
> +29 Kern) · Formularkarte **123** grün · Stapellauf **73** Masken (81 − 8), 71 erreichbar,
> 0 × „nein" · SQL-Prüfer 1 272 Texte, 0 Fundstellen · **ChartProben 12 Bilder**, 0 Verstöße
> · Referenzlauf 1030/1007/1017 **PASS, byte-gleich** (815 043 Werte) · `dotnet publish` mit
> vollständigem `wwwroot`.
>
> **Protokoll** mit Feldkartenabgleich (8 Masken), 30 Abweichungen (A‑1…A‑30),
> Windows-Abnahmeliste mit 13 Punkten und sieben offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W7_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: die vier Überlagerungsebenen mit Esc und Fokusfalle je
> Ebene, die Kennlinienbilder gegen die alten Charts (der Bildvergleich ist mit iF23 gelöscht),
> Wärme/Kühlung, „Kosten bearbeiten…" als zweites Fenster, Assistentenseiten 7 und 8 (jetzt
> sechs WebViews im Assistenten), beide Solarthermie-Zweige, W7‑O‑4 auf einer Anlage mit
> gepflegter Regelung.
> **Anwenderentscheid #76 vom 05.09.2026 („#76: Empfehlung"), umgesetzt in `b6fd863`:** `SolarkollektorenDialog` und `SolarganglinieDialog` stehen im
> Baustein `Zweispaltenauswahl` (Anordnung unverändert, Klartext-Knöpfe, Umbruch unter 900 px). Die drei
> Wärmepumpenmasken bleiben auf `epos-auswahlpaar`, weil sie keine Projekt↔DB-Auswahl sind — geprüft und im
> Protokoll begründet.
>
>
> **Anwenderwunsch W7‑E‑1 vom 05.09.2026 („Admin-Menüs sind nicht an Größe Bildschirm angepasst"), umgesetzt in
> `ddf4d00`:** Die sechs Fenster der Welle öffnen im Anteil des Arbeitsbereichs (Hüllenregel iU8‑E‑1, 85 % × 90 %,
> gedeckelt auf 92 %); `SolarganglinieAdminDialog` stellt Liste und Eingabe nebeneinander (Baustein `Katalograhmen`).
> Die drei Katalogeditoren bleiben Überlagerung ohne volle Höhe, `WaermepumpenKatalogDialog` bleibt unverändert.
>
> **Formularraster, Paket P1 (iU8‑E‑2, 05.09.2026, `6b2a23f`; Anwenderbeispiel „Verwaltung BHKW"):** Fünf der sechs Masken sind umgestellt (`KennlinienEditorDialog` „Neue Stützstelle",
> `WaermepumpeStammDialog` zehn Felder, `WaermepumpeAnlageDialog` mit Kenndaten/Auslegung/Spitzenlast,
> `SolarkollektorKatalogDialog` 14, `SolarkollektorenDialog` 8); die Wärmepumpen-Anlage nutzt als erster Dialog des
> Hauses den benannten Rückweg `Einspaltig` — ihr Block „Spitzenlast" ist eine Regel, die sich von oben nach unten
> aufblättert. Der `WaermepumpenKatalogDialog` bleibt bewusst außen vor: seine zwölf Felder filtern eine Liste, sie
> beschreiben kein Gerät. `PufferSpProjektDialog` (W10a) ist in keinem Paket umgestellt — offen als iU8‑O‑1.
>
> **Windows-Abnahme 06.09.2026, Wärmepumpe — W7‑B‑1, W7‑B‑2, W7‑E‑2, umgesetzt in `2c9d969`, zusammengeführt in `ffa213d`:**
> Befunde des Anwenders zu Erzeuger → Wärmepumpe → Verwaltung: „Anstelle Name sollte Typ stehen, es fehlt der Hersteller (vor
> Typ). OK-Button funktioniert nicht im Dialog Detailansicht bei Aufruf über button Ändern. Dialoganordnung sehr
> unübersichtlich — angelehnt an die alte Version in branch version_august_2026." **W7‑B‑2, Ursache belegt statt geraten:** Der
> OK-Knopf reagierte, seine Meldung stand nur außerhalb des Sichtfelds — Warnband oben, Knopf unten in einer rollenden
> Überlagerung. Bei einer Zeile aus „Ändern.." sind genau zwei Prüfungen erreichbar: `TemperaturenPruefen` (Bestandszeilen
> tragen vielfach Rücklauf 0, „Neu.." setzt seit W6‑E‑4 wenigstens den Vorlauf — die Asymmetrie, die der Anwender sah) und die
> leere Betriebsart bei bivalentem Betrieb, verschärft durch Altwerte der freien ComboBox des Vorläufers (Befund L0‑1), die
> das `select` gar nicht zeigte; die vier Pflicht-Ganzzahlen können dort nicht `null` werden. Behoben auf beiden Seiten:
> `DbWerte.BetriebsartOderDefault` liest den Altwert über den Wortstamm auf den Steuerwert zurück — an der Lesekante (Hülle,
> Dialogaufbau), die Engine vergleicht unverändert zeichengleich —, und Warnband, Speichern-Leiste und Feldmarkierung sitzen in
> EINER am Rand klebenden Fußleiste. Beleg: `W7_B_2_Das_Warnband_steht_bei_der_Speichernleiste` war rot (OK blockiert, Band
> unauffindbar), die Gegenprobe mit Rücklauf 28 grün; 10 der 11 neuen bunit-Fälle waren vorher rot. **W7‑B‑1:** beide Listen
> führen „Wahl | Hersteller | Typ" — Typ ist die Modellbezeichnung, der Wärmepumpentyp bleibt im Kenndatenblock; vier neue
> Ressourcenschlüssel. **W7‑E‑2:** Die Detailansicht steht in der Dreispalten-Anordnung von `Wizard_WPItem` (Feldkarte per
> Werkzeug gezogen, 48 von 48 Texten des Vorbilds im neuen Dialog; die sieben Pufferfelder und die Modulkosten fehlen bewusst
> seit Ä19): links Auswahl mit eigenem Rollbalken, „Modul-Katalog…", Heizstab- und Sperrzeit-Häkchen mit den Texten des
> Vorbilds, Bivalenz und Betriebsart mit den drei farbigen Erklärkästen; Mitte Auslegung für Verteilung (Vorlauf/Rücklauf,
> Nutzungsdauer in „a"); rechts Kenndaten in der Reihenfolge des Vorbilds, Kosten-/Parameterknöpfe, Reiter COP/Leistung. Der
> Titel steht einmal (`TitelText=""`), die Überlagerung ist breit (`Ueberlagerung.Zusatzklasse`, Wunschmaß 1 280 × 860), kein
> innerer Rollbalken bei 1 400 × 900. Nachweis: Kern 1 640 / UI 2 998 grün (+21 Kern, +11 UI), Formularkarte 122, Designer
> „abweichend 0", SQL 0, Gate grün, Referenzlauf byte-gleich. Abnahmepunkte A‑W7‑B12‑1…9 in der Sitzungsmeldung.
>
> **W7‑B‑3 (Windows-Abnahme V2, PDF 07.09.2026: „im Projekt-Wärmepumpen-Dialog keine Kennlinie", T800-2), behoben in
> `bd606ec`, zusammengeführt in `e8f2bb3`:** Ursache war die **falsche Tabelle** im `Bilder`-Delegaten der Anlagenhülle
> (`WaermepumpeAnlageHuelle.cs:86`): Er ging über `KenndatenCtrl.Reihen` auf `Tab_Kenndaten_STAMM`, bekam aber
> `Daten.IdWp` — bei einer gespeicherten Anlage die Id der Projektkopie (`Tab_WP.ID`, gesetzt in `WizardCtrl` aus
> `WPCtrl.CopyFromStamm`). Gemessen an `Kenndaten_Test.sqlite`: T 800-2 im Projekt 1006 hat Id 1006020 und 16 Stützstellen
> in `Tab_Kenndaten` (Vorlauf 35/45/55/65), in `Tab_Kenndaten_STAMM` keine einzige — das galt für **alle 38 Gerätekopien**,
> betroffen war jede gespeicherte Anlage, ebenso die leere Vorlauf-Klappliste; bei kleiner Projekt-Id hätte der Weg sogar
> die Kennlinien eines fremden Katalogsatzes gezeigt. Neu: `KenndatenCtrl.ReihenProjekt` (dieselbe Tabelle wie der Lauf),
> `WaermepumpeKennlinienCtrl.FuerAnlage` (Projektkopie vor Stammkatalog, mit Herkunft) und `WPCtrl.KennlinienAusKatalog`
> (Nachholen in einer Transaktion, nur in eine Tabelle ohne Zeilen für dieses Gerät). Der Dialog weist eine Katalogkennlinie
> als Herleitung aus und bietet „Kennlinien aus dem Katalog übernehmen" an; der Lauf rechnete nie still mit 0 (Abbruch
> `SIMENG_WP_KEINE_KENNDATEN`), schreibt jetzt zusätzlich den Hinweis `SIMENG_WP_KENNLINIEN_FEHLEN`, wenn das Gerät im Projekt
> gar keine Kennlinien führt. Fünf Schlüssel in beiden `.resx`. Wachen: `WaermepumpeKennlinienTests` (9),
> `WaermepumpeAnlageDialogTests` (5). Referenzlauf byte-gleich (die vier Projekte erreichen den Zweig nie). Abnahme:
> A‑W7‑B3‑1…7 (Kurven für vier Vorlaufstufen, gefüllte Vorlauf-Klappliste, Herleitung und Knopf bei fehlenden
> Projektkennlinien, „16 Stützstellen übernommen", Lauf danach durch, Protokollhinweis, Katalogdialog unverändert).

## Statusblock iU9 — Welle 6 umgesetzt (03.09.2026, Basis 740c73e, zusammengeführt mit W5 ddaea70 und iF22–iF28 f7fefdf)

> **Statusblock iU9 — Welle 6 umgesetzt (03.09.2026, Basis `740c73e`, zusammengeführt mit W5 `ddaea70` und iF22–iF28 `f7fefdf`)**
>
> Die Welle stammt aus dem Wellenplan iU9, Abschnitt C Zeile W6, Arbeitsanweisung
> `iU9_W6_Arbeitsanweisung.md` (Scratchpad der Sitzung): **sieben Masken der
> Erzeugerkacheln → sieben Razor-Komponenten** in `EPOS.UI/Dialoge/Erzeuger/`, jede
> WinForms-Fassung gelöscht (Regel M1) — 4 202 Zeilen Oberflächencode, 55 `MessageBox`.
> **Vier davon sind zugleich Assistentenseiten** (PV, Stromspeicher, Heizkessel, BHKW),
> die ersten Razor-Komponenten im Assistentenrahmen. Vierzehn Sachcommits:
>
> | Commit | Inhalt |
> |---|---|
> | `c825649` | **W6.0a/b** `EnergietraegerVarianteCtrl.Anlegen` (die 185 Zeilen Trägeranlage aus Heizkessel und BHKW, zweimal wortgleich, jetzt **einmal** im Kern, eine Transaktion), `VariantenDerGruppe`, `TraegerUmhaengen` |
> | `68f3634` | **W6.0c** Katalogfilter und Detailblöcke der fünf Projektdialoge in die Stamm-Controller (Heizkessel, BHKW, Photovoltaik, Pufferspeicher); `PufferSpFilter` aus der App in den Kern |
> | `9991259` | **W6.0d** vier Sprungziele (`HeizkesselAdmin`, `StromspeicherAdmin`, `PvAdmin`, `PufferSpAdmin`) — die Katalogverwaltungen bleiben WinForms bis W14a |
> | `ca73a39` | **W6.0f** `KostenKnoepfeLeiste.razor` — der KD6-Kostenblock als Razor-Teilstück; die Ziele sind selbst Blazor-Hüllen und öffnen als **zweites Fenster** (A‑1, wie W4‑O3) |
> | `8fc101e` | **W6.0e** `BlazorAssistentSeite<T>` + `IAssistentErzeugerSeite`: eine randlose, `TopLevel=false`-taugliche Hüllenform mit einer verzögert gebauten WebView; `WizardParent` bedient die vier Seiten über **einen** Zweig statt vier |
> | `bd9151f` `dd11c2b` | **W6.1/W6.2** die Katalogeditoren `HeizkesselKatalogDialog` (42 Felder, 3 Speicherwege) und `BhkwKatalogDialog` (58 Felder, abgeleitete Investition, Katalogsatz-Rückfrage) |
> | `448d4c5` `1bb2c19` `ef28099` | **W6.3/W6.4** die Hosts `HeizkesselDialog` und `BhkwDialog` — Trägerwahl, Katalogeditor und Namensdialog als Überlagerungen im selben Fenster; `ErzeugerAuswahlDaten.cs` als gemeinsame Datenform (`Schluessel` ≠ `GeraetId`: zwei gleiche Kessel teilen eine Projektkopie) |
> | `329a1be` `fa670fc` `6e2a2f5` | **W6.5–W6.7** `PhotovoltaikDialog`, `StromspeicherDialog`, `PufferspeicherDialog` (Eindeutigkeitsrückfrage als `Rueckfrage`-Baustein statt `Dienste.Dialog`) |
> | `18a3eb9` | **W6.8/W6.10** Ressourcen-Sammelnachtrag, Protokoll, drei CLAUDE.md, STAND.md |
>
> **Der Ertrag ist die Assistentenseite.** Bis W5 saß jede Razor-Komponente in einem
> modalen Fenster oder in einer `BlazorSeite` einer bestehenden Maske. Der Assistent
> hält seine 13 Seiten als `Func<Form>` und zeigt sie randlos in seinem Panel — dafür
> brauchte es eine **Form**, die eine WebView trägt und erst beim Anzeigen baut
> (Risiko R5: vier WebViews im Voraus). `AssistentSeiten.ERZEUGER[9..12]` zeigen jetzt
> auf `BlazorAssistentSeite<…>`; Welle 7 hängt Wärmepumpe und Solar auf demselben Weg ein.
>
> **Zwei Befunde für den Anwender** (W6‑O‑1, W6‑O‑2, wörtlich übernommen nach Regel F3):
> die Gruppen→`Brennstoff`-Ketten von Heizkessel und BHKW sind uneinheitlich („Sonstige"
> trifft beim Heizkessel nie, ist auf `23` = Fernwärme abgebildet; Fernwärme und
> Wasserstoff fehlen der Kesselkette), und die Filterstufe „Alle" (`Ptherm Like '%'`)
> lässt Katalogsätze ohne Ptherm herausfallen. Vorschlag: künftig über
> `Tab_Brennstoff_Stamm.ID_Kategorie` filtern, dann gibt es die Ketten nicht mehr.
>
> **Nachweise** (auf dem gemergten Stand `198506f`, Linux): `dotnet build WP-Plan.sln
> -c Release -p:Platform=x64` → 0 Fehler, **20** Warnungen · `dotnet test
> WP-Plan.Kern.slnf` → **1 636** grün (1 485 nach W5; +91 bunit, +27 Kern-Tests für die
> neuen Controller-Methoden gegen `Kenndaten_Test.sqlite`) · Formularkarte **123** grün
> (Anker von Heizkessel/BHKWEing auf Klimadaten, Gebäude und Brauchwasser umgehängt) ·
> Stapellauf **81** Masken (88 − 7), 79 erreichbar, 0 × „nein", 0 × „verwaist" ·
> SQL-Prüfer 1 283 Texte, 0 Fundstellen (Prüfer: lokale Variablen werden nicht mehr gegen
> fremde Konstanten aufgelöst, W6‑O‑4) · ChartProben 10 Bilder, 0 Verstöße ·
> Referenzlauf 1030/1007/1017 gegen `2026-08-30_B3-Kaskade` **PASS, byte-gleich**
> (815 043 Werte) · `dotnet publish` mit vollständigem `wwwroot`.
>
> **Protokoll** mit Feldkartenabgleich (7 Masken), 20 Abweichungen (A‑1…A‑20),
> Windows-Abnahmeliste mit zehn Aufrufwegen und sechs offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W6_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus**: die fünf Startkacheln, das Kontextmenü der
> Übersichtslisten (auch REF-Liste), Assistentenseiten 9–12 (Wechsel unter 1 s, kein
> Aufblitzen, Speicher der Browserprozesse), Heizkessel-/BHKW-Admin → Bearbeiten/Neu,
> die Sprungbrücke in die vier Katalogverwaltungen (W2‑7) und die Kostenleiste als
> zweites Fenster.
> **Windows-Abnahme 04./05.09.2026, Befund W6‑B‑1 (Hauptfenster als ungestyltes HTML):** Der Regel
> `.epos-mehrzeilig { white-space: pre-line;` fehlte das schließende `}`. Es ging **nicht** in W6.4a
> (`1bb2c19`, dort heil und letzte Regel des Blatts) verloren, sondern im Merge **`7e8e341`** (Welle 5 in
> Welle 6, 03.09.2026): beide Zweige hatten an dasselbe Dateiende angebaut, beim Auflösen blieb die eine
> Zeile liegen. Chromium las die 414 Blöcke dahinter (Reiter, Kachelraster, Zellenaktionen, Startseite,
> Menüband) als **verschachtelte** Regeln unter `.epos-mehrzeilig` — gültiges CSS, keine Meldung; die 155
> Blöcke davor (Dialoge, Knöpfe, Felder, Raster) waren nie betroffen, darum sahen die Dialoge der Wellen 6
> bis 15 in der Abnahme richtig aus und erst das Hauptfenster (W16c) fiel um. Auch der Stilblattteil von
> W5‑B‑1 war bis dahin wirkungslos. Klammer gesetzt (`aa98738`, Bilanz 619/619); Wache
> **`EPOS.UI.Tests/StilblattTests.cs`** (`5c9d95c`): eigener Strukturparser über jedes `.css` unter
> `EPOS.UI/wwwroot` — Klammerbilanz, keine Stilregel in einer Stilregel, kein `&`-Selektor, Zeile und
> Selektor in der Meldung, Gegenprobe mit entfernter Klammer; Bestandsaufnahme ohne weiteren Befund.
> Hausregel in `EPOS.UI/CLAUDE.md`, Herleitung in Protokoll W6 § 12.
> **Anwenderentscheid #76 vom 05.09.2026 („#76: Empfehlung"), umgesetzt in `b6fd863`:** die fünf Erzeugerdialoge (Heizkessel, BHKW,
> Photovoltaik, Pufferspeicher, Stromspeicher) stehen im neuen Baustein **`Zweispaltenauswahl`** — Anordnung
> unverändert nebeneinander wie `Form_Heizkessel` (316/88/313 px), neu sind der Klartext mit Kurztext auf den zwei
> Knöpfen (beide Sprachen, `AUSWAHL_BTN_*`) und der Umbruch untereinander unter 900 px (Token
> `--epos-zweispalten-umbruch`, Glyphen ◀▶/▲▼ je Breite). Protokollabschnitt „Anwenderentscheid #76", Abnahmepunkte
> A‑#76 (breit nebeneinander, schmal untereinander, Listen begrenzt, Knöpfe beschriftet und gesperrt ohne Markierung).
>
> **Formularraster, Paket P1 (iU8‑E‑2, 05.09.2026, `6b2a23f`; Anwenderbeispiel „Verwaltung BHKW"):** Sechs Masken der Welle hängen ihren Parameterblock ins `Formularraster`
> (`HeizkesselKatalogDialog` 21 Felder in vier Gruppen, `BhkwKatalogDialog` 26, `BhkwDialog`, `PhotovoltaikDialog` —
> der gestrichelte Anlagenrahmen bleibt, der Raster steht darin —, `StromspeicherDialog`, `PufferspeicherDialog`,
> dazu `HeizkesselDialog` aus #90 nachgezogen); die handgebauten `epos-feldpaar`-Wirte sind in `Dialoge/Erzeuger/`
> restlos verschwunden. Der Detailblock des BHKW-Projektdialogs — das Beispiel des Anwenders — steht jetzt in drei
> bis fünf Zeilen statt sieben: Name und Hersteller in der Feldspalte, thermische und elektrische Leistung kurz
> nebeneinander, Beschreibung über beide Spalten, darunter Brennstoff und die drei kurzen Felder Grenzleistung,
> Vorlauf, Rücklauf; die Summenzeile unter der linken Liste ist einspaltig kompakt. Eine `Herleitungszeile` im Raster
> spannt seither über alle Spalten, sodass die Kostengruppe des BHKW-Katalogs ein Raster bleibt. Neu hausweit:
> `Textfeld.Kurz` und `ErzeugerDetail.IstZahl` — welches nur lesbare Anzeigefeld kurz ist, entscheidet sich an einer
> Stelle am Wert, nicht an der Beschriftung. Nicht umgestellt: die Spalte „Eigenschaften" der BHKW-Datenbankliste
> (vier Zeilen je Zelle) kommt aus `BhkwHuelle.KatalogZeilen` in der Windows-Hülle — als offener Punkt im W6-Protokoll.
>
> **Zusammenführung Rechner 2 am 05.09.2026 (`12aa3a5`):** Auf dem zweiten Rechner des Anwenders lief seit dem
> 02.09.2026 eine eigene Entwicklungslinie, sechsmal mit `origin/ios_migration` zusammengeführt und jedes Mal mit
> Referenzlauf-Nachweis (M1–M5, 355/355 byte-gleich zur jeweiligen Vorstufe); sie kam als Zweig
> `pv-ertragsmodell-rechner2` (28 Commits auf Basis `ed71d73`, Tip `d331823`) nach GitHub und ist hier mit **einem**
> Konflikt (`epos-ui.css`, beide Seiten hatten Blöcke ans Dateiende gehängt) zusammengeführt. Inhalt: das
> **PV-Ertragsmodell** Paket A (Zeitbasis UTC→Ortszeit der `Tab_Solar`-Leser, Anlagenparameter
> WR-Wirkungsgrad/Systemverluste, Migration 62) und Paket B (Rechenmodell ERWEITERT mit Hay-Davies, Huld,
> WR-Kennlinie, Clipping, Degradation; Modellwahl im PV-Dialog über `PvModellFelder`; Datenmodell PV_Modell/
> Wechselrichter/Technologie, Migration 63 — EINFACH unverändert), die **PV-Katalog-Koeffizienten** (Import CEC/PAN,
> `PvModulPlausibilitaet`, Reparaturskript unter `sql/pv_katalog/`), die **Projektdialoge** (Löschen mit Mehrfachauswahl,
> Öffnen, Neues Projekt) und die **Projektstammdaten** (Datumspflege, Kunde/Bearbeiter, `ProjektKopfSeite`), dazu
> **FS1 N‑1** (AnkerNachziehen unter SQLite: Access-JOIN-UPDATE durch korreliertes UPDATE ersetzt). Konzepte:
> `Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md`, `Konzept_Projektstammdaten_EPOS-Plan.md`; Protokolle unter
> `WindowsFormsApplication1/Allgemein/Simulation/` (PaketA, PaketB, FS1, Merge 1–5); acht Referenzlauf-Ordner
> `Referenzlaeufe/2026-09-0x_*` (je 356 Dateien) als Nachweise mit `LIESMICH.md`. Gate hier: Bau 0 Fehler, Tests 469 + 337 + 2 634 + 1 168 grün (serialisiert), Formularkarte 122, SQL-Prüfer 0 Fundstellen, ChartProben 36 Bilder grün. Betroffene
> Wellen: W3 (`PhotovoltaikVerguetungDialog`), W6 (`PhotovoltaikDialog`), W13 (`PvModulImportDialog`), W14a
> (`ModulKatalogDialog`), W15a (`ProjektWahlDialog`), W16a (`ProjektKopfSeite`). **Nachzug `a738d08`:** Die
> Rechner-2-Linie brachte die Migrationen 62–64 mit, die Testdatenbank `Referenzlaeufe/Kenndaten_Test.sqlite` stand
> aber auf Schemastand 61 — der SQL-Prüfer meldete deshalb neun Spaltenfehler in PV- und Projektabfragen, dazu eine
> Prüferlücke (dynamischer `SELECT` ohne `FROM` in `WizardCtrl.FachspaltenSelect`). Neues Werkzeug
> `Werkzeuge/Testdatenbankschema` zieht die Testdatenbank idempotent auf den Stand des `SchemaKatalog` (`--trocken`
> zeigt die anstehenden Schritte), Regel „Testdatenbank mitziehen" in `BETRIEB_SQLITE.md` 6.5; Prüfer 0 Fundstellen.
> **Referenzbasis:** Der Referenzlauf weicht seit dieser Zusammenführung für 1030, 1007 und 1017 von
> `2026-08-30_B3-Kaskade` ab — genau die Paket-A-Verschiebung der Solar-Zeitbasis von UTC auf Ortszeit, die auf
> Rechner 2 als PA0→PA1 dieselben Zahlen zeigt (1007 und 1017 hier byte-gleich zu deren PA1/PB1/M5). Ob die Basis
> neu eingefroren wird, war dem Anwender vorgelegt — **Entscheid 05.09.2026: ja.** Neue CI-Basis
> `Referenzlaeufe/2026-09-05_R2_Zeitbasis` aus `EPOS.Referenzlauf` auf Linux: elf Projekte, 282 CSV (1011 und 1021 der
> B3-Basis stehen nicht in `Kenndaten_Test.sqlite`), zweiter Lauf byte-gleich; gegen die Windows-Basis M5 von Rechner 2
> sind sechs der acht gemeinsamen Projekte byte-gleich, 1030 und 1039 tragen die schon zwischen B3 und PA0 bekannte
> Umgebungsdifferenz des zweiten Rechners. `kern.yml`, das Gate und `CLAUDE.md` halten seither gegen R2_Zeitbasis;
> `2026-08-30_B3-Kaskade` bleibt zur Geschichte liegen.
>
> **Anwenderwunsch W6‑E‑1 vom 05.09.2026 („optional sollten beim ausgewählten PV-Modul alle Eigenschaften/Parameter
> angezeigt werden"), umgesetzt in `87191a8`:** Der Block „Modul Eigenschaften:" zeigte vier der neunzehn Spalten von
> `Tab_PV_STAMM`; die übrigen dreizehn standen nur im Katalogdialog — dort, wo man ein Modul ÄNDERT, nicht dort, wo man
> es AUSWÄHLT. Unter dem Block steht jetzt ein Aufklapper „Alle Modulparameter anzeigen" (`PVD_AUFKLAPP_PARAMETER`,
> de/en), zugeklappt als Vorgabe, nur lesend, im `Formularraster` mit Einheit hinter dem kurzen Feld. Es ist ein Knopf
> mit `aria-expanded` und kein `<details>`: Nur so gehört der Offen-Zustand dem Dialog und übersteht den Wechsel des
> gewählten Moduls. Beschriftung und Einheit kommen aus `ModulKatalogProfil` (Ausprägung Photovoltaik) — derselben
> Quelle wie der Katalogdialog —, die zwei Temperaturkoeffizienten aus dem Modulimport (`PVIMP_LBL_ALPHA_ISC`/
> `_BETA_VOC`), die der Katalog nicht führt; neu ist genau EIN Anzeigetext. Gelesen wird im selben Vorgang:
> `PhotovoltaikStammCtrl.Detail` trägt seither alle Spalten (4 → 17), und weil der Dialog ihn bei jeder
> Auswahländerung ruft, zieht der Block von selbst nach. Nicht gepflegt heißt „–", nicht 0 — NULL und die 0 des
> Bestands sind dieselbe Aussage; ein unbekannter Technologiecode bleibt sichtbar. `Textfeld` bekommt dafür `Einheit`
> wie `Zahlenfeld`. Tests: `PvModulparameterTests` 12 neu, `PhotovoltaikDialogTests` 14 → 21, beide Reihen unter de
> und en grün; SQL-Prüfer 0 Fundstellen, Kern-Wächter leer. Beobachtung W6‑O‑5: Die Einheit „[KW]" an Modul- und
> Gesamtleistung war bestandstreu aus `Form_PV` übernommen, sachlich aber Watt (der Katalog nennt denselben Wert
> „Nennleistung (Pmax)" in W, `AnlagenKwp` teilt durch 1000). Zehn Abnahmepunkte A‑W6‑E‑1 im W6-Protokoll.
>
> **Anwenderentscheid W6‑O‑5 vom 05.09.2026 („Gesamtleistung in kW"), umgesetzt in `d534af4`:** Der PV-Projektdialog
> zeigte zwei Leistungen unter der Beschriftung „[KW]", die beide Watt waren — `Tab_PV.Leistung` führt die
> Modulleistung in Watt, und die Gesamtleistung war deren rohes Produkt mit der Modulanzahl; zehn Module ergaben
> „2751,912 KW". Seither heißt das Modulfeld „Modul Leistung [W]" und die Gesamtleistung „Gesamtleistung [kW]" mit drei
> Nachkommastellen („2,752"); der englische Text „Total power [kW]" sagte die Einheit als einziger schon richtig und
> bleibt. Die Wandlung steht als `PhotovoltaikCtrl.GesamtleistungText` im Kern neben `KwpSumme` — eine kWp-Wahrheit,
> ohne Windows nachweisbar — und ist reine Anzeige: Der Rechenweg (`AnlagenKwp`, `KwpSumme`, Simulation,
> Wirtschaftlichkeit) ist unberührt, der Referenzlauf bitgleich. Nachweis: Kern 1 209 → 1 213, UI 2 656 → 2 659, beide
> grün unter de und en; Windows-Bau 0 Fehler; Kern-Wächter leer. Vier Abnahmepunkte A‑W6‑O‑5 im W6-Protokoll.
>
> **Anwenderwunsch W6‑E‑2 vom 06.09.2026 („Wechselrichter – ausgegraut. Import liegt nicht vor, Admin zum
> Anlegen/Bearbeiten liegt nicht vor … Mockup und Konzept vor Umsetzung"), Konzept und Mockup in `8fee437`:** Der Knopf
> trägt genau eine Sperrbedingung (`PvModellFelder.razor`: `disabled`, solange das Modell nicht ERWEITERT ist) und ist
> im Modell EINFACH bestimmungsgemäß gesperrt; EINFACH multipliziert den Ertrag mit dem konstanten Faktor
> `PV_WrWirkungsgrad` (NULL = 0,95, `SimulationPV`) — ohne Clipping, Kennlinie und AC-Nennleistung. Nachgeprüft fehlen
> Wechselrichtertabelle, Katalogeintrag, Verwaltungsausprägung, Import, Strangbegriff und Menüpunkt vollständig; die
> Modulkennwerte für eine Auslegungsprüfung liegen seit W6‑E‑1 ungenutzt im Katalog. Das neue Papier
> `Konzept_Wechselrichter_EPOS-Plan.md` (982 Zeilen) schlägt `Tab_Wechselrichter_STAMM` mit Projektkopie, die
> Strangzuordnung `Z_AnlageStrang` (Migrationsschritte 65/66), eine Kennlinie aus sechs Stützstellen mit
> mitgeschriebenen Sandia-Koeffizienten, den CEC-Wechselrichterimport und den Rechenweg Module → Strang → MPPT → Gerät
> → Clipping mit acht Auslegungsprüfungen vor; ohne Strangzuordnung bleibt der Rechenweg Zeichen für Zeichen
> erhalten, die Basis `2026-09-05_R2_Zeitbasis` also byte-gleich. Vorgeschlagen sind drei Stufen (S1
> Katalog/Verwaltung/Import ohne Rechenwirkung sofort, S2 und S3 zusammen). Das Mockup
> `Mockups/Wechselrichter_Mockup_2026-09-06.html` (1 439 Zeilen, eigenständig, Hausstil) zeigt vier Ansichten:
> PV-Dialog mit dem Abschnitt „Wechselrichter und Stränge" samt Plausibilitätsampel, Verwaltung, Import und den
> Rechenfluss als SVG. **Nichts umgesetzt; zehn Entscheidungsfragen W6‑E‑2‑Q1…Q10 liegen beim Anwender**, darunter
> Kennlinienform (Empfehlung Stützstellen, weil Sandia die DC-Spannung je Stunde bräuchte) und ob der Wechselrichter
> auch in EINFACH wirkt (Empfehlung ja — damit entfällt der ausgegraute Knopf).
>
> **Wechselrichter Stufe S1 (Anwenderentscheid 06.09.2026: W6‑E‑2‑Q1…Q10 Empfehlung angenommen, neuer Wunsch W6‑E‑3
> „zwei Optionen: vereinfacht ohne Wechselrichter mit Pauschalen / mit Wechselrichter"), umgesetzt in `6baee8b`:** Der
> Wechselrichter war die einzige Gerätefamilie ohne Katalog — seine Kennlinie stand als drei Zahlen an der Anlagenzeile,
> von Hand getippt, ohne Herkunft und ohne Prüfung. **Migrationsschritt 65** legt `Tab_Wechselrichter_STAMM` und die
> spaltengleiche Projektkopie `Tab_Wechselrichter` an (34+34 Spalten, DDL EINMAL in `WechselrichterSchema`, gefahren von
> Migration, `Werkzeuge/Testdatenbankschema` und der Testvorrichtung); `SchemaStand.Zielversion` steht auf **65**, die
> Testdatenbank ist nachgezogen. Dazu `WechselrichterModel`/`-StammCtrl`/`-Ctrl` (alle Fachwerte `double?` — NULL heißt
> „keine Prüfung", nicht 0), der zwanzigste `KatalogRegistry`-Eintrag, die **dritte Ausprägung** von `ModulKatalogDialog`
> (25 Felder in drei Gruppen, erster Herstellerfilter des Hauses, Parameterübersicht als achte `Anlagenart`) und der
> **CEC-Wechselrichterimport** samt geschlossener Sandia→Stützstellen-Umrechnung (`η100 = Paco/Pdco` exakt; die Liste
> mit 2 343 Geräten von 152 Herstellern wurde über das Netz geholt und die Umrechnung über alle nachgerechnet). Zwei neue
> Menüpunkte unter „Administration". **Ohne jede Rechenwirkung:** kein Rechenweg liest die zwei Tabellen, kein
> Bestandsprojekt führt eine Kopie; Referenzlauf 1030/1007/1017 gegen `2026-09-05_R2_Zeitbasis` **byte-gleich**,
> Migration idempotent, SQL-Prüfer 0. Nachweis: 36 Kern-Fälle und 17 bunit-Fälle; der Fall
> `Der_Wechselrichter_rechnet_in_S1_noch_nicht` fällt rot aus, sobald S3 gelaufen ist. Q10 ist **teilweise** eingelöst:
> der Importdialog ist eine eigene Komponente (~300 Zeilen) auf den geteilten Bausteinen, nicht eine Ausprägung des
> 771-Zeilen-Modulimports — offen als **W6‑O‑1** (ein Wirt in S2). W6‑E‑3 ist im Konzept 7.1 festgeschrieben, mit dem
> Vorschlag einer eigenen Spalte `Tab_Energieanlagen.PV_Wechselrichterweg` (NULL = vereinfacht) statt der Ableitung aus
> der Strangtabelle; Umsetzung in S2.4. Weitere offene Punkte: **W6‑O‑2** (die CEC-Liste führt weder `Anzahl_Mppt` noch
> `S_AC_Max`), **W6‑O‑3** (Auslieferungskatalog leer — vorbefüllen?). Elf Abnahmepunkte A‑W6‑E‑2‑S1 in der
> Sitzungsmeldung; Gate grün.
>
> **Wechselrichter Stufe S2 (W6‑E‑2, W6‑E‑3), umgesetzt in `706fe6f`:** Migrationsschritt **66** legt `Z_AnlageStrang` an (zwölf
> Spalten, `ID_Anlage` mit `ON DELETE CASCADE`, `ID_Wechselrichter` restriktiv auf die Projektkopie) und die Spalte
> `Tab_Energieanlagen.PV_Wechselrichterweg` des sichtbaren Schalters aus **W6‑E‑3** (NULL = vereinfacht, `KATALOG` = mit
> Wechselrichter — Empfehlung (a) des Konzepts 7.1); `SchemaStand.Zielversion` steht auf 66, die Testdatenbank ist nachgezogen.
> `AnlageStrangModel`/`AnlageStrangCtrl` sind Zeile für Zeile die Bauart von `Z_AnlageSenke`; die **Falle N3.3** schließt Block
> **ST1** in `WizardCtrl` — der Speicherweg ist Löschen + Neuanlegen, und ohne Rettung räumte jedes Speichern die Strangzuordnung
> ab. `StrangPlausibilitaet` liefert **P1 bis P8** als Ampel je Strang und je Gerät; die sechs Zeilen und drei Gegenproben aus
> **Anhang A** kommen Zahl für Zahl heraus (425,3 V / 260,9 V / 355,3 V / 9,5515 A / DC/AC 1,10076). Im PV-Dialog ersetzt der
> Abschnitt **„Wechselrichter und Stränge"** den ausgegrauten Knopf: zwei sichtbare Optionen mit weicher Sperre (W16b‑E‑6),
> Strangtabelle mit Ampelsätzen, abgeleitete Modulzahl (**Q9**) und die Anlagenüberlagerung als Rückfall — in beiden Modellen
> bedienbar (**Q5**); der Projekttransfer `.wpx` trägt die zwei Tabellen. **S2 rechnet nicht:** `SimulationPV` ist unberührt,
> ein sichtbarer Satz in der Maske sagt es bis S3, und der Referenzlauf 1030/1007/1017 bleibt **byte-gleich**. 1 446 Kern- und
> 2 822 UI-Fälle grün, SQL-Prüfer 0, Designer „abweichend 0", Gate grün; der Merkposten heißt seither
> `Der_Wechselrichter_rechnet_in_S2_noch_nicht`. Neue offene Punkte **W6‑O‑4** (kein Herstellerfilter über der Klappliste der
> Strangtabelle) und **W6‑O‑5** (die Ampel prüft gegen das Modul der ersten Projektzeile). Elf Abnahmepunkte A‑W6‑E‑2‑S2 in der
> Sitzungsmeldung. S3 (Rechenweg, Kennzahlen, Prüfstand) läuft.
>
> **Wechselrichter Stufe S3 (W6‑E‑2, W6‑E‑3), umgesetzt in `d88243e`:** Der Rechenweg Module → Strang → MPPT → Gerät → Clipping
> steht. `PvStrangModell` (neu, ohne Datenbank und Oberfläche) trägt die Kennlinie mit sechs Stützstellen, die Gerätegruppierung
> nach (Wechselrichter, Gerätenummer), Clipping und Nachtverbrauch; `SimulationPV` bekommt einen DRITTEN Zweig, die zwei
> vorhandenen bleiben Zeichen für Zeichen stehen. **Die Vorrangregel steht VOR dem Datenbankzugriff:** Gelesen wird erst, wenn
> eine Anlagenzeile den Schalter `KATALOG` trägt — ein Bestandsprojekt kostet keine einzige zusätzliche Abfrage, dann sind es
> zwei für das ganze Projekt; ein Transpositions-Zwischenspeicher je (Neigung, Azimut) rechnet die Sonnengeometrie eines
> Ost/West-Feldes zweimal, nicht achtmal. Kennzahlen (DC/AC, Clipping in kWh und %, Volllaststunden AC, Jahresnutzungsgrad,
> Nachtverbrauch) stehen im Simulationsprotokoll, auf der PV-Karte und als zweite Tabelle im Ergebnisreiter;
> `Tab_ErgebnisPhotovoltaik` bleibt unverändert. Die Wechselrichterkosten sind ein eigener Posten der PV-Investition (**Q8**),
> nur für Anlagen auf dem Weg `KATALOG`. Aufgeräumt: der S3-Hinweis ist fort, zehn Katalogspalten stehen auf `Simulation` und
> `Kosten` auf `Wirtschaftlichkeit`, der Merkposten ist zum Zeugen `Der_Wechselrichter_rechnet_ab_S3` geworden. **Ein Befund
> aus S2 dazu:** Der Persistenzwert „vereinfacht" war 21 Zeichen lang und passte nicht in seine `TEXT(20)`-Spalte — jedes
> Speichern einer Anlage mit dieser Wahl scheiterte; er heißt jetzt `VEREINFACHT`, eine Migration braucht es nicht. Prüfstand
> `PvStrangRechnungTests`: **15 Fälle**, darunter die Bitgleichheit ohne Zuordnung, der Ein-Strang-Fall gegen den Anlagenweg
> (bitgleich) und Ost/West als Zerlegung — **478,6 kWh = 554,5 kWh gemeinsames Clipping − 75,9 kWh Kennliniengewinn**. Gate
> grün: Kern 1 479 / UI 2 829, Designer „abweichend 0", SQL 0 Fundstellen, ChartProben 40/0, **Referenzlauf 1030/1007/1017
> byte-gleich**. Konzept auf Rev. 4; neu offen **W6‑O‑6** (Modultyp je Strang rechnet noch nicht, Zwilling zu W6‑O‑5) und
> **W6‑O‑7** (Referenzbasis mit Strängen: Empfehlung ein zwölftes Prüfprojekt Ost/West in der Testdatenbank, Anwenderentscheid).
> Damit sind S1, S2 und S3 des Wechselrichter-Konzepts umgesetzt; sechs Abnahmepunkte A‑W6‑E‑2‑S3 in der Sitzungsmeldung.
>
> **W6‑E‑4 (Anwenderentscheid 06.09.2026 zu „Vorlauf und Rücklauf des Katalogs rechnen nicht mit"), umgesetzt in `b55b46b`,
> zusammengeführt in `1e0ede8`:** Beim Anlegen eines Erzeugers übernimmt die Anlagenzeile das Temperaturpaar des Katalogs;
> danach bleibt es projektweise änderbar. Die Vorbelegung ist **eine Wahrheit im Kern** — `EPOS.Kern/Controller/AnlagenTemperaturen`
> mit `AusStammsatz` (Katalogsatz je `ID_Type`), `AusGeraetekopie` (Projektkopie über den Fremdschlüssel) und
> `VorlaufAusKennlinien` (die Wärmepumpe hat keine Katalogtemperaturen; ihr Katalog sind die Vorlaufstufen, die kleinste wird
> vorgeschlagen). Sie stand vorher dreimal in den Windows-Hüllen; wer ohne Hülle kam — Assistent, Import, iOS —, legte 0/0 an.
> Gerufen wird sie im **einen Schreibweg** `WizardCtrl.Add_WP_Waermeerzeuger` (nach `CopyFromStamm`, vor `AnlagenParameter`)
> und in den vier Hüllen. **Ein vorhandenes vollständiges Paar (`ProjektPuffer.IstTemperaturpaar`) wird nie überschrieben** —
> ergänzt wird nur, was fehlt; eine Bestandsanlage mit 0/0 und einer Gerätekopie mit Paar bekommt es beim nächsten Speichern
> (für den Kessel dieselbe Aussage wie die Lesekette, für BHKW und Solar neu wirksam auf `SystemVorlauf/SystemRuecklauf`
> und `AnlagenVorlauf`; in der Testdatenbank ändert sich keine Bestandszeile). Übertragen wird das PAAR, nicht das einzelne
> Feld: eine halbe Katalogangabe („90/0", 5 von 79 BHKW-Stammsätzen) kommt nicht mehr mit, weil sie über
> `SQL_SYSTEM_VORLAUF` die Systemvorgabe verzerrte. **Der WP-Rücklauf bleibt bewusst leer** — `RuecklaufVorschlaege` ist eine
> feste Liste ohne Bezug zur Vorlaufstufe, die Kennlinien führen keinen Rücklauf. Katalogeditoren schreiben das Paar in beiden
> Wegen; der veraltete Kommentar an `SolarkollektorenStammCtrl.UpdateFrom` ist berichtigt. Die Sätze „Vor- und Rücklauf des
> Katalogs rechnen nicht mit" in den Hilfeseiten BHKW und Solarthermie sind umformuliert (Vorbelegung, im Projekt änderbar,
> gerechnet wird mit der Projektzeile). Nachweis: `EPOS.Kern.Tests/AnlagenTemperaturenTests` (sechs Fälle, bis in
> `Tab_Energieanlagen`), Gate grün, Referenzlauf 1030/1007/1017 byte-gleich; Abnahmepunkte A‑W6‑E‑4‑1…4 in der Sitzungsmeldung.
>
> **W6‑O‑2 / O‑4 / O‑5 / O‑6 (Anwenderentscheide 06.09.2026: O‑2 „Empfehlung", O‑4 „Hersteller kann vom Modul verschieden sein.
> Herstellerfilter etc. wie in Modulliste einfügen", O‑5 „Modul der gewählten Zeile", O‑6 „jeder Strang mit nur einem Modultyp,
> unterschiedliche Stränge können jeweils einen anderen Modultyp haben"), umgesetzt in `35a48eb`, Papiere `df4f8c2`/`6e10dd6`,
> zusammengeführt in `c6e2d8d`:** Eine Frage — *welches Modul gilt für diesen Strang?* — an drei Stellen beantwortet. Im
> **Rechenweg** bündelt `SimulationPV` die Modulgrößen als `Modulsatz` je Modultyp (Nennleistung, Fläche, Wirkungsgrad, γ,
> NOCT, Huld-Satz), gewählt über `Z_AnlageStrang.ID_PV`, NULL/0/unbekannt → Anlagenmodul; gelesen einmal vor der Stundenschleife
> und nur, wenn eine Strangzeile ein eigenes Modul führt; kWp je Gerät ist die Summe der Stränge mit je eigener Modulleistung. In
> der **Ampel** bekommt `Pruefen` die gewählte Projektzeile (`ModulDer(zeile)` statt „erstes bekanntes Modul"), `Gaben.Module`
> trägt die Strangmodule; P1–P4 je Strang gegen sein Modul, der MPPT-Strom als Summe der Stränge mit je eigenem I_sc, P8 bleibt
> Anlagenprüfung. In der **Maske** steht die Filterzeile ÜBER der Strangtabelle wie über der Modulliste (Hersteller aus dem
> Wechselrichterkatalog, unabhängig vom Modulfilter; ein gewähltes Fremdgerät bleibt in seiner Zeile), dazu die Spalte „Modul"
> je Strang mit „(Modul der Anlage)" als Vorgabe. **O‑2** ist als Entscheid geschlossen: nur eingesetzte Geräte werden von Hand
> nachgepflegt, keine Programmarbeit. Prüfstand: `Ein_Strang_ohne_ID_PV_rechnet_mit_dem_Anlagenmodul` (bitgleich),
> `Zwei_Module_an_einem_Geraet_kosten_das_gemeinsame_Clipping` (275,19 W + 400 W an 2,0 kW, 6,7519 kWp, Zerlegung auf sechs
> Stellen; Gegenprobe mit abgeschaltetem Strangmodul fällt rot), drei Ampelfälle (425,3 V / 495,5 V, DC/AC 1,35038) und acht
> bunit-Fälle (Filter 4 → 2, „Alle", Fremdgerät bleibt, Modulspalte, `Assert.Same` auf die gewählte Zeile). Konzept Rev. 5,
> Mockup M1, `Photovoltaik.wiki` nachgezogen (Fassung 3, Wächter grün). Nachweis: Kern 1 619 / UI 2 987 grün, Designer
> „abweichend 0", SQL 0 Fundstellen, Gate grün, **Referenzlauf byte-gleich** (ohne `ID_PV` ändert sich nichts, ohne
> Strangzuordnung gar nichts). Auf Windows abzunehmen (A‑W6‑O‑456‑1…3): Anmutung der Filterzeile, Breite der Modulspalte auf
> schmalem Schirm, Zusammenspiel von Modulwahl und abgeleiteter „Anzahl Module".
>
> **W6‑O‑1 / O‑3 (Anwenderentscheide 06.09.2026: „der OND-Import soll umgesetzt werden. baue daher den Modulimport schon jetzt
> um" und „hole die Wechselrichterdaten für den Import … Liste als Datei und dann über import (aus Admin Menü)"), umgesetzt in
> `9ef8ca5`, Papiere `1d4365d`, zusammengeführt in `c7c2eb7`:** Aus `PvModulImportDialog` (771 Z.) und `WechselrichterImportDialog`
> (655 Z.) ist **EIN Wirt** geworden — `ModulImportDialog` (669 Z.) mit zwei Ausprägungen, Modul (CEC, CEC-Datei, PAN) und
> Wechselrichter (CEC, CEC-Datei, OND); Spalten, Felder, Reiter, Filter und Quellen sind DATEN (`ModulImportProfil` im Kern,
> Zwilling zu `ModulKatalogProfil`: `ModulKatalog*` pflegt, `ModulImport*` liest ein), die Komponente kennt keinen Satztyp
> (`ImportZeile` trägt Zellwerte, Detailwerte und Filtergrößen), die zwei Hüllen sind eine (`ModulImportHuelle`), beide
> Hilfeschlüssel bleiben, `help_mapping.txt` ist unverändert; die 17 Fälle des alten Modulimports laufen wortgleich im neuen
> Prüfstand (34 Fälle über beide Ausprägungen). **Neu der OND-Import** (Konzept 5.2): `OndWechselrichterDienst` liest
> PVsyst-`.OND`-Dateien (ANSI/1252, Leistungen in kW, Schwellen in W, Wirkungsgrade in %; `ProfilPIO` sind Paare P_in/P_out,
> interpoliert über P_out; bei drei Fassungen die nominale), zwei synthetische Proben (`ond_muster_2500tl.ond` mit den Zahlen
> des Konzept-Anhangs A, `ond_muster_10000tl_3profile.ond`), 20 Fälle. **Die CEC-Wechselrichterliste** liegt als
> Auslieferungsdatei `VDI-3805-Daten/PV/CEC Inverters.csv` neben `CEC Modules.csv` (NREL SAM, Abruf 06.09.2026, 2 343 Geräte,
> 152 Hersteller, Kennlinie für alle vollständig; Plausibilität 2 040 grün / 303 gelb / 0 rot) mit LIESMICH; der vom Anwender
> bestätigte Weg ist gebaut: **Administration → Datenimport → „Wechselrichter (CEC, OND)…" → „CEC-Datei laden"** (Dateiwähler
> im Unterordner PV des Herstellerdatenpfads; denselben Knopf hat jetzt auch der Modulimport). Das Setup liefert
> `VDI-3805-Daten` wie bisher nicht aus — im LIESMICH vermerkt. Nachweis: Kern 1 619 / UI 2 987 grün, Formularkarte 122,
> Designer „abweichend 0", SQL 0 Fundstellen, Gate grün, Referenzlauf byte-gleich. **Neu offen W6‑O‑8:** 303 der 2 343
> CEC-Geräte sind gelb aus EINEM Grund („Die Kennlinie fällt im Teillastast", η30 > η50 — der Scheitel der Sandia-Parabel bei
> Geräten mit hohem Wirkungsgrad, kein Datenfehler); Empfehlung: die Regel auf einen Schwellwert heben, Anwenderentscheid.
> Abnahmepunkte A‑W6‑O‑13‑1…3 in der Sitzungsmeldung.
>
> **W6‑O‑7 (Anwenderentscheid 06.09.2026: „Empfehlung"), umgesetzt in `5fa9960`/`555fa4e`, zusammengeführt in `0d5f02d`:** Die elf
> Bestandsprojekte führen keine Strangzeile, kein Referenzlauf rechnete den Strangweg der Stufe S3 mit. Projekt **1045
> „Prüfprojekt Ost/West Stränge"** (Tiefkopie von 1040: Klimaregion Stuttgart, EFH, Standardlastprofil, WP + Kessel + Puffer)
> hängt ihn ins Netz: PV-Anlage mit `PV_Wechselrichterweg = KATALOG`, Modell ERWEITERT, „Muster 2500TL" des Konzept-Anhangs A
> als Stamm- und Projektsatz (2,50 kW, zwei MPP-Tracker, 80…500 V, 600 V, 12,0 A, η 0,900…0,975, Nachtverbrauch 2 W), zwei
> Stränge zu 6 Modulen — Ost (Azimut −90) mit Ablytek 275 W, West (+90) mit **eigenem Modul** 290 W (W6‑O‑6) —, Neigung 10°.
> Kennzahlen: DC/AC 1,36 (3,39 kWp / 2,50 kW), 3 545,5 kWh (1 418 Vbh AC), Clipping 2,0 kWh (0,06 %), Jahresnutzungsgrad
> 0,9629, Nachtverbrauch 9,3 kWh in 4 669 h; Ampel P1–P8 grün. Zwei begründete Abweichungen von Anhang A: zwei Tracker (zwei
> Stränge an einem wären P4 rot, 19,1 A > 12,0 A) und 10° statt 30° (bei 30° klippt das Gerät in keiner Stunde). Befund: Solange
> P6 DC/AC bei 1,5 deckelt, bleibt das Clipping eines Ost/West-Felds klein (6+6 → 0,06 %, 6+7 → 0,50 %, 7+7 → 1,46 % bei P6 gelb).
> **Neue Basis `Referenzlaeufe/2026-09-06_R3_Straenge`** — zwölf Projekte, **312 CSV**; die elf alten **byte-gleich** zu R2
> (282 Dateien ohne Unterschied, Toleranzvergleich 11/11 PASS, 3 006 238 Werte), zweiter Lauf byte-gleich; wiederholbar über
> `Referenzlaeufe/Skripte/pruefprojekt_1045_ost_west.py`. Netz umgehängt: `kern.yml` rechnet und vergleicht 1030, 1007, 1017,
> **1045**; `ios.yml` vergleicht 1030 gegen R3; Gate-Skript ebenso; `CLAUDE.md`, `Referenzlaeufe/LIESMICH.md`, Konzept
> (S3.7, Kapitel 12, zwei überholte Sätze in Kapitel 4 und 10) nachgezogen; R2 bleibt zur Geschichte liegen. Zwei
> Prüfstandszahlen zogen mit (die Testdatenbank führt jetzt einen Wechselrichter, der Auslieferungskatalog bleibt leer).
> Nachweis: Kern 1 619 / UI 2 987 grün, SQL 0, Testdatenbankschema trocken 0/0, Gate grün gegen R3 mit vier Projekten byte-gleich.
>
> **W6‑O‑8 („Empfehlung") und W6‑O‑9 („ja"), Anwenderentscheide 06.09.2026, umgesetzt in `de5da29`/`88eeb27`/`68752a9`,
> zusammengeführt in `04b12e5`:** Die Regel „Die Kennlinie fällt im Teillastast" meldet erst ab
> `WechselrichterPlausibilitaet.TEILLAST_ABFALL_SCHWELLE = 0,01` (ein Prozentpunkt). **Der Wert ist gemessen**, nicht gesetzt:
> Über alle 2 343 Geräte der Auslieferungsdatei fällt die Kennlinie zwischen 5 und 30 % nur zweimal (größter 0,372 PP) und
> zwischen 30 und 50 % 303‑mal, davon 302 unter 0,4 PP (246 unter 0,1); zwischen 0,4 und 2,4 PP liegt **keine einzige**
> Kennlinie — die Schwelle sitzt in einer Lücke, nicht in einer Verteilung. Übrig bleibt ein Gerät (OutBack GS8048A, 2,423 PP),
> ein Tippfehler 0,79 statt 0,97 sind 18 PP. Der Meldungssatz nennt die Zahl; die Auslieferungsliste steht bei **2 342 grün /
> 1 gelb / 0 rot** statt 2 040 / 303 / 0 (Auslieferungstest festgeschrieben, vier neue Fälle). **W6‑O‑9:** `Setup/EPOS-Plan.iss`
> liefert den Ordner `VDI-3805-Daten` (186 MB, davon WP-Daten 134) als **vorgewählte, abwählbare** Komponente „Herstellerdaten
> (VDI 3805, CEC)" nach `{app}\VDI-3805-Daten` aus — neben dem Programm wie die Vorlagendatenbank, weil die Importmasken nur
> lesen, ein Update ihn ersetzt und `%ProgramData%` bei der Deinstallation absichtlich stehen bleibt (Setup-Konzept E10);
> `[Types]`/`[Components]`/`[UninstallDelete]`, `build-setup.ps1` bricht ohne den Ordner ab. `IPfade.Herstellerdaten` findet den
> Ordner ohne Einstellung (installiert neben dem Programm, im Entwicklungsstand die Repowurzel),
> `EinstellungenCtrl.HerstellerdatenpfadOderVorgabe` ordnet gespeicherter Pfad → Auslieferung → alter Vorgabeordner, beide
> Importmasken starten dort; der schreibende VDI-Pfad bleibt getrennt. Ein Setup-Bau ist auf Linux nicht möglich — die `.iss` ist
> per Skript geprüft (293 Sätze, 0 Fundstellen, Gegenprobe meldet), der Nachweis der Komponentenseite folgt beim nächsten
> Windows-Bau (Abnahmepunkt A‑W6‑O‑9‑1). Nachweis: Kern 1 644 / UI 2 998 grün, Designer „abweichend 0", SQL 0, Gate grün,
> Referenzlauf 1030/1007/1017/1045 byte-gleich. **Kapitel 12 des Wechselrichterkonzepts: alle neun Punkte geschlossen.**
>
> **W6‑E‑6 (Anwenderentscheid 07.09.2026: „der Wechselrichter soll beliebig wählbar sein und nur die Vorauswahl auf den
> Modulhersteller verweisen"), umgesetzt in `5762f32`/`363d3d0`, zusammengeführt in `83a394a`:** Der Herstellerfilter über
> der Strangtabelle (W6‑O‑4) steht beim Aufmachen auf dem Hersteller des ANLAGENmoduls, sofern der Wechselrichterkatalog
> ein Gerät dieses Herstellers führt — sonst auf „Alle"; er sperrt nichts, jeder andere Hersteller bleibt eine
> Klapplistenwahl entfernt, und eine gewählte Zeile behält ihr Gerät. **Die freie Wahl war schon gegeben** (`Filtern("Alle")`
> liefert den ganzen Katalog, `BeiGeraet` kennt keine Herstellerbedingung), nur nie als Fall belegt — jetzt wählt ein Test
> nach der Vorauswahl ein fremdes Gerät. Abgleich zweistufig (Gleichheit ohne Groß-/Kleinschreibung und Randleerzeichen,
> dann „beginnt mit" in beide Richtungen ab drei Zeichen — „S" träfe Siemens wie SMA), vorgestellt nur beim ersten Zeichnen
> und beim Wechsel des Anlagenmoduls (`_vorgestelltFuer`), damit kein Neuzeichnen dem Anwender seine Filterwahl nimmt;
> Herleitungszeile unter dem Filter nennt den Fall (drei Schlüssel `PVS_HERLEITUNG_FILTER*` in beiden `.resx`). Trefferprüfung
> in `PvStraengeFelder` gegen die Liste `Hersteller`, die Hülle reicht nur `ModulDer(zeile).Firma` — iOS hat denselben Weg.
> Nachweis: `PvStraengeFelderTests` 26 → 37, Kern 1644 / UI 3009 grün, Designer „abweichend 0", SQL 0, Gate grün,
> Referenzlauf 1030/1007/1017/1045 byte-gleich. Abnahme auf Windows: A‑W6‑E6‑1…7 (Vorauswahl mit/ohne Treffer, fremdes Gerät
> wählbar, Filter bleibt bei Zellenänderung, zweite PV-Zeile stellt neu vor, zugeordnetes fremdes Gerät bleibt sichtbar,
> „beginnt mit" am CEC-Bestand, englische Sätze).
>
> **W6‑B‑2 (Windows-Befund 07.09.2026: „der Filter bei der Herstellerauswahl funktioniert nicht"), behoben in `202b867`,
> zusammengeführt in `42b7846`:** Der Status stimmte („109 Geräte gefunden" ist exakt die Zahl der SMA-America-Geräte
> der CEC-Liste), die Tabelle zeigte ABB. **Ursache belegt** mit einer Playwright-Probe (Blazor-Server-Wirt im Scratchpad,
> reines QuickGrid 10.0.11, 2 343 Zeilen, elf Fälle, drei rot): Nicht das Virtualisieren ist schuld, sondern der WECHSEL
> des Schalters. QuickGrid trägt beide Wege in EINER Instanz; der flache Zwischenspeicher `_currentNonVirtualizedViewItems`
> wird genau einmal gefüllt — beim ersten Abruf, als das `@ref` auf das Virtualize-Kind noch `null` war, also mit der GANZEN
> Liste — und das `@ref` wird beim Entfernen des Kindes nie zurückgesetzt; jeder spätere Refresh landet im entfernten Kind.
> Fällt der Schalter unter der Schwelle 120 (2 343 → 109), zeichnet QuickGrid den uralten Stand: das Bildschirmfoto des
> Anwenders. **Fix im Standard `Raster`:** `@key` an `(Virtualisiert, Zeilenzahl)` baut das QuickGrid bei geänderter Kennung
> neu auf — kein Wirt ändert sich, alle virtualisierten Listen des Hauses haben den Fix, und die Liste steht nach einem
> Filterwechsel wieder am Anfang; der Schlüssel hängt an der Zahl, nicht an der Menge, damit das Tippen in einer bearbeitbaren
> Zelle nichts neu aufbaut. Weg (a) `@ref` + `RefreshDataAsync()` wäre wirkungslos gewesen — er landet im fehlerhaften Zweig.
> Vier Wächter (drei `RasterTests`, ein Dialogfall mit den ABB-Zeilen des Fotos), alle ohne Fix rot; Probe 3 rot → 20 grün.
> Nebenbefund erledigt: vier Spaltenköpfe trugen Feldbeschriftungen mit Doppelpunkt (`PVIMP_SP_HERSTELLER`,
> `PVIMP_SP_TECHNOLOGIE`, `PVIMP_LBL_MODULNAME`, `WRK_IMP_LBL_GERAET`). Abnahme: A‑W6‑B2‑1…5.
>
> **W6‑E‑5 (Anwenderentscheid 07.09.2026: „die Mehrfachauswahl funktioniert nicht", „die Auswahl per Doppelklick geht
> nicht", „der Mehrfachimport soll grundsätzlich für alle Importe möglich sein"), umgesetzt in `7912c3e`, zusammengeführt
> in `42b7846`:** Befund verifiziert — der Katalogimport (vier VDI-Ausprägungen) führte unter dem Kontrollkästchen die
> Semantik der ListBox (`Zeilenmarkierung.Anklicken` leerte ohne Strg die Wahl), der Geräteimport (Module, Wechselrichter)
> hatte gar keine Mehrfachwahl (Konzeptkasten „Eine Zeilenwahl, kein Mehrfachimport" — jetzt ersetzt). **Eine Klickregel im
> Baustein `Zeilenmarkierung`** statt je Wirt: Klick schaltet um, Strg dito, Umschalt nimmt den Bereich ab dem Anker dazu
> (`Hinzufuegen`); `Zeilenwahl.Doppelklick` (`@ondblclick`) übernimmt die Zeile sofort, ohne die übrige Wahl zu löschen —
> die zwei Klicks des Browsers vor dem Doppelklick heben sich auf. `ModulImportDialog` schreibt alle gewählten Sätze in
> einem Zug: Vorprüfung je Satz, EINE Rückfrage für alle Warnungen (mit Geräteliste), EIN `ImportKonflikteDialog`, Bilanz
> „n übernommen, m übersprungen"; die Wahl hängt an den Sätzen und überlebt das Umfiltern (Status „n gewählt",
> „Zurücksetzen" leert); ein einzelner Satz verhält sich wie vorher. Nachweis je Ausprägung: sechs Fälle „zwei einfache
> Klicks → beide geschrieben" (vier VDI mit ihren Proben, zwei Geräteimporte mit `cec_module_50.csv` /
> `cec_wechselrichter_21.csv`), dazu Doppelklick, Konflikt unter zweien, zwei Warnungen → eine Rückfrage, Umfiltern behält
> die Wahl. Nachweis: Kern 1672 / UI 3038 grün, Designer „abweichend 0", SQL 0, Gate grün, Referenzlauf
> 1030/1007/1017/1045 byte-gleich. Abnahme: A‑W6‑E5‑1…7.
>
> **W6‑E‑7 (Anwenderentscheid 07.09.2026, revidiert PAKET BHKW-REGULÄR Punkt 2 vom 17.08.2026: „Es soll kein Fallback
> geben, wenn 0 dann bleibt es so, oder es soll in der Einstellung sichtbar sein"), umgesetzt in `b5dae54`, zusammengeführt
> in `e039e4e`:** In `SimulationBHKW.Moduldaten_Einlesen` fällt die stille Rücklage `Leistungsgrenze 0 → 30 %`; 0 rechnet als 0
> (keine Untergrenze, das Modul moduliert bis 0 — schärfster neuer Fall: 10 kW elektrisch bei 2 kW Bedarf steht mit 30 % still
> und moduliert mit 0 auf genau 2 kW). **Migrationsschritt 67** hebt `Tab_Einstellungen.Leistungsgrenze` **NULL → 30** — nur
> NULL, eine gepflegte 0 bleibt 0 —, damit ein Bestandsprojekt ohne gepflegten Wert nicht anders rechnet; die eine Anweisung
> steht in `BhkwLeistungsgrenzeVorgabe`, aus der sich Migration, `Werkzeuge/Testdatenbankschema` und der Nachweis bedienen
> (Testdatenbank: 1007/1008/1009/1017 NULL → 30, 1039 bleibt 0, 1024 bleibt 10, Schemastand 66 → 67). **Port-Befund
> mitbehoben:** Das sichtbare Feld der Simulationskonfiguration schrieb seit dem Blazor-Port (W10a) in die tote Altspalte
> `BHKW_Grenzleistung` statt in `Leistungsgrenze` — der Vorläufer `Form_Simulation_Detail` (`numericUpDown_UnteresteLG`) tat es
> richtig; der interaktive Lauf hing damit allein am Fallback, der Stapellauf nahm den gepflegten Wert. Beide Wege lesen und
> schreiben jetzt `Leistungsgrenze`. Sichtbar: Parameterblatt „BHKW" der Simulationskonfiguration, Feld „Untere Leistungsgrenze
> der Module" in %, Herleitung „0 = keine Untergrenze, das BHKW moduliert bis 0"; `BhkwDialog` zeigt bei Modulwert 0 „0 =
> Projektvorgabe (n %)"; ein neues Projekt startet mit 30 % (`KonfigurationModel`). Der Kommentar nannte „Schritt 13" (das ist
> die Puffer-Notreserve), gemeint war der Access-Teilschritt 13b des Pakets — berichtigt; `BHKW.wiki` Gleichung (3) neu gefasst
> (Wächter: gemeinsamer Stand aller 13 Seiten, `\wedge` nicht im Befehlsvorrat → `\text{und}`). Nachweis: 12 Kern-, 6 UI-Fälle,
> Kern 1684 / UI 3044 grün, Designer „abweichend 0", SQL 0, Gate grün, Referenzlauf 1030/1007/1017/1045 byte-gleich, alle
> zwölf Projekte 312/312 CSV byte-gleich. Abnahme auf Windows: A‑W6‑E7‑1…7 — darunter A‑W6‑E7‑2 (Wert ändern und simulieren
> bewegt das Ergebnis; vor der Behebung nicht) und A‑W6‑E7‑7 (`.wpx`-Pakete auf Stand 66 werden abgewiesen, beide Rechner
> müssen auf 67).
>
> **W6‑B‑3 (Windows-Abnahme 07.09.2026: „Die gesamte Zuordnung Wechselrichter zum PV-Modul und Strang funktioniert nicht —
> Auswahl Wechselrichter nicht vorhanden"), behoben in `70fb00e`/`f52b30e`, zusammengeführt in `29bd5c5`:** Die Ursache
> war die **weiche Sperre W16b‑E‑6 auf der Option „mit Wechselrichter"** in `PvStraengeFelder`: Sie verlangte einen
> Strang, bevor sie den Weg freigab — angelegt wird ein Strang aber nur INNERHALB dieses Weges; eine frische Anlage kam
> nie hinein. Belegt mit bunit in der Lage des Anwenders (Klick auf die Option wird verweigert, gerendert bleibt der
> Rückfallzweig „Die Anlage rechnet mit dem Wirkungsgrad 0,950 …" mit EINEM Knopf, der die Pauschalen öffnet — Zeichen
> für Zeichen das Bildschirmfoto; der Browser lässt das Kästchen gewählt aussehen, weil Blazor bei unveränderter Auswahl
> nichts zurückstellt) und hüllennah gegen die Testdatenbank (die Hülle liefert Hersteller, Geräte und Modulhersteller —
> H4 widerlegt). Die Sperre fällt ersatzlos (`PVS_SPERRE_OHNE_STRANG` gelöscht); bei „mit Wechselrichter" stehen jetzt
> **immer** Herstellerfilter, die Klappliste „Wechselrichter aus dem Katalog" und „Strang anlegen" (das gewählte Gerät
> geht über `CopyFromStamm` in den neuen Strang), bei leerem Katalog der Importweg „Administration → Daten & Import →
> Photovoltaik → Wechselrichter (CEC, OND)…" statt eines stummen Abschnitts; der Rückfall heißt „Anlagenwerte
> (Rückfall)…" und steht getrennt am Fuß, auch die Überlagerung trägt den Namen. Der Titel „Verwaltung Photovoltaik
> Module" bleibt — historischer Titel von `Form_PV` und Hausschema („Verwaltung Heizkessel"). `PhotovoltaikDialog.razor`
> und `PhotovoltaikHuelle.cs` sind unberührt (kein Konflikt mit S2). Nachweis: 14 neue Fälle (`PvStraengeFelderTests`
> 38 → 45, `PvWechselrichterZuordnungTests` 5), Kern 1930 / UI 3202 grün, Gate grün, Referenzlauf byte-gleich gegen R5;
> Konzept Kapitel 7/7.1/12. Abnahme auf Windows: A‑W6‑B3‑1…10.
>
> **W6‑B‑2‑O‑1 erledigt (07.09.2026, `586d8b5`, zusammengeführt in `cd5deb0`) — der flatterhafte Herstellerfilter-Test.**
> Der Prüffall `ModulImportDialogTests.Der_Herstellerfilter_zeigt_nur_noch_die_Zeilen_des_Herstellers` fiel im Kern-Lauf
> **216** (Ereignis `pull_request` des Anwender-PR #1 auf `833ff69`) als einziger von 3 202 aus („Expected 5, Actual
> 155"), während Lauf **215** auf demselben Commit grün war. Ursache, am wörtlichen Prüfstand gemessen: **bunits
> synchrone Ereignisse warten nicht** — `Click()`, `Change()`, `Input()` geben das Ereignis nur beim Zeichner ab, und der
> `RendererSynchronizationContextDispatcher` führt es nur dann auf dem Prüffaden aus, wenn seine Warteschlange frei ist;
> nach dem Laden von 155 Zeilen liegt dort das `OnAfterRenderAsync` von QuickGrid/`Virtualize` (Schalterwechsel bei
> `VIRTUALISIEREN_AB` = 120), das Ereignis wird eingereiht und der Fall liest den Stand davor. Die Verdachtshypothese
> „eine späte Fortsetzung setzt den Filter zurück" ist widerlegt: `ListenAufbauen()` läuft vor `Filtern()` im selben
> synchronen Zug. **Behebung nur am Test** (Muster W16b‑O‑2, kein Produktcode): Helfer, die auf den GEZEICHNETEN Stand
> warten — `Geladen`/`Gefiltert`/`Gemeldet`/`Ueberlagert` in `ModulImportDialogTests`,
> `Einlesen(cut, n)`/`Gezeichnet`/`Markiert`/`Gemeldet` in `KatalogImportDialogTests`; beide Klassen vollständig
> nachgezogen. Messung: unter Rechenlast (8 Fäden auf 4 Kernen) 68–95 von 400 rot, mit nachgebender Quelle 58 von 60 —
> nach dem Fix 0 von 400 und 0 von 60; 30/30 in `de` und `en_US.UTF-8`; `EPOS.UI.Tests` 3202 grün in beiden Kulturen,
> Kern 1930 unberührt, Warnungen unverändert 6. Gate reduziert auf Bau und UI-Tests (nur Test- und Doku-Dateien im
> Merge). **Hausregel seither (Verschärfung von W16b‑O‑2):** Nach einem synchronen bunit-Ereignis wird auf den
> gezeichneten Zustand gewartet, nicht sofort geprüft — gilt für `Click()`, `Change()`, `Input()`, `DoubleClick()`
> gleichermaßen (Protokoll `iU9_W15a_Blazor_Port_Protokoll.md`, Abschnitt W6‑B‑2‑O‑1).
>
> **W6‑B‑4 (Windows-Abnahme 07.09.2026: „Anzahl Module fehlt? Welche Werte?"), behoben in `9aa6970` (D) · `f2676b0` (C) ·
> `945a8a7` (A und B) · `937f631` (Doku), zusammengeführt in `cd6d529`.** Vier Beobachtungen an EINEM Bildschirmfoto des
> PV-Dialogs „Wechselrichter und Stränge". **(A)** Die Strangtabelle war bei 1 100 px Fensterbreite **1 974 px** breit
> gegen 1 046 px sichtbar — sechs der elf Spalten (Gerät, MPPT, Module in Reihe, Stränge parallel, Neigung, Azimut)
> standen daneben: die zwei `<select>` so breit wie ihr längster Eintrag (302 px), die Zahlenfelder so breit wie ein
> `<input>` von Haus aus (je 196 px), die Köpfe ohne Umbruch. Behoben durch die Spaltenfolge „alles Schmale zuerst, die
> zwei elastischen Listen zuletzt" und drei Regeln im Hausblatt (Deckel 11 rem + `text-overflow: ellipsis` mit vollem
> Namen im `title`, 4 rem auf den Zahlenfeldern, `th.klasse, td.klasse` für den Kopfumbruch — eine bloße Klasse verliert
> gegen `.epos-raster th` (0,1,1)). **Nachher 1 046 px, `scrollWidth == clientWidth`** (Playwright-Probe, bunit misst
> keine Breite — Lehre W6‑B‑1). **(B)** Ein neuer Strang begann mit „0 Module in Reihe"; die Vorbelegung rechnet
> seither der Kern (`Strangvorbelegung.FuerNeuenStrang`): erster Strang = Modulzahl der Anlage, jeder weitere = der
> Rest (mindestens 1), parallel 1, Gerät 1, MPPT = nächster freier Tracker (`WechselrichterStammCtrl.TrackerZahl`,
> `null` = 1); nur beim Anlegen, nie beim Laden — Projekt 1045 bleibt byte-gleich. **(C)** Die Meldung nannte ein Paar
> mit „oder", obwohl `StrangPlausibilitaet` jeden Wert einzeln abfragt, und klagte über Modulwerte, wenn nur „Module in
> Reihe" fehlte (`SpannungReihe` gibt auch bei `reihe = 0` `null`). Neu: `Fehlliste` — je Wert eine Meldung, jeder
> höchstens einmal, dahinter EIN Satz mit dem Pflegeweg (`PVS_PFLEGEWEG`: PV Module → Bearbeiten, Felder alpha_SC,
> beta_OC, T_NOCT, oder Neuimport aus „CEC Modules.csv"), nur bei einem echten Modulwert; sechs neue Texte beider
> Sprachen, kein bestehender geändert; die Parameterübersicht schreibt „– nicht gepflegt" statt nur „–". **(D)** Die
> Klappliste zeigte einen Namen, den die Zeile nicht trug: Ein `<select>` hat im DOM kein `value`-Attribut, Blazor
> setzt `element.value` **nur beim Erzeugen** nach — ein späterer Austausch der Einträge lässt die Wahl unter einem
> fremden Eintrag stehen (gemessen: Strang mit „SMA America: SB30-1SP-US-40 {240V}" zeigte nach einem Filterwechsel
> „ABB: PVI-3.0-OUTD-S-US-A {208V}"). Der Standard `Auswahlfeld` gibt jeder `<option>` seither `selected` **und** ein
> `@key`; beides zusammen ist nötig (mit `selected` allein fiel der Rückweg „ABB → Alle" auf „(kein Gerät)"). **bunit
> sieht das nicht** (Htmlizer schreibt `selected` aus dem `value` heraus) — der Beleg ist die Playwright-Probe.
> Nachweis: 12 neue UI-Fälle (`PvStraengeFelderTests` 45 → 55, `StilblattTests` +2) und 27 neue Kernfälle
> (`StrangvorbelegungTests` 17, `StrangPlausibilitaetTests` 7, `PvModulparameterTests` 2), Kern 1957 / UI 3214 grün,
> Warnungen unverändert 6, SQL 0, Designer „abweichend 0", ChartProben 44, Formularkarte 122, Gate grün, Referenzlauf
> 1030/1007/1017/1045 byte-gleich gegen R5; Konzept Kapitel 7/12, zwei Hausregeln in `EPOS.UI/CLAUDE.md` (Klapplisten in
> Tabellen mit Deckel; jede `<option>` mit `selected` und `@key`). Die drei alten Paartexte `PVS_FEHLT_UOC/_UMPP/_ISC`
> stehen ungenutzt und fallen in einem Aufräumlauf. Abnahme auf Windows: **A‑W6‑B4‑1…9**. Der Datenbefund dahinter —
> die verdorbenen Koeffizienten des Bestands (A1) — läuft als **W6‑B‑5** (Schemaschritt 69).
>
> **W6‑B‑5 — die verdorbenen PV‑Modulkoeffizienten (Anwenderentscheid 07.09.2026, Q1–Q3 = Empfehlung), umgesetzt in
> `651894f` (Schritt 69 + 35 Tests) · `15defad` (Testdatenbank) · `373447a` (Basis R6, `kern.yml`/`ios.yml`, LIESMICH) ·
> `17f2bfb` (Doku, Einfrierregel) · `ace49bc`, zusammengeführt in `9489d85`.** Paket‑A‑Befund A1 ist geschlossen:
> `alpha_SC`, `beta_OC` und `T_NOCT` trugen in 3 von 6 Stammsätzen und 7 von 9 Projektkopien der Testdatenbank den Wert
> von `I_Kurzschluss` (der Kopierfehler des alten Editors), ein Satz Nullen. Migrationsschritt **69**
> (`EPOS.Kern/Allgemein/Update/PvKoeffizientenReparatur.cs`, `SchemaStand.Zielversion` 68 → 69) erkennt die
> **Giftsignatur** (Wert = `I_Kurzschluss` auf 1e‑6 genau oder außerhalb des physikalischen Fensters; die 0 liegt in
> jedem der vier Fenster außerhalb), repariert **aus der CEC‑Liste** über `CECDataService` — die vier ausgelieferten
> Module eingebettet, weil `VDI-3805-Daten` seit W6‑O‑9 abwählbar ist; liegt die Datei am Herstellerdatenpfad, kommt sie
> dazu (gemessen 4 + 20 197) —, nimmt die Projektkopien `Tab_PV` mit (**Q2**) und setzt alles ohne Treffer auf `NULL`
> mit einer Protokollzeile je Satz (Jinkosolar JKM 260P‑60 und LG 320 N1K‑A5 stehen nicht in der Liste, nur
> Schwesterzeilen eines anderen Prüflabors); idempotent, gesunde Werte bleiben Satz für Satz unberührt. Testdatenbank
> auf Stand 69: 4 von 6 Katalog‑ und 7 von 9 Projektsätzen geändert, STRICT 117, Größe unverändert. **Q3 eingelöst:**
> neue Basis **`Referenzlaeufe/2026-09-07_R6_PvKoeffizienten`** (12 Projekte, 312 CSV, 1 792 Skalare) — elf Projekte
> byte‑gleich zu R5, nur **1007** weicht ab (acht Dateien der PV‑Kette, theoretische PV‑Erzeugung −0,69 %, Überschuss
> −2,2 %), Ursache allein `T_NOCT` (Rückfall 45 °C → Katalogwert 47,4 °C; +2,4 K Zelltemperatur bei 800 W/m² mit
> γ = −0,4509 %/K); `alpha_SC`/`beta_OC` liest kein Rechenweg. Gegenbeweis im `protokoll.txt`: `T_NOCT` allein zurück
> auf 45 → 1007 byte‑gleich zu R5. 1040 (Jinkosolar, `NULL` → Rückfall wie vorher) und 1045 (gesund) byte‑gleich.
> `kern.yml`/`ios.yml`/`gate.sh` zeigen auf R6, R5 bleibt zur Geschichte liegen. **Zweite Einfrierregel** (analog
> Em‑9.8‑Q4): Wer einen Modulkoeffizienten der Testdatenbank ändert, friert die Basis neu ein (`CLAUDE.md`,
> `Referenzlaeufe/LIESMICH.md`). Die Handläufe unter `sql/pv_katalog/` bleiben als Beleg liegen (neue `LIESMICH.md`
> dort: der eine gewollte Unterschied — das Skript trug PAN‑Werte für Jinkosolar/LG ein, Schritt 69 setzt `NULL`, weil
> Q1 die CEC‑Liste als Quelle nennt). Nachweis: 35 Fälle in `PvKoeffizientenReparaturTests`, Kern 1992 / UI 3214
> grün, SpeicherEngine 337, KiKern 469, Formularkarte 122, SQL 0, Designer 0, ChartProben 44, Gate grün, Referenzlauf
> 1030/1007/1017/1045 byte‑gleich gegen R6. Abnahme auf Windows: **A‑W6‑B5‑1…9** — **Update-Hinweis: Schemastand 69,
> die Produktivdatenbank wird beim nächsten Start mit Sicherung migriert, ein `.wpx` auf Stand 68 wird abgewiesen.**
>
> **Testdatenbank auf Schemastand 72 (Aufgabe #154, 09.09.2026, umgesetzt in `c176f12`, zusammengeführt in `91137e1`).**
> Der Befund aus dem Statusblock O‑14: `SchemaStand.Zielversion` stand seit den Schritten 70 (PV-Strangprüfung, W6‑B‑10/11),
> 71 (zwölf Szenario-Spalten, W5‑B‑9) und 72 (p_I und Freitext, W5‑B‑12) auf 72, `Kenndaten_Test.sqlite` aber auf 69 —
> der SQL-Dialektprüfer meldete fünf Fundstellen, und `kern.yml` war seit Lauf 249 rot. Nachgezogen nach dem Muster von
> Schritt 69 (#150) über `Werkzeuge/Testdatenbankschema`: 20 nullbare Spalten (`I_Sc_Max` an `Tab_Wechselrichter[_STAMM]`,
> `Ausleg_T_Kalt`/`Ausleg_T_Heiss` an `Tab_Einstellungen`, die zwölf `Szen_Best_*`/`Szen_Worst_*` sowie
> `Preissteigerung_Investition`, `Szen_Best_Preis_I`, `Szen_Worst_Preis_I`, `Nicht_Monetaer` an
> `Tab_ProjektWirtschaftlichkeit`); der zweite Lauf meldet 0 Spalten (idempotent). Eine Fundstelle blieb danach:
> `Tab_ErgebnisWirtschaftlichkeit.ErsatzBarwert` — diese Tabelle führt `WirtschaftlichkeitCtrl.StelleTabellenSicher`
> seit jeher selbst nach („doppelte Schema-Wahrheit"), das Werkzeug erreicht sie nicht; die Spalte ist einzeln als `REAL`
> nachgetragen, ohne `StelleTabellenSicher` zu rufen, weil das den Gesetzeskatalog anstösst (Hinweis in
> `BETRIEB_SQLITE.md` § 6.5). Nachweis: SQL-Dialektprüfer 5 → **0** Fundstellen (1 304 Texte, Selbsttest 32/32); STRICT
> 117 und 70 012 928 Byte unverändert (NULL-Spalten brauchen keine Seite); im Vollvergleich aller 118 Tabellen ist
> `Tab_Applikation.SchemaVersion` 69 → 72 der einzige Wertunterschied; **Referenzlauf aller zwölf Projekte der Basis R6
> byte-gleich** (`diff -rq` 12/12, Toleranzvergleich 12/12) — keine neue Basis, R6 bleibt; Kern 2194 / UI 3362 grün.
> `EPOS.Kern/CLAUDE.md` nennt die `Zielversion` seither mit 72. Abnahme: Ein Projektexport (`.wpx`) mit Schemastand 71
> oder älter wird beim Import abgewiesen — das gilt seit den Schritten 70–72 und ist hier nur nachgezogen.

## Statusblock iU9 — Welle 5 umgesetzt (03.09.2026, Basis 740c73e)

> **Statusblock iU9 — Welle 5 umgesetzt (03.09.2026, Basis `740c73e`)**
>
> Die Welle stammt aus dem Wellenplan iU9, Abschnitt C Zeile W5: **sechs Masken →
> sechs Razor-Komponenten**, jede WinForms-Fassung gelöscht (Regel M1). Es ist die
> **erste Welle mit Seiten statt Dialogen** — der ganze Reiter „Berichte & Kosten"
> der Startmaske ist jetzt Blazor, in **einer** WebView. Zehn Commits:
>
> | Commit | Inhalt |
> |---|---|
> | `d95283c` | **W5.0** Bausteinlücken 9–11: `Allgemein/Blazor/BlazorSeite.cs` (nicht-modale Hülle), `EPOS.UI/Dienste/SeitenZustand.cs` (Projektwechsel ohne Neuaufbau der WebView), `Bausteine/Reiter.razor` + `Reiterblatt.razor`, `Kachelraster.razor`, `Kennzahlkachel.razor`; dazu der Nachzug von **A‑17 (W3)** und **A‑2 (W4)** — Kostenprofil, Kostenverwaltung und Trägerkarte bekommen ihre Reiterform zurück |
> | `a39fe13` | **W5.1** `Form_BkUebernahme` → `Dialoge/Berichte/BkUebernahmeDialog.razor` (ein Dialog, zwei Füllungen — Wertgegenüberstellung oder Klartext) |
> | `cd4213d` | **W5.2** `UcBericht` (508 Z.) → `Seiten/Berichte/BerichtSeite.razor` |
> | `bf38fa6` | **W5.3** `UcWirtschaftlichkeit` (831 Z.) → `Seiten/Berichte/WirtschaftlichkeitSeite.razor`; die **fünf Unterdialoge** stehen jetzt in Überlagerungen desselben Fensters (W4‑O3 erledigt) |
> | `47ea9e3` | **W5.4** `UcBkKosten` (1 311 Z., K4) → `Seiten/Berichte/KostenSeite.razor` |
> | `8ea1e2e` | **W5.5** `UcBkUebersicht` (1 552 Z., K4) → `Seiten/Berichte/UebersichtSeite.razor` |
> | `f59aed1` | **W5.6a** Sieben Hüllen liefern ihren Parametersatz (`Gaben`) — Voraussetzung dafür, dass ein Blazor-**Wirt** seine Unterdialoge ohne zweite WebView zeigt |
> | `ff4e6f7` | **W5.6** `UcBerichteKosten` (810 Z., K4) → `Seiten/Berichte/BerichteKostenSeite.razor`; `Form_Start.tabPage6` trägt eine `BlazorSeite<T>`; sechs Masken gelöscht, fünf Windows-Datenseiten neu |
> | `f5d660f` | **W5.7** Ressourcen-Sammelnachtrag: 34 Schlüssel (`BKS_*`, `WIRT_*`) in `Resource.resx` und `Resource.en-US.resx`; `help_mapping.txt` |
> | `f39b4a3` | **W5.8** Formularkarte: Zähler 91 → 88, achtes Prüfmuster (`UcBericht` — einziger Beleg für die `CheckedListBox`) |
>
> **Die Seiten-Hülle ist der Ertrag.** `BlazorDialogForm<T>` ist ein eigenes modales
> Fenster; eine SEITE sitzt in einer vorhandenen Maske und bleibt. `BlazorSeite<T>`
> ist deshalb ein `UserControl` mit denselben `CreationProperties` — insbesondere
> demselben `UserDataFolder`, also **einem gemeinsamen Browserprozess**. Die vier
> Seiten laufen in **einer** WebView (Risiko **R5**); umgeschaltet wird in der
> Komponente. Der Projektwechsel läuft über `SeitenZustand`: ein Objekt mit
> Änderungsereignis, damit die WebView **nicht** neu gebaut wird.
>
> **DPI bleibt offen (Risiko R4, Entscheid iF21).** Die `DpiInsel` der Dialoghülle
> wirkt nur für einen modalen Lauf mit eigenem Fenster. Eine eingebettete Seite
> sitzt im Fenster der DpiUnaware-`Form_Start` und wird bei 125–200 % bitmapskaliert;
> ein Fenster kann seinen DPI-Kontext nachträglich nicht wechseln. `BlazorSeite`
> versucht es deshalb **gar nicht erst**, dokumentiert den Befund und setzt
> `DefaultBackgroundColor` gegen das weiße Aufblitzen. **Die Schärfe der Seiten ist
> damit ein Abnahmepunkt, keine Zusage** — und der eigentliche Entscheid der Welle
> (W5‑O1).
>
> **H11 entfällt.** Die 110 Zeilen Messcode, mit denen `UcBerichteKosten` den
> Infoknopf jeder eingebetteten Seite von der Kopfzeile abrückte, sind ersatzlos
> weg: Die Kopfzeile trägt den Knopf des Behälters, jede Seite ihren eigenen im
> Fluss ihres Inhalts.
>
> **Kein neuer Kern-Controller.** Alle vier Seiten riefen schon vorher ausschließlich
> Kern-Controller (Hausmuster Ä9); die vier SQL-Anweisungen der Kostenseite sind
> wortgleich in die Windows-Datenseite gewandert.
>
> **Nachweise.** `dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental`
> → 0 Fehler, **20** Warnungen (Basis 22; WFO1000 16 → 14) · `dotnet test
> WP-Plan.Kern.slnf` → **1 485** grün (1 352 vorher; 133 neue bunit-Tests) ·
> Formularkarte **123** grün · Stapellauf **88** Masken, 0 × „nein", 0 × „verwaist" ·
> SQL-Prüfer 1 301 Texte, 0 Fundstellen · ChartProben 10 Bilder, 0 Verstöße ·
> Referenzlauf 1030/1007/1017 gegen `2026-08-30_B3-Kaskade` **PASS/PASS/PASS**,
> `diff -rq` ohne Unterschied · `dotnet publish` mit vollständigem `wwwroot`.
>
> **Protokoll** mit Feldkartenabgleich (6 Masken), 18 Abweichungen (A‑1…A‑18),
> Windows-Abnahmeliste mit 25 Punkten und acht offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W5_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus** — alles Obige ist auf Linux gemessen.
> **Windows-Abnahme 04.09.2026, erster Befund (Kosten-Seite):** die Aktionsspalte der Anlagentabelle war unsichtbar
> (`display:flex` direkt am `<td>` nahm der Zelle ihre Tabellenrolle, der Spaltenkopf war leer — W5‑B‑1) und der
> Doppelklick auf der losen Position fehlte. **W5‑O3 entschieden: der Doppelklick ist als zweiter Weg zurück, der
> Knopf bleibt**; Aktionsspalte als gewöhnliche Zelle mit beschriftetem Kopf (`5f153f1`, sechs bunit-Fälle, darunter eine
> Wache auf das Stilblatt; Regel in `EPOS.UI/CLAUDE.md`: kein `display:flex` auf `<td>`/`<th>`, Aktionsknöpfe ohne
> Hover sichtbar). Die Sichtprüfung in der WebView2 bleibt beim Anwender.
> **Anwenderwunsch W5‑E‑1 vom 05.09.2026 („Variantenprojekte-Auswahl als Dropdown, damit weniger Platz verwendet
> wird"), umgesetzt in `06332f2`:** Die Variantenwahl der Übersichtsseite ist ein `Auswahlfeld` „Variante:" (Stamm
> zuerst, dann „Bezeichner — Projektname", Id = `Tab_Projekt.ID`) statt einer Tabelle; Bezeichnerfeld und die drei
> Knöpfe stehen mit ihm in einer Zeile (Umbruch auf schmalem Fenster), der Simulationsstand darunter als leise
> `aria-live`-Zeile „Simulation: ‹Datum›" bzw. „noch nicht simuliert" mit dem ⚠ als eigenem Element und dem Grund
> im Kurztext (`BerichtsDatenSammler.ErmittleStatus`: kein Ergebnis oder Ergebnis älter als das Änderungsdatum), und
> die Unterschiedstabelle bekommt die frei gewordene Höhe (`epos-raster-huelle--vergleich`, 35,2 rem; Hausregel
> W9‑B‑2 bleibt). Neu dafür `VarianteZeile.SimZeitpunkt` und `Auswahlfeld.Kurzname`; die Parameter
> `SpalteArt`/`SpalteBezeichner`/`SpalteProjektname` entfallen. Protokoll Abschnitt 13, zehn neue bunit-Fälle.
>
> **Formularraster, Paket P2 (iU8‑E‑2, 05.09.2026, `ac6be91`):** Seiten tragen ihre Felder in Werkzeugzeilen und Tabellenspalten; genau eine Stelle — das
> Pfadfeld des Zielordners der `BerichtSeite` — ist ein Formularfeld und steht einspaltig im Raster. `UebersichtSeite`
> und `WirtschaftlichkeitSeite` bleiben unverändert und sind zugleich die Gegenprobe, dass die Regel nur innerhalb
> `.epos-formularraster` greift. Paket P2: 16 Dateien, UI 2 562 (+16), Formularkarte 122.
>
> **Anwenderbefund W5‑E‑2 vom 05.09.2026 („Gewerk Anlage gibt es nicht. Dort stehen Parameter. Dargestellt werden nur
> die Erzeugerkomponenten, die verwendet werden, keine Parameter"), umgesetzt in `7dcda25`:** Die Gegenüberstellung der
> Seite „Übersicht" lief über `AbweichungsErmittler.Felder` und nahm damit die Blöcke „Anlage" und „Gebäude" mit —
> Konfigurationsblöcke ohne Komponentenbestand; im Projekt des Bildschirmfotos (1042 „Booster-Kette mit
> Kombi-Speicher" mit Variante 1044 „Schichtspeicher") waren das 21 Anlagen- und 4 Gebäudemerkmale über 10
> Komponentenzeilen. Das Vorbild ist nachgesehen: Die gelöschte Maske `UcBkUebersicht` zeigte den Block ebenfalls (der
> Wächter `AnlagenEinheitlich` greift nur bei verschiedenen Anlagengewerken), schon in ihrer ersten Fassung — das
> wirkliche Vorbild ist der Berichtsbaustein `BausteineProjekt`, der seit jeher allein über
> `ProjektDetails.GewerkTabellen` zählt. Die Zeilenbildung zieht deshalb in den Kern: `Allgemein/Bericht/
> KomponentenVergleich.cs` mit dem anzeigefreien `KomponentenVergleichZeile` liefert je verwendetem Erzeugergewerk eine
> Kopfzeile „Anzahl Komponenten" und darunter eine Zeile je Komponente; ein Gewerk mit Stückzahl 0 in allen Versionen
> erscheint gar nicht. `UebersichtSeiteGaben.FuelleVergleich` schrumpft von 88 auf 10 Zeilen und bildet nur noch ab.
> Die Unterschiedsansicht einer Variante bleibt vollständig — dort zeigt eine Zeile eine Änderung und trägt die
> Merkmalsübernahme; `AbweichungsErmittler` ist nicht angefasst, der Referenzlauf unberührt. Nachgezogen ist ein Text
> (`BK_MSG_VERGLEICH_UMFANG`: „Komponentenzeile(n)" statt „Merkmalszeile(n)", de/en). Nachweis:
> `KomponentenVergleichTests` (7 Fälle) und ein bunit-Fall in `UebersichtSeiteTests`; Kern 1 197 und UI 2 649 grün unter
> de und en, SQL-Prüfer 0 Fundstellen, Kern-Wächter leer. **W5‑O‑4 — Anwenderentscheid 05.09.2026: „soll bleiben".**
> Die Unterschiedsansicht einer Variante zeigt weiterhin alle Abweichungen einschließlich der Anlagen- und
> Gebäudeparameter, weil eine Zeile dort eine tatsächliche Änderung ist und die Übernahme (z. B. einer geänderten
> Vorlauftemperatur) daran hängt. Acht Abnahmepunkte A‑W5‑E‑2 im W5-Protokoll.

## Statusblock iU9 — Welle 4 umgesetzt (03.09.2026, Basis ae1af82)

> **Statusblock iU9 — Welle 4 umgesetzt (03.09.2026, Basis `ae1af82`)**
>
> Die Welle stammt aus dem Wellenplan iU9, Abschnitt C Zeile W4: **sieben Masken →
> sieben Razor-Komponenten**, jede WinForms-Fassung im selben Schritt gelöscht (Regel M1).
> Es ist die größte Welle bisher — die beiden **Hosts** der Kostenseite fallen mit ihren
> fünf Unterbausteinen auf einmal, zusammen 5 216 Zeilen WinForms. Acht Commits, einer je
> Nummer:
>
> | Commit | Inhalt |
> |---|---|
> | `6c3cbc5` | **W4.0** Bausteinlücken 6–8: `Bausteine/Ueberlagerung.razor` (modaler Bereich IN der Komponente, Fokusfalle ohne JS), `Bausteine/Rueckfrage.razor` (Ja/Nein/Abbrechen), `Bausteine/Zeilenraster.razor` (Spaltenkopf, Zeilen, Abschlusszeile, Summenfuß); dazu der Nachzug von **A‑10 aus Welle 3** — die Untereditoren des Emissionskatalogs stehen jetzt in einer Überlagerung |
> | `3db98e0` | **W4.1** `ucVorlagenZeile` → `Dialoge/Kosten/VorlagenZeile.razor`, `ucErtragBonus` → `Dialoge/Kosten/ErtragBonus.razor` mit `ErtragBonusGaben`; erster Aufrufer der Sprungbrücke aus W2.2 (W2‑O6 erledigt) |
> | `e0b63be` | **W4.2** `Form_KostenKomponente` (918 Z.) → `Dialoge/Kosten/KostenKomponenteDialog.razor` + `KostenKomponenteHuelle`; die **fünf Unterdialoge der Welle 1** stehen jetzt in Überlagerungen desselben Fensters statt in je einer zweiten WebView (R2) |
> | `4527d66` | **W4.3** `ucStromAufschlaege` und `ucBrennstoffBestandteile` → `Dialoge/Kosten/StromAufschlaege.razor` und `BrennstoffBestandteile.razor`; Summen, Restzeilen und Schnellwahlsätze kommen als fertiger Text aus der Hülle |
> | `b43e8fd` | **W4.4** `Form_Energietraeger` (535 Z.) und `ucFuelSettings` (2 103 Z.) → `Dialoge/Kosten/EnergietraegerDialog.razor` + `EnergietraegerEinstellungen.razor` + `EnergietraegerHuelle`; **neu im Kern:** `EnergietraegerPreisCtrl` mit den neun SQL-Anweisungen der Maske; neuer Baustein `Mehrfachauswahl` (Bausteinlücke 11) |
> | `09ecd37` | **W4.5** Ressourcen-Sammelnachtrag: 50 Schlüssel (`KKOMP_*`, `ETV_*`, `KDLG_EM_*`, `KDLG_ANLAGE_*`) in `Resource.resx`, `Resource.en-US.resx` und — von Hand — `Resource.Designer.cs` |
> | `45246be` | **W4.6** Formularkarte-Tests: sechstes und siebtes Prüfmuster (`Form_KostenKomponente`, `ucVorlagenZeile`), Anker auf `Form_Heizkessel` umgehängt, Stapellauf-Zähler 98 → 91 |
> | *dieses Paket* | **W4.7** Protokoll, Statusblock, `CLAUDE.md` ×2 |
>
> **Die Überlagerung ist der eigentliche Gewinn.** Bis Welle 3 wich jeder Blazor-Dialog,
> der einen zweiten braucht, aus — der Kostenfaktor-Katalog legt inline an (W1.5, A‑13),
> der Emissionskatalog zeigt seine Untereditoren als eingerückte Blöcke (W3.3, A‑10).
> Grund war immer Risiko **R2**: ein zweites Fenster hieße eine zweite `BlazorWebView`.
> Seit W4.0 gibt es dafür einen Baustein, und **neun Unterdialoge** stehen im selben
> Fenster wie ihr Wirt: Worst/Best, Zeileneditor, Namensabfrage, Übernahme,
> Kostenfaktor-Katalog, Kostenprofil, Spotpreis-Import, saisonale Sätze und der
> Emissionskatalog. Die sechs Hüllen der Wellen 1 bis 3 liefern dafür statt eines
> Fensters ihren **Parametersatz** (`Gaben`). Auf iOS ist diese Bauform ohnehin die
> einzige (iL5).
>
> **Neun SQL-Anweisungen gehen in den Kern.** `ucFuelSettings` las und schrieb selbst;
> `EPOS.Kern/Controller/EnergietraegerPreisCtrl.cs` trägt sie wortgleich — dieselben
> Spalten, dieselbe Rundung, dieselbe Reihenfolge. Zwei Änderungen an der Bauform: Der
> `dynamic`-Rückgabewert ist ein benannter Typ geworden, und die eine
> `RecordSet`-Abfrage mit Zeichenkettenverkettung hat einen Parameter bekommen — sie ist
> damit erstmals für den SQL-Dialektprüfer sichtbar.
>
> **`Views/Kosten` führt keine Designer-Maske mehr.** Mit den sieben Masken fallen die
> zwei nutzerlos gewordenen Karten-Controls `EinstiegsKarte` und `SectionPanel`; ihre
> Nachfolger heißen `Kachel` und `Gruppenkopf`. Der Stapellauf-Test des Werkzeugs läuft
> deshalb über `Views/Heizkessel`.
>
> **Nachweise.** `dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental` →
> 0 Fehler, **22** Warnungen (Basis 26; WFO1000 20 → 16) · `dotnet test WP-Plan.Kern.slnf` →
> **1 352** grün (1 217 vorher; 135 neue bunit-Tests) · Formularkarte **122** grün, Build
> 0/0 · Stapellauf **91** Masken, 0 × „nein", 0 × „verwaist" · SQL-Prüfer 1 301 Texte,
> 0 Fundstellen · ChartProben 10 Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017 gegen
> `2026-08-30_B3-Kaskade` **PASS/PASS/PASS**, `diff -rq` ohne Unterschied ·
> `dotnet publish` mit vollständigem `wwwroot`.
>
> **Protokoll** mit Feldkartenabgleich (7 Masken, alle vollständig, dazu 28
> Laufzeitfelder), 20 Abweichungen (A‑1…A‑20), Windows-Abnahmeliste mit achtzehn
> fachlichen Proben und acht offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W4_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus** — alles Obige ist auf Linux gemessen.
> **Windows-Abnahme 04.09.2026, Anwenderwunsch W4‑E‑1 (Energieträgerverwaltung):** Suche und Filter in der
> Trägerauswahl wie in den Importdialogen — Filterfeld über der Liste mit demselben Kernbaustein `VdiAuswahlFilter`
> (Teilzeichenkette, Groß-/Kleinschreibung egal, mehrere Begriffe UND-verknüpft, Bezeichnung und Gruppe), Gruppenköpfe
> nur über Treffern, der gewählte Träger übersteht den Filterwechsel, Pfeiltasten wandern über die Treffer; der Baustein
> `Zeilenwahl` um `Beschriftung`/`Zusatzklasse` erweitert, die 19 Bestandsaufrufe unverändert (`80e73c7`, neun bunit-Fälle,
> Abnahmeprobe W4‑19).
> **Windows-Abnahme 04.09.2026, Befund W4‑B‑1 (Preisbasis doppelt oder leer):** die Hülle baute die Preisbasen aus der
> Zieleinheit jeder Umrechnungsregel ohne Dublettenprüfung (Nm³→Nm³ und m³→Nm³ ergaben Nm³ doppelt, 8 der 27 Träger),
> ohne Regeln blieb das Feld leer (5 Träger), ohne Treffer fiel die Wahl still auf Index 0 — wortgleich vom Vorläufer
> `ucFuelSettings` übernommen. Der Listenaufbau liegt jetzt datenbankfrei im Kern (`EnergietraegerPreisCtrl.Preisbasen`:
> Abrechnungseinheit zuerst, Zieleinheiten in Regelreihenfolge, jede genau einmal, normalisiert verglichen; die Id
> indiziert die bereinigte Liste); m³ ist bewusst keine Preisbasis (nur Quelle des z-Faktors, L4). 14 Kern-Fälle, ein
> bunit-Fall, Referenzlauf byte-gleich, keine Datenzeile berührt (`cac4a1d`, W4-Protokoll § 9a).
>
> **Formularraster, Paket P2 (iU8‑E‑2, 05.09.2026, `ac6be91`):** `StromAufschlaege`, `EnergietraegerDialog`, `EnergietraegerEinstellungen`, `ErtragBonus`: drei
> handgebaute `epos-feldpaar` entfallen — der Raster misst die eigene Breite und legt unter `--epos-formularspalte`
> selbst auf eine Spalte um. Preisblock (Schalter + Wert + Schnellwahl), Datenraster und das Suchfeld der Trägerliste
> bleiben, wo sie waren; der Übernahmeknopf des Energieträgers steht in einer `epos-leiste`.
>
> **#166 (10.09.2026) — `BetriebskostenBaugroesseTests` (Anwenderbefund H4c, Sync-Commit `9eee87c`) auf Linux grün.**
> Die 35 Fälle legen ein synthetisches Projekt (190001) mit fünf Anlagenzeilen an; 13 fielen mit „Nullable object must have
> a value" bzw. Betrag 300 erwartet / 0 erhalten (Kern-CI 273 rot auf `9432329`). Ursache: `Tab_Energieanlagen` führt vier
> UNIQUE-Indizes `idx_Anlage_ID_WP/ID_Kessel/ID_PUFFER/ID_BHKW` je auf (`ID_Projekt`, Verweis) — aus
> `sql/schema/003_indizes_fk.sql` und `AnlagenEindeutigkeit.cs`, also auch produktiv —, die sieben Verweisspalten haben
> `DEFAULT 0`, der Bestand trägt für unbenutzte Verweise aber NULL (der Produktweg `AnlagenSql.SQL_ANLAGE_INSERT` setzt alle
> Spalten). Der Testhelfer `Anlage()` ließ die unbenutzten Verweise weg → 0 → ab der zweiten Anlagenzeile UNIQUE-Verstoß;
> `SqliteDatenzugriff.ExecuteSQL` schluckt ihn (`false`), `Sql()` warf den Rückgabewert weg. Mit `PRAGMA foreign_keys = ON`
> (setzt der Zugriff je Verbindung) fällt die 0 sogar schon am Fremdschlüssel — es landete keine einzige Anlagenzeile.
> Zweite Ursache, erst durch das laute `Sql()` sichtbar: `CecWechselrichterAuslieferungTests` setzt `DefaultThreadCurrentCulture` prozessweit auf de-DE und stellt sie nicht zurück — im Projektlauf wurde `2.5` im SQL zu „2,5" (fünf Werte für vier Spalten); jede Zahl im SQL nun in `InvariantCulture`, das Leck selbst schließt #167. Fix (`3abbf1f`, nur Testcode): `Sql()` prüft den Rückgabewert (`Assert.True`, SQL-Text in der Meldung), `Anlage()` setzt
> die sieben Verweisspalten ausdrücklich (Geräte-ID bzw. NULL), Klassenkopf erklärt den Grund. Gate auf `ed7af4e`: Kern 2 273 grün (vorher 2 260 von 2 273),
> UI 3 360, Referenzlauf byte-gleich; die Warnungsschranke bleibt allein durch die vorbestehende CS8602 (`RasterTests.cs:335`,
> `97a7fec`) gerissen. Hinweis an die Windows-Seite: Ein Testhelfer, der `Tab_Energieanlagen` direkt beschreibt,
> muss die Verweisspalten setzen — oder über den Produktweg gehen.
>
> **#167 (10.09.2026) — Kultur-Leck in `EPOS.Kern.Tests` geschlossen.** Befund aus #166: `CecWechselrichterAuslieferungTests`
> setzte `CultureInfo.DefaultThreadCurrentCulture` und `…UICulture` im Konstruktor prozessweit auf de-DE und stellte nichts
> zurück — jeder danach gestartete xunit-Thread eines FREMDEN Tests rechnete in de-DE, im Projektlauf fielen Klassen, die
> allein grün sind. Der Durchgang über alle Setzer fand elf weitere Klassen mit derselben oder einer verwandten Lücke:
> `BhkwKostenTests` (Thread-Kultur nie zurück), die vier Katalogfilter-Klassen (`Dispose` stellte die UI-Kultur aus dem
> Merkwert der Kultur zurück, Thread-Kultur fehlte), `OndImportTests`, `StromspeicherImportTests`,
> `StromspeicherUebernahmeTests` (Konstruktor-Leck), `WechselrichterKatalogTests`, `ZahlenausdruckTests` (Thread-Kultur
> fehlte), `StrangAuslegungTests` (falscher Merkwert). `DiensteTests` ist kein Leck (rechnet die UI-Kultur im `finally` neu).
> Fix (`a8af0c5`, nur Testcode, 13 Dateien): je ein gemerkter Wert je Ziel, Rückstellung in `Dispose`/`finally`; neuer
> Wächter `KulturwaechterTests` (vier Fälle) prüft jede Datei mit Default-Kultur-Setzer auf eine Rückstellung auf dasselbe
> Ziel. **Offen #167‑O‑1 (Anwenderentscheid):** ~65 bunit-Klassen in `EPOS.UI.Tests` pinnen die Kultur im Konstruktor ohne
> Rückstellung — eigener Testprozess, jede Klasse pinnt selbst neu, kein beobachteter Fehler; Empfehlung: gemeinsame
> Vorrichtung statt 65 Handgriffe, Wächter ausweiten (#168, nur auf Wunsch). Gate auf `fd78124`: Kern 2 277 grün (vier neue Wächterfälle), UI 3 359 von 3 360 — der eine rote Fall `KlimadatenDialogTests.Der_Fortschritt_meldet_die_Schritte_und_laesst_sich_abbrechen` (Zeile 357, Abbruchzähler 0 statt 1) ist bunit-Flattern: #167 berührt `EPOS.UI.Tests` nicht, im Gate zu #166 und in drei Wiederholungen der Klasse (16/16) grün → #169 nach dem Muster W16b‑O‑2,
> Referenzlauf byte-gleich; Warnungsschranke weiter allein durch die vorbestehende CS8602 (`RasterTests.cs:335`) gerissen.
>
> **#168 (Anwenderentscheid 11.09.2026 „setze empfehlungen um", #167‑O‑1) — EINE Kulturvorrichtung für `EPOS.UI.Tests`.**
> 113 Testdateien pinnten die Kultur im Konstruktor auf de-DE, nur acht stellten etwas zurück. Statt 113 Handgriffen:
> `EPOS.UI.Tests/Kulturvorrichtung.cs` (`IDisposable`, merkt die vier Werte je Ziel einzeln und stellt jeden aus seinem
> eigenen Merkwert zurück) und `EposBunitContext : BunitContext`, der sie im Konstruktor anlegt und in `Dispose` freigibt.
> 105 Klassen erben jetzt statt zu pinnen (mechanisch, jeder Diff gelesen), acht Sonderfälle mit Sprachwechsel im Fall
> behalten ihre Logik auf Basis der Vorrichtung; `LizenzTexteTests` stellte vorher hart auf de-DE statt auf die
> Ausgangskultur zurück (mitbehoben). Der Kulturwächter aus #167 prüft seither beide Testprojekte (fünf neue Fälle).
> Fix `1f3e56a` (nur Testcode, 115 Dateien, −665 Zeilen). Gate auf `1d7b1e0`: Kern 2 346 grün (+5 Wächterfälle), UI 3 403 grün — auch unter
> en_US-Prozesskultur —, Referenzlauf byte-gleich.

## Statusblock iU9 — Welle 3 umgesetzt (03.09.2026, Basis 95cf8be)

> **Statusblock iU9 — Welle 3 umgesetzt (03.09.2026, Basis `95cf8be`)**
>
> Die Welle stammt aus dem Wellenplan iU9, Abschnitt C Zeile W3: **vier Masken →
> vier Razor-Komponenten**, jede WinForms-Fassung im selben Schritt gelöscht (Regel M1).
> Alle vier hängen am Energieträger — `Form_Energietraeger` öffnet zwei direkt,
> `ucFuelSettings` die anderen beiden. Acht Commits, einer je Nummer:
>
> | Commit | Inhalt |
> |---|---|
> | `afd599d` | **W3.0** Bausteinlücken 4–6: `Standards/Dateiwahl.razor` (Pfad + Knopf, Wähler als Delegat), `Bausteine/Zeilenwahl.razor` (der Wahlknopf, der bisher zweimal wortgleich im Markup stand), `Textfeld` + `Mehrzeilig`/`Zeilen`/`NurLesen`, `Raster` + `Bearbeitbar` |
> | `624ce28` | **W3.1** `Form_LeistungspreisReihe` → `Dialoge/Kosten/LeistungspreisReiheDialog.razor` + Hülle; zwölf Monatssätze im mitwachsenden Gitter, Ebenenregel Projekt-/Stammreihe unverändert |
> | `b2a9511` | **W3.2** `Form_SpotpreisImport` → `Dialoge/Kosten/SpotpreisImportDialog.razor` + Hülle; Dateiwahl über `Dienste.Datei`, Prüfen und Schreiben in `Task.Run`, Protokoll mehrzeilig und festbreit |
> | `15417a8` | **W3.3** `Form_Emissionskatalog` (767 Z., zwei Raster) → `Dialoge/Kosten/EmissionskatalogDialog.razor` + Hülle; die beiden zur Laufzeit gebauten Unterdialoge werden **eingerückte Blöcke** statt zweiter WebViews (R2) |
> | `cb700f0` | **W3.4** `Form_Kostenprofil` (36 Laufzeitfelder + Chart) → `Dialoge/Kosten/KostenprofilDialog.razor` + Hülle; **neu im Kern:** `ChartRenderer.Kostenprofil` samt `C_PROFIL` |
> | `5a25c1d` | **W3.5** Ressourcen-Sammelnachtrag: 67 Schlüssel (`LPR_*`, `SPOT_*`, `EMK_*`, `KPROF_*`) in `Resource.resx`, `Resource.en-US.resx` und — von Hand — `Resource.Designer.cs` |
> | `4ea688c` | **W3.6** Formularkarte-Tests: fünftes Prüfmuster (`Form_Kostenprofil`, neun Testbezüge), Stapellauf-Zähler 102 → 98 |
> | *dieses Paket* | **W3.7** Protokoll, Statusblock, `CLAUDE.md` ×3 |
>
> **Die Renderer-Erweiterung ist der eigentliche Gewinn.** `ChartRenderer.Kostenprofil` ist die
> erste neue Methode seit der SkiaSharp-Portierung (iU7) — der Nachweis, dass der Weg
> „Diagramm im Kern zeichnen, in der Oberfläche nur das PNG zeigen" auch für **Eingabemasken**
> trägt, nicht nur für den Bericht. Bildmaß 1296 × 780 (doppelte Zielauflösung des abgelösten
> WinForms-Chart), Linienfarbe wörtlich übernommen, y-Achse vorzeichenfähig. Die Probe
> `Proben/ChartProben` prüft es als zehntes Bild.
>
> **Bausteinsatz.** Zwei neue Bausteine (`Dateiwahl`, `Zeilenwahl`), drei erweiterte Standards
> (`Textfeld`, `Raster`, dazu `Zahlenfeld` unverändert) und sieben CSS-Klassen. Damit sind die
> Bausteinlücken 4, 5 und 6 des Wellenplans geschlossen; `Dateiwahl` bedient ab Welle 13 die
> sechs Importmasken.
>
> **Kein neuer Controller, keine neue SQL-Zeile.** Alle vier Masken riefen schon vorher
> ausschließlich Kern-Controller (`PreisreiheCtrl`, `SpotpreisImportCtrl`,
> `EmissionskatalogCtrl`/`EmissionenCtrl`, `KostenprofilCtrl`) — Hausmuster Ä9.
>
> **Nachweise.** `dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental` →
> 0 Fehler, **26** Warnungen (Basis 28; WFO1000 22 → 20) · `dotnet test WP-Plan.Kern.slnf` →
> **1 217** grün (1 110 vorher; 105 neue bunit- und 2 neue Kern-Tests) · Formularkarte **121**
> grün, Build 0/0 · Stapellauf **98** Masken, 0 × „nein", 0 × „verwaist" · SQL-Prüfer 1 303
> Texte, 0 Fundstellen · ChartProben **10** Bilder, 0 Verstöße · Referenzlauf 1030/1007/1017
> gegen `2026-08-30_B3-Kaskade` **PASS/PASS/PASS**, `diff -rq` ohne Unterschied ·
> `dotnet publish` mit vollständigem `wwwroot`.
>
> **Protokoll** mit Feldkartenabgleich (4 Masken, alle vollständig, dazu die 36
> Laufzeitfelder und die beiden Untereditoren), 22 Abweichungen (A‑1…A‑22),
> Windows-Abnahmeliste mit vierzehn fachlichen Proben und sieben offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W3_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus** — alles Obige ist auf Linux gemessen.
>
> **Formularraster, Paket P2 (iU8‑E‑2, 05.09.2026, `ac6be91`):** Vier Dialoge (`SpotpreisImportDialog` einspaltig wegen des Pfadfelds, `KostenprofilDialog`,
> `EmissionskatalogDialog`, `GesetzeskatalogZeileDialog`), davon drei mit geteiltem Raster: Der Satz zwischen den Feldern
> behält die volle Zeile, die Beschriftungskante läuft über beide Raster durch. Die Wertetafeln (12 Monats-, 24
> Stundenwerte) und Datenraster bleiben, was sie sind.

## Statusblock iU9 — Welle 2 umgesetzt (03.09.2026, Basis b0d3d86)

> **Statusblock iU9 — Welle 2 umgesetzt (03.09.2026, Basis `b0d3d86`)**
>
> Die Welle stammt aus dem Wellenplan iU9, Abschnitt C Zeile W2: **sechs Masken →
> vier Razor-Komponenten** (drei neue, eine erweiterte), jede WinForms-Fassung im selben
> Schritt gelöscht (Regel M1). Acht Commits, einer je Nummer:
>
> | Commit | Inhalt |
> |---|---|
> | `f9b5016` | **W2.1** `Form_StromspeicherItemNeu` (28 Aufrufer), `Form_GebaeudetypNeu` und `Form_AlsVariante` → **eine** erweiterte `Dialoge/Allgemein/NamensDialog.razor`; der Variantenablauf steht als `Views/Varianten/AlsVarianteHuelle.cs` |
> | `41db247` | **W2.2** **Sprungbrücke** — `Dialoge/Allgemein/Sprungziel.cs` (Schlüssel) + `Allgemein/Blazor/Sprungbruecke.cs` (Schlüssel → `Form`, modal aus dem Rückruf). Entscheid zu B5b‑O1 |
> | `938947a` | **W2.3** `Form_Tarifstruktur` (K4, 588 Z.) → `Dialoge/Wirtschaftlichkeit/TarifstrukturDialog.razor` + `TarifstrukturHuelle`; `Zahlenfeld`/`Ganzzahlfeld` bekommen `Aktiv` |
> | `a684fcd` | **W2.4** `Form_PhotovoltaikVerguetung` → `Dialoge/Wirtschaftlichkeit/PhotovoltaikVerguetungDialog.razor` + `PhotovoltaikVerguetungHuelle` |
> | `8ef5b60` | **W2.5** `Form_WirtschaftlichkeitParameter` (K4, 740 Z.) → `Dialoge/Wirtschaftlichkeit/WirtschaftlichkeitParameterDialog.razor` + Hülle; **Ersteinsatz der Sprungbrücke** (Gesetzeskatalog) |
> | `a2b3bd2` | **W2.6** Ressourcen-Sammelnachtrag: 78 Schlüssel (`NAMD_*`, `TARIF_*`, `PVV_*`, `WPAR_*`) in `Resource.resx`, `Resource.en-US.resx` und — von Hand — `Resource.Designer.cs` |
> | `3fd320e` | **W2.7** Formularkarte-Tests: viertes Prüfmuster (`Form_StromspeicherItemNeu`, sechs Testbezüge), Stapellauf-Zähler 105 → 102 |
> | *dieses Paket* | **W2.8** Protokoll, Statusblock, `CLAUDE.md` |
>
> **Die Sprungbrücke ist der eigentliche Gewinn.** Bis W2 konnte ein Blazor-Dialog nur
> *nachgelagert* weiterführen (schließen → Ziel → wieder öffnen, B5b‑O1). Jetzt zeigt ein
> Delegat mit sprachneutralem Schlüssel ein **WinForms**-Ziel modal über dem Dialog — dieselbe
> verschachtelte Nachrichtenschleife wie ein `OpenFileDialog` im Click. Für Ziele, die selbst
> Blazor-Hüllen sind, bleibt es beim nachgelagerten Sprung (Risiko R2), bis Welle 4 den
> Baustein `Ueberlagerung` bringt. **Ob die Schleife am Gerät trägt, ist Abnahmepunkt W2‑7.**
>
> **Bausteinsatz.** Kein neuer Baustein — `Zahlenfeld` und `Ganzzahlfeld` bekommen nur den
> Parameter `Aktiv` (additiv), dazu die CSS-Klasse `epos-untergruppe`. Der Namensdialog aus
> W1 trägt jetzt fünf Masken statt zwei.
>
> **Nachweise.** `dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental` →
> 0 Fehler, **28** Warnungen (Basis 28, unverändert) · `dotnet test WP-Plan.Kern.slnf` →
> **1 110** grün (1 036 vorher; 74 neue bunit-Tests) · Formularkarte **120** grün, Build 0/0 ·
> Stapellauf **102** Masken, 0 × „nein", 0 × „verwaist" · SQL-Prüfer 1 303 Texte, 0
> Fundstellen · Referenzlauf 1030/1007/1017 gegen `2026-08-30_B3-Kaskade` **PASS/PASS/PASS**,
> `diff -rq` ohne Unterschied · `dotnet publish` mit vollständigem `wwwroot`.
>
> **Protokoll** mit Feldkartenabgleich (6 Masken, alle vollständig), 18 Abweichungen
> (A‑1…A‑18), Windows-Abnahmeliste mit elf fachlichen Proben und sieben offenen Punkten:
> `WindowsFormsApplication1/Allgemein/Reporting/iU9_W2_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus** — alles Obige ist auf Linux gemessen.
>
> **Formularraster, Paket P2 (iU8‑E‑2, 05.09.2026, `ac6be91`):** Die drei Wirtschaftlichkeitsmasken (`TarifstrukturDialog`, `PhotovoltaikVerguetungDialog`,
> `WirtschaftlichkeitParameterDialog`) sind der eigentliche Anlass des Pakets: 53 Felder, vierzehn Raster, sechs
> `Formulargruppe`n (erster echter Einsatz; die vier zur Laufzeit gebauten Untergruppen der Tarifstruktur und die Gruppe
> „Bilanz"). Der Block „Rollenmodell" war der höchste des Hauses und halbiert sich, weil alle Stufenfelder kurz sind.
> Beim Merge mit der Rechner-2-Linie blieb das Degradationsfeld (Paket B) im Raster — sein Tooltip-Wirt ist durchsichtig.

## Statusblock iU9 — Welle 1 umgesetzt (03.09.2026, Basis aef9509)

> **Statusblock iU9 — Welle 1 umgesetzt (03.09.2026, Basis `aef9509`)**
>
> Die Welle stammt aus dem Wellenplan iU9, Abschnitt D: **sieben Masken der Kostenvorlagen und der
> Wirtschaftlichkeit → sechs Razor-Komponenten**, jede WinForms-Fassung im selben Schritt gelöscht
> (Regel M1). Neun Commits, einer je Nummer:
>
> | Commit | Inhalt |
> |---|---|
> | `9e4fa37` | **W1.0** Baustein `Optionsgruppe` (RadioButton-Gruppe; 45 im Bestand) samt 10 bunit-Tests |
> | `0d92c89` | **W1.1** `Form_VorlagenPosition` → `Dialoge/Kosten/VorlagenPositionDialog.razor` |
> | `e94978a` | **W1.2** `Form_VariantenName` + `Form_KostenItemNeu` → **eine** `Dialoge/Allgemein/NamensDialog.razor` mit dem Windows-Helfer `NamensDialogHuelle` |
> | `f6e9264` | **W1.3** `Form_CaseEingabe` → `Dialoge/Kosten/CaseEingabeDialog.razor` |
> | `584be20` | **W1.4** `Form_VorlagenUebernahme` → `Dialoge/Kosten/VorlagenUebernahmeDialog.razor` + `VorlagenUebernahmeHuelle` |
> | `8c40854` | **W1.5** `Form_KostenAdmin` → `Dialoge/Kosten/KostenfaktorKatalogDialog.razor` + `KostenfaktorKatalogHuelle` + Kern-Controller `KostenfaktorCtrl` |
> | `9a5df28` | **W1.6** `Form_WirtschaftlichkeitVerlauf` → `Dialoge/Wirtschaftlichkeit/KapitalwertVerlaufDialog.razor` + `KapitalwertVerlaufHuelle` |
> | `e6a613e` | **W1.7** Ressourcen-Sammelnachtrag: 43 Schlüssel (`VPOS_*`, `NAMD_*`, `KCASE_*`, `KUEB_*`, `KFAK_*`, `WVERL_*`) in `Resource.resx`, `Resource.en-US.resx` und — von Hand — `Resource.Designer.cs` |
> | `21e399c` | **W1.8** Formularkarte-Tests: Prüfmuster für `Form_CaseEingabe`, Stapellauf-Zähler 118 → 111 |
>
> **Bausteinsatz.** Genau ein neuer Baustein (`Optionsgruppe`, Lücke 1 aus Abschnitt E) und ein
> neuer Dialogtyp (`NamensDialog`, Lücke 2). `Ueberlagerung`, `Rueckfrage` und `Fortschritt` bleiben
> offen — die drei Stellen, an denen sie fehlen, sind im Protokoll als A‑16 und A‑17 benannt und
> laufen bis dahin über die Hülle bzw. über die Statuszeile.
>
> **Nachweise.** `dotnet build WP-Plan.sln -c Release -p:Platform=x64` → 0 Fehler, **30** Warnungen
> (Basis 34; WFO1000 30 → 24) · `dotnet test WP-Plan.Kern.slnf` → **1 024** grün (929 vorher; 95 neue
> bunit-Tests) · Formularkarte **119** grün, Build 0/0 · Stapellauf **111** Masken · SQL-Prüfer
> 1 333 Texte, 0 Fundstellen · Referenzlauf 1030/1007/1017 gegen `2026-08-30_B3-Kaskade`
> **PASS/PASS/PASS**, `diff -rq` ohne Unterschied · `dotnet publish` mit vollständigem `wwwroot`.
>
> **Protokoll** mit Feldkartenabgleich, 19 Abweichungen (A‑1…A‑19), Windows-Abnahmeliste und sieben
> offenen Punkten: `WindowsFormsApplication1/Allgemein/Reporting/iU9_W1_Blazor_Port_Protokoll.md`.
> **Windows-Abnahme steht aus** — alles Obige ist auf Linux gemessen.
>
> **Formularraster, Paket P2 (iU8‑E‑2, 05.09.2026, `ac6be91`):** Die drei Kostenvorlagen-Kleindialoge (`CaseEingabeDialog`, `VorlagenPositionDialog`,
> `VorlagenUebernahmeDialog`) und der BHKW-Wirtschaftlichkeitsdialog tragen den Raster; die Vorlagenübernahme ist die
> Stelle, an der `Einspaltig` seinen Namen verdient — eine Kette von Wahlen bleibt eine Kette, die Beschriftung steht
> trotzdem neben dem Feld. `VorlagenZeile` bleibt Bearbeitungszeile in der Tabelle.
>
> **Ae25 (Anwenderentscheid 10.09.2026, Windows-Seite) und #165 — `VorlagenUebernahmeDialog`: OK übernimmt und schließt,
> Abbrechen schließt ohne Wirkung, ein Fehlschlag hält die Maske offen.** Der Sync-Commit `4beb37f` (08:21) brachte den
> Dialog und die Hülle auf Ae25, die drei bunit-Fälle der alten Regel A‑7 („Übernehmen" schreibt und lässt die Maske stehen)
> blieben rot — Kern-CI 269/271. #165 (`90097ee`) schrieb sie um; der zweite Sync-Commit `9eee87c` (08:45) brachte die
> Windows-Fassung derselben Tests (19 Fälle, u. a. `OK_uebernimmt_und_schliesst_mit_true`,
> `Abbrechen_schliesst_mit_false_und_schreibt_nicht`, `Ein_Fehler_erscheint_als_Fehlerbanner_und_der_Dialog_bleibt_offen`)
> — **im Merge `89ee918` gilt die Windows-Fassung**, #165 ist damit überholt. Lehre: Vor einem Sync-Push von der Windows-Seite
> `dotnet test EPOS.UI.Tests` fahren, sonst wird die CI mit jedem Sync rot. Gate: UI 3360 grün; Kern 2 260 von 2 273 — **13 Fälle der
> neuen `BetriebskostenBaugroesseTests` (444 Zeilen, ebenfalls aus `9eee87c`) rot** („Nullable object must have a value",
> Betrag 300 erwartet, 0 erhalten): vorbestehend auf dem Sync-Kopf `9432329` (Kern-CI 273 rot), nicht durch diesen Merge
> verursacht — Aufgabe #166.

## Statusblock iU9 — Welle 0 umgesetzt (03.09.2026, Basis 908926a)

> **Statusblock iU9 — Welle 0 umgesetzt (03.09.2026, Basis `908926a`)**
>
> **Die K6-Liste ist abgetragen.** Der Anwenderentscheid **iF29** (Register § 1) hat entschieden,
> was der Erreichbarkeitsgraph vom Vormittag gemeldet hatte: Vier unerreichbare und eine verwaiste
> Maske werden **nicht** nach Blazor umgestellt, sondern stillgelegt — dazu drei Masken, die nur an
> ihnen hingen, und `Form_KwkgModule`, deren Knopf seit B5b ausgeblendet war und deren Felder
> vollständig im `BhkwWirtschaftlichkeitDialog` stehen. Drei Commits:
>
> | Commit | Inhalt |
> |---|---|
> | `bb0474c` | **W0.1** Was von außen an `Form_Kosten` hing, in den Kern gerettet: `EPOS.Kern/Controller/KostenSummenCtrl.cs` (`KATEGORIE_INVESTITION`/`KATEGORIE_BETRIEB`, `GetAllCarriers`, `LiesKomponentenSummen`, `LiesAnlagenSummen` — Rümpfe wörtlich) und `EPOS.Kern/Model/EnergietraegerModel.cs` (`EnergyCarrier`, `EnergyConversion`); sieben Aufrufer umgestellt |
> | `16b106a` | **W0.2** 25 Dateien / 10 625 Zeilen gelöscht: `Form_Kosten`, `Form_KostenfaktorItem`, `ucKostenItem` (Klasse `ucKostenZeile`), `Form_Betriebskosten`, `Form_Variantentest`, `Form_Wirtschaftlichkeit`, `Form_Bericht`, `Form_Simulation_Kurz`, `Form_Simulation_Detail - Kopie.cs`, `ChartManagerNeu.cs`, `Form_KwkgModule` — samt den zwei Altknöpfen der Startseite (`btn_Kosten`, `btn_Varianten`, jetzt auch aus Designer und `.resx` entfernt), dem Modul-Knopf der Wirtschaftlichkeitsparameter, der `Compile Remove`-Liste der `.csproj`, sieben `HilfeKontext`-Einträgen, neun Zeilen `help_mapping.txt` und 26 Kommentarverweisen |
> | `43452a7` | **W0.3** Formularkarte: drittes — und erstes **stillgelegtes** — Prüfmuster (`Form_KostenfaktorItem`), `Form_Kosten.Auszug.cs` um `AddKostenItem` erweitert, 15 Tests umgehängt bzw. ersetzt, `Erreichbarkeit_2026-09-03.md` neu gezogen |
>
> **Zähler.** Designer-Dateien 114 → **108**, Masken 111 → **105**, lokalisiert 62 → **61**,
> Kartenzeilen 2 322 → **2 231**, Felder ohne Beschriftung 172 → **168**. Erreichbarkeit:
> ja 104 → **103**, **nein 4 → 0**, **verwaist 1 → 0**, unklar 2 → **2**
> (`Form_GebWohnflaeche`, `Form_PufferSp_Bearbeiten` — beide bleiben und werden umgestellt).
>
> **Behalten wie entschieden:** `FormMain`/`Form_StromTest` (der Menüpunkt „Projektdetail" bleibt),
> `Form_GebWohnflaeche`, `Form_PufferSp_Bearbeiten`, `Form_AlsVariante`.
>
> **Nachweise.** `dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental` → 0 Fehler,
> **28** Warnungen (30 vorher; die zwei WFO1000 von `AlsDialog` entfallen mit den Dialoghüllen) ·
> `dotnet test WP-Plan.Kern.slnf` → **1 036** grün (35/450/337/214) · Formularkarte **119** grün,
> Build 0/0 · Stapellauf **105** Masken, 0 nein / 0 verwaist · SQL-Prüfer 1 303 Texte, 0 Fundstellen ·
> Referenzlauf 1030/1007/1017 gegen `2026-08-30_B3-Kaskade` **PASS/PASS/PASS**, `diff -rq` ohne
> Unterschied · `dotnet publish` mit vollständigem `wwwroot`. **Windows-Abnahme steht aus** — die
> vier Prüfpunkte stehen im Protokoll, Abschnitt W0.

## Statusblock iU10 — Stand 03.09.2026

#### Statusblock iU10 — Stand 03.09.2026

**Sieben Schritte umgesetzt, einer offen.** Nachweise im Einzelnen:
[`Umsetzung_iU10_Nachweise.md`](../../../aktuell/Umsetzung_iU10_Nachweise.md).

| Schritt | Inhalt | Stand |
|---|---|---|
| **iU10-1** | Paketlage berichtigt: `bundle_green` raus (die Fassung 2.1.12 **existiert nicht**), `bundle_e_sqlite3` 2.1.12 rein, MAUI 10.0.100 und `SkiaSharp.NativeAssets.iOS` 3.119.0 dazu; `InternalsVisibleTo EPOS.iOS` | ✅ Linux |
| **iU10-2** | `EPOS.UI/Seiten/` — `AppWurzel` (Zustandsmaschine Liste ↔ Dialog), `Projektliste`, `Seitenschluessel`; die Schnittstellen `IProjektQuelle` und `INavigationsZiel` | ✅ Linux, **+12 bunit-Tests** |
| **iU10-3** | `EPOS.iOS/` angelegt: csproj, eigene `.sln`, `MauiProgram`, `App`, `HauptSeite`, `wwwroot/index.html`, `Platforms/iOS/`, Ressourcen | ✅ Restore-Probe · Bau nur CI |
| **iU10-4** | Datenbankweg: Seed-Kopie beim Erststart, `DataRepository.PfadUeberschreibung`, Gate-Zeilen `SQLite …` / `STRICT=…`, Sicherung über `VACUUM INTO` | ✅ Übersetzungsprobe · Wirkung nur CI |
| **iU10-5** | die neun Umgebungsdienste als `Ios*`-Adapter, dazu `IosHilfeDienst`; Belegung in `MauiProgram` in der Reihenfolge von `Program.Main` | ✅ Attrappenprobe · Wirkung nur CI |
| **iU10-6** | Prüfmodus (`EPOS_PRUEFLAUF`) mit den **verlinkten** Bausteinen `Ergebnisexport`/`Protokoll`; CI-Job `.github/workflows/ios.yml` | ✅ YAML geprüft · Lauf nur CI |
| **iU10-7** | `IosProjektQuelle` — Projektliste, Energieträgerliste und der BHKW-Parametersatz über **dieselben** Kern-Controller wie die Windows-Hülle | ✅ Prüfstand gegen `Kenndaten_Test.sqlite` |
| **iU10-CI** | Achter Lauf `ios.yml` (33748736894): Workload 23 s, Bau 57 s, Simulator, Erststart 73 MB, `SQLite 3.53.3`, `STRICT=114`, `Projekte=23`, Prüfmodus 5 s, **iZ6-Vergleich 1030 PASS und byte-gleich** | ✅ CI macOS, 5 min 44 s · **Neunter Lauf** (33785012663, 03.09.2026, 9 min 52 s) auf `f1d387b` nach W5/W6: grün · **Zehnter Lauf** (33809247370, 03.09.2026, 4 min 53 s) auf `21ab680` nach W7–W9: grün · **Elfter Lauf** (33826084944, 04.09.2026, 8 min 50 s) auf `a398c9a` nach W10a/W10b: grün · **Zwölfter Lauf** (33832613617, 04.09.2026, 9 min 02 s) auf `43fb9c3` nach W11a/W11b: grün · **Dreizehnter Lauf** (33838762108, 04.09.2026, 6 min 30 s) auf `62b3457` nach W12: grün · **Vierzehnter Lauf** (33844935661, 04.09.2026, 10 min 04 s) auf `29aecbc` nach W13: grün · **Fünfzehnter Lauf** (33852944072, 04.09.2026, 7 min 24 s) auf `ecd6cfe` nach W14a/W14b: grün · **Sechzehnter Lauf** (33861268537, 04.09.2026, 9 min 28 s) auf `0cc1495` nach W14c: grün · **Siebzehnter Lauf** (33867643966, 04.09.2026, 10 min 30 s) auf `c11f13d` nach W15a: grün · **Achtzehnter Lauf** (33876284942, 04.09.2026, 2 min 26 s) auf `f71853b` nach W15b: **rot** (CS0103 `IosHilfeDienst`, nur der macOS-Läufer übersetzt die iOS-Hülle; behoben `f0e23a4`) · **Neunzehnter Lauf** (33878903371, 04.09.2026, 6 min 27 s) auf `f0e23a4`: grün · **Zwanzigster Lauf** (33883210632, 04.09.2026, 10 min 14 s) auf `975ead5` nach W15c: grün · **Einundzwanzigster Lauf** (33890882150, 04.09.2026, 8 min 03 s) auf `84d7c16` nach W16a: grün · **Zweiundzwanzigster Lauf** (33898599945, 04.09.2026, 7 min 56 s) auf `c8fbd77` nach W16b: grün · **Vierundzwanzigster Lauf** (33904433007, 04.09.2026, 8 min 43 s) auf `555ef11` nach W16c: grün (Nr. 23 war eine abgebrochene Dublette) · **Fünfundzwanzigster Lauf** (33913313694, 04.09.2026, 6 min 29 s) auf `853b8c6` nach den W16-Nachträgen (E‑2/E‑3, LizenzTexte, W16b‑O‑3): grün · **Sechsundzwanzigster Lauf** (33975880961, 05.09.2026, 6 min 14 s) auf `7bec4ad` nach den Abnahmebefunden vom 05.09. (Baustein `Diagramm`, `epos-diagramm.js` über `import()`): grün · **Siebenundzwanzigster Lauf** (33982889724, 05.09.2026, 10 min 51 s) auf `c563a40` nach den sechs Nachmittagsbefunden vom 05.09. (W13‑B‑1: `…Async`-Zwillinge in `IDateiDienst`/`IDialogDienst`, Fehlerschranke `Wurzel<T>` in `EPOS.iOS/HauptSeite`; W9.8, W15a‑E‑1, W16b‑E‑7, iU8‑E‑1): grün · **Achtundzwanzigster Lauf** (33992594094, 05.09.2026, 6 min 53 s) auf `6eddd27` nach der Zusammenführung der Rechner-2-Linie: Bau, Start, Prüfmodus grün, **iZ6-Vergleich rot**, weil `ios.yml` noch gegen `2026-08-30_B3-Kaskade` hielt (8 711 Abweichungen = Paket-A-Zeitbasis; Basiswechsel `37dfebb`, Workflow `e3fd980`) · **Neunundzwanzigster Lauf** (33993379551, 05.09.2026, 6 min 32 s) auf `e3fd980` gegen `2026-09-05_R2_Zeitbasis`: grün, PASS und byte-gleich (236 670 Werte, `diff -rq` leer; die Simulation meldet die Paket-A-Zeitbasis „Klimadaten: UTC → MEZ/MESZ, Referenzjahr 2025") · **Dreißigster Lauf** (34017405042, 06.09.2026, 5 min 54 s) auf `cb8379e` nach der Welle iF30 (Schreibnaht, Werkzeug-Freigabe im Prüfmodus, Lizenzbanner): grün, PASS und byte-gleich (236 670 Werte, `diff -rq` leer; das Vergleichswerkzeug meldet „Schreibnaht: freigegeben für EPOS.Referenzlauf (Rechennachweis ohne Lizenz)") |
| **iU10-9** | der iL5-Wizard in `EPOS.UI/Seiten/` und `IosNavigation` vollständig | **offen** |

## #245 — Fokus bleibt beim Tippen in der Suchraum-Tabelle (12.09.2026)

> **#245 (12.09.2026, Anwenderbefund per Bildschirmfoto: in Station „4 Optimierung" springt bei jeder Tastatureingabe der Fokus aus den
> Zahlenfeldern der Tabelle „Suchraum je Einheit") — umgesetzt (`7deecd56`, Merge `a35b1022`, Agent Opus; 5 Dateien, +208/−4).** Ursache
> in jedem Glied am Code belegt: `Zahlenfeld`/`Ganzzahlfeld` melden per `@oninput` je Tastendruck, `OptimierungBlock` reicht über
> `Geaendert` an `StromspeicherAuslegungSeite.FlotteGeschrieben()`, und das ersetzt die Flottenkonfiguration durch eine JSON-Tiefenkopie
> (`SpeicherAuslegungKopie.Von`) — aus gutem Grund, vier Blätter schreiben an derselben Konfiguration; `FlottenAuslegungsAchse`
> überschreibt `Equals` nicht, `<tr @key="a">` verglich also Referenzen, sah je Tastendruck einen neuen Schlüssel, und Blazor riss die
> Zeile samt `<input>` ab. Zweite Hälfte des Befunds, die niemand gemeldet hatte: ein frisch aufgebautes `Zahlenfeld` hat keine
> Texterinnerung und schrieb „640," zu „640" — das Dezimaltrennzeichen ging mitten in der Eingabe verloren. **Fix a) allein:** `@key` auf
> Wertidentität — `OptimierungBlock.Zeilenschluessel` = „id:" + (`ErsetztEinheitId` ?? `Vorlage.Id`), dieselbe Zuordnung, mit der der
> Optimierer die Achse ihrer Einheit zuordnet, als Text kopiefest; fehlt die Kennung oder trägt eine zweite Achse dieselbe, entscheidet
> „#" + Zeilennummer (ein doppelter `@key` bricht den Zeichenlauf ab). **Fix b) geprüft und begründet verworfen:** die Tiefenkopie je
> Tastendruck ist lasttragend — drei Editoren (`SpeicherFlottenEditor`, `SpeicherFlottenBetriebEditor`, `SpeicherAuslegungEditor`)
> frischen ihre Arbeitskopie nur an einer geänderten REFERENZ auf; bliebe sie stehen, schriebe jeder beim nächsten Feld seinen alten Stand
> zurück. Aufwandspunkt, kein Fehler: die Kopie serialisiert je Tastendruck die ganze Konfiguration; eine Fassungsnummer statt der
> Referenzprüfung wäre der saubere Weg, falls es je spürbar wird. **Zweite Stelle desselben Musters** in `SpeicherFlottenEditor.razor`
> (Lebensdauerkurve, `@key="punkt"` — `OnParametersSet` baut `_wert` je gemeldetem Feld neu auf): Schlüssel ist jetzt die Zeilennummer
> innerhalb der per `einheit.Id` getrennten Einheit. Alle übrigen `@key` in EPOS.UI gesichtet und belassen — Wertidentitäten
> (`einheit.Id`, `k.AnlageId`, `zeile.Id`, `z.Kennung`, `eintrag.Id`, `Rasterstand`) oder gewollte Neuaufbauten über Zähler (`_stand`,
> `_wahlfeldSchluessel`, `_extrapolationSchluessel`, KiChat); die einzige verbleibende Objektreferenz `WaermepumpenDialog` (`_gewaehlt`)
> wechselt nur bei Zeilenwahl und MUSS die Detailansicht neu aufbauen (W7‑B‑3). **Wachen:** drei bunit-Fälle, vor dem Fix rot
> (`Assert.Same` „not the same instance", Erwartet „640," / Tatsächlich „640"), messen die Identität der KOMPONENTEN-Instanzen statt der
> DOM-Knoten — bunit liest das Markup nach jeder Änderung neu ein, AngleSharp-Knoten sind danach immer neu; Hausregel in `EPOS.UI/CLAUDE.md`
> nach dem #235-Absatz: „`@key` nie auf ein Objekt, das eine Kopie je Änderung neu erzeugt — Wertidentität nehmen". UI-Tests 3 958 → 3 961.
> Kein Rechenweg, keine Ressource, kein SQL berührt. **Dazu #246 (Konzept, SD‑E‑10):** die Anwenderrückmeldung zur Suchsemantik
> („Variation der Größe ergibt nur Sinn ohne vorgegebenen Speichertyp; für mehrere Speicher ist die Stückzahl die Variable — eine zweite
> Methode; Dialog übersichtlicher") steht als Kapitel 8 im Konzept `Dokumentation/aktuell/Konzept_Stromspeicher_Dialoge_EPOS-Plan.md`
> (`a47e0f54`): zwei Suchmethoden „Größe suchen" (freie Einheit, Stückzahl fest 1) und „Stückzahl suchen" (Einheit aus Projektanlage oder
> Katalog, Größe fest, Stückzahl von–bis je Einheit, Kandidaten = Produkt der Stückzahlbereiche × Ziele, kein Feinraster), Herkunftsfeld
> `FlottenEinheit.Herkunft` (Altbestand: `AnlageId` → Projektanlage, sonst Katalog), Engine-Feld `FlottenAuslegungsAchse.Suchart`, Karten
> je Einheit statt der Neun-Spalten-Tabelle, Fragen SD‑Q13…Q18 mit Empfehlung — **Entscheid offen**; das Mockup
> `Mockups/stromspeicher-optimierung-v2.html` (Agent Opus, `ff2f12bb`, Merge `82f00552`) zeigt Kopfblock, Karten und Ergebniskasten für beide
> Methoden. Der in 7.4 genannte Pfad `stromspeicher-optimierung.html` existierte nie, der Verweis ist berichtigt. **Gate sept55 auf
> `a35b1022`** (#245-Merge; das Mockup kam danach hinzu und ändert nichts, was das Gate misst): Kern 2 743, UI 3 961, Engine 425, KiKern 488,
> 5 eindeutige Warnungen, SQL 0 von 1 344, ChartProben 61 Bilder und 14 Gegenproben, 0 Verstöße, Referenzlauf 5/5 byte-gleich gegen R7; en-US-Lauf grün.

## #246/#247 — Zwei Suchmethoden der Station „4 Optimierung“ (12.09.2026)

> **#246/#247 (12.09.2026, Anwenderentscheid SD‑E‑10 zur Station „4 Optimierung": SD‑Q13 Empfehlung · SD‑Q14 „variierbar" · SD‑Q15 „die
> Markierung einer Einheit ist nicht erforderlich, sie werden nur temporär für die Optimierung benötigt; ein Button ‚Einheiten in
> Projekt übernehmen' für die ausgewählten Einheiten" · SD‑Q16 „nur Stückzahl variieren" · SD‑Q17/Q18 Empfehlung; „Größensuche
> bleibt") — umgesetzt (Konzept `316152dd`; Engine `51166101`, Kern `d51c91d1`, Oberfläche `57baf809`, Doku `8e00629b`, Merge
> `80e2788f`, Agent Opus; 37 Dateien, +4 564/−414).** Das Konzeptkapitel 8 ist auf die Entscheide umgeschrieben: Einheiten tragen keinen
> Typ, je Einheitenkarte ein Schalter „variieren", die gewählte Methode bestimmt, WAS an den eingeschalteten Einheiten variiert —
> **Größe suchen** (zwei der drei Größen nach Kopplung, Stückzahl fest auf dem Kartenwert, Feinraster erlaubt) oder **Stückzahl suchen**
> (Stückzahl von–bis je Einheit, 0 = entfällt, Größe fest, Kandidaten = Π Stückzahlbereiche × Ziele, kein Feinraster, Kosten je Stück);
> je Lauf EINE Variationsart, das Mischraster Stückzahl × Größe gibt es nicht mehr. Zwei Folgefragen entschied die Orchestrierung
> (8.5): Übernahme je Stück EINE Projektanlage, Rückschreiben statt Dublette bei `AnlageId`. **Engine:** `FlottenAuslegungEingang.Suchmethode`
> (Bewerten/Groesse/Stueckzahl, Vorgabe Groesse, damit ein Stand ohne Feld weiterrechnet — Bauart wie `Feinraster`), `Kandidatenzahl`,
> `Achsengroesse`, `FeinrasterHoechstzahl`, `Feinrasterwerte`, `BildeAchse`, `BaueEinheiten` lesen sie; `FlottenSuchbefund` (keine
> aktive Achse, Stückzahlbereich leer, Größenbereich unbrauchbar) als benannte Ablehnung VOR dem Bau der Achsen; Stückzahlen je Kandidat
> in der Zusammenfassung; 12 Engine-Tests (266/4/1 Kandidaten am Konzeptbeispiel, zwei JSON-Rundläufe). **Kern:**
> `SpeicherFlottenAnzeigeCtrl.Stueckzahl.cs` (Kurve „Kapitalwert über Stückzahl" mit Bestwertmarke und Schraffur, Rasterkarte n₁ × n₂
> als dieselbe `Optimierungsraster`-Zeichnung mit ganzzahligen Achsen), `ChartRenderer.Stueckzahlkurve`, ChartProben 61 → **64 Bilder**
> (+2) und **15 Gegenproben** (+1: die Bestwertmarke ändert das Bild); `SpeicherFlottenStudieCtrl.Uebernahme.cs` mit `Vorschau` und
> `EinheitenInProjektUebernehmen` — je Stück eine Anlage über `Tab_Stromspeicher` UND `AnlagenSql.SQL_ANLAGE_INSERT` in EINEM
> `DbVorgang`, Bezeichner im Vorgang eindeutig, acht Gerätewerte, Bezeichner bleibt beim Rückschreiben stehen; 16 Kern-Tests gegen eine
> Kopie der Testdatenbank; KI-Maskenbrücke meldet „Suchmethode" (Katalogfelder 26 → 27). **Oberfläche:** `OptimierungBlock.razor` neu
> nach 8.4 — Kopfblock zweispaltig (drei Optionen mit Erklärsatz, gedimmte Suchoption mit EINER Abhilfezeile und weicher Sperre
> `Optionsgruppe.WeichGesperrt`; rechts Ziel, Kandidatenzeile live, Feinraster nur bei G, Rechenknopf), Suchraum als eine Karte je
> Einheit (Kopf Name · Kenndaten · „variieren", Rumpf je Methode im `Formularraster` mit eigener Beschriftung je Feld statt der
> Mockup-Kompaktzeile — iU8‑E‑2, Fußzeile „Kandidaten dieser Einheit", nicht eingeschaltet gedimmt „fest: …"), die Neun-Spalten-Tabelle
> ist gefallen; „Bestes Ergebnis" nennt unter S die Bestückung; `SpeicherFlottenGroessenAnsicht.Methode` zeigt unter S Stückzahlkurve
> bzw. n₁ × n₂-Karte, Rasterkarte/Schnitte/Schieber entstehen dann nicht; „Kandidat übernehmen" setzt unter S `AnzahlVon = AnzahlBis`;
> Schritt 1 mit Auswahlkästchen je Karte und Knopf „Ausgewählte Einheiten in Projekt übernehmen" (Rückfrage mit den zwei Zahlen aus
> `Vorschau`, Lesemodus als Delegat `Schreibgeschuetzt` aus der Hülle — Hausregel S‑2); 37 Ressourcen de/en; UI-Tests 3 961 → **3 973**.
> Acht begründete Abweichungen in Konzept 8.8. Mockup v2 auf den Entscheid nachgezogen (Herkunftspille weg). **Wiki:** Bedienungsseite
> Revision 565 (Schritt 1 Übernahme, Schritt 4 drei Suchmethoden, Karte je Einheit), Rechenwegseite Fassung 7 Revision 566
> (Tabelle der drei Methoden mit Kandidatenzahl, Kostenregel je Stück, die drei Ablehnungen); drei Fassungs-Erwartungswerte der
> H13-Wächter nachgezogen. Der Projektlauf ist nicht berührt (`SpeicherFlottenProjektCtrl` setzt `FlottenGroessenOptimieren` weiter auf
> `false`), Referenzbasis R7 und Einfrierregel SP‑O‑8 bleiben. **Gate sept56 auf `80e2788f`:** Kern 2 759, UI 3 973, Engine 437,
> KiKern 488, 5 eindeutige Warnungen, SQL 0 von 1 351, ChartProben 64 Bilder und 15 Gegenproben, 0 Verstöße, Referenzlauf 5/5 byte-gleich gegen R7; en-US-Lauf
> grün. Windows-Abnahme der Station 4 und des Übernahmeknopfs beim Anwender offen.

## #248 — Kleinpunkte ohne Auftrag: Zeilenhöhe im Kommentar, Fassungsnummer statt Tiefenkopie (13.09.2026, Nachtrag aus dem Merge)

> **#248 (13.09.2026, Anwender: „Kleinpunkte ohne Auftrag: setze um") — umgesetzt (K1 `a1ae098a`, K2 `844efed0`, Merge `d6dfe01d`,
> Agent Opus; 14 Dateien, +377/−51).** **K1:** Die Kommentare zur Höhe der Katalog- und Projektliste (`Katalograhmen.razor`,
> `Zweispaltenauswahl.razor`, `epos-ui.css`) leiteten sie noch aus „45 px Kopfzelle und 37 px Zeilenhöhe" her — das waren die Maße des
> Mockups zu W14a‑E‑10; seit #240 (Zellenpolsterung auch im QuickGrid) ist die Zeile 53,0 px hoch. Im Browser nachgemessen (Rasterprobe-Wirt,
> Playwright, 1 300 × 900): Katalogliste 456 px innen, Kopfzelle 52,5 px, sieben ganze Zeilen (statt elf); Projektliste 190 px innen,
> Kopfzelle 29,9 px, drei ganze Zeilen (statt vier). Nur Kommentare berichtigt, kein Maß geändert; `grep "37 px"` trifft in EPOS.UI nur
> noch die ausdrückliche Rücknahme im Test; Katalogfilter-Konzept 5.6.5 und Anhang A datiert nachgetragen. **K2:** Der Rest aus #245 —
> `StromspeicherAuslegungSeite.FlotteGeschrieben()` ersetzte die Flottenkonfiguration nach JEDER gemeldeten Eingabe durch eine
> JSON-Tiefenkopie, nur damit drei Editoren an der neuen Referenz ihre Arbeitskopie auffrischen. Gemessen vor dem Umbau (Median über
> 1 000 Aufrufe): 102–116 µs je Kopie bei zwei Einheiten, 527–783 µs bei zehn Einheiten mit je 20 Rainflow-Punkten, 3,3 ms und 1,6 MB
> je Kopie der Auslegungseingaben mit eingelesener 8 760er Zeitreihe. Analyse: An der neuen Referenz hing NUR das Auffrischen der drei
> Arbeitskopien — nicht das Veraltet-Signal (`Geaendert()`), nicht Kandidatenzähler und Vorprüfung der Station 4 (lesen unmittelbar),
> nicht die Ergebnis-Entkopplung (`FlotteStarten()` kopiert zum Rechenzeitpunkt), nicht Speichern, Blattwechsel, „Kandidat übernehmen"
> oder die Projektübernahme aus #247. Umbau: ein nicht serialisiertes `_fassung` auf der Seite, das allein `Geaendert()` hochzählt, als
> Parameter `Fassung` an `SpeicherFlottenEditor`, `SpeicherFlottenBetriebEditor` (über `PeakZielBlock`) und `SpeicherAuslegungEditor`;
> sie frischen auf, wenn Referenz ODER Fassung wechselt; `FlotteGeschrieben()` ist nur noch `Geaendert()`; zwei doppelte Kopien in den
> Meldewegen gefallen. Nebenbefund behoben: `SpeicherAuslegungEditor.OnParametersSet` verglich `Wert` mit seiner eigenen Kopie und
> kopierte deshalb bei JEDEM Zeichenlauf (mit Zeitreihe 3,3 ms), jetzt Eingangsreferenz und Fassung. Je Tastendruck bleibt nur die
> handgeschriebene Kopie, die das meldende Blatt selbst herausgibt (Grenze „halbfertig ↔ gilt"). Drei neue bunit-Wachen (keine neue
> Flotteninstanz je Eingabe per `Assert.Same`, Netzblock und Betriebseditor schreiben denselben Stand, ein Name aus Schritt 1 steht in
> der Suchraumkarte), die drei #245-Wachen grün; UI-Tests 3 973 → **3 976**. Hausregel in `EPOS.UI/CLAUDE.md` ergänzt („wer eine Kopie
> nur macht, damit eine Referenz sich ändert, nimmt eine Fassungsnummer"), Konzept Stromspeicher-Dialoge 8.8 mit Messtabelle. Offen
> (kein Auftrag): die Kopie der Auslegungseingaben mit Zeitreihe (3,3 ms) bleibt einmal je Tastendruck auf Blatt 2 — die Meldegrenze
> des Editors. Kein Rechenweg berührt. **Gate sept57 auf `d6dfe01d`:** Kern 2 759, UI 3 976, Engine 437, KiKern 488, 5 eindeutige
> Warnungen, SQL 0 von 1 351, ChartProben 64 Bilder und 15 Gegenproben, 0 Verstöße, Referenzlauf 5/5 byte-gleich gegen R7; en-US-Lauf grün. Windows-Abnahme beim
> Anwender offen.

## #249 — Herleitungszeile der Investition unter der Jahresprojektion (13.09.2026, Nachtrag aus dem Merge)

> **#249 (13.09.2026, Anwender „Herleitungszeile Investition: ja") — umgesetzt (Merge `c0d85c39`, Agent Opus).** Anlass war die
> Anwenderfrage vom 12.09.: 150 €/kWh × 1 395 kWh sind 209 250 €, die Station „5 Ergebnis" nannte unter der Jahresprojektion aber
> 284 250 € — die Differenz steckte im festen und im Leistungsanteil, die die Summenzeile nicht auswies. Jetzt steht unter
> „Investition: … €" je Einheit eine Herleitungszeile des Hauses (`Herleitungszeile`, Text = Name, Formel =
> „fest + kWh × €/kWh + kW × €/kW = Summe"), ab zwei Einheiten eine Summenzeile darunter, einmal der Hinweis, dass die Leistung die
> größere der beiden Leistungen ist. Die Zahlen liefert `SpeicherFlottenAnzeigeCtrl.Investitionsherleitung` aus der Formel
> `FlottenWirtschaftlichkeit.Investition` (jetzt public), die auch den Kapitalwert speist — der Kern-Test hält die Summe der Zeilen
> gegen `InvestitionEuro` der gerechneten Studie und führt den Fall des Anwenders (75 000 € fest, 1 395 kWh × 150, 500 kW × 0 =
> 284 250). Ressourcen `FLOTTE_PROJ_INVEST_*` de/en, bunit-Fälle (eine Einheit ohne Summenzeile, zwei mit), Konzept
> Stromspeicher-Dialoge 2.2, Wiki-Bedienungsseite Station 5 (Revision 567). Kein Rechenweg berührt. **Gate sept58 auf `c0d85c39`:**
> Kern 2 764, UI 3 981, 5 eindeutige Warnungen, Referenzlauf 5/5 byte-gleich gegen R7; en-US-Lauf grün. Windows-Abnahme beim
> Anwender offen.

## #250 — Wiki „Programm Dokumentation" ohne Änderungshinweise (13.09.2026, Nachtrag aus dem Merge)

> **Anwenderauftrag 13.09.2026:** „entferne aus der wiki unter Programm Dokumentation die Änderungshinweise, weis zum
> Beispiel "seit dem <datum>…"." — zusammen mit der neuen Wiki-Regel der Wurzel-`CLAUDE.md` (Fachseiten beschreiben nur
> die Funktion, wie sie ist; Änderungskommentare nur im „Update-Logbuch").
>
> **Umsetzung (zwei Agenten Opus in Worktrees, Merges `f867255e` und `7846a234`):** Rubrik Berechnung — die 13
> Rechenwegseiten und der Index aus `EPOS.Kern/Allgemein/Hilfe/Berechnung/` ohne Datums-, Auftrags-, Befund- und
> Entscheidvermerke (rund 61 Stellen), Kopfblock `Seite | Fassung n | Rechenkern` ohne Stand; zuvor Live-Stand von
> Heizkessel (Handbereinigung) und Photovoltaik (Abschnitt „Modul, Strang und Wechselrichter — Prüfung und Auslegung",
> rund 200 Zeilen, nur im Wiki entstanden) in die Quellen übernommen. Wächter `BerechnungsHilfeTests`: Kopfblock ohne
> Datum, neuer Fall `Keine_Seite_der_Rubrik_traegt_einen_Aenderungshinweis` mit 13 benannten fachlichen Ausnahmen;
> Zwillingsliste in `EPOS.UI.Tests/BerechnungsknopfTests` nachgezogen. Bedienungsseiten — vier bestehende Quellen
> unter `Projekte/Wiki/` (Stromspeicher, Simulation, Hilfe-Assistent, Varianten) bereinigt, fünf neue Quellen aus dem
> Live-Stand (Emissionen, Photovoltaik, Pufferspeicher, Simulationsergebnisse, Wirtschaftlichkeit) plus Kosten (für
> #251); Kopfblock dreizeilig ohne Stand; Konzept Hilfesystem Regel 2 und Quellentabelle (zehn Seiten). Fachliche
> Treffer der Tabuliste („Vorschlag zur Entscheidung", Spalte „Geändert", „Bezugsspitze vorher → nachher") bleiben.
> **Gate sept60 auf `ef432b9f`:** GRÜN — Kern 2 779, UI 3 987, SpeicherEngine 437, KiKern 488, SpeicherPlanung 27 (1 übersprungen), Formularkarte 122; 5 eindeutige Warnungen (alle vorbestehend); SQL-Dialektprüfer 0 von 1 362; ChartProben 64 Bilder; Referenzlauf 5/5 byte-gleich gegen R7; en-US-Lauf grün. Gate sept60 auf `c212f259` war rot: 17 Fälle des Zwillingswächters `EPOS.UI.Tests/BerechnungshilfeTests` (Kopfblock erwartete „Stand: 2026-", neun LaTeX-Befehle des übernommenen PV-Abschnitts fehlten, eine unnummerierte Anzeigegleichung) — behoben mit #250C (Merge `ef432b9f`: Wächter auf „Fassung n", Befehlsliste ergänzt, Umrechnung der Temperaturkoeffizienten als Fließtext). **Upload (Bot-Konto, Kennwort nur als Umgebungsvariable):** 24 Seiten am 13.09.2026, Probe vor dem Upload alle 24 Live-Stände gleich der Kopie vor der Bereinigung, Nachprobe per `action=raw` alle 24 zeichengleich zur Quelle — Rechenwegseiten Revisionen 568–580 (BHKW 568, Brauchwasser 569, Heizkessel 570, Photovoltaik 571, Prozesswärme 572, Pufferspeicher 573, Simulationsablauf 574, Solarthermie 575, Strombedarf 576, Stromspeicher 577, Wärmebedarf 578, Wärmepumpe 579, Wärmequelle Erdreich 580), Rubrikseite Berechnung 581, Bedienungsseiten Stromspeicher 582, Simulation 583, Hilfe-Assistent 584, Varianten 585, Emissionen 586, Photovoltaik 587, Pufferspeicher 588, Simulationsergebnisse 589, Wirtschaftlichkeit 590, Kosten 591; Fassungsnummern der Rechenwegseiten unverändert (12 × Fassung 3, Stromspeicher Fassung 7).
> **Logbuch:** 20 Einträge als Entwurf gesammelt; Versionsnummer beim Anwender erfragt, Upload erst nach Antwort.
> **Befunde ohne Auftrag:** elf Live-Rechenwegseiten setzen eingerückte Fortsetzungszeilen als `<pre>`-Kästen (BHKW 16);
> `BerechnungsSeite.Stand` ohne Leser; „Was ist neu"-Liste hinter `{{Anker|ablauf-neu}}` der Simulationsseite doppelt
> Teile des Abschnitts darüber.

## #251 — Wiki ohne Hersteller- und Produktdaten, Regel und Wächter (13.09.2026, Nachtrag aus dem Merge)

> **Anwenderauftrag 13.09.2026:** „prüfe die Dokumentation auf der Wiki: es dürfen keine Herstellerdaten oder
> Produktdaten enthalten sein. Schreibe die Regel auch in die wiki .md Dokumentation".
>
> **Prüfung:** alle 138 Seiten des Hauptnamensraums (Kopien über `action=raw`), Wortgrenzen-Scan gegen die 25 Firmen
> und Typen der Katalogtabellen von `Kenndaten_Test.sqlite` plus bekannte Hersteller und Typcode-Muster, danach drei
> Leseagenten (Sonnet) mit Urteil über jede Seite. Befund: genau zwei Fundstellen — Bedienungsseite Stromspeicher
> („Growatt WIT 1", „Growatt WIT 2" als Beispiel laufender Nummern) und Bedienungsseite Kosten („Wärmepumpe
> „CS6800iAW", Photovoltaik „Jinkosolar"" als Beispiel der Trägerliste). Datenquellen und Formate (VDI 3805, CEC-Liste,
> PVsyst .PAN/.OND, bslib, GEMIS) sind keine Produktdaten und bleiben; ein Praxisbeispiel in Grundlagen/Hydraulikschemata
> nennt weder Hersteller noch Typ.
>
> **Umsetzung (Agent Opus, Merge `be09f8e0`, Glättung `c212f259`):** beide Beispiele neutral („Speicher A 1", „Speicher
> A 2"; „Wärmepumpe „Anlage A", Photovoltaik „Modul B""); Kosten bekam dafür eine Repo-Quelle aus dem Live-Stand
> (`6f960c92`). Regel als Abschnitt 13 „Inhaltsregeln für die Wiki-Seiten" im Konzept Hilfesystem (13.1 nur der
> gültige Stand, 13.2 keine Hersteller- und Produktdaten: Regel, Grund, was erlaubt bleibt, wie Beispiele geschrieben
> werden, Prüfung) und als Sätze im Wiki-Punkt der Wurzel-`CLAUDE.md`. Wächter `EPOS.Kern.Tests/WikiProduktdatenWacheTests`
> (5 Fälle) hält die 25 Repo-Wikiquellen gegen 257 Katalognamen der Testdatenbank (Firma/Hersteller/Bezeichner der acht
> Kataloge, Platzhalter und Kurznamen ausgenommen), eine feste Liste von 55 Herstellern und ein Typcode-Muster mit
> Normausnahmen; Seiten ohne Repo-Quelle prüft die Orchestrierung vor jedem Upload. **Gate sept60 auf `ef432b9f`:** siehe #250.
> **Upload:** Stromspeicher und Kosten zusammen mit #250 (Revisionen 582 und 591).

## #252 — Update-Logbuch 1.2.0.0 und Regel zum Upload-Rhythmus (13.09.2026, Nachtrag aus dem Merge)

> **Anwender 13.09.2026:** Versionsnummer 1.2.0.0 für die 20 Logbuch-Einträge; „aktualisiere die wiki nicht nach jeder
> änderung, maximal ein mal pro woche oder bei wesentlichen Änderungen. Nehme die Regel in die entsprechenden .md
> Dokumentation auf."
>
> **Umsetzung:** Seite „Update-Logbuch" — Live-Stand gleich der Kopie, Abschnitt „Version 1.2.0.0 – September 2026" um die
> 20 Einträge aus #250 (nach Datum, neueste zuerst) ergänzt, Einleitungssatz auf „An der Bedienung ändert die Umstellung
> nichts." geschärft; Revision 592, Nachprobe per `action=raw` zeichengleich, 40 Listenpunkte gerendert ohne Fehler. Regel
> als Abschnitt 13.3 im Konzept Hilfesystem (Regel, Warum, Ablauf mit „Upload ausstehend" in der Statusdatei) und als Absatz
> im Wiki-Punkt der `CLAUDE.md`. Kein Code, kein Gate nötig; die zwei Dokumentationswachen laufen vor dem Push.

## #253 — Suchraumfelder lassen sich leeren, Fortschrittsbalken in Station 4 (13.09.2026, Nachtrag aus dem Merge)

> **Anwenderbefund 13.09.2026 (zwei Bildschirmfotos, Stromspeicher-Auslegung › 4 Optimierung, „Größe suchen"):**
> „Fehler in Eingabefeld ‚Kapazität Schritt': 1 bleibt stehen, Eingabe nicht korrekt möglich"; „Sekundärfehler –
> Anzahl Auslegungskandidaten" (123 504 von höchstens 10 000, Lauf abgewiesen); „prüfe alle Felder – gleiches Problem"
> (alle Suchraumfelder zeigen nur noch eine Ziffer); „Progress bar bei Berechnung nicht mehr vorhanden".
>
> **Befund (Agent Opus, bunit-Prüfstand mit den Werten der Fotos):** H1 „Kandidatenzähler baut je Tastendruck das Raster"
> ausgeschlossen — `FlottenOptimierer.Kandidatenzahl` multipliziert Stützstellen, ein Tastendruck der ganzen Ansicht kostet
> 0,39–0,97 ms, auch bei 1,2 Millionen Kandidaten. H3 zutreffend: Ein geleertes Zahlenfeld meldet `null`, `OptimierungBlock.
> ZahlSetzen` verwarf `null`, der Suchraum behielt die letzte Ziffer, und `Zahlenfeld.OnParametersSet` schrieb sie zurück —
> „1 bleibt stehen" wörtlich; die Schreibmarke stand danach hinter der Ziffer, weitere Zeichen landeten dahinter, ein Feld ließ
> sich nie leeren. Die Kandidatenzahl 123 504 (496 × 249, Obergrenze eingeschlossen) plus 19 Feinrasterpunkte war richtig und
> die Folge des hängenden Felds. Der Fortschrittsbalken wurde gesetzt und gezeichnet, stand aber am Kopf der Ansicht, seit der
> Rechenknopf ans Ende des Blattes Optimierung gewandert war — außer Sicht; Simulationslauf nicht betroffen (Wache grün).
>
> **Umsetzung (Merge `79a1a572`, Commit `904255b3`):** `Zahlenfeld`/`Ganzzahlfeld` merken „geleert" und schreiben bis zum
> nächsten Tastendruck nichts zurück (Prüfhilfe `Geleert`); `OptimierungBlock` schreibt bei `null` 0 („kein Wert"), das Raster
> gilt benannt als ungültig, Kandidatenzeile „Raster ungültig", Vorprüfung sperrt den Rechenknopf und nennt das Feld;
> `Fortschritt` unter dem Rechenknopf von Station 4, gespeist aus dem Wirt, der eigene Balken der Ansicht bleibt aus, solange
> Station 4 vorn steht. Keine Engine-Änderung, keine neue Ressource. Tests: `SchrittfeldUndFortschrittTests` (5),
> `ZahlenfeldTests`/`GanzzahlfeldTests` (+1, rot ohne Behebung), `FlottenKandidatenzaehlungTests` (8), `SimulationSeiteTests` (+1);
> #245/#248-Wachen grün. Referenzlauf 1046 byte-gleich. Konzept Stromspeicher-Dialoge Abschnitt #253; Wiki-Quelle Stromspeicher
> zwei Sätze (Upload ausstehend, gebündelt nach 13.3). **Gate sept63 auf `79a1a572`:** GRÜN — Kern 2 782, UI 3 996, SpeicherEngine 445, KiKern 488, SpeicherPlanung 27 (1 übersprungen), Formularkarte 122; 5 eindeutige Warnungen (vorbestehend); SQL-Dialektprüfer 0 von 1 363; ChartProben 64 Bilder; Referenzlauf 5/5 byte-gleich gegen R7; en-US grün. Gate sept63 auf `79a1a572` war rot durch einen fremden Bruch: Der Sync-Commit `17aa33fe` der Gegenseite hatte das Hauswerkzeug EposSqliteMigrator entfernt, `WP-Plan.sln` verwies weiter darauf (MSB3202, Vollbau rot, Tests auf alten Binärdateien); bereinigt mit `769a8fcb` (sln, windows.yml, Wurzel-CLAUDE.md-Tabellenzeile, EPOS.Kern/CLAUDE.md).
> **Offen:** #254 (Vorprüfung je Tastendruck mit Tiefenkopien und Datenbankzugriffen in der Anwendung messen und entkoppeln;
> Anwenderentscheid ausstehend). Ein geleertes Feld bleibt leer, auch wenn der Wirt von außen einen neuen Wert setzt, bis der
> Anwender wieder tippt.

## #255 — Feinraster nur als Ausschnitt um das Optimum (13.09.2026, Nachtrag aus dem Merge)

> **Anwenderfrage 13.09.2026 (Bildschirmfoto Größen-Sicht, Projekt „Stromspeicher Optimierung", Kopplung Kapazität und
> Leistung, Grobraster 50…500 kWh / 50…500 kW in Schritten von 50):** „Warum gibt es weiße Bereiche im Graf Kapitalwert über
> Kapazität und Entladeleistung?" — Befund: Die Zeilen 305,6 … 394,4 kWh sind Feinrasterpunkte der zweiten Phase; das
> Feinraster läuft nur auf der ersten Größenachse in Neunteln der Grobschrittweite um das Grob-Optimum (350 kWh bei 200 kW),
> die zweite Achse bleibt fest. Die Karte nahm alle vorkommenden Kapazitätswerte als Zeilen, darum war in den Feinzeilen nur
> die Spalte 200 kW besetzt, die übrigen Zellen Löcher (NaN, hell gezeichnet).
>
> **Anwenderentscheid 13.09.2026:** „Entferne das Feinraster in der Gesamtheit. Das Feinraster soll nur um das Optimum
> (höchster Kapitalwert) herum als Ausschnitt dargestellt werden, um das Optimum genauer darzustellen. Evtl. ist eine andere
> Darstellung besser geeignet."
>
> **Konzeptentscheid (Orchestrierung):** Weil das Feinraster eindimensional ist, wäre eine zweidimensionale Ausschnittkarte
> eine einzige Spalte. Der Ausschnitt ist deshalb eine Kurve über der Feinraster-Achse im Fenster des Feinrasters. Engine
> unverändert; ein zweidimensionales Feinraster wäre ein Rechenwegwechsel mit neuer Referenzbasis und bleibt ein eigener
> Entscheid.
>
> **Umsetzung (Agent Opus, Worktree, Commits `46e4653b`, `47dfd1c6`; Merge `5dc7ca2a`):**
> `SpeicherFlottenAnzeigeCtrl.Rasterdaten` nimmt nur Kandidaten der Phase Grob — Zeilen und Spalten sind wieder genau die
> eingegebenen Stützstellen, die zwei Gesamtschnitte (aus dem Raster abgeleitet) führen keine Feinpunkte mehr;
> `FlottenRasterdaten.Feinraster` samt Durchreichung entfernt, `FlottenSchnittdaten.Feinpunkte` und der Renderer-Parameter
> bleiben (der Ausschnitt nutzt sie). Neu `Ausschnittdaten`/`Ausschnittbild`/`Ausschnitttitel`/`Ausschnittbeschreibung`:
> alle Fein-Kandidaten plus die Grobpunkte desselben festgehaltenen Wertes im Fenster [min Fein, max Fein], aufsteigend,
> Doppelstellen einfach (Zulässigkeit, Kapitalwert, bei Gleichstand der Grobpunkt — dieselbe Ordnung wie `IstBesser` und
> „Feinraster gewinnt nur bei strikt besserem Wert"); Marke = `BesterKandidat`; Bild über `ChartRenderer.Schnittkurve`, dessen
> y-Achse sich auf die Werte skaliert. Ohne zweite Phase (kein Feinraster, Suchmethode Stückzahl oder Bewerten) `Leer` und
> kein Bild. Grob-Optimum an einer Stelle (`GrobOptimum`: bester zulässiger Grob-Kandidat, größter endlicher Kapitalwert, bei
> Gleichstand der zuerst gerechnete — die Engine führt es nicht eigens); `Kartenoptimum` markiert den besten Kandidaten,
> solange er im Grobraster steht, sonst das Grob-Optimum, und die SP-O-4-Fußzeile nennt dann Wert und Fundort. Ansicht:
> Ausschnitt als eigener Block direkt unter der Karte (neue Regel `.epos-flotte-groessen-spalte`, damit die Reihenfolge Karte →
> Ausschnitt auch unter 1000 px bleibt); Alt-Text = Ausschnitttitel. Ressourcen `FLOTTE_AUSSCHNITT_*` de/en. Tests:
> `SpeicherFlottenGroessenCtrlTests` 34 → 44, `SpeicherFlottenGroessenAnsichtTests` 21 → 25; ChartProben 64 Bilder grün,
> Proben `flottenschnitt_feinraster`/`flottenschnitt_feinpunkte_wirken` unverändert. Doku: Doku Mehrspeicher (Größen-Sicht,
> Phase sichtbar im Ausschnitt), Konzept Stromspeicher-Dialoge 2.5/7.9/8.8; `Konzept_Stromspeicher_EPOS-Plan.md` bewusst
> unberührt (Abschnitte 6.3/7.2 beschreiben den früheren Einzelspeicher-Optimierer). Wiki-Quelle Stromspeicher (Größen-Sicht,
> Phase 2) fortgeschrieben, Upload gebündelt (Anwenderregel 13.09.: höchstens einmal je Woche, zusammengefasst); zwei
> Logbuch-Einträge entworfen.
> **Gate sept65 auf `5dc7ca2a`:** GRÜN auf `5dc7ca2a` — Kern 2 792, UI 4 000, SpeicherEngine 445, KiKern 488, SpeicherPlanung 27 (+1 übersprungen), Formularkarte 122; 5 vorbestehende Warnungen (CS0108/CS0109/WFO0003); SQL-Dialekt 0 von 1 363; ChartProben 64 Bilder; Referenzlauf 5/5 byte-gleich gegen R7; en-US 0 FAIL.
> **Offen:** Windows-Abnahme; Sammel-Upload frühestens 20.09.2026 (Stromspeicher-Quelle aus #253 und #255, vier
> Logbuch-Einträge); #254 auf Anwenderentscheid.

## #254 — Vorprüfung in Station 4 ohne Datenbank je Tastendruck, entprellt (13.09.2026, Nachtrag aus dem Merge)

> **Herkunft:** Befund des Agenten aus #253 (13.09.2026): Jede Eingabe in einem Suchraumfeld von Station 4 „Optimierung"
> lief über `Geaendert()` der Auslegungsseite in `Dienste.Vorpruefen` → `StromspeicherAuslegungCtrl.Vorpruefen` →
> `SpeicherAuslegungCtrl.Vorbereiten` — die volle Vorbereitung eines Laufs mit Tiefenkopien, Datenbankzugriffen und
> Zeitreihenaufbau, obwohl ein Suchraumfeld weder Kosten noch Lastgang ändert. **Anwenderentscheid 13.09.2026:**
> „#254: Empfehlung" (messen, billige Prüfung sofort, teure entprellt, Zwischenspeicher).
>
> **Messung (Agent Opus, headless gegen die Testdatenbank, Projekt 1046 mit gerechnetem Jahreslauf, 100 Aufrufe mit je
> einem geänderten Suchraumfeld):** vorher Median 69,7 ms, Mittel 75,9 ms, sechs Datenbankvorgänge je Aufruf, 103
> Beschaffungen auf 103 Aufrufe; davon Vorbereitung 3,9 ms, `Eingang`+`Konfiguration` 64,7 ms (nur 2,7 ms für die 35 040
> Intervalle, der Rest zwei SHA-256-Kennungen über den JSON-Text der ganzen Reihe), Prüfung 0,8 ms. Nachher Median 2,5–2,9 ms,
> null Datenbankvorgänge, eine Beschaffung auf 103 Aufrufe; schnelle Stufe 0,24 ms. Der Kandidatenzähler war nie das Problem.
>
> **Befund:** Eingabenabhängig in der Vorbereitung sind nur Quellenwahl, Kostenquellen, übernommener Projektflottenstand,
> die Einheiten (daraus der aggregierte Parametersatz), Betriebsoptionen/Tarif und die Länge der Lastdatei; der Suchraum
> (Achsen, Schritte, Stückzahlen, Feinraster, Suchmethode) wird von der Beschaffung nicht gelesen.
> `FlottenPlausibilitaet.Pruefe` ist ohne Eingang aufrufbar (dann entfallen genau die zwei Peak-Ziel-Prüfungen); Raster- und
> Kandidatenhinweise kamen schon immer aus `FlottenOptimierer.Kandidatenzahl` in der Seite.
>
> **Umsetzung (Commits `11db798e` Messlauf, `57feaf63` Kern, `a9238c4f` Seite, `2911d0db` Doku, `7a05b827` Tiefenkopie;
> Merge `151949d7`):** Trennung an einer Stelle im Kern: `QuellenBeschaffen` (Datenbank und Zeitreihen) und
> `VorbereitenAusQuellen` (eingabenabhängiger Rest); `Vorbereiten` ruft beides, Studien- und Projektlauf unverändert.
> `StromspeicherAuslegungCtrl` speichert die beschafften Quellen samt Istreihe und aufgelösten Kostensätzen unter einem
> Schlüssel aus Lauf-Fassung, Projekt, Quellen-/Kostenwahl, Einheiten, Betriebsoptionen, Tarif und den Kennungen der
> Dateireihen (Name, Rolle, Länge); verworfen bei `LaufUebernehmen`, `Vorgaben` und jedem schreibenden Weg (Projektflotte
> aktivieren, Einheiten übernehmen, Größe und Leistungspreis schreiben). Wache: interner Zähler `Beschaffungen` — zwei
> Vorprüfungen mit geändertem Suchraum ergeben eine Beschaffung. `VorpruefenSchnell` (ohne Datenbank, ohne Eingang) als
> zweiter plattformfreier Nahteintrag, die Hülle reicht durch, keine Schalenänderung. Die Seite ruft je Tastendruck nur die
> schnelle Stufe; die volle Stufe läuft 400 ms nach dem letzten Zeichen (`CancellationTokenSource` + `Task.Delay` +
> `InvokeAsync`, kein `Task.Run`) und sofort beim Öffnen, beim Blattwechsel, nach neuen Vorgaben und vor dem Lauf;
> `EntprellungMs` ist Seitenparameter (400, 0 = sofort), `Dispose` bricht ab; die Hinweisliste bleibt eine Liste. Die
> zusätzliche `eingaben.Kopie()` in `Vorpruefen`/`PeakZielVorschlag` ist gefallen (`VorbereitenAusQuellen` kopiert selbst;
> Wache: der übergebene Stand bleibt zeichengleich). Tests: Kern 2 792 → 2 800 (`StromspeicherAuslegungCtrlTests`,
> `FlottenPlausibilitaetTests`, `VorpruefungMessungTests` mit Trait „Messung" ohne Zeitschranke, `Zaehlzugriff`), bunit
> 4 000 → 4 008 (`VorpruefungEntprelltTests`); #245/#253-Wachen grün. Referenzlauf 1046 byte-gleich (464 425 Werte).
> Doku: `EPOS.UI/CLAUDE.md` (Hausregel zwei Stufen), Konzept Stromspeicher-Dialoge; Wiki-Quelle Stromspeicher ein Absatz
> im Abschnitt „Der Lauf" (Upload gebündelt), Logbuch-Eintrag entworfen.
> **Gate sept66 auf `151949d7`:** GRÜN auf `151949d7` — Kern 2 800, UI 4 008, SpeicherEngine 445, KiKern 488, SpeicherPlanung 27 (+1 übersprungen), Formularkarte 122; 5 vorbestehende Warnungen (CS0108/CS0109/WFO0003); SQL-Dialekt 0 von 1 363; ChartProben 64 Bilder; Referenzlauf 5/5 byte-gleich gegen R7; en-US 0 FAIL.
> **Offen:** Windows-Abnahme; Messfall läuft im CI mit (rund 4 s), bei Bedarf nach `Proben/`; Sammel-Upload frühestens
> 20.09.2026 (Stromspeicher-Quelle aus #253/#255/#254, fünf Logbuch-Einträge).

## #256 — Prognosepflicht planender Betriebsziele benannt vor dem Lauf (13.09.2026, Nachtrag aus dem Merge)

> **Anwenderbefund 13.09.2026 (Bildschirmfoto Visual Studio):** Projektlauf eines Projekts mit aktivierter Speicherflotte
> bricht ab: „Die Speicherflotte für diesen Projektlauf ist ungültig oder konnte nicht geplant werden: Kein expliziter
> Prognose-Snapshot der Art VerifiziertBekannt ist verfuegbar. Ausweg: die Flotte im Auslegungsdialog deaktivieren oder die
> Eingaben vervollständigen." (`SimulationControl.SpeicherlaufAusfuehren` ← `Simulation_Stromspeicher_Ctrl` ←
> `Do_Simulation` ← `SimulationLaufCtrl.Laufen` ← `SimulationErgebnisHuelle.Laufen` ← `Kulturweitergabe.Starten`).
>
> **Ursache (Orchestrierung, belegt):** Die drei planenden Betriebsziele verlangen im `FlottenSimulator` je Planungsschritt
> einen Prognose-Snapshot der eingestellten Art (`WaehleSnapshot`). Vorgabe jeder Flotte ist `VerifiziertBekannt`; dafür
> nimmt der Kern nur archivierte Snapshots aus dem CSV-Import „Prognosen und Projektjahre…", die EPOS-Projektzeitreihen
> enthalten keine. Nur bei `Oracle` baut `SpeicherFlottenStudieCtrl.Eingang` den Snapshot „Idealwissen-Standortreihe" aus
> der Istreihe (Spezifikation 9.4: die Istreihe wird nie still als Prognose verwendet — Engine-Regel bleibt). Lücke: weder
> `FlottenPlausibilitaet.Pruefe` noch `SpeicherFlottenProjektCtrl.Pruefe` kannten die Regel, der Abbruchtext (#185-Muster)
> nannte die Ursache nicht. Kein Zusammenhang mit #254 (Diff geprüft, Prognosen-Weg unverändert). Unter Visual Studio
> erscheint der Wurf als „vom Benutzer nicht behandelte Ausnahme", weil er im Rechenthread liegt; die Anwendung fängt ihn
> in `SimulationErgebnisHuelle.Laufen`.
>
> **Anwenderentscheid 13.09.2026:** „Empfehlung für Auftrag #256: Umsetzen" — Vorbelegung des Informationsstands bleibt
> `VerifiziertBekannt` (bewusste Wahl des Wissensstands), Abhilfe per Knopf.
>
> **Umsetzung (Agent Opus, Worktree, Commits `445db7a1` Kern, `07f3111a` Oberfläche, `b4ed7ff7` Doku; Merge `c90cfe13`):**
> `FlottenPlausibilitaet.Prognosepflicht(konfiguration, prognosen)`: planendes Ziel (`FlottenPlanerLage.IstPlanend`) und
> `VerifiziertBekannt` und kein geladener Snapshot dieser Art (nur die Art, kein Horizont) → Hinweis `PrognoseFehlt` mit
> neuer Stufe `Problem` (blockierend); Text nennt Ziel, Informationsstand und beide Auswege (Idealwissen in Schritt 3 oder
> Prognosen laden in Schritt 2). Drei Wege rufen dieselbe Funktion: `FlottenPlausibilitaet.Pruefe` (beide Überladungen,
> damit `Vorpruefen` und `VorpruefenSchnell` aus #254), `SpeicherFlottenProjektCtrl.Pruefe` als Problemzeile (Muster
> `FLOTTE_PLANER_PROFIL`; Aktivierung und beide `Rechnen`-Wege scheitern benannt vor der Engine, der Abbruchtext des
> Projektlaufs trägt die Ursache über `ex.Message`), `SpeicherFlottenStudieCtrl.Rechnen` weist den Optimierungslauf nach
> der Vorprüfung ab (`FlotteRechnen` legt ihn als Meldung ab). Rechenknopf der Ansicht an der Stufe „Problem" gesperrt (mit
> Befund als `title`). Oberfläche: vierter Abhilfeknopf `IdealwissenSetzen` im `FlottenDiagnosebanner` (setzt
> `PrognoseArt = Oracle` über den Wirt → Fassung hoch, `Geaendert`, Vorprüfung frisch); derselbe Wortlaut als Zeile unter
> dem Auswahlfeld „Informationsstand" im `SpeicherFlottenNetzBlock`; Hinweiszeilen mit eigenem Zeichen und Fehlerfarbe für
> „Problem". Ressourcen `FLOTTE_MSG_PROGNOSE_FEHLT`, `FLOTTE_ABHILFE_IDEALWISSEN`, `KI_FRAGE_FLOTTE_PROGNOSE_FEHLT` de/en,
> KI-Kennung `FLOTTE_PROGNOSE_FEHLT` mit Wissensabschnitt. Tests: Kern +8 (`FlottenPlausibilitaetTests`,
> `SpeicherFlottenProjektCtrlTests`; `KiDialogaufrufTests` 15 → 16 Kennungen, `FlottenPlanerLageTests` mit Idealwissen),
> bunit +4 (`StromspeicherAuslegungBannerTests`). Referenzlauf 1046 byte-gleich (464 425 Werte). Doku: Konzept
> Stromspeicher-Dialoge (Diagnose und Abhilfen), Doku Mehrspeicher (Vorprüfungsregeln); Wiki-Quelle Stromspeicher
> (Schritt 3, Tabelle der Betriebsziele; Upload gebündelt), Logbuch-Einträge entworfen.
> **Gate sept67 auf `c90cfe13`:** GRÜN auf `c90cfe13` — Kern 2 811, UI 4 012, SpeicherEngine 445, KiKern 488, SpeicherPlanung 27 (+1 übersprungen), Formularkarte 122; 5 vorbestehende Warnungen (CS0108/CS0109/WFO0003); SQL-Dialekt 0 von 1 363; ChartProben 64 Bilder; Referenzlauf 5/5 byte-gleich gegen R7; en-US 0 FAIL.
> **Offen:** Windows-Abnahme am Projekt des Befunds; doppelter Ausweg-Satz im Abbruchtext (vorbestehend, Befund ohne
> Auftrag); der vierte Abhilfeknopf wird nur mit einem Ergebnis sichtbar, der gewöhnliche Weg ist die Hinweiszeile in
> Schritt 3; Sammel-Upload frühestens 20.09.2026.

## #257 — Lebensdauerkurve benannt geprüft vor dem Lauf (14.09.2026, Nachtrag aus dem Merge)

> **Anwenderbefund 14.09.2026 (Bildschirmfoto Visual Studio, Projekt 1050):** Projektlauf mit aktivierter Speicherflotte
> bricht ab: „Die Speicherflotte für diesen Projektlauf ist ungültig oder konnte nicht geplant werden: Rainflow-Kurve ist
> ungueltig. Ausweg: …" — derselbe Abbruchweg wie #256, innere Ausnahme aus `SpeicherEngine/FlottenRainflow.cs`.
>
> **Ursache (Orchestrierung, belegt):** `FlottenRainflow.Auswerten` verlangt für eine nicht leere Lebensdauerkurve je Punkt
> Endlichkeit, Entladetiefe in (0, 1], Zyklen bis EOL über 0 und keine doppelte Entladetiefe; eine leere Kurve ist zulässig.
> Die Kurve kommt nur aus dem Editor (Schritt 1, Block „Alterung"): „Punkt hinzufügen" legt 0 % / 0 Zyklen an, die Felder
> lassen 0 zu, zwei neue Punkte sind Duplikate. Weder `FlottenPlausibilitaet.Pruefe` noch `SpeicherFlottenProjektCtrl.Pruefe`
> kannten die Regel; der Lauf scheiterte erst in der Engine. Kein Zusammenhang mit #254/#256.
>
> **Anwenderentscheid 14.09.2026:** „#257: Empfehlung".
>
> **Umsetzung (Agent Opus, Worktree, Commits `21ad7a2b` Kern, `f8dc8c9c` Oberfläche, `6ac5163d` Doku; Merge `45daa16b`):**
> Die Bedingung steht einmal in der Engine — `FlottenRainflow.PruefeKurve(kurve)` liefert den ersten Mangel als
> `FlottenRainflowBefund` (Index in gelieferter Reihenfolge, `FlottenRainflowMangel`: NichtAusgefuellt, Entladetiefe außerhalb,
> Zyklen ≤ 0, Duplikat), `null` bei gültiger oder leerer Kurve; `Auswerten` ruft sie und wirft unverändert denselben Satz
> (Rechenweg unberührt). Kern: `FlottenPlausibilitaet.Lebensdauerkurve(konfiguration)` mit Kennung
> `LebensdauerkurveUngueltig`, Stufe `Problem`, gerufen aus `FlottenPlausibilitaet.Pruefe` (beide Stufen der Vorprüfung)
> und `SpeicherFlottenProjektCtrl.Pruefe` (Aktivierung, Projektlauf; Abbruchtext trägt die Ursache); Abweisung des
> Studienlaufs (`SpeicherFlottenStudieCtrl` ~543) und Rechenknopf-Sperre (`Vorpruefungssperre`) greifen generisch über die
> Stufe „Problem" aus #256 — je ein Prüffall belegt es. Text nennt Einheit, Punktnummer (1-basiert), Grund und beide Auswege
> (ausfüllen oder „Punkt entfernen"; leere Kurve zulässig); vier Gründe als Ressourcen de/en, KI-Kennung mit Wissensabschnitt
> (17 Kennungen). Editor: `FlottenPlausibilitaet.Kurvenbefund(einheit)` markiert die Zeile (`epos-flotte-feldraster--fehler`,
> `aria-invalid`) mit demselben Wortlaut darunter; der neue Punkt bleibt im Modell 0/0 (`double`, NaN bräche die
> JSON-Serialisierung) und wird sofort als „nicht ausgefüllt" markiert. Tests: SpeicherEngine 449 → 456, Kern +6
> (`FlottenPlausibilitaetTests`, `SpeicherFlottenProjektCtrlTests`, `KiDialogaufrufTests` 16 → 17), bunit +3
> (`SpeicherFlottenEditorTests`, `StromspeicherAuslegungBannerTests`). Referenzlauf 1046 byte-gleich (464 425 Werte).
> Doku: Konzept Stromspeicher-Dialoge und Doku Mehrspeicher (Vorprüfungsregeln), Wiki-Quelle Stromspeicher Schritt 1
> „Alterung" (Upload gebündelt), Logbuch-Einträge entworfen.
> **Gate sept68 auf `45daa16b`:** GRÜN auf `45daa16b` — Kern 2 819, UI 4 015, SpeicherEngine 456, KiKern 488, SpeicherPlanung 27 (+1 übersprungen), Formularkarte 122; 5 vorbestehende Warnungen (CS0108/CS0109/WFO0003); SQL-Dialekt 0 von 1 363; ChartProben 64 Bilder; Referenzlauf 5/5 byte-gleich gegen R7; en-US 0 FAIL.
> **Offen:** Windows-Abnahme am Projekt 1050; nur die erste beanstandete Einheit erzeugt einen Listenhinweis (Editor markiert
> jede Zeile; Sammelliste wäre ein Folgeentscheid); Sammel-Upload frühestens 20.09.2026.

## #258 — Der alte Berichtsweg „Projektvergleich + Bericht (alt)" ist gefallen (14.09.2026, Nachtrag aus dem Merge)

> **Anwenderauftrag 14.09.2026:** „EPOS-Plan Bericht alt entfernen" — und im Nachgang „alles Zugehörige entfernen, lokal".
>
> **Ausgangslage (belegt):** Die Berichtsseite trug neben „Erstellen" einen zweiten Knopf „Projektvergleich + Bericht (alt)"
> (`BK_BTN_VERGLEICH_ALT`). Er führte über den Rückruf `VergleichAlt` in `BerichtSeiteGaben.cs` auf
> `Views/Varianten/ProjektvergleichBericht.cs` — den Vorgängerbericht (OpenXML, eigene Vergleichslogik, eigene
> Kuchendiagramme), der die ganze Projektgruppe synchron und ohne Abbruch neu simulierte und dabei schrieb. Fachlich ist er
> seit dem Berichtsmodul (`BerichtCtrl`, `BerichtsDatenSammler`, `WordBerichtGenerator`, Bausteine im Kern) abgelöst; der
> Prüfbericht des Berichtsmoduls führte ihn als offenen Restpunkt (Δ auf Rohwerten nur im neuen Weg behoben).
>
> **Umsetzung (Agent Opus im Klon des Arbeitszweigs, Commits `36343c6` Code und Ressourcen, `e55b4e3` KI-Konzept,
> `e7c3539` verwaiste Reste):** `EPOS.UI/Seiten/Berichte/BerichtSeite.razor` ohne den Knopf, ohne die Parameter
> `VergleichAlt`/`VergleichAltText`/`TitelVergleich` und ohne die Methoden `VergleichFragen`/`VergleichLaufen`; `Frageart`
> führt nur noch `Keine, Erstellen, Oeffnen`. `WindowsFormsApplication1/Views/Bericht/BerichtSeiteGaben.cs` ohne die drei
> Wörterbuchschlüssel und ohne die Methode `VergleichAlt` — Parametersatz und `[Parameter]` bleiben deckungsgleich
> (`ParametersatzTests`). `Views/Varianten/ProjektvergleichBericht.cs` (822 Zeilen) gelöscht; `EnergieMengen.cs` bleibt, weil
> `BerichtsDatenSammler` die Brennstoffmengen daraus zieht. Sechs Ressourcenschlüssel des alten Wegs fielen aus beiden
> `.resx` (`BK_BTN_VERGLEICH_ALT`, `BK_BER_TITEL_VERGLEICH`, `BK_BER_TITEL_FEHLER_VERGLEICH`, `BK_BER_MSG_VERGLEICH_FERTIG`,
> `BK_BER_STATUS_FEHLER`, `BK_BER_DLG_FILTER_WORD`), `Resource.Designer.cs` über `designer_neu.py schreiben` neu erzeugt
> (6 214 → 6 208 Einträge, zweiter Lauf +0). **Die Grenze zieht der Knopf:** `BK_BER_BTN_SCHLIESSEN` und
> `BK_BER_TITEL_FEHLER` sind ebenfalls ohne Verwender, gehören aber zur Berichtsseite selbst und bleiben deshalb stehen
> (Anwenderentscheid 14.09.2026: nur entfernen, was am alten Weg hängt). Belegverfahren der Waisen: jeder
> `BK_BER_*`/`BK_BTN_*`-Name gegen den Volltext aller kompilierten `.cs`/`.razor` gehalten, den string-basierten Zugriff
> `ResourceManager.GetString` eingeschlossen; `Werkzeuge/Formularkarte.Tests/Pruefmuster/**` zählt nicht als Verwendung.
> Die Paketreferenz `DocumentFormat.OpenXml` ist aus `WindowsFormsApplication1.csproj` gefallen — keine `.cs` der Schale
> nutzt OpenXML mehr; der Kern behält das Paket, `Directory.Packages.props` ist unberührt. Doku: Lokalisierungskatalog
> nachgezogen (Gruppenzahlen 37 → 30), `CLAUDE.md` der Schale ohne den OpenXML-Spiegelstrich, KI-Konzept ohne die Aktion
> `variantenbericht_erstellen` (im Code nie gebaut) und mit Beleg auf `BerichtsDatenSammler.cs:345`.
> **Abnahme:** `WP-Plan.Kern.slnf -c Release` grün, 0 Fehler, 4 vorbestehende Warnungen; KiKern 488, SpeicherEngine 456,
> SpeicherPlanung 27 (+1 übersprungen), bunit 4 015 → 4 013 (die zwei Fälle des alten Wegs); gezielte Wachen grün
> (`BerichtSeiteTests`, `ParametersatzTests`, `DokumentationLinkWacheTests`, Lokalisierung). `EPOS.Kern.Tests` ist in der
> Cloud-Sitzung kein Gate: `Referenzlaeufe/Kenndaten_Test.sqlite` liegt dort nur als 133-Byte-LFS-Zeiger, `git lfs pull`
> scheitert am Proxy — daher auch kein SQL-Dialekt-Lauf und kein Referenzlauf. Der Rechenweg ist unberührt.
> **Offen:** Windows-Build der Schale und Gate am Gerät; `ProjektvergleichBericht.cs` ist im Arbeitsbaum des Anwenders von
> Hand zu löschen (die Cloud-Sitzung kann dort schreiben, aber nicht löschen); der Katalogabschnitt führt die
> Schlüsselfamilie weiter unter der Vorgängermaske `Views/Bericht/UcBericht.cs`, die es nicht mehr gibt — eigener Entscheid;
> `BK_BTN_SIMULIEREN` ist ebenfalls eine Waise, gehört aber zu einer anderen Maske; Logbuch-Eintrag zum entfallenen Knopf
> mit Versionsnummer beim Anwender zu erfragen.

## #259 — Energieträger-Trägerkarte: Einheiten, Preishistorie, Katalogwerte (14.09.2026, Nachtrag aus dem Merge)

> **Anwenderbefund 14.09.2026 (drei Bildschirmfotos, Energieträger-Dialog):** Heizwert und Brennwert tragen die Einheit
> „kWh/kWh" („sind mit Einheit kWh/Nm³"); im Block „Preishistorie" „funktioniert Speichern nicht", „die Eingaben werden nicht
> gespeichert"; dazu der Wunsch, die Stammdaten-Kosten aus der Administration in das Projekt übernehmen zu können.
>
> **Ursache (Orchestrierung, belegt; Agent bestätigt):** Die Hülle `WindowsFormsApplication1/Views/Kosten/EnergietraegerHuelle.cs`
> (1 819 Zeilen, ohne Test, vom Linux-Gate nicht gebaut) bildete die Einheit von Heizwert und Brennwert aus der gewählten
> Preisbasis („kWh/" + Basis), teilte beim Basiswechsel Heizwert, Brennwert und Leistungspreis durch den Faktor und bildete in
> `Nachziehen()` die Basiswerte als Anzeige × Faktor; `TraegerWaehlen()` setzte die gespeicherte Preisbasis, rechnete die
> Anzeige aber nicht um. Zusätzlich zeigte `["Nachrechnen"]` auf `Ansicht()`, das `Nachziehen()` nicht rief — Basiswerte,
> Formel und Effektivzeile blieben auf dem Stand des Ladens, gespeichert wurde der alte Wert. `Stand.Historie` wurde nirgends
> befüllt (der Kern-Leser hatte außer dem Speicheroptimierer keinen Aufrufer; der WinForms-Vorläufer rief `LoadHistory` beim
> Trägerwechsel und nach dem Speichern). Im Projektkontext fehlte jeder Weg zu den Katalogwerten.
>
> **Anwenderentscheid 14.09.2026:** „Empfehlung: Katalogübernahme eine einmalige Kopie", „Empfehlung #259: umsetzen".
>
> **Umsetzung (Agent Opus, Worktree, sieben Commits `15259039` … `2f1256a4`; Merge `c0ae674d`, dessen Betreff die Nummer #258 aus der Zählung vor der
> Windows-Synchronisation vom selben Tag trägt):** Heizwert und Brennwert sind
> Stoffwerte je Abrechnungseinheit (`kWh/Nm³`, `kWh/L`, `kWh/kg`); nur der Arbeitspreis folgt der Preisbasis, der Leistungspreis
> wird nicht mehr umgerechnet. Die Rechnung liegt als `EPOS.Kern/Controller/EnergietraegerPreiskarte.cs` ohne Datenbank im
> Kern; Formelzeile und Effektivprüfung laufen über die Basiswerte und nennen die Einheiten; nach jeder Feldänderung wird
> nachgezogen. Preishistorie: Laden beim Trägerwechsel und nach jedem erfolgreichen Speichern, Datum aus „Gültig ab", zweites
> Speichern am selben Tag aktualisiert, Spaltenkopf mit `kWh/<Abrechnungseinheit>`; im Katalogkontext entsteht keine Zeile,
> weil `energy_price.ID_Projekt` einen Fremdschlüssel auf `Tab_Projekt` trägt (Projekt 0 existiert nicht) — die Karte nennt den
> Grund (`ETV_HISTORIE_NUR_PROJEKT`). Knopf „Katalogwerte übernehmen" im Fuß der Preisgruppe, nur im Projektkontext: holt
> Arbeits-, Grund-, Leistungspreis, Heiz-, Brennwert und die drei Emissionswerte aus der Katalogzeile, setzt die Preisbasis auf
> die Abrechnungseinheit, meldet „Katalogwerte übernommen — noch nicht gespeichert"; geschrieben wird erst mit Speichern/OK
> (Historienzeile entsteht dabei). Bauweise: die Hülle liegt in `EPOS.UI.Daten/Kosten/` (Namensraum bleibt
> `WindowsFormsApplication1` nach Hausregel des Projekts), Windows-Adapter `Views/Kosten/EnergietraegerFenster.cs` (58 Zeilen);
> mitgezogen `EmissionskatalogHuelle`, `KostenprofilHuelle`, `SpotpreisImportHuelle`, `LeistungspreisReiheHuelle`,
> `EnergietraegerKatalogCtrl` → `EPOS.Kern/Controller/`, `NamensabfrageGaben` als plattformfreier Parametersatz. Tests:
> `EnergietraegerPreiskarteTests` (15), `EnergietraegerHuelleTests` (12, gegen die Arbeitskopie der Testdatenbank), sechs
> bunit-Fälle; Kern 2 819 → 2 846, UI 4 015 → 4 021; Referenzlauf 5/5 PASS und byte-gleich; SQL-Dialektprüfer 0 von 1 363;
> en-US-Lauf der betroffenen Klassen grün. Doku: Konzept Wirtschaftlichkeit konsolidiert, Wiki-Quelle Kosten (Upload
> gebündelt), drei Logbuch-Einträge entworfen. Zwei einmalige Ausreißer in `EPOS.UI.Tests/Seiten/Strom`
> (`VorpruefungEntprelltTests`, `SchrittfeldUndFortschrittTests`), einzeln und im Wiederholungslauf grün.
> **Gates:** sept69 auf `c0ae674d` rot durch einen zeitabhängigen Ausreißer (`SchrittfeldUndFortschrittTests`), sept70 rot
> durch den zweiten (`VorpruefungEntprelltTests`, en-US-Lauf) — beide Male alle übrigen Stufen grün, Referenzlauf 5/5
> byte-gleich; nach #260 **Gate sept71 auf `ffc8be4f`:** GRÜN auf `ffc8be4f` — Kern 2 846, UI 4 022, SpeicherEngine 456, KiKern 488, SpeicherPlanung 27 (+1 übersprungen), Formularkarte 122; 5 vorbestehende Warnungen (CS0108/CS0109/WFO0003); SQL-Dialekt 0 von 1 363; ChartProben 64 Bilder; Referenzlauf 5/5 byte-gleich gegen R7; en-US 0 FAIL.
> **Offen:** Windows-Abnahme der Nähte und fachlich an Erdgas E im Projekt; Preishistorie im Katalogkontext braucht einen
> Schemaentscheid (Fremdschlüssel lösen oder NULL zulassen, nummerierter Migrationsschritt); `KernwerteSpiegeln()` liest die
> Emissionswerte vor `EmissionenSpeichern()` — für Handeingaben der drei Kernarten ein Folgeauftrag; Sammel-Upload frühestens
> 20.09.2026.

## #260 — Strom-Prüfstände warten auf den gezeichneten Zustand (14.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Die Gates sept69 und sept70 zum Merge `c0ae674d` (#259) waren rot durch je einen Fall in
> `EPOS.UI.Tests/Seiten/Strom`: `SchrittfeldUndFortschrittTests.Die_Eingabefolge_im_Schrittfeld_kommt_vollstaendig_im_Modell_an`
> (de-Lauf) und `VorpruefungEntprelltTests.Drei_schnelle_Tastendruecke_ergeben_genau_eine_volle_Pruefung` (en-US-Lauf) — je 1 von
> 4 021, acht Einzelläufe der ersten Klasse einmal rot, Projektwiederholung 4 021/4 021; beide Gates liefen parallel zu einem
> Agentenbuild. Alle übrigen Stufen beider Gates grün, Referenzlauf 5/5 byte-gleich.
>
> **Ursache (Agent, belegt):** bunits synchrones `Input()`/`Click()` gibt das Ereignis in den Zeichenverteiler und kehrt zurück,
> ohne den Zeichenlauf abzuwarten. Die Seite legt je gemeldeter Änderung eine entprellte Vorprüfung auf; deren Fortsetzung
> meldet sich nach 400 ms aus dem Fadenvorrat über `InvokeAsync(StateHasChanged)` zurück und belegt den Verteiler. Unter Last
> rutscht der nächste Tastendruck dahinter, und der Sofort-Assert liest den Stand **vor** dem Zeichen (gemessen: Feldwert genau
> eine Eingabe zurück). Der Verdacht „der Zeitgeber schreibt den Modellwert ins Feld zurück" ist widerlegt: ein Fall, der den
> Zeitgeber bewusst zwischen zwei Eingaben feuern lässt, zeigt Feld, `Zahlenfeld._text` und Modell vor und nach dem Feuern
> gleich (Sperren in `Zahlenfeld.OnParametersSet`). Unter künstlicher Rechenlast (12 Fäden): mit Zeitgeber 7 Ausreißer in
> 6 Ansichten, ohne Zeitgeber 0 in 420 Eingaben. Kein Bedienfehler, die Komponente bleibt unverändert.
>
> **Umsetzung (Agent Opus, Worktree, Commit `134ae715`; Merge `ffc8be4f`, Betreff mit #259 aus derselben alten Zählung):** nur Prüfstände — `Auslegungshilfe.Schritt`
> wartet auf das gezeichnete Blatt; beide Klassen tippen über eine `Tippen`-Hilfe, die auf `Fassung` und Feldtext wartet;
> `SchrittfeldUndFortschrittTests` und `OptimierungStationTests` setzen `EntprellungMs = 0`; die überholten Entprellungen in
> `Drei_schnelle_Tastendruecke…` tragen eine im Lauf nicht ablaufende Zeit, nur die dritte eine kurze; der `Dispose`-Fall
> vergrößert Entprellzeit und Schranke im gleichen Verhältnis; neue Wache
> `Der_Zeitgeber_zwischen_zwei_Eingaben_ueberschreibt_das_Feld_nicht`. Kein `Task.Delay`, kein übersprungener Fall, keine
> geweitete Toleranz. Nachweis: 30/30 beide Klassen (de und `LANG=en_US.UTF-8`), 3 × 4 022/4 022 `EPOS.UI.Tests` (+1 unter
> en-US), 12/12 unter Rechenlast; Kern-Filter grün (488 · 456 · 27+1 · 4 022 · 2 846); keine neue Warnung; kein Referenzlauf
> nötig. Hausregel (Orchestrierung) in `EPOS.UI/CLAUDE.md`, Abschnitt Tests: nach `Input`/`Click` auf den gezeichneten Zustand
> warten, wo ein Zeitgeber läuft.
> **Gate sept71 auf `ffc8be4f`:** GRÜN auf `ffc8be4f` — Kern 2 846, UI 4 022, SpeicherEngine 456, KiKern 488, SpeicherPlanung 27 (+1 übersprungen), Formularkarte 122; 5 vorbestehende Warnungen (CS0108/CS0109/WFO0003); SQL-Dialekt 0 von 1 363; ChartProben 64 Bilder; Referenzlauf 5/5 byte-gleich gegen R7; en-US 0 FAIL.
> **Offen:** keine.

## #261 — Preishistorie: einzelne Preisstände löschen, Upsert je Kalendertag (14.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Anwenderwunsch 14.09.2026 mit Bildschirmfoto der Trägerkarte: „historische Energieträger werte sollen
> gelöscht werden können." Die Preishistorie zeigte zwei Zeilen mit demselben „Gültig ab 14.09.2026" (Basiseinheit m³ und
> Nm³, Arbeitspreis 0,0000 und 0,0500).
>
> **Befund Doppelzeile (Agent, belegt):** `energy_price.valid_from` ist TEXT und trägt im Bestand zwei Schreibweisen:
> Tagesstände (`2026-08-12 00:00:00`) aus der Trägerkarte und Zeitpunkte (`2026-08-09 20:33:16`) aus der Zuordnung —
> `WizardCtrl` und `EnergietraegerVarianteCtrl` binden `DateTime.Now`. `SqliteDatenzugriff.NormalisiereWert` wertet
> `DbParamTyp.Date` nicht aus; allein der CLR-Typ entscheidet (`DateTime` → `yyyy-MM-dd HH:mm:ss`). `HistorieSchreiben`
> band `gueltigAb.Date`, verglich `valid_from = ?`, verfehlte den Zeitpunkt desselben Tages und fiel in den INSERT-Zweig; das
> UNIQUE `unq_price_date` (Textgleichheit) ließ die zweite Zeile zu. In der Testdatenbank stehen beide Formen nebeneinander.
>
> **Umsetzung:** `Historienzeile.Id` aus `energy_price.id`; `EnergietraegerPreisCtrl.HistorieLoeschen(zeileId, traegerId,
> projektId)` löscht genau eine Zeile (Träger und Projekt als Riegel, Rückgabe Zeilenzahl). Prüfung und UPDATE des Upserts
> vergleichen den Kalendertag (`date(valid_from) = date(?)`); das Datum der getroffenen Zeile bleibt, neu angelegt wird zum
> Tagesbeginn. Hülle `EnergietraegerHuelle.HistorieLoeschen(zeile)` mit `HistorieLoeschenGrund` als Gaben neben
> `Speichern`/`SpeichernGrund`; Katalogkontext lehnt benannt ab (`ETV_HISTORIE_NUR_PROJEKT`), fehlende Zeile
> `ETV_HISTORIE_LOESCH_FEHLT`; danach `HistorieLaden()`. Oberfläche: Aktionsspalte am Ende der Historientabelle mit
> Löschknopf je Zeile, `Rueckfrage` mit Vorgabe Nein, Meldung wie beim Speichern. Windows-Schale unverändert (der volle
> Gabensatz wird auf den Dialog gesplattet). Ressourcen `ETV_HISTORIE_LOESCHEN`, `ETV_HISTORIE_LOESCH_TITEL`,
> `ETV_HISTORIE_LOESCHFRAGE`, `ETV_HISTORIE_LOESCH_FEHLT` de/en.
>
> **Abnahme:** Build 0 Fehler, keine neuen Warnungen; `EPOS.Kern.Tests ~Energietraeger` 52/52, `EPOS.UI.Tests ~Energietraeger`
> 73/73 (je 6 neue Fälle), `EnergietraegerDialogTests` dreimal 52/52; Wachen 43/43; SqlDialektPruefer 1 363 Texte, 0 Fundstellen;
> ResourceDesigner unverändert; Testdatenbank unberührt. `StromPreisCtrl.ArbeitspreisCtKwh` liest `energy_price` zum Stichtag —
> unverändert, kein Referenzlauf. Merge `46282032`.
>
> **Offen:** Vereinheitlichung der Zuordnung auf den Tagesbeginn (Bestandsdaten) nur auf Zuruf; Logbuch-Eintrag entworfen
> (Version 1.2.0.1), Upload gebündelt ab 20.09.

## #262 — Wärmequelle Erdreich: Erdkollektor und Erdsonde als eigene Rubriken (14.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Anwenderrückmeldung 14.09.2026 mit Bildschirmfoto der Überlagerung „Wärmequelle Erdreich — <Wärmepumpe>" aus
> der Simulationskonfiguration: „OK Button soll aus Dialog raus" und „Auswahl Erdkollektor und Erdsonde soll jeweils in einer
> Rubrik sein mit den jeweils relevanten Parametern (nicht alle gemischt wie jetzt)". Die Gruppe „Quellsystem" trug die
> `Optionsgruppe` und darunter Verlegetiefe, Fläche, Länge je Sonde und Anzahl Sonden in einem Formularraster; die Felder des
> nicht gewählten Zweigs waren nur gesperrt (Abweichung A‑4: getrennte Felder je Zweig, Umschalten überschreibt nicht).
>
> **Umsetzung (Teil Rubriken):** `QuelleErdreichDialog.razor` zeichnet zwei Rubriken (`Gruppenkopf`) **Erdkollektor** und
> **Erdsonde**, jede mit ihrem Optionsfeld als erster Zeile und nur ihren Feldern; die nicht gewählte Rubrik bleibt sichtbar,
> gesperrt und behält ihre Werte, gedimmt über `epos-erdreich-zweig--ruht` (Farbe `--epos-text-sehr-leise`, keine neue Farbe).
> Damit die Wahl EINE bleibt, bekam der Baustein `Optionsgruppe` die Gaben `NurEintrag` (zeichnet genau einen Eintrag,
> `Eintraege` bleibt der Stand der Wahl) und `Gruppenname` (geteilter HTML-Name mehrerer Aufrufe); der Einzelaufruf meldet keine
> eigene `radiogroup`, behält das `aria-label`. `SystemUmschalten`, `KOLLEKTOR`/`SONDE`, `Zweigfelder` und die Prüfmeldungen
> unverändert; die übrigen 20 Wirte der `Optionsgruppe` ebenso. Ressourcen `SIMQ_ERDREICH_RB_KOLLEKTOR_WAHL` und
> `SIMQ_ERDREICH_RB_SONDE_WAHL` de/en über `QuelleErdreichHuelle`; `RB_KOLLEKTOR`/`RB_SONDE` sind Rubriktitel, `GB_QUELLSYSTEM`
> bleibt Gruppenname der Wahl. Hausregel in `EPOS.UI/CLAUDE.md` („Eine Wahl darf über mehrere Rubriken laufen … nie ein nacktes
> `<input type="radio">`"). Wiki-Quelle `EPOS.Kern/Allgemein/Hilfe/Berechnung/Wärmequelle Erdreich.wiki`: Herkunftsspalte nennt
> die zwei Rubriken (kein Upload).
>
> **Abnahme:** Build 0 Fehler, keine neuen Warnungen; `QuelleErdreichDialogTests` 32 (3 neu), dreimal grün; `OptionsgruppeTests`
> 16 (4 neu); `SimulationKonfig` 38; `EPOS.UI.Tests` 4 027 grün; Wachen Dokumentation-Link 7, Repository-Ordnung 10,
> Wiki-Produktdaten 5 grün; ResourceDesigner unverändert. Kein Rechenweg, kein Referenzlauf. Merge `5b0df675`.
>
> **Offen:** Nachtrag OK-Knopf nach Anwenderentscheid (Lesart a/b, Reichweite auf die sieben Simulationsdialoge);
> vorbestehende Tabuwörter der Wiki-Quelle („vorher", „Befund" als Prüfbefund) nur auf Zuruf; Logbuch-Eintrag entworfen
> (Version 1.2.0.1), Upload gebündelt.

## #263 — Kostenverwaltung: Zeilenaktionen behalten ungespeicherte Eingaben (14.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Anwenderbefund 14.09.2026 mit Bildschirmfoto der Kostenverwaltung (Administration): „Bei zufügen von Position
> werden zuvor eingegebene Werte auf null gesetzt."
>
> **Ursache (belegt):** `PositionAnlegenMit` schreibt die neue Position sofort (`PositionNeu`, wie `btnPositionNeu_Click` im
> Vorbild) und ruft danach `KontextLaden(_stand.VarianteId)`. Die Windows-Hülle `KostenKomponenteHuelle` baut in
> `VorlagenRasterAufbauen`/`ProjektRasterAufbauen` bei jedem Laden `_bindungen` und `_zeilen` neu aus der Datenbank. Die Regel
> Ä12/Ä19 („die Änderung lebt bis Speichern nur im Objekt") trug nur, solange niemand die Objekte austauschte — beim Auffrischen
> war alles Ungespeicherte weg. Dieselben Zeilen stehen in `LoeschenFragen` und `UebernahmeFertigMachen`. Der Bestandsfake der
> bunit-Tests gab stets dieselben Objekte zurück, deshalb blieb der Befund unsichtbar.
>
> **Beleg:** Fünf neue Fälle in `KostenKomponenteDialogTests` mit einem datenbankartigen Fake (Ablage, aus der jeder `Laden`-Aufruf
> neue `KostenPositionZeile`-Objekte baut): vor dem Fix vier rot (Fußknopf „+ Position hinzufügen", Neuzeile, „Position löschen",
> „In Projekt übernehmen" — Satz fällt von 1500 auf den Datenbankwert 1200, ebenso `Stand.Zeilen[0].Satz` und der Summenfuß);
> der fünfte Fall (`Ein_Kontextwechsel_laedt_ohne_Uebertrag`) hält die gewollte Grenze fest.
>
> **Umsetzung:** Fix ausschließlich im plattformfreien `EPOS.UI/Dialoge/Kosten/KostenKomponenteDialog.razor`:
> `KontextLaden(int? varianteId, bool eingabenUebernehmen = false)`; `EingabenUebertragen` überträgt nach dem Laden die
> ungespeicherten Eingaben bisheriger Zeilen auf die neu gebauten Objekte gleicher `Id` — nur `Bezeichnung`, `BemessungId`, `Satz`,
> `Nutzungsdauer`, nur wo sie vom geladenen Stand abweichen; je Zeile `Nachziehen` (Einheit, Betrag, Kette, Kurztexte), danach
> `SummenNachziehen()`; `ReferenceEquals`-Wächter gegen identische Objekte. Gesetzt für Hinzufügen (beide Wege), Löschen,
> Übernehmen; Kontextwechsel (Eintrag, Kategorie, Variante, Variante neu/gelöscht) laden ohne Übertrag; „Abbrechen" verwirft weiter
> alles; nichts wird still gespeichert; die neue Position kommt weiter mit Einheit, Betragstext, Kette und Kurztexten aus der
> Hülle. `Zahlenfeld`: `@key="zeile.Id"` bleibt, Instanz wird wiederverwendet, Sperren `Fehlerhaft`/`_geleert` wirksam.
> Kopfkommentar und Hausregel in `EPOS.UI/CLAUDE.md` (Abschnitt Zustand, Meldung, Leerzustand). Windows-Schale unverändert.
>
> **Abnahme:** Build 0 Fehler, keine neuen Warnungen; gefilterter Lauf 76/76; `KostenKomponenteDialogTests` dreimal 46/46;
> `EPOS.UI.Tests` 4 025/4 025; Wachen 17/17. Kein Rechenweg, keine Ressourcen, kein Referenzlauf. Merge `84ea142d`.
>
> **Offen:** Kontextwechsel verwirft still (Rückfrage nur auf Zuruf); Zeileneditor und Gesetzeskatalog ohne Übertrag
> (`EditorFertig` der Windows-Hülle zieht `z.Bezeichnung` nicht nach); Nachweis am laufenden Windows-Build; Logbuch-Eintrag
> entworfen (Version 1.2.0.1), Upload gebündelt.

## #264 — Konzept Nutzungsdauer und AfA je Technik (14.09.2026, Nachtrag aus dem Konzeptcommit)

> **Anlass:** Anwenderwunsch 14.09.2026: „in allen Kostendialogen soll die Nutzungsdauer nach Technik/Kategorie
> standardmäßig vorbelegt werden können. Grundlage ist eine eigene AfA-Tabelle (Absetzung für Abnutzung / Nutzungsdauer,
> editierbar) unter Administration an geeigneter Stelle."
>
> **Befund (Sonnet-Agent):** Nutzungsdauer liegt je Vorlagenposition (`Tab_KostenVorlagePosition.Nutzungsdauer`, Saat
> NULL) und je Projektposition (`Tab_ProjektWerte.Nutzungsdauer` samt Worst/Best); `KapitalwertRechner` setzt n < 1 auf T;
> zehn Komponenten in `Tab_KostenKomponente`, `Tab_Kostenfaktor` flach; Menü Administration → Kostenverwaltung
> (Kostenvorlagen, Energieträger); keine Technik-Vorgabe im Bestand. Ein zweiter Befund derselben Runde (Energieträger je
> Komponente, ein Preis je Trägertyp im Projekt) ging in #268 auf.
>
> **Ergebnis:** `Dokumentation/aktuell/Konzept_Nutzungsdauer_AfA_EPOS-Plan.md` (Commits `9136b233`, `4453eaec`): Ist-Stand,
> Zielbild 2.1–2.7 (Tabelle `Tab_Nutzungsdauer` STRICT je Komponente und Positionsart mit Nutzungsdauer, steuerlicher AfA,
> Instandsetzungs- und Wartungssatz, Quelle, Standardkennung, Sortierung; `NutzungsdauerID` an Vorlagen- und Projektposition;
> Vorbelegung mit Herkunftsanzeige in den Kostendialogen; Administrationsdialog unter Kostenverwaltung; Startwerte der Saat
> nach VDI 2067 / AfA-Tabelle; Rechenwirkung und Referenzlauf), Stufenplan S1–S3, Fragen ND-Q1–Q8 mit Empfehlung — vom
> Anwender angenommen —, Abschnitt 6 Umsetzung (S1 = #269, S2 = #270). Indexzeile in `Dokumentation/LIESMICH.md`;
> Wachen `DokumentationLinkWache|RepositoryOrdnungWache` 17/17.
>
> **Offen:** S1 (#269), S2 (#270), S3 auf Zuruf; Konzept nach `ueberholt/` nach S3.

## #266 — Aufschläge auf den Strombezugspreis: Vorgabe „kein Aufschlag" (14.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Anwenderwunsch 14.09.2026 mit Bildschirmfoto der Gruppe „Aufschläge auf den Strombezugspreis" in der
> Energieträger-Trägerkarte: Modus „Gesamtwert (Override)" mit Gesamtaufschlag 0, darunter die fünf Komponenten
> (Netzentgelt 6,44 / Umlagen 2,946 / Stromsteuer 2,05 / Konzessionsabgabe 0,11 / Vertrieb 0,2) mit gesetzten, gesperrten
> Haken und rot „Nicht aufgeschlüsselter Rest: −11,746 ct/kWh". „Der Aufschlag für den Strombezugspreis soll standard auf
> null sein. Optional kann der Gesamtwert oder der aufgeschlüsselte Wert angegeben werden."
>
> **Befund:** `StromAufschlagModel` gab Modus aufgeschlüsselt mit fünf aktiven Vorschlagswerten vor; ein neues Projekt
> trug damit von selbst 11,746 ct/kWh Aufschlag. Die Restzeile der Engine (`Override − Summe aktiv` im Modus Gesamtwert)
> erklärte die rote −11,746. Bestandsprüfung an der Testdatenbank: die Stromträgerzeilen von 1017, 1019, 1023, 1024 tragen
> einen ausdrücklichen Modus; 1030 ist die einzige Zeile mit NULL (dort folgenlos: `Aufschlaege_Anwenden = 0`, kein
> Stromspeicher); 1007, 1045, 1046 haben keine Stromzeile — kein Referenzergebnis betroffen, kein Schema-Schritt.
>
> **Umsetzung:** dritter Modus „kein Aufschlag" (`DbWerte.SP_AUFSCHLAG_MODUS_KEINER`, `AufschlagsModus.Keiner = 2`:
> wirksam 0, keine Abweichung) als Vorgabe des Modells; die fünf Vorschlagswerte und Haken bleiben als Vorschlag.
> `StromAufschlagCtrl.Modus(text)`: nur die zwei ausdrücklichen Texte ergeben einen Rechenmodus, NULL/leer/unbekannt heißt
> „kein Aufschlag"; gespeicherte Zeilen behalten ihren Modus. Baustein `StromAufschlaege`: Optionsgruppe mit „kein
> Aufschlag" zuerst, Komponenten und Gesamtwert nur im zuständigen Modus bedienbar, Abweichungszeile nur im Modus
> Gesamtwert und ohne Alarmfarbe („Gesamtwert liegt … über/unter der Summe der Komponenten"). Kohärenzprüfung und
> `WirtschaftlichkeitCtrl.RechneAufschlaege` benennen den Modus (`KOH_GRUND_STROM_KEIN_AUFSCHLAG`). Brennstoffblock
> geprüft, nicht geändert (Vorgabe Gesamtwert = erfasster Arbeitspreis, Bestandteile null; Kommentar abgegrenzt).
> Wiki-Quellen: Bedienseite Kosten und Rechenwegseite Stromspeicher (drei Modi, Vorgabe).
>
> **Abnahme:** Build 0 Fehler, Warnungen Bestand; SpeicherEngine 29, Kern 64 (neu `StromAufschlagVorgabeTests` 11),
> UI 97 grün; `EnergietraegerDialogTests` dreimal 52/52; Wachen und H13-Wächter 174 grün; volles Gate des Agenten 7 879
> grün, 1 übersprungen (OR-Tools); SqlDialektPruefer 1 363 Texte, 0 Fundstellen; ResourceDesigner unverändert; Referenzlauf
> 5/5 byte-gleich. Merge `9dc67660`.
>
> **Offen:** Bestandsprojekte des Anwenders ohne gespeicherten Modus rechnen jetzt mit 0 statt 11,746 ct/kWh — Schema-Schritt
> zur Sicherung des alten Stands nur auf Zuruf (dann Referenzbasis neu); Logbuch-Eintrag entworfen, Upload gebündelt.

## #267 — Wirtschaftlichkeit: Energiekosten und Rechenweg (14.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Anwenderbefund 14.09.2026 mit zwei Bildschirmfotos der Wirtschaftlichkeit (Kapitalwertmethode, Projekt
> „Beispiel WP WG 1" mit den Varianten „Andere WP" und „Erdwärme", alle mit Simulation 09.09.26): „die Energiekosten sind 0
> auch nach berechnung. Kosten sind angegeben." und „die gesamte Wirtschaftlichkeitsberechnung scheint nicht zu
> funktionieren." Tabelle Stamm: Investition 88.767, Betriebskosten 0, Energiekosten „—", Ersatzbeschaffungen und Restwert 0,
> Nettobarwert/Annuität/Amortisation/Wärmegestehungskosten „—"; Kacheln „nur Stammprojekt gerechnet"; Fußzeile „Parameter
> gespeichert — bitte neu berechnen" ohne sichtbaren Knopf.
>
> **Befund (Agent, headless am Projekt 1026 der Testdatenbank — dasselbe Projekt):**
> (a) Wärmepumpe, PV und Speicher tragen keinen `ID_Carrier`, dem Projekt ist nur „Erdgas E" zugeordnet;
> `Emissionsquelle.StromTraeger(1026)` = 0, `ProjektEnergietraegerCtrl.StandardStromTraeger(1026)` = 60 — die Kostenrechnung
> fragte enger als Anzeige und Assistent; Netzbezug 19,08 MWh/a unbepreist → Energiekosten null → Fehlgrund → kein Kapitalwert.
> (b) Ein nur in `energy_price` (Trägerkarte, „Gültig ab") gepflegter Arbeitspreis mit `custom_price_work = 0` blieb ungelesen:
> vorher NULL, nachher 6.678,00 €/a und Kapitalwert −106.127,28 €. (c/h) „nur Stammprojekt gerechnet" verlangt eine Variante mit
> `KapitalwertDiff`; alle drei brachen an (a) ab. (d/g) Ersatz- und Restwert 0, weil `WirtschaftlichkeitCtrl.RechneProjekt` bei
> fehlenden Energiekosten vor `RechneBild` umkehrt (Gegenprobe mit Preis: Ersatzbarwert 5.041,61 €); die Parameterzeile
> „Ersatzbeschaffung im Jahr 10" entsteht unabhängig davon aus `NutzungsdauerAbgleich`. (e) Betriebskosten 0: 17 Vorlagenzeilen
> mit leerem `EingegebenerWert`, kein Codefehler. (f) `Laden()` liest nur persistierte Ergebnisse; Rechnung nur auf Zuruf wie
> beim Vorläufer `UcWirtschaftlichkeit`, der Knopf „Berechnen" lag in der Fußleiste unterhalb der Vergleichstabelle.
>
> **Umsetzung:** `KostenEmissionRechner` bepreist den Netzbezug mit Rückfall auf `StandardStromTraeger` (am Ergebnis vermerkt,
> `VariantenDaten.StromTraegerRueckfall`); Preiskette Projektwert → `energy_price` zum Stichtag (`StromPreisCtrl.Stichtag`) →
> Katalog; sechs benannte Gründe mit Ausweg (`VariantenDaten.EnergiekostenGrund`); `WirtschaftlichkeitCtrl.Fehlgrund` (Tabelle
> und Verlauf) trägt den Grund des Rechners; der CO₂-Faktor bleibt am zugeordneten Träger (benannter Vorgabewert
> `STROMMIX_CO2_G_JE_KWH`, Emissionszahlen des Bestands unverändert). Seite: `ErgebnisMatrix.Warnungen()` sammelt die
> Warnzellen (`WARN_PRAEFIX`), Warnband über den Kennzahlkarten mit Grund und Knopf „Neu berechnen", auch nach dem Speichern
> eines Unterdialogs und bei veralteter Statuszeile; Rechnung bewusst nicht automatisch beim Öffnen. Ressourcen de/en
> nachgetragen. Wiki-Quelle Wirtschaftlichkeit (Hinweisband, „Neu berechnen").
>
> **Abnahme:** `EnergiekostenGrundTests` 5 von 7 rot vor dem Fix, bunit 5 von 33 rot ohne Warnband, danach grün; Kern gefiltert
> 46 grün; Wachen 22 grün; Gate des Agenten Kern 2 859, UI 4 044, KiKern 488, Engine 456, SpeicherPlanung 27+1 grün, dreimal;
> SqlDialektPruefer 1 365 Texte, 0 Fundstellen; Referenzlauf 5/5 byte-gleich (Wirtschaftlichkeit nicht im Referenzexport).
> Merge `ebdbdde0`.
>
> **Offen:** CO₂-Rückfall auf denselben Stromträger (Anwenderentscheid); Kopplung der Parameterzeile an den Rechenlauf;
> Windows-Hülle `WirtschaftlichkeitSeiteGaben.cs` mit Warnzeichen-Literal; Logbuch-Eintrag entworfen, Upload gebündelt.

## #268 — Energieträgerverwaltung: nur zugelassene Träger je Komponente (14.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Anwenderwunsch 14.09.2026 mit zwei Bildschirmfotos (Energieträgerverwaltung, Projekt 1027; Strom mit
> „Elektrische Energie" und „Strom Variante"): „Die Energieträgerverwaltung (Dialog) sollte nur die zugelassenen
> Energieträger für die ausgewählte Komponente zulassen (zum Beispiel: Wärmepumpe → Strom). Bei gleichem Energieträger-Typ
> soll im Projekt der gleiche Energiepreis sein … Das Gleiche gilt für die Emissionswerte." Nachfrage des Anwenders: der
> zweite Teil sei schon umgesetzt — prüfen.
>
> **Befund (Sonnet-Agent, am Bestand geprüft):** Preis und Emissionen hängen nur an (Projekt, Träger): `energy_price`
> UNIQUE (carrier, valid_from, Projekt), `energy_project_settings` je (Projekt, Träger) (App-Logik, kein DB-Index),
> Variante = eigener `energy_carrier`-Datensatz mit eigener Id, Emissionen je Träger global mit Projektspalten; die Rechnung
> (`StromPreisCtrl.ArbeitspreisCtKwh`, `EnergietraegerPreisCtrl.ProjektpreisLesen`) liest (Projekt, Träger), die Komponente
> wählt nur den Träger → Teil 2 erfüllt. Teil 1: die Verwaltung kannte keinen Komponentenkontext (`Gaben(traegerId)` nur
> Vorwahl; Öffner `KostenKnoepfe.cs`, `EnergietraegerFenster.Oeffnen(besitzer, projektId, traegerId)`), die Trägerwahl je
> Anlage (`EnergietraegerWahl.razor`/`ErzeugerTraegerHuelle`) zeigte alle Gruppen; Einengung nur im Varianten-Anlegedialog
> (`EnergietraegerVarianteCtrl.KategorieZu`).
>
> **Umsetzung:** Kern `EPOS.Kern/Controller/EnergietraegerZulaessigkeit.cs` — `ZulaessigeGruppen(erzeugerart, geraeteId)`
> (null = keine Einengung, leer = kein Träger), `Kategoriecodes`, `IstZulaessig`, `PasstGruppe`, `MitTraeger`,
> `ZulaessigerKatalog`; entschieden wird am Kategoriecode (`Tab_BrennstoffKategorien.Code` = `energy_carrier.pricing_model`),
> angezeigt über `group_code`: Wärmepumpe/PV/Stromspeicher/Heizstab ELECTRICITY → Strom; Heizkessel/BHKW mit Gerät nach
> Gerätekategorie (GASEOUS → Gas, Wasserstoff; LIQUID → Öl; SOLID → Holz, Kohle, Koks; HEAT → Fernwärme; ELECTRICITY →
> Strom), BHKW ohne Gerät GASEOUS+LIQUID (Biogas liegt in Gas); Solarthermie/Pufferspeicher leer; Sicherheitsnetz: bleibt
> kein Katalogträger übrig, keine Einengung. Hülle `EnergietraegerHuelle.Gaben(traegerId, erzeugerart, geraeteId)`: gefilterte
> Liste und Katalogübernahme, Kopfzeile mit Kontext, unpassender zugeordneter Träger bleibt markiert; Solarthermie/
> Pufferspeicher nennen „kein eigener Energieträger" statt leerer Liste. `EnergietraegerListe.Passend`,
> `ErzeugerTraegerHuelle.Katalog` aus `ZulaessigerKatalog`, drei Anlagenhüllen setzen ihre Erzeugerart; Kostenseite reicht die
> gewählte Anlagenzeile an den Öffner (`KostenSeite.GewaehlteZeile()`, `AnlagenEintrag.GeraeteId`,
> `KostenSeiteGaben.TraegerGaben(zeile)`); `KostenKnoepfe.cs` (WinForms-Leiste ohne Aufrufer) entfernt; Windows-Schale mit
> `EnableWindowsTargeting` gebaut (0 Fehler, 5 Warnungen Bestand). Ressourcen
> `KDLG_ET_KONTEXT_KOMPONENTE`, `_OHNE_TRAEGER`, `_ALLE`, `KDLG_ET_PASST_NICHT` de/en. Wiki-Quelle Kosten
> (Energieträgerverwaltung, Träger je Anlage).
>
> **Abnahme:** Build 0 Fehler, Warnungen Bestand; Kern `Energietraeger|ProjektEnergietraeger` 72 grün (14 neue
> `EnergietraegerZulaessigkeitTests`, 6 Hüllentests); UI 79 grün, `KostenSeiteTests` 45, `EnergietraegerDialogTests` dreimal 57; Wachen 22;
> Gate des Agenten 7 906 grün; SqlDialektPruefer 1 366 Texte, 0 Fundstellen; ResourceDesigner unverändert; Referenzlauf 5/5.
> Merge `4dba8d30`.
>
> **Offen:** Heizkessel-/BHKW-Dialog: `KostenOeffnen`/`EnergiekostenOeffnen` in `HeizkesselHuelle`/`BhkwHuelle` unbelegt
> (eigener Auftrag); `ANIMAL_FAT` beim BHKW ohne Gerät; Wasserstoff in der Gasmenge (fachlich richtig); iOS-Variantendialog ohne
> Kategorie; UNIQUE-Index `energy_project_settings` auf Zuruf; Logbuch-Eintrag entworfen, Upload gebündelt.

## #269 — Nutzungsdauern (AfA) Stufe S1: Tabelle, Saat, Verwaltung, Vorbelegung (14.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Stufe S1 des Konzepts `Konzept_Nutzungsdauer_AfA_EPOS-Plan.md` (#264) nach den Entscheiden ND-Q1–Q8
> (Empfehlung angenommen, 14.09.2026). Befund: im Bestand keine Technik-Vorgabe; Nutzungsdauer nur je Vorlagen- und
> Projektposition, Saat NULL, `KapitalwertRechner` setzt n < 1 auf T.
>
> **Umsetzung:** `EPOS.Kern/Allgemein/Update/NutzungsdauerSchema.cs` als eine Quelle für DDL, Saat und Zuordnung
> (Migration, Werkzeug Testdatenbankschema, Nachweis); Schema-Schritt 75, `SchemaStand.Zielversion` 74 → 75.
> `Tab_Nutzungsdauer` STRICT mit KomponentenID, Positionsart, IstStandard, Nutzungsdauer_a, AfA_steuerlich_a,
> Instandsetzung_Prozent, Wartung_Prozent, Quelle, ReadOnly, Sortierung; Boolean-Spalten mit `CHECK (… IN (0,1))`,
> `UNIQUE (KomponentenID, Positionsart)` mit FK auf `Tab_KostenKomponente`, dazu ein eindeutiger Index über
> `COALESCE(KomponentenID, 0)`, weil SQLite NULL nicht gegen NULL hält (zwei technikübergreifende Zeilen „Montage",
> „Planung / Baunebenkosten"). `NutzungsdauerID` (INTEGER NULL, FK) an `Tab_KostenVorlagePosition` und `Tab_ProjektWerte`.
> Saat 28 Zeilen, alle ReadOnly, genau zehn Standardzeilen (eine je Technik), Quellen „VDI 2067 Blatt 1, Tab. A2
> (Richtwert)" und bei vier Zeilen „AfA-Tabelle AV (Richtwert)"; Zuordnung 31 von 53 Investitionspositionen der Vorlagen
> (22 nicht zuordenbare Namen bleiben NULL), 0 von 67 Betriebspositionen; `Tab_ProjektWerte` bekommt die Spalte, aber
> keinen Wert (175 Zeilen Feld für Feld unverändert). `NutzungsdauerCtrl`: `Alle`, `Zeile`, `Standard`, `Vorgabe(technik,
> positionsart)` mit NULL-Auflösung und Herleitungstext, `Neu`, `Speichern`, `Loeschen` (ReadOnly → benannte Ablehnung),
> `AuslieferungWiederherstellen`, tolerante Tabellen- und Spaltenprobe; Vorbelegung in `KostenVorlagenCtrl.PositionNeu`
> (nur Investition), `KostenProjektPositionenCtrl.Neu`, `KostenVorlagenUebernahmeCtrl.AusVorlage` (kopiert `NutzungsdauerID`,
> nimmt den Vorlagenwert, sonst `Vorgabe`); nichts Gespeichertes wird still verändert. Dialog
> `EPOS.UI/Dialoge/Kosten/NutzungsdauerDialog.razor` mit `NutzungsdauerDaten.cs`, Hülle
> `EPOS.UI.Daten/Kosten/NutzungsdauerHuelle.cs`, Menüpunkt „Nutzungsdauern (AfA)…" als dritter Eintrag unter Administration →
> Kostenverwaltung, `Seitenschluessel.NutzungsdauerVerwaltung`, Menüwächter 60 Punkte / 8 Trenner / 13 klappend / 47 handelnd,
> Zielmenge Administration 32; Windows-Öffner `WindowsFormsApplication1/Views/Kosten/NutzungsdauerFenster.cs` nach dem Muster
> `EnergietraegerFenster`, Fall in `HauptfensterHuelle.Weg`, Zeile in `help_mapping.txt`; 31 Ressourcen `ND_*` de/en,
> ResourceDesigner gezogen. Auslieferungsvorlage: Tabelle ohne Projektspalte und ohne `_STAMM` bleibt vollständig, Nachweis P6b,
> P6 (STRICT) 118 → 119. Testdatenbank auf Schemastand 75 (120 Tabellen, 119 STRICT, 25 Projekte, `integrity_check` ok,
> `foreign_key_check` 0), Zweitlauf des Werkzeugs 0/0. Wiki-Quelle Kosten: Absatz „Nutzungsdauern (AfA)"; Abschnitt
> „Kosten in der Speicherauslegung" nennt nur den gültigen Stand. `BETRIEB_SQLITE.md` 6.5, `Referenzlaeufe/LIESMICH.md`.
>
> **Abnahme:** Build 0 Fehler, 4 Warnungen (Bestand); Gate des Agenten 7 906 grün, 1 übersprungen; `NutzungsdauerTests` 15
> und `NutzungsdauerDialogTests` 12 je dreimal grün; Wachen 43, `MenuebandTests` 57, `ParametersatzTests` 17,
> `StilblattTests` 23, `FormularrasterTests` 69, `KiDialogaufrufTests` 37; Auslieferungsvorlage 19, Formularkarte 122;
> SqlDialektPruefer 1 383 Texte, 0 Fundstellen; Windows-Schale mit `EnableWindowsTargeting` 0 Fehler, 5 Warnungen (Bestand);
> Referenzlauf 5/5 PASS und byte-gleich gegen `2026-09-11_R7_Speicherflotte`. Merge `de689bfe`.
>
> **Offen:** S2 (#270) und S3; `Instandsetzung_Prozent`/`Wartung_Prozent` angelegt, nicht gelesen; iOS ohne Rubrik
> Kostenverwaltung; Logbuch-Eintrag entworfen, Upload gebündelt.

## #271 — Kostenverwaltung: Bemessung „je kW Leistung" bei Solarthermie (14.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Anwenderbefund 14.09.2026 mit Bildschirmfoto der Kostenverwaltung Solarthermie (Projekt „Beispiel WP WG 1 -
> Erdwärme", Investitionskosten nach VDI 2067): Zeile „Solarthermie", Bemessung „je kW Leistung", Satz 700 €/kW, Betrag netto 0,
> Summe 0,00 €, kein Hinweis. „Bei Solarthermie und Auswahl ‚je kW Leistung' wird nicht die Leistung der Solarthermie-
> Komponente genommen."
>
> **Befund:** `TechnikPlanwertCtrl.Geraetespalte` kannte für Komponente 4 nur „je m² Kollektorfläche" (`Aperturflaeche` ×
> `Kollektormodulanzahl`); „je kW Leistung" traf keine Spalte, weil `Tab_Solarkollektoren` (Modulfläche, Aperturfläche, h0, k1,
> k2, Kdir, Kdfu, Investitionskosten) keine Leistung führt und weder `SimulationSolarthermie` (spezifische Leistung je Stunde
> in W/m²) noch Dialoge oder Hilfeseite eine Nenngröße kennen. Der H4c-Grund („Art passt nicht zum Gewerk") wurde gebaut
> (`KostenKomponenteHuelle.BasisKurztext`), landete aber nur im Werkzeugtipp der Betragszelle.
>
> **Umsetzung:** Leistung des Kollektorfelds an EINER Stelle im Kern: `TechnikPlanwertCtrl.KOLLEKTOR_KW_JE_M2` = 0,7 kW/m²
> (Konvention für die thermische Nennleistung von Solarkollektoren, Quellkommentar), `KollektorfeldKw` = 0,7 × Aperturfläche ×
> Kollektormodulanzahl für „je kW Leistung" und „je kW Heizleistung" (`IstSolarLeistungsart`); Herleitungstext
> `KollektorfeldHerleitung` („0,7 kW/m² × 2,50 m² × 10 Module = 17,50 kW") über `BaugroesseHerleitung`;
> `KostenProjektPositionenCtrl.Zeile.BasisHerleitung` beim Laden, Nachziehen und Bemessungswechsel. Kein stilles 0:
> `KostenPositionZeile.OhneBasis`/`BasisHerleitung`, `VorlagenZeile` setzt ⚠ in die Betragszelle (Grund als `aria-label`,
> Kurztext und Herleitung im Werkzeugtipp), `KostenKomponenteDialog` sammelt betroffene Zeilen in einer leisen Zeile unter dem
> Raster; zwei Stilregeln; Windows-Hülle `KopplungAnwenden` belegt die zwei Felder und reicht `VorlageOhneBasis`. Gewerk-Tabelle:
> Wärmepumpe `Nennleistung`, Kessel `Ptherm`, PV `KwpSumme`, Speicher `Leistung`/`Energie`, BHKW `Ptherm`/`Pel`; bewusst „passt
> nicht": WP elektrisch, PV thermisch, Pufferspeicher kWh, Komponenten 8–10 ohne Gerätetabelle. Wiki-Quelle Kosten: Abschnitt
> „Bemessung der Positionen" mit Gewerk-Tabelle, Formel und Rechenbeispiel aus runden Zahlen, Punkt „Ohne Bezugsgröße".
>
> **Abnahme:** `SolarthermieLeistungTests` 18 (8 rot vor dem Fix), Kostenfilter 81/81, 7 bunit-Fälle (94/94), Wachen 22/22;
> Gate des Agenten Kern 2 870, UI 4 045, KiKern 488, Engine 456, SpeicherPlanung 27+1 — 7 886 grün, 4 Warnungen Bestand;
> SqlDialektPruefer 1 365 Texte, 0 Fundstellen; Referenzlauf 5/5 (Kosten nicht im Referenzexport, keine Solarthermie-Position
> mit kW-Bemessung in der Testdatenbank). Merge `ddbed2b7`.
>
> **Offen:** BHKW und Pufferspeicher bei „je kW Leistung" mehrdeutig — eigener Grund nur nach Anwenderentscheid; Windows-
> Abnahme der Hüllenzeilen; Logbuch-Eintrag entworfen, Upload gebündelt.

## #273 — Stromspeicher-Auslegung: Ergebnisse in Station 5, Bedienblock unter der Suche, Größensuche ohne Gerät (14.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Anwenderwunsch 14.09.2026 (drei Bildschirmfotos): Ergebnisse der Optimierung aus Station 4 in Station 5,
> dort gegliedert in „Ergebnisse aus der Optimierung" und „Ergebnisse nach Simulation"; der rechte Bedienblock unter die
> Suche. Nachtrag (zwei Bildschirmfotos): „Bei ‚Größe suchen' ist eine Speicherauswahl nicht sinnvoll — die
> konfigurierten Speicher nicht in der Suche anzeigen, ‚variieren' entfällt."
>
> **Umsetzung:** `OptimierungBlock.razor` einspaltig: Suche (drei Sucharten mit Erklärzeile) → Bedienblock in voller
> Breite (Ziel, Kandidatenzeile, Feinraster, Rechenknopf mit Fortschritt/Abbrechen) → „Suchraum je Einheit";
> `.epos-flotte-optimierung-kopf` als einspaltiges Raster, feste Bedienblockbreite (26 rem) und `@media`-Umbruch entfallen.
> Station 5 mit zwei `Gruppenkopf`-Rubriken: „Ergebnisse der Optimierung" im neuen Baustein
> `EPOS.UI/Seiten/Strom/OptimierungsergebnisBlock.razor` (Kasten „Bestes Ergebnis" mit drei Karten, darunter
> `SpeicherFlottenGroessenAnsicht`; nur nach einer Suche, sonst `FLOTTE_ERG_NUR_BEWERTET`), danach „Ergebnisse der
> Simulation" (Diagnosebanner, Kandidatenhinweis, `SpeicherFlottenErgebnisAnsicht`, Fußleiste). Wechsel auf Station 5 nach
> gelungenem Lauf (`FlotteStarten`), Abbruch und Vorprüfungssperre lassen Station 4 stehen; `_laufMethode` hält die Suchart
> des Laufs, weil „Kandidat übernehmen" jetzt aus Station 5 kommt und der Anwender dazwischen umschalten kann. Größensuche:
> keine Kopfzeile mit Hersteller/Typ/Kenndaten, kein Schalter „variieren"; eine Einheit → Block ohne Kopfzeile, mehrere →
> „Einheit 1", „Einheit 2"; Erklärzeile `FLOTTE_OPT_GROESSE_UNABHAENGIG` de/en; `OnParametersSet` stellt unter „Größe
> suchen" alle Achsen auf `Aktiv` (still), die Sperre gilt nur ohne Einheit; „Stückzahl suchen" bleibt an „variieren"
> gebunden. Konzept Stromspeicher-Dialoge Abschnitt 7.10 (SD‑E‑11) mit Rechenwegbefund, 7.4/7.9/8.4 verweisen; Mockup;
> Wiki-Quelle Stromspeicher (Schritt 4 als Eingabe, Suchraum je Suchart, Schritt 5 mit beiden Rubriken, Anker bleiben).
>
> **Rechenwegbefund (unverändert):** `FlottenOptimierer.BaueEinheiten` kopiert die Achsenvorlage vollständig und überschreibt
> nur Kapazität, Lade- und Entladeleistung (gleicher Faktor aus max(Lade, Entlade)); vom Produkt fließen Wirkungsgrade,
> SoC-Band, Peak-Reserve und Hilfsverbrauch (ungeskaliert), Grenzverschleiß, Rainflow-Kurve und `AnlageId` ein; Kosten aus
> Station 2 überschreiben je Einheit und Achsenvorlage, außer bei `EigeneKosten` (dann Pauschalen ungeskaliert);
> C-Rate-Kopplung aus dem Achsenmodus, Netzladung/Betriebsziel/Peak-Ziel flottenweit aus Station 3. Neutralisierung ist
> eine offene Anwenderfrage.
>
> **Abnahme:** Build 0 Fehler, 4 Warnungen (Bestand); Filter `Strom|Flotte|Optimierung|Auslegung` 428 grün;
> `OptimierungStationTests|StromspeicherAuslegungGroessenTests|StueckzahlsucheTests` dreimal 44 grün; Wachen 22,
> `StilblattTests` 24 (neu: Kopf der Station einspaltig); Gate des Agenten 7 932 grün, 1 übersprungen; ResourceDesigner
> wiederholbar; kein Rechenweg berührt. Merge `c98de53d` mit Auflösung der resx-Konflikte (beide Seiten, Designer neu);
> Folgecommit: „zuvor" statt Tabuwort in der Wiki-Quelle.
>
> **Offen:** Anwenderfrage Produktparameter im Größenlauf; `FLOTTE_OPT_KEIN_ERGEBNIS` ohne Verwender; zwei
> Logbuch-Einträge entworfen, Upload gebündelt.

## #274 — Stromspeicher-Auslegung aus der Konfiguration ①, Ergebnisreiter nur Ergebnis (14.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Anwenderwunsch 14.09.2026 mit zwei Bildschirmfotos: „Der Dialog Stromspeicher soll in den Dialog
> Konfiguration verschoben werden. Ähnlich zu ‚Pufferspeicher anlegen/verwalten' einen Konfigurationsbutton
> ‚Stromspeicher auslegen' (anstelle Aufruf aus ‚Speicherflotte und Auslegung')."
>
> **Umsetzung:** `SimulationKonfigSeite.razor`, Spalte „Speicher im Projekt": unter dem Pufferspeicher-Knopf der Knopf
> „Stromspeicher auslegen…" (`SIM_BTN_SP_AUSLEGUNG`, de/en) mit Kurzzeile zum Stand (Mehrspeicherbetrieb aktiviert oder
> deaktiviert, Betriebsziel, Einheitenzahl); ohne Stromspeicher und ohne Flottenstand eine Erklärzeile, ohne eingelegten
> Weg kein Knopf. Neue Gabe `StromspeicherStand`, Delegat `SimulationKonfigDienste.AuslegungOeffnen`. Gegangen wird der
> Weg von der **Ergebnishülle**, weil sie den gerechneten Lauf hält, aus dem die Auslegung ihre Zeitreihen zieht;
> `SimulationAnsichtQuelle` verknüpft Konfigurations- und Ergebnishülle — dadurch gilt der Einstieg unter Windows **und**
> auf iOS ohne eine Zeile in `EPOS.iOS`. Rückweg über den Rückwegstapel mit Marke in ① (`schritt=1`); die Konfiguration
> wird beim Rückweg neu aufgebaut und liest ihre Speicherzeile frisch, der Kurzstand-Merker der Hülle fällt beim Öffnen.
> `StromspeicherReiter.razor` verliert Kopfzeile, `SpeicherFlottenBetriebEditor`, Speichertabelle, Datenstand und
> Öffnerknopf samt dem Schreibweg `FlottenbetriebGesetzt` — die Betriebsoptionen werden nur noch in der Auslegungsansicht
> gepflegt (#213 gilt dort weiter); an ihrer Stelle eine Herkunftszeile aus `Daten.AktiveFlotte` (Betriebsziel,
> Einheitenzahl, Peak-Ziel mit Modus nach #215) mit Verweis „Konfiguration ändern → ①". **Nebenbefund behoben:** Kacheln,
> Betriebsbild und die 39 Kennzahlen des Reiters hingen an `!FlottenEinstiegMoeglich` und waren damit auf beiden
> Plattformen unerreichbar; sie hängen jetzt am Ergebnis. Aus `SimulationErgebnisDienste` fallen `OptimierungVorgaben`,
> `OptimierungFlottenRechnen`, `OptimierungEinstellungenSpeichern` und `AuslegungOeffnen` samt den zwei toten Hüllenwegen
> und der Abbruchmarke; geblieben ist `OptimierungCsv`, neu `KonfigurationOeffnen`. `SpeicherParameterBlock` verliert
> seinen zweiten, auf beiden Plattformen gesperrten Optimierungsknopf. Der Startseiten-Reiter (#220) bettet dieselbe
> Ergebnisseite ein; sein Verweis geht über `KonfigurationOeffnen` auf die Einstiegsmarke `SIMULATION_KONFIGURATION`, die
> die `AppWurzel` seit #207 in die Ansicht SIMULATION ① übersetzt — kein zweiter Datenweg. Konzept Simulationsablauf:
> Zielbild und Ablaufregel auf ① umgestellt, Abschnitt 11 (Rückmeldung, Befund, Entscheid, Bauweise, Prüfmuster);
> Wiki-Quellen Simulation, Stromspeicher (Abschnitt Erreichbarkeit) und Simulationsergebnisse.
>
> **Abnahme:** Kern-Filter 0 Fehler, 4 Warnungen (Bestand); Windows-Schale mit `EnableWindowsTargeting` 0 Fehler,
> 5 Warnungen (Bestand); EPOS.UI.Tests 4 072, EPOS.Kern.Tests 2 933, SpeicherEngine 459, KiKern 488, SpeicherPlanung 27+1
> grün; die sechs betroffenen Klassen dreimal grün; Wachen `DokumentationLinkWache`, `RepositoryOrdnungWache`,
> `WikiProduktdatenWache`, `Ueberlagerungstitel`, `Stilblatt` grün; ResourceDesigner wiederholbar; kein Rechenweg berührt,
> kein Referenzlauf nötig. Der Zweig zog `ios_migration_september` (Stand mit #273) selbst nach; einziger Konflikt waren
> die angehängten Ressourcenblöcke beider Seiten, beide bleiben. Merge `6c811f56`; Folgecommit: „gerechneten Lauf" statt
> Tabuwort in der Wiki-Quelle Stromspeicher.
>
> **Offen:** Logbuch-Eintrag entworfen, Upload gebündelt; `Proben/Rasterprobe` nicht gezogen (nicht berührt).

## #275 — Wärmequelle Erdreich: der OK-Knopf fällt, das Schließen übernimmt (15.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Anwenderwunsch 14.09.2026: „Dialog: Wärmequelle Erdreich / OK Button soll aus Dialog raus". Die zwei
> übrigen Teile des Wunsches (Erdkollektor und Erdsonde als eigene Rubriken) waren mit #262 erledigt; der Knopf blieb
> offen, weil zwei Lesarten möglich waren. Entscheid 15.09.2026 („umsetzen"): die Knopfleiste fällt ganz, das Schließen
> übernimmt mit Prüfung. Reichweite nur dieser Dialog.
>
> **Umsetzung:** `EPOS.UI/Dialoge/Simulation/QuelleErdreichDialog.razor` trägt keine `SpeichernLeiste` mehr;
> `BeiErgebnis(bool)` ist zu `public Task UebernehmenUndSchliessen()` geworden — dieselben acht Prüfregeln in derselben
> Reihenfolge und mit demselben Wortlaut, dasselbe `Geschlossen.InvokeAsync(Daten with { … })`. Kreuz und Escape münden
> beide dort hinein; es gibt keine zweite Kopie der Regeln und keinen Abbruchweg mehr (der Wirt behandelt eine leere
> Rückgabe weiterhin). Eine verletzte Regel meldet im Warnband und hält den Dialog offen; `Meldet` zeichnet selbst nach,
> weil der Ruf nun auch von außen kommt. Bei laufender Simulation (`_laeuft`) steigt der Weg sofort aus, der Dialog
> schließt nicht. Die Parameter `OkText`/`AbbrechenText` hatten danach keinen Verwender mehr und fielen samt ihren zwei
> Gaben in `EPOS.UI.Daten/Simulation/QuelleErdreichHuelle.cs` — ein unbekannter Schlüssel im `@attributes`-Splat würde
> zur Laufzeit werfen.
>
> **Schließweg und Begründung:** Der Baustein `Ueberlagerung` bleibt unangetastet. Die Einbettung in
> `SimulationKonfigSeite.razor` setzt `Offen="true"` ohne Zweiwegbindung; damit hat `OffenChanged` keinen Delegaten und
> `BeiSchliessen` ruft allein `Geschlossen` — der Wirt entscheidet ohnehin, ob geschlossen wird. Die Seite ruft dort
> `ErdreichSchliessen()` und darin über einen Verweis die eine öffentliche Methode des Dialogs; ist eine Regel verletzt,
> tut sie nichts und die Überlagerung bleibt stehen. Ohne Verweis (Dialog noch nicht gezeichnet) schließt sie wie bisher,
> damit niemand festsitzt; `Ebene2Schliessen` räumt den Verweis mit weg. Verworfen wurden ein neuer Prüf-Rückruf am
> Baustein (unnötig) und `Schliessbar="false"` samt eigenem Kreuz im Dialogkopf (verdoppelt die Schließgeste). Escape
> steigt vom Dialog bis zur Überlagerung auf und hätte den Weg zweimal angestoßen; die Dialogwurzel hält den Tastendruck
> deshalb an und wertet ihn selbst aus. Liegt der Fokus noch auf der Überlagerungswurzel, greift deren Escape über den
> Wirt auf dieselbe Methode.
>
> **Abnahme:** Kern-Filter 0 Fehler, 4 Warnungen (Bestand); Windows-Schale mit `EnableWindowsTargeting` 0 Fehler,
> 5 Warnungen (Bestand); `~Erdreich` 37 und `~SimulationKonfig` 43 je dreimal grün (darunter zwei Ende-zu-Ende-Fälle, die
> das echte Kreuz der Überlagerung klicken), `~SpeichernLeiste` 10, `~Ueberlagerung` 77, `~Stilblatt` 24,
> `~KiDialogaufruf` 37, Kern-Wachen 43; voller `EPOS.UI.Tests`-Lauf 4 077 grün; ResourceDesigner unverändert und
> wiederholbar; kein Rechenweg berührt. Merge `ede82c79`.
>
> **Offen:** kein Verwerfen mehr in diesem Dialog (Bedienänderung); die sechs übrigen Simulationsdialoge mit derselben
> Leiste unverändert, Ausweitung auf Zuruf; Logbuch-Eintrag entworfen, Upload gebündelt.

## #276 — Wärmequelle Erdreich: OK und Abbrechen kehren zurück, #275 zurückgenommen (15.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Punkt 8 der Entscheide vom 15.09.2026 lautete „Ausweitung des Schließmusters: immer mit Abbrechen (neben
> OK), der ohne Speichern den Dialog verlässt" — das ließ drei Lesarten zu und stand quer zu #275, das im Erdreich-Dialog
> gerade den OK-Knopf hatte fallen lassen. Auf Rückfrage entschied der Anwender: „Der OK Button soll in jedem Dialog
> vorhanden sein und diesen mit Speichern verlassen, Abbrechen Button ohne Speichern den Dialog verlassen." Damit ist
> #275 zurückgenommen; die Rubriken Erdkollektor und Erdsonde aus #262 bleiben unangetastet.
>
> **Rückbau:** `QuelleErdreichDialog.razor`, `SimulationKonfigSeite.razor` und `QuelleErdreichHuelle.cs` stehen byte-genau
> auf dem Stand vor #275. Der Dialog trägt wieder die `SpeichernLeiste`: OK prüft, speichert und schließt; Abbrechen, das
> Kreuz und Escape verlassen ohne Speichern. `UebernehmenUndSchliessen()` ist wieder `BeiErgebnis(bool)` mit denselben
> acht Prüfregeln in derselben Reihenfolge und demselben Wortlaut. Der `@ref`-Griff der Konfigurationsseite und
> `ErdreichSchliessen()` entfallen, weil der Dialog sein Schließen wieder selbst verantwortet; `OkText` und
> `AbbrechenText` samt ihren Gaben in der Hülle kehren zurück.
>
> **Was die Nachbardialoge betrifft:** nichts. Sie trugen das Hausmuster schon, es war nur nicht überall belegt. Belegt
> ist jetzt ihr dritter Ausgang, und zwar am Wirt, wo das Kreuz wirklich hängt — je ein Fall an der Konfigurationsseite
> für Betriebsmodus, Wärmesenke, Quelle Pufferspeicher, Quellprofil und Erdreich (Kreuz schließt, es wird nichts
> geschrieben), dazu am Erdreich-Dialog OK (schreibt und schließt) und Abbrechen mit verletzter Regel (schließt ohne
> Prüfung und ohne zu schreiben). Im Dialog selbst kommen zwei Fälle hinzu: Das Kreuz der Klimazonenkarte lässt die
> markierte Zone fallen, und der Wartezustand des Simulationslaufs sperrt den OK-Knopf, während Escape ihn nicht
> wegdrückt. Beim Quellprofil, das selbst speichert, prüft der Abbruchfall zusätzlich, dass der Speicherweg unberührt
> bleibt.
>
> **Die eine Ausnahme:** `PufferSpProjektDialog` trägt „Übernehmen" und „Schließen", weil er beim Übernehmen sofort
> schreibt; sein Kreuz wirkt wie dieses Schließen. `EPOS.UI/CLAUDE.md` hält das Muster jetzt als Regelzeile fest statt
> eine Abweichung davon zu beschreiben, und benennt diese eine Ausnahme.
>
> **Abnahme:** Kern-Filter 0 Fehler; Windows-Schale mit `EnableWindowsTargeting` 0 Fehler, 5 Warnungen (Bestand);
> sechs neue Testfälle netto, betroffene Klassen mehrfach grün; ResourceDesigner unverändert und wiederholbar; kein
> Rechenweg berührt. Merge `1c53c95c`.
>
> **Offen:** ob das Muster auch im `PufferSpProjektDialog` gelten soll — das hieße, das Schreiben bis zum OK
> aufzuschieben — ist Anwenderentscheid. Der Logbuch-Eintrag zu #275 ist durch diese Rücknahme umgeschrieben worden,
> Upload gebündelt.

## #277 — Kostenknöpfe im Heizkessel- und BHKW-Dialog (15.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Anwenderentscheid 15.09.2026 „Kostenknöpfe im Heizkessel- und BHKW-Dialog: Knöpfe belegen/ausführen."
>
> **Befund, der die Aufgabe verschob:** Es standen keine toten Knöpfe da — es standen keine. Die
> `KostenKnoepfeLeiste` zeichnet ohne Delegat gar keinen Knopf, und alle vier Wirte reichten nur die Beschriftungen
> durch. Vorbild für einen belegten Weg war allein `WaermepumpeAnlageHuelle`.
>
> **Weg:** Beide Projektdialoge führen ihre drei Wege jetzt mit der gewählten Projektzeile aus. Die Leiste selbst bleibt
> unverändert, weil vier Wirte sie teilen. Die Windows-Seite steht einmal in
> `WindowsFormsApplication1/Views/Kosten/ErzeugerKostenwege.cs`: Investitions- und Betriebskosten öffnen die
> Kostenverwaltung im Projektmodus — die Anlagen-Id wird über `ProjektEnergietraegerCtrl.AnlagenMitTraeger`
> nachgeschlagen, weil eine frisch aufgenommene Zeile ihre Datenbankzeile erst beim OK bekommt —, Energiekosten öffnen
> die Energieträgerverwaltung mit Träger und Gerät. Beide Fenster gehen über `Blazornachlauf.Nachgelagert` auf (Regel b:
> kein modales Systemfenster direkt aus einem Blazor-Ereignis). Ohne Projekt bleibt der Delegat weg, und damit der Knopf.
>
> **Was bewusst leer bleibt:** die Katalogeditoren. Ein Katalogsatz gehört keinem Projekt, also gibt es nichts zu
> öffnen. Der neue Hüllen-Wächter `KostenknopfWegeTests` hält das ausdrücklich fest, statt es dem nächsten Leser zu
> überlassen.
>
> **Abnahme:** Kern-Filter 0 Fehler, 0 Warnungen; Windows-Schale 0 Fehler, 5 Warnungen (Bestand); Kern-Gate des Agenten
> 8 001 grün; 12 neue bunit-Fälle und 5 Wächterfälle. Der Leerlauf ist belegt: mit den alten Hüllenständen fällt der
> Wächter 2 von 5. Merge `42061e80`.
>
> **Offen:** `WaermepumpeAnlageHuelle.KostenOeffnen` öffnet `KostenKomponenteHuelle.OeffnenProjekt` synchron aus dem
> Blazor-Rückruf und verstößt gegen Regel b; der Regex der `HuellenwegTests` greift dort nicht. Bestand, nicht neu —
> eigener Auftrag.

## #278 — Ein Satz je Energieträger und Projekt: Schemaschritt 76 (15.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Anwenderentscheid 15.09.2026 „UNIQUE-Index auf die Projekteinstellungen je Energieträger: Umsetzen."
>
> **Bestand zuerst, dann der Schritt:** 28 Zeilen in `energy_project_settings` auf 18 Projekte, kein Paar zweimal — es
> gab nichts zu entdoppeln, der Auftrag musste nirgends anhalten. Auch die Spaltenkombination ist gemessen, nicht
> geraten: Jede Lesekette fragt `WHERE ID_Projekt = ? AND ID_Energieträger = ?` ohne `ORDER BY` und nimmt die erste
> Zeile; der Index läuft deshalb plain über genau diese zwei Spalten und ist zugleich ihr Suchweg. `ID_Umrechnung`
> gehört nicht zum Schlüssel.
>
> **Schritt 76** (`SCHRITT_76_TRAEGERSATZ_EINDEUTIG` in `EPOS.Kern/Allgemein/Update/ProjektEnergietraegerEindeutig.cs`)
> entdoppelt erst und legt dann `idx_EnergyProjectSettings_Traeger` an. Das Entdoppeln behält je Paar `MIN(ID)` — also
> die Zeile, die auch bisher galt, weil jede Lesekette die erste nimmt; der Schritt ist damit ergebnisneutral. Diese
> Reihenfolge ist der Punkt: Ein unsauberer Altbestand auf einem fremden Rechner lässt den Schritt nicht scheitern und
> sperrt den Simulationsbereich nicht (ADR-001). `SchemaStand.Zielversion` geht von 75 auf 76, die Testdatenbank ist
> eingespielt (Schemastand 76, 120 Tabellen davon 119 STRICT, `integrity_check` ok, 70 766 592 Byte).
>
> **Die fünf Schreibwege bleiben, wie sie sind** — geprüft, nicht geändert: Katalogsatz und Assistent zählen vorher per
> `COUNT`, die Preiszeile schreibt als Upsert, die Variantenanlage zählt in ihrer eigenen Transaktion, und die drei
> kopierenden Wege schreiben immer in ein frisch angelegtes Projekt. Keiner kann den Index verletzen; eine
> SQLite-Ausnahme erreicht den Anwender an keiner Stelle.
>
> **Abnahme:** Kern-Filter 0 Fehler, 0 Warnungen; Kern-Gate des Agenten 7 992 grün mit acht neuen Fällen;
> SQL-Dialektprüfer 1 396 Texte, 0 Fundstellen; Windows-Schale 0 Fehler, 5 Warnungen (Bestand); Referenzlauf 5 von 5
> PASS und byte-gleich gegen R7. Merge `319c172b`.
>
> **Offen:** nichts. Kein Logbuch-Eintrag — die Änderung ist für den Anwender nicht sichtbar.

## #279 — Bemessung: BHKW je kW elektrisch, Pufferspeicher je Liter (15.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Anwenderentscheid 15.09.2026, wortgleich: „‚je kW Leistung‘ beim BHKW ist ‚je kW elektr. Leistung‘, beim
> Pufferspeicher soll das Volumen die Bezugsgröße sein (€/Ltr.)“.
>
> **Befund:** Es ist EINE Stelle — `TechnikPlanwertCtrl.Geraetespalte` ist die Landkarte Art↔Gewerk, daneben stehen nur
> die zwei benannten Sonderwege für PV und Solarthermie (aus #271/H4c, jeweils begründet, weil die Größe gerechnet statt
> gelesen wird). Kein zusammenzuführender Zweitort. Was die beiden Komponenten bekamen: BHKW bei „je kW Leistung“ `null`
> mit dem Kommentar „Pel ODER Ptherm — offen“; Pufferspeicher `null` bei **jeder** Bemessungsart, also gar keine Baugröße.
> In beiden Fällen galt über Anwenderentscheid I‑2 der erfasste Betrag, bei satzbasierten Zeilen also 0.
>
> **BHKW:** `Tab_BHKW.Pel` [kW el] — belegt über `BHKWStammCtrl` („Elektrische Leistung [kW]“), `AbweichungsErmittler`
> („el. Leistung“, „kW“), `ParameterVerwendung` und `SimulationBHKW`, das `bhkwStromLeistung` aus `m_Pel` speist. Nicht
> Ptherm, nicht die Summe. „je kW Heizleistung“ bleibt unverändert auf Ptherm.
>
> **Pufferspeicher:** `Tab_Pufferspeicher.Gesamtvolumen` in Litern (belegt über `PufferSpStammCtrl` „Gesamtvolumen: Liter“
> und `AbweichungsErmittler` Einheit „l“). Die vorhandene Art wird **umgedeutet, nicht verdoppelt** — am Bestand
> entschieden: `Tab_ProjektWerte` führt `EUR_PRO_KW_LEISTUNG` genau einmal, an Komponente 2 (Heizkessel), und die
> Auslieferungsvorlage ebenfalls nur dort; an Komponente 6 oder 7 keine einzige Zeile. Umgekehrt wäre die Umdeutung von
> `EUR_PRO_KWH_KAPAZITAET` genau der Fall, vor dem zu warnen war: dort steht eine ausgelieferte Vorlagenzeile, deren Zahl
> still von €/kWh auf €/Ltr. umgedeutet würde — deshalb blieb sie hier unberührt und wird mit #284 aufgelöst.
>
> **Beschriftung:** ein Persistenzwert, gewerkabhängige Anzeige. `BemessungKatalog.Anzeige(persistenz, komponentenId)` und
> `.Einheit(...)` liefern am BHKW „je kW elektr. Leistung“/„€/kW“, am Pufferspeicher „je Liter“/„€/Ltr.“, sonst
> unverändert; `BetriebskostenCtrl.SatzEinheit` bekam eine Überladung mit Komponente, damit auch die Herleitungszeile des
> Berichts „€/Ltr.“ schreibt. Zwei neue Ressourcenschlüssel in beiden Sprachen.
>
> **Tests:** neue Klasse `BemessungBhkwPufferspeicherTests` (29 Fälle, synthetisches Projekt BHKW 50 kW el / 100 kW th,
> Puffer 1 000 l): je Komponente ein nachgerechneter Betrag, je Komponente ein Fall mit Größe 0, der `BASISGRUND_GERAET`
> statt eines stillen 0 verlangt, dazu die unveränderten Gewerke und beide Beschriftungen in beiden Sprachen. Gegenprobe:
> mit den zwei zurückgenommenen Zeilen sind 9 der 29 rot. Zwei Bestandsklassen zogen nach.
>
> **Abnahme:** Kern-Filter 0 Fehler; Windows-Schale 0 Fehler, 5 Warnungen (Bestand); Gate des Agenten 8 068 grün;
> SQL-Dialektprüfer 1 398 Texte, 0 Fundstellen; Gate sept81 und sept82 grün; Referenzlauf 5 von 5 PASS und byte-gleich
> gegen R7. Der Entscheid wirkt erst an einem gepflegten Satz beim Anwender — dort wird aus einem stillen 0 der Betrag
> Satz × P_el bzw. Satz × Volumen. Merge `72d57143`.
>
> **Offen:** die Vorlagenzeile der Komponente 6 (siehe oben, #284); Windows-Abnahme der Hüllenzeilen in `KopplungAnwenden`.

## #282 — Pufferverwaltung mit OK und Abbrechen, geschrieben wird im OK-Weg (15.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Anwenderentscheid 15.09.2026 auf die offene Frage aus #276: „1. PufferSpProjektDialog: Auch OK/Abbrechen“.
> Das ist nicht die Beschriftung, sondern der Schreibzeitpunkt — wer Abbrechen anbietet, darf vorher nichts geschrieben
> haben.
>
> **Messung zuerst:** Dialog → `PufferSpProjektDienste.Anlegen/Aendern/Entfernen` → `PufferSpProjektHuelle` →
> `PufferSpCtrl` → `StilleDb`. Anlegen: INSERT in `Tab_Pufferspeicher`, danach zwei zielgenaue UPDATE (Klassen-Set,
> Schichtdaten), dazu eine Anlagenzeile in `Tab_Energieanlagen` (`ID_Type = 12`). Ändern: UPDATE auf denselben drei Wegen,
> bei Namenswechsel zusätzlich der Bezeichner der Anlagenzeile. Entfernen: DELETE in `Z_ProjektPufferSp`, in
> `Tab_Energieanlagen`, `ReferenzenLoesen`, DELETE in `Tab_Pufferspeicher`, dann `ProjektWaisenEntfernen`.
>
> **Abhängig vom frühen Schreiben war genau eine Stelle: die neue Id.** Alle drei Wirte lesen erst NACH dem Rückruf
> `Geschlossen` — `SimulationKonfigSeite.VerwaltungFertig` lädt neu, `QuellePufferspeicherDialog` und `WaermesenkeDialog`
> nehmen die gelieferte Id in ihre Auswahl bzw. in die markierte Senkenzeile. Damit war die Abhängigkeit **auflösbar statt
> umbaubar**. Sonst hängt nichts daran: `IstLeitspeicher` und `Referenzen` fragen Beziehungen ab, die anderswo entstehen;
> der Klemmhinweis hängt allein an den Eingaben.
>
> **Umbau:** Der Dialog führt einen Arbeitsstand. „Anlegen“/„Übernehmen“ prüft die Felder (dieselbe Prüfkette, Reihenfolge
> und derselbe Wortlaut) und legt sie als Zeile in die Liste; „Entfernen“ nimmt eine Zeile heraus. Eine vorläufige Zeile
> trägt eine **negative** Nummer, nur eine positive Id hat eine Datenbankentsprechung. Erst OK schreibt — in der
> Reihenfolge Entfernen → Ändern → Anlegen, so ist ein Bezeichner wieder frei, den eine neue Zeile tragen soll; dort
> entsteht die neue Id, die an den Wirt geht. Scheitert ein Schritt, bleibt der Dialog offen und nennt den Grund;
> Geschriebenes wird bei einem zweiten OK nicht wiederholt. Abbrechen, ✕ und Esc verlassen ohne Schreibzugriff.
>
> OK prüft die offene Zeile **nur, wenn an ihr etwas geändert wurde** — sonst verriegelte ein Altbestand, der die heutigen
> Regeln verletzt (etwa ein leeres Klassen-Set), den Dialog. Die Nutzungsrückfrage führt den OK-Weg zu Ende.
>
> **Belegt am Datenbankstand, nicht am Dialogzustand:** Abbrechen nach Anlegen UND Ändern UND Entfernen im selben
> Durchgang ergibt `Schreibzugriffe == 0`; OK schreibt den ganzen Arbeitsstand in der genannten Reihenfolge; Esc verwirft
> wie Abbrechen; eine vorläufige Zeile bekommt ihre Id erst beim OK; dazu je zwei Fälle an allen drei Einbettungsstellen.
>
> **Bewusst in Kauf genommen:** Die drei Kontrollanzeigen (Ladereihenfolge, Automatiktext, Entladeposition) lesen weiter
> die Datenbank und zeigen bis zum OK den gespeicherten Stand; ein doppelter Bezeichner bekommt sein Unterscheidungssuffix
> erst beim Schreiben; der mittlere Knopf behält „Anlegen“/„Übernehmen“, weil „Speichern“ dort eine Behauptung wäre, die
> nicht stimmt; der Klemmhinweis erscheint beim Übernehmen, wer direkt OK drückt, sieht ihn nicht.
>
> **Abnahme:** Kern-Filter 0 Fehler, 0 Warnungen; Windows-Schale 0 Fehler, 5 Warnungen (Bestand); 341 Fälle der acht
> betroffenen Klassen dreimal grün; voller Kern-Lauf des Agenten grün; Gate sept82 grün. Kein Rechenweg berührt.
> Merge `5e01bed7`.
>
> **Offen:** `BhkwWirtschaftlichkeitDialog` als letzte Abweichung (Anwenderentscheid).

## #283 — Regel b im Wärmepumpenweg, und der Wächter, der ihn übersah (15.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Nebenbefund aus #277, Anwenderentscheid 15.09.2026 „Empfehlung“ — die Stelle umstellen **und** den Wächter
> schärfen. `WaermepumpeAnlageHuelle.KostenOeffnen` rief das Kostenfenster als gewöhnliche Anweisung auf und schloss mit
> `Task.CompletedTask`: ein modales Systemfenster direkt aus einem Blazor-Ereignis.
>
> **Warum der Wächter sie nicht fand:** Der `ModalRegex` der `HuellenwegTests` sucht `Task.FromResult(… Huelle.Oeffnen …)`.
> Er kennt nur die eine Schreibweise; „Aufruf als Anweisung, danach `return Task.CompletedTask;`“ fällt durch.
>
> **Zweite Regel statt erweitertem Regex:** Die neue Frage gilt dem ganzen Methodenrumpf — Rückgabetyp `Task`, kein
> `await`, kein `Blazornachlauf`/`Blazorsprung`, Abschluss mit `Task.CompletedTask`/`Task.FromResult` — und ein Regex kann
> einen Rumpf nicht begrenzen (Klammertiefe). Die zweite Regel bringt deshalb einen eigenen Leser mit, der den Rumpf über
> die Klammertiefe herausschneidet. Die Aufrufliste steht nun einmal und wird von beiden Regeln benutzt. Eine dritte
> Schreibweise (derselbe Rumpf als Lambda im Gabensatz) kommt im Bestand nicht vor, ist aber mitgeprüft.
>
> **Der Beleg:** Der Wächter in seinem Endstand, gelaufen gegen den Bestand VOR der Korrektur, meldet genau zwei Stellen
> und keine weitere — `KatalogDublettenHuelle.ProtokollSpeichern` und `WaermepumpeAnlageHuelle.KostenOeffnen`.
>
> **Beide umgestellt.** Die Wärmepumpe über `return Blazornachlauf.Nachgelagert(() => …)` wie der gleichlautende
> Brennerweg aus #277; der Projektname wird weiterhin davor gelesen, er gehört nicht in die nachgelagerte Nachricht. Die
> zweite Stelle ist **kein Falsch-Positiv**, sondern derselbe Befund am Dateiwähler (W13‑B‑1): Die Methode fuhr
> `Dienste.Datei.DateiSpeichern` synchron hoch und gab das Ergebnis als `Task.FromResult` heraus; jetzt
> `await Dienste.Datei.DateiSpeichernAsync(…)`, der seinerseits über `Blazornachlauf` läuft. Keine Ausnahmeliste angelegt —
> es war keine nötig.
>
> **Zwei Nebenfunde:** `Datei.DateienOeffnen` fehlte in der alten Aufrufliste (`Datei.DateiOeffnen` deckt es nicht ab),
> ergänzt für beide Regeln. Und beide Regeln zusätzlich über `EPOS.UI.Daten` und `EPOS.UI` laufen lassen: null
> Fundstellen.
>
> **Abnahme:** Kern-Filter 0 Fehler; Windows-Schale 0 Fehler, 5 Warnungen (Bestand); `HuellenwegTests` 4 von 4 grün (je
> ein Bestandsfall und eine Gegenprobe pro Regel, die Gegenprobe der zweiten trägt `KostenOeffnen` im Wortlaut vor der
> Umstellung); Kern-Gate des Agenten 8 042 grün; Gate sept81 und sept82 grün. Kein Rechenweg berührt, kein Referenzlauf
> nötig. Merge `0643cbbb`.
>
> **Offen:** Dateikreis des Wächters (Zuschnittsentscheid, siehe Statusdatei „Nach #283“).

## #284 — Pufferspeicher bemisst sich nur am Volumen (15.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Anwenderentscheid 15.09.2026 zur Vorlagenzeile, die #279 gefunden hatte: „Prüfe Pufferspeicher
> Daten mit Volumen/Größe. EUR_PRO_KWH_KAPAZITAET spielt keine Rolle, nur das Volumen als Bezugsgröße."
>
> **Zwei Dinge, nicht eines.** Die eine Hälfte ist die Auswahl, die dem Anwender angeboten wird; die andere
> ist die Zeile, die schon ausgeliefert wird. Nur beide zusammen lösen den Fall auf: Ohne die Auswahl könnte
> er sich morgen wiederholen, ohne die Datenumstellung bliebe er im Bestand stehen.
>
> **Die Auswahl.** Der `BemessungKatalog` war eine flache Liste ohne Zuordnung je Gewerk — jede Art wurde
> jedem Gewerk angeboten, auch eine, für die es dort keine Bezugsgröße gibt. Ein Satz an so einer Zeile fällt
> über den Anwenderentscheid I‑2 auf den erfassten Betrag zurück; bei einer reinen Satzzeile ist das 0, und
> zwar ohne Warnung. Das ist derselbe Mechanismus, den #271 an der Solarthermie gefunden hat.
>
> **Die Zuordnung steht an EINER Stelle.** `BemessungKatalog.PasstZuGewerk(art, gewerk)` und `Auswahl(…)`
> fragen `WirtschaftlichkeitCtrl.BasisGrund` und lesen damit dieselbe Landkarte, aus der der Dialog schon
> seinen Grundtext holt (`TechnikPlanwertCtrl`, `EndenergieAufloeser`). Eine zweite Zuordnung wäre eine
> zweite Wahrheit — es gibt keine. Der GELTUNGSBEREICH ist benannt statt eingestreut
> (`AUSWAHLFILTER_GEWERKE`) und umfasst heute allein den Pufferspeicher; die übrigen neun Gewerke behalten
> ihre vollständige Auswahl, bis der Anwender über sie entschieden hat, und die Erweiterung ist dann eine
> Zeile. **Eine Art, die eine vorhandene Zeile trägt, bleibt überall in der Liste** — sonst verschwände ein
> gepflegter Wert aus der Auswahl, und der Anwender könnte ihn nicht mehr ändern.
>
> **Die Daten: Schemaschritt 77** (`SchemaStand.Zielversion` 76 → 77, `PufferspeicherBemessungVolumen`).
> Die ausgelieferte Investitionsvorlage des Pufferspeichers trägt jetzt die Bemessung je Liter Gesamtvolumen.
> Kern, Migration, das Werkzeug `Testdatenbankschema` und der Nachweis lesen dieselbe Quelle; die Saat der
> zwanzig Auslieferungsvorlagen trägt dieselbe Art. **Umgestellt wird nur eine Zeile mit `Satz IS NULL`:**
> Ein gepflegter Satz wäre eine Zahl je kWh und dürfte nicht stillschweigend zu einer Zahl je Liter werden —
> das wäre eine Zahlenänderung ohne Anlass. Solche Zeilen zählt der Migrationsbericht (`ZaehlungGepflegt`),
> statt sie anzufassen. Der Fall ist überhaupt möglich, weil der Schreibschutz der Auslieferungsvorlagen
> seit Ä8 aufgehoben ist. `Tab_ProjektWerte` und die Vorlagen der übrigen neun Komponenten bleiben unberührt.
>
> **Die Testdatenbank nachgezogen:** genau eine Zeile umgestellt (Position 38, Vorlage 6, Satz NULL),
> `integrity_check` ok, `foreign_key_check` leer, 119 STRICT-Tabellen, 70 766 592 Byte, Schemastand 77. Die
> beiden Pufferspeicher der Referenzprojekte 1007 und 1046 sind nicht angefasst; `EUR_PRO_KWH_KAPAZITAET`
> kommt in `Tab_ProjektWerte` überhaupt nicht vor (unabhängig nachgezählt: 0).
>
> **Abnahme:** Kern-Filter 0 Fehler; Windows-Schale 0 Fehler, 5 Warnungen (Bestand); 8 097 Tests grün;
> `SqlDialektPruefer` 0 Fundstellen; Referenzlauf gegen `2026-09-11_R7_Speicherflotte` 5/5 PASS und 5/5
> byte-gleich; Gate sept83 grün. Der Rechenweg ist nicht berührt — die Zeile rechnete vorher 0 und rechnet
> jetzt mit dem Volumen, aber kein Referenzprojekt führt sie. Merge `bb550eeb`.
>
> **Offen:** Ausweitung der Filterung auf die übrigen neun Gewerke und die gleich gelagerte PV-Vorlagenzeile
> „Batteriespeicher" (beides Anwenderentscheid, siehe Statusdatei „Nach #284").

## #285 — Summenlinien heißen nach ihrer Summe und sind abschaltbar (15.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Anwenderwunsch 15.09.2026 mit Bildschirmfoto des Blatts „Wärme Produktion Chart": Die grüne
> Linie „Gesamt" soll „Summe Wärmeerzeugung" heißen und ein Kästchen bekommen wie die Wärmepumpe daneben;
> gleichartige Darstellungen sollen mitgeändert werden.
>
> **Der Name war nur die Oberfläche.** Darunter lag ein Schlüssel für drei Dinge: `CHART_LEGENDE_GESAMT`
> beschriftete die Summenlinie des Wärmegangs, die des Stromgangs, die des Bedarfsreiters **und** den ersten
> Eintrag der Klappliste „Bedarfsart". Eine globale Umbenennung hätte aus einer Verbrauchssumme eine
> Erzeugung gemacht und den Auswahleintrag gleich mit umbenannt.
>
> **Deshalb zuerst gemessen, was jede Linie addiert** — nicht, wie sie heißt:
>
> | Diagramm | Summanden im Code | Name |
> |---|---|---|
> | Wärmegang | die gestapelten Erzeugerreihen | Summe Wärmeerzeugung |
> | Stromgang | Lastgang + Wärmepumpe + Heizstab + Heizkessel | Summe Stromverbrauch |
> | Bedarfsreiter | der gesamte Wärmebedarf über alle Bedarfsarten | Summe Wärmebedarf |
>
> Dass der Stromgang eine **Verbrauchs**summe führt, ist zweifach belegt: Der Bildtitel heißt „Strombedarf,
> Stromverbrauch Jahresganglinie", und BHKW wie Photovoltaik stehen als Erzeugungslinien NEBEN dem Stapel
> und gehen in die Kontur ausdrücklich nicht ein. „Summe Stromerzeugung" wäre dort schlicht falsch gewesen.
>
> **`CHART_LEGENDE_GESAMT` bleibt** — an der einen Stelle, an der „Gesamt" zutrifft: der Klappliste
> „Bedarfsart". Dort benennt es eine Auswahl, keine Linie; die Trennung der Schlüssel verhindert, dass die
> Umbenennung dorthin durchschlägt.
>
> **Warum der Schalter fehlte.** Die Kontur des Wärmegangs hing nicht an der Reihenwahl: Sie entstand,
> sobald `stapel.Count > 0`. Es gab also nichts zu schalten, und `WaermegangDaten` führte die Reihe gar
> nicht erst. Jetzt ist sie eine Reihe wie die anderen — als erster Eintrag der Erzeugerliste, vorbelegt an,
> und `Vorhanden` hängt daran, dass es überhaupt einen Erzeuger gibt, damit kein Schalter ohne Wirkung
> erscheint. Die Kontur entsteht nur noch bei gewählter Reihe; sonst ist am Zeichenweg nichts geändert —
> gleiche Reihenfolge, gleiche Farbe, gleiche Strichstärke.
>
> **Die Summe steht über ihren Summanden, nicht neben ihnen.** Dafür bekam der Baustein `Mehrfachauswahl`
> den Parameter `AbsatzNach`: eine Trennlinie nach n Einträgen. Die Summe bleibt damit IN der Liste —
> „Alle" und „Keine" fassen sie mit, und sie fällt in dasselbe Sitzungsgedächtnis wie die Erzeuger — ist aber
> sichtbar abgesetzt. Der Stromgang bekam dieselbe Anordnung.
>
> **Was bewusst nicht angefasst wurde, je mit Grund:** die CSV-Schlüssel der Referenzbasis (sonst kein
> Vergleich gegen die eingefrorene Basis mehr möglich); die Speicherreihe „Speicher gesamt" im Netzbild (der
> Name sagt schon, was sie summiert, und sie ist längst abschaltbar — dazu eine Linie unter Linien, keine
> Kontur über einem Stapel); die kumulierte Linie der Jahresprojektion (eine Zeitkumulierte, keine
> Spaltensumme); Peak-Shaving- und Speicherbetriebsbild (übergeben ausdrücklich keine Kontur); die
> Fachreiter der Erzeuger (keine Kontur, ihre Linien sind Bezugsgrößen und einzeln abwählbar); der Bericht
> (benutzt keinen der drei Schlüssel). Eine Summenlinie, die das Diagramm trägt und deshalb nicht
> abschaltbar sein dürfte, gibt es nirgends.
>
> **Abnahme:** Kern-Filter 0 Fehler; Windows-Schale 0 Fehler, 5 Warnungen (Bestand); 8 101 Tests grün;
> ChartProben 64 Bilder, 0 Verstöße, und im Vergleich zweier Läufe vor und nach der Änderung **alle 49 PNG
> byte-gleich** — der Renderer zeichnet die Kontur mit eigenem Text, nicht über die Ressource, also ist der
> Zeichenweg nachweislich unberührt; Referenzlauf gegen `2026-09-11_R7_Speicherflotte` 5/5 PASS und alle 135
> CSV byte-gleich; Gate sept84 grün. Vier neue Prüfungen in `GangUndErgebnisReiterTests`. Merge `19b3ca8b`.
>
> **Offen:** `CHART_CSV_GESAMT` ohne Verwender (siehe Statusdatei „Nach #285").

## #280 — Fehlende Werte benennen, aus der Kategorie nur mit Rückfrage leihen (15.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Anwenderentscheid 15.09.2026, vier Punkte: CO₂-Rückfall aus der Kategorie **mit Rückfrage**
> („Werte nicht ohne Wissen setzen!"), Warnung bei fehlenden Emissionswerten, dasselbe für die Kosten,
> Kosten als Pflicht für die Wirtschaftlichkeitsrechnung — und gleiche Werte für alle Anlagen eines
> Trägers.
>
> **Zwei der vier Punkte standen schon.** Das war das Ergebnis der Messung, bevor irgendetwas gebaut
> wurde: Die Trägerregel (Punkt 4) ist seit #268 umgesetzt und seit #278 über den UNIQUE-Index
> `idx_EnergyProjectSettings_Traeger` auch im Schema erzwungen — zwei Anlagen, die auf denselben Träger
> zeigen, **können** gar nicht verschiedene Werte lesen. Der Anwender hat Punkt 4 am selben Tag
> ausdrücklich auf den Träger eingegrenzt (nicht auf die Gruppe), damit Varianten ihren Zweck behalten.
> Und Punkt 3 hält die Wirtschaftlichkeit seit #267 ein: „Arbeitspreis 0 zählt als NICHT gepflegt",
> sechs benannte Gründe, Warnband mit „Neu berechnen", „—" statt eines erfundenen Betrags. Beides blieb
> unangetastet.
>
> **Die wirkliche Lücke lag davor — an der Pflegestelle.** `EnergietraegerHuelle.Speichern()` prüfte
> beim Schreiben nur, ob die Einheit auf kWh umrechenbar ist; ein Arbeitspreis 0, ein Leistungspreis 0
> oder ein CO₂-Wert 0 ließen sich **ohne jede Meldung** speichern. Auffallen konnte es erst beim
> Rechnen. Dazu ein zweiter Befund: `Emissionsquelle` setzt `Co2Gepflegt = false` mit der Herkunft
> „kein Emissionsfaktor gepflegt" — aber **kein einziger Aufrufer las das Feld** außer der eigenen Datei
> und ihren Tests. Die Kennzeichnung war da, nur zeigte sie niemand.
>
> **Die Karte sagt es jetzt, wo der Wert gepflegt wird:** die Preise im Reiter „Preise & Umrechnung",
> der CO₂-Wert im Reiter „Emissionen". Die Lücke misst sich an **Karte und Lesekette zusammen** — die
> Frage lautet „bliebe hier eine Lücke, wenn ich jetzt speicherte?", also das Feld, sonst die Kette.
> Eine gepflegte Saisonreihe zählt beim Leistungspreis als gepflegt, und ein Träger ohne Leistungspreis
> wird gar nicht erst danach gefragt. **Gesperrt wird nichts:** Ein halb gepflegter Träger muss sich
> anlegen lassen, und derselbe Hinweis erscheint beim Speichern.
>
> **Der Übernahmeweg ist EIN Weg für drei Größen** (`Controller/EnergietraegerRueckfall.cs`): Kategorie,
> `Wert()`, `Kandidaten()`, `Uebernehmen()`. `Wert()` baut keine eigene Lesekette nach, sondern fragt die
> bestehenden — CO₂ über `Emissionsquelle.Fuer(…).Co2Gepflegt`, die Preise über eine neue, rein lesende
> Auskunft, die dieselbe Vorrangkette Projekt → Preisstand → Katalog benutzt. Eine zweite Wahrheit
> entsteht nicht.
>
> **Die Kategorie ist `energy_carrier.pricing_model`**, im Code KATEGORIECODE — die Spalte, auf der schon
> die Zulässigkeitsprüfung aus #268 rechnet. Die angezeigte „Gruppe" wäre die falsche Klammer, und das
> ist gemessen, nicht vermutet: Die acht GASEOUS_FUEL-Träger der Testdatenbank verteilen sich auf drei
> Gruppen (Gas, Wasserstoff, Sonstige). Über die Gruppe zu suchen hielte Geber zurück, die fachlich zur
> selben Familie gehören — und Zulässigkeit und Übernahme ruhten auf zwei verschiedenen Wahrheiten.
>
> **Nichts wird ohne Bestätigung gesetzt.** Auch der einzige Kandidat wird vorgelegt. Gibt es keinen,
> sagt der Weg das und öffnet nichts. Die Reihenfolge ist fest und kulturunabhängig: erst die dem
> Projekt zugeordneten Träger, dann die übrigen, je nach Namen und bei Gleichstand nach Id. Geschrieben
> wird in die **Projektübersteuerung** — an der Lesekette nachgeprüft, sie ist dort die oberste Ebene,
> der Wert gilt also sofort und ohne zweite Regel. Der Katalog gilt für alle Projekte und wird nie
> angefasst; im Katalogkontext erscheint deshalb der Hinweis, aber kein Knopf.
>
> **Einheitentreue — vom Auftrag nicht verlangt, aber nötig.** Ein Preis ist eine Zahl je
> Abrechnungseinheit. 0,95 €/L in einen Träger zu schreiben, der nach Nm³ abrechnet, wäre eine falsche
> Zahl mit richtigem Anschein (GASEOUS_FUEL führt in der Testdatenbank Nm³ **und** kg). Als Geber eines
> Preises kommt deshalb nur in Frage, wer dieselbe Abrechnungseinheit führt, beim Leistungspreis
> zusätzlich denselben Modus. Der CO₂-Faktor steht überall in g/kWh und kennt die Einschränkung nicht.
> Geprüft wird beim Übernehmen gegen die frisch gelesene Kandidatenliste, nicht gegen die angezeigte.
>
> **Kein Rückfall im Rechenweg.** `Emissionsquelle` und `KostenEmissionRechner` sind inhaltlich
> unverändert; ein zugeordneter Träger ohne CO₂-Wert bleibt eine Datenlücke. Ein Wächter hält das fest,
> und der Beleg ist der byte-gleiche Referenzlauf: Wäre ein Rückfall in den Rechenweg geraten, hätten
> sich Zahlen bewegt.
>
> **Abnahme:** Kern-Filter 0 Fehler; Windows-Schale 0 Fehler, 5 Warnungen (Bestand); 8 113 Tests grün,
> davon 12 neue in `EnergietraegerRueckfallTests`; `SqlDialektPruefer` 1 407 Texte, 0 Fundstellen;
> Referenzlauf gegen `2026-09-11_R7_Speicherflotte` PASS und byte-gleich; Gate sept85 grün. Merge
> `ad37b3d3`. Hausregel „Ein geliehener Wert wird nie still gesetzt" in `EPOS.Kern/CLAUDE.md`.
>
> **Offen:** Die Herkunft eines geliehenen Wertes wird nicht persistiert (siehe Statusdatei „Nach #280").

## #281 — Größensuche wählt Geräte, sie erfindet keine (15.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Anwenderentscheid 15.09.2026: „Größensuch Stromspeicher nur über Kapazität und Leistung im
> Katalog des Projektes oder wahlweise aus Stammdaten. Bei Such aus Stammdaten mit Möglichkeit der
> Übernahme aus Stammdaten in Projekt." Am selben Tag geschärft: „Sind ‚Projektkatalog oder Stammdaten'
> → Kandidaten (Stromspeicher, die Bedingungen Größe (Leistung, Kapazität) erfüllen bzw. nahe kommen)."
>
> **Der Bruch liegt im Gegenstand der Suche, nicht in der Bedienung.** Bisher variierte die Suche eine
> freie Größe und koppelte die zweite Achse über die C-Rate. Was dabei herauskam, war eine Zahl, kein
> Speicher: Der Anwender bekam eine Auslegung vorgelegt, die er nicht kaufen kann, und musste danach
> selbst ein Gerät suchen, das ungefähr passt. Jetzt gibt er je einen Bereich für Kapazität und Leistung
> vor, wählt die Quelle, und die Suche rechnet die Geräte durch, die im Projektkatalog oder in den
> Stammdaten stehen.
>
> **Die C-Rate fällt als Suchachse, nicht als Kennzahl.** `FlottenAuslegungsmodus.KapazitaetUndLeistung`
> ist die einzige gültige Kopplung; die beiden C-Rate-Werte bleiben im Enum, damit gespeicherte Stände
> lesbar bleiben (JSON führt dort 1 bzw. 2). Die C-Rate steht weiter an jedem Gerät als abgeleitetes
> P/E, in keinem Eingabefeld. Ein Stand mit C-Rate-Kopplung wird beim Laden **benannt** umgerechnet
> (`FlottenAltstand.Normalisiere`): Der Leistungsbereich entsteht aus den Ecken P = E · C, der
> Kapazitätsbereich als E = P / C. Das ist der Unterschied zwischen „die Absicht übersetzen" und „den
> Stand still verwerfen" — der Anwender findet seinen Suchraum wieder, nur anders beschriftet.
>
> **Die Auswahlregel steht an einer Stelle** (`FlottenGeraetewahl.Waehle`). Treffer sind die Geräte in
> beiden Bereichen, Grenzen eingeschlossen; gibt es welche, sind nur sie die Kandidaten. Sonst kommen
> höchstens fünf nächstliegende, damit die Suche nie leer ausgeht. Der Abstand je Größe ist der
> Überstand über den Bereich, bezogen auf die **Bereichsmitte**, und beide Anteile werden addiert: So
> wiegt ein Überstand von 10 kWh bei einem schmalen Bereich schwerer als bei einem weiten, und Kapazität
> und Leistung sind trotz verschiedener Einheiten vergleichbar. Sortiert wird nach Abstand, Kapazität,
> Leistung, Quellkennung — zwei Läufe auf derselben Datenbank liefern dieselbe Reihenfolge. Die
> Abweichung in Prozent steht in der Kandidatentabelle und in der Geräteliste der Station „Optimierung".
>
> **Die Kandidatenzahl kann nicht mehr explodieren.** Sie ist die Zahl der gefundenen Geräte, nicht mehr
> das Produkt zweier Rasterachsen; die Schranke „Maximale Auslegungskandidaten" bleibt als Fangnetz
> stehen. Das **Feinraster entfällt ersatzlos**: Zwischen zwei Geräten liegt kein drittes, und eine
> zwischengerechnete Größe wäre ein Speicher, den es nicht gibt. Der gespeicherte Schalter bleibt lesbar
> und wirkungslos; die Zählregel nennt `FeinHoechstens` durchgehend 0.
>
> **Die Parameter kommen vom Gerät.** Fehlen sie, greifen die neutralen Vorgaben aus
> `FlottenGeraetevorgaben` an **einer** Stelle (Lade- und Entladewirkungsgrad je 0,95, SoC-Fenster
> 0,10…0,90, keine Alterung), und der Kandidat wird gekennzeichnet. Ein Katalogsatz führt keine
> Betriebsführung und ist deshalb immer gekennzeichnet — der Anwender sieht, welche Zahl gemessen und
> welche angenommen ist.
>
> **Ein Fehler, in dieser Welle gefunden und behoben** (`75a582c5`): Die Rückabbildung eines **nicht
> besten** Kandidaten nahm ihre Vorlage aus der Suchachse. Unter „Größe suchen" bringt aber jeder
> Kandidat sein eigenes Gerät mit — Wirkungsgrade, SoC-Band, Hilfsverbrauch, Kostensätze. Der Anwender
> hätte ein Gerät mit den Kennwerten eines anderen übernommen: dieselben Zahlen im Bild, andere im
> nächsten Lauf. Die Vorlage kommt jetzt in fester Reihenfolge — Einheit gleicher Kennung im
> Arbeitsstand, Gerät des Kandidaten über seine Quellkennung, Achsenvorlage, erste Einheit. Der beste
> Kandidat war nie betroffen: Für ihn legt der Optimierer seine eigene Konfiguration bei.
>
> **Nicht angefasst:** „Stückzahl suchen" bleibt unverändert; die Übernahme eines gefundenen
> Stammdatengeräts nimmt den vorhandenen Weg (`EinheitenInProjektUebernehmen`), ein zweiter wird nicht
> gebaut. Der Einzelspeicher-Optimierer je Anlage bleibt beim Rastern (siehe Statusdatei „Nach #281").
>
> **Abnahme:** Kern-Filter 0 Fehler; Windows-Schale 0 Fehler, 5 Warnungen (Bestand); 8 117 Tests grün;
> `SqlDialektPruefer` 0 Fundstellen; ChartProben 47 von 49 Bildern byte-gleich —
> `flottenraster_schraffur` zeigt jetzt die dünn besetzte Gerätekarte Kapazität × Entladeleistung,
> `flottenschnitt_leistung` die Kurve über den Entladeleistungen der gefundenen Geräte; Referenzlauf
> gegen `2026-09-11_R7_Speicherflotte` PASS und byte-gleich; Gate sept86 grün. Merge `cafa6313`.
> Konzept „Stromspeicher-Dialoge" Abschnitt 8.9, Doku Mehrspeicher und die beiden Wiki-Quellen
> (Bedienung, Rechenweg) nachgezogen — noch nicht hochgeladen.

## #286 — Die letzte Maske ohne Abbrechen (15.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Anwenderentscheid 15.09.2026 auf den offenen Punkt aus #282: Der
> `BhkwWirtschaftlichkeitDialog` war die letzte Maske ohne Abbrechen und bekommt denselben
> Umbau wie die Pufferverwaltung. Wieder gilt: Das ist keine Frage der Beschriftung, sondern
> des Schreibzeitpunkts. Wer Abbrechen anbietet, darf vorher nichts geschrieben haben.
>
> **Die Messung fiel anders aus als bei #282.** Geschrieben wurde **nicht** beim Tippen und
> nicht beim Verlassen eines Feldes, sondern erst beim Klick auf den nicht schließenden
> „Speichern"-Knopf: Dialog → Rückruf `Speichern` → `BhkwWirtschaftlichkeitHuelle.Speichern`
> bzw. `IosProjektQuelle.Speichern` → `KwkgAnlagenCtrl.Speichere` je Anlagenzeile und
> `WirtschaftlichkeitCtrl.SpeichereParameter`. Kein zweiter Weg, kein Inline-SQL. Der Dialog
> schrieb aber **jede Eingabe sofort in die vom Wirt hereingereichten Objekte**, und genau
> die gingen in den Schreibweg. Nach einem Klick auf Speichern gab es kein Zurück — deshalb
> trug die Leiste „Speichern"/„Schließen" statt OK/Abbrechen, und `SpeichernLeiste.MitAbbrechen`
> nannte diesen Dialog namentlich als die eine Ausnahme des Hauses.
>
> **Am frühen Schreiben hing niemand.** Beide Wirte werfen ihren Gabensatz nach dem
> Schließen weg und laden neu (`WirtschaftlichkeitSeite.Fertig`, `AppWurzel.ZurueckZurListe`);
> gebraucht wird allein die Kennzeichnung, ob gespeichert wurde, für die Aufforderung zum
> Nachrechnen. Anders als bei #282, wo die neue Id aufzulösen war, gab es hier **keine**
> Abhängigkeit zu behandeln. Einbettungsstellen: zwei, dazu das eigenständige Fenster.
>
> **Der Arbeitsstand ist bewusst kein Vollabbild.** `BhkwAnlagenstand` führt je Zeile die elf
> gepflegten Felder, `BhkwVorgabenstand` die 17 Projektfelder — nicht die Kern-Objekte im
> Ganzen. Der Grund ist die Fehlerquelle, die ein Vollabbild mitbringt: Ein beim Kopieren
> vergessenes Feld ginge beim Schreiben verloren. Was der Dialog nicht pflegt, bleibt in der
> geladenen Zeile stehen. Anlagentabelle, Warn- und Herleitungszeilen und der Knopf
> „Vorschlag übernehmen" lesen und schreiben denselben Stand, damit Tabelle und Felder nicht
> auseinanderlaufen.
>
> **OK schreibt**, in der Reihenfolge Anlagenzeilen (Listenreihenfolge) vor Projektvorgaben —
> so steht der Projektwert nie vor den Zeilen, die ihn überschreiben. Scheitert ein Schritt,
> bleibt der Dialog offen und nennt die Zahl der gescheiterten Sätze im bisherigen Wortlaut;
> Geschriebenes wird bei einem zweiten OK nicht wiederholt.
>
> **Dafür musste die Schreibnaht aufgetrennt werden.** `Speichern` als `Func<int>` sagte
> nicht, welcher Schritt durch war; an seine Stelle treten `SpeichereAnlage` und
> `SpeichereVorgaben`, beide mit Rückgabe. `BhkwDialogDaten` und beide Hüllen sind
> nachgezogen, die Fehlerklammer liegt jetzt in den Hüllen. Ohne diesen Schnitt wäre „nicht
> zweimal schreiben" nicht einlösbar gewesen.
>
> **Abbrechen, ✕ und Esc** verlassen ohne einen Schreibzugriff — und ohne dass sich ein
> hereingereichtes Objekt geändert hätte. Der OK-Knopf heißt „Speichern": Hier stimmt es, er
> schreibt und schließt. In #282 blieb der mittlere Knopf bei „Anlegen"/„Übernehmen", weil
> „Speichern" dort eine Behauptung gewesen wäre, die nicht zutrifft.
>
> **Der Sprung nimmt jetzt den OK-Weg.** Die zwei Sprungknöpfe schreiben und schließen. Ohne
> das wären Eingaben mit dem Sprung verloren: Die Hülle lädt beim Zurückbringen neu, und
> einen nicht schließenden Speichern-Knopf gibt es nicht mehr. Ein Hinweis sagt es in beiden
> Sprachen (siehe Statusdatei „Nach #286").
>
> **Eine Wache gegen den Rückfall.** `FussleisteAusgaengeTests` lässt keine `SpeichernLeiste`
> im Haus mit „Speichern", aber ohne „Abbrechen" zu — genau die Paarung, bei der ein nicht
> schließender Knopf ohne Verwerfen dasteht. Sie prüft jede Razor-Datei, nicht diesen Dialog,
> und ist am eingefrorenen Bestand von vorher rot (Gegenprobe nach Lehre W6-B-1). Ein reiner
> Ansichtsdialog mit nur „Schließen" bleibt erlaubt: Dort gibt es nichts zu verwerfen.
> `EPOS.UI/CLAUDE.md` hält die Regel jetzt ohne Ausnahme.
>
> **Abnahme:** Kern-Filter 0 Fehler, 0 Warnungen; Windows-Schale 0 Fehler, 5 Warnungen
> (Bestand); 136 Fälle der betroffenen Klassen dreimal grün ohne Flattern; voller Kern-Lauf
> 8 129 grün; Referenzlauf gegen `2026-09-11_R7_Speicherflotte` PASS und byte-gleich; Gate
> sept87 grün. Kein Rechenweg berührt, kein SQL angefasst. Merge `3f2c70b9`.
>
> **Offen:** der Sprung-Entscheid und drei Befunde aus derselben Messung (siehe Statusdatei
> „Nach #286").

## #287 — Die Auswahl folgt der Bezugsgröße, auch bei der PV-Zeile (15.09.2026, Nachtrag aus dem Merge)

> **Anlass:** Zwei Anwenderentscheide vom 15.09.2026 auf die offenen Punkte aus #284 — die
> Filterung der Bemessungsarten auf die übrigen neun Gewerke ausweiten, und die
> PV-Vorlagenzeile „Batteriespeicher" (Vorlage 5, Komponente 3) mit `EUR_PRO_KWH_KAPAZITAET`
> auflösen. Wieder dieselben zwei Hälften: die Auswahl, die dem Anwender angeboten wird, und
> die Zeile, die schon ausgeliefert wird.
>
> **Die Matrix wurde neu ausgezählt, bevor sie scharf geschaltet wurde** — je Gewerk **und je
> Raster**, aus `WirtschaftlichkeitCtrl.BasisGrund`, also derselben Landkarte, dazu der
> Bestand. Der Grund: Eine Filterung, die nur ein Gewerk traf, war billig zu belegen; jetzt
> trifft sie alle zehn, und ein Fehler wäre nicht mehr auf eine Maske begrenzt.
>
> **Kein Gewerk wird leergeräumt, keines behält nur die Pauschale.** Die dünnsten Listen sind
> die Betriebsraster der vier Gewerke ohne Geräte- und Laufgrößen (Pufferspeicher,
> Wärmezentrale, Bauliche Anlagen, Stromeinspeisung): fester Jahresbetrag **und** „% der
> Investition". Die zweite trägt dort wirklich — ihre Basis ist die Investitionskaskade, nicht
> ein Gerät. Für den Pufferspeicher ist genau das seit #284 ausgeliefert; die drei übrigen
> landen auf demselben Bild. Die dünnste Investitionsliste hat drei Arten.
>
> **Genau eine Bestandszeile fiel heraus** — und zwar dieselbe, um die es in der zweiten
> Hälfte geht: `Tab_KostenVorlagePosition` 33, Satz `NULL`. Sonst nichts: weder in den zwanzig
> Auslieferungsvorlagen noch irgendwo in `Tab_ProjektWerte` (175 Zeilen, alle zehn Gewerke),
> und in **keinem** Referenzprojekt. Der Bestandsschutz aus #284 greift unverändert und ist je
> Gewerk × gewerksfremder Art einzeln geprüft.
>
> **Die PV-Zeile wird ein fester Betrag, nicht „je kWp".** Die Begründung ist gemessen, nicht
> geraten, und sie ist der Kern dieser Hälfte:
>
> - Die Photovoltaik führt im Investitionsraster genau **zwei** Arten mit echter Baugröße —
>   `EUR_PRO_KWP` und `EUR_PRO_KW_ELEKTRISCH` —, und **beide liefern dieselbe Zahl**
>   (`PhotovoltaikCtrl.KwpSumme`, Modulanzahl × Modulleistung). Ein Batteriespeicher-Satz je
>   kWp bemäße den **Preis eines Geräts an der Größe eines anderen**: eine erfundene
>   Bezugsgröße anstelle der alten, nicht die richtige.
> - Die Vorlage sagt selbst, wie sie Gerätepositionen ohne eigene Baugröße bemisst:
>   „Wechselrichter", „Montagesystem / Unterkonstruktion" und „Bauliche Anlagen" stehen **alle**
>   auf festem Betrag; nur die Modulzeile trägt den kWp-Satz. Der Batteriespeicher reiht sich
>   ein.
> - Ein fester Betrag ist absolut — der erfasste Wert **ist** der Betrag. Es gibt keine Menge,
>   die fehlen könnte, also auch keinen stillen Betrag 0. Genau das war der Mechanismus, den
>   #271 an der Solarthermie und #284 am Pufferspeicher gefunden hat.
> - Die kapazitätsbemessene Zeile geht nicht verloren: Sie steht am Gewerk **Stromspeicher**
>   („Speicher", Bezugsgröße `Tab_Stromspeicher.Energie`) — unverändert.
>
> **Schemaschritt 78** nach dem Muster 77: eine Quelle (`PvVorlageBatteriespeicher`), aus der
> Kern, Migration, das Werkzeug `Testdatenbankschema`, der Nachweis und die Saat der zwanzig
> Auslieferungsvorlagen lesen. Die Anweisung filtert am **Gewerk**, nicht am Positionsnamen —
> ein Name ist keine Zusicherung —, und fasst nur eine Zeile mit `Satz IS NULL` an. Eine Zeile
> mit gepflegtem Satz wird gezählt (`ZaehlungGepflegt`), nicht geändert: Ein Satz je kWh darf
> nicht stillschweigend zu einem festen Betrag werden.
>
> **Testdatenbank nachgezogen:** Schemastand 78, 70 766 592 Byte (unverändert), 119
> STRICT-Tabellen, `integrity_check` ok, `foreign_key_check` leer. Tabellenweiser
> Fingerabdruck gegen den Git-Stand: **genau zwei** Tabellen ändern sich — `Tab_Applikation`
> (77 → 78) und `Tab_KostenVorlagePosition` (Zeile 33; die übrigen 120 Zeilen unverändert).
>
> **Abnahme:** Kern-Filter 0 Fehler; Windows-Schale 0 Fehler, 5 Warnungen (Bestand); 8 144
> Tests grün, davon 27 neue in `BemessungsauswahlJeGewerkTests` (je Gewerk ein Fall mit
> vollständiger Invest- und Betriebsliste, „kein Gewerk ohne Auswahl", Bestandsschutz je
> Gewerk) und `PvBatteriespeicherBemessungTests` (Landkarte, Saat = Nachzug, gepflegte vs.
> ungepflegte Zeile, Wiederholbarkeit); `SqlDialektPruefer` 1 409 Texte, 0 Fundstellen;
> Referenzlauf gegen `2026-09-11_R7_Speicherflotte` 5/5 PASS und **135 von 135 Dateien
> byte-gleich** (sha256-weise nachgezählt); Gate sept88 grün. Keine Einfrierregel berührt, die
> Basis bleibt. Merge `4cf8efe1`.
>
> **Offen:** ein flatterhafter Fremdtest und eine Datei ohne BOM (siehe Statusdatei „Nach #287").

## #288 — Zwei Wege werden einer, eine Betriebsart fällt (15.09.2026, Nachtrag aus dem Merge)

> **Anwenderwunsch und Entscheid.** „‚Nachtnutzung' herausnehmen und nicht mehr verwenden."
> Die Messung ergab, dass der Name zwei verschiedene Dinge trägt: eine **tote**
> Auslegungsstrategie des Einzelspeicher-Optimierers und eine **lebende** Berechnungsart des
> Simulationslaufs (`StromspeicherSimCtrl.BaueStrategie`, Klappliste im Ergebnis, gespeichert
> in `Tab_StromspeicherVariante.Berechnungsart`). Auf die Rückfrage, ob beides oder nur das
> tote Stück fallen soll: „(a) Beides raus". Dazu der offene Punkt aus #281 — die Gegenprobe
> bestätigte, dass `SpeicherOptimierungCtrl.Rechnen` außerhalb der Tests **keinen Aufrufer**
> hat: Die einzige Produktionsreferenz auf die Klasse war `Vorbelegung`, gezogen aus
> `SpeicherAuslegungCtrl.cs:119`; alle übrigen Treffer waren Kommentare. Ebenso ohne
> Produktionsaufrufer: `StromspeicherSimCtrl.StarteOptimierung` und `.FuehreOptimierungAus`.
>
> **Teil 1 — Nachtnutzung.** Entfernt: `SpeicherEngine/Nachtnutzung.cs` (350 Zeilen),
> `NachtnutzungTests.cs` (757 Zeilen, 17 Methoden), `OptimiererStrategie.Nachtnutzung`, der
> Zweig in `BaueStrategie`, der Klapplisteneintrag des Simulationsergebnisses und drei
> Ressourcenschlüssel in beiden Sprachen. Die drei Vergleichslauf-Texte bleiben — sie tragen
> die Vergleichsspalte der Preissteuerung.
>
> Ein gespeicherter Stand wird **benannt** umgesetzt statt still: `SpeicherAltstand` trägt den
> entfallenen Persistenzwert und die Regel an einer Stelle (Muster
> `FlottenAltstand.Normalisiere`). Gesagt wird es zweimal — im Protokoll des Laufs
> (`SP_ALTSTAND_BERECHNUNGSART`) und in jedem Anzeigetext
> (`SP_BERECHNUNG_ANZEIGE_ALTSTAND`, `SpeicherAnzeigeCtrl.BerechnungsartText`). **Kein
> Schemaschritt, Testdatenbank nicht angefasst**; der gespeicherte Text wird auch nicht
> überschrieben. Alle 13 Zeilen der Testdatenbank stehen ohnehin auf Dauernutzung — kein
> Einfluss auf den Referenzlauf, keine neue Basis.
>
> **Eine Berichtigung, die im Auftrag nicht stand:** Der Excel-Kompatibilitätsmodus hing an
> der Nachtnutzung (`SimulationErgebnisHuelle.cs:721`, `SpeicherParameterBlock.razor:279`),
> obwohl er nach Fachkonzept 5.2 allein zur Dauernutzung gehört — nur sie hat eine
> Excel-Vorlage. Ohne diese Berichtigung wäre der Schalter dauerhaft gesperrt gewesen. Er
> hängt jetzt an der Dauernutzung.
>
> **Teil 2 — der tote Weg.** Entfernter Produktionscode: `SpeicherOptimierer.cs` (555),
> `OptimiererOptionen.cs` (419), `OptimiererErgebnis.cs` (444), `SpeicherOptimierungCtrl.cs`
> (1 619) = **3 037 Zeilen**, dazu `StarteOptimierung` und `FuehreOptimierungAus`; acht
> Testdateien mit **3 095 Zeilen**.
>
> Nach dem Löschen bestand die Klasse nur noch aus dem gespeicherten Stand und den
> Leistungspreis-Quellen — der Name stimmte nicht mehr. Sie heißt jetzt
> **`SpeicherAuslegungVorgabenCtrl`** (drei Methoden: `Vorbelegung`, `Leistungspreisquellen`,
> `TarifQuelle`). Die drei DTO-Typen behalten ihre Namen: Sie sind der **serialisierte** Stand
> von `Tab_SpeicherAuslegung` und stehen an über achtzig Stellen; ihr Umbenennen ist ein
> eigener Schritt. `SpeicherOptimierungEingaben` verliert die zwölf Felder des
> Einzelspeicher-Suchraums samt zwei Aufzählungen, die sonst als Zombies weitergelebt hätten;
> ein älterer JSON-Stand trägt sie noch, der Leser überliest sie
> (`UnmappedMemberHandling.Skip`) — **kein Schemaschritt**.
>
> Nachgezogen: `ParallelitaetWacheTests` (Belegdatei), `KulturweitergabeTests` (der Beleg am
> echten Rechenweg fällt — die `SpeicherEngine` führt jetzt überhaupt keine
> Rechenparallelität mehr; die synthetischen Belege bleiben), **73 verwaiste `OPT_*`-Ressourcen**
> in beiden Sprachen, der KI-Aktionskatalog, vier Konzeptpapiere, die Hilfeseite
> `Berechnung/Stromspeicher.wiki` und die offene Statuszeile „Nach #281". Berichtigt: Der
> Kopfkommentar von `SpeicherBetriebsbild` stellte die Abhängigkeit umgekehrt dar.
>
> **Die Bilderzahl sinkt nicht — sie bleibt 64.** `ChartRenderer.Optimierungsraster` und
> `.Schnittkurve` haben weiterhin Aufrufer: die Größen- und Stückzahlsicht der Flotte. Nur
> `RasterCsvText` ist mit dem Controller gefallen, und die ChartProben kennen es nicht.
>
> **Teil 3 — die Flottenlücke.** `FlottenOptimierer.BaueGeraeteeinheiten` ersetzte die Vorlage
> bisher vollständig durch das Gerät. Die eine Entscheidung (`FlottenGeraeteuebernahme` in
> `SpeicherEngine/FlottenModel.cs`): Was der Gerätesatz **führt**, kommt vom Gerät; was der
> Anwender in Schritt 1 gesetzt hat, bleibt seins. Kapazität und Lade-/Entladeleistung immer
> vom Gerät (Gegenstand der Suche); Wirkungsgrade, SoC-Band, Hilfsverbrauch und
> Investitionssätze vom Gerät, **wenn sein Satz sie führt**; Peak-Reserve, Grenzverschleiß,
> Betriebs- und Durchsatzkosten, Ersatz, Restwert und Alterungskurve **immer vom Anwender** —
> ein Gerätesatz führt sie nie.
>
> Was „geführt" heißt, entscheidet der **Kern** (`FlottenGeraeteuebernahme.Gefuehrt`, gesetzt
> in `SpeicherFlottenStudieCtrl.Geraetekandidaten`): brauchbar **und** nicht genau auf der
> neutralen Vorgabe — nach `LueckenFuellen` ist an der Zahl allein nicht mehr zu sehen, woher
> sie kommt. Die Engine rät nichts. Sichtbar an einer Stelle: neue Spalte „Herleitung" der
> Kandidatentabelle, eine Zeile je Kandidat („Gerät: … · eigene Eingabe: …"), leer, wo kein
> Gerät dahintersteht oder mehrere Achsen mehrere Geräte liefern. Nebenbefund erledigt:
> `SpeicherFlottenEditor.razor` zieht die vier neutralen Vorgaben jetzt aus
> `FlottenGeraetevorgaben`.
>
> **Abnahme:** Kern-Filter 0 Fehler, 0 Warnungen; Windows-Schale 0 Fehler, 5 Warnungen
> (Bestand); Tests grün in beiden Kulturen — die Laufzahlen sinken um **125 Fälle** mit dem
> entfernten Weg, davon 6 gerettet; ChartProben 64 Bilder, 0 Verstöße; SqlDialektPrüfer 0
> Fundstellen von 1 407; Referenzlauf 1030/1007/1017/1045/1046 gegen
> `2026-09-11_R7_Speicherflotte` **byte-gleich**; Gate a288 grün. Entfernt: rund 3 390 Zeilen
> Produktionscode und 3 852 Zeilen Testcode. Keine Einfrierregel berührt, die Basis bleibt.
> Merge `4372f51a`.
>
> **Offen:** die Umbenennung der drei DTO-Typen als eigener Schritt (siehe Statusdatei
> „Nach #288").

## #289 — Die Wache lernt sehen, der Sprung lernt schweigen (15.09.2026, Nachtrag aus dem Merge)

> **Fünf Kleinpunkte** aus den Messungen zu #286 und #287, vom Anwender freigegeben.
>
> **K1 — vier doppelte Titel, und die Lücke dahinter.** `TarifstrukturDialog`,
> `PhotovoltaikVerguetungDialog`, `WirtschaftlichkeitParameterDialog` und
> `KapitalwertVerlaufDialog` zeichneten ihren `epos-dialog-titel` unbedingt, obwohl die
> Überlagerung denselben Titel schon trägt. Sie führen jetzt `TitelAnzeigen` (Vorgabe `true`,
> Bestand unverändert); die vier Einbettungsstellen in `WirtschaftlichkeitSeite.razor` setzen
> `false`. Alle Einbettungsstellen geprüft: je Dialog genau zwei Wirte — die Überlagerung und
> die Windows-Hülle (eigenes Fenster, behält ihren Kopf).
>
> Der eigentliche Punkt ist die **Lücke**: Die Wache `UeberlagerungstitelTests` verglich
> Titel**ausdrücke** und griff deshalb nicht, weil die vier ihren Text aus `@_t.Titel`,
> `@_t.Titel(Sicht)` und `@TitelText` beziehen. Die neue Prüfung `StrukturFunde` kommt **ohne
> jeden Textvergleich** aus: Wer in einer betitelten `Ueberlagerung` steckt, darf keinen
> unbedingten Dialogtitel zeichnen — gleich, woher sein Text kommt. Belegt an einem
> nachgebauten Vorstand: Die geschärfte Wache meldet dort alle vier, nach der Behebung
> nichts; die alten Bauarten melden in beiden Fassungen nichts. Dazu acht bunit-Fälle je
> Dialog und eine Theory über alle fünf Bereiche.
>
> **K2 — der Befund traf nur halb zu.** Der Auftrag ging davon aus, `WPAR_SPRUNG_HINWEIS`
> („bitte vorher speichern") sei seit #282/#286 überholt. Die Nachprüfung, wie der
> Parameterdialog seine Sprünge **wirklich** abwickelt, ergab das Gegenteil: `BhkwKlick()`
> meldet den Sprungwunsch und schließt — `Speichern` läuft **nicht**. Anders als der
> BHKW-Dialog nimmt dieser Sprung nicht den OK-Weg; nicht gespeicherte Eingaben sind fort,
> weil die Hülle beim nächsten Öffnen frisch lädt. Deshalb **nicht** der Wortlaut des
> BHKW-Dialogs übernommen, sondern der Text sagt, was **dieser** Dialog tut: „Der Sprung
> schließt diesen Dialog, ohne die Eingaben zu speichern — bitte vorher speichern." Beide
> Sprachen, Designer wiederholbar gezogen.
>
> **K3 — Sprung schreibt nur bei Änderung** (Anwenderentscheid 15.09.2026, Empfehlung
> angenommen). Die zwei Sprungknöpfe des BHKW-Dialogs bleiben beim OK-Weg — ein Sprung ist
> kein Abbruch, und ohne Schreiben wären die Eingaben verloren. Aber sie schreiben nur bei
> Unterschied: `BhkwAnlagenstand.Gleicht` (elf Felder) und `BhkwVorgabenstand.Gleicht`
> (siebzehn Felder) vergleichen **Werte** gegen das hereingereichte Objekt — **kein
> Merkflag**, denn ein Flag kippt schon bei einer Bedienung ohne Wertänderung. Ohne
> Unterschied: Sprung ohne einen einzigen Schreibaufruf, `Gespeichert` bleibt `false`, der
> Wirt hat keinen Grund neu zu rechnen; der Hinweissatz steht nur, wenn wirklich geschrieben
> würde. Begründung im Kommentar: Wer nur nachschlägt, soll keinen Schreibzugriff auslösen —
> ein Schreiben ohne Änderung überschriebe in einer Mehrbenutzerlage fremde Änderungen mit
> dem eigenen geladenen Stand. Dass ein Fehlschlag den Sprung verhindert und die Maske offen
> hält (Muster Ae25), hielt der Bestand schon; jetzt mit Testfall belegt. Fünf bunit-Fälle.
>
> **K4 — der sechste Flatterer.** Ursache: Der Messpunkt wurde **sofort** nach dem Klick auf
> den Spaltenkopf genommen, das virtualisierte QuickGrid holt seine Zeilen aber über einen
> `ItemsProvider` nach — der Messpunkt lag in der Einschwingphase. Neuer Helfer `ZurRuhe`:
> zeichnet, bis zwei Läufe in Folge dieselbe Rechnung, Datenquelle **und** Menge zeigen; kommt
> die Liste nach zwanzig Läufen nicht zur Ruhe, fällt der Fall mit klarem Grund — die Prüfung
> bleibt scharf. Drei weitere Zeilenklicks derselben Klasse ohne Wartepunkt bekamen den
> vorhandenen Helfer. **Nachweis: 30 Klassenläufe, je 59 von 59 grün.**
>
> **K5 — BOM.** `EPOS.Kern/Controller/KostenVorlagenCtrl.cs`: vorher `75 73 69`, 44 530 Byte,
> 0 CR, 875 LF — nachher `ef bb bf`, 44 533 Byte, 0 CR, 875 LF, Rest byte-identisch. Genau das
> BOM, keine Zeile sonst.
>
> **Abnahme:** Kern-Filter 0 Fehler, 4 Warnungen (Bestand); Windows-Schale 0 Fehler, 5
> Warnungen (Bestand); **8 023 Tests grün**, 1 übersprungen, 0 rot (EPOS.UI 4 136 → 4 151);
> Referenzlauf 1030/1007/1017/1045/1046 byte-gleich; Gate a289 grün. Kein Rechenweg berührt.
> Merge `6245e330`.
>
> **Offen:** vier Entscheide — der Sprungknopf, den niemand auswertet; 37 weitere doppelte
> Titel; 332 Dateien ohne BOM; die Schreibbedingung des OK-Wegs (siehe Statusdatei
> „Nach #289").

## #290 — Erst die Erzeuger, dann die Dateien (15.09.2026, Nachtrag aus dem Merge)

> **Befund.** `.editorconfig` verlangt für `.cs` UTF-8 mit BOM. Von 1 311 versionierten
> `.cs` lagen 332 ohne — quer durch `EPOS.Kern` (125), `EPOS.Kern.Tests` (60),
> `EPOS.UI.Tests` (34), `EPOS.UI` (32), `SpeicherEngine` (32) und neun weitere Orte.
>
> **Warum die Reihenfolge der Punkt war.** Zwei dieser Dateien werden von Werkzeugen
> geschrieben, und beide Werkzeuge schrieben ohne BOM: `sql/tools/Erzeuge-Schema.ps1`
> (`UTF8Encoding($false)` für **alle** Ausgaben) und `Werkzeuge/KlimazonenPfade/erzeugen.py`
> (`encoding="utf-8"`). Hätte man nur die Dateien angefasst, wäre das BOM beim nächsten
> Werkzeuglauf wieder verschwunden und der Befund zurückgekommen. Die Erzeuger wurden
> gesucht, nicht nach Dateinamen geraten, und zuerst umgestellt: Das PowerShell-Skript
> bekommt einen Schalter, den **nur** der C#-Aufruf setzt — die drei `.sql` und zwei `.json`
> bleiben bewusst BOM-frei —, `erzeugen.py` schreibt `utf-8-sig`, und ein zweiter Lauf auf
> dasselbe Ziel liefert byte-gleiche 407 739 Byte (Muster #152). Ein dritter Kandidat wurde
> geprüft und **nicht** geändert: `designer_neu.py` schreibt `Resource.Designer.cs` schon mit
> BOM.
>
> **Gemessen statt angenommen.** Die Wurzel-`CLAUDE.md` warnt, dass ältere Dateien
> Windows-1252 ohne BOM sein können — ein BOM davor gesetzt ergäbe Byte-Salat. Alle 332
> wurden streng dekodiert: **keine einzige** ist 1252, alle sind gültiges UTF-8. Die
> Ausnahmeliste der Wache ist deshalb leer, und das ist ein Messergebnis, keine Auslassung.
>
> **Die Gegenprobe.** Sie trägt den ganzen Auftrag und wurde von der Orchestrierung
> unabhängig nachgerechnet: Zieht man aus jeder geänderten Datei die drei BOM-Bytes wieder
> ab, ist sie byte-identisch zu ihrem Stand in `be2eb4c7` — **332 von 332, 0 abweichend**,
> verglichen gegen die Git-Blobs. Der zeilenbasierte Diff zeigt für alle 332 genau `1 1`;
> das ist das Minimum, das eine reine BOM-Änderung zeigen kann, und mehr kommt nicht vor.
>
> **Die Wache.** Eine solche gab es nicht. Neu: `EPOS.Kern.Tests/QuelltextKodierungWacheTests`
> (Zuschnitt und Ton nach `RepositoryOrdnungWacheTests`, sechs Fälle, im Kern-Filter). Sie
> prüft zweierlei: BOM-Pflicht je versionierter `.cs` **und** UTF-8-Gültigkeit — die zweite
> Regel fängt den Rückweg ab, bei dem jemand eine Signatur vor einen 1252-Rumpf setzt; ihre
> Meldung sagt ausdrücklich, dass ein BOM die Sache in diesem Fall schlimmer macht. Die Liste
> stammt aus `git ls-files -z` (Dateisystem nur als Rückfall, damit sie nie still grün wird),
> dazu vier Gegenproben — ganzer Bestand, synthetischer Baum mit allen drei Fällen,
> Ausnahmeliste, Pfade mit Umlaut. Im Lauf live belegt: BOM entfernt → rot **mit Dateinamen**,
> wiederhergestellt → grün. Zeilenenden prüft sie bewusst nicht: `* text=auto` legt LF ab und
> checkt auf Windows CRLF aus — eine Prüfung darauf wäre auf einer der beiden Plattformen
> immer rot.
>
> **Ein Punkt, an dem der Agent angehalten hat.** `KlimazonenPfade.cs` wurde nicht neu
> erzeugt: Die eingecheckte Datei weicht vom `KOPF`-Muster des Erzeugers in genau einer
> Kommentarzeile ab (der double-Durchgang `b76c53d7` hat sie dort geändert, im Erzeuger
> nicht). Neuerzeugen hätte diese Zeile zurückgedreht — mehr als das BOM, was der Auftrag
> verbietet. Das BOM wurde von Hand gesetzt, byte-identisch zu dem, was der geänderte
> Erzeuger für diesen Inhalt schreiben würde, und die Drift im Commit festgehalten.
>
> **Abnahme:** Kern-Filter 0 Fehler, 4 Warnungen; Windows-Schale 0 Fehler, 5 Warnungen
> (Bestand); 8 029 Tests grün, 1 übersprungen; Formularkarte 122 grün (die Prüfmuster tragen
> jetzt BOM — `File.ReadAllText` verwirft es), Auslieferungsvorlage 19 grün; SqlDialektPrüfer
> 0 Fundstellen von 1 409; ResourceDesigner-Trockenlauf +0; Referenzlauf
> 1030/1007/1017/1045/1046 byte-gleich; Gate a290 grün. Kein Rechenweg berührt — geändert
> sind drei Bytes je Datei, zwei Erzeuger-Schreibpfade und eine neue Testklasse.
> Merge `5be42948`.
>
> **Offen:** der Windows-Lauf des Schema-Skripts, die Kommentardrift des Klimazonen-Erzeugers
> und 19 weitere Dateien derselben Regel (siehe Statusdatei „Nach #290").

## #291 — Der Strombezug-Einstieg bleibt: die Deckung fehlt (15.09.2026, Nachtrag aus dem Merge)

Der Auftrag lautete, den Knopf „Strombezug…" von der Wirtschaftlichkeitsseite zu
entfernen. Er hat nichts entfernt — die vorgeschaltete Anhalteregel hat gegriffen.

> „Wirtschaftlichkeit - button Strombezug und damit im zusammenhang stehende was
> nicht benötigt wird kann entfernt werden. Ist alles unter Verwaltung
> Energiekosten und Energiepreisstruktur enthalten."

Der letzte Satz war die Annahme, auf der alles ruhte, und er trifft nicht zu.

**Sieben Werte ohne zweiten Pflegeweg.** Die vier Zonen-Bezugspreise (Winter und
Sommer, je HT und NT) rechnen in `StromMatrix.Bezugskosten`; die drei Staffelgrößen
des Leistungspreises (Grenze, Preis darunter, Preis darüber) in
`StromMatrix.Leistungspreis` und in `SpeicherAuslegungVorgabenCtrl`. Beide Blöcke
baut der Dialog nur in den Sichten `Komplett` und `Strombezug`
(`MitZonenBezug => Sicht is TarifSicht.Komplett or TarifSicht.Strombezug`).

**Und `Komplett` hat keinen Wirt.** Die zweistellige Überladung
`TarifstrukturHuelle.Oeffnen(besitzer, idStamm)`, die diese Sicht öffnen würde, wird
nirgends gerufen; es gibt keinen Menüpunkt dorthin. Die einzigen Aufrufer der Hülle
sind die PV-Vergütung (Sicht `Photovoltaik`) und der BHKW-Dialog (Sichten `Bhkw` und
`Strombezug`). Damit ist der Knopf auf der Wirtschaftlichkeitsseite der einzige
Zugang zu diesen sieben Werten.

**Die Kostenverwaltung deckt sie nicht ab.** `Tab_ProjektTarif` hat genau einen
Schreibweg — `WirtschaftlichkeitCtrl.SpeichereTarif` mit einem einzigen Aufrufer.
Trägerkarte, Aufschläge und Leistungspreisreihe pflegen andere Größen auf anderen
Spalten. Der Kern benennt den Unterschied selbst: die Tarifstruktur trägt den Preis
der Wirtschaftlichkeitsrechnung, der Energieträger den des Kostenmoduls. Der
Tarifsatz **ersetzt** die Flat-Preise, er wiederholt sie nicht.

**Eine zweite, breitere Lücke** aus derselben Messung: Der Knopf erscheint nicht nur
bei aktivem Tarif, sondern auch, sobald die Gruppe eine Wärmepumpe führt. In einem
Wärmepumpenprojekt ohne BHKW und ohne PV ist er der einzige Zugang zum Tarifsatz
überhaupt — die Sichten `Bhkw` und `Photovoltaik` sind dort nicht erreichbar.

Ergebnis im Repositorium ist allein die Messung: Sichtentabelle mit ihren Wirten,
Feldkarte in vier Blöcken mit Datei und Zeilennummer je Zeile, die Begründung gegen
die Kostenverwaltung und drei Wege zur Entscheidung. Kein Quelltext, kein
Ressourcenschlüssel, keine Wiki-Quelle wurde angefasst; ein Referenzlauf war nicht
nötig, weil kein Rechenweg berührt ist.

Die Annahme der Orchestrierung, der gleichnamige Sprungknopf im BHKW-Dialog falle
mit, ist bewusst **nicht** umgesetzt. Sie bleibt sachlich richtig — zwei Pflegewege
auf dieselben Werte sind der Fehler, nicht die Bequemlichkeit —, setzt aber voraus,
dass überhaupt ein Pflegeweg bleibt.

## #292 — Zwei Einstiege fallen, einer davon war nie einer (15.09.2026, Nachtrag aus dem Merge)

Zwei Anwenderentscheide an derselben Seite, beide ohne Rechenwirkung.

**Teil A — „Tarifstruktur für Wärmepumpe entfällt."** Der Knopf „Strombezug…" erschien
bisher unter zwei Bedingungen: aktiver Tarifsatz **oder** eine Wärmepumpe in der Gruppe
(`WirtschaftlichkeitSeiteGaben.cs`, `stand.MitStrombezug = (flags != null &&
flags.Waermepumpe) || tarifAktiv`). Der zweite Zweig fällt. Damit hängt der Einstieg
allein an der Frage, ob das Projekt überhaupt mit einer Tarifstruktur rechnet.

Nicht gefallen ist der Knopf selbst, und das hat einen belegten Grund: Die Sicht
`Strombezug` ist der einzige Pflegeweg für vier Zonen-Bezugspreise und drei Staffelgrößen
des Leistungspreises, die alle in den Rechenweg gehen. Die Messung dazu steht unter
`Dokumentation/aktuell/`.

Mitgefallen ist `ErzeugerFlags.Waermepumpe` samt seiner Abfrage: Nach der Änderung hatte
das Feld genau null Leser.

**Teil B — der Knopf, der nichts tat.** Der Sprungknopf „BHKW-Wirtschaftlichkeit…" im
Wirtschaftlichkeitsparameter-Dialog meldete einen Sprungwunsch, den niemand auswertete.
Der einzige lebende Wirt ist die Überlagerung der Wirtschaftlichkeitsseite, und die
schreibt `Geschlossen="@((WirtParameterErgebnis e) => Fertig(e is not null))"` — der
Sprungwert fällt auf den Boden. Die Schleife, die früher wieder öffnete, hatte keinen
Aufrufer mehr.

Gefallen sind deshalb nicht nur der Knopf, sondern die ganze Kette dahinter: `BhkwKlick`,
die Beschriftung, der Hinweistext, der Aufzählungstyp `WirtParameterSprung` (nach dem
Wegfall des einen Wertes blieb nur `Keiner`), der Parameter `Sprung` des Ergebnisses —
`Schliessen()` ist jetzt parameterlos — und in der Hülle `Oeffnen` samt `EinmalZeigen`,
beide ohne Aufrufer, mitsamt vier Usings. Die BHKW-Gruppe im Dialog bleibt stehen, als
reiner Verweis darauf, wo die Angaben gepflegt werden.

**Eine Folge, die benannt gehört.** Nach Teil A gilt: In einer Gruppe ohne BHKW und ohne
Photovoltaik ist bei inaktivem Tarifsatz keine Sicht des Tarifdialogs mehr erreichbar.
Der Schalter „Aktiv" sitzt im Kopfblock, den jede Sicht baut — aber wer keine Sicht
öffnen kann, kommt an den Schalter nicht heran. Das ist die konsequente Folge des
Entscheids und bewusst so umgesetzt; ein Rückweg wäre ein Wirt für die Sicht `Komplett`,
die als Überladung existiert und seit jeher keinen Aufrufer hat.

Zur Wiki-Quelle: Die Fußleiste der Seite „Wirtschaftlichkeit" nannte einen Knopf
„Tarifstruktur…", den es seit Ä16 nicht mehr gibt. Er heißt dort jetzt „Strombezug…",
mit seiner Bedingung, und daneben steht „BHKW-Wirtschaftlichkeit…" als das, was es nach
dieser Welle ist: der einzige Weg in jenen Dialog.

## #293 — Der Rückfall setzt an der Zuordnung an, nicht am Wert (15.09.2026, Nachtrag aus dem Merge)

Der Anwenderentscheid lautete:

> „bereits zugewiesen CO-Zahlen nicht überschreiben"

Das verlangt, eine Lücke von einem Wert zu unterscheiden. Die Messung zeigt zuerst,
dass der heutige Datenstand genau das **nicht kann**.

**Eine gepflegte 0 ist heute nicht von „nie ausgefüllt" zu unterscheiden.** Im Code
zählt jede der vier Ebenen der Lesekette einen Wert erst ab *größer als 0* als gepflegt;
eine 0 fällt durch auf die nächste Ebene. Im Schema stehen `energy_project_settings.co2`,
`energy_carrier.co2` und `Tab_Brennstoff_Stamm.CO2` auf `DEFAULT 0` statt `NULL` — in der
Testdatenbank finden sich dort 2 / 3 / 2 Zeilen mit exakt 0 und **keine einzige** `NULL`,
dazu 41 Nullen in `emissionswert.wert`. Projekt 1017 ist das lebende Beispiel: zwei
Stromträger zugeordnet, beide mit Projektwert 0. Ob das „Ökostrom, bewusst 0" heißen soll
oder „nie gepflegt", steht nirgends.

**Die Folgerung war, nicht zu raten.** Der Rückfall setzt an der **Trägerzuordnung** an:
Er greift nur, wenn dem Projekt gar kein Stromträger zugeordnet ist — das ist eine
Tatsache der Struktur und braucht keine Deutung. Ist ein Träger zugeordnet, gilt sein
Faktor, auch wenn er 0 ist. Damit stellt sich die 0-Frage im umgesetzten Weg nicht.

`Emissionsquelle.Netzstrom` ist nun die eine Stelle für den Netzstromfaktor: zugeordneter
Träger über die Lesekette, sonst der Auslieferungsträger des Katalogs, sonst der
Vorgabewert. `KostenEmissionRechner` liest von dort statt aus einer eigenen Kette — die
zweite Wahrheit aus #267 ist weg, wo Kosten- und Emissionsseite dieselbe Lage
verschieden beantwortet haben. Wo der Rückfall greift, nennt eine Herleitungszeile den
geliehenen Träger.

**Kein einziger Emissionsskalar ändert sich** — gemessen je Projekt, nicht geschätzt:
1030 (10 Skalare, 0 Abweichungen), 1017 (10, 0), 1045 (5, 0); 1007 und 1046 führen keine.
1030 und 1017 haben einen Träger zugeordnet, der Rückfall greift dort nicht. Bei 1007,
1045 und 1046 greift er und trifft auf Träger 60 „Elektrische Energie" mit 435 g/kWh —
genau die Zahl, mit der diese Projekte vorher als anonymer Vorgabewert gerechnet haben.
Die Lücke wird gefüllt, die ausgewiesene Zahl bleibt gleich. Das Gate bestätigt es
unabhängig: Referenzlauf 5/5 byte-gleich.

Zwei Stellen blieben bewusst unberührt. Eine BHKW- oder Kesselzeile ohne Trägerbezug
lässt `CO2Gesamt` weiterhin auf `null` laufen („—"); ihr den **Strom**träger zu leihen
wäre fachlich falsch, dafür gibt es den Brennstoff-Rückfall. Und `Emissionsquelle.Fuer`
samt `StromTraeger` bleibt, wie sie war, damit Simulation, Strompreis und Aufschläge
unverändert rechnen und der Wächter aus #280 weiter gilt.

Eine Nebenwirkung gehört genannt: Dieselbe Stelle speist die Autarkie-Kachel. In einem
Projekt ohne zugeordneten Stromträger bekommt sie künftig den Katalogfaktor statt des
Vorgabewerts. In der Testdatenbank sind beide 435, deshalb kein Unterschied; in einer
Datenbank mit abweichend gepflegtem Auslieferungsträger verschiebt sie sich um dessen
Differenz. Das war der Preis dafür, aus zwei Fassungen derselben Frage eine zu machen.

## #294 — Die Absage nennt die Maske, nicht ihre Typnamen (15./16.09.2026, Nachtrag aus dem Merge)

Der Anwenderbefund vom 15.09.2026 galt einer Antwort, die formal richtig und praktisch
unbrauchbar war: Wer den Assistenten bat, Felder zu setzen, während die zugehörige Maske
nicht offen stand, bekam eine Liste von **Typnamen** zurück. Sie sagte weder, welche Maske
gemeint war, noch was man tun soll.

**Die Absage entsteht in der Vorbedingung**, nicht im Ausführer — und dort liegt auch die
Lösung. `KiAktionenDialog` leitet die gemeinte Maske jetzt aus den **Feldnamen** der Aktion
ab (`BrueckenGrund`, `GemeinteMaske`, `Anzeigenamen`) und nennt zwei Dinge: den
Anzeigenamen der Maske und `dialog_oeffnen` als den Weg dorthin
(`KI_DLG_MASKE_NICHT_OFFEN`, de/en). Das gilt für beide Formularaktionen,
`formular_ausfuellen` und `feld_setzen`.

**Bei Mehrdeutigkeit schweigt sie.** Passt ein Feldname auf mehr als eine Maske, wird nicht
geraten: Die Absage fällt auf die bisherige Liste zurück, jetzt aber mit Anzeigenamen statt
Typnamen. Eine falsch benannte Maske wäre schlechter als gar keine — sie schickte den
Anwender in die falsche Richtung und der Assistent bekäme sie obendrein als Tatsache
zurückgespielt.

**Der zweite Weg führt am Anwender vorbei.** Der Systemprompt weist das Modell an, nach
einer gescheiterten Formularaktion selbst `dialog_oeffnen` zu rufen (`KiChatService`); im
Regelfall sieht der Anwender die Absage dann gar nicht, sondern die geöffnete Maske mit den
gesetzten Werten. Dazu eine Kleinigkeit aus derselben Stelle: `dialog_lesen` zählt jetzt,
was **wirklich** gelesen wurde, nicht, was angefordert war.

**Geprüft wird an der Vorbedingung** (`EPOS.Kern.Tests/KiMaskenwegTests`, fünf Fälle: die
Maske zu den Feldern, dasselbe für `feld_setzen`, das mehrdeutige Feld ohne Rateversuch,
das unbekannte Feld auf der Liste mit Anzeigenamen, die Heizkesselmaske mit Vor- und
Rücklauf). Am Ausführer wäre der Weg gar nicht messbar: Er weist eine Stufe-2-Aktion ohne
Freigabe schon vorher ab.

Die Ressourcen stehen in beiden Sprachen. Die erzeugte `Resource.Designer.cs` trägt dadurch
bereits `SCHLIESSKREUZ_TOOLTIP` aus der folgenden Welle — sie entsteht als Ganzes aus der
neutralen `.resx` und lässt sich nicht nach Commits schneiden.

**Logbuch-Vorschlag** (Version 1.2.0.2):

> kein eigener Eintrag — zu klein für das Logbuch (Regel 13.4).

## #295 — Das Kreuz steht beim Titel (15./16.09.2026, Nachtrag aus dem Merge)

Der Anwenderentscheid vom 15.09.2026 lautete:

> „alle Dialoge sollten mit einem Kreuz zu schließen sein … nicht erst ganz unten mit den
> Buttons … einheitlich"

und, auf den ersten Stand hin nachgesetzt:

> „Doppeltes Kreuz dürfen nicht sein!"

Beides zusammen ist eine Regel, nicht zwei: Genau **ein** Kreuz je geschlossenem Bereich,
und zwar oben rechts.

**Der Baustein.** `Schliesskreuz` (`button.epos-dialog-zu`) steht als letztes Kind jedes
`epos-dialog-kopf`. Es wirkt wie Esc und wie „Abbrechen" — `Geschlossen` bekommt die
Esc-Aktion des Dialogs, es entsteht kein zweiter Verwerfen-Weg, der eigene Regeln bekommen
könnte. Enter auf dem fokussierten Kreuz erreicht die Dialogwurzel nicht
(`stopPropagation`), sonst löste die Taste zugleich den OK-Weg aus. Die Stilregel teilt es
sich mit dem Kreuz der Überlagerung, der Tooltip ist `SCHLIESSKREUZ_TOOLTIP` (de/en).

**Das Kreuz hängt an derselben Bedingung wie der Titel.** Wo ein Kopf nur unter Bedingung
gezeichnet wird, wird auch das Kreuz nur unter dieser Bedingung gezeichnet — daher die
Kurzform „das Kreuz steht beim Titel". Sie ist der Grund, warum die Doppelkreuz-Frage
überhaupt beantwortbar ist: Betitelte Überlagerungen verlieren `Schliessbar="false"` und
tragen ihr Kreuz selbst (es ruft dasselbe `Geschlossen`, das Esc dort schon rief); die
darin eingebetteten Dialoge verbergen dafür Titel **und** Kreuz — `TitelText=""` bzw.
`TitelAnzeigen`, gesetzt am **Einbettungs**-Starttag und hinter einem etwaigen
`@attributes`, damit die Regel nicht daran hängt, was eine Hülle in ihren Parametersatz
legt.

**Wo bewusst kein Kreuz steht**, hat es jedes Mal denselben Grund: Zugehen würde schaden.
Das sind die Rückfragen, die laufenden Vorgänge (Import, Klimadaten, Projektkopie und
-transfer), die Schritte des Assistenten sowie `KiEinstellungen` und `KiHinweisDialog`,
deren Wirte eins tragen. Die Lizenzverwaltung schließt über das Fensterkreuz bzw. über ihre
Überlagerung.

**Eine Strukturwache statt einer Zählung.** `EPOS.UI.Tests/SchliesskreuzWacheTests` hält
drei Regeln: (1) Jede Dialogdatei, die einen Kopf zeichnet, enthält ein `<Schliesskreuz`.
(2) Kein `<Ueberlagerung>`-Starttag trägt zugleich einen nicht-leeren Titel und
`Schliessbar="false"` — dann hätte der Bereich gar kein Kreuz. (3) Die Gegenrichtung: keine
betitelte, schließbare Überlagerung mit **zwei** Kreuzen. Neun Fälle, jede Regel mit ihrer
Gegenprobe; die Ausnahmeliste der ersten beiden Regeln nennt zwei begründete Stellen (die
Warte-Überlagerung des Erdreich-Laufs, die selbst zugeht, und `WertAbfrage`, die eine
Antwort braucht), die zum Doppelkreuz ist **leer** — der Anwender hat es ausdrücklich
zurückgegeben. Die Wache wird erst mit dem Kreuz-Anteil des Erzeuger-Commits überall grün;
solange die vier Erzeuger-Dialoge noch fehlten, war sie rot, und das ist die Gegenprobe im
Lauf.

**Aus dem Merge.** Drei Stellen trafen auf die Wellen von `origin`, alle inhaltlich
zusammengeführt: Der einmalige Titel der vier Wirtschaftlichkeitsdialoge (#289) nutzt
denselben Parameter `TitelAnzeigen` wie diese Welle — die Seite setzt ihn an allen fünf
Einbettungen auf `false`. Im `BhkwWirtschaftlichkeitDialog` (#286, OK und Abbrechen) ruft
das Kreuz `Verwerfen`, genau die Methode hinter Esc, ohne Schreibzugriff. Und im
Parameterdialog, dessen Sprungwert mit #292 gefallen ist, geht das Kreuz über eine
herausgelöste `Abbrechen()`, die auch Esc ruft. Die Bauart-C-Restliste der
`UeberlagerungstitelTests` ist danach leer: kein doppelter Überlagerungstitel mehr im Baum.

**Logbuch-Vorschlag** (Version 1.2.0.2):

> Seit 16.09.2026 lässt sich jeder Dialog über ein Kreuz oben rechts schließen.

## #296 — Alle sechs Erzeuger im Schema des Heizkessels (15./16.09.2026, Nachtrag aus dem Merge)

Der Anwenderentscheid vom 15.09.2026 fiel an der Verwaltung Heizkessel und galt
ausdrücklich für **alle sechs** Erzeuger: Der Bearbeiten-Dialog trägt keine Kosten und
Emissionen mehr; im Modulbereich steht ein Aufklapper „Alle Daten anzeigen" mit
bearbeitbaren Feldern, darüber die Kostenknöpfe, darunter „Bearbeiten…"; Neu und Löschen
bleiben bei der Liste; die Administration entfällt.

**Ein Baustein statt sechs Feldraster.** Das Feldraster des Katalogbrowsers wird zum
Baustein `Katalogfelder` — je Feld in seiner Art, bearbeitbar dort, wo der Katalog einen
Speicherweg hat. Der Browser benutzt ihn selbst; damit gibt es die Darstellung einmal, nicht
siebenmal.

**Die Profile führen jetzt jede fachliche Spalte.** `KatalogBrowserProfil`: Heizkessel 21
Felder (20 editierbar), BHKW 25 (23), Solarkollektoren 14 (13), Pufferspeicher 6 (5); der
Bezeichner bleibt Schlüssel. Geschrieben wird read-modify-write und **nur** auf den
editierbaren Spalten: `AnzeigefelderHeizkessel` und `AnzeigefelderBhkw` sind erweitert (neue
Felder optional, `null` = unverändert), `AnzeigefelderPufferspeicher` und
`AnzeigefelderSolarkollektor` mit `AnzeigefelderSchreiben` neu; `HatSpeicherweg` gilt jetzt
für alle vier. Fehleingaben werden benannt abgelehnt, nicht still verschluckt
(`KatalogFeldPruefung`: negativer Wert, Wert außerhalb des Bereichs, unbekannter
Listenwert).

**Vier Entscheide, die später sonst als Versehen gelesen würden.**

*Emissionen bleiben editierbar*, obwohl sie aus dem Bearbeiten-Dialog gefallen sind. Sie
sind gespeicherte **Herstellerangaben** des Katalogsatzes; der Rechenweg liest den
Emissionskatalog, nicht diese Spalten. Wer sie schreibgeschützt hätte, hätte ein Datenblatt
unpflegbar gemacht, ohne eine Zahl zu schützen.

*Energieträger und Wartungseinheit* stehen als Textfelder mit Nachschlag gegen die
erlaubten Werte da, leer heißt unverändert. Das Feldraster kennt keine Klappliste, und eine
eigene Feldart wäre mehr gewesen, als der Entscheid verlangt.

*Die BHKW-Investitionsrechnung* (fünf Posten ↔ € je kW elektrisch) ist aus dem Katalogeditor
gefallen. `BHKWKosten` bleibt im Kern: `Investition_kwel` wird beim Speichern aus den
Posten gerechnet und im Aufklapper nur **angezeigt** — das ist die zweite der beiden nicht
editierbaren BHKW-Spalten. Zugleich ist das BHKW die **einzige** Familie mit einer
Schreibschutz-Rückfrage; genau dafür trägt der Speichern-Delegat ein drittes Argument.

*Der Entscheid E-11 („leere Kollektorfläche") ist abgelöst.* Der Vorläufer ließ das Feld
leer, und das war richtig, **solange der Block nur anzeigte** (die Modulfläche wurde gelesen
und sofort von der Aperturfläche überschrieben). Mit einem Speicherweg kehrt sich das um:
Ein leeres Feld hätte die gespeicherte Modulfläche beim ersten Speichern auf 0 gesetzt.
Deshalb zeigt der Block sie jetzt und schreibt sie zurück.

**Die sechs im Einzelnen.** Heizkessel und BHKW bekamen den Umbau zuerst (Aufklapper im
Modulbereich, Katalogeditor ohne Kosten/BEHG/Emissionen). Photovoltaik und Stromspeicher
gehen über die `ModulFeldwertBruecke`, die `ModulFeldwert` auf `BrowserFeldwert` abbildet
und den Satz als Ganzes über `ModulKatalogWege` speichert; die PV-Koeffizienten `alpha_SC`
und `beta_OC` bleiben dabei Lesewerte. Beim Stromspeicher wandert die Überlagerung aus der
Kopfzeile ans Dialogende. Pufferspeicher und Solarkollektoren speichern über die
Admin-Hüllen; ihre Katalogeditoren tragen keine Investitionskosten mehr (der OK-Weg reicht
die Spalte unverändert durch), und „Kollektor in DB ändern…" heißt jetzt „Bearbeiten…" und
steht im Modulbereich. Die Kostenknöpfe aller sechs Wirte stehen unter Aufsicht
(`KostenknopfWegeTests`); **Puffer und Solar haben keinen Energiekosten-Knopf** — dort gibt
es keinen Energieträger, an dem er hinge.

**Zwei Nachzüge zum Schluss.** `ParameterVerwendung` nennt die im Aufklapper gepflegten
Spalten mit ihrer neuen Fundstelle statt mit entfallenen Dialogzeilen. Und
`KiDialoge.Heizkessel` führt nur noch die **sechs** sichtbaren Felder des Katalogeditors
statt fünfzehn: Die neun entfallenen Kosten- und Emissionsfelder sind im Aufklapper des
Projektdialogs pflegbar, dort ohne KI-Weg. Damit das nicht wieder auseinanderläuft, prüft
eine neue Wache in `KiDialogkatalogTests`, dass jeder Feldpfad einer Katalogmaske im Markup
der zugeordneten Razor-Datei steht. Aus dem `HeizkesselKatalogDialog` sind zugleich 23 nicht
mehr gezeichnete Parameter samt `BeiCo2Vorgabe` und `GewaehlterBrennstoff` gefallen; ein
Fact hält fest, dass der OK-Weg die nicht mehr gezeigten Spalten unverändert durchreicht.

**Verwaist, aber nicht entfernt:** `PVD_AUFKLAPP_PARAMETER`, `PSPK_GRP_KOSTEN`,
`PSPK_FELD_INVEST`, die 18 `Hk*`-Texte in `KiDialogTexte` samt ihren `KI_DLG_HK_*`-Schlüsseln
und `ErzeugerDetail.Parameterzeilen`/`Modulparameter`. Das Aufräumen zieht an Ressourcen,
erzeugtem Designer und Katalogtests zugleich und ist deshalb ein eigener Schritt; in dieser
Welle wäre es ein Merge-Risiko gewesen.

**Logbuch-Vorschlag** (Version 1.2.0.2):

> Seit 16.09.2026 sind in den Dialogen der Energieerzeuger alle Gerätedaten direkt bearbeitbar.

## #297 — Bearbeiten nach oben, alle Daten offen, die Konfiguration als eigener Dialog (16.09.2026)

Der Anwenderentscheid vom 16.09.2026 kam in drei Sätzen. Keiner davon galt einer Maske;
jeder galt allen.

> „Der Bearbeiten-Button soll weiter oben (unter der blauen Zeile Modul) stehen, so dass er
> besser sichtbar ist. Unter Bearbeiten sollen alle Parameter angezeigt werden und
> bearbeitbar sein. Das gleiche Schema für alle Komponenten."

> „Brennstoff Typ und Brennstoff Variante sollen untereinander stehen — bei allen
> brennstoffgekoppelten Komponenten."

Und zum Wärmepumpendialog: Die Bezeichnung „Wärmeerzeuger Spitzenlast" bezieht sich nur auf
die elektrische Nachheizung; der Titel soll „Konfiguration" heißen, der Bereich aus dem
Dialog herausgelöst und separat aufrufbar sein — erstens aus diesem Dialog über einen Knopf
„Konfiguration", zweitens aus Simulation > Konfiguration, dort anstelle des Balkens
„Parameter für die Simulation" und für alle Komponenten, mit den bestehenden Parametern
(Heizstab, Betriebsbereitschaft) darin. Dazu: „Der Dialog ‚Parameter Bearbeiten' ist nicht
mehr nötig, alle Parameter samt Kennlinie direkt im Dialog."

**Eine Knopfzeile, nicht zwei.** Der Entscheid sagt, wo „Bearbeiten…" stehen soll, aber
nicht, was aus den Kostenknöpfen wird, die seit #296 darüber standen. Zwei gestapelte Zeilen
hätten den Knopf wieder nach unten geschoben — genau das, was abgestellt werden sollte.
Deshalb ist es **eine** Zeile: Kostenknöpfe links, Füller, „Bearbeiten…" rechts, als erstes
Kind des Modulkörpers in allen sechs Erzeuger-Dialogen. Die Wahl ist die des Orchestrators,
nicht die des Anwenders — die Rückfrage dazu hat er abgebrochen, und sie bleibt damit
widerrufbar. Dass die Kostenleiste in einer Knopfzeile ihren Trennstrich verliert, ist die
Stilfolge davon.

**„Alle Daten" startet offen und bleibt zuklappbar.** „Unter Bearbeiten sollen alle Parameter
angezeigt werden" heißt: sichtbar ohne einen zweiten Klick. Der Aufklapper ist deshalb nicht
gefallen, sondern umgestellt — er geht offen auf und holt seine Felder mit der Satzwahl. Wer
den Dialog schmal haben will, klappt weiterhin zu; wer ihn zum Arbeiten öffnet, sieht alles.
Ein entfernter Aufklapper hätte die Wahl genommen, ohne etwas zu gewinnen.

**Das Brennstoffpaar ohne neues CSS.** „Untereinander" ist im Feldraster eine Frage der
Spaltenbreite, nicht der Reihenfolge: Ein Paarblock über die volle Breite setzt seine zwei
Felder untereinander. Dafür steht `epos-feld--breit` schon da — eine eigene Regel für diesen
einen Fall wäre eine zweite Antwort auf eine beantwortete Frage gewesen. Beim Heizkessel
standen beide Felder bereits im Raster. **Das BHKW nicht:** Ihm fehlte das Katalogfeld
„Brennstoff Typ" ganz, es zeigte nur die Projektwahl. Die Hülle liefert den
Katalog-Brennstoff jetzt über `BHKWStammCtrl.KatalogsatzAnzeige` nach, und die Beschriftung
folgt dem Heizkessel (`HZK_LBL_BRENNSTOFFTYP`, `HZK_LBL_TRAEGER`) statt dem eigenen
`BHKWV_LBL_TRAEGER` — zwei Namen für dasselbe Feld wären dem Entscheid „bei allen
brennstoffgekoppelten Komponenten" zuwidergelaufen.

**Die Wärmepumpe: die Konfiguration wird ein Bereich mit eigenem Leben.**
`WaermepumpeKonfiguration` trägt Nachheizung, Sperrzeit, bivalenten Betrieb, Betriebsart,
Bivalenztemperatur, Energieträger und die Erklärkästen — ein Baustein mit eigenem Textbündel,
damit ihn beide Wirte gleich beschriften. Der Anlagendialog zeigt ihn in einer Überlagerung
mit OK/Abbrechen auf einer **Arbeitskopie**: Abbrechen lässt den Stand, wie er war. Fehlt
beim OK des Anlagendialogs ein Feld der Konfiguration, geht die Überlagerung auf, statt die
Eingabe wortlos zu verweigern.

**Die Stammfelder bearbeiten den KATALOGSATZ.** `WaermepumpeStammFelder` ist dasselbe
Feldraster im Stammdialog und im Anlagendialog; im Anlagendialog ist es bearbeitbar und hat
seinen eigenen Speichern-Knopf. Geschrieben wird damit `Tab_WP_STAMM`, zugeordnet über den
**Bezeichner**: Ohne Katalogsatz sind die Felder gesperrt, ein Auslieferungssatz (`ReadOnly`)
lehnt das Speichern benannt ab. Das ist genau die Reichweite, die das entfallene „Parameter
Bearbeiten…" hatte — es führte über denselben Weg in denselben Satz, nur über einen zweiten
Dialog. Neu ist nicht, **was** geschrieben wird, sondern dass es ohne Umweg geschieht. **Der
Anwender muss das bestätigen:** Eine Kennzahl, die im Projektdialog geändert wird, gilt
danach für jedes Projekt, das denselben Katalogsatz benutzt.

**Der Heizstab steht doppelt — und nur einer der beiden rechnet.** Der Befund fiel beim
Zusammenlegen auf und gehört festgehalten, weil die Maske ihn sonst verdeckt. Der
Simulationslauf liest den Heizstab **projektweit** aus `Tab_Einstellungen.WP_Heizstab`
(`SimulationControl` → `SimulationWaermepumpe.Mit_Heizstab`) — nur wenn dieser Schalter
steht, rechnet der Lauf überhaupt eine Heizstabphase. Der Anlagenschalter
`Tab_Energieanlagen.Heizstab`, in der Maske „Elektrische Nachheizung aktivieren (falls
vorhanden)", entscheidet dagegen allein über die **Energieträgerwahl**
(`EnergietraegerZulaessigkeit`, `WizardCtrl.BrauchtStromTraeger`): Er hebt die Anlage in die
elektrische Welt. Beides steht jetzt im selben Dialog, und beides ist dort benannt
(`SIMKONF_HRL_HEIZSTAB`, `SIMKONF_HRL_HEIZSTAB_ANLAGE`), damit niemand am falschen Schalter
dreht. **Empfehlung für einen eigenen Kern-Schritt:** den Anlagenschalter im Rechenweg
auswerten und den Projektschalter abschaffen. Das greift in den Rechenweg ein, verlangt eine
Migration der bestehenden Projekte und eine Referenzprüfung — es in diese Welle zu ziehen
hätte aus einer Maskenänderung einen Eingriff in die Physik gemacht. Anwenderentscheid offen.

**Warum der teure Schreibweg.** Speichert man die Wärmepumpen-Konfiguration von der
Simulationsseite, geht der Weg über `Del_Projekt_Waermeerzeuger` + `Add_WP_Waermeerzeuger`,
also über Löschen und Neuschreiben aller Wärmepumpen-Anlagen des Projekts — derselbe Weg, den
der Reiter „Wärmepumpe" nach seinem Übernehmen schon immer ging. Der kürzere
`WErzeugerCtrl.Update` führt in seinen sechzehn Spalten **weder `Heizstab` noch
`ID_Carrier`**; beide stehen in diesem Feldsatz. Ein Schreibweg, der zwei gerade bearbeitete
Felder still fallen ließe, wäre schlimmer als der teure. Senken, Stränge und Varianten
überleben, weil sie über das Paar (`ID_Type`, Bezeichner) zugeordnet werden — **die Ids der
Anlagen wechseln dabei jedoch**, und die Hervorhebung der gerade bearbeiteten Karte geht
danach verloren. Kosmetisch, sichtbar, offen.

**Nur drei Karten tragen den Knopf.** Auf Simulation > Konfiguration steht er an
Wärmepumpen-, Heizkessel- und BHKW-Karten, weil nur diese drei Laufparameter haben: Heizstab,
Betriebsbereitschaft, BHKW-Betriebsart samt unterer Leistungsgrenze. Solarthermie,
Photovoltaik, Strom- und Pufferspeicher haben keine — ein Knopf, der einen leeren Dialog
öffnet, wäre schlechter als kein Knopf. Die Zuordnung liegt an **einer** Stelle
(`Komponentenart`), damit eine vierte Art sie nicht an drei Orten suchen muss. Dahinter steht
der `KomponentenKonfigurationDialog`: OK schreibt, Abbrechen nicht. Die Naht dorthin ist
`SimulationParameterDienste` (Laden und Speichern der Anlagenkonfiguration, Trägerkatalog);
die Windows-Schale belegt sie mit dem Bestandsweg der Wärmepumpenhülle.

**Die Wache zieht mit.** `KiDialogkatalogTests` prüft seit #296, dass jeder Feldpfad einer
Katalogmaske im Markup der zugeordneten Razor-Datei steht. `Form_WP` bindet jetzt über zwei
Dateien — `WaermepumpeStammDialog` und `WaermepumpeStammFelder` —, weil das Feldraster ein
eigener Baustein geworden ist; die Wache nimmt beide. Gefallen sind `WPA_LBL_SPITZENLAST`,
`WPA_GRP_SPITZENLAST`, `WPA_BTN_PARAMETER`, `WPA_LBL_BESCHREIBUNG`, `BHKWV_LBL_TRAEGER` und
`SIMKONF_GRP_LAUFPARAMETER`.

**Prüfung.** Kern-Filter 0 Fehler, Windows-Schale 0 Fehler, `EPOS.UI.Tests` 4 478/4 478; das
Gate grün bis auf die zwei fremden Fälle (Bildvergleich, nicht committete Papiere). Je
Erzeuger-Dialog gemessen: Reihenfolge in der Knopfzeile, offener Startzustand, Zuklappen; bei
Heizkessel und BHKW die Nachbarschaft des Brennstoffpaars. Neu sind
`WaermepumpeKonfigurationTests`, `WaermepumpeStammFelderTests` und
`KomponentenKonfigurationDialogTests`; nachgezogen `WaermepumpeAnlageDialogTests`,
`WaermepumpenDialogTests`, `SimulationKonfigSeiteTests` und `KiDialogkatalogTests`.

**Logbuch-Vorschlag Erzeuger** (Version 1.2.0.2):

> kein eigener Eintrag — im Eintrag zu #296 enthalten.

**Logbuch-Vorschlag Wärmepumpe und Simulation** (Version 1.2.0.2):

> Seit 16.09.2026 haben Wärmepumpe, Heizkessel und BHKW einen eigenen Dialog „Konfiguration",
> auch aus der Simulation erreichbar.

## #298 — Die Stammfelder ändern die Anlage, die Übernahme den Katalog (16.09.2026)

Der offene Punkt „Nach #297" hatte genau diese Entscheidung verlangt: Die Stammfelder im
Anlagendialog schrieben den Katalogsatz, und wer dort eine Kennzahl änderte, änderte sie für
jedes Projekt, das denselben Satz benutzt. Die Antwort kam am 16.09.2026.

> „Stammfelder im Wärmepumpendialog ändern den Katalogsatz: speichern nicht in Stamm sondern
> nur in Projektdaten. Es soll die Option zur Übernahme in Stamm gegeben sein (überschreiben
> — mit Warnung!)"

Der Satz enthält beides: das Verbot und den Ersatz. Beides musste gebaut werden — der Ersatz
mehr als das Verbot, denn den Weg, den der Entscheid verlangt, gab es nicht.

**Es gab keinen Schreibweg in die Projektkopie.** `Tab_WP` trägt je Projekt eine Kopie des
Katalogsatzes. Angelegt wird sie einmal, beim Zuweisen der Wärmepumpe (`CopyFromStamm`);
danach schrieb sie **niemand** mehr. Der Entscheid „speichern nur in Projektdaten" ließ sich
also nicht dadurch erfüllen, dass man einen Aufruf umhängt — die Zieltür fehlte. Was der
Controller an Schreibweg führte, war ein `Update` **ohne Aufrufer**, und es war gefährlich:
`WHERE Bezeichner`, **ohne Projektfilter**. Hätte es jemand in Betrieb genommen, hätte das
Speichern einer Anlage die gleichnamigen Geräte **aller** Projekte mitgeschrieben — genau der
Fehler, den der Entscheid abschafft, nur eine Ebene tiefer. Es ist entfallen statt repariert:
Ein Weg, der Geräte über ihren Namen sucht, ist nicht der Weg, der hier gebraucht wird.

**Der neue Weg geht über die Ids.** `WPCtrl.ProjektgeraetSchreiben(idWp, idProjekt, …)` ist
read-modify-write über **ID und ID_Projekt**, nie über den Bezeichner: acht Felder (Firma,
Beschreibung, Typ, Regelung, Aufstellung, Baujahr, Nennleistung, Heizstableistung), `null` =
unverändert, damit ein Aufrufer schreiben kann, was er kennt, ohne den Rest zu berühren.
Abgelehnt wird benannt: kein Projektsatz, negative Werte, Baujahr außerhalb 1900 … Jahr+1.
`WPCtrl.ProjektgeraetVorhanden` sagt vorab, ob es die Kopie überhaupt gibt — davon hängen die
weichen Sperren im Dialog ab.

**Die Leistung des Heizstabs hatte nirgends eine Heimat.** Sie stand in der Maske und wurde
in **keine** Tabelle geschrieben: `Tab_Energieanlagen` hat keine Spalte dafür, und die
Projektkopie wurde nicht beschrieben. Der Wert ging beim Schließen verloren. Er gehört in
`Tab_WP.Heizung` und geht seither dorthin. Das ist kein Nebenbefund der Welle, sondern der
Grund, warum sie an dieser Stelle etwas **gewinnt** und nicht nur verschiebt.

**Gespeichert wird mit OK, nicht mit einem eigenen Knopf.** Der Speichern-Knopf, den die
Stammfelder in #297 bekommen hatten, entfällt: Er gehörte zum Katalogweg. Jetzt binden die
Felder an die Anlagendaten und gehen mit dem OK des Dialogs hinaus — die Hausregel für jeden
Dialog dieses Hauses. Der **Bezeichner** ist im Anlagendialog nur lesend
(`BezeichnerAenderbar` am Baustein, im Stammdialog weiterhin änderbar). Das ist keine
Bequemlichkeit: Die Kopplung an den Katalogsatz läuft allein über den Namen, und die Senken
werden über das Paar (Typ, Bezeichner) zugeordnet. Ein im Anlagendialog geänderter Name hätte
beide Zuordnungen zugleich gelöst, ohne dass es jemand sieht.

**Der Kennlinieneditor bearbeitet seither die Projektkennlinien.** `KenndatenCtrl.LiesProjekt`
und `AbgleichenProjekt` liefern sie in derselben Form wie der Katalogweg seine — gemeinsamer
Rumpf, verhaltensgleiches Abgleichen —, damit der Editor nicht zwei Welten kennen muss. Vor
dem ersten Speichern der Anlage gibt es die Kopie noch nicht; dann ist der Editor **weich
gesperrt mit Grund**, ebenso die Übernahme in den Stamm. Weich heißt: Die Reiter stehen da,
sie sagen, warum sie noch nicht können, und sie können, sobald die Anlage einmal gespeichert
ist. Eine ausgeblendete Schaltfläche hätte dieselbe Wirkung und keine Erklärung.

**Die Übernahme in den Stamm ist die gewarnte Gegenrichtung.** „In Stamm übernehmen…" öffnet
eine Überlagerung, die zuerst sagt, was geschehen wird (`WPStammCtrl.UebernahmeVorschau`),
und drei Fälle unterscheidet: Steht ein freier Katalogsatz dieses Namens da, wird er
**überschrieben** — genannt mit der Zahl der anderen Projekte, die eine eigene Kopie besitzen
und sich dabei **nicht** ändern, denn deren Kopien sind ja gerade der Sinn der ganzen Welle.
Steht keiner da, wird ein Satz **neu angelegt**. Ist der Satz ein Auslieferungssatz
(`ReadOnly`), wird nichts übernommen und die Absage benannt. Dazu der Schalter „Kennlinien
mitübernehmen": Er ersetzt Wärme- **und** Kühlkennlinien des Katalogsatzes, beide oder keine
— eine halb ersetzte Kennlinienmenge wäre schlimmer als eine unveränderte.
`WPStammCtrl.UebernehmenAusProjekt` schreibt in **einer** Transaktion; danach wird die
Stammliste neu geholt, damit der Dialog nicht auf einem Stand von vorhin weiterarbeitet.

**Vier Speicherwege, ein Nachzug.** Die Anlage wird an vier Stellen gespeichert: in der
Wärmepumpen-Verwaltung, auf dem Simulationsreiter Wärmepumpe, in Simulation > Konfiguration
und im Assistenten (neu wie bearbeiten). Alle vier ziehen die Projektkopie über
`WaermepumpeGeraeteCtrl.ProjektgeraetNachziehen` nach — die Stammfelder dürfen nicht davon
abhängen, über welchen Weg jemand auf OK geklickt hat. Der teure Schreibweg aus #297
(`Del_Projekt_Waermeerzeuger` + `Add_WP_Waermeerzeuger`) schadet dabei **nicht**: Gelöscht
wird nur `Tab_Energieanlagen`; Projektkopie und Projektkennlinien bleiben stehen, und
`CopyFromStamm` gibt beim Neuanlegen die vorhandene Kopie zurück, statt sie aus dem Katalog zu
überschreiben. Der Nachzug schreibt anschließend die Gerätezeile nicht, sondern nur die Kopie.

**Was offen bleibt, hat eine gemeinsame Wurzel.** Projektkopie und Katalogsatz hängen allein
am **Bezeichner**. Wird der Katalogsatz umbenannt, findet die Übernahme keinen Satz dieses
Namens und legt einen neuen an, statt den gemeinten zu überschreiben; die Vorschau sagt das
ehrlich, aber sie kann den Zusammenhang nicht kennen. Sauber wird das erst mit einer Spalte
`ID_Stamm` in `Tab_WP` — ein nummerierter Schritt über `SchemaMigration` samt Nachziehen der
bestehenden Kopien über den Namen. Das ist ein eigener Auftrag im Kern und keine Nacharbeit an
der Maske, deshalb steht es nicht in dieser Welle. Drei kleinere Punkte sind Folgen davon,
dass der Dialog jetzt die **Anlage** zeigt: Die Kühlleistung kommt nicht mehr aus dem
Katalogsatz und steht ohne gepflegten Projektwert auf 0; ein Baujahr 0 einer nie gepflegten
Anlage erscheint als Bestandswert des Projekts; und der Mangelrahmen einer fehlenden
Pflichtangabe markiert den ganzen Stammfeldblock statt des einzelnen Feldes. Zuletzt:
`WPCtrl.Delete` ist derselbe Fall wie das entfernte `Update` — `WHERE Bezeichner` ohne
Projektfilter, ohne Aufrufer. Entfernt wurde hier nur, was der neue Schreibweg ersetzt; der
Löschweg gehört in denselben Aufräumschritt.

**Prüfung.** Kern-Filter 0 Fehler, Windows-Schale 0 Fehler, `EPOS.UI.Tests` 4 489/4 489;
SQL-Dialekt-Prüfer 0 Fundstellen; das Gate grün bis auf den bekannten Bildvergleich. Der
**Rechenweg ist unverändert** — geschrieben wird in Felder, die der Lauf schon vorher las,
und der Referenzlauf ist nicht betroffen. Neu sind `WaermepumpeProjektgeraetTests` mit 18
Fällen gegen eine **Kopie der Testdatenbank**, darunter der Fall, um den es dem Anwender geht:
ein gleichnamiges Gerät im zweiten Projekt bleibt unverändert. Nachgezogen sind
`WaermepumpeAnlageDialogTests` (Bindung und OK, Bezeichner nur lesend, kein Speichern-Knopf,
weiche Sperren, Vorschautexte, Übernahme mit und ohne Kennlinienschalter, Abbrechen) und
`WaermepumpeStammFelderTests` (`BezeichnerAenderbar`).

**Logbuch-Vorschlag** (Version 1.2.0.2):

> Seit 16.09.2026 ändern die Stammdaten einer Wärmepumpe nur das Projekt; die Übernahme in den
> Katalog ist eine eigene Funktion.

## #299 — Wärmepumpe: Heizstab je Anlage, Katalogkopplung über ID, Kühlleistung wählbar (16.09.2026)

Der Anwenderentscheid vom 16.09.2026 traf drei Punkte der Wärmepumpe zugleich: Der Heizstab
soll keine projektweite Einstellung mehr sein, sondern eine Eigenschaft je
Wärmepumpen-Anlage — Wärmepumpenbetrieb mit und ohne Heizstab soll im selben Projekt
vorkommen dürfen. Die Kopplung zwischen Projektkopie und Katalog soll über einen
Schemaschritt auf eine ID statt auf den Namen gestellt werden. Und Wärmepumpen mit
Kühlfunktion sollen wählbar sein. Dazu die letzte Stelle der Versionsnummer erhöhen.

**Schema 78 → 80, zwei Schritte nach ADR-001.** Schritt 79 übernimmt
`Tab_Einstellungen.WP_Heizstab` in `Tab_Energieanlagen.Heizstab` aller Wärmepumpen-Anlagen
und entfernt danach die Spalte — der erste Schritt des SQLite-Zweigs, der eine Spalte
entfernt, statt nur anzulegen; der Rechenweg liest den Heizstab seither je Modul. Schritt 80
legt `Tab_WP.ID_Stamm` an, einen Verweis auf `Tab_WP_STAMM`, und trägt ihn bei eindeutigem
Bezeichner nach; Katalogkopie, Übernahme in den Stamm und das Lesen der Kennlinien aus dem
Katalog laufen seither über die ID. Die Übernahme-Vorschau nennt dabei einen inzwischen
umbenannten Katalogsatz beim richtigen Namen.

**`WErzeugerCtrl.KonfigurationSchreiben` schreibt, ohne zu löschen.** Die acht
Konfigurationsfelder einer Anlage gehen jetzt über ein read-modify-write; Ids und die
Hervorhebung der bearbeiteten Karte auf der Simulationsseite bleiben dabei erhalten — der
teure Weg über `Del_Projekt_Waermeerzeuger` + `Add_WP_Waermeerzeuger` aus #297/#298 ist an
dieser Stelle nicht mehr nötig, die Simulationshülle nutzt den neuen Weg. Entfallen sind
`KonfigurationModel.m_WP_Heizstab`, `ParameterDaten.Heizstab`, das KI-Feld `wp_heizstab` und
der projektweite Heizstab-Schalter im Konfigurationsdialog der Simulation; fehlt dort die
Anlagen-Naht, meldet der Dialog das jetzt benannt statt leer zu bleiben.

**Oberfläche.** Der Anlagendialog trägt den Schalter „Heizstab mitrechnen" je Anlage, mit
Hinweis, wenn die Projektkopie keine Heizstableistung führt. Die Kühlleistung der
Projektkopie ist im Stammfelderblock als Kommazahl bearbeitbar; die Katalogauswahl führt
dafür die Zahlenspalte „Kühlleistung [kW]" statt der bisherigen Ja/Nein-Spalte, dazu den
Schalter „nur mit Kühlfunktion". Das Variantenmerkmal „Heizstab mitrechnen" ist als Ressource
angelegt.

**Ein Nebenbefund, der für sich stand: Anwendereinstellungen gingen bei einem Versionssprung
verloren.** `Settings.Default.Upgrade()` fehlte im Programmstart; ohne ihn wären bei jedem
Versionswechsel neun Anwenderwerte zurückgesetzt worden, darunter der Datenbankpfad. Der
Programmstart ruft ihn jetzt auf.

**Was offen bleibt.** Zwei Punkte aus „Nach #298" sind mit dieser Welle erledigt — die
Katalogkopplung über `ID_Stamm` und die sichtbare, bearbeitbare Kühlleistung. Drei bleiben
unverändert offen: das Baujahr 0 einer nie gepflegten Anlage, der Mangelrahmen, der den
ganzen Stammfeldblock statt des einzelnen Feldes markiert, und `WPCtrl.Delete` mit
`WHERE Bezeichner` ohne Projektfilter. Dazu drei neue Punkte dieser Welle: Die Einstellungen
zum Kühlbetrieb — welche Parameter, wie sie in die Simulation gehen — warten auf das
Gebäudesimulationskonzept. Altzeilen in `Tab_Energieanlagen` mit `Heizstab = 1` ohne
Wärmepumpe stehen in der Testdatenbank nicht; in Kundendatenbanken würden sie weiter einen
Stromträger ziehen, was nur dort auffällt, wo eine Kessel- oder BHKW-Zeile das Kennzeichen
trägt. Und `Tab_WP.Kuehlleistung` NULL wird beim Speichern im Anlagendialog zu 0,0 — fachlich
gleichbedeutend, Nullbarkeit bis ins Modell wäre die Alternative.

**Prüfung.** Kern-Filter 0 Fehler; `EPOS.UI.Tests` 4 516/4 516; `EPOS.Kern.Tests`
3 055/3 056 (einzig rot: der bekannte fremde Fall
`SpeicherFlottenGroessenCtrlTests.Die_Fusszeile_der_Karte_nennt_den_Feinpunkt`);
Windows-Schale 0 Fehler; SqlDialektPruefer 1 438 Texte, 0 Fundstellen; Referenzlauf 5/5 PASS,
byte-gleich; Testdatenbank auf Schemastand 80, `integrity_check` ok, `foreign_key_check`
leer. 23 neue Testmethoden (Kern 10, Oberfläche 13).

**Logbuch-Vorschlag** (Version 1.2.0.2):

> Seit 16.09.2026 wird der Heizstab je Wärmepumpe eingestellt, und Wärmepumpen mit Kühlfunktion
> sind in der Katalogauswahl filterbar.
>
> Seit 16.09.2026 bleiben Anwendereinstellungen beim Versionswechsel erhalten.

## #300 — Neues Projekt: der Assistent startet in der Projektkonfiguration (16.09.2026)

Der Anwenderentscheid vom 16.09.2026 galt der Kachel „Neues Projekt": Sie soll nicht mehr die
Komponentenauswahl zeigen — bei einer Neuanlage ist sie ohne Bedeutung —, sondern den
Assistenten unmittelbar in der Projektkonfiguration beginnen; „Weiter" soll auf die Kachel
Wärmebedarf springen.

**Die Kachel meldet den Einstieg.** Sie trägt der `AppWurzel` jetzt mit, dass es sich um eine
Neuanlage handelt (`AssistentEinstieg`); der Assistent beginnt daraufhin auf der
Projektkonfiguration, ohne den Komponentenschritt, mit der Knopfleiste Abbrechen / ◀ Zurück /
Weiter ▶. „Weiter" prüft und speichert wie bisher und meldet sein Ziel; die `AppWurzel`
wechselt zur Startseite und holt dort den Reiter „Wärmebedarf" nach vorn. „Zurück" führt ohne
Rückfrage zur Startseite, „Abbrechen" fragt weiterhin nach. Das Menü „Projekt → Neu…" und die
iOS-Projektliste behalten den vollständigen Assistenten mit Komponentenauswahl — nur die
Kachel ändert ihr Verhalten. Es kommen keine neuen Ressourcen und keine Hüllenänderung hinzu.

**Was offen bleibt.** Beide Fragen sind erledigt. Die erste — soll „bei Neuanlage" auch für
das Menü „Projekt → Neu…" und den iOS-Knopf gelten, sodass allein die Betriebsart
entscheidet? — ist mit dem Nachtrag vom 16.09.2026 (Commit `934b7fc2`) erledigt: Der Einstieg
Neuanlage gilt jetzt für jede Neuanlage, Menü und iOS-Knopf starten wie die Kachel in der
Projektkonfiguration; BEARBEITEN zeigt weiterhin den vollständigen Assistenten mit
Komponentenauswahl. Die zweite — „Zurück" verwirft einen eingegebenen Projektnamen ohne
Rückfrage — ist mit dem Nachtrag vom 16.09.2026 (Commit `c899de4b`) erledigt: Der
Zurück-Knopf entfällt in der Neuanlage vollständig, an seine Stelle tritt das Schließkreuz,
das denselben Weg wie „Abbrechen" nimmt.

**Prüfung.** 13 neue Facts (`EPOS.UI.Tests/Seiten/AssistentNeuanlageTests.cs`); das Gate lief
gemeinsam mit #299 (Zahlen dort).

**Nachtrag 16.09.2026 (Commit `c899de4b`).** Anwenderentscheid: „Es gibt kein Zurück in
diesem Dialog, stattdessen Abbrechen (oder Kreuz zum Schließen)." Die Knopfleiste der
Neuanlage wird zu Abbrechen / Weiter ▶: Der Zurück-Knopf wird über die Baustein-Gabe
`ZurueckAnzeigen` nicht mehr gezeichnet, die Gabe `Zurueckgetreten` und der Weg „Zurück ohne
Rückfrage" entfallen. Dabei fiel auf, dass die Assistentenseite in keinem Einstieg — weder
Neuanlage-Kachel noch vollständigem Assistenten — ein Schließkreuz trug; die Wache
`SchliesskreuzWacheTests` prüft nur Dialoge und Überlagerungen, nicht Seiten. Sie trägt jetzt
in beiden Einstiegen genau eins, das denselben Weg wie „Abbrechen" nimmt (Rückfrage bei
Änderungen, ohne Änderungen sofort); neue CSS-Regel `.epos-seite-kopf .epos-dialog-zu`.
`EPOS.UI.Tests/Seiten/AssistentNeuanlageTests.cs` zählt danach 17 Methoden / 19 Fälle
(Knopfleiste, kein Zurück-Knopf, Kreuz in beiden Einstiegen, Kreuz wirkt wie Abbrechen,
Gegenprobe vollständiger Lauf mit drei Knöpfen). Gate: Kern-Filter 0 Fehler, `EPOS.UI.Tests`
4 523/4 523, `EPOS.Kern.Tests` 3 074/3 075 (einzig rot: bekannt fremd
`SpeicherFlottenGroessenCtrlTests.Die_Fusszeile_der_Karte_nennt_den_Feinpunkt`).

**Nachtrag 16.09.2026, Anwenderentscheide (Commit `934b7fc2`).** Antwort auf die offene
Frage aus „Was offen bleibt": (1 Ja) Der Einstieg Neuanlage gilt für jede Neuanlage — Kachel,
Menü „Projekt → Neu…" und der iOS-Knopf der Projektliste starten in der Projektkonfiguration,
allein die Betriebsart NEU entscheidet; die Kachelmerker (`_neuanlageWunsch`,
`BeiNeuanlageKachel`) und die Startseiten-Gabe `ProjektNeuGewaehlt` entfallen, BEARBEITEN
zeigt weiterhin den vollständigen Assistenten mit Komponentenauswahl. (2 OK) Das
Schließkreuz bleibt auch im vollständigen Assistenten. 21 Fälle (Menüweg NEU startet in der
Projektkonfiguration; Gegenprobe BEARBEITEN; Ende-zu-Ende über die Kachel).

**Logbuch-Vorschlag** (Version 1.2.0.2):

> Seit 16.09.2026 startet „Neues Projekt" direkt in der Projektkonfiguration.

## #301 — Berichte & Kosten: Kosten nach Rückwechsel auf Stamm, Kostenfaktor-Löschen ohne Kaskade (16.09.2026)

Zwei Anwendermeldungen vom 16.09.2026 zu „Berichte & Kosten", unabhängig voneinander und im
selben Schritt behoben. Commits: `b5ab356c` (Rückwechsel), `4eafaed0` (Kostenfaktor löschen),
`6d29183e` (Testdatenbank auf Schemastand 81 nachgezogen).

**Fehler 1 — Kosten nach Rückwechsel auf das Stammprojekt.** Am Projekt „Booster-Kette mit
Kombi-Speicher" zeigte die Seite „Kosten" nach dem Wechsel Stamm → Variante → Stamm „Kein
Projekt gewählt." mit leeren Kacheln und Tabellen; erst nach Verlassen und Neubetreten des
Bereichs waren die Stammkosten wieder sichtbar. Ursache waren zwei nicht zusammenpassende
Regeln in der Windows-Hülle des Bereichs (`WindowsFormsApplication1/Views/BerichteKosten/
UebersichtSeiteGaben.cs`, `BerichteKostenHuelle.cs`): Die Markierung wurde beim Wechsel des
aktiven Projekts geräumt und nur im Varianten-Zweig neu gesetzt; die Kostenseite erhielt
danach die leere Markierung (−1), obwohl eine Rettung eine Zeile zuvor das Stammprojekt
gesetzt hatte. Behoben mit der neuen plattformfreien Klasse
`EPOS.Kern/Allgemein/Bericht/Berichtsgruppe.cs` (Nachbarin von `Vergleichsauswahl`): Sie hält
Stammprojekt und markierte Version und hütet beide Regeln an einer Stelle — der Kontext
markiert sich selbst, Variante wie Stamm; die Kostenseite zeigt die markierte Version, ohne
Markierung das Stammprojekt. Beide Hüllen teilen sich eine Instanz. Mitbehoben:
Wirtschaftlichkeit und Bericht hängen ihre Vergleichsgruppe jetzt am Stand statt am
Meldungsargument — der Rückfall des Listenladens hatte die Gruppe sonst ohne Meldung
gewechselt; der Name der Markierung folgt der Id. Übersicht, Wirtschaftlichkeit und Bericht
haben auf demselben Wechselweg keinen weiteren Fehler. 7 neue Fälle in
`EPOS.Kern.Tests/BerichtsgruppeTests.cs`, 1 bUnit-Fall in
`EPOS.UI.Tests/Seiten/BerichteKostenSeiteTests.cs` (Stamm → Variante → Stamm auf derselben
Seiteninstanz); mit der alten Regel sind 4 Kern-Fälle und der bUnit-Fall rot.

**Fehler 2 — Kostenfaktor löschen riss Projektpositionen mit.** Die Anwendermeldung lautete:
„Beim Löschen einer nicht zugeordneten Komponente werden die Kosten anderer, nicht aller
Komponenten einschließlich der kompletten Kostenpositionen gelöscht." Ursache war nicht der
Papierkorb an der Zeile „ohne Anlagenzuordnung" (der löscht nachweislich genau eine
Position), sondern „Kostenverwaltung öffnen… → Administration Kostenfaktoren → Löschen":
`KostenfaktorCtrl.Loeschen` löschte den Katalogeintrag in `Tab_Kostenfaktor`, und der
Fremdschlüssel `Tab_ProjektWerte.StammID → Tab_Kostenfaktor` trug `ON DELETE CASCADE` — damit
verschwand jede Projektposition mit dieser Kostenfaktor-ID in allen Projekten und Gewerken;
die Rückfrage nannte nur den Katalognamen. „Nicht alle", weil die zehn Hauptpositionen der
Gewerke vom Löschen ausgenommen sind. Nachgerechnet auf einer Kopie der Testdatenbank:
„Planung / Baunebenkosten" löschen → 175 → 169 Positionen in 5 Projekten und 3 Gewerken;
„Wärmepumpe (Aggregat)" löschen → 175 → 171, Wärmepumpen-Investition in den Projekten
1042/1043/1044 von je 13 000 € auf 0 €. Behoben in zwei Schichten: (1)
`KostenfaktorCtrl.Loeschen(int, out string)` zählt vor dem Löschen Projekt- und
Vorlagenpositionen und verweigert mit benanntem Grund (Ressource `KFAK_MSG_IN_BENUTZUNG`:
„{0} Projektposition(en) in {1} Projekt(en) und {2} Vorlagenposition(en) verweisen auf diesen
Kostenfaktor."); Hülle und Dialog zeigen den Grund als Warnbanner, der Eintrag bleibt stehen;
mitbehoben ein am Filter leer gelaufener DELETE, der bisher Erfolg meldete. (2) Schemaschritt
81 nach ADR-001 (`EPOS.Kern/Allgemein/Update/ProjektWerteLoeschschutz.cs`): `Tab_ProjektWerte`
wird neu gebaut mit `ON DELETE RESTRICT` (STRICT, fünf Fremdschlüssel, AUTOINCREMENT-Stand,
fünf Indizes und die Sicht `Abfrage_Kostenfaktoren` bleiben erhalten; die Umbenennung läuft
unter `PRAGMA legacy_alter_table`); ein zweiter Lauf ist No-op. Testdatenbank auf Stand 81
(175 Zeilen unverändert, `integrity_check` ok, `foreign_key_check` leer), Nachtrag in
`Referenzlaeufe/LIESMICH.md`; die Referenzbasis wird dafür nicht neu eingefroren. 4 Fälle in
`EPOS.Kern.Tests/KostenfaktorCtrlTests.cs` (3 davon vorher rot), 8 Fälle in
`ProjektWerteSchemaWacheTests.cs` (mit Gegenbeweis auf dem alten Stand und
Umbau-Wiederholbarkeit), 2 weitere in `KostenfaktorKatalogDialogTests`.

**Prüfung.** Beide Fixe liefen im eigenen Worktree-Gate, zusammengeführt per Cherry-Pick ohne
Konflikt; das Gesamtgate folgt auf dem zusammengeführten Stand. Fix 1: Kern-Filter 0 Fehler,
`EPOS.UI.Tests` 4 504/4 504, `EPOS.Kern.Tests` 3 060/3 061, Windows-Schale 0 Fehler. Fix 2:
Kern-Filter 0 Fehler, `EPOS.UI.Tests` 4 505/4 505, `EPOS.Kern.Tests` 3 065/3 066,
Windows-Schale 0 Fehler, SqlDialektPruefer 1 460 Texte, 0 Fundstellen, Referenzlauf 5/5 PASS
byte-gleich. Einzig rot jeweils der bekannt fremde Fall
`SpeicherFlottenGroessenCtrlTests.Die_Fusszeile_der_Karte_nennt_den_Feinpunkt`.

**Was offen bleibt.** Vier Punkte, alle eigener Auftrag, weil sie
`KostenProjektPositionenCtrl` und `KostenSeiteGaben` berühren: Der Papierkorb an der Zeile
„ohne Anlagenzuordnung" fragt ohne Anzahl und Summe nach. Beim Anlegen einer Variante
behalten bereits lose Positionen den Geräteanker des Quellprojekts (`AnkerNachziehen` zieht
nur Zeilen mit gültiger Anlage nach) und können nie wieder zugeordnet werden.
`BetriebskostenCtrl` schreibt Betriebskosten anlagenblind — bei mehreren Anlagen desselben
Gewerks wird die erste überschrieben; für die Investition ist das bereits behoben.
Variante/Projekt löschen läuft über den Projektnamen statt über die Id. Dazu ein
Bestandsbefund ohne Wächter: Es gibt keine Prüfung, die die Repo-Testdatenbank selbst auf
ihren Schemastand hält — jede Arbeitskopie wird im Test selbst nachgezogen.

**Logbuch-Vorschlag** (Version 1.2.0.2):

> Seit 16.09.2026 zeigt die Seite Kosten nach dem Wechsel zurück auf das Stammprojekt wieder
> dessen Kosten, und verwendete Kostenfaktoren lassen sich nicht mehr löschen.

## #302 — Kostenbereich: fünf Nebenbefunde behoben (16.09.2026)

Anwenderentscheid 16.09.2026 zu den fünf offenen Punkten aus „Nach #301": „Nebenbefunde
Kosten für einen eigenen Auftrag: Empfehlung" — alle fünf Punkte in einem Auftrag. Commit
`e2784cf9`.

**Punkt 1 — Papierkorb der Zeile „ohne Anlagenzuordnung" nennt Anzahl und Summe.** Neu im
Kern `KostenProjektPositionenCtrl.LoseZaehlen(projekt, komponente, kategorie)` → Anzahl und
angezeigte Summe, über denselben Leseweg, der „ohne Anlagenzuordnung" definiert;
`KomponentenId(name)` ersetzt das Inline-SQL der Windows-Hülle. Die Rückfrage nennt jetzt
Anzahl und Betrag (Ressource `BK_KOSTEN_LOSE_LOESCHEN_SUMME`); gibt es nichts zu löschen,
entfällt die Rückfrage benannt (Fußzeile, `BK_KOSTEN_LOSE_KEINE`, Razor-Parameter
`LoeschNichts`). `BK_KOSTEN_LOSE_LOESCHEN` entfernt.

**Punkt 2 — Geräteanker loser Positionen beim Kopieren.** `AnkerNachziehen` setzt je
Komponente zusätzlich `ID_AnlageGeraet = NULL` für Zeilen ohne gültige Anlage. Kein
Schemaschritt: Alle vier Leser des Ankers fassen keine Zeile ohne gültige Anlage an, die
toten Anker des Bestands sind inerte Daten; Testdatenbank unverändert.

**Punkt 3 — Betriebskosten anlagenbewusst.** `BetriebskostenCtrl.Lies`/`Speichere` bekommen
eine `idAnlage`-Überladung über `FindePosition`/`SetzeBetrag(…, idAnlage)`; die
Bestandssignaturen delegieren mit 0. Befund dazu: `BetriebskostenCtrl.Lies(int,
Bezugsgroessen)`, `Speichere(int, List<Zeile>)`, `LiesBezugsgroessen`, `Bezugsgroessen`,
`Zeile` haben repoweit keinen Aufrufer mehr — Rest des alten WinForms-Betriebskostendialogs —,
berichtigt statt entfernt; Aufräum-Entscheid offen. Die anlagenblinde
`KostenPositionCtrl.SetzeBetrag`-Überladung bleibt, sie hat zwei weitere Aufrufer.

**Punkt 4 — Löschen über die Id.** `ProjektCtrl.Delete(int)` ersetzt `Delete(string)`; die
Vorarbeiten `PufferReferenzenLoesen`, `BerichtsKonfigurationEntfernen`,
`VariantenVerknuepfungenEntfernen` arbeiten über die Id (drei Namensabfragen entfallen);
`LoeschenMitVorarbeiten` und `VariantenCtrl.LoescheVariante` reichen die Id durch. Die
Namensprüfung bleibt als Anzeige (`LoeschStand.Mehrdeutig`, Text `PROJ_MSG_NAME_MEHRDEUTIG`
geändert): Bei doppeltem Namen fällt nur noch das gewählte Projekt.

**Punkt 5 — Wächter Schemastand der Repo-Testdatenbank.** Neu
`EPOS.Kern.Tests/TestdatenbankSchemastandWacheTests`: öffnet
`Referenzlaeufe/Kenndaten_Test.sqlite` nur lesend (`mode=ro&immutable=1`, keine
`-wal`/`-shm`-Reste) und hält `Tab_Applikation.SchemaVersion` gegen `SchemaStand.Zielversion`
(beide 81).

**Dateien.** `EPOS.Kern/Controller/{KostenProjektPositionenCtrl,BetriebskostenCtrl,
ProjektCtrl,VariantenCtrl}.cs`, `EPOS.Kern/Model/ProjektAngaben.cs`, Ressourcen (resx beide
Sprachen + Designer), `EPOS.UI/Seiten/Berichte/KostenSeite.razor`,
`WindowsFormsApplication1/Views/BerichteKosten/KostenSeiteGaben.cs`, Tests
`EPOS.Kern.Tests/{KostenProjektPositionenCtrlTests,ProjektpflegeTests,
BetriebskostenAnlagenbezugTests,TestdatenbankSchemastandWacheTests}.cs`,
`EPOS.UI.Tests/Seiten/KostenSeiteTests.cs`. 9 neue Testfälle, 4 nachgezogen; rot-vor/grün-nach
belegt für Punkt 2 (3 tote Anker statt 0) und Punkt 3 (zweite Anlage bekam keine eigene
Zeile).

**Prüfung.** Worktree-Gate, per Cherry-Pick ohne Konflikt übernommen; das Gesamtgate läuft auf
dem zusammengeführten Stand. Kern-Filter 0 Fehler; `EPOS.Kern.Tests` 3082/3083,
`EPOS.UI.Tests` 4522/4522, KiKern 499/499, SpeicherEngine 370/370, SpeicherPlanung 27/28 (1
übersprungen); einzig rot der bekannt fremde
`SpeicherFlottenGroessenCtrlTests.Die_Fusszeile_der_Karte_nennt_den_Feinpunkt`; Windows-Schale
0 Fehler; SqlDialektPruefer 1458 Texte, 0 Fundstellen; Referenzlauf 5/5 PASS.

**Was offen bleibt.** Beide Punkte sind mit dem Nachtrag vom 16.09.2026 erledigt (siehe
unten): die aufruferlosen Betriebskosten-Methoden sind entfernt (Commit `de624db9`), der
`SqlDialektPruefer` öffnet die Testdatenbank seither nur lesend (Commit `88f58dbf`).
**Neuer offener Punkt, mit dem zweiten Nachtrag erledigt:** Mit dem Wegfall von `Lies`
war auch der VDI-Katalog in `BetriebskostenCtrl` (`Katalog`, Typ `Position`, `Finde`, die sechs
`BEZUG_*`) ohne Leser; er blieb zunächst stehen, weil
`WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs` (Zeile ~828) ihn als die eine
Stelle nannte, an der die VDI-Empfehlungsbereiche (`EmpfehlungVon`/`EmpfehlungBis`, § 7.6)
leben, und deshalb keine Datenbankspalten dafür anlegt. Der Anwender hat am 16.09.2026
„beides entfernen" entschieden (Commit `605531ab`, zweiter Nachtrag unten). Aus #301 bleibt
nichts offen, was nicht mit #302 erledigt ist.

**Nachtrag 16.09.2026, Anwenderentscheide (Commits `de624db9`, `88f58dbf`).** Antwort auf die
zwei offenen Punkte aus „Was offen bleibt": (3 Aufräumen) `BetriebskostenCtrl` —
`Lies`/`Speichere` samt den in `e2784cf9` ergänzten `idAnlage`-Überladungen,
`LiesBezugsgroessen`, die Typen `Bezugsgroessen` und `Zeile` und drei private Helfer
(`LetztesErgebnis`, `ModulSumme`, `LiesBrennstoffkosten`) sind entfernt (925 → 624 Zeilen);
geblieben, weil Aufrufer da sind: `Betrag`, `SatzEinheit`, `MengenEinheit`,
`Kaskadensummen`, `InvestSummeFuer`. Die Tests `BetriebskostenAnlagenbezugTests` prüfen
jetzt den lebenden Weg der Kostenseite (`KostenProjektPositionenCtrl.Lies/Neu/Speichern` mit
Anlagenbezug; Projekt 1030, Anlagen 14920/14921). (4 Empfehlung)
`Werkzeuge/SqlDialektPruefer/pruefer.py` öffnet die Testdatenbank als file-URI mit
`mode=ro&immutable=1`; keine `-wal`/`-shm`-Reste mehr; Ergebnis unverändert 1460 Texte, 0
Fundstellen; Werkzeug-LIESMICH ergänzt. **Prüfung** (Commit-Stand `88f58dbf`): Kern-Filter 0
Fehler; `EPOS.UI.Tests` 4532/4532; `EPOS.Kern.Tests` 3100/3101 (einzig rot: bekannt fremd
`SpeicherFlottenGroessenCtrlTests.Die_Fusszeile_der_Karte_nennt_den_Feinpunkt`);
Windows-Schale 0 Fehler.

**Nachtrag 2 vom 16.09.2026, Anwenderentscheid (Commit `605531ab`).** Antwort auf den neuen
offenen Punkt: **beides entfernen**. **Befund:** Der VDI-Katalog war eine tote Zweitkopie der
Normdaten — die Empfehlungsbereiche nach VDI 2067 leben an der Position einer Kostenvorlage
(`Tab_KostenVorlagePosition`, Spalten `Empfehlung_von`/`Empfehlung_bis`, gesät aus
`SchemaKatalog.Schritt39_Vorlagen`), sind über die Vorlagenpflege änderbar — die Konstanten
waren es nicht — und erscheinen über `KostenKomponenteHuelle.EmpfehlungText` am Satzfeld.
`Katalog`, der Typ `Position`, `Finde` und die sechs `BEZUG_*` sind entfernt (625 → 418
Zeilen); der Kommentar zu Schritt 27 in `SchemaMigration` und die Zeile in `DbWerte` nennen
jetzt den lebenden Ort, `EndenergieAufloeser` verweist im Klartext statt auf `BEZUG_VBH_BHKW`.

**Folgearbeiten zum selben Entscheid.** (1) Sieben Ressourcenschlüssel ohne Verwender —
`VDI_BEZUG_INVEST_BHKW`, `…_INVEST_KESSEL`, `…_INVEST_GESAMT`, `VDI_BEZUG_STROM`,
`VDI_BEZUG_VBH`, `VDI_BEZUG_BRENNSTOFF`, `VDI_BEZUG_FEHLT` — je Schlüssel geprüft (kein
Codeverwender, kein dynamisches `GetString`) und aus `Resource.resx`, `Resource.en-US.resx`
und `Resource.Designer.cs` entfernt; Designer über `Werkzeuge/ResourceDesigner` neu erzeugt,
6 331 → 6 324 Einträge, Prüflauf „unverändert; wiederholbar". (2) `Lokalisierung_Katalog.md`:
die beiden Katalogzeilen der `VDI_BEZUG_*` und die Zeile der `BEZUG_*`-Konstanten aus „Nicht
lokalisiert" gestrichen, dafür ein Abschnitt „Entfallen (7)" nach dem Muster der Datei.
(3) `Konzept_Nutzungsdauer_AfA_EPOS-Plan.md`, Abschnitt 1.6: Die Aussage „stehen mit ihren
Empfehlungsbereichen als Konstanten in `BetriebskostenCtrl` — nicht in einer editierbaren
Tabelle" ist durch den gültigen Stand ersetzt. (4) Zwei tote Kommentare auf den lebenden Weg
umformuliert: `SchemaKatalog.SPALTE_KVP_IST_PFLICHT` (jetzt
`KostenProjektPositionenCtrl.Loeschen`/`…Speichern` statt `BetriebskostenCtrl.Speichere`) und
der Klassenkopf von `BetriebskostenAnlagenbezugTests`.

**Prüfung** (Commit-Stand `605531ab` samt Folgearbeiten): Kern-Filter 0 Fehler / 5 Warnungen
(Bestand); `EPOS.Kern.Tests` 3100/3101, `EPOS.UI.Tests` 4532/4532, KiKern 499/499,
SpeicherEngine 370/370, SpeicherPlanung 27/28 (1 übersprungen) — einzig rot der bekannt
fremde `SpeicherFlottenGroessenCtrlTests.Die_Fusszeile_der_Karte_nennt_den_Feinpunkt`;
Windows-Schale 0 Fehler. Kein Referenzlauf nötig: keine Zeile am Rechenweg.

**Logbuch-Vorschlag** (Version 1.2.0.2):

> kein eigener Eintrag — zu klein für das Logbuch (Regel 13.4).

## #303 — Die gepflegte Kaskade und der Weg zurück in sie (16.09.2026)

Drei Anwenderentscheide vom 16.09.2026 auf den offenen Punkt „Nach #302". Sie hängen
inhaltlich zusammen, aber nicht technisch, und sind einzeln gemessen. Commits: `2145dcc7`
(Merkspalte, Schemaschritt 82), `7e179ab5` (die Meldung wird handlungsfähig), dazu die
Papieränderung zum Größenlauf. **Keine neue Referenzbasis** — die Abnahme war der Nachweis,
dass sich nichts verschiebt.

**Entscheid 1 — die Merkspalte (Wahl (b)).** Der Befund aus „Nach #302": Wer den Heizkessel
in der Simulationskonfiguration mit „×" aus der Kaskade nahm, fand ihn beim nächsten Lesen
der Konfiguration wieder darin. `Kaskade.Entfernen` setzt den Platz leer, und ein leerer Platz
ist genau die Bedingung, unter der `KonfigurationCtrl.HeizkesselNachziehen` ihn erneut
aufnimmt — `Tab_Einstellungen.Tool_1..4` trägt die BELEGUNG, nicht die ABSICHT. Gebaut ist
**Schemaschritt 82** nach ADR-001: `Tab_Einstellungen.Kaskade_Gepflegt`, 0/1, `NOT NULL
DEFAULT 0` mit `CHECK`, angelegt per `ALTER TABLE … ADD COLUMN` (die Tabelle ist STRICT, ein
INTEGER mit Vorgabe ist dort zulässig — kein Tabellenneubau). Name und Spaltenliste stehen im
Kern (`SchemaKatalog.SPALTE_KASKADE_GEPFLEGT`, `Schritt82_KaskadeGepflegt`); Migration und
`Werkzeuge/Testdatenbankschema` bedienen sich derselben Quelle, `SchemaStand.Zielversion`
steht auf 82. Gelesen wird die Spalte namensbasiert (die Ordinalkette von `ZeileUebernehmen`
bleibt unberührt), geschrieben über ein eigenes, zielgenaues UPDATE
(`KonfigurationCtrl.KaskadeGepflegtSchreiben`). `HeizkesselNachziehen` steigt bei 1 aus,
bevor es irgendetwas schreibt; der Doc-Satz „nur EINMAL je Projekt" war falsch, sobald jemand
entfernt, und ist berichtigt. Gesetzt wird die Marke an EINER Stelle — in
`SimulationKonfigHuelle` bei allen drei Handgriffen (aufnehmen, entfernen, verschieben) und
nur, wenn der Handgriff wirklich etwas geändert hat —, und zwar ins Modell UND in die
Datenbank, weil `Ladeordnung.Kaskadenpositionen` `Tool_1..4` während des Laufs ein zweites Mal
von dort liest. `Speichern` (Delete + Insert) reicht sie nach, sonst verlöre gerade das
Speichern die Aussage. **Zweite Fundstelle, im Auftrag nicht genannt:** Die Vorwahl Ä15
(`VerbauteAnlagenVorwaehlen`) füllt beim Öffnen der Konfigurationsseite jeden leeren Platz mit
dem, was das Projekt führt — sie hätte den eben entfernten Erzeuger im Arbeitsstand
zurückgeholt und den Entscheid an der Oberfläche wirkungslos gemacht. Sie trägt deshalb
dieselbe Sperre. **Bewusste Folge:** Wer die Kaskade einmal von Hand angefasst hat, bekommt
die Automatik auch dann nicht mehr, wenn er später eine neue Kesselanlage anlegt.

**Entscheid 2 — die Meldung wird handlungsfähig (Wahl (c)).** Für Wärmepumpe, Solarthermie,
BHKW, Photovoltaik und Stromspeicher wird **ausdrücklich nichts nachgezogen**: Wer einen
Erzeuger weglässt, meint das oft so. Statt dessen bekommt die Meldung einen Griff.
`SimulationLaufCtrl.AufnahmeMoeglich` beantwortet, ob überhaupt ein Platz frei wäre (einer der
vier Wärmeplätze bzw. der eigene Stromplatz `Tool_5`/`Tool_6`); die Ergebnishülle baut daraus
je Anlage ein `Platzangebot` — aus DERSELBEN Vorprüfung, die schon den Zusatz „(nicht in der
Kaskade)" an der Tabellenzeile speist. Der Übersichtsreiter zeigt sie als `Warnbanner` über
dem Dashboard; das Banner trägt dafür einen optionalen Handgriff (`AktionText`/`Aktion`) neben
„erklären lassen". **Gewählt ist die Ausprägung „springen und hervorheben", nicht das
unmittelbare Aufnehmen**, und der Grund ist die Datenhaltung: Die Kaskade gehört Schritt ①,
ihr Arbeitsstand liegt in `SimulationKonfigHuelle`, die zwischen zwei Besuchen lebt und erst
mit „Speichern" schreibt. Aus Schritt ③ heraus aufzunehmen schriebe an diesem Arbeitsstand
vorbei — zwei Wahrheiten über dieselbe Kaskade und ein angezeigtes Ergebnis, das still nicht
mehr zu seinen Eingaben passt. Der Sprung führt statt dessen an die Stelle, die die
vorhandene Bedienung ohnehin kennt (Hinweisleiste „N Erzeuger nicht aufgenommen", verfügbare
Karten, „+ aufnehmen"): Die Ansicht SIMULATION wechselt nach ① und ruft dort
`SimulationKonfigSeite.KarteHervorheben` — Auswahl auf die Anlage, verfügbare Karten
eingeblendet. Damit bleibt es bei EINEM Aufnahmeweg, und das ist derselbe, der die Kaskade als
gepflegte merkt. Ist kein Platz frei, steht kein Knopf da, sondern der Grund. Vier neue
Ressourcenschlüssel in beiden Sprachen, Designer neu erzeugt. Projekt 1017 rechnet
unverändert: Seine Wärmepumpe bekäme Platz 3, also hinter BHKW und Kessel, die die Wärme
bereits vollständig decken — genau deshalb entscheidet hier der Anwender.

**Entscheid 3 — die Produktparameter im Größenlauf bleiben (nur Papier).** Keine Zeile am
Rechenweg, `FlottenGeraeteuebernahme` unverändert. Im Konzept Stromspeicher-Dialoge 7.10
ersetzt die Entscheidung die offene Frage: Peak-Reserve, Grenzverschleiß, Betriebs- und
Durchsatzkosten, Ersatz, Restwert und Alterungskurve bleiben aus der Achsenvorlage stehen,
auch wenn diese aus einer Projektanlage oder einem Katalogsatz entstanden ist. Was das heißt,
steht dabei: Es sind die Werte dieser einen Anlage, und sie gelten an jedem Rasterpunkt; wer
andere will, ändert sie in Schritt 1 oder 2. Die Herleitungszeile
(`SpeicherFlottenAnzeigeCtrl.Herleitung`, Abschnitt 8.9) sagt je Kandidat, welcher Kennwert
vom Gerät kommt — darauf verweist der Absatz, statt eine zweite Auskunft daneben zu stellen.

**Prüfung.** Kern-Filter 0 Fehler / 5 Warnungen (Bestand, darunter der fremde `xUnit2000` in
`HeizstabJeWaermepumpeTests`), Windows-Schale 0 Fehler / 5 Warnungen. Tests 8 512 grün, 1
bewusst übersprungen (`EPOS.Kern.Tests` 3 093, `EPOS.UI.Tests` 4 523, `SpeicherEngine.Tests`
370, `KiKern.Tests` 499, `SpeicherPlanung.Tests` 27/28). SqlDialektPrüfer 1 464 Texte / 0
Fundstellen, ChartProben 64 Bilder / 0 Verstöße. Referenzlauf über alle dreizehn Projekte
gegen `2026-09-16_R8_Heizkessel_Kaskade`: **13/13 PASS, 3 882 737 Werte, `diff -rq` ohne einen
einzigen Unterschied** außer `protokoll.txt` (Zeitstempel). Testdatenbank auf Schemastand 82,
eine Spalte angelegt, kein DML; alle 22 Zeilen von `Tab_Einstellungen` stehen auf
`Kaskade_Gepflegt = 0`, die Datei bleibt echte SQLite-Datei in Git LFS. **Kein iOS-Lauf, kein
Push, keine neue Referenzbasis.**

**Was offen bleibt.** Drei Punkte. (1) Die Marke wirkt auch auf die Vorwahl Ä15 und damit auf
ALLE Erzeugerarten, nicht nur auf den Heizkessel: Wer in einem Projekt einmal umgeordnet hat
und danach eine Wärmepumpe anlegt, findet sie nicht mehr von selbst in der Kaskade, sondern
als verfügbare Karte samt Hinweisleiste. Das ist die konsequente Lesart des Entscheids („die
Pflege gehört dann ihm"), aber sie ändert das Verhalten über den Heizkessel hinaus — der
Anwender sollte sie bestätigen. (2) Es gibt keinen Weg, eine gepflegte Kaskade wieder der
Automatik zu überlassen; ein Rücksetzen wäre ein eigener Handgriff in der
Simulationskonfiguration. (3) Der Knopf an der Meldung führt in Schritt ①, nimmt aber nicht
auf — der letzte Griff bleibt beim Anwender. Ob er statt dessen unmittelbar aufnehmen soll
(mit dem Preis, den der Entscheid-2-Absatz nennt), ist eine Anwenderfrage.

**Logbuch-Vorschlag** (Version 1.2.0.2):

> Seit 16.09.2026 gehört die Kaskade der Simulationskonfiguration dem Anwender, sobald er sie
> selbst angefasst hat: Ein Heizkessel, den Sie mit „×" herausnehmen, bleibt draußen und wird
> nicht mehr von selbst aufgenommen — auch dann nicht, wenn Sie später eine weitere
> Kesselanlage anlegen. Meldet ein Lauf einen Erzeuger, der im Projekt angelegt ist, aber auf
> keinem Platz der Simulation steht, trägt die Meldung über der Ergebnisübersicht jetzt den
> Knopf „in der Konfiguration aufnehmen": Er führt in Schritt 1 und hebt die Karte dieser
> Anlage hervor. Ist kein Platz frei, nennt die Meldung den Grund.

---

## #304 — Energieträger je Brenner-Anlage: gemessen, abgesagt (16.09.2026)

Anwenderentscheid vom 16.09.2026 auf den offenen Punkt „Nach #302 (Kaskade)": „Was fehlt, ist
die Zuordnung je Anlage zum Energieträger: Brennstoff, Kosten und Emissionen dieser Anlagen
lassen sich im Bericht keinem Träger zuordnen: Korrigiere & setze um." Dazu der Entscheid
**„240 gilt"** vom selben Tag. Commit: `d353369e`.

**Der Auftrag ist an seiner eigenen Abbruchbedingung angehalten und dann vom Anwender
abgesagt worden** („nicht zuordnen“, 16.09.2026, nach Vorlage der drei Wege). Die Zuordnung ist
**nicht** emissionsneutral: Sie verschiebt den wirksamen CO₂-Faktor der betroffenen Anlagen
von **240 auf 201 g/kWh** (Gas) und von **560 auf 435 g/kWh** (Strom) — also **weg** von der
Zahl, die der Anwender am selben Tag festgelegt hat. Das Werkzeug ist gebaut und geprüft, die
Zuordnung ist gemessen, **geschrieben ist nichts**: Testdatenbank und Basis stehen unverändert
auf dem Stand R8, der Referenzlauf ist **13/13 byte-gleich**.

### Was gebaut war (Schritt 1) — mit der Absage wieder entfernt

Das Werkzeug ist mit dem Entscheid aus dem Arbeitsbaum genommen worden (Hausregel
Aufräumen: kein Werkzeug auf Vorrat). Was es leistete, steht hier, damit ein späterer
Auftrag nicht bei null beginnt:

`sql/tools/Setze-Energietraeger-Brenner.sql` samt Läufer `.py`, im Muster von
`Bereinige-Probierpuffer` (#302): Mehrdeutigkeitsprobe **vor** jedem Schreiben (bricht ab und
nennt die Kandidaten), Probe auf einer Kopie, Sollzuordnung je Anlage, Zählungen
vorher/nachher, Nachweis, dass weder eine andere Spalte noch eine Zeile außerhalb des Auftrags
wandert, `PRAGMA foreign_key_check`, `PRAGMA integrity_check`, Wiederholungslauf (zweiter Lauf
ändert 0 Zeilen), `PRAGMA wal_checkpoint(TRUNCATE)` — erst dann die echte Datei. Ohne
`--anwenden` schreibt es nichts. Das SQL setzt `ID_Carrier` nur an Anlagen mit `ID_Type` 10/11,
deren Spalte leer oder 0 ist, und fasst keine andere Spalte an.

Die Probe nennt **elf** Anlagen im ganzen Bestand, davon acht in Referenzprojekten:
1007/10358, 1008/10134, 1017/10260 (BHKW), 1046/14939 auf Träger **64** „Stadtgas";
1018/10369, 1023/11205 auf **63** „Erdgas E"; 1017/10259, 1024/11255 auf **60**. Dazu 1009
zweimal und 1031 einmal, die in keiner Basis mitrechnen. **Nachgeprüft:** Wärmepumpe (29),
Solarthermie (3), Photovoltaik (9), Batteriespeicher (12) und Pufferspeicher (49) führen im
ganzen Bestand keinen `ID_Carrier` — so vorgesehen, keine Lücke.

### Warum angehalten wurde — die Lesekette, an der Messung geprüft

Die Ergänzung zum Auftrag ging davon aus, die aktive `emissionswert`-Zeile greife im Modus
`CO2` nicht, weil sie `ist_co2e = 1` trägt; die Kette falle deshalb auf
`Tab_Brennstoff_Stamm` und damit auf die 240. **Das trifft nicht zu**, und beides ist belegt:

- **Im Quelltext.** `EmissionskatalogCtrl.AktiveWerte` filtert allein auf `ist_aktiv = TRUE`;
  `ist_co2e` kommt in der Bedingung nicht vor. `EmissionsFaktorLader.Lade` übernimmt den Wert
  der aktiven Zeile, sobald er größer 0 ist, und legt `ist_co2e` nur als Merkmal daneben
  (`z.IstCo2e`). `Wirksam("CO2")` gibt genau diesen Wert zurück. Stufe 2 greift also sehr
  wohl, und Stufe 3 wird nie erreicht.
- **In der Rechnung.** Der Referenzlauf mit gesetzten Trägern gegen R8:

  | Projekt | Verbrauch | CO₂ mit R8 (ohne Träger) | CO₂ mit Träger | wirksamer Faktor |
  |---|---:|---:|---:|---|
  | 1007 | 15,47 MWh Gas | 3,71363094 t | **3,11016591 t** | 240,05 → **201,04** g/kWh |
  | 1008 | 12,02 MWh Gas | 2,88510606 t | **2,41627632 t** | 240 → **201** |
  | 1023 | 78,64 MWh Gas | 18,872987 t | **15,8061266 t** | 239,99 → **200,99** |
  | 1046 | 15,47 MWh Gas | 3,71363094 t | **3,11016591 t** | 240,05 → **201,04** |
  | 1017 (Kessel) | 18,70 MWh Strom | 10,4728765 t | **8,13518083 t** | 560 → **435** |
  | 1017 (BHKW) | 90,17 MWh Gas | 21,6405316 t | **18,1239452 t** | 240 → **201** |

  Die Verhältnisse treffen die Katalogzahlen auf fünf Stellen: 0,83750 = 201/240 und
  0,77679 = 435/560.

- **Der Modus ändert daran nichts.** Ausgewählt sind allein die drei Kernarten CO₂, SO₂ und
  NOₓ; SO₂ und NOₓ tragen den Äquivalenzfaktor 0, die CO₂e-Summe ist deshalb gleich dem
  CO₂-Wert. `CO2` und `CO2E` enden auf derselben aktiven Zeile. Alle dreizehn Referenzprojekte
  stehen auf `CO2`.

**Damit steht die Zuordnung gegen den Entscheid „240 gilt".** Heute erfüllen ihn alle
dreizehn Referenzprojekte — die einen über den Rückfall auf den Brennstoffstamm (keine
Zuordnung), die anderen über eine Projektübersteuerung in `energy_project_settings`, die für
jeden bereits zugeordneten Träger eines Referenzprojekts bei 240 (bzw. 310, 560) steht. Genau
deshalb verschieben sich 1018 und 1024 auch mit neuem Träger nicht: Ihre Übersteuerung steht
in der Kette über der Katalogzeile. Ohne Übersteuerung landet ein zugeordneter Träger auf der
aktiven `BAFA_EEW`-Zeile — 201 statt 240.

### Was die Zuordnung sonst bewegt

- **Emissionen:** 14 Abweichungen in 3 882 737 Werten, alle in `aggregate.csv` — die sechs
  CO₂-Größen oben und die neu belegte `carrier_id` je Modul. SO₂, NOₓ, CO und Staub bleiben
  Wert für Wert gleich; ebenso Wärme, Strom, Brennstoffmengen und Deckungsgrade. Kein Vektor
  einer Ganglinie bewegt sich.
- **Kosten: im Referenzlauf nicht messbar.** Der Export führt 310 eindeutige Skalare und
  **keinen einzigen** mit Kosten-, Preis- oder Kapitalwertbezug. Die Verbesserung, um die es
  dem Entscheid geht — `KostenEmissionRechner` zählt die acht Anlagen heute als
  `verbrauchOhneTraeger` mit `kostenVollstaendig = false` —, ist damit real, aber durch die
  Basis nicht nachweisbar. Wer sie belegen will, braucht einen Kostennachweis neben dem
  Referenzlauf.

### Die Trägerwahl bei Brennstoff 13 — aus den Preisfeldern gemessen

Die drei Kandidaten sind im Katalog fast gleich: `pricing_model` ELECTRICITY,
`billing_unit` kWh, `hi_kwh_per_unit` 1,0, `price_work`/`price_base`/`price_power` je 0,0.
Sie unterscheiden sich in `hs_kwh_per_unit` (54: **0,0**; 58 und 60: 1,0). Entscheidend sind
aber die Preiszeilen je Projekt (`energy_price`), und die zeigen **in zwei Richtungen**:

| Projekt | 54 | 58 | 60 |
|---|---|---|---|
| **1017** (Kessel 10259) | 0,38 €/kWh + 50 € Grundpreis | 0,32 €/kWh | **keine Preiszeile** |
| **1024** (Kessel 11255) | keine | keine | **0,35 €/kWh + 50 € Grundpreis** |

In 1024 ist 60 der gepflegte Träger; in 1017 ist er der einzige der drei **ohne** Preis — der
Strom des Elektrokessels käme dort mit 0 €/kWh in die Wirtschaftlichkeit. **Eine einheitliche
Regel kann beide Projekte nicht zugleich richtig bedienen**; die Namensgleichheit ist das
einzige Argument, das für alle Fälle gleich gilt. Das Skript steht deshalb weiter auf 60, mit
der Zahl an einer Stelle — aber die Wahl ist eine Anwenderfrage, keine Messung.

### Prüfung

Kern-Filter 0 Fehler, Windows-Schale 0 Fehler / 5 Warnungen (Bestand, unverändert).
Tests 8 527 grün / 1 übersprungen / 0 rot. SqlDialektPrüfer 1 462 Texte / 0 Fundstellen.
ChartProben 64 Bilder / 0 Verstöße. Referenzlauf **13/13 byte-gleich gegen R8** (3 882 737
Werte) — die Testdatenbank steht unverändert, Schemastand 82, ohne LFS-Zeigerdatei. Beide
`UPDATE` des Skripts sind mit `EXPLAIN` gegen die Testdatenbank geprüft. **Kein Test war auf
die Emissionswerte festgenagelt**; die Emissionstests arbeiten mit ausdrücklich genannten
Träger-IDs und sind von der Zuordnung an der Anlage unberührt. **Keine neue Basis, kein Push,
kein CI-Lauf, kein iOS-Lauf, kein Wiki-Upload, keine Zahl in `emissionswert`,
`energy_carrier` oder `Tab_Brennstoff_Stamm` geändert, `GEG_NACHWEIS` nicht aktiv gesetzt.**

### Was ein Weg zurück zur 240 kosten würde — gemessen

Für den Fall, dass der Anwender beides will (Zuordnung UND die Zahlen des Entscheids):

| Träger | aktiv heute | Zeile mit der Zahl des Entscheids |
|---|---|---|
| 63, 64 (Gas) | `BAFA_EEW` 201 | `GEG_NACHWEIS` **240**, vorhanden, nicht aktiv |
| 54, 58, 60 (Strom) | `BAFA_EEW` 435 | **keine** — `GEG_NACHWEIS` steht auf 100, `UBA_STROMMIX` auf 379/387/442 |
| 71 (Heizöl L) | `BAFA_EEW` 266 | `GEG_NACHWEIS` **310**, vorhanden, nicht aktiv |

Beim **Gas** und beim **Heizöl** genügte es also, die vorhandene `GEG_NACHWEIS`-Zeile aktiv zu
setzen. Beim
**Strom gibt es diesen Weg nicht**: Die 560 steht ausschließlich in `Tab_Brennstoff_Stamm`
und in den Projektübersteuerungen. Wer beide Zahlen halten will, braucht deshalb je Projekt
eine Übersteuerung in `energy_project_settings` — genau die Bauart, die 1018, 1024, 1030 und
1039–1045 bereits führen und die sie in dieser Messung unbewegt gelassen hat.

### Bestandsbefund ohne Auftrag

Ein bereits gepflegter Träger muss zum Brennstoff seines Geräts nicht passen: Anlage 14920 des
Projekts 1030 führt Brennstoff 1 „Stadtgas" und Träger 63 „Erdgas E". Das Skript fasst so
etwas bewusst nicht an — ein gepflegter Träger ist ein Anwenderwert. Ob solche Paare gemeldet
werden sollen, ist ein eigener Entscheid.

### Der Entscheid

Vorgelegt wurden drei Wege: (a) zuordnen und 201/435 hinnehmen — der Katalogwert gälte, der
Entscheid „240 gilt“ wäre für zugeordnete Anlagen aufgehoben, neue Basis R9; (b) zuordnen
und die Zahlen sichern — bei Gas und Heizöl über die vorhandene, nicht aktive Quelle
`GEG_NACHWEIS` (240 bzw. 310), beim Strom nur über eine Projektübersteuerung oder eine neu
angelegte Katalogzeile, weil der Katalog zu 54/58/60 keine Zeile mit 560 führt; (c) nicht
zuordnen. Empfohlen war (b).

**Der Anwender hat (c) gewählt: nicht zuordnen.** Damit bleibt es dabei, dass Brennstoff,
Kosten und Emissionen der acht Brenner-Anlagen ohne Träger im Bericht der Sammelzeile „ohne
Zuordnung“ zugeschlagen werden und `KostenEmissionRechner` sie als `verbrauchOhneTraeger` mit
`kostenVollstaendig = false` zählt — zugunsten der Emissionszahlen, die heute gelten.

### Kein Logbuch-Eintrag

Der Entwurf für Version 1.2.0.2 ist mit der Absage hinfällig: Es hat sich nichts geändert,
was ein Anwender sehen würde.

## #433 — Wirtschaftlichkeit: Strombedarf ohne Verwendung, Simulationslauf speichert automatisch, Veraltung sichtbar (22.09.2026)

Anwendermeldung 22.09.2026 am Projekt „Test: G+SP“ (Gaskessel Erdgas LL mit zugeordnetem
Energieträger, Pufferspeicher, Strombedarf aus der Kachel Strombedarf): Die Wirtschaftlichkeit
meldete „Energiekosten nicht bestimmbar: Der elektrischen Erzeugung … ist kein Energieträger
zugeordnet“, die Energieträger-Seite bot Strom gar nicht an (eingeengt auf Gas, Wasserstoff);
nach Entfernen des Strombedarfs blieb die Meldung stehen; „Simulation starten“ auf der Kachel
Simulation half nicht, dieselbe Schaltfläche in der Übersicht schon. Commits `2398ca99`
(Wirtschaftlichkeit/Kern), `23c7bbbc` (Simulationslauf speichert), `9354c1ea` (Änderungsdatum
in den Schreibcontrollern).

**Befund — drei Ursachen.** (1) Zwei Maßstäbe fragten Verschiedenes: Die Energieträger-Seite
prüft anlagenbasiert (Wärmepumpe, Photovoltaik, Stromspeicher, Heizstab, Elektrokessel), die
Wirtschaftlichkeit ergebnisbasiert (Netzbezug > 0 ohne Strompreis → Fehlgrund, alle Kennzahlen
bleiben leer); der gemeldete Netzbezug war in diesem Projekt der Gebäudestrombedarf der
Bedarfsseite, keine elektrische Erzeugung. (2) Die Simulationsseite rechnete, speicherte das
Ergebnis aber nicht — Speichern war ein eigener Knopf neben „Simulation starten“; die
Übersicht dagegen rechnet und speichert in einem Zug, beide Wege rechnen identisch. (3) Die
Veraltungsprüfung der Wirtschaftlichkeit übersprang Zeilen mit Fehlgrund, ein veraltetes
Simulationsergebnis blieb deshalb unsichtbar; das Änderungsdatum des Projekts wurde bei
Wärmepumpe, BHKW, Strom- und Pufferspeicher gar nicht gesetzt.

**Anwenderentscheid 22.09.2026.** „Falls es einen Bedarf Strom gibt und keinen Erzeuger mit
Zuordnung Strombedarf, gebe nur eine Warnung aus (Strombedarf ohne Verwendung) und bestimme
die Energiekosten ohne Stromkosten. Energiekosten (Strom, Gas, …) sollen nur anfallen, falls
sie auch Verwendung finden.“

**Teil a — Energiekosten ohne Verwendung (Commit `2398ca99`).** `KostenEmissionRechner` weist
bei Netzbezug ohne stromverwendenden Erzeuger die Energiekosten nur noch als Brennstoffkosten
aus, mit dem Hinweis `WIRT_HINWEIS_STROMBEDARF_OHNE_VERWENDUNG` als Warnband statt als
Fehlgrund; `KohaerenzPruefung` folgt derselben Regel. `ProjektEnergietraegerCtrl.BrauchtStromTraeger`
gilt zusätzlich wahr bei Brenneranlagen mit `Hilfsenergie_Anteil > 0` (nicht beim reinen
Strombedarf), `WizardCtrl.BrauchtStromTraeger` nutzt dieselbe Kernfassung.

**Teil b — Simulationslauf speichert automatisch (Commit `23c7bbbc`).** `SimulationErgebnisHuelle`
legt nach einem gültigen Lauf das Ergebnis automatisch über `ErgebnisSpeichern` ab, die Seite
meldet den Erfolg, der Knopf „Ergebnis speichern“ bleibt bedienbar; Startseiten-Reiter und iOS
laufen über denselben Dienst.

**Teil c — Veraltung sichtbar, Änderungsdatum vollständig (Commit `9354c1ea`).**
`WirtschaftlichkeitCtrl.ErgebnisAktuell` prüft nicht mehr `Fehlgrund == null`, damit ein
veraltetes Ergebnis mit Fehlgrund nicht mehr als aktuell gilt; neues Warnband
`WIRT_BAND_SIMULATION_VERALTET` und die Fahne `VarianteZeile.Veraltet` erscheinen in
Wirtschaftlichkeit, Bericht und Übersicht. `MerkmalUebernahmeCtrl.MarkiereProjektGeaendert`
setzt das Änderungsdatum jetzt in `WErzeugerCtrl.Insert/Update/Delete` und in 19
`Add_/Del_`-Wegen von `WizardCtrl` — vorher fehlte es bei Wärmepumpe, BHKW, Strom- und
Pufferspeicher; die bisherigen Kachelaufrufe in `StartseiteHuelle` entfallen (−48 Zeilen).

**Dateien.** `EPOS.Kern/Controller/{KostenEmissionRechner,KohaerenzPruefung,
ProjektEnergietraegerCtrl,WirtschaftlichkeitCtrl,MerkmalUebernahmeCtrl,WErzeugerCtrl,
WizardCtrl}.cs`, die Simulationsergebnis-Hülle, `WindowsFormsApplication1/.../StartseiteHuelle.cs`,
Ressourcen (resx beide Sprachen + Designer), zugehörige Testklassen in `EPOS.Kern.Tests` und
`EPOS.UI.Tests`.

**Prüfung.** Kern-Filter 0 Fehler; Tests KiKern 524, SpeicherEngine 386, SpeicherPlanung 27
(1 übersprungen), `EPOS.UI.Tests` 5218, `EPOS.Kern.Tests` 4314 — 0 Fehlschläge; 18 neue
Testfälle, 10 davon vor der Änderung rot belegt; Windows-Schale 0 Fehler; SqlDialektPrüfer
1567 Texte, 0 Fundstellen; kein Rechenweg der Simulation berührt, kein Referenzlauf nötig.

**Was offen bleibt.** (1) Emissionen und Stromkosten bei Strombedarf ohne
stromverwendenden Erzeuger sind mit dem Nachtrag vom 22.09.2026 (Commit `ae1bb3c8`)
erledigt (siehe unten): Beide sind null, unabhängig davon, ob ein Träger zugeordnet oder
ein Katalogpreis vorhanden ist. (2) Die Kennzahl „Stromkosten Netzbezug“ zeigt bei
Strombedarf ohne Verwendung „—“ statt 0 — bewusst, damit kein Tarif den Strom
nachträglich bepreist; Anwenderentscheid offen, ob 0 stehen soll. (3) Neue
Schreibwirkung: Beim Speichern einer Anlage ordnet `WErzeugerCtrl` jetzt auch bei BHKW,
Elektrokessel und Hilfsenergie automatisch einen Stromträger zu. (4) Scheitert das Lesen
der Anlagen, gilt der Strom still als „ohne Verwendung“. (5) Der Wegfall der Tarif- und
Rollenmeldung ohne Verwendung ist nicht durch einen Test gedeckt. (6) `ErgebnisAktuell`
wirkt auch auf vier weitere Aufrufer (Bausteine Wirtschaftlichkeit, Excel-Bericht,
Verlauf, KI-Aktionen): Fehlgrund-Zeilen mit passender Lauf-Id gelten dort jetzt
ebenfalls als aktuell. (7) Der Stromzweig von `ProfilBedarf` sucht Stromverbraucher
weiterhin nur über den Bezeichner ohne Projektfilter (offener Punkt K1‑O1) — der
Kopfsatz eines fremden Projekts kann in die Summe geraten; eigener Auftrag.

**Nachtrag 22.09.2026, Anwenderentscheide (Commit `ae1bb3c8`).** Antwort auf die zwei
offenen Punkte aus „Was offen bleibt“: „1. Kostenentscheid folgen und diesen Strom
ebenfalls auslassen. 2. Auch auf 0 setzen.“ Emissionen aus dem Netzbezug ohne
stromverwendenden Erzeuger sind damit null; Stromkosten sind null, unabhängig davon, ob
ein Träger zugeordnet oder ein Katalogpreis vorhanden ist.

**Umsetzung.** Eine Regelstelle
`ProjektEnergietraegerCtrl.StromOhneVerwendung(idProjekt, netzbezugMWh)` entscheidet
(Netzbezug > 0 und kein Erzeuger, der Strom verwendet). `KostenEmissionRechner` fragt
sie einmal ab: kein Strompreis geladen, keine Rückfallzeile „Stromträger aus dem
Katalog“, `StromkostenNetz` bleibt leer (Kennzahl zeigt „—“), Emissionen aus dem
Netzbezug null, keine Zeile „Netzbezug“ in den Energiekosten je Anlage; Tarifstruktur,
Rollenmodell und Stromsteuer-Entlastung nach § 9b rechnen ohne diesen Strom;
`WErzeugerCtrl.StromTraegerNachziehen` nutzt dieselbe Regel statt einer Kopie. Der
Hinweis `WIRT_HINWEIS_STROMBEDARF_OHNE_VERWENDUNG` endet jetzt mit „Energiekosten und
Emissionen sind ohne diesen Strom bestimmt“; die Meldung „kein Stromträger“ nennt
zusätzlich Elektrokessel, BHKW und Hilfsenergie. Geprüft ohne Änderung: Kohärenzprüfung,
`EndenergieAufloeser` (bepreist nur mit Verwendung), `EmissionsBilanzRechner` (nur
KWK-Gutschrift), Bericht, Excel und KI lesen über `KennzahlenKatalog`; SO₂, NOₓ und
Staub werden nirgends aus dem Netzbezug abgeleitet.

**BHKW zählt als Stromverwender.** Ohne diese Ergänzung hätte die neue Regel Projekt
1030 (Gaskessel, zwei Gas-BHKW, Puffer, Netzbezug 4 357,78 MWh/a, „Elektrische Energie“
0,25 €/kWh) falsch behandelt: 1 089 445 €/a Arbeitspreis plus 2 400 €/a Grundpreis und
2 440,36 t/a CO₂ wären auf null gefallen. `BrauchtStromTraeger` prüft jetzt auch
`ID_BHKW > 0` (Eigenverbrauch deckt den Bedarf; Reststrom, Eigenstrom und Einspeisung
brauchen einen Preis). Damit bekommen BHKW-Projekte ohne Stromträger Rückfallträger,
Automatik und das Angebot „Strom“ auf der Energieträger-Seite — vorher fehlte das dort
ganz. Betrifft in der Testdatenbank auch 1018 und 1031 (gespeicherter Netzbezug
negativ, keine Zahl ändert sich).

**Tests.** 16 neue Fälle (Emissionen ohne Verwendung; zugeordneter Träger 0,30 €/kWh: 50
€/a statt 4 886 €/a; Katalogpreis mit/ohne Zuordnung: 0; Regel selbst; Gegenproben
WP/PV/BHKW; 1030 Differenzen 1 089 445 €/a und 2 440,36 t/a; 1030 ohne Träger über
Rückfall 1 525 223 €/a; § 9b mit/ohne BHKW; Energieträger-Seite bietet BHKW-Projekt
Strom an). Vier bestehende Tests nachgezogen (`Co2StromtraegerRueckfallTests`,
`EnergietraegerHuelleTests`, `ProjektEnergietraegerCtrlTests`,
`EnergiekostenGrundTests`).

**Gate.** Kern-Filter 0 Fehler; `EPOS.Kern.Tests` 4330, `EPOS.UI.Tests` 5218, KiKern
524, SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen) — alle grün; Windows-Schale
0 Fehler; SqlDialektPrüfer 1567 Texte, 0 Fundstellen; kein Referenzlauf nötig (die Basis
exportiert nur Simulationsgrößen, der Simulationscode ist unberührt).

**Logbuch-Vorschlag** (Version 1.2.0.4):

> Seit 22.09.2026 gehen ein Strombedarf ohne stromverwendenden Erzeuger weder in die
> Energiekosten noch in die Emissionen ein, und ein BHKW gilt als Stromverwender. Seit
> 22.09.2026 speichert „Simulation starten“ das Ergebnis sofort, und die
> Wirtschaftlichkeit warnt, wenn das Simulationsergebnis älter als die letzte
> Projektänderung ist.

## #441 — Simulation: Solarthermie ohne Puffer, Bedarfskanal ohne Versorger, Ergebnis veraltet (23.09.2026)

Anwendermeldung 23.09.2026 an den Projekten „Test: Prozesswärme+ST", „Test:
Prozesswärme+ST+WP" und „Test: Wärmeganglinie+ST": Simulation ohne Solarergebnis;
Überschuss 28,88 MWh/a bei Modulleistung 0; Prozesswärme wird nicht gedeckt, auch
nicht mit Wärmepumpe; Diagramm mit konstanter Last. Frage: Kann Solarthermie ohne
Puffer Prozesswärme decken? Entscheid: möglich, nicht sinnvoll, Warnung. Commits
`5d00937b` (Warnungen und Hinweise), `898f110c` (Ergebnisansicht veraltet,
Änderungsdatum).

**Befund (Analyse auf einer Kopie der Anwenderdatenbank).** Keine Anlage dieser
Projekte hatte eine Senkenzeile; die Vorbelegung „Heizkreis/Beides" bedient nur
Heizung und Warmwasser, der Bedarf lag im Kanal Prozesswärme — kein Erzeuger durfte
ihn decken, das gesamte Solarpotenzial (28,88 MWh/a) wurde als Überschuss geführt; mit
Senke Prozesswärme deckt die Solarthermie 9,79 MWh/a (19,6 %), zwei Drittel werden
ohne Puffer verworfen; mit Wärmepumpe und Prozesssenke Rest 0. Die konstante Last im
Diagramm war ein alter Lauf (Projekt vorher mit Prozesswärme statt Ganglinie); die
Ergebnisansicht wurde bei Bedarfs- oder Senkenänderung nicht als veraltet markiert.
Kein Rechenfehler.

**Umsetzung (kein Eingriff in den Rechenweg).** (1) Weiches Warnkriterium
`SOLAR_DIREKT_OHNE_PUFFER` im Warnkatalog (Solarthermie mit Direktsenke Prozesswärme
ohne Puffersenke) an Senkendialog, Erzeugerkarte und Laufprotokoll;
Heizkreis-Gegenstück `SOLAR_HEIZKREIS_OHNE_PUFFER` vorbereitet, abgeschaltet
(Anwenderentscheid offen). (2) Prüfung „Bedarfskanal ohne Versorger"
(`Warnkriterien.KanaeleOhneVersorger`): Meldung im Lauf, Warnbanner der
Ergebnisübersicht, Warnknoten des unversorgten Abnehmers in der Hydraulikübersicht;
Protokollzeile zur fehlenden Senke in Anwendersprache; „Heizkreis (beides)" →
„Heizkreis (Heizung + Warmwasser)". (3) Solarthermie-Reiter: Hinweiszeile „Ertrag x
MWh/a ohne Abnehmer …", Etikett „Wärmeproduktion der Module". (4) Ergebnisansicht
veraltet, wenn das Projekt-Änderungsdatum jünger als der Lauf ist (Anlass „Bedarf,
Senken oder Anlagen wurden nach dem Lauf geändert"); Senken speichern, Pufferspeicher
anlegen/ändern/entfernen und Jahressummen von Prozesswärme, Brauchwasser,
Stromverbrauchern setzen das Änderungsdatum jetzt. Neue Schlüssel:
`SIMWARN_SOLAR_DIREKT_OHNE_PUFFER`, `SIMWARN_SOLAR_HEIZKREIS_OHNE_PUFFER`,
`SIMWARN_KANAL_OHNE_VERSORGER`, `SIMWARN_KANAL_OHNE_VERSORGER_OHNE_MENGE`,
`SIM_SCHEMA_WARNUNG_OHNE_VERSORGER`, `SIMERG_ZUSTAND_ANLASS_PROJEKT`,
`SIMERG_ST_HINWEIS_OHNE_ABNEHMER`; geändert `SIMENG_SENKENLISTE_LEER`,
`SIM_HEIZKREIS_BEIDES`, `BK_KOMP_HINW_SENKEN`, `SIMERG_LBL_GESAMTLEISTUNG_MODULE`.

**Prüfung.** Kern-Filter 0 Fehler; EPOS.Kern.Tests 4801, EPOS.UI.Tests 5283, KiKern
524, SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen) — 0 Fehlschläge (der
früher als fremd rot geführte Feinpunkt-Test ist grün); 25 neue Tests (20 Kern, 5
Oberfläche; 21 vorher rot belegt); Windows-Schale 0 Fehler; SQL-Prüfer 1647 Texte, 0
Fundstellen; Referenzlauf gegen `2026-09-22_R11_Bestandsbefunde`: 5 CI-Projekte und
alle 13 Basisprojekte PASS (3 882 737 Werte); Anwenderkopie 1065–1067 alter gegen
neuer Code PASS (587 117 Werte), neue Meldungen erscheinen (1065/1066: Senke fehlt,
Kanal Prozesswärme ohne Versorger, Hinweis im Solarreiter, Warnknoten; 1067: nur
„keine Senke zugeordnet").

**Was offen bleibt.** (1) Heizkreis-Kriterium (Solarthermie ohne Puffer auf dem
Heizkreis; in 1067 werden 86 % des Ertrags verworfen) aktivieren? Anwenderentscheid.
(2) Der Senkendialog nennt die Bedarfsart weiter „beides", die Senkenanzeige „Heizung
+ Warmwasser". (3) Warnbanner ohne KI-Erklärung (Einträge im KI-Meldungsregister
nötig). (4) Neue Anlagen bekommen generell keine Senkenzeile — Vorbelegung ist der
Normalfall; Entscheid, ob der Anlagendialog die Senke beim Anlegen verlangt. (5)
Nebenbefunde aus der Analyse: feste Speichertemperatur 50 °C im Kollektormodell; drei
zusammengesetzte SQL-Texte (`SimulationWaermebedarf.cs:268`,
`SimulationSolarthermie.cs:196/:219`); Wirtschaftlichkeitszeilen der Projekte
1066/1067 verweisen auf das Ergebnis von 1065 (vermutlich Duplizieren) — gesondert
prüfen.

**Logbuch-Vorschlag** (Version 1.2.0.4):

> Seit 23.09.2026 warnt die Simulation, wenn ein Bedarfskanal keinen Versorger hat
> oder Solarthermie ohne Pufferspeicher Prozesswärme decken soll, und die
> Ergebnisansicht meldet, wenn Bedarf, Senken oder Anlagen nach dem Lauf geändert
> wurden.

## #442 — Administrationsdialoge Stufe 1: ein Rollbereich, Spalten nach Rang (23.09.2026)

Anwenderzuruf 23.09.2026 „Starte Stufe 1" zum Konzept
`Konzept_Administrationsdialoge_Neuordnung_EPOS-Plan.md`: ein Rollbereich je
Spalte im `Katalograhmen`, Spalten mit Rang statt Querrollen, keine gefilterte
Spalte verschwindet, kein Katalogeditor „Bearbeiten…" neben dem bedienbaren
Stammblatt (AD-Q6). Commits `c8e5f775` (Rahmen, Spaltenränge, AD-Q6),
`d9ef80b8` (Proben mit den Messfällen 1 088 × 624 und 400 × 624).

**Befund (Katalogprobe, 28 Fälle N01–N14 vor Stufe 1).** 27 von 28 Fällen mit
Rollbereichen ineinander; bei 1 088 × 624 lag die Fußleiste zwar im Fenster,
der Rahmen schnitt Eingabeblock und Liste ab (Heizkessel: Rahmen 474 von
1 261 px, Liste 367 von 456 px, Eingabeblock 0 px sichtbar; die Wärmepumpe mit
einem dritten Rollbereich im Kennlinienblatt). Die Tabelle war 1 288 px breit
in einer 1 054 px breiten Hülle (Bezeichner 487 px, Hersteller 251 px), der
größte waagerechte Überlauf lag bei der Wärmepumpe (1 269 px bei 1 088,
1 957 px bei 400 px Fensterbreite); Zahlen standen linksbündig, weil das
eigene CSS von QuickGrid die Hausregel überstimmte.

**Umsetzung (kein Eingriff in den Rechenweg).** V1: `Katalograhmen` als
Flex-Spalte ohne eigenes Rollen — die Liste nimmt die verbleibende Höhe ohne
Deckelung, nur ihre Hülle rollt; der Eingabeblock ist höchstens 34 % des
Rahmens hoch, rollt eigenständig und ist oben durch eine Trennlinie
abgesetzt; der Dialog selbst rollt nur unter 22 rem Rahmenhöhe; eingebettet
nimmt er die Höhe der Überlagerung; bei der Wärmepumpe steht der
Kennlinienblock im Eingabeschlitz. V2: neue Aufzählung `Katalogspaltenrang`
und `Katalogspalte.Rang` in allen 14 Profilen der Verwaltungen
(`Katalogfilterprofil.cs`) und im Klimaprofil; der Baustein
`EPOS.UI/Bausteine/Spaltenraenge.cs` schätzt Spaltenbreiten aus ihrem Inhalt
und ordnet einer Leiter von 400 bis 2 400 px eine Stufe zu, `Katalogliste.razor`
vergibt die Klassen, Container-Queries blenden Spalten nach Breite aus; die
Bezeichnerspalte ist elastisch mit Auslassung („…") und Tooltip, andere
Textspalten reichen bis 14 rem, Zahlen stehen rechtsbündig; die Liste wird
nur zum Container, wenn ihr Profil Ränge trägt (Importe, Flotte und Gesetze
bleiben unverändert). V7: Eine Spalte mit gesetztem Filter oder Sortierung
weicht nie, die Suche bleibt über alle Spalten. AD-Q6: `KatalogBrowserDialog`
ohne „Bearbeiten…", stattdessen „Verwerfen" (neuer Schlüssel
`ADM_BTN_VERWERFEN`) neben „Speichern"; die Windows-Hülle
`KatalogBrowserHuelle` ist nachgezogen. Die Stromganglinie (A8) ist dabei
gleich auf den Rahmen gezogen worden; im Rahmen stehen jetzt A1 bis A8 (15
Menüpunkte), noch außerhalb: Gebäude (A9), Gebäudetypen (A10) und die
Lastspitzenkappung (A11) — sie brauchen erst ein Profil der `Katalogliste`
(V16, Stufe 5).

**Nachher (Proben).** Katalogprobe 45 Fälle grün: keine Rollbereiche
ineinander, 0 px waagerechter Überlauf bei 1 088 und bei 400 px, der Dialog
selbst rollt nicht, die Fußleiste bleibt im Fenster, das Zeilenmaß ist
53 px, der Bezeichner trägt einen Tooltip; beim Heizkessel bei 1 088 px:
Liste 209 px, Eingabe 160 px, alle sieben Spalten sichtbar; die Gegenproben
G1/G2 melden weiterhin, der benachbarte Heizkessel-Projektdialog zeigt
keinen Einbruch. Rasterprobe 13 Fälle grün (virtualisierte Liste mit 6 654
Zeilen im Dialog: Rollbehälter ist die Hülle, `ItemSize` 53, die
Beobachter-Rückrufe laufen nach dem Rollen). Node kommt aus dem
Playwright-NuGet-Paket, dokumentiert in `Proben/Rasterprobe/LIESMICH.md`.

**Prüfung.** Kern-Filter 0 Fehler; EPOS.Kern.Tests 4 832, EPOS.UI.Tests
5 317, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen) —
0 Fehlschläge; 28 neue Tests (`SpaltenraengeTests` 21, `KatalogBrowserDialogTests`
+5, `KatalograhmenTests`, `WaermepumpeStammDialogTests`), 10 bestehende
angepasst; Windows-Schale 0 Fehler; die Wachen Schließkreuz,
Überlagerungstitel und Knopfleisten grün. Kein Rechenweg berührt, kein
Referenzlauf nötig.

**Was offen bleibt.** (1) Bei 624 px Fensterhöhe bleibt die Liste klein
(etwa drei Zeilen bei 1 088 px, eine bei 400 px), weil Liste und
Eingabeblock die Höhe teilen — das löst erst V3 (Stammblatt neben der
Liste). (2) V10 (Schloss statt Spalten Auslieferung/Schreibschutz) und V15
(OK → Beenden) aus der Stufe-1-Liste des Konzepts sind nicht umgesetzt, sie
folgen in Stufe 2. (3) Mit AD-Q6 entfallen Umbenennen und „Speichern unter"
in der Verwaltung; „Duplizieren" kommt mit V13 (Stufe 3). Der Schlüssel
`KBROW_BTN_BEARBEITEN` ist verwaist (noch in einem Kern-Test genannt) und
wird mit dem nächsten Ressourcenaufräumen entfernt. (4) Die Projektdialoge
erben die Spaltenränge; der Heizkessel-Projektdialog rollt bei 400 px noch
um 91 px waagerecht, weil die Spalte „im Projekt verwendet" keinen Rang hat
— außerhalb des Geltungsbereichs dieses Konzepts. (5) Die Breitenschätzung
ist auf Segoe UI 13 px in Chromium kalibriert; WebView2 auf dem
Anwenderrechner ist nicht gemessen. (6) Ein kurzer Testlauf überschnitt sich
mit einem fremden Testprozess (Worktree `z1`) — das Ergebnis blieb grün, die
Regel dazu gilt künftig strikt.

**Logbuch-Vorschlag** (Version 1.2.0.4):

> Seit 23.09.2026 rollt in den Verwaltungsdialogen nur noch die Liste,
> Spalten passen sich der Fensterbreite an, und die Felder des gewählten
> Satzes werden ohne „Bearbeiten…" direkt geändert und gespeichert.

## #444 — Simulation: Senken beim Anlegen, Heizkreis-Kriterium, Duplizieren ohne Ergebnisverweise (23.09.2026)

Anwenderentscheide 23.09.2026 (aus „Nach #441"): Heizkreis-Kriterium
einschalten; Senke beim Anlegen aus dem Bedarf; Kopierweg der
Wirtschaftlichkeitszeilen prüfen und beheben; dazu Push ohne Rückfrage nach
grünem Gate. Commits `aa407bfd` (Senken beim Anlegen aus dem Bedarf,
Heizkreis-Kriterium aktiv), `b570394a` (Duplizieren kopiert keine
Ergebnisverweise, Schemaschritt 106), `05997e0a` (Testdatenbank auf Schemastand
106 nachgezogen), `7c078901` (Merge origin/ios_migration_september:
Gebäudemodell R12 in die Welle).

**Umsetzung.** (1) `SOLAR_HEIZKREIS_OHNE_PUFFER_AKTIV = true`; Text „{Anlage}:
Solarthermie ohne Pufferspeicher deckt Heizwärme nur zeitgleich; Ertrag über
dem Momentanbedarf wird verworfen. Empfehlung: Pufferspeicher."; in der
Testdatenbank melden nur 1026, 1028, 1029 (Solarthermie direkt am Heizkreis),
keines der 13 Referenzprojekte. (2) Neuer Kern-Baustein `Senkenvorbelegung`
(`EPOS.Kern/Allgemein/Simulation/`): Bedarf je Kanal aus Gebäude, Brauchwasser,
Prozesswärme und Ganglinie; Heizung/Warmwasser → Heizkreis (Heizung +
Warmwasser), Prozesswärme → zusätzlich oder allein Prozesswärme, kein Bedarf →
keine Zeile; gilt für Wärmepumpe, Solarthermie, Kessel (auch Elektrokessel) und
BHKW; alle Erzeugerdialoge und der Assistent schreiben über
`WizardCtrl.Add_WP_Waermeerzeuger` (löschen und neu anlegen) — nur wirklich
neue Anlagen (Typ und Bezeichner vorher nicht vorhanden) bekommen Zeilen,
bestehende Senken werden gerettet; der Assistent zieht nach dem Speichern nach
(`AssistentCtrl`). Die Anlagendialoge Wärmepumpe, Heizkessel, BHKW,
Solarkollektoren zeigen die Zeile „Senken: …" (Ressourcen `ANL_SENKEN_ZEILE`,
`ANL_SENKEN_VORBELEGUNG`, `ANL_SENKEN_BEIM_SPEICHERN`), aus dem Kern
formuliert. Kein Schemaschritt, Rechenweg unberührt. (3) Ursache:
`ProjektDuplizierenCtrl.ErmittleZieltabelle` lieferte für
`Tab_ErgebnisWirtschaftlichkeit.ID_Ergebnis` kein Ziel, die Spalte wurde
unversetzt kopiert — jede Kopie und Variante zeigte auf den Lauf des
Quellprojekts. Regel `ERGEBNISVERWEISE_LEEREN`: Kopie behält die Zeilen,
Verweis NULL, Wirtschaftlichkeit rechnet nach dem ersten Lauf neu.
Schemaschritt 106 (`WirtschaftlichkeitFremdverweis`, reines DML): jeder Verweis
ohne Lauf desselben Projekts wird NULL; Zielversion 106; Testdatenbank: 21
Zellen NULL (Zeilen 16/18/20 Projekt 1028, 21/23/25 1029, 189/191/193 1040,
194/196/198 1041 auf Lauf 167 von 1026; 213–218 1043 und 219–221 1044 auf Lauf
206 von 1042), sonst unverändert, integrity_check ok, foreign_key_check leer,
zweiter Lauf No-op; nach dem Merge von origin die dortige Datenbank (Stand 105,
Gebäudemodell) mit `Werkzeuge/Testdatenbankschema` erneut auf 106 gezogen
(dieselben 21 Zellen). Merge-Konflikt nur in der Testdatenbank.

**Prüfung (vor dem Merge, Basis R11).** Kern-Filter 0 Fehler; EPOS.Kern.Tests 4
851, EPOS.UI.Tests 5 324, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27 (1
übersprungen) — 0 Fehlschläge; 27 neue/geänderte Tests (14 vorher rot belegt);
Windows-Schale 0 Fehler; SQL-Prüfer 1 657 Texte, 0 Fundstellen; Referenzlauf
13/13 PASS (3 882 737 Werte). Gate auf dem gemergten Stand `7c078901`:
Kern-Filter 0 Fehler; EPOS.Kern.Tests 4 981, EPOS.UI.Tests 5 360, KiKern 524,
SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen) — 0 Fehlschläge;
Windows-Schale 0 Fehler; Referenzlauf gegen `2026-09-23_R12_Gebaeudemodell`:
alle 13 Basisprojekte PASS (4 250 839 Werte innerhalb der Toleranz).

**Was offen bleibt.** (1) Zwei gleichnamige Anlagen gleichen Typs teilen sich
den Senkenstand; eine im Dialog umbenannte Anlage gilt als neu (wie bisher).
(2) „Bedarf vorhanden" heißt „zugeordnet", nicht „gerechnete Menge". (3) Kopien
tragen weiter einen kopierten Simulationslauf in `Tab_Ergebnis` —
Anwenderentscheid, ob Ergebnistabellen künftig nicht mitkopiert oder der
Verweis auf den kopierten Lauf versetzt werden soll. (4) Komponenten-Übernahme
und Duplizieren kopieren die Senkenliste der Quelle, auch eine leere. (5) iOS
zeigt die Senkenzeile nicht (Hüllen nur in der Windows-Schale). (6) Ein
Referenzlauf startete, während ein fremder Testprozess lief (Ergebnis PASS; die
Regel dazu gilt künftig strikt).

**Logbuch-Vorschlag** (Version 1.2.0.4):

> Seit 23.09.2026 erhalten neue Wärmeerzeuger beim Anlegen ihre
> Wärmesenken aus dem Bedarf des Projekts, auch für Prozesswärme, und
> die Anlagendialoge zeigen sie. Seit 23.09.2026 wird Solarthermie
> ohne Pufferspeicher am Heizkreis als Hinweis gemeldet. Seit
> 23.09.2026 übernehmen Kopien und Varianten keine gespeicherten
> Wirtschaftlichkeitsergebnisse mehr als aktuell.

## #445 — Administrationsdialoge Stufe 2: Zeile ist Wahl, Tastatur, Schloss, Duplizieren statt Überschreiben (23.09.2026)

Anwenderzuruf „Stufe 2 der Administrationsdialoge umsetzen" mit Entscheid
AD-Q11 (Auslieferungssätze nie überschreiben, nur duplizieren) nach dem Konzept
`Konzept_Administrationsdialoge_Neuordnung_EPOS-Plan.md`. Commits `5767e273`
(Stufe 2 und AD-Q11), `e2fbb829` (Proben), `f6290028` (Merge in den
Arbeitszweig), `852c42ca` (Ressourcen repariert).

**Umsetzung.** V4: `Katalogliste` mit Schalter `ZeileIstWahl`, gesetzt in allen
acht Verwaltungen (A1–A8); Wahlspalte entfällt, jede Zelle ist Klickfläche,
Zeile 46 px (45 + 1 px Linie) als `ItemSize` und `--epos-rasterzeile`; gewählte
Zeile mit Fläche und linkem Balken (bei ausgeblendeter erster Spalte trägt die
Namensspalte den Balken); Zellen der gewählten Zeile tragen `aria-current`;
Strg-Klick markiert weiter für den Vergleich; Projektdialoge und Importe
behalten Wahlspalte und 53 px. V11: Liste ist Tabulatorhalt; ↑ ↓ Pos1 Ende
bewegen die Wahl, Enter und Leertaste tun nichts; `epos-katalogliste.js` hält
die Tasten vom Rollen ab und rollt die gewählte Zeile ins Bild (auch
virtualisiert); Esc wirkt wie das Kreuz; Gelungenes in der Statuszeile der
Fußleiste, Gescheitertes im Warnband, Rückfrage nur vor dem Löschen;
`KatalogBrowserDialog` und `ModulKatalogDialog` halten bei geänderten Feldern
Zeilenwechsel, Neu…, Duplizieren… und Beenden an („Speichern oder Verwerfen");
der Modulkatalog hat Änderungserkennung und „Verwerfen" bekommen. V10: Baustein
`Kennzeichen` (Schloss ohne Wort, Kurztext und `aria-label` „Auslieferungssatz
– nur lesen, Duplizieren erlaubt", bei Klimadaten/Zeitreihen/Bedarfsprofilen
ohne den Zusatz); Spalten „Auslieferung" (Bedarfsprofil) und „Schreibschutz"
(Klimadaten) entfallen; `ModulKatalogDialog` wertet `ReadOnly` jetzt aus. V15:
„OK" heißt „Beenden" (KatalogBrowser, Modulkatalog, Solar- und Stromganglinie),
Reihenfolge Speichern · Verwerfen · Füller/Statuszeile · Neu… · Duplizieren… ·
Löschen · Beenden; Kreuz und Esc = Beenden. AD-Q11: Auslieferungssatz nur
lesend, „Speichern" weich gesperrt mit Grund; die BHKW-Rückfrage „Trotzdem
überschreiben?" entfällt; Kern `Katalogkopie.Duplizieren` (alle Spalten außer
ID, Bezeichner, ReadOnly per `pragma_table_info`, `ReadOnly = 0`, Transaktion)
in allen acht Stamm-Controllern, Wärmepumpe mit Heiz- und Kühlkennlinien; Knopf
„Duplizieren…" fragt den Namen („Name (Kopie)"/„(Kopie 2)") und wählt die Kopie
(KatalogBrowser 4 Ausprägungen, Modulkatalog 3, Wärmepumpe). Auslieferungssätze
außerhalb der Gerätekataloge: Brauchwasserprofile 6 von 16 (Schreibweg lehnt
ab), Klimadaten 0, Wärmebedarf-Zeitreihen 3 von 4 (keine Felder) — dort kein
Duplizieren laut Konzept. Neue Schlüssel (12): ADM_KOPIE_NAME,
ADM_KOPIE_NAME_N, ADM_MSG_KOPIE_FEHLT, ADM_MSG_KOPIE_FEHLER, ADM_BTN_BEENDEN,
ADM_BTN_DUPLIZIEREN, ADM_MSG_DUPLIZIERT, ADM_SCHLOSS, ADM_SCHLOSS_DUPLIZIEREN,
ADM_SPEICHERN_GESPERRT, ADM_MSG_UNGESPEICHERT, ADM_LISTE_TASTEN.

**Prüfung (Worktree, Stand `f6290028`).** Kern-Filter 0 Fehler; EPOS.UI.Tests 5
346, EPOS.Kern.Tests 4 845, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27
(1 übersprungen) — 0 Fehlschläge; 13 neue Kern-Tests
(`KatalogduplizierenTests`), 17 bunit (`ZeileIstWahlTests`) plus Dialogfälle;
Wachen Schließkreuz, Überlagerungstitel, Knopfleisten grün; Windows-Schale 0
Fehler; SQL-Prüfer 1 653 Texte, 0 Fundstellen; Katalogprobe 45 Fälle und
Rasterprobe 13 Fälle ohne Verstoß (Zeile 46 px, kein Umbruch, 0 px Querlauf,
Schloss 16 × 16 in der Zelle, Tastatur wählt und rollt ins Bild, virtualisiert
Ende → Zeile 6 653).

**Nachtrag: Ressourcen repariert.** Die Vereinigung der resx-Konflikte im Merge
`f6290028` hatte ein verwaistes `</data>` und den Schlüssel ADM_LISTE_TASTEN
verloren; Commit `852c42ca` baut beide Sprachdateien neu aus dem Stand vor dem
Merge plus den zwölf Schlüsseln der Stufe 2 (XML geprüft, 7 935 Schlüssel je
Sprache), Designer neu erzeugt. **Gate auf `852c42ca`:** Kern-Filter 0 Fehler;
EPOS.Kern.Tests 4 995, EPOS.UI.Tests 5 389, KiKern 524, SpeicherEngine 386,
SpeicherPlanung 27 (1 übersprungen) — 0 Fehlschläge; Windows-Schale 0 Fehler.

**Was offen bleibt.** (1) „Beenden" schreibt offene Änderungen nicht mehr still
zurück, sondern hält an (Konzept Administrationsdialoge 3.3). (2) Der
Bedarfs-Projektdialog teilt das Profil der Verwaltung und zeigt statt der
Spalte „Auslieferung" ebenfalls das Schloss. (3) Die Wärmepumpenverwaltung hat
keine Änderungserkennung; Beenden hält dort nicht an. (4) Verwaist:
`KatalogBrowserWege.IstGeschuetzt` (nur noch BHKW-Projektdialog),
`KatalogBrowserProfil.ZeigtSchreibschutz`, Ressourcen ADM_SCHUTZ_FRAGE/TITEL,
KLIMA_SP_SCHREIBSCHUTZ, KFLT_SP_AUSLIEFERUNG, aus #442 KBROW_BTN_BEARBEITEN —
Aufräumen mit Stufe 3. (5) Heizkessel-Projektdialog bei 400 px weiter 91 px
Querlauf (Spalte „im Projekt" ohne Rang, außerhalb des Geltungsbereichs). (6)
Stufe 3: Leertaste und Kästchen der Mehrfachwahl (V6), Löschen eines
Auslieferungssatzes weich sperren (V13), Duplizieren für Bedarfsprofile,
Änderungserkennung Wärmepumpenverwaltung, Auswahlleiste und Stammblatt (V8,
V9).

**Logbuch-Vorschlag** (Version beim Anwender erfragen):

> Seit 23.09.2026 wählt in den Verwaltungsdialogen ein Klick auf die Zeile
> den Satz, die Pfeiltasten bewegen die Wahl, Esc schließt, und der Dialog
> endet mit „Beenden". Seit 23.09.2026 tragen Auslieferungssätze ein Schloss,
> werden nicht mehr überschrieben und lassen sich mit „Duplizieren…" als
> eigener Satz kopieren.

## #447 — Administrationsdialoge Stufe 3: Stammblatt neben der Liste, Auswahlleiste, Mehrfachwahl, Vergleich (23.09.2026)

Anwenderzuruf „fahre fort Stufe 3“ nach dem Konzept
`Konzept_Administrationsdialoge_Neuordnung_EPOS-Plan.md`. Commits `d0660247`
(Umsetzung), `d8660fde` (Katalogprobe), Merge `f3957840` (Kühlungswelle von
origin mit den Schemaschritten 108–110, keine Konflikte).

**Umsetzung.** Neue Bausteine `Auswahlleiste` (Handlungen als Daten, weich
gesperrt mit Grund, „Auswahl aufheben“ nur bei gesetzten Kästchen),
`Stammblatt` mit `Stammblattgruppe` (steckbare Gruppen Kenndaten, Kosten, Alle
Daten, dialogspezifisch Kennlinie/Wochenprofil), `Vergleichstabelle` (bei zwei
oder drei Sätzen im Blatt, ab vier in der breiten Überlagerung),
`Zeilenauswahl`; `Katalograhmen` mit zweiter Anordnung Stammblatt neben der
Liste ab 900 px Rahmenbreite (Container-Query), schmal als Blatt über der
Liste; `Katalogliste` mit Kästchenspalte (Leertaste, Strg-Klick, Kopfkästchen,
ohne Obergrenze). Abweichung vom Konzept: Die Auswahlleiste steht im breiten
Fenster über dem Stammblatt, nicht über der Liste, damit die Liste bei 1 088 ×
624 acht Zeilen behält (Suchzeile 44 px, Spaltenkopf 53 px); die Überschrift
über der Liste entfällt, bei der Wärmepumpe wird die Kopfzeile zum Kurztext am
Titel. Pilot Heizkessel: Stammblatt mit Kenndaten, Kosten
(`KatalogBrowserProfil.IstKostenfeld`) und Alle Daten, Fuß zählt geänderte
Felder; Löschen mehrerer Zeilen mit Rückfrage, die nennt, was stehen bleibt;
Auslieferungssatz weich gesperrt mit Text im Stammblattkopf; Fußleiste
Speichern · Verwerfen · Statuszeile · Neu… · Beenden, Duplizieren… und Löschen
in der Auswahlleiste. BHKW, Solarkollektoren, Pufferspeicher über dieselbe
Komponente; Wärmepumpe mit Gruppe Kennlinie („Kennliniendaten…“),
Änderungserkennung und „Verwerfen“, Löschen gesperrt bei Projektzuordnung mit
Projektnamen; PV-Module, Wechselrichter, Stromspeicher mit Profilgruppen unter
den Kenndaten (`ModulKatalogProfil.IstKostenfeld`); Bedarfsprofile mit Gruppe
Wochenprofil (Grafik…, Typ ändern…), Kenndaten mit „Ändern…“ und neu
`BedarfStammCtrl.Duplizieren`. Aufgeräumt: `KBROW_BTN_BEARBEITEN`,
`KLIMA_SP_SCHREIBSCHUTZ`, `KFLT_SP_AUSLIEFERUNG`,
`KatalogBrowserWege.IstGeschuetzt`, `KatalogBrowserProfil.ZeigtSchreibschutz`
(`ADM_SCHUTZ_FRAGE/TITEL` bleiben für den BHKW-Projektdialog). 35 neue
Schlüssel `ADM_AW_*`, `ADM_SB_*`, `ADM_VG_*`, `ADM_LOESCHEN_*`,
`ADM_MSG_GELOESCHT*`, `ADM_KAESTCHEN_ZEILE`.

**Messung (Katalogprobe, Vergleich gegen Prüfwirt aus HEAD).** Listenhülle 1
088 × 624 vorher 209 px (3 Zeilen), nachher 424 px (8 ganze Zeilen); 400 × 624
vorher 118/110 px, nachher 230/286 px; Stammblatt 380 × 370 px rechts; Querlauf
0, Vergleichstabelle 0; Kästchen verschieben die Liste nicht;
Heizkessel-Projektdialog unverändert (91 px bei 400 px wie vorher). Rasterprobe
13/13.

**Prüfung (vor dem Merge, Stand `d8660fde`).** Kern-Filter 0 Fehler;
EPOS.UI.Tests 5 450, EPOS.Kern.Tests 5 007, KiKern 524, SpeicherEngine 386,
SpeicherPlanung 27 (1 übersprungen) — 0 Fehlschläge; 27 Fälle nach dem Umbau
rot und angepasst; Windows-Schale 0 Fehler; SQL-Prüfer 0 Fundstellen.

**Gate auf dem Merge-Stand `f3957840`.** Kern-Filter 0 Fehler; EPOS.Kern.Tests
5 045, EPOS.UI.Tests 5 453, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27
(1 übersprungen) — 0 Fehlschläge; Windows-Schale 0 Fehler; Referenzlauf gegen
`2026-09-23_R12_Gebaeudemodell`: alle 13 Basisprojekte PASS (4 250 839 Werte).

**Was offen bleibt.** (1) Rest von Stufe 3: Klimadaten und die drei Zeitreihen
ohne Stammblatt (hängen am Einlesen als Überlagerung, Stufe 4); Schalter „nur
mit Kühlfunktion“ in der Wärmepumpenverwaltung, „Import…“ in der Fußleiste,
direkt bedienbare Bedarfsfelder, eigene Kostengruppe bei der Wärmepumpe,
Auslieferungssätze als Text statt gesperrter Felder. (2) `field-sizing:
content` kennt WebKit nicht — auf iOS kann ein Rollbereich im Rollbereich
entstehen. (3) Die Anlagen-Überlagerung der Wärmepumpe im Projektdialog bettet
die Verwaltung ein und zeigt jetzt deren Stammblatt-Anordnung. (4)
`BhkwDialog.razor:477` nennt in einem Kommentar das entfernte Glied. (5)
`Lokalisierung_Katalog.md`: prüfen, ob die 35 neuen Schlüssel Einträge
brauchen.

**Logbuch-Vorschlag** (Version wie #445, beim Anwender erfragen):

> Seit 23.09.2026 zeigen die Verwaltungsdialoge den gewählten Satz in einem
> Stammblatt neben der Liste; mehrere Sätze lassen sich ankreuzen, vergleichen,
> duplizieren oder löschen.

## #448 — Duplizieren und Varianten ohne Ergebnistabellen (23.09.2026)

Anwenderentscheid 23.09.2026: „Ergebnistabellen nicht mitkopieren — umsetzen“.
Merge `7eb189b3` (Worktree-Commit `a9efeb71`).

**Umsetzung.** `ProjektDuplizierenCtrl` bekommt eine Regelstelle
`IstErgebnisTabelle` (Namensanfang `Tab_Ergebnis` und Detailtabellen per
Fremdschlüssel; Feld `Spec.Ergebnis`); von 76 Tabellen des Kopierplans werden
18 Ergebnistabellen (`Tab_Ergebnis` mit 14 Detailtabellen einschließlich
`Tab_ErgebnisGebaeude`, Wirtschaftlichkeit, Sensitivität, Strommatrix) bei
Kopie und Variante nicht mehr kopiert, die 58 Eingabetabellen vollständig;
Export/Import nehmen Ergebnisse weiter mit; `ERGEBNISVERWEISE_LEEREN` (#444)
entfällt zugunsten eines allgemeinen Sicherheitsnetzes (Verweis einer
Eingabetabelle auf eine Ergebnistabelle wird in der Kopie leer). Kopie ohne
Lauf: Simulation „nicht gerechnet“, Wirtschaftlichkeit „Noch keine
Wirtschaftlichkeitsberechnung gespeichert“ (Berechnen rechnet den Lauf vor),
Bericht simuliert frisch, Übersicht markiert die Variante als fehlend und
rechnet sie mit „Simulation starten“ mit (rechnet Stamm und markierte Zeile).
Kein Schemaschritt, Bestand unverändert.

**Prüfung (Worktree).** EPOS.Kern.Tests 5 009, EPOS.UI.Tests 5 389, alle grün;
9 neue Fälle in `ErgebnisverweisKopieTests` (Kopie aus 1024/1018, Variante aus
1018: keine Ergebniszeilen, Eingabetabellen zählen wie die Quelle, Quelle
unverändert), P7 in `ProjektpflegeTests` angepasst; 10 Fälle vorher rot;
SQL-Prüfer 1 693 Texte, 0 Fundstellen.

**Was offen bleibt.** (1) Eine künftige Detailtabelle ohne Namensanfang, deren
erste Beziehung auf eine Eingabetabelle zeigt, gälte als Eingabe (das
Sicherheitsnetz leert nur den Verweis). (2) Der Text „— (fehlt) ⚠“ in
`BerichtsDatenSammler.VariantenStatus.SimStandText` steht fest im Code, nicht
in `MyResource`. (3) Nach #444 Punkt (c) ist damit erledigt.

**Logbuch-Vorschlag** (Version wie #445, beim Anwender erfragen):

> Seit 23.09.2026 übernehmen Projektkopien und neue Varianten keine
> Simulations- und Wirtschaftlichkeitsergebnisse mehr; sie werden nach dem
> Anlegen neu gerechnet.

## #449 — Administrationsdialoge Stufe 4: Klimadaten und Zeitreihen im Stammblatt, Einlesen als Überlagerung, Reste aus Stufe 3 (23.09.2026)

Anwenderzuruf „fahre fort Stufe 4“ nach dem Konzept
`Konzept_Administrationsdialoge_Neuordnung_EPOS-Plan.md`. Commits `5a46b0b1`
(Umsetzung), `b1569752` (Katalogprobe), Merge `df78272c` (von origin: Kühlung
KU1 Welle 2, keine Konflikte).

**Umsetzung.** Klimadaten-Verwaltung im Stammblatt: Kopf mit Kennzahlen
Jahresmittel, Tiefst-, Höchstwert; Gruppe „Jahresverlauf“ (Reiter
Temperatur/Sonnenwinkel, Bild vom Kern-Renderer, „groß…“ öffnet die breite
Überlagerung); Gruppe „Herkunft“ als Text (Quelle, Importdatum, Standort,
Länge, Breite, Vermerk); Fußleiste Statuszeile · Import… · Beenden; „Import…“
öffnet die Überlagerung „Klimadaten einlesen“ mit Kreuz beim Titel, nach dem
Einlesen ist der neue Satz gewählt; Löschen bei Auslieferungssätzen weich
gesperrt, Schloss ohne „Duplizieren erlaubt“; neu Kästchen, Vergleich (nur
Kennwerte) und Löschen mehrerer Sätze. Zeitreihen (Wärmebedarf extern,
Solarthermie-, Stromganglinie) im Stammblatt mit Gruppen „Ganglinie“
(Jahresverlauf, „groß…“) und „Herkunft“ (mit „Verwendet in“), Kennzahlen
Jahresarbeit, Spitze, Volllaststunden; Einlesen als Überlagerung hinter
„Import…“; kein Duplizieren; Löschsperre bei Projektverwendung mit Projektnamen
(`ZeitreihenKatalogCtrl.Projektverwendung`); Stromganglinie: Dateiwahl setzt
nur den Pfad, das Einlesen startet „Datei einlesen…“, Löschen durch
Projektzuordnung gesperrt. Reste aus Stufe 3: Schalter „nur mit Kühlfunktion“
in der Wärmepumpenverwaltung (Schlitz `Werkzeug` der Katalogliste, Filter
`AUSDRUCK_MIT_KUEHLUNG`); eigene Gruppe „Kosten“ im Wärmepumpen-Stammblatt;
Bedarfsprofile direkt bedienbar (Typ, Beschreibung, Monatswerte;
Speichern/Verwerfen, Rückhalt bei ungespeicherten Änderungen), „Ändern…“
entfernt; Auslieferungssätze als Text (Lesemodus der `Stammblattgruppe` mit
Baustein `Stammblattwerte`) in Gerätekatalogen, Modulkatalogen, Wärmepumpe und
Bedarfsprofilen. Neue Bausteine `Stammblattwerte`,
`Ganglinienblattgruppe`/`Ganglinienblatt`/`Ganglinienansicht`, Naht
`ZeitreihenAdminWege`. 20 neue Schlüssel (ADM_BTN_IMPORT, ADM_SB_*,
ADM_AW_LOESCHEN_*, ADM_LOESCHEN_BLEIBEN_VERWENDET, ADM_MSG_EINGELESEN,
KLIMA_IMPORT_TITEL, KLIMA_KZ_*, KLIMA_SB_VERMERK).

**Messung (Katalogprobe, 46 Fälle, Rasterprobe 13/13).** Klimadaten 1 088 ×
624: Liste 424 px (8 Zeilen), Stammblatt 380 × 370, Bild 358 × 122,
Import-Überlagerung 900 × 562 mit einem Kreuz; 400 × 624: Liste 286 px (5
Zeilen), Überlagerung 368 × 562; Zeitreihen Bild 358 × 199 / 346 × 193,
Import-Überlagerungen 900 × 292/272/272; Vergleich Klimadaten (drei gewählt)
358 × 535 im Blatt; überall 0 px Querlauf, Kreuz schließt jede Überlagerung.
Gegenprobe G1 auf neuer Probeseite `rahmen` (116 471 px² Überschneidung), Fall
K4 mit Behebung (Liste 372 px).

**Prüfung (vor dem Merge).** Kern-Filter 0 Fehler; EPOS.UI.Tests 5 500,
EPOS.Kern.Tests 5 101, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27 (1
übersprungen) — 0 Fehlschläge; 35 neue Tests, rund 60 umgestellt;
Windows-Schale 0 Fehler; SQL-Prüfer 1 715 Texte, 0 Fundstellen; Wachen grün.

**Gate auf dem Merge-Stand `df78272c`.** Kern-Filter 0 Fehler; EPOS.Kern.Tests
5 142, EPOS.UI.Tests 5 500, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27
(1 übersprungen) — 0 Fehlschläge; Windows-Schale 0 Fehler; Referenzlauf gegen
`2026-09-23_R12_Gebaeudemodell`: alle 13 Basisprojekte PASS (4 250 839 Werte).

**Was offen bleibt.** (1) Klimadaten und Zeitreihen lassen sich nicht
umbenennen (Klimaname ist Schlüssel, kein Speicherweg) — deshalb ohne
Speichern/Verwerfen. (2) „Originaldatei…“ fehlt, keine Spalte speichert den
Quellpfad; bei Wärmebedarf und Solar bleibt „Anzeigen“ in der
Import-Überlagerung. (3) Klimavergleich nur Kennwerte, keine überlagerten
Kurven. (4) „Import…“ als zweiter Weg in den Gerätekatalogen nicht umgesetzt.
(5) Diagrammbeschriftung im Stammblatt klein (Kern-Modelle 978 × 542
verkleinert), „groß…“ gleicht aus. (6) Schlitz `Eingabe` des `Katalograhmen`
nutzt kein Dialog mehr; unbenutzte Parameter `BtnAendernText`,
`GruppeListeText`. (7) `Proben/Rasterprobe/LIESMICH.md` trägt ein BOM
(Bestand).

**Logbuch-Vorschlag** (Version wie #447, beim Anwender erfragen):

> Seit 23.09.2026 zeigen auch Klimadaten und Zeitreihen den gewählten Satz im
> Stammblatt mit Jahresverlauf und Herkunft, das Einlesen läuft über „Import…“,
> und Bedarfsprofile werden direkt im Stammblatt geändert.

## #450 — Administrationsdialoge Stufe 5: Gebäude, Gebäudetypen, Lastspitzenkappung im Gerüst; „Import…“ in den Gerätekatalogen (23.09.2026)

Anwenderzuruf „fahre fort Stufe 5“ nach dem Konzept
`Konzept_Administrationsdialoge_Neuordnung_EPOS-Plan.md` (V16, Abschnitt 7
Stufe 5, Abschnitt 7.1 d). Commits `9cb41354` (Gebäude, Gebäudetypen,
Lastspitzenkappung), `7e7ecb9c` (Katalogprobe Stufe 5), `c938ed32`
(„Import…“ in den Gerätekatalogen); Merge `b3fa8658`.

**Umsetzung.** Neuer Dialog
`EPOS.UI/Dialoge/Bedarf/GebaeudeAdminDialog.razor` (Verwaltungsmodus aus
`GebaeudeDialog` ausgegliedert; Projekt und Assistent unverändert):
Katalogliste mit Profil `FuerGebaeude`, die vier Vorfilter als Trichter
(Name, Gebäudeart, Verwendung, Baujahr); Stammblatt mit Gruppen Kenndaten
(direkt bearbeitbar, Knopf „Gebäudetypen…“ als Überlagerung), Hülle (lesbar,
„Bearbeiten…“ öffnet den Katalogeditor) und Alle Daten; Auswahlleiste
Vergleichen, Duplizieren…, Löschen (gesperrt, solange ein Projekt das
Gebäude nutzt); Schloss, Fußleiste, KI-Anmeldung. `GebaeudetypDialog` neu:
Zeile ist Wahl, Schloss, Gruppe „Tagesprofil“ (Kurve als Klappliste mit
Diagramm, „Stundenwerte…“ als Überlagerung mit 24 Feldern, Titel und Kreuz),
Beschreibung direkt bearbeitbar mit Speichern/Verwerfen,
Neu…/Duplizieren…/Löschen (gesperrt, solange ein Gebäude den Typ nutzt).
`PeakShavingDialog` neu: Liste der Lastgänge (Quelle, Intervall,
Jahresmaximum), Stammblatt mit Gruppen Speicher, Schwelle, Kosten, darunter
„Berechnen“ und Ergebnis; „Lastgang aus Datei…“ als Überlagerung; kein
Schloss, keine Kästchen; Fußleiste Statuszeile · Beenden plus CSV-Export und
„In Variante übernehmen“. „Import…“ in den Gerätekatalogen: Baustein
`EPOS.UI/Dialoge/Import/ImportUeberlagerung.razor` (Überlagerung ohne
eigenen Kopf, Titel und Kreuz trägt der Importdialog) für KatalogBrowser,
Modulkatalog und Wärmepumpenverwaltung (BHKW hat keinen Herstellerimport);
nach dem Import liest die Liste neu, erster neuer Satz gewählt, Statuszeile
nennt die Zahl; Windows-Hüllen nur im eigenen Verwaltungsfenster
(`GebaeudeFenster.cs` neu). Neue Stilregel: Tabellen im Stammblatt brechen
um. 59 neue Schlüssel (KFLT_SP_*, ADM_BTN_NEU, ADM_MSG_IMPORTIERT, GEBA_*,
GTYP_*, PEAK_*).

**Messung (Katalogprobe, 60 Fälle, Rasterprobe 13/13, Zeilenmaß 46 px,
nirgends Querlauf).** Gebäude 1 088 × 624: Liste 426 px (8 Zeilen),
Stammblatt 380 × 370, Überlagerung „Gebäudetypen…“ 900 × 562 mit einem
Kreuz; 400 × 624: Liste 288 px (5 Zeilen); Gebäudetypen: 8 Zeilen,
„Stundenwerte…“ 900 × 507 mit 24 Feldern; Lastspitzenkappung:
Ergebnistabelle 356 px, 21 Zeilen, „Lastgang aus Datei…“ 900 × 224;
Import-Überlagerungen 1 044,5 × 586,5 bzw. 384 × 586,5, Fußleiste schmal
zweizeilig.

**Prüfung (vor dem Merge).** Kern-Filter 0 Fehler; EPOS.UI.Tests 5 556,
EPOS.Kern.Tests 5 174, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27 (1
übersprungen) — 0 Fehlschläge; neue Tests: `GebaeudeAdminDialogTests` (~20),
`GebaeudetypDialogTests` und `PeakShavingDialogTests` neu geschrieben,
`SonderlistenVerwaltungTests` 13, Import 9 bunit-Fälle; Windows-Schale 0
Fehler; SQL-Prüfer 1 724 Texte, 0 Fundstellen; Wachen grün
(Parametersatz-Wache: Aufruf `KatalogImportHuelle.Gaben` in `Importsatz()`
verlegt).

**Gate auf dem Merge-Stand `b3fa8658`.** Kern-Filter 0 Fehler;
EPOS.Kern.Tests 5 306, EPOS.UI.Tests 5 591, KiKern 524, SpeicherEngine 386,
SpeicherPlanung 27 (1 übersprungen) — 0 Fehlschläge; Windows-Schale 0
Fehler; Referenzlauf gegen `2026-09-23_R12_Gebaeudemodell`: alle 13
Basisprojekte PASS (4 250 839 Werte).

**Was offen bleibt.** (1) Gebäude: Hülle und Wohnfläche nur lesbar
(Bearbeiten im Katalogeditor); keine Gruppe Wärmebedarf (Verwaltung kennt
weder Projekt noch Klima); Löschsperre erkennt Nutzung am Namen
(`Z_ProjektGebaeude` ohne Katalogverweis). (2) Gebäudetypen: Klappliste aus
`TagVCtrl.Typen`, Löschsperre über Stamm-Gebäude neu, Kurvenwechsel bei
ungespeicherten Änderungen gesperrt. (3) Lastspitzenkappung: Parameter in
drei Gruppen; CSV-Export und „In Variante übernehmen“ in der Fußleiste —
Anwenderentscheid, ob das zu „Statuszeile · Beenden“ passt; Auswahlleiste
nur im schmalen Fenster, kein Vergleich. (4) Import: erster neuer Satz im
Fokus statt „neue Zeilen gewählt“; Stromspeicherimport im 860 px breiten
Modulkatalog-Fenster schmaler. (5) Die KI-Maske GEBAEUDE teilen sich
Projekt- und Verwaltungsdialog. Damit sind alle fünf Stufen des Konzepts
umgesetzt.

**Logbuch-Vorschlag** (Version wie #449, beim Anwender erfragen):

> Seit 23.09.2026 stehen auch Gebäude, Gebäudetypen und Lastspitzenkappung
> im einheitlichen Verwaltungsgerüst mit Liste und Stammblatt, und die
> Gerätekataloge haben „Import…“ in der Fußleiste.

**Merge mit KU1 Welle 4.** Nach dem Doku-Commit `780f47fe` wurde
`origin/ios_migration_september` (acht Commits Kühlung KU1 Welle 4, neue
Basis `2026-09-23_R13_Kuehlung`, Testdatenbank mit Referenzprojekt 1017
unter Kühlung) konfliktfrei als `b587b9c9` zusammengeführt; resx und
`Resource.Designer.cs` geprüft (unverändert, wiederholbar). Ein
Folgefehler zeigte sich dabei: `EPOS.Kern.Tests/KuehlungOberflaecheTests.cs`
rief `GebaeudeHuelle.Gaben` noch mit dem in Stufe 5 entfernten Parameter
`admin` auf — behoben in `2edc081e`. Gate danach: Kern-Filter 0 Fehler,
Tests Kern 5 308 / UI 5 591 / KiKern 524 / SpeicherEngine 386 /
SpeicherPlanung 27 (1 übersprungen) grün, Windows-Schale 0 Fehler,
Referenzlauf 13/13 PASS gegen R13 (4 145 687 Werte in Toleranz); Push von
`2edc081e`. Die Commits `b3fa8658` und `780f47fe` lagen zunächst
versehentlich auf `main` (Hauptbaum stand auf `main`); `ios_migration_september`
wurde per Fast-Forward darauf gezogen und `main` auf `origin/main`
(`591229e1`) zurückgesetzt.

## #457 — Energieträgerverwaltung: Arbeitspreis wahlweise in €/kWh, Preisbasis direkt am Feld (ET-D-4) (23.09.2026)

Auftrag des Anwenders wörtlich: „Der Arbeitspreis soll immer zusätzlich
in €/kWh wählbar sein (außer €/Mengeneinheit)." Commits `2cd92e16`
(Energieträger: Preisbasis €/kWh direkt am Arbeitspreis), `a71f2260`
(KI-Sicht Energieträger: Preisbasis über den Weg der Klappliste), `1421701c`
(Tests zu ET-D-4), `47a45cef` (Papiere zu ET-D-4); Merge `48717545`.

**Befund.** Die Klappliste „Preisbasis" (Mengeneinheit ↔ kWh, seit
#446 in `energy_project_settings.Preisbasis` gemerkt) existierte, lag
aber im standardmäßig zugeklappten Block D „Einheiten und Umrechnung"
(Entscheid ET‑D‑3). Gespeichert wird weiterhin je Mengeneinheit; kein
Schemaschritt, Rechenkern unverändert.

**Entscheide.** ET‑D‑4 (neu): Die Preisbasis steht künftig direkt am
Arbeitspreis im Block „Preis und Heizwert", nicht mehr verborgen im Block D
„Einheiten und Umrechnung"; die Anordnung aus ET‑D‑3 gilt als abgelöst.

**Umsetzung.** Klappliste steht im Block „Preis und Heizwert" direkt unter
dem Arbeitspreis („€/Nm³" / „€/kWh", ohne eigene Beschriftung,
Sprachausgabe „Einheit des Arbeitspreises"); bei nur einer Einheit (Strom,
Fernwärme, Sonstige) keine Liste; die Liste wird bei jedem Nachziehen neu
gebaut — ein erst im Dialog eingetragener Heizwert macht €/kWh sofort
wählbar, ohne Heizwert eine leise Hinweiszeile; fällt der Heizwert bei
gewählter €/kWh auf 0, geht die Wahl auf die Mengeneinheit zurück und
zeigt den zuletzt gespeicherten Preis; Formelzeile bei €/kWh „0,0700
€/kWh × 4,80 kWh/Nm³ = 0,3360 €/Nm³ (gespeichert je Nm³)",
`ETV_FORMEL_DIREKT_BASIS` entfernt; „Katalogwerte übernehmen" behält
die Preisbasis; KI-Sicht `EnergietraegerKiSicht` setzt die Preisbasis über
den Weg der Klappliste (vorher verschob sich der gespeicherte Preis um den
Faktor Hi). Neue Ressourcen `ETV_FORMEL_JE_KWH`, `ETV_PREISBASIS_ARIA`,
`ETV_PREISBASIS_OHNE_HEIZWERT` (beide Sprachen), `KI_DLG_ET_PREISBASIS_ERL`.

**Tests.** Voller Lauf 11 845 bestanden, 1 übersprungen, 0 rot (Kern 5 314,
UI 5 594, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27); Doku-Wachen
26 grün; Windows-Schale 0 Fehler; Referenzlauf 1030 PASS. Neue Tests:
`EnergietraegerHuelleTests` (Umschalten und Speichern je Nm³, nachgetragener
Heizwert, Heizwert auf 0, Katalogübernahme behält Preisbasis, KI ohne
Wertverschiebung), `EnergietraegerDialogTests` (Lage und aria-label, eine
Einheit samt Hinweis, Assistent über die Klappliste), Preiskarte de/en.

**Papiere.** Entscheidungsregister Wirtschaftlichkeit
(`Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md`):
ET‑D‑4 neu, ET‑D‑3 Anordnung als abgelöst vermerkt;
`Rechenweg/04_Energiekosten.md` Block A/D; konsolidiertes Konzept §
2.5 (drei Sätze berichtigt); Mockup `Dialog_Formel_Zahlenprobe.html`
zwei Stellen; Wiki-Quelle `Projekte/Wiki/Programm Dokumentation -
Kosten.wiki` (Anker `preisbasis`, `umrechnungsregeln`, `preis-je-kwh`,
`katalogwerte-uebernehmen`).

**Logbuch-Vorschlag** (Version wie #452, beim Anwender erfragen):

> Der Arbeitspreis eines Energieträgers lässt sich direkt am Feld wahlweise
> je Mengeneinheit oder in €/kWh eingeben.

**Was offen bleibt.** Katalogkontext merkt die Preisbasis bewusst nicht
(nur Eingabehilfe, im Entscheidungsregister vermerkt). Nebenbefund:
`EnergietraegerKiSicht.LeistungspreisMonatlich` setzt nur das Kartenfeld,
die Oberfläche schreibt den Modus sofort in den Katalog — über den
Assistenten wird er nie gespeichert (wandert in Welle #458, KI-Maskensteuerung
der übrigen Masken).

**Gate nach Merge auf `48717545`.** Kern-Filter 0 Fehler; Tests Kern
5 314, UI 5 594, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27 (1
übersprungen) — 0 Fehlschläge; Windows-Schale 0 Fehler; Referenzlauf gegen
`2026-09-23_R13_Kuehlung`: alle 13 Basisprojekte PASS (4 145 687 Werte in
Toleranz).

## #456 — KI-Assistent: Maskensteuerung für die Katalogbrowser-Verwaltungen und die Bedarfsverwaltung (KI‑D‑Q11) (23.09.2026)

Auftrag des Anwenders wörtlich: „KI Assistent: Parameter sollen gesetzt
werden können (wie auch schon in anderen Dialogen)." Anlass: In
„Administration Heizkessel" antwortete „setze die Vorlauftemperatur auf
55°C" mit „Aktion nicht ausgeführt: feld_setzen … Es ist keine
steuerbare Maske geöffnet. Steuerbar sind: … (44)"; Nachtrag des
Anwenders: „alle Masken außer den nicht sinnvoll steuerbaren sollen
steuerbar sein". Commits `60a11eec` (KI-Kern: Feldtafel, Satzwahl,
Schutzgrund, Absage je Ziel), `189db4a5` (Katalogbrowser: vier
Erzeugerverwaltungen für den Assistenten), `7fbef566` (Bedarfsverwaltung:
Typ, Beschreibung, Monatswerte setzbar), `44f0dd0e` (Papiere zu #456); Merge
`ee84fce5`.

**Befund.** `EPOS.UI/Dialoge/Erzeuger/KatalogBrowserDialog.razor`
(Administration Heizkessel/BHKW/Solarkollektoren/Pufferspeicher) meldete sich
nie bei der `KiMaskenbruecke` an (Ausnahme aus KI‑F5, als der Browser nur
las); `BedarfAdminDialog` war im Katalog veraltet (Typ/Beschreibung
`nurLesen`, Monatswerte fehlten). Die „(44)" der Absage ist der Rest der
Liste hinter den ersten 20 von 64 Masken. Zwei weitere Fehler beim Testen:
Satzwahl aus einem geschützten Auslieferungssatz heraus wäre mit
Schreibschutz auf Maskenebene unmöglich gewesen (neues Kennzeichen
`KiDialogFeld.Satzwahl`, Schutz je Feld); Absagen zeigten nur „Exception
has been thrown by the target of an invocation" (`KiMaskenanmeldung` gibt den
Grund des Dialogs jetzt weiter).

**Entscheide.** KI‑D‑Q11 (neu): Steuerbar ist jede Maske mit
Einstellwerten; Ausnahmen: reine Anzeigen, Verwaltungen ohne Einstellwerte,
Auslieferungssätze, Neu/Duplizieren/Löschen/Import/Export, Dateidialoge,
Rückfragen, Lizenz-/Schlüsseleingaben, der Assistent selbst; die
Gebäude-Verwaltung bleibt offen, solange ihre Hülle nur liest.

**Umsetzung.** Anmeldeart „Sichtklasse als Feldtafel" (`IKiFeldtafel`,
`KatalogBrowserKiSicht`; `KiMaskenanmeldung.Fuer` fragt erst per Reflection,
dann die Tafel); die Feldkarte erzeugt der Kern aus `KatalogBrowserProfil`
(`KiDialoge.ErzeugerVerwaltung`, keine zweite Feldliste); Haken
`KiMaskenhaken.Schreibschutzgrund` (die Absage nennt „Duplizieren…" oder
den Lesemodus); Katalog jetzt 68 Masken, neu `Form_Heizkessel_Admin`,
`Form_BHKWAdmin`, `Form_SolarKollektorenAdmin`, `Form_PufferSp_Admin` mit
Öffnungszielen in `KiMaskenziele`; iOS unverändert (die Wurzel lehnt
Katalogverwaltungen benannt ab, KI‑D‑Q10). Bedarfsverwaltung: Typ (Wahl),
Beschreibung und zwölf Monatswerte setzbar, Schutz/Prüfen/Speichern über
die Wege der Knöpfe; eine Absage ohne offene Maske nennt bei gleichem
Öffnungsziel die Verwaltung; Anleitungstext in beiden Sprachen
umgeschrieben; 18 neue Ressourcenschlüssel; Ausnahmeliste als Daten
`KiDialogAusnahmen.Alle` (`KiAusnahmegrund`, 9 erste Einträge, Wächter
folgt in #458).

**Tests.** Voller Lauf UI 5 622, Kern 5 311, KiKern 524, SpeicherEngine 386,
SpeicherPlanung 27 (1 übersprungen), 0 rot, Doku-Wachen eingeschlossen;
Kern-Filter und Windows-Schale 0 Fehler; kein Referenzlauf (kein Rechenweg).

**Papiere.** `Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`
(KI‑D‑Q11, Stufenzeile S5 „Folgewelle: übrige Masken nach Inventar");
`Konzept_KI-Assistent_Aufgabensteuerung.md` 11.7 als Ausnahmeliste;
`Konzept_Administrationsdialoge_Neuordnung_EPOS-Plan.md` 7.1 (e);
`EPOS.UI/CLAUDE.md` (Anmelderegel: Feldtafel, Satzwahl, Schutzgrund);
Wiki-Quelle `Projekte/Wiki/Programm Dokumentation - Hilfe-Assistent.wiki`
(Verwaltungen in der Aufzählung, Absatz zu gewähltem Satz und
„Duplizieren…").

**Logbuch-Vorschlag** (Version beim Anwender erfragen):

> Der Hilfe-Assistent setzt Werte auch in den Verwaltungen der
> Gerätekataloge und der Bedarfsprofile.

**Was offen bleibt.** Die Gebäude-Verwaltung, solange ihre Hülle nur liest.
Folgewelle #458 (Inventar: 5 Kandidaten, 4 Grenzfälle, 4 veraltete
Feldkarten, 7 Masken mit Zahlenfolgen, Wächter über `KiDialogAusnahmen`).
Ein Satzwechsel bei ungespeicherten Änderungen wird erst nach der
Bestätigung abgelehnt (Setzer statt Vorbedingung); Wiki-Upload
Hilfe-Assistent ausstehend.

**Gate nach Merge auf `ee84fce5`.** Kern-Filter 0 Fehler; Tests Kern 5 317,
UI 5 625, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27 (1
übersprungen) — 0 Fehlschläge; Windows-Schale 0 Fehler; Referenzlauf
gegen `2026-09-23_R13_Kuehlung`: alle 13 Basisprojekte PASS (4 145 687 Werte
in Toleranz).

## #458 Stufe 1 — Wächter für die Maskenabdeckung, Ausnahmeliste, benannte Absage, Nachzüge veralteter Feldkarten (24.09.2026)

Anlass: Anwenderentscheid KI‑D‑Q11 vom 23.09.2026 („alle Masken
außer den nicht sinnvoll steuerbaren sollen steuerbar sein"), nach
dem Inventar vom 23.09.2026 (151 Razor-Masken unter
`EPOS.UI/Dialoge/**` und `Seiten/**`, davon 79 angemeldet: 62
Dateien mit eigener Anmeldung, 18 Kind-Bausteine über ihren Wirt).
Commits `eac5e34d` (KI: Waechter Maskenabdeckung, Ausnahmeliste,
benannte Absage), `09e906a4` (KI: Simulation fuehrt Kuehlbetrieb
und die Werte je Anlage), `47ff167c` (KI: Gebaeudetyp,
WP-Extrapolation, Leistungspreismodus nachgezogen), `1d44cecc`
(KI: Kommentare berichtigt, Peak-Shaving-Quelle benannt),
`111dfd49` (Papiere zu #458 Stufe 1); Zweig
`worktree-agent-a0079ae45bc09d90a` von `ee84fce5`; Merge
`4f236bf8` (konfliktfrei, resx/Designer geprüft).

**Befund/Inventar.** Kandidaten für Stufe 2: Kennlinien-Editor,
Projektkopf, Startseite Klimaregion/Solarart,
Einstellungen-Teilmenge, „Alle Daten" der Erzeugermasken,
Anzeigeschalter. Kandidaten für Stufe 3: drei Zapfprofil-Dialoge
und die Rechenweg-Optionsgruppe, beide erst nach dem Z3-Merge der
Zapfprofil-Sitzung; dafür braucht es einen Rahmen für Zahlenfolgen
(TYPPROFIL 7×24, GEBAEUDETYP 24, TYPSTAMM 12, KOSTENPROFIL,
LEISTUNGSPREISREIHE, QUELLPROFIL, GEBAEUDE_KATALOG).

**Umsetzung.** Wächter
`EPOS.UI.Tests/Dialoge/Hilfe/KiMaskenabdeckungWacheTests.cs`: jede
Maske mit Eingabefeldern ist angemeldet, hängt an einem
anmeldenden Wirt oder steht mit Grund in `KiDialogAusnahmen.Alle`
(40 Einträge: Anzeige 13, Import 5, Aktion 4, Assistent 3,
Lizenz/Schlüssel 2, je 1 Export, Rückfrage, Werkzeug,
Anlegen/Entfernen, FeldDesWirts; 8 „Offen" mit Auftrag — fünf
„#458 Stufe 2", drei „#458 Stufe 3 nach Z3"); vier Einträge aus
#456 gestrichen, weil sie nicht mehr zutrafen (LizenzDialog,
Rueckfrage, WaermebedarfAdminDialog, SolarganglinieAdminDialog).
Eingabebilanz: die Zahl der Eingabestellen ist für 66 Dateien
festgeschrieben, Markup-Masken sind gegen ihren Katalog gehalten;
Altlasten mit Grund in `BewusstDraussen` (Form_WP
Filterschalter/Kennfeldwahl; Form_PV_Anlagenwerte
Auslegungstemperaturen, Hersteller/Gerät, Modul/Gerät je Strang;
„Alle Daten" der fünf Erzeuger-Projektmasken;
Typstamm-Monatswerte). Benannte Absage: aus einer ausgenommenen
Maske heraus sagt der Assistent „Diese Maske ist bewusst nicht
steuerbar: ⟨Grund⟩" bzw. „noch nicht steuerbar" (Erkennung über
den Hilfeschlüssel des Aufrufs; Ausnahmen ohne eigenen
Hilfeschlüssel bleiben bei der Maskenliste). Nachzüge: die
Simulation führt jetzt 46 statt 40 Felder (`kuehlbetrieb`, je
Karte `quellanlage`, `waermequelle`, `quelltemperatur_konstant`,
`wp_prioritaet`, `wp_betriebsmodus`, mit denselben Prüfungen und
Schreibwegen wie die Überlagerungen; eine offene Überlagerung
oder eine gesperrte Seite lehnt benannt ab); die
Gebäudetyp-Beschreibung ist setzbar (der Auslieferungstyp bleibt
geschützt); neue Sichtklasse `WaermepumpeAnlageKiSicht` mit
Extrapolationsschalter (23 Felder, Muster `PhotovoltaikKiSicht`);
der Betriebsmodus-Dialog ist Ausnahme `FeldDesWirts` (der Wert
läuft über `wp_betriebsmodus` der Simulation); die
Peak-Shaving-Kommentare sind berichtigt, „Datei" ohne eingelesene
Datei lehnt benannt ab; widersprüchliche Kommentare in
`KiDialoge.cs` und `KiMaskenanmeldung` sind bereinigt; der
Energieträger-Modus `leistungspreis_monatlich` geht jetzt über
`LeistungsModusGewechselt` und wird gespeichert (Nebenbefund aus
#457).

**Tests.** Voller Lauf im Worktree 11 928 bestanden, 1
übersprungen, 0 rot (EPOS.UI 5 664, EPOS.Kern 5 327, KiKern 524,
SpeicherEngine 386, SpeicherPlanung 27); Kern-Filter und
Windows-Schale 0 Fehler; kein Referenzlauf nötig (kein Rechenweg).
Neue Tests: `KiDialogAusnahmenTests`, `SimulationKonfigKiTests`
(12 Fälle), Fälle in `KiMaskenwegTests`, `KiSimulationMaskeTests`,
`GebaeudetypDialogTests`, `WaermepumpeAnlageDialogTests`,
`EnergietraegerDialogTests`, `PeakShavingDialogTests`.

**Papiere.** `Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`
(neuer Abschnitt 4 „Abdeckung" mit Regel, Stufenzeile #458/1,
KI‑D‑Q11 aktualisiert, Abschnitt 5 berichtigt);
`Konzept_KI-Assistent_Aufgabensteuerung.md` 11.7 (Gruppen, Regel,
Wächter); `EPOS.UI/CLAUDE.md` (Satz zur Wache); Wiki-Quelle
`Projekte/Wiki/Programm Dokumentation - Hilfe-Assistent.wiki`
(Absatz Simulation, Absatz zu bewusst nicht steuerbaren Masken;
Upload ausstehend).

**Logbuch-Vorschlag** (Version beim Anwender erfragen):

> Der Hilfe-Assistent setzt in der Simulation auch Kühlbetrieb,
> Wärmequelle und Betriebsmodus.

**Was offen bleibt.** Stufe 2 und Stufe 3 wie oben — die
Zahlenfolgen brauchen noch einen Rahmen, ebenso „Alle Daten" der
Erzeugermasken. Der Z3-Merge macht die Wache absichtlich rot und
erzwingt den Entscheid über die Zahlenfolgen. Zusammenführung mit
#459 steht aus (`GebaeudetypDialog.razor` eine Zeile,
resx-Zeilenenden). Wiki-Upload Hilfe-Assistent weiterhin
ausstehend.

**Gate nach Merge auf `4f236bf8`.** Kern-Filter 0 Fehler; Tests
Kern 5 327, UI 5 664, KiKern 524, SpeicherEngine 386,
SpeicherPlanung 27 (1 übersprungen) — 0 Fehlschläge; Windows-Schale
0 Fehler; Referenzlauf gegen `2026-09-23_R13_Kuehlung`: alle 13
Basisprojekte PASS (4 145 687 Werte in Toleranz).

**Nachzug im Hauptbaum.** Vor dem Merge von #459 lag im Hauptbaum
bereits der Merge `c897aab0` von `origin/ios_migration_september`
(KU2 Welle 2, Schema 114, E8b-Nachtrag; die `.resx` dreiseitig
vereinigt). Darauf folgte der Wächter-Nachzug `76e62117`:
`WirtschaftlichkeitSeite` meldet die zwei ValERI-Wahlen
`zahlungsreihen_stand` und `zahlungsreihen_szenario` jetzt als
Anzeigewahlen an die KI-Maskenbrücke, die Zählliste der Wachen
wächst dadurch auf neun Wahlen. Beide Commits sind kein Teil der
Welle #458, standen aber vor ihrem Merge im Hauptbaum und gehören
deshalb an dieser Stelle vermerkt.

## #459 — Administrationsdialoge: Auslieferungskennzeichen (Schloss) in den Verwaltungen umschaltbar, mit Rückfrage (AD-Q15) (24.09.2026)

Anlass: Auftrag des Anwenders wörtlich (23.09.2026, zur
„Administration Brauchwasser"): „1. Die Auslieferungssätze
sollten auch auf änderbar (vom Nutzer) gesetzt werden können (ohne
Schloss). 2. Es sollen Datensätze als Auslieferungssätze gesetzt
werden können." Nachtrag: „Beim Ändern eines
Auslieferungsdatensatzes sollte ein Hinweis erscheinen." Commits
`37d50246` (Kern: Auslieferungskennzeichen umschaltbar, AD-Q15),
`8ffc31c9` (Bausteine: Schloss setzen/aufheben in Auswahlleiste und
Stammblatt), `5b2eec73` (Verwaltungen A1-A3, A5), `0a35945e`
(Verwaltungen A4, A6-A10), `9b9c1864` (Auswahlleiste: breit zwei
Zeilen mit vier Handlungen, Katalogprobe), `07cded05` (Papiere und
Wiki zu AD-Q15); Zweig `worktree-agent-a91f9e845fae03873` von
`cd2ac9ec`; Merge `80672c0d` (Konflikte: Konzept 7.1 (e)+(f),
`GebaeudetypDialogTests.cs`, `KatalogBrowserDialogTests.cs` —
jeweils beide Seiten übernommen; resx auto, 8 713 Schlüssel,
Designer unverändert); Nachbesserung `23b50d29` (zwei beim
Zusammenführen verlorene Methodenklammern in
`GebaeudetypDialogTests.cs`/`KatalogBrowserDialogTests.cs`
nachgetragen).

**Befund.** Das Kennzeichen ist die Spalte `ReadOnly` der
`Tab_*_STAMM`-Kopftabellen; sie steuert Schloss und Lesemodus,
Speicher- und Löschsperre, das Duplizieren (Kopie ohne Schloss) und
den KI-Schreibschutz — der Gebäudetyp führt zusätzlich
`Veraenderbar`, die Tww-Kataloge `Status`. Programm-Update,
`Erstbereitstellung` und `SchemaMigration` fassen Katalogsätze nie
an, weder überschreibend noch nachsäend; die gegenteilige Aussage
stand im Importhinweis (`IMP_KONFLIKT_HINWEIS_READONLY`), im
`WPStammCtrl`-Kommentar, in `Brauchwasser.wiki` und in
`KONTEXT_Brauchwassertypen_VDI6002.md` — ein Rest aus der
Access-Zeit, falsch. Die Auslieferungsvorlage übernimmt mit der
Vorgabe „alle" jede Zeile 1:1 samt Kennzeichen. Das Lizenzkonzept
kennt keine Rolle und keinen Herstellermodus; einziges Gatter bleibt
der Lizenz-Lesemodus.

**Entscheide (AD-Q15, löst AD-Q11 ab).** Ein Kennzeichen, in beide
Richtungen umschaltbar über die Auswahlleiste (Mehrfachauswahl) in
allen zehn Verwaltungen, auch Zeitreihen und Klimadaten; die
Beschriftung folgt der Auswahl („Schloss aufheben…" bei
gesperrten Sätzen, sonst „Schloss setzen…"); eine Rückfrage in
beide Richtungen nennt die echten Folgen; ein Band `ADM_SB_ENTSPERRT`
im Stammblatt markiert die in dieser Sitzung entsperrten Sätze, ohne
Schemaschritt; der Gebäudetyp schaltet beide Spalten zusammen;
Tww-Kataloge und Tabellen ohne die Spalte lehnen benannt ab; das
Typ-Schloss der Brauchwasser- und Stromverbrauchertypen bleibt
eigenständig; Werte werden nie zurückgesetzt; kein Weg über den
KI-Assistenten.

**Umsetzung.** Kern-Klasse
`EPOS.Kern/Allgemein/Katalog/Auslieferungskennzeichen.cs` (`Setzen`
läuft in einer Transaktion, meldet je ID geändert oder
unverändert, mit Rollback bei fehlendem Satz; lehnt Tww, Kataloge
ohne Spalte, den Lizenz-Lesemodus und unbekannte Kataloge benannt
ab); `KatalogRegistry` um `SchlossGegenspalte` (Gebäudetyp:
`Veraenderbar`) und `SchlossAusStatus` (Tww) erweitert; ein Einzeiler
in acht Gerätecontrollern, der Wärmepumpe, dem Gebäude, der
Klimaregion, `BedarfStammCtrl` und `TagVCtrl`, für Zeitreihen
zentral in `ZeitreihenKatalogCtrl`. Neuer Baustein
`Schlossumschaltung` (Handlung, Rückfrage, Statuszeile, für alle
zehn Wirte gleich) und `Auswahlhandlung` mit `Kurztext` und
`Breitenvorlage` (der Knopf wird so breit wie seine längere
Beschriftung); `Schlosswege.Aus(...)` trägt den Lesemodus in
`EPOS.UI.Daten` über die Schreibnaht; das Stammblatt-Band sitzt
daneben. Die Gebäudetyp-Handlung greift nur bei echten
Katalogzeilen. Berichtigt: `IMP_KONFLIKT_HINWEIS_READONLY`,
`WP_STAMM_UEBERNAHME_MSG_READONLY`, `…_OHNE_KOPIE`. Die
Auswahlleiste steht in der Breite jetzt zweizeilig mit vier
Handlungen (erstes Wort höchstens 10rem breit, der Hinweis
„Kästchen: mehrere wählen" gekürzt, der volle Text im Kurztext).

**Rasterprobe** (Playwright, wie vor jeder Änderung an der
Auswahlleiste vorgeschrieben). Katalogprobe 60 Fälle, Rasterprobe 13
Fälle, die Liste springt nicht. Bei 1 088 × 624: die Auswahlleiste
94 px hoch in beiden Zuständen (Bedarfsprofile mit langem Löschtext
118/94 px). Bei 400 × 624: die Auswahlleiste 150 statt 100 px
(Klimadaten weiterhin 100 px), die Liste durchgehend 50 px niedriger.

**Tests.** Im Worktree Kern 5 346, UI 5 620, KiKern 524,
SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen), 0 rot;
Kern-Filter und Windows-Schale 0 Fehler; neue Tests
`SchlossumschaltungTests`/`AuswahlleisteTests` mit 32 Fällen auf
Arbeitskopien, dazu je Wirt ein bunit-Fall. Testdatenbank
unverändert; SqlDialektPruefer ohne Fundstelle.

**Papiere.** Konzept Administrationsdialoge (AD-Q15 neu, AD-Q11
abgelöst; Stand, 3.3, 3.4, V8, V13, 6.2, 7.1 (f));
`EPOS.UI/CLAUDE.md` (die Regel „nie durch Überschreiben"
angepasst, neue Regel zur Schlossumschaltung); Konzept Knopfleisten;
Setup-Konzept 6.1; `KONTEXT_Brauchwassertypen_VDI6002.md`;
Wiki-Quellen `Programm Dokumentation - Gerätekataloge.wiki` und `…
- Klimadaten.wiki` (Spalte „Schreibschutz" entfernt),
`EPOS.Kern/Allgemein/Hilfe/Berechnung/Brauchwasser.wiki` (die
Update-Aussage berichtigt). Upload ausstehend.

**Logbuch-Vorschlag** (Version beim Anwender erfragen):

> In der Administration lässt sich das Schloss eines
> Auslieferungssatzes aufheben und wieder setzen.

**Was offen bleibt.** Das Band „Schloss aufgehoben" im Stammblatt
kennt nur der offene Dialog — die Datenbank führt allein das
Kennzeichen, ein Neustart oder ein zweiter Anwender sieht den Hinweis
nicht mehr. Die vierte Handlung in der Auswahlleiste kostet Platz:
schmal 50 px weniger Liste, breit ein Nachrücken beim
Stromverbraucher-Katalog. Wiki-Upload (Gerätekataloge, Klimadaten,
Brauchwasser) ausstehend.

**Gate nach Merge auf `23b50d29`.** Kern-Filter 0 Fehler; Tests Kern
5 448, UI 5 705, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27
(1 übersprungen), 0 rot; Windows-Schale 0 Fehler; Referenzlauf gegen
`2026-09-23_R13_Kuehlung`: alle 13 Basisprojekte PASS (4 145 687
Werte in Toleranz).

## #458 Stufe 2 — KI-Assistent: die übrigen Masken mit Einstellwerten angemeldet (24.09.2026)

Anlass: Entscheide der Hauptsitzung nach KI‑D‑Q11, aufbauend auf den
Kandidaten aus #458 Stufe 1 (Kennlinien-Editor, Projektkopf,
Startseite Klimaregion/Solarart, Einstellungen-Teilmenge, „Alle Daten"
der Erzeugermasken, Anzeigeschalter). Commits `24643eda`
(Kennlinieneditor), `bf82be6c` (Projektkopf und Startseite),
`0af5116e` (Programmeinstellungen, Farben als Feldtafel), `9d681d33`
(„Alle Daten" der sechs Erzeugermasken), `8b6daa99` (Anzeigeschalter
der Ergebnisreiter und des Verlaufs), `8e78d31d` (Papiere); Zweig
`worktree-agent-a0506527f9e4cf95f` von `4f236bf8`; Merge `ab580309`
(Konflikte an der Wirtschaftlichkeitsseite: `KiDialoge.cs`,
`WirtschaftlichkeitSeiteKiSicht.cs`, `WirtschaftlichkeitSeite.razor` —
jeweils beide Seiten übernommen, die Feldkarte trägt jetzt zehn
Felder: die ValERI-Wahlen aus `76e62117` plus Zeitraum und Haken des
Kapitalwertverlaufs; resx dreiseitig vereinigt, 8 757 Schlüssel,
Designer unverändert; die Wächter-Zählliste wächst auf 81 Einträge,
sechs Zahlenfolgen-Vermerke stehen jetzt auf „#458 Stufe 3
(Zahlenfolgen)").

**Umfang.** Kennlinien-Editor `KennlinienEditorDialog` (Maske
„Kenndaten", Sichtklasse `KennlinienKiSicht`): die Überlagerung meldet
die Vorlaufstufe als Wahlfeld, Temperatur, COP und Ptherm der Stufe
als Spalten, dazu die neue Vorlauftemperatur und die neue Stützstelle;
Schreibschutz `NurLesen`, kein Speicherweg, „OK" bleibt beim Anwender;
8 Felder. Projektkopf `ProjektKopfSeite` (Maske `Wizard_Projekt`): die
Sichtklasse `ProjektKopfKiSicht` löst die Reflection ab, weil die
Klimaregion als Id+Name über `KlimaGewaehlt` gesetzt wird; die Prüfung
ist die Kopfregel (Name leer oder vergeben, Klima fehlt); kein
Speicherweg, „Fertig" bleibt beim Anwender; 5 Felder. Startseite
`Startseite`+`ErzeugerReiter` (Maske `Form_Start`,
`StartseiteKiSicht`): Klimaregion und Solarart; Speichern läuft über
den Knopf neben der Klimaregion, ohne offenes Projekt bleibt die Sicht
schreibgeschützt; die Variantenwahl bleibt Navigation; die nackten
Radioknöpfe sind durch `Optionsgruppe` ersetzt, die Stilregel steht in
`epos-ui.css`. Einstellungen `EinstellungenDialog` (Maske
`Form_AdminSettings`, `EinstellungenKiSicht`): fünf Adressen, „Neue
Projekte mit Kühlung anlegen", die Diagrammfarben als Feldtafel je
Farbrolle (54 Rollen aus `Diagrammfarben.Gruppen`), der Farbwert nur
als `#RRGGBB`; Ordner, Datenbankname und der KI-Abschalter bleiben
draußen; Speichern läuft über den Weg von OK ohne Schließen; neues
Öffnungsziel `EINSTELLUNGEN` in `WinFormsNavigation`, iOS lehnt es
benannt ab. „Alle Daten" der sechs Erzeuger-Projektmasken: eine
Feldtafel über `ErzeugerProjektKiSicht`, `SolarkollektorenKiSicht` und
`PhotovoltaikKiSicht`; die Feldkarte kommt aus `KatalogBrowserProfil`
bzw. `ModulKatalogProfil`, die Namen tragen das Muster
`katalog_<schlüssel>`; Speichern läuft über den Knopf des Aufklappers;
ein zugeklappter Aufklapper, ein fehlender Speicherweg oder ein
Auslieferungssatz lehnen mit Grund ab. Anzeigeschalter der neun
Ergebnisreiter und des Kapitalwertverlaufs: keine eigene Maske — die
Schalter hängen an `Simulation` als Spalte `anzeige` samt
Reihenauswahl und Bedarfsart, `reiter` steht als Wahlfeld nur in
Schritt ③, über das Register `Ergebnisanzeige` und die Grundklasse
`Ergebnisblattwirt`; die Simulation wächst von 46 auf 47 Felder; die
Wirtschaftlichkeitsseite trägt zusätzlich Zeitraum und Haken des
Kapitalwertverlaufs.

**Wächter danach.** Katalog 72 Masken, 151 Komponenten, 66
Anmeldungen, 29 Wirte, 25 Ausnahmen (drei „Offen" = die
Zapfprofil-Masken, Stufe 3), 81 Dateien in der Eingabebilanz; die fünf
„#458 Stufe 2"-Ausnahmen, zehn Anzeige-Ausnahmen und fünf
`BewusstDraussen`-Einträge entfallen; die Markup-Probe und die
Eingabebilanz erkennen die Feldtafel; neue Profilwächter für „Alle
Daten" und für die Farbrollen.

**Tests.** Im Worktree Kern 5 327, UI 5 696, KiKern 524,
SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen), 0 rot;
Kern-Filter und Windows-Schale 0 Fehler. Nach dem Merge gefiltert: UI
1 283 + 767, Kern 337 grün.

**Papiere.** `Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`
(Stufenzeile #458/2, Abschnitt 4 „Stand der Abdeckung", die
KI‑D‑Q11-Zeile fortgeschrieben); Wiki-Quelle `Projekte/Wiki/Programm
Dokumentation - Hilfe-Assistent.wiki` (Absatz zum Ist-Zustand; Upload
ausstehend).

**Logbuch-Vorschlag** (Version beim Anwender erfragen):

> Der Hilfe-Assistent steuert auch Startseite, Projektkopf,
> Einstellungen, Kenndaten der Wärmepumpe und die Anzeige der
> Simulationsergebnisse.

**Was offen bleibt.** Stufe 3 — das Zapfprofil-Trio und der Rechenweg
nach dem Z3-Merge, dafür fehlt weiterhin ein Rahmen für die
Zahlenfolgen. iOS kennt das Öffnungsziel `EINSTELLUNGEN` nicht. Bei
der Photovoltaik bleiben in „Alle Daten" zwei Temperaturkoeffizienten
reine Lesewerte außerhalb des Profils. Die Puffer- und
Stromspeicher-Projektmasken haben jetzt einen Speicherweg, der nur den
Aufklapper trägt. Wiki-Upload (Hilfe-Assistent) weiterhin ausstehend.

**Gate nach Merge auf `ab580309`.** Kern-Filter 0 Fehler; Tests Kern
5 448, UI 5 737, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27
(1 übersprungen), 0 rot; Windows-Schale 0 Fehler; Referenzlauf gegen
`2026-09-23_R13_Kuehlung`: alle 13 Basisprojekte PASS (4 145 687
Werte in Toleranz).

## #458 Stufe 3a — KI-Assistent: Zapfprofil-Dialoge und Rechenweg Brauchwasser angemeldet (24.09.2026)

Anlass: Rest aus #458 Stufe 2 — das Zapfprofil-Trio und der
Rechenweg Brauchwasser nach dem Z3-Merge (#453). Commits (Zweig
`worktree-agent-ad741b786b99144de`, Basis `ea6f8608` = Z3):
`35f7ec6d` Rechenweg Brauchwasser der Bedarfsprofile setzbar;
`15a566fa` Zapfprofil und Auslegung angemeldet; `dd5a5a91`
Bedarfstag-Konstruktor angemeldet, keine Maske mehr offen;
`2e355650` Papiere. Merge in den Hauptbaum `6f2284ba`
(konfliktfrei); danach Nachzug origin `0d296ca0` (E9a #461, Schema
118, neue Testdatenbank) als `5c2e7b1d` (konfliktfrei; resx auto,
Designer unverändert).

**Umfang.** `ZapfprofilDialog.razor` (Maske `Form_Zapfprofil`,
Sichtklasse `ZapfprofilKiSicht`, 11 Felder, davon 5 Spalten mit
Zeilenkennzeichen Zonenname; neue Z3-Felder Rechenweg
deterministisch/stochastisch, Seed, Realisierungen);
`ZapfprofilAuslegungDialog.razor` (Maske `ZapfprofilAuslegung`,
`ZapfprofilAuslegungKiSicht`, 12 Felder inkl. „Stochastisch
rechnen", Perzentil P95/P99, Realisierungen);
`BedarfstagKonstruktor.razor` (Maske `BedarfstagKonstruktor`,
`BedarfstagKonstruktorKiSicht`, 8 Felder, davon 7 Spalten,
Kennzeichen „6–8 h · Verbraucher"); `BedarfsProfileDialog.razor`
(`Form_Prozesswaerme`, bestehende Sichtklasse, 9 Felder, neu
Wahlfeld `rechenweg` als Zapfprofil-Weiche; die sechs
`nurLesen`-Felder bleiben Anzeigen). Die drei neuen Dialoge sind
Überlagerungen: angemeldet nur solange offen, danach die aktive
Maske; Haken Auffrischen und Prüfen (dieselbe Prüfung wie am OK);
kein Speicherweg, `dialog_speichern` lehnt benannt ab; Setzen
nimmt die Wege der Handeingabe (Vorschau/Karten rechnen neu, ein
übernommener Auslegungspunkt wird überholt); benannte Absagen für
gerade nicht sichtbare Felder (Stufe Experte, Rechenweg
stochastisch, Anzahl nur mit Zapfregel, Volumen/Temperatur nur bei
„Volumen direkt") und für Gesperrtes (Stufe Erweitert, gesperrte
Nutzungsart, gesperrter Bedarfstag), Zahlen außerhalb der
Feldgrenzen werden abgewiesen; Öffnungsziel ist die Startseite,
Reiter Wärmebedarf. Zapfprofil und Auslegung teilen den
Hilfeschlüssel `Form_Zapfprofil.btn_Help`.

**Wächter danach.** Katalog 72 → 75 Masken, 152 Komponenten, 69
Anmeldungen, 29 Wirte, 22 Ausnahmen, keine mehr `Offen`, 84
Dateien in der Zählliste (neu `ZapfprofilDialog` 9,
`ZapfprofilAuslegungDialog` 11, `BedarfstagKonstruktor` 8 Felder),
Vermerk „Rechenweg offen" gestrichen; neue Methode
`KiDialogAusnahmen.AbsageFuer(KiAusnahme)` für den Test der „noch
nicht steuerbar"-Absage.

**Tests.** Im Worktree Kern 5 585, UI 5 783, KiKern 524,
SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen), 0 rot;
Kern-Filter und Windows-Schale 0 Fehler.

**Papiere.** `Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`
(Stufenzeile 3a, „Stand der Abdeckung": nur noch Zahlenfolgen
offen → Stufe 3b); Wiki-Quelle `Projekte/Wiki/Programm
Dokumentation - Hilfe-Assistent.wiki` (Zapfprofil mit Auslegung
und Konstruktor, Rechenweg Brauchwasser, Absage für nicht
bedienbare Felder; Upload ausstehend).

**Logbuch-Vorschlag** (Version beim Anwender erfragen):

> Der Hilfe-Assistent bedient auch das Brauchwasser-Zapfprofil
> samt Auslegung und Bedarfstag-Konstruktor.

**Was offen bleibt.** Die Rechenwege `Vorschau`/`Rechnen` sind
nicht als Haken gebaut — sie bräuchten eine eigene Aktion;
„Stochastisch rechnen" und „Bedarfstag konstruieren…" bleiben
Klicks. Spalten werden über die Zeilennummer gesetzt: eine
weggefallene Zeile wird still übersprungen — das betrifft alle
Spaltenmasken. Eine ungültige Handeingabe bleibt im Feld stehen,
auch wenn der Assistent einen gültigen Wert setzt — ebenfalls alle
Masken. Stufe 3b (Zahlenfolgen) läuft.

**Gate nach Merge auf `5c2e7b1d`.** Kern-Filter 0 Fehler; Tests
Kern 5 607, UI 5 783, KiKern 524, SpeicherEngine 386,
SpeicherPlanung 27 (1 übersprungen), 0 rot; Windows-Schale 0
Fehler; Referenzlauf gegen `2026-09-23_R13_Kuehlung`: alle 13
Basisprojekte PASS (4 145 687 Werte in Toleranz); Schemastand 118.

## #458 Stufe 3b — KI-Assistent: Zahlenfolgen als Zahlenreihen setzbar (Feldtyp Zahlenreihe, Aktion `reihe_setzen`) (24.09.2026)

Anlass: Rest aus #458 Stufe 3a — für die gleichartigen
Zahlenfolgen der sieben Masken fehlte weiterhin ein Rahmen. Commits
(Zweig `worktree-agent-aa90e37e0bef9284b`, Basis `ea6f8608` =
3a): `7b64c453` KI-Rahmen: Zahlenreihe als Feldtyp und Aktion
reihe_setzen; `97660220` KI-Masken: Zahlenfolgen der sieben
Masken als Zahlenreihen; `8da525ce` Papiere. Merge in den Hauptbaum
`84e743fc` (Konflikt nur im Konzept Dialogintegration: Stufentabelle
3a+3b, Abschnitt 4 „Stand der Abdeckung" zusammengeführt;
Zählisten geprüft; Wiki-Widerspruch berichtigt: Stundenwerte eines
Kostenprofils sind setzbar, nicht setzbar sind nur Zeitreihen aus
Dateien und Ladevorgänge; resx 8 934/8 935 Schlüssel, Designer
unverändert).

**Rahmenentscheid.** Tabellen mit benannten Zeilen bleiben im
Spaltenmodell (Ferien des Gebäudekatalogs: vier Zeiträume
mit Zeilenkennzeichen „Zeitraum", je Beginn/Ende mit Tag und
Monat). Gleichartige Zahlenfolgen (Monats-, Stunden-, Wochenwerte)
trägt das Spaltenmodell nicht (eine Wochenreihe wären 168 Felder,
`formular_ausfuellen` hat 2 000 Zeichen Grenze, `dialog_lesen` und
Bestätigung hätten 168 Zeilen; Stellen sind Listenplätze, keine
Zeilen eines Datenobjekts). Deshalb neuer Feldtyp Zahlenreihe
und neue Aktion `reihe_setzen` (`maske`, `feld`, `werte`
als Zahlenliste, optional `ab`: ganze Reihe oder Ausschnitt ab
Stelle); Länge und Grenzen prüft der Kern je Wert mit dem Namen
der Stelle in der Absage (Grenzen gelten jetzt auch für Einzel-
und Spaltenfelder); Bestätigung zeigt „alt → neu" gekürzt auf
zwölf Stellen plus Gesamtzahl, Protokoll trägt die volle Liste;
`feld_setzen`/`formular_ausfuellen` lehnen eine Reihe ab und nennen
den Weg, umgekehrt ebenso. Eine Wahrheit: Reihen hängen an den
Datenobjekten/Sichtklassen. Nebenbefund behoben: Vorbedingung von
`dialog_parameter_erklaeren` lehnte jedes Feld ab (Aktion konnte
nie antworten) — jetzt mit Test und Gegenprobe.

**Umfang je Maske.** TYPSTAMM `monatswerte` (12; 4 Felder);
TYPPROFIL `wochenwerte` (168; 4); GEBAEUDETYP `stundenwerte` (24,
gewählte Kurve im Arbeitsstand, Kurvenwechsel bis zum Speichern
gesperrt; 4); KOSTENPROFIL `monatswerte` + `wochenwerte` (5);
LEISTUNGSPREISREIHE `monatssaetze` (12, Grenzen 0–100 000;
4); QUELLPROFIL `monatswerte` (12, nur Betriebsart Monat; 5);
GEBAEUDE_KATALOG Ferien als vier Spalten + Randbedingung Bodenplatte
als Wahlfeld (59). Schreibwege = die der Masken (Speicher-Haken,
Schreibschutz bei Auslieferungssatz, Prüfung des Dialogs).

**Wächter danach.** Katalog 75 Masken (wie 3a), genau 7 Zahlenreihen
(neue Zählliste in `KiDialogkatalogTests`); Vermerke „Stufe 3
(Zahlenfolgen)" aufgelöst (drei entfallen, Gebäudetyp „Felder
von Neu…", Quellprofil „Anzeigeschalter und Zeitreihen");
`EINGABESTELLEN` 84 Einträge; `BewusstDraussen` ohne Typstamm;
Ausnahmeliste 22, keine `Offen`.

**Tests.** Im Worktree KiKern 542, UI 5 791, Kern 5 606,
SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen), 0
rot; neue Tests `KiZahlenreiheTests` (18), `KiReiheSetzenTests`
(18), Dialogtests je Maske, `KiFeldwerteTests`; Kern-Filter und
Windows-Schale 0 Fehler. Nach dem Merge gefiltert: UI 928, Kern 358,
KiKern 542, Doku-Wachen 26 grün.

**Papiere.** `Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`
(Stufenzeile 3b, Abschnitt 4 Punkt „Zahlenfolgen: Tabelle
oder Zahlenreihe", Stand der Abdeckung: alles abgedeckt);
`Konzept_KI-Assistent_Aufgabensteuerung.md` (`reihe_setzen` in 11.4,
neuer Abschnitt 11.4a Feldtypen); Wiki-Quelle `Projekte/Wiki/Programm
Dokumentation - Hilfe-Assistent.wiki` (Absatz mit neutralem Beispiel;
Upload ausstehend).

**Logbuch-Vorschlag** (Version beim Anwender erfragen):

> Der Hilfe-Assistent setzt auch Monats-, Stunden- und Wochenwerte
> als ganze Reihe.

**Was offen bleibt.** Bedarfsverwaltungen (#456) behalten ihre zwölf
Einzelfelder — andere Form als der Bedarfskopfsatz, im Konzept
vermerkt. Kostenprofil, Leistungspreisreihe und Quellprofil bleiben
ohne Speicherweg für den Assistenten (OK/„Übernehmen" klickt der
Anwender, die gesetzten Reihen gehen mit; Kommentare an den Haken
verweisen fälschlich auf `dialog_speichern`, Bestand). 365/8 760
Werte des Quellprofils bleiben Zeitreihen über den Dateiweg. Das
Konzept Dialogintegration trägt ein BOM (Bestand, Markdown-Regel
sagt ohne BOM — Aufräumpunkt).

**Gate nach Merge auf `84e743fc`.** Kern-Filter 0 Fehler; Tests Kern
5 628, UI 5 810, KiKern 542, SpeicherEngine 386, SpeicherPlanung
27 (1 übersprungen), 0 rot; Windows-Schale 0 Fehler; Referenzlauf
gegen `2026-09-23_R13_Kuehlung`: alle 13 Basisprojekte PASS (4 145
687 Werte in Toleranz); Schemastand 118.

## #466 — Administrationsdialoge: Import-Reste nach Stufe 5 — Auswahl nach dem Import, Fensterbreite des Stromspeicherimports (24.09.2026)

Zwei Restpunkte aus #450 (Konzept Administrationsdialoge Neuordnung, Abschnitt
7.1 (d)): „Import…“ wählte die neuen Sätze nicht, und der Stromspeicherimport
blieb im 860 px breiten Modulkatalog-Fenster schmaler als sein eigenes
Fenster. Commits (Zweig `worktree-agent-aff8742a9839b06de`, Basis `48d8836d`):
`d515b7fe` Import in den Gerätekatalogen: neue Sätze gewählt; `d614c3e7`
Modulkatalog öffnet so breit wie sein Import; `84eb0328` Papiere: Konzept 7.1
(d) erledigt, Wiki Gerätekataloge. Merge in den Hauptbaum `221a36f9`,
konfliktfrei.

**Befund und Umsetzung, Punkt 1 (Auswahl nach dem Import).** `ImportFertig`
setzte in den drei Wirten mit „Import…“ (`KatalogBrowserDialog`,
`ModulKatalogDialog`, `WaermepumpeStammDialog`) nur die Fokuszeile; Kästchen
der neuen Sätze blieben leer, alte Kästchen blieben stehen (A4, A6–A8 lesen je
Lauf genau einen Satz — nichts zu ändern). Neue Methode
`Zeilenauswahl.Uebernommen` (`EPOS.UI/Bausteine/Zeilenauswahl.cs`), von allen
drei Wirten gerufen: ab zwei neuen Sätzen alle angekreuzt, Auswahlleiste „n
gewählt“ (Vergleichen, Schloss, Löschen wirken auf genau diese), erster neuer
Satz Fokuszeile im Stammblatt, alte Kästchen fallen, ein laufender Vergleich
endet; ein einzelner neuer Satz bekommt kein Kästchen (Fokuszeile allein ist
die Wahl, Auswahlleiste nennt ihn beim Namen — wie beim Einlesen in A4/A6–A8).
Statuszeile: `ADM_MSG_IMPORTIERT` neu „{0} Sätze übernommen und gewählt.“/„{0}
records imported and selected.“

**Befund und Umsetzung, Punkt 2 (Fensterbreite des Stromspeicherimports).**
Ursache war die Fensterbreite der Ausprägung, nicht das CSS: alle Importe
tragen dieselbe Überlagerung `min(96vw, 1400px)`, nie breiter als ihr Fenster;
der Modulkatalog wünschte 860 × 780 für alle drei Ausprägungen, der
Stromspeicherimport als eigenes Fenster 1 180 × 700 (Konzept
Stromspeicherimport). Neue plattformfreie Regel `Fenstermass.MitUeberlagerung`
(`EPOS.UI/Dienste/Fenstermass.cs`): ein Fenster, das einen Import als
Überlagerung trägt, wünscht mindestens dessen Maß;
`KatalogImportHuelle.Wunschmass(art)` und `ModulImportHuelle.Wunschmass`
liefern die Maße, `ModulKatalogHuelle.Oeffnen` nimmt sie entgegen, die drei
Verwaltungshüllen reichen sie herein. Neue Wunschmaße: Stromspeicher 1 180 ×
780, PV-Module und Wechselrichter 1 240 × 800 (dieselbe Ursache, bewusst
mitgenommen).

**Messung.** Im Rasterprobe-Wirt bei 860 px war die Überlagerung 826 px breit,
Dialoghöhe in 733 px Überlagerung: Stromspeicher 818 px, PV-Module 1 137 px,
Wechselrichter 1 524 px, alle rollten senkrecht. Bei 1 180 × 780 ist die
Überlagerung 1 132,8 × 711 px, der Import passt ohne Rollen (677 px). Beim
Anwender (1 920 px bei 150 %) ändert sich nichts (85 % Breite = 1 088 CSS-px),
spürbar nur auf kleinen Schirmen (1 280 × 1 024: 1 177 statt 1 088 px).
Rasterprobe nicht gezogen — Katalogliste, Raster und die
`.epos-raster*`-Regeln sind unberührt, der Rasterprobe-Wirt diente nur als
Messbrücke.

**Tests.** Neue Fälle in `KaestchenTests` (je Wirt ein Fall mit mehreren neuen
Sätzen: Kästchen, Fokus, „n gewählt“, Stammblatt, Statuszeile; dazu je Wirt
ein Fall mit einem einzelnen Satz); zwei neue Fälle in `FenstermassTests`;
kein CSS geändert. Im Worktree Kern 5 700, UI 5 883, KiKern 542,
SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen), 0 rot; Kern-Filter
und Windows-Schale 0 Fehler; gefiltert 582 grün.

**Papiere.** `Konzept_Administrationsdialoge_Neuordnung_EPOS-Plan.md`
(Abschnitt 7.1 (d) erledigt, mit Ursache, Lösung und Messung, Beispiel der
Statuszeile in 3.3); Wiki-Quelle `Programm Dokumentation -
Gerätekataloge.wiki` (Satz zu „Import…“: neue Sätze gewählt; 0 Treffer der
Verbotsmuster; Upload ausstehend).

**Logbuch-Vorschlag** (Version beim Anwender erfragen; gehört zum Eintrag
„Import…“ aus #450, falls der noch nicht veröffentlicht ist):

> Nach „Import…“ in den Gerätekatalogen stehen alle übernommenen Sätze
> gewählt in der Liste.

**Was offen bleibt.** Die Liste rollt nach dem Import nicht von selbst zur
neuen Fokuszeile, nur bei Tastaturschritten — eine Änderung träfe die
`Katalogliste` samt Rasterprobe.

**Gate nach Merge auf `221a36f9`.** Kern-Filter 0 Fehler; Kern 5 700,
UI 5 883, KiKern 542, SpeicherEngine 386, SpeicherPlanung 27
(1 übersprungen), 0 rot; Windows-Schale 0 Fehler; Referenzlauf gegen
`2026-09-23_R13_Kuehlung`: alle 13 Basisprojekte PASS (4 145 687 Werte in
Toleranz), Schemastand 119.

## #465 — Gebäudeverwaltung (A9): Hülle, Wohnfläche und alle Gebäudedaten im
Stammblatt editierbar, Verwaltung für den Hilfe-Assistenten steuerbar
(24.09.2026)

**Anlass.** Konzept Administrationsdialoge, Abschnitt 7.1 (a) und (e): Hülle
und Wohnfläche standen im Stammblatt nur lesbar, bearbeitbar nur im
Katalogeditor; die Verwaltung war beim Hilfe-Assistenten unter der
Projektmaske `Form_Gebaeude` angemeldet und blieb offen, „solange ihre Hülle
nur liest“ (KI‑D‑Q11). Commits (Zweig `worktree-agent-a0e85f8966f6d1c0c`,
Basis `48d8836d`): `29c4ed45` Gebaeude: ein Arbeitsstand fuer Katalogeditor
und Stammblatt; `cd548c3c` Gebaeudeverwaltung: Stammblatt editierbar, eigene
KI-Maske; `e8e9ed4c` Papiere #465. Merge in den Hauptbaum `3dd0348a`,
konfliktfrei (resx 9 020 Schlüssel, Designer unverändert).

**Umsetzung.** Eine Wahrheit: Prüfung, Ableitungen und Hüllrechnung stehen
einmal in der neuen Klasse `GebaeudeArbeitsstand`; Katalogeditor
(`GebaeudeKatalogDialog`) und Stammblatt (`GebaeudeAdminDialog`) nutzen sie,
gespeichert wird über `GebaeudeKatalogHuelle.Schreiben`; der alte Schreibweg
für fünf Kenndaten ist entfernt. Im Stammblatt editierbar: Kenndaten (jetzt
mit Wohn-/Nutzfläche und Bauart), Hülle (acht Bauteile mit Kennwert und
Größe, Randbedingung der Bodenplatte samt Kellertemperatur, Zeile
H_T/H_ve/H_ges), Fenster nach Orientierung und Kenngrößen (Luftwechsel,
Fensterdurchlassgrad), aufklappbar „Alle Daten“ (Raumtemperaturen, Ferien,
Modellparameter, Rechenweg, Kühlung). Bedienung wie in den übrigen
Verwaltungen: Speichern/Verwerfen in der Fußleiste, „n Felder geändert“,
Statuszeile, Lesemodus, Schloss; verstößt ein Feld unter „Alle Daten“ gegen
eine Regel, klappt die Gruppe beim Speichern auf. Katalogeditor nur noch für
„Neu…“ (wie Heizkessel), „Bearbeiten…“ entfällt. Neue Stilregel
`.epos-gebaeude-huellraster` gegen Querrollen des Hüll-Rasters (im Browser
gemessen 1088 × 624 und 400 × 624: kein Querrollen, Fußleiste im Fenster).
Gruppe Wärmebedarf bewusst nicht: Der Kern rechnet den Wärmebedarf je
Projektzuordnung (Klimaregion, Kalender, Wohnflächen-/Verbrauchsangabe,
Kühlbetrieb) — ohne Projekt wäre ein zweiter Rechenweg mit erfundenen
Annahmen nötig; Begründung im Konzept 7.1 (a).

**KI-Maske.** Neuer Schlüssel `Form_Gebaeude_Admin` (Navigationsschlüssel
`Masken.GebaeudeAdmin` war bisher gleich `Form_Gebaeude`, umbenannt);
Anmeldung mit der Sichtklasse des Katalogeditors `GebaeudeKatalogKiSicht`
auf demselben Arbeitsstand; 59 Felder (Wahlfeld `satz` plus 58 aus derselben
Liste wie der Editor; Name nur lesbar, Betriebsart des Editors entfällt);
Haken Auffrischen, Schreibschutz (Absage beim Auslieferungssatz:
„duplizieren oder das Schloss aufheben“), Prüfen, Speichern über den Knopf;
Öffnungsziele: Verwaltung und Katalogeditor führen zur Verwaltung,
`Form_Gebaeude` zur Startseite. Maskenkatalog 76 (vorher 75);
Abdeckungswächter: neuer Wirt-Eintrag für das Stammblatt, Eingabestellen 7
in der Verwaltung und 36 im neuen Baustein; neuer Test: Feldliste der
Verwaltung = Feldliste des Editors; `GebaeudeAdminDialogTests` neu gefasst
(27 Tests).

**Löschsperre.** Bleibt beim Namen (weder `Z_ProjektGebaeude` noch
`Tab_Gebaeude` führen einen Katalogverweis). Vorschlag, nicht angelegt:
Schemaschritt 122 mit Spalte `Tab_Gebaeude.ID_Gebaeude_Stamm` (gefüllt beim
Übernehmen, einmalig über den Namen nachgetragen); die Löschsperre fragt
dann zuerst die ID statt des Namens.

**Tests.** Im Worktree voller Lauf 12 542 bestanden, 0 rot, 1 übersprungen;
Kern-Filter und Windows-Schale 0 Fehler; SQL-Dialekt-Prüfer 0; Referenzlauf
1030 und 1045 PASS gegen `2026-09-23_R13_Kuehlung`; Testdatenbank
unverändert.

**Papiere.** `Konzept_Administrationsdialoge_Neuordnung_EPOS-Plan.md`
(Stand, 3.5, 3.6, Stufe 5, 7.1 (a) und (e) abgeschlossen); Konzept
KI-Dialogintegration (Abdeckung, Schrittzeile); `EPOS.UI/CLAUDE.md` (zwei
neue Regeln: gemeinsamer Arbeitsstand von Stammblatt und Editor;
Eingabefelder in einer Stammblatt-Tabelle nehmen die Zellbreite);
Wiki-Quellen „Gebäudemodell VDI 6007“ und „Hilfe-Assistent“ nachgezogen
(Upload ausstehend).

**Logbuch-Vorschlag** (Version beim Anwender erfragen):

> Die Gebäudeverwaltung bearbeitet Hülle, Wohnfläche und alle übrigen
> Gebäudedaten direkt im Stammblatt.

**Was offen bleibt.** Die Wiki-Seite „Gebäude“ hat keine Quelle im Repo —
ihre Beschreibung („Bearbeiten…“) beim nächsten Upload anpassen; Speichern
schreibt jetzt den ganzen Satz über den Editorweg (leere
`spez_Waermeverbrauch`/`Waermebedarf` werden 0, leere Baualtersklasse „A“ —
wie der Editor schon immer); 5 der 277 Katalogsätze der Testdatenbank
verletzen Editor-Regeln (U-Wert „Sonstiges“/„Fenster“ außerhalb des
Bereichs) — der Anwender muss den Wert vor dem Speichern berichtigen; Feld
`verwaltung` der Projektmaske `Form_Gebaeude` meldet immer „nein“ (könnte
samt zwei Ressourcen entfallen); die Playwright-Katalogprobe lief nicht
(kein Node auf dem Rechner), Messung stattdessen im Browser über den
Probe-Wirt.

**Gate nach Merge auf `3dd0348a`.** Kern-Filter 0 Fehler; Kern 5 700,
UI 5 893, KiKern 542, SpeicherEngine 386, SpeicherPlanung 27
(1 übersprungen), 0 rot; Windows-Schale 0 Fehler; Referenzlauf gegen
`2026-09-23_R13_Kuehlung`: alle 13 Basisprojekte PASS (4 145 687 Werte in
Toleranz), Schemastand 119.

# Öffner erreichbar — Befund aller Masken (03.09.2026, Zahlen nachgezogen 04.09.2026)

Die **K6-Liste** für iU9: Welche der WinForms-Masken sind vom Einstieg der Anwendung aus
überhaupt noch zu erreichen — und welche nicht? Erzeugt mit

```bash
dotnet run --project Werkzeuge/Formularkarte -c Release -- \
    --alle WindowsFormsApplication1 --ziel dev/karten --erreichbarkeit
# schreibt neben UEBERSICHT.md auch ERREICHBARKEIT.md; diese Datei ist deren Inhalt
```

**Wozu.** Paket iU8-9 hat den ersten Blazor-Dialog an `Form_Kosten` gehängt, weil die Feldkarte
dort einen Aufrufer auswies. `Form_Kosten` selbst war aber seit KD6a ohne Einstieg: Die
Startseite nahm `btn_Kosten` in `BaueBerichteKostenSeite` mit `EntferneAltknopf` aus der Maske,
und der Designer meldete den Handler seither gar nicht mehr an. Die Karte nannte den Aufrufer,
nicht den Weg dorthin — genau diese Lücke schließt die Spalte „Öffner erreichbar" (Paket
iU8-12f, Entscheidungsregister § 2.8).

**Wie gelesen wird.** Knoten sind die Masken (Klassen mit `: Form`, `: UserControl`,
`: BaseForm`), Kanten sind „A öffnet B" aus `new B(…)`, `B.Zeigen(…)`, `ShowDialog`, `Show`
sowie `Dienste.Navigation.OeffneMaske(Masken.X)` — der Schlüssel wird über die Sprungtabelle in
`Dienste/WinFormsNavigation.cs` aufgelöst. Wurzeln sind `MDIMainForm` und `Form_Start`, dazu der
Programmeinsprung `Program.Main` (er zeigt den Erststart-Dialog, bevor es ein Fenster gibt).
Abgezogen werden die Wege, die es zur Laufzeit nicht mehr gibt. Regeln und Grenzen stehen in
[`LIESMICH.md`](LIESMICH.md), Abschnitt „Öffner erreichbar".

> **Nachgezogen mit iU9‑W14b** (04.09.2026): Die Tabellen unten stammen aus einem frischen
> Stapellauf. Der Befund zählt jetzt **28 Masken, davon 27 erreichbar** — Welle 12 nahm fünf
> (38), Welle 13 sechs (32): die vier VDI‑3805‑Einlesemasken (sie werden EINE Razor-Komponente
> mit vier Ausprägungen), die Wärmebedarfsverwaltung und den CEC‑Modulimport; Welle 14b vier
> weitere (28): die drei Bedarfs-Katalogverwaltungen (`Form_Brauchwasser_Admin`,
> `Form_Prozesswaerme_Admin`, `Form_Stromverbraucher_Admin` — sie werden EINE Razor-Komponente
> mit drei Ausprägungen) und `Form_Solarganglinie_Admin`. Die eine „unklar"-Maske bleibt
> `Form_PufferSp_Bearbeiten` (Welle 14a). Der erklärende Teil oben ist unverändert.

## Stand nach iU9-W0 (Anwenderentscheid iF29)

Der Befund vom Vormittag nannte **vier unerreichbare und eine verwaiste Maske**. Alle fünf sind
mit iU9-W0 **stillgelegt** statt umgestellt worden — dazu `Form_KwkgModule`, deren Knopf seit
B5b ausgeblendet war, und die zwei K4-Hüllen `Form_Wirtschaftlichkeit` und `Form_Bericht`, die
nur `Form_Variantentest` geöffnet hat:

| Maske | Zustand vorher | Was geschehen ist |
|---|---|---|
| `Form_Kosten` | nein | gelöscht; Nachfolge ist der Reiter „Berichte & Kosten" (`UcBkKosten`) mit `Form_KostenKomponente`. Die von außen genutzten Statics stehen als `KostenSummenCtrl` im Kern. |
| `Form_KostenfaktorItem` | nein | gelöscht (hing allein an `Form_Kosten.AddKostenItem`); der letzte Stand liegt als Prüfmuster unter `Werkzeuge/Formularkarte.Tests/Pruefmuster/Kosten/`. |
| `ucKostenItem` (Klasse `ucKostenZeile`) | nein | gelöscht (hing allein an `Form_Kosten.UpdateDetailPanel`). |
| `Form_Variantentest` | nein | gelöscht; die Variantenfunktion lebt in `UcBkUebersicht`. Mit ihr die beiden K4-Hüllen `Form_Wirtschaftlichkeit` und `Form_Bericht`. |
| `Form_Simulation_Kurz` | verwaist | gelöscht, mit ihr `Form_Simulation_Detail - Kopie.cs`, `ChartManagerNeu.cs` und die `Compile Remove`-Liste der `.csproj`. |
| `Form_KwkgModule` | ja (Knopf ausgeblendet) | gelöscht; alle Felder stehen im `BhkwWirtschaftlichkeitDialog`. |

**Damit steht der Bestand auf 0 × „nein" und 0 × „verwaist".**

## Was noch zu tun ist

| Maske | Zustand | Befund und Vorschlag |
|---|---|---|
| `Form_GebWohnflaeche` | unklar | Erreichbar über `Form_Gebaeude.btn_Aendern`, der aber im `m_bAdmin`-Zweig auf `Visible = false` gesetzt und dort nicht wieder eingeschaltet wird. Im Projektmodus ist er sichtbar — die Maske **bleibt und wird umgestellt** (Welle W9). |
| `Form_PufferSp_Bearbeiten` | unklar | Erreichbar über `Form_PufferSp_Admin.btn_Bearbeiten` / `btn_Neu`; beide werden im `m_bReadOnly`-Zweig gesperrt und dort nicht wieder eingeschaltet. Die Maske **bleibt und wird umgestellt** (Welle W14a). |

Die übrigen 89 Masken haben einen Weg von `MDIMainForm` bzw. `Form_Start`; er steht je Maske in
der Tabelle unten und im Kopf ihrer Feldkarte.

**Für die Wellenplanung iU9:** Eine Maske mit „nein" oder „verwaist" wird **nicht** nach Blazor
umgestellt — sie wird stillgelegt. Eine Maske mit „unklar" wird vor der Umstellung geklärt. Vor
jeder Welle ist der Stapellauf neu zu ziehen; jede stillgelegte und jede umgestellte Maske senkt
die Gesamtzahl.

## Stand nach iU9-W2 (Nachtrag 03.09.2026)

Welle 2 hat sechs Masken umgestellt und gelöscht — drei mit Designer, drei ohne (K4):

| Maske | Klasse | Nachfolge |
|---|---|---|
| `Form_StromspeicherItemNeu` | K1, lokalisiert, 28 Aufrufer | `EPOS.UI/Dialoge/Allgemein/NamensDialog.razor` über `NamensDialogHuelle.Bezeichner` |
| `Form_GebaeudetypNeu` | K1, lokalisiert | derselbe Dialog, mit zweitem Feld (`BezeichnerUndBeschreibung`) |
| `Form_AlsVariante` | K4 | derselbe Dialog, mit Hinweiszeile (`FragenMitHinweis`); der Ablauf steht als `Views/Varianten/AlsVarianteHuelle.cs` |
| `Form_Tarifstruktur` | K4 | `EPOS.UI/Dialoge/Wirtschaftlichkeit/TarifstrukturDialog.razor` |
| `Form_PhotovoltaikVerguetung` | K1 | `EPOS.UI/Dialoge/Wirtschaftlichkeit/PhotovoltaikVerguetungDialog.razor` |
| `Form_WirtschaftlichkeitParameter` | K4 | `EPOS.UI/Dialoge/Wirtschaftlichkeit/WirtschaftlichkeitParameterDialog.razor` |

Der Stapellauf zählt seither **102 Masken** (105 nach iU9‑W0), **0 × „nein"**, **0 × „verwaist"**,
unverändert **2 × „unklar"**. Die drei K4-Masken hatten nie eine Designer-Datei und sind in
dieser Zählung deshalb nie erschienen; sichtbar wird ihr Verschwinden nur in der
Erreichbarkeitstabelle unten.

## Stand nach iU9-W3 (Nachtrag 03.09.2026)

Welle 3 hat vier Masken umgestellt und gelöscht, alle vier mit Designer:

| Maske | Klasse | Nachfolge |
|---|---|---|
| `Form_LeistungspreisReihe` | K1 | `EPOS.UI/Dialoge/Kosten/LeistungspreisReiheDialog.razor` mit `LeistungspreisReiheHuelle` |
| `Form_SpotpreisImport` | K1 | `EPOS.UI/Dialoge/Kosten/SpotpreisImportDialog.razor` mit `SpotpreisImportHuelle` |
| `Form_Emissionskatalog` | K2 (zwei Raster) | `EPOS.UI/Dialoge/Kosten/EmissionskatalogDialog.razor` mit `EmissionskatalogHuelle` |
| `Form_Kostenprofil` | K3 (Chart + 36 Laufzeitfelder) | `EPOS.UI/Dialoge/Kosten/KostenprofilDialog.razor` mit `KostenprofilHuelle` |

Der Stapellauf zählte danach **98 Masken**.

## Stand nach iU9-W4 (Kostenverwaltung und Energieträgerkatalog)

Welle 4 hat die beiden **Hosts** der Kostenseite umgestellt und mit ihnen ihre fünf
Unterbausteine — sieben Designer-Masken auf einmal, die größte Löschung seit iU9‑W0:

| Maske | Klasse | Nachfolge |
|---|---|---|
| `Form_KostenKomponente` | K1 (Host, TabControl) | `EPOS.UI/Dialoge/Kosten/KostenKomponenteDialog.razor` mit `KostenKomponenteHuelle` |
| `ucVorlagenZeile` | K1 (uc, dynamisch ×n) | `EPOS.UI/Dialoge/Kosten/VorlagenZeile.razor` |
| `ucErtragBonus` | K1 (uc) | `EPOS.UI/Dialoge/Kosten/ErtragBonus.razor` mit `ErtragBonusGaben` |
| `Form_Energietraeger` | K1 (Host) | `EPOS.UI/Dialoge/Kosten/EnergietraegerDialog.razor` mit `EnergietraegerHuelle` |
| `ucFuelSettings` | K2 (uc, 2 103 Z.) | `EPOS.UI/Dialoge/Kosten/EnergietraegerEinstellungen.razor` |
| `ucStromAufschlaege` | K1 (uc) | `EPOS.UI/Dialoge/Kosten/StromAufschlaege.razor` |
| `ucBrennstoffBestandteile` | K1 (uc) | `EPOS.UI/Dialoge/Kosten/BrennstoffBestandteile.razor` |

Ohne Nutzer geblieben und mitgelöscht: `EinstiegsKarte.cs` (Nachfolge `Kachel`) und
`SectionPanel.cs` (Nachfolge `Gruppenkopf`).

Der Stapellauf zählt seither **91 Masken** (98 nach iU9‑W3, 102 nach iU9‑W2, 105 nach iU9‑W0),
**0 × „nein"**, **0 × „verwaist"**, unverändert **2 × „unklar"**.
`Form_KostenKomponente` und `ucVorlagenZeile` liegen als sechstes und siebtes Prüfmuster unter
`Werkzeuge/Formularkarte.Tests/Pruefmuster/Kosten/`; **`Views/Kosten` führt seither keine
Designer-Maske mehr** — der Stapellauf-Test des Werkzeugs läuft über `Views/Heizkessel`.

## Stand nach iU9-W10a (Simulationskonfiguration I: die sieben Dialoge)

Welle 10a stellt die **sieben Dialoge** um, die aus `Form_Simulation_Config` heraus geöffnet
werden. Fünf davon hatte diese Liste geführt; `Form_Quellprofil` und `Form_Waermesenke` bauen
ihre Oberfläche im Quelltext auf und haben nie einen Designer gehabt (Befund W10‑B38), tauchen
hier also gar nicht erst auf.

| Maske | Zustand vorher | Was geschehen ist |
|---|---|---|
| `Form_Betriebsmodus` | ja | gelöscht; `EPOS.UI/Dialoge/Simulation/BetriebsmodusDialog` mit `BetriebsmodusHuelle`. |
| `Form_Klimazonenkarte` | ja | gelöscht, mit ihr das Steuerelement `KlimazonenKarte` und die beiden eingebetteten Ressourcen; `KlimazonenkarteDialog` auf dem neuen Baustein `Bildkarte`. |
| `Form_QuelleErdreich` | ja | gelöscht; `QuelleErdreichDialog` mit `QuelleErdreichHuelle`, die Klimazonenkarte darin als Überlagerung. |
| `Form_QuellePufferspeicher` | ja | gelöscht; `QuellePufferspeicherDialog`, die Pufferverwaltung darin als Überlagerung. |
| `Form_PufferSp_Projekt` | ja | gelöscht; `PufferSpProjektDialog` mit sechzehn Delegaten, in drei Rollen (eigenes Fenster und zwei Überlagerungen). |

`Form_Simulation_Config` bleibt in dieser Welle WinForms und erreichbar — sie wird mit **W10b**
zur Seite. `Form_PufferSp_Admin` bleibt bis Welle 14a; der Projektdialog springt über
`Sprungziel.PufferSpAdminNurLesen` dorthin.

## Stand nach iU9-W10b (Simulationskonfiguration II: die Seite selbst)

Welle 10b stellt den **Wirt** der sieben Dialoge um: `Form_Simulation_Config` mit ihren
vier Teildateien, dem Designer und der einzigen `.resx` der ganzen Welle. Mit ihr fallen
die drei Steuerelement-Klassen `ErzeugerKarte`, `SpeicherKarte` und `SchemaAnsicht` sowie
`Eingabefrage` (letzter Nutzer) — keine davon hat einen eigenen Designer, sie tauchen in
dieser Liste also nicht auf.

| Maske | Zustand vorher | Was geschehen ist |
|---|---|---|
| `Form_Simulation_Config` | ja | gelöscht; `EPOS.UI/Seiten/Simulation/SimulationKonfigSeite` mit `SimulationKonfigHuelle`. Die Komponente ist eine **Seite** (Entscheid R‑W10b‑1) und erscheint unter Windows bis W16 in der modalen Dialoghülle. |

Damit führt `Views/Simulation` keine Designer-Maske der Simulationskonfiguration mehr;
übrig bleiben dort `Form_Simulation_Detail`, die drei `Navigator*` und `DashboardForm`.

## Stand nach iU9-W11b (Simulationsergebnis II: die Ergebnisseite)

Welle 11b stellt die **Ergebnisansicht** um — und mit ihr in EINEM Schritt ihre fünf
Nebenmasken (Regel R‑W11‑2: maskenweise, nicht reiterweise; reiterweise stünden zwei
WebViews in einem Fenster). Zusammen 11 031 Zeilen `.cs`, 4 201 Zeilen Designer und
21 `MessageBox`.

| Maske | Zustand vorher | Was geschehen ist |
|---|---|---|
| `Form_Simulation_Detail` | ja | gelöscht; `EPOS.UI/Seiten/Simulation/SimulationErgebnisSeite` mit `SimulationErgebnisHuelle` (vier Teildateien). Die Komponente ist eine **Seite** (Entscheid R‑W11‑1) und erscheint unter Windows bis W16 in der modalen Dialoghülle, 1 474 × 821. |
| `DashboardForm` | ja | gelöscht; die Autarkie-Analyse ist ein Blatt des `ErgebnisReiter`. |
| `NavigatorUebersicht` | ja | gelöscht; ihr Inhalt ist der `UebersichtReiter` in seiner zweiten Rolle (`NurNavigator`). |
| `NavigatorStrom` | ja | gelöscht; `StromgangReiter` — jetzt MIT Sortiertumschalter (Befund W11‑B41). |
| `NavigatorWaerme` | ja | gelöscht; `WaermegangReiter`. |
| `Form_SpeicherVariantenVergleich` | ja | gelöscht; `SpeicherVariantenVergleich` als **Überlagerung** der Ergebnisseite, mit echtem Fortschritt („n von m"). |

Ohne Designer und deshalb nie in dieser Liste: `TabNavigationManager` (226 Z.),
`TabListMapper` (462 Z.), `GanglinienDarstellung` (97 Z., Rest) und
`SchluesselEintrag` (37 Z.) — alle vier ebenfalls gelöscht.

**`Views/Simulation` führt seither KEINE Designer-Maske mehr**; `Views/Stromspeicher`
noch zwei (`Form_AdminStromspeicher`, `Form_PeakShaving`).
`Form_SpeicherOptimierung` bleibt WinForms (iF22) und hatte nie einen Designer —
sie ist ab jetzt über die **Sprungbrücke** (`Sprungziel.SpeicherOptimierung`) zu
erreichen, aus dem Parameterblatt der Ergebnisseite heraus.

## Stand nach iU9-W12 (Stromganglinie, Peak-Shaving, Importkonflikte)

Welle 12 stellt **sechs** Masken um — die vier Glieder der AP5-Importkette, die
Zuordnung der Projektganglinien und die Lastspitzenkappung; zusammen 2 134 Zeilen
`.cs`, 1 409 Zeilen Designer, 10 `MessageBox` und 13 indirekte über
`Program.ZahlPruefen`.

| Maske | Zustand vorher | Was geschehen ist |
|---|---|---|
| `Form_GanglinieProtokoll` | ja | gelöscht; `EPOS.UI/Dialoge/Strom/GanglinieProtokollDialog` als Überlagerung ihrer beiden Wirte. |
| `Form_GanglinieImportOptionen` | ja | gelöscht; `GanglinieImportOptionenDialog`, ebenfalls als Überlagerung. |
| `Form_ImportKonflikte` | (nie gelistet) | gelöscht; `Dialoge/Import/ImportKonflikteDialog` mit `ImportKonflikteHuelle` für die vier W13-Aufrufer. Sie hatte KEINEN Designer und stand deshalb nie in dieser Liste (Befund W12‑B21). |
| `Form_Stromganglinie_Admin` | ja | gelöscht; `StromganglinieAdminDialog` mit `StromganglinieAdminHuelle` (`Masken.StromganglinieAdmin`). |
| `Form_Stromganglinie` | ja | gelöscht; `StromganglinieDialog` mit `StromganglinieHuelle`. **Mit ihr fällt der Anker des Erreichbarkeitstests**: Von den zwölf Masken mit einem Pfad ab `Form_Start` fällt keine erst in W13/W14 (Befund W12‑B26). Nachfolger ist `Form_AdminSettings` über `MDIMainForm → MenuItem_Einstellungen` — W14c ist die letzte der W13/W14-Wellen. |
| `Form_PeakShaving` | ja | gelöscht; `PeakShavingDialog` mit `PeakShavingHuelle` (`Masken.PeakShaving`, mit Projekt-Id). |

**`Views/Stromverbraucher` und `Views/Stromspeicher` führten danach je eine
Designer-Maske** (`Form_Stromverbraucher_Admin`, `Form_AdminStromspeicher`);
`Views/Import` führt gar keine mehr.

## Stand nach iU9-W14b (Bedarfs-Admin, 04.09.2026)

Welle 14b hat die vier ruhenden Verwaltungsmasken des Bedarfs umgestellt und gelöscht:

| Maske | Zustand vorher | Was geschehen ist |
|---|---|---|
| `Form_Stromverbraucher_Admin` | ja | gelöscht; Ausprägung `Stromverbraucher` von `EPOS.UI/Dialoge/Bedarf/BedarfAdminDialog` mit `BedarfAdminHuelle`. |
| `Form_Prozesswaerme_Admin` | ja | gelöscht; Ausprägung `Prozesswaerme` derselben Komponente. |
| `Form_Brauchwasser_Admin` | ja | gelöscht; Ausprägung `Brauchwasser` derselben Komponente. **Mit ihr wandert der KLEINSCHREIBUNGS-Zeuge** des Tests `FindetAlleDesignerDateienUnabhaengigVonDerSchreibweise` auf `WizardParent.designer.cs` (Welle 16). |
| `Form_Solarganglinie_Admin` | ja | gelöscht; `EPOS.UI/Dialoge/Solarthermie/SolarganglinieAdminDialog` mit `SolarganglinieAdminHuelle`. Ihr `Sprungziel` entfällt — der Projektdialog zeigt sie als Überlagerung. |

**`Views/Brauchwasser`, `Views/Prozesswärme` und `Views/Stromverbraucher` führen seither
keine Designer-Maske mehr**; `Views/Solarthermie` führt noch eine
(`Form_SolarKollektorenAdmin`, Welle 14a).

## Zählung

| Zustand | Masken | Bedeutung |
|---|---|---|
| ja | 27 | Weg von MDIMainForm bzw. Form_Start vorhanden |
| nein | 0 | Öffner steht im Quelltext, ist selbst aber nicht zu erreichen |
| verwaist | 0 | die Maske wird nirgends erzeugt |
| unklar | 1 | nur über einen zweifelhaften Weg (verborgener oder gesperrter Knopf) |
| gesamt | 28 | |

| Maske | Öffner erreichbar | Pfad bzw. Öffner | Datei |
|---|---|---|---|
| Form_PufferSp_Bearbeiten | unklar | MDIMainForm → InitPeakShavingMenue → MenueCtrl.PufferSp → Masken.PufferSpAdmin → Form_PufferSp_Admin → btn_Bearbeiten → Form_PufferSp_Bearbeiten — Öffner: Form_PufferSp_Admin.btn_Bearbeiten_Click (Form_PufferSp_Admin.cs:164) — zweifelhaft: Steuerelement btn_Bearbeiten bleibt auf Visible/Enabled = false; Form_PufferSp_Admin.btn_Neu_Click (Form_PufferSp_Admin.cs:181) — zweifelhaft: Steuerelement btn_Neu bleibt auf Visible/Enabled = false | `WindowsFormsApplication1/Views/Pufferspeicher/Form_PufferSp_Bearbeiten.designer.cs` |
| AktionsKarte | ja | Form_Start → InitializeComponent → AktionsKarte | `WindowsFormsApplication1/Views/GemeinsameBausteine/AktionsKarte.Designer.cs` |
| FormMain | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.ProjektInFormMainLaden → Masken.ProjektDetail → WinFormsNavigation.ProjektDetailZeigen → FormMain | `WindowsFormsApplication1/Views/Hauptformular/FormMain.Designer.cs` |
| Form_AdminPV | ja | MDIMainForm → MenuItem_PC_Bearbeiten → Form_AdminPV | `WindowsFormsApplication1/Views/Photovoltaik/Form_AdminPV.designer.cs` |
| Form_AdminSettings | ja | MDIMainForm → MenuItem_Einstellungen → Form_AdminSettings | `WindowsFormsApplication1/Views/Admin/Form_AdminSettings.Designer.cs` |
| Form_AdminStromspeicher | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.StromspeicherBearbeiten → Masken.StromspeicherAdmin → Form_AdminStromspeicher | `WindowsFormsApplication1/Views/Stromspeicher/Form_AdminStromspeicher.designer.cs` |
| Form_BHKWAdmin | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.BHKW → Masken.BhkwAdmin → Form_BHKWAdmin | `WindowsFormsApplication1/Views/BHKW/Form_BHKWAdmin.designer.cs` |
| Form_Gesetzesparameter | ja | MDIMainForm → InitGesetzeMenue → Form_Gesetzesparameter | `WindowsFormsApplication1/Views/Admin/Form_Gesetzesparameter.Designer.cs` |
| Form_GesetzparameterZeile | ja | MDIMainForm → InitGesetzeMenue → Form_Gesetzesparameter → Dialog → Form_GesetzparameterZeile | `WindowsFormsApplication1/Views/Admin/Form_GesetzparameterZeile.Designer.cs` |
| Form_Heizkessel_Admin | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.Kessel → Masken.HeizkesselAdmin → Form_Heizkessel_Admin | `WindowsFormsApplication1/Views/Heizkessel/Form_Heizkessel_Admin.Designer.cs` |
| Form_HelpPopup | ja | Form_Start → pBox_Heizkessel → Program.FillRoundedRectangle → Program.Main → HilfeAutomatik.Starten → HelpExtender.PopupBereitstellen → Form_HelpPopup | `WindowsFormsApplication1/Views/Help/Form_HelpPopup.Designer.cs` |
| Form_Hinweis | ja | Form_Start → HinweisProjektGeoeffnet → Form_Hinweis | `WindowsFormsApplication1/Allgemein/Form_Hinweis.Designer.cs` |
| Form_KiEinstellungen | ja | MDIMainForm → InitKiHilfe → Form_KiChat.Oeffnen → Form_KiChat → EinstellungenOeffnen → Form_KiEinstellungen | `WindowsFormsApplication1/Views/Help/Form_KiEinstellungen.Designer.cs` |
| Form_Klimadaten | ja | MDIMainForm → MenuItem_Klimadaten → Form_Klimadaten | `WindowsFormsApplication1/Views/Klimadaten/Form_Klimadaten.Designer.cs` |
| Form_LizenzVerwaltung | ja | MDIMainForm → InitLizenzMenue → Form_LizenzVerwaltung | `WindowsFormsApplication1/Views/Admin/Form_LizenzVerwaltung.Designer.cs` |
| Form_ProjektAuswahl | ja | Form_Start → karte_ProjektZuletzt → Form_ProjektAuswahl | `WindowsFormsApplication1/Views/Projekt/Form_ProjektAuswahl.Designer.cs` |
| Form_ProjektDelete | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.ProjektDelete → Masken.ProjektDelete → Form_ProjektDelete | `WindowsFormsApplication1/Views/Projekt/Form_ProjektDelete.Designer.cs` |
| Form_ProjektSpeichernUnter | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.ProjektSpeichernUnter → Masken.ProjektSpeichernUnter → Form_ProjektSpeichernUnter | `WindowsFormsApplication1/Views/Projekt/Form_ProjektSpeichernUnter.Designer.cs` |
| Form_PufferSp_Admin | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.PufferSp → Masken.PufferSpAdmin → Form_PufferSp_Admin | `WindowsFormsApplication1/Views/Pufferspeicher/Form_PufferSp_Admin.Designer.cs` |
| Form_SolarKollektorenAdmin | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.Solarkollektoren → Masken.SolarkollektorenAdmin → Form_SolarKollektorenAdmin | `WindowsFormsApplication1/Views/Solarthermie/Form_SolarKollektorenAdmin.designer.cs` |
| Form_Start | ja | Wurzel (Einstieg der Anwendung) | `WindowsFormsApplication1/Views/Hauptformular/Form_Start.Designer.cs` |
| Form_StromTest | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.ProjektInFormMainLaden → Masken.ProjektDetail → WinFormsNavigation.ProjektDetailZeigen → FormMain → button1 → Form_StromTest | `WindowsFormsApplication1/Views/Form_StromTest.Designer.cs` |
| MDIMainForm | ja | Wurzel (Einstieg der Anwendung) | `WindowsFormsApplication1/MDIMainForm.Designer.cs` |
| ProjektAuswahl | ja | Form_Start → karte_ProjektZuletzt → Form_ProjektAuswahl → InitializeComponent → ProjektAuswahl | `WindowsFormsApplication1/Views/Projekt/ProjektAuswahl.Designer.cs` |
| WizardParent | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.AssistentZeigen → Masken.Assistent → WinFormsNavigation.AssistentZeigen → WizardParent | `WindowsFormsApplication1/Views/Wizard/WizardParent.designer.cs` |
| Wizard_Komponenten | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.AssistentZeigen → Masken.Assistent → WinFormsNavigation.AssistentZeigen → AssistentSeiten.Erzeugen → AssistentSeiten (Felder) → Wizard_Komponenten | `WindowsFormsApplication1/Views/Wizard/Wizard_Komponenten.designer.cs` |
| Wizard_Projekt | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.AssistentZeigen → Masken.Assistent → WinFormsNavigation.AssistentZeigen → AssistentSeiten.Erzeugen → AssistentSeiten (Felder) → Wizard_Projekt | `WindowsFormsApplication1/Views/Wizard/Wizard_Projekt.Designer.cs` |
| Wizard_Stromlastgang | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.AssistentZeigen → Masken.Assistent → WinFormsNavigation.AssistentZeigen → AssistentSeiten.Erzeugen → AssistentSeiten (Felder) → Wizard_Stromlastgang | `WindowsFormsApplication1/Views/Wizard/Wizard_Stromlastgang.Designer.cs` |


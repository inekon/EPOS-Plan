# Öffner erreichbar — Befund aller Masken (03.09.2026)

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

Die übrigen 100 Masken haben einen Weg von `MDIMainForm` bzw. `Form_Start`; er steht je Maske in
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

## Zählung

| Zustand | Masken | Bedeutung |
|---|---|---|
| ja | 100 | Weg von MDIMainForm bzw. Form_Start vorhanden |
| nein | 0 | Öffner steht im Quelltext, ist selbst aber nicht zu erreichen |
| verwaist | 0 | die Maske wird nirgends erzeugt |
| unklar | 2 | nur über einen zweifelhaften Weg (verborgener oder gesperrter Knopf) |
| gesamt | 102 | |

| Maske | Öffner erreichbar | Pfad bzw. Öffner | Datei |
|---|---|---|---|
| Form_GebWohnflaeche | unklar | Form_Start → pBox_Gebaude_Click → Form_Gebaeude → btn_Aendern → Form_GebWohnflaeche — Öffner: Form_Gebaeude.btn_Aendern_Click (Form_Gebaeude.cs:425) — zweifelhaft: Steuerelement btn_Aendern bleibt auf Visible/Enabled = false | `WindowsFormsApplication1/Views/Gebäude/Form_GebWohnflaeche.designer.cs` |
| Form_PufferSp_Bearbeiten | unklar | MDIMainForm → InitPeakShavingMenue → MenueCtrl.PufferSp → Masken.PufferSpAdmin → Form_PufferSp_Admin → btn_Bearbeiten → Form_PufferSp_Bearbeiten — Öffner: Form_PufferSp_Admin.btn_Bearbeiten_Click (Form_PufferSp_Admin.cs:164) — zweifelhaft: Steuerelement btn_Bearbeiten bleibt auf Visible/Enabled = false; Form_PufferSp_Admin.btn_Neu_Click (Form_PufferSp_Admin.cs:178) — zweifelhaft: Steuerelement btn_Neu bleibt auf Visible/Enabled = false | `WindowsFormsApplication1/Views/Pufferspeicher/Form_PufferSp_Bearbeiten.designer.cs` |
| AktionsKarte | ja | Form_Start → InitializeComponent → AktionsKarte | `WindowsFormsApplication1/Views/GemeinsameBausteine/AktionsKarte.Designer.cs` |
| DashboardForm | ja | Form_Start → pBox_DetailSim → Form_Simulation_Detail → .ctor → TabNavigationManager.ShowContent → DashboardForm | `WindowsFormsApplication1/Views/Simulation/DashboardForm.Designer.cs` |
| FormMain | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.ProjektInFormMainLaden → Masken.ProjektDetail → WinFormsNavigation.ProjektDetailZeigen → FormMain | `WindowsFormsApplication1/Views/Hauptformular/FormMain.Designer.cs` |
| Form_AdminPV | ja | MDIMainForm → MenuItem_PC_Bearbeiten → Form_AdminPV | `WindowsFormsApplication1/Views/Photovoltaik/Form_AdminPV.designer.cs` |
| Form_AdminSettings | ja | MDIMainForm → MenuItem_Einstellungen → Form_AdminSettings | `WindowsFormsApplication1/Views/Admin/Form_AdminSettings.Designer.cs` |
| Form_AdminStromspeicher | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.StromspeicherBearbeiten → Masken.StromspeicherAdmin → Form_AdminStromspeicher | `WindowsFormsApplication1/Views/Stromspeicher/Form_AdminStromspeicher.designer.cs` |
| Form_AdminWaermeeinlesen | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.WaermebedarfExtern → Masken.WaermebedarfExternAdmin → Form_AdminWaermeeinlesen | `WindowsFormsApplication1/Views/Wärmebedarf/Form_AdminWaermeeinlesen.designer.cs` |
| Form_BHKWAdmin | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.BHKW → Masken.BhkwAdmin → Form_BHKWAdmin | `WindowsFormsApplication1/Views/BHKW/Form_BHKWAdmin.designer.cs` |
| Form_BHKWEing | ja | Form_Start → label2_pBox_BHKW → Form_BHKWEing | `WindowsFormsApplication1/Views/BHKW/Form_BHKWEing.designer.cs` |
| Form_Betriebsmodus | ja | Form_Start → btn_SimKonfig → Form_Simulation_Config → BetriebsmodusBearbeiten → Form_Betriebsmodus | `WindowsFormsApplication1/Views/Simulation/Form_Betriebsmodus.Designer.cs` |
| Form_BkUebernahme | ja | Form_Start → BaueBerichteKostenSeite → UcBerichteKosten → Uebersicht → UcBkUebersicht → MerkmalUebernahme → Form_BkUebernahme | `WindowsFormsApplication1/Views/BerichteKosten/Form_BkUebernahme.Designer.cs` |
| Form_Brauchwasser | ja | Form_Start → pBox_Brauchwasser_Click → Form_Brauchwasser | `WindowsFormsApplication1/Views/Brauchwasser/Form_Brauchwasser.designer.cs` |
| Form_Brauchwasser_Admin | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.Brauchwasser → Masken.BrauchwasserAdmin → Form_Brauchwasser_Admin | `WindowsFormsApplication1/Views/Brauchwasser/Form_Brauchwasser_Admin.designer.cs` |
| Form_CECImport | ja | MDIMainForm → MenuItem_PV_Import_CEC → Main_PV_Test | `WindowsFormsApplication1/Views/Photovoltaik/Form_CECImport.Designer.cs` |
| Form_DBBHKW | ja | Form_Start → label2_pBox_BHKW → Form_BHKWEing → btn_DBBHKW_Edit → Form_DBBHKW | `WindowsFormsApplication1/Views/BHKW/Form_DBBHKW.designer.cs` |
| Form_EingBrauchwasserTyp | ja | Form_Start → pBox_Brauchwasser_Click → Form_Brauchwasser → btn_ProzTypeDBedit → Form_EingBrauchwasserTyp | `WindowsFormsApplication1/Views/Brauchwasser/Form_EingBrauchwasserTyp.designer.cs` |
| Form_EingDBBrauchwasser | ja | Form_Start → pBox_Brauchwasser_Click → Form_Brauchwasser → btn_Prozess_DBedit → Form_EingDBBrauchwasser | `WindowsFormsApplication1/Views/Brauchwasser/Form_EingDBBrauchwasser.designer.cs` |
| Form_EingDBProzess | ja | Form_Start → pBox_Prozess_Click → Form_Prozesswaerme → btn_Prozess_DBedit → Form_EingDBProzess | `WindowsFormsApplication1/Views/Prozesswärme/Form_EingDBProzess.designer.cs` |
| Form_EingDBStromverbraucher | ja | Form_Start → pBox_StdLastProfil_Click → Form_Stromverbraucher → btn_Strom_DBedit → Form_EingDBStromverbraucher | `WindowsFormsApplication1/Views/Stromverbraucher/Form_EingDBStromverbraucher.designer.cs` |
| Form_EingGebTyp | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.GebaeudetypenBearbeiten → Masken.GebaeudetypenAdmin → Form_EingGebTyp | `WindowsFormsApplication1/Views/Gebäude/Form_EingGebTyp.designer.cs` |
| Form_EingProzTyp | ja | Form_Start → pBox_Prozess_Click → Form_Prozesswaerme → btn_ProzTypeDBedit → Form_EingProzTyp | `WindowsFormsApplication1/Views/Prozesswärme/Form_EingProzTyp.designer.cs` |
| Form_EingStromTyp | ja | Form_Start → pBox_StromProfilEigenes_Click → Form_EingStromTyp | `WindowsFormsApplication1/Views/Stromverbraucher/Form_EingStromTyp.designer.cs` |
| Form_Emissionskatalog | ja | MDIMainForm → InitKostenvorlagenMenue → Form_Energietraeger → ZeigeTraeger → ucFuelSettings → KatalogFuerZeile → Form_Emissionskatalog | `WindowsFormsApplication1/Views/Kosten/Form_Emissionskatalog.Designer.cs` |
| Form_Energietraeger | ja | MDIMainForm → InitKostenvorlagenMenue → Form_Energietraeger | `WindowsFormsApplication1/Views/Kosten/Form_Energietraeger.Designer.cs` |
| Form_ErgBrauchwasserwaerme | ja | Form_Start → pBox_DetailSim → Form_Simulation_Detail → btn_Details → Form_ErgBrauchwasserwaerme | `WindowsFormsApplication1/Views/Brauchwasser/Form_ErgBrauchwasserwaerme.designer.cs` |
| Form_ErgProzesswaerme | ja | Form_Start → pBox_Prozess_Click → Form_Prozesswaerme → btn_Simulation → Form_ErgProzesswaerme | `WindowsFormsApplication1/Views/Prozesswärme/Form_ErgProzesswaerme.designer.cs` |
| Form_ErgStromverbraucher | ja | Form_Start → pBox_StdLastProfil_Click → Form_Stromverbraucher → btn_Simulation → Form_ErgStromverbraucher | `WindowsFormsApplication1/Views/Stromverbraucher/Form_ErgStromverbraucher.designer.cs` |
| Form_GanglinieImportOptionen | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.PeakShavingBearbeiten → Masken.PeakShaving → Form_PeakShaving → btn_Datei → Form_GanglinieImportOptionen | `WindowsFormsApplication1/Views/Stromverbraucher/Form_GanglinieImportOptionen.Designer.cs` |
| Form_GanglinieProtokoll | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.PeakShavingBearbeiten → Masken.PeakShaving → Form_PeakShaving → btn_Datei → Form_GanglinieProtokoll.Zeigen → Form_GanglinieProtokoll | `WindowsFormsApplication1/Views/Stromverbraucher/Form_GanglinieProtokoll.Designer.cs` |
| Form_Gebaeude | ja | Form_Start → pBox_Gebaude_Click → Form_Gebaeude | `WindowsFormsApplication1/Views/Gebäude/Form_Gebaeude.designer.cs` |
| Form_Gebaeude1 | ja | Form_Start → pBox_Gebaude_Click → Form_Gebaeude → btn_GebAendern_DB → Form_Gebaeude1 | `WindowsFormsApplication1/Views/Gebäude/Form_Gebaeude1.designer.cs` |
| Form_Gebaeude2 | ja | Form_Start → pBox_Gebaude_Click → Form_Gebaeude → btn_GebAendern_DB → Form_Gebaeude1 → btn_Dialog2 → Form_Gebaeude2 | `WindowsFormsApplication1/Views/Gebäude/Form_Gebaeude2.designer.cs` |
| Form_Gesetzesparameter | ja | MDIMainForm → InitGesetzeMenue → Form_Gesetzesparameter | `WindowsFormsApplication1/Views/Admin/Form_Gesetzesparameter.Designer.cs` |
| Form_GesetzparameterZeile | ja | MDIMainForm → InitGesetzeMenue → Form_Gesetzesparameter → Dialog → Form_GesetzparameterZeile | `WindowsFormsApplication1/Views/Admin/Form_GesetzparameterZeile.Designer.cs` |
| Form_Heizkessel | ja | Form_Start → pBox_Heizkessel_Click → Form_Heizkessel | `WindowsFormsApplication1/Views/Heizkessel/Form_Heizkessel.Designer.cs` |
| Form_Heizkessel_Admin | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.Kessel → Masken.HeizkesselAdmin → Form_Heizkessel_Admin | `WindowsFormsApplication1/Views/Heizkessel/Form_Heizkessel_Admin.Designer.cs` |
| Form_Heizkessel_Bearbeiten | ja | Form_Start → pBox_Heizkessel_Click → Form_Heizkessel → btn_Bearbeiten → Form_Heizkessel_Bearbeiten | `WindowsFormsApplication1/Views/Heizkessel/Form_Heizkessel_Bearbeiten.designer.cs` |
| Form_Heizkessel_einlesen | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.SPKImport → Masken.HeizkesselImport → Form_Heizkessel_einlesen | `WindowsFormsApplication1/Views/Heizkessel/Form_Heizkessel_einlesen.designer.cs` |
| Form_HelpPopup | ja | Form_Start → pBox_Heizkessel → Program.FillRoundedRectangle → Program.Main → HilfeAutomatik.Starten → HelpExtender.PopupBereitstellen → Form_HelpPopup | `WindowsFormsApplication1/Views/Help/Form_HelpPopup.Designer.cs` |
| Form_Hinweis | ja | Form_Start → HinweisProjektGeoeffnet → Form_Hinweis | `WindowsFormsApplication1/Allgemein/Form_Hinweis.Designer.cs` |
| Form_KiEinstellungen | ja | MDIMainForm → InitKiHilfe → Form_KiChat.Oeffnen → Form_KiChat → EinstellungenOeffnen → Form_KiEinstellungen | `WindowsFormsApplication1/Views/Help/Form_KiEinstellungen.Designer.cs` |
| Form_Klimadaten | ja | MDIMainForm → MenuItem_Klimadaten → Form_Klimadaten | `WindowsFormsApplication1/Views/Klimadaten/Form_Klimadaten.Designer.cs` |
| Form_Klimazonenkarte | ja | Form_Start → btn_SimKonfig → Form_Simulation_Config → _wqCombo → Form_QuelleErdreich → _btnKarte → Form_Klimazonenkarte | `WindowsFormsApplication1/Views/Simulation/Form_Klimazonenkarte.Designer.cs` |
| Form_KostenKomponente | ja | MDIMainForm → InitKostenvorlagenMenue → Form_KostenKomponente | `WindowsFormsApplication1/Views/Kosten/Form_KostenKomponente.Designer.cs` |
| Form_Kostenprofil | ja | MDIMainForm → InitKostenvorlagenMenue → Form_Energietraeger → ZeigeTraeger → Form_Kostenprofil | `WindowsFormsApplication1/Views/Kosten/Form_Kostenprofil.Designer.cs` |
| Form_LeistungspreisReihe | ja | MDIMainForm → InitKostenvorlagenMenue → Form_Energietraeger → ZeigeTraeger → ucFuelSettings → BaueLeistungspreisZusatz → Form_LeistungspreisReihe | `WindowsFormsApplication1/Views/Kosten/Form_LeistungspreisReihe.Designer.cs` |
| Form_LizenzVerwaltung | ja | MDIMainForm → InitLizenzMenue → Form_LizenzVerwaltung | `WindowsFormsApplication1/Views/Admin/Form_LizenzVerwaltung.Designer.cs` |
| Form_PV | ja | Form_Start → pBox_PV → Form_PV | `WindowsFormsApplication1/Views/Photovoltaik/Form_PV.Designer.cs` |
| Form_PeakShaving | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.PeakShavingBearbeiten → Masken.PeakShaving → Form_PeakShaving | `WindowsFormsApplication1/Views/Stromspeicher/Form_PeakShaving.Designer.cs` |
| Form_ProjektAuswahl | ja | Form_Start → karte_ProjektZuletzt → Form_ProjektAuswahl | `WindowsFormsApplication1/Views/Projekt/Form_ProjektAuswahl.Designer.cs` |
| Form_ProjektDelete | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.ProjektDelete → Masken.ProjektDelete → Form_ProjektDelete | `WindowsFormsApplication1/Views/Projekt/Form_ProjektDelete.Designer.cs` |
| Form_ProjektSpeichernUnter | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.ProjektSpeichernUnter → Masken.ProjektSpeichernUnter → Form_ProjektSpeichernUnter | `WindowsFormsApplication1/Views/Projekt/Form_ProjektSpeichernUnter.Designer.cs` |
| Form_Prozesswaerme | ja | Form_Start → pBox_Prozess_Click → Form_Prozesswaerme | `WindowsFormsApplication1/Views/Prozesswärme/Form_Prozesswaerme.designer.cs` |
| Form_Prozesswaerme_Admin | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.Prozesswaerme → Masken.ProzesswaermeAdmin → Form_Prozesswaerme_Admin | `WindowsFormsApplication1/Views/Prozesswärme/Form_Prozesswaerme_Admin.designer.cs` |
| Form_PufferSp | ja | Form_Start → pBox_Pufferspeicher → Form_PufferSp | `WindowsFormsApplication1/Views/Pufferspeicher/Form_PufferSp.Designer.cs` |
| Form_PufferSp_Admin | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.PufferSp → Masken.PufferSpAdmin → Form_PufferSp_Admin | `WindowsFormsApplication1/Views/Pufferspeicher/Form_PufferSp_Admin.Designer.cs` |
| Form_PufferSp_Projekt | ja | Form_Start → btn_SimKonfig → Form_Simulation_Config → PufferVerwaltungOeffnen → Form_PufferSp_Projekt | `WindowsFormsApplication1/Views/Pufferspeicher/Form_PufferSp_Projekt.Designer.cs` |
| Form_PufferSp_einlesen | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.PufferSPImport → Masken.PufferSpImport → Form_PufferSp_einlesen | `WindowsFormsApplication1/Views/Pufferspeicher/Form_PufferSp_einlesen.designer.cs` |
| Form_QuelleErdreich | ja | Form_Start → btn_SimKonfig → Form_Simulation_Config → _wqCombo → Form_QuelleErdreich | `WindowsFormsApplication1/Views/Simulation/Form_QuelleErdreich.Designer.cs` |
| Form_QuellePufferspeicher | ja | Form_Start → btn_SimKonfig → Form_Simulation_Config → _wqCombo → Form_QuellePufferspeicher | `WindowsFormsApplication1/Views/Simulation/Form_QuellePufferspeicher.Designer.cs` |
| Form_Simulation_Config | ja | Form_Start → btn_SimKonfig → Form_Simulation_Config | `WindowsFormsApplication1/Views/Simulation/Form_Simulation_Config.Designer.cs` |
| Form_Simulation_Detail | ja | Form_Start → pBox_DetailSim → Form_Simulation_Detail | `WindowsFormsApplication1/Views/Simulation/Form_Simulation_Detail.Designer.cs` |
| Form_SolarDB | ja | Form_Start → pBox_Solarthermie → Form_SolarKollektoren → btn_Kollektor_DB_Edit → Form_SolarDB | `WindowsFormsApplication1/Views/Solarthermie/Form_SolarDB.designer.cs` |
| Form_SolarKollektoren | ja | Form_Start → pBox_Solarthermie → Form_SolarKollektoren | `WindowsFormsApplication1/Views/Solarthermie/Form_SolarKollektoren.designer.cs` |
| Form_SolarKollektorenAdmin | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.Solarkollektoren → Masken.SolarkollektorenAdmin → Form_SolarKollektorenAdmin | `WindowsFormsApplication1/Views/Solarthermie/Form_SolarKollektorenAdmin.designer.cs` |
| Form_SolarKollektoren_einlesen | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.SolarThermieImport → Masken.SolarkollektorenImport → Form_SolarKollektoren_einlesen | `WindowsFormsApplication1/Views/Solarthermie/Form_SolarKollektoren_einlesen.designer.cs` |
| Form_Solarganglinie | ja | Form_Start → pBox_Solarthermie → Form_Solarganglinie | `WindowsFormsApplication1/Views/Solarthermie/Form_Solarganglinie.designer.cs` |
| Form_Solarganglinie_Admin | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.Solarganglinie → Masken.SolarganglinieAdmin → Form_Solarganglinie_Admin | `WindowsFormsApplication1/Views/Solarthermie/Form_Solarganglinie_Admin.designer.cs` |
| Form_SpeicherVariantenVergleich | ja | Form_Start → pBox_DetailSim → Form_Simulation_Detail → btn_SpVariantenVergleich → Form_SpeicherVariantenVergleich | `WindowsFormsApplication1/Views/Stromspeicher/Form_SpeicherVariantenVergleich.Designer.cs` |
| Form_SpotpreisImport | ja | MDIMainForm → InitKostenvorlagenMenue → Form_Energietraeger → ZeigeTraeger → Form_SpotpreisImport | `WindowsFormsApplication1/Views/Kosten/Form_SpotpreisImport.Designer.cs` |
| Form_Start | ja | Wurzel (Einstieg der Anwendung) | `WindowsFormsApplication1/Views/Hauptformular/Form_Start.Designer.cs` |
| Form_StromTest | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.ProjektInFormMainLaden → Masken.ProjektDetail → WinFormsNavigation.ProjektDetailZeigen → FormMain → button1 → Form_StromTest | `WindowsFormsApplication1/Views/Form_StromTest.Designer.cs` |
| Form_Stromganglinie | ja | Form_Start → pBox_StromMessdaten_Click → Form_Stromganglinie | `WindowsFormsApplication1/Views/Stromverbraucher/Form_Stromganglinie.designer.cs` |
| Form_Stromganglinie_Admin | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.Stromganglinie → Masken.StromganglinieAdmin → Form_Stromganglinie_Admin | `WindowsFormsApplication1/Views/Stromverbraucher/Form_Stromganglinie_Admin.designer.cs` |
| Form_Stromspeicher | ja | Form_Start → pBox_Stromspeicher → Form_Stromspeicher | `WindowsFormsApplication1/Views/Stromspeicher/Form_Stromspeicher.designer.cs` |
| Form_Stromverbraucher | ja | Form_Start → pBox_StdLastProfil_Click → Form_Stromverbraucher | `WindowsFormsApplication1/Views/Stromverbraucher/Form_Stromverbraucher.designer.cs` |
| Form_Stromverbraucher_Admin | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.Stromverbraucher → Masken.StromverbraucherAdmin → Form_Stromverbraucher_Admin | `WindowsFormsApplication1/Views/Stromverbraucher/Form_Stromverbraucher_Admin.designer.cs` |
| Form_WP | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.WP_Administration → Masken.WpAdministration → Form_WP | `WindowsFormsApplication1/Views/Wärmepumpe/Form_WP.Designer.cs` |
| Form_WPAuswahl | ja | Form_Start → pBox_WP → Form_WPAuswahl | `WindowsFormsApplication1/Views/Wärmepumpe/Form_WPAuswahl.designer.cs` |
| Form_WPFilterAuswahl | ja | Form_Start → pBox_WP → Form_WPAuswahl → btn_Neu → Form_WpFilterAuswahl | `WindowsFormsApplication1/Views/Wärmepumpe/Form_WPFilterAuswahl.Designer.cs` |
| Form_WP_einlesen | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.WPImport → Masken.WpImport → Form_WP_einlesen | `WindowsFormsApplication1/Views/Wärmepumpe/Form_WP_einlesen.designer.cs` |
| Form_Waermebedarf | ja | Form_Start → pBox_WBedarfDaten_Click → Form_Waermebedarf | `WindowsFormsApplication1/Views/Wärmebedarf/Form_Waermebedarf.designer.cs` |
| Kenndaten | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.WP_Administration → Masken.WpAdministration → Form_WP → btn_Kenndaten → Kenndaten | `WindowsFormsApplication1/Views/Wärmepumpe/Kenndaten.Designer.cs` |
| MDIMainForm | ja | Wurzel (Einstieg der Anwendung) | `WindowsFormsApplication1/MDIMainForm.Designer.cs` |
| NavigatorStrom | ja | Form_Start → pBox_DetailSim → Form_Simulation_Detail → .ctor → TabNavigationManager.ShowContent → NavigatorStrom | `WindowsFormsApplication1/Views/Simulation/NavigatorStrom.Designer.cs` |
| NavigatorUebersicht | ja | Form_Start → pBox_DetailSim → Form_Simulation_Detail → .ctor → TabNavigationManager.ShowContent → NavigatorUebersicht | `WindowsFormsApplication1/Views/Simulation/NavigatorUebersicht.Designer.cs` |
| NavigatorWaerme | ja | Form_Start → pBox_DetailSim → Form_Simulation_Detail → .ctor → TabNavigationManager.ShowContent → NavigatorWaerme | `WindowsFormsApplication1/Views/Simulation/NavigatorWaerme.Designer.cs` |
| ProjektAuswahl | ja | Form_Start → karte_ProjektZuletzt → Form_ProjektAuswahl → InitializeComponent → ProjektAuswahl | `WindowsFormsApplication1/Views/Projekt/ProjektAuswahl.Designer.cs` |
| UcBericht | ja | Form_Start → BaueBerichteKostenSeite → UcBerichteKosten → Bericht → UcBericht | `WindowsFormsApplication1/Views/Bericht/UcBericht.Designer.cs` |
| UcWirtschaftlichkeit | ja | Form_Start → BaueBerichteKostenSeite → UcBerichteKosten → Wirtschaftlichkeit → UcWirtschaftlichkeit | `WindowsFormsApplication1/Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.Designer.cs` |
| WizardParent | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.AssistentZeigen → Masken.Assistent → WinFormsNavigation.AssistentZeigen → WizardParent | `WindowsFormsApplication1/Views/Wizard/WizardParent.designer.cs` |
| Wizard_Komponenten | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.AssistentZeigen → Masken.Assistent → WinFormsNavigation.AssistentZeigen → AssistentSeiten.Erzeugen → AssistentSeiten (Felder) → Wizard_Komponenten | `WindowsFormsApplication1/Views/Wizard/Wizard_Komponenten.designer.cs` |
| Wizard_Projekt | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.AssistentZeigen → Masken.Assistent → WinFormsNavigation.AssistentZeigen → AssistentSeiten.Erzeugen → AssistentSeiten (Felder) → Wizard_Projekt | `WindowsFormsApplication1/Views/Wizard/Wizard_Projekt.Designer.cs` |
| Wizard_Stromlastgang | ja | MDIMainForm → InitPeakShavingMenue → MenueCtrl.AssistentZeigen → Masken.Assistent → WinFormsNavigation.AssistentZeigen → AssistentSeiten.Erzeugen → AssistentSeiten (Felder) → Wizard_Stromlastgang | `WindowsFormsApplication1/Views/Wizard/Wizard_Stromlastgang.Designer.cs` |
| Wizard_WPItem | ja | Form_Start → pBox_WP → Form_WPAuswahl → btn_Uebernehmen → Wizard_WPItem | `WindowsFormsApplication1/Views/Wizard/Wizard_WPItem.Designer.cs` |
| ucBrennstoffBestandteile | ja | MDIMainForm → InitKostenvorlagenMenue → Form_Energietraeger → ZeigeTraeger → ucFuelSettings → BaueBrennstoffblock → ucBrennstoffBestandteile | `WindowsFormsApplication1/Views/Kosten/ucBrennstoffBestandteile.Designer.cs` |
| ucErtragBonus | ja | MDIMainForm → InitKostenvorlagenMenue → Form_KostenKomponente → .ctor → ucErtragBonus | `WindowsFormsApplication1/Views/Kosten/ucErtragBonus.Designer.cs` |
| ucFuelSettings | ja | MDIMainForm → InitKostenvorlagenMenue → Form_Energietraeger → ZeigeTraeger → ucFuelSettings | `WindowsFormsApplication1/Views/Kosten/ucFuelSettings.Designer.cs` |
| ucStromAufschlaege | ja | MDIMainForm → InitKostenvorlagenMenue → Form_Energietraeger → ZeigeTraeger → ucFuelSettings → BaueAufschlagsblock → ucStromAufschlaege | `WindowsFormsApplication1/Views/Kosten/ucStromAufschlaege.Designer.cs` |
| ucVorlagenZeile | ja | MDIMainForm → InitKostenvorlagenMenue → Form_KostenKomponente → ZeileBauen → ucVorlagenZeile | `WindowsFormsApplication1/Views/Kosten/ucVorlagenZeile.Designer.cs` |

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

using Color = System.Drawing.Color;
using HorizontalAlignment = System.Windows.Forms.HorizontalAlignment;


namespace WindowsFormsApplication1
{
    public partial class Form_Simulation_Detail : Form
    {
        public SimulationWaermebedarf simulation_Waermebedarf = new SimulationWaermebedarf();
        public SimulationStrombedarf simulation_Strombedarf = new SimulationStrombedarf();
        SimulationWaermepumpe simulation_wp = new SimulationWaermepumpe();
        SimulationControl sim = new SimulationControl();
        KonfigurationCtrl ctrl = new KonfigurationCtrl();
        ProjektCtrl projektCtrl = new ProjektCtrl();
        ChartManager[] _chartManager = new ChartManager[11];
        ToolTip tooltip = new ToolTip();

        public int m_ID_Projekt;
        private System.Windows.Forms.ListView listView_SimSolar;
        private System.Windows.Forms.ListView listView_SimPV;

        // Ergebnistabelle der Pufferspeicher (Konzept 13.3) - ersetzt die
        // textBox_Pufferspeicher, die nur einen Speicher zeigen konnte. Programmatisch
        // angelegt wie listView_SimSolar/listView_SimPV; kein Designer-, kein .resx-Eingriff.
        private System.Windows.Forms.ListView listView_SimPuffer;

        // Kompakte Textzeile mit den Warnungen der VDI-4640-Auslegungsprüfung
        // (Konzept 4.5/13.1, Ergebnisanbindung aus Paket 3).
        private System.Windows.Forms.Label label_Erdreich;

        // Nicht-modale Meldungszeile des Protokollkanals (Paket 8, Konzept 13.4).
        // Programmatisch angelegt und an btn_Simulation ausgerichtet - Designer und
        // .resx bleiben unangetastet.
        private System.Windows.Forms.Label label_Laufmeldungen;
        private string _laufmeldungenText = "";

        // Ergebnisdiagramm der Heizkessel-Seite (Sichttest: die Seite zeigte bisher nur
        // Zahlen). Aufbau wie die Wärmepumpen-Seite - Diagramm, Umschalter "sortiert",
        // CSV-Ausgabe; alles programmatisch, Designer und .resx bleiben unangetastet.
        private System.Windows.Forms.DataVisualization.Charting.Chart chart_Kessel;
        private System.Windows.Forms.CheckBox checkBox_Kessel_sortiert;
        private System.Windows.Forms.Button btn_CsvExportKessel;
        private ChartManager _chartKesselManager;

        /// <summary>
        /// Gehört zum aktuellen Ergebnis ein Kessel-Diagramm? Gesetzt von
        /// <see cref="KesselErgebnisAnzeigen"/>.
        ///
        /// NICHT über <c>chart_Kessel.Visible</c> geprüft: Dessen Getter liefert false,
        /// solange ein Elternelement nicht angezeigt wird — und die Steuerelemente der
        /// Registerkarte liegen zeitweise in einer nicht sichtbaren TabPage, während sie
        /// zwischen Seitenleiste und Panel wandern (siehe listViewQuellen_SelectedIndexChanged).
        /// Der Umschalter „sortiert" hätte dann wortlos nichts getan.
        /// </summary>
        private bool _kesselChartAktiv;

        // ------------------------------------------------------------------------------
        //  BHKW-Seite: Umschalter „sortiert" und die zwei Speicher-Kennzahlen
        // ------------------------------------------------------------------------------
        //
        // Das Diagramm chart_BHKW_Waerme steht im Designer, der Umschalter kommt
        // programmatisch dazu — genau wie auf der Heizkessel-Seite (dort ist auch das
        // Diagramm programmatisch, weil es keines gab). Designer und .resx bleiben für
        // den Umschalter unangetastet; die zwei neuen Kennzahlzeilen entstehen ebenfalls
        // programmatisch nach dem Muster von InitKesselQuellwaerme.
        private System.Windows.Forms.CheckBox checkBox_BHKW_sortiert;
        private ChartManager _chartBhkwManager;

        /// <summary>
        /// Gehört zum aktuellen Ergebnis ein BHKW-Diagramm? Gesetzt von
        /// <see cref="BhkwErgebnisAnzeigen"/>. Dieselbe Begründung wie bei
        /// <see cref="_kesselChartAktiv"/>: <c>chart_BHKW_Waerme.Visible</c> liefert
        /// false, solange ein Elternelement nicht angezeigt wird.
        /// </summary>
        private bool _bhkwChartAktiv;

        // Kennzahlzeilen „davon in den Speicher" und „aus dem Speicher gedeckt"
        // (SimulationBHKW.Speicherladung_gesamt bzw. Speicherentladung_Anteil).
        private Label label_BhkwSpeicherladung;
        private TextBox tb_BhkwSpeicherladung;
        private Label label_BhkwSpeicherladungEinheit;
        private Label label_BhkwSpeicherdeckung;
        private TextBox tb_BhkwSpeicherdeckung;
        private Label label_BhkwSpeicherdeckungEinheit;

        // ------------------------------------------------------------------------------
        //  PAKET P2: Speichertemperaturen als dritte Diagrammseite (Konzept 7.4, P1-O5)
        // ------------------------------------------------------------------------------
        //
        // Das Schichtmodell aus Paket P1 führt je Senkenspeicher die Ganglinien der
        // obersten und der untersten Schicht, Paket B1 dazu die Quelltemperatur jedes
        // temperaturgekoppelten Erzeugers. Bis P2 gab es sie nur als Datenreihen
        // (ZeitreihenSatz, CSV) - im Programm war keine davon zu sehen.
        //
        // WO: als dritte Seite von tabControl2 auf der Wärmepumpen-Registerkarte, neben
        // „Wärmproduktion" und „Stromverbrauch". Das ist das etablierte Muster dieser
        // Ansicht für eine weitere Ganglinie, und die Speicher-Ergebnistabelle
        // (listView_SimPuffer) steht auf derselben Registerkarte - Zahlen und Kurve
        // bleiben beieinander.
        //
        // Diagramm und Seite entstehen PROGRAMMATISCH; Designer und .resx bleiben
        // unangetastet (Muster InitKesselChart). Die Seite hängt sich nur ein, wenn der
        // Lauf mindestens eine Temperaturreihe hat - eine leere Registerkarte wäre eine
        // Zusage ohne Inhalt.
        private System.Windows.Forms.TabPage tabPage_Speichertemperatur;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart_Speichertemperatur;
        private ChartManager _chartTemperaturManager;

        /// <summary>
        /// Farbfolge der Speicher im Temperaturdiagramm; sie wiederholt sich ab dem
        /// fünften Speicher - dieselbe Bauform wie die Speicherserien des Berichts
        /// (<c>ChartRenderer.C_SPEICHER</c>).
        /// </summary>
        private static readonly Color[] TEMP_FARBEN =
        {
            Color.FromArgb(0xC0, 0x39, 0x2B),   // Rot
            Color.FromArgb(0x28, 0x80, 0xB9),   // Blau
            Color.FromArgb(0x1D, 0x9E, 0x75),   // Grün
            Color.FromArgb(0x8E, 0x44, 0xAD)    // Violett
        };

        /// <summary>Farbe der Quelltemperatur-Reihen (Koralle) - sie gehören keinem Speicher.</summary>
        private static readonly Color TEMP_FARBE_QUELLE = Color.FromArgb(0xD8, 0x5A, 0x30);

        /// <summary>Eine Reihe des Temperaturdiagramms: Schlüssel (Schicht 2), Anzeigetext
        /// (Schicht 3), Werte und Darstellung.</summary>
        private sealed class Temperaturreihe
        {
            public string Schluessel = "";
            public string Legende = "";
            public float[] Werte;
            public Color Farbe;
            public bool Gestrichelt;
        }

        // ETAPPE E2 (L6): Kennzahlzeile „Vollbenutzungsstunden elektrisch"
        // (SimulationBHKW.VbhElektrischGesamt) — die Größe, an der der KWK-Zuschlag hängt.
        // Programmatisch wie die zwei Speicherzeilen darüber; Designer und .resx der Form
        // bleiben unangetastet.
        private Label label_BhkwVbhElektrisch;
        private TextBox tb_BhkwVbhElektrisch;
        private Label label_BhkwVbhElektrischEinheit;

        /// <summary>
        /// Zustand der Schaltfläche „Ergebnis speichern" (Nacharbeit Paket 8, Befund N1).
        ///
        /// true erst, wenn ein Lauf VOLLSTÄNDIG durchgelaufen ist und die Ergebnisfelder
        /// gefüllt wurden. Vorher stünde in den Simulationsobjekten entweder gar nichts
        /// (Formular gerade geöffnet) oder das Bruchstück eines abgebrochenen Laufs — und
        /// <c>ErgebnisCtrl.Save</c> löscht das bisherige Ergebnis des Projekts, BEVOR es
        /// das neue schreibt. Ein Klick auf „Speichern" nach einem Abbruch hätte also ein
        /// gültiges Bestandsergebnis durch einen Nullsatz ersetzt.
        ///
        /// Das Feld trägt den Zustand zusätzlich zur <c>Enabled</c>-Eigenschaft, weil die
        /// Schaltfläche im Designer aktiviert ist und der Anwender sie vor dem ersten Lauf
        /// erreichen kann.
        /// </summary>
        private bool _ergebnisGueltig = false;
        public double m_Waermebedarf_Gesamt;
        public double m_Strombedarf_Gesamt;

        double waerme_spk = 0;
        double waerme_wp = 0;
        double waerme_heizstab = 0;
        double waerme_solar = 0;
        double gesamt_waerme = 0;
        double restwaermebedarf = 0;

        Point prevPosition;

        private TabNavigationManager _navManager; // Global im Formular speichern
        private TabListMapper _einstellungenMapper; // Mappt tabControl_Einstellungen auf ListView-Navigation
        private Dictionary<string, TabPage> dictAllTabPages = new Dictionary<string, TabPage>();
        private Dictionary<string, TabPage> dictParameterTabPages = new Dictionary<string, TabPage>();

        // Das ist deine Zielvariable (0 = Wärmegeführt, 1 = Stromgeführt, 2 = Ohne Einspeisung)
        private int bhkwSimulationsArt = 0;

        // Merkt sich, von welcher TabPage aktuell die Controls im rechten Panel angezeigt werden
        private TabPage aktuellAusgeliehenePage = null;

        private int mainTabPageIndex = 0; // 0 = Parameter, 1 = Simulation
        private int mainTablistIndex = 0;

        // --- Menü-Optik für listViewQuellen (dunkles WordPress-Stil-Menü) ---
        private int _hoverIndex = -1;                 // aktuell überfahrene Zeile (-1 = keine)
        private bool _quellenMenuStyled = false;      // verhindert doppeltes Verdrahten
        private System.Windows.Forms.ImageList _quellenRowSizer; // erzwingt die Zeilenhöhe

        // Farbpalette (klassisches WP-Admin-Menü)
        private static readonly Color cMenuBase = Color.FromArgb(0x23, 0x28, 0x2d); // Grundfläche
        private static readonly Color cMenuText = Color.FromArgb(0xee, 0xee, 0xee); // Text normal
        private static readonly Color cMenuIcon = Color.FromArgb(0xa7, 0xaa, 0xad); // Icon normal (grau)
        private static readonly Color cMenuHoverBg = Color.FromArgb(0x19, 0x1e, 0x23); // Hover-Hintergrund
        private static readonly Color cMenuHoverFg = Color.FromArgb(0x00, 0xb9, 0xeb); // Hover-Text/Icon (cyan)
        private static readonly Color cMenuSelBg = Color.FromArgb(0x00, 0x73, 0xaa); // aktiv (blau)
        private static readonly Color cMenuSelFg = Color.White;                      // aktiv Text/Icon
        private static readonly Color cMenuDisabled = Color.FromArgb(0x55, 0x5d, 0x66); // deaktiviert


        // --- Technische Serienschlüssel (Paket 9 / L7) --------------------------------
        //
        // Schicht 2 der Drei-Schichten-Regel: sprachneutral, ASCII, unveränderlich.
        // Sie sind der ZUGRIFFSSCHLÜSSEL auf die Chart-Serien (Series["…"],
        // Series.IndexOf(…)); der angezeigte Text steht ausschließlich in
        // Series.LegendText und kommt aus dem Ressourcenkatalog. Muster wie
        // NavigatorWaerme (Paket 9 / L6).
        //
        // Vorher trugen die Serien ihre deutschen Anzeigenamen — und zwar uneinheitlich:
        // „Wärmebedarf" mit Umlaut in Diagramm 4, „Waermebedarf" ohne in den
        // Diagrammen 8 und 10. Ein übersetzter Name ließe die Nachschlagestellen in
        // checkBox_Ueberschuss_CheckedChanged und checkBox_Speicherzustand_CheckedChanged
        // ins Leere laufen.
        private const string S_HEIZWAERMEBEDARF = "HEIZWAERMEBEDARF";
        private const string S_WARMWASSERBEDARF = "WARMWASSERBEDARF";
        private const string S_HEIZSTAB = "HEIZSTAB";
        private const string S_WAERMEPRODUKTION = "WAERMEPRODUKTION";
        private const string S_WAERMEBEDARF = "WAERMEBEDARF";
        private const string S_STROMBEDARF = "STROMBEDARF";
        private const string S_SPEICHERFUELLSTAND = "SPEICHERFUELLSTAND";
        private const string S_UEBERSCHUSS = "UEBERSCHUSS";
        private const string S_PHOTOVOLTAIK = "PHOTOVOLTAIK";
        private const string S_RESTWAERME = "RESTWAERME";

        // BHKW-ANZEIGE-NACHZUG: Die Wärme, die das BHKW in einen Pufferspeicher legt
        // (SimulationBHKW.Speicherladung_stuendlich). Sie ist ein TEIL der Produktion,
        // nicht ihre Ergänzung — deshalb eine eigene Serie und KEINE Stapelgruppe
        // (Begründung in BhkwSerienAufbauen).
        private const string S_SPEICHERLADUNG = "SPEICHERLADUNG";
        /// <summary>
        /// Legt eine Serie unter ihrem technischen Schlüssel an und hängt den Anzeigetext
        /// an <c>LegendText</c> (Muster aus NavigatorWaerme, Paket 9 / L6).
        /// </summary>
        private static void SerieAnlegen(ChartManager cm, string schluessel, string legende,
                                         Color farbe, float[] werte)
        {
            cm.AddSeries(schluessel, farbe, werte);
            cm._chart.Series[schluessel].LegendText = legende;
        }
        /// <summary>
        /// Wie oben, aber mit ausdrücklichem Serientyp und optionaler Stapelgruppe.
        ///
        /// <c>ChartManager.AddSeries</c> vergibt <c>FastLine</c>, und <c>FastLine</c> kann
        /// nicht stapeln. Serientyp, Stapelgruppe und Balkenbreite setzt
        /// <see cref="GanglinienDarstellung.StapelEinstellen"/> — dieselbe Regel wie in
        /// NavigatorWaerme/NavigatorStrom.
        /// </summary>
        private static void SerieAnlegen(ChartManager cm, string schluessel, string legende,
                                         Color farbe, float[] werte, SeriesChartType typ,
                                         string stapelgruppe = null)
        {
            cm.AddSeries(schluessel, farbe, werte);
            Series s = cm._chart.Series[schluessel];
            s.LegendText = legende;
            GanglinienDarstellung.StapelEinstellen(s, typ, stapelgruppe);
        }
        /// <summary>Wie oben, für die XY-Variante mit <see cref="PointF"/>-Punkten.</summary>
        private static void SerieAnlegen(ChartManager cm, string schluessel, string legende,
                                         Color farbe, PointF[] punkte, int borderWidth)
        {
            cm.AddSeries(schluessel, farbe, punkte, borderWidth);
            cm._chart.Series[schluessel].LegendText = legende;
        }
        public Form_Simulation_Detail(int iD_Projekt)
        {
            InitializeComponent();
            m_ID_Projekt = iD_Projekt;

            btn_ErgebnisSpeichern.Click += btn_ErgebnisSpeichern_Click;

            init_Chart(chart1);
            init_Chart(chart2);

            // Übersicht-Diagramm (Kreis) initialisieren – entspricht chart5 aus Form_Simulation_Kurz
            ueb_chart.Legends[0].LegendStyle = LegendStyle.Table;
            ueb_chart.Legends[0].Docking = Docking.Right;
            ueb_chart.Legends[0].Alignment = StringAlignment.Center;
            ueb_chart.Legends[0].Title = MyResource.Resource.CHART_LEGENDE_WAERMEBEDARFSDECKUNG;
            ueb_chart.Legends[0].BorderColor = Color.Green;
            ueb_chart.Series[0].IsValueShownAsLabel = false;
            ueb_chart.Series[0]["PieLabelStyle"] = "Outside";
            ueb_chart.Series[0].Points.Clear();

            listView_SimSPK.View = View.Details;
            listView_SimSPK.Columns.Add(MyResource.Resource.SIM_ERZEUGERNAME_HEIZKESSEL, -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add(MyResource.Resource.SIM_SPALTE_NAME, -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add(MyResource.Resource.SIM_SPALTE_BRENNSTOFFE, -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add(MyResource.Resource.SIM_SPALTE_OEL, -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add(MyResource.Resource.SIM_SPALTE_JAHRESNUTZUNGSGRAD, -2, HorizontalAlignment.Left);
            listView_SimSPK.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_SimSPK.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            // Initialisiere die Navigation 
            _navManager = new TabNavigationManager(tabPage_Ergebnis, sim);

            // Dictionary befüllen
            foreach (TabPage page in tabControl_Simulation.TabPages)
            {
                if (!dictAllTabPages.ContainsKey(page.Name))
                {
                    dictAllTabPages.Add(page.Name, page);
                }
            }

            this.splitContainer_Parameter.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer_Parameter_SplitterMoved);
            this.listViewQuellen.TileSize = new System.Drawing.Size(this.listViewQuellen.Width, 40);

            // Menü-Optik (dunkles WP-Stil-Menü mit Icons) aktivieren – einmalig.
            // Beeinflusst nur die Darstellung; Auswahl-/Resize-Logik bleibt unverändert.
            StyleQuellenListeAlsMenu();

            // Ansicht & Verhalten der BHKW-Liste (jetzt ListView, gleiches Steuerelement
            // wie Heizkessel/Solarthermie; Feldname bleibt dataGridView_BHKW wegen .resx-Layout)
            dataGridView_BHKW.View = View.Details;
            dataGridView_BHKW.FullRowSelect = true;
            dataGridView_BHKW.GridLines = true;
            dataGridView_BHKW.MultiSelect = false;
            dataGridView_BHKW.Columns.Add(MyResource.Resource.SIM_ERZEUGERNAME_BHKW, -2, HorizontalAlignment.Left);
            dataGridView_BHKW.Columns.Add(MyResource.Resource.SIM_SPALTE_NAME, -2, HorizontalAlignment.Left);
            dataGridView_BHKW.Columns.Add(MyResource.Resource.SIM_SPALTE_WAERMEPRODUKTION, -2, HorizontalAlignment.Left);
            dataGridView_BHKW.Columns.Add(MyResource.Resource.SIM_SPALTE_STROMPRODUKTION, -2, HorizontalAlignment.Left);

            VereinheitlichePageSchriftarten(this.tabPage_Bedarf);
            VereinheitlichePageSchriftarten(this.tabPage_Wärmepumpe);
            VereinheitlichePageSchriftarten(this.tabPage_Heizkessel);
            VereinheitlichePageSchriftarten(this.tabPage_BHKW);
            VereinheitlichePageSchriftarten(this.tabPage_Solarthermie);

            // Solarkollektoren-Auflistung (analog listView_SimSPK beim Heizkessel) programmatisch
            // anlegen und im Solarthermie-Tab unter dem Diagramm platzieren (kein Designer noetig).
            listView_SimSolar = new System.Windows.Forms.ListView();
            listView_SimSolar.Name = "listView_SimSolar";
            listView_SimSolar.View = View.Details;
            listView_SimSolar.FullRowSelect = true;
            listView_SimSolar.GridLines = true;
            // Schriftart/-groesse exakt von der Heizkessel-Liste uebernehmen.
            listView_SimSolar.Font = listView_SimSPK.Font;
            listView_SimSolar.Columns.Add(MyResource.Resource.SIM_SPALTE_SOLARKOLLEKTOR, -2, HorizontalAlignment.Left);
            listView_SimSolar.Columns.Add(MyResource.Resource.SIM_SPALTE_NAME, -2, HorizontalAlignment.Left);
            listView_SimSolar.Columns.Add(MyResource.Resource.SIM_SPALTE_FLAECHE, -2, HorizontalAlignment.Left);
            listView_SimSolar.Columns.Add(MyResource.Resource.SIM_SPALTE_ANZAHL, -2, HorizontalAlignment.Left);
            listView_SimSolar.Columns.Add(MyResource.Resource.SIM_SPALTE_WAERMEPRODUKTION, -2, HorizontalAlignment.Left);
            listView_SimSolar.Columns.Add(MyResource.Resource.SIM_SPALTE_UEBERSCHUSS, -2, HorizontalAlignment.Left);
            if (chart8 != null && chart8.Parent != null)
            {
                listView_SimSolar.Location = new System.Drawing.Point(chart8.Left, chart8.Bottom + 12);
                listView_SimSolar.Width = chart8.Width;
                listView_SimSolar.Height = 180;
                listView_SimSolar.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
                chart8.Parent.Controls.Add(listView_SimSolar);
            }

            // BHKW-Liste an Font/Schriftgroesse der Heizkessel-/Solar-Liste angleichen.
            dataGridView_BHKW.Font = listView_SimSPK.Font;
            VereinheitlichePageSchriftarten(this.tabPage_Photovoltaik);
            VereinheitlichePageSchriftarten(this.tabPage_Stromspeicher);
            VereinheitlichePageSchriftarten(this.tabPage_Ergebnis);

            // Photovoltaik-Modul-Auflistung (ListView, analog Heizkessel/Solarthermie) programmatisch
            // anlegen und im PV-Tab unter dem Diagramm platzieren (kein Designer noetig).
            listView_SimPV = new System.Windows.Forms.ListView();
            listView_SimPV.Name = "listView_SimPV";
            listView_SimPV.View = View.Details;
            listView_SimPV.FullRowSelect = true;
            listView_SimPV.GridLines = true;
            listView_SimPV.Font = listView_SimSPK.Font;
            listView_SimPV.Columns.Add(MyResource.Resource.SIM_PHOTOVOLTAIK, -2, HorizontalAlignment.Left);
            listView_SimPV.Columns.Add(MyResource.Resource.SIM_SPALTE_NAME, -2, HorizontalAlignment.Left);
            listView_SimPV.Columns.Add(MyResource.Resource.SIM_SPALTE_FLAECHE, -2, HorizontalAlignment.Left);
            listView_SimPV.Columns.Add(MyResource.Resource.SIM_SPALTE_ANZAHL, -2, HorizontalAlignment.Left);
            listView_SimPV.Columns.Add(MyResource.Resource.SIM_SPALTE_STROMPRODUKTION, -2, HorizontalAlignment.Left);
            if (chart_PV != null && chart_PV.Parent != null)
            {
                listView_SimPV.Location = new System.Drawing.Point(chart_PV.Left, chart_PV.Bottom + 12);
                listView_SimPV.Width = chart_PV.Width;
                listView_SimPV.Height = 180;
                listView_SimPV.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
                chart_PV.Parent.Controls.Add(listView_SimPV);
            }

            // Pufferspeicher-Ergebnistabelle und Erdreich-Hinweis im Wärmepumpen-Tab
            // (programmatisch, Muster listView_SimSolar/listView_SimPV).
            InitPufferspeicherRubrik();

            // Diagrammseite „Speichertemperaturen" (Paket P2, Konzept 7.4) - sie hängt
            // sich erst nach einem Lauf mit Temperaturreihen in tabControl2 ein.
            InitSpeichertemperaturChart();

            // Wärmelast-Jahresganglinie im Heizkessel-Tab (programmatisch, Muster wie oben).
            InitKesselChart();

            // BHKW-Seite: Umschalter „sortiert" und die zwei Speicher-Kennzahlen
            // (BHKW-Anzeige-Nachzug, Bedienmuster der Heizkessel-Seite).
            InitBhkwChart();

            // Bedarfsseite: der Wärmebedarf je Bedarfsart (Paket E1, Konzept 4.4).
            InitBedarfKanalzeilen();

            // HIER DIE KORREKTUR: ReihenfolgeTabPages() komplett weglassen
            // und stattdessen direkt unsere neue Update-Logik starten!
            UpdateTabPages();

            string targetTabName = "tabPage_Parameter";
            if (dictAllTabPages.ContainsKey(targetTabName))
            {
                // Die TabPage aus dem Dictionary holen und direkt selektieren
                tabControl_Simulation.SelectedTab = dictAllTabPages[targetTabName];
                mainTabPageIndex = tabControl_Simulation.SelectedIndex; // Aktualisiere den Index der Haupt-TabPage
            }

            // tabControl_Einstellungen als ListView-Navigation darstellen (Original-TabControl bleibt erhalten)
            _einstellungenMapper = new TabListMapper(tabControl_Einstellungen, 200);

            // CSV-Export-Buttons (programmatisch, kein Designer/.resx nötig)
            InitCsvExportButtons();

            // Stromspeicher (AP3b): Parametereingaben je Variante und die bis hierher
            // leere Ergebnisseite - beide programmatisch, wie die Blöcke darüber.
            InitStromspeicherParameter();
            InitStromspeicherSeite();

            // Bereich für den KI-Hilfe-Assistenten melden; die aktive
            // Registerkarte wird automatisch mit erkannt.
            this.Activated += (s, e) => HilfeKontext.SetzeBereich("Detaillierte Simulation");

            // Notebook-Schutz: Fenster in die Arbeitsflaeche des Bildschirms einpassen und
            // den Inhalt per Bildlauf erreichbar halten (Allgemein\FensterEinpassung.cs).
            // Auf ausreichend grossen Schirmen wirkungslos.
            FensterEinpassung.Einhaengen(this);
        }

        /// <summary>
        /// Legt die CSV-Export-Buttons auf den Bereichen Energiebedarf und Wärmepumpe an.
        /// </summary>
        private void InitCsvExportButtons()
        {
            // Bereich Energiebedarf (tabPage_Bedarf): Wärmelast + Strombedarf
            Button btnExportBedarf = new Button();
            btnExportBedarf.Name = "btn_CsvExportBedarf";
            btnExportBedarf.Text = MyResource.Resource.SIM_BTN_CSV_EXPORT;
            btnExportBedarf.Size = new Size(150, 36);
            // Feste Position unterhalb des Wärmelast-Blocks - die Controls der TabPage
            // werden zur Laufzeit in ein schmaleres Panel verschoben, daher keine
            // Rechts-Verankerung verwenden (sonst liegt der Button außerhalb des Sichtbereichs).
            btnExportBedarf.Location = new Point(22, 565);
            btnExportBedarf.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnExportBedarf.BackColor = SystemColors.Control;
            btnExportBedarf.ForeColor = Color.Black;
            btnExportBedarf.UseVisualStyleBackColor = false;
            btnExportBedarf.Click += btn_CsvExportBedarf_Click;
            tooltip.SetToolTip(btnExportBedarf, MyResource.Resource.SIM_TOOLTIP_CSV_BEDARF);
            tabPage_Bedarf.Controls.Add(btnExportBedarf);
            btnExportBedarf.BringToFront();

            // Bereich Wärmepumpe (tabPage_Wärmepumpe)
            Button btnExportWP = new Button();
            btnExportWP.Name = "btn_CsvExportWP";
            btnExportWP.Text = MyResource.Resource.SIM_BTN_CSV_EXPORT;
            btnExportWP.Size = new Size(150, 32);
            // Feste Position rechts neben der Bivalenzpunkt-Zeile, oberhalb der Modul-Tabelle
            // (keine Rechts-Verankerung, siehe Kommentar beim Bedarf-Button).
            btnExportWP.Location = new Point(1085, 350);
            btnExportWP.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnExportWP.BackColor = SystemColors.Control;
            btnExportWP.ForeColor = Color.Black;
            btnExportWP.UseVisualStyleBackColor = false;
            btnExportWP.Click += btn_CsvExportWP_Click;
            tooltip.SetToolTip(btnExportWP, MyResource.Resource.SIM_TOOLTIP_CSV_WAERMEPUMPE);
            tabPage_Wärmepumpe.Controls.Add(btnExportWP);
            btnExportWP.BringToFront();

            // Bereich Heizkessel (tabPage_Heizkessel) - in der linken Grafikspalte unter
            // der Kesseltabelle. Erscheint erst mit dem Diagramm (siehe KesselErgebnisAnzeigen).
            if (chart_Kessel != null && chart_Kessel.Parent != null)
            {
                btn_CsvExportKessel = new Button();
                btn_CsvExportKessel.Name = "btn_CsvExportKessel";
                btn_CsvExportKessel.Text = MyResource.Resource.SIM_BTN_CSV_EXPORT;
                btn_CsvExportKessel.Size = new Size(150, 32);
                btn_CsvExportKessel.Location = new Point(chart_Kessel.Left, listView_SimSPK.Bottom + 8);
                btn_CsvExportKessel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                btn_CsvExportKessel.BackColor = SystemColors.Control;
                btn_CsvExportKessel.ForeColor = Color.Black;
                btn_CsvExportKessel.UseVisualStyleBackColor = false;
                btn_CsvExportKessel.Visible = false;
                btn_CsvExportKessel.Click += btn_CsvExportKessel_Click;
                tooltip.SetToolTip(btn_CsvExportKessel, MyResource.Resource.SIM_TOOLTIP_CSV_HEIZKESSEL);
                chart_Kessel.Parent.Controls.Add(btn_CsvExportKessel);
                btn_CsvExportKessel.BringToFront();
            }
        }

        // ====================================================================
        //  Heizkessel-Seite: Wärmelast-Jahresganglinie
        // ====================================================================
        //
        // AUSGANGSLAGE (Sichttest). Die Heizkessel-Seite zeigte ausschließlich Zahlen:
        // Brennstoffverbräuche links, Bedarfs- und Deckungsfelder rechts, darunter die
        // Tabelle der einzelnen Spitzenkessel. Die Frage, WARUM der Kessel wann läuft,
        // beantwortete keine dieser Zahlen — anders als auf der Wärmepumpen-Seite, wo
        // die Jahresganglinie Bedarf und Produktion nebeneinanderlegt.
        //
        // AUFBAU wie dort: Diagramm LINKS, Ergebniszahlen RECHTS. Der Entwurf der Seite
        // hatte die Zahlen über die ganze Breite verteilt (Brennstoffe links, Bedarf in
        // der Mitte, Tabelle unten links) — für ein Diagramm blieb nur eine Ecke. Die
        // Bestandsfelder wandern deshalb geschlossen in eine rechte Spalte
        // (siehe KesselSeiteAnordnen); Designer und .resx bleiben unangetastet.
        //
        // BEZUGSGRÖSSE DES BEDARFS (Sichttest 16.08.2026). Gezeigt wird der
        // GESAMTWÄRMEBEDARF des Projekts (simulation_Waermebedarf.Waermebedarf) —
        // dieselbe Größe, die die Seite „Energiebedarf" als Gesamtwert führt.
        //
        // Bis hierher stand hier der Stufeneingang der Kessel
        // (simulation_spk.Waermebedarf, also der Rest nach den vorgelagerten Erzeugern).
        // Der ist der beim Kessel unmittelbar anliegende Bedarf, beantwortet die Frage
        // der Seite aber nicht: Wieviel des PROJEKTbedarfs deckt der Kessel? Steht dem
        // Kessel eine Wärmepumpe oder ein BHKW voran, lag die gezeigte Bedarfslinie
        // unter der wirklichen Wärmelast, ohne dass das Bild das kenntlich machte.
        // Der Stufeneingang bleibt als Zahl im Feld textBox_Waermebedarf_Heizkessel
        // und als eigene Spalte der CSV-Ausgabe erhalten — er geht also nicht verloren.
        //
        // KEINE Serie je Kessel: Die Engine führt die Kesselleistung nur als SUMME über
        // alle Kessel (SimulationSPK.Kesselleistung_stuendlich); je-Kessel-Ganglinien
        // gibt es im Rechenkern nicht, und eine Engine-Änderung gehört nicht in eine
        // Anzeigeaufgabe. Die Aufteilung je Kessel steht als Jahressumme in der Tabelle
        // darunter. (Offen dokumentiert.)

        // Maße der neuen Aufteilung. Die rechte Spalte endet bei x≈1080 und bleibt damit
        // deutlich innerhalb der Entwurfsbreite des aufnehmenden Panels (≈1295).
        private const int KESSEL_CHART_LINKS = 16;
        private const int KESSEL_CHART_OBEN = 40;
        private const int KESSEL_CHART_BREITE = 600;
        private const int KESSEL_CHART_HOEHE = 380;
        private const int KESSEL_SPALTE_RECHTS = 656;

        /// <summary>
        /// Legt Diagramm und Umschalter im Heizkessel-Tab an — programmatisch nach dem
        /// Muster von <see cref="InitPufferspeicherRubrik"/>; Designer und .resx bleiben
        /// unangetastet.
        /// </summary>
        private void InitKesselChart()
        {
            if (listView_SimSPK == null || listView_SimSPK.Parent == null) return;

            KesselSeiteAnordnen();

            chart_Kessel = new System.Windows.Forms.DataVisualization.Charting.Chart();
            chart_Kessel.Name = "chart_Kessel";
            // Ein programmatisch erzeugtes Chart hat KEINE ChartArea - ChartManager.Init
            // steigt ohne sie wortlos aus (if (_chart.ChartAreas.Count == 0) return).
            chart_Kessel.ChartAreas.Add(new ChartArea("ChartArea_Kessel"));
            chart_Kessel.BackColor = Color.WhiteSmoke;
            chart_Kessel.BorderlineColor = Color.Transparent;
            // Feste Position, keine Rechts-Verankerung: Die Controls der TabPage werden
            // zur Laufzeit in das schmalere splitContainer_Parameter.Panel2 verschoben
            // (siehe Kommentar bei InitCsvExportButtons).
            chart_Kessel.Location = new Point(KESSEL_CHART_LINKS, KESSEL_CHART_OBEN);
            chart_Kessel.Size = new Size(KESSEL_CHART_BREITE, KESSEL_CHART_HOEHE);
            chart_Kessel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            chart_Kessel.Visible = false;   // erst nach einem Lauf mit Kessel
            listView_SimSPK.Parent.Controls.Add(chart_Kessel);

            checkBox_Kessel_sortiert = new CheckBox();
            checkBox_Kessel_sortiert.Name = "checkBox_Kessel_sortiert";
            // Derselbe Text wie die Bestands-Checkbox der Wärmepumpen-Seite
            // (checkBox_WP_sortiert, „sortiert"/„sorted" aus der Satelliten-.resx der
            // Form). Programmatische Steuerelemente kommen an diese .resx nicht heran und
            // nehmen den wortgleichen Katalogschlüssel — wie in NavigatorWaerme.
            checkBox_Kessel_sortiert.Text = MyResource.Resource.SIM_CHK_SORTIERT;
            checkBox_Kessel_sortiert.AutoSize = true;
            checkBox_Kessel_sortiert.Font = checkBox_WP_sortiert.Font;
            checkBox_Kessel_sortiert.ForeColor = Color.Black;
            // Rechts oben AM Diagramm, wie auf der Wärmepumpen-Seite. Dort liegt der
            // Umschalter über der Zeichenfläche; hier ist er ein Geschwister des Charts
            // und bekäme dessen Hintergrund nicht (WinForms-Transparenz nimmt den des
            // Elternelements) — deshalb dieselbe Farbe wie die Chartfläche.
            checkBox_Kessel_sortiert.BackColor = chart_Kessel.BackColor;
            checkBox_Kessel_sortiert.Location = new Point(chart_Kessel.Right - 90, chart_Kessel.Top + 8);
            checkBox_Kessel_sortiert.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            checkBox_Kessel_sortiert.Visible = false;
            checkBox_Kessel_sortiert.CheckedChanged += checkBox_Kessel_sortiert_CheckedChanged;
            listView_SimSPK.Parent.Controls.Add(checkBox_Kessel_sortiert);
            checkBox_Kessel_sortiert.BringToFront();

            InitKesselQuellwaerme();
        }

        // ====================================================================
        //  ETAPPE D4: Quellwärme der Kessel-Kaskade als Ergebnisgröße
        // ====================================================================
        //
        // Bis D5b war die Kessel-Kaskade in der Ergebnisansicht nur INDIREKT sichtbar - am
        // gesunkenen Brennstoffverbrauch (D5b-Restpunkt 3). Der Rechenkern führt die Größe
        // längst (SimulationSPK.Quellwaerme_gesamt); sie fehlte nur in der Ergebniszeile
        // und auf der Seite. Beides holt Etappe D4 nach: Schritt 10 der SchemaMigration
        // legt Tab_ErgebnisHeizkessel.Quellwaerme an, der Runner schreibt sie, und diese
        // Zeile zeigt sie.
        //
        // AUFBAU wie das Diagramm daneben: programmatisch, Designer und .resx bleiben
        // unangetastet. Die Zeile übernimmt Maße, Schrift und Farben ihrer Nachbarzeile
        // "Gasspitze" - so bleibt sie auch dann bündig, wenn der Entwurf sich ändert.

        private Label label_KesselQuellwaerme;
        private TextBox tb_KesselQuellwaerme;
        private Label label_KesselQuellwaermeEinheit;

        /// <summary>
        /// Legt die Ergebniszeile „Quellwärme aus der Kaskade" unter der Zeile
        /// „Gasspitze" an.
        ///
        /// Die Nachbarschaft wird zur Laufzeit GEMESSEN (Beschriftung links vom Feld,
        /// Einheit rechts davon) statt über Steuerelementnamen aufgelöst: Die Seite ist mit
        /// <see cref="KesselSeiteAnordnen"/> bereits umgeräumt, und eine Namensliste wäre
        /// bei der nächsten Designer-Änderung still falsch — dieselbe Begründung wie bei
        /// der Gruppenzuordnung dort.
        /// </summary>
        private void InitKesselQuellwaerme()
        {
            if (tabPage_Heizkessel == null || tb_Gasspitze == null) return;

            Control beschriftung = NachbarZeile(tb_Gasspitze, true);
            Control einheit = NachbarZeile(tb_Gasspitze, false);

            int y = tb_Gasspitze.Bottom + 9;

            tb_KesselQuellwaerme = new TextBox();
            tb_KesselQuellwaerme.Name = "tb_KesselQuellwaerme";
            tb_KesselQuellwaerme.ReadOnly = true;
            tb_KesselQuellwaerme.BackColor = tb_Gasspitze.BackColor;
            tb_KesselQuellwaerme.ForeColor = tb_Gasspitze.ForeColor;
            tb_KesselQuellwaerme.BorderStyle = tb_Gasspitze.BorderStyle;
            tb_KesselQuellwaerme.Font = tb_Gasspitze.Font;
            tb_KesselQuellwaerme.TextAlign = tb_Gasspitze.TextAlign;
            tb_KesselQuellwaerme.Bounds =
                new Rectangle(tb_Gasspitze.Left, y, tb_Gasspitze.Width, tb_Gasspitze.Height);
            tb_KesselQuellwaerme.Visible = false;
            tabPage_Heizkessel.Controls.Add(tb_KesselQuellwaerme);

            label_KesselQuellwaerme = new Label();
            label_KesselQuellwaerme.Name = "label_KesselQuellwaerme";
            label_KesselQuellwaerme.Text = MyResource.Resource.SIM_KESSEL_QUELLWAERME;
            label_KesselQuellwaerme.AutoSize = false;
            label_KesselQuellwaerme.Visible = false;
            if (beschriftung != null)
            {
                label_KesselQuellwaerme.Font = beschriftung.Font;
                label_KesselQuellwaerme.ForeColor = beschriftung.ForeColor;
                label_KesselQuellwaerme.BackColor = beschriftung.BackColor;
                label_KesselQuellwaerme.TextAlign = ContentAlignment.MiddleRight;
                label_KesselQuellwaerme.Bounds =
                    new Rectangle(beschriftung.Left, y, beschriftung.Width, tb_Gasspitze.Height);
            }
            else
            {
                label_KesselQuellwaerme.TextAlign = ContentAlignment.MiddleRight;
                label_KesselQuellwaerme.Bounds =
                    new Rectangle(tb_Gasspitze.Left - 250, y, 244, tb_Gasspitze.Height);
            }
            tabPage_Heizkessel.Controls.Add(label_KesselQuellwaerme);

            label_KesselQuellwaermeEinheit = new Label();
            label_KesselQuellwaermeEinheit.Name = "label_KesselQuellwaermeEinheit";
            label_KesselQuellwaermeEinheit.Text = MyResource.Resource.SIM_KESSEL_QUELLWAERME_EINHEIT;
            label_KesselQuellwaermeEinheit.AutoSize = true;
            label_KesselQuellwaermeEinheit.Visible = false;
            if (einheit != null)
            {
                label_KesselQuellwaermeEinheit.Font = einheit.Font;
                label_KesselQuellwaermeEinheit.ForeColor = einheit.ForeColor;
                label_KesselQuellwaermeEinheit.BackColor = einheit.BackColor;
                label_KesselQuellwaermeEinheit.Location = new Point(einheit.Left, y + 4);
            }
            else
            {
                label_KesselQuellwaermeEinheit.Location = new Point(tb_Gasspitze.Right + 8, y + 4);
            }
            tabPage_Heizkessel.Controls.Add(label_KesselQuellwaermeEinheit);

            _tooltipQuellwaerme.SetToolTip(tb_KesselQuellwaerme,
                Zeilenumbruch.Normalisieren(MyResource.Resource.SIM_KESSEL_QUELLWAERME_TIP));
            _tooltipQuellwaerme.SetToolTip(label_KesselQuellwaerme,
                Zeilenumbruch.Normalisieren(MyResource.Resource.SIM_KESSEL_QUELLWAERME_TIP));
        }

        private readonly ToolTip _tooltipQuellwaerme = new ToolTip();

        /// <summary>
        /// Das Steuerelement, das auf derselben Zeile LINKS (<paramref name="links"/>)
        /// bzw. RECHTS von <paramref name="feld"/> steht und ihm am nächsten liegt;
        /// <c>null</c>, wenn es keines gibt.
        /// </summary>
        private static Control NachbarZeile(Control feld, bool links)
        {
            if (feld == null || feld.Parent == null) return null;

            int mitte = feld.Top + feld.Height / 2;
            Control treffer = null;
            int abstand = int.MaxValue;

            foreach (Control c in feld.Parent.Controls)
            {
                if (ReferenceEquals(c, feld)) continue;
                if (mitte < c.Top || mitte > c.Bottom) continue;

                int d = links ? feld.Left - c.Right : c.Left - feld.Right;
                if (d < 0 || d >= abstand) continue;

                abstand = d;
                treffer = c;
            }

            return treffer;
        }

        /// <summary>
        /// Ordnet die Heizkessel-Seite nach dem Muster der Wärmepumpen-Seite:
        /// <b>links</b> die Grafikspalte (Diagramm oben, Kesseltabelle darunter),
        /// <b>rechts</b> die Ergebniszahlen (Bedarfs-/Deckungsfelder oben, Brennstoffblock
        /// darunter).
        ///
        /// Die Steuerelemente behalten ihre Anordnung INNERHALB ihrer Gruppe: Verschoben
        /// wird jede Gruppe als Ganzes um denselben Versatz, ermittelt aus ihrer heutigen
        /// umschließenden Ecke. Damit bleiben Abstände und Fluchten des Entwurfs erhalten,
        /// und weder Designer noch .resx werden angefasst (Projektregel).
        ///
        /// Die Gruppenzuordnung geht über die Entwurfsposition statt über eine Liste von
        /// 60 Steuerelementnamen: alles links von x=400 gehört zum Brennstoffblock, alles
        /// rechts davon zum Bedarfsblock; Tabelle und ihre Überschrift stehen namentlich
        /// darin. Eine Namensliste wäre bei jeder Designer-Änderung still unvollständig.
        ///
        /// Läuft genau EINMAL beim Aufbau des Formulars.
        /// </summary>
        private void KesselSeiteAnordnen()
        {
            TabPage seite = tabPage_Heizkessel;
            if (seite == null) return;

            List<Control> tabelle = new List<Control>();
            List<Control> brennstoff = new List<Control>();
            List<Control> bedarf = new List<Control>();

            foreach (Control c in seite.Controls)
            {
                if (c == listView_SimSPK || c.Name == "label80") tabelle.Add(c);
                else if (c.Left < 400) brennstoff.Add(c);
                else bedarf.Add(c);
            }

            seite.SuspendLayout();

            // Rechte Spalte: Bedarfs- und Deckungsfelder nach oben, Brennstoffblock darunter.
            GruppeVerschieben(bedarf, KESSEL_SPALTE_RECHTS, 24);
            GruppeVerschieben(brennstoff, KESSEL_SPALTE_RECHTS, 330);

            // Linke Spalte: Kesseltabelle unter das Diagramm; sie wird dafür etwas
            // schmaler als im Entwurf (Spaltenbreiten stellt AutoResizeColumns).
            GruppeVerschieben(tabelle, KESSEL_CHART_LINKS, KESSEL_CHART_OBEN + KESSEL_CHART_HOEHE + 12);
            listView_SimSPK.Width = KESSEL_CHART_BREITE;
            listView_SimSPK.Height = 212;

            seite.ResumeLayout();
        }

        /// <summary>
        /// Verschiebt eine Gruppe von Steuerelementen so, dass ihre umschließende linke
        /// obere Ecke auf (<paramref name="zielX"/>, <paramref name="zielY"/>) liegt.
        /// </summary>
        private static void GruppeVerschieben(List<Control> gruppe, int zielX, int zielY)
        {
            if (gruppe.Count == 0) return;

            int links = int.MaxValue, oben = int.MaxValue;
            foreach (Control c in gruppe)
            {
                if (c.Left < links) links = c.Left;
                if (c.Top < oben) oben = c.Top;
            }

            int dx = zielX - links, dy = zielY - oben;
            foreach (Control c in gruppe) c.Location = new Point(c.Left + dx, c.Top + dy);
        }

        /// <summary>
        /// Zeigt das Kessel-Diagramm — oder blendet es aus, wenn das Ergebnis keinen
        /// Kessel führt (Präsenzregel, siehe <see cref="ErgebnisPraesenz"/>).
        ///
        /// BEWUSST außerhalb von <c>if (sim.bSimulationKessel)</c> aufgerufen: Wird der
        /// Kessel in einem Folgelauf abgewählt, muss das Diagramm des Vorlaufs
        /// verschwinden statt stehenzubleiben — dieselbe Begründung wie bei
        /// <see cref="PufferspeicherErgebnisAnzeigen"/>.
        /// </summary>
        private void KesselErgebnisAnzeigen()
        {
            if (chart_Kessel == null) return;

            bool zeigen = sim != null && sim.simulation_spk != null
                          && ErgebnisPraesenz.Ermitteln(sim).Heizkessel;

            _kesselChartAktiv = zeigen;
            chart_Kessel.Visible = zeigen;
            if (checkBox_Kessel_sortiert != null) checkBox_Kessel_sortiert.Visible = zeigen;
            if (btn_CsvExportKessel != null) btn_CsvExportKessel.Visible = zeigen;

            // ETAPPE D4: Die Quellwärme-Zeile folgt derselben Präsenzregel wie das
            // Diagramm - ohne Kessel im Ergebnis hat sie nichts zu sagen.
            if (tb_KesselQuellwaerme != null) tb_KesselQuellwaerme.Visible = zeigen;
            if (label_KesselQuellwaerme != null) label_KesselQuellwaerme.Visible = zeigen;
            if (label_KesselQuellwaermeEinheit != null)
                label_KesselQuellwaermeEinheit.Visible = zeigen;

            if (!zeigen)
            {
                // Serien des Vorlaufs abräumen, damit kein Bild ohne Bezug stehenbleibt.
                if (_chartKesselManager != null) _chartKesselManager.HardReset();
                return;
            }

            if (_chartKesselManager == null) _chartKesselManager = new ChartManager(chart_Kessel);
            KesselSerienAufbauen();
            KesselBrennstoffZeilenAnpassen();
        }

        /// <summary>
        /// Baut Diagrammkonfiguration und Serien der Heizkessel-Seite auf — in der
        /// Darstellungsform, die der Umschalter „sortiert" vorgibt. Der Ablauf folgt der
        /// Wärmepumpen-Seite (<see cref="checkBox_WP_sortiert_CheckedChanged"/>):
        /// <c>XAxisAsNumber</c> setzen, <c>HardReset()</c>, <c>Init()</c>, Serien neu.
        ///
        /// DREI Serien — hier wird nichts nachgerechnet und nichts von der
        /// Wärmepumpen-Seite übernommen:
        ///   Wärmeproduktion Heizkessel = <c>simulation_spk.Kesselleistung_stuendlich</c>,
        ///                  die Summe über ALLE Kessel des Projekts und nur über sie.
        ///                  SÄULEN, unten.
        ///   Restwärme    = <c>simulation_spk.Restwaerme</c>, Linie darüber.
        ///   Wärmebedarf gesamt = <c>simulation_Waermebedarf.Waermebedarf</c>, der
        ///                  Projektwärmebedarf. LINIE, zuletzt angelegt und damit ganz
        ///                  oben (Begründung der Bezugsgröße im Blockkommentar oben).
        ///
        /// <b>Warum der Bedarf eine LINIE über den Säulen ist und keine Fläche darunter.</b>
        /// Als Fläche stand er hinter den Produktionssäulen: In jeder Stunde, in der die
        /// Kessel den anliegenden Bedarf deckten, war die Fläche vollständig von den
        /// Säulen überdeckt — Blau verschluckte Rot (Sichttest-Befund „die Überlappung
        /// soll verbessert werden, blau und rot beide sichtbar"). Als Linie ÜBER dem
        /// Gefüllten bleiben beide lesbar: Bei Deckung liegt die rote Linie auf der
        /// blauen Oberkante, bei Unterdeckung darüber. Dasselbe Muster trägt in
        /// NavigatorWaerme die Kontrolllinie „Gesamt".
        ///
        /// <b>Warum der Bedarf EINE Serie ist</b> (anders als auf der Wärmepumpen-Seite,
        /// die ihn in Heizwärme und Warmwasser teilt): Der Kessel bekommt vom klassischen
        /// Rechenweg einen einzigen Bedarfsvektor gereicht; welcher Anteil davon
        /// Warmwasser ist, sagt er an dieser Stelle nicht. Eine Aufteilung wäre hier eine
        /// Behauptung über den Warmwasserkanal, die die Engine nicht deckt.
        ///
        /// <b>Warum EINE Stapelgruppe:</b> Zwei <c>StackedGroupName</c>-Gruppen stellt
        /// MS-Chart bei Säulen NEBENEINANDER; bei 8760 Punkten auf 600 Bildpunkten ist
        /// eine Säule 0,07 Punkte breit, halbiert verschwindet die Produktion in der
        /// Rasterung — der frühere Befund „die Wärmeproduktion fehlt in der Grafik". Die
        /// Bedarfslinie belegt keinen Säulenplatz; die Produktionssäulen bekommen die
        /// volle Breite.
        /// </summary>
        private void KesselSerienAufbauen()
        {
            if (_chartKesselManager == null || sim == null || sim.simulation_spk == null) return;
            if (simulation_Waermebedarf == null) return;

            bool sortiert = checkBox_Kessel_sortiert != null && checkBox_Kessel_sortiert.Checked;

            // Der GESAMTbedarf des Projekts, nicht der Stufeneingang der Kessel — dasselbe
            // Feld, aus dem die Seite „Energiebedarf" ihren Gesamtwert bildet.
            float[] bedarf = simulation_Waermebedarf.Waermebedarf;
            float[] produktion = sim.simulation_spk.Kesselleistung_stuendlich;
            float[] rest = sim.simulation_spk.Restwaerme;

            ChartManager cm = _chartKesselManager;
            cm.YMaxValue = Math.Max(bedarf.Max(), produktion.Max()) + 1;
            cm.YMinValue = 0;
            cm.XAxisAsNumber = sortiert;
            cm.XAxisTitle = sortiert
                ? MyResource.Resource.CHART_ACHSE_JAHRESSTUNDEN
                : MyResource.Resource.CHART_ACHSE_MONATE;
            cm.YAxisTitle = MyResource.Resource.CHART_ACHSE_WAERMELAST;
            cm.toolTipUnit = "kW";
            cm.ChartTitle = MyResource.Resource.CHART_TITEL_WAERMELAST_JAHRESGANGLINIE;
            cm.MitLegende = true;
            cm.MitChartBorder = true;
            cm.MaxXVALUE = 8760;
            cm.MitViertelStunde = false;

            cm.HardReset();
            cm.Init();

            // Reihenfolge = Zeichenreihenfolge: MS-Chart zeichnet in der Reihenfolge der
            // Series-Collection, das Zuletztangelegte liegt oben. Erst die Produktion,
            // dann die Restwärme, ZULETZT der Bedarf.
            SerieAnlegen(cm, S_WAERMEPRODUKTION, MyResource.Resource.CHART_LEGENDE_WAERMEPRODUKTION_HEIZKESSEL,
                         Color.Blue, GanglinienDarstellung.Anzeigewerte(produktion, sortiert),
                         GanglinienDarstellung.Stapeltyp(sortiert), "Produktion");

            // DAUERLINIE: Dort ist die Produktion keine Säule mehr, sondern eine Linie
            // (GanglinienDarstellung.Stapeltyp) — und in einem Projekt, dessen Kessel den
            // ganzen Bedarf decken, ist ihre Dauerlinie PUNKTGLEICH mit der des Bedarfs.
            // Zwei gleich breite Linien übereinander ergäben genau wieder das Bild, das
            // der Sichttest bemängelt hat. Die untere wird deshalb breiter gezeichnet als
            // die obere; von Blau bleibt dann links und rechts der roten Linie ein Rand
            // stehen. Strichelung wäre der übliche zweite Weg, ist aber bei FastLine
            // wirkungslos (BorderDashStyle greift dort nicht) — siehe NavigatorWaerme.
            if (sortiert) cm._chart.Series[S_WAERMEPRODUKTION].BorderWidth = 4;

            SerieAnlegen(cm, S_RESTWAERME, MyResource.Resource.CHART_SEGMENT_RESTWAERME,
                         Color.Green, GanglinienDarstellung.Anzeigewerte(rest, sortiert));

            // Der Bedarf ZULETZT und damit ganz oben — er ist die Bezugsgröße, gegen die
            // alles Übrige gelesen wird, und darf von keiner Erzeugerserie verdeckt
            // werden. Volle Deckkraft statt der früheren 90/255: Eine Linie hat keine
            // Fläche, hinter der etwas durchscheinen müsste.
            SerieAnlegen(cm, S_WAERMEBEDARF, MyResource.Resource.CHART_LEGENDE_WAERMEBEDARF_GESAMT,
                         Color.Red, GanglinienDarstellung.Anzeigewerte(bedarf, sortiert),
                         SeriesChartType.FastLine);

            cm._chart.Invalidate();
        }

        // ====================================================================
        //  Heizkessel-Seite: nur verwendete Brennstoffe zeigen
        // ====================================================================
        //
        // Der Block „Brennstoffverbrauch der Spitzenkessel" führte ALLE zehn Brennstoffe
        // untereinander auf — in einem Projekt mit einem Gaskessel standen dort neun
        // Zeilen „0,00" für Öl, Koks, Kohle, Holz, Pellets, Rapsöl, Strom, tierische Fette
        // und Sonstiges. Dieselbe Regel wie im Übersicht-Reiter (siehe ErgebnisPraesenz):
        // sichtbar ist eine Zeile, wenn ihr JAHRESWERT > 0 ist ODER ein Kessel des
        // Projekts diesen Brennstoff führt — so bleibt der vorhandene Gaskessel mit
        // 0-Ergebnis sichtbar (die Antwort auf „warum verbraucht mein Kessel nichts?"),
        // und Nichtvorhandenes verschwindet. Die übrigen Zeilen rücken nach.
        //
        // Die Spalten der Kesseltabelle darunter bleiben unangetastet: „Brennstoffe" und
        // „Öl" sind dort die zwei Nutzwärmekanäle der Engine (s_waerme_Gas_Spk /
        // s_waerme_Oel_Spk), keine Brennstoffliste.

        private List<AnkerZeile> _kesselBrennstoffZeilen;
        private Func<double>[] _kesselBrennstoffWerte;
        private int[][] _kesselBrennstoffIds;

        /// <summary>
        /// Baut die Zeilenbeschreibung des Brennstoffblocks einmalig auf — NACH
        /// <see cref="KesselSeiteAnordnen"/>, damit die gesicherten Anker die Positionen
        /// der neuen Spalte sind und nicht die des Entwurfs.
        /// </summary>
        private void KesselBrennstoffZeilenVorbereiten()
        {
            if (_kesselBrennstoffZeilen != null) return;

            // Reihenfolge = Reihenfolge im Entwurf (von oben nach unten).
            Control[][] felder =
            {
                new Control[] { label61, tb_Gasverbrauch,      label65 },
                new Control[] { label66, tb_Oelverbrauch,      label67 },
                new Control[] { label68, tb_Koks,              label69 },
                new Control[] { label74, tb_Kohle,             label75 },
                new Control[] { label72, tb_Holzverbrauch,     label73 },
                new Control[] { label84, tb_Pellets,           label85 },
                new Control[] { label70, tb_Rapsoelverbrauch,  label71 },
                new Control[] { label76, tb_Stromverbrauch,    label77 },
                new Control[] { label86, tb_TierischeFette,    label87 },
                new Control[] { label78, tb_Sonstigverbrauch,  label79 }
            };

            // Brennstoff-Kennungen je Zeile. Sie spiegeln die Buchung der Engine
            // (SimulationSPK.Bilanz_und_Nutzungsgrad) — dort sind Brennstoff_Art und die
            // Zuordnung privat und ohne Engine-Eingriff nicht erreichbar. Ändert sich die
            // Buchung dort, gehört diese Tabelle nachgezogen (offener Punkt).
            _kesselBrennstoffIds = new[]
            {
                new[] { 1, 2, 3, 4, 5, 14 },                    // Gas (14 = Biogas)
                new[] { 6, 7, 8, 9, 18, 19, 20, 21, 22 },       // Öl
                new[] { 10 },                                   // Koks
                new[] { 11 },                                   // Kohle
                new[] { 12 },                                   // Holz
                new[] { 15 },                                   // Pellets
                new[] { 16 },                                   // Rapsöl
                new[] { 13 },                                   // Elektrowärme
                new[] { 17 },                                   // Tierische Fette
                new int[0]                                      // Sonstige: alles Übrige
            };

            _kesselBrennstoffWerte = new Func<double>[]
            {
                () => sim.simulation_spk.Gasverbrauch_SPK,
                () => sim.simulation_spk.Oelverbrauch_SPK,
                () => sim.simulation_spk.Koks_SPK,
                () => sim.simulation_spk.Kohle_SPK,
                () => sim.simulation_spk.Holzverbrauch_SPK,
                () => sim.simulation_spk.Pellets_SPK,
                () => sim.simulation_spk.Rapsoelverbrauch_SPK,
                () => sim.simulation_spk.Stromverbrauch_Spk,
                () => sim.simulation_spk.TierischeFette_SPK,
                () => sim.simulation_spk.Sonstigverbrauch_SPK
            };

            _kesselBrennstoffZeilen = new List<AnkerZeile>();
            foreach (Control[] zeile in felder)
            {
                AnkerZeile z = new AnkerZeile();
                AnkerErfassen(z, zeile);
                _kesselBrennstoffZeilen.Add(z);
            }
        }

        /// <summary>
        /// Blendet die Zeilen nicht verwendeter Brennstoffe aus und lässt die übrigen
        /// nachrücken. Reine Anzeige.
        /// </summary>
        private void KesselBrennstoffZeilenAnpassen()
        {
            if (sim == null || sim.simulation_spk == null) return;

            KesselBrennstoffZeilenVorbereiten();

            System.Collections.Generic.HashSet<int> arten = KesselBrennstoffartenLesen(m_ID_Projekt);

            // "Sonstige" fängt jede Kennung auf, die keine der übrigen Zeilen führt -
            // dieselbe else-Verzweigung wie in der Engine.
            System.Collections.Generic.HashSet<int> bekannt = new System.Collections.Generic.HashSet<int>();
            foreach (int[] ids in _kesselBrennstoffIds) foreach (int id in ids) bekannt.Add(id);

            // Rückfall: Kennt die Datenbank keinen Brennstoff (Abfrage fehlgeschlagen,
            // Anlage ohne Katalogeintrag) UND trägt kein Feld einen Wert, bleibt der Block
            // vollständig stehen. Ein leerer Block wäre die schlechtere Auskunft.
            bool nichtsBekannt = arten.Count == 0;
            if (nichtsBekannt)
                foreach (Func<double> w in _kesselBrennstoffWerte) if (w() > 0) { nichtsBekannt = false; break; }

            for (int i = 0; i < _kesselBrennstoffZeilen.Count; i++)
            {
                if (nichtsBekannt) { _kesselBrennstoffZeilen[i].Sichtbar = true; continue; }

                bool wert = _kesselBrennstoffWerte[i]() > 0;

                bool vorhanden;
                if (_kesselBrennstoffIds[i].Length == 0)
                {
                    vorhanden = false;
                    foreach (int a in arten) if (!bekannt.Contains(a)) { vorhanden = true; break; }
                }
                else
                {
                    vorhanden = false;
                    foreach (int id in _kesselBrennstoffIds[i]) if (arten.Contains(id)) { vorhanden = true; break; }
                }

                _kesselBrennstoffZeilen[i].Sichtbar = wert || vorhanden;
            }

            tabPage_Heizkessel.SuspendLayout();
            AnkerAnordnen(_kesselBrennstoffZeilen);
            tabPage_Heizkessel.ResumeLayout();
        }

        /// <summary>
        /// Die Brennstoff-Kennungen der Kessel dieses Projekts (<c>Tab_Heizkessel.Brennstoff</c>,
        /// dieselbe Quelle, aus der die Engine <c>Brennstoff_Art</c> liest).
        ///
        /// <b>Der Verbund mit <c>Tab_Energieanlagen</c> ist nicht kosmetisch.</b>
        /// <c>Tab_Heizkessel</c> führt je Projekt AUCH die Katalogauswahlen, die nie eine
        /// Anlage geworden sind — Projekt 1023 hat dort 17 Zeilen, aber genau EINEN
        /// Kessel. Ohne den Verbund erschienen die Brennstoffe aller Karteileichen als
        /// „vorhanden" (im Beispiel eine Zeile Stromverbrauch wegen eines nie eingebauten
        /// Elektrokessels). Verbunden wird über <c>Bezeichner</c> + <c>ID_Projekt</c> —
        /// genau der Weg, auf dem <c>SimulationSPK</c> seine Kesseldaten sucht.
        ///
        /// Dialogfrei über <see cref="StilleDb"/> wie <see cref="ErgebnisPraesenz"/>:
        /// Schlägt die Abfrage fehl, bleibt die Menge leer, und der Aufrufer fällt auf die
        /// vollständige Anzeige zurück.
        /// </summary>
        private static System.Collections.Generic.HashSet<int> KesselBrennstoffartenLesen(int idProjekt)
        {
            System.Collections.Generic.HashSet<int> arten = new System.Collections.Generic.HashSet<int>();
            if (idProjekt <= 0) return arten;

            System.Data.DataTable dt = StilleDb.Tabelle(
                "SELECT DISTINCT k.Brennstoff FROM Tab_Heizkessel AS k " +
                "INNER JOIN Tab_Energieanlagen AS a ON k.Bezeichner = a.Bezeichner " +
                "WHERE k.ID_Projekt = ? AND a.ID_Projekt = ? AND a.ID_Type = ?",
                StilleDb.Par("@proj1", System.Data.OleDb.OleDbType.Integer, idProjekt),
                StilleDb.Par("@proj2", System.Data.OleDb.OleDbType.Integer, idProjekt),
                StilleDb.Par("@typ", System.Data.OleDb.OleDbType.Integer, WizardItemClass.KESSEL_TYP));

            if (dt == null) return arten;

            foreach (System.Data.DataRow r in dt.Rows)
            {
                int a = StilleDb.Zahl(StilleDb.Feld(r, "Brennstoff"), -1);
                if (a >= 0) arten.Add(a);
            }
            return arten;
        }

        /// <summary>
        /// Umschalter „sortiert" der Heizkessel-Seite: Jahresganglinie ↔ Jahresdauerlinie.
        /// Baut das Diagramm neu auf; an den Vektoren ändert sich nichts (der
        /// chronologische Zweig zeigt wieder die Originalwerte).
        /// </summary>
        private void checkBox_Kessel_sortiert_CheckedChanged(object sender, EventArgs e)
        {
            if (!_kesselChartAktiv) return;
            KesselSerienAufbauen();
        }

        /// <summary>
        /// CSV-Export Bereich Heizkessel: Zeitstempel; Außentemperatur; Wärmebedarf
        /// gesamt; Wärmebedarf Kesselstufe; Heizkessel; Restwärme (Stundenwerte).
        ///
        /// Die erste Bedarfsspalte ist die Bedarfslinie des Diagramms (Projektbedarf),
        /// die zweite der Stufeneingang der Kessel — die Größe, die das Diagramm bis zum
        /// Sichttest vom 16.08.2026 zeigte und die als Zahl weiter auf der Seite steht.
        /// Sie bleibt in der Datei, weil sie die Kesselstunden erklärt; die getrennten
        /// Spaltenköpfe sagen, welche der beiden gemeint ist.
        ///
        /// Immer CHRONOLOGISCH und immer aus den Rohvektoren — „sortiert" ist eine
        /// Darstellungsform, keine andere Datenlage (gleiche Regel wie in NavigatorWaerme).
        /// </summary>
        private void btn_CsvExportKessel_Click(object sender, EventArgs e)
        {
            if (sim == null || !sim.bSimulationKessel || sim.simulation_spk == null)
            {
                MessageBox.Show(MyResource.Resource.SIM_MSG_KEINE_DATEN_HEIZKESSEL,
                    MyResource.Resource.SIM_BTN_CSV_EXPORT, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<CsvSpalte> spalten = new List<CsvSpalte>();
            spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_WAERMEBEDARF_GESAMT, simulation_Waermebedarf.Waermebedarf));
            spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_WAERMEBEDARF_KESSELSTUFE, sim.simulation_spk.Waermebedarf));
            spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_HEIZKESSEL, sim.simulation_spk.Kesselleistung_stuendlich));
            spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_RESTWAERME, sim.simulation_spk.Restwaerme));

            CsvExportClass.Export(string.Format(MyResource.Resource.CHART_DATEI_HEIZKESSEL, m_ID_Projekt),
                simulation_Waermebedarf.Stundentemperatur, spalten, false);
        }

        // ====================================================================
        //  BHKW-Seite: Nachzug auf den Speicherstufen-Rechenweg
        // ====================================================================
        //
        // AUSGANGSLAGE (Live-Test 17.08.2026, zwei Meldungen des Anwenders).
        //
        //  (1) „Der Pufferspeicher wird noch nicht berücksichtigt." Gemessen an Projekt
        //      1018 („BHKW Test München") legt das BHKW 14,32 von 25,61 MWh — also 56 %
        //      seiner Jahresproduktion — in den Pufferspeicher, und 14,11 MWh deckt es
        //      über dessen Entladung. KEINE dieser drei Zahlen stand auf der Seite. Die
        //      Restwärme entstand hier als Vektordifferenz „Bedarf − Produktion"
        //      (SubVectors) und der Deckungsgrad als „Produktion / Projektbedarf" — beides
        //      die Altpfad-Formeln aus Konzept 6.5, die genau dann falsch werden, wenn ein
        //      Speicher im Bilanzraum steht: Geladene Wärme deckt noch keinen Bedarf, und
        //      entladene Wärme deckt Bedarf, ohne in der Produktionsstunde zu erscheinen.
        //      Der Rechenkern führt die richtigen Größen längst
        //      (SimulationBHKW.Direktdeckung_gesamt, Speicherentladung_Anteil,
        //      Speicherladung_stuendlich/_gesamt, waermerestbedarf, Waermebedarf_gesamt);
        //      Tab_ErgebnisBHKW rechnet damit seit Paket 6 (SimulationRunner:381-448),
        //      nur die Seite tat es nicht — offener Punkt 2 des Pakets BHKW-Regulär.
        //
        //  (2) „Die Darstellung ‚sortiert' fehlt." Sie fehlte nicht nur — sie war die
        //      EINZIGE. Die Seite sortierte Bedarf und Produktion unbedingt absteigend
        //      (OrderByDescending, jede Serie für sich), trug darüber aber den Titel
        //      „Wärmelast Jahresganglinie" und beschriftete die Achse mit Jahresstunden.
        //      Damit erklärt sich der gemeldete „harte Abfall auf 0 nach etwa 1460 h":
        //      Das BHKW hat in 1018 genau 1505 Stunden mit Produktion > 0 — die Dauerlinie
        //      MUSS dort auf 0 fallen. Ein Speicherfehler war das nicht, wohl aber eine
        //      Darstellung, die als Ganglinie gelesen werden musste und keine war.
        //
        // BEDIENMUSTER = HEIZKESSEL-SEITE, Steuerelement für Steuerelement:
        //   - eine programmatische CheckBox mit demselben Text (MyResource.SIM_CHK_SORTIERT
        //     „sortiert"), derselben Schrift (checkBox_WP_sortiert.Font), derselben
        //     Position (rechts oben AM Diagramm) und derselben Sichtbarkeitsregel
        //     (nur wenn das Ergebnis die Komponente führt, siehe ErgebnisPraesenz),
        //   - CHRONOLOGISCH ist der Grundzustand, „sortiert" die Umschaltung,
        //   - Sortierung, Serientyp und Achsenwechsel kommen ausschließlich aus
        //     GanglinienDarstellung (Anzeigewerte/Stapeltyp) — dieselbe Regel, die auch
        //     Wärmepumpe, Heizkessel und die beiden Navigatoren benutzen. Damit ist jede
        //     Serie für sich absteigend sortiert, wie es das Vorbild tut.
        //
        // BEZUGSGRÖSSE DES BEDARFS: hier bleibt es beim STUFENEINGANG
        // (SimulationBHKW.waermebedarf) und NICHT beim Projektbedarf, den die
        // Heizkessel-Seite zeigt. Grund: Die Restwärme-Ganglinie des Kerns ist
        // stundenweise als „Stufeneingang − Direktdeckung − zugerechnete Entladung"
        // definiert (SimulationBHKW.Stunde_Ende). Mit dem Projektbedarf als Linie stünde
        // im Bild eine Bezugsgröße, gegen die die beiden anderen Serien nicht gerechnet
        // sind — die Summe Rest + Direktdeckung + Entladung ginge sichtbar nicht auf. Der
        // PROJEKTbedarf bleibt die Bezugsgröße des Deckungsgrades (so weist ihn der Kern
        // aus) und steht als Zahl auf der Seite. Offen dokumentiert.

        /// <summary>
        /// Legt den Umschalter „sortiert" am BHKW-Diagramm und die zwei
        /// Speicher-Kennzahlzeilen an — programmatisch nach dem Muster von
        /// <see cref="InitKesselChart"/> bzw. <see cref="InitKesselQuellwaerme"/>;
        /// Designer und .resx der Form bleiben unangetastet.
        /// </summary>
        private void InitBhkwChart()
        {
            if (chart_BHKW_Waerme == null || chart_BHKW_Waerme.Parent == null) return;

            checkBox_BHKW_sortiert = new CheckBox();
            checkBox_BHKW_sortiert.Name = "checkBox_BHKW_sortiert";
            // Wortgleich mit der Heizkessel- und der Wärmepumpen-Seite.
            checkBox_BHKW_sortiert.Text = MyResource.Resource.SIM_CHK_SORTIERT;
            checkBox_BHKW_sortiert.AutoSize = true;
            checkBox_BHKW_sortiert.Font = checkBox_WP_sortiert.Font;
            checkBox_BHKW_sortiert.ForeColor = Color.Black;
            // Der Umschalter ist ein GESCHWISTER des Diagramms und bekäme dessen
            // Hintergrund nicht (WinForms-Transparenz nimmt den des Elternelements) —
            // deshalb dieselbe Farbe wie die Chartfläche, wie beim Kessel.
            checkBox_BHKW_sortiert.BackColor = chart_BHKW_Waerme.BackColor;
            checkBox_BHKW_sortiert.Location =
                new Point(chart_BHKW_Waerme.Right - 90, chart_BHKW_Waerme.Top + 8);
            checkBox_BHKW_sortiert.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            checkBox_BHKW_sortiert.Visible = false;   // erst nach einem Lauf mit BHKW
            checkBox_BHKW_sortiert.CheckedChanged += checkBox_BHKW_sortiert_CheckedChanged;
            chart_BHKW_Waerme.Parent.Controls.Add(checkBox_BHKW_sortiert);
            checkBox_BHKW_sortiert.BringToFront();

            InitBhkwSpeicherzeilen();
        }

        /// <summary>
        /// Legt die zwei Kennzahlzeilen „davon in den Speicher" und „aus dem Speicher
        /// gedeckt" unter der Zeile „Wärmeüberschuß" an und rückt die darunter liegenden
        /// Zeilen der rechten Spalte nach.
        ///
        /// Maße, Schrift und Farben kommen von der Nachbarzeile
        /// (<see cref="NachbarZeile"/>), nicht aus einer Namensliste — dieselbe Begründung
        /// wie bei <see cref="InitKesselQuellwaerme"/>: Die Zeilen bleiben bündig, auch
        /// wenn der Entwurf sich verschiebt.
        ///
        /// <b>Warum die Seite nachrücken muss.</b> Der Wärmeblock der rechten Spalte endet
        /// im Entwurf bei y≈205, die nächste Zeile („Stromproduktion") beginnt bei y=236.
        /// Für EINE Zeile ist Platz, für zwei nicht. Die Zeilen darunter wandern deshalb um
        /// zwei Zeilenhöhen nach unten — derselbe Eingriff wie das „+32" der
        /// Pufferspeicher-Maske, nur zur Laufzeit und damit ohne .resx-Änderung. Der
        /// unterste Block (Brennstoffverbrauch) endet danach bei y≈696 und bleibt innerhalb
        /// der Entwurfshöhe der Seite (721).
        /// </summary>
        private void InitBhkwSpeicherzeilen()
        {
            if (tabPage_BHKW == null || textBox_Waermeueberschuss_BHKW == null) return;

            TextBox muster = textBox_Waermeueberschuss_BHKW;
            Control beschriftung = NachbarZeile(muster, true);
            Control einheit = NachbarZeile(muster, false);

            int schritt = muster.Height + 7;          // Zeilenabstand des Entwurfs (25 + 7)
            int y1 = muster.Bottom + 7;
            int y2 = y1 + schritt;

            // Erst nachrücken, dann einfügen: Sonst schöbe der Versatz die neuen Zeilen
            // gleich wieder mit nach unten.
            BhkwZeilenNachruecken(muster.Bottom + 4, 2 * schritt);

            BhkwKennzahlZeile(muster, beschriftung, einheit, y1,
                              "tb_BhkwSpeicherladung",
                              TextAusFormResx("SIMDET_BHKW_SPEICHERLADUNG", "davon in den Speicher:"),
                              out label_BhkwSpeicherladung, out tb_BhkwSpeicherladung,
                              out label_BhkwSpeicherladungEinheit);

            BhkwKennzahlZeile(muster, beschriftung, einheit, y2,
                              "tb_BhkwSpeicherdeckung",
                              TextAusFormResx("SIMDET_BHKW_SPEICHERDECKUNG", "aus dem Speicher gedeckt:"),
                              out label_BhkwSpeicherdeckung, out tb_BhkwSpeicherdeckung,
                              out label_BhkwSpeicherdeckungEinheit);

            InitBhkwVbhZeile();
        }

        // ====================================================================
        //  PAKET E1 (Konzept 4.4) — Wärmebedarf je Bedarfsart auf der Bedarfsseite
        // ====================================================================

        /// <summary>Überschrift und die drei Wertfelder des Kanalblocks; null vor <see cref="InitBedarfKanalzeilen"/>.</summary>
        private Label label_BedarfKanalKopf;
        private Label[] label_BedarfKanal;
        private TextBox[] tb_BedarfKanal;
        private Label[] label_BedarfKanalEinheit;

        /// <summary>
        /// Legt unter „Gesamter Wärmebedarf" drei Kennzahlzeilen für die drei
        /// Bedarfskanäle an — Heizung, Brauchwasser, Prozesswärme (Paket E1,
        /// Konzept 4.4). Sie addieren sich zur Zeile darüber.
        ///
        /// <b>Warum zur Laufzeit und nicht im Designer.</b> Dieselbe Begründung wie bei
        /// <see cref="InitBhkwSpeicherzeilen"/> und <see cref="InitKesselQuellwaerme"/>:
        /// Designer und .resx der Form bleiben unangetastet, die Texte kommen aus
        /// <c>MyResource.Resource</c> und sind damit in beiden Sprachen gepflegt
        /// (Drei-Schichten-Regel). Die Beschriftungen sind die KANAL_*_ANZEIGE-Schlüssel
        /// aus Paket K1 — es gibt genau einen Katalogeintrag je Kanal.
        ///
        /// <b>Warum hier NICHTS nachrücken muss.</b> Die linke Spalte der Bedarfsseite
        /// endet mit „Gesamter Wärmebedarf" bei y≈548; die Seite ist 721 hoch, darunter
        /// steht in dieser Spalte kein Steuerelement mehr. Der Block belegt y 566…650 und
        /// bleibt damit innerhalb des Entwurfs — anders als auf der BHKW-Seite ist kein
        /// Verschieben nötig.
        ///
        /// <b>Anker bewusst Standard (Top|Left).</b> Ein Vierseitenanker an einem im
        /// Konstruktor eingehängten Steuerelement blähte sich beim ersten Layout gegen die
        /// Entwurfsgröße der TabPage auf — dieselbe Falle wie in
        /// <c>Form_PeakShaving.ChartAufbauen</c>.
        /// </summary>
        private void InitBedarfKanalzeilen()
        {
            if (tabPage_Bedarf == null || textBox_Gesamt_Waermebedarf == null) return;

            TextBox muster = textBox_Gesamt_Waermebedarf;
            Control beschriftung = NachbarZeile(muster, true);    // „Gesamter Wärmebedarf"
            Control einheit = NachbarZeile(muster, false);        // „MWh"

            int schritt = muster.Height + 6;
            int oben = muster.Bottom + 14;

            label_BedarfKanalKopf = new Label();
            label_BedarfKanalKopf.Name = "label_BedarfKanalKopf";
            label_BedarfKanalKopf.Text = MyResource.Resource.SIM_LABEL_BEDARF_JE_KANAL;
            label_BedarfKanalKopf.AutoSize = true;
            label_BedarfKanalKopf.Visible = false;
            if (beschriftung != null)
            {
                label_BedarfKanalKopf.Font = beschriftung.Font;
                label_BedarfKanalKopf.ForeColor = beschriftung.ForeColor;
                label_BedarfKanalKopf.BackColor = beschriftung.BackColor;
            }
            label_BedarfKanalKopf.Location = new Point(muster.Left - 4, oben);
            tabPage_Bedarf.Controls.Add(label_BedarfKanalKopf);

            string[] namen =
            {
                MyResource.Resource.KANAL_HEIZUNG_ANZEIGE,
                MyResource.Resource.KANAL_BRAUCHWASSER_ANZEIGE,
                MyResource.Resource.KANAL_PROZESS_ANZEIGE
            };

            label_BedarfKanal = new Label[Kanal.ANZAHL];
            tb_BedarfKanal = new TextBox[Kanal.ANZAHL];
            label_BedarfKanalEinheit = new Label[Kanal.ANZAHL];

            // Die Beschriftung steht LINKS neben dem Feld (nicht darüber wie bei der
            // Summenzeile): drei Zeilen mit je einer Überschrift darüber wären eine
            // Neugestaltung des Blocks, die hier nicht gewollt ist.
            int breiteLabel = 0;
            foreach (string n in namen)
                breiteLabel = Math.Max(breiteLabel,
                                       BreiteMessen(n, label_BedarfKanalKopf.Font, 90));

            for (int k = 0; k < Kanal.ANZAHL; k++)
            {
                int y = oben + 22 + k * schritt;

                Label lbl = new Label();
                lbl.Name = "label_BedarfKanal" + k;
                lbl.Text = namen[k];
                lbl.AutoSize = false;
                lbl.TextAlign = ContentAlignment.MiddleLeft;
                lbl.Visible = false;
                if (beschriftung != null)
                {
                    lbl.Font = beschriftung.Font;
                    lbl.ForeColor = beschriftung.ForeColor;
                    lbl.BackColor = beschriftung.BackColor;
                }
                lbl.Bounds = new Rectangle(muster.Left, y, breiteLabel, muster.Height);
                tabPage_Bedarf.Controls.Add(lbl);
                label_BedarfKanal[k] = lbl;

                TextBox feld = new TextBox();
                feld.Name = "tb_BedarfKanal" + k;
                feld.ReadOnly = true;
                feld.BackColor = muster.BackColor;
                feld.ForeColor = muster.ForeColor;
                feld.BorderStyle = muster.BorderStyle;
                feld.Font = muster.Font;
                feld.TextAlign = muster.TextAlign;
                feld.Bounds = new Rectangle(muster.Left + breiteLabel + 8, y,
                                            muster.Width, muster.Height);
                feld.Visible = false;
                tabPage_Bedarf.Controls.Add(feld);
                tb_BedarfKanal[k] = feld;

                Label lblE = new Label();
                lblE.Name = "label_BedarfKanalEinheit" + k;
                // Einheit ÜBERNOMMEN, nicht neu getextet — so kann sie nicht von der
                // Summenzeile abweichen (Muster BhkwKennzahlZeile).
                lblE.Text = (einheit != null) ? einheit.Text : "MWh";
                lblE.AutoSize = true;
                lblE.Visible = false;
                if (einheit != null)
                {
                    lblE.Font = einheit.Font;
                    lblE.ForeColor = einheit.ForeColor;
                    lblE.BackColor = einheit.BackColor;
                }
                lblE.Location = new Point(feld.Right + 8, y + 4);
                tabPage_Bedarf.Controls.Add(lblE);
                label_BedarfKanalEinheit[k] = lblE;
            }
        }

        /// <summary>
        /// Schreibt die drei Kanalwerte und blendet den Block ein (Paket E1).
        /// Quelle ist <see cref="SimulationRunner.BedarfJeKanal"/> — dieselbe Methode,
        /// aus der auch die Persistenz ihre Spalten füllt (Befund V0-7: Dialog und
        /// <c>Tab_Ergebnis</c> zeigen dieselbe Zahl, nicht zwei nachgebaute).
        /// </summary>
        private void BedarfKanalzeilenFuellen()
        {
            if (tb_BedarfKanal == null) return;

            double[] mwh = SimulationRunner.BedarfJeKanal(simulation_Waermebedarf);

            if (label_BedarfKanalKopf != null) label_BedarfKanalKopf.Visible = true;
            for (int k = 0; k < Kanal.ANZAHL; k++)
            {
                tb_BedarfKanal[k].Text = mwh[k].ToString("F2");
                tb_BedarfKanal[k].Visible = true;
                label_BedarfKanal[k].Visible = true;
                label_BedarfKanalEinheit[k].Visible = true;
            }
        }

        /// <summary>
        /// ETAPPE E2 (Leitentscheidung L6) — legt die Kennzahlzeile
        /// „Vollbenutzungsstunden elektrisch" unmittelbar unter die beiden vorhandenen
        /// Vbh-Zeilen und benennt diese beiden zugleich richtig.
        ///
        /// <b>Warum die beiden Bestandszeilen umbenannt werden.</b> Sie hießen
        /// „Betriebsstunden gesamt" und „Betriebsstunden Durchschnitt", zeigen aber
        /// <c>SimulationBHKW.Betriebsstunden</c> bzw. <c>dLaufzeiten</c> — also die SUMME
        /// beziehungsweise das MITTEL THERMISCHER Vollbenutzungsstunden je Modul
        /// (<c>Wärme / Wärmeleistung</c>). Betriebsstunden sind das nicht: Der Rechenkern
        /// bildet keine Taktung ab, und die Summe kann 8.760 h überschreiten. Genau diese
        /// Verwechslung war der Fehler, den E2 in der Wirtschaftlichkeit behebt — sie im
        /// Ergebnisreiter stehen zu lassen, hieße ihn zu konservieren.
        ///
        /// <b>Warum zur Laufzeit und nicht in der .resx.</b> Dieselbe Begründung wie bei
        /// <see cref="InitBhkwSpeicherzeilen"/>: Designer und .resx der Form bleiben
        /// unangetastet. Die Texte kommen aus dem Katalog <c>MyResource.Resource</c> und
        /// sind damit in beiden Sprachen gepflegt (Drei-Schichten-Regel).
        ///
        /// <b>Warum nur bis zur Brennstoff-Überschrift nachgerückt wird.</b> Nach dem
        /// Einschub der beiden Speicherzeilen endet der Brennstoffblock der rechten Spalte
        /// bei y≈696 bei einer Entwurfshöhe von 721 — für weitere 32 px ist dort kein
        /// Platz. Zwischen der letzten Deckungszeile und der Überschrift
        /// „Brennstoffverbrauch" stehen dagegen 39 px frei; genau die werden verbraucht.
        /// </summary>
        private void InitBhkwVbhZeile()
        {
            if (tabPage_BHKW == null || textBox_Betriebsstunden_Durchschnitt == null) return;

            // Die zwei Bestandszeilen richtig benennen (Beschriftung links, Einheit bleibt).
            Control besch1 = NachbarZeile(textBox_Betriebsstunden, true);
            if (besch1 is Label) besch1.Text = MyResource.Resource.SIM_BHKW_VBH_TH_SUMME;
            Control besch2 = NachbarZeile(textBox_Betriebsstunden_Durchschnitt, true);
            if (besch2 is Label) besch2.Text = MyResource.Resource.SIM_BHKW_VBH_TH_MITTEL;

            TextBox muster = textBox_Betriebsstunden_Durchschnitt;
            Control beschriftung = NachbarZeile(muster, true);
            Control einheit = NachbarZeile(muster, false);   // trägt bereits „h/a"

            int schritt = muster.Height + 7;
            int y = muster.Bottom + 7;

            // Nur den Block zwischen dieser Zeile und der Brennstoff-Überschrift schieben.
            BhkwZeilenNachruecken(muster.Bottom + 4, BhkwBrennstoffBlockOben(), schritt);

            BhkwKennzahlZeile(muster, beschriftung, einheit, y,
                              "tb_BhkwVbhElektrisch",
                              MyResource.Resource.SIM_BHKW_VBH_EL,
                              out label_BhkwVbhElektrisch, out tb_BhkwVbhElektrisch,
                              out label_BhkwVbhElektrischEinheit);
        }

        /// <summary>
        /// Obere Kante des Brennstoffblocks der rechten Spalte — die Grenze, bis zu der
        /// nachgerückt werden darf. Ermittelt aus den Steuerelementen selbst
        /// (unterstes Element der Spalte), nicht aus einer Namensliste; verschiebt sich
        /// der Entwurf, verschiebt sich die Grenze mit.
        /// </summary>
        private int BhkwBrennstoffBlockOben()
        {
            if (tabPage_BHKW == null) return int.MaxValue;
            int grenzeLinks = (chart_BHKW_Waerme != null) ? chart_BHKW_Waerme.Right + 8 : 0;
            int unten = int.MaxValue;
            foreach (Control c in tabPage_BHKW.Controls)
            {
                if (ReferenceEquals(c, chart_BHKW_Waerme)) continue;
                if (c.Left < grenzeLinks) continue;
                // Der Block beginnt unterhalb der Deckungszeilen; als Anker dient das
                // Panel der Brennstoffliste samt seiner Überschrift darüber.
                if (c is FlowLayoutPanel && c.Top < unten) unten = c.Top;
            }
            if (unten == int.MaxValue) return int.MaxValue;

            // Die Überschrift steht dicht über dem Panel — sie darf nicht mitwandern.
            int ueberschrift = unten;
            foreach (Control c in tabPage_BHKW.Controls)
            {
                if (c.Left < grenzeLinks) continue;
                if (c.Top < unten && c.Top > unten - 60 && c.Top < ueberschrift) ueberschrift = c.Top;
            }
            return ueberschrift;
        }

        /// <summary>
        /// Schiebt alle Steuerelemente der RECHTEN Spalte der BHKW-Seite, die unterhalb
        /// von <paramref name="abY"/> beginnen, um <paramref name="dy"/> nach unten.
        ///
        /// Die Spalte wird über die Waagerechte abgegrenzt (Left &gt;= halbe Seitenbreite)
        /// und nicht über eine Liste von Steuerelementnamen: Das Diagramm und die
        /// Modultabelle links reichen bis y≈693 und dürfen sich nicht bewegen.
        /// </summary>
        private void BhkwZeilenNachruecken(int abY, int dy)
        {
            BhkwZeilenNachruecken(abY, int.MaxValue, dy);
        }

        /// <summary>
        /// Wie oben, aber nur bis zur Höhe <paramref name="bisY"/> (ausschließlich) —
        /// gebraucht seit Etappe E2: Unterhalb der Deckungszeilen ist genau eine Zeilenhöhe
        /// frei, der Brennstoffblock darunter darf nicht mitwandern (er stieße sonst über
        /// die Entwurfshöhe der Seite hinaus).
        /// </summary>
        private void BhkwZeilenNachruecken(int abY, int bisY, int dy)
        {
            if (tabPage_BHKW == null || chart_BHKW_Waerme == null) return;

            int grenzeLinks = chart_BHKW_Waerme.Right + 8;

            tabPage_BHKW.SuspendLayout();
            foreach (Control c in tabPage_BHKW.Controls)
            {
                if (ReferenceEquals(c, chart_BHKW_Waerme)) continue;
                if (c.Left < grenzeLinks) continue;
                if (c.Top < abY || c.Top >= bisY) continue;
                c.Location = new Point(c.Left, c.Top + dy);
            }
            tabPage_BHKW.ResumeLayout();
        }

        /// <summary>
        /// Baut EINE Kennzahlzeile (Beschriftung – Feld – Einheit) auf der Höhe
        /// <paramref name="y"/>, mit den Maßen und Farben von <paramref name="muster"/>.
        /// </summary>
        private void BhkwKennzahlZeile(TextBox muster, Control beschriftung, Control einheit,
                                       int y, string feldName, string text,
                                       out Label lblText, out TextBox feld, out Label lblEinheit)
        {
            feld = new TextBox();
            feld.Name = feldName;
            feld.ReadOnly = true;
            feld.BackColor = muster.BackColor;
            feld.ForeColor = muster.ForeColor;
            feld.BorderStyle = muster.BorderStyle;
            feld.Font = muster.Font;
            feld.TextAlign = muster.TextAlign;
            feld.Bounds = new Rectangle(muster.Left, y, muster.Width, muster.Height);
            feld.Visible = false;
            tabPage_BHKW.Controls.Add(feld);

            lblText = new Label();
            lblText.Name = feldName + "_Label";
            lblText.Text = text;
            lblText.AutoSize = false;
            lblText.TextAlign = ContentAlignment.MiddleRight;
            lblText.Visible = false;
            if (beschriftung != null)
            {
                lblText.Font = beschriftung.Font;
                lblText.ForeColor = beschriftung.ForeColor;
                lblText.BackColor = beschriftung.BackColor;
                // RECHTE Kante wie die Nachbarzeile (dort endet der rechtsbündige Text),
                // Breite aber GEMESSEN: „aus dem Speicher gedeckt:" ist länger als jede
                // Entwurfsbeschriftung der Spalte und würde in deren Breite abschneiden.
                // Nach links wird höchstens bis an das Diagramm herangerückt.
                int noetig = BreiteMessen(text, beschriftung.Font, beschriftung.Width);
                int rechts = beschriftung.Right;
                int links = rechts - noetig;
                int grenze = (chart_BHKW_Waerme != null) ? chart_BHKW_Waerme.Right + 8 : 0;
                if (links < grenze) links = grenze;
                lblText.Bounds = new Rectangle(links, y, rechts - links, muster.Height);
            }
            else
            {
                lblText.Bounds = new Rectangle(muster.Left - 250, y, 244, muster.Height);
            }
            tabPage_BHKW.Controls.Add(lblText);

            lblEinheit = new Label();
            lblEinheit.Name = feldName + "_Einheit";
            // Die Einheit ist die der Nachbarzeile („MWh/a"); sie wird ÜBERNOMMEN und
            // nicht neu getextet - so kann sie nicht von ihr abweichen.
            lblEinheit.Text = (einheit != null) ? einheit.Text : "MWh/a";
            lblEinheit.AutoSize = true;
            lblEinheit.Visible = false;
            if (einheit != null)
            {
                lblEinheit.Font = einheit.Font;
                lblEinheit.ForeColor = einheit.ForeColor;
                lblEinheit.BackColor = einheit.BackColor;
                lblEinheit.Location = new Point(einheit.Left, y + 4);
            }
            else
            {
                lblEinheit.Location = new Point(muster.Right + 8, y + 4);
            }
            tabPage_BHKW.Controls.Add(lblEinheit);
        }

        /// <summary>
        /// Die Breite, die <paramref name="text"/> in <paramref name="schrift"/> braucht —
        /// mindestens <paramref name="mindestens"/>. Gemessen mit demselben Renderer, mit
        /// dem WinForms zeichnet (<c>TextRenderer</c>, GDI); ein Zuschlag von 6 px hält den
        /// Text von der Kante frei.
        /// </summary>
        private static int BreiteMessen(string text, Font schrift, int mindestens)
        {
            try
            {
                int gemessen = TextRenderer.MeasureText(text ?? "", schrift).Width + 6;
                return (gemessen > mindestens) ? gemessen : mindestens;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Textbreite nicht messbar: " + ex.Message);
                return mindestens;
            }
        }

        /// <summary>
        /// Ein Anzeigetext aus der EIGENEN .resx-Familie der Form
        /// (<c>Form_Simulation_Detail.resx</c> und ihre Satelliten).
        ///
        /// <b>Warum nicht der Katalog <c>MyResource.Resource</c>.</b> Programmatische
        /// Steuerelemente nehmen dort sonst ihren Text (siehe
        /// <see cref="InitKesselChart"/>). Für die drei NEUEN Texte dieses Nachzugs war der
        /// Katalog nicht verfügbar (parallele Arbeit an derselben Datei), und die
        /// formulareigene .resx ist der zweite vorgesehene Ort — sie trägt die
        /// Oberflächentexte dieser Form ohnehin und wird beim Sprachwechsel über ihre
        /// Satelliten mitgezogen.
        ///
        /// <b>Warum mit Rückfalltext.</b> <c>GetString</c> liefert <c>null</c>, wenn der
        /// Eintrag fehlt — ein leeres Etikett wäre eine stille Fehlanzeige. Der Rückfall
        /// ist derselbe deutsche Wortlaut, der in der neutralen .resx steht.
        /// </summary>
        private static string TextAusFormResx(string schluessel, string rueckfall)
        {
            try
            {
                string wert = _formTexte.GetString(schluessel);
                if (!string.IsNullOrEmpty(wert)) return wert;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Anzeigetext '" + schluessel + "' nicht lesbar: " + ex.Message);
            }
            return rueckfall;
        }

        private static readonly System.ComponentModel.ComponentResourceManager _formTexte =
            new System.ComponentModel.ComponentResourceManager(typeof(Form_Simulation_Detail));

        /// <summary>
        /// Zeigt Diagramm und Speicher-Kennzahlen der BHKW-Seite — oder blendet sie aus,
        /// wenn das Ergebnis kein BHKW führt (Präsenzregel, siehe
        /// <see cref="ErgebnisPraesenz"/>).
        ///
        /// BEWUSST außerhalb von <c>if (sim.bSimulationBHKW)</c> gerufen: Wird das BHKW in
        /// einem Folgelauf abgewählt, muss das Bild des Vorlaufs verschwinden statt
        /// stehenzubleiben — dieselbe Begründung wie bei
        /// <see cref="KesselErgebnisAnzeigen"/>.
        /// </summary>
        private void BhkwErgebnisAnzeigen()
        {
            if (chart_BHKW_Waerme == null) return;

            bool zeigen = sim != null && sim.simulation_bhkw != null
                          && ErgebnisPraesenz.Ermitteln(sim).BHKW;

            _bhkwChartAktiv = zeigen;
            if (checkBox_BHKW_sortiert != null) checkBox_BHKW_sortiert.Visible = zeigen;

            // Die zwei Speicherzeilen folgen derselben Regel wie das Diagramm.
            BhkwSpeicherzeilenSichtbar(zeigen);

            if (!zeigen)
            {
                if (_chartBhkwManager != null) _chartBhkwManager.HardReset();
                return;
            }

            if (_chartBhkwManager == null) _chartBhkwManager = new ChartManager(chart_BHKW_Waerme);
            BhkwSerienAufbauen();
        }

        /// <summary>Blendet die zwei Speicher-Kennzahlzeilen ein oder aus.</summary>
        private void BhkwSpeicherzeilenSichtbar(bool zeigen)
        {
            if (tb_BhkwSpeicherladung != null) tb_BhkwSpeicherladung.Visible = zeigen;
            if (label_BhkwSpeicherladung != null) label_BhkwSpeicherladung.Visible = zeigen;
            if (label_BhkwSpeicherladungEinheit != null) label_BhkwSpeicherladungEinheit.Visible = zeigen;
            if (tb_BhkwSpeicherdeckung != null) tb_BhkwSpeicherdeckung.Visible = zeigen;
            if (label_BhkwSpeicherdeckung != null) label_BhkwSpeicherdeckung.Visible = zeigen;
            if (label_BhkwSpeicherdeckungEinheit != null) label_BhkwSpeicherdeckungEinheit.Visible = zeigen;

            // ETAPPE E2: die Vbh-Zeile folgt derselben Präsenzregel.
            if (tb_BhkwVbhElektrisch != null) tb_BhkwVbhElektrisch.Visible = zeigen;
            if (label_BhkwVbhElektrisch != null) label_BhkwVbhElektrisch.Visible = zeigen;
            if (label_BhkwVbhElektrischEinheit != null) label_BhkwVbhElektrischEinheit.Visible = zeigen;
        }

        /// <summary>
        /// Baut Diagrammkonfiguration und Serien der BHKW-Seite auf — in der
        /// Darstellungsform, die der Umschalter „sortiert" vorgibt. Ablauf wie
        /// <see cref="KesselSerienAufbauen"/>: <c>XAxisAsNumber</c> setzen,
        /// <c>HardReset()</c>, <c>Init()</c>, Serien neu.
        ///
        /// VIER Serien, alle unmittelbar aus dem Rechenkern — hier wird nichts
        /// nachgerechnet:
        ///   Wärmeproduktion = <c>waermeproduktion</c>, die BRUTTOerzeugung der Motoren.
        ///                     Sie enthält Direktdeckung, Speicherladung und Überschuss
        ///                     (Energieprobe in <c>SimulationBHKW.Energieprobe</c>).
        ///                     SÄULEN, unten.
        ///   Speicherladung  = <c>Speicherladung_stuendlich</c>, der Anteil der Produktion,
        ///                     der in einen Pufferspeicher geht. LINIE.
        ///   Restwärme       = <c>waermerestbedarf</c>, die Ganglinie des Kerns
        ///                     („Stufeneingang − Direktdeckung − zugerechnete Entladung",
        ///                     <c>SimulationBHKW.Stunde_Ende</c>). Ersetzt die frühere
        ///                     Vektordifferenz.
        ///   Wärmebedarf     = <c>waermebedarf</c>, der Stufeneingang. LINIE, zuletzt
        ///                     angelegt und damit ganz oben.
        ///
        /// <b>Warum die Speicherladung KEINE Stapelserie ist.</b> Sie ist ein TEIL der
        /// Produktion, nicht ihre Ergänzung. In derselben Stapelgruppe zeigte das Bild die
        /// Summe „Produktion + Ladung" und damit die Ladung doppelt; in einer zweiten
        /// Stapelgruppe stellt MS-Chart die Säulen NEBENEINANDER — bei 8760 Punkten auf
        /// 575 Bildpunkten verschwinden dann beide in der Rasterung (Befund der
        /// Heizkessel-Seite). Als Linie über den Säulen ist sie ablesbar: Sie liegt
        /// zwischen 0 und der Oberkante der Produktion, und der Abstand nach oben ist die
        /// unmittelbar gedeckte Wärme.
        ///
        /// <b>Warum EINE Stapelgruppe „Produktion".</b> Dieselbe Begründung wie beim
        /// Kessel: Nur die Produktionssäulen stapeln, die drei Linien belegen keinen
        /// Säulenplatz und lassen ihnen die volle Breite.
        /// </summary>
        private void BhkwSerienAufbauen()
        {
            if (_chartBhkwManager == null || sim == null || sim.simulation_bhkw == null) return;

            SimulationBHKW bh = sim.simulation_bhkw;
            bool sortiert = checkBox_BHKW_sortiert != null && checkBox_BHKW_sortiert.Checked;

            float[] bedarf = bh.waermebedarf;
            float[] produktion = bh.waermeproduktion;
            float[] rest = bh.waermerestbedarf;
            float[] ladung = Array.ConvertAll<double, float>(bh.Speicherladung_stuendlich, x => (float)x);

            ChartManager cm = _chartBhkwManager;
            cm.YMaxValue = Math.Max(bedarf.Max(), produktion.Max()) + 1;
            cm.YMinValue = 0;
            cm.XAxisAsNumber = sortiert;
            cm.XAxisTitle = sortiert
                ? MyResource.Resource.CHART_ACHSE_JAHRESSTUNDEN
                : MyResource.Resource.CHART_ACHSE_MONATE;
            cm.YAxisTitle = MyResource.Resource.CHART_ACHSE_WAERMELAST;
            cm.toolTipUnit = "kW";
            cm.ChartTitle = MyResource.Resource.CHART_TITEL_WAERMELAST_JAHRESGANGLINIE;
            cm.MitLegende = true;
            cm.MitChartBorder = true;
            cm.AreaLine = false;
            cm.MaxXVALUE = 8760;
            cm.MitViertelStunde = false;

            cm.HardReset();
            cm.Init();

            // Reihenfolge = Zeichenreihenfolge: Das Zuletztangelegte liegt oben.
            SerieAnlegen(cm, S_WAERMEPRODUKTION, MyResource.Resource.CHART_LEGENDE_WAERMEPRODUKTION,
                         Color.Blue, GanglinienDarstellung.Anzeigewerte(produktion, sortiert),
                         GanglinienDarstellung.Stapeltyp(sortiert), "Produktion");

            // DAUERLINIE: Dort ist die Produktion eine Linie und kann mit einer anderen
            // punktgleich verlaufen; die untere wird deshalb breiter gezeichnet (Muster der
            // Heizkessel-Seite, Begründung dort).
            if (sortiert) cm._chart.Series[S_WAERMEPRODUKTION].BorderWidth = 4;

            SerieAnlegen(cm, S_SPEICHERLADUNG,
                         TextAusFormResx("SIMDET_BHKW_SERIE_SPEICHERLADUNG", "Speicherladung"),
                         Color.DarkOrange, GanglinienDarstellung.Anzeigewerte(ladung, sortiert),
                         SeriesChartType.FastLine);

            SerieAnlegen(cm, S_RESTWAERME, MyResource.Resource.CHART_SEGMENT_RESTWAERME,
                         Color.Green, GanglinienDarstellung.Anzeigewerte(rest, sortiert),
                         SeriesChartType.FastLine);

            // Der Stufeneingang ZULETZT und damit ganz oben — er ist die Bezugsgröße,
            // gegen die die drei anderen Serien gerechnet sind (Blockkommentar oben).
            SerieAnlegen(cm, S_WAERMEBEDARF, MyResource.Resource.CHART_LEGENDE_WAERMEBEDARF,
                         Color.Red, GanglinienDarstellung.Anzeigewerte(bedarf, sortiert),
                         SeriesChartType.FastLine);

            cm._chart.Invalidate();
        }

        /// <summary>
        /// Umschalter „sortiert" der BHKW-Seite: Jahresganglinie ↔ Jahresdauerlinie.
        /// Baut das Diagramm neu auf; an den Vektoren ändert sich nichts (wortgleich mit
        /// <see cref="checkBox_Kessel_sortiert_CheckedChanged"/>).
        /// </summary>
        private void checkBox_BHKW_sortiert_CheckedChanged(object sender, EventArgs e)
        {
            if (!_bhkwChartAktiv) return;
            BhkwSerienAufbauen();
        }

        /// <summary>
        /// Legt die Pufferspeicher-Ergebnistabelle und die Erdreich-Hinweiszeile im
        /// Wärmepumpen-Tab an (Konzept 13.3 bzw. 4.5/13.1).
        ///
        /// Die bisherige <c>textBox_Pufferspeicher</c> konnte nur EINEN Speicher zeigen
        /// und blieb bei mehreren stillschweigend unvollständig. Sie bleibt im Designer
        /// erhalten (kein Designer-/.resx-Eingriff), wird aber ausgeblendet, sobald der
        /// Lauf mindestens einen Speicher hatte; ohne Speicher zeigt sie weiter den
        /// bisherigen Text. Die Tabelle entsteht programmatisch nach dem Muster von
        /// listView_SimSolar/listView_SimPV.
        ///
        /// Alle sichtbaren Texte sind deutsch hartkodiert - das entspricht dem
        /// Bestandsmuster des Simulationsbereichs; die durchgängige Lokalisierung
        /// gehört zu Paket 9.
        /// </summary>
        private void InitPufferspeicherRubrik()
        {
            if (listView_SimWP == null || listView_SimWP.Parent == null) return;

            // Platz schaffen: die Modul-Liste ist für ihre wenigen Zeilen sehr hoch.
            int hoeheWP = Math.Max(120, listView_SimWP.Height - 110);
            listView_SimWP.Height = hoeheWP;

            listView_SimPuffer = new System.Windows.Forms.ListView();
            listView_SimPuffer.Name = "listView_SimPuffer";
            listView_SimPuffer.View = View.Details;
            listView_SimPuffer.FullRowSelect = true;
            listView_SimPuffer.GridLines = true;
            listView_SimPuffer.MultiSelect = false;
            // D4: Die Kombispeicher-Zeile erklärt ihre Vollzyklen im Hinweisfenster
            // (siehe PufferspeicherErgebnisAnzeigen).
            listView_SimPuffer.ShowItemToolTips = true;
            listView_SimPuffer.Font = listView_SimSPK.Font;
            listView_SimPuffer.Columns.Add(MyResource.Resource.PSP_BEZEICHNER_ERSATZ, -2, HorizontalAlignment.Left);
            listView_SimPuffer.Columns.Add(MyResource.Resource.PSP_SPALTE_ROLLE, -2, HorizontalAlignment.Left);
            listView_SimPuffer.Columns.Add(MyResource.Resource.PSP_SPALTE_KAPAZITAET, -2, HorizontalAlignment.Left);
            listView_SimPuffer.Columns.Add(MyResource.Resource.PSP_SPALTE_LADUNG, -2, HorizontalAlignment.Left);
            listView_SimPuffer.Columns.Add(MyResource.Resource.PSP_SPALTE_ENTLADUNG, -2, HorizontalAlignment.Left);
            listView_SimPuffer.Columns.Add(MyResource.Resource.PSP_SPALTE_VERLUSTE, -2, HorizontalAlignment.Left);
            listView_SimPuffer.Columns.Add(MyResource.Resource.PSP_SPALTE_VOLLZYKLEN, -2, HorizontalAlignment.Left);
            listView_SimPuffer.Columns.Add(MyResource.Resource.PSP_SPALTE_FUELLSTAND_ENDE, -2, HorizontalAlignment.Left);
            listView_SimPuffer.Location = new Point(listView_SimWP.Left, listView_SimWP.Bottom + 10);
            listView_SimPuffer.Width = listView_SimWP.Width;
            listView_SimPuffer.Height = 82;
            listView_SimPuffer.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            // Erst nach einem Lauf mit Speicher sichtbar (siehe PufferspeicherErgebnisAnzeigen).
            listView_SimPuffer.Visible = false;
            listView_SimWP.Parent.Controls.Add(listView_SimPuffer);

            label_Erdreich = new Label();
            label_Erdreich.Name = "label_Erdreich";
            label_Erdreich.AutoSize = false;
            label_Erdreich.Font = listView_SimSPK.Font;
            label_Erdreich.Location = new Point(listView_SimPuffer.Left, listView_SimPuffer.Bottom + 6);
            label_Erdreich.Size = new Size(listView_SimWP.Width, 44);
            label_Erdreich.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            label_Erdreich.Text = "";
            label_Erdreich.Visible = false;
            listView_SimWP.Parent.Controls.Add(label_Erdreich);
        }

        /// <summary>
        /// Füllt die Pufferspeicher-Ergebnistabelle aus denselben Speicherobjekten, die
        /// auch Tab_ErgebnisPufferspeicher speisen (eine Quelle der Wahrheit, Konzept 6.6).
        /// </summary>
        private void PufferspeicherErgebnisAnzeigen()
        {
            List<SimulationPufferspeicher> speicher = sim.AlleSpeicher();

            if (listView_SimPuffer != null)
            {
                listView_SimPuffer.Items.Clear();
                foreach (SimulationPufferspeicher sp in speicher)
                {
                    ListViewItem li = new ListViewItem(sp.BezeichnerAnzeige());
                    li.SubItems.Add(sp.RolleAnzeige());
                    li.SubItems.Add(sp.Q_max.ToString("F1"));
                    li.SubItems.Add(sp.Ladung_gesamt.ToString("F0"));
                    li.SubItems.Add(sp.Entladung_gesamt.ToString("F0"));
                    li.SubItems.Add(sp.Verluste_gesamt.ToString("F0"));

                    // ETAPPE D4 (D5b-Restpunkt 4): Beim KOMBISPEICHER laufen beide Kanäle
                    // über EINEN Vorrat; die Kennzahl wird dann groß und misst den
                    // JAHRESDURCHSATZ bezogen auf die Kapazität, nicht die Alterung des
                    // Speichers (D5b-Szenario: 6627 an einem 13,9-kWh-Puffer).
                    //
                    // ENTSCHÄRFT WIRD SIE HIER, NICHT IM RECHENKERN: Der gespeicherte Wert
                    // in Tab_ErgebnisPufferspeicher.Vollzyklen bleibt Bit für Bit der
                    // bisherige - eine andere Formel wäre eine Ergebnisänderung und
                    // gehörte in eine Etappe mit eigenem Referenznachweis. Die Anzeige
                    // markiert den Wert und erklärt ihn im Hinweisfenster.
                    li.SubItems.Add(sp.Vollzyklen.ToString("F1") + (sp.IstKombi ? " *" : ""));
                    li.SubItems.Add(sp.SOC.ToString("F1"));

                    if (sp.IstKombi)
                        li.ToolTipText = Zeilenumbruch.Normalisieren(
                            MyResource.Resource.PSP_VOLLZYKLEN_KOMBI_TIP);

                    listView_SimPuffer.Items.Add(li);
                }
                listView_SimPuffer.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                listView_SimPuffer.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                listView_SimPuffer.Visible = speicher.Count > 0;
            }

            // Ohne Speicher bleibt es beim bisherigen Textfeld (Legacy-Ausdruck);
            // mit Speicher übernimmt die Tabelle - der Übergangshinweis
            // "Speicher 1 von n" aus Konzept 6.7 entfällt damit.
            bool mitSpeicher = speicher.Count > 0;
            textBox_Pufferspeicher.Visible = !mitSpeicher;
            Control[] beschriftung = tabPage_Wärmepumpe.Controls.Find("label38", true);
            if (beschriftung.Length == 0 && textBox_Pufferspeicher.Parent != null)
                beschriftung = textBox_Pufferspeicher.Parent.Controls.Find("label38", true);
            foreach (Control c in beschriftung) c.Visible = !mitSpeicher;

            if (!mitSpeicher)
                textBox_Pufferspeicher.Text = (sim.simulation_wp != null)
                    ? (sim.simulation_wp.Volumen_Pufferspeicher * 1.16).ToString()
                    : "";
        }

        // ==================================================================
        // PAKET P2 — Diagrammseite „Speichertemperaturen" (Konzept 7.4, P1-O5)
        // ==================================================================

        /// <summary>
        /// Legt Seite und Diagramm der Speichertemperaturen an. Die Seite wird noch
        /// NICHT in <c>tabControl2</c> eingehängt — das entscheidet erst
        /// <see cref="SpeichertemperaturAnzeigen"/> anhand des Laufergebnisses.
        ///
        /// <para><b>Dock statt fester Maße</b> (Fixmuster der TabPage-Vierseitenanker-
        /// Falle): Eine Tabseite steht im Konstruktor noch auf der Vorgabegröße 200×100;
        /// ein Diagramm mit festen Bounds und Vierseitenanker verankerte seine Ränder
        /// gegen diese Vorgabe und wüchse beim ersten echten Layout um die Differenz aus
        /// der Seite heraus. <c>Padding</c> an der Seite plus <c>Dock.Fill</c> am
        /// Diagramm ist unabhängig von der Reihenfolge.</para>
        /// </summary>
        private void InitSpeichertemperaturChart()
        {
            if (tabControl2 == null) return;

            chart_Speichertemperatur = new System.Windows.Forms.DataVisualization.Charting.Chart();
            chart_Speichertemperatur.Name = "chart_Speichertemperatur";
            chart_Speichertemperatur.Dock = DockStyle.Fill;

            // Ein programmatisch erzeugtes Chart hat KEINE ChartArea - ChartManager.Init
            // steigt ohne sie wortlos aus (dieselbe Falle wie bei chart_Kessel).
            chart_Speichertemperatur.ChartAreas.Add(new ChartArea("ChartArea_Speichertemperatur"));

            tabPage_Speichertemperatur = new TabPage(MyResource.Resource.SIM_TAB_SPEICHERTEMPERATUR);
            tabPage_Speichertemperatur.Name = "tabPage_Speichertemperatur";
            tabPage_Speichertemperatur.UseVisualStyleBackColor = true;
            tabPage_Speichertemperatur.Padding = new Padding(3);
            tabPage_Speichertemperatur.Controls.Add(chart_Speichertemperatur);
        }

        /// <summary>
        /// Sammelt die Temperaturreihen des Laufs — je Senkenspeicher die oberste und die
        /// unterste Schicht (Paket P1), dazu die Quelltemperatur jedes
        /// temperaturgekoppelten Erzeugers (Paket B1). Nie <c>null</c>.
        ///
        /// <para><b>Dieselben Quellen und dieselben Schlüssel wie der
        /// <c>ZeitreihenExtraktor</c></b>: die Speicherliste <c>sim.AlleSpeicher()</c>,
        /// <c>SimulationPufferspeicher.Schluessel</c> mit den Nachsilben aus
        /// <see cref="ZeitreihenSatz"/> und dieselben Ausschlussregeln (Quellspeicher
        /// tragen keine Schichttemperatur, ungekoppelte Erzeuger keine
        /// Quelltemperatur-Ganglinie). Der Extraktor selbst ist hier nicht aufrufbar — er
        /// baut den Satz aus einem <c>SimulationRunner</c>, den diese Ansicht nicht
        /// führt; sie rechnet über <c>SimulationControl</c>. Gemeinsam ist beiden Wegen
        /// die EINE Datenquelle: die Vektoren der Speicher- und Erzeugerobjekte.</para>
        /// </summary>
        private List<Temperaturreihe> Temperaturreihen()
        {
            var liste = new List<Temperaturreihe>();
            if (sim == null) return liste;

            List<SimulationPufferspeicher> speicher = sim.AlleSpeicher();
            int nummer = 0;

            for (int i = 0; i < speicher.Count; i++)
            {
                SimulationPufferspeicher sp = speicher[i];
                if (sp == null || sp.IstQuelle || !sp.T_oben_Mittel.HasValue) continue;
                if (sp.T_oben_stuendlich == null || sp.T_unten_stuendlich == null) continue;

                string schluessel = sp.Schluessel(i);
                Color farbe = TEMP_FARBEN[nummer % TEMP_FARBEN.Length];
                nummer++;

                liste.Add(new Temperaturreihe
                {
                    Schluessel = schluessel + ZeitreihenSatz.SUFFIX_T_OBEN,
                    Legende = sp.BezeichnerAnzeige() + " " + MyResource.Resource.SIM_REIHE_T_OBEN,
                    Werte = sp.T_oben_stuendlich,
                    Farbe = farbe
                });
                liste.Add(new Temperaturreihe
                {
                    Schluessel = schluessel + ZeitreihenSatz.SUFFIX_T_UNTEN,
                    Legende = sp.BezeichnerAnzeige() + " " + MyResource.Resource.SIM_REIHE_T_UNTEN,
                    Werte = sp.T_unten_stuendlich,
                    Farbe = farbe,
                    Gestrichelt = true
                });
            }

            if (sim.bSimulationWP && sim.simulation_wp != null)
            {
                var profile = sim.simulation_wp.Quelltemperaturen;
                var anlagen = sim.simulation_wp.wp_list;

                for (int i = 0; i < profile.Count && i < anlagen.Count; i++)
                {
                    if (!sim.simulation_wp.QuelleGekoppelt(i) || profile[i] == null) continue;
                    QuellreiheAnhaengen(liste, anlagen[i], profile[i], sim.simulation_wp.WP_Modul[i]);
                }
            }

            if (sim.bSimulationKessel && sim.simulation_spk != null)
            {
                var anlagen = sim.simulation_spk.spk_anlagen_ids;

                for (int i = 0; i < anlagen.Count; i++)
                {
                    float[] reihe = sim.simulation_spk.Quelltemperaturen(i);
                    if (reihe == null) continue;
                    QuellreiheAnhaengen(liste, anlagen[i], reihe, sim.simulation_spk.KesselName(i));
                }
            }

            return liste;
        }

        /// <summary>Hängt eine Quelltemperatur-Reihe an; doppelte Anlagen-IDs übergeht sie.</summary>
        private static void QuellreiheAnhaengen(List<Temperaturreihe> liste, int idAnlage,
                                                float[] werte, string bezeichner)
        {
            if (idAnlage <= 0 || werte == null) return;

            string schluessel = ZeitreihenSatz.QUELLTEMP_PRAEFIX + idAnlage;
            foreach (Temperaturreihe r in liste)
                if (string.Equals(r.Schluessel, schluessel, StringComparison.Ordinal)) return;

            liste.Add(new Temperaturreihe
            {
                Schluessel = schluessel,
                Legende = (string.IsNullOrEmpty(bezeichner) ? schluessel : bezeichner) +
                          " " + MyResource.Resource.SIM_REIHE_QUELLTEMPERATUR,
                Werte = werte,
                Farbe = TEMP_FARBE_QUELLE
            });
        }

        /// <summary>
        /// Baut die Diagrammseite „Speichertemperaturen" aus dem aktuellen Lauf — oder
        /// nimmt sie aus <c>tabControl2</c> heraus, wenn er keine Temperaturreihe trägt
        /// (Projekt ohne Senkenspeicher, oder ein Folgelauf, in dem der Speicher
        /// abgewählt wurde). Dieselbe Regel wie beim Kessel-Diagramm: lieber nichts
        /// zeigen als die Zahlen des Vorlaufs.
        /// </summary>
        private void SpeichertemperaturAnzeigen()
        {
            if (tabControl2 == null || tabPage_Speichertemperatur == null ||
                chart_Speichertemperatur == null) return;

            List<Temperaturreihe> reihen = Temperaturreihen();

            if (reihen.Count == 0)
            {
                if (tabControl2.TabPages.Contains(tabPage_Speichertemperatur))
                    tabControl2.TabPages.Remove(tabPage_Speichertemperatur);
                return;
            }

            if (!tabControl2.TabPages.Contains(tabPage_Speichertemperatur))
                tabControl2.TabPages.Add(tabPage_Speichertemperatur);

            // EIGENE °C-ACHSE: Sie beginnt NICHT bei 0 - die Temperaturen eines
            // Pufferspeichers liegen zwischen Rücklauf und Vorlauf, und eine bei 0
            // startende Achse drückte das ganze Band in den oberen Rand. ChartManager
            // rundet Minimum und Maximum auf ein glattes Intervall (siehe dort).
            double min = double.MaxValue, max = double.MinValue;
            foreach (Temperaturreihe r in reihen)
                for (int h = 0; h < r.Werte.Length && h < 8760; h++)
                {
                    if (r.Werte[h] < min) min = r.Werte[h];
                    if (r.Werte[h] > max) max = r.Werte[h];
                }

            if (min > max) { min = 0; max = 100; }        // sollte nicht vorkommen
            if (max - min < 5) max = min + 5;             // flache Kurve nicht auf eine Linie pressen

            if (_chartTemperaturManager == null)
                _chartTemperaturManager = new ChartManager(chart_Speichertemperatur);

            ChartManager cm = _chartTemperaturManager;
            cm.YMinValue = min;
            cm.YMaxValue = max;
            cm.XAxisAsNumber = false;
            cm.XAxisTitle = MyResource.Resource.CHART_ACHSE_JAHRESSTUNDEN;
            cm.YAxisTitle = MyResource.Resource.CHART_ACHSE_TEMPERATUR;
            // Der Achsentitel trägt die Einheit bereits; toolTipUnit bleibt deshalb leer,
            // sonst stünde „Temperatur [°C] [°C]" an der Achse (ChartManager.Init).
            cm.toolTipUnit = "";
            cm.ChartTitle = MyResource.Resource.CHART_TITEL_SPEICHERTEMPERATUR;
            cm.MitLegende = true;
            cm.MitChartBorder = true;
            cm.AreaLine = false;
            cm.MaxXVALUE = 8760;
            cm.MitViertelStunde = false;

            cm.HardReset();
            cm.Init();

            foreach (Temperaturreihe r in reihen)
            {
                SerieAnlegen(cm, r.Schluessel, r.Legende, r.Farbe, r.Werte,
                             SeriesChartType.FastLine);

                // Die untere Schicht läuft GESTRICHELT in derselben Farbe wie ihre obere:
                // Zwei Temperaturen desselben Behälters gehören zusammen, und bei vier
                // Speichern wären acht verschiedene Farben nicht mehr auseinanderzuhalten.
                if (r.Gestrichelt)
                    cm._chart.Series[r.Schluessel].BorderDashStyle = ChartDashStyle.Dash;
            }

            cm._chart.Invalidate();
        }

        /// <summary>
        /// Zeigt die Warnungen der VDI-4640-Auslegungsprüfung als kompakte Textzeilen
        /// im Wärmepumpen-Ergebnisbereich (Konzept 4.5: die Prüfung muss den Anwender
        /// auch dann erreichen, wenn er den Quellendialog nicht mehr öffnet).
        /// </summary>
        private void ErdreichHinweisAnzeigen()
        {
            if (label_Erdreich == null) return;

            List<ErdreichAuswertung.AnlageErgebnis> erg = ErdreichAuswertung.FuerProjekt(m_ID_Projekt);
            if (erg.Count == 0)
            {
                label_Erdreich.Visible = false;
                label_Erdreich.Text = "";
                return;
            }

            List<string> zeilen = new List<string>();
            bool warnung = false;
            foreach (ErdreichAuswertung.AnlageErgebnis a in erg)
            {
                zeilen.Add(a.Kurztext());
                if ((a.Pruefung != null && a.Pruefung.Moeglich && a.Pruefung.Warnung) || a.FrostWarnung)
                    warnung = true;
            }

            label_Erdreich.Text = string.Join(Environment.NewLine, zeilen);
            label_Erdreich.ForeColor = warnung ? Color.Firebrick : SystemColors.ControlText;

            // Höhe an den tatsächlichen Umbruch anpassen. AutoSize = true würde die
            // Breite sprengen (eine einzige lange Zeile), deshalb wird bei fester
            // Breite gemessen und nur die Höhe nachgezogen - sonst schneidet die im
            // Konstruktor gesetzte Festhöhe von 44 px alles ab der dritten Zeile ab,
            // und ausgerechnet die Warnungen stehen am Ende.
            Size gemessen = TextRenderer.MeasureText(label_Erdreich.Text, label_Erdreich.Font,
                new Size(label_Erdreich.Width, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            label_Erdreich.Height = Math.Max(44, gemessen.Height + 6);

            label_Erdreich.Visible = true;
        }

        /// <summary>
        /// CSV-Export Bereich Energiebedarf:
        /// Zeitstempel; Außentemperatur; Wärmelast [kW]; Strombedarf [kW] (Stundenwerte).
        /// </summary>
        private void btn_CsvExportBedarf_Click(object sender, EventArgs e)
        {
            if (simulation_Waermebedarf == null || simulation_Waermebedarf.Waermebedarf_Gesamt <= 0)
            {
                MessageBox.Show(MyResource.Resource.SIM_MSG_KEINE_DATEN_ENERGIEBEDARF,
                    MyResource.Resource.SIM_BTN_CSV_EXPORT, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<CsvSpalte> spalten = new List<CsvSpalte>();
            spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_WAERMELAST, simulation_Waermebedarf.Waermebedarf));
            // Strombedarf liegt viertelstündlich vor und wird als Stundenmittel exportiert
            spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_STROMBEDARF, simulation_Strombedarf.Strombedarf_viertelStundenwerte));

            CsvExportClass.Export(string.Format(MyResource.Resource.CHART_DATEI_ENERGIEBEDARF, m_ID_Projekt),
                simulation_Waermebedarf.Stundentemperatur, spalten, false);
        }

        /// <summary>
        /// CSV-Export Bereich Wärmepumpe:
        /// Zeitstempel; Außentemperatur; Wärmebedarf; Heizstab; Wärmeproduktion WP; Strombedarf WP (Stundenwerte).
        /// </summary>
        private void btn_CsvExportWP_Click(object sender, EventArgs e)
        {
            if (sim == null || !sim.bSimulationWP || sim.simulation_wp == null)
            {
                MessageBox.Show(MyResource.Resource.SIM_MSG_KEINE_DATEN_WAERMEPUMPE,
                    MyResource.Resource.SIM_BTN_CSV_EXPORT, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<CsvSpalte> spalten = new List<CsvSpalte>();
            spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_WAERMEBEDARF, sim.simulation_wp.Waermebedarf_stuendlich));
            spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_HEIZSTAB, sim.simulation_wp.Heizstab_stuendlich));
            spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_WAERMEPRODUKTION_WP, sim.simulation_wp.WP_Waermeproduktion_stuendlich));
            spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_STROMBEDARF_WP, sim.simulation_wp.WP_Strombedarf_stuendlich));

            // Speicher-Ganglinien mit exportieren: DREI Spalten je Speicher (Ladung,
            // Entladung, Speicherinhalt) mit dem Bezeichner im Kopf - Senken- wie
            // Quellspeicher (Konzept 13.3). Die Kopfzeile bleibt deutsch, sie ist
            // Exportformat und nicht Oberfläche (13.6).
            foreach (SimulationPufferspeicher sp in sim.AlleSpeicher())
            {
                string name = sp.Anzeige();
                spalten.Add(new CsvSpalte(string.Format(MyResource.Resource.CHART_CSV_SPEICHER_LADUNG, name), sp.Ladung_stuendlich));
                spalten.Add(new CsvSpalte(string.Format(MyResource.Resource.CHART_CSV_SPEICHER_ENTLADUNG, name), sp.Entladung_stuendlich));
                spalten.Add(new CsvSpalte(string.Format(MyResource.Resource.CHART_CSV_SPEICHER_INHALT, name), sp.SOC_stuendlich));
            }

            CsvExportClass.Export(string.Format(MyResource.Resource.CHART_DATEI_WAERMEPUMPE, m_ID_Projekt),
                sim.simulation_wp.Temperatur, spalten, false);
        }

        public void SetControls()
        {
        }

        public void UpdateTabPages()
        {
            KonfigurationCtrl ctrl = new KonfigurationCtrl();
            ctrl.ReadSingle("select * from Tab_Einstellungen where ID_Projekt=" + m_ID_Projekt);

            // Alle 6 Tools lückenlos von Index 0 bis 5 auslesen
            string[] tool = new string[6];
            tool[0] = ctrl.model.m_Tool_1;
            tool[1] = ctrl.model.m_Tool_2;
            tool[2] = ctrl.model.m_Tool_3;
            tool[3] = ctrl.model.m_Tool_4;
            tool[4] = ctrl.model.m_Tool_5;
            tool[5] = ctrl.model.m_Tool_6;

            // Verhindert das Flackern des Controls während des Umbaus
            tabControl_Simulation.SuspendLayout();
            tabControl_Einstellungen.SuspendLayout();

            dictAllTabPages.Clear();

            // Alle TabPages im Dictionary registrieren, damit sie im Hintergrund existieren
            dictAllTabPages.Add("tabPage_Parameter", this.tabPage_Parameter);
            dictAllTabPages.Add("tabPage_Uebersicht", this.tabPage_Uebersicht);
            dictAllTabPages.Add("tabPage_Simulation", this.tabPage_Simulation);
            dictAllTabPages.Add("tabPage_Bedarf", this.tabPage_Bedarf);
            dictAllTabPages.Add("tabPage_Wärmepumpe", this.tabPage_Wärmepumpe);
            dictAllTabPages.Add("tabPage_Heizkessel", this.tabPage_Heizkessel);
            dictAllTabPages.Add("tabPage_Photovoltaik", this.tabPage_Photovoltaik);
            dictAllTabPages.Add("tabPage_BHKW", this.tabPage_BHKW);
            dictAllTabPages.Add("tabPage_Solarthermie", this.tabPage_Solarthermie);
            dictAllTabPages.Add("tabPage_Stromspeicher", this.tabPage_Stromspeicher);
            dictAllTabPages.Add("tabPage_Ergebnis", this.tabPage_Ergebnis);

            // Radikal alle Tabs oben aus der Reiterleiste löschen
            tabControl_Simulation.TabPages.Clear();

            TabPage gefundeneSeite;

            // NUR NOCH die 2 echten Haupt-Tabs oben sichtbar einfügen:
            if (dictAllTabPages.TryGetValue("tabPage_Parameter", out gefundeneSeite) && gefundeneSeite != null)
                tabControl_Simulation.TabPages.Add(gefundeneSeite);

            // Übersicht als 2. Haupt-Tab (wie Parameter/Simulation NICHT Teil des ListView-Menü-Mappings)
            if (dictAllTabPages.TryGetValue("tabPage_Uebersicht", out gefundeneSeite) && gefundeneSeite != null)
                tabControl_Simulation.TabPages.Add(gefundeneSeite);

            if (dictAllTabPages.TryGetValue("tabPage_Simulation", out gefundeneSeite) && gefundeneSeite != null)
                tabControl_Simulation.TabPages.Add(gefundeneSeite);

            // ==========================================
            // STEUERUNG INNERES TAB-CONTROL (Parameter-Sub-Tabs)
            // ==========================================
            dictParameterTabPages.Clear();
            // Wir registrieren die 5 verfügbaren inneren Seiten im eigenen Dictionary
            // (Hinweis: Die Namen müssen exakt mit den Designer-Namen deiner inneren Pages übereinstimmen!)
            dictParameterTabPages.Add("tabPage_Bedarf_Parameter", this.tabPage_Bedarf_Parameter);
            dictParameterTabPages.Add("tabPage_Wärmepumpe_Parameter", this.tabPage_Wärmepumpe_Parameter);
            dictParameterTabPages.Add("tabPage_Heizkessel_Parameter", this.tabPage_Heizkessel_Parameter);
            dictParameterTabPages.Add("tabPage_BHKW_Parameter", this.tabPage_BHKW_Parameter);
            dictParameterTabPages.Add("tabPage_Stromspeicher_Parameter", this.tabPage_Stromspeicher_Parameter);

            // Das innere TabControl komplett leeren
            tabControl_Einstellungen.TabPages.Clear();

            // "Bedarf" ist immer aktiv und wird standardmäßig als erster Tab gesetzt
            if (dictParameterTabPages.TryGetValue("tabPage_Bedarf_Parameter", out gefundeneSeite) && gefundeneSeite != null)
                tabControl_Einstellungen.TabPages.Add(gefundeneSeite);

            // Jetzt die restlichen Erzeuger-Tabs dynamisch anhand der DB-Liste (tool) hinzufügen
            foreach (string toolName in tool)
            {
                if (string.IsNullOrEmpty(toolName)) continue;

                // prüfen, ob das aktivierte Tool eine der inneren Parameter-Seiten betrifft
                if (dictParameterTabPages.TryGetValue("tabPage_" + toolName + "_Parameter", out gefundeneSeite) && gefundeneSeite != null)
                {
                    // Verhindert doppeltes Hinzufügen, falls ein Tool fälschlicherweise zweimal in der DB steht
                    if (!tabControl_Einstellungen.TabPages.Contains(gefundeneSeite))
                    {
                        tabControl_Einstellungen.TabPages.Add(gefundeneSeite);
                    }
                }
            }

            // Steuerelement wieder freigeben
            tabControl_Simulation.ResumeLayout();
            tabControl_Einstellungen.ResumeLayout();

            // ListView-Navigation an die neu aufgebauten Einstellungen-Seiten angleichen
            _einstellungenMapper?.BuildItems();

            // Jetzt rufen wir die korrigierte Listenbefüllung auf
            BefuelleQuellenListe(tool, ctrl);

        }

        private void BefuelleQuellenListe(string[] tool, KonfigurationCtrl ctrl)
        {
            _hoverIndex = -1; // Hover-Markierung beim Neuaufbau zurücksetzen

            // Listeneigenschaften für die saubere Detail-Darstellung erzwingen
            listViewQuellen.View = View.Details;
            listViewQuellen.FullRowSelect = true;
            listViewQuellen.HeaderStyle = ColumnHeaderStyle.None;

            // HIER DIE ANPASSUNG: Schriftart fest auf Segoe UI, 9.75pt (oder 10f) einstellen
            listViewQuellen.Font = new Font("Segoe UI", 12f, FontStyle.Regular);

            listViewQuellen.Columns.Clear();
            // Verwende die Breite von Panel1 abzüglich eines kleinen Puffers (25), 
            // damit keine horizontale Scrollleiste entsteht
            listViewQuellen.Columns.Add("Komponente", splitContainer_Parameter.Panel1.Width - 4);

            listViewQuellen.Items.Clear();

            // --- POS 1: Wärme-/Strombedarf (MUSS IMMER AN ERSTER STELLE SEIN) ---
            ListViewItem itemBedarf = new ListViewItem(MyResource.Resource.SIM_MENUE_ENERGIEBEDARF);
            itemBedarf.Tag = "tabPage_Bedarf";
            listViewQuellen.Items.Add(itemBedarf);

            // --- POS 2: Dynamische Erzeuger (Egal in welcher der Boxen 1-4 ausgewählt!) ---
            List<string> hinzugefuegteGerete = new List<string>();

            for (int i = 0; i < 4; i++)
            {
                if (!string.IsNullOrEmpty(tool[i]))
                {
                    string geraetName = tool[i].Trim();

                    // Verhindert doppelte Einträge in der Liste
                    if (hinzugefuegteGerete.Contains(geraetName))
                        continue;

                    if (geraetName == DbWerte.ERZEUGER_WAERMEPUMPE)
                    {
                        ListViewItem itemWP = new ListViewItem(MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE);
                        itemWP.Tag = "tabPage_Wärmepumpe";
                        listViewQuellen.Items.Add(itemWP);
                        hinzugefuegteGerete.Add(geraetName);
                    }
                    else if (geraetName == DbWerte.ERZEUGER_HEIZKESSEL)
                    {
                        ListViewItem itemKessel = new ListViewItem(MyResource.Resource.SIM_ERZEUGERNAME_HEIZKESSEL);
                        itemKessel.Tag = "tabPage_Heizkessel";
                        listViewQuellen.Items.Add(itemKessel);
                        hinzugefuegteGerete.Add(geraetName);
                    }
                    else if (geraetName == DbWerte.ERZEUGER_BHKW)
                    {
                        ListViewItem itemBHKW = new ListViewItem(MyResource.Resource.SIM_ERZEUGERNAME_BHKW);
                        itemBHKW.Tag = "tabPage_BHKW";
                        listViewQuellen.Items.Add(itemBHKW);
                        hinzugefuegteGerete.Add(geraetName);
                    }
                    else if (geraetName == DbWerte.ERZEUGER_SOLARTHERMIE)
                    {
                        ListViewItem itemSolar = new ListViewItem(MyResource.Resource.SIM_ERZEUGERNAME_SOLARTHERMIE);
                        itemSolar.Tag = "tabPage_Solarthermie";
                        listViewQuellen.Items.Add(itemSolar);
                        hinzugefuegteGerete.Add(geraetName);
                    }
                }
            }

            // --- POS 3: Photovoltaik (Fest zugewiesen an Tool 5 / Index 4) ---
            if (tool[4] == DbWerte.ERZEUGER_PHOTOVOLTAIK || tool[4] == "true" || tool.Contains(DbWerte.ERZEUGER_PHOTOVOLTAIK))
            {
                ListViewItem itemPV = new ListViewItem(MyResource.Resource.SIM_PHOTOVOLTAIK);
                itemPV.Tag = "tabPage_Photovoltaik";
                listViewQuellen.Items.Add(itemPV);
            }

            // --- POS 4: Stromspeicher (Fest zugewiesen an Tool 6 / Index 5) ---
            if (tool[5] == DbWerte.ERZEUGER_STROMSPEICHER || tool[5] == "true" || tool.Contains(DbWerte.ERZEUGER_STROMSPEICHER))
            {
                ListViewItem itemSpeicher = new ListViewItem(MyResource.Resource.SIM_STROMSPEICHER);
                itemSpeicher.Tag = "tabPage_Stromspeicher";
                listViewQuellen.Items.Add(itemSpeicher);
            }

            // --- AM ENDE DER LISTE: Ergebnisauswertung (MUSS IMMER DA SEIN) ---
            ListViewItem itemErgebnis = new ListViewItem(MyResource.Resource.SIM_ERGEBNIS);
            itemErgebnis.Tag = "tabPage_Ergebnis";
            listViewQuellen.Items.Add(itemErgebnis);

            // Panel-Breite an den breitesten Eintrag anpassen.
            AdjustQuellenPanelWidth();
        }

        // Passt die Breite des linken Menue-Panels (splitContainer_Parameter.Panel1)
        // an den breitesten Listeneintrag an: Icon + Abstand + gemessene Textbreite + Rand.
        private void AdjustQuellenPanelWidth()
        {
            if (listViewQuellen == null || splitContainer_Parameter == null || listViewQuellen.Items.Count == 0) return;

            int maxText = 0;
            foreach (ListViewItem it in listViewQuellen.Items)
            {
                int w = TextRenderer.MeasureText(it.Text, listViewQuellen.Font).Width;
                if (w > maxText) maxText = w;
            }

            // Layout aus listViewQuellen_DrawItem: Icon links 16 + Icon 22 + Abstand 12 = 50,
            // plus rechter Rand und Reserve fuer die vertikale Scrollbar.
            int needed = 50 + maxText + 20;

            int min = splitContainer_Parameter.Panel1MinSize;
            int max = splitContainer_Parameter.Width - splitContainer_Parameter.Panel2MinSize - splitContainer_Parameter.SplitterWidth;
            if (max > min && needed > max) needed = max;
            if (needed < min) needed = min;

            try { splitContainer_Parameter.SplitterDistance = needed; } catch { }

            if (listViewQuellen.Columns.Count > 0)
                listViewQuellen.Columns[0].Width = listViewQuellen.ClientSize.Width;
            listViewQuellen.TileSize = new System.Drawing.Size(listViewQuellen.ClientSize.Width, 40);
        }

        // ============================================================
        //  Menü-Optik für listViewQuellen (dunkles WordPress-Stil-Menü)
        //  Rein visuell – ergänzt das bestehende Verhalten, ohne den
        //  Designer oder die .resx-Dateien zu verändern.
        // ============================================================
        private void StyleQuellenListeAlsMenu()
        {
            if (_quellenMenuStyled || listViewQuellen == null) return;
            _quellenMenuStyled = true;

            // Zeilenhöhe über eine (leere) SmallImageList erzwingen (~40 px).
            _quellenRowSizer = new ImageList();
            _quellenRowSizer.ImageSize = new Size(1, 40);
            _quellenRowSizer.ColorDepth = ColorDepth.Depth32Bit;
            listViewQuellen.SmallImageList = _quellenRowSizer;

            listViewQuellen.OwnerDraw = true;
            listViewQuellen.BorderStyle = BorderStyle.None;
            listViewQuellen.BackColor = cMenuBase;
            listViewQuellen.ForeColor = cMenuText;

            // Linkes Panel auf dieselbe Grundfarbe -> nahtlose dunkle Spalte.
            if (splitContainer_Parameter != null)
                splitContainer_Parameter.Panel1.BackColor = cMenuBase;

            // Flicker beim Hover reduzieren (DoubleBuffering der ListView).
            try
            {
                typeof(ListView).GetProperty("DoubleBuffered",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(listViewQuellen, true, null);
            }
            catch { /* unkritisch */ }

            // Eigene Zeichen- und Hover-Logik verdrahten (einmalig).
            listViewQuellen.DrawColumnHeader += (s, e) => { /* Header ist ausgeblendet */ };
            listViewQuellen.DrawSubItem += (s, e) => { /* gesamte Zeile wird in DrawItem gezeichnet */ };
            listViewQuellen.DrawItem += listViewQuellen_DrawItem;
            listViewQuellen.MouseMove += listViewQuellen_MouseMove;
            listViewQuellen.MouseLeave += listViewQuellen_MouseLeave;
        }

        private void listViewQuellen_MouseMove(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit = listViewQuellen.HitTest(e.Location);
            int idx = (hit != null && hit.Item != null) ? hit.Item.Index : -1;
            if (idx == _hoverIndex) return;

            int alt = _hoverIndex;
            _hoverIndex = idx;
            if (alt >= 0 && alt < listViewQuellen.Items.Count)
                listViewQuellen.Invalidate(listViewQuellen.Items[alt].Bounds);
            if (idx >= 0 && idx < listViewQuellen.Items.Count)
                listViewQuellen.Invalidate(listViewQuellen.Items[idx].Bounds);
        }

        private void listViewQuellen_MouseLeave(object sender, EventArgs e)
        {
            if (_hoverIndex < 0) return;
            int alt = _hoverIndex;
            _hoverIndex = -1;
            if (alt < listViewQuellen.Items.Count)
                listViewQuellen.Invalidate(listViewQuellen.Items[alt].Bounds);
        }

        private void listViewQuellen_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle r = e.Bounds;

            string tag = (e.Item.Tag != null) ? e.Item.Tag.ToString() : "";
            bool disabled = (tag == "DEAKTIVIERT");
            bool selected = e.Item.Selected && !disabled;
            bool hot = (e.ItemIndex == _hoverIndex) && !selected && !disabled;

            Color bg = selected ? cMenuSelBg : (hot ? cMenuHoverBg : cMenuBase);
            Color fg = disabled ? cMenuDisabled : (selected ? cMenuSelFg : (hot ? cMenuHoverFg : cMenuText));
            Color ic = disabled ? cMenuDisabled : (selected ? cMenuSelFg : (hot ? cMenuHoverFg : cMenuIcon));

            using (SolidBrush b = new SolidBrush(bg))
                g.FillRectangle(b, r);

            // Icon (quadratisch, vertikal zentriert)
            int s = 22;
            int iconX = r.X + 16;
            int iconY = r.Y + (r.Height - s) / 2;
            ZeichneGewerkIcon(g, new Rectangle(iconX, iconY, s, s), tag, ic);

            // Beschriftung
            int textX = iconX + s + 12;
            Rectangle textRect = new Rectangle(textX, r.Y, Math.Max(0, r.Right - textX - 8), r.Height);
            TextRenderer.DrawText(g, e.Item.Text, listViewQuellen.Font, textRect, fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        // ------------------------------------------------------------
        //  Vektor-Icons je Gewerk (einfarbig, per GDI+ gezeichnet)
        //  Zuordnung über das Item.Tag (z. B. "tabPage_BHKW").
        // ------------------------------------------------------------
        private void ZeichneGewerkIcon(Graphics g, Rectangle box, string tag, Color farbe)
        {
            System.Drawing.Drawing2D.SmoothingMode alt = g.SmoothingMode;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            float pw = Math.Max(1.8f, box.Width / 11f);
            using (Pen pen = new Pen(farbe, pw))
            using (SolidBrush brush = new SolidBrush(farbe))
            {
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;

                // normalisierte Koordinate (0..1) -> Punkt in der Box
                Func<float, float, PointF> P = (nx, ny) =>
                    new PointF(box.X + nx * box.Width, box.Y + ny * box.Height);

                switch (tag)
                {
                    case "tabPage_Bedarf": // Energiebedarf – Blitz
                        {
                            PointF[] bolt =
                            {
                            P(0.58f, 0.06f), P(0.30f, 0.54f), P(0.48f, 0.54f),
                            P(0.40f, 0.94f), P(0.74f, 0.42f), P(0.54f, 0.42f)
                        };
                            g.FillPolygon(brush, bolt);
                            break;
                        }
                    case "tabPage_Heizkessel": // Flamme
                        {
                            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                            {
                                path.AddBezier(P(0.50f, 0.96f), P(0.14f, 0.86f), P(0.16f, 0.60f), P(0.30f, 0.50f));
                                path.AddBezier(P(0.30f, 0.50f), P(0.40f, 0.42f), P(0.36f, 0.22f), P(0.50f, 0.05f));
                                path.AddBezier(P(0.50f, 0.05f), P(0.60f, 0.26f), P(0.70f, 0.34f), P(0.72f, 0.54f));
                                path.AddBezier(P(0.72f, 0.54f), P(0.80f, 0.66f), P(0.78f, 0.88f), P(0.50f, 0.96f));
                                path.CloseFigure();
                                g.FillPath(brush, path);
                            }
                            break;
                        }
                    case "tabPage_BHKW": // Zahnrad
                        ZeichneZahnrad(g, pen, brush, box, farbe);
                        break;

                    case "tabPage_Wärmepumpe": // Kreislauf (zwei Pfeile)
                        ZeichneWaermepumpe(g, pen, box);
                        break;

                    case "tabPage_Solarthermie": // Sonne
                        {
                            float cx = box.X + box.Width * 0.5f;
                            float cy = box.Y + box.Height * 0.5f;
                            float rCore = box.Width * 0.17f;
                            g.FillEllipse(brush, cx - rCore, cy - rCore, rCore * 2, rCore * 2);
                            float r1 = box.Width * 0.30f, r2 = box.Width * 0.46f;
                            for (int i = 0; i < 8; i++)
                            {
                                double a = i * Math.PI / 4.0;
                                float dx = (float)Math.Cos(a), dy = (float)Math.Sin(a);
                                g.DrawLine(pen, cx + dx * r1, cy + dy * r1, cx + dx * r2, cy + dy * r2);
                            }
                            break;
                        }
                    case "tabPage_Photovoltaik": // Solarpanel (Raster) auf Ständer
                        {
                            RectangleF panel = new RectangleF(
                                box.X + box.Width * 0.16f, box.Y + box.Height * 0.18f,
                                box.Width * 0.68f, box.Height * 0.46f);
                            g.DrawRectangle(pen, panel.X, panel.Y, panel.Width, panel.Height);
                            g.DrawLine(pen, panel.X + panel.Width / 3f, panel.Y, panel.X + panel.Width / 3f, panel.Bottom);
                            g.DrawLine(pen, panel.X + 2f * panel.Width / 3f, panel.Y, panel.X + 2f * panel.Width / 3f, panel.Bottom);
                            g.DrawLine(pen, panel.X, panel.Y + panel.Height / 2f, panel.Right, panel.Y + panel.Height / 2f);
                            g.DrawLine(pen, P(0.50f, 0.64f).X, P(0.50f, 0.64f).Y, P(0.50f, 0.90f).X, P(0.50f, 0.90f).Y);
                            g.DrawLine(pen, P(0.34f, 0.90f).X, P(0.34f, 0.90f).Y, P(0.66f, 0.90f).X, P(0.66f, 0.90f).Y);
                            break;
                        }
                    case "tabPage_Stromspeicher": // Batterie
                        {
                            RectangleF body = new RectangleF(
                                box.X + box.Width * 0.24f, box.Y + box.Height * 0.30f,
                                box.Width * 0.52f, box.Height * 0.60f);
                            g.DrawRectangle(pen, body.X, body.Y, body.Width, body.Height);
                            // Pluspol
                            g.FillRectangle(brush, P(0.42f, 0.16f).X, P(0.42f, 0.16f).Y, box.Width * 0.16f, box.Height * 0.14f);
                            // Ladestand-Linie
                            g.DrawLine(pen, P(0.34f, 0.60f).X, P(0.34f, 0.60f).Y, P(0.66f, 0.60f).X, P(0.66f, 0.60f).Y);
                            break;
                        }
                    case "tabPage_Ergebnis": // Balkendiagramm
                        {
                            g.DrawLine(pen, P(0.16f, 0.84f).X, P(0.16f, 0.84f).Y, P(0.86f, 0.84f).X, P(0.86f, 0.84f).Y);
                            float bw = box.Width * 0.12f;
                            DrawBar(g, brush, P(0.24f, 0.60f), bw, P(0.24f, 0.84f).Y);
                            DrawBar(g, brush, P(0.44f, 0.46f), bw, P(0.44f, 0.84f).Y);
                            DrawBar(g, brush, P(0.64f, 0.30f), bw, P(0.64f, 0.84f).Y);
                            break;
                        }
                    default: // u. a. "DEAKTIVIERT" oder unbekannt – schlichter Punkt
                        {
                            float d = box.Width * 0.20f;
                            g.DrawEllipse(pen, box.X + box.Width * 0.5f - d, box.Y + box.Height * 0.5f - d, d * 2, d * 2);
                            break;
                        }
                }
            }

            g.SmoothingMode = alt;
        }

        private static void DrawBar(Graphics g, SolidBrush brush, PointF topLeft, float width, float baselineY)
        {
            g.FillRectangle(brush, topLeft.X, topLeft.Y, width, baselineY - topLeft.Y);
        }

        private void ZeichneZahnrad(Graphics g, Pen pen, SolidBrush brush, Rectangle box, Color farbe)
        {
            float cx = box.X + box.Width * 0.5f, cy = box.Y + box.Height * 0.5f;
            float rRing = box.Width * 0.28f;   // Zahnkranz (Ring)
            float rTeeth = box.Width * 0.44f;  // Zahnspitzen
            float rHub = box.Width * 0.11f;    // Nabe

            using (Pen tp = new Pen(farbe, Math.Max(2.2f, box.Width / 8f)))
            {
                tp.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                tp.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                for (int i = 0; i < 8; i++)
                {
                    double a = i * Math.PI / 4.0;
                    float dx = (float)Math.Cos(a), dy = (float)Math.Sin(a);
                    g.DrawLine(tp, cx + dx * rRing, cy + dy * rRing, cx + dx * rTeeth, cy + dy * rTeeth);
                }
            }
            g.DrawEllipse(pen, cx - rRing, cy - rRing, rRing * 2, rRing * 2);
            g.FillEllipse(brush, cx - rHub, cy - rHub, rHub * 2, rHub * 2);
        }

        private void ZeichneWaermepumpe(Graphics g, Pen pen, Rectangle box)
        {
            RectangleF arc = new RectangleF(
                box.X + box.Width * 0.18f, box.Y + box.Height * 0.18f,
                box.Width * 0.64f, box.Height * 0.64f);
            float rx = arc.Width / 2f, ry = arc.Height / 2f;
            float cx = arc.X + rx, cy = arc.Y + ry;

            g.DrawArc(pen, arc.X, arc.Y, arc.Width, arc.Height, 105f, 150f);
            g.DrawArc(pen, arc.X, arc.Y, arc.Width, arc.Height, 285f, 150f);

            float ah = box.Width * 0.18f;
            DrawArcArrow(g, pen, cx, cy, rx, ry, 255f, ah); // Ende 1. Bogen (105+150)
            DrawArcArrow(g, pen, cx, cy, rx, ry, 75f, ah);  // Ende 2. Bogen (285+150=435->75)
        }

        private static void DrawArcArrow(Graphics g, Pen pen, float cx, float cy, float rx, float ry, float deg, float size)
        {
            PointF tip = new PointF(
                cx + rx * (float)Math.Cos(deg * Math.PI / 180.0),
                cy + ry * (float)Math.Sin(deg * Math.PI / 180.0));
            float pdeg = deg - 12f;
            PointF prev = new PointF(
                cx + rx * (float)Math.Cos(pdeg * Math.PI / 180.0),
                cy + ry * (float)Math.Sin(pdeg * Math.PI / 180.0));
            double ang = Math.Atan2(tip.Y - prev.Y, tip.X - prev.X);
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                double b = ang + sgn * 2.5; // ~143° Widerhaken
                PointF q = new PointF(
                    tip.X + (float)Math.Cos(b) * size,
                    tip.Y + (float)Math.Sin(b) * size);
                g.DrawLine(pen, tip, q);
            }
        }

        private void Form_Simulation_Detail_Load(object sender, EventArgs e)
        {
            // 1. Wunschgröße des gesamten Inhalts (inklusive hochskalierter Schriften) messen
            Size wunschGroesse = this.PreferredSize;

            // 2. Das Fenster nur vergrößern, wenn es aktuell kleiner als die Wunschgröße ist
            if (wunschGroesse.Width > this.Width) this.Width = wunschGroesse.Width;
            if (wunschGroesse.Height > this.Height) this.Height = wunschGroesse.Height;

            // 3. SICHERHEITS-DECKEL FÜR KLEINE NOTEBOOKS:
            // Verhindert, dass das Fenster größer wird als der eigentliche Bildschirm
            Rectangle bildschirm = Screen.FromControl(this).WorkingArea;

            if (this.Width > bildschirm.Width) this.Width = bildschirm.Width;
            if (this.Height > bildschirm.Height) this.Height = bildschirm.Height;

            // 4. Falls das Fenster maximiert gestartet werden soll (oft die sauberste Notebook-Lösung):
            // this.WindowState = FormWindowState.Maximized; 

            radioButton_Waermegefuehrt.Checked = true;

            MacheTextAbschnittFett(richTextBox_Info, MyResource.Resource.SIM_BETRIEBSART_WAERMEGEFUEHRT);
            MacheTextAbschnittFett(richTextBox_Info, MyResource.Resource.SIM_BETRIEBSART_STROMGEFUEHRT);
            MacheTextAbschnittFett(richTextBox_Info, MyResource.Resource.SIM_BETRIEBSART_OHNE_EINSPEISUNG);

            LeseKonfiguration();
            LeseSpeicherVariante();
            // PAKET BHKW-REGULÄR: Hier stand PendelspeicherFeldEinrichten(). Das Feld
            // „Volumen Pendelspeicher [l]" ist von der BHKW-Parameterseite entfallen
            // (Entscheidung des Anwenders 17.08.2026, Punkt 4) - siehe die Begründung an
            // der entfallenen Methode weiter unten.

            // Blockade bei nicht abgeschlossener Schema-Migration (ADR-001, Aufgabe 6):
            // gar nicht erst automatisch rechnen. Die Prüfung steht hier VOR dem Lauf
            // und nicht in btn_Simulation_Click allein, damit die Meldung auch dann
            // kommt, wenn schon Tab_Einstellungen nicht lesbar ist - und sie kommt genau
            // einmal, weil dieser Zweig den automatischen Lauf überspringt.
            if (SimulationBlockiert())
            {
                tabControl_Simulation.SelectedTab = tabPage_Uebersicht;
                return;
            }

            // Beim Öffnen automatisch simulieren (wie btn_Simulation in Form_Simulation_Kurz)
            // und anschließend den Übersicht-Tab in den Vordergrund holen.
            ctrl.ReadSingle("select * from Tab_Einstellungen where ID_Projekt=" + m_ID_Projekt);
            if (ctrl.rows > 0)
            {
                btn_Simulation_Click(this, EventArgs.Empty);
            }
            tabControl_Simulation.SelectedTab = tabPage_Uebersicht;
        }

        private void MacheTextAbschnittFett(RichTextBox rtb, string textZuFormatieren)
        {
            int index = rtb.Text.IndexOf(textZuFormatieren);
            if (index != -1)
            {
                rtb.Select(index, textZuFormatieren.Length);
                rtb.SelectionFont = new Font(rtb.Font, FontStyle.Bold);
                rtb.SelectionLength = 0; // Auswahl aufheben
            }
        }

        private void init_Chart(Chart chart)
        {
            var ca = chart.ChartAreas[0];

            // Plotflaeche neutral weiss (konsistent zu den ChartManager-Diagrammen)
            ca.BackColor = Color.White;
            ca.BackGradientStyle = GradientStyle.None;

            // Enable cursors and selections
            ca.CursorX.IsUserEnabled = true;
            ca.CursorX.IsUserSelectionEnabled = true;
            ca.CursorY.IsUserEnabled = true;
            ca.CursorY.IsUserSelectionEnabled = true;

            // Allow zooming on both axes
            ca.AxisY.ScaleView.Zoomable = true;
            ca.AxisX.ScaleView.Zoomable = true;

            ca.AxisX.ScaleView.SmallScrollSize = 1;

            chart.ChartAreas[0].CursorX.Interval = 0;
            ca.AxisX.Minimum = 0;
            ca.AxisY.Maximum = 100.2;

            chart.Series[0].BorderWidth = 2;
            chart.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            chart.ChartAreas[0].AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            chart.ChartAreas[0].CursorX.LineDashStyle = ChartDashStyle.Dot;
            chart.ChartAreas[0].CursorY.LineDashStyle = ChartDashStyle.Dot;
            chart.ChartAreas[0].CursorX.LineColor = Color.Red;
            chart.ChartAreas[0].CursorY.LineColor = Color.Red;
        }

        private void btn_StromDetails_Click(object sender, EventArgs e)
        {
            Form_ErgStromverbraucher frm = new Form_ErgStromverbraucher();
            frm.Init(simulation_Strombedarf);
            frm.ShowDialog();
        }

        /// <summary>
        /// Blockade bei nicht abgeschlossener Schema-Migration (ADR-001, Aufgabe 6).
        ///
        /// Sitzt VOR jedem Rechenlauf dieses Formulars - der automatische aus
        /// <c>Form_Simulation_Detail_Load</c> läuft über dieselbe Methode
        /// <c>btn_Simulation_Click</c>, es gibt also genau eine Prüfung und genau eine
        /// Meldung.
        ///
        /// Vorher fiel die Sperre hier gar nicht auf: <c>SimulationControl.Do_Simulation</c>
        /// kehrt zwar früh zurück (dialogfrei, wie es sich für die Engine gehört), das
        /// Formular rechnete danach aber ungerührt <c>Endergebniss_Simulation</c> und
        /// <c>FuelleUebersicht</c> auf leeren Objekten - der Anwender sah ein
        /// vollständig aussehendes Ergebnis aus Nullwerten und konnte es speichern.
        /// </summary>
        /// <returns>true, wenn NICHT gerechnet werden darf.</returns>
        private bool SimulationBlockiert()
        {
            string sperrgrund;
            if (!SchemaMigration.SimulationGesperrt(out sperrgrund)) return false;

            // Kein Ergebnis darf entstehen - also auch keines gespeichert werden.
            // (Nacharbeit Paket 8, Befund N1: über dieselbe Zustandsmaschine wie alle
            // übrigen Sperrstellen, damit der Knopf und das Merkmal nie auseinanderlaufen.)
            ErgebnisUngueltig();

            MessageBox.Show(sperrgrund, MyResource.Resource.SIM_TITEL_SIMULATION_NICHT_VERFUEGBAR,
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return true;
        }

        private void btn_Simulation_Click(object sender, EventArgs e)
        {
            // NACHARBEIT PAKET 8, BEFUND N1 — Zustandsmaschine „Ergebnis speichern".
            //
            // ZUERST, vor jeder anderen Prüfung: Ab hier ist das angezeigte Ergebnis
            // nicht mehr gültig. Jeder Frühausstieg dieser Methode (Migrationssperre,
            // fehlende Konfiguration, Abbruch der Bedarfsrechnung, Abbruch der Kaskade)
            // lässt den Knopf damit gesperrt zurück — vorher blieb er bei zwei dieser
            // Wege aktiv, und ein Klick hätte das gültige Bestandsergebnis des Projekts
            // durch einen Nullsatz ersetzt.
            ErgebnisUngueltig();

            // Blockade zuerst: weder rechnen noch Ergebnisfelder füllen, solange die
            // Datenbank nicht auf dem benötigten Stand ist.
            if (SimulationBlockiert()) return;

            // PAKET 8 (Konzept 13.4): EIN Protokollkanal je Lauf, angelegt VOR der
            // Bedarfsrechnung - auch SimulationWaermebedarf und SimulationStrombedarf
            // melden dorthin, und beide laufen vor der Kaskade.
            SimulationProtokoll.NeuStarten();
            LaufmeldungenLeeren();

            // TextBoxe leeren
            for (int i = 0; i < tabControl_Simulation.TabCount; i++)
            {
                InitTextBoxen(tabControl_Simulation.TabPages[i]);
            }

            m_Waermebedarf_Gesamt = simulation_Waermebedarf.Waermebedarf_Gesamt;
            m_Strombedarf_Gesamt = simulation_Strombedarf.Strombedarf_gesamt;
            textBox_gesStrombedarf.Text = m_Strombedarf_Gesamt.ToString("F2");
            textBox_gesWaermebedarf.Text = m_Waermebedarf_Gesamt.ToString("F2");

            // Konfiguration auslesen
            ctrl.ReadSingle("select * from Tab_Einstellungen where ID_Projekt=" + m_ID_Projekt);
            if (ctrl.rows == 0)
            {
                MessageBox.Show(MyResource.Resource.SIM_MSG_KONFIGURATION_FEHLT, MyResource.Resource.SIM_TITEL_FEHLER, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string[] tool = new string[6];
            tool[0] = ctrl.model.m_Tool_1;
            tool[1] = ctrl.model.m_Tool_2;
            tool[2] = ctrl.model.m_Tool_3;
            tool[3] = ctrl.model.m_Tool_4;
            tool[4] = ctrl.model.m_Tool_5;
            tool[5] = ctrl.model.m_Tool_6;

            // Nacharbeit Paket 8, Befund N6: Auch dieser Frühausstieg zeigt die Warnungen
            // des bisher Gerechneten in der Fußzeile - der Knopf „Ergebnis speichern"
            // bleibt dabei gesperrt (Befund N1, gesetzt am Kopf dieser Methode).
            if (!Energiebedarf(ctrl.m_Netzverluste, ctrl.m_szNetzverlusteEinheit))
            {
                LaufmeldungenAnzeigen();
                return;
            }

            // Wärmebedarf und Strombedarf Simulation durchführen
            sim.tool = tool;
            sim.Stundentemperatur = simulation_Waermebedarf.Stundentemperatur;
            sim.simulation_Waermebedarf = simulation_Waermebedarf;
            sim.simulation_Strombedarf = simulation_Strombedarf;
            sim.ctrl_konfig = ctrl;

            textBox_gesStrombedarf.Text = simulation_Strombedarf.Strombedarf_gesamt.ToString("F2");
            textBox_gesWaermebedarf.Text = simulation_Waermebedarf.Waermebedarf_Gesamt.ToString("F2");

            sim.GrenzleistungBHKW = (int)numericUpDown_UnteresteLG.Value;
            // Etappe 3: Das Pendelspeichervolumen kommt aus dem Projekt-Puffer
            // "BHKW-Pendelspeicher" in LITERN - dieselbe Quelle wie im SimulationRunner.
            // Das Eingabefeld schreibt beim Verlassen dorthin, gerechnet wird also mit
            // dem gespeicherten Stand (Tab_Einstellungen.Pendelspeicher ist tot).
            sim.VolumenPendelspeicherBHKW = PufferSpCtrl.PendelspeicherVolumenLiter(m_ID_Projekt);
            sim.modeBHKW = bhkwSimulationsArt;

            // Tool Simulation WP, SPK usw. durchführen
            sim.Do_Simulation(m_ID_Projekt);

            // PAKET 8 (Konzept 13.4) — Auswertung der beiden Kanäle NACH dem Lauf.
            //
            // Bis hierher wertete das Formular weder Sperrgrund noch Fehlertext aus: Die
            // Engine hatte ihre Meldung selbst als MessageBox gezeigt, und der Umbau auf
            // den Fehlerkanal (Paket 5/6) hatte diesen Dialog stillschweigend entfallen
            // lassen - ein abgebrochener Lauf sah aus wie ein Ergebnis aus Nullwerten
            // und ließ sich speichern. Das ist der dokumentierte offene Punkt aus
            // Paket 5 (N10) und wird hier geschlossen.
            if (LaufAbgebrochen()) return;

            Endergebniss_Simulation();

            // Inhalt des Übersicht-Tabs aktualisieren (wie zuvor in Form_Simulation_Kurz.btn_Simulation_Click)
            FuelleUebersicht();

            // NACHARBEIT PAKET 8, BEFUND N1: Erst JETZT ist ein Ergebnis da, das
            // gespeichert werden darf - und erst jetzt wird der Knopf wieder frei. Ohne
            // diese Zeile bliebe er nach dem ersten abgebrochenen Lauf für immer gesperrt
            // (LaufAbgebrochen() setzte ihn aus, und niemand setzte ihn je zurück): Ein
            // korrigierter Erfolgslauf im selben Fenster ließ sich nicht speichern.
            ErgebnisGueltig();

            // Warnungen und Hinweise nicht-modal in der Fußzeile - sie halten den
            // Anwender nicht auf, sind aber sichtbar (bisher standen sie nur auf einer
            // Konsole, die im Programm niemand sieht).
            LaufmeldungenAnzeigen();

            tabControl_Simulation.SelectedTab = tabPage_Simulation;
            if (tabControl_Simulation.SelectedTab.Name == "tabPage_Simulation")
            {
                listViewQuellen.SelectedIndices.Add(0);
            }

        }

        /// <summary>
        /// Wertet die ABBRUCH-Gründe des Laufs aus (Paket 8, Konzept 13.4) und meldet sie
        /// als Dialog — hier in der Oberfläche ist ein Dialog richtig, mitten in der
        /// Kaskade war er es nie.
        ///
        /// Zwei Quellen, in dieser Reihenfolge:
        ///   <c>sim.Sperrgrund</c>  — der Lauf ist gar nicht erst angelaufen (Migration).
        ///   <c>sim.Fehlertext</c>  — ein Erzeugermodul hat abgebrochen (fehlende
        ///                            WP-Kennlinie, verbotene Extrapolation, Kessel nicht
        ///                            hinterlegt, mehr als MAX_BHKW, Pendelspeicher ohne
        ///                            Puffer-Zeile).
        ///
        /// In beiden Fällen bleiben Ergebnisfelder und Diagramme unangetastet und
        /// „Ergebnis speichern" ist gesperrt: Ein unvollständiger Lauf darf kein
        /// Ergebnis hinterlassen — dieselbe Regel, nach der <c>SimulationRunner</c>
        /// headless verfährt.
        /// </summary>
        /// <returns>true, wenn der Lauf abgebrochen ist.</returns>
        private bool LaufAbgebrochen()
        {
            string grund = !string.IsNullOrEmpty(sim.Sperrgrund) ? sim.Sperrgrund : sim.Fehlertext;
            if (string.IsNullOrEmpty(grund)) return false;

            ErgebnisUngueltig();

            // NACHARBEIT PAKET 8, BEFUNDE N6 und N12b — was im Dialog steht und was nicht.
            //
            // In den Dialog gehören der Abbruchgrund und die FEHLER des Kanals. Die
            // Module legen in ihren Fehlertext einen kurzen, allgemeinen Satz; die
            // sprechende Diagnose (Ausnahmetext, betroffenes Stromprofil, betroffene
            // Anlage) steht nur im Fehlerkanal und erreichte die Oberfläche bisher nie.
            //
            // Die WARNUNGEN gehören NICHT in denselben Dialog: Sie standen bis zur
            // Nacharbeit doppelt da - einmal hier und einmal in der Fußzeile. Sie bleiben
            // in der Fußzeile, wo sie den Anwender nicht aufhalten.
            string fehlerZusatz = SimulationProtokoll.Aktuell.FehlertextFuerAnzeige(grund);
            string text = string.IsNullOrEmpty(fehlerZusatz)
                ? grund
                : grund + Environment.NewLine + Environment.NewLine + MyResource.Resource.SIM_MSG_WEITERE_FEHLERMELDUNGEN +
                  Environment.NewLine + fehlerZusatz;

            MessageBox.Show(text, MyResource.Resource.SIM_TITEL_SIMULATION_ABGEBROCHEN, MessageBoxButtons.OK, MessageBoxIcon.Warning);

            LaufmeldungenAnzeigen();
            return true;
        }

        /// <summary>
        /// Sperrt „Ergebnis speichern" (Nacharbeit Paket 8, Befund N1). Aufzurufen, sobald
        /// feststeht, dass die angezeigten Werte kein vollständiges Laufergebnis sind.
        /// </summary>
        private void ErgebnisUngueltig()
        {
            _ergebnisGueltig = false;
            btn_ErgebnisSpeichern.Enabled = false;
        }

        /// <summary>
        /// Gibt „Ergebnis speichern" frei (Nacharbeit Paket 8, Befund N1). Aufzurufen
        /// ausschließlich nach einem vollständig durchgelaufenen Lauf.
        /// </summary>
        private void ErgebnisGueltig()
        {
            _ergebnisGueltig = true;
            btn_ErgebnisSpeichern.Enabled = true;
        }

        /// <summary>
        /// Zeigt Warnungen und Hinweise des Laufs NICHT-MODAL an (Paket 8, Konzept 13.4):
        /// eine Zeile in der Fußzeile neben dem Simulationsknopf, der vollständige Text
        /// im Mouseover und per Klick in einem sammelnden Dialog.
        ///
        /// Bewusst kein Layout-Umbau: Das Label entsteht programmatisch und richtet sich
        /// an <c>btn_Simulation</c> aus — dasselbe Muster wie die Fußzeile aus Paket 2 in
        /// <c>Form_Simulation_Config</c>. Designer und .resx bleiben unangetastet.
        ///
        /// SAMMELND statt n Einzelmeldungen: Genau das nennt Konzept 13.4 als spürbare
        /// Verbesserung — die Engine konnte bisher dutzende Dialoge nacheinander zeigen.
        /// </summary>
        private void LaufmeldungenAnzeigen()
        {
            SimulationProtokoll p = SimulationProtokoll.Aktuell;
            int anzahl = p.AnzahlWarnungenUndHinweise;
            if (anzahl == 0) { LaufmeldungenLeeren(); return; }

            LaufmeldungenLabelSicherstellen();

            _laufmeldungenText = p.HinweistextFuerAnzeige();
            label_Laufmeldungen.Visible = true;
            label_Laufmeldungen.Text = anzahl == 1
                ? MyResource.Resource.SIM_LAUFMELDUNG_EINER
                : string.Format(MyResource.Resource.SIM_LAUFMELDUNG_MEHRERE, anzahl);
            tooltip.SetToolTip(label_Laufmeldungen, _laufmeldungenText);
        }

        /// <summary>Blendet die Meldungszeile aus (Beginn eines neuen Laufs).</summary>
        private void LaufmeldungenLeeren()
        {
            _laufmeldungenText = "";
            if (label_Laufmeldungen != null) label_Laufmeldungen.Visible = false;
        }

        private void LaufmeldungenLabelSicherstellen()
        {
            if (label_Laufmeldungen != null) return;

            label_Laufmeldungen = new Label();
            label_Laufmeldungen.Name = "label_Laufmeldungen";
            label_Laufmeldungen.AutoSize = false;
            label_Laufmeldungen.TextAlign = ContentAlignment.MiddleLeft;
            label_Laufmeldungen.ForeColor = Color.FromArgb(0x8A, 0x53, 0x00);   // gedecktes Bernstein
            label_Laufmeldungen.Cursor = Cursors.Hand;
            label_Laufmeldungen.Location = new Point(btn_Simulation.Right + 16, btn_Simulation.Top + 8);
            label_Laufmeldungen.Size = new Size(440, 24);
            label_Laufmeldungen.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label_Laufmeldungen.Click += label_Laufmeldungen_Click;
            this.Controls.Add(label_Laufmeldungen);
            label_Laufmeldungen.BringToFront();
        }

        private void label_Laufmeldungen_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_laufmeldungenText)) return;
            MessageBox.Show(_laufmeldungenText, MyResource.Resource.SIM_TITEL_MELDUNGEN_LAUF,
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Button-Handler: speichert das aktuelle Gesamtergebnis der Simulation.
        private void btn_ErgebnisSpeichern_Click(object sender, EventArgs e)
        {
            SpeichereErgebnis();
        }

        // Bildet aus den aktuellen Simulationsobjekten ein ErgebnisModel und speichert es
        // (Strategie: letztes Ergebnis je Projekt -> ErgebnisCtrl.Save loescht das bisherige).
        private void SpeichereErgebnis()
        {
            if (m_ID_Projekt <= 0)
            {
                MessageBox.Show(MyResource.Resource.SIM_MSG_KEIN_PROJEKT, MyResource.Resource.SIM_TITEL_HINWEIS);
                return;
            }

            // NACHARBEIT PAKET 8, BEFUND N1 — der eigentliche Schutz.
            //
            // ErgebnisCtrl.Save LÖSCHT das bisherige Ergebnis des Projekts, bevor es das
            // neue schreibt (Strategie „letztes Ergebnis je Projekt"). Ohne diesen
            // Frühausstieg würde ein Klick nach einem abgebrochenen Lauf - oder direkt
            // nach dem Öffnen des Formulars, wo die Simulationsobjekte leer sind - ein
            // gültiges Bestandsergebnis durch einen Nullsatz ersetzen. Der gesperrte
            // Knopf allein reicht als Schutz nicht: Er ist im Designer aktiviert.
            if (!_ergebnisGueltig)
            {
                MessageBox.Show(
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIM_MSG_KEIN_VOLLSTAENDIGES_ERGEBNIS),
                    MyResource.Resource.SIM_TITEL_ERGEBNIS_SPEICHERN, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Ergebnismodell zentral über den SimulationRunner aufbauen (eine Quelle der Wahrheit).
            ErgebnisModel m = SimulationRunner.BaueErgebnis(m_ID_Projekt,
                simulation_Waermebedarf, simulation_Strombedarf, sim);

            int id = new ErgebnisCtrl().Save(m);
            if (id > 0)
            {
                // AP3b: Die Speicherübersicht in FormMain zeigt Ertrag und Amortisation
                // der ZULETZT GESPEICHERTEN Rechnung - ohne diese Auffrischung stünden
                // dort bis zum nächsten Projektwechsel die Werte des Vorlaufs.
                try
                {
                    projektCtrl.ReadSingle(m_ID_Projekt);
                    if (Program.mainfrm != null) Program.mainfrm.SetSPControl(projektCtrl.m_szProjektname);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Die Speicheruebersicht konnte nicht aufgefrischt werden: " + ex.Message);
                }

                MessageBox.Show(MyResource.Resource.SIM_MSG_ERGEBNIS_GESPEICHERT, MyResource.Resource.SIM_ERGEBNIS);
            }
            else
                MessageBox.Show(MyResource.Resource.SIM_MSG_ERGEBNIS_NICHT_GESPEICHERT, MyResource.Resource.SIM_TITEL_FEHLER);
        }

        // Befüllt den Übersicht-Tab mit den Simulationsergebnissen
        // (entspricht der Ergebnisdarstellung aus Form_Simulation_Kurz.btn_Simulation_Click).
        private void FuelleUebersicht()
        {
            ueb_textBox_gesStrombedarf.Text = simulation_Strombedarf.Strombedarf_gesamt.ToString("F2");
            ueb_textBox_gesWaermebedarf.Text = simulation_Waermebedarf.Waermebedarf_Gesamt.ToString("F2");

            ueb_textBox_Restwaermebedarf.Text = sim.Restwaerme.ToString("F2");
            ueb_textBox_Reststrombedarf.Text = sim.Reststrom.ToString("F2");
            ueb_textBox_WPWaermeproduktion.Text = (sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000).ToString("F2");
            ueb_textBox_WPStromverbrauch.Text = (sim.simulation_wp.WP_Strombedarf_gesamt / 1000).ToString("F2");
            ueb_textBox_SPKWaermeproduktion.Text = sim.simulation_spk.S_Waerme_spk.ToString("F2");
            ueb_textBox_HeizstabStromverbrauch.Text = (sim.simulation_wp.Heizstab_gesamt / 1000).ToString("F2");
            ueb_textBox_SPKStromverbrauch.Text = sim.simulation_spk.Stromverbrauch_Spk.ToString("F2");
            ueb_textBox_BHKWWaermeproduktion.Text = sim.simulation_bhkw.Waermeproduktion_BHKW_MWh.ToString("F2");
            ueb_textBox_BHKWStromproduktion.Text = sim.simulation_bhkw.Stromproduktion_BHKW_MWh.ToString("F2");

            // Diese beiden Felder blieben bisher LEER: sie standen im Designer, wurden aber
            // nie beschrieben. In einem Projekt mit Solarthermie bzw. PV zeigte die
            // Übersicht deshalb eine leere Zeile statt des Ergebnisses. Umrechnung wie in
            // NavigatorUebersicht.SetControl (kWh -> MWh/a).
            ueb_textBox_SWWaermeproduktion.Text = (sim.simulation_solarthermie.Waermeproduktion_gesamt / 1000).ToString("F2");
            ueb_textBox_PVStromproduktion.Text = (sim.simulation_pv.Stromproduktion_gesamt / 1000).ToString("F2");

            // Zeilen nicht vorhandener Komponenten ausblenden und die übrigen nachrücken
            // lassen (siehe UebersichtZeilenAnpassen).
            UebersichtZeilenAnpassen(ErgebnisPraesenz.Ermitteln(sim));

            ueb_chart.Series[0].Points.Clear();
            if (sim.simulation_wp.WP_Waermeproduktion_gesamt > 0)
                ueb_chart.Series[0].Points.AddXY(MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE, sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000);
            if (sim.simulation_wp.Heizstab_gesamt > 0)
                ueb_chart.Series[0].Points.AddXY(MyResource.Resource.CHART_SEGMENT_HEIZSTAB, sim.simulation_wp.Heizstab_gesamt / 1000);
            if (sim.simulation_spk.S_Waerme_spk > 0)
                ueb_chart.Series[0].Points.AddXY(MyResource.Resource.SIM_ERZEUGERNAME_HEIZKESSEL, sim.simulation_spk.S_Waerme_spk);
            if (sim.simulation_bhkw.Waermeproduktion_BHKW_MWh > 0)
                ueb_chart.Series[0].Points.AddXY(MyResource.Resource.SIM_ERZEUGERNAME_BHKW, sim.simulation_bhkw.Waermeproduktion_BHKW_MWh);
            if (sim.Restwaerme > 0)
                ueb_chart.Series[0].Points.AddXY(MyResource.Resource.CHART_SEGMENT_REST, sim.Restwaerme);
        }

        // ====================================================================
        //  Übersicht-Reiter: nur vorhandene Komponenten zeigen
        // ====================================================================
        //
        // Der Ergebnisblock des Reiters bestand aus fest platzierten Zeilen für ALLE
        // Komponenten. In einem Projekt aus Wärmepumpe und Kessel standen dort trotzdem
        // „Wärmeproduktion BHKW: 0,00", eine leere „Solare Wärme" und eine leere
        // „Stromproduktion PV" — Zeilen ohne Aussage, die den Blick auf die drei
        // relevanten Zahlen verstellten.
        //
        // Die Zeilen werden deshalb nach der Präsenzregel (siehe ErgebnisPraesenz)
        // AUSGEBLENDET — nicht nur gesperrt — und die verbleibenden rücken auf die
        // vorderen Entwurfsplätze ihrer Spalte nach. Dass die Zielhöhen die ORIGINALEN
        // Ankerhöhen sind (und keine gerechnete Schrittweite), erhält die Abstände des
        // Entwurfs exakt; Designer und .resx bleiben unangetastet.
        //
        // Immer sichtbar bleiben der Energiebedarf-Block oben und die beiden gelben
        // Zeilen „Restwärmebedarf"/„Reststrombedarf" unten: sie beschreiben das Projekt,
        // nicht eine Komponente.

        /// <summary>
        /// Eine Zeile aus Beschriftung, Wertfeld und Einheit, die nach einer Präsenzregel
        /// ein- oder ausgeblendet wird und dabei auf die vorderen Entwurfsanker nachrückt.
        ///
        /// Die Mechanik wird an zwei Stellen gebraucht — im Übersicht-Reiter (Erzeuger)
        /// und im Brennstoffblock der Heizkessel-Seite —, deshalb steht sie hier einmal.
        /// </summary>
        private class AnkerZeile
        {
            /// <summary>Die Steuerelemente der Zeile.</summary>
            public Control[] Felder;

            /// <summary>Ankerhöhe aus dem Entwurf (kleinstes <c>Top</c> der Zeile).</summary>
            public int Anker;

            /// <summary>Abstand jedes Feldes zum Anker — hält die Zeile in sich stabil.</summary>
            public int[] Versatz;

            /// <summary>Gehört die Zeile zum aktuellen Ergebnis? Vor dem Anordnen setzen.</summary>
            public bool Sichtbar;
        }

        /// <summary>Eine Ergebniszeile der Übersicht mit ihrer Präsenzregel.</summary>
        private sealed class UebersichtZeile : AnkerZeile
        {
            /// <summary>Entscheidet, ob die Zeile zu diesem Ergebnis gehört.</summary>
            public Func<ErgebnisPraesenz, bool> Regel;
        }

        /// <summary>Erfasst Anker und Versätze einer Zeile aus den Entwurfspositionen.</summary>
        private static void AnkerErfassen(AnkerZeile z, Control[] felder)
        {
            int anker = int.MaxValue;
            foreach (Control c in felder) if (c.Top < anker) anker = c.Top;

            int[] versatz = new int[felder.Length];
            for (int i = 0; i < felder.Length; i++) versatz[i] = felder[i].Top - anker;

            z.Felder = felder;
            z.Anker = anker;
            z.Versatz = versatz;
        }

        /// <summary>
        /// Ordnet eine Zeilenspalte: sichtbare Zeilen der Reihe nach auf die vorderen
        /// Entwurfsanker, unsichtbare raus. <c>Sichtbar</c> muss gesetzt sein.
        /// </summary>
        private static void AnkerAnordnen<T>(List<T> spalte) where T : AnkerZeile
        {
            int platz = 0;
            foreach (T z in spalte)
            {
                foreach (Control c in z.Felder) c.Visible = z.Sichtbar;
                if (!z.Sichtbar) continue;

                int anker = spalte[platz].Anker;
                for (int i = 0; i < z.Felder.Length; i++)
                    z.Felder[i].Top = anker + z.Versatz[i];
                platz++;
            }
        }

        private List<UebersichtZeile> _uebSpalteWaerme;
        private List<UebersichtZeile> _uebSpalteStrom;

        /// <summary>
        /// Baut die Zeilenbeschreibung einmalig auf. Die Entwurfspositionen kommen aus der
        /// .resx (<c>resources.ApplyResources</c>) und werden hier gesichert, damit ein
        /// zweiter Lauf mit anderer Zusammenstellung wieder von den ORIGINALWERTEN ausgeht
        /// und nicht von den bereits verschobenen.
        /// </summary>
        private void UebersichtZeilenVorbereiten()
        {
            if (_uebSpalteWaerme != null) return;

            _uebSpalteWaerme = new List<UebersichtZeile>
            {
                Zeile(p => p.Waermepumpe,  ueb_label20, ueb_textBox_WPWaermeproduktion,   ueb_label23),
                Zeile(p => p.BHKW,         ueb_label18, ueb_textBox_BHKWWaermeproduktion, ueb_label64),
                Zeile(p => p.Solarthermie, ueb_label21, ueb_textBox_SWWaermeproduktion,   ueb_label19),
                Zeile(p => p.Heizkessel,   ueb_label59, ueb_textBox_SPKWaermeproduktion,  ueb_label22)
            };

            _uebSpalteStrom = new List<UebersichtZeile>
            {
                Zeile(p => p.Waermepumpe,  ueb_label32, ueb_textBox_WPStromverbrauch,       ueb_label31),
                Zeile(p => p.Heizstab,     ueb_label34, ueb_textBox_HeizstabStromverbrauch, ueb_label33),
                Zeile(p => p.BHKW,         ueb_label25, ueb_textBox_BHKWStromproduktion,    ueb_label24),
                Zeile(p => p.Photovoltaik, ueb_label27, ueb_textBox_PVStromproduktion,      ueb_label26),
                Zeile(p => p.Heizkessel,   ueb_label3,  ueb_textBox_SPKStromverbrauch,      ueb_label2)
            };
        }

        /// <summary>Hilfskonstruktor einer Zeile: Anker und Versätze aus dem Entwurf.</summary>
        private static UebersichtZeile Zeile(Func<ErgebnisPraesenz, bool> sichtbar, params Control[] felder)
        {
            UebersichtZeile z = new UebersichtZeile { Regel = sichtbar };
            AnkerErfassen(z, felder);
            return z;
        }

        /// <summary>
        /// Blendet die Zeilen nicht vorhandener Komponenten aus und lässt die übrigen
        /// nachrücken. Reine Anzeige — an den Werten und an der Persistenz ändert sich nichts.
        /// </summary>
        private void UebersichtZeilenAnpassen(ErgebnisPraesenz p)
        {
            UebersichtZeilenVorbereiten();

            tabPage_Uebersicht.SuspendLayout();
            SpalteAnordnen(_uebSpalteWaerme, p);
            SpalteAnordnen(_uebSpalteStrom, p);
            tabPage_Uebersicht.ResumeLayout();
        }

        /// <summary>
        /// Ordnet eine Spalte: sichtbare Zeilen der Reihe nach auf die vorderen
        /// Entwurfsanker, unsichtbare raus.
        /// </summary>
        private static void SpalteAnordnen(List<UebersichtZeile> spalte, ErgebnisPraesenz p)
        {
            foreach (UebersichtZeile z in spalte) z.Sichtbar = z.Regel(p);
            AnkerAnordnen(spalte);
        }

        private bool Energiebedarf(double Netzverluste, string NetzverlusteEinheit)
        {
            int netzverluste = (int)ctrl.m_Netzverluste;
            if (ctrl.m_szNetzverlusteEinheit == "%" && netzverluste > 100)
            {
                MessageBox.Show(MyResource.Resource.SIM_MSG_NETZVERLUSTE_ZU_GROSS);
                return false;
            }

            projektCtrl.ReadSingle(m_ID_Projekt);
            int nKlimaregion = projektCtrl.m_ID_Klimaregion;
            if (nKlimaregion == 0)
            {
                MessageBox.Show(MyResource.Resource.SIM_MSG_KLIMAREGION_WAEHLEN);
                return false;
            }

            // Parameter für die Wärmebedarf Simulation durchführen 
            simulation_Waermebedarf.Netzverluste = netzverluste;
            simulation_Waermebedarf.Netzverluste_Einheit = ctrl.m_szNetzverlusteEinheit;

            // Wärmebedarf Simulation
            simulation_Waermebedarf.Waermebedarf_berechnen(m_ID_Projekt, nKlimaregion);
            simulation_Strombedarf.m_ID_Projekt = m_ID_Projekt;

            // K1 (F3): denselben Klimadaten-Kalender wie die Wärmerechnung verwenden -
            // erspart der Stromrechnung die eigene Klimadaten-Lesung und schließt aus,
            // dass beide Bedarfsarten je einen anderen Wochentag ermitteln.
            simulation_Strombedarf.WochentagJan1 = simulation_Waermebedarf.WochentagJan1;

            // Strombedarf Simulation
            simulation_Strombedarf.Berechnung(m_ID_Projekt);

            // PAKET 8 (Konzept 13.4): Der Abbruch der Strombedarfsrechnung kam bisher als
            // MessageBox aus der Engine; das Formular rechnete danach mit einem leeren
            // Stromprofil weiter. Jetzt meldet die Engine über den Fehlerkanal, und der
            // Dialog steht hier - in der Oberfläche, wo er hingehört.
            if (!string.IsNullOrEmpty(simulation_Strombedarf.Fehlertext))
            {
                // NACHARBEIT PAKET 8, BEFUND N6: mit den FEHLERN des Kanals. Der
                // Fehlertext des Moduls ist bewusst allgemein ("Die Stromprofile des
                // Projekts konnten nicht berechnet werden"); die eigentliche Diagnose -
                // Ausnahmetext und betroffenes Stromprofil - steht im Fehlerkanal und
                // erreichte die Oberfläche vorher nicht. Ohne sie hat der Anwender keinen
                // Ansatzpunkt.
                string grund = simulation_Strombedarf.Fehlertext;
                string fehlerZusatz = SimulationProtokoll.Aktuell.FehlertextFuerAnzeige(grund);
                MessageBox.Show(
                    string.IsNullOrEmpty(fehlerZusatz)
                        ? grund
                        : grund + Environment.NewLine + Environment.NewLine +
                          MyResource.Resource.SIM_MSG_WEITERE_FEHLERMELDUNGEN + Environment.NewLine + fehlerZusatz,
                    MyResource.Resource.SIM_TITEL_SIMULATION_ABGEBROCHEN, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // chart Wärmebedarf füllen
            textBox_MaxWaermelast.Text = simulation_Waermebedarf.Waermebedarf_Max.ToString("F2");
            textBox_Gesamt_Waermebedarf.Text = simulation_Waermebedarf.Waermebedarf_Gesamt.ToString("F2");

            // PAKET E1 (Konzept 4.4): die drei Bedarfskanäle unter der Summe.
            BedarfKanalzeilenFuellen();

            chart1.Annotations.Clear();
            chart1.Series[0].Points.Clear();
            chart1.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
            chart1.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);

            if (checkBox_Sortiert.Checked)
                ConfigureXAxisWithHours(chart1, simulation_Waermebedarf.Dauerlinie);
            else
            {
                ConfigureXAxisWithMonths(chart1);
                for (int j = 0; j < 8760; j++)
                {
                    double d = (double)j * 12 / (8760);
                    chart1.Series[0].Points.AddXY(d, simulation_Waermebedarf.Dauerlinie_nicht_sortiert[j]);
                }
            }

            chart1.ChartAreas[0].AxisY.Maximum = 100.2;

            // chart Strombedarf füllen
            textBox_MaxStrombedarf.Text = simulation_Strombedarf.Strombedarf_Max.ToString("F2");
            textBox_Gesamt_Strombedarf.Text = simulation_Strombedarf.Strombedarf_gesamt.ToString("F2");

            chart2.Annotations.Clear();
            chart2.Series[0].Points.Clear();
            chart2.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
            chart2.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);

            if (checkBox_StromSortiert.Checked)
            {
                ConfigureXAxisWithHours2(chart2, 4);
                for (int j = 0; j < 8760 * 4; j += 4)
                {
                    double d = (double)j * 12 / (8760);
                    chart2.Series[0].Points.AddXY(d, simulation_Strombedarf.Dauerlinie[j]);
                }
            }
            else
            {
                ConfigureXAxisWithMonths(chart2);
                for (int j = 0; j < 8760 * 4; j += 10)
                {
                    double d = (double)j * 12 / (8760);
                    chart2.Series[0].Points.AddXY(d, simulation_Strombedarf.Dauerlinie_nicht_sortiert[j]);
                }
            }

            return true;
        }

        private void checkBox_Sortiert_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_Sortiert.Checked)
                ConfigureXAxisWithHours(chart1, simulation_Waermebedarf.Dauerlinie);
            else
            {
                ConfigureXAxisWithMonths(chart1);
                for (int j = 0; j < 8760; j++)
                {
                    double d = (double)j * 12 / (8760);
                    chart1.Series[0].Points.AddXY(d, simulation_Waermebedarf.Dauerlinie_nicht_sortiert[j]);
                }
            }
        }

        private void checkBox_StromSortiert_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_StromSortiert.Checked)
                ConfigureXAxisWithHours(chart2, simulation_Strombedarf.Dauerlinie, 4);
            else
            {
                ConfigureXAxisWithMonths(chart2);
                for (int j = 0; j < 8760 * 4; j++)
                {
                    double d = (double)j * 12 / (8760);
                    chart2.Series[0].Points.AddXY(d, simulation_Strombedarf.Dauerlinie_nicht_sortiert[j]);
                }
            }
        }

        private void btn_Details_Click(object sender, EventArgs e)
        {
            Form_ErgBrauchwasserwaerme frm = new Form_ErgBrauchwasserwaerme();
            frm.Init(simulation_Waermebedarf);
            frm.SetPage(1);
            frm.ShowDialog();
        }

        private void btn_Konfiguration_Click(object sender, EventArgs e)
        {
            Form_Simulation_Config frm = new Form_Simulation_Config();
            KonfigurationCtrl ctrl = new KonfigurationCtrl();

            mainTabPageIndex = tabControl_Simulation.SelectedIndex; // Aktuellen Index der Haupt-TabPage merken, damit wir nach dem Konfigurationsfenster dorthin zurückspringen können
            ctrl.ReadSingle("select * from Tab_Einstellungen where ID_Projekt=" + m_ID_Projekt);
            frm.Konfiguration = ctrl.model;
            frm.SetControls(m_ID_Projekt);
            System.Drawing.Point p1 = btn_Konfiguration.Location;
            p1 = this.PointToScreen(p1);
            frm.Location = p1;
            frm.ShowDialog();

            UpdateTabPages();

            tabControl_Simulation.SelectedIndex = mainTabPageIndex;
            if (tabControl_Simulation.SelectedTab.Name == "tabPage_Simulation")
            {
                //               listViewQuellen.SelectedIndices.Add(mainTablistIndex);
            }
        }

        /// <summary>
        /// Liefert den Warmwasser-(Brauchwasser-)Anteil des Wärmebedarfs als
        /// Stundenganglinie, passend zur übergebenen Bedarfsganglinie.
        ///
        /// Die Wärmepumpe sieht ggf. nur einen Teil des Gesamtbedarfs (Kaskade,
        /// vorgeschaltete Erzeuger). Der Warmwasseranteil wird deshalb je Stunde
        /// auf den tatsächlich anliegenden Bedarf begrenzt.
        /// </summary>
        private float[] WarmwasserAnteil(float[] bedarf)
        {
            float[] ww = new float[8760];
            if (simulation_Waermebedarf == null || simulation_Waermebedarf.brauchwasserwerte == null)
                return ww;

            float[] quelle = simulation_Waermebedarf.brauchwasserwerte;
            for (int i = 0; i < 8760 && i < quelle.Length; i++)
            {
                float wert = quelle[i];
                if (bedarf != null && i < bedarf.Length && wert > bedarf[i]) wert = bedarf[i];
                if (wert < 0) wert = 0;
                ww[i] = wert;
            }
            return ww;
        }

        private void Endergebniss_Simulation()
        {
            // ********************************************************************************************/
            // Wärmepumpe
            // ********************************************************************************************/
            if (sim.bSimulationWP)
            {
                // Chart Wärmepumpe Wärmerbedarf und Produktion
                _chartManager[3] = new ChartManager(chart3);
                _chartManager[3].YMaxValue = sim.simulation_Waermebedarf.Waermebedarf.Max();
                _chartManager[3].YMinValue = 0;
                _chartManager[3].XAxisAsNumber = false;
                _chartManager[3].XAxisTitle = MyResource.Resource.CHART_ACHSE_JAHRESSTUNDEN;
                _chartManager[3].YAxisTitle = MyResource.Resource.CHART_ACHSE_WAERMELAST;
                _chartManager[3].toolTipUnit = "kW";
                _chartManager[3].ChartTitle = MyResource.Resource.CHART_TITEL_WAERMELAST_JAHRESGANGLINIE;
                _chartManager[3].MitLegende = true;
                _chartManager[3].Init();

                // Wärmebedarf getrennt nach Heizwärme und Warmwasser darstellen
                float[] bedarfWW = WarmwasserAnteil(sim.simulation_wp.Waermebedarf_stuendlich);
                float[] bedarfHeizung = new float[8760];
                for (int n = 0; n < 8760; n++)
                    bedarfHeizung[n] = sim.simulation_wp.Waermebedarf_stuendlich[n] - bedarfWW[n];

                // Gestapelt, in zwei getrennten Gruppen — Begründung siehe
                // checkBox_WP_sortiert_CheckedChanged (chronologischer Zweig). Der Aufbau
                // steht hier gleich, damit beide Wege dasselbe Bild ergeben; unmittelbar
                // danach baut der Umschalter das Diagramm ohnehin einmal neu auf.
                SerieAnlegen(_chartManager[3], S_HEIZWAERMEBEDARF, MyResource.Resource.CHART_LEGENDE_HEIZWAERMEBEDARF,
                             Color.Red, bedarfHeizung, SeriesChartType.StackedArea, "Bedarf");
                SerieAnlegen(_chartManager[3], S_WARMWASSERBEDARF, MyResource.Resource.CHART_LEGENDE_WARMWASSERBEDARF,
                             Color.DeepSkyBlue, bedarfWW, SeriesChartType.StackedArea, "Bedarf");
                SerieAnlegen(_chartManager[3], S_WAERMEPRODUKTION, MyResource.Resource.CHART_LEGENDE_WAERMEPRODUKTION,
                             Color.Blue, sim.simulation_wp.WP_Waermeproduktion_stuendlich,
                             GanglinienDarstellung.Stapeltyp(false), "Produktion");
                SerieAnlegen(_chartManager[3], S_HEIZSTAB, MyResource.Resource.CHART_SEGMENT_HEIZSTAB,
                             Color.Yellow, sim.simulation_wp.Heizstab_stuendlich,
                             GanglinienDarstellung.Stapeltyp(false), "Produktion");

                // Chart Wärmepumpe Strombedarf und Produktion
                float[] temp = simulation_Strombedarf.AddVectors(sim.simulation_wp.WP_Strombedarf_stuendlich, sim.simulation_wp.Heizstab_stuendlich);
                _chartManager[6] = new ChartManager(chart6);
                _chartManager[6].YMaxValue = temp.Max();
                _chartManager[6].YMinValue = 0;
                _chartManager[6].XAxisAsNumber = false;
                _chartManager[6].XAxisTitle = MyResource.Resource.CHART_ACHSE_JAHRESSTUNDEN;
                _chartManager[6].YAxisTitle = MyResource.Resource.CHART_ACHSE_STROMBEDARF;
                _chartManager[6].toolTipUnit = "kW";
                _chartManager[6].ChartTitle = MyResource.Resource.CHART_TITEL_STROMBEDARF_JAHRESGANGLINIE;
                _chartManager[6].Init();
                SerieAnlegen(_chartManager[6], S_STROMBEDARF, MyResource.Resource.CHART_ACHSE_STROMBEDARF, Color.Red, temp);

                textBox_WB_Deckung.Text = "";
                double a = (double)simulation_Waermebedarf.Waermebedarf_Gesamt;

                // EIGENANTEIL der Wärmepumpe an der Bedarfsdeckung: unmittelbar abgegebene
                // Wärme, der ihr zugerechnete Anteil an der bedarfsdeckenden
                // Speicherentladung und der Heizstab (er gehört zur WP). Er ist die
                // Bezugsgröße von Restbedarf UND Deckungsgrad - beides zwei Seiten
                // derselben Rechnung, wortgleich mit SimulationRunner:264-351. Vorher stand
                // hier der Rest der GANZEN Speicherstufe: Ab zwei Erzeugern in der Stufe
                // enthielt der auch die Lieferung von Kessel und BHKW, die ihre Deckung
                // zusätzlich selbst melden.
                double wpStufeneingangMWh = sim.simulation_wp.Waermebedarf_gesamt / 1000.0;
                double wpEigenMWh = (sim.simulation_wp.Direktdeckung_gesamt +
                                     sim.simulation_wp.Speicherentladung_Anteil +
                                     sim.simulation_wp.Heizstab_gesamt) / 1000.0;

                // PAKET L: Hier stand die Fallunterscheidung „Speicherstufe oder Altpfad"
                // (sim.KaskadeZweikanalig). Der Altpfad ist mit Paket A1 ersatzlos
                // entfallen, das Feld war seither konstant true - es bleibt der EINE
                // Rechenweg: Rest = Stufeneingang − Eigenanteil, Deckung aus demselben
                // Eigenanteil. Der Altpfad-Zweig hätte die Jahressumme der
                // Restwärmeganglinie (waermerestbedarf_gesamt) genommen; sie ist im
                // heutigen Rechenweg keine Bilanz mehr.
                double wpRestMWh = wpStufeneingangMWh - wpEigenMWh;
                if (wpRestMWh < 0) wpRestMWh = 0;   // Rundungsschutz

                double deckung = 0;
                if (a > 0)
                {
                    deckung = wpEigenMWh / a * 100.0;   // dieselbe Größe wie im Restbedarf
                    if (deckung > 100) deckung = 100;
                    if (deckung < 0) deckung = 0;
                }
                textBox_WB_Deckung.Text = deckung.ToString("F2");


                if (sim.simulation_wp.Bivalenzpunkt != -100)
                    textBox_Bivalenzpunkt.Text = sim.simulation_wp.Bivalenzpunkt.ToString("F2");
                else
                    textBox_Bivalenzpunkt.Text = "-";

                textBox_WPWaermebedarf.Text = wpStufeneingangMWh.ToString("F2");
                textBox_WPRestwermebedarf.Text = wpRestMWh.ToString("F2");
                textBox_WPStromverbrauch.Text = (sim.simulation_wp.WP_Strombedarf_gesamt / 1000).ToString("F2");
                textBox_HeizstabStromverbrauch.Text = (sim.simulation_wp.Heizstab_gesamt / 1000).ToString("F2");
                textBox_WPWaermeproduktion.Text = (sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000).ToString("F2");
                // Speicher-Ergebnisse als kleine Tabelle statt als eine Textzeile
                // (Konzept 13.3) und die Warnungen der VDI-4640-Auslegungsprüfung
                // werden weiter unten für JEDEN Lauf gefüllt - auch für einen ohne
                // Wärmepumpe, damit die Rubrik dann geleert wird.
                textBox_WPVollbenutzungsstunden.Text = (sim.simulation_wp.WP_Laufzeit / sim.simulation_wp.wp_list.Count).ToString("F0");

                // BEWUSST weiter aus der Ganglinie und damit NICHT aus wpRestMWh: Sie führt
                // den PROJEKTrest der Stunde, und genau der ist die Bezugsgröße für die
                // Auslegung eines Spitzenkessels (siehe SimulationRunner:295-307).
                double Max_Spk = 0;
                for (int i = 0; i < 8750; i++)
                {
                    if (sim.simulation_wp.waermerestbedarf_stuendlich[i] > Max_Spk) Max_Spk = sim.simulation_wp.waermerestbedarf_stuendlich[i];
                }
                textBox_MinSPKLeistung.Text = Max_Spk.ToString("F2");

                // Ansicht & Verhalten der WP-Liste (ListView, gleiches Steuerelement wie Heizkessel/BHKW/Solar)
                if (listView_SimWP.Columns.Count == 0)
                {
                    listView_SimWP.View = View.Details;
                    listView_SimWP.FullRowSelect = true;
                    listView_SimWP.GridLines = true;
                    listView_SimWP.MultiSelect = false;
                    listView_SimWP.Font = listView_SimSPK.Font;
                    listView_SimWP.Columns.Add(MyResource.Resource.SIM_SPALTE_MODUL, -2, HorizontalAlignment.Left);
                    listView_SimWP.Columns.Add(MyResource.Resource.SIM_SPALTE_LEISTUNG, -2, HorizontalAlignment.Left);
                    listView_SimWP.Columns.Add(MyResource.Resource.SIM_SPALTE_WAERMEPRODUKTION, -2, HorizontalAlignment.Left);
                    listView_SimWP.Columns.Add(MyResource.Resource.SIM_SPALTE_STROMVERBRAUCH, -2, HorizontalAlignment.Left);
                    listView_SimWP.Columns.Add(MyResource.Resource.SIM_SPALTE_HEIZSTAB, -2, HorizontalAlignment.Left);
                    listView_SimWP.Columns.Add(MyResource.Resource.SIM_SPALTE_BETRIEBSSTUNDEN, -2, HorizontalAlignment.Left);
                }

                // Daten zeilenweise eintragen
                listView_SimWP.Items.Clear();
                for (int i = 0; i < sim.simulation_wp.wp_list.Count(); i++)
                {
                    ListViewItem lvitem = new ListViewItem(sim.simulation_wp.WP_Modul[i]);
                    lvitem.SubItems.Add(sim.simulation_wp.wp_model[i].Grenzleistung.ToString("F2"));
                    lvitem.SubItems.Add((sim.simulation_wp.Modul_WP_Waermeproduktion[i] / 1000.0).ToString("F2"));
                    lvitem.SubItems.Add((sim.simulation_wp.Modul_WP_Strombedarf[i] / 1000.0).ToString("F2"));
                    lvitem.SubItems.Add((sim.simulation_wp.Modul_Heizstab[i] / 1000.0).ToString("F2"));
                    lvitem.SubItems.Add(sim.simulation_wp.Modul_WP_Laufzeit[i].ToString("F2"));
                    listView_SimWP.Items.Add(lvitem);
                }
                listView_SimWP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                listView_SimWP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

                // charts und Textfelder Wärmepumpe
                checkBox_WP_sortiert.Checked = true;
                checkBox_WP_sortiert.Checked = false;

                // chart Temperatur - Leistung  
                PointF[] ps_produktion_raw = new PointF[8760];
                PointF[] ps_bedarf_raw = new PointF[8760];
                PointF[] ps_heizstab_raw = new PointF[8760];

                List<double> werte_produktion = new List<double>();
                List<double> werte_bedarf = new List<double>();

                // nur 1 Leistungswert Wert pro gleicher Temperatur nehmen
                int index = 0;
                for (int n = 0; n < 8760; n++)
                {
                    //if (werte_produktion.Contains(sim.simulation_wp.Temperatur[n])) continue;
                    double d = Math.Round(sim.simulation_wp.Temperatur[n], 1);
                    ps_produktion_raw[index].X = (float)d;
                    ps_produktion_raw[index].Y = sim.simulation_wp.WP_Waermeproduktion_stuendlich[n];
                    ps_bedarf_raw[index].X = ps_produktion_raw[index].X;
                    ps_bedarf_raw[index].Y = sim.simulation_wp.Waermebedarf_stuendlich[n];

                    if (sim.simulation_wp.Heizstab_stuendlich[n] > 0)
                        ps_heizstab_raw[index].Y = sim.simulation_wp.WP_Waermeproduktion_stuendlich[n] + sim.simulation_wp.Heizstab_stuendlich[n];
                    else
                        ps_heizstab_raw[index].Y = 0;

                    ps_heizstab_raw[index].X = ps_produktion_raw[index].X;
                    werte_produktion.Add(sim.simulation_wp.Temperatur[n]);
                    werte_bedarf.Add(sim.simulation_wp.Waermebedarf_stuendlich[n]);
                    index++;
                }

                // Points Array nur mit der tatsächlichen Anzahl(mehrfache Werte gleicher Tempeatur filtern) füllen
                PointF[] ps_produktion = new PointF[index];
                PointF[] ps_bedarf = new PointF[index];
                PointF[] ps_heizstab = new PointF[index];

                for (int n = 0; n < index; n++)
                {
                    ps_produktion.SetValue(ps_produktion_raw[n], n);
                    ps_bedarf.SetValue(ps_bedarf_raw[n], n);
                    ps_heizstab.SetValue(ps_heizstab_raw[n], n);
                }

                // ChartManager instanziieren
                _chartManager[4] = new ChartManager(chart4);
                _chartManager[4].ChartTitle = MyResource.Resource.CHART_TITEL_LEISTUNG_UEBER_AUSSENTEMPERATUR;
                _chartManager[4].XAxisTitle = MyResource.Resource.CHART_ACHSE_TEMPERATUR;
                _chartManager[4].YAxisTitle = MyResource.Resource.SIM_SPALTE_LEISTUNG;
                _chartManager[4].IsXYChart = true;
                _chartManager[4].AreaLine = true; // Area Chart Effekt
                _chartManager[4].MitLegende = true;
                _chartManager[4].YMaxValue = sim.simulation_wp.Waermebedarf_stuendlich.Max();
                _chartManager[4].Init();

                // Daten hinzufügen (gefilterte PointF[] Arrays)
                SerieAnlegen(_chartManager[4], S_WAERMEBEDARF, MyResource.Resource.CHART_LEGENDE_WAERMEBEDARF, Color.FromArgb(120, Color.Red), ps_bedarf, 0);
                SerieAnlegen(_chartManager[4], S_HEIZSTAB, MyResource.Resource.CHART_SEGMENT_HEIZSTAB, Color.FromArgb(120, Color.Yellow), ps_heizstab, 0);
                SerieAnlegen(_chartManager[4], S_WAERMEPRODUKTION, MyResource.Resource.CHART_LEGENDE_WAERMEPRODUKTION, Color.FromArgb(120, Color.Blue), ps_produktion, 0);
            }

            // Speicher-Ergebnisse und Erdreich-Hinweis BEWUSST ausserhalb von
            // "if (sim.bSimulationWP)": wird die Wärmepumpe in einem Folgelauf
            // abgewählt, muss die Rubrik geleert werden statt die Zahlen des
            // Vorlaufs stehen zu lassen. Beide Methoden vertragen einen Lauf ohne
            // Wärmepumpe und blenden dann alles aus.
            PufferspeicherErgebnisAnzeigen();
            ErdreichHinweisAnzeigen();

            // Speichertemperaturen (Paket P2): aus demselben Grund ausserhalb von
            // "if (sim.bSimulationWP)" - die Seite muss auch wieder verschwinden, wenn
            // ein Folgelauf keine Temperaturreihe mehr trägt.
            SpeichertemperaturAnzeigen();

            // ********************************************************************************************/
            // Heizkessel
            // ********************************************************************************************/
            if (sim.bSimulationKessel)
            {
                // Textfelder Spitzenkessel
                //
                // EIGENANTEIL des Kessels an der Bedarfsdeckung: S_Waerme_spk ist seine
                // gesamte NUTZWÄRME, seit Paket 5 also Direktdeckung PLUS Speicherladung -
                // als Produktion richtig, als Deckung nicht. Geladene Wärme deckt noch
                // keinen Bedarf; entladene deckt Bedarf, ohne in der Produktionsstunde zu
                // stehen. Abgezogen wird deshalb die Ladung, hinzu kommt der zugerechnete
                // Anteil an der bedarfsdeckenden Entladung - Bezugsgröße von Restbedarf
                // UND Deckungsgrad, wortgleich mit SimulationRunner:565-583. Der Restbedarf
                // konnte vorher NEGATIV werden. Im Altpfad und ohne Puffer-Senke sind beide
                // Speichergrößen exakt 0, der Ausdruck ist dann bitgleich dem bisherigen -
                // eine Fallunterscheidung wie beim BHKW braucht der Kessel nicht.
                double kesselDirektMWh = sim.simulation_spk.S_Waerme_spk -
                                         sim.simulation_spk.Speicherladung_gesamt / 1000.0;
                if (kesselDirektMWh < 0) kesselDirektMWh = 0;   // Rundungsschutz
                double kesselEigenMWh = kesselDirektMWh +
                                        sim.simulation_spk.Speicherentladung_Anteil / 1000.0;

                double kesselRestMWh = sim.simulation_spk.Waermebedarf_gesamt - kesselEigenMWh;
                if (kesselRestMWh < 0) kesselRestMWh = 0;       // Rundungsschutz

                if (simulation_Waermebedarf.Waermebedarf_Gesamt > 0)
                {
                    double kesselDeckung = kesselEigenMWh * 100.0 / simulation_Waermebedarf.Waermebedarf_Gesamt;
                    if (kesselDeckung > 100) kesselDeckung = 100;
                    if (kesselDeckung < 0) kesselDeckung = 0;
                    textBox_SPKWaermebedarfsdeckung.Text = kesselDeckung.ToString("F2");
                }
                else
                    textBox_SPKWaermebedarfsdeckung.Text = "0";
                textBox_Waermebedarf_Heizkessel.Text = sim.simulation_spk.Waermebedarf_gesamt.ToString("F2");
                textBox_Restwermebedarf_Heizkessel.Text = kesselRestMWh.ToString("F2");
                tb_WaermeprSpk.Text = (sim.simulation_spk.S_Waerme_spk).ToString("F2");
                textBox_Strombedarf_Heizkessel.Text = (sim.simulation_spk.Strombedarf_gesamt / 1000).ToString("F2");
                textBox_Reststrombedarf_Heizkessel.Text = (sim.simulation_spk.Strombedarf_gesamt / 1000 + sim.simulation_spk.Stromverbrauch_Spk).ToString("F2");

                tb_Gasverbrauch.Text = (sim.simulation_spk.Gasverbrauch_SPK).ToString("F2");
                tb_Oelverbrauch.Text = (sim.simulation_spk.Oelverbrauch_SPK).ToString("F2");
                tb_Koks.Text = (sim.simulation_spk.Koks_SPK).ToString("F2");
                tb_Rapsoelverbrauch.Text = (sim.simulation_spk.Rapsoelverbrauch_SPK).ToString("F2");
                tb_Holzverbrauch.Text = (sim.simulation_spk.Holzverbrauch_SPK).ToString("F2");
                tb_Kohle.Text = (sim.simulation_spk.Kohle_SPK).ToString("F2");
                tb_Stromverbrauch.Text = (sim.simulation_spk.Stromverbrauch_Spk).ToString("F2");
                tb_Sonstigverbrauch.Text = (sim.simulation_spk.Sonstigverbrauch_SPK).ToString("F2");
                tb_Pellets.Text = (sim.simulation_spk.Pellets_SPK).ToString("F2");
                tb_Koks.Text = (sim.simulation_spk.Koks_SPK).ToString("F2");
                tb_TierischeFette.Text = (sim.simulation_spk.TierischeFette_SPK).ToString("F2");

                tb_Max_Kesselleistung.Text = (sim.simulation_spk.Maximale_Kesselleistung_Spk).ToString("F2");
                tb_Gasspitze.Text = sim.simulation_spk.Gasspitze_Spk.ToString("F2");

                // ETAPPE D4: Quellwärme der Kaskade. Der Rechenkern führt sie in kWh, die
                // Seite zeigt MWh wie die übrigen Wärmegrößen daneben. Ohne Quellbezug
                // steht dort 0,00 - die Zeile bleibt sichtbar, denn „der Kessel bezieht
                // nichts aus einem Puffer" ist die Antwort auf genau diese Frage.
                if (tb_KesselQuellwaerme != null)
                    tb_KesselQuellwaerme.Text =
                        (sim.simulation_spk.Quellwaerme_gesamt / 1000.0).ToString("F2");

                listView_SimSPK.Items.Clear();
                for (int i = 0; i < sim.simulation_spk.spk_list.Count(); i++)
                {

                    ListViewItem lvitem = new ListViewItem();
                    lvitem.Text = (i + 1).ToString();
                    lvitem.SubItems.Add(sim.simulation_spk.spk_list[i]);
                    lvitem.SubItems.Add((sim.simulation_spk.s_waerme_Gas_Spk[i]).ToString("F2"));
                    lvitem.SubItems.Add((sim.simulation_spk.s_waerme_Oel_Spk[i]).ToString("F2"));
                    lvitem.SubItems.Add((sim.simulation_spk.Kessel_Jahresnutzungsgrad_Spk[i]).ToString("F1"));

                    listView_SimSPK.Items.Add(lvitem);
                }

                listView_SimSPK.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                listView_SimSPK.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }

            // Wärmelast-Jahresganglinie der Kesselseite - außerhalb des if, damit sie
            // nach einem Lauf ohne Kessel wieder verschwindet (siehe KesselErgebnisAnzeigen).
            KesselErgebnisAnzeigen();

            // ********************************************************************************************/
            // Solarthermie
            // ********************************************************************************************/
            if (sim.bSimulationSolarthermie)
            {
                // Textfelder Solarthermie
                //
                // EIGENANTEIL der Solarthermie an der Bedarfsdeckung: Produktion abzüglich
                // Speicherladung (das ist die Direktdeckung) plus der ihr zugerechnete
                // Anteil an der bedarfsdeckenden Speicherentladung. Bezugsgröße von
                // Restbedarf UND Deckungsgrad, wortgleich mit SimulationRunner:648-684.
                // Vorher stand die ganze Produktion im Zähler - damit überschritt die
                // Deckung 100 % und der Restbedarf wurde NEGATIV, sobald das Kollektorfeld
                // zusätzlich einen Puffer lud. Ohne Puffer-Senke sind beide
                // Speichergrößen exakt 0, der Ausdruck ist dann bitgleich dem bisherigen.
                //
                // PAKET E1 — BEFUND V0-O1 MITGEZOGEN: Der NENNER des Deckungsgrades ist
                // jetzt der PROJEKTbedarf, nicht mehr der Stufeneingang der Solarthermie
                // — genau wie bei Wärmepumpe, Kessel und BHKW und genau wie im Runner
                // (V0-7: der Dialog zeigt, was in Tab_Ergebnis steht). Der RESTBEDARF
                // darunter bleibt auf dem Stufeneingang: Er beantwortet „was bleibt nach
                // diesem Erzeuger offen" und ist damit eine Stufengröße.
                double solarDirektKWh = sim.simulation_solarthermie.Waermeproduktion_gesamt -
                                        sim.simulation_solarthermie.Speicherladung_gesamt;
                if (solarDirektKWh < 0) solarDirektKWh = 0;   // Rundungsschutz
                double solarEigenKWh = solarDirektKWh +
                                       sim.simulation_solarthermie.Speicherentladung_Anteil;

                double solarRestMWh =
                    (sim.simulation_solarthermie.Waermebedarf_gesamt - solarEigenKWh) / 1000.0;
                if (solarRestMWh < 0) solarRestMWh = 0;       // Rundungsschutz

                if (simulation_Waermebedarf.Waermebedarf_Gesamt > 0)
                {
                    double solarDeckung = solarEigenKWh / 1000.0 * 100.0
                                          / simulation_Waermebedarf.Waermebedarf_Gesamt;
                    if (solarDeckung > 100) solarDeckung = 100;
                    if (solarDeckung < 0) solarDeckung = 0;
                    textBox_STWaermebedarfsdeckung.Text = solarDeckung.ToString("F2");
                }
                else
                    textBox_STWaermebedarfsdeckung.Text = "";
                textBox_STWaermebedarf.Text = (sim.simulation_solarthermie.Waermebedarf_gesamt / 1000).ToString("F2");
                textBox_STRestwermebedarf.Text = solarRestMWh.ToString("F2");
                tb_WaermeprST.Text = (sim.simulation_solarthermie.Waermeproduktion_gesamt / 1000).ToString("F2");
                textBox_Ueberschuss.Text = (sim.simulation_solarthermie.Ueberschuss_summe / 1000).ToString("F2");

                // Chart Solarthermie Wärmerbedarf und Produktion
                _chartManager[8] = new ChartManager(chart8);
                _chartManager[8].YMaxValue = sim.simulation_solarthermie.Waermebedarf.Max();
                _chartManager[8].YMinValue = 0;
                _chartManager[8].XAxisAsNumber = false;
                _chartManager[8].XAxisTitle = MyResource.Resource.CHART_ACHSE_JAHRESSTUNDEN;
                _chartManager[8].YAxisTitle = MyResource.Resource.CHART_ACHSE_WAERMELAST;
                _chartManager[8].toolTipUnit = "kW";
                _chartManager[8].ChartTitle = MyResource.Resource.CHART_TITEL_WAERMELAST_JAHRESGANGLINIE;
                _chartManager[8].MitLegende = true;
                _chartManager[8].MitChartBorder = true;
                _chartManager[8].AreaLine = false;
                _chartManager[8].Init();
                SerieAnlegen(_chartManager[8], S_WAERMEBEDARF, MyResource.Resource.CHART_LEGENDE_WAERMEBEDARF, Color.Red, Array.ConvertAll<double, float>(sim.simulation_solarthermie.Waermebedarf, x => (float)x));
                SerieAnlegen(_chartManager[8], S_WAERMEPRODUKTION, MyResource.Resource.CHART_LEGENDE_WAERMEPRODUKTION, Color.Blue, Array.ConvertAll<double, float>(sim.simulation_solarthermie.Waermeproduktion, x => (float)x));

                // Auflistung der einzelnen Solarkollektoren (analog listView_SimSPK beim Heizkessel).
                listView_SimSolar.Items.Clear();
                if (sim.simulation_solarthermie.Kollektor_Ergebnisse != null)
                {
                    for (int i = 0; i < sim.simulation_solarthermie.Kollektor_Ergebnisse.Count; i++)
                    {
                        var k = sim.simulation_solarthermie.Kollektor_Ergebnisse[i];
                        ListViewItem lvitem = new ListViewItem((i + 1).ToString());
                        lvitem.SubItems.Add(k.Name);
                        lvitem.SubItems.Add(k.Flaeche.ToString("F2"));
                        lvitem.SubItems.Add(k.Anzahl.ToString());
                        lvitem.SubItems.Add((k.Waermeproduktion / 1000.0).ToString("F2"));
                        lvitem.SubItems.Add((k.Ueberschuss / 1000.0).ToString("F2"));
                        listView_SimSolar.Items.Add(lvitem);
                    }
                }
                listView_SimSolar.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                listView_SimSolar.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }

            // ********************************************************************************************/
            // PV
            // ********************************************************************************************/
            textBox_PVStrom.Text = (sim.simulation_pv.Stromproduktion.Sum() / 1000.0).ToString("F2");
            textBox_PVUeberschuss.Text = (sim.simulation_pv.Ueberschuss.Sum() / 1000.0).ToString("F2");
            textBox_PVStrombedarfsdeckung.Text = (sim.simulation_pv.Stromproduktion.Sum() * 100 / sim.simulation_pv.Strombedarf_stuendlich.Sum()).ToString("F2");
            textBox_PVStrombedarf.Text = (sim.simulation_pv.Strombedarf.Sum() / 4000.0).ToString("F2");
            textBox_PVReststrombedarf.Text = (sim.Rest_Strombedarf_viertelstuendlich.Sum() / 4000.0).ToString("F2");

            _chartManager[9] = new ChartManager(chart_PV);
            _chartManager[9].YMaxValue = sim.simulation_pv.Strombedarf.Max();
            _chartManager[9].YMinValue = 0;
            _chartManager[9].XAxisAsNumber = false;
            _chartManager[9].XAxisTitle = MyResource.Resource.CHART_ACHSE_MONATE;
            _chartManager[9].YAxisTitle = MyResource.Resource.CHART_ACHSE_LEISTUNG;
            _chartManager[9].toolTipUnit = "kW";
            _chartManager[9].ChartTitle = MyResource.Resource.CHART_TITEL_STROMBEDARF_PV_JAHRESGANGLINIE;
            _chartManager[9].MitLegende = true;
            _chartManager[9].MaxXVALUE = 8760 * 4;
            _chartManager[9].MitViertelStunde = true;
            _chartManager[9].Init();
            // NUR DER SPEICHER geht auf die rechte Achse (true = Sekundärachse kWh)
            // AP2b: Der Ladezustand kommt aus dem SpeicherErgebnis der Engine, nicht
            // mehr aus der abgelösten PV-Batterielogik - Serie, Farbe und Achse bleiben.
            SerieAnlegen(_chartManager[9], S_SPEICHERFUELLSTAND, MyResource.Resource.PSP_CHECKBOX_SPEICHERFUELLSTAND, Color.FromArgb(120, 130, 140), sim.Speicherfuellstand_viertelstuendlich);
            SerieAnlegen(_chartManager[9], S_UEBERSCHUSS, MyResource.Resource.CHART_LEGENDE_UEBERSCHUSS, Color.Yellow, sim.simulation_pv.Ueberschuss_viertelstunde);
            SerieAnlegen(_chartManager[9], S_STROMBEDARF, MyResource.Resource.CHART_ACHSE_STROMBEDARF, Color.Red, sim.simulation_pv.Strombedarf);
            SerieAnlegen(_chartManager[9], S_PHOTOVOLTAIK, MyResource.Resource.SIM_PHOTOVOLTAIK, Color.BlueViolet, sim.simulation_pv.Stromproduktion_viertelstunde);
            _chartManager[9]._chart.Series[S_UEBERSCHUSS].Enabled = false;
            _chartManager[9]._chart.Series[S_SPEICHERFUELLSTAND].Enabled = false;
            checkBox_Ueberschuss.Checked = false;
            checkBox_Speicherzustand.Checked = false;
            textBox_MaxPSolar.Text = sim.simulation_pv.MaxPSolar.ToString("F2");

            // Auflistung der einzelnen PV-Module (ListView, analog Heizkessel/Solarthermie).
            listView_SimPV.Items.Clear();
            if (sim.simulation_pv.Modul_Ergebnisse != null)
            {
                for (int i = 0; i < sim.simulation_pv.Modul_Ergebnisse.Count; i++)
                {
                    var p = sim.simulation_pv.Modul_Ergebnisse[i];
                    ListViewItem lvitem = new ListViewItem((i + 1).ToString());
                    lvitem.SubItems.Add(p.Name);
                    lvitem.SubItems.Add(p.Flaeche.ToString("F2"));
                    lvitem.SubItems.Add(p.Anzahl.ToString());
                    lvitem.SubItems.Add((p.Stromproduktion / 1000.0).ToString("F2"));
                    listView_SimPV.Items.Add(lvitem);
                }
            }
            listView_SimPV.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_SimPV.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);



            // ********************************************************************************************/
            // BHKW
            // ********************************************************************************************/
            //
            // BHKW-ANZEIGE-NACHZUG: Diagramm und Kennzahlen kommen jetzt aus den Größen des
            // Speicherstufen-Wegs. Hier stand bis dahin
            //
            //   - eine unbedingte Sortierung (OrderByDescending) unter dem Titel
            //     „Jahresganglinie" - der gemeldete „harte Abfall auf 0",
            //   - die Restwärme als Vektordifferenz SubVectors(Bedarf, Produktion),
            //   - der Deckungsgrad als Produktion / Projektbedarf.
            //
            // Diagramm und Umschalter liegen jetzt in BhkwErgebnisAnzeigen (Muster
            // KesselErgebnisAnzeigen); _chartManager[10] ist damit entfallen. Die
            // Begründungen stehen im Blockkommentar dort.
            BhkwErgebnisAnzeigen();

            // ETAPPE E2: Werte unverändert, Beschriftungen richtiggestellt (siehe
            // InitBhkwVbhZeile) — beide Felder führen THERMISCHE Vollbenutzungsstunden,
            // keine Betriebsstunden. Darunter neu die elektrischen Vbh: die Größe, an der
            // der KWK-Zuschlag hängt und die 8.760 h nicht überschreiten kann.
            textBox_Betriebsstunden.Text = sim.simulation_bhkw.Betriebsstunden.ToString("F0");
            textBox_Betriebsstunden_Durchschnitt.Text = sim.simulation_bhkw.dLaufzeiten.ToString("F0");
            if (tb_BhkwVbhElektrisch != null)
                tb_BhkwVbhElektrisch.Text = sim.simulation_bhkw.VbhElektrischGesamt > 0
                    ? sim.simulation_bhkw.VbhElektrischGesamt.ToString("F0")
                    : "—";   // keine elektrische Nennleistung gepflegt — keine Zahl erfinden

            AktualisiereBrennstoffAnzeige(sim.simulation_bhkw);

            // STUFENEINGANG: dieselbe Größe wie bisher, aber aus der double-Jahressumme des
            // Moduls statt aus 8760 float-Additionen - wortgleich mit dem Ausdruck, aus dem
            // Tab_ErgebnisBHKW.Waermebedarf entsteht (SimulationRunner:381-383).
            //
            // PAKET L: Der Altpfad-Zweig (Ganglinien-Summe waermebedarf.Sum(), weil das
            // Modul dort keine Jahressumme führte) ist entfallen - mit Paket A1 gibt es
            // nur EINEN Rechenweg, und der füllt Waermebedarf_gesamt.
            double bhkwWaermebedarfMWh = sim.simulation_bhkw.Waermebedarf_gesamt / 1000.0;

            textBox_Waermebedarf_BHKW.Text = bhkwWaermebedarfMWh.ToString("F2");
            textBox_Strombedarf_BHKW.Text = (sim.simulation_bhkw.strombedarf.Sum() / 1000).ToString("F2");
            textBox_Waermeproduktion_gesamt_BHKW.Text = sim.simulation_bhkw.Waermeproduktion_BHKW_MWh.ToString("F2");
            textBox_Stromproduktion_gesamt_BHKW.Text = sim.simulation_bhkw.Stromproduktion_BHKW_MWh.ToString("F2");

            // EIGENANTEIL des BHKW an der Bedarfsdeckung: unmittelbar abgegebene Wärme plus
            // der ihm zugerechnete Anteil an der bedarfsdeckenden Speicherentladung
            // (Interimsregel „Vermischung im Speicher", Kaskadenschleife). Er ist die
            // Bezugsgröße von Restbedarf UND Deckungsgrad - beides zwei Seiten derselben
            // Rechnung, genau wie in SimulationRunner:434-449.
            double bhkwDirektMWh = sim.simulation_bhkw.Direktdeckung_gesamt / 1000.0;
            double bhkwEntladungMWh = sim.simulation_bhkw.Speicherentladung_Anteil / 1000.0;
            double bhkwEigenMWh = bhkwDirektMWh + bhkwEntladungMWh;

            // RESTWÄRME: Vorher die Vektordifferenz „Bedarf − Produktion" - der
            // Bilanzfehler aus Konzept 6.5. Sobald das BHKW einen Speicher lädt, gilt sie
            // nicht mehr: Geladene Wärme deckt noch keinen Bedarf, entladene deckt Bedarf
            // ohne in der Produktionsstunde zu stehen.
            //
            // PAKET L: Der Altpfad-Zweig (Vektordifferenz über SubVectors, weil dort
            // Direktdeckung und Entladungsanteil exakt 0 waren) ist mit dem Feld
            // sim.KaskadeZweikanalig entfallen - seit Paket A1 gibt es nur EINEN
            // Rechenweg, und der führt beide Größen.
            double bhkwRestwaermeMWh = bhkwWaermebedarfMWh - bhkwEigenMWh;
            if (bhkwRestwaermeMWh < 0) bhkwRestwaermeMWh = 0;   // Rundungsschutz
            textBox_Restwaermebedarf_BHKW.Text = bhkwRestwaermeMWh.ToString("F2");

            textBox_Reststrombedarf_BHKW.Text = ((sim.simulation_bhkw.strombedarf.Sum() / 1000) - sim.simulation_bhkw.Stromproduktion_BHKW_MWh).ToString("F2");
            textBox_Waermeueberschuss_BHKW.Text = (sim.simulation_bhkw.Waermeueberschuss / 1000).ToString("F2");

            // DER SPEICHERBEITRAG, bisher nirgends sichtbar (Live-Test-Meldung 1):
            // wohin die Produktion geht und woher die Deckung kommt.
            if (tb_BhkwSpeicherladung != null)
                tb_BhkwSpeicherladung.Text =
                    (sim.simulation_bhkw.Speicherladung_gesamt / 1000.0).ToString("F2");
            if (tb_BhkwSpeicherdeckung != null)
                tb_BhkwSpeicherdeckung.Text = bhkwEntladungMWh.ToString("F2");

            // DECKUNGSGRAD: Vorher die PRODUKTION im Zähler - damit wies die Seite Wärme
            // als Deckung aus, die noch im Speicher lag. Jetzt der Eigenanteil, auf 0..100
            // geklemmt wie im Runner. Bezugsgröße bleibt der PROJEKTwärmebedarf.
            if (simulation_Waermebedarf.Waermebedarf_Gesamt > 0)
            {
                double bhkwDeckung =
                    bhkwEigenMWh * 100.0 / simulation_Waermebedarf.Waermebedarf_Gesamt;
                if (bhkwDeckung > 100) bhkwDeckung = 100;
                if (bhkwDeckung < 0) bhkwDeckung = 0;
                textBox_Waermedeckung.Text = bhkwDeckung.ToString("F2");
            }
            else
                textBox_Waermedeckung.Text = "0";
            if (simulation_Strombedarf.Strombedarf_gesamt > 0)
                textBox_Stromdeckung.Text = (sim.simulation_bhkw.Stromproduktion_BHKW_MWh * 100 / simulation_Strombedarf.Strombedarf_gesamt).ToString("F2");
            else
                textBox_Stromdeckung.Text = "0";

            // Auflistung der BHKW-Module (ListView, analog Heizkessel/Solarthermie).
            //
            // BHKW-ANZEIGE-NACHZUG, geprüft und UNVERÄNDERT gelassen: s_waerme_MWh und
            // s_strom_MWh sind genau die Felder, aus denen der Runner die Zeilen von
            // Tab_ErgebnisBHKWModul bildet (SimulationRunner:498-511). Die Tabelle deckt
            // sich damit ohne Eingriff mit dem Kern; die Modulwärme ist - wie die
            // Gesamtproduktion daneben - die BRUTTOerzeugung inklusive Speicherladung.
            dataGridView_BHKW.Items.Clear();
            if (sim != null && sim.simulation_bhkw != null)
            {
                for (int i = 0; i < sim.simulation_bhkw.bhkw_list.Count; i++)
                {
                    string name = sim.simulation_bhkw.bhkw_list_Namen[i] ?? MyResource.Resource.SIM_BHKW_MODUL_STANDARD;
                    ListViewItem lvitem = new ListViewItem((i + 1).ToString());
                    lvitem.SubItems.Add(name);
                    lvitem.SubItems.Add(sim.simulation_bhkw.s_waerme_MWh[i].ToString("F2"));
                    lvitem.SubItems.Add(sim.simulation_bhkw.s_strom_MWh[i].ToString("F2"));
                    dataGridView_BHKW.Items.Add(lvitem);
                }
            }
            dataGridView_BHKW.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            dataGridView_BHKW.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            // ********************************************************************************************/
            // Ergebnisübersicht
            // ********************************************************************************************/

            // Heizkessel
            waerme_spk = 0;
            for (int i = 0; i < sim.simulation_spk.spk_list.Count(); i++)
            {
                waerme_spk += sim.simulation_spk.s_waerme_Gas_Spk[i] + sim.simulation_spk.s_waerme_Oel_Spk[i];
            }

            // Wärmepumpe
            waerme_wp = sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000;
            waerme_heizstab = sim.simulation_wp.Heizstab_gesamt / 1000;

            // Solarthermie
            waerme_solar = sim.simulation_solarthermie.Waermeproduktion_gesamt / 1000;
            gesamt_waerme = waerme_spk + waerme_wp + waerme_heizstab + waerme_solar;
            restwaermebedarf = sim.simulation_Waermebedarf.Waermebedarf_Gesamt - gesamt_waerme;

            // ********************************************************************************************/
            // Stromspeicher (AP3b) - außerhalb jeder Bedingung, damit die Seite nach
            // einem Lauf OHNE Speicher wieder leer ist (Muster KesselErgebnisAnzeigen).
            // ********************************************************************************************/
            SpeicherErgebnisAnzeigen();

            _navManager.RefreshActivePage();
        }

        private void btn_Beenden_Click(object sender, EventArgs e)
        {
            Close();
        }

        public void ConfigureXAxisWithMonths(Chart chartControl)
        {
            // Define your custom labels in an array
            string[] monthArray = { "1", "2", "3", "4", "5", "6", "7", "8", "8", "10", "11", "12" };

            chartControl.ChartAreas[0].AxisX.CustomLabels.Clear();
            chartControl.Annotations.Clear();
            chartControl.Series[0].Points.Clear();

            chartControl.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
            chartControl.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);

            chartControl.ChartAreas[0].AxisX.Minimum = 0;
            chartControl.ChartAreas[0].AxisX.Maximum = monthArray.Length;
            chartControl.ChartAreas[0].AxisX.Interval = 1;

            for (int i = 0; i < monthArray.Length; i++)
            {
                CustomLabel lblMonth = new CustomLabel();
                lblMonth.FromPosition = i;
                lblMonth.ToPosition = i + 0.8;
                lblMonth.Text = monthArray[i];
                chartControl.ChartAreas[0].AxisX.CustomLabels.Add(lblMonth);
            }

            chartControl.ChartAreas[0].AxisX.IntervalOffsetType = DateTimeIntervalType.Months;
            chartControl.ChartAreas[0].AxisX.Title = "Monat";
            chartControl.ChartAreas[0].AxisX.ScaleView.Size = 12;

            return;
        }

        public void ConfigureXAxisWithHours(Chart chartControl, float[] Dauerlinie_sortiert, int Interval = 1)
        {
            // custom labels in array
            string[] hourArray = { "2000", "4000", "6000", "8000" };

            chartControl.ChartAreas[0].AxisX.CustomLabels.Clear();
            chartControl.Annotations.Clear();
            chartControl.Series[0].Points.Clear();

            chartControl.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
            chartControl.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);

            chartControl.ChartAreas[0].AxisX.Minimum = 0;
            chartControl.ChartAreas[0].AxisX.Maximum = hourArray.Length;
            chartControl.ChartAreas[0].AxisX.Interval = 1;

            // Add custom labels for each data point position
            for (int i = 0; i < hourArray.Length; i++)
            {
                CustomLabel lblMonth = new CustomLabel();
                lblMonth.FromPosition = i;
                lblMonth.ToPosition = i + 0.8;
                lblMonth.Text = hourArray[i];
                chartControl.ChartAreas[0].AxisX.CustomLabels.Add(lblMonth);
            }

            for (int j = 0; j < 8760 * Interval; j++)
            {
                double d = (double)j * 4 / (8760 * Interval);
                chartControl.Series[0].Points.AddXY(d, Dauerlinie_sortiert[j]);
            }
            chartControl.ChartAreas[0].AxisX.IntervalOffsetType = DateTimeIntervalType.Hours;
            chartControl.ChartAreas[0].AxisX.Title = MyResource.Resource.CHART_ACHSE_JAHRESSTUNDEN;

            return;
        }

        public void ConfigureXAxisWithHours2(Chart chartControl, int Interval = 1)
        {
            // custom labels in array
            string[] hourArray = { "2000", "4000", "6000", "8000" };

            chartControl.ChartAreas[0].AxisX.CustomLabels.Clear();
            chartControl.Annotations.Clear();

            chartControl.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
            chartControl.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);

            chartControl.ChartAreas[0].AxisX.Minimum = 0;
            chartControl.ChartAreas[0].AxisX.Maximum = hourArray.Length;
            chartControl.ChartAreas[0].AxisX.Interval = 1;

            // Add custom labels for each data point position
            for (int i = 0; i < hourArray.Length; i++)
            {
                CustomLabel lblMonth = new CustomLabel();
                lblMonth.FromPosition = i;
                lblMonth.ToPosition = i + 0.8;
                lblMonth.Text = hourArray[i];
                chartControl.ChartAreas[0].AxisX.CustomLabels.Add(lblMonth);
            }

            chartControl.ChartAreas[0].AxisX.IntervalOffsetType = DateTimeIntervalType.Hours;
            chartControl.ChartAreas[0].AxisX.Title = MyResource.Resource.CHART_ACHSE_JAHRESSTUNDEN;

            return;
        }

        private void checkBox_WP_sortiert_CheckedChanged(object sender, EventArgs e)
        {
            if (sim == null || !sim.bSimulationWP) return;

            // 1. Hilfsarray für Heizstab vorbereiten
            float[] tempHeizstab = new float[8760];
            for (int i = 0; i < 8760; i++)
            {
                tempHeizstab[i] = ctrl.model.m_WP_Heizstab ?
                    (sim.simulation_wp.WP_Waermeproduktion_stuendlich[i] + sim.simulation_wp.Heizstab_stuendlich[i]) : 0;
            }

            // Manager referenzieren für bessere Lesbarkeit
            var manager = _chartManager[3];

            // Wärmebedarf in Heizwärme und Warmwasser aufteilen
            float[] bedarfWW = WarmwasserAnteil(sim.simulation_wp.Waermebedarf_stuendlich);
            float[] bedarfHeizung = new float[8760];
            for (int i = 0; i < 8760; i++)
                bedarfHeizung[i] = sim.simulation_wp.Waermebedarf_stuendlich[i] - bedarfWW[i];

            if (checkBox_WP_sortiert.Checked)
            {
                // --- SORTIERTER MODUS (Numerische X-Achse) ---
                // Dauerlinie je Serie - die Sortierregel steht in GanglinienDarstellung,
                // damit Wärmepumpen-, Kessel- und Navigatorseite dieselbe verwenden.
                float[] sortedWBArray = GanglinienDarstellung.Dauerlinie(sim.simulation_wp.WP_Waermeproduktion_stuendlich);
                float[] sortedHeizung = GanglinienDarstellung.Dauerlinie(bedarfHeizung);
                float[] sortedWW = GanglinienDarstellung.Dauerlinie(bedarfWW);
                float[] sortedHeizstab = GanglinienDarstellung.Dauerlinie(tempHeizstab);

                manager.XAxisAsNumber = true; // Wichtig für Init()
                manager.HardReset();
                manager.Init();

                SerieAnlegen(manager, S_HEIZWAERMEBEDARF, MyResource.Resource.CHART_LEGENDE_HEIZWAERMEBEDARF, Color.Red, sortedHeizung);
                SerieAnlegen(manager, S_WARMWASSERBEDARF, MyResource.Resource.CHART_LEGENDE_WARMWASSERBEDARF, Color.DeepSkyBlue, sortedWW);
                SerieAnlegen(manager, S_HEIZSTAB, MyResource.Resource.CHART_SEGMENT_HEIZSTAB, Color.Yellow, sortedHeizstab);
                SerieAnlegen(manager, S_WAERMEPRODUKTION, MyResource.Resource.CHART_LEGENDE_WAERMEPRODUKTION, Color.Blue, sortedWBArray);
            }
            else
            {
                // --- CHRONOLOGISCHER MODUS (Datum X-Achse), GESTAPELT ---
                //
                // Zwei Größen, die sich jeweils zu einer Summe addieren, und deshalb ZWEI
                // getrennte Stapel (StackedGroupName):
                //   Bedarf     = Heizwärme + Warmwasser
                //   Produktion = Wärmepumpe + Heizstab
                //
                // Der Heizstab geht dabei mit seinem EIGENEN Anteil in den Stapel, nicht
                // mit der kumulierten Kurve "WP-Produktion + Heizstab" (tempHeizstab), die
                // der sortierte Zweig zeichnet: gestapelt wäre die WP-Produktion sonst
                // doppelt enthalten. Die Oberkante des Stapels ist derselbe Wert wie die
                // bisherige kumulierte Linie — nur richtig zusammengesetzt.
                float[] heizstabAnteil = new float[8760];
                for (int i = 0; i < 8760; i++)
                    heizstabAnteil[i] = ctrl.model.m_WP_Heizstab ? sim.simulation_wp.Heizstab_stuendlich[i] : 0;

                manager.XAxisAsNumber = false;
                manager.HardReset();
                manager.Init(); // Hier wird FormatXAxisWithDate() aufgerufen

                // Die PRODUKTION gestapelt als SÄULEN, nicht als Fläche: Läuft die
                // Wärmepumpe im Alternativbetrieb, ist ihre Produktion in den
                // Kesselstunden 0, und eine Fläche zöge zwischen den Stützstellen eine
                // Gerade — sie überdeckte den Bedarf mit Dreiecken über Stunden, in denen
                // die Wärmepumpe nichts produziert hat. Ausführliche Begründung in
                // GanglinienDarstellung.Stapeltyp.
                //
                // Der BEDARF bleibt eine Flächengruppe. Nicht aus Nachlässigkeit: Zwei
                // Säulengruppen stellt MS-Chart NEBENEINANDER und halbiert damit die
                // ohnehin nur 0,07 Bildpunkte breite Säule — die Produktion verschwände in
                // der Rasterung. Heizwärme und Warmwasser addieren sich außerdem stetig
                // und gehen gemeinsam auf null; die Interpolationsfalle, gegen die die
                // Säulen helfen, gibt es zwischen ihnen nicht.
                SerieAnlegen(manager, S_HEIZWAERMEBEDARF, MyResource.Resource.CHART_LEGENDE_HEIZWAERMEBEDARF,
                             Color.Red, bedarfHeizung, SeriesChartType.StackedArea, "Bedarf");
                SerieAnlegen(manager, S_WARMWASSERBEDARF, MyResource.Resource.CHART_LEGENDE_WARMWASSERBEDARF,
                             Color.DeepSkyBlue, bedarfWW, SeriesChartType.StackedArea, "Bedarf");
                SerieAnlegen(manager, S_WAERMEPRODUKTION, MyResource.Resource.CHART_LEGENDE_WAERMEPRODUKTION,
                             Color.Blue, sim.simulation_wp.WP_Waermeproduktion_stuendlich,
                             GanglinienDarstellung.Stapeltyp(false), "Produktion");
                SerieAnlegen(manager, S_HEIZSTAB, MyResource.Resource.CHART_SEGMENT_HEIZSTAB,
                             Color.Yellow, heizstabAnteil, GanglinienDarstellung.Stapeltyp(false), "Produktion");
            }

            // Skalierung erzwingen
            //manager.UpdateYScaleBasedOnVisibleSeries();
            manager._chart.Invalidate();
        }
        //List view header formatters
        public static void colorListViewHeader(ref ListView list, Color backColor, Color foreColor)
        {
            list.OwnerDraw = true;
            list.DrawColumnHeader +=
                new DrawListViewColumnHeaderEventHandler
                (
                    (sender, e) => headerDraw(sender, e, backColor, foreColor)
                );
            list.DrawItem += new DrawListViewItemEventHandler(bodyDraw);
        }

        private static void headerDraw(object sender, DrawListViewColumnHeaderEventArgs e, Color backColor, Color foreColor)
        {
            using (SolidBrush backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            using (SolidBrush foreBrush = new SolidBrush(foreColor))
            {
                e.Graphics.DrawString(e.Header.Text, e.Font, foreBrush, e.Bounds);
            }
        }

        private static void bodyDraw(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void chart2_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.Location;
            if (pos == prevPosition || checkBox_StromSortiert.Checked) return;

            prevPosition = pos;

            var results = chart2.HitTest(pos.X, pos.Y, false, ChartElementType.DataPoint);

            foreach (var result in results)
            {
                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    var yVal = result.ChartArea.AxisY.PixelPositionToValue(pos.Y);
                    var xVal = result.ChartArea.AxisX.PixelPositionToValue(pos.X);
                    DateTime startDatum = new DateTime(DateTime.Now.Year, 1, 1); // Start: 1. Januar 

                    // Addiere diesen Wert zum Startdatum.
                    int d = (int)(xVal * 365 * 24 * 4 / 12); // mit (int) erhält man nur vielfache von 1/4 Stunden, 15 Minuten Takt

                    // auf Minuten zurückrechnen
                    d = d * 15;
                    DateTime neuesDatum = startDatum.AddMinutes(d);
                    tooltip.Show(neuesDatum.ToString("dd/MM H:mm [" + (int)yVal).ToString() + "%]", chart2, pos.X, pos.Y - 15);
                }
                else
                {
                    tooltip.Hide(chart2);
                }
            }
        }

        private void chart1_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.Location;
            if (pos == prevPosition || checkBox_Sortiert.Checked) return;

            prevPosition = pos;

            var results = chart1.HitTest(pos.X, pos.Y, false, ChartElementType.DataPoint);

            foreach (var result in results)
            {
                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    var yVal = result.ChartArea.AxisY.PixelPositionToValue(pos.Y);
                    var xVal = result.ChartArea.AxisX.PixelPositionToValue(pos.X);
                    DateTime startDatum = new DateTime(DateTime.Now.Year, 1, 1); // Start: 1. Januar 

                    // Addiere diesen Wert zum Startdatum.
                    int d = (int)(xVal * 365 * 24 * 4 / 12); // mit (int) erhält man nur vielfache von 1/4 Stunden, 15 Minuten Takt

                    // auf Minuten zurückrechnen
                    d = d * 15;
                    DateTime neuesDatum = startDatum.AddMinutes(d);

                    tooltip.Show(neuesDatum.ToString("dd/MM H:mm [" + (int)yVal).ToString() + "%]", chart1, pos.X, pos.Y - 15);
                }
                else
                {
                    tooltip.Hide(chart1);
                }
            }
        }

        private void InitTextBoxen(TabPage page)
        {
            page.Controls.OfType<TextBox>().ToList().ForEach(tb => tb.Text = "");
        }

        private void listView_SimWP_MouseDown(object sender, MouseEventArgs e)
        {
            // Prüfen, ob es ein Doppelklick (2 Klicks) mit der linken Maustaste war
            if (e.Clicks == 2 && e.Button == MouseButtons.Left)
            {
                Form_WPAuswahl frm = new Form_WPAuswahl();
                WErzeugerCtrl werzctrl = new WErzeugerCtrl();
                WPCtrl wpctrl = new WPCtrl();
                int id_type;

                frm.list_werzmodel.Clear();
                werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.WP_TYP);
                id_type = WizardItemClass.WP_TYP;

                WErzeugerModel item = new WErzeugerModel();
                for (int i = 0; i < werzctrl.rows; i++)
                {
                    frm.list_werzmodel.Add(werzctrl.items[i]);
                }

                frm.SetControls(Program.startfrm.m_szProjektname);
                DialogResult result = frm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    WizardCtrl wizctrl = new WizardCtrl();
                    wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
                    wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, frm.list_werzmodel);
                }
            }
        }



        private void checkBox_Ueberschuss_CheckedChanged(object sender, EventArgs e)
        {
            // 1. Serie im Chart suchen und umschalten
            if (chart_PV.Series.IndexOf(S_UEBERSCHUSS) != -1)
            {
                chart_PV.Series[S_UEBERSCHUSS].Enabled = checkBox_Ueberschuss.Checked;
            }

            // 2. Skalierung über den Manager korrigieren
            // _chartManager[9].UpdateYScaleBasedOnVisibleSeries();
        }

        private void checkBox_Speicherzustand_CheckedChanged(object sender, EventArgs e)
        {
            double neueMax = 0;

            _chartManager[9]._chart.Series[S_SPEICHERFUELLSTAND].Enabled = checkBox_Speicherzustand.Checked;

            if (checkBox_Speicherzustand.Checked)
            {
                // AP2b: Quelle ist die viertelstündliche SoC-Reihe der SpeicherEngine -
                // die Spreizung stündlicher Werte entfällt, das Maximum bleibt dasselbe.
                neueMax = sim.Speicherfuellstand_viertelstuendlich.Max() * 1.1;
                if (neueMax < 10) neueMax = 10; // Minimum setzen, damit die Achse nicht zu klein wird
            }
            else
                neueMax = sim.simulation_pv.Strombedarf.Max() * 1.1;

            // Achsen-Maximum darf nie 0 oder negativ sein, sonst wirft RecalculateAxesScale
            // "Axis Object - Auto interval does not have proper value" (z. B. wenn noch keine
            // Bedarfsdaten vorliegen bzw. der Handler vor der Simulation feuert).
            if (neueMax < 10 || double.IsNaN(neueMax)) neueMax = 10;

            // Nur die Achse updaten ohne die Daten zu löschen:
            var ca = _chartManager[9]._chart.ChartAreas[0];

            ca.AxisY.Maximum = neueMax; // Den oben berechneten Wert direkt setzen
            ca.AxisY.Interval = 0;      // Auf Auto stellen

            // 2. Prüfen, ob die Serie existiert
            if (_chartManager[9]._chart.Series.IndexOf(S_SPEICHERFUELLSTAND) != -1)
            {
                var s = _chartManager[9]._chart.Series[S_SPEICHERFUELLSTAND];
                bool anzeigen = checkBox_Speicherzustand.Checked;

                s.Enabled = anzeigen;

                if (anzeigen)
                {
                    // --- SPEZIALFALL: Y2-ACHSE AKTIVIEREN ---
                    s.YAxisType = AxisType.Secondary; // Serie nach rechts binden
                    ca.AxisY2.Enabled = AxisEnabled.True;

                    // Optik der rechten Achse
                    ca.AxisY2.Title = MyResource.Resource.CHART_ACHSE_SPEICHER_KWH;
                    ca.AxisY2.TitleForeColor = Color.Black;
                    ca.AxisY2.LabelStyle.ForeColor = Color.Black;
                    ca.AxisY2.MajorGrid.Enabled = false; // Gitter nur links lassen

                    // Skalierung berechnen (falls nicht automatisch gewünscht)
                    if (s.Points.Count > 0)
                    {
                        double maxVal = s.Points.Max(p => p.YValues[0]);
                        ca.AxisY2.Maximum = maxVal > 0 ? maxVal * 1.1 : 10;
                    }

                    // Den inneren Bereich schrumpfen, damit rechts Platz für die 2. Achse ist
                    ca.InnerPlotPosition.Auto = false;
                    ca.InnerPlotPosition.X = 10;      // Start links
                    ca.InnerPlotPosition.Width = 75;  // Vorher ca. 85, jetzt weniger für Y2-Platz
                    ca.InnerPlotPosition.Y = 8;
                    ca.InnerPlotPosition.Height = 75;

                    // Sicherstellen, dass die Achse nicht abgeschnitten wird
                    ca.AxisY2.LabelStyle.Enabled = true;

                }
                else
                {
                    // Y2-Achse wieder verstecken, wenn Speicher aus
                    ca.AxisY2.Enabled = AxisEnabled.False;
                }
            }

            ca.RecalculateAxesScale();
            _chartManager[9]._chart.Invalidate();
        }

        private void radioButton_Stromgefuehrt_CheckedChanged(object sender, EventArgs e)
        {
            // 0 = Wärmegeführt, 1 = Stromgeführt, 2 = Ohne Einspeisung
            // Sicherstellen, dass der "Sender" wirklich ein RadioButton ist
            if (sender is RadioButton geklickterButton)
            {
                // Wichtig: Das Event feuert beim alten Button (wird false) UND beim neuen Button (wird true).
                // Wir wollen nur reagieren, wenn ein Button AKTIVIERT wurde.
                if (geklickterButton.Checked)
                {
                    // Den Wert aus dem 'Tag' auslesen und in ein Int umwandeln
                    if (geklickterButton.Tag != null)
                    {
                        bhkwSimulationsArt = Convert.ToInt32(geklickterButton.Tag);
                        SpeichereKonfigurationsAenderung(model => model.Betriebsart = Convert.ToInt32(geklickterButton.Tag));

                        // Zum Testen im Ausgabefenster:
                        System.Diagnostics.Debug.WriteLine($"Simulationsart geändert auf: {bhkwSimulationsArt}");
                    }
                }
            }
        }

        private void listViewQuellen_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewQuellen.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = listViewQuellen.SelectedItems[0];
                mainTablistIndex = listViewQuellen.SelectedIndices[0];

                // 1. Verhindern, dass deaktivierte Geräte geöffnet werden
                if (selectedItem.Tag.ToString() == "DEAKTIVIERT")
                {
                    listViewQuellen.SelectedIndices.Clear();
                    return;
                }

                string targetTabName = selectedItem.Tag.ToString();

                // Prüfen, ob die TabPage im Dictionary existiert
                if (dictAllTabPages.ContainsKey(targetTabName))
                {
                    TabPage zielPage = dictAllTabPages[targetTabName];

                    // === HIER DIE ENTSCHEIDENDE WEICHE ===
                    // Wenn die neue Zielseite absolut identisch mit der bereits geladenen Seite ist
                    // und sich bereits Controls im Panel befinden, brechen wir sofort ab!
                    if (aktuellAusgeliehenePage == zielPage && splitContainer_Parameter.Panel2.Controls.Count > 0)
                    {
                        return; // Nichts tun, die Controls sind schon da und bleiben unberührt!
                    }

                    // 2. KORREKTUR: Nur zurücklegen, wenn vorher eine ANDERE Seite ausgeliehen war!
                    if (aktuellAusgeliehenePage != null && aktuellAusgeliehenePage != zielPage)
                    {
                        // Solange noch Steuerelemente im rechten Panel liegen...
                        while (splitContainer_Parameter.Panel2.Controls.Count > 0)
                        {
                            Control c = splitContainer_Parameter.Panel2.Controls[0];
                            splitContainer_Parameter.Panel2.Controls.Remove(c); // Aus Panel2 entfernen
                            aktuellAusgeliehenePage.Controls.Add(c);            // Zurück zur alten TabPage
                        }
                    }

                    // 3. Rechtes Panel komplett leeren (Passiert jetzt nur noch bei echtem Seitenwechsel)
                    splitContainer_Parameter.Panel2.Controls.Clear();

                    // Die neue Zielseite als aktuell ausgeliehen merken
                    aktuellAusgeliehenePage = zielPage;

                    // 4. Alle Controls der neuen Ziel-TabPage in eine temporäre Liste kopieren
                    List<Control> controlsToMove = new List<Control>();
                    foreach (Control c in zielPage.Controls)
                    {
                        controlsToMove.Add(c);
                    }

                    // 5. Controls physisch in das rechte Panel (Panel2) einsetzen
                    foreach (Control c in controlsToMove)
                    {
                        zielPage.Controls.Remove(c);
                        splitContainer_Parameter.Panel2.Controls.Add(c);
                    }

                    // 6. Dem Windows-Form sagen, dass es das rechte Panel frisch zeichnen soll
                    splitContainer_Parameter.Panel2.Refresh();
                }
            }
        }

        private void splitContainer_Parameter_SplitterMoved(object sender, SplitterEventArgs e)
        {
            // Sobald der Balken verschoben wird, passen wir die Spaltenbreite der ListView
            // exakt an die neue Breite des linken Panels an.
            if (listViewQuellen.Columns.Count > 0)
            {
                // Spalte fuellt die volle Client-Breite -> Zeilen-Selektion ueber die ganze Breite.
                listViewQuellen.Columns[0].Width = listViewQuellen.ClientSize.Width;
            }
        }

        private void VereinheitlichePageSchriftarten(Control parentControl)
        {
            string zielFamilie = "Segoe UI";
            float zielGroesse = 10f;

            foreach (Control ctrl in parentControl.Controls)
            {
                // Wenn es ein DataGridView ist, passen wir es speziell an
                if (ctrl is DataGridView dgv)
                {
                    // 1. Schriftgröße für die normalen Tabellenzellen (Größe 10)
                    dgv.DefaultCellStyle.Font = new Font(zielFamilie, zielGroesse, FontStyle.Regular);

                    // 2. Schriftgröße für den Spaltenkopf / Header (Größe 10)
                    // Hier lesen wir den aktuellen Stil aus, falls der Header z.B. Fett markiert ist
                    FontStyle headerStil = dgv.ColumnHeadersDefaultCellStyle.Font?.Style ?? FontStyle.Regular;
                    dgv.ColumnHeadersDefaultCellStyle.Font = new Font(zielFamilie, zielGroesse, headerStil);

                    // 3. Optional: Auch für die Zeilenköpfe ganz links (falls eingeblendet)
                    dgv.RowHeadersDefaultCellStyle.Font = new Font(zielFamilie, zielGroesse, FontStyle.Regular);
                }
                // Alle anderen Standard-Controls (außer Charts und ListViews)
                else if (!(ctrl is System.Windows.Forms.DataVisualization.Charting.Chart) && !(ctrl is ListView))
                {
                    if (ctrl is RichTextBox)
                    {
                        FontStyle aktuellerStil = ctrl.Font?.Style ?? FontStyle.Regular;
                        ctrl.Font = new Font(zielFamilie, 8f, aktuellerStil);
                    }
                    else
                    {
                        FontStyle aktuellerStil = ctrl.Font?.Style ?? FontStyle.Regular;
                        ctrl.Font = new Font(zielFamilie, zielGroesse, aktuellerStil);
                    }
                }

                // Rekursion für Unterelemente
                if (ctrl.Controls.Count > 0)
                {
                    VereinheitlichePageSchriftarten(ctrl);
                }
            }
        }

        private void checkBox_Heizstab_CheckedChanged(object sender, EventArgs e)
        {
            SpeichereKonfigurationsAenderung(model => model.m_WP_Heizstab = checkBox_Heizstab.Checked);
        }

        private void SpeichereKonfigurationsAenderung(Action<KonfigurationModel> anpassungsAktion)
        {
            try
            {
                // Controller instanziieren und aktuellen DB-Stand laden
                KonfigurationCtrl ctrl = new KonfigurationCtrl();
                ctrl.ReadSingle("select * from Tab_Einstellungen where ID_Projekt=" + m_ID_Projekt);

                if (ctrl.rows > 0)
                {
                    // Die übergebene Aktion ausführen (z. B. den Wert des Controls zuweisen)
                    anpassungsAktion(ctrl.model);

                    // Den gesamten Datensatz mit der neuen Änderung aktualisieren
                    ctrl.Update(m_ID_Projekt);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim automatischen Speichern: " + ex.Message);
            }
        }

        // =====================================================================
        // PAKET BHKW-REGULÄR (Entscheidung des Anwenders 17.08.2026, Punkt 4):
        //
        // Hier stand PendelspeicherFeldEinrichten() - die Einrichtung des Feldes „Volumen
        // Pendelspeicher [l]" auf der BHKW-Parameterseite. Das FELD IST AUSGEBAUT, samt
        // Beschriftung (label56) und Eingabefeld (numericUpDown_Volumen) im Designer.
        //
        // WARUM. Ein Pendelspeicher ist ein Pufferspeicher. Ihn an ZWEI Stellen zu pflegen
        // - hier als bloße Literzahl und in der Pufferverwaltung als vollständiger Speicher
        // mit Verwendung, Temperaturpaar, Schaltschwellen und jetzt Notreserve - hieß, dass
        // dieselbe Anlage zwei Wahrheiten hatte. Der BHKW-Speicher wird ab diesem Paket
        // ausschließlich über die Pufferverwaltung und die Senkenzuordnung der BHKW-Anlage
        // geführt, wie bei jedem anderen Wärmeerzeuger auch.
        //
        // WAS BLEIBT. Bestehende Puffer mit dem Bezeichner „BHKW-Pendelspeicher" bleiben
        // normale Projektpuffer und rechnen weiter mit - über den Ersatzspeicher-Weg
        // (SimulationControl.BhkwErsatzspeicherAufnehmen). Auch die Lesefunktion
        // PufferSpCtrl.PendelspeicherVolumenLiter bleibt: Sie speist
        // SimulationControl.VolumenPendelspeicherBHKW, und dieses Feld entscheidet weiterhin
        // über die Speicherbeteiligung eines BHKW ohne Puffer-Senke (Altbestände aus
        // Migrationsregel R6). Nur der SCHREIBWEG über die Oberfläche ist entfallen; die
        // Volumenpflege läuft über die Pufferverwaltung.
        // =====================================================================

        private void LeseKonfiguration()
        {
            KonfigurationCtrl ctrl = new KonfigurationCtrl();
            ctrl.ReadSingle("select * from Tab_Einstellungen where ID_Projekt=" + m_ID_Projekt);
            if (ctrl.rows > 0)
            {
                checkBox_Heizstab.Checked = ctrl.model.m_WP_Heizstab;
                textBox_Netzverluste.Text = ctrl.model.m_Netzverluste.ToString();
                comboBox_NetzvEinheit.Text = ctrl.model.m_szNetzverlusteEinheit;
                numericUpDown_UnteresteLG.Value = (decimal)ctrl.model.Leistungsgrenze;
                textBox_Bereitschaft.Text = ctrl.model.m_Kessel_Betriebsbereitschaft.ToString();
                int mode = ctrl.model.Betriebsart;
                radioButton_OhneStromEinspeisung.CheckedChanged -= radioButton_Stromgefuehrt_CheckedChanged;
                if (mode == 0) radioButton_Waermegefuehrt.Checked = true;
                else if (mode == 1) radioButton_Stromgefuehrt.Checked = true;
                else radioButton_OhneStromEinspeisung.Checked = true;
                radioButton_OhneStromEinspeisung.CheckedChanged += radioButton_Stromgefuehrt_CheckedChanged;
                // Die vier Ladeparameter standen bis AP3b hier: sie kamen projektweit
                // aus Tab_Einstellungen und wirkten nirgends. Seit Fachkonzept 5.6 hängen
                // sie an der aktiven Speichervariante - gelesen wird in
                // LeseSpeicherVariante(), geschrieben zielgenau je Feld.
            }
        }

        private void numericUpDown_UnteresteLG_Leave(object sender, EventArgs e)
        {
            SpeichereKonfigurationsAenderung(model => model.Leistungsgrenze = (int)numericUpDown_UnteresteLG.Value);
        }

        // PAKET BHKW-REGULÄR: Hier stand numericUpDown_Volumen_Leave - der SCHREIBWEG des
        // Pendelspeichervolumens über PufferSpCtrl.SetPendelspeicherVolumenLiter. Er ist mit
        // dem Eingabefeld entfallen (Begründung oben). Die Ctrl-Methode selbst bleibt
        // bestehen: Migrationsregel R6 benutzt sie beim Anlegen des Puffers.
        private void textBox_Netzverluste_Leave(object sender, EventArgs e)
        {
            SpeichereKonfigurationsAenderung(model => model.m_Netzverluste = Convert.ToDouble(textBox_Netzverluste.Text));
        }

        private void comboBox_NetzvEinheit_SelectedValueChanged(object sender, EventArgs e)
        {
            SpeichereKonfigurationsAenderung(model => model.m_szNetzverlusteEinheit = comboBox_NetzvEinheit.Text);
        }

        private void textBox_Bereitschaft_Leave(object sender, EventArgs e)
        {
            SpeichereKonfigurationsAenderung(model => model.m_Kessel_Betriebsbereitschaft = Convert.ToInt32(textBox_Bereitschaft.Text));
        }



        // ====================================================================
        //  Ladeparameter: seit AP3b Sache der aktiven Speichervariante
        // ====================================================================
        //
        // Die vier Felder im Designer bleiben, ihr Ziel wechselt: Bis hierher schrieben
        // sie projektweit nach Tab_Einstellungen (Ladefuellstand_Min/_Max,
        // Ladeleistung_Max, Ladeschwellwert) - Werte, die keine Simulation je gelesen
        // hat (Umsetzungskonzept 1.2 g). Sie gehen jetzt in Tab_StromspeicherVariante,
        // weil jede Variante ein eigenes SoC-Band braucht (Fachkonzept 5.6/7.3).
        //
        // Das Schreibmuster der Seite bleibt: zielgenau beim Verlassen des Feldes.

        private void textBox_Stromspeicher_Ladeenergie_min_Leave(object sender, EventArgs e)
        {
            double wert;
            if (SpZahl(textBox_Stromspeicher_Ladeenergie_min, out wert))
                SpeichereVariantenAenderung(v => v.SoC_Min_Prozent = wert);
            else
                SpFeldZuruecksetzen(textBox_Stromspeicher_Ladeenergie_min,
                                    _speicherVariante != null ? _speicherVariante.SoC_Min_Prozent : 0.0, "F0");
        }

        private void textBox_Stromspeicher_Ladeenergie_max_Leave(object sender, EventArgs e)
        {
            double wert;
            if (SpZahl(textBox_Stromspeicher_Ladeenergie_max, out wert))
                SpeichereVariantenAenderung(v => v.SoC_Max_Prozent = wert);
            else
                SpFeldZuruecksetzen(textBox_Stromspeicher_Ladeenergie_max,
                                    _speicherVariante != null ? _speicherVariante.SoC_Max_Prozent : 0.0, "F0");
        }

        /// <summary>
        /// Die Lade-/Entladeleistung ist seit AP3b GERÄTEDATUM (Summe der
        /// <c>Tab_Stromspeicher.Leistung</c> aller Speicheranlagen des Projekts) und
        /// wird hier nur angezeigt — das Feld steht auf <c>ReadOnly</c>. Gepflegt wird
        /// die Leistung im Speicherkatalog (<c>Form_AdminStromspeicher</c>).
        /// </summary>
        private void textBox_Stromspeicher_Ladeleistung_max_Leave(object sender, EventArgs e)
        {
        }

        private void textBox_Speicher_Ladeschwelle_Leave(object sender, EventArgs e)
        {
            double wert;
            if (SpZahl(textBox_Speicher_Ladeschwelle, out wert))
                SpeichereVariantenAenderung(v => v.Ladeschwellwert = wert);
            else
                SpFeldZuruecksetzen(textBox_Speicher_Ladeschwelle,
                                    _speicherVariante != null ? _speicherVariante.Ladeschwellwert : 0.0, "F2");
        }

        // Die drei Einheiten-Auswahlfelder sind seit AP3b fest: Das SoC-Band einer
        // Variante steht immer in % der Nennkapazität (die Alternative "kWh/a" der
        // Auswahlliste ist ohne Gerätekapazität nicht umrechenbar), die Leistung immer
        // in kW. Die Felder sind deshalb gesperrt bzw. ausgeblendet; ihre Ereignisse
        // können nur noch beim programmatischen Setzen auslösen und dürfen dann nichts
        // schreiben.

        private void comboBox_Stromspeicher_LadeenergieMax_auswahl_SelectedValueChanged(object sender, EventArgs e)
        {
        }

        private void comboBox8_Stromspeicher_LadeenergieMin_auswahl_SelectedValueChanged(object sender, EventArgs e)
        {
        }

        private void comboBox_Stromspeicher_LadeleistungMax_auswahl_SelectedValueChanged(object sender, EventArgs e)
        {
        }

        // ====================================================================
        //  Parameterseite Stromspeicher (AP3b, Fachkonzept 5.1/5.6)
        // ====================================================================
        //
        // AUSGANGSLAGE. Die Seite trug vier Eingabefelder (SoC-Band, Ladeleistung,
        // Ladeschwellwert), die projektweit nach Tab_Einstellungen schrieben und von
        // keiner Rechnung gelesen wurden. Alles, was die Engine wirklich braucht -
        // Betriebsart, Quellen, Berechnungsart, Zins, Nutzungsdauer - fehlte.
        //
        // AUFBAU. Die vier Bestandsfelder hängen jetzt an der aktiven Variante; die
        // neuen Eingaben entstehen HIER IM CODE und nicht im Designer, aus demselben
        // Grund wie in Form_AdminStromspeicher (AP3a): Das Formular legt jede Position
        // und Beschriftung in Form_Simulation_Detail.resx ab (durchgängig
        // resources.ApplyResources); neue Steuerelemente dort einzutragen hieße,
        // Designer- und Ressourcendateien von Hand zu schreiben - was CLAUDE.md
        // ausschließt.
        //
        // Auch die vier BESTANDSBESCHRIFTUNGEN werden hier gesetzt: Sie tragen im
        // Designer feste deutsche Texte ("Minimum Ladeenergie"), und die englische
        // Satellitendatei bildet zwei von ihnen sogar auf fremde Texte ab ("Result",
        // "Residual heat requirement:"). Der Weg über den Ressourcenkatalog ist das
        // Hausmuster für genau diesen Fall (NavigatorUebersicht.BeschriftungenSetzen).
        //
        // LAYOUT. Linke Spalte = Raster des Bestands (Label 35, Feld 217, Einheit 316),
        // darunter die Betriebsführung; rechte Spalte ab x = 420 die Ladequellen und
        // die Wirtschaftlichkeit.

        private const int SP_SPALTE_A_LABEL = 35;
        private const int SP_SPALTE_A_FELD = 217;
        private const int SP_SPALTE_A_EINHEIT = 316;

        /// <summary>
        /// Spalte des kWh-Äquivalents neben den SoC-Prozentfeldern (Abnahmebefund 1).
        /// Sie liegt auf dem Platz der beiden gesperrten Einheiten-Auswahllisten, die
        /// dafür unsichtbar werden, und endet vor der rechten Spalte (x = 420).
        /// </summary>
        private const int SP_SPALTE_A_KWH = 340;
        // Die rechte Spalte endet bei x ≈ 760 und bleibt damit innerhalb der Breite, die
        // von tabPage_Stromspeicher_Parameter (1016 px) nach Abzug der 200 px breiten
        // Menüspalte des TabListMapper sichtbar bleibt.
        private const int SP_SPALTE_B_LABEL = 420;
        private const int SP_SPALTE_B_FELD = 600;
        private const int SP_SPALTE_B_EINHEIT = 696;
        private const int SP_FELD_BREITE = 89;
        private const int SP_FELD_HOEHE = 25;
        private const int SP_ZEILE_HOEHE = 32;

        private ComboBox comboBox_SpBetriebsart;
        private ComboBox comboBox_SpBerechnungsart;
        private CheckBox checkBox_SpPV;
        private CheckBox checkBox_SpBHKW;
        private CheckBox checkBox_SpBHKWStromgefuehrt;
        private CheckBox checkBox_SpNetzentladung;
        private CheckBox checkBox_SpKompatibilitaet;
        private TextBox textBox_SpKapitalzins;
        private TextBox textBox_SpNutzungsdauer;
        private TextBox textBox_SpLeistungspreis;
        private TextBox textBox_SpNetzladeaufschlag;
        private Label label_SpVariantenstatus;

        // --- Gerätedaten der gerechneten Einheit (Abnahmebefund 1) ---
        private TextBox textBox_SpKapazitaet;
        private Label label_SpSoCMinKwh;
        private Label label_SpSoCMaxKwh;

        // --- Preisbeschaffung (AP4, Fachkonzept 4.1/4.2) ---
        private ComboBox comboBox_SpPreisquelle;
        private ComboBox comboBox_SpPreisreihe;
        private CheckBox checkBox_SpAufschlag;
        private Label label_SpPreisinfo;
        private Label label_SpPreisreiheLabel;

        /// <summary>Die aktive Speichervariante des Projekts, oder <c>null</c>.</summary>
        private StromspeicherVarianteModel _speicherVariante;

        /// <summary>
        /// Sperrt das Zurückschreiben, solange die Felder programmatisch befüllt
        /// werden — sonst löste jedes Setzen von <c>Text</c> bzw. <c>Checked</c> ein
        /// UPDATE aus.
        /// </summary>
        private bool _speicherFelderLaden;

        /// <summary>
        /// Ob das Projekt eine aktive Speichervariante führt — die Grundbedingung
        /// jeder Eingabe auf der Parameterseite. Gemerkt, weil
        /// <see cref="SpKompatibilitaetVerfuegbarkeit"/> auch außerhalb von
        /// <see cref="LeseSpeicherVariante"/> läuft (bei jedem Wechsel der
        /// Berechnungsart).
        /// </summary>
        private bool _speicherVarianteVorhanden;

        /// <summary>
        /// Auswahleintrag einer ComboBox: Persistenzwert (Schicht 1, deutsch und
        /// eingefroren) und Anzeigetext (Schicht 3) getrennt — die Drei-Schichten-Regel
        /// verbietet, den Anzeigetext als Steuerwert zu verwenden.
        /// </summary>
        private sealed class SpAuswahl
        {
            public readonly string Wert;
            private readonly string _anzeige;

            public SpAuswahl(string wert, string anzeige)
            {
                Wert = wert;
                _anzeige = anzeige;
            }

            public override string ToString() { return _anzeige; }
        }

        /// <summary>
        /// Auswahleintrag einer Preisreihe bzw. eines Kostenprofils: die
        /// Datenbank-ID und der Anzeigetext (AP4).
        /// </summary>
        private sealed class SpReihe
        {
            public readonly int Id;
            private readonly string _anzeige;

            public SpReihe(int id, string anzeige)
            {
                Id = id;
                _anzeige = anzeige;
            }

            public override string ToString() { return _anzeige; }
        }

        private void InitStromspeicherParameter()
        {
            if (tabPage_Stromspeicher_Parameter == null) return;

            // Die Seite hat mit dem Preisblock (AP4) mehr Inhalt als Fläche auf kleinen
            // Bildschirmen - AutoScroll statt gequetschter Zeilenabstände.
            tabPage_Stromspeicher_Parameter.AutoScroll = true;

            // --- Bestandsfelder: Beschriftung, Rolle, Einheit -----------------
            label40.Text = MyResource.Resource.SP_PARAM_LABEL_SOC_MIN;
            label11.Text = MyResource.Resource.SP_PARAM_LABEL_SOC_MAX;
            label7.Text = MyResource.Resource.SP_PARAM_LABEL_LADELEISTUNG;
            label12.Text = MyResource.Resource.SP_PARAM_LABEL_LADESCHWELLE;

            textBox_Stromspeicher_Ladeenergie_min.TextChanged += (s, e) => Program.ZahlFaerben(s);
            textBox_Stromspeicher_Ladeenergie_max.TextChanged += (s, e) => Program.ZahlFaerben(s);
            textBox_Speicher_Ladeschwelle.TextChanged += (s, e) => Program.ZahlFaerben(s);

            // Abnahmebefund 1: Das SoC-Band steht in Prozent, der Anwender denkt in kWh.
            // Das Aequivalent laeuft am TextChanged mit - also schon waehrend der Eingabe
            // und nicht erst beim Verlassen des Feldes.
            textBox_Stromspeicher_Ladeenergie_min.TextChanged += (s, e) => SpSoCAequivalenteAktualisieren();
            textBox_Stromspeicher_Ladeenergie_max.TextChanged += (s, e) => SpSoCAequivalenteAktualisieren();

            // Der Ladeschwellwert hat seit AP10 die Bedeutung, die Fachkonzept 5.6 für
            // ihn vorsieht: manuelle Zusatzschranke der Preissteuerung (6.5). Bis dahin
            // war er ein migriertes Altfeld ohne Wirkung.
            tooltip.SetToolTip(textBox_Speicher_Ladeschwelle, MyResource.Resource.ARB_PARAM_HINWEIS_LADESCHWELLE);
            tooltip.SetToolTip(label12, MyResource.Resource.ARB_PARAM_HINWEIS_LADESCHWELLE);

            // Die Einheit des SoC-Bands ist fest: % der Nennkapazität. Die zweite
            // Auswahl "kWh/a" der Liste ist ohne Gerätekapazität nicht umrechenbar.
            //
            // ABNAHMEBEFUND 1: Die beiden gesperrten Auswahllisten weichen dem
            // kWh-Äquivalent - dieselbe Behandlung, die die Ladeleistungs-Liste eine
            // Zeile weiter unten schon seit AP3b bekommt. Eine Liste mit genau einem
            // wählbaren Eintrag ist keine Auswahl, sondern eine Einheit; als Text
            // geschrieben bleibt in der linken Spalte Platz für die Umrechnung, die der
            // Anwender wirklich braucht.
            comboBox8_Stromspeicher_LadeenergieMin_auswahl.Text = DbWerte.SP_EINHEIT_PROZENT;
            comboBox8_Stromspeicher_LadeenergieMin_auswahl.Enabled = false;
            comboBox8_Stromspeicher_LadeenergieMin_auswahl.Visible = false;
            comboBox_Stromspeicher_LadeenergieMax_auswahl.Text = DbWerte.SP_EINHEIT_PROZENT;
            comboBox_Stromspeicher_LadeenergieMax_auswahl.Enabled = false;
            comboBox_Stromspeicher_LadeenergieMax_auswahl.Visible = false;

            SpEinheitAnlegen(DbWerte.SP_EINHEIT_PROZENT, SP_SPALTE_A_EINHEIT, 32,
                             MyResource.Resource.SP_PARAM_HINWEIS_SOC_EINHEIT);
            SpEinheitAnlegen(DbWerte.SP_EINHEIT_PROZENT, SP_SPALTE_A_EINHEIT, 64,
                             MyResource.Resource.SP_PARAM_HINWEIS_SOC_EINHEIT);

            label_SpSoCMinKwh = SpEinheitAnlegen("", SP_SPALTE_A_KWH, 32,
                                                 MyResource.Resource.SP_PARAM_HINWEIS_SOC_KWH);
            label_SpSoCMaxKwh = SpEinheitAnlegen("", SP_SPALTE_A_KWH, 64,
                                                 MyResource.Resource.SP_PARAM_HINWEIS_SOC_KWH);

            // Die Ladeleistung ist Gerätedatum und wird nur noch angezeigt. Die
            // zugehörige Einheitenauswahl entfällt - "kW" steht am Label, und die
            // Liste kennt die Einheit gar nicht (nur "%" und "kWh/a").
            textBox_Stromspeicher_Ladeleistung_max.ReadOnly = true;
            textBox_Stromspeicher_Ladeleistung_max.BackColor = SystemColors.Control;
            comboBox_Stromspeicher_LadeleistungMax_auswahl.Visible = false;
            tooltip.SetToolTip(textBox_Stromspeicher_Ladeleistung_max,
                               MyResource.Resource.SP_PARAM_HINWEIS_LADELEISTUNG);

            // ABNAHMEBEFUND 1: Die KAPAZITÄT fehlte auf der Seite ganz - die Größe, um
            // die es beim Speicher zuerst geht. Sie steht direkt bei der zweiten
            // schreibgeschützten Gerätegröße (Lade-/Entladeleistung) und im selben
            // Raster; die Einheit trägt das Label, wie bei der Leistung auch.
            textBox_SpKapazitaet = SpAnzeigefeldAnlegen(
                MyResource.Resource.SP_PARAM_LABEL_KAPAZITAET, 168,
                MyResource.Resource.SP_PARAM_HINWEIS_KAPAZITAET);

            // Der Hinweis steht UNTER dem Block der Bestandsfelder und bleibt in der
            // linken Spalte (x < 420) - rechts daneben liegen die Ladequellen-Schalter.
            SpHinweisAnlegen(MyResource.Resource.SP_PARAM_HINWEIS_LADELEISTUNG,
                             SP_SPALTE_A_LABEL, 200, 370, 30);

            // --- Linke Spalte: Betriebsführung --------------------------------
            int zeile = 234;
            SpKopfAnlegen(MyResource.Resource.SP_PARAM_GRUPPE_BETRIEBSFUEHRUNG, SP_SPALTE_A_LABEL, zeile);

            zeile += 28;
            comboBox_SpBetriebsart = SpAuswahlAnlegen(
                MyResource.Resource.SP_PARAM_LABEL_BETRIEBSART, SP_SPALTE_A_LABEL, SP_SPALTE_A_FELD, zeile,
                new[]
                {
                    new SpAuswahl(DbWerte.SP_BETRIEBSART_GRUENSTROM, MyResource.Resource.SP_BETRIEBSART_ANZEIGE_GRUENSTROM),
                    new SpAuswahl(DbWerte.SP_BETRIEBSART_GRAUSTROM, MyResource.Resource.SP_BETRIEBSART_ANZEIGE_GRAUSTROM)
                });
            comboBox_SpBetriebsart.SelectedIndexChanged += (s, e) =>
                SpeichereVariantenAenderung(v => v.Betriebsart = SpGewaehlterWert(comboBox_SpBetriebsart));

            zeile += SP_ZEILE_HOEHE;
            // Berechnungsart: Die Liste wächst mit den Ausbaustufen - jede ist EINE
            // weitere Zeile hier. Was die Engine nicht kann, steht bewusst nicht in der
            // Liste: Ein wählbarer, aber wirkungsloser Eintrag wäre schlimmer als ein
            // fehlender. Umgesetzt sind Dauernutzung (AP1), Nachtnutzung (AP6) und
            // Preissteuerung/Arbitrage (AP10).
            comboBox_SpBerechnungsart = SpAuswahlAnlegen(
                MyResource.Resource.SP_PARAM_LABEL_BERECHNUNGSART, SP_SPALTE_A_LABEL, SP_SPALTE_A_FELD, zeile,
                new[]
                {
                    new SpAuswahl(DbWerte.SP_BERECHNUNG_DAUERNUTZUNG, MyResource.Resource.SP_BERECHNUNG_ANZEIGE_DAUERNUTZUNG),
                    new SpAuswahl(DbWerte.SP_BERECHNUNG_NACHTNUTZUNG, MyResource.Resource.SP_BERECHNUNG_ANZEIGE_NACHTNUTZUNG),
                    new SpAuswahl(DbWerte.SP_BERECHNUNG_ARBITRAGE, MyResource.Resource.SP_BERECHNUNG_ANZEIGE_ARBITRAGE)
                });
            comboBox_SpBerechnungsart.SelectedIndexChanged += (s, e) =>
            {
                SpeichereVariantenAenderung(v => v.Berechnungsart = SpGewaehlterWert(comboBox_SpBerechnungsart));
                SpKompatibilitaetVerfuegbarkeit();
            };

            zeile += SP_ZEILE_HOEHE;
            checkBox_SpKompatibilitaet = SpSchalterAnlegen(
                MyResource.Resource.SP_PARAM_LABEL_KOMPATIBILITAET, SP_SPALTE_A_FELD, zeile, true);
            tooltip.SetToolTip(checkBox_SpKompatibilitaet, MyResource.Resource.SP_PARAM_HINWEIS_KOMPATIBILITAET);
            checkBox_SpKompatibilitaet.CheckedChanged += (s, e) =>
                SpeichereVariantenAenderung(v => v.Kompatibilitaetsmodus = checkBox_SpKompatibilitaet.Checked);

            zeile += 24;
            // Breite so bemessen, dass der Hinweis vor der rechten Spalte (x = 420) endet.
            SpHinweisAnlegen(MyResource.Resource.SP_PARAM_HINWEIS_KOMPATIBILITAET, SP_SPALTE_A_LABEL, zeile, 370, 30);

            // --- Rechte Spalte: Ladequellen -----------------------------------
            zeile = 32;
            SpKopfAnlegen(MyResource.Resource.SP_PARAM_GRUPPE_QUELLEN, SP_SPALTE_B_LABEL, zeile);

            zeile += 28;
            checkBox_SpPV = SpSchalterAnlegen(MyResource.Resource.SP_PARAM_CHK_PV, SP_SPALTE_B_LABEL, zeile, true);
            checkBox_SpPV.CheckedChanged += (s, e) =>
                SpeichereVariantenAenderung(v => v.PV_Zulaessig = checkBox_SpPV.Checked);

            zeile += 26;
            checkBox_SpBHKW = SpSchalterAnlegen(MyResource.Resource.SP_PARAM_CHK_BHKW, SP_SPALTE_B_LABEL, zeile, true);
            checkBox_SpBHKW.CheckedChanged += (s, e) =>
                SpeichereVariantenAenderung(v => v.BHKW_Ueberschuss_Zulaessig = checkBox_SpBHKW.Checked);

            // Netzentladung: seit AP10 WIRKSAM. Sie ist unabhängig von der Betriebsart
            // (Fachkonzept 2.1 - auch ein Grünstromspeicher darf verkaufen), braucht
            // aber die Berechnungsart „Preissteuerung / Arbitrage": Erst die bestimmt,
            // WANN verkauft wird. Genau das sagt der Tooltip.
            zeile += 26;
            checkBox_SpNetzentladung = SpSchalterAnlegen(
                MyResource.Resource.SP_PARAM_CHK_NETZENTLADUNG, SP_SPALTE_B_LABEL, zeile, true);
            tooltip.SetToolTip(checkBox_SpNetzentladung, MyResource.Resource.ARB_PARAM_HINWEIS_NETZENTLADUNG);
            checkBox_SpNetzentladung.CheckedChanged += (s, e) =>
                SpeichereVariantenAenderung(v => v.Netzentladung = checkBox_SpNetzentladung.Checked);

            // Stromgeführtes BHKW-Nachladen bleibt ohne Rechenweg (Ausbaustufe 11). Der
            // Schalter ist sichtbar und ausgegraut - der Anwender soll sehen, dass es
            // ihn gibt, aber nicht auf eine Wirkung warten, die ausbleibt. Der
            // Ausbaustufen-Hinweis steht deshalb jetzt unter IHM.
            zeile += 26;
            checkBox_SpBHKWStromgefuehrt = SpSchalterAnlegen(
                MyResource.Resource.SP_PARAM_CHK_BHKW_STROMGEFUEHRT, SP_SPALTE_B_LABEL, zeile, false);
            tooltip.SetToolTip(checkBox_SpBHKWStromgefuehrt, MyResource.Resource.SP_PARAM_HINWEIS_AUSBAUSTUFE);

            zeile += 26;
            SpHinweisAnlegen(MyResource.Resource.SP_PARAM_HINWEIS_AUSBAUSTUFE, SP_SPALTE_B_LABEL + 18, zeile, 340);

            // --- Rechte Spalte: Wirtschaftlichkeit ----------------------------
            zeile = 200;
            SpKopfAnlegen(MyResource.Resource.SP_PARAM_GRUPPE_WIRTSCHAFT, SP_SPALTE_B_LABEL, zeile);

            zeile += 28;
            textBox_SpKapitalzins = SpFeldAnlegen(MyResource.Resource.SP_PARAM_LABEL_KAPITALZINS, "%", zeile);
            textBox_SpKapitalzins.Leave += (s, e) =>
            {
                double wert;
                if (SpZahl(textBox_SpKapitalzins, out wert)) SpeichereVariantenAenderung(v => v.Kapitalzins = wert);
                else SpFeldZuruecksetzen(textBox_SpKapitalzins, _speicherVariante != null ? _speicherVariante.Kapitalzins : 0.0, "F2");
            };

            zeile += SP_ZEILE_HOEHE;
            textBox_SpNutzungsdauer = SpFeldAnlegen(MyResource.Resource.SP_PARAM_LABEL_NUTZUNGSDAUER, "a", zeile);
            textBox_SpNutzungsdauer.Leave += (s, e) =>
            {
                double wert;
                if (SpZahl(textBox_SpNutzungsdauer, out wert)) SpeichereVariantenAenderung(v => v.Nutzungsdauer = wert);
                else SpFeldZuruecksetzen(textBox_SpNutzungsdauer, _speicherVariante != null ? _speicherVariante.Nutzungsdauer : 0.0, "F0");
            };

            // L_P wird auf DIESER Seite nicht monetarisiert: Die Leistungspreisersparnis
            // entsteht im Peak-Shaving, und das hat seit AP7 eine eigene Maske
            // (Fachkonzept 4.4/6.4). Das Feld speichert deshalb nur; der Tooltip weist
            // die Ausbaustufe aus. Der Netzladeaufschlag a_netzlade darunter ist seit
            // AP10 wirksam - er bildet p_netzlade = p_energie + a_netzlade.
            zeile += SP_ZEILE_HOEHE;
            textBox_SpLeistungspreis = SpFeldAnlegen(MyResource.Resource.SP_PARAM_LABEL_LEISTUNGSPREIS, "€/(kW·a)", zeile);
            tooltip.SetToolTip(textBox_SpLeistungspreis, MyResource.Resource.SP_PARAM_HINWEIS_AUSBAUSTUFE);
            textBox_SpLeistungspreis.Leave += (s, e) =>
            {
                double wert;
                if (SpZahl(textBox_SpLeistungspreis, out wert)) SpeichereVariantenAenderung(v => v.L_P = wert);
                else SpFeldZuruecksetzen(textBox_SpLeistungspreis, _speicherVariante != null ? _speicherVariante.L_P : 0.0, "F2");
            };

            zeile += SP_ZEILE_HOEHE;
            textBox_SpNetzladeaufschlag = SpFeldAnlegen(MyResource.Resource.SP_PARAM_LABEL_NETZLADEAUFSCHLAG, "ct/kWh", zeile);
            tooltip.SetToolTip(textBox_SpNetzladeaufschlag, MyResource.Resource.ARB_PARAM_HINWEIS_NETZLADEAUFSCHLAG);
            textBox_SpNetzladeaufschlag.Leave += (s, e) =>
            {
                double wert;
                if (SpZahl(textBox_SpNetzladeaufschlag, out wert)) SpeichereVariantenAenderung(v => v.A_Netzlade = wert);
                else SpFeldZuruecksetzen(textBox_SpNetzladeaufschlag, _speicherVariante != null ? _speicherVariante.A_Netzlade : 0.0, "F2");
            };

            // --- Linke Spalte: Preisbeschaffung (AP4, Fachkonzept 4.1/4.2) -----
            //
            // Der Block steht bei der BETRIEBSFÜHRUNG und nicht bei der
            // Wirtschaftlichkeit: Die Preisquelle entscheidet über den Geldwert JEDES
            // Intervalls und damit über den Fahrplan des Speichers, nicht erst über die
            // Jahresauswertung. Die Aufschlagskomponenten selbst werden im Kostenmodul
            // gepflegt (ucStromAufschlaege) - hier steht nur, OB sie gelten.
            zeile = 390;
            SpKopfAnlegen(MyResource.Resource.PREIS_PARAM_GRUPPE_PREISQUELLE, SP_SPALTE_A_LABEL, zeile);

            zeile += 28;
            comboBox_SpPreisquelle = SpAuswahlAnlegen(
                MyResource.Resource.PREIS_PARAM_LABEL_PREISQUELLE, SP_SPALTE_A_LABEL, SP_SPALTE_A_FELD, zeile,
                new[]
                {
                    new SpAuswahl(DbWerte.SP_PREISQUELLE_FIXPREIS, MyResource.Resource.PREIS_QUELLE_ANZEIGE_FIXPREIS),
                    new SpAuswahl(DbWerte.SP_PREISQUELLE_PROFIL, MyResource.Resource.PREIS_QUELLE_ANZEIGE_PROFIL),
                    new SpAuswahl(DbWerte.SP_PREISQUELLE_SPOTMARKT, MyResource.Resource.PREIS_QUELLE_ANZEIGE_SPOTMARKT)
                });
            comboBox_SpPreisquelle.SelectedIndexChanged += (s, e) =>
            {
                SpeichereVariantenAenderung(v => v.Preisquelle = SpGewaehlterWert(comboBox_SpPreisquelle));
                SpReihenlisteFuellen();
            };

            zeile += SP_ZEILE_HOEHE;
            label_SpPreisreiheLabel = new Label
            {
                Text = MyResource.Resource.PREIS_PARAM_LABEL_REIHE,
                Location = new Point(SP_SPALTE_A_LABEL, zeile + 4),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.75f, FontStyle.Regular)
            };
            tabPage_Stromspeicher_Parameter.Controls.Add(label_SpPreisreiheLabel);

            comboBox_SpPreisreihe = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(SP_SPALTE_A_FELD, zeile),
                Size = new Size(340, SP_FELD_HOEHE),
                Font = new Font("Segoe UI", 9.75f, FontStyle.Regular)
            };
            comboBox_SpPreisreihe.SelectedIndexChanged += (s, e) => SpReiheGewaehlt();
            tabPage_Stromspeicher_Parameter.Controls.Add(comboBox_SpPreisreihe);

            zeile += SP_ZEILE_HOEHE;
            checkBox_SpAufschlag = SpSchalterAnlegen(
                MyResource.Resource.PREIS_PARAM_CHK_AUFSCHLAG, SP_SPALTE_A_FELD, zeile, true);
            tooltip.SetToolTip(checkBox_SpAufschlag, MyResource.Resource.PREIS_PARAM_HINWEIS_AUFSCHLAG);
            checkBox_SpAufschlag.CheckedChanged += (s, e) =>
            {
                SpeichereVariantenAenderung(v => v.Aufschlag_Anwenden = checkBox_SpAufschlag.Checked);
                SpPreisinfoAktualisieren();
            };

            zeile += 26;
            label_SpPreisinfo = SpHinweisAnlegen("", SP_SPALTE_A_LABEL, zeile, 700, 34);

            // --- Fußzeile: welche Variante wird hier bearbeitet? ---------------
            label_SpVariantenstatus = SpHinweisAnlegen("", SP_SPALTE_A_LABEL, zeile + 40, 700, 24);
            label_SpVariantenstatus.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

            // --- Auslegungsoptimierung (AP8, Fachkonzept 6.3) ------------------
            //
            // Der EINZIGE Einbau dieses Pakets auf der Parameterseite: ein Knopf, der
            // den eigenständigen Dialog öffnet. Die Rastersuche braucht nichts von
            // dieser Seite außer dem Projekt und dem gerechneten Simulationsobjekt;
            // alles Weitere - Suchraum, Fortschritt, Heatmap, Übernahme - steht in
            // Form_SpeicherOptimierung.
            Button knopfOptimierung = new Button();
            knopfOptimierung.Name = "button_SpOptimierung";
            knopfOptimierung.Text = MyResource.Resource.OPT_BTN_OEFFNEN;
            knopfOptimierung.Location = new Point(SP_SPALTE_B_LABEL, zeile + 36);
            knopfOptimierung.Size = new Size(220, 30);
            knopfOptimierung.Click += SpOptimierung_Click;
            tooltip.SetToolTip(knopfOptimierung, MyResource.Resource.OPT_HINWEIS_ZIELFUNKTION);
            tabPage_Stromspeicher_Parameter.Controls.Add(knopfOptimierung);
        }

        /// <summary>
        /// Öffnet die Auslegungsoptimierung (AP8).
        /// </summary>
        /// <remarks>
        /// Nach einer Übernahme des Bestpunkts wird die Parameterseite aufgefrischt —
        /// die angezeigte Ladeleistung ist ein Gerätedatum und hat sich dann geändert.
        /// Neu gerechnet wird bewusst nicht; das entscheidet der Anwender.
        /// </remarks>
        private void SpOptimierung_Click(object sender, EventArgs e)
        {
            using (Form_SpeicherOptimierung frm = new Form_SpeicherOptimierung(sim, m_ID_Projekt))
            {
                frm.ShowDialog(this);
                if (frm.AuslegungUebernommen) LeseSpeicherVariante();
            }
        }

        // ====================================================================
        //  Preisbeschaffung der Parameterseite (AP4)
        // ====================================================================

        /// <summary>
        /// Füllt die Reihenauswahl passend zur gewählten Preisquelle: Spotreihen aus
        /// <c>Tab_Preisreihe</c>, Kostenprofile aus <c>Tab_Kostenprofil</c>. Beim
        /// Fixpreis ist die Liste leer und gesperrt.
        /// </summary>
        private void SpReihenlisteFuellen()
        {
            if (comboBox_SpPreisreihe == null) return;

            string quelle = SpGewaehlterWert(comboBox_SpPreisquelle);
            bool laden = _speicherFelderLaden;
            _speicherFelderLaden = true;

            try
            {
                comboBox_SpPreisreihe.Items.Clear();
                comboBox_SpPreisreihe.Enabled = quelle != DbWerte.SP_PREISQUELLE_FIXPREIS;

                if (quelle == DbWerte.SP_PREISQUELLE_SPOTMARKT)
                {
                    label_SpPreisreiheLabel.Text = MyResource.Resource.PREIS_PARAM_LABEL_REIHE;
                    foreach (PreisreiheModel p in new PreisreiheCtrl().ReadVerfuegbare(m_ID_Projekt))
                        comboBox_SpPreisreihe.Items.Add(new SpReihe(p.ID,
                            string.Format(MyResource.Resource.PREIS_PARAM_REIHE_EINTRAG,
                                          p.Bezeichner, p.Jahr, p.Werteanzahl)));

                    SpReiheWaehlen(_speicherVariante != null ? _speicherVariante.ID_Preisreihe : 0);
                }
                else if (quelle == DbWerte.SP_PREISQUELLE_PROFIL)
                {
                    label_SpPreisreiheLabel.Text = MyResource.Resource.PREIS_PARAM_LABEL_PROFIL;
                    foreach (KostenprofilModel p in new KostenprofilCtrl().ReadAllByProjekt(m_ID_Projekt))
                        comboBox_SpPreisreihe.Items.Add(new SpReihe(p.ID, p.Bezeichner));

                    SpReiheWaehlen(_speicherVariante != null ? _speicherVariante.ID_Kostenprofil : 0);
                }
                else
                {
                    label_SpPreisreiheLabel.Text = MyResource.Resource.PREIS_PARAM_LABEL_REIHE;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Preisreihen konnten nicht gelesen werden: " + ex.Message);
            }
            finally
            {
                _speicherFelderLaden = laden;
            }

            SpPreisinfoAktualisieren();
        }

        private void SpReiheWaehlen(int id)
        {
            for (int i = 0; i < comboBox_SpPreisreihe.Items.Count; i++)
            {
                SpReihe r = comboBox_SpPreisreihe.Items[i] as SpReihe;
                if (r != null && r.Id == id) { comboBox_SpPreisreihe.SelectedIndex = i; return; }
            }
            comboBox_SpPreisreihe.SelectedIndex = -1;
        }

        /// <summary>
        /// Schreibt die gewählte Reihe in die Variante — je nach Preisquelle in
        /// <c>ID_Preisreihe</c> oder <c>ID_Kostenprofil</c>. Beide Felder getrennt zu
        /// führen erlaubt den Wechsel zwischen den Quellen, ohne die jeweils andere
        /// Auswahl zu verlieren.
        /// </summary>
        private void SpReiheGewaehlt()
        {
            SpReihe r = comboBox_SpPreisreihe.SelectedItem as SpReihe;
            int id = r != null ? r.Id : 0;
            string quelle = SpGewaehlterWert(comboBox_SpPreisquelle);

            if (quelle == DbWerte.SP_PREISQUELLE_SPOTMARKT)
                SpeichereVariantenAenderung(v => v.ID_Preisreihe = id);
            else if (quelle == DbWerte.SP_PREISQUELLE_PROFIL)
                SpeichereVariantenAenderung(v => v.ID_Kostenprofil = id);

            SpPreisinfoAktualisieren();
        }

        /// <summary>
        /// Zeigt an, welcher Bezugspreis mit den aktuellen Einstellungen entsteht —
        /// dieselbe Kette, die auch die Simulation durchläuft
        /// (<see cref="StromPreisCtrl"/>), damit auf dem Bildschirm keine zweite
        /// Preisrechnung steht.
        /// </summary>
        private void SpPreisinfoAktualisieren()
        {
            if (label_SpPreisinfo == null) return;

            try
            {
                StromPreisErgebnis p = new StromPreisCtrl().Baue(
                    m_ID_Projekt, _speicherVariante, SpeicherEngine.RasterAdapter.ViertelstundenJahr);

                CultureInfo k = CultureInfo.CurrentCulture;
                string text = string.Format(MyResource.Resource.PREIS_PARAM_INFO,
                                            p.EnergiepreisMittelCtKwh.ToString("0.###", k),
                                            p.AufschlagCtKwh.ToString("0.###", k),
                                            p.BezugspreisMittelCtKwh.ToString("0.###", k),
                                            p.Preisversion);

                if (!string.IsNullOrEmpty(p.Hinweis))
                    text += Environment.NewLine + p.Hinweis.Replace(Environment.NewLine, "  ");

                label_SpPreisinfo.Text = text;
                label_SpPreisinfo.ForeColor = string.IsNullOrEmpty(p.Hinweis)
                    ? Color.FromArgb(100, 100, 100)
                    : Color.Firebrick;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Preisvorschau konnte nicht gerechnet werden: " + ex.Message);
                label_SpPreisinfo.Text = "";
            }
        }

        /// <summary>Gruppenüberschrift der Parameterseite.</summary>
        private void SpKopfAnlegen(string text, int links, int oben)
        {
            Label kopf = new Label();
            kopf.Text = text;
            kopf.Location = new Point(links, oben);
            kopf.AutoSize = true;
            kopf.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            tabPage_Stromspeicher_Parameter.Controls.Add(kopf);
        }

        /// <summary>Kleingedruckter Hinweis (grau, mehrzeilig).</summary>
        private Label SpHinweisAnlegen(string text, int links, int oben, int breite, int hoehe = 34)
        {
            Label hinweis = new Label();
            hinweis.Text = text;
            hinweis.Location = new Point(links, oben);
            hinweis.Size = new Size(breite, hoehe);
            hinweis.AutoSize = false;
            hinweis.Font = new Font("Segoe UI", 8.25f, FontStyle.Regular);
            hinweis.ForeColor = Color.FromArgb(100, 100, 100);
            tabPage_Stromspeicher_Parameter.Controls.Add(hinweis);
            return hinweis;
        }

        /// <summary>Beschriftung + Eingabefeld + Einheit in der rechten Spalte.</summary>
        private TextBox SpFeldAnlegen(string beschriftung, string einheit, int oben)
        {
            Label lbl = new Label();
            lbl.Text = beschriftung;
            lbl.Location = new Point(SP_SPALTE_B_LABEL, oben + 4);
            lbl.AutoSize = true;
            lbl.Font = new Font("Segoe UI", 9.75f, FontStyle.Regular);
            tabPage_Stromspeicher_Parameter.Controls.Add(lbl);

            TextBox tb = new TextBox();
            tb.Location = new Point(SP_SPALTE_B_FELD, oben);
            tb.Size = new Size(SP_FELD_BREITE, SP_FELD_HOEHE);
            tb.Font = new Font("Segoe UI", 9.75f, FontStyle.Regular);
            tb.TextChanged += (s, e) => Program.ZahlFaerben(s);
            tabPage_Stromspeicher_Parameter.Controls.Add(tb);

            Label lblEinheit = new Label();
            lblEinheit.Text = einheit;
            lblEinheit.Location = new Point(SP_SPALTE_B_EINHEIT, oben + 4);
            lblEinheit.AutoSize = true;
            lblEinheit.Font = new Font("Segoe UI", 9.75f, FontStyle.Regular);
            tabPage_Stromspeicher_Parameter.Controls.Add(lblEinheit);

            return tb;
        }

        /// <summary>
        /// Kleines Beschriftungsfeld in der linken Spalte (Einheit bzw. kWh-Äquivalent) —
        /// Abnahmebefund 1.
        /// </summary>
        private Label SpEinheitAnlegen(string text, int links, int oben, string hinweis)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Location = new Point(links, oben + 4);
            lbl.AutoSize = true;
            lbl.Font = new Font("Segoe UI", 9.75f, FontStyle.Regular);
            lbl.ForeColor = Color.FromArgb(100, 100, 100);
            if (!string.IsNullOrEmpty(hinweis)) tooltip.SetToolTip(lbl, hinweis);
            tabPage_Stromspeicher_Parameter.Controls.Add(lbl);
            lbl.BringToFront();
            return lbl;
        }

        /// <summary>
        /// Schreibgeschütztes Anzeigefeld im Raster der linken Spalte (Abnahmebefund 1):
        /// Beschriftung inklusive Einheit, Feld auf <c>ReadOnly</c> und in Systemfarbe —
        /// dieselbe Darstellung, die die Lade-/Entladeleistung seit AP3b hat.
        /// </summary>
        private TextBox SpAnzeigefeldAnlegen(string beschriftung, int oben, string hinweis)
        {
            Label lbl = new Label();
            lbl.Text = beschriftung;
            lbl.Location = new Point(SP_SPALTE_A_LABEL, oben + 4);
            lbl.AutoSize = true;
            lbl.Font = new Font("Segoe UI", 9.75f, FontStyle.Regular);
            tabPage_Stromspeicher_Parameter.Controls.Add(lbl);

            TextBox tb = new TextBox();
            tb.Location = new Point(SP_SPALTE_A_FELD, oben);
            tb.Size = new Size(SP_FELD_BREITE, SP_FELD_HOEHE);
            tb.Font = new Font("Segoe UI", 9.75f, FontStyle.Regular);
            tb.ReadOnly = true;
            tb.BackColor = SystemColors.Control;
            tabPage_Stromspeicher_Parameter.Controls.Add(tb);

            if (!string.IsNullOrEmpty(hinweis))
            {
                tooltip.SetToolTip(lbl, hinweis);
                tooltip.SetToolTip(tb, hinweis);
            }

            return tb;
        }

        /// <summary>
        /// Schreibt das kWh-Äquivalent der beiden SoC-Prozentfelder fort
        /// (Abnahmebefund 1) — bezogen auf die Kapazität, die eine Handbreit darunter
        /// steht, damit die beiden Zahlen zueinander passen.
        /// </summary>
        /// <remarks>
        /// Ohne gepflegte Kapazität oder bei unlesbarem Prozentwert bleibt das Feld leer:
        /// Eine „0,00 kWh" wäre an dieser Stelle eine Aussage, die niemand gemacht hat.
        /// </remarks>
        private void SpSoCAequivalenteAktualisieren()
        {
            if (label_SpSoCMinKwh == null || label_SpSoCMaxKwh == null) return;

            double kapazitaet;
            if (!Program.ZahlParsen(textBox_SpKapazitaet != null ? textBox_SpKapazitaet.Text : "", out kapazitaet))
                kapazitaet = 0.0;

            label_SpSoCMinKwh.Text = SpKwhText(textBox_Stromspeicher_Ladeenergie_min, kapazitaet);
            label_SpSoCMaxKwh.Text = SpKwhText(textBox_Stromspeicher_Ladeenergie_max, kapazitaet);
        }

        private static string SpKwhText(TextBox prozentfeld, double kapazitaetKwh)
        {
            double prozent;
            if (kapazitaetKwh <= 0.0 || !Program.ZahlParsen(prozentfeld.Text, out prozent)) return "";

            return string.Format(MyResource.Resource.SP_PARAM_SOC_KWH,
                                 (kapazitaetKwh * prozent / 100.0).ToString("N2", CultureInfo.CurrentCulture));
        }

        /// <summary>Beschriftung + Auswahlliste (nur Listenauswahl, kein freier Text).</summary>
        private ComboBox SpAuswahlAnlegen(string beschriftung, int linksLabel, int linksFeld, int oben,
                                          SpAuswahl[] eintraege)
        {
            Label lbl = new Label();
            lbl.Text = beschriftung;
            lbl.Location = new Point(linksLabel, oben + 4);
            lbl.AutoSize = true;
            lbl.Font = new Font("Segoe UI", 9.75f, FontStyle.Regular);
            tabPage_Stromspeicher_Parameter.Controls.Add(lbl);

            ComboBox cb = new ComboBox();
            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            cb.Location = new Point(linksFeld, oben);
            cb.Size = new Size(177, SP_FELD_HOEHE);
            cb.Font = new Font("Segoe UI", 9.75f, FontStyle.Regular);
            cb.Items.AddRange(eintraege);
            tabPage_Stromspeicher_Parameter.Controls.Add(cb);

            return cb;
        }

        /// <summary>Schalter; <paramref name="bedienbar"/> false = sichtbar, aber ausgegraut.</summary>
        private CheckBox SpSchalterAnlegen(string beschriftung, int links, int oben, bool bedienbar)
        {
            CheckBox cb = new CheckBox();
            cb.Text = beschriftung;
            cb.Location = new Point(links, oben);
            cb.AutoSize = true;
            cb.Font = new Font("Segoe UI", 9.75f, FontStyle.Regular);
            cb.Enabled = bedienbar;
            tabPage_Stromspeicher_Parameter.Controls.Add(cb);
            return cb;
        }

        /// <summary>Persistenzwert des gewählten Listeneintrags (leer, wenn nichts gewählt ist).</summary>
        private static string SpGewaehlterWert(ComboBox cb)
        {
            SpAuswahl a = cb.SelectedItem as SpAuswahl;
            return a != null ? a.Wert : "";
        }

        /// <summary>
        /// Gibt den Kompatibilitätsschalter nur für die Berechnungsart frei, für die
        /// es überhaupt eine Excel-Vorlage gibt — die Dauernutzung (AP6).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Für die Nachtnutzung hinterlegte die V7-Mappe nur eine als
        /// Dauernutzungssimulation unbrauchbare Altversion; sie ist eine Neudefinition
        /// und ausdrücklich nicht Excel-verifizierbar (Fachkonzept 6.1). Die Engine
        /// lehnt die Kombination ab, die Oberfläche bietet sie deshalb erst gar nicht
        /// an.
        /// </para>
        /// <para>
        /// <b>Der gespeicherte Wert bleibt unangetastet.</b> Der Schalter wird nur
        /// gesperrt, nicht zurückgesetzt: Wer von der Nachtnutzung zur Dauernutzung
        /// zurückwechselt, findet seine Einstellung wieder vor. Eine Altvariante mit
        /// beidem rechnet der Controller mit Protokollhinweis energetisch.
        /// </para>
        /// </remarks>
        private void SpKompatibilitaetVerfuegbarkeit()
        {
            if (checkBox_SpKompatibilitaet == null) return;

            string berechnungsart = SpGewaehlterWert(comboBox_SpBerechnungsart);
            bool nurDauernutzung = berechnungsart == DbWerte.SP_BERECHNUNG_DAUERNUTZUNG;

            checkBox_SpKompatibilitaet.Enabled = _speicherVarianteVorhanden && nurDauernutzung;

            string hinweis;
            if (nurDauernutzung) hinweis = MyResource.Resource.SP_PARAM_HINWEIS_KOMPATIBILITAET;
            else if (berechnungsart == DbWerte.SP_BERECHNUNG_ARBITRAGE)
                hinweis = MyResource.Resource.ARB_HINWEIS_KOMPATIBILITAET;
            else hinweis = MyResource.Resource.NACHT_HINWEIS_KOMPATIBILITAET;

            tooltip.SetToolTip(checkBox_SpKompatibilitaet, hinweis);
        }

        /// <summary>Wählt den Eintrag mit dem angegebenen Persistenzwert aus.</summary>
        private static void SpWertWaehlen(ComboBox cb, string wert)
        {
            for (int i = 0; i < cb.Items.Count; i++)
            {
                SpAuswahl a = cb.Items[i] as SpAuswahl;
                if (a != null && a.Wert == wert) { cb.SelectedIndex = i; return; }
            }
            if (cb.Items.Count > 0) cb.SelectedIndex = 0;
        }

        /// <summary>
        /// Liest ein Zahlenfeld der Speicherseite beim Verlassen — Komma oder Punkt
        /// zulässig (<see cref="Program.ZahlParsen"/>, also der Eingabe des Anwenders in
        /// seiner Kultur folgend).
        /// </summary>
        /// <remarks>
        /// Bewusst OHNE die modale Meldung aus <see cref="Program.ZahlPruefen"/>: Die
        /// setzt Fokus und Auswahl zurück, und aus einem <c>Leave</c>-Ereignis heraus
        /// kann das dasselbe Ereignis erneut auslösen. Die Seite speichert beim
        /// Verlassen des Feldes, hat also keinen Übernehmen-Knopf, an dem gemeldet
        /// werden könnte. Rückmeldung gibt stattdessen die Einfärbung am
        /// <c>TextChanged</c> (<see cref="Program.ZahlFaerben"/>); ein unlesbares Feld
        /// behält den gespeicherten Wert.
        /// </remarks>
        private static bool SpZahl(TextBox feld, out double wert)
        {
            return Program.ZahlParsen(feld.Text, out wert);
        }

        /// <summary>Stellt den gespeicherten Wert eines Feldes wieder her.</summary>
        private void SpFeldZuruecksetzen(TextBox feld, double wert, string format)
        {
            bool vorher = _speicherFelderLaden;
            _speicherFelderLaden = true;
            feld.Text = wert.ToString(format, CultureInfo.CurrentCulture);
            Program.ZahlFaerben(feld);
            _speicherFelderLaden = vorher;
        }

        /// <summary>
        /// Übernimmt eine Änderung in die aktive Variante und schreibt sie zielgenau
        /// zurück (Muster <see cref="SpeichereKonfigurationsAenderung"/>).
        /// </summary>
        private void SpeichereVariantenAenderung(Action<StromspeicherVarianteModel> anpassungsAktion)
        {
            if (_speicherFelderLaden || _speicherVariante == null) return;

            try
            {
                anpassungsAktion(_speicherVariante);
                new StromspeicherVarianteCtrl().Update(_speicherVariante);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim automatischen Speichern der Speichervariante: " + ex.Message);
            }
        }

        /// <summary>
        /// Füllt die Parameterseite aus der aktiven Speichervariante des Projekts und
        /// aus den Gerätedaten. Ohne aktive Variante bleiben die Eingaben gesperrt.
        /// </summary>
        private void LeseSpeicherVariante()
        {
            if (comboBox_SpBetriebsart == null) return;   // Seite nicht aufgebaut

            try
            {
                _speicherVariante = new StromspeicherVarianteCtrl().ReadAktiveVariante(m_ID_Projekt);
            }
            catch (Exception ex)
            {
                _speicherVariante = null;
                Console.WriteLine("Die Speichervariante konnte nicht gelesen werden: " + ex.Message);
            }

            bool vorhanden = _speicherVariante != null;
            StromspeicherVarianteModel v = _speicherVariante ?? new StromspeicherVarianteModel();

            _speicherFelderLaden = true;
            try
            {
                CultureInfo kultur = CultureInfo.CurrentCulture;

                textBox_Stromspeicher_Ladeenergie_min.Text = v.SoC_Min_Prozent.ToString("F0", kultur);
                textBox_Stromspeicher_Ladeenergie_max.Text = v.SoC_Max_Prozent.ToString("F0", kultur);
                textBox_Speicher_Ladeschwelle.Text = v.Ladeschwellwert.ToString("F2", kultur);

                // Gerätedaten der Einheit, die auch gerechnet wird (Abnahmebefund 1).
                double kapazitaetKwh, leistungKw;
                SpGeraetedaten(out kapazitaetKwh, out leistungKw);
                textBox_Stromspeicher_Ladeleistung_max.Text = leistungKw.ToString("F2", kultur);
                if (textBox_SpKapazitaet != null)
                    textBox_SpKapazitaet.Text = kapazitaetKwh.ToString("F2", kultur);

                SpWertWaehlen(comboBox_SpBetriebsart, v.Betriebsart);
                SpWertWaehlen(comboBox_SpBerechnungsart, v.Berechnungsart);
                SpWertWaehlen(comboBox_SpPreisquelle, v.Preisquelle);
                checkBox_SpAufschlag.Checked = v.Aufschlag_Anwenden;

                checkBox_SpPV.Checked = v.PV_Zulaessig;
                checkBox_SpBHKW.Checked = v.BHKW_Ueberschuss_Zulaessig;
                checkBox_SpBHKWStromgefuehrt.Checked = v.BHKW_Stromgefuehrt;
                checkBox_SpNetzentladung.Checked = v.Netzentladung;
                checkBox_SpKompatibilitaet.Checked = v.Kompatibilitaetsmodus;

                textBox_SpKapitalzins.Text = v.Kapitalzins.ToString("F2", kultur);
                textBox_SpNutzungsdauer.Text = v.Nutzungsdauer.ToString("F0", kultur);
                textBox_SpLeistungspreis.Text = v.L_P.ToString("F2", kultur);
                textBox_SpNetzladeaufschlag.Text = v.A_Netzlade.ToString("F2", kultur);

                label_SpVariantenstatus.Text = vorhanden
                    ? string.Format(MyResource.Resource.SP_PARAM_STATUS_VARIANTE, SpVariantenname(v))
                    : MyResource.Resource.SP_PARAM_STATUS_KEINE_VARIANTE;
                label_SpVariantenstatus.ForeColor = vorhanden ? Color.Black : Color.Firebrick;

                // Ohne aktive Variante gäbe es kein Ziel für das Zurückschreiben - dann
                // wären die Felder Attrappen. Der Ausbaustufen-Schalter
                // "BHKW stromgeführt" bleibt in jedem Fall gesperrt.
                textBox_Stromspeicher_Ladeenergie_min.Enabled = vorhanden;
                textBox_Stromspeicher_Ladeenergie_max.Enabled = vorhanden;
                textBox_Speicher_Ladeschwelle.Enabled = vorhanden;
                comboBox_SpBetriebsart.Enabled = vorhanden;
                comboBox_SpBerechnungsart.Enabled = vorhanden;
                checkBox_SpPV.Enabled = vorhanden;
                checkBox_SpBHKW.Enabled = vorhanden;
                checkBox_SpNetzentladung.Enabled = vorhanden;
                textBox_SpKapitalzins.Enabled = vorhanden;
                textBox_SpNutzungsdauer.Enabled = vorhanden;
                textBox_SpLeistungspreis.Enabled = vorhanden;
                textBox_SpNetzladeaufschlag.Enabled = vorhanden;

                comboBox_SpPreisquelle.Enabled = vorhanden;
                checkBox_SpAufschlag.Enabled = vorhanden;

                _speicherVarianteVorhanden = vorhanden;
                SpKompatibilitaetVerfuegbarkeit();

                // Erst NACH den Prozent- und Kapazitätsfeldern: Das Äquivalent liest
                // beide. Am TextChanged hängt es zwar auch, dort ist es aber durch
                // _speicherFelderLaden nicht gesperrt und die Kapazität stünde beim
                // ersten der beiden Felder noch nicht.
                SpSoCAequivalenteAktualisieren();
            }
            finally
            {
                _speicherFelderLaden = false;
            }

            // Erst NACH dem Freigeben der Felder: Die Reihenliste liest die Datenbank
            // und aktualisiert die Preisvorschau - beides braucht die fertig gefüllte
            // Variante, und die Auswahl darf dabei kein UPDATE auslösen.
            SpReihenlisteFuellen();
        }

        /// <summary>
        /// Anzeigename der Variante: der Bezeichner der zugehörigen Anlagenzeile, sonst
        /// deren ID. Die Variantentabelle selbst führt keinen Namen — der Name gehört
        /// zur Anlage (Fachkonzept 7.3).
        /// </summary>
        private static string SpVariantenname(StromspeicherVarianteModel v)
        {
            try
            {
                object wert = DataRepository.ExecuteScalar(
                    "SELECT Bezeichner FROM Tab_Energieanlagen WHERE ID = ?",
                    new System.Data.OleDb.OleDbParameter("@id", v.ID_Energieanlage));
                if (wert != null && wert != DBNull.Value && wert.ToString().Length > 0)
                    return wert.ToString();
            }
            catch { /* Anzeigename ist Beiwerk - der Status erscheint auch ohne ihn */ }

            return v.ID_Energieanlage.ToString(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Kapazität [kWh] und Lade-/Entladeleistung [kW] der Einheit, die auch
        /// GERECHNET wird — dieselbe Auswahlregel wie
        /// <see cref="StromspeicherSimCtrl.LeseParameter(int)"/> (Fachkonzept 7.3).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Bis zum Abnahmebefund 1 summierte die Seite hier über ALLE
        /// <c>SP_TYP</c>-Anlagen des Projekts. Seit AP9b rechnet die Simulation aber die
        /// Anlagenzeile der aktiven Variante, nicht deren Summe — bei mehreren Varianten
        /// zeigte die Parameterseite damit eine Leistung, mit der nie jemand gerechnet
        /// hat (Projekt 1011 der Produktiv-DB: vier Speicherzeilen, angezeigt wurden
        /// 43,9 kW statt der 11,04 kW der aktiven Variante).
        /// </para>
        /// <para>
        /// Der Rückfall auf die Aggregation bleibt genau dort, wo ihn auch der Controller
        /// nimmt: wenn sich keine aktive Variantenzeile bestimmen lässt (Altprojekt vor
        /// Migrationsschritt 11d oder eine Variante, die auf keine Speicheranlage dieses
        /// Projekts mehr zeigt). Der Hinweistext am Feld nennt beide Fälle.
        /// </para>
        /// </remarks>
        private void SpGeraetedaten(out double kapazitaetKwh, out double leistungKw)
        {
            kapazitaetKwh = 0.0;
            leistungKw = 0.0;

            try
            {
                string sql =
                    "SELECT SUM(sp.Energie) AS C, SUM(sp.Leistung) AS P " +
                    "FROM Tab_Energieanlagen AS a " +
                    "INNER JOIN Tab_Stromspeicher AS sp ON a.ID_SP = sp.ID " +
                    "WHERE a.ID_Projekt = ? AND a.ID_Type = ?";

                var parameter = new System.Collections.Generic.List<System.Data.OleDb.OleDbParameter>
                {
                    new System.Data.OleDb.OleDbParameter("@proj", m_ID_Projekt),
                    new System.Data.OleDb.OleDbParameter("@typ", WizardItemClass.SP_TYP)
                };

                // Die Anlage der aktiven Variante, sofern sie eine Speicheranlage dieses
                // Projekts ist - die WHERE-Bedingung oben prüft das gleich mit.
                if (_speicherVariante != null && _speicherVariante.ID_Energieanlage > 0)
                {
                    sql += " AND a.ID = ?";
                    parameter.Add(new System.Data.OleDb.OleDbParameter(
                                      "@anlage", _speicherVariante.ID_Energieanlage));
                }

                System.Data.DataTable dt = DataRepository.GetDataTable(sql, parameter.ToArray());
                if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["C"] != DBNull.Value)
                {
                    kapazitaetKwh = Convert.ToDouble(dt.Rows[0]["C"]);
                    if (dt.Rows[0]["P"] != DBNull.Value) leistungKw = Convert.ToDouble(dt.Rows[0]["P"]);
                    return;
                }

                // Rückfall: Die aktive Variante zeigt ins Leere - dann gilt wieder die
                // Aggregation über alle Speicheranlagen (Verhalten bis AP9a).
                if (parameter.Count > 2)
                {
                    dt = DataRepository.GetDataTable(
                        "SELECT SUM(sp.Energie) AS C, SUM(sp.Leistung) AS P " +
                        "FROM Tab_Energieanlagen AS a " +
                        "INNER JOIN Tab_Stromspeicher AS sp ON a.ID_SP = sp.ID " +
                        "WHERE a.ID_Projekt = ? AND a.ID_Type = ?",
                        parameter[0], parameter[1]);

                    if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["C"] != DBNull.Value)
                    {
                        kapazitaetKwh = Convert.ToDouble(dt.Rows[0]["C"]);
                        if (dt.Rows[0]["P"] != DBNull.Value) leistungKw = Convert.ToDouble(dt.Rows[0]["P"]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Gerätedaten des Speichers konnten nicht gelesen werden: " + ex.Message);
            }
        }

        // ====================================================================
        //  Ergebnisseite Stromspeicher (AP3b, Fachkonzept 7.1/7.2)
        // ====================================================================
        //
        // AUSGANGSLAGE. tabPage_Stromspeicher war leer (Designer.cs:2739-2743) - die
        // Seite erschien in der Navigation samt Batterie-Icon und zeigte nichts.
        //
        // AUFBAU wie auf den Nachbarseiten: Diagramm LINKS, Kennzahlen RECHTS, darunter
        // die CSV-Ausgabe. Programmatisch aus demselben Grund wie die Parameterseite
        // (das Formular arbeitet durchgängig mit resources.ApplyResources).
        //
        // DER KENNZAHLENBLOCK ist eine ListView mit drei Gruppen (Energie, Speicher,
        // Wirtschaft) statt drei Dutzend Beschriftungspaaren: Er bleibt dadurch
        // erweiterbar, scrollt von selbst und braucht keine Positionsrechnerei. Die
        // Werte kommen aus GENAU DER Abbildung, die auch gespeichert wird
        // (StromspeicherSimCtrl.AlsErgebnismodell) - Bildschirm und Datenbank können
        // nicht auseinanderlaufen.
        //
        // DAS SOC-DIAGRAMM setzt MaxXVALUE UND MitViertelStunde (Vorbild
        // NavigatorStrom): Ohne beides kappt ChartManager.AddSeries die Reihe auf 8.760
        // Punkte, und der Jahresgang bräche Ende März ab. Eine Sekundärachse braucht
        // die Seite nicht - anders als im PV-Diagramm ist der Ladezustand hier die
        // Primärgröße.

        // ====================================================================
        //  SEITENAUFTEILUNG (Abnahmebefund „nur die Spalte Kennzahl ist sichtbar")
        // ====================================================================
        //
        // AUSGANGSLAGE. Alle sieben Steuerelemente der Seite lagen auf FESTEN
        // Koordinaten OHNE Verankerung: das Diagramm bei x = 16..656, die
        // Kennzahlenliste bei x = 676..1276, die Ampelzeile darunter. Das passte zur
        // ENTWURFSBREITE des aufnehmenden Panels (splitContainer_Parameter.Panel2,
        // rund 1266 px), nicht aber zu dem, was der Anwender davon SIEHT: Das
        // Formular führt seinen Reiterblock in fester Größe (tabControl_Simulation,
        // 1456 px, ohne Anker) und rollt bei kleineren Fenstern über AutoScroll.
        // Gemessen bei 1280 x 800: von den 1266 px des Panels sind 1065 px sichtbar -
        // die Liste endete 211 px, ihre Einheitenspalte 173 px hinter dem rechten
        // Fensterrand. Sichtbar blieb allein die Spalte „Kennzahl".
        //
        // UMBAU. Die Seite trägt jetzt EIN Steuerelement, das Raster
        // <see cref="tabelle_SpeicherSeite"/>. Es teilt die Fläche in eine wachsende
        // Diagrammspalte und eine Kennzahlenspalte fester Breite; die Höhe verteilen
        // Zeilenstile statt Pixelabstände. Zwei Eigenschaften des Rasters tragen den
        // Befund:
        //
        //   1. Die Kennzahlenspalte ist ABSOLUT bemaßt, die Diagrammspalte
        //      prozentual. Ein TableLayoutPanel bedient absolute Spalten zuerst - bei
        //      Platzmangel weicht das Diagramm, nie die Liste (Vorrangregel des
        //      Befunds).
        //   2. Das Raster wird nicht auf die GRÖSSE des aufnehmenden Panels gelegt,
        //      sondern auf dessen SICHTBAREN Ausschnitt (SpSeiteEinpassen). Solange
        //      das Formular seinen Reiterblock fest führt, ist genau das der
        //      Unterschied zwischen „steht da" und „ist zu sehen".
        //
        // Unterhalb von SP_ERG_SEITE_MINBREITE hört das Einpassen auf; dann rollt
        // Panel2 (AutoScroll ist dort gesetzt), statt die Liste zu stauchen.
        private const int SP_ERG_RAND = 16;

        // Breite der Kennzahlenspalte. Die vier Spalten der Liste ergeben in BEIDEN
        // Zuständen 560 px (siehe SpVergleichsspalteSetzen); dazu kommen die
        // senkrechte Bildlaufleiste und der Rahmen der ListView.
        private const int SP_ERG_LISTE_BREITE = 584;

        // Kleinste noch brauchbare Diagrammbreite und der Abstand zwischen den Spalten.
        private const int SP_ERG_CHART_MINBREITE = 300;
        private const int SP_ERG_SPALTENABSTAND = 12;

        // Mindestmaße der ganzen Seite - darunter übernimmt der Bildlauf von Panel2.
        private const int SP_ERG_SEITE_MINBREITE =
            2 * SP_ERG_RAND + SP_ERG_LISTE_BREITE + SP_ERG_SPALTENABSTAND + SP_ERG_CHART_MINBREITE;
        private const int SP_ERG_SEITE_MINHOEHE = 420;

        // Feste Zeilenhöhen der Nebenzeilen (Knöpfe, Warnzeile, Zyklenampel).
        private const int SP_ERG_ZEILE_KNOEPFE = 40;
        private const int SP_ERG_ZEILE_HINWEIS = 46;

        // Maße einer Kachel des Kernblocks „Wesentliche Daten".
        private const int SP_ERG_KACHEL_BREITE = 176;
        private const int SP_ERG_KACHEL_HOEHE = 38;
        private const int SP_ERG_KACHEL_TITEL = 15;
        private const int SP_ERG_KERN_GRUPPE_BREITE = 132;

        // Anfangshöhe des Kernblocks (zwei Gruppen zu je einer Kachelreihe). Die
        // gültige Höhe rechnet SpKernblockEinpassen aus dem tatsächlichen Umbruch.
        private const int SP_ERG_KERN_HOEHE_START = 2 * (SP_ERG_KACHEL_HOEHE + 4) + 8;

        // Spaltenbreiten des Kennzahlenblocks. Die Summe bleibt in beiden Zuständen
        // bei 560 px und damit innerhalb der Listenbreite - die Vergleichsspalte
        // (AP6) wird nicht angehängt, sondern aus den drei Bestandsspalten
        // freigeräumt. Eine ListView kann Spalten nicht ausblenden; "nicht vorhanden"
        // heißt hier deshalb Breite 0.
        private const int SP_ERG_SP_KENNZAHL = 300;
        private const int SP_ERG_SP_WERT = 150;
        private const int SP_ERG_SP_EINHEIT = 110;
        private const int SP_ERG_SP_KENNZAHL_VGL = 230;
        private const int SP_ERG_SP_WERT_VGL = 130;
        private const int SP_ERG_SP_VERGLEICH_VGL = 130;
        private const int SP_ERG_SP_EINHEIT_VGL = 70;

        // Spaltenindizes des Kennzahlenblocks.
        private const int SP_ERG_IDX_WERT = 1;
        private const int SP_ERG_IDX_VERGLEICH = 2;
        private const int SP_ERG_IDX_EINHEIT = 3;

        private System.Windows.Forms.DataVisualization.Charting.Chart chart_Speicher;
        private ChartManager _chartSpeicherManager;
        private ListView listView_SpeicherKennzahlen;
        private Label label_SpeicherStatus;
        private Label label_SpeicherAmpel;

        /// <summary>Grundraster der Seite - das EINZIGE Kind der TabPage.</summary>
        private TableLayoutPanel tabelle_SpeicherSeite;

        /// <summary>Diagrammspalte (links) und Kennzahlenspalte (rechts).</summary>
        private TableLayoutPanel tabelle_SpeicherDiagramm;
        private TableLayoutPanel tabelle_SpeicherKennzahlen;

        /// <summary>Kernblock „Wesentliche Daten" zwischen Kopfzeile und Diagramm.</summary>
        private Panel panel_SpKernblock;
        private FlowLayoutPanel flow_SpKernAnlage;
        private FlowLayoutPanel flow_SpKernErgebnis;

        /// <summary>Wertfelder des Kernblocks, Schlüssel siehe <c>SPK_*</c>.</summary>
        private readonly Dictionary<string, Label> _spKernwerte = new Dictionary<string, Label>();

        // Schlüssel der Kernblock-Kacheln. Sie stehen als Konstanten da, weil sie
        // sowohl den Aufbau (SpKachel) als auch das Füllen (SpKernblockFuellen)
        // adressieren - ein Tippfehler soll den Übersetzer stören, nicht den Anwender.
        private const string SPK_KAPAZITAET = "Kapazitaet";
        private const string SPK_LEISTUNG = "Leistung";
        private const string SPK_SOC_PROZENT = "SoCProzent";
        private const string SPK_SOC_KWH = "SoCKwh";
        private const string SPK_BETRIEBSART = "Betriebsart";
        private const string SPK_BERECHNUNGSART = "Berechnungsart";
        private const string SPK_ERTRAG = "Ertrag";
        private const string SPK_UEBERSCHUSS = "Ueberschuss";
        private const string SPK_AMORTISATION = "Amortisation";
        private const string SPK_VOLLZYKLEN = "Vollzyklen";
        private const string SPK_EIGENVERBRAUCH = "Eigenverbrauch";
        private const string SPK_AUTARKIE = "Autarkie";

        /// <summary>
        /// Warnzeile „dieser Lauf enthält keine Erzeugung" (Abnahmebefund 2). Sie steht
        /// unter den Ausgabeknöpfen und ist nur belegt, wenn sie etwas zu sagen hat.
        /// </summary>
        private Label label_SpeicherErzeugungshinweis;
        private Button btn_CsvExportSpeicher;
        private Button btn_SpVariantenVergleich;

        private void InitStromspeicherSeite()
        {
            if (tabPage_Stromspeicher == null) return;

            // --- Grundraster: Kopfzeile, Kernblock, darunter Diagramm | Kennzahlen ---
            tabelle_SpeicherSeite = new TableLayoutPanel();
            tabelle_SpeicherSeite.Name = "tabelle_SpeicherSeite";
            tabelle_SpeicherSeite.ColumnCount = 2;
            tabelle_SpeicherSeite.RowCount = 3;
            tabelle_SpeicherSeite.Padding = new Padding(SP_ERG_RAND, 12, SP_ERG_RAND, 8);
            tabelle_SpeicherSeite.MinimumSize = new Size(SP_ERG_SEITE_MINBREITE, SP_ERG_SEITE_MINHOEHE);
            // Kein Dock: Die Seite wird auf den SICHTBAREN Ausschnitt eingepasst, nicht
            // auf die (breitere) Größe des aufnehmenden Panels - siehe SpSeiteEinpassen.
            tabelle_SpeicherSeite.Dock = DockStyle.None;
            tabelle_SpeicherSeite.Location = new Point(0, 0);
            tabelle_SpeicherSeite.Size = new Size(SP_ERG_SEITE_MINBREITE, SP_ERG_SEITE_MINHOEHE);
            // Die absolute Spalte wird zuerst bedient: Bei Platzmangel weicht das
            // Diagramm, nie die Kennzahlenliste.
            tabelle_SpeicherSeite.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tabelle_SpeicherSeite.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SP_ERG_LISTE_BREITE));
            tabelle_SpeicherSeite.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // Kopfzeile
            // Der Kernblock bekommt seine Höhe gerechnet statt gemessen - eine
            // selbstmessende Zeile über umbrechenden Kacheln misst ohne
            // Breitenvorgabe (siehe SpKernGruppe/SpKernblockEinpassen).
            tabelle_SpeicherSeite.RowStyles.Add(new RowStyle(SizeType.Absolute, SP_ERG_KERN_HOEHE_START));
            tabelle_SpeicherSeite.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            label_SpeicherStatus = new Label();
            label_SpeicherStatus.Name = "label_SpeicherStatus";
            label_SpeicherStatus.Text = MyResource.Resource.SP_ERG_KEIN_LAUF;
            label_SpeicherStatus.AutoSize = false;
            label_SpeicherStatus.Dock = DockStyle.Fill;
            label_SpeicherStatus.Height = 24;
            label_SpeicherStatus.Margin = new Padding(0, 0, 0, 2);
            label_SpeicherStatus.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            tabelle_SpeicherSeite.Controls.Add(label_SpeicherStatus, 0, 0);
            tabelle_SpeicherSeite.SetColumnSpan(label_SpeicherStatus, 2);

            panel_SpKernblock = SpKernblockAufbauen();
            tabelle_SpeicherSeite.Controls.Add(panel_SpKernblock, 0, 1);
            tabelle_SpeicherSeite.SetColumnSpan(panel_SpKernblock, 2);

            tabelle_SpeicherSeite.Controls.Add(SpDiagrammspalteAufbauen(), 0, 2);
            tabelle_SpeicherSeite.Controls.Add(SpKennzahlenspalteAufbauen(), 1, 2);

            tabPage_Stromspeicher.Controls.Add(tabelle_SpeicherSeite);

            // Das Raster wandert zur Laufzeit samt Seite in splitContainer_Parameter.Panel2
            // (siehe listViewQuellen_SelectedIndexChanged) und muss sich dort neu
            // einpassen; dasselbe gilt bei jeder Größenänderung des Formulars und beim
            // Verschieben des Menü-Splitters.
            tabelle_SpeicherSeite.ParentChanged += (s, e) => SpSeiteEinpassen();
            tabelle_SpeicherSeite.VisibleChanged += (s, e) => SpSeiteEinpassen();
            this.ClientSizeChanged += (s, e) => SpSeiteEinpassen();
            if (splitContainer_Parameter != null)
                splitContainer_Parameter.SplitterMoved += (s, e) => SpSeiteEinpassen();
        }

        /// <summary>
        /// Linke Spalte: SoC-Diagramm, darunter die Ausgabeknöpfe und die Warnzeile.
        /// </summary>
        private Control SpDiagrammspalteAufbauen()
        {
            TableLayoutPanel spalte = new TableLayoutPanel();
            spalte.Name = "tabelle_SpeicherDiagramm";
            spalte.Dock = DockStyle.Fill;
            spalte.Margin = new Padding(0, 0, SP_ERG_SPALTENABSTAND, 0);
            spalte.ColumnCount = 1;
            spalte.RowCount = 3;
            spalte.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            spalte.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            spalte.RowStyles.Add(new RowStyle(SizeType.Absolute, SP_ERG_ZEILE_KNOEPFE));
            spalte.RowStyles.Add(new RowStyle(SizeType.Absolute, SP_ERG_ZEILE_HINWEIS));

            chart_Speicher = new System.Windows.Forms.DataVisualization.Charting.Chart();
            chart_Speicher.Name = "chart_Speicher";
            // Ein programmatisch erzeugtes Chart hat KEINE ChartArea - ChartManager.Init
            // steigt ohne sie wortlos aus (siehe InitKesselChart).
            chart_Speicher.ChartAreas.Add(new ChartArea("ChartArea_Speicher"));
            chart_Speicher.BackColor = Color.WhiteSmoke;
            chart_Speicher.BorderlineColor = Color.Transparent;
            chart_Speicher.Dock = DockStyle.Fill;
            chart_Speicher.Margin = new Padding(0);
            chart_Speicher.Visible = false;              // erst nach einem Speicherlauf
            spalte.Controls.Add(chart_Speicher, 0, 0);

            FlowLayoutPanel knoepfe = new FlowLayoutPanel();
            knoepfe.Name = "flow_SpeicherKnoepfe";
            knoepfe.Dock = DockStyle.Fill;
            knoepfe.FlowDirection = FlowDirection.LeftToRight;
            knoepfe.WrapContents = false;
            knoepfe.Margin = new Padding(0, 6, 0, 0);
            knoepfe.Padding = new Padding(0);

            btn_CsvExportSpeicher = new Button();
            btn_CsvExportSpeicher.Name = "btn_CsvExportSpeicher";
            btn_CsvExportSpeicher.Text = MyResource.Resource.SIM_BTN_CSV_EXPORT;
            btn_CsvExportSpeicher.Size = new Size(150, 32);
            btn_CsvExportSpeicher.Margin = new Padding(0, 0, 12, 0);
            btn_CsvExportSpeicher.BackColor = SystemColors.Control;
            btn_CsvExportSpeicher.ForeColor = Color.Black;
            btn_CsvExportSpeicher.UseVisualStyleBackColor = false;
            btn_CsvExportSpeicher.Visible = false;
            btn_CsvExportSpeicher.Click += btn_CsvExportSpeicher_Click;
            tooltip.SetToolTip(btn_CsvExportSpeicher, MyResource.Resource.SP_TOOLTIP_CSV);
            knoepfe.Controls.Add(btn_CsvExportSpeicher);

            // AP9: Einstieg in den Variantenvergleich (Fachkonzept 7.3). Er steht HIER
            // und nicht am Kontextmenü der Übersicht, weil er genau eines braucht, was
            // es nur auf dieser Seite gibt: das fertig gerechnete sim-Objekt, auf dem
            // StromspeicherSimCtrl.RechneVariante je Variante läuft. Sichtbar wird der
            // Knopf erst nach einem Lauf und nur, wenn es überhaupt etwas zu vergleichen
            // gibt (mehr als eine Speicheranlage) - siehe SpeicherErgebnisAnzeigen.
            btn_SpVariantenVergleich = new Button();
            btn_SpVariantenVergleich.Name = "btn_SpVariantenVergleich";
            btn_SpVariantenVergleich.Text = MyResource.Resource.VAR_VGL_BTN_OEFFNEN;
            btn_SpVariantenVergleich.Size = new Size(200, 32);
            btn_SpVariantenVergleich.Margin = new Padding(0);
            btn_SpVariantenVergleich.BackColor = SystemColors.Control;
            btn_SpVariantenVergleich.ForeColor = Color.Black;
            btn_SpVariantenVergleich.UseVisualStyleBackColor = false;
            btn_SpVariantenVergleich.Visible = false;
            btn_SpVariantenVergleich.Click += btn_SpVariantenVergleich_Click;
            tooltip.SetToolTip(btn_SpVariantenVergleich, MyResource.Resource.VAR_VGL_TOOLTIP_OEFFNEN);
            knoepfe.Controls.Add(btn_SpVariantenVergleich);

            spalte.Controls.Add(knoepfe, 0, 1);

            // Abnahmebefund 2: Ein Lauf ohne jede Erzeugung sagt es hier im Klartext -
            // sonst liest sich die 0-%-Eigenverbrauchsquote wie ein Rechenfehler.
            label_SpeicherErzeugungshinweis = new Label();
            label_SpeicherErzeugungshinweis.Name = "label_SpeicherErzeugungshinweis";
            label_SpeicherErzeugungshinweis.AutoSize = false;
            label_SpeicherErzeugungshinweis.Dock = DockStyle.Fill;
            label_SpeicherErzeugungshinweis.Margin = new Padding(0, 2, 0, 0);
            label_SpeicherErzeugungshinweis.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            label_SpeicherErzeugungshinweis.ForeColor = Color.Firebrick;
            spalte.Controls.Add(label_SpeicherErzeugungshinweis, 0, 2);

            tabelle_SpeicherDiagramm = spalte;
            return spalte;
        }

        /// <summary>
        /// Rechte Spalte: Kennzahlenliste, darunter die Zyklenampel. Ihre Breite ist
        /// im Grundraster absolut gesetzt (<see cref="SP_ERG_LISTE_BREITE"/>) - die
        /// vier Spalten der Liste müssen vollständig sichtbar bleiben.
        /// </summary>
        private Control SpKennzahlenspalteAufbauen()
        {
            TableLayoutPanel spalte = new TableLayoutPanel();
            spalte.Name = "tabelle_SpeicherKennzahlen";
            spalte.Dock = DockStyle.Fill;
            spalte.Margin = new Padding(0);
            spalte.ColumnCount = 1;
            spalte.RowCount = 2;
            spalte.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            spalte.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            spalte.RowStyles.Add(new RowStyle(SizeType.Absolute, SP_ERG_ZEILE_HINWEIS));

            listView_SpeicherKennzahlen = new ListView();
            listView_SpeicherKennzahlen.Name = "listView_SpeicherKennzahlen";
            listView_SpeicherKennzahlen.View = View.Details;
            listView_SpeicherKennzahlen.FullRowSelect = true;
            listView_SpeicherKennzahlen.GridLines = true;
            listView_SpeicherKennzahlen.MultiSelect = false;
            listView_SpeicherKennzahlen.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            listView_SpeicherKennzahlen.ShowGroups = true;
            listView_SpeicherKennzahlen.Font = new Font("Segoe UI", 9.75f, FontStyle.Regular);
            listView_SpeicherKennzahlen.Dock = DockStyle.Fill;
            listView_SpeicherKennzahlen.Margin = new Padding(0);
            listView_SpeicherKennzahlen.Columns.Add(MyResource.Resource.SP_ERG_SPALTE_KENNZAHL, SP_ERG_SP_KENNZAHL, HorizontalAlignment.Left);
            listView_SpeicherKennzahlen.Columns.Add(MyResource.Resource.SP_ERG_SPALTE_WERT, SP_ERG_SP_WERT, HorizontalAlignment.Right);
            // Vergleichsspalte (AP6): steht immer im Spaltensatz, ist aber ohne
            // Vergleichslauf 0 px breit - siehe SpVergleichsspalteSetzen.
            listView_SpeicherKennzahlen.Columns.Add(MyResource.Resource.NACHT_ERG_SPALTE_VERGLEICH, 0, HorizontalAlignment.Right);
            listView_SpeicherKennzahlen.Columns.Add(MyResource.Resource.SP_ERG_SPALTE_EINHEIT, SP_ERG_SP_EINHEIT, HorizontalAlignment.Left);
            listView_SpeicherKennzahlen.Groups.Add(new ListViewGroup("ENERGIE", MyResource.Resource.SP_ERG_GRUPPE_ENERGIE));
            listView_SpeicherKennzahlen.Groups.Add(new ListViewGroup("SPEICHER", MyResource.Resource.SP_ERG_GRUPPE_SPEICHER));
            listView_SpeicherKennzahlen.Groups.Add(new ListViewGroup("WIRTSCHAFT", MyResource.Resource.SP_ERG_GRUPPE_WIRTSCHAFT));
            spalte.Controls.Add(listView_SpeicherKennzahlen, 0, 0);

            label_SpeicherAmpel = new Label();
            label_SpeicherAmpel.Name = "label_SpeicherAmpel";
            label_SpeicherAmpel.AutoSize = false;
            label_SpeicherAmpel.Dock = DockStyle.Fill;
            label_SpeicherAmpel.Margin = new Padding(0, 2, 0, 0);
            label_SpeicherAmpel.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            spalte.Controls.Add(label_SpeicherAmpel, 0, 1);

            tabelle_SpeicherKennzahlen = spalte;
            return spalte;
        }

        // ====================================================================
        //  Kernblock „Wesentliche Daten"
        // ====================================================================
        //
        // Der Anwender fragte nach den WESENTLICHEN Daten des Speichers. Die
        // Kennzahlenliste führt knapp vierzig Zeilen in drei Gruppen - vollständig,
        // aber nicht auf einen Blick lesbar. Der Block darüber greift daraus zwölf
        // Größen heraus: links, was gerechnet WURDE (Anlage und Variante), rechts
        // daneben, was dabei HERAUSKAM.
        //
        // KEINE ZWEITE RECHENWELT: Jede Kachel liest aus derselben Quelle wie die
        // Liste - dem Ergebnismodell (StromspeicherSimCtrl.AlsErgebnismodell), dem
        // Engine-Ergebnis und dem Laufkontext. Gefüllt wird im selben Zug wie die
        // Liste (SpeicherErgebnisAnzeigen); ohne Lauf räumt SpKernblockLeeren.
        //
        // Die Kacheln liegen in FlowLayoutPanels: Wird die Seite schmal, brechen sie
        // um, statt abgeschnitten zu werden.

        private Panel SpKernblockAufbauen()
        {
            Panel block = new Panel();
            block.Name = "panel_SpKernblock";
            block.Dock = DockStyle.Fill;
            block.Margin = new Padding(0, 4, 0, 8);
            block.Padding = new Padding(8, 4, 8, 4);
            block.BackColor = Color.FromArgb(246, 248, 250);

            flow_SpKernAnlage = SpKernGruppe("flow_SpKernAnlage",
                                             MyResource.Resource.SP_ERG_KERN_GRUPPE_ANLAGE);
            flow_SpKernAnlage.Controls.Add(SpKachel(SPK_KAPAZITAET, MyResource.Resource.SP_ERG_KERN_KAPAZITAET));
            flow_SpKernAnlage.Controls.Add(SpKachel(SPK_LEISTUNG, MyResource.Resource.SP_ERG_KERN_LEISTUNG));
            flow_SpKernAnlage.Controls.Add(SpKachel(SPK_SOC_PROZENT, MyResource.Resource.SP_ERG_KERN_SOC_PROZENT));
            flow_SpKernAnlage.Controls.Add(SpKachel(SPK_SOC_KWH, MyResource.Resource.SP_ERG_KERN_SOC_KWH));
            flow_SpKernAnlage.Controls.Add(SpKachel(SPK_BETRIEBSART, MyResource.Resource.SP_ERG_KERN_BETRIEBSART));
            flow_SpKernAnlage.Controls.Add(SpKachel(SPK_BERECHNUNGSART, MyResource.Resource.SP_ERG_KERN_BERECHNUNGSART));

            flow_SpKernErgebnis = SpKernGruppe("flow_SpKernErgebnis",
                                               MyResource.Resource.SP_ERG_KERN_GRUPPE_ERGEBNIS);
            flow_SpKernErgebnis.Controls.Add(SpKachel(SPK_ERTRAG, MyResource.Resource.SP_ERG_KERN_ERTRAG));
            flow_SpKernErgebnis.Controls.Add(SpKachel(SPK_UEBERSCHUSS, MyResource.Resource.SP_ERG_KERN_UEBERSCHUSS));
            flow_SpKernErgebnis.Controls.Add(SpKachel(SPK_AMORTISATION, MyResource.Resource.SP_ERG_KERN_AMORTISATION));
            flow_SpKernErgebnis.Controls.Add(SpKachel(SPK_VOLLZYKLEN, MyResource.Resource.SP_ERG_KERN_VOLLZYKLEN));
            flow_SpKernErgebnis.Controls.Add(SpKachel(SPK_EIGENVERBRAUCH, MyResource.Resource.SP_ERG_KERN_EIGENVERBRAUCH));
            flow_SpKernErgebnis.Controls.Add(SpKachel(SPK_AUTARKIE, MyResource.Resource.SP_ERG_KERN_AUTARKIE));

            // Reihenfolge beachten: Bei Dock = Top liegt das ZULETZT hinzugefügte
            // Steuerelement oben. Die Anlagendaten stehen über den Ergebnissen.
            block.Controls.Add(flow_SpKernErgebnis);
            block.Controls.Add(flow_SpKernAnlage);

            // Die Zeilenhöhe des Blocks im Grundraster hängt davon ab, wie viele
            // Kachelreihen die aktuelle Breite trägt - siehe SpKernblockEinpassen.
            flow_SpKernAnlage.SizeChanged += (s, e) => SpKernblockEinpassen();
            flow_SpKernErgebnis.SizeChanged += (s, e) => SpKernblockEinpassen();

            return block;
        }

        /// <summary>
        /// Eine Zeile des Kernblocks: Gruppenname links, danach die Kacheln.
        /// </summary>
        /// <remarks>
        /// <c>Dock = Top</c> zusammen mit <c>AutoSize</c> ist das tragfähige Gespann:
        /// Die Breite kommt vom Behälter, die Höhe ergibt sich aus dem Umbruch. Ein
        /// <c>Dock = Fill</c> in einer selbstmessenden Zeile misst dagegen ohne
        /// Breitenvorgabe - dann bricht jede Kachel in eine eigene Zeile um, und der
        /// Block frisst die halbe Seite.
        /// </remarks>
        private FlowLayoutPanel SpKernGruppe(string name, string beschriftung)
        {
            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.Name = name;
            flow.Dock = DockStyle.Top;
            flow.AutoSize = true;
            flow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flow.FlowDirection = FlowDirection.LeftToRight;
            flow.WrapContents = true;
            flow.Margin = new Padding(0);
            flow.Padding = new Padding(0);

            Label kopf = new Label();
            kopf.Name = name + "_Kopf";
            kopf.Text = beschriftung;
            kopf.AutoSize = false;
            kopf.Size = new Size(SP_ERG_KERN_GRUPPE_BREITE, SP_ERG_KACHEL_HOEHE);
            kopf.Margin = new Padding(0, 2, 8, 2);
            kopf.TextAlign = ContentAlignment.MiddleLeft;
            kopf.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            kopf.ForeColor = Color.FromArgb(0, 92, 140);
            flow.Controls.Add(kopf);

            return flow;
        }

        /// <summary>
        /// Eine Kachel: Beschriftung (klein, grau) über dem Wert (groß, fett). Das
        /// Wertfeld wird unter <paramref name="schluessel"/> gemerkt und von
        /// <see cref="SpKernwert"/> beschrieben.
        /// </summary>
        private Panel SpKachel(string schluessel, string beschriftung)
        {
            Panel kachel = new Panel();
            kachel.Name = "panel_SpKern_" + schluessel;
            kachel.Size = new Size(SP_ERG_KACHEL_BREITE, SP_ERG_KACHEL_HOEHE);
            kachel.Margin = new Padding(0, 2, 10, 2);

            Label titel = new Label();
            titel.Name = "label_SpKernTitel_" + schluessel;
            titel.Text = beschriftung;
            titel.AutoSize = false;
            titel.Bounds = new Rectangle(0, 0, SP_ERG_KACHEL_BREITE, SP_ERG_KACHEL_TITEL);
            titel.Font = new Font("Segoe UI", 7.75f, FontStyle.Regular);
            titel.ForeColor = Color.FromArgb(90, 96, 104);
            kachel.Controls.Add(titel);

            Label wert = new Label();
            wert.Name = "label_SpKernWert_" + schluessel;
            wert.Text = SP_ERG_UNBESTIMMT;
            wert.AutoSize = false;
            wert.Bounds = new Rectangle(0, SP_ERG_KACHEL_TITEL,
                                        SP_ERG_KACHEL_BREITE, SP_ERG_KACHEL_HOEHE - SP_ERG_KACHEL_TITEL);
            wert.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            wert.ForeColor = Color.FromArgb(28, 32, 38);
            kachel.Controls.Add(wert);

            _spKernwerte[schluessel] = wert;
            return kachel;
        }

        private void SpKernwert(string schluessel, string text)
        {
            Label l;
            if (_spKernwerte.TryGetValue(schluessel, out l)) l.Text = text;
        }

        private void SpKernwert(string schluessel, double wert, string format)
        {
            SpKernwert(schluessel, wert.ToString(format, CultureInfo.CurrentCulture));
        }

        /// <summary>
        /// Füllt den Kernblock aus DENSELBEN Quellen wie die Kennzahlenliste:
        /// <paramref name="k"/> ist das Ergebnismodell, <paramref name="erg"/> das
        /// Engine-Ergebnis (Amortisation samt Sonderfällen, Erzeugungsprüfung),
        /// <paramref name="kontext"/> der Laufkontext (Auslegung der Variante).
        /// </summary>
        private void SpKernblockFuellen(ErgebnisStromspeicherModel k,
                                        SpeicherEngine.SpeicherErgebnis erg,
                                        StromspeicherLaufKontext kontext)
        {
            SpeicherEngine.SpeicherParameter p = kontext != null ? kontext.Parameter : null;

            if (p != null)
            {
                SpKernwert(SPK_KAPAZITAET, p.CNomKwh, "N1");
                SpKernwert(SPK_LEISTUNG, p.PKw, "N1");
                SpKernwert(SPK_SOC_KWH, string.Format(MyResource.Resource.SP_ERG_KERN_BEREICH,
                                                      p.SoCMinKwh.ToString("N1", CultureInfo.CurrentCulture),
                                                      p.SoCMaxKwh.ToString("N1", CultureInfo.CurrentCulture)));
                // Das Band in Prozent der Nennkapazität - dieselbe Lesart wie die
                // Eingabemaske der Variante (Fachkonzept 5.1).
                if (p.CNomKwh > 0.0)
                    SpKernwert(SPK_SOC_PROZENT, string.Format(MyResource.Resource.SP_ERG_KERN_BEREICH,
                                                              (p.SoCMinKwh / p.CNomKwh * 100.0).ToString("N0", CultureInfo.CurrentCulture),
                                                              (p.SoCMaxKwh / p.CNomKwh * 100.0).ToString("N0", CultureInfo.CurrentCulture)));
                else
                    SpKernwert(SPK_SOC_PROZENT, SP_ERG_UNBESTIMMT);
            }
            else
            {
                SpKernwert(SPK_KAPAZITAET, SP_ERG_UNBESTIMMT);
                SpKernwert(SPK_LEISTUNG, SP_ERG_UNBESTIMMT);
                SpKernwert(SPK_SOC_KWH, SP_ERG_UNBESTIMMT);
                SpKernwert(SPK_SOC_PROZENT, SP_ERG_UNBESTIMMT);
            }

            SpKernwert(SPK_BETRIEBSART, string.IsNullOrEmpty(k.Betriebsart) ? SP_ERG_UNBESTIMMT : k.Betriebsart);
            SpKernwert(SPK_BERECHNUNGSART, string.IsNullOrEmpty(k.Berechnungsart) ? SP_ERG_UNBESTIMMT : k.Berechnungsart);

            SpKernwert(SPK_ERTRAG, k.Ertrag_Aequivalent, "N2");
            SpKernwert(SPK_UEBERSCHUSS, k.Jahresueberschuss, "N2");
            // Genau der Text der Listenzeile - mit „nicht amortisierbar" und
            // „> Nutzungsdauer" statt einer Zahl, die es dort nicht gibt.
            SpKernwert(SPK_AMORTISATION, SpAmortisationstext(erg.Wirtschaftlichkeit.StatischeAmortisation));
            SpKernwert(SPK_VOLLZYKLEN, k.Vollzyklen, "N1");

            // Ohne Erzeugung ist die Eigenverbrauchsquote unbestimmt (0/0), nicht null -
            // dieselbe Regel wie in der Liste; die Warnzeile sagt, warum.
            if (erg.Kennzahlen.ErzeugungKwh > 0.0)
                SpKernwert(SPK_EIGENVERBRAUCH, k.Eigenverbrauchsquote, "N1");
            else
                SpKernwert(SPK_EIGENVERBRAUCH, SP_ERG_UNBESTIMMT);

            SpKernwert(SPK_AUTARKIE, k.Autarkiegrad, "N1");
        }

        /// <summary>Räumt den Kernblock ab - Zustand „noch kein Lauf".</summary>
        private void SpKernblockLeeren()
        {
            foreach (Label l in _spKernwerte.Values) l.Text = SP_ERG_UNBESTIMMT;
        }

        /// <summary>
        /// Passt das Grundraster auf den SICHTBAREN Ausschnitt des aufnehmenden
        /// Behälters ein.
        /// </summary>
        /// <remarks>
        /// Der Behälter (splitContainer_Parameter.Panel2) ist breiter als das, was
        /// davon zu sehen ist: Das Formular führt tabControl_Simulation in fester
        /// Größe und rollt darüber hinaus (AutoScroll). Ein Raster mit
        /// <c>Dock = Fill</c> bekäme deshalb die volle Panelbreite und schöbe die
        /// Kennzahlenliste erneut hinter den Fensterrand - genau der Abnahmebefund.
        /// Maßgeblich ist die Schnittmenge der Client-Flächen aller Vorfahren.
        ///
        /// Unterhalb von <see cref="SP_ERG_SEITE_MINBREITE"/> hört das Einpassen auf;
        /// dann übernimmt der Bildlauf von Panel2, und die Liste behält ihre Breite.
        /// </remarks>
        private void SpSeiteEinpassen()
        {
            if (tabelle_SpeicherSeite == null) return;

            Control eltern = tabelle_SpeicherSeite.Parent;
            if (eltern == null || !tabelle_SpeicherSeite.Visible) return;

            Rectangle sichtbar = eltern.RectangleToScreen(eltern.ClientRectangle);
            for (Control p = eltern.Parent; p != null; p = p.Parent)
                sichtbar = Rectangle.Intersect(sichtbar, p.RectangleToScreen(p.ClientRectangle));

            if (sichtbar.Width <= 0 || sichtbar.Height <= 0) return;

            // MinimumSize deckelt nach unten ab; SetBounds erzwingt die Maße auch dann,
            // wenn der Behälter größer ist.
            tabelle_SpeicherSeite.Bounds = new Rectangle(0, 0, sichtbar.Width, sichtbar.Height);
            SpKernblockEinpassen();
            SpTextzeilenEinpassen();
        }

        /// <summary>
        /// Zieht die Höhe der beiden Textzeilen nach - Warnzeile unter dem Diagramm,
        /// Zyklenampel unter der Liste.
        /// </summary>
        /// <remarks>
        /// Muster <c>label_Erdreich</c> weiter oben: Bei fester Breite messen und nur
        /// die Höhe nachziehen. Eine starre Zeilenhöhe schnitte auf schmalen Seiten
        /// genau das ab, was die Zeile zu sagen hat - und das sind Warnungen.
        /// </remarks>
        private void SpTextzeilenEinpassen()
        {
            SpZeilenhoeheNachziehen(tabelle_SpeicherDiagramm, 2, label_SpeicherErzeugungshinweis);
            SpZeilenhoeheNachziehen(tabelle_SpeicherKennzahlen, 1, label_SpeicherAmpel);
        }

        private static void SpZeilenhoeheNachziehen(TableLayoutPanel tabelle, int zeile, Label text)
        {
            if (tabelle == null || text == null || text.Width <= 0) return;
            if (zeile < 0 || zeile >= tabelle.RowStyles.Count) return;

            int hoehe = SP_ERG_ZEILE_HINWEIS;
            if (!string.IsNullOrEmpty(text.Text))
            {
                Size gemessen = TextRenderer.MeasureText(text.Text, text.Font,
                    new Size(text.Width, int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
                if (gemessen.Height + 8 > hoehe) hoehe = gemessen.Height + 8;
            }

            // Nur bei echter Änderung anfassen (siehe SpKernblockEinpassen).
            RowStyle rs = tabelle.RowStyles[zeile];
            if (Math.Abs(rs.Height - hoehe) < 0.5f) return;
            rs.SizeType = SizeType.Absolute;
            rs.Height = hoehe;
        }

        /// <summary>
        /// Setzt die Zeilenhöhe des Kernblocks auf das, was die Kacheln nach dem
        /// Umbruch tatsächlich brauchen (eine Reihe je Gruppe bei breiter Seite, zwei
        /// oder drei bei schmaler).
        /// </summary>
        private void SpKernblockEinpassen()
        {
            if (tabelle_SpeicherSeite == null || panel_SpKernblock == null) return;
            if (flow_SpKernAnlage == null || flow_SpKernErgebnis == null) return;

            int hoehe = panel_SpKernblock.Padding.Vertical
                        + flow_SpKernAnlage.Height + flow_SpKernErgebnis.Height;
            if (hoehe < SP_ERG_KERN_HOEHE_START) hoehe = SP_ERG_KERN_HOEHE_START;

            // Nur bei echter Änderung anfassen - sonst löst jede Layoutrunde die
            // nächste aus (die Höhe hängt an den Kacheln, die Kacheln an der Höhe).
            RowStyle zeile = tabelle_SpeicherSeite.RowStyles[1];
            if (Math.Abs(zeile.Height - hoehe) < 0.5f) return;
            zeile.SizeType = SizeType.Absolute;
            zeile.Height = hoehe;
        }

        /// <summary>
        /// Füllt die Ergebnisseite aus dem Speicherlauf — oder räumt sie ab, wenn der
        /// Lauf keinen Speicher enthielt.
        /// </summary>
        private void SpeicherErgebnisAnzeigen()
        {
            if (listView_SpeicherKennzahlen == null) return;

            // Die Seite kann seit dem Umbau schmaler sein als der Behälter, in dem sie
            // liegt - vor jeder Anzeige neu einpassen (siehe SpSeiteEinpassen).
            SpSeiteEinpassen();

            listView_SpeicherKennzahlen.Items.Clear();
            label_SpeicherAmpel.Text = "";
            if (label_SpeicherErzeugungshinweis != null) label_SpeicherErzeugungshinweis.Text = "";

            SpeicherEngine.SpeicherErgebnis erg = sim != null ? sim.Speicherergebnis : null;
            if (erg == null || !sim.bSimulationSSP)
            {
                label_SpeicherStatus.Text = MyResource.Resource.SP_ERG_KEIN_LAUF;
                label_SpeicherStatus.ForeColor = Color.Firebrick;
                chart_Speicher.Visible = false;
                btn_CsvExportSpeicher.Visible = false;
                btn_SpVariantenVergleich.Visible = false;
                SpKernblockLeeren();
                SpVergleichsspalteSetzen(false);
                // Serien des Vorlaufs abräumen, damit kein Bild ohne Bezug stehenbleibt
                // (Muster KesselErgebnisAnzeigen).
                if (_chartSpeicherManager != null) _chartSpeicherManager.HardReset();
                SpTextzeilenEinpassen();
                return;
            }

            StromspeicherLaufKontext kontext = sim.Speicherkontext;
            ErgebnisStromspeicherModel k = StromspeicherSimCtrl.AlsErgebnismodell(erg, kontext);

            // Vergleichslauf (AP6): Er existiert genau dann, wenn die Variante mit einer
            // anderen Berechnungsart als der Dauernutzung gerechnet hat. Abgebildet wird
            // er über DIESELBE Methode wie das Hauptergebnis - beide Spalten zeigen
            // damit garantiert dieselbe Größe, nur aus einem anderen Lauf.
            SpeicherEngine.SpeicherErgebnis vergleich = kontext != null ? kontext.Vergleichsergebnis : null;
            ErgebnisStromspeicherModel kv = vergleich != null
                ? StromspeicherSimCtrl.AlsErgebnismodell(vergleich, kontext)
                : null;

            label_SpeicherStatus.Text = string.Format(MyResource.Resource.SP_ERG_KOPF_VARIANTE,
                                                      k.Bezeichner, k.Betriebsart, k.Berechnungsart);
            label_SpeicherStatus.ForeColor = Color.Black;

            SpVergleichsspalteSetzen(vergleich != null);
            SpKernblockFuellen(k, erg, kontext);
            SpKennzahlenFuellen(k, erg, kontext, kv, vergleich);
            SpZyklenampelSetzen(k, kontext);
            SpErzeugungshinweisSetzen(erg);
            SpSoCDiagrammZeichnen();
            // Erst jetzt stehen die Texte fest - Warnzeile und Ampel bekommen die Höhe,
            // die ihr Umbruch braucht.
            SpTextzeilenEinpassen();

            btn_CsvExportSpeicher.Visible = true;

            // Vergleichen lässt sich erst ab zwei Varianten (Fachkonzept 7.3). Bei
            // einer einzigen zeigte die Maske dieselben Zahlen, die eine Handbreit
            // weiter links bereits stehen.
            btn_SpVariantenVergleich.Visible = SpVariantenzahl() > 1;
        }

        /// <summary>
        /// Anzahl der <c>SP_TYP</c>-Anlagenzeilen des Projekts — die Zahl der
        /// Speichervarianten (Fachkonzept 7.3: eine Variante ist eine Anlagenzeile).
        /// </summary>
        private int SpVariantenzahl()
        {
            try
            {
                object wert = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                    new System.Data.OleDb.OleDbParameter("@proj", m_ID_Projekt),
                    new System.Data.OleDb.OleDbParameter("@typ", WizardItemClass.SP_TYP));

                if (wert != null && wert != DBNull.Value) return Convert.ToInt32(wert);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Zahl der Speichervarianten konnte nicht gelesen werden: " + ex.Message);
            }

            return 0;
        }

        /// <summary>
        /// Öffnet den Variantenvergleich (AP9) auf dem vorliegenden Simulationslauf.
        /// </summary>
        /// <remarks>
        /// Wurde dort die aktive Variante umgestellt, lädt die Parameterseite neu — sie
        /// zeigt immer die AKTIVE Variante (<see cref="LeseSpeicherVariante"/>). Neu
        /// gerechnet wird bewusst nicht: Der angezeigte Lauf beschreibt, was gerechnet
        /// wurde, und wann er wiederholt wird, entscheidet der Anwender. Dasselbe
        /// Verhalten wie bei der Übernahme aus der Auslegungsoptimierung.
        /// </remarks>
        private void btn_SpVariantenVergleich_Click(object sender, EventArgs e)
        {
            using (Form_SpeicherVariantenVergleich frm =
                       new Form_SpeicherVariantenVergleich(sim, m_ID_Projekt))
            {
                frm.ShowDialog(this);
                if (frm.AktiveVarianteGeaendert) LeseSpeicherVariante();
            }
        }

        /// <summary>
        /// Blendet die Vergleichsspalte ein oder aus (AP6, Fachkonzept Etappe 6).
        /// </summary>
        /// <remarks>
        /// Eine <see cref="ListView"/> kann Spalten nicht ausblenden; "nicht vorhanden"
        /// heißt deshalb Breite 0. Der Platz kommt aus den drei Bestandsspalten, die
        /// Gesamtbreite bleibt gleich — die Liste liegt in einer TabPage mit fester
        /// Entwurfsbreite und darf nicht über deren rechte Kante hinauswachsen.
        /// </remarks>
        private void SpVergleichsspalteSetzen(bool mitVergleich)
        {
            listView_SpeicherKennzahlen.Columns[0].Width =
                mitVergleich ? SP_ERG_SP_KENNZAHL_VGL : SP_ERG_SP_KENNZAHL;
            listView_SpeicherKennzahlen.Columns[SP_ERG_IDX_WERT].Width =
                mitVergleich ? SP_ERG_SP_WERT_VGL : SP_ERG_SP_WERT;
            listView_SpeicherKennzahlen.Columns[SP_ERG_IDX_VERGLEICH].Width =
                mitVergleich ? SP_ERG_SP_VERGLEICH_VGL : 0;
            listView_SpeicherKennzahlen.Columns[SP_ERG_IDX_EINHEIT].Width =
                mitVergleich ? SP_ERG_SP_EINHEIT_VGL : SP_ERG_SP_EINHEIT;

            tooltip.SetToolTip(listView_SpeicherKennzahlen,
                               mitVergleich ? MyResource.Resource.NACHT_ERG_VERGLEICH_HINWEIS : "");
        }

        /// <summary>
        /// Füllt den Kennzahlenblock. <paramref name="kv"/> und
        /// <paramref name="vergleich"/> sind <c>null</c>, wenn es keinen
        /// Vergleichslauf gibt; dann bleibt die Vergleichsspalte leer.
        /// </summary>
        private void SpKennzahlenFuellen(ErgebnisStromspeicherModel k,
                                         SpeicherEngine.SpeicherErgebnis erg,
                                         StromspeicherLaufKontext kontext,
                                         ErgebnisStromspeicherModel kv,
                                         SpeicherEngine.SpeicherErgebnis vergleich)
        {
            const string KWH = "kWh/a";
            const string EUR_A = "€/a";

            // ABNAHMEBEFUND 2: Zuerst die EINGANGSGRÖSSEN des Laufs. Bis hierher zeigte
            // die Seite ausschließlich Ergebnisse; ob der Speicher überhaupt eine Last
            // und eine Erzeugung vor sich hatte, war ihr nicht zu entnehmen - und genau
            // das war die Frage des Anwenders („die Kopplung an PV und Strombedarf
            // scheint nicht zu passen"). Die vier Zeilen stehen bereits in
            // SpeicherKennzahlen und kosten keine zweite Rechnung.
            SpeicherEngine.SpeicherKennzahlen ein = erg.Kennzahlen;
            SpeicherEngine.SpeicherKennzahlen einVgl = vergleich != null ? vergleich.Kennzahlen : null;

            SpZeile("ENERGIE", MyResource.Resource.SP_ERG_LAST, ein.LastKwh,
                    einVgl != null ? einVgl.LastKwh : (double?)null, "N0", KWH);
            SpZeile("ENERGIE", MyResource.Resource.SP_ERG_ERZEUGUNG_PV, ein.ErzeugungPvKwh,
                    einVgl != null ? einVgl.ErzeugungPvKwh : (double?)null, "N0", KWH);
            SpZeile("ENERGIE", MyResource.Resource.SP_ERG_ERZEUGUNG_BHKW, ein.ErzeugungBhkwKwh,
                    einVgl != null ? einVgl.ErzeugungBhkwKwh : (double?)null, "N0", KWH);
            SpZeile("ENERGIE", MyResource.Resource.SP_ERG_DIREKTVERBRAUCH, ein.DirektverbrauchKwh,
                    einVgl != null ? einVgl.DirektverbrauchKwh : (double?)null, "N0", KWH);

            SpZeile("ENERGIE", MyResource.Resource.SP_ERG_LADUNG_PV, k.Ladung_PV, Vgl(kv, x => x.Ladung_PV), "N0", KWH);
            SpZeile("ENERGIE", MyResource.Resource.SP_ERG_LADUNG_BHKW, k.Ladung_BHKW, Vgl(kv, x => x.Ladung_BHKW), "N0", KWH);
            SpZeile("ENERGIE", MyResource.Resource.SP_ERG_LADUNG_NETZ, k.Ladung_Netz, Vgl(kv, x => x.Ladung_Netz), "N0", KWH);
            SpZeile("ENERGIE", MyResource.Resource.SP_ERG_LADUNG_GESAMT, k.Ladung_Gesamt, Vgl(kv, x => x.Ladung_Gesamt), "N0", KWH);
            SpZeile("ENERGIE", MyResource.Resource.SP_ERG_ENTLADUNG, k.Entladung_Gesamt, Vgl(kv, x => x.Entladung_Gesamt), "N0", KWH);
            // Netzverkauf (AP10): Die Größe steht nicht im Ergebnismodell - sie ist dort
            // im Entladungssummenwert enthalten und wird hier eigens ausgewiesen.
            SpZeile("ENERGIE", MyResource.Resource.ARB_ERG_VERKAUF, SpVerkaufKwh(kontext), "N0", KWH);
            SpZeile("ENERGIE", MyResource.Resource.SP_ERG_VERLUSTE, k.Verluste_Gesamt, Vgl(kv, x => x.Verluste_Gesamt), "N0", KWH);
            SpZeile("ENERGIE", MyResource.Resource.SP_ERG_NETZBEZUG_OHNE, k.Netzbezug_Ohne, Vgl(kv, x => x.Netzbezug_Ohne), "N0", KWH);
            SpZeile("ENERGIE", MyResource.Resource.SP_ERG_NETZBEZUG_MIT, k.Netzbezug_Mit, Vgl(kv, x => x.Netzbezug_Mit), "N0", KWH);
            SpZeile("ENERGIE", MyResource.Resource.SP_ERG_EINSPEISUNG_OHNE, k.Einspeisung_Ohne, Vgl(kv, x => x.Einspeisung_Ohne), "N0", KWH);
            SpZeile("ENERGIE", MyResource.Resource.SP_ERG_EINSPEISUNG_MIT, k.Einspeisung_Mit, Vgl(kv, x => x.Einspeisung_Mit), "N0", KWH);
            // ABNAHMEBEFUND 2: Ohne Erzeugung ist die Eigenverbrauchsquote NICHT NULL,
            // sondern unbestimmt (0/0). Die Engine muss dafür 0 führen - das Feld geht so
            // in Tab_ErgebnisStromspeicher, und Access nimmt kein NaN entgegen. Auf dem
            // Bildschirm steht deshalb der Gedankenstrich; die Warnzeile unter den
            // Ausgabeknöpfen sagt, warum.
            bool mitErzeugung = ein.ErzeugungKwh > 0.0;
            if (mitErzeugung)
                SpZeile("ENERGIE", MyResource.Resource.SP_ERG_EIGENVERBRAUCH, k.Eigenverbrauchsquote, Vgl(kv, x => x.Eigenverbrauchsquote), "N1", "%");
            else
                SpZeileText("ENERGIE", MyResource.Resource.SP_ERG_EIGENVERBRAUCH, SP_ERG_UNBESTIMMT, "", "%");

            SpZeile("ENERGIE", MyResource.Resource.SP_ERG_AUTARKIE, k.Autarkiegrad, Vgl(kv, x => x.Autarkiegrad), "N1", "%");

            SpZeile("SPEICHER", MyResource.Resource.SP_ERG_VOLLZYKLEN, k.Vollzyklen, Vgl(kv, x => x.Vollzyklen), "N1", "1/a");
            SpZeile("SPEICHER", MyResource.Resource.SP_ERG_SOC_MIN, k.SoC_Min, Vgl(kv, x => x.SoC_Min), "N1", "kWh");
            SpZeile("SPEICHER", MyResource.Resource.SP_ERG_SOC_MITTEL, k.SoC_Mittel, Vgl(kv, x => x.SoC_Mittel), "N1", "kWh");
            SpZeile("SPEICHER", MyResource.Resource.SP_ERG_SOC_MAX, k.SoC_Max, Vgl(kv, x => x.SoC_Max), "N1", "kWh");
            SpZeile("SPEICHER", MyResource.Resource.SP_ERG_ZEITANTEIL_UNTEN, k.Zeitanteil_Untergrenze, Vgl(kv, x => x.Zeitanteil_Untergrenze), "N1", "%");
            SpZeile("SPEICHER", MyResource.Resource.SP_ERG_ZEITANTEIL_OBEN, k.Zeitanteil_Obergrenze, Vgl(kv, x => x.Zeitanteil_Obergrenze), "N1", "%");
            ListViewItem zyklen = SpZeile("SPEICHER", MyResource.Resource.SP_ERG_ZYKLEN_HOCHRECHNUNG,
                                          k.Zyklen_Hochrechnung, Vgl(kv, x => x.Zyklen_Hochrechnung), "N0", "-");
            zyklen.BackColor = SpAmpelfarbe(k, kontext);
            // Zugesicherte Zyklen sind ein Gerätedatum, kein Ergebnis - hier gibt es
            // nichts zu vergleichen.
            SpZeile("SPEICHER", MyResource.Resource.SP_ERG_ZYKLEN_ZUGESICHERT,
                    kontext != null ? kontext.ZyklenZugesichert : 0.0, "N0", "-");

            SpBudgetzeilen(kontext);

            SpZeile("WIRTSCHAFT", MyResource.Resource.SP_ERG_ERTRAG_BEZUG, k.Ertrag_Bezugsersparnis, Vgl(kv, x => x.Ertrag_Bezugsersparnis), "N2", EUR_A);
            SpZeile("WIRTSCHAFT", MyResource.Resource.SP_ERG_ERTRAG_VERGUETUNG, -k.Ertrag_Verguetung_Entgangen, Vgl(kv, x => -x.Ertrag_Verguetung_Entgangen), "N2", EUR_A);
            SpZeile("WIRTSCHAFT", MyResource.Resource.SP_ERG_ERTRAG_NETZ, k.Ertrag_Netzerloes, Vgl(kv, x => x.Ertrag_Netzerloes), "N2", EUR_A);
            SpZeile("WIRTSCHAFT", MyResource.Resource.SP_ERG_KOSTEN_LADUNG, k.Kosten_Ladung, Vgl(kv, x => x.Kosten_Ladung), "N2", EUR_A);
            SpZeile("WIRTSCHAFT", MyResource.Resource.SP_ERG_ERTRAG_LEISTUNGSPREIS, k.Ertrag_Leistungspreis, Vgl(kv, x => x.Ertrag_Leistungspreis), "N2", EUR_A);
            SpZeile("WIRTSCHAFT", MyResource.Resource.SP_ERG_VERSCHLEISS, k.Verschleisskosten, Vgl(kv, x => x.Verschleisskosten), "N2", EUR_A);
            // Investition und Annuität hängen allein an den Parametern, nicht an der
            // Betriebsstrategie - sie stehen in beiden Spalten gleich und bekommen
            // deshalb keinen Vergleichswert.
            SpZeile("WIRTSCHAFT", MyResource.Resource.SP_ERG_INVESTITION, k.Investition, "N2", "€");
            SpZeile("WIRTSCHAFT", MyResource.Resource.SP_ERG_ANNUITAET, k.Annuitaet, "N2", EUR_A);
            SpZeile("WIRTSCHAFT", MyResource.Resource.SP_ERG_JAHRESUEBERSCHUSS, k.Jahresueberschuss, Vgl(kv, x => x.Jahresueberschuss), "N2", EUR_A);
            SpZeile("WIRTSCHAFT", MyResource.Resource.SP_ERG_ERTRAG_JAHR1, k.Ertrag_Jahr1, Vgl(kv, x => x.Ertrag_Jahr1), "N2", EUR_A);
            SpZeile("WIRTSCHAFT", MyResource.Resource.SP_ERG_ERTRAG_AEQUIVALENT, k.Ertrag_Aequivalent, Vgl(kv, x => x.Ertrag_Aequivalent), "N2", EUR_A);

            // Amortisation direkt aus dem Engine-Ergebnis: Es kennt die beiden Fälle
            // "nicht amortisierbar" und "> Nutzungsdauer", die der gespeicherte Satz als
            // 0 führen muss (Access nimmt kein Infinity entgegen).
            SpZeileText("WIRTSCHAFT", MyResource.Resource.SP_ERG_AMORT_STATISCH,
                        SpAmortisationstext(erg.Wirtschaftlichkeit.StatischeAmortisation),
                        vergleich != null ? SpAmortisationstext(vergleich.Wirtschaftlichkeit.StatischeAmortisation) : "",
                        "a");
            SpZeileText("WIRTSCHAFT", MyResource.Resource.SP_ERG_AMORT_DYNAMISCH,
                        SpAmortisationstext(erg.Wirtschaftlichkeit.DynamischeAmortisation),
                        vergleich != null ? SpAmortisationstext(vergleich.Wirtschaftlichkeit.DynamischeAmortisation) : "",
                        "a");
            SpZeile("WIRTSCHAFT", MyResource.Resource.SP_ERG_KAPITALWERT, k.Kapitalwert, Vgl(kv, x => x.Kapitalwert), "N2", "€");
        }

        /// <summary>
        /// Anzeige einer Kennzahl, die in DIESEM Lauf keinen Bezug hat (Abnahmebefund 2).
        /// Ein Symbol, kein Text — sprachneutral wie die Einheitenspalte.
        /// </summary>
        private const string SP_ERG_UNBESTIMMT = "–";

        /// <summary>
        /// Setzt die Warnzeile für einen Lauf ohne jede Erzeugung (Abnahmebefund 2).
        /// </summary>
        /// <remarks>
        /// Sie ist das Gegenstück zum Protokollhinweis aus
        /// <c>StromspeicherSimCtrl.ErzeugungPruefen</c>: Der Anwender sieht das Protokoll
        /// nicht zwingend, die Ergebnisseite dagegen immer. Bedingung ist die Erzeugung
        /// des Laufs, nicht das PV-Modulflag — eine gerechnete PV-Anlage ohne Ertrag
        /// führt zu demselben Bild.
        /// </remarks>
        private void SpErzeugungshinweisSetzen(SpeicherEngine.SpeicherErgebnis erg)
        {
            if (label_SpeicherErzeugungshinweis == null) return;

            label_SpeicherErzeugungshinweis.Text = erg.Kennzahlen.ErzeugungKwh > 0.0
                ? ""
                : MyResource.Resource.SP_ERG_OHNE_ERZEUGUNG;
        }

        /// <summary>
        /// Wert des Vergleichslaufs, oder <c>null</c>, wenn es keinen gibt — damit
        /// steht die Fallunterscheidung genau einmal statt in jeder Zeile.
        /// </summary>
        private static double? Vgl(ErgebnisStromspeicherModel kv, Func<ErgebnisStromspeicherModel, double> auswahl)
        {
            return kv != null ? auswahl(kv) : (double?)null;
        }

        /// <summary>Ins Netz verkaufte Energie des Laufs [kWh/a]; 0 ohne Preissteuerung.</summary>
        private static double SpVerkaufKwh(StromspeicherLaufKontext kontext)
        {
            return kontext != null && kontext.Arbitrageergebnis != null
                ? kontext.Arbitrageergebnis.Kennzahlen.VerkaufKwh
                : 0.0;
        }

        /// <summary>
        /// Zeilen der Preissteuerung im Speicherblock (AP10, Fachkonzept 6.5): das
        /// Jahres-Zyklenbudget, seine Auslastung mit Warnfärbung analog zur
        /// Zyklen-Ampel, der Verschleiß je ausgespeicherter kWh und die Zahl der
        /// angenommenen Paarungen.
        /// </summary>
        /// <remarks>
        /// Sie erscheinen nur, wenn wirklich mit der Preissteuerung gerechnet wurde —
        /// ohne sie ist das Budget keine Schranke, sondern nur eine zweite Schreibweise
        /// der Zyklenhochrechnung, die eine Zeile weiter oben bereits steht.
        /// </remarks>
        private void SpBudgetzeilen(StromspeicherLaufKontext kontext)
        {
            SpeicherEngine.ArbitrageErgebnis arb = kontext != null ? kontext.Arbitrageergebnis : null;
            if (arb == null) return;

            SpeicherEngine.ArbitrageKennzahlen a = arb.Kennzahlen;

            SpZeile("SPEICHER", MyResource.Resource.ARB_ERG_BUDGET, a.ZyklenbudgetDcKwhProA, "N0", "kWh/a");

            ListViewItem auslastung = SpZeile("SPEICHER", MyResource.Resource.ARB_ERG_BUDGET_AUSLASTUNG,
                                              a.BudgetauslastungProzent, "N1", "%");
            auslastung.BackColor = SpBudgetfarbe(a);

            SpZeile("SPEICHER", MyResource.Resource.ARB_ERG_KVER, a.VerschleissCtKwh, "N3", "ct/kWh");
            SpZeile("SPEICHER", MyResource.Resource.ARB_ERG_PAARE,
                    a.PaareAngenommen + a.VerkaufsslotsAngenommen, "N0", "-");
        }

        /// <summary>
        /// Warnfärbung der Budgetzeile — dieselbe Staffelung wie
        /// <see cref="SpAmpelfarbe"/>: grün bis 90 %, gelb darüber, rot bei
        /// Überschreitung, neutral ohne gepflegtes Budget.
        /// </summary>
        private static Color SpBudgetfarbe(SpeicherEngine.ArbitrageKennzahlen a)
        {
            if (a.ZyklenbudgetDcKwhProA <= 0.0) return Color.FromArgb(240, 240, 240);
            if (a.BudgetauslastungProzent > 100.0) return Color.FromArgb(255, 205, 205);
            if (a.BudgetauslastungProzent > 90.0) return Color.FromArgb(255, 240, 190);
            return Color.FromArgb(215, 245, 215);
        }

        private ListViewItem SpZeile(string gruppe, string bezeichnung, double wert, string format, string einheit)
        {
            return SpZeileText(gruppe, bezeichnung, wert.ToString(format, CultureInfo.CurrentCulture), "", einheit);
        }

        private ListViewItem SpZeile(string gruppe, string bezeichnung, double wert, double? vergleich,
                                     string format, string einheit)
        {
            return SpZeileText(gruppe, bezeichnung,
                               wert.ToString(format, CultureInfo.CurrentCulture),
                               vergleich.HasValue ? vergleich.Value.ToString(format, CultureInfo.CurrentCulture) : "",
                               einheit);
        }

        private ListViewItem SpZeileText(string gruppe, string bezeichnung, string wert, string vergleich, string einheit)
        {
            ListViewItem item = new ListViewItem(bezeichnung);
            item.SubItems.Add(wert);
            item.SubItems.Add(vergleich);
            item.SubItems.Add(einheit);

            foreach (ListViewGroup g in listView_SpeicherKennzahlen.Groups)
                if (g.Name == gruppe) { item.Group = g; break; }

            listView_SpeicherKennzahlen.Items.Add(item);
            return item;
        }

        /// <summary>
        /// Amortisationszeit als Text: die Jahre, oder der Klartext des Sonderfalls
        /// (Fachkonzept 7.1 — die V7-Mappe schrieb beides in dieselbe Zelle, die Engine
        /// trennt Zustand und Zahl).
        /// </summary>
        private static string SpAmortisationstext(SpeicherEngine.Amortisation a)
        {
            switch (a.Status)
            {
                case SpeicherEngine.AmortisationStatus.NichtAmortisierbar:
                    return MyResource.Resource.SP_ERG_NICHT_AMORTISIERBAR;
                case SpeicherEngine.AmortisationStatus.UeberNutzungsdauer:
                    return MyResource.Resource.SP_ERG_UEBER_NUTZUNGSDAUER;
                default:
                    return a.Jahre.ToString("N1", CultureInfo.CurrentCulture);
            }
        }

        /// <summary>
        /// Ampelfarbe der Zyklenzeile (Fachkonzept 5.4/7.1): grün bis 90 % des Budgets,
        /// gelb darüber, rot bei Überschreitung, neutral ohne gepflegte N_zyk.
        /// </summary>
        private static Color SpAmpelfarbe(ErgebnisStromspeicherModel k, StromspeicherLaufKontext kontext)
        {
            double budget = kontext != null ? kontext.ZyklenZugesichert : 0.0;
            if (budget <= 0.0) return Color.FromArgb(240, 240, 240);
            if (k.Zyklen_Hochrechnung > budget) return Color.FromArgb(255, 205, 205);
            if (k.Zyklen_Hochrechnung > budget * 0.9) return Color.FromArgb(255, 240, 190);
            return Color.FromArgb(215, 245, 215);
        }

        private void SpZyklenampelSetzen(ErgebnisStromspeicherModel k, StromspeicherLaufKontext kontext)
        {
            double budget = kontext != null ? kontext.ZyklenZugesichert : 0.0;
            string text;
            Color farbe;

            if (budget <= 0.0)
            {
                text = MyResource.Resource.SP_ERG_AMPEL_OHNE_ANGABE;
                farbe = Color.FromArgb(100, 100, 100);
            }
            else if (k.Zyklen_Hochrechnung > budget)
            {
                text = string.Format(MyResource.Resource.SP_ERG_AMPEL_UEBERSCHRITTEN, k.Zyklen_Hochrechnung, budget);
                farbe = Color.Firebrick;
            }
            else if (k.Zyklen_Hochrechnung > budget * 0.9)
            {
                text = string.Format(MyResource.Resource.SP_ERG_AMPEL_KNAPP, k.Zyklen_Hochrechnung, budget);
                farbe = Color.DarkGoldenrod;
            }
            else
            {
                text = string.Format(MyResource.Resource.SP_ERG_AMPEL_OK, k.Zyklen_Hochrechnung, budget);
                farbe = Color.DarkGreen;
            }

            // Ein vorzeitig aufgebrauchtes Zyklenbudget erklärt, warum die
            // Preissteuerung ab einem bestimmten Tag nichts mehr geplant hat (AP10,
            // Fachkonzept 6.5).
            if (kontext != null && kontext.Arbitrageergebnis != null
                && kontext.Arbitrageergebnis.Kennzahlen.BudgetErschoepft)
            {
                text = MyResource.Resource.ARB_ERG_AMPEL_ERSCHOEPFT + Environment.NewLine + text;
                farbe = Color.Firebrick;
            }

            // Der Kompatibilitätsmodus liefert bewusst kein Produktivergebnis - das darf
            // auf der Seite nicht untergehen (Fachkonzept 5.2).
            if (kontext != null && kontext.Kompatibilitaetsmodus)
            {
                text = MyResource.Resource.SP_ERG_KOMPATIBILITAET_AKTIV + Environment.NewLine + text;
                farbe = Color.Firebrick;
            }

            label_SpeicherAmpel.Text = text;
            label_SpeicherAmpel.ForeColor = farbe;
        }

        private void SpSoCDiagrammZeichnen()
        {
            // EIN ChartManager über die Lebensdauer des Formulars, davor HardReset -
            // Muster der Kesselseite. Init() abonniert Legenden- und Mausrad-Ereignisse
            // am rohen Chart; ein neuer Manager je Lauf sammelte diese Abonnements an.
            if (_chartSpeicherManager == null) _chartSpeicherManager = new ChartManager(chart_Speicher);

            ChartManager cm = _chartSpeicherManager;
            cm.YMaxValue = sim.Speicherfuellstand_viertelstuendlich.Max();
            cm.YMinValue = 0;
            cm.XAxisAsNumber = false;
            cm.XAxisTitle = MyResource.Resource.CHART_ACHSE_JAHRESSTUNDEN;
            cm.YAxisTitle = MyResource.Resource.SP_CHART_ACHSE_SOC;
            cm.toolTipUnit = "kWh";
            cm.ChartTitle = MyResource.Resource.SP_CHART_TITEL_SOC;
            cm.MitLegende = true;
            cm.MitChartBorder = true;
            cm.AreaLine = false;
            // BEIDE Angaben sind nötig - sonst kappt AddSeries auf 8.760 Punkte
            // (Vorbild NavigatorStrom).
            cm.MaxXVALUE = 8760 * 4;
            cm.MitViertelStunde = true;

            cm.HardReset();
            cm.Init();

            SerieAnlegen(cm, S_SPEICHERFUELLSTAND,
                         MyResource.Resource.PSP_CHECKBOX_SPEICHERFUELLSTAND,
                         Color.FromArgb(120, 130, 140), sim.Speicherfuellstand_viertelstuendlich);

            chart_Speicher.Visible = true;
        }

        /// <summary>
        /// Intervallreihen des Speicherlaufs als CSV (Fachkonzept 7.2: Zeitreihen
        /// ausschließlich als CSV, nie über die Excel-Interop-Schnittstelle).
        /// </summary>
        private void btn_CsvExportSpeicher_Click(object sender, EventArgs e)
        {
            if (sim == null || sim.Speicherergebnis == null)
            {
                MessageBox.Show(MyResource.Resource.SP_ERG_KEIN_LAUF, MyResource.Resource.SIM_STROMSPEICHER,
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SpeicherEngine.SpeicherErgebnis erg = sim.Speicherergebnis;

            List<CsvSpalte> spalten = new List<CsvSpalte>();
            spalten.Add(new CsvSpalte(MyResource.Resource.SP_CSV_SOC,
                                      SpeicherEngine.RasterAdapter.ZuFloat(erg.SoCKwh)));
            spalten.Add(new CsvSpalte(MyResource.Resource.SP_CSV_LADUNG,
                                      SpeicherEngine.RasterAdapter.ZuFloat(erg.LadungAcKwh)));
            spalten.Add(new CsvSpalte(MyResource.Resource.SP_CSV_ENTLADUNG,
                                      SpeicherEngine.RasterAdapter.ZuFloat(erg.EntladungAcKwh)));
            spalten.Add(new CsvSpalte(MyResource.Resource.SP_CSV_GELDWERT,
                                      SpeicherEngine.RasterAdapter.ZuFloat(erg.GeldwertEur)));

            // Netzpfade (AP10) nur, wenn mit der Preissteuerung gerechnet wurde - zwei
            // dauerhafte Nullspalten wären im Export nur Ballast.
            SpeicherEngine.ArbitrageErgebnis arb = sim.Speicherkontext != null
                ? sim.Speicherkontext.Arbitrageergebnis
                : null;
            if (arb != null)
            {
                spalten.Add(new CsvSpalte(MyResource.Resource.ARB_CSV_LADUNG_NETZ,
                                          SpeicherEngine.RasterAdapter.ZuFloat(arb.LadungNetzAcKwh)));
                spalten.Add(new CsvSpalte(MyResource.Resource.ARB_CSV_VERKAUF,
                                          SpeicherEngine.RasterAdapter.ZuFloat(arb.VerkaufAcKwh)));
            }

            CsvExportClass.Export(string.Format(MyResource.Resource.SP_DATEI_STROMSPEICHER, m_ID_Projekt),
                                  simulation_Waermebedarf.Stundentemperatur, spalten, true);
        }

        private Panel ErstelleBrennstoffZeile(string bezeichnung, double verbrauchswert)
        {
            // Ein schmales, horizontales Container-Panel für eine Zeile
            Panel zeilenPanel = new Panel();
            zeilenPanel.Size = new Size(350, 30); // Breite an dein UI anpassen, Höhe 30px
            zeilenPanel.Margin = new Padding(0, 2, 0, 2);

            // 1. Das Label für die Bezeichnung (z.B. "Gasverbrauch (Hu):")
            Label lblName = new Label();
            lblName.Text = bezeichnung;
            lblName.Location = new Point(0, 5);
            lblName.Size = new Size(150, 20); // Genug Platz für den Text
            lblName.TextAlign = ContentAlignment.MiddleLeft;

            // 2. Die TextBox für den berechneten Simulationswert
            TextBox txtWert = new TextBox();
            txtWert.Text = verbrauchswert.ToString("F2");
            txtWert.Location = new Point(160, 2);
            txtWert.Size = new Size(80, 20);
            txtWert.ReadOnly = true; // Simulationsergebnisse sollten schreibgeschützt sein
            txtWert.TextAlign = HorizontalAlignment.Right;

            // 3. Das Label für die Einheit
            Label lblEinheit = new Label();
            lblEinheit.Text = "MWh/a";
            lblEinheit.Location = new Point(245, 5);
            lblEinheit.Size = new Size(60, 20);
            lblEinheit.TextAlign = ContentAlignment.MiddleLeft;

            // Alle drei Steuerelemente in das Zeilen-Panel packen
            zeilenPanel.Controls.Add(lblName);
            zeilenPanel.Controls.Add(txtWert);
            zeilenPanel.Controls.Add(lblEinheit);

            return zeilenPanel;
        }

        private void AktualisiereBrennstoffAnzeige(SimulationBHKW simBHKW)
        {
            // Verhindert Flackern beim Neuaufbau
            flowLayoutPanelBrennstoffe.SuspendLayout();

            // Alte dynamische Zeilen restlos löschen
            flowLayoutPanelBrennstoffe.Controls.Clear();

            // Beispiel-Logik: Prüfe deine DB-Werte oder Simulationsergebnisse
            // Füge nur hinzu, was wirklich verbraucht wurde oder definiert ist

            if (simBHKW.Gasverbrauch_BHKW > 0)
            {
                var zeile = ErstelleBrennstoffZeile(MyResource.Resource.SIM_LABEL_GASVERBRAUCH, simBHKW.Gasverbrauch_BHKW);
                flowLayoutPanelBrennstoffe.Controls.Add(zeile);
            }

            if (simBHKW.Oelverbrauch_BHKW > 0)
            {
                var zeile = ErstelleBrennstoffZeile(MyResource.Resource.SIM_LABEL_OELVERBRAUCH, simBHKW.Oelverbrauch_BHKW);
                flowLayoutPanelBrennstoffe.Controls.Add(zeile);
            }

            if (simBHKW.Holzmenge_BHKW > 0)
            {
                var zeile = ErstelleBrennstoffZeile(MyResource.Resource.SIM_LABEL_HOLZVERBRAUCH, simBHKW.Holzmenge_BHKW);
                flowLayoutPanelBrennstoffe.Controls.Add(zeile);
            }

            if (simBHKW.Pellets_BHKW > 0)
            {
                var zeile = ErstelleBrennstoffZeile(MyResource.Resource.SIM_LABEL_PELLETS, simBHKW.Pellets_BHKW);
                flowLayoutPanelBrennstoffe.Controls.Add(zeile);
            }

            if (simBHKW.Rapsoelverbrauch_BHKW > 0)
            {
                var zeile = ErstelleBrennstoffZeile(MyResource.Resource.SIM_LABEL_RAPSOEL, simBHKW.Rapsoelverbrauch_BHKW);
                flowLayoutPanelBrennstoffe.Controls.Add(zeile);
            }
            if (simBHKW.TierischeFette_BHKW > 0)
            {
                var zeile = ErstelleBrennstoffZeile(MyResource.Resource.SIM_LABEL_TIERISCHE_FETTE, simBHKW.TierischeFette_BHKW);
                flowLayoutPanelBrennstoffe.Controls.Add(zeile);
            }
            if (simBHKW.Koks_BHKW > 0)
            {
                var zeile = ErstelleBrennstoffZeile(MyResource.Resource.SIM_LABEL_KOKS, simBHKW.Koks_BHKW);
                flowLayoutPanelBrennstoffe.Controls.Add(zeile);
            }
            if (simBHKW.Kohle_BHKW > 0)
            {
                var zeile = ErstelleBrennstoffZeile(MyResource.Resource.SIM_LABEL_KOHLE, simBHKW.Kohle_BHKW);
                flowLayoutPanelBrennstoffe.Controls.Add(zeile);
            }
            if (simBHKW.Sonstigemenge_BHKW > 0)
            {
                var zeile = ErstelleBrennstoffZeile(MyResource.Resource.SIM_LABEL_SONSTIGE, simBHKW.Sonstigemenge_BHKW);
                flowLayoutPanelBrennstoffe.Controls.Add(zeile);
            }

            // Falls GAR kein Brennstoff aktiv war (z.B. Fehler im Datensatz)
            if (flowLayoutPanelBrennstoffe.Controls.Count == 0)
            {
                Label lblHinweis = new Label();
                lblHinweis.Text = MyResource.Resource.SIM_MSG_KEIN_BRENNSTOFF;
                lblHinweis.ForeColor = Color.Red;
                lblHinweis.AutoSize = true;
                flowLayoutPanelBrennstoffe.Controls.Add(lblHinweis);
            }

            // Layout wieder freigeben -> Windows Forms ordnet alles perfekt untereinander an!
            flowLayoutPanelBrennstoffe.ResumeLayout();
        }

    }

}

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
        /// nicht stapeln. <paramref name="stapelgruppe"/> trennt mehrere Stapel in
        /// EINEM Diagramm — ohne sie würde MS-Chart alle gestapelten Serien in einen
        /// gemeinsamen Stapel werfen.
        /// </summary>
        private static void SerieAnlegen(ChartManager cm, string schluessel, string legende,
                                         Color farbe, float[] werte, SeriesChartType typ,
                                         string stapelgruppe = null)
        {
            cm.AddSeries(schluessel, farbe, werte);
            Series s = cm._chart.Series[schluessel];
            s.LegendText = legende;
            s.ChartType = typ;
            if (!string.IsNullOrEmpty(stapelgruppe)) s["StackedGroupName"] = stapelgruppe;
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

            // Bereich für den KI-Hilfe-Assistenten melden; die aktive
            // Registerkarte wird automatisch mit erkannt.
            this.Activated += (s, e) => HilfeKontext.SetzeBereich("Detaillierte Simulation");
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
                    li.SubItems.Add(sp.Vollzyklen.ToString("F1"));
                    li.SubItems.Add(sp.SOC.ToString("F1"));
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
            PendelspeicherFeldEinrichten();

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
                    MyResource.Resource.SIM_MSG_KEIN_VOLLSTAENDIGES_ERGEBNIS.Replace("\n", Environment.NewLine),
                    MyResource.Resource.SIM_TITEL_ERGEBNIS_SPEICHERN, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Ergebnismodell zentral über den SimulationRunner aufbauen (eine Quelle der Wahrheit).
            ErgebnisModel m = SimulationRunner.BaueErgebnis(m_ID_Projekt,
                simulation_Waermebedarf, simulation_Strombedarf, sim);

            int id = new ErgebnisCtrl().Save(m);
            if (id > 0)
                MessageBox.Show(MyResource.Resource.SIM_MSG_ERGEBNIS_GESPEICHERT, MyResource.Resource.SIM_ERGEBNIS);
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

        /// <summary>Eine Ergebniszeile der Übersicht: Beschriftung, Wertfeld, Einheit.</summary>
        private sealed class UebersichtZeile
        {
            /// <summary>Die Steuerelemente der Zeile.</summary>
            public Control[] Felder;

            /// <summary>Entscheidet, ob die Zeile zu diesem Ergebnis gehört.</summary>
            public Func<ErgebnisPraesenz, bool> Sichtbar;

            /// <summary>Ankerhöhe aus dem Entwurf (kleinstes <c>Top</c> der Zeile).</summary>
            public int Anker;

            /// <summary>Abstand jedes Feldes zum Anker — hält die Zeile in sich stabil.</summary>
            public int[] Versatz;
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
            int anker = int.MaxValue;
            foreach (Control c in felder) if (c.Top < anker) anker = c.Top;

            int[] versatz = new int[felder.Length];
            for (int i = 0; i < felder.Length; i++) versatz[i] = felder[i].Top - anker;

            return new UebersichtZeile { Felder = felder, Sichtbar = sichtbar, Anker = anker, Versatz = versatz };
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
            int platz = 0;
            foreach (UebersichtZeile z in spalte)
            {
                bool an = z.Sichtbar(p);
                foreach (Control c in z.Felder) c.Visible = an;
                if (!an) continue;

                int anker = spalte[platz].Anker;
                for (int i = 0; i < z.Felder.Length; i++)
                    z.Felder[i].Top = anker + z.Versatz[i];
                platz++;
            }
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
                             SeriesChartType.StackedArea, "Produktion");
                SerieAnlegen(_chartManager[3], S_HEIZSTAB, MyResource.Resource.CHART_SEGMENT_HEIZSTAB,
                             Color.Yellow, sim.simulation_wp.Heizstab_stuendlich,
                             SeriesChartType.StackedArea, "Produktion");

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
                double b = (double)sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000;
                double c = (double)sim.simulation_wp.Heizstab_gesamt / 1000;

                // Deckung über die echte Restwärme rechnen - mit Pufferspeicher verschiebt
                // sich Energie zwischen den Stunden, "Produktion / Bedarf" wäre dann ungenau.
                double restMWh = sim.simulation_wp.waermerestbedarf_gesamt / 1000.0;
                double deckung = a > 0 ? (a - restMWh) / a * 100.0 : 0;
                if (deckung > 100) deckung = 100;
                if (deckung < 0) deckung = 0;
                textBox_WB_Deckung.Text = deckung.ToString("F2");


                if (sim.simulation_wp.Bivalenzpunkt != -100)
                    textBox_Bivalenzpunkt.Text = sim.simulation_wp.Bivalenzpunkt.ToString("F2");
                else
                    textBox_Bivalenzpunkt.Text = "-";

                textBox_WPWaermebedarf.Text = (sim.simulation_wp.Waermebedarf_gesamt / 1000).ToString("F2");
                textBox_WPRestwermebedarf.Text = (sim.simulation_wp.waermerestbedarf_gesamt / 1000).ToString("F2");
                textBox_WPStromverbrauch.Text = (sim.simulation_wp.WP_Strombedarf_gesamt / 1000).ToString("F2");
                textBox_HeizstabStromverbrauch.Text = (sim.simulation_wp.Heizstab_gesamt / 1000).ToString("F2");
                textBox_WPWaermeproduktion.Text = (sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000).ToString("F2");
                // Speicher-Ergebnisse als kleine Tabelle statt als eine Textzeile
                // (Konzept 13.3) und die Warnungen der VDI-4640-Auslegungsprüfung
                // werden weiter unten für JEDEN Lauf gefüllt - auch für einen ohne
                // Wärmepumpe, damit die Rubrik dann geleert wird.
                textBox_WPVollbenutzungsstunden.Text = (sim.simulation_wp.WP_Laufzeit / sim.simulation_wp.wp_list.Count).ToString("F0");

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

            // ********************************************************************************************/
            // Heizkessel
            // ********************************************************************************************/
            if (sim.bSimulationKessel)
            {
                // Textfelder Spitzenkessel
                if (simulation_Waermebedarf.Waermebedarf_Gesamt > 0)
                    textBox_SPKWaermebedarfsdeckung.Text = (sim.simulation_spk.S_Waerme_spk * 100 / simulation_Waermebedarf.Waermebedarf_Gesamt).ToString("F2");
                else
                    textBox_SPKWaermebedarfsdeckung.Text = "0";
                textBox_Waermebedarf_Heizkessel.Text = sim.simulation_spk.Waermebedarf_gesamt.ToString("F2");
                textBox_Restwermebedarf_Heizkessel.Text = (sim.simulation_spk.Waermebedarf_gesamt - sim.simulation_spk.S_Waerme_spk).ToString("F2");
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

            // ********************************************************************************************/
            // Solarthermie
            // ********************************************************************************************/
            if (sim.bSimulationSolarthermie)
            {
                // Textfelder Solarthermie
                if (sim.simulation_solarthermie.Waermebedarf_gesamt > 0)
                    textBox_STWaermebedarfsdeckung.Text = (sim.simulation_solarthermie.Waermeproduktion_gesamt * 100 / sim.simulation_solarthermie.Waermebedarf_gesamt).ToString("F2");
                else
                    textBox_STWaermebedarfsdeckung.Text = "";
                textBox_STWaermebedarf.Text = (sim.simulation_solarthermie.Waermebedarf_gesamt / 1000).ToString("F2");
                textBox_STRestwermebedarf.Text = ((sim.simulation_solarthermie.Waermebedarf_gesamt - sim.simulation_solarthermie.Waermeproduktion_gesamt) / 1000).ToString("F2");
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
            SerieAnlegen(_chartManager[9], S_SPEICHERFUELLSTAND, MyResource.Resource.PSP_CHECKBOX_SPEICHERFUELLSTAND, Color.FromArgb(120, 130, 140), sim.simulation_pv.Speicherfuellstand_viertelstunde);
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
            // Chart Solarthermie Wärmerbedarf und Produktion
            _chartManager[10] = new ChartManager(chart_BHKW_Waerme);
            _chartManager[10].YMaxValue = sim.simulation_bhkw.waermebedarf.Max();
            _chartManager[10].YMinValue = 0;
            _chartManager[10].XAxisAsNumber = true;
            _chartManager[10].XAxisTitle = MyResource.Resource.CHART_ACHSE_JAHRESSTUNDEN;
            _chartManager[10].YAxisTitle = MyResource.Resource.CHART_ACHSE_WAERMELAST;
            _chartManager[10].toolTipUnit = "kW";
            _chartManager[10].ChartTitle = MyResource.Resource.CHART_TITEL_WAERMELAST_JAHRESGANGLINIE;
            _chartManager[10].MitLegende = true;
            _chartManager[10].MitChartBorder = true;
            _chartManager[10].AreaLine = false;
            _chartManager[10].Init();

            float[] waermebedarfSortiert = sim.simulation_bhkw.waermebedarf.OrderByDescending(w => w).ToArray();
            SerieAnlegen(_chartManager[10], S_WAERMEBEDARF, MyResource.Resource.CHART_LEGENDE_WAERMEBEDARF, Color.Red, waermebedarfSortiert);
            float[] waermeproduktionSortiert = sim.simulation_bhkw.waermeproduktion.OrderByDescending(w => w).ToArray();
            SerieAnlegen(_chartManager[10], S_WAERMEPRODUKTION, MyResource.Resource.CHART_LEGENDE_WAERMEPRODUKTION, Color.Blue, waermeproduktionSortiert);

            textBox_Betriebsstunden.Text = sim.simulation_bhkw.Betriebsstunden.ToString("F0");
            textBox_Betriebsstunden_Durchschnitt.Text = sim.simulation_bhkw.dLaufzeiten.ToString("F0");

            AktualisiereBrennstoffAnzeige(sim.simulation_bhkw);

            textBox_Waermebedarf_BHKW.Text = (sim.simulation_bhkw.waermebedarf.Sum() / 1000).ToString("F2");
            textBox_Strombedarf_BHKW.Text = (sim.simulation_bhkw.strombedarf.Sum() / 1000).ToString("F2");
            textBox_Waermeproduktion_gesamt_BHKW.Text = sim.simulation_bhkw.Waermeproduktion_BHKW_MWh.ToString("F2");
            textBox_Stromproduktion_gesamt_BHKW.Text = sim.simulation_bhkw.Stromproduktion_BHKW_MWh.ToString("F2");

            float[] restwaerme = sim.SubVectors(sim.simulation_bhkw.waermebedarf, sim.simulation_bhkw.waermeproduktion);
            textBox_Restwaermebedarf_BHKW.Text = (restwaerme.Sum() / 1000f).ToString("F2");

            textBox_Reststrombedarf_BHKW.Text = ((sim.simulation_bhkw.strombedarf.Sum() / 1000) - sim.simulation_bhkw.Stromproduktion_BHKW_MWh).ToString("F2");
            textBox_Waermeueberschuss_BHKW.Text = (sim.simulation_bhkw.Waermeueberschuss / 1000).ToString("F2");

            if (simulation_Waermebedarf.Waermebedarf_Gesamt > 0)
                textBox_Waermedeckung.Text = (sim.simulation_bhkw.Waermeproduktion_BHKW_MWh * 100 / simulation_Waermebedarf.Waermebedarf_Gesamt).ToString("F2");
            else
                textBox_Waermedeckung.Text = "0";
            if (simulation_Strombedarf.Strombedarf_gesamt > 0)
                textBox_Stromdeckung.Text = (sim.simulation_bhkw.Stromproduktion_BHKW_MWh * 100 / simulation_Strombedarf.Strombedarf_gesamt).ToString("F2");
            else
                textBox_Stromdeckung.Text = "0";

            // Auflistung der BHKW-Module (ListView, analog Heizkessel/Solarthermie).
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
                float[] sortedWBArray = (float[])sim.simulation_wp.WP_Waermeproduktion_stuendlich.Clone();
                Array.Sort(sortedWBArray);
                Array.Reverse(sortedWBArray);

                float[] sortedHeizung = (float[])bedarfHeizung.Clone();
                Array.Sort(sortedHeizung);
                Array.Reverse(sortedHeizung);

                float[] sortedWW = (float[])bedarfWW.Clone();
                Array.Sort(sortedWW);
                Array.Reverse(sortedWW);

                float[] sortedHeizstab = (float[])tempHeizstab.Clone();
                Array.Sort(sortedHeizstab);
                Array.Reverse(sortedHeizstab);

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

                SerieAnlegen(manager, S_HEIZWAERMEBEDARF, MyResource.Resource.CHART_LEGENDE_HEIZWAERMEBEDARF,
                             Color.Red, bedarfHeizung, SeriesChartType.StackedArea, "Bedarf");
                SerieAnlegen(manager, S_WARMWASSERBEDARF, MyResource.Resource.CHART_LEGENDE_WARMWASSERBEDARF,
                             Color.DeepSkyBlue, bedarfWW, SeriesChartType.StackedArea, "Bedarf");
                SerieAnlegen(manager, S_WAERMEPRODUKTION, MyResource.Resource.CHART_LEGENDE_WAERMEPRODUKTION,
                             Color.Blue, sim.simulation_wp.WP_Waermeproduktion_stuendlich,
                             SeriesChartType.StackedArea, "Produktion");
                SerieAnlegen(manager, S_HEIZSTAB, MyResource.Resource.CHART_SEGMENT_HEIZSTAB,
                             Color.Yellow, heizstabAnteil, SeriesChartType.StackedArea, "Produktion");
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
                neueMax = sim.Stundenwerte_zu_viertelstunden(sim.simulation_pv.Speicherfuellstand).Max() * 1.1;//sim.simulation_pv.Strombedarf.Max() + 1;
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

        /// <summary>
        /// Richtet das Eingabefeld des BHKW-Pendelspeichers auf LITER ein und laedt den
        /// Wert aus dem Projekt-Puffer "BHKW-Pendelspeicher" (Etappe 3, 14.08.2026).
        ///
        /// Der Alt-Parameter Tab_Einstellungen.Pendelspeicher stand in m3 und wird nicht
        /// mehr gelesen; die Migration hat ihn als m3 x 1000 in den Puffer uebernommen.
        ///
        /// Beschriftung und Wertebereich stehen bewusst HIER und nicht im Designer bzw.
        /// in der .resx: der Designer wuerde die Aenderung beim naechsten Oeffnen
        /// zurueckschreiben, und die Satellitendateien dieses Ordners kennen label56.Text
        /// ohnehin nicht. Die durchgaengige Lokalisierung der Simulationsformulare ist
        /// Paket 9 - dort gehoert dieser Text in die de-DE-/en-US-.resx.
        /// </summary>
        private void PendelspeicherFeldEinrichten()
        {
            label56.Text = MyResource.Resource.PSP_LABEL_VOLUMEN_PENDELSPEICHER;

            // Liter sind ganzzahlig; die Vorgaben des Designers (eine Nachkommastelle,
            // Schrittweite 0,1, Maximum 100) stammen aus der m3-Zeit.
            numericUpDown_Volumen.DecimalPlaces = 0;
            numericUpDown_Volumen.Increment = 50;
            numericUpDown_Volumen.Minimum = 0;
            numericUpDown_Volumen.Maximum = 1000000;

            // Wert beim Laden in den Wertebereich klemmen. NumericUpDown.Value wirft eine
            // ArgumentOutOfRangeException, sobald der Wert ausserhalb von Minimum/Maximum
            // liegt - und das laesst sich hier nicht ausschliessen: Gesamtvolumen kommt
            // aus der Datenbank und kann jede Zahl tragen (Altbestand, Import, Tippfehler
            // in Access). Ein Volumen, das die Anzeige nicht fassen kann, darf das
            // Formular nicht am Oeffnen hindern.
            decimal gelesen = PufferSpCtrl.PendelspeicherVolumenLiter(m_ID_Projekt);
            numericUpDown_Volumen.Value = Math.Max(numericUpDown_Volumen.Minimum,
                                                   Math.Min(numericUpDown_Volumen.Maximum, gelesen));
        }

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
                textBox_Stromspeicher_Ladeenergie_min.Text = ctrl.model.m_Ladefuellstand_Min.ToString();
                textBox_Stromspeicher_Ladeenergie_max.Text = ctrl.model.m_Ladefuellstand_Max.ToString();
                comboBox8_Stromspeicher_LadeenergieMin_auswahl.Text = ctrl.model.m_Ladefuellstand_Min_Auswahl;
                comboBox_Stromspeicher_LadeenergieMax_auswahl.Text = ctrl.model.m_Ladefuellstand_Max_Auswahl;
                textBox_Stromspeicher_Ladeleistung_max.Text = ctrl.model.m_Ladeleistung_Max.ToString();
                comboBox_Stromspeicher_LadeleistungMax_auswahl.Text = ctrl.model.m_Ladeleistung_Max_Auswahl.ToString();
                textBox_Speicher_Ladeschwelle.Text = ctrl.model.m_Ladeschwellwert.ToString();
            }
        }

        private void numericUpDown_UnteresteLG_Leave(object sender, EventArgs e)
        {
            SpeichereKonfigurationsAenderung(model => model.Leistungsgrenze = (int)numericUpDown_UnteresteLG.Value);
        }

        /// <summary>
        /// Speichert das Pendelspeichervolumen in LITERN im Projekt-Puffer
        /// "BHKW-Pendelspeicher" (Etappe 3). Frueher ging der Wert als m3 nach
        /// Tab_Einstellungen.Pendelspeicher - diese Spalte wird nicht mehr gelesen und
        /// bewusst auch nicht mehr geschrieben.
        /// </summary>
        private void numericUpDown_Volumen_Leave(object sender, EventArgs e)
        {
            int liter;
            if (!int.TryParse(numericUpDown_Volumen.Value.ToString("F0", CultureInfo.InvariantCulture),
                              NumberStyles.Integer, CultureInfo.InvariantCulture, out liter))
                return;                                   // unlesbarer Wert: nichts speichern

            if (liter < 0)                                // negative Volumina ablehnen
            {
                liter = 0;
                numericUpDown_Volumen.Value = 0;
            }

            if (!PufferSpCtrl.SetPendelspeicherVolumenLiter(m_ID_Projekt, liter))
                Console.WriteLine("Das Volumen des BHKW-Pendelspeichers konnte nicht gespeichert werden.");
        }

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



        private void textBox_Stromspeicher_Ladeenergie_min_Leave(object sender, EventArgs e)
        {
            SpeichereKonfigurationsAenderung(model => model.m_Ladefuellstand_Min = Convert.ToInt32(textBox_Stromspeicher_Ladeenergie_min.Text));
        }

        private void textBox_Stromspeicher_Ladeenergie_max_Leave(object sender, EventArgs e)
        {
            SpeichereKonfigurationsAenderung(model => model.m_Ladefuellstand_Max = Convert.ToInt32(textBox_Stromspeicher_Ladeenergie_max.Text));
        }

        private void textBox_Stromspeicher_Ladeleistung_max_Leave(object sender, EventArgs e)
        {
            SpeichereKonfigurationsAenderung(model => model.m_Ladeleistung_Max = Convert.ToInt32(textBox_Stromspeicher_Ladeleistung_max.Text));
        }

        private void textBox_Speicher_Ladeschwelle_Leave(object sender, EventArgs e)
        {
            SpeichereKonfigurationsAenderung(model => model.m_Ladeschwellwert = Convert.ToDouble(textBox_Speicher_Ladeschwelle.Text));
        }

        private void comboBox_Stromspeicher_LadeenergieMax_auswahl_SelectedValueChanged(object sender, EventArgs e)
        {
            SpeichereKonfigurationsAenderung(model => model.m_Ladefuellstand_Max_Auswahl = comboBox_Stromspeicher_LadeenergieMax_auswahl.Text);
        }

        private void comboBox8_Stromspeicher_LadeenergieMin_auswahl_SelectedValueChanged(object sender, EventArgs e)
        {
            SpeichereKonfigurationsAenderung(model => model.m_Ladefuellstand_Min_Auswahl = comboBox8_Stromspeicher_LadeenergieMin_auswahl.Text);
        }

        private void comboBox_Stromspeicher_LadeleistungMax_auswahl_SelectedValueChanged(object sender, EventArgs e)
        {
            SpeichereKonfigurationsAenderung(model => model.m_Ladeleistung_Max_Auswahl = comboBox_Stromspeicher_LadeleistungMax_auswahl.Text);
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

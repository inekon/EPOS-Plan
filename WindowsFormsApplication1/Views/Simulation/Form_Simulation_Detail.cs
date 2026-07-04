using System;
using System.Collections.Generic;
using System.Drawing;
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
        private static readonly Color cMenuBase     = Color.FromArgb(0x23, 0x28, 0x2d); // Grundfläche
        private static readonly Color cMenuText     = Color.FromArgb(0xee, 0xee, 0xee); // Text normal
        private static readonly Color cMenuIcon     = Color.FromArgb(0xa7, 0xaa, 0xad); // Icon normal (grau)
        private static readonly Color cMenuHoverBg  = Color.FromArgb(0x19, 0x1e, 0x23); // Hover-Hintergrund
        private static readonly Color cMenuHoverFg  = Color.FromArgb(0x00, 0xb9, 0xeb); // Hover-Text/Icon (cyan)
        private static readonly Color cMenuSelBg    = Color.FromArgb(0x00, 0x73, 0xaa); // aktiv (blau)
        private static readonly Color cMenuSelFg    = Color.White;                      // aktiv Text/Icon
        private static readonly Color cMenuDisabled = Color.FromArgb(0x55, 0x5d, 0x66); // deaktiviert


        public Form_Simulation_Detail(int iD_Projekt)
        {
            InitializeComponent();
            m_ID_Projekt = iD_Projekt;

            init_Chart(chart1);
            init_Chart(chart2);

            // Übersicht-Diagramm (Kreis) initialisieren – entspricht chart5 aus Form_Simulation_Kurz
            ueb_chart.Legends[0].LegendStyle = LegendStyle.Table;
            ueb_chart.Legends[0].Docking = Docking.Right;
            ueb_chart.Legends[0].Alignment = StringAlignment.Center;
            ueb_chart.Legends[0].Title = "Wärmebedarfsdeckung";
            ueb_chart.Legends[0].BorderColor = Color.Green;
            ueb_chart.Series[0].IsValueShownAsLabel = false;
            ueb_chart.Series[0]["PieLabelStyle"] = "Outside";
            ueb_chart.Series[0].Points.Clear();

            listView_SimSPK.View = View.Details;
            listView_SimSPK.Columns.Add("Heizkessel", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Gas/Biogas/Rapsöl/Holz... [MWh/a]", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Öl [MWh/a]", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Jahresnutzungsgrad [%]", -2, HorizontalAlignment.Left);
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

            // Ansicht & Verhalten konfigurieren
            dataGridView_BHKW.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView_BHKW.MultiSelect = false;
            dataGridView_BHKW.AllowUserToAddRows = false;
            dataGridView_BHKW.RowHeadersVisible = false;

            // Zeilenumbruch für den Header aktivieren und Höhe setzen
            dataGridView_BHKW.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dataGridView_BHKW.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridView_BHKW.ColumnHeadersHeight = 42;

            // Spalten hinzufügen (\n für den zentrierten Umbruch)
            dataGridView_BHKW.Columns.Add("Name", "BHKW-Modul");
            dataGridView_BHKW.Columns.Add("Waermeprod", "Wärmeprod.\n[MWh/a]");
            dataGridView_BHKW.Columns.Add("Stromprod", "Stromprod.\n[MWh/a]");

            // Formatierung: Zahlen rechtsbündig, Header zentriert (ab Index 1)
            for (int i = 1; i < dataGridView_BHKW.Columns.Count; i++)
            {
                dataGridView_BHKW.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dataGridView_BHKW.Columns[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // Spalten füllen die gesamte verfügbare Breite (passt sich bei Größenänderung an)
            dataGridView_BHKW.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            // Gewichtung: Modulname breiter als die beiden Zahlenspalten
            dataGridView_BHKW.Columns[0].FillWeight = 50; // BHKW-Modul
            dataGridView_BHKW.Columns[1].FillWeight = 25; // Wärmeprod. [MWh/a]
            dataGridView_BHKW.Columns[2].FillWeight = 25; // Stromprod. [MWh/a]

            VereinheitlichePageSchriftarten(this.tabPage_Bedarf);
            VereinheitlichePageSchriftarten(this.tabPage_Wärmepumpe);
            VereinheitlichePageSchriftarten(this.tabPage_Heizkessel);
            VereinheitlichePageSchriftarten(this.tabPage_BHKW);
            VereinheitlichePageSchriftarten(this.tabPage_Solarthermie);
            VereinheitlichePageSchriftarten(this.tabPage_Photovoltaik);
            VereinheitlichePageSchriftarten(this.tabPage_Stromspeicher);
            VereinheitlichePageSchriftarten(this.tabPage_Ergebnis);

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
            ListViewItem itemBedarf = new ListViewItem("Energiebedarf");
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

                    if (geraetName == "Wärmepumpe")
                    {
                        ListViewItem itemWP = new ListViewItem("Wärmepumpe");
                        itemWP.Tag = "tabPage_Wärmepumpe";
                        listViewQuellen.Items.Add(itemWP);
                        hinzugefuegteGerete.Add(geraetName);
                    }
                    else if (geraetName == "Heizkessel")
                    {
                        ListViewItem itemKessel = new ListViewItem("Heizkessel");
                        itemKessel.Tag = "tabPage_Heizkessel";
                        listViewQuellen.Items.Add(itemKessel);
                        hinzugefuegteGerete.Add(geraetName);
                    }
                    else if (geraetName == "BHKW")
                    {
                        ListViewItem itemBHKW = new ListViewItem("BHKW");
                        itemBHKW.Tag = "tabPage_BHKW";
                        listViewQuellen.Items.Add(itemBHKW);
                        hinzugefuegteGerete.Add(geraetName);
                    }
                    else if (geraetName == "Solarthermie")
                    {
                        ListViewItem itemSolar = new ListViewItem("Solarthermie");
                        itemSolar.Tag = "tabPage_Solarthermie";
                        listViewQuellen.Items.Add(itemSolar);
                        hinzugefuegteGerete.Add(geraetName);
                    }
                }
            }

            // --- POS 3: Photovoltaik (Fest zugewiesen an Tool 5 / Index 4) ---
            if (tool[4] == "Photovoltaik" || tool[4] == "true" || tool.Contains("Photovoltaik"))
            {
                ListViewItem itemPV = new ListViewItem("Photovoltaik");
                itemPV.Tag = "tabPage_Photovoltaik";
                listViewQuellen.Items.Add(itemPV);
            }

            // --- POS 4: Stromspeicher (Fest zugewiesen an Tool 6 / Index 5) ---
            if (tool[5] == "Stromspeicher" || tool[5] == "true" || tool.Contains("Stromspeicher"))
            {
                ListViewItem itemSpeicher = new ListViewItem("Stromspeicher");
                itemSpeicher.Tag = "tabPage_Stromspeicher";
                listViewQuellen.Items.Add(itemSpeicher);
            }

            // --- AM ENDE DER LISTE: Ergebnisauswertung (MUSS IMMER DA SEIN) ---
            ListViewItem itemErgebnis = new ListViewItem("Ergebnis");
            itemErgebnis.Tag = "tabPage_Ergebnis";
            listViewQuellen.Items.Add(itemErgebnis);
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

            MacheTextAbschnittFett(richTextBox_Info, "Wärmegeführt (Standard)");
            MacheTextAbschnittFett(richTextBox_Info, "Stromgeführt (Wirtschaftlich)");
            MacheTextAbschnittFett(richTextBox_Info, "Ohne Einspeisung (Zero-Export)");

            LeseKonfiguration();

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

        private void btn_Simulation_Click(object sender, EventArgs e)
        {
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
                MessageBox.Show("Bitte zuerst die Konfiguration festlegen.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string[] tool = new string[6];
            tool[0] = ctrl.model.m_Tool_1;
            tool[1] = ctrl.model.m_Tool_2;
            tool[2] = ctrl.model.m_Tool_3;
            tool[3] = ctrl.model.m_Tool_4;
            tool[4] = ctrl.model.m_Tool_5;
            tool[5] = ctrl.model.m_Tool_6;

            if (!Energiebedarf(ctrl.m_Netzverluste, ctrl.m_szNetzverlusteEinheit)) return;

            // Wärmebedarf und Strombedarf Simulation durchführen
            sim.tool = tool;
            sim.Stundentemperatur = simulation_Waermebedarf.Stundentemperatur;
            sim.simulation_Waermebedarf = simulation_Waermebedarf;
            sim.simulation_Strombedarf = simulation_Strombedarf;
            sim.ctrl_konfig = ctrl;

            textBox_gesStrombedarf.Text = simulation_Strombedarf.Strombedarf_gesamt.ToString("F2");
            textBox_gesWaermebedarf.Text = simulation_Waermebedarf.Waermebedarf_Gesamt.ToString("F2");

            sim.GrenzleistungBHKW = (int)numericUpDown_UnteresteLG.Value;
            sim.VolumenPendelspeicherBHKW = (int)numericUpDown_Volumen.Value;
            sim.modeBHKW = bhkwSimulationsArt;

            // Tool Simulation WP, SPK usw. durchführen
            sim.Do_Simulation(m_ID_Projekt);
            Endergebniss_Simulation();

            // Inhalt des Übersicht-Tabs aktualisieren (wie zuvor in Form_Simulation_Kurz.btn_Simulation_Click)
            FuelleUebersicht();

            tabControl_Simulation.SelectedTab = tabPage_Simulation;
            if (tabControl_Simulation.SelectedTab.Name == "tabPage_Simulation")
            {
                listViewQuellen.SelectedIndices.Add(0);
            }

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

            ueb_chart.Series[0].Points.Clear();
            if (sim.simulation_wp.WP_Waermeproduktion_gesamt > 0)
                ueb_chart.Series[0].Points.AddXY("Wärmepumpe", sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000);
            if (sim.simulation_wp.Heizstab_gesamt > 0)
                ueb_chart.Series[0].Points.AddXY("Heizstab", sim.simulation_wp.Heizstab_gesamt / 1000);
            if (sim.simulation_spk.S_Waerme_spk > 0)
                ueb_chart.Series[0].Points.AddXY("Heizkessel", sim.simulation_spk.S_Waerme_spk);
            if (sim.simulation_bhkw.Waermeproduktion_BHKW_MWh > 0)
                ueb_chart.Series[0].Points.AddXY("BHKW", sim.simulation_bhkw.Waermeproduktion_BHKW_MWh);
            if (sim.Restwaerme > 0)
                ueb_chart.Series[0].Points.AddXY("Rest", sim.Restwaerme);
        }

        private bool Energiebedarf(double Netzverluste, string NetzverlusteEinheit)
        {
            int netzverluste = (int)ctrl.m_Netzverluste;
            if (ctrl.m_szNetzverlusteEinheit == "%" && netzverluste > 100)
            {
                MessageBox.Show("die Netzverluste dürfen nicht größer als 100 % sein!");
                return false;
            }

            projektCtrl.ReadSingle(m_ID_Projekt);
            int nKlimaregion = projektCtrl.m_ID_Klimaregion;
            if (nKlimaregion == 0)
            {
                MessageBox.Show("Klimaregion auswählen!");
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
                listViewQuellen.SelectedIndices.Add(mainTablistIndex);
            }
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
                _chartManager[3].XAxisTitle = "Jahresstunden";
                _chartManager[3].YAxisTitle = "Wärmelast";
                _chartManager[3].toolTipUnit = "kW";
                _chartManager[3].ChartTitle = "Wärmelast Jahresganglinie";
                _chartManager[3].MitLegende = true;
                _chartManager[3].Init();
                _chartManager[3].AddSeries("Waermebedarf", Color.Red, sim.simulation_wp.Waermebedarf_stuendlich);
                _chartManager[3].AddSeries("Heizstab", Color.Yellow, sim.simulation_wp.Heizstab_stuendlich);
                _chartManager[3].AddSeries("Wärmeproduktion", Color.Blue, sim.simulation_wp.WP_Waermeproduktion_stuendlich);

                // Chart Wärmepumpe Strombedarf und Produktion
                float[] temp = simulation_Strombedarf.AddVectors(sim.simulation_wp.WP_Strombedarf_stuendlich, sim.simulation_wp.Heizstab_stuendlich);
                _chartManager[6] = new ChartManager(chart6);
                _chartManager[6].YMaxValue = temp.Max();
                _chartManager[6].YMinValue = 0;
                _chartManager[6].XAxisAsNumber = false;
                _chartManager[6].XAxisTitle = "Jahresstunden";
                _chartManager[6].YAxisTitle = "Strombedarf";
                _chartManager[6].toolTipUnit = "kW";
                _chartManager[6].ChartTitle = "Strombedarf Jahresganglinie";
                _chartManager[6].Init();
                _chartManager[6].AddSeries("Strombedarf", Color.Red, temp);

                textBox_WB_Deckung.Text = "";
                double a = (double)simulation_Waermebedarf.Waermebedarf_Gesamt;
                double b = (double)sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000;
                double c = (double)sim.simulation_wp.Heizstab_gesamt / 1000;

                if ((b / a * 100) > 100)
                    textBox_WB_Deckung.Text = "100";
                else
                    textBox_WB_Deckung.Text = ((b + c) / a * 100).ToString("F2");


                if (sim.simulation_wp.Bivalenzpunkt != -100)
                    textBox_Bivalenzpunkt.Text = sim.simulation_wp.Bivalenzpunkt.ToString("F2");
                else
                    textBox_Bivalenzpunkt.Text = "-";

                textBox_WPWaermebedarf.Text = (sim.simulation_wp.Waermebedarf_gesamt / 1000).ToString("F2");
                textBox_WPRestwermebedarf.Text = (sim.simulation_wp.Waermebedarf_gesamt / 1000 - sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000 - sim.simulation_wp.Heizstab_gesamt / 1000).ToString("F2");
                textBox_WPStromverbrauch.Text = (sim.simulation_wp.WP_Strombedarf_gesamt / 1000).ToString("F2");
                textBox_HeizstabStromverbrauch.Text = (sim.simulation_wp.Heizstab_gesamt / 1000).ToString("F2");
                textBox_WPWaermeproduktion.Text = (sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000).ToString("F2");
                textBox_Pufferspeicher.Text = (sim.simulation_wp.Volumen_Pufferspeicher * 1.16).ToString();
                textBox_WPVollbenutzungsstunden.Text = (sim.simulation_wp.WP_Laufzeit / sim.simulation_wp.wp_list.Count).ToString("F0");

                double Max_Spk = 0;
                for (int i = 0; i < 8750; i++)
                {
                    if (sim.simulation_wp.waermerestbedarf_stuendlich[i] > Max_Spk) Max_Spk = sim.simulation_wp.waermerestbedarf_stuendlich[i];
                }
                textBox_MinSPKLeistung.Text = Max_Spk.ToString("F2");

                // Ansicht & Verhalten konfigurieren
                listView_SimWP.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                listView_SimWP.MultiSelect = false;
                listView_SimWP.AllowUserToAddRows = false;
                listView_SimWP.RowHeadersVisible = false;

                // 1. Zeilenumbruch für den Header aktivieren und feste Höhe vergeben
                listView_SimWP.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
                listView_SimWP.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                listView_SimWP.ColumnHeadersHeight = 42;

                // Spalten hinzufügen (\n sorgt für den harten Umbruch)
                listView_SimWP.Columns.Add("Modul", "Modul");
                listView_SimWP.Columns.Add("Leistung", "Leistung\n[kW]");
                listView_SimWP.Columns.Add("Waermeprod", "Wärmeprod.\n[MWh/a]");
                listView_SimWP.Columns.Add("Stromverbr", "Stromverbr.\n[MWh/a]");
                listView_SimWP.Columns.Add("Heizstab", "Heizstab\n[MWh/a]");
                listView_SimWP.Columns.Add("Betriebsstunden", "Betriebsstunden\n[h/a]");

                // 2. Formatierung für die Zahlen-Spalten (Ab Index 1)
                for (int i = 1; i < listView_SimWP.Columns.Count; i++)
                {
                    // Die Datenzeilen (Zahlen) bleiben rechtsbündig für bessere Lesbarkeit
                    listView_SimWP.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                    // NEU: Der Spaltenkopf (Header) wird exakt mittig zentriert!
                    listView_SimWP.Columns[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                // Automatische Breitenanpassung aktivieren
                listView_SimWP.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;

                // Grid leeren
                listView_SimWP.Rows.Clear();

                // Daten zeilenweise eintragen
                for (int i = 0; i < sim.simulation_wp.wp_list.Count(); i++)
                {
                    listView_SimWP.Rows.Add(
                        sim.simulation_wp.WP_Modul[i],
                        sim.simulation_wp.wp_model[i].Grenzleistung.ToString("F2"),
                        (sim.simulation_wp.Modul_WP_Waermeproduktion[i] / 1000.0).ToString("F2"),
                        (sim.simulation_wp.Modul_WP_Strombedarf[i] / 1000.0).ToString("F2"),
                        (sim.simulation_wp.Modul_Heizstab[i] / 1000.0).ToString("F2"),
                        sim.simulation_wp.Modul_WP_Laufzeit[i].ToString("F2")
                    );
                }

                // Spaltenbreiten final an den frisch geschriebenen Inhalt anpassen
                listView_SimWP.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);

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
                _chartManager[4].ChartTitle = "Leistung über Außentemperatur";
                _chartManager[4].XAxisTitle = "Temperatur [°C]";
                _chartManager[4].YAxisTitle = "Leistung [kW]";
                _chartManager[4].IsXYChart = true;
                _chartManager[4].AreaLine = true; // Area Chart Effekt
                _chartManager[4].MitLegende = true;
                _chartManager[4].YMaxValue = sim.simulation_wp.Waermebedarf_stuendlich.Max();
                _chartManager[4].Init();

                // Daten hinzufügen (gefilterte PointF[] Arrays)
                _chartManager[4].AddSeries("Wärmebedarf", Color.FromArgb(120, Color.Red), ps_bedarf, 0);
                _chartManager[4].AddSeries("Heizstab", Color.FromArgb(120, Color.Yellow), ps_heizstab, 0);
                _chartManager[4].AddSeries("Wärmeproduktion", Color.FromArgb(120, Color.Blue), ps_produktion, 0);
            }

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
                _chartManager[8].XAxisTitle = "Jahresstunden";
                _chartManager[8].YAxisTitle = "Wärmelast";
                _chartManager[8].toolTipUnit = "kW";
                _chartManager[8].ChartTitle = "Wärmelast Jahresganglinie";
                _chartManager[8].MitLegende = true;
                _chartManager[8].MitChartBorder = true;
                _chartManager[8].AreaLine = false;
                _chartManager[8].Init();
                _chartManager[8].AddSeries("Waermebedarf", Color.Red, Array.ConvertAll<double, float>(sim.simulation_solarthermie.Waermebedarf, x => (float)x));
                _chartManager[8].AddSeries("Wärmeproduktion", Color.Blue, Array.ConvertAll<double, float>(sim.simulation_solarthermie.Waermeproduktion, x => (float)x));
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
            _chartManager[9].XAxisTitle = "Monate";
            _chartManager[9].YAxisTitle = "Leistung";
            _chartManager[9].toolTipUnit = "kW";
            _chartManager[9].ChartTitle = "Strombedarf, Photovoltaik Jahresganglinie";
            _chartManager[9].MitLegende = true;
            _chartManager[9].MaxXVALUE = 8760 * 4;
            _chartManager[9].MitViertelStunde = true;
            _chartManager[9].Init();
            // NUR DER SPEICHER geht auf die rechte Achse (true = Sekundärachse kWh)
            _chartManager[9].AddSeries("Speicherfüllstand", Color.FromArgb(120, 130, 140), sim.simulation_pv.Speicherfuellstand_viertelstunde);
            _chartManager[9].AddSeries("Überschuss", Color.Yellow, sim.simulation_pv.Ueberschuss_viertelstunde);
            _chartManager[9].AddSeries("Strombedarf", Color.Red, sim.simulation_pv.Strombedarf);
            _chartManager[9].AddSeries("Photovoltaik", Color.BlueViolet, sim.simulation_pv.Stromproduktion_viertelstunde);
            _chartManager[9]._chart.Series["Überschuss"].Enabled = false;
            _chartManager[9]._chart.Series["Speicherfüllstand"].Enabled = false;
            checkBox_Ueberschuss.Checked = false;
            checkBox_Speicherzustand.Checked = false;
            textBox_MaxPSolar.Text = sim.simulation_pv.MaxPSolar.ToString("F2");



            // ********************************************************************************************/
            // BHKW
            // ********************************************************************************************/
            // Chart Solarthermie Wärmerbedarf und Produktion
            _chartManager[10] = new ChartManager(chart_BHKW_Waerme);
            _chartManager[10].YMaxValue = sim.simulation_bhkw.waermebedarf.Max();
            _chartManager[10].YMinValue = 0;
            _chartManager[10].XAxisAsNumber = true;
            _chartManager[10].XAxisTitle = "Jahresstunden";
            _chartManager[10].YAxisTitle = "Wärmelast";
            _chartManager[10].toolTipUnit = "kW";
            _chartManager[10].ChartTitle = "Wärmelast Jahresganglinie";
            _chartManager[10].MitLegende = true;
            _chartManager[10].MitChartBorder = true;
            _chartManager[10].AreaLine = false;
            _chartManager[10].Init();

            float[] waermebedarfSortiert = sim.simulation_bhkw.waermebedarf.OrderByDescending(w => w).ToArray();
            _chartManager[10].AddSeries("Waermebedarf", Color.Red, waermebedarfSortiert);
            float[] waermeproduktionSortiert = sim.simulation_bhkw.waermeproduktion.OrderByDescending(w => w).ToArray();
            _chartManager[10].AddSeries("Wärmeproduktion", Color.Blue, waermeproduktionSortiert);

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

            // Grid einmal komplett leeren
            dataGridView_BHKW.Rows.Clear();

            // Falls eine gültige Simulation vorliegt
            if (sim != null && sim.simulation_bhkw != null)
            {
                // Umrechnung des Überschusses von kWh in MWh
                double ueberschussMWh = sim.simulation_bhkw.Waermeueberschuss / 1000.0;

                // 2. Werte aus den Simulationsergebnissen ziehen
                for (int i = 0; i < sim.simulation_bhkw.bhkw_list.Count; i++)
                {
                    string name = sim.simulation_bhkw.bhkw_list_Namen[i] ?? "Standard BHKW";
                    double waermeproduktionMWh = sim.simulation_bhkw.s_waerme_MWh[i];
                    double stromproduktionMWh = sim.simulation_bhkw.s_strom_MWh[i];

                    // 3. Zeile im DataGridView hinzufügen
                    dataGridView_BHKW.Rows.Add(
                        name,
                        waermeproduktionMWh.ToString("F2"),
                        stromproduktionMWh.ToString("F2")
                    );
                }
            }

            // Spaltenbreiten an den neuen Inhalt anpassen
            dataGridView_BHKW.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);

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
            chartControl.ChartAreas[0].AxisX.Title = "Jahresstunden";

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
            chartControl.ChartAreas[0].AxisX.Title = "Jahresstunden";

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

            if (checkBox_WP_sortiert.Checked)
            {
                // --- SORTIERTER MODUS (Numerische X-Achse) ---
                float[] sortedWBArray = (float[])sim.simulation_wp.WP_Waermeproduktion_stuendlich.Clone();
                Array.Sort(sortedWBArray);
                Array.Reverse(sortedWBArray);

                float[] sortedBedarf = (float[])sim.simulation_wp.Waermebedarf_stuendlich.Clone();
                Array.Sort(sortedBedarf);
                Array.Reverse(sortedBedarf);

                float[] sortedHeizstab = (float[])tempHeizstab.Clone();
                Array.Sort(sortedHeizstab);
                Array.Reverse(sortedHeizstab);

                manager.XAxisAsNumber = true; // Wichtig für Init()
                manager.HardReset();
                manager.Init();

                manager.AddSeries("Wärmebedarf", Color.Red, sortedBedarf);
                manager.AddSeries("Heizstab", Color.Yellow, sortedHeizstab);
                manager.AddSeries("Wärmeproduktion", Color.Blue, sortedWBArray);
            }
            else
            {
                // --- CHRONOLOGISCHER MODUS (Datum X-Achse) ---
                manager.XAxisAsNumber = false;
                manager.HardReset();
                manager.Init(); // Hier wird FormatXAxisWithDate() aufgerufen

                manager.AddSeries("Wärmebedarf", Color.Red, sim.simulation_wp.Waermebedarf_stuendlich);
                manager.AddSeries("Heizstab", Color.Yellow, tempHeizstab);
                manager.AddSeries("Wärmeproduktion", Color.Blue, sim.simulation_wp.WP_Waermeproduktion_stuendlich);
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
            if (chart_PV.Series.IndexOf("Überschuss") != -1)
            {
                chart_PV.Series["Überschuss"].Enabled = checkBox_Ueberschuss.Checked;
            }

            // 2. Skalierung über den Manager korrigieren
            // _chartManager[9].UpdateYScaleBasedOnVisibleSeries();
        }

        private void checkBox_Speicherzustand_CheckedChanged(object sender, EventArgs e)
        {
            double neueMax = 0;

            _chartManager[9]._chart.Series["Speicherfüllstand"].Enabled = checkBox_Speicherzustand.Checked;

            if (checkBox_Speicherzustand.Checked)
            {
                neueMax = sim.Stundenwerte_zu_viertelstunden(sim.simulation_pv.Speicherfuellstand).Max() * 1.1;//sim.simulation_pv.Strombedarf.Max() + 1;
                if (neueMax < 10) neueMax = 10; // Minimum setzen, damit die Achse nicht zu klein wird
            }
            else
                neueMax = sim.simulation_pv.Strombedarf.Max() * 1.1;

            // Nur die Achse updaten ohne die Daten zu löschen:
            var ca = _chartManager[9]._chart.ChartAreas[0];

            ca.AxisY.Maximum = neueMax; // Den oben berechneten Wert direkt setzen
            ca.AxisY.Interval = 0;      // Auf Auto stellen

            // 2. Prüfen, ob die Serie existiert
            if (_chartManager[9]._chart.Series.IndexOf("Speicherfüllstand") != -1)
            {
                var s = _chartManager[9]._chart.Series["Speicherfüllstand"];
                bool anzeigen = checkBox_Speicherzustand.Checked;

                s.Enabled = anzeigen;

                if (anzeigen)
                {
                    // --- SPEZIALFALL: Y2-ACHSE AKTIVIEREN ---
                    s.YAxisType = AxisType.Secondary; // Serie nach rechts binden
                    ca.AxisY2.Enabled = AxisEnabled.True;

                    // Optik der rechten Achse
                    ca.AxisY2.Title = "Speicher [kWh]";
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
                // -25 sorgt für einen kleinen Puffer, damit keine horizontale Scrollleiste entsteht
                listViewQuellen.Columns[0].Width = splitContainer_Parameter.Panel1.Width - 25;
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
                numericUpDown_Volumen.Value = (decimal)ctrl.model.Pendelspeicher;
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

        private void numericUpDown_Volumen_Leave(object sender, EventArgs e)
        {
            SpeichereKonfigurationsAenderung(model => model.Pendelspeicher = (double)numericUpDown_Volumen.Value);
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
                var zeile = ErstelleBrennstoffZeile("Gasverbrauch (Hu):", simBHKW.Gasverbrauch_BHKW);
                flowLayoutPanelBrennstoffe.Controls.Add(zeile);
            }

            if (simBHKW.Oelverbrauch_BHKW > 0)
            {
                var zeile = ErstelleBrennstoffZeile("Ölverbrauch:", simBHKW.Oelverbrauch_BHKW);
                flowLayoutPanelBrennstoffe.Controls.Add(zeile);
            }

            if (simBHKW.Holzmenge_BHKW > 0)
            {
                var zeile = ErstelleBrennstoffZeile("Holzverbrauch:", simBHKW.Holzmenge_BHKW);
                flowLayoutPanelBrennstoffe.Controls.Add(zeile);
            }

            if (simBHKW.Pellets_BHKW > 0)
            {
                var zeile = ErstelleBrennstoffZeile("Pellets:", simBHKW.Pellets_BHKW);
                flowLayoutPanelBrennstoffe.Controls.Add(zeile);
            }

            // Falls GAR kein Brennstoff aktiv war (z.B. Fehler im Datensatz)
            if (flowLayoutPanelBrennstoffe.Controls.Count == 0)
            {
                Label lblHinweis = new Label();
                lblHinweis.Text = "Kein Brennstoff für dieses BHKW definiert.";
                lblHinweis.ForeColor = Color.Red;
                lblHinweis.AutoSize = true;
                flowLayoutPanelBrennstoffe.Controls.Add(lblHinweis);
            }

            // Layout wieder freigeben -> Windows Forms ordnet alles perfekt untereinander an!
            flowLayoutPanelBrennstoffe.ResumeLayout();
        }
    }

}

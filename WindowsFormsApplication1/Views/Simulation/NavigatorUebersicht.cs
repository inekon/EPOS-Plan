using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace WindowsFormsApplication1
{
    public partial class NavigatorUebersicht : UserControl, INavigatableContent
    {
        double waerme_solar = 0, gesamt_waerme = 0, restwaermebedarf = 0;
        double waerme_spk = 0, waerme_wp = 0, waerme_heizstab = 0, waerme_bhkw = 0;
        SimulationControl sim;

        // Donut Farben (WP, Solar, Heizstab, Kessel, Rest)
        Color[] palette = new Color[] {
            ColorTranslator.FromHtml("#2ECC71"), // WP
            ColorTranslator.FromHtml("#E67E22"), // Solar
            ColorTranslator.FromHtml("#F1C40F"), // Heizstab
            ColorTranslator.FromHtml("#95A5A6"), // Kessel
            ColorTranslator.FromHtml("#75A5A6"), // BHKW
            ColorTranslator.FromHtml("#3498DB")  // Rest
        };

        public NavigatorUebersicht(SimulationControl simctrl)
        {
            InitializeComponent();
            sim = simctrl;
            this.DoubleBuffered = true;
            this.Paint += new PaintEventHandler(NavigatorUebersicht_Paint);

            // Reagiert auf Größenänderungen des Fensters
            this.Resize += (s, e) => this.Invalidate();

            // --- WICHTIG: Erst Spalten definieren ---
            dataGridView1.Columns.Clear();

            // AutoGenerateColumns weglassen oder auf true
            // dataGridView1.AutoGenerateColumns = true; 

            // Spalte 0
            var colErzeuger = new DataGridViewTextBoxColumn
            {
                HeaderText = "Energie-Erzeuger",
                Name = "Erzeuger",
                FillWeight = 150 // Nutze FillWeight statt fester Breite für AutoSizeMode.Fill
            };

            // Spalte 1
            var colErgebnis = new DataGridViewTextBoxColumn
            {
                HeaderText = "Ergebnis [MWh/a]",
                Name = "Ergebnis",
                FillWeight = 100
            };

            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { colErzeuger, colErgebnis });

            // --- DANN das Styling aufrufen (weil Spalten jetzt existieren) ---
            SetupDataGridViewLook(dataGridView1);
        }

        private void FillTableWithData(DataGridView dgvErgebnisse)
        {
            dgvErgebnisse.Rows.Clear();
            dgvErgebnisse.Rows.Add("Wärmepumpe", (sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000).ToString("F2"));
            dgvErgebnisse.Rows.Add("Heizstab", (sim.simulation_wp.Heizstab_gesamt / 1000).ToString("F2"));
            dgvErgebnisse.Rows.Add("Solarthermie-Anlage", (sim.simulation_solarthermie.Waermeproduktion_gesamt / 1000).ToString("F2"));
            dgvErgebnisse.Rows.Add("HeizKessel", sim.simulation_spk.S_Waerme_spk.ToString("F2"));
            //dgvErgebnisse.Rows.Add("Photovoltaik", (sim.simulation_pv.Stromproduktion_gesamt / 1000).ToString("F2"));
            dgvErgebnisse.Rows.Add("BHKW", (sim.simulation_bhkw.Waermeproduktion_gesamt / 1000).ToString("F2"));
        }

        private void bt_WaermebedarfUebersicht_Click(object sender, EventArgs e)
        {
            Form_ErgBrauchwasserwaerme frm = new Form_ErgBrauchwasserwaerme();
            frm.Init(sim.simulation_Waermebedarf);
            frm.SetPage(2);
            frm.ShowDialog();
        }

        private void SetupDataGridViewLook(DataGridView dgvErgebnisse)
        {
            // --- 1. System-Styles deaktivieren (WICHTIG für Header-Font & Color) ---
            dgvErgebnisse.EnableHeadersVisualStyles = false;

            // --- 2. Grund-Layout & Interaktion ---
            dgvErgebnisse.BackgroundColor = Color.White;
            dgvErgebnisse.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvErgebnisse.RowHeadersVisible = false;      // Versteckt die linke graue Spalte
            dgvErgebnisse.AllowUserToAddRows = false;     // Keine leere Zeile am Ende
            dgvErgebnisse.AllowUserToResizeRows = false;  // Zeilenhöhe fixieren
            dgvErgebnisse.AllowUserToOrderColumns = false; // Spalten festsetzen
            dgvErgebnisse.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvErgebnisse.GridColor = Color.FromArgb(235, 235, 235);
            dgvErgebnisse.BorderStyle = BorderStyle.None;
            dgvErgebnisse.CellBorderStyle = DataGridViewCellBorderStyle.Single;

            // --- 3. Zeilen-Styling (Farben & Font) ---
            // Wir definieren den Font hier einmal zentral
            Font rowFont = new Font("Segoe UI", 12.0f, FontStyle.Regular);

            dgvErgebnisse.DefaultCellStyle.Font = rowFont;
            dgvErgebnisse.DefaultCellStyle.ForeColor = Color.Black;
            dgvErgebnisse.DefaultCellStyle.BackColor = Color.White;

            // Selektions-Farbe (Hellblau wie gewünscht)
            dgvErgebnisse.DefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 245, 255);
            dgvErgebnisse.DefaultCellStyle.SelectionForeColor = Color.Black;

            // --- 4. Header-Styling (Blau, Weiss, Starr) ---
            DataGridViewCellStyle headerStyle = new DataGridViewCellStyle();
            headerStyle.BackColor = Color.FromArgb(0, 120, 215);
            headerStyle.ForeColor = Color.White;
            headerStyle.Font = new Font("Segoe UI", 12.0f, FontStyle.Regular);

            // Header-Selektion neutralisieren (bleibt blau/weiss beim Klick)
            headerStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            headerStyle.SelectionForeColor = Color.White;

            dgvErgebnisse.ColumnHeadersDefaultCellStyle = headerStyle;
            dgvErgebnisse.ColumnHeadersHeight = 35;
            dgvErgebnisse.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // Spalten-Spezifische Einstellungen (Sortierung & Alignment) ---
            // Dieser Teil setzt voraus, dass die Spalten bereits existieren (Columns.Add wurde gerufen)

            Font rowFont2 = new Font("Segoe UI", 10.0f, FontStyle.Regular); // Dein Wunsch-Font

            foreach (DataGridViewColumn col in dgvErgebnisse.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;

                //  Font direkt der Spalte zuweisen. 
                // Das überschreibt alle globalen Grid-Einstellungen!
                col.DefaultCellStyle.Font = rowFont2;
            }

            if (dgvErgebnisse.Columns.Count >= 2)
            {
                // Spalte 0: Erzeuger (Links)
                dgvErgebnisse.Columns[0].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                dgvErgebnisse.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

                // Spalte 1: Ergebnis (Rechtsbündig)
                dgvErgebnisse.Columns[1].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgvErgebnisse.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            // --- 6. Performance (Gegen Flimmern) ---
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.SetProperty,
                null, dgvErgebnisse, new object[] { true });
        }

        public void RefreshContent()
        {
            SetControl(this.sim);
            this.Invalidate();
        }

        public void SetControl(SimulationControl sim)
        {
            if (sim == null || sim.simulation_Waermebedarf == null) return;

            // Berechnung Wärme
            waerme_spk = sim.simulation_spk.spk_list.Sum(x => 0); // Platzhalter für deine Liste
            // Falls Sum() nicht geht, deine Schleife nutzen:
            waerme_spk = 0;
            for (int i = 0; i < sim.simulation_spk.spk_list.Count(); i++)
                waerme_spk += sim.simulation_spk.s_waerme_Gas_Spk[i] + sim.simulation_spk.s_waerme_Oel_Spk[i];

            waerme_wp = sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000;
            waerme_heizstab = sim.simulation_wp.Heizstab_gesamt / 1000;
            waerme_solar = sim.simulation_solarthermie.Waermeproduktion_gesamt / 1000;
            waerme_bhkw = sim.simulation_bhkw.waermeproduktion.Sum() / 1000;
            
            gesamt_waerme = waerme_spk + waerme_wp + waerme_heizstab + waerme_solar + waerme_bhkw;
            restwaermebedarf = sim.simulation_Waermebedarf.Waermebedarf_Gesamt - gesamt_waerme;

            FillTableWithData(dataGridView1);
        }

        private void NavigatorUebersicht_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.FromArgb(240, 240, 240));

            if (sim == null || sim.simulation_Waermebedarf == null) return;

            if (dataGridView1.Visible == false) dataGridView1.Visible = true;

            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // --- MAẞE DEFINIEREN (Das "Skelett" des Dashboards) ---
            int margin = 20;
            int kachelBreiteLinks = 300; // Breite für die Donut-Kacheln
            int kachelHoeheDonut = 250;  // Höhe für die Donut-Kacheln
            int spaltenAbstand = 25;     // Abstand zwischen linker und rechter Säule
            int rechtsX = margin + kachelBreiteLinks + spaltenAbstand;

            // --- LINKE SPALTE: DIAGRAMME ---

            // 1. Wärme-Deckung
            Rectangle rectWaermeChart = new Rectangle(margin, margin, kachelBreiteLinks, kachelHoeheDonut);
            Kacheln.DrawKPICard(e.Graphics, rectWaermeChart, "Wärmebedarfsdeckung [%]", "", "", Color.SeaGreen);

            double wb_gesamt = sim.simulation_Waermebedarf.Waermebedarf_Gesamt;
            double[] werteWaerme = {
                waerme_wp * 100 / wb_gesamt,
                waerme_solar * 100 / wb_gesamt,
                waerme_heizstab * 100 / wb_gesamt,
                waerme_spk * 100 / wb_gesamt,
                waerme_bhkw * 100 / wb_gesamt,
                Math.Max(0, restwaermebedarf * 100 / wb_gesamt)
            };
            string[] namenWaerme = { "Wärmepumpe", "Solarthermie", "Heizstab", "Spitzenkessel", "BHKW", "Restwärme" };

            Rectangle innerWaerme = new Rectangle(rectWaermeChart.X + 10, rectWaermeChart.Y + 20, rectWaermeChart.Width - 20, rectWaermeChart.Height - 30);
            DonutChartDrawer.DrawChartWithDynamicLegend(e.Graphics, innerWaerme, werteWaerme, (gesamt_waerme * 100 / wb_gesamt), namenWaerme, palette);

            // 2. Strom-Deckung (Direkt darunter)
            Rectangle rectStromChart = new Rectangle(margin, rectWaermeChart.Bottom + margin, kachelBreiteLinks, kachelHoeheDonut);
            Kacheln.DrawKPICard(e.Graphics, rectStromChart, "Strombedarfsdeckung [%]", "", "", Color.DodgerBlue);

            double se_pv = (sim.simulation_pv.Stromproduktion_gesamt) / 1000;
            double se_bhkw = (sim.simulation_bhkw.Stromproduktion_gesamt) / 1000;

            double sb_gesamt = 0;
            sb_gesamt = sim.simulation_Strombedarf.Strombedarf_gesamt + (sim.simulation_wp.WP_Strombedarf_gesamt + sim.simulation_wp.Heizstab_gesamt) / 1000;

            double[] werteStrom = new double[3];
            if (sb_gesamt > 0)
            {
                werteStrom[0] = se_pv * 100 / sb_gesamt;
                werteStrom[1] = se_bhkw * 100 / sb_gesamt;
                werteStrom[2] = Math.Max(0, (sb_gesamt - se_pv - se_bhkw) * 100 / sb_gesamt);
            }
            else
            {
                werteStrom[0] = 0;
                werteStrom[1] = 0;
                werteStrom[2] = 0;
            }

            string[] namenStrom = { "Photovoltaik", "BHKW", "Reststrom" };

            Rectangle innerStrom = new Rectangle(rectStromChart.X + 10, rectStromChart.Y + 20, rectStromChart.Width - 20, rectStromChart.Height - 30);
            
            if(sb_gesamt > 0)
                DonutChartDrawer.DrawChartWithDynamicLegend(e.Graphics, innerStrom, werteStrom, ((se_pv + se_bhkw) * 100 / sb_gesamt), namenStrom, palette);
            else
                DonutChartDrawer.DrawChartWithDynamicLegend(e.Graphics, innerStrom, werteStrom, 100, namenStrom, palette);


            // --- RECHTE SPALTE: KPIs & TABELLE ---
            int kpiBreite = 220;
            int kpiHoehe = 90;

            // KPI Reststrom
            //Rectangle rectKPIStrom = new Rectangle(rectKPIWaerme.Right + margin, margin, kpiBreite, kpiHoehe);
            Rectangle rectKPIStrom = new Rectangle(rechtsX, margin, kpiBreite, kpiHoehe);
            Kacheln.DrawKPICard(e.Graphics, rectKPIStrom, "Reststrombedarf", sim.Reststrom.ToString("N2"), "MWh/a", Color.DodgerBlue);
            
            // KPI Restwärme
            //Rectangle rectKPIWaerme = new Rectangle(rechtsX, margin, kpiBreite, kpiHoehe);
            Rectangle rectKPIWaerme = new Rectangle(rectKPIStrom.Right + margin, margin, kpiBreite, kpiHoehe);
            Kacheln.DrawKPICard(e.Graphics, rectKPIWaerme, "Restwärmebedarf", restwaermebedarf.ToString("N2"), "MWh/a", Color.SeaGreen);

            // button für Wärmebedarf Übersicht positionieren
            bt_WaermebedarfUebersicht.Top = margin;
            bt_WaermebedarfUebersicht.Left  = rectKPIWaerme.Left + kpiBreite + 20; 

            // TABELLEN-KACHEL (Der große Bereich darunter)
            int tabelleY = rectKPIWaerme.Bottom + margin;
            int tabelleBreite = Math.Max(500, this.Width - rechtsX - margin);
            int tabelleHoehe = Math.Max(300, rectStromChart.Bottom - tabelleY); // Passt sich der Höhe der Donuts an

            Rectangle rectTabelle = new Rectangle(rechtsX, tabelleY, tabelleBreite, tabelleHoehe);

            // eine "leere" KPI Card als Container für die Tabelle
            Kacheln.DrawKPICard(e.Graphics, rectTabelle, "Simulationsergebnisse im Detail", "", "", Color.Gray);

            // WICHTIG: Falls DataGridView existiert, richte es hier aus
            if (this.Controls.ContainsKey("dgvErgebnisse")) // Angenommen dein Grid heißt so
            {
                Control dgv = this.Controls["dgvErgebnisse"];
                dgv.Location = new Point(rectTabelle.X + 10, rectTabelle.Y + 45);
                dgv.Size = new Size(rectTabelle.Width - 20, rectTabelle.Height - 55);
                dgv.Visible = true;
            }
        }
    }
}
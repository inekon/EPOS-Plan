using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Welche Erzeuger gehören zu diesem Ergebnis? Vorbelegt mit „alles sichtbar",
        /// damit vor dem ersten <see cref="SetControl"/> nichts fehlt.
        /// </summary>
        private ErgebnisPraesenz _praesenz = ErgebnisPraesenz.Alles();

        // Donut Farben (WP, Solar, Heizstab, Kessel, Rest)
        Color[] palette = new Color[] {
            ColorTranslator.FromHtml("#2ECC71"), // WP
            ColorTranslator.FromHtml("#E67E22"), // Solar
            ColorTranslator.FromHtml("#F1C40F"), // Heizstab
            ColorTranslator.FromHtml("#95A5A6"), // Kessel
            ColorTranslator.FromHtml("#75A5A6"), // BHKW
            ColorTranslator.FromHtml("#3498DB"), // Rest
            ColorTranslator.FromHtml("#9B59B6")  // Speicherentladung (AP3b)
        };

        public NavigatorUebersicht(SimulationControl simctrl)
        {
            InitializeComponent();
            BeschriftungenSetzen();
            sim = simctrl;
            this.DoubleBuffered = true;
            this.Paint += new PaintEventHandler(NavigatorUebersicht_Paint);

            // Reagiert auf Größenänderungen des Fensters
            this.Resize += (s, e) => this.Invalidate();

            // --- WICHTIG: Erst Spalten definieren ---
            dataGridView1.Columns.Clear();

            // AutoGenerateColumns weglassen oder auf true
            // dataGridView1.AutoGenerateColumns = true; 

            // Spalte 0. Name ist der technische Zugriffsschlüssel (Schicht 2),
            // HeaderText die Anzeige (Schicht 3) - beides bewusst getrennt.
            var colErzeuger = new DataGridViewTextBoxColumn
            {
                HeaderText = MyResource.Resource.SIM_SPALTE_ENERGIE_ERZEUGER,
                Name = "Erzeuger",
                FillWeight = 150 // Nutze FillWeight statt fester Breite für AutoSizeMode.Fill
            };

            // Spalte 1
            var colErgebnis = new DataGridViewTextBoxColumn
            {
                HeaderText = MyResource.Resource.SIM_SPALTE_ERGEBNIS_MWH,
                Name = "Ergebnis",
                FillWeight = 100
            };

            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { colErzeuger, colErgebnis });

            // --- DANN das Styling aufrufen (weil Spalten jetzt existieren) ---
            SetupDataGridViewLook(dataGridView1);
        }

        /// <summary>
        /// Setzt die im Designer angelegten Beschriftungen aus dem Ressourcenkatalog
        /// (Paket 9 / L7). Zur Begründung, warum das programmatisch und nicht über eine
        /// <c>Localizable</c>-Designer-Ressource geschieht, siehe
        /// <see cref="NavigatorStrom"/>.
        ///
        /// <c>label_1</c> und <c>label_2</c> bleiben unangetastet: beide stehen im
        /// Designer auf <c>Visible = false</c> und werden nirgends eingeblendet.
        /// </summary>
        private void BeschriftungenSetzen()
        {
            bt_WaermebedarfUebersicht.Text = MyResource.Resource.SIM_BTN_WAERMEBEDARF_UEBERSICHT;
        }

        /// <summary>
        /// Ergebnistabelle — nur die Erzeuger, die zum Ergebnis gehören.
        ///
        /// Bis hierher standen alle fünf Zeilen fest in der Tabelle; ein Projekt aus
        /// Wärmepumpe und Kessel zeigte „Solarthermieanlage 0,00" und „BHKW 0,00" mit.
        /// Die Zeilen fehlender Erzeuger entfallen jetzt (Regel siehe ErgebnisPraesenz);
        /// die Tabelle rückt dabei von selbst nach, weil sie zeilenweise aufgebaut wird.
        /// </summary>
        private void FillTableWithData(DataGridView dgvErgebnisse)
        {
            dgvErgebnisse.Rows.Clear();
            if (_praesenz.Waermepumpe)
                dgvErgebnisse.Rows.Add(MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE, waerme_wp.ToString("F2"));
            if (_praesenz.Heizstab)
                dgvErgebnisse.Rows.Add(MyResource.Resource.CHART_SEGMENT_HEIZSTAB, waerme_heizstab.ToString("F2"));
            if (_praesenz.Solarthermie)
                dgvErgebnisse.Rows.Add(MyResource.Resource.SIM_SOLARTHERMIE_ANLAGE, waerme_solar.ToString("F2"));
            if (_praesenz.Heizkessel)
                dgvErgebnisse.Rows.Add(MyResource.Resource.SIM_TABELLE_HEIZKESSEL, waerme_spk.ToString("F2"));
            if (_praesenz.BHKW)
                dgvErgebnisse.Rows.Add(MyResource.Resource.SIM_ERZEUGERNAME_BHKW, waerme_bhkw.ToString("F2"));
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

            // Welche Erzeuger gehören zu diesem Ergebnis? Tabelle und Donut-Segmente
            // richten sich danach (siehe ErgebnisPraesenz).
            _praesenz = ErgebnisPraesenz.Ermitteln(sim);

            // Berechnung Wärme
            waerme_spk = sim.simulation_spk.S_Waerme_spk;
            waerme_wp = sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000;
            waerme_heizstab = sim.simulation_wp.Heizstab_gesamt / 1000;
            waerme_solar = sim.simulation_solarthermie.Waermeproduktion_gesamt / 1000;
            waerme_bhkw = sim.simulation_bhkw.Waermeproduktion_BHKW_MWh;
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
            Kacheln.DrawKPICard(e.Graphics, rectWaermeChart, MyResource.Resource.CHART_KACHEL_WAERMEBEDARFSDECKUNG, "", "", Color.SeaGreen);

            // Segmente nur für vorhandene Erzeuger. Werte, Namen und Farben werden GEMEINSAM
            // gefiltert - die Farbzuordnung des Donuts läuft über die Position im Array,
            // ein Filtern allein der Werte hätte die Legende umgefärbt.
            double wb_gesamt = sim.simulation_Waermebedarf.Waermebedarf_Gesamt;
            var segWaerme = new List<Tuple<double, string, Color>>();
            if (_praesenz.Waermepumpe)
                segWaerme.Add(Tuple.Create(waerme_wp * 100 / wb_gesamt,
                    MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE, palette[0]));
            if (_praesenz.Solarthermie)
                segWaerme.Add(Tuple.Create(waerme_solar * 100 / wb_gesamt,
                    MyResource.Resource.SIM_ERZEUGERNAME_SOLARTHERMIE, palette[1]));
            if (_praesenz.Heizstab)
                segWaerme.Add(Tuple.Create(waerme_heizstab * 100 / wb_gesamt,
                    MyResource.Resource.CHART_SEGMENT_HEIZSTAB, palette[2]));
            if (_praesenz.Heizkessel)
                segWaerme.Add(Tuple.Create(waerme_spk * 100 / wb_gesamt,
                    MyResource.Resource.CHART_SEGMENT_SPITZENKESSEL, palette[3]));
            if (_praesenz.BHKW)
                segWaerme.Add(Tuple.Create(waerme_bhkw * 100 / wb_gesamt,
                    MyResource.Resource.SIM_ERZEUGERNAME_BHKW, palette[4]));
            // Der Rest beschreibt das Projekt, nicht einen Erzeuger - er bleibt immer.
            segWaerme.Add(Tuple.Create(Math.Max(0, restwaermebedarf * 100 / wb_gesamt),
                MyResource.Resource.CHART_SEGMENT_RESTWAERME, palette[5]));

            double[] werteWaerme = segWaerme.Select(s => s.Item1).ToArray();
            string[] namenWaerme = segWaerme.Select(s => s.Item2).ToArray();
            Color[] farbenWaerme = segWaerme.Select(s => s.Item3).ToArray();

            Rectangle innerWaerme = new Rectangle(rectWaermeChart.X + 10, rectWaermeChart.Y + 20, rectWaermeChart.Width - 20, rectWaermeChart.Height - 30);
            DonutChartDrawer.DrawChartWithDynamicLegend(e.Graphics, innerWaerme, werteWaerme, (gesamt_waerme * 100 / wb_gesamt), namenWaerme, farbenWaerme);

            // 2. Strom-Deckung (Direkt darunter)
            Rectangle rectStromChart = new Rectangle(margin, rectWaermeChart.Bottom + margin, kachelBreiteLinks, kachelHoeheDonut);
            Kacheln.DrawKPICard(e.Graphics, rectStromChart, MyResource.Resource.CHART_KACHEL_STROMBEDARFSDECKUNG, "", "", Color.DodgerBlue);

            double se_pv = (sim.simulation_pv.Stromproduktion_gesamt) / 1000;
            double se_bhkw = sim.simulation_bhkw.Stromproduktion_BHKW_MWh;
            double se_spk = sim.simulation_spk.Stromverbrauch_Spk;

            // Speicherentladung [MWh/a] als eigener Deckungsanteil (AP3b, Fachkonzept 7.1).
            // Keine Doppelzählung mit der PV-Zeile: se_pv führt seit dem Rückbau in AP2b
            // ausschließlich den DIREKTverbrauch (SimulationPV: Stromproduktion =
            // min(Erzeugung, Bedarf)); was über den Speicher läuft, steckt dort nicht drin.
            //
            // Die eigene Null-Prüfung neben dem Präsenzflag ist nötig: Vor dem ersten
            // SetControl gilt ErgebnisPraesenz.Alles(), und ein Speicherergebnis gibt es
            // dann noch nicht - anders als bei simulation_pv/-bhkw, die immer instanziiert sind.
            bool zeigeSpeicher = _praesenz.Stromspeicher && sim.Speicherergebnis != null;
            double se_speicher = zeigeSpeicher ? sim.Speicherergebnis.EntladeenergieKwh / 1000.0 : 0.0;

            double sb_gesamt = 0;
            sb_gesamt = sim.simulation_Strombedarf.Strombedarf_gesamt
                        + (sim.simulation_wp.WP_Strombedarf_gesamt
                        + sim.simulation_wp.Heizstab_gesamt
                        + sim.simulation_spk.Stromverbrauch_Spk
                        );

            // Segmente wie beim Wärme-Donut: nur vorhandene Erzeuger, Werte/Namen/Farben
            // gemeinsam gefiltert. Der Reststrom bleibt immer.
            var segStrom = new List<Tuple<double, string, Color>>();
            if (_praesenz.Photovoltaik)
                segStrom.Add(Tuple.Create(sb_gesamt > 0 ? se_pv * 100 / sb_gesamt : 0,
                    MyResource.Resource.SIM_PHOTOVOLTAIK, palette[0]));
            if (_praesenz.BHKW)
                segStrom.Add(Tuple.Create(sb_gesamt > 0 ? se_bhkw * 100 / sb_gesamt : 0,
                    MyResource.Resource.SIM_ERZEUGERNAME_BHKW, palette[1]));
            // Eigenes Segment für den Speicher - er ist keine Erzeugung, deckt aber
            // Bedarf und gehört damit sichtbar in die Strom-Deckung.
            if (zeigeSpeicher)
                segStrom.Add(Tuple.Create(sb_gesamt > 0 ? se_speicher * 100 / sb_gesamt : 0,
                    MyResource.Resource.CHART_SEGMENT_SPEICHERENTLADUNG, palette[6]));
            segStrom.Add(Tuple.Create(
                sb_gesamt > 0 ? Math.Max(0, (sb_gesamt - se_pv - se_bhkw - se_speicher) * 100 / sb_gesamt) : 0,
                MyResource.Resource.CHART_SEGMENT_RESTSTROM, palette[2]));

            double[] werteStrom = segStrom.Select(s => s.Item1).ToArray();
            string[] namenStrom = segStrom.Select(s => s.Item2).ToArray();
            Color[] farbenStrom = segStrom.Select(s => s.Item3).ToArray();

            Rectangle innerStrom = new Rectangle(rectStromChart.X + 10, rectStromChart.Y + 20, rectStromChart.Width - 20, rectStromChart.Height - 30);

            if(sb_gesamt > 0)
                DonutChartDrawer.DrawChartWithDynamicLegend(e.Graphics, innerStrom, werteStrom, ((se_pv + se_spk + se_bhkw + se_speicher) * 100 / sb_gesamt), namenStrom, farbenStrom);
            else
                DonutChartDrawer.DrawChartWithDynamicLegend(e.Graphics, innerStrom, werteStrom, 100, namenStrom, farbenStrom);


            // --- RECHTE SPALTE: KPIs & TABELLE ---
            int kpiBreite = 220;
            int kpiHoehe = 90;

            // KPI Reststrom
            //Rectangle rectKPIStrom = new Rectangle(rectKPIWaerme.Right + margin, margin, kpiBreite, kpiHoehe);
            Rectangle rectKPIStrom = new Rectangle(rechtsX, margin, kpiBreite, kpiHoehe);
            Kacheln.DrawKPICard(e.Graphics, rectKPIStrom, MyResource.Resource.SIM_KACHEL_RESTSTROMBEDARF, sim.Reststrom.ToString("N2"), "MWh/a", Color.DodgerBlue);
            
            // KPI Restwärme
            //Rectangle rectKPIWaerme = new Rectangle(rechtsX, margin, kpiBreite, kpiHoehe);
            Rectangle rectKPIWaerme = new Rectangle(rectKPIStrom.Right + margin, margin, kpiBreite, kpiHoehe);
            Kacheln.DrawKPICard(e.Graphics, rectKPIWaerme, MyResource.Resource.SIM_KACHEL_RESTWAERMEBEDARF, restwaermebedarf.ToString("N2"), "MWh/a", Color.SeaGreen);

            // button für Wärmebedarf Übersicht positionieren
            bt_WaermebedarfUebersicht.Top = margin;
            bt_WaermebedarfUebersicht.Left  = rectKPIWaerme.Left + kpiBreite + 20; 

            // TABELLEN-KACHEL (Der große Bereich darunter)
            int tabelleY = rectKPIWaerme.Bottom + margin;
            int tabelleBreite = Math.Max(500, this.Width - rechtsX - margin);
            int tabelleHoehe = Math.Max(300, rectStromChart.Bottom - tabelleY); // Passt sich der Höhe der Donuts an

            Rectangle rectTabelle = new Rectangle(rechtsX, tabelleY, tabelleBreite, tabelleHoehe);

            // eine "leere" KPI Card als Container für die Tabelle
            Kacheln.DrawKPICard(e.Graphics, rectTabelle, MyResource.Resource.SIM_KACHEL_SIMULATIONSERGEBNISSE, "", "", Color.Gray);

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
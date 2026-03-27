using Json.Schema.Generation.Intents;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Main_PV_Test : Form
    {
        public CECDataService _cecSvc = new CECDataService();
        public PanDataService _panSvc = new PanDataService();
        private BindingSource _moduleBindingSource = new BindingSource();
        public List<PVModule> listPVModules = new List<PVModule>();
        public UnifiedModule pvum = new UnifiedModule();

        private bool m_bCEC = true;

        public Main_PV_Test()
        {
            InitializeComponent();

            // Erst nach Initialisierung die Distance setzen (vermeidet Exception)
            this.Load += (s, e) =>
            {
                _splitContainer.SplitterDistance = (int)(this.Width * 0.66);
                MakeSmooth(_splitContainer);
            };

            _splitContainer.SplitterWidth = 2; // Ein schmaler, moderner Balken
            _splitContainer.BorderStyle = BorderStyle.None;

            tabControl1.Appearance = TabAppearance.Normal;

            _txtSearch.SetPlaceholder("z.B.  Trina*  oder  *410*  oder  *mono*2022*");

            _btnFilter.ForeColor = Color.White;
            _btnReset.ForeColor = Color.White;

            _btnFilter.BackColor = Color.FromArgb(0, 120, 215);
            _btnReset.BackColor = Color.FromArgb(180, 40, 40);
            _bottomPanel.BackColor = Color.FromArgb(230, 235, 240);

            btnSelect.MakeSmoothButton(btnSelect.Height / 4);
            btnCancel.MakeSmoothButton(btnCancel.Height / 4);

            btnSelect.FlatAppearance.BorderSize = 0;
            btnCancel.FlatAppearance.BorderSize = 0;

            Nud(num_PMin, 0, 999, 0, 2); Nud(num_PMax, 0, 999, 999, 2);
            Nud(num_EffMin, 0, 100, 0, 2); Nud(num_EffMax, 0, 100, 50, 2);

            // Erst die Struktur festlegen.. Grid-Struktur vorbereiten (Spaltenüberschriften etc.)
            SetupGridColumns();

            // BindingSource mit dem Grid verknüpfen
            _dgvModules.DataSource = _moduleBindingSource;
            _dgvModules.RowHeadersVisible = false;

            // Header-Styling (optional für besseren Look)
            _dgvModules.EnableHeadersVisualStyles = false;
            _dgvModules.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 87, 153);
            _dgvModules.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;

            _dgvModules.ReadOnly = true;                // Verhindert das Tippen in Zellen
            _dgvModules.AllowUserToAddRows = false;     // Entfernt die leere Sternchen-Zeile am Ende
            _dgvModules.AllowUserToDeleteRows = false;  // Verhindert das Löschen mit der Entf-Taste
            _dgvModules.EditMode = DataGridViewEditMode.EditProgrammatically; // Deaktiviert das automatische Öffnen von Editoren
            _dgvModules.MultiSelect = false; // Verhindert die Auswahl mehrerer Zeilen (auch mit Strg/Shift)

            statusStrip1.Items[0].Text = "Bereit. Bitte CEC Datenbank oder PAN Datei laden.";
        }

        // Hilfsmethode für flüssigeres Zeichnen beim Resizen
        private void MakeSmooth(Control container)
        {
            var prop = typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (Control c in container.Controls)
            {
                prop?.SetValue(c, true, null);
                if (c.HasChildren) MakeSmooth(c);
            }
        }

        private async void _btnCEC_Click(object sender, EventArgs e)
        {
            m_bCEC = true;
            await LoadCecAsync();
            PopulateFilters();
            statusStrip1.Items[0].Text = $"Filter Auswahl ({_dgvModules.RowCount} Module gefunden)";
        }

        // ==============================================================
        //  Laden
        // ==============================================================
        private async Task LoadCecAsync()
        {

            var (ok, msg) = await _cecSvc.LoadDataAsync();
            if (ok)
            {
                RefreshModuleGrid();
            }
            else
            {
                MessageBox.Show(msg, "CEC – Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RefreshModuleGrid()
        {
            try
            {
                // Die Rohdaten laden
                if (m_bCEC)
                    listPVModules = (List<PVModule>)_cecSvc.AllModules;
                else
                   listPVModules = (List<PVModule>)_panSvc.AllModules;

                if (listPVModules == null || listPVModules.Count == 0)
                {
                    MessageBox.Show("Keine Daten im CEC-Service gefunden.");
                    return;
                }

                // Umwandeln in die "schönen" UnifiedModules
                // Hier wird die FromPanCec-Methode für jedes Modul aufgerufen.
                // Dabei wird Pmp = I * V berechnet und in die neue Liste geschrieben.
                List<UnifiedModule> displayList;
                displayList = listPVModules.Select(m => UnifiedModule.FromPanCec(m)).ToList();

                // NUR die displayList an das Grid binden
                // Die BindingSource sorgt dafür, dass das Grid die Änderung mitbekommt.
                _moduleBindingSource.DataSource = displayList;

                // Optional: Das Grid zwingen, die Anzeige zu aktualisieren
                _moduleBindingSource.ResetBindings(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Laden der Daten: " + ex.Message);
            }
        }

        private void PopulateFilters()
        {
            comboBox_Hersteller.Items.Clear(); comboBox_Hersteller.Items.Add("(alle)");
            comboBox_Technologie.Items.Clear(); comboBox_Technologie.Items.Add("(alle)");

            if (m_bCEC)
            {
                var allMfg = _cecSvc.GetManufacturers().Where(x => !string.IsNullOrEmpty(x))
                    .OrderBy(x => x);
                var allTech = _cecSvc.GetTechnologies().Where(x => !string.IsNullOrEmpty(x))
                    .OrderBy(x => x);
                foreach (var m in allMfg) comboBox_Hersteller.Items.Add(m);
                foreach(var t in allTech) comboBox_Technologie.Items.Add(t);
            }
            else
            {
                var allMfg = _panSvc.GetManufacturers().Where(x => !string.IsNullOrEmpty(x))
                    .OrderBy(x => x);
                var allTech = _panSvc.GetTechnologies().Where(x => !string.IsNullOrEmpty(x))
                    .OrderBy(x => x);
                foreach (var m in allMfg) comboBox_Hersteller.Items.Add(m);
                foreach (var t in allTech) comboBox_Technologie.Items.Add(t);
            }
  
            comboBox_Hersteller.SelectedIndex = 0;
            comboBox_Technologie.SelectedIndex = 0;
        }

        private void Nud(NumericUpDown ctrl, decimal min, decimal max, decimal val, int w)
        {
            ctrl.Minimum = min;
            ctrl.Maximum = max;
            ctrl.Value = val;
            ctrl.Width = 60;
            ctrl.DecimalPlaces = w;
        }

        private void ApplyFilter()
        {
            // 1. Strings/Regex für Textfelder holen
            Regex nameRegex = GetFilterRegex(_txtSearch.Text);
            Regex mfgRegex = GetFilterRegex(comboBox_Hersteller.Text);
            Regex techRegex = GetFilterRegex(comboBox_Technologie.Text);

            // 2. Werte von den NumericUpDown-Controls holen
            // Wir casten auf double, da Pmp und Efficiency in der Klasse double sind
            double minP = (double)num_PMin.Value;
            double maxP = (double)num_PMax.Value;
            double minE = (double)num_EffMin.Value;
            double maxE = (double)num_EffMax.Value;

            // 3. Filtere die Rohdaten
            var filteredRaw = listPVModules.Where(x =>
            {
                // TEXT-FILTER (Regex)
                bool nameMatch = nameRegex == null || (x.Name != null && nameRegex.IsMatch(x.Name));
                bool mfgMatch = mfgRegex == null || (x.Manufacturer != null && mfgRegex.IsMatch(x.Manufacturer));
                // Technologie-Check: wahr wenn kein Regex vorhanden ODER Match
                bool techMatch = techRegex == null || (x.Technology != null && techRegex.IsMatch(x.Technology));

                // Da Pmp und Efficiency in UnifiedModule berechnet werden, 
                // müssen wir sie hier kurz für den Vergleich berechnen:
                double currentPmp = x.I_mp_ref * x.V_mp_ref;
                double currentEff = x.Efficiency;

                // NUMERISCHE FILTER (Range-Check)
                // Wenn Max auf 0 steht, ignorieren wir den Max-Filter oft (optional)
                bool pmpMatch = currentPmp >= minP && (maxP <= 0 || currentPmp <= maxP);
                bool effMatch = currentEff >= minE && (maxE <= 0 || currentEff <= maxE);

                return nameMatch && mfgMatch && techMatch && pmpMatch && effMatch;
            }).ToList();

   
            // Umwandeln in UnifiedModules (für das Grid)
            var filteredUnified = filteredRaw
                .Select(m => UnifiedModule.FromPanCec(m))
                .ToList();

            // BindingSource aktualisieren
            _moduleBindingSource.DataSource = filteredUnified;

            // UI Feedback
            UpdateGridBackground(filteredRaw.Count);
            statusStrip1.Items[0].Text = $"Filter Auswahl ({filteredRaw.Count} Module gefunden)";
        }

        private Regex GetFilterRegex(string input)
        {
            string text = input?.Trim();

            // Wir prüfen jetzt zusätzlich auf "(alle)" (case-insensitive)
            if (string.IsNullOrEmpty(text) ||
                text == "*" ||
                text.Equals("(alle)", StringComparison.OrdinalIgnoreCase))
            {
                return null; // Kein Filter notwendig
            }

            try
            {
                // Wildcard-Logik: Ersetzt * durch .* und ? durch .
                string pattern = "^" + Regex.Escape(text).Replace("\\*", ".*").Replace("\\?", ".") + "$";

                // Wenn keine Wildcards da sind, machen wir eine einfache Teilsuche (Contains)
                if (!text.Contains("*") && !text.Contains("?"))
                {
                    pattern = Regex.Escape(text);
                }

                return new Regex(pattern, RegexOptions.IgnoreCase);
            }
            catch
            {
                return null;
            }
        }

        private void UpdateGridBackground(int count)
        {
            _dgvModules.BackgroundColor = (count == 0) ? Color.WhiteSmoke : SystemColors.AppWorkspace;
        }

        private void _btnFilter_Click(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void _btnReset_Click(object sender, EventArgs e)
        {
            _txtSearch.Text = "";
            comboBox_Hersteller.Text = "(alle)";
            comboBox_Technologie.Text = "(alle)";
            Nud(num_PMin, 0, 999, 0, 2); Nud(num_PMax, 0, 999, 999, 2);
            Nud(num_EffMin, 0, 100, 0, 2); Nud(num_EffMax, 0, 100, 50, 2);
            ApplyFilter();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void SetupGridColumns()
        {
            // Verhindert das automatische Erzeugen aller Properties als Spalten
            _dgvModules.AutoGenerateColumns = false;
            _dgvModules.Columns.Clear();

            // Spalte: Quelle
            _dgvModules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Database",
                HeaderText = "Quelle",
                Name = "colName",
                Width = 50,
            });

            // Spalte: Modellname
            _dgvModules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Name",
                HeaderText = "Modulname",
                Name = "colName",
                Width = 180,
            });

            // Spalte: Hersteller
            _dgvModules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Manufacturer",
                HeaderText = "Hersteller",
                Name = "colManufacturer",
            });
            // Spalte: Technologie
            _dgvModules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Technology",
                HeaderText = "Technologie",
                Name = "colManufacturer",
            });


            // Spalte: Leistung (W)
            _dgvModules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Pmp",
                HeaderText = "Pmp (W)",
                Name = "colPmp",
                Width = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N1", Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            // Spalte: Wirkungsgrad (%)
            _dgvModules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Efficiency",
                HeaderText = "Effizienz (%)",
                Name = "colEff",
                Width = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight }

            });

            // Spalte: Isc
            _dgvModules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Isc",
                HeaderText = "Isc [A]",
                Name = "colIsc",
                Width = 80,
            });

            // Spalte: Bifazial
            _dgvModules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Bifacial",
                HeaderText = "Bifazial",
                Name = "colBifazial",
                Width = 50,
            });

            // Spalte: Bifazial
            _dgvModules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Voc",
                HeaderText = "Voc [V]",
                Name = "colVoc",
                Width = 50,
            });

            // Spalte: Date
            _dgvModules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Date",
                HeaderText = "Jahr",
                Name = "colDate",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill // Nimmt den Restplatz ein
            });
        }

        private void _dgvModules_SelectionChanged(object sender, EventArgs e)
        {
            if (_dgvModules.SelectedRows.Count == 0) return;
            if (_dgvModules.SelectedRows[0].DataBoundItem is UnifiedModule um) ShowDetail(um);
        }

        private void ShowDetail(UnifiedModule um)
        {
            pvum = um;
            textBox_1.Text = um.Name;
            textBox_2.Text = um.Manufacturer;
            textBox_3.Text = um.Technology;
            textBox_4.Text = um.Pmp.ToString("F2");
            textBox_5.Text = um.Efficiency.ToString("F2");
            textBox_6.Text = um.Bifacial;
            textBox_7.Text = um.Database == "CEC" ? um.CecModule.A_c.ToString("F2") : um.PanModule.Area.ToString("F2");
            textBox_8.Text = um.Database == "CEC" ? um.CecModule.Length.ToString() : um.PanModule.Height.ToString("F2");
            textBox_9.Text = um.Database == "CEC" ? um.CecModule.Width.ToString() : um.PanModule.Width.ToString("F2");
            textBox_10.Text = um.Date.ToString();
            textBox_11.Text = um.Isc.ToString();
            textBox_12.Text = um.Voc.ToString();
            textBox_13.Text = um.Imp.ToString();
            textBox_14.Text = um.Vmp.ToString();
            textBox_15.Text = um.Pmp.ToString();
            textBox_16.Text = um.Database == "CEC" ? um.CecModule.alpha_sc.ToString() : "-";//um.PanModule.muISC.ToString();
            textBox_17.Text = um.Database == "CEC" ? um.CecModule.beta_oc.ToString() : "-"; //um.PanModule.muVocSpec.ToString();
            textBox_18.Text = um.Database == "CEC" ? um.CecModule.gamma_pmp.ToString() : um.PanModule.muPmpReq.ToString();
            textBox_19.Text = um.Database == "CEC" ? um.CecModule.STC.ToString() : um.PanModule.PNom.ToString();

            if (um.Database == "PAN")
            {
                double tempVerlust = (um.PanModule.muPmpReq / 100.0) * (45 - 25); // -0.00394 * 20 = -0.0788
                double pPTC = um.PanModule.PNom * (1 + tempVerlust);
                textBox_20.Text = pPTC.ToString();
            }
            else textBox_20.Text = um.CecModule.PTC.ToString();
            textBox_21.Text = um.Database == "CEC" ? um.CecModule.T_NOCT.ToString() : "-";
            
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            OdbcTransaction transaction = null;

            if (pvum.Name == null)
            {
                MessageBox.Show("Bitte ein PV-Modul selektieren!");
                return;
            }

            rs.Open("select * from [Tab_PV] where Modulname='" + pvum.Name + "'");
            if (rs.Next()) { MessageBox.Show("Daten bereits eingelesen!"); rs.Close(); return; }
            rs.Close();

            try
            {
                transaction = Program.DBConnection.BeginTransaction();
                rs.DBCommand.Transaction = transaction;
                rs.Insert("INSERT INTO [Tab_PV] (Modulname) SELECT '" + pvum.Name + "' AS Ausdr1");
                rs.Close();

                PhotovoltaikCtrl ctrl = new PhotovoltaikCtrl();
                
                ctrl.model = InitDatensatzUpdate();
                ctrl.DBCommand.Transaction = transaction;

                if (ctrl.Update())
                {
                    transaction.Commit();
                    //this.DialogResult = DialogResult.OK;
                    MessageBox.Show("Datensatz gespeichert");
                }
                else
                {
                    transaction.Rollback();
                    //this.DialogResult = DialogResult.Cancel;
                    MessageBox.Show("Fehler beim Speichern des Datensatzes!");
                }
                //Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                try
                {
                    // Attempt to roll back the transaction.
                    transaction.Rollback();
                }
                catch
                {
                    // Do nothing here; transaction is not active.
                }
            }
        }

        PhotovoltaikModel InitDatensatzUpdate()
        {
            PhotovoltaikModel model = new PhotovoltaikModel();

            model.m_szName = pvum.Name;
            model.m_szFirma = pvum.Manufacturer;
            model.m_Leistung = pvum.Pmp;
            model.m_Wirkungsgrad = pvum.Efficiency;
            model.m_U_Mpp = pvum.Vmp;
            model.m_U_Leerlauf = pvum.Voc;
            model.m_I_Mpp = pvum.Imp;
            model.m_I_Kurzschluss = pvum.Isc;
            
            if (pvum.Database =="CEC")
            { 
                model.m_alpha_SC = pvum.CecModule.alpha_sc;
                model.m_beta_OC = pvum.CecModule.beta_oc;
                model.m_Temp_Coeff_Pmax = pvum.CecModule.gamma_pmp;
                model.m_T_NOCT = pvum.CecModule.T_NOCT;
                model.m_Laenge = pvum.CecModule.Length;
                model.m_Breite = pvum.CecModule.Width;
            }
            else {
                model.m_alpha_SC = 0;//pvum.PanModule.muISC;
                model.m_beta_OC = 0;//pvum.PanModule.muVocSpec;
                model.m_Temp_Coeff_Pmax = pvum.PanModule.muPmpReq;
                model.m_T_NOCT = 0;
                model.m_Laenge = pvum.PanModule.Height;
                model.m_Breite = pvum.PanModule.Width;
            }
            return model;
        }

        private void _btnPAN_Click(object sender, EventArgs e)
        {
            m_bCEC = false;

            string dateiPfad = "";

            string szAppDataPath = Path.Combine(Program.ApplicationPath_User, "PAN");

            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = szAppDataPath;
            openFileDialog.Filter = "(*.pan)|*.pan";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                dateiPfad = openFileDialog.FileName;
                string inhalt = File.ReadAllText(dateiPfad, Encoding.Default);
                PanModule m = PanDataService.ParsePan(inhalt);

                RefreshModuleGrid();
                PopulateFilters();
            }
        }

    }

    public class HeaderGradientPanel : Panel
    {
        public HeaderGradientPanel()
        {
            this.DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Sicherstellen, dass die Größe gültig ist
            if (this.Width <= 0 || this.Height <= 0) return;

            using (LinearGradientBrush brush = new LinearGradientBrush(
                this.ClientRectangle,
                Color.FromArgb(30, 87, 153),
                Color.FromArgb(16, 52, 110),
                LinearGradientMode.Horizontal))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }

            TextRenderer.DrawText(e.Graphics, this.Text, this.Font,
                new Rectangle(20, 0, this.Width, this.Height),
                this.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
  
    }
}
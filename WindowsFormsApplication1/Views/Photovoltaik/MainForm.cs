using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class MainForm : Form
    {
        // ── Datenservices ──────────────────────────────────────────────
        private readonly CECDataService _cecSvc = new CECDataService();
        private List<UnifiedModule> _filtered = new List<UnifiedModule>();
        private bool _showCEC = true;

        // ── Toolbar ────────────────────────────────────────────────────
        private Button _btnCecLoad = null;
        private Button _btnCecFile = null;

        // ── Filter ─────────────────────────────────────────────────────
        private TextBox _txtName = null;
        private ComboBox _cmbMfg = null;
        private ComboBox _cmbTech = null;
        private NumericUpDown _nMinP = null;
        private NumericUpDown _nMaxP = null;
        private NumericUpDown _nMinE = null;
        private NumericUpDown _nMaxE = null;
        private CheckBox _chkBif = null;
        private Button _btnFind = null;
        private Button _btnReset = null;
        private Button _btnOk;
        private Button _btnCancel;

        // ── Grid ───────────────────────────────────────────────────────
        private DataGridView _grid = null;
        private Label _lblCount = null;

        // ── Detail ─────────────────────────────────────────────────────
        private TabControl _tabs = null;
        private readonly Dictionary<string, Label> _dv = new Dictionary<string, Label>();

        // ── Status ─────────────────────────────────────────────────────
        private StatusStrip _strip = null;
        private ToolStripStatusLabel _lblSt = null;
        private ToolStripProgressBar _prog = null;

        // ── Farben ─────────────────────────────────────────────────────
        private static readonly Color C_CEC = Color.FromArgb(30, 87, 153);
        private static readonly Color C_GREEN = Color.FromArgb(20, 130, 70);
        private static readonly Color C_RED = Color.FromArgb(180, 40, 40);

        public MainForm()
        {
            Text = "PV-Module Import";
            Size = new Size(1480, 940);
            MinimumSize = new Size(1150, 720);
            StartPosition = FormStartPosition.CenterScreen;
            Icon = SystemIcons.Information;
            BuildUI();
        }

        private void BuildUI()
        {
            Font fBase = new Font("Segoe UI", 9f);
            Font fBold = new Font("Segoe UI", 9f, FontStyle.Bold);
            Font fTitle = new Font("Segoe UI", 14f, FontStyle.Bold);

            _strip = new StatusStrip();
            _lblSt = new ToolStripStatusLabel("Bereit. Bitte Datenbank laden.");
            _prog = new ToolStripProgressBar { Visible = false, Width = 200 };
            _strip.Items.AddRange(new ToolStripItem[] { _lblSt, _prog });

            Panel hdr = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = C_CEC };
            hdr.Paint += (object s, PaintEventArgs e) => {
                using (System.Drawing.Drawing2D.LinearGradientBrush br = new System.Drawing.Drawing2D.LinearGradientBrush(
                    hdr.ClientRectangle, C_CEC, Color.FromArgb(16, 52, 110),
                    System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(br, hdr.ClientRectangle);
                }
            };

            hdr.Controls.Add(new Label
            {
                Text = "⚡  PV-Modul Import  —  CEC",
                Font = fTitle,
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(12, 10)
            });

            ToolStrip toolbar = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = Color.FromArgb(240, 244, 248),
                Padding = new Padding(4, 2, 4, 2)
            };
            _btnCecLoad = TBtn("🌐 CEC laden", Color.FromArgb(0, 120, 215));
            _btnCecFile = TBtn("📂 CEC CSV…", Color.FromArgb(70, 105, 140));

            toolbar.Items.Add(new ToolStripControlHost(_btnCecLoad));
            toolbar.Items.Add(new ToolStripControlHost(_btnCecFile));
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(new ToolStripControlHost(new Label
            {
                Text = "Anzeige:",
                Font = fBold,
                AutoSize = true,
                Padding = new Padding(4, 4, 0, 0)
            }));

            Panel fltPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 142,
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10, 6, 10, 4)
            };

            FlowLayoutPanel row1 = Row();
            _txtName = new TextBox { Width = 340, Font = fBase }; // PlaceholderText gibt es erst ab Win10/neuem .NET
            _txtName.KeyDown += (object s, KeyEventArgs e) => { if (e.KeyCode == Keys.Enter) ApplyFilter(); };
            row1.Controls.Add(Lbl("🔍 Modulname:", fBold)); row1.Controls.Add(_txtName);

            FlowLayoutPanel row2 = Row();
            _cmbMfg = Combo(150); _cmbTech = Combo(130);
            _cmbMfg.SelectedIndexChanged += (object s, EventArgs e) => ApplyFilter();
            _cmbTech.SelectedIndexChanged += (object s, EventArgs e) => ApplyFilter();
            row2.Controls.Add(Lbl("Hersteller:", fBold)); row2.Controls.Add(_cmbMfg);
            row2.Controls.Add(Lbl("  Technologie:", fBold)); row2.Controls.Add(_cmbTech);

            FlowLayoutPanel row3 = Row();
            _nMinP = Nud(0, 99999, 0, 65); _nMaxP = Nud(0, 99999, 99999, 65);
            _nMinE = Nud(0, 100, 0, 50); _nMaxE = Nud(0, 100, 30, 50);
            _chkBif = new CheckBox { Text = "nur Bifaziale", AutoSize = true, Padding = new Padding(4, 6, 0, 0) };
            _btnFind = ABtn("🔍 Suchen", Color.FromArgb(0, 120, 215), 100);
            _btnReset = ABtn("✖ Zurücksetzen", C_RED, 115);
            row3.Controls.Add(Lbl("Leistung [W]:", fBold));
            row3.Controls.Add(_nMinP); row3.Controls.Add(Lbl("–", fBase)); row3.Controls.Add(_nMaxP);
            row3.Controls.Add(Lbl("  Effizienz [%]:", fBold));
            row3.Controls.Add(_nMinE); row3.Controls.Add(Lbl("–", fBase)); row3.Controls.Add(_nMaxE);
            row3.Controls.Add(_chkBif);
            row3.Controls.Add(_btnFind); row3.Controls.Add(_btnReset);

            fltPanel.Controls.Add(row3); fltPanel.Controls.Add(row2); fltPanel.Controls.Add(row1);

   

            SplitContainer split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };
            // 1. Zuerst dem Parent hinzufügen (dadurch nimmt er per Dock.Fill die Größe an)
            this.Controls.Add(split);

            // 2. Jetzt erst die MinSizes setzen
            split.Panel1MinSize = 380;
            split.Panel2MinSize = 340;

            // 3. SplitterDistance setzen (falls gewünscht) immer in einem Try-Catch 
            // oder nach dem Sicherstellen, dass Width > Panel1MinSize + Panel2MinSize
            if (split.Width > (split.Panel1MinSize + split.Panel2MinSize))
            {
                split.SplitterDistance = 400;
            }


            _lblCount = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Font = fBase,
                ForeColor = Color.Gray,
                Padding = new Padding(4, 4, 0, 0)
            };
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            split.Panel1.Controls.Add(_grid);
            split.Panel1.Controls.Add(_lblCount);

            _tabs = new TabControl { Dock = DockStyle.Fill };
            TabPage t1 = new TabPage("📋  Übersicht");
            TabPage t2 = new TabPage("⚡  Elektrisch");
            TabPage t4 = new TabPage("🌡  Thermisch");
            _tabs.TabPages.AddRange(new TabPage[] { t1, t2, t4 });

            // Ersetze Tuples durch explizite Aufrufe oder Hilfsklasse, falls C# 7.3 Tuples zickt
            BuildTab(t1, fBase, fBold, new[] {
                new KeyValuePair<string,string>("Quelle / Datenbank:",   "ov_db"),
                new KeyValuePair<string,string>("Modulname:",            "ov_name"),
                new KeyValuePair<string,string>("Hersteller:",           "ov_mfg"),
                new KeyValuePair<string,string>("Technologie:",          "ov_tech"),
                new KeyValuePair<string,string>("Bifazial:",             "ov_bif"),
                new KeyValuePair<string,string>("Nennleistung Pmp [W]:", "ov_pmp"),
                new KeyValuePair<string,string>("Effizienz [%]:",        "ov_eta")
            });

            Panel footer = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(230, 235, 240) };
            _btnOk = ABtn("✅  Übernehmen", Color.FromArgb(40, 167, 69), 130);
            _btnCancel = ABtn("Abbrechen", Color.FromArgb(108, 117, 125), 110);
            _btnOk.DialogResult = DialogResult.OK;
            _btnCancel.DialogResult = DialogResult.Cancel;
            _btnOk.Dock = DockStyle.Right;
            _btnCancel.Dock = DockStyle.Right;
            footer.Controls.Add(_btnOk);
            footer.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 10 });
            footer.Controls.Add(_btnCancel);

            Controls.Add(split);
            Controls.Add(footer);
            Controls.Add(fltPanel);
            Controls.Add(toolbar);
            Controls.Add(hdr);
            Controls.Add(_strip);

            _btnCecLoad.Click += async (object s, EventArgs e) => await LoadCecAsync();
            _btnCecFile.Click += LoadCecFile;
            _btnFind.Click += (object s, EventArgs e) => ApplyFilter();
            _btnReset.Click += ResetClick;
            _grid.SelectionChanged += GridSelectionChanged;
            _grid.CellFormatting += GridCellFormatting;
            _grid.CellDoubleClick += GridDoubleClick;
        }

        private void BuildTab(TabPage tab, Font f, Font fb, IEnumerable<KeyValuePair<string, string>> rows)
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10) };
            TableLayoutPanel tbl = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 275));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (KeyValuePair<string, string> row in rows)
            {
                tbl.Controls.Add(Lbl(row.Key, fb));
                Label val = new Label { Text = "—", Font = f, AutoSize = true, ForeColor = C_CEC, Padding = new Padding(2, 4, 0, 4) };
                tbl.Controls.Add(val);
                _dv[row.Value] = val;
            }
            panel.Controls.Add(tbl);
            tab.Controls.Add(panel);
        }

        private async Task LoadCecAsync()
        {
            _btnCecLoad.Enabled = false;
            Status("Lade CEC-Daten…", true);

            // Wir entpacken das Tuple direkt in (bool ok, string msg)
            var result = await _cecSvc.LoadDataAsync(new Progress<string>(m => Status(m, true)));

            // Falls dein Service (bool, string) zurückgibt, prüf result.Item1 oder nutze:
            // var (ok, msg) = await ... (falls ValueTuple installiert ist)

            if (result.Item1) // Item1 entspricht dem "bool ok"
            {
                PopulateFilters();
                ApplyFilter();
                Status("✔ Geladen: " + result.Item2);
            }
            else
            {
                Status("Fehler beim Laden.");
                MessageBox.Show(result.Item2, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            _btnCecLoad.Enabled = true;
        }

        private void LoadCecFile(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog { Filter = "CSV|*.csv", Title = "CEC CSV wählen" })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    Status("Parse Datei...", true);
                    var result = _cecSvc.LoadFromFile(dlg.FileName);

                    // result ist hier das Tuple (bool, string)
                    if (result.Item1)
                    {
                        PopulateFilters();
                        ApplyFilter();
                        Status("✔ Datei importiert");
                    }
                    else
                    {
                        MessageBox.Show(result.Item2, "Import Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Status("Fehler.");
                    }
                }
            }
        }

        // ==============================================================
        //  Filter
        // ==============================================================
        private void PopulateFilters()
        {
            var allMfg = _cecSvc.GetManufacturers().Where(x => !string.IsNullOrEmpty(x))
                .OrderBy(x => x);
            var allTech = _cecSvc.GetTechnologies().Where(x => !string.IsNullOrEmpty(x))
                .OrderBy(x => x);
            _cmbMfg.Items.Clear(); _cmbMfg.Items.Add("(alle)");
            foreach (var m in allMfg) _cmbMfg.Items.Add(m);
            _cmbMfg.SelectedIndex = 0;
            _cmbTech.Items.Clear(); _cmbTech.Items.Add("(alle)");
            foreach (var t in allTech) _cmbTech.Items.Add(t);
            _cmbTech.SelectedIndex = 0;
        }

        private void ApplyFilter()
        {
            string mfg = V(_cmbMfg);
            string tech = V(_cmbTech);
            string name = string.IsNullOrWhiteSpace(_txtName.Text) ? null : _txtName.Text.Trim();

            // Filterlogik aufrufen (C# 7.3 konform)
            var result = _cecSvc.Filter(mfg, name, tech, (double)_nMinP.Value, (double)_nMaxP.Value);
            _filtered = result.Select(x => UnifiedModule.FromCec(x)).ToList();
            BindGrid();
        }

        private void BindGrid()
        {
            _grid.DataSource = null;
            _grid.DataSource = _filtered;
            _lblCount.Text = "Anzahl: " + _filtered.Count;
        }

        private void GridCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            UnifiedModule m = _grid.Rows[e.RowIndex].DataBoundItem as UnifiedModule;
            if (m != null && _grid.Columns[e.ColumnIndex].DataPropertyName == "Database")
            {
                e.CellStyle.ForeColor = m.Database == "CEC" ? C_CEC : Color.Black;
            }
        }

        private void GridSelectionChanged(object sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0) return;
            UnifiedModule um = _grid.SelectedRows[0].DataBoundItem as UnifiedModule;
            if (um != null) ShowDetail(um);
        }

        private void ShowDetail(UnifiedModule um)
        {
            // Hilfsmethode statt Switch-Expression
            string dbName = um.Database;
            switch (um.Database)
            {
                case "CEC": dbName = "CEC (California Energy Commission)"; break;
                case "Sandia": dbName = "Sandia SAPM"; break;
            }

            if (_dv.ContainsKey("ov_db")) _dv["ov_db"].Text = dbName;
            if (_dv.ContainsKey("ov_name")) _dv["ov_name"].Text = um.Name;
            // ... usw für andere Felder
        }

        private void GridDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            UnifiedModule um = _grid.Rows[e.RowIndex].DataBoundItem as UnifiedModule;
            if (um != null && um.CecModule != null)
            {
                using (Form_ModuleDetailDialog d = new Form_ModuleDetailDialog(um.CecModule))
                {
                    d.ShowDialog(this);
                }
            }
        }

        private void Status(string msg, bool busy = false)
        {
            _lblSt.Text = msg;
            _prog.Visible = busy;
            Application.DoEvents();
        }

        private static string V(ComboBox c)
        {
            string v = c.Text != null ? c.Text.Trim() : "";
            return (string.IsNullOrEmpty(v) || v == "(alle)") ? null : v;
        }

        private static FlowLayoutPanel Row()
        {
            return new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        }

        private static Label Lbl(string t, Font f = null)
        {
            return new Label { Text = t, Font = f ?? new Font("Segoe UI", 9f), AutoSize = true, Padding = new Padding(2, 6, 4, 0) };
        }

        private static ComboBox Combo(int w)
        {
            return new ComboBox { Width = w, DropDownStyle = ComboBoxStyle.DropDownList };
        }

        private static NumericUpDown Nud(decimal min, decimal max, decimal val, int w)
        {
            return new NumericUpDown { Minimum = min, Maximum = max, Value = val, Width = w };
        }

        private static Button ABtn(string t, Color back, int w)
        {
            Button b = new Button { Text = t, Width = w, Height = 28, BackColor = back, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private static Button TBtn(string t, Color back)
        {
            Button b = new Button { Text = t, AutoSize = true, Height = 30, BackColor = back, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private void ResetClick(object sender, EventArgs e) { /* Reset Logik */ }
    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;


namespace WindowsFormsApplication1
{
    public partial class Form_WpFilterAuswahl : Form
    {
        private List<WPData> _allData;
        public WPData SelectedWP { get; private set; }

        public Form_WpFilterAuswahl()
        {
            InitializeComponent();

            dgv.Visible = false; // Erstmal verstecken

            AttachEvents();
            LoadData();

            txtSucheBezeichnung.SetPlaceholder("🔍  Suchen…");
            btnSelect.FlatStyle = FlatStyle.Flat;
            btnSelect.FlatAppearance.BorderSize = 0; // Entfernt den Rahmen
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0; // Entfernt den Rahmen
            btnSelect.MakeSmoothButton(btnSelect.Height / 4);
            btnCancel.MakeSmoothButton(btnCancel.Height / 4);
            btnSelect.TabStop = false;
        }

        private void AttachEvents()
        {
            btnFilter.Click += (s, e) => ApplyFilter();
            btnSelect.Click += (s, e) => SelectAndClose();
            dgv.CellDoubleClick += (s, e) => SelectAndClose();
            txtSucheBezeichnung.TextChanged += (s, e) => ApplyFilter();
        }

        private void LoadData()
        {
            _allData = WPDataCtrl.ReadAll() ?? new List<WPData>();

            FillCombo(cbHersteller, x => x.Hersteller);
            FillCombo(cbAuslegung, x => x.Auslegung);
            FillCombo(cbPrinzip, x => x.Funktionsprinzip);
            FillCombo(cbRegelung, x => x.Regelung);
            FillCombo(cbBauart, x => x.Bauart);
            FillCombo(cbAufstellung, x => x.Aufstellung);
            FillCombo(cbZuheizung, x => x.ElZuheizung.ToString());
            numLeistungMax.Value = (decimal)_allData.Max(x => x.MaxLeistung);
            numTempMax.Value = (decimal)_allData.Max(x => x.MaxVorlaufTemp);
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            // Den rohen Suchbegriff holen
            string searchInput = txtSucheBezeichnung.Text.Trim();

            // Vorbereitung der Wildcard-Logik
            bool noFilter = string.IsNullOrEmpty(searchInput) || searchInput == "*";
            Regex filterRegex = null;

            if (!noFilter)
            {
                try
                {
                    // Wir wandeln den Input in ein Regex-Pattern um:
                    // 1. Regex.Escape maskiert Sonderzeichen wie "." oder "+", damit sie nicht als Befehl interpretiert werden.
                    // 2. Wir ersetzen das maskierte "\*" durch ".*" (Regex für "beliebige Zeichenfolge").
                    // 3. Wir ersetzen das maskierte "\?" durch "." (Regex für "genau ein beliebiges Zeichen").
                    string pattern = "^" + Regex.Escape(searchInput).Replace("\\*", ".*").Replace("\\?", ".") + "$";

                    // Falls der User KEINE Wildcards nutzt, wollen wir trotzdem eine Teilsuche (wie "Contains")
                    if (!searchInput.Contains("*") && !searchInput.Contains("?"))
                    {
                        pattern = Regex.Escape(searchInput); // Einfache Suche ohne Begrenzung durch ^ und $
                    }

                    filterRegex = new Regex(pattern, RegexOptions.IgnoreCase);
                }
                catch
                {
                    // Falls der User ungültige Zeichen eingibt, die Regex sprengen
                    noFilter = true;
                }
            }

            var filtered = _allData.Where(x =>
                (cbHersteller.Text == "Alle" || x.Hersteller == cbHersteller.Text) &&
                (cbAuslegung.Text == "Alle" || x.Auslegung == cbAuslegung.Text) &&
                (cbPrinzip.Text == "Alle" || x.Funktionsprinzip == cbPrinzip.Text) &&
                (cbRegelung.Text == "Alle" || x.Regelung == cbRegelung.Text) &&
                (cbBauart.Text == "Alle" || x.Bauart == cbBauart.Text) &&
                (cbAufstellung.Text == "Alle" || x.Aufstellung == cbAufstellung.Text) &&
                (cbZuheizung.Text == "Alle" || x.ElZuheizung.ToString() == cbZuheizung.Text) &&
                (x.MaxVorlaufTemp >= (double)numTempMin.Value && x.MaxVorlaufTemp <= (double)numTempMax.Value) &&
                (x.MaxLeistung >= (double)numLeistungMin.Value && x.MaxLeistung <= (double)numLeistungMax.Value) &&

                // Die neue Wildcard-Bedingung:
                (noFilter || (x.Bezeichnung != null && filterRegex.IsMatch(x.Bezeichnung)))
            ).ToList();

            dgv.DataSource = null;
            dgv.DataSource = filtered;
            SetupGridColumns();

            if (filtered.Count == 0)
            {
                dgv.BackgroundColor = Color.WhiteSmoke; // Signalisiert: "Hier ist gerade nichts"
            }
            else
            {
                dgv.BackgroundColor = SystemColors.AppWorkspace; // Standard-Farbe
            }
            dgv.Visible = true;

            this.Text = $"WP-Filter Auswahl ({filtered.Count} Wärmepumpen gefunden)";
        }

        private void SetupGridColumns()
        {
            if (dgv.Columns.Count == 0) return;

            var headers = new Dictionary<string, string> {
                { "Hersteller", "Hersteller" }, { "Bezeichnung", "Modell" },
                { "MaxVorlaufTemp", "VLT max [°C]" }, { "MinVorlaufTemp", "VLT min [°C]" },
                { "MaxLeistung", "Leistung [kW]" },
                { "ElZuheizung", "Zuheizer [kW]" }, { "Bauart", "Bauart" }
            };

            // 1. ZUERST den globalen Modus auf AllCells setzen
            // Das sorgt dafür, dass sich alle Spalten dem Inhalt anpassen
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (headers.ContainsKey(col.Name)) col.HeaderText = headers[col.Name];

                if (col.Name.Contains("Leistung") || col.Name.Contains("Zuheizung"))
                {
                    //col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    col.DefaultCellStyle.Format = "N1";
                }
                if (col.Name.Contains("Bezeichnung") || col.Name.Contains("Modell"))
                {
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }
            }

            dgv.ReadOnly = true;                // Verhindert das Tippen in Zellen
            dgv.AllowUserToAddRows = false;     // Entfernt die leere Sternchen-Zeile am Ende
            dgv.AllowUserToDeleteRows = false;  // Verhindert das Löschen mit der Entf-Taste
            dgv.EditMode = DataGridViewEditMode.EditProgrammatically; // Deaktiviert das automatische Öffnen von Editoren
            dgv.MultiSelect = false; // Verhindert die Auswahl mehrerer Zeilen (auch mit Strg/Shift)
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect; // Markiert immer die ganze Zeile

            // 1. Hintergrundfarbe für alle (geraden) Zeilen (Standard: Weiß)
            dgv.RowsDefaultCellStyle.BackColor = Color.White;

            // 2. Hintergrundfarbe für jede zweite (ungerade) Zeile
            // Ein ganz sanftes Grau oder Hellblau wirkt am professionellsten
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(242, 245, 250);

            // 3. Optional: Farbe der selektierten Zeile anpassen
            // Damit das "Zebra-Muster" nicht mit der Auswahl kollidiert
            dgv.DefaultCellStyle.SelectionBackColor = Color.SteelBlue;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
        }

        private void SelectAndClose()
        {
            if (dgv.CurrentRow != null)
            {
                SelectedWP = dgv.CurrentRow.DataBoundItem as WPData;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private ComboBox CreateFilterCombo(Control parent, string text)
        {
            var container = new Panel { Width = 155, Height = 55, Margin = new Padding(5) };
            container.Controls.Add(new Label { Text = text, Dock = DockStyle.Top, Height = 18, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) });
            var cb = new ComboBox { Dock = DockStyle.Bottom, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.White };
            container.Controls.Add(cb);
            parent.Controls.Add(container);
            return cb;
        }

        private NumericUpDown CreateNumeric(Control parent, string text, int min, int max, int val)
        {
            var container = new Panel { Width = 100, Height = 55, Margin = new Padding(5) };
            container.Controls.Add(new Label { Text = text, Dock = DockStyle.Top, Height = 18, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) });
            var num = new NumericUpDown { Dock = DockStyle.Bottom, Minimum = min, Maximum = max, Value = val, DecimalPlaces = 1 };
            container.Controls.Add(num);
            parent.Controls.Add(container);
            return num;
        }

        private void FillCombo(ComboBox cb, Func<WPData, string> selector)
        {
            cb.Items.Clear();
            cb.Items.Add("Alle");
            var items = _allData.Select(selector).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToArray();
            cb.Items.AddRange(items);
            cb.SelectedIndex = 0;
            cb.SelectedIndexChanged += (s, e) => ApplyFilter();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        private void ResetFilter()
        {
            // 1. Alle ComboBoxen auf den ersten Eintrag ("Alle") setzen
            cbHersteller.SelectedIndex = 0;
            cbAuslegung.SelectedIndex = 0;
            cbPrinzip.SelectedIndex = 0;
            cbRegelung.SelectedIndex = 0;
            cbBauart.SelectedIndex = 0;
            cbAufstellung.SelectedIndex = 0;
            cbZuheizung.SelectedIndex = 0;

            // 2. NumericUpDowns auf Standardwerte (oder Extremwerte der Daten)
            numTempMin.Value = 0;
            numLeistungMin.Value = 0;

            // Dynamische Max-Werte aus den geladenen Daten holen
            if (_allData != null && _allData.Count > 0)
            {
                numTempMax.Value = (decimal)_allData.Max(x => x.MaxVorlaufTemp);
                numLeistungMax.Value = (decimal)_allData.Max(x => x.MaxLeistung);
            }

            txtSucheBezeichnung.Text = string.Empty; // Textfeld leeren

            // 3. Filter sofort anwenden, um die volle Liste wieder anzuzeigen
            ApplyFilter();
        }

        private void btn_Reset_Click(object sender, EventArgs e)
        {
            ResetFilter();
        }

    }

    public class WPData
    {
        public string Hersteller { get; set; }
        public string Bezeichnung { get; set; }
        public string Bauart { get; set; }
        public string Aufstellung { get; set; }
        public double MaxVorlaufTemp { get; set; }
        public double MinVorlaufTemp { get; set; }
        public double MaxLeistung { get; set; }
        public double ElZuheizung { get; set; }
        public string Funktionsprinzip { get; set; }
        public string Regelung { get; set; }
        public string Auslegung { get; set; }
    }

    public static class WPDataCtrl
    {
        static WPData dat;
        
        public static List<WPData> ReadAll()
        {
            List<WPData> list = new List<WPData>();
            WPCtrl ctrl = new WPCtrl();
            
            ctrl.ReadAll_MitMinMaxVorlauf("select * from Abfrage_MaxMin_Vorlauf order by WPName");
            
            for (int i = 0; i < ctrl.rows; i++)
            {
                dat = new WPData
                {
                    Hersteller = ctrl.items[i].Firma,
                    Bauart = ctrl.items[i].Bauart,
                    Bezeichnung = ctrl.items[i].WPName,
                    Aufstellung = ctrl.items[i].Aufstellung,
                    MaxVorlaufTemp = ctrl.items[i].MaxVorlauf,
                    MinVorlaufTemp = ctrl.items[i].MinVorlauf,
                    MaxLeistung = ctrl.items[i].Nennleistung,
                    ElZuheizung = ctrl.items[i].Heizung,
                    Funktionsprinzip = ctrl.items[i].Typ,
                    Regelung = ctrl.items[i].Regelung,
                    Auslegung = ctrl.items[i].Kuehlleistung > 0 ? "Heizen/Kühlen" : "Heizen"
                };
                list.Add(dat);
            }

            return list;
        }
    }
}

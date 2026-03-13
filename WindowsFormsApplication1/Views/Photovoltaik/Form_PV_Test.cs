using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Main_PV_Test : Form
    {
        public CECDataService _cecSvc = new CECDataService();
        public List<PVModule> listPVModules = new List<PVModule>(); 

        public Main_PV_Test()
        {
            InitializeComponent();

            // Erst nach Initialisierung die Distance setzen (vermeidet Exception)
            this.Load += (s, e) =>
            {
                _splitContainer.SplitterDistance = (int)(this.Width * 0.66);
                MakeSmooth(_splitContainer);
            };
            this._splitContainer.SplitterWidth = 2; // Ein schmaler, moderner Balken
            _splitContainer.Panel1.Padding = new Padding(3);
            _splitContainer.Panel2.Padding = new Padding(0);
            this.tabControl1.Margin = new Padding(0);
            _splitContainer.BorderStyle = BorderStyle.None;
            // Wenn du willst, dass das TabControl bündig abschließt:
            this.tabControl1.Appearance = TabAppearance.Normal;

            _txtSearch.SetPlaceholder("z.B.  Trina*  oder  *410*  oder  *mono*2022*");

            _btnFilter.BackColor = Color.FromArgb(0, 120, 215);
            _btnFilter.ForeColor = Color.White;
            _btnReset.BackColor = Color.Red;
            _btnReset.ForeColor = Color.White;
            _bottomPanel.BackColor = Color.FromArgb(230, 235, 240);
            _dgvModules.RowHeadersVisible = false;
            btnSelect.MakeSmoothButton(btnSelect.Height / 4);
            btnCancel.MakeSmoothButton(btnCancel.Height / 4);
            btnSelect.FlatAppearance.BorderSize = 0;
            btnCancel.FlatAppearance.BorderSize = 0;
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

        private void _btnCEC_Click(object sender, EventArgs e)
        {
            LoadCecAsync();
        }

        // ==============================================================
        //  Laden
        // ==============================================================
        private async Task LoadCecAsync()
        {

            var (ok, msg) = await _cecSvc.LoadDataAsync();
            if (ok)
            {
                _dgvModules.DataSource = _cecSvc.AllModules;
                listPVModules = (List<PVModule>)_cecSvc.AllModules;
            }
            else
            {
                MessageBox.Show(msg, "CEC – Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }

        private void ApplyFilter()
        {
            // Den rohen Suchbegriff holen
            string searchInput = _txtSearch.Text.Trim();

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

            var filtered = listPVModules.Where(x =>
  

                // Die neue Wildcard-Bedingung:
                (noFilter || (x.Name != null && filterRegex.IsMatch(x.Name)))
            ).ToList();

            _dgvModules.DataSource = null;
            _dgvModules.DataSource = filtered;
            //SetupGridColumns();

            if (filtered.Count == 0)
            {
                _dgvModules.BackgroundColor = Color.WhiteSmoke; // Signalisiert: "Hier ist gerade nichts"
            }
            else
            {
                _dgvModules.BackgroundColor = SystemColors.AppWorkspace; // Standard-Farbe
            }
           //dgv.Visible = true;

            this.Text = $"WP-Filter Auswahl ({filtered.Count} Wärmepumpen gefunden)";
        }

        private void _btnFilter_Click(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void _btnReset_Click(object sender, EventArgs e)
        {
            _txtSearch.Text = "";
            ApplyFilter();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();    
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
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Auswahl des Pufferspeichers, der als Wärmequelle einer Wärmepumpe dient
    /// (Sole-Wasser / Wasser-Wasser).
    ///
    /// Der gewählte Speicher liefert in der Simulation die Quellwärme:
    /// Je Stunde entzieht die Wärmepumpe dem Speicher die Verdampferwärme
    /// (Wärmeproduktion - Stromaufnahme). Reicht der Speicherinhalt nicht aus,
    /// wird die Leistung der Wärmepumpe entsprechend begrenzt - der Wärmebedarf
    /// muss also tatsächlich aus dem Speicher gedeckt werden.
    ///
    /// Das Formular wird komplett programmatisch aufgebaut (kein Designer/.resx).
    /// </summary>
    public class Form_QuellePufferspeicher : Form
    {
        private ListBox _lbSpeicher;
        private TextBox _tbTemperatur;
        private TextBox _tbSpreizung;
        private TextBox _tbRegeneration;
        private CheckBox _cbUnbegrenzt;
        private Label _lblKapazitaet;
        private Label _lblDaten;

        private DataTable _speicherTabelle;

        /// <summary>Name der Wärmepumpe (nur für den Fenstertitel).</summary>
        public string WPName = "";

        /// <summary>Bezeichner des gewählten Pufferspeichers.</summary>
        public string Pufferspeicher = "";

        /// <summary>Quelltemperatur des Speichers [°C].</summary>
        public double Quelltemperatur = 10;

        /// <summary>Nutzbare Temperaturspreizung des Speichers [K].</summary>
        public double Spreizung = 5;

        /// <summary>Regeneration/Nachladung der Quelle [kW], 0 = keine.</summary>
        public double Regeneration = 0;

        /// <summary>true = Quelle immer verfügbar (nur die Temperatur wirkt).</summary>
        public bool Unbegrenzt = false;

        public Form_QuellePufferspeicher()
        {
            BaueOberflaeche();
        }

        private void BaueOberflaeche()
        {
            this.Text = "Wärmequelle Pufferspeicher";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ClientSize = new Size(620, 430);

            Label kopf = new Label
            {
                Text = "Pufferspeicher als Wärmequelle auswählen:",
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold),
                Location = new Point(14, 12)
            };
            this.Controls.Add(kopf);

            _lbSpeicher = new ListBox
            {
                Location = new Point(14, 38),
                Size = new Size(300, 200)
            };
            _lbSpeicher.SelectedIndexChanged += (s, e) => { ZeigeSpeicherDaten(); BerechneKapazitaet(); };
            this.Controls.Add(_lbSpeicher);

            _lblDaten = new Label
            {
                AutoSize = false,
                Location = new Point(330, 38),
                Size = new Size(275, 90),
                Text = ""
            };
            this.Controls.Add(_lblDaten);

            // Parameter der Wärmequelle
            GroupBox gb = new GroupBox
            {
                Text = "Parameter der Wärmequelle",
                Location = new Point(14, 250),
                Size = new Size(590, 130)
            };
            this.Controls.Add(gb);

            Label l1 = new Label { Text = "Quelltemperatur [°C]:", AutoSize = true, Location = new Point(16, 30) };
            _tbTemperatur = new TextBox { Location = new Point(180, 27), Width = 80, Text = "10,0" };
            _tbTemperatur.TextChanged += (s, e) => BerechneKapazitaet();

            Label l2 = new Label { Text = "nutzbare Spreizung [K]:", AutoSize = true, Location = new Point(16, 62) };
            _tbSpreizung = new TextBox { Location = new Point(180, 59), Width = 80, Text = "5,0" };
            _tbSpreizung.TextChanged += (s, e) => BerechneKapazitaet();

            Label l3 = new Label { Text = "Regeneration [kW]:", AutoSize = true, Location = new Point(16, 94) };
            _tbRegeneration = new TextBox { Location = new Point(180, 91), Width = 80, Text = "0,0" };

            _lblKapazitaet = new Label
            {
                AutoSize = false,
                Location = new Point(285, 28),
                Size = new Size(290, 40),
                Text = ""
            };

            _cbUnbegrenzt = new CheckBox
            {
                Text = "Quelle unbegrenzt verfügbar (nur Temperatur maßgeblich)",
                AutoSize = true,
                Location = new Point(285, 92)
            };

            gb.Controls.Add(l1);
            gb.Controls.Add(_tbTemperatur);
            gb.Controls.Add(l2);
            gb.Controls.Add(_tbSpreizung);
            gb.Controls.Add(l3);
            gb.Controls.Add(_tbRegeneration);
            gb.Controls.Add(_lblKapazitaet);
            gb.Controls.Add(_cbUnbegrenzt);

            Label hinweis = new Label
            {
                AutoSize = false,
                Location = new Point(330, 132),
                Size = new Size(275, 105),
                Text = "Die Wärmepumpe entzieht dem Speicher je Stunde die " +
                       "Verdampferwärme (Wärmeproduktion − Stromaufnahme).\n\n" +
                       "Ist der Speicher leer, wird die Leistung der Wärmepumpe " +
                       "begrenzt; die Regeneration lädt den Speicher laufend nach."
            };
            this.Controls.Add(hinweis);

            Button btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(this.ClientSize.Width - 190, 392),
                Width = 85
            };
            Button btnAbbruch = new Button
            {
                Text = "Abbrechen",
                DialogResult = DialogResult.Cancel,
                Location = new Point(this.ClientSize.Width - 97, 392),
                Width = 85
            };
            btnOk.Click += btnOk_Click;

            this.Controls.Add(btnOk);
            this.Controls.Add(btnAbbruch);
            this.AcceptButton = btnOk;
            this.CancelButton = btnAbbruch;
        }

        /// <summary>
        /// Füllt die Auswahlliste aus den Pufferspeicher-Stammdaten und
        /// belegt die Felder mit den gespeicherten Werten vor.
        /// </summary>
        public void SetControls()
        {
            if (!string.IsNullOrEmpty(WPName)) this.Text = "Wärmequelle Pufferspeicher - " + WPName;

            _speicherTabelle = DataRepository.GetDataTable(
                "SELECT Bezeichner, Speichertyp, Gesamtvolumen, Bereitschaftsverluste FROM [" +
                PufferSpStammCtrl.TABLE + "] ORDER BY Bezeichner");

            _lbSpeicher.Items.Clear();
            if (_speicherTabelle != null)
            {
                foreach (DataRow r in _speicherTabelle.Rows)
                    _lbSpeicher.Items.Add(r["Bezeichner"].ToString());
            }

            if (_lbSpeicher.Items.Count == 0)
            {
                MessageBox.Show("Es sind keine Pufferspeicher in den Stammdaten vorhanden!",
                    "Wärmequelle Pufferspeicher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            _tbTemperatur.Text = Quelltemperatur.ToString("F1");
            _tbSpreizung.Text = Spreizung.ToString("F1");
            _tbRegeneration.Text = Regeneration.ToString("F1");
            _cbUnbegrenzt.Checked = Unbegrenzt;

            if (!string.IsNullOrEmpty(Pufferspeicher) && _lbSpeicher.Items.Contains(Pufferspeicher))
                _lbSpeicher.SelectedItem = Pufferspeicher;
            else if (_lbSpeicher.Items.Count > 0)
                _lbSpeicher.SelectedIndex = 0;

            ZeigeSpeicherDaten();
            BerechneKapazitaet();
        }

        /// <summary>Zeigt Stammdaten des markierten Speichers an.</summary>
        private void ZeigeSpeicherDaten()
        {
            DataRow r = AktuelleZeile();
            if (r == null) { _lblDaten.Text = ""; return; }

            _lblDaten.Text =
                "Speichertyp: " + Feld(r, "Speichertyp") + "\n" +
                "Gesamtvolumen: " + Feld(r, "Gesamtvolumen") + " l\n" +
                "Bereitschaftsverluste: " + Feld(r, "Bereitschaftsverluste") + " kWh/24h";
        }

        private string Feld(DataRow r, string spalte)
        {
            if (r == null || !r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return "-";
            return r[spalte].ToString();
        }

        private DataRow AktuelleZeile()
        {
            if (_speicherTabelle == null || _lbSpeicher.SelectedIndex < 0) return null;
            if (_lbSpeicher.SelectedIndex >= _speicherTabelle.Rows.Count) return null;
            return _speicherTabelle.Rows[_lbSpeicher.SelectedIndex];
        }

        /// <summary>Zeigt die nutzbare Speicherkapazität aus Volumen und Spreizung.</summary>
        private void BerechneKapazitaet()
        {
            DataRow r = AktuelleZeile();
            float spreizung;
            if (r == null || !WaermequelleClass.ZahlParsen(_tbSpreizung.Text, out spreizung))
            {
                _lblKapazitaet.Text = "";
                return;
            }

            double volumen = 0;
            if (r.Table.Columns.Contains("Gesamtvolumen") && r["Gesamtvolumen"] != DBNull.Value)
                volumen = Convert.ToDouble(r["Gesamtvolumen"]);

            double kapazitaet = volumen * 1.16 * spreizung / 1000.0;
            _lblKapazitaet.Text = "nutzbare Kapazität:\n" + kapazitaet.ToString("F1") + " kWh";
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (_lbSpeicher.SelectedIndex < 0)
            {
                MessageBox.Show("Bitte einen Pufferspeicher auswählen!", "Wärmequelle Pufferspeicher",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            float temp, spreizung, regeneration;
            if (!WaermequelleClass.ZahlParsen(_tbTemperatur.Text, out temp) ||
                !WaermequelleClass.ZahlParsen(_tbSpreizung.Text, out spreizung) ||
                !WaermequelleClass.ZahlParsen(_tbRegeneration.Text, out regeneration))
            {
                MessageBox.Show("Bitte gültige Zahlenwerte eintragen!", "Wärmequelle Pufferspeicher",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (spreizung <= 0)
            {
                MessageBox.Show("Die nutzbare Spreizung muss größer als 0 K sein!",
                    "Wärmequelle Pufferspeicher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            Pufferspeicher = _lbSpeicher.SelectedItem.ToString();
            Quelltemperatur = temp;
            Spreizung = spreizung;
            Regeneration = regeneration;
            Unbegrenzt = _cbUnbegrenzt.Checked;
        }
    }
}

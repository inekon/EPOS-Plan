using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Parameterdialog „Wirtschaftlichkeits-Parameter" (Konzept Kap. 6, Punkt 4).
    /// Eine Zeile je STAMMprojekt in Tab_ProjektWirtschaftlichkeit — die Parameter
    /// gelten für die ganze Vergleichsgruppe (Stamm + Varianten), damit alle
    /// Projekte mit identischen Annahmen verglichen werden (Normanforderung
    /// Nachvollziehbarkeit, DIN EN 17463).
    ///
    /// Komplett im Code aufgebaut (kein Designer/.resx nötig) — Muster Form_Bericht.
    /// </summary>
    public class Form_WirtschaftlichkeitParameter : Form
    {
        private readonly WirtschaftlichkeitCtrl _ctrl = new WirtschaftlichkeitCtrl();
        private readonly WirtschaftlichkeitParameter _parameter;

        private NumericUpDown numZins, numJahre, numPreisE, numPreisB, numEinspeisung;
        private Button btnOk, btnAbbrechen;

        /// <summary>true, wenn gespeichert wurde (Aufrufer rechnet dann neu).</summary>
        public bool Gespeichert { get; private set; }

        public Form_WirtschaftlichkeitParameter(int idStamm)
        {
            _parameter = _ctrl.LadeParameter(idStamm);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            int y = 15;

            numZins = Zeile("Kalkulationszinssatz i [%]:", ref y, 0m, 15m, 2, (decimal)_parameter.Zinssatz, 0.1m);
            numJahre = Zeile("Betrachtungszeitraum T [a]:", ref y, 1m, 50m, 0, _parameter.Betrachtungszeitraum, 1m);
            numPreisE = Zeile("Preissteigerung Energie [%/a]:", ref y, -10m, 20m, 2, (decimal)_parameter.PreissteigerungEnergie, 0.1m);
            numPreisB = Zeile("Preissteigerung Betrieb [%/a]:", ref y, -10m, 20m, 2, (decimal)_parameter.PreissteigerungBetrieb, 0.1m);
            numEinspeisung = Zeile("Einspeisevergütung PV [€/kWh]:", ref y, 0m, 2m, 4, (decimal)_parameter.Einspeiseverguetung, 0.001m);

            var lblHinweis = new Label
            {
                Location = new Point(15, y + 4),
                Size = new Size(360, 50),
                ForeColor = Color.DimGray,
                Text = "Die Parameter gelten für Stamm und alle Varianten der Vergleichsgruppe. " +
                       "Energie- und Strompreise kommen aus der Kostenmaske (keine Doppelpflege)."
            };
            this.Controls.Add(lblHinweis);
            y += 58;

            btnOk = new Button
            {
                Location = new Point(184, y),
                Size = new Size(120, 28),
                Text = "Speichern",
                DialogResult = DialogResult.None
            };
            btnOk.Click += new EventHandler(btnOk_Click);

            btnAbbrechen = new Button
            {
                Location = new Point(310, y),
                Size = new Size(90, 28),
                Text = "Abbrechen",
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(btnOk);
            this.Controls.Add(btnAbbrechen);

            this.AutoScaleMode = AutoScaleMode.Font;
            this.Font = new Font("Segoe UI", 9f);
            this.ClientSize = new Size(415, y + 45);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false; this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.AcceptButton = btnOk;
            this.CancelButton = btnAbbrechen;
            this.Name = "Form_WirtschaftlichkeitParameter";
            this.Text = "Wirtschaftlichkeits-Parameter";
            this.ResumeLayout(false);
        }

        private NumericUpDown Zeile(string beschriftung, ref int y,
                                    decimal min, decimal max, int dez, decimal wert, decimal schritt)
        {
            var lbl = new Label { Location = new Point(15, y + 3), Size = new Size(215, 20), Text = beschriftung };
            var num = new NumericUpDown
            {
                Location = new Point(240, y),
                Size = new Size(160, 23),
                Minimum = min,
                Maximum = max,
                DecimalPlaces = dez,
                Increment = schritt,
                TextAlign = HorizontalAlignment.Right
            };
            num.Value = wert < min ? min : (wert > max ? max : wert);
            this.Controls.Add(lbl);
            this.Controls.Add(num);
            y += 32;
            return num;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            _parameter.Zinssatz = (double)numZins.Value;
            _parameter.Betrachtungszeitraum = (int)numJahre.Value;
            _parameter.PreissteigerungEnergie = (double)numPreisE.Value;
            _parameter.PreissteigerungBetrieb = (double)numPreisB.Value;
            _parameter.Einspeiseverguetung = (double)numEinspeisung.Value;

            if (!_ctrl.SpeichereParameter(_parameter))
            {
                MessageBox.Show("Die Parameter konnten nicht gespeichert werden.", "Fehler",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Gespeichert = true;
            this.DialogResult = DialogResult.OK;
            Close();
        }
    }
}

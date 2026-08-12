using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Dialog „Tarifstruktur Strom" (Stufe W3, Phase 8; Konzept Kap. 2.5) —
    /// vereinfachtes Modell laut Entscheidung 11.08.2026: Winterzeitraum als
    /// Monatsspanne, EIN HT-Fenster Mo–Fr, je vier Zonenpreise für Bezug und
    /// Einspeisung, zweistufige Leistungspreis-Staffel. Eine Zeile je Stamm
    /// (Tab_ProjektTarif); inaktiv = Flat-Preise der Kostenmaske gelten weiter.
    ///
    /// Komplett im Code aufgebaut (kein Designer/.resx nötig) — Muster Form_Bericht.
    /// </summary>
    public class Form_Tarifstruktur : Form
    {
        private readonly WirtschaftlichkeitCtrl _ctrl = new WirtschaftlichkeitCtrl();
        private readonly TarifParameter _tarif;

        private CheckBox chkAktiv;
        private NumericUpDown numWinterVon, numWinterBis, numHtVon, numHtBis;
        private NumericUpDown numBezugWHT, numBezugWNT, numBezugSHT, numBezugSNT;
        private NumericUpDown numEinspWHT, numEinspWNT, numEinspSHT, numEinspSNT;
        private NumericUpDown numGrenze, numPreis1, numPreis2;
        private Button btnOk, btnAbbrechen;

        /// <summary>true, wenn gespeichert wurde (Aufrufer rechnet dann neu).</summary>
        public bool Gespeichert { get; private set; }

        public Form_Tarifstruktur(int idStamm)
        {
            _tarif = _ctrl.LadeTarif(idStamm);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            int y = 12;

            chkAktiv = new CheckBox
            {
                Location = new Point(15, y),
                AutoSize = true,
                Text = "Tarifstruktur aktiv (ersetzt die Flat-Strompreise der Kostenmaske)",
                Checked = _tarif.Aktiv
            };
            this.Controls.Add(chkAktiv);
            y += 32;

            Gruppe("Zeitzonen (HT gilt Mo–Fr; Referenzjahr 2026)", ref y);
            numWinterVon = Zeile("Winter von Monat:", ref y, 1, 12, 0, _tarif.WinterVonMonat, 1);
            numWinterBis = Zeile("Winter bis Monat:", ref y, 1, 12, 0, _tarif.WinterBisMonat, 1);
            numHtVon = Zeile("HT von Stunde:", ref y, 0, 23, 0, _tarif.HtVonStunde, 1);
            numHtBis = Zeile("HT bis Stunde (exklusiv):", ref y, 1, 24, 0, _tarif.HtBisStunde, 1);

            Gruppe("Bezugspreise [€/kWh]", ref y);
            numBezugWHT = Zeile("Winter HT:", ref y, 0, 5, 4, (decimal)_tarif.PreisBezugWinterHT, 0.005m);
            numBezugWNT = Zeile("Winter NT:", ref y, 0, 5, 4, (decimal)_tarif.PreisBezugWinterNT, 0.005m);
            numBezugSHT = Zeile("Sommer HT:", ref y, 0, 5, 4, (decimal)_tarif.PreisBezugSommerHT, 0.005m);
            numBezugSNT = Zeile("Sommer NT:", ref y, 0, 5, 4, (decimal)_tarif.PreisBezugSommerNT, 0.005m);

            Gruppe("Einspeisepreise [€/kWh] (PV- und KWK-Einspeisung)", ref y);
            numEinspWHT = Zeile("Winter HT:", ref y, 0, 5, 4, (decimal)_tarif.PreisEinspWinterHT, 0.005m);
            numEinspWNT = Zeile("Winter NT:", ref y, 0, 5, 4, (decimal)_tarif.PreisEinspWinterNT, 0.005m);
            numEinspSHT = Zeile("Sommer HT:", ref y, 0, 5, 4, (decimal)_tarif.PreisEinspSommerHT, 0.005m);
            numEinspSNT = Zeile("Sommer NT:", ref y, 0, 5, 4, (decimal)_tarif.PreisEinspSommerNT, 0.005m);

            Gruppe("Leistungspreis-Staffel (auf die Jahres-Bezugsspitze)", ref y);
            numGrenze = Zeile("Staffelgrenze [kW]:", ref y, 0, 100000, 0, (decimal)_tarif.StaffelGrenzeKW, 10);
            numPreis1 = Zeile("Preis bis Grenze [€/kW·a]:", ref y, 0, 1000, 2, (decimal)_tarif.StaffelPreis1EurKW, 1);
            numPreis2 = Zeile("Preis über Grenze [€/kW·a]:", ref y, 0, 1000, 2, (decimal)_tarif.StaffelPreis2EurKW, 1);

            y += 8;
            btnOk = new Button
            {
                Location = new Point(214, y),
                Size = new Size(120, 28),
                Text = "Speichern"
            };
            btnOk.Click += new EventHandler(btnOk_Click);
            btnAbbrechen = new Button
            {
                Location = new Point(340, y),
                Size = new Size(90, 28),
                Text = "Abbrechen",
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(btnOk);
            this.Controls.Add(btnAbbrechen);

            this.AutoScaleMode = AutoScaleMode.Font;
            this.Font = new Font("Segoe UI", 9f);
            this.ClientSize = new Size(445, y + 42);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.AutoScroll = true;   // Schutz bei hoher DPI-Skalierung (Review Phase 8)
            this.MaximizeBox = false; this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.AcceptButton = btnOk;
            this.CancelButton = btnAbbrechen;
            this.Name = "Form_Tarifstruktur";
            this.Text = "Tarifstruktur Strom (HT/NT × Winter/Sommer)";
            this.ResumeLayout(false);
        }

        private void Gruppe(string text, ref int y)
        {
            var lbl = new Label
            {
                Location = new Point(15, y + 4),
                Size = new Size(415, 18),
                Text = text,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            this.Controls.Add(lbl);
            y += 26;
        }

        private NumericUpDown Zeile(string beschriftung, ref int y,
                                    decimal min, decimal max, int dez, decimal wert, decimal schritt)
        {
            var lbl = new Label { Location = new Point(28, y + 3), Size = new Size(237, 20), Text = beschriftung };
            var num = new NumericUpDown
            {
                Location = new Point(270, y),
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
            y += 29;
            return num;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if ((int)numHtVon.Value >= (int)numHtBis.Value)
            {
                MessageBox.Show("Das HT-Fenster ist leer (von ≥ bis).", "Tarifstruktur",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (chkAktiv.Checked &&
                numBezugWHT.Value <= 0 && numBezugWNT.Value <= 0 &&
                numBezugSHT.Value <= 0 && numBezugSNT.Value <= 0)
            {
                MessageBox.Show("Die Tarifstruktur ist aktiv, aber es ist kein Bezugspreis gepflegt — " +
                    "die Berechnung fällt dann auf die Flat-Preise der Kostenmaske zurück.",
                    "Tarifstruktur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            _tarif.Aktiv = chkAktiv.Checked;
            _tarif.WinterVonMonat = (int)numWinterVon.Value;
            _tarif.WinterBisMonat = (int)numWinterBis.Value;
            _tarif.HtVonStunde = (int)numHtVon.Value;
            _tarif.HtBisStunde = (int)numHtBis.Value;
            _tarif.PreisBezugWinterHT = (double)numBezugWHT.Value;
            _tarif.PreisBezugWinterNT = (double)numBezugWNT.Value;
            _tarif.PreisBezugSommerHT = (double)numBezugSHT.Value;
            _tarif.PreisBezugSommerNT = (double)numBezugSNT.Value;
            _tarif.PreisEinspWinterHT = (double)numEinspWHT.Value;
            _tarif.PreisEinspWinterNT = (double)numEinspWNT.Value;
            _tarif.PreisEinspSommerHT = (double)numEinspSHT.Value;
            _tarif.PreisEinspSommerNT = (double)numEinspSNT.Value;
            _tarif.StaffelGrenzeKW = (double)numGrenze.Value;
            _tarif.StaffelPreis1EurKW = (double)numPreis1.Value;
            _tarif.StaffelPreis2EurKW = (double)numPreis2.Value;

            if (!_ctrl.SpeichereTarif(_tarif))
            {
                MessageBox.Show("Die Tarifstruktur konnte nicht gespeichert werden.", "Fehler",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Gespeichert = true;
            this.DialogResult = DialogResult.OK;
            Close();
        }
    }
}

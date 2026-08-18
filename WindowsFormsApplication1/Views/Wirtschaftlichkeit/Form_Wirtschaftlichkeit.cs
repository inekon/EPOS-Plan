using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Dialog-Wrapper um <see cref="UcWirtschaftlichkeit"/>.
    ///
    /// Seit dem Umbau des Reiters „Berichte &amp; Kosten" ist die Kapitalwert-
    /// Vergleichsansicht dort als eigene Seite eingebettet; der gesamte Inhalt
    /// (Aufbau, Laden, Berechnen, Anzeige) liegt deshalb im UserControl. Dieses
    /// Formular bleibt als schmale Hülle bestehen, damit vorhandene und künftige
    /// Aufrufer weiterhin
    /// <c>new Form_Wirtschaftlichkeit(idProjekt).ShowDialog();</c>
    /// verwenden können. Ist das Projekt eine Variante, löst das UserControl
    /// selbst auf deren Stammprojekt auf.
    /// </summary>
    public class Form_Wirtschaftlichkeit : Form
    {
        private readonly UcWirtschaftlichkeit _seite;

        public Form_Wirtschaftlichkeit(int idProjekt)
        {
            _seite = new UcWirtschaftlichkeit(idProjekt)
            {
                Dock = DockStyle.Fill,
                AlsDialog = true
            };
            _seite.SchliessenAngefordert += (s, e) => Close();

            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(900, 536);
            this.MinimumSize = new Size(900, 480);   // Client ≥ ~884 px: btnTarif überlappt lblStatus nicht (Review 11)
            this.Font = new Font("Segoe UI", 9f);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Name = "Form_Wirtschaftlichkeit";
            this.Text = _seite.Titel;
            this.Controls.Add(_seite);
            this.Load += (s, e) => _seite.LadeDaten();
            this.FormClosing += Form_Wirtschaftlichkeit_FormClosing;
        }

        private void Form_Wirtschaftlichkeit_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Erst den laufenden Vorgang beenden, dann schließen (Verhalten wie bisher).
            if (_seite.Beschaeftigt) { _seite.Abbrechen(); e.Cancel = true; }
        }
    }
}

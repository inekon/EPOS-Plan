using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Dialog-Wrapper um <see cref="UcBericht"/>.
    ///
    /// Seit dem Umbau des Reiters „Berichte &amp; Kosten" ist die Berichtserstellung
    /// dort als eigene Seite eingebettet; der gesamte Inhalt (Variantenliste,
    /// Bausteine, Ausgabe, Erstellen) liegt deshalb im UserControl. Dieses Formular
    /// bleibt als schmale Hülle bestehen, damit vorhandene und künftige Aufrufer
    /// weiterhin <c>new Form_Bericht(idStamm, stammName).ShowDialog();</c>
    /// verwenden können.
    ///
    /// Aufruf immer vom Stammprojekt aus; ist eine Variante aktiv, ermittelt der
    /// Aufrufer vorher deren Stamm (VariantenCtrl.StammRefDerVariante).
    /// </summary>
    public class Form_Bericht : Form
    {
        private readonly UcBericht _seite;

        public Form_Bericht(int idStamm, string stammName)
        {
            _seite = new UcBericht(idStamm, stammName)
            {
                Dock = DockStyle.Fill,
                AlsDialog = true
            };
            _seite.SchliessenAngefordert += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                Close();
            };

            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(730, 436);
            this.MinimumSize = new Size(700, 420);
            this.Font = new Font("Segoe UI", 9f);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Name = "Form_Bericht";
            this.Text = _seite.Titel;
            this.Controls.Add(_seite);
            this.Load += (s, e) => _seite.LadeDatenEinmalig();
            this.FormClosing += Form_Bericht_FormClosing;
        }

        private void Form_Bericht_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Erst den laufenden Vorgang beenden, dann schließen (Verhalten wie bisher).
            if (_seite.Beschaeftigt) { _seite.Abbrechen(); e.Cancel = true; }
        }
    }
}

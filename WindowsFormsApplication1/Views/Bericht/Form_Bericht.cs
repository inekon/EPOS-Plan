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
            this.Font = new Font("Segoe UI", 9f);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Name = "Form_Bericht";
            this.Text = _seite.Titel;
            this.Controls.Add(_seite);
            this.Load += (s, e) => _seite.LadeDatenEinmalig();
            this.FormClosing += Form_Bericht_FormClosing;

            // Mindestgröße: Die eingebettete Seite hat seit der Nacharbeit vom
            // 21.08.2026 eine MinimumSize von 730 x 436 (Entwurfsmaß) — darunter
            // überlagern sich ihre Bottom|Right- und Top|Right-verankerten Gruppen.
            // Die Hülle muss deshalb dafür sorgen, dass ihre CLIENT-Fläche dieses Maß
            // nie unterschreitet. Form.MinimumSize ist aber ein AUSSENMASS: Der Bestand
            // stand auf 700 x 420 und ließ die Client-Fläche damit auf 684 x 381 fallen,
            // also deutlich unter den Entwurf.
            //
            // this.Size ist an dieser Stelle genau „ClientSize + Rahmen + Titelzeile" —
            // WinForms hat die Differenz beim Setzen von ClientSize selbst gerechnet.
            // Sie hier zu übernehmen ist deshalb belastbar und kommt ohne geratene
            // Rahmenpixel aus (die je nach Berandung und Systemeinstellung anders
            // ausfallen; auf dem Prüfsystem 16 x 39).
            this.MinimumSize = this.Size;

            // Notebook-Schutz: Fenster in die Arbeitsflaeche des Bildschirms einpassen
            // und den Inhalt per Bildlauf erreichbar halten
            // (Allgemein\FensterEinpassung.cs). Auf ausreichend grossen Schirmen
            // wirkungslos. Sie darf die eben gesetzte Mindestgröße auf kleinen Schirmen
            // ABSENKEN (GroesseKlemmen senkt MinimumSize genau auf der Achse, die nicht
            // mehr passt) — das ist gewollt: Ein Fenster, das nicht auf den Bildschirm
            // passt, ist der schwerere Fehler als eine unterschrittene Mindestgröße, und
            // die Einpassung schaltet dafür Bildlaufleisten zu.
            FensterEinpassung.Einhaengen(this);
        }

        private void Form_Bericht_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Erst den laufenden Vorgang beenden, dann schließen (Verhalten wie bisher).
            if (_seite.Beschaeftigt) { _seite.Abbrechen(); e.Cancel = true; }
        }
    }
}

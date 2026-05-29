using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_HelpPopup : Form
    {
        private string _targetUrl = "";
        private Timer _closeDelayTimer;

        public Form_HelpPopup()
        {
            InitializeComponent();

            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.None; // Falls nicht schon im Designer gesetzt

            // LinkLabel-Klick-Event einrichten
            this.linkLabel_Doku.LinkClicked += LinkLabel_Doku_LinkClicked;

            // Timer für das verzögerte Schließen initialisieren
            _closeDelayTimer = new Timer();
            _closeDelayTimer.Interval = 50; // 50 Millisekunden Puffer gegen Flackern
            _closeDelayTimer.Tick += CloseDelayTimer_Tick;

            // Events für alle Beteiligten registrieren
            this.MouseLeave += (s, e) => StartCloseCheck();
            this.linkLabel_Doku.MouseLeave += (s, e) => StartCloseCheck();

            // WICHTIG: Wenn die Maus INS Fenster eintritt, stoppen wir das Schließen sofort!
            this.MouseEnter += (s, e) => StopCloseCheck();
            this.linkLabel_Doku.MouseEnter += (s, e) => StopCloseCheck();
        }

        public void ShowHelp(string titel, string url, Point position)
        {
            // Verhindert, dass ein laufender Schließ-Timer das Fenster sofort wieder killt
            StopCloseCheck();

            _targetUrl = url;

            // Text im LinkLabel formatieren
            linkLabel_Doku.Text = $"Kapitel: {titel}\r\n➔ Hier klicken für Online-Doku";

            // Y-Versatz minimal auf +25 erhöhen, um der Maus mehr "Luft" zu geben,
            // damit sie beim Erscheinen nicht direkt AUF dem Fenster landet.
            this.Location = new Point(position.X + 15, position.Y + 25);

            if (!this.Visible)
            {
                this.Show();
            }
        }

        private void StartCloseCheck()
        {
            // Starte die Überprüfung verzögert
            _closeDelayTimer.Start();
        }

        private void StopCloseCheck()
        {
            // Maus ist wieder im Fenster -> Schließen abbrechen!
            _closeDelayTimer.Stop();
        }

        private void CloseDelayTimer_Tick(object sender, EventArgs e)
        {
            _closeDelayTimer.Stop();

            // Jetzt prüfen wir unfehlbar anhand der echten Bildschirmkoordinaten,
            // ob sich die Maus WIRKLICH außerhalb des gesamten Popup-Fensters befindet
            if (!this.Bounds.Contains(Cursor.Position))
            {
                this.Hide();
            }
        }

        private void LinkLabel_Doku_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_targetUrl))
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = _targetUrl, UseShellExecute = true });
                    this.Hide();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Fehler beim Öffnen des Links: " + ex.Message);
                }
            }
        }

    }
}
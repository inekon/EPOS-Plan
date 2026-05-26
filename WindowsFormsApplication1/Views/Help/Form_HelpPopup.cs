using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_HelpPopup : Form
    {
        private string _targetUrl = "";

        public Form_HelpPopup()
        {
            InitializeComponent();

            this.TopMost = true;

            // LinkLabel-Klick-Event einrichten
            this.linkLabel_Doku.LinkClicked += LinkLabel_Doku_LinkClicked;

            // Wenn die Maus das Popup verlässt, schließt es sich automatisch wieder
            this.MouseLeave += (s, e) => { CheckMouseLeave(); };
            this.linkLabel_Doku.MouseLeave += (s, e) => { CheckMouseLeave(); };
        }

        // Methode, um das Fenster mit Daten zu füttern und anzuzeigen
        public void ShowHelp(string titel, string url, Point position)
        {
            _targetUrl = url;

            // Text im LinkLabel formatieren
            linkLabel_Doku.Text = $"Kapitel: {titel}\r\n➔ Hier klicken für Online-Doku";

            // Position knapp unter dem Mauszeiger setzen
            this.Location = new Point(position.X + 15, position.Y + 20);
            this.Show();
        }

        private void LinkLabel_Doku_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_targetUrl))
            {
                Process.Start(new ProcessStartInfo { FileName = _targetUrl, UseShellExecute = true });
                this.Hide(); // Nach Klick schließen
            }
        }

        private void CheckMouseLeave()
        {
            // Nur schließen, wenn die Maus WIRKLICH nicht mehr über dem Fenster schwebt
            if (!this.Bounds.Contains(Cursor.Position))
            {
                this.Hide();
            }
        }
    }
}
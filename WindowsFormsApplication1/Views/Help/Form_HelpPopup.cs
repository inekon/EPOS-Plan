using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_HelpPopup : Form, IMessageFilter
    {
        // Fensternachrichten, die ein angeheftetes Popup beenden duerfen.
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int WM_NCRBUTTONDOWN = 0x00A4;

        private string _targetUrl = "";
        private Timer _closeDelayTimer;

        // Der Nachrichtenfilter laeuft nur, solange das Popup angeheftet ist.
        private bool _filterAktiv;

        /// <summary>
        /// F1: Angeheftet bleibt das Popup stehen, bis der Anwender es schliesst
        /// oder woanders hinklickt. Der Schliess-Timer ruht solange.
        /// </summary>
        public bool IstAngeheftet { get; private set; }

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

        /// <summary>
        /// Flüchtige Kurzinfo beim Überfahren: verschwindet wieder, sobald die
        /// Maus Steuerelement und Popup verlassen hat.
        /// </summary>
        public void ShowHelp(string titel, string url, Point position)
        {
            Anzeigen(titel, url, position, angeheftet: false);
        }

        /// <summary>
        /// F1 - der Klick führt: Das Popup wird angeheftet und bleibt stehen, bis
        /// der Anwender es schließt (Esc) oder woanders hin klickt.
        /// </summary>
        public void ShowHelpAngeheftet(string titel, string url, Point position)
        {
            Anzeigen(titel, url, position, angeheftet: true);
        }

        private void Anzeigen(string titel, string url, Point position, bool angeheftet)
        {
            // Verhindert, dass ein laufender Schließ-Timer das Fenster sofort wieder killt
            StopCloseCheck();

            _targetUrl = url;
            IstAngeheftet = angeheftet;

            // Text im LinkLabel formatieren.
            //
            // Drei-Schichten-Regel (A4): Anzeigetexte ausschließlich über
            // MyResource.Resource.* — bis H1 standen sie hier fest auf Deutsch.
            string kapitel = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                MyResource.Resource.HILFE_POPUP_KAPITEL, titel);
            string verweis = "➔ " + MyResource.Resource.HILFE_POPUP_LINK;

            linkLabel_Doku.Text = angeheftet
                ? kapitel + "\r\n" + verweis + "\r\n" + MyResource.Resource.HILFE_POPUP_ESC
                : kapitel + "\r\n" + verweis;

            // Y-Versatz minimal auf +25 erhöhen, um der Maus mehr "Luft" zu geben,
            // damit sie beim Erscheinen nicht direkt AUF dem Fenster landet.
            //
            // Die endgültige Größe steht erst nach dem Textwechsel fest (AutoSize) —
            // vor der Randprüfung Layout erzwingen, sonst rechnet sie mit der alten Breite.
            this.PerformLayout();
            Size groesse = this.Size;
            Size bevorzugt = this.PreferredSize;
            if (bevorzugt.Width > groesse.Width) groesse.Width = bevorzugt.Width;
            if (bevorzugt.Height > groesse.Height) groesse.Height = bevorzugt.Height;

            // Knöpfe am rechten oder unteren Fensterrand: das Popup darf den
            // Arbeitsbereich des Monitors nicht verlassen, sonst ist der Link
            // unerreichbar — dann links neben bzw. oberhalb des Knopfs öffnen.
            Rectangle bereich = Screen.FromPoint(position).WorkingArea;
            int x = position.X + 15;
            int y = position.Y + 25;
            if (x + groesse.Width > bereich.Right) x = position.X - 15 - groesse.Width;
            if (y + groesse.Height > bereich.Bottom) y = position.Y - 25 - groesse.Height;
            if (x < bereich.Left) x = bereich.Left;
            if (y < bereich.Top) y = bereich.Top;
            this.Location = new Point(x, y);

            if (!this.Visible)
            {
                this.Show();
            }

            if (angeheftet) FilterEinschalten();
            else FilterAusschalten();
        }

        /// <summary>
        /// Löst die Anheftung und blendet das Popup aus.
        /// </summary>
        public void Schliessen()
        {
            IstAngeheftet = false;
            FilterAusschalten();
            StopCloseCheck();
            this.Hide();
        }

        private void FilterEinschalten()
        {
            if (_filterAktiv) return;

            Application.AddMessageFilter(this);
            _filterAktiv = true;
        }

        private void FilterAusschalten()
        {
            if (!_filterAktiv) return;

            Application.RemoveMessageFilter(this);
            _filterAktiv = false;
        }

        /// <summary>
        /// Solange das Popup angeheftet ist, wird anwendungsweit mitgehört: ein Klick
        /// neben das Popup oder Esc beendet es. Das Popup wird dafür bewusst NICHT
        /// aktiviert - der Klick soll sein eigentliches Ziel trotzdem erreichen.
        /// </summary>
        public bool PreFilterMessage(ref Message m)
        {
            if (!IstAngeheftet || !this.Visible) return false;

            switch (m.Msg)
            {
                case WM_LBUTTONDOWN:
                case WM_RBUTTONDOWN:
                case WM_MBUTTONDOWN:
                case WM_NCLBUTTONDOWN:
                case WM_NCRBUTTONDOWN:
                    // Ein Klick INS Popup gehört dem Popup (Link anklicken).
                    if (!this.Bounds.Contains(Cursor.Position)) Schliessen();
                    return false;

                case WM_KEYDOWN:
                case WM_SYSKEYDOWN:
                    if ((Keys)m.WParam.ToInt32() == Keys.Escape)
                    {
                        Schliessen();
                        return true; // Esc ist verbraucht.
                    }
                    return false;
            }

            return false;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            // Ein zurückgelassener Nachrichtenfilter überlebte das Fenster.
            FilterAusschalten();
            base.OnHandleDestroyed(e);
        }

        private void StartCloseCheck()
        {
            // Angeheftet bleibt stehen - nur der Anwender schließt (F1).
            if (IstAngeheftet) return;

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

            // Zweiter Riegel: ein angeheftetes Popup darf der Timer nicht wegnehmen.
            if (IstAngeheftet) return;

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
                    // A6 / Entscheid 7.1a: Bei englischer Oberfläche wird die
                    // deutsche Wiki-Seite durch den Übersetzungs-Proxy geleitet;
                    // sonst (und bei jedem Fehler) bleibt es die Original-URL.
                    string _anzeigeUrl = DokuUebersetzung.FuerAnzeige(_targetUrl);

                    Process.Start(new ProcessStartInfo { FileName = _anzeigeUrl, UseShellExecute = true });
                    Schliessen();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Fehler beim Öffnen des Links: " + ex.Message);
                }
            }
        }

    }
}
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
            // Schonfrist beim Verlassen des Popups selbst: 50 ms waren nur ein
            // Flacker-Puffer und fuer den Weg zum Link zu knapp (Befund 29.08.).
            _closeDelayTimer.Interval = 300;
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
        /// <param name="beschreibung">
        /// H11 (7.6) - der Einleitungssatz der Hilfeseite. Leer ist der Regelfall
        /// (offline, alte Sicherung, Nachladelauf noch nicht durch) und ergibt
        /// exakt das Erscheinungsbild vor H11.
        /// </param>
        public void ShowHelp(string titel, string beschreibung, string url, Point position)
        {
            Anzeigen(titel, beschreibung, url, position, angeheftet: false);
        }

        /// <summary>
        /// F1 - der Klick führt: Das Popup wird angeheftet und bleibt stehen, bis
        /// der Anwender es schließt (Esc) oder woanders hin klickt.
        /// </summary>
        public void ShowHelpAngeheftet(string titel, string beschreibung, string url, Point position)
        {
            Anzeigen(titel, beschreibung, url, position, angeheftet: true);
        }

        private void Anzeigen(string titel, string beschreibung, string url, Point position, bool angeheftet)
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

            // H11 (7.6): Der Einleitungssatz der Wiki-Seite steht zwischen
            // Kapitelzeile und Verweis. Ist keiner da, bleibt alles wie bisher.
            string kurzbeschreibung = BeschreibungUmbrechen(beschreibung);

            string kopf = kurzbeschreibung.Length > 0
                ? kapitel + "\r\n" + kurzbeschreibung + "\r\n"
                : kapitel + "\r\n";

            linkLabel_Doku.Text = angeheftet
                ? kopf + verweis + "\r\n" + MyResource.Resource.HILFE_POPUP_ESC
                : kopf + verweis;

            // Nur der Verweis darf wie ein Link aussehen - sonst waere die
            // Kurzbeschreibung unterstrichen und blau.
            //
            // Ohne Beschreibung wird der Linkbereich ausdruecklich auf den GANZEN
            // Text zurueckgestellt: Das ist das Verhalten vor H11 (der Standard
            // eines LinkLabel ohne gesetzten LinkArea), und einmal gesetzt wandert
            // der Bereich beim naechsten Textwechsel nicht von selbst mit.
            linkLabel_Doku.LinkArea = kurzbeschreibung.Length > 0
                ? new LinkArea(kopf.Length, verweis.Length)
                : new LinkArea(0, linkLabel_Doku.Text.Length);

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

        // --------------------------------------------------------------------
        // H11 (7.6) - Umbruch der Kurzbeschreibung
        // --------------------------------------------------------------------

        /// <summary>Ziellänge einer Zeile der Kurzbeschreibung, in Zeichen.</summary>
        internal const int BESCHREIBUNG_ZEICHEN = 70;

        /// <summary>Mehr als so viele Zeilen zeigt das Popup nicht.</summary>
        internal const int BESCHREIBUNG_ZEILEN = 2;

        /// <summary>
        /// Bricht die Kurzbeschreibung an Wortgrenzen auf höchstens
        /// <see cref="BESCHREIBUNG_ZEILEN"/> Zeilen zu je rund
        /// <see cref="BESCHREIBUNG_ZEICHEN"/> Zeichen um. Was nicht mehr
        /// hineinpasst, endet mit einem Auslassungszeichen.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Umgebrochen wird über die Zeichenzahl, nicht über die gemessene
        /// Textbreite. Das ist bewusst grob: Das Popup ist AutoSize, die
        /// Randklemmung in <c>Anzeigen</c> holt jede Breite wieder auf den
        /// Bildschirm, und eine Messung über <c>TextRenderer</c> bräuchte einen
        /// Grafikkontext an einer Stelle, die sonst ohne auskommt.
        /// </para>
        /// <para>
        /// Ein einzelnes überlanges Wort wird NICHT getrennt - es bekommt seine
        /// Zeile und darf länger sein. Getrennte Fachwörter wären schlimmer als
        /// eine zu lange Zeile.
        /// </para>
        /// <para>
        /// <c>internal</c> statt <c>private</c>, damit der Prüfstand die Kappung
        /// ohne Bildschirm nachrechnen kann.
        /// </para>
        /// </remarks>
        internal static string BeschreibungUmbrechen(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            // Der Katalog liefert bereits einzeilig; ein Umbruch aus einer alten
            // Sicherung würde die Zeilenrechnung sonst durcheinanderbringen.
            string flach = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
            if (flach.Length == 0) return "";

            string[] woerter = flach.Split(' ');
            var zeilen = new System.Collections.Generic.List<string>();
            var aktuell = new System.Text.StringBuilder();

            int i = 0;
            for (; i < woerter.Length; i++)
            {
                string wort = woerter[i];

                if (aktuell.Length == 0)
                {
                    aktuell.Append(wort);
                }
                else if (aktuell.Length + 1 + wort.Length <= BESCHREIBUNG_ZEICHEN)
                {
                    aktuell.Append(' ').Append(wort);
                }
                else if (zeilen.Count + 1 >= BESCHREIBUNG_ZEILEN)
                {
                    // Die angefangene Zeile ist die letzte erlaubte - hier ist Schluss.
                    break;
                }
                else
                {
                    zeilen.Add(aktuell.ToString());
                    aktuell.Clear();
                    aktuell.Append(wort);
                }
            }

            // Rest übrig? Dann wurde gekappt und das muss man sehen.
            if (i < woerter.Length) aktuell.Append('…');

            zeilen.Add(aktuell.ToString());

            return string.Join("\r\n", zeilen);
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
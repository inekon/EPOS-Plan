using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Rechtshinweis zum KI-Assistenten - einmal als Einwilligung vor der ersten
    /// Nutzung, danach jederzeit zum Nachlesen über das Chatfenster.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Der Text beschreibt ausschließlich den TATSÄCHLICHEN Datenfluss (Ressourcen
    /// <c>KI_HINWEIS_*</c>): was hinausgeht, was zusätzlich im Aktionsbetrieb hinausgeht,
    /// was nicht hinausgeht, wer Empfänger ist, was der Anwender beachten muss, wer
    /// verantwortlich ist und wie sich alles abschalten lässt. Er gibt keine
    /// rechtlichen Zusicherungen ab.
    /// </para>
    /// <para>
    /// <b>Wird der Text inhaltlich geändert, muss <see cref="KiEinwilligung.FASSUNG"/>
    /// erhöht werden</b> - dann wird die Einwilligung erneut eingeholt.
    /// </para>
    /// <para>
    /// Komplett programmatisch aufgebaut (kein Designer, keine .resx) - dasselbe
    /// Hausmuster wie <see cref="Form_KiChat"/>.
    /// </para>
    /// </remarks>
    public class Form_KiHinweis : Form
    {
        private readonly bool _mitEinwilligung;
        private RichTextBox _text;

        /// <param name="mitEinwilligung">
        /// <c>true</c> = Einwilligung einholen („Verstanden und einverstanden" /
        /// „Abbrechen"), <c>false</c> = nur nachlesen („Schließen").
        /// </param>
        public Form_KiHinweis(bool mitEinwilligung)
        {
            _mitEinwilligung = mitEinwilligung;
            BaueOberflaeche();
            TextAufbauen();
        }

        // ------------------------------------------------------------------
        // Einstiegspunkte
        // ------------------------------------------------------------------

        /// <summary>
        /// Hängt diesen Dialog als Nachfrage der <see cref="KiEinwilligung"/> ein.
        /// Aufruf einmalig beim Programmstart (<c>Program.Main</c>).
        /// </summary>
        /// <remarks>
        /// Ohne diesen Aufruf gibt es keinen Weg zu einer Einwilligung - und damit auch
        /// keine Übertragung. Genau darauf baut der Aktionsharnisch: er hängt nichts ein
        /// und weist damit nach, dass ohne Einwilligung nichts gesendet wird.
        /// </remarks>
        public static void Einhaengen()
        {
            KiEinwilligung.Nachfragen = () => Einholen();
        }

        /// <summary>
        /// Zeigt den Hinweis zur Bestätigung. Rückgabe <c>true</c> = eingewilligt.
        /// Merkt die Einwilligung NICHT selbst - das tut <see cref="KiEinwilligung"/>.
        /// </summary>
        public static bool Einholen(IWin32Window besitzer = null)
        {
            Control anker = (besitzer as Control) ?? Form.ActiveForm;

            // Der Riegel wird aus dem Dienst heraus gezogen. Läuft der ausnahmsweise
            // nicht auf dem Oberflächenstrang, muss der Dialog trotzdem dort erscheinen.
            if (anker != null && anker.InvokeRequired)
                return (bool)anker.Invoke(new Func<bool>(() => Zeigen(anker, true)));

            return Zeigen(anker, true);
        }

        /// <summary>Zeigt den Hinweis zum Nachlesen; ändert nichts.</summary>
        public static void Anzeigen(IWin32Window besitzer = null)
        {
            Zeigen((besitzer as Control) ?? Form.ActiveForm, false);
        }

        private static bool Zeigen(Control anker, bool mitEinwilligung)
        {
            using (Form_KiHinweis frm = new Form_KiHinweis(mitEinwilligung))
            {
                DialogResult ergebnis = anker != null ? frm.ShowDialog(anker) : frm.ShowDialog();
                return ergebnis == DialogResult.OK;
            }
        }

        // ------------------------------------------------------------------
        // Oberfläche
        // ------------------------------------------------------------------

        private void BaueOberflaeche()
        {
            this.Text = MyResource.Resource.KI_HINWEIS_FENSTER;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;
            this.ClientSize = new Size(660, 560);
            this.MinimumSize = new Size(560, 420);

            _text = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                Margin = new Padding(0)
            };

            Panel rahmen = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 10, 10, 6) };
            rahmen.Controls.Add(_text);

            // --- Fußzeile: Stand der Einwilligung links, Schaltflächen rechts ---
            Panel fuss = new Panel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(14, 8, 10, 8) };

            Label stand = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Text = StandText()
            };

            FlowLayoutPanel rechts = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false
            };

            if (_mitEinwilligung)
            {
                Button abbruch = new Button
                {
                    Text = MyResource.Resource.KI_HINWEIS_ABBRECHEN,
                    DialogResult = DialogResult.Cancel,
                    Width = 100,
                    Height = 28,
                    Margin = new Padding(6, 0, 0, 0)
                };
                Button ok = new Button
                {
                    Text = MyResource.Resource.KI_HINWEIS_OK,
                    DialogResult = DialogResult.OK,
                    Width = 190,
                    Height = 28,
                    Margin = new Padding(6, 0, 0, 0)
                };

                rechts.Controls.Add(abbruch);   // ganz rechts
                rechts.Controls.Add(ok);

                this.AcceptButton = ok;
                this.CancelButton = abbruch;
            }
            else
            {
                Button schliessen = new Button
                {
                    Text = MyResource.Resource.KI_HINWEIS_SCHLIESSEN,
                    DialogResult = DialogResult.Cancel,
                    Width = 100,
                    Height = 28,
                    Margin = new Padding(6, 0, 0, 0)
                };
                rechts.Controls.Add(schliessen);

                this.AcceptButton = schliessen;
                this.CancelButton = schliessen;
            }

            // Reihenfolge beachten: Fill zuerst, dann die andockenden Elemente
            fuss.Controls.Add(stand);
            fuss.Controls.Add(rechts);

            this.Controls.Add(rahmen);
            this.Controls.Add(fuss);
        }

        /// <summary>Zeile über den Stand der Einwilligung - auch beim Nachlesen sichtbar.</summary>
        private static string StandText()
        {
            int fassung = KiEinwilligung.BestaetigteFassung;
            string am = KiEinwilligung.BestaetigtAm;

            if (fassung <= 0) return MyResource.Resource.KI_HINWEIS_STAND_NEIN;

            if (fassung < KiEinwilligung.FASSUNG)
                return string.Format(MyResource.Resource.KI_HINWEIS_STAND_ALT,
                                     am, fassung, KiEinwilligung.FASSUNG);

            return string.Format(MyResource.Resource.KI_HINWEIS_STAND_JA, am, fassung);
        }

        // ------------------------------------------------------------------
        // Der Text
        // ------------------------------------------------------------------

        private void TextAufbauen()
        {
            _text.Clear();

            Ueberschrift(MyResource.Resource.KI_HINWEIS_TITEL, 12f);
            Grau(string.Format(MyResource.Resource.KI_HINWEIS_FASSUNG, KiEinwilligung.FASSUNG));
            Leer();

            Absatz(MyResource.Resource.KI_HINWEIS_EINLEITUNG);
            Leer();

            Abschnitt(MyResource.Resource.KI_HINWEIS_UEB_UEBERTRAGEN,
                      MyResource.Resource.KI_HINWEIS_UEBERTRAGEN);
            Abschnitt(MyResource.Resource.KI_HINWEIS_UEB_AKTIONEN,
                      MyResource.Resource.KI_HINWEIS_AKTIONEN);
            Abschnitt(MyResource.Resource.KI_HINWEIS_UEB_NICHT,
                      MyResource.Resource.KI_HINWEIS_NICHT);
            Abschnitt(MyResource.Resource.KI_HINWEIS_UEB_EMPFAENGER,
                      MyResource.Resource.KI_HINWEIS_EMPFAENGER);
            Abschnitt(MyResource.Resource.KI_HINWEIS_UEB_BEACHTEN,
                      MyResource.Resource.KI_HINWEIS_BEACHTEN);
            Abschnitt(MyResource.Resource.KI_HINWEIS_UEB_VERANTWORTUNG,
                      MyResource.Resource.KI_HINWEIS_VERANTWORTUNG);
            Abschnitt(MyResource.Resource.KI_HINWEIS_UEB_ABSCHALTEN,
                      MyResource.Resource.KI_HINWEIS_ABSCHALTEN);

            _text.SelectionStart = 0;
            _text.SelectionLength = 0;
            _text.ScrollToCaret();
        }

        private void Abschnitt(string ueberschrift, string inhalt)
        {
            Ueberschrift(ueberschrift, 9.5f);
            Absatz(inhalt);
            Leer();
        }

        private void Ueberschrift(string text, float groesse)
        {
            Schreibe(text, new Font("Segoe UI", groesse, FontStyle.Bold), Color.FromArgb(0, 90, 160));
        }

        private void Absatz(string text)
        {
            Schreibe(text, new Font("Segoe UI", 9.5f, FontStyle.Regular), Color.Black);
        }

        private void Grau(string text)
        {
            Schreibe(text, new Font("Segoe UI", 8.5f, FontStyle.Regular), Color.DimGray);
        }

        private void Leer()
        {
            Schreibe("", new Font("Segoe UI", 5f, FontStyle.Regular), Color.Black);
        }

        private void Schreibe(string text, Font schrift, Color farbe)
        {
            // Die Ressourcen tragen die Zeilenumbrüche als "\n" (XML normalisiert CRLF);
            // die Anzeige braucht die Umbrüche der Plattform.
            string sichtbar = (text ?? "").Replace("\r\n", "\n").Replace("\n", Environment.NewLine);

            _text.SelectionStart = _text.TextLength;
            _text.SelectionLength = 0;
            _text.SelectionFont = schrift;
            _text.SelectionColor = farbe;
            _text.AppendText(sichtbar + Environment.NewLine);
        }
    }
}

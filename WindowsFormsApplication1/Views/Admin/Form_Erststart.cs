using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das Fortschrittsfenster der einmaligen Datenbankumstellung (Arbeitspaket S8).
    ///
    /// <para><b>Eine dünne Hülle, mehr nicht.</b> Sämtliche Entscheidungen und der
    /// gesamte Ablauf stehen in <see cref="ErststartMigration"/> - dieses Fenster fragt,
    /// zeigt an und meldet zurück. Was es hier NICHT gibt, ist Absicht:</para>
    /// <list type="bullet">
    ///   <item><description><b>Kein Abbrechen während des Laufs.</b> Ein Abbruch mitten in
    ///     der Übertragung hinterließe eine halbe Zieldatei. Der Kern räumt bei jedem
    ///     Fehler selbst auf; ein Anwenderabbruch wäre ein zweiter, deutlich schlechterer
    ///     Weg dorthin. Vor dem Start lässt sich das Fenster jederzeit beenden - dann
    ///     bleibt der Access-Bestand unangetastet liegen.</description></item>
    ///   <item><description><b>Kein Designer und keine .resx</b> - Hausmuster wie
    ///     <see cref="Form_KiHinweis"/>. Der Designer würde bei 150 % Skalierung die
    ///     AutoScale-Basis verschreiben; ein programmatisch gebautes Fenster kann das
    ///     nicht passieren.</description></item>
    /// </list>
    ///
    /// <para>Die Texte stehen bewusst als deutsche Literale hier, wie schon die
    /// Startprüfung in <c>Program.Main</c>: Der Dialog erscheint genau einmal je
    /// Bestand, VOR dem ersten Datenbankzugriff. Der Ressourcenbestand
    /// (<c>MyResource.Resource</c>) wird mit dem übrigen Textbestand der Umstellung in
    /// einem Zug nachgezogen.</para>
    /// </summary>
    public class Form_Erststart : Form
    {
        private readonly string _ordner;
        private readonly bool _settingsFixup;

        private Label _status;
        private ProgressBar _balken;
        private TextBox _protokoll;
        private Button _starten;
        private Button _beenden;

        private bool _laufend;
        private bool _erfolg;
        private string _berichtPfad;

        private Form_Erststart(string dbOrdner, bool settingsFixup)
        {
            _ordner = dbOrdner;
            _settingsFixup = settingsFixup;
            BaueOberflaeche();
        }

        // ------------------------------------------------------------------
        // Einstiegspunkt
        // ------------------------------------------------------------------

        /// <summary>
        /// Zeigt den Assistenten und führt die Umstellung durch, wenn der Anwender
        /// zustimmt.
        /// </summary>
        /// <param name="dbOrdner">Ordner mit <c>Kenndaten.accdb</c>.</param>
        /// <param name="settingsFixup">Gespeicherten <c>DBName</c> mit umstellen (N7).</param>
        /// <param name="berichtPfad">Pfad des Migrationsberichts, sofern einer entstand.</param>
        /// <returns>
        /// <c>true</c> = die SQLite-Datei steht; das Programm kann normal weiterstarten.
        /// <c>false</c> = abgelehnt oder fehlgeschlagen; der Grund steht in
        /// <see cref="ErststartMigration.LetzteMeldung"/>.
        /// </returns>
        public static bool Zeigen(string dbOrdner, bool settingsFixup, out string berichtPfad)
        {
            using (Form_Erststart frm = new Form_Erststart(dbOrdner, settingsFixup))
            {
                frm.ShowDialog();
                berichtPfad = frm._berichtPfad;
                return frm._erfolg;
            }
        }

        // ------------------------------------------------------------------
        // Oberfläche
        // ------------------------------------------------------------------

        private void BaueOberflaeche()
        {
            this.Text = "Datenbankumstellung";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ShowInTaskbar = true;
            this.ClientSize = new Size(680, 460);
            this.MinimumSize = new Size(600, 400);
            this.FormBorderStyle = FormBorderStyle.Sizable;

            Label kopf = new Label
            {
                Dock = DockStyle.Top,
                Height = 158,
                Padding = new Padding(14, 12, 14, 4),
                Text =
                    "Die Datenbank dieses Rechners liegt noch im alten Access-Format vor und wird " +
                    "jetzt einmalig auf das neue Format umgestellt." + Environment.NewLine +
                    Environment.NewLine +
                    "Ordner: " + _ordner + Environment.NewLine +
                    Environment.NewLine +
                    "Ablauf:" + Environment.NewLine +
                    "   1. " + ErststartMigration.ACCDB_DATEI + " wird auf den letzten Access-Stand gebracht." + Environment.NewLine +
                    "   2. Alle Daten werden nach " + ErststartMigration.SQLITE_DATEI + " übertragen und Tabelle " +
                    "für Tabelle nachgezählt und geprüft." + Environment.NewLine +
                    "   3. Die Altdatei bleibt als " + ErststartMigration.ACCDB_UMBENANNT + " liegen." + Environment.NewLine +
                    Environment.NewLine +
                    "Das dauert je nach Bestand einige Minuten. Bei einem Fehler wird die neue Datei " +
                    "wieder entfernt und die Altdatei bleibt unverändert gültig."
            };

            _status = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Padding = new Padding(14, 2, 14, 2),
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Bereit."
            };

            _balken = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 16,
                Margin = new Padding(14, 0, 14, 0),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 0,
                Visible = false
            };

            _protokoll = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = ScrollBars.Both,
                BackColor = SystemColors.Window,
                Font = new Font(FontFamily.GenericMonospace, 8.25f)
            };

            Panel mitte = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 4, 14, 4) };
            mitte.Controls.Add(_protokoll);

            // --- Fußzeile ---------------------------------------------------
            Panel fuss = new Panel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(14, 8, 14, 8) };

            FlowLayoutPanel rechts = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false
            };

            _beenden = new Button
            {
                Text = "Beenden",
                Width = 120,
                Height = 28,
                Margin = new Padding(6, 0, 0, 0)
            };
            _beenden.Click += (s, e) => { _erfolg = false; Close(); };

            _starten = new Button
            {
                Text = "Jetzt umstellen",
                Width = 150,
                Height = 28,
                Margin = new Padding(6, 0, 0, 0)
            };
            _starten.Click += (s, e) => Starten();

            rechts.Controls.Add(_beenden);   // ganz rechts
            rechts.Controls.Add(_starten);

            fuss.Controls.Add(rechts);

            // Reihenfolge beachten: Fill zuerst, dann die andockenden Elemente
            this.Controls.Add(mitte);
            this.Controls.Add(fuss);
            this.Controls.Add(_balken);
            this.Controls.Add(_status);
            this.Controls.Add(kopf);

            this.AcceptButton = _starten;
            this.CancelButton = _beenden;

            this.FormClosing += (s, e) =>
            {
                // Während der Übertragung gibt es kein Zurück - weder über das Kreuz
                // noch über Alt+F4. Der Kern räumt bei einem echten Fehler selbst auf.
                if (_laufend && e.CloseReason == CloseReason.UserClosing) e.Cancel = true;
            };
        }

        // ------------------------------------------------------------------
        // Ablauf
        // ------------------------------------------------------------------

        private void Starten()
        {
            _laufend = true;
            _starten.Enabled = false;
            _beenden.Enabled = false;
            this.ControlBox = false;
            _balken.Visible = true;
            _balken.MarqueeAnimationSpeed = 30;
            _status.Text = "Umstellung läuft - bitte nicht abschalten.";

            // Progress<T> wird HIER erzeugt, also auf dem Oberflächenstrang: seine
            // Meldungen kommen damit von selbst dort wieder an.
            Progress<string> fortschritt = new Progress<string>(ZeileAnhaengen);

            Thread strang = new Thread(() =>
            {
                bool ok = false;
                string bericht = null;
                try
                {
                    ok = ErststartMigration.Fuehredurch(_ordner, fortschritt, _settingsFixup, out bericht);
                }
                catch (Exception)
                {
                    // Fuehredurch faengt selbst ab und fuellt LetzteMeldung; hier bleibt
                    // nur der Fall "gar nicht erst losgelaufen".
                    ok = false;
                }

                try { BeginInvoke(new Action(() => Fertig(ok, bericht))); }
                catch (Exception) { /* Fenster schon zu - dann ist auch nichts mehr anzuzeigen */ }
            });
            strang.IsBackground = true;
            strang.Name = "Erststart-Datenbankumstellung";
            strang.Start();
        }

        private void ZeileAnhaengen(string zeile)
        {
            if (string.IsNullOrEmpty(zeile)) return;

            _protokoll.AppendText(zeile + Environment.NewLine);

            // Die Statuszeile trägt immer den zuletzt gemeldeten Schritt.
            string kurz = zeile.Trim();
            if (kurz.Length > 0) _status.Text = kurz;
        }

        private void Fertig(bool erfolg, string berichtPfad)
        {
            _laufend = false;
            _erfolg = erfolg;
            _berichtPfad = berichtPfad;

            _balken.MarqueeAnimationSpeed = 0;
            _balken.Visible = false;
            this.ControlBox = true;
            _status.Text = erfolg ? "Umstellung abgeschlossen." : "Umstellung fehlgeschlagen.";

            ZeileAnhaengen(ErststartMigration.LetzteMeldung);

            // Bei Erfolg geht es ohne weiteren Klick weiter - das Programm startet
            // normal. Ein Fehlschlag wird vom Aufrufer gemeldet (mit Berichtspfad).
            Close();
        }
    }
}

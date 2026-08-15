using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Schwellenband eines Pufferspeichers: ein waagerechter Balken 0…100 % mit den
    /// Marken Einschaltschwelle, Abschaltschwelle für Nachrangige und Abschaltschwelle
    /// (Konzept 3a, „Schwellenband 10/70/95-Mini-Balken wie im Entwurf").
    ///
    /// Die drei Zahlen stehen im Dialog <c>Form_PufferSp_Projekt</c> als Eingabefelder;
    /// hier zeigen sie ihre Wirkung: Unterhalb der Einschaltschwelle darf geladen werden,
    /// zwischen Nachrang- und Abschaltschwelle liegt die Reservezone der vorrangigen
    /// Anlage (<see cref="Ladeordnung.ObergrenzenAufloesen"/>).
    /// </summary>
    internal sealed class SchwellenBand : Control
    {
        private double _ein = Ladeordnung.SCHWELLE_EIN_DEFAULT;
        private double _nachrang = Ladeordnung.SCHWELLE_AUS_DEFAULT;
        private double _aus = Ladeordnung.SCHWELLE_AUS_DEFAULT;

        public SchwellenBand()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Height = 12;
            Margin = new Padding(0, 3, 0, 3);

            // KEIN BackColor = Transparent: Ein blankes Control lehnt eine transparente
            // Hintergrundfarbe mit ArgumentException ab, solange
            // ControlStyles.SupportsTransparentBackColor nicht gesetzt ist (im Harness
            // beim Anlegen der ersten Speicherkarte aufgeschlagen). Gebraucht wird die
            // Transparenz hier ohnehin nicht - OnPaint füllt die Fläche zuerst mit der
            // Hintergrundfarbe des übergeordneten Steuerelements.
        }

        public void Setzen(double ein, double nachrang, double aus)
        {
            _ein = ein;
            _nachrang = nachrang;
            _aus = aus;
            Invalidate();
        }

        private int X(double prozent)
        {
            if (prozent < 0) prozent = 0;
            if (prozent > 100) prozent = 100;
            return (int)Math.Round((Width - 1) * prozent / 100.0);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent != null ? Parent.BackColor : SystemColors.Control);

            Rectangle bahn = new Rectangle(0, 2, Math.Max(1, Width - 1), Height - 5);
            using (SolidBrush b = new SolidBrush(KartenStil.FLAECHE)) e.Graphics.FillRectangle(b, bahn);

            // Reservezone der vorrangigen Anlage: zwischen Nachrang- und Abschaltschwelle.
            int xNach = X(_nachrang), xAus = X(_aus);
            if (xAus > xNach)
            {
                using (SolidBrush b = new SolidBrush(KartenStil.BADGE_FLAECHE))
                    e.Graphics.FillRectangle(b, new Rectangle(bahn.X + xNach, bahn.Y,
                                                              xAus - xNach, bahn.Height));
            }

            using (Pen p = new Pen(KartenStil.RAHMEN_LEISE)) e.Graphics.DrawRectangle(p, bahn);

            Marke(e.Graphics, bahn, X(_ein), KartenStil.QUELLE_RAHMEN);
            Marke(e.Graphics, bahn, xNach, KartenStil.TEXT_SEHR_LEISE);
            Marke(e.Graphics, bahn, xAus, KartenStil.SENKE_RAHMEN);
        }

        private static void Marke(Graphics g, Rectangle bahn, int x, Color farbe)
        {
            using (Pen p = new Pen(farbe, 2f))
                g.DrawLine(p, bahn.X + x, bahn.Y - 1, bahn.X + x, bahn.Bottom + 1);
        }
    }

    /// <summary>
    /// ETAPPE D3 (Konzept_KonfigUI_Hydraulik, Abschnitt 3a) — eine KOMPAKTE Karte je
    /// Projekt-Pufferspeicher in der rechten Spalte der Simulationskonfiguration.
    ///
    /// <b>Warum kompakt.</b> Konzept 3a: „Bei mehreren Speichern passen die Vollkarten
    /// nicht in die Spalte." Zugeklappt ist die Karte EINE Zeile (Name,
    /// Verwendungs-Badge, Volumen, Temperaturpaar, rechts die Kurzbilanz
    /// „n Lader · m Abnehmer"); ein Klick klappt sie auf und pinnt die Detailkarte.
    /// Der Mouseover zeigt dieselben Details als Hinweisfenster — beides, wie in 3a
    /// ausdrücklich gefordert. Dass höchstens EINE Karte offen ist, steuert die
    /// Konfigurationsseite (die Karte selbst kennt ihre Nachbarn nicht).
    ///
    /// <b>Invariante S-1 (Konzept Abschnitt 5).</b> „Quelle für" listet ausschließlich
    /// ERZEUGER. Ein Speicher kann weder Senke noch Quelle eines anderen Speichers sein;
    /// diese Karte darf deshalb nie einen anderen Speicher nennen. Die Liste kommt aus
    /// <c>Tab_Energieanlagen.WQ_ID_Puffer</c> — dort stehen von Haus aus nur Anlagen.
    ///
    /// <b>Reine Lesefläche</b>, wie <see cref="ErzeugerKarte"/>: ✎ meldet nur, geöffnet
    /// wird <c>Form_PufferSp_Projekt</c> durch die Konfigurationsseite.
    /// </summary>
    internal sealed class SpeicherKarte : UserControl
    {
        /// <summary>Alles, was die Karte anzeigt — von der Konfigurationsseite gefüllt.</summary>
        public sealed class Daten
        {
            /// <summary>Tab_Pufferspeicher.ID — Kontext für den Editor-Aufruf.</summary>
            public int ID_Puffer;

            public string Bezeichner = "";

            /// <summary>Verwendungs-Badge (Heizung | Warmwasser | …), bereits übersetzt.</summary>
            public string Verwendung = "";

            /// <summary>Volumen mit Einheit, z. B. „778 l"; leer = nicht gepflegt.</summary>
            public string Volumen = "";

            /// <summary>Temperaturpaar, z. B. „55 / 45 °C"; leer = nicht gepflegt.</summary>
            public string Temperaturpaar = "";

            public int LaderAnzahl;
            public int AbnehmerAnzahl;

            /// <summary>Die Zeilen der Detailkarte (Lader, Versorgt, Quelle für, …).</summary>
            public List<string> Detailzeilen = new List<string>();

            /// <summary>Beschriftung unter dem Schwellenband, z. B. „Schwellen 10 / 70 / 95 %".</summary>
            public string Schwellentext = "";

            public double SchwelleEin = Ladeordnung.SCHWELLE_EIN_DEFAULT;
            public double SchwelleAusNachrang = Ladeordnung.SCHWELLE_AUS_DEFAULT;
            public double SchwelleAus = Ladeordnung.SCHWELLE_AUS_DEFAULT;
        }

        private readonly Label _lblPfeil = new Label();
        private readonly Label _lblName = new Label();
        private readonly FlowLayoutPanel _kopfChips = new FlowLayoutPanel();
        private readonly Label _lblBilanz = new Label();
        private readonly Label _lnkEdit = new Label();
        private readonly FlowLayoutPanel _detail = new FlowLayoutPanel();
        private readonly SchwellenBand _band = new SchwellenBand();
        private readonly Label _lblSchwellen = new Label();
        private readonly ToolTip _tip = new ToolTip();

        private bool _aufbau;
        private bool _aufgeklappt;

        /// <summary>Der Puffer, den die Karte zeigt (Tab_Pufferspeicher.ID).</summary>
        public int ID_Puffer { get; private set; }

        /// <summary>Klick auf die zugeklappte Zeile bzw. auf den Pfeil.</summary>
        public event EventHandler Umschalten;

        /// <summary>Klick auf ✎ oder Doppelklick auf die Karte.</summary>
        public event EventHandler Bearbeiten;

        public SpeicherKarte()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);

            BackColor = Color.White;
            Margin = new Padding(0, 0, 0, 6);
            Cursor = Cursors.Hand;
            Height = 40;

            _tip.AutoPopDelay = 20000;
            _tip.InitialDelay = 400;
            _tip.ReshowDelay = 100;

            _lblPfeil.AutoSize = true;
            _lblPfeil.BackColor = Color.Transparent;
            _lblPfeil.ForeColor = KartenStil.TEXT_LEISE;
            _lblPfeil.Text = "▸";

            _lblName.AutoSize = false;
            _lblName.AutoEllipsis = true;
            _lblName.BackColor = Color.Transparent;
            _lblName.ForeColor = KartenStil.TEXT;
            _lblName.TextAlign = ContentAlignment.MiddleLeft;
            KartenStil.Schnitt(_lblName, FontStyle.Bold);

            _kopfChips.AutoSize = true;
            _kopfChips.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _kopfChips.WrapContents = true;
            _kopfChips.FlowDirection = FlowDirection.LeftToRight;
            _kopfChips.Margin = Padding.Empty;
            _kopfChips.Padding = Padding.Empty;
            _kopfChips.BackColor = Color.Transparent;
            _kopfChips.SizeChanged += delegate { HoeheNachfuehren(); };

            _lblBilanz.AutoSize = true;
            _lblBilanz.BackColor = Color.Transparent;
            _lblBilanz.ForeColor = KartenStil.TEXT_LEISE;

            _lnkEdit.Text = "✎";
            _lnkEdit.AutoSize = true;
            _lnkEdit.BackColor = Color.Transparent;
            _lnkEdit.ForeColor = KartenStil.TEXT_SEHR_LEISE;
            _lnkEdit.Cursor = Cursors.Hand;
            _lnkEdit.Click += delegate { Melden(Bearbeiten); };
            _lnkEdit.MouseEnter += delegate { _lnkEdit.ForeColor = KartenStil.QUELLE_TEXT; };
            _lnkEdit.MouseLeave += delegate { _lnkEdit.ForeColor = KartenStil.TEXT_SEHR_LEISE; };
            _tip.SetToolTip(_lnkEdit, MyResource.Resource.PSP_KARTE_TIP_BEARBEITEN);

            _detail.AutoSize = true;
            _detail.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _detail.WrapContents = false;
            _detail.FlowDirection = FlowDirection.TopDown;
            _detail.Margin = Padding.Empty;
            _detail.Padding = Padding.Empty;
            _detail.BackColor = Color.Transparent;
            _detail.Visible = false;
            _detail.SizeChanged += delegate { HoeheNachfuehren(); };

            _lblSchwellen.AutoSize = true;
            _lblSchwellen.BackColor = Color.Transparent;
            _lblSchwellen.ForeColor = KartenStil.TEXT_LEISE;
            _lblSchwellen.Margin = new Padding(0, 0, 0, 2);

            Controls.Add(_lblPfeil);
            Controls.Add(_lblName);
            Controls.Add(_kopfChips);
            Controls.Add(_lblBilanz);
            Controls.Add(_lnkEdit);
            Controls.Add(_detail);

            KlickDurchreichen(this);
        }

        /// <summary>
        /// Klick und Doppelklick auf JEDES Kind bedienen die Karte — außer auf ✎, das
        /// seinen eigenen Klick behält. Ohne den Durchgriff wäre nur der schmale freie
        /// Streifen zwischen den Beschriftungen anklickbar.
        /// </summary>
        private void KlickDurchreichen(Control c)
        {
            foreach (Control k in c.Controls)
            {
                if (ReferenceEquals(k, _lnkEdit)) continue;
                k.Click += ZeileGeklickt;
                k.DoubleClick += KarteDoppelklick;
                KlickDurchreichen(k);
            }

            if (ReferenceEquals(c, this))
            {
                c.Click += ZeileGeklickt;
                c.DoubleClick += KarteDoppelklick;
            }
        }

        private void ZeileGeklickt(object sender, EventArgs e)
        {
            if (Umschalten != null) Umschalten(this, EventArgs.Empty);
        }

        private void KarteDoppelklick(object sender, EventArgs e)
        {
            Melden(Bearbeiten);
        }

        /// <summary>
        /// Meldet ein Ereignis erst NACH der laufenden Nachricht — gleiche Begründung wie
        /// <c>ErzeugerKarte.Melden</c>: Der Empfänger von <see cref="Bearbeiten"/> öffnet
        /// die Puffer-Verwaltung und baut anschließend die Speicherspalte neu auf; dabei
        /// wird diese Karte entsorgt.
        ///
        /// <see cref="Umschalten"/> läuft bewusst NICHT hierüber: Das Auf- und Zuklappen
        /// entsorgt nichts und soll unmittelbar wirken.
        /// </summary>
        private void Melden(EventHandler ereignis)
        {
            if (ereignis == null) return;

            if (IsHandleCreated && !IsDisposed)
                BeginInvoke((MethodInvoker)delegate { ereignis(this, EventArgs.Empty); });
            else
                ereignis(this, EventArgs.Empty);
        }

        /// <summary>true = Detailkarte sichtbar (gepinnt).</summary>
        public bool Aufgeklappt
        {
            get { return _aufgeklappt; }
            set
            {
                if (_aufgeklappt == value) return;
                _aufgeklappt = value;
                _lblPfeil.Text = value ? "▾" : "▸";
                _detail.Visible = value;
                Neuordnen();
            }
        }

        /// <summary>Setzt den Inhalt der Karte aus den Projektdaten.</summary>
        public void Setzen(Daten d)
        {
            if (d == null) return;

            _aufbau = true;
            try
            {
                ID_Puffer = d.ID_Puffer;
                Tag = d;

                _lblName.Text = d.Bezeichner;
                _lblBilanz.Text = string.Format(MyResource.Resource.PSP_KARTE_BILANZ,
                                                d.LaderAnzahl, d.AbnehmerAnzahl);

                foreach (Control c in _kopfChips.Controls) c.Dispose();
                _kopfChips.Controls.Clear();

                if (!string.IsNullOrEmpty(d.Verwendung))
                {
                    KartenChip badge = new KartenChip();
                    badge.Text = d.Verwendung;
                    badge.OhneRand = true;
                    badge.BackColor = KartenStil.BADGE_FLAECHE;
                    badge.ForeColor = KartenStil.BADGE_TEXT;
                    _kopfChips.Controls.Add(badge);
                }

                FlaechenChip(d.Volumen);
                FlaechenChip(d.Temperaturpaar);

                foreach (Control c in _detail.Controls)
                {
                    // Band und Schwellentext überleben den Neuaufbau - sie sind Felder.
                    if (ReferenceEquals(c, _band) || ReferenceEquals(c, _lblSchwellen)) continue;
                    c.Dispose();
                }
                _detail.Controls.Clear();

                foreach (string zeile in d.Detailzeilen)
                {
                    if (string.IsNullOrEmpty(zeile)) continue;
                    Label l = new Label();
                    l.AutoSize = true;
                    l.BackColor = Color.Transparent;
                    l.ForeColor = KartenStil.TEXT_LEISE;
                    l.Margin = new Padding(0, 0, 0, 3);
                    l.Text = zeile;
                    _detail.Controls.Add(l);
                }

                _band.Setzen(d.SchwelleEin, d.SchwelleAusNachrang, d.SchwelleAus);
                _lblSchwellen.Text = d.Schwellentext;
                _detail.Controls.Add(_band);
                _detail.Controls.Add(_lblSchwellen);

                // Mouseover auf der zugeklappten Zeile zeigt dieselben Details (Konzept 3a).
                string hinweis = string.Join(Environment.NewLine, d.Detailzeilen.ToArray());
                if (!string.IsNullOrEmpty(d.Schwellentext))
                    hinweis = hinweis.Length > 0
                        ? hinweis + Environment.NewLine + d.Schwellentext
                        : d.Schwellentext;
                HinweisSetzen(this, hinweis);
            }
            finally { _aufbau = false; }

            KlickDurchreichen(this);
            Neuordnen();
        }

        private void FlaechenChip(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            KartenChip chip = new KartenChip();
            chip.Text = text;
            chip.OhneRand = true;
            chip.BackColor = KartenStil.FLAECHE;
            _kopfChips.Controls.Add(chip);
        }

        private void HinweisSetzen(Control c, string text)
        {
            foreach (Control k in c.Controls)
            {
                if (ReferenceEquals(k, _lnkEdit) || ReferenceEquals(k, _detail)) continue;
                _tip.SetToolTip(k, text);
                HinweisSetzen(k, text);
            }
            if (ReferenceEquals(c, this)) _tip.SetToolTip(c, text);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (!_aufbau) Neuordnen();
        }

        private void Neuordnen()
        {
            if (_aufbau) return;

            int y = KartenStil.RAND - 2;
            _lblPfeil.Location = new Point(KartenStil.RAND - 4, y);

            int rechts = ClientSize.Width - KartenStil.RAND;
            _lnkEdit.Location = new Point(rechts - _lnkEdit.Width, y);
            rechts -= _lnkEdit.Width + 8;

            _lblBilanz.Location = new Point(Math.Max(0, rechts - _lblBilanz.Width), y + 1);
            rechts -= _lblBilanz.Width + 8;

            int links = _lblPfeil.Right + 4;
            int namensBreite = Math.Max(60, rechts - links);

            // Der Name bekommt so viel, wie er braucht, aber höchstens die Hälfte der
            // freien Breite - sonst quetschen lange Herstellerbezeichner die Chips weg.
            int gewuenscht = TextRenderer.MeasureText(_lblName.Text, _lblName.Font).Width + 4;
            int nameBreite = Math.Min(gewuenscht, Math.Max(60, namensBreite / 2));
            if (gewuenscht <= namensBreite / 2) nameBreite = gewuenscht;

            _lblName.Bounds = new Rectangle(links, y, nameBreite, _lblPfeil.Height);

            _kopfChips.Location = new Point(_lblName.Right + 6, y - 2);
            _kopfChips.Width = Math.Max(40, rechts - _lblName.Right - 6);

            int kopfUnten = Math.Max(_kopfChips.Bottom, _lblName.Bottom);

            // Innenbreite aus der KARTE, nicht aus _detail: Der Detailbereich hat
            // AutoSize mit GrowAndShrink und schrumpft auf seine Inhaltsbreite zusammen
            // (gemessen: 177 px in einer 374 px breiten Karte). Wer die Zeilenbreite
            // daraus ableitet, umbricht die Texte viel zu früh und macht das
            // Schwellenband schmaler als die Karte.
            int innen = Math.Max(60, ClientSize.Width - 2 * KartenStil.RAND);

            _detail.Location = new Point(KartenStil.RAND, kopfUnten + 6);
            foreach (Control c in _detail.Controls)
            {
                if (c is SchwellenBand) { c.Width = innen; continue; }
                c.MaximumSize = new Size(innen, 0);
            }

            HoeheNachfuehren();
        }

        /// <summary>
        /// Höhe der Karte an ihren Inhalt anpassen.
        ///
        /// Maßgeblich ist <see cref="_aufgeklappt"/> und NICHT <c>_detail.Visible</c>:
        /// Der Visible-GETTER eines Steuerelements liefert den WIRKSAMEN Zustand und
        /// damit <c>false</c>, solange ein übergeordnetes Fenster noch nicht angezeigt
        /// wird. Die Karten entstehen aber im Konstruktor bzw. in <c>SetControls</c> —
        /// also vor dem ersten Anzeigen. Mit dem Getter bekäme eine aufgeklappte Karte
        /// dort die zugeklappte Höhe und richtete sich erst bei der nächsten
        /// Größenänderung. Im Harness aufgeschlagen (Höhe 46 px trotz sechs
        /// Detailzeilen).
        /// </summary>
        private void HoeheNachfuehren()
        {
            int kopfUnten = Math.Max(_kopfChips.Bottom, _lblName.Bottom);
            int noetig = _aufgeklappt
                ? _detail.Top + _detail.Height + KartenStil.RAND
                : kopfUnten + KartenStil.RAND - 2;

            if (Height != noetig) Height = noetig;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent != null ? Parent.BackColor : SystemColors.Control);

            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath p = KartenStil.Rundeck(r, KartenStil.ECKE))
            {
                using (SolidBrush b = new SolidBrush(BackColor)) e.Graphics.FillPath(b, p);
                using (Pen stift = new Pen(_aufgeklappt ? KartenStil.RAHMEN_SPEICHER : KartenStil.RAHMEN))
                    e.Graphics.DrawPath(stift, p);
            }

            base.OnPaint(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _tip.Dispose();
            base.Dispose(disposing);
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine anklickbare Einstiegskarte des Reiters „Kostenprofil" (Konzept
    /// Kosten/Energieträger HF4, Etappe K4): Titelzeile, Beschreibung und eine
    /// Statuszeile, die den gepflegten Bestand zeigt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum eine Karte und kein Knopf.</b> Beide Einstiege führen in einen
    /// eigenen Dialog und tragen einen Zustand, der vor dem Klick interessiert
    /// („ist schon ein Profil da, und wie sieht es aus?"). Ein Knopf kann nur
    /// beschriftet werden; die Karte zeigt den Bestand gleich mit und erspart das
    /// Öffnen zum Nachsehen. Dieselbe Überlegung steht hinter den KonfigUI-Karten
    /// (<see cref="ErzeugerKarte"/>/<see cref="SpeicherKarte"/>).
    /// </para>
    /// <para>
    /// Bewusst NICHT von <c>SectionPanel</c> abgeleitet: Das ist eine
    /// <c>Dock=Top</c>-Abschnittsüberschrift mit dunklem Balken ohne Klick- und
    /// Hover-Verhalten — ein anderer Baustein. Es bleibt unberührt (und ist
    /// cp1252-kodiert, siehe Protokoll K4).
    /// </para>
    /// </remarks>
    internal sealed class EinstiegsKarte : Panel
    {
        private readonly Label _lblTitel;
        private readonly Label _lblInfo;
        private readonly Label _lblStatus;

        private static readonly Color RAHMEN = Color.FromArgb(209, 213, 219);
        private static readonly Color RAHMEN_HOVER = Color.FromArgb(59, 130, 246);
        private static readonly Color FLAECHE = Color.White;
        private static readonly Color FLAECHE_HOVER = Color.FromArgb(239, 246, 255);
        private static readonly Color TITEL = Color.FromArgb(15, 31, 61);
        private static readonly Color INFO = Color.FromArgb(90, 98, 112);
        private static readonly Color STATUS = Color.FromArgb(26, 50, 97);

        private bool _hover;

        /// <summary>Wird ausgelöst, wenn die Karte (oder einer ihrer Texte) angeklickt wird.</summary>
        public event EventHandler Geklickt;

        public EinstiegsKarte()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            BackColor = FLAECHE;
            Cursor = Cursors.Hand;
            Padding = new Padding(16, 14, 16, 14);

            _lblTitel = new Label
            {
                AutoSize = false,
                Location = new Point(16, 14),
                Size = new Size(10, 26),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = TITEL,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblInfo = new Label
            {
                AutoSize = false,
                Location = new Point(16, 44),
                Size = new Size(10, 44),
                Font = new Font("Segoe UI", 9.75F),
                ForeColor = INFO,
                BackColor = Color.Transparent
            };

            _lblStatus = new Label
            {
                AutoSize = false,
                Location = new Point(16, 96),
                Size = new Size(10, 40),
                Font = new Font("Segoe UI", 9.75F, FontStyle.Bold),
                ForeColor = STATUS,
                BackColor = Color.Transparent
            };

            Controls.Add(_lblTitel);
            Controls.Add(_lblInfo);
            Controls.Add(_lblStatus);

            // Klick und Hover müssen auf der ganzen Fläche wirken — die Beschriftungen
            // fangen die Mausereignisse sonst ab und die Karte flackerte beim Überfahren.
            HaengeEreignisseEin(this);
            foreach (Control c in Controls) HaengeEreignisseEin(c);
        }

        private void HaengeEreignisseEin(Control c)
        {
            c.Click += (s, e) => { EventHandler h = Geklickt; if (h != null) h(this, EventArgs.Empty); };
            c.MouseEnter += (s, e) => SetzeHover(true);
            c.MouseLeave += (s, e) => SetzeHover(ClientRectangle.Contains(PointToClient(MousePosition)));
            if (!ReferenceEquals(c, this)) c.Cursor = Cursors.Hand;
        }

        private void SetzeHover(bool an)
        {
            if (_hover == an) return;
            _hover = an;
            BackColor = an ? FLAECHE_HOVER : FLAECHE;
            Invalidate();
        }

        /// <summary>Überschrift der Karte (fett).</summary>
        public string Titel
        {
            get { return _lblTitel.Text; }
            set { _lblTitel.Text = value ?? ""; }
        }

        /// <summary>Beschreibungstext unter der Überschrift.</summary>
        public string Beschreibung
        {
            get { return _lblInfo.Text; }
            set { _lblInfo.Text = value ?? ""; }
        }

        /// <summary>Kennwertzeile: der gepflegte Bestand, „—" wenn nicht lesbar.</summary>
        public string Status
        {
            get { return _lblStatus.Text; }
            set { _lblStatus.Text = value ?? ""; }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            int breite = Math.Max(10, Width - 32);
            _lblTitel.Width = breite;
            _lblInfo.Width = breite;
            _lblStatus.Width = breite;
            _lblStatus.Top = Math.Max(_lblInfo.Bottom + 6, Height - 14 - _lblStatus.Height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);

            using (GraphicsPath p = Rundeck(r, 6))
            using (SolidBrush b = new SolidBrush(BackColor))
            using (Pen s = new Pen(_hover ? RAHMEN_HOVER : RAHMEN, _hover ? 2f : 1f))
            {
                e.Graphics.FillPath(b, p);
                e.Graphics.DrawPath(s, p);
            }
        }

        /// <summary>Rechteck mit abgerundeten Ecken (wie <c>KartenStil.Rundeck</c>).</summary>
        private static GraphicsPath Rundeck(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            if (radius <= 0 || r.Width <= 2 * radius || r.Height <= 2 * radius)
            {
                p.AddRectangle(r);
                return p;
            }

            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}

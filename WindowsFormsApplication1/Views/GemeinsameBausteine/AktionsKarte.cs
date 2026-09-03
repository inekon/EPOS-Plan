using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine anklickbare Aktionskarte: Bild (optional), Überschrift, Beschreibung,
    /// optionaler Statuspunkt, Hover-Zustand und ein Ereignis <see cref="Geklickt"/>.
    ///
    /// <para>
    /// <b>Woher.</b> Weiterentwicklung der <c>EinstiegsKarte</c>
    /// (Views\Kosten) — dort ein Laufzeit-<c>Panel</c>, hier ein im
    /// Visual-Studio-Designer platzierbares <see cref="UserControl"/> mit
    /// <c>.Designer.cs</c> und <c>.resx</c> (Kern-Vorgabe des Konzepts
    /// „Projektdialoge vereinheitlichen": der Designer bleibt das Pflegewerkzeug).
    /// Sie löst auf dem Startmasken-Reiter „Projekt" das Trio aus
    /// <c>PictureBox</c> mit JPG-Hintergrund und zwei absolut platzierten
    /// <c>Label</c>n ab.
    /// </para>
    /// <para>
    /// <b>Farben, Radien, Abstände</b> stammen ausschließlich aus
    /// <see cref="KartenStil"/> (Allgemein\GrafikTools). Sie werden im Konstruktor
    /// gesetzt, nicht in <c>InitializeComponent</c>: Der Designer serialisiert
    /// Farben als <c>Color.FromArgb</c>-Literale zurück und würde die Token damit
    /// wieder auseinanderlaufen lassen. Geometrie und Schrift stehen dagegen im
    /// Designer, damit die Karte dort auch aussieht wie zur Laufzeit.
    /// </para>
    /// <para>
    /// <b>Klick auf jedes Kind.</b> Bild und Beschriftungen fangen die
    /// Mausereignisse sonst ab — die Karte reagierte nur auf ihren Rand und
    /// flackerte beim Überfahren. Deshalb hängen Klick und Hover an der Karte UND
    /// an jedem Kind; nachträglich eingefügte Kinder werden über
    /// <see cref="OnControlAdded"/> mitgenommen.
    /// </para>
    /// </summary>
    [ToolboxItem(true)]
    [Description("Anklickbare Karte mit Bild, Titel, Beschreibung und optionalem Statuspunkt.")]
    [DefaultEvent("Geklickt")]
    [DefaultProperty("Titel")]
    public partial class AktionsKarte : UserControl
    {
        private bool _hover;
        private bool _statusSichtbar;
        private Color _statusFarbe = KartenStil.KARTE_STATUS;

        /// <summary>Wird ausgelöst, wenn die Karte oder eines ihrer Kinder angeklickt wird.</summary>
        [Category("Aktion")]
        [Description("Wird ausgelöst, wenn die Karte oder eines ihrer Kinder angeklickt wird.")]
        public event EventHandler Geklickt;

        public AktionsKarte()
        {
            InitializeComponent();

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            BackColor = KartenStil.KARTE_FLAECHE;
            label_Titel.ForeColor = KartenStil.KARTE_TITEL;
            label_Beschreibung.ForeColor = KartenStil.KARTE_TEXT;

            // NUR die Karte selbst: die drei Kinder aus InitializeComponent haben ihre
            // Ereignisse bereits ueber OnControlAdded bekommen (Controls.Add loest das
            // aus). Eine zusaetzliche Schleife ueber Controls haenge alles ein ZWEITES
            // Mal ein - Geklickt feuerte dann bei jedem Kachelklick doppelt.
            HaengeEreignisseEin(this);
        }

        // ------------------------------------------------------------------
        //  Eigenschaften (im Eigenschaftenfenster des Designers pflegbar)
        // ------------------------------------------------------------------

        /// <summary>Bild im Kopf der Karte; <c>null</c> = Karte ohne Bild.</summary>
        [Category("Darstellung")]
        [Description("Bild im Kopf der Karte. Ohne Bild rücken Titel und Beschreibung nach oben.")]
        [DefaultValue(null)]
        public Image KartenBild
        {
            get { return pictureBox_Bild.Image; }
            set
            {
                pictureBox_Bild.Image = value;
                pictureBox_Bild.Visible = value != null;
                NeuAnordnen();
                Invalidate();
            }
        }

        /// <summary>Überschrift der Karte.</summary>
        [Category("Darstellung")]
        [Description("Überschrift der Karte.")]
        [DefaultValue("")]
        [Localizable(true)]
        public string Titel
        {
            get { return label_Titel.Text; }
            set { label_Titel.Text = value ?? ""; NeuAnordnen(); }
        }

        /// <summary>Beschreibungstext unter der Überschrift.</summary>
        [Category("Darstellung")]
        [Description("Beschreibungstext unter der Überschrift.")]
        [DefaultValue("")]
        [Localizable(true)]
        public string Beschreibung
        {
            get { return label_Beschreibung.Text; }
            set { label_Beschreibung.Text = value ?? ""; NeuAnordnen(); }
        }

        // Vorgabeschrift der Überschrift - dieselben Werte, die InitializeComponent
        // setzt. Sie steht hier zusaetzlich als Feld, damit ShouldSerialize/Reset dem
        // Designer melden koennen, ob eine Karte von der Vorgabe abweicht.
        private static readonly Font TITEL_VORGABE =
            new Font("Segoe UI Semibold", 13F, FontStyle.Bold);

        /// <summary>
        /// Schrift der Überschrift. Vorgabe ist „Segoe UI Semibold 13 pt fett" — die
        /// Optik der grossen Startmasken-Kacheln. Kleine Kachelfelder (etwa die dreizehn
        /// Komponentenkacheln des Projektassistenten) setzen hier eine kleinere Schrift,
        /// damit Überschrift und Beschreibung in eine niedrige Karte passen.
        /// </summary>
        [Category("Darstellung")]
        [Description("Schrift der Überschrift.")]
        public Font TitelSchrift
        {
            get { return label_Titel.Font; }
            set
            {
                label_Titel.Font = value ?? TITEL_VORGABE;
                NeuAnordnen();
                Invalidate();
            }
        }

        private bool ShouldSerializeTitelSchrift()
        {
            return !TITEL_VORGABE.Equals(label_Titel.Font);
        }

        private void ResetTitelSchrift()
        {
            TitelSchrift = TITEL_VORGABE;
        }

        /// <summary>true = der Statuspunkt oben rechts wird gezeichnet.</summary>
        [Category("Darstellung")]
        [Description("Zeigt oben rechts einen Statuspunkt in der Farbe StatusFarbe.")]
        [DefaultValue(false)]
        public bool StatusSichtbar
        {
            get { return _statusSichtbar; }
            set
            {
                if (_statusSichtbar == value) return;
                _statusSichtbar = value;
                Invalidate();
            }
        }

        /// <summary>Farbe des Statuspunkts.</summary>
        [Category("Darstellung")]
        [Description("Farbe des Statuspunkts oben rechts.")]
        [DefaultValue(typeof(Color), "90, 0, 255, 0")]
        public Color StatusFarbe
        {
            get { return _statusFarbe; }
            set
            {
                if (_statusFarbe == value) return;
                _statusFarbe = value;
                if (_statusSichtbar) Invalidate();
            }
        }

        // ------------------------------------------------------------------
        //  Klick und Hover
        // ------------------------------------------------------------------

        private void HaengeEreignisseEin(Control c)
        {
            c.Click += KindGeklickt;
            c.MouseEnter += KindMausRein;
            c.MouseLeave += KindMausRaus;
            if (!ReferenceEquals(c, this)) c.Cursor = Cursors.Hand;
        }

        private void KindGeklickt(object sender, EventArgs e)
        {
            EventHandler h = Geklickt;
            if (h != null) h(this, EventArgs.Empty);
        }

        private void KindMausRein(object sender, EventArgs e)
        {
            SetzeHover(true);
        }

        private void KindMausRaus(object sender, EventArgs e)
        {
            SetzeHover(ClientRectangle.Contains(PointToClient(MousePosition)));
        }

        private void SetzeHover(bool an)
        {
            if (_hover == an) return;
            _hover = an;
            BackColor = an ? KartenStil.KARTE_FLAECHE_HOVER : KartenStil.KARTE_FLAECHE;
            Invalidate();
        }

        /// <summary>Auch Kinder, die erst später dazukommen, lösen Klick und Hover aus.</summary>
        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            if (e.Control != null) HaengeEreignisseEin(e.Control);
        }

        // ------------------------------------------------------------------
        //  Anordnung und Anstrich
        // ------------------------------------------------------------------

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            NeuAnordnen();
        }

        /// <summary>
        /// Ordnet Bild, Überschrift und Beschreibung als senkrecht mittig stehenden
        /// Block an. Die Karte ist im Designer frei skalierbar; die feste Geometrie
        /// aus <c>InitializeComponent</c> gilt nur für die Standardgröße 404×185.
        /// </summary>
        private void NeuAnordnen()
        {
            int rand = KartenStil.KARTE_RAND;
            int breite = Math.Max(10, Width - 2 * rand);

            // WICHTIG: nach Image fragen, nie nach Visible — der Visible-Getter
            // liefert false, solange die Elternkette unsichtbar ist (also während
            // des gesamten InitializeComponent). Genau das ließ die Anordnung im
            // „ohne Bild“-Zweig laufen, obwohl ein Logo gesetzt war.
            bool mitBild = pictureBox_Bild.Image != null;

            int titelHoehe = label_Titel.Font.Height + 6;

            if (mitBild)
            {
                // Anordnung wie die früheren Kacheln: Logo links oben, Überschrift
                // rechts daneben auf Logo-Mitte, Beschreibung darunter über die
                // volle Breite.
                int seite = Math.Max(24, Math.Min(64, Height / 3));
                int bildX = rand + 24;
                int bildY = rand + 24;
                pictureBox_Bild.Bounds = new Rectangle(bildX, bildY, seite, seite);

                int titelX = bildX + seite + 16;
                int titelBreite = Math.Max(10, Width - titelX - rand);
                int titelY = bildY + Math.Max(0, (seite - titelHoehe) / 2);
                label_Titel.Bounds = new Rectangle(titelX, titelY, titelBreite, titelHoehe);

                int oben = pictureBox_Bild.Bottom + 14;
                int beschrHoehe = Math.Max(18, Height - oben - rand);
                label_Beschreibung.Bounds = new Rectangle(rand, oben, breite, beschrHoehe);
                return;
            }

            // Ohne Bild: Überschrift und Beschreibung als senkrecht mittiger Block.
            int gemessen = TextRenderer.MeasureText(label_Beschreibung.Text ?? "", label_Beschreibung.Font,
                                                    new Size(breite, 0), TextFormatFlags.WordBreak).Height;
            int platz = Math.Max(18, Height - 2 * rand - titelHoehe - 6);
            int beschrHoeheOhne = Math.Max(18, Math.Min(gemessen + 2, platz));

            int blockHoehe = titelHoehe + 6 + beschrHoeheOhne;
            int start = Math.Max(rand, (Height - blockHoehe) / 2);

            label_Titel.Bounds = new Rectangle(rand, start, breite, titelHoehe);
            label_Beschreibung.Bounds = new Rectangle(rand, label_Titel.Bottom + 6, breite, beschrHoeheOhne);
        }

        /// <summary>Schriftwechsel (auch durch AutoScale nach InitializeComponent) ordnet neu an.</summary>
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            NeuAnordnen();
        }

        /// <summary>Sicherheitsanker: spätestens mit dem Handle stimmt die Anordnung.</summary>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NeuAnordnen();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);

            using (GraphicsPath p = KartenStil.Rundeck(r, KartenStil.ECKE))
            using (SolidBrush b = new SolidBrush(BackColor))
            using (Pen s = new Pen(_hover ? KartenStil.KARTE_RAHMEN_HOVER : KartenStil.KARTE_RAHMEN, _hover ? 2f : 1f))
            {
                e.Graphics.FillPath(b, p);
                e.Graphics.DrawPath(s, p);
            }

            if (!_statusSichtbar) return;

            int d = KartenStil.KARTE_STATUSPUNKT;
            Rectangle punkt = new Rectangle(Width - KartenStil.KARTE_RAND - d, KartenStil.KARTE_RAND, d, d);
            using (SolidBrush b = new SolidBrush(_statusFarbe))
            {
                e.Graphics.FillEllipse(b, punkt);
            }
        }
    }
}

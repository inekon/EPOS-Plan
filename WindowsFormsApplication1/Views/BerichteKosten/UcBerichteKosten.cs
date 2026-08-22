using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Inhalt des Haupt-Reiters „Berichte &amp; Kosten".
    ///
    /// Links eine senkrechte Navigation im Stil der Detaillierten Simulation
    /// (dunkles Menü, eigengezeichnete Zeilen mit Vektor-Icon — Muster
    /// <c>Form_Simulation_Detail.listViewQuellen</c>), rechts die gewählte Seite:
    ///
    /// <list type="number">
    ///   <item><b>Übersicht</b> — Stammprojekt, Varianten, Komponenten/Unterschiede
    ///         (<see cref="UcBkUebersicht"/>)</item>
    ///   <item><b>Kosten</b> — Investition/Betrieb/Energie kompakt
    ///         (<see cref="UcBkKosten"/>)</item>
    ///   <item><b>Wirtschaftlichkeit</b> — eingebettete Kapitalwert-Vergleichsansicht
    ///         (<see cref="UcWirtschaftlichkeit"/>)</item>
    ///   <item><b>Bericht</b> — eingebettete Berichtserstellung
    ///         (<see cref="UcBericht"/>)</item>
    /// </list>
    ///
    /// Die Stammprojekt-Wahl der Übersicht gilt für alle Seiten: Wirtschaftlichkeit
    /// und Bericht arbeiten auf derselben Vergleichsgruppe und werden beim Wechsel
    /// des Stammprojekts verworfen und beim nächsten Aufruf neu aufgebaut; die
    /// Kostenseite folgt der in der Liste markierten Zeile (Stamm ODER Variante).
    ///
    /// Die schweren Seiten werden erst beim ersten Aufruf erzeugt — der Reiter
    /// öffnet sich damit ohne Simulations- oder Berichtsdatenzugriff.
    /// </summary>
    public class UcBerichteKosten : UserControl
    {
        // Seitenschlüssel — Schicht 2 der Drei-Schichten-Regel: sprachneutral, ASCII.
        public const string SEITE_UEBERSICHT = "UEBERSICHT";
        public const string SEITE_KOSTEN = "KOSTEN";
        public const string SEITE_WIRTSCHAFT = "WIRTSCHAFT";
        public const string SEITE_BERICHT = "BERICHT";

        // Farbpalette der senkrechten Navigation (identisch zur Detailsimulation).
        private static readonly Color cMenuBase = Color.FromArgb(0x23, 0x28, 0x2d);
        private static readonly Color cMenuText = Color.FromArgb(0xee, 0xee, 0xee);
        private static readonly Color cMenuIcon = Color.FromArgb(0xa7, 0xaa, 0xad);
        private static readonly Color cMenuHoverBg = Color.FromArgb(0x19, 0x1e, 0x23);
        private static readonly Color cMenuHoverFg = Color.FromArgb(0x00, 0xb9, 0xeb);
        private static readonly Color cMenuSelBg = Color.FromArgb(0x00, 0x73, 0xaa);
        private static readonly Color cMenuSelFg = Color.White;

        private const int NAV_BREITE = 208;
        private const int ZEILE_HOEHE = 40;

        private ListView lvNav;
        private ImageList _zeilenHoehe;
        private int _hoverIndex = -1;

        private Panel pnlInhalt;
        private Label lblKopf;

        // Seiten (lazy)
        private UcBkUebersicht _uebersicht;
        private UcBkKosten _kosten;
        private UcWirtschaftlichkeit _wirtschaft;
        private UcBericht _bericht;
        private Control _aktiveSeite;

        // Gemeinsamer Zustand aller Seiten
        private int _idStamm = -1;
        private string _stammName = "";
        private int _idMarkiert = -1;
        private string _nameMarkiert = "";

        public UcBerichteKosten()
        {
            InitializeComponent();
        }

        // ------------------------------------------------------------- Aufbau

        private void InitializeComponent()
        {
            this.lvNav = new ListView();
            this.pnlInhalt = new Panel();
            this.lblKopf = new Label();
            this.SuspendLayout();

            // --- senkrechte Navigation ---
            this.lvNav.Dock = DockStyle.Left;
            this.lvNav.Width = NAV_BREITE;
            this.lvNav.View = View.Details;
            this.lvNav.FullRowSelect = true;
            this.lvNav.HeaderStyle = ColumnHeaderStyle.None;
            this.lvNav.MultiSelect = false;
            this.lvNav.HideSelection = false;
            this.lvNav.BorderStyle = BorderStyle.None;
            this.lvNav.BackColor = cMenuBase;
            this.lvNav.ForeColor = cMenuText;
            this.lvNav.Font = new Font("Segoe UI", 11f, FontStyle.Regular);
            this.lvNav.Columns.Add("", NAV_BREITE);

            // Zeilenhöhe über eine (leere) SmallImageList erzwingen.
            this._zeilenHoehe = new ImageList();
            this._zeilenHoehe.ImageSize = new Size(1, ZEILE_HOEHE);
            this._zeilenHoehe.ColorDepth = ColorDepth.Depth32Bit;
            this.lvNav.SmallImageList = this._zeilenHoehe;

            this.lvNav.OwnerDraw = true;
            this.lvNav.DrawColumnHeader += (s, e) => { /* Kopfzeile ist ausgeblendet */ };
            this.lvNav.DrawSubItem += (s, e) => { /* ganze Zeile wird in DrawItem gezeichnet */ };
            this.lvNav.DrawItem += new DrawListViewItemEventHandler(this.lvNav_DrawItem);
            this.lvNav.MouseMove += new MouseEventHandler(this.lvNav_MouseMove);
            this.lvNav.MouseLeave += new EventHandler(this.lvNav_MouseLeave);
            this.lvNav.SelectedIndexChanged += new EventHandler(this.lvNav_SelectedIndexChanged);

            // Flimmern beim Überfahren dämpfen.
            try
            {
                typeof(ListView).GetProperty("DoubleBuffered",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(this.lvNav, true, null);
            }
            catch { /* unkritisch */ }

            FuelleNavigation();

            // --- Kopfzeile der Seite ---
            this.lblKopf.Dock = DockStyle.Top;
            this.lblKopf.Height = 30;
            this.lblKopf.Padding = new Padding(10, 6, 6, 0);
            this.lblKopf.Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);
            this.lblKopf.ForeColor = Color.FromArgb(0x1F, 0x4E, 0x79);

            // --- Inhaltsfläche ---
            this.pnlInhalt.Dock = DockStyle.Fill;
            this.pnlInhalt.AutoScroll = true;    // Rückfallebene auf sehr kleinen Flächen

            this.Controls.Add(this.pnlInhalt);
            this.Controls.Add(this.lblKopf);
            this.Controls.Add(this.lvNav);

            this.Font = new Font("Segoe UI", 9f);
            this.Name = "UcBerichteKosten";
            this.Size = new Size(1265, 560);
            this.ResumeLayout(false);
        }

        private void FuelleNavigation()
        {
            lvNav.Items.Clear();
            lvNav.Items.Add(new ListViewItem(MyResource.Resource.BK_NAV_UEBERSICHT) { Tag = SEITE_UEBERSICHT });
            lvNav.Items.Add(new ListViewItem(MyResource.Resource.BK_NAV_KOSTEN) { Tag = SEITE_KOSTEN });
            lvNav.Items.Add(new ListViewItem(MyResource.Resource.BK_NAV_WIRTSCHAFT) { Tag = SEITE_WIRTSCHAFT });
            lvNav.Items.Add(new ListViewItem(MyResource.Resource.BK_NAV_BERICHT) { Tag = SEITE_BERICHT });
            if (lvNav.Columns.Count > 0) lvNav.Columns[0].Width = NAV_BREITE;
        }

        // ------------------------------------------------------------- Öffentlich

        /// <summary>
        /// Setzt den Projektkontext (das in Form_Start geöffnete Projekt). Beim ersten
        /// Aufruf öffnet der Reiter die Seite „Übersicht"; bei jedem weiteren bleibt die
        /// zuletzt gewählte Seite stehen — sie wird nur neu aufgebaut, weil ein
        /// Stammwechsel Wirtschaftlichkeit und Bericht verwirft.
        /// </summary>
        public void SetzeProjekt(int idProjekt)
        {
            string vorher = AktiveSeite;
            bool erstesMal = string.IsNullOrEmpty(vorher);
            if (erstesMal) ZeigeSeite(SEITE_UEBERSICHT);

            Uebersicht.SetzeAktuellesProjekt(idProjekt);

            if (!erstesMal) ZeigeSeite(vorher);
        }

        /// <summary>Schlüssel der aktuell sichtbaren Seite.</summary>
        public string AktiveSeite
        {
            get
            {
                return lvNav.SelectedItems.Count > 0
                    ? lvNav.SelectedItems[0].Tag as string : null;
            }
        }

        /// <summary>Übersichtsseite (wird bei Bedarf erzeugt).</summary>
        public UcBkUebersicht Uebersicht
        {
            get
            {
                if (_uebersicht == null)
                {
                    _uebersicht = new UcBkUebersicht { Dock = DockStyle.Fill };
                    _uebersicht.StammGewechselt += Uebersicht_StammGewechselt;
                    _uebersicht.ProjektMarkiert += Uebersicht_ProjektMarkiert;
                }
                return _uebersicht;
            }
        }

        /// <summary>Kostenseite (wird bei Bedarf erzeugt).</summary>
        public UcBkKosten Kosten
        {
            get
            {
                if (_kosten == null) _kosten = new UcBkKosten { Dock = DockStyle.Fill };
                return _kosten;
            }
        }

        /// <summary>
        /// Wirtschaftlichkeitsseite der aktuellen Vergleichsgruppe (wird bei Bedarf
        /// erzeugt; nach einem Stammwechsel neu aufgebaut). null, solange kein
        /// Stammprojekt gewählt ist.
        /// </summary>
        public UcWirtschaftlichkeit Wirtschaftlichkeit
        {
            get
            {
                if (_idStamm <= 0) return null;
                if (_wirtschaft == null)
                    _wirtschaft = new UcWirtschaftlichkeit(_idStamm) { Dock = DockStyle.Fill };
                return _wirtschaft;
            }
        }

        /// <summary>
        /// Berichtsseite der aktuellen Vergleichsgruppe (wird bei Bedarf erzeugt;
        /// nach einem Stammwechsel neu aufgebaut). null ohne Stammprojekt.
        /// </summary>
        public UcBericht Bericht
        {
            get
            {
                if (_idStamm <= 0) return null;
                if (_bericht == null)
                    _bericht = new UcBericht(_idStamm, _stammName) { Dock = DockStyle.Fill };
                return _bericht;
            }
        }

        /// <summary>Stellt die Seite mit dem angegebenen Schlüssel ein.</summary>
        public void ZeigeSeite(string schluessel)
        {
            foreach (ListViewItem it in lvNav.Items)
                if ((it.Tag as string) == schluessel)
                {
                    if (!it.Selected) { it.Selected = true; it.Focused = true; }
                    else BaueSeiteAuf(schluessel);   // erneut anzeigen (z. B. nach Verwerfen)
                    return;
                }
        }

        // ------------------------------------------------------------- Navigation

        private void lvNav_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvNav.SelectedItems.Count == 0) return;
            BaueSeiteAuf(lvNav.SelectedItems[0].Tag as string);
        }

        private void BaueSeiteAuf(string schluessel)
        {
            if (this.DesignMode) return;

            Control neu = null;

            switch (schluessel)
            {
                case SEITE_UEBERSICHT:
                    neu = Uebersicht;
                    break;

                case SEITE_KOSTEN:
                    SichereMarkierung();
                    Kosten.SetzeProjekt(_idMarkiert, _nameMarkiert);
                    neu = Kosten;
                    break;

                case SEITE_WIRTSCHAFT:
                    neu = Wirtschaftlichkeit;
                    break;

                case SEITE_BERICHT:
                    neu = Bericht;
                    break;
            }

            SetzeKopfzeile();

            if (neu == null)
            {
                // Ohne Stammprojekt gibt es keine Vergleichsgruppe — Hinweis statt Seite.
                ZeigeHinweis(MyResource.Resource.BK_MSG_KEIN_STAMM);
                return;
            }

            if (ReferenceEquals(neu, _aktiveSeite)) return;

            pnlInhalt.SuspendLayout();
            try
            {
                pnlInhalt.Controls.Clear();
                pnlInhalt.Controls.Add(neu);
                _aktiveSeite = neu;
            }
            finally { pnlInhalt.ResumeLayout(true); }

            // Erstbefüllung der eingebetteten Bestandsseiten anstoßen (in der
            // Anwendung erledigt das sonst OnCreateControl; hier deterministisch).
            UcWirtschaftlichkeit uw = neu as UcWirtschaftlichkeit;
            if (uw != null) uw.LadeDaten();
            UcBericht ub = neu as UcBericht;
            if (ub != null) ub.LadeDatenEinmalig();
        }

        private void ZeigeHinweis(string text)
        {
            var lbl = new Label
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ForeColor = Color.Firebrick,
                Text = text
            };
            pnlInhalt.Controls.Clear();
            pnlInhalt.Controls.Add(lbl);
            _aktiveSeite = lbl;
        }

        // ------------------------------------------------------------- Zustand

        private void Uebersicht_StammGewechselt(int idStamm, string name)
        {
            if (idStamm == _idStamm) { _stammName = name ?? ""; return; }

            _idStamm = idStamm;
            _stammName = name ?? "";
            SetzeKopfzeile();

            // Wirtschaftlichkeit und Bericht hängen fest an ihrer Vergleichsgruppe:
            // beim Stammwechsel verwerfen, damit sie beim nächsten Aufruf frisch
            // mit dem neuen Stamm entstehen.
            VerwirfGruppenSeiten();
        }

        // Kopfzeile der gerade sichtbaren Seite mit dem aktuellen Stammnamen nachziehen.
        private void SetzeKopfzeile()
        {
            string kopf = KopfText(AktiveSeite);
            lblKopf.Text = _idStamm > 0 && !string.IsNullOrEmpty(kopf)
                ? kopf + "  ·  " + _stammName
                : kopf;
        }

        private static string KopfText(string schluessel)
        {
            switch (schluessel)
            {
                case SEITE_UEBERSICHT: return MyResource.Resource.BK_KOPF_UEBERSICHT;
                case SEITE_KOSTEN: return MyResource.Resource.BK_KOPF_KOSTEN;
                case SEITE_WIRTSCHAFT: return MyResource.Resource.BK_KOPF_WIRTSCHAFT;
                case SEITE_BERICHT: return MyResource.Resource.BK_KOPF_BERICHT;
                default: return "";
            }
        }

        private void Uebersicht_ProjektMarkiert(int idProjekt, bool istStamm)
        {
            _idMarkiert = idProjekt;
            UcBkUebersicht.AuswahlZeile z = Uebersicht.AktuelleZeile;
            _nameMarkiert = z != null ? z.Projektname : "";

            // Nur nachziehen, wenn die Kostenseite gerade sichtbar ist — sonst holt
            // sie sich den Stand beim nächsten Aufruf (BaueSeiteAuf).
            if (_kosten != null && ReferenceEquals(_aktiveSeite, _kosten))
                _kosten.SetzeProjekt(_idMarkiert, _nameMarkiert);
        }

        /// <summary>
        /// Fängt den Fall ab, dass die Kostenseite ohne Projekt dastünde.
        ///
        /// <para>
        /// <see cref="_idMarkiert"/> ist mit -1 vorbelegt und wird ausschließlich vom
        /// Ereignis <see cref="UcBkUebersicht.ProjektMarkiert"/> gefüllt. Das Ereignis
        /// stammt aus <c>ListView.SelectedIndexChanged</c> und BLEIBT AUS, solange die
        /// Übersichtsseite an keinem Fenster hängt (sie wird erst in
        /// <see cref="BaueSeiteAuf"/> in die Inhaltsfläche gehängt). Wer den Reiter
        /// betritt und ohne Umweg über die Übersicht auf „Kosten" geht, bekäme dann -1
        /// und damit die Anzeige „kein Projekt".
        /// </para>
        /// <para>
        /// Ersatz in dieser Reihenfolge: die markierte Listenzeile, sonst die Zeile des
        /// tatsächlich GEÖFFNETEN Projekts (die Zeilen stehen auch ohne Fenster im
        /// Steuerelement, nur die Markierung meldet sich dann nicht), sonst das
        /// Stammprojekt der Gruppe.
        /// </para>
        /// </summary>
        private void SichereMarkierung()
        {
            if (_idMarkiert > 0) return;

            UcBkUebersicht.AuswahlZeile z = Uebersicht.AktuelleZeile
                                            ?? Uebersicht.ZeileFuer(Uebersicht.AktuellesProjekt);
            if (z != null)
            {
                _idMarkiert = z.IdProjekt;
                _nameMarkiert = z.Projektname ?? "";
                return;
            }

            if (_idStamm > 0)
            {
                _idMarkiert = _idStamm;
                _nameMarkiert = _stammName;
            }
        }

        private void VerwirfGruppenSeiten()
        {
            if (_wirtschaft != null)
            {
                if (ReferenceEquals(_aktiveSeite, _wirtschaft)) { pnlInhalt.Controls.Clear(); _aktiveSeite = null; }
                _wirtschaft.Dispose();
                _wirtschaft = null;
            }
            if (_bericht != null)
            {
                if (ReferenceEquals(_aktiveSeite, _bericht)) { pnlInhalt.Controls.Clear(); _aktiveSeite = null; }
                _bericht.Dispose();
                _bericht = null;
            }
        }

        // ------------------------------------------------------- Menü-Zeichnung

        private void lvNav_MouseMove(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit = lvNav.HitTest(e.Location);
            int idx = (hit != null && hit.Item != null) ? hit.Item.Index : -1;
            if (idx == _hoverIndex) return;

            int alt = _hoverIndex;
            _hoverIndex = idx;
            if (alt >= 0 && alt < lvNav.Items.Count) lvNav.Invalidate(lvNav.Items[alt].Bounds);
            if (idx >= 0 && idx < lvNav.Items.Count) lvNav.Invalidate(lvNav.Items[idx].Bounds);
        }

        private void lvNav_MouseLeave(object sender, EventArgs e)
        {
            if (_hoverIndex < 0) return;
            int alt = _hoverIndex;
            _hoverIndex = -1;
            if (alt < lvNav.Items.Count) lvNav.Invalidate(lvNav.Items[alt].Bounds);
        }

        private void lvNav_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle r = e.Bounds;

            bool selected = e.Item.Selected;
            bool hot = (e.ItemIndex == _hoverIndex) && !selected;

            Color bg = selected ? cMenuSelBg : (hot ? cMenuHoverBg : cMenuBase);
            Color fg = selected ? cMenuSelFg : (hot ? cMenuHoverFg : cMenuText);
            Color ic = selected ? cMenuSelFg : (hot ? cMenuHoverFg : cMenuIcon);

            using (SolidBrush b = new SolidBrush(bg)) g.FillRectangle(b, r);

            int s = 22;
            int iconX = r.X + 16;
            int iconY = r.Y + (r.Height - s) / 2;
            ZeichneSeitenIcon(g, new Rectangle(iconX, iconY, s, s), e.Item.Tag as string, ic);

            int textX = iconX + s + 12;
            Rectangle textRect = new Rectangle(textX, r.Y, Math.Max(0, r.Right - textX - 8), r.Height);
            TextRenderer.DrawText(g, e.Item.Text, lvNav.Font, textRect, fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        /// <summary>Einfarbige Vektor-Icons je Seite (GDI+, Muster Detailsimulation).</summary>
        private static void ZeichneSeitenIcon(Graphics g, Rectangle box, string schluessel, Color farbe)
        {
            SmoothingMode alt = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float pw = Math.Max(1.6f, box.Width / 12f);
            using (Pen pen = new Pen(farbe, pw))
            using (SolidBrush brush = new SolidBrush(farbe))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                Func<float, float, PointF> P = (nx, ny) =>
                    new PointF(box.X + nx * box.Width, box.Y + ny * box.Height);

                switch (schluessel)
                {
                    case SEITE_UEBERSICHT:
                        // Liste: drei Punkte mit Linien
                        for (int i = 0; i < 3; i++)
                        {
                            float y = 0.22f + i * 0.28f;
                            g.FillEllipse(brush, P(0.10f, y - 0.06f).X, P(0.10f, y - 0.06f).Y,
                                          box.Width * 0.13f, box.Height * 0.13f);
                            g.DrawLine(pen, P(0.36f, y), P(0.92f, y));
                        }
                        break;

                    case SEITE_KOSTEN:
                        // Euro-Zeichen
                        g.DrawArc(pen, box.X + box.Width * 0.22f, box.Y + box.Height * 0.14f,
                                  box.Width * 0.68f, box.Height * 0.72f, 40, 280);
                        g.DrawLine(pen, P(0.06f, 0.42f), P(0.56f, 0.42f));
                        g.DrawLine(pen, P(0.06f, 0.60f), P(0.52f, 0.60f));
                        break;

                    case SEITE_WIRTSCHAFT:
                        // Säulendiagramm mit steigender Linie
                        g.DrawLine(pen, P(0.10f, 0.88f), P(0.92f, 0.88f));
                        g.DrawLine(pen, P(0.26f, 0.88f), P(0.26f, 0.60f));
                        g.DrawLine(pen, P(0.50f, 0.88f), P(0.50f, 0.42f));
                        g.DrawLine(pen, P(0.74f, 0.88f), P(0.74f, 0.20f));
                        break;

                    case SEITE_BERICHT:
                        // Dokument mit geknickter Ecke
                        g.DrawLines(pen, new[]
                        {
                            P(0.22f, 0.10f), P(0.62f, 0.10f), P(0.80f, 0.30f),
                            P(0.80f, 0.90f), P(0.22f, 0.90f), P(0.22f, 0.10f)
                        });
                        g.DrawLine(pen, P(0.62f, 0.10f), P(0.62f, 0.30f));
                        g.DrawLine(pen, P(0.62f, 0.30f), P(0.80f, 0.30f));
                        g.DrawLine(pen, P(0.34f, 0.52f), P(0.68f, 0.52f));
                        g.DrawLine(pen, P(0.34f, 0.70f), P(0.68f, 0.70f));
                        break;
                }
            }

            g.SmoothingMode = alt;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Seiten hängen zeitweise NICHT in pnlInhalt (nur die gerade sichtbare) —
                // sie müssen deshalb einzeln freigegeben werden.
                if (_uebersicht != null) { _uebersicht.Dispose(); _uebersicht = null; }
                if (_kosten != null) { _kosten.Dispose(); _kosten = null; }
                if (_wirtschaft != null) { _wirtschaft.Dispose(); _wirtschaft = null; }
                if (_bericht != null) { _bericht.Dispose(); _bericht = null; }
                if (_zeilenHoehe != null) { _zeilenHoehe.Dispose(); _zeilenHoehe = null; }
            }
            base.Dispose(disposing);
        }
    }
}

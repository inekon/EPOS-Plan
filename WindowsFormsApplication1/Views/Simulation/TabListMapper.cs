using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Bildet ein bestehendes TabControl auf eine ListView-Navigation ab:
    /// links eine ListView mit den TabPage-Bezeichnungen, rechts der Inhalt der
    /// gerade selektierten TabPage.
    ///
    /// Das ursprüngliche TabControl bleibt vollständig erhalten – es wird lediglich
    /// in einen SplitContainer umgehängt und sein Reiter-Kopf (die Tab-Buttons)
    /// durch Clipping ausgeblendet. Die Inhaltsumschaltung erfolgt nativ über
    /// <c>TabControl.SelectedIndex</c>; es werden KEINE Controls zwischen den
    /// Seiten verschoben.
    ///
    /// Optik entspricht <c>listViewQuellen</c> (dunkles WordPress-Stil-Menü mit
    /// einfarbigen Vektor-Icons je Gewerk). Die Icon-Zuordnung erfolgt über den
    /// TabPage-Namen; ein evtl. Suffix "_Parameter" wird abgeschnitten
    /// (tabPage_Bedarf_Parameter -> Icon-Schlüssel "tabPage_Bedarf").
    ///
    /// Verwendung (z. B. im Konstruktor, nach InitializeComponent()):
    ///     _einstellungenMapper = new TabListMapper(tabControl_Einstellungen, 200);
    ///
    /// Wird die TabPage-Liste zur Laufzeit umgebaut (z. B. in UpdateTabPages()),
    /// danach <see cref="BuildItems"/> aufrufen.
    /// </summary>
    public class TabListMapper
    {
        private readonly TabControl _tab;
        private readonly ListView _list;
        private readonly SplitContainer _split;
        private readonly Panel _host;     // Träger rechts; clippt den Reiter-Kopf weg
        private readonly bool _menuStyle;
        private bool _syncing;            // verhindert Event-Rückkopplung beim Neuaufbau
        private int _hoverIndex = -1;     // aktuell überfahrene Zeile (-1 = keine)
        private ImageList _rowSizer;      // erzwingt die Zeilenhöhe (~40 px)

        // --- Farbpalette (klassisches WP-Admin-Menü, identisch zu listViewQuellen) ---
        private static readonly Color cMenuBase    = Color.FromArgb(0x23, 0x28, 0x2d);
        private static readonly Color cMenuText    = Color.FromArgb(0xee, 0xee, 0xee);
        private static readonly Color cMenuIcon    = Color.FromArgb(0xa7, 0xaa, 0xad);
        private static readonly Color cMenuHoverBg = Color.FromArgb(0x19, 0x1e, 0x23);
        private static readonly Color cMenuHoverFg = Color.FromArgb(0x00, 0xb9, 0xeb);
        private static readonly Color cMenuSelBg   = Color.FromArgb(0x00, 0x73, 0xaa);
        private static readonly Color cMenuSelFg   = Color.White;
        private static readonly Color cMenuDisabled= Color.FromArgb(0x55, 0x5d, 0x66);

        /// <summary>Die erzeugte Navigations-ListView (links).</summary>
        public ListView ListView => _list;

        /// <summary>Der erzeugte SplitContainer – sitzt an der Stelle des TabControls.</summary>
        public SplitContainer Split => _split;

        public TabListMapper(TabControl tab, int listWidth = 200, bool menuStyle = true)
        {
            _tab = tab ?? throw new ArgumentNullException(nameof(tab));
            _menuStyle = menuStyle;
            Control parent = _tab.Parent;
            if (parent == null)
                throw new InvalidOperationException(
                    "Das TabControl muss bereits einem Parent zugewiesen sein (nach InitializeComponent aufrufen).");

            // --- SplitContainer exakt an Position/Geometrie des TabControls setzen ---
            _split = new SplitContainer
            {
                Name = _tab.Name + "_MapSplit",
                Bounds = _tab.Bounds,
                Dock = _tab.Dock,
                Anchor = _tab.Anchor,
                FixedPanel = FixedPanel.Panel1,
                SplitterWidth = 4
            };

            // --- ListView (links) – die "Reiter" als Menüeinträge ---
            _list = new ListView
            {
                Name = _tab.Name + "_MapList",
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false,
                HeaderStyle = ColumnHeaderStyle.None,
                Font = new Font("Segoe UI", 12f, FontStyle.Regular)
            };
            _list.Columns.Add("Bezeichnung");
            _list.SelectedIndexChanged += List_SelectedIndexChanged;

            // --- Host (rechts) – clippt den Reiter-Kopf des TabControls weg ---
            _host = new Panel { Dock = DockStyle.Fill };

            // Original-TabControl in den Host umhängen (Reihenfolge im Parent merken)
            int idx = parent.Controls.GetChildIndex(_tab);
            parent.Controls.Remove(_tab);

            _split.Panel1.Controls.Add(_list);
            _split.Panel2.Controls.Add(_host);
            _host.Controls.Add(_tab);

            parent.Controls.Add(_split);
            parent.Controls.SetChildIndex(_split, idx);

            if (_split.Width > listWidth + 50)
                _split.SplitterDistance = listWidth;

            _tab.Dock = DockStyle.None; // wird im Host frei positioniert (Kopf nach oben weggeclippt)

            _host.SizeChanged += (s, e) => LayoutTab();
            _split.SplitterMoved += (s, e) => FitColumn();

            if (_menuStyle) ApplyMenuStyle();
            BuildItems();

            if (_tab.IsHandleCreated) Initialize();
            else _tab.HandleCreated += (s, e) => Initialize();
        }

        private void Initialize()
        {
            LayoutTab();
            FitColumn();
            SelectCurrent();
        }

        /// <summary>Liest die aktuellen TabPage-Bezeichnungen in die ListView ein.</summary>
        public void BuildItems()
        {
            _syncing = true;
            _list.Items.Clear();
            for (int i = 0; i < _tab.TabPages.Count; i++)
            {
                TabPage p = _tab.TabPages[i];
                // Tag = Icon-Schlüssel (Seitenname ohne "_Parameter"); Zeilenindex == Tab-Index
                _list.Items.Add(new ListViewItem(p.Text) { Tag = IconKeyFromPage(p) });
            }
            _syncing = false;
            FitColumn();
            SelectCurrent();
        }

        /// <summary>Markiert in der ListView die Zeile der aktuell aktiven TabPage.</summary>
        private void SelectCurrent()
        {
            if (_list.Items.Count == 0) return;
            int sel = _tab.SelectedIndex >= 0 ? _tab.SelectedIndex : 0;
            if (sel >= _list.Items.Count) sel = 0;

            _syncing = true;
            _list.SelectedItems.Clear();
            _list.Items[sel].Selected = true;
            if (_list.IsHandleCreated) _list.Items[sel].EnsureVisible();
            _syncing = false;

            _tab.SelectedIndex = sel;
        }

        private void List_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_syncing || _list.SelectedItems.Count == 0) return;
            int i = _list.SelectedItems[0].Index;   // Zeilenindex == Tab-Index
            if (i >= 0 && i < _tab.TabPages.Count)
                _tab.SelectedIndex = i;             // native Umschaltung – TabControl bleibt unangetastet
        }

        private static string IconKeyFromPage(TabPage p)
        {
            string n = p.Name ?? string.Empty;
            const string suffix = "_Parameter";
            if (n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                n = n.Substring(0, n.Length - suffix.Length);
            return n;
        }

        // ============================================================
        //  Reiter-Kopf verstecken (Clipping über die obere Host-Kante)
        // ============================================================
        private void LayoutTab()
        {
            if (_host.ClientSize.Width <= 0 || _host.ClientSize.Height <= 0) return;
            int header = HeaderHeight();
            _tab.Location = new Point(0, -header);
            _tab.Size = new Size(_host.ClientSize.Width, _host.ClientSize.Height + header);
        }

        private int HeaderHeight()
        {
            try
            {
                if (_tab.IsHandleCreated && _tab.TabCount > 0)
                    return _tab.GetTabRect(0).Bottom;   // exakte Höhe des Reiter-Streifens
            }
            catch { /* Handle/Index noch nicht bereit -> Fallback */ }
            return _tab.ItemSize.Height > 0 ? _tab.ItemSize.Height + 4 : 22;
        }

        private void FitColumn()
        {
            if (_list.Columns.Count > 0)
                _list.Columns[0].Width = _list.ClientSize.Width - 2;
        }

        // ============================================================
        //  Menü-Optik (dunkles WordPress-Stil-Menü) – wie listViewQuellen
        // ============================================================
        private void ApplyMenuStyle()
        {
            // Zeilenhöhe über eine (leere) SmallImageList erzwingen (~40 px).
            _rowSizer = new ImageList { ImageSize = new Size(1, 40), ColorDepth = ColorDepth.Depth32Bit };
            _list.SmallImageList = _rowSizer;

            _list.OwnerDraw = true;
            _list.BorderStyle = BorderStyle.None;
            _list.BackColor = cMenuBase;
            _list.ForeColor = cMenuText;
            _split.Panel1.BackColor = cMenuBase; // nahtlose dunkle Spalte

            // Flicker beim Hover reduzieren (DoubleBuffering der ListView).
            try
            {
                typeof(ListView).GetProperty("DoubleBuffered",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(_list, true, null);
            }
            catch { /* unkritisch */ }

            _list.DrawColumnHeader += (s, e) => { /* Header ist ausgeblendet */ };
            _list.DrawSubItem += (s, e) => { /* gesamte Zeile wird in DrawItem gezeichnet */ };
            _list.DrawItem += List_DrawItem;
            _list.MouseMove += List_MouseMove;
            _list.MouseLeave += List_MouseLeave;
        }

        private void List_MouseMove(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit = _list.HitTest(e.Location);
            int idx = (hit != null && hit.Item != null) ? hit.Item.Index : -1;
            if (idx == _hoverIndex) return;

            int alt = _hoverIndex;
            _hoverIndex = idx;
            if (alt >= 0 && alt < _list.Items.Count) _list.Invalidate(_list.Items[alt].Bounds);
            if (idx >= 0 && idx < _list.Items.Count) _list.Invalidate(_list.Items[idx].Bounds);
        }

        private void List_MouseLeave(object sender, EventArgs e)
        {
            if (_hoverIndex < 0) return;
            int alt = _hoverIndex;
            _hoverIndex = -1;
            if (alt < _list.Items.Count) _list.Invalidate(_list.Items[alt].Bounds);
        }

        private void List_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle r = e.Bounds;

            string tag = (e.Item.Tag != null) ? e.Item.Tag.ToString() : "";
            bool disabled = (tag == "DEAKTIVIERT");
            bool selected = e.Item.Selected && !disabled;
            bool hot = (e.ItemIndex == _hoverIndex) && !selected && !disabled;

            Color bg = selected ? cMenuSelBg : (hot ? cMenuHoverBg : cMenuBase);
            Color fg = disabled ? cMenuDisabled : (selected ? cMenuSelFg : (hot ? cMenuHoverFg : cMenuText));
            Color ic = disabled ? cMenuDisabled : (selected ? cMenuSelFg : (hot ? cMenuHoverFg : cMenuIcon));

            using (SolidBrush b = new SolidBrush(bg))
                g.FillRectangle(b, r);

            // Icon (quadratisch, vertikal zentriert)
            int s = 22;
            int iconX = r.X + 16;
            int iconY = r.Y + (r.Height - s) / 2;
            ZeichneGewerkIcon(g, new Rectangle(iconX, iconY, s, s), tag, ic);

            // Beschriftung
            int textX = iconX + s + 12;
            Rectangle textRect = new Rectangle(textX, r.Y, Math.Max(0, r.Right - textX - 8), r.Height);
            TextRenderer.DrawText(g, e.Item.Text, _list.Font, textRect, fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        // ------------------------------------------------------------
        //  Vektor-Icons je Gewerk (einfarbig, per GDI+ gezeichnet)
        // ------------------------------------------------------------
        private void ZeichneGewerkIcon(Graphics g, Rectangle box, string tag, Color farbe)
        {
            SmoothingMode altMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float pw = Math.Max(1.8f, box.Width / 11f);
            using (Pen pen = new Pen(farbe, pw))
            using (SolidBrush brush = new SolidBrush(farbe))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                Func<float, float, PointF> P = (nx, ny) =>
                    new PointF(box.X + nx * box.Width, box.Y + ny * box.Height);

                switch (tag)
                {
                    case "tabPage_Bedarf": // Energiebedarf – Blitz
                    {
                        PointF[] bolt =
                        {
                            P(0.58f, 0.06f), P(0.30f, 0.54f), P(0.48f, 0.54f),
                            P(0.40f, 0.94f), P(0.74f, 0.42f), P(0.54f, 0.42f)
                        };
                        g.FillPolygon(brush, bolt);
                        break;
                    }
                    case "tabPage_Heizkessel": // Flamme
                    {
                        using (var path = new GraphicsPath())
                        {
                            path.AddBezier(P(0.50f, 0.96f), P(0.14f, 0.86f), P(0.16f, 0.60f), P(0.30f, 0.50f));
                            path.AddBezier(P(0.30f, 0.50f), P(0.40f, 0.42f), P(0.36f, 0.22f), P(0.50f, 0.05f));
                            path.AddBezier(P(0.50f, 0.05f), P(0.60f, 0.26f), P(0.70f, 0.34f), P(0.72f, 0.54f));
                            path.AddBezier(P(0.72f, 0.54f), P(0.80f, 0.66f), P(0.78f, 0.88f), P(0.50f, 0.96f));
                            path.CloseFigure();
                            g.FillPath(brush, path);
                        }
                        break;
                    }
                    case "tabPage_BHKW": // Zahnrad
                        ZeichneZahnrad(g, pen, brush, box, farbe);
                        break;

                    case "tabPage_Wärmepumpe": // Kreislauf (zwei Pfeile)
                        ZeichneWaermepumpe(g, pen, box);
                        break;

                    case "tabPage_Solarthermie": // Sonne
                    {
                        float cx = box.X + box.Width * 0.5f;
                        float cy = box.Y + box.Height * 0.5f;
                        float rCore = box.Width * 0.17f;
                        g.FillEllipse(brush, cx - rCore, cy - rCore, rCore * 2, rCore * 2);
                        float r1 = box.Width * 0.30f, r2 = box.Width * 0.46f;
                        for (int i = 0; i < 8; i++)
                        {
                            double a = i * Math.PI / 4.0;
                            float dx = (float)Math.Cos(a), dy = (float)Math.Sin(a);
                            g.DrawLine(pen, cx + dx * r1, cy + dy * r1, cx + dx * r2, cy + dy * r2);
                        }
                        break;
                    }
                    case "tabPage_Photovoltaik": // Solarpanel (Raster) auf Ständer
                    {
                        RectangleF panel = new RectangleF(
                            box.X + box.Width * 0.16f, box.Y + box.Height * 0.18f,
                            box.Width * 0.68f, box.Height * 0.46f);
                        g.DrawRectangle(pen, panel.X, panel.Y, panel.Width, panel.Height);
                        g.DrawLine(pen, panel.X + panel.Width / 3f, panel.Y, panel.X + panel.Width / 3f, panel.Bottom);
                        g.DrawLine(pen, panel.X + 2f * panel.Width / 3f, panel.Y, panel.X + 2f * panel.Width / 3f, panel.Bottom);
                        g.DrawLine(pen, panel.X, panel.Y + panel.Height / 2f, panel.Right, panel.Y + panel.Height / 2f);
                        g.DrawLine(pen, P(0.50f, 0.64f).X, P(0.50f, 0.64f).Y, P(0.50f, 0.90f).X, P(0.50f, 0.90f).Y);
                        g.DrawLine(pen, P(0.34f, 0.90f).X, P(0.34f, 0.90f).Y, P(0.66f, 0.90f).X, P(0.66f, 0.90f).Y);
                        break;
                    }
                    case "tabPage_Stromspeicher": // Batterie
                    {
                        RectangleF body = new RectangleF(
                            box.X + box.Width * 0.24f, box.Y + box.Height * 0.30f,
                            box.Width * 0.52f, box.Height * 0.60f);
                        g.DrawRectangle(pen, body.X, body.Y, body.Width, body.Height);
                        g.FillRectangle(brush, P(0.42f, 0.16f).X, P(0.42f, 0.16f).Y, box.Width * 0.16f, box.Height * 0.14f);
                        g.DrawLine(pen, P(0.34f, 0.60f).X, P(0.34f, 0.60f).Y, P(0.66f, 0.60f).X, P(0.66f, 0.60f).Y);
                        break;
                    }
                    case "tabPage_Ergebnis": // Balkendiagramm
                    {
                        g.DrawLine(pen, P(0.16f, 0.84f).X, P(0.16f, 0.84f).Y, P(0.86f, 0.84f).X, P(0.86f, 0.84f).Y);
                        float bw = box.Width * 0.12f;
                        DrawBar(g, brush, P(0.24f, 0.60f), bw, P(0.24f, 0.84f).Y);
                        DrawBar(g, brush, P(0.44f, 0.46f), bw, P(0.44f, 0.84f).Y);
                        DrawBar(g, brush, P(0.64f, 0.30f), bw, P(0.64f, 0.84f).Y);
                        break;
                    }
                    default: // unbekannt / DEAKTIVIERT – schlichter Punkt
                    {
                        float d = box.Width * 0.20f;
                        g.DrawEllipse(pen, box.X + box.Width * 0.5f - d, box.Y + box.Height * 0.5f - d, d * 2, d * 2);
                        break;
                    }
                }
            }

            g.SmoothingMode = altMode;
        }

        private static void DrawBar(Graphics g, SolidBrush brush, PointF topLeft, float width, float baselineY)
        {
            g.FillRectangle(brush, topLeft.X, topLeft.Y, width, baselineY - topLeft.Y);
        }

        private void ZeichneZahnrad(Graphics g, Pen pen, SolidBrush brush, Rectangle box, Color farbe)
        {
            float cx = box.X + box.Width * 0.5f, cy = box.Y + box.Height * 0.5f;
            float rRing = box.Width * 0.28f;
            float rTeeth = box.Width * 0.44f;
            float rHub = box.Width * 0.11f;

            using (Pen tp = new Pen(farbe, Math.Max(2.2f, box.Width / 8f)))
            {
                tp.StartCap = LineCap.Round;
                tp.EndCap = LineCap.Round;
                for (int i = 0; i < 8; i++)
                {
                    double a = i * Math.PI / 4.0;
                    float dx = (float)Math.Cos(a), dy = (float)Math.Sin(a);
                    g.DrawLine(tp, cx + dx * rRing, cy + dy * rRing, cx + dx * rTeeth, cy + dy * rTeeth);
                }
            }
            g.DrawEllipse(pen, cx - rRing, cy - rRing, rRing * 2, rRing * 2);
            g.FillEllipse(brush, cx - rHub, cy - rHub, rHub * 2, rHub * 2);
        }

        private void ZeichneWaermepumpe(Graphics g, Pen pen, Rectangle box)
        {
            RectangleF arc = new RectangleF(
                box.X + box.Width * 0.18f, box.Y + box.Height * 0.18f,
                box.Width * 0.64f, box.Height * 0.64f);
            float rx = arc.Width / 2f, ry = arc.Height / 2f;
            float cx = arc.X + rx, cy = arc.Y + ry;

            g.DrawArc(pen, arc.X, arc.Y, arc.Width, arc.Height, 105f, 150f);
            g.DrawArc(pen, arc.X, arc.Y, arc.Width, arc.Height, 285f, 150f);

            float ah = box.Width * 0.18f;
            DrawArcArrow(g, pen, cx, cy, rx, ry, 255f, ah);
            DrawArcArrow(g, pen, cx, cy, rx, ry, 75f, ah);
        }

        private static void DrawArcArrow(Graphics g, Pen pen, float cx, float cy, float rx, float ry, float deg, float size)
        {
            PointF tip = new PointF(
                cx + rx * (float)Math.Cos(deg * Math.PI / 180.0),
                cy + ry * (float)Math.Sin(deg * Math.PI / 180.0));
            float pdeg = deg - 12f;
            PointF prev = new PointF(
                cx + rx * (float)Math.Cos(pdeg * Math.PI / 180.0),
                cy + ry * (float)Math.Sin(pdeg * Math.PI / 180.0));
            double ang = Math.Atan2(tip.Y - prev.Y, tip.X - prev.X);
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                double b = ang + sgn * 2.5;
                PointF q = new PointF(
                    tip.X + (float)Math.Cos(b) * size,
                    tip.Y + (float)Math.Sin(b) * size);
                g.DrawLine(pen, tip, q);
            }
        }
    }
}

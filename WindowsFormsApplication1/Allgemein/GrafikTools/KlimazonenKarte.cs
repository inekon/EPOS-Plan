using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Deutschlandkarte der 15 Klimazonen nach DIN 4710 mit Zonen-Hervorhebung und
    /// Klickauswahl (Konzept_Klimazonenkarte_EPOS-Plan.md, Anwenderwunsch 29.08.2026).
    ///
    /// Datengrundlage sind zwei eingebettete Ressourcen aus der vom Anwender
    /// erstellten Kartengrafik (Repo-Wurzel <c>Klimazonen DIN4710\</c>):
    /// <list type="bullet">
    ///   <item><description><c>Zonenkarte_Klimazonen.png</c> (3390 × 3510) — das
    ///     ANZEIGEBILD mit allem, was die SVG-Nachzeichnung nicht hergäbe (Flüsse,
    ///     Gradnetz, Städtenamen, Legende, Maßstab).</description></item>
    ///   <item><description><c>Zonenkarte_Klimazonen.svg</c> — die QUELLE der
    ///     15 Zonenpolygone (Gruppe <c>zonenflaechen</c>, reine M/L/Z-Pfade, je Zone
    ///     ein Pfad mit bis zu 8 Teilflächen und <c>fill-rule="evenodd"</c>) und der
    ///     Zonennummern (Gruppe <c>zonennummern</c>).</description></item>
    /// </list>
    ///
    /// Die Zuordnung Pfad → Zonennummer wird beim ersten Bedarf ZUR LAUFZEIT
    /// hergestellt: Jede in der SVG beschriftete Nummer wird per Punkt-in-Fläche-Test
    /// ihrer Zonenfläche zugeschlagen (Nummern liegen immer in der eigenen Zone;
    /// mehrfach beschriftete Zonen wie die 6 stören nicht). So überlebt das Control
    /// eine überarbeitete Kartengrafik ohne Codeänderung, solange die Gruppennamen
    /// stehen. Scheitert das Laden, zeigt das Control eine Hinweiszeile und meldet
    /// <see cref="AuswahlMoeglich"/> = false — der Dialog bleibt bedienbar.
    ///
    /// Bild und Polygone teilen sich den Koordinatenraum der SVG-viewBox (das PNG ist
    /// eine 2,6-fach aufgelöste Wiedergabe genau dieser Box); gezeichnet wird über
    /// einen gemeinsamen Skalierungsfaktor mit Zentrierung, der Hit-Test rechnet den
    /// Mauspunkt in den SVG-Raum zurück. DpiUnaware wie die gesamte Anwendung.
    /// </summary>
    public class KlimazonenKarte : Control
    {
        // ------------------------------------------------------------------
        // Kartendaten - einmal je Prozess
        // ------------------------------------------------------------------

        private sealed class Kartendaten
        {
            public Image Bild;
            public float SvgBreite;
            public float SvgHoehe;
            /// <summary>Index 0..14 = Zone 1..15 (nach der Nummern-Zuordnung).</summary>
            public GraphicsPath[] Zonen;
        }

        private static readonly object _ladeSchloss = new object();
        private static Kartendaten _daten;
        private static bool _ladeVersucht;

        /// <summary>true, sobald Bild und alle 15 Zonenpolygone bereitstehen.</summary>
        public bool AuswahlMoeglich { get { return Daten() != null; } }

        private static Kartendaten Daten()
        {
            if (_daten != null) return _daten;
            lock (_ladeSchloss)
            {
                if (!_ladeVersucht)
                {
                    _ladeVersucht = true;
                    try { _daten = Laden(); }
                    catch { _daten = null; }
                }
                return _daten;
            }
        }

        private static Kartendaten Laden()
        {
            var asm = typeof(KlimazonenKarte).Assembly;

            Image bild;
            using (Stream s = asm.GetManifestResourceStream("Zonenkarte_Klimazonen.png"))
            {
                if (s == null) return null;
                // Kopie in den Speicher: Image.FromStream hält den Stream sonst offen.
                using (var ms = new MemoryStream())
                {
                    s.CopyTo(ms);
                    bild = Image.FromStream(new MemoryStream(ms.ToArray()));
                }
            }

            string svg;
            using (Stream s = asm.GetManifestResourceStream("Zonenkarte_Klimazonen.svg"))
            {
                if (s == null) return null;
                using (var r = new StreamReader(s)) svg = r.ReadToEnd();
            }

            Match box = Regex.Match(svg, "viewBox=\"0 0 ([0-9.]+) ([0-9.]+)\"");
            if (!box.Success) return null;
            float breite = float.Parse(box.Groups[1].Value, CultureInfo.InvariantCulture);
            float hoehe = float.Parse(box.Groups[2].Value, CultureInfo.InvariantCulture);

            Match flaechen = Regex.Match(svg, "<g id=\"zonenflaechen\".*?</g>", RegexOptions.Singleline);
            if (!flaechen.Success) return null;

            var pfade = new List<GraphicsPath>();
            foreach (Match m in Regex.Matches(flaechen.Value, "\\sd=\"([^\"]+)\""))
            {
                GraphicsPath p = PfadParsen(m.Groups[1].Value);
                if (p != null) pfade.Add(p);
            }

            // Zonennummern: je Beschriftung eine Doppelzeile (weißer Halo + Text);
            // gezählt wird nur der gefüllte Text. Zuordnung per Punkt-in-Fläche.
            var zonen = new GraphicsPath[15];
            Match nummern = Regex.Match(svg, "<g id=\"zonennummern\".*?</g>", RegexOptions.Singleline);
            if (nummern.Success && pfade.Count == 15)
            {
                foreach (Match t in Regex.Matches(nummern.Value,
                    "<text x=\"([0-9.]+)\" y=\"([0-9.]+)\"[^>]*fill=\"#15181C\"[^>]*>([0-9]+)</text>"))
                {
                    int zone = int.Parse(t.Groups[3].Value, CultureInfo.InvariantCulture);
                    if (zone < 1 || zone > 15) continue;
                    var punkt = new PointF(
                        float.Parse(t.Groups[1].Value, CultureInfo.InvariantCulture),
                        float.Parse(t.Groups[2].Value, CultureInfo.InvariantCulture));
                    foreach (GraphicsPath p in pfade)
                        if (p.IsVisible(punkt)) { zonen[zone - 1] = p; break; }
                }
            }

            for (int i = 0; i < 15; i++)
                if (zonen[i] == null) return null; // unvollständig - lieber gar keine Auswahl

            return new Kartendaten { Bild = bild, SvgBreite = breite, SvgHoehe = hoehe, Zonen = zonen };
        }

        /// <summary>
        /// Baut aus einem M/L/Z-Pfad (Koordinaten mit Leerzeichen getrennt, so liefert
        /// sie die Anwender-SVG) einen <see cref="GraphicsPath"/>. Mehrere M…Z-Teile
        /// werden eigene Teilpolygone; <c>FillMode.Alternate</c> entspricht dem
        /// <c>fill-rule="evenodd"</c> der Quelle und trägt damit auch Lochflächen.
        /// </summary>
        private static GraphicsPath PfadParsen(string d)
        {
            var pfad = new GraphicsPath(FillMode.Alternate);
            var punkte = new List<PointF>();
            string[] token = d.Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            int i = 0;
            while (i < token.Length)
            {
                string t = token[i];
                if (t == "M" || t == "L")
                {
                    if (t == "M" && punkte.Count > 2) { pfad.AddPolygon(punkte.ToArray()); punkte.Clear(); }
                    if (i + 2 >= token.Length) break;
                    punkte.Add(new PointF(
                        float.Parse(token[i + 1], CultureInfo.InvariantCulture),
                        float.Parse(token[i + 2], CultureInfo.InvariantCulture)));
                    i += 3;
                }
                else if (t == "Z" || t == "z")
                {
                    if (punkte.Count > 2) { pfad.AddPolygon(punkte.ToArray()); punkte.Clear(); }
                    i++;
                }
                else
                {
                    // Nackte Koordinatenpaare nach L (implizite Fortsetzung).
                    if (i + 1 >= token.Length) break;
                    punkte.Add(new PointF(
                        float.Parse(token[i], CultureInfo.InvariantCulture),
                        float.Parse(token[i + 1], CultureInfo.InvariantCulture)));
                    i += 2;
                }
            }
            if (punkte.Count > 2) pfad.AddPolygon(punkte.ToArray());

            return pfad.PointCount > 2 ? pfad : null;
        }

        // ------------------------------------------------------------------
        // Instanz
        // ------------------------------------------------------------------

        private int _gewaehlt;
        private int _hover;
        private readonly ToolTip _tip = new ToolTip();

        /// <summary>Der Anwender hat eine Zone angeklickt (Einfachklick).</summary>
        public event EventHandler ZoneGewaehlt;

        /// <summary>Doppelklick auf eine Zone - der Dialog darf direkt übernehmen.</summary>
        public event EventHandler ZoneUebernommen;

        public KlimazonenKarte()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
        }

        /// <summary>Gewählte Zone 1…15; 0 = keine. Setzen zeichnet neu.</summary>
        public int GewaehlteZone
        {
            get { return _gewaehlt; }
            set { _gewaehlt = (value >= 1 && value <= 15) ? value : 0; Invalidate(); }
        }

        /// <summary>Skalierungsfaktor SVG → Client samt Zentrierversatz.</summary>
        private bool Massstab(Kartendaten k, out float f, out float ox, out float oy)
        {
            f = 0; ox = 0; oy = 0;
            if (k == null || ClientSize.Width < 10 || ClientSize.Height < 10) return false;
            f = Math.Min(ClientSize.Width / k.SvgBreite, ClientSize.Height / k.SvgHoehe);
            ox = (ClientSize.Width - k.SvgBreite * f) / 2f;
            oy = (ClientSize.Height - k.SvgHoehe * f) / 2f;
            return true;
        }

        private int ZoneAnPunkt(Point p)
        {
            Kartendaten k = Daten();
            float f, ox, oy;
            if (!Massstab(k, out f, out ox, out oy)) return 0;
            var svgPunkt = new PointF((p.X - ox) / f, (p.Y - oy) / f);
            for (int i = 0; i < 15; i++)
                if (k.Zonen[i].IsVisible(svgPunkt)) return i + 1;
            return 0;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Kartendaten k = Daten();
            float f, ox, oy;
            if (!Massstab(k, out f, out ox, out oy))
            {
                // Ohne Kartendaten bleibt der Dialog bedienbar - nur eben ohne Karte.
                TextRenderer.DrawText(e.Graphics, MyResource.Resource.SIMQ_KARTE_LADEFEHLER,
                    Font, ClientRectangle, Color.Firebrick,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
                return;
            }

            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(k.Bild, new RectangleF(ox, oy, k.SvgBreite * f, k.SvgHoehe * f));

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TranslateTransform(ox, oy);
            e.Graphics.ScaleTransform(f, f);

            if (_hover >= 1 && _hover != _gewaehlt)
            {
                using (var fuellung = new SolidBrush(Color.FromArgb(70, 30, 80, 160)))
                using (var stift = new Pen(Color.FromArgb(30, 80, 160), 2f / f))
                {
                    e.Graphics.FillPath(fuellung, k.Zonen[_hover - 1]);
                    e.Graphics.DrawPath(stift, k.Zonen[_hover - 1]);
                }
            }

            if (_gewaehlt >= 1)
            {
                using (var fuellung = new SolidBrush(Color.FromArgb(60, 178, 34, 34)))
                using (var stift = new Pen(Color.Firebrick, 3f / f))
                {
                    e.Graphics.FillPath(fuellung, k.Zonen[_gewaehlt - 1]);
                    e.Graphics.DrawPath(stift, k.Zonen[_gewaehlt - 1]);
                }
            }

            e.Graphics.ResetTransform();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int zone = ZoneAnPunkt(e.Location);
            if (zone == _hover) return;
            _hover = zone;
            Cursor = zone >= 1 ? Cursors.Hand : Cursors.Default;
            _tip.SetToolTip(this, zone >= 1
                ? string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMQ_KARTE_ZONE_TIP,
                    zone, VDI4640Pruefung.VolllaststundenZone(zone).ToString("N0", CultureInfo.CurrentCulture))
                : "");
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hover != 0) { _hover = 0; Invalidate(); }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int zone = ZoneAnPunkt(e.Location);
            if (zone < 1) return;
            GewaehlteZone = zone;
            EventHandler h = ZoneGewaehlt;
            if (h != null) h(this, EventArgs.Empty);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            int zone = ZoneAnPunkt(e.Location);
            if (zone < 1) return;
            GewaehlteZone = zone;
            EventHandler h = ZoneUebernommen;
            if (h != null) h(this, EventArgs.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _tip.Dispose();
            base.Dispose(disposing);
        }
    }
}

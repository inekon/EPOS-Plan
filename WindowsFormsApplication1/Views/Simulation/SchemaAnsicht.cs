using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE D4 (Konzept_KonfigUI_Hydraulik, Abschnitt 3; Mockup
    /// <c>Entwurf_Hydraulikuebersicht_Konfiguration.html</c>, Abschnitte 1 und 2) —
    /// die gezeichnete Hydraulikübersicht.
    ///
    /// <b>Vier Spalten wie im Entwurf:</b> Wärmequelle → Erzeuger → Speicher → Abnehmer.
    /// Ladeleitungen koralle mit Prioritätskreis, Versorgung grün, Quellseite blau,
    /// Kaskade blau gestrichelt. Darunter das Pillen-Band der automatisch abgeleiteten
    /// Kaskadenkette und die Legende.
    ///
    /// <b>Freies GDI+, kein Chart.</b> Die MS-Chart-Fallstricke des Projekts betreffen
    /// Diagramme; hier wird gezeichnet wie in <c>Allgemein/GrafikTools</c> und in
    /// <see cref="SpeicherKarte"/>: eigenes <c>OnPaint</c>, Doppelpufferung über
    /// <see cref="ControlStyles.OptimizedDoubleBuffer"/>. Farben und Maße kommen aus
    /// <see cref="KartenStil"/> — dieselbe Tabelle wie die Kartenansicht, damit Liste und
    /// Schema nicht zwei Farbklänge führen.
    ///
    /// <b>Reine Lesefläche.</b> Wie die Karten hält die Ansicht keinen Datenbankzustand.
    /// Klick meldet die Auswahl, Doppelklick den Editorwunsch; was daraufhin geschieht,
    /// entscheidet <c>Form_Simulation_Config</c>.
    ///
    /// <b>Was gezeichnet wird, entscheidet <see cref="SchemaModell"/>.</b> Diese Klasse
    /// kennt keine Datenbank und keine Fachregel; sie ordnet Kästen an und malt Linien.
    /// Deshalb ist die AUSSAGE des Schemas ohne Bildschirm prüfbar (Knoten- und
    /// Kantenliste), und geprüft werden hier nur noch Trefferflächen.
    /// </summary>
    internal sealed class SchemaAnsicht : Panel
    {
        // --- Maße (Entwurf sinngemäß, nicht pixelgenau) --------------------------------

        private const int RAND = 18;
        private const int SPALTE_ABSTAND = 56;
        private const int KNOTEN_ABSTAND = 14;
        private const int KOPF_HOEHE = 26;

        private static readonly int[] SPALTEN_BREITE = { 150, 214, 190, 132 };

        private const int ZEILE_HOEHE = 15;
        private const int TITEL_HOEHE = 19;
        private const int BADGE_HOEHE = 17;
        private const int KNOTEN_RAND = 8;

        private const int BAND_ABSTAND = 26;
        private const int BAND_ZEILE = 30;
        private const int PILLE_RAND = 10;
        private const int PFEIL_BREITE = 18;

        private const int LEGENDE_ZEILE = 20;

        /// <summary>Radius des Prioritätskreises an einer Ladeleitung.</summary>
        private const int PRIO_RADIUS = 9;

        // --- Zustand ------------------------------------------------------------------

        private SchemaModell _modell = new SchemaModell();

        /// <summary>Rechteck je Knotenschlüssel — die Trefferfläche.</summary>
        private readonly Dictionary<string, Rectangle> _flaechen =
            new Dictionary<string, Rectangle>(StringComparer.Ordinal);

        /// <summary>Rechteck je Kettenglied (Band); Schlüssel ist der Knotenschlüssel.</summary>
        private readonly List<KeyValuePair<string, Rectangle>> _bandflaechen =
            new List<KeyValuePair<string, Rectangle>>();

        private readonly ToolTip _tip = new ToolTip();
        private string _hinweisFuer = "";

        private string _auswahl = "";
        private Font _kleinschrift;
        private Font _fettschrift;

        /// <summary>Klick auf ein Element; Parameter ist der sprachneutrale Knotenschlüssel.</summary>
        public event Action<string> Ausgewaehlt;

        /// <summary>Doppelklick auf ein Element — öffnet denselben Editor wie die Karte.</summary>
        public event Action<string> Bearbeiten;

        public SchemaAnsicht()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);

            AutoScroll = true;
            BackColor = Color.White;

            _tip.AutoPopDelay = 20000;
            _tip.InitialDelay = 400;
            _tip.ReshowDelay = 100;

            _kleinschrift = new Font(Font.FontFamily, Math.Max(6.5f, Font.Size - 1f));
            _fettschrift = new Font(Font, FontStyle.Bold);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);

            if (_kleinschrift != null) _kleinschrift.Dispose();
            if (_fettschrift != null) _fettschrift.Dispose();
            _kleinschrift = new Font(Font.FontFamily, Math.Max(6.5f, Font.Size - 1f));
            _fettschrift = new Font(Font, FontStyle.Bold);

            Neuordnen();
        }

        /// <summary>Das gezeichnete Modell; nie <c>null</c>.</summary>
        public SchemaModell Modell
        {
            get { return _modell; }
        }

        /// <summary>Setzt das Modell und ordnet neu an.</summary>
        public void Setzen(SchemaModell modell)
        {
            _modell = modell ?? new SchemaModell();

            // Eine Auswahl, die es im neuen Modell nicht mehr gibt, verfällt - sonst
            // bliebe eine Hervorhebung ohne Element stehen.
            if (_modell.Finden(_auswahl) == null) _auswahl = "";

            Neuordnen();
        }

        /// <summary>
        /// Der ausgewählte Knoten (sprachneutraler Schlüssel); "" = keiner.
        /// Das Setzen meldet NICHT zurück — sonst schaukelten sich Liste und Schema
        /// gegenseitig auf, wenn eine Seite die andere nachführt.
        /// </summary>
        public string Auswahl
        {
            get { return _auswahl; }
            set
            {
                string neu = value ?? "";
                if (string.Equals(_auswahl, neu, StringComparison.Ordinal)) return;
                _auswahl = _modell.Finden(neu) != null ? neu : "";
                SichtbarMachen(_auswahl);
                Invalidate();
            }
        }

        /// <summary>Trefferfläche eines Knotens in Modellkoordinaten; leer = unbekannt.</summary>
        public Rectangle FlaecheVon(string schluessel)
        {
            Rectangle r;
            if (schluessel != null && _flaechen.TryGetValue(schluessel, out r)) return r;
            return Rectangle.Empty;
        }

        /// <summary>
        /// Knoten an einer Stelle in MODELLkoordinaten (also ohne Bildlaufversatz);
        /// "" = kein Treffer. Für Prüfprogramme ohne Maus.
        /// </summary>
        public string Treffer(Point modellPunkt)
        {
            foreach (KeyValuePair<string, Rectangle> f in _flaechen)
                if (f.Value.Contains(modellPunkt)) return f.Key;

            foreach (KeyValuePair<string, Rectangle> f in _bandflaechen)
                if (f.Value.Contains(modellPunkt)) return f.Key;

            return "";
        }

        /// <summary>Rechnet einen Punkt der Anzeige in Modellkoordinaten um.</summary>
        private Point InModell(Point anzeige)
        {
            return new Point(anzeige.X - AutoScrollPosition.X, anzeige.Y - AutoScrollPosition.Y);
        }

        // --- Anordnung ----------------------------------------------------------------

        private readonly int[] _spaltenX = new int[4];
        private int _inhaltBreite;
        private int _inhaltHoehe;
        private int _bandOben;
        private int _legendeOben;

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Neuordnen();
        }

        /// <summary>
        /// Berechnet alle Rechtecke neu.
        ///
        /// <b>Der Ablauf.</b> Erst die Erzeugerspalte von oben nach unten — sie gibt die
        /// senkrechte Ordnung vor (Kaskadenreihenfolge). Dann die Quellen auf die Höhe
        /// ihres Erzeugers, dann die Speicher auf die MITTLERE Höhe ihrer Lader und die
        /// Abnehmer auf die mittlere Höhe ihrer Zuflüsse; Überschneidungen werden
        /// anschließend nach unten aufgelöst. Damit laufen die Leitungen weitgehend
        /// waagerecht, ohne dass eine Kantenoptimierung nötig wäre.
        /// </summary>
        private void Neuordnen()
        {
            _flaechen.Clear();
            _bandflaechen.Clear();

            int x = RAND;
            for (int i = 0; i < 4; i++)
            {
                _spaltenX[i] = x;
                x += SPALTEN_BREITE[i] + SPALTE_ABSTAND;
            }
            _inhaltBreite = x - SPALTE_ABSTAND + RAND;

            int oben = RAND + KOPF_HOEHE;
            int unten = oben;

            // 1. Erzeuger
            int y = oben;
            foreach (SchemaModell.Knoten k in _modell.Spalte(SchemaModell.Knotenart.Erzeuger))
            {
                int h = KnotenHoehe(k);
                _flaechen[k.Schluessel] = new Rectangle(_spaltenX[1], y, SPALTEN_BREITE[1], h);
                y += h + KNOTEN_ABSTAND;
            }
            if (y > unten) unten = y;

            // 2. Quellen - je Erzeuger genau eine, also auf dessen Höhe.
            foreach (SchemaModell.Knoten k in _modell.Spalte(SchemaModell.Knotenart.Quelle))
            {
                int h = KnotenHoehe(k);
                Rectangle erz = FlaecheVon(SchemaModell.PRAEFIX_ERZEUGER + k.ID);
                int mitte = erz.IsEmpty ? oben + h / 2 : erz.Top + erz.Height / 2;
                _flaechen[k.Schluessel] =
                    new Rectangle(_spaltenX[0], Math.Max(oben, mitte - h / 2), SPALTEN_BREITE[0], h);
            }
            UeberschneidungenAufloesen(_modell.Spalte(SchemaModell.Knotenart.Quelle), oben);

            // 3. Speicher - mittlere Höhe ihrer Lader.
            SpalteAusrichten(_modell.Spalte(SchemaModell.Knotenart.Speicher), 2, oben);

            // 4. Abnehmer - mittlere Höhe ihrer Zuflüsse.
            SpalteAusrichten(_modell.Spalte(SchemaModell.Knotenart.Abnehmer), 3, oben);

            foreach (KeyValuePair<string, Rectangle> f in _flaechen)
                if (f.Value.Bottom > unten) unten = f.Value.Bottom;

            _inhaltHoehe = unten;

            // 5. Kaskadenband und Legende darunter.
            _bandOben = _inhaltHoehe + BAND_ABSTAND;
            int bandHoehe = BandAnordnen(_bandOben);
            _legendeOben = _bandOben + bandHoehe + BAND_ABSTAND / 2;

            // PAKET E1: drei statt zwei Legendenzeilen reserviert. Die Legende bricht
            // selbst um (LegendeZeichnen); mit dem fünften Eintrag reicht ein schmales
            // Fenster für zwei Zeilen nicht mehr, und die dritte wäre abgeschnitten.
            int gesamt = _legendeOben + 3 * LEGENDE_ZEILE + RAND;
            AutoScrollMinSize = new Size(_inhaltBreite, gesamt);
            Invalidate();
        }

        /// <summary>Richtet eine Spalte an der mittleren Höhe der eingehenden Kanten aus.</summary>
        private void SpalteAusrichten(List<SchemaModell.Knoten> knoten, int spalte, int oben)
        {
            foreach (SchemaModell.Knoten k in knoten)
            {
                int summe = 0, anzahl = 0;
                foreach (SchemaModell.Kante e in _modell.Kantenliste)
                {
                    if (!string.Equals(e.Nach, k.Schluessel, StringComparison.Ordinal)) continue;

                    Rectangle von = FlaecheVon(e.Von);
                    if (von.IsEmpty) continue;
                    summe += von.Top + von.Height / 2;
                    anzahl++;
                }

                int h = KnotenHoehe(k);
                int mitte = anzahl > 0 ? summe / anzahl : oben + h / 2;
                _flaechen[k.Schluessel] =
                    new Rectangle(_spaltenX[spalte], Math.Max(oben, mitte - h / 2),
                                  SPALTEN_BREITE[spalte], h);
            }

            UeberschneidungenAufloesen(knoten, oben);
        }

        /// <summary>
        /// Schiebt Kästen einer Spalte so weit nach unten, dass sie sich nicht mehr
        /// überlappen — in der Reihenfolge ihrer berechneten Höhe.
        /// </summary>
        private void UeberschneidungenAufloesen(List<SchemaModell.Knoten> knoten, int oben)
        {
            List<SchemaModell.Knoten> sortiert = new List<SchemaModell.Knoten>(knoten);
            sortiert.Sort(delegate (SchemaModell.Knoten a, SchemaModell.Knoten b)
            {
                return FlaecheVon(a.Schluessel).Top.CompareTo(FlaecheVon(b.Schluessel).Top);
            });

            int grenze = oben;
            foreach (SchemaModell.Knoten k in sortiert)
            {
                Rectangle r = FlaecheVon(k.Schluessel);
                if (r.IsEmpty) continue;

                if (r.Top < grenze) r.Y = grenze;
                _flaechen[k.Schluessel] = r;
                grenze = r.Bottom + KNOTEN_ABSTAND;
            }
        }

        private int KnotenHoehe(SchemaModell.Knoten k)
        {
            int h = 2 * KNOTEN_RAND + TITEL_HOEHE;
            h += k.Zeilen.Count * ZEILE_HOEHE;
            if (k.Badges.Count > 0) h += BADGE_HOEHE + 3;
            if (k.Warnung) h += ZEILE_HOEHE;
            return h;
        }

        /// <summary>Legt die Pillen des Kaskadenbands ab; Rückgabe ist die belegte Höhe.</summary>
        private int BandAnordnen(int oben)
        {
            if (_modell.Ketten.Count == 0) return BAND_ZEILE;

            int y = oben + BAND_ZEILE - 6;   // eine Zeile für die Überschrift
            foreach (List<SchemaModell.Kettenglied> kette in _modell.Ketten)
            {
                int x = RAND;
                foreach (SchemaModell.Kettenglied g in kette)
                {
                    if (x > RAND) x += PFEIL_BREITE;

                    int breite = TextRenderer.MeasureText(g.Text, _kleinschrift).Width + 2 * PILLE_RAND;
                    if (x + breite > _inhaltBreite - RAND && x > RAND)
                    {
                        x = RAND + 24;
                        y += BAND_ZEILE;
                    }

                    _bandflaechen.Add(new KeyValuePair<string, Rectangle>(
                        g.Schluessel, new Rectangle(x, y, breite, BAND_ZEILE - 8)));
                    x += breite;
                }
                y += BAND_ZEILE;
            }

            return y - oben;
        }

        /// <summary>Scrollt einen Knoten in den sichtbaren Bereich.</summary>
        private void SichtbarMachen(string schluessel)
        {
            Rectangle r = FlaecheVon(schluessel);
            if (r.IsEmpty || !IsHandleCreated) return;

            Rectangle sicht = new Rectangle(-AutoScrollPosition.X, -AutoScrollPosition.Y,
                                            ClientSize.Width, ClientSize.Height);
            if (sicht.Contains(r)) return;

            AutoScrollPosition = new Point(Math.Max(0, r.Left - 40), Math.Max(0, r.Top - 40));
        }

        // --- Maus ---------------------------------------------------------------------

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            string treffer = Treffer(InModell(e.Location));
            if (string.Equals(treffer, _hinweisFuer, StringComparison.Ordinal)) return;
            _hinweisFuer = treffer;

            SchemaModell.Knoten k = _modell.Finden(treffer);
            Cursor = k != null ? Cursors.Hand : Cursors.Default;

            if (k == null) { _tip.SetToolTip(this, ""); return; }

            string text = k.Titel;
            // Hinweis/Warntext mischen Ressourcentexte (CRLF) und bereits mit
            // Environment.NewLine gebaute Zeilen — Normalisieren ist idempotent.
            if (!string.IsNullOrEmpty(k.Hinweis))
                text += Environment.NewLine + Zeilenumbruch.Normalisieren(k.Hinweis);
            if (k.Warnung && !string.IsNullOrEmpty(k.Warntext))
                text += Environment.NewLine + Zeilenumbruch.Normalisieren(k.Warntext);

            _tip.SetToolTip(this, text);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            string treffer = Treffer(InModell(e.Location));
            if (treffer.Length == 0) return;

            _auswahl = treffer;
            Invalidate();

            Action<string> ziel = Ausgewaehlt;
            if (ziel != null) ziel(treffer);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button != MouseButtons.Left) return;

            string treffer = Treffer(InModell(e.Location));
            if (treffer.Length == 0) return;

            // Verzögert melden - dieselbe Begründung wie in ErzeugerKarte.Melden: Der
            // Empfänger öffnet einen Editor und baut anschließend die Seite neu auf.
            Action<string> ziel = Bearbeiten;
            if (ziel == null) return;

            if (IsHandleCreated && !IsDisposed)
                BeginInvoke((MethodInvoker)delegate { ziel(treffer); });
            else
                ziel(treffer);
        }

        // --- Zeichnen -----------------------------------------------------------------

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

            if (_modell.IstLeer)
            {
                TextRenderer.DrawText(g, MyResource.Resource.SIM_SCHEMA_LEER, Font,
                                      new Point(RAND, RAND + KOPF_HOEHE), KartenStil.TEXT_LEISE);
                return;
            }

            SpaltenkoepfeZeichnen(g);

            foreach (SchemaModell.Kante k in _modell.Kantenliste) KanteZeichnen(g, k);
            foreach (SchemaModell.Knoten k in _modell.Knotenliste) KnotenZeichnen(g, k);

            BandZeichnen(g);
            LegendeZeichnen(g);
        }

        private void SpaltenkoepfeZeichnen(Graphics g)
        {
            string[] koepfe =
            {
                MyResource.Resource.SIM_SCHEMA_SPALTE_QUELLE,
                MyResource.Resource.SIM_SCHEMA_SPALTE_ERZEUGER,
                MyResource.Resource.SIM_SCHEMA_SPALTE_SPEICHER,
                MyResource.Resource.SIM_SCHEMA_SPALTE_ABNEHMER
            };

            for (int i = 0; i < koepfe.Length; i++)
                TextRenderer.DrawText(g, koepfe[i], _kleinschrift,
                                      new Rectangle(_spaltenX[i], RAND, SPALTEN_BREITE[i], KOPF_HOEHE),
                                      KartenStil.TEXT_LEISE,
                                      TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private Color KantenFarbe(SchemaModell.Kantenart art)
        {
            switch (art)
            {
                case SchemaModell.Kantenart.Ladung: return KartenStil.SENKE_RAHMEN;
                case SchemaModell.Kantenart.Versorgung: return FARBE_VERSORGUNG;
                case SchemaModell.Kantenart.Prozess: return FARBE_PROZESS;
                default: return KartenStil.QUELLE_RAHMEN;
            }
        }

        /// <summary>Grün der Versorgungsleitung (#1D9E75 aus dem Entwurf).</summary>
        private static readonly Color FARBE_VERSORGUNG = Color.FromArgb(29, 158, 117);

        /// <summary>
        /// PAKET E1 (Befund S2-O7): Violett der PROZESS-Versorgung (#7E57A6).
        ///
        /// <para>Die Farbwahl ist an den Bestand angelegt, nicht daneben gestellt: Die
        /// drei belegten Farbwinkel sind Blau (Quelle, #378ADD), Koralle (Ladung,
        /// #D85A30) und Grün (Versorgung, #1D9E75); Violett liegt zwischen Blau und
        /// Koralle und ist der einzige verbliebene Sektor mit deutlichem Abstand zu
        /// allen dreien. Sättigung und Helligkeit sind bewusst auf demselben gedämpften
        /// Niveau wie die Nachbarn — ein kräftiges Violett spränge aus dem Bild.
        /// AMBER (~40°) wäre der andere freie Sektor, ist im Kartenstil aber mit
        /// „Warnung" belegt (siehe <c>ErzeugerKarte</c>) und schiede damit aus.</para>
        /// </summary>
        private static readonly Color FARBE_PROZESS = Color.FromArgb(126, 87, 166);

        private void KanteZeichnen(Graphics g, SchemaModell.Kante kante)
        {
            Rectangle von = FlaecheVon(kante.Von);
            Rectangle nach = FlaecheVon(kante.Nach);
            if (von.IsEmpty || nach.IsEmpty) return;

            bool hervor = string.Equals(_auswahl, kante.Von, StringComparison.Ordinal) ||
                          string.Equals(_auswahl, kante.Nach, StringComparison.Ordinal);

            Point a, b, c1, c2;
            if (nach.Left >= von.Right)
            {
                // Vorwärts: rechte Kante -> linke Kante, waagerechte Kontrollpunkte.
                a = new Point(von.Right, von.Top + von.Height / 2);
                b = new Point(nach.Left, nach.Top + nach.Height / 2);
                int d = Math.Max(24, (b.X - a.X) / 2);
                c1 = new Point(a.X + d, a.Y);
                c2 = new Point(b.X - d, b.Y);
            }
            else
            {
                // Rückwärts (Kaskade): unter den Kästen herum, damit keine Linie durch
                // einen Kasten läuft.
                a = new Point(von.Left, von.Bottom);
                b = new Point(nach.Right, nach.Bottom);
                int tief = Math.Max(a.Y, b.Y) + 26;
                c1 = new Point(a.X - 30, tief);
                c2 = new Point(b.X + 30, tief);
            }

            using (Pen p = new Pen(KantenFarbe(kante.Art), hervor ? 2.6f : 1.8f))
            using (AdjustableArrowCap spitze = new AdjustableArrowCap(4.5f, 5f))
            {
                if (kante.Art == SchemaModell.Kantenart.Kaskade)
                    p.DashStyle = DashStyle.Dash;

                p.CustomEndCap = spitze;
                g.DrawBezier(p, a, c1, c2, b);
            }

            if (kante.Prioritaet <= 0) return;

            // Prioritätskreis auf der Kurvenmitte (Bezier bei t = 0,5).
            PointF mitte = BezierPunkt(a, c1, c2, b, 0.5f);
            Rectangle kreis = new Rectangle((int)mitte.X - PRIO_RADIUS, (int)mitte.Y - PRIO_RADIUS,
                                            2 * PRIO_RADIUS, 2 * PRIO_RADIUS);

            using (SolidBrush b1 = new SolidBrush(Color.White)) g.FillEllipse(b1, kreis);
            using (Pen p = new Pen(KartenStil.SENKE_RAHMEN)) g.DrawEllipse(p, kreis);

            TextRenderer.DrawText(g, kante.Prioritaet.ToString(), _kleinschrift, kreis,
                                  KartenStil.TEXT,
                                  TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private static PointF BezierPunkt(Point a, Point c1, Point c2, Point b, float t)
        {
            float u = 1 - t;
            float x = u * u * u * a.X + 3 * u * u * t * c1.X + 3 * u * t * t * c2.X + t * t * t * b.X;
            float y = u * u * u * a.Y + 3 * u * u * t * c1.Y + 3 * u * t * t * c2.Y + t * t * t * b.Y;
            return new PointF(x, y);
        }

        private void KnotenZeichnen(Graphics g, SchemaModell.Knoten k)
        {
            Rectangle r = FlaecheVon(k.Schluessel);
            if (r.IsEmpty) return;

            bool gewaehlt = string.Equals(_auswahl, k.Schluessel, StringComparison.Ordinal);

            Color rahmen;
            Color flaeche = Color.White;
            int ecke = KartenStil.ECKE;

            switch (k.Art)
            {
                case SchemaModell.Knotenart.Quelle:
                    rahmen = k.ID_Type == ProjektPuffer.TYP_WP
                        ? KartenStil.QUELLE_RAHMEN : KartenStil.RAHMEN_LEISE;
                    flaeche = KartenStil.FLAECHE;
                    ecke = 4;
                    break;

                case SchemaModell.Knotenart.Speicher:
                    rahmen = KartenStil.RAHMEN_SPEICHER;
                    break;

                case SchemaModell.Knotenart.Abnehmer:
                    rahmen = KartenStil.RAHMEN;
                    flaeche = KartenStil.FLAECHE;
                    ecke = r.Height / 2;
                    break;

                default:
                    rahmen = k.Kaskade ? KartenStil.QUELLE_RAHMEN : KartenStil.RAHMEN;
                    break;
            }

            if (k.Warnung) rahmen = KartenStil.WARN_RAHMEN;

            using (GraphicsPath p = KartenStil.Rundeck(r, ecke))
            {
                using (SolidBrush b = new SolidBrush(flaeche)) g.FillPath(b, p);

                if (gewaehlt)
                    using (Pen halo = new Pen(Color.FromArgb(70, KartenStil.QUELLE_RAHMEN), 5f))
                        g.DrawPath(halo, p);

                using (Pen stift = new Pen(rahmen, gewaehlt ? 2f : 1f))
                {
                    if (k.Kaskade && k.Art == SchemaModell.Knotenart.Quelle)
                        stift.DashStyle = DashStyle.Dash;
                    g.DrawPath(stift, p);
                }
            }

            int y = r.Top + KNOTEN_RAND;
            int innenBreite = r.Width - 2 * KNOTEN_RAND;

            if (k.Rang.Length > 0)
            {
                Rectangle rang = new Rectangle(r.Left + KNOTEN_RAND, y, 14, TITEL_HOEHE);
                TextRenderer.DrawText(g, k.Rang, _fettschrift, rang, KartenStil.TEXT_LEISE,
                                      TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }

            int titelLinks = r.Left + KNOTEN_RAND + (k.Rang.Length > 0 ? 16 : 0);
            TextRenderer.DrawText(g, k.Titel, _fettschrift,
                                  new Rectangle(titelLinks, y, r.Right - KNOTEN_RAND - titelLinks, TITEL_HOEHE),
                                  KartenStil.TEXT,
                                  TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                                  TextFormatFlags.EndEllipsis);
            y += TITEL_HOEHE;

            foreach (string zeile in k.Zeilen)
            {
                TextRenderer.DrawText(g, zeile, _kleinschrift,
                                      new Rectangle(r.Left + KNOTEN_RAND, y, innenBreite, ZEILE_HOEHE),
                                      KartenStil.TEXT_LEISE,
                                      TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                                      TextFormatFlags.EndEllipsis);
                y += ZEILE_HOEHE;
            }

            if (k.Badges.Count > 0)
            {
                int x = r.Left + KNOTEN_RAND;
                foreach (string badge in k.Badges)
                {
                    int w = TextRenderer.MeasureText(badge, _kleinschrift).Width + 12;
                    if (x + w > r.Right - KNOTEN_RAND) break;

                    Rectangle rb = new Rectangle(x, y, w, BADGE_HOEHE);
                    using (GraphicsPath p = KartenStil.Rundeck(rb, BADGE_HOEHE / 2))
                    using (SolidBrush b = new SolidBrush(KartenStil.BADGE_FLAECHE))
                        g.FillPath(b, p);

                    TextRenderer.DrawText(g, badge, _kleinschrift, rb, KartenStil.BADGE_TEXT,
                                          TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    x += w + 4;
                }
                y += BADGE_HOEHE + 3;
            }

            if (!k.Warnung) return;

            // Die amber-Warnung der Kartenansicht, hier am Schema-Element (Aufgabe D4-3).
            Rectangle warn = new Rectangle(r.Left + KNOTEN_RAND, y, innenBreite, ZEILE_HOEHE);
            using (SolidBrush b = new SolidBrush(KartenStil.WARN_FLAECHE)) g.FillRectangle(b, warn);
            TextRenderer.DrawText(g, MyResource.Resource.SIM_SCHEMA_WARNUNG, _kleinschrift, warn,
                                  KartenStil.WARN_TEXT,
                                  TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                                  TextFormatFlags.EndEllipsis);
        }

        private void BandZeichnen(Graphics g)
        {
            TextRenderer.DrawText(g, MyResource.Resource.SIM_SCHEMA_KETTE_KOPF, _fettschrift,
                                  new Point(RAND, _bandOben), KartenStil.TEXT);

            if (_modell.Ketten.Count == 0)
            {
                TextRenderer.DrawText(g, MyResource.Resource.SIM_SCHEMA_KEINE_KETTE, _kleinschrift,
                                      new Point(RAND, _bandOben + 16), KartenStil.TEXT_LEISE);
                return;
            }

            int index = 0;
            foreach (List<SchemaModell.Kettenglied> kette in _modell.Ketten)
            {
                for (int i = 0; i < kette.Count && index < _bandflaechen.Count; i++, index++)
                {
                    SchemaModell.Kettenglied glied = kette[i];
                    Rectangle r = _bandflaechen[index].Value;

                    if (i > 0)
                    {
                        Rectangle vorher = _bandflaechen[index - 1].Value;
                        if (vorher.Top == r.Top)
                            PfeilZeichnen(g, vorher.Right + 3, r.Left - 3, r.Top + r.Height / 2,
                                          KantenFarbe(glied.PfeilDavor));
                    }

                    bool gewaehlt = string.Equals(_auswahl, glied.Schluessel, StringComparison.Ordinal);
                    Color rahmen = glied.Art == SchemaModell.Knotenart.Speicher
                        ? KartenStil.RAHMEN_SPEICHER
                        : glied.Art == SchemaModell.Knotenart.Quelle
                            ? KartenStil.QUELLE_RAHMEN
                            : KartenStil.RAHMEN;

                    using (GraphicsPath p = KartenStil.Rundeck(r, r.Height / 2))
                    {
                        using (SolidBrush b = new SolidBrush(Color.White)) g.FillPath(b, p);
                        using (Pen stift = new Pen(rahmen, gewaehlt ? 2f : 1f)) g.DrawPath(stift, p);
                    }

                    TextRenderer.DrawText(g, glied.Text, _kleinschrift, r, KartenStil.TEXT,
                                          TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
        }

        private void PfeilZeichnen(Graphics g, int von, int nach, int y, Color farbe)
        {
            if (nach <= von) return;

            using (Pen p = new Pen(farbe, 1.8f))
            using (AdjustableArrowCap spitze = new AdjustableArrowCap(3.5f, 4f))
            {
                p.CustomEndCap = spitze;
                g.DrawLine(p, von, y, nach, y);
            }
        }

        private void LegendeZeichnen(Graphics g)
        {
            // PAKET E1 (Befund S2-O7): fünfter Eintrag für die Prozessversorgung. Die
            // drei Felder sind index-gekoppelt; die Strichelung hängt seither an einem
            // eigenen Feld statt an der hart kodierten Position „i == 3" — ein
            // eingeschobener Eintrag hätte sie sonst auf die falsche Zeile geschoben.
            string[] texte =
            {
                MyResource.Resource.SIM_SCHEMA_LEGENDE_LADUNG,
                MyResource.Resource.SIM_SCHEMA_LEGENDE_VERSORGUNG,
                MyResource.Resource.SIM_SCHEMA_LEGENDE_PROZESS,
                MyResource.Resource.SIM_SCHEMA_LEGENDE_QUELLE,
                MyResource.Resource.SIM_SCHEMA_LEGENDE_KASKADE
            };
            Color[] farben =
            {
                KartenStil.SENKE_RAHMEN, FARBE_VERSORGUNG, FARBE_PROZESS,
                KartenStil.QUELLE_RAHMEN, KartenStil.QUELLE_RAHMEN
            };
            bool[] gestrichelt = { false, false, false, false, true };

            int x = RAND;
            int y = _legendeOben;

            for (int i = 0; i < texte.Length; i++)
            {
                int breite = TextRenderer.MeasureText(texte[i], _kleinschrift).Width;
                if (x + breite + 40 > _inhaltBreite - RAND && x > RAND)
                {
                    x = RAND;
                    y += LEGENDE_ZEILE;
                }

                using (Pen p = new Pen(farben[i], 2f))
                {
                    if (gestrichelt[i]) p.DashStyle = DashStyle.Dash;
                    g.DrawLine(p, x, y + LEGENDE_ZEILE / 2, x + 26, y + LEGENDE_ZEILE / 2);
                }

                TextRenderer.DrawText(g, texte[i], _kleinschrift,
                                      new Rectangle(x + 32, y, breite + 4, LEGENDE_ZEILE),
                                      KartenStil.TEXT_LEISE,
                                      TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                x += breite + 52;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tip.Dispose();
                if (_kleinschrift != null) _kleinschrift.Dispose();
                if (_fettschrift != null) _fettschrift.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

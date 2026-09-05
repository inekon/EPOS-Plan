using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// iU9-W10b.0a — die ANORDNUNG des Hydraulikschemas: Wo steht welcher Kasten, wie
    /// laeuft welche Leitung, wo sitzt das Kaskadenband, wo die Legende.
    ///
    /// <para><b>Warum im Kern.</b> Bis W10b stand diese Rechnung in
    /// <c>Views/Simulation/SchemaAnsicht.cs</c> (:203-351) und war damit an GDI+ und an
    /// ein <c>Panel</c> gebunden - pruefbar nur ueber Pixel. <see cref="SchemaModell"/>
    /// war schon oberflaechenfrei („was gezeichnet wird"); mit dieser Klasse ist es die
    /// Anordnung ebenso („wo es steht"). Uebrig bleibt fuer die Oberflaeche das reine
    /// Malen - unter Windows und auf iOS derselbe SVG-Baustein
    /// (<c>EPOS.UI/Bausteine/Schema.razor</c>).</para>
    ///
    /// <para><b>Die Regeln sind woertlich uebernommen.</b> Spaltenbreiten, Abstaende,
    /// Knotenhoehen, die vier Ausrichtungsschritte, die Aufloesung von
    /// Ueberschneidungen, der Bogen der Rueckwaertskante und der Prioritaetspunkt bei
    /// t = 0,5 stehen hier so, wie sie in <c>SchemaAnsicht</c> standen.</para>
    ///
    /// <para><b>Eine Abweichung: die Textbreite.</b> Der Vorlaeufer maß sie mit
    /// <c>TextRenderer.MeasureText</c>, also mit GDI+. Ohne Oberflaeche gibt es keine
    /// Messung; die Breite einer Bandpille und eines Legendeneintrags wird deshalb aus
    /// der Zeichenzahl geschaetzt (<see cref="ZEICHEN_BREITE"/>). Das betrifft
    /// ausschliesslich den Umbruch von Band und Legende - Knoten und Kanten sind
    /// zeichenunabhaengig. Im SVG traegt die Pille ihren Text zentriert; eine
    /// Schaetzung daneben verschiebt nichts, sie macht die Pille nur etwas breiter oder
    /// schmaler.</para>
    /// </summary>
    public sealed class SchemaLayout
    {
        // --- Masse (woertlich aus SchemaAnsicht.cs:39-59) -----------------------------

        /// <summary>Aussenrand der Zeichenflaeche [px].</summary>
        public const int RAND = 18;

        /// <summary>Waagerechter Abstand zwischen zwei Spalten [px].</summary>
        public const int SPALTE_ABSTAND = 56;

        /// <summary>Senkrechter Mindestabstand zweier Kaesten derselben Spalte [px].</summary>
        public const int KNOTEN_ABSTAND = 14;

        /// <summary>Hoehe der Spaltenkopfzeile [px].</summary>
        public const int KOPF_HOEHE = 26;

        /// <summary>Breite der vier Spalten: Quelle, Erzeuger, Speicher, Abnehmer [px].</summary>
        public static readonly int[] SPALTEN_BREITE = { 150, 214, 190, 132 };

        public const int ZEILE_HOEHE = 15;
        public const int TITEL_HOEHE = 19;
        public const int BADGE_HOEHE = 17;
        public const int KNOTEN_RAND = 8;

        public const int BAND_ABSTAND = 26;
        public const int BAND_ZEILE = 30;
        public const int PILLE_RAND = 10;
        public const int PFEIL_BREITE = 18;

        public const int LEGENDE_ZEILE = 20;

        /// <summary>Radius des Prioritaetskreises an einer Ladeleitung [px].</summary>
        public const int PRIO_RADIUS = 9;

        /// <summary>
        /// Geschaetzte Breite EINES Zeichens der Kleinschrift [px] — der Ersatz fuer
        /// <c>TextRenderer.MeasureText</c> (siehe Klassenkopf). 6 px entspricht der
        /// mittleren Zeichenbreite der Standardschrift bei 8 pt, mit der der Vorlaeufer
        /// gemessen hat.
        /// </summary>
        public const double ZEICHEN_BREITE = 6.0;

        // --- Bausteine der Anordnung --------------------------------------------------

        /// <summary>Ein Punkt in Modellkoordinaten.</summary>
        public struct Punkt
        {
            public int X;
            public int Y;

            public Punkt(int x, int y) { X = x; Y = y; }
        }

        /// <summary>Ein Rechteck in Modellkoordinaten.</summary>
        public struct Rechteck
        {
            public int X;
            public int Y;
            public int Breite;
            public int Hoehe;

            public Rechteck(int x, int y, int breite, int hoehe)
            {
                X = x; Y = y; Breite = breite; Hoehe = hoehe;
            }

            public int Rechts { get { return X + Breite; } }
            public int Unten { get { return Y + Hoehe; } }
            public int MitteY { get { return Y + Hoehe / 2; } }

            /// <summary>Ein Rechteck ohne Ausdehnung — „gibt es nicht".</summary>
            public bool IstLeer { get { return Breite <= 0 && Hoehe <= 0; } }

            public bool Enthaelt(int x, int y)
            {
                return x >= X && x < Rechts && y >= Y && y < Unten;
            }
        }

        /// <summary>Ein angeordneter Kasten: Knoten des Modells samt seiner Flaeche.</summary>
        public sealed class Knotenflaeche
        {
            public SchemaModell.Knoten Knoten;
            public Rechteck Flaeche;

            public string Schluessel
            {
                get { return Knoten != null ? Knoten.Schluessel : ""; }
            }
        }

        /// <summary>
        /// Eine angeordnete Leitung: die vier Punkte ihres Bezier-Bogens, die Stelle
        /// des Prioritaetskreises und die Richtung.
        /// </summary>
        public sealed class Kantenzug
        {
            public SchemaModell.Kante Kante;

            public Punkt A;
            public Punkt C1;
            public Punkt C2;
            public Punkt B;

            /// <summary>Mitte der Kurve (t = 0,5) — dort sitzt der Prioritaetskreis.</summary>
            public Punkt Mitte;

            /// <summary>true = Rueckwaertskante (Kaskade), unter den Kaesten herum.</summary>
            public bool Rueckwaerts;

            public SchemaModell.Kantenart Art
            {
                get { return Kante != null ? Kante.Art : SchemaModell.Kantenart.Quelle; }
            }

            public int Prioritaet
            {
                get { return Kante != null ? Kante.Prioritaet : 0; }
            }
        }

        /// <summary>Ein Glied des Kaskadenbands samt seiner Pillenflaeche.</summary>
        public sealed class Bandflaeche
        {
            public SchemaModell.Kettenglied Glied;
            public Rechteck Flaeche;

            /// <summary>true = erstes Glied einer Kette (davor steht kein Pfeil).</summary>
            public bool Kettenanfang;

            public string Schluessel
            {
                get { return Glied != null ? Glied.Schluessel : ""; }
            }
        }

        // --- Ergebnis -----------------------------------------------------------------

        /// <summary>Das angeordnete Modell; nie <c>null</c>.</summary>
        public SchemaModell Modell { get; private set; }

        /// <summary>Die Kaesten in der Reihenfolge des Modells.</summary>
        public List<Knotenflaeche> Knoten { get; private set; }

        /// <summary>Die Leitungen; Kanten ohne angeordnete Enden fehlen.</summary>
        public List<Kantenzug> Kanten { get; private set; }

        /// <summary>Die Pillen des Kaskadenbands in Zeichenreihenfolge.</summary>
        public List<Bandflaeche> Band { get; private set; }

        /// <summary>Linke Kante der vier Spalten [px].</summary>
        public int[] SpaltenX { get; private set; }

        /// <summary>Breite des Inhalts [px].</summary>
        public int InhaltBreite { get; private set; }

        /// <summary>Unterkante des Kastenbereichs [px].</summary>
        public int InhaltHoehe { get; private set; }

        /// <summary>Oberkante des Kaskadenbands [px].</summary>
        public int BandOben { get; private set; }

        /// <summary>Oberkante der Legende [px].</summary>
        public int LegendeOben { get; private set; }

        /// <summary>Gesamthoehe der Zeichenflaeche [px].</summary>
        public int Gesamthoehe { get; private set; }

        /// <summary>true = das Modell hat weder Erzeuger noch Speicher (Leerbild).</summary>
        public bool IstLeer
        {
            get { return Modell == null || Modell.IstLeer; }
        }

        private readonly Dictionary<string, Rechteck> _flaechen =
            new Dictionary<string, Rechteck>(StringComparer.Ordinal);

        private SchemaLayout()
        {
            Modell = new SchemaModell();
            Knoten = new List<Knotenflaeche>();
            Kanten = new List<Kantenzug>();
            Band = new List<Bandflaeche>();
            SpaltenX = new int[4];
        }

        /// <summary>Flaeche eines Knotens; leeres Rechteck = unbekannt.</summary>
        public Rechteck FlaecheVon(string schluessel)
        {
            Rechteck r;
            if (schluessel != null && _flaechen.TryGetValue(schluessel, out r)) return r;
            return new Rechteck(0, 0, 0, 0);
        }

        /// <summary>
        /// Knoten- oder Bandglied an einer Stelle in Modellkoordinaten; "" = kein
        /// Treffer (woertlich <c>SchemaAnsicht.Treffer</c>:162-171).
        /// </summary>
        public string Treffer(int x, int y)
        {
            foreach (Knotenflaeche k in Knoten)
                if (k.Flaeche.Enthaelt(x, y)) return k.Schluessel;

            foreach (Bandflaeche b in Band)
                if (b.Flaeche.Enthaelt(x, y)) return b.Schluessel;

            return "";
        }

        // --- Die Anordnung ------------------------------------------------------------

        /// <summary>
        /// Ordnet ein Modell an.
        ///
        /// <para><b>Der Ablauf</b> (woertlich <c>SchemaAnsicht.Neuordnen</c>:203-262):
        /// Erst die Erzeugerspalte von oben nach unten — sie gibt die senkrechte Ordnung
        /// vor (Kaskadenreihenfolge). Dann die Quellen auf die Hoehe ihres Erzeugers,
        /// dann die Speicher auf die MITTLERE Hoehe ihrer Lader und die Abnehmer auf die
        /// mittlere Hoehe ihrer Zufluesse; Ueberschneidungen werden anschliessend nach
        /// unten aufgeloest. Zuletzt Kaskadenband und Legende.</para>
        /// </summary>
        /// <param name="modell">Das Zeichenmodell; <c>null</c> = leeres Modell.</param>
        /// <param name="breite">
        /// Verfuegbare Breite [px]. <c>0</c> oder kleiner = die Spaltenbreite selbst —
        /// genau der Zustand des Vorlaeufers, der die Fensterbreite nie in die
        /// Anordnung einbezog.
        /// </param>
        public static SchemaLayout Anordnen(SchemaModell modell, int breite)
        {
            SchemaLayout l = new SchemaLayout();
            l.Modell = modell ?? new SchemaModell();
            l.Rechnen(breite);
            return l;
        }

        private void Rechnen(int breite)
        {
            int x = RAND;
            for (int i = 0; i < 4; i++)
            {
                SpaltenX[i] = x;
                x += SPALTEN_BREITE[i] + SPALTE_ABSTAND;
            }
            int spaltenBreite = x - SPALTE_ABSTAND + RAND;
            InhaltBreite = breite > spaltenBreite ? breite : spaltenBreite;

            int oben = RAND + KOPF_HOEHE;
            int unten = oben;

            // 1. Erzeuger — von oben nach unten in Kaskadenreihenfolge.
            int y = oben;
            foreach (SchemaModell.Knoten k in Modell.Spalte(SchemaModell.Knotenart.Erzeuger))
            {
                int h = KnotenHoehe(k);
                _flaechen[k.Schluessel] = new Rechteck(SpaltenX[1], y, SPALTEN_BREITE[1], h);
                y += h + KNOTEN_ABSTAND;
            }
            if (y > unten) unten = y;

            // 2. Quellen — je Erzeuger genau eine, also auf dessen Hoehe.
            foreach (SchemaModell.Knoten k in Modell.Spalte(SchemaModell.Knotenart.Quelle))
            {
                int h = KnotenHoehe(k);
                Rechteck erz = FlaecheVon(SchemaModell.PRAEFIX_ERZEUGER + k.ID);
                int mitte = erz.IstLeer ? oben + h / 2 : erz.MitteY;
                _flaechen[k.Schluessel] =
                    new Rechteck(SpaltenX[0], Math.Max(oben, mitte - h / 2), SPALTEN_BREITE[0], h);
            }
            UeberschneidungenAufloesen(Modell.Spalte(SchemaModell.Knotenart.Quelle), oben);

            // 3. Speicher — mittlere Hoehe ihrer Lader.
            SpalteAusrichten(Modell.Spalte(SchemaModell.Knotenart.Speicher), 2, oben);

            // 4. Abnehmer — mittlere Hoehe ihrer Zufluesse.
            SpalteAusrichten(Modell.Spalte(SchemaModell.Knotenart.Abnehmer), 3, oben);

            foreach (KeyValuePair<string, Rechteck> f in _flaechen)
                if (f.Value.Unten > unten) unten = f.Value.Unten;

            InhaltHoehe = unten;

            // Die Kaesten in Modellreihenfolge — so wird auch gezeichnet.
            foreach (SchemaModell.Knoten k in Modell.Knotenliste)
            {
                Rechteck r = FlaecheVon(k.Schluessel);
                if (r.IstLeer) continue;
                Knoten.Add(new Knotenflaeche { Knoten = k, Flaeche = r });
            }

            KantenLegen();

            // 5. Kaskadenband und Legende darunter.
            BandOben = InhaltHoehe + BAND_ABSTAND;
            int bandHoehe = BandAnordnen(BandOben);
            LegendeOben = BandOben + bandHoehe + BAND_ABSTAND / 2;

            // PAKET E1: drei statt zwei Legendenzeilen reserviert (SchemaAnsicht:256-259).
            Gesamthoehe = LegendeOben + 3 * LEGENDE_ZEILE + RAND;
        }

        /// <summary>Richtet eine Spalte an der mittleren Hoehe der eingehenden Kanten aus.</summary>
        private void SpalteAusrichten(List<SchemaModell.Knoten> knoten, int spalte, int oben)
        {
            foreach (SchemaModell.Knoten k in knoten)
            {
                int summe = 0, anzahl = 0;
                foreach (SchemaModell.Kante e in Modell.Kantenliste)
                {
                    if (!string.Equals(e.Nach, k.Schluessel, StringComparison.Ordinal)) continue;

                    Rechteck von = FlaecheVon(e.Von);
                    if (von.IstLeer) continue;
                    summe += von.MitteY;
                    anzahl++;
                }

                int h = KnotenHoehe(k);
                int mitte = anzahl > 0 ? summe / anzahl : oben + h / 2;
                _flaechen[k.Schluessel] =
                    new Rechteck(SpaltenX[spalte], Math.Max(oben, mitte - h / 2),
                                 SPALTEN_BREITE[spalte], h);
            }

            UeberschneidungenAufloesen(knoten, oben);
        }

        /// <summary>
        /// Schiebt Kaesten einer Spalte so weit nach unten, dass sie sich nicht mehr
        /// ueberlappen — in der Reihenfolge ihrer berechneten Hoehe.
        /// </summary>
        private void UeberschneidungenAufloesen(List<SchemaModell.Knoten> knoten, int oben)
        {
            List<SchemaModell.Knoten> sortiert = new List<SchemaModell.Knoten>(knoten);
            sortiert.Sort(delegate (SchemaModell.Knoten a, SchemaModell.Knoten b)
            {
                return FlaecheVon(a.Schluessel).Y.CompareTo(FlaecheVon(b.Schluessel).Y);
            });

            int grenze = oben;
            foreach (SchemaModell.Knoten k in sortiert)
            {
                Rechteck r = FlaecheVon(k.Schluessel);
                if (r.IstLeer) continue;

                if (r.Y < grenze) r.Y = grenze;
                _flaechen[k.Schluessel] = r;
                grenze = r.Unten + KNOTEN_ABSTAND;
            }
        }

        /// <summary>
        /// Hoehe eines Kastens (woertlich <c>SchemaAnsicht.KnotenHoehe</c>:314-321):
        /// zwei Raender, die Titelzeile, je Zusatzzeile eine Zeilenhoehe, bei Badges
        /// deren Hoehe plus 3, bei einer Warnung eine weitere Zeile.
        /// </summary>
        public static int KnotenHoehe(SchemaModell.Knoten k)
        {
            if (k == null) return 2 * KNOTEN_RAND + TITEL_HOEHE;

            int h = 2 * KNOTEN_RAND + TITEL_HOEHE;
            h += k.Zeilen.Count * ZEILE_HOEHE;
            if (k.Badges.Count > 0) h += BADGE_HOEHE + 3;
            if (k.Warnung) h += ZEILE_HOEHE;
            return h;
        }

        /// <summary>
        /// Legt die Bogenpunkte aller Kanten (woertlich
        /// <c>SchemaAnsicht.KanteZeichnen</c>:497-525 und <c>BezierPunkt</c>:552-558).
        /// </summary>
        private void KantenLegen()
        {
            foreach (SchemaModell.Kante kante in Modell.Kantenliste)
            {
                Rechteck von = FlaecheVon(kante.Von);
                Rechteck nach = FlaecheVon(kante.Nach);
                if (von.IstLeer || nach.IstLeer) continue;

                Punkt a, b, c1, c2;
                bool rueckwaerts = nach.X < von.Rechts;

                if (!rueckwaerts)
                {
                    // Vorwaerts: rechte Kante -> linke Kante, waagerechte Kontrollpunkte.
                    a = new Punkt(von.Rechts, von.MitteY);
                    b = new Punkt(nach.X, nach.MitteY);
                    int d = Math.Max(24, (b.X - a.X) / 2);
                    c1 = new Punkt(a.X + d, a.Y);
                    c2 = new Punkt(b.X - d, b.Y);
                }
                else
                {
                    // Rueckwaerts (Kaskade): unter den Kaesten herum, damit keine Linie
                    // durch einen Kasten laeuft.
                    a = new Punkt(von.X, von.Unten);
                    b = new Punkt(nach.Rechts, nach.Unten);
                    int tief = Math.Max(a.Y, b.Y) + 26;
                    c1 = new Punkt(a.X - 30, tief);
                    c2 = new Punkt(b.X + 30, tief);
                }

                Kanten.Add(new Kantenzug
                {
                    Kante = kante,
                    A = a,
                    C1 = c1,
                    C2 = c2,
                    B = b,
                    Mitte = BezierPunkt(a, c1, c2, b, 0.5),
                    Rueckwaerts = rueckwaerts
                });
            }
        }

        /// <summary>Punkt auf einer kubischen Bezier-Kurve (gerundet).</summary>
        public static Punkt BezierPunkt(Punkt a, Punkt c1, Punkt c2, Punkt b, double t)
        {
            double u = 1 - t;
            double x = u * u * u * a.X + 3 * u * u * t * c1.X + 3 * u * t * t * c2.X + t * t * t * b.X;
            double y = u * u * u * a.Y + 3 * u * u * t * c1.Y + 3 * u * t * t * c2.Y + t * t * t * b.Y;
            return new Punkt((int)x, (int)y);
        }

        /// <summary>
        /// Legt die Pillen des Kaskadenbands ab; Rueckgabe ist die belegte Hoehe
        /// (woertlich <c>SchemaAnsicht.BandAnordnen</c>:324-351).
        /// </summary>
        private int BandAnordnen(int oben)
        {
            if (Modell.Ketten.Count == 0) return BAND_ZEILE;

            int y = oben + BAND_ZEILE - 6;   // eine Zeile fuer die Ueberschrift
            foreach (List<SchemaModell.Kettenglied> kette in Modell.Ketten)
            {
                int x = RAND;
                bool erstes = true;

                foreach (SchemaModell.Kettenglied g in kette)
                {
                    if (x > RAND) x += PFEIL_BREITE;

                    int breite = TextBreite(g.Text) + 2 * PILLE_RAND;
                    if (x + breite > InhaltBreite - RAND && x > RAND)
                    {
                        x = RAND + 24;
                        y += BAND_ZEILE;
                    }

                    Band.Add(new Bandflaeche
                    {
                        Glied = g,
                        Flaeche = new Rechteck(x, y, breite, BAND_ZEILE - 8),
                        Kettenanfang = erstes
                    });

                    x += breite;
                    erstes = false;
                }
                y += BAND_ZEILE;
            }

            return y - oben;
        }

        /// <summary>
        /// Geschaetzte Textbreite [px] — der oberflaechenfreie Ersatz fuer
        /// <c>TextRenderer.MeasureText</c> (Begruendung im Klassenkopf).
        /// </summary>
        public static int TextBreite(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return (int)Math.Ceiling(text.Length * ZEICHEN_BREITE);
        }
    }
}

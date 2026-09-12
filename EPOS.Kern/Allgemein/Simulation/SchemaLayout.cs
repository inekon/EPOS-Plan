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
    /// <para><b>Die Regeln der Kaesten sind woertlich uebernommen.</b> Spaltenbreiten,
    /// Abstaende, Knotenhoehen, die vier Ausrichtungsschritte und die Aufloesung von
    /// Ueberschneidungen stehen hier so, wie sie in <c>SchemaAnsicht</c> standen.</para>
    ///
    /// <para><b>Die Leitungen NICHT — Anwenderbefund W10b-B-1 (05.09.2026).</b> Der
    /// Vorlaeufer zog jede Leitung als kubischen Bezierbogen von Kastenrand zu
    /// Kastenrand und schickte die Rueckwaertskante 26 px unter ihre beiden Endkaesten.
    /// Beides lief im Bild MITTEN DURCH einen Kasten, sobald eine Kante eine Spalte
    /// uebersprang (Erzeuger → Abnehmer) oder ein dritter Kasten tiefer stand als die
    /// zwei Enden. An die Stelle tritt eine Wegfuehrung in SPALTENBAHNEN: waagerecht aus
    /// dem Kasten, senkrecht in der Gasse zwischen zwei Spalten, waagerecht in den
    /// Zielkasten (<see cref="Kantenzug"/>). Der Prioritaetskreis sitzt seither bei
    /// halber WEGLAENGE statt bei t = 0,5 einer Kurve — bei einem Streckenzug ist das
    /// dieselbe Aussage, nur ohne Bezier.</para>
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

        // --- Linienbreite (Anwenderbefund W10b-B-1, 05.09.2026) -----------------------
        //
        // Eine Leitung ist eine LINIE GLEICHMAESSIGER BREITE. Sie bemisst sich an
        // NICHTS aus der Fachrechnung - nicht an einer Leistung, nicht an einem
        // Volumen, nicht an einer Ladeposition. Das Schema sagt aus, WER MIT WEM
        // verbunden ist; eine breitenkodierte Menge wuerde eine Genauigkeit
        // behaupten, die das Bild nicht hat (die Kaesten stehen in festen Spalten,
        // nicht massstaeblich). Wer eine Menge sehen will, liest die Kachel.
        //
        // Die zwei Werte sind trotzdem GEDECKELT hinterlegt, damit eine spaetere
        // Erweiterung nicht wieder ins Uferlose laeuft: Was hier steht, muss
        // zwischen MIN und MAX liegen, und SchemaLayoutTests prueft das nach.

        /// <summary>Untergrenze jeder Leitungsbreite [px].</summary>
        public const int LINIE_BREITE_MIN = 2;

        /// <summary>Obergrenze jeder Leitungsbreite [px].</summary>
        public const int LINIE_BREITE_MAX = 6;

        /// <summary>Breite einer Leitung [px] - fuer JEDE Kantenart dieselbe.</summary>
        public const int LINIE_BREITE = 2;

        /// <summary>Breite einer Leitung am GEWAEHLTEN Kasten [px].</summary>
        public const int LINIE_BREITE_HERVOR = 3;

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
        /// Eine angeordnete Leitung: ihr WEGZUG aus lauter waagerechten und senkrechten
        /// Stuecken, die Stelle des Prioritaetskreises und die Richtung.
        ///
        /// <para><b>Anwenderbefund W10b-B-1 (05.09.2026).</b> Bis hierher war der Weg ein
        /// kubischer Bezierbogen von Kastenrand zu Kastenrand mit waagerechten
        /// Kontrollpunkten. Ueber ZWEI Spalten hinweg — Erzeuger → Abnehmer, wenn eine
        /// Anlage den Heizkreis unmittelbar deckt — lief der Bogen quer durch die
        /// Speicherspalte; die Rueckwaertskante der Kaskade tauchte 26 px unter ihre
        /// beiden Endkaesten und schnitt dabei jeden Kasten, der tiefer stand. Beides
        /// war im Bild eine Leitung MITTEN DURCH einen Kasten.</para>
        ///
        /// <para><b>Die Regel jetzt: Spaltenbahnen.</b> Waagerecht aus dem Kasten
        /// heraus, senkrecht in der GASSE zwischen zwei Spalten, waagerecht in den
        /// Zielkasten hinein. Ueberspringt eine Kante eine Spalte, wechselt sie in der
        /// ersten Gasse auf eine FREIE BAHN — eine Hoehe, in der in den uebersprungenen
        /// Spalten kein Kasten steht — quert dort und faellt in der letzten Gasse wieder
        /// auf die Zielhoehe. Zwischen zwei Spalten liegen <see cref="SPALTE_ABSTAND"/>
        /// px ohne jeden Kasten; eine Senkrechte dort kann keinen kreuzen. Nachgeprueft
        /// wird das in <c>SchemaLayoutTests</c> Kasten fuer Kasten.</para>
        /// </summary>
        public sealed class Kantenzug
        {
            public SchemaModell.Kante Kante;

            /// <summary>Die Stuetzpunkte des Wegzugs, mindestens zwei.</summary>
            public List<Punkt> Punkte = new List<Punkt>();

            /// <summary>Anfang am Startkasten.</summary>
            public Punkt A
            {
                get { return Punkte.Count > 0 ? Punkte[0] : new Punkt(0, 0); }
            }

            /// <summary>Ende am Zielkasten — dort sitzt die EINE Pfeilspitze.</summary>
            public Punkt B
            {
                get { return Punkte.Count > 0 ? Punkte[Punkte.Count - 1] : new Punkt(0, 0); }
            }

            /// <summary>Mitte des Wegzugs (halbe Weglaenge) — dort sitzt der Prioritaetskreis.</summary>
            public Punkt Mitte;

            /// <summary>true = die Kante laeuft nach LINKS (Kaskade: Speicher → Erzeuger).</summary>
            public bool Rueckwaerts;

            /// <summary>Breite der Linie [px]; immer <see cref="LINIE_BREITE"/>.</summary>
            public int Breite = LINIE_BREITE;

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

        // --- Die Wegfuehrung der Leitungen (Anwenderbefund W10b-B-1) -------------------

        /// <summary>
        /// Der Vorsatz einer Kante, bevor ihre Gassen belegt sind: Welche Kaesten, welche
        /// Spalten, welche Gassen, welche Richtung.
        /// </summary>
        private sealed class Wegvorsatz
        {
            public SchemaModell.Kante Kante;
            public Rechteck Von;
            public Rechteck Nach;
            public int SpalteVon;
            public int SpalteNach;
            public bool Rueckwaerts;

            /// <summary>Gasse unmittelbar am Startkasten (Kennung 0..2).</summary>
            public int GasseA;

            /// <summary>Gasse unmittelbar am Zielkasten (Kennung 0..2).</summary>
            public int GasseB;

            /// <summary>Hoehe, auf der die Leitung den Startkasten verlaesst.</summary>
            public int YA;

            /// <summary>Hoehe, auf der die Leitung den Zielkasten erreicht.</summary>
            public int YB;

            public int XA;
            public int XB;

            /// <summary>Seite, an der die Leitung den Startkasten verlaesst (0 = links).</summary>
            public int SeiteA;

            /// <summary>Seite, an der die Leitung den Zielkasten erreicht (0 = links).</summary>
            public int SeiteB;
        }

        /// <summary>Linke Kante der Gasse zwischen Spalte <paramref name="g"/> und g+1 [px].</summary>
        private int GasseLinks(int g)
        {
            return SpaltenX[g] + SPALTEN_BREITE[g];
        }

        /// <summary>Die Spalte eines Knotens; -1 = unbekannt.</summary>
        private int SpalteZu(string schluessel)
        {
            SchemaModell.Knoten k = Modell.Finden(schluessel);
            return k == null ? -1 : (int)k.Art;
        }

        /// <summary>
        /// Legt den Wegzug jeder Kante — waagerecht aus dem Kasten, senkrecht in der
        /// GASSE zwischen zwei Spalten, waagerecht in den Zielkasten.
        ///
        /// <para><b>Zwei Durchgaenge.</b> Erst steht fest, WELCHE Gassen eine Kante
        /// braucht; dann bekommt jede Kante IN jeder Gasse ihre eigene Senkrechte, damit
        /// zwei Leitungen nicht uebereinanderliegen. Die Senkrechten einer Gasse stehen
        /// nach ihrer Einlaufhoehe sortiert — so kreuzen sich in der Gasse so wenige
        /// Leitungen wie moeglich.</para>
        ///
        /// <para><b>Ueber mehr als eine Gasse</b> (Erzeuger → Abnehmer springt ueber die
        /// Speicherspalte) wechselt die Leitung in der ersten Gasse auf eine FREIE BAHN,
        /// quert dort die uebersprungenen Spalten und faellt in der letzten Gasse auf die
        /// Zielhoehe. <see cref="FreieBahn"/> sucht die Hoehe, in der dort kein Kasten
        /// steht.</para>
        /// </summary>
        private void KantenLegen()
        {
            List<Wegvorsatz> vorsaetze = new List<Wegvorsatz>();

            foreach (SchemaModell.Kante kante in Modell.Kantenliste)
            {
                Rechteck von = FlaecheVon(kante.Von);
                Rechteck nach = FlaecheVon(kante.Nach);
                if (von.IstLeer || nach.IstLeer) continue;

                int sv = SpalteZu(kante.Von);
                int sn = SpalteZu(kante.Nach);
                if (sv < 0 || sn < 0) continue;

                Wegvorsatz w = new Wegvorsatz
                {
                    Kante = kante,
                    Von = von,
                    Nach = nach,
                    SpalteVon = sv,
                    SpalteNach = sn,
                    YA = von.MitteY,
                    YB = nach.MitteY
                };

                if (sn > sv)
                {
                    // Vorwaerts: rechts hinaus, links hinein.
                    w.Rueckwaerts = false;
                    w.GasseA = sv;
                    w.GasseB = sn - 1;
                    w.XA = von.Rechts;
                    w.XB = nach.X;
                    w.SeiteA = 1;
                    w.SeiteB = 0;
                }
                else if (sn < sv)
                {
                    // Rueckwaerts (Kaskade): links hinaus, rechts hinein — die Leitung
                    // bleibt in derselben Gasse, in der auch die Ladeleitung laeuft.
                    w.Rueckwaerts = true;
                    w.GasseA = sv - 1;
                    w.GasseB = sn;
                    w.XA = von.X;
                    w.XB = nach.Rechts;
                    w.SeiteA = 0;
                    w.SeiteB = 1;
                }
                else
                {
                    // Dieselbe Spalte — die Invariante S-1 schliesst das fuer Speicher
                    // aus; bleibt es dennoch stehen, wird nach rechts ausgewichen.
                    w.Rueckwaerts = false;
                    w.GasseA = sv < 3 ? sv : sv - 1;
                    w.GasseB = w.GasseA;
                    w.XA = von.Rechts;
                    w.XB = nach.Rechts;
                    w.SeiteA = 1;
                    w.SeiteB = 1;
                }

                vorsaetze.Add(w);
            }

            AnkerVerteilen(vorsaetze);
            Dictionary<long, int> gassenX = GassenBelegen(vorsaetze);

            for (int i = 0; i < vorsaetze.Count; i++)
            {
                Wegvorsatz w = vorsaetze[i];

                int gxA = gassenX[Wegschluessel(i, w.GasseA)];
                int gxB = gassenX[Wegschluessel(i, w.GasseB)];

                List<Punkt> punkte = new List<Punkt>();
                punkte.Add(new Punkt(w.XA, w.YA));

                if (w.GasseA == w.GasseB)
                {
                    if (w.YA != w.YB)
                    {
                        punkte.Add(new Punkt(gxA, w.YA));
                        punkte.Add(new Punkt(gxA, w.YB));
                    }
                }
                else
                {
                    int bahn = FreieBahn(w, (w.YA + w.YB) / 2);
                    punkte.Add(new Punkt(gxA, w.YA));
                    punkte.Add(new Punkt(gxA, bahn));
                    punkte.Add(new Punkt(gxB, bahn));
                    punkte.Add(new Punkt(gxB, w.YB));
                }

                punkte.Add(new Punkt(w.XB, w.YB));
                Vereinfachen(punkte);

                Kanten.Add(new Kantenzug
                {
                    Kante = w.Kante,
                    Punkte = punkte,
                    Mitte = Wegmitte(punkte),
                    Rueckwaerts = w.Rueckwaerts,
                    Breite = LINIE_BREITE
                });
            }
        }

        private static long Wegschluessel(int vorsatz, int gasse)
        {
            return (long)vorsatz * 8 + gasse;
        }

        /// <summary>
        /// Verteilt die ANSATZPUNKTE mehrerer Leitungen auf DERSELBEN Kastenseite ueber
        /// die Kastenhoehe: <c>n</c> Leitungen setzen an <c>n</c> gleich weit
        /// auseinanderliegenden Hoehen an statt alle in der Kastenmitte.
        ///
        /// <para><b>Warum.</b> Am Puffer 3000Ltr des Projekts 1042 haengen an der LINKEN
        /// Seite drei Leitungen: die Ladung des ersten Erzeugers und die zwei
        /// Kaskadenabgaenge zu den nachgeschalteten. Alle drei setzten in der
        /// Kastenmitte an und lagen auf dem ersten Stueck uebereinander — die
        /// gestrichelte Kaskade verschwand unter der Ladeleitung. Bei genau EINER
        /// Leitung ergibt die Formel wieder <c>MitteY</c>; der Regelfall aendert sich
        /// also nicht.</para>
        /// </summary>
        private static void AnkerVerteilen(List<Wegvorsatz> vorsaetze)
        {
            // (Kasten, Seite) -> die Ansaetze dort: {Vorsatz, A oder B, Gegenhoehe}
            Dictionary<string, List<int[]>> jeSeite = new Dictionary<string, List<int[]>>();

            for (int i = 0; i < vorsaetze.Count; i++)
            {
                Wegvorsatz w = vorsaetze[i];
                Eintragen(jeSeite, w.Kante.Von + "|" + w.SeiteA, i, 0, w.Nach.MitteY);
                Eintragen(jeSeite, w.Kante.Nach + "|" + w.SeiteB, i, 1, w.Von.MitteY);
            }

            foreach (KeyValuePair<string, List<int[]>> e in jeSeite)
            {
                List<int[]> liste = e.Value;
                if (liste.Count < 2) continue;

                // Nach der Hoehe des GEGENUEBERLIEGENDEN Endes — so kreuzen sich die
                // Leitungen unmittelbar am Kasten nicht.
                liste.Sort(delegate (int[] a, int[] b)
                {
                    int c = a[2].CompareTo(b[2]);
                    if (c != 0) return c;
                    c = a[0].CompareTo(b[0]);
                    return c != 0 ? c : a[1].CompareTo(b[1]);
                });

                for (int k = 0; k < liste.Count; k++)
                {
                    Wegvorsatz w = vorsaetze[liste[k][0]];
                    Rechteck r = liste[k][1] == 0 ? w.Von : w.Nach;
                    int y = r.Y + (r.Hoehe * (k + 1)) / (liste.Count + 1);

                    if (liste[k][1] == 0) w.YA = y; else w.YB = y;
                }
            }
        }

        private static void Eintragen(Dictionary<string, List<int[]>> jeSeite, string schluessel,
                                      int vorsatz, int ende, int gegenY)
        {
            List<int[]> liste;
            if (!jeSeite.TryGetValue(schluessel, out liste))
            {
                liste = new List<int[]>();
                jeSeite[schluessel] = liste;
            }
            liste.Add(new int[] { vorsatz, ende, gegenY });
        }

        /// <summary>
        /// Verteilt die Senkrechten auf die Breite ihrer Gasse: <c>n</c> Leitungen
        /// bekommen <c>n</c> gleich weit auseinanderliegende Bahnen zwischen den beiden
        /// Spalten. Zwischen zwei Spalten liegen <see cref="SPALTE_ABSTAND"/> px ohne
        /// jeden Kasten — jede dieser Bahnen ist damit kastenfrei.
        /// </summary>
        private Dictionary<long, int> GassenBelegen(List<Wegvorsatz> vorsaetze)
        {
            // Gasse -> die Paare (Vorsatz, Einlaufhoehe), die dort eine Senkrechte legen.
            Dictionary<int, List<int[]>> jeGasse = new Dictionary<int, List<int[]>>();

            for (int i = 0; i < vorsaetze.Count; i++)
            {
                Wegvorsatz w = vorsaetze[i];
                Eintragen(jeGasse, w.GasseA, i, w.YA);
                if (w.GasseB != w.GasseA) Eintragen(jeGasse, w.GasseB, i, w.YB);
            }

            Dictionary<long, int> ergebnis = new Dictionary<long, int>();

            foreach (KeyValuePair<int, List<int[]>> e in jeGasse)
            {
                List<int[]> liste = e.Value;

                // Nach Einlaufhoehe, bei Gleichstand nach Modellreihenfolge — die
                // Anordnung muss bei zwei Laeufen dieselbe sein.
                liste.Sort(delegate (int[] a, int[] b)
                {
                    int c = a[1].CompareTo(b[1]);
                    return c != 0 ? c : a[0].CompareTo(b[0]);
                });

                int links = GasseLinks(e.Key);
                for (int k = 0; k < liste.Count; k++)
                {
                    int x = links + (SPALTE_ABSTAND * (k + 1)) / (liste.Count + 1);
                    ergebnis[Wegschluessel(liste[k][0], e.Key)] = x;
                }
            }

            return ergebnis;
        }

        private static void Eintragen(Dictionary<int, List<int[]>> jeGasse, int gasse,
                                      int vorsatz, int y)
        {
            List<int[]> liste;
            if (!jeGasse.TryGetValue(gasse, out liste))
            {
                liste = new List<int[]>();
                jeGasse[gasse] = liste;
            }
            liste.Add(new int[] { vorsatz, y });
        }

        /// <summary>
        /// Die Hoehe, auf der eine Leitung die UEBERSPRUNGENEN Spalten queren darf —
        /// naeher an <paramref name="zielY"/> gibt es keine kastenfreie.
        ///
        /// <para>Gesucht wird in den Luecken zwischen den Kaesten dieser Spalten (die
        /// Anordnung laesst dort <see cref="KNOTEN_ABSTAND"/> px), ueber dem obersten und
        /// unter dem untersten. Steht dort ueberhaupt kein Kasten, bleibt es bei
        /// <paramref name="zielY"/>.</para>
        /// </summary>
        private int FreieBahn(Wegvorsatz w, int zielY)
        {
            int von = Math.Min(w.SpalteVon, w.SpalteNach) + 1;
            int bis = Math.Max(w.SpalteVon, w.SpalteNach) - 1;

            List<int[]> belegt = new List<int[]>();
            foreach (Knotenflaeche k in Knoten)
            {
                int spalte = (int)k.Knoten.Art;
                if (spalte < von || spalte > bis) continue;
                belegt.Add(new int[] { k.Flaeche.Y, k.Flaeche.Unten });
            }

            if (belegt.Count == 0) return zielY;

            belegt.Sort(delegate (int[] a, int[] b) { return a[0].CompareTo(b[0]); });

            // Ueberlappende Streifen zusammenfassen — die Luecken dazwischen sind frei.
            List<int[]> streifen = new List<int[]>();
            foreach (int[] s in belegt)
            {
                if (streifen.Count > 0 && s[0] <= streifen[streifen.Count - 1][1])
                {
                    int[] letzter = streifen[streifen.Count - 1];
                    if (s[1] > letzter[1]) letzter[1] = s[1];
                }
                else streifen.Add(new int[] { s[0], s[1] });
            }

            List<int> bahnen = new List<int>();
            bahnen.Add(streifen[0][0] - KNOTEN_ABSTAND / 2);
            for (int i = 1; i < streifen.Count; i++)
                bahnen.Add((streifen[i - 1][1] + streifen[i][0]) / 2);
            bahnen.Add(streifen[streifen.Count - 1][1] + KNOTEN_ABSTAND / 2);

            int beste = bahnen[0];
            foreach (int b in bahnen)
                if (Math.Abs(b - zielY) < Math.Abs(beste - zielY)) beste = b;

            return beste;
        }

        /// <summary>
        /// Wirft doppelte und auf einer Geraden liegende Stuetzpunkte weg — sonst
        /// entstuende an einer Stelle, an der Ein- und Ausgangshoehe zusammenfallen, ein
        /// Nullstueck, und die Pfeilspitze richtete sich daran aus.
        /// </summary>
        private static void Vereinfachen(List<Punkt> p)
        {
            for (int i = p.Count - 1; i > 0; i--)
                if (p[i].X == p[i - 1].X && p[i].Y == p[i - 1].Y) p.RemoveAt(i);

            for (int i = p.Count - 2; i > 0; i--)
                if ((p[i - 1].X == p[i].X && p[i].X == p[i + 1].X) ||
                    (p[i - 1].Y == p[i].Y && p[i].Y == p[i + 1].Y))
                    p.RemoveAt(i);
        }

        /// <summary>Der Punkt bei halber Weglaenge — dort sitzt der Prioritaetskreis.</summary>
        public static Punkt Wegmitte(List<Punkt> p)
        {
            if (p == null || p.Count == 0) return new Punkt(0, 0);
            if (p.Count == 1) return p[0];

            double gesamt = 0;
            for (int i = 1; i < p.Count; i++) gesamt += Laenge(p[i - 1], p[i]);
            if (gesamt <= 0) return p[0];

            double halb = gesamt / 2;
            for (int i = 1; i < p.Count; i++)
            {
                double l = Laenge(p[i - 1], p[i]);
                if (l <= 0) continue;
                if (halb > l) { halb -= l; continue; }

                double t = halb / l;
                return new Punkt((int)Math.Round(p[i - 1].X + t * (p[i].X - p[i - 1].X)),
                                 (int)Math.Round(p[i - 1].Y + t * (p[i].Y - p[i - 1].Y)));
            }

            return p[p.Count - 1];
        }

        private static double Laenge(Punkt a, Punkt b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return Math.Sqrt(dx * dx + dy * dy);
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

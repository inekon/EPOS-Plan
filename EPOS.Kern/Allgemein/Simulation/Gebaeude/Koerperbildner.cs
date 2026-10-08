using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Die Richtung, in die eine Fläche um ihre Dicke extrudiert wird — gemessen am Raum (Konzept 17.3 Nr. 3).</summary>
    internal enum Extrusionsrichtung
    {
        /// <summary>Die Fläche liegt in der Außenoberfläche (Außenmaß): um die Dicke zum Raum hin.</summary>
        NachInnen,

        /// <summary>Die Fläche liegt in der Innenoberfläche des Raums: um die Dicke vom Raum weg.</summary>
        NachAussen,

        /// <summary>Die Fläche liegt in der Achse: je halbe Dicke zu beiden Seiten.</summary>
        Beidseitig,
    }

    /// <summary>
    /// Eine Fläche der Datei als Vieleck im Raum [m]: Außenring, Löcher, die Kennung der Fläche bzw. des Bauteils der Datei
    /// (<see cref="Kennung"/>, wird zur Quellfläche jedes Dreiecks) und je Loch optional eine eigene Kennung (etwa die der
    /// Öffnung, für die Laibung). Der Umlaufsinn ist beliebig — der Bildner richtet die Flächen selbst (17.2 Nr. 2).
    /// </summary>
    internal sealed class Quellflaeche
    {
        internal string Kennung { get; init; } = "";

        internal IReadOnlyList<double[]> Aussen { get; init; } = Array.Empty<double[]>();

        internal IReadOnlyList<IReadOnlyList<double[]>> Loecher { get; init; } = Array.Empty<IReadOnlyList<double[]>>();

        /// <summary>Je Loch die Kennung seiner Laibung; fehlt sie (oder ist sie leer), gilt <see cref="Kennung"/>.</summary>
        internal IReadOnlyList<string> Lochkennungen { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Ein Ring eines Raumpolygons (17.2 Weg a): die Punkte im Grundriss (x, y) [m] und je Punkt die Höhe von Boden und
    /// Decke [m] — verschiedene Deckenhöhen ergeben eine geneigte Decke. Je Kante optional die Kennung ihrer Mantelfläche.
    /// </summary>
    internal sealed class Raumring
    {
        internal IReadOnlyList<double[]> Punkte { get; init; } = Array.Empty<double[]>();

        internal IReadOnlyList<double> Boden { get; init; } = Array.Empty<double>();

        internal IReadOnlyList<double> Decke { get; init; } = Array.Empty<double>();

        /// <summary>Die Kennung der Mantelfläche je Kante (Kante i: Punkt i → Punkt i+1); leer = abgeleitet.</summary>
        internal IReadOnlyList<string> Kantenkennungen { get; init; } = Array.Empty<string>();

        /// <summary>Ein Ring mit gleicher Boden- und Deckenhöhe an allen Punkten.</summary>
        internal static Raumring Eben(IReadOnlyList<double[]> punkte, double boden, double decke)
            => new Raumring { Punkte = punkte, Boden = punkte.Select(_ => boden).ToArray(), Decke = punkte.Select(_ => decke).ToArray() };

        /// <summary>
        /// Ein Ring aus Höhen je Kante, wie die Projektdatei sie führt (je Kante Boden am Anfang, Boden am Ende, Decke am
        /// Anfang, Decke am Ende). An jedem Punkt müssen Ende der Vorkante und Anfang der Folgekante auf
        /// <paramref name="toleranzM"/> übereinstimmen; sonst <c>null</c> und die Stelle in <paramref name="unstetig"/>.
        /// </summary>
        internal static Raumring AusKantenhoehen(IReadOnlyList<double[]> punkte, IReadOnlyList<double[]> kantenhoehen,
                                                 double toleranzM, out int unstetig)
        {
            unstetig = -1;
            int n = punkte.Count;
            if (kantenhoehen.Count != n) { unstetig = 0; return null; }
            var boden = new double[n];
            var decke = new double[n];
            for (int i = 0; i < n; i++)
            {
                double[] vor = kantenhoehen[(i + n - 1) % n], nach = kantenhoehen[i];
                if (Math.Abs(vor[1] - nach[0]) > toleranzM || Math.Abs(vor[3] - nach[2]) > toleranzM) { unstetig = i; return null; }
                boden[i] = nach[0];
                decke[i] = nach[2];
            }
            return new Raumring { Punkte = punkte, Boden = boden, Decke = decke };
        }
    }

    /// <summary>Ein Polygon eines Raums: Außenring, Löcher und die Kennungen von Boden und Decke (leer = abgeleitet).</summary>
    internal sealed class Raumpolygon
    {
        internal string Kennung { get; init; } = "";

        internal Raumring Aussen { get; init; }

        internal IReadOnlyList<Raumring> Loecher { get; init; } = Array.Empty<Raumring>();

        internal string BodenKennung { get; init; } = "";

        internal string DeckenKennung { get; init; } = "";
    }

    /// <summary>
    /// Das Ergebnis des Bildners: entweder ein geschlossener <see cref="Koerper"/> oder ein benannter Fehlschlag
    /// (<see cref="Grund"/> mit <see cref="Werte"/>) — dann zeigt die Ansicht das Umrissprisma (<see cref="Rueckfall"/>).
    /// </summary>
    internal sealed class Koerperergebnis
    {
        internal Dateikoerper Koerper { get; init; }

        /// <summary>Der Meldungsschlüssel des Fehlschlags, sprachneutral; leer bei Erfolg.</summary>
        internal string Grund { get; init; } = "";

        /// <summary>Die Werte zum <see cref="Grund"/> (invariante Kultur), etwa die Zahl der offenen Kanten.</summary>
        internal IReadOnlyList<string> Werte { get; init; } = Array.Empty<string>();

        /// <summary>Die Zahl der Kanten, die nicht genau zwei Dreiecken angehören (nach Teilung an T-Stößen).</summary>
        internal int OffeneKanten { get; init; }

        internal bool Gebildet => Koerper != null;

        /// <summary>Der Rückfall der Ansicht bei einem Fehlschlag; <c>null</c> bei Erfolg.</summary>
        internal string Rueckfall => Gebildet ? null : Koerperbildner.RUECKFALL_UMRISSPRISMA;

        public override string ToString() => Gebildet ? Koerper.ToString() : Grund + " (" + string.Join(",", Werte) + ")";
    }

    /// <summary>Eine Öffnung, die aus einer Wandfläche ausgespart werden soll: Kennung und Ring (in der Ebene der Wand oder nah daran).</summary>
    internal sealed class Wandoeffnung
    {
        internal string Kennung { get; init; } = "";

        internal IReadOnlyList<double[]> Ring { get; init; } = Array.Empty<double[]>();
    }

    /// <summary>Wie eine Öffnung aus ihrer Wand ausgespart wurde.</summary>
    internal enum Aussparungsart
    {
        /// <summary>Als Loch im Inneren der Fläche, die Laibung als Mantel des Lochs.</summary>
        Loch,
        /// <summary>Als Kerbe am Rand der Fläche, die Laibung nur an den Kanten im Inneren der Wand.</summary>
        Kerbe,
        /// <summary>Nicht ausgespart; der Grund steht in <see cref="Aussparung.Grund"/>.</summary>
        Keine,
    }

    /// <summary>Das Ergebnis der Aussparung je Öffnung, in der Reihenfolge der Eingabe.</summary>
    internal sealed class Aussparung
    {
        internal string Kennung { get; init; } = "";

        internal Aussparungsart Art { get; init; }

        /// <summary>Bei <see cref="Aussparungsart.Keine"/>: einer der Gründe <c>Koerperbildner.GRUND_*</c>.</summary>
        internal string Grund { get; init; } = "";

        internal bool Ausgespart => Art != Aussparungsart.Keine;

        public override string ToString() => Kennung + ":" + Art + (string.IsNullOrEmpty(Grund) ? "" : "(" + Grund + ")");
    }

    /// <summary>
    /// <b>Der formatfreie Körperbildner</b> (Datenaustauschkonzept 17.2 bis 17.5, Stufe K1): bildet aus den Flächen einer
    /// Datei Körper derselben Art wie der IFC-Körper (<see cref="Dateikoerper"/> mit Quelle
    /// <see cref="Koerperquelle.AusFlaechen"/> und je Dreieck der Kennung seiner Quellfläche) — das Raumprisma aus einem
    /// Raumpolygon mit Höhen je Punkt (<see cref="Raumprisma"/>), die Hülle aus Flächen (<see cref="Huelle"/>) und die
    /// Extrusion einer Fläche um ihre Dicke (<see cref="Extrusion"/>).
    ///
    /// <para><b>Hüllprüfung:</b> Punkte auf <see cref="TOLERANZ_M"/> zusammengelegt, Kanten an T-Stößen geteilt; jede Kante
    /// muss danach genau zwei Dreiecken angehören. Die Flächen werden über die Kantenpaare gleichsinnig gerichtet — der
    /// Umlauf der Datei wird nicht übernommen —, und ein Bestandteil mit negativem Volumen wird gewendet: alle Normalen
    /// zeigen nach außen. Ein offener Körper wird nicht zurückgegeben, sondern als Fehlschlag
    /// <see cref="GRUND_NICHT_GESCHLOSSEN"/> mit der Zahl der offenen Kanten (17.2 Nr. 4).</para>
    ///
    /// <para><b>Deterministisch:</b> keine Hash-Reihenfolge, feste Startecken, Punkte auf <see cref="Zonenkoerper.STELLEN"/>
    /// Stellen gerundet — gleiche Eingabe, gleicher <see cref="Dateikoerper.Text"/>. Ohne Plattformzugriff.</para>
    /// </summary>
    internal static class Koerperbildner
    {
        /// <summary>Die Toleranz der Kantenpaarung und der T-Teilung [m] (17.2 Nr. 1).</summary>
        internal const double TOLERANZ_M = 0.001;

        /// <summary>Weg (a) der Projektdatei: Prisma aus dem Raumpolygon.</summary>
        internal const string ART_RAUMPOLYGON = "Raumpolygon";

        /// <summary>Hülle aus den Flächen eines Raums.</summary>
        internal const string ART_HUELLFLAECHEN = "Huellflaechen";

        /// <summary>Bauteilkörper aus einer extrudierten Fläche.</summary>
        internal const string ART_FLAECHENEXTRUSION = "Flaechenextrusion";

        /// <summary>Der Rückfall der Ansicht, wenn kein Körper gebildet wird.</summary>
        internal const string RUECKFALL_UMRISSPRISMA = "Umrissprisma";

        /// <summary>Die Hülle schließt nicht; Wert: Zahl der offenen Kanten.</summary>
        internal const string GRUND_NICHT_GESCHLOSSEN = "KOERPER_NICHT_GESCHLOSSEN";

        /// <summary>Die Flächen lassen sich nicht gleichsinnig richten (verdrehte Hülle).</summary>
        internal const string GRUND_NICHT_ORIENTIERBAR = "KOERPER_NICHT_ORIENTIERBAR";

        /// <summary>Keine Fläche mit Inhalt, oder ein Bestandteil ohne Volumen.</summary>
        internal const string GRUND_LEER = "KOERPER_LEER";

        /// <summary>Mehr Dreiecke als <see cref="Dateikoerper.DREIECKSGRENZE"/>; Wert: Zahl der Dreiecke.</summary>
        internal const string GRUND_GRENZE = "KOERPER_GRENZE";

        /// <summary>Keine Dicke größer null; Wert: die Dicke.</summary>
        internal const string GRUND_DICKE = "KOERPER_DICKE";

        /// <summary>Ein Raumring ohne passende Höhen (Zahl der Höhen ungleich Zahl der Punkte, oder Decke unter Boden).</summary>
        internal const string GRUND_HOEHEN = "KOERPER_HOEHEN";

        /// <summary>Die Öffnung liegt ganz außerhalb ihrer Wandfläche.</summary>
        internal const string GRUND_OEFFNUNG_AUSSERHALB = "OEFFNUNG_AUSSERHALB";

        /// <summary>Die Öffnung deckt ihre Wandfläche ganz.</summary>
        internal const string GRUND_OEFFNUNG_DECKT = "OEFFNUNG_DECKT";

        /// <summary>Die Öffnung überlappt eine andere Öffnung derselben Wand.</summary>
        internal const string GRUND_OEFFNUNG_UEBERLAPPT = "OEFFNUNG_UEBERLAPPT";

        /// <summary>Der Ring der Öffnung ist leer oder ohne Fläche.</summary>
        internal const string GRUND_OEFFNUNG_LEER = "OEFFNUNG_LEER";

        /// <summary>Der Schnitt ergibt kein einfaches Vieleck (etwa eine Berührung in einem Punkt).</summary>
        internal const string GRUND_KERBE_NICHT_EINFACH = "KERBE_NICHT_EINFACH";

        /// <summary>
        /// Die Toleranz der Aussparung: Eine Öffnung, deren Ecke höchstens so weit vom Wandrand liegt oder deren Rand den
        /// Wandrand schneidet, wird als Kerbe geschnitten, sonst als Loch ausgespart.
        /// </summary>
        internal const double KERBTOLERANZ_M = 0.002;

        // ==================================================================
        //  Schnittstelle
        // ==================================================================

        /// <summary>
        /// Der Raumkörper aus einem oder mehreren Polygonen eines Raums (17.2 Weg a): Boden und Decke per Ohrenschnitt im
        /// Grundriss, je Punkt auf ihre Höhe gehoben (geneigte Decke), der Mantel je Kante als ebenes Viereck; Löcher mit
        /// eigenem Mantel. Mehrere Polygone ergeben einen Körper aus mehreren Bestandteilen.
        /// </summary>
        internal static Koerperergebnis Raumprisma(IReadOnlyList<Raumpolygon> polygone)
        {
            var teile = new List<Teilflaeche>();
            var vermerke = new SortedSet<Koerpervermerk>();
            if (polygone == null || polygone.Count == 0) return Fehlschlag(GRUND_LEER);
            for (int pg = 0; pg < polygone.Count; pg++)
            {
                Raumpolygon p = polygone[pg];
                var ringe = new List<Raumring> { p.Aussen };
                ringe.AddRange(p.Loecher ?? Array.Empty<Raumring>());
                foreach (Raumring r in ringe)
                    if (r == null || r.Punkte.Count < 3 || r.Boden.Count != r.Punkte.Count || r.Decke.Count != r.Punkte.Count
                        || Enumerable.Range(0, r.Punkte.Count).Any(i => r.Decke[i] < r.Boden[i]))
                        return Fehlschlag(GRUND_HOEHEN, pg.ToString(CultureInfo.InvariantCulture));
                string kennung = string.IsNullOrEmpty(p.Kennung) ? "Polygon" + pg.ToString(CultureInfo.InvariantCulture) : p.Kennung;

                // Boden und Decke: ein Ohrenschnitt im Grundriss, auf die Höhen je Punkt gehoben.
                var eben = new List<double[]>();
                var stellen = new List<List<int>>();
                for (int r = 0; r < ringe.Count; r++)
                {
                    var s = new List<int>();
                    foreach (double[] q in ringe[r].Punkte) { s.Add(eben.Count); eben.Add(new[] { q[0], q[1] }); }
                    if (Polygonnetz.Flaeche2D(s.Select(i => eben[i]).ToList()) > 0.0 != (r == 0)) s.Reverse();
                    stellen.Add(s);
                }
                List<int[]> deckel = Polygonnetz.Dreiecke2D(eben, stellen, out List<int> verloren);
                if (verloren.Count > 0) vermerke.Add(Koerpervermerk.Loch);
                double[] HoeheBoden(int i) { (int r, int k) = Ort(ringe, i); return new[] { eben[i][0], eben[i][1], ringe[r].Boden[k] }; }
                double[] HoeheDecke(int i) { (int r, int k) = Ort(ringe, i); return new[] { eben[i][0], eben[i][1], ringe[r].Decke[k] }; }
                var rand = new List<int[]>();
                for (int r = 0; r < ringe.Count; r++)
                    for (int k = 0; k < ringe[r].Punkte.Count; k++)
                        rand.Add(new[] { stellen[r].Min() + k, stellen[r].Min() + (k + 1) % ringe[r].Punkte.Count });
                List<double[]> boden = Enumerable.Range(0, eben.Count).Select(HoeheBoden).ToList();
                List<double[]> decke = Enumerable.Range(0, eben.Count).Select(HoeheDecke).ToList();
                if (!Eben(boden) || !Eben(decke)) vermerke.Add(Koerpervermerk.Uneben);
                teile.Add(new Teilflaeche(boden, deckel, rand, Kennung(p.BodenKennung, kennung + ":Boden")));
                teile.Add(new Teilflaeche(decke, deckel, rand, Kennung(p.DeckenKennung, kennung + ":Decke")));

                // Mantel je Kante: Boden am Anfang, Boden am Ende, Decke am Ende, Decke am Anfang.
                for (int r = 0; r < ringe.Count; r++)
                {
                    Raumring ring = ringe[r];
                    int n = ring.Punkte.Count;
                    for (int k = 0; k < n; k++)
                    {
                        int j = (k + 1) % n;
                        double[] a = ring.Punkte[k], b = ring.Punkte[j];
                        var viereck = new List<double[]>
                        {
                            new[] { a[0], a[1], ring.Boden[k] }, new[] { b[0], b[1], ring.Boden[j] },
                            new[] { b[0], b[1], ring.Decke[j] }, new[] { a[0], a[1], ring.Decke[k] },
                        };
                        string ke = k < ring.Kantenkennungen.Count && !string.IsNullOrEmpty(ring.Kantenkennungen[k])
                            ? ring.Kantenkennungen[k]
                            : kennung + ":Kante" + r.ToString(CultureInfo.InvariantCulture) + "." + k.ToString(CultureInfo.InvariantCulture);
                        Teilflaeche t = Vieleck(viereck, null, ke, vermerke);
                        if (t != null) teile.Add(t);
                    }
                }
            }
            return Schliessen(teile, ART_RAUMPOLYGON, TOLERANZ_M, vermerke);
        }

        /// <summary>
        /// Die Hülle aus Flächen der Datei (17.2 Weg b, gbXML <c>ClosedShell</c>): jede Fläche per Ohrenschnitt zerlegt,
        /// Kanten an T-Stößen geteilt, gleichsinnig gerichtet, Normalen nach außen; offen → Fehlschlag mit Grund.
        /// </summary>
        internal static Koerperergebnis Huelle(IReadOnlyList<Quellflaeche> flaechen, string art = ART_HUELLFLAECHEN, double toleranzM = TOLERANZ_M)
        {
            var teile = new List<Teilflaeche>();
            var vermerke = new SortedSet<Koerpervermerk>();
            foreach (Quellflaeche f in flaechen ?? Array.Empty<Quellflaeche>())
            {
                Teilflaeche t = Vieleck(f.Aussen, f.Loecher, f.Kennung, vermerke);
                if (t != null) teile.Add(t);
            }
            return Schliessen(teile, art, toleranzM, vermerke);
        }

        /// <summary>
        /// Der Bauteilkörper einer Fläche (17.3): das Vieleck samt Löchern um <paramref name="dickeM"/> extrudiert, in der
        /// <paramref name="richtung"/> gemessen am Raum — <paramref name="zumRaum"/> ist ein Vektor zur Raumseite der
        /// Fläche; ohne ihn gilt die Gegenrichtung der Newell-Normale des Außenrings als Raumseite. Löcher werden
        /// ausgespart, ihre Laibung entsteht als Mantel des Lochs (Quellfläche: die Lochkennung, sonst die Fläche).
        /// </summary>
        internal static Koerperergebnis Extrusion(Quellflaeche flaeche, double dickeM, Extrusionsrichtung richtung, double[] zumRaum = null)
        {
            if (!(dickeM > 0.0) || double.IsInfinity(dickeM))
                return Fehlschlag(GRUND_DICKE, dickeM.ToString("R", CultureInfo.InvariantCulture));
            List<double[]> aussen = Polygonnetz.Bereinigt(flaeche?.Aussen ?? Array.Empty<double[]>());
            if (aussen.Count < 3) return Fehlschlag(GRUND_LEER);
            double[] n = Polygonnetz.Normiert(Polygonnetz.Newell(aussen));
            if (n == null) return Fehlschlag(GRUND_LEER);
            double[] raum = zumRaum != null && Polygonnetz.Punkt(zumRaum, n) > 0.0 ? n : Polygonnetz.Mal(n, -1.0);
            (double von, double bis) = richtung switch
            {
                Extrusionsrichtung.NachInnen => (0.0, dickeM),
                Extrusionsrichtung.NachAussen => (-dickeM, 0.0),
                _ => (-dickeM / 2.0, dickeM / 2.0),
            };
            List<double[]> Versetzt(IEnumerable<double[]> ring, double um) => ring.Select(p => Polygonnetz.Plus(p, Polygonnetz.Mal(raum, um))).ToList();
            var loecher = (flaeche.Loecher ?? Array.Empty<IReadOnlyList<double[]>>()).Select(l => Polygonnetz.Bereinigt(l)).Where(l => l.Count >= 3).ToList();

            var vermerke = new SortedSet<Koerpervermerk>();
            var teile = new List<Teilflaeche>();
            foreach (double um in new[] { von, bis })
            {
                Teilflaeche t = Vieleck(Versetzt(aussen, um), loecher.Select(l => (IReadOnlyList<double[]>)Versetzt(l, um)).ToList(), flaeche.Kennung, vermerke);
                if (t != null) teile.Add(t);
            }
            void Mantel(List<double[]> ring, string kennung)
            {
                for (int k = 0; k < ring.Count; k++)
                {
                    double[] a = ring[k], b = ring[(k + 1) % ring.Count];
                    var viereck = new List<double[]> { Versetzt(new[] { a }, von)[0], Versetzt(new[] { b }, von)[0], Versetzt(new[] { b }, bis)[0], Versetzt(new[] { a }, bis)[0] };
                    Teilflaeche t = Vieleck(viereck, null, kennung, vermerke);
                    if (t != null) teile.Add(t);
                }
            }
            Mantel(aussen, flaeche.Kennung);
            int stelle = 0;
            foreach (IReadOnlyList<double[]> roh in flaeche.Loecher ?? Array.Empty<IReadOnlyList<double[]>>())
            {
                List<double[]> l = Polygonnetz.Bereinigt(roh);
                string kl = stelle < flaeche.Lochkennungen.Count && !string.IsNullOrEmpty(flaeche.Lochkennungen[stelle]) ? flaeche.Lochkennungen[stelle] : flaeche.Kennung;
                stelle++;
                if (l.Count >= 3) Mantel(l, kl);
            }
            return Schliessen(teile, ART_FLAECHENEXTRUSION, TOLERANZ_M, vermerke);
        }

        /// <summary>
        /// Der Bauteilkörper einer Wandfläche mit ihren Öffnungen (17.3 Nr. 4): Eine Öffnung im Inneren der Fläche wird als
        /// Loch ausgespart wie bei <see cref="Extrusion(Quellflaeche, double, Extrusionsrichtung, double[])"/>; eine Öffnung,
        /// die den Rand berührt oder überschreitet, wird als <b>Kerbe</b> aus der Fläche geschnitten (Differenz in der
        /// Wandebene, <see cref="Kerbschnitt"/>) — die Laibung entsteht nur an den Kanten der Kerbe im Inneren der Wand, an
        /// Kanten auf dem Wandrand bleibt die Stirnfläche der Wand. Zerfällt die Fläche, wird jedes Teil extrudiert; der
        /// Körper trägt dann mehrere Bestandteile. Ohne Kerbe ist das Ergebnis dasselbe wie das der Extrusion mit Löchern.
        /// Löcher der Fläche selbst (<see cref="Quellflaeche.Loecher"/>) gehen vor den Öffnungen ein.
        /// <paramref name="aussparungen"/> nennt je Öffnung, wie sie ausgespart wurde, oder den Grund, warum nicht.
        /// </summary>
        internal static Koerperergebnis Extrusion(Quellflaeche flaeche, IReadOnlyList<Wandoeffnung> oeffnungen, double dickeM, Extrusionsrichtung richtung,
                                                  double[] zumRaum, out List<Aussparung> aussparungen)
        {
            aussparungen = new List<Aussparung>();
            var alle = new List<Wandoeffnung>();
            int eigene = 0;
            if (flaeche?.Loecher != null)
                for (int i = 0; i < flaeche.Loecher.Count; i++, eigene++)
                    alle.Add(new Wandoeffnung
                    {
                        Kennung = i < flaeche.Lochkennungen.Count && !string.IsNullOrEmpty(flaeche.Lochkennungen[i]) ? flaeche.Lochkennungen[i] : flaeche.Kennung,
                        Ring = flaeche.Loecher[i],
                    });
            alle.AddRange(oeffnungen ?? Array.Empty<Wandoeffnung>());

            List<double[]> aussen = Polygonnetz.Bereinigt(flaeche?.Aussen ?? Array.Empty<double[]>());
            double[] n = aussen.Count >= 3 ? Polygonnetz.Normiert(Polygonnetz.Newell(aussen)) : null;
            if (n == null || !(dickeM > 0.0) || double.IsInfinity(dickeM))
            {
                foreach (Wandoeffnung o in alle.Skip(eigene)) aussparungen.Add(new Aussparung { Kennung = o.Kennung, Art = Aussparungsart.Keine, Grund = GRUND_LEER });
                return Extrusion(flaeche ?? new Quellflaeche(), dickeM, richtung, zumRaum);
            }

            // Die Ebene der Wand: Ursprung, zwei Achsen.
            double[] ursprung = aussen[0];
            double[] hilf = Math.Abs(n[2]) < 0.9 ? new[] { 0.0, 0.0, 1.0 } : new[] { 1.0, 0.0, 0.0 };
            double[] e1 = Polygonnetz.Normiert(Polygonnetz.Kreuz(hilf, n));
            double[] e2 = Polygonnetz.Kreuz(n, e1);
            List<double[]> Flach(IEnumerable<double[]> ring)
                => ring.Select(q => { double[] d = Polygonnetz.Minus(q, ursprung); return new[] { Polygonnetz.Punkt(d, e1), Polygonnetz.Punkt(d, e2) }; }).ToList();
            const double tol = KERBTOLERANZ_M;

            List<double[]> wand2D = Flach(aussen);
            var teile2D = new List<Kerbring>
            {
                new Kerbring
                {
                    Punkte = wand2D,
                    Kanten = Enumerable.Repeat(flaeche.Kennung ?? "", aussen.Count).ToList(),
                    Herkunft = Enumerable.Range(0, aussen.Count).ToList(),
                },
            };
            var ergebnis = new Aussparung[alle.Count];
            var ringe = new List<double[]>[alle.Count];
            bool kerbe = false;
            for (int i = 0; i < alle.Count; i++)
            {
                Wandoeffnung o = alle[i];
                List<double[]> b = Kerbschnitt.Bereinigt(Flach(Polygonnetz.Bereinigt(o.Ring ?? Array.Empty<double[]>())), tol);
                ringe[i] = b;
                if (b.Count < 3 || Math.Abs(Kerbschnitt.Flaeche(b)) <= tol * tol)
                {
                    ergebnis[i] = new Aussparung { Kennung = o.Kennung, Art = Aussparungsart.Keine, Grund = GRUND_OEFFNUNG_LEER };
                    continue;
                }
                Kerblage lage = Kerbschnitt.Lage(wand2D, b, tol);
                if (lage == Kerblage.Innen) continue;   // Loch, nach den Kerben zugeordnet
                if (lage == Kerblage.Aussen || lage == Kerblage.Deckt)
                {
                    ergebnis[i] = new Aussparung
                    {
                        Kennung = o.Kennung, Art = Aussparungsart.Keine, Grund = lage == Kerblage.Aussen ? GRUND_OEFFNUNG_AUSSERHALB : GRUND_OEFFNUNG_DECKT,
                    };
                    continue;
                }
                var neu = new List<Kerbring>();
                string grund = null;
                foreach (Kerbring t in teile2D)
                {
                    List<Kerbring> r = Kerbschnitt.Differenz(t, b, o.Kennung, tol, out bool beruehrt);
                    if (r != null) neu.AddRange(r);
                    else if (beruehrt) { grund = GRUND_KERBE_NICHT_EINFACH; break; }
                    else if (Kerbschnitt.Lage(t.Punkte, b, tol) == Kerblage.Aussen) neu.Add(t);
                    else { grund = GRUND_OEFFNUNG_UEBERLAPPT; break; }
                }
                if (grund == null && neu.Count == 0) grund = GRUND_OEFFNUNG_DECKT;
                if (grund != null)
                {
                    ergebnis[i] = new Aussparung { Kennung = o.Kennung, Art = Aussparungsart.Keine, Grund = grund };
                    continue;
                }
                teile2D = neu;
                kerbe = true;
                ergebnis[i] = new Aussparung { Kennung = o.Kennung, Art = Aussparungsart.Kerbe };
            }

            // Löcher: je Teil, ganz im Inneren und frei von den übrigen Löchern dieses Teils.
            var loecherJeTeil = teile2D.Select(_ => new List<int>()).ToList();
            for (int i = 0; i < alle.Count; i++)
            {
                if (ergebnis[i] != null) continue;
                int teil = -1;
                for (int t = 0; t < teile2D.Count && teil < 0; t++)
                    if (Kerbschnitt.Lage(teile2D[t].Punkte, ringe[i], tol) == Kerblage.Innen
                        && loecherJeTeil[t].All(j => Kerbschnitt.Lage(ringe[j], ringe[i], tol) == Kerblage.Aussen))
                        teil = t;
                if (teil < 0)
                {
                    ergebnis[i] = new Aussparung { Kennung = alle[i].Kennung, Art = Aussparungsart.Keine, Grund = GRUND_OEFFNUNG_UEBERLAPPT };
                    continue;
                }
                loecherJeTeil[teil].Add(i);
                ergebnis[i] = new Aussparung { Kennung = alle[i].Kennung, Art = Aussparungsart.Loch };
            }

            Koerperergebnis Ohne()
            {
                List<int> loch = Enumerable.Range(0, alle.Count).Where(i => ergebnis[i].Art == Aussparungsart.Loch).ToList();
                return Extrusion(new Quellflaeche
                {
                    Kennung = flaeche.Kennung, Aussen = flaeche.Aussen,
                    Loecher = loch.Select(i => alle[i].Ring).ToList(), Lochkennungen = loch.Select(i => alle[i].Kennung).ToList(),
                }, dickeM, richtung, zumRaum);
            }

            Koerperergebnis e;
            if (!kerbe) e = Ohne();
            else
            {
                double[] raum = zumRaum != null && Polygonnetz.Punkt(zumRaum, n) > 0.0 ? n : Polygonnetz.Mal(n, -1.0);
                (double von, double bis) = richtung switch
                {
                    Extrusionsrichtung.NachInnen => (0.0, dickeM),
                    Extrusionsrichtung.NachAussen => (-dickeM, 0.0),
                    _ => (-dickeM / 2.0, dickeM / 2.0),
                };
                double[] Versetzt(double[] p, double um) => Polygonnetz.Plus(p, Polygonnetz.Mal(raum, um));
                double[] Raumpunkt(Kerbring t, int k)
                    => t.Herkunft[k] >= 0 ? aussen[t.Herkunft[k]]
                       : Polygonnetz.Plus(ursprung, Polygonnetz.Plus(Polygonnetz.Mal(e1, t.Punkte[k][0]), Polygonnetz.Mal(e2, t.Punkte[k][1])));
                var vermerke = new SortedSet<Koerpervermerk>();
                var teile = new List<Teilflaeche>();
                void Viereck(double[] a, double[] b, string kennung)
                {
                    Teilflaeche m = Vieleck(new List<double[]> { Versetzt(a, von), Versetzt(b, von), Versetzt(b, bis), Versetzt(a, bis) }, null, kennung, vermerke);
                    if (m != null) teile.Add(m);
                }
                for (int t = 0; t < teile2D.Count; t++)
                {
                    Kerbring kr = teile2D[t];
                    List<double[]> ring = Enumerable.Range(0, kr.Punkte.Count).Select(k => Raumpunkt(kr, k)).ToList();
                    List<List<double[]>> loecher = loecherJeTeil[t].Select(i => Polygonnetz.Bereinigt(alle[i].Ring)).ToList();
                    foreach (double um in new[] { von, bis })
                    {
                        Teilflaeche f = Vieleck(ring.Select(p => Versetzt(p, um)).ToList(),
                                                loecher.Select(l => (IReadOnlyList<double[]>)l.Select(p => Versetzt(p, um)).ToList()).ToList(), flaeche.Kennung, vermerke);
                        if (f != null) teile.Add(f);
                    }
                    for (int k = 0; k < ring.Count; k++) Viereck(ring[k], ring[(k + 1) % ring.Count], kr.Kanten[k]);
                    for (int j = 0; j < loecher.Count; j++)
                        for (int k = 0; k < loecher[j].Count; k++)
                            Viereck(loecher[j][k], loecher[j][(k + 1) % loecher[j].Count], alle[loecherJeTeil[t][j]].Kennung);
                }
                e = Schliessen(teile, ART_FLAECHENEXTRUSION, TOLERANZ_M, vermerke);
                if (!e.Gebildet)
                {
                    // Die Kerben gelingen nicht: Rückfall auf die Löcher, die Kerben mit dem Grund der Hüllprüfung.
                    for (int i = 0; i < alle.Count; i++)
                        if (ergebnis[i].Art == Aussparungsart.Kerbe)
                            ergebnis[i] = new Aussparung { Kennung = alle[i].Kennung, Art = Aussparungsart.Keine, Grund = e.Grund };
                    e = Ohne();
                }
            }
            aussparungen.AddRange(ergebnis.Skip(eigene));
            return e;
        }

        // ==================================================================
        //  Teilflächen
        // ==================================================================

        /// <summary>Eine zerlegte Fläche: Punkte, Dreiecke (lokale Indizes), Randkanten und die Kennung der Quellfläche.</summary>
        private sealed class Teilflaeche
        {
            internal Teilflaeche(List<double[]> punkte, List<int[]> dreiecke, List<int[]> rand, string kennung)
            {
                Punkte = punkte;
                Dreiecke = dreiecke;
                Rand = rand;
                Kennung = kennung ?? "";
            }

            internal List<double[]> Punkte { get; }

            internal List<int[]> Dreiecke { get; }

            internal List<int[]> Rand { get; }

            internal string Kennung { get; }
        }

        private static string Kennung(string gegeben, string abgeleitet) => string.IsNullOrEmpty(gegeben) ? abgeleitet : gegeben;

        private static (int Ring, int Stelle) Ort(List<Raumring> ringe, int i)
        {
            int r = 0;
            while (i >= ringe[r].Punkte.Count) { i -= ringe[r].Punkte.Count; r++; }
            return (r, i);
        }

        private static bool Eben(List<double[]> punkte)
        {
            double[] n = Polygonnetz.Normiert(Polygonnetz.Newell(punkte));
            if (n == null) return true;
            return punkte.All(p => Math.Abs(Polygonnetz.Punkt(Polygonnetz.Minus(p, punkte[0]), n)) <= Zonengeometrie.EBEN_TOLERANZ_M);
        }

        /// <summary>Ein Vieleck im Raum mit Löchern als <see cref="Teilflaeche"/>; <c>null</c> bei weniger als drei Punkten.</summary>
        private static Teilflaeche Vieleck(IReadOnlyList<double[]> aussenRoh, IReadOnlyList<IReadOnlyList<double[]>> loecherRoh, string kennung,
                                           SortedSet<Koerpervermerk> vermerke)
        {
            List<double[]> aussen = Polygonnetz.Bereinigt(aussenRoh);
            if (aussen.Count < 3) return null;
            var loecher = (loecherRoh ?? Array.Empty<IReadOnlyList<double[]>>()).Select(l => Polygonnetz.Bereinigt(l)).Where(l => l.Count >= 3).ToList();
            List<int[]> d = Polygonnetz.Flaeche3D(aussen, loecher, Zonengeometrie.EBEN_TOLERANZ_M, out bool eben, out List<int> verloren);
            if (!eben) vermerke.Add(Koerpervermerk.Uneben);
            if (verloren.Count > 0) vermerke.Add(Koerpervermerk.Loch);
            var punkte = new List<double[]>(aussen);
            var rand = new List<int[]>();
            for (int i = 0; i < aussen.Count; i++) rand.Add(new[] { i, (i + 1) % aussen.Count });
            foreach (List<double[]> l in loecher)
            {
                int start = punkte.Count;
                punkte.AddRange(l);
                for (int i = 0; i < l.Count; i++) rand.Add(new[] { start + i, start + (i + 1) % l.Count });
            }
            return new Teilflaeche(punkte, d, rand, kennung);
        }

        // ==================================================================
        //  Hüllprüfung
        // ==================================================================

        private static Koerperergebnis Fehlschlag(string grund, params string[] werte)
            => new Koerperergebnis { Grund = grund, Werte = werte ?? Array.Empty<string>() };

        /// <summary>
        /// Zusammenlegen, T-Teilung, Kantenpaarung, Ausrichtung, Abschluss. Die Reihenfolge der Dreiecke folgt den
        /// Teilflächen; ein geteiltes Dreieck wird an seiner Stelle durch seine Teile ersetzt.
        /// </summary>
        private static Koerperergebnis Schliessen(List<Teilflaeche> teile, string art, double tol, SortedSet<Koerpervermerk> vermerke)
        {
            // 1. Punkte zusammenlegen (Raster der Toleranz, Nachbarzellen; der kleinste Index gewinnt).
            var punkte = new List<double[]>();
            var raster = new Dictionary<(long, long, long), List<int>>();
            (long, long, long) Zelle(double[] p) => ((long)Math.Floor(p[0] / tol), (long)Math.Floor(p[1] / tol), (long)Math.Floor(p[2] / tol));
            int Punkt(double[] p)
            {
                (long x, long y, long z) = Zelle(p);
                int best = -1;
                for (long dx = -1; dx <= 1; dx++)
                    for (long dy = -1; dy <= 1; dy++)
                        for (long dz = -1; dz <= 1; dz++)
                            if (raster.TryGetValue((x + dx, y + dy, z + dz), out List<int> liste))
                                foreach (int i in liste)
                                {
                                    double[] q = punkte[i];
                                    if (Math.Abs(q[0] - p[0]) <= tol && Math.Abs(q[1] - p[1]) <= tol && Math.Abs(q[2] - p[2]) <= tol && (best < 0 || i < best)) best = i;
                                }
                if (best >= 0) return best;
                punkte.Add(p);
                if (!raster.TryGetValue((x, y, z), out List<int> l)) { l = new List<int>(); raster[(x, y, z)] = l; }
                l.Add(punkte.Count - 1);
                return punkte.Count - 1;
            }

            var dreiecke = new List<int[]>();
            var quelle = new List<string>();
            var rand = new List<int[]>();
            foreach (Teilflaeche t in teile)
            {
                int[] g = t.Punkte.Select(Punkt).ToArray();
                foreach (int[] d in t.Dreiecke)
                {
                    int a = g[d[0]], b = g[d[1]], c = g[d[2]];
                    if (a == b || b == c || a == c) continue;
                    dreiecke.Add(new[] { a, b, c });
                    quelle.Add(t.Kennung);
                }
                foreach (int[] k in t.Rand) if (g[k[0]] != g[k[1]]) rand.Add(new[] { g[k[0]], g[k[1]] });
            }
            if (dreiecke.Count == 0) return Fehlschlag(GRUND_LEER);

            // 2. T-Teilung: liegt ein Punkt im Inneren einer Dreieckskante (Abstand ≤ tol), wird das Dreieck dort geteilt.
            var geteilt = new List<int[]>(dreiecke.Count);
            var geteiltQuelle = new List<string>(dreiecke.Count);
            var stapel = new Stack<int[]>();
            for (int i = 0; i < dreiecke.Count; i++)
            {
                stapel.Push(dreiecke[i]);
                while (stapel.Count > 0)
                {
                    int[] d = stapel.Pop();
                    if (geteilt.Count + stapel.Count > Dateikoerper.DREIECKSGRENZE)
                        return Fehlschlag(GRUND_GRENZE, (geteilt.Count + stapel.Count).ToString(CultureInfo.InvariantCulture));
                    (int kante, int m) = TStoss(punkte, d, tol);
                    if (m < 0) { geteilt.Add(d); geteiltQuelle.Add(quelle[i]); continue; }
                    int a = d[kante], b = d[(kante + 1) % 3], c = d[(kante + 2) % 3];
                    stapel.Push(new[] { m, b, c });
                    stapel.Push(new[] { a, m, c });
                }
            }
            if (geteilt.Count > Dateikoerper.DREIECKSGRENZE)
                return Fehlschlag(GRUND_GRENZE, geteilt.Count.ToString(CultureInfo.InvariantCulture));

            // 3. Kantenpaarung: je ungerichtete Kante die Dreiecke daran samt Richtung (+1: kleiner → größer).
            var kanten = new Dictionary<(int, int), List<(int Dreieck, int Richtung)>>();
            var kantenfolge = new List<(int, int)>();
            for (int t = 0; t < geteilt.Count; t++)
                for (int e = 0; e < 3; e++)
                {
                    int p = geteilt[t][e], q = geteilt[t][(e + 1) % 3];
                    (int, int) s = p < q ? (p, q) : (q, p);
                    if (!kanten.TryGetValue(s, out var an)) { an = new List<(int, int)>(2); kanten[s] = an; kantenfolge.Add(s); }
                    an.Add((t, p < q ? 1 : -1));
                }
            int offen = kantenfolge.Count(s => kanten[s].Count != 2);
            if (offen > 0)
                return new Koerperergebnis
                {
                    Grund = GRUND_NICHT_GESCHLOSSEN,
                    Werte = new[] { offen.ToString(CultureInfo.InvariantCulture) },
                    OffeneKanten = offen,
                };

            // 4. Ausrichtung über die Kantenpaare: Nachbarn durchlaufen ihre gemeinsame Kante gegenläufig.
            var wenden = new int[geteilt.Count];   // 0 = offen, +1 = wie gegeben, -1 = gewendet
            var bestandteil = new int[geteilt.Count];
            int zahlBestandteile = 0;
            for (int start = 0; start < geteilt.Count; start++)
            {
                if (wenden[start] != 0) continue;
                wenden[start] = 1;
                bestandteil[start] = zahlBestandteile;
                var schlange = new Queue<int>();
                schlange.Enqueue(start);
                while (schlange.Count > 0)
                {
                    int t = schlange.Dequeue();
                    for (int e = 0; e < 3; e++)
                    {
                        int p = geteilt[t][e], q = geteilt[t][(e + 1) % 3];
                        (int, int) s = p < q ? (p, q) : (q, p);
                        int wirksam = (p < q ? 1 : -1) * wenden[t];
                        foreach ((int u, int richtung) in kanten[s])
                        {
                            if (u == t) continue;
                            int soll = -wirksam * richtung;   // wirksame Richtung bei u muss −wirksam sein
                            if (wenden[u] == 0)
                            {
                                wenden[u] = soll;
                                bestandteil[u] = zahlBestandteile;
                                schlange.Enqueue(u);
                            }
                            else if (wenden[u] != soll) return Fehlschlag(GRUND_NICHT_ORIENTIERBAR);
                        }
                    }
                }
                zahlBestandteile++;
            }
            var volumen = new double[zahlBestandteile];
            for (int t = 0; t < geteilt.Count; t++)
            {
                int[] d = wenden[t] > 0 ? geteilt[t] : new[] { geteilt[t][0], geteilt[t][2], geteilt[t][1] };
                volumen[bestandteil[t]] += Spat(punkte[d[0]], punkte[d[1]], punkte[d[2]]);
            }
            if (volumen.Any(v => Math.Abs(v) < 1e-12)) return Fehlschlag(GRUND_LEER);
            var ausgerichtet = new List<int[]>(geteilt.Count);
            for (int t = 0; t < geteilt.Count; t++)
            {
                bool gewendet = (wenden[t] < 0) != (volumen[bestandteil[t]] < 0.0);
                int[] d = geteilt[t];
                ausgerichtet.Add(gewendet ? new[] { d[0], d[2], d[1] } : d);
            }
            return new Koerperergebnis { Koerper = Abschluss(punkte, ausgerichtet, geteiltQuelle, rand, art, vermerke) };
        }

        /// <summary>Die erste Kante (0 bis 2) mit einem Punkt in ihrem Inneren und der Punkt nächst ihrem Anfang; sonst (−1, −1).</summary>
        private static (int Kante, int Punkt) TStoss(List<double[]> punkte, int[] d, double tol)
        {
            for (int e = 0; e < 3; e++)
            {
                double[] a = punkte[d[e]], b = punkte[d[(e + 1) % 3]];
                double[] ab = Polygonnetz.Minus(b, a);
                double l2 = Polygonnetz.Punkt(ab, ab);
                if (l2 <= tol * tol) continue;
                double minX = Math.Min(a[0], b[0]) - tol, maxX = Math.Max(a[0], b[0]) + tol;
                double minY = Math.Min(a[1], b[1]) - tol, maxY = Math.Max(a[1], b[1]) + tol;
                double minZ = Math.Min(a[2], b[2]) - tol, maxZ = Math.Max(a[2], b[2]) + tol;
                int best = -1;
                double bestT = double.MaxValue;
                for (int i = 0; i < punkte.Count; i++)
                {
                    if (i == d[0] || i == d[1] || i == d[2]) continue;
                    double[] m = punkte[i];
                    if (m[0] < minX || m[0] > maxX || m[1] < minY || m[1] > maxY || m[2] < minZ || m[2] > maxZ) continue;
                    double[] am = Polygonnetz.Minus(m, a);
                    double t = Polygonnetz.Punkt(am, ab) / l2;
                    if (t <= 0.0 || t >= 1.0) continue;
                    double[] lot = Polygonnetz.Minus(am, Polygonnetz.Mal(ab, t));
                    if (Polygonnetz.Punkt(lot, lot) > tol * tol) continue;
                    double[] mb = Polygonnetz.Minus(b, m);
                    if (Polygonnetz.Punkt(am, am) <= tol * tol || Polygonnetz.Punkt(mb, mb) <= tol * tol) continue;
                    if (t < bestT) { bestT = t; best = i; }
                }
                if (best >= 0) return (e, best);
            }
            return (-1, -1);
        }

        private static double Spat(double[] a, double[] b, double[] c)
            => (a[0] * (b[1] * c[2] - b[2] * c[1]) - a[1] * (b[0] * c[2] - b[2] * c[0]) + a[2] * (b[0] * c[1] - b[1] * c[0])) / 6.0;

        /// <summary>
        /// Gerundet auf <see cref="Zonenkoerper.STELLEN"/>, gleiche Punkte zusammengelegt, nur benutzte Punkte in der
        /// Reihenfolge ihres ersten Auftretens (Dreiecke, dann Randkanten); Normalen aus den gerundeten Punkten.
        /// </summary>
        private static Dateikoerper Abschluss(List<double[]> roh, List<int[]> dreiecke, List<string> quelle, List<int[]> rand,
                                              string art, SortedSet<Koerpervermerk> vermerke)
        {
            var punkte = new List<double[]>();
            var stelle = new Dictionary<(double, double, double), int>();
            var neu = new int[roh.Count];
            for (int i = 0; i < neu.Length; i++) neu[i] = -1;
            int Neu(int i)
            {
                if (neu[i] >= 0) return neu[i];
                double[] q = { R(roh[i][0]), R(roh[i][1]), R(roh[i][2]) };
                (double, double, double) s = (q[0], q[1], q[2]);
                if (!stelle.TryGetValue(s, out int j)) { j = punkte.Count; stelle[s] = j; punkte.Add(q); }
                return neu[i] = j;
            }
            var d = new List<int[]>(dreiecke.Count);
            var normalen = new List<double[]>(dreiecke.Count);
            foreach (int[] t in dreiecke)
            {
                int[] x = { Neu(t[0]), Neu(t[1]), Neu(t[2]) };
                d.Add(x);
                double[] n = Polygonnetz.Normiert(Polygonnetz.Kreuz(Polygonnetz.Minus(punkte[x[1]], punkte[x[0]]), Polygonnetz.Minus(punkte[x[2]], punkte[x[0]])))
                             ?? new[] { 0.0, 0.0, 0.0 };
                normalen.Add(new[] { R(n[0]), R(n[1]), R(n[2]) });
            }
            var kanten = new List<int[]>();
            var gesehen = new HashSet<(int, int)>();
            foreach (int[] k in rand)
            {
                int a = Neu(k[0]), b = Neu(k[1]);
                if (a == b) continue;
                (int, int) s = a < b ? (a, b) : (b, a);
                if (gesehen.Add(s)) kanten.Add(new[] { s.Item1, s.Item2 });
            }
            return new Dateikoerper
            {
                PunkteM = punkte,
                Dreiecke = d,
                Normalen = normalen,
                Randkanten = kanten,
                Art = art,
                Vermerke = vermerke.ToList(),
                Quelle = Koerperquelle.AusFlaechen,
                Quellflaechen = quelle.ToList(),
            };
        }

        private static double R(double v) => Math.Round(v, Zonenkoerper.STELLEN, MidpointRounding.ToEven) + 0.0;
    }
}

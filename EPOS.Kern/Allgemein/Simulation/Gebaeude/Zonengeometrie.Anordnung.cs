using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    // ======================================================================================
    //  Das Zonengeometrie-Modell — Aneinanderlegen der Räume mit gemeinsamer Trennfläche (Stufe G7b)
    // ======================================================================================

    /// <summary>
    /// Die Strecke einer Wand auf einer Kante des schematischen Rechtecks [m], vom Startpunkt der Kante
    /// gemessen (Stufe G7b). Die Strecken einer Kante liegen lückenlos hintereinander.
    /// </summary>
    /// <param name="Verweis">Die Wand.</param>
    /// <param name="VonM">Beginn der Strecke [m].</param>
    /// <param name="BisM">Ende der Strecke [m].</param>
    internal sealed record Kantenabschnitt(Grenzverweis Verweis, double VonM, double BisM);

    /// <summary>
    /// <b>Ein Nachbarpaar</b> (Mehrzonenkonzept M13; Datenaustauschkonzept 5.5 Punkt 3): eine Trennfläche, die
    /// genau zwei Räume teilen — beide Seiten mit dem nachgezogenen Gegenstück. Raum A ist der mit der kleineren
    /// Kennung (Ordinalvergleich).
    /// </summary>
    internal sealed class Nachbarpaar
    {
        /// <summary>Die Kennung von Raum A.</summary>
        internal string RaumA { get; init; } = "";

        /// <summary>Die Kennung von Raum B.</summary>
        internal string RaumB { get; init; } = "";

        /// <summary>Die Kennung des Bauteils der Trennfläche.</summary>
        internal string BauteilKennung { get; init; } = "";

        /// <summary>Die Seite in Raum A; ihr Gegenstück ist die Seite in Raum B.</summary>
        internal Grenzverweis VerweisA { get; init; }

        /// <summary>Die Seite in Raum B; ihr Gegenstück ist die Seite in Raum A.</summary>
        internal Grenzverweis VerweisB { get; init; }

        /// <summary>Liegen die beiden schematischen Rechtecke an dieser Fläche deckungsgleich aneinander?</summary>
        internal bool Angelegt { get; init; }

        /// <summary>Ist das Aneinanderlegen der Gruppe dieses Paars benannt abgelehnt (widersprüchliche Anordnung)?</summary>
        internal bool Abgelehnt { get; init; }

        /// <summary>Ist an diesem Paar der Widerspruch aufgetreten (eines je abgelehnter Gruppe)?</summary>
        internal bool Widerspruch { get; init; }

        /// <summary>
        /// Liegt die Trennwand bei beiden Räumen <b>nicht</b> auf gegenüberliegenden Himmelsseiten? Dann ist das Paar
        /// nicht angelegt — gedreht wird nie, der Azimut einer Wand ist eine physikalische Eigenschaft —, beide stehen
        /// getrennt; die Gegenstücke sind trotzdem nachgezogen.
        /// </summary>
        internal bool SeitenUnpassend { get; init; }
    }

    internal sealed partial class Zonengeometrie
    {
        /// <summary>I — {0} Zahl, {1} Beispiele: Paare schematischer Räume mit gemeinsamer Trennfläche liegen deckungsgleich aneinander.</summary>
        internal const string ANGELEGT = "ZGEO_ANGELEGT";
        /// <summary>I — {0} Zahl, {1} Beispiele: Paare, deren Trennwand bei beiden Räumen nicht auf gegenüberliegenden Himmelsseiten liegt — nicht angelegt.</summary>
        internal const string NICHT_ANGELEGT = "ZGEO_NICHT_ANGELEGT";
        /// <summary>W — {0} Zahl, {1} Beispiele: widersprüchliche Anordnung — die Gruppe ist nicht aneinandergelegt (benannte Ablehnung).</summary>
        internal const string ANORDNUNG_ABGELEHNT = "ZGEO_ANORDNUNG_ABGELEHNT";

        /// <summary>Toleranz [m] für Deckungsgleichheit und Überlappung beim Aneinanderlegen.</summary>
        internal const double ANLAGE_TOLERANZ_M = 1e-6;

        /// <summary>Die Kante des Rechtecks je Sektor: 0 Nord → 2, 1 Ost → 1, 2 Süd → 0, 3 West → 3.</summary>
        private static readonly int[] KanteJeSektor = { 2, 1, 0, 3 };

        /// <summary>Der Azimut der Außennormalen je Kante: 0 Süd, 1 Ost, 2 Nord, 3 West.</summary>
        private static readonly double[] AzimutJeRichtung = { 180.0, 90.0, 0.0, 270.0 };

        // ------------------------------------------------------------------
        //  Paare
        // ------------------------------------------------------------------

        /// <summary>Der Arbeitsstand eines Nachbarpaars.</summary>
        private sealed class Paarentwurf
        {
            internal Umrissentwurf A, B;
            internal Umrissseite SeiteA, SeiteB;
            internal string Bauteil = "";
            internal bool Wand;
            internal bool Angelegt, Abgelehnt, Widerspruch, SeitenUnpassend;

            internal Nachbarpaar Abschluss(Dictionary<Grenzverweis, Grenzverweis> nachgezogen) => new Nachbarpaar
            {
                RaumA = A.Raum.Kennung ?? "",
                RaumB = B.Raum.Kennung ?? "",
                BauteilKennung = Bauteil,
                VerweisA = Neu(nachgezogen, SeiteA.Verweis),
                VerweisB = Neu(nachgezogen, SeiteB.Verweis),
                Angelegt = Angelegt,
                Abgelehnt = Abgelehnt,
                Widerspruch = Widerspruch,
                SeitenUnpassend = SeitenUnpassend,
            };

            internal string Name => A.Raum.Name + " – " + B.Raum.Name;
        }

        /// <summary>Eine Gruppe aneinandergelegter Rechtecke (oder ein einzelnes) mit ihrem Kasten.</summary>
        private sealed class Rechteckblock
        {
            internal readonly List<Umrissentwurf> Glieder = new List<Umrissentwurf>();
            internal double MinX, MinY, MaxX, MaxY;
            internal double Breite => MaxX - MinX;
            internal double Tiefe => MaxY - MinY;

            internal void Kasten()
            {
                MinX = Glieder.Min(e => e.X);
                MinY = Glieder.Min(e => e.Y);
                MaxX = Glieder.Max(e => e.X + e.L);
                MaxY = Glieder.Max(e => e.Y + e.B);
            }
        }

        /// <summary>
        /// Die Nachbarpaare: zuerst die Gegenstücke der Datei (<see cref="Grenzverweis.GegenstueckKennung"/>),
        /// dann je Bauteil, das genau zwei Seiten in zwei verschiedenen Räumen trägt (die Rekonstruktion nach M13).
        /// Innere Masse eines Raums (beide Seiten im selben Raum) und Bauteile an mehr als zwei Seiten bilden kein
        /// Paar. Geordnet nach Bauteilkennung, dann nach den Raumkennungen — unabhängig von der Reihenfolge der
        /// Listen.
        /// </summary>
        private static List<Paarentwurf> Paare(List<Umrissentwurf> entwurf)
        {
            var seiten = new List<(Umrissentwurf E, Umrissseite S)>();
            foreach (Umrissentwurf e in entwurf)
                foreach (Umrissseite s in e.Raum.Seiten)
                    if (s.Verweis != null && !string.IsNullOrEmpty(s.Verweis.BauteilKennung)) seiten.Add((e, s));

            var vergeben = new HashSet<Umrissseite>();
            var paare = new List<Paarentwurf>();
            void Paar((Umrissentwurf E, Umrissseite S) x, (Umrissentwurf E, Umrissseite S) y)
            {
                if (ReferenceEquals(x.E, y.E) || vergeben.Contains(x.S) || vergeben.Contains(y.S)) return;
                if (string.CompareOrdinal(x.E.Raum.Kennung ?? "", y.E.Raum.Kennung ?? "") > 0) (x, y) = (y, x);
                vergeben.Add(x.S);
                vergeben.Add(y.S);
                paare.Add(new Paarentwurf
                {
                    A = x.E, B = y.E, SeiteA = x.S, SeiteB = y.S,
                    Bauteil = x.S.Verweis.BauteilKennung,
                    Wand = x.S.Verweis.Stellung == Grenzstellung.Wand && y.S.Verweis.Stellung == Grenzstellung.Wand
                           && x.E.Herkunft == Geometrieherkunft.Schematisch && y.E.Herkunft == Geometrieherkunft.Schematisch
                           && x.E.L > 0.0 && y.E.L > 0.0 && x.E.Geschoss == y.E.Geschoss,
                });
            }

            // 1. Die Gegenstücke der Datei.
            var jeKennung = new Dictionary<string, List<(Umrissentwurf E, Umrissseite S)>>(StringComparer.Ordinal);
            foreach (var x in seiten)
            {
                string k = x.S.Verweis.Kennung ?? "";
                if (!jeKennung.TryGetValue(k, out var liste)) jeKennung[k] = liste = new List<(Umrissentwurf, Umrissseite)>();
                liste.Add(x);
            }
            foreach (var x in seiten.Where(x => !string.IsNullOrEmpty(x.S.Verweis.GegenstueckKennung))
                                    .OrderBy(x => x.S.Verweis.Kennung ?? "", StringComparer.Ordinal))
                if (jeKennung.TryGetValue(x.S.Verweis.GegenstueckKennung, out var gegen) && gegen.Count == 1)
                    Paar(x, gegen[0]);

            // 2. Je Bauteil genau zwei Seiten in zwei Räumen.
            foreach (var gruppe in seiten.Where(x => !vergeben.Contains(x.S))
                                         .GroupBy(x => x.S.Verweis.BauteilKennung, StringComparer.Ordinal)
                                         .OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                List<(Umrissentwurf E, Umrissseite S)> liste = gruppe.ToList();
                if (liste.Count == 2) Paar(liste[0], liste[1]);
            }

            return paare.OrderBy(p => p.Bauteil, StringComparer.Ordinal)
                        .ThenBy(p => p.A.Raum.Kennung ?? "", StringComparer.Ordinal)
                        .ThenBy(p => p.B.Raum.Kennung ?? "", StringComparer.Ordinal)
                        .ThenBy(p => p.SeiteA.Verweis.Kennung ?? "", StringComparer.Ordinal)
                        .ToList();
        }

        /// <summary>
        /// Die Kante jeder Wand am schematischen Rechteck: die ihres Sektors. Eine Trennwand ohne Sektor bekommt
        /// die Gegenkante ihrer Gegenseite; fehlt beiden der Sektor, steht sie in Raum A an der Ost-, in Raum B an
        /// der Westkante. Liegt die Trennwand danach bei beiden nicht auf gegenüberliegenden Kanten, ist das Paar
        /// <see cref="Paarentwurf.SeitenUnpassend"/> und wird nicht angelegt — gedreht wird nie.
        /// </summary>
        private static void Kantenzuordnung(List<Umrissentwurf> entwurf, List<Paarentwurf> paare)
        {
            foreach (Umrissentwurf e in entwurf)
            {
                if (e.Herkunft != Geometrieherkunft.Schematisch || !(e.L > 0.0)) continue;
                foreach (Umrissseite s in e.Raum.Seiten)
                    if (s.Verweis?.Stellung == Grenzstellung.Wand && s.Sektor is int sektor && sektor >= 0 && sektor < 4)
                        e.Kante[s] = KanteJeSektor[sektor];
            }
            foreach (Paarentwurf p in paare.Where(p => p.Wand))
            {
                bool a = p.A.Kante.TryGetValue(p.SeiteA, out int ka), b = p.B.Kante.TryGetValue(p.SeiteB, out int kb);
                if (a && b) continue;
                if (!a && !b)
                {
                    p.A.Kante[p.SeiteA] = 1;
                    p.B.Kante[p.SeiteB] = 3;
                }
                else if (!a) p.A.Kante[p.SeiteA] = (kb + 2) % 4;
                else p.B.Kante[p.SeiteB] = (ka + 2) % 4;
            }
            foreach (Paarentwurf p in paare.Where(p => p.Wand))
                if (p.A.Kante.TryGetValue(p.SeiteA, out int ka) && p.B.Kante.TryGetValue(p.SeiteB, out int kb) && kb != (ka + 2) % 4)
                    p.SeitenUnpassend = true;
        }

        // ------------------------------------------------------------------
        //  Aneinanderlegen
        // ------------------------------------------------------------------

        /// <summary>
        /// <b>Legt die Rechtecke eines Geschosses mit gemeinsamer Trennwand aneinander</b> und liefert die Blöcke
        /// in der Reihenfolge des ersten Glieds in <paramref name="ersatz"/>. Angelegt werden nur Paare, deren
        /// Trennwand bei beiden Räumen auf gegenüberliegenden Kanten liegt; gedreht wird nie (der Azimut einer Wand
        /// ist eine physikalische Eigenschaft, die Anordnung nur schematisch). Je Gruppe (Zusammenhang über die
        /// Paare) steht der Raum mit der kleinsten Kennung im Ursprung; die übrigen folgen in Breitensuche, je Raum
        /// die Paare nach Partnerkennung und Bauteil geordnet:
        /// <list type="number">
        /// <item>der Partner wird flächentreu gestreckt, bis die Strecke der Trennwand auf seiner Kante so lang ist wie
        /// auf der Kante des gelegten Raums;</item>
        /// <item>er wird so verschoben, dass beide Strecken deckungsgleich liegen.</item>
        /// </list>
        /// Ein Paar, dessen beide Räume schon liegen, muss deckungsgleich sein; ein neu gelegter Raum darf keinen
        /// gelegten überlappen (Toleranz <see cref="ANLAGE_TOLERANZ_M"/>). Sonst ist die Anordnung
        /// <b>widersprüchlich</b>: die ganze Gruppe wird benannt abgelehnt und gereiht wie ohne Nachbarn.
        /// </summary>
        private static List<Rechteckblock> Anlegen(List<Umrissentwurf> ersatz, List<Paarentwurf> paare)
        {
            var hier = new HashSet<Umrissentwurf>(ersatz);
            List<Paarentwurf> eigene = paare.Where(p => p.Wand && !p.SeitenUnpassend && hier.Contains(p.A) && hier.Contains(p.B)
                                                        && p.A.Kante.ContainsKey(p.SeiteA) && p.B.Kante.ContainsKey(p.SeiteB)).ToList();
            var blockVon = new Dictionary<Umrissentwurf, Rechteckblock>();
            foreach (Umrissentwurf start in ersatz)
            {
                if (blockVon.ContainsKey(start)) continue;
                // Die Gruppe über die Paare.
                var glieder = new HashSet<Umrissentwurf> { start };
                var offen = new Queue<Umrissentwurf>();
                offen.Enqueue(start);
                while (offen.Count > 0)
                {
                    Umrissentwurf e = offen.Dequeue();
                    foreach (Paarentwurf p in eigene)
                    {
                        Umrissentwurf partner = ReferenceEquals(p.A, e) ? p.B : ReferenceEquals(p.B, e) ? p.A : null;
                        if (partner != null && glieder.Add(partner)) offen.Enqueue(partner);
                    }
                }
                List<Paarentwurf> gruppe = eigene.Where(p => glieder.Contains(p.A)).ToList();
                List<Umrissentwurf> folge = ersatz.Where(glieder.Contains).ToList();
                foreach (Umrissentwurf e in folge)
                {
                    e.X = 0.0;
                    e.Y = 0.0;
                }
                if (gruppe.Count > 0 && Auslegen(folge, gruppe))
                {
                    foreach (Paarentwurf p in gruppe) p.Angelegt = true;
                    var block = new Rechteckblock();
                    foreach (Umrissentwurf e in folge)
                    {
                        e.Angelegt = true;
                        block.Glieder.Add(e);
                        blockVon[e] = block;
                    }
                    block.Kasten();
                    continue;
                }
                if (gruppe.Count > 0)
                {
                    foreach (Paarentwurf p in gruppe) p.Abgelehnt = true;
                    foreach (Umrissentwurf e in folge)
                    {
                        (double l, double b, _) = Rechteck(e.Raum);
                        e.L = l;
                        e.B = b;
                        e.X = 0.0;
                        e.Y = 0.0;
                    }
                }
                foreach (Umrissentwurf e in folge)
                {
                    var einzeln = new Rechteckblock();
                    einzeln.Glieder.Add(e);
                    einzeln.Kasten();
                    blockVon[e] = einzeln;
                }
            }
            var bloecke = new List<Rechteckblock>();
            foreach (Umrissentwurf e in ersatz)
                if (!bloecke.Contains(blockVon[e])) bloecke.Add(blockVon[e]);
            return bloecke;
        }

        /// <summary>Legt eine Gruppe aus (Regeln: <see cref="Anlegen"/>); <c>false</c> = widersprüchlich, das Paar ist markiert.</summary>
        private static bool Auslegen(List<Umrissentwurf> glieder, List<Paarentwurf> gruppe)
        {
            Umrissentwurf wurzel = glieder.OrderBy(e => e.Raum.Kennung ?? "", StringComparer.Ordinal).First();
            var gelegt = new List<Umrissentwurf> { wurzel };
            var offen = new Queue<Umrissentwurf>();
            offen.Enqueue(wurzel);
            while (offen.Count > 0)
            {
                Umrissentwurf a = offen.Dequeue();
                var anA = gruppe.Where(p => ReferenceEquals(p.A, a) || ReferenceEquals(p.B, a))
                                .Select(p => ReferenceEquals(p.A, a)
                                    ? (Paar: p, Partner: p.B, Eigen: p.SeiteA, Fremd: p.SeiteB)
                                    : (Paar: p, Partner: p.A, Eigen: p.SeiteB, Fremd: p.SeiteA))
                                .OrderBy(x => x.Partner.Raum.Kennung ?? "", StringComparer.Ordinal)
                                .ThenBy(x => x.Paar.Bauteil, StringComparer.Ordinal)
                                .ToList();
                foreach (var x in anA)
                {
                    if (!gelegt.Contains(x.Partner))
                    {
                        if (!Legen(a, x.Eigen, x.Partner, x.Fremd) || gelegt.Any(g => Ueberlappt(g, x.Partner)))
                        {
                            x.Paar.Widerspruch = true;
                            return false;
                        }
                        gelegt.Add(x.Partner);
                        offen.Enqueue(x.Partner);
                    }
                    else if (!Deckungsgleich(a, x.Eigen, x.Partner, x.Fremd))
                    {
                        x.Paar.Widerspruch = true;
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Streckt und verschiebt <paramref name="b"/> so, dass seine Seite <paramref name="sb"/> auf der Seite
        /// <paramref name="sa"/> des gelegten <paramref name="a"/> liegt; beide Kanten liegen einander gegenüber
        /// (<see cref="Kantenzuordnung"/>).
        /// </summary>
        private static bool Legen(Umrissentwurf a, Umrissseite sa, Umrissentwurf b, Umrissseite sb)
        {
            int ka = a.Kante[sa], kb = b.Kante[sb];
            if (kb != (ka + 2) % 4) return false;

            (double vonA, double bisA) = Strecke(a, sa);
            (double anteilVon, double anteilBis) = Anteil(b, sb);
            double laenge = (bisA - vonA) / (anteilBis - anteilVon);
            if (!(laenge > 0.0) || double.IsInfinity(laenge)) return false;
            double flaeche = b.L * b.B;
            if (kb % 2 == 0)
            {
                b.L = laenge;
                b.B = flaeche / laenge;
            }
            else
            {
                b.B = laenge;
                b.L = flaeche / laenge;
            }

            (double[] startA, double[] richtung) = Kante(a, ka);
            double versatz = bisA + laenge * anteilVon;
            double px = startA[0] + richtung[0] * versatz, py = startA[1] + richtung[1] * versatz;
            double[] ecke = Eckversatz(b, kb);
            b.X = px - ecke[0];
            b.Y = py - ecke[1];
            return true;
        }

        /// <summary>Liegen die Strecken einer Trennwand an beiden gelegten Räumen deckungsgleich und gegenläufig?</summary>
        private static bool Deckungsgleich(Umrissentwurf a, Umrissseite sa, Umrissentwurf b, Umrissseite sb)
        {
            (double[] a0, double[] a1) = Strecke3(a, sa);
            (double[] b0, double[] b1) = Strecke3(b, sb);
            return Abstand(a0, b1) <= ANLAGE_TOLERANZ_M && Abstand(a1, b0) <= ANLAGE_TOLERANZ_M;
        }

        private static bool Ueberlappt(Umrissentwurf x, Umrissentwurf y)
            => Math.Min(x.X + x.L, y.X + y.L) - Math.Max(x.X, y.X) > ANLAGE_TOLERANZ_M
            && Math.Min(x.Y + x.B, y.Y + y.B) - Math.Max(x.Y, y.Y) > ANLAGE_TOLERANZ_M;

        // ------------------------------------------------------------------
        //  Das Rechteck
        // ------------------------------------------------------------------

        /// <summary>Der Versatz der Ecke c vom Ursprung des Rechtecks: 0 Südwest, 1 Südost, 2 Nordost, 3 Nordwest.</summary>
        private static double[] Eckversatz(Umrissentwurf e, int c)
        {
            double w = e.L, h = e.B;
            switch (c)
            {
                case 1: return new[] { w, 0.0 };
                case 2: return new[] { w, h };
                case 3: return new[] { 0.0, h };
                default: return new[] { 0.0, 0.0 };
            }
        }

        private static double[] Ecke(Umrissentwurf e, int c)
        {
            double[] v = Eckversatz(e, c);
            return new[] { e.X + v[0], e.Y + v[1] };
        }

        /// <summary>Startpunkt und Einheitsrichtung der Kante k in Weltkoordinaten.</summary>
        private static (double[] Start, double[] Richtung) Kante(Umrissentwurf e, int k)
        {
            double[] a = Ecke(e, k), b = Ecke(e, (k + 1) % 4);
            double dx = b[0] - a[0], dy = b[1] - a[1], l = Math.Sqrt(dx * dx + dy * dy);
            return (a, new[] { dx / l, dy / l });
        }

        private static double Kantenlaenge(Umrissentwurf e, int k) => k % 2 == 0 ? e.L : e.B;

        /// <summary>Die Strecke einer Wand auf ihrer Kante [m].</summary>
        private static (double Von, double Bis) Strecke(Umrissentwurf e, Umrissseite s)
        {
            int k = e.Kante[s];
            foreach (var x in Abschnitte(e, k, Kantenlaenge(e, k)))
                if (ReferenceEquals(x.Seite, s)) return (x.Von, x.Bis);
            return (0.0, 0.0);
        }

        /// <summary>Der Anteil einer Wand an ihrer Kante (0 … 1).</summary>
        private static (double Von, double Bis) Anteil(Umrissentwurf e, Umrissseite s)
        {
            foreach (var x in Abschnitte(e, e.Kante[s], 1.0))
                if (ReferenceEquals(x.Seite, s)) return (x.Von, x.Bis);
            return (0.0, 0.0);
        }

        private static (double[] Von, double[] Bis) Strecke3(Umrissentwurf e, Umrissseite s)
        {
            (double[] start, double[] u) = Kante(e, e.Kante[s]);
            (double von, double bis) = Strecke(e, s);
            return (new[] { start[0] + u[0] * von, start[1] + u[1] * von }, new[] { start[0] + u[0] * bis, start[1] + u[1] * bis });
        }

        private static double Abstand(double[] a, double[] b) => Math.Sqrt((a[0] - b[0]) * (a[0] - b[0]) + (a[1] - b[1]) * (a[1] - b[1]));

        /// <summary>
        /// Die Strecken der Wände einer Kante: nach Kennung, dann Bauteilkennung geordnet, lückenlos, je Wand im
        /// Verhältnis ihrer Fläche — fehlt einer Wand die Fläche, gleich lang. Innere Masse desselben Raums (ein
        /// Bauteil an mehreren Wandseiten des Raums) steht auf keiner Strecke.
        /// </summary>
        private static List<(Umrissseite Seite, double Von, double Bis)> Abschnitte(Umrissentwurf e, int k, double laenge)
        {
            List<Umrissseite> waende = e.Raum.Seiten
                .Where(s => e.Kante.TryGetValue(s, out int kk) && kk == k && !InnereMasse(e, s))
                .OrderBy(s => s.Verweis.Kennung ?? "", StringComparer.Ordinal)
                .ThenBy(s => s.Verweis.BauteilKennung ?? "", StringComparer.Ordinal)
                .ToList();
            var ergebnis = new List<(Umrissseite, double, double)>(waende.Count);
            if (waende.Count == 0) return ergebnis;
            bool mitFlaeche = waende.All(s => s.FlaecheM2 > 0.0);
            double summe = 0.0;
            foreach (Umrissseite s in waende) summe += mitFlaeche ? s.FlaecheM2.Value : 1.0;
            double lauf = 0.0;
            foreach (Umrissseite s in waende)
            {
                double von = laenge * lauf / summe;
                lauf += mitFlaeche ? s.FlaecheM2.Value : 1.0;
                ergebnis.Add((s, von, laenge * lauf / summe));
            }
            return ergebnis;
        }

        private static bool InnereMasse(Umrissentwurf e, Umrissseite s)
            => e.Raum.Seiten.Count(x => x.Verweis?.Stellung == Grenzstellung.Wand
                                        && string.Equals(x.Verweis.BauteilKennung, s.Verweis.BauteilKennung, StringComparison.Ordinal)) > 1;

        /// <summary>
        /// Das Polygon des Rechtecks an seiner Lage: die Punkte gegen den Uhrzeigersinn ab der Ecke, an der Kante 0
        /// beginnt; Kante i (0 Süd, 1 Ost, 2 Nord, 3 West) mit Länge, Azimut ihrer Richtung, Wänden und Strecken —
        /// die Richtung der Kante und der Azimut ihrer Wände stimmen immer überein.
        /// </summary>
        private static void Rechteckpolygon(Umrissentwurf e)
        {
            var punkte = new List<double[]>(4);
            for (int i = 0; i < 4; i++) punkte.Add(Ecke(e, i));
            var jeKante = new List<Grenzverweis>[] { new List<Grenzverweis>(), new List<Grenzverweis>(), new List<Grenzverweis>(), new List<Grenzverweis>() };
            var ohne = new List<Grenzverweis>();
            foreach (Umrissseite s in e.Raum.Seiten)
            {
                if (s.Verweis == null || s.Verweis.Stellung == Grenzstellung.Boden || s.Verweis.Stellung == Grenzstellung.Decke) continue;
                if (e.Kante.TryGetValue(s, out int k)) jeKante[k].Add(s.Verweis);
                else ohne.Add(s.Verweis);
            }
            var kanten = new List<Umrisskante>(4);
            for (int i = 0; i < 4; i++)
            {
                double laenge = Kantenlaenge(e, i);
                kanten.Add(new Umrisskante(i, laenge, AzimutJeRichtung[i], jeKante[i])
                {
                    Abschnitte = Abschnitte(e, i, laenge).Select(x => new Kantenabschnitt(x.Seite.Verweis, x.Von, x.Bis)).ToList(),
                });
            }
            e.Polygone = new List<Umrisspolygon> { new Umrisspolygon(punkte, kanten, e.L * e.B, null, null) };
            e.OhneKante = ohne;
            e.BodenStreifen = Streifen(e, Grenzstellung.Boden);
            e.DeckenStreifen = Streifen(e, Grenzstellung.Decke);
        }

        /// <summary>Die Boden- bzw. Deckengrenzen als Streifen entlang der Kante 0 — Ordnung und Anteile wie <see cref="Abschnitte"/>.</summary>
        private static List<Kantenabschnitt> Streifen(Umrissentwurf e, Grenzstellung stellung)
        {
            List<Umrissseite> seiten = e.Raum.Seiten.Where(s => s.Verweis?.Stellung == stellung)
                .OrderBy(s => s.Verweis.Kennung ?? "", StringComparer.Ordinal)
                .ThenBy(s => s.Verweis.BauteilKennung ?? "", StringComparer.Ordinal)
                .ToList();
            var ergebnis = new List<Kantenabschnitt>(seiten.Count);
            bool mitFlaeche = seiten.All(s => s.FlaecheM2 > 0.0);
            double summe = 0.0, lauf = 0.0;
            foreach (Umrissseite s in seiten) summe += mitFlaeche ? s.FlaecheM2.Value : 1.0;
            foreach (Umrissseite s in seiten)
            {
                double von = e.L * lauf / summe;
                lauf += mitFlaeche ? s.FlaecheM2.Value : 1.0;
                ergebnis.Add(new Kantenabschnitt(s.Verweis, von, e.L * lauf / summe));
            }
            return ergebnis;
        }

        // ------------------------------------------------------------------
        //  Gegenstücke und Meldungen
        // ------------------------------------------------------------------

        /// <summary>Je Seite eines Paars ohne Gegenstück der Datei der Verweis mit der Kennung der Gegenseite.</summary>
        private static Dictionary<Grenzverweis, Grenzverweis> Nachziehen(List<Paarentwurf> paare)
        {
            var neu = new Dictionary<Grenzverweis, Grenzverweis>(ReferenceEqualityComparer.Instance);
            foreach (Paarentwurf p in paare)
            {
                Grenzverweis a = p.SeiteA.Verweis, b = p.SeiteB.Verweis;
                if (string.IsNullOrEmpty(a.GegenstueckKennung)) neu[a] = a with { GegenstueckKennung = b.Kennung };
                if (string.IsNullOrEmpty(b.GegenstueckKennung)) neu[b] = b with { GegenstueckKennung = a.Kennung };
            }
            return neu;
        }

        private static Grenzverweis Neu(Dictionary<Grenzverweis, Grenzverweis> neu, Grenzverweis v)
            => v != null && neu.TryGetValue(v, out Grenzverweis n) ? n : v;

        /// <summary>Setzt die nachgezogenen Verweise in Polygone, Kanten, Strecken, Boden, Decke und die Grenzen ohne Kante ein.</summary>
        private static void Ersetzen(Umrissentwurf e, Dictionary<Grenzverweis, Grenzverweis> neu)
        {
            if (neu.Count == 0) return;
            e.Boden = e.Boden.Select(v => Neu(neu, v)).ToList();
            e.Decke = e.Decke.Select(v => Neu(neu, v)).ToList();
            e.OhneKante = e.OhneKante.Select(v => Neu(neu, v)).ToList();
            e.BodenStreifen = e.BodenStreifen.Select(x => x with { Verweis = Neu(neu, x.Verweis) }).ToList();
            e.DeckenStreifen = e.DeckenStreifen.Select(x => x with { Verweis = Neu(neu, x.Verweis) }).ToList();
            e.Polygone = e.Polygone.Select(p => new Umrisspolygon(
                p.Punkte,
                p.Kanten.Select(k => new Umrisskante(k.Index, k.LaengeM, k.AzimutGrad, k.Grenzen.Select(v => Neu(neu, v)).ToList())
                {
                    Abschnitte = k.Abschnitte.Select(x => x with { Verweis = Neu(neu, x.Verweis) }).ToList(),
                }).ToList(),
                p.FlaecheM2, p.EbeneM, Neu(neu, p.Quelle))).ToList();
        }

        private static IEnumerable<PruefMeldung> Paarmeldungen(List<Paarentwurf> paare)
        {
            PruefMeldung Sammel(string schluessel, PruefStufe stufe, List<string> namen)
            {
                if (namen.Count == 0) return null;
                string beispiele = namen.Count <= 5 ? string.Join(", ", namen) : string.Join(", ", namen.Take(5)) + ", …";
                return new PruefMeldung(stufe, schluessel, namen.Count.ToString(CultureInfo.InvariantCulture), beispiele);
            }
            return new[]
            {
                Sammel(ANGELEGT, PruefStufe.Info, paare.Where(p => p.Angelegt).Select(p => p.Name).ToList()),
                Sammel(NICHT_ANGELEGT, PruefStufe.Info, paare.Where(p => p.SeitenUnpassend).Select(p => p.Name).ToList()),
                Sammel(ANORDNUNG_ABGELEHNT, PruefStufe.Warnung, paare.Where(p => p.Widerspruch).Select(p => p.Name).ToList()),
            }.Where(m => m != null);
        }
    }
}

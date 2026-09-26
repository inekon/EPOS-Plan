using System.Globalization;
using EPOS.UI.Dialoge.Bedarf;

namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Die Zeichenrechnung der Grundrissansicht</b> (Baustein <see cref="GebaeudeAnsicht"/>, Stufe G6c, Welle D2;
/// Mehrzonenkonzept 6.7): Ausschnitt, Punkte und Lage der Beschriftung — rein, ohne Oberfläche und ohne
/// Zustand, damit dieselben Daten in jeder Sprache zeichengleich dasselbe Bild ergeben.
///
/// <para><b>Koordinaten.</b> Die Daten stehen in Metern, x nach Osten, y nach Norden. SVG zählt y nach unten;
/// die Zeichnung spiegelt deshalb y (<c>−y</c>), damit Nord oben steht. Der Ausschnitt (<c>viewBox</c>) ist die
/// Ausdehnung des Geschosses mit einem Rand von <see cref="RANDANTEIL"/> der größeren Seite, jede Seite
/// mindestens <see cref="MINDESTMASS_M"/>.</para>
///
/// <para><b>Zahlen</b> stehen invariant mit <see cref="STELLEN"/> Nachkommastellen (Millimeter), eine negative
/// Null als Null — kein Zeichen der Zeichnung hängt an der Oberflächensprache.</para>
///
/// <para><b>Die Beschriftung</b> (Name, darunter die Fläche) steht im Flächenschwerpunkt des größten Polygons
/// eines Raums, und nur, wenn ihr geschätzter Kasten ganz im Polygon liegt; sonst trägt der Raum seinen Namen
/// allein in Kurztext und Beschreibung. Die Schrift wächst mit dem Geschoss (<see cref="SCHRIFTANTEIL"/>), damit
/// sie bei jeder Gebäudegröße gleich groß auf dem Schirm steht.</para>
/// </summary>
internal static class GebaeudeAnsichtZeichnung
{
    private static readonly CultureInfo INV = CultureInfo.InvariantCulture;

    /// <summary>Nachkommastellen jeder Zahl im Bild (Millimeter).</summary>
    internal const int STELLEN = 3;

    /// <summary>Kleinste Seite eines Ausschnitts [m] — ein Geschoss aus einer Linie bleibt zeichenbar.</summary>
    internal const double MINDESTMASS_M = 1.0;

    /// <summary>Der Rand um den Grundriss als Anteil der größeren Seite.</summary>
    internal const double RANDANTEIL = 0.04;

    /// <summary>Die Schriftgröße des Raumnamens als Anteil der größeren Seite des Geschosses.</summary>
    internal const double SCHRIFTANTEIL = 0.028;

    /// <summary>Die Schrift der Flächenzeile relativ zum Namen.</summary>
    internal const double FLAECHENSCHRIFT = 0.85;

    /// <summary>Geschätzte mittlere Zeichenbreite als Anteil der Schriftgröße.</summary>
    private const double ZEICHENBREITE = 0.6;

    /// <summary>Die Grundlinie des Namens über dem Schwerpunkt, als Anteil der Schrift (Bildkoordinaten).</summary>
    private const double NAME_GRUNDLINIE = 0.2;

    /// <summary>Die Grundlinie der Fläche unter dem Schwerpunkt, als Anteil der Schrift (Bildkoordinaten).</summary>
    private const double FLAECHE_GRUNDLINIE = 0.95;

    /// <summary>Oberkante des Namens über dem Schwerpunkt, als Anteil der Schrift.</summary>
    private const double KASTEN_OBEN = 1.0;

    /// <summary>Unterkante der Flächenzeile unter dem Schwerpunkt, als Anteil der Schrift.</summary>
    private const double KASTEN_UNTEN = 1.15;

    /// <summary>
    /// Der Ausschnitt eines Geschosses in Bildkoordinaten (y gespiegelt) samt der Schriftgröße, alles in Metern.
    /// </summary>
    /// <param name="X">Linker Rand.</param>
    /// <param name="Y">Oberer Rand (= −Nordrand).</param>
    /// <param name="Breite">Breite.</param>
    /// <param name="Hoehe">Höhe.</param>
    /// <param name="Schrift">Die Schriftgröße des Raumnamens.</param>
    internal readonly record struct Ausschnitt(double X, double Y, double Breite, double Hoehe, double Schrift)
    {
        /// <summary>Der Wert des Attributs <c>viewBox</c>.</summary>
        internal string ViewBox => Zahl(X) + " " + Zahl(Y) + " " + Zahl(Breite) + " " + Zahl(Hoehe);
    }

    /// <summary>Die Lage der Beschriftung eines Raums in Bildkoordinaten.</summary>
    /// <param name="X">Mitte der Zeilen.</param>
    /// <param name="NameY">Grundlinie des Namens.</param>
    /// <param name="FlaecheY">Grundlinie der Fläche.</param>
    internal readonly record struct Beschriftungslage(double X, double NameY, double FlaecheY);

    /// <summary>Der Ausschnitt des Geschosses — aus seiner Ausdehnung, mit Rand, Nord oben.</summary>
    internal static Ausschnitt AusschnittVon(GebaeudeAnsichtGeschoss g)
    {
        double breite = Math.Max(g.MaxX - g.MinX, MINDESTMASS_M);
        double hoehe = Math.Max(g.MaxY - g.MinY, MINDESTMASS_M);
        double mitteX = (g.MinX + g.MaxX) / 2.0;
        double mitteY = (g.MinY + g.MaxY) / 2.0;
        double gross = Math.Max(breite, hoehe);
        double rand = RANDANTEIL * gross;
        double w = breite + 2.0 * rand;
        double h = hoehe + 2.0 * rand;
        return new Ausschnitt(mitteX - w / 2.0, -(mitteY + h / 2.0), w, h, SCHRIFTANTEIL * gross);
    }

    /// <summary>Die Punkte eines Polygons als Wert des Attributs <c>points</c> — y gespiegelt.</summary>
    internal static string Punkte(IReadOnlyList<GebaeudeAnsichtPunkt> polygon)
    {
        var teile = new string[polygon.Count];
        for (int i = 0; i < polygon.Count; i++)
            teile[i] = Zahl(polygon[i].X) + "," + Zahl(-polygon[i].Y);
        return string.Join(" ", teile);
    }

    /// <summary>Eine Zahl des Bildes: invariant, <see cref="STELLEN"/> Nachkommastellen, nie „−0".</summary>
    internal static string Zahl(double wert)
    {
        double r = double.IsFinite(wert) ? Math.Round(wert, STELLEN, MidpointRounding.AwayFromZero) : 0.0;
        if (r == 0.0) r = 0.0;   // −0 wird 0: „-0.000" hinge sonst am Vorzeichen einer Rechnung
        return r.ToString("F" + STELLEN.ToString(INV), INV);
    }

    /// <summary>
    /// Wo die Beschriftung eines Raums steht — im Schwerpunkt seines größten Polygons, wenn der Kasten aus Name
    /// und Fläche dort ganz im Polygon liegt; sonst <c>null</c> (keine Beschriftung).
    /// </summary>
    /// <param name="raum">Der Raum.</param>
    /// <param name="schrift">Die Schriftgröße des Namens [m].</param>
    internal static Beschriftungslage? Beschriftung(GebaeudeAnsichtRaum raum, double schrift)
    {
        if (schrift <= 0.0 || !double.IsFinite(schrift)) return null;

        IReadOnlyList<GebaeudeAnsichtPunkt>? groesstes = null;
        double groesste = 0.0;
        foreach (IReadOnlyList<GebaeudeAnsichtPunkt> p in raum.Polygone)
        {
            if (p.Count < 3) continue;
            double a = Math.Abs(Flaeche(p));
            if (a > groesste) { groesste = a; groesstes = p; }
        }
        if (groesstes is null) return null;

        (double cx, double cy) = Schwerpunkt(groesstes);
        double halb = Math.Max(Textbreite(raum.Name, schrift), Textbreite(raum.Flaeche, schrift * FLAECHENSCHRIFT)) / 2.0;
        double oben = cy + KASTEN_OBEN * schrift, unten = cy - KASTEN_UNTEN * schrift;
        bool passt = Innen(groesstes, cx, cy)
                     && Innen(groesstes, cx - halb, oben) && Innen(groesstes, cx + halb, oben)
                     && Innen(groesstes, cx - halb, unten) && Innen(groesstes, cx + halb, unten);
        if (!passt) return null;
        return new Beschriftungslage(cx, -cy - NAME_GRUNDLINIE * schrift, -cy + FLAECHE_GRUNDLINIE * schrift);
    }

    /// <summary>Die geschätzte Breite eines Textes [m] bei der Schriftgröße <paramref name="schrift"/>.</summary>
    private static double Textbreite(string? text, double schrift) => (text ?? "").Length * ZEICHENBREITE * schrift;

    /// <summary>Der vorzeichenbehaftete Flächeninhalt (Gaußsche Trapezformel).</summary>
    private static double Flaeche(IReadOnlyList<GebaeudeAnsichtPunkt> p)
    {
        double s = 0.0;
        for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
            s += p[j].X * p[i].Y - p[i].X * p[j].Y;
        return s / 2.0;
    }

    /// <summary>Der Flächenschwerpunkt; ein entartetes Polygon nimmt das Mittel seiner Punkte.</summary>
    private static (double X, double Y) Schwerpunkt(IReadOnlyList<GebaeudeAnsichtPunkt> p)
    {
        double a = 0.0, cx = 0.0, cy = 0.0;
        for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
        {
            double k = p[j].X * p[i].Y - p[i].X * p[j].Y;
            a += k;
            cx += (p[j].X + p[i].X) * k;
            cy += (p[j].Y + p[i].Y) * k;
        }
        if (Math.Abs(a) < 1e-12)
        {
            double mx = 0.0, my = 0.0;
            foreach (GebaeudeAnsichtPunkt q in p) { mx += q.X; my += q.Y; }
            return (mx / p.Count, my / p.Count);
        }
        return (cx / (3.0 * a), cy / (3.0 * a));
    }

    /// <summary>Liegt der Punkt im Polygon (Strahlprobe)?</summary>
    private static bool Innen(IReadOnlyList<GebaeudeAnsichtPunkt> p, double x, double y)
    {
        bool innen = false;
        for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
        {
            if ((p[i].Y > y) != (p[j].Y > y)
                && x < (p[j].X - p[i].X) * (y - p[i].Y) / (p[j].Y - p[i].Y) + p[i].X)
                innen = !innen;
        }
        return innen;
    }
}

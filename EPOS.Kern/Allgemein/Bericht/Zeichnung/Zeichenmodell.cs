using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WindowsFormsApplication1.Zeichnung
{
    // =========================================================================
    // DAS ZEICHENMODELL (Konzept Diagramme, Etappe E1)
    //
    // Ein Bild ist eine Flaeche (Breite, Hoehe, Hintergrund) und eine LISTE VON
    // BEFEHLEN in Zeichenreihenfolge. Die Befehle tragen fertige Koordinaten in
    // Bildpunkten; gerechnet wird VOR dem Befehl, im Layout des Renderers. Damit
    // kann derselbe Inhalt zweimal ausgegeben werden: als PNG ueber den
    // SkiaMaler (Bericht) und spaeter als SVG (Bildschirm).
    //
    // DAS MODELL KENNT KEINE SKIA-TYPEN. Es hat eigene Werttypen fuer Farbe,
    // Punkt und Rahmen; die Bruecke nach SkiaSharp steht in SkiaBruecke.cs.
    // Nur so kann ein Ausgabeweg ohne Skia (SVG) dasselbe Modell lesen.
    //
    // Die Textvermessung bleibt eine Kern-Funktion und gehoert NICHT ins
    // Modell: Wer einen Text mittig setzen will, misst ihn vorher ueber die
    // Schriftkette und gibt die fertige Koordinate in den Befehl.
    //
    // FARBEN stehen als Farbton (Rolle + Abwandlung) im Befehl, nie als nackte
    // Zahl; aufgeloest wird beim Malen gegen Farbpalette.Aktuell. Siehe
    // Farbpalette.cs.
    // =========================================================================

    /// <summary>Ein Punkt in Bildpunkten — der Skia-freie Ersatz für <c>SKPoint</c>.</summary>
    public readonly record struct Punkt(float X, float Y);

    /// <summary>Ein achsparalleles Rechteck in Bildpunkten — der Ersatz für <c>SKRect</c>.</summary>
    public readonly record struct Rahmen(float X, float Y, float Breite, float Hoehe)
    {
        public float Rechts => X + Breite;
        public float Unten => Y + Hoehe;
    }

    /// <summary>Die Form des Strichendes (Skia: <c>SKStrokeCap</c>).</summary>
    public enum Strichkappe { Stumpf = 0, Rund = 1, Quadratisch = 2 }

    /// <summary>Die Form der Streckenverbindung (Skia: <c>SKStrokeJoin</c>).</summary>
    public enum Strichverbindung { Gehrung = 0, Rund = 1, Fase = 2 }

    /// <summary>Wie ein Text an seiner x-Koordinate hängt.</summary>
    public enum Ausrichtung { Links = 0, Mitte = 1, Rechts = 2 }

    /// <summary>
    /// Ein Strichmuster: <paramref name="Strich"/> Bildpunkte Linie,
    /// <paramref name="Luecke"/> Bildpunkte Lücke, ab <paramref name="Versatz"/>.
    /// Alle Muster des Bestands sind zweiteilig mit Versatz 0.
    /// </summary>
    public sealed record Strichmuster(float Strich, float Luecke, float Versatz = 0f);

    /// <summary>Ein Stift (Skia: <c>SKPaint</c> im Stroke-Stil).</summary>
    public sealed record Stift(
        Farbton Ton,
        float Breite,
        Strichmuster Muster = null,
        Strichkappe Kappe = Strichkappe.Stumpf,
        Strichverbindung Verbindung = Strichverbindung.Gehrung,
        bool Antialias = true);

    /// <summary>Eine Flächenfarbe (Skia: <c>SKPaint</c> im Fill-Stil).</summary>
    public sealed record Fuellung(Farbton Ton, bool Antialias = true);

    /// <summary>
    /// Eine Schrift in PUNKT — so, wie der Bestand sie führt. Die Umrechnung nach
    /// Bildpunkten und die Wahl der Schriftart macht die <see cref="Schriftkette"/>.
    /// </summary>
    public sealed record Schrift(float Punkt, bool Fett = false, bool Kursiv = false);

    // ----------------------------------------------------------------- Befehle

    /// <summary>Ein einzelner Zeichenbefehl.</summary>
    public abstract record Zeichenbefehl;

    /// <summary>Eine Strecke.</summary>
    public sealed record Linie(float X1, float Y1, float X2, float Y2, Stift Stift) : Zeichenbefehl;

    /// <summary>
    /// Ein Rechteck. Liegt beides an, wird ZUERST gefüllt und DANN umrandet —
    /// dieselbe Reihenfolge, in der der Bestand die zwei Aufrufe absetzt.
    /// </summary>
    public sealed record Rechteck(float X, float Y, float Breite, float Hoehe,
                                  Stift Rand = null, Fuellung Fuellung = null) : Zeichenbefehl;

    /// <summary>Ein Kreis um (<paramref name="X"/>, <paramref name="Y"/>).</summary>
    public sealed record Kreis(float X, float Y, float Radius,
                               Stift Rand = null, Fuellung Fuellung = null) : Zeichenbefehl;

    /// <summary>Eine Ellipse im umschließenden Rechteck.</summary>
    public sealed record Ellipse(float X, float Y, float Breite, float Hoehe,
                                 Stift Rand = null, Fuellung Fuellung = null) : Zeichenbefehl;

    /// <summary>
    /// Ein Kreissegment (Tortenstück) im umschließenden Rechteck, von
    /// <paramref name="Startwinkel"/> über <paramref name="Winkel"/> Grad.
    /// Ein Winkel ab 360° ist die volle <see cref="Ellipse"/> — Skia zieht sonst
    /// nichts (Befund zu Auftrag #222).
    /// </summary>
    public sealed record Kreissegment(float X, float Y, float Breite, float Hoehe,
                                      float Startwinkel, float Winkel,
                                      Stift Rand = null, Fuellung Fuellung = null) : Zeichenbefehl;

    /// <summary>
    /// Ein Streckenzug (<c>Geschlossen = false</c>) oder ein Vieleck
    /// (<c>Geschlossen = true</c>).
    /// </summary>
    public sealed record Pfad(Wertliste<Punkt> Punkte, bool Geschlossen,
                              Stift Rand = null, Fuellung Fuellung = null) : Zeichenbefehl;

    /// <summary>
    /// Ein Text an seiner linken OBEREN Ecke (dieselbe Bezugsecke wie im Bestand).
    /// Der Ausgabeweg setzt ihn auf die Grundlinie um.
    /// </summary>
    public sealed record Text(string Inhalt, float X, float Y, Schrift Schrift, Farbton Ton,
                              Ausrichtung Ausrichtung = Ausrichtung.Links) : Zeichenbefehl;

    /// <summary>
    /// Eine Gruppe von Befehlen, wahlweise auf ein Rechteck zugeschnitten
    /// (Skia: <c>Save</c>/<c>ClipRect</c>/<c>Restore</c>).
    /// </summary>
    public sealed record Gruppe(Rahmen? Zuschnitt, Wertliste<Zeichenbefehl> Befehle) : Zeichenbefehl;

    // ------------------------------------------------------------------ Ziele

    /// <summary>
    /// Wohin ein Helfer seine Befehle gibt: in ein ganzes <see cref="Zeichenmodell"/>
    /// oder in einen <see cref="Befehlssammler"/>, der eine Gruppe füllt.
    ///
    /// <para>Die Schnittstelle trennt den HELFER vom Behälter: Titel, Raster, Achsen
    /// und Legende haben EINEN Rumpf, gleich ob sie ins Bild oder in eine
    /// zugeschnittene Gruppe schreiben. Ein Ziel, das unmittelbar malt, gibt es
    /// nicht mehr — gemalt wird erst das fertige Modell
    /// (<c>SkiaMaler.Png</c>).</para>
    /// </summary>
    public interface IZeichenziel
    {
        void Fuege(Zeichenbefehl befehl);
    }

    /// <summary>Eine Befehlsliste ohne eigene Fläche — der Sammler einer Gruppe.</summary>
    public sealed class Befehlssammler : IZeichenziel
    {
        private readonly List<Zeichenbefehl> _befehle = new List<Zeichenbefehl>();

        public IReadOnlyList<Zeichenbefehl> Befehle => _befehle;

        public void Fuege(Zeichenbefehl befehl) { if (befehl != null) _befehle.Add(befehl); }

        public Wertliste<Zeichenbefehl> Liste() => new Wertliste<Zeichenbefehl>(_befehle);
    }

    /// <summary>
    /// Ein ganzes Bild: Fläche, Hintergrund und die Befehle in Zeichenreihenfolge.
    /// </summary>
    public sealed class Zeichenmodell : IZeichenziel
    {
        private readonly List<Zeichenbefehl> _befehle = new List<Zeichenbefehl>();

        public Zeichenmodell(int breite, int hoehe, Farbton hintergrund)
        {
            Breite = breite;
            Hoehe = hoehe;
            Hintergrund = hintergrund;
        }

        public int Breite { get; }
        public int Hoehe { get; }
        public Farbton Hintergrund { get; }

        public IReadOnlyList<Zeichenbefehl> Befehle => _befehle;

        public void Fuege(Zeichenbefehl befehl) { if (befehl != null) _befehle.Add(befehl); }

        /// <summary>
        /// Gleichheit zweier Modelle — Fläche, Hintergrund und jeder Befehl. Damit
        /// prüft ein Test, dass zweimal Erzeugen dasselbe Modell liefert.
        /// </summary>
        public bool Gleicht(Zeichenmodell andere)
        {
            if (andere == null) return false;
            if (Breite != andere.Breite || Hoehe != andere.Hoehe ||
                !Equals(Hintergrund, andere.Hintergrund)) return false;
            if (_befehle.Count != andere._befehle.Count) return false;
            for (int i = 0; i < _befehle.Count; i++)
                if (!Equals(_befehle[i], andere._befehle[i])) return false;
            return true;
        }
    }

    /// <summary>
    /// Eine unveränderliche Liste MIT WERTGLEICHHEIT. Ein Record mit einem
    /// gewöhnlichen Array oder einer <c>List</c> vergliche die Referenz — zwei
    /// gleich gefüllte Pfade wären dann verschieden, und der Determinismustest des
    /// Modells liefe ins Leere.
    /// </summary>
    public sealed class Wertliste<T> : IReadOnlyList<T>, IEquatable<Wertliste<T>>
    {
        private readonly T[] _werte;

        public Wertliste(IEnumerable<T> werte)
        {
            _werte = werte == null ? Array.Empty<T>() : new List<T>(werte).ToArray();
        }

        public T this[int i] => _werte[i];
        public int Count => _werte.Length;
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_werte).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _werte.GetEnumerator();

        public bool Equals(Wertliste<T> andere)
        {
            if (ReferenceEquals(this, andere)) return true;
            if (andere == null || andere._werte.Length != _werte.Length) return false;
            for (int i = 0; i < _werte.Length; i++)
                if (!EqualityComparer<T>.Default.Equals(_werte[i], andere._werte[i])) return false;
            return true;
        }

        public override bool Equals(object o) => Equals(o as Wertliste<T>);

        public override int GetHashCode()
        {
            int h = _werte.Length;
            foreach (T w in _werte) h = unchecked(h * 31 + (w == null ? 0 : w.GetHashCode()));
            return h;
        }
    }

    // --------------------------------------------------------------- Bequemes

    /// <summary>
    /// Die bequemen Schreibweisen für die Helfer des Renderers: <c>z.Linie(...)</c>
    /// statt <c>z.Fuege(new Zeichnung.Linie(...))</c>.
    /// </summary>
    public static class Zeichenhilfe
    {
        public static void Linie(this IZeichenziel z, float x1, float y1, float x2, float y2, Stift stift)
            => z.Fuege(new Zeichnung.Linie(x1, y1, x2, y2, stift));

        public static void Rechteck(this IZeichenziel z, float x, float y, float breite, float hoehe,
                                    Stift rand = null, Fuellung fuellung = null)
            => z.Fuege(new Zeichnung.Rechteck(x, y, breite, hoehe, rand, fuellung));

        public static void Kreis(this IZeichenziel z, float x, float y, float radius,
                                 Stift rand = null, Fuellung fuellung = null)
            => z.Fuege(new Zeichnung.Kreis(x, y, radius, rand, fuellung));

        public static void Ellipse(this IZeichenziel z, float x, float y, float breite, float hoehe,
                                   Stift rand = null, Fuellung fuellung = null)
            => z.Fuege(new Zeichnung.Ellipse(x, y, breite, hoehe, rand, fuellung));

        public static void Pfad(this IZeichenziel z, IEnumerable<Punkt> punkte, bool geschlossen,
                                Stift rand = null, Fuellung fuellung = null)
            => z.Fuege(new Zeichnung.Pfad(new Wertliste<Punkt>(punkte), geschlossen, rand, fuellung));

        public static void Text(this IZeichenziel z, string inhalt, float x, float y,
                                Schrift schrift, Farbton ton,
                                Ausrichtung ausrichtung = Ausrichtung.Links)
            => z.Fuege(new Zeichnung.Text(inhalt, x, y, schrift, ton, ausrichtung));

        /// <summary>Eine Gruppe, wahlweise zugeschnitten; <paramref name="inhalt"/> füllt sie.</summary>
        public static void Gruppe(this IZeichenziel z, Rahmen? zuschnitt, Action<IZeichenziel> inhalt)
        {
            var sammler = new Befehlssammler();
            inhalt(sammler);
            z.Fuege(new Zeichnung.Gruppe(zuschnitt, sammler.Liste()));
        }
    }
}

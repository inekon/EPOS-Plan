using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

namespace ChartProben
{
    // =========================================================================
    // DIE GEGENPROBEN DER GRUPPE (d) - Etappe DG-E3, die vier reinen
    // BERICHTSBILDER: Jahresverlauf Waerme, Jahresdauerlinie, Speicherverlauf
    // und Speichertemperaturen.
    //
    // ENTSCHEID DG-E3-7: Sie sind reine PIXELBILDER. Kein Datenfenster, keine
    // Datenreihe - im Bericht gibt es keine Bedienung, und der Gewinn des SVG
    // ist Schaerfe und Text. Die Gegenprobe der Gruppe (a) (SvgModellprobe)
    // passt deshalb NICHT auf sie: Sie verlangt eine Zeichenflaeche und je
    // Datenreihe einen path.epos-reihe. Hier wird das Gegenteil geprueft.
    //
    // OHNE ABLAGE UND OHNE MESSLATTE: Es entsteht kein PNG; die eingefrorene
    // Hashliste misst den PNG-Weg und bekommt hier keine Zeile. Die vier PNG dieser
    // Bilder stehen als Probe 3, 4, 6 und 7 weiter oben.
    // =========================================================================
    internal static partial class Program
    {
        /// <summary>
        /// Die vier Gegenproben der Gruppe (d) samt dem Schreiben ihrer Dateien bei
        /// <c>--svg-alle</c>. Gerufen aus <c>Main</c>, hinter den Gegenproben der
        /// Gruppe (a).
        /// </summary>
        private static void GruppeDProben(ZeitreihenSatz z)
        {
            var bilder = new List<KeyValuePair<string, Func<Zeichenmodell>>>
            {
                Modellprobe("jahresverlauf_waerme",
                    () => ChartRenderer.JahresverlaufWaermeModell(z)),
                Modellprobe("dauerlinie_waerme",
                    () => ChartRenderer.DauerlinieWaermeModell(z)),
                Modellprobe("speicherverlauf",
                    () => ChartRenderer.SpeicherverlaufModell(z)),
                Modellprobe("speichertemperaturen",
                    () => ChartRenderer.SpeichertemperaturenModell(z))
            };

            foreach (KeyValuePair<string, Func<Zeichenmodell>> b in bilder)
                SvgPixelprobe(b.Key, b.Value);

            if (_svgordner != null) SvgOrdnerSchreiben(bilder);
        }

        /// <summary>
        /// <b>Die SVG-Gegenprobe EINES reinen Pixelbildes</b> (Auftrag DG-E3d).
        /// Geprüft wird, was der PNG-Vergleich nicht sieht:
        ///
        /// <list type="number">
        ///   <item>Zweimal geschrieben UND zweimal erzeugt ist byte-gleich.</item>
        ///   <item>Das Modell führt <b>keine</b> Zeichenfläche und <b>keine</b>
        ///   Datenreihe (DG-E3-7) — und der Baum deshalb kein inneres
        ///   <c>&lt;svg class="epos-flaeche"&gt;</c> und keinen
        ///   <c>path.epos-reihe</c>.</item>
        ///   <item>Jeder Textbefehl des Modells steht als <c>&lt;text&gt;</c> im Baum:
        ///   Der Text bleibt Text und wird nicht zum Pfad.</item>
        ///   <item>Die Marken stehen im Baum — <c>titel</c>, <c>xachse</c>,
        ///   <c>yachse</c>, je Reihe eine <c>reihe:…</c> und je Legendeneintrag eine
        ///   <c>legende:…</c> —, und zu jeder gezeichneten Reihe gehört genau ein
        ///   Legendeneintrag.</item>
        ///   <item>Der Text beginnt wohlgeformt mit <c>&lt;svg</c> und trägt den
        ///   Namensraum — sonst wäre er als eigener Teil im Wortbericht unbrauchbar
        ///   (DG-E3-8).</item>
        /// </list>
        /// </summary>
        private static void SvgPixelprobe(string name, Func<Zeichenmodell> bau)
        {
            SvgProbe("svgd_" + name, e =>
            {
                Zeichenmodell m = bau();
                if (m == null) { e.Maengel.Add("das Bild liefert kein Modell"); return; }

                string a = SvgSchreiber.Text(m);
                e.Masse = m.Breite + "x" + m.Hoehe;
                e.Groesse = Encoding.UTF8.GetByteCount(a)
                                    .ToString("N0", CultureInfo.InvariantCulture);

                if (!string.Equals(a, SvgSchreiber.Text(m), StringComparison.Ordinal))
                    e.Maengel.Add("zweimal geschrieben ist nicht byte-gleich");
                if (!string.Equals(a, SvgSchreiber.Text(bau()), StringComparison.Ordinal))
                    e.Maengel.Add("zweimal erzeugt ist nicht byte-gleich");
                if (!a.StartsWith("<svg", StringComparison.Ordinal))
                    e.Maengel.Add("der Text beginnt nicht mit <svg");
                if (!a.Contains("xmlns=\"http://www.w3.org/2000/svg\"", StringComparison.Ordinal))
                    e.Maengel.Add("dem Wurzelknoten fehlt der Namensraum");

                // DG-E3-7: ein reines Pixelbild.
                if (m.Flaeche != null) e.Maengel.Add("das Modell fuehrt eine Zeichenflaeche");
                if (m.Reihen.Count > 0)
                    e.Maengel.Add("das Modell fuehrt " + m.Reihen.Count + " Datenreihen");

                List<SvgKnoten> alle = SvgSchreiber.Baum(m).Alle().ToList();
                e.Knoten = alle.Count.ToString(CultureInfo.InvariantCulture);

                if (alle.Any(k => k.Name == "svg" && Attributwert(k, "class") == "epos-flaeche"))
                    e.Maengel.Add("es gibt ein inneres svg (Datenkoordinaten statt Pixel)");
                if (alle.Any(k => Attributwert(k, "class") == "epos-reihe"))
                    e.Maengel.Add("es gibt einen Reihenpfad in Datenkoordinaten");

                // Jeder Text des Modells steht als <text> im Baum.
                int texte = Textbefehle(m.Befehle);
                int knoten = alle.Count(k => k.Name == "text");
                if (texte == 0) e.Maengel.Add("das Bild fuehrt keinen Text");
                else if (texte != knoten)
                    e.Maengel.Add("Textknoten: " + knoten + " statt " + texte);

                // Die Marken - im MODELL und im BAUM.
                var marken = new HashSet<string>(StringComparer.Ordinal);
                Marken(m.Befehle, marken);
                foreach (string pflicht in new[] { "titel", "xachse", "yachse" })
                    if (!marken.Contains(pflicht)) e.Maengel.Add("es fehlt die Marke " + pflicht);

                string[] reihen = Namen(marken, "reihe:");
                string[] legende = Namen(marken, "legende:");
                if (reihen.Length == 0) e.Maengel.Add("keine Reihenmarke");
                if (!reihen.SequenceEqual(legende, StringComparer.Ordinal))
                    e.Maengel.Add("Reihen und Legende nennen nicht dieselben Namen: [" +
                                  string.Join(", ", reihen) + "] gegen [" +
                                  string.Join(", ", legende) + "]");

                var imBaum = new HashSet<string>(
                    alle.Select(k => Attributwert(k, "data-marke")).Where(s => s != null),
                    StringComparer.Ordinal);
                foreach (string marke in marken)
                    if (!imBaum.Contains(marke))
                        e.Maengel.Add("die Marke „" + marke + "\" steht nicht im Baum");
            });
        }

        /// <summary>Alle Marken einer Befehlsliste, auch die in Gruppen.</summary>
        private static void Marken(IReadOnlyList<Zeichenbefehl> befehle, HashSet<string> ziel)
        {
            if (befehle == null) return;
            foreach (Zeichenbefehl b in befehle)
            {
                if (b.Marke != null) ziel.Add(b.Marke);
                if (b is Gruppe g) Marken(g.Befehle, ziel);
            }
        }

        /// <summary>Die Namen hinter einem Markenvorsatz, geordnet.</summary>
        private static string[] Namen(HashSet<string> marken, string vorsatz)
            => marken.Where(s => s.StartsWith(vorsatz, StringComparison.Ordinal))
                     .Select(s => s.Substring(vorsatz.Length))
                     .OrderBy(s => s, StringComparer.Ordinal)
                     .ToArray();
    }
}

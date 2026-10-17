using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Kein Stummel, kein Verlustabzweig und keine Beschriftung überdeckt ein Band</b> (Energieflussbild, alle drei
    /// Darstellungen). Die Kreuzungszahl des Renderers sieht nur die Mittellinien durchgehender Bänder; hier wird jede
    /// Fläche unter einer Marke <c>fluss:</c> — Band, Verlustabzweig, Verbinderstummel samt Pfeilspitze — und jeder
    /// Beschriftungsrahmen gegen die Flächen der übrigen Marken gehalten.
    ///
    /// <para>Die Gaben stellen das Anwenderbild „Booster-Kette mit Kombispeicher“ nach: Netzbezug groß und Photovoltaik
    /// klein am Strombus, der größte Teil des Stroms geht als Verbinder an die Wärmepumpe, ein kleiner Rest an den
    /// Strombedarf; ein kleiner Kessel mit Verlust; die Wärmepumpe mit Umweltwärme; der Wärmebus an Heizung und
    /// Warmwasser.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class EnergieflussUeberdeckungTests
    {
        /// <summary>So weit [px] dürfen sich zwei Flächen am Rand berühren (Saum, Rundung der Stützstellen).</summary>
        private const float TOLERANZ = 1.0f;

        internal static EnergieflussBildDaten BoosterKette()
        {
            var knoten = new List<EnergieflussBildKnoten>();
            void K(string id, EnergieflussSpur spur, string name, string kurz, EnergieflussKnotenart art)
                => knoten.Add(new EnergieflussBildKnoten { Kennung = id, Spur = spur, Name = name, Kurzname = kurz, Art = art });
            K("netz", EnergieflussSpur.Strom, "Netzbezug", "Netz", EnergieflussKnotenart.Quelle);
            K("pv", EnergieflussSpur.Strom, "Photovoltaik", "PV", EnergieflussKnotenart.Quelle);
            K("gas", EnergieflussSpur.Waerme, "Erdgas", "Gas", EnergieflussKnotenart.Quelle);
            K("umwelt", EnergieflussSpur.Waerme, "Umweltwärme", "Umwelt", EnergieflussKnotenart.Quelle);
            K("kessel", EnergieflussSpur.Waerme, "Heizkessel", "Kessel", EnergieflussKnotenart.Wandler);
            K("v_kessel", EnergieflussSpur.Waerme, "Verlust", "Verlust", EnergieflussKnotenart.Verlust);
            K("wp", EnergieflussSpur.Waerme, "Wärmepumpe", "WP", EnergieflussKnotenart.Wandler);
            K("strom", EnergieflussSpur.Strom, "Strom", "Strom", EnergieflussKnotenart.Bus);
            K("waerme", EnergieflussSpur.Waerme, "Wärme · Speicher", "Wärme", EnergieflussKnotenart.Bus);
            K("strombedarf", EnergieflussSpur.Strom, "Strombedarf", "Strom", EnergieflussKnotenart.Senke);
            K("heizung", EnergieflussSpur.Waerme, "Heizung", "Heizung", EnergieflussKnotenart.Senke);
            K("warmwasser", EnergieflussSpur.Waerme, "Warmwasser", "WW", EnergieflussKnotenart.Senke);
            var baender = new List<EnergieflussBildBand>();
            void B(string von, string nach, double mwh, Farbrolle rolle, EnergieflussBandart art = EnergieflussBandart.Normal)
                => baender.Add(new EnergieflussBildBand { Von = von, Nach = nach, Mwh = mwh, Rolle = rolle, Art = art });
            B("netz", "strom", 60.0, Farbrolle.STROM);
            B("pv", "strom", 2.0, Farbrolle.STROM);
            B("strom", "wp", 52.0, Farbrolle.STROM);
            B("strom", "strombedarf", 10.0, Farbrolle.STROM);
            B("gas", "kessel", 18.0, Farbrolle.BRENNSTOFF);
            B("kessel", "waerme", 16.0, Farbrolle.WAERME);
            B("kessel", "v_kessel", 2.0, Farbrolle.VERLUST, EnergieflussBandart.Verlust);
            B("umwelt", "wp", 70.0, Farbrolle.UMWELTWAERME);
            B("wp", "waerme", 120.0, Farbrolle.WAERME);
            B("waerme", "heizung", 100.0, Farbrolle.WAERME);
            B("waerme", "warmwasser", 36.0, Farbrolle.WAERME);
            return new EnergieflussBildDaten
            {
                Knoten = knoten, Baender = baender,
                Stufentitel = new[] { "Einsatz", "Anlagen", "Energieform", "Nutzen" },
            };
        }

        /// <summary>Eine x-monotone Fläche: obere Kante in der ersten Hälfte der Punkte, untere rückwärts in der zweiten.</summary>
        private sealed class Flaeche
        {
            public string Marke;
            public Punkt[] Oben, Unten;
            public float X0, X1;

            /// <summary>Die senkrechte Ausdehnung bei <paramref name="x"/>; <c>null</c> außerhalb.</summary>
            public (float O, float U)? Bei(float x)
            {
                if (x < X0 || x > X1) return null;
                float o = Lies(Oben, x), u = Lies(Unten, x);
                return (Math.Min(o, u), Math.Max(o, u));
            }

            private static float Lies(Punkt[] p, float x)
            {
                for (int i = 0; i < p.Length - 1; i++)
                {
                    float a = Math.Min(p[i].X, p[i + 1].X), b = Math.Max(p[i].X, p[i + 1].X);
                    if (x < a || x > b) continue;
                    if (b - a < 1e-6f) return Math.Min(p[i].Y, p[i + 1].Y);
                    float t = (x - p[i].X) / (p[i + 1].X - p[i].X);
                    return p[i].Y + (p[i + 1].Y - p[i].Y) * t;
                }
                return p[p.Length - 1].Y;
            }
        }

        private static Flaeche AusPfad(Pfad p)
        {
            int n = p.Punkte.Count;
            Punkt[] oben, unten;
            if (n == 3)
            {
                // Pfeilspitze: zwei Kanten von der Basis zur Spitze.
                oben = new[] { p.Punkte[0], p.Punkte[1] };
                unten = new[] { p.Punkte[2], p.Punkte[1] };
            }
            else
            {
                oben = p.Punkte.Take(n / 2).OrderBy(q => q.X).ToArray();
                unten = p.Punkte.Skip(n / 2).OrderBy(q => q.X).ToArray();
            }
            return new Flaeche
            {
                Marke = p.Marke, Oben = oben, Unten = unten,
                X0 = p.Punkte.Min(q => q.X), X1 = p.Punkte.Max(q => q.X),
            };
        }

        /// <summary>Die Flächen aller Marken <c>fluss:</c> (Bänder, Verluste, Stummel und Pfeilspitzen).</summary>
        private static List<Flaeche> Flaechen(Zeichenmodell m)
            => m.Befehle.OfType<Pfad>().Where(p => p.Marke != null && p.Marke.StartsWith("fluss:", StringComparison.Ordinal) && p.Punkte.Count >= 3)
                        .Select(AusPfad).ToList();

        /// <summary>Die größte Überdeckung zweier Flächen [px], abgetastet in Schritten von 0,5 px.</summary>
        private static float Ueberdeckung(Flaeche a, Flaeche b)
        {
            float x0 = Math.Max(a.X0, b.X0) + TOLERANZ, x1 = Math.Min(a.X1, b.X1) - TOLERANZ, max = 0f;
            for (float x = x0; x <= x1; x += 0.5f)
            {
                var ia = a.Bei(x); var ib = b.Bei(x);
                if (ia == null || ib == null) continue;
                max = Math.Max(max, Math.Min(ia.Value.U, ib.Value.U) - Math.Max(ia.Value.O, ib.Value.O));
            }
            return max;
        }

        /// <summary>Die größte Überdeckung eines Rahmens mit einer Fläche [px].</summary>
        private static float Ueberdeckung(Rahmen r, Flaeche f)
        {
            float x0 = Math.Max(r.X, f.X0) + TOLERANZ, x1 = Math.Min(r.Rechts, f.X1) - TOLERANZ, max = 0f;
            for (float x = x0; x <= x1; x += 0.5f)
            {
                var i = f.Bei(x);
                if (i == null) continue;
                max = Math.Max(max, Math.Min(r.Unten, i.Value.U) - Math.Max(r.Y, i.Value.O));
            }
            return max;
        }

        /// <summary>
        /// Alle Überdeckungen eines Bildes: Fläche gegen Fläche fremder Marken — außer zwei durchgehenden Bändern, die
        /// misst die Kreuzungszahl; Stummel und Verlustabzweige (Verlustknoten aus <paramref name="daten"/>) zählen — und jede Beschriftung (ohne Kastentext, Stufentitel und Legende) gegen jede Fläche
        /// einer fremden Marke.
        /// </summary>
        internal static List<string> Ueberdeckungen(Zeichenmodell m, EnergieflussBildDaten daten)
        {
            var befund = new List<string>();
            List<Flaeche> flaechen = Flaechen(m);
            var jeMarke = flaechen.GroupBy(f => f.Marke).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
            // Ein Verlustabzweig ist ein Pfad wie ein Band, endet aber an keinem Knoten: Er wird wie ein Stummel geprüft.
            var verlustknoten = new HashSet<string>((daten?.Knoten ?? Array.Empty<EnergieflussBildKnoten>())
                .Where(k => k != null && k.Art == EnergieflussKnotenart.Verlust).Select(k => k.Kennung), StringComparer.Ordinal);
            bool Band(Flaeche f) => jeMarke[f.Marke] == 1 && !verlustknoten.Any(k => f.Marke.EndsWith(":" + k, StringComparison.Ordinal));
            for (int i = 0; i < flaechen.Count; i++)
                for (int j = i + 1; j < flaechen.Count; j++)
                {
                    Flaeche a = flaechen[i], b = flaechen[j];
                    if (a.Marke == b.Marke || (Band(a) && Band(b))) continue;
                    float u = Ueberdeckung(a, b);
                    if (u > TOLERANZ) befund.Add("Fläche " + a.Marke + " ↔ " + b.Marke + " um " + u.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " px");
                }
            foreach (Text t in m.Befehle.OfType<Text>())
            {
                if (t.Marke == null || t.Marke.StartsWith("stufe:", StringComparison.Ordinal) || t.Marke.StartsWith("legende:", StringComparison.Ordinal)) continue;
                if (t.Ton?.Rolle == Farbrolle.HINTERGRUND) continue;                    // Kastentext steht im Kasten
                float w;
                using (var f = new Schriftmass(t.Schrift)) w = f.MeasureText(t.Inhalt);
                float h;
                using (var f = new Schriftmass(t.Schrift)) h = f.Hoehe;
                var r = new Rahmen(t.X, t.Y, w, h);
                foreach (Flaeche f in flaechen)
                {
                    float u = f.Marke == t.Marke ? 0f : Ueberdeckung(r, f);
                    if (u > TOLERANZ) befund.Add("Text „" + t.Inhalt + "“ (" + t.Marke + ") ↔ " + f.Marke + " um " + u.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " px");
                }
            }
            return befund.Distinct().ToList();
        }

        public static IEnumerable<object[]> Fassungen()
        {
            foreach (EnergieflussDarstellung d in new[] { EnergieflussDarstellung.Stufen, EnergieflussDarstellung.Kette, EnergieflussDarstellung.Zuordnung })
                foreach (string f in new[] { "breit", "schmal", "bericht" })
                    yield return new object[] { d, f };
        }

        private static (Bildmass Mass, bool Werte) Fassung(string f)
            => f == "schmal" ? (ChartRenderer.ENERGIEFLUSS_SCHMAL, false)
             : f == "bericht" ? (ChartRenderer.ENERGIEFLUSS_BERICHT, true)
             : (ChartRenderer.ENERGIEFLUSS_BREIT, false);

        [Theory]
        [MemberData(nameof(Fassungen))]
        public void Booster_Kette_Stummel_Verluste_und_Beschriftungen_ueberdecken_kein_Band(EnergieflussDarstellung darstellung, string fassung)
        {
            var (mass, werte) = Fassung(fassung);
            EnergieflussBildDaten daten = BoosterKette();
            Zeichenmodell m = ChartRenderer.EnergieflussModell(daten, mass, werte, darstellung);
            Assert.Empty(ChartRenderer.EnergieflussKreuzungspaare(m));
            Assert.Empty(Ueberdeckungen(m, daten));
        }
    }
}

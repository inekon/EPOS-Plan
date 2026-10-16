using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

namespace ChartProben
{
    /// <summary>
    /// <b>DAS ENERGIEFLUSSBILD (SANKEY)</b> (Konzept Energieflussbild 3 und 6, Schritte SK-2 und SK-2c, Variante A):
    /// <c>ChartRenderer.EnergieflussModell</c> — Strom, Wärme und Kälte eines Jahres als gestraffte Kette mit Spuren nach
    /// Energieform, Bänder als S-Kurven-Polygone über Wegpunkte, Wandler als Kästen, Verluste als Abzweige, Legende unter
    /// dem Bild.
    ///
    /// <para><b>Die Gaben sind synthetisch</b> und fest verdrahtet — keine Datenbank, kein Zufall: ein Objekt mit Netz,
    /// Photovoltaik, BHKW und Kessel am Erdgas, einer Wärmepumpe, einem Heizstab, einer Kältemaschine, Wärme- und
    /// Strombus mit Speicherverlust (die Speicher sind in ihren Bus gefaltet) und einem Band „nicht zugeordnet“ am
    /// Strombus. Jeder Bus schließt.</para>
    ///
    /// <para><b>Die Gegenproben</b> halten fest, was Maß-, Farb- und Determinismusprüfung nicht sehen: dass der Speicher
    /// am Bus, das Band „nicht zugeordnet“, die schmale Fassung und die Knotenwerte des Berichts im Bild ankommen. Die
    /// SVG-Probe zählt Bänder und Knoten mit ihren Marken und Werten, prüft die Legende und hält die Bildschirmfassung
    /// frei von Zahlen (SK-Q12).</para>
    /// </summary>
    internal static partial class Program
    {
        /// <summary>
        /// Die Gaben der Probe. <paramref name="speicher"/> schaltet den Pufferspeicher am Wärmebus (Zusatz „· Speicher“
        /// und Speicherverlust), <paramref name="rest"/> das Band „nicht zugeordnet“ am Strombus, <paramref name="split"/>
        /// ein Split-Gerät neben der Kältemaschine.
        /// </summary>
        internal static EnergieflussBildDaten Energieflussgaben(bool speicher = true, bool rest = true, bool split = false)
        {
            var knoten = new List<EnergieflussBildKnoten>();
            void K(string id, EnergieflussSpur spur, string name, string kurz, EnergieflussKnotenart art)
                => knoten.Add(new EnergieflussBildKnoten { Kennung = id, Spur = spur, Name = name, Kurzname = kurz, Art = art });
            K("netz", EnergieflussSpur.Strom, "Netzbezug", "Netz", EnergieflussKnotenart.Quelle);
            K("pv", EnergieflussSpur.Strom, "Photovoltaik", "PV", EnergieflussKnotenart.Quelle);
            K("gas", EnergieflussSpur.Gemischt, "Erdgas", "Erdgas", EnergieflussKnotenart.Quelle);
            K("umwelt", EnergieflussSpur.Waerme, "Umweltwärme", "Umwelt", EnergieflussKnotenart.Quelle);
            K("umweltk", EnergieflussSpur.Kaelte, "Umwelt (Kälte)", "Umwelt (K)", EnergieflussKnotenart.Quelle);
            K("bhkw", EnergieflussSpur.Gemischt, "BHKW", "BHKW", EnergieflussKnotenart.Wandler);
            K("kessel", EnergieflussSpur.Waerme, "Heizkessel", "Kessel", EnergieflussKnotenart.Wandler);
            K("strom", EnergieflussSpur.Strom, "Strom · Speicher", "Strom", EnergieflussKnotenart.Bus);
            K("wp", EnergieflussSpur.Waerme, "Wärmepumpe (Heizen)", "WP Heizen", EnergieflussKnotenart.Wandler);
            K("heizstab", EnergieflussSpur.Waerme, "Heizstab", "Heizstab", EnergieflussKnotenart.Wandler);
            K("km", EnergieflussSpur.Kaelte, "Kältemaschine", "KM", EnergieflussKnotenart.Wandler);
            K("split", EnergieflussSpur.Kaelte, "Split-Geräte", "Split", EnergieflussKnotenart.Wandler);
            K("waerme", EnergieflussSpur.Waerme, speicher ? "Wärme · Speicher" : "Wärme", "Wärme", EnergieflussKnotenart.Bus);
            K("kaelte", EnergieflussSpur.Kaelte, "Kälte · Speicher", "Kälte", EnergieflussKnotenart.Bus);
            K("heizung", EnergieflussSpur.Waerme, "Heizung", "Heizung", EnergieflussKnotenart.Senke);
            K("warmwasser", EnergieflussSpur.Waerme, "Warmwasser", "WW", EnergieflussKnotenart.Senke);
            K("kuehlung", EnergieflussSpur.Kaelte, "Kühlung", "Kühlung", EnergieflussKnotenart.Senke);
            K("strombedarf", EnergieflussSpur.Strom, "Strombedarf", "Strom", EnergieflussKnotenart.Senke);
            K("einsp_pv", EnergieflussSpur.Strom, "Einspeisung PV", "Einsp. PV", EnergieflussKnotenart.Senke);
            K("nichtzug", EnergieflussSpur.Strom, "nicht zugeordnet", "n. zug.", EnergieflussKnotenart.Senke);
            K("v_bhkw", EnergieflussSpur.Gemischt, "Verlust", "Verlust", EnergieflussKnotenart.Verlust);
            K("v_kessel", EnergieflussSpur.Waerme, "Verlust", "Verlust", EnergieflussKnotenart.Verlust);
            K("v_waerme", EnergieflussSpur.Waerme, "Speicherverlust", "Verlust", EnergieflussKnotenart.Verlust);
            K("v_kaelte", EnergieflussSpur.Kaelte, "Speicherverlust", "Verlust", EnergieflussKnotenart.Verlust);
            K("v_strom", EnergieflussSpur.Strom, "Speicherverlust", "Verlust", EnergieflussKnotenart.Verlust);

            var baender = new List<EnergieflussBildBand>();
            void B(string von, string nach, double mwh, Farbrolle rolle,
                   EnergieflussBandart art = EnergieflussBandart.Normal)
                => baender.Add(new EnergieflussBildBand { Von = von, Nach = nach, Mwh = mwh, Rolle = rolle, Art = art });
            B("netz", "strom", 30.0, Farbrolle.STROM);
            B("pv", "strom", 12.0, Farbrolle.STROM);
            B("gas", "bhkw", 40.0, Farbrolle.BRENNSTOFF);
            B("gas", "kessel", 20.0, Farbrolle.BRENNSTOFF);
            B("bhkw", "strom", 13.0, Farbrolle.STROM);
            B("bhkw", "waerme", 22.0, Farbrolle.WAERME);
            B("bhkw", "v_bhkw", 5.0, Farbrolle.VERLUST, EnergieflussBandart.Verlust);
            B("kessel", "waerme", 18.0, Farbrolle.WAERME);
            B("kessel", "v_kessel", 2.0, Farbrolle.VERLUST, EnergieflussBandart.Verlust);
            B("umwelt", "wp", 24.0, Farbrolle.UMWELTWAERME);
            B("strom", "wp", 8.0, Farbrolle.STROM);
            B("wp", "waerme", 32.0, Farbrolle.WAERME);
            B("strom", "heizstab", 1.5, Farbrolle.STROM);
            B("heizstab", "waerme", 1.5, Farbrolle.WAERME);
            B("umweltk", "km", 9.0, Farbrolle.UMWELTWAERME);
            B("strom", "km", 3.0, Farbrolle.STROM);
            B("km", "kaelte", 12.0, Farbrolle.KAELTE);
            B("kaelte", "kuehlung", 11.8 + (split ? 6.0 : 0.0), Farbrolle.KAELTE);
            B("kaelte", "v_kaelte", 0.2, Farbrolle.VERLUST, EnergieflussBandart.Verlust);
            if (split)
            {
                B("umweltk", "split", 4.0, Farbrolle.UMWELTWAERME);
                B("strom", "split", 2.0, Farbrolle.STROM);
                B("split", "kaelte", 6.0, Farbrolle.KAELTE);
            }
            B("waerme", "heizung", speicher ? 62.5 : 63.5, Farbrolle.WAERME);
            if (speicher) B("waerme", "v_waerme", 1.0, Farbrolle.VERLUST, EnergieflussBandart.Verlust);
            B("waerme", "warmwasser", 10.0, Farbrolle.WAERME);
            B("strom", "strombedarf", (rest ? 37.8 : 38.3) - (split ? 2.0 : 0.0), Farbrolle.STROM);
            B("strom", "v_strom", 0.2, Farbrolle.VERLUST, EnergieflussBandart.Verlust);
            B("strom", "einsp_pv", 4.0, Farbrolle.STROM);
            if (rest) B("strom", "nichtzug", 0.5, Farbrolle.REST, EnergieflussBandart.NichtZugeordnet);

            var legende = new List<EnergieflussBildLegende>
            {
                new EnergieflussBildLegende { Rolle = Farbrolle.STROM, Text = "Strom" },
                new EnergieflussBildLegende { Rolle = Farbrolle.WAERME, Text = "Wärme" },
                new EnergieflussBildLegende { Rolle = Farbrolle.KAELTE, Text = "Kälte" },
                new EnergieflussBildLegende { Rolle = Farbrolle.BRENNSTOFF, Text = "Brennstoff" },
                new EnergieflussBildLegende { Rolle = Farbrolle.UMWELTWAERME, Text = "Umweltwärme / Umwelt (Kälte)" },
                new EnergieflussBildLegende { Rolle = Farbrolle.VERLUST, Text = "Verlust" },
            };
            if (rest) legende.Add(new EnergieflussBildLegende { Rolle = Farbrolle.REST, Text = "nicht zugeordnet / ungedeckt", Gestrichelt = true });
            return new EnergieflussBildDaten { Knoten = knoten, Baender = baender, Legende = legende };
        }

        private static byte[] Energieflussbild(EnergieflussBildDaten d, Bildmass mass, bool knotenwerte = false)
            => ChartRenderer.EnergieflussBild(d, mass, knotenwerte);

        private static void EnergieflussProben(string ziel)
        {
            Bildmass breit = ChartRenderer.ENERGIEFLUSS_BREIT, schmal = ChartRenderer.ENERGIEFLUSS_SCHMAL,
                     bericht = ChartRenderer.ENERGIEFLUSS_BERICHT;
            // Die Kästen und Busse in KNOTEN, die Namen in TEXT — die Bänder sind halbdurchsichtig (Deckung 0,9) und
            // treffen ihre Rollenfarbe deshalb nie wörtlich, die Verluste sind Abzweige ohne Knotenbalken.
            var farben = new[] { Rollenfarbe(Farbrolle.KNOTEN), Rollenfarbe(Farbrolle.TEXT) };

            // Maßproben: breit, schmal, Bericht mit Knotenwerten, Kälte mit Split-Gerät.
            Pruefe(ziel, "energiefluss", breit.Breite, breit.Hoehe, farben, () => Energieflussbild(Energieflussgaben(), breit));
            Pruefe(ziel, "energiefluss_schmal", schmal.Breite, schmal.Hoehe, farben,
                   () => Energieflussbild(Energieflussgaben(), schmal));
            Pruefe(ziel, "energiefluss_bericht", bericht.Breite, bericht.Hoehe, farben,
                   () => Energieflussbild(Energieflussgaben(), bericht, true));
            Pruefe(ziel, "energiefluss_kaelte_split", breit.Breite, breit.Hoehe, farben,
                   () => Energieflussbild(Energieflussgaben(split: true), breit));

            // Gegenproben: Speicher am Bus, Band „nicht zugeordnet“, schmale Fassung und Knotenwerte kommen im Bild an.
            Unterschiedlich("energiefluss_speicher_wirkt",
                            () => Energieflussbild(Energieflussgaben(speicher: true), breit),
                            () => Energieflussbild(Energieflussgaben(speicher: false), breit));
            Unterschiedlich("energiefluss_rest_wirkt",
                            () => Energieflussbild(Energieflussgaben(rest: false), breit),
                            () => Energieflussbild(Energieflussgaben(rest: true), breit));
            Unterschiedlich("energiefluss_schmal_wirkt",
                            () => Energieflussbild(Energieflussgaben(), breit),
                            () => Energieflussbild(Energieflussgaben(), schmal));
            Unterschiedlich("energiefluss_knotenwerte_wirkt",
                            () => Energieflussbild(Energieflussgaben(), bericht, false),
                            () => Energieflussbild(Energieflussgaben(), bericht, true));

            // SVG-Probe: je Band ein Pfad mit Marke und Wert, je Knoten (ohne Verluste) ein Rechteck mit Marke und Wert, die
            // Legende je Energieform, keine Zeichenfläche, byte-gleich; die Bildschirmfassung ohne Zahl im Bild, die
            // Berichtsfassung mit Knotenwerten; die Kette der Gaben kreuzt sich nur dort, wo die Topologie es verlangt.
            SvgProbe("svg_energiefluss", e =>
            {
                EnergieflussBildDaten d = Energieflussgaben();
                Zeichenmodell m = ChartRenderer.EnergieflussModell(d, breit);
                e.Masse = m.Breite + "x" + m.Hoehe;
                if (!string.Equals(SvgSchreiber.Text(m), SvgSchreiber.Text(ChartRenderer.EnergieflussModell(d, breit)),
                                   StringComparison.Ordinal))
                    e.Maengel.Add("zweimal erzeugt ist nicht byte-gleich");
                if (m.Flaeche != null) e.Maengel.Add("das Modell fuehrt eine Zeichenflaeche");

                List<SvgKnoten> alle = SvgSchreiber.Baum(m).Alle().ToList();
                var baender = alle.Where(k => (Attributwert(k, "data-marke") ?? "").StartsWith("fluss:", StringComparison.Ordinal) && k.Name == "path").ToList();
                var knoten = alle.Where(k => (Attributwert(k, "data-marke") ?? "").StartsWith("knoten:", StringComparison.Ordinal)
                                             && k.Name == "rect").ToList();
                var legende = alle.Where(k => (Attributwert(k, "data-marke") ?? "").StartsWith("legende:", StringComparison.Ordinal)
                                              && k.Name == "rect").ToList();
                e.Knoten = baender.Count + "/" + knoten.Count + "/" + legende.Count;
                if (baender.Count != d.Baender.Count)
                    e.Maengel.Add("Baender " + baender.Count + " statt " + d.Baender.Count);
                int erwartet = d.Knoten.Count(k => k.Art != EnergieflussKnotenart.Verlust) - 1;   // das Split-Gerät hat ohne Split kein Band
                if (knoten.Count != erwartet)
                    e.Maengel.Add("Knoten " + knoten.Count + " statt " + erwartet);
                if (legende.Count != d.Legende.Count)
                    e.Maengel.Add("Legende " + legende.Count + " statt " + d.Legende.Count);
                if (baender.Concat(knoten).Any(k => string.IsNullOrEmpty(Attributwert(k, "data-wert"))))
                    e.Maengel.Add("ein Band oder Knoten ohne data-wert");
                if (!baender.Any(k => Attributwert(k, "data-wert") == "Strom · Speicher → nicht zugeordnet: 0,5 MWh/a"))
                    e.Maengel.Add("Zeigertext des Bandes „nicht zugeordnet“ fehlt");
                if (m.Befehle.OfType<WindowsFormsApplication1.Zeichnung.Text>().Any(t => t.Inhalt.Any(char.IsDigit)))
                    e.Maengel.Add("die Bildschirmfassung traegt eine Zahl im Bild (SK-Q12)");
                // Die drei topologisch unvermeidbaren Kreuzungen: der Strom der Kältemaschine muss von der Stromspur durch
                // die Wärmespur (Umweltwärme → WP, Erdgas → Kessel, BHKW → Wärme) in die Kältespur; sonst kreuzt nichts.
                var kreuzungen = ChartRenderer.EnergieflussKreuzungspaare(m);
                if (kreuzungen.Count > 3 || kreuzungen.Any(k => k.A != "fluss:strom:km" && k.B != "fluss:strom:km"))
                    e.Maengel.Add("die Kette der Gaben kreuzt sich " + kreuzungen.Count + "-mal: " + string.Join("; ", kreuzungen.Select(k => k.A + " x " + k.B)));
                Zeichenmodell b = ChartRenderer.EnergieflussModell(d, bericht, true);
                if (!b.Befehle.OfType<WindowsFormsApplication1.Zeichnung.Text>().Any(t => t.Inhalt == "Wärme · Speicher · 73,5"))
                    e.Maengel.Add("die Berichtsfassung nennt die Jahressumme des Waermebusses nicht");
            });
        }
    }
}

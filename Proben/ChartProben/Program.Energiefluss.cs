using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

namespace ChartProben
{
    /// <summary>
    /// <b>DAS ENERGIEFLUSSBILD (SANKEY)</b> (Konzept Energieflussbild 3 und 6, Schritt SK-2):
    /// <c>ChartRenderer.EnergieflussModell</c> — Strom, Wärme und Kälte eines Jahres in sieben Spalten, Bänder als
    /// S-Kurven-Polygone, Verluste in Knoten in der Spaltenlücke.
    ///
    /// <para><b>Die Gaben sind synthetisch</b> und fest verdrahtet — keine Datenbank, kein Zufall: ein Objekt mit Netz,
    /// Photovoltaik, BHKW und Kessel am Erdgas, einer Wärmepumpe, einem Heizstab, einer Kältemaschine mit Kältespeicher,
    /// Puffer- und Stromspeicher mit Verlust und einem Band „nicht zugeordnet“ am Strombus. Jeder Bus schließt.</para>
    ///
    /// <para><b>Die Gegenproben</b> halten fest, was Maß-, Farb- und Determinismusprüfung nicht sehen: dass der Speicher,
    /// das Band „nicht zugeordnet“, die schmale Fassung und die Knotenwerte des Berichts im Bild ankommen. Die SVG-Probe
    /// zählt Bänder und Knoten mit ihren Marken und Werten und hält die Bildschirmfassung frei von Zahlen (SK-Q12).</para>
    /// </summary>
    internal static partial class Program
    {
        /// <summary>Die Spaltentitel der Probe.</summary>
        private static readonly string[] EF_TITEL =
            { "Quellen", "Wandler", "Strom", "Wandler", "Wärme · Kälte", "Speicher", "Bedarf" };

        /// <summary>
        /// Die Gaben der Probe. <paramref name="speicher"/> schaltet den Pufferspeicher (ohne ihn geht die Wärme direkt
        /// in die Heizung), <paramref name="rest"/> das Band „nicht zugeordnet“ am Strombus, <paramref name="split"/>
        /// ein Split-Gerät neben der Kältemaschine.
        /// </summary>
        internal static EnergieflussBildDaten Energieflussgaben(bool speicher = true, bool rest = true, bool split = false)
        {
            var knoten = new List<EnergieflussBildKnoten>();
            void K(string id, int sp, string name, string kurz, EnergieflussKnotenart art)
                => knoten.Add(new EnergieflussBildKnoten { Kennung = id, Spalte = sp, Name = name, Kurzname = kurz, Art = art });
            K("netz", 0, "Netzbezug", "Netz", EnergieflussKnotenart.Quelle);
            K("pv", 0, "Photovoltaik", "PV", EnergieflussKnotenart.Quelle);
            K("gas", 0, "Erdgas", "Erdgas", EnergieflussKnotenart.Quelle);
            K("umwelt", 0, "Umweltwärme", "Umwelt", EnergieflussKnotenart.Quelle);
            K("umweltk", 0, "Umwelt (Kälte)", "Umwelt (K)", EnergieflussKnotenart.Quelle);
            K("bhkw", 1, "BHKW", "BHKW", EnergieflussKnotenart.Wandler);
            K("kessel", 1, "Heizkessel", "Kessel", EnergieflussKnotenart.Wandler);
            K("strom", 2, "Strom", "Strom", EnergieflussKnotenart.Bus);
            K("wp", 3, "Wärmepumpe (Heizen)", "WP Heizen", EnergieflussKnotenart.Wandler);
            K("heizstab", 3, "Heizstab", "Heizstab", EnergieflussKnotenart.Wandler);
            K("km", 3, "Kältemaschine", "KM", EnergieflussKnotenart.Wandler);
            K("split", 3, "Split-Geräte", "Split", EnergieflussKnotenart.Wandler);
            K("waerme", 4, "Wärme", "Wärme", EnergieflussKnotenart.Bus);
            K("kaelte", 4, "Kälte", "Kälte", EnergieflussKnotenart.Bus);
            K("puffer", 5, "Pufferspeicher", "Puffer", EnergieflussKnotenart.Speicher);
            K("kspeicher", 5, "Kältespeicher", "K-Speicher", EnergieflussKnotenart.Speicher);
            K("sspeicher", 5, "Stromspeicher", "Batterie", EnergieflussKnotenart.Speicher);
            K("heizung", 6, "Heizung", "Heizung", EnergieflussKnotenart.Senke);
            K("warmwasser", 6, "Warmwasser", "WW", EnergieflussKnotenart.Senke);
            K("kuehlung", 6, "Kühlung", "Kühlung", EnergieflussKnotenart.Senke);
            K("strombedarf", 6, "Strombedarf", "Strom", EnergieflussKnotenart.Senke);
            K("einsp_pv", 6, "Einspeisung PV", "Einsp. PV", EnergieflussKnotenart.Senke);
            K("nichtzug", 6, "nicht zugeordnet", "n. zug.", EnergieflussKnotenart.Senke);
            K("v_bhkw", 1, "Verlust", "Verlust", EnergieflussKnotenart.Verlust);
            K("v_kessel", 1, "Verlust", "Verlust", EnergieflussKnotenart.Verlust);
            K("v_puffer", 5, "Verlust", "Verlust", EnergieflussKnotenart.Verlust);
            K("v_kspeicher", 5, "Verlust", "Verlust", EnergieflussKnotenart.Verlust);
            K("v_sspeicher", 5, "Verlust", "Verlust", EnergieflussKnotenart.Verlust);

            var baender = new List<EnergieflussBildBand>();
            void B(string von, string nach, double mwh, Farbrolle rolle,
                   EnergieflussBandart art = EnergieflussBandart.Normal)
                => baender.Add(new EnergieflussBildBand { Von = von, Nach = nach, Mwh = mwh, Rolle = rolle, Art = art });
            B("netz", "strom", 30.0, Farbrolle.STROM_NETZ);
            B("pv", "strom", 12.0, Farbrolle.STROM_PV);
            B("gas", "bhkw", 40.0, Farbrolle.BRENNSTOFF);
            B("gas", "kessel", 20.0, Farbrolle.BRENNSTOFF);
            B("bhkw", "strom", 13.0, Farbrolle.STROM_BHKW);
            B("bhkw", "waerme", 22.0, Farbrolle.WAERME_BHKW);
            B("bhkw", "v_bhkw", 5.0, Farbrolle.VERLUST, EnergieflussBandart.Verlust);
            B("kessel", "waerme", 18.0, Farbrolle.WAERME_KESSEL);
            B("kessel", "v_kessel", 2.0, Farbrolle.VERLUST, EnergieflussBandart.Verlust);
            B("umwelt", "wp", 24.0, Farbrolle.UMWELTWAERME);
            B("strom", "wp", 8.0, Farbrolle.WAERME_WP);
            B("wp", "waerme", 32.0, Farbrolle.WAERME_WP);
            B("strom", "heizstab", 1.5, Farbrolle.HEIZSTAB);
            B("heizstab", "waerme", 1.5, Farbrolle.HEIZSTAB);
            B("umweltk", "km", 9.0, Farbrolle.UMWELTWAERME);
            B("strom", "km", 3.0, Farbrolle.KAELTE);
            B("km", "kaelte", 12.0, Farbrolle.KAELTE);
            B("kaelte", "kspeicher", 4.0, Farbrolle.KAELTE);
            B("kspeicher", "kuehlung", 3.8, Farbrolle.KAELTE);
            B("kspeicher", "v_kspeicher", 0.2, Farbrolle.VERLUST, EnergieflussBandart.Verlust);
            B("kaelte", "kuehlung", 8.0, Farbrolle.KAELTE);
            if (split)
            {
                B("umweltk", "split", 4.0, Farbrolle.UMWELTWAERME);
                B("strom", "split", 2.0, Farbrolle.KAELTE_DEZENTRAL);
                B("split", "kuehlung", 6.0, Farbrolle.KAELTE_DEZENTRAL);
            }
            if (speicher)
            {
                B("waerme", "puffer", 30.0, Farbrolle.HEIZWAERME);
                B("puffer", "heizung", 29.0, Farbrolle.HEIZWAERME);
                B("puffer", "v_puffer", 1.0, Farbrolle.VERLUST, EnergieflussBandart.Verlust);
                B("waerme", "heizung", 33.5, Farbrolle.HEIZWAERME);
            }
            else B("waerme", "heizung", 63.5, Farbrolle.HEIZWAERME);
            B("waerme", "warmwasser", 10.0, Farbrolle.WARMWASSER);
            B("strom", "strombedarf", rest ? 36.0 - (split ? 2.0 : 0.0) : 36.5 - (split ? 2.0 : 0.0), Farbrolle.VERBRAUCH_GESAMT);
            B("strom", "sspeicher", 2.0, Farbrolle.SPEICHERLADUNG);
            B("sspeicher", "strombedarf", 1.8, Farbrolle.STROM_SPEICHER);
            B("sspeicher", "v_sspeicher", 0.2, Farbrolle.VERLUST, EnergieflussBandart.Verlust);
            B("strom", "einsp_pv", 4.0, Farbrolle.UEBERSCHUSS);
            if (rest) B("strom", "nichtzug", 0.5, Farbrolle.REST, EnergieflussBandart.NichtZugeordnet);

            return new EnergieflussBildDaten { Knoten = knoten, Baender = baender, Spaltentitel = EF_TITEL };
        }

        private static byte[] Energieflussbild(EnergieflussBildDaten d, Bildmass mass, bool knotenwerte = false)
            => ChartRenderer.EnergieflussBild(d, mass, knotenwerte);

        private static void EnergieflussProben(string ziel)
        {
            Bildmass breit = ChartRenderer.ENERGIEFLUSS_BREIT, schmal = ChartRenderer.ENERGIEFLUSS_SCHMAL,
                     bericht = ChartRenderer.ENERGIEFLUSS_BERICHT;
            // Die Knoten in KNOTEN, die Verlustknoten in VERLUST, die Namen in TEXT — die Bänder sind halbdurchsichtig
            // und treffen ihre Rollenfarbe deshalb nie wörtlich.
            var farben = new[] { Rollenfarbe(Farbrolle.KNOTEN), Rollenfarbe(Farbrolle.VERLUST), Rollenfarbe(Farbrolle.TEXT) };

            // Maßproben: breit, schmal, Bericht mit Knotenwerten, Kälte mit Split-Gerät.
            Pruefe(ziel, "energiefluss", breit.Breite, breit.Hoehe, farben, () => Energieflussbild(Energieflussgaben(), breit));
            Pruefe(ziel, "energiefluss_schmal", schmal.Breite, schmal.Hoehe, farben,
                   () => Energieflussbild(Energieflussgaben(), schmal));
            Pruefe(ziel, "energiefluss_bericht", bericht.Breite, bericht.Hoehe, farben,
                   () => Energieflussbild(Energieflussgaben(), bericht, true));
            Pruefe(ziel, "energiefluss_kaelte_split", breit.Breite, breit.Hoehe, farben,
                   () => Energieflussbild(Energieflussgaben(split: true), breit));

            // Gegenproben: Speicher, Band „nicht zugeordnet“, schmale Fassung und Knotenwerte kommen im Bild an.
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

            // SVG-Probe: je Band ein Pfad mit Marke und Wert, je Knoten ein Rechteck mit Marke und Wert, keine
            // Zeichenfläche, byte-gleich; die Bildschirmfassung ohne Zahl im Bild, die Berichtsfassung mit Knotenwerten.
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
                var baender = alle.Where(k => (Attributwert(k, "data-marke") ?? "").StartsWith("fluss:", StringComparison.Ordinal)).ToList();
                var knoten = alle.Where(k => (Attributwert(k, "data-marke") ?? "").StartsWith("knoten:", StringComparison.Ordinal)
                                             && k.Name == "rect").ToList();
                e.Knoten = baender.Count + "/" + knoten.Count;
                if (baender.Count != d.Baender.Count)
                    e.Maengel.Add("Baender " + baender.Count + " statt " + d.Baender.Count);
                if (knoten.Count != d.Knoten.Count - 1)   // das Split-Gerät hat ohne Split kein Band
                    e.Maengel.Add("Knoten " + knoten.Count + " statt " + (d.Knoten.Count - 1));
                if (baender.Concat(knoten).Any(k => string.IsNullOrEmpty(Attributwert(k, "data-wert"))))
                    e.Maengel.Add("ein Band oder Knoten ohne data-wert");
                if (!baender.Any(k => Attributwert(k, "data-wert") == "Strom → nicht zugeordnet: 0,5 MWh/a"))
                    e.Maengel.Add("Zeigertext des Bandes „nicht zugeordnet“ fehlt");
                if (m.Befehle.OfType<WindowsFormsApplication1.Zeichnung.Text>().Any(t => t.Inhalt.Any(char.IsDigit)))
                    e.Maengel.Add("die Bildschirmfassung traegt eine Zahl im Bild (SK-Q12)");
                Zeichenmodell b = ChartRenderer.EnergieflussModell(d, bericht, true);
                if (!b.Befehle.OfType<WindowsFormsApplication1.Zeichnung.Text>().Any(t => t.Inhalt == "73,5 MWh/a" || t.Inhalt.Contains("Wärme · 73,5")))
                    e.Maengel.Add("die Berichtsfassung nennt die Jahressumme des Waermebusses nicht");
            });
        }
    }
}

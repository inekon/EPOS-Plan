#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>Art einer Bereichsfläche im Bivalenzdiagramm.</summary>
    public enum BivalenzFlaechenart
    {
        /// <summary>Nur der Kessel (B0, B4; unter dem Abschaltpunkt oder unter θ_biv,2).</summary>
        NurKessel = 0,

        /// <summary>Vorwärmung (B3): Die Wärmepumpe wärmt vor, der Kessel hebt in Reihe.</summary>
        Vorwaermung = 1,

        /// <summary>Parallel (B2): Die Wärmepumpe liefert ihr Kennfeld, der Kessel den Rest.</summary>
        Parallel = 2,

        /// <summary>Die Wärmepumpe allein (B1).</summary>
        WaermepumpeAllein = 3,
    }

    /// <summary>Art einer senkrechten Marke im Bivalenzdiagramm.</summary>
    public enum BivalenzMarkenart
    {
        /// <summary>θ_biv,1 — Kennfeld und Übergabe.</summary>
        ErsterBivalenzpunkt = 0,

        /// <summary>θ_biv,2 — Grenze der Mindestspreizung im Vorwärmbetrieb.</summary>
        ZweiterBivalenzpunkt = 1,

        /// <summary>Der eingegebene Abschaltpunkt.</summary>
        Abschaltpunkt = 2,

        /// <summary>Der Bivalenzpunkt nach Kennfeld allein, ohne Übergabe.</summary>
        NachKennfeld = 3,
    }

    /// <summary>Ein Punkt einer Kurve des Bivalenzdiagramms: Außentemperatur [°C] und Leistung.</summary>
    public readonly record struct BivalenzKurvenpunkt(double AussenC, double Leistung);

    /// <summary>Eine Bereichsfläche von <paramref name="VonC"/> bis <paramref name="BisC"/> [°C].</summary>
    public readonly record struct BivalenzFlaeche(BivalenzFlaechenart Art, double VonC, double BisC);

    /// <summary>Eine senkrechte Marke an der Außentemperatur <paramref name="AussenC"/> [°C].</summary>
    public readonly record struct BivalenzMarke(BivalenzMarkenart Art, double AussenC);

    /// <summary>Ein Stundenpunkt des Laufs: Außentemperatur der Stunde [°C] und Leistung der Wärmepumpe.</summary>
    public readonly record struct BivalenzStundenpunkt(double AussenC, double Leistung);

    /// <summary>
    /// Die Texte des Bivalenzdiagramms. Die Vorgaben sind die deutschen Texte (Proben, Tests);
    /// <see cref="AusRessourcen"/> liest dieselben Texte aus <c>BER_BILD_BIVALENZ_*</c> in der Sprache der Oberfläche.
    /// Platzhalter <c>{0}</c> nimmt eine Temperatur, fertig formatiert.
    /// </summary>
    public sealed class BivalenzdiagrammTexte
    {
        public string Titel { get; init; } = "Bivalenzdiagramm";
        public string AchseX { get; init; } = "Außentemperatur [°C]";
        public string AchseY { get; init; } = "Leistung [kW]";
        public string Heizlast { get; init; } = "Heizlast";
        public string Kennfeld { get; init; } = "Kennfeld bei {0} °C";
        public string Uebergabe { get; init; } = "Übergabe bei {0} °C";
        public string Waermepumpe { get; init; } = "Wärmepumpe";
        public string Stunden { get; init; } = "Stundenwerte Wärmepumpe";
        public string NurKessel { get; init; } = "nur Kessel";
        public string Vorwaermung { get; init; } = "Vorwärmung";
        public string Parallel { get; init; } = "parallel";
        public string WaermepumpeAllein { get; init; } = "Wärmepumpe allein";
        public string ErsterBivalenzpunkt { get; init; } = "θ_biv,1 = {0} °C";
        public string ZweiterBivalenzpunkt { get; init; } = "θ_biv,2 = {0} °C";
        public string Abschaltpunkt { get; init; } = "Abschaltpunkt {0} °C";
        public string NachKennfeld { get; init; } = "nach Kennfeld {0} °C";
        public string Platzhalter { get; init; } = "kein Bivalenzdiagramm — Kopplung aus";

        /// <summary>Dieselben Texte aus den Ressourcen <c>BER_BILD_BIVALENZ_*</c>.</summary>
        public static BivalenzdiagrammTexte AusRessourcen() => new BivalenzdiagrammTexte
        {
            Titel = MyResource.Resource.BER_BILD_BIVALENZ_TITEL,
            AchseX = MyResource.Resource.BER_BILD_BIVALENZ_ACHSE_X,
            AchseY = MyResource.Resource.BER_BILD_BIVALENZ_ACHSE_Y,
            Heizlast = MyResource.Resource.BER_BILD_BIVALENZ_HEIZLAST,
            Kennfeld = MyResource.Resource.BER_BILD_BIVALENZ_KENNFELD,
            Uebergabe = MyResource.Resource.BER_BILD_BIVALENZ_UEBERGABE,
            Waermepumpe = MyResource.Resource.BER_BILD_BIVALENZ_WAERMEPUMPE,
            Stunden = MyResource.Resource.BER_BILD_BIVALENZ_STUNDEN,
            NurKessel = MyResource.Resource.BER_BILD_BIVALENZ_NUR_KESSEL,
            Vorwaermung = MyResource.Resource.BER_BILD_BIVALENZ_VORWAERMUNG,
            Parallel = MyResource.Resource.BER_BILD_BIVALENZ_PARALLEL,
            WaermepumpeAllein = MyResource.Resource.BER_BILD_BIVALENZ_WP_ALLEIN,
            ErsterBivalenzpunkt = MyResource.Resource.BER_BILD_BIVALENZ_MARKE_BIV1,
            ZweiterBivalenzpunkt = MyResource.Resource.BER_BILD_BIVALENZ_MARKE_BIV2,
            Abschaltpunkt = MyResource.Resource.BER_BILD_BIVALENZ_MARKE_ABSCHALT,
            NachKennfeld = MyResource.Resource.BER_BILD_BIVALENZ_MARKE_KENNFELD,
            Platzhalter = MyResource.Resource.BER_BILD_BIVALENZ_PLATZHALTER,
        };
    }

    /// <summary>
    /// <b>Das Modell des Bivalenzdiagramms</b> (Fachkonzept Übergabegrenze 7.2; Umsetzungskonzept 5.4, 7.2): die Kurven,
    /// Flächen, Marken und Achsen, die <see cref="ChartRenderer.Bivalenzdiagramm"/> zeichnet — gerechnet aus der
    /// <see cref="Bivalenzherleitung"/>, ohne Datenbank. Der Renderer rechnet nichts nach.
    ///
    /// <para><b>Achsen</b>: x von der kältesten Stelle (Auslegungs-Außentemperatur, eingegebener Abschaltpunkt,
    /// Stundenpunkte) bis zur Raumtemperatur, Schritt <see cref="Skala.Rund"/> eines Achtels der Spanne, auf den Schritt
    /// gerundet; y ab null bis zum Größtwert aller Kurven und Stundenpunkte mit <see cref="Y_LUFT"/> Luft, Schritt
    /// <see cref="Skala.Rund"/> eines Fünftels, aufgerundet.</para>
    /// <para><b>Flächen</b>: die Läufe gleichen Betriebsbereichs über der Außentemperatur
    /// (<see cref="Bivalenzherleitung.Betrieb"/>), die Grenzen zwischen zwei Stützstellen durch Bisektion — so stehen sie
    /// genau auf θ_biv,1, θ_biv,2 und dem Abschaltpunkt. Nicht verfügbar zählt als „nur Kessel“.</para>
    /// <para><b>Die Leistung der Wärmepumpe</b> ist ein Linienzug über die Stützstellen im Schritt
    /// <see cref="Bivalenzherleitung.DIAGRAMM_SCHRITT_K"/> und beide Seiten jeder Bereichsgrenze; ein Sprung (θ_biv,2) wird
    /// damit senkrecht.</para>
    /// </summary>
    public sealed class BivalenzdiagrammModell
    {
        /// <summary>Luft über dem Größtwert der y-Achse (Faktor).</summary>
        public const double Y_LUFT = 1.08;

        /// <summary>Bisektionsschritte der Bereichsgrenzen (1 K / 2^40 ≪ 1e-9 K).</summary>
        private const int BISEKTION = 40;

        private BivalenzdiagrammModell() { }

        /// <summary>Hat das Modell Übergabedaten? false → Platzhalterbild.</summary>
        public bool MitUebergabe { get; private init; }

        /// <summary>Höchstvorlauf θ_WP,max [°C] — Bezug von Kennfeld und Übergabegrenze.</summary>
        public double HoechstvorlaufC { get; private init; }

        /// <summary>Heizlast über der Außentemperatur (Gerade aus dem Auslegungspunkt).</summary>
        public IReadOnlyList<BivalenzKurvenpunkt> Heizlast { get; private init; } = Array.Empty<BivalenzKurvenpunkt>();

        /// <summary>Kennfeldleistung bei θ_WP,max.</summary>
        public IReadOnlyList<BivalenzKurvenpunkt> Kennfeld { get; private init; } = Array.Empty<BivalenzKurvenpunkt>();

        /// <summary>Übergabegrenze Φ_UE,max bei θ_WP,max (Waagerechte); NaN ohne Übergabe.</summary>
        public double Uebergabegrenze { get; private init; } = double.NaN;

        /// <summary>Leistung der Wärmepumpe nach Betriebsbereich, aufsteigend; Sprünge als zwei Punkte gleicher Stelle.</summary>
        public IReadOnlyList<BivalenzKurvenpunkt> Waermepumpe { get; private init; } = Array.Empty<BivalenzKurvenpunkt>();

        /// <summary>Die Bereichsflächen, lückenlos von <see cref="XVon"/> bis <see cref="XBis"/>.</summary>
        public IReadOnlyList<BivalenzFlaeche> Flaechen { get; private init; } = Array.Empty<BivalenzFlaeche>();

        /// <summary>Die senkrechten Marken (nur vorhandene; θ_biv,2 und „nach Kennfeld“ nur, wo sie von θ_biv,1 abweichen).</summary>
        public IReadOnlyList<BivalenzMarke> Marken { get; private init; } = Array.Empty<BivalenzMarke>();

        /// <summary>Die Stundenpunkte des Laufs (endliche Werte); leer = keine.</summary>
        public IReadOnlyList<BivalenzStundenpunkt> Stundenpunkte { get; private init; } = Array.Empty<BivalenzStundenpunkt>();

        /// <summary>Linke Grenze der x-Achse [°C].</summary>
        public double XVon { get; private init; }

        /// <summary>Rechte Grenze der x-Achse [°C].</summary>
        public double XBis { get; private init; }

        /// <summary>Schritt der x-Achse [K].</summary>
        public double XSchritt { get; private init; } = 1.0;

        /// <summary>Obergrenze der y-Achse (Untergrenze 0).</summary>
        public double YBis { get; private init; } = 1.0;

        /// <summary>Schritt der y-Achse.</summary>
        public double YSchritt { get; private init; } = 1.0;

        /// <summary>Die Texte.</summary>
        public BivalenzdiagrammTexte Texte { get; private init; } = new BivalenzdiagrammTexte();

        /// <summary>Das Modell ohne Übergabedaten — der Renderer zeichnet den Platzhalter.</summary>
        public static BivalenzdiagrammModell Platzhalter(BivalenzdiagrammTexte? texte = null)
            => new BivalenzdiagrammModell { MitUebergabe = false, Texte = texte ?? new BivalenzdiagrammTexte() };

        /// <summary>
        /// Das Modell aus der <paramref name="herleitung"/>, dazu wahlweise die Stundenpunkte des Laufs (Leistung der
        /// Wärmepumpe je Stunde über der Außentemperatur). Ohne Kopplung (keine Übergabedaten) der Platzhalter.
        /// </summary>
        internal static BivalenzdiagrammModell Aus(Bivalenzherleitung herleitung,
                                                   IReadOnlyList<BivalenzStundenpunkt>? stundenpunkte = null,
                                                   BivalenzdiagrammTexte? texte = null)
        {
            if (herleitung == null) throw new ArgumentNullException(nameof(herleitung));
            texte ??= new BivalenzdiagrammTexte();
            IReadOnlyList<Diagrammpunkt> reihen = herleitung.Diagramm.Reihen;
            if (herleitung.Zustand == Herleitungszustand.OhneKopplung || reihen.Count < 2)
                return Platzhalter(texte);

            var stunden = (stundenpunkte ?? Array.Empty<BivalenzStundenpunkt>())
                .Where(p => Endlich(p.AussenC) && Endlich(p.Leistung)).ToArray();

            // Die Leistung der Wärmepumpe und die Flächen: Stützstellen der Reihen, an jeder Bereichsgrenze beide Seiten.
            var wp = new List<BivalenzKurvenpunkt>();
            var flaechen = new List<BivalenzFlaeche>();
            BivalenzFlaechenart art = Flaechenart(reihen[0].Bereich);
            double beginn = reihen[0].AussenC;
            wp.Add(new BivalenzKurvenpunkt(reihen[0].AussenC, reihen[0].Waermepumpe));
            for (int i = 1; i < reihen.Count; i++)
            {
                Diagrammpunkt links = reihen[i - 1], rechts = reihen[i];
                if (links.Bereich != rechts.Bereich)
                {
                    double lo = links.AussenC, hi = rechts.AussenC;
                    for (int k = 0; k < BISEKTION; k++)
                    {
                        double mitte = 0.5 * (lo + hi);
                        if (herleitung.Betrieb(mitte).Bereich == links.Bereich) lo = mitte; else hi = mitte;
                    }
                    wp.Add(new BivalenzKurvenpunkt(lo, herleitung.Betrieb(lo).Waermepumpe));
                    wp.Add(new BivalenzKurvenpunkt(hi, herleitung.Betrieb(hi).Waermepumpe));
                    BivalenzFlaechenart neu = Flaechenart(rechts.Bereich);
                    if (neu != art)
                    {
                        flaechen.Add(new BivalenzFlaeche(art, beginn, hi));
                        art = neu;
                        beginn = hi;
                    }
                }
                wp.Add(new BivalenzKurvenpunkt(rechts.AussenC, rechts.Waermepumpe));
            }

            // Die Achsen.
            double xMin = reihen[0].AussenC, xMax = reihen[reihen.Count - 1].AussenC;
            if (herleitung.Punkte.AbschaltpunktC is double ab && Endlich(ab)) xMin = Math.Min(xMin, ab);
            foreach (BivalenzStundenpunkt p in stunden) { xMin = Math.Min(xMin, p.AussenC); xMax = Math.Max(xMax, p.AussenC); }
            double xSchritt = Skala.Rund((xMax - xMin) / 8.0);
            xMin = Math.Floor(xMin / xSchritt) * xSchritt;
            xMax = Math.Ceiling(xMax / xSchritt) * xSchritt;
            flaechen.Add(new BivalenzFlaeche(art, beginn, xMax));
            flaechen[0] = flaechen[0] with { VonC = xMin };

            double phiUe = herleitung.PhiUeMax;
            double yMax = 0.0;
            foreach (Diagrammpunkt p in reihen) yMax = Math.Max(yMax, Math.Max(p.Heizlast, p.Kennfeld));
            if (Endlich(phiUe)) yMax = Math.Max(yMax, phiUe);
            foreach (BivalenzStundenpunkt p in stunden) yMax = Math.Max(yMax, p.Leistung);
            if (!(yMax > 0.0)) yMax = 1.0;
            double ySchritt = Skala.Rund(yMax * Y_LUFT / 5.0);
            double yBis = Math.Ceiling(yMax * Y_LUFT / ySchritt) * ySchritt;

            // Die Marken: θ_biv,2 und „nach Kennfeld“ nur, wo sie von θ_biv,1 abweichen.
            var marken = new List<BivalenzMarke>();
            Bivalenzpunkte pk = herleitung.Punkte;
            if (Endlich(pk.ErsterC)) marken.Add(new BivalenzMarke(BivalenzMarkenart.ErsterBivalenzpunkt, pk.ErsterC));
            if (Endlich(pk.ZweiterC) && !Gleich(pk.ZweiterC, pk.ErsterC))
                marken.Add(new BivalenzMarke(BivalenzMarkenart.ZweiterBivalenzpunkt, pk.ZweiterC));
            if (pk.AbschaltpunktC is double eingegeben && Endlich(eingegeben))
                marken.Add(new BivalenzMarke(BivalenzMarkenart.Abschaltpunkt, eingegeben));
            if (Endlich(pk.KennfeldAlleinC) && !Gleich(pk.KennfeldAlleinC, pk.ErsterC))
                marken.Add(new BivalenzMarke(BivalenzMarkenart.NachKennfeld, pk.KennfeldAlleinC));

            return new BivalenzdiagrammModell
            {
                MitUebergabe = Endlich(phiUe),
                HoechstvorlaufC = herleitung.HoechstvorlaufC,
                Heizlast = reihen.Select(p => new BivalenzKurvenpunkt(p.AussenC, p.Heizlast)).ToArray(),
                Kennfeld = reihen.Select(p => new BivalenzKurvenpunkt(p.AussenC, p.Kennfeld)).ToArray(),
                Uebergabegrenze = phiUe,
                Waermepumpe = wp,
                Flaechen = flaechen,
                Marken = marken,
                Stundenpunkte = stunden,
                XVon = xMin,
                XBis = xMax,
                XSchritt = xSchritt,
                YBis = yBis,
                YSchritt = ySchritt,
                Texte = texte,
            };
        }

        private static BivalenzFlaechenart Flaechenart(Betriebsbereich b) => b switch
        {
            Betriebsbereich.WpAllein => BivalenzFlaechenart.WaermepumpeAllein,
            Betriebsbereich.Parallel => BivalenzFlaechenart.Parallel,
            Betriebsbereich.Vorwaermung => BivalenzFlaechenart.Vorwaermung,
            _ => BivalenzFlaechenart.NurKessel,
        };

        private static bool Endlich(double x) => !double.IsNaN(x) && !double.IsInfinity(x);

        private static bool Gleich(double a, double b) => Math.Abs(a - b) < 1e-6;
    }
}

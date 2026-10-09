#nullable enable

using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Zustand der Herleitung (Fachkonzept Übergabegrenze 6.2; Umsetzungskonzept U‑1).</summary>
    internal enum Herleitungszustand
    {
        /// <summary>Übergabe beschrieben und <c>Einbindung</c> gesetzt: Die Übergabegrenze wirkt.</summary>
        Wirksam = 0,

        /// <summary>Übergabe beschrieben, <c>Einbindung</c> nicht gesetzt: Die Übergabegrenze ruht (U‑1).</summary>
        NichtWirksam = 1,

        /// <summary>Keine Übergabedaten (Kopplung aus): Die Wärmepumpe rechnet ohne Übergabegrenze.</summary>
        OhneKopplung = 2,
    }

    /// <summary>Gebäudeseite der Herleitung: Übergabe je Zone bzw. des Gebäudes, Heizlast und Auslegung.</summary>
    internal sealed class BivalenzGebaeudedaten
    {
        /// <summary>Übergabe je Zone; leer oder null → <see cref="Gebaeude"/> als eine Zone.</summary>
        internal IReadOnlyList<Uebergabezone>? Zonen { get; init; }

        /// <summary>Übergabe des Gebäudes (Heizkurve am Gebäude); null mit leeren Zonen → ohne Kopplung.</summary>
        internal Uebergabezone? Gebaeude { get; init; }

        /// <summary>Heizlast Φ_N bei θ_a,N (Leistungseinheit wie Übergabe und Kennfeld).</summary>
        internal double HeizlastN { get; init; }

        /// <summary>Auslegungs-Außentemperatur θ_a,N [°C].</summary>
        internal double AuslegungAussenC { get; init; }

        /// <summary>Auslegungsraumtemperatur θ_i [°C].</summary>
        internal double AuslegungRaumC { get; init; }
    }

    /// <summary>Geräteseite der Herleitung: Wärmepumpe und Kessel.</summary>
    internal sealed class BivalenzGeraetedaten
    {
        /// <summary>Höchstvorlauf θ_WP,max [°C].</summary>
        internal double HoechstvorlaufC { get; init; }

        /// <summary>Kennfeldstützpunkte bei θ_WP,max über der Quelltemperatur.</summary>
        internal IReadOnlyList<Kennfeldpunkt> Kennfeld { get; init; } = Array.Empty<Kennfeldpunkt>();

        /// <summary>Mindestspreizung σ_min [K].</summary>
        internal double SpreizungMinK { get; init; } = Bivalenzvorgaben.SPREIZUNG_MIN_K;

        /// <summary>Betriebsart des bivalenten Betriebs.</summary>
        internal Bivalenzbetriebsart Betriebsart { get; init; } = Bivalenzbetriebsart.Teilparallel;

        /// <summary>Eingegebener Abschaltpunkt (Bivalenztemperatur) [°C]; null = leer.</summary>
        internal double? AbschaltpunktC { get; init; }

        /// <summary>Vorwärmbetrieb (Kessel in Reihe).</summary>
        internal bool Vorwaermbetrieb { get; init; }

        /// <summary>Kesselleistung (Leistungseinheit wie Kennfeld); NaN oder ≤ 0 = kein Kessel.</summary>
        internal double Kesselleistung { get; init; } = double.NaN;

        /// <summary>Einbindung (<c>DIREKT</c>, <c>PUFFER</c>, <c>WEICHE</c>); leer = nicht gesetzt (U‑1).</summary>
        internal string? Einbindung { get; init; }
    }

    /// <summary>Ein Punkt der Reihen des Bivalenzdiagramms über der Außentemperatur.</summary>
    /// <param name="AussenC">Außentemperatur θ_a [°C].</param>
    /// <param name="Heizlast">Lastlineare Heizlast Φ(θ_a).</param>
    /// <param name="Kennfeld">Kennfeldleistung bei θ_WP,max.</param>
    /// <param name="Uebergabegrenze">Übergabegrenze Φ_UE,max (konstant über θ_a); NaN ohne Kopplung.</param>
    internal readonly record struct Diagrammpunkt(double AussenC, double Heizlast, double Kennfeld, double Uebergabegrenze);

    /// <summary>Art einer Marke im Bivalenzdiagramm.</summary>
    internal enum Diagrammmarkenart
    {
        ErsterBivalenzpunkt = 0,
        ZweiterBivalenzpunkt = 1,
        KennfeldAllein = 2,
        AbschaltpunktEingegeben = 3,
        AbschaltpunktMassgebend = 4,
    }

    /// <summary>Eine Marke im Bivalenzdiagramm: Art, Außentemperatur, Heizlast an der Stelle.</summary>
    internal readonly record struct Diagrammmarke(Diagrammmarkenart Art, double AussenC, double Heizlast);

    /// <summary>Das Zeichenmodell des Bivalenzdiagramms (Fachkonzept 6.2, 7.2) — Reihen und Marken, ohne Zeichnen.</summary>
    internal sealed class Bivalenzdiagramm
    {
        internal Bivalenzdiagramm(IReadOnlyList<Diagrammpunkt> reihen, IReadOnlyList<Diagrammmarke> marken)
        {
            Reihen = reihen;
            Marken = marken;
        }

        /// <summary>Die Reihen über θ_a in Schritten von <see cref="Bivalenzherleitung.DIAGRAMM_SCHRITT_K"/>, aufsteigend.</summary>
        internal IReadOnlyList<Diagrammpunkt> Reihen { get; }

        /// <summary>Die Marken (nur vorhandene Punkte).</summary>
        internal IReadOnlyList<Diagrammmarke> Marken { get; }
    }

    /// <summary>
    /// <b>Die Werte der Herleitungszeile</b> der Gruppe „Bivalenz und Übergabe" (Fachkonzept
    /// Übergabegrenze 6.2; Umsetzungskonzept 3.1) und das Diagrammmodell — ein Datenobjekt ohne
    /// Text und ohne Ressourcen; die Formatierung liegt in der Hülle. Gerechnet aus Gebäude- und
    /// Gerätedaten, ohne Lauf, bei Auslegungsraumtemperatur.
    ///
    /// <para><b>Heizkurve mehrerer Zonen</b> (Abl.): Die Heizkurve des zweiten Bivalenzpunkts folgt
    /// der Übergabe des Gebäudes; ohne sie der Zone mit dem höchsten Auslegungsvorlauf, weil sie
    /// den gemeinsamen Vorlauf bestimmt.</para>
    /// </summary>
    internal sealed class Bivalenzherleitung
    {
        /// <summary>Schrittweite der Diagrammreihen [K].</summary>
        internal const double DIAGRAMM_SCHRITT_K = 1.0;

        private Bivalenzherleitung() { }

        /// <summary>Zustand: wirksam, nicht wirksam (Einbindung leer) oder ohne Kopplung.</summary>
        internal Herleitungszustand Zustand { get; private init; }

        /// <summary>Übergabegrenze je Zone und Summe; null ohne Kopplung.</summary>
        internal Gebaeudegrenze? Grenze { get; private init; }

        /// <summary>Φ_UE,max; NaN ohne Kopplung.</summary>
        internal double PhiUeMax { get; private init; } = double.NaN;

        /// <summary>Heizlast Φ_N bei θ_a,N.</summary>
        internal double HeizlastN { get; private init; }

        /// <summary>Anteil Φ_UE,max/Φ_N [–]; NaN ohne Kopplung.</summary>
        internal double Anteil { get; private init; } = double.NaN;

        /// <summary>θ_R,UE [°C]; NaN ohne Kopplung.</summary>
        internal double RuecklaufUeC { get; private init; } = double.NaN;

        /// <summary>Δθ_UE [K]; NaN ohne Kopplung.</summary>
        internal double SpreizungUeK { get; private init; } = double.NaN;

        /// <summary>Höchstvorlauf θ_WP,max [°C].</summary>
        internal double HoechstvorlaufC { get; private init; }

        /// <summary>Die Bivalenzpunkte samt maßgebendem Abschaltpunkt.</summary>
        internal Bivalenzpunkte Punkte { get; private init; }

        /// <summary>Wärmepumpenleistung bei −7 °C (Kennfeld bei θ_WP,max).</summary>
        internal double WaermepumpeVergleich { get; private init; }

        /// <summary>Anteil der Wärmepumpenleistung bei −7 °C an der Kesselleistung [–]; NaN ohne Kessel.</summary>
        internal double HybridAnteil { get; private init; } = double.NaN;

        /// <summary>Hybrid-Mindestanteil zum Vergleich (30 % parallel/teilparallel, 40 % alternativ) [–].</summary>
        internal double HybridMindestanteil { get; private init; }

        /// <summary>Das Zeichenmodell des Bivalenzdiagramms.</summary>
        internal Bivalenzdiagramm Diagramm { get; private init; } = new Bivalenzdiagramm(Array.Empty<Diagrammpunkt>(), Array.Empty<Diagrammmarke>());

        /// <summary>Rechnet die Herleitung aus Gebäude- und Gerätedaten.</summary>
        internal static Bivalenzherleitung Rechnen(BivalenzGebaeudedaten gebaeude, BivalenzGeraetedaten geraet)
        {
            if (gebaeude == null) throw new ArgumentNullException(nameof(gebaeude));
            if (geraet == null) throw new ArgumentNullException(nameof(geraet));
            var kennfeld = new Kennfeldgerade(geraet.Kennfeld);
            double raumC = gebaeude.AuslegungRaumC;
            double hoechstvorlauf = geraet.HoechstvorlaufC;

            bool mitZonen = gebaeude.Zonen != null && gebaeude.Zonen.Count > 0;
            bool ohneKopplung = !mitZonen && gebaeude.Gebaeude == null;
            Herleitungszustand zustand = ohneKopplung ? Herleitungszustand.OhneKopplung
                : string.IsNullOrWhiteSpace(geraet.Einbindung) ? Herleitungszustand.NichtWirksam
                : Herleitungszustand.Wirksam;

            Gebaeudegrenze? grenze = ohneKopplung ? null
                : Uebergabegrenze.Gebaeude(gebaeude.Zonen, gebaeude.Gebaeude, hoechstvorlauf, raumC);
            double phiUe = grenze?.PhiUeMax ?? double.NaN;

            Bivalenzpunkte punkte = Bivalenzrechner.Punkte(gebaeude.HeizlastN, gebaeude.AuslegungAussenC, raumC,
                phiUe, kennfeld, HeizkurvenZone(gebaeude), hoechstvorlauf, geraet.SpreizungMinK,
                geraet.Vorwaermbetrieb, geraet.Betriebsart, geraet.AbschaltpunktC);

            double wpVergleich = kennfeld.Leistung(Bivalenzvorgaben.HYBRID_VERGLEICH_AUSSEN_C);
            double kessel = geraet.Kesselleistung;
            double hybridAnteil = kessel > 0.0 ? wpVergleich / kessel : double.NaN;

            return new Bivalenzherleitung
            {
                Zustand = zustand,
                Grenze = grenze,
                PhiUeMax = phiUe,
                HeizlastN = gebaeude.HeizlastN,
                Anteil = grenze != null && gebaeude.HeizlastN > 0.0 ? phiUe / gebaeude.HeizlastN : double.NaN,
                RuecklaufUeC = grenze?.RuecklaufC ?? double.NaN,
                SpreizungUeK = grenze?.SpreizungK ?? double.NaN,
                HoechstvorlaufC = hoechstvorlauf,
                Punkte = punkte,
                WaermepumpeVergleich = wpVergleich,
                HybridAnteil = hybridAnteil,
                HybridMindestanteil = geraet.Betriebsart == Bivalenzbetriebsart.Alternativ
                    ? Bivalenzvorgaben.HYBRID_MINDESTANTEIL_ALTERNATIV
                    : Bivalenzvorgaben.HYBRID_MINDESTANTEIL_PARALLEL,
                Diagramm = Diagrammmodell(gebaeude, kennfeld, phiUe, punkte),
            };
        }

        /// <summary>Die Übergabe, aus deren Auslegungspunkt die Heizkurve folgt (Abl., siehe Klassenkopf).</summary>
        private static Uebergabezone? HeizkurvenZone(BivalenzGebaeudedaten g)
        {
            if (g.Gebaeude != null) return g.Gebaeude;
            Uebergabezone? beste = null;
            if (g.Zonen != null)
                foreach (Uebergabezone z in g.Zonen)
                    if (beste == null || z.AuslegungVorlaufC > beste.AuslegungVorlaufC) beste = z;
            return beste;
        }

        private static Bivalenzdiagramm Diagrammmodell(BivalenzGebaeudedaten g, Kennfeldgerade kennfeld,
                                                       double phiUe, Bivalenzpunkte p)
        {
            double unten = g.AuslegungAussenC;
            if (p.AbschaltpunktC is double ab && ab < unten) unten = ab;
            unten = Math.Floor(unten);
            double oben = Math.Ceiling(g.AuslegungRaumC);
            int n = (int)Math.Round((oben - unten) / DIAGRAMM_SCHRITT_K);
            var reihen = new Diagrammpunkt[n + 1];
            for (int i = 0; i <= n; i++)
            {
                double ta = unten + i * DIAGRAMM_SCHRITT_K;
                reihen[i] = new Diagrammpunkt(ta, Last(g, ta), kennfeld.Leistung(ta), phiUe);
            }

            var marken = new List<Diagrammmarke>();
            void Marke(Diagrammmarkenart art, double ta)
            {
                if (!double.IsNaN(ta)) marken.Add(new Diagrammmarke(art, ta, Last(g, ta)));
            }
            Marke(Diagrammmarkenart.ErsterBivalenzpunkt, p.ErsterC);
            Marke(Diagrammmarkenart.ZweiterBivalenzpunkt, p.ZweiterC);
            Marke(Diagrammmarkenart.KennfeldAllein, p.KennfeldAlleinC);
            if (p.AbschaltpunktC is double eingegeben) Marke(Diagrammmarkenart.AbschaltpunktEingegeben, eingegeben);
            Marke(Diagrammmarkenart.AbschaltpunktMassgebend, p.MassgebendC);
            return new Bivalenzdiagramm(reihen, marken);
        }

        private static double Last(BivalenzGebaeudedaten g, double ta)
            => Bivalenzrechner.Heizlast(g.HeizlastN, g.AuslegungAussenC, g.AuslegungRaumC, ta);
    }
}

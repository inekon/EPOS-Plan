using System;

namespace WindowsFormsApplication1
{
    /// <summary>Woher ein Wert der Vorgabenkaskade stammt (<see cref="Zonenvorgaben"/>).</summary>
    public enum Vorgabeherkunft
    {
        /// <summary>Kein Wert — weder die Zone noch das Gebäude trägt einen (etwa: keine Leistungsgrenze).</summary>
        Leer,

        /// <summary>Der Wert der Zone.</summary>
        Zone,

        /// <summary>Der Wert des Gebäudes, unverändert.</summary>
        Gebaeude,

        /// <summary>Der Wert des Gebäudes, mit dem Flächenschlüssel der Zone anteilig.</summary>
        GebaeudeAnteilig,

        /// <summary>Aus anderen Werten der Zone gebildet (Volumen = Fläche × Höhe).</summary>
        Abgeleitet,
    }

    /// <summary>Ein Wert der Vorgabenkaskade samt seiner Herkunft.</summary>
    /// <param name="Wert">Der wirksame Wert; <c>null</c> = keiner.</param>
    /// <param name="Herkunft">Woher er stammt — die Anzeige „Vorgabe: …" zeigt alles außer <see cref="Vorgabeherkunft.Zone"/>.</param>
    public readonly record struct Vorgabewert(double? Wert, Vorgabeherkunft Herkunft)
    {
        /// <summary>Stammt der Wert nicht aus der Zone selbst (die Anzeige nennt ihn als Vorgabe)?</summary>
        public bool IstVorgabe => Herkunft != Vorgabeherkunft.Zone;
    }

    /// <summary>
    /// Die Werte des GEBÄUDES, aus denen eine Zone erbt (<see cref="Zonenvorgaben"/>) — Katalogsatz
    /// bzw. Projektkopie (<see cref="GebaeudeModel"/>) und Projektgebäude des Laufs
    /// (<see cref="ProjektGebaeudeModel"/>) führen dieselben Namen; beide Fabriken lesen sie gleich.
    /// Leistungsgrenzen in kW wie am Gebäude; <c>null</c> heißt „keine".
    /// </summary>
    public sealed record Gebaeudevorgaben(
        double Nutzflaeche, double Raumhoehe,
        double SollTag, double SollNacht, double SollWochenende, double SollFerien, double Maximaleraumtemperatur,
        double? HeizungStrahlungsanteil, double? HeizleistungMaxKw,
        double Luftwechselrate, double? LuftwechselInfiltration, double? LuftwechselNutzer,
        double InterneWaermegewinne, double Bewohner,
        bool KuehlungAktiv, double? KuehlSollwert, double? KuehlSollwertNacht, double? KuehlleistungMaxKw)
    {
        /// <summary>Die Vorgaben eines Katalogsatzes bzw. einer Projektkopie (Dialog).</summary>
        public static Gebaeudevorgaben Aus(GebaeudeModel g)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            return new Gebaeudevorgaben(g.Nutzflaeche, g.Raumhoehe,
                g.Raumsolltemperatur_Tag, g.Raumsolltemperatur_Nachtabsenkung, g.Raumsolltemperatur_Wochenende,
                g.Raumsolltemperatur_Ferien, g.Maximaleraumtemperatur, g.Heizung_Strahlungsanteil, g.Heizleistung_Max,
                g.Luftwechselrate, g.Luftwechsel_Infiltration, g.Luftwechsel_Nutzer, g.Interne_Waermegewinne, g.Bewohner,
                g.Kuehlung_Aktiv, g.Kuehl_Sollwert, g.Kuehl_Sollwert_Nacht, g.Kuehlleistung_Max);
        }

        /// <summary>Die Vorgaben eines Projektgebäudes, wie der Lauf es liest.</summary>
        internal static Gebaeudevorgaben Aus(ProjektGebaeudeModel g)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            return new Gebaeudevorgaben(g.Nutzflaeche, g.Raumhoehe,
                g.Raumsolltemperatur_Tag, g.Raumsolltemperatur_Nachtabsenkung, g.Raumsolltemperatur_Wochenende,
                g.Raumsolltemperatur_Ferien, g.Maximaleraumtemperatur, g.Heizung_Strahlungsanteil, g.Heizleistung_Max,
                g.Luftwechselrate, g.Luftwechsel_Infiltration, g.Luftwechsel_Nutzer, g.Interne_Waermegewinne, g.Bewohner,
                g.Kuehlung_Aktiv, g.Kuehl_Sollwert, g.Kuehl_Sollwert_Nacht, g.Kuehlleistung_Max);
        }
    }

    /// <summary>
    /// <b>Die Vorgabenkaskade einer Zone</b> (Mehrzonenkonzept 2.6, 4.2; Auftrag G6b, Welle W1) — als
    /// reine Funktion: <b>der Zonenwert, sonst der Wert des Gebäudes</b>. Dieselbe Funktion speist
    /// die Anzeige „Vorgabe: …" des Zonendialogs und den Eingang des Laufs; nach Anwenderentscheid
    /// A5 (a) gilt sie für jede Zahl der Zonen, auch für genau eine.
    ///
    /// <para><b>Die Regeln je Wert:</b>
    /// <list type="bullet">
    /// <item>Nutzfläche, Raumhöhe, die vier Sollwerte, <c>Maximaleraumtemperatur</c>,
    /// <c>Heizung_Strahlungsanteil</c>, Infiltration und Nutzerlüftung: Zone, sonst Gebäude.</item>
    /// <item>Volumen: Zone, sonst Nutzfläche × Raumhöhe der Kaskade (abgeleitet).</item>
    /// <item><b>Der Flächenschlüssel</b> Anteil = A_Zone / A_Gebäude (wie
    /// <c>GebaeudeModellEingang.Flaechenschluessel</c>; eine Zone ohne eigene Nutzfläche hat den
    /// Anteil genau 1): innere Gewinne und Bewohner — Zone, sonst Gebäude × Anteil.</item>
    /// <item><b>Leistungsgrenzen</b> (Heizung und Kühlung, Festlegung 5): Zone, sonst ab zwei Zonen
    /// Gebäude × Anteil; bei einer Zone der Wert des Gebäudes unverändert (G3-Stand).</item>
    /// <item><b>Kühlung</b> (Anwenderentscheid A4 (a)): Schalter, Kühlsollwert und Nachtwert kommen
    /// vom Gebäude, die Kühlspalten der Zone bleiben ungelesen; nur die Grenze wird wie oben
    /// anteilig.</item>
    /// <item><c>IstBeheizt</c> ist ein Schalter der Zone ohne Gebäudewert (Festlegung 2); die Nachtzeit
    /// kommt vom Gebäude (Festlegung 1) und steht deshalb nicht hier.</item>
    /// </list></para>
    ///
    /// <para><b>Bitgleich, wo nichts übersteuert ist:</b> Jeder geerbte Wert ist der Gebäudewert selbst
    /// oder das Produkt mit dem Anteil in derselben Schreibweise wie der Lauf (Wert × (A_Zone /
    /// A_Gebäude)); der Anteil 1 ändert kein Bit. Ohne Datenbank, ohne Zustand.</para>
    /// </summary>
    public sealed record Zonenvorgaben(
        Vorgabewert Nutzflaeche, Vorgabewert Raumhoehe, Vorgabewert Volumen, bool IstBeheizt, double Flaechenanteil,
        Vorgabewert SollTag, Vorgabewert SollNacht, Vorgabewert SollWochenende, Vorgabewert SollFerien,
        Vorgabewert Maximaleraumtemperatur, Vorgabewert HeizungStrahlungsanteil, Vorgabewert HeizleistungMaxKw,
        Vorgabewert LuftwechselInfiltration, Vorgabewert LuftwechselNutzer, double Luftwechselrate,
        Vorgabewert InterneWaermegewinne, Vorgabewert Bewohner,
        bool KuehlungAktiv, Vorgabewert KuehlSollwert, Vorgabewert KuehlSollwertNacht, Vorgabewert KuehlleistungMaxKw)
    {
        /// <summary>
        /// Bildet die Kaskade der Zone <paramref name="zone"/> eines Gebäudes mit den Vorgaben
        /// <paramref name="gebaeude"/> und <paramref name="zonenzahl"/> Zonen (Klassenkopf).
        /// </summary>
        /// <exception cref="ArgumentNullException">ohne Zone oder Gebäude.</exception>
        public static Zonenvorgaben Bilden(ZoneModel zone, Gebaeudevorgaben gebaeude, int zonenzahl)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            if (gebaeude == null) throw new ArgumentNullException(nameof(gebaeude));

            Vorgabewert flaeche = Erben(zone.Nutzflaeche, gebaeude.Nutzflaeche);
            Vorgabewert hoehe = Erben(zone.Raumhoehe, gebaeude.Raumhoehe);
            Vorgabewert volumen = zone.Volumen.HasValue
                ? new Vorgabewert(zone.Volumen, Vorgabeherkunft.Zone)
                : new Vorgabewert(flaeche.Wert.HasValue && hoehe.Wert.HasValue ? flaeche.Wert.Value * hoehe.Wert.Value : (double?)null,
                                  Vorgabeherkunft.Abgeleitet);

            // Der Flaechenschluessel: ohne eigene (oder mit gleicher) Nutzflaeche genau 1, sonst
            // A_Zone / A_Gebaeude; ohne Nutzflaeche des Gebaeudes gibt es keinen Schluessel (NaN) -
            // kein stiller Rueckfall, der Lauf lehnt das benannt ab.
            double anteil = 1.0;
            if (zone.Nutzflaeche.HasValue && zone.Nutzflaeche.Value != gebaeude.Nutzflaeche)
                anteil = gebaeude.Nutzflaeche > 0.0 && !double.IsInfinity(gebaeude.Nutzflaeche)
                    ? zone.Nutzflaeche.Value / gebaeude.Nutzflaeche
                    : double.NaN;
            bool mehrere = zonenzahl >= 2;

            return new Zonenvorgaben(
                flaeche, hoehe, volumen, zone.IstBeheizt, anteil,
                Erben(zone.Raumsolltemperatur_Tag, gebaeude.SollTag),
                Erben(zone.Raumsolltemperatur_Nachtabsenkung, gebaeude.SollNacht),
                Erben(zone.Raumsolltemperatur_Wochenende, gebaeude.SollWochenende),
                Erben(zone.Raumsolltemperatur_Ferien, gebaeude.SollFerien),
                Erben(zone.Maximaleraumtemperatur, gebaeude.Maximaleraumtemperatur),
                Erben(zone.Heizung_Strahlungsanteil, gebaeude.HeizungStrahlungsanteil),
                Grenze(zone.Heizleistung_Max, gebaeude.HeizleistungMaxKw, anteil, mehrere),
                Erben(zone.Luftwechsel_Infiltration, gebaeude.LuftwechselInfiltration),
                Erben(zone.Luftwechsel_Nutzer, gebaeude.LuftwechselNutzer),
                gebaeude.Luftwechselrate,
                Anteilig(zone.Interne_Waermegewinne, gebaeude.InterneWaermegewinne, anteil),
                Anteilig(zone.Bewohner, gebaeude.Bewohner, anteil),
                gebaeude.KuehlungAktiv,
                Gebaeudewert(gebaeude.KuehlSollwert),
                Gebaeudewert(gebaeude.KuehlSollwertNacht),
                Grenze(null, gebaeude.KuehlleistungMaxKw, anteil, mehrere));
        }

        /// <summary>Zone, sonst Gebäude (auch ein fehlender Gebäudewert bleibt fehlend).</summary>
        private static Vorgabewert Erben(double? zone, double? gebaeude)
            => zone.HasValue ? new Vorgabewert(zone, Vorgabeherkunft.Zone) : Gebaeudewert(gebaeude);

        private static Vorgabewert Gebaeudewert(double? gebaeude)
            => new Vorgabewert(gebaeude, gebaeude.HasValue ? Vorgabeherkunft.Gebaeude : Vorgabeherkunft.Leer);

        /// <summary>Zone, sonst Gebäude × Anteil — beim Anteil 1 der Gebäudewert selbst.</summary>
        private static Vorgabewert Anteilig(double? zone, double gebaeude, double anteil)
        {
            if (zone.HasValue) return new Vorgabewert(zone, Vorgabeherkunft.Zone);
            return anteil == 1.0 ? new Vorgabewert(gebaeude, Vorgabeherkunft.Gebaeude)
                                 : new Vorgabewert(gebaeude * anteil, Vorgabeherkunft.GebaeudeAnteilig);
        }

        /// <summary>Eine Leistungsgrenze: Zone, sonst ab zwei Zonen Gebäude × Anteil, bei einer Zone das Gebäude (Festlegung 5).</summary>
        private static Vorgabewert Grenze(double? zone, double? gebaeude, double anteil, bool mehrere)
        {
            if (zone.HasValue) return new Vorgabewert(zone, Vorgabeherkunft.Zone);
            if (!gebaeude.HasValue) return new Vorgabewert(null, Vorgabeherkunft.Leer);
            return mehrere && anteil != 1.0 ? new Vorgabewert(gebaeude.Value * anteil, Vorgabeherkunft.GebaeudeAnteilig)
                                            : new Vorgabewert(gebaeude, Vorgabeherkunft.Gebaeude);
        }
    }
}

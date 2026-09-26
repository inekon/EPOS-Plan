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

        /// <summary>
        /// Die Bewohner aus Nutzfläche [m²] und Fläche je Nutzer [m²] — dieselbe Ableitung wie beim
        /// Schreiben des Gebäudes („Fläche je Nutzer 0 → Vorgabe 35 m²"); der Gebäudedialog bildet damit
        /// die Vorgaben seiner Zonen aus dem Arbeitsstand.
        /// </summary>
        public static double BewohnerAusFlaeche(double nutzflaeche, double flaecheJeNutzer)
            => nutzflaeche / (flaecheJeNutzer == 0 ? GebaeudeStammCtrl.FLAECHE_JE_NUTZER_VORGABE : flaecheJeNutzer);

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
    /// <b>Die Eingaben EINER Zone</b>, aus denen die Vorgabenkaskade liest (<see cref="Zonenvorgaben"/>) —
    /// die Spalten von <c>Tab_Zone</c> ohne Fachklasse, damit der Zonendialog seine Anzeige „Vorgabe: …"
    /// aus DERSELBEN Funktion bildet wie der Lauf (Auftrag G6b, Welle W2). <c>null</c> heißt „leer" —
    /// der Wert des Gebäudes gilt; die Kühlspalten der Zone bleiben ungelesen (Anwenderentscheid A4 (a)).
    /// </summary>
    public sealed record Zoneneingaben(
        double? Nutzflaeche = null, double? Raumhoehe = null, double? Volumen = null, bool IstBeheizt = true,
        double? SollTag = null, double? SollNacht = null, double? SollWochenende = null, double? SollFerien = null,
        double? Maximaleraumtemperatur = null, double? HeizungStrahlungsanteil = null, double? HeizleistungMaxKw = null,
        double? LuftwechselInfiltration = null, double? LuftwechselNutzer = null,
        double? InterneWaermegewinne = null, double? Bewohner = null)
    {
        /// <summary>Die Eingaben einer gespeicherten Zone.</summary>
        public static Zoneneingaben Aus(ZoneModel z)
        {
            if (z == null) throw new ArgumentNullException(nameof(z));
            return new Zoneneingaben(z.Nutzflaeche, z.Raumhoehe, z.Volumen, z.IstBeheizt,
                z.Raumsolltemperatur_Tag, z.Raumsolltemperatur_Nachtabsenkung, z.Raumsolltemperatur_Wochenende,
                z.Raumsolltemperatur_Ferien, z.Maximaleraumtemperatur, z.Heizung_Strahlungsanteil, z.Heizleistung_Max,
                z.Luftwechsel_Infiltration, z.Luftwechsel_Nutzer, z.Interne_Waermegewinne, z.Bewohner);
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
            return Bilden(Zoneneingaben.Aus(zone), gebaeude, zonenzahl);
        }

        /// <summary>
        /// Dasselbe aus den Eingaben der Zone (<see cref="Zoneneingaben"/>) — der Weg des Zonendialogs,
        /// der keine Fachklasse kennt.
        /// </summary>
        /// <exception cref="ArgumentNullException">ohne Zone oder Gebäude.</exception>
        public static Zonenvorgaben Bilden(Zoneneingaben zone, Gebaeudevorgaben gebaeude, int zonenzahl)
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
                Erben(zone.SollTag, gebaeude.SollTag),
                Erben(zone.SollNacht, gebaeude.SollNacht),
                Erben(zone.SollWochenende, gebaeude.SollWochenende),
                Erben(zone.SollFerien, gebaeude.SollFerien),
                Erben(zone.Maximaleraumtemperatur, gebaeude.Maximaleraumtemperatur),
                Erben(zone.HeizungStrahlungsanteil, gebaeude.HeizungStrahlungsanteil),
                Grenze(zone.HeizleistungMaxKw, gebaeude.HeizleistungMaxKw, anteil, mehrere),
                Erben(zone.LuftwechselInfiltration, gebaeude.LuftwechselInfiltration),
                Erben(zone.LuftwechselNutzer, gebaeude.LuftwechselNutzer),
                gebaeude.Luftwechselrate,
                Anteilig(zone.InterneWaermegewinne, gebaeude.InterneWaermegewinne, anteil),
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

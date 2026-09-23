namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Randbedingungen EINER Blockstunde für <see cref="Zonenmodell2K.Schritt"/>
    /// (Umsetzungskonzept 1.3). Alle Größen gelten über die ganze Stunde konstant.
    ///
    /// <para><b>Temperaturen</b> in °C: Außenluft (masseloser Zweig), äquivalente
    /// Außentemperatur am Außenwandpfad, Heizsollwert, obere Grenze der Raumluft (der
    /// Kühlsollwert der idealen Kühlung). <b>Lasten</b> in W, positiv = Wärme in den Raum: der
    /// Strahlungsanteil je Oberflächenknoten und der konvektive Anteil an der Luft —
    /// verteilt hat sie bereits der Aufrufer (Schritt E).</para>
    ///
    /// <para><b>Abschalten über NaN.</b> <see cref="ThetaSoll"/> = NaN heißt „keine
    /// Heizung", <see cref="ThetaMax"/> = NaN oder +∞ heißt „keine Kühlung, die Raumluft läuft
    /// nach oben frei" (so rechnet jedes Gebäude ohne wirksame Kühlung, Entscheid E32),
    /// <see cref="HeizleistungMaxW"/> bzw. <see cref="KuehlleistungMaxW"/> = NaN heißt
    /// „unbegrenzt". Die übrigen Größen müssen endlich sein.</para>
    ///
    /// <para><b>Übergabe der Leistung.</b> Die Heizleistung geht zum Anteil
    /// <see cref="HeizungStrahlungsanteil"/> als Strahlung flächenproportional auf die
    /// beiden Oberflächenknoten, der Rest konvektiv an die Luft. Die Kühlleistung wirkt
    /// konvektiv an der Luft; zum Anteil <see cref="KuehlungAnteilInnenflaeche"/> greift
    /// sie an der Innenbauteiloberfläche an (Flächenkühlung, etwa eine Kühldecke).</para>
    /// </summary>
    internal readonly struct Stundenrand
    {
        internal Stundenrand(
            double thetaOut,
            double thetaEq,
            double thetaSoll,
            double thetaMax,
            double phiRadAW,
            double phiRadIW,
            double phiConv,
            double heizleistungMaxW = double.NaN,
            double kuehlleistungMaxW = double.NaN,
            double heizungStrahlungsanteil = 0.0,
            double kuehlungAnteilInnenflaeche = 0.0,
            double zusatzleitwertWK = 0.0)
        {
            ThetaOut = thetaOut;
            ThetaEq = thetaEq;
            ThetaSoll = thetaSoll;
            ThetaMax = thetaMax;
            PhiRadAW = phiRadAW;
            PhiRadIW = phiRadIW;
            PhiConv = phiConv;
            HeizleistungMaxW = heizleistungMaxW;
            KuehlleistungMaxW = kuehlleistungMaxW;
            HeizungStrahlungsanteil = heizungStrahlungsanteil;
            KuehlungAnteilInnenflaeche = kuehlungAnteilInnenflaeche;
            ZusatzleitwertWK = zusatzleitwertWK;
        }

        /// <summary>Außenlufttemperatur am masselosen Zweig [°C].</summary>
        internal double ThetaOut { get; }

        /// <summary>Äquivalente Außentemperatur am Außenwandpfad [°C].</summary>
        internal double ThetaEq { get; }

        /// <summary>Heizsollwert der Raumluft [°C]; NaN = keine Heizung.</summary>
        internal double ThetaSoll { get; }

        /// <summary>Obere Grenze der Raumluft, Kühlsollwert [°C]; NaN oder +∞ = keine Kühlung.</summary>
        internal double ThetaMax { get; }

        /// <summary>Strahlungslast auf die Außenbauteiloberfläche [W].</summary>
        internal double PhiRadAW { get; }

        /// <summary>Strahlungslast auf die Innenbauteiloberfläche [W].</summary>
        internal double PhiRadIW { get; }

        /// <summary>Konvektive Last an der Raumluft [W].</summary>
        internal double PhiConv { get; }

        /// <summary>Größte Heizleistung [W]; NaN = unbegrenzt.</summary>
        internal double HeizleistungMaxW { get; }

        /// <summary>Größte Kühlleistung [W], als positiver Betrag; NaN = unbegrenzt.</summary>
        internal double KuehlleistungMaxW { get; }

        /// <summary>Strahlungsanteil der Heizübergabe [–], 0 = rein konvektiv.</summary>
        internal double HeizungStrahlungsanteil { get; }

        /// <summary>Anteil der Kühlleistung an der Innenbauteiloberfläche [–], 0 = rein konvektiv.</summary>
        internal double KuehlungAnteilInnenflaeche { get; }

        /// <summary>
        /// Zusätzlicher masseloser Leitwert Außenluft ↔ Raumluft in dieser Stunde [W/K], etwa
        /// ein stündlich wechselnder Luftwechsel; er liegt parallel zu R_ext. 0 = nur R_ext.
        /// </summary>
        internal double ZusatzleitwertWK { get; }

        /// <summary>Ist eine Heizung vorhanden?</summary>
        internal bool MitHeizung => !double.IsNaN(ThetaSoll);

        /// <summary>Ist eine Kühlung (obere Regelgrenze der idealen Regelung) vorhanden?</summary>
        internal bool MitKuehlung => !double.IsNaN(ThetaMax) && !double.IsPositiveInfinity(ThetaMax);
    }

    /// <summary>
    /// Das Ergebnis EINER Blockstunde (Umsetzungskonzept 1.3). Alle Leistungen und
    /// Temperaturen sind <b>Blockmittel</b> der Stunde — die Prüfgröße der Richtlinie
    /// (Konzept N1.2) —, nur die beiden Massentemperaturen am Ende sind Momentanwerte.
    ///
    /// <para><b>Vorzeichen:</b> <see cref="HeizleistungW"/> und <see cref="KuehlleistungW"/>
    /// sind beide ≥ 0. Eine Stunde mit Fallwechsel kann beide größer null tragen; die
    /// Heizlast der Richtlinie ist <c>HeizleistungW − KuehlleistungW</c>.</para>
    /// </summary>
    internal readonly struct Stundenergebnis
    {
        internal Stundenergebnis(
            double heizleistungW,
            double kuehlleistungW,
            double thetaAirMittel,
            double thetaOpMittel,
            double thetaSAwMittel,
            double thetaSIwMittel,
            double thetaMAwMittel,
            double thetaMIwMittel,
            double thetaMAwEnde,
            double thetaMIwEnde,
            int abschnitte)
        {
            HeizleistungW = heizleistungW;
            KuehlleistungW = kuehlleistungW;
            ThetaAirMittel = thetaAirMittel;
            ThetaOpMittel = thetaOpMittel;
            ThetaSAwMittel = thetaSAwMittel;
            ThetaSIwMittel = thetaSIwMittel;
            ThetaMAwMittel = thetaMAwMittel;
            ThetaMIwMittel = thetaMIwMittel;
            ThetaMAwEnde = thetaMAwEnde;
            ThetaMIwEnde = thetaMIwEnde;
            Abschnitte = abschnitte;
        }

        /// <summary>Mittlere Heizleistung der Stunde [W], ≥ 0.</summary>
        internal double HeizleistungW { get; }

        /// <summary>Mittlere Kühlleistung der Stunde [W], ≥ 0.</summary>
        internal double KuehlleistungW { get; }

        /// <summary>Heizlast mit dem Vorzeichen der Richtlinie [W]: Heizen positiv, Kühlen negativ.</summary>
        internal double LastW => HeizleistungW - KuehlleistungW;

        /// <summary>Mittlere Raumlufttemperatur [°C].</summary>
        internal double ThetaAirMittel { get; }

        /// <summary>Mittlere operative Temperatur [°C]: halb Luft, halb flächengewichtete Oberflächen.</summary>
        internal double ThetaOpMittel { get; }

        /// <summary>Mittlere Temperatur der Außenbauteiloberfläche [°C].</summary>
        internal double ThetaSAwMittel { get; }

        /// <summary>Mittlere Temperatur der Innenbauteiloberfläche [°C].</summary>
        internal double ThetaSIwMittel { get; }

        /// <summary>Mittlere Temperatur des Außenbauteil-Massenknotens [°C].</summary>
        internal double ThetaMAwMittel { get; }

        /// <summary>Mittlere Temperatur des Innenbauteil-Massenknotens [°C].</summary>
        internal double ThetaMIwMittel { get; }

        /// <summary>Temperatur des Außenbauteil-Massenknotens am Stundenende [°C].</summary>
        internal double ThetaMAwEnde { get; }

        /// <summary>Temperatur des Innenbauteil-Massenknotens am Stundenende [°C].</summary>
        internal double ThetaMIwEnde { get; }

        /// <summary>Zahl der Abschnitte (Betriebsfälle) in dieser Stunde, ≥ 1.</summary>
        internal int Abschnitte { get; }
    }
}

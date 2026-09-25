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
    ///
    /// <para><b>Anlagenkopplung (Stufe AK1, Konzept Anlagenkopplung 6.1, 10.2).</b> Mit
    /// <see cref="Uebergabe"/> ≠ <c>null</c> ist die Heizung keine ideale Regelung mehr, sondern
    /// eine Übergabe bei <see cref="VorlaufC"/> mit dem Raumregler <see cref="ReglerbandK"/>
    /// (Schritt H). <c>null</c> heißt: der Bestandsweg, Zeichen für Zeichen.</para>
    ///
    /// <para><b>Kälteseite der Kopplung (Schritt K, E37).</b> Mit
    /// <see cref="KuehlUebergabeGespiegelt"/> ≠ <c>null</c> ist die Kühlung keine ideale Regelung
    /// mehr, sondern eine Kühlübergabe beim festen Kaltwasser-Vorlauf <see cref="KuehlVorlaufC"/>
    /// mit demselben Raumregler <see cref="ReglerbandK"/>; sie setzt eine Kühlung voraus
    /// (<see cref="MitKuehlung"/>). <c>null</c> heißt: die Kühlung des Bestands.</para>
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
            double zusatzleitwertWK = 0.0,
            Uebergabekennwerte uebergabe = null,
            double vorlaufC = double.NaN,
            double reglerbandK = 0.0,
            Uebergabekennwerte kuehlUebergabeGespiegelt = null,
            double kuehlVorlaufC = double.NaN,
            double kuehlStrahlungsanteil = 0.0,
            bool kuehlVorlaufGekappt = false)
        {
            KuehlUebergabeGespiegelt = kuehlUebergabeGespiegelt;
            KuehlVorlaufC = kuehlVorlaufC;
            KuehlStrahlungsanteil = kuehlStrahlungsanteil;
            KuehlVorlaufGekappt = kuehlVorlaufGekappt;
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
            Uebergabe = uebergabe;
            VorlaufC = vorlaufC;
            ReglerbandK = reglerbandK;
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

        /// <summary>
        /// Die Kennwerte der Wärmeübergabe (Anlagenkopplung AK1); <c>null</c> = ideale Regelung,
        /// der Bestandsweg.
        /// </summary>
        internal Uebergabekennwerte Uebergabe { get; }

        /// <summary>
        /// Vorlauf der Stunde [°C] aus Heizkurve oder Festwert (Schritt E); NaN = Heizkurve aus
        /// (Heizgrenze) — die Übergabe liefert dann nichts. Nur mit <see cref="Uebergabe"/>.
        /// </summary>
        internal double VorlaufC { get; }

        /// <summary>Proportionalband Xp des Raumreglers [K]; 0 = ideale Regelung mit Grenze (H1, E25).</summary>
        internal double ReglerbandK { get; }

        /// <summary>Ist eine Heizung vorhanden?</summary>
        internal bool MitHeizung => !double.IsNaN(ThetaSoll);

        /// <summary>Ist eine Kühlung (obere Regelgrenze der idealen Regelung) vorhanden?</summary>
        internal bool MitKuehlung => !double.IsNaN(ThetaMax) && !double.IsPositiveInfinity(ThetaMax);

        /// <summary>Rechnet die Heizung als Übergabe (Anlagenkopplung, Schritt H)?</summary>
        internal bool MitUebergabe => Uebergabe != null;

        /// <summary>
        /// Die Kennwerte der Kühlübergabe, <b>gespiegelt</b> (−V, −R, −θ_i,N; Schritt K, E37);
        /// <c>null</c> = ideale Kühlung, der Bestandsweg.
        /// </summary>
        internal Uebergabekennwerte KuehlUebergabeGespiegelt { get; }

        /// <summary>Fester Kaltwasser-Vorlauf der Stunde [°C], schon auf die Vorlaufgrenze hochgemischt (7.2). Nur mit Kühlübergabe.</summary>
        internal double KuehlVorlaufC { get; }

        /// <summary>Strahlungsanteil der Kühlübergabe [–] (Vorgabe der Art); verteilt wie die Heizseite.</summary>
        internal double KuehlStrahlungsanteil { get; }

        /// <summary>Steht der Kaltwasser-Vorlauf an der Vorlaufgrenze, weil die Anlage kälter liefert (7.2)?</summary>
        internal bool KuehlVorlaufGekappt { get; }

        /// <summary>Rechnet die Kühlung als Kühlübergabe (Anlagenkopplung, Schritt K, E37)?</summary>
        internal bool MitKuehluebergabe => KuehlUebergabeGespiegelt != null;
    }

    /// <summary>
    /// Das Ergebnis EINER Blockstunde (Umsetzungskonzept 1.3). Alle Leistungen und
    /// Temperaturen sind <b>Blockmittel</b> der Stunde — die Prüfgröße der Richtlinie
    /// (Konzept N1.2) —, nur die beiden Massentemperaturen am Ende sind Momentanwerte.
    ///
    /// <para><b>Vorzeichen:</b> <see cref="HeizleistungW"/> und <see cref="KuehlleistungW"/>
    /// sind beide ≥ 0. Eine Stunde mit Fallwechsel kann beide größer null tragen; die
    /// Heizlast der Richtlinie ist <c>HeizleistungW − KuehlleistungW</c>.</para>
    ///
    /// <para><b>Anlagenkopplung (AK1).</b> Mit Übergabe trägt die Stunde zusätzlich Vorlauf,
    /// Rücklauf zur GELIEFERTEN Leistung (H6), den überwiegenden Begrenzungsgrund und die
    /// Zeitanteile der Gründe; ohne Übergabe stehen sie auf NaN bzw. null. Mit Kühlübergabe
    /// (Schritt K, E37) trägt sie dasselbe für die Kälteseite, je Seite getrennt.</para>
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
            int abschnitte,
            double vorlaufC = double.NaN,
            double ruecklaufC = double.NaN,
            Begrenzungsgrund begrenzungsgrund = Begrenzungsgrund.KeineBegrenzung,
            double uebergabeBegrenztAnteil = 0.0,
            double heizleistungMaxAnteil = 0.0,
            double heizgrenzeAnteil = 0.0,
            double kuehlVorlaufC = double.NaN,
            double kuehlRuecklaufC = double.NaN,
            Begrenzungsgrund kuehlBegrenzungsgrund = Begrenzungsgrund.KeineBegrenzung,
            double kuehlUebergabeBegrenztAnteil = 0.0,
            double vorlaufgrenzeAnteil = 0.0,
            double kuehlleistungMaxAnteil = 0.0,
            double keineKaelteAnteil = 0.0)
        {
            KuehlVorlaufC = kuehlVorlaufC;
            KuehlRuecklaufC = kuehlRuecklaufC;
            KuehlBegrenzungsgrund = kuehlBegrenzungsgrund;
            KuehlUebergabeBegrenztAnteil = kuehlUebergabeBegrenztAnteil;
            VorlaufgrenzeAnteil = vorlaufgrenzeAnteil;
            KuehlleistungMaxAnteil = kuehlleistungMaxAnteil;
            KeineKaelteAnteil = keineKaelteAnteil;
            VorlaufC = vorlaufC;
            RuecklaufC = ruecklaufC;
            Begrenzungsgrund = begrenzungsgrund;
            UebergabeBegrenztAnteil = uebergabeBegrenztAnteil;
            HeizleistungMaxAnteil = heizleistungMaxAnteil;
            HeizgrenzeAnteil = heizgrenzeAnteil;
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

        /// <summary>Vorlauf der Stunde [°C] (Anlagenkopplung); NaN ohne Übergabe oder jenseits der Heizgrenze.</summary>
        internal double VorlaufC { get; }

        /// <summary>
        /// Rücklauf der Stunde [°C], θ_V − Φ̄/W_H zur gelieferten mittleren Leistung (H6, H-F2) —
        /// das Stundenmittel des Rücklaufs bei konstantem Massenstrom; NaN wie <see cref="VorlaufC"/>.
        /// </summary>
        internal double RuecklaufC { get; }

        /// <summary>Der Begrenzungsgrund mit dem größten Zeitanteil der Stunde (Anlagenkopplung 4.5).</summary>
        internal Begrenzungsgrund Begrenzungsgrund { get; }

        /// <summary>Zeitanteil der Stunde, in dem die Übergabe die Grenze war [–], 0 … 1.</summary>
        internal double UebergabeBegrenztAnteil { get; }

        /// <summary>Zeitanteil der Stunde, in dem <c>Heizleistung_Max</c> gekappt hat [–].</summary>
        internal double HeizleistungMaxAnteil { get; }

        /// <summary>Zeitanteil der Stunde an der Heizgrenze der Übergabe [–] (Heizkurve aus oder Vorlauf nicht über der Raumluft).</summary>
        internal double HeizgrenzeAnteil { get; }

        /// <summary>War die Übergabe in dieser Stunde die Grenze — ja/nein?</summary>
        internal bool UebergabeBegrenzt => UebergabeBegrenztAnteil > 0.0;

        // ---- die Kälteseite der Kopplung (Schritt K, E37) ----

        /// <summary>Kaltwasser-Vorlauf der Stunde [°C] (fest, 7.2); NaN ohne Kühlübergabe.</summary>
        internal double KuehlVorlaufC { get; }

        /// <summary>
        /// Rücklauf der Kühlübergabe [°C], θ_V + Φ̄_c/W_K zur gelieferten mittleren Kühlleistung —
        /// der Spiegel von <see cref="RuecklaufC"/>; NaN wie <see cref="KuehlVorlaufC"/>.
        /// </summary>
        internal double KuehlRuecklaufC { get; }

        /// <summary>Der Begrenzungsgrund der Kälteseite mit dem größten Zeitanteil der Stunde.</summary>
        internal Begrenzungsgrund KuehlBegrenzungsgrund { get; }

        /// <summary>
        /// Zeitanteil der Stunde, in dem die Kühlübergabe die Grenze war [–] — einschließlich der
        /// Zeit an der Vorlaufgrenze (<see cref="VorlaufgrenzeAnteil"/>).
        /// </summary>
        internal double KuehlUebergabeBegrenztAnteil { get; }

        /// <summary>Zeitanteil der Stunde, in dem die gesättigte Kühlübergabe an der Vorlaufgrenze stand [–] (7.2).</summary>
        internal double VorlaufgrenzeAnteil { get; }

        /// <summary>Zeitanteil der Stunde, in dem <c>Kuehlleistung_Max</c> gekappt hat [–] (gekoppelt).</summary>
        internal double KuehlleistungMaxAnteil { get; }

        /// <summary>Zeitanteil der Stunde, in dem die Kühlübergabe nichts lieferte [–] (Vorlauf nicht unter der Raumluft).</summary>
        internal double KeineKaelteAnteil { get; }

        /// <summary>War die Kühlübergabe in dieser Stunde die Grenze — ja/nein?</summary>
        internal bool KuehlUebergabeBegrenzt => KuehlUebergabeBegrenztAnteil > 0.0;
    }
}

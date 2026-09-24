using System;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die fünf Betriebsfälle der idealen Regelung (Rechenschritte 7.1, Kühlkonzept 3.2) und
    /// die zwei Sättigungszustände des Falls „Übergabe begrenzt" der Anlagenkopplung
    /// (Anlagenkopplung 10.2 H5, Rechenschritte 7.4). Jeder Abschnitt einer Stunde hat genau
    /// einen davon.
    /// </summary>
    internal enum Betriebsfall
    {
        /// <summary>Die Heizung hält die Raumluft auf dem Sollwert; Leistung zwischen 0 und der Grenze.</summary>
        HeizenGeregelt,

        /// <summary>Die Heizung liefert ihre Grenzleistung; die Raumluft liegt unter dem Sollwert.</summary>
        Heizgrenze,

        /// <summary>Weder Heizen noch Kühlen; die Raumluft liegt zwischen Sollwert und oberer Grenze.</summary>
        Totband,

        /// <summary>Die Kühlung hält die Raumluft auf der oberen Grenze, dem Kühlsollwert.</summary>
        KuehlenGeregelt,

        /// <summary>Die Kühlung liefert ihre Grenzleistung; die Raumluft liegt über der oberen Grenze.</summary>
        Kuehlgrenze,

        /// <summary>
        /// Anlagenkopplung, „Übergabe begrenzt", gesättigt (y = 1): Das Ventil steht voll offen,
        /// die Übergabe hängt als Sekantenleitwert G_H gegen θ_H im freien Lauf, die Raumluft
        /// liegt unter θ_soll − Xp.
        /// </summary>
        UebergabeGesaettigt,

        /// <summary>
        /// Anlagenkopplung, „Übergabe begrenzt", Regelbereich (0 &lt; y &lt; 1): Der P-Regler
        /// drosselt; der Leitwert der gefahrenen Kennlinie ist Φ_ue,max/Xp + y·G_H, die Raumluft
        /// liegt zwischen θ_soll − Xp und θ_soll.
        /// </summary>
        UebergabeRegelbereich,

        /// <summary>
        /// Anlagenkopplung, Kälteseite (Schritt K, E37), „Kühlübergabe begrenzt", gesättigt (y = 1):
        /// Das Ventil steht voll offen, die Kühlübergabe hängt als Sekantenleitwert G gegen θ_K im
        /// freien Lauf, die Raumluft liegt über θ_kühl + Xp. Das Spiegelbild von
        /// <see cref="UebergabeGesaettigt"/>.
        /// </summary>
        KuehluebergabeGesaettigt,

        /// <summary>
        /// Anlagenkopplung, Kälteseite (Schritt K, E37), Regelbereich (0 &lt; y &lt; 1): Der
        /// P-Regler drosselt die Kühlübergabe, die Raumluft liegt zwischen θ_kühl und
        /// θ_kühl + Xp. Das Spiegelbild von <see cref="UebergabeRegelbereich"/>.
        /// </summary>
        KuehluebergabeRegelbereich,
    }

    /// <summary>
    /// Welche Seite der Übergabe ein gekoppelter Abschnitt rechnet (Anlagenkopplung 10.2 und
    /// 10.5): die Wärmeseite (Schritt H) oder die Kälteseite (Schritt K, E37). Schritt K ist
    /// Schritt H im gespiegelten Raum; der Wert <see cref="Heizen"/> ist die Vorgabe.
    /// </summary>
    internal enum Uebergabeseite
    {
        /// <summary>Die Wärmeseite — Schritt H, die Vorgabe.</summary>
        Heizen = 0,

        /// <summary>Die Kälteseite — Schritt K, Schritt H im gespiegelten Raum (E37).</summary>
        Kuehlen,
    }

    /// <summary>
    /// Der Löser des 2-K-Modells nach VDI 6007 Blatt 1 für EINE Zone (Stufe G0 der
    /// Gebäudesimulation, Umsetzungskonzept 1.3; Rechenweg in den Rechenschritten,
    /// Kapitel 4, 5 und 7). „7R2C" ist nur die Kurzform; die Richtlinie sagt 2-K-Modell.
    ///
    /// <para><b>Das Netz.</b> Zwei Massenknoten (Außen- und Innenbauteile) sind der
    /// Zustand; die beiden Oberflächenknoten und der Luftknoten sind kapazitätslos und
    /// werden algebraisch eliminiert. Je Betriebsfall entsteht so ein lineares System
    /// <c>dx/dt = A·x + b</c> mit voll besetzter 2×2-Matrix A. Im freien Lauf und an einer
    /// Leistungsgrenze ist die Leistung fest und die Lufttemperatur unbekannt; in der
    /// geregelten Lage ist die Lufttemperatur fest (Sollwert bzw. obere Grenze) und die
    /// Leistung unbekannt. Beide Lagen löst dieselbe 3×3-Elimination über die Unbekannten
    /// (θ_s,AW, θ_s,IW, θ_air bzw. Φ); A hängt nicht von den Lasten ab und wird je
    /// Betriebslage einmal gebildet, b je Stunde.</para>
    ///
    /// <para><b>Die Stunde.</b> Φ, Γ und Ψ über die volle Stunde entstehen einmal je
    /// Betriebslage; <see cref="Schritt"/> wählt am Abschnittsbeginn den Betriebsfall,
    /// prüft seine Gültigkeit am Ende der Reststunde und sucht, wenn sie verletzt ist,
    /// den Umschaltzeitpunkt per Bisektion (höchstens <see cref="HALBIERUNGEN"/>
    /// Halbierungen, die einzige Iteration). Heiz- und Kühlanteil werden je Abschnitt
    /// getrennt aufsummiert; ein Abschnitt bucht nie beides. Ergebnis ist das exakte
    /// Blockmittel der Stunde.</para>
    ///
    /// <para><b>Vergleiche an Betriebsschwellen</b> tragen den Zahlenrand
    /// <see cref="Rechenrand.Zu"/>: Ein Fall gilt als verletzt, wenn sein Verletzungsmaß
    /// den Rand übersteigt.</para>
    ///
    /// <para><b>Zustand</b> sind die beiden Massentemperaturen — mehr nicht. Die
    /// Fallsysteme der geregelten Lagen werden zum Strahlungsanteil der Übergabe gebildet
    /// und zwischengespeichert; das ist ein Rechenpuffer, das Ergebnis hängt nicht an
    /// ihm. <see cref="Zuruecksetzen"/> ist Bedingung vor jedem Lauf.</para>
    ///
    /// <para><b>Schritt H — die Anlagenkopplung (Stufe AK1).</b> Trägt der Stundenrand eine
    /// Übergabe (<see cref="Stundenrand.MitUebergabe"/>), wählt <see cref="SchrittUebergabe"/> den Fall
    /// der Heizung: Liegt die verlangte Leistung innerhalb der Übergabe <b>und</b> ist das
    /// Proportionalband null, gilt WÖRTLICH der geregelte Fall des Bestands (Grenzfall 3.7);
    /// sonst hängt die Übergabe als Sekantenleitwert (G, θ_H) im freien Lauf, je
    /// Sättigungszustand mit ihrem Leitwert und ihrem zweiseitigen Verletzungsmaß (10.2 H5).
    /// Der Linearisierungspunkt ist die Raumluft, die am Abschnittsbeginn zur gelieferten
    /// Leistung passt — der Arbeitspunkt θ* = θ₀ + s·Φ(θ*) (benannte Festlegung: „die
    /// aktuelle Raumlufttemperatur" von 10.2 ist beim masselosen Luftknoten dieser
    /// Arbeitspunkt, nicht der Wert eines früheren Abschnitts). Ohne Übergabe bleibt jede
    /// Anweisung des Bestands, wie sie ist.</para>
    ///
    /// <para>Ohne Datenbank, ohne Protokoll, ohne Statik, einfädig, durchgehend
    /// <c>double</c>.</para>
    /// </summary>
    internal sealed class Zonenmodell2K
    {
        /// <summary>Länge einer Blockstunde [s].</summary>
        internal const double STUNDE_S = 3600.0;

        /// <summary>Höchstzahl der Abschnitte je Stunde (Rechenschritte 7.1).</summary>
        internal const int ABSCHNITTSDECKEL = 60;

        /// <summary>Halbierungen der Bisektion eines Umschaltzeitpunkts.</summary>
        internal const int HALBIERUNGEN = 60;

        private readonly ErsatzparameterRC _p;
        private readonly string _bezeichnung;
        private readonly int _deckel;

        private readonly double _g1;
        private readonly double _g2;
        private readonly double _gRest;
        private readonly double _gcAW;
        private readonly double _gcIW;
        private readonly double _gRad;
        private readonly double _gExt;
        private readonly double _c1;
        private readonly double _c2;
        private readonly double _wAW;
        private readonly double _wIW;

        private readonly Fallsystem _frei;

        // Rechenpuffer der geregelten Lagen, je Übergabeanteil, und des freien Laufs mit
        // stündlichem Zusatzleitwert (kein Zustand).
        private Fallsystem _freiZusatz;
        private Fallsystem _heizen;
        private Fallsystem _kuehlen;

        // Der Zustand: die beiden Massentemperaturen [°C].
        private double _thetaMAw;
        private double _thetaMIw;

        /// <summary>Baut die Systemmatrix des freien Laufs samt Φ, Γ, Ψ der vollen Stunde.</summary>
        internal Zonenmodell2K(ErsatzparameterRC p)
            : this(p, null, ABSCHNITTSDECKEL)
        {
        }

        /// <summary>Wie oben; <paramref name="bezeichnung"/> erscheint in jeder Fehlermeldung.</summary>
        internal Zonenmodell2K(ErsatzparameterRC p, string bezeichnung)
            : this(p, bezeichnung, ABSCHNITTSDECKEL)
        {
        }

        /// <summary>
        /// Wie oben, mit eigenem Abschnittsdeckel — nur für die Probe, die den Deckel
        /// erzwingt (Rechenschritte 10.4).
        /// </summary>
        internal Zonenmodell2K(ErsatzparameterRC p, string bezeichnung, int abschnittsdeckel)
        {
            _p = p ?? throw new ArgumentNullException(nameof(p));
            if (abschnittsdeckel < 1) throw new ArgumentOutOfRangeException(nameof(abschnittsdeckel));
            _bezeichnung = string.IsNullOrEmpty(bezeichnung) ? "Zone" : bezeichnung;
            _deckel = abschnittsdeckel;

            _g1 = 1.0 / p.R_1_AWGruppe_KW;
            _gRest = 1.0 / p.R_Rest_AWGruppe_KW;
            _g2 = 1.0 / p.R_1_IW_KW;
            _gcAW = 1.0 / p.R_conv_AW_KW;
            _gcIW = 1.0 / p.R_conv_IW_KW;
            _gRad = 1.0 / p.R_rad_KW;
            _gExt = double.IsPositiveInfinity(p.R_ext_KW) ? 0.0 : 1.0 / p.R_ext_KW;
            _c1 = p.C_AW_Jk;
            _c2 = p.C_IW_Jk;

            double aSumme = p.A_AW_gesamt_M2 + p.A_IW_M2;
            _wAW = p.A_AW_gesamt_M2 / aSumme;
            _wIW = p.A_IW_M2 / aSumme;

            _frei = new Fallsystem(this, geregelt: false, anteilAW: 0.0, anteilIW: 0.0, anteilLuft: 1.0, gExt: _gExt, schluessel: 0.0);
            Zuruecksetzen(20.0);
        }

        /// <summary>Der Parametersatz, mit dem gerechnet wird.</summary>
        internal ErsatzparameterRC Parameter => _p;

        /// <summary>
        /// Die beiden Eigenwerte der Systemmatrix des freien Laufs [1/s] — reell und
        /// negativ (Konzept 10.2); der langsamere zuerst. Die Zeitkonstanten sind −1/λ.
        /// </summary>
        internal double[] Eigenwerte => _frei.Rechner.Eigenwerte;

        /// <summary>Die Eigenwerte der Systemmatrix eines Betriebsfalls [1/s].</summary>
        /// <param name="fall">Der Betriebsfall.</param>
        /// <param name="anteil">Heizfälle: Strahlungsanteil; Kühlfälle: Anteil an der Innenfläche.</param>
        internal double[] EigenwerteImFall(Betriebsfall fall, double anteil = 0.0)
        {
            switch (fall)
            {
                case Betriebsfall.HeizenGeregelt:
                    return Heizsystem(anteil).Rechner.Eigenwerte;
                case Betriebsfall.KuehlenGeregelt:
                    return Kuehlsystem(anteil).Rechner.Eigenwerte;
                default:
                    return _frei.Rechner.Eigenwerte;
            }
        }

        /// <summary>Die Systemmatrix des freien Laufs [1/s] — für die Rechenproben.</summary>
        internal Matrix2 SystemmatrixFrei => _frei.Rechner.A;

        /// <summary>Temperatur des Außenbauteil-Massenknotens [°C] (Zustand).</summary>
        internal double ThetaMAw => _thetaMAw;

        /// <summary>Temperatur des Innenbauteil-Massenknotens [°C] (Zustand).</summary>
        internal double ThetaMIw => _thetaMIw;

        /// <summary>Setzt beide Massenknoten auf <paramref name="thetaStart"/> [°C].</summary>
        internal void Zuruecksetzen(double thetaStart)
        {
            Zuruecksetzen(thetaStart, thetaStart);
        }

        /// <summary>Setzt die Massenknoten einzeln [°C].</summary>
        internal void Zuruecksetzen(double thetaMAw, double thetaMIw)
        {
            if (double.IsNaN(thetaMAw) || double.IsInfinity(thetaMAw) || double.IsNaN(thetaMIw) || double.IsInfinity(thetaMIw))
                throw new GebaeudeModellException(GebaeudeModellFehler.RandUngueltig,
                    _bezeichnung + ": Der Startzustand muss endlich sein.");
            _thetaMAw = thetaMAw;
            _thetaMIw = thetaMIw;
        }

        /// <summary>
        /// Rechnet eine Blockstunde mit den Randbedingungen <paramref name="r"/> und
        /// schreibt den Zustand fort.
        /// </summary>
        /// <exception cref="GebaeudeModellException">bei ungültigem Rand oder erreichtem Abschnittsdeckel;
        /// der Zustand bleibt dann unverändert.</exception>
        internal Stundenergebnis Schritt(in Stundenrand r)
        {
            RandPruefen(in r);

            Vektor2 x = new Vektor2(_thetaMAw, _thetaMIw);
            double t = 0.0;
            double akkHeiz = 0.0, akkKuehl = 0.0, akkAir = 0.0;
            double akkS1 = 0.0, akkS2 = 0.0, akkM1 = 0.0, akkM2 = 0.0;
            int abschnitte = 0;
            Span<Betriebsfall> folge = stackalloc Betriebsfall[ABSCHNITTSDECKEL];

            // Anlagenkopplung: Zeit je Begrenzungsgrund [s], je Seite ein Feld (Wärmeseite nur mit
            // Übergabe, Kälteseite nur mit Kühlübergabe geführt). Ein Abschnitt der Kälteseite bucht
            // das Kältefeld, jeder andere wie gehabt das Heizfeld; so bleibt die Heizseite bitgleich.
            Span<double> tauJeGrund = stackalloc double[GRUENDE];
            tauJeGrund.Clear();
            Span<double> tauJeGrundKuehl = stackalloc double[GRUENDE];
            tauJeGrundKuehl.Clear();

            while (t < STUNDE_S)
            {
                if (abschnitte >= _deckel)
                    throw new GebaeudeModellException(GebaeudeModellFehler.AbschnittsdeckelErreicht,
                        _bezeichnung + ": Die Stunde ist nach " + abschnitte.ToString(CultureInfo.InvariantCulture) +
                        " Abschnitten nicht zur Ruhe gekommen (Fallfolge " + Folge(folge, abschnitte) +
                        "). Es entsteht kein Teilstundenergebnis.");

                Betriebsfall fall = FallWaehlen(x, in r, out Abschnitt ab);
                if (abschnitte < folge.Length) folge[abschnitte] = fall;
                abschnitte++;

                double rest = STUNDE_S - t;
                Uebergang u = rest == STUNDE_S ? ab.System.Stunde : ab.System.Rechner.Bei(rest);
                double tau = rest;

                if (Verletzt(fall, in ab, u.Ende(x, ab.B), in r))
                {
                    double lo = 0.0, hi = rest;
                    for (int i = 0; i < HALBIERUNGEN; i++)
                    {
                        double mitte = 0.5 * (lo + hi);
                        if (!(mitte > lo) || !(mitte < hi)) break;
                        Vektor2 xm = ab.System.Rechner.Bei(mitte).Ende(x, ab.B);
                        if (Verletzt(fall, in ab, xm, in r)) hi = mitte;
                        else lo = mitte;
                    }
                    tau = hi;
                    if (tau < rest) u = ab.System.Rechner.Bei(tau);
                }

                Vektor2 xMittel = u.Mittel(x, ab.B);
                double s1 = ab.Ausgang(0, xMittel);
                double s2 = ab.Ausgang(1, xMittel);
                double z2 = ab.Ausgang(2, xMittel);
                double air = ab.System.Geregelt ? ab.ThetaFest : z2;
                // Die Übergabe als Leitwert (Schritt H): Φ = G·(θ_H − θ_air) ist affin im Zustand,
                // ihr Abschnittsmittel folgt exakt aus dem Mittel der Raumluft.
                double q = ab.System.Geregelt ? z2 : ab.MitLeitwert ? ab.LeitwertWK * (ab.ThetaHC - z2) : ab.QFest;

                // Je Abschnitt nie beides (Festlegung F-K3, Kühlkonzept 3.3) - die SCHARFE
                // Zusicherung: Heizfälle buchen nur Heizen, Kühlfälle nur Kühlen, das Totband
                // nichts. Bucht ein Fall eine Leistung mit falschem Vorzeichen über den Zahlenrand
                // hinaus, ist das ein Fehler und kein Klemmwert - er fällt laut.
                switch (fall)
                {
                    case Betriebsfall.HeizenGeregelt:
                    case Betriebsfall.Heizgrenze:
                        if (-q > Rechenrand.Zu(0.0)) AbschnittsregelVerletzt(fall, q);
                        akkHeiz += Math.Max(q, 0.0) * tau;
                        break;
                    case Betriebsfall.KuehlenGeregelt:
                    case Betriebsfall.Kuehlgrenze:
                        if (q > Rechenrand.Zu(0.0)) AbschnittsregelVerletzt(fall, q);
                        akkKuehl += Math.Max(-q, 0.0) * tau;
                        break;
                    case Betriebsfall.UebergabeGesaettigt:
                    case Betriebsfall.UebergabeRegelbereich:
                        // Der Zahlenrand des Leitwerts: θ_air darf θ_H um den Rand der Gültigkeit
                        // überschreiten, die Leistung also um G mal diesen Rand unter null liegen.
                        if (-q > Rechenrand.Zu(0.0) + ab.LeitwertWK * Rechenrand.Zu(ab.ThetaHC))
                            AbschnittsregelVerletzt(fall, q);
                        akkHeiz += Math.Max(q, 0.0) * tau;
                        break;
                    case Betriebsfall.KuehluebergabeGesaettigt:
                    case Betriebsfall.KuehluebergabeRegelbereich:
                        // Spiegel des Übergabefalls (Schritt K): Die Leistung darf um G mal den Rand
                        // der Gültigkeit über null liegen.
                        if (q > Rechenrand.Zu(0.0) + ab.LeitwertWK * Rechenrand.Zu(ab.ThetaHC))
                            AbschnittsregelVerletzt(fall, q);
                        akkKuehl += Math.Max(-q, 0.0) * tau;
                        break;
                }
                if (ab.Gekoppelt && ab.K.Seite == Uebergabeseite.Kuehlen) tauJeGrundKuehl[(int)ab.Grund] += tau;
                else if (r.MitUebergabe) tauJeGrund[(int)ab.Grund] += tau;
                akkAir += air * tau;
                akkS1 += s1 * tau;
                akkS2 += s2 * tau;
                akkM1 += xMittel.A * tau;
                akkM2 += xMittel.B * tau;

                x = u.Ende(x, ab.B);
                t = tau == rest ? STUNDE_S : t + tau;
            }

            _thetaMAw = x.A;
            _thetaMIw = x.B;

            double airMittel = akkAir / STUNDE_S;
            double s1Mittel = akkS1 / STUNDE_S;
            double s2Mittel = akkS2 / STUNDE_S;
            double opMittel = 0.5 * airMittel + 0.5 * (_wAW * s1Mittel + _wIW * s2Mittel);
            if (!r.MitUebergabe)
                return new Stundenergebnis(
                    akkHeiz / STUNDE_S,
                    akkKuehl / STUNDE_S,
                    airMittel,
                    opMittel,
                    s1Mittel,
                    s2Mittel,
                    akkM1 / STUNDE_S,
                    akkM2 / STUNDE_S,
                    x.A,
                    x.B,
                    abschnitte);

            // Anlagenkopplung (10.2 H6, 10.4): Vorlauf der Stunde und Rücklauf zur GELIEFERTEN
            // mittleren Leistung; der Grund mit dem größten Zeitanteil.
            LetzteFallfolge = folge.Slice(0, Math.Min(abschnitte, folge.Length)).ToArray();
            double heizMittel = akkHeiz / STUNDE_S;
            double vorlauf = r.VorlaufC;
            double ruecklauf = double.IsNaN(vorlauf) ? double.NaN : Waermeuebergabe.RuecklaufC(r.Uebergabe, vorlauf, heizMittel);
            int grund = 0;
            for (int i = 1; i < GRUENDE; i++) if (tauJeGrund[i] > tauJeGrund[grund]) grund = i;
            return new Stundenergebnis(
                heizMittel,
                akkKuehl / STUNDE_S,
                airMittel,
                opMittel,
                s1Mittel,
                s2Mittel,
                akkM1 / STUNDE_S,
                akkM2 / STUNDE_S,
                x.A,
                x.B,
                abschnitte,
                vorlauf,
                ruecklauf,
                (Begrenzungsgrund)grund,
                tauJeGrund[(int)Begrenzungsgrund.Uebergabe] / STUNDE_S,
                tauJeGrund[(int)Begrenzungsgrund.HeizleistungMax] / STUNDE_S,
                tauJeGrund[(int)Begrenzungsgrund.Heizgrenze] / STUNDE_S);
        }

        /// <summary>Zahl der Begrenzungsgründe beider Seiten (Länge der Zeitsummen je Grund).</summary>
        private const int GRUENDE = 8;

        /// <summary>
        /// Die Folge der Betriebsfälle der zuletzt gerechneten Stunde MIT Übergabe — ein Befund
        /// für die Proben (zwei Knicke, 11.1), kein Zustand: Das Ergebnis hängt nicht an ihr.
        /// Stunden ohne Übergabe schreiben sie nicht.
        /// </summary>
        internal Betriebsfall[] LetzteFallfolge { get; private set; } = Array.Empty<Betriebsfall>();

        /// <summary>
        /// <b>Die stationäre Heizlast</b> [W] bei festen Randbedingungen — Raumluft auf
        /// <paramref name="thetaRaumC"/>, keine solaren und inneren Lasten, keine
        /// Leistungsgrenze: der eingeschwungene Zustand des geregelten Systems, A·x + b = 0.
        /// Grundlage der hergeleiteten Nennleistung der Übergabe (Anlagenkopplung 8.4, H7): ein
        /// Aufruf des vorhandenen Lösers mit fester Randbedingung, ausdrücklich kein
        /// Normnachweis (H-F12). Der Zustand des Modells bleibt unberührt.
        /// </summary>
        internal double StationaereHeizlastW(double thetaRaumC, double thetaOutC, double thetaEqC, double strahlungsanteil)
        {
            var r = new Stundenrand(thetaOutC, thetaEqC, thetaRaumC, double.PositiveInfinity, 0.0, 0.0, 0.0,
                                    heizungStrahlungsanteil: strahlungsanteil);
            Abschnitt h = Aufbauen(Betriebsfall.HeizenGeregelt, in r);
            Vektor2 xStationaer = -1.0 * (h.System.Rechner.A.Inverse() * h.B);
            return h.Ausgang(2, xStationaer);
        }

        // =============================================================================
        //  Betriebsfall wählen und prüfen
        // =============================================================================

        /// <summary>
        /// Wählt den Betriebsfall am Abschnittsbeginn (Rechenschritte 7.1): erst die Heizung
        /// im geregelten System auf den Sollwert, dann die Kühlung auf die obere Grenze,
        /// sonst das Totband.
        /// </summary>
        private Betriebsfall FallWaehlen(Vektor2 x, in Stundenrand r, out Abschnitt ab)
        {
            if (r.MitHeizung)
            {
                Abschnitt h = Aufbauen(Betriebsfall.HeizenGeregelt, in r);
                double q0 = h.Ausgang(2, x);
                if (q0 > 0.0)
                {
                    if (r.MitUebergabe) return SchrittUebergabe(x, in r, in h, q0, Uebergabeseite.Heizen, out ab);
                    if (Begrenzt(r.HeizleistungMaxW) && q0 > r.HeizleistungMaxW)
                    {
                        ab = Aufbauen(Betriebsfall.Heizgrenze, in r);
                        return Betriebsfall.Heizgrenze;
                    }
                    ab = h;
                    return Betriebsfall.HeizenGeregelt;
                }
            }
            if (r.MitKuehlung)
            {
                Abschnitt k = Aufbauen(Betriebsfall.KuehlenGeregelt, in r);
                double qc0 = -k.Ausgang(2, x);
                if (qc0 > 0.0)
                {
                    if (Begrenzt(r.KuehlleistungMaxW) && qc0 > r.KuehlleistungMaxW)
                    {
                        ab = Aufbauen(Betriebsfall.Kuehlgrenze, in r);
                        return Betriebsfall.Kuehlgrenze;
                    }
                    ab = k;
                    return Betriebsfall.KuehlenGeregelt;
                }
            }
            ab = Aufbauen(Betriebsfall.Totband, in r);
            return Betriebsfall.Totband;
        }

        /// <summary>
        /// <b>Schritt H</b> (Anlagenkopplung 10.2) — der Fall der Heizung, wenn die Stunde eine
        /// Übergabe trägt und das Gebäude Wärme verlangt (q₀ &gt; 0 im geregelten System).
        /// <list type="number">
        /// <item><b>H1 Heizgrenze:</b> Ist der Vorlauf undefiniert (Heizkurve aus) oder nicht über
        /// der Raumluft des freien Laufs θ₀, liefert die Übergabe nichts: freier Lauf ohne
        /// Heizung, gültig, bis die Raumluft unter den Vorlauf fällt.</item>
        /// <item><b>Grenzfall (3.7):</b> Xp = 0 und q₀ ≤ Φ_ue,max(θ_soll) — der geregelte Fall bzw.
        /// die Leistungsgrenze des Bestands, WÖRTLICH mit dessen Leistungsgleichung.</item>
        /// <item><b>H4 Heizleistung_Max:</b> Kappt die Grenze, was die Kennlinie des Reglers am
        /// Punkt θ_HL = θ₀ + s·HL hergäbe, gilt der Fall „Leistung fest" (Heizgrenze des
        /// Bestands), gültig bis zur Kappungstemperatur.</item>
        /// <item><b>H5 gesättigt:</b> Der Arbeitspunkt mit voll offenem Ventil liegt unter
        /// θ_soll − Xp: Leitwert G = G_H.</item>
        /// <item><b>H5 Regelbereich:</b> sonst — Leitwert G = Φ_ue,max/Xp + y·G_H.</item>
        /// </list>
        ///
        /// <para><b>Schritt K — die Kälteseite (10.5, E37)</b> ist dieselbe Routine im
        /// <b>gespiegelten Raum</b>: Mit <paramref name="seite"/> = <see cref="Uebergabeseite.Kuehlen"/>
        /// werden Raumluft, freier Lauf θ₀, Sollwert (θ_kühl) und Vorlauf negiert (<see cref="Sp"/>:
        /// x ↦ −x, <b>ohne Multiplikation</b>, damit die Wärmeseite Zeichen für Zeichen bleibt), die
        /// Kennwerte der Kühlübergabe kommen gespiegelt (−V, −R, −θ_i,N) aus dem Rand, die
        /// Empfindlichkeit s bleibt. Alle Vergleiche und Aufrufe von <see cref="Waermeuebergabe"/>
        /// laufen im gespiegelten Raum; zurück gilt θ_K = −θ′_H und [unten, oben] = [−oben′, −unten′].
        /// q₀ ist dann die verlangte Kühlleistung q_c,0 &gt; 0.</para>
        /// </summary>
        private Betriebsfall SchrittUebergabe(Vektor2 x, in Stundenrand r, in Abschnitt geregelt, double q0,
                                              Uebergabeseite seite, out Abschnitt ab)
        {
            bool heizen = seite == Uebergabeseite.Heizen;
            Seitenrand sr = heizen ? Seitenrand.Heizseite(in r) : Seitenrand.Kaelteseite(in r);
            Uebergabekennwerte k = sr.Kennwerte;
            double soll = Sp(heizen, sr.SollC);
            double xp = r.ReglerbandK;
            double vorlauf = Sp(heizen, sr.VorlaufC);
            double a = sr.Strahlungsanteil;
            double eAW = a * _wAW, eIW = a * _wIW, eLuft = 1.0 - a;

            // Die Gründe und Fälle der Seite (Spiegel: Heizgrenze ↔ KeineKaelte usw.).
            Begrenzungsgrund grundNichts = heizen ? Begrenzungsgrund.Heizgrenze : Begrenzungsgrund.KeineKaelte;
            Begrenzungsgrund grundLeistungMax = heizen ? Begrenzungsgrund.HeizleistungMax : Begrenzungsgrund.KuehlleistungMax;
            Betriebsfall fallGrenze = heizen ? Betriebsfall.Heizgrenze : Betriebsfall.Kuehlgrenze;

            // Der freie Lauf ohne Heizung am Abschnittsbeginn und die Antwort der Raumluft auf
            // die Heizleistung: θ_air = θ₀ + s·Φ (affin im freien System; gespiegelt ebenso).
            Abschnitt frei = Aufbauen(Betriebsfall.Totband, in r);
            double theta0 = Sp(heizen, frei.Ausgang(2, x));
            double s = frei.System.Empfindlichkeit(eAW, eIW, eLuft);

            // H1 / K1 — die Übergabe liefert nichts.
            if (double.IsNaN(vorlauf) || !(vorlauf > theta0))
            {
                ab = frei.MitKopplung(KopplungFrei(seite, grundNichts, double.IsNaN(vorlauf) ? double.NegativeInfinity : vorlauf, soll));
                return Betriebsfall.Totband;
            }

            bool hlBegrenzt = Begrenzt(sr.LeistungMaxW);
            double hl = sr.LeistungMaxW;

            // Grenzfall 3.7: Xp = 0 und die Übergabe reicht am Sollwert — der Bestand, wörtlich.
            if (xp == 0.0)
            {
                double phiSoll = Waermeuebergabe.LeistungOffenW(k, vorlauf, soll);
                if (q0 <= phiSoll)
                {
                    if (hlBegrenzt && q0 > hl)
                    {
                        ab = Aufbauen(fallGrenze, in r).MitKopplung(
                            KopplungFrei(seite, grundLeistungMax, double.NegativeInfinity,
                                         Waermeuebergabe.Kappungstemperatur(k, vorlauf, soll, xp, hl)));
                        return fallGrenze;
                    }
                    ab = geregelt.MitKopplung(Kopplung.Geregelt(phiSoll, seite));
                    return heizen ? Betriebsfall.HeizenGeregelt : Betriebsfall.KuehlenGeregelt;
                }
            }

            // H4 — kappt die Leistungsgrenze, was die Kennlinie des Reglers hergäbe?
            if (hlBegrenzt)
            {
                double thetaHl = theta0 + s * hl;
                if (thetaHl < soll && Waermeuebergabe.LeistungKennlinieW(k, vorlauf, soll, xp, thetaHl) >= hl)
                {
                    ab = Aufbauen(fallGrenze, in r).MitKopplung(
                        KopplungFrei(seite, grundLeistungMax, double.NegativeInfinity,
                                     Waermeuebergabe.Kappungstemperatur(k, vorlauf, soll, xp, hl)));
                    return fallGrenze;
                }
            }

            // H5 — gesättigt oder Regelbereich.
            double knick = soll - xp;
            double phiSatt = Waermeuebergabe.LeistungGesaettigtW(k, vorlauf, theta0, s);
            double thetaSatt = theta0 + s * phiSatt;
            double thetaStern, phiStern, leitwert;
            Betriebsfall fall;
            Begrenzungsgrund grund;
            if (xp == 0.0 || thetaSatt < knick - Rechenrand.Zu(knick))
            {
                thetaStern = thetaSatt;
                phiStern = phiSatt;
                leitwert = Waermeuebergabe.SteigungOffenWK(k, phiSatt, vorlauf, thetaSatt);
                fall = heizen ? Betriebsfall.UebergabeGesaettigt : Betriebsfall.KuehluebergabeGesaettigt;
                // Die Vorlaufgrenze der Kälteseite (7.2) ist nur dann der Grund, wenn die Übergabe
                // gesättigt ist UND der Vorlauf an der Grenze steht.
                grund = heizen ? Begrenzungsgrund.Uebergabe
                        : sr.VorlaufGekappt ? Begrenzungsgrund.VorlaufgrenzeKuehlung : Begrenzungsgrund.KuehlUebergabe;
            }
            else
            {
                Waermeuebergabe.ArbeitspunktRegelbereich(k, vorlauf, soll, xp, theta0, s,
                                                         out thetaStern, out phiStern, out leitwert);
                fall = heizen ? Betriebsfall.UebergabeRegelbereich : Betriebsfall.KuehluebergabeRegelbereich;
                grund = Begrenzungsgrund.KeineBegrenzung;
            }

            // Ohne Leistung (verschwindend kleine Übertemperatur) gibt es keinen Leitwert: dann
            // ist das die Heizgrenze, bis die Raumluft fällt.
            if (!(phiStern > 0.0) || !(leitwert > 0.0))
            {
                ab = frei.MitKopplung(KopplungFrei(seite, grundNichts, thetaStern, soll));
                return Betriebsfall.Totband;
            }

            double thetaH = thetaStern + phiStern / leitwert;
            // Obere Gültigkeit: der Knick (gesättigt) bzw. θ_H, wo die lineare Leistung null wird;
            // untere: der Knick (Regelbereich) und die Heizleistungsgrenze der linearen Leistung.
            bool gesaettigt = fall == Betriebsfall.UebergabeGesaettigt || fall == Betriebsfall.KuehluebergabeGesaettigt;
            double oben = gesaettigt ? Math.Min(knick, thetaH) : Math.Min(soll, thetaH);
            double unten = !gesaettigt ? knick : double.NegativeInfinity;
            if (hlBegrenzt) unten = Math.Max(unten, thetaH - hl / leitwert);

            // Zurück in den Raum der Raumluft: θ_K = −θ′_H, [unten, oben] = [−oben′, −unten′].
            double thetaHC = Sp(heizen, thetaH);
            double untenC = heizen ? unten : -oben;
            double obenC = heizen ? oben : -unten;
            ab = FreiMitLeitwert(leitwert, thetaHC, eAW, eIW, eLuft, in r,
                                 Kopplung.Leitwert(grund, leitwert, thetaHC, untenC, obenC, seite));
            return fall;
        }

        /// <summary>
        /// Der Spiegel der Kälteseite (Schritt K): die Wärmeseite unverändert, die Kälteseite
        /// negiert — <b>ohne Multiplikation</b>, damit die Heizseite Zeichen für Zeichen bleibt.
        /// </summary>
        private static double Sp(bool heizen, double x) => heizen ? x : -x;

        /// <summary>
        /// Die Kopplungsangabe einer freien Lage fester Leistung aus Grenzen im Raum der Seite: auf
        /// der Wärmeseite unverändert, auf der Kälteseite zurückgespiegelt ([−oben′, −unten′]).
        /// </summary>
        private static Kopplung KopplungFrei(Uebergabeseite seite, Begrenzungsgrund grund, double untenSeite, double obenSeite)
            => seite == Uebergabeseite.Heizen
                ? Kopplung.Frei(grund, untenSeite, obenSeite, seite)
                : Kopplung.Frei(grund, -obenSeite, -untenSeite, seite);

        /// <summary>
        /// Ist der Betriebsfall im Zustand <paramref name="x"/> verletzt — über den
        /// Zahlenrand hinaus? (Verletzungsmaße der Rechenschritte 7.1.)
        /// </summary>
        private static bool Verletzt(Betriebsfall fall, in Abschnitt ab, Vektor2 x, in Stundenrand r)
        {
            if (ab.Gekoppelt) return VerletztGekoppelt(fall, in ab, x, in r);
            double z2 = ab.Ausgang(2, x);
            switch (fall)
            {
                case Betriebsfall.HeizenGeregelt:
                    if (-z2 > Rechenrand.Zu(0.0)) return true;
                    return Begrenzt(r.HeizleistungMaxW) && z2 - r.HeizleistungMaxW > Rechenrand.Zu(r.HeizleistungMaxW);
                case Betriebsfall.Heizgrenze:
                    return z2 - r.ThetaSoll > Rechenrand.Zu(r.ThetaSoll);
                case Betriebsfall.KuehlenGeregelt:
                    if (z2 > Rechenrand.Zu(0.0)) return true;
                    return Begrenzt(r.KuehlleistungMaxW) && -z2 - r.KuehlleistungMaxW > Rechenrand.Zu(r.KuehlleistungMaxW);
                case Betriebsfall.Kuehlgrenze:
                    return r.ThetaMax - z2 > Rechenrand.Zu(r.ThetaMax);
                default:
                    if (r.MitHeizung && r.ThetaSoll - z2 > Rechenrand.Zu(r.ThetaSoll)) return true;
                    return r.MitKuehlung && z2 - r.ThetaMax > Rechenrand.Zu(r.ThetaMax);
            }
        }

        /// <summary>
        /// Die Verletzungsmaße der Fälle mit Übergabe (Anlagenkopplung 10.2, Rechenschritte 7.4):
        /// der geregelte Fall zusätzlich gegen die Übergabe am Sollwert, die freien Lagen
        /// zweiseitig gegen ihre untere und obere Grenze der Raumluft. Bei unbegrenzter Übergabe
        /// und Xp = 0 fallen die Maße auf die des Bestands (3.7).
        /// </summary>
        private static bool VerletztGekoppelt(Betriebsfall fall, in Abschnitt ab, Vektor2 x, in Stundenrand r)
        {
            double z2 = ab.Ausgang(2, x);
            Kopplung k = ab.K;
            if (fall == Betriebsfall.HeizenGeregelt)
            {
                if (-z2 > Rechenrand.Zu(0.0)) return true;
                if (Begrenzt(r.HeizleistungMaxW) && z2 - r.HeizleistungMaxW > Rechenrand.Zu(r.HeizleistungMaxW)) return true;
                return Begrenzt(k.GrenzeQW) && z2 - k.GrenzeQW > Rechenrand.Zu(k.GrenzeQW);
            }
            if (k.ThetaUntenC - z2 > Rechenrand.Zu(k.ThetaUntenC)) return true;
            if (z2 - k.ThetaObenC > Rechenrand.Zu(k.ThetaObenC)) return true;
            return r.MitKuehlung && z2 - r.ThetaMax > Rechenrand.Zu(r.ThetaMax);
        }

        private static bool Begrenzt(double grenze) => !double.IsNaN(grenze) && !double.IsPositiveInfinity(grenze);

        /// <summary>Bildet System, rechte Seite und b eines Abschnitts im gegebenen Fall.</summary>
        private Abschnitt Aufbauen(Betriebsfall fall, in Stundenrand r)
        {
            switch (fall)
            {
                case Betriebsfall.HeizenGeregelt:
                    return Geregelt(Heizsystem(r.HeizungStrahlungsanteil), r.ThetaSoll, in r);
                case Betriebsfall.KuehlenGeregelt:
                    return Geregelt(Kuehlsystem(r.KuehlungAnteilInnenflaeche), r.ThetaMax, in r);
                case Betriebsfall.Heizgrenze:
                {
                    double a = r.HeizungStrahlungsanteil;
                    return Frei(r.HeizleistungMaxW, a * _wAW, a * _wIW, 1.0 - a, in r);
                }
                case Betriebsfall.Kuehlgrenze:
                {
                    double k = r.KuehlungAnteilInnenflaeche;
                    return Frei(-r.KuehlleistungMaxW, 0.0, k, 1.0 - k, in r);
                }
                default:
                    return Frei(0.0, 0.0, 0.0, 1.0, in r);
            }
        }

        /// <summary>Geregelte Lage: Lufttemperatur fest, Leistung unbekannt.</summary>
        private Abschnitt Geregelt(Fallsystem s, double theta, in Stundenrand r)
        {
            double r0 = r.PhiRadAW + _gcAW * theta;
            double r1 = r.PhiRadIW + _gcIW * theta;
            double gExt = _gExt + r.ZusatzleitwertWK;
            double r2 = gExt * r.ThetaOut + r.PhiConv - (_gcAW + _gcIW + gExt) * theta;
            return new Abschnitt(this, s, r0, r1, r2, r.ThetaEq, qFest: double.NaN, thetaFest: theta);
        }

        /// <summary>Freie Lage: Leistung fest (0 oder Grenze), Lufttemperatur unbekannt.</summary>
        private Abschnitt Frei(double q, double eAW, double eIW, double eLuft, in Stundenrand r)
        {
            double r0 = r.PhiRadAW + eAW * q;
            double r1 = r.PhiRadIW + eIW * q;
            double r2 = (_gExt + r.ZusatzleitwertWK) * r.ThetaOut + r.PhiConv + eLuft * q;
            return new Abschnitt(this, Freisystem(r.ZusatzleitwertWK), r0, r1, r2, r.ThetaEq, qFest: q, thetaFest: double.NaN);
        }

        /// <summary>
        /// Freie Lage mit der Übergabe als Leitwert (Schritt H5): Φ = G·(θ_H − θ_air), zu den
        /// Anteilen des Strahlungsanteils auf die Oberflächen und die Luft verteilt. G und θ_H
        /// ändern sich je Abschnitt; das System wird deshalb je Abschnitt gebildet — dieselbe
        /// Last wie ein Lüftungszustandswechsel (Anlagenkopplung 3.3).
        /// </summary>
        private Abschnitt FreiMitLeitwert(double g, double thetaH, double eAW, double eIW, double eLuft,
                                          in Stundenrand r, in Kopplung kopplung)
        {
            double gExt = _gExt + r.ZusatzleitwertWK;
            Fallsystem s;
            try
            {
                s = new Fallsystem(this, geregelt: false, anteilAW: eAW, anteilIW: eIW, anteilLuft: eLuft,
                                   gExt: gExt, schluessel: double.NaN, leitwertH: g);
            }
            catch (GebaeudeModellException ex) when (ex.Grund == GebaeudeModellFehler.EigenwerteNichtNegativ)
            {
                throw new GebaeudeModellException(ex.Grund,
                    _bezeichnung + ": Die Übergabe als Leitwert G = " + g.ToString("G6", CultureInfo.InvariantCulture) +
                    " W/K bei einem Strahlungsanteil von " + (1.0 - eLuft).ToString("G6", CultureInfo.InvariantCulture) +
                    " ergibt kein zulässiges Fallsystem. " + ex.Message);
            }
            double r0 = r.PhiRadAW + eAW * g * thetaH;
            double r1 = r.PhiRadIW + eIW * g * thetaH;
            double r2 = gExt * r.ThetaOut + r.PhiConv + eLuft * g * thetaH;
            return new Abschnitt(this, s, r0, r1, r2, r.ThetaEq, qFest: double.NaN, thetaFest: double.NaN).MitKopplung(in kopplung);
        }

        /// <summary>
        /// Das System des freien Laufs. Der Zusatzleitwert steckt in der Luftbilanz und damit
        /// in A — ohne ihn gilt das im Erbauer gebildete System.
        /// </summary>
        private Fallsystem Freisystem(double zusatzleitwert)
        {
            if (zusatzleitwert == 0.0) return _frei;
            if (_freiZusatz == null || _freiZusatz.Schluessel != zusatzleitwert)
                _freiZusatz = new Fallsystem(this, geregelt: false, anteilAW: 0.0, anteilIW: 0.0, anteilLuft: 1.0,
                                             gExt: _gExt + zusatzleitwert, schluessel: zusatzleitwert);
            return _freiZusatz;
        }

        private Fallsystem Heizsystem(double strahlungsanteil)
        {
            if (_heizen == null || _heizen.Schluessel != strahlungsanteil)
                _heizen = new Fallsystem(this, geregelt: true,
                                         anteilAW: strahlungsanteil * _wAW,
                                         anteilIW: strahlungsanteil * _wIW,
                                         anteilLuft: 1.0 - strahlungsanteil,
                                         gExt: _gExt,
                                         schluessel: strahlungsanteil);
            return _heizen;
        }

        private Fallsystem Kuehlsystem(double anteilInnenflaeche)
        {
            if (_kuehlen == null || _kuehlen.Schluessel != anteilInnenflaeche)
                _kuehlen = new Fallsystem(this, geregelt: true,
                                          anteilAW: 0.0,
                                          anteilIW: anteilInnenflaeche,
                                          anteilLuft: 1.0 - anteilInnenflaeche,
                                          gExt: _gExt,
                                          schluessel: anteilInnenflaeche);
            return _kuehlen;
        }

        /// <summary>
        /// Die Abschnittsregel ist verletzt (F-K3): Ein Heizfall hat Kälte bzw. ein Kühlfall
        /// Wärme gebucht. Kein Teilstundenergebnis, der Zustand bleibt unverändert.
        /// </summary>
        private void AbschnittsregelVerletzt(Betriebsfall fall, double q)
        {
            throw new GebaeudeModellException(GebaeudeModellFehler.AbschnittsregelVerletzt,
                _bezeichnung + ": Ein Abschnitt im Betriebsfall " + fall + " bucht die Leistung " +
                q.ToString("G6", CultureInfo.InvariantCulture) + " W mit falschem Vorzeichen " +
                "(Abschnittsregel: je Abschnitt nie Heizen und Kühlen zugleich).");
        }

        private void RandPruefen(in Stundenrand r)
        {
            string fehler = null;
            if (!Endlich(r.ThetaOut)) fehler = "ThetaOut";
            else if (!Endlich(r.ThetaEq)) fehler = "ThetaEq";
            else if (!Endlich(r.PhiRadAW)) fehler = "PhiRadAW";
            else if (!Endlich(r.PhiRadIW)) fehler = "PhiRadIW";
            else if (!Endlich(r.PhiConv)) fehler = "PhiConv";
            else if (double.IsInfinity(r.ThetaSoll)) fehler = "ThetaSoll";
            else if (double.IsNegativeInfinity(r.ThetaMax)) fehler = "ThetaMax";
            else if (r.MitHeizung && r.MitKuehlung && r.ThetaMax < r.ThetaSoll) fehler = "ThetaMax < ThetaSoll";
            else if (!GrenzeGueltig(r.HeizleistungMaxW)) fehler = "HeizleistungMaxW";
            else if (!GrenzeGueltig(r.KuehlleistungMaxW)) fehler = "KuehlleistungMaxW";
            else if (!AnteilGueltig(r.HeizungStrahlungsanteil)) fehler = "HeizungStrahlungsanteil";
            else if (!AnteilGueltig(r.KuehlungAnteilInnenflaeche)) fehler = "KuehlungAnteilInnenflaeche";
            else if (!Endlich(r.ZusatzleitwertWK) || r.ZusatzleitwertWK < 0.0) fehler = "ZusatzleitwertWK";
            else if (r.MitUebergabe)
            {
                Uebergabekennwerte k = r.Uebergabe;
                if (double.IsInfinity(r.VorlaufC)) fehler = "VorlaufC";
                else if (!Endlich(r.ReglerbandK) || r.ReglerbandK < 0.0) fehler = "ReglerbandK";
                else if (!(k.PhiNW > 0.0) || !Endlich(k.Exponent) || k.Exponent < 1.0
                         || !(k.DeltaThetaMNK > 0.0) || !Endlich(k.DeltaThetaMNK) || !(k.SpreizungNK > 0.0))
                    fehler = "Uebergabe";
            }

            if (fehler != null)
                throw new GebaeudeModellException(GebaeudeModellFehler.RandUngueltig,
                    _bezeichnung + ": Die Randbedingung " + fehler + " der Stunde ist ungültig.");
        }

        private static bool Endlich(double w) => !double.IsNaN(w) && !double.IsInfinity(w);

        private static bool GrenzeGueltig(double w) => double.IsNaN(w) || double.IsPositiveInfinity(w) || w >= 0.0;

        private static bool AnteilGueltig(double w) => w >= 0.0 && w <= 1.0;

        private static string Folge(Span<Betriebsfall> folge, int anzahl)
        {
            var sb = new StringBuilder();
            int n = Math.Min(anzahl, folge.Length);
            for (int i = 0; i < n; i++)
            {
                if (i > 0) sb.Append(" → ");
                sb.Append(folge[i].ToString());
            }
            return sb.ToString();
        }

        // =============================================================================
        //  Die Elimination der drei algebraischen Knoten
        // =============================================================================

        /// <summary>
        /// Das lineare System EINER Betriebslage. Unbekannte z = (θ_s,AW, θ_s,IW, z₃) mit
        /// z₃ = θ_air im freien Lauf und z₃ = Φ (Leistung in den Raum) in der geregelten
        /// Lage. Aus den drei Knotenbilanzen <c>L·z = −P·x − r′</c> folgt
        /// <c>z = Z·x + c</c> mit <c>Z = −L⁻¹·P</c> und <c>c = −L⁻¹·r′</c>; eingesetzt in
        /// die beiden Massenbilanzen ergibt sich A.
        /// </summary>
        private sealed class Fallsystem
        {
            /// <param name="leitwertH">Nur freie Lage mit Übergabe (Schritt H5): der Leitwert G [W/K]
            /// der Übergabe gegen θ_H, verteilt zu den Anteilen auf Oberflächen und Luft; 0 = ohne.</param>
            internal Fallsystem(Zonenmodell2K m, bool geregelt, double anteilAW, double anteilIW,
                                double anteilLuft, double gExt, double schluessel, double leitwertH = 0.0)
            {
                Geregelt = geregelt;
                Schluessel = schluessel;

                double l00 = -(m._g1 + m._gRad + m._gcAW), l01 = m._gRad, l02;
                double l10 = m._gRad, l11 = -(m._g2 + m._gRad + m._gcIW), l12;
                double l20 = m._gcAW, l21 = m._gcIW, l22;
                if (geregelt)
                {
                    l02 = anteilAW;
                    l12 = anteilIW;
                    l22 = anteilLuft;
                }
                else if (leitwertH == 0.0)
                {
                    l02 = m._gcAW;
                    l12 = m._gcIW;
                    l22 = -(m._gcAW + m._gcIW + gExt);
                }
                else
                {
                    // Die Übergabe G·(θ_H − θ_air) hängt an der Raumluft: ihr Anteil an jeder
                    // Knotenbilanz tritt in die Spalte der Luft, der Anteil mit θ_H in die rechte Seite.
                    l02 = m._gcAW - anteilAW * leitwertH;
                    l12 = m._gcIW - anteilIW * leitwertH;
                    l22 = -(m._gcAW + m._gcIW + gExt + anteilLuft * leitwertH);
                }

                // Inverse über die Adjunkte.
                double k00 = l11 * l22 - l12 * l21;
                double k01 = l12 * l20 - l10 * l22;
                double k02 = l10 * l21 - l11 * l20;
                double det = l00 * k00 + l01 * k01 + l02 * k02;
                double skala = Math.Abs(l00 * l11 * l22) + Math.Abs(l01 * l12 * l20) + Math.Abs(l02 * l10 * l21)
                             + Math.Abs(l02 * l11 * l20) + Math.Abs(l01 * l10 * l22) + Math.Abs(l00 * l12 * l21);
                if (!(Math.Abs(det) > 1e-12 * skala))
                    throw new GebaeudeModellException(GebaeudeModellFehler.SystemSingulaer,
                        m._bezeichnung + ": Das Knotensystem der " + (geregelt ? "geregelten" : "freien") +
                        " Lage ist singulär.");
                double f = 1.0 / det;
                _i00 = k00 * f;
                _i01 = (l02 * l21 - l01 * l22) * f;
                _i02 = (l01 * l12 - l02 * l11) * f;
                _i10 = k01 * f;
                _i11 = (l00 * l22 - l02 * l20) * f;
                _i12 = (l02 * l10 - l00 * l12) * f;
                _i20 = k02 * f;
                _i21 = (l01 * l20 - l00 * l21) * f;
                _i22 = (l00 * l11 - l01 * l10) * f;

                // Z = −L⁻¹·P, P = [[G1, 0], [0, G2], [0, 0]].
                Z00 = -_i00 * m._g1; Z01 = -_i01 * m._g2;
                Z10 = -_i10 * m._g1; Z11 = -_i11 * m._g2;
                Z20 = -_i20 * m._g1; Z21 = -_i21 * m._g2;

                // C·dx/dt = [−(G_Rest + G1)·x1 + G1·θ_s,AW + G_Rest·θ_eq ; −G2·x2 + G2·θ_s,IW]
                var a = new Matrix2(
                    (-(m._gRest + m._g1) + m._g1 * Z00) / m._c1, (m._g1 * Z01) / m._c1,
                    (m._g2 * Z10) / m._c2, (-m._g2 + m._g2 * Z11) / m._c2);
                Rechner = new Uebergangsrechner(a);
                Stunde = Rechner.Bei(STUNDE_S);
            }

            private readonly double _i00, _i01, _i02, _i10, _i11, _i12, _i20, _i21, _i22;

            internal bool Geregelt { get; }
            internal double Schluessel { get; }
            internal double Z00 { get; }
            internal double Z01 { get; }
            internal double Z10 { get; }
            internal double Z11 { get; }
            internal double Z20 { get; }
            internal double Z21 { get; }
            internal Uebergangsrechner Rechner { get; }
            internal Uebergang Stunde { get; }

            /// <summary>c = −L⁻¹·r′.</summary>
            internal void Konstante(double r0, double r1, double r2, out double c0, out double c1, out double c2)
            {
                c0 = -(_i00 * r0 + _i01 * r1 + _i02 * r2);
                c1 = -(_i10 * r0 + _i11 * r1 + _i12 * r2);
                c2 = -(_i20 * r0 + _i21 * r1 + _i22 * r2);
            }

            /// <summary>
            /// ∂z₃/∂q: die Antwort der dritten Unbekannten auf eine Last q, die zu den Anteilen
            /// (<paramref name="eAW"/>, <paramref name="eIW"/>, <paramref name="eLuft"/>) auf die
            /// drei Knoten geht — in der freien Lage die Antwort der Raumluft auf die Heizleistung
            /// [K/W] (Schritt H). Unabhängig vom Zustand, weil das System affin ist.
            /// </summary>
            internal double Empfindlichkeit(double eAW, double eIW, double eLuft)
                => -(_i20 * eAW + _i21 * eIW + _i22 * eLuft);
        }

        /// <summary>
        /// Was ein Abschnitt mit Übergabe zusätzlich trägt (Schritt H): den Begrenzungsgrund, den
        /// Leitwert samt Ersatztemperatur im Fall „Übergabe begrenzt", die Grenzen der Raumluft
        /// seiner freien Lage und im geregelten Fall die Übergabe am Sollwert. <c>default</c> =
        /// ohne Kopplung, der Bestand (Seite „Heizen", nicht gekoppelt). Die Angaben stehen im
        /// Raum der Raumluft — auf der Kälteseite also schon zurückgespiegelt (Schritt K).
        /// </summary>
        private readonly struct Kopplung
        {
            private Kopplung(Begrenzungsgrund grund, double leitwertWK, double thetaHC,
                             double thetaUntenC, double thetaObenC, double grenzeQW, Uebergabeseite seite)
            {
                Gekoppelt = true;
                Grund = grund;
                LeitwertWK = leitwertWK;
                ThetaHC = thetaHC;
                ThetaUntenC = thetaUntenC;
                ThetaObenC = thetaObenC;
                GrenzeQW = grenzeQW;
                Seite = seite;
            }

            /// <summary>Gehört der Abschnitt zu einer Stunde mit Übergabe?</summary>
            internal bool Gekoppelt { get; }

            /// <summary>Welche Seite der Abschnitt rechnet (Schritt H oder K); <c>default</c> = Heizen.</summary>
            internal Uebergabeseite Seite { get; }

            /// <summary>Welche Grenze die Leistung in diesem Abschnitt gekappt hat.</summary>
            internal Begrenzungsgrund Grund { get; }

            /// <summary>Leitwert G der Übergabe [W/K]; &gt; 0 nur im Fall „Übergabe begrenzt".</summary>
            internal double LeitwertWK { get; }

            /// <summary>Ersatztemperatur θ_H [°C] des Leitwerts.</summary>
            internal double ThetaHC { get; }

            /// <summary>Untere Grenze der Raumluft der freien Lage [°C]; −∞ = keine.</summary>
            internal double ThetaUntenC { get; }

            /// <summary>Obere Grenze der Raumluft der freien Lage [°C]; +∞ = keine.</summary>
            internal double ThetaObenC { get; }

            /// <summary>
            /// Geregelter Fall: die Übergabe am Sollwert Φ_ue,max(θ_soll) [W] — auf der Kälteseite die
            /// Kühlübergabe am Kühlsollwert als positiver Betrag; +∞ = unbegrenzt.
            /// </summary>
            internal double GrenzeQW { get; }

            /// <summary>Geregelter Fall mit Übergabe (Grenzfall 3.7): die Leistung darf bis <paramref name="grenzeQW"/> steigen.</summary>
            internal static Kopplung Geregelt(double grenzeQW, Uebergabeseite seite = Uebergabeseite.Heizen)
                => new Kopplung(Begrenzungsgrund.KeineBegrenzung, 0.0, double.NaN,
                                double.NegativeInfinity, double.PositiveInfinity, grenzeQW, seite);

            /// <summary>Freie Lage mit fester Leistung (0 oder Leistungsgrenze), gültig für die Raumluft in [unten, oben].</summary>
            internal static Kopplung Frei(Begrenzungsgrund grund, double untenC, double obenC,
                                          Uebergabeseite seite = Uebergabeseite.Heizen)
                => new Kopplung(grund, 0.0, double.NaN, untenC, obenC, double.PositiveInfinity, seite);

            /// <summary>Freie Lage mit der Übergabe als Leitwert (H5), gültig für die Raumluft in [unten, oben].</summary>
            internal static Kopplung Leitwert(Begrenzungsgrund grund, double leitwertWK, double thetaHC,
                                              double untenC, double obenC, Uebergabeseite seite = Uebergabeseite.Heizen)
                => new Kopplung(grund, leitwertWK, thetaHC, untenC, obenC, double.PositiveInfinity, seite);
        }

        /// <summary>
        /// Die Angaben EINER Seite der Übergabe aus dem Stundenrand (Schritt H bzw. K), im Raum der
        /// Raumluft: Sollwert, Vorlauf, Leistungsgrenze, Kennwerte und Strahlungsanteil. Auf der
        /// Kälteseite sind die Kennwerte schon gespiegelt (−V, −R, −θ_i,N); Sollwert und Vorlauf
        /// spiegelt <see cref="SchrittUebergabe"/>.
        /// </summary>
        private readonly struct Seitenrand
        {
            private Seitenrand(double sollC, double vorlaufC, double leistungMaxW, Uebergabekennwerte kennwerte,
                               double strahlungsanteil, bool vorlaufGekappt)
            {
                SollC = sollC;
                VorlaufC = vorlaufC;
                LeistungMaxW = leistungMaxW;
                Kennwerte = kennwerte;
                Strahlungsanteil = strahlungsanteil;
                VorlaufGekappt = vorlaufGekappt;
            }

            /// <summary>Sollwert der Seite [°C]: Heizsollwert bzw. Kühlsollwert (obere Grenze).</summary>
            internal double SollC { get; }

            /// <summary>Vorlauf der Stunde [°C]; NaN = Heizkurve aus.</summary>
            internal double VorlaufC { get; }

            /// <summary>Leistungsgrenze der Seite als positiver Betrag [W]; NaN = unbegrenzt.</summary>
            internal double LeistungMaxW { get; }

            /// <summary>Die Kennwerte der Übergabe — auf der Kälteseite gespiegelt.</summary>
            internal Uebergabekennwerte Kennwerte { get; }

            /// <summary>Strahlungsanteil der Übergabe [–].</summary>
            internal double Strahlungsanteil { get; }

            /// <summary>Kälteseite: Steht der Vorlauf an der Vorlaufgrenze (7.2)? Wärmeseite: nie.</summary>
            internal bool VorlaufGekappt { get; }

            /// <summary>Die Wärmeseite (Schritt H).</summary>
            internal static Seitenrand Heizseite(in Stundenrand r)
                => new Seitenrand(r.ThetaSoll, r.VorlaufC, r.HeizleistungMaxW, r.Uebergabe,
                                  r.HeizungStrahlungsanteil, false);

            /// <summary>Die Kälteseite (Schritt K, E37).</summary>
            internal static Seitenrand Kaelteseite(in Stundenrand r)
                => new Seitenrand(r.ThetaMax, r.KuehlVorlaufC, r.KuehlleistungMaxW, r.KuehlUebergabeGespiegelt,
                                  r.KuehlStrahlungsanteil, r.KuehlVorlaufGekappt);
        }

        /// <summary>Ein Abschnitt: System, konstanter Teil der Unbekannten und b.</summary>
        private readonly struct Abschnitt
        {
            internal Abschnitt(Zonenmodell2K m, Fallsystem s, double r0, double r1, double r2,
                               double thetaEq, double qFest, double thetaFest)
            {
                System = s;
                QFest = qFest;
                ThetaFest = thetaFest;
                s.Konstante(r0, r1, r2, out double c0, out double c1, out double c2);
                _c0 = c0;
                _c1 = c1;
                _c2 = c2;
                B = new Vektor2((m._gRest * thetaEq + m._g1 * c0) / m._c1, (m._g2 * c1) / m._c2);
                K = default;
            }

            private Abschnitt(in Abschnitt quelle, in Kopplung k)
            {
                System = quelle.System;
                QFest = quelle.QFest;
                ThetaFest = quelle.ThetaFest;
                _c0 = quelle._c0;
                _c1 = quelle._c1;
                _c2 = quelle._c2;
                B = quelle.B;
                K = k;
            }

            /// <summary>Derselbe Abschnitt mit den Angaben der Übergabe (Schritt H).</summary>
            internal Abschnitt MitKopplung(in Kopplung k) => new Abschnitt(in this, in k);

            private readonly double _c0, _c1, _c2;

            internal Fallsystem System { get; }
            internal Vektor2 B { get; }
            internal double QFest { get; }
            internal double ThetaFest { get; }

            /// <summary>Die Angaben der Übergabe; <c>default</c> ohne Kopplung.</summary>
            internal Kopplung K { get; }

            /// <summary>Gehört der Abschnitt zu einer Stunde mit Übergabe?</summary>
            internal bool Gekoppelt => K.Gekoppelt;

            /// <summary>Hängt die Übergabe als Leitwert im freien Lauf (Fall „Übergabe begrenzt")?</summary>
            internal bool MitLeitwert => K.LeitwertWK > 0.0;

            /// <summary>Leitwert G der Übergabe [W/K].</summary>
            internal double LeitwertWK => K.LeitwertWK;

            /// <summary>Ersatztemperatur θ_H [°C].</summary>
            internal double ThetaHC => K.ThetaHC;

            /// <summary>Begrenzungsgrund des Abschnitts.</summary>
            internal Begrenzungsgrund Grund => K.Grund;

            /// <summary>Die Unbekannte <paramref name="i"/> (0: θ_s,AW, 1: θ_s,IW, 2: θ_air bzw. Φ) im Zustand x.</summary>
            internal double Ausgang(int i, Vektor2 x)
            {
                switch (i)
                {
                    case 0: return System.Z00 * x.A + System.Z01 * x.B + _c0;
                    case 1: return System.Z10 * x.A + System.Z11 * x.B + _c1;
                    default: return System.Z20 * x.A + System.Z21 * x.B + _c2;
                }
            }
        }
    }
}

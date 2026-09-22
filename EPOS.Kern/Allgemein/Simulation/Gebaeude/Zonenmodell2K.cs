using System;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die fünf Betriebsfälle der idealen Regelung (Rechenschritte 7.1, Kühlkonzept 3.2).
    /// Jeder Abschnitt einer Stunde hat genau einen davon.
    /// </summary>
    internal enum Betriebsfall
    {
        /// <summary>Die Heizung hält die Raumluft auf dem Sollwert; Leistung zwischen 0 und der Grenze.</summary>
        HeizenGeregelt,

        /// <summary>Die Heizung liefert ihre Grenzleistung; die Raumluft liegt unter dem Sollwert.</summary>
        Heizgrenze,

        /// <summary>Weder Heizen noch Kühlen; die Raumluft liegt zwischen Sollwert und oberer Grenze.</summary>
        Totband,

        /// <summary>Die Kühlung hält die Raumluft auf der oberen Grenze (Kappung).</summary>
        KuehlenGeregelt,

        /// <summary>Die Kühlung liefert ihre Grenzleistung; die Raumluft liegt über der oberen Grenze.</summary>
        Kuehlgrenze,
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
                double q = ab.System.Geregelt ? z2 : ab.QFest;

                // Je Abschnitt nie beides: Heizfälle buchen nur Heizen, Kühlfälle nur Kühlen.
                switch (fall)
                {
                    case Betriebsfall.HeizenGeregelt:
                    case Betriebsfall.Heizgrenze:
                        akkHeiz += Math.Max(q, 0.0) * tau;
                        break;
                    case Betriebsfall.KuehlenGeregelt:
                    case Betriebsfall.Kuehlgrenze:
                        akkKuehl += Math.Max(-q, 0.0) * tau;
                        break;
                }
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
                    if (Begrenzt(r.HeizleistungMaxW) && q0 > r.HeizleistungMaxW)
                    {
                        ab = Aufbauen(Betriebsfall.Heizgrenze, in r);
                        return Betriebsfall.Heizgrenze;
                    }
                    ab = h;
                    return Betriebsfall.HeizenGeregelt;
                }
            }
            if (r.MitKappung)
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
        /// Ist der Betriebsfall im Zustand <paramref name="x"/> verletzt — über den
        /// Zahlenrand hinaus? (Verletzungsmaße der Rechenschritte 7.1.)
        /// </summary>
        private static bool Verletzt(Betriebsfall fall, in Abschnitt ab, Vektor2 x, in Stundenrand r)
        {
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
                    return r.MitKappung && z2 - r.ThetaMax > Rechenrand.Zu(r.ThetaMax);
            }
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
            else if (r.MitHeizung && r.MitKappung && r.ThetaMax < r.ThetaSoll) fehler = "ThetaMax < ThetaSoll";
            else if (!GrenzeGueltig(r.HeizleistungMaxW)) fehler = "HeizleistungMaxW";
            else if (!GrenzeGueltig(r.KuehlleistungMaxW)) fehler = "KuehlleistungMaxW";
            else if (!AnteilGueltig(r.HeizungStrahlungsanteil)) fehler = "HeizungStrahlungsanteil";
            else if (!AnteilGueltig(r.KuehlungAnteilInnenflaeche)) fehler = "KuehlungAnteilInnenflaeche";
            else if (!Endlich(r.ZusatzleitwertWK) || r.ZusatzleitwertWK < 0.0) fehler = "ZusatzleitwertWK";

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
            internal Fallsystem(Zonenmodell2K m, bool geregelt, double anteilAW, double anteilIW,
                                double anteilLuft, double gExt, double schluessel)
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
                else
                {
                    l02 = m._gcAW;
                    l12 = m._gcIW;
                    l22 = -(m._gcAW + m._gcIW + gExt);
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
            }

            private readonly double _c0, _c1, _c2;

            internal Fallsystem System { get; }
            internal Vektor2 B { get; }
            internal double QFest { get; }
            internal double ThetaFest { get; }

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

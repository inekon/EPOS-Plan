using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Welche Grenze die Heizleistung im Raum gekappt hat — die Gebäudeseite der beiden
    /// Aufzählungen aus Anlagenkopplung 5.3, soweit sie in der Stufe AK1 entstehen (4.5,
    /// Schritt H4 in 10.2). Die Anlagenseite (<c>Verfuegbarkeitsgrund</c>) und die Gründe
    /// <c>VORLAUF_ANLAGE</c>, <c>VERFUEGBARKEIT</c>, <c>UMSCHALTUNG</c> kommen mit AK2,
    /// <c>VORLAUFGRENZE_KUEHLUNG</c> mit der Kälteseite (H9).
    /// </summary>
    internal enum Begrenzungsgrund
    {
        /// <summary>Keine Grenze hat gegriffen: ideale Regelung oder Regelbereich des P-Reglers.</summary>
        KeineBegrenzung,

        /// <summary>Die Übergabe liefert nichts: Heizkurve aus (Heizgrenze) oder Vorlauf nicht über der Raumluft (H1).</summary>
        Heizgrenze,

        /// <summary>Das Ventil steht voll offen, und die Heizfläche gibt her, was sie kann — die Übergabe ist die Grenze.</summary>
        Uebergabe,

        /// <summary>Die Leistungsgrenze des Gebäudes (<c>Heizleistung_Max</c>) hat gegriffen.</summary>
        HeizleistungMax,
    }

    /// <summary>
    /// Woher der Vorlauf eines gekoppelten Gebäudes kommt (Anlagenkopplung 3.4, 8.1).
    /// </summary>
    internal enum Vorlaufquelle
    {
        /// <summary>Außentemperaturgeführte Heizkurve (<c>Heizkurve_Aktiv</c> = 1, H2).</summary>
        Heizkurve,

        /// <summary>Fester Vorlauf der Anlage — der höchste projektierte Vorlauf der Wärmeerzeuger des Heizkanals.</summary>
        Anlage,

        /// <summary>Fester Vorlauf ohne Anlagenwert: der Auslegungsvorlauf der Übergabe, benannt als Hinweis.</summary>
        Auslegung,
    }

    /// <summary>
    /// <b>Die Kennwerte der Wärmeübergabe eines Gebäudes</b> (Anlagenkopplung 3.1, 3.2, 8.1) —
    /// unveränderlich, in <b>W und W/K</b>: Die Umrechnung aus den kW der Eingabe geschieht
    /// einmal im Eingangsbauer (3.3, N-A8); ab hier rechnet Schritt H nur in W.
    ///
    /// <para><see cref="PhiNW"/> = +∞ ist die <b>unbegrenzte Übergabe</b> der Probe B
    /// (3.7, 11.1): Sie liefert bei jedem Vorlauf über der Raumluft jede verlangte Leistung.</para>
    /// </summary>
    internal sealed class Uebergabekennwerte
    {
        /// <param name="phiNW">Nennleistung der Übergabe im Auslegungspunkt [W]; +∞ = unbegrenzt.</param>
        /// <param name="exponent">Exponent der Übergabeart [–].</param>
        /// <param name="auslegungVorlaufC">Auslegungsvorlauf [°C].</param>
        /// <param name="auslegungRuecklaufC">Auslegungsrücklauf [°C].</param>
        /// <param name="auslegungRaumC">Raumtemperatur im Auslegungspunkt [°C].</param>
        internal Uebergabekennwerte(double phiNW, double exponent, double auslegungVorlaufC,
                                    double auslegungRuecklaufC, double auslegungRaumC)
        {
            PhiNW = phiNW;
            Exponent = exponent;
            AuslegungVorlaufC = auslegungVorlaufC;
            AuslegungRuecklaufC = auslegungRuecklaufC;
            AuslegungRaumC = auslegungRaumC;
            SpreizungNK = auslegungVorlaufC - auslegungRuecklaufC;
            DeltaThetaMNK = 0.5 * (auslegungVorlaufC + auslegungRuecklaufC) - auslegungRaumC;
            WHWK = phiNW / SpreizungNK;
        }

        /// <summary>Nennleistung der Übergabe Φ_N [W]; +∞ = unbegrenzte Übergabe (Probe B).</summary>
        internal double PhiNW { get; }

        /// <summary>Exponent n der Übergabegleichung [–].</summary>
        internal double Exponent { get; }

        /// <summary>Auslegungsvorlauf θ_V,N [°C].</summary>
        internal double AuslegungVorlaufC { get; }

        /// <summary>Auslegungsrücklauf θ_R,N [°C].</summary>
        internal double AuslegungRuecklaufC { get; }

        /// <summary>Raumtemperatur im Auslegungspunkt θ_i,N [°C].</summary>
        internal double AuslegungRaumC { get; }

        /// <summary>Spreizung im Auslegungspunkt θ_V,N − θ_R,N [K].</summary>
        internal double SpreizungNK { get; }

        /// <summary>Übertemperatur im Auslegungspunkt Δθ_m,N = (θ_V,N + θ_R,N)/2 − θ_i,N [K] (arithmetisches Mittel, H-F1).</summary>
        internal double DeltaThetaMNK { get; }

        /// <summary>Wärmekapazitätsstrom des Heizkreises W_H = Φ_N/(θ_V,N − θ_R,N) [W/K] — konstanter Massenstrom (H-F2).</summary>
        internal double WHWK { get; }

        /// <summary>Ist die Übergabe unbegrenzt (Φ_N = +∞, Probe B)?</summary>
        internal bool Unbegrenzt => double.IsPositiveInfinity(PhiNW);
    }

    /// <summary>
    /// <b>Die Wärmeübergabe</b> (Anlagenkopplung 3.1 bis 3.3, Schritt H in 10.2) — eine reine
    /// Rechenklasse ohne Zustand, ohne Datenbank, durchgehend <c>double</c>.
    ///
    /// <para><b>Die Übergabegleichung</b> Φ = Φ_N·((θ_m − θ_i)/Δθ_m,N)^n mit dem
    /// arithmetischen Mittel θ_m = (θ_V + θ_R)/2 (H-F1) und <b>konstantem Massenstrom</b>
    /// θ_R = θ_V − Φ/W_H (H-F2) wird je Aufruf als skalares Nullstellenproblem gelöst:
    /// <c>f(Φ) = Φ_N·((θ_V − Φ/(2·W_H) − θ_i)/Δθ_m,N)^n − Φ</c>. f ist konvex und streng
    /// fallend; Newton von unten konvergiert deshalb monoton, ohne Überschwingen.</para>
    ///
    /// <para><b>Startwert und Schrittzahl (3.2, H2).</b> Gestartet wird an der Nullstelle der
    /// linearisierten Form (Tangente in Φ = 0) — sie liegt unter der Lösung. Ist die Übergabe
    /// im Verhältnis zum Heizkreis sehr stark (Kennzahl K = g₀·b/a &gt; 1), wird der Start auf
    /// die geschlossene untere Schranke a/b·(1 − K^(−1/n)) angehoben; sie liegt ebenfalls unter
    /// der Lösung. So bleibt die Schrittzahl über den ganzen zulässigen Bereich klein.
    /// Höchstens <see cref="NEWTON_SCHRITTE_MAX"/> Schritte, Abbruch bei
    /// |f| ≤ <see cref="NEWTON_ABBRUCH_W"/> + <see cref="NEWTON_ABBRUCH_RELATIV"/>·Φ und danach
    /// ein Nachschritt, der die Lösung auf die Genauigkeit des <c>double</c> bringt — so hängt
    /// sie nicht von der Einheit ab (Probe „Einheiten", 11.1); sonst
    /// <see cref="GebaeudeModellFehler.UebergabeNichtKonvergiert"/>.</para>
    /// </summary>
    internal static class Waermeuebergabe
    {
        /// <summary>Höchstzahl der Newton-Schritte der Übergabegleichung (10.2 H2).</summary>
        internal const int NEWTON_SCHRITTE_MAX = 8;

        /// <summary>Abbruchschwelle der Newton-Iteration, absolut [W] (10.2 H2).</summary>
        internal const double NEWTON_ABBRUCH_W = 1e-6;

        /// <summary>
        /// Abbruchschwelle der Newton-Iteration, relativ zur Leistung [–] — benannte Ergänzung:
        /// Bei Leistungen im Megawattbereich liegt 1e-6 W unter der Auflösung eines
        /// <c>double</c>, und die absolute Schwelle allein wäre nie zu erreichen.
        /// </summary>
        internal const double NEWTON_ABBRUCH_RELATIV = 1e-12;

        /// <summary>Höchstzahl der Schritte der Arbeitspunktsuche im Regelbereich.</summary>
        internal const int ARBEITSPUNKT_SCHRITTE_MAX = 80;

        /// <summary>Abbruchschwelle der Arbeitspunktsuche [K].</summary>
        internal const double ARBEITSPUNKT_ABBRUCH_K = 1e-11;

        // =====================================================================
        //  Vorgaben je Übergabeart (3.1) und die Frage „ist die Kopplung wirksam?"
        // =====================================================================

        /// <summary>Kennt der Rechenweg die Übergabeart (Radiator, Flächenheizung, Konvektor)?</summary>
        internal static bool ArtBekannt(string art)
            => art == DbWerte.UEBERGABE_RADIATOR || art == DbWerte.UEBERGABE_FLAECHE || art == DbWerte.UEBERGABE_KONVEKTOR;

        /// <summary>Exponent der Übergabeart [–] (3.1); NaN für eine unbekannte Art.</summary>
        internal static double VorgabeExponent(string art)
        {
            switch (art)
            {
                case DbWerte.UEBERGABE_RADIATOR: return GebaeudeFestwerte.UEBERGABE_EXPONENT_RADIATOR;
                case DbWerte.UEBERGABE_FLAECHE: return GebaeudeFestwerte.UEBERGABE_EXPONENT_FLAECHE;
                case DbWerte.UEBERGABE_KONVEKTOR: return GebaeudeFestwerte.UEBERGABE_EXPONENT_KONVEKTOR;
                default: return double.NaN;
            }
        }

        /// <summary>Auslegungsvorlauf der Übergabeart [°C] (3.1).</summary>
        internal static double VorgabeVorlaufC(string art)
            => art == DbWerte.UEBERGABE_FLAECHE ? GebaeudeFestwerte.AUSLEGUNG_VORLAUF_FLAECHE
               : ArtBekannt(art) ? GebaeudeFestwerte.AUSLEGUNG_VORLAUF_RADIATOR : double.NaN;

        /// <summary>Auslegungsrücklauf der Übergabeart [°C] (3.1).</summary>
        internal static double VorgabeRuecklaufC(string art)
            => art == DbWerte.UEBERGABE_FLAECHE ? GebaeudeFestwerte.AUSLEGUNG_RUECKLAUF_FLAECHE
               : ArtBekannt(art) ? GebaeudeFestwerte.AUSLEGUNG_RUECKLAUF_RADIATOR : double.NaN;

        /// <summary>Strahlungsanteil der Übergabeart [–] (3.1, H12) — gilt, wenn <c>Heizung_Strahlungsanteil</c> leer ist.</summary>
        internal static double VorgabeStrahlungsanteil(string art)
        {
            switch (art)
            {
                case DbWerte.UEBERGABE_RADIATOR: return GebaeudeFestwerte.STRAHLUNGSANTEIL_RADIATOR;
                case DbWerte.UEBERGABE_FLAECHE: return GebaeudeFestwerte.STRAHLUNGSANTEIL_FLAECHE;
                case DbWerte.UEBERGABE_KONVEKTOR: return GebaeudeFestwerte.STRAHLUNGSANTEIL_KONVEKTOR;
                default: return double.NaN;
            }
        }

        /// <summary>
        /// Ist eine Kopplungsstufe gesetzt, die den Heizkreis rechnet (AK1 oder höher)? NULL,
        /// leer und <c>AUS</c> heißen „aus" (8.1).
        /// </summary>
        internal static bool StufeAn(string stufe)
            => stufe == DbWerte.ANLAGENKOPPLUNG_AK1 || stufe == DbWerte.ANLAGENKOPPLUNG_AK2
               || stufe == DbWerte.ANLAGENKOPPLUNG_AK3;

        /// <summary>
        /// <b>Ist die Kopplung für dieses Gebäude wirksam?</b> Genau dann, wenn das PROJEKT eine
        /// Kopplungsstufe rechnet (<see cref="StufeAn"/>), der Schalter <c>Heizkreis_Aktiv</c> am
        /// Gebäude steht und eine Übergabeart gesetzt ist, die nicht „ideal" heißt (F-A1, F-A17,
        /// 8.1). Sonst rechnet das Gebäude wie bisher — byte-gleich (N-A3).
        /// </summary>
        internal static bool KopplungWirksamFuer(ProjektGebaeudeModel g, string stufe)
        {
            if (g == null || !StufeAn(stufe) || !g.Heizkreis_Aktiv) return false;
            return !string.IsNullOrWhiteSpace(g.Uebergabe_Art)
                   && !string.Equals(g.Uebergabe_Art, DbWerte.UEBERGABE_IDEAL, StringComparison.Ordinal);
        }

        // =====================================================================
        //  H2 — die Übergabe bei voll geöffnetem Ventil
        // =====================================================================

        /// <summary>
        /// <b>H2:</b> die Leistung der voll geöffneten Übergabe Φ_ue,max [W] bei Vorlauf
        /// <paramref name="thetaVC"/> und Raumluft <paramref name="thetaIC"/>. Null, wenn der
        /// Vorlauf nicht über der Raumluft liegt oder undefiniert ist (Heizkurve aus, H1).
        /// </summary>
        internal static double LeistungOffenW(Uebergabekennwerte k, double thetaVC, double thetaIC)
        {
            if (!(thetaVC > thetaIC)) return 0.0;
            if (k.Unbegrenzt) return double.PositiveInfinity;
            return Loesen(k.PhiNW, k.Exponent, thetaVC - thetaIC, 0.5 / k.WHWK, k.DeltaThetaMNK, out _);
        }

        /// <summary>
        /// Der Arbeitspunkt der <b>gesättigten</b> Übergabe im Raum [W]: Die Raumluft antwortet
        /// auf die Heizleistung affin, θ_air = θ₀ + s·Φ (freier Lauf am Abschnittsbeginn); mit
        /// dem voll offenen Ventil gilt dann Φ = Φ_N·((θ_V − θ₀ − Φ·(1/(2·W_H) + s))/Δθ_m,N)^n
        /// — dieselbe Gleichung wie H2 mit einem um s vergrößerten Glied, also derselbe Löser.
        /// </summary>
        /// <param name="theta0C">Raumluft im freien Lauf ohne Heizung [°C].</param>
        /// <param name="sKW">Antwort der Raumluft auf die Heizleistung ∂θ_air/∂Φ [K/W], ≥ 0.</param>
        internal static double LeistungGesaettigtW(Uebergabekennwerte k, double thetaVC, double theta0C, double sKW)
        {
            if (!(thetaVC > theta0C)) return 0.0;
            if (k.Unbegrenzt) return double.PositiveInfinity;
            return Loesen(k.PhiNW, k.Exponent, thetaVC - theta0C, 0.5 / k.WHWK + sKW, k.DeltaThetaMNK, out _);
        }

        /// <summary>
        /// Steigung der voll geöffneten Übergabe nach der Raumluft, G_H = −dΦ/dθ_i [W/K] (3.3):
        /// <c>G_H = (n·Φ/(θ_m − θ_i)) / (1 + n·Φ/((θ_m − θ_i)·2·W_H))</c> mit
        /// θ_m = θ_V − Φ/(2·W_H). Null ohne Leistung.
        /// </summary>
        internal static double SteigungOffenWK(Uebergabekennwerte k, double phiOffenW, double thetaVC, double thetaIC)
        {
            if (!(phiOffenW > 0.0) || double.IsInfinity(phiOffenW)) return 0.0;
            double dm = thetaVC - phiOffenW / (2.0 * k.WHWK) - thetaIC;
            if (!(dm > 0.0)) return 0.0;
            double z = k.Exponent * phiOffenW / dm;
            return z / (1.0 + z / (2.0 * k.WHWK));
        }

        /// <summary>Rücklauf zur GELIEFERTEN Leistung, θ_R = θ_V − Φ/W_H [°C] (H6, H-F2).</summary>
        internal static double RuecklaufC(Uebergabekennwerte k, double thetaVC, double phiW)
            => k.Unbegrenzt ? thetaVC : thetaVC - phiW / k.WHWK;

        /// <summary>
        /// Die Kennlinie des Raumreglers (4.4, H3): die Leistung, die die Anlage bei Raumluft
        /// <paramref name="thetaIC"/> liefern will, <c>y·Φ_ue,max</c> mit
        /// <c>y = clamp((θ_soll − θ_i)/Xp, 0, 1)</c>; mit Xp = 0 ist y = 1 unter dem Sollwert
        /// und 0 darüber. Ohne <c>Heizleistung_Max</c>.
        /// </summary>
        internal static double LeistungKennlinieW(Uebergabekennwerte k, double thetaVC, double sollC,
                                                  double reglerbandK, double thetaIC)
        {
            double y = Stellgrad(sollC, reglerbandK, thetaIC);
            return y > 0.0 ? y * LeistungOffenW(k, thetaVC, thetaIC) : 0.0;
        }

        /// <summary>Der Stellgrad y des P-Reglers (4.4).</summary>
        internal static double Stellgrad(double sollC, double reglerbandK, double thetaIC)
        {
            if (reglerbandK == 0.0) return thetaIC < sollC ? 1.0 : 0.0;
            double y = (sollC - thetaIC) / reglerbandK;
            return y <= 0.0 ? 0.0 : y >= 1.0 ? 1.0 : y;
        }

        // =====================================================================
        //  H3/H5 — der Arbeitspunkt im Regelbereich und die Kappungstemperatur
        // =====================================================================

        /// <summary>
        /// Der <b>Arbeitspunkt im Regelbereich</b> des P-Reglers (0 &lt; y &lt; 1): die Raumluft
        /// θ*, bei der θ* = θ₀ + s·y(θ*)·Φ_ue,max(θ*) gilt, dazu die Leistung Φ* und der
        /// Leitwert der GEFAHRENEN Kennlinie G = Φ_ue,max/Xp + y·G_H (10.2 H5). Gesucht auf
        /// [max(θ₀, θ_soll − Xp), θ_soll], wo h(θ) = θ₀ + s·Φ(θ) − θ streng fällt: Newton mit
        /// Schutzintervall (ein Schritt, der das Intervall verlässt, wird halbiert).
        /// </summary>
        internal static void ArbeitspunktRegelbereich(Uebergabekennwerte k, double thetaVC, double sollC,
                                                      double reglerbandK, double theta0C, double sKW,
                                                      out double thetaSternC, out double phiSternW, out double leitwertWK)
        {
            double lo = Math.Max(theta0C, sollC - reglerbandK);
            double hi = sollC;
            double theta = lo;
            for (int i = 0; i < ARBEITSPUNKT_SCHRITTE_MAX; i++)
            {
                double phiOffen = LeistungOffenW(k, thetaVC, theta);
                double y = Stellgrad(sollC, reglerbandK, theta);
                double g = phiOffen / reglerbandK + y * SteigungOffenWK(k, phiOffen, thetaVC, theta);
                double h = theta0C + sKW * y * phiOffen - theta;
                if (h > 0.0) lo = theta; else hi = theta;
                double neu = theta + h / (1.0 + sKW * g);
                // Erst die Abbruchbedingung, dann das Schutzintervall: Ein Newton-Schritt, der auf
                // der Nullstelle landet, liegt auf der Grenze des Intervalls und ist kein Ausreißer.
                bool fertig = h == 0.0 || Math.Abs(neu - theta) <= ARBEITSPUNKT_ABBRUCH_K;
                if (!fertig && !(neu > lo && neu < hi)) neu = 0.5 * (lo + hi);
                if (fertig || hi - lo <= ARBEITSPUNKT_ABBRUCH_K)
                {
                    thetaSternC = neu;
                    phiOffen = LeistungOffenW(k, thetaVC, neu);
                    y = Stellgrad(sollC, reglerbandK, neu);
                    phiSternW = y * phiOffen;
                    leitwertWK = phiOffen / reglerbandK + y * SteigungOffenWK(k, phiOffen, thetaVC, neu);
                    return;
                }
                theta = neu;
            }
            throw new GebaeudeModellException(GebaeudeModellFehler.UebergabeNichtKonvergiert,
                "Der Arbeitspunkt des Raumreglers ist nach " + ARBEITSPUNKT_SCHRITTE_MAX.ToString(CultureInfo.InvariantCulture) +
                " Schritten nicht gefunden (Intervall " + Text(lo) + " … " + Text(hi) + " °C).");
        }

        /// <summary>
        /// Die <b>Kappungstemperatur</b> [°C]: die Raumluft, bei der die Kennlinie des Reglers
        /// (<see cref="LeistungKennlinieW"/>) gerade <paramref name="heizleistungMaxW"/> liefert.
        /// Darunter kappt <c>Heizleistung_Max</c>, darüber nicht — die obere Gültigkeitsgrenze
        /// des Falls „Leistung fest" (10.2 H5). Im gesättigten Bereich geschlossen,
        /// θ = θ_V − HL/(2·W_H) − Δθ_m,N·(HL/Φ_N)^(1/n); mit Xp = 0 höchstens der Sollwert —
        /// bei unbegrenzter Übergabe also genau der Sollwert, die Grenze des Bestands.
        /// </summary>
        internal static double Kappungstemperatur(Uebergabekennwerte k, double thetaVC, double sollC,
                                                  double reglerbandK, double heizleistungMaxW)
        {
            double satt = k.Unbegrenzt
                ? thetaVC
                : thetaVC - heizleistungMaxW / (2.0 * k.WHWK)
                  - k.DeltaThetaMNK * Math.Pow(heizleistungMaxW / k.PhiNW, 1.0 / k.Exponent);
            if (reglerbandK == 0.0) return Math.Min(satt, sollC);
            double knick = sollC - reglerbandK;
            if (satt <= knick) return satt;

            // Regelbereich: g(θ) = y(θ)·Φ_ue,max(θ) − HL fällt streng von g(knick) > 0 auf g(θ_soll) = −HL.
            double lo = knick, hi = sollC, theta = knick;
            for (int i = 0; i < ARBEITSPUNKT_SCHRITTE_MAX; i++)
            {
                double phiOffen = LeistungOffenW(k, thetaVC, theta);
                double y = Stellgrad(sollC, reglerbandK, theta);
                double g = y * phiOffen - heizleistungMaxW;
                double steigung = phiOffen / reglerbandK + y * SteigungOffenWK(k, phiOffen, thetaVC, theta);
                if (g > 0.0) lo = theta; else hi = theta;
                double neu = steigung > 0.0 ? theta + g / steigung : 0.5 * (lo + hi);
                bool fertig = g == 0.0 || Math.Abs(neu - theta) <= ARBEITSPUNKT_ABBRUCH_K;
                if (!fertig && !(neu > lo && neu < hi)) neu = 0.5 * (lo + hi);
                if (fertig || hi - lo <= ARBEITSPUNKT_ABBRUCH_K) return neu;
                theta = neu;
            }
            throw new GebaeudeModellException(GebaeudeModellFehler.UebergabeNichtKonvergiert,
                "Die Kappungstemperatur der Leistungsgrenze ist nach " + ARBEITSPUNKT_SCHRITTE_MAX.ToString(CultureInfo.InvariantCulture) +
                " Schritten nicht gefunden (Intervall " + Text(lo) + " … " + Text(hi) + " °C).");
        }

        // =====================================================================
        //  Der Löser der Übergabegleichung
        // =====================================================================

        /// <summary>
        /// Löst Φ = Φ_N·((a − b·Φ)/c)^n nach Φ [W] (3.2): a = θ_V − θ_Bezug [K], b [K/W] das Glied
        /// der Heizmitteltemperatur (1/(2·W_H), ggf. plus die Antwort der Raumluft), c = Δθ_m,N.
        /// <paramref name="schritte"/> nennt die Zahl der Newton-Schritte.
        /// </summary>
        /// <exception cref="GebaeudeModellException">
        /// <see cref="GebaeudeModellFehler.UebergabeNichtKonvergiert"/> nach
        /// <see cref="NEWTON_SCHRITTE_MAX"/> Schritten ohne Abbruchbedingung.</exception>
        internal static double Loesen(double phiNW, double n, double a, double b, double c, out int schritte)
        {
            schritte = 0;
            if (!(a > 0.0)) return 0.0;
            double g0 = phiNW * Math.Pow(a / c, n);
            if (!(b > 0.0)) return g0;
            double kennzahl = g0 * b / a;
            double phi = g0 / (1.0 + n * kennzahl);
            if (kennzahl > 1.0) phi = Math.Max(phi, a / b * (1.0 - Math.Pow(kennzahl, -1.0 / n)));
            double f = double.NaN;
            for (; ; schritte++)
            {
                double t = (a - b * phi) / c;
                if (!(t > 0.0)) break;
                double g = phiNW * Math.Pow(t, n);
                f = g - phi;
                double ableitung = -n * phiNW * Math.Pow(t, n - 1.0) * b / c - 1.0;
                if (Math.Abs(f) <= NEWTON_ABBRUCH_W + NEWTON_ABBRUCH_RELATIV * phi)
                {
                    // Nachschritt: Die Schwelle ist in W gemessen (3.3); ein weiterer Schritt der
                    // quadratisch konvergierenden Iteration bringt die Lösung auf die Genauigkeit
                    // des double, damit sie nicht von der Einheit abhängt (Probe „Einheiten", 11.1).
                    // Er steht außerhalb der Zählung und läuft von unten, also ohne Überschwingen.
                    return phi - f / ableitung;
                }
                if (schritte >= NEWTON_SCHRITTE_MAX) break;
                phi -= f / ableitung;
            }
            throw new GebaeudeModellException(GebaeudeModellFehler.UebergabeNichtKonvergiert,
                "Die Übergabegleichung ist nach " + schritte.ToString(CultureInfo.InvariantCulture) +
                " Newton-Schritten nicht gelöst (Rest " + Text(f) + " W, Φ_N = " + Text(phiNW) +
                " W, n = " + Text(n) + ", a = " + Text(a) + " K) — benannter Abbruch statt stiller Näherung.");
        }

        private static string Text(double w) => w.ToString("G6", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// <b>Die außentemperaturgeführte Heizkurve</b> (Anlagenkopplung 3.4, H2) — aus dem
    /// Auslegungspunkt abgeleitet, mit Niveau und Steilheit als Feinschliff:
    /// <code>
    /// φ   = (θ_i,soll − θ_out)/(θ_i,N − θ_out,N), oben auf 1 begrenzt
    /// θ_V = θ_i,soll + Niveau + Steilheit·(φ^(1/n)·Δθ_m,N + φ·Δθ_N/2)
    /// θ_R = θ_i,soll + Niveau + Steilheit·(φ^(1/n)·Δθ_m,N − φ·Δθ_N/2)   (Sollkurve)
    /// </code>
    /// Oben gekappt an θ_V,N (die Anlage kann nicht mehr), unten an der <b>Heizgrenze</b>:
    /// Mit φ ≤ 0 ist die Kurve aus, der Vorlauf undefiniert (NaN) und die Übergabe null.
    /// </summary>
    internal sealed class Heizkurve
    {
        private readonly Uebergabekennwerte _k;

        internal Heizkurve(Uebergabekennwerte k, double auslegungAussenC, double niveauK, double steilheit)
        {
            _k = k ?? throw new ArgumentNullException(nameof(k));
            AuslegungAussenC = auslegungAussenC;
            NiveauK = niveauK;
            Steilheit = steilheit;
        }

        /// <summary>Auslegungs-Außentemperatur θ_out,N [°C].</summary>
        internal double AuslegungAussenC { get; }

        /// <summary>Niveau [K].</summary>
        internal double NiveauK { get; }

        /// <summary>Steilheit [–].</summary>
        internal double Steilheit { get; }

        /// <summary>Die relative Last φ, oben auf 1 begrenzt; ≤ 0 heißt: Heizgrenze, Kurve aus.</summary>
        internal double Relativlast(double sollC, double aussenC)
        {
            double phi = (sollC - aussenC) / (_k.AuslegungRaumC - AuslegungAussenC);
            return phi > 1.0 ? 1.0 : phi;
        }

        /// <summary>Vorlauf der Kurve [°C]; NaN jenseits der Heizgrenze.</summary>
        internal double VorlaufC(double sollC, double aussenC)
        {
            double phi = Relativlast(sollC, aussenC);
            if (!(phi > 0.0)) return double.NaN;
            double v = sollC + NiveauK + Steilheit * (Math.Pow(phi, 1.0 / _k.Exponent) * _k.DeltaThetaMNK
                                                      + phi * _k.SpreizungNK / 2.0);
            return v > _k.AuslegungVorlaufC ? _k.AuslegungVorlaufC : v;
        }

        /// <summary>
        /// Die Sollkurve des Rücklaufs [°C] — NICHT der gerechnete Rücklauf, der aus der
        /// tatsächlichen Übergabe entsteht (3.2); beide fallen am Auslegungspunkt zusammen.
        /// </summary>
        internal double RuecklaufSollC(double sollC, double aussenC)
        {
            double phi = Relativlast(sollC, aussenC);
            if (!(phi > 0.0)) return double.NaN;
            return sollC + NiveauK + Steilheit * (Math.Pow(phi, 1.0 / _k.Exponent) * _k.DeltaThetaMNK
                                                  - phi * _k.SpreizungNK / 2.0);
        }
    }
}

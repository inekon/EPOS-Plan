using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kühlübergabe</b> — die Kälteseite der Anlagenkopplung (Entscheid E37; Konzept
    /// Anlagenkopplung 7, 8.1, 10.5): die EPOS-Vorgaben je Kühlübergabeart, die Frage „ist die
    /// Kälteseite für dieses Gebäude wirksam?" und der Spiegel der Kennwerte für Schritt K.
    ///
    /// <para><b>Schritt K ist Schritt H im gespiegelten Raum.</b> Die Kennwerte der Kühlübergabe
    /// (Φ_N, n, V, R, θ_i,N mit V &lt; R &lt; θ_i,N) gehen gespiegelt in den Löser: (Φ_N, n, −V, −R,
    /// −θ_i,N). Dann ist die Spreizung R − V &gt; 0 und die Übertemperatur
    /// Δθ_m,N = θ_i,N − (V + R)/2 &gt; 0, und jede Rechenfunktion von <see cref="Waermeuebergabe"/>
    /// gilt unverändert — sie wächst nicht.</para>
    ///
    /// <para>Reine Rechenklasse ohne Zustand, ohne Datenbank, durchgehend <c>double</c>. Alle
    /// Zahlen der Vorgaben sind EPOS-Vorgaben (A4), keine Normwerte.</para>
    /// </summary>
    internal static class Kuehluebergabe
    {
        /// <summary>Kennt der Rechenweg die Kühlübergabeart (Kühldecke, Flächenkühlung, Gebläsekonvektor)?</summary>
        internal static bool ArtBekannt(string art)
            => art == DbWerte.KUEHLUEBERGABE_KUEHLDECKE || art == DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG
               || art == DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR;

        /// <summary>Exponent der Kühlübergabeart [–]; NaN für eine unbekannte Art.</summary>
        internal static double VorgabeExponent(string art)
        {
            switch (art)
            {
                case DbWerte.KUEHLUEBERGABE_KUEHLDECKE: return GebaeudeFestwerte.KUEHLUEBERGABE_EXPONENT_KUEHLDECKE;
                case DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG: return GebaeudeFestwerte.KUEHLUEBERGABE_EXPONENT_FLAECHENKUEHLUNG;
                case DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR: return GebaeudeFestwerte.KUEHLUEBERGABE_EXPONENT_GEBLAESEKONVEKTOR;
                default: return double.NaN;
            }
        }

        /// <summary>Auslegungsvorlauf der Kühlübergabeart [°C]; NaN für eine unbekannte Art.</summary>
        internal static double VorgabeVorlaufC(string art)
            => art == DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR ? GebaeudeFestwerte.KUEHL_AUSLEGUNG_VORLAUF_KONVEKTOR
               : ArtBekannt(art) ? GebaeudeFestwerte.KUEHL_AUSLEGUNG_VORLAUF_FLAECHE : double.NaN;

        /// <summary>Auslegungsrücklauf der Kühlübergabeart [°C]; NaN für eine unbekannte Art.</summary>
        internal static double VorgabeRuecklaufC(string art)
            => art == DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR ? GebaeudeFestwerte.KUEHL_AUSLEGUNG_RUECKLAUF_KONVEKTOR
               : ArtBekannt(art) ? GebaeudeFestwerte.KUEHL_AUSLEGUNG_RUECKLAUF_FLAECHE : double.NaN;

        /// <summary>Strahlungsanteil der Kühlübergabeart [–] — eine Vorgabe ohne eigene Spalte (KU 3.2, 7.4 Punkt 8).</summary>
        internal static double VorgabeStrahlungsanteil(string art)
            => art == DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR ? GebaeudeFestwerte.KUEHL_STRAHLUNGSANTEIL_KONVEKTOR
               : ArtBekannt(art) ? GebaeudeFestwerte.KUEHL_STRAHLUNGSANTEIL_FLAECHE : double.NaN;

        /// <summary>
        /// Vorlaufgrenze der Kühlübergabeart [°C] — die Vorgabe statt einer Taupunktrechnung (7.2).
        /// <b>NaN heißt „keine Grenze"</b> (Gebläsekonvektor) und ebenso „unbekannte Art".
        /// </summary>
        internal static double VorgabeVorlaufgrenzeC(string art)
            => art == DbWerte.KUEHLUEBERGABE_KUEHLDECKE || art == DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG
               ? GebaeudeFestwerte.KUEHL_VORLAUFGRENZE_FLAECHE : double.NaN;

        /// <summary>
        /// <b>Ist die Kälteseite der Kopplung wirksam?</b> Genau dann, wenn das PROJEKT eine
        /// Kopplungsstufe rechnet (<see cref="Waermeuebergabe.StufeAn"/>, AK1 bis AK3), die Kühlung
        /// nach E32 wirksam ist (Projektschalter Kühlbetrieb, <c>Kuehlung_Aktiv</c>, Kühlsollwert),
        /// der Schalter <c>Kuehluebergabe_Aktiv</c> steht (A1) und eine Kühlübergabeart gesetzt ist,
        /// die nicht „ideal" heißt. <b>Unabhängig von <c>Heizkreis_Aktiv</c></b> — damit hält E32
        /// von selbst. Sonst rechnet die Kühlung wie bisher — byte-gleich.
        /// </summary>
        internal static bool KopplungWirksam(string stufe, bool kuehlbetriebProjekt, bool kuehlungAktiv,
                                             double? kuehlSollwert, bool? kuehluebergabeAktiv, string art)
        {
            if (!Waermeuebergabe.StufeAn(stufe)) return false;
            if (!kuehlbetriebProjekt || !kuehlungAktiv || !kuehlSollwert.HasValue) return false;
            if (kuehluebergabeAktiv != true) return false;
            return !string.IsNullOrWhiteSpace(art)
                   && !string.Equals(art, DbWerte.KUEHLUEBERGABE_IDEAL, StringComparison.Ordinal);
        }

        /// <summary>
        /// <see cref="KopplungWirksam"/> für eine Gebäudezeile — die Frage, die Eingangsbauer und
        /// Fassade (feste Nennleistung, H7) gleich beantworten.
        /// </summary>
        internal static bool KopplungWirksamFuer(ProjektGebaeudeModel g, string stufe, bool kuehlbetrieb)
            => g != null && KopplungWirksam(stufe, kuehlbetrieb, g.Kuehlung_Aktiv, g.Kuehl_Sollwert,
                                            g.Kuehluebergabe_Aktiv, g.Kuehl_Uebergabe_Art);

        /// <summary>
        /// Die <b>gespiegelten</b> Kennwerte der Kühlübergabe für Schritt K: (Φ_N, n, −V, −R, −θ_i,N).
        /// Spreizung R − V, Übertemperatur θ_i,N − (V + R)/2, Wärmekapazitätsstrom W_K = Φ_N/(R − V).
        /// </summary>
        /// <param name="phiNW">Nennleistung der Kühlübergabe [W], sensibel; +∞ = unbegrenzt (Probe B).</param>
        /// <param name="exponent">Exponent n [–].</param>
        /// <param name="auslegungVorlaufC">Auslegungsvorlauf V [°C].</param>
        /// <param name="auslegungRuecklaufC">Auslegungsrücklauf R [°C], V &lt; R.</param>
        /// <param name="auslegungRaumC">Raumtemperatur im Auslegungspunkt θ_i,N [°C], R &lt; θ_i,N.</param>
        internal static Uebergabekennwerte Gespiegelt(double phiNW, double exponent, double auslegungVorlaufC,
                                                      double auslegungRuecklaufC, double auslegungRaumC)
            => new Uebergabekennwerte(phiNW, exponent, -auslegungVorlaufC, -auslegungRuecklaufC, -auslegungRaumC);

        /// <summary>
        /// Die Leistung der voll geöffneten Kühlübergabe [W], positiv — bei Kaltwasser-Vorlauf
        /// <paramref name="vorlaufC"/> und Raumluft <paramref name="raumC"/>: der Spiegel von
        /// <see cref="Waermeuebergabe.LeistungOffenW"/>. Null, wenn der Vorlauf nicht unter der
        /// Raumluft liegt.
        /// </summary>
        internal static double LeistungOffenW(Uebergabekennwerte gespiegelt, double vorlaufC, double raumC)
            => Waermeuebergabe.LeistungOffenW(gespiegelt, -vorlaufC, -raumC);

        /// <summary>Rücklauf zur gelieferten Kühlleistung, θ_R = θ_V + Φ_c/W_K [°C] — der Spiegel von <see cref="Waermeuebergabe.RuecklaufC"/>.</summary>
        internal static double RuecklaufC(Uebergabekennwerte gespiegelt, double vorlaufC, double kuehlleistungW)
            => -Waermeuebergabe.RuecklaufC(gespiegelt, -vorlaufC, kuehlleistungW);
    }
}

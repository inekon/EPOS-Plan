using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kalenderschicht der Profilroutine</b> (Entscheidungsvorlage Modellgrenzen PW2, BW2;
    /// Konzept Simulationsablauf, Abschnitt 17) — zwischen der Kachelung des Wochenprofils und der
    /// Monatsnormierung, dieselbe Routine für Brauchwasser, Prozesswärme und Strom.
    ///
    /// <code>
    /// t(h)  = w((24 · w₀ + h) mod 168)                       Kachelung ab dem Wochentag des 1. Januar
    /// a(h)  = w(144 + s)        an einem Feiertag (Sonntag des Wochenprofils), sonst t(h)
    /// b(h)  = f · m_s           an einem Ferientag,   m_s = (1/7) Σ_d w(24 d + s), sonst a(h)
    /// q(h)  = b(h) / Σ_M b · M_m · 1000                      Ferien verteilen um (Vorgabe)
    /// q(h)  = b(h) / Σ_M a · M_m · 1000                      „Ferien kürzen die Monatsmenge"
    /// </code>
    /// h = 24 d + s, M = die Stunden des Monats m, M_m die Monatsmenge [MWh]. Feiertage verteilen
    /// stets nur um; gekürzt wird allein um die Ferien.
    ///
    /// <para><b>Ohne Kalender</b> ruft die Profilroutine diese Klasse nicht — es gilt
    /// <see cref="WPPlan.Core.BhkwPlan.StromWocheToJahr(double[], double[], double[], int[], int[], int)"/>
    /// Zeichen für Zeichen wie zuvor.</para>
    ///
    /// <para><b>Randfall:</b> Hat ein Monat ohne Kürzen keine Stunde mit Bedarf mehr (alle Tage
    /// Ferien mit f = 0), lässt sich seine Menge nicht umverteilen; der Monat rechnet dann ohne
    /// Ferien (Rückgabe <c>false</c>, der Aufrufer meldet es).</para>
    /// </summary>
    public static class Betriebskalenderschicht
    {
        private const int STUNDEN = 8760;
        private const int WOCHE = 168;
        private const int TAG = 24;
        private const int MONATE = 12;

        /// <summary>
        /// Expandiert das Wochenprofil <paramref name="wo"/> mit dem Kalender auf 8760 Stunden und
        /// normiert je Monat auf <paramref name="monatsverbrauch"/> [MWh] · 1000 = kWh.
        /// </summary>
        /// <param name="tagesarten">365 Tagesarten (<see cref="Betriebskalender.Tagesarten"/>).</param>
        /// <returns><c>false</c>, wenn mindestens ein Monat wegen des Randfalls ohne Ferien rechnete.</returns>
        public static bool WocheZuJahr(double[] wo, double[] monatsverbrauch, double[] outJahr,
                                       int[] moAnfang, int[] moEnde, int wochentagJan1,
                                       byte[] tagesarten, double ferienfaktor, bool ferienKuerzen)
        {
            if (wo == null) throw new ArgumentNullException(nameof(wo));
            if (tagesarten == null || tagesarten.Length < 365) throw new ArgumentException("365 Tagesarten erwartet.", nameof(tagesarten));

            // Kachelung wie BhkwPlan.StromWocheToJahr, Phase 1.
            int start = (((wochentagJan1 % 7) + 7) % 7) * TAG;
            var a = new double[STUNDEN];
            for (int h = 0; h < STUNDEN; h++) a[h] = wo[(start + h) % WOCHE];

            // Tagesmittel des Wochenprofils je Stunde des Tages.
            var mittel = new double[TAG];
            for (int s = 0; s < TAG; s++)
            {
                double summe = 0;
                for (int d = 0; d < 7; d++) summe += wo[d * TAG + s];
                mittel[s] = summe / 7.0;
            }

            // Feiertage: der Sonntag des Wochenprofils (wo[144..167]).
            for (int d = 0; d < 365; d++)
                if (tagesarten[d] == Betriebskalender.TAG_FEIERTAG)
                    for (int s = 0; s < TAG; s++) a[d * TAG + s] = wo[6 * TAG + s];

            // Ferien: f · Tagesmittel.
            var b = (double[])a.Clone();
            for (int d = 0; d < 365; d++)
                if (tagesarten[d] == Betriebskalender.TAG_FERIEN)
                    for (int s = 0; s < TAG; s++) b[d * TAG + s] = ferienfaktor * mittel[s];

            bool vollstaendig = true;
            for (int m = 0; m < MONATE; m++)
            {
                double summeA = 0.0, summeB = 0.0;
                for (int h = moAnfang[m]; h <= moEnde[m]; h++)
                {
                    summeA = summeA + a[h];
                    summeB = summeB + b[h];
                }

                // Mit Kürzen misst der Monat an seinem Profil OHNE Ferien; ohne Kürzen verteilt er
                // um. Randfall: ohne Kürzen und ohne Stunde mit Bedarf rechnet der Monat ohne Ferien.
                double[] reihe;
                double nenner;
                if (ferienKuerzen) { reihe = b; nenner = summeA; }
                else if (summeB > 0) { reihe = b; nenner = summeB; }
                else { reihe = a; nenner = summeA; vollstaendig = false; }

                for (int h = moAnfang[m]; h <= moEnde[m]; h++)
                    outJahr[h] = nenner > 0 ? reihe[h] / nenner * monatsverbrauch[m] * 1000.0 : 0.0;
            }
            return vollstaendig;
        }
    }
}

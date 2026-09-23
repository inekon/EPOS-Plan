using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Zeitbezug der Sonnengeometrie je Klimastunde (Frage U6, entschieden mit E29:
    /// Stundenanfang; Rechenschritte E1, Umsetzungskonzept 1.2). Die Stundenmitte bleibt als
    /// Messschalter.
    /// </summary>
    internal enum Zeitbezug
    {
        /// <summary>Sonnenstand zum Stundenanfang der UTC-Stunde — die Konvention des Klimaimports, von PV und Solarthermie.</summary>
        Stundenanfang,

        /// <summary>Sonnenstand zur Stundenmitte (eine halbe Stunde später, 7,5° Stundenwinkel) — die Konvention von Blatt 3.</summary>
        Stundenmitte,
    }

    /// <summary>
    /// Die Einstrahlung auf die vier senkrechten Fassaden je Stunde [W/m²], Ortszeit.
    /// </summary>
    internal sealed class Fassadenstrahlung
    {
        internal Fassadenstrahlung(double[] sued, double[] ost, double[] west, double[] nord)
        {
            Sued = sued; Ost = ost; West = west; Nord = nord;
        }

        /// <summary>Südfassade (Azimut 0°) [W/m²].</summary>
        internal double[] Sued { get; }

        /// <summary>Ostfassade (Azimut −90°) [W/m²].</summary>
        internal double[] Ost { get; }

        /// <summary>Westfassade (Azimut +90°) [W/m²].</summary>
        internal double[] West { get; }

        /// <summary>Nordfassade (Azimut 180°) [W/m²].</summary>
        internal double[] Nord { get; }
    }

    /// <summary>
    /// <b>Der Klimaweg des Gebäudemodells</b> (Stufe G1; Entscheid A18: eigene Klasse,
    /// <b>ausschließlich</b> vom Eingangsbauer <see cref="GebaeudeModellEingang.Bauen"/>
    /// gerufen). Hier — und nur hier — stehen die vier Entscheidungen des Klimawegs
    /// (Umsetzungskonzept 1.4): der <b>Zeitbezug</b> der Sonnengeometrie (U6, entschieden mit
    /// E29: <see cref="ZEITBEZUG_VORGABE"/> = Stundenanfang; <see cref="Zeitbezug"/> bleibt als
    /// Messschalter), die
    /// <b>Azimutzuordnung</b> der vier Fensterrichtungen, die <b>Erdreichtemperatur</b> und
    /// die Regel für die <b>Gegenstrahlung</b>.
    ///
    /// <para><b>Sonnenstand auf UTC, Bilanz auf Ortszeit</b> (Rechenschritte E1): Die Zeilen
    /// kommen in Ortszeit, jede trägt ihre UTC-Herkunft (<c>TagUtc</c>, <c>StundeUtc</c>);
    /// die Transposition nach Hay-Davies rechnet mit dieser Herkunft — derselbe Sonnenstand
    /// wie PV und Solarthermie. Die isotrope Bestandsfunktion mit ihren statischen Feldern
    /// wird nicht benutzt (Umsetzungskonzept 1.2).</para>
    ///
    /// <para><b>Gegenstrahlung</b> (E5, Rechenschritte 1.2): NULL heißt „nicht verfügbar",
    /// dann gilt Δθ_lw = 0 und α_str,A = 5,0 W/(m²K). In Stufe G1 ist der langwellige Term
    /// auch bei vorhandener Gegenstrahlung nicht rechenwirksam — die Normformel füllt den
    /// Schalter <c>Aussenbauteile_Strahlung</c> erst mit G2 (Rechenschritte E5); die Zahl der
    /// Stunden mit Gegenstrahlung wird deshalb ausgewiesen (<see cref="StundenMitGegenstrahlung"/>).</para>
    ///
    /// <para>Reine Umrechnung: ohne Datenbank, ohne Protokoll, ohne Zustand, durchgehend
    /// <c>double</c>.</para>
    /// </summary>
    internal static class GebaeudeKlimaweg
    {
        /// <summary>
        /// Der Zeitbezug des Auslieferungswegs — <b>entschieden: Stundenanfang</b> (U6, Entscheid
        /// E29 vom 23.09.2026, Konzept N1.34), dieselbe Konvention wie Photovoltaik und
        /// Solarthermie. Gemessen in G1 verschiebt die Stundenmitte die Fassadenstrahlung Ost um
        /// −10,1 %, West um +10,5 % und die Jahresheizwärme um höchstens +0,10 %. Umgestellt wird
        /// nur für Gebäude, PV und Solarthermie gemeinsam; der Parameter <c>zeitbezug</c> des
        /// Eingangsbauers bleibt für Messungen und Tests.
        /// </summary>
        internal const Zeitbezug ZEITBEZUG_VORGABE = Zeitbezug.Stundenanfang;

        /// <summary>Neigung einer Fassade [°].</summary>
        internal const int NEIGUNG_FASSADE = 90;

        /// <summary>Azimut Süd [°] — Konvention des Eingangsbauers: Grad gegen Süd, Ost negativ.</summary>
        internal const int AZIMUT_SUED = 0;

        /// <summary>Azimut Ost [°].</summary>
        internal const int AZIMUT_OST = -90;

        /// <summary>Azimut West [°].</summary>
        internal const int AZIMUT_WEST = 90;

        /// <summary>Azimut Nord [°].</summary>
        internal const int AZIMUT_NORD = 180;

        /// <summary>Die Stunde der Sonnengeometrie einer Zeile [h, UTC].</summary>
        internal static double Stunde(SolardatenModel zeile, Zeitbezug bezug)
        {
            return zeile.StundeUtc + (bezug == Zeitbezug.Stundenmitte ? 0.5 : 0.0);
        }

        /// <summary>
        /// Prüft die Klimazeilen: genau 8 760 Stunden, jede mit UTC-Herkunft und endlichen
        /// Werten, Koordinaten endlich.
        /// </summary>
        /// <exception cref="GebaeudeModellException"><see cref="GebaeudeModellFehler.KlimadatenUnvollstaendig"/>.</exception>
        internal static void Pruefen(IReadOnlyList<SolardatenModel> zeilen, double laengengrad, double breitengrad)
        {
            if (zeilen == null || zeilen.Count != 8760)
                throw new GebaeudeModellException(GebaeudeModellFehler.KlimadatenUnvollstaendig,
                    "Die Klimareihe führt " + (zeilen == null ? 0 : zeilen.Count).ToString(CultureInfo.InvariantCulture) +
                    " statt 8760 Stunden in Ortszeit; das Stundenmodell rechnet nur auf dem vollen Jahresraster.");
            if (!Endlich(laengengrad) || !Endlich(breitengrad))
                throw new GebaeudeModellException(GebaeudeModellFehler.KlimadatenUnvollstaendig,
                    "Längen- oder Breitengrad der Klimaregion fehlt; ohne sie gibt es keinen Sonnenstand.");
            for (int h = 0; h < 8760; h++)
            {
                SolardatenModel z = zeilen[h];
                if (z == null || z.TagUtc < 1 || z.TagUtc > 365 || z.StundeUtc < 0 || z.StundeUtc > 23
                    || !Endlich(z.Außen_Temp) || !Endlich(z.Globalstrahlung)
                    || !Endlich(z.Direktstrahlung) || !Endlich(z.Diffusstrahlung))
                    throw new GebaeudeModellException(GebaeudeModellFehler.KlimadatenUnvollstaendig,
                        "Die Klimastunde " + h.ToString(CultureInfo.InvariantCulture) +
                        " ist unvollständig (UTC-Herkunft oder ein Wert fehlt).");
            }
        }

        /// <summary>Die Außenlufttemperatur je Stunde [°C], Ortszeit.</summary>
        internal static double[] Aussentemperatur(IReadOnlyList<SolardatenModel> zeilen)
        {
            var t = new double[8760];
            for (int h = 0; h < 8760; h++) t[h] = zeilen[h].Außen_Temp;
            return t;
        }

        /// <summary>
        /// Die Einstrahlung auf die vier Fassaden nach Hay-Davies (Rechenschritte E2), mit
        /// Sonnenstand auf der UTC-Herkunft jeder Zeile und dem gewählten Zeitbezug.
        /// </summary>
        internal static Fassadenstrahlung Fassaden(IReadOnlyList<SolardatenModel> zeilen,
                                                   double laengengrad, double breitengrad, Zeitbezug bezug)
        {
            var s = new double[8760];
            var o = new double[8760];
            var w = new double[8760];
            var n = new double[8760];
            for (int h = 0; h < 8760; h++)
            {
                SolardatenModel z = zeilen[h];
                double stunde = Stunde(z, bezug);
                s[h] = Flaeche(z, laengengrad, breitengrad, AZIMUT_SUED, stunde);
                o[h] = Flaeche(z, laengengrad, breitengrad, AZIMUT_OST, stunde);
                w[h] = Flaeche(z, laengengrad, breitengrad, AZIMUT_WEST, stunde);
                n[h] = Flaeche(z, laengengrad, breitengrad, AZIMUT_NORD, stunde);
            }
            return new Fassadenstrahlung(s, o, w, n);
        }

        private static double Flaeche(SolardatenModel z, double lon, double lat, int azimut, double stunde)
        {
            double i = SolarCalculator.CalculateHourlyHayDavies(lon, lat, NEIGUNG_FASSADE, azimut,
                z.Globalstrahlung, z.Direktstrahlung, z.Diffusstrahlung, z.TagUtc, stunde);
            // Eine negative Transposition gibt es physikalisch nicht; sie entsteht nur aus
            // negativen Eingangswerten, die Pruefen bereits als endlich zulässt.
            return i > 0.0 ? i : 0.0;
        }

        /// <summary>
        /// Die Temperatur an der Grundfläche je Stunde [°C] nach der Randbedingung
        /// (Rechenschritte E6): Erdreich nach Kusuda in z = 1 m mit α = 0,06 m²/d, Keller mit
        /// fester Kellertemperatur, Außenluft.
        /// </summary>
        /// <param name="erdreichAusKlimadaten"><c>false</c>, wenn der Jahresgang der
        /// Erdreichrechnung auf Ersatzwerte zurückfiel — der Aufrufer meldet das.</param>
        internal static double[] Grundtemperatur(string randbedingung, double kellertemperatur,
                                                 double[] thetaOut, out bool erdreichAusKlimadaten)
        {
            erdreichAusKlimadaten = true;
            if (string.Equals(randbedingung, DbWerte.GRUND_AUSSENLUFT, StringComparison.Ordinal))
                return (double[])thetaOut.Clone();

            if (string.Equals(randbedingung, DbWerte.GRUND_KELLER, StringComparison.Ordinal))
            {
                var k = new double[8760];
                for (int h = 0; h < 8760; h++) k[h] = kellertemperatur;
                return k;
            }

            return ErdreichTemperatur.Jahresprofil(thetaOut, GebaeudeFestwerte.ERDREICH_TIEFE_M,
                GebaeudeFestwerte.ERDREICH_TEMPERATURLEITFAEHIGKEIT_M2D, out erdreichAusKlimadaten);
        }

        /// <summary>
        /// Der langwellige Term Δθ_lw einer Fläche [K] (Rechenschritte E5). NULL-Regel: ohne
        /// Gegenstrahlung 0. In Stufe G1 ist er stets 0 — die Normformel kommt mit G2.
        /// </summary>
        internal static double DeltaThetaLangwellig(double gegenstrahlungWm2)
        {
            return 0.0;
        }

        /// <summary>
        /// Der äußere Übergang α_A [W/(m²K)] (Gl. (38)): α_kon,A + α_str,A mit dem Rückfallwert
        /// α_str,A = 5,0, solange die Gegenstrahlung fehlt — und in G1 stets.
        /// </summary>
        internal static double AlphaAussen(double gegenstrahlungWm2)
        {
            return GebaeudeFestwerte.ALPHA_KON_AUSSEN + GebaeudeFestwerte.ALPHA_STR_AUSSEN_RUECKFALL;
        }

        /// <summary>Die Gegenstrahlung einer Zeile [W/m²]; NaN = nicht verfügbar (NULL).</summary>
        internal static double Gegenstrahlung(SolardatenModel zeile)
        {
            return zeile.Gegenstrahlung ?? double.NaN;
        }

        /// <summary>Zahl der Stunden, deren Zeile eine Gegenstrahlung führt.</summary>
        internal static int StundenMitGegenstrahlung(IReadOnlyList<SolardatenModel> zeilen)
        {
            int k = 0;
            for (int h = 0; h < zeilen.Count; h++) if (zeilen[h].Gegenstrahlung.HasValue) k++;
            return k;
        }

        private static bool Endlich(double w) => !double.IsNaN(w) && !double.IsInfinity(w);
    }
}

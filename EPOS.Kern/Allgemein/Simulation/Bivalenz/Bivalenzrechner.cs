#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Betriebsart des bivalenten Betriebs (Fachkonzept Übergabegrenze 4.5).</summary>
    internal enum Bivalenzbetriebsart
    {
        /// <summary>Parallel: Der Kessel ergänzt, kein Abschaltpunkt.</summary>
        Parallel = 0,

        /// <summary>Teilparallel: wie parallel, unter dem Abschaltpunkt nur der Kessel.</summary>
        Teilparallel = 1,

        /// <summary>Alternativ: Wärmepumpe oder Kessel; Vorwärmbetrieb nicht wählbar.</summary>
        Alternativ = 2,
    }

    /// <summary>
    /// Wie der eingegebene Abschaltpunkt gegen den berechneten Punkt steht (UB‑Q4,
    /// Umsetzungskonzept 1). Gilt als Konstante <see cref="Bivalenzrechner.REGEL"/>, kein Feld.
    /// </summary>
    internal enum AbschaltpunktRegel
    {
        /// <summary>a: Der eingegebene Abschaltpunkt bleibt Deckel, maßgebend ist der wärmere Wert.</summary>
        Deckel = 0,

        /// <summary>b: Der berechnete Punkt ersetzt den eingegebenen.</summary>
        Ersetzen = 1,

        /// <summary>c: Der berechnete Punkt wird nur angezeigt, maßgebend bleibt der eingegebene.</summary>
        NurAnzeigen = 2,
    }

    /// <summary>Ein Stützpunkt der Kennfeldgeraden bei θ_WP,max: Außen- bzw. Quelltemperatur und Leistung.</summary>
    internal readonly record struct Kennfeldpunkt(double AussenC, double Leistung);

    /// <summary>
    /// Die Kennfeldleistung der Wärmepumpe bei θ_WP,max über der Quelltemperatur: Stützpunkte,
    /// linear interpoliert, außerhalb gehalten.
    /// </summary>
    internal sealed class Kennfeldgerade
    {
        private readonly Kennfeldpunkt[] _punkte;

        internal Kennfeldgerade(IEnumerable<Kennfeldpunkt> punkte)
        {
            if (punkte == null) throw new ArgumentNullException(nameof(punkte));
            // OrderBy sortiert stabil: gleiche Außentemperaturen behalten ihre Reihenfolge.
            _punkte = punkte.OrderBy(p => p.AussenC).ToArray();
            if (_punkte.Length == 0) throw new ArgumentException("Die Kennfeldgerade braucht mindestens einen Stützpunkt.", nameof(punkte));
        }

        /// <summary>Die Stützpunkte, aufsteigend nach der Außentemperatur.</summary>
        internal IReadOnlyList<Kennfeldpunkt> Punkte => _punkte;

        /// <summary>Kennfeldleistung bei Außentemperatur <paramref name="aussenC"/>.</summary>
        internal double Leistung(double aussenC)
        {
            if (aussenC <= _punkte[0].AussenC) return _punkte[0].Leistung;
            int letzter = _punkte.Length - 1;
            if (aussenC >= _punkte[letzter].AussenC) return _punkte[letzter].Leistung;
            for (int i = 1; i <= letzter; i++)
            {
                Kennfeldpunkt b = _punkte[i];
                if (aussenC > b.AussenC) continue;
                Kennfeldpunkt a = _punkte[i - 1];
                double dx = b.AussenC - a.AussenC;
                if (!(dx > 0.0)) return b.Leistung;
                return a.Leistung + (b.Leistung - a.Leistung) * (aussenC - a.AussenC) / dx;
            }
            return _punkte[letzter].Leistung;
        }
    }

    /// <summary>Die berechneten Bivalenzpunkte (Fachkonzept Übergabegrenze 4.5). NaN = kein Punkt im Auslegungsbereich.</summary>
    /// <param name="ErsterC">θ_biv,1 [°C] — Kennfeld und Übergabe.</param>
    /// <param name="KennfeldAlleinC">Bivalenzpunkt nach Kennfeld allein, ohne Übergabe [°C].</param>
    /// <param name="ZweiterC">θ_biv,2 [°C] — mit Vorwärmbetrieb die Grenze der Mindestspreizung, sonst θ_biv,1.</param>
    /// <param name="UebergabeBegrenzt">Begrenzt am ersten Bivalenzpunkt die Übergabe (nicht das Kennfeld)?</param>
    /// <param name="AbschaltpunktC">Eingegebener Abschaltpunkt [°C], sofern er nach der Betriebsart gilt; sonst null.</param>
    /// <param name="MassgebendC">Maßgebender Punkt nach <see cref="Bivalenzrechner.Massgebend"/> [°C].</param>
    internal readonly record struct Bivalenzpunkte(double ErsterC, double KennfeldAlleinC, double ZweiterC,
                                                   bool UebergabeBegrenzt, double? AbschaltpunktC, double MassgebendC);

    /// <summary>
    /// <b>Die Bivalenzpunkte, statisch</b> (Fachkonzept Übergabegrenze 4.5; Umsetzungskonzept 3.1,
    /// in UB‑E1 nur <see cref="Punkte"/> und <see cref="Massgebend"/>). Lastlineare Heizlast
    /// Φ(θ_a) = Φ_N·φ(θ_a), φ = (θ_i − θ_a)/(θ_i − θ_a,N); Kennfeldgerade bei θ_WP,max über der
    /// Quelltemperatur; Heizkurve θ_R,soll = θ_i + Δθ_m,N·φ^(1/n) − σ_N·φ/2 (Anlagenkopplung 3.4,
    /// Niveau 0, Steilheit 1). Gesucht wird zwischen Auslegungs-Außentemperatur und
    /// Raumtemperatur: Abtastung von oben in festen Schritten, dann Bisektion — deterministisch.
    /// </summary>
    internal static class Bivalenzrechner
    {
        /// <summary>Die Regel gegen den eingegebenen Abschaltpunkt (UB‑Q4 a, entschieden 08.10.2026).</summary>
        internal const AbschaltpunktRegel REGEL = AbschaltpunktRegel.Deckel;

        /// <summary>Schrittweite der Abtastung [K].</summary>
        internal const double ABTASTUNG_K = 0.25;

        /// <summary>Bisektionsschritte nach der Abtastung (0,25 K / 2^50 ≪ 1e-9 K).</summary>
        internal const int BISEKTION_SCHRITTE = 50;

        /// <summary>Heizlast der Stunde, lastlinear: Φ_N·(θ_i − θ_a)/(θ_i − θ_a,N), nicht unter 0.</summary>
        internal static double Heizlast(double heizlastN, double auslegungAussenC, double raumC, double aussenC)
        {
            double phi = (raumC - aussenC) / (raumC - auslegungAussenC);
            return phi > 0.0 ? heizlastN * phi : 0.0;
        }

        /// <summary>
        /// Die Bivalenzpunkte. <paramref name="phiUeMax"/> ist die Übergabegrenze bei θ_WP,max und
        /// Auslegungsraumtemperatur (+∞ oder NaN: ohne Übergabe). <paramref name="heizkurve"/> ist die
        /// Übergabe, aus deren Auslegungspunkt die Heizkurve folgt; null → θ_biv,2 = θ_biv,1.
        /// Bei alternativ ist der Vorwärmbetrieb nicht wählbar und rechnet als aus; der eingegebene
        /// Abschaltpunkt gilt nur bei teilparallel und alternativ.
        /// </summary>
        internal static Bivalenzpunkte Punkte(double heizlastN, double auslegungAussenC, double raumC,
                                              double phiUeMax, Kennfeldgerade kennfeld, Uebergabezone? heizkurve,
                                              double hoechstvorlaufC, double spreizungMinK, bool vorwaermbetrieb,
                                              Bivalenzbetriebsart betriebsart, double? abschaltpunktC)
        {
            if (kennfeld == null) throw new ArgumentNullException(nameof(kennfeld));
            if (!(raumC > auslegungAussenC))
                throw new ArgumentOutOfRangeException(nameof(auslegungAussenC), "Die Auslegungs-Außentemperatur muss unter der Raumtemperatur liegen.");
            double ue = double.IsNaN(phiUeMax) ? double.PositiveInfinity : phiUeMax;

            double kennfeldAllein = HoechsteAussentemperatur(
                ta => Heizlast(heizlastN, auslegungAussenC, raumC, ta) > kennfeld.Leistung(ta),
                auslegungAussenC, raumC);
            double erster = HoechsteAussentemperatur(
                ta => Heizlast(heizlastN, auslegungAussenC, raumC, ta) > Math.Min(ue, kennfeld.Leistung(ta)),
                auslegungAussenC, raumC);
            bool uebergabeBegrenzt = !double.IsNaN(erster) && ue < kennfeld.Leistung(erster);

            bool vorwaermen = vorwaermbetrieb && betriebsart != Bivalenzbetriebsart.Alternativ;
            double zweiter = erster;
            if (vorwaermen && heizkurve != null)
            {
                var kurve = new Heizkurve(new Uebergabekennwerte(heizkurve.PhiN, heizkurve.Exponent,
                    heizkurve.AuslegungVorlaufC, heizkurve.AuslegungRuecklaufC, heizkurve.AuslegungRaumC),
                    auslegungAussenC, 0.0, 1.0);
                double schwelle = hoechstvorlaufC - spreizungMinK;
                zweiter = HoechsteAussentemperatur(ta => kurve.RuecklaufSollC(raumC, ta) >= schwelle,
                                                   auslegungAussenC, raumC);
                // Der zweite Punkt liegt nie über dem ersten: Über θ_biv,1 deckt die Wärmepumpe allein.
                if (!double.IsNaN(erster) && !double.IsNaN(zweiter) && zweiter > erster) zweiter = erster;
            }

            double? deckel = betriebsart == Bivalenzbetriebsart.Parallel ? null : abschaltpunktC;
            return new Bivalenzpunkte(erster, kennfeldAllein, zweiter, uebergabeBegrenzt, deckel,
                                      Massgebend(deckel, zweiter));
        }

        /// <summary>
        /// Der maßgebende Abschaltpunkt nach <see cref="REGEL"/> (UB‑Q4 a): Der eingegebene Punkt
        /// bleibt Deckel, maßgebend ist der wärmere Wert. Fehlt einer, gilt der andere; fehlen
        /// beide, NaN.
        /// </summary>
        internal static double Massgebend(double? eingegebenerAbschaltpunktC, double berechneterPunktC)
            => Massgebend(eingegebenerAbschaltpunktC, berechneterPunktC, REGEL);

        /// <summary>Wie <see cref="Massgebend(double?, double)"/> mit ausdrücklicher Regel (Gegenoptionen b, c).</summary>
        internal static double Massgebend(double? eingegebenerAbschaltpunktC, double berechneterPunktC, AbschaltpunktRegel regel)
        {
            double eingegeben = eingegebenerAbschaltpunktC ?? double.NaN;
            switch (regel)
            {
                case AbschaltpunktRegel.Ersetzen:
                    return double.IsNaN(berechneterPunktC) ? eingegeben : berechneterPunktC;
                case AbschaltpunktRegel.NurAnzeigen:
                    return eingegeben;
                default:
                    if (double.IsNaN(eingegeben)) return berechneterPunktC;
                    if (double.IsNaN(berechneterPunktC)) return eingegeben;
                    return Math.Max(eingegeben, berechneterPunktC);
            }
        }

        /// <summary>
        /// Die höchste Außentemperatur in [unten, oben], an der <paramref name="bedingung"/> gilt,
        /// unter der Annahme, dass sie unterhalb davon gilt (monotoner Fall). Abtastung von oben,
        /// dann Bisektion; NaN, wenn sie nirgends gilt.
        /// </summary>
        internal static double HoechsteAussentemperatur(Func<double, bool> bedingung, double unten, double oben)
        {
            if (bedingung(oben)) return oben;
            int n = (int)Math.Ceiling((oben - unten) / ABTASTUNG_K);
            double vorher = oben;
            for (int i = 1; i <= n; i++)
            {
                double ta = Math.Max(unten, oben - i * ABTASTUNG_K);
                if (bedingung(ta))
                {
                    double gilt = ta, giltNicht = vorher;
                    for (int k = 0; k < BISEKTION_SCHRITTE; k++)
                    {
                        double m = 0.5 * (gilt + giltNicht);
                        if (bedingung(m)) gilt = m; else giltNicht = m;
                    }
                    return 0.5 * (gilt + giltNicht);
                }
                vorher = ta;
            }
            return double.NaN;
        }
    }
}

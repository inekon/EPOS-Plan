using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Form, an der die Stufenformel eine Rampe bemisst (Entwurf KP3 Abschnitt 2 Nr. 2, Befund B2,
    /// Entscheid E58 F1 (b)).
    /// </summary>
    internal enum Aufheizform
    {
        /// <summary>Das Stundenmittel der Sprungstunde Φ̄_n — die Form der Zielleistung.</summary>
        Stundenmittel,

        /// <summary>
        /// Der Augenblickswert am Beginn der Sprungstunde Φ̂_n — die Form der Grenze
        /// <c>Heizleistung_Max</c>: Der Löser kappt am Augenblickswert (Befund B2).
        /// </summary>
        Augenblick,
    }

    /// <summary>
    /// Das Ergebnis der Wahl von n (<see cref="Aufheizstufen.Waehlen"/>): die kleinste haltende
    /// Stufenzahl samt ihrer Leistung — oder „unerreichbar".
    /// </summary>
    internal readonly struct Aufheizwahl
    {
        internal Aufheizwahl(int n, bool erreichbar, double leistungW)
        {
            N = n;
            Erreichbar = erreichbar;
            LeistungW = leistungW;
        }

        /// <summary>Die kleinste haltende Stufenzahl n ≥ 1; 0, wenn kein n ≤ n_max hält.</summary>
        internal int N { get; }

        /// <summary>Hält ein n ≤ n_max die Aufheizleistung? Sonst ist der Sprung unerreichbar (W1 rechnet R2).</summary>
        internal bool Erreichbar { get; }

        /// <summary>Die Leistung der gewählten Form bei <see cref="N"/> [W]; unerreichbar: bei n_max.</summary>
        internal double LeistungW { get; }
    }

    /// <summary>
    /// <b>Die Stufenformel der Aufheizoptimierung als reine Funktionen</b> (Entwurf KP3 Abschnitt 2
    /// Nr. 2, Festlegungen 5 und 15; Teilkonzept Konditionierungsprofile 4.3, 4.5). Die Rampe mit n
    /// Stufen ist eine Treppe aus n Sprüngen ΔT/n im Stundenabstand; die letzte Stufe fällt in die
    /// Sprungstunde (4.1). Bei festen Rändern folgt aus der <see cref="Aufheizantwort"/> mit
    /// h = 3 600 s:
    /// <list type="bullet">
    /// <item><b>Stundenmittel</b> der Sprungstunde aus einem Gleichgewicht bei θ_N (Gleichgewichtsform):
    /// Φ̄_n = Φ_stat(θ_T) + ΔT/(n·h) · z·Γ(n·h)·v.</item>
    /// <item><b>Augenblick</b> am Beginn der Sprungstunde, das Maximum der Leistung über die Rampe:
    /// Φ̂_n = Φ_stat(θ_T) + ΔT/n · z·(Σ_{m=0…n−1} Φ(h)^m)·v.</item>
    /// <item><b>Absenkform</b> — vorher eingeschwungen bei θ_T, D Stunden auf θ_N vor der Sprungstunde:
    /// Φ̄_n − ΔT/h · z·(Γ((D+1)·h) − Γ(D·h))·v; geschlossen nur, solange die Heizung in der Absenkung
    /// geregelt bleibt (Φ_stat(θ_T) &gt; G_0·ΔT, Befund B12).</item>
    /// <item><b>Erste Ordnung</b> als Schranke: n_F = ⌈C_w·ΔT/(h·(P − Φ_stat))⌉ (Mittel) und
    /// ⌈(C_w/h + G_0 − H_s)·ΔT/(P − Φ_stat)⌉ (Augenblick).</item>
    /// </list>
    ///
    /// <para><b>Matrixfunktionen statt Modalsummen</b> (Festlegung 5): Φ und Γ kommen aus
    /// <see cref="Aufheizantwort.Bei"/>, also aus <see cref="Uebergangsrechner.Bei"/> über die Naht
    /// <see cref="Plattformrundung"/> — mit dem Zweig für zusammenfallende Eigenwerte wie im Löser.
    /// <b>Jede Entscheidung</b> (die Wahl von n) vergleicht über <see cref="Rechenrand"/> (Grundsatz 6).
    /// Keine Kultur, kein Zufall, keine Datenbank. Φ_stat(θ_T, T_a) liefert der Aufrufer
    /// (<see cref="Zonenmodell2K.StationaereHeizlastW"/>); die Planung je Tag folgt mit der
    /// <c>Aufheizoptimierung</c> (Teilkonzept 6).</para>
    /// </summary>
    internal static class Aufheizstufen
    {
        /// <summary>Länge einer Stufe h [s].</summary>
        internal const double STUNDE_S = Zonenmodell2K.STUNDE_S;

        /// <summary>
        /// Die Form der Bemessung zur Quelle der Aufheizleistung (Entscheid E58 F1 (b)): Grenze
        /// (<c>Heizleistung_Max</c>) → Augenblick, Zielleistung → Stundenmittel.
        /// </summary>
        internal static Aufheizform FormZurQuelle(bool quelleGrenze)
            => quelleGrenze ? Aufheizform.Augenblick : Aufheizform.Stundenmittel;

        /// <summary>Das Stundenmittel der Sprungstunde Φ̄_n [W] (Gleichgewichtsform).</summary>
        internal static double StundenmittelW(Aufheizantwort antwort, double phiStatW, double deltaTK, int n)
        {
            Pruefen(antwort, phiStatW, deltaTK, n);
            Uebergang u = antwort.Bei(n * STUNDE_S);
            return phiStatW + deltaTK / (n * STUNDE_S) * antwort.Ausgang(u.Gamma);
        }

        /// <summary>Der Augenblickswert am Beginn der Sprungstunde Φ̂_n [W] (Gleichgewichtsform).</summary>
        internal static double AugenblickW(Aufheizantwort antwort, double phiStatW, double deltaTK, int n)
        {
            Pruefen(antwort, phiStatW, deltaTK, n);
            Matrix2 summe = Matrix2.Einheit, potenz = Matrix2.Einheit;
            Matrix2 phi = antwort.Stunde.Phi;
            for (int m = 1; m < n; m++)
            {
                potenz = potenz * phi;
                summe = summe + potenz;
            }
            return Augenblick(antwort, phiStatW, deltaTK, n, summe);
        }

        /// <summary>
        /// Das Stundenmittel der Sprungstunde in der <b>Absenkform</b> [W]: eingeschwungen bei θ_T,
        /// dann <paramref name="d"/> Stunden auf θ_N vor der Sprungstunde, die letzten n − 1 davon als
        /// Rampe. Nie über der Gleichgewichtsform. Geschlossen nur mit
        /// <see cref="AbsenkformGeschlossen"/> — sonst läge die Absenkung zeitweise im Totband.
        /// </summary>
        /// <param name="d">Die Absenkdauer D [h], D ≥ n − 1.</param>
        /// <exception cref="ArgumentOutOfRangeException">bei D &lt; n − 1.</exception>
        internal static double AbsenkformW(Aufheizantwort antwort, double phiStatW, double deltaTK, int n, int d)
        {
            Pruefen(antwort, phiStatW, deltaTK, n);
            if (d < n - 1)
                throw new ArgumentOutOfRangeException(nameof(d), d,
                    "Die Absenkdauer D muss mindestens n − 1 = " + (n - 1).ToString(CultureInfo.InvariantCulture) + " Stunden betragen.");
            double mittel = StundenmittelW(antwort, phiStatW, deltaTK, n);
            Matrix2 nach = antwort.Bei((d + 1) * STUNDE_S).Gamma;
            Matrix2 vor = d == 0 ? new Matrix2(0.0, 0.0, 0.0, 0.0) : antwort.Bei(d * STUNDE_S).Gamma;
            return mittel - deltaTK / STUNDE_S * antwort.Ausgang(nach - vor);
        }

        /// <summary>
        /// Bleibt die Heizung in der Absenkung geregelt (Befund B12)? Die Leistung fällt beim Sprung
        /// nach unten um G_0·ΔT unter Φ_stat(θ_T) und steigt danach; nur wenn sie positiv bleibt, gilt
        /// <see cref="AbsenkformW"/> geschlossen.
        /// </summary>
        internal static bool AbsenkformGeschlossen(Aufheizantwort antwort, double phiStatTW, double deltaTK)
        {
            if (antwort == null) throw new ArgumentNullException(nameof(antwort));
            return phiStatTW - antwort.G0WK * deltaTK > Rechenrand.Zu(0.0);
        }

        /// <summary>
        /// Die erste Ordnung als Schranke der Mittelform: n_F = ⌈C_w·ΔT/(h·(P − Φ_stat))⌉, mindestens 1;
        /// +∞, wenn P ≤ Φ_stat. Herleitungszeile und Probenband — bemessen wird mit ihr nicht (4.3).
        /// </summary>
        internal static double ErsteOrdnungMittel(Aufheizantwort antwort, double phiStatW, double deltaTK, double pAufW)
        {
            Pruefen(antwort, phiStatW, deltaTK, 1);
            double reserve = pAufW - phiStatW;
            if (!(reserve > 0.0)) return double.PositiveInfinity;
            return Math.Max(1.0, Math.Ceiling(antwort.CwJk * deltaTK / (STUNDE_S * reserve)));
        }

        /// <summary>
        /// Die erste Ordnung als Schranke der Augenblicksform: n_F = ⌈(C_w/h + G_0 − H_s)·ΔT/(P − Φ_stat)⌉,
        /// mindestens 1; +∞, wenn P ≤ Φ_stat. Der Summand G_0 − H_s ist der Sprung am Beginn jeder Stufe.
        /// </summary>
        internal static double ErsteOrdnungAugenblick(Aufheizantwort antwort, double phiStatW, double deltaTK, double pAufW)
        {
            Pruefen(antwort, phiStatW, deltaTK, 1);
            double reserve = pAufW - phiStatW;
            if (!(reserve > 0.0)) return double.PositiveInfinity;
            return Math.Max(1.0, Math.Ceiling((antwort.CwJk / STUNDE_S + antwort.G0WK - antwort.HsWK) * deltaTK / reserve));
        }

        /// <summary>
        /// <b>Die Wahl von n</b> (Entwurf KP3 Abschnitt 2 Nr. 2 und 6, Teilkonzept 4.3, 4.6): das kleinste
        /// n ≥ 1 mit Φ_form(n) − P_auf ≤ <see cref="Rechenrand.Zu"/>(P_auf), n ≤ n_max; sonst
        /// „unerreichbar". P_auf = +∞ hält mit n = 1 (Grenzfall N-AH8). Den Hinweis W1 und die
        /// Begrenzung durch die Absenkdauer rechnet die Planung (R2).
        /// </summary>
        /// <param name="pAufW">Die Aufheizleistung P_auf [W]; nicht NaN, +∞ erlaubt.</param>
        /// <param name="form">Mittel oder Augenblick (<see cref="FormZurQuelle"/>).</param>
        /// <param name="nMax">Die größte zulässige Stufenzahl, ≥ 1.</param>
        internal static Aufheizwahl Waehlen(Aufheizantwort antwort, double phiStatW, double deltaTK, double pAufW,
                                            Aufheizform form, int nMax)
        {
            Pruefen(antwort, phiStatW, deltaTK, 1);
            if (double.IsNaN(pAufW))
                throw new ArgumentOutOfRangeException(nameof(pAufW), pAufW, "Die Aufheizleistung darf nicht NaN sein.");
            if (nMax < 1)
                throw new ArgumentOutOfRangeException(nameof(nMax), nMax, "Die größte Stufenzahl muss mindestens 1 sein.");
            if (form != Aufheizform.Stundenmittel && form != Aufheizform.Augenblick)
                throw new ArgumentOutOfRangeException(nameof(form), form, "Unbekannte Form der Stufenformel.");

            double rand = Rechenrand.Zu(pAufW);
            Matrix2 summe = Matrix2.Einheit, potenz = Matrix2.Einheit;
            Matrix2 phi = antwort.Stunde.Phi;
            double leistung = double.NaN;
            for (int n = 1; n <= nMax; n++)
            {
                if (form == Aufheizform.Augenblick)
                {
                    // Dieselbe Folge der Rechenschritte wie AugenblickW — bitgleich zu ihr.
                    if (n > 1)
                    {
                        potenz = potenz * phi;
                        summe = summe + potenz;
                    }
                    leistung = Augenblick(antwort, phiStatW, deltaTK, n, summe);
                }
                else
                {
                    leistung = StundenmittelW(antwort, phiStatW, deltaTK, n);
                }
                if (leistung - pAufW <= rand) return new Aufheizwahl(n, true, leistung);
            }
            return new Aufheizwahl(0, false, leistung);
        }

        private static double Augenblick(Aufheizantwort antwort, double phiStatW, double deltaTK, int n, Matrix2 summe)
            => phiStatW + deltaTK / n * antwort.Ausgang(summe);

        private static void Pruefen(Aufheizantwort antwort, double phiStatW, double deltaTK, int n)
        {
            if (antwort == null) throw new ArgumentNullException(nameof(antwort));
            if (double.IsNaN(phiStatW) || double.IsInfinity(phiStatW))
                throw new ArgumentOutOfRangeException(nameof(phiStatW), phiStatW, "Die stationäre Last muss endlich sein.");
            if (double.IsNaN(deltaTK) || double.IsInfinity(deltaTK) || deltaTK < 0.0)
                throw new ArgumentOutOfRangeException(nameof(deltaTK), deltaTK, "Der Sprung ΔT muss endlich und nicht negativ sein.");
            if (n < 1)
                throw new ArgumentOutOfRangeException(nameof(n), n, "Die Stufenzahl n muss mindestens 1 sein.");
        }
    }
}

using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Eingabearten Monatswerte und Wochenprofil eines Kältebedarfsprofils</b> (Konzept Kältebedarfsprofile 3.2,
    /// 3.3; Welle KBP-3a) — die reine Rechnung ohne Datenbank und ohne Oberfläche. Beide Arten enden in denselben
    /// Eingangsgrößen der unveränderten Kette <see cref="ProfilBedarf"/>: Jahressumme <c>Summe</c> [MWh] der Zuordnung,
    /// Monatsanteile <c>Monat_1…12</c> [%] und Wochenprofil <c>"1"…"168"</c> der Projektkopie. Index 0 des Wochenprofils
    /// ist Montag 0 Uhr (Kachelung <see cref="WPPlan.Core.BhkwPlan.StromWocheToJahr"/>, Montag = 0 … Sonntag = 6).
    /// </summary>
    public static class Kaelteprofileingabe
    {
        /// <summary>Monate des Rasters.</summary>
        public const int MONATE = 12;

        /// <summary>Stunden des Tages.</summary>
        public const int STUNDEN = 24;

        /// <summary>Stunden der Woche.</summary>
        public const int WOCHE = 168;

        /// <summary>Erster Samstag-Tagindex im Wochenprofil (Montag = 0).</summary>
        public const int SAMSTAG = 5;

        /// <summary>Sonntag-Tagindex im Wochenprofil.</summary>
        public const int SONNTAG = 6;

        /// <summary>Zulässige Abweichung der Prozentsumme von 100 [%-Punkte].</summary>
        public const double PROZENT_TOLERANZ = 0.01;

        // ---- Monatswerte -------------------------------------------------------------------------------------------

        /// <summary>Summe zwölf Werte; fehlende zählen 0.</summary>
        public static double Summe(double?[] werte)
        {
            double s = 0;
            if (werte == null) return 0;
            foreach (double? w in werte) s += w ?? 0;
            return s;
        }

        /// <summary>
        /// Prüft zwölf Monatswerte in MWh (<paramref name="prozent"/> = false) oder in Prozent: jeder Wert vorhanden,
        /// endlich und ≥ 0; in MWh mindestens ein Monat mit Bedarf, in Prozent die Summe 100 (± <see cref="PROZENT_TOLERANZ"/>).
        /// </summary>
        /// <returns><c>null</c> = gültig, sonst der Grund in der Oberflächensprache.</returns>
        public static string PruefeMonate(double?[] werte, bool prozent)
        {
            if (werte == null || werte.Length != MONATE) return MyResource.Resource.KBP_MSG_MONAT_NEGATIV;
            foreach (double? w in werte)
                if (w is not double x || double.IsNaN(x) || double.IsInfinity(x) || x < 0)
                    return MyResource.Resource.KBP_MSG_MONAT_NEGATIV;
            double s = Summe(werte);
            if (prozent)
                return Math.Abs(s - 100.0) <= PROZENT_TOLERANZ
                    ? null
                    : string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KBP_MSG_SUMME_PROZENT,
                                    s.ToString("0.##", CultureInfo.CurrentCulture));
            return s > 0 ? null : MyResource.Resource.KBP_MSG_MONATE_NULL;
        }

        /// <summary>
        /// Monatswerte [MWh] → Jahressumme [MWh] und Monatsanteile [%]: <c>Summe = Σ M_m</c>, <c>a_m = 100 · M_m / Summe</c>
        /// (Konzept 3.2). Ein Monat mit 0 MWh bleibt 0. Ohne Bedarf (Summe 0) sind alle Anteile 0.
        /// </summary>
        public static (double SummeMwh, double[] Anteile) AusMwh(double?[] mwh)
        {
            double summe = Summe(mwh);
            var anteile = new double[MONATE];
            if (summe > 0)
                for (int m = 0; m < MONATE; m++) anteile[m] = 100.0 * (mwh[m] ?? 0) / summe;
            return (summe, anteile);
        }

        /// <summary>Monatsanteile [%] · Jahressumme [MWh] → Monatswerte [MWh] (Vorbelegung, Konzept 3.2).</summary>
        public static double?[] NachMwh(double[] anteile, double summeMwh)
        {
            var w = new double?[MONATE];
            for (int m = 0; m < MONATE; m++) w[m] = (anteile != null && m < anteile.Length ? anteile[m] : 0) * summeMwh / 100.0;
            return w;
        }

        // ---- Wochenprofil ------------------------------------------------------------------------------------------

        /// <summary>Sind Montag bis Freitag des Wochenprofils gleich (Bedingung der Dreitagesansicht, Konzept 3.3)?</summary>
        public static bool WerktageGleich(double[] woche)
        {
            if (woche == null || woche.Length != WOCHE) return false;
            for (int d = 1; d < SAMSTAG; d++)
                for (int s = 0; s < STUNDEN; s++)
                    if (woche[d * STUNDEN + s] != woche[s]) return false;
            return true;
        }

        /// <summary>Die 24 Stunden eines Wochentags (Montag = 0 … Sonntag = 6) als Kopie.</summary>
        public static double[] Tag(double[] woche, int tag)
        {
            var t = new double[STUNDEN];
            if (woche != null) Array.Copy(woche, tag * STUNDEN, t, 0, STUNDEN);
            return t;
        }

        /// <summary>Schreibt die 24 Stunden <paramref name="stunden"/> in den Wochentag <paramref name="tag"/>.</summary>
        public static void TagSetzen(double[] woche, int tag, double[] stunden)
        {
            if (woche == null || stunden == null) return;
            Array.Copy(stunden, 0, woche, tag * STUNDEN, Math.Min(STUNDEN, stunden.Length));
        }

        /// <summary>
        /// Dreitagesansicht → 168 Stunden: Werktag auf Montag bis Freitag, Samstag, Sonntag (Konzept 3.3, „Speichern
        /// schreibt Montag bis Freitag gleich“).
        /// </summary>
        public static double[] WocheAusTagen(double[] werktag, double[] samstag, double[] sonntag)
        {
            var w = new double[WOCHE];
            for (int d = 0; d < SAMSTAG; d++) TagSetzen(w, d, werktag);
            TagSetzen(w, SAMSTAG, samstag);
            TagSetzen(w, SONNTAG, sonntag);
            return w;
        }

        /// <summary>Prüft ein Wochenprofil: 168 endliche Werte ≥ 0 und eine positive Summe (sonst NaN in der Normierung).</summary>
        public static string PruefeWoche(double[] woche)
        {
            if (woche == null || woche.Length != WOCHE) return MyResource.Resource.KBP_MSG_WOCHE;
            double s = 0;
            foreach (double x in woche)
            {
                if (double.IsNaN(x) || double.IsInfinity(x) || x < 0) return MyResource.Resource.KBP_MSG_WOCHE;
                s += x;
            }
            return s > 0 ? null : MyResource.Resource.KBP_MSG_WOCHE;
        }
    }
}

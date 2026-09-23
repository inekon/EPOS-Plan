using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE E7c2 (E7c1‑Q7, Mockup U22 „Wirkung Jahr 1") — der Jahresbetrag des
    /// KWK-Zuschlags einer Anlage, ausgelagert aus dem Schleifenrumpf von
    /// <c>WirtschaftlichkeitCtrl.ReiheJeAnlage</c>. Der Lauf ruft ihn Jahr für Jahr, die
    /// Überlagerung „Sätze und Herkunft" für das erste Jahr — dieselbe Rechnung, ein
    /// zweites Mal GERUFEN, nicht abgeschrieben (Kern-Regel „Eine Auskunft ruft den
    /// Rechenweg des Laufs"). Die Ausdrücke stehen Zeichen für Zeichen, wie sie in der
    /// Schleife standen; die Reihe bleibt bitgleich.
    /// </summary>
    public static class KwkgJahresbetrag
    {
        /// <summary>
        /// Die Rückfallstaffel des Jahresdeckels nach § 8 Abs. 4 KWKG (JahrVon, h/a), wenn
        /// der Gesetzeskatalog keine Reihe <c>KWKG_VBH_JAHRESDECKEL</c> führt.
        /// </summary>
        public static readonly IReadOnlyList<KeyValuePair<int, double>> STAFFEL_RUECKFALL =
            new List<KeyValuePair<int, double>>
            {
                new KeyValuePair<int, double>(2020, 5000), new KeyValuePair<int, double>(2023, 4000),
                new KeyValuePair<int, double>(2025, 3500), new KeyValuePair<int, double>(2026, 3300),
                new KeyValuePair<int, double>(2027, 3100), new KeyValuePair<int, double>(2028, 2900),
                new KeyValuePair<int, double>(2029, 2700), new KeyValuePair<int, double>(2030, 2500)
            };

        /// <summary>Zuschlag eines vollen Jahres ohne Deckel [€]: Mengen [MWh] × Sätze [ct/kWh].</summary>
        public static double Voll(double eigenMWh, double satzEigenCt, double einspMWh, double satzEinspCt)
        {
            return eigenMWh * 1000.0 * (satzEigenCt / 100.0)
                 + einspMWh * 1000.0 * (satzEinspCt / 100.0);
        }

        /// <summary>
        /// Vergütete Vollbenutzungsstunden eines Jahres [h]: Jahresdeckel und
        /// Restkontingent begrenzen, die Negativpreis-Stunden (<paramref name="abschlag"/>,
        /// Anteil 0…1) zählen nicht.
        /// </summary>
        public static double VerguetetH(double vbh, double deckelH, double restH, double abschlag)
        {
            return Math.Min(vbh, Math.Min(deckelH, restH)) * (1.0 - abschlag);
        }

        /// <summary>
        /// Der wirksame Eigenstromsatz [ct/kWh]: Mit dem Tatbestand „keiner" nach § 6 Abs. 3
        /// entfällt der eigene Satz (§ 7 Abs. 2) — die Regel von
        /// <c>WirtschaftlichkeitCtrl.SatzEigenDerAnlage</c>, die ihn hier ruft.
        /// </summary>
        public static double SatzEigenWirksam(double? satzEigenCt, string eigenfall)
        {
            double satz = satzEigenCt ?? 0;
            if (satz <= 0) return satz;
            return string.Equals((eigenfall ?? "").Trim(), DbWerte.KWKG_EIGENFALL_KEINER, StringComparison.Ordinal)
                ? 0 : satz;
        }

        /// <summary>Der Abschlag für Negativpreis-Stunden als Anteil 0…1 aus dem Prozentwert.</summary>
        public static double Abschlag(double prozent)
        {
            return Math.Min(100.0, Math.Max(0.0, prozent)) / 100.0;
        }

        /// <summary>Deckel des Kalenderjahres: letzte Staffelzeile mit JahrVon ≤ Jahr; vor der
        /// ersten Zeile gilt deren Wert, ohne Zeilen 3.500 h/a.</summary>
        public static double StaffelDeckel(IReadOnlyList<KeyValuePair<int, double>> staffel, int jahr)
        {
            double deckel = staffel.Count > 0 ? staffel[0].Value : 3500;
            foreach (KeyValuePair<int, double> z in staffel)
                if (z.Key <= jahr) deckel = z.Value; else break;
            return deckel;
        }

        /// <summary>
        /// Der Staffeldeckel eines Jahres über einen Katalogzugriff (Dialog): der
        /// Katalogwert des Jahres, sonst die <see cref="STAFFEL_RUECKFALL"/>.
        /// </summary>
        public static double StaffelDeckel(Func<string, int, GesetzParameter> katalog, int jahr)
        {
            try
            {
                GesetzParameter p = katalog == null ? null : katalog(DbWerte.GESETZ_KWKG_VBH_JAHRESDECKEL, jahr);
                if (p != null && p.Wert.HasValue && p.Wert.Value > 0) return p.Wert.Value;
            }
            catch { /* ein stummer Katalog fällt auf die Rückfallstaffel */ }
            return StaffelDeckel(STAFFEL_RUECKFALL, jahr);
        }
    }
}

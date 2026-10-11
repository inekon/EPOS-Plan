using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Feiertage eines Bundeslands</b> als Jahrestage im Gemeinjahr — die neun
    /// bundeseinheitlichen aus <see cref="Feiertage"/> und dazu die gesetzlichen Feiertage des
    /// Landes, die in ganz dem Land gelten. Gebraucht vom Betriebskalender der Bedarfsprofile
    /// (Konzept Simulationsablauf, Abschnitt 17); die Konditionierungsprofile führen
    /// Länderfeiertage weiter als gewöhnliche Perioden (Konzept Konditionierungsprofile 3.2).
    ///
    /// <para><b>Dieselbe Rechenvorschrift wie <see cref="Feiertage"/>:</b> Jede Regel liegt über
    /// <see cref="Feiertage.Jahrestag"/> nach der Konvention <see cref="Gemeinjahrkalender"/> (E114).
    /// Kein zweites Kalendermodell: Es gibt weder Perioden noch Ränge, nur eine Menge von Tagen.</para>
    ///
    /// <para><b>Nicht enthalten</b> sind Feiertage, die nur in Teilen eines Landes gelten
    /// (Fronleichnam in Teilen Sachsens und Thüringens, Mariä Himmelfahrt in Teilen Bayerns,
    /// Augsburger Friedensfest) und Feiertage, die stets auf einen Sonntag fallen (Oster- und
    /// Pfingstsonntag).</para>
    /// </summary>
    public static class Landesfeiertage
    {
        /// <summary>Die sechzehn Länderkennungen (ISO 3166-2 ohne „DE-") in Schemareihenfolge.</summary>
        public static readonly IReadOnlyList<string> BUNDESLAENDER = new[]
        {
            "BW", "BY", "BE", "BB", "HB", "HH", "HE", "MV",
            "NI", "NW", "RP", "SL", "SN", "ST", "SH", "TH"
        };

        /// <summary>Ist die Kennung eines der sechzehn Länder?</summary>
        public static bool Bekannt(string bundesland)
        {
            if (bundesland == null) return false;
            foreach (string b in BUNDESLAENDER)
                if (string.Equals(b, bundesland, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>
        /// <b>Die Feiertage des Gemeinjahrs als Jahrestage</b> (1 … 365, aufsteigend, ohne Doppel) nach der
        /// Konvention <paramref name="kalender"/>:
        /// die neun bundeseinheitlichen und, mit einer bekannten Länderkennung, die
        /// Landesfeiertage. <c>null</c> oder eine leere Kennung liefert allein die neun.
        /// Eine unbekannte Kennung liefert ebenfalls allein die neun — die Prüfklausel der
        /// Spalte lässt sie nicht zu.
        /// </summary>
        public static IReadOnlyList<int> Jahrestage(string bundesland, Gemeinjahrkalender kalender)
        {
            var tage = new SortedSet<int>();
            foreach (string regel in Feiertage.Regeln)
            {
                int t = Feiertage.Jahrestag(regel, kalender);
                if (t > 0) tage.Add(t);
            }

            if (Bekannt(bundesland))
                foreach (string regel in Regeln(bundesland))
                {
                    int t = Feiertage.Jahrestag(regel, kalender);
                    if (t > 0) tage.Add(t);
                }

            return new List<int>(tage);
        }

        /// <summary>
        /// <b>Die Feiertagsregeln eines Landes</b> (über die bundeseinheitlichen hinaus) in der Reihenfolge von
        /// <see cref="DbWerte.KOND_FEIERTAGE_LAENDER"/>; leer für ein unbekanntes Land oder <c>null</c>.
        /// </summary>
        public static IReadOnlyList<string> Regeln(string bundesland)
        {
            var liste = new List<string>();
            if (!Bekannt(bundesland)) return liste;
            foreach (string regel in DbWerte.KOND_FEIERTAGE_LAENDER)
                if (Fuehrt(bundesland, regel)) liste.Add(regel);
            return liste;
        }

        /// <summary>Führt das Land die Regel? (Die Zuordnung der acht Länderregeln.)</summary>
        public static bool Fuehrt(string bl, string regel)
        {
            switch (regel)
            {
                case DbWerte.KOND_FEIERTAG_HEILIGE_DREI_KOENIGE: return bl == "BW" || bl == "BY" || bl == "ST";
                case DbWerte.KOND_FEIERTAG_FRAUENTAG: return bl == "BE" || bl == "MV";
                case DbWerte.KOND_FEIERTAG_FRONLEICHNAM:
                    return bl == "BW" || bl == "BY" || bl == "HE" || bl == "NW" || bl == "RP" || bl == "SL";
                case DbWerte.KOND_FEIERTAG_MARIAE_HIMMELFAHRT: return bl == "SL";
                case DbWerte.KOND_FEIERTAG_WELTKINDERTAG: return bl == "TH";
                case DbWerte.KOND_FEIERTAG_REFORMATIONSTAG:
                    return bl == "BB" || bl == "HB" || bl == "HH" || bl == "MV" || bl == "NI" ||
                           bl == "SN" || bl == "ST" || bl == "SH" || bl == "TH";
                case DbWerte.KOND_FEIERTAG_ALLERHEILIGEN: return bl == "BW" || bl == "BY" || bl == "NW" || bl == "RP" || bl == "SL";
                case DbWerte.KOND_FEIERTAG_BUSS_UND_BETTAG: return bl == "SN";
                default: return false;
            }
        }
    }
}

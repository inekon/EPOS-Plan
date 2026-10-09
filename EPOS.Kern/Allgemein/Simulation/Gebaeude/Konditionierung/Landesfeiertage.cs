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
    /// <para><b>Dieselbe Rechenvorschrift wie <see cref="Feiertage"/>:</b> Das Osterdatum kommt aus
    /// <see cref="Feiertage.Ostersonntag"/>, Tag und Monat werden über
    /// <see cref="Feiertage.Gemeinjahrestag"/> auf den Jahrestag des Gemeinjahres abgebildet. Kein
    /// zweites Kalendermodell: Es gibt weder Perioden noch Ränge, nur eine Menge von Tagen.</para>
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
        /// <b>Die Feiertage eines Jahres als Jahrestage</b> (1 … 365, aufsteigend, ohne Doppel):
        /// die neun bundeseinheitlichen und, mit einer bekannten Länderkennung, die
        /// Landesfeiertage. <c>null</c> oder eine leere Kennung liefert allein die neun.
        /// Eine unbekannte Kennung liefert ebenfalls allein die neun — die Prüfklausel der
        /// Spalte lässt sie nicht zu.
        /// </summary>
        public static IReadOnlyList<int> Jahrestage(string bundesland, int referenzjahr)
        {
            var tage = new SortedSet<int>();
            foreach (string regel in Feiertage.Regeln)
            {
                int t = Feiertage.Jahrestag(regel, referenzjahr);
                if (t > 0) tage.Add(t);
            }

            if (Bekannt(bundesland))
                foreach (int t in Landestage(bundesland, referenzjahr))
                    if (t > 0) tage.Add(t);

            return new List<int>(tage);
        }

        /// <summary>Die Landesfeiertage allein — ohne die neun bundeseinheitlichen.</summary>
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
        private static IEnumerable<int> Landestage(string bl, int jahr)
        {
            DateTime ostern = Feiertage.Ostersonntag(jahr);

            // Heilige Drei Könige, 6. Januar
            if (bl == "BW" || bl == "BY" || bl == "ST") yield return Feiertage.Gemeinjahrestag(1, 6);

            // Internationaler Frauentag, 8. März
            if (bl == "BE" || bl == "MV") yield return Feiertage.Gemeinjahrestag(3, 8);

            // Fronleichnam, Ostersonntag + 60 Tage
            if (bl == "BW" || bl == "BY" || bl == "HE" || bl == "NW" || bl == "RP" || bl == "SL")
            {
                DateTime f = ostern.AddDays(60);
                yield return Feiertage.Gemeinjahrestag(f.Month, f.Day);
            }

            // Mariä Himmelfahrt, 15. August (landesweit nur im Saarland)
            if (bl == "SL") yield return Feiertage.Gemeinjahrestag(8, 15);

            // Weltkindertag, 20. September
            if (bl == "TH") yield return Feiertage.Gemeinjahrestag(9, 20);

            // Reformationstag, 31. Oktober
            if (bl == "BB" || bl == "HB" || bl == "HH" || bl == "MV" || bl == "NI" ||
                bl == "SN" || bl == "ST" || bl == "SH" || bl == "TH")
                yield return Feiertage.Gemeinjahrestag(10, 31);

            // Allerheiligen, 1. November
            if (bl == "BW" || bl == "BY" || bl == "NW" || bl == "RP" || bl == "SL")
                yield return Feiertage.Gemeinjahrestag(11, 1);

            // Buß- und Bettag: der Mittwoch vor dem 23. November
            if (bl == "SN")
            {
                DateTime d = new DateTime(jahr, 11, 22);
                while (d.DayOfWeek != DayOfWeek.Wednesday) d = d.AddDays(-1);
                yield return Feiertage.Gemeinjahrestag(d.Month, d.Day);
            }
        }
    }
}

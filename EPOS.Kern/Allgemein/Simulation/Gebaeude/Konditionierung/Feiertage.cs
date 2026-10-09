using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die neun bundeseinheitlichen Feiertage als Regel</b> (Konzept Konditionierungsprofile 3.2,
    /// Festlegung F11). In der Periode steht die <b>Regelkennung</b>, nicht ein Jahrestag: Das
    /// Referenzjahr und das Schaltjahr verschieben jeden Jahrestag, die Regel bleibt. Der Lauf löst
    /// sie gegen das Referenzjahr auf — das Osterdatum als Rechenvorschrift — und bildet Tag und
    /// Monat im <b>Gemeinjahr</b> (365 Tage, kein 29. Februar) ab.
    ///
    /// <para>Länderfeiertage sind gewöhnliche Perioden mit Datum; sie stehen nicht in dieser
    /// Liste.</para>
    ///
    /// <para>Ohne Datenbank, ohne Uhr, ohne Zufall: <see cref="Jahrestag"/> ist eine reine Funktion
    /// von Regel und Jahr.</para>
    /// </summary>
    public static class Feiertage
    {
        /// <summary>
        /// Die neun Regeln in Schemareihenfolge — dieselbe Ordnung wie
        /// <see cref="DbWerte.KOND_FEIERTAGE"/>.
        /// </summary>
        public static IReadOnlyList<string> Regeln => DbWerte.KOND_FEIERTAGE;

        /// <summary>Ist das Kennwort eine der neun Regeln? (Keine stille Umdeutung, Konzept 3.6.)</summary>
        public static bool Bekannt(string regel)
        {
            if (regel == null) return false;
            foreach (string r in DbWerte.KOND_FEIERTAGE_ALLE)
                if (string.Equals(regel, r, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>
        /// <b>Der Jahrestag einer Regel im Gemeinjahr</b> — 1 … 365, aus Tag und Monat des
        /// Referenzjahres. −1, wenn das Kennwort keine der neun Regeln ist.
        ///
        /// <para>Der Umweg über Tag und Monat ist Absicht: In einem Schaltjahr liegt Ostern auf
        /// einem anderen <em>Jahrestag</em> als im Gemeinjahr, aber auf demselben <em>Datum</em> —
        /// und der Kalender speichert Daten, keine Jahrestage (Konzept 3.2).</para>
        /// </summary>
        public static int Jahrestag(string regel, int referenzjahr)
        {
            if (!Bekannt(regel)) return -1;

            switch (regel)
            {
                case DbWerte.KOND_FEIERTAG_NEUJAHR: return Gemeinjahrestag(1, 1);
                case DbWerte.KOND_FEIERTAG_ERSTER_MAI: return Gemeinjahrestag(5, 1);
                case DbWerte.KOND_FEIERTAG_EINHEIT: return Gemeinjahrestag(10, 3);
                case DbWerte.KOND_FEIERTAG_WEIHNACHTEN_1: return Gemeinjahrestag(12, 25);
                case DbWerte.KOND_FEIERTAG_WEIHNACHTEN_2: return Gemeinjahrestag(12, 26);
                // Die Regeln der Laender (Schemaschritt KalenderbedienungSchema).
                case DbWerte.KOND_FEIERTAG_HEILIGE_DREI_KOENIGE: return Gemeinjahrestag(1, 6);
                case DbWerte.KOND_FEIERTAG_FRAUENTAG: return Gemeinjahrestag(3, 8);
                case DbWerte.KOND_FEIERTAG_MARIAE_HIMMELFAHRT: return Gemeinjahrestag(8, 15);
                case DbWerte.KOND_FEIERTAG_WELTKINDERTAG: return Gemeinjahrestag(9, 20);
                case DbWerte.KOND_FEIERTAG_REFORMATIONSTAG: return Gemeinjahrestag(10, 31);
                case DbWerte.KOND_FEIERTAG_ALLERHEILIGEN: return Gemeinjahrestag(11, 1);
                case DbWerte.KOND_FEIERTAG_BUSS_UND_BETTAG:
                {
                    // Der Mittwoch vor dem 23. November.
                    DateTime d = new DateTime(referenzjahr, 11, 22);
                    while (d.DayOfWeek != DayOfWeek.Wednesday) d = d.AddDays(-1);
                    return Gemeinjahrestag(d.Month, d.Day);
                }
            }

            DateTime ostern = Ostersonntag(referenzjahr);
            DateTime tag;
            switch (regel)
            {
                case DbWerte.KOND_FEIERTAG_KARFREITAG: tag = ostern.AddDays(-2); break;
                case DbWerte.KOND_FEIERTAG_OSTERMONTAG: tag = ostern.AddDays(1); break;
                case DbWerte.KOND_FEIERTAG_HIMMELFAHRT: tag = ostern.AddDays(39); break;
                case DbWerte.KOND_FEIERTAG_PFINGSTMONTAG: tag = ostern.AddDays(50); break;
                case DbWerte.KOND_FEIERTAG_FRONLEICHNAM: tag = ostern.AddDays(60); break;
                default: return -1;
            }
            return Gemeinjahrestag(tag.Month, tag.Day);
        }

        /// <summary>
        /// <b>Das Osterdatum als Rechenvorschrift</b> — der gaußsche Osteralgorithmus in der
        /// Fassung von Meeus/Jones/Butcher für den gregorianischen Kalender. Reine Arithmetik,
        /// gültig für jedes Jahr ab 1583; keine Tabelle, die veralten könnte.
        /// </summary>
        public static DateTime Ostersonntag(int jahr)
        {
            int a = jahr % 19;
            int b = jahr / 100;
            int c = jahr % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int monat = (h + l - 7 * m + 114) / 31;          // 3 = März, 4 = April
            int tag = (h + l - 7 * m + 114) % 31 + 1;
            return new DateTime(jahr, monat, tag);
        }

        /// <summary>
        /// Tag und Monat als Jahrestag im <b>Gemeinjahr</b> (1 … 365). Der 29. Februar kommt im
        /// Gemeinjahr nicht vor und ergibt −1 wie jedes unmögliche Datum (B13: im Dialog eine
        /// Fehleingabe, <see cref="Ferienzeit.Jahrestag(string, string)"/>); eine Feiertagsregel trifft
        /// ihn nie.
        /// </summary>
        public static int Gemeinjahrestag(int monat, int tagImMonat)
        {
            if (monat < 1 || monat > 12) return -1;
            if (tagImMonat < 1 || tagImMonat > TageJeMonat[monat - 1]) return -1;
            int tag = tagImMonat;
            for (int m = 0; m < monat - 1; m++) tag += TageJeMonat[m];
            return tag;
        }

        /// <summary>Die Tage der zwölf Monate im Gemeinjahr (kein Schaltjahr, wie der ganze Rechenkern).</summary>
        public static readonly int[] TageJeMonat = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>
        /// Zerlegt einen Jahrestag 1 … 365 des Gemeinjahres in Monat und Tag; <c>false</c>
        /// außerhalb. Das Gegenstück zu <see cref="Gemeinjahrestag"/> — die Umrechnung, die der
        /// Dialog für ein Datumsfeld braucht (B13).
        /// </summary>
        public static bool Datum(int jahrestag, out int monat, out int tagImMonat)
        {
            monat = 0;
            tagImMonat = 0;
            if (jahrestag < 1 || jahrestag > 365) return false;
            int rest = jahrestag;
            for (int m = 0; m < 12; m++)
            {
                if (rest <= TageJeMonat[m])
                {
                    monat = m + 1;
                    tagImMonat = rest;
                    return true;
                }
                rest -= TageJeMonat[m];
            }
            return false;
        }
    }
}

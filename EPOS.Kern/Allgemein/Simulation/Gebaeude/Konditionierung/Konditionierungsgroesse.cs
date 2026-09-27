using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die fünf Größen der Konditionierung</b> (Konzept Konditionierungsprofile 3.1, Festlegung
    /// F1). Je Größe gibt es einen Kalender und eine Spalte der Vorgabe-Matrix; die Persistenzwerte
    /// stehen in <see cref="DbWerte.KOND_GROESSEN"/>.
    /// </summary>
    public enum Konditionierungsgroesse
    {
        /// <summary>Der Heizsollwert θ_H [°C]; „aus“ heißt NaN — der Löser rechnet die Zone ohne Heizung.</summary>
        Heizsoll,

        /// <summary>Der Kühlsollwert θ_K [°C]; „aus“ heißt +∞ — die Zone schwingt nach oben frei (E32).</summary>
        Kuehlsoll,

        /// <summary>Die Nutzerlüftung n_N [1/h] <b>absolut</b>; die Infiltration bleibt konstant darunter (F15). „aus“ heißt 0 1/h.</summary>
        Lueftung,

        /// <summary>Geräte und Anlage als Anteil 0 … 1 eines Nennwerts [W]. „aus“ heißt 0.</summary>
        Geraete,

        /// <summary>Die Anwesenheit der Personen als Anteil 0 … 1 eines Nennwerts [W] (P1 (b)). „aus“ heißt 0.</summary>
        Personen,
    }

    /// <summary>
    /// <b>Die Regeln einer Größe</b> — Grenzen, Persistenzwert und die Bedeutung von „aus“
    /// (Konzept 3.1, 3.6). Ohne Datenbank und ohne Oberfläche: EINE Stelle, an der die Grenzen
    /// stehen, für Leser, Schreiber, Generator, Controller und Dialog.
    ///
    /// <para><b>Die Anteile führen 0 … 1, nicht 0 … 100.</b> Die Matrix und die Oberfläche zeigen
    /// Prozent, der Rechenkern rechnet mit dem Anteil — so steht nirgends im Rechenweg ein Faktor
    /// 100 auf einer Last.</para>
    /// </summary>
    public static class Konditionierungsgroessen
    {
        /// <summary>Die fünf Größen in Schemareihenfolge (dieselbe wie <see cref="DbWerte.KOND_GROESSEN"/>).</summary>
        public static readonly Konditionierungsgroesse[] Alle =
        {
            Konditionierungsgroesse.Heizsoll, Konditionierungsgroesse.Kuehlsoll,
            Konditionierungsgroesse.Lueftung, Konditionierungsgroesse.Geraete,
            Konditionierungsgroesse.Personen,
        };

        /// <summary>Die kleinste Lüftung [1/h] (Konzept 3.6).</summary>
        public const double LUEFTUNG_MIN = 0.0;

        /// <summary>Die größte Lüftung [1/h] (Konzept 3.6).</summary>
        public const double LUEFTUNG_MAX = 20.0;

        /// <summary>Der kleinste Anteil einer Last (0 %).</summary>
        public const double ANTEIL_MIN = 0.0;

        /// <summary>Der größte Anteil einer Last (100 %).</summary>
        public const double ANTEIL_MAX = 1.0;

        /// <summary>Der Persistenzwert einer Größe (<see cref="DbWerte"/>).</summary>
        public static string Kennwort(Konditionierungsgroesse g)
        {
            switch (g)
            {
                case Konditionierungsgroesse.Heizsoll: return DbWerte.KOND_GROESSE_HEIZSOLL;
                case Konditionierungsgroesse.Kuehlsoll: return DbWerte.KOND_GROESSE_KUEHLSOLL;
                case Konditionierungsgroesse.Lueftung: return DbWerte.KOND_GROESSE_LUEFTUNG;
                case Konditionierungsgroesse.Geraete: return DbWerte.KOND_GROESSE_GERAETE;
                case Konditionierungsgroesse.Personen: return DbWerte.KOND_GROESSE_PERSONEN;
                default: throw new ArgumentOutOfRangeException(nameof(g));
            }
        }

        /// <summary>
        /// Die Größe zu einem Persistenzwert; <c>false</c>, wenn das Kennwort keine der fünf
        /// Größen ist (ein Datenbankfall, keine stille Umdeutung).
        /// </summary>
        public static bool AusKennwort(string kennwort, out Konditionierungsgroesse g)
        {
            foreach (Konditionierungsgroesse k in Alle)
                if (string.Equals(kennwort, Kennwort(k), StringComparison.Ordinal))
                {
                    g = k;
                    return true;
                }
            g = Konditionierungsgroesse.Heizsoll;
            return false;
        }

        /// <summary>Die kleinste zulässige Zahl der Größe (die Grenze der Zelle, Konzept 3.6).</summary>
        public static double Min(Konditionierungsgroesse g)
        {
            switch (g)
            {
                case Konditionierungsgroesse.Heizsoll:
                case Konditionierungsgroesse.Kuehlsoll: return GebaeudeFestwerte.SOLLWERTPROFIL_MIN_C;
                case Konditionierungsgroesse.Lueftung: return LUEFTUNG_MIN;
                default: return ANTEIL_MIN;
            }
        }

        /// <summary>Die größte zulässige Zahl der Größe.</summary>
        public static double Max(Konditionierungsgroesse g)
        {
            switch (g)
            {
                case Konditionierungsgroesse.Heizsoll:
                case Konditionierungsgroesse.Kuehlsoll: return GebaeudeFestwerte.SOLLWERTPROFIL_MAX_C;
                case Konditionierungsgroesse.Lueftung: return LUEFTUNG_MAX;
                default: return ANTEIL_MAX;
            }
        }

        /// <summary>
        /// <b>Was „aus“ bei dieser Größe bedeutet</b> (Konzept 3.1): beim Heizen NaN — der Löser
        /// rechnet die Zone ohne Heizung (B11) —, beim Kühlen +∞ (E32), bei der Lüftung 0 1/h
        /// Nutzerlüftung und bei einem Anteil 0.
        /// </summary>
        public static double AusWert(Konditionierungsgroesse g)
        {
            switch (g)
            {
                case Konditionierungsgroesse.Heizsoll: return double.NaN;
                case Konditionierungsgroesse.Kuehlsoll: return double.PositiveInfinity;
                default: return 0.0;
            }
        }

        /// <summary>Trägt die Größe einen Nennwert [W], hinter dem der Kalender Anteile führt?</summary>
        public static bool HatNennwert(Konditionierungsgroesse g)
            => g == Konditionierungsgroesse.Geraete || g == Konditionierungsgroesse.Personen;

        /// <summary>Liegt die Zahl in den Grenzen der Größe? (NaN und ±∞ liegen nie darin.)</summary>
        public static bool ImBereich(Konditionierungsgroesse g, double wert)
            => double.IsFinite(wert) && wert >= Min(g) && wert <= Max(g);

        /// <summary>Die Grenzen als Text für eine Meldung („0 … 30“).</summary>
        public static string Bereichstext(Konditionierungsgroesse g)
            => Min(g).ToString("G6", CultureInfo.InvariantCulture) + " … " +
               Max(g).ToString("G6", CultureInfo.InvariantCulture);
    }
}

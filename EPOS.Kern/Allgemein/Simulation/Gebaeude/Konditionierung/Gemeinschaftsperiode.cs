using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Eine Periode des gemeinsamen Kalenders</b> (Schemaschritt <see cref="KalenderbedienungSchema"/>, Konzept 7.8): EINE
    /// Zeile mit der Maske „gilt für" (<c>Gilt_Fuer</c>, Bit k = k-te Größe, 31 = alle). Sie wirkt in jeder Größe ihrer
    /// Maske, deren Kalender angelegt ist, als Periode mit gleichem Rang und gleicher Angabe; eine eigene Periode des
    /// Größenkalenders am selben Rang geht vor (<see cref="Kalendergemeinschaft.Ausbreiten"/>).
    /// </summary>
    /// <param name="Regel">Rang, Art, Bezeichner, Tage bzw. Feiertagsregel und Angabe.</param>
    /// <param name="Maske">Die Größenmaske 1 … 31.</param>
    public sealed record Gemeinschaftsperiode(Kalenderregel Regel, int Maske)
    {
        /// <summary>Der Rang der Periode.</summary>
        public int Rang => Regel.Rang;

        /// <summary>Gilt die Periode für die Größe?</summary>
        public bool Gilt(Konditionierungsgroesse g) => (Maske & Bit(g)) != 0;

        /// <summary>Das Maskenbit einer Größe (Schemareihenfolge, HEIZSOLL = 1 … PERSONEN = 16).</summary>
        public static int Bit(Konditionierungsgroesse g) => KalenderbedienungSchema.Maskenbit(Konditionierungsgroessen.Kennwort(g));

        /// <summary>Die Maske einer Größenauswahl; <c>null</c> = alle (31).</summary>
        public static int MaskeVon(IEnumerable<Konditionierungsgroesse> groessen)
            => groessen == null ? KalenderbedienungSchema.MASKE_ALLE : groessen.Aggregate(0, (m, g) => m | Bit(g));

        /// <summary>Die Größen der Maske in Schemareihenfolge.</summary>
        public static IReadOnlyList<Konditionierungsgroesse> Groessen(int maske)
            => Konditionierungsgroessen.Alle.Where(g => (maske & Bit(g)) != 0).ToList();

        /// <summary>Dieselbe Periode mit anderer Maske.</summary>
        public Gemeinschaftsperiode MitMaske(int maske) => new Gemeinschaftsperiode(Regel, maske);

        /// <summary>Sprachunabhängige Kurzfassung.</summary>
        public override string ToString() => Regel + " · " + Maske.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// <b>Eine benannte Woche</b> (<c>Tab_Konditionierungswoche</c>, Konzept 7.8): Name, Größe und 168 Werte (NaN = „aus")
    /// eines Eigentümers. Perioden verweisen über <c>ID_Woche</c> auf sie; <see cref="Id"/> ≤ 0 heißt im Arbeitsstand
    /// angelegt, die Zeile entsteht erst im OK-Weg.
    /// </summary>
    public sealed record BenannteWoche(long Id, Konditionierungsgroesse Groesse, string Name, double[] Werte)
    {
        /// <summary>Ist die Woche erst im Arbeitsstand angelegt?</summary>
        public bool IstNeu => Id <= 0;

        /// <summary>Gleich in Id, Größe, Name und jeder Zelle (bitgleich, NaN = NaN)?</summary>
        public bool Gleich(BenannteWoche andere)
            => andere != null && Id == andere.Id && Groesse == andere.Groesse
               && string.Equals(Name, andere.Name, StringComparison.Ordinal)
               && Werte.Length == andere.Werte.Length
               && Werte.Zip(andere.Werte, (a, b) => Kalendervergleich.Gleich(a, b)).All(x => x);
    }
}

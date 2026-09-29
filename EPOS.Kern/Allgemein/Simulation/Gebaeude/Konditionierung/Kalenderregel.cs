using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Eine Periode eines Konditionierungskalenders</b> (Konzept Konditionierungsprofile 3.2 und
    /// 5.1): Rang, Art, Bezeichner und entweder ein Zeitraum mit Datum <b>oder</b> eine
    /// Feiertagsregel, dazu genau eine Angabe. Perioden gelten <b>ganze Tage</b>: Ein Zeitraum
    /// beginnt um 00:00 seines Starttags und endet um 24:00 seines Endtags.
    ///
    /// <para><b>Der Rang ordnet, die Art benennt.</b> Je Stunde gewinnt die <em>ranghöchste</em>
    /// Periode, die den Tag enthält — die größere Zahl schlägt die kleinere; gerechnet wird mit der
    /// Art nicht (Konzept 3.2). Der Generator vergibt Ferien 200 + k und die Saison ab 900, damit
    /// die Betriebspause über den Perioden der Matrix liegt (Konzept 3.3).</para>
    ///
    /// <para>Unveränderlich, ohne Datenbank, ohne Uhr.</para>
    /// </summary>
    public sealed class Kalenderregel
    {
        /// <summary>Der kleinste Rang (Konzept 3.6).</summary>
        public const int RANG_MIN = 1;

        /// <summary>Der größte Rang.</summary>
        public const int RANG_MAX = 999;

        /// <summary>Der kleinste Tag eines Zeitraums (Gemeinjahr).</summary>
        public const int TAG_MIN = 1;

        /// <summary>Der größte Tag eines Zeitraums (Gemeinjahr, kein Schaltjahr).</summary>
        public const int TAG_MAX = 365;

        /// <summary>Die Höchstzahl der Perioden je Kalender (EPOS-Wert, Konzept 3.6).</summary>
        public const int PERIODEN_MAX = 64;

        private Kalenderregel(int rang, string art, string bezeichner, int beginn, int ende,
                              string feiertagsregel, Kalenderangabe angabe)
        {
            Rang = rang;
            Art = art;
            Bezeichner = bezeichner;
            Beginn = beginn;
            Ende = ende;
            Feiertagsregel = feiertagsregel;
            Angabe = angabe;
        }

        /// <summary>Der Rang 1 … 999, je Kalender eindeutig; die größere Zahl gewinnt.</summary>
        public int Rang { get; }

        /// <summary>Die Art (<see cref="DbWerte.KOND_ARTEN"/>) — sie ordnet und benennt, gerechnet wird mit ihr nicht.</summary>
        public string Art { get; }

        /// <summary>Der Bezeichner, den die Vorschau als Quelle nennt („Quelle: Sommerferien“).</summary>
        public string Bezeichner { get; }

        /// <summary>Der erste Tag 1 … 365 eines Zeitraums, 0 bei einer Feiertagsregel.</summary>
        public int Beginn { get; }

        /// <summary>Der letzte Tag 1 … 365 eines Zeitraums, 0 bei einer Feiertagsregel; <see cref="Beginn"/> über <see cref="Ende"/> heißt über den Jahreswechsel.</summary>
        public int Ende { get; }

        /// <summary>Die Feiertagsregel (<see cref="Feiertage"/>) oder <c>null</c> bei einem Zeitraum.</summary>
        public string Feiertagsregel { get; }

        /// <summary>Die Angabe der Periode — Wert, „aus“, eigene Woche oder „wie Wochentag X“.</summary>
        public Kalenderangabe Angabe { get; }

        /// <summary>Ist die Periode eine Feiertagsregel (statt eines Zeitraums mit Datum)?</summary>
        public bool IstFeiertag => Feiertagsregel != null;

        /// <summary>Geht der Zeitraum über den Jahreswechsel (Beginn nach Ende, Konzept 3.2)?</summary>
        public bool UeberJahreswechsel => !IstFeiertag && Beginn > Ende;

        /// <summary>
        /// Ein Zeitraum mit Datum (Tag 1 … 365 im Gemeinjahr); <paramref name="beginn"/> nach
        /// <paramref name="ende"/> heißt über den Jahreswechsel.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Rang oder ein Tag liegt außerhalb.</exception>
        /// <exception cref="ArgumentNullException">Art, Bezeichner oder Angabe fehlt.</exception>
        public static Kalenderregel Zeitraum(int rang, string art, string bezeichner, int beginn, int ende,
                                             Kalenderangabe angabe)
        {
            Rangpruefen(rang);
            Tagpruefen(beginn, nameof(beginn));
            Tagpruefen(ende, nameof(ende));
            Pflicht(art, bezeichner, angabe);
            return new Kalenderregel(rang, art, bezeichner, beginn, ende, null, angabe);
        }

        /// <summary>
        /// Eine Feiertagsregel ohne Datum — die Art ist <see cref="DbWerte.KOND_ART_FEIERTAG"/>
        /// (F11); der Lauf löst sie gegen das Referenzjahr auf.
        /// </summary>
        /// <exception cref="ArgumentException">Die Regel ist keine der neun (<see cref="Feiertage.Bekannt"/>).</exception>
        public static Kalenderregel Feiertag(int rang, string bezeichner, string feiertagsregel,
                                             Kalenderangabe angabe)
        {
            Rangpruefen(rang);
            if (!Feiertage.Bekannt(feiertagsregel))
                throw new ArgumentException("„" + (feiertagsregel ?? "leer") +
                                           "“ ist keine der neun bundeseinheitlichen Feiertagsregeln.",
                                           nameof(feiertagsregel));
            Pflicht(DbWerte.KOND_ART_FEIERTAG, bezeichner, angabe);
            return new Kalenderregel(rang, DbWerte.KOND_ART_FEIERTAG, bezeichner, 0, 0, feiertagsregel, angabe);
        }

        /// <summary>
        /// <b>Enthält die Periode den Tag?</b> <paramref name="tag0"/> zählt ab 0 (0 = 1. Januar …
        /// 364 = 31. Dezember). Ein Zeitraum über den Jahreswechsel enthält beide Enden;
        /// <paramref name="feiertag0"/> ist der aufgelöste Tag einer Feiertagsregel (ab 0) oder −1,
        /// wenn sie sich nicht auflösen ließ — dann enthält sie keinen Tag.
        /// </summary>
        public bool Enthaelt(int tag0, int feiertag0)
        {
            if (IstFeiertag) return feiertag0 >= 0 && tag0 == feiertag0;
            int b = Beginn - 1, e = Ende - 1;
            return b <= e ? tag0 >= b && tag0 <= e : tag0 >= b || tag0 <= e;
        }

        private static void Rangpruefen(int rang)
        {
            if (rang < RANG_MIN || rang > RANG_MAX)
                throw new ArgumentOutOfRangeException(nameof(rang),
                    "Ein Rang liegt zwischen " + Zahl(RANG_MIN) + " und " + Zahl(RANG_MAX) + ".");
        }

        private static void Tagpruefen(int tag, string name)
        {
            if (tag < TAG_MIN || tag > TAG_MAX)
                throw new ArgumentOutOfRangeException(name,
                    "Ein Tag liegt zwischen " + Zahl(TAG_MIN) + " und " + Zahl(TAG_MAX) + " (Gemeinjahr).");
        }

        private static void Pflicht(string art, string bezeichner, Kalenderangabe angabe)
        {
            if (string.IsNullOrEmpty(art)) throw new ArgumentNullException(nameof(art));
            if (string.IsNullOrEmpty(bezeichner)) throw new ArgumentNullException(nameof(bezeichner));
            if (angabe == null) throw new ArgumentNullException(nameof(angabe));
        }

        private static string Zahl(int n) => n.ToString(CultureInfo.InvariantCulture);

        /// <summary>Sprachunabhängige Kurzfassung für Protokoll und Fehlermeldung.</summary>
        public override string ToString()
            => Zahl(Rang) + " " + Art + " „" + Bezeichner + "“ " +
               (IstFeiertag ? Feiertagsregel : Zahl(Beginn) + "…" + Zahl(Ende)) + " = " + Angabe;
    }
}

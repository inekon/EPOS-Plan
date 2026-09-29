using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Vorgabe der Nachtauskühlung einer Größe</b> (Stufe KP1b, Konzept
    /// Konditionierungsprofile 3.7, P9 (b)): Nachtfenster, Tagwert n_T der Nutzerlüftung und der
    /// Außenabstand ΔT — alles, was der Eingang braucht, um die Nutzerreihe in einen
    /// <b>unbedingten</b> und einen <b>bedingten</b> Anteil zu teilen.
    ///
    /// <para><b>Sie kommt aus der Matrix des Eigentümers, dessen Lüftungskalender gilt</b> (Zone,
    /// Gebäude oder abgeleitet) — nie aus der Kalenderwoche. So trägt auch ein angelegter, von Hand
    /// geänderter Kalender das Fenster und den Tagwert, gegen die P9 seinen Überschuss hält.</para>
    ///
    /// <para><b>Ohne Tagwert gibt es keinen bedingten Anteil</b> (<see cref="TagwertH"/> leer): Der
    /// Lauf nennt es als Hinweis, und die Reihe wirkt unbedingt wie jeder andere Kalenderwert —
    /// eine unbedingte Nachtauskühlung erfindet der Rechenweg nicht.</para>
    ///
    /// <para>Unveränderlich, ohne Datenbank.</para>
    /// </summary>
    public sealed class Nachtauskuehlvorgabe
    {
        /// <summary>Der Außenabstand ΔT ohne Angabe [K] — die Vorgabe des Programms (P9).</summary>
        public const double ABSTAND_VORGABE_K = GebaeudeFestwerte.SOMMERLUEFTUNG_ABSTAND_AUSSEN;

        /// <summary>Der kleinste zulässige Außenabstand [K] (Konzept 5.6).</summary>
        public const double ABSTAND_MIN_K = 0.0;

        /// <summary>Der größte zulässige Außenabstand [K] (Konzept 5.6).</summary>
        public const double ABSTAND_MAX_K = 5.0;

        /// <summary>Baut die Vorgabe.</summary>
        /// <param name="fenster">Das Nachtfenster der Lüftungsspalte (F19, <see cref="Vorgabematrix.Nachtfenster"/>).</param>
        /// <param name="tagwertH">Der Tagwert n_T der Nutzerlüftung [1/h]; <c>null</c> = kein bedingter Anteil.</param>
        /// <param name="abstandK">ΔT [K] — <c>Bedingt_K</c>, <c>null</c> heißt <see cref="ABSTAND_VORGABE_K"/>.</param>
        /// <exception cref="ArgumentNullException">ohne Fenster.</exception>
        /// <exception cref="ArgumentOutOfRangeException">ΔT außerhalb 0 … 5 K oder nicht endlich.</exception>
        public Nachtauskuehlvorgabe(Nachtzeit fenster, double? tagwertH, double? abstandK)
        {
            Fenster = fenster ?? throw new ArgumentNullException(nameof(fenster));
            double dt = abstandK ?? ABSTAND_VORGABE_K;
            if (!double.IsFinite(dt) || dt < ABSTAND_MIN_K || dt > ABSTAND_MAX_K)
                throw new ArgumentOutOfRangeException(nameof(abstandK),
                    "Der Außenabstand der Nachtauskühlung liegt zwischen " +
                    ABSTAND_MIN_K.ToString("G6", CultureInfo.InvariantCulture) + " und " +
                    ABSTAND_MAX_K.ToString("G6", CultureInfo.InvariantCulture) + " K.");
            TagwertH = tagwertH.HasValue && double.IsFinite(tagwertH.Value) && tagwertH.Value >= 0.0
                ? tagwertH
                : (double?)null;
            AbstandK = dt;
        }

        /// <summary>Das Nachtfenster, in dem ein Überschuss bedingt ist (F19).</summary>
        public Nachtzeit Fenster { get; }

        /// <summary>
        /// Der Tagwert n_T der Nutzerlüftung [1/h] (Zelle Lüftung/Tag); <c>null</c> heißt: kein
        /// bedingter Anteil, benannter Hinweis im Lauf.
        /// </summary>
        public double? TagwertH { get; }

        /// <summary>Der Außenabstand ΔT [K] (<c>Bedingt_K</c>, leer = 2 K).</summary>
        public double AbstandK { get; }

        /// <summary>Kann überhaupt ein bedingter Anteil entstehen? (Nur mit Tagwert.)</summary>
        public bool TraegtBedingtes => TagwertH.HasValue;

        /// <summary>Sprachunabhängige Kurzfassung für Protokoll und Hinweis.</summary>
        public override string ToString()
            => Fenster + ", n_T " +
               (TagwertH.HasValue ? TagwertH.Value.ToString("G6", CultureInfo.InvariantCulture) : "—") +
               " 1/h, ΔT " + AbstandK.ToString("G6", CultureInfo.InvariantCulture) + " K";
    }
}

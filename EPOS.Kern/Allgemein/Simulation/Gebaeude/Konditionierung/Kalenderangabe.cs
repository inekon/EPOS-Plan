using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>Welcher Art die Angabe einer Kalenderebene ist (Konzept Konditionierungsprofile 3.2).</summary>
    public enum Angabeart
    {
        /// <summary>Ein Wert, alle Stunden gleich.</summary>
        Wert,

        /// <summary>„aus“ — beim Heizen NaN, beim Kühlen +∞, bei Lüftung und Anteilen 0 (P2 (b)).</summary>
        Aus,

        /// <summary>Eine eigene Woche mit 168 Zellen (Konzept 5.2).</summary>
        Woche,

        /// <summary>„wie Wochentag X“ (1 = Montag … 7 = Sonntag) — die Stunden dieses Tags aus der Standardwoche; nur an einer Periode.</summary>
        WieWochentag,
    }

    /// <summary>
    /// <b>Die Angabe einer Kalenderebene</b> — genau eine der vier Arten (Konzept
    /// Konditionierungsprofile 3.2 und 5.1: „genau eine Angabe aus Wert, „aus“, eigene Woche oder
    /// „wie Wochentag X“"). Unveränderlich, ohne Datenbank; die Woche trägt
    /// <see cref="double.NaN"/> als Kennzeichen „aus“ (siehe <see cref="Kalenderwoche"/>).
    /// </summary>
    public sealed class Kalenderangabe
    {
        private readonly double[] _woche;

        private Kalenderangabe(Angabeart art, double wert, double[] woche, int wieWochentag)
        {
            Art = art;
            Wert = wert;
            _woche = woche;
            WieWochentag = wieWochentag;
        }

        /// <summary>Die Art der Angabe.</summary>
        public Angabeart Art { get; }

        /// <summary>Der Wert bei <see cref="Angabeart.Wert"/>, sonst <see cref="double.NaN"/>.</summary>
        public double Wert { get; }

        /// <summary>Der Wochentag 1 … 7 bei <see cref="Angabeart.WieWochentag"/>, sonst 0.</summary>
        public int WieWochentag { get; }

        /// <summary>Die 168 Zellen bei <see cref="Angabeart.Woche"/>, sonst <c>null</c> (NaN = „aus“).</summary>
        public IReadOnlyList<double> Woche => _woche;

        /// <summary>Ein Wert für alle Stunden.</summary>
        /// <exception cref="ArgumentException">Der Wert ist nicht endlich — dafür gibt es <see cref="Abgeschaltet"/>.</exception>
        public static Kalenderangabe AusWert(double wert)
        {
            if (!double.IsFinite(wert))
                throw new ArgumentException("Eine Wertangabe braucht eine endliche Zahl; „aus“ ist eine eigene Art.",
                                           nameof(wert));
            return new Kalenderangabe(Angabeart.Wert, wert, null, 0);
        }

        /// <summary>„aus“ — die Größe wirkt in diesen Stunden nicht (P2 (b)).</summary>
        public static Kalenderangabe Abgeschaltet { get; } =
            new Kalenderangabe(Angabeart.Aus, double.NaN, null, 0);

        /// <summary>Eine eigene Woche mit genau 168 Zellen; die Liste wird kopiert (Unveränderlichkeit).</summary>
        /// <exception cref="ArgumentNullException">Die Liste fehlt.</exception>
        /// <exception cref="ArgumentException">Nicht genau 168 Zellen.</exception>
        public static Kalenderangabe AusWoche(IReadOnlyList<double> woche)
        {
            if (woche == null) throw new ArgumentNullException(nameof(woche));
            if (woche.Count != Kalenderwoche.WOCHENWERTE)
                throw new ArgumentException("Eine Woche hat genau " +
                                           Kalenderwoche.WOCHENWERTE.ToString(CultureInfo.InvariantCulture) +
                                           " Zellen, übergeben sind " +
                                           woche.Count.ToString(CultureInfo.InvariantCulture) + ".", nameof(woche));
            var kopie = new double[Kalenderwoche.WOCHENWERTE];
            for (int i = 0; i < kopie.Length; i++) kopie[i] = woche[i];
            return new Kalenderangabe(Angabeart.Woche, double.NaN, kopie, 0);
        }

        /// <summary>„wie Wochentag X“ (1 = Montag … 7 = Sonntag) — die Angabe der Feiertage (F11).</summary>
        /// <exception cref="ArgumentOutOfRangeException">Der Wochentag liegt außerhalb 1 … 7.</exception>
        public static Kalenderangabe AlsWochentag(int wochentag)
        {
            if (wochentag < 1 || wochentag > 7)
                throw new ArgumentOutOfRangeException(nameof(wochentag),
                    "Ein Wochentag liegt zwischen 1 (Montag) und 7 (Sonntag).");
            return new Kalenderangabe(Angabeart.WieWochentag, double.NaN, null, wochentag);
        }

        /// <summary>
        /// <b>Der Wert dieser Angabe in einer Wochenstunde</b> — <see cref="double.NaN"/> steht für
        /// „aus“ und wird erst in <see cref="Konditionierungskalender.Auswerten"/> durch den Wert
        /// der Größe ersetzt.
        ///
        /// <para><paramref name="standardwoche"/> braucht nur <see cref="Angabeart.WieWochentag"/>:
        /// Fehlt sie dort (der Kalender führt keine Standardwoche), bleibt die Angabe wirkungslos —
        /// der Aufrufer fällt dann auf die Ebene darunter zurück, und
        /// <see cref="Greift"/> sagt es ihm vorher.</para>
        /// </summary>
        /// <param name="wochentag">0 = Montag … 6 = Sonntag.</param>
        /// <param name="stundeDesTages">0 … 23.</param>
        /// <param name="standardwoche">Die Standardwoche des Kalenders oder <c>null</c>.</param>
        public double Stundenwert(int wochentag, int stundeDesTages, IReadOnlyList<double> standardwoche)
        {
            switch (Art)
            {
                case Angabeart.Wert: return Wert;
                case Angabeart.Aus: return double.NaN;
                case Angabeart.Woche: return _woche[Kalenderwoche.Stelle(wochentag, stundeDesTages)];
                default:
                    return standardwoche == null
                        ? double.NaN
                        : standardwoche[Kalenderwoche.Stelle(WieWochentag - 1, stundeDesTages)];
            }
        }

        /// <summary>
        /// Greift die Angabe? Nur „wie Wochentag X“ kann leerlaufen — ohne Standardwoche hat sie
        /// keine Stunden, aus denen sie schöpfen könnte (Konzept 3.2).
        /// </summary>
        public bool Greift(IReadOnlyList<double> standardwoche)
            => Art != Angabeart.WieWochentag || standardwoche != null;

        /// <summary>Sprachunabhängige Kurzfassung für Protokoll und Fehlermeldung.</summary>
        public override string ToString()
        {
            switch (Art)
            {
                case Angabeart.Wert: return Wert.ToString("G6", CultureInfo.InvariantCulture);
                case Angabeart.Aus: return DbWerte.KOND_WOCHE_AUS;
                case Angabeart.Woche: return "Woche";
                default: return "Wochentag " + WieWochentag.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}

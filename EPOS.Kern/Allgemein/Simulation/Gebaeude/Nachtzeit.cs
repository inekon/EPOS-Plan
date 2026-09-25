using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>Was die Prüfung eines Nachtzeit-Paars ergibt (<see cref="Nachtzeit.Pruefen"/>).</summary>
    public enum NachtzeitBefund
    {
        /// <summary>Beide leer (die Vorgabe) oder beide gesetzt, im Tag und verschieden.</summary>
        Gueltig,

        /// <summary>Nur Beginn oder nur Ende ist gesetzt.</summary>
        NurEineGesetzt,

        /// <summary>Eine Stunde liegt außerhalb 0 … 23.</summary>
        AusserhalbDesTages,

        /// <summary>Beginn und Ende sind gleich — die Nacht wäre leer oder der ganze Tag.</summary>
        BeginnGleichEnde,
    }

    /// <summary>
    /// <b>Die Nachtzeit eines Gebäudes</b> (Entscheid E43, Konzept-Nachtrag N1.48): Beginn und Ende der
    /// Nachtabsenkung als volle Stunde des Tages, die Nacht ist das halboffene Intervall
    /// [Beginn, Ende) — zyklisch über Mitternacht, wenn Beginn nach Ende liegt. Außerhalb der Nacht ist
    /// Nutzungszeit (Tagsollwert); Wochenende und Ferien wirken im Sollwertfahrplan darüber.
    ///
    /// <para><b>Die Vorgabe ist der bisherige Fahrplan, nicht eine zweite Wahrheit.</b> Beide Spalten
    /// leer heißt <see cref="Vorgabe"/>: Beginn <see cref="VORGABE_BEGINN"/> = 22 Uhr und Ende
    /// <see cref="VORGABE_ENDE"/> = 6 Uhr, hergeleitet aus den Stunden des Tagsollwerts
    /// <c>GebaeudeFestwerte.TAG_ERSTE_STUNDE</c>/<c>TAG_LETZTE_STUNDE</c> (1-basiert 7 … 22, Entscheid
    /// E8). <see cref="Nutzungszeit"/> der Vorgabe ist deshalb Stunde für Stunde dieselbe Aussage wie
    /// die frühere feste Regel — der Referenzlauf bleibt byte-gleich.</para>
    ///
    /// <para><b>Eine Regel für Editor und Stundenmodell.</b> <see cref="Pruefen"/> ist die Prüfung, die
    /// der Gebäudeeditor im Arbeitsstand und der Eingangsbauer des VDI-Wegs ziehen: beide leer oder
    /// beide gesetzt, je 0 … 23, nicht gleich. Öffentlich, weil der Katalogeditor in <c>EPOS.UI</c> sie
    /// braucht (dieselbe Lage wie <see cref="Gebaeudemodellvorgaben"/>). Unveränderlich; ohne
    /// Datenbank.</para>
    /// </summary>
    public sealed class Nachtzeit
    {
        /// <summary>Beginn der Nacht ohne Angabe [Uhr] — die Stunde nach der letzten Stunde des Tagsollwerts (22).</summary>
        public const int VORGABE_BEGINN = GebaeudeFestwerte.TAG_LETZTE_STUNDE;

        /// <summary>Ende der Nacht ohne Angabe [Uhr], ausschließlich — der Beginn der ersten Stunde des Tagsollwerts (6).</summary>
        public const int VORGABE_ENDE = GebaeudeFestwerte.TAG_ERSTE_STUNDE - 1;

        /// <summary>Kleinste zulässige Stunde (0 Uhr) — die Grenze der Spalte.</summary>
        public const int STUNDE_MIN = GebaeudeSchema.NACHTSTUNDE_MIN;

        /// <summary>Größte zulässige Stunde (23 Uhr) — die Grenze der Spalte.</summary>
        public const int STUNDE_MAX = GebaeudeSchema.NACHTSTUNDE_MAX;

        private Nachtzeit(int beginn, int ende, bool vorgabe)
        {
            Beginn = beginn;
            Ende = ende;
            IstVorgabe = vorgabe;
        }

        /// <summary>Die Nachtzeit ohne Angabe: 22 bis 6 Uhr.</summary>
        public static Nachtzeit Vorgabe { get; } = new Nachtzeit(VORGABE_BEGINN, VORGABE_ENDE, true);

        /// <summary>Beginn der Nacht [Stunde des Tages 0 … 23], einschließlich.</summary>
        public int Beginn { get; }

        /// <summary>Ende der Nacht [Stunde des Tages 0 … 23], ausschließlich.</summary>
        public int Ende { get; }

        /// <summary>Kam die Nachtzeit aus zwei leeren Spalten (<see cref="Vorgabe"/>)?</summary>
        public bool IstVorgabe { get; }

        /// <summary>Die Zahl der Nachtstunden je Tag (1 … 23).</summary>
        public int Nachtstunden => ((Ende - Beginn) % 24 + 24) % 24;

        /// <summary>
        /// <b>Die Prüfregel des Paars</b> — dieselbe in Editor und Eingangsbauer: beide leer ist die
        /// Vorgabe; nur eine gesetzt, eine Stunde außerhalb 0 … 23 oder Beginn = Ende ist ein benannter
        /// Eingabefehler.
        /// </summary>
        public static NachtzeitBefund Pruefen(int? beginn, int? ende)
        {
            if (!beginn.HasValue && !ende.HasValue) return NachtzeitBefund.Gueltig;
            if (beginn.HasValue != ende.HasValue) return NachtzeitBefund.NurEineGesetzt;
            if (!ImTag(beginn.Value) || !ImTag(ende.Value)) return NachtzeitBefund.AusserhalbDesTages;
            if (beginn.Value == ende.Value) return NachtzeitBefund.BeginnGleichEnde;
            return NachtzeitBefund.Gueltig;
        }

        /// <summary>
        /// Die Nachtzeit aus den zwei Spalten: beide leer = <see cref="Vorgabe"/>, sonst die gesetzten
        /// Stunden.
        /// </summary>
        /// <exception cref="ArgumentException">wenn <see cref="Pruefen"/> nicht <see cref="NachtzeitBefund.Gueltig"/> ergibt.</exception>
        public static Nachtzeit Aus(int? beginn, int? ende)
        {
            NachtzeitBefund befund = Pruefen(beginn, ende);
            if (befund != NachtzeitBefund.Gueltig)
                throw new ArgumentException("Die Nachtzeit " + Text(beginn) + " … " + Text(ende) + " ist ungültig (" + befund + ").");
            return beginn.HasValue ? new Nachtzeit(beginn.Value, ende.Value, false) : Vorgabe;
        }

        /// <summary>Liegt die Stunde des Tages <paramref name="stundeDesTages"/> (0 … 23) in der Nacht [Beginn, Ende)?</summary>
        public bool IstNacht(int stundeDesTages)
        {
            int s = ((stundeDesTages % 24) + 24) % 24;
            return Beginn < Ende
                ? s >= Beginn && s < Ende
                : s >= Beginn || s < Ende;
        }

        /// <summary>
        /// Ist die Stunde <paramref name="h"/> Nutzungszeit (Tagsollwert)? <paramref name="h"/> zählt ab
        /// Mitternacht in Stunden — eine Jahresstunde 0 … 8759 oder eine Wochenstunde 0 … 167; es zählt
        /// allein die Stunde des Tages <c>h mod 24</c>.
        /// </summary>
        public bool Nutzungszeit(int h) => !IstNacht(h);

        private static bool ImTag(int stunde) => stunde >= STUNDE_MIN && stunde <= STUNDE_MAX;

        private static string Text(int? stunde)
            => stunde.HasValue ? stunde.Value.ToString(CultureInfo.InvariantCulture) : "leer";

        /// <summary>Sprachunabhängige Kurzfassung: <c>22–6</c>.</summary>
        public override string ToString()
            => Beginn.ToString(CultureInfo.InvariantCulture) + "–" + Ende.ToString(CultureInfo.InvariantCulture);
    }
}

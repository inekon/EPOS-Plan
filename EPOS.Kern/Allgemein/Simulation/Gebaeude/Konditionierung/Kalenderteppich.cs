using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Woher ein Tag des Teppichbilds seinen Wert hat</b> (Konzept Konditionierungsprofile 3.2,
    /// 7.5; Entwurf KP2, Festlegung 8) — die Ebene des Kalenders, die an diesem Tag gewinnt.
    /// </summary>
    public enum Teppichquellart
    {
        /// <summary>Ebene 1: ein Wert oder „aus" für alle Stunden, keine Periode greift.</summary>
        Grundangabe,

        /// <summary>Ebene 2: die Standardwoche, keine Periode greift.</summary>
        Standardwoche,

        /// <summary>Eine Periode der Art <see cref="DbWerte.KOND_ART_ZEITRAUM"/>.</summary>
        Zeitraum,

        /// <summary>Eine Periode der Art <see cref="DbWerte.KOND_ART_FERIEN"/>.</summary>
        Ferien,

        /// <summary>Eine Feiertagsregel (<see cref="DbWerte.KOND_ART_FEIERTAG"/>).</summary>
        Feiertag,

        /// <summary>Eine eigene Periode der Art <see cref="DbWerte.KOND_ART_BETRIEBSPAUSE"/>.</summary>
        Betriebspause,

        /// <summary>
        /// Die Betriebspause der Saisonzeile (<see cref="Standardfahrplan.RANG_SAISON"/>) — die Tage
        /// außerhalb der Heiz- bzw. Kühlperiode, in denen die Größe „aus" steht (E53).
        /// </summary>
        Saison,
    }

    /// <summary>
    /// <b>Die Quelle eines Tages</b> — Art, Bezeichner, Rang und Periodenart der Ebene, die ihn
    /// bestimmt. Wertgleich, damit das Teppichbild Folgetage gleicher Quelle zusammenfassen kann.
    /// </summary>
    public sealed class Teppichquelle : IEquatable<Teppichquelle>
    {
        private Teppichquelle(Teppichquellart art, string bezeichner, int rang, string periodenart)
        {
            Art = art;
            Bezeichner = bezeichner;
            Rang = rang;
            Periodenart = periodenart;
        }

        /// <summary>Die Ebene des Kalenders, die den Tag bestimmt.</summary>
        public Teppichquellart Art { get; }

        /// <summary>Der Bezeichner der Periode („Sommerferien", „Saison"); <c>null</c> ohne Periode.</summary>
        public string Bezeichner { get; }

        /// <summary>Der Rang der Periode; 0 ohne Periode.</summary>
        public int Rang { get; }

        /// <summary>Die Art der Periode (<see cref="DbWerte.KOND_ARTEN"/>); <c>null</c> ohne Periode.</summary>
        public string Periodenart { get; }

        /// <summary>Hat eine Periode den Tag bestimmt?</summary>
        public bool IstPeriode => Rang > 0;

        /// <summary>Die Grundangabe (Ebene 1) als Quelle.</summary>
        public static Teppichquelle Grundangabe { get; } =
            new Teppichquelle(Teppichquellart.Grundangabe, null, 0, null);

        /// <summary>Die Standardwoche (Ebene 2) als Quelle.</summary>
        public static Teppichquelle Standardwoche { get; } =
            new Teppichquelle(Teppichquellart.Standardwoche, null, 0, null);

        /// <summary>
        /// Die Quelle zu einer Periode — die Art folgt der Periodenart; die Betriebspause der
        /// Saisonzeile (Rang <see cref="Standardfahrplan.RANG_SAISON"/>) ist <see cref="Teppichquellart.Saison"/>.
        /// </summary>
        public static Teppichquelle AusPeriode(Kalenderregel periode)
        {
            if (periode == null) throw new ArgumentNullException(nameof(periode));
            Teppichquellart art;
            switch (periode.Art)
            {
                case DbWerte.KOND_ART_FERIEN: art = Teppichquellart.Ferien; break;
                case DbWerte.KOND_ART_FEIERTAG: art = Teppichquellart.Feiertag; break;
                case DbWerte.KOND_ART_BETRIEBSPAUSE:
                    art = periode.Rang == Standardfahrplan.RANG_SAISON
                        ? Teppichquellart.Saison
                        : Teppichquellart.Betriebspause;
                    break;
                default: art = Teppichquellart.Zeitraum; break;
            }
            return new Teppichquelle(art, periode.Bezeichner, periode.Rang, periode.Art);
        }

        /// <summary>Wertgleichheit über Art, Bezeichner, Rang und Periodenart.</summary>
        public bool Equals(Teppichquelle andere)
            => andere != null && Art == andere.Art && Rang == andere.Rang &&
               string.Equals(Bezeichner, andere.Bezeichner, StringComparison.Ordinal) &&
               string.Equals(Periodenart, andere.Periodenart, StringComparison.Ordinal);

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as Teppichquelle);

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine((int)Art, Rang, Bezeichner ?? "", Periodenart ?? "");

        /// <summary>Sprachunabhängige Kurzfassung für Protokoll und Fehlermeldung.</summary>
        public override string ToString()
            => IstPeriode
                ? Art + " „" + Bezeichner + "“ (" + Rang.ToString(CultureInfo.InvariantCulture) + ")"
                : Art.ToString();
    }

    /// <summary>
    /// <b>Das Teppichbild eines Kalenders</b> (Konzept Konditionierungsprofile 7.5; Entwurf KP2,
    /// Festlegung 8): die <b>Rohreihe</b> über 8 760 Stunden und je Tag die <b>Quelle</b>.
    ///
    /// <para><b>Roh heißt: „aus" bleibt NaN — bei jeder Größe</b> (Befund B12). Der Lauf ersetzt
    /// „aus" in <see cref="Konditionierungskalender.Auswerten"/> durch den Wert der Größe (Heizen NaN,
    /// Kühlen +∞, Lüftung und Anteile 0); im Bild wäre die gelüftete Nacht mit 0 1/h dann nicht
    /// von der abgeschalteten zu unterscheiden. Die Rohreihe hält das Kennzeichen, <c>Auswerten</c>
    /// bleibt unberührt.</para>
    ///
    /// <para><b>Dieselbe Entscheidung wie der Lauf — gerufen, nicht abgeschrieben.</b> Je Tag gilt
    /// <see cref="Konditionierungskalender.Quellperiode"/> (die ranghöchste Periode, die den Tag
    /// enthält und greift), sonst die Grundangabe; der Wert einer Stunde ist
    /// <see cref="Kalenderangabe.Stundenwert"/> dieser Angabe. Ohne „aus" ist die Rohreihe deshalb
    /// Stunde für Stunde <see cref="Konditionierungskalender.Auswerten"/> (Rundlauf,
    /// <c>KalenderteppichTests</c>).</para>
    ///
    /// <para><b>Das Bezugsjahr</b> löst Wochentage und Feiertage auf: im Projekt das des Laufs, im
    /// Katalog <c>SolardatenCtrl.Referenzjahr(0)</c> — beides liefert
    /// <see cref="Konditionierungdatenweg.Bezugsjahr"/>. Der Teppich selbst kennt keine Uhr und
    /// keine Datenbank; er ist eine reine Funktion von Kalender, w₀ und Bezugsjahr.</para>
    ///
    /// <para>Die Werte stehen in der Einheit des Rechenkerns — Anteile 0 … 1, nicht Prozent; die
    /// Anzeige rechnet mit <see cref="Anzeigefaktor"/> um (<see cref="Einheit"/>).</para>
    /// </summary>
    public sealed class Kalenderteppich
    {
        /// <summary>Die Tage des Gemeinjahres.</summary>
        public const int TAGE = 365;

        /// <summary>Die Stunden eines Tages.</summary>
        public const int STUNDEN = Kalenderwoche.TAGESSTUNDEN;

        private readonly double[] _reihe;
        private readonly Teppichquelle[] _quellen;

        private Kalenderteppich(Konditionierungsgroesse groesse, int w0, int bezugsjahr,
                                double[] reihe, Teppichquelle[] quellen)
        {
            Groesse = groesse;
            WochentagDesErstenTags = w0;
            Bezugsjahr = bezugsjahr;
            _reihe = reihe;
            _quellen = quellen;
        }

        /// <summary>Die Größe des Kalenders.</summary>
        public Konditionierungsgroesse Groesse { get; }

        /// <summary>w₀: 0 = Montag … 6 = Sonntag für den 1. Januar des Bezugsjahres.</summary>
        public int WochentagDesErstenTags { get; }

        /// <summary>Das Jahr, gegen das Wochentage und Feiertage aufgelöst sind.</summary>
        public int Bezugsjahr { get; }

        /// <summary>
        /// Die Rohreihe über 8 760 Stunden, <c>h = 24 · Tag + Stunde</c>; NaN heißt „aus" — bei
        /// jeder Größe. Werte in der Einheit des Rechenkerns (Anteile 0 … 1).
        /// </summary>
        public IReadOnlyList<double> Rohreihe => _reihe;

        /// <summary>Die Quelle je Tag (0 = 1. Januar … 364 = 31. Dezember).</summary>
        public IReadOnlyList<Teppichquelle> Quellen => _quellen;

        /// <summary>Der Wert einer Stunde; NaN heißt „aus".</summary>
        public double Wert(int tag0, int stunde) => _reihe[tag0 * STUNDEN + stunde];

        /// <summary>Der Wochentag eines Tages: 0 = Montag … 6 = Sonntag.</summary>
        public int Wochentag(int tag0) => (WochentagDesErstenTags + tag0) % 7;

        /// <summary>
        /// <b>Bildet den Teppich eines Kalenders.</b>
        /// </summary>
        /// <param name="kalender">Der Kalender (angelegt oder aus der Matrix erzeugt).</param>
        /// <param name="wochentagDesErstenTags">w₀: 0 = Montag … 6 = Sonntag für den 1. Januar.</param>
        /// <param name="bezugsjahr">Das Bezugsjahr, gegen das die Feiertagsregeln aufgelöst werden.</param>
        /// <exception cref="ArgumentNullException">Der Kalender fehlt.</exception>
        /// <exception cref="ArgumentOutOfRangeException">w₀ liegt außerhalb 0 … 6.</exception>
        public static Kalenderteppich Bilden(Konditionierungskalender kalender, int wochentagDesErstenTags,
                                             int bezugsjahr)
            => Bilden(kalender, new Gemeinjahrkalender(wochentagDesErstenTags, bezugsjahr), bezugsjahr);

        /// <summary>
        /// <b>Der Teppich im Gemeinjahr</b> (E114): w₀ aus dem <paramref name="rasterjahr"/> — dieselbe
        /// Wochenendmaske wie der Lauf —, die Feiertage nach dem Jahr der Preisreihe
        /// <paramref name="feiertagsjahr"/>, ohne es (<c>null</c>) nach der Konvention <see cref="Gemeinjahrkalender"/>.
        /// </summary>
        public static Kalenderteppich ImGemeinjahr(Konditionierungskalender kalender, int rasterjahr, int? feiertagsjahr)
            => Bilden(kalender, Gemeinjahrkalender.Aus(WochentagDesErstenTagsIm(rasterjahr), feiertagsjahr), rasterjahr);

        private static Kalenderteppich Bilden(Konditionierungskalender kalender, Gemeinjahrkalender konvention,
                                              int bezugsjahr)
        {
            int wochentagDesErstenTags = konvention.W0;
            if (kalender == null) throw new ArgumentNullException(nameof(kalender));
            if (wochentagDesErstenTags < 0 || wochentagDesErstenTags > 6)
                throw new ArgumentOutOfRangeException(nameof(wochentagDesErstenTags),
                    "w₀ liegt zwischen 0 (Montag) und 6 (Sonntag).");

            IReadOnlyList<double> woche = kalender.Standardwoche;
            Teppichquelle ohnePeriode = kalender.Grundangabe.Art == Angabeart.Woche
                ? Teppichquelle.Standardwoche
                : Teppichquelle.Grundangabe;

            var reihe = new double[TAGE * STUNDEN];
            var quellen = new Teppichquelle[TAGE];
            for (int d = 0; d < TAGE; d++)
            {
                int w = (wochentagDesErstenTags + d) % 7;

                // DIESELBE Entscheidung wie Auswerten: die ranghoechste Periode, die den Tag
                // enthaelt und greift, sonst die Grundangabe bzw. Standardwoche.
                Kalenderregel periode = kalender.Quellperiode(d, konvention);
                Kalenderangabe angabe = periode != null ? periode.Angabe : kalender.Grundangabe;
                quellen[d] = periode != null ? Teppichquelle.AusPeriode(periode) : ohnePeriode;

                int h0 = d * STUNDEN;
                for (int s = 0; s < STUNDEN; s++)
                    reihe[h0 + s] = angabe.Stundenwert(w, s, woche);     // NaN bleibt „aus"
            }
            return new Kalenderteppich(kalender.Groesse, wochentagDesErstenTags, bezugsjahr, reihe, quellen);
        }

        /// <summary>
        /// Derselbe Teppich mit w₀ aus dem Bezugsjahr — dieselbe Wochenendmaske, aus der der Lauf
        /// sein w₀ nimmt (<c>KlimakalenderGemeinsam.WochenendmaskeBilden</c>, U7).
        /// </summary>
        public static Kalenderteppich Bilden(Konditionierungskalender kalender, int bezugsjahr)
            => Bilden(kalender, WochentagDesErstenTagsIm(bezugsjahr), bezugsjahr);

        /// <summary>
        /// w₀ eines Bezugsjahres — der Wochentag des 1. Januar (0 = Montag), aus derselben
        /// Wochenendmaske, nach der der Lauf seine Ortszeit-Woche setzt.
        /// </summary>
        public static int WochentagDesErstenTagsIm(int bezugsjahr)
            => GebaeudeModellEingang.WochentagDesErstenTags(KlimakalenderGemeinsam.WochenendmaskeBilden(bezugsjahr));

        /// <summary>Die Einheit der Anzeige: °C bei Heizen und Kühlen, 1/h bei der Lüftung, % bei den Anteilen.</summary>
        public static string Einheit(Konditionierungsgroesse g)
        {
            switch (g)
            {
                case Konditionierungsgroesse.Heizsoll:
                case Konditionierungsgroesse.Kuehlsoll: return "°C";
                case Konditionierungsgroesse.Lueftung: return "1/h";
                default: return "%";
            }
        }

        /// <summary>
        /// Der Faktor vom Rechenkern zur Anzeige: 100 bei den Anteilen (0 … 1 → %), sonst 1 — der
        /// Kern rechnet mit dem Anteil, die Matrix und das Bild zeigen Prozent.
        /// </summary>
        public static double Anzeigefaktor(Konditionierungsgroesse g)
            => Konditionierungsgroessen.HatNennwert(g) ? 100.0 : 1.0;

        /// <summary>
        /// Die Rohreihe in der Einheit der Anzeige (<see cref="Anzeigefaktor"/>); NaN bleibt NaN.
        /// </summary>
        public double[] Anzeigereihe()
        {
            double f = Anzeigefaktor(Groesse);
            var r = new double[_reihe.Length];
            for (int i = 0; i < r.Length; i++) r[i] = _reihe[i] * f;
            return r;
        }

        /// <summary>Sprachunabhängige Kurzfassung für Protokoll und Fehlermeldung.</summary>
        public override string ToString()
            => Konditionierungsgroessen.Kennwort(Groesse) + ", Bezugsjahr " +
               Bezugsjahr.ToString(CultureInfo.InvariantCulture) + ", w₀ " +
               WochentagDesErstenTags.ToString(CultureInfo.InvariantCulture);
    }
}

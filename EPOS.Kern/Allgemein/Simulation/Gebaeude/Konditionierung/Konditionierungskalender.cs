using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein Konditionierungskalender einer Größe</b> (Konzept Konditionierungsprofile 3.2 und 6):
    /// Grundangabe, Standardwoche und Perioden — und <see cref="Auswerten"/> als die eine
    /// Auswertung zur 8760-Reihe.
    ///
    /// <para><b>Drei Ebenen, von schwach nach stark</b> (Konzept 3.2):</para>
    /// <code>
    /// Ebene 1  Grundangabe     ein Wert oder „aus“
    /// Ebene 2  Standardwoche   168 Zellen; ist sie angelegt, tritt sie an die Stelle der Grundangabe
    /// Ebene 3  Perioden        ranghöchste Periode, die den Tag enthält
    /// </code>
    ///
    /// <para><b>Raster und Zeitbasis:</b> 8 760 Werte, Tag d = 0 … 364, Stunde s = 0 … 23,
    /// h = 24 · d + s; Gemeinjahr mit 365 Tagen. Der Wochentag ist w(d) = (w₀ + d) mod 7 mit w₀ aus
    /// der Wochenendmaske des Ortszeit-Kalenders — <b>derselben, nach der der Bestandsfahrplan das
    /// Wochenende setzt</b> (U7, <see cref="GebaeudeModellEingang.WochentagDesErstenTags"/>). Die
    /// Kalenderstunde ist die Uhrstunde der Ortszeit, in der der Lauf rechnet; an den zwei
    /// Umstelltagen gilt deren Regel, der Kalender führt keine eigene.</para>
    ///
    /// <para><b>Der Kalender speichert kein Jahr:</b> Wechselt das Referenzjahr, wandern die
    /// Wochentage und die Feiertage, die Daten nicht (B13).</para>
    ///
    /// <para>Unveränderlich, ohne Datenbank, ohne Uhr, ohne Zufall,
    /// <see cref="CultureInfo.InvariantCulture"/>: <see cref="Auswerten"/> ist eine reine Funktion
    /// von Kalender, w₀ und Referenzjahr.</para>
    /// </summary>
    public sealed class Konditionierungskalender
    {
        private readonly double[] _woche;
        private readonly Kalenderregel[] _perioden;

        /// <summary>
        /// Baut den Kalender. Genau eine Grundangabe (Wert oder „aus“) <b>oder</b> eine
        /// Standardwoche — das ist die Regel „genau eines von <c>Wert</c>, <c>Aus = 1</c>,
        /// <c>Woche</c>" der Tabelle (Konzept 5.1).
        /// </summary>
        /// <param name="groesse">Die Größe; sie bestimmt, was „aus“ bedeutet.</param>
        /// <param name="grundangabe">Ebene 1 oder 2: <see cref="Angabeart.Wert"/>, <see cref="Angabeart.Aus"/> oder <see cref="Angabeart.Woche"/>.</param>
        /// <param name="nennwert">Der Nennwert [W] bei Geräten und Personen; sonst <c>null</c>.</param>
        /// <param name="perioden">Die Perioden; <c>null</c> oder leer ist zulässig.</param>
        /// <exception cref="ArgumentNullException">Die Grundangabe fehlt.</exception>
        /// <exception cref="ArgumentException">
        /// Die Grundangabe ist „wie Wochentag X“ (die gibt es nur an einer Periode), ein Rang kommt
        /// doppelt vor, es sind mehr als <see cref="Kalenderregel.PERIODEN_MAX"/> Perioden, oder ein
        /// Nennwert steht an einer Größe ohne Nennwert.
        /// </exception>
        public Konditionierungskalender(Konditionierungsgroesse groesse, Kalenderangabe grundangabe,
                                        double? nennwert, IEnumerable<Kalenderregel> perioden)
        {
            if (grundangabe == null) throw new ArgumentNullException(nameof(grundangabe));
            if (grundangabe.Art == Angabeart.WieWochentag)
                throw new ArgumentException("„wie Wochentag X“ gibt es nur an einer Periode, nicht als Grundangabe.",
                                           nameof(grundangabe));
            if (nennwert.HasValue && !Konditionierungsgroessen.HatNennwert(groesse))
                throw new ArgumentException("Die Größe " + Konditionierungsgroessen.Kennwort(groesse) +
                                           " trägt keinen Nennwert.", nameof(nennwert));

            Groesse = groesse;
            Grundangabe = grundangabe;
            Nennwert = nennwert;
            _woche = grundangabe.Art == Angabeart.Woche ? Kopie(grundangabe.Woche) : null;

            var liste = new List<Kalenderregel>();
            if (perioden != null)
                foreach (Kalenderregel r in perioden)
                {
                    if (r == null) throw new ArgumentNullException(nameof(perioden));
                    liste.Add(r);
                }
            if (liste.Count > Kalenderregel.PERIODEN_MAX)
                throw new ArgumentException("Ein Kalender trägt höchstens " +
                                           Zahl(Kalenderregel.PERIODEN_MAX) + " Perioden, übergeben sind " +
                                           Zahl(liste.Count) + ".", nameof(perioden));
            // Der Rang ist je Kalender eindeutig (Konzept 3.6); ein doppelter Rang waere zwei
            // Wahrheiten ueber dieselbe Stunde.
            var raenge = new HashSet<int>();
            foreach (Kalenderregel r in liste)
                if (!raenge.Add(r.Rang))
                    throw new ArgumentException("Der Rang " + Zahl(r.Rang) + " kommt zweimal vor.", nameof(perioden));

            // ABSTEIGEND nach Rang: Die erste Periode, die den Tag enthaelt, ist die ranghoechste.
            liste.Sort((a, b) => b.Rang.CompareTo(a.Rang));
            _perioden = liste.ToArray();
        }

        /// <summary>Die Größe des Kalenders.</summary>
        public Konditionierungsgroesse Groesse { get; }

        /// <summary>Die Grundangabe — Ebene 1 (Wert oder „aus“) oder Ebene 2 (Standardwoche).</summary>
        public Kalenderangabe Grundangabe { get; }

        /// <summary>
        /// Der Nennwert [W] bei Geräten und Personen, hinter dem der Kalender Anteile führt; sonst
        /// <c>null</c>. Bei Geräten heißt <c>null</c> „<c>Interne_Waermegewinne</c>“ (Konzept 5.1).
        /// </summary>
        public double? Nennwert { get; }

        /// <summary>Die Standardwoche (Ebene 2) oder <c>null</c>; NaN ist das Kennzeichen „aus“.</summary>
        public IReadOnlyList<double> Standardwoche => _woche;

        /// <summary>Die Perioden, <b>absteigend nach Rang</b> — die erste, die einen Tag enthält, gewinnt.</summary>
        public IReadOnlyList<Kalenderregel> Perioden => _perioden;

        /// <summary>
        /// <b>Die Auswertung zur 8760-Reihe</b> (Konzept 3.2, 6). Je Stunde gewinnt genau eine
        /// Quelle: die ranghöchste Periode, die den Tag enthält, sonst Standardwoche bzw.
        /// Grundangabe. „aus“ wird durch den Wert der Größe ersetzt
        /// (<see cref="Konditionierungsgroessen.AusWert"/>) — beim Heizen NaN, beim Kühlen +∞, bei
        /// Lüftung und Anteilen 0.
        ///
        /// <para><b>Deterministisch:</b> gleiche Eingaben, gleiche Reihe — keine Uhr, kein Zufall,
        /// keine Kultur.</para>
        /// </summary>
        /// <param name="wochentagDesErstenTags">w₀: 0 = Montag … 6 = Sonntag für den 1. Januar.</param>
        /// <param name="referenzjahr">Das Jahr der Preisreihe, dessen Kalender gilt (w₀ muss das dieses Jahres sein);
        /// 0 = Regelfall ohne Jahr (<see cref="Gemeinjahrkalender.Aus"/>, E114/E115).</param>
        /// <exception cref="ArgumentOutOfRangeException">w₀ liegt außerhalb 0 … 6.</exception>
        /// <exception cref="ArgumentException">Ein Jahr mit fremdem w₀.</exception>
        public double[] Auswerten(int wochentagDesErstenTags, int referenzjahr)
            => Auswerten(Gemeinjahrkalender.Aus(wochentagDesErstenTags, referenzjahr));

        /// <summary>
        /// Die 8760-Reihe nach der Konvention <paramref name="kalender"/> — Wochentagsraster und, wenn
        /// vorhanden, das Jahr der Preisreihe (E114). Dieselbe Rechnung wie <see cref="Auswerten(int, int)"/>.
        /// </summary>
        public double[] Auswerten(Gemeinjahrkalender kalender)
        {
            int wochentagDesErstenTags = kalender.W0;

            // Die Feiertagsregeln EINMAL je Lauf aufloesen, nicht je Tag.
            var feiertag0 = new int[_perioden.Length];
            for (int p = 0; p < _perioden.Length; p++)
                feiertag0[p] = _perioden[p].IstFeiertag
                    ? Feiertage.Jahrestag(_perioden[p].Feiertagsregel, kalender) - 1
                    : -1;

            double ausWert = Konditionierungsgroessen.AusWert(Groesse);
            var reihe = new double[8760];
            for (int d = 0; d < 365; d++)
            {
                int w = (wochentagDesErstenTags + d) % 7;

                // Die ranghoechste Periode, die den Tag enthaelt und greift (Konzept 3.2).
                Kalenderangabe quelle = null;
                for (int p = 0; p < _perioden.Length; p++)
                {
                    Kalenderregel r = _perioden[p];
                    if (!r.Enthaelt(d, feiertag0[p]) || !r.Angabe.Greift(_woche)) continue;
                    quelle = r.Angabe;
                    break;
                }
                if (quelle == null) quelle = Grundangabe;

                int h0 = d * Kalenderwoche.TAGESSTUNDEN;
                for (int s = 0; s < Kalenderwoche.TAGESSTUNDEN; s++)
                {
                    double v = quelle.Stundenwert(w, s, _woche);
                    reihe[h0 + s] = double.IsNaN(v) ? ausWert : v;
                }
            }
            return reihe;
        }

        /// <summary>
        /// <b>Die Quelle einer Stunde</b> — der Bezeichner der Periode, die gewinnt, oder
        /// <c>null</c> für Standardwoche bzw. Grundangabe. Für die Vorschau der Kalenderkarte
        /// („Quelle: Sommerferien“, Konzept 3.2); dieselbe Entscheidung wie in
        /// <see cref="Auswerten"/>.
        /// </summary>
        public string Quelle(int tag0, int referenzjahr) => Quellperiode(tag0, referenzjahr)?.Bezeichner;

        /// <summary>Die Quelle eines Tags nach der Konvention <paramref name="kalender"/> (E114).</summary>
        public string Quelle(int tag0, Gemeinjahrkalender kalender) => Quellperiode(tag0, kalender)?.Bezeichner;

        /// <summary>
        /// <b>Die Periode, die einen Tag bestimmt</b> — die ranghöchste, die ihn enthält und greift,
        /// oder <c>null</c> für Standardwoche bzw. Grundangabe; dieselbe Entscheidung wie in
        /// <see cref="Auswerten"/> und in <see cref="Quelle"/>, nur mit der ganzen Regel statt ihres
        /// Bezeichners. Der Lauf fragt sie nach der <b>Saisonperiode</b> (Art
        /// <see cref="DbWerte.KOND_ART_BETRIEBSPAUSE"/>), um die Tage außerhalb der Heizperiode zu
        /// finden (E53).
        /// </summary>
        public Kalenderregel Quellperiode(int tag0, int referenzjahr)
            => Quellperiode(tag0, referenzjahr > 0 ? Gemeinjahrkalender.Kalenderjahr(referenzjahr) : new Gemeinjahrkalender(0));

        /// <summary>Die Periode eines Tags nach der Konvention <paramref name="kalender"/> (E114).</summary>
        public Kalenderregel Quellperiode(int tag0, Gemeinjahrkalender kalender)
        {
            for (int p = 0; p < _perioden.Length; p++)
            {
                Kalenderregel r = _perioden[p];
                int f0 = r.IstFeiertag ? Feiertage.Jahrestag(r.Feiertagsregel, kalender) - 1 : -1;
                if (r.Enthaelt(tag0, f0) && r.Angabe.Greift(_woche)) return r;
            }
            return null;
        }

        /// <summary>
        /// Trägt der Kalender irgendwo „aus“? Die Grundangabe, eine Zelle der Standardwoche oder
        /// eine Periode. Beim Heizsollwert ist das die Frage „gibt es Stunden ohne Heizung?“ (E53).
        /// </summary>
        public bool TraegtAus()
        {
            if (Grundangabe.Art == Angabeart.Aus) return true;
            if (_woche != null)
                foreach (double w in _woche)
                    if (double.IsNaN(w)) return true;
            foreach (Kalenderregel r in _perioden)
            {
                if (r.Angabe.Art == Angabeart.Aus) return true;
                if (r.Angabe.Art == Angabeart.Woche)
                    foreach (double w in r.Angabe.Woche)
                        if (double.IsNaN(w)) return true;
            }
            return false;
        }

        private static double[] Kopie(IReadOnlyList<double> werte)
        {
            var k = new double[werte.Count];
            for (int i = 0; i < k.Length; i++) k[i] = werte[i];
            return k;
        }

        private static string Zahl(int n) => n.ToString(CultureInfo.InvariantCulture);

        /// <summary>Sprachunabhängige Kurzfassung für Protokoll und Fehlermeldung.</summary>
        public override string ToString()
            => Konditionierungsgroessen.Kennwort(Groesse) + ": " + Grundangabe + ", " +
               Zahl(_perioden.Length) + " Periode(n)";
    }
}

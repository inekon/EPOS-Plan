using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die aufgelöste Konditionierung eines Gebäudes oder einer Zone</b> (Konzept
    /// Konditionierungsprofile 3.4 und 6): je Größe <b>ein</b> Kalender — die erste Quelle der Kette
    /// Zone → Gebäude → abgeleitet — samt Referenzjahr und w₀, aus denen
    /// <see cref="Konditionierungskalender.Auswerten"/> die 8760-Reihen bildet.
    ///
    /// <para><b>Ein leerer Satz ist der Bestandszweig.</b> <see cref="Leer"/> trägt keine Größe;
    /// <see cref="GebaeudeModellEingang"/> nimmt dann wörtlich den heutigen Weg — keine
    /// Multiplikation mit 1, kein neues Minimum, kein Umweg über den Kalender (Konzept 6,
    /// „Bauvorschrift der Byte-Gleichheit"). Deshalb <b>legt der Datenweg einen Satz nur an, wenn
    /// wirklich ein Kalender gilt</b>.</para>
    ///
    /// <para>Unveränderlich, ohne Datenbank. Das <b>Referenzjahr</b> ist Pflicht: Ohne es ließe sich
    /// eine Feiertagsregel nicht auflösen, und eine stille Auslassung wäre eine erfundene
    /// Betriebszeit (F11).</para>
    /// </summary>
    public sealed class Konditionierungssatz
    {
        private readonly Konditionierungskalender[] _kalender = new Konditionierungskalender[5];

        /// <summary>
        /// Baut den Satz.
        /// </summary>
        /// <param name="wochentagDesErstenTags">w₀ aus der Wochenendmaske des Ortszeit-Kalenders (U7), 0 = Montag.</param>
        /// <param name="referenzjahr">Das Jahr, gegen das die Feiertagsregeln aufgelöst werden.</param>
        /// <exception cref="ArgumentOutOfRangeException">w₀ außerhalb 0 … 6 oder ein unmögliches Jahr.</exception>
        public Konditionierungssatz(int wochentagDesErstenTags, int referenzjahr)
        {
            if (wochentagDesErstenTags < 0 || wochentagDesErstenTags > 6)
                throw new ArgumentOutOfRangeException(nameof(wochentagDesErstenTags),
                    "w₀ liegt zwischen 0 (Montag) und 6 (Sonntag).");
            if (referenzjahr < 1583 || referenzjahr > 9999)
                throw new ArgumentOutOfRangeException(nameof(referenzjahr),
                    "Das Referenzjahr liegt zwischen 1583 und 9999 (das Osterdatum ist gregorianisch).");
            WochentagDesErstenTags = wochentagDesErstenTags;
            Referenzjahr = referenzjahr;
        }

        /// <summary>Der leere Satz — kein Kalender, der Bestandszweig gilt.</summary>
        public static Konditionierungssatz Leer { get; } = new Konditionierungssatz(0, 2025);

        /// <summary>w₀: 0 = Montag … 6 = Sonntag für den 1. Januar.</summary>
        public int WochentagDesErstenTags { get; }

        /// <summary>Das Referenzjahr des Laufs — es löst die Feiertagsregeln auf (F11).</summary>
        public int Referenzjahr { get; }

        /// <summary>Trägt der Satz überhaupt einen Kalender? <c>false</c> heißt: wörtlich der Bestandszweig.</summary>
        public bool Wirksam { get; private set; }

        /// <summary>
        /// <b>Die Vorgabe der Nachtauskühlung</b> (Stufe KP1b, Konzept 3.7, P9 (b)) — Nachtfenster,
        /// Tagwert n_T und ΔT aus der Matrix des Eigentümers, dessen Lüftungskalender gilt.
        /// <c>null</c> heißt: keine Teilung der Nutzerreihe, alles wirkt unbedingt wie in KP1a.
        /// </summary>
        public Nachtauskuehlvorgabe Nachtauskuehlung { get; private set; }

        /// <summary>
        /// Legt die Vorgabe der Nachtauskühlung ab; <c>null</c> entfernt sie. Wie
        /// <see cref="Setzen"/> nur für den Datenweg gedacht — danach wird der Satz nur gelesen.
        /// </summary>
        public void NachtauskuehlungSetzen(Nachtauskuehlvorgabe vorgabe) => Nachtauskuehlung = vorgabe;

        /// <summary>Der Kalender einer Größe oder <c>null</c> — dann gilt für diese Größe der Bestandszweig.</summary>
        public Konditionierungskalender Kalender(Konditionierungsgroesse g) => _kalender[(int)g];

        /// <summary>Gilt für diese Größe ein Kalender?</summary>
        public bool Hat(Konditionierungsgroesse g) => _kalender[(int)g] != null;

        /// <summary>
        /// Legt den Kalender einer Größe ab; <c>null</c> entfernt ihn. Der Satz ist nach dem Bauen
        /// noch offen, damit der Datenweg die fünf Größen der Reihe nach füllen kann — danach wird er
        /// nur gelesen.
        /// </summary>
        /// <exception cref="ArgumentException">Der Kalender trägt eine andere Größe.</exception>
        public void Setzen(Konditionierungsgroesse g, Konditionierungskalender kalender)
        {
            if (kalender != null && kalender.Groesse != g)
                throw new ArgumentException("Der Kalender trägt die Größe " +
                                           Konditionierungsgroessen.Kennwort(kalender.Groesse) + ", abgelegt wird " +
                                           Konditionierungsgroessen.Kennwort(g) + ".", nameof(kalender));
            _kalender[(int)g] = kalender;
            Wirksam = false;
            foreach (Konditionierungskalender k in _kalender)
                if (k != null) { Wirksam = true; break; }
        }

        /// <summary>
        /// Die 8760-Reihe einer Größe — <c>null</c>, wenn die Größe keinen Kalender trägt.
        /// Deterministisch: dieselbe Reihe bei denselben Eingaben.
        /// </summary>
        public double[] Reihe(Konditionierungsgroesse g)
            => _kalender[(int)g]?.Auswerten(WochentagDesErstenTags, Referenzjahr);

        /// <summary>
        /// <b>Die Lastreihe einer Anteilsgröße</b> [W]: Anteil × Nennwert. Der Nennwert kommt aus dem
        /// Kalender; fehlt er, gilt <paramref name="nennwertRueckfall"/> — bei Geräten
        /// <c>Interne_Waermegewinne</c> (Konzept 5.1), bei Personen 0 W (ohne Nennwert gibt es keine
        /// Personenwärme). <c>null</c>, wenn die Größe keinen Kalender trägt.
        /// </summary>
        public double[] Lastreihe(Konditionierungsgroesse g, double nennwertRueckfall)
        {
            Konditionierungskalender k = _kalender[(int)g];
            if (k == null) return null;
            if (!Konditionierungsgroessen.HatNennwert(g))
                throw new ArgumentException("Die Größe " + Konditionierungsgroessen.Kennwort(g) +
                                           " führt keine Anteile.", nameof(g));
            double nennwert = k.Nennwert ?? nennwertRueckfall;
            double[] anteil = k.Auswerten(WochentagDesErstenTags, Referenzjahr);
            var last = new double[anteil.Length];
            for (int h = 0; h < anteil.Length; h++) last[h] = anteil[h] * nennwert;
            return last;
        }

        /// <summary>
        /// <b>Die Stunden ohne Heizung</b> (E53): wie oft der Heizsollwert „aus" ist — außerhalb der
        /// Heizperiode oder stundenweise. 0 ohne Heizkalender. Der Lauf nennt die Zahl als Hinweis,
        /// und der Kanal Raumwärme ist in diesen Stunden 0.
        /// </summary>
        public int StundenOhneHeizung()
        {
            double[] r = Reihe(Konditionierungsgroesse.Heizsoll);
            if (r == null) return 0;
            int n = 0;
            foreach (double v in r)
                if (double.IsNaN(v)) n++;
            return n;
        }

        /// <summary>Sprachunabhängige Kurzfassung für Protokoll und Hinweis.</summary>
        public override string ToString()
        {
            var t = new List<string>();
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                if (Hat(g)) t.Add(Konditionierungsgroessen.Kennwort(g));
            return t.Count == 0
                ? "kein Kalender"
                : string.Join(", ", t) + " (w0 = " + WochentagDesErstenTags.ToString(CultureInfo.InvariantCulture) +
                  ", Jahr " + Referenzjahr.ToString(CultureInfo.InvariantCulture) + ")";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>Was die Prüfung von Kalender- und Periodenzeilen ergibt (<see cref="Kalenderleser.Lesen"/>).</summary>
    public enum Kalenderbefund
    {
        /// <summary>Der Kalender steht.</summary>
        Gelesen,

        /// <summary>Das Kennwort der Größe ist keine der fünf.</summary>
        GroesseUnbekannt,

        /// <summary>Nicht genau eine Angabe aus Wert, „aus“ und Woche (Kalender) bzw. zusätzlich „wie Wochentag“ (Periode).</summary>
        AngabeNichtEindeutig,

        /// <summary>Ein Wert liegt außerhalb der Grenzen seiner Größe (Konzept 3.6).</summary>
        WertAusserhalb,

        /// <summary>Die Standardwoche oder die Woche einer Periode ist ungültig; <see cref="Kalenderlesung.Wochenbefund"/> und <see cref="Kalenderlesung.Stelle"/> nennen es genauer.</summary>
        WocheUngueltig,

        /// <summary>Die Art einer Periode ist keine der vier.</summary>
        ArtUnbekannt,

        /// <summary>Der Rang liegt außerhalb 1 … 999 oder kommt doppelt vor.</summary>
        RangUngueltig,

        /// <summary>Weder Datum noch Feiertagsregel, beides zugleich, ein Tag außerhalb 1 … 365 oder eine unbekannte Regel.</summary>
        ZeitraumUngueltig,

        /// <summary>„wie Wochentag X“ liegt außerhalb 1 … 7.</summary>
        WochentagUngueltig,

        /// <summary>Mehr als <see cref="Kalenderregel.PERIODEN_MAX"/> Perioden.</summary>
        ZuVielePerioden,

        /// <summary>Ein Nennwert steht an einer Größe ohne Nennwert oder ist negativ.</summary>
        NennwertUngueltig,
    }

    /// <summary>
    /// Der Befund eines Leseversuchs samt Kalender und Fundstelle — unveränderlich, ohne Ausnahme.
    /// Der Verwender baut die Meldung: der Lauf als
    /// <see cref="GebaeudeModellFehler.KalenderUngueltig"/> mit Größe, Periode und Stelle
    /// (<c>SIMENG_KOND_*</c>), der Dialog als <c>KOND_MSG_*</c> (Konzept 3.6).
    /// </summary>
    public sealed class Kalenderlesung
    {
        internal Kalenderlesung(Kalenderbefund befund, Konditionierungskalender kalender, string groesse,
                                int rang, string bezeichner, Wochenbefund wochenbefund, int stelle)
        {
            Befund = befund;
            Kalender = kalender;
            Groesse = groesse;
            Rang = rang;
            Bezeichner = bezeichner;
            Wochenbefund = wochenbefund;
            Stelle = stelle;
        }

        /// <summary>Der Befund.</summary>
        public Kalenderbefund Befund { get; }

        /// <summary>Der gelesene Kalender bei <see cref="Kalenderbefund.Gelesen"/>, sonst <c>null</c>.</summary>
        public Konditionierungskalender Kalender { get; }

        /// <summary>Das Kennwort der Größe — steht in jeder Meldung.</summary>
        public string Groesse { get; }

        /// <summary>Der Rang der Periode, an der es scheiterte; 0, wenn es die Kalenderzeile selbst war.</summary>
        public int Rang { get; }

        /// <summary>Der Bezeichner der Periode, an der es scheiterte; <c>null</c> bei der Kalenderzeile.</summary>
        public string Bezeichner { get; }

        /// <summary>Der Befund der Woche bei <see cref="Kalenderbefund.WocheUngueltig"/>.</summary>
        public Wochenbefund Wochenbefund { get; }

        /// <summary>Die Stelle 1 … 168 in der Woche, sonst 0.</summary>
        public int Stelle { get; }

        /// <summary>
        /// Die Fundstelle als Text für die Meldung: Größe, Periode und Stelle, sprachneutral und
        /// ohne Kultur — der Ressourcentext trägt den Satz, dieser Text die Fakten.
        /// </summary>
        public string Fundstelle()
        {
            string t = Groesse ?? "?";
            if (Rang > 0)
                t += ", Periode " + Rang.ToString(CultureInfo.InvariantCulture) +
                     (string.IsNullOrEmpty(Bezeichner) ? string.Empty : " „" + Bezeichner + "“");
            if (Stelle > 0) t += ", Stelle " + Stelle.ToString(CultureInfo.InvariantCulture);
            if (Befund == Kalenderbefund.WocheUngueltig) t += " (" + Wochenbefund + ")";
            return t;
        }
    }

    /// <summary>
    /// <b>Der strenge Leser der Zeilen</b> (Konzept Konditionierungsprofile 3.6 und 6): macht aus
    /// einer <see cref="Kalenderzeile"/> und ihren <see cref="Periodenzeile"/>n einen
    /// <see cref="Konditionierungskalender"/> — oder einen <b>benannten</b> Befund mit Größe,
    /// Periode und Stelle. Keine stille Umdeutung, kein Auffüllen, kein Rückfall: Ein Kalender mit
    /// 167 Wochenzellen ist ein Datenfehler.
    ///
    /// <para><b>Ohne Datenbank und ohne Ausnahme</b> — der Leser wirft nicht, er berichtet; der
    /// Verwender entscheidet, ob daraus ein Laufabbruch
    /// (<see cref="GebaeudeModellFehler.KalenderUngueltig"/>) oder eine Dialogmeldung wird.</para>
    /// </summary>
    public static class Kalenderleser
    {
        /// <summary>
        /// Liest einen Kalender samt Perioden. <paramref name="perioden"/> darf <c>null</c> sein;
        /// Zeilen mit fremdem <c>ID_Kalender</c> werden übergangen — der Aufrufer darf die Perioden
        /// aller Kalender eines Gebäudes in einem Rutsch übergeben.
        /// </summary>
        public static Kalenderlesung Lesen(Kalenderzeile zeile, IEnumerable<Periodenzeile> perioden)
        {
            if (zeile == null) throw new ArgumentNullException(nameof(zeile));

            if (!Konditionierungsgroessen.AusKennwort(zeile.Groesse, out Konditionierungsgroesse g))
                return Fehler(Kalenderbefund.GroesseUnbekannt, zeile.Groesse);

            // ---- Ebene 1/2: genau eine Angabe aus Wert, "aus" und Woche (Konzept 5.1) ----
            int belegt = (zeile.Wert.HasValue ? 1 : 0) + (zeile.Aus ? 1 : 0) +
                         (string.IsNullOrWhiteSpace(zeile.Woche) ? 0 : 1);
            if (belegt != 1) return Fehler(Kalenderbefund.AngabeNichtEindeutig, zeile.Groesse);

            Kalenderangabe grund;
            if (zeile.Aus) grund = Kalenderangabe.Abgeschaltet;
            else if (zeile.Wert.HasValue)
            {
                if (!Konditionierungsgroessen.ImBereich(g, zeile.Wert.Value))
                    return Fehler(Kalenderbefund.WertAusserhalb, zeile.Groesse);
                grund = Kalenderangabe.AusWert(zeile.Wert.Value);
            }
            else
            {
                Wochenlesung wl = Kalenderwoche.Lesen(zeile.Woche, g);
                if (wl.Befund != Wochenbefund.Gelesen)
                    return new Kalenderlesung(Kalenderbefund.WocheUngueltig, null, zeile.Groesse, 0, null,
                                              wl.Befund, wl.Stelle);
                grund = Kalenderangabe.AusWoche(wl.Werte);
            }

            // ---- Nennwert: nur an Geraeten und Personen, nicht negativ (Konzept 5.1) ----
            if (zeile.Nennwert.HasValue &&
                (!Konditionierungsgroessen.HatNennwert(g) || !(zeile.Nennwert.Value >= 0.0)))
                return Fehler(Kalenderbefund.NennwertUngueltig, zeile.Groesse);

            // ---- Ebene 3: die Perioden ----
            var regeln = new List<Kalenderregel>();
            var raenge = new HashSet<int>();
            if (perioden != null)
                foreach (Periodenzeile p in perioden)
                {
                    if (p == null || p.IdKalender != zeile.Id) continue;

                    if (!Artbekannt(p.Art))
                        return Periodenfehler(Kalenderbefund.ArtUnbekannt, zeile.Groesse, p);
                    if (p.Rang < Kalenderregel.RANG_MIN || p.Rang > Kalenderregel.RANG_MAX || !raenge.Add(p.Rang))
                        return Periodenfehler(Kalenderbefund.RangUngueltig, zeile.Groesse, p);
                    if (string.IsNullOrEmpty(p.Bezeichner))
                        return Periodenfehler(Kalenderbefund.ZeitraumUngueltig, zeile.Groesse, p);

                    // Genau eine Angabe aus Wert, "aus", Woche und "wie Wochentag X".
                    int pb = (p.Wert.HasValue ? 1 : 0) + (p.Aus ? 1 : 0) +
                             (string.IsNullOrWhiteSpace(p.Woche) ? 0 : 1) + (p.WieWochentag.HasValue ? 1 : 0);
                    if (pb != 1) return Periodenfehler(Kalenderbefund.AngabeNichtEindeutig, zeile.Groesse, p);

                    Kalenderangabe angabe;
                    if (p.Aus) angabe = Kalenderangabe.Abgeschaltet;
                    else if (p.Wert.HasValue)
                    {
                        if (!Konditionierungsgroessen.ImBereich(g, p.Wert.Value))
                            return Periodenfehler(Kalenderbefund.WertAusserhalb, zeile.Groesse, p);
                        angabe = Kalenderangabe.AusWert(p.Wert.Value);
                    }
                    else if (p.WieWochentag.HasValue)
                    {
                        if (p.WieWochentag.Value < 1 || p.WieWochentag.Value > 7)
                            return Periodenfehler(Kalenderbefund.WochentagUngueltig, zeile.Groesse, p);
                        angabe = Kalenderangabe.AlsWochentag(p.WieWochentag.Value);
                    }
                    else
                    {
                        Wochenlesung pwl = Kalenderwoche.Lesen(p.Woche, g);
                        if (pwl.Befund != Wochenbefund.Gelesen)
                            return new Kalenderlesung(Kalenderbefund.WocheUngueltig, null, zeile.Groesse,
                                                      p.Rang, p.Bezeichner, pwl.Befund, pwl.Stelle);
                        angabe = Kalenderangabe.AusWoche(pwl.Werte);
                    }

                    // Entweder Datum ODER Feiertagsregel (Konzept 5.1).
                    bool hatDatum = p.Beginn.HasValue && p.Ende.HasValue;
                    bool halbesDatum = p.Beginn.HasValue != p.Ende.HasValue;
                    bool hatRegel = !string.IsNullOrEmpty(p.Feiertagsregel);
                    if (halbesDatum || hatDatum == hatRegel)
                        return Periodenfehler(Kalenderbefund.ZeitraumUngueltig, zeile.Groesse, p);

                    if (hatRegel)
                    {
                        if (!Feiertage.Bekannt(p.Feiertagsregel) ||
                            !string.Equals(p.Art, DbWerte.KOND_ART_FEIERTAG, StringComparison.Ordinal))
                            return Periodenfehler(Kalenderbefund.ZeitraumUngueltig, zeile.Groesse, p);
                        regeln.Add(Kalenderregel.Feiertag(p.Rang, p.Bezeichner, p.Feiertagsregel, angabe));
                    }
                    else
                    {
                        if (!Tag(p.Beginn.Value) || !Tag(p.Ende.Value))
                            return Periodenfehler(Kalenderbefund.ZeitraumUngueltig, zeile.Groesse, p);
                        regeln.Add(Kalenderregel.Zeitraum(p.Rang, p.Art, p.Bezeichner,
                                                          p.Beginn.Value, p.Ende.Value, angabe));
                    }

                    if (regeln.Count > Kalenderregel.PERIODEN_MAX)
                        return Periodenfehler(Kalenderbefund.ZuVielePerioden, zeile.Groesse, p);
                }

            var kalender = new Konditionierungskalender(g, grund, zeile.Nennwert, regeln);
            return new Kalenderlesung(Kalenderbefund.Gelesen, kalender, zeile.Groesse, 0, null,
                                      Wochenbefund.Gelesen, 0);
        }

        /// <summary>
        /// <b>Der Schreiber</b> — das Gegenstück zu <see cref="Lesen"/>: macht aus einem Kalender die
        /// Zeilen, die der Controller schreibt. Der <b>Rundlauf</b> jeder Zelle und jedes Werts ist
        /// vorher zu prüfen (<see cref="Rundlaeuft"/>), sonst verschiebt „Anlegen“ eine Reihe um eine
        /// Rundung (Konzept 3.3 Regel 1).
        /// </summary>
        public static void Schreiben(Konditionierungskalender kalender, Kalenderzeile zeile,
                                     IList<Periodenzeile> perioden)
        {
            if (kalender == null) throw new ArgumentNullException(nameof(kalender));
            if (zeile == null) throw new ArgumentNullException(nameof(zeile));

            zeile.Groesse = Konditionierungsgroessen.Kennwort(kalender.Groesse);
            zeile.Wert = null;
            zeile.Aus = false;
            zeile.Woche = null;
            zeile.Nennwert = kalender.Nennwert;
            Angabeschreiben(kalender.Grundangabe, kalender.Groesse,
                            w => zeile.Wert = w, () => zeile.Aus = true, t => zeile.Woche = t, null);

            if (perioden == null) return;
            foreach (Kalenderregel r in kalender.Perioden)
            {
                var p = new Periodenzeile
                {
                    IdKalender = zeile.Id,
                    Rang = r.Rang,
                    Art = r.Art,
                    Bezeichner = r.Bezeichner,
                    Beginn = r.IstFeiertag ? (int?)null : r.Beginn,
                    Ende = r.IstFeiertag ? (int?)null : r.Ende,
                    Feiertagsregel = r.Feiertagsregel,
                };
                Angabeschreiben(r.Angabe, kalender.Groesse,
                                w => p.Wert = w, () => p.Aus = true, t => p.Woche = t, x => p.WieWochentag = x);
                perioden.Add(p);
            }
        }

        /// <summary>
        /// <b>Läuft der Kalender rund?</b> Jeder Wert und jede Wochenzelle kommt über Text und Leser
        /// bitgleich zurück (<see cref="Kalenderwoche.Rundlauf"/>) — die Invariante hinter „Anlegen
        /// ändert keine Reihe". <paramref name="stelle"/> nennt die erste Zelle 1 … 168, an der es
        /// scheitert, oder 0 für die Grundangabe bzw. einen Periodenwert.
        /// </summary>
        public static bool Rundlaeuft(Konditionierungskalender kalender, out int rang, out int stelle)
        {
            if (kalender == null) throw new ArgumentNullException(nameof(kalender));
            rang = 0;
            stelle = 0;
            if (!Angaberundlauf(kalender.Grundangabe, out stelle)) return false;
            foreach (Kalenderregel r in kalender.Perioden)
                if (!Angaberundlauf(r.Angabe, out stelle))
                {
                    rang = r.Rang;
                    return false;
                }
            return true;
        }

        private static bool Angaberundlauf(Kalenderangabe a, out int stelle)
        {
            stelle = 0;
            switch (a.Art)
            {
                case Angabeart.Wert: return Kalenderwoche.Rundlauf(a.Wert);
                case Angabeart.Woche:
                    for (int i = 0; i < a.Woche.Count; i++)
                        if (!Kalenderwoche.Rundlauf(a.Woche[i]))
                        {
                            stelle = i + 1;
                            return false;
                        }
                    return true;
                default: return true;
            }
        }

        private static void Angabeschreiben(Kalenderangabe a, Konditionierungsgroesse g,
                                            Action<double> wert, Action aus, Action<string> woche,
                                            Action<int> wochentag)
        {
            switch (a.Art)
            {
                case Angabeart.Wert: wert(a.Wert); break;
                case Angabeart.Aus: aus(); break;
                case Angabeart.Woche: woche(Kalenderwoche.Schreiben(a.Woche, g)); break;
                default:
                    if (wochentag == null)
                        throw new ArgumentException("„wie Wochentag X“ gibt es nur an einer Periode.", nameof(a));
                    wochentag(a.WieWochentag);
                    break;
            }
        }

        private static bool Artbekannt(string art)
        {
            if (art == null) return false;
            foreach (string a in DbWerte.KOND_ARTEN)
                if (string.Equals(art, a, StringComparison.Ordinal)) return true;
            return false;
        }

        private static bool Tag(int t) => t >= Kalenderregel.TAG_MIN && t <= Kalenderregel.TAG_MAX;

        private static Kalenderlesung Fehler(Kalenderbefund b, string groesse)
            => new Kalenderlesung(b, null, groesse, 0, null, Wochenbefund.Gelesen, 0);

        private static Kalenderlesung Periodenfehler(Kalenderbefund b, string groesse, Periodenzeile p)
            => new Kalenderlesung(b, null, groesse, p.Rang, p.Bezeichner, Wochenbefund.Gelesen, 0);
    }
}

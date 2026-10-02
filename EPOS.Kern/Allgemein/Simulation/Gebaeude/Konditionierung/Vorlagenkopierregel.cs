using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Wie eine Vorlage in eine andere Größe reist</b> — „Kopieren nach …" der Vorlagenverwaltung
    /// (Teilkonzept Konditionierungsprofile 3.5, 7.4).
    /// </summary>
    public enum Vorlagenkopierweg
    {
        /// <summary>Die Richtung gibt es nicht — sie wird benannt abgelehnt, nie still umgerechnet.</summary>
        Keiner,

        /// <summary>Geräte ↔ Personen: gleiche Einheit (Anteil 0 … 1), Werte und Zeitstruktur unverändert.</summary>
        Direkt,

        /// <summary>
        /// Heizen → Kühlen: Zeitstruktur und Aus-Zeiten bleiben, jede Zelle mit Sollwert bekommt den
        /// Komfortsollwert.
        /// </summary>
        Zeitstruktur,
    }

    /// <summary>
    /// <b>Die Regel von „Kopieren nach …"</b> (Teilkonzept Konditionierungsprofile 3.5, 7.4): Eine Vorlage
    /// gehört genau einer Größe (P11); diese Regel macht aus dem Inhalt einer Vorlage den Inhalt einer
    /// eigenen Vorlage einer <b>anderen</b> Größe. Drei Wege, keine stillen Umrechnungen:
    /// <list type="number">
    /// <item><b>Geräte ↔ Personen direkt</b> — gleiche Einheit, Vorgabezeilen, Standardwoche, Perioden und
    /// Feiertagsregeln gehen unverändert in die Zielgröße.</item>
    /// <item><b>Heizen → Kühlen</b> — nur die Zeitstruktur (Standardwoche, Perioden, Feiertagsregeln, die
    /// Nacht-, Wochenend- und Ferienzeilen mit ihren Zeiten) und die Aus-Zeiten: Wo Heizen „aus" ist, ist
    /// Kühlen „aus"; jede Zelle, die beim Heizen einen Sollwert trägt, bekommt den <b>Komfortsollwert</b>
    /// (Vorgabe <see cref="KOMFORTSOLLWERT_VORGABE"/>).</item>
    /// <item><b>Alle anderen Richtungen</b> (Kühlen → Heizen, alles mit Lüftung) gibt es nicht —
    /// <see cref="Ziele"/> nennt sie nicht, <see cref="Pruefen"/> lehnt sie benannt ab.</item>
    /// </list>
    /// Nennwert und Saison trägt eine Vorlage ohnehin nicht (E54); auch die Kopie trägt sie nicht.
    /// </summary>
    /// <remarks>
    /// Rein, ohne Datenbank: Der Vorlagen-Controller (<see cref="KonditionierungsvorlageCtrl.KopierenNach"/>)
    /// und die Ablage ohne Datenbank (<see cref="Konditionierungsvorlagenablage"/>) setzen denselben
    /// Inhalt um — so gelten in Anwendung und Prüfstand dieselben Regeln.
    /// </remarks>
    public static class Vorlagenkopierregel
    {
        /// <summary>
        /// Der Komfortsollwert [°C], den „Kopieren nach …" von Heizen nach Kühlen vorschlägt — derselbe
        /// Tagwert wie die ausgelieferten Kühlvorlagen.
        /// </summary>
        public const double KOMFORTSOLLWERT_VORGABE = 26.0;

        /// <summary>
        /// Der kleinste Komfortsollwert [°C]: die Grenzen der Kühlspalte der Matrix — die
        /// Plausibilitätsgrenze des Kühlsollwerts (<see cref="Gebaeudemodellvorgaben.KUEHLSOLLWERT_MIN"/>)
        /// und die Zellgrenze der Größe Kühlen (<see cref="Konditionierungsgroessen.Min"/>), die engere gilt.
        /// </summary>
        public static double KomfortsollwertMin
            => Math.Max(Gebaeudemodellvorgaben.KUEHLSOLLWERT_MIN, Konditionierungsgroessen.Min(Konditionierungsgroesse.Kuehlsoll));

        /// <summary>
        /// Der größte Komfortsollwert [°C] — wie <see cref="KomfortsollwertMin"/> die engere der beiden Grenzen
        /// der Kühlspalte.
        /// </summary>
        public static double KomfortsollwertMax
            => Math.Min(Gebaeudemodellvorgaben.KUEHLSOLLWERT_MAX, Konditionierungsgroessen.Max(Konditionierungsgroesse.Kuehlsoll));

        /// <summary>Der Weg von einer Größe in eine andere; <see cref="Vorlagenkopierweg.Keiner"/> = die Richtung gibt es nicht.</summary>
        public static Vorlagenkopierweg Weg(Konditionierungsgroesse quelle, Konditionierungsgroesse ziel)
        {
            if (quelle == Konditionierungsgroesse.Geraete && ziel == Konditionierungsgroesse.Personen) return Vorlagenkopierweg.Direkt;
            if (quelle == Konditionierungsgroesse.Personen && ziel == Konditionierungsgroesse.Geraete) return Vorlagenkopierweg.Direkt;
            if (quelle == Konditionierungsgroesse.Heizsoll && ziel == Konditionierungsgroesse.Kuehlsoll) return Vorlagenkopierweg.Zeitstruktur;
            return Vorlagenkopierweg.Keiner;
        }

        /// <summary>
        /// <b>Die erlaubten Ziele einer Quellgröße</b> in Schemareihenfolge — nur diese bietet der Dialog an;
        /// leer bei Kühlen und Lüftung.
        /// </summary>
        public static IReadOnlyList<Konditionierungsgroesse> Ziele(Konditionierungsgroesse quelle)
        {
            var ziele = new List<Konditionierungsgroesse>();
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                if (Weg(quelle, g) != Vorlagenkopierweg.Keiner) ziele.Add(g);
            return ziele;
        }

        /// <summary>Braucht die Richtung den Komfortsollwert (Heizen → Kühlen)?</summary>
        public static bool MitKomfortsollwert(Konditionierungsgroesse quelle, Konditionierungsgroesse ziel)
            => Weg(quelle, ziel) == Vorlagenkopierweg.Zeitstruktur;

        /// <summary>Gibt es die Richtung? <c>null</c> = ja, sonst die benannte Ablehnung.</summary>
        public static string Richtungspruefung(Konditionierungsgroesse quelle, Konditionierungsgroesse ziel)
            => Weg(quelle, ziel) != Vorlagenkopierweg.Keiner
                ? null
                : string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_KOPIE_RICHTUNG,
                                Konditionierungsarbeit.Groessenname(quelle), Konditionierungsarbeit.Groessenname(ziel));

        /// <summary>
        /// <b>Der Komfortsollwert</b> — eine Zahl in den Grenzen der Kühlspalte
        /// (<see cref="KomfortsollwertMin"/> … <see cref="KomfortsollwertMax"/>), die mit
        /// <see cref="Kalenderwoche.NACHKOMMASTELLEN"/> Nachkommastellen rund läuft.
        /// </summary>
        /// <returns><c>null</c>, wenn der Wert passt, sonst die benannte Ablehnung.</returns>
        public static string KomfortsollwertPruefen(double? wert)
        {
            if (!wert.HasValue || !double.IsFinite(wert.Value))
                return MyResource.Resource.KOND_MSG_KOMFORTSOLLWERT_FEHLT;
            double w = wert.Value;
            if (w < KomfortsollwertMin || w > KomfortsollwertMax)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_KOMFORTSOLLWERT_AUSSERHALB,
                                     Zahltext(w), Zahltext(KomfortsollwertMin), Zahltext(KomfortsollwertMax));
            if (!Kalenderwoche.Rundlauf(w))
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_WERT_RUNDLAUF,
                                     Zahltext(w), Kalenderwoche.NACHKOMMASTELLEN.ToString(CultureInfo.InvariantCulture));
            return null;
        }

        /// <summary>
        /// <b>Die Regeln vor dem Kopieren</b> ohne Datenbank: die Richtung und, bei Heizen → Kühlen, der
        /// Komfortsollwert. Den Namen prüft der Träger gegen seine Zielliste.
        /// </summary>
        /// <returns><c>null</c> = es darf kopiert werden, sonst die benannte Ablehnung.</returns>
        public static string Pruefen(Konditionierungsgroesse quelle, Konditionierungsgroesse ziel, double? komfortsollwert)
            => Richtungspruefung(quelle, ziel)
               ?? (MitKomfortsollwert(quelle, ziel) ? KomfortsollwertPruefen(komfortsollwert) : null);

        /// <summary>
        /// <b>Setzt den Inhalt einer Vorlage in die Zielgröße um</b> — das Ergebnis ist der Inhalt der Kopie
        /// als Ebene der Art <see cref="Kalendereigentuemer.Vorlage"/>, <b>nur in der Zielgröße</b>.
        ///
        /// <para><b>Was reist:</b> die Nutzungszeilen Tag, Nacht mit Zeiten, Wochenende und Ferien und, falls
        /// angelegt, der Kalender samt Standardwoche, eigenen Perioden und Feiertagsregeln. <b>Was nicht
        /// reist</b> (E54): die Zeilen <c>NENNWERT</c> und <c>SAISON</c>, Perioden der Arten <c>FERIEN</c>
        /// und <c>BETRIEBSPAUSE</c>, ein Nennwert am Kalender — und alles, was die Quelle in einer anderen
        /// Größe trüge.</para>
        ///
        /// <para><b>Direkt</b> (Geräte ↔ Personen) bleiben Werte, Zeiten, „aus" und die Herkunft des
        /// Kalenders Zeichen für Zeichen; ein Komfortsollwert spielt keine Rolle. <b>Zeitstruktur</b>
        /// (Heizen → Kühlen): Eine Zelle, Grundangabe, Wochenstunde oder Periode mit Sollwert bekommt den
        /// Komfortsollwert, „aus" bleibt „aus", eine Zelle nur mit Zeiten bleibt eine Zelle nur mit Zeiten,
        /// „wie Wochentag X" bleibt; die Herkunft des Kalenders fällt — ihr Werkzeugvermerk nennte
        /// Heizwerte.</para>
        /// </summary>
        /// <param name="inhalt">Der Inhalt der Quelle (Vorgabezellen und höchstens ein Kalender ihrer Größe).</param>
        /// <param name="quelle">Die Größe der Quelle.</param>
        /// <param name="ziel">Die Zielgröße.</param>
        /// <param name="komfortsollwert">Der Komfortsollwert [°C] bei Heizen → Kühlen; sonst ohne Bedeutung.</param>
        public static Ebenenergebnis Umsetzen(Konditionierungsstand inhalt, Konditionierungsgroesse quelle,
                                             Konditionierungsgroesse ziel, double? komfortsollwert)
        {
            if (inhalt == null) throw new ArgumentNullException(nameof(inhalt));
            string regel = Pruefen(quelle, ziel, komfortsollwert);
            if (regel != null) return Ebenenergebnis.Fehler(regel);

            Vorlagenkopierweg weg = Weg(quelle, ziel);
            double komfort = weg == Vorlagenkopierweg.Zeitstruktur ? komfortsollwert.Value : double.NaN;

            Konditionierungsstand v = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null);
            foreach (string zeile in NUTZUNGSZEILEN)
            {
                Matrixzelle alt = inhalt.Vorgabe(quelle, zeile);
                if (!Konditionierungsstand.Traegt(alt)) continue;
                Matrixzelle neu = weg == Vorlagenkopierweg.Direkt ? alt : Zeitstrukturzelle(alt, komfort);
                string pruefung = Konditionierungsarbeit.Zellenpruefung(ziel, zeile, neu);
                if (pruefung != null) return Ebenenergebnis.Fehler(pruefung);
                v = v.MitVorgabe(ziel, zeile, neu);
            }

            Konditionierungskalender k = inhalt.Kalender(quelle);
            if (k == null) return Ebenenergebnis.Gut(v);
            try
            {
                var perioden = new List<Kalenderregel>();
                foreach (Kalenderregel r in k.Perioden)
                {
                    if (Konditionierungsarbeit.IstMatrixbereich(r)) continue;      // E54: keine Ferien-, keine Saisonperiode
                    perioden.Add(weg == Vorlagenkopierweg.Direkt ? r : MitAngabe(r, Zeitstrukturangabe(r.Angabe, komfort)));
                }
                Kalenderangabe grund = weg == Vorlagenkopierweg.Direkt ? k.Grundangabe : Zeitstrukturangabe(k.Grundangabe, komfort);
                var kalender = new Konditionierungskalender(ziel, grund, null, perioden);
                Kalenderherkunft herkunft = weg == Vorlagenkopierweg.Direkt ? inhalt.Herkunft(quelle) : Kalenderherkunft.Keine;
                return Ebenenergebnis.Gut(v.MitKalender(ziel, kalender, herkunft));
            }
            catch (ArgumentException ex)
            {
                return Ebenenergebnis.Fehler(ex.Message);
            }
        }

        /// <summary>
        /// <b>Die Beschreibung der Kopie</b>: die der Quelle, ergänzt um die Herkunft
        /// („… — aus Vorlage ‚Büro‘ (Heizen)"); ohne Beschreibung nur die Herkunft. Höchstens
        /// <see cref="KonditionierungVorlagenSchema.BESCHREIBUNG_MAX_ZEICHEN"/> Zeichen — gekürzt wird die
        /// Beschreibung der Quelle, nicht die Herkunft.
        /// </summary>
        public static string Beschreibung(string beschreibung, string quellname, Konditionierungsgroesse quelle)
        {
            int max = KonditionierungVorlagenSchema.BESCHREIBUNG_MAX_ZEICHEN;
            string herkunft = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_KOPIE_HERKUNFT,
                                            (quellname ?? "").Trim(), Konditionierungsarbeit.Groessenname(quelle));
            string alt = (beschreibung ?? "").Trim();
            if (alt.Length == 0) return Gekuerzt(herkunft, max);

            string text = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_KOPIE_BESCHREIBUNG, alt, herkunft);
            if (text.Length <= max) return text;
            int behalten = alt.Length - (text.Length - max) - 1;              // ein Zeichen für „…"
            if (behalten < 1) return Gekuerzt(herkunft, max);
            text = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_KOPIE_BESCHREIBUNG,
                                 alt.Substring(0, behalten).TrimEnd() + "…", herkunft);
            return Gekuerzt(text, max);
        }

        // =================================================================
        //  Heizen → Kühlen: Zeitstruktur und „aus" bleiben, Sollwerte werden der Komfortsollwert
        // =================================================================

        /// <summary>Eine Zelle: „aus" bleibt „aus" samt Zeiten, ein Sollwert wird der Komfortsollwert, Zeiten bleiben.</summary>
        private static Matrixzelle Zeitstrukturzelle(Matrixzelle z, double komfort)
        {
            if (z.Aus) return Matrixzelle.Abgeschaltet(z.Von, z.Bis);
            return z.Belegt ? Matrixzelle.AusWert(komfort, z.Von, z.Bis) : Matrixzelle.NurZeiten(z.Von, z.Bis);
        }

        /// <summary>Eine Angabe: Wert → Komfortsollwert, „aus" bleibt, die Woche Stunde für Stunde, „wie Wochentag" bleibt.</summary>
        private static Kalenderangabe Zeitstrukturangabe(Kalenderangabe a, double komfort)
        {
            switch (a.Art)
            {
                case Angabeart.Wert:
                    return Kalenderangabe.AusWert(komfort);
                case Angabeart.Woche:
                    var woche = new double[Kalenderwoche.WOCHENWERTE];
                    for (int i = 0; i < woche.Length; i++)
                        woche[i] = double.IsNaN(a.Woche[i]) ? double.NaN : komfort;
                    return Kalenderangabe.AusWoche(woche);
                default:
                    return a;                                                 // „aus" und „wie Wochentag X"
            }
        }

        /// <summary>Dieselbe Periode mit einer anderen Angabe — Rang, Art, Bezeichner und Tage bzw. Regel bleiben.</summary>
        private static Kalenderregel MitAngabe(Kalenderregel r, Kalenderangabe angabe)
            => r.IstFeiertag
                ? Kalenderregel.Feiertag(r.Rang, r.Bezeichner, r.Feiertagsregel, angabe)
                : Kalenderregel.Zeitraum(r.Rang, r.Art, r.Bezeichner, r.Beginn, r.Ende, angabe);

        // =================================================================
        //  Kleine Helfer
        // =================================================================

        /// <summary>Die vier Nutzungszeilen einer Vorlage (E54) — ohne <c>NENNWERT</c> und <c>SAISON</c>.</summary>
        private static readonly string[] NUTZUNGSZEILEN =
        {
            DbWerte.KOND_ZEILE_TAG, DbWerte.KOND_ZEILE_NACHT, DbWerte.KOND_ZEILE_WOCHENENDE, DbWerte.KOND_ZEILE_FERIEN,
        };

        private static string Gekuerzt(string text, int max) => text.Length <= max ? text : text.Substring(0, max);

        private static string Zahltext(double w) => w.ToString("G6", CultureInfo.InvariantCulture);
    }
}

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
        /// Heizen → Kühlen: Zeitstruktur und Aus-Zeiten bleiben; eine Zelle mit Sollwert in Höhe des Tagwerts
        /// bekommt den Komfortsollwert, eine mit niedrigerem Sollwert (Absenkzeit) den Absenksollwert oder „aus".
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
    /// Kühlen „aus"; jede Zelle, die beim Heizen einen Sollwert in Höhe des <see cref="Tagwert"/>s trägt,
    /// bekommt den <b>Komfortsollwert</b> (Vorgabe <see cref="KOMFORTSOLLWERT_VORGABE"/>), jede mit einem
    /// niedrigeren Sollwert — die <b>Absenkzeit</b> (Nacht, Wochenende, Ferien) — den <b>Absenksollwert</b>
    /// (Vorgabe <see cref="ABSENKSOLLWERT_VORGABE"/>) oder „aus" (<see cref="ABSENKSOLLWERT_AUS"/>).</item>
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
        /// Der Absenksollwert [°C], den „Kopieren nach …" von Heizen nach Kühlen für die Absenkzeiten vorschlägt —
        /// beim Kühlen liegt die Absenkung über dem Komfortsollwert.
        /// </summary>
        public const double ABSENKSOLLWERT_VORGABE = 28.0;

        /// <summary>
        /// Der Absenksollwert „aus" — die Kopie kühlt in den Absenkzeiten nicht (wie die ausgelieferte Kühlvorlage
        /// „Büro" nachts, am Wochenende und in den Ferien). <see cref="double.NaN"/> wie „aus" in der Woche
        /// (<see cref="Kalenderwoche"/>) und im Zahlenfeld der Oberfläche.
        /// </summary>
        public const double ABSENKSOLLWERT_AUS = double.NaN;

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

        /// <summary>Braucht die Richtung den Komfortsollwert und den Absenksollwert (Heizen → Kühlen)?</summary>
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
        /// <b>Der Absenksollwert</b> — „aus" (<see cref="ABSENKSOLLWERT_AUS"/>) oder eine Zahl in denselben Grenzen
        /// wie der Komfortsollwert (<see cref="KomfortsollwertMin"/> … <see cref="KomfortsollwertMax"/>), die rund
        /// läuft und <b>nicht unter dem Komfortsollwert</b> liegt: Beim Kühlen ist die Absenkung ein höherer
        /// Sollwert; ein niedrigerer kühlte in der Absenkzeit stärker als am Tag. Ist der Komfortsollwert selbst
        /// ungültig, prüft diese Regel den Vergleich nicht — die Ablehnung gehört an dessen Feld.
        /// </summary>
        /// <returns><c>null</c>, wenn der Wert passt, sonst die benannte Ablehnung.</returns>
        public static string AbsenksollwertPruefen(double? wert, double? komfortsollwert)
        {
            if (!wert.HasValue || double.IsInfinity(wert.Value))
                return MyResource.Resource.KOND_MSG_ABSENKSOLLWERT_FEHLT;
            double w = wert.Value;
            if (double.IsNaN(w)) return null;                                // „aus"
            if (w < KomfortsollwertMin || w > KomfortsollwertMax)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_ABSENKSOLLWERT_AUSSERHALB,
                                     Zahltext(w), Zahltext(KomfortsollwertMin), Zahltext(KomfortsollwertMax));
            if (!Kalenderwoche.Rundlauf(w))
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_WERT_RUNDLAUF,
                                     Zahltext(w), Kalenderwoche.NACHKOMMASTELLEN.ToString(CultureInfo.InvariantCulture));
            if (KomfortsollwertPruefen(komfortsollwert) == null && w < komfortsollwert.Value)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_ABSENKSOLLWERT_UNTER_KOMFORT,
                                     Zahltext(w), Zahltext(komfortsollwert.Value));
            return null;
        }

        /// <summary>
        /// <b>Die Regeln vor dem Kopieren</b> ohne Datenbank: die Richtung und, bei Heizen → Kühlen, der
        /// Komfort- und der Absenksollwert. Den Namen prüft der Träger gegen seine Zielliste.
        /// </summary>
        /// <returns><c>null</c> = es darf kopiert werden, sonst die benannte Ablehnung.</returns>
        public static string Pruefen(Konditionierungsgroesse quelle, Konditionierungsgroesse ziel, double? komfortsollwert,
                                     double? absenksollwert)
            => Richtungspruefung(quelle, ziel)
               ?? (MitKomfortsollwert(quelle, ziel)
                   ? KomfortsollwertPruefen(komfortsollwert) ?? AbsenksollwertPruefen(absenksollwert, komfortsollwert)
                   : null);

        /// <summary>
        /// <b>Der Tagwert einer Heizvorlage</b> [°C] — die Schwelle der Absenkzeit von Heizen → Kühlen: der Wert der
        /// Vorgabezeile <c>TAG</c>, wenn sie einen Sollwert trägt; sonst der höchste Heizsollwert der Vorlage
        /// (Vorgabezeilen, Grundangabe und Perioden des Kalenders samt eigener Wochen). Die Tagzeile ist die
        /// Komfortzeit der Matrix (Nacht, Wochenende und Ferien sind ihre Absenkungen); ohne sie bleibt der
        /// höchste Wert die Komfortzeit. <see cref="double.NaN"/>, wenn die Vorlage keinen Heizsollwert trägt.
        /// </summary>
        public static double Tagwert(Konditionierungsstand inhalt, Konditionierungsgroesse quelle)
        {
            if (inhalt == null) throw new ArgumentNullException(nameof(inhalt));
            Matrixzelle tag = inhalt.Vorgabe(quelle, DbWerte.KOND_ZEILE_TAG);
            if (tag.Belegt && !tag.Aus && double.IsFinite(tag.Wert)) return tag.Wert;

            double hoechst = double.NaN;
            void Nehmen(double w)
            {
                if (double.IsFinite(w) && (double.IsNaN(hoechst) || w > hoechst)) hoechst = w;
            }
            void Angabe(Kalenderangabe a)
            {
                if (a == null) return;
                if (a.Art == Angabeart.Wert) Nehmen(a.Wert);
                else if (a.Art == Angabeart.Woche) foreach (double w in a.Woche) Nehmen(w);
            }
            foreach (string zeile in NUTZUNGSZEILEN)
            {
                Matrixzelle z = inhalt.Vorgabe(quelle, zeile);
                if (z.Belegt && !z.Aus) Nehmen(z.Wert);
            }
            Konditionierungskalender k = inhalt.Kalender(quelle);
            if (k != null)
            {
                Angabe(k.Grundangabe);
                foreach (Kalenderregel r in k.Perioden)
                    if (!Konditionierungsarbeit.IstMatrixbereich(r)) Angabe(r.Angabe);
            }
            return hoechst;
        }

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
        /// Kalenders Zeichen für Zeichen; Komfort- und Absenksollwert spielen keine Rolle. <b>Zeitstruktur</b>
        /// (Heizen → Kühlen): Eine Zelle, Grundangabe, Wochenstunde oder Periode, deren Heizsollwert unter dem
        /// <see cref="Tagwert"/> liegt (Absenkzeit), bekommt den Absenksollwert bzw. „aus"; jede andere mit
        /// Sollwert den Komfortsollwert. „aus" bleibt „aus", eine Zelle nur mit Zeiten bleibt eine Zelle nur mit
        /// Zeiten, „wie Wochentag X" bleibt; die Herkunft des Kalenders fällt — ihr Werkzeugvermerk nennte
        /// Heizwerte.</para>
        /// </summary>
        /// <param name="inhalt">Der Inhalt der Quelle (Vorgabezellen und höchstens ein Kalender ihrer Größe).</param>
        /// <param name="quelle">Die Größe der Quelle.</param>
        /// <param name="ziel">Die Zielgröße.</param>
        /// <param name="komfortsollwert">Der Komfortsollwert [°C] bei Heizen → Kühlen; sonst ohne Bedeutung.</param>
        /// <param name="absenksollwert">
        /// Der Absenksollwert [°C] bei Heizen → Kühlen, <see cref="ABSENKSOLLWERT_AUS"/> = „aus"; sonst ohne Bedeutung.
        /// </param>
        public static Ebenenergebnis Umsetzen(Konditionierungsstand inhalt, Konditionierungsgroesse quelle,
                                             Konditionierungsgroesse ziel, double? komfortsollwert, double? absenksollwert)
        {
            if (inhalt == null) throw new ArgumentNullException(nameof(inhalt));
            string regel = Pruefen(quelle, ziel, komfortsollwert, absenksollwert);
            if (regel != null) return Ebenenergebnis.Fehler(regel);

            Vorlagenkopierweg weg = Weg(quelle, ziel);
            Kuehlwerte kuehl = weg == Vorlagenkopierweg.Zeitstruktur
                ? new Kuehlwerte(komfortsollwert.Value, absenksollwert.Value, Tagwert(inhalt, quelle))
                : default;

            Konditionierungsstand v = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null);
            foreach (string zeile in NUTZUNGSZEILEN)
            {
                Matrixzelle alt = inhalt.Vorgabe(quelle, zeile);
                if (!Konditionierungsstand.Traegt(alt)) continue;
                Matrixzelle neu = weg == Vorlagenkopierweg.Direkt ? alt : Zeitstrukturzelle(alt, kuehl);
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
                    perioden.Add(weg == Vorlagenkopierweg.Direkt ? r : MitAngabe(r, Zeitstrukturangabe(r.Angabe, kuehl)));
                }
                Kalenderangabe grund = weg == Vorlagenkopierweg.Direkt ? k.Grundangabe : Zeitstrukturangabe(k.Grundangabe, kuehl);
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
        //  Heizen → Kühlen: Zeitstruktur und „aus" bleiben, Sollwerte werden Komfort- bzw. Absenksollwert
        // =================================================================

        /// <summary>
        /// Die Kühlwerte einer Kopie Heizen → Kühlen: Komfort- und Absenksollwert (NaN = „aus") und der Tagwert der
        /// Quelle, unter dem ein Heizsollwert eine Absenkzeit kennzeichnet.
        /// </summary>
        private readonly record struct Kuehlwerte(double Komfort, double Absenk, double Tagwert)
        {
            /// <summary>Der Kühlwert zu einem Heizsollwert; NaN = „aus".</summary>
            public double Zu(double heizwert)
                => !double.IsNaN(Tagwert) && heizwert < Tagwert - TOLERANZ ? Absenk : Komfort;
        }

        /// <summary>Innerhalb dieser Spanne [K] gilt ein Heizwert als gleich dem Tagwert.</summary>
        private const double TOLERANZ = 1e-9;

        /// <summary>
        /// Eine Zelle: „aus" bleibt „aus" samt Zeiten, ein Sollwert wird Komfort- oder Absenksollwert bzw. „aus",
        /// Zeiten bleiben.
        /// </summary>
        private static Matrixzelle Zeitstrukturzelle(Matrixzelle z, Kuehlwerte k)
        {
            if (z.Aus) return Matrixzelle.Abgeschaltet(z.Von, z.Bis);
            if (!z.Belegt) return Matrixzelle.NurZeiten(z.Von, z.Bis);
            double w = k.Zu(z.Wert);
            return double.IsNaN(w) ? Matrixzelle.Abgeschaltet(z.Von, z.Bis) : Matrixzelle.AusWert(w, z.Von, z.Bis);
        }

        /// <summary>
        /// Eine Angabe: Wert → Komfort- oder Absenksollwert bzw. „aus", „aus" bleibt, die Woche Stunde für Stunde,
        /// „wie Wochentag" bleibt.
        /// </summary>
        private static Kalenderangabe Zeitstrukturangabe(Kalenderangabe a, Kuehlwerte k)
        {
            switch (a.Art)
            {
                case Angabeart.Wert:
                    double w = k.Zu(a.Wert);
                    return double.IsNaN(w) ? Kalenderangabe.Abgeschaltet : Kalenderangabe.AusWert(w);
                case Angabeart.Woche:
                    var woche = new double[Kalenderwoche.WOCHENWERTE];
                    for (int i = 0; i < woche.Length; i++)
                        woche[i] = double.IsNaN(a.Woche[i]) ? double.NaN : k.Zu(a.Woche[i]);
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

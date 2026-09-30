using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Woher „Zeitstruktur übernehmen" seine Stunden nimmt (Konzept Konditionierungsprofile 3.5; Entwurf
    /// KP2, Festlegung 11). Nicht zu verwechseln mit dem Record <c>Zeitstruktur</c> des Zapfprofils.
    /// </summary>
    public enum Zeitstrukturquelle
    {
        /// <summary>„wie Heizung": die Stunden der Standardwoche des Heizkalenders mit endlichem Wert ≥ Tagwert der Heizspalte.</summary>
        WieHeizung,

        /// <summary>„wie Anwesenheit": die Stunden der Standardwoche des Personenkalenders mit einem Anteil über 0.</summary>
        WieAnwesenheit,
    }

    /// <summary>
    /// <b>Die Werkzeuge der Kalenderkarte</b> (Konzept Konditionierungsprofile 3.5, Stufe KP1b):
    /// das <b>Zeitfenster</b> „Tage, von, bis, Wert" für die Standardwoche, die <b>Feiertage als
    /// Regel</b> (F11), <b>„Zeitstruktur übernehmen"</b> (Entwurf KP2, Festlegung 11) und die
    /// <b>Rangbänder</b>, in denen eine Periode ihren Platz bekommt (N1.61 Nr. 5).
    ///
    /// <para><b>Ohne Datenbank, ohne Uhr, ohne Zufall</b>: Jedes Werkzeug nimmt einen
    /// <see cref="Konditionierungskalender"/> und gibt einen neuen zurück — dieselben Eingaben,
    /// derselbe Kalender. Geschrieben wird er vom Controller
    /// (<see cref="KonditionierungsvorlageCtrl"/>), der dazu auch den <see cref="Werkzeugbefund.Vermerk"/>
    /// in die Spalte <c>Bemerkung</c> setzt: Ein Werkzeug hinterlässt keine Sonderregel, sondern
    /// gewöhnliche Perioden und einen lesbaren Vermerk (Konzept 3.5).</para>
    ///
    /// <para><b>Jedes Werkzeug ersetzt genau seinen Zielbereich.</b> Das Zeitfenster fasst nur die
    /// Stunden seiner Tage an, die Feiertagsregel nur ihre neun Perioden, die Zeitstruktur nur die
    /// Standardwoche; alles andere bleibt Zeichen für Zeichen stehen. Ein Verstoß — ein Wert außerhalb
    /// der Grenzen, ein leeres Fenster, ein schon belegter Rang, ein fehlender Tag- oder Nachtwert — ist
    /// ein <b>benannter</b> Befund, keine stille Verschiebung.</para>
    /// </summary>
    public static class Kalenderwerkzeuge
    {
        /// <summary>
        /// Was ein Werkzeug ergeben hat: der neue Kalender und der Vermerk für <c>Bemerkung</c>,
        /// oder die benannte Ablehnung.
        /// </summary>
        /// <param name="Ok">Hat das Werkzeug gegriffen?</param>
        /// <param name="Kalender">Der neue Kalender; <c>null</c> im Fehlerfall.</param>
        /// <param name="Vermerk">Der lesbare Vermerk für die Spalte <c>Bemerkung</c> (Konzept 3.5).</param>
        /// <param name="Meldung">Die benannte Ablehnung; leer im guten Fall.</param>
        public sealed record Werkzeugbefund(bool Ok, Konditionierungskalender Kalender, string Vermerk,
                                            string Meldung)
        {
            /// <summary>Der gute Fall.</summary>
            public static Werkzeugbefund Gut(Konditionierungskalender kalender, string vermerk)
                => new Werkzeugbefund(true, kalender, vermerk ?? "", "");

            /// <summary>Der benannte Fehlschlag.</summary>
            public static Werkzeugbefund Fehler(string meldung)
                => new Werkzeugbefund(false, null, "", meldung ?? "");
        }

        // =================================================================
        //  Das Zeitfenster „Tage, von, bis, Wert" (Konzept 3.5)
        // =================================================================

        /// <summary>
        /// <b>Setzt einen Wert in ein Zeitfenster der Standardwoche</b> und lässt alles andere stehen.
        ///
        /// <para>Führt der Kalender noch keine Standardwoche, entsteht sie aus seiner Grundangabe —
        /// ein Wert füllt alle 168 Zellen, „aus" füllt sie mit <see cref="double.NaN"/>. So ist das
        /// Zeitfenster auch auf einem konstanten Kalender anwendbar, ohne dass eine Stunde außerhalb
        /// des Fensters ihren Wert wechselt.</para>
        /// </summary>
        /// <param name="kalender">Der Kalender, dessen Woche das Fenster bekommt.</param>
        /// <param name="wochentage">
        /// Die Tage 0 = Montag … 6 = Sonntag; <c>null</c> oder leer heißt <b>alle sieben</b>.
        /// </param>
        /// <param name="von">Die erste Stunde 0 … 23 (einschließlich).</param>
        /// <param name="bis">
        /// Die Stunde, vor der das Fenster endet, 1 … 24 (ausschließlich, wie
        /// <see cref="Nachtzeit"/>); <paramref name="bis"/> <b>unter</b> <paramref name="von"/> heißt
        /// <b>über Mitternacht</b> (22 … 6 sind die acht Stunden 22, 23, 0 … 5), <c>0 … 24</c> ist
        /// der ganze Tag. Gleiche Werte wären ein Fenster von null Stunden und werden abgelehnt.
        /// </param>
        /// <param name="wert">Der Wert in den Grenzen der Größe; <c>null</c> heißt „aus".</param>
        public static Werkzeugbefund Zeitfenster(Konditionierungskalender kalender,
                                                 IReadOnlyList<int> wochentage, int von, int bis,
                                                 double? wert)
        {
            if (kalender == null) throw new ArgumentNullException(nameof(kalender));

            List<int> tage = Tage(wochentage, out string tagfehler);
            if (tagfehler != null) return Werkzeugbefund.Fehler(tagfehler);

            List<int> stunden = Stunden(von, bis, out string stundenfehler);
            if (stundenfehler != null) return Werkzeugbefund.Fehler(stundenfehler);

            double zelle = double.NaN;
            if (wert.HasValue)
            {
                if (!Konditionierungsgroessen.ImBereich(kalender.Groesse, wert.Value))
                    return Werkzeugbefund.Fehler(string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KOND_MSG_WERT_AUSSERHALB,
                        Zahltext(wert.Value), Konditionierungsgroessen.Bereichstext(kalender.Groesse)));
                // Der Rundlauf ist die Invariante hinter „Anlegen aendert keine Reihe" (Konzept 3.3
                // Regel 1): Ein Wert, der ueber den Text der Wochenspalte nicht bitgleich
                // zurueckkommt, wird hier benannt abgelehnt - nicht erst beim Schreiben.
                if (!Kalenderwoche.Rundlauf(wert.Value))
                    return Werkzeugbefund.Fehler(string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KOND_MSG_WERT_RUNDLAUF,
                        Zahltext(wert.Value),
                        Kalenderwoche.NACHKOMMASTELLEN.ToString(CultureInfo.InvariantCulture)));
                zelle = wert.Value;
            }

            double[] woche = WocheAus(kalender);
            foreach (int t in tage)
                foreach (int s in stunden)
                    woche[Kalenderwoche.Stelle(t, s)] = zelle;

            var neu = new Konditionierungskalender(kalender.Groesse, Kalenderangabe.AusWoche(woche),
                                                   kalender.Nennwert, kalender.Perioden);
            string vermerk = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.KOND_MSG_WERKZEUG_ZEITFENSTER,
                Tagtext(tage), Zahl(von), Zahl(bis),
                wert.HasValue ? Zahltext(wert.Value) : DbWerte.KOND_WOCHE_AUS);
            return Werkzeugbefund.Gut(neu, vermerk);
        }

        // =================================================================
        //  Die Feiertage als Regel (F11, Konzept 3.5)
        // =================================================================

        /// <summary>
        /// <b>Legt die neun bundeseinheitlichen Feiertage als Regeln an</b> — je Regel eine Periode
        /// der Art <see cref="DbWerte.KOND_ART_FEIERTAG"/> mit
        /// <see cref="Angabeart.WieWochentag"/> und dem festen Rang
        /// <see cref="Standardfahrplan.RANG_FEIERTAG"/> + k.
        ///
        /// <para><b>Eine vorhandene Regel wird nicht doppelt angelegt</b> (gleich, welchen Rang und
        /// welche Angabe sie trägt) — das Werkzeug ist wiederholbar. Belegt eine <b>andere</b>
        /// Periode den Rang einer noch fehlenden Regel, wird benannt abgelehnt.</para>
        ///
        /// <para><b>Unter den Ferien</b> (Band 100 … 108): Ein Feiertag in den Ferien behält den
        /// Ferienwert, statt „wie Sonntag" zu heizen (N1.61 Nr. 5).</para>
        /// </summary>
        /// <param name="kalender">Der Kalender, der die Regeln bekommt.</param>
        /// <param name="wieWochentag">
        /// Der Wochentag 1 = Montag … 7 = Sonntag, dessen Stunden an einem Feiertag gelten; Vorgabe
        /// ist <b>7</b> (Sonntag).
        /// </param>
        public static Werkzeugbefund Feiertagsregeln(Konditionierungskalender kalender, int wieWochentag = 7)
        {
            if (kalender == null) throw new ArgumentNullException(nameof(kalender));
            if (wieWochentag < 1 || wieWochentag > 7)
                return Werkzeugbefund.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_WOCHENTAG_UNGUELTIG, Zahl(wieWochentag)));

            var vorhandeneRegeln = new HashSet<string>(StringComparer.Ordinal);
            var belegteRaenge = new HashSet<int>();
            foreach (Kalenderregel r in kalender.Perioden)
            {
                belegteRaenge.Add(r.Rang);
                if (r.IstFeiertag) vorhandeneRegeln.Add(r.Feiertagsregel);
            }

            Kalenderangabe angabe = Kalenderangabe.AlsWochentag(wieWochentag);
            var perioden = new List<Kalenderregel>(kalender.Perioden);
            IReadOnlyList<string> namen = Feiertagsnamen();
            int angelegt = 0;

            for (int k = 0; k < DbWerte.KOND_FEIERTAGE.Count; k++)
            {
                string regel = DbWerte.KOND_FEIERTAGE[k];
                if (vorhandeneRegeln.Contains(regel)) continue;        // nicht doppelt

                int rang = Standardfahrplan.RANG_FEIERTAG + k;
                if (belegteRaenge.Contains(rang))
                    return Werkzeugbefund.Fehler(string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KOND_MSG_RANG_BELEGT, Zahl(rang), regel));

                perioden.Add(Kalenderregel.Feiertag(rang, k < namen.Count ? namen[k] : regel, regel, angabe));
                belegteRaenge.Add(rang);
                angelegt++;
            }

            if (perioden.Count > Kalenderregel.PERIODEN_MAX)
                return Werkzeugbefund.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_PERIODEN_ZU_VIELE,
                    Zahl(perioden.Count), Zahl(Kalenderregel.PERIODEN_MAX)));

            var neu = new Konditionierungskalender(kalender.Groesse, kalender.Grundangabe,
                                                   kalender.Nennwert, perioden);
            string vermerk = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.KOND_MSG_WERKZEUG_FEIERTAGE,
                Zahl(angelegt), Zahl(DbWerte.KOND_FEIERTAGE.Count));
            return Werkzeugbefund.Gut(neu, vermerk);
        }

        /// <summary>
        /// Die neun Feiertagsnamen in der Reihenfolge von <see cref="DbWerte.KOND_FEIERTAGE"/>,
        /// gelesen aus <c>KOND_TEXT_FEIERTAGE</c> (mit <c>;</c> getrennt). Sie werden der Bezeichner
        /// der Periode — der Text, den die Vorschau als Quelle nennt.
        /// </summary>
        public static IReadOnlyList<string> Feiertagsnamen()
        {
            string t = MyResource.Resource.KOND_TEXT_FEIERTAGE;
            return string.IsNullOrEmpty(t) ? Array.Empty<string>() : t.Split(';');
        }

        // =================================================================
        //  „Zeitstruktur übernehmen" (Konzept 3.5, Entwurf KP2 Festlegung 11)
        // =================================================================

        /// <summary>
        /// <b>Übernimmt die Zeitstruktur der Heizung oder der Anwesenheit in die Standardwoche von Kühlen,
        /// Lüftung oder Geräten</b> (Konzept 3.5; Entwurf KP2, Festlegung 11). Die Quelle bestimmt je
        /// Wochenstunde, ob sie „an" ist:
        /// <list type="bullet">
        /// <item><b>„wie Heizung"</b>: der Heizkalender trägt dort einen endlichen Wert ≥ dem Tagwert der
        /// Heizspalte (mit dem Zahlenrand, <see cref="Rechenrand.SchwelleErreicht"/>);</item>
        /// <item><b>„wie Anwesenheit"</b>: der Personenkalender trägt dort einen Anteil über 0.</item>
        /// </list>
        /// Gezählt wird die Standardwoche der Quelle, ohne sie ihre Grundangabe; „aus" ist nie „an".
        /// Die Stunden „an" bekommen den <b>Tagwert</b> des Ziels, die übrigen seinen <b>Nachtwert</b>.
        ///
        /// <para><b>Ersetzt wird nur die Standardwoche</b> — Perioden und Nennwert des Ziels bleiben
        /// Zeichen für Zeichen. Benannt abgelehnt wird: ein Ziel außer Kühlen, Lüftung und Geräten, ein
        /// fehlender Tag- oder Nachtwert (beim Kühlen darf er „aus" sein, sonst braucht er eine Zahl), ein
        /// Wert außerhalb der Grenzen oder ohne bitgleichen Rundlauf, und bei „wie Heizung" ein fehlender
        /// Tagwert der Heizspalte. Eine Quelle der falschen Größe ist ein Aufruferfehler.</para>
        /// </summary>
        /// <param name="ziel">Der Kalender, dessen Standardwoche ersetzt wird: Kühlen, Lüftung oder Geräte.</param>
        /// <param name="art">„wie Heizung" oder „wie Anwesenheit".</param>
        /// <param name="quelle">Der Heizkalender bzw. der Personenkalender, dessen Stunden übernommen werden.</param>
        /// <param name="heizTagwert">Der Tagwert der Heizspalte [°C]; nur „wie Heizung" braucht ihn.</param>
        /// <param name="tag">Die Tagzelle der Spalte des Ziels — ihr Wert gilt in den Stunden „an".</param>
        /// <param name="nacht">Die Nachtzelle der Spalte des Ziels — ihr Wert gilt in den übrigen Stunden.</param>
        /// <exception cref="ArgumentNullException">Ziel, Quelle oder eine Zelle fehlt.</exception>
        /// <exception cref="ArgumentException">Die Quelle trägt nicht die Größe ihrer Art (Heizen bzw. Personen).</exception>
        public static Werkzeugbefund ZeitstrukturUebernehmen(Konditionierungskalender ziel, Zeitstrukturquelle art,
                                                             Konditionierungskalender quelle, double? heizTagwert,
                                                             Matrixzelle tag, Matrixzelle nacht)
        {
            if (ziel == null) throw new ArgumentNullException(nameof(ziel));
            if (quelle == null) throw new ArgumentNullException(nameof(quelle));
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            if (nacht == null) throw new ArgumentNullException(nameof(nacht));
            bool wieHeizung = art == Zeitstrukturquelle.WieHeizung;
            Konditionierungsgroesse quellgroesse = wieHeizung ? Konditionierungsgroesse.Heizsoll : Konditionierungsgroesse.Personen;
            if (quelle.Groesse != quellgroesse)
                throw new ArgumentException("„" + art + "“ braucht einen Kalender der Größe " +
                                           Konditionierungsgroessen.Kennwort(quellgroesse) + ", übergeben ist " +
                                           Konditionierungsgroessen.Kennwort(quelle.Groesse) + ".", nameof(quelle));

            Konditionierungsgroesse g = ziel.Groesse;
            if (g != Konditionierungsgroesse.Kuehlsoll && g != Konditionierungsgroesse.Lueftung
                && g != Konditionierungsgroesse.Geraete)
                return Werkzeugbefund.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_ZEITSTRUKTUR_ZIEL, Konditionierungsgroessen.Kennwort(g)));

            bool ausErlaubt = g == Konditionierungsgroesse.Kuehlsoll;
            if (!Zielangabe(tag, ausErlaubt) || !Zielangabe(nacht, ausErlaubt))
                return Werkzeugbefund.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_ZEITSTRUKTUR_WERT_FEHLT, Konditionierungsgroessen.Kennwort(g)));
            string wertfehler = Wertpruefung(g, tag) ?? Wertpruefung(g, nacht);
            if (wertfehler != null) return Werkzeugbefund.Fehler(wertfehler);

            if (wieHeizung && !(heizTagwert.HasValue && double.IsFinite(heizTagwert.Value)))
                return Werkzeugbefund.Fehler(MyResource.Resource.KOND_MSG_ZEITSTRUKTUR_HEIZTAG_FEHLT);

            double[] stundenDerQuelle = WocheAus(quelle);
            double an = tag.Aus ? double.NaN : tag.Wert;
            double sonst = nacht.Aus ? double.NaN : nacht.Wert;
            var woche = new double[Kalenderwoche.WOCHENWERTE];
            int anzahl = 0;
            for (int i = 0; i < woche.Length; i++)
            {
                double q = stundenDerQuelle[i];
                bool gilt = double.IsFinite(q)
                            && (wieHeizung ? Rechenrand.SchwelleErreicht(q, heizTagwert.Value) : q > 0.0);
                woche[i] = gilt ? an : sonst;
                if (gilt) anzahl++;
            }

            var neu = new Konditionierungskalender(g, Kalenderangabe.AusWoche(woche), ziel.Nennwert, ziel.Perioden);
            string vermerk = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.KOND_MSG_WERKZEUG_ZEITSTRUKTUR,
                wieHeizung ? MyResource.Resource.KOND_MSG_ZEITSTRUKTUR_WIE_HEIZUNG
                           : MyResource.Resource.KOND_MSG_ZEITSTRUKTUR_WIE_ANWESENHEIT,
                Zahl(anzahl), Zellentext(tag), Zellentext(nacht));
            return Werkzeugbefund.Gut(neu, vermerk);
        }

        /// <summary>Trägt die Zelle eine Angabe, die das Ziel nehmen kann — eine Zahl, beim Kühlen auch „aus"?</summary>
        private static bool Zielangabe(Matrixzelle zelle, bool ausErlaubt)
            => zelle.Belegt && (zelle.Aus ? ausErlaubt : double.IsFinite(zelle.Wert));

        /// <summary>Die Grenzen der Größe und der Rundlauf der Wochenspalte — dieselben Regeln wie das Zeitfenster; <c>null</c> = gut.</summary>
        private static string Wertpruefung(Konditionierungsgroesse g, Matrixzelle zelle)
        {
            if (zelle.Aus) return null;
            if (!Konditionierungsgroessen.ImBereich(g, zelle.Wert))
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_WERT_AUSSERHALB,
                                     Zahltext(zelle.Wert), Konditionierungsgroessen.Bereichstext(g));
            if (!Kalenderwoche.Rundlauf(zelle.Wert))
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_WERT_RUNDLAUF,
                                     Zahltext(zelle.Wert),
                                     Kalenderwoche.NACHKOMMASTELLEN.ToString(CultureInfo.InvariantCulture));
            return null;
        }

        /// <summary>Der Wert einer Zelle im Vermerk: die Zahl invariant, „aus" als Kennwort (wie im Zeitfenster).</summary>
        private static string Zellentext(Matrixzelle zelle) => zelle.Aus ? DbWerte.KOND_WOCHE_AUS : Zahltext(zelle.Wert);

        // =================================================================
        //  Die Rangbänder (N1.61 Nr. 5, Entwurf KP1b Nr. 12)
        // =================================================================

        /// <summary>
        /// <b>Vergibt den Perioden Ränge im Band der eigenen und übernommenen Perioden</b>
        /// (<see cref="Standardfahrplan.RANG_EIGEN"/> … <see cref="Standardfahrplan.RANG_EIGEN_LETZTER"/>),
        /// in ihrer Reihenfolge und ohne einen belegten Rang zu nehmen. Ist im Band kein Platz mehr,
        /// wird <b>benannt abgelehnt</b>, statt eine Periode still über die Saison oder unter die
        /// Ferien zu schieben.
        /// </summary>
        /// <param name="perioden">Die Perioden, die einen Platz brauchen; <c>null</c> ergibt eine leere Liste.</param>
        /// <param name="belegteRaenge">
        /// Die Ränge, die schon vergeben sind — sie wachsen um die neu vergebenen mit, damit zwei
        /// Aufrufe hintereinander sich nicht überschreiben.
        /// </param>
        /// <param name="vergeben">Die Perioden mit ihren neuen Rängen.</param>
        /// <returns><c>null</c> im guten Fall, sonst die benannte Ablehnung.</returns>
        public static string ImEigenband(IEnumerable<Kalenderregel> perioden, ISet<int> belegteRaenge,
                                         out List<Kalenderregel> vergeben)
        {
            if (belegteRaenge == null) throw new ArgumentNullException(nameof(belegteRaenge));
            vergeben = new List<Kalenderregel>();
            if (perioden == null) return null;

            int naechster = Standardfahrplan.RANG_EIGEN;
            foreach (Kalenderregel r in perioden)
            {
                if (r == null) continue;
                while (naechster <= Standardfahrplan.RANG_EIGEN_LETZTER && belegteRaenge.Contains(naechster))
                    naechster++;
                if (naechster > Standardfahrplan.RANG_EIGEN_LETZTER)
                    return string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KOND_MSG_RANG_BAND_VOLL,
                        Zahl(Standardfahrplan.RANG_EIGEN), Zahl(Standardfahrplan.RANG_EIGEN_LETZTER));

                vergeben.Add(MitRang(r, naechster));
                belegteRaenge.Add(naechster);
                naechster++;
            }
            return null;
        }

        /// <summary>
        /// Dieselbe Periode mit einem anderen Rang — <see cref="Kalenderregel"/> ist unveränderlich,
        /// also entsteht eine neue über denselben Weg (Zeitraum oder Feiertagsregel).
        /// </summary>
        public static Kalenderregel MitRang(Kalenderregel r, int rang)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));
            return r.IstFeiertag
                ? Kalenderregel.Feiertag(rang, r.Bezeichner, r.Feiertagsregel, r.Angabe)
                : Kalenderregel.Zeitraum(rang, r.Art, r.Bezeichner, r.Beginn, r.Ende, r.Angabe);
        }

        /// <summary>
        /// <b>Kommt ein Rang doppelt vor?</b> Dann tragen zwei Perioden zwei Wahrheiten über
        /// dieselbe Stunde (Konzept 3.6); das wird benannt abgelehnt.
        /// </summary>
        /// <returns><c>null</c>, wenn jeder Rang einmal vorkommt, sonst die benannte Ablehnung.</returns>
        public static string Rangpruefung(IEnumerable<Kalenderregel> perioden)
        {
            if (perioden == null) return null;
            var raenge = new HashSet<int>();
            foreach (Kalenderregel r in perioden)
            {
                if (r == null) continue;
                if (!raenge.Add(r.Rang))
                    return string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KOND_MSG_RANG_DOPPELT, Zahl(r.Rang));
            }
            return null;
        }

        // =================================================================
        //  Kleine Helfer
        // =================================================================

        /// <summary>
        /// Die Standardwoche eines Kalenders als beschreibbare Kopie; führt er keine, entsteht sie
        /// aus der Grundangabe (Wert oder „aus").
        /// </summary>
        public static double[] WocheAus(Konditionierungskalender kalender)
        {
            if (kalender == null) throw new ArgumentNullException(nameof(kalender));
            var w = new double[Kalenderwoche.WOCHENWERTE];
            if (kalender.Grundangabe.Art == Angabeart.Woche)
            {
                for (int i = 0; i < w.Length; i++) w[i] = kalender.Standardwoche[i];
                return w;
            }
            double v = kalender.Grundangabe.Art == Angabeart.Aus ? double.NaN : kalender.Grundangabe.Wert;
            for (int i = 0; i < w.Length; i++) w[i] = v;
            return w;
        }

        /// <summary>Die Wochentage des Fensters, aufsteigend und ohne Dublette; leer heißt alle sieben.</summary>
        private static List<int> Tage(IReadOnlyList<int> wochentage, out string fehler)
        {
            fehler = null;
            var tage = new List<int>();
            if (wochentage == null || wochentage.Count == 0)
            {
                for (int t = 0; t < 7; t++) tage.Add(t);
                return tage;
            }
            foreach (int t in wochentage)
            {
                if (t < 0 || t > 6)
                {
                    fehler = string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KOND_MSG_WOCHENTAG_UNGUELTIG, Zahl(t + 1));
                    return tage;
                }
                if (!tage.Contains(t)) tage.Add(t);
            }
            tage.Sort();
            return tage;
        }

        /// <summary>
        /// Die Stunden des Fensters [von, bis) — über Mitternacht, wenn <paramref name="bis"/> unter
        /// <paramref name="von"/> liegt. <c>0 … 24</c> ist der ganze Tag, <c>von = bis</c> wäre ein
        /// Fenster von null Stunden und wird abgelehnt.
        /// </summary>
        private static List<int> Stunden(int von, int bis, out string fehler)
        {
            fehler = null;
            var stunden = new List<int>();
            if (von < 0 || von > 23 || bis < 1 || bis > 24 || von == bis)
            {
                fehler = string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_ZEITFENSTER_UNGUELTIG, Zahl(von), Zahl(bis));
                return stunden;
            }
            int ende = bis % 24;
            for (int s = von; ; s = (s + 1) % 24)
            {
                stunden.Add(s);
                if ((s + 1) % 24 == ende) break;
            }
            return stunden;
        }

        /// <summary>Die Tage als Text („Mo, Di, Mi"), aus <c>KOND_TEXT_WOCHENTAGE</c>.</summary>
        private static string Tagtext(IReadOnlyList<int> tage)
        {
            string[] namen = (MyResource.Resource.KOND_TEXT_WOCHENTAGE ?? "").Split(';');
            var teile = new List<string>(tage.Count);
            foreach (int t in tage)
                teile.Add(t >= 0 && t < namen.Length ? namen[t] : Zahl(t + 1));
            return string.Join(", ", teile);
        }

        private static string Zahl(int n) => n.ToString(CultureInfo.InvariantCulture);

        private static string Zahltext(double w) => w.ToString("G6", CultureInfo.InvariantCulture);
    }
}

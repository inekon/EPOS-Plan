using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>Was die Erzeugung eines Standardfahrplans ergibt (<see cref="Standardfahrplan.Erzeugen"/>).</summary>
    public enum Fahrplanbefund
    {
        /// <summary>Der Kalender steht.</summary>
        Erzeugt,

        /// <summary>Die Spalte trägt keine Angabe — es gibt nichts zu erzeugen (der Bestandszweig gilt).</summary>
        KeineAngabe,

        /// <summary>Die Nachtzeit ist ungültig (nur eine Stunde gesetzt, außerhalb 0 … 23 oder Beginn = Ende).</summary>
        NachtzeitUngueltig,

        /// <summary>Ein Ferienzeitraum hat einen Tag außerhalb 1 … 365 (derselbe Grund wie im Bestandsfahrplan).</summary>
        FerienzeitraumUngueltig,

        /// <summary>Die Saison hat nur einen von zwei Tagen oder einen Tag außerhalb 1 … 365 (E53).</summary>
        SaisonUngueltig,

        /// <summary>Das Sollwert-Zeitprogramm der Anlagenkopplung ist ungültig (168 Werte, H-F10).</summary>
        SollwertprofilUngueltig,

        /// <summary>Ein Wert liegt außerhalb der Grenzen seiner Größe (Konzept 3.6).</summary>
        WertAusserhalb,

        /// <summary>Der Luftwechsel stammt aus der Gesamtangabe <c>Luftwechselrate</c>; eine Lüftungsvorgabe verlangt die getrennte Angabe (F15).</summary>
        LuftwechselOhneTrennung,

        /// <summary>Ein Wert läuft nicht rund (Wert → Text → Wert nicht bitgleich); „Anlegen" würde die Reihe verschieben (Konzept 3.3 Regel 1).</summary>
        RundlaufVerletzt,
    }

    /// <summary>Der Befund einer Erzeugung samt Kalender und Fundstelle — unveränderlich, ohne Ausnahme.</summary>
    public sealed class Fahrplanlesung
    {
        internal Fahrplanlesung(Fahrplanbefund befund, Konditionierungskalender kalender,
                                Konditionierungsgroesse groesse, string stelle)
        {
            Befund = befund;
            Kalender = kalender;
            Groesse = groesse;
            Stelle = stelle;
        }

        /// <summary>Der Befund.</summary>
        public Fahrplanbefund Befund { get; }

        /// <summary>Der erzeugte Kalender bei <see cref="Fahrplanbefund.Erzeugt"/>, sonst <c>null</c>.</summary>
        public Konditionierungskalender Kalender { get; }

        /// <summary>Die Größe, um die es geht.</summary>
        public Konditionierungsgroesse Groesse { get; }

        /// <summary>Die Fundstelle als sprachneutraler Text (Zeile, Zeitraum, Stelle) oder <c>null</c>.</summary>
        public string Stelle { get; }

        /// <summary>Größe und Fundstelle für die Meldung.</summary>
        public string Fundstelle()
            => Konditionierungsgroessen.Kennwort(Groesse) +
               (string.IsNullOrEmpty(Stelle) ? string.Empty : ", " + Stelle);
    }

    /// <summary>
    /// <b>Der Generator: aus der Vorgabe-Matrix wird ein Kalender</b> (Konzept
    /// Konditionierungsprofile 3.3). Er ist zugleich der <b>abgeleitete Fahrplan</b> (ohne angelegten
    /// Kalender rechnet der Lauf aus der Matrix) und das <b>„Kalender anlegen"</b> des Dialogs —
    /// EINE Vorschrift, damit „Anlegen" keine Reihe ändert.
    ///
    /// <para><b>Bitgleich mit dem Bestandsfahrplan</b> (Konzept 3.3, „Bitgleich"): Für die Heizspalte
    /// gelten die heutigen Regeln <em>wörtlich</em> — der Wochenendwert ist absolut und wirkt nur
    /// über <see cref="GebaeudeFestwerte.WOCHENENDE_SOLLWERT_SCHWELLE"/> = 5 °C, Sa und So dann
    /// ganztägig; der Ferienwert wirkt ab <see cref="GebaeudeFestwerte.FERIEN_SOLLWERT_MIN"/> = 1 °C
    /// und nur mit dem Merker über <see cref="GebaeudeFestwerte.FERIEN_FLAG_SCHWELLE"/> = 0,9; ein
    /// Ferienzeitraum mit 0 oder 366 an einer Grenze ist „aus"; der Rang ist Ferien über Wochenende
    /// über Tag/Nacht. Mit wirksamer Kopplung (AK1) ersetzt <c>Sollwertprofil</c> die Standardwoche,
    /// die Ferien bleiben darüber.</para>
    ///
    /// <para><b>Die Nachtzeit wird streng geprüft</b> — mit der Prüfung des Laufs statt mit dem
    /// Rückfall von <c>Bestandswoche</c>: Eine halbe Angabe ist ein benannter Befund, keine stille
    /// Vorgabe (Konzept 3.3).</para>
    ///
    /// <para>Ohne Datenbank, ohne Uhr, ohne Zufall, <see cref="CultureInfo.InvariantCulture"/>.</para>
    /// </summary>
    public static class Standardfahrplan
    {
        // =================================================================
        //  Die Rangbänder — die EINE Stelle (N1.61 Nr. 5, Entwurf KP1b Nr. 12)
        // =================================================================
        //
        // Der groessere Rang gewinnt. Die vier Baender liegen so uebereinander, dass jede
        // Ebene die schwaechere schlaegt und keine eine staerkere verdeckt:
        //
        //   100 … 108   Feiertage als Regel   (neun, UNTER den Ferien)
        //   200 … 203   Ferien 200 + k        (der Generator, vier Zeitraeume)
        //   310 … 899   eigene und uebernommene Perioden
        //   900         Saison (Betriebspause)
        //
        // WARUM DIE FEIERTAGE UNTEN LIEGEN: Ein Feiertag IN den Ferien soll den Ferienwert
        // behalten. Stuenden die Feiertage ueber den Ferien, gaelte an ihnen „wie Sonntag"
        // — bei Ferien „aus" und Sonntag 16 Grad wuerde also mitten in den Ferien geheizt.

        /// <summary>Der Rang der ersten Feiertagsregel; die neun bekommen 100 + k — <b>unter</b> den Ferien.</summary>
        public const int RANG_FEIERTAG = 100;

        /// <summary>Der Rang der letzten Feiertagsregel (<see cref="RANG_FEIERTAG"/> + 8).</summary>
        public const int RANG_FEIERTAG_LETZTER = RANG_FEIERTAG + 8;

        /// <summary>Der Rang der ersten Ferienperiode; die vier Zeiträume bekommen 200 + k (Konzept 3.3).</summary>
        public const int RANG_FERIEN = 200;

        /// <summary>Der kleinste Rang einer eigenen oder übernommenen Periode — <b>über</b> den Ferien.</summary>
        public const int RANG_EIGEN = 310;

        /// <summary>Der größte Rang einer eigenen oder übernommenen Periode — <b>unter</b> der Saison.</summary>
        public const int RANG_EIGEN_LETZTER = 899;

        /// <summary>Der Rang der Betriebspause aus der Saisonzeile — <b>über</b> den Perioden der Matrix (Konzept 3.2, 3.3).</summary>
        public const int RANG_SAISON = 900;

        /// <summary>Der Bezeichner einer erzeugten Ferienperiode, ohne Nummer.</summary>
        public const string BEZEICHNER_FERIEN = "Ferien";

        /// <summary>Der Bezeichner der erzeugten Betriebspause (Heiz- bzw. Kühlperiode, E53).</summary>
        public const string BEZEICHNER_SAISON = "Saison";

        /// <summary>
        /// <b>Erzeugt den Kalender einer Größe aus der Matrix.</b> Trägt die Spalte keine Angabe, ist
        /// der Befund <see cref="Fahrplanbefund.KeineAngabe"/> — dann nimmt der Lauf wörtlich den
        /// Bestandszweig (Konzept 6).
        /// </summary>
        /// <param name="matrix">Die wirksame Matrix des Eigentümers (nach der Kaskade, F2).</param>
        /// <param name="groesse">Die Größe, deren Kalender entsteht.</param>
        /// <param name="rundlaufPruefen">
        /// <c>true</c> beim „Kalender anlegen": Jeder Wert muss Wert → Text → Wert bitgleich
        /// zurückkommen, sonst <see cref="Fahrplanbefund.RundlaufVerletzt"/>. Im Lauf (abgeleitet)
        /// ist die Prüfung nicht nötig — dort wird nichts geschrieben.
        /// </param>
        public static Fahrplanlesung Erzeugen(Vorgabematrix matrix, Konditionierungsgroesse groesse,
                                              bool rundlaufPruefen)
        {
            if (matrix == null) throw new ArgumentNullException(nameof(matrix));
            Matrixspalte s = matrix.Spalte(groesse);
            Matrixeingang b = matrix.Bestand;

            // ---- Die Standardwoche der Spalte ----
            double[] woche;
            Fahrplanlesung fehler = Woche(matrix, s, groesse, out woche);
            if (fehler != null) return fehler;

            Kalenderangabe grund;
            if (woche != null) grund = Kalenderangabe.AusWoche(woche);
            else if (s.Tag.Belegt && s.Tag.Aus) grund = Kalenderangabe.Abgeschaltet;
            else if (s.Tag.Belegt) grund = Kalenderangabe.AusWert(s.Tag.Wert);
            else return Kein(groesse);

            // ---- Ebene 3: Ferien und Saison als Perioden ----
            var perioden = new List<Kalenderregel>();
            fehler = Ferienperioden(b, s, groesse, perioden);
            if (fehler != null) return fehler;
            fehler = Saisonperiode(s, groesse, perioden);
            if (fehler != null) return fehler;

            double? nennwert = Konditionierungsgroessen.HatNennwert(groesse) && s.Nennwert.Belegt
                ? s.Nennwert.Wert
                : (double?)null;

            var kalender = new Konditionierungskalender(groesse, grund, nennwert, perioden);
            if (rundlaufPruefen && !Kalenderleser.Rundlaeuft(kalender, out int rang, out int stelle))
                return new Fahrplanlesung(Fahrplanbefund.RundlaufVerletzt, null, groesse,
                                          "Rang " + Z(rang) + ", Stelle " + Z(stelle));
            return new Fahrplanlesung(Fahrplanbefund.Erzeugt, kalender, groesse, null);
        }

        /// <summary>
        /// Erzeugt die Kalender <b>aller fünf Größen</b>, die eine Angabe tragen; Größen ohne Angabe
        /// fehlen im Ergebnis (dann gilt der Bestandszweig). Bricht beim ersten Befund ab, der kein
        /// <see cref="Fahrplanbefund.Erzeugt"/> und kein <see cref="Fahrplanbefund.KeineAngabe"/> ist.
        /// </summary>
        public static Fahrplanlesung ErzeugenAlle(Vorgabematrix matrix, bool rundlaufPruefen,
                                                  IDictionary<Konditionierungsgroesse, Konditionierungskalender> ziel)
        {
            if (ziel == null) throw new ArgumentNullException(nameof(ziel));
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                Fahrplanlesung l = Erzeugen(matrix, g, rundlaufPruefen);
                if (l.Befund == Fahrplanbefund.KeineAngabe) continue;
                if (l.Befund != Fahrplanbefund.Erzeugt) return l;
                ziel[g] = l.Kalender;
            }
            return new Fahrplanlesung(Fahrplanbefund.Erzeugt, null, Konditionierungsgroesse.Heizsoll, null);
        }

        // =================================================================
        //  Die Standardwoche
        // =================================================================

        /// <summary>
        /// Baut die Standardwoche der Spalte: werktags der Tagwert und im Nachtfenster der
        /// Nachtwert, Sa und So ganztags der Wochenendwert. <paramref name="woche"/> bleibt
        /// <c>null</c>, wenn die Spalte weder Nacht- noch Wochenendwert trägt — dann ist die
        /// Grundangabe ein Wert, und der Kalender ist konstant (bitgleich mit der heutigen
        /// Konstante).
        /// </summary>
        private static Fahrplanlesung Woche(Vorgabematrix matrix, Matrixspalte s,
                                            Konditionierungsgroesse groesse, out double[] woche)
        {
            woche = null;
            Matrixeingang b = matrix.Bestand;

            // ---- AK1: das Sollwert-Zeitprogramm ersetzt Tag, Nacht und Wochenende der Heizspalte ----
            if (groesse == Konditionierungsgroesse.Heizsoll && b.KopplungWirksam &&
                !string.IsNullOrWhiteSpace(b.Sollwertprofil))
            {
                AnlagenkopplungSchema.Wochenprofil p = AnlagenkopplungSchema.WochenprofilLesen(b.Sollwertprofil);
                if (p.Befund != AnlagenkopplungSchema.WochenprofilBefund.Gelesen)
                    return new Fahrplanlesung(Fahrplanbefund.SollwertprofilUngueltig, null, groesse,
                                              "Stelle " + Z(p.Stelle) + ", " + Z(p.Gefunden) + " Werte");
                for (int i = 0; i < p.Werte.Length; i++)
                    if (!Konditionierungsgroessen.ImBereich(groesse, p.Werte[i]))
                        return new Fahrplanlesung(Fahrplanbefund.SollwertprofilUngueltig, null, groesse,
                                                  "Stelle " + Z(i + 1));
                woche = p.Werte;
                return null;
            }

            // ---- Die Lüftung verlangt die getrennte Angabe, sobald sie eine Vorgabe trägt (F15) ----
            if (groesse == Konditionierungsgroesse.Lueftung && b.LuftwechselAusGesamtangabe &&
                (s.Nacht.Belegt || s.Wochenende.Belegt || s.Ferien.Belegt))
                return new Fahrplanlesung(Fahrplanbefund.LuftwechselOhneTrennung, null, groesse, null);

            bool nachtWirksam = Wirksam(s.Nacht, groesse);
            bool weWirksam = Wirksam(s.Wochenende, groesse);
            if (!nachtWirksam && !weWirksam) return null;      // konstante Grundangabe

            // Das Nachtfenster der Spalte; leer heißt das der Heizspalte (F19), und deren leeres
            // heißt die Vorgabe 22-6 Uhr.
            int? von = s.Nacht.Von ?? matrix.Heizsoll.Nacht.Von ?? b.NachtBeginn;
            int? bis = s.Nacht.Bis ?? matrix.Heizsoll.Nacht.Bis ?? b.NachtEnde;
            NachtzeitBefund nb = Nachtzeit.Pruefen(von, bis);
            if (nb != NachtzeitBefund.Gueltig)
                return new Fahrplanlesung(Fahrplanbefund.NachtzeitUngueltig, null, groesse, nb.ToString());
            Nachtzeit nacht = Nachtzeit.Aus(von, bis);

            double tag = Zellenwert(s.Tag, groesse, out bool tagBelegt);
            if (!tagBelegt) return Kein(groesse);
            double nachtwert = nachtWirksam ? Zellenwert(s.Nacht, groesse, out _) : tag;
            double wochenende = weWirksam ? Zellenwert(s.Wochenende, groesse, out _) : double.NaN;

            woche = new double[Kalenderwoche.WOCHENWERTE];
            for (int w = 0; w < 7; w++)
                for (int st = 0; st < Kalenderwoche.TAGESSTUNDEN; st++)
                    woche[Kalenderwoche.Stelle(w, st)] =
                        w >= 5 && weWirksam ? wochenende
                        : nacht.IstNacht(st) ? nachtwert
                        : tag;
            return null;
        }

        // =================================================================
        //  Ferien und Saison als Perioden
        // =================================================================

        /// <summary>
        /// Die Ferienzeiträume des Gebäudes werden Perioden der Art <c>FERIEN</c> mit Rang 200 + k.
        /// <b>Wörtlich wie der Bestandsfahrplan:</b> Sie entstehen nur, wenn die Ferienzeile der
        /// Spalte wirksam ist — bei der Heizspalte heißt das Merker über 0,9 <em>und</em> Wert ab
        /// 1 °C —, eine Grenze mit 0 oder 366 ist „aus", und ein anderer Tag außerhalb 1 … 365 ist
        /// derselbe benannte Fehler wie heute.
        /// </summary>
        private static Fahrplanlesung Ferienperioden(Matrixeingang b, Matrixspalte s,
                                                     Konditionierungsgroesse groesse,
                                                     ICollection<Kalenderregel> perioden)
        {
            if (!FerienWirksam(b, s, groesse)) return null;

            Kalenderangabe angabe = s.Ferien.Aus
                ? Kalenderangabe.Abgeschaltet
                : Kalenderangabe.AusWert(Zellenwert(s.Ferien, groesse, out _));

            for (int k = 0; k < Matrixeingang.FERIENZEITRAEUME; k++)
            {
                double von = b.Ferienbeginn[k], bis = b.Ferienende[k];
                if (AusTag(von) || AusTag(bis)) continue;
                if (!Jahrestag(von) || !Jahrestag(bis))
                    return new Fahrplanlesung(Fahrplanbefund.FerienzeitraumUngueltig, null, groesse,
                                              BEZEICHNER_FERIEN + " " + Z(k + 1));
                perioden.Add(Kalenderregel.Zeitraum(RANG_FERIEN + k, DbWerte.KOND_ART_FERIEN,
                                                    BEZEICHNER_FERIEN + " " + Z(k + 1),
                                                    (int)von, (int)bis, angabe));
            }
            return null;
        }

        /// <summary>
        /// Die Saisonzeile wird <b>eine</b> Periode der Art <c>BETRIEBSPAUSE</c> mit „aus" über die
        /// Tage <b>außerhalb</b> von Start bis Ende — vom Tag nach dem Ende bis zum Tag vor dem
        /// Start, wo nötig über den Jahreswechsel (E53, Konzept 3.2). Leer heißt ganzjährig, ein
        /// Zeitraum über das ganze Jahr ebenso.
        /// </summary>
        private static Fahrplanlesung Saisonperiode(Matrixspalte s, Konditionierungsgroesse groesse,
                                                    ICollection<Kalenderregel> perioden)
        {
            int? von = s.Saison.Von, bis = s.Saison.Bis;
            if (!von.HasValue && !bis.HasValue) return null;                      // ganzjährig
            if (!von.HasValue || !bis.HasValue ||
                !Jahrestag(von.Value) || !Jahrestag(bis.Value))
                return new Fahrplanlesung(Fahrplanbefund.SaisonUngueltig, null, groesse,
                                          BEZEICHNER_SAISON + " " + Z(von ?? 0) + "…" + Z(bis ?? 0));

            // Das ganze Jahr: es gibt kein "ausserhalb" (Konzept 3.2).
            int aussenBeginn = bis.Value % Kalenderregel.TAG_MAX + 1;            // Tag nach dem Ende
            int aussenEnde = von.Value == 1 ? Kalenderregel.TAG_MAX : von.Value - 1;  // Tag vor dem Start
            if (von.Value == 1 && bis.Value == Kalenderregel.TAG_MAX) return null;

            perioden.Add(Kalenderregel.Zeitraum(RANG_SAISON, DbWerte.KOND_ART_BETRIEBSPAUSE, BEZEICHNER_SAISON,
                                                aussenBeginn, aussenEnde, Kalenderangabe.Abgeschaltet));
            return null;
        }

        // =================================================================
        //  Die Schwellen des Bestands — wörtlich (Konzept 3.3)
        // =================================================================

        /// <summary>
        /// Wirkt die Wochenend- bzw. Nachtzelle? Beim <b>Heizsollwert</b> gilt die Schwelle des
        /// Bestands: Ein Wochenendwert bis 5 °C heißt „Wochenende wie Werktag"
        /// (<see cref="GebaeudeFestwerte.WOCHENENDE_SOLLWERT_SCHWELLE"/>). Bei den übrigen Größen
        /// wirkt jede belegte Zelle — dort gibt es keine Bestandsschwelle, die zu erhalten wäre.
        /// </summary>
        private static bool Wirksam(Matrixzelle zelle, Konditionierungsgroesse groesse)
        {
            if (!zelle.Belegt) return false;
            if (zelle.Aus) return true;
            // Die Schwelle steht NICHT hier: Gebaeudemodellvorgaben traegt sie fuer Lauf, Dialog und
            // Generator zugleich (eine Quelle, kein abgeschriebener Vergleich).
            if (groesse == Konditionierungsgroesse.Heizsoll &&
                !Gebaeudemodellvorgaben.WochenendsollwertWirksam(zelle.Wert))
                return false;
            return true;
        }

        /// <summary>
        /// Wirkt die Ferienzeile? Beim <b>Heizsollwert</b> wörtlich wie heute: der Merker
        /// <c>Ferien</c> über <see cref="GebaeudeFestwerte.FERIEN_FLAG_SCHWELLE"/> <em>und</em> der
        /// Wert ab <see cref="GebaeudeFestwerte.FERIEN_SOLLWERT_MIN"/>. Bei den übrigen Größen
        /// genügt eine belegte Zelle.
        /// </summary>
        private static bool FerienWirksam(Matrixeingang b, Matrixspalte s, Konditionierungsgroesse groesse)
        {
            if (!s.Ferien.Belegt) return false;
            if (groesse != Konditionierungsgroesse.Heizsoll) return true;
            if (s.Ferien.Aus) return true;
            return b.Ferienmerker > GebaeudeFestwerte.FERIEN_FLAG_SCHWELLE
                   && Gebaeudemodellvorgaben.FeriensollwertWirksam(s.Ferien.Wert);
        }

        /// <summary>
        /// Der Wert einer Zelle; „aus" wird <see cref="double.NaN"/> — das Kennzeichen, das
        /// <see cref="Konditionierungskalender.Auswerten"/> in den Wert der Größe übersetzt. Leere
        /// Zellen der Anteilsgrößen heißen 100 % (Konzept 3.3).
        /// </summary>
        private static double Zellenwert(Matrixzelle zelle, Konditionierungsgroesse groesse, out bool belegt)
        {
            belegt = zelle.Belegt;
            if (zelle.Aus) return double.NaN;
            if (zelle.Belegt) return zelle.Wert;
            if (Konditionierungsgroessen.HatNennwert(groesse))
            {
                belegt = true;
                return Konditionierungsgroessen.ANTEIL_MAX;     // leer = 100 %
            }
            return double.NaN;
        }

        /// <summary>Ein Jahrestag, der „aus" heißt: 0 oder 366 an einer Grenze (Bestandsregel E8).</summary>
        private static bool AusTag(double tag) => tag == 0.0 || tag == 366.0;

        private static bool Jahrestag(double tag)
            => double.IsFinite(tag) && tag >= Kalenderregel.TAG_MIN && tag <= Kalenderregel.TAG_MAX
               && tag == Math.Floor(tag);

        private static Fahrplanlesung Kein(Konditionierungsgroesse g)
            => new Fahrplanlesung(Fahrplanbefund.KeineAngabe, null, g, null);

        private static string Z(int n) => n.ToString(CultureInfo.InvariantCulture);

        private static string Z(double d) => d.ToString("G6", CultureInfo.InvariantCulture);
    }
}

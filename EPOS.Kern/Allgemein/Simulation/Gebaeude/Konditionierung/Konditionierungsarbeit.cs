using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>Was ein Schritt an EINER Ebene ergeben hat: die neue Ebene oder die benannte Ablehnung.</summary>
    /// <param name="Stand">Die neue Ebene; <c>null</c> bei Ablehnung.</param>
    /// <param name="Meldung">Die benannte Ablehnung; <c>null</c> im guten Fall.</param>
    public sealed record Ebenenergebnis(Konditionierungsstand Stand, string Meldung)
    {
        /// <summary>Hat der Schritt gegriffen?</summary>
        public bool Ok => Meldung == null;

        /// <summary>Der gute Fall.</summary>
        public static Ebenenergebnis Gut(Konditionierungsstand stand) => new Ebenenergebnis(stand, null);

        /// <summary>Die benannte Ablehnung.</summary>
        public static Ebenenergebnis Fehler(string meldung) => new Ebenenergebnis(null, meldung ?? "");
    }

    /// <summary>
    /// <b>Die reinen Schritte der Konditionierung</b> (Stufe KP2, Welle K2; Entwurf KP2 Abschnitt 2):
    /// Zelle setzen, Anlegen, Verwerfen, Matrix erneut anwenden (P12), Vorlage übernehmen,
    /// Als-Vorlage-Inhalt (E54), die drei Werkzeuge der Karte und der Abdruck — <b>ohne Datenbank</b>.
    /// Jeder Schritt bekommt einen Stand und gibt einen NEUEN zurück; der alte bleibt, wie er war.
    /// </summary>
    /// <remarks>
    /// <para><b>Zwei Stufen.</b> Der <b>Kern je Ebene</b> (<see cref="Eintragen"/>,
    /// <see cref="KalenderAnlegen"/>, <see cref="KalenderVerwerfen"/>, <see cref="MatrixbereichErsetzen"/>,
    /// <see cref="VorlageEintragen"/>, <see cref="Werkzeug"/>, <see cref="AlsVorlageInhalt"/>) trägt die
    /// Regeln einer Zelle und eines Kalenders; die Schreibwege von KP1 in
    /// <see cref="KonditionierungCtrl"/> sind dünne Hüllen darüber (Lesen → Kern → Schreiben). Die
    /// <b>Schritte des Arbeitsstands</b> (<see cref="ZelleSetzen"/>, <see cref="Anlegen"/> …) setzen den
    /// Kern für den Editor zusammen — mit der Kaskade Zone → Gebäude, den Zonen, P1 und den
    /// Rückfragen.</para>
    /// <para>Die Oberfläche kennt diese Klasse nicht; die Hülle in <c>EPOS.UI.Daten</c> übersetzt
    /// ihr DTO in den Arbeitsstand und zurück.</para>
    /// </remarks>
    public static class Konditionierungsarbeit
    {
        // =================================================================
        //  Prüfen
        // =================================================================

        /// <summary>
        /// <b>Was an einer Zelle ohne Datenbank prüfbar ist</b> — das Zeilenkennwort, die Grenzen der
        /// Größe je Zeile (N1.61 Nr. 14) und die Zeiten, die die Vorgabetabelle je Zeile annimmt:
        /// Stunde 0 … 23 in der Nachtzeile, Tag 1 … 365 in der Saison (beide oder keiner, E53), sonst
        /// keine; ΔT allein an Lüftung/Nacht, 0 … 5 K (P9).
        /// </summary>
        /// <returns><c>null</c>, wenn die Zelle passt, sonst die benannte Ablehnung.</returns>
        public static string Zellenpruefung(Konditionierungsgroesse groesse, string zeile, Matrixzelle zelle)
        {
            if (zelle == null) throw new ArgumentNullException(nameof(zelle));
            if (Konditionierungsstand.Zeilenindex(zeile) < 0)
                return "Die Zeile „" + (zeile ?? "leer") + "“ ist keine der sechs.";

            if (zelle.Belegt && !zelle.Aus)
            {
                if (string.Equals(zeile, DbWerte.KOND_ZEILE_NENNWERT, StringComparison.Ordinal))
                {
                    if (Konditionierungsgroessen.HatNennwert(groesse))
                    {
                        if (!(zelle.Wert >= 0.0) || double.IsInfinity(zelle.Wert))
                            return "Der Nennwert " + Zahltext(zelle.Wert) + " W ist negativ oder nicht endlich.";
                    }
                    else if (!Konditionierungsgroessen.ImBereich(groesse, zelle.Wert))
                        return Ausserhalb(groesse, zelle.Wert);
                }
                else if (!string.Equals(zeile, DbWerte.KOND_ZEILE_SAISON, StringComparison.Ordinal)
                         && !Konditionierungsgroessen.ImBereich(groesse, zelle.Wert))
                    return Ausserhalb(groesse, zelle.Wert);
            }

            // Die Zeiten je Zeile - dieselbe Regel wie der CHECK der Vorgabetabelle (Konzept 5.6).
            bool nacht = string.Equals(zeile, DbWerte.KOND_ZEILE_NACHT, StringComparison.Ordinal);
            bool saison = string.Equals(zeile, DbWerte.KOND_ZEILE_SAISON, StringComparison.Ordinal);
            bool zeitenGut = nacht
                ? Stunde(zelle.Von) && Stunde(zelle.Bis)
                : saison
                    ? (!zelle.Von.HasValue && !zelle.Bis.HasValue)
                      || (Tag(zelle.Von) && Tag(zelle.Bis) && zelle.Von.HasValue && zelle.Bis.HasValue)
                    : !zelle.Von.HasValue && !zelle.Bis.HasValue;
            if (!zeitenGut)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_ZELLE_ZEITEN,
                                     Zahl(zelle.Von), Zahl(zelle.Bis), zeile);

            if (zelle.BedingtK.HasValue)
            {
                double k = zelle.BedingtK.Value;
                bool gut = groesse == Konditionierungsgroesse.Lueftung && nacht && double.IsFinite(k)
                           && k >= Nachtauskuehlvorgabe.ABSTAND_MIN_K && k <= Nachtauskuehlvorgabe.ABSTAND_MAX_K;
                if (!gut)
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_ZELLE_DELTA_T,
                                         Zahltext(k));
            }
            return null;
        }

        private static bool Stunde(int? s) => !s.HasValue || (s.Value >= 0 && s.Value <= 23);

        private static bool Tag(int? t) => !t.HasValue || (t.Value >= Kalenderregel.TAG_MIN && t.Value <= Kalenderregel.TAG_MAX);

        private static string Ausserhalb(Konditionierungsgroesse g, double w)
            => "Der Wert liegt außerhalb der Grenzen " + Konditionierungsgroessen.Bereichstext(g) + ".";

        // =================================================================
        //  Der Kern je Ebene
        // =================================================================

        /// <summary>
        /// <b>Trägt eine Zelle in eine Ebene ein</b> — die Weiche „ein Ort je Zelle"
        /// (<see cref="Matrixzellenort"/>, Konzept 5.6): Hat die Zelle am Eigentümer eine
        /// Bestandsspalte, geht ihr Zahlenwert in den <see cref="Konditionierungsstand.Bestand"/>, und
        /// die Vorgabezelle trägt nur „aus", die Zeiten und ΔT; sonst trägt die Vorgabezelle den Wert.
        /// An einer Zone heißt eine unbelegte Bestandszelle „wie das Gebäude" — ihr Feld wird leer;
        /// an Gebäude und Katalogbau lässt sie die Spalte stehen (die Spalte führt immer einen Wert).
        /// Die Prüfung (<see cref="Zellenpruefung"/>) hält der Aufrufer.
        ///
        /// <para><b>Nachtzeiten (B6, Festlegung 5):</b> An Gebäude und Katalogbau stehen die Zeiten der
        /// Heiz-Nachtzeile allein in <c>Nachtabsenkung_Beginn</c>/<c>_Ende</c> — die Vorgabezeile trägt
        /// sie nicht, so zeigen Matrix und Lauf dieselbe Nachtzeit; eine leere Zeit lässt die Spalte
        /// stehen. An der Zone bleiben sie in der Vorgabezeile und machen die Heizspalte wirksam.</para>
        /// <para><b>Merker (B7):</b> Ein Heizwert Wochenende bzw. Ferien setzt an Gebäude und Katalogbau
        /// den Merker nach der Regel des Dialogs (<see cref="Merker"/>).</para>
        /// </summary>
        public static Ebenenergebnis Eintragen(Konditionierungsstand ebene, Konditionierungsgroesse groesse,
                                              string zeile, Matrixzelle zelle)
        {
            if (ebene == null) throw new ArgumentNullException(nameof(ebene));
            if (zelle == null) throw new ArgumentNullException(nameof(zelle));
            if (Konditionierungsstand.Zeilenindex(zeile) < 0)
                return Ebenenergebnis.Fehler("Die Zeile „" + (zeile ?? "leer") + "“ ist keine der sechs.");

            Konditionierungsstand e = ebene;
            Matrixzelle vorgabe = zelle;
            if (Matrixzellenort.HatBestandsspalte(e.Art, groesse, zeile))
            {
                if (zelle.Belegt && !zelle.Aus)
                    e = e.MitBestand(b =>
                    {
                        Bestandswert(b, groesse, zeile, zelle.Wert);
                        Merker(b, ebene.Art, groesse, zeile);
                    });
                else if (!zelle.Belegt && e.Art == Kalendereigentuemer.Zone)
                    e = e.MitBestand(b => Bestandswert(b, groesse, zeile, null));

                int? von = zelle.Von, bis = zelle.Bis;
                if (NachtzeitImBestand(e.Art, groesse, zeile))
                {
                    if (von.HasValue || bis.HasValue)
                        e = e.MitBestand(b =>
                        {
                            if (von.HasValue) b.NachtBeginn = von;
                            if (bis.HasValue) b.NachtEnde = bis;
                        });
                    von = null;
                    bis = null;
                }
                vorgabe = zelle.Aus
                    ? Matrixzelle.Abgeschaltet(von, bis, zelle.BedingtK)
                    : Matrixzelle.NurZeiten(von, bis, zelle.BedingtK);
            }
            return Ebenenergebnis.Gut(e.MitVorgabe(groesse, zeile, vorgabe));
        }

        /// <summary>
        /// Stehen die Zeiten dieser Zeile in den Bestandsspalten <c>Nachtabsenkung_Beginn</c>/<c>_Ende</c>?
        /// Genau die Heiz-Nachtzeile an Gebäude und Katalogbau (B6, <see cref="Matrixzellenort.Nachtzeittabelle"/>).
        /// </summary>
        public static bool NachtzeitImBestand(Kalendereigentuemer art, Konditionierungsgroesse groesse, string zeile)
            => groesse == Konditionierungsgroesse.Heizsoll
               && string.Equals(zeile, DbWerte.KOND_ZEILE_NACHT, StringComparison.Ordinal)
               && Matrixzellenort.Nachtzeittabelle(art) != null;

        /// <summary>
        /// <b>Die Merker nach der Regel des Dialogs</b> (B7; <c>GebaeudeArbeitsstand.Ableiten</c>): Der
        /// Heizwert Wochenende setzt <c>Wochenende</c> auf 1, wenn er wirkt
        /// (<see cref="Gebaeudemodellvorgaben.WochenendsollwertWirksam"/>), sonst 0; der Heizwert Ferien
        /// ebenso <c>Ferien</c> (<see cref="Gebaeudemodellvorgaben.FeriensollwertWirksam"/>). Nur an
        /// Gebäude und Katalogbau — die Zone erbt die Merker vom Gebäude.
        /// </summary>
        public static void Merker(Matrixeingang b, Kalendereigentuemer art, Konditionierungsgroesse groesse, string zeile)
        {
            if (b == null) throw new ArgumentNullException(nameof(b));
            if (groesse != Konditionierungsgroesse.Heizsoll || Matrixzellenort.Nachtzeittabelle(art) == null) return;
            if (string.Equals(zeile, DbWerte.KOND_ZEILE_WOCHENENDE, StringComparison.Ordinal) && b.SollWochenende.HasValue)
                b.Wochenendmerker = Gebaeudemodellvorgaben.WochenendsollwertWirksam(b.SollWochenende.Value) ? 1.0 : 0.0;
            else if (string.Equals(zeile, DbWerte.KOND_ZEILE_FERIEN, StringComparison.Ordinal) && b.SollFerien.HasValue)
                b.Ferienmerker = Gebaeudemodellvorgaben.FeriensollwertWirksam(b.SollFerien.Value) ? 1.0 : 0.0;
        }

        /// <summary>
        /// <b>„Kalender anlegen"</b> an einer Ebene (Konzept 3.3): Der Generator macht aus der
        /// <paramref name="matrix"/> den Kalender der Größe — mit dem Rundlauf (Regel 1); ein
        /// vorhandener derselben Größe wird ersetzt, Herkunft und Vermerk fallen.
        /// </summary>
        public static Ebenenergebnis KalenderAnlegen(Konditionierungsstand ebene, Vorgabematrix matrix,
                                                    Konditionierungsgroesse groesse)
        {
            if (ebene == null) throw new ArgumentNullException(nameof(ebene));
            if (matrix == null) throw new ArgumentNullException(nameof(matrix));
            Fahrplanlesung l = Standardfahrplan.Erzeugen(matrix, groesse, rundlaufPruefen: true);
            if (l.Befund != Fahrplanbefund.Erzeugt) return Ebenenergebnis.Fehler(Abgelehnt(l));
            return Ebenenergebnis.Gut(ebene.MitKalender(groesse, l.Kalender, Kalenderherkunft.Keine));
        }

        /// <summary>
        /// <b>„Verwerfen"</b> (Konzept 3.3): Der angelegte Kalender der Größe fällt samt Perioden; die
        /// Vorgabezellen bleiben — sie sind die Matrix, nicht der Kalender.
        /// </summary>
        public static Konditionierungsstand KalenderVerwerfen(Konditionierungsstand ebene, Konditionierungsgroesse groesse)
        {
            if (ebene == null) throw new ArgumentNullException(nameof(ebene));
            return ebene.Kalender(groesse) == null ? ebene : ebene.MitKalender(groesse, null, null);
        }

        /// <summary>
        /// <b>„Matrix erneut anwenden"</b> (P12 (a)): Am angelegten Kalender wird nur der
        /// <b>Matrixbereich</b> ersetzt — Grundangabe samt Standardwoche und die Perioden der Arten
        /// <c>FERIEN</c> und <c>BETRIEBSPAUSE</c> (N1.61 Nr. 13); eigene Perioden und Ausnahmetage
        /// bleiben samt Rang. Ohne angelegten Kalender ist es „Anlegen".
        /// </summary>
        public static Ebenenergebnis MatrixbereichErsetzen(Konditionierungsstand ebene, Vorgabematrix matrix,
                                                          Konditionierungsgroesse groesse)
        {
            if (ebene == null) throw new ArgumentNullException(nameof(ebene));
            if (matrix == null) throw new ArgumentNullException(nameof(matrix));
            Konditionierungskalender alt = ebene.Kalender(groesse);
            if (alt == null) return KalenderAnlegen(ebene, matrix, groesse);

            Fahrplanlesung l = Standardfahrplan.Erzeugen(matrix, groesse, rundlaufPruefen: true);
            if (l.Befund != Fahrplanbefund.Erzeugt) return Ebenenergebnis.Fehler(Abgelehnt(l));
            Konditionierungskalender neu = MatrixbereichMischen(alt, l.Kalender, out string fehler);
            if (fehler != null) return Ebenenergebnis.Fehler(fehler);
            // B8: Die Herkunft „aus Vorlage …" bleibt; der Vermerk des letzten Werkzeugs beschreibt die
            // ersetzte Standardwoche nicht mehr und fällt.
            return Ebenenergebnis.Gut(ebene.MitKalender(groesse, neu, new Kalenderherkunft(ebene.Herkunft(groesse).Vorlage, null)));
        }

        /// <summary>
        /// Der Matrixbereich aus <paramref name="generator"/>, die eigenen Perioden aus
        /// <paramref name="alt"/> samt Rang; eine Rangkollision wird benannt abgelehnt statt still
        /// verschoben (N1.61 Nr. 13).
        /// </summary>
        public static Konditionierungskalender MatrixbereichMischen(Konditionierungskalender alt,
                                                                    Konditionierungskalender generator,
                                                                    out string fehler)
        {
            if (alt == null) throw new ArgumentNullException(nameof(alt));
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            fehler = null;
            var perioden = new List<Kalenderregel>();
            foreach (Kalenderregel r in alt.Perioden)
                if (!IstMatrixbereich(r)) perioden.Add(r);
            foreach (Kalenderregel r in generator.Perioden)
                perioden.Add(r);

            var raenge = new HashSet<int>();
            foreach (Kalenderregel r in perioden)
                if (!raenge.Add(r.Rang))
                {
                    fehler = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KOND_FAHRPLAN_ABGELEHNT,
                                           Fahrplanbefund.RundlaufVerletzt.ToString(),
                                           "Rang " + r.Rang.ToString(CultureInfo.InvariantCulture) + " doppelt");
                    return null;
                }
            try
            {
                return new Konditionierungskalender(alt.Groesse, generator.Grundangabe, generator.Nennwert, perioden);
            }
            catch (ArgumentException ex)
            {
                fehler = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Gehört die Periode zum <b>Matrixbereich</b> (P12)? Genau die Arten, die der Generator
        /// vergibt: <c>FERIEN</c> (die Ferienzeiträume) und <c>BETRIEBSPAUSE</c> (die Saison).
        /// <c>ZEITRAUM</c> und <c>FEIERTAG</c> gehören dem Anwender und bleiben.
        /// </summary>
        public static bool IstMatrixbereich(Kalenderregel r)
            => r != null
               && (string.Equals(r.Art, DbWerte.KOND_ART_FERIEN, StringComparison.Ordinal)
                   || string.Equals(r.Art, DbWerte.KOND_ART_BETRIEBSPAUSE, StringComparison.Ordinal));

        /// <summary>
        /// <b>„Vorlage übernehmen"</b> an einer Ebene (Konzept 3.5, P11, P12): Die Zellen der Vorlage
        /// gehen über die Weiche in die Spalte ihrer Größe — eine leere Zelle der Vorlage lässt die
        /// des Ziels stehen, also bleiben Nennwert und Saison des Ziels (E54) —, und der Kalender wird
        /// angelegt: der Generator über der ergänzten Matrix mit den <b>Ferienzeiträumen des Ziels</b>,
        /// darüber Standardwoche und eigene Perioden der Vorlage (<see cref="Zusammenfuehren"/>). Die
        /// Herkunft ist der Name der Vorlage.
        /// </summary>
        /// <param name="ebene">Die Ebene des Ziels.</param>
        /// <param name="zielmatrix">Die WIRKSAME Matrix des Ziels (nach der Kaskade, F2).</param>
        /// <param name="vorlage">Die Vorlage samt Inhalt.</param>
        public static Ebenenergebnis VorlageEintragen(Konditionierungsstand ebene, Vorgabematrix zielmatrix,
                                                     Konditionierungsvorlage vorlage)
        {
            if (ebene == null) throw new ArgumentNullException(nameof(ebene));
            if (zielmatrix == null) throw new ArgumentNullException(nameof(zielmatrix));
            if (vorlage == null) throw new ArgumentNullException(nameof(vorlage));
            if (ebene.Art == Kalendereigentuemer.Vorlage)
                return Ebenenergebnis.Fehler(MyResource.Resource.KOND_MSG_VORLAGE_ALS_ZIEL);

            Konditionierungsgroesse groesse = vorlage.Groesse;
            Konditionierungsstand inhalt = vorlage.Inhalt ?? Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null);

            // Die Spalte der Vorlage ueber der des Ziels; eine Vorlage fuehrt keine Bestandsspalten.
            Vorgabematrix vorlagenmatrix = Vorgabematrix.Bilden(new Matrixeingang(), inhalt.Vorgabezeilen(),
                                                                Kalendereigentuemer.Vorlage);
            Matrixspalte vorlagenspalte = vorlagenmatrix.Spalte(groesse);
            Matrixspalte neueSpalte = vorlagenspalte.Erben(zielmatrix.Spalte(groesse));

            Fahrplanlesung l = Standardfahrplan.Erzeugen(zielmatrix.MitSpalte(groesse, neueSpalte), groesse,
                                                         rundlaufPruefen: true);
            if (l.Befund != Fahrplanbefund.Erzeugt) return Ebenenergebnis.Fehler(Abgelehnt(l));

            Konditionierungskalender neu = Zusammenfuehren(l.Kalender, inhalt.Kalender(groesse), ebene.Kalender(groesse),
                                                           out string fehler);
            if (fehler != null) return Ebenenergebnis.Fehler(fehler);

            Konditionierungsstand e = ebene;
            foreach (string zeile in DbWerte.KOND_ZEILEN)
            {
                Matrixzelle zelle = vorlagenspalte.Zeile(zeile);
                if (zelle == null || !Konditionierungsstand.Traegt(zelle)) continue;
                string pruefung = Zellenpruefung(groesse, zeile, zelle);
                if (pruefung != null) return Ebenenergebnis.Fehler(pruefung);
                Ebenenergebnis r = Eintragen(e, groesse, zeile, zelle);
                if (!r.Ok) return r;
                e = r.Stand;
            }
            return Ebenenergebnis.Gut(e.MitKalender(groesse, neu, new Kalenderherkunft(vorlage.Name, null)));
        }

        /// <summary>
        /// <b>Generator, Vorlage und Bestand zu einem Kalender</b> (P12, N1.61 Nr. 13): die
        /// Grundangabe vom Generator, es sei denn, die Vorlage bringt eine Standardwoche mit; der
        /// Matrixbereich vom Generator (Ferien und Saison DES ZIELS); die eigenen Perioden eines
        /// vorhandenen Kalenders samt Rang; die Perioden der Vorlage im Eigenband dazu, ihre
        /// Feiertagsregeln aber im Feiertagsband 100 + k unter den Ferien — eine Feiertagsregel, die
        /// schon steht, nur einmal; ein belegter Rang wird benannt abgelehnt.
        /// </summary>
        public static Konditionierungskalender Zusammenfuehren(Konditionierungskalender generator,
                                                               Konditionierungskalender ausVorlage,
                                                               Konditionierungskalender alt,
                                                               out string fehler)
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            fehler = null;
            var perioden = new List<Kalenderregel>();
            var belegt = new HashSet<int>();
            var feiertagsregeln = new HashSet<string>(StringComparer.Ordinal);

            if (alt != null)
                foreach (Kalenderregel r in alt.Perioden)
                {
                    if (IstMatrixbereich(r)) continue;
                    perioden.Add(r);
                    belegt.Add(r.Rang);
                    if (r.IstFeiertag) feiertagsregeln.Add(r.Feiertagsregel);
                }

            foreach (Kalenderregel r in generator.Perioden)
            {
                perioden.Add(r);
                belegt.Add(r.Rang);
            }

            // Die Feiertagsregeln der Vorlage kommen in IHR Band 100 + k — UNTER die Ferien, damit ein
            // Feiertag in den Ferien den Ferienwert behaelt (Standardfahrplan.RANG_FEIERTAG; dieselbe
            // Stelle wie das Werkzeug Kalenderwerkzeuge.Feiertagsregeln). Die uebrigen Perioden der
            // Vorlage gehen ins Eigenband 310+.
            var ausDerVorlage = new List<Kalenderregel>();
            if (ausVorlage != null)
                foreach (Kalenderregel r in ausVorlage.Perioden)
                {
                    if (!r.IstFeiertag)
                    {
                        ausDerVorlage.Add(r);
                        continue;
                    }
                    if (!feiertagsregeln.Add(r.Feiertagsregel)) continue;          // steht schon: nur einmal
                    int k = Feiertagsstelle(r.Feiertagsregel);
                    if (k < 0)
                    {
                        ausDerVorlage.Add(r);
                        continue;
                    }
                    int rang = Standardfahrplan.RANG_FEIERTAG + k;
                    if (!belegt.Add(rang))
                    {
                        fehler = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_RANG_BELEGT,
                                               rang.ToString(CultureInfo.InvariantCulture), r.Feiertagsregel);
                        return null;
                    }
                    perioden.Add(Kalenderwerkzeuge.MitRang(r, rang));
                }

            fehler = Kalenderwerkzeuge.ImEigenband(ausDerVorlage, belegt, out List<Kalenderregel> vergeben);
            if (fehler != null) return null;
            perioden.AddRange(vergeben);

            fehler = Kalenderwerkzeuge.Rangpruefung(perioden);
            if (fehler != null) return null;

            if (perioden.Count > Kalenderregel.PERIODEN_MAX)
            {
                fehler = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_PERIODEN_ZU_VIELE,
                                       perioden.Count.ToString(CultureInfo.InvariantCulture),
                                       Kalenderregel.PERIODEN_MAX.ToString(CultureInfo.InvariantCulture));
                return null;
            }

            Kalenderangabe grund = ausVorlage != null && ausVorlage.Grundangabe.Art == Angabeart.Woche
                ? ausVorlage.Grundangabe
                : generator.Grundangabe;
            try
            {
                return new Konditionierungskalender(generator.Groesse, grund, generator.Nennwert, perioden);
            }
            catch (ArgumentException ex)
            {
                fehler = ex.Message;
                return null;
            }
        }

        /// <summary>Die Stelle einer Feiertagsregel in <see cref="DbWerte.KOND_FEIERTAGE"/>; −1 = keine.</summary>
        private static int Feiertagsstelle(string regel)
        {
            for (int k = 0; k < DbWerte.KOND_FEIERTAGE.Count; k++)
                if (string.Equals(DbWerte.KOND_FEIERTAGE[k], regel, StringComparison.Ordinal)) return k;
            return -1;
        }

        /// <summary>
        /// <b>Ein Werkzeug der Karte</b> auf den angelegten Kalender einer Größe
        /// (<see cref="Kalenderwerkzeuge"/>): Ohne angelegten Kalender gibt es nichts zu ändern — das
        /// wird benannt abgelehnt, statt still einen anzulegen. Der Vermerk des Werkzeugs geht in die
        /// Herkunft; die Vorlage darin bleibt (B8: <c>Bemerkung</c> = Herkunft · letzter Vermerk).
        /// </summary>
        public static Ebenenergebnis Werkzeug(Konditionierungsstand ebene, Konditionierungsgroesse groesse,
                                             Func<Konditionierungskalender, Kalenderwerkzeuge.Werkzeugbefund> werkzeug)
        {
            if (ebene == null) throw new ArgumentNullException(nameof(ebene));
            if (werkzeug == null) throw new ArgumentNullException(nameof(werkzeug));
            Konditionierungskalender k = ebene.Kalender(groesse);
            if (k == null)
                return Ebenenergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_KEIN_KALENDER,
                                                           Konditionierungsgroessen.Kennwort(groesse)));
            Kalenderwerkzeuge.Werkzeugbefund b = werkzeug(k);
            if (!b.Ok) return Ebenenergebnis.Fehler(b.Meldung);
            return Ebenenergebnis.Gut(ebene.MitKalender(groesse, b.Kalender, ebene.Herkunft(groesse).MitVermerk(b.Vermerk)));
        }

        /// <summary>
        /// <b>Der Inhalt einer Vorlage aus einer Ebene</b> — „Als Vorlage speichern" (E54, Konzept 3.5,
        /// 5.7): die Nutzungszeilen der Spalte (Tag, Nacht mit Zeiten und ΔT, Wochenende, Ferienwert),
        /// dazu, falls angelegt, der Kalender der Größe ohne Ferien- und Saisonperioden und ohne
        /// Nennwert. <b>Weder Nennwert noch Saison.</b> Bestandszellen werden Vorgabezellen (eine
        /// Vorlage führt keine Bestandsspalten), „aus" schlägt den Zahlenwert; die Nachtzeiten kommen
        /// aus den Bestandsfeldern, wo die Nachtzeile keine eigenen trägt.
        /// </summary>
        public static Ebenenergebnis AlsVorlageInhalt(Konditionierungsstand quelle, Konditionierungsgroesse groesse)
        {
            if (quelle == null) throw new ArgumentNullException(nameof(quelle));
            if (quelle.Art == Kalendereigentuemer.Vorlage)
                return Ebenenergebnis.Fehler(MyResource.Resource.KOND_MSG_VORLAGE_ALS_QUELLE);

            Konditionierungsstand v = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null);
            foreach (string zeile in NUTZUNGSZEILEN)
            {
                Matrixzelle c = quelle.Vorgabe(groesse, zeile);
                if (Konditionierungsstand.Traegt(c)) v = v.MitVorgabe(groesse, zeile, c);
            }

            Konditionierungskalender k = quelle.Kalender(groesse);
            if (k != null)
            {
                var perioden = new List<Kalenderregel>();
                foreach (Kalenderregel r in k.Perioden)
                    if (!IstMatrixbereich(r) && r.Rang != Standardfahrplan.RANG_SAISON) perioden.Add(r);
                v = v.MitKalender(groesse, new Konditionierungskalender(groesse, k.Grundangabe, null, perioden),
                                  quelle.Herkunft(groesse));
            }

            Matrixeingang bestand = quelle.Bestand;
            foreach (string zeile in NUTZUNGSZEILEN)
            {
                if (!Matrixzellenort.HatBestandsspalte(quelle.Art, groesse, zeile)) continue;
                double? wert = Bestandswert(bestand, groesse, zeile);
                if (!wert.HasValue) continue;
                if (!Konditionierungsgroessen.ImBereich(groesse, wert.Value))
                    return Ebenenergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KOND_MSG_WERT_AUSSERHALB,
                        Zahltext(wert.Value), Konditionierungsgroessen.Bereichstext(groesse)));
                Matrixzelle c = v.Vorgabe(groesse, zeile);
                if (c.Aus) continue;                        // „aus" schlägt den Zahlenwert (Konzept 5.6)
                v = v.MitVorgabe(groesse, zeile, Matrixzelle.AusWert(wert.Value, c.Von, c.Bis, c.BedingtK));
            }

            if (Matrixzellenort.Nachtzeittabelle(quelle.Art) != null
                && bestand.NachtBeginn.HasValue && bestand.NachtEnde.HasValue)
            {
                if (Nachtzeit.Pruefen(bestand.NachtBeginn, bestand.NachtEnde) != NachtzeitBefund.Gueltig)
                    return Ebenenergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KOND_MSG_ZEITFENSTER_UNGUELTIG,
                        bestand.NachtBeginn.Value.ToString(CultureInfo.InvariantCulture),
                        bestand.NachtEnde.Value.ToString(CultureInfo.InvariantCulture)));
                Matrixzelle n = v.Vorgabe(groesse, DbWerte.KOND_ZEILE_NACHT);
                if (Konditionierungsstand.Traegt(n) && !n.Von.HasValue && !n.Bis.HasValue)
                    v = v.MitVorgabe(groesse, DbWerte.KOND_ZEILE_NACHT, Mit(n, bestand.NachtBeginn, bestand.NachtEnde));
            }
            return Ebenenergebnis.Gut(v);
        }

        /// <summary>Die vier Nutzungszeilen einer Vorlage (E54) — ohne <c>NENNWERT</c> und <c>SAISON</c>.</summary>
        private static readonly string[] NUTZUNGSZEILEN =
        {
            DbWerte.KOND_ZEILE_TAG, DbWerte.KOND_ZEILE_NACHT, DbWerte.KOND_ZEILE_WOCHENENDE, DbWerte.KOND_ZEILE_FERIEN,
        };

        // =================================================================
        //  Die Schritte des Arbeitsstands (der Editor)
        // =================================================================

        /// <summary>
        /// <b>Eine Zelle setzen</b> (Entwurf KP2 Abschnitt 2): Die Zelle trägt den gewollten Zustand
        /// samt Wert, auch den einer Bestandszelle. Geprüft wird wie am Schreibweg
        /// (<see cref="Zellenpruefung"/>), eingetragen über die Weiche (<see cref="Eintragen"/>).
        /// Danach die Regeln des Editors:
        /// <list type="bullet">
        /// <item>eine unbeheizte Zone trägt weder Heiz- noch Kühlzellen, die Kühlspalte einer Zone folgt
        /// dem Gebäude (Konzept 3.4, E49 A4 (a)) — beides benannt abgelehnt;</item>
        /// <item>eine Lüftungszelle über der Gesamtangabe <c>Luftwechselrate</c> verlangt erst die
        /// Rückfrage „aufteilen" (E56 F5 (a), <see cref="LuftwechselAufteilen"/>);</item>
        /// <item>P1 beim Übergang der Personenspalte (<see cref="Personenvorschlag"/>,
        /// <see cref="Personenlast"/>);</item>
        /// <item>angelegte, nicht von Hand geänderte Kalender folgen der Matrix (E56 F2 (a),
        /// <see cref="Folgen"/>).</item>
        /// </list>
        /// </summary>
        public static Konditionierungsschritt ZelleSetzen(Konditionierungsarbeitsstand stand, Konditionierungsort ort,
                                                          string zeile, Matrixzelle zelle)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (ort == null) throw new ArgumentNullException(nameof(ort));
            if (zelle == null) throw new ArgumentNullException(nameof(zelle));
            Konditionierungsstand ebene = stand.Ebene(ort.Zone);
            if (ebene == null) return ZoneFehlt(ort);
            string regel = Zonenregel(stand, ort);
            if (regel != null) return Konditionierungsschritt.Fehler(regel);

            string pruefung = Zellenpruefung(ort.Groesse, zeile, zelle);
            if (pruefung != null) return Konditionierungsschritt.Fehler(pruefung);
            if (ort.Groesse == Konditionierungsgroesse.Lueftung && Konditionierungsstand.Traegt(zelle)
                && Gesamtangabe(stand, ort))
                return Konditionierungsschritt.Frage(Bilanz(Ersetzt(Konditionierungspostenart.Luftwechsel, 1)));

            Ebenenergebnis r = Eintragen(ebene, ort.Groesse, zeile, zelle);
            if (!r.Ok) return Konditionierungsschritt.Fehler(r.Meldung);
            Konditionierungsarbeitsstand neu = stand.MitEbene(ort.Zone, r.Stand);
            if (ort.Groesse == Konditionierungsgroesse.Personen
                && !string.Equals(zeile, DbWerte.KOND_ZEILE_NENNWERT, StringComparison.Ordinal))
                neu = Personenvorschlag(neu, ort, ohneAnteil: false);
            neu = Personenlast(stand, neu, ort);
            string fehler = Folgen(stand, ref neu);
            if (fehler != null) return Konditionierungsschritt.Fehler(fehler);
            return Konditionierungsschritt.Gut(neu, Bilanz(Ersetzt(Konditionierungspostenart.Matrixzellen, 1)));
        }

        /// <summary>
        /// <b>„Kalender anlegen"</b> am Ort (Konzept 3.3): der Generator über der wirksamen Matrix des
        /// Orts, mit dem Rundlauf; ein vorhandener Kalender derselben Größe wird ersetzt. Am Gebäude
        /// legt derselbe Schritt die Kalender der Zonen mit eigenen Werten mit an (F2 Regel 2, B4 —
        /// „Anlegen ändert keine Reihe", <see cref="Zonenkalender"/>); ein Personenkalender bringt P1 mit.
        /// </summary>
        public static Konditionierungsschritt Anlegen(Konditionierungsarbeitsstand stand, Konditionierungsort ort)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (ort == null) throw new ArgumentNullException(nameof(ort));
            if (stand.Ebene(ort.Zone) == null) return ZoneFehlt(ort);
            string regel = Zonenregel(stand, ort);
            if (regel != null) return Konditionierungsschritt.Fehler(regel);

            Konditionierungsarbeitsstand a = ort.Groesse == Konditionierungsgroesse.Personen
                ? Personenvorschlag(stand, ort, ohneAnteil: true)
                : stand;
            Ebenenergebnis r = KalenderAnlegen(a.Ebene(ort.Zone), a.Matrix(ort.Zone), ort.Groesse);
            if (!r.Ok) return Konditionierungsschritt.Fehler(r.Meldung);
            Konditionierungsarbeitsstand neu = a.MitEbene(ort.Zone, r.Stand);

            var zonen = new List<string>();
            if (!ort.Zone.HasValue)
            {
                string f = Zonenkalender(ref neu, ort.Groesse, zonen);
                if (f != null) return Konditionierungsschritt.Fehler(f);
            }
            neu = Personenlast(stand, neu, ort);
            string fehler = Folgen(stand, ref neu);
            if (fehler != null) return Konditionierungsschritt.Fehler(fehler);
            return Konditionierungsschritt.Gut(neu, new Konditionierungsbilanz(
                new[] { Ersetzt(Konditionierungspostenart.Kalender, 1), Ersetzt(Konditionierungspostenart.Zonenkalender, zonen.Count) },
                null, zonen));
        }

        /// <summary><b>„Verwerfen"</b> am Ort: Der angelegte Kalender fällt samt Perioden, die Matrix bleibt; P1 beim Übergang.</summary>
        public static Konditionierungsschritt Verwerfen(Konditionierungsarbeitsstand stand, Konditionierungsort ort)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (ort == null) throw new ArgumentNullException(nameof(ort));
            Konditionierungsstand ebene = stand.Ebene(ort.Zone);
            if (ebene == null) return ZoneFehlt(ort);
            Konditionierungskalender k = ebene.Kalender(ort.Groesse);
            if (k == null) return Konditionierungsschritt.Gut(stand);
            Konditionierungsarbeitsstand neu = stand.MitEbene(ort.Zone, KalenderVerwerfen(ebene, ort.Groesse));
            neu = Personenlast(stand, neu, ort);
            string fehler = Folgen(stand, ref neu);
            if (fehler != null) return Konditionierungsschritt.Fehler(fehler);
            return Konditionierungsschritt.Gut(neu, Bilanz(Kalenderposten(k, mitKalender: true)));
        }

        /// <summary><b>„Matrix erneut anwenden"</b> am Ort (P12 (a)) — nur der Matrixbereich; ohne angelegten Kalender „Anlegen".</summary>
        public static Konditionierungsschritt MatrixErneut(Konditionierungsarbeitsstand stand, Konditionierungsort ort)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (ort == null) throw new ArgumentNullException(nameof(ort));
            Konditionierungsstand ebene = stand.Ebene(ort.Zone);
            if (ebene == null) return ZoneFehlt(ort);
            Konditionierungskalender alt = ebene.Kalender(ort.Groesse);
            if (alt == null) return Anlegen(stand, ort);
            Ebenenergebnis r = MatrixbereichErsetzen(ebene, stand.Matrix(ort.Zone), ort.Groesse);
            if (!r.Ok) return Konditionierungsschritt.Fehler(r.Meldung);
            return Konditionierungsschritt.Gut(stand.MitEbene(ort.Zone, r.Stand), MatrixbereichBilanz(alt));
        }

        /// <summary>
        /// <b>„Vorlage übernehmen"</b> am Ort (P11, P12): nur in der Größe der Vorlage; die Zellen der
        /// Vorlage in die Spalte, der Kalender angelegt (<see cref="VorlageEintragen"/>). Dieselben
        /// Regeln wie „Zelle setzen" und „Anlegen": Zonenregel, Rückfrage „aufteilen" (F5),
        /// Zonenkalender am Gebäude (B4), P1, Folgen (F2).
        /// </summary>
        public static Konditionierungsschritt VorlageUebernehmen(Konditionierungsarbeitsstand stand, Konditionierungsort ort,
                                                                 Konditionierungsvorlage vorlage)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (ort == null) throw new ArgumentNullException(nameof(ort));
            if (vorlage == null) throw new ArgumentNullException(nameof(vorlage));
            Konditionierungsstand ebene = stand.Ebene(ort.Zone);
            if (ebene == null) return ZoneFehlt(ort);
            if (vorlage.Groesse != ort.Groesse)
                return Konditionierungsschritt.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_VORLAGE_GROESSE,
                    Konditionierungsgroessen.Kennwort(vorlage.Groesse), Konditionierungsgroessen.Kennwort(ort.Groesse)));
            string regel = Zonenregel(stand, ort);
            if (regel != null) return Konditionierungsschritt.Fehler(regel);

            Konditionierungsstand inhalt = vorlage.Inhalt ?? Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null);
            if (ort.Groesse == Konditionierungsgroesse.Lueftung && inhalt.VorgabenAnzahl > 0 && Gesamtangabe(stand, ort))
                return Konditionierungsschritt.Frage(Bilanz(Ersetzt(Konditionierungspostenart.Luftwechsel, 1)));

            Konditionierungskalender alt = ebene.Kalender(ort.Groesse);
            Ebenenergebnis r = VorlageEintragen(ebene, stand.Matrix(ort.Zone), vorlage);
            if (!r.Ok) return Konditionierungsschritt.Fehler(r.Meldung);
            Konditionierungsarbeitsstand neu = stand.MitEbene(ort.Zone, r.Stand);

            var zonen = new List<string>();
            if (!ort.Zone.HasValue)
            {
                string f = Zonenkalender(ref neu, ort.Groesse, zonen);
                if (f != null) return Konditionierungsschritt.Fehler(f);
            }
            if (ort.Groesse == Konditionierungsgroesse.Personen)
                neu = Personenvorschlag(neu, ort, ohneAnteil: true);
            neu = Personenlast(stand, neu, ort);
            string fehler = Folgen(stand, ref neu);
            if (fehler != null) return Konditionierungsschritt.Fehler(fehler);

            int zellen = inhalt.VorgabenAnzahl;
            Konditionierungsbilanz b = alt == null
                ? new Konditionierungsbilanz(new[]
                  {
                      Ersetzt(Konditionierungspostenart.Matrixzellen, zellen), Ersetzt(Konditionierungspostenart.Kalender, 1),
                      Ersetzt(Konditionierungspostenart.Zonenkalender, zonen.Count),
                  }, null, zonen)
                : MatrixbereichBilanz(alt, zellen);
            return Konditionierungsschritt.Gut(neu, b);
        }

        /// <summary>
        /// <b>„Aufteilen"</b> — die Antwort auf die Rückfrage nach F5 (E56 F5 (a), B3): Die Gesamtangabe
        /// <c>Luftwechselrate</c> des Gebäudes wird Infiltration = min(0,3 1/h; Rate) und Nutzerlüftung =
        /// der Rest; <b>der wirksame Luftwechsel bleibt</b> (<see cref="Gebaeudemodellvorgaben.WirksamerLuftwechsel(double?, double?, double?)"/>).
        /// Der Rest wird dezimal gebildet, damit er rund läuft (0,7 − 0,3 = 0,4, nicht 0,39999…). Die
        /// Zonen erben die Aufteilung; trägt das Gebäude keine Gesamtangabe, ändert sich nichts.
        /// </summary>
        public static Konditionierungsschritt LuftwechselAufteilen(Konditionierungsarbeitsstand stand)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            Matrixeingang b = stand.Gebaeude.Bestand;
            if (!Gesamtangabe(b)) return Konditionierungsschritt.Gut(stand);

            double rate = b.Luftwechselrate.Value;
            double infiltration = Math.Min(Gebaeudemodellvorgaben.LuftwechselInfiltration, rate);
            double nutzer = (double)((decimal)rate - (decimal)infiltration);
            Konditionierungsarbeitsstand neu = stand.MitGebaeude(stand.Gebaeude.MitBestand(x =>
            {
                x.LuftwechselInfiltration = infiltration;
                x.LuftwechselNutzer = nutzer;
                HerkunftDesLuftwechsels(x);
            }));
            string fehler = Folgen(stand, ref neu);
            if (fehler != null) return Konditionierungsschritt.Fehler(fehler);
            return Konditionierungsschritt.Gut(neu, Bilanz(Ersetzt(Konditionierungspostenart.Luftwechsel, 1)));
        }

        // ---- Die Regeln der Schritte ----

        /// <summary>
        /// <b>Was eine Zone nicht trägt</b> (Konzept 3.4): Eine unbeheizte Zone hat weder Heiz- noch
        /// Kühlkalender (N1.56 Festlegung 2), und die Kühlspalte einer Zone folgt dem Gebäude, bis KU3
        /// Kühlkalender je Zone bringt (E49 A4 (a)). <c>null</c> = der Schritt darf.
        /// </summary>
        private static string Zonenregel(Konditionierungsarbeitsstand stand, Konditionierungsort ort)
        {
            if (!ort.Zone.HasValue) return null;
            Konditionierungszone z = stand.Zone(ort.Zone.Value);
            if (z == null) return null;
            bool klima = ort.Groesse == Konditionierungsgroesse.Heizsoll || ort.Groesse == Konditionierungsgroesse.Kuehlsoll;
            if (klima && !z.IstBeheizt)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_ZONE_UNBEHEIZT, z.Name);
            if (ort.Groesse == Konditionierungsgroesse.Kuehlsoll)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_ZONE_KUEHLEN, z.Name);
            return null;
        }

        /// <summary>Trägt der Ort nur die Gesamtangabe <c>Luftwechselrate</c> (F15)? An einer Zone die aufgelösten Felder.</summary>
        private static bool Gesamtangabe(Konditionierungsarbeitsstand stand, Konditionierungsort ort)
        {
            Konditionierungszone z = ort.Zone.HasValue ? stand.Zone(ort.Zone.Value) : null;
            return Gesamtangabe(z == null ? stand.Gebaeude.Bestand : stand.AufgeloesterBestand(z));
        }

        /// <summary>Stammt der wirksame Luftwechsel aus der Gesamtangabe?</summary>
        public static bool Gesamtangabe(Matrixeingang b)
        {
            if (b == null) throw new ArgumentNullException(nameof(b));
            Gebaeudemodellvorgaben.WirksamerLuftwechsel(b.Luftwechselrate, b.LuftwechselInfiltration, b.LuftwechselNutzer,
                                                        out Luftwechselherkunft herkunft);
            return herkunft == WindowsFormsApplication1.Luftwechselherkunft.Luftwechselrate;
        }

        /// <summary>
        /// <b>Der Personen-Nennwert als Vorschlag</b> (P1, Konzept 3.1): Trägt die Personenspalte am Ort
        /// einen Anteil (oder wird ein Personenkalender angelegt, <paramref name="ohneAnteil"/>) und keinen
        /// Nennwert, bekommt die Ebene Personenzahl × 70 W, auf 0,1 W gerundet (Rundlauf). Die
        /// Personenzahl kommt aus <c>Bewohner</c> (an der Zone der Flächenanteil); ohne sie kein Vorschlag.
        /// </summary>
        private static Konditionierungsarbeitsstand Personenvorschlag(Konditionierungsarbeitsstand stand,
                                                                      Konditionierungsort ort, bool ohneAnteil)
        {
            Vorgabematrix m = stand.Matrix(ort.Zone);
            Matrixspalte s = m.Personen;
            if (s.Nennwert.Belegt) return stand;
            if (!ohneAnteil && !(s.Tag.Belegt || s.Nacht.Belegt || s.Wochenende.Belegt || s.Ferien.Belegt)) return stand;
            Konditionierungszone z = ort.Zone.HasValue ? stand.Zone(ort.Zone.Value) : null;
            Matrixeingang b = z == null ? stand.Gebaeude.Bestand : stand.AufgeloesterBestand(z);
            double vorschlag = Math.Round(PersonenNennwertVorschlag(b.Bewohner, 0.0, null), 1, MidpointRounding.AwayFromZero);
            if (!(vorschlag > 0.0)) return stand;
            Konditionierungsstand e = stand.Ebene(ort.Zone).MitVorgabe(Konditionierungsgroesse.Personen,
                                                                     DbWerte.KOND_ZEILE_NENNWERT, Matrixzelle.AusWert(vorschlag));
            return stand.MitEbene(ort.Zone, e);
        }

        /// <summary>
        /// <b>Energie bleibt (P1)</b> beim Übergang der Personenspalte am Ort (Entwurf KP2,
        /// Festlegung 5): Wird sie wirksam (es gilt ein Personenkalender, angelegt oder abgeleitet),
        /// sinkt <c>Interne_Waermegewinne</c> der Ebene um das Jahresmittel der Personenwärme, nie unter
        /// 0; fällt sie weg, kommt das Jahresmittel des bisherigen Kalenders dazu. Innerhalb eines
        /// Zustands ändert sich nichts. An der Zone wird der aufgelöste Wert ihr eigener. Auf 4
        /// Nachkommastellen gerundet (Rundlauf).
        /// </summary>
        private static Konditionierungsarbeitsstand Personenlast(Konditionierungsarbeitsstand vor,
                                                                 Konditionierungsarbeitsstand nach, Konditionierungsort ort)
        {
            Konditionierungskalender kVor = vor.GeltenderKalender(Konditionierungsgroesse.Personen, ort.Zone);
            Konditionierungskalender kNach = nach.GeltenderKalender(Konditionierungsgroesse.Personen, ort.Zone);
            if ((kVor == null) == (kNach == null)) return nach;

            Konditionierungszone z = ort.Zone.HasValue ? vor.Zone(ort.Zone.Value) : null;
            double? iwg = (z == null ? vor.Gebaeude.Bestand : vor.AufgeloesterBestand(z)).InterneWaermegewinne;
            if (!iwg.HasValue) return nach;
            double neu = kNach != null
                ? GeraeteNennwertNachPersonen(iwg.Value, kNach, nach.W0, nach.Referenzjahr)
                : iwg.Value + PersonenJahresmittelW(kVor, vor.W0, vor.Referenzjahr);
            neu = Math.Round(neu, 4, MidpointRounding.AwayFromZero);
            return nach.MitEbene(ort.Zone, nach.Ebene(ort.Zone).MitBestand(b => b.InterneWaermegewinne = neu));
        }

        /// <summary>
        /// <b>F2 Regel 2 (B4)</b> — nach Anlegen oder Vorlage am Gebäude: Jede Zone ohne eigenen
        /// Kalender der Größe, deren Matrix einen anderen Kalender ergäbe als die des Gebäudes (eigene
        /// Zellen, eigene Bestandswerte, Flächenanteil), bekommt ihren abgeleiteten Kalender angelegt —
        /// sonst erbte sie den des Gebäudes und verlöre ihre Werte. Unbeheizte Zonen tragen weder Heiz-
        /// noch Kühlkalender. Scheitert der Generator an einer Zone, wird benannt abgelehnt.
        /// </summary>
        /// <returns><c>null</c> oder die benannte Ablehnung.</returns>
        private static string Zonenkalender(ref Konditionierungsarbeitsstand stand, Konditionierungsgroesse groesse,
                                            ICollection<string> zonen)
        {
            Fahrplanlesung lg = Standardfahrplan.Erzeugen(stand.Matrix(null), groesse, rundlaufPruefen: false);
            foreach (Konditionierungszone z in stand.Zonen)
            {
                if (z.Stand.Kalender(groesse) != null) continue;
                bool klima = groesse == Konditionierungsgroesse.Heizsoll || groesse == Konditionierungsgroesse.Kuehlsoll;
                if (klima && !z.IstBeheizt) continue;
                Fahrplanlesung lz = Standardfahrplan.Erzeugen(stand.Matrix(z.Id), groesse, rundlaufPruefen: true);
                if (lz.Befund == Fahrplanbefund.KeineAngabe) continue;
                if (lz.Befund != Fahrplanbefund.Erzeugt)
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_ZONE_KALENDER,
                                         z.Name, Abgelehnt(lz));
                if (lg.Befund == Fahrplanbefund.Erzeugt && Kalendervergleich.KalenderGleich(lz.Kalender, lg.Kalender))
                    continue;
                stand = stand.MitEbene(z.Id, z.Stand.MitKalender(groesse, lz.Kalender, Kalenderherkunft.Keine));
                zonen.Add(z.Name);
            }
            return null;
        }

        /// <summary>
        /// <b>Ein angelegter, nicht von Hand geänderter Kalender folgt der Matrix</b> (E56 F2 (a),
        /// Konzept 3.3): Für jede Ebene und Größe, deren Kalender der Schritt nicht selbst gesetzt hat
        /// und dessen Matrixbereich dem Generator der ALTEN Matrix gleicht, wird der Matrixbereich aus
        /// der NEUEN Matrix ersetzt — eigene Perioden und Herkunft bleiben. Gleicht er nicht (eine
        /// Handänderung), bleibt er; dann gilt P12 mit „Matrix erneut anwenden…". Läuft der Generator
        /// nicht rund oder kollidiert ein Rang, lehnt der ganze Schritt benannt ab.
        /// </summary>
        /// <returns><c>null</c> oder die benannte Ablehnung.</returns>
        private static string Folgen(Konditionierungsarbeitsstand vor, ref Konditionierungsarbeitsstand nach)
        {
            var orte = new List<long?> { null };
            foreach (Konditionierungszone z in nach.Zonen) orte.Add(z.Id);
            foreach (long? ort in orte)
            {
                Konditionierungsstand eVor = vor.Ebene(ort);
                Konditionierungsstand e = nach.Ebene(ort);
                if (eVor == null || e == null) continue;
                bool geaendert = false;
                Vorgabematrix mVor = null, mNach = null;
                foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                {
                    Konditionierungskalender k = e.Kalender(g);
                    if (k == null || !Kalendervergleich.KalenderGleich(eVor.Kalender(g), k)) continue;
                    mVor ??= vor.Matrix(ort);
                    Fahrplanlesung la = Standardfahrplan.Erzeugen(mVor, g, rundlaufPruefen: false);
                    if (la.Befund != Fahrplanbefund.Erzeugt || !Kalendervergleich.MatrixbereichGleich(k, la.Kalender)) continue;
                    mNach ??= nach.Matrix(ort);
                    Fahrplanlesung ln = Standardfahrplan.Erzeugen(mNach, g, rundlaufPruefen: true);
                    string fehler = null;
                    Konditionierungskalender neu = null;
                    if (ln.Befund != Fahrplanbefund.Erzeugt) fehler = Abgelehnt(ln);
                    else if (!Kalendervergleich.MatrixbereichGleich(k, ln.Kalender))
                        neu = MatrixbereichMischen(k, ln.Kalender, out fehler);
                    if (fehler != null)
                        return ort.HasValue
                            ? string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_ZONE_KALENDER,
                                            nach.Zone(ort.Value).Name, fehler)
                            : fehler;
                    if (neu == null) continue;
                    e = e.MitKalender(g, neu, e.Herkunft(g));
                    geaendert = true;
                }
                if (geaendert) nach = nach.MitEbene(ort, e);
            }
            return null;
        }

        /// <summary><b>Das Zeitfenster „Tage, von, bis, Wert"</b> auf den angelegten Kalender des Orts.</summary>
        public static Konditionierungsschritt Zeitfenster(Konditionierungsarbeitsstand stand, Konditionierungsort ort,
                                                          IReadOnlyList<int> tage, int von, int bis, double? wert)
            => WerkzeugSchritt(stand, ort, k => Kalenderwerkzeuge.Zeitfenster(k, tage, von, bis, wert),
                               Konditionierungspostenart.Standardwoche);

        /// <summary><b>Die Feiertage als Regel</b> (F11) auf den angelegten Kalender des Orts.</summary>
        public static Konditionierungsschritt Feiertage(Konditionierungsarbeitsstand stand, Konditionierungsort ort,
                                                        int wieWochentag)
            => WerkzeugSchritt(stand, ort, k => Kalenderwerkzeuge.Feiertagsregeln(k, wieWochentag),
                               Konditionierungspostenart.Feiertage);

        /// <summary>
        /// <b>„Zeitstruktur übernehmen"</b> (Entwurf KP2, Festlegung 11) auf den angelegten Kalender des
        /// Orts: die Quelle ist der Heiz- bzw. Personenkalender, der am Ort gilt; Tag- und Nachtwert
        /// kommen aus der wirksamen Matrix — eine leere Anteilszelle heißt 100 %, eine leere Nacht „wie
        /// Tag" (dieselben Regeln wie der Generator).
        /// </summary>
        public static Konditionierungsschritt Zeitstruktur(Konditionierungsarbeitsstand stand, Konditionierungsort ort,
                                                           Zeitstrukturquelle art)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (ort == null) throw new ArgumentNullException(nameof(ort));
            if (stand.Ebene(ort.Zone) == null) return ZoneFehlt(ort);

            Konditionierungsgroesse quellgroesse = art == Zeitstrukturquelle.WieHeizung
                ? Konditionierungsgroesse.Heizsoll : Konditionierungsgroesse.Personen;
            Konditionierungskalender quelle = stand.Ansichtskalender(quellgroesse, ort.Zone);
            if (quelle == null)
                return Konditionierungsschritt.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_ZEITSTRUKTUR_QUELLE, Konditionierungsgroessen.Kennwort(quellgroesse)));

            Vorgabematrix m = stand.Matrix(ort.Zone);
            Matrixzelle heizTag = m.Heizsoll.Tag;
            double? heizTagwert = heizTag.Belegt && !heizTag.Aus ? heizTag.Wert : (double?)null;
            Matrixspalte s = m.Spalte(ort.Groesse);
            Matrixzelle tag = s.Tag.Belegt || !Konditionierungsgroessen.HatNennwert(ort.Groesse)
                ? s.Tag : Matrixzelle.AusWert(Konditionierungsgroessen.ANTEIL_MAX);
            Matrixzelle nacht = s.Nacht.Belegt ? s.Nacht : tag;
            return WerkzeugSchritt(stand, ort,
                                   k => Kalenderwerkzeuge.ZeitstrukturUebernehmen(k, art, quelle, heizTagwert, tag, nacht),
                                   Konditionierungspostenart.Standardwoche);
        }

        /// <summary><b>Der Inhalt „Als Vorlage speichern"</b> am Ort (E54) — schreibt nichts; siehe <see cref="AlsVorlageInhalt"/>.</summary>
        public static Ebenenergebnis AlsVorlage(Konditionierungsarbeitsstand stand, Konditionierungsort ort)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (ort == null) throw new ArgumentNullException(nameof(ort));
            Konditionierungsstand ebene = stand.Ebene(ort.Zone);
            if (ebene == null)
                return Ebenenergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_ZONE_FEHLT,
                                                           Zahl(ort.Zone)));
            return AlsVorlageInhalt(ebene, ort.Groesse);
        }

        private static Konditionierungsschritt WerkzeugSchritt(Konditionierungsarbeitsstand stand, Konditionierungsort ort,
                                                               Func<Konditionierungskalender, Kalenderwerkzeuge.Werkzeugbefund> werkzeug,
                                                               Konditionierungspostenart posten)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (ort == null) throw new ArgumentNullException(nameof(ort));
            Konditionierungsstand ebene = stand.Ebene(ort.Zone);
            if (ebene == null) return ZoneFehlt(ort);
            Ebenenergebnis r = Werkzeug(ebene, ort.Groesse, werkzeug);
            if (!r.Ok) return Konditionierungsschritt.Fehler(r.Meldung);
            return Konditionierungsschritt.Gut(stand.MitEbene(ort.Zone, r.Stand), Bilanz(Ersetzt(posten, 1)));
        }

        // =================================================================
        //  „Aus dem Katalog erneut übernehmen" und die Rückfragen (Festlegung 3, 4; B9)
        // =================================================================

        /// <summary>
        /// <b>„Aus dem Katalog erneut übernehmen…"</b> (Entwurf KP2, Festlegung 4; Befund B9): Die
        /// <b>ganze Gebäudeebene</b> wird die des Katalogbaus — Vorgabezellen, Kalender samt Perioden und
        /// Herkunft, die neun Bestandszellen, Nachtzeiten, Ferienzeiträume samt Merkern und die
        /// Gesamtangabe des Luftwechsels (<see cref="Katalogebene"/>). <b>Die Zonen bleiben</b>, wie sie
        /// sind. Die Bilanz ist dieselbe, die <see cref="Rueckfrage"/> vorher nennt.
        /// </summary>
        /// <param name="stand">Der Arbeitsstand des Projektgebäudes.</param>
        /// <param name="katalog">Die Ebene des Katalogbaus; <c>null</c> = es gibt keinen — benannt abgelehnt.</param>
        public static Konditionierungsschritt KatalogErneut(Konditionierungsarbeitsstand stand, Konditionierungsstand katalog)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (katalog == null) return Konditionierungsschritt.Fehler(MyResource.Resource.KOND_MSG_KATALOG_FEHLT);
            Konditionierungsstand neu = Katalogebene(stand.Gebaeude, katalog);
            return Konditionierungsschritt.Gut(stand.MitGebaeude(neu), KatalogBilanz(stand, neu));
        }

        /// <summary>
        /// <b>Die Gebäudeebene aus dem Katalogbau</b>: Tabellen und Herkunft des Katalogbaus; aus seinem
        /// Bestand die neun Bestandszellen (<see cref="Matrixzellenort"/>), Nachtbeginn und -ende,
        /// Merker, Ferienzeiträume und <c>Luftwechselrate</c>. Alles Übrige des Gebäudebestands
        /// (Bewohner, Maximaltemperatur, Sollwertprofil, Schalter) bleibt.
        /// </summary>
        public static Konditionierungsstand Katalogebene(Konditionierungsstand gebaeude, Konditionierungsstand katalog)
        {
            if (gebaeude == null) throw new ArgumentNullException(nameof(gebaeude));
            if (katalog == null) throw new ArgumentNullException(nameof(katalog));
            Matrixeingang k = katalog.Bestand;
            Matrixeingang b = gebaeude.Bestand;
            b.SollTag = k.SollTag;
            b.SollNacht = k.SollNacht;
            b.SollWochenende = k.SollWochenende;
            b.SollFerien = k.SollFerien;
            b.KuehlSollwert = k.KuehlSollwert;
            b.KuehlSollwertNacht = k.KuehlSollwertNacht;
            b.LuftwechselInfiltration = k.LuftwechselInfiltration;
            b.LuftwechselNutzer = k.LuftwechselNutzer;
            b.InterneWaermegewinne = k.InterneWaermegewinne;
            b.NachtBeginn = k.NachtBeginn;
            b.NachtEnde = k.NachtEnde;
            b.Ferienmerker = k.Ferienmerker;
            b.Wochenendmerker = k.Wochenendmerker;
            b.Luftwechselrate = k.Luftwechselrate;
            for (int i = 0; i < Matrixeingang.FERIENZEITRAEUME; i++)
            {
                b.Ferienbeginn[i] = k.Ferienbeginn[i];
                b.Ferienende[i] = k.Ferienende[i];
            }
            HerkunftDesLuftwechsels(b);
            return katalog.AlsArt(gebaeude.Art).MitBestand(b);
        }

        /// <summary>
        /// <b>Der Rückfragebefund VOR dem Schreiben</b> (Entwurf KP2, Festlegung 3; Befund B9): was die
        /// Handlung ersetzt, was bleibt und welche Zonen sie betrifft, mit Namen — aus dem Arbeitsstand,
        /// ohne Datenbank. <c>null</c> = keine Rückfrage nötig (nichts, was verloren ginge).
        /// <list type="bullet">
        /// <item><b>Matrix erneut anwenden</b>, <b>Verwerfen</b>: nur mit angelegtem Kalender; die Zonen
        /// sind die, deren geltender Kalender sich mit ändert (sie folgen dem Gebäude).</item>
        /// <item><b>Vorlage übernehmen</b>: mit angelegtem Kalender der Matrixbereich (P12), sonst die
        /// Zellen der Spalte; die Zonen sind die, die ihren Kalender mit angelegt bekommen (B4).</item>
        /// <item><b>Aus dem Katalog erneut übernehmen</b> (<paramref name="katalog"/>): die geänderten
        /// Zellen, die Kalender samt eigenen Perioden, Nachtzeiten und Ferienzeiträume; die Zonen mit
        /// eigenen Werten bleiben und stehen mit Namen da. Immer eine Rückfrage.</item>
        /// </list>
        /// </summary>
        public static Konditionierungsbilanz Rueckfrage(Konditionierungsarbeitsstand stand, Konditionierungsort ort,
                                                        Konditionierungshandlung handlung,
                                                        Konditionierungsstand katalog = null)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (handlung == Konditionierungshandlung.KatalogErneut)
                return katalog == null ? null : KatalogBilanz(stand, Katalogebene(stand.Gebaeude, katalog));
            if (ort == null) throw new ArgumentNullException(nameof(ort));
            Konditionierungsstand ebene = stand.Ebene(ort.Zone);
            if (ebene == null) return null;
            Konditionierungskalender alt = ebene.Kalender(ort.Groesse);

            switch (handlung)
            {
                case Konditionierungshandlung.MatrixErneut:
                {
                    if (alt == null) return null;
                    Konditionierungsschritt s = MatrixErneut(stand, ort);
                    return MitZonen(MatrixbereichBilanz(alt), s.Ok ? BetroffeneZonen(stand, s.Stand, ort) : null);
                }
                case Konditionierungshandlung.Verwerfen:
                {
                    if (alt == null) return null;
                    Konditionierungsschritt s = Verwerfen(stand, ort);
                    return MitZonen(Bilanz(Kalenderposten(alt, mitKalender: true)),
                                    s.Ok ? BetroffeneZonen(stand, s.Stand, ort) : null);
                }
                case Konditionierungshandlung.VorlageUebernehmen:
                {
                    int zellen = 0;
                    Matrixspalte spalte = ebene.Matrix().Spalte(ort.Groesse);
                    foreach (string zeile in DbWerte.KOND_ZEILEN)
                        if (Konditionierungsstand.Traegt(spalte.Zeile(zeile))
                            && (Konditionierungsstand.Traegt(ebene.Vorgabe(ort.Groesse, zeile))
                                || Matrixzellenort.HatBestandsspalte(ebene.Art, ort.Groesse, zeile)))
                            zellen++;
                    if (alt == null && zellen == 0) return null;
                    Konditionierungsbilanz b = alt != null
                        ? MatrixbereichBilanz(alt, zellen)
                        : Bilanz(Ersetzt(Konditionierungspostenart.Matrixzellen, zellen));
                    var zonen = new List<string>();
                    if (!ort.Zone.HasValue)
                    {
                        Konditionierungsarbeitsstand probe = stand;
                        Zonenkalender(ref probe, ort.Groesse, zonen);
                    }
                    return MitZonen(b, zonen);
                }
                default:
                    return null;
            }
        }

        /// <summary>
        /// <b>Die Rückfrage von „Speichern unter" im Projekt</b> (Festlegung 3): Der neue Katalogbau
        /// nimmt nur die Gebäudeebene mit; Zonen, ihre Bauteile und ihre Konditionierung bleiben im
        /// Projekt zurück — die Frage nennt alles zusammen. <see cref="Konditionierungsbilanz.Bleibt"/>
        /// zählt die Kalender und Zellen der Zonen (<see cref="Konditionierungspostenart.Zonenkalender"/>)
        /// und die <paramref name="bauteile"/>; <see cref="Konditionierungsbilanz.Zonen"/> nennt jede Zone.
        /// <c>null</c> = ohne Zonen und Bauteile keine Rückfrage.
        /// </summary>
        public static Konditionierungsbilanz RueckfrageSpeichernUnter(Konditionierungsarbeitsstand stand, int bauteile)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (stand.Zonen.Count == 0 && bauteile <= 0) return null;
            int konditionierung = 0;
            var namen = new List<string>();
            foreach (Konditionierungszone z in stand.Zonen)
            {
                konditionierung += z.Stand.KalenderAnzahl + z.Stand.VorgabenAnzahl;
                namen.Add(z.Name);
            }
            return new Konditionierungsbilanz(null,
                new[]
                {
                    Ersetzt(Konditionierungspostenart.Zonenkalender, konditionierung),
                    Ersetzt(Konditionierungspostenart.Bauteile, bauteile),
                },
                namen);
        }

        /// <summary>Die Bilanz von „erneut übernehmen": ersetzt am Gebäude, was sich ändert; die Zonen mit eigenen Werten bleiben.</summary>
        private static Konditionierungsbilanz KatalogBilanz(Konditionierungsarbeitsstand stand, Konditionierungsstand neu)
        {
            Konditionierungsstand alt = stand.Gebaeude;
            Vorgabematrix ma = alt.Matrix(), mn = neu.Matrix();
            int zellen = 0;
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                foreach (string zeile in DbWerte.KOND_ZEILEN)
                    if (!Kalendervergleich.ZelleGleich(ma.Spalte(g).Zeile(zeile), mn.Spalte(g).Zeile(zeile))) zellen++;

            int eigene = 0, feiertage = 0;
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                Konditionierungskalender k = alt.Kalender(g);
                if (k == null) continue;
                foreach (Konditionierungsposten p in Kalenderposten(k, mitKalender: false))
                    if (p.Art == Konditionierungspostenart.EigenePerioden) eigene += p.Anzahl;
                    else if (p.Art == Konditionierungspostenart.Feiertage) feiertage += p.Anzahl;
            }

            Matrixeingang ba = alt.Bestand, bn = neu.Bestand;
            int nacht = ba.NachtBeginn != bn.NachtBeginn || ba.NachtEnde != bn.NachtEnde ? 1 : 0;
            int ferien = 0;
            for (int i = 0; i < Matrixeingang.FERIENZEITRAEUME; i++)
                if (!Kalendervergleich.Gleich(ba.Ferienbeginn[i], bn.Ferienbeginn[i])
                    || !Kalendervergleich.Gleich(ba.Ferienende[i], bn.Ferienende[i]))
                    ferien++;

            int zonenkalender = 0;
            var namen = new List<string>();
            foreach (Konditionierungszone z in stand.Zonen)
            {
                zonenkalender += z.Stand.KalenderAnzahl;
                if (EigeneWerte(z)) namen.Add(z.Name);
            }

            return new Konditionierungsbilanz(
                new[]
                {
                    Ersetzt(Konditionierungspostenart.Matrixzellen, zellen),
                    Ersetzt(Konditionierungspostenart.Kalender, alt.KalenderAnzahl),
                    Ersetzt(Konditionierungspostenart.EigenePerioden, eigene),
                    Ersetzt(Konditionierungspostenart.Feiertage, feiertage),
                    Ersetzt(Konditionierungspostenart.Nachtzeiten, nacht),
                    Ersetzt(Konditionierungspostenart.Ferienzeitraeume, ferien),
                },
                new[] { Ersetzt(Konditionierungspostenart.Zonenkalender, zonenkalender) },
                namen);
        }

        /// <summary>Trägt die Zone eigene Werte — eine Vorgabezelle, einen Kalender oder eine eigene Bestandszelle?</summary>
        private static bool EigeneWerte(Konditionierungszone z)
        {
            if (!z.Stand.TabellenLeer) return true;
            Matrixeingang b = z.Stand.Bestand;
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                foreach (string zeile in DbWerte.KOND_ZEILEN)
                    if (Matrixzellenort.HatBestandsspalte(Kalendereigentuemer.Zone, g, zeile) && Bestandswert(b, g, zeile).HasValue)
                        return true;
            return false;
        }

        /// <summary>Die Zonen, deren geltender Kalender der Größe sich zwischen zwei Ständen ändert — nur für eine Handlung am Gebäude.</summary>
        private static List<string> BetroffeneZonen(Konditionierungsarbeitsstand vor, Konditionierungsarbeitsstand nach,
                                                    Konditionierungsort ort)
        {
            var namen = new List<string>();
            if (ort.Zone.HasValue) return namen;
            foreach (Konditionierungszone z in vor.Zonen)
                if (!Kalendervergleich.KalenderGleich(vor.GeltenderKalender(ort.Groesse, z.Id),
                                                      nach.GeltenderKalender(ort.Groesse, z.Id)))
                    namen.Add(z.Name);
            return namen;
        }

        private static Konditionierungsbilanz MitZonen(Konditionierungsbilanz b, IEnumerable<string> zonen)
            => new Konditionierungsbilanz(b.Ersetzt, b.Bleibt, zonen);

        // =================================================================
        //  Der Abdruck
        // =================================================================

        /// <summary>
        /// <b>Der Abdruck eines Arbeitsstands</b> — ein Text, der sich genau dann ändert, wenn sich
        /// die Konditionierung ändert (Bestandsfelder, Vorgabezellen, Kalender, Herkunft, Zonen). Bitgenau,
        /// ohne Kultur.
        /// </summary>
        public static string Abdruck(Konditionierungsarbeitsstand stand)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            var sb = new StringBuilder();
            sb.Append("J").Append(stand.Referenzjahr.ToString(CultureInfo.InvariantCulture))
              .Append("|A").Append(Z(stand.Nutzflaeche)).Append('\n');
            Ebenenabdruck(sb, "G", stand.Gebaeude);
            foreach (Konditionierungszone z in stand.Zonen)
            {
                sb.Append("Z").Append(z.Id.ToString(CultureInfo.InvariantCulture)).Append('|').Append(z.Name)
                  .Append('|').Append(Z(z.Nutzflaeche)).Append('|').Append(z.IstBeheizt ? '1' : '0').Append('\n');
                Ebenenabdruck(sb, "E", z.Stand);
            }
            return sb.ToString();
        }

        private static void Ebenenabdruck(StringBuilder sb, string marke, Konditionierungsstand e)
        {
            sb.Append(marke).Append(e.Art).Append('\n');
            Matrixeingang b = e.Bestand;
            sb.Append("B").Append(Z(b.SollTag)).Append(';').Append(Z(b.SollNacht)).Append(';').Append(Z(b.SollWochenende))
              .Append(';').Append(Z(b.SollFerien)).Append(';').Append(Z(b.NachtBeginn)).Append(';').Append(Z(b.NachtEnde))
              .Append(';').Append(Z(b.Ferienmerker)).Append(';').Append(Z(b.Wochenendmerker))
              .Append(';').Append(Z(b.KuehlSollwert)).Append(';').Append(Z(b.KuehlSollwertNacht))
              .Append(';').Append(Z(b.LuftwechselInfiltration)).Append(';').Append(Z(b.LuftwechselNutzer))
              .Append(';').Append(Z(b.Luftwechselrate)).Append(';').Append(Z(b.InterneWaermegewinne))
              .Append(';').Append(Z(b.Bewohner)).Append(';').Append(Z(b.Maximaleraumtemperatur));
            for (int i = 0; i < Matrixeingang.FERIENZEITRAEUME; i++)
                sb.Append(';').Append(Z(b.Ferienbeginn[i])).Append('-').Append(Z(b.Ferienende[i]));
            sb.Append('\n');
            foreach (Vorgabezeile v in e.Vorgabezeilen())
                sb.Append("V").Append(v.Groesse).Append('/').Append(v.Zeile).Append('=').Append(Z(v.Wert))
                  .Append(v.Aus ? "|aus" : "").Append('|').Append(Z(v.Von)).Append('|').Append(Z(v.Bis))
                  .Append('|').Append(Z(v.BedingtK)).Append('\n');
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                Konditionierungskalender k = e.Kalender(g);
                if (k == null) continue;
                var zeile = new Kalenderzeile();
                var perioden = new List<Periodenzeile>();
                Kalenderleser.Schreiben(k, zeile, perioden);
                sb.Append("K").Append(zeile.Groesse).Append('=').Append(Z(zeile.Wert)).Append(zeile.Aus ? "|aus" : "")
                  .Append('|').Append(zeile.Woche).Append('|').Append(Z(zeile.Nennwert)).Append('|')
                  .Append(e.Herkunft(g).Bemerkung()).Append('\n');
                foreach (Periodenzeile p in perioden)
                    sb.Append("P").Append(p.Rang.ToString(CultureInfo.InvariantCulture)).Append('|').Append(p.Art)
                      .Append('|').Append(p.Bezeichner).Append('|').Append(Z(p.Beginn)).Append('|').Append(Z(p.Ende))
                      .Append('|').Append(p.Feiertagsregel).Append('|').Append(Z(p.Wert)).Append(p.Aus ? "|aus" : "")
                      .Append('|').Append(p.Woche).Append('|').Append(Z(p.WieWochentag)).Append('\n');
            }
        }

        // =================================================================
        //  Die Lasten (P1)
        // =================================================================

        /// <summary>
        /// <b>Der Vorschlag für den Personen-Nennwert</b> [W] (Konzept 3.1): Personenzahl ×
        /// <see cref="Matrixeingang.PERSON_W"/>. Die Zahl kommt aus <c>Bewohner</c>, sonst aus
        /// Nutzfläche ÷ <c>Flaeche_Nutzer</c>; ohne beides 0.
        /// </summary>
        public static double PersonenNennwertVorschlag(double? bewohner, double nutzflaecheM2, double? flaecheJeNutzer)
        {
            double zahl = bewohner.HasValue && bewohner.Value > 0.0
                ? bewohner.Value
                : flaecheJeNutzer.HasValue && flaecheJeNutzer.Value > 0.0 && nutzflaecheM2 > 0.0
                    ? nutzflaecheM2 / flaecheJeNutzer.Value
                    : 0.0;
            return zahl * Matrixeingang.PERSON_W;
        }

        /// <summary>
        /// <b>Der Geräte-Nennwert nach dem Anlegen eines Personenkalenders</b> (P1 (b),
        /// energieerhaltend): <c>Interne_Waermegewinne</c> − Jahresmittel der Personenwärme, nie unter 0.
        /// </summary>
        public static double GeraeteNennwertNachPersonen(double interneWaermegewinneW, Konditionierungskalender personen,
                                                        int w0, int referenzjahr)
        {
            if (personen == null) return interneWaermegewinneW;
            double rest = interneWaermegewinneW - PersonenJahresmittelW(personen, w0, referenzjahr);
            return rest > 0.0 ? rest : 0.0;
        }

        /// <summary>Das Jahresmittel der Personenwärme [W] — Anteil × Nennwert über 8 760 Stunden.</summary>
        public static double PersonenJahresmittelW(Konditionierungskalender personen, int w0, int referenzjahr)
        {
            if (personen == null) return 0.0;
            double nennwert = personen.Nennwert ?? 0.0;
            double[] anteil = personen.Auswerten(w0, referenzjahr);
            double summe = 0.0;
            foreach (double a in anteil) summe += a;
            return summe * nennwert / anteil.Length;
        }

        // =================================================================
        //  Helfer
        // =================================================================

        /// <summary>Der Wert einer Bestandszelle im Eingang; <c>null</c> = leer oder keine Bestandszelle.</summary>
        public static double? Bestandswert(Matrixeingang b, Konditionierungsgroesse g, string zeile)
        {
            if (b == null) throw new ArgumentNullException(nameof(b));
            switch (Schluessel(g, zeile))
            {
                case "HEIZSOLL|TAG": return b.SollTag;
                case "HEIZSOLL|NACHT": return b.SollNacht;
                case "HEIZSOLL|WOCHENENDE": return b.SollWochenende;
                case "HEIZSOLL|FERIEN": return b.SollFerien;
                case "KUEHLSOLL|TAG": return b.KuehlSollwert;
                case "KUEHLSOLL|NACHT": return b.KuehlSollwertNacht;
                case "LUEFTUNG|NENNWERT": return b.LuftwechselInfiltration;
                case "LUEFTUNG|TAG": return b.LuftwechselNutzer;
                case "GERAETE|NENNWERT": return b.InterneWaermegewinne;
                default: return null;
            }
        }

        /// <summary>Setzt den Wert einer Bestandszelle im Eingang; eine Zelle ohne Bestandsspalte bleibt unberührt.</summary>
        public static void Bestandswert(Matrixeingang b, Konditionierungsgroesse g, string zeile, double? wert)
        {
            if (b == null) throw new ArgumentNullException(nameof(b));
            switch (Schluessel(g, zeile))
            {
                case "HEIZSOLL|TAG": b.SollTag = wert; break;
                case "HEIZSOLL|NACHT": b.SollNacht = wert; break;
                case "HEIZSOLL|WOCHENENDE": b.SollWochenende = wert; break;
                case "HEIZSOLL|FERIEN": b.SollFerien = wert; break;
                case "KUEHLSOLL|TAG": b.KuehlSollwert = wert; break;
                case "KUEHLSOLL|NACHT": b.KuehlSollwertNacht = wert; break;
                case "LUEFTUNG|NENNWERT": b.LuftwechselInfiltration = wert; HerkunftDesLuftwechsels(b); break;
                case "LUEFTUNG|TAG": b.LuftwechselNutzer = wert; HerkunftDesLuftwechsels(b); break;
                case "GERAETE|NENNWERT": b.InterneWaermegewinne = wert; break;
            }
        }

        /// <summary>Stellt die Herkunft des wirksamen Luftwechsels neu fest (Gesamtangabe oder getrennt, F15).</summary>
        public static void HerkunftDesLuftwechsels(Matrixeingang b)
        {
            Gebaeudemodellvorgaben.WirksamerLuftwechsel(b.Luftwechselrate, b.LuftwechselInfiltration, b.LuftwechselNutzer,
                                                        out Luftwechselherkunft herkunft);
            b.LuftwechselAusGesamtangabe = herkunft == WindowsFormsApplication1.Luftwechselherkunft.Luftwechselrate;
        }

        private static string Schluessel(Konditionierungsgroesse g, string zeile)
            => Konditionierungsgroessen.Kennwort(g) + "|" + zeile;

        /// <summary>Dieselbe Zelle mit anderen Zeiten.</summary>
        internal static Matrixzelle Mit(Matrixzelle z, int? von, int? bis)
            => z.Aus ? Matrixzelle.Abgeschaltet(von, bis, z.BedingtK)
               : z.Belegt ? Matrixzelle.AusWert(z.Wert, von, bis, z.BedingtK)
               : Matrixzelle.NurZeiten(von, bis, z.BedingtK);

        /// <summary>Die Ablehnung eines Generatorbefunds — derselbe Satz wie im Lauf.</summary>
        internal static string Abgelehnt(Fahrplanlesung l)
            => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KOND_FAHRPLAN_ABGELEHNT,
                             l.Befund.ToString(), l.Fundstelle());

        private static Konditionierungsschritt ZoneFehlt(Konditionierungsort ort)
            => Konditionierungsschritt.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_ZONE_FEHLT,
                                                            Zahl(ort.Zone)));

        /// <summary>Die Bilanz eines ersetzten Matrixbereichs (P12): was ersetzt wird, was bleibt.</summary>
        internal static Konditionierungsbilanz MatrixbereichBilanz(Konditionierungskalender alt, int zellen = 0)
        {
            int ferien = 0, saison = 0, eigene = 0, feiertage = 0;
            foreach (Kalenderregel r in alt.Perioden)
            {
                if (string.Equals(r.Art, DbWerte.KOND_ART_FERIEN, StringComparison.Ordinal)) ferien++;
                else if (string.Equals(r.Art, DbWerte.KOND_ART_BETRIEBSPAUSE, StringComparison.Ordinal)) saison++;
                else if (r.IstFeiertag) feiertage++;
                else eigene++;
            }
            return new Konditionierungsbilanz(
                new[]
                {
                    new Konditionierungsposten(Konditionierungspostenart.Matrixzellen, zellen),
                    new Konditionierungsposten(Konditionierungspostenart.Standardwoche, 1),
                    new Konditionierungsposten(Konditionierungspostenart.Ferienperioden, ferien),
                    new Konditionierungsposten(Konditionierungspostenart.Saisonperioden, saison),
                },
                new[]
                {
                    new Konditionierungsposten(Konditionierungspostenart.EigenePerioden, eigene),
                    new Konditionierungsposten(Konditionierungspostenart.Feiertage, feiertage),
                },
                null);
        }

        /// <summary>Die Posten eines ganzen Kalenders (Verwerfen): er selbst, seine eigenen Perioden und Feiertage.</summary>
        internal static IEnumerable<Konditionierungsposten> Kalenderposten(Konditionierungskalender k, bool mitKalender)
        {
            int eigene = 0, feiertage = 0;
            foreach (Kalenderregel r in k.Perioden)
            {
                if (IstMatrixbereich(r)) continue;
                if (r.IstFeiertag) feiertage++;
                else eigene++;
            }
            if (mitKalender) yield return new Konditionierungsposten(Konditionierungspostenart.Kalender, 1);
            yield return new Konditionierungsposten(Konditionierungspostenart.EigenePerioden, eigene);
            yield return new Konditionierungsposten(Konditionierungspostenart.Feiertage, feiertage);
        }

        internal static Konditionierungsposten Ersetzt(Konditionierungspostenart art, int anzahl)
            => new Konditionierungsposten(art, anzahl);

        internal static Konditionierungsbilanz Bilanz(params Konditionierungsposten[] ersetzt)
            => new Konditionierungsbilanz(ersetzt, null, null);

        internal static Konditionierungsbilanz Bilanz(IEnumerable<Konditionierungsposten> ersetzt)
            => new Konditionierungsbilanz(ersetzt, null, null);

        private static string Zahltext(double w) => w.ToString("G6", CultureInfo.InvariantCulture);

        private static string Zahl(long? n) => n.HasValue ? n.Value.ToString(CultureInfo.InvariantCulture) : "—";

        private static string Zahl(int? n) => n.HasValue ? n.Value.ToString(CultureInfo.InvariantCulture) : "—";

        private static string Z(double? w) => w.HasValue ? w.Value.ToString("R", CultureInfo.InvariantCulture) : "";

        private static string Z(int? w) => w.HasValue ? w.Value.ToString(CultureInfo.InvariantCulture) : "";
    }

    /// <summary>
    /// <b>Bitgenaue Vergleiche</b> von Zellen, Kalendern und Bestandsfeldern — für die Folgeregel
    /// (E56 F2 (a): „solange sein Matrixbereich dem Generator gleicht"), für „nichts geändert" und
    /// für die Paritätsprobe. NaN gleicht NaN; ohne Kultur, ohne Toleranz.
    /// </summary>
    public static class Kalendervergleich
    {
        /// <summary>Zwei Zahlen bitgleich (NaN = NaN)?</summary>
        public static bool Gleich(double a, double b)
            => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b) || (double.IsNaN(a) && double.IsNaN(b));

        /// <summary>Zwei nullbare Zahlen bitgleich?</summary>
        public static bool Gleich(double? a, double? b)
            => a.HasValue == b.HasValue && (!a.HasValue || Gleich(a.Value, b.Value));

        /// <summary>Zwei Zellen gleich — Belegung, „aus", Wert, Zeiten, ΔT?</summary>
        public static bool ZelleGleich(Matrixzelle a, Matrixzelle b)
        {
            a ??= Matrixzelle.Leer;
            b ??= Matrixzelle.Leer;
            if (a.Belegt != b.Belegt || a.Aus != b.Aus || a.Von != b.Von || a.Bis != b.Bis) return false;
            if (!Gleich(a.BedingtK, b.BedingtK)) return false;
            return !a.Belegt || a.Aus || Gleich(a.Wert, b.Wert);
        }

        /// <summary>Zwei Angaben gleich — Art, Wert, Woche, Wochentag?</summary>
        public static bool AngabeGleich(Kalenderangabe a, Kalenderangabe b)
        {
            if (a == null || b == null) return a == b;
            if (a.Art != b.Art || a.WieWochentag != b.WieWochentag) return false;
            if (a.Art == Angabeart.Wert && !Gleich(a.Wert, b.Wert)) return false;
            if (a.Art == Angabeart.Woche)
            {
                if (a.Woche.Count != b.Woche.Count) return false;
                for (int i = 0; i < a.Woche.Count; i++)
                    if (!Gleich(a.Woche[i], b.Woche[i])) return false;
            }
            return true;
        }

        /// <summary>Zwei Perioden gleich — Rang, Art, Bezeichner, Tage oder Regel, Angabe?</summary>
        public static bool RegelGleich(Kalenderregel a, Kalenderregel b)
            => a.Rang == b.Rang && string.Equals(a.Art, b.Art, StringComparison.Ordinal)
               && string.Equals(a.Bezeichner, b.Bezeichner, StringComparison.Ordinal)
               && a.Beginn == b.Beginn && a.Ende == b.Ende
               && string.Equals(a.Feiertagsregel, b.Feiertagsregel, StringComparison.Ordinal)
               && AngabeGleich(a.Angabe, b.Angabe);

        /// <summary>Zwei Kalender gleich — Größe, Grundangabe, Nennwert und jede Periode?</summary>
        public static bool KalenderGleich(Konditionierungskalender a, Konditionierungskalender b)
        {
            if (a == null || b == null) return a == b;
            if (a.Groesse != b.Groesse || !Gleich(a.Nennwert, b.Nennwert) || !AngabeGleich(a.Grundangabe, b.Grundangabe))
                return false;
            if (a.Perioden.Count != b.Perioden.Count) return false;
            for (int i = 0; i < a.Perioden.Count; i++)
                if (!RegelGleich(a.Perioden[i], b.Perioden[i])) return false;
            return true;
        }

        /// <summary>
        /// <b>Gleicht der Matrixbereich eines Kalenders dem eines anderen</b> — Grundangabe samt
        /// Standardwoche, Nennwert und die Perioden der Arten Ferien und Betriebspause (P12)? Die
        /// eigenen Perioden zählen nicht.
        /// </summary>
        public static bool MatrixbereichGleich(Konditionierungskalender a, Konditionierungskalender b)
        {
            if (a == null || b == null) return a == b;
            if (a.Groesse != b.Groesse || !Gleich(a.Nennwert, b.Nennwert) || !AngabeGleich(a.Grundangabe, b.Grundangabe))
                return false;
            List<Kalenderregel> ma = Matrixbereich(a), mb = Matrixbereich(b);
            if (ma.Count != mb.Count) return false;
            for (int i = 0; i < ma.Count; i++)
                if (!RegelGleich(ma[i], mb[i])) return false;
            return true;
        }

        private static List<Kalenderregel> Matrixbereich(Konditionierungskalender k)
        {
            var l = new List<Kalenderregel>();
            foreach (Kalenderregel r in k.Perioden)
                if (Konditionierungsarbeit.IstMatrixbereich(r)) l.Add(r);
            return l;
        }

        /// <summary>Zwei Bestandsfelder gleich — jede Zahl, Zeit, jeder Merker und Ferienzeitraum?</summary>
        public static bool BestandGleich(Matrixeingang a, Matrixeingang b)
        {
            if (a == null || b == null) return a == b;
            bool gleich = Gleich(a.SollTag, b.SollTag) && Gleich(a.SollNacht, b.SollNacht)
                          && Gleich(a.SollWochenende, b.SollWochenende) && Gleich(a.SollFerien, b.SollFerien)
                          && a.NachtBeginn == b.NachtBeginn && a.NachtEnde == b.NachtEnde
                          && Gleich(a.Ferienmerker, b.Ferienmerker) && Gleich(a.Wochenendmerker, b.Wochenendmerker)
                          && string.Equals(a.Sollwertprofil, b.Sollwertprofil, StringComparison.Ordinal)
                          && a.KopplungWirksam == b.KopplungWirksam
                          && Gleich(a.KuehlSollwert, b.KuehlSollwert) && Gleich(a.KuehlSollwertNacht, b.KuehlSollwertNacht)
                          && a.KuehlungWirksam == b.KuehlungWirksam
                          && Gleich(a.LuftwechselInfiltration, b.LuftwechselInfiltration)
                          && Gleich(a.LuftwechselNutzer, b.LuftwechselNutzer)
                          && Gleich(a.Luftwechselrate, b.Luftwechselrate)
                          && a.LuftwechselAusGesamtangabe == b.LuftwechselAusGesamtangabe
                          && Gleich(a.InterneWaermegewinne, b.InterneWaermegewinne) && Gleich(a.Bewohner, b.Bewohner)
                          && Gleich(a.Maximaleraumtemperatur, b.Maximaleraumtemperatur);
            if (!gleich) return false;
            for (int i = 0; i < Matrixeingang.FERIENZEITRAEUME; i++)
                if (!Gleich(a.Ferienbeginn[i], b.Ferienbeginn[i]) || !Gleich(a.Ferienende[i], b.Ferienende[i])) return false;
            return true;
        }
    }
}

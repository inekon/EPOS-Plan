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
                    e = e.MitBestand(b => Bestandswert(b, groesse, zeile, zelle.Wert));
                else if (!zelle.Belegt && e.Art == Kalendereigentuemer.Zone)
                    e = e.MitBestand(b => Bestandswert(b, groesse, zeile, null));
                vorgabe = zelle.Aus
                    ? Matrixzelle.Abgeschaltet(zelle.Von, zelle.Bis, zelle.BedingtK)
                    : Matrixzelle.NurZeiten(zelle.Von, zelle.Bis, zelle.BedingtK);
            }
            return Ebenenergebnis.Gut(e.MitVorgabe(groesse, zeile, vorgabe));
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
            return Ebenenergebnis.Gut(ebene.MitKalender(groesse, neu, Kalenderherkunft.Keine));
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
        /// vorhandenen Kalenders samt Rang; die Perioden der Vorlage im Eigenband dazu — eine
        /// Feiertagsregel, die schon steht, nur einmal.
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

            var ausDerVorlage = new List<Kalenderregel>();
            if (ausVorlage != null)
                foreach (Kalenderregel r in ausVorlage.Perioden)
                {
                    if (r.IstFeiertag && !feiertagsregeln.Add(r.Feiertagsregel)) continue;
                    ausDerVorlage.Add(r);
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

        /// <summary>
        /// <b>Ein Werkzeug der Karte</b> auf den angelegten Kalender einer Größe
        /// (<see cref="Kalenderwerkzeuge"/>): Ohne angelegten Kalender gibt es nichts zu ändern — das
        /// wird benannt abgelehnt, statt still einen anzulegen. Der Vermerk des Werkzeugs geht in die
        /// Herkunft.
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
            return Ebenenergebnis.Gut(ebene.MitKalender(groesse, b.Kalender, new Kalenderherkunft(null, b.Vermerk)));
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
        /// </summary>
        public static Konditionierungsschritt ZelleSetzen(Konditionierungsarbeitsstand stand, Konditionierungsort ort,
                                                          string zeile, Matrixzelle zelle)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (ort == null) throw new ArgumentNullException(nameof(ort));
            if (zelle == null) throw new ArgumentNullException(nameof(zelle));
            Konditionierungsstand ebene = stand.Ebene(ort.Zone);
            if (ebene == null) return ZoneFehlt(ort);

            string pruefung = Zellenpruefung(ort.Groesse, zeile, zelle);
            if (pruefung != null) return Konditionierungsschritt.Fehler(pruefung);

            Ebenenergebnis r = Eintragen(ebene, ort.Groesse, zeile, zelle);
            if (!r.Ok) return Konditionierungsschritt.Fehler(r.Meldung);
            Konditionierungsarbeitsstand neu = stand.MitEbene(ort.Zone, r.Stand);
            return Konditionierungsschritt.Gut(neu, Bilanz(Ersetzt(Konditionierungspostenart.Matrixzellen, 1)));
        }

        /// <summary>
        /// <b>„Kalender anlegen"</b> am Ort (Konzept 3.3): der Generator über der wirksamen Matrix des
        /// Orts, mit dem Rundlauf; ein vorhandener Kalender derselben Größe wird ersetzt.
        /// </summary>
        public static Konditionierungsschritt Anlegen(Konditionierungsarbeitsstand stand, Konditionierungsort ort)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (ort == null) throw new ArgumentNullException(nameof(ort));
            Konditionierungsstand ebene = stand.Ebene(ort.Zone);
            if (ebene == null) return ZoneFehlt(ort);

            Ebenenergebnis r = KalenderAnlegen(ebene, stand.Matrix(ort.Zone), ort.Groesse);
            if (!r.Ok) return Konditionierungsschritt.Fehler(r.Meldung);
            return Konditionierungsschritt.Gut(stand.MitEbene(ort.Zone, r.Stand),
                                               Bilanz(Ersetzt(Konditionierungspostenart.Kalender, 1)));
        }

        /// <summary><b>„Verwerfen"</b> am Ort: Der angelegte Kalender fällt samt Perioden, die Matrix bleibt.</summary>
        public static Konditionierungsschritt Verwerfen(Konditionierungsarbeitsstand stand, Konditionierungsort ort)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (ort == null) throw new ArgumentNullException(nameof(ort));
            Konditionierungsstand ebene = stand.Ebene(ort.Zone);
            if (ebene == null) return ZoneFehlt(ort);
            Konditionierungskalender k = ebene.Kalender(ort.Groesse);
            if (k == null) return Konditionierungsschritt.Gut(stand);
            return Konditionierungsschritt.Gut(stand.MitEbene(ort.Zone, KalenderVerwerfen(ebene, ort.Groesse)),
                                               Bilanz(Kalenderposten(k, mitKalender: true)));
        }

        /// <summary><b>„Matrix erneut anwenden"</b> am Ort (P12 (a)) — nur der Matrixbereich; ohne angelegten Kalender „Anlegen".</summary>
        public static Konditionierungsschritt MatrixErneut(Konditionierungsarbeitsstand stand, Konditionierungsort ort)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (ort == null) throw new ArgumentNullException(nameof(ort));
            Konditionierungsstand ebene = stand.Ebene(ort.Zone);
            if (ebene == null) return ZoneFehlt(ort);
            Konditionierungskalender alt = ebene.Kalender(ort.Groesse);
            Ebenenergebnis r = MatrixbereichErsetzen(ebene, stand.Matrix(ort.Zone), ort.Groesse);
            if (!r.Ok) return Konditionierungsschritt.Fehler(r.Meldung);
            return Konditionierungsschritt.Gut(stand.MitEbene(ort.Zone, r.Stand),
                                               alt == null ? Bilanz(Ersetzt(Konditionierungspostenart.Kalender, 1))
                                                           : MatrixbereichBilanz(alt));
        }

        /// <summary>
        /// <b>„Vorlage übernehmen"</b> am Ort (P11, P12): nur in der Größe der Vorlage; die Zellen der
        /// Vorlage in die Spalte, der Kalender angelegt (<see cref="VorlageEintragen"/>).
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

            Konditionierungskalender alt = ebene.Kalender(ort.Groesse);
            Ebenenergebnis r = VorlageEintragen(ebene, stand.Matrix(ort.Zone), vorlage);
            if (!r.Ok) return Konditionierungsschritt.Fehler(r.Meldung);
            int zellen = (vorlage.Inhalt ?? Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null)).VorgabenAnzahl;
            Konditionierungsbilanz b = alt == null
                ? Bilanz(Ersetzt(Konditionierungspostenart.Matrixzellen, zellen), Ersetzt(Konditionierungspostenart.Kalender, 1))
                : MatrixbereichBilanz(alt, zellen);
            return Konditionierungsschritt.Gut(stand.MitEbene(ort.Zone, r.Stand), b);
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

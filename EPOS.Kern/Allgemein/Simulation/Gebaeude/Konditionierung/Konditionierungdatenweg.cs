using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Datenweg der Konditionierung</b> (Stufe KP1, Konzept Konditionierungsprofile 5.1, 5.6
    /// und 6): Er liest <c>Tab_Konditionierungskalender</c>, <c>Tab_Konditionierungsperiode</c> und
    /// <c>Tab_Konditionierungsvorgabe</c> zu einem Eigentümer und macht daraus den
    /// <see cref="Konditionierungssatz"/>, mit dem der Lauf rechnet.
    ///
    /// <para><b>Die einzige Stelle mit Datenbankzugriff</b> im Modul <c>Konditionierung/</c> — das
    /// Kalendermodell selbst kennt keine Datenbank. Zugriffe gehen über
    /// <see cref="DataRepository"/> mit <c>?</c>-Parametern, nie mit zusammengesetztem SQL-Text.</para>
    ///
    /// <para><b>Ergebnisneutral, wo nichts steht</b> (Konzept 6): Fehlen die Tabellen (ein
    /// Datenbankstand vor Schritt <see cref="KonditionierungSchema.SCHRITT"/>) oder trägt der
    /// Eigentümer keinen Kalender und keine neue Matrixzelle, gibt der Datenweg <c>null</c> zurück —
    /// der Eingang nimmt dann wörtlich den Bestandszweig, und der Referenzlauf bleibt byte-gleich.</para>
    ///
    /// <para><b>Der Lauf liest ausschließlich Projektmatrix und Projektkalender</b> (Konzept 6): Die
    /// Kalender eines Katalogbaus erreichen ein Projekt allein über die Übernahme (P3 (b)), nie über
    /// den Lauf.</para>
    /// </summary>
    public static class Konditionierungdatenweg
    {
        /// <summary>
        /// <b>Der Satz eines Projektgebäudes oder einer seiner Zonen.</b> <c>null</c> heißt: wörtlich
        /// der Bestandszweig.
        /// </summary>
        /// <param name="gebaeude">Die Gebäudezeile des Laufs (mit aufgelösten Zonenvorgaben).</param>
        /// <param name="wochenende">Die Wochenendmaske des Ortszeit-Kalenders — sie liefert w₀ (U7).</param>
        /// <param name="referenzjahr">Das Referenzjahr des Laufs; es löst die Feiertagsregeln auf (F11).</param>
        /// <param name="kopplungWirksam">Wirkt die Anlagenkopplung (AK1)? Dann ersetzt <c>Sollwertprofil</c> die Woche der Heizspalte.</param>
        /// <param name="kuehlungWirksam">Wirkt die Kühlung (Projektschalter, <c>Kuehlung_Aktiv</c>, Sollwert; E32)?</param>
        /// <param name="idZone">Die Zone, deren Satz gesucht ist, oder <c>null</c> für das Gebäude.</param>
        /// <exception cref="GebaeudeModellException">
        /// Wenn eine Zeile ungültig ist (<see cref="GebaeudeModellFehler.KalenderUngueltig"/>) oder der
        /// Generator die Matrix nicht annimmt — mit Größe, Periode und Stelle in der Meldung. Keine
        /// stille Umdeutung.
        /// </exception>
        public static Konditionierungssatz Satz(ProjektGebaeudeModel gebaeude, bool[] wochenende,
                                                int referenzjahr, bool kopplungWirksam,
                                                bool kuehlungWirksam, long? idZone = null)
        {
            if (gebaeude == null) throw new ArgumentNullException(nameof(gebaeude));
            if (!KonditionierungSchema.Lesbar()) return null;

            // DIE VORSCHAU DES ARBEITSSTANDS (Befund NB3): Ein Gebaeude ohne Projektkopie
            // (ID_Gebaeude = 0) traegt den Katalogbau, aus dem der Speicherweg es kopieren wird -
            // dann liest der Datenweg DESSEN Konditionierung, damit die Vorschau vor dem OK
            // dieselbe Zahl zeigt wie der Lauf danach (Konzept 3.4, P3 (b)). Der LAUF geht hier
            // nie herein: Ein Projektgebaeude traegt keine Katalogquelle.
            if (gebaeude.ID_Gebaeude <= 0 && gebaeude.KonditionierungKatalogbau.HasValue && !idZone.HasValue)
                return Satz(KonditionierungCtrl.Eigner.Katalogbau(gebaeude.KonditionierungKatalogbau.Value),
                            gebaeude, wochenende, referenzjahr, kopplungWirksam, kuehlungWirksam);

            long idGebaeude = gebaeude.ID_Gebaeude;
            List<Kalenderzeile> kalenderzeilen = Kalenderzeilen(idGebaeude, null);
            List<Periodenzeile> perioden = Periodenzeilen(idGebaeude, null);
            List<Vorgabezeile> vorgaben = Vorgabezeilen(idGebaeude, null);

            List<Kalenderzeile> zonenkalender = idZone.HasValue ? Kalenderzeilen(idGebaeude, idZone) : null;
            List<Periodenzeile> zonenperioden = idZone.HasValue ? Periodenzeilen(idGebaeude, idZone) : null;
            List<Vorgabezeile> zonenvorgaben = idZone.HasValue ? Vorgabezeilen(idGebaeude, idZone) : null;

            bool leer = kalenderzeilen.Count == 0 && vorgaben.Count == 0
                        && (zonenkalender == null || zonenkalender.Count == 0)
                        && (zonenvorgaben == null || zonenvorgaben.Count == 0);
            if (leer) return null;      // wörtlich der Bestandszweig

            Matrixeingang bestand = Konditionierungseingang.Bestand(gebaeude, kopplungWirksam, kuehlungWirksam);
            Dictionary<Konditionierungsgroesse, Konditionierungskalender> gebaeudeangelegt =
                Angelegt(kalenderzeilen, perioden, gebaeude.Gebaeudename);
            Dictionary<Konditionierungsgroesse, Konditionierungskalender> zoneangelegt =
                idZone.HasValue ? Angelegt(zonenkalender, zonenperioden, gebaeude.Gebaeudename) : null;
            return SatzBilden(gebaeude.Gebaeudename, bestand, vorgaben, gebaeudeangelegt,
                              idZone.HasValue ? zonenvorgaben : null, zoneangelegt, wochenende, referenzjahr,
                              idZone.HasValue ? Nennwerte(gebaeude, idZone.Value, zonenvorgaben) : null);
        }

        /// <summary>
        /// <b>Die Nennwerte einer Zone im Lauf</b> (Konzept 3.4; Stufe KP2, Welle U4): der Flächenanteil
        /// aus derselben Vorgabenkaskade wie ihre inneren Gewinne (<see cref="Zonenvorgaben"/>), die
        /// eigenen inneren Gewinne der Zone und ihr eigener Personen-Nennwert. Führt die Gebäudezeile
        /// die Zone nicht, gilt der Anteil 1 — wie ohne eigene Nutzfläche.
        /// </summary>
        private static Zonennennwerte Nennwerte(ProjektGebaeudeModel gebaeude, long idZone, List<Vorgabezeile> zonenvorgaben)
        {
            GebaeudeZonensatz zone = null;
            foreach (GebaeudeZonensatz z in gebaeude.Zonen ?? new List<GebaeudeZonensatz>())
                if (z != null && z.ZonenId == idZone) { zone = z; break; }
            if (zone == null) return Zonennennwerte.Aus(1.0, null, zonenvorgaben);
            Zoneneingaben e = zone.EingabenOderNutzflaeche();
            double anteil = Zonenvorgaben.Bilden(e, Gebaeudevorgaben.Aus(gebaeude), Math.Max(1, gebaeude.Zonen.Count))
                                         .Flaechenanteil;
            return Zonennennwerte.Aus(anteil, new Matrixeingang { InterneWaermegewinne = e.InterneWaermegewinne },
                                      zonenvorgaben);
        }

        /// <summary>
        /// <b>Der Satz aus dem Speicher</b> (Stufe KP2, Welle K2) — dieselbe Kette wie
        /// <see cref="Satz(ProjektGebaeudeModel, bool[], int, bool, bool, long?)"/>, nur kommen
        /// Vorgabezeilen und angelegte Kalender aus dem Arbeitsstand statt aus der Datenbank und die
        /// Bestandsfelder aus seinen Ebenen (an einer Zone die aufgelösten,
        /// <see cref="Konditionierungsarbeitsstand.AufgeloesterBestand"/>). Für die Vorschau vor dem OK
        /// und die Zonenübernahme; der Lauf liest weiter die Datenbank.
        /// </summary>
        /// <param name="speicher">Der Arbeitsstand.</param>
        /// <param name="idZone">Die Zone, deren Satz gesucht ist, oder <c>null</c> für das Gebäude.</param>
        /// <param name="wochenende">Die Wochenendmaske des Ortszeit-Kalenders — sie liefert w₀ (U7).</param>
        /// <param name="referenzjahr">Das Referenzjahr (F11).</param>
        /// <param name="kopplungWirksam">Wirkt die Anlagenkopplung (AK1)?</param>
        /// <param name="kuehlungWirksam">Wirkt die Kühlung (E32)?</param>
        /// <param name="name">Der Name in einer Fehlermeldung.</param>
        /// <returns>Der Satz oder <c>null</c> — dann gilt wörtlich der Bestandszweig.</returns>
        /// <exception cref="GebaeudeModellException">wie der Datenbankweg.</exception>
        public static Konditionierungssatz Satz(Konditionierungsarbeitsstand speicher, long? idZone, bool[] wochenende,
                                                int referenzjahr, bool kopplungWirksam, bool kuehlungWirksam,
                                                string name = null)
        {
            if (speicher == null) throw new ArgumentNullException(nameof(speicher));
            Konditionierungszone zone = null;
            if (idZone.HasValue)
            {
                zone = speicher.Zone(idZone.Value);
                if (zone == null)
                    throw new ArgumentException("Die Zone " + idZone.Value.ToString(CultureInfo.InvariantCulture) +
                                                " steht nicht im Arbeitsstand.", nameof(idZone));
            }

            Konditionierungsstand gebaeude = speicher.Gebaeude;
            bool leer = gebaeude.TabellenLeer && (zone == null || zone.Stand.TabellenLeer);
            if (leer) return null;      // wörtlich der Bestandszweig

            Matrixeingang bestand = zone == null ? gebaeude.Bestand : speicher.AufgeloesterBestand(zone);
            bestand.KopplungWirksam = kopplungWirksam;
            bestand.KuehlungWirksam = kuehlungWirksam;
            return SatzBilden(name, bestand, gebaeude.Vorgabezeilen(), Kalenderliste(gebaeude),
                              zone?.Stand.Vorgabezeilen(), zone == null ? null : Kalenderliste(zone.Stand),
                              wochenende, referenzjahr, zone == null ? null : speicher.Nennwerte(zone));
        }

        private static Dictionary<Konditionierungsgroesse, Konditionierungskalender> Kalenderliste(Konditionierungsstand e)
        {
            var d = new Dictionary<Konditionierungsgroesse, Konditionierungskalender>();
            foreach (KeyValuePair<Konditionierungsgroesse, Konditionierungskalender> p in e.Angelegt()) d[p.Key] = p.Value;
            return d;
        }

        /// <summary>
        /// <b>Die gemeinsame Kette</b> beider Wege: die Matrix des Gebäudes, darüber die der Zone je
        /// Zelle (F2), je Größe die erste Quelle, die Nachtauskühlung und die zwei Matrixwerte.
        /// <paramref name="zonenvorgaben"/> <c>null</c> heißt: der Satz des Gebäudes selbst. An einer Zone
        /// rechnen die Anteilskalender mit ihren <paramref name="nennwerte"/> (Konzept 3.4, Stufe KP2
        /// Welle U4) — eigener Wert oder Flächenanteil, auch unter dem angelegten Kalender des Gebäudes.
        /// </summary>
        private static Konditionierungssatz SatzBilden(string name, Matrixeingang bestand, List<Vorgabezeile> vorgaben,
                                                       Dictionary<Konditionierungsgroesse, Konditionierungskalender> gebaeudeangelegt,
                                                       List<Vorgabezeile> zonenvorgaben,
                                                       Dictionary<Konditionierungsgroesse, Konditionierungskalender> zoneangelegt,
                                                       bool[] wochenende, int referenzjahr, Zonennennwerte nennwerte = null)
        {
            // ---- Die Matrix: Gebäude, darüber die Zone je Zelle (F2) ----
            Vorgabematrix gebaeudematrix = Vorgabematrix.Bilden(bestand, vorgaben);
            Vorgabematrix matrix = gebaeudematrix;
            if (zonenvorgaben != null)
                matrix = Konditionierungseingang.Zonenmatrix(bestand, zonenvorgaben, gebaeudematrix,
                                                             nennwerte?.Flaechenanteil ?? 1.0);

            // ---- w₀ aus derselben Maske, nach der der Bestandsfahrplan das Wochenende setzt ----
            int w0 = GebaeudeModellEingang.WochentagDesErstenTags(wochenende);
            if (w0 < 0)
                throw Fehler(name, MyResource.Resource.SIMENG_AK_SOLLWERTPROFIL_KALENDER);

            // Festlegung 5 (Entwurf KP2): eine eigene Heiz-Nachtzeile der Zone macht die Heizspalte wirksam.
            bool nachtzeileDerZone = Konditionierungseingang.EigeneNachtzeile(zonenvorgaben);

            var satz = new Konditionierungssatz(w0, referenzjahr);
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                Fahrplanlesung befund;
                Konditionierungskalender k = zonenvorgaben != null
                    ? Konditionierungseingang.ErsteQuelleDerZone(g, matrix, zoneangelegt, gebaeudeangelegt, nachtzeileDerZone,
                                                                 nennwerte, out befund)
                    : Konditionierungseingang.ErsteQuelle(g, matrix, zoneangelegt, gebaeudeangelegt, nachtzeileDerZone,
                                                          out befund);
                if (befund != null && befund.Befund != Fahrplanbefund.Erzeugt &&
                    befund.Befund != Fahrplanbefund.KeineAngabe)
                    throw Fehler(name, string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.SIMENG_KOND_FAHRPLAN_ABGELEHNT,
                        befund.Befund.ToString(), befund.Fundstelle()));
                satz.Setzen(g, k);
            }
            if (satz.Hat(Konditionierungsgroesse.Lueftung))
                satz.NachtauskuehlungSetzen(Nachtauskuehlung(
                    Quelle(Konditionierungsgroesse.Lueftung, matrix, gebaeudematrix, zoneangelegt, gebaeudeangelegt),
                    name));
            Matrixwerte(satz,
                        Quelle(Konditionierungsgroesse.Lueftung, matrix, gebaeudematrix, zoneangelegt, gebaeudeangelegt),
                        Quelle(Konditionierungsgroesse.Heizsoll, matrix, gebaeudematrix, zoneangelegt, gebaeudeangelegt));
            return satz.Wirksam ? satz : null;
        }

        /// <summary>
        /// <b>Die Matrix, aus der die Werte neben den Reihen kommen</b> (Stufe KP1b, Konzept 3.7,
        /// 3.6): die des Eigentümers, dessen Kalender dieser Größe gilt. Trägt die Zone einen
        /// eigenen angelegten Kalender, ist es ihre wirksame Matrix; gilt der angelegte Kalender des
        /// Gebäudes, dessen Matrix; ist der Kalender abgeleitet, die Matrix, aus der der Generator
        /// ihn gebildet hat.
        /// </summary>
        private static Vorgabematrix Quelle(Konditionierungsgroesse groesse,
                                            Vorgabematrix wirksam, Vorgabematrix gebaeudematrix,
                                            IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender> zoneangelegt,
                                            IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender> gebaeudeangelegt)
        {
            if (zoneangelegt != null && zoneangelegt.ContainsKey(groesse))
                return wirksam;
            if (gebaeudeangelegt != null && gebaeudeangelegt.ContainsKey(groesse))
                return gebaeudematrix;
            return wirksam;
        }

        /// <summary>
        /// <b>Die zwei Werte neben den Reihen</b> (Stufe KP1b): die <b>Infiltration</b> aus der
        /// Matrixzelle Lüftung/<c>NENNWERT</c> (Konzept 3.1, 3.3, F15 — der Kalender selbst führt
        /// keinen Nennwert) und der <b>Tagwert der Heizspalte</b> aus Heizen/<c>TAG</c> (Konzept 3.6,
        /// E53). Eine leere Zelle und eine Zelle auf „aus" liefern <c>null</c> — dann rechnet der
        /// Eingang wie ohne Angabe.
        /// </summary>
        private static void Matrixwerte(Konditionierungssatz satz, Vorgabematrix lueftungsquelle,
                                        Vorgabematrix heizquelle)
        {
            double? infiltration = satz.Hat(Konditionierungsgroesse.Lueftung) && lueftungsquelle != null
                ? Zahl(lueftungsquelle.Lueftung.Nennwert)
                : null;
            double? heizTag = satz.Hat(Konditionierungsgroesse.Heizsoll) && heizquelle != null
                ? Zahl(heizquelle.Heizsoll.Tag)
                : null;
            satz.MatrixwerteSetzen(infiltration, heizTag);
        }

        /// <summary>Die Zahl einer Matrixzelle; <c>null</c>, wenn sie leer, „aus" oder nicht endlich ist.</summary>
        private static double? Zahl(Matrixzelle zelle)
            => zelle != null && zelle.Belegt && !zelle.Aus && double.IsFinite(zelle.Wert)
                ? zelle.Wert
                : (double?)null;

        /// <summary>
        /// <b>Die Vorgabe der Nachtauskühlung aus der Matrix</b> (Stufe KP1b, Konzept 3.7, P9 (b)):
        /// das Nachtfenster der Lüftungsspalte (<see cref="Vorgabematrix.Nachtfenster"/>, F19), der
        /// Tagwert n_T der Nutzerlüftung und ΔT aus <c>Bedingt_K</c> (leer = 2 K). Ohne Tagwert
        /// bleibt <see cref="Nachtauskuehlvorgabe.TagwertH"/> leer — dann gibt es keinen bedingten
        /// Anteil, und der Lauf sagt es.
        /// </summary>
        /// <exception cref="GebaeudeModellException">bei ungültigem Nachtfenster oder ΔT außerhalb 0 … 5 K.</exception>
        private static Nachtauskuehlvorgabe Nachtauskuehlung(Vorgabematrix quelle, string name)
        {
            if (quelle == null) return null;
            quelle.Nachtfenster(Konditionierungsgroesse.Lueftung, out int? von, out int? bis);
            NachtzeitBefund nb = Nachtzeit.Pruefen(von, bis);
            if (nb != NachtzeitBefund.Gueltig)
                throw Fehler(name, string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.SIMENG_KOND_FAHRPLAN_ABGELEHNT,
                    Fahrplanbefund.NachtzeitUngueltig.ToString(),
                    Konditionierungsgroessen.Kennwort(Konditionierungsgroesse.Lueftung) + ", " + nb));

            Matrixspalte spalte = quelle.Lueftung;
            double? abstand = spalte.Nacht.BedingtK;
            if (abstand.HasValue && (!double.IsFinite(abstand.Value) ||
                                     abstand.Value < Nachtauskuehlvorgabe.ABSTAND_MIN_K ||
                                     abstand.Value > Nachtauskuehlvorgabe.ABSTAND_MAX_K))
                throw Fehler(name, string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.SIMENG_KOND_NACHTKUEHL_ABSTAND,
                    abstand.Value.ToString("G6", CultureInfo.InvariantCulture),
                    Nachtauskuehlvorgabe.ABSTAND_MIN_K.ToString("G6", CultureInfo.InvariantCulture),
                    Nachtauskuehlvorgabe.ABSTAND_MAX_K.ToString("G6", CultureInfo.InvariantCulture)));

            Matrixzelle tag = spalte.Tag;
            double? tagwert = tag.Belegt && !tag.Aus ? tag.Wert : (double?)null;
            return new Nachtauskuehlvorgabe(Nachtzeit.Aus(von, bis), tagwert, abstand);
        }

        /// <summary>
        /// <b>Der Satz EINES Eigentümers</b> — dieselbe Kette ohne Zonenebene: ein Katalogbau oder
        /// eine Vorlage hat keine Zonen (Konzept 3.4, 5.7). <paramref name="bestand"/> liefert die
        /// Bestandsspalten und die Ferienzeiträume; <c>null</c> heißt wie oben „wörtlich der
        /// Bestandszweig".
        ///
        /// <para>Gebraucht wird er von der <b>Vorschau des Arbeitsstands</b> (Befund NB3) und von
        /// den Werkzeugen der Karte (KP2); der Lauf selbst liest ausschließlich Projektmatrix und
        /// Projektkalender.</para>
        /// </summary>
        public static Konditionierungssatz Satz(KonditionierungCtrl.Eigner eigner,
                                                ProjektGebaeudeModel bestand, bool[] wochenende,
                                                int referenzjahr, bool kopplungWirksam,
                                                bool kuehlungWirksam)
        {
            if (eigner == null) throw new ArgumentNullException(nameof(eigner));
            if (bestand == null) throw new ArgumentNullException(nameof(bestand));
            if (!KonditionierungSchema.Lesbar()) return null;

            List<Kalenderzeile> kalenderzeilen = KalenderzeilenVon(eigner);
            List<Vorgabezeile> vorgaben = VorgabezeilenVon(eigner);
            if (kalenderzeilen.Count == 0 && vorgaben.Count == 0) return null;

            List<Periodenzeile> perioden = PeriodenzeilenVon(kalenderzeilen);
            Vorgabematrix matrix = Vorgabematrix.Bilden(
                Konditionierungseingang.Bestand(bestand, kopplungWirksam, kuehlungWirksam),
                vorgaben, eigner.Art);
            Dictionary<Konditionierungsgroesse, Konditionierungskalender> angelegt =
                Angelegt(kalenderzeilen, perioden, bestand.Gebaeudename);

            int w0 = GebaeudeModellEingang.WochentagDesErstenTags(wochenende);
            if (w0 < 0) throw Fehler(bestand, MyResource.Resource.SIMENG_AK_SOLLWERTPROFIL_KALENDER);

            var satz = new Konditionierungssatz(w0, referenzjahr);
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                Konditionierungskalender k = Konditionierungseingang.ErsteQuelle(
                    g, matrix, null, angelegt, out Fahrplanlesung befund);
                if (befund != null && befund.Befund != Fahrplanbefund.Erzeugt &&
                    befund.Befund != Fahrplanbefund.KeineAngabe)
                    throw Fehler(bestand, string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.SIMENG_KOND_FAHRPLAN_ABGELEHNT,
                        befund.Befund.ToString(), befund.Fundstelle()));
                satz.Setzen(g, k);
            }
            if (satz.Hat(Konditionierungsgroesse.Lueftung))
                satz.NachtauskuehlungSetzen(Nachtauskuehlung(matrix, bestand.Gebaeudename));
            Matrixwerte(satz, matrix, matrix);
            return satz.Wirksam ? satz : null;
        }

        /// <summary>
        /// Die angelegten Kalender eines Eigentümers, je Größe — der strenge Leser über jede Zeile;
        /// eine zweite Zeile derselben Größe ist ein benannter Fehler (die Eindeutigkeit halten die
        /// Teilindizes des Schemaschritts <see cref="KonditionierungVorlagenSchema.SCHRITT"/>,
        /// Konzept 5.1).
        /// </summary>
        private static Dictionary<Konditionierungsgroesse, Konditionierungskalender> Angelegt(
            List<Kalenderzeile> zeilen, List<Periodenzeile> perioden, string name)
        {
            var ziel = new Dictionary<Konditionierungsgroesse, Konditionierungskalender>();
            if (zeilen == null) return ziel;
            foreach (Kalenderzeile z in zeilen)
            {
                Kalenderlesung l = Kalenderleser.Lesen(z, perioden);
                if (l.Befund != Kalenderbefund.Gelesen)
                    throw Fehler(name, string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.SIMENG_KOND_KALENDER_UNGUELTIG,
                        l.Befund.ToString(), l.Fundstelle()));
                if (ziel.ContainsKey(l.Kalender.Groesse))
                    throw Fehler(name, string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.SIMENG_KOND_KALENDER_UNGUELTIG,
                        "zweimal dieselbe Größe", l.Fundstelle()));
                ziel[l.Kalender.Groesse] = l.Kalender;
            }
            return ziel;
        }

        // =================================================================
        //  Die drei Leseabfragen — über DataRepository mit ?-Parametern
        // =================================================================

        /// <summary>Die Kalenderzeilen eines Gebäudes (<paramref name="idZone"/> <c>null</c>) oder einer Zone.</summary>
        public static List<Kalenderzeile> Kalenderzeilen(long idGebaeude, long? idZone)
        {
            var liste = new List<Kalenderzeile>();
            DataTable t = Lesen(
                "SELECT ID, ID_Gebaeude, ID_Zone, ID_Gebaeude_Stamm, ID_Vorlage, Groesse, Wert, Aus, Woche, " +
                "Nennwert, Bemerkung FROM \"" + KonditionierungSchema.TAB_KALENDER +
                "\" WHERE ID_Gebaeude = ? AND ID_Zone IS " + (idZone.HasValue ? "NOT NULL AND ID_Zone = ?" : "NULL") +
                " ORDER BY Groesse, ID", idGebaeude, idZone);
            if (t == null) return liste;
            foreach (DataRow r in t.Rows)
                liste.Add(new Kalenderzeile
                {
                    Id = L(r, "ID") ?? 0,
                    IdGebaeude = L(r, "ID_Gebaeude"),
                    IdZone = L(r, "ID_Zone"),
                    IdGebaeudeStamm = L(r, "ID_Gebaeude_Stamm"),
                    IdVorlage = L(r, "ID_Vorlage"),
                    Groesse = S(r, "Groesse"),
                    Wert = D(r, "Wert"),
                    Aus = (L(r, "Aus") ?? 0) != 0,
                    Woche = S(r, "Woche"),
                    Nennwert = D(r, "Nennwert"),
                    Bemerkung = S(r, "Bemerkung"),
                });
            return liste;
        }

        /// <summary>
        /// Die Periodenzeilen aller Kalender eines Eigentümers — in <b>einer</b> Abfrage über den
        /// Verbund; der Leser sortiert sie je Kalender selbst.
        /// </summary>
        public static List<Periodenzeile> Periodenzeilen(long idGebaeude, long? idZone)
        {
            var liste = new List<Periodenzeile>();
            DataTable t = Lesen(
                "SELECT p.ID, p.ID_Kalender, p.Rang, p.Art, p.Bezeichner, p.Beginn, p.Ende, p.Feiertagsregel, " +
                "p.Wert, p.Aus, p.Woche, p.WieWochentag FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" p " +
                "JOIN \"" + KonditionierungSchema.TAB_KALENDER + "\" k ON k.ID = p.ID_Kalender " +
                "WHERE k.ID_Gebaeude = ? AND k.ID_Zone IS " +
                (idZone.HasValue ? "NOT NULL AND k.ID_Zone = ?" : "NULL") +
                " ORDER BY p.ID_Kalender, p.Rang DESC", idGebaeude, idZone);
            if (t == null) return liste;
            foreach (DataRow r in t.Rows)
                liste.Add(new Periodenzeile
                {
                    Id = L(r, "ID") ?? 0,
                    IdKalender = L(r, "ID_Kalender") ?? 0,
                    Rang = (int)(L(r, "Rang") ?? 0),
                    Art = S(r, "Art"),
                    Bezeichner = S(r, "Bezeichner"),
                    Beginn = I(r, "Beginn"),
                    Ende = I(r, "Ende"),
                    Feiertagsregel = S(r, "Feiertagsregel"),
                    Wert = D(r, "Wert"),
                    Aus = (L(r, "Aus") ?? 0) != 0,
                    Woche = S(r, "Woche"),
                    WieWochentag = I(r, "WieWochentag"),
                });
            return liste;
        }

        /// <summary>Die Vorgabezeilen eines Gebäudes (<paramref name="idZone"/> <c>null</c>) oder einer Zone.</summary>
        public static List<Vorgabezeile> Vorgabezeilen(long idGebaeude, long? idZone)
        {
            var liste = new List<Vorgabezeile>();
            DataTable t = Lesen(
                "SELECT ID, ID_Gebaeude, ID_Zone, ID_Gebaeude_Stamm, ID_Vorlage, Groesse, Zeile, Wert, Aus, " +
                "Von, Bis, Bedingt_K FROM \"" + KonditionierungSchema.TAB_VORGABE +
                "\" WHERE ID_Gebaeude = ? AND ID_Zone IS " + (idZone.HasValue ? "NOT NULL AND ID_Zone = ?" : "NULL") +
                " ORDER BY Groesse, Zeile, ID", idGebaeude, idZone);
            if (t == null) return liste;
            foreach (DataRow r in t.Rows)
                liste.Add(new Vorgabezeile
                {
                    Id = L(r, "ID") ?? 0,
                    IdGebaeude = L(r, "ID_Gebaeude"),
                    IdZone = L(r, "ID_Zone"),
                    IdGebaeudeStamm = L(r, "ID_Gebaeude_Stamm"),
                    IdVorlage = L(r, "ID_Vorlage"),
                    Groesse = S(r, "Groesse"),
                    Zeile = S(r, "Zeile"),
                    Wert = D(r, "Wert"),
                    Aus = (L(r, "Aus") ?? 0) != 0,
                    Von = I(r, "Von"),
                    Bis = I(r, "Bis"),
                    BedingtK = D(r, "Bedingt_K"),
                });
            return liste;
        }

        // -----------------------------------------------------------------
        //  Dieselben drei Abfragen je EIGENTÜMER (Katalogbau, Vorlage, Gebäude, Zone)
        // -----------------------------------------------------------------

        /// <summary>Die Kalenderzeilen eines Eigentümers — die Bedingung stellt der <c>Eigner</c>.</summary>
        internal static List<Kalenderzeile> KalenderzeilenVon(KonditionierungCtrl.Eigner eigner)
        {
            var liste = new List<Kalenderzeile>();
            DataTable t = DataRepository.GetDataTable(
                "SELECT ID, ID_Gebaeude, ID_Zone, ID_Gebaeude_Stamm, ID_Vorlage, Groesse, Wert, Aus, Woche, " +
                "Nennwert, Bemerkung FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE " +
                eigner.Bedingung() + " ORDER BY Groesse, ID", eigner.Parameter());
            if (t == null) return liste;
            foreach (DataRow r in t.Rows)
                liste.Add(new Kalenderzeile
                {
                    Id = L(r, "ID") ?? 0,
                    IdGebaeude = L(r, "ID_Gebaeude"),
                    IdZone = L(r, "ID_Zone"),
                    IdGebaeudeStamm = L(r, "ID_Gebaeude_Stamm"),
                    IdVorlage = L(r, "ID_Vorlage"),
                    Groesse = S(r, "Groesse"),
                    Wert = D(r, "Wert"),
                    Aus = (L(r, "Aus") ?? 0) != 0,
                    Woche = S(r, "Woche"),
                    Nennwert = D(r, "Nennwert"),
                    Bemerkung = S(r, "Bemerkung"),
                });
            return liste;
        }

        /// <summary>Die Vorgabezeilen eines Eigentümers.</summary>
        internal static List<Vorgabezeile> VorgabezeilenVon(KonditionierungCtrl.Eigner eigner)
        {
            var liste = new List<Vorgabezeile>();
            DataTable t = DataRepository.GetDataTable(
                "SELECT ID, ID_Gebaeude, ID_Zone, ID_Gebaeude_Stamm, ID_Vorlage, Groesse, Zeile, Wert, Aus, " +
                "Von, Bis, Bedingt_K FROM \"" + KonditionierungSchema.TAB_VORGABE + "\" WHERE " +
                eigner.Bedingung() + " ORDER BY Groesse, Zeile, ID", eigner.Parameter());
            if (t == null) return liste;
            foreach (DataRow r in t.Rows)
                liste.Add(new Vorgabezeile
                {
                    Id = L(r, "ID") ?? 0,
                    IdGebaeude = L(r, "ID_Gebaeude"),
                    IdZone = L(r, "ID_Zone"),
                    IdGebaeudeStamm = L(r, "ID_Gebaeude_Stamm"),
                    IdVorlage = L(r, "ID_Vorlage"),
                    Groesse = S(r, "Groesse"),
                    Zeile = S(r, "Zeile"),
                    Wert = D(r, "Wert"),
                    Aus = (L(r, "Aus") ?? 0) != 0,
                    Von = I(r, "Von"),
                    Bis = I(r, "Bis"),
                    BedingtK = D(r, "Bedingt_K"),
                });
            return liste;
        }

        /// <summary>Die Perioden der übergebenen Kalenderzeilen — eine Abfrage je Kalender.</summary>
        private static List<Periodenzeile> PeriodenzeilenVon(List<Kalenderzeile> kalender)
        {
            var liste = new List<Periodenzeile>();
            foreach (Kalenderzeile k in kalender)
            {
                DataTable t = DataRepository.GetDataTable(
                    "SELECT ID, ID_Kalender, Rang, Art, Bezeichner, Beginn, Ende, Feiertagsregel, Wert, Aus, " +
                    "Woche, WieWochentag FROM \"" + KonditionierungSchema.TAB_PERIODE +
                    "\" WHERE ID_Kalender = ? ORDER BY Rang DESC", new DbParam("@k", k.Id));
                if (t == null) continue;
                foreach (DataRow r in t.Rows)
                    liste.Add(new Periodenzeile
                    {
                        Id = L(r, "ID") ?? 0,
                        IdKalender = L(r, "ID_Kalender") ?? 0,
                        Rang = (int)(L(r, "Rang") ?? 0),
                        Art = S(r, "Art"),
                        Bezeichner = S(r, "Bezeichner"),
                        Beginn = I(r, "Beginn"),
                        Ende = I(r, "Ende"),
                        Feiertagsregel = S(r, "Feiertagsregel"),
                        Wert = D(r, "Wert"),
                        Aus = (L(r, "Aus") ?? 0) != 0,
                        Woche = S(r, "Woche"),
                        WieWochentag = I(r, "WieWochentag"),
                    });
            }
            return liste;
        }

        // -----------------------------------------------------------------
        //  Das Bezugsjahr einer Vorschau (Teppichbild, Welle K4)
        // -----------------------------------------------------------------

        /// <summary>
        /// <b>Das Bezugsjahr einer Kalendervorschau</b> (Entwurf KP2, Festlegung 8): im Projekt das
        /// des Laufs (<see cref="SolardatenCtrl.Referenzjahr"/> — das Jahr der Spotpreisreihe oder die
        /// Vorgabe), im Katalog dasselbe wie der Lauf ohne Projekt,
        /// <c>SolardatenCtrl.Referenzjahr(0)</c> = <see cref="DbWerte.SOLAR_REFERENZJAHR_STANDARD"/>.
        /// Keine Uhr: Ein laufendes Jahr würde die Vorschau am Jahreswechsel verschieben (B13).
        /// </summary>
        /// <param name="idProjekt">Das Projekt; 0 oder kleiner heißt Katalog.</param>
        public static int Bezugsjahr(int idProjekt)
            => SolardatenCtrl.Referenzjahr(idProjekt > 0 ? idProjekt : 0);

        // -----------------------------------------------------------------
        //  Die angelegten Kalender je Katalogbau (Spalte „Kalender", Welle K4)
        // -----------------------------------------------------------------

        /// <summary>
        /// <b>Die Zahl der angelegten Kalender je Katalogbau</b> (Entwurf KP2, Festlegung 15) —
        /// dieselbe Eigentümerspalte <c>ID_Gebaeude_Stamm</c>, über die
        /// <see cref="KalenderzeilenVon"/> die Kalender eines Katalogbaus liest, in <b>einer</b>
        /// gruppierten Abfrage für den ganzen Katalog statt einer je Satz. Gezählt werden die
        /// Größen (0 … 5). Ohne die Tabellen der Konditionierung ist das Verzeichnis leer.
        /// </summary>
        public static Dictionary<long, int> KalenderJeKatalogbau()
        {
            var zahl = new Dictionary<long, int>();
            if (!KonditionierungSchema.Lesbar()) return zahl;
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"ID_Gebaeude_Stamm\", COUNT(DISTINCT \"Groesse\") AS \"Anzahl\" FROM \"" +
                KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID_Gebaeude_Stamm\" IS NOT NULL " +
                "GROUP BY \"ID_Gebaeude_Stamm\"");
            if (t == null) return zahl;
            foreach (DataRow r in t.Rows)
            {
                long? id = L(r, "ID_Gebaeude_Stamm");
                if (id.HasValue) zahl[id.Value] = (int)(L(r, "Anzahl") ?? 0);
            }
            return zahl;
        }

        private static DataTable Lesen(string sql, long idGebaeude, long? idZone)
            => idZone.HasValue
                ? DataRepository.GetDataTable(sql, new DbParam("@g", idGebaeude), new DbParam("@z", idZone.Value))
                : DataRepository.GetDataTable(sql, new DbParam("@g", idGebaeude));

        private static GebaeudeModellException Fehler(ProjektGebaeudeModel g, string text)
            => Fehler(g?.Gebaeudename, text);

        private static GebaeudeModellException Fehler(string name, string text)
            => new GebaeudeModellException(GebaeudeModellFehler.KalenderUngueltig, (name ?? "?") + ": " + text);

        private static long? L(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != null && r[spalte] != DBNull.Value
                ? Convert.ToInt64(r[spalte], CultureInfo.InvariantCulture)
                : (long?)null;

        private static int? I(DataRow r, string spalte)
        {
            long? l = L(r, spalte);
            return l.HasValue ? (int)l.Value : (int?)null;
        }

        private static double? D(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != null && r[spalte] != DBNull.Value
                ? Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture)
                : (double?)null;

        private static string S(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != null && r[spalte] != DBNull.Value
                ? Convert.ToString(r[spalte], CultureInfo.InvariantCulture)
                : null;
    }
}

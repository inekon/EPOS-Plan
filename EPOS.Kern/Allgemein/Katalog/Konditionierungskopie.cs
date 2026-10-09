using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der EINE Kopierer der Konditionierung</b> (Konzept Konditionierungsprofile 5.5, Stufe
    /// KP1b): Vorgabezeilen, angelegte Kalender und ihre Perioden von einem Eigentümer zum
    /// anderen — Katalogbau → Projektgebäude (Übernahme, P3 (b)), Gebäude oder Katalogbau →
    /// Katalogbau („Speichern unter"), Katalogbau → Katalogbau (Duplizieren), Vorlage ↔ Ziel.
    ///
    /// <para><b>Warum ein eigener Kopierer.</b> <see cref="Katalogkopie"/> kopiert einen Kopfsatz
    /// mit EINER Ebene Kindzeilen über EINEN Fremdschlüssel und gibt keine Alt→Neu-Zuordnung
    /// heraus. Hier braucht es beides nicht: die Perioden hängen eine Ebene tiefer am Kalender,
    /// und die Eigentümerregel führt vier Spalten, von denen genau eine gesetzt ist.
    /// <see cref="Katalogkopie"/> bleibt deshalb unverändert.</para>
    ///
    /// <para><b>Auf SQL-Ebene, bitgenau.</b> Die Spaltenlisten kommen aus
    /// <c>pragma_table_info</c> ohne <c>ID</c> und die vier Eigentümerspalten — eine Spalte, die
    /// ein späterer Schemaschritt anlegt, reist von selbst mit. Werte gehen als <c>?</c> hinein,
    /// nie als Text.</para>
    ///
    /// <para><b>Alles in EINEM <see cref="DbVorgang"/></b>, den der Aufrufer hält: Die
    /// Alt→Neu-Zuordnung der Kalender entsteht über <c>last_insert_rowid()</c>, und das ist nur
    /// auf DERSELBEN Verbindung und in DERSELBEN Transaktion die Id, die eben entstanden ist
    /// (Befund KP1a). Scheitert ein Schritt, rollt der Aufrufer alles zurück — es bleibt kein
    /// halb kopierter Kalender stehen.</para>
    /// </summary>
    public static class Konditionierungskopie
    {
        /// <summary>
        /// <b>Was kopiert wird.</b> <paramref name="Groessen"/> <c>null</c> oder leer heißt „alle
        /// fünf"; <paramref name="Ersetzen"/> löscht vorher die Zeilen dieser Ebene am Ziel (die
        /// Zonen bleiben, sie sind eine andere Ebene); <paramref name="Vorlagenfilter"/> gilt für
        /// „Als Vorlage speichern" nach E54 und Konzept 5.7.
        /// </summary>
        /// <param name="Groessen">Die Größen, die mitreisen.</param>
        /// <param name="Ersetzen">Die Zeilen der Zielebene fallen vorher.</param>
        /// <param name="Vorlagenfilter">
        /// Keine Vorgabezeilen <c>NENNWERT</c> und <c>SAISON</c>, keine Perioden der Arten
        /// <c>FERIEN</c> und <c>BETRIEBSPAUSE</c> (darunter die Saisonperiode) und
        /// <c>Nennwert</c> am Kalender NULL — Nennwert und Saison gehören dem Ziel (E54).
        /// </param>
        public sealed record Auswahl(IReadOnlyList<Konditionierungsgroesse> Groessen,
                                     bool Ersetzen, bool Vorlagenfilter)
        {
            /// <summary>Alles, ohne Ersetzen und ohne Vorlagenfilter.</summary>
            public static readonly Auswahl Alles = new Auswahl(null, false, false);

            /// <summary>Alles, die Zeilen der Zielebene fallen vorher („erneut übernehmen").</summary>
            public static readonly Auswahl Ersetzend = new Auswahl(null, true, false);

            /// <summary>Eine Größe nach der Vorlagenregel (E54, 5.7) — ersetzend.</summary>
            public static Auswahl FuerVorlage(Konditionierungsgroesse groesse)
                => new Auswahl(new[] { groesse }, true, true);
        }

        /// <summary>
        /// <b>Was die Kopie ergeben hat.</b> <see cref="ZonenZurueck"/> zählt die Zeilen der
        /// ZONEN, die die Kopie nicht anfasst (Kalender und Vorgabezeilen zusammen) — die des
        /// Quellgebäudes bzw., wenn das Ziel das Gebäude ist, die seinen. Die Rückfrage, die sie
        /// nennt, kommt mit KP2.
        /// </summary>
        public sealed record Befund(bool Ok, int Vorgaben, int Kalender, int Perioden,
                                    int ZonenZurueck, string Meldung)
        {
            /// <summary>Nichts zu tun — kein Fehler (etwa ein Datenbankstand vor dem Schemaschritt).</summary>
            public static readonly Befund Nichts = new Befund(true, 0, 0, 0, 0, "");

            /// <summary>Der benannte Fehlschlag.</summary>
            public static Befund Fehler(string meldung) => new Befund(false, 0, 0, 0, 0, meldung ?? "");
        }

        /// <summary>Die Spalten, die eine Kopie NICHT vom Original übernimmt — sie gehören dem Eigentümer.</summary>
        private static readonly HashSet<string> EIGENTUEMERSPALTEN =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ID",
                KonditionierungSchema.SPALTE_ID_GEBAEUDE,
                KonditionierungSchema.SPALTE_ID_ZONE,
                KonditionierungSchema.SPALTE_ID_GEBAEUDE_STAMM,
                KonditionierungSchema.SPALTE_ID_VORLAGE,
                ZonenKatalogSchema.SPALTE_ID_ZONE_STAMM,
            };

        /// <summary>Die Spalten der Periodentabelle, die nicht mitreisen — sie hängt am Kalender.</summary>
        private static readonly HashSet<string> PERIODENSPALTEN =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ID", "ID_Kalender" };

        /// <summary>
        /// <b>Kopiert die Konditionierung von <paramref name="von"/> nach <paramref name="nach"/></b>
        /// im laufenden Vorgang <paramref name="v"/>. Der Aufrufer hält die Klammer: Er entscheidet
        /// über Commit und Rollback, und er führt die Rückfrage.
        /// </summary>
        /// <param name="v">Der laufende Vorgang — nie <c>null</c>.</param>
        /// <param name="von">Der Eigentümer, dessen Zeilen gelesen werden.</param>
        /// <param name="nach">Der Eigentümer, dem die Kopien gehören.</param>
        /// <param name="auswahl">Was mitreist; <c>null</c> heißt <see cref="Auswahl.Alles"/>.</param>
        public static Befund Kopieren(DbVorgang v, KonditionierungCtrl.Eigner von,
                                      KonditionierungCtrl.Eigner nach, Auswahl auswahl)
        {
            if (v == null) throw new ArgumentNullException(nameof(v));
            if (von == null) throw new ArgumentNullException(nameof(von));
            if (nach == null) throw new ArgumentNullException(nameof(nach));
            auswahl ??= Auswahl.Alles;

            if (!KonditionierungSchema.Lesbar()) return Befund.Nichts;
            if (von == nach)
                return Befund.Fehler(MyResource.Resource.KOND_MSG_KOPIE_GLEICHER_EIGNER);

            string groessenfilter = Groessenfilter(auswahl, out DbParam[] groessen);
            bool k2 = Kalendergemeinschaft.SchrittSteht();

            // DIE ZONENZEILEN BLEIBEN - eine andere Ebene (Konzept 5.5). Gezaehlt wird die Seite,
            // die ein Gebaeude ist: bei "Speichern unter" die QUELLE (ihre Zonen reisen nicht mit),
            // bei der erneuten Uebernahme das ZIEL (seine Zonen werden nicht ersetzt).
            int zonenZurueck = von.Art == Kalendereigentuemer.Gebaeude
                ? Zonenzeilen(v, von.IdGebaeude)
                : nach.Art == Kalendereigentuemer.Gebaeude
                    ? Zonenzeilen(v, nach.IdGebaeude)
                    : 0;

            if (auswahl.Ersetzen)
            {
                v.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_VORGABE + "\" WHERE " +
                             nach.Bedingung() + groessenfilter,
                             Mit(nach.Parameter(), groessen));
                v.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE " +
                             nach.Bedingung() + groessenfilter,
                             Mit(nach.Parameter(), groessen));
                // Die benannten Wochen des Ziels (Schemaschritt K2) - ihre Perioden sind mit den Kalendern gefallen.
                if (k2)
                    v.Ausfuehren("DELETE FROM \"" + KalenderbedienungSchema.TAB_WOCHE + "\" WHERE " +
                                 nach.Bedingung() + groessenfilter,
                                 Mit(nach.Parameter(), groessen));
            }

            int vorgaben = VorgabenKopieren(v, von, nach, auswahl, groessenfilter, groessen);
            int perioden = 0;
            Dictionary<long, long> wochen = k2 ? WochenKopieren(v, von, nach, groessenfilter, groessen)
                                               : new Dictionary<long, long>();
            int kalender = KalenderKopieren(v, von, nach, auswahl, groessenfilter, groessen, ref perioden, k2, wochen);

            return new Befund(true, vorgaben, kalender, perioden, zonenZurueck, "");
        }

        // =================================================================
        //  Die Vorgabezeilen
        // =================================================================

        private static int VorgabenKopieren(DbVorgang v, KonditionierungCtrl.Eigner von,
                                            KonditionierungCtrl.Eigner nach, Auswahl auswahl,
                                            string groessenfilter, DbParam[] groessen)
        {
            List<string> spalten = Spalten(v, KonditionierungSchema.TAB_VORGABE, EIGENTUEMERSPALTEN);
            if (spalten.Count == 0) return 0;

            // E54: Nennwert und Saison gehoeren dem Ziel und reisen nie in eine Vorlage.
            string zeilenfilter = auswahl.Vorlagenfilter ? " AND \"Zeile\" NOT IN (?, ?)" : "";
            var zeilenwerte = auswahl.Vorlagenfilter
                ? new[] { new DbParam("@z1", DbWerte.KOND_ZEILE_NENNWERT),
                          new DbParam("@z2", DbWerte.KOND_ZEILE_SAISON) }
                : Array.Empty<DbParam>();

            string liste = Liste(spalten);
            var parameter = new List<DbParam>();
            parameter.AddRange(nach.Spaltenwerte("@n"));
            parameter.AddRange(von.Parameter());
            parameter.AddRange(groessen);
            parameter.AddRange(zeilenwerte);

            return v.Ausfuehren(
                "INSERT INTO \"" + KonditionierungSchema.TAB_VORGABE + "\" (" + nach.Eigentuemerspalten + ", " + liste + ") " +
                "SELECT " + nach.Eigentuemerplatzhalter + ", " + liste + " FROM \"" + KonditionierungSchema.TAB_VORGABE +
                "\" WHERE " + von.Bedingung() + groessenfilter + zeilenfilter + " ORDER BY \"ID\"",
                parameter.ToArray());
        }

        // =================================================================
        //  Die Kalender und ihre Perioden
        // =================================================================

        private static int KalenderKopieren(DbVorgang v, KonditionierungCtrl.Eigner von,
                                            KonditionierungCtrl.Eigner nach, Auswahl auswahl,
                                            string groessenfilter, DbParam[] groessen,
                                            ref int perioden, bool k2, Dictionary<long, long> wochen)
        {
            List<string> spalten = Spalten(v, KonditionierungSchema.TAB_KALENDER, EIGENTUEMERSPALTEN);
            if (spalten.Count == 0) return 0;

            DataTable quellen = v.Lese(
                "SELECT \"ID\", \"Groesse\" FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE " +
                von.Bedingung() + groessenfilter + " ORDER BY \"ID\"",
                Mit(von.Parameter(), groessen));
            if (quellen == null || quellen.Rows.Count == 0) return 0;

            string liste = Liste(spalten);
            // Der Vorlagenfilter setzt Nennwert auf NULL (E54); die Spalte bleibt in der Liste,
            // nur ihr Ausdruck im SELECT wird NULL - so reist jede kuenftige Spalte weiter mit.
            string auswahlliste = auswahl.Vorlagenfilter ? Liste(spalten, "Nennwert") : liste;

            List<string> periodenspalten = Spalten(v, KonditionierungSchema.TAB_PERIODE, PERIODENSPALTEN);
            string periodenliste = Liste(periodenspalten);

            int kalender = 0;
            foreach (DataRow r in quellen.Rows)
            {
                long alt = Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture);

                var parameter = new List<DbParam>();
                parameter.AddRange(nach.Spaltenwerte("@n"));
                parameter.Add(new DbParam("@alt", alt));
                string groesse = Convert.ToString(r["Groesse"], CultureInfo.InvariantCulture);

                // Der gemeinsame Kalender des Ziels (etwa vom Ferienspiegel angelegt) weicht dem der Quelle.
                if (k2 && string.Equals(groesse, DbWerte.KOND_GROESSE_ALLE, StringComparison.Ordinal))
                    v.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE " + nach.Bedingung() +
                                 " AND \"Groesse\" = ?", Mit(nach.Parameter(), new[] { new DbParam("@a", DbWerte.KOND_GROESSE_ALLE) }));

                v.Ausfuehren(
                    "INSERT INTO \"" + KonditionierungSchema.TAB_KALENDER + "\" (" + nach.Eigentuemerspalten + ", " + liste + ") " +
                    "SELECT " + nach.Eigentuemerplatzhalter + ", " + auswahlliste + " FROM \"" +
                    KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID\" = ?",
                    parameter.ToArray());
                kalender++;

                // DIE ALT->NEU-ZUORDNUNG: last_insert_rowid() im SELBEN Vorgang, also auf
                // derselben Verbindung und in derselben Transaktion (Befund KP1a).
                long neu = Convert.ToInt64(v.Skalar("SELECT last_insert_rowid()"),
                                           CultureInfo.InvariantCulture);

                if (periodenspalten.Count == 0) continue;
                perioden += PeriodenKopieren(v, auswahl, periodenliste, alt, neu);
                if (!k2) continue;

                // Die Wochenverweise zeigen auf die Kopien der Wochen im Ziel.
                foreach (KeyValuePair<long, long> w in wochen)
                    v.Ausfuehren("UPDATE \"" + KonditionierungSchema.TAB_PERIODE + "\" SET \"" + KalenderbedienungSchema.SPALTE_ID_WOCHE +
                                 "\" = ? WHERE \"ID_Kalender\" = ? AND \"" + KalenderbedienungSchema.SPALTE_ID_WOCHE + "\" = ?",
                                 new DbParam("@n", w.Value), new DbParam("@k", neu), new DbParam("@a", w.Key));

                // Reist der gemeinsame Kalender nicht mit (Auswahl einzelner Groessen), kommen seine Perioden dieser
                // Groesse als eigene Perioden an - mit aufgeloester Woche, unter den eigenen Raengen.
                int bit = KalenderbedienungSchema.Maskenbit(groesse);
                if (groessenfilter.Length > 0 && bit != 0)
                    perioden += GemeinsameAusbreiten(v, von, auswahl, bit, neu);
            }
            return kalender;
        }

        /// <summary>Die Perioden mit Angabe des gemeinsamen Kalenders der Quelle, deren Maske die Größe trägt, als eigene.</summary>
        private static int GemeinsameAusbreiten(DbVorgang v, KonditionierungCtrl.Eigner von, Auswahl auswahl, int bit, long neu)
        {
            string filter = auswahl.Vorlagenfilter ? " AND p.\"Art\" NOT IN (?, ?) AND p.\"Rang\" <> ?" : "";
            var parameter = new List<DbParam> { new DbParam("@k", neu) };
            parameter.AddRange(von.Parameter());
            parameter.Add(new DbParam("@g", DbWerte.KOND_GROESSE_ALLE));
            parameter.Add(new DbParam("@b", bit));
            parameter.Add(new DbParam("@k2", neu));
            if (auswahl.Vorlagenfilter)
            {
                parameter.Add(new DbParam("@a1", DbWerte.KOND_ART_FERIEN));
                parameter.Add(new DbParam("@a2", DbWerte.KOND_ART_BETRIEBSPAUSE));
                parameter.Add(new DbParam("@rs", Standardfahrplan.RANG_SAISON));
            }
            return v.Ausfuehren(
                "INSERT INTO \"" + KonditionierungSchema.TAB_PERIODE + "\" (\"ID_Kalender\", \"Rang\", \"Art\", \"Bezeichner\", \"Beginn\", " +
                "\"Ende\", \"Feiertagsregel\", \"Wert\", \"Aus\", \"Woche\", \"WieWochentag\") SELECT ?, p.\"Rang\", p.\"Art\", p.\"Bezeichner\", " +
                "p.\"Beginn\", p.\"Ende\", p.\"Feiertagsregel\", p.\"Wert\", p.\"Aus\", COALESCE((SELECT w.\"Woche\" FROM \"" +
                KalenderbedienungSchema.TAB_WOCHE + "\" w WHERE w.\"ID\" = p.\"ID_Woche\"), p.\"Woche\"), p.\"WieWochentag\" FROM \"" +
                KonditionierungSchema.TAB_PERIODE + "\" p WHERE p.\"ID_Kalender\" IN (SELECT \"ID\" FROM \"" + KonditionierungSchema.TAB_KALENDER +
                "\" WHERE " + von.Bedingung() + " AND \"Groesse\" = ?) AND (p.\"Gilt_Fuer\" / ?) % 2 = 1 AND (p.\"Wert\" IS NOT NULL OR " +
                "p.\"Aus\" = 1 OR p.\"Woche\" IS NOT NULL OR p.\"ID_Woche\" IS NOT NULL OR p.\"WieWochentag\" IS NOT NULL) AND NOT EXISTS " +
                "(SELECT 1 FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" q WHERE q.\"ID_Kalender\" = ? AND q.\"Rang\" = p.\"Rang\")" +
                filter + " ORDER BY p.\"Rang\"",
                parameter.ToArray());
        }

        /// <summary>
        /// Die benannten Wochen der Quelle ins Ziel (Schemaschritt K2); eine gleichnamige Woche des Ziels bleibt und wird
        /// genommen. Liefert die Zuordnung alt → neu für die Verweise der kopierten Perioden.
        /// </summary>
        private static Dictionary<long, long> WochenKopieren(DbVorgang v, KonditionierungCtrl.Eigner von,
                                                             KonditionierungCtrl.Eigner nach, string groessenfilter,
                                                             DbParam[] groessen)
        {
            var zuordnung = new Dictionary<long, long>();
            DataTable quellen = v.Lese("SELECT \"ID\", \"Groesse\", \"Name\" FROM \"" + KalenderbedienungSchema.TAB_WOCHE + "\" WHERE " +
                                       von.Bedingung() + groessenfilter + " ORDER BY \"ID\"", Mit(von.Parameter(), groessen));
            if (quellen == null) return zuordnung;
            foreach (DataRow r in quellen.Rows)
            {
                long alt = Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture);
                var gleich = new List<DbParam>(nach.Parameter()) { new DbParam("@gr", r["Groesse"]), new DbParam("@na", r["Name"]) };
                object da = v.Skalar("SELECT \"ID\" FROM \"" + KalenderbedienungSchema.TAB_WOCHE + "\" WHERE " + nach.Bedingung() +
                                     " AND \"Groesse\" = ? AND \"Name\" = ?", gleich.ToArray());
                if (da == null || da == DBNull.Value)
                {
                    var parameter = new List<DbParam>(nach.Spaltenwerte("@n")) { new DbParam("@alt", alt) };
                    v.Ausfuehren("INSERT INTO \"" + KalenderbedienungSchema.TAB_WOCHE + "\" (" + nach.Eigentuemerspalten +
                                 ", \"Groesse\", \"Name\", \"Woche\") SELECT " + nach.Eigentuemerplatzhalter + ", \"Groesse\", \"Name\", " +
                                 "\"Woche\" FROM \"" + KalenderbedienungSchema.TAB_WOCHE + "\" WHERE \"ID\" = ?", parameter.ToArray());
                    da = v.Skalar("SELECT last_insert_rowid()");
                }
                zuordnung[alt] = Convert.ToInt64(da, CultureInfo.InvariantCulture);
            }
            return zuordnung;
        }

        private static int PeriodenKopieren(DbVorgang v, Auswahl auswahl, string periodenliste,
                                            long alt, long neu)
        {
            // Eine Vorlage traegt keine datierten Ferien und keine Saison (Konzept 3.5, 5.7, E54);
            // der Rang der Saison steht zusaetzlich, falls jemand sie von Hand als ZEITRAUM fuehrt.
            string filter = auswahl.Vorlagenfilter
                ? " AND \"Art\" NOT IN (?, ?) AND \"Rang\" <> ?"
                : "";
            var parameter = new List<DbParam> { new DbParam("@k", neu), new DbParam("@alt", alt) };
            if (auswahl.Vorlagenfilter)
            {
                parameter.Add(new DbParam("@a1", DbWerte.KOND_ART_FERIEN));
                parameter.Add(new DbParam("@a2", DbWerte.KOND_ART_BETRIEBSPAUSE));
                parameter.Add(new DbParam("@rs", Standardfahrplan.RANG_SAISON));
            }

            return v.Ausfuehren(
                "INSERT INTO \"" + KonditionierungSchema.TAB_PERIODE + "\" (\"ID_Kalender\", " +
                periodenliste + ") SELECT ?, " + periodenliste + " FROM \"" +
                KonditionierungSchema.TAB_PERIODE + "\" WHERE \"ID_Kalender\" = ?" + filter +
                " ORDER BY \"Rang\"",
                parameter.ToArray());
        }

        // =================================================================
        //  Kleine Helfer
        // =================================================================

        /// <summary>Die Zeilen der ZONEN eines Gebäudes — Kalender und Vorgabezeilen zusammen.</summary>
        internal static int Zonenzeilen(DbVorgang v, long idGebaeude)
        {
            object k = v.Skalar("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_KALENDER +
                                "\" WHERE \"" + KonditionierungSchema.SPALTE_ID_GEBAEUDE +
                                "\" = ? AND \"" + KonditionierungSchema.SPALTE_ID_ZONE + "\" IS NOT NULL",
                                new DbParam("@g", idGebaeude));
            object g = v.Skalar("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE +
                                "\" WHERE \"" + KonditionierungSchema.SPALTE_ID_GEBAEUDE +
                                "\" = ? AND \"" + KonditionierungSchema.SPALTE_ID_ZONE + "\" IS NOT NULL",
                                new DbParam("@g", idGebaeude));
            return Zahl(k) + Zahl(g);
        }

        /// <summary>Der <c>IN</c>-Ausdruck der Größen samt seinen Werten; leer heißt „alle fünf".</summary>
        private static string Groessenfilter(Auswahl auswahl, out DbParam[] werte)
        {
            if (auswahl.Groessen == null || auswahl.Groessen.Count == 0)
            {
                werte = Array.Empty<DbParam>();
                return "";
            }

            var platzhalter = new List<string>(auswahl.Groessen.Count);
            var liste = new List<DbParam>(auswahl.Groessen.Count);
            for (int i = 0; i < auswahl.Groessen.Count; i++)
            {
                platzhalter.Add("?");
                liste.Add(new DbParam("@gr" + i.ToString(CultureInfo.InvariantCulture),
                                      Konditionierungsgroessen.Kennwort(auswahl.Groessen[i])));
            }
            werte = liste.ToArray();
            return " AND \"Groesse\" IN (" + string.Join(", ", platzhalter) + ")";
        }

        /// <summary>Die Spalten einer Tabelle in ihrer Reihenfolge, ohne die genannten.</summary>
        private static List<string> Spalten(DbVorgang v, string tabelle, HashSet<string> ohne)
        {
            var liste = new List<string>();
            DataTable dt = v.Lese("SELECT name FROM pragma_table_info(?) ORDER BY cid",
                                  new DbParam("@t", tabelle));
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
            {
                string spalte = Convert.ToString(r["name"], CultureInfo.InvariantCulture) ?? "";
                if (spalte.Length == 0 || ohne.Contains(spalte)) continue;
                liste.Add(spalte);
            }
            return liste;
        }

        /// <summary>Die Spaltennamen in Anführungszeichen (Umlaute und Schlüsselwörter bleiben lesbar).</summary>
        private static string Liste(List<string> spalten) => Liste(spalten, null);

        /// <summary>Dieselbe Liste; <paramref name="alsNull"/> steht als <c>NULL</c> statt als Spalte.</summary>
        private static string Liste(List<string> spalten, string alsNull)
        {
            var teile = new List<string>(spalten.Count);
            foreach (string s in spalten)
                teile.Add(alsNull != null && string.Equals(s, alsNull, StringComparison.OrdinalIgnoreCase)
                    ? "NULL" : "\"" + s + "\"");
            return string.Join(", ", teile);
        }

        private static DbParam[] Mit(DbParam[] erste, params DbParam[][] weitere)
        {
            var alle = new List<DbParam>(erste);
            foreach (DbParam[] w in weitere) alle.AddRange(w);
            return alle.ToArray();
        }

        private static int Zahl(object wert)
            => wert == null || wert == DBNull.Value ? 0 : Convert.ToInt32(wert, CultureInfo.InvariantCulture);
    }
}

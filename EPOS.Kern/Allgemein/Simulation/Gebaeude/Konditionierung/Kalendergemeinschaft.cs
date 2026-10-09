using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der gemeinsame Kalender „alle Größen"</b> (Schemaschritt <see cref="KalenderbedienungSchema"/>) — Lesen und
    /// Zusammenführen. Eine Periode des gemeinsamen Kalenders (Größe <see cref="DbWerte.KOND_GROESSE_ALLE"/>) wirkt in
    /// jeder Größe ihrer Maske <c>Gilt_Fuer</c>, deren Kalender angelegt ist, als Kopie mit gleichem Rang und gleicher
    /// Angabe; eine eigene Periode des Größenkalenders am selben Rang geht vor. Perioden ohne Angabe (die Ferienliste)
    /// breitet der Leser nicht aus — ihre Angabe stellt die Ferienzeile der Matrix, gelesen über die Ferienspalten.
    /// </summary>
    public static class Kalendergemeinschaft
    {
        /// <summary>Der Eigentümer eines Kalenders als Fünfertupel der Eigentümerspalten (null-sicher verglichen).</summary>
        public sealed record Schluessel(long? IdGebaeude, long? IdZone, long? IdGebaeudeStamm, long? IdZoneStamm, long? IdVorlage)
        {
            /// <summary>Der Schlüssel eines <see cref="KonditionierungCtrl.Eigner"/>.</summary>
            public static Schluessel Von(KonditionierungCtrl.Eigner e)
            {
                if (e == null) throw new ArgumentNullException(nameof(e));
                switch (e.Art)
                {
                    case Kalendereigentuemer.Gebaeude: return new Schluessel(e.IdGebaeude, null, null, null, null);
                    case Kalendereigentuemer.Zone: return new Schluessel(e.IdGebaeude, e.IdZone, null, null, null);
                    case Kalendereigentuemer.Katalogbau: return new Schluessel(null, null, e.IdStamm, null, null);
                    case Kalendereigentuemer.Katalogzone: return new Schluessel(null, null, e.IdStamm, e.IdZone, null);
                    default: return new Schluessel(null, null, null, null, e.IdVorlage);
                }
            }

            internal DbParam[] Parameter() => new[]
            {
                new DbParam("@g", (object)IdGebaeude), new DbParam("@z", (object)IdZone), new DbParam("@s", (object)IdGebaeudeStamm),
                new DbParam("@zs", (object)IdZoneStamm), new DbParam("@v", (object)IdVorlage),
            };
        }

        /// <summary>Die null-sichere Bedingung über die fünf Eigentümerspalten (Parameter: <see cref="Schluessel.Parameter"/>).</summary>
        private const string BEDINGUNG =
            "\"ID_Gebaeude\" IS ? AND \"ID_Zone\" IS ? AND \"ID_Gebaeude_Stamm\" IS ? AND \"ID_Zone_Stamm\" IS ? AND \"ID_Vorlage\" IS ?";

        private const string ALLE = DbWerte.KOND_GROESSE_ALLE;

        // =================================================================
        //  Lesen
        // =================================================================

        /// <summary>
        /// Die Spaltenliste einer Periodenabfrage (<paramref name="alias"/> etwa <c>"p."</c> oder <c>""</c>): nach dem
        /// Schritt mit <c>Gilt_Fuer</c>, <c>ID_Woche</c> und der aufgelösten Woche (der Verweis geht der eingebetteten
        /// Woche vor), davor die Bestandsliste.
        /// </summary>
        public static string Periodenspalten(string alias, bool schrittSteht)
        {
            string a = alias ?? "";
            string basis = a + "ID, " + a + "ID_Kalender, " + a + "Rang, " + a + "Art, " + a + "Bezeichner, " + a + "Beginn, " +
                           a + "Ende, " + a + "Feiertagsregel, " + a + "Wert, " + a + "Aus, ";
            if (!schrittSteht) return basis + a + "Woche, " + a + "WieWochentag";
            return basis + "COALESCE((SELECT w.Woche FROM \"" + KalenderbedienungSchema.TAB_WOCHE + "\" w WHERE w.ID = " + a +
                   "ID_Woche), " + a + "Woche) AS Woche, " + a + "WieWochentag, " + a + "Gilt_Fuer, " + a + "ID_Woche";
        }

        /// <summary>Steht der Schritt so weit, dass die Leser die neuen Spalten abfragen dürfen?</summary>
        public static bool SchrittSteht()
            => DataRepository.SpalteVorhanden(KonditionierungSchema.TAB_PERIODE, KalenderbedienungSchema.SPALTE_GILT_FUER) &&
               DataRepository.TabelleVorhanden(KalenderbedienungSchema.TAB_WOCHE);

        /// <summary>Ist die Zeile der gemeinsame Kalender?</summary>
        public static bool IstGemeinsam(Kalenderzeile z) => z != null && string.Equals(z.Groesse, ALLE, StringComparison.Ordinal);

        /// <summary>Die Kalenderzeilen ohne den gemeinsamen Kalender (dieselbe Liste, wenn keiner dabei ist).</summary>
        public static List<Kalenderzeile> OhneGemeinsam(List<Kalenderzeile> zeilen)
            => zeilen == null || !zeilen.Any(IstGemeinsam) ? zeilen : zeilen.Where(z => !IstGemeinsam(z)).ToList();

        /// <summary>
        /// <b>Breitet die Perioden des gemeinsamen Kalenders aus</b>: Sie verlassen <paramref name="perioden"/> und kehren
        /// je Größe ihrer Maske, deren Kalender in <paramref name="kalender"/> steht, als Kopie mit dessen Id zurück —
        /// außer dort, wo der Größenkalender am selben Rang eine eigene Periode trägt. Ohne gemeinsamen Kalender bleibt
        /// die Liste unberührt (dieselbe Reihenfolge, byte-gleich).
        /// </summary>
        public static void Ausbreiten(IReadOnlyList<Kalenderzeile> kalender, List<Periodenzeile> perioden)
        {
            if (kalender == null || perioden == null) return;
            var gemeinsam = new HashSet<long>(kalender.Where(IstGemeinsam).Select(k => k.Id));
            if (gemeinsam.Count == 0) return;

            List<Periodenzeile> quelle = perioden.Where(p => gemeinsam.Contains(p.IdKalender)).ToList();
            perioden.RemoveAll(p => gemeinsam.Contains(p.IdKalender));
            bool zugefuegt = false;
            foreach (Kalenderzeile k in kalender.Where(k => !IstGemeinsam(k)))
            {
                int bit = KalenderbedienungSchema.Maskenbit(k.Groesse);
                if (bit == 0) continue;
                var belegt = new HashSet<int>(perioden.Where(p => p.IdKalender == k.Id).Select(p => p.Rang));
                foreach (Periodenzeile g in quelle)
                {
                    if (((g.GiltFuer ?? 0) & bit) == 0 || !HatAngabe(g) || belegt.Contains(g.Rang)) continue;
                    perioden.Add(new Periodenzeile
                    {
                        Id = g.Id, IdKalender = k.Id, Rang = g.Rang, Art = g.Art, Bezeichner = g.Bezeichner, Beginn = g.Beginn,
                        Ende = g.Ende, Feiertagsregel = g.Feiertagsregel, Wert = g.Wert, Aus = g.Aus, Woche = g.Woche,
                        WieWochentag = g.WieWochentag, IdWoche = g.IdWoche,
                    });
                    zugefuegt = true;
                }
            }
            if (zugefuegt)
            {
                List<Periodenzeile> sortiert = perioden.OrderBy(p => p.IdKalender).ThenByDescending(p => p.Rang).ToList();
                perioden.Clear();
                perioden.AddRange(sortiert);
            }
        }

        /// <summary>Trägt die Periode eine Angabe (Wert, „aus", Woche oder Wochentag)?</summary>
        public static bool HatAngabe(Periodenzeile p)
            => p.Wert.HasValue || p.Aus || p.Woche != null || p.IdWoche.HasValue || p.WieWochentag.HasValue;

        // =================================================================
        //  Zusammenführen und Auflösen
        // =================================================================

        /// <summary>Führt die gekoppelten Kopien ALLER Eigentümer zusammen (Migration); deterministisch nach Eigentümer.</summary>
        /// <returns>Die Zahl der entstandenen Gemeinschaftsperioden; <paramref name="kopien"/> die Zahl der ersetzten Kopien.</returns>
        public static int AlleZusammenfuehren(DbVorgang v, out int kopien)
        {
            kopien = 0;
            int gruppen = 0;
            DataTable eigner = v.Lese(
                "SELECT DISTINCT \"ID_Gebaeude\", \"ID_Zone\", \"ID_Gebaeude_Stamm\", \"ID_Zone_Stamm\", \"ID_Vorlage\" FROM \"" +
                KonditionierungSchema.TAB_KALENDER + "\" WHERE \"Groesse\" <> ? ORDER BY 1, 2, 3, 4, 5", new DbParam("@a", ALLE));
            foreach (DataRow r in eigner.Rows)
            {
                gruppen += Zusammenfuehren(v, new Schluessel(L(r, "ID_Gebaeude"), L(r, "ID_Zone"), L(r, "ID_Gebaeude_Stamm"),
                                                             L(r, "ID_Zone_Stamm"), L(r, "ID_Vorlage")), out int k);
                kopien += k;
            }
            return gruppen;
        }

        /// <summary>
        /// <b>Führt die gekoppelten Kopien EINES Eigentümers zusammen</b>: Perioden außer <c>FERIEN</c>, die in mindestens
        /// zwei Größenkalendern mit gleichem Rang, gleicher Art, gleichem Bezeichner, gleichen Tagen bzw. gleicher Regel und
        /// gleicher Angabe stehen, werden EINE Periode des gemeinsamen Kalenders mit der Maske ihrer Größen. Ist der Rang
        /// dort belegt, bleiben die Kopien stehen. Ein neu angelegter gemeinsamer Kalender ohne Periode fällt wieder.
        /// </summary>
        public static int Zusammenfuehren(DbVorgang v, Schluessel eigner, out int kopien)
        {
            kopien = 0;
            DataTable kal = v.Lese("SELECT \"ID\", \"Groesse\" FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE " + BEDINGUNG +
                                   " ORDER BY \"ID\"", eigner.Parameter());
            var groesse = new Dictionary<long, string>();
            long? gemeinsam = null;
            foreach (DataRow r in kal.Rows)
            {
                long id = L(r, "ID") ?? 0;
                string g = Convert.ToString(r["Groesse"], CultureInfo.InvariantCulture);
                if (string.Equals(g, ALLE, StringComparison.Ordinal)) gemeinsam = id; else groesse[id] = g;
            }
            if (groesse.Count < 2) return 0;

            var gruppen = new SortedDictionary<string, List<DataRow>>(StringComparer.Ordinal);
            foreach (long idKalender in groesse.Keys)
            {
                DataTable per = v.Lese("SELECT \"ID\", \"Rang\", \"Art\", \"Bezeichner\", \"Beginn\", \"Ende\", \"Feiertagsregel\", \"Wert\", " +
                                       "\"Aus\", \"Woche\", \"WieWochentag\", \"ID_Woche\", \"ID_Kalender\" FROM \"" +
                                       KonditionierungSchema.TAB_PERIODE + "\" WHERE \"ID_Kalender\" = ? AND \"Art\" <> ? ORDER BY \"Rang\"",
                                       new DbParam("@k", idKalender), new DbParam("@f", DbWerte.KOND_ART_FERIEN));
                foreach (DataRow p in per.Rows)
                {
                    string schluessel = string.Join("|", new[]
                    {
                        (L(p, "Rang") ?? 0).ToString("D4", CultureInfo.InvariantCulture), T(p, "Art"), T(p, "Bezeichner"), T(p, "Beginn"),
                        T(p, "Ende"), T(p, "Feiertagsregel"), T(p, "Wert"), T(p, "Aus"), T(p, "Woche"), T(p, "WieWochentag"), T(p, "ID_Woche"),
                    });
                    if (!gruppen.TryGetValue(schluessel, out List<DataRow> liste)) gruppen[schluessel] = liste = new List<DataRow>();
                    liste.Add(p);
                }
            }

            bool neu = false;
            int zusammen = 0;
            foreach (List<DataRow> g in gruppen.Values)
            {
                if (g.Select(p => L(p, "ID_Kalender")).Distinct().Count() < 2) continue;
                long rang = L(g[0], "Rang") ?? 0;
                if (!gemeinsam.HasValue)
                {
                    v.Ausfuehren("INSERT INTO \"" + KonditionierungSchema.TAB_KALENDER + "\" (\"ID_Gebaeude\", \"ID_Zone\", \"ID_Gebaeude_Stamm\", " +
                                 "\"ID_Zone_Stamm\", \"ID_Vorlage\", \"Groesse\", \"Aus\") VALUES (?, ?, ?, ?, ?, ?, 0)",
                                 eigner.Parameter().Concat(new[] { new DbParam("@a", ALLE) }).ToArray());
                    gemeinsam = Convert.ToInt64(v.Skalar("SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
                    neu = true;
                }
                if (Convert.ToInt64(v.Skalar("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" WHERE \"ID_Kalender\" = ? AND \"Rang\" = ?",
                                             new DbParam("@k", gemeinsam.Value), new DbParam("@r", rang)), CultureInfo.InvariantCulture) > 0)
                    continue;
                int maske = 0;
                foreach (DataRow p in g) maske |= KalenderbedienungSchema.Maskenbit(groesse[L(p, "ID_Kalender") ?? 0]);
                DataRow m = g[0];
                v.Ausfuehren("INSERT INTO \"" + KonditionierungSchema.TAB_PERIODE + "\" (\"ID_Kalender\", \"Rang\", \"Art\", \"Bezeichner\", " +
                             "\"Beginn\", \"Ende\", \"Feiertagsregel\", \"Wert\", \"Aus\", \"Woche\", \"WieWochentag\", \"ID_Woche\", \"" +
                             KalenderbedienungSchema.SPALTE_GILT_FUER + "\") VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                             new DbParam("@k", gemeinsam.Value), new DbParam("@r", rang), new DbParam("@ar", m["Art"]),
                             new DbParam("@bz", m["Bezeichner"]), new DbParam("@b", m["Beginn"]), new DbParam("@e", m["Ende"]),
                             new DbParam("@ft", m["Feiertagsregel"]), new DbParam("@w", m["Wert"]), new DbParam("@au", m["Aus"]),
                             new DbParam("@wo", m["Woche"]), new DbParam("@ww", m["WieWochentag"]), new DbParam("@iw", m["ID_Woche"]),
                             new DbParam("@gf", maske));
                foreach (DataRow p in g)
                    v.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" WHERE \"ID\" = ?", new DbParam("@id", L(p, "ID") ?? 0));
                zusammen++;
                kopien += g.Count;
            }
            if (neu && zusammen == 0)
                v.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID\" = ?", new DbParam("@k", gemeinsam.Value));
            return zusammen;
        }

        /// <summary>Trägt der gemeinsame Kalender des Eigentümers Perioden mit Angabe (also ausgebreitete)?</summary>
        public static bool HatAusgebreitete(DbVorgang v, Schluessel eigner)
            => Convert.ToInt64(v.Skalar(
                "SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" WHERE \"ID_Kalender\" IN (SELECT \"ID\" FROM \"" +
                KonditionierungSchema.TAB_KALENDER + "\" WHERE " + BEDINGUNG + " AND \"Groesse\" = ?) AND (\"Wert\" IS NOT NULL OR " +
                "\"Aus\" = 1 OR \"Woche\" IS NOT NULL OR \"ID_Woche\" IS NOT NULL OR \"WieWochentag\" IS NOT NULL)",
                eigner.Parameter().Concat(new[] { new DbParam("@a", ALLE) }).ToArray()), CultureInfo.InvariantCulture) > 0;

        /// <summary>
        /// Löst die Perioden mit Angabe des gemeinsamen Kalenders auf — der Schreibweg hat sie als Kopien in jeden
        /// Größenkalender geschrieben; danach führt <see cref="Zusammenfuehren"/> sie wieder zusammen. Die Ferienliste
        /// (Perioden ohne Angabe) bleibt.
        /// </summary>
        public static int Aufloesen(DbVorgang v, Schluessel eigner)
            => v.Ausfuehren(
                "DELETE FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" WHERE \"ID_Kalender\" IN (SELECT \"ID\" FROM \"" +
                KonditionierungSchema.TAB_KALENDER + "\" WHERE " + BEDINGUNG + " AND \"Groesse\" = ?) AND (\"Wert\" IS NOT NULL OR " +
                "\"Aus\" = 1 OR \"Woche\" IS NOT NULL OR \"ID_Woche\" IS NOT NULL OR \"WieWochentag\" IS NOT NULL)",
                eigner.Parameter().Concat(new[] { new DbParam("@a", ALLE) }).ToArray());

        private static long? L(DataRow r, string spalte)
            => r[spalte] == DBNull.Value ? (long?)null : Convert.ToInt64(r[spalte], CultureInfo.InvariantCulture);

        private static string T(DataRow r, string spalte)
            => r[spalte] == DBNull.Value ? "∅" : Convert.ToString(r[spalte], CultureInfo.InvariantCulture);
    }
}

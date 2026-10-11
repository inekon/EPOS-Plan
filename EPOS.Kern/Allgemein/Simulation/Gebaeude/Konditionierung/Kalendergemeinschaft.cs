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

        // =================================================================
        //  Der Arbeitsstand: gemeinsamer Kalender, Ferienliste, benannte Wochen
        // =================================================================

        /// <summary>Der erste Rang der Ferienliste jenseits des Spiegels der vier Ferienspalten (204).</summary>
        public const int RANG_FERIENLISTE = KalenderbedienungSchema.RANG_FERIEN_ERSTER + KalenderbedienungSchema.FERIENSPALTEN;

        /// <summary>Der letzte Rang der Ferienliste (unter dem Eigenband 310).</summary>
        public const int RANG_FERIENLISTE_LETZTER = Standardfahrplan.RANG_EIGEN - 1;

        /// <summary>
        /// <b>Liest den gemeinsamen Kalender eines Eigentümers</b>: die Perioden mit Angabe als
        /// <see cref="Gemeinschaftsperiode"/> (Angabe gelesen mit dem strengen Leser in der ersten Größe der Maske; eine
        /// ungültige Periode fehlt) und die Ferienliste ab Rang <see cref="RANG_FERIENLISTE"/>. Ohne Schritt leer.
        /// </summary>
        public static void GemeinsamLesen(Schluessel eigner, out List<Gemeinschaftsperiode> gemeinsam, out List<Ferienzeile> ferien)
        {
            gemeinsam = new List<Gemeinschaftsperiode>();
            ferien = new List<Ferienzeile>();
            if (eigner == null || !SchrittSteht()) return;
            DataTable t = DataRepository.GetDataTable(
                "SELECT " + Periodenspalten("p.", true) + " FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" p JOIN \"" +
                KonditionierungSchema.TAB_KALENDER + "\" k ON k.\"ID\" = p.\"ID_Kalender\" WHERE k.\"Groesse\" = ? AND k.\"ID_Gebaeude\" IS ? AND " +
                "k.\"ID_Zone\" IS ? AND k.\"ID_Gebaeude_Stamm\" IS ? AND k.\"ID_Zone_Stamm\" IS ? AND k.\"ID_Vorlage\" IS ? ORDER BY p.\"Rang\" DESC",
                new[] { new DbParam("@a", ALLE) }.Concat(eigner.Parameter()).ToArray());
            if (t == null) return;
            foreach (DataRow r in t.Rows)
            {
                var p = new Periodenzeile
                {
                    Id = L(r, "ID") ?? 0, IdKalender = L(r, "ID_Kalender") ?? 0, Rang = (int)(L(r, "Rang") ?? 0),
                    Art = Tx(r, "Art"), Bezeichner = Tx(r, "Bezeichner"), Beginn = I(r, "Beginn"), Ende = I(r, "Ende"),
                    Feiertagsregel = Tx(r, "Feiertagsregel"), Wert = r["Wert"] == DBNull.Value ? null : Convert.ToDouble(r["Wert"], CultureInfo.InvariantCulture),
                    Aus = (L(r, "Aus") ?? 0) != 0, Woche = Tx(r, "Woche"), WieWochentag = I(r, "WieWochentag"),
                    GiltFuer = I(r, "Gilt_Fuer"), IdWoche = L(r, "ID_Woche"),
                };
                if (!HatAngabe(p))
                {
                    if (p.Rang >= RANG_FERIENLISTE && string.Equals(p.Art, DbWerte.KOND_ART_FERIEN, StringComparison.Ordinal)
                        && p.Beginn.HasValue && p.Ende.HasValue)
                        ferien.Add(new Ferienzeile(p.Bezeichner, p.Beginn.Value, p.Ende.Value));
                    continue;
                }
                int maske = p.GiltFuer ?? 0;
                Konditionierungsgroesse g = Gemeinschaftsperiode.Groessen(maske).FirstOrDefault();
                if (maske == 0) continue;
                var zeile = new Kalenderzeile { Id = p.IdKalender, Groesse = Konditionierungsgroessen.Kennwort(g), Aus = true };
                Kalenderlesung l = Kalenderleser.Lesen(zeile, new[] { p });
                if (l.Befund != Kalenderbefund.Gelesen || l.Kalender.Perioden.Count != 1) continue;
                gemeinsam.Add(new Gemeinschaftsperiode(l.Kalender.Perioden[0], maske));
            }
            ferien.Reverse();
        }

        /// <summary>Die benannten Wochen eines Eigentümers (nach Größe und Name); ohne Schritt leer.</summary>
        public static List<BenannteWoche> WochenLesen(Schluessel eigner)
        {
            var liste = new List<BenannteWoche>();
            if (eigner == null || !SchrittSteht()) return liste;
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"ID\", \"Groesse\", \"Name\", \"Woche\" FROM \"" + KalenderbedienungSchema.TAB_WOCHE + "\" WHERE " + BEDINGUNG +
                " ORDER BY \"Groesse\", \"Name\", \"ID\"", eigner.Parameter());
            if (t == null) return liste;
            foreach (DataRow r in t.Rows)
            {
                if (!Konditionierungsgroessen.AusKennwort(Tx(r, "Groesse"), out Konditionierungsgroesse g)) continue;
                Wochenlesung wl = Kalenderwoche.Lesen(Tx(r, "Woche"), g);
                if (wl.Befund != Wochenbefund.Gelesen) continue;
                liste.Add(new BenannteWoche(L(r, "ID") ?? 0, g, Tx(r, "Name"), wl.Werte.ToArray()));
            }
            return liste;
        }

        /// <summary>
        /// <b>Schreibt die benannten Wochen</b>: neue (Id ≤ 0) bekommen eine Zeile — <paramref name="ids"/> ordnet ihre
        /// vorläufige Id der neuen zu —, geänderte werden ersetzt. Entfernte fallen erst mit <see cref="WochenEntfernen"/>,
        /// nachdem die Perioden geschrieben sind.
        /// </summary>
        public static bool WochenSchreiben(DbVorgang v, Schluessel eigner, IReadOnlyList<BenannteWoche> alt,
                                           IReadOnlyList<BenannteWoche> neu, Dictionary<long, long> ids)
        {
            bool geschrieben = false;
            foreach (BenannteWoche w in neu ?? Array.Empty<BenannteWoche>())
            {
                string text = Kalenderwoche.Schreiben(w.Werte, w.Groesse);
                if (w.IstNeu)
                {
                    v.Ausfuehren("INSERT INTO \"" + KalenderbedienungSchema.TAB_WOCHE + "\" (\"ID_Gebaeude\", \"ID_Zone\", \"ID_Gebaeude_Stamm\", " +
                                 "\"ID_Zone_Stamm\", \"ID_Vorlage\", \"Groesse\", \"Name\", \"Woche\") VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                                 eigner.Parameter().Concat(new[] { new DbParam("@g", Konditionierungsgroessen.Kennwort(w.Groesse)),
                                     new DbParam("@n", w.Name), new DbParam("@w", text) }).ToArray());
                    ids[w.Id] = Convert.ToInt64(v.Skalar("SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
                    geschrieben = true;
                }
                else if (!(alt ?? Array.Empty<BenannteWoche>()).Any(a => a.Gleich(w)))
                {
                    v.Ausfuehren("UPDATE \"" + KalenderbedienungSchema.TAB_WOCHE + "\" SET \"Name\" = ?, \"Woche\" = ? WHERE \"ID\" = ?",
                                 new DbParam("@n", w.Name), new DbParam("@w", text), new DbParam("@id", w.Id));
                    geschrieben = true;
                }
            }
            return geschrieben;
        }

        /// <summary>Löscht die Wochen, die <paramref name="neu"/> nicht mehr führt (nach dem Schreiben der Perioden).</summary>
        public static bool WochenEntfernen(DbVorgang v, IReadOnlyList<BenannteWoche> alt, IReadOnlyList<BenannteWoche> neu)
        {
            bool geschrieben = false;
            foreach (BenannteWoche w in alt ?? Array.Empty<BenannteWoche>())
                if (!w.IstNeu && !(neu ?? Array.Empty<BenannteWoche>()).Any(n => n.Id == w.Id))
                {
                    v.Ausfuehren("DELETE FROM \"" + KalenderbedienungSchema.TAB_WOCHE + "\" WHERE \"ID\" = ?", new DbParam("@id", w.Id));
                    geschrieben = true;
                }
            return geschrieben;
        }

        /// <summary>Die endgültige Id eines Wochenverweises (eine vorläufige über <paramref name="ids"/>).</summary>
        public static long? WochenId(long? id, IReadOnlyDictionary<long, long> ids)
            => id.HasValue && id.Value <= 0 && ids != null && ids.TryGetValue(id.Value, out long neu) ? neu : id;

        /// <summary>
        /// <b>Schreibt den gemeinsamen Kalender eines Eigentümers</b>: Seine Perioden mit Angabe und die Ferienliste ab Rang
        /// <see cref="RANG_FERIENLISTE"/> werden ersetzt; der Spiegel der vier Ferienspalten (Rang 200 … 203) bleibt dem
        /// Trigger. Fehlt der Kalender und gibt es etwas zu schreiben, entsteht er.
        /// </summary>
        public static void GemeinsamSchreiben(DbVorgang v, Schluessel eigner, IReadOnlyList<Gemeinschaftsperiode> gemeinsam,
                                              IReadOnlyList<Ferienzeile> ferien, IReadOnlyDictionary<long, long> ids)
        {
            gemeinsam ??= Array.Empty<Gemeinschaftsperiode>();
            ferien ??= Array.Empty<Ferienzeile>();
            object o = v.Skalar("SELECT \"ID\" FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE " + BEDINGUNG + " AND \"Groesse\" = ?",
                                eigner.Parameter().Concat(new[] { new DbParam("@a", ALLE) }).ToArray());
            long? id = o == null || o == DBNull.Value ? null : Convert.ToInt64(o, CultureInfo.InvariantCulture);
            if (id.HasValue)
                v.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" WHERE \"ID_Kalender\" = ? AND ((\"Wert\" IS NOT NULL OR " +
                             "\"Aus\" = 1 OR \"Woche\" IS NOT NULL OR \"ID_Woche\" IS NOT NULL OR \"WieWochentag\" IS NOT NULL) OR \"Rang\" >= ?)",
                             new DbParam("@k", id.Value), new DbParam("@r", RANG_FERIENLISTE));
            if (gemeinsam.Count == 0 && ferien.Count == 0) return;
            if (!id.HasValue)
            {
                v.Ausfuehren("INSERT INTO \"" + KonditionierungSchema.TAB_KALENDER + "\" (\"ID_Gebaeude\", \"ID_Zone\", \"ID_Gebaeude_Stamm\", " +
                             "\"ID_Zone_Stamm\", \"ID_Vorlage\", \"Groesse\", \"Aus\") VALUES (?, ?, ?, ?, ?, ?, 0)",
                             eigner.Parameter().Concat(new[] { new DbParam("@a", ALLE) }).ToArray());
                id = Convert.ToInt64(v.Skalar("SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
            }
            const string EINFUEGEN = "INSERT INTO \"" + KonditionierungSchema.TAB_PERIODE + "\" (\"ID_Kalender\", \"Rang\", \"Art\", \"Bezeichner\", " +
                                     "\"Beginn\", \"Ende\", \"Feiertagsregel\", \"Wert\", \"Aus\", \"Woche\", \"WieWochentag\", \"ID_Woche\", \"" +
                                     KalenderbedienungSchema.SPALTE_GILT_FUER + "\") VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
            foreach (Gemeinschaftsperiode p in gemeinsam)
            {
                Kalenderregel r = p.Regel;
                Kalenderangabe a = r.Angabe;
                long? woche = a.Art == Angabeart.Woche ? WochenId(a.IdWoche, ids) : null;
                string text = a.Art == Angabeart.Woche && !woche.HasValue
                    ? Kalenderwoche.Schreiben(a.Woche, Gemeinschaftsperiode.Groessen(p.Maske).First()) : null;
                v.Ausfuehren(EINFUEGEN, new DbParam("@k", id.Value), new DbParam("@r", r.Rang), new DbParam("@ar", r.Art),
                             new DbParam("@bz", r.Bezeichner), new DbParam("@b", r.IstFeiertag ? null : (object)r.Beginn),
                             new DbParam("@e", r.IstFeiertag ? null : (object)r.Ende), new DbParam("@ft", (object)r.Feiertagsregel),
                             new DbParam("@w", a.Art == Angabeart.Wert ? (object)a.Wert : null), new DbParam("@au", a.Art == Angabeart.Aus ? 1 : 0),
                             new DbParam("@wo", (object)text), new DbParam("@ww", a.Art == Angabeart.WieWochentag ? (object)a.WieWochentag : null),
                             new DbParam("@iw", (object)woche), new DbParam("@gf", p.Maske));
            }
            for (int i = 0; i < ferien.Count; i++)
                v.Ausfuehren(EINFUEGEN, new DbParam("@k", id.Value), new DbParam("@r", RANG_FERIENLISTE + i),
                             new DbParam("@ar", DbWerte.KOND_ART_FERIEN), new DbParam("@bz", ferien[i].Name),
                             new DbParam("@b", ferien[i].Beginn), new DbParam("@e", ferien[i].Ende), new DbParam("@ft", null),
                             new DbParam("@w", null), new DbParam("@au", (object)0), new DbParam("@wo", null), new DbParam("@ww", null),
                             new DbParam("@iw", null), new DbParam("@gf", KalenderbedienungSchema.MASKE_ALLE));
        }

        // =================================================================
        //  Die Ferienperioden als Quelle der Leser (Stufe 2, Teil B)
        // =================================================================

        /// <summary>Ist die Zeile eine Ferienperiode des gemeinsamen Kalenders (Art FERIEN, ohne Angabe, Rang 200 … 309)?</summary>
        private static bool IstFerienperiode(Periodenzeile p)
            => p != null && !HatAngabe(p) && string.Equals(p.Art, DbWerte.KOND_ART_FERIEN, StringComparison.Ordinal)
               && p.Rang >= KalenderbedienungSchema.RANG_FERIEN_ERSTER && p.Rang <= RANG_FERIENLISTE_LETZTER
               && p.Beginn.HasValue && p.Ende.HasValue;

        /// <summary>
        /// <b>Die Ferienliste ab dem fünften Zeitraum aus gelesenen Zeilen</b> — die Ferienperioden der gemeinsamen
        /// Kalender in <paramref name="kalender"/> ab Rang <see cref="RANG_FERIENLISTE"/>, aufsteigend nach Rang. Der Lauf
        /// ruft sie VOR <see cref="Ausbreiten"/> (das die Zeilen ohne Angabe verwirft) und gibt sie dem Generator
        /// (<see cref="Matrixeingang.WeitereFerien"/>). Ohne gemeinsamen Kalender leer.
        /// </summary>
        public static List<Ferienzeile> Ferienliste(IReadOnlyList<Kalenderzeile> kalender, IReadOnlyList<Periodenzeile> perioden)
        {
            var liste = new List<Ferienzeile>();
            if (kalender == null || perioden == null) return liste;
            var gemeinsam = new HashSet<long>(kalender.Where(IstGemeinsam).Select(k => k.Id));
            if (gemeinsam.Count == 0) return liste;
            foreach (Periodenzeile p in perioden.Where(p => gemeinsam.Contains(p.IdKalender) && IstFerienperiode(p) && p.Rang >= RANG_FERIENLISTE)
                                                .OrderBy(p => p.Rang))
                liste.Add(new Ferienzeile(p.Bezeichner ?? "", p.Beginn.Value, p.Ende.Value));
            return liste;
        }

        /// <summary>
        /// <b>Alle Ferienperioden des gemeinsamen Kalenders eines Eigentümers</b> (Rang 200 … 309, aufsteigend) — der
        /// Spiegel der vier Ferienspalten und die Ferienliste dahinter. Der Zapfkalender liest sie (Konzept 7.8). Ohne
        /// Schritt oder ohne gemeinsamen Kalender leer.
        /// </summary>
        public static List<Ferienzeile> Ferienperioden(Schluessel eigner)
        {
            var liste = new List<Ferienzeile>();
            if (eigner == null || !SchrittSteht()) return liste;
            DataTable t = DataRepository.GetDataTable(
                "SELECT p.\"Rang\", p.\"Bezeichner\", p.\"Beginn\", p.\"Ende\" FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" p JOIN \"" +
                KonditionierungSchema.TAB_KALENDER + "\" k ON k.\"ID\" = p.\"ID_Kalender\" WHERE k.\"Groesse\" = ? AND k.\"ID_Gebaeude\" IS ? AND " +
                "k.\"ID_Zone\" IS ? AND k.\"ID_Gebaeude_Stamm\" IS ? AND k.\"ID_Zone_Stamm\" IS ? AND k.\"ID_Vorlage\" IS ? AND p.\"Art\" = ? AND " +
                "p.\"Wert\" IS NULL AND p.\"Aus\" = 0 AND p.\"Woche\" IS NULL AND p.\"ID_Woche\" IS NULL AND p.\"WieWochentag\" IS NULL AND " +
                "p.\"Beginn\" IS NOT NULL AND p.\"Ende\" IS NOT NULL AND p.\"Rang\" BETWEEN ? AND ? ORDER BY p.\"Rang\"",
                new[] { new DbParam("@a", ALLE) }.Concat(eigner.Parameter())
                    .Concat(new[] { new DbParam("@art", DbWerte.KOND_ART_FERIEN), new DbParam("@von", KalenderbedienungSchema.RANG_FERIEN_ERSTER),
                                    new DbParam("@bis", RANG_FERIENLISTE_LETZTER) }).ToArray());
            if (t == null) return liste;
            foreach (DataRow r in t.Rows)
                liste.Add(new Ferienzeile(Tx(r, "Bezeichner") ?? "", I(r, "Beginn").Value, I(r, "Ende").Value));
            return liste;
        }

        // =================================================================
        //  Die Namen der Ferienzeiträume 1 bis 4 (Bezeichner der Spiegelperioden)
        // =================================================================

        /// <summary>Die Spiegelperiode k (Rang 199 + k) im gemeinsamen Kalender eines Eigentümers (Art FERIEN, ohne Angabe).</summary>
        private const string SPIEGELPERIODE =
            "\"Rang\" = ? AND \"Art\" = ? AND \"Wert\" IS NULL AND \"Aus\" = 0 AND \"Woche\" IS NULL AND \"ID_Woche\" IS NULL AND " +
            "\"WieWochentag\" IS NULL AND \"ID_Kalender\" IN (SELECT \"ID\" FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE " +
            BEDINGUNG + " AND \"Groesse\" = ?)";

        /// <summary>
        /// <b>Die Namen der Ferienzeiträume 1 bis 4</b>: je Ferienspalte der Bezeichner ihrer Spiegelperiode im gemeinsamen
        /// Kalender (Rang 200 … 203); <c>null</c>, wo keine steht. Ohne Schritt vier Mal <c>null</c>.
        /// </summary>
        public static string[] FeriennamenLesen(Schluessel eigner)
        {
            var namen = new string[KalenderbedienungSchema.FERIENSPALTEN];
            if (eigner == null || !SchrittSteht()) return namen;
            for (int k = 0; k < namen.Length; k++)
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT \"Bezeichner\" FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" WHERE " + SPIEGELPERIODE + " LIMIT 1",
                    SpiegelParameter(eigner, k));
                namen[k] = o == null || o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);
            }
            return namen;
        }

        /// <summary>
        /// <b>Schreibt die Namen der Ferienzeiträume 1 bis 4</b> in die Bezeichner ihrer Spiegelperioden. Der Spiegel-Trigger
        /// (<see cref="KalenderbedienungSchema"/>) legt die Perioden bei jeder Datumsänderung einer Ferienspalte neu als
        /// „Ferien k" an; deshalb läuft dieser Schritt NACH dem Schreiben der Spalten im selben Vorgang. Ein Name
        /// <c>null</c> lässt den Bezeichner stehen, eine fehlende Spiegelperiode (Zeitraum „aus") bleibt ohne Zeile.
        /// </summary>
        /// <returns>Die Zahl der umbenannten Perioden.</returns>
        public static int FeriennamenSchreiben(DbVorgang v, Schluessel eigner, IReadOnlyList<string> namen)
        {
            if (v == null) throw new ArgumentNullException(nameof(v));
            if (eigner == null || namen == null || !SchrittSteht()) return 0;
            int n = 0;
            for (int k = 0; k < KalenderbedienungSchema.FERIENSPALTEN && k < namen.Count; k++)
            {
                string name = string.IsNullOrWhiteSpace(namen[k]) ? null : namen[k].Trim();
                if (name == null || name.Length > KonditionierungSchema.BEZEICHNER_MAX_ZEICHEN) continue;
                n += v.Ausfuehren("UPDATE \"" + KonditionierungSchema.TAB_PERIODE + "\" SET \"Bezeichner\" = ? WHERE \"Bezeichner\" IS NOT ? AND " +
                                  SPIEGELPERIODE,
                                  new[] { new DbParam("@n", name), new DbParam("@n2", name) }.Concat(SpiegelParameter(eigner, k)).ToArray());
            }
            return n;
        }

        /// <summary>
        /// <b>Die Hülle jedes Schreibwegs der Ferienspalten</b> <c>Ferienbeginn/-ende_1…4</c>: liest vor der
        /// <paramref name="aktion"/> die Namen der Ferienzeiträume 1 bis 4 (<see cref="FeriennamenLesen"/>), führt sie aus und
        /// schreibt die Namen danach zurück (<see cref="FeriennamenSchreiben"/>) — der Spiegel-Trigger legt die Spiegelperioden
        /// bei jeder Datumsänderung neu als „Ferien k" an. Im laufenden Vorgang (<see cref="Vorgangsklammer"/>) schreibt sie in
        /// ihm, sonst in einem eigenen. Ein Name, dessen Zeitraum die Aktion entfernt, fällt mit seiner Spiegelperiode; ein
        /// unverändertes Datum lässt den Trigger ruhen und die Namen stehen. Namen wirken nie auf die Rechnung.
        /// </summary>
        /// <param name="eigner">Gebäude oder Katalogbau; <c>null</c> führt nur die Aktion aus.</param>
        /// <param name="aktion">Das Schreiben der Spalten; <c>false</c> heißt gescheitert, dann bleibt alles, wie die Aktion es lässt.</param>
        /// <returns>Das Ergebnis der Aktion.</returns>
        public static bool MitFeriennamen(Schluessel eigner, Func<bool> aktion)
        {
            if (aktion == null) throw new ArgumentNullException(nameof(aktion));
            string[] namen = FeriennamenLesen(eigner);
            if (!aktion()) return false;
            if (eigner == null || namen.All(n => n == null)) return true;
            DbVorgang laufend = Vorgangsklammer.Aktueller;
            if (laufend != null)
            {
                FeriennamenSchreiben(laufend, eigner, namen);
                return true;
            }
            using (DbVorgang v = DataRepository.Vorgang())
            {
                FeriennamenSchreiben(v, eigner, namen);
                v.Commit();
            }
            return true;
        }

        private static DbParam[] SpiegelParameter(Schluessel eigner, int k)
            => new[] { new DbParam("@r", KalenderbedienungSchema.RANG_FERIEN_ERSTER + k), new DbParam("@art", DbWerte.KOND_ART_FERIEN) }
               .Concat(eigner.Parameter()).Concat(new[] { new DbParam("@a", ALLE) }).ToArray();

        /// <summary>
        /// <b>Bereinigt die Doppelzeilen der Ferienliste</b> (Stufe 2, Teil B): Solange der Generator die Ferienliste ab
        /// Rang 204 nicht las, legte die Kalenderbedienung je weiterem Zeitraum in jedem angelegten Größenkalender von
        /// Gebäude, Zonen und Katalogbau eine Zeile „Ferien n" (Art ZEITRAUM, Eigenband ab 310) mit der Ferienangabe des
        /// Kalenders an. Eine solche Zeile — Bezeichner „Ferien n", Zeitraum gleich der Ferienperiode des gemeinsamen
        /// Kalenders des Gebäudes auf Rang 199 + n, Angabe gleich einer FERIEN-Periode desselben Kalenders — wird die
        /// FERIEN-Periode auf Rang 199 + n, die der Generator dort erzeugt; ist der Rang schon belegt, fällt sie.
        /// Deterministisch, wiederholbar und ergebnisneutral (dieselben Tage mit derselben Angabe).
        /// </summary>
        /// <returns>Die Zahl der umgesetzten und entfernten Zeilen.</returns>
        public static int FerienzeilenBereinigen(DbVorgang v)
        {
            if (v == null) throw new ArgumentNullException(nameof(v));
            if (!SchrittSteht()) return 0;
            int n = 0;
            foreach ((string eigner, string zone) in new[] { ("ID_Gebaeude", "ID_Zone"), ("ID_Gebaeude_Stamm", "ID_Zone_Stamm") })
            {
                string per = "\"" + KonditionierungSchema.TAB_PERIODE + "\"", kal = "\"" + KonditionierungSchema.TAB_KALENDER + "\"";
                // Die passende Ferienperiode des gemeinsamen Kalenders zur Zeile d (aeussere Zeile der Anweisung).
                string ferienperiode =
                    "FROM " + per + " a JOIN " + kal + " ka ON ka.\"ID\" = a.\"ID_Kalender\" JOIN " + kal + " kd ON kd.\"ID\" = " + per + ".\"ID_Kalender\" " +
                    "WHERE ka.\"Groesse\" = ? AND ka.\"" + eigner + "\" = kd.\"" + eigner + "\" AND ka.\"" + zone + "\" IS NULL AND kd.\"Groesse\" <> ? " +
                    "AND a.\"Art\" = ? AND a.\"Rang\" BETWEEN ? AND ? AND a.\"Wert\" IS NULL AND a.\"Aus\" = 0 AND a.\"Woche\" IS NULL " +
                    "AND a.\"ID_Woche\" IS NULL AND a.\"WieWochentag\" IS NULL AND a.\"Beginn\" = " + per + ".\"Beginn\" AND a.\"Ende\" = " + per +
                    ".\"Ende\" AND " + per + ".\"Bezeichner\" = '" + Standardfahrplan.BEZEICHNER_FERIEN + " ' || (a.\"Rang\" - ?)";
                string zeile =
                    per + ".\"Art\" = ? AND " + per + ".\"Rang\" >= ? AND EXISTS (SELECT 1 FROM " + per + " f WHERE f.\"ID_Kalender\" = " + per +
                    ".\"ID_Kalender\" AND f.\"Art\" = ? AND f.\"Rang\" BETWEEN ? AND ? AND f.\"Wert\" IS " + per + ".\"Wert\" AND f.\"Aus\" = " + per +
                    ".\"Aus\" AND f.\"Woche\" IS " + per + ".\"Woche\" AND f.\"ID_Woche\" IS " + per + ".\"ID_Woche\" AND f.\"WieWochentag\" IS " + per +
                    ".\"WieWochentag\" AND (f.\"Wert\" IS NOT NULL OR f.\"Aus\" = 1 OR f.\"Woche\" IS NOT NULL OR f.\"ID_Woche\" IS NOT NULL OR " +
                    "f.\"WieWochentag\" IS NOT NULL))";
                DbParam[] pz() => new[]
                {
                    new DbParam("@z", DbWerte.KOND_ART_ZEITRAUM), new DbParam("@e", Standardfahrplan.RANG_EIGEN),
                    new DbParam("@fa", DbWerte.KOND_ART_FERIEN), new DbParam("@fv", KalenderbedienungSchema.RANG_FERIEN_ERSTER),
                    new DbParam("@fb", RANG_FERIENLISTE_LETZTER),
                };
                DbParam[] pf() => new[]
                {
                    new DbParam("@a", ALLE), new DbParam("@a2", ALLE), new DbParam("@art", DbWerte.KOND_ART_FERIEN),
                    new DbParam("@von", RANG_FERIENLISTE), new DbParam("@bis", RANG_FERIENLISTE_LETZTER),
                    new DbParam("@off", KalenderbedienungSchema.RANG_FERIEN_ERSTER - 1),
                };
                // 1. Umsetzen, wo der Rang im Kalender frei ist.
                n += v.Ausfuehren(
                    "UPDATE " + per + " SET \"Art\" = ?, \"Rang\" = (SELECT a.\"Rang\" " + ferienperiode + " ORDER BY a.\"Rang\" LIMIT 1), " +
                    "\"Bezeichner\" = (SELECT a.\"Bezeichner\" " + ferienperiode + " ORDER BY a.\"Rang\" LIMIT 1) WHERE " + zeile +
                    " AND EXISTS (SELECT 1 " + ferienperiode + " AND NOT EXISTS (SELECT 1 FROM " + per + " b WHERE b.\"ID_Kalender\" = " + per +
                    ".\"ID_Kalender\" AND b.\"Rang\" = a.\"Rang\"))",
                    new[] { new DbParam("@neu", DbWerte.KOND_ART_FERIEN) }.Concat(pf()).Concat(pf()).Concat(pz()).Concat(pf()).ToArray());
                // 2. Entfernen, wo die FERIEN-Periode schon steht.
                n += v.Ausfuehren("DELETE FROM " + per + " WHERE " + zeile + " AND EXISTS (SELECT 1 " + ferienperiode + ")",
                                  pz().Concat(pf()).ToArray());
            }
            return n;
        }

        private static int? I(DataRow r, string spalte)
            => r[spalte] == DBNull.Value ? (int?)null : Convert.ToInt32(r[spalte], CultureInfo.InvariantCulture);

        private static string Tx(DataRow r, string spalte)
            => r[spalte] == DBNull.Value ? null : Convert.ToString(r[spalte], CultureInfo.InvariantCulture);

        private static long? L(DataRow r, string spalte)
            => r[spalte] == DBNull.Value ? (long?)null : Convert.ToInt64(r[spalte], CultureInfo.InvariantCulture);

        private static string T(DataRow r, string spalte)
            => r[spalte] == DBNull.Value ? "∅" : Convert.ToString(r[spalte], CultureInfo.InvariantCulture);
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Baustoffkatalog und seine Projektkopie</b> (Gebäudesimulation Stufe G3, Schritt S-A;
    /// Softwarearchitektur 2.2 und 2.6) — Lesen, Anlegen, Ändern, Löschen und der Kopierweg
    /// Katalog → Projekt.
    ///
    /// <para><b>Kopiersemantik</b> wie bei den Erzeugern: Ein Projekt KOPIERT den Katalogsatz
    /// (<see cref="CopyFromStamm(int, int)"/>), und jede Schicht eines Projektaufbaus zeigt auf
    /// die Projektkopie, nie auf den Katalog (W11). Eine spätere Katalogpflege verschiebt kein
    /// gerechnetes Projekt.</para>
    ///
    /// <para><b>NULL-erhaltend über die eine Spaltenliste.</b> Der Kopierweg geht über
    /// <see cref="BaustoffSchema.Fachspalten"/> — keine abgetippte Aufzählung, und ein NULL bleibt
    /// NULL (ein fehlender Stoffwert ist „nicht angegeben", eine 0 wäre ein anderer Stoff).</para>
    ///
    /// <para><b>Der Schutz der Auslieferung:</b> Ein Katalogsatz mit <c>ReadOnly</c> lässt sich
    /// weder ändern noch löschen — wer ihn ändern will, dupliziert ihn (Entscheid AD-Q11). Ein
    /// Stoff, auf den eine Schicht zeigt, lässt sich nicht löschen: Der Fremdschlüssel ist
    /// restriktiv, und die Prüfung sagt es vorher mit Namen und Zahl.</para>
    ///
    /// <para><b>Gefiltert wird VOR dem Raster</b> — in der Abfrage, nicht in der Liste
    /// (<see cref="LesenKatalog"/>). Alle Zugriffe über <see cref="DataRepository"/> mit
    /// <c>?</c>-Parametern; die Meldungen kommen aus <c>MyResource</c> (Präfix <c>BAUSTOFF_</c>)
    /// und gehen als <see cref="Ergebnis"/> an den Aufrufer — der Kern zeigt keinen Dialog.</para>
    /// </summary>
    public sealed class BaustoffCtrl
    {
        /// <summary>Der Katalog.</summary>
        public const string TAB_STAMM = BaustoffSchema.TAB_STAMM;

        /// <summary>Die Projektkopie.</summary>
        public const string TAB_PROJEKT = BaustoffSchema.TAB_PROJEKT;

        // ---- Die Bänder der Stoffwerte (Mehrzonenkonzept 3.5): außerhalb ist ein Wert kein Wert.

        /// <summary>Kleinste zulässige Wärmeleitfähigkeit [W/(m·K)].</summary>
        public const double LAMBDA_MIN = 0.005;
        /// <summary>Größte zulässige Wärmeleitfähigkeit [W/(m·K)].</summary>
        public const double LAMBDA_MAX = 500.0;
        /// <summary>Kleinste zulässige Rohdichte [kg/m³].</summary>
        public const double RHO_MIN = 5.0;
        /// <summary>Größte zulässige Rohdichte [kg/m³].</summary>
        public const double RHO_MAX = 8000.0;
        /// <summary>Kleinste zulässige Wärmekapazität [J/(kg·K)].</summary>
        public const double CP_MIN = 100.0;
        /// <summary>Größte zulässige Wärmekapazität [J/(kg·K)].</summary>
        public const double CP_MAX = 5000.0;

        /// <summary>Was ein Schreibversuch ergeben hat: gelungen, die Meldung für den Anwender, die Id.</summary>
        public sealed record Ergebnis(bool Ok, string Meldung, int Id)
        {
            internal static Ergebnis Gut(int id) => new Ergebnis(true, "", id);
            internal static Ergebnis Fehler(string meldung) => new Ergebnis(false, meldung ?? "", 0);
        }

        // =================================================================
        //  Lesen
        // =================================================================

        /// <summary>
        /// Der Katalog, nach Gruppe und Bezeichner sortiert, wahlweise eingeengt — die Einengung
        /// steht in der Abfrage. <paramref name="gruppe"/> <c>null</c> = alle Gruppen;
        /// <paramref name="hersteller"/> <c>null</c> = alle, <c>""</c> = nur herstellerneutral.
        /// </summary>
        public List<BaustoffModel> LesenKatalog(string gruppe = null, string hersteller = null)
        {
            var bedingung = new List<string>();
            var p = new List<DbParam>();
            if (gruppe != null) { bedingung.Add("\"Gruppe\" = ?"); p.Add(new DbParam("@g", gruppe)); }
            if (hersteller != null)
            {
                if (hersteller.Length == 0) bedingung.Add("\"Hersteller\" IS NULL");
                else { bedingung.Add("\"Hersteller\" = ?"); p.Add(new DbParam("@h", hersteller)); }
            }
            string sql = "SELECT * FROM \"" + TAB_STAMM + "\"" +
                         (bedingung.Count > 0 ? " WHERE " + string.Join(" AND ", bedingung) : "") +
                         " ORDER BY \"Gruppe\", \"Bezeichner\", \"ID\"";
            return Liste(DataRepository.GetDataTable(sql, p.ToArray()));
        }

        /// <summary>Die Baustoffe eines Projekts, nach Bezeichner sortiert.</summary>
        public List<BaustoffModel> LesenProjekt(int idProjekt)
        {
            return Liste(DataRepository.GetDataTable(
                "SELECT * FROM \"" + TAB_PROJEKT + "\" WHERE \"ID_Projekt\" = ? ORDER BY \"Bezeichner\", \"ID\"",
                new DbParam("@p", idProjekt)));
        }

        /// <summary>Ein Katalogsatz über seine Id; <c>null</c>, wenn es ihn nicht gibt.</summary>
        public BaustoffModel LesenKatalogsatz(int id)
            => Liste(DataRepository.GetDataTable("SELECT * FROM \"" + TAB_STAMM + "\" WHERE \"ID\" = ?",
                                                 new DbParam("@id", id))).FirstOrDefault();

        /// <summary>Eine Projektkopie über ihre Id; <c>null</c>, wenn es sie nicht gibt.</summary>
        public BaustoffModel LesenProjektsatz(int id)
            => Liste(DataRepository.GetDataTable("SELECT * FROM \"" + TAB_PROJEKT + "\" WHERE \"ID\" = ?",
                                                 new DbParam("@id", id))).FirstOrDefault();

        /// <summary>Die Gruppen des Katalogs in Anzeigereihenfolge — die Auswahl des Gruppenfilters.</summary>
        public static IReadOnlyList<string> Gruppen() => Werte("Gruppe");

        /// <summary>Die Hersteller des Katalogs in Anzeigereihenfolge (ohne die herstellerneutralen Sätze).</summary>
        public static IReadOnlyList<string> Hersteller() => Werte("Hersteller");

        /// <summary>
        /// <b>Die Zeilen der Katalogverwaltung</b> nach <see cref="Katalogfilterprofil.FuerBaustoff"/>
        /// — EINE Abfrage; ein Satz der Auslieferung trägt das Schloss.
        /// </summary>
        public static IReadOnlyList<Katalogfilterzeile> Katalogfilterzeilen()
        {
            var liste = new List<Katalogfilterzeile>();
            DataTable dt = StilleDb.Tabelle(
                "SELECT \"ID\", \"Bezeichner\", \"Gruppe\", \"Hersteller\", \"Lambda\", \"Rho\", \"cp\", \"Quelle\", \"ReadOnly\" " +
                "FROM \"" + TAB_STAMM + "\" ORDER BY \"Gruppe\", \"Bezeichner\"");
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                string bezeichner = Katalogfeld.Text(r, "Bezeichner");
                var zeile = new Katalogfilterzeile(Katalogfeld.Ganzzahl(r, "ID"), bezeichner)
                {
                    Geschuetzt = Katalogfeld.Kennzeichen(r, "ReadOnly")
                };
                liste.Add(zeile
                    .MitText(Katalogfilterprofil.SpBezeichner, bezeichner)
                    .MitText(Katalogfilterprofil.SpGruppe, Katalogfeld.Text(r, "Gruppe"))
                    .MitText(Katalogfilterprofil.SpHersteller, Katalogfeld.Text(r, "Hersteller"))
                    .MitZahl(Katalogfilterprofil.SpLambda, Katalogfeld.Zahl(r, "Lambda"), 3)
                    .MitZahl(Katalogfilterprofil.SpRho, Katalogfeld.Zahl(r, "Rho"), 0)
                    .MitZahl(Katalogfilterprofil.SpCp, Katalogfeld.Zahl(r, "cp"), 0)
                    .MitText(Katalogfilterprofil.SpQuelle, Katalogfeld.Text(r, "Quelle")));
            }
            return liste;
        }

        // =================================================================
        //  Prüfen
        // =================================================================

        /// <summary>
        /// Die Prüfung eines Satzes vor dem Schreiben — <c>null</c>, wenn er gültig ist, sonst die
        /// Meldung für den Anwender. Name Pflicht, Längen nach Schema, Herkunft aus der Liste,
        /// Stoffwerte — wenn angegeben — im Band (Mehrzonenkonzept 3.5).
        /// </summary>
        public static string Pruefen(BaustoffModel m)
        {
            if (m == null || string.IsNullOrWhiteSpace(m.Bezeichner)) return MyResource.Resource.BAUSTOFF_MSG_NAME_LEER;
            string t = Laenge(MyResource.Resource.KFLT_SP_NAME, m.Bezeichner, BaustoffSchema.LAENGE_BEZEICHNER)
                       ?? Laenge(MyResource.Resource.KFLT_SP_GRUPPE, m.Gruppe, BaustoffSchema.LAENGE_GRUPPE)
                       ?? Laenge(MyResource.Resource.KFLT_SP_HERSTELLER, m.Hersteller, BaustoffSchema.LAENGE_HERSTELLER)
                       ?? Laenge(MyResource.Resource.KFLT_SP_QUELLE, m.Quelle, BaustoffSchema.LAENGE_QUELLE)
                       ?? Laenge(MyResource.Resource.BAUTEIL_FELD_QUELLKENNUNG, m.Quellkennung, BaustoffSchema.LAENGE_QUELLKENNUNG)
                       ?? HerkunftPruefen(m.Herkunft);
            if (t != null) return t;
            return StoffwertePruefen(m.Lambda, m.Rho, m.Cp);
        }

        /// <summary>
        /// Die Bandprüfung der drei Stoffwerte — gemeinsam für Baustoff und Schicht; <c>null</c>
        /// = in Ordnung. Ein NULL ist erlaubt („nicht angegeben").
        /// </summary>
        public static string StoffwertePruefen(double? lambda, double? rho, double? cp)
        {
            return Band(MyResource.Resource.KFLT_SP_LAMBDA, lambda, LAMBDA_MIN, LAMBDA_MAX)
                   ?? Band(MyResource.Resource.KFLT_SP_RHO, rho, RHO_MIN, RHO_MAX)
                   ?? Band(MyResource.Resource.KFLT_SP_CP, cp, CP_MIN, CP_MAX);
        }

        /// <summary>Die Herkunft ist NULL oder einer der fünf Persistenzwerte; sonst die Meldung.</summary>
        internal static string HerkunftPruefen(string herkunft)
        {
            if (herkunft == null || DbWerte.HERKUENFTE.Contains(herkunft)) return null;
            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_HERKUNFT, herkunft);
        }

        /// <summary>Ein Text ist höchstens <paramref name="max"/> Zeichen lang; sonst die Meldung.</summary>
        internal static string Laenge(string feld, string wert, int max)
        {
            if (wert == null || wert.Length <= max) return null;
            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUSTOFF_MSG_TEXT_LANG, feld, max);
        }

        private static string Band(string feld, double? wert, double min, double max)
        {
            if (!wert.HasValue) return null;
            double w = wert.Value;
            if (!double.IsNaN(w) && w >= min && w <= max) return null;
            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUSTOFF_MSG_WERT_BAND,
                                 feld, w, min, max);
        }

        // =================================================================
        //  Schreiben — Katalog
        // =================================================================

        /// <summary>
        /// Legt einen Katalogsatz an — <c>ReadOnly = 0</c>, die Id vergibt AUTOINCREMENT (oberhalb
        /// der Saatgrenze <see cref="BaustoffSchema.SAAT_ID_GRENZE"/>). Ohne Herkunft gilt
        /// <see cref="DbWerte.HERKUNFT_MANUELL"/>.
        /// </summary>
        public Ergebnis KatalogAnlegen(BaustoffModel m)
        {
            string fehler = Pruefen(m) ?? NameFrei(TAB_STAMM, null, m, 0);
            if (fehler != null) return Ergebnis.Fehler(fehler);
            try
            {
                int id = DataRepository.ExecuteInsertAndGetId(
                    "INSERT INTO \"" + TAB_STAMM + "\" (\"Bezeichner\", " + Spaltenliste() + ", \"ReadOnly\") VALUES (?, " +
                    Fragezeichen(BaustoffSchema.Fachspalten.Count) + ", 0)",
                    new[] { new DbParam("@b", m.Bezeichner.Trim()) }.Concat(Fachwerte(m, m.Herkunft ?? DbWerte.HERKUNFT_MANUELL)).ToArray());
                if (id <= 0) return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                                                                  MyResource.Resource.BAUSTOFF_MSG_NICHT_GESPEICHERT, ""));
                m.ID = id;
                m.ReadOnly = false;
                return Ergebnis.Gut(id);
            }
            catch (Exception ex)
            {
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUSTOFF_MSG_NICHT_GESPEICHERT, ex.Message));
            }
        }

        /// <summary>Schreibt einen Katalogsatz zurück — nie einen der Auslieferung (<c>ReadOnly</c>).</summary>
        public Ergebnis KatalogAendern(BaustoffModel m)
        {
            BaustoffModel alt = m == null ? null : LesenKatalogsatz(m.ID);
            if (alt == null) return Ergebnis.Fehler(NichtGefunden(m?.ID ?? 0));
            if (alt.ReadOnly) return Ergebnis.Fehler(Schreibgeschuetzt(alt.Bezeichner));
            string fehler = Pruefen(m) ?? NameFrei(TAB_STAMM, null, m, m.ID);
            if (fehler != null) return Ergebnis.Fehler(fehler);
            return Zurueckschreiben(TAB_STAMM, m);
        }

        /// <summary>
        /// Löscht einen Katalogsatz — nie einen der Auslieferung und nie einen, auf den eine
        /// Katalogschicht zeigt (restriktiver Fremdschlüssel, W11).
        /// </summary>
        public Ergebnis KatalogLoeschen(int id)
        {
            BaustoffModel alt = LesenKatalogsatz(id);
            if (alt == null) return Ergebnis.Fehler(NichtGefunden(id));
            if (alt.ReadOnly) return Ergebnis.Fehler(Schreibgeschuetzt(alt.Bezeichner));
            return Loeschen(TAB_STAMM, SchemaKatalog.TAB_BAUTEILSCHICHT_STAMM, alt);
        }

        /// <summary>
        /// <b>„Duplizieren…"</b>: kopiert den Katalogsatz <paramref name="id"/> als EIGENEN Satz
        /// unter <paramref name="neuerName"/> — <c>ReadOnly = 0</c>, Herkunft
        /// <see cref="DbWerte.HERKUNFT_MANUELL"/>, alle Stoffwerte und die Quelle wie im Original
        /// (Entscheid AD-Q11: Auslieferungssätze werden nie überschrieben).
        /// </summary>
        public Ergebnis KatalogDuplizieren(int id, string neuerName)
        {
            BaustoffModel alt = LesenKatalogsatz(id);
            if (alt == null) return Ergebnis.Fehler(NichtGefunden(id));
            BaustoffModel neu = alt.Kopie();
            neu.ID = 0;
            neu.Bezeichner = neuerName ?? "";
            neu.Herkunft = DbWerte.HERKUNFT_MANUELL;
            neu.Quellkennung = null;
            return KatalogAnlegen(neu);
        }

        /// <summary>
        /// <b>„Schloss setzen…" / „Schloss aufheben…"</b> (Entscheid AD-Q15) — nur das
        /// Auslieferungskennzeichen; die Regel steht einmal in
        /// <see cref="Auslieferungskennzeichen.SetzenInTabelle"/>.
        /// </summary>
        public static Auslieferungskennzeichen.Ergebnis SchlossSetzen(IReadOnlyList<int> ids, bool gesperrt)
            => Auslieferungskennzeichen.SetzenInTabelle(TAB_STAMM, ids, gesperrt);

        // =================================================================
        //  Schreiben — Projektkopie
        // =================================================================

        /// <summary>Legt einen Baustoff des Projekts an (freie Eingabe); ohne Herkunft gilt MANUELL.</summary>
        public Ergebnis ProjektAnlegen(int idProjekt, BaustoffModel m)
        {
            if (m == null) return Ergebnis.Fehler(MyResource.Resource.BAUSTOFF_MSG_NAME_LEER);
            string fehler = Pruefen(m) ?? NameFrei(TAB_PROJEKT, idProjekt, m, 0);
            if (fehler != null) return Ergebnis.Fehler(fehler);
            try
            {
                int id = DataRepository.ExecuteInsertAndGetId(
                    "INSERT INTO \"" + TAB_PROJEKT + "\" (\"ID_Projekt\", \"Bezeichner\", " + Spaltenliste() + ") VALUES (?, ?, " +
                    Fragezeichen(BaustoffSchema.Fachspalten.Count) + ")",
                    new[] { new DbParam("@p", idProjekt), new DbParam("@b", m.Bezeichner.Trim()) }
                        .Concat(Fachwerte(m, m.Herkunft ?? DbWerte.HERKUNFT_MANUELL)).ToArray());
                if (id <= 0) return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                                                                  MyResource.Resource.BAUSTOFF_MSG_NICHT_GESPEICHERT, ""));
                m.ID = id;
                m.ID_Projekt = idProjekt;
                return Ergebnis.Gut(id);
            }
            catch (Exception ex)
            {
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUSTOFF_MSG_NICHT_GESPEICHERT, ex.Message));
            }
        }

        /// <summary>Schreibt einen Baustoff des Projekts zurück.</summary>
        public Ergebnis ProjektAendern(BaustoffModel m)
        {
            BaustoffModel alt = m == null ? null : LesenProjektsatz(m.ID);
            if (alt == null) return Ergebnis.Fehler(NichtGefunden(m?.ID ?? 0));
            string fehler = Pruefen(m) ?? NameFrei(TAB_PROJEKT, alt.ID_Projekt, m, m.ID);
            if (fehler != null) return Ergebnis.Fehler(fehler);
            return Zurueckschreiben(TAB_PROJEKT, m);
        }

        /// <summary>Löscht einen Baustoff des Projekts — nie einen, auf den eine Projektschicht zeigt.</summary>
        public Ergebnis ProjektLoeschen(int id)
        {
            BaustoffModel alt = LesenProjektsatz(id);
            if (alt == null) return Ergebnis.Fehler(NichtGefunden(id));
            return Loeschen(TAB_PROJEKT, SchemaKatalog.TAB_BAUTEILSCHICHT, alt);
        }

        // =================================================================
        //  Katalog → Projekt
        // =================================================================

        /// <summary>
        /// Kopiert einen Katalogsatz in das Projekt, sofern es ihn (gleicher Bezeichner und
        /// Hersteller) noch nicht führt — in einem eigenen Vorgang.
        /// </summary>
        /// <returns>Die Id der kopierten ODER vorhandenen Projektzeile, <c>-1</c> bei Fehler.</returns>
        public int CopyFromStamm(int stammId, int idProjekt)
        {
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    int id = CopyFromStamm(v, stammId, idProjekt);
                    if (id > 0) v.Commit(); else v.Rollback();
                    return id;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Baustoff " + stammId + " nicht in das Projekt " + idProjekt + " kopiert: " + ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Derselbe Kopierweg im Vorgang des Aufrufers — für <c>BauteilaufbauCtrl.CopyFromStamm</c>,
        /// der die Stoffe seiner Schichten mitnimmt (W11). <b>Spaltenliste
        /// <see cref="BaustoffSchema.Fachspalten"/>, NULL-erhaltend</b>; die Herkunft der Kopie ist
        /// <see cref="DbWerte.HERKUNFT_KATALOG"/>, <c>ReadOnly</c> gibt es in der Kopie nicht.
        /// </summary>
        internal static int CopyFromStamm(DbVorgang v, int stammId, int idProjekt)
        {
            DataTable dt = v.Lese("SELECT * FROM \"" + TAB_STAMM + "\" WHERE \"ID\" = ?", new DbParam("@id", stammId));
            if (dt == null || dt.Rows.Count == 0 || idProjekt <= 0) return -1;
            DataRow s = dt.Rows[0];
            string bezeichner = Convert.ToString(s["Bezeichner"], CultureInfo.InvariantCulture);
            object hersteller = s["Hersteller"];

            object vorhanden = v.Skalar(
                "SELECT \"ID\" FROM \"" + TAB_PROJEKT + "\" WHERE \"ID_Projekt\" = ? AND \"Bezeichner\" = ? AND \"Hersteller\" IS ? " +
                "ORDER BY \"ID\" LIMIT 1",
                new DbParam("@p", idProjekt), new DbParam("@b", bezeichner), new DbParam("@h", hersteller ?? DBNull.Value));
            if (vorhanden != null) return Convert.ToInt32(vorhanden, CultureInfo.InvariantCulture);

            var ps = new List<DbParam> { new DbParam("@p", idProjekt), new DbParam("@b", bezeichner) };
            foreach (string spalte in BaustoffSchema.Fachspalten)
                ps.Add(spalte == BaustoffSchema.SPALTE_HERKUNFT
                    ? new DbParam("@herkunft", DbWerte.HERKUNFT_KATALOG)
                    : new DbParam("@" + spalte, s.Table.Columns.Contains(spalte) ? s[spalte] : DBNull.Value));

            return v.EinfuegenUndId(
                "INSERT INTO \"" + TAB_PROJEKT + "\" (\"ID_Projekt\", \"Bezeichner\", " + Spaltenliste() + ") VALUES (?, ?, " +
                Fragezeichen(BaustoffSchema.Fachspalten.Count) + ")", ps.ToArray());
        }

        // =================================================================
        //  intern
        // =================================================================

        private Ergebnis Zurueckschreiben(string tabelle, BaustoffModel m)
        {
            try
            {
                var ps = new List<DbParam> { new DbParam("@b", m.Bezeichner.Trim()) };
                ps.AddRange(Fachwerte(m, m.Herkunft));
                ps.Add(new DbParam("@id", m.ID));
                int n = DataRepository.ExecuteNonQuery(
                    "UPDATE \"" + tabelle + "\" SET \"Bezeichner\" = ?, " +
                    string.Join(", ", BaustoffSchema.Fachspalten.Select(s => "\"" + s + "\" = ?")) + " WHERE \"ID\" = ?",
                    ps.ToArray());
                return n == 1 ? Ergebnis.Gut(m.ID) : Ergebnis.Fehler(NichtGefunden(m.ID));
            }
            catch (Exception ex)
            {
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUSTOFF_MSG_NICHT_GESPEICHERT, ex.Message));
            }
        }

        private Ergebnis Loeschen(string tabelle, string schichttabelle, BaustoffModel alt)
        {
            long benutzt = Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM \"" + schichttabelle + "\" WHERE \"ID_Baustoff\" = ?", new DbParam("@id", alt.ID)),
                CultureInfo.InvariantCulture);
            if (benutzt > 0)
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUSTOFF_MSG_VERWENDET,
                                                     alt.Bezeichner, benutzt));
            int n = DataRepository.ExecuteNonQuery("DELETE FROM \"" + tabelle + "\" WHERE \"ID\" = ?", new DbParam("@id", alt.ID));
            return n == 1 ? Ergebnis.Gut(alt.ID) : Ergebnis.Fehler(NichtGefunden(alt.ID));
        }

        /// <summary>
        /// Name und Hersteller sind auf ihrer Seite frei (der Katalog als Ganzes, die Kopie je
        /// Projekt); <c>null</c> = frei, sonst die Meldung. <c>Hersteller IS ?</c> hält NULL gegen NULL.
        /// </summary>
        private static string NameFrei(string tabelle, int? idProjekt, BaustoffModel m, int eigeneId)
        {
            var ps = new List<DbParam>
            {
                new DbParam("@b", m.Bezeichner.Trim()),
                new DbParam("@h", (object)Leer(m.Hersteller) ?? DBNull.Value),
                new DbParam("@id", eigeneId)
            };
            string sql = "SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"Bezeichner\" = ? AND \"Hersteller\" IS ? AND \"ID\" <> ?";
            if (idProjekt.HasValue) { sql += " AND \"ID_Projekt\" = ?"; ps.Add(new DbParam("@p", idProjekt.Value)); }
            long n = Convert.ToInt64(DataRepository.ExecuteScalar(sql, ps.ToArray()), CultureInfo.InvariantCulture);
            return n == 0 ? null
                : string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUSTOFF_MSG_NAME_VERGEBEN, m.Bezeichner.Trim());
        }

        /// <summary>Die Fachwerte in der Reihenfolge von <see cref="BaustoffSchema.Fachspalten"/>; NULL bleibt NULL.</summary>
        private static IEnumerable<DbParam> Fachwerte(BaustoffModel m, string herkunft)
        {
            yield return Text("@gr", m.Gruppe);
            yield return Text("@he", m.Hersteller);
            yield return Zahl("@l", m.Lambda);
            yield return Zahl("@r", m.Rho);
            yield return Zahl("@c", m.Cp);
            yield return Text("@q", m.Quelle);
            yield return Text("@hk", herkunft);
            yield return Text("@qk", m.Quellkennung);
        }

        private static string Spaltenliste()
            => string.Join(", ", BaustoffSchema.Fachspalten.Select(s => "\"" + s + "\""));

        internal static string Fragezeichen(int n) => string.Join(", ", Enumerable.Repeat("?", n));

        /// <summary>Ein Text als Parameter; leer oder nur Leerraum wird NULL.</summary>
        internal static DbParam Text(string name, string wert)
            => new DbParam(name, DbParamTyp.VarWChar) { Wert = (object)Leer(wert) ?? DBNull.Value };

        /// <summary>Eine nullbare Zahl als Parameter mit ausdrücklichem Typ.</summary>
        internal static DbParam Zahl(string name, double? wert)
            => new DbParam(name, DbParamTyp.Double) { Wert = wert.HasValue ? (object)wert.Value : DBNull.Value };

        private static string Leer(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static string NichtGefunden(int id)
            => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUSTOFF_MSG_NICHT_GEFUNDEN, id);

        private static string Schreibgeschuetzt(string name)
            => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUSTOFF_MSG_SCHREIBGESCHUETZT, name);

        private static IReadOnlyList<string> Werte(string spalte)
        {
            var liste = new List<string>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT \"" + spalte + "\" FROM \"" + TAB_STAMM + "\" WHERE \"" + spalte + "\" IS NOT NULL " +
                "GROUP BY \"" + spalte + "\" ORDER BY \"" + spalte + "\"");
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
            {
                string w = Convert.ToString(r[0], CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(w)) liste.Add(w);
            }
            return liste;
        }

        private static List<BaustoffModel> Liste(DataTable dt)
        {
            var liste = new List<BaustoffModel>();
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows) liste.Add(AusZeile(r));
            return liste;
        }

        /// <summary>Ein Satz aus einer Zeile beider Tabellen — nach Namen, NULL bleibt <c>null</c>.</summary>
        internal static BaustoffModel AusZeile(DataRow r)
        {
            return new BaustoffModel
            {
                ID = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture),
                ID_Projekt = r.Table.Columns.Contains("ID_Projekt") && r["ID_Projekt"] != DBNull.Value
                    ? Convert.ToInt32(r["ID_Projekt"], CultureInfo.InvariantCulture) : (int?)null,
                Bezeichner = Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture) ?? "",
                Gruppe = TextAus(r, "Gruppe"),
                Hersteller = TextAus(r, "Hersteller"),
                Lambda = ZahlAus(r, "Lambda"),
                Rho = ZahlAus(r, "Rho"),
                Cp = ZahlAus(r, "cp"),
                Quelle = TextAus(r, "Quelle"),
                Herkunft = TextAus(r, "Herkunft"),
                Quellkennung = TextAus(r, "Quellkennung"),
                ReadOnly = r.Table.Columns.Contains("ReadOnly") && r["ReadOnly"] != DBNull.Value
                           && Convert.ToInt64(r["ReadOnly"], CultureInfo.InvariantCulture) != 0
            };
        }

        internal static string TextAus(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value
                ? Convert.ToString(r[spalte], CultureInfo.InvariantCulture) : null;

        internal static double? ZahlAus(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value
                ? Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture) : (double?)null;

        internal static int? GanzAus(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value
                ? Convert.ToInt32(r[spalte], CultureInfo.InvariantCulture) : (int?)null;
    }
}

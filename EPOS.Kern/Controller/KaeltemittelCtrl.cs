using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kältemitteltabelle</b> (<c>Tab_Kaeltemittel_STAMM</c>, Schritt <see cref="KaeltemittelSchema.SCHRITT"/>,
    /// Fachkonzept Kältemittel als Stammdatum 4.1): Liste, Satz zur ID, Code → ID und die Pflege der Verwaltung
    /// (KM4-E3-c, Fachkonzept 7.6: Anlegen, Ändern, Löschen, Schloss). Alle Zugriffe über <see cref="DataRepository"/> mit <c>?</c>-Parametern;
    /// eine Datenbank vor dem Schritt liefert die leere Liste bzw. <c>null</c>.
    /// </summary>
    public static class KaeltemittelCtrl
    {
        /// <summary>Die Kältemitteltabelle.</summary>
        public const string TABLE = KaeltemittelSchema.TAB_STAMM;

        /// <summary>
        /// Code → ID (Parameter: der getrimmte Code). Den Vergleich ohne Groß/Klein trägt die Spalte selbst
        /// (<c>COLLATE NOCASE</c>, <c>UNIQUE</c>) — höchstens ein Treffer.
        /// </summary>
        internal const string SQL_ID_ZUM_CODE =
            "SELECT \"ID\" FROM \"" + KaeltemittelSchema.TAB_STAMM + "\" WHERE \"" + KaeltemittelSchema.SPALTE_CODE + "\" = ?";

        /// <summary>Trefferprobe einer ID (Parameter: die ID); liefert die ID, wenn es die Zeile gibt.</summary>
        internal const string SQL_ID_PROBE =
            "SELECT \"ID\" FROM \"" + KaeltemittelSchema.TAB_STAMM + "\" WHERE \"ID\" = ?";

        // Reihenfolge: gepflegte Sortierung zuerst, dann der Code; ohne Sortierung nach dem Code.
        private const string SQL_LISTE =
            "SELECT * FROM \"" + KaeltemittelSchema.TAB_STAMM + "\" ORDER BY IIF(\"" + KaeltemittelSchema.SPALTE_SORTIERUNG +
            "\" IS NULL, 1, 0), \"" + KaeltemittelSchema.SPALTE_SORTIERUNG + "\", \"" + KaeltemittelSchema.SPALTE_CODE + "\"";

        /// <summary>Ein Kältemittel; Persistenzwerte der Wertemengen aus <see cref="DbWerte"/> (<c>KAELTEMITTEL_*</c>).</summary>
        public sealed record Kaeltemittelsatz(
            int Id, string Code, string Bezeichnung, string Gruppe, double? GWP, string GWP_Quelle, bool? F_Gas,
            bool? Natuerlich, string Sicherheitsgruppe, string PFAS_Status, string Status, string Quelle, int? Sortierung,
            bool ReadOnly);

        /// <summary>Gibt es die Tabelle (Schritt 216)?</summary>
        public static bool Vorhanden() => DataRepository.TabelleVorhanden(TABLE);

        /// <summary>Alle Kältemittel, nach Sortierung und Code; leer vor dem Schritt.</summary>
        public static IReadOnlyList<Kaeltemittelsatz> Liste()
        {
            var liste = new List<Kaeltemittelsatz>();
            if (!Vorhanden()) return liste;
            DataTable dt = DataRepository.GetDataTable(SQL_LISTE);
            if (dt != null) foreach (DataRow r in dt.Rows) liste.Add(Satz(r));
            return liste;
        }

        /// <summary>Der Satz zur ID; <c>null</c>, wenn es ihn (oder die Tabelle) nicht gibt.</summary>
        public static Kaeltemittelsatz Laden(int id)
        {
            if (!Vorhanden()) return null;
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM \"" + TABLE + "\" WHERE \"ID\" = ?", new DbParam("?", id));
            return dt == null || dt.Rows.Count == 0 ? null : Satz(dt.Rows[0]);
        }

        /// <summary>
        /// Code → ID: Trim, ohne Groß/Klein; <c>null</c> bei leerem Text, ohne Treffer oder vor dem Schritt. Derselbe Abgleich
        /// wie beim Speichern der Kältemaschine und beim Nachtrag des Projektpakets.
        /// </summary>
        public static int? IdZumCode(string code)
        {
            string c = code?.Trim();
            if (string.IsNullOrEmpty(c) || !Vorhanden()) return null;
            object o = DataRepository.ExecuteScalar(SQL_ID_ZUM_CODE, new DbParam("?", c));
            return o == null || o == DBNull.Value ? (int?)null : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Der Satz zum Code (Abgleich wie <see cref="IdZumCode"/>); <c>null</c> ohne Treffer.</summary>
        public static Kaeltemittelsatz LadenZumCode(string code)
        {
            int? id = IdZumCode(code);
            return id.HasValue ? Laden(id.Value) : null;
        }

        // =================================================================
        //  Pflege (KM4-E3-c, Fachkonzept 7.6): Anlegen, Ändern, Löschen, Schloss
        // =================================================================

        /// <summary>Der Schlüssel der Kältemitteltabelle in der <see cref="KatalogRegistry"/> (Schloss, Verwendung).</summary>
        public const string KATALOG_SCHLUESSEL = "KAELTEMITTEL";

        /// <summary>Der Ausgang eines Schreibwegs: bei Erfolg die ID, sonst der Grund in der Oberflächensprache.</summary>
        public sealed record Ergebnis(bool Ok, string Meldung, int Id);

        /// <summary>
        /// Wie viele Kältemaschinen (Katalog und Projektkopien) verweisen über <c>ID_Kaeltemittel</c> auf die Zeile? Eine
        /// Tabelle ohne die Spalte zählt nicht.
        /// </summary>
        public static int Verwendung(int id)
        {
            int n = 0;
            foreach (string tabelle in new[] { KaeltemaschineSchema.TAB_STAMM, KaeltemaschineSchema.TAB_PROJEKT })
            {
                if (!DataRepository.TabelleVorhanden(tabelle) ||
                    !DataRepository.SpalteVorhanden(tabelle, KaeltemittelSchema.SPALTE_ID_KAELTEMITTEL)) continue;
                object o = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"" + KaeltemittelSchema.SPALTE_ID_KAELTEMITTEL + "\" = ?",
                    new DbParam("?", id));
                n += o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            return n;
        }

        /// <summary>Gibt es eine andere Zeile mit diesem Code (getrimmt, ohne Groß/Klein)?</summary>
        public static bool CodeBelegt(string code, int ausserId)
        {
            int? id = IdZumCode(code);
            return id.HasValue && id.Value != ausserId;
        }

        /// <summary>
        /// Prüft einen Satz vor dem Schreiben — ohne Datenbank, dieselben Grenzen wie die <c>CHECK</c>-Klauseln von
        /// <see cref="KaeltemittelSchema"/>, die Wertemengen aus <see cref="DbWerte"/> (<c>KAELTEMITTEL_*</c>). Liefert
        /// <c>null</c>, wenn er schreibbar ist, sonst den Grund. Leere Fachfelder sind zulässig (leer = unbekannt).
        /// </summary>
        public static string Pruefen(Kaeltemittelsatz s)
        {
            string code = s?.Code?.Trim() ?? "";
            if (code.Length == 0 || code.Length > KaeltemittelSchema.CODE_MAX)
                return Format(MyResource.Resource.KMT_MSG_CODE_UNGUELTIG, KaeltemittelSchema.CODE_MAX);
            if (Zulang(s.Bezeichnung) || Zulang(s.GWP_Quelle) || Zulang(s.Quelle))
                return Format(MyResource.Resource.KMT_MSG_TEXT_ZU_LANG, KaeltemittelSchema.TEXT_MAX);
            if (s.GWP.HasValue && (double.IsNaN(s.GWP.Value) || double.IsInfinity(s.GWP.Value) || s.GWP.Value < 0))
                return MyResource.Resource.KMT_MSG_GWP_UNGUELTIG;
            return Wertemenge(s.Gruppe, DbWerte.KAELTEMITTEL_GRUPPEN, MyResource.Resource.KMT_SP_GRUPPE)
                   ?? Wertemenge(s.Sicherheitsgruppe, DbWerte.KAELTEMITTEL_SICHERHEITSGRUPPEN, MyResource.Resource.KMT_SP_SICHERHEIT)
                   ?? Wertemenge(s.PFAS_Status, DbWerte.KAELTEMITTEL_PFAS_STATUS, MyResource.Resource.KMT_SP_PFAS)
                   ?? Wertemenge(s.Status, DbWerte.KAELTEMITTEL_STATUS, MyResource.Resource.KMT_SP_STATUS);
        }

        /// <summary>
        /// Schreibt eine Zeile: neu (<c>Id &lt;= 0</c>) oder über eine bestehende Anwenderzeile. Abgelehnt — benannt, nie
        /// mit Ausnahme — werden eine ungültige Eingabe (<see cref="Pruefen"/>), eine Auslieferungszeile (<c>ReadOnly = 1</c>;
        /// sie wird erst nach „Schloss aufheben…" änderbar und behält dann Katalogschlüssel und Prüfsumme, so dass der
        /// Katalogabgleich sie als angepasst erkennt und stehen lässt), ein vergebener Code und der geänderte Code einer
        /// verwendeten Zeile (Fachkonzept 3.2). Ein leerer Status wird <c>VORLAEUFIG</c> (4.1).
        /// </summary>
        public static Ergebnis Speichern(Kaeltemittelsatz s)
        {
            string grund = Pruefen(s);
            if (grund != null) return new Ergebnis(false, grund, s?.Id ?? 0);
            if (!Vorhanden()) return new Ergebnis(false, MyResource.Resource.KMT_MSG_TABELLE_FEHLT, 0);

            bool neu = s.Id <= 0;
            string code = s.Code.Trim();
            try
            {
                if (!neu)
                {
                    Kaeltemittelsatz alt = Laden(s.Id);
                    if (alt == null) return new Ergebnis(false, MyResource.Resource.KMT_MSG_SATZ_FEHLT, s.Id);
                    if (alt.ReadOnly) return new Ergebnis(false, MyResource.Resource.ADM_SCHLOSS_ERST_AUFHEBEN, s.Id);
                    int verwendet = Verwendung(s.Id);
                    if (!string.Equals(alt.Code, code, StringComparison.Ordinal) && verwendet > 0)
                        return new Ergebnis(false, Format(MyResource.Resource.KMT_MSG_CODE_VERWENDET, alt.Code, verwendet), s.Id);
                }
                if (CodeBelegt(code, neu ? 0 : s.Id))
                    return new Ergebnis(false, Format(MyResource.Resource.KMT_MSG_CODE_BELEGT, code), s.Id);

                string status = string.IsNullOrWhiteSpace(s.Status) ? DbWerte.GESETZ_STATUS_VORLAEUFIG : s.Status;
                var p = new List<DbParam>
                {
                    new DbParam("?", code),
                    Wert(s.Bezeichnung), Wert(s.Gruppe),
                    new DbParam("?", s.GWP.HasValue ? (object)s.GWP.Value : DBNull.Value),
                    Wert(s.GWP_Quelle),
                    new DbParam("?", s.F_Gas.HasValue ? (object)(s.F_Gas.Value ? 1 : 0) : DBNull.Value),
                    new DbParam("?", s.Natuerlich.HasValue ? (object)(s.Natuerlich.Value ? 1 : 0) : DBNull.Value),
                    Wert(s.Sicherheitsgruppe), Wert(s.PFAS_Status), new DbParam("?", status), Wert(s.Quelle),
                    new DbParam("?", s.Sortierung.HasValue ? (object)s.Sortierung.Value : DBNull.Value),
                };
                int id;
                if (neu)
                {
                    DataRepository.ExecuteNonQuery(SQL_EINFUEGEN, p.ToArray());
                    id = IdZumCode(code) ?? 0;
                }
                else
                {
                    p.Add(new DbParam("?", s.Id));
                    DataRepository.ExecuteNonQuery(SQL_AENDERN, p.ToArray());
                    id = s.Id;
                }
                return new Ergebnis(true, MyResource.Resource.PSP_MELDUNG_DATENSATZ_GESPEICHERT, id);
            }
            catch (Exception ex)
            {
                return new Ergebnis(false, Format(MyResource.Resource.PSP_MELDUNG_FEHLER_AUFGETRETEN, ex.Message), s.Id);
            }
        }

        /// <summary>
        /// Löscht eine Anwenderzeile, die keine Kältemaschine verwendet (<see cref="Verwendung"/>). Eine Auslieferungszeile
        /// und eine verwendete Zeile bleiben — benannt abgelehnt.
        /// </summary>
        public static Ergebnis Loeschen(int id)
        {
            try
            {
                Kaeltemittelsatz s = Laden(id);
                if (s == null) return new Ergebnis(false, MyResource.Resource.KMT_MSG_SATZ_FEHLT, id);
                if (s.ReadOnly) return new Ergebnis(false, MyResource.Resource.ADM_SCHLOSS_ERST_AUFHEBEN, id);
                int n = Verwendung(id);
                if (n > 0) return new Ergebnis(false, Format(MyResource.Resource.KMT_MSG_VERWENDET, s.Code, n), id);
                int geloescht = DataRepository.ExecuteNonQuery(
                    "DELETE FROM \"" + TABLE + "\" WHERE \"ID\" = ? AND \"ReadOnly\" = 0", new DbParam("?", id));
                return geloescht > 0
                    ? new Ergebnis(true, "", id)
                    : new Ergebnis(false, MyResource.Resource.KMT_MSG_SATZ_FEHLT, id);
            }
            catch (Exception ex)
            {
                return new Ergebnis(false, Format(MyResource.Resource.PSP_MELDUNG_FEHLER_AUFGETRETEN, ex.Message), id);
            }
        }

        /// <summary>
        /// „Schloss setzen…" / „Schloss aufheben…" (AD-Q15) — das Auslieferungskennzeichen umschalten; Katalogschlüssel und
        /// Prüfsumme bleiben, eine entsperrte und geänderte Zeile gilt dem Katalogabgleich damit als angepasst.
        /// </summary>
        public static Auslieferungskennzeichen.Ergebnis SchlossSetzen(IReadOnlyList<int> ids, bool gesperrt) =>
            Auslieferungskennzeichen.Setzen(KATALOG_SCHLUESSEL, ids, gesperrt);

        private static readonly string SPALTENLISTE =
            "\"" + KaeltemittelSchema.SPALTE_CODE + "\", \"" + KaeltemittelSchema.SPALTE_BEZEICHNUNG + "\", \"" +
            KaeltemittelSchema.SPALTE_GRUPPE + "\", \"" + KaeltemittelSchema.SPALTE_GWP + "\", \"" +
            KaeltemittelSchema.SPALTE_GWP_QUELLE + "\", \"" + KaeltemittelSchema.SPALTE_F_GAS + "\", \"" +
            KaeltemittelSchema.SPALTE_NATUERLICH + "\", \"" + KaeltemittelSchema.SPALTE_SICHERHEITSGRUPPE + "\", \"" +
            KaeltemittelSchema.SPALTE_PFAS_STATUS + "\", \"" + KaeltemittelSchema.SPALTE_STATUS + "\", \"" +
            KaeltemittelSchema.SPALTE_QUELLE + "\", \"" + KaeltemittelSchema.SPALTE_SORTIERUNG + "\"";

        /// <summary>Neue Anwenderzeile (zwölf Parameter in der Folge der Fachspalten; <c>ReadOnly = 0</c>).</summary>
        internal static readonly string SQL_EINFUEGEN =
            "INSERT INTO \"" + KaeltemittelSchema.TAB_STAMM + "\" (" + SPALTENLISTE + ", \"ReadOnly\") " +
            "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 0)";

        /// <summary>Ändert eine Anwenderzeile (zwölf Fachwerte, dann die ID); eine gesperrte Zeile trifft es nie.</summary>
        internal static readonly string SQL_AENDERN =
            "UPDATE \"" + KaeltemittelSchema.TAB_STAMM + "\" SET \"" + KaeltemittelSchema.SPALTE_CODE + "\" = ?, \"" +
            KaeltemittelSchema.SPALTE_BEZEICHNUNG + "\" = ?, \"" + KaeltemittelSchema.SPALTE_GRUPPE + "\" = ?, \"" +
            KaeltemittelSchema.SPALTE_GWP + "\" = ?, \"" + KaeltemittelSchema.SPALTE_GWP_QUELLE + "\" = ?, \"" +
            KaeltemittelSchema.SPALTE_F_GAS + "\" = ?, \"" + KaeltemittelSchema.SPALTE_NATUERLICH + "\" = ?, \"" +
            KaeltemittelSchema.SPALTE_SICHERHEITSGRUPPE + "\" = ?, \"" + KaeltemittelSchema.SPALTE_PFAS_STATUS + "\" = ?, \"" +
            KaeltemittelSchema.SPALTE_STATUS + "\" = ?, \"" + KaeltemittelSchema.SPALTE_QUELLE + "\" = ?, \"" +
            KaeltemittelSchema.SPALTE_SORTIERUNG + "\" = ? WHERE \"ID\" = ? AND \"ReadOnly\" = 0";

        private static bool Zulang(string t) => t != null && t.Trim().Length > KaeltemittelSchema.TEXT_MAX;

        private static string Wertemenge(string wert, IReadOnlyList<string> menge, string feld)
        {
            if (string.IsNullOrEmpty(wert)) return null;
            foreach (string w in menge) if (string.Equals(w, wert, StringComparison.Ordinal)) return null;
            return Format(MyResource.Resource.KMT_MSG_WERTEMENGE, wert, feld);
        }

        private static DbParam Wert(string t) =>
            new DbParam("?", string.IsNullOrWhiteSpace(t) ? DBNull.Value : (object)t.Trim());

        private static string Format(string muster, params object[] werte) =>
            string.Format(CultureInfo.CurrentCulture, muster, werte);

        private static Kaeltemittelsatz Satz(DataRow r) => new Kaeltemittelsatz(
            Ganz(r["ID"]) ?? 0,
            Text(r[KaeltemittelSchema.SPALTE_CODE]) ?? "",
            Text(r[KaeltemittelSchema.SPALTE_BEZEICHNUNG]),
            Text(r[KaeltemittelSchema.SPALTE_GRUPPE]),
            r[KaeltemittelSchema.SPALTE_GWP] == DBNull.Value ? (double?)null
                : Convert.ToDouble(r[KaeltemittelSchema.SPALTE_GWP], CultureInfo.InvariantCulture),
            Text(r[KaeltemittelSchema.SPALTE_GWP_QUELLE]),
            Schalter(r[KaeltemittelSchema.SPALTE_F_GAS]),
            Schalter(r[KaeltemittelSchema.SPALTE_NATUERLICH]),
            Text(r[KaeltemittelSchema.SPALTE_SICHERHEITSGRUPPE]),
            Text(r[KaeltemittelSchema.SPALTE_PFAS_STATUS]),
            Text(r[KaeltemittelSchema.SPALTE_STATUS]),
            Text(r[KaeltemittelSchema.SPALTE_QUELLE]),
            Ganz(r[KaeltemittelSchema.SPALTE_SORTIERUNG]),
            Ganz(r["ReadOnly"]) == 1);

        private static string Text(object o) => o == null || o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);

        private static int? Ganz(object o) => o == null || o == DBNull.Value ? (int?)null : Convert.ToInt32(o, CultureInfo.InvariantCulture);

        private static bool? Schalter(object o) => Ganz(o) is int i ? i == 1 : (bool?)null;
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kältemitteltabelle</b> (<c>Tab_Kaeltemittel_STAMM</c>, Schritt <see cref="KaeltemittelSchema.SCHRITT"/>,
    /// Fachkonzept Kältemittel als Stammdatum 4.1): Liste, Satz zur ID und Code → ID. Nur lesend — Anlegen, Ändern und
    /// Löschen kommen mit der Verwaltung (KM4-E3). Alle Zugriffe über <see cref="DataRepository"/> mit <c>?</c>-Parametern;
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

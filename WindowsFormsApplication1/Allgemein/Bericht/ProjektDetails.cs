using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Lesend geladene Detail-Daten eines Projekts für Projektbeschreibung,
    /// Komponententabellen und Abweichungserkennung (Phase 2).
    /// Zugriff bewusst über tolerante SQL-Reads (Spalten je DB-Stand prüfbar),
    /// nicht über die Formular-Controller — Spaltennamen sind gegen das Schema
    /// von Kenndaten.accdb verifiziert (11.08.2026).
    /// </summary>
    public class ProjektDetails
    {
        /// <summary>Gewerk-Schlüssel → Eingabetabelle des Projekts.</summary>
        public static readonly KeyValuePair<string, string>[] GewerkTabellen = new KeyValuePair<string, string>[]
        {
            new KeyValuePair<string, string>("Wärmepumpe",    "Tab_WP"),
            new KeyValuePair<string, string>("BHKW",          "Tab_BHKW"),
            new KeyValuePair<string, string>("Spitzenkessel", "Tab_Heizkessel"),
            new KeyValuePair<string, string>("Solarthermie",  "Tab_Solarkollektoren"),
            new KeyValuePair<string, string>("Photovoltaik",  "Tab_PV"),
            new KeyValuePair<string, string>("Pufferspeicher","Tab_Pufferspeicher"),
            new KeyValuePair<string, string>("Stromspeicher", "Tab_Stromspeicher"),
        };

        public int IdProjekt;

        /// <summary>Bezeichner der Klimaregion des Projekts (Tab_Klimaregion, leer wenn keiner).</summary>
        public string KlimaregionName = "";

        /// <summary>Gebäude des Projekts (Tab_Gebaeude; null/leer möglich).</summary>
        public DataTable Gebaeude;

        /// <summary>Anlagenkonfiguration (Tab_Energieanlagen; null/leer möglich).</summary>
        public DataTable Anlagen;

        /// <summary>Gewerk → erste Komponentenzeile des Projekts (fehlt das Gewerk: kein Eintrag).</summary>
        public Dictionary<string, DataRow> Komponenten = new Dictionary<string, DataRow>();

        /// <summary>Gewerk → Anzahl der Komponenten-Einträge des Projekts.</summary>
        public Dictionary<string, int> KomponentenAnzahl = new Dictionary<string, int>();

        public bool HatGewerk(string gewerk)
        { return KomponentenAnzahl.ContainsKey(gewerk) && KomponentenAnzahl[gewerk] > 0; }

        public static ProjektDetails Lade(int idProjekt)
        {
            var d = new ProjektDetails { IdProjekt = idProjekt };

            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT TOP 1 Bezeichner FROM Tab_Klimaregion WHERE ID_Projekt = ?",
                    new OleDbParameter("@p", idProjekt));
                d.KlimaregionName = o as string ?? "";
            }
            catch { }

            d.Gebaeude = LadeTabelle("Tab_Gebaeude", idProjekt);
            d.Anlagen = LadeTabelle("Tab_Energieanlagen", idProjekt);

            foreach (KeyValuePair<string, string> g in GewerkTabellen)
            {
                DataTable dt = LadeTabelle(g.Value, idProjekt);
                int anzahl = dt != null ? dt.Rows.Count : 0;
                d.KomponentenAnzahl[g.Key] = anzahl;
                if (anzahl > 0) d.Komponenten[g.Key] = dt.Rows[0];
            }
            return d;
        }

        private static DataTable LadeTabelle(string tabelle, int idProjekt)
        {
            try
            {
                return DataRepository.GetDataTable(
                    "SELECT * FROM " + tabelle + " WHERE ID_Projekt = ? ORDER BY ID",
                    new OleDbParameter("@p", idProjekt));
            }
            catch { return null; }
        }

        // ------------------------------------------------------------- tolerante Zugriffe

        public static string S(DataRow r, string col)
        { return (r != null && r.Table.Columns.Contains(col) && r[col] != DBNull.Value) ? r[col].ToString() : ""; }

        public static double? D(DataRow r, string col)
        {
            if (r == null || !r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[col]); } catch { return null; }
        }

        public static bool? B(DataRow r, string col)
        {
            if (r == null || !r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return null;
            try { return Convert.ToBoolean(r[col]); } catch { return null; }
        }
    }
}

using System;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Dialogfreier Datenbankzugriff für die Bausteine aus Paket 2 (Konfigurations-UI).
    ///
    /// <see cref="DataRepository"/> zeigt im Fehlerfall eine MessageBox und liefert bei
    /// Fehlern leere Ergebnisse statt <c>null</c>. Für alles, was auch aus dem Engine-Pfad
    /// oder aus einem headless laufenden Prüfprogramm heraus aufgerufen werden kann,
    /// verlangt Konzept 13.4 Dialogfreiheit — ein hängender Referenzlauf ist sonst die
    /// Folge.
    ///
    /// Bestehende Klassen bringen denselben Vorlauf jeweils privat mit
    /// (<c>PufferSpCtrl.StillScalar</c>, <c>WaermequelleClass.SkalarStill</c>). Diese
    /// Klasse ist die gemeinsame Fassung für den NEUEN Code; die vorhandenen privaten
    /// Helfer bleiben bewusst unangetastet, damit Paket 2 keinen Rechenpfad anfasst.
    ///
    /// Alle Methoden schlucken Fehler und melden sie nur auf die Konsole.
    ///
    /// PROTOKOLLKANAL-NACHZUG, KATEGORIE (c): Das bleibt so. Die drei Meldungen sind
    /// generische Zugriffsdiagnosen ohne Anwenderaussage („StilleDb.Tabelle
    /// fehlgeschlagen: …"); die fachliche Folge meldet jeweils der AUFRUFER über
    /// <see cref="SimulationProtokoll"/> (z. B. „die Stufe rechnet ohne Module").
    /// Hinzu kommt, dass dieselben Methoden auch aus der Konfigurations-Oberfläche
    /// heraus laufen, also außerhalb jedes Simulationslaufs.
    /// </summary>
    internal static class StilleDb
    {
        /// <summary>Skalare Abfrage; <c>null</c> bei Fehler, fehlender Zeile oder NULL.</summary>
        public static object Scalar(string sql, params OleDbParameter[] parameter)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        if (parameter != null) cmd.Parameters.AddRange(parameter);
                        object v = cmd.ExecuteScalar();
                        return (v == DBNull.Value) ? null : v;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("StilleDb.Scalar fehlgeschlagen: " + ex.Message);
                return null;
            }
        }

        /// <summary>Tabellenabfrage; <c>null</c> bei Fehler (z. B. fehlende Spalte).</summary>
        public static DataTable Tabelle(string sql, params OleDbParameter[] parameter)
        {
            try
            {
                DataTable dt = new DataTable();
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        if (parameter != null) cmd.Parameters.AddRange(parameter);
                        using (OleDbDataAdapter ad = new OleDbDataAdapter(cmd))
                        {
                            ad.Fill(dt);
                        }
                    }
                }
                return dt;
            }
            catch (Exception ex)
            {
                Console.WriteLine("StilleDb.Tabelle fehlgeschlagen: " + ex.Message);
                return null;
            }
        }

        /// <summary>Schreibende Anweisung; Anzahl betroffener Zeilen, -1 bei Fehler.</summary>
        public static int NonQuery(string sql, params OleDbParameter[] parameter)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        if (parameter != null) cmd.Parameters.AddRange(parameter);
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("StilleDb.NonQuery fehlgeschlagen: " + ex.Message);
                return -1;
            }
        }

        /// <summary>Ganzzahl aus einem Datenbankwert; <paramref name="vorgabe"/> bei NULL/Unfug.</summary>
        public static int Zahl(object o, int vorgabe = 0)
        {
            if (o == null || o == DBNull.Value) return vorgabe;
            try { return Convert.ToInt32(o); }
            catch { return vorgabe; }
        }

        /// <summary>Kommazahl aus einem Datenbankwert; <paramref name="vorgabe"/> bei NULL/Unfug.</summary>
        public static double Kommazahl(object o, double vorgabe = 0)
        {
            if (o == null || o == DBNull.Value) return vorgabe;
            try { return Convert.ToDouble(o); }
            catch { return vorgabe; }
        }

        /// <summary>Text aus einem Datenbankwert; "" bei NULL.</summary>
        public static string Text(object o)
        {
            if (o == null || o == DBNull.Value) return "";
            return o.ToString();
        }

        /// <summary>Feldwert einer DataRow oder <c>null</c>, wenn die Spalte fehlt.</summary>
        public static object Feld(DataRow r, string spalte)
        {
            if (r == null || !r.Table.Columns.Contains(spalte)) return null;
            return r[spalte] == DBNull.Value ? null : r[spalte];
        }

        /// <summary>
        /// Parameter mit ausdrücklichem Typ — nötig überall dort, wo der Wert
        /// <see cref="DBNull"/> sein kann (aus DBNull allein leitet der OLE-DB-Provider
        /// keinen Spaltentyp ab). Gleiche Bauart wie <c>ProjektPuffer.Par</c>.
        /// </summary>
        public static OleDbParameter Par(string name, OleDbType typ, object wert)
        {
            return new OleDbParameter(name, typ) { Value = wert ?? DBNull.Value };
        }
    }
}

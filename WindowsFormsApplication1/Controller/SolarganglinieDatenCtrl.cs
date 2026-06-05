using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    public class SolarganglinieDatenModel
    {
        public int m_ID_GanglinieDaten { get; set; }
        public double m_Wert { get; set; }

        public SolarganglinieDatenModel()
        {
            m_ID_GanglinieDaten = 0;
            m_Wert = 0;
        }
    }

    class SolarganglinieDatenCtrl : SolarganglinieDatenModel
    {
        public List<SolarganglinieDatenModel> list_GanglinieDaten = new List<SolarganglinieDatenModel>();
        public int rows => list_GanglinieDaten.Count;
        public List<SolarganglinieDatenModel> items => list_GanglinieDaten;

        public SolarganglinieDatenCtrl()
        {
        }

        public bool Delete(string szName)
        {
            try
            {
                // Standardkonformes DELETE ohne "*" und typsichere Parameterübergabe
                string sql = "DELETE FROM Tab_SolarganglinieDaten WHERE ID_GanglinieDaten = ?";

                OleDbParameter paramId = new OleDbParameter("@idGang", OleDbType.Integer);
                paramId.Value = m_ID_GanglinieDaten;

                OleDbParameter[] ps = { paramId };

                return DataRepository.ExecuteSQL(sql, ps);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Delete: " + ex.Message);
                return false;
            }
        }

        public bool Insert()
        {
            if (list_GanglinieDaten == null || list_GanglinieDaten.Count == 0) return true;

            try
            {
                // Verbindung explizit öffnen, um Massendaten gebündelt zu verarbeiten
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();

                    // Die Transaktion bündelt alle Schreibvorgänge im RAM und schreibt sie erst am Ende auf die Platte
                    using (OleDbTransaction trans = conn.BeginTransaction())
                    {
                        using (OleDbCommand cmd = new OleDbCommand())
                        {
                            cmd.Connection = conn;
                            cmd.Transaction = trans;
                            cmd.CommandText = "INSERT INTO Tab_SolarganglinieDaten (ID_GanglinieDaten, Wert) VALUES (?, ?)";

                            // Parameter vorab mit expliziten OleDbTypes definieren (verhindert den Laufzeitfehler)
                            cmd.Parameters.Add("@id", OleDbType.Integer);
                            cmd.Parameters.Add("@wert", OleDbType.Double);

                            try
                            {
                                foreach (var item in list_GanglinieDaten)
                                {
                                    // In der Schleife werden hocheffizient nur die Werte ausgetauscht
                                    cmd.Parameters[0].Value = item.m_ID_GanglinieDaten;
                                    cmd.Parameters[1].Value = item.m_Wert;

                                    cmd.ExecuteNonQuery();
                                }

                                // Erst jetzt wird die Änderung physikalisch in der *.accdb gespeichert
                                trans.Commit();
                                return true;
                            }
                            catch (Exception ex)
                            {
                                // Bei einem Fehler in der Schleife (z.B. Verletzung von DB-Regeln) wird alles zurückgerollt
                                trans.Rollback();
                                Console.WriteLine("Fehler beim Massen-Insert in der Schleife: " + ex.Message);
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Verbindungsfehler bei Massen-Insert: " + ex.Message);
                return false;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    public class StromganglinieDatenModel
    {
        public int m_ID_GanglinieDaten;
        public double m_Wert;

        public StromganglinieDatenModel()
        {
            m_ID_GanglinieDaten = 0;
            m_Wert = 0;
        }
    }

    class StromganglinieDatenCtrl : StromganglinieDatenModel
    {
        OdbcCommand DBCommand;

        public List<StromganglinieDatenModel> list_GanglinieDaten = new List<StromganglinieDatenModel>();

        public StromganglinieDatenCtrl()
        {
            DBCommand = Program.DBConnection.CreateCommand();
        }

        ~StromganglinieDatenCtrl()
        {
            DBCommand.Dispose();
        }

        public bool Delete(string szName)
        {
            try
            {
                DBCommand.CommandText = "DELETE * FROM Tab_StromganglinieDaten where ID_GanglinieDaten= '" + m_ID_GanglinieDaten + "'";
                DBCommand.ExecuteNonQuery();
            }
            catch (OdbcException sqlEx)
            {
                // Fehler beim Datenbankzugriff abfangen
                Console.WriteLine("SQL Fehler: " + sqlEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                // Allgemeine Fehler abfangen
                Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
                return false;
            }
            return true;
        }

        public bool Insert()
        {
            if (list_GanglinieDaten == null || list_GanglinieDaten.Count == 0) return true;

            // Wir greifen direkt auf die Verbindung zu, um sie offen zu halten
            using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
            {
                conn.Open();
                // Eine Transaction bündelt alle Schreibvorgänge in einen einzigen Festplatten-Zugriff
                using (OleDbTransaction trans = conn.BeginTransaction())
                {
                    using (OleDbCommand cmd = new OleDbCommand())
                    {
                        cmd.Connection = conn;
                        cmd.Transaction = trans;
                        cmd.CommandText = "INSERT INTO Tab_StromganglinieDaten (ID_Ganglinie, Wert) VALUES (?, ?)";

                        // Parameter einmalig definieren
                        cmd.Parameters.Add("@id", OleDbType.Integer);
                        cmd.Parameters.Add("@wert", OleDbType.Double);

                        try
                        {
                            foreach (var item in list_GanglinieDaten)
                            {
                                // Nur die Werte der Parameter aktualisieren
                                cmd.Parameters[0].Value = item.m_ID_GanglinieDaten;
                                cmd.Parameters[1].Value = item.m_Wert;

                                cmd.ExecuteNonQuery();
                            }

                            // Erst jetzt wird alles physisch auf die Platte geschrieben
                            trans.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            Console.WriteLine("Fehler beim Massen-Insert: " + ex.Message);
                            return false;
                        }
                    }
                }
            }
        }


    }
}

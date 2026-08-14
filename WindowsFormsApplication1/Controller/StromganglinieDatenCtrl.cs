using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    public class StromganglinieDatenModel
    {
        public int m_ID_GanglinieDaten { get; set; }
        public double m_Wert { get; set; }

        public StromganglinieDatenModel()
        {
            m_ID_GanglinieDaten = 0;
            m_Wert = 0;
        }
    }

    class StromganglinieDatenCtrl : StromganglinieDatenModel
    {
        // Verwendung der bestehenden Liste aus der Altanwendung
        public List<StromganglinieDatenModel> list_GanglinieDaten = new List<StromganglinieDatenModel>();

        // Kompatibilitätseigenschaften für das einheitliche Listen-Schema
        public int rows => list_GanglinieDaten.Count;
        public List<StromganglinieDatenModel> items => list_GanglinieDaten;

        public StromganglinieDatenCtrl()
        {
            // Konstruktor bereinigt - Ressourcen werden lokal verwaltet
        }

        public bool Delete(string szName)
        {
            try
            {
                // Standardkonforme DELETE-Syntax ohne "*" und Typkorrektur über expliziten Parameter
                string sql = "DELETE FROM Tab_StromganglinieDaten WHERE ID_Ganglinie = ?";

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
                // Direktzugriff auf die Verbindung, um Massendaten (z.B. 8760 Stundenwerte) performant zu schreiben
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

                            // Parameter einmalig mit expliziten OleDbTypes initialisieren, um Laufzeitfehler zu verhindern
                            cmd.Parameters.Add("@id", OleDbType.Integer);
                            cmd.Parameters.Add("@wert", OleDbType.Double);

                            try
                            {
                                foreach (var item in list_GanglinieDaten)
                                {
                                    // Nur die Werte der Parameter aktualisieren (sehr schnell)
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

        public bool InsertKompletteGanglinie(StromganglinieCtrl kopfCtrl, List<string> roheWerte)
        {
            if (roheWerte == null || roheWerte.Count == 0) return true;

            try
            {
                // 1. Eine einzige, gemeinsame Verbindung für beide Tabellen öffnen
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();

                    // 2. Die gemeinsame Transaktion starten
                    using (OleDbTransaction trans = conn.BeginTransaction())
                    {
                        // ======================================================================
                        // VORGANG 1: Der Kopfdatensatz (Tab_Stromganglinie)
                        // ======================================================================

                        // Zuerst die ID ermitteln
                        int neueGanglinieID = 1;
                        using (OleDbCommand cmdCount = new OleDbCommand("SELECT COUNT(*) FROM Tab_Stromganglinie", conn, trans))
                        {
                            int count = Convert.ToInt32(cmdCount.ExecuteScalar() ?? 0);
                            if (count > 0)
                            {
                                using (OleDbCommand cmdMax = new OleDbCommand("SELECT MAX(ID) FROM Tab_Stromganglinie", conn, trans))
                                {
                                    neueGanglinieID = Convert.ToInt32(cmdMax.ExecuteScalar() ?? 0) + 1;
                                }
                            }
                        }

                        // ID an das Kopf-Objekt zurückgeben, damit die UI sie nach dem Erfolg kennt
                        kopfCtrl.m_ID_Ganglinie = neueGanglinieID;

                        // Kopfdatensatz über die geteilte Verbindung einfügen
                        string sqlKopf = "INSERT INTO Tab_Stromganglinie (ID, Bezeichner, Zeitinterval) VALUES (?, ?, ?)";
                        using (OleDbCommand cmdKopf = new OleDbCommand(sqlKopf, conn, trans))
                        {
                            cmdKopf.Parameters.Add("@id", OleDbType.Integer).Value = neueGanglinieID;
                            cmdKopf.Parameters.Add("@bez", OleDbType.VarWChar).Value = kopfCtrl.m_szBezeichner ?? (object)DBNull.Value;
                            cmdKopf.Parameters.Add("@interval", OleDbType.Integer).Value = kopfCtrl.m_Zeitinterval;

                            cmdKopf.ExecuteNonQuery();
                        }

                        // ======================================================================
                        // VORGANG 2: Die 8760 Datenpunkte (Tab_StromganglinieDaten)
                        // ======================================================================
                        string sqlDaten = "INSERT INTO Tab_StromganglinieDaten (ID_Ganglinie, Wert) VALUES (?, ?)";
                        using (OleDbCommand cmdDaten = new OleDbCommand(sqlDaten, conn, trans))
                        {
                            // Parameter vorbereiten
                            cmdDaten.Parameters.Add("@idGang", OleDbType.Integer);
                            cmdDaten.Parameters.Add("@wert", OleDbType.Double);

                            try
                            {
                                // Die Liste leeren und neu befüllen für interne Kompatibilität
                                this.list_GanglinieDaten.Clear();

                                foreach (string roherWert in roheWerte)
                                {
                                    // Konvertierung mit InvariantCulture fängt Punkt/Komma-Probleme ab
                                    double parsedWert = double.Parse(roherWert, System.Globalization.CultureInfo.InvariantCulture);

                                    // Werte den Parametern zuweisen
                                    cmdDaten.Parameters[0].Value = neueGanglinieID;
                                    cmdDaten.Parameters[1].Value = parsedWert;

                                    cmdDaten.ExecuteNonQuery();

                                    // Optional: Das lokale Modell befüllen, falls du die Liste 'list_GanglinieDaten' danach noch im Code brauchst
                                    StromganglinieDatenModel model = new StromganglinieDatenModel();
                                    model.m_ID_GanglinieDaten = neueGanglinieID;
                                    model.m_Wert = parsedWert;
                                    this.list_GanglinieDaten.Add(model);
                                }

                                // ======================================================================
                                // ERFOLG: Erst jetzt wird ALLES permanent in die Access-Datei geschrieben
                                // ======================================================================
                                trans.Commit();
                                return true;
                            }
                            catch (Exception ex)
                            {
                                // Wird aufgerufen, wenn beim double.Parse ODER beim Schleifen-Insert ein Fehler auftritt
                                trans.Rollback();
                                Console.WriteLine("Schleifen-Fehler. Transaktion zurückgerollt: " + ex.Message);
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Wird aufgerufen, falls die Verbindung fehlschlägt oder der Kopf-Insert abstürzt
                Console.WriteLine("Allgemeiner Transaktions-Fehler: " + ex.Message);
                return false;
            }
        }
    }
}
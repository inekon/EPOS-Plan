using System;
using System.Collections.Generic;

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
                string sql = "DELETE FROM Tab_SolarganglinieDaten WHERE ID_Ganglinie = ?";

                DbParam paramId = new DbParam("@idGang", DbParamTyp.Integer);
                paramId.Wert = m_ID_GanglinieDaten;

                DbParam[] ps = { paramId };

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
                // Der Vorgang bündelt alle Schreibvorgänge in EINER Transaktion und
                // schreibt sie erst am Ende auf die Platte
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    string sqlInsert = "INSERT INTO Tab_SolarganglinieDaten (ID_Ganglinie, Wert) VALUES (?, ?)";

                    try
                    {
                        foreach (var item in list_GanglinieDaten)
                        {
                            v.Ausfuehren(sqlInsert,
                                new DbParam("@id", DbParamTyp.Integer) { Wert = item.m_ID_GanglinieDaten },
                                new DbParam("@wert", DbParamTyp.Double) { Wert = item.m_Wert });
                        }

                        // Erst jetzt wird die Änderung physikalisch gespeichert
                        v.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // Bei einem Fehler in der Schleife (z.B. Verletzung von DB-Regeln) wird alles zurückgerollt
                        v.Rollback();
                        Console.WriteLine("Fehler beim Massen-Insert in der Schleife: " + ex.Message);
                        return false;
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
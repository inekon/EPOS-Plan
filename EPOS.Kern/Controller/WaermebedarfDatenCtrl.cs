using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    public class WaermebedarfDatenModel
    {
        public int m_ID_GanglinieDaten { get; set; }
        public double m_Wert { get; set; }

        public WaermebedarfDatenModel()
        {
            m_ID_GanglinieDaten = 0;
            m_Wert = 0;
        }
    }

    class WaermebedarfDatenCtrl : WaermebedarfDatenModel
    {
        // Verwendung der bestehenden Liste aus der Altanwendung
        public List<WaermebedarfDatenModel> list_GanglinieDaten = new List<WaermebedarfDatenModel>();

        // Kompatibilitätseigenschaften für das einheitliche Schema (falls benötigt)
        public int rows => list_GanglinieDaten.Count;
        public List<WaermebedarfDatenModel> items => list_GanglinieDaten;

        public WaermebedarfDatenCtrl()
        {
            // Konstruktor bereinigt - Command wird nun zentral vom DataRepository verwaltet
        }

        public bool Delete(string szName)
        {
            try
            {
                // Standardkonforme DELETE-Syntax ohne "*" und Typkorrektur über Parameter
                string sql = "DELETE FROM Tab_WaermebedarfDaten WHERE ID_Ganglinie = ?";
                DbParam[] ps = {
                    new DbParam("@idGang", m_ID_GanglinieDaten)
                };

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
            try
            {
                // Vorbereitung des parametrisierten SQL-Statements mit standardkonformer VALUES-Klausel
                string sql = "INSERT INTO Tab_WaermebedarfDaten (ID_Ganglinie, Wert) VALUES (?, ?)";

                // Schleife über die dynamische Liste unter Verwendung von foreach statt dem langsameren .ElementAt(i)
                foreach (var item in list_GanglinieDaten)
                {
                    DbParam[] ps = {
                        new DbParam("@idGang", item.m_ID_GanglinieDaten),
                        new DbParam("@wert", item.m_Wert) // Regelt Dezimaltrennzeichen (Punkt/Komma) automatisch fehlerfrei
                    };

                    bool success = DataRepository.ExecuteSQL(sql, ps);
                    if (!success)
                    {
                        // Falls ein einzelner Eintrag fehlschlägt, abbrechen oder loggen
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Insert: " + ex.Message);
                return false;
            }
            return true;
        }
    }
}

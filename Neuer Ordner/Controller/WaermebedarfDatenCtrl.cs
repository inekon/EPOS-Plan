using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.Odbc;
using System.Globalization;

namespace WindowsFormsApplication1
{
    public class WaermebedarfDatenModel
    {
        public int m_ID_GanglinieDaten;
        public double m_Wert;

        public WaermebedarfDatenModel()
        {
            m_ID_GanglinieDaten = 0;
            m_Wert = 0;
        }
    }

    class WaermebedarfDatenCtrl : WaermebedarfDatenModel
    {
        OdbcCommand DBCommand;

        public List<WaermebedarfDatenModel> list_GanglinieDaten = new List<WaermebedarfDatenModel>();

 

        public WaermebedarfDatenCtrl()
        {
            DBCommand = Program.DBConnection.CreateCommand();
        }

        ~WaermebedarfDatenCtrl()
        {
            DBCommand.Dispose();
        }

        public bool Delete(string szName)
        {
            try
            {
                DBCommand.CommandText = "DELETE * FROM Tab_WaermebedarfDaten where ID_GanglinieDaten= '" + m_ID_GanglinieDaten + "'";
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
            try {

                NumberFormatInfo formatInfo = new NumberFormatInfo();
                formatInfo.NumberDecimalSeparator = ","; // Komma als Dezimaltrennzeichen

                for (int i = 0; i < list_GanglinieDaten.Count(); i++)
                {
                    WaermebedarfDatenModel item = list_GanglinieDaten.ElementAt(i);
                    string sql = "INSERT INTO Tab_WaermebedarfDaten ( ID_GanglinieDaten, Wert) SELECT " + item.m_ID_GanglinieDaten +
                        " AS Ausdr1, " + item.m_Wert + " AS Ausdr2"; 
                    DBCommand.CommandText = sql;
                    DBCommand.ExecuteNonQuery();
                }
            }
            catch (OdbcException  sqlEx)
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

   
     }
}

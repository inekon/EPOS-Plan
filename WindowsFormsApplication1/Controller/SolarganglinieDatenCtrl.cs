using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.Odbc;
using System.Globalization;

namespace WindowsFormsApplication1
{
    public class SolarganglinieDatenModel
    {
        public int m_ID_GanglinieDaten;
        public double m_Wert;

        public SolarganglinieDatenModel()
        {
            m_ID_GanglinieDaten = 0;
            m_Wert = 0;
        }
    }

    class SolarganglinieDatenCtrl : SolarganglinieDatenModel
    {
        OdbcCommand DBCommand;

        public List<SolarganglinieDatenModel> list_GanglinieDaten = new List<SolarganglinieDatenModel>();

        public SolarganglinieDatenCtrl ()
        {
            DBCommand = Program.DBConnection.CreateCommand();
        }

        ~SolarganglinieDatenCtrl()
        {
            DBCommand.Dispose();
        }

        public bool Delete(string szName)
        {
            try
            {
                DBCommand.CommandText = "DELETE * FROM Tab_SolarganglinieDaten where ID_GanglinieDaten= '" + m_ID_GanglinieDaten + "'";
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
            try
            {
                for (int i = 0; i < list_GanglinieDaten.Count; i++)
                {
                    SolarganglinieDatenModel item = list_GanglinieDaten.ElementAt(i);

                    string sql = FormattableString.Invariant($@"
                        INSERT INTO Tab_SolarganglinieDaten (ID_GanglinieDaten, Wert) 
                        SELECT {item.m_ID_GanglinieDaten}, {item.m_Wert}");

                    DBCommand.CommandText = sql;
                    DBCommand.ExecuteNonQuery();
                }
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


    }
}

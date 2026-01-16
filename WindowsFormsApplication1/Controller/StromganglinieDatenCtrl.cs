using System;
using System.Collections.Generic;
using System.Data.Odbc;
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
            try
            {
                NumberFormatInfo formatInfo = new NumberFormatInfo();
                formatInfo.NumberDecimalSeparator = ","; // Komma als Dezimaltrennzeichen

                for (int i = 0; i < list_GanglinieDaten.Count; i++)
                {
                    StromganglinieDatenModel item = list_GanglinieDaten.ElementAt(i);
                    string sql = "INSERT INTO Tab_StromganglinieDaten ( ID_GanglinieDaten, Wert) SELECT " + item.m_ID_GanglinieDaten +
                        " AS Ausdr1, " + item.m_Wert.ToString(CultureInfo.CreateSpecificCulture("en-US")) + " AS Ausdr2";
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

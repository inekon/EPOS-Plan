using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.Odbc;
using System.Globalization;

namespace WindowsFormsApplication1
{
    class KlimadatenCtrl : KlimadatenModel
    {
        OdbcCommand DBCommand;
        public KlimadatenModel klimamodel = new KlimadatenModel();
        public List<double> list_Temperatur = new List<double>();
        public List<int> list_Tag = new List<int>();
        public string Klimazone;
  
        public KlimadatenCtrl()
        {
            Klimazone = "";
            m_ID_Klimaregion = 0;
            DBCommand = Program.DBConnection.CreateCommand();
        }

        ~KlimadatenCtrl()
        {
            DBCommand.Dispose();
        }

        public void ReadAll()
        {
            DBCommand.CommandText = "select * from Tab_Klimadaten order by ID_Klimadaten";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new KlimadatenModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                KlimadatenModel item = new KlimadatenModel();

                if (!DBReader.IsDBNull(0)) item.m_ID_Klimadaten = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_ID_Klimaregion = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.m_Sol_Nord = (double)DBReader.GetValue(2);
                if (!DBReader.IsDBNull(3)) item.m_Sol_Ost = (double)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) item.m_Sol_Sued = (double)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) item.m_Sol_West = (double)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) item.m_nTemperatur = (double)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) item.m_WE = (bool)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(8)) item.m_TagTyp_W = (double)DBReader.GetValue(8);
                if (!DBReader.IsDBNull(9)) item.m_TagTyp_NW = (double)DBReader.GetValue(9);

                items[rows] = item;
                item = null;
                rows += 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public void ReadAll(int ID_Klimaregion)
        {
            DBCommand.CommandText = "select * from Tab_Klimadaten where ID_Klimaregion=" + ID_Klimaregion + " order by ID_Klimadaten";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new KlimadatenModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                KlimadatenModel item = new KlimadatenModel();

                if (!DBReader.IsDBNull(0)) item.m_ID_Klimadaten = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_ID_Klimaregion = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.m_Sol_Nord = (double)DBReader.GetValue(2);
                if (!DBReader.IsDBNull(3)) item.m_Sol_Ost = (double)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) item.m_Sol_Sued = (double)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) item.m_Sol_West = (double)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) item.m_nTemperatur = (double)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) item.m_WE = (bool)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(8)) item.m_TagTyp_W = (double)DBReader.GetValue(8);
                if (!DBReader.IsDBNull(9)) item.m_TagTyp_NW = (double)DBReader.GetValue(9);

                list_Temperatur.Add(item.m_nTemperatur);
                list_Tag.Add(rows+1);
                items[rows] = item;
                item = null;
               
                rows += 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public void ReadSingle(string sql)
        {
            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            rows = 0;
            DBReader.Read();
            if (DBReader.HasRows)
            {
                if (!DBReader.IsDBNull(0)) m_ID_Klimadaten = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) m_ID_Klimaregion = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) m_Sol_Nord = (double)DBReader.GetValue(2);
                if (!DBReader.IsDBNull(3)) m_Sol_Ost = (double)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) m_Sol_Sued = (double)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) m_Sol_West = (double)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) m_nTemperatur = (double)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) m_WE = (bool)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(8)) m_TagTyp_W = (double)DBReader.GetValue(8);
                if (!DBReader.IsDBNull(9)) m_TagTyp_NW = (double)DBReader.GetValue(9);
   
                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public bool Delete(string szName)
        {
            try
            {
                DBCommand.CommandText = "DELETE * FROM Tab_Klimaregion where Name= '" + szName + "'";
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

        public bool Insert(int ID_Klimaregion, List<KlimadatenModel> list)
        {
       
            try
            {
                DBCommand.CommandText = "SELECT Count(*) FROM Tab_Klimadaten";
                OdbcDataReader DBReader = DBCommand.ExecuteReader();
                DBReader.Read();  
                int result = (int)DBReader.GetValue(0);
                DBReader.Close();

                if (result == 0) m_ID_Klimadaten = 1;
                else
                {
                    DBCommand.CommandText = "SELECT Max(ID_Klimadaten) AS Ausdr1 FROM Tab_Klimadaten";
                    DBReader = DBCommand.ExecuteReader();
                    DBReader.Read();
                    m_ID_Klimadaten = (int)DBReader.GetValue(0) + 1;
                    DBReader.Close();
                }

                NumberFormatInfo formatInfo = new NumberFormatInfo();
                formatInfo.NumberDecimalSeparator = "."; // Komma als Dezimaltrennzeichen
       

                for (int i = 0; i < list.Count(); i++)
                {
                    KlimadatenModel item = list.ElementAt(i);
                                   
                    DBCommand.CommandText = "INSERT INTO TAB_Klimadaten ( ID_Klimadaten, ID_Klimaregion, Sol_Nord, Sol_Ost," +
                        "Sol_Sued, Sol_West, Temperatur, WE, TagTyp_W, TagTyp_NW ) SELECT " + m_ID_Klimadaten +
                        " AS Ausdr1, " + ID_Klimaregion + " AS Ausdr2, " + item.m_Sol_Nord.ToString("N5", formatInfo) + " AS Ausdr3," +
                        item.m_Sol_Ost.ToString("N5", formatInfo) + " AS Ausdr4, " + item.m_Sol_Sued.ToString("N5", formatInfo) + " AS Ausdr5," +
                        item.m_Sol_West.ToString("N5", formatInfo) + " AS Ausdr6, " + item.m_nTemperatur.ToString("N5",formatInfo)  + " AS Ausdr7," +
                        item.m_WE.ToString() + " AS Ausdr8, " + item.m_TagTyp_W.ToString("N5", formatInfo) + " AS Ausdr9, " +
                        item.m_TagTyp_NW.ToString("N5", formatInfo) + " AS Ausdr10"; 
                    DBCommand.ExecuteNonQuery();
                    m_ID_Klimadaten += 1;
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

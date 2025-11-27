using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    class SolardatenCtrl : SolardatenModel
    {
        OdbcCommand DBCommand;
        public SolardatenModel Solarmodel = new SolardatenModel();
        public List<double> list_Temperatur = new List<double>();
        public List<int> list_Tag = new List<int>();
        public string Klimazone;
  
        public SolardatenCtrl ()
        {
            Klimazone = "";
            m_ID_Klimaregion = 0;
            DBCommand = Program.DBConnection.CreateCommand();
        }

        ~SolardatenCtrl ()
        {
            DBCommand.Dispose();
        }

        public void ReadAll()
        {
            DBCommand.CommandText = "select * from Tab_Solar order by ID";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new SolardatenModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                SolardatenModel item = new SolardatenModel();

                if (!DBReader.IsDBNull(0)) item.m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_ID_Klimaregion = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.Außen_Temp = (double)DBReader.GetValue(2);
            
                items[rows] = item;
                item = null;
                rows += 1;
            }
            DBReader.Close();
            DBReader.Dispose();
        }

        public void ReadAll(int ID_Klimaregion)
        {
            DBCommand.CommandText = "select * from Tab_Klimadaten where ID_Klimaregion=" + ID_Klimaregion + " order by ID_Klimadaten";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new SolardatenModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                SolardatenModel item = new SolardatenModel();


                if (!DBReader.IsDBNull(0)) item.m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_ID_Klimaregion = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.Außen_Temp = (double)DBReader.GetValue(2);

                list_Temperatur.Add(item.Außen_Temp);
                list_Tag.Add(rows+1);
                items[rows] = item;
                item = null;
               
                rows += 1;
            }
            DBReader.Close();
            DBReader.Dispose();
        }

        public bool Insert(int ID_Klimaregion, List<SolardatenModel> list)
        {
       
            try
            {
                DBCommand.CommandText = "SELECT Count(*) FROM Tab_Solar";
                OdbcDataReader DBReader = DBCommand.ExecuteReader();
                DBReader.Read();  
                int result = (int)DBReader.GetValue(0);
                DBReader.Close();

                if (result == 0) m_ID = 1;
                else
                {
                    DBCommand.CommandText = "SELECT Max(ID) AS Ausdr1 FROM Tab_Solar";
                    DBReader = DBCommand.ExecuteReader();
                    DBReader.Read();
                    m_ID = (int)DBReader.GetValue(0) + 1;
                    DBReader.Close();
                }

                NumberFormatInfo formatInfo = new NumberFormatInfo();
                formatInfo.NumberDecimalSeparator = "."; // Komma als Dezimaltrennzeichen
       

                for (int i = 0; i < list.Count(); i++)
                {
                    SolardatenModel item = list.ElementAt(i);
                                   
                    DBCommand.CommandText = "INSERT INTO Tab_Solar ( ID, ID_Klimaregion, Temperatur ) SELECT " + m_ID +
                        " AS Ausdr1, " + ID_Klimaregion + " AS Ausdr2, " + item.Außen_Temp.ToString("N5", formatInfo); 
                    DBCommand.ExecuteNonQuery();
                    m_ID += 1;
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
        public bool WritetDataTable(System.Data.DataTable dt, string szName)
        {
            DBCommand.CommandText = "SELECT Max(ID) AS Ausdr1 FROM Tab_Solar";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();
            DBReader.Read();
            int id = (int)DBReader.GetValue(0) + 1;
            DBReader.Close();

            RecordSet rs = new RecordSet();
            rs.Open("Select ID_Klimaregion from Tab_Klimaregion where Name='" + szName + "'");
            rs.Next();
            int id_ref = (int)rs.Read("ID_Klimaregion");
            rs.Close();

            OdbcDataAdapter adapter = new OdbcDataAdapter("select ID, ID_Klimaregion, Temperatur from Tab_Solar", Program.DBConnection);
            DataSet dataSet = new DataSet();
            OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);

            dt.Columns.Add("ID_Klimaregion", typeof(int)).SetOrdinal(0);
            dt.Columns.Add("ID", typeof(int)).SetOrdinal(0);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dataRow = dt.Rows[i];
                dataRow[0] = id++;
                dataRow[1] = id_ref;
            }
            dt.Columns[2].ColumnName = "Temperatur";

            dataSet.Tables.Add(dt);

            adapter.Update(dataSet, dataSet.Tables[0].TableName);
            return true;
        }


    }
}

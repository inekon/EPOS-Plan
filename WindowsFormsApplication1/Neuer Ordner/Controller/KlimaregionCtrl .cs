using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ListBox = System.Windows.Forms.ListBox;

namespace WindowsFormsApplication1
{
    class KlimaregionCtrl : KlimaregionModel 
    {
        OdbcCommand DBCommand;
        KlimaregionModel klimaregionmodel;
        private Microsoft.Office.Interop.Excel.Application oExcelApp = new Microsoft.Office.Interop.Excel.Application();
        private Workbook oWorkbook = null;
        private Worksheet oWorksheet = null;
        private List<double> list = new List<double>();
        private int rows;

        public KlimaregionCtrl()
        {
            DBCommand = Program.DBConnection.CreateCommand();
            klimaregionmodel = new KlimaregionModel();
            rows = 0;
        }

        ~KlimaregionCtrl()
        {
            DBCommand.Dispose();
        }

        public bool OpenExcel(string szSheet)
        {
            FileDlgClass dlg = new FileDlgClass();
            Range oRange;
            object oValue;

            string file = dlg.FileDlg();
            if (file != "")
            {
                System.Windows.Forms.Application.UseWaitCursor = true;
                Klimaregion = dlg.filebasename;
                Klimaregion =  System.IO.Path.GetFileNameWithoutExtension(dlg.filebasename);

                // Datei öffnen und aktives Blatt und benutzten Bereich auswählen
                oWorkbook = oExcelApp.Workbooks.Open(file);
                oWorksheet = (Worksheet)oWorkbook.Sheets[szSheet];
                oRange = oWorksheet.UsedRange;

                for (int j = 6; j <= 370; j++)
                {
                    oValue = (oRange.Cells[j + 1, 7] as Range).Value;
                    // muss unbedingt abgefangen werden
                    if (oValue != null) list.Add((double)oValue);
                }
                if (oWorkbook != null) oWorkbook.Close();
                System.Windows.Forms.Application.UseWaitCursor = false;
            }
            return true;
        }

        public bool WriteKlimaregion()
        {
            int ID = 0;
            String sql = "";

            try
            {
                DBCommand.CommandText = "SELECT Max(ID_Klimaregion) AS Ausdr1 FROM Tab_Klimaregion";
                OdbcDataReader DBReader = DBCommand.ExecuteReader();
                DBReader.Read();
                ID = (int)DBReader.GetValue(0) + 1;

                //DBCommand.CommandText = "DELETE * FROM Tab_Klimadaten";
                //DBCommand.ExecuteNonQuery();

                //DBCommand.CommandText = "DELETE * FROM Tab_Klimaregion";
                //DBCommand.ExecuteNonQuery();
                DBReader.Close();
                //DBReader = DBCommand.ExecuteReader();
                sql = "INSERT INTO TAB_Klimaregion ( ID_Klimaregion, Name ) SELECT " + ID + " AS Ausdr1, '" + Klimaregion + "'";
                DBCommand.CommandText = sql;
                DBCommand.ExecuteNonQuery();
                ID_Klimaregion = ID;
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

        public bool WriteKlimadaten()
        {
            int ID = 0;
            string temp = "";
            String sql = "";

            try {
                DBCommand.CommandText = "SELECT Max(ID_Klimadaten) AS Ausdr1 FROM Tab_Klimadaten";
                OdbcDataReader DBReader = DBCommand.ExecuteReader();
                DBReader.Read();
                ID = (int)DBReader.GetValue(0) + 1;
                DBReader.Close();

                //DBCommand.CommandText = "DELETE * FROM Tab_Klimadaten";
                //DBCommand.ExecuteNonQuery();

                for (int i = 0; i < list.Count(); i++)
                {
                    Temperatur = (double)list.ElementAt(i);
                    temp = Convert.ToString(Temperatur).Replace(",", ".");
                    sql = "INSERT INTO TAB_Klimadaten ( ID_Klimaregion, ID_Klimadaten, Temperatur ) SELECT " + ID_Klimaregion + " AS Ausdr1, " + ID + " AS Ausdr2, " + temp + " AS Ausdr3";
                    DBCommand.CommandText = sql;
                    DBCommand.ExecuteNonQuery();
                    ID += 1;
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
        public void ReadAllKlimaregion()
        {
            DBCommand.CommandText = "select * from Tab_Klimaregion order by Name";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            klimaregionmodel.items = new KlimaregionModel[100];
            rows = 0;
            while (DBReader.Read())
            {
                KlimaregionModel item = new KlimaregionModel();

                if (!DBReader.IsDBNull(0)) item.ID_Klimaregion = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.Klimaregion = DBReader.GetString(1);
                
                klimaregionmodel.items[rows] = item;
                item = null;
                rows += 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public void ReadSingleKlimaregion(string sql)
        {
            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            rows = 0;
            DBReader.Read();
            if (DBReader.HasRows)
            {
                if (!DBReader.IsDBNull(0)) ID_Klimaregion = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) Klimaregion = DBReader.GetString(1);
                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public void FillListBox(ListBox ctrl)
        {
            ctrl.Items.Clear();
            for (int i = 0; i < rows; i++)
            {
                ctrl.Items.Add(klimaregionmodel.items[i].Klimaregion);
            }
        }
        public void FillComboBox(ComboBox ctrl)
        {
            ctrl.Items.Clear();
            for (int i = 0; i < rows; i++)
            {
                ctrl.Items.Add(klimaregionmodel.items[i].Klimaregion);
            }
        }

    }
}

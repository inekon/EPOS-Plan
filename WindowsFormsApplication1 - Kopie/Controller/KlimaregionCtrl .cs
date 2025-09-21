using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    class KlimaregionCtrl : KlimaregionModel 
    {
        OdbcCommand DBCommand;
        private Microsoft.Office.Interop.Excel.Application oExcelApp = new Microsoft.Office.Interop.Excel.Application();
        private Workbook oWorkbook = null;
        private Worksheet oWorksheet = null;
        private List<double> list = new List<double>();

        public KlimaregionCtrl()
        {
            DBCommand = Program.DBConnection.CreateCommand();
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
            int ID = 1;
            String sql = "";

            try
            {
                DBCommand.CommandText = "DELETE * FROM Tab_Klimadaten";
                DBCommand.ExecuteNonQuery();

                DBCommand.CommandText = "DELETE * FROM Tab_Klimaregion";
                DBCommand.ExecuteNonQuery();
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
            int ID = 1;
            string temp = "";
            String sql = "";

            try {

                DBCommand.CommandText = "DELETE * FROM Tab_Klimadaten";
                DBCommand.ExecuteNonQuery();

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

    }
}

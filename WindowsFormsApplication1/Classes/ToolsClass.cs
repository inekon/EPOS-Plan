using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Documents;
using System.Windows.Forms;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace WindowsFormsApplication1
{
  
    public class ToolsClass
    {
        private Microsoft.Office.Interop.Excel.Application oExcelApp;
        private Microsoft.Office.Interop.Excel.Workbook oWorkbook = null;
        private Microsoft.Office.Interop.Excel.Worksheet oWorksheet = null;

        public List<KlimadatenModel> excelList = new List<KlimadatenModel>();
        public List<SolardatenModel> excelListSolar = new List<SolardatenModel>();
        public List<string> textList = new List<string>();

        public bool Exist(string szName)
        {
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Klimaregion where Name='" + szName +"'");
            if (!rs.EOF()) return true;
            return false;
        }

        public System.Data.DataTable ReadExcel(string file, string szSheet, int startZeile, int stopZeile, int startSpalte, int stopSpalte, ProgressBar pBar_Import)
        {
            System.Data.DataTable dt;

            try
            {
                if (!File.Exists(file)) { MessageBox.Show("Datei " + file + " existiert nicht!"); return null; }

                // Datei öffnen und aktives Blatt und benutzten Bereich auswählen
                oExcelApp = new Microsoft.Office.Interop.Excel.Application();

                oWorkbook = oExcelApp.Workbooks.Open(file);
                oWorksheet = (Worksheet)oWorkbook.Sheets[szSheet];
    
                dt = new System.Data.DataTable(oWorksheet.Name);
                DataColumn[] xco = new DataColumn[stopSpalte - startSpalte + 1];

                for (int c = startSpalte; c <= stopSpalte; c++)
                {
                    //var colName = oWorksheet.Cells[startZeile, c].Value;
                    xco[c - startSpalte] = new DataColumn(c.ToString(), typeof(Object));
                }

                dt.Columns.AddRange(xco);
                var headerOffset = startZeile; //have to skip header row
                var width = dt.Columns.Count;
                var depth = stopZeile - startZeile;
                for (var i = 0; i <= depth; i++)
                {
                    var row = dt.NewRow();
                    for (var j = 0; j < width; j++)
                    {
                        var currentValue = oWorksheet.Cells[i + headerOffset, j + startSpalte].Value;

                        //have to decrement b/c excel is 1 based and datatable is 0 based.
                        row[j] = currentValue == null ? null : currentValue.ToString();
                    }

                    dt.Rows.Add(row);
                    pBar_Import.Value += 1;
                    pBar_Import.Refresh();
                }

            }
            catch(SystemException ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
            oWorkbook.Close(false);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(oWorkbook);
            oWorkbook = null;
     
            oExcelApp.Quit();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(oExcelApp);
            oExcelApp = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            return dt;
        }

        public bool OpenText(string file)
        {
            char[] trennzeichen = { ',', ';' };
            if (file == "") return false;

            var textFile = File.ReadAllLines(file);
            
            for (int i = 0; i < textFile.Length; i++)
            {
                if(textFile[i].IndexOfAny(trennzeichen) > 0)
                {
                    MessageBox.Show("Format Fehler:\n" + file + "\nDatei überprüfen!\nWerte müssen zeilenorientiert sein ohne Trennzeichen ',' bzw. ';'");  
                    return false;
                }
            }

            textList = new List<string>(textFile);
            return true;
        }

        public void OpenFileWithDefaultApp(string filePath)
        {
            // Überprüfen Sie, ob die Datei existiert
            if (!System.IO.File.Exists(filePath))
            {
                // Fehlerbehandlung: Datei nicht gefunden
                return;
            }

            try
            {
                // Startet die Datei mit der Standardanwendung des Systems.
                Process.Start(filePath);
            }
            catch (Exception ex)
            {
                // Fehlerbehandlung für den Fall, dass kein Programm zum Öffnen der Datei vorhanden ist.
                Console.WriteLine("Fehler beim Öffnen der Datei: " + ex.Message);
            }
        }
    }
}

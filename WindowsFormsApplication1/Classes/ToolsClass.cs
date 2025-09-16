using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Office.Interop.Excel;
using System.IO;
using System.Diagnostics;

namespace WindowsFormsApplication1
{
  
    class ToolsClass
    {
        private Microsoft.Office.Interop.Excel.Application oExcelApp;
        private Microsoft.Office.Interop.Excel.Workbook oWorkbook = null;
        private Microsoft.Office.Interop.Excel.Worksheet oWorksheet = null;

        public List<KlimadatenModel> excelList = new List<KlimadatenModel>();
        public List<string> textList = new List<string>();

        public bool Exist(string szName)
        {
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Klimaregion where Name='" + szName +"'");
            if (!rs.EOF()) return true;
            return false;
        }

        public bool OpenExcel(string file, string szSheet, int startZeile, int stopZeile, int startSpalte, int stopSpalte)
        {
            Range oRange;
            object[] oValue;

            try
            {
                if (!File.Exists(file)) { MessageBox.Show("Datei " + file + " existiert nicht!"); return false; }  

                // Datei öffnen und aktives Blatt und benutzten Bereich auswählen
                oExcelApp = new Microsoft.Office.Interop.Excel.Application();

                oWorkbook = oExcelApp.Workbooks.Open(file);
                oWorksheet = (Worksheet)oWorkbook.Sheets[szSheet];
                oRange = oWorksheet.UsedRange;

                for (int n = startZeile; n <= stopZeile; n++)
                {
                    oValue = new object[stopSpalte - startSpalte + 1];
                    int i = 0;
                    for (int col = startSpalte-1; col < stopSpalte; col++)
                    {
                        oValue[i] = (oRange.Cells[n, col] as Range).Value;
                        i++;
                    }


                    // muss unbedingt abgefangen werden
                    if (oValue != null)
                    {
                        KlimadatenModel item = new KlimadatenModel();
                        item.m_Sol_Nord = (double)oValue[0];
                        item.m_Sol_Ost = (double)oValue[1];
                        item.m_Sol_Sued = (double)oValue[2];
                        item.m_Sol_West = (double)oValue[3];
                        item.m_nTemperatur = (double)oValue[4];
                        item.m_WE = (bool)oValue[5];
                        item.m_TagTyp_W = (double)oValue[6];
                        item.m_TagTyp_NW = (double)oValue[7];
                        excelList.Add(item);
                    }
                }
            }
            catch(SystemException ex)
            {
                MessageBox.Show(ex.Message); 
            }

                
            oWorkbook.Close(false);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(oWorkbook);
            oWorkbook = null;
     
            oExcelApp.Quit();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(oExcelApp);
            oExcelApp = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            return true;
        }

        public bool OpenText(string file)
        {
            char[] trennzeichen = { ',', ';' };
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

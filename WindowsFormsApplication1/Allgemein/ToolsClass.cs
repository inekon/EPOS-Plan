using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using File = System.IO.File;

namespace WindowsFormsApplication1
{
  
    public class ToolsClass
    {
        public List<string> textList = new List<string>();

        public bool Exist(string szName)
        {
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Klimaregion_STAMM where Name='" + szName +"'");
            if (!rs.EOF()) { rs.Close(); return true; }
            rs.Close();
            return false;
        }

        public bool OpenText(string file)
        {
            char[] trennzeichen = { ',', ';' };
            if (file == "") return false;

            var textFile = File.ReadAllLines(file);
            
            for (int i = 0; i < textFile.Length; i++)
            {
                char lastChar = textFile[i].Substring(textFile[i].Length - 1, 1)[0];
                if (lastChar.Equals(trennzeichen[0]) || lastChar.Equals(trennzeichen[1]))
                {
                    Dienste.Dialog.Meldung("Format Fehler:\n" + file + "\nDatei überprüfen!\nWerte müssen zeilenorientiert sein ohne Trennzeichen ',' bzw. ';' am Zeilenende");
                    return false;
                }
            }

            textList = new List<string>(textFile);
            return true;
        }

        public bool OpenFileWithDefaultApp(string filePath)
        {
            // Überprüfen Sie, ob die Datei existiert
            if (!System.IO.File.Exists(filePath))
            {
                // Fehlerbehandlung: Datei nicht gefunden
                return false;
            }

            try
            {
                // Startet die Datei mit der Standardanwendung des Systems.
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath)
                {
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                // Fehlerbehandlung für den Fall, dass kein Programm zum Öffnen der Datei vorhanden ist.
                Console.WriteLine("Fehler beim Öffnen der Datei: " + ex.Message);
                return false;
            }
        }
    }
}

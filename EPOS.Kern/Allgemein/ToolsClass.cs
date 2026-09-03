using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// <summary>
        /// Oeffnet eine Datei mit der im System hinterlegten Anwendung.
        /// <c>false</c>, wenn die Datei fehlt oder kein Programm dafuer da ist.
        ///
        /// <para>Der Weg dorthin liegt seit iU5 in <c>Dienste.Datei</c>: Die Pruefung auf
        /// Vorhandensein, <c>UseShellExecute = true</c> und die Fehlerzeile auf der
        /// Konsole sind unveraendert, sie stehen jetzt nur im Adapter.</para>
        /// </summary>
        public bool OpenFileWithDefaultApp(string filePath)
        {
            return Dienste.Datei.MitSystemOeffnen(filePath);
        }
    }
}

using NReco.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class Attrribute_psp
    {
        public string m_szName;
        public string m_szFirma;
        public string m_szBauart;
        public string m_szVerluste;
        public string m_szVolumen;
        public string m_szTyp;

        public Attrribute_psp()
        {
            m_szName = "";;
            m_szFirma = "";
            m_szBauart = "";
            m_szVerluste = "";
            m_szVolumen = ""; 
            m_szTyp = "";   
        }
    }

    public class PufferSpImport
    {
        public List<Attrribute_psp> _list = new List<Attrribute_psp>();

        public void Import(string filename)
        {
            DateTime a = DateTime.Now;

            // ANSI (Windows-1252) explizit: Encoding.Default waere unter .NET 8 UTF-8
            // und macht aus jedem Umlaut-Byte U+FFFD (siehe AnsiEncoding).
            TextReader sr = new StringReader(File.ReadAllText(filename, AnsiEncoding.Get()));
            var csvReader = new CsvReader(sr, ";");

            csvReader.BufferSize = 32768;

            string szFirma = "";
            bool bBeginn = false;
            bool bHeizungswasserSp = false;
            bool bHeizungSp = false; 
            string szVolumen = "";  
            string szVerluste = "";
            string szTyp = "";  

            Attrribute_psp temp = null;
            _list.Clear();

            while (csvReader.Read())
            {
                if (csvReader[0] == "700" && bBeginn && bHeizungswasserSp && bHeizungSp)
                {
                    // Ende
                    temp.m_szFirma = szFirma;
                    temp.m_szVolumen = szVolumen;
                    temp.m_szVerluste = szVerluste;
                    temp.m_szTyp = szTyp;   
                    _list.Add(temp);
                    bBeginn = false;
                    bHeizungSp = false;
                }

                if (csvReader[0] == "010")
                {
                    szFirma = csvReader[3];
                    bHeizungswasserSp = false;
                }
                else if (csvReader[0] == "100")
                {
                    if(csvReader[1] == "2") bHeizungswasserSp = true;
                }
                else if (csvReader[0] == "110")
                {
                    if (csvReader[1] != "1") bHeizungswasserSp = false;
                }
                else if (csvReader[0] == "700")
                {
                    temp = new Attrribute_psp();
                    temp.m_szName = csvReader[3];
                    bBeginn = true;
                }
                else if (csvReader[0] == "710.03")
                {
                    szVolumen = csvReader[2];
                    szVerluste = csvReader[17];
                    if (csvReader[23] == "1") szTyp = "Solarspeicher";
                    else if (csvReader[23] == "2") szTyp = "Pufferspeicher";
                    else if (csvReader[23] == "3") szTyp = "Kombispeicher";
                    else szTyp = "";
                    bHeizungSp = true;
                }
            }
          //  fileReader.Close();

            //string[] tokens = szDaten.Split(';');


            DateTime b = DateTime.Now;
            TimeSpan time;
            time = b - a;
            string g = String.Format("{0}.{1}", time.Seconds, time.Milliseconds.ToString().PadLeft(3, '0'));

        }
    }
}

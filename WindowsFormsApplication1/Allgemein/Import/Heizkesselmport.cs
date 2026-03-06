using NReco.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class Attrribute_hk
    {
        public string m_szName;
        public string m_szFirma;
        public string m_szBauart;
        public string m_szBrennstoff;
        public string m_szBrennstoffIndex;
        public string szBrennstoffart;
        public string m_szThLeistung;
        public string m_szWirkungsgrad;
        public string m_szVerluste;
        public string m_szCO;
        public string m_szCO2;
        public string m_szNOX;

        public Attrribute_hk()
        {
            m_szName = "";;
            m_szFirma = "";
            m_szBauart = "";
            m_szBrennstoff = "";
            m_szBrennstoffIndex = "";
            szBrennstoffart = "";
            m_szThLeistung = "";
            m_szVerluste = "";
            m_szWirkungsgrad = "";
            m_szCO = "";
            m_szCO2 = "";
            m_szNOX = "";
        }
    }

    public class HeizkesselImport
    {
        public List<Attrribute_hk> _list = new List<Attrribute_hk>();

        public void Import(string filename)
        {
            DateTime a = DateTime.Now;

            TextReader sr = new StringReader(File.ReadAllText(filename, Encoding.Default));
            var csvReader = new CsvReader(sr, ";");

            csvReader.BufferSize = 32768;

            string szFirma = "";
            string szBrennstoff = "";
            string szBrennstoffIndex = "";
            string szBrennstoffart = "";
            string szCO = "";
            string szCO2 = "";
            string szNOX = "";
            bool bBeginn = false;
   
            Attrribute_hk temp = null;
            _list.Clear();

            while (csvReader.Read())
            {
                if (csvReader[0] == "700" && bBeginn)
                {
                    // Ende
                    temp.m_szFirma = szFirma;
                    temp.m_szBrennstoff = szBrennstoff;
                    temp.m_szBrennstoffIndex = szBrennstoffIndex;
                    temp.m_szCO = szCO;
                    temp.m_szCO2 = szCO2;
                    temp.m_szNOX = szNOX;
                    _list.Add(temp);
                    szBrennstoff = "";
                    szBrennstoffIndex = "";
                    szBrennstoffart = "";
                    szCO = "";
                    szCO2 = "";
                    szNOX = "";
                    bBeginn = false;
                }

                if (csvReader[0] == "010")
                {
                    szFirma = csvReader[3];
                }
                else if (csvReader[0] == "100")
                {
  
                }
                else if (csvReader[0] == "710.05")
                {
                    szCO2 = csvReader[10];
                    szCO = csvReader[13];
                    szNOX = csvReader[12];
                }
                else if (csvReader[0] == "710.11")
                {
                    if (szBrennstoffIndex == "")
                    {
                        // noch nicht gesetzt
                        szBrennstoffart = csvReader[2];
                        szBrennstoffIndex = csvReader[3]; // _Aufstellung[Int32.Parse(csvReader[1])-1];
                        szBrennstoff = csvReader[4];
                        byte[] bytes = Encoding.ASCII.GetBytes(szBrennstoff);
                        string value = new ASCIIEncoding().GetString(bytes);
                    }
                }
                else if (csvReader[0] == "700")
                {
                    temp = new Attrribute_hk();
                    temp.m_szName = csvReader[4];
                    temp.m_szBauart = csvReader[14];
                    temp.m_szThLeistung = csvReader[5];
                    temp.m_szWirkungsgrad = csvReader[26];
                    temp.m_szVerluste = csvReader[28];
                    bBeginn = true;
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

using NReco.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
            m_szName = ""; ;
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

            // ANSI (Windows-1252) explizit: deterministisch fuer deutsche Umlaute (ae, oe, ue, ss),
            // unabhaengig von der System-Locale. (Encoding.Default waere locale-/runtime-abhaengig.)
            TextReader sr = new StringReader(File.ReadAllText(filename, AnsiEncoding.Get()));
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
                if (Col(csvReader, 0) == "700" && bBeginn)
                {
                    // Ende
                    temp.m_szFirma = szFirma;
                    temp.m_szBrennstoff = szBrennstoff;
                    temp.m_szBrennstoffIndex = szBrennstoffIndex;
                    temp.szBrennstoffart = szBrennstoffart;
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

                if (Col(csvReader, 0) == "010")
                {
                    szFirma = Col(csvReader, 3);
                }
                else if (Col(csvReader, 0) == "100")
                {

                }
                else if (Col(csvReader, 0) == "710.01")
                {
                    // Rueckfallquelle Wirkungsgrad: manche Kataloge (z. B. Vaillant icoVIT,
                    // Viessmann Vitocrossal) lassen Spalte 26 des 700er-Satzes leer und
                    // fuehren den Kesselwirkungsgrad nur im 710.01-Satz (Spalte 6, bei
                    // Nennleistung, in Prozent wie Spalte 26). Ohne Rueckfall setzte die
                    // Uebernahme beide Wirkungsgradfelder auf den Platzhalter 1. Es zaehlt
                    // die erste 710.01-Zeile des Blocks, die einen Wert liefert; ein in
                    // Spalte 26 vorhandener Wert bleibt unangetastet.
                    if (bBeginn && temp != null && temp.m_szWirkungsgrad == "")
                    {
                        temp.m_szWirkungsgrad = Col(csvReader, 6);
                    }
                }
                else if (Col(csvReader, 0) == "710.05")
                {
                    szCO2 = Col(csvReader, 10);
                    szCO = Col(csvReader, 13);
                    szNOX = Col(csvReader, 12);
                }
                else if (Col(csvReader, 0) == "710.11")
                {
                    if (szBrennstoffIndex == "")
                    {
                        // noch nicht gesetzt
                        szBrennstoffart = Col(csvReader, 2);
                        szBrennstoffIndex = Col(csvReader, 3); // _Aufstellung[Int32.Parse(Col(csvReader, 1))-1];
                        szBrennstoff = Col(csvReader, 4);
                        // Hinweis: frueherer ASCII-Roundtrip entfernt - er haette Umlaute in "?" verwandelt.
                    }
                }
                else if (Col(csvReader, 0) == "700")
                {
                    temp = new Attrribute_hk();
                    temp.m_szName = Col(csvReader, 4);
                    temp.m_szBauart = Col(csvReader, 14);
                    temp.m_szThLeistung = Col(csvReader, 5);
                    temp.m_szWirkungsgrad = Col(csvReader, 26);
                    temp.m_szVerluste = Col(csvReader, 28);
                    bBeginn = true;
                }
            }

            // Letzten offenen Datensatz uebernehmen: das in-loop-Finalisieren passiert erst beim
            // naechsten "700"; ohne diesen Block fiele der LETZTE Heizkessel der Datei weg.
            if (bBeginn && temp != null)
            {
                temp.m_szFirma = szFirma;
                temp.m_szBrennstoff = szBrennstoff;
                temp.m_szBrennstoffIndex = szBrennstoffIndex;
                temp.szBrennstoffart = szBrennstoffart;
                temp.m_szCO = szCO;
                temp.m_szCO2 = szCO2;
                temp.m_szNOX = szNOX;
                _list.Add(temp);
                bBeginn = false;
            }
            //  fileReader.Close();

            //string[] tokens = szDaten.Split(';');


            DateTime b = DateTime.Now;
            TimeSpan time;
            time = b - a;
            string g = String.Format("{0}.{1}", time.Seconds, time.Milliseconds.ToString().PadLeft(3, '0'));

        }

        // Sicherer Feldzugriff: leerer String statt IndexOutOfRange, falls die Zeile
        // weniger Spalten hat als erwartet.
        private static string Col(CsvReader r, int i)
        {
            return (i >= 0 && i < r.FieldsCount) ? r[i] : "";
        }
    }
}



using NReco.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class _attrribute
    {
        public string szName;
        public string szFirma;
        public string szAufstellung;
        public string szWPTyp;
        public string szElektrZuheizung;
        public string szThLeistung;
        public string szStufen;
        public string szMaxVorlauf;
        public string szCOP;
        public string szKuehlung;
        public string szKuehlleistung;
        public List<string> x;
  
        public _attrribute()
        {
            szName = "";;
            szFirma = "";
            szAufstellung = "";
            szWPTyp = "";
            szElektrZuheizung = "";
            szThLeistung = "";
            szStufen = ""; ;
            szMaxVorlauf = "";
            szCOP = "";
            szKuehlung = "";
            szKuehlleistung = "";
            x = new List<string>();
        }

    }

    public class WaermepumpenImport
    {
        public List<_attrribute> _list = new List<_attrribute>();
        static string[] _Aufstellung = { "innen", "außen", "innen/außen", "innen Eck" };


        public void Import(string filename)
        {
            DateTime a = DateTime.Now;

            TextReader fileReader = File.OpenText(filename);

            var csvReader = new CsvReader(fileReader, ";");
            csvReader.BufferSize = 32768;

            bool startTagWPWaermeDef = false;
            bool DatenGefunden = false;
            bool DatenUebernehmen = false;

 
            string szDaten = "";
            string szFirma = "";
            string szAufstellung = "";
            string wptyp = "";
            int stufen = 0;

            _attrribute dat = null;
            _attrribute temp = null;
            _list.Clear();

            while (csvReader.Read())
            {
                if (csvReader[0] == "010")
                {
                    szFirma = csvReader[3];
                    startTagWPWaermeDef = false;
                    DatenGefunden = false;
                }
                else if (csvReader[0] == "100" && csvReader[1] != "2")
                {
                    startTagWPWaermeDef = true;
                    DatenGefunden = false;
                }
                else if (csvReader[0] == "110")
                {
                    wptyp = csvReader[3];
                }
                else if (startTagWPWaermeDef)
                {
                    if (csvReader[0] == "450")
                    {
                        szAufstellung = _Aufstellung[Int32.Parse(csvReader[1])-1];
                        DatenGefunden = false;
                    }
                    else if (csvReader[0] == "700")
                    {
                        temp = new _attrribute();
                        temp.x.Clear();
                        temp.szFirma = szFirma;
                        temp.szWPTyp = wptyp;
                        temp.szAufstellung = szAufstellung;
                        temp.szName = csvReader[3];
                        temp.szElektrZuheizung = csvReader[7];
                        temp.szThLeistung = csvReader[4];
                        temp.szStufen = csvReader[22];
                        if (temp.szStufen != "") stufen = Int32.Parse(temp.szStufen);
                        else stufen = 0;
                        temp.szMaxVorlauf = csvReader[27];
                        temp.szCOP = csvReader[31];
                        temp.szKuehlung = csvReader[19];
                        temp.szKuehlleistung = csvReader[20];

                        DatenGefunden = false;
                        DatenUebernehmen = false;
                        dat = new _attrribute();
                        dat.x.Clear();  
                    }

                    else if (csvReader[0] == "710.09") // nur Heizungs WP
                    {
                        szDaten = "";
                        for (int k = 0; k < csvReader.FieldsCount; k++)
                        {
                            szDaten = szDaten + csvReader[k] + ";";
                        }
                        if (csvReader[2] == "1")
                        {
                            // Heizung
                            if (checkDaten(csvReader,stufen))
                            {
                                temp.x.Add(szDaten);
                                DatenGefunden = true;
                                DatenUebernehmen = true;
                            }
                            else DatenUebernehmen = false; // bei modulierenden bzw. mit mehereren Regelstufen
                            // wir lesen nur Max bzw. 100% ein
                        }
                        if (csvReader[2] == "2")
                        {
                            temp.x.Add(szDaten);
                            //DatenGefunden = true;
                            DatenUebernehmen = true;
                        }
 
                    }
                    else if (csvReader[0] == "710.91" && DatenGefunden) // nur Heizungs WP
                    {
                        szDaten = "";
                        for (int k = 0; k < csvReader.FieldsCount; k++)
                        {
                            szDaten = szDaten + csvReader[k] + ";";
                        }

                        if(DatenUebernehmen)
                            temp.x.Add(szDaten);
                    }
                    else if (DatenGefunden && csvReader[0] != "710.09" && csvReader[0] != "710.91")
                    {
                        dat.szName = temp.szName;
                        dat.szFirma = temp.szFirma;
                        dat.szAufstellung = temp.szAufstellung;
                        dat.szElektrZuheizung = temp.szElektrZuheizung;
                        dat.szThLeistung = temp.szThLeistung;
                        dat.szWPTyp = temp.szWPTyp;
                        dat.szStufen = temp.szStufen;
                        dat.szMaxVorlauf = temp.szMaxVorlauf;
                        dat.szCOP = temp.szCOP;
                        dat.szKuehlleistung = temp.szKuehlleistung; 
                        dat.x = temp.x;
                        _list.Add(dat);

                        DatenGefunden = false;
                    }
                }
            }
            fileReader.Close();

            //string[] tokens = szDaten.Split(';');


            DateTime b = DateTime.Now;
            TimeSpan time;
            time = b - a;
            string g = String.Format("{0}.{1}", time.Seconds, time.Milliseconds.ToString().PadLeft(3, '0'));

        }

        private bool checkDaten(CsvReader csvReader, int stufen)
        {
            bool checkDatenZeile = false;
            int n = csvReader.FieldsCount-1;

            if (n < 8)
            {
                // keine Lastangabe, keine modulierende WP => Daten übernehmen
                // nur Daten für die höchste Stufe einlesen
                if (n > 6)
                {
                   // if (csvReader[6] == "1") checkDatenZeile = true;
                    if (csvReader[6] == stufen.ToString() )
                        checkDatenZeile = true;
                    else
                        checkDatenZeile = false;
                }
                else
                    checkDatenZeile = true;
            }
            else
            {
                string temp = "";
                if (csvReader[7] != "")
                {
                    // mit Lastangabe
                    temp = csvReader[7].ToUpper();
                    if (temp.Length >= 3)
                    {
                        temp = temp.Substring(0, 3);
                        if (temp == "100")
                            checkDatenZeile = true;
                        else if (temp == "MAX")
                            checkDatenZeile = true;
                        else
                        {
                            // weitere Daten bei mod. WP mit Lastangabe=> nicht einlesen
                            checkDatenZeile = false;
                        }
                    }
                    else
                    {
                        // weitere Daten (z.B. 80, also 80%) bei modulierenden WP mit Lastangabe
                        // => Daten bisher zumindest, nicht einlesen
                        checkDatenZeile = false;
                    }
                }
                else
                {
                    // Lastangabe Leerstring => Daten übernehmen
                    checkDatenZeile = true;
                }
            }

            return checkDatenZeile;

        }


    }
}

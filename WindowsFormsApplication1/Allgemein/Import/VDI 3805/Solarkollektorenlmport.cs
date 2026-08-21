using NReco.Csv;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
 
    public class Attrribute_st
    {
        public string m_szName;
        public string m_szFirma;
        public string m_szBeschreibung;
        public string m_szBauart;
        public double m_Aperturfläche;
        public double m_Modulfläche;
        public double m_Leistung;   // Blatt 19 liefert keine Nennleistungsangabe - bleibt 0 (nur Anzeige im Einlesedialog)
        public double m_h0;
        public double m_a1;
        public double m_a2;
        public double m_kdiff;
        public double m_kdir;

        public Attrribute_st()
        {
            m_szName = "";;
            m_szFirma = "";
            m_szBauart = "";
            m_szBeschreibung = "";
            m_Aperturfläche = 0.0;
            m_Modulfläche = 0.0;
            m_Leistung = 0.0;
            m_h0 = 0.0;
            m_a1 = 0.0;
            m_a2 = 0.0;
            m_kdiff = 0.0;
            m_kdir = 0.0;
        }
    }

    public class Solarkollektorenlmport
    {
        public List<Attrribute_st> _list = new List<Attrribute_st>();

        public void Import(string filename)
        {
            DateTime a = DateTime.Now;

            // ANSI (Windows-1252) explizit: Encoding.Default waere unter .NET 8 UTF-8
            // und macht aus jedem Umlaut-Byte U+FFFD (siehe AnsiEncoding).
            TextReader sr = new StringReader(File.ReadAllText(filename, AnsiEncoding.Get()));
            var csvReader = new CsvReader(sr, ";");

            csvReader.BufferSize = 32768;

            string szFirma = "";
            string szBauart = "";
            string szBeschreibung = "";
            double Aperturfläche = 0.0;
            double Modulfläche = 0.0;
            double h0 = 0.0;
            double a1 = 0.0;
            double a2 = 0.0;
            double kdiff = 0.0; // bleibt 0 - Blatt 19 (2006-02) führt keinen Diffus-IAM; Kdfu ist im Katalogdialog nachpflegbar
            double kdir = 0.0;
            bool bBeginn = false;

            Attrribute_st temp = null;
            _list.Clear();

            while (csvReader.Read())
            {
  
                if (csvReader[0] == "700" && bBeginn)
                {
                    // Ende des vorigen Kollektorblocks: dessen 710.01-Kennwerte sind
                    // erst beim Folge-700er vollständig gelesen (Satzfolge: 700, dann
                    // 710.01 ...). Muster wie im HeizkesselImport.
                    temp.m_szFirma = szFirma;
                    temp.m_szBeschreibung = szBeschreibung;
                    temp.m_Aperturfläche = Aperturfläche;
                    temp.m_Modulfläche = Modulfläche;
                    temp.m_h0 = h0;
                    temp.m_a1 = a1;
                    temp.m_a2 = a2;
                    temp.m_kdiff = kdiff;
                    temp.m_kdir = kdir;
                    temp.m_szBauart = szBauart;
                    _list.Add(temp);

                    // Akkumulatoren zurücksetzen, damit ein Block ohne eigenen
                    // 710.01-Satz keine Werte des Vorgängers erbt.
                    szBauart = "";
                    szBeschreibung = "";
                    Aperturfläche = 0.0;
                    Modulfläche = 0.0;
                    h0 = 0.0;
                    a1 = 0.0;
                    a2 = 0.0;
                    kdir = 0.0;
                    bBeginn = false;
                }

                if (csvReader[0] == "010")
                {
                    szFirma = csvReader[3];
                }
                else if (csvReader[0] == "100")
                {

                }
                else if (csvReader[0] == "710.01")
                {
                    int typ = int.Parse(csvReader[8]);
                    
                    if (typ == 1) szBauart = "Flachkollektor";
                    else if (typ == 2) szBauart = "Röhrenkollektor";
                    else if (typ == 3) szBauart = "Schwimmbadabsorber";
                    else szBauart = "Sonderkonstruktion";
                    
                    szBeschreibung = csvReader[9];
                    
                    // Feld 11 ist die Bezugsfläche, auf die sich h0/a1/a2 beziehen
                    // (Solar Keymark, meist die Apertur) - sie gehört als A_ref in
                    // die Ertragsrechnung. Feld 26 (Aperturfläche) nur als Rückfall,
                    // wenn keine Bezugsfläche angegeben ist.
                    Aperturfläche = ParseDouble(csvReader[11]);
                    if (Aperturfläche == 0.0) Aperturfläche = ParseDouble(csvReader[26]);

                    // Feld 25 ist die Brutto-Kollektorfläche (Rahmenaußenmaß) für
                    // Dachbelegung/Flächenbilanz - nicht die Kennlinien-Bezugsfläche.
                    Modulfläche = ParseDouble(csvReader[25]);

                    h0 = ParseDouble(csvReader[12]);
                    a1 = ParseDouble(csvReader[13]);
                    a2 = ParseDouble(csvReader[14]);
                    kdir = ParseDouble(csvReader[15]);
                }
                else if (csvReader[0] == "700")
                {
                    // Anfang eines Kollektorblocks
                    temp = new Attrribute_st();
                    temp.m_szName = csvReader[3];
                    bBeginn = true;
                }

            }

            // Letzten offenen Block übernehmen: das Finalisieren in der Schleife
            // passiert erst beim nächsten "700" - ohne diesen Block fiele der letzte
            // Kollektor der Datei weg (wie im HeizkesselImport).
            if (bBeginn && temp != null)
            {
                temp.m_szFirma = szFirma;
                temp.m_szBeschreibung = szBeschreibung;
                temp.m_Aperturfläche = Aperturfläche;
                temp.m_Modulfläche = Modulfläche;
                temp.m_h0 = h0;
                temp.m_a1 = a1;
                temp.m_a2 = a2;
                temp.m_kdiff = kdiff;
                temp.m_kdir = kdir;
                temp.m_szBauart = szBauart;
                _list.Add(temp);
                bBeginn = false;
            }
        } 

        private static double ParseDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            // Ersetzt Komma durch Punkt für universelles Parsing.
            // NumberStyles.Float (statt Any): ohne AllowThousands, damit ein
            // Gruppenzeichen nie still als Tausendertrennzeichen durchrutscht.
            double.TryParse(value.Replace(',', '.'), System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out double result);
            return result;
        }

    }

}

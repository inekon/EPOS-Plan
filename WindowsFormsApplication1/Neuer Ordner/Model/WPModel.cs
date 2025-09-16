using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    
    public class WPModel
    {
        public WPModel[] items;
        public int ID;
        public string WPName;
        public string Firma;
        public string Beschreibung;
        public string Typ;
        public int Baujahr;
        public string Aufstellung;
        public int Nennleistung;
        public int maxPTherm;
        public int Heizung;
        public string Regelung;
        public int Modulkosten;
        public string Leistungsstufen;
        
        public WPModel()
        {
            items = null;
            ID = 0;
            WPName = "";
            Firma = "";
            Beschreibung = "";
            Typ = "";
            Baujahr = 2000;
            Aufstellung = "";
            Nennleistung = 0;
            maxPTherm = 0;
            Heizung = 0;
            Regelung = "";
            Modulkosten = 0;
            Leistungsstufen = "";
        } 
    }

}

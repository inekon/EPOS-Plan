using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    
    public class WErzeugerModel : WPModel
    {
        public WErzeugerModel[] items;
        public int ID;
        public int ID_Projekt;
        public string Bezeichner;
        public int ID_Type;
        public int ID_WP;
        public string Betriebsart;
        public bool Sperrung;
        public int Sperrzeit_von;
        public int Sperrzeit_bis;
        public int Vorlauf;
        public int Ruecklauf;
        public bool Bivalenter_Betrieb;
        public double Abschaltpunkt;
        public int Nutzungszeit;
        public int ID_SP;
        public int ID_PV;
        public int ID_Solar;
        public bool Heizstab;
        public double Volumen;
        public bool rendeMix;
        public int Solaranteil;
        public int ID_Kessel;
        public int ID_BHKW;
        public double Grenzleistung;
        public double Kollektorneigung;
        public int Kollektorausrichtung;
        public int Kollektormodulanzahl;
        public double PV_Leistung;
        public int m_Neigung;
        public int m_Azimut;
        public int ID_PUFFER;

        public WErzeugerModel()
        {
            items = null;
            ID = 0;
            ID_Projekt = 0;
            Betriebsart = "";;
            Sperrung = false;
            Sperrzeit_von = 0;
            Sperrzeit_bis = 0;
            Vorlauf = 0;
            Ruecklauf = 0;
            Bivalenter_Betrieb= false;
            Abschaltpunkt = 0;
            Nutzungszeit = 0;
            ID_SP = 0;
            ID_PV = 0;
            ID_Solar = 0;
            Heizstab = false;
            Volumen = 0.0;
            rendeMix = false;
            Solaranteil = 0;
            ID_Kessel = 0;
            ID_BHKW = 0;
            Grenzleistung = 0;
            Kollektorneigung = 0;
            Kollektorausrichtung = 0;
            Kollektormodulanzahl = 0;
            PV_Leistung = 0.0;
            m_Neigung = 0;
            m_Azimut = 0;
            ID_PUFFER = 0;
        } 
    }

}

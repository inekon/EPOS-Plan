using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    class BrennstoffModel
    {
        public int ID;
        public string Name;
        public string Firma;
        public string Beschreibung;
        public double Ptherm;
        public int Brennstoff;
        public double Wirkungsgrad_Gas;
        public double Wirkungsgrad_Oel;
        public double Investitionskosten;
        public double Raumbedarf;
        public double Wartungskosten;
        public double Nutzungsdauer;
        public double CO2;
        public double SO2;
        public double NOx;
        public double CO;
        public double Staub;
        public double Betriebsbereitschaftverlust;

        public BrennstoffModel[] items;

        public BrennstoffModel()
        {
            items = null;
            ID = 0;
            Name = "";
            Firma = "";
            Beschreibung = "";
            Ptherm = 0.0;
            Brennstoff = 0;
            Wirkungsgrad_Gas = 0;
            Wirkungsgrad_Oel = 0;
            Investitionskosten = 0;
            Raumbedarf = 0;
            Wartungskosten = 0;
            Nutzungsdauer = 0;
            CO2 = 0;    
            SO2 = 0;    
            NOx = 0;    
            CO = 0;
            Staub = 0;
            Betriebsbereitschaftverlust = 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    class PufferSpModel
    {
        public int ID;
        public string Name;
        public string Firma;
        public string Speichertyp;
        public double Betriebsbereitschaftverlust;
        public int Gesamtvolumen;
        public double Investitionskosten;

        public PufferSpModel()
        {
            ID = 0;
            Name = "";
            Firma = "";
            Speichertyp = "";
            Betriebsbereitschaftverlust = 0;
            Gesamtvolumen = 0;
            Investitionskosten = 0;
        }
    }
}

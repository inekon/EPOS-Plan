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
        }
    }
}

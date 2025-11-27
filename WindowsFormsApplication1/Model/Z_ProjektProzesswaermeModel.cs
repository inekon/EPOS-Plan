using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class Z_ProjektProzesswaermeModel
    {
        public Z_ProjektProzesswaermeModel[] items;
        public int ID_Z;
        public int ID_Projekt;
        public int ID_Prozesswaerme;
        public string szProzessname;
        public double Summe;

        public Z_ProjektProzesswaermeModel()
        {
            items = null;
            ID_Z = 0;
            ID_Projekt = 0;
            ID_Prozesswaerme = 0;
            szProzessname = "";
            Summe = 0;
        }

    }
}

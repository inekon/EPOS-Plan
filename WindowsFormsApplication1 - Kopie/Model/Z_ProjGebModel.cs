using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class Z_ProjGebModel
    {
        public Z_ProjGebModel[] items;
        public int ID_Z;
        public int ID_Projekt;
        public int ID_Gebaeude;
        public string szGebaeude;
        public double Wohnflaeche;
        public string Einheit;
        public double Jahresnutzungsgrad;
        public bool DezentralWarmwasser;
        public string Baualtersklasse;
        public string Gebaeudename;
        public string Beschreibung;
        public string Gebaeudeart;

        public Z_ProjGebModel()
        {
            items = null;
            ID_Z = 0;
            ID_Projekt = 0;
            ID_Gebaeude = 0;
            szGebaeude = "";
            Wohnflaeche = 0.0;
            Einheit = "";
            Jahresnutzungsgrad = 0.0;
            DezentralWarmwasser = false;
        }
    }
}

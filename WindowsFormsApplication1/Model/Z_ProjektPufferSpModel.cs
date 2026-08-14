using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    public class Z_ProjektPufferSpModel
    {
        public int ID;
        public int ID_Projekt;
        public int ID_Pufferspeicher;
        public string Erzeuger;
        public string PufferSp;
        public int Vorlauf;
        public int Ruecklauf;
        public int Prioritaet;
        // B0-1: Schwellen der Speicherregelung [%]; null = nicht gesetzt (Defaults 10/95)
        public double? Schwelle_Ein;
        public double? Schwelle_Aus;
        public Z_ProjektPufferSpModel[] items;

        public Z_ProjektPufferSpModel()
        {
            ID = 0;
            ID_Projekt = 0;
            ID_Pufferspeicher = 0;
            Erzeuger = "";
            PufferSp = "";
            Vorlauf = 0;
            Ruecklauf = 0;
            Prioritaet = 0;
            Schwelle_Ein = null;
            Schwelle_Aus = null;
            items = null;
        }
    }
}

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
        public string Erzeuger;
        public string PufferSp;
        public int Vorlauf;
        public int Ruecklauf;
        public int Prioritaet;
        public Z_ProjektPufferSpModel[] items;

        public Z_ProjektPufferSpModel()
        {
            ID = 0;
            ID_Projekt = 0;
            Erzeuger = "";
            PufferSp = "";
            Vorlauf = 0;
            Ruecklauf = 0;
            Prioritaet = 0;
            items = null;
        }
    }
}

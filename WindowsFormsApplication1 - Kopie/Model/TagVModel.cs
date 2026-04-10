using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    class TagVModel
    {
        public TagVModel[] items;
        public int ID;
        public string Name;
        public string Beschreibung;
        public bool Veraenderbar;

        public TagVModel()
        {
           items = null;
           ID = 0;
           Name = "";
           Beschreibung = "";
           Veraenderbar = false;
        }
    }
 
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    
    class ProjektModel
    {
        public ProjektModel[] items;

        public int ID;
        public string Projektname;
        public string Bearbeiter;
        public string Beschreibung;
        public string Kunde;
        public DateTime Aenderung;

        public ProjektModel()
        {
            items = null;
            ID = 0;
            Projektname = "";
            Bearbeiter = "";
            Beschreibung = "";
            Kunde = "";
            Aenderung = DateTime.Now;
        }
 
    }

}

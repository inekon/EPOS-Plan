using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    
    class ApplikationModel
    {
        public int ID;
        public int ID_Projekt;
        public string Projektname;
        public string Beschreibung;

        public ApplikationModel()
        {
            ID = 0;
            ID_Projekt = 0;
            Projektname = "";
            Beschreibung = "";
        }
 
    }

}

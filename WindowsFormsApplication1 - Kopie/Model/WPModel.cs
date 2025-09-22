using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    
    class WPModel
    {
        public WPModel[] items;

        public int ID;
        public string WPName;
        public string Firma;
        public string Beschreibung;

        public WPModel()
        {
            items = null;
            ID = 0;
            WPName = "";
            Firma = "";
            Beschreibung = "";
        }
 
    }

}

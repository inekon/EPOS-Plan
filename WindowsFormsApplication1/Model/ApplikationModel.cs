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
        public int m_ID;
        public int m_ID_Projekt;
        public string m_szProjektname;
        public string m_szBeschreibung;
        public string m_icon;

        public ApplikationModel()
        {
            m_ID = 0;
            m_ID_Projekt = 0;
            m_szProjektname = "";
            m_szBeschreibung = "";
            m_icon = "";
        }
 
    }

}

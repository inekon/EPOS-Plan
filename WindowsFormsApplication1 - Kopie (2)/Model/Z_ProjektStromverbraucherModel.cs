using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    class Z_ProjektStromverbraucherModel
    {
        public Z_ProjektStromverbraucherModel[] items;
        public int m_ID_Z;
        public int m_ID_Projekt;
        public int m_ID_Stromverbraucher;
        public string m_szVerbraucher;

        public Z_ProjektStromverbraucherModel()
        {
            items = null;
            m_ID_Z = 0;
            m_ID_Projekt = 0;
            m_ID_Stromverbraucher = 0;
            m_szVerbraucher = "";
        }

    }
}

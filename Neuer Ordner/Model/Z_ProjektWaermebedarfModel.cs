using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    class Z_ProjWaermebedarfModel
    {
        public Z_ProjWaermebedarfModel[] items;
        public int m_ID_Z;
        public int m_ID_Projekt;
        public int m_ID_Ganglinie;
        public string m_szBezeichner;

        public Z_ProjWaermebedarfModel()
        {
            items = null;
            m_ID_Z = 0;
            m_ID_Projekt = 0;
            m_ID_Ganglinie = 0;
            m_szBezeichner = "";
        }
    }
}

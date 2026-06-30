using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class Z_ProjektStromganglinieModel
    {
        public Z_ProjektStromganglinieModel[] items;
        public int m_ID_Z;
        public int m_ID_Projekt;
        public int m_ID_Stromganglinie;
        public string m_szStromganglinie;

        public Z_ProjektStromganglinieModel()
        {
            items = null;
            m_ID_Z = 0;
            m_ID_Projekt = 0;
            m_ID_Stromganglinie = 0;
            m_szStromganglinie = "";
        }

    }
}

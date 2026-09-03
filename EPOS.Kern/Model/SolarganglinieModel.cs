using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    class SolarganglinieModel
    {
        public int ID;
        public int m_ID_Ganglinie;
        public string m_szBezeichner;
        public string m_szBeschreibung;
        public SolarganglinieModel[] items;

        public SolarganglinieModel()
        {
            ID = 0;
            m_ID_Ganglinie = 0;
            m_szBezeichner = "";
            m_szBeschreibung = "";
            items = null;
        }
    }
}

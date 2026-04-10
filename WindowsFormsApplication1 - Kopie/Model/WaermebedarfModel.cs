using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    class WaermebedarfModel
    {
        public int ID;
        public int m_ID_Ganglinie;
        public string m_szBezeichner;
        public WaermebedarfModel[] items;

        public WaermebedarfModel()
        {
            ID = 0;
            m_szBezeichner = "";
            m_ID_Ganglinie = 0;
            items = null;
        }
    }
}

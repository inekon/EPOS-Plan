using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    class WaermebedarfExternModel
    {
        public int ID;
        public int m_ID_Projekt;
        public string m_szDateiname;
        public WaermebedarfExternModel[] items;

        public WaermebedarfExternModel()
        {
            ID = 0;
            m_ID_Projekt = 0;
            m_szDateiname = "";
            items = null;
        }
    }
}

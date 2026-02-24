using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    class KlimaregionModel
    {
        public KlimaregionModel[] items;
        public int m_ID_Klimaregion;
        public string m_szName;
        public double Longitude;
        public double Latitude;
        public int rows;

        public KlimaregionModel()
        {
            items = null;
            m_ID_Klimaregion = 0;
            m_szName = "";
            Longitude = 0;
            Latitude = 0;    
            rows = 0;
        }


    }

 
}

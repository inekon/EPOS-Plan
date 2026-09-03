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
        public string Details;

        /// <summary>
        /// Klimazone 1…15 nach DIN 4710 (Bild A1 der VDI 4640 Blatt 2);
        /// 0 = nicht zugeordnet. Eingangsgröße der Auslegungsprüfung des
        /// Erdreichmodells (Konzept 13.1).
        /// </summary>
        public int Klimazone_DIN4710;

        public int rows;

        public KlimaregionModel()
        {
            items = null;
            m_ID_Klimaregion = 0;
            m_szName = "";
            Longitude = 0;
            Latitude = 0;
            Details = "";
            Klimazone_DIN4710 = 0;
            rows = 0;
        }
    }
}

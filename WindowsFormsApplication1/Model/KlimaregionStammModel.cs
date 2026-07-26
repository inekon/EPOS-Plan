using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // Stammdaten-Model als Spiegel der Tabelle Tab_Klimaregion_STAMM.
    // Die STAMM-Tabelle behaelt die alte Struktur: Schluessel = ID_Klimaregion, Namensfeld = Name.
    // Zusaetzlich das neue Feld ReadOnly (schreibgeschuetzte Stammdatensaetze).
    class KlimaregionStammModel
    {
        public int m_ID_Klimaregion;
        public string m_szName;
        public double Longitude;
        public double Latitude;
        public string Details;
        public bool m_bReadOnly;

        public KlimaregionStammModel()
        {
            m_ID_Klimaregion = 0;
            m_szName = "";
            Longitude = 0;
            Latitude = 0;
            Details = "";
            m_bReadOnly = false;
        }
    }
}

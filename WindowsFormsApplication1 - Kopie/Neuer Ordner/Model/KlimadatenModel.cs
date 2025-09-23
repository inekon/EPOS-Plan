using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    class KlimadatenModel
    {
        public KlimadatenModel[] items;
        public int m_ID_Klimaregion;
        public int m_ID_Klimadaten;
        public double m_Sol_Nord;
        public double m_Sol_Ost;
        public double m_Sol_Sued;
        public double m_Sol_West;
        public double m_nTemperatur;
        public bool m_WE;
        public double m_TagTyp_W;
        public double m_TagTyp_NW;

        public int rows;
              
        public KlimadatenModel()
        {
	        m_ID_Klimadaten = 0;
            m_ID_Klimaregion = 0;
            m_Sol_Nord = 0;
            m_Sol_Ost = 0;
            m_Sol_Sued = 0;
            m_Sol_West = 0;
            m_nTemperatur = 0;
            m_WE = false;
            m_TagTyp_W = 0;
            m_TagTyp_NW = 0;
            rows = 0;
        }
    }

}

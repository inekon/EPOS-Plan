using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class ProzesswaermeModel
    {
        public ProzesswaermeModel[] items;

        public int m_ID;
        public string m_szProzessname;
        public string m_szTyp;
        public string m_szBeschreibung;
        public double[] m_Monat; 

        public ProzesswaermeModel()
        {
            m_ID = 0;
            m_szProzessname = "";
            m_szTyp = "";
            m_szBeschreibung = "";
            m_Monat = new double[12];
        }

    }
}

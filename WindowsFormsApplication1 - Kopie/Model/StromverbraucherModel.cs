using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class StromverbraucherModel
    {
        public StromverbraucherModel[] items;

        public int m_ID;
        public string m_szBezeichner;
        public string m_szTyp;
        public string m_szBeschreibung;
        public double[] m_Monat; 

        public StromverbraucherModel()
        {
            m_ID = 0;
            m_szBezeichner= "";
            m_szTyp = "";
            m_szBeschreibung = "";
            m_Monat = new double[12];
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
 
    
    public class StromspeicherModel
    {
        public int rows;
        public StromspeicherModel[] items;
        public int m_ID;
        public string m_szBezeichner;
        public double m_Energie;
        public double m_Leistung;
        public double m_Degradation;
        public double m_Ladezustand;
        public string m_szTyp;
     
        public StromspeicherModel()
        {
            items = null;
            m_ID = 0;
            m_szBezeichner = string.Empty;
            m_Energie = 0;
            m_Leistung = 0;
            m_Degradation = 0.0;
            m_Ladezustand = 0;
            m_szTyp = string.Empty;
        }

    }
}

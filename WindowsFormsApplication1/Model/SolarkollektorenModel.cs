using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    public class SolarkollektorenModel
    {
        public int rows;
        public SolarkollektorenModel[] items;
        public int m_ID;
        public string m_szKollektorname;
        public string m_szFirma;
        public string m_szBeschreibung;
        public string m_szKollektortyp;
        public double m_Modulfläche;
        public double m_Aperturfläche;
        public double m_h0;
        public double m_k1;
        public double m_k2;
        public double m_C;
        public double m_Kdir;
        public double m_Kdfu;
        public double m_Ertrag;
        public double m_Kosten;
        public double m_Vorlauf;
        public double m_Ruecklauf;

        public SolarkollektorenModel()
        {
            rows = 0;
            items = null;
            m_szKollektorname = "";
            m_szFirma = "";
            m_szBeschreibung = "";
            m_szKollektortyp = "";
            m_Modulfläche = 0;
            m_Aperturfläche = 0;
            m_h0 = 0;
            m_k1 = 0;
            m_k2 = 0;
            m_C = 0;
            m_Kdir = 0;
            m_Kdfu = 0;
            m_Ertrag = 0;
            m_Kosten = 0;
            m_Vorlauf = 0;  
            m_Ruecklauf = 0;
        }
    }
}

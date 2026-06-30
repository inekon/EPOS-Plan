namespace WindowsFormsApplication1
{
    public class KlimadatenModel
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
        public double m_Globalstrahlung;

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
            m_Globalstrahlung = 0;  
        }
    }

    public class SolardatenModel
    {
        public SolardatenModel[] items;
        public int m_ID;
        public int m_ID_Klimaregion;
        public double Außen_Temp;
        public double Sol_Nord;
        public double Sol_Ost;
        public double Sol_Sued;
        public double Sol_West;
        public double Globalstrahlung;
        public double Direktstrahlung;
        public double Diffusstrahlung;
        public double Sonnenwinkel;

        public SolardatenModel()
        {
            m_ID = 0;
            m_ID_Klimaregion = 0;
            Außen_Temp = 0;
            Sol_Nord = 0;
            Sol_Ost = 0;
            Sol_Sued = 0;
            Sol_West = 0;
            Globalstrahlung= 0;
            Direktstrahlung = 0;
            Diffusstrahlung = 0;
            Sonnenwinkel = 0;
        }
    }
}

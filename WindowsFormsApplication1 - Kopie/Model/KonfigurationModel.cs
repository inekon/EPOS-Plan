namespace WindowsFormsApplication1
{
    public class KonfigurationModel
    {
        public int m_ID;
        public int m_ID_Projekt;
        public double m_Netzverluste;
        public string m_szNetzverlusteEinheit;
        public double m_BHKW_Grenzleistung;
        public bool m_WP_Heizstab;
        public int m_Kessel_Betriebsbereitschaft;
        public string m_Tool_1;
        public string m_Tool_2;
        public string m_Tool_3;
        public string m_Tool_4;
        public string m_Tool_5;
        public string m_Tool_6;
        public int m_Ladefuellstand_Min;
        public int m_Ladefuellstand_Max;
        public int m_Ladeleistung_Max;
        public double m_Ladeschwellwert;
        public string m_Ladefuellstand_Min_Auswahl;
        public string m_Ladefuellstand_Max_Auswahl;
        public string m_Ladeleistung_Max_Auswahl;

        public KonfigurationModel()
        {
            m_ID = 0;
            m_ID_Projekt = 0;
            m_Netzverluste = 0;
            m_szNetzverlusteEinheit = "";
            m_BHKW_Grenzleistung = 0;
            m_WP_Heizstab = false;
            m_Kessel_Betriebsbereitschaft = 0;
            m_Tool_1 = "";
            m_Tool_2 = "";
            m_Tool_3 = "";
            m_Tool_4 = "";
            m_Tool_5 = "";
            m_Tool_6 = "";
            m_Ladefuellstand_Min = 0;
            m_Ladefuellstand_Max = 0;
            m_Ladeleistung_Max =0;
            m_Ladeschwellwert = 0;
            m_Ladefuellstand_Min_Auswahl = "";
            m_Ladefuellstand_Max_Auswahl = "";
            m_Ladeleistung_Max_Auswahl = "";
        }
    }
}

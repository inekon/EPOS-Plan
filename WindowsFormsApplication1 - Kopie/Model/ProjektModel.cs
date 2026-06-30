using System;

namespace WindowsFormsApplication1
{
    
    public class ProjektModel
    {
        public int m_ID;
        public string m_szProjektname;
        public string m_szBearbeiter;
        public string m_szBeschreibung;
        public string m_szKunde;
        public DateTime m_Aenderungsdatum;
        public int m_ID_Klimaregion;
        public int rows;
        public DateTime m_Erstelldatum;
        public string m_szEinheit;
        public int m_nNetzverluste;

        public ProjektModel()
        {
            m_ID = 0;
            m_szProjektname = "";
            m_szBearbeiter = "";
            m_szBeschreibung = "";
            m_szKunde = "";
            m_Aenderungsdatum = DateTime.Now;
            m_ID_Klimaregion = 0;
            m_Erstelldatum = DateTime.Now;
            rows = 0;
        }
 
    }

}

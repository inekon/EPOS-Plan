using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    
    public class ProjektModel
    {
        public ProjektModel[] items;
        public int m_ID;
        public string m_szProjektname;
        public string m_szBearbeiter;
        public string m_szBeschreibung;
        public string m_szKunde;
        public DateTime m_Aenderungsdatum;
        public int m_ID_Klimaregion;
        public int m_ID_Gebaeude;
        public int rows;
        public DateTime m_Erstelldatum;
        public string m_szEinheit;
        public int m_nNetzverluste;

        public ProjektModel()
        {
            items = null;
            m_ID = 0;
            m_szProjektname = "";
            m_szBearbeiter = "";
            m_szBeschreibung = "";
            m_szKunde = "";
            m_Aenderungsdatum = DateTime.Now;
            m_ID_Klimaregion = 0;
            m_ID_Gebaeude = 0;
            m_Erstelldatum = DateTime.Now;
            m_szEinheit = "";
            m_nNetzverluste = 0;
            rows = 0;
        }
 
    }

}

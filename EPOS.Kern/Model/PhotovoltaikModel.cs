using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class PhotovoltaikModel
    {
        public int m_ID;
        public string m_szName;
        public string m_szFirma;
        public string m_szBeschreibung;
        public double m_Leistung;
        public double m_Wirkungsgrad;
        public double m_U_Mpp;
        public double m_U_Leerlauf;
        public double m_I_Mpp;
        public double m_I_Kurzschluss;
        public double m_alpha_SC;
        public double m_beta_OC;
        public double m_Temp_Coeff_Pmax;
        public double m_T_NOCT;
        public double m_Laenge;
        public double m_Breite;
        public double m_Modulkosten;


        public PhotovoltaikModel[] items;

        public PhotovoltaikModel()
        {
            items = null;
            m_ID = 0;
            m_szName = "";
            m_szFirma = "";
            m_szBeschreibung = "";
            m_Leistung= 0.0;
            m_Wirkungsgrad = 0.0;
            m_U_Mpp = 0.0;
            m_U_Leerlauf = 0.0;
            m_I_Mpp = 0.0;
            m_I_Kurzschluss = 0.0;
            m_alpha_SC = 0.0;
            m_beta_OC = 0.0;
            m_Temp_Coeff_Pmax = 0.0;
            m_T_NOCT = 0.0;
            m_Laenge = 0.0;
            m_Breite = 0.0;
            m_Modulkosten = 0.0;    
        }
    }
}

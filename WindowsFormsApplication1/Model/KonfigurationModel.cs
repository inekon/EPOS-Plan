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
        public int Betriebsart;
        public int Leistungsgrenze;

        /// <summary>
        /// TOT seit Etappe 3 (14.08.2026): Alt-Parameter Tab_Einstellungen.Pendelspeicher
        /// in m³. Wird von KonfigurationCtrl nur noch gelesen und geschrieben, damit der
        /// positionsbasierte Zugriff row[0..22] und die INSERT-/UPDATE-Spaltenlisten
        /// unverändert bleiben — den Wert wertet niemand mehr aus.
        /// Das Volumen des BHKW-Pendelspeichers steht in LITERN im Projekt-Puffer
        /// "BHKW-Pendelspeicher" (PufferSpCtrl.PendelspeicherVolumenLiter).
        /// </summary>
        public double Pendelspeicher;

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
            Betriebsart = 0;
            Leistungsgrenze = 0;
            Pendelspeicher = 0;
        }
    }
}

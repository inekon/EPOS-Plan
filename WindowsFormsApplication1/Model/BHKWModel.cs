namespace WindowsFormsApplication1
{
    
    public class BHKWModel
    {
        public int rows;
        public BHKWModel[] items;
        public int m_ID;
        public string m_szBezeichner;
        public string m_szFirma;
        public string m_szBeschreibung;
        public double m_Ptherm;
        public double m_Pel;
        public int m_Brennstoff;
        public double m_Wirkungsgrad;
        public double m_Investition_KWel;
        public double m_Raumbedarf;
        public double m_Wartungskosten_kWhel;
        public int m_Nutzungsdauer;
        public int m_NOx;
        public int m_SO2;
        public int m_CO;
        public int m_CO2;
        public int m_Staub;
        public string m_szMotortyp;
        public double m_Grenzleistung;
        public double m_Kosten_Modul;
        public double m_Kosten_Schallschutzhaube;
        public double m_Kosten_Lieferung;
        public double m_Kosten_Montage;
        public double m_Kosten_Abgasreinigung;

        public BHKWModel()
        {
            items = null;
            m_ID = 0;
            m_szBezeichner = string.Empty;
            m_szFirma = string.Empty;
            m_szBeschreibung = string.Empty;
            m_Ptherm = 0;
            m_Pel = 0;
            m_Brennstoff = 0;
            m_Wirkungsgrad = 0;
            m_Investition_KWel = 0; 
            m_Raumbedarf = 0;
            m_Wartungskosten_kWhel = 0;
            m_Nutzungsdauer = 0;
            m_NOx = 0;  
            m_SO2 = 0;
            m_CO = 0;
            m_CO2 = 0;
            m_Staub = 0;
            m_szMotortyp = "";
            m_Grenzleistung = 0;
            m_Kosten_Modul = 0;
            m_Kosten_Schallschutzhaube = 0;
            m_Kosten_Lieferung = 0;
            m_Kosten_Montage = 0;
            m_Kosten_Abgasreinigung = 0;
        }

}
}

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

        // =====================================================================
        // Herkunft der Zeile im UTC-Raster (Befund B1, Paket A des
        // PV-Ertragsmodell-Konzepts)
        // =====================================================================
        //
        // Tab_Solar(_STAMM) hat KEINE Zeitspalte; der Zeitbezug ist allein die
        // Zeilenreihenfolge (ORDER BY ID) und die ist UTC. Sortiert
        // SolardatenCtrl.ReadOrtszeit die Reihe auf Ortszeit um, geht diese
        // Position verloren - der Sonnenstand braucht sie aber weiterhin:
        // SolarCalculator.CalculateHourly rechnet ausdruecklich auf UTC-Basis
        // (Solarzeit = Stunde + (EoT + 4*Lon)/60). Beide Felder tragen sie
        // deshalb an der Zeile mit.
        //
        // 0 bedeutet "nicht gesetzt": Wer die Reihe ueber ReadAll/ReadAllStamm
        // liest, bekommt weiterhin die rohe UTC-Reihenfolge, und dort ist die
        // Position der Index selbst.

        /// <summary>Tag im UTC-Raster, 1-BASIERT (1…365) = utcIndex / 24 + 1.</summary>
        public int TagUtc;

        /// <summary>Stunde im UTC-Raster (0…23) = utcIndex % 24.</summary>
        public int StundeUtc;

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
            TagUtc = 0;
            StundeUtc = 0;
        }
    }
}

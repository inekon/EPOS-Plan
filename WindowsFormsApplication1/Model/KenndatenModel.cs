namespace WindowsFormsApplication1
{
    
    class KenndatenModel
    {
        public KenndatenModel[] items;

        public int m_ID;
        public int m_ID_WP;
        public int m_nVorlauf;
        public int m_nTemperatur;
        public double m_nCOP;
        public double m_nPTherm;

        public KenndatenModel()
        {
            items = null;
            m_ID = 0;
            m_ID_WP = 0;
            m_nVorlauf = 0;
            m_nTemperatur = 0;
            m_nCOP = 0;
            m_nPTherm = 0;
        } 
    }

}

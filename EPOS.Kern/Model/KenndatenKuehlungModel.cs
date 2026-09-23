namespace WindowsFormsApplication1
{
    
    class KenndatenKuehlungModel
    {
        public KenndatenKuehlungModel[] items;

        public int m_ID;
        public int m_ID_WP;
        public int m_nVorlauf;
        public int m_nTemperatur;
        public double m_nCOP;
        public double m_nPkuehl;

        /// <summary>
        /// <c>Last</c> - die Laststufe der Stuetzstelle [%] (VDI-3805-Import: „MAX" = 100).
        /// <b>NULL-treu</b> (Kuehlkonzept 5.1, Festlegung 1; 7.3): <c>null</c> heisst „keine
        /// Laststufe" - dann nimmt <see cref="KenndatenKuehlungCtrl.Reihen"/> alle Zeilen. Aus
        /// einem NULL darf beim Zurueckschreiben keine 0 werden.
        /// </summary>
        public int? m_nLast;

        public KenndatenKuehlungModel()
        {
            items = null;
            m_ID = 0;
            m_ID_WP = 0;
            m_nVorlauf = 0;
            m_nTemperatur = 0;
            m_nCOP = 0;
            m_nPkuehl = 0;
            m_nLast = null;
        } 
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Windows.Forms;

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
        public int m_nLast;

        public KenndatenKuehlungModel()
        {
            items = null;
            m_ID = 0;
            m_ID_WP = 0;
            m_nVorlauf = 0;
            m_nTemperatur = 0;
            m_nCOP = 0;
            m_nPkuehl = 0;
            m_nLast = 0;
        } 
    }

}

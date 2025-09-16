using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    
    class KenndatenModel
    {
        public KenndatenModel[] items;

        public int m_ID;
        public int m_ID_WP;
        public int m_nVorlauf;
        public int m_nTemperatur;
        public int m_nCOP;
        public int m_nPTherm;

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

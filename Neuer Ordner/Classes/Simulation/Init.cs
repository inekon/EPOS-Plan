using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1.Classes.Simulation
{
    class Init
    {
        public void Monatswerte_berechnen(int[] mo_anfang, int[] mo_ende)
        {
            int tageImMonat;
            // Monatsanfang bzw. Monatsende bestimmen
            DateTime date = DateTime.Today;  
            tageImMonat = DateTime.DaysInMonth(date.Year, 1) * 24;

            mo_anfang[0] = 0;
            mo_ende[0] = (mo_anfang[0] + tageImMonat) -1;
            
            for(int i=2; i<=12; i++)
            {
                tageImMonat = DateTime.DaysInMonth(date.Year, i) * 24;
                mo_anfang[i-1] = (mo_ende[i-2] + 1);
                mo_ende[i-1] = (mo_anfang[i-1] + tageImMonat)-1;
            }
        }
    }
}

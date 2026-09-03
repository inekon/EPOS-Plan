using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1.Classes.Simulation
{
    class Init
    {
        /// <summary>
        /// Rechenbasis der Monatsgrenzen (V0-6). Bewusst ein festes NICHT-Schaltjahr und
        /// bewusst nicht <c>DateTime.Today.Year</c>: Der Rechenkern ist fest auf 8760
        /// Stunden bzw. 365 Tage verdrahtet. In einem Schaltjahr ergäbe sich
        /// <c>mo_ende[11] = 8783</c> auf <c>float[8760]</c>-Vektoren, und jeder Lauf wäre
        /// in <c>BhkwPlan.MonatsSumme</c> mit einer IndexOutOfRangeException abgebrochen
        /// (ab 2028 sicher eintretend). Die Grenzen eines Nicht-Schaltjahres sind für alle
        /// Nicht-Schaltjahre identisch — das bisherige Verhalten bleibt damit unverändert.
        /// </summary>
        private const int REFERENZJAHR = 2025;

        public void Monatswerte_berechnen(int[] mo_anfang, int[] mo_ende)
        {
            int tageImMonat;
            // Monatsanfang bzw. Monatsende bestimmen
            tageImMonat = DateTime.DaysInMonth(REFERENZJAHR, 1) * 24;

            mo_anfang[0] = 0;
            mo_ende[0] = (mo_anfang[0] + tageImMonat) -1;

            for(int i=2; i<=12; i++)
            {
                tageImMonat = DateTime.DaysInMonth(REFERENZJAHR, i) * 24;
                mo_anfang[i-1] = (mo_ende[i-2] + 1);
                mo_ende[i-1] = (mo_anfang[i-1] + tageImMonat)-1;
            }
        }
    }
}

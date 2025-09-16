using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    class StromTestClass
    {
        public void MyTest()
        {
            // Strombedarf berechnen gemäß Projektprofile
            SimulationStrombedarf sim = new SimulationStrombedarf();
            
            sim.Strombedarf_berechnen("Type_A"); 

            // alle Prozesswerte durchlaufen
            for(int i = 0; i < 8760; i++)
            {
                float val = sim.prozesswerte[i];
            }

            StromspeicherCtrl ctrl = new StromspeicherCtrl();
            // Daten für test1 holen
            ctrl.ReadSingle("test1");
            
            double leistung = ctrl.m_Leistung;
            double Energie = ctrl.m_Energie;
            double ladezustand = ctrl.m_Ladezustand;
            double Degradation = ctrl.m_Degradation; 

        }


    }
}

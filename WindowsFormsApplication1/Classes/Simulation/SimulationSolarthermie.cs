using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    public class SimulationSolarthermie
    {
        public List<int> solarthermie_list = new List<int>();
        public int m_ID_Projekt = 0;
        public double Max_Waermebedarf;
        public float[] Waermebedarf = new float[8760];
        public float[] Restwaerme = new float[8760];
        public float[] Waermeproduktion = new float[8760];
        public double Waermeproduktion_gesamt = 0;
        public double Waermebedarf_gesamt = 0;


        public bool Berechnung(int ID_Projekt)
        {
            int Anzahl = 0;
            m_ID_Projekt = ID_Projekt;

            Init();


            // Wärmebedarf gesamt berechnen, in MWh
            Waermebedarf_gesamt = 0;
            Max_Waermebedarf = Maximaler_Waermebedarf(Waermebedarf); // kW
            Array.ForEach(Waermebedarf, value => Waermebedarf_gesamt += value);
            Waermebedarf_gesamt /= 1000; // in MWh  

//zum Test--------------------

            double x = 0;
            for (int i = 0; i < 8760; i++)
            {
                if (i < 1000)
                {
                    Waermeproduktion[i] = Waermebedarf[i];
                    Waermeproduktion_gesamt += Waermeproduktion[i];
                    Restwaerme[i] = 0;
                }
                else Restwaerme[i] = Waermebedarf[i];

            }

    
            //-------------------------------------            

            Waermeproduktion_gesamt /= 1000; // in MWh

    
            return true;
        }

        public float Maximaler_Waermebedarf(float[] waermebedarf)
        {
            float Waermebedarf_Max;

            Waermebedarf_Max = 0;
            for (int i = 0; i < waermebedarf.Length; i++)
            {
                if (Waermebedarf_Max < waermebedarf[i]) Waermebedarf_Max = waermebedarf[i];
            }

            return Waermebedarf_Max;
        }
        public void Init()
        {
            Array.Clear(Restwaerme, 0, Restwaerme.Length);
            Array.Clear(Waermeproduktion, 0, Waermeproduktion.Length);
            Max_Waermebedarf = 0;
            Waermeproduktion_gesamt = 0;
        }

    }

}

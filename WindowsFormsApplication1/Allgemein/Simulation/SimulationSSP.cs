using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    internal class SimulationSSP
    {

        public void Berechnung(int ID_Projekt)
        {


            SimulationStrombedarf simStrom = new SimulationStrombedarf();
            simStrom.Berechnung(ID_Projekt);

            for (int i = 0; i < 8760; i++)
            {
                double value = simStrom.Stromganglinie[i];
            }

        }
    }


}

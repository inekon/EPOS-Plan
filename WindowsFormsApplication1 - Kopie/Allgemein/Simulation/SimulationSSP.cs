using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    public class SimulationSSP
    {
        // --- Datenstrukturen ---
        public List<int> stromspeicher_list = new List<int>();
        public int m_ID_Projekt = 0;

        // Input-Arrays
        public float[] Strombedarf = new float[8760 * 4];

        public float[] Berechnung(int ID_Projekt)
        {
            /* in Strombedarf sind 15-Minuten-Werte des Strombedarfs gespeichert */

            WErzeugerCtrl ctrl = new WErzeugerCtrl();
            RecordSet rs = new RecordSet();
            double SpeicherKapazitaetKWh = 0;
            double MaxLadeLeistungKW = 0;
            int id = 0;

            // alle Sromspeicher zum Projekt durchgehen und Leistung aufsummieren 
            ctrl.ReadAllFilter("ID_Projekt=" + ID_Projekt + " and ID_Type=" + WizardItemClass.SP_TYP);

            for (int i = 0; i < ctrl.rows; i++)
            {
                id = ctrl.items[i].ID_SP;
                rs.Open("select * from Tab_Stromspeicher where ID=" + id);
                if (rs.Next())
                {
                    SpeicherKapazitaetKWh += (double)rs.Read("Energie");
                }
                rs.Close();
                MaxLadeLeistungKW = SpeicherKapazitaetKWh;
            }

            // hier wird die eigentliche Simulation des Strombedarfs mit den vorhandenen Stromspeichern durchgeführt
            for (int i = 0; i < 8760 * 4; i++)
            {
                double value = Strombedarf[i];



            }
            return Strombedarf; // den korrigierten Wert zurückgeben, der den Strombedarf unter Berücksichtigung der Stromspeicher darstellt
        }
    }
}




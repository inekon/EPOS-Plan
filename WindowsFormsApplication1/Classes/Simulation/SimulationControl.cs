using System;

namespace WindowsFormsApplication1
{
    internal class SimulationControl
    {
       
        public SimulationWaermepumpe simulation_wp = new SimulationWaermepumpe();
        public SimulationSPK simulation_spk = new SimulationSPK();
        public SimulationSolarthermie simulation_solarthermie = new SimulationSolarthermie();

        private bool m_bError = false;

        // Eingangsparameter
        public SimulationWaermebedarf simulation_Waermebedarf;
        public SimulationStrombedarf simulation_Strombedarf;

        public KonfigurationCtrl ctrl_konfig;
        public int m_ID_Projekt;
        public string[] tool;
        public float[] Stundentemperatur = new float[8760];
        
        // Rückgabe
        public float Restwaerme;
        public float Reststrom;
        float[] Rest_Wermebedarf_stuendlich = new float[8760];
        float[] Rest_Strombedarf_viertelstuendlich = new float[8760 * 4];

        public bool bSimulationWP = false;
        public bool bSimulationKessel = false;
        public bool bSimulationSolarthermie = false;


        public void Do_Simulation(int ID_Projekt)
        {
            float[] temp = new float[8760 * 4];
            float[] Eingang = new float[2];
            float[] Ausgang = new float[2];

            m_ID_Projekt = ID_Projekt;

            Array.Clear(Rest_Wermebedarf_stuendlich, 0, Rest_Wermebedarf_stuendlich.Length);
            Array.Clear(Rest_Strombedarf_viertelstuendlich, 0, Rest_Strombedarf_viertelstuendlich.Length);
            
            simulation_wp.Init();
            simulation_solarthermie.Init();
            simulation_spk.Init();

            Stundentemperatur = simulation_Waermebedarf.Stundentemperatur;
            Restwaerme = 0;
            Reststrom = simulation_Strombedarf.Strombedarf_gesamt; //MWh
            Rest_Strombedarf_viertelstuendlich = simulation_Strombedarf.Strombedarf_viertelStundenwerte;
            Rest_Wermebedarf_stuendlich = (float[])simulation_Waermebedarf.Waermebedarf.Clone();

            bSimulationWP = false;
            bSimulationKessel = false;
            bSimulationSolarthermie = false;

            // Startpunkt der Simulation ist der Wärmebedarf    
            Eingang = simulation_Waermebedarf.Waermebedarf;

            for (int i = 0; i < 4; i++)
            {
                if (tool[i] == "Wärmepumpe")
                {
                    Ausgang = Simulation_WP_Ctrl(Eingang, ctrl_konfig.model.m_WP_Heizstab);
                    if (m_bError) Ausgang = Eingang;
                    Restwaerme = 0;
                    for (int n = 0; n < 8760; n++) Restwaerme += Ausgang[n];
                    Rest_Wermebedarf_stuendlich = Ausgang;
                    Eingang = Ausgang;

                    Reststrom += (float)simulation_wp.WP_Strombedarf_gesamt / 1000f; // in MWh
                    Reststrom += (float)simulation_wp.Heizstab_gesamt / 1000f; // in MWh

                    temp = Stundenwerte_zu_viertelstunden(simulation_wp.WP_Strombedarf_stuendlich);
                    Rest_Strombedarf_viertelstuendlich = AddVectors(Rest_Strombedarf_viertelstuendlich, temp);
                    temp = Stundenwerte_zu_viertelstunden(simulation_wp.Heizstab_stuendlich);
                    Rest_Strombedarf_viertelstuendlich = AddVectors(Rest_Strombedarf_viertelstuendlich, temp);

                    bSimulationWP = true;
                }
                else if (tool[i] == "Heizkessel")
                {
                    Ausgang = Simulation_SPK_Ctrl(Eingang, ctrl_konfig.model.m_Kessel_Betriebsbereitschaft);
                    Restwaerme = 0;
                    for (int n = 0; n < 8760; n++) Restwaerme += Ausgang[n];
                    Rest_Wermebedarf_stuendlich = Ausgang;
                    Eingang = Ausgang;
                    Reststrom += (float)simulation_spk.Stromverbrauch_Spk;

                    temp = Stundenwerte_zu_viertelstunden(simulation_spk.Strombedarf_stuendlich);
                    Rest_Strombedarf_viertelstuendlich = AddVectors(Rest_Strombedarf_viertelstuendlich, temp);

                    bSimulationKessel = true;
                }
                else if (tool[i] == "Solarthermie")
                {
                    Ausgang = Simulation_Solarthermie_Ctrl(Eingang);
                    Restwaerme = 0;
                    for (int n = 0; n < 8760; n++) Restwaerme += Ausgang[n];
                    Rest_Wermebedarf_stuendlich = Ausgang;
                    Eingang = Ausgang;

                    bSimulationSolarthermie = true;
                }
            }

            Restwaerme /= 1000; // in MWh
            
            if (tool[5] == "Stromspeicher")
            {
                // Rest_Strombedarf_viertelstuendlich
            }

        }

        private float[] Simulation_WP_Ctrl(float[] Waermebedarf, bool bHeizstab)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.WP_TYP);

            simulation_wp.wp_list.Clear();
            while (rs.Next())
            {
                simulation_wp.wp_list.Add((int)rs.Read("ID"));
            }
            rs.Close();

            simulation_wp.Temperatur = Stundentemperatur;
            simulation_wp.Waermebedarf_stuendlich = Waermebedarf;
            simulation_wp.Mit_Heizstab = bHeizstab;
            // Simulation starten
            m_bError = !simulation_wp.Berechnung();

            return  m_bError ? Waermebedarf : simulation_wp.waermerestbedarf_stuendlich;
        }

        private float[] Simulation_SPK_Ctrl(float[] Waermebedarf, int nBereitschaft)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.KESSEL_TYP);
   
            simulation_spk.spk_list.Clear();
            while (rs.Next())
            {
                simulation_spk.spk_list.Add((string)rs.Read("Bezeichner"));
            }
            rs.Close();

            simulation_spk.Waermebedarf = Waermebedarf;
            simulation_spk.Vorgabe_Betriebsbereitschaft = nBereitschaft;
            
            // Simulation starten
       //     if (simulation_spk.spk_list.Count == 0) return Waermebedarf;
            simulation_spk.Berechnung(m_ID_Projekt);

            double summe = 0;
            for(int i=0;i<8760;i++) summe+= simulation_spk.Restwaerme[i];   

            return simulation_spk.Restwaerme;
        }

        private float[] Simulation_Solarthermie_Ctrl(float[] Waermebedarf)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SOLAR_TYP);

            simulation_solarthermie.solarthermie_list.Clear();
            while (rs.Next())
            {
                simulation_solarthermie.solarthermie_list.Add((int)rs.Read("ID_SOLAR"));
            }
            rs.Close();

            simulation_solarthermie.Waermebedarf = Waermebedarf;
    
            // Simulation starten
            simulation_solarthermie.Berechnung(m_ID_Projekt);

            return simulation_solarthermie.Restwaerme;
        }

        public float[] AddVectors(float[] array1, float[] array2)
        {
            if (array1.Length != array2.Length)
                throw new ArgumentException("Arrays must be of the same length.");

            float[] result = new float[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            {
                result[i] = array1[i] + array2[i];
            }
            return result;
        }

        public float[] Stundenwerte_zu_viertelstunden(float[] stundenwerte)
        {
            float[] viertelstundenwerte = new float[stundenwerte.Length * 4];
            for (int i = 0; i < stundenwerte.Length; i++)
            {
                viertelstundenwerte[i * 4] = stundenwerte[i];
                viertelstundenwerte[i * 4 + 1] = stundenwerte[i];
                viertelstundenwerte[i * 4 + 2] = stundenwerte[i];
                viertelstundenwerte[i * 4 + 3] = stundenwerte[i];
            }
            return viertelstundenwerte;
        }

    }
}

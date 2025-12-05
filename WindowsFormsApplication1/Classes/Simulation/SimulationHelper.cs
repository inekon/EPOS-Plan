namespace WindowsFormsApplication1
{
    internal class SimulationHelper
    {
       
        public SimulationWaermepumpe simulation_wp = new SimulationWaermepumpe();
        public SimulationSPK simulation_spk = new SimulationSPK();
        
        private bool m_bError = false;

        //Parameter
        public SimulationWaermebedarf simulation_Waermebedarf;
        public SimulationStrombedarf simulation_Strombedarf;
        public KonfigurationCtrl ctrl_konfig;
        public int m_ID_Projekt;
        public string[] tool;
        public float[] Stundentemperatur = new float[8760];
        
        //Rückgabe
        public float Restwaerme;
        public float Reststrom;

        public void Do_Simulation(int ID_Projekt)
        {
            float[] Wermebedarf = new float[8760];
            float[] Strombedarf = new float[8760];
            float[] Eingang = new float[2];
            float[] Ausgang = new float[2];

            for (int i = 0; i < 8760; i++)
            {
                Wermebedarf[i] = 0;
                Strombedarf[i] = 0;
            }

            m_ID_Projekt = ID_Projekt;
            Restwaerme = 0;
            Reststrom = simulation_Strombedarf.Strombedarf_gesamt;
            Stundentemperatur = simulation_Waermebedarf.Stundentemperatur;

            Eingang = simulation_Waermebedarf.Waermebedarf;
            for (int i = 0; i < 4; i++)
            {
                if (tool[i] == "Wärmepumpe")
                {
                    Ausgang = Simulation_WP(Eingang, ctrl_konfig.model.m_WP_Heizstab);
                    if(m_bError) Ausgang = Eingang;
                    Restwaerme = 0;
                    for (int n = 0; n < 8760; n++) Restwaerme += Ausgang[n];

                    Eingang = Ausgang;
                    Reststrom += (float)simulation_wp.WP_Strombedarf_gesamt / 1000;
                    Reststrom += (float)simulation_wp.Heizstab_gesamt/1000;

                }
                else if (tool[i] == "Spitzenkessel")
                {
                    Ausgang = Simulation_SPK(Eingang, ctrl_konfig.model.m_Kessel_Betriebsbereitschaft);
                    Restwaerme = 0;
                    for (int n = 0; n < 8760; n++) Restwaerme += Ausgang[n];
                    Eingang = Ausgang;
                    Reststrom += (float)simulation_spk.Stromverbrauch_Spk;
                }

            }
  
            Restwaerme /= 1000;

        }

        private float[] Simulation_WP(float[] Waermebedarf, bool bHeizstab)
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

        private float[] Simulation_SPK(float[] Waermebedarf, int nBereitschaft)
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
            if (simulation_spk.spk_list.Count == 0) return simulation_spk.Waermebedarf;
            simulation_spk.Berechnung(m_ID_Projekt);

            double summe = 0;
            for(int i=0;i<8760;i++) summe+= simulation_spk.Restwaerme[i];   


            return simulation_spk.Restwaerme;
        }



    }
}

using System;
using System.Linq;

namespace WindowsFormsApplication1
{
    public  class SimulationControl
    {
        // Simulationen Deklaration
        public SimulationWaermepumpe simulation_wp = new SimulationWaermepumpe();
        public SimulationSPK simulation_spk = new SimulationSPK();
        public SimulationSolarthermie simulation_solarthermie = new SimulationSolarthermie();
        public SimulationPV simulation_pv = new SimulationPV(); 
        public SimulationSSP simulation_ssp = new SimulationSSP();
        public SimulationBHKW simulation_bhkw = new SimulationBHKW();

        private bool m_bError = false;

        // Eingangsparameter
        public SimulationWaermebedarf simulation_Waermebedarf;
        public SimulationStrombedarf simulation_Strombedarf;

        // Pufferspeicher der Wärmepumpe (Stufe 1), null = keiner zugeordnet
        public SimulationPufferspeicher puffer_wp = null;

        public KonfigurationCtrl ctrl_konfig;
        public int m_ID_Projekt;
        public string[] tool;
        public float[] Stundentemperatur = new float[8760];
        public int modeBHKW;
        public int GrenzleistungBHKW;
        public int VolumenPendelspeicherBHKW;


        // Rückgabe
        public float Restwaerme;
        public float Reststrom;
        public float[] Rest_Waermebedarf_stuendlich = new float[8760];
        public float[] Rest_Strombedarf_viertelstuendlich = new float[8760 * 4];

        public bool bSimulationWP = false;
        public bool bSimulationKessel = false;
        public bool bSimulationSolarthermie = false;
        public bool bSimulationPV = false;
        public bool bSimulationSSP = false;
        public bool bSimulationBHKW = false;

        public void Do_Simulation(int ID_Projekt)
        {
            float[] temp = new float[8760 * 4];
            float[] Eingang;
            float[] Ausgang;

            m_ID_Projekt = ID_Projekt;

            Array.Clear(Rest_Waermebedarf_stuendlich, 0, Rest_Waermebedarf_stuendlich.Length);
            Array.Clear(Rest_Strombedarf_viertelstuendlich, 0, Rest_Strombedarf_viertelstuendlich.Length);
            
            simulation_wp.Init();
            simulation_solarthermie.Init();
            simulation_spk.Init();
            simulation_pv.Init();

            // Neue Spalten (Prioritaet, Wärmequelle/-senke, Speicherregelung) sicherstellen,
            // bevor darauf zugegriffen wird - die Simulation kann ohne vorheriges Öffnen
            // des Konfigurationsdialogs gestartet werden.
            WaermequelleClass.SchemaSicherstellen();

            // ***********************************************************************
            // Pufferspeicher-Zuordnung laden (Stufe 1: nur Wärmepumpe).
            // Quelle: Z_ProjektPufferSp (Erzeuger, Vorlauf/Rücklauf, Priorität) und
            // Tab_Pufferspeicher (Gesamtvolumen, Bereitschaftsverluste). Die erste
            // Zuordnung je Erzeuger (höchste Priorität) wird verwendet.
            // ***********************************************************************
            puffer_wp = null;
            Z_ProjektPufferSpCtrl pspZuordnung = new Z_ProjektPufferSpCtrl();
            pspZuordnung.ReadAll("ID_Projekt=" + m_ID_Projekt);
            for (int n = 0; n < pspZuordnung.rows; n++)
            {
                if (pspZuordnung.items[n].Erzeuger != "Wärmepumpe") continue;

                PufferSpCtrl psp = new PufferSpCtrl();
                psp.ReadAll("ID=" + pspZuordnung.items[n].ID_Pufferspeicher);
                if (psp.rows == 0 && !string.IsNullOrEmpty(pspZuordnung.items[n].PufferSp))
                {
                    // Fallback für Altdaten ohne ID: über Bezeichner im Projekt suchen
                    psp.ReadAll("Bezeichner='" + pspZuordnung.items[n].PufferSp.Replace("'", "''") +
                                "' AND ID_Projekt=" + m_ID_Projekt);
                }

                if (psp.rows > 0)
                {
                    puffer_wp = new SimulationPufferspeicher();
                    puffer_wp.Bezeichner = psp.items[0].Name;
                    puffer_wp.Erzeuger = "Wärmepumpe";
                    puffer_wp.Init(psp.items[0].Gesamtvolumen,
                                   pspZuordnung.items[n].Vorlauf,
                                   pspZuordnung.items[n].Ruecklauf,
                                   psp.items[0].Betriebsbereitschaftverlust);

                    // Konfigurierbare Schwellen der Speicherregelung [%]
                    object sEin = WaermequelleClass.WertLesenStill("Z_ProjektPufferSp", "Schwelle_Ein", pspZuordnung.items[n].ID);
                    object sAus = WaermequelleClass.WertLesenStill("Z_ProjektPufferSp", "Schwelle_Aus", pspZuordnung.items[n].ID);
                    if (sEin != null && Convert.ToDouble(sEin) > 0)
                        puffer_wp.SchwelleEin = Convert.ToDouble(sEin) / 100.0;
                    if (sAus != null && Convert.ToDouble(sAus) > 0)
                        puffer_wp.SchwelleAus = Convert.ToDouble(sAus) / 100.0;
                }
                break; // ReadAll sortiert nach Priorität -> erster Treffer gewinnt
            }
            simulation_wp.Pufferspeicher = puffer_wp;

            Stundentemperatur = simulation_Waermebedarf.Stundentemperatur;
            Restwaerme = 0;
            Reststrom = simulation_Strombedarf.Strombedarf_gesamt; //MWh
            Rest_Strombedarf_viertelstuendlich = simulation_Strombedarf.Strombedarf_viertelStundenwerte;
            Rest_Waermebedarf_stuendlich = (float[])simulation_Waermebedarf.Waermebedarf.Clone();

            bSimulationWP = false;
            bSimulationKessel = false;
            bSimulationSolarthermie = false;
            bSimulationPV = false;  

            // Startpunkt der Simulation ist der Wärmebedarf    
            Eingang = simulation_Waermebedarf.Waermebedarf;

            for (int i = 0; i < 4; i++)
            {
                if (tool[i] == "Wärmepumpe")
                {
                    Ausgang = Simulation_WP_Ctrl(Eingang, Viertelstunden_zu_Stundenwerte_Mittelwert(Rest_Strombedarf_viertelstuendlich), ctrl_konfig.model.m_WP_Heizstab);
     
                    if (m_bError) Ausgang = Eingang;
                    Restwaerme = 0;
                    for (int n = 0; n < 8760; n++) Restwaerme += Ausgang[n];
                    Rest_Waermebedarf_stuendlich = Ausgang;
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
                    Ausgang = Simulation_SPK_Ctrl(Eingang, Viertelstunden_zu_Stundenwerte_Mittelwert(Rest_Strombedarf_viertelstuendlich), ctrl_konfig.model.m_Kessel_Betriebsbereitschaft);
                    Restwaerme = 0;
                    for (int n = 0; n < 8760; n++) Restwaerme += Ausgang[n];
                    Rest_Waermebedarf_stuendlich = Ausgang;
                    Eingang = Ausgang;
                   
                    temp = Stundenwerte_zu_viertelstunden(simulation_spk.Stromverbrauch_stuendlich);
                    Rest_Strombedarf_viertelstuendlich = AddVectors(Rest_Strombedarf_viertelstuendlich, temp);
                    
                    bSimulationKessel = true;
                }
                else if (tool[i] == "Solarthermie")
                {
                    Ausgang = Simulation_Solarthermie_Ctrl(Eingang);

                    Restwaerme = 0;
                    for (int n = 0; n < 8760; n++) Restwaerme += Ausgang[n];
                    Rest_Waermebedarf_stuendlich = Ausgang;
                    Eingang = Ausgang;

                    bSimulationSolarthermie = true;
                }
                else if (tool[i] == "BHKW")
                {
                    Ausgang = Simulation_BHKW_Ctrl(Eingang, Viertelstunden_zu_Stundenwerte_Mittelwert(Rest_Strombedarf_viertelstuendlich));

                    Restwaerme = Ausgang.Sum();
                    Rest_Waermebedarf_stuendlich = Ausgang;
                    Eingang = Ausgang;

                    // Erzeugung holen und in Viertelstunden wandeln
                    float[] bhkwStromStuendlich = simulation_bhkw.stromproduktion;
                    float[] bhkwStromViertelstuendlich = Stundenwerte_zu_viertelstunden(bhkwStromStuendlich);

                    // Erzeugung vom Vektor abziehen
                    Rest_Strombedarf_viertelstuendlich = SubVectors(Rest_Strombedarf_viertelstuendlich, bhkwStromViertelstuendlich, false);
                    bSimulationBHKW = true;
                }
            }

            // Photovoltaik abziehen
            if (tool[4] == "Photovoltaik")
            {
                var x = Rest_Strombedarf_viertelstuendlich.Sum() / 4000;
                temp = Simulation_Photovoltaik_Ctrl(Rest_Strombedarf_viertelstuendlich);
                Rest_Strombedarf_viertelstuendlich = SubVectors(Rest_Strombedarf_viertelstuendlich, temp);
                bSimulationPV = true;
            }

            // Stromspeicher verrechnen
            if (tool[5] == "Stromspeicher")
            {
                temp = Simulation_Stromspeicher_Ctrl(Rest_Strombedarf_viertelstuendlich);
                Rest_Strombedarf_viertelstuendlich = SubVectors(Rest_Strombedarf_viertelstuendlich, temp);
                bSimulationSSP = true;
            }

            // Wärmebedarf von kWh in MWh umrechnen
            Restwaerme /= 1000f;

            // Reststrom mathematisch korrekt aus dem finalen Ergebnis-Vektor berechnen
            // Falls deine Quell-Vektoren stündliche kW-Mittelwerte/kWh enthalten:
            Reststrom = Rest_Strombedarf_viertelstuendlich.Sum() / 4000f;

        }

        private float[] Simulation_WP_Ctrl(float[] Waermebedarf, float[] Strombedarf, bool bHeizstab)
        {
            RecordSet rs = new RecordSet();

            // Neue Spalten (Prioritaet, WQ_*) bei Bedarf anlegen und die WPs in
            // der eingestellten Prioritätsreihenfolge einsetzen (Kaskade).
            WaermequelleClass.SchemaSicherstellen();

            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.WP_TYP + " order by Prioritaet, ID");

            simulation_wp.wp_list.Clear();
            
            while (rs.Next())
            {
                simulation_wp.wp_list.Add((int)rs.Read("ID"));
            }
            rs.Close();

            simulation_wp.Temperatur = Stundentemperatur;
            simulation_wp.Waermebedarf_stuendlich = Waermebedarf;
            simulation_wp.PV_Ueberschuss_stuendlich = PV_Ueberschuss_Vorabberechnen();
            // Warmwasseranteil für die Wärmesenken-Zuordnung der Module
            simulation_wp.Warmwasserbedarf_stuendlich =
                simulation_Waermebedarf != null ? simulation_Waermebedarf.brauchwasserwerte : null;
            simulation_wp.WP_Strombedarf_stuendlich = Strombedarf;
            simulation_wp.Mit_Heizstab = bHeizstab;
            
            // Simulation starten
            m_bError = !simulation_wp.Berechnung();

            return  m_bError ? Waermebedarf : simulation_wp.waermerestbedarf_stuendlich;
        }

        /// <summary>
        /// Ermittelt den stündlichen PV-Überschuss [kW] für den Betriebsmodus
        /// "PV-optimiert" der Wärmepumpe. Da die Photovoltaik erst nach den
        /// Wärmeerzeugern gerechnet wird, wird ihr wetterabhängiges Potenzial
        /// hier vorab bestimmt und um den übrigen Strombedarf reduziert.
        /// Liefert null, wenn keine Wärmepumpe im PV-Modus betrieben wird.
        /// </summary>
        private float[] PV_Ueberschuss_Vorabberechnen()
        {
            // Läuft überhaupt eine Wärmepumpe im PV-Modus?
            bool pvModus = false;
            RecordSet rs = new RecordSet();
            rs.Open("select ID from Tab_Energieanlagen where ID_Projekt=" + m_ID_Projekt +
                    " and ID_Type=" + WizardItemClass.WP_TYP);
            while (rs.Next())
            {
                string modus = WaermequelleClass.WertLesen((int)rs.Read("ID"), "BM_Typ") as string;
                if (modus == WaermequelleClass.MODUS_PV) { pvModus = true; break; }
            }
            rs.Close();

            if (!pvModus || tool == null || tool.Length < 5 || tool[4] != "Photovoltaik") return null;

            try
            {
                // PV-Potenzial vorab bestimmen (wetterabhängig, unabhängig vom Bedarf)
                simulation_pv.m_ID_Projekt = m_ID_Projekt;
                simulation_pv.Strombedarf = (float[])Rest_Strombedarf_viertelstuendlich.Clone();
                simulation_pv.Berechnung(m_ID_Projekt);

                float[] potenzial = (float[])simulation_pv.pvPotentialGesamt_stuendlich.Clone();
                float[] bedarf = Viertelstunden_zu_Stundenwerte_Mittelwert(Rest_Strombedarf_viertelstuendlich);

                float[] ueberschuss = new float[8760];
                for (int i = 0; i < 8760; i++)
                {
                    float rest = potenzial[i] - (i < bedarf.Length ? bedarf[i] : 0);
                    ueberschuss[i] = rest > 0 ? rest : 0;
                }

                // Zustand der PV-Simulation zurücksetzen - sie wird später regulär gerechnet
                simulation_pv.Init();
                return ueberschuss;
            }
            catch (Exception ex)
            {
                Console.WriteLine("PV-Überschuss konnte nicht vorab bestimmt werden: " + ex.Message);
                simulation_pv.Init();
                return null;
            }
        }

        private float[] Simulation_BHKW_Ctrl(float[] Waermebedarf, float[] Strombedarf)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass. BHKW_TYP);

            simulation_bhkw.bhkw_list.Clear();
            
            int i = 0;
            while (rs.Next())
            {
                simulation_bhkw.bhkw_list.Add((int)rs.Read("ID_BHKW"));
                simulation_bhkw.bhkw_list_Namen.Add((string)rs.Read("Bezeichner"));
                double Grenzleistung = (double)rs.Read("Grenzleistung") / 100;
                simulation_bhkw.bhkwGrenzL[i++] = (float)Grenzleistung;
            }
            rs.Close();

            simulation_bhkw.waermebedarf = Waermebedarf;
            simulation_bhkw.strombedarf = Strombedarf;
            simulation_bhkw.bhkwGrenzleistungAllgemein = GrenzleistungBHKW;
            simulation_bhkw.kapazitaetPendelspeicher = (float)VolumenPendelspeicherBHKW * 20000 / 860;
            simulation_bhkw.modeBHKW = modeBHKW;

            // Simulation starten
            m_bError = !simulation_bhkw.Berechnung(m_ID_Projekt);

            float[] restwaerme = SubVectors(Waermebedarf, simulation_bhkw.waermeproduktion);
      
            return m_bError ? Waermebedarf : restwaerme;
        }

        private float[] Simulation_SPK_Ctrl(float[] Waermebedarf, float[] Strombedarf, int nBereitschaft)
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
            simulation_spk.Strombedarf_stuendlich = Strombedarf;
            simulation_spk.Vorgabe_Betriebsbereitschaft = nBereitschaft;
            
            // Simulation starten
            simulation_spk.Berechnung(m_ID_Projekt);

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

            simulation_solarthermie.Waermebedarf = Array.ConvertAll<float, double>(Waermebedarf, x => (double)x);

            // Simulation starten
            simulation_solarthermie.Berechnung(m_ID_Projekt);

            return Array.ConvertAll<double, float>(simulation_solarthermie.Restwaerme, x => (float)x);
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

        public float[] SubVectors(float[] array1, float[] array2, bool korrigiert=true)
        {
            if (array1.Length != array2.Length)
                throw new ArgumentException("Arrays must be of the same length.");

            float[] result = new float[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            {
                if (korrigiert)
                {
                    if (array1[i] >= array2[i])
                        result[i] = array1[i] - array2[i];
                    else result[i] = 0;
                }
                else result[i] = array1[i] - array2[i];
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

        public float[] Viertelstunden_zu_Stundenwerte_Mittelwert(float[] viertelstundenwerte)
        {
            // Die Länge des neuen Arrays ist genau ein Viertel des Originals
            float[] stundenwerte = new float[viertelstundenwerte.Length / 4];

            for (int i = 0; i < stundenwerte.Length; i++)
            {
                // Die 4 Viertelstunden einer Stunde zusammenrechnen und den Durchschnitt bilden
                float summe = viertelstundenwerte[i * 4] +
                              viertelstundenwerte[i * 4 + 1] +
                              viertelstundenwerte[i * 4 + 2] +
                              viertelstundenwerte[i * 4 + 3];

                stundenwerte[i] = summe / 4f;
            }

            return stundenwerte;
        }

        private float[] Simulation_Photovoltaik_Ctrl(float[] Strombedarf)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.PV_TYP);

            simulation_pv.photovoltaik_list.Clear();
            while (rs.Next())
            {
                simulation_pv.photovoltaik_list.Add((int)rs.Read("ID_PV"));
            }
            rs.Close();

            simulation_pv.Strombedarf = Strombedarf;

            // Simulation starten
            float[] temp = simulation_pv.Berechnung(m_ID_Projekt);

            TestePVAnlage();

            return temp;
        }

        public void TestePVAnlage()
        {
            // Test-Parameter für eine 10 kWp Anlage
            double testStrahlung = 1000.0; // W/m² (STC-Bedingung)
            double flaeche = 50.0;          // ca. 50m² für 10 kWp
            double wirkungsgrad = 0.20;     // 20% Wirkungsgrad
            double tempKoeff = -0.004;      // -0.4%/K
            double tAmb = 25.0;             // 25°C Luft
            double cosTheta = 1.0;          // Sonne steht perfekt senkrecht
            double strombedarf = 100.0;     // Hoher Bedarf, damit wir Produktion nicht kappen

            var ergebnis = simulation_pv.BerechnePV(strombedarf, testStrahlung, flaeche, wirkungsgrad, tempKoeff, tAmb, cosTheta);

            Console.WriteLine($"--- PV TESTLAUF ---");
            Console.WriteLine($"Potenzielle Produktion: {ergebnis.produktion} kW");

            if (ergebnis.produktion < 8.0)
            {
                Console.WriteLine("WARNUNG: Der Wert ist zu niedrig für 10kWp bei 1000W/m²!");
                Console.WriteLine("Prüfe: Ist h0 wirklich 0.20? Wird flaeche korrekt übergeben?");
            }
            else
            {
                Console.WriteLine("ERGEBNIS OK: Die Formel arbeitet physikalisch korrekt.");
            }
        }

        private float[] Simulation_Stromspeicher_Ctrl(float[] Strombedarf)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SP_TYP);

            simulation_ssp.stromspeicher_list.Clear();
            while (rs.Next())
            {
                simulation_ssp.stromspeicher_list.Add((int)rs.Read("ID_SP"));
            }
            rs.Close();

            simulation_ssp.Strombedarf = Strombedarf;

            // Simulation starten
            float[] temp = simulation_ssp.Berechnung(m_ID_Projekt);
            return temp;
        }

    }
}

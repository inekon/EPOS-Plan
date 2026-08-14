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

        /// <summary>
        /// Volumen des BHKW-Pendelspeichers in LITERN (Etappe 3, 14.08.2026).
        ///
        /// Bis dahin stand hier der Alt-Parameter Tab_Einstellungen.Pendelspeicher in m³.
        /// Quelle ist jetzt ausschließlich der Projekt-Puffer "BHKW-Pendelspeicher"
        /// (PufferSpCtrl.PendelspeicherVolumenLiter); die Migration hat den Alt-Wert
        /// dorthin als m³ × 1000 übernommen.
        /// </summary>
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

        /// <summary>
        /// Grund, aus dem der letzte Simulationsversuch gar nicht erst angelaufen ist
        /// (leer, wenn gerechnet wurde). Gefüllt von der Migrationsblockade, damit
        /// aufrufende Formulare den Anwender informieren können, statt Nullwerte
        /// anzuzeigen.
        /// </summary>
        public string Sperrgrund = "";

        public void Do_Simulation(int ID_Projekt)
        {
            // Engine-Einstieg: Blockade bei nicht abgeschlossener Schema-Migration
            // (ADR-001, Aufgabe 6). Bewusst ohne MessageBox - die Engine bleibt
            // dialogfrei (Konzept 13.4); der Grund steht in Sperrgrund.
            Sperrgrund = "";
            string sperrgrund;
            if (SchemaMigration.SimulationGesperrt(out sperrgrund))
            {
                Sperrgrund = sperrgrund;
                Console.WriteLine("Simulation abgebrochen: " + sperrgrund);
                return;
            }

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
                    // Etappe 4: Die PUFFER-Zeile ist die führende Ablage der
                    // Betriebstemperaturen (Konzept 5.1) - ein Speicher hat genau einen
                    // Betriebszustand, unabhängig davon, wie viele Anlagen ihn laden.
                    // Nur wenn dort kein vollständiges Paar steht (Alt-Datenbank, nie
                    // migriert, Werte gelöscht), gilt weiter die Zuordnungszeile.
                    //
                    // Regressionsneutral: Migration R1 hat genau die Werte DIESER
                    // Zuordnungszeile an den Puffer geschrieben - der Vorrang liefert
                    // auf migrierten Beständen dieselben Zahlen wie bisher.
                    int vorlauf = pspZuordnung.items[n].Vorlauf;
                    int ruecklauf = pspZuordnung.items[n].Ruecklauf;

                    int vPuffer, rPuffer;
                    if (PufferSpCtrl.TemperaturenLesen(psp.items[0].ID, out vPuffer, out rPuffer))
                    {
                        vorlauf = vPuffer;
                        ruecklauf = rPuffer;
                    }

                    puffer_wp = new SimulationPufferspeicher();
                    puffer_wp.Bezeichner = psp.items[0].Name;
                    puffer_wp.Erzeuger = "Wärmepumpe";
                    // Konzept 6.6: Rolle und Speicher-ID wandern in die Ergebniszeile
                    // und bilden den technischen Serienschlüssel der Anzeigen (13.3).
                    puffer_wp.ID_Pufferspeicher = psp.items[0].ID;
                    puffer_wp.Verwendung = SimulationPufferspeicher.VERWENDUNG_HEIZUNG;
                    puffer_wp.Init(psp.items[0].Gesamtvolumen,
                                   vorlauf,
                                   ruecklauf,
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

            // ***********************************************************************
            // Nachlauf (Paket 7): Kennzahlen aller beteiligten Speicher aus ihren
            // Ganglinien bilden (SOC_Mittel/SOC_Max/Vollzyklen, Konzept 6.6) und die
            // Eingangsgrößen der VDI-4640-Auslegungsprüfung bereitstellen (13.1).
            // Beides ist reine Auswertung - es verändert kein Simulationsergebnis.
            // ***********************************************************************
            foreach (SimulationPufferspeicher sp in AlleSpeicher())
                sp.KennzahlenBerechnen();

            ErdreichAuswertung.AusLauf(this);
        }

        /// <summary>
        /// Alle am Lauf beteiligten Speicher in stabiler Reihenfolge: erst der
        /// Senkenspeicher der Wärmepumpe (Alias <see cref="puffer_wp"/>), danach die
        /// Quellspeicher der WP-Module in Modulreihenfolge.
        ///
        /// Das ist die EINE Quelle der Wahrheit für Ergebnis-Persistenz
        /// (Tab_ErgebnisPufferspeicher), Navigator-Serien, CSV-Export und die
        /// Ergebnistabelle der Detailansicht (Konzept 6.6/13.3).
        /// </summary>
        public System.Collections.Generic.List<SimulationPufferspeicher> AlleSpeicher()
        {
            var liste = new System.Collections.Generic.List<SimulationPufferspeicher>();
            if (puffer_wp != null) liste.Add(puffer_wp);

            if (simulation_wp != null && simulation_wp.Quellspeicher != null)
                foreach (SimulationPufferspeicher q in simulation_wp.Quellspeicher)
                    if (q != null && !liste.Contains(q)) liste.Add(q);

            return liste;
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
            // Kapazität des Pendelspeichers aus dem Volumen in LITERN (Etappe 3):
            //   Liter · 1,163 Wh/(l·K) · 20 K / 1000 = Liter · 20 / 860 [kWh]
            // Formelgleich zur Altfassung "m³ · 20000 / 860", weil die Migration den
            // Alt-Parameter mit dem Faktor 1000 in Liter überführt hat: 800 l ergeben
            // 16000/860 = 18,60 kWh, genau wie 0,8 · 20000/860. Die Zwischenprodukte
            // (Liter · 20 bzw. m³ · 20000) sind gleich groß und in float exakt
            // darstellbar, das Ergebnis ist damit bitgleich.
            //
            // ACHTUNG, bewusster Verhaltensunterschied: das Feld war IMMER int. Der
            // Alt-Parameter in m³ wurde deshalb auf ganze Kubikmeter abgeschnitten
            // ((int)0,8 = 0, (int)1,5 = 1) - die Nachkommastelle der Eingabe war
            // wirkungslos. In Litern verschwindet dieser Effekt; Projekte mit einem
            // Alt-Wert, der keine ganze Zahl war, rechnen jetzt mit dem eingegebenen
            // Volumen statt mit dem abgeschnittenen.
            simulation_bhkw.kapazitaetPendelspeicher = (float)VolumenPendelspeicherBHKW * 20 / 860;
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

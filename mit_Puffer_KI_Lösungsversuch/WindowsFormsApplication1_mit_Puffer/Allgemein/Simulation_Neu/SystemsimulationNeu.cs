using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Neues, gekoppeltes Anlagenmodell. Erbt von SimulationControl und wird per Checkbox
    /// (chk_ModellNeu) anstelle des Bestandsmodells ausgefuehrt. Es nutzt DIESELBEN
    /// validierten Einzelsimulationen (simulation_wp, simulation_bhkw, simulation_solarthermie,
    /// simulation_spk) und schreibt in dieselben Ergebnisfelder, damit Uebersicht,
    /// ErgebnisModel und Charts unveraendert weiterlaufen.
    ///
    /// Unterschied zum Bestand: ein einziger, dem Projekt zugeordneter Pufferspeicher
    /// (PufferProjekt) speichert den bisher verworfenen Solar-Ueberschuss und verdraengt
    /// damit Spitzenkessel-Waerme. Zusaetzlich stehen Vorlauf-/Ruecklauf-Auslegung und
    /// Bivalenzpunkt als Randbedingungen bereit.
    /// </summary>
    public class SystemsimulationNeu : SimulationControl
    {
        // Neue Randbedingungen (aus dem Tab "Modell/Randbedingungen")
        public double VorlaufAusleg = 55;
        public double RuecklaufAusleg = 40;
        public double Bivalenz = -5;

        // Der eine gemeinsame Projekt-Puffer
        public PufferProjekt puffer = new PufferProjekt();

        // --- Ergebnisgroessen der Puffer-Kopplung ---
        public float KapazitaetPuffer_kWh = 0f;
        public double PufferGenutzt_MWh = 0;      // Solar-Ueberschuss, der ueber den Puffer Kessel verdraengt
        public double PufferVerluste_MWh = 0;
        public double BoilerEinsparung_MWh = 0;   // verdraengte Kessel-Nutzwaerme

        public override void Do_Simulation(int ID_Projekt)
        {
            // Gemeinsamen Projekt-Puffer aufbauen: identische Kapazitaetsformel wie im Bestand
            // (SimulationControl.Simulation_BHKW_Ctrl: Volumen * 20000 / 860).
            puffer.Kapazitaet_kWh = (float)VolumenPendelspeicherBHKW * 20000f / 860f;
            puffer.VorlaufSoll = (float)VorlaufAusleg;
            puffer.Ruecklauf = (float)RuecklaufAusleg;
            puffer.Reset();

            // Basis-Kaskade der validierten Einzelsimulationen ausfuehren.
            // Erzeuger-Physik, DB-Reads und Ergebnisbelegung bleiben unveraendert.
            base.Do_Simulation(ID_Projekt);

            // Kopplung ueber den gemeinsamen Puffer aktiv schalten.
            KoppleUeberGemeinsamenPuffer();
        }

        /// <summary>
        /// Kopplungsphysik des neuen Modells auf Basis der stuendlichen Arrays der
        /// Einzelsimulationen:
        ///   - simulation_solarthermie.Ueberschuss[h]  (bisher verworfener Solar-Ueberschuss)
        ///   - simulation_spk.Kesselleistung_stuendlich[h] (stuendliche Spitzenkessel-Last)
        ///
        /// Der gemeinsame Puffer speichert den Solar-Ueberschuss chronologisch und gibt ihn
        /// in spaeteren Stunden ab, um Kessel-Last zu verdraengen. Die dadurch eingesparte
        /// Kessel-Nutzwaerme wird anteilig aus Brennstoff- und Emissions-Aggregaten des
        /// Kessels herausgerechnet und dem Solar-Ergebnis gutgeschrieben. Betrifft nur die
        /// simulation_*-Instanzen DIESES (neuen) Laufs; das Bestandsmodell bleibt unberuehrt.
        /// </summary>
        private void KoppleUeberGemeinsamenPuffer()
        {
            KapazitaetPuffer_kWh = puffer.Kapazitaet_kWh;
            PufferGenutzt_MWh = 0;
            PufferVerluste_MWh = 0;
            BoilerEinsparung_MWh = 0;

            if (!bSimulationSolarthermie || simulation_solarthermie == null) return;
            if (puffer.Kapazitaet_kWh <= 0f) return;

            double[] solarUeberschuss = simulation_solarthermie.Ueberschuss;

            // Kessel-Stundenlast (kWh) nur, wenn ein Kessel gerechnet wurde.
            float[] kesselLast = (bSimulationKessel && simulation_spk != null)
                ? simulation_spk.Kesselleistung_stuendlich
                : null;

            double genutzt = 0; // kWh Solar-Ueberschuss, der Kesselwaerme verdraengt

            puffer.Reset();
            for (int h = 0; h < 8760; h++)
            {
                // 1) Solar-Ueberschuss dieser Stunde in den gemeinsamen Puffer laden
                double surplus = (solarUeberschuss != null && h < solarUeberschuss.Length) ? solarUeberschuss[h] : 0;
                if (surplus > 0) puffer.Laden((float)surplus);

                // 2) gespeicherte Waerme verdraengt Kessel-Last dieser Stunde
                if (kesselLast != null && h < kesselLast.Length)
                {
                    double last = kesselLast[h];
                    if (last > 0)
                    {
                        float geliefert = puffer.Entladen((float)last);
                        genutzt += geliefert;
                    }
                }

                // 3) stehende Verluste des Puffers
                float vorher = puffer.Inhalt_kWh;
                puffer.Verluste();
                PufferVerluste_MWh += (vorher - puffer.Inhalt_kWh);
            }

            PufferGenutzt_MWh = genutzt / 1000.0;
            PufferVerluste_MWh /= 1000.0;

            // Wirkung des gemeinsamen Puffers auf die Ergebnisbilanz:
            // verdraengte Kesselwaerme anteilig aus den Kessel-Aggregaten herausrechnen.
            if (kesselLast != null && simulation_spk.S_Waerme_spk > 0 && PufferGenutzt_MWh > 0)
            {
                double boiler = simulation_spk.S_Waerme_spk;               // MWh Kessel-Nutzwaerme
                double verdraengt = Math.Min(PufferGenutzt_MWh, boiler);
                BoilerEinsparung_MWh = verdraengt;
                double faktor = (boiler - verdraengt) / boiler;           // verbleibender Kessel-Anteil

                simulation_spk.S_Waerme_spk         *= faktor;
                simulation_spk.Gasverbrauch_SPK     *= faktor;
                simulation_spk.Oelverbrauch_SPK     *= faktor;
                simulation_spk.Rapsoelverbrauch_SPK *= faktor;
                simulation_spk.Holzverbrauch_SPK    *= faktor;
                simulation_spk.Sonstigverbrauch_SPK *= faktor;
                simulation_spk.Koks_SPK             *= faktor;
                simulation_spk.Kohle_SPK            *= faktor;
                simulation_spk.Pellets_SPK          *= faktor;
                simulation_spk.TierischeFette_SPK   *= faktor;
                simulation_spk.Em_CO2_SPK           *= faktor;
                simulation_spk.Em_CO_SPK            *= faktor;
                simulation_spk.Em_SO2_SPK           *= faktor;
                simulation_spk.Em_NOX_SPK           *= faktor;
                simulation_spk.Em_Staub_SPK         *= faktor;

                // Verdraengte Waerme dem Solar-Ergebnis als effektiv genutzte Waerme gutschreiben
                // (Umrechnung MWh -> kWh, da die Solar-Summen in kWh gefuehrt werden).
                simulation_solarthermie.Waermeproduktion_gesamt += verdraengt * 1000.0;
                simulation_solarthermie.Ueberschuss_summe       -= verdraengt * 1000.0;
                if (simulation_solarthermie.Ueberschuss_summe < 0) simulation_solarthermie.Ueberschuss_summe = 0;

                // Systemweite Restwaerme bleibt unveraendert (Kessel + Puffer decken dieselbe Last).
            }
        }
    }
}

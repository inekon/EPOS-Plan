using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Sammelt nach einem In-Memory-Simulationslauf die Stundenreihen (8760) für die
    /// Ganglinien-Diagramme des Berichts ein (Konzept Kap. 6.2).
    ///
    /// Grundsätze (aus der Engine-Analyse, 11.08.2026):
    ///  - Alle Reihen sind öffentliche Felder der Simulationsobjekte; fehlt ein Gewerk,
    ///    sind sie durchgehend 0 (nie null) — maßgeblich sind die bSimulation*-Flags.
    ///  - Mehrere Reihen sind ARRAY-REFERENZEN auf andere (Aliasing) → hier wird
    ///    grundsätzlich kopiert.
    ///  - Viertelstundenreihen (35040, mittlere Leistung kW) werden per arithmetischem
    ///    Mittel auf Stunden gebracht (Stundenmittel kW = Stundenenergie kWh).
    ///  - Einheiten der abgelegten Reihen: Energie kWh je Stunde, SOC in kWh, °C.
    /// </summary>
    public static class ZeitreihenExtraktor
    {
        public static ZeitreihenSatz AusLauf(SimulationRunner runner)
        {
            if (runner == null || runner.sim == null) return null;
            var z = new ZeitreihenSatz();
            SimulationControl sim = runner.sim;

            try
            {
                // Bedarf und Temperatur (immer vorhanden).
                z.Reihen[ZeitreihenSatz.WAERMEBEDARF] = D(runner.simulation_Waermebedarf.Waermebedarf);
                z.Reihen[ZeitreihenSatz.TEMPERATUR] = D(runner.simulation_Waermebedarf.Stundentemperatur);
                z.Reihen[ZeitreihenSatz.STROMBEDARF] =
                    Stunden(sim, runner.simulation_Strombedarf.Strombedarf_viertelStundenwerte);

                // Wärmeerzeuger.
                if (sim.bSimulationWP && sim.simulation_wp != null)
                {
                    z.Reihen[ZeitreihenSatz.WP_WAERME] = D(sim.simulation_wp.WP_Waermeproduktion_stuendlich);
                    z.Reihen[ZeitreihenSatz.WP_STROM] = D(sim.simulation_wp.WP_Strombedarf_stuendlich);
                    z.Reihen[ZeitreihenSatz.HEIZSTAB] = D(sim.simulation_wp.Heizstab_stuendlich);
                }
                if (sim.bSimulationBHKW && sim.simulation_bhkw != null)
                {
                    z.Reihen[ZeitreihenSatz.BHKW_WAERME] = D(sim.simulation_bhkw.waermeproduktion);
                    z.Reihen[ZeitreihenSatz.BHKW_STROM] = D(sim.simulation_bhkw.stromproduktion);
                }
                if (sim.bSimulationKessel && sim.simulation_spk != null)
                    z.Reihen[ZeitreihenSatz.KESSEL_WAERME] = D(sim.simulation_spk.Kesselleistung_stuendlich);
                if (sim.bSimulationSolarthermie && sim.simulation_solarthermie != null)
                    z.Reihen[ZeitreihenSatz.SOLAR_WAERME] = D(sim.simulation_solarthermie.Waermeproduktion);

                // Strom: PV und Netz.
                if (sim.bSimulationPV && sim.simulation_pv != null)
                {
                    z.Reihen[ZeitreihenSatz.PV_GENUTZT] = D(sim.simulation_pv.Stromproduktion);
                    z.Reihen[ZeitreihenSatz.PV_UEBERSCHUSS] = D(sim.simulation_pv.Ueberschuss);
                }

                // Stromspeicher: seit AP2b eigenes Gewerk mit eigenem Flag - der SOC
                // hing bis dahin am PV-Objekt (simulation_pv.Speicherfuellstand).
                if (sim.bSimulationSSP)
                    z.Reihen[ZeitreihenSatz.PV_SPEICHER_SOC] = D(sim.Speicherfuellstand_stuendlich);

                z.Reihen[ZeitreihenSatz.NETZBEZUG] = Stunden(sim, sim.Rest_Strombedarf_viertelstuendlich);

                // Restwärme (Referenz des letzten Gewerks → Kopie zwingend).
                z.Reihen[ZeitreihenSatz.WAERMEREST] = D(sim.Rest_Waermebedarf_stuendlich);

                // Thermischer Pufferspeicher (nur wenn zugeordnet).
                if (sim.puffer_wp != null && sim.puffer_wp.SOC_stuendlich != null)
                    z.Reihen[ZeitreihenSatz.PUFFER_SOC] = D(sim.puffer_wp.SOC_stuendlich);
            }
            catch
            {
                // Ganglinien sind Komfort — ein Extraktionsfehler kippt den Bericht nicht.
            }

            return z.Reihen.Count > 0 ? z : null;
        }

        // float[] → double[] (Kopie; Aliasing-sicher).
        private static double[] D(float[] q)
        {
            if (q == null) return null;
            var r = new double[q.Length];
            for (int i = 0; i < q.Length; i++) r[i] = q[i];
            return r;
        }

        private static double[] D(double[] q)
        {
            if (q == null) return null;
            var r = new double[q.Length];
            Array.Copy(q, r, q.Length);
            return r;
        }

        // 35040 → 8760 über das Stundenmittel (kW-Mittel = kWh je Stunde);
        // 8760er-Eingaben werden nur kopiert.
        private static double[] Stunden(SimulationControl sim, float[] viertel)
        {
            if (viertel == null) return null;
            if (viertel.Length == ZeitreihenSatz.Stunden) return D(viertel);
            try { return D(sim.Viertelstunden_zu_Stundenwerte_Mittelwert(viertel)); }
            catch
            {
                // Fallback: eigenes Mittel.
                int n = viertel.Length / 4;
                var r = new double[n];
                for (int h = 0; h < n; h++)
                    r[h] = (viertel[h * 4] + viertel[h * 4 + 1] + viertel[h * 4 + 2] + viertel[h * 4 + 3]) / 4.0;
                return r;
            }
        }
    }
}

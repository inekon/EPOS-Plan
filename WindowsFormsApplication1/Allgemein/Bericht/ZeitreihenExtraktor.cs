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

                    // V2 (PV-Konzept § 2.3, Etappe P1): Die Einspeisereihe ist der
                    // Überschuss NACH der Speicherladung — geladene Energie wirkt als
                    // vermiedener Netzbezug, nicht als Einspeisung. Ladung je Stunde =
                    // Summe der vier Viertelstunden (LadungAcKwh der SpeicherEngine).
                    double[] pvUeb = D(sim.simulation_pv.Ueberschuss);
                    if (sim.Speicherergebnis != null &&
                        sim.Speicherergebnis.LadungAcKwh != null &&
                        sim.Speicherergebnis.LadungAcKwh.Length == pvUeb.Length * 4)
                    {
                        double[] ladung = sim.Speicherergebnis.LadungAcKwh;
                        for (int h = 0; h < pvUeb.Length; h++)
                        {
                            double lad = ladung[h * 4] + ladung[h * 4 + 1] +
                                         ladung[h * 4 + 2] + ladung[h * 4 + 3];
                            pvUeb[h] = Math.Max(0, pvUeb[h] - lad);
                        }
                    }
                    z.Reihen[ZeitreihenSatz.PV_UEBERSCHUSS] = pvUeb;

                    // V1: BHKW-Überschuss als eigene Reihe — er stand bis P1 in der
                    // PV-Überschussreihe (falsches Etikett).
                    if (sim.simulation_pv.BhkwUeberschuss_gesamt > 0.5f)
                        z.Reihen[ZeitreihenSatz.BHKW_UEBERSCHUSS] =
                            D(sim.simulation_pv.BhkwUeberschuss);
                }

                // Stromspeicher: seit AP2b eigenes Gewerk mit eigenem Flag - der SOC
                // hing bis dahin am PV-Objekt (simulation_pv.Speicherfuellstand).
                if (sim.bSimulationSSP)
                    z.Reihen[ZeitreihenSatz.PV_SPEICHER_SOC] = D(sim.Speicherfuellstand_stuendlich);

                z.Reihen[ZeitreihenSatz.NETZBEZUG] = Stunden(sim, sim.Rest_Strombedarf_viertelstuendlich);

                // Restwärme (Referenz des letzten Gewerks → Kopie zwingend).
                z.Reihen[ZeitreihenSatz.WAERMEREST] = D(sim.Rest_Waermebedarf_stuendlich);

                // Thermische Speicher — PAKET E1 (Konzept 6.3, Befund S-1): JE SPEICHER
                // eine Reihe unter dem technischen Serienschlüssel PUFFER_<ID> bzw.
                // QUELLE_<AnlagenID>, statt einer einzigen Reihe aus dem Alias
                // sim.puffer_wp (dem ersten Heizungspuffer). Quelle ist dieselbe
                // Speicherliste, aus der sich auch Ergebnis-Persistenz, Navigator und
                // CSV-Export speisen (Konzept 6.6/13.3, eine Quelle der Wahrheit).
                {
                    var speicher = sim.AlleSpeicher();
                    for (int i = 0; i < speicher.Count; i++)
                    {
                        SimulationPufferspeicher sp = speicher[i];
                        if (sp == null || sp.SOC_stuendlich == null) continue;

                        string schluessel = sp.Schluessel(i);
                        if (z.Reihen.ContainsKey(schluessel)) continue;   // je Speicher genau eine Reihe

                        z.Reihen[schluessel] = D(sp.SOC_stuendlich);
                        z.Speicherreihen.Add(schluessel);
                        z.Beschriftungen[schluessel] = sp.Anzeige();
                    }
                }
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

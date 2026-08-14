using System;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Headless-Simulationslauf für ein Projekt – ohne Form_Simulation_Detail zu öffnen.
    ///
    /// Kapselt exakt die Nicht-UI-Logik aus Form_Simulation_Detail:
    ///  - Konfiguration lesen (Tab_Einstellungen), Energiebedarf (Wärme/Strom) rechnen,
    ///    SimulationControl konfigurieren und Do_Simulation ausführen,
    ///  - aus den Simulationsobjekten ein ErgebnisModel bauen und über ErgebnisCtrl speichern
    ///    (letztes Ergebnis je Projekt -> ErgebnisCtrl.Save ersetzt das bisherige).
    ///
    /// Für jeden Projektlauf am besten eine neue Instanz verwenden (frische Simulationsobjekte).
    /// Aufruf: int erg = new SimulationRunner().SimuliereUndSpeichere(idProjekt, out string fehler);
    /// </summary>
    public class SimulationRunner
    {
        // Gleiche Feldnamen wie in Form_Simulation_Detail, damit der Ergebnisaufbau 1:1 passt.
        public SimulationWaermebedarf simulation_Waermebedarf = new SimulationWaermebedarf();
        public SimulationStrombedarf simulation_Strombedarf = new SimulationStrombedarf();
        public SimulationControl sim = new SimulationControl();

        /// <summary>
        /// Führt die komplette Simulation für ein Projekt aus (ohne UI).
        /// Rückgabe false + Fehlertext, wenn Konfiguration oder Klimaregion fehlen.
        /// </summary>
        public bool Simuliere(int idProjekt, out string fehler)
        {
            fehler = null;

            // Engine-Einstieg: Blockade bei nicht abgeschlossener Schema-Migration
            // (ADR-001, Aufgabe 6). Auf einem halb migrierten Schema zu rechnen liefert
            // stillschweigend falsche Ergebnisse - lieber sauber abbrechen.
            string sperrgrund;
            if (SchemaMigration.SimulationGesperrt(out sperrgrund))
            {
                fehler = sperrgrund;
                return false;
            }

            // Konfiguration des Projekts lesen.
            KonfigurationCtrl ctrl = new KonfigurationCtrl();
            ctrl.ReadSingle("select * from Tab_Einstellungen where ID_Projekt=" + idProjekt);
            if (ctrl.rows == 0)
            {
                fehler = "Für Projekt " + idProjekt + " ist keine Konfiguration (Tab_Einstellungen) hinterlegt.";
                return false;
            }

            // Netzverluste prüfen.
            int netzverluste = (int)ctrl.m_Netzverluste;
            if (ctrl.m_szNetzverlusteEinheit == "%" && netzverluste > 100)
            {
                fehler = "Die Netzverluste dürfen nicht größer als 100 % sein.";
                return false;
            }

            // Klimaregion aus dem Projekt.
            ProjektCtrl projektCtrl = new ProjektCtrl();
            projektCtrl.ReadSingle(idProjekt);
            int nKlimaregion = projektCtrl.m_ID_Klimaregion;
            if (nKlimaregion == 0)
            {
                fehler = "Für Projekt " + idProjekt + " ist keine Klimaregion gesetzt.";
                return false;
            }

            // Tool-Auswahl (wie in Form_Simulation_Detail.btn_Simulation_Click).
            string[] tool = new string[6];
            tool[0] = ctrl.model.m_Tool_1;
            tool[1] = ctrl.model.m_Tool_2;
            tool[2] = ctrl.model.m_Tool_3;
            tool[3] = ctrl.model.m_Tool_4;
            tool[4] = ctrl.model.m_Tool_5;
            tool[5] = ctrl.model.m_Tool_6;

            // Energiebedarf: Wärme- und Strombedarf rechnen (ohne Diagramm-/Textbox-Ausgabe).
            simulation_Waermebedarf.Netzverluste = netzverluste;
            simulation_Waermebedarf.Netzverluste_Einheit = ctrl.m_szNetzverlusteEinheit;
            simulation_Waermebedarf.Waermebedarf_berechnen(idProjekt, nKlimaregion);

            simulation_Strombedarf.m_ID_Projekt = idProjekt;
            simulation_Strombedarf.Berechnung(idProjekt);

            // SimulationControl konfigurieren (BHKW-Parameter kommen aus der Konfiguration
            // statt aus den UI-Steuerelementen: Leistungsgrenze/Betriebsart). Das Volumen
            // des Pendelspeichers kommt seit Etappe 3 aus dem Projekt-Puffer
            // "BHKW-Pendelspeicher" in LITERN; Tab_Einstellungen.Pendelspeicher (m³) wird
            // nicht mehr gelesen.
            sim.tool = tool;
            sim.Stundentemperatur = simulation_Waermebedarf.Stundentemperatur;
            sim.simulation_Waermebedarf = simulation_Waermebedarf;
            sim.simulation_Strombedarf = simulation_Strombedarf;
            sim.ctrl_konfig = ctrl;
            sim.GrenzleistungBHKW = (int)ctrl.model.Leistungsgrenze;
            sim.VolumenPendelspeicherBHKW = PufferSpCtrl.PendelspeicherVolumenLiter(idProjekt);
            sim.modeBHKW = ctrl.model.Betriebsart;

            // Erzeuger-Simulationen (WP, Kessel, BHKW, Solar, PV, Speicher).
            sim.Do_Simulation(idProjekt);
            return true;
        }

        /// <summary>
        /// Baut aus den übergebenen Simulationsobjekten ein ErgebnisModel (eine Quelle der Wahrheit,
        /// wird auch von Form_Simulation_Detail.SpeichereErgebnis genutzt). Ohne UI/MessageBox.
        /// </summary>
        public static ErgebnisModel BaueErgebnis(int idProjekt,
            SimulationWaermebedarf simulation_Waermebedarf,
            SimulationStrombedarf simulation_Strombedarf,
            SimulationControl sim)
        {
            ProjektCtrl pc = new ProjektCtrl();
            pc.ReadSingle(idProjekt);

            ErgebnisModel m = new ErgebnisModel();
            m.ID_Projekt = idProjekt;
            m.ID_Klimaregion = pc.m_ID_Klimaregion;
            m.Bezeichner = "Simulation " + pc.m_szProjektname;

            // Welche Simulationsarten dieser Lauf enthaelt.
            m.Sim_Energiebedarf = true;
            m.Sim_Waermepumpe = sim.bSimulationWP;
            m.Sim_Heizkessel = sim.bSimulationKessel;
            m.Sim_Solarthermie = sim.bSimulationSolarthermie;
            m.Sim_BHKW = sim.bSimulationBHKW;
            m.Sim_PV = sim.bSimulationPV;
            m.Sim_Stromspeicher = sim.bSimulationSSP;

            // Detail: Waerme-/Strombedarf (immer vorhanden).
            m.Energiebedarf = new ErgebnisEnergiebedarfModel();
            m.Energiebedarf.Waermebedarf_Gesamt = simulation_Waermebedarf.Waermebedarf_Gesamt;
            m.Energiebedarf.Waermelast_Max = simulation_Waermebedarf.Waermebedarf_Max;
            m.Energiebedarf.Strombedarf_Gesamt = simulation_Strombedarf.Strombedarf_gesamt;
            m.Energiebedarf.Strombedarf_Max = simulation_Strombedarf.Strombedarf_Max;
            m.Energiebedarf.Waermerestbedarf = sim.Restwaerme;   // Restwärmebedarf nach allen Erzeugern
            m.Energiebedarf.Stromrestbedarf = sim.Reststrom;     // Reststrombedarf/Netzbezug

            // Detail: Waermepumpe (nur wenn gerechnet), Werte wie in der WP-Ansicht (MWh).
            if (sim.bSimulationWP)
            {
                SimulationWaermepumpe wp = sim.simulation_wp;
                ErgebnisWaermepumpeModel w = new ErgebnisWaermepumpeModel();
                w.Waermebedarf = wp.Waermebedarf_gesamt / 1000.0;
                w.Waermeproduktion_WP = wp.WP_Waermeproduktion_gesamt / 1000.0;
                w.Stromverbrauch_WP = wp.WP_Strombedarf_gesamt / 1000.0;
                w.Stromverbrauch_Heizstab = wp.Heizstab_gesamt / 1000.0;
                // B0-7a: Restbedarf aus der Stundenganglinie statt aus der Differenzformel —
                // die alte Formel ignorierte Speichereffekte und zog zudem den Heizstab
                // (Stromgröße) von einer Wärmemenge ab. Quelle ist dieselbe Größe,
                // die auch die Detailansicht anzeigt (waermerestbedarf_gesamt).
                w.Restwaermebedarf = wp.waermerestbedarf_gesamt / 1000.0;
                w.Kapazitaet_Pufferspeicher = wp.Volumen_Pufferspeicher * 1.16;
                w.Vollbenutzungsstunden = (wp.wp_list.Count > 0) ? wp.WP_Laufzeit / wp.wp_list.Count : 0;
                w.Bivalenzpunkt = (wp.Bivalenzpunkt != -100) ? (double?)wp.Bivalenzpunkt : null;

                // Minimale Spitzenkesselleistung = max. stuendlicher Waermerestbedarf.
                double maxSpk = 0;
                for (int i = 0; i < wp.waermerestbedarf_stuendlich.Length; i++)
                    if (wp.waermerestbedarf_stuendlich[i] > maxSpk) maxSpk = wp.waermerestbedarf_stuendlich[i];
                w.Min_Spitzenkesselleistung = maxSpk;

                // B0-7b: Waermebedarfsdeckung (%) restbedarfsbasiert als EIGENANTEIL der
                // WP-Stufe: (Stufeneingang - Rest) / Gesamtbedarf. Bericht und
                // Wirtschaftlichkeit addieren die Erzeugeranteile zu 100 % — eine Differenz
                // gegen den Gesamtbedarf würde vorgelagerte Erzeuger doppelt zählen, wenn
                // die WP nicht an erster Kaskadenposition steht. Mit WP an erster Stelle
                // identisch zur Detailansicht; die alte produktionsbasierte Formel zählte
                // Speicherladung als Deckung.
                double basis = simulation_Waermebedarf.Waermebedarf_Gesamt;
                if (basis > 0)
                {
                    double deckung = (w.Waermebedarf - w.Restwaermebedarf) / basis * 100.0;
                    if (deckung > 100) deckung = 100;
                    if (deckung < 0) deckung = 0;
                    w.Waermebedarfsdeckung = deckung;
                }

                // Modulauflistung.
                for (int i = 0; i < wp.wp_list.Count; i++)
                {
                    ErgebnisWaermepumpeModulModel mo = new ErgebnisWaermepumpeModulModel();
                    mo.Modul = wp.WP_Modul[i];
                    mo.Leistung = (i < wp.wp_model.Count) ? wp.wp_model[i].Grenzleistung : 0;
                    mo.Waermeproduktion = wp.Modul_WP_Waermeproduktion[i] / 1000.0;
                    mo.Stromverbrauch = wp.Modul_WP_Strombedarf[i] / 1000.0;
                    mo.Heizstab = wp.Modul_Heizstab[i] / 1000.0;
                    mo.Betriebsstunden = wp.Modul_WP_Laufzeit[i];
                    w.Module.Add(mo);
                }

                m.Waermepumpe = w;
            }

            // Detail: BHKW (nur wenn gerechnet). Werte wie in der BHKW-Ergebnisansicht (MWh/a).
            if (sim.bSimulationBHKW && sim.simulation_bhkw != null)
            {
                SimulationBHKW bh = sim.simulation_bhkw;
                ErgebnisBHKWModel b = new ErgebnisBHKWModel();

                double waermebedarfMWh = bh.waermebedarf.Sum() / 1000.0;
                double strombedarfMWh = bh.strombedarf.Sum() / 1000.0;
                float[] restwaermeBhkw = sim.SubVectors(bh.waermebedarf, bh.waermeproduktion);

                b.Waermebedarf = waermebedarfMWh;
                b.Restwaermebedarf = restwaermeBhkw.Sum() / 1000.0;
                b.Strombedarf = strombedarfMWh;
                b.Reststrombedarf = strombedarfMWh - bh.Stromproduktion_BHKW_MWh;
                b.Waermeproduktion = bh.Waermeproduktion_BHKW_MWh;
                b.Waermeueberschuss = bh.Waermeueberschuss / 1000.0;
                b.Stromproduktion = bh.Stromproduktion_BHKW_MWh;
                b.Betriebsstunden_Gesamt = bh.Betriebsstunden;
                b.Betriebsstunden_Durchschnitt = bh.dLaufzeiten;
                b.Waermebedarfsdeckung = (simulation_Waermebedarf.Waermebedarf_Gesamt > 0)
                    ? bh.Waermeproduktion_BHKW_MWh * 100.0 / simulation_Waermebedarf.Waermebedarf_Gesamt : 0;
                b.Strombedarfsdeckung = (simulation_Strombedarf.Strombedarf_gesamt > 0)
                    ? bh.Stromproduktion_BHKW_MWh * 100.0 / simulation_Strombedarf.Strombedarf_gesamt : 0;
                //b.Gasverbrauch_Hu = bh.Gasverbrauch_BHKW;

                if (bh.Gasverbrauch_BHKW > 0)
                {
                    b.Gasverbrauch = bh.Gasverbrauch_BHKW;
                }

                if (bh.Oelverbrauch_BHKW > 0)
                {
                    b.Oelverbrauch = bh.Oelverbrauch_BHKW;
                }

                if (bh.Holzmenge_BHKW > 0)
                {
                    b.Holzverbrauch = bh.Holzmenge_BHKW;
                }

                if (bh.Pellets_BHKW > 0)
                {
                    b.Pellets = bh.Pellets_BHKW;
                }

                if (bh.Rapsoelverbrauch_BHKW > 0)
                {      
                    b.Rapsoelverbrauch = bh.Rapsoelverbrauch_BHKW;
                }
                
                if (bh.TierischeFette_BHKW > 0)
                {
                    b.TierischeFette = bh.TierischeFette_BHKW;
                }
                
                if (bh.Koks_BHKW > 0)
                {
                    b.Koks = bh.Koks_BHKW;
                }
                
                if (bh.Kohle_BHKW > 0)
                {
                    b.Kohle = bh.Kohle_BHKW;
                }
                
                if (bh.Sonstigemenge_BHKW > 0)
                {
                    b.Sonstigverbrauch = bh.Sonstigemenge_BHKW;
                }

                // Modulauflistung (wie dataGridView_BHKW).
                for (int i = 0; i < bh.bhkw_list.Count; i++)
                {
                    ErgebnisBHKWModulModel mo = new ErgebnisBHKWModulModel();
                    mo.Modul = bh.bhkw_list_Namen[i] ?? "Standard BHKW";
                    mo.Waermeproduktion = bh.s_waerme_MWh[i];
                    mo.Stromproduktion = bh.s_strom_MWh[i];
                    b.Module.Add(mo);
                }

                m.BHKW = b;
            }

            // Detail: Heizkessel/Spitzenkessel (nur wenn gerechnet). Werte wie in der Kessel-Ansicht.
            if (sim.bSimulationKessel && sim.simulation_spk != null)
            {
                var spk = sim.simulation_spk;
                ErgebnisHeizkesselModel h = new ErgebnisHeizkesselModel();
                h.Waermebedarf = spk.Waermebedarf_gesamt;
                h.Waermeproduktion = spk.S_Waerme_spk;
                h.Restwaermebedarf = spk.Waermebedarf_gesamt - spk.S_Waerme_spk;
                h.Strombedarf = spk.Strombedarf_gesamt / 1000.0;
                h.Reststrombedarf = spk.Strombedarf_gesamt / 1000.0 + spk.Stromverbrauch_Spk;
                h.Stromverbrauch = spk.Stromverbrauch_Spk;
                h.Waermebedarfsdeckung = (simulation_Waermebedarf.Waermebedarf_Gesamt > 0)
                    ? spk.S_Waerme_spk * 100.0 / simulation_Waermebedarf.Waermebedarf_Gesamt : 0;
                h.Maximale_Kesselleistung = spk.Maximale_Kesselleistung_Spk;
                h.Gasspitze = spk.Gasspitze_Spk;
                h.Gasverbrauch = spk.Gasverbrauch_SPK;
                h.Oelverbrauch = spk.Oelverbrauch_SPK;
                h.Koks = spk.Koks_SPK;
                h.Rapsoelverbrauch = spk.Rapsoelverbrauch_SPK;
                h.Holzverbrauch = spk.Holzverbrauch_SPK;
                h.Kohle = spk.Kohle_SPK;
                h.Sonstigverbrauch = spk.Sonstigverbrauch_SPK;
                h.Pellets = spk.Pellets_SPK;
                h.TierischeFette = spk.TierischeFette_SPK;

                // Modulauflistung (wie listView_SimSPK).
                for (int i = 0; i < spk.spk_list.Count(); i++)
                {
                    ErgebnisHeizkesselModulModel mo = new ErgebnisHeizkesselModulModel();
                    mo.Modul = spk.spk_list[i];
                    mo.Waerme_Gas = spk.s_waerme_Gas_Spk[i];
                    mo.Waerme_Oel = spk.s_waerme_Oel_Spk[i];
                    mo.Jahresnutzungsgrad = spk.Kessel_Jahresnutzungsgrad_Spk[i];
                    h.Module.Add(mo);
                }

                m.Heizkessel = h;
            }

            // Detail: Solarthermie (nur wenn gerechnet). Werte wie in der Solarthermie-Ansicht.
            if (sim.bSimulationSolarthermie && sim.simulation_solarthermie != null)
            {
                var st = sim.simulation_solarthermie;
                ErgebnisSolarthermieModel stm = new ErgebnisSolarthermieModel();
                stm.Waermebedarf = st.Waermebedarf_gesamt / 1000.0;
                stm.Waermeproduktion = st.Waermeproduktion_gesamt / 1000.0;
                stm.Restwaermebedarf = (st.Waermebedarf_gesamt - st.Waermeproduktion_gesamt) / 1000.0;
                stm.Waermebedarfsdeckung = (st.Waermebedarf_gesamt > 0)
                    ? st.Waermeproduktion_gesamt * 100.0 / st.Waermebedarf_gesamt : 0;
                stm.Ueberschuss = st.Ueberschuss_summe / 1000.0;

                if (st.Kollektor_Ergebnisse != null)
                    foreach (SolarKollektorErgebnis k in st.Kollektor_Ergebnisse)
                        stm.Module.Add(new ErgebnisSolarthermieModulModel
                        {
                            Modul = k.Name,
                            Flaeche = k.Flaeche,
                            Anzahl = k.Anzahl,
                            Waermeproduktion = k.Waermeproduktion / 1000.0,
                            Ueberschuss = k.Ueberschuss / 1000.0
                        });

                m.Solarthermie = stm;
            }

            // Detail: Photovoltaik (nur wenn gerechnet). Werte wie in der PV-Ansicht.
            if (sim.bSimulationPV && sim.simulation_pv != null)
            {
                var pvs = sim.simulation_pv;
                ErgebnisPhotovoltaikModel pvm = new ErgebnisPhotovoltaikModel();
                pvm.Stromproduktion = pvs.Stromproduktion.Sum() / 1000.0;
                pvm.Ueberschuss = pvs.Ueberschuss.Sum() / 1000.0;
                pvm.Strombedarf = pvs.Strombedarf.Sum() / 4000.0;
                pvm.Reststrombedarf = sim.Rest_Strombedarf_viertelstuendlich.Sum() / 4000.0;
                pvm.Strombedarfsdeckung = (pvs.Strombedarf_stuendlich.Sum() > 0)
                    ? pvs.Stromproduktion.Sum() * 100.0 / pvs.Strombedarf_stuendlich.Sum() : 0;
                pvm.MaxSolareLeistung = pvs.MaxPSolar;

                if (pvs.Modul_Ergebnisse != null)
                    foreach (PVModulErgebnis p in pvs.Modul_Ergebnisse)
                        pvm.Module.Add(new ErgebnisPhotovoltaikModulModel
                        {
                            Modul = p.Name,
                            Flaeche = p.Flaeche,
                            Anzahl = p.Anzahl,
                            Stromproduktion = p.Stromproduktion / 1000.0
                        });

                m.Photovoltaik = pvm;
            }

            return m;
        }

        /// <summary>
        /// Führt die Simulation aus und schreibt das Ergebnis neu.
        /// Rückgabe: neue Ergebnis-Kopf-ID (&gt; 0) oder -1 bei Fehler (Fehlertext in 'fehler').
        /// </summary>
        public int SimuliereUndSpeichere(int idProjekt, out string fehler)
        {
            if (!Simuliere(idProjekt, out fehler))
                return -1;

            ErgebnisModel m = BaueErgebnis(idProjekt, simulation_Waermebedarf, simulation_Strombedarf, sim);
            int id = new ErgebnisCtrl().Save(m);
            if (id <= 0) fehler = "Das Simulationsergebnis konnte nicht gespeichert werden.";
            return id;
        }
    }
}

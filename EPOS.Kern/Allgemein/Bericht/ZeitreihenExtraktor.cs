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
                // E26 (Befund N3): der Bedarf aller Verbraucher vor jeder Eigenerzeugung —
                // die Bezugsgröße der Strommatrix (vermiedene Menge, KWK-Split).
                if (sim.Strombedarf_Verbraucher_viertelstuendlich != null)
                    z.Reihen[ZeitreihenSatz.STROMBEDARF_GESAMT] =
                        Stunden(sim, sim.Strombedarf_Verbraucher_viertelstuendlich);

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
                {
                    z.Reihen[ZeitreihenSatz.KESSEL_WAERME] = D(sim.simulation_spk.Kesselleistung_stuendlich);
                    Kesselbetriebswerte(sim, z);
                }
                if (sim.bSimulationSolarthermie && sim.simulation_solarthermie != null)
                    z.Reihen[ZeitreihenSatz.SOLAR_WAERME] = D(sim.simulation_solarthermie.Waermeproduktion);

                // Strom: PV und Netz.
                if (sim.bSimulationPV && sim.simulation_pv != null)
                {
                    z.Reihen[ZeitreihenSatz.PV_GENUTZT] = D(sim.simulation_pv.Stromproduktion);

                    if (sim.Speicherflottennetzbilanz == null)
                    {
                    // V2 (PV-Konzept § 2.3, Etappe P1) mit SB1 (a) und PV3: Die Einspeisereihe ist
                    // der Überschuss NACH der Speicherladung und unter der Einspeisegrenze - je
                    // Viertelstunde gebildet (SimulationPV.EinspeisungAufteilen), dann zum
                    // Stundenmittel. Geladene Energie wirkt als vermiedener Netzbezug, nicht als
                    // Einspeisung; was über der Grenze bleibt, ist Abregelung.
                    sim.PvEinspeisungAufteilen(out double[] einspeisungKw, out double[] abregelungKw);
                    z.Reihen[ZeitreihenSatz.PV_UEBERSCHUSS] = Stunden(sim, einspeisungKw);
                    if (SimulationPV.ViertelstundenKwh(abregelungKw) > 0.5)
                    {
                        z.Reihen[ZeitreihenSatz.PV_ABREGELUNG] = Stunden(sim, abregelungKw);
                        z.Beschriftungen[ZeitreihenSatz.PV_ABREGELUNG] = "PV-Abregelung";
                    }
                    // V1: Der BHKW-Überschuss ist keine PV-Größe; seine Reihe setzt der
                    // gemeinsame Zweig unten (Einspeisung des Laufs, mit und ohne PV).
                    }
                }

                // Eine aktivierte Flotte führt alle Netzausgänge bereits nach ihrer
                // tatsächlichen Quelle. Die alte Näherung „PV-Überschuss minus gesamte
                // Speicherladung" würde Netz- und BHKW-Ladung falsch als PV abziehen
                // und Batterieexport als PV-Einspeisung etikettieren.
                if (sim.Speicherflottennetzbilanz != null)
                {
                    SpeicherFlottenNetzbilanz f = sim.Speicherflottennetzbilanz;
                    z.Reihen[ZeitreihenSatz.PV_UEBERSCHUSS] = Stunden(sim, f.PvNetzeinspeisungKw);
                    z.Reihen[ZeitreihenSatz.BHKW_UEBERSCHUSS] = Stunden(sim, f.BhkwNetzeinspeisungKw);
                    z.Reihen[ZeitreihenSatz.BATTERIE_EINSPEISUNG] = Stunden(sim, f.BatterieNetzeinspeisungKw);
                    z.Reihen[ZeitreihenSatz.NETZEINSPEISUNG] = Stunden(sim, f.NetzeinspeisungKw);
                    z.Reihen[ZeitreihenSatz.PV_ABREGELUNG] = Stunden(sim, f.PvAbregelungKw);
                    z.Beschriftungen[ZeitreihenSatz.PV_UEBERSCHUSS] = "PV-Einspeisung";
                    z.Beschriftungen[ZeitreihenSatz.BHKW_UEBERSCHUSS] = "BHKW-Einspeisung";
                    z.Beschriftungen[ZeitreihenSatz.BATTERIE_EINSPEISUNG] = "Batterie-Einspeisung";
                    z.Beschriftungen[ZeitreihenSatz.NETZEINSPEISUNG] = "Netzeinspeisung gesamt";
                    z.Beschriftungen[ZeitreihenSatz.PV_ABREGELUNG] = "PV-Abregelung";
                }

                // Die BHKW-Einspeisung ohne Flotte, mit und ohne Photovoltaik: das Stundenmittel
                // der Viertelstundenbilanz des Laufs (SimulationControl.BhkwEinspeisungDesLaufs).
                // Dieselbe Reihe lesen BHKW-Reiter und Kennzahl; der KWK-Split der Strommatrix
                // liest sie aus diesem Satz. Mit Flotte steht oben die Flottenbilanz.
                // Die Schwelle 0,5 kWh/a bleibt: Ohne nennenswerten Überschuss erscheinen weder
                // Spalte noch Reihe im Bericht; die Matrix rechnet dann alles als Eigenstrom
                // (Unterschied zur Kennzahl höchstens 0,5 kWh/a, unter jeder Anzeigestelle).
                if (sim.Speicherflottennetzbilanz == null)
                {
                    double[] einspeisung = sim.BhkwEinspeisungDesLaufs();
                    double summe = 0;
                    if (einspeisung != null) foreach (double w in einspeisung) summe += w;
                    if (summe > 0.5)
                        z.Reihen[ZeitreihenSatz.BHKW_UEBERSCHUSS] = einspeisung;
                }

                // Katalog v12: die Stromlast des BHKW-Reiters — Strombedarf am BHKW und Reststrombedarf je Stunde,
                // dieselben Reihen wie das Bild der Seite (SimulationErgebnisCtrl.BhkwStromStunden); Stromproduktion
                // und Einspeisung stehen oben schon als BHKW_STROM und BHKW_UEBERSCHUSS.
                if (sim.bSimulationBHKW && sim.simulation_bhkw != null)
                {
                    SimulationErgebnisCtrl.BhkwStromreihen bs = SimulationErgebnisCtrl.BhkwStromStunden(sim);
                    if (bs != null)
                    {
                        z.Reihen[ZeitreihenSatz.BHKW_STROMBEDARF] = bs.Strombedarf;
                        z.Reihen[ZeitreihenSatz.BHKW_RESTSTROM] = bs.Reststrombedarf;
                    }
                }

                // Stromspeicher: seit AP2b eigenes Gewerk mit eigenem Flag - der SOC
                // hing bis dahin am PV-Objekt (simulation_pv.Speicherfuellstand).
                if (sim.bSimulationSSP)
                    z.Reihen[ZeitreihenSatz.PV_SPEICHER_SOC] = D(sim.Speicherfuellstand_stuendlich);

                // SP1 (Welle M5): der Eigenverbrauch des Speichersystems - Standby aus PV und Netz,
                // im Flottenpfad der Hilfsverbrauch der Einheiten. Nur mit Eigenverbrauch > 0,5 kWh.
                if (sim.bSimulationSSP && sim.SpeichersystemEigenverbrauchKwh > 0.5)
                {
                    double[] eigen = SpeichersystemEigenverbrauchKw(sim);
                    if (eigen != null)
                    {
                        z.Reihen[ZeitreihenSatz.SPEICHER_EIGENVERBRAUCH] = Stunden(sim, eigen);
                        z.Beschriftungen[ZeitreihenSatz.SPEICHER_EIGENVERBRAUCH] = "Eigenverbrauch Speichersystem";
                    }
                }

                z.Reihen[ZeitreihenSatz.NETZBEZUG] = Stunden(sim, sim.Rest_Strombedarf_viertelstuendlich);

                // Die BEZUGSSPITZE entsteht aus der VIERTELSTUNDENreihe, nicht aus der
                // Zeile darüber: Das Stundenmittel glättet die Spitze, und bepreist
                // wird die gemessene Viertelstundenleistung. Dieselbe Reihe, die der
                // Speicher kappt — deshalb misst die Zahl den Effekt der Kappung.
                z.Bezugsspitze = Netzbezugsspitze.AusReihe(sim.Rest_Strombedarf_viertelstuendlich);

                // ENTSCHEID E35 (Konzept Gebäudesimulation N1.40): Ein eigener Zähler des
                // Kältestroms trägt den Leistungspreis seines Kühlträgers auf SEINE Spitze — sie
                // entsteht hier aus der Stundenreihe der Anlage (je Stunde dieselbe Leistung in
                // jeder Viertelstunde, die Viertelstundenspitze ist also die Stundenspitze).
                // Anlagen, deren Kältestrom durch die Stufenrechnung läuft, stehen in der
                // Bezugsspitze oben und bekommen keinen Eintrag.
                Kaeltekaskade kaskade = runner.simulation_Kaeltebedarf != null
                    ? runner.simulation_Kaeltebedarf.Kaskade : null;
                if (kaskade != null && kaskade.Erzeuger != null)
                {
                    // KU3-4d: die Kältemaschinen unter ihrem eigenen Schlüssel - der Platz in der Ergebnisliste
                    // Kaeltemaschinen (dieselbe Folge wie im SimulationRunner).
                    int km = 0;
                    foreach (Kaelteerzeuger e in kaskade.Erzeuger)
                    {
                        if (e == null) continue;
                        int schluessel = e.Maschine != null ? Kaeltestromabrechnung.SchluesselKaeltemaschine(km++) : e.Modulindex;
                        if (!e.NebenDerStufenrechnung || (e.Maschine == null && e.Modulindex < 0)) continue;
                        Netzbezugsspitze s = Netzbezugsspitze.AusReihe(e.Strom_stuendlich);
                        if (s != null) z.Kaeltestromspitzen[schluessel] = s;
                    }
                }

                // Katalog v12: die Kälteproduktion des Kältereiters — je Kälteerzeuger die gedeckte Kälte, dazu die
                // ungedeckte Kälte; dieselben Reihen wie das Bild der Seite (KaelteProduktionBild.AusLauf). Nur,
                // wenn das Projekt Kälte rechnet; der Kältebedarf steht als Kanalreihe BEDARF_KUEHLUNG im Satz.
                Kaeltereihen(runner, z);

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

                        // PAKET P1 (Konzept 7.4): zusätzlich die beiden
                        // SCHICHTTEMPERATUREN je Speicher, unter den abgeleiteten
                        // Serienschlüsseln PUFFER_<ID>_TOBEN und _TUNTEN (sprachneutral
                        // und ASCII, Schicht 2 der Drei-Schichten-Regel).
                        //
                        // Sie kommen BEWUSST NICHT in z.Speicherreihen: Diese Liste
                        // führt das Füllstandsdiagramm (ChartRenderer.Speicherverlauf),
                        // und eine Temperaturreihe in kWh-Achse wäre dort sinnlos. Als
                        // Reihe im Satz stehen sie dem CSV-Export und einem künftigen
                        // Temperaturdiagramm zur Verfügung.
                        //
                        // Quellspeicher tragen keine Schichttemperatur (Konzept 8.2) —
                        // ihre Ganglinie bleibt 0 und wird deshalb nicht ausgewiesen.
                        //
                        // PAKET L (P2-O1): Nachsilben und Legendentexte kommen aus den
                        // Konstanten bzw. dem Ressourcenkatalog statt als Zeichenketten
                        // im Code — dieselben Werte, nur an EINER Stelle definiert.
                        if (!sp.IstQuelle && sp.T_oben_Mittel.HasValue)
                        {
                            string oben = schluessel + ZeitreihenSatz.SUFFIX_T_OBEN;
                            string unten = schluessel + ZeitreihenSatz.SUFFIX_T_UNTEN;

                            z.Reihen[oben] = D(sp.T_oben_stuendlich);
                            z.Reihen[unten] = D(sp.T_unten_stuendlich);
                            z.Beschriftungen[oben] =
                                sp.BezeichnerAnzeige() + " " + MyResource.Resource.SIM_REIHE_T_OBEN;
                            z.Beschriftungen[unten] =
                                sp.BezeichnerAnzeige() + " " + MyResource.Resource.SIM_REIHE_T_UNTEN;
                        }
                    }
                }

                // PAKET B1 (Konzept 8.2/8.4): Die QUELLTEMPERATUR eines
                // temperaturgekoppelten Erzeugers ist ein LAUFERGEBNIS, kein
                // Eingangswert mehr — je gekoppeltem Modul eine Reihe unter dem
                // sprachneutralen Serienschlüssel QUELLTEMP_<AnlagenID> (Schicht 2 der
                // Drei-Schichten-Regel). Ungekoppelte Module bekommen KEINE Reihe: Ihre
                // Quelltemperatur steht unverändert in der Konfiguration und wäre hier
                // eine zweite Wahrheit.
                //
                // Bewusst NICHT in z.Speicherreihen — diese Liste führt das
                // kWh-Füllstandsdiagramm (dieselbe Begründung wie bei den
                // PUFFER_*_TOBEN-Reihen aus Paket P1).
                Quelltemperaturreihen(sim, z);

                // PAKET E2 (Nachtrag zu Konzept 4.4): Bedarf UND Deckung je Kanal.
                Kanalreihen(sim, runner, z);
            }
            catch
            {
                // Ganglinien sind Komfort — ein Extraktionsfehler kippt den Bericht nicht.
            }

            return z.Reihen.Count > 0 ? z : null;
        }

        /// <summary>
        /// <b>Der Betrieb je Heizkessel</b> (Konzept Kesselkennlinie 5, Bericht): Jahresnutzungsgrad, Brennwertanteil nach
        /// Stunden und Wärme und Starts. Keine Rechnung hier — die Zeilen sind die des Kessel-Reiters
        /// (<see cref="SimulationErgebnisCtrl.Heizkessel"/>), einmal gerufen: Bericht und Oberfläche nennen dieselben Zahlen.
        /// </summary>
        private static void Kesselbetriebswerte(SimulationControl sim, ZeitreihenSatz z)
        {
            SimulationErgebnisCtrl.HeizkesselErgebnis kessel = SimulationErgebnisCtrl.Heizkessel(sim, null);
            if (kessel == null) return;
            foreach (SimulationErgebnisCtrl.KesselModulZeile m in kessel.Module)
                z.Kessel.Add(new Kesselbetrieb(m.Name, m.JahresnutzungsgradProzent, m.MitBrennwertkennlinie,
                                               m.BrennwertStundenProzent, m.BrennwertWaermeProzent, m.Starts));
        }

        /// <summary>
        /// PAKET E2 (Nachtrag zu Konzept 4.4) — die KANALREIHEN: je Bedarfskanal seine
        /// Bedarfsganglinie und, je gerechnetem Erzeuger, die Ganglinie seiner Deckung
        /// AUF DIESEM KANAL.
        ///
        /// <para><b>Aufgenommen wird nur, was es gibt.</b> Ein Kanal ohne Bedarf bekommt
        /// keine Reihe — und damit auch keine Deckungsreihen. In einem Projekt ohne
        /// Prozesswärme wächst der Satz deshalb um drei bis acht Reihen, nicht um
        /// achtzehn. Dieselbe Präsenzregel, mit der die Oberfläche ihre Kanalauswahl
        /// aufbaut.</para>
        ///
        /// <para><b>Kein Bilanzduplikat.</b> Die Werte sind die Auflösung der Größen, die
        /// seit Paket E1 als Jahressummen in <c>Tab_Ergebnis*</c> stehen — dieselbe
        /// Buchführung, nur nach Stunden. Der Bericht rechnet daraus nichts nach.</para>
        /// </summary>
        private static void Kanalreihen(SimulationControl sim, SimulationRunner runner, ZeitreihenSatz z)
        {
            if (sim == null || runner == null || runner.simulation_Waermebedarf == null) return;

            for (int k = 0; k < Kanal.ANZAHL; k++)
            {
                double[] bedarf = SimulationControl.BedarfKanalStuendlich(runner.simulation_Waermebedarf, k);

                double summe = 0;
                for (int h = 0; h < bedarf.Length; h++) summe += bedarf[h];
                if (summe <= 0) continue;                 // Kanal ohne Bedarf: keine Reihe

                z.Reihen[ZeitreihenSatz.BedarfSchluessel(k)] = D(bedarf);

                if (sim.bSimulationWP && sim.simulation_wp != null)
                {
                    Kanalreihe(z, "WAERMEPUMPE", k, sim.DeckungKanalStuendlich(ProjektPuffer.TYP_WP, k));
                    Kanalreihe(z, "HEIZSTAB", k, sim.HeizstabKanalStuendlich(k));
                }
                if (sim.bSimulationKessel && sim.simulation_spk != null)
                    Kanalreihe(z, "HEIZKESSEL", k, sim.DeckungKanalStuendlich(ProjektPuffer.TYP_KESSEL, k));
                if (sim.bSimulationSolarthermie && sim.simulation_solarthermie != null)
                    Kanalreihe(z, "SOLARTHERMIE", k, sim.DeckungKanalStuendlich(ProjektPuffer.TYP_SOLARTHERMIE, k));
                if (sim.bSimulationBHKW && sim.simulation_bhkw != null)
                    Kanalreihe(z, "BHKW_WAERME", k, sim.DeckungKanalStuendlich(ProjektPuffer.TYP_BHKW, k));
            }
        }

        /// <summary>
        /// Katalog v12: die Kältedeckung je Kälteerzeuger (<see cref="ZeitreihenSatz.KAELTE_PRAEFIX"/>, in der Folge der
        /// Kältekaskade, Beschriftung = Bezeichner der Wärmepumpe) und die ungedeckte Kälte
        /// (<see cref="ZeitreihenSatz.KAELTEREST"/>). Keine Rechnung — die Reihen sind die des Kältereiters
        /// (<see cref="KaelteProduktionBild.AusLauf"/>); ohne Kälte bleibt der Satz unberührt.
        /// </summary>
        private static void Kaeltereihen(SimulationRunner runner, ZeitreihenSatz z)
        {
            KaelteProduktionBild.Reihen r = KaelteProduktionBild.AusLauf(runner.simulation_Kaeltebedarf);
            if (r == null) return;
            for (int i = 0; i < r.Erzeuger.Count; i++)
            {
                string schluessel = ZeitreihenSatz.KAELTE_PRAEFIX + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                z.Reihen[schluessel] = r.Erzeuger[i].Werte;
                z.Beschriftungen[schluessel] = r.Erzeuger[i].Name;
                z.Kaeltereihen.Add(schluessel);
            }
            z.Reihen[ZeitreihenSatz.KAELTEREST] = r.Rest;
        }

        /// <summary>Eine Deckungsreihe eintragen — nur, wenn sie überhaupt Werte trägt.</summary>
        private static void Kanalreihe(ZeitreihenSatz z, string erzeuger, int kanal, double[] werte)
        {
            if (werte == null) return;

            double summe = 0;
            for (int h = 0; h < werte.Length; h++) summe += werte[h];
            if (summe <= 0) return;

            z.Reihen[ZeitreihenSatz.DeckungSchluessel(erzeuger, kanal)] = D(werte);
        }

        /// <summary>
        /// PAKET B1 — die Quelltemperatur-Ganglinien der temperaturgekoppelten Erzeuger
        /// (Wärmepumpe UND Heizkessel, Konzept 8.2/8.4).
        ///
        /// <para>Der Schlüssel trägt die ANLAGEN-ID, nicht die Modulnummer: Sie ist über
        /// den Lauf hinweg stabil und dieselbe, die <c>QUELLE_&lt;AnlagenID&gt;</c> für
        /// den Quellspeicher benutzt.</para>
        /// </summary>
        private static void Quelltemperaturreihen(SimulationControl sim, ZeitreihenSatz z)
        {
            if (sim.bSimulationWP && sim.simulation_wp != null)
            {
                var profile = sim.simulation_wp.Quelltemperaturen;
                var anlagen = sim.simulation_wp.wp_list;

                for (int i = 0; i < profile.Count && i < anlagen.Count; i++)
                {
                    if (!sim.simulation_wp.QuelleGekoppelt(i) || profile[i] == null) continue;
                    ReiheQuelltemperatur(z, anlagen[i], profile[i],
                                         sim.simulation_wp.WP_Modul[i]);
                }
            }

            if (sim.bSimulationKessel && sim.simulation_spk != null)
            {
                var anlagen = sim.simulation_spk.spk_anlagen_ids;

                for (int i = 0; i < anlagen.Count; i++)
                {
                    double[] reihe = sim.simulation_spk.Quelltemperaturen(i);
                    if (reihe == null) continue;
                    ReiheQuelltemperatur(z, anlagen[i], reihe,
                                         sim.simulation_spk.KesselName(i));
                }
            }
        }

        private static void ReiheQuelltemperatur(ZeitreihenSatz z, int idAnlage,
                                                 double[] werte, string bezeichner)
        {
            if (idAnlage <= 0 || werte == null) return;

            string schluessel = ZeitreihenSatz.QUELLTEMP_PRAEFIX + idAnlage;
            if (z.Reihen.ContainsKey(schluessel)) return;

            z.Reihen[schluessel] = D(werte);
            z.Beschriftungen[schluessel] =
                (string.IsNullOrEmpty(bezeichner) ? schluessel : bezeichner) +
                " " + MyResource.Resource.SIM_REIHE_QUELLTEMPERATUR;
        }

        // Kopie einer Reihe (Aliasing-sicher: mehrere Felder der Simulation zeigen auf
        // dasselbe Array). Bis W8-O-5d gab es hier zwei Ueberladungen, float[] und double[];
        // seit der Kern durchgehend in double rechnet, bleibt eine.
        /// <summary>
        /// Der Eigenverbrauch des Speichersystems je Viertelstunde [kW] (SP1): Standby aus PV plus aus dem
        /// Netz, im Flottenpfad der Hilfsverbrauch der Einheiten; <c>null</c> ohne Reihe.
        /// </summary>
        private static double[] SpeichersystemEigenverbrauchKw(SimulationControl sim)
        {
            if (sim.SpeichersystemStandbyAusPvKw != null && sim.SpeichersystemStandbyAusNetzKw != null &&
                sim.SpeichersystemStandbyAusPvKw.Length == sim.SpeichersystemStandbyAusNetzKw.Length)
            {
                var r = new double[sim.SpeichersystemStandbyAusPvKw.Length];
                for (int i = 0; i < r.Length; i++)
                    r[i] = sim.SpeichersystemStandbyAusPvKw[i] + sim.SpeichersystemStandbyAusNetzKw[i];
                return r;
            }
            var intervalle = sim.Speicherflottenergebnis?.Variante?.Intervalle;
            if (intervalle == null || intervalle.Count == 0) return null;
            var f = new double[intervalle.Count];
            for (int i = 0; i < f.Length; i++) f[i] = intervalle[i].HilfsverbrauchKw;
            return f;
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
        private static double[] Stunden(SimulationControl sim, double[] viertel)
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

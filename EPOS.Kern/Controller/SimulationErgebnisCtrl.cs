using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Zahlen der Ergebnisreiter — als DTO je Erzeuger (iU9-W11a.3).
    ///
    /// <para><b>Warum es diesen Controller gibt.</b> <c>Form_Simulation_Detail</c> rechnete
    /// die angezeigten Kennzahlen SELBST: 13 Übersichtsfelder, sechs Summen, fünf
    /// Eigenanteile, sechs Modultabellen — rund 600 Zeilen Fachrechnung in einer Maske
    /// (Vermessung § 1c, „Berechnungen in der Maske"). Fünf dieser Rechnungen bezeichnete
    /// der Quelltext selbst als „wortgleich mit <c>SimulationRunner</c>". Zwei Kopien
    /// einer Fachformel laufen beim ersten Fachwechsel auseinander; deshalb ruft dieser
    /// Controller die geteilten Methoden des Runners
    /// (<see cref="SimulationRunner.EigenanteilWpMwh"/> und Geschwister) statt sie
    /// abzuschreiben.
    ///
    /// <para><b>Statisch und ohne Datenbank.</b> Alles hier ist eine Abbildung eines
    /// gerechneten <see cref="SimulationControl"/> auf Anzeigegrößen. Wer eine
    /// Datenbankauskunft braucht (Erdreichhinweise, Brennstoffarten), holt sie beim
    /// zuständigen Controller und reicht sie herein.</para>
    ///
    /// <para><b>Zahlen, keine Texte.</b> Die DTO führen <c>double</c> und <c>string</c>
    /// aus dem Rechenkern; Formatierung, Einheiten und Beschriftungen bleiben bei der
    /// Oberfläche. Einzige Ausnahme sind die Namen, die schon im Lauf stehen
    /// (Modulbezeichner, Speicherrolle).</para>
    ///
    /// <para><b>Drei behobene Befunde.</b> W11-B15 (Vollbenutzungsstunden ohne
    /// Nullprüfung → ∞), W11-B16 (Mindest-Spitzenkesselleistung über 8 750 statt 8 760
    /// Stunden) und W11-B22 (PV-Deckungsgrad ohne Nullprüfung → NaN) sind hier in der
    /// Fassung des Runners umgesetzt, die alle drei nicht hat.</para>
    /// </summary>
    public static class SimulationErgebnisCtrl
    {
        // =================================================================
        //  Übersicht
        // =================================================================

        /// <summary>
        /// Die 13 Felder des Übersicht-Reiters und die sechs Summen des Ergebnisblocks.
        /// </summary>
        public sealed class UebersichtKennzahlen
        {
            // --- die 13 Felder (FuelleUebersicht :3764-3784) ---
            public double StrombedarfGesamtMwh;
            public double WaermebedarfGesamtMwh;
            /// <summary>Der Restwärmebedarf des LAUFS (<c>SimulationControl.Restwaerme</c>) —
            /// dieselbe Größe, die <c>SimulationRunner.BaueErgebnis</c> nach
            /// <c>Tab_Ergebnis</c> schreibt.</summary>
            public double RestwaermeMwh;
            public double ReststromMwh;
            public double WpWaermeproduktionMwh;
            public double WpStromverbrauchMwh;
            public double KesselWaermeproduktionMwh;
            public double HeizstabStromverbrauchMwh;
            public double KesselStromverbrauchMwh;
            public double BhkwWaermeproduktionMwh;
            public double BhkwStromproduktionMwh;
            public double SolarWaermeproduktionMwh;
            public double PvStromproduktionMwh;

            // --- die sechs Summen (Ergebnisblock und Eigenanteilsraster) ---
            //
            // ANWENDERENTSCHEID 04.09.2026 (W11a-O-1): Sie führen die DECKUNG je
            // Erzeuger, nicht die Produktion — Direktdeckung plus die zugerechnete
            // Speicherentladung, je Kanal, genau die Summanden, aus denen auch das
            // Eigenanteilsraster seine drei Kanalspalten bildet. Damit gilt
            // „Bedarf − Summe Deckung = Restwärme ≥ 0" PER KONSTRUKTION.
            public double WaermeKesselMwh;
            public double WaermeWpMwh;
            public double WaermeHeizstabMwh;
            public double WaermeSolarMwh;
            public double WaermeBhkwMwh;
            public double WaermeGesamtMwh;
            /// <summary>
            /// Der Restwärmebedarf — dieselbe Zahl wie <see cref="RestwaermeMwh"/>.
            ///
            /// <para><b>Anwenderentscheid 04.09.2026 (W11a-O-1).</b> Bis dahin stand hier
            /// <c>Projektwärmebedarf − Summe der PRODUKTION</c>, und das konnte NEGATIV
            /// werden: Geladene Speicherwärme steht in der Produktion und deckt trotzdem
            /// keinen Bedarf (Projekt 1030: −1,76 MWh). Eine negative Restwärme darf
            /// rechnerisch nicht entstehen — sie zeigt eine falsche Zuordnung zu den
            /// Erzeugern. Geklemmt wird sie deshalb NICHT; die Übersicht führt EINE
            /// Restwärmezahl, und das ist die Bilanzgröße des Laufs
            /// (<c>SimulationControl.Restwaerme</c>, gespeichert als
            /// <c>Tab_Ergebnis.Waermerestbedarf</c>).</para>
            ///
            /// <para>Übersteigt die Produktion eines Erzeugers seine Deckung, ist das ein
            /// ÜBERSCHUSS (Feld <c>Wärmeüberschuss</c>, wie beim BHKW) — nicht
            /// Restwärme.</para>
            /// </summary>
            public double RestwaermebedarfMwh;
        }

        /// <summary>
        /// Die Übersichtszahlen eines gerechneten Laufs.
        ///
        /// <para><b>W11-B35 — die sechs Summen standen zweimal, mit zwei Unterschieden.</b>
        /// <c>Form_Simulation_Detail</c> :4720-4734 und <c>NavigatorUebersicht.SetControl</c>
        /// :266-275 rechneten dieselben Größen und wichen an zwei Stellen ab:</para>
        /// <list type="number">
        ///   <item><b>Die Kesselwärme.</b> Die Detailansicht summierte
        ///   <c>s_waerme_Gas_Spk[i] + s_waerme_Oel_Spk[i]</c> über die Kesselliste, der
        ///   Navigator nahm <c>S_Waerme_spk</c>. Das ist KEINE Abweichung: <c>S_Waerme_spk</c>
        ///   entsteht in <c>SimulationSPK.Bilanz_und_Nutzungsgrad</c> aus genau dieser Summe
        ///   über genau diese Liste. Zwei Wege, ein Wert — hier steht der kürzere.</item>
        ///   <item><b>Das BHKW.</b> Der Navigator zählt <c>waerme_bhkw</c> in die Summe, die
        ///   Detailansicht nicht. DAS ist die echte Abweichung. Genommen wird die
        ///   NAVIGATOR-Fassung: Das BHKW ist eine Kaskadenstufe wie die anderen, und
        ///   <c>SimulationControl.Restwaerme</c> — die Wahrheit des Referenzlaufs — zieht
        ///   seine Lieferung ebenfalls ab. Die Detailansicht wies ohne den Term für jedes
        ///   Projekt mit BHKW einen zu großen Rest aus (Projekt 1030: 734,46 MWh statt
        ///   −1,76 MWh; der Lauf selbst meldet 0,00 MWh).</item>
        /// </list>
        /// <para><b>W11a-O-1 ist entschieden (Anwender, 04.09.2026) — und damit ist der
        /// BHKW-Streit gegenstandslos geworden.</b> Die Summen führen seither die
        /// DECKUNG je Erzeuger und nicht die Produktion: Direktdeckung plus die
        /// zugerechnete Speicherentladung, je Kanal — genau die Summanden, aus denen
        /// <c>NavigatorUebersicht.FillTableWithData</c> seine Kanalspalten bildete. Der
        /// Grund: „Produktion" ist nicht „Deckung"; geladene Speicherwärme steht in der
        /// Produktion und deckt trotzdem keinen Bedarf, und deshalb konnte
        /// <c>Bedarf − Produktion</c> NEGATIV werden (Projekt 1030: −1,76 MWh). Eine
        /// negative Restwärme darf rechnerisch nicht entstehen — sie zeigt eine falsche
        /// Zuordnung zu den Erzeugern. Geklemmt wird deshalb nichts; gerechnet wird
        /// richtig, und der Rest ist die Bilanzgröße des Laufs.</para>
        ///
        /// <para><b>Zwei Zahlen werden eine.</b>
        /// <see cref="UebersichtKennzahlen.RestwaermebedarfMwh"/> ist seither identisch
        /// mit <see cref="UebersichtKennzahlen.RestwaermeMwh"/> — Übersichtsreiter und
        /// Ergebnisreiter zeigen denselben Wert, wie es der Entscheid verlangt.</para>
        /// </summary>
        public static UebersichtKennzahlen Uebersicht(SimulationControl sim,
                                                      SimulationWaermebedarf wb,
                                                      SimulationStrombedarf sb)
        {
            if (sim == null) return null;

            UebersichtKennzahlen u = new UebersichtKennzahlen();

            u.StrombedarfGesamtMwh = sb != null ? sb.Strombedarf_gesamt : 0.0;
            u.WaermebedarfGesamtMwh = wb != null ? wb.Waermebedarf_Gesamt : 0.0;
            u.RestwaermeMwh = sim.Restwaerme;
            u.ReststromMwh = sim.Reststrom;
            u.WpWaermeproduktionMwh = sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000.0;
            u.WpStromverbrauchMwh = sim.simulation_wp.WP_Strombedarf_gesamt / 1000.0;
            u.KesselWaermeproduktionMwh = sim.simulation_spk.S_Waerme_spk;
            u.HeizstabStromverbrauchMwh = sim.simulation_wp.Heizstab_gesamt / 1000.0;
            u.KesselStromverbrauchMwh = sim.simulation_spk.Stromverbrauch_Spk;
            u.BhkwWaermeproduktionMwh = sim.simulation_bhkw.Waermeproduktion_BHKW_MWh;
            u.BhkwStromproduktionMwh = sim.simulation_bhkw.Stromproduktion_BHKW_MWh;
            u.SolarWaermeproduktionMwh = sim.simulation_solarthermie.Waermeproduktion_gesamt / 1000.0;
            u.PvStromproduktionMwh = sim.simulation_pv.Stromproduktion_gesamt / 1000.0;

            // ANWENDERENTSCHEID 04.09.2026 (W11a-O-1): DECKUNG statt Produktion.
            // Dieselben Summanden, aus denen NavigatorUebersicht.FillTableWithData seine
            // Kanalspalten bildete - Direktdeckung plus zugerechnete Speicherentladung,
            // der Heizstab in EIGENER Zeile (er gehoert in der Ergebnispersistenz zur
            // Waermepumpe, auf dem Bildschirm bekommt er seine eigene).
            u.WaermeWpMwh = DeckungMwh(SimulationRunner.Summiere(
                sim.simulation_wp.Direktdeckung_Kanal, sim.simulation_wp.Speicherentladung_Kanal));
            u.WaermeHeizstabMwh = DeckungMwh(SimulationRunner.Summiere(
                sim.simulation_wp.Heizstab_Kanal));
            u.WaermeSolarMwh = DeckungMwh(SimulationRunner.Summiere(
                sim.simulation_solarthermie.Direktdeckung_Kanal,
                sim.simulation_solarthermie.Speicherentladung_Kanal));
            u.WaermeKesselMwh = DeckungMwh(SimulationRunner.Summiere(
                sim.simulation_spk.Direktdeckung_Kanal, sim.simulation_spk.Speicherentladung_Kanal));
            u.WaermeBhkwMwh = DeckungMwh(SimulationRunner.Summiere(
                sim.simulation_bhkw.Direktdeckung_Kanal, sim.simulation_bhkw.Speicherentladung_Kanal));

            u.WaermeGesamtMwh = u.WaermeKesselMwh + u.WaermeWpMwh + u.WaermeHeizstabMwh +
                                u.WaermeSolarMwh + u.WaermeBhkwMwh;

            // EINE Restwaermezahl: die Bilanzgroesse des Laufs. Sie ist per Konstruktion
            // nicht negativ, und sie ist dieselbe, die Tab_Ergebnis.Waermerestbedarf
            // fuehrt - Uebersichtsreiter und Ergebnisreiter zeigen damit denselben Wert.
            u.RestwaermebedarfMwh = u.RestwaermeMwh;

            return u;
        }

        /// <summary>
        /// Summe einer Kanalzeile [kWh] als [MWh] — die Umrechnung, die auch
        /// <c>NavigatorUebersicht.Zeile</c> je Kanalspalte vornahm.
        /// </summary>
        private static double DeckungMwh(double[] kanalKwh)
        {
            double summe = 0.0;
            if (kanalKwh == null) return summe;

            foreach (double k in kanalKwh) summe += k;
            return summe / 1000.0;
        }

        // =================================================================
        //  Wärmepumpe
        // =================================================================

        /// <summary>Eine Zeile der WP-Modultabelle.</summary>
        public sealed record WpModulZeile(string Name, double GrenzleistungKw,
                                          double WaermeproduktionMwh, double StrombedarfMwh,
                                          double HeizstabMwh, double LaufzeitStunden);

        /// <summary>Eine Zeile der Pufferspeichertabelle (Konzept 6.6).</summary>
        public sealed record PufferZeile(string Bezeichner, string Rolle, double KapazitaetKwh,
                                         double LadungKwh, double EntladungKwh, double VerlusteKwh,
                                         double Vollzyklen, double FuellstandEndeProzent,
                                         bool IstKombi);

        public sealed class WaermepumpeErgebnis
        {
            public double DeckungProzent;
            public bool BivalenzpunktVorhanden;
            public double Bivalenzpunkt;
            public double StufeneingangMwh;
            public double RestwaermeMwh;
            public double StromverbrauchMwh;
            public double HeizstabStromverbrauchMwh;
            public double WaermeproduktionMwh;
            public double Vollbenutzungsstunden;
            public double MinSpkLeistungKw;
            public List<WpModulZeile> Module = new List<WpModulZeile>();
            public List<PufferZeile> Puffer = new List<PufferZeile>();
            /// <summary>
            /// Der Altausdruck der Rubrik ohne Speicher: <c>Volumen · 1,16</c>.
            ///
            /// <para><b>Geprüft, ob <c>ProjektPuffer.NutzbareKapazitaetKWh</c> passt: nein.</b>
            /// Die Kernformel aus iU9-W10a lautet <c>Volumen · 1,16 · ΔT / 1000</c> und
            /// braucht eine Spreizung; der Ausdruck der Maske hat weder ΔT noch die
            /// Division. Er ist damit keine Kapazität in kWh, sondern eine Altzeile — und
            /// bleibt deshalb WÖRTLICH stehen (offener Punkt W11a-O-1b im Protokoll).</para>
            /// </summary>
            public double PufferVolumenKwh;
            /// <summary>Kurztexte der VDI-4640-Auslegungsprüfung; leer ohne Erdreichquelle.</summary>
            public List<string> ErdreichHinweise = new List<string>();
            /// <summary>true, wenn mindestens ein Erdreichhinweis eine Warnung ist.</summary>
            public bool ErdreichWarnung;
        }

        /// <summary>
        /// Die Zahlen des Wärmepumpen-Reiters; <c>null</c>, wenn der Lauf keine
        /// Wärmepumpe hatte (<c>bSimulationWP</c>) — dann ist die Rubrik leer statt mit
        /// den Zahlen des Vorlaufs gefüllt.
        ///
        /// <para><b>Zwei Befunde behoben.</b> W11-B15: Die Vollbenutzungsstunden teilten
        /// ohne Nullprüfung durch <c>wp_list.Count</c> — bei leerer Liste ∞. W11-B16: Die
        /// Mindest-Spitzenkesselleistung lief über <c>i &lt; 8750</c> und ließ die letzten
        /// zehn Jahresstunden aus. Beide Größen stehen hier in der Fassung des Runners
        /// (<c>SimulationRunner</c> :290-298), der beide Fehler nicht hat.</para>
        ///
        /// <para>Die Pufferzeilen kommen aus <c>sim.AlleSpeicher()</c> und damit aus
        /// denselben Objekten, die <c>Tab_ErgebnisPufferspeicher</c> speisen.</para>
        /// </summary>
        /// <param name="erdreich">
        /// Die Erdreichauswertung des Projekts (<c>ErdreichAuswertung.FuerProjekt</c>).
        /// Sie liest die Datenbank und wird deshalb vom Aufrufer hereingereicht;
        /// <c>null</c> heißt „keine Hinweise".
        /// </param>
        public static WaermepumpeErgebnis Waermepumpe(SimulationControl sim,
                                                      SimulationWaermebedarf wb,
                                                      IEnumerable<ErdreichAuswertung.AnlageErgebnis> erdreich = null)
        {
            if (sim == null || !sim.bSimulationWP) return null;

            SimulationWaermepumpe wp = sim.simulation_wp;
            WaermepumpeErgebnis e = new WaermepumpeErgebnis();

            e.StufeneingangMwh = wp.Waermebedarf_gesamt / 1000.0;
            double eigen = SimulationRunner.EigenanteilWpMwh(wp);
            e.RestwaermeMwh = SimulationRunner.RestNachEigenanteil(e.StufeneingangMwh, eigen);
            e.DeckungProzent = SimulationRunner.DeckungProzent(
                eigen, wb != null ? wb.Waermebedarf_Gesamt : 0.0);

            e.BivalenzpunktVorhanden = wp.Bivalenzpunkt != -100;
            e.Bivalenzpunkt = wp.Bivalenzpunkt;

            e.StromverbrauchMwh = wp.WP_Strombedarf_gesamt / 1000.0;
            e.HeizstabStromverbrauchMwh = wp.Heizstab_gesamt / 1000.0;
            e.WaermeproduktionMwh = wp.WP_Waermeproduktion_gesamt / 1000.0;

            // W11-B15: Nullprüfung wie im Runner.
            e.Vollbenutzungsstunden = wp.wp_list.Count > 0 ? wp.WP_Laufzeit / wp.wp_list.Count : 0.0;

            // W11-B16: über die GANZE Ganglinie, nicht bis 8 750.
            double maxSpk = 0;
            for (int i = 0; i < wp.waermerestbedarf_stuendlich.Length; i++)
                if (wp.waermerestbedarf_stuendlich[i] > maxSpk) maxSpk = wp.waermerestbedarf_stuendlich[i];
            e.MinSpkLeistungKw = maxSpk;

            for (int i = 0; i < wp.wp_list.Count; i++)
                e.Module.Add(new WpModulZeile(
                    wp.WP_Modul[i],
                    wp.wp_model[i].Grenzleistung,
                    wp.Modul_WP_Waermeproduktion[i] / 1000.0,
                    wp.Modul_WP_Strombedarf[i] / 1000.0,
                    wp.Modul_Heizstab[i] / 1000.0,
                    wp.Modul_WP_Laufzeit[i]));

            e.Puffer.AddRange(Pufferzeilen(sim));
            e.PufferVolumenKwh = PufferVolumenKwh(sim);

            if (erdreich != null)
                foreach (ErdreichAuswertung.AnlageErgebnis a in erdreich)
                {
                    e.ErdreichHinweise.Add(a.Kurztext());
                    if ((a.Pruefung != null && a.Pruefung.Moeglich && a.Pruefung.Warnung) || a.FrostWarnung)
                        e.ErdreichWarnung = true;
                }

            return e;
        }

        /// <summary>
        /// Die Pufferspeicherzeilen eines Laufs — BEWUSST außerhalb von
        /// <see cref="Waermepumpe"/> auch einzeln erreichbar: Der Vorläufer füllte die
        /// Rubrik außerhalb von <c>if (sim.bSimulationWP)</c>, damit sie nach einem
        /// Folgelauf ohne Wärmepumpe GELEERT wird statt die Zahlen des Vorlaufs zu
        /// behalten.
        /// </summary>
        public static List<PufferZeile> Pufferzeilen(SimulationControl sim)
        {
            List<PufferZeile> zeilen = new List<PufferZeile>();
            if (sim == null) return zeilen;

            foreach (SimulationPufferspeicher sp in sim.AlleSpeicher())
                zeilen.Add(new PufferZeile(
                    sp.BezeichnerAnzeige(), sp.RolleAnzeige(), sp.Q_max,
                    sp.Ladung_gesamt, sp.Entladung_gesamt, sp.Verluste_gesamt,
                    sp.Vollzyklen, sp.SOC, sp.IstKombi));

            return zeilen;
        }

        /// <summary>
        /// Der ALTAUSDRUCK der Pufferzeile ohne Speicherliste: <c>Volumen · 1,16</c>
        /// (Form_Simulation_Detail :2446-2448).
        ///
        /// <para>Wie <see cref="Pufferzeilen"/> BEWUSST unabhaengig von der Waermepumpe:
        /// Der Vorlaeufer rief die Rubrik ausserhalb von <c>if (sim.bSimulationWP)</c>,
        /// damit sie nach einem Folgelauf ohne Waermepumpe geleert wird.</para>
        ///
        /// <para><b>Geprueft, ob <c>ProjektPuffer.NutzbareKapazitaetKWh</c> passt: nein.</b>
        /// Die Kernformel aus iU9-W10a lautet <c>Volumen · 1,16 · ΔT / 1000</c> und
        /// braucht eine Spreizung; hier fehlen ΔT und die Division. Der Ausdruck bleibt
        /// deshalb woertlich stehen — er ist keine Kapazitaet in kWh, sondern eine
        /// Altzeile (offener Punkt im W11a-Protokoll).</para>
        /// </summary>
        public static double PufferVolumenKwh(SimulationControl sim)
        {
            if (sim == null || sim.simulation_wp == null) return 0.0;
            return sim.simulation_wp.Volumen_Pufferspeicher * 1.16;
        }

        // =================================================================
        //  Heizkessel
        // =================================================================

        /// <summary>Eine Zeile der Kesseltabelle.</summary>
        public sealed record KesselModulZeile(string Name, double GasMwh, double OelMwh,
                                              double JahresnutzungsgradProzent);

        public sealed class HeizkesselErgebnis
        {
            public double DeckungProzent;
            public double StufeneingangMwh;
            public double RestwaermeMwh;
            public double WaermeproduktionMwh;
            public double StrombedarfMwh;
            public double ReststrombedarfMwh;
            public double GasMwh;
            public double OelMwh;
            public double KoksMwh;
            public double RapsoelMwh;
            public double HolzMwh;
            public double KohleMwh;
            public double StromMwh;
            public double SonstigeMwh;
            public double PelletsMwh;
            public double TierischeFetteMwh;
            public double MaxKesselleistungKw;
            public double GasspitzeKw;
            public double QuellwaermeMwh;
            public List<KesselModulZeile> Module = new List<KesselModulZeile>();
        }

        /// <summary>
        /// Die Zahlen des Heizkessel-Reiters; <c>null</c> ohne Kessel im Lauf.
        ///
        /// <para><b>W11-B19 behoben:</b> Der Vorläufer setzte <c>tb_Koks</c> ZWEIMAL mit
        /// demselben Wert (:4418 und :4425). Ein DTO-Feld gibt es nur einmal.</para>
        /// </summary>
        public static HeizkesselErgebnis Heizkessel(SimulationControl sim, SimulationWaermebedarf wb)
        {
            if (sim == null || !sim.bSimulationKessel) return null;

            SimulationSPK spk = sim.simulation_spk;
            HeizkesselErgebnis e = new HeizkesselErgebnis();

            double eigen = SimulationRunner.EigenanteilKesselMwh(spk);
            e.StufeneingangMwh = spk.Waermebedarf_gesamt;
            e.RestwaermeMwh = SimulationRunner.RestNachEigenanteil(e.StufeneingangMwh, eigen);
            e.DeckungProzent = SimulationRunner.DeckungProzent(
                eigen, wb != null ? wb.Waermebedarf_Gesamt : 0.0);

            e.WaermeproduktionMwh = spk.S_Waerme_spk;
            e.StrombedarfMwh = spk.Strombedarf_gesamt / 1000.0;
            e.ReststrombedarfMwh = spk.Strombedarf_gesamt / 1000.0 + spk.Stromverbrauch_Spk;

            e.GasMwh = spk.Gasverbrauch_SPK;
            e.OelMwh = spk.Oelverbrauch_SPK;
            e.KoksMwh = spk.Koks_SPK;
            e.RapsoelMwh = spk.Rapsoelverbrauch_SPK;
            e.HolzMwh = spk.Holzverbrauch_SPK;
            e.KohleMwh = spk.Kohle_SPK;
            e.StromMwh = spk.Stromverbrauch_Spk;
            e.SonstigeMwh = spk.Sonstigverbrauch_SPK;
            e.PelletsMwh = spk.Pellets_SPK;
            e.TierischeFetteMwh = spk.TierischeFette_SPK;

            e.MaxKesselleistungKw = spk.Maximale_Kesselleistung_Spk;
            e.GasspitzeKw = spk.Gasspitze_Spk;
            e.QuellwaermeMwh = spk.Quellwaerme_gesamt / 1000.0;

            for (int i = 0; i < spk.spk_list.Count; i++)
                e.Module.Add(new KesselModulZeile(
                    spk.spk_list[i], spk.s_waerme_Gas_Spk[i], spk.s_waerme_Oel_Spk[i],
                    spk.Kessel_Jahresnutzungsgrad_Spk[i]));

            return e;
        }

        /// <summary>
        /// Sichtbarkeitsregel der zehn Brennstoffzeilen der Kesselseite: eine Zeile
        /// erscheint, wenn ihr JAHRESWERT &gt; 0 ist ODER ein Kessel des Projekts diesen
        /// Brennstoff führt (<c>KesselBrennstoffZeilenAnpassen</c> :1132-1193).
        /// </summary>
        /// <param name="jahreswertMwh">Der Jahreswert der Zeile.</param>
        /// <param name="brennstoffId">Die Brennstoffnummer der Zeile.</param>
        /// <param name="artenDesProjekts">
        /// Die Brennstoffarten der Projektkessel — aus
        /// <c>HeizkesselStammCtrl.BrennstoffartenJeProjekt</c>. <c>null</c> heißt
        /// „unbekannt" und lässt die Zeile allein am Jahreswert hängen.
        /// </param>
        public static bool BrennstoffZeileSichtbar(double jahreswertMwh, int brennstoffId,
                                                   ICollection<int> artenDesProjekts)
        {
            if (jahreswertMwh > 0) return true;
            return artenDesProjekts != null && artenDesProjekts.Contains(brennstoffId);
        }

        // =================================================================
        //  Solarthermie
        // =================================================================

        /// <summary>Eine Zeile der Kollektortabelle.</summary>
        public sealed record SolarModulZeile(string Name, double FlaecheM2, long Anzahl,
                                             double WaermeproduktionMwh, double UeberschussMwh);

        public sealed class SolarthermieErgebnis
        {
            /// <summary>false, wenn das Projekt keinen Wärmebedarf führt — dann blieb das
            /// Feld im Vorläufer LEER, nicht „0".</summary>
            public bool DeckungBekannt;
            public double DeckungProzent;
            public double StufeneingangMwh;
            public double RestwaermeMwh;
            public double WaermeproduktionMwh;
            public double UeberschussMwh;
            public List<SolarModulZeile> Module = new List<SolarModulZeile>();
        }

        /// <summary>
        /// Die Zahlen des Solarthermie-Reiters; <c>null</c> ohne Solarthermie im Lauf.
        ///
        /// <para><b>W11-B20 löst sich von selbst.</b> Die Felder der Maske standen
        /// INNERHALB von <c>if (sim.bSimulationSolarthermie)</c> ohne Gegenstück außerhalb;
        /// ein Folgelauf ohne Solarthermie ließ die Zahlen des Vorlaufs stehen. Ein DTO,
        /// das dann <c>null</c> ist, kann das nicht.</para>
        ///
        /// <para><b>Befund V0-O1 wörtlich übernommen:</b> Der NENNER des Deckungsgrades ist
        /// der PROJEKTbedarf, nicht der Stufeneingang der Solarthermie — genau wie bei
        /// Wärmepumpe, Kessel und BHKW und genau wie im Runner. Der RESTBEDARF bleibt auf
        /// dem Stufeneingang: Er beantwortet „was bleibt nach diesem Erzeuger offen" und
        /// ist damit eine Stufengröße.</para>
        /// </summary>
        public static SolarthermieErgebnis Solarthermie(SimulationControl sim, SimulationWaermebedarf wb)
        {
            if (sim == null || !sim.bSimulationSolarthermie) return null;

            SimulationSolarthermie st = sim.simulation_solarthermie;
            SolarthermieErgebnis e = new SolarthermieErgebnis();

            double eigenKwh = SimulationRunner.EigenanteilSolarKwh(st);
            e.StufeneingangMwh = st.Waermebedarf_gesamt / 1000.0;
            // In kWh klemmen und erst danach umrechnen - wortgleich mit Maske und
            // Runner, die beide (Stufeneingang - Eigenanteil) / 1000 rechnen.
            e.RestwaermeMwh = SimulationRunner.RestNachEigenanteil(
                                  st.Waermebedarf_gesamt, eigenKwh) / 1000.0;
            e.DeckungBekannt = wb != null && wb.Waermebedarf_Gesamt > 0;
            e.DeckungProzent = SimulationRunner.DeckungProzent(
                eigenKwh / 1000.0, wb != null ? wb.Waermebedarf_Gesamt : 0.0);

            e.WaermeproduktionMwh = st.Waermeproduktion_gesamt / 1000.0;
            e.UeberschussMwh = st.Ueberschuss_summe / 1000.0;

            if (st.Kollektor_Ergebnisse != null)
                foreach (var k in st.Kollektor_Ergebnisse)
                    e.Module.Add(new SolarModulZeile(k.Name, k.Flaeche, k.Anzahl,
                                                     k.Waermeproduktion / 1000.0,
                                                     k.Ueberschuss / 1000.0));

            return e;
        }

        // =================================================================
        //  BHKW
        // =================================================================

        /// <summary>Eine Zeile der BHKW-Modultabelle.</summary>
        public sealed record BhkwModulZeile(string Name, double WaermeMwh, double StromMwh);

        public sealed class BhkwErgebnis
        {
            public double BetriebsstundenThermisch;
            public double BetriebsstundenDurchschnitt;
            /// <summary>false ohne gepflegte elektrische Nennleistung — die Maske zeigt
            /// dann „—" statt einer erfundenen Zahl.</summary>
            public bool VbhElektrischBekannt;
            public double VbhElektrisch;
            public double StufeneingangMwh;
            public double StrombedarfMwh;
            public double WaermeproduktionMwh;
            public double StromproduktionMwh;
            public double RestwaermeMwh;
            public double ReststrombedarfMwh;
            public double WaermeueberschussMwh;
            public double SpeicherladungMwh;
            public double SpeicherdeckungMwh;
            public double WaermedeckungProzent;
            public double StromdeckungProzent;
            public List<BhkwModulZeile> Module = new List<BhkwModulZeile>();
        }

        /// <summary>
        /// Die Zahlen des BHKW-Reiters. Anders als bei den drei anderen Erzeugern gibt es
        /// hier KEIN <c>null</c>: Der Vorläufer füllte die Felder außerhalb jeder
        /// <c>if</c>-Bedingung (:4613-4714), und ein Lauf ohne BHKW zeigt dort Nullen.
        /// </summary>
        public static BhkwErgebnis Bhkw(SimulationControl sim, SimulationWaermebedarf wb,
                                        SimulationStrombedarf sb)
        {
            if (sim == null) return null;

            SimulationBHKW bh = sim.simulation_bhkw;
            BhkwErgebnis e = new BhkwErgebnis();

            e.BetriebsstundenThermisch = bh.Betriebsstunden;
            e.BetriebsstundenDurchschnitt = bh.dLaufzeiten;
            e.VbhElektrischBekannt = bh.VbhElektrischGesamt > 0;
            e.VbhElektrisch = bh.VbhElektrischGesamt;

            e.StufeneingangMwh = bh.Waermebedarf_gesamt / 1000.0;
            e.StrombedarfMwh = bh.strombedarf.Sum() / 1000.0;
            e.WaermeproduktionMwh = bh.Waermeproduktion_BHKW_MWh;
            e.StromproduktionMwh = bh.Stromproduktion_BHKW_MWh;

            double eigen = SimulationRunner.EigenanteilBhkwMwh(bh);
            e.RestwaermeMwh = SimulationRunner.RestNachEigenanteil(e.StufeneingangMwh, eigen);
            e.WaermedeckungProzent = SimulationRunner.DeckungProzent(
                eigen, wb != null ? wb.Waermebedarf_Gesamt : 0.0);

            e.ReststrombedarfMwh = e.StrombedarfMwh - bh.Stromproduktion_BHKW_MWh;
            e.WaermeueberschussMwh = bh.Waermeueberschuss / 1000.0;
            e.SpeicherladungMwh = bh.Speicherladung_gesamt / 1000.0;
            e.SpeicherdeckungMwh = bh.Speicherentladung_Anteil / 1000.0;

            // Die Stromdeckung ist die PRODUKTION, nicht der Eigenanteil - Strom kennt
            // keine Speicherzurechnung dieser Art (der Stromspeicher rechnet eigens).
            // Wortgleich mit SimulationRunner: b.Strombedarfsdeckung.
            e.StromdeckungProzent = (sb != null && sb.Strombedarf_gesamt > 0)
                ? bh.Stromproduktion_BHKW_MWh * 100.0 / sb.Strombedarf_gesamt
                : 0.0;

            for (int i = 0; i < bh.bhkw_list.Count; i++)
                e.Module.Add(new BhkwModulZeile(
                    bh.bhkw_list_Namen[i], bh.s_waerme_MWh[i], bh.s_strom_MWh[i]));

            return e;
        }

        // =================================================================
        //  Photovoltaik
        // =================================================================

        /// <summary>Eine Zeile der PV-Modultabelle.</summary>
        public sealed record PvModulZeile(string Name, double FlaecheM2, long Anzahl,
                                          double StromproduktionMwh);

        public sealed class PhotovoltaikErgebnis
        {
            public double StromproduktionMwh;
            public double UeberschussMwh;
            public double DeckungProzent;
            public double StrombedarfMwh;
            public double ReststrombedarfMwh;
            public double MaxLeistungKw;
            public List<PvModulZeile> Module = new List<PvModulZeile>();
        }

        /// <summary>
        /// Die Zahlen des Photovoltaik-Reiters. Wie beim BHKW ohne <c>null</c>-Fall — der
        /// Vorläufer füllte auch diese Felder ohne <c>if</c>-Bedingung.
        ///
        /// <para><b>W11-B22 behoben:</b> Der Deckungsgrad teilte ohne Nullprüfung durch
        /// <c>Strombedarf_stuendlich.Sum()</c>. In einem Projekt ohne Strombedarf stand
        /// dort „NaN"; gemessen an Projekt 1030 ist das kein Randfall, sondern der Fall.
        /// Die Nachbarzeilen der Maske prüfen alle (:4678, :4688, :4401, :4493).</para>
        /// </summary>
        public static PhotovoltaikErgebnis Photovoltaik(SimulationControl sim)
        {
            if (sim == null) return null;

            SimulationPV pv = sim.simulation_pv;
            PhotovoltaikErgebnis e = new PhotovoltaikErgebnis();

            double produktionKwh = pv.Stromproduktion.Sum();
            double bedarfKwh = pv.Strombedarf_stuendlich.Sum();

            e.StromproduktionMwh = produktionKwh / 1000.0;
            e.UeberschussMwh = pv.Ueberschuss.Sum() / 1000.0;
            e.DeckungProzent = bedarfKwh > 0 ? produktionKwh * 100.0 / bedarfKwh : 0.0;
            e.StrombedarfMwh = pv.Strombedarf.Sum() / 4000.0;
            e.ReststrombedarfMwh = sim.Rest_Strombedarf_viertelstuendlich.Sum() / 4000.0;
            e.MaxLeistungKw = pv.MaxPSolar;

            if (pv.Modul_Ergebnisse != null)
                foreach (var m in pv.Modul_Ergebnisse)
                    e.Module.Add(new PvModulZeile(m.Name, m.Flaeche, m.Anzahl,
                                                  m.Stromproduktion / 1000.0));

            return e;
        }

        // =================================================================
        //  Bedarf
        // =================================================================

        public sealed class BedarfErgebnis
        {
            public double WaermelastMaxKw;
            public double WaermebedarfGesamtMwh;
            public double StrombedarfMaxKw;
            public double StrombedarfGesamtMwh;
            /// <summary>Heizung, Brauchwasser, Prozesswärme [MWh] — Reihenfolge nach
            /// <c>Kanal</c>.</summary>
            public IReadOnlyList<double> KanalMwh = new double[0];
        }

        /// <summary>Die Zahlen des Bedarfs-Reiters.</summary>
        public static BedarfErgebnis Bedarf(SimulationWaermebedarf wb, SimulationStrombedarf sb)
        {
            BedarfErgebnis e = new BedarfErgebnis();
            if (wb != null)
            {
                e.WaermelastMaxKw = wb.Waermebedarf_Max;
                e.WaermebedarfGesamtMwh = wb.Waermebedarf_Gesamt;
                e.KanalMwh = SimulationRunner.BedarfJeKanal(wb);
            }
            if (sb != null)
            {
                e.StrombedarfMaxKw = sb.Strombedarf_Max;
                e.StrombedarfGesamtMwh = sb.Strombedarf_gesamt;
            }
            return e;
        }

        /// <summary>
        /// Der Warmwasser-(Brauchwasser-)Anteil des Wärmebedarfs als Stundenganglinie,
        /// passend zur übergebenen Bedarfsganglinie (wörtlich aus
        /// <c>Form_Simulation_Detail.WarmwasserAnteil</c> :4136-4151).
        ///
        /// <para>Die Wärmepumpe sieht ggf. nur einen Teil des Gesamtbedarfs (Kaskade,
        /// vorgeschaltete Erzeuger). Der Warmwasseranteil wird deshalb je Stunde auf den
        /// tatsächlich anliegenden Bedarf begrenzt.</para>
        /// </summary>
        public static float[] WarmwasserAnteil(SimulationWaermebedarf wb, float[] bedarf)
        {
            float[] ww = new float[Kanalsatz.STUNDEN_JAHR];
            if (wb == null || wb.brauchwasserwerte == null) return ww;

            float[] quelle = wb.brauchwasserwerte;
            for (int i = 0; i < Kanalsatz.STUNDEN_JAHR && i < quelle.Length; i++)
            {
                float wert = quelle[i];
                if (bedarf != null && i < bedarf.Length && wert > bedarf[i]) wert = bedarf[i];
                if (wert < 0) wert = 0;
                ww[i] = wert;
            }
            return ww;
        }
    }
}

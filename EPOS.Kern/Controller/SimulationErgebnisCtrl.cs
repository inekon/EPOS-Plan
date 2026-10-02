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
            /// <summary>Der Restwärmebedarf des LAUFS (<c>SimulationControl.RestwaermeMwh</c>) —
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
            /// <summary>E30/3 (#548, N10): Eigenverbrauch des BHKW-Stroms [MWh/a] —
            /// Erzeugung minus KWK-Einspeisung; Ring, Stromtabelle und Stromdeckung.</summary>
            public double BhkwStromEigenverbrauchMwh;
            public double SolarWaermeproduktionMwh;
            public double PvStromproduktionMwh;

            // --- W8-O-5c / S1.2: die vier Größen, für die die Windows-Hülle bis
            //     hierher am DTO VORBEI gerechnet hat (Befund U6 des Einheitenkonzepts).
            //     Sie stehen jetzt hier, in MWh und mit der Einheit am Namen; die Hülle
            //     liest sie und teilt nicht mehr selbst durch 1000.

            /// <summary>
            /// Die WÄRME des Heizstabs [MWh/a] — zahlengleich mit
            /// <see cref="HeizstabStromverbrauchMwh"/>.
            ///
            /// <para>Der Heizstab setzt Strom mit dem Wirkungsgrad 1 in Wärme um; der
            /// Lauf führt für beides dieselbe Reihe
            /// (<c>SimulationWaermepumpe.HeizstabGesamtKwh</c>). Das Feld steht
            /// trotzdem eigens da, weil die Wärmebedarfstorte eine WÄRMEmenge zeigt:
            /// Eine Torte, die ihren Heizstabanteil aus einem Feld namens
            /// „Stromverbrauch" holt, liest sich falsch, und genau solche stillen
            /// Gleichsetzungen sind der Grund für W8‑O‑5c.</para>
            /// </summary>
            public double HeizstabWaermeproduktionMwh;

            /// <summary>Die Entladung des Stromspeichers [MWh/a]; 0 ohne Speicher.</summary>
            public double StromspeicherEntladungMwh;

            /// <summary>
            /// Der NENNER des Strom-Rings [MWh/a]: der Projektstrombedarf PLUS die
            /// Eigenverbräuche der Wärmeerzeuger (Wärmepumpe, Heizstab, Kessel) —
            /// wörtlich <c>NavigatorUebersicht</c> :355-359; seit E29 (#536, N6) dazu der
            /// Kältestrom der Stufenrechnung (<see cref="KaeltestromStufeMwh"/>).
            /// </summary>
            public double StrombedarfMitEigenverbrauchMwh;

            /// <summary>
            /// Der Kältestrom der Stufenrechnung [MWh/a] (E29 #536, Befund N6) — der vierte
            /// Eigenverbrauch im Nenner des Strom-Rings; 0 ohne Kälte. Ohne den Kältestrom
            /// der Anlagen mit eigenem Zähler (E34).
            /// </summary>
            public double KaeltestromStufeMwh;

            /// <summary>
            /// Die GEDECKTE Strommenge [MWh/a]: Photovoltaik, BHKW und die
            /// Speicherentladung — der Zähler des Strom-Rings.
            /// </summary>
            public double StromGesamtMwh;

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
            /// (<c>SimulationControl.RestwaermeMwh</c>, gespeichert als
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
        ///   Navigator nahm <c>SWaermeSpkMwh</c>. Das ist KEINE Abweichung: <c>SWaermeSpkMwh</c>
        ///   entsteht in <c>SimulationSPK.Bilanz_und_Nutzungsgrad</c> aus genau dieser Summe
        ///   über genau diese Liste. Zwei Wege, ein Wert — hier steht der kürzere.</item>
        ///   <item><b>Das BHKW.</b> Der Navigator zählt <c>waerme_bhkw</c> in die Summe, die
        ///   Detailansicht nicht. DAS ist die echte Abweichung. Genommen wird die
        ///   NAVIGATOR-Fassung: Das BHKW ist eine Kaskadenstufe wie die anderen, und
        ///   <c>SimulationControl.RestwaermeMwh</c> — die Wahrheit des Referenzlaufs — zieht
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

            u.StrombedarfGesamtMwh = sb != null ? sb.StrombedarfGesamtMwh : 0.0;
            u.WaermebedarfGesamtMwh = wb != null ? wb.Waermebedarf_Gesamt : 0.0;
            u.RestwaermeMwh = sim.RestwaermeMwh;
            u.ReststromMwh = sim.ReststromMwh;
            u.WpWaermeproduktionMwh = sim.simulation_wp.WpWaermeproduktionGesamtKwh / 1000.0;
            u.WpStromverbrauchMwh = sim.simulation_wp.WpStrombedarfGesamtKwh / 1000.0;
            u.KesselWaermeproduktionMwh = sim.simulation_spk.SWaermeSpkMwh;
            u.HeizstabStromverbrauchMwh = sim.simulation_wp.HeizstabGesamtKwh / 1000.0;
            u.KesselStromverbrauchMwh = sim.simulation_spk.StromverbrauchSpkMwh;
            u.BhkwWaermeproduktionMwh = sim.simulation_bhkw.Waermeproduktion_BHKW_MWh;
            u.BhkwStromproduktionMwh = sim.simulation_bhkw.Stromproduktion_BHKW_MWh;
            // E30/3 (#548, N10): Ring und Stromtabelle zeigen den Eigenverbrauch des BHKW —
            // die Einspeisung deckt keinen Bedarf.
            u.BhkwStromEigenverbrauchMwh = BhkwEigenverbrauchMwh(sim);
            u.SolarWaermeproduktionMwh = sim.simulation_solarthermie.WaermeproduktionGesamtKwh / 1000.0;
            u.PvStromproduktionMwh = sim.simulation_pv.StromproduktionGesamtKwh / 1000.0;

            // W8-O-5c / S1.2 (Befund U6): Die vier Groessen standen bis hierher in der
            // Windows-Huelle - StrombedarfGesamt() und StromgedecktMwh() der
            // Ergebnisseite rechneten dort mit eigenen Teilern am DTO vorbei. Die
            // Summanden und ihre Reihenfolge sind woertlich uebernommen, damit die
            // angezeigte Zahl dieselbe bleibt.
            u.HeizstabWaermeproduktionMwh = u.HeizstabStromverbrauchMwh;
            u.StromspeicherEntladungMwh = sim.Speicherergebnis != null
                ? sim.Speicherergebnis.EntladeenergieKwh / 1000.0 : 0.0;
            // E29 (#536, Befund N6, Entscheid E29‑Q9 a): der Kältestrom der Stufenrechnung
            // gehört in den Nenner - er steht im Netzbezug (ReststromMwh) und in
            // STROMBEDARF_GESAMT. Der Kältestrom mit eigenem Zähler (E34) läuft neben der
            // Stufenrechnung und bleibt draußen. Ohne Kälte + 0,0: bitgleich.
            u.KaeltestromStufeMwh = KaeltestromStufeMwh(sim);
            u.StrombedarfMitEigenverbrauchMwh = u.StrombedarfGesamtMwh
                                              + u.WpStromverbrauchMwh
                                              + u.HeizstabStromverbrauchMwh
                                              + u.KesselStromverbrauchMwh
                                              + u.KaeltestromStufeMwh;
            u.StromGesamtMwh = u.PvStromproduktionMwh
                             + u.BhkwStromEigenverbrauchMwh
                             + u.StromspeicherEntladungMwh;

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
        /// Der Kältestrom der Stufenrechnung [MWh/a] (E29 #536, N6) —
        /// <c>SimulationControl.Kaeltestrom_Stufenrechnung_stuendlich</c>, 0 ohne Kälte.
        /// </summary>
        private static double KaeltestromStufeMwh(SimulationControl sim)
        {
            double summe = 0.0;
            double[] reihe = sim.Kaeltestrom_Stufenrechnung_stuendlich;
            if (reihe == null) return summe;
            foreach (double k in reihe) summe += k;
            return summe / 1000.0;
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

        /// <summary>
        /// Eine Kanalzeile [kWh] als Kanalzeile [MWh] — je Kanal geteilt, nicht
        /// summiert (W8‑O‑5c / S1.2).
        ///
        /// <para><b>Warum das hier steht und nicht in der Hülle.</b> Das
        /// Eigenanteilsraster der Ergebnisseite zeigt drei Kanalspalten in MWh; die
        /// Engine-Buchführung (<c>Direktdeckung_Kanal</c>,
        /// <c>Speicherentladung_Kanal</c>, <c>Heizstab_Kanal</c>) führt kWh. Die
        /// Umrechnung war bis W8‑O‑5c die letzte Energie-Division in
        /// <c>SimulationErgebnisHuelle</c> (Befund U6). Sie gehört an die
        /// Kerngrenze, damit die Regel „außerhalb der zwei Nähte steht in der Hülle
        /// kein Faktor 1000 auf einer Energiemenge" ohne Ausnahme gilt.</para>
        ///
        /// <para><c>null</c> ergibt <c>null</c>; ein leeres Feld bleibt leer.</para>
        /// </summary>
        public static double[] KanalMwh(double[] kanalKwh)
        {
            if (kanalKwh == null) return null;

            double[] mwh = new double[kanalKwh.Length];
            for (int k = 0; k < kanalKwh.Length; k++) mwh[k] = kanalKwh[k] / 1000.0;
            return mwh;
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

            e.StufeneingangMwh = wp.WaermebedarfGesamtKwh / 1000.0;
            double eigen = SimulationRunner.EigenanteilWpMwh(wp);
            e.RestwaermeMwh = SimulationRunner.RestNachEigenanteil(e.StufeneingangMwh, eigen);
            e.DeckungProzent = SimulationRunner.DeckungProzent(
                eigen, wb != null ? wb.Waermebedarf_Gesamt : 0.0);

            e.BivalenzpunktVorhanden = wp.Bivalenzpunkt != -100;
            e.Bivalenzpunkt = wp.Bivalenzpunkt;

            e.StromverbrauchMwh = wp.WpStrombedarfGesamtKwh / 1000.0;
            e.HeizstabStromverbrauchMwh = wp.HeizstabGesamtKwh / 1000.0;
            e.WaermeproduktionMwh = wp.WpWaermeproduktionGesamtKwh / 1000.0;

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

        /// <summary>
        /// Eine Zeile der Kesseltabelle. Die Kennliniengrößen (Konzept Kesselkennlinie 5, Etappe E2)
        /// tragen Vorgaben, mit denen eine Zeile ohne Kennlinie — der Elektrokessel — sie nicht zeigt:
        /// <paramref name="MitKennlinie"/> false.
        /// </summary>
        /// <param name="TeillastwirkungsgradProzent">das wirksame η₃₀ [%], gepflegt oder Normvorgabe</param>
        /// <param name="TeillastVorgabe">η₃₀ ist die Normvorgabe nach Bauart (Feld leer)</param>
        /// <param name="WirkungsgradBetriebProzent">mittlerer Wirkungsgrad der Laufstunden [%], wärmegewichtet</param>
        /// <param name="LaststufeProzent">mittlere Laststufe der Laufstunden [%]</param>
        /// <param name="MitBrennwertkennlinie">der Kessel rechnet mit der Brennwertkennlinie (Etappe E3)</param>
        /// <param name="RuecklaufMittelC">mittlerer Rücklauf der Laufstunden [°C], wärmegewichtet</param>
        /// <param name="BrennwertStundenProzent">Anteil der Laufstunden im Brennwertbetrieb [%]</param>
        /// <param name="Starts">Starts des Kessels [1/a] nach Konzept 4.2 (Etappe E4); beim Elektrokessel seine Laufphasen</param>
        /// <param name="BrennwertWaermeProzent">Anteil der Wärme im Brennwertbetrieb an der Wärme der Laufstunden [%] — dieselbe
        /// Teilung wie der Anteil über alle Kessel (<see cref="HeizkesselErgebnis.BrennwertWaermeProzent"/>); die Zeile des
        /// Berichts (Konzept Kesselkennlinie 5)</param>
        public sealed record KesselModulZeile(string Name, double GasMwh, double OelMwh,
                                              double JahresnutzungsgradProzent,
                                              bool MitKennlinie = false,
                                              double TeillastwirkungsgradProzent = 0,
                                              bool TeillastVorgabe = false,
                                              double WirkungsgradBetriebProzent = 0,
                                              double LaststufeProzent = 0,
                                              bool MitBrennwertkennlinie = false,
                                              double RuecklaufMittelC = 0,
                                              double BrennwertStundenProzent = 0,
                                              int Starts = 0,
                                              double BrennwertWaermeProzent = 0);

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

            /// <summary>
            /// Der Teil des Restwärmebedarfs nach dem Kessel, den der Puffer aus der Ladung
            /// der ANDEREN Erzeuger gedeckt hat [MWh/a] (#568) — ein „davon" von
            /// <see cref="RestwaermeMwh"/>.
            /// </summary>
            public double AusPufferAndereMwh;

            /// <summary>Laufstunden aller Kessel [h/a] (Summe über die Kessel).</summary>
            public int Laufstunden;

            /// <summary>
            /// Starts aller Kessel [1/a] nach Konzept Kesselkennlinie 4.2 (Etappe E4): je Laufphase einer, in
            /// einer Taktstunde so viele, wie Mindestläufe die Wärme braucht.
            /// </summary>
            public int Starts;

            /// <summary>Taktstunden der Brennstoffkessel [h/a]: Laufstunden mit einer Wärme unter der Mindestleistung.</summary>
            public int Taktstunden;

            /// <summary>Anfahrverlust der Brennstoffkessel [kWh/a] — Starts mal Anfahrverlust je Start; Teil des Brennstoffeinsatzes.</summary>
            public double AnfahrverlustKwh;

            /// <summary>Betriebsbereite Stillstandsstunden aller Kessel [h/a].</summary>
            public int Bereitschaftsstunden;

            /// <summary>Bereitschaftsverlust aller Kessel [kWh/a] — Teil des Brennstoffeinsatzes.</summary>
            public double BereitschaftsverlustKwh;

            /// <summary>
            /// Führt der Lauf mindestens einen Brennstoffkessel, also einen Kessel mit
            /// Teillastkennlinie (Konzept Kesselkennlinie 4.1)? Der Elektrokessel hat keine.
            /// </summary>
            public bool MitKennlinie;

            /// <summary>
            /// Mittlerer Wirkungsgrad der Brennstoffkessel im Betrieb [%]: Wärme der Laufstunden
            /// durch ihren Brennstoff, über alle Brennstoffkessel — ohne Bereitschaftsverlust.
            /// </summary>
            public double WirkungsgradBetriebProzent;

            /// <summary>
            /// Mehrbrennstoff aus Teillast gegenüber dem Betrieb mit η₁₀₀ [kWh/a], Summe über die
            /// Brennstoffkessel; negativ, wo die Teillast Brennstoff spart (Brennwertkessel).
            /// </summary>
            public double TeillastMehrbrennstoffKwh;

            /// <summary>
            /// Rechnet mindestens ein Kessel des Laufs mit der Brennwertkennlinie (Konzept
            /// Kesselkennlinie 4.1, Etappe E3)? Nur dann zeigt der Reiter die Brennwertgrößen.
            /// </summary>
            public bool MitBrennwertkennlinie;

            /// <summary>Laufstunden der Kessel mit Brennwertkennlinie [h/a].</summary>
            public int BrennwertLaufstunden;

            /// <summary>Davon Stunden im Brennwertbetrieb (Rücklauf unter dem Taupunkt) [h/a].</summary>
            public int Brennwertstunden;

            /// <summary>Anteil der <see cref="Brennwertstunden"/> an den <see cref="BrennwertLaufstunden"/> [%].</summary>
            public double BrennwertStundenProzent;

            /// <summary>Anteil der Wärme im Brennwertbetrieb an der Wärme der Laufstunden dieser Kessel [%].</summary>
            public double BrennwertWaermeProzent;

            /// <summary>
            /// Mehrbrennstoff aus Brennwertnutzung gegenüber der trockenen Teillastkurve [kWh/a], Summe
            /// über die Kessel mit Brennwertkennlinie — negativ, wo die Kondensation Brennstoff spart.
            /// </summary>
            public double BrennwertMehrbrennstoffKwh;

            /// <summary>Mittlerer Rücklauf der Laufstunden dieser Kessel [°C], wärmegewichtet; 0 ohne Laufstunde.</summary>
            public double RuecklaufMittelC;

            /// <summary>
            /// Die WIRKSAME Heizgrenze des Laufs [°C] — ein Tag mit einem Tagesmittel der
            /// Außentemperatur darunter ist Heiztag (<see cref="SimulationSPK.Heizgrenze_C"/>).
            /// </summary>
            public double HeizgrenzeC = SimulationSPK.HEIZGRENZE_VORGABE_C;

            /// <summary>Die Heiztage des Laufs (0 … 365, <see cref="SimulationSPK.Heiztage_Anzahl"/>).</summary>
            public int Heiztage = 365;

            /// <summary>
            /// Die Brennstoffkessel, deren Wirkungsgrad genau 1,0 ist — ein Platzhalter
            /// statt eines gepflegten Katalogwerts: Ihr Brennstoffeinsatz ist dann ihre
            /// Nutzwärme. Der Elektrokessel zählt nicht dazu.
            /// </summary>
            public List<string> NutzungsgradPlatzhalter = new List<string>();

            public List<KesselModulZeile> Module = new List<KesselModulZeile>();
        }

        /// <summary>Die drei Stapelreihen des Kesselbildes [kWh je Stunde] (#568).</summary>
        public sealed record Kesselbildreihen(double[] Kesselwaerme, double[] AusPufferAndere,
                                              double[] RestNachKessel);

        /// <summary>
        /// DIE STUNDENREIHEN DES KESSELBILDES (#568) — dieselbe Aufteilung wie die Tafel,
        /// Stunde für Stunde; ihr Stapel ist der Stufeneingang (<c>spk.Waermebedarf</c>):
        /// <list type="number">
        /// <item><b>Kesselwärme</b>: der Eigenanteil des Kessels an der Deckung — Abgabe
        /// minus Speicherladung (die Direktdeckung) plus die ihm zugerechnete
        /// Speicherentladung. Jahressumme = <see cref="SimulationRunner.EigenanteilKesselMwh"/>.</item>
        /// <item><b>aus Puffer (andere Erzeuger)</b>: die bedarfsdeckende Speicherentladung
        /// aus der Ladung von Wärmepumpe, Solarthermie und BHKW.</item>
        /// <item><b>übrige Erzeuger / ungedeckt</b>: der Rest des Stufeneingangs.</item>
        /// </list>
        /// Die Reihe <c>spk.Restwaerme</c> taugt dafür nicht: Sie steht NACH der
        /// Direktdeckung, aber VOR Ladephase und Nachentladung — was der Kessel über den
        /// Puffer liefert, stünde dort als Restwärme.
        /// </summary>
        public static Kesselbildreihen KesselbildReihen(SimulationSPK spk)
        {
            const int N = 8760;
            var kessel = new double[N];
            var andere = new double[N];
            var rest = new double[N];
            if (spk == null) return new Kesselbildreihen(kessel, andere, rest);

            double[] entladungEigen = new double[N];
            double[] entladungAndere = new double[N];
            foreach (int k in Kanal.KANAELE_WAERME)
            {
                double[] e = spk.Speicherentladung_KanalStuendlich.Zeile(k);
                double[] a = spk.SpeicherentladungAndere_KanalStuendlich.Zeile(k);
                for (int h = 0; h < N; h++) { entladungEigen[h] += e[h]; entladungAndere[h] += a[h]; }
            }

            for (int h = 0; h < N; h++)
            {
                double direkt = spk.Kesselleistung_stuendlich[h] - spk.Speicherladung_stuendlich[h];
                if (direkt < 0) direkt = 0;
                kessel[h] = direkt + entladungEigen[h];
                andere[h] = entladungAndere[h];
                double r = spk.Waermebedarf[h] - kessel[h] - andere[h];
                rest[h] = r > 0 ? r : 0;
            }
            return new Kesselbildreihen(kessel, andere, rest);
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
            e.StufeneingangMwh = spk.WaermebedarfGesamtMwh;
            e.RestwaermeMwh = SimulationRunner.RestNachEigenanteil(e.StufeneingangMwh, eigen);
            e.DeckungProzent = SimulationRunner.DeckungProzent(
                eigen, wb != null ? wb.Waermebedarf_Gesamt : 0.0);

            e.WaermeproduktionMwh = spk.SWaermeSpkMwh;
            e.StrombedarfMwh = spk.StrombedarfGesamtKwh / 1000.0;
            e.ReststrombedarfMwh = spk.StrombedarfGesamtKwh / 1000.0 + spk.StromverbrauchSpkMwh;

            e.GasMwh = spk.GasverbrauchSpkMwh;
            e.OelMwh = spk.OelverbrauchSpkMwh;
            e.KoksMwh = spk.KoksSpkMwh;
            e.RapsoelMwh = spk.RapsoelverbrauchSpkMwh;
            e.HolzMwh = spk.HolzverbrauchSpkMwh;
            e.KohleMwh = spk.KohleSpkMwh;
            e.StromMwh = spk.StromverbrauchSpkMwh;
            e.SonstigeMwh = spk.SonstigverbrauchSpkMwh;
            e.PelletsMwh = spk.PelletsSpkMwh;
            e.TierischeFetteMwh = spk.TierischeFetteSpkMwh;

            e.MaxKesselleistungKw = spk.Maximale_Kesselleistung_Spk;
            e.GasspitzeKw = spk.Gasspitze_Spk;
            e.QuellwaermeMwh = spk.QuellwaermeGesamtKwh / 1000.0;
            e.AusPufferAndereMwh = spk.SpeicherentladungAndere_Kwh / 1000.0;
            e.HeizgrenzeC = spk.Heizgrenze_C;
            e.Heiztage = spk.Heiztage_Anzahl;

            int kessel = Math.Min(spk.spk_list.Count, SimulationSPK.MAX_SPK);
            double waermeBetrieb = 0, brennstoffBetrieb = 0;
            for (int i = 0; i < kessel; i++)
            {
                e.Laufstunden += spk.Laufstunden_Spk[i];
                e.Starts += spk.Starts_Spk[i];
                e.Bereitschaftsstunden += spk.Bereitschaftsstunden_Spk[i];
                e.BereitschaftsverlustKwh += spk.Bereitschaftsverlust_KWh_Spk[i];
                if (spk.WirkungsgradIstPlatzhalter(i)) e.NutzungsgradPlatzhalter.Add(spk.spk_list[i]);

                // Konzept Kesselkennlinie 5 (Etappe E2): die Teillastgrößen der Brennstoffkessel.
                if (spk.IstStromkessel(i)) continue;
                e.MitKennlinie = true;
                waermeBetrieb += spk.WaermeBetriebKwh(i);
                brennstoffBetrieb += spk.BrennstoffBetrieb_KWh_Spk[i];
                e.TeillastMehrbrennstoffKwh += spk.TeillastMehrbrennstoff_KWh_Spk[i];

                // Etappe E4: das Takten der Brennstoffkessel.
                e.Taktstunden += spk.Taktstunden_Spk[i];
                e.AnfahrverlustKwh += spk.Anfahrverlust_KWh_Spk[i];
            }
            e.WirkungsgradBetriebProzent = brennstoffBetrieb > 0 ? waermeBetrieb / brennstoffBetrieb * 100.0 : 0;

            // Konzept Kesselkennlinie 5 (Etappe E3): die Brennwertgrößen der Kessel mit Brennwertkennlinie.
            double waermeBrennwertKessel = 0, waermeBrennwertBetrieb = 0, ruecklaufGewichtet = 0;
            for (int i = 0; i < kessel; i++)
            {
                if (!spk.RechnetMitBrennwertkennlinie(i)) continue;
                e.MitBrennwertkennlinie = true;
                e.BrennwertLaufstunden += spk.Laufstunden_Spk[i];
                e.Brennwertstunden += spk.Brennwertstunden_Spk[i];
                e.BrennwertMehrbrennstoffKwh += spk.BrennwertMehrbrennstoff_KWh_Spk[i];
                double w = spk.WaermeBetriebKwh(i);
                waermeBrennwertKessel += w;
                waermeBrennwertBetrieb += spk.BrennwertWaerme_KWh_Spk[i];
                double rl = spk.RuecklaufMittel(i);
                if (w > 0 && !double.IsNaN(rl)) ruecklaufGewichtet += w * rl;
            }
            e.BrennwertStundenProzent = e.BrennwertLaufstunden > 0
                ? e.Brennwertstunden * 100.0 / e.BrennwertLaufstunden : 0;
            e.BrennwertWaermeProzent = waermeBrennwertKessel > 0 ? waermeBrennwertBetrieb / waermeBrennwertKessel * 100.0 : 0;
            e.RuecklaufMittelC = waermeBrennwertKessel > 0 ? ruecklaufGewichtet / waermeBrennwertKessel : 0;

            for (int i = 0; i < spk.spk_list.Count; i++)
            {
                bool kennlinie = i < SimulationSPK.MAX_SPK && !spk.IstStromkessel(i);
                bool brennwert = kennlinie && spk.RechnetMitBrennwertkennlinie(i);
                double ruecklauf = brennwert ? spk.RuecklaufMittel(i) : 0;
                double waermeLauf = kennlinie ? spk.WaermeBetriebKwh(i) : 0;
                e.Module.Add(new KesselModulZeile(
                    spk.spk_list[i], spk.s_waerme_Gas_Spk[i], spk.s_waerme_Oel_Spk[i],
                    spk.Kessel_Jahresnutzungsgrad_Spk[i],
                    kennlinie,
                    kennlinie ? spk.Teillastwirkungsgrad(i) * 100.0 : 0,
                    kennlinie && spk.TeillastwirkungsgradIstVorgabe(i),
                    kennlinie ? spk.WirkungsgradBetrieb(i) * 100.0 : 0,
                    kennlinie ? spk.LaststufeMittel(i) * 100.0 : 0,
                    brennwert,
                    double.IsNaN(ruecklauf) ? 0 : ruecklauf,
                    brennwert && spk.Laufstunden_Spk[i] > 0
                        ? spk.Brennwertstunden_Spk[i] * 100.0 / spk.Laufstunden_Spk[i] : 0,
                    i < SimulationSPK.MAX_SPK ? spk.Starts_Spk[i] : 0,
                    brennwert && waermeLauf > 0 ? spk.BrennwertWaerme_KWh_Spk[i] / waermeLauf * 100.0 : 0));
            }

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

        /// <summary>
        /// Eine Zeile der Kollektortabelle. <paramref name="Ganglinie"/>: die Zeile der
        /// Solarthermieganglinie (Folgeauftrag 4) — ohne Fläche und Anzahl.
        /// </summary>
        public sealed record SolarModulZeile(string Name, double FlaecheM2, long Anzahl,
                                             double WaermeproduktionMwh, double UeberschussMwh,
                                             bool Ganglinie = false);

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

            /// <summary>
            /// Pumpenstrom der Solarkreise [MWh/a] (ST1) — der Strom, den der Lauf an der Position der
            /// Solarthermie in den Strombedarf bucht; 0 ohne gepflegte Pumpenleistung und ohne
            /// Hilfsenergieanteil.
            /// </summary>
            public double PumpenstromMwh;

            public List<SolarModulZeile> Module = new List<SolarModulZeile>();

            /// <summary>
            /// „Ertrag … ohne Abnehmer" — gesetzt, wenn Überschuss anfällt UND Bedarf in
            /// einem Kanal liegt, den keine Senke der Felder bedient; "" = kein Hinweis.
            /// Siehe <see cref="SolarHinweisOhneAbnehmer"/>.
            /// </summary>
            public string HinweisOhneAbnehmer = "";
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
            e.StufeneingangMwh = st.WaermebedarfGesamtKwh / 1000.0;
            // In kWh klemmen und erst danach umrechnen - wortgleich mit Maske und
            // Runner, die beide (Stufeneingang - Eigenanteil) / 1000 rechnen.
            e.RestwaermeMwh = SimulationRunner.RestNachEigenanteil(
                                  st.WaermebedarfGesamtKwh, eigenKwh) / 1000.0;
            e.DeckungBekannt = wb != null && wb.Waermebedarf_Gesamt > 0;
            e.DeckungProzent = SimulationRunner.DeckungProzent(
                eigenKwh / 1000.0, wb != null ? wb.Waermebedarf_Gesamt : 0.0);

            e.WaermeproduktionMwh = st.WaermeproduktionGesamtKwh / 1000.0;
            e.UeberschussMwh = st.UeberschussSummeKwh / 1000.0;
            e.PumpenstromMwh = st.PumpenstromGesamtKwh / 1000.0;

            if (st.Kollektor_Ergebnisse != null)
                foreach (var k in st.Kollektor_Ergebnisse)
                    e.Module.Add(new SolarModulZeile(k.Name, k.Flaeche, k.Anzahl,
                                                     k.WaermeproduktionKwh / 1000.0,
                                                     k.UeberschussKwh / 1000.0,
                                                     k.IstGanglinie));

            e.HinweisOhneAbnehmer = SolarHinweisOhneAbnehmer(sim, wb, e.UeberschussMwh);

            return e;
        }

        /// <summary>
        /// ERTRAG OHNE ABNEHMER — der Hinweis des Solarthermie-Reiters, wenn der
        /// Überschuss daher rührt, dass die Senken der Felder den Kanal gar nicht
        /// bedienen, in dem der Bedarf liegt (Befund: Kollektorfeld mit Vorbelegung
        /// Heizkreis/Beides in einem reinen Prozesswärmeprojekt — Produktion 0, das ganze
        /// Potenzial als Überschuss). "" = kein Hinweis.
        ///
        /// <para><b>Alles aus dem Lauf.</b> Die Senken sind die Senkenlisten, mit denen
        /// die Felder gerechnet haben (<c>SimulationSolarthermie.FeldSenke</c>, samt
        /// Vorbelegung und Rückfall), der Kanalbedarf der des Laufs
        /// (<see cref="SimulationRunner.BedarfJeKanal"/>), die Entladekanäle einer
        /// Puffersenke das Klassen-Set ihres Speichers im Lauf
        /// (<c>SimulationPufferspeicher.BedientKanal</c>). Gelesen wird nur der
        /// Puffername für den Satz — und nur, wenn der Hinweis wirklich entsteht.</para>
        /// </summary>
        internal static string SolarHinweisOhneAbnehmer(SimulationControl sim,
                                                        SimulationWaermebedarf wb,
                                                        double ueberschussMwh)
        {
            if (sim == null || wb == null || sim.simulation_solarthermie == null) return "";
            if (ueberschussMwh < Warnkriterien.KANAL_BEDARF_SCHWELLE_MWH) return "";

            SimulationSolarthermie st = sim.simulation_solarthermie;
            List<Senkenzeile> zeilen = new List<Senkenzeile>();
            bool[] bedient = new bool[Kanal.ANZAHL];

            for (int f = 0; f < st.FelderAnzahl; f++)
            {
                Senkenliste liste = st.FeldSenke(f) ?? Senkenliste.Vorbelegung(0);
                foreach (Senkenzeile z in liste.Zeilen)
                {
                    if (z == null) continue;
                    zeilen.Add(z);
                    for (int k = 0; k < Kanal.ANZAHL; k++)
                        if (ZeileBedient(sim, z, k)) bedient[k] = true;
                }
            }
            if (zeilen.Count == 0) return "";

            // Nur die Wärmekanäle: Solarwärme ist nie Abnehmer-los, weil die Kälteseite
            // offen bleibt (Kühlkonzept 4.2) - der Kühlkanal ist keine Frage der Senken.
            double[] bedarf = SimulationRunner.BedarfJeKanal(wb);
            List<string> offen = new List<string>();
            foreach (int k in Kanal.KANAELE_WAERME)
                if (bedarf[k] >= Warnkriterien.KANAL_BEDARF_SCHWELLE_MWH && !bedient[k])
                    offen.Add(Warnkriterien.KanalAnzeige(k));
            if (offen.Count == 0) return "";

            List<string> senken = new List<string>();
            foreach (Senkenzeile z in zeilen)
            {
                string anzeige = WaermesenkeClass.SenkeAnzeige(new Z_AnlageSenkeModel
                {
                    Ziel = Senkenzuordnung.ZielAusSenke(z.Ziel),
                    Bedarfsart = z.Bedarfsart,
                    ID_Puffer = z.IDPuffer
                });
                if (!senken.Contains(anzeige)) senken.Add(anzeige);
            }

            return string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                 MyResource.Resource.SIMERG_ST_HINWEIS_OHNE_ABNEHMER,
                                 ueberschussMwh.ToString("N1", System.Globalization.CultureInfo.CurrentCulture),
                                 string.Join(", ", senken),
                                 string.Join(MyResource.Resource.SIMWARN_TRENNER, offen));
        }

        /// <summary>
        /// Bedient eine Senkenzeile des Laufs diesen Kanal? Direktsenke: ihre Kanalmaske;
        /// Puffersenke: das Klassen-Set ihres Speichers in DIESEM Lauf.
        /// </summary>
        private static bool ZeileBedient(SimulationControl sim, Senkenzeile z, int kanal)
        {
            if (z.IstPuffersenke)
            {
                SimulationPufferspeicher sp;
                return sim.speicherRegistry != null &&
                       sim.speicherRegistry.TryGetValue(z.IDPuffer, out sp) && sp != null &&
                       sp.BedientKanal(kanal);
            }

            bool[] maske = Kaskadenschleife.SenkenMaske(z);
            return maske != null && maske[kanal];
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
            /// <summary>
            /// Die BHKW-Einspeisung [MWh/a] (E29 #536, Entscheide E27‑Q3 b, E29‑Q1 a/Q3 a):
            /// ohne Speicherflotte der KWK-Split je Stunde
            /// (<see cref="SimulationControl.BhkwEinspeisungStuendlich"/>, dieselbe Menge wie
            /// <c>StromMatrix.KwkEinspeisungGesamtMWh</c>), mit Flotte die BHKW-Einspeisung
            /// der Flottenbilanz. Anzeige, nicht persistiert.
            /// </summary>
            public double EinspeisungMwh;
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

            e.StufeneingangMwh = bh.WaermebedarfGesamtKwh / 1000.0;
            e.StrombedarfMwh = bh.strombedarf.Sum() / 1000.0;
            e.WaermeproduktionMwh = bh.Waermeproduktion_BHKW_MWh;
            e.StromproduktionMwh = bh.Stromproduktion_BHKW_MWh;

            double eigen = SimulationRunner.EigenanteilBhkwMwh(bh);
            e.RestwaermeMwh = SimulationRunner.RestNachEigenanteil(e.StufeneingangMwh, eigen);
            e.WaermedeckungProzent = SimulationRunner.DeckungProzent(
                eigen, wb != null ? wb.Waermebedarf_Gesamt : 0.0);

            // E27 (E27‑Q4): je Stunde geklemmt - wortgleich mit SimulationRunner.
            e.ReststrombedarfMwh = SimulationControl.BhkwReststrombedarfMwh(bh.strombedarf, bh.stromproduktion,
                                                                         bh.Stromproduktion_BHKW_MWh);
            e.EinspeisungMwh = BhkwEinspeisungMwh(sim);
            e.WaermeueberschussMwh = bh.WaermeueberschussKwh / 1000.0;
            e.SpeicherladungMwh = bh.SpeicherladungGesamtKwh / 1000.0;
            e.SpeicherdeckungMwh = bh.Speicherentladung_Anteil / 1000.0;

            // E30/3 (#548, N10): der Eigenverbrauch am Bedarf aller Verbraucher -
            // dieselbe Formel wie SimulationRunner (b.Strombedarfsdeckung).
            e.StromdeckungProzent = BhkwStromdeckungProzent(sim);

            for (int i = 0; i < bh.bhkw_list.Count; i++)
                e.Module.Add(new BhkwModulZeile(
                    bh.bhkw_list_Namen[i], bh.s_waerme_MWh[i], bh.s_strom_MWh[i]));

            return e;
        }

        /// <summary>
        /// Die BHKW-Einspeisung des Laufs [MWh/a] (E29 #536): mit Speicherflotte deren
        /// BHKW-Einspeisung (Entscheid E29‑Q3 a, dieselbe Quelle wie die Reihe
        /// <c>BHKW_UEBERSCHUSS</c>), sonst die Stundenformel des KWK-Splits. 0 ohne BHKW.
        /// </summary>
        internal static double BhkwEinspeisungMwh(SimulationControl sim)
        {
            if (sim == null || !sim.bSimulationBHKW) return 0.0;
            if (sim.Speicherflottennetzbilanz != null)
                return sim.Speicherflottennetzbilanz.BhkwNetzeinspeisungKwh / 1000.0;
            double[] einspeisung = sim.BhkwEinspeisungDesLaufs();
            return einspeisung != null ? einspeisung.Sum() / 1000.0 : 0.0;
        }

        /// <summary>
        /// E30/3 (#548, Befund N10, Entscheid E30‑Q7 a) — der <b>Eigenverbrauch des
        /// BHKW-Stroms</b> [MWh/a]: Erzeugung minus KWK-Einspeisung
        /// (<see cref="BhkwEinspeisungMwh"/>), nie unter 0. 0 ohne BHKW.
        /// </summary>
        internal static double BhkwEigenverbrauchMwh(SimulationControl sim)
        {
            if (sim == null || !sim.bSimulationBHKW || sim.simulation_bhkw == null) return 0.0;
            return Math.Max(0.0, sim.simulation_bhkw.Stromproduktion_BHKW_MWh - BhkwEinspeisungMwh(sim));
        }

        /// <summary>
        /// E30/3 (#548, N10) — der <b>Strombedarf aller Verbraucher</b> [MWh/a] vor jeder
        /// Eigenerzeugung (<see cref="SimulationControl.Strombedarf_Verbraucher_viertelstuendlich"/>,
        /// dieselbe Reihe wie <c>STROMBEDARF_GESAMT</c> der Strommatrix): Projektlast,
        /// Wärmepumpe, Heizstab, Elektrokessel, Kälte.
        /// </summary>
        internal static double StrombedarfVerbraucherMwh(SimulationControl sim)
        {
            if (sim == null || sim.Strombedarf_Verbraucher_viertelstuendlich == null) return 0.0;
            return sim.Strombedarf_Verbraucher_viertelstuendlich.Sum() / 4000.0;
        }

        /// <summary>
        /// E30/3 (#548, Befund N10, Entscheid E30‑Q7 a) — der <b>Stromdeckungsgrad des BHKW</b>
        /// [%]: Eigenverbrauch des BHKW-Stroms ÷ Strombedarf aller Verbraucher, auf 0 bis 100
        /// geklemmt; 0 ohne Bedarf. Bis hierher zählte die ganze Erzeugung samt Einspeisung am
        /// Projekt-Strombedarf (bei 1018 über 100 %). EINE Formel für Lauf
        /// (<c>Tab_ErgebnisBHKW.Strombedarfsdeckung</c>), BHKW-Reiter und Übersicht.
        /// </summary>
        internal static double BhkwStromdeckungProzent(SimulationControl sim)
        {
            double bedarf = StrombedarfVerbraucherMwh(sim);
            if (!(bedarf > 0)) return 0.0;
            double d = BhkwEigenverbrauchMwh(sim) * 100.0 / bedarf;
            return d > 100.0 ? 100.0 : (d < 0.0 ? 0.0 : d);
        }

        // =================================================================
        //  Photovoltaik
        // =================================================================

        /// <summary>Eine Zeile der PV-Modultabelle.</summary>
        /// <param name="FlaecheGeschaetzt">
        /// W11b‑B‑8: Die Fläche ist aus Nennleistung und Wirkungsgrad geschätzt, weil der
        /// Katalog keine Modulmaße führt (CEC-Import ohne Länge und Breite).
        /// </param>
        public sealed record PvModulZeile(string Name, double FlaecheM2, long Anzahl,
                                          double StromproduktionMwh,
                                          bool FlaecheGeschaetzt = false);

        /// <summary>
        /// Eine Zeile der WECHSELRICHTER-Tabelle des PV-Reiters (Stufe S3 des
        /// <c>Konzept_Wechselrichter_EPOS-Plan.md</c>, Kennzahlen nach 4.4).
        ///
        /// <para><b>Die Liste ist LEER, solange keine Anlage auf der Strangebene
        /// rechnet</b> — dann zeigt der Reiter die Tabelle nicht, und für ein
        /// Bestandsprojekt ändert sich nichts. <c>Tab_ErgebnisPhotovoltaik</c> bleibt
        /// unverändert; diese Zahlen sind Ausweis eines LAUFS, keine Ergebnisebene.</para>
        /// </summary>
        /// <param name="Anlage">Bezeichner der PV-Anlage.</param>
        /// <param name="Geraet">Gerätename und Gerätenummer.</param>
        /// <param name="DcAc">Σ P_STC des Geräts / P_AC_Nenn; 0 = ohne AC-Nennleistung.</param>
        /// <param name="ErtragMwh">Jahresertrag nach Clipping [MWh/a].</param>
        /// <param name="ClippingKwh">Clipping-Verlust [kWh/a].</param>
        /// <param name="ClippingProzent">Clipping als Anteil des ungeklippten AC-Ertrags [%].</param>
        /// <param name="VolllaststundenAc">Ertrag / AC-Nennleistung [h/a].</param>
        /// <param name="Jahresnutzungsgrad">Σ P_AC / Σ P_DC,sys.</param>
        /// <param name="NachtverbrauchKwh">Eigenverbrauch in den Nachtstunden [kWh/a].</param>
        public sealed record PvWechselrichterZeile(string Anlage, string Geraet, double DcAc,
                                                   double ErtragMwh, double ClippingKwh,
                                                   double ClippingProzent, double VolllaststundenAc,
                                                   double Jahresnutzungsgrad, double NachtverbrauchKwh);

        public sealed class PhotovoltaikErgebnis
        {
            /// <summary>
            /// Die ERZEUGUNG der Module nach Wechselrichter [MWh/a] — dieselbe Reihe, die
            /// die Modultabelle summiert (<c>Stromproduktion_Theoretisch</c>; W11b‑B‑6).
            /// </summary>
            public double StromproduktionMwh;

            /// <summary>Davon im Projekt direkt genutzt [MWh/a]: min(Erzeugung, Bedarf) je Stunde.</summary>
            public double GenutztMwh;

            public double UeberschussMwh;

            /// <summary>
            /// Abgeregelte PV-Energie [MWh/a] an der Einspeisegrenze (PV3) — nach der Speicherladung,
            /// im Flottenpfad die Abregelung der Flotte. 0 ohne Grenze.
            /// </summary>
            public double AbregelungMwh;

            /// <summary>Die Abregelung in % der Erzeugung der Module; 0 ohne Erzeugung.</summary>
            public double AbregelungProzent;

            /// <summary>Die Einspeisegrenze des Laufs [kW]; <c>null</c> = keine.</summary>
            public double? EinspeisegrenzeKw;

            public double DeckungProzent;
            public double StrombedarfMwh;
            public double ReststrombedarfMwh;

            /// <summary>
            /// Maximale solare Einstrahlung auf die Modulebene [W/m²]
            /// (<c>SimulationPV.MaxPSolar</c>). Hieß bis W11b‑B‑7 „MaxLeistungKw" und
            /// stand im Reiter mit der Einheit kW neben einer Beschriftung in W/m².
            /// </summary>
            public double MaxEinstrahlungWm2;

            public List<PvModulZeile> Module = new List<PvModulZeile>();

            /// <summary>
            /// Die Wechselrichter der Anlagen, die auf der Strangebene gerechnet haben —
            /// leer ohne Strangzuordnung (Stufe S3, Vorrangregel des Konzepts 3.5/7.1).
            /// </summary>
            public List<PvWechselrichterZeile> Wechselrichter = new List<PvWechselrichterZeile>();
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

            // W11b-B-6 (Windows-Abnahme V3, 07.09.2026): "Gesamte Stromerzeugung der
            // Module" ist die ERZEUGUNG - Stromproduktion_Theoretisch, nach Wechselrichter
            // und vor dem Abgleich mit dem Bedarf; dieselbe Reihe, die die Modultabelle
            // summiert. Stromproduktion ist der GENUTZTE Anteil min(Erzeugung, Bedarf):
            // Ohne Strombedarf stand die Zeile damit auf 0,00, waehrend Ueberschuss und
            // Tabelle 13,26 MWh zeigten. Der Vorlaeufer (:4551) hatte dieselbe Reihe;
            // der Port war woertlich, die Beschriftung nicht. Der Deckungsgrad bleibt
            // am genutzten Anteil - das ist seine Definition.
            double erzeugungKwh = pv.Stromproduktion_Theoretisch.Sum();
            double genutztKwh = pv.Stromproduktion.Sum();
            // E29 (#536, E29‑Q10 a) mit SB1 (a): je Viertelstunde geklemmt - wortgleich mit
            // SimulationRunner.
            double bedarfKwh = SimulationControl.NetzbezugGeklemmt(pv.Strombedarf).Sum() / 4.0;

            e.StromproduktionMwh = erzeugungKwh / 1000.0;
            e.GenutztMwh = genutztKwh / 1000.0;
            e.EinspeisegrenzeKw = pv.EinspeisegrenzeKw;
            if (sim.Speicherflottennetzbilanz != null)
            {
                e.UeberschussMwh = sim.Speicherflottennetzbilanz.PvNetzeinspeisungKwh / 1000.0;
                e.AbregelungMwh = sim.Speicherflottennetzbilanz.PvAbregelungKwh / 1000.0;
            }
            else
            {
                // Der Reiter zeigt den Überschuss VOR der Speicherladung (die Einspeisung mit
                // Speicher steht im Reiter „Stromspeicher"). PV3: die Abregelung je Viertelstunde
                // nach der Speicherladung (Laden vor Abregeln) - dieselbe Aufteilung wie
                // SimulationRunner.
                e.UeberschussMwh = pv.Ueberschuss.Sum() / 1000.0;
                sim.PvEinspeisungAufteilen(out _, out double[] abregelungKw);
                e.AbregelungMwh = SimulationPV.ViertelstundenKwh(abregelungKw) / 1000.0;
            }
            e.AbregelungProzent = erzeugungKwh > 0 ? e.AbregelungMwh * 1000.0 / erzeugungKwh * 100.0 : 0.0;
            e.DeckungProzent = bedarfKwh > 0 ? genutztKwh * 100.0 / bedarfKwh : 0.0;
            // E28 (#535, E28‑Q3 a): dieselbe Klemme wie die Ergebniszeile (SimulationRunner).
            e.StrombedarfMwh = SimulationControl.NetzbezugGeklemmt(pv.Strombedarf).Sum() / 4000.0;
            e.ReststrombedarfMwh = sim.Speicherflottennetzbilanz != null
                ? sim.Speicherflottennetzbilanz.NetzbezugKwh / 1000.0
                : sim.Rest_Strombedarf_viertelstuendlich.Sum() / 4000.0;
            e.MaxEinstrahlungWm2 = pv.MaxPSolar;

            if (pv.Modul_Ergebnisse != null)
                foreach (var m in pv.Modul_Ergebnisse)
                {
                    e.Module.Add(new PvModulZeile(m.Name, m.Flaeche, m.Anzahl,
                                                  m.StromproduktionKwh / 1000.0,
                                                  m.FlaecheGeschaetzt));

                    // Stufe S3: je Geraet eine Zeile - und nur, wenn diese Anlage auf
                    // der Strangebene gerechnet hat. Ohne Zuordnung bleibt die Liste
                    // leer, und der Reiter zeigt die Tabelle nicht.
                    if (m.Geraete == null) continue;
                    foreach (PvStrangModell.Geraetegruppe g in m.Geraete)
                        e.Wechselrichter.Add(new PvWechselrichterZeile(
                            m.Name, g.Anzeigename, g.DcAc, g.ErtragKwh / 1000.0,
                            g.ClippingKwh, g.ClippingAnteilProzent, g.VolllaststundenAc,
                            g.Jahresnutzungsgrad, g.NachtKwh));
                }

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
                e.StrombedarfGesamtMwh = sb.StrombedarfGesamtMwh;
            }
            return e;
        }

        // =================================================================
        //  Kälte (Stufe KU1; Kühlkonzept 8.4; E21, K5, K6)
        // =================================================================

        /// <summary>
        /// Die Zahlen der KÄLTESEITE eines Laufs — Bedarfsreiter und Übersicht. Es gibt sie nur,
        /// wenn der Lauf Kälte ERHOBEN hat (<see cref="SimulationKaeltebedarf.Gerechnet"/>): Ein
        /// Projekt ohne Kühlung zeigt keine Kühlnullen, sondern gar keine Kältegruppe (K18).
        /// Jahressummen in MWh, Spitze in kW, die Stundenreihe in kWh je Stunde.
        /// </summary>
        public sealed class KaelteErgebnis
        {
            /// <summary>Jahreskälte = Summe des Kühlkanals [MWh] (<c>kaelte.jahresbedarf</c>).</summary>
            public double KaeltebedarfMwh;

            /// <summary>Kältespitze [kW] (<c>Kaeltelast_Max</c>, <c>kaelte.spitze</c>).</summary>
            public double KaeltelastMaxKw;

            /// <summary>Stunden mit Kühlbedarf, gezählt am Kanalvektor (<c>kaelte.stunden</c>).</summary>
            public int StundenMitKuehlbedarf;

            /// <summary>Vollbenutzungsstunden der Kälte [h/a] — aus Jahreskälte und Spitze; <c>null</c> ohne Spitze.</summary>
            public double? VollbenutzungsstundenH;

            /// <summary>Ungedeckte Kälte [MWh] (<c>Kaelterestbedarf</c>) — ohne Kälteerzeuger der ganze Bedarf.</summary>
            public double KaelterestbedarfMwh;

            /// <summary>Gedeckte Kälte [MWh] — die Kältekaskade (Stufe KU2); 0 ohne Kälteerzeuger.</summary>
            public double KaeltedeckungMwh;

            /// <summary>Hat ein Kälteerzeuger gerechnet (Wärmepumpe im Kühlbetrieb, Stufe KU2)?</summary>
            public bool MitKaelteerzeuger;

            /// <summary>Davon aus den Gebäuden [MWh].</summary>
            public double GebaeudeMwh;

            /// <summary>Davon aus Lastgängen mit dem Kanal „Kühlung" [MWh].</summary>
            public double ExternMwh;

            /// <summary>Zahl der Gebäude, deren Kühlreihe in den Kühlkanal ging.</summary>
            public int GekuehlteGebaeude;

            /// <summary>Stunden mit gleichzeitigem Heizen und Kühlen (K6), Maximum über die Gebäude.</summary>
            public int StundenHeizenUndKuehlen;

            /// <summary>Das Gebäude, von dem <see cref="StundenHeizenUndKuehlen"/> stammt.</summary>
            public string StundenHeizenUndKuehlenGebaeude = "";

            /// <summary>Der Kühlkanal je Stunde [kWh] — für das eigene Bild und den CSV-Export.</summary>
            public double[] KaeltebedarfKwh = new double[0];

            // ---- Die Deckung (Stufe KU2 Welle 3; Kühlkonzept 8.4; E21, E34) ----------

            /// <summary>Deckungsgrad des Kühlkanals [%] — nur mit Kälteerzeuger und Kältebedarf.</summary>
            public double? DeckungsgradProzent;

            /// <summary>Kältestrom aller Kälteerzeuger samt Hilfsstrom [MWh].</summary>
            public double KaeltestromMwh;

            /// <summary>Jahresarbeitszahl Kälte (EER-Jahreswert) = Kälte / Kältestrom; <c>null</c> ohne Kältestrom.</summary>
            public double? EerJahreswert;

            /// <summary>Der Netzbezug des Kältestroms [MWh] — anteilig bzw. über eigene Zähler (E34).</summary>
            public double KaeltestromNetzbezugMwh;

            /// <summary>Die Kälteerzeuger in Kaskadenreihenfolge — die Zeilen der Kälteerzeugertabelle (#32).</summary>
            public List<KaelteerzeugerZeile> Erzeuger = new List<KaelteerzeugerZeile>();
        }

        /// <summary>Eine Zeile der Kälteerzeugertabelle (Stufe KU2 Welle 3) — Mengen in MWh.</summary>
        public sealed class KaelteerzeugerZeile
        {
            /// <summary>Bezeichner der Anlage.</summary>
            public string Bezeichner = "";

            /// <summary>Der Kühl-Vorlauf, mit dem gerechnet wurde [°C].</summary>
            public int Vorlauf;

            /// <summary>Gedeckte Kälte [MWh].</summary>
            public double KaelteMwh;

            /// <summary>Kältestrom samt Hilfsstrom [MWh].</summary>
            public double StromMwh;

            /// <summary>EER-Jahreswert; <c>null</c> ohne Kältestrom.</summary>
            public double? Eer;

            /// <summary>Netzbezug des Kältestroms [MWh] (E34).</summary>
            public double NetzbezugMwh;

            /// <summary>Abweichender Kühlträger (<c>energy_carrier.id</c>); 0 = Stromträger des Projekts.</summary>
            public int Kuehltraeger;

            /// <summary>Eigener Zähler (E34, Wahl 2)?</summary>
            public bool EigenerZaehler;
        }

        /// <summary>
        /// Die Kälteseite des Laufs; <c>null</c>, wenn nicht erhoben (Projektschalter aus) — das
        /// ist etwas anderes als ein Kältebedarf von 0.
        /// </summary>
        public static KaelteErgebnis Kaelte(SimulationWaermebedarf wb)
        {
            SimulationKaeltebedarf k = wb?.Kaelteseite;
            if (k == null || !k.Gerechnet) return null;

            var e = new KaelteErgebnis
            {
                KaeltebedarfMwh = k.Kaeltebedarf_Gesamt,
                KaeltelastMaxKw = k.Kaeltebedarf_Max,
                StundenMitKuehlbedarf = k.StundenMitKuehlbedarf,
                VollbenutzungsstundenH = k.Kaeltebedarf_Max > 0 ? k.VollbenutzungsstundenKaelte : (double?)null,
                KaelterestbedarfMwh = k.Kaelterestbedarf,
                KaeltedeckungMwh = k.Kaskade != null ? k.Kaskade.DeckungGesamtKwh / 1000.0 : 0.0,
                MitKaelteerzeuger = k.Kaskade != null && k.Kaskade.Erzeuger.Count > 0,
                GebaeudeMwh = k.Kaeltebedarf_Gebaeude_Gesamt,
                ExternMwh = k.Kaeltebedarf_Extern_Gesamt,
                GekuehlteGebaeude = k.GekuehlteGebaeude,
                StundenHeizenUndKuehlen = k.StundenHeizenUndKuehlen,
                StundenHeizenUndKuehlenGebaeude = k.StundenHeizenUndKuehlenGebaeude ?? "",
                KaeltebedarfKwh = (double[])k.Kaeltebedarf.Clone()
            };

            // STUFE KU2 WELLE 3 (Kühlkonzept 8.4): die Deckung der Kälteerzeuger - Deckungsgrad,
            // Kältestrom, EER-Jahreswert, Netzbezug und je Erzeuger eine Tabellenzeile.
            Kaeltekaskade kaskade = k.Kaskade;
            if (kaskade != null && kaskade.Erzeuger.Count > 0)
            {
                e.DeckungsgradProzent = k.Kaeltebedarf_Gesamt > 0
                    ? SimulationRunner.DeckungKuehlkanalProzent(k) : (double?)null;
                e.KaeltestromMwh = kaskade.StromGesamtKwh / 1000.0;
                e.EerJahreswert = kaskade.StromGesamtKwh > 0 ? kaskade.EerJahreswert : (double?)null;
                double netz = 0.0;
                foreach (Kaelteerzeuger z in kaskade.Erzeuger)
                {
                    netz += z.NetzbezugKwh;
                    e.Erzeuger.Add(new KaelteerzeugerZeile
                    {
                        Bezeichner = z.Bezeichner ?? "",
                        Vorlauf = z.Kennlinie != null ? z.Kennlinie.Vorlauf : 0,
                        KaelteMwh = z.KaelteGesamtKwh / 1000.0,
                        StromMwh = z.StromGesamtKwh / 1000.0,
                        Eer = z.StromGesamtKwh > 0 ? z.EerJahreswert : (double?)null,
                        NetzbezugMwh = z.NetzbezugKwh / 1000.0,
                        Kuehltraeger = z.Kuehltraeger,
                        EigenerZaehler = z.NebenDerStufenrechnung
                    });
                }
                e.KaeltestromNetzbezugMwh = netz / 1000.0;
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
        public static double[] WarmwasserAnteil(SimulationWaermebedarf wb, double[] bedarf)
        {
            double[] ww = new double[Kanalsatz.STUNDEN_JAHR];
            if (wb == null || wb.brauchwasserwerte == null) return ww;

            double[] quelle = wb.brauchwasserwerte;
            for (int i = 0; i < Kanalsatz.STUNDEN_JAHR && i < quelle.Length; i++)
            {
                double wert = quelle[i];
                if (bedarf != null && i < bedarf.Length && wert > bedarf[i]) wert = bedarf[i];
                if (wert < 0) wert = 0;
                ww[i] = wert;
            }
            return ww;
        }
    }
}

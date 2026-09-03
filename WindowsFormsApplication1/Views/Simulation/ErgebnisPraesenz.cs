using System;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Welche Komponenten gehören zu dem Ergebnis, das gerade angezeigt wird?
    ///
    /// <b>Warum es diese Klasse gibt.</b> Die Ergebnisansichten (Übersicht-Reiter der
    /// Detailform, NavigatorWaerme/-Strom/-Uebersicht, DashboardForm) zeigten bis hierher
    /// IMMER alle Komponenten — Zeilen, Checkboxen, Chartserien und Tortensegmente für
    /// Solarthermie, BHKW und PV auch dann, wenn das Projekt nichts davon enthält. Der
    /// Anwender sah „0,00" oder leere Felder und wirkungslose Schalter. Die Regel dafür
    /// gehört an EINE Stelle, sonst driften die vier Ansichten auseinander.
    ///
    /// <b>Die Regel.</b> Eine Komponente gilt als vorhanden, wenn mindestens eines zutrifft:
    /// <list type="number">
    ///   <item>Sie war Bestandteil des Laufs (<c>SimulationControl.bSimulation…</c>) —
    ///         das ist der maßgebliche Marker, denn genau dieses Flag wird als
    ///         <c>ErgebnisModel.Sim_…</c> mitgespeichert.</item>
    ///   <item>Ihre Modulliste ist gefüllt (<c>wp_list</c>, <c>spk_list</c>, …) — die
    ///         Anlagen des Projekts, die die Engine für den Lauf geladen hat.</item>
    ///   <item>Sie hat im Ergebnis einen Wert &gt; 0.</item>
    ///   <item>Das Projekt führt eine Anlage dieses Typs in <c>Tab_Energieanlagen</c>.</item>
    /// </list>
    ///
    /// Punkt 1 allein genügt nicht: Er sagt nur, dass die Kaskadenstufe gerechnet hat.
    /// Punkt 4 allein genügt ebenso wenig: Er kennt das Ergebnis nicht. Die ODER-Verknüpfung
    /// hat die Eigenschaft, auf die es ankommt — <b>eine vorhandene Anlage mit 0-kWh-Ergebnis
    /// bleibt sichtbar</b> (sonst verschwände die Antwort auf die Frage „warum liefert mein
    /// Kessel nichts?"), und eine nicht vorhandene Komponente verschwindet.
    ///
    /// <b>„Gesamt", „Wärmebedarf", „Restwärme-/Reststrombedarf" und der Energiebedarf-Block
    /// sind nie betroffen</b> — sie beschreiben das Projekt, nicht eine Komponente.
    /// <b>Heizstab</b> hängt an der Wärmepumpe (er ist Teil von ihr, kein eigener Erzeuger).
    /// <b>Speicher</b> zählt allein über die Speicherliste des Laufs
    /// (<see cref="SimulationControl.AlleSpeicher"/>), weil die Füllstandsserien genau aus
    /// ihr entstehen.
    ///
    /// <b>Reine Anzeige.</b> Nichts hier schreibt; der Anlagenbestand wird dialogfrei über
    /// <see cref="StilleDb"/> gelesen (kein <c>DataRepository</c>: dessen MessageBox im
    /// Fehlerfall hätte in einem headless laufenden Prüfprogramm nichts zu suchen).
    /// Schlägt die Abfrage fehl, bleibt es bei den Punkten 1–3.
    /// </summary>
    internal sealed class ErgebnisPraesenz
    {
        /// <summary>Wärmepumpe vorhanden.</summary>
        public bool Waermepumpe;

        /// <summary>Heizstab — nur bei vorhandener Wärmepumpe.</summary>
        public bool Heizstab;

        /// <summary>Heizkessel/Spitzenkessel vorhanden.</summary>
        public bool Heizkessel;

        /// <summary>Solarthermie vorhanden.</summary>
        public bool Solarthermie;

        /// <summary>BHKW vorhanden.</summary>
        public bool BHKW;

        /// <summary>Photovoltaik vorhanden.</summary>
        public bool Photovoltaik;

        /// <summary>Mindestens ein Speicher (Senken-Puffer oder Quellspeicher) im Lauf.</summary>
        public bool Speicher;

        /// <summary>
        /// Stromspeicher mit Rechenergebnis im Lauf (AP3b).
        ///
        /// Bewusst NICHT über den Anlagenbestand (Punkt 4 der Regel): Ein
        /// Speichersegment im Strom-Donut hat nur dann eine Größe, wenn die
        /// <c>SpeicherEngine</c> gerechnet hat. Ein vorhandener, aber nicht gerechneter
        /// Speicher (Tool 6 aus, kein Lauf) hätte sonst ein Segment der Größe 0.
        /// </summary>
        public bool Stromspeicher;

        /// <summary>
        /// Rückfallebene: alles sichtbar. Für Aufrufer ohne <see cref="SimulationControl"/>
        /// und für den Zustand vor dem ersten Lauf — dort darf nichts verschwinden, was
        /// später erscheinen soll.
        /// </summary>
        public static ErgebnisPraesenz Alles()
        {
            return new ErgebnisPraesenz
            {
                Waermepumpe = true,
                Heizstab = true,
                Heizkessel = true,
                Solarthermie = true,
                BHKW = true,
                Photovoltaik = true,
                Speicher = true,
                Stromspeicher = true
            };
        }

        /// <summary>
        /// Ermittelt die Präsenz nach der oben beschriebenen Regel.
        /// <paramref name="sim"/> darf <c>null</c> sein — dann gilt <see cref="Alles"/>.
        /// </summary>
        public static ErgebnisPraesenz Ermitteln(SimulationControl sim)
        {
            if (sim == null) return Alles();

            ErgebnisPraesenz p = new ErgebnisPraesenz();

            // --- Punkte 1 bis 3: der Lauf selbst -------------------------------------
            p.Waermepumpe = sim.bSimulationWP
                            || (sim.simulation_wp != null &&
                                (sim.simulation_wp.wp_list.Count > 0 ||
                                 sim.simulation_wp.WP_Waermeproduktion_gesamt > 0));

            p.Heizkessel = sim.bSimulationKessel
                           || (sim.simulation_spk != null &&
                               (sim.simulation_spk.spk_list.Count > 0 ||
                                sim.simulation_spk.S_Waerme_spk > 0));

            p.Solarthermie = sim.bSimulationSolarthermie
                             || (sim.simulation_solarthermie != null &&
                                 (sim.simulation_solarthermie.solarthermie_list.Count > 0 ||
                                  sim.simulation_solarthermie.Waermeproduktion_gesamt > 0));

            p.BHKW = sim.bSimulationBHKW
                     || (sim.simulation_bhkw != null &&
                         (sim.simulation_bhkw.bhkw_list.Count > 0 ||
                          sim.simulation_bhkw.Waermeproduktion_BHKW_MWh > 0 ||
                          sim.simulation_bhkw.Stromproduktion_BHKW_MWh > 0));

            p.Photovoltaik = sim.bSimulationPV
                             || (sim.simulation_pv != null &&
                                 (sim.simulation_pv.photovoltaik_list.Count > 0 ||
                                  sim.simulation_pv.Stromproduktion_gesamt > 0));

            // --- Punkt 4: der Anlagenbestand des Projekts ----------------------------
            AnlagenbestandUebernehmen(sim.m_ID_Projekt, p);

            // --- Abhängige Angaben ---------------------------------------------------
            // Der Heizstab ist Teil der Wärmepumpe und hat keine eigene Anlagenzeile.
            p.Heizstab = p.Waermepumpe;

            // Die Füllstandsserien entstehen aus der Speicherliste des Laufs - ohne sie
            // gibt es nichts anzuzeigen, unabhängig vom Anlagenbestand.
            p.Speicher = (sim.AlleSpeicher() != null && sim.AlleSpeicher().Count > 0);

            // Der Stromspeicher zählt allein über das Engine-Ergebnis (AP3b) - es ist
            // die einzige Quelle der Entladeenergie, aus der das Donut-Segment entsteht.
            p.Stromspeicher = (sim.bSimulationSSP && sim.Speicherergebnis != null);

            return p;
        }

        /// <summary>
        /// Punkt 4 der Regel: eine einzige Abfrage über die Typen der Projektanlagen.
        /// Schlägt sie fehl (oder ist kein Projekt gesetzt), bleibt <paramref name="p"/>
        /// unverändert — die Anzeige fällt dann auf die Lauf-Auskunft zurück, statt
        /// fälschlich Komponenten auszublenden.
        /// </summary>
        private static void AnlagenbestandUebernehmen(int idProjekt, ErgebnisPraesenz p)
        {
            if (idProjekt <= 0) return;

            DataTable dt = StilleDb.Tabelle(
                "SELECT DISTINCT ID_Type FROM Tab_Energieanlagen WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));

            if (dt == null) return;   // stiller Fehler - Regel greift ohne Punkt 4

            foreach (DataRow r in dt.Rows)
            {
                switch (StilleDb.Zahl(StilleDb.Feld(r, "ID_Type"), -1))
                {
                    case WizardItemClass.WP_TYP: p.Waermepumpe = true; break;
                    case WizardItemClass.SOLAR_TYP: p.Solarthermie = true; break;
                    case WizardItemClass.PV_TYP: p.Photovoltaik = true; break;
                    case WizardItemClass.KESSEL_TYP: p.Heizkessel = true; break;
                    case WizardItemClass.BHKW_TYP: p.BHKW = true; break;
                }
            }
        }
    }
}

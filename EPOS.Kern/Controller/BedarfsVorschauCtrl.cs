using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das Ergebnis einer Bedarfsvorschau (iU9-W14b.0b): je nach Ausprägung ein
    /// gerechnetes <see cref="SimulationWaermebedarf"/> ODER
    /// <see cref="SimulationStrombedarf"/> — nie beides.
    ///
    /// <para>Die Vorschau SCHREIBT nichts. Sie füllt ein Rechenobjekt, das der
    /// Ergebnisdialog danach nur liest.</para>
    /// </summary>
    internal sealed class BedarfsVorschau
    {
        /// <summary>Die Ausprägung, für die gerechnet wurde.</summary>
        internal BedarfsArt Art;

        /// <summary>Hat die Engine gerechnet? Bei <c>false</c> bleibt der Ergebnisdialog zu.</summary>
        internal bool Erfolgreich;

        /// <summary>Der Wärmebedarf — bei <see cref="BedarfsArt.Stromverbraucher"/> <c>null</c>.</summary>
        internal SimulationWaermebedarf Waerme;

        /// <summary>Der Strombedarf — nur bei <see cref="BedarfsArt.Stromverbraucher"/>.</summary>
        internal SimulationStrombedarf Strom;
    }

    /// <summary>
    /// <b>Die Vorschaurechnung der drei Bedarfsverwaltungen</b> (iU9-W14b.0b) — der
    /// Knopf „Grafik" von <c>Form_Brauchwasser_Admin</c>,
    /// <c>Form_Prozesswaerme_Admin</c> und <c>Form_Stromverbraucher_Admin</c>.
    ///
    /// <para><b>Warum es den Controller gibt.</b> Dieselbe Rechnung stand dreimal im
    /// Formularcode (<c>btn_Simulation_Click</c>: 79‑85, 92‑99, 95‑109) und
    /// unterschied sich in genau vier Punkten — Simulationsklasse, Engine-Methode,
    /// Teiler und Nachlauf. Alle vier hängen an <see cref="BedarfsArt"/>, und damit
    /// gehört die Rechnung dorthin, wo die Ausprägung schon liegt: in den Kern.</para>
    ///
    /// <para><b>Bitgleich je Art.</b> Was hier steht, ist Zeile für Zeile das, was die
    /// drei Masken taten — einschließlich des FEHLENDEN Teilers beim Brauchwasser
    /// (Befund W14‑B49): <c>Waermebedarf_Brauchwasser</c> ist die nackte Summe der
    /// Stundenreihe und liegt damit in KILOWATTSTUNDEN, während jede andere Größe der
    /// Klasse in MWh vorliegt. Das ist kein Versehen, das hier zu beheben wäre,
    /// sondern die Grundlage des Anwenderentscheids W8‑O‑5 vom 04.09.2026: Die
    /// Einheit steht seither AM WERT (<see cref="Energieeinheit"/>), und der
    /// Ergebnisdialog rechnet um. Würde die Vorschau hier durch 1000 teilen, wäre die
    /// Zahl anschließend ein zweites Mal geteilt.</para>
    ///
    /// <para><b>Der Nachweis</b> steht in
    /// <c>EPOS.Kern.Tests/BedarfVerwaltungTests.cs</c>: Die drei Vorrechnungen sind
    /// dort einmal WÖRTLICH wie in den Masken eingefroren und einmal gegen diesen
    /// Controller gehalten.</para>
    /// </summary>
    internal static class BedarfsVorschauCtrl
    {
        /// <summary>
        /// Rechnet die Vorschau für EINEN Katalogsatz.
        /// </summary>
        /// <param name="art">Die Ausprägung.</param>
        /// <param name="idProjekt">
        /// Das Projekt der Maske (<c>m_ID_Projekt</c>). Es war in allen drei
        /// Verwaltungen 0 — sie werden nie mit einem Projekt geöffnet —, wird aber
        /// wörtlich durchgereicht: Die Engine wählt daran ihren Kalender.
        /// </param>
        /// <param name="bezeichner">Der gewählte Katalogsatz; leer ergibt keine Rechnung.</param>
        internal static BedarfsVorschau Rechnen(BedarfsArt art, int idProjekt, string bezeichner)
        {
            var ergebnis = new BedarfsVorschau { Art = art };
            if (string.IsNullOrEmpty(bezeichner)) return ergebnis;

            // Die Liste mit EINEM Namen - woertlich wie in den drei Masken. Sie ist
            // zugleich das Zeichen fuer die Engine, im Modus Katalogvorschau zu rechnen
            // (list == null hiesse Projektrechnung).
            var liste = new List<string> { bezeichner };

            if (art == BedarfsArt.Stromverbraucher) return Strom(ergebnis, idProjekt, liste);
            return Waerme(ergebnis, art, idProjekt, liste);
        }

        /// <summary>
        /// Brauchwasser und Prozesswärme — beide über <see cref="SimulationWaermebedarf"/>,
        /// unterschieden allein durch Engine-Methode, Zielfeld und Teiler.
        /// </summary>
        private static BedarfsVorschau Waerme(BedarfsVorschau ergebnis, BedarfsArt art,
                                              int idProjekt, List<string> liste)
        {
            var sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };

            if (art == BedarfsArt.Prozesswaerme)
            {
                // Form_Prozesswaerme_Admin:95-99 - MIT Teiler.
                sim.Prozesswaerme_berechnen(liste);
                sim.Waermebedarf_Prozess = sim.prozesswerte.Sum() / 1000;
                WPPlan.Core.BhkwPlan.MonatsSumme(sim.prozesswerte, sim.Waermebedarf_Prozess_Monat,
                                                 sim.mo_anfang, sim.mo_ende);
            }
            else
            {
                // Form_Brauchwasser_Admin:82-85 - OHNE Teiler (Befund W14-B49): Der Wert
                // liegt in kWh, und genau so nennt ihn die Ergebnishuelle seit W8-O-5.
                sim.Brauchwasserwaerme_berechnen(liste);
                sim.Waermebedarf_Brauchwasser = sim.brauchwasserwerte.Sum();
                WPPlan.Core.BhkwPlan.MonatsSumme(sim.brauchwasserwerte, sim.Waermebedarf_Brauchwasser_Monat,
                                                 sim.mo_anfang, sim.mo_ende);
            }

            ergebnis.Waerme = sim;
            ergebnis.Erfolgreich = true;
            return ergebnis;
        }

        /// <summary>
        /// Der Stromverbraucher — die längste der drei Rechnungen
        /// (<c>Form_Stromverbraucher_Admin</c>:95‑109).
        ///
        /// <para><b>Die Null-Prüfung bleibt</b> (<c>:99</c>): Sie greift bei einem
        /// unbekannten Bezeichner zwar nicht — dann kommt eine leere Reihe zurück, keine
        /// <c>null</c> —, aber die Engine hat andere Wege, an denen sie es tut, und der
        /// Vorläufer stieg dort aus.</para>
        /// </summary>
        private static BedarfsVorschau Strom(BedarfsVorschau ergebnis, int idProjekt,
                                             List<string> liste)
        {
            var sim = new SimulationStrombedarf { m_ID_Projekt = idProjekt };

            float[] reihe = sim.Stromprofil_Strombedarf_berechnen(liste);
            if (reihe == null) return ergebnis;

            sim.Strombedarf_Gebaeude_gesamt = reihe.Sum() / 1000;

            // Das Zielfeld hat 35 040 Plaetze (Viertelstunden); belegt werden die ersten
            // 8 760. Woertlich wie im Vorlaeufer.
            Array.Copy(reihe, sim.Strombedarf_viertelStundenwerte, reihe.Length);

            WPPlan.Core.BhkwPlan.MonatsSumme(sim.Strombedarf_viertelStundenwerte, sim.Strombedarf_monat,
                                             sim.mo_anfang, sim.mo_ende);
            sim.Strombedarf_Max = sim.Maximaler_Strombedarf(sim.Strombedarf_viertelStundenwerte);
            sim.Strombedarf_gesamt = sim.Strombedarf_Gebaeude_gesamt;

            ergebnis.Strom = sim;
            ergebnis.Erfolgreich = true;
            return ergebnis;
        }
    }
}

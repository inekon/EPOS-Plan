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

        /// <summary>
        /// Rechnet die Vorschau den Zapfprofilweg (Umsetzungskonzept Zapfprofilgenerator 2.2,
        /// 5.2)? Dann trägt <see cref="Waerme"/> die Monatssummen von Zapfung und Zirkulation
        /// und <c>Waerme.Zapfprofil</c> das Ergebnis des Generators.
        /// </summary>
        internal bool Zapfprofilweg;

        /// <summary>
        /// Der benannte Grund, wenn der Zapfprofilweg nicht rechnen konnte
        /// (<see cref="Erfolgreich"/> <c>false</c>); bei Erfolg die Ablehnungen einzelner Zonen,
        /// die 0 tragen (je Zeile eine, N8); sonst leer.
        /// </summary>
        internal string Meldung = "";
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
    /// <para><b>Bitgleich je Art — mit EINER Ausnahme, dem Brauchwasser.</b> Was hier
    /// steht, ist Zeile für Zeile das, was die drei Masken taten. Der FEHLENDE Teiler
    /// beim Brauchwasser (Befund W14‑B49) ist seit dem Anwenderentscheid <b>W8‑O‑5b</b>
    /// vom 07.09.2026 nicht mehr dabei: <c>Waermebedarf_Brauchwasser</c> lag hier in
    /// kWh und im Lauf in MWh, und die Ergebnisanzeige konnte nur eine der beiden
    /// Angaben glauben — sie teilte den Wert des Laufs deshalb ein zweites Mal. Beide
    /// Wege setzen das Feld jetzt über <c>SimulationWaermebedarf.BrauchwassersummeUebernehmen</c>,
    /// also in MWh, und die Anzeige rechnet von dort über <see cref="Energieeinheit"/>
    /// in die gewählte Einheit um. Die ANGEZEIGTE Zahl der Vorschau bleibt damit
    /// unverändert; die des Laufs wird richtig.</para>
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
        /// <b>Die Vorschau AUS EINEM PROJEKT</b> — der Knopf „Simulation" des
        /// Bedarfsprofildialogs (Windows-Abnahme 05.09.2026, Befund W8‑B‑3).
        ///
        /// <para><b>Warum sie hierher gehört.</b> Bis zu diesem Befund stand sie in der
        /// Windows-Hülle (<c>BedarfsProfileHuelle.Rechenstand.Rechnen</c>) als zweite,
        /// von Hand nachgezogene Abschrift derselben Rechnung — und in ihr fehlte beim
        /// Strom die Zeile, die <c>Strombedarf_Gebaeude_gesamt</c> belegt. Die Anzeige
        /// stand danach auf 0. Eine Vorschau ist keine zweite Rechnung; sie rechnet, was
        /// der Lauf rechnet, nur auf einer Namensliste.</para>
        ///
        /// <para><b>Der Unterschied zur Katalogvorschau</b> ist die Namensliste: Hier
        /// kommen MEHRERE Namen herein — die Zuordnungen des Projekts —, und sie sind
        /// die der PROJEKTKOPIEN (W9‑O‑3c). Die Engine sucht daraufhin zuerst in der
        /// Kopie und erst danach im Katalog.</para>
        /// </summary>
        /// <param name="art">Die Ausprägung.</param>
        /// <param name="idProjekt">Das Projekt des Dialogs.</param>
        /// <param name="namen">Die angezeigten Profilnamen; leer ergibt keine Rechnung.</param>
        /// <param name="zapfprofil">
        /// Der Arbeitsstand des Zapfprofils im Dialog (Umsetzungskonzept Zapfprofilgenerator 2.2,
        /// 5.2); <c>null</c> = der gespeicherte Stand des Projekts. Steht er beim Brauchwasser
        /// auf <see cref="BrauchwasserWeg.Generator"/>, zeigt die Vorschau die Monatssummen aus
        /// dem Generator über diesen Stand — derselbe Aufruf wie im Lauf
        /// (<see cref="SimulationWaermebedarf.BrauchwasserAusGenerator"/>), mit dem Kalender
        /// der Klimaregion des Projekts, stets auf dem deterministischen Pfad (5.1); die
        /// Profilnamen rechnen dann nicht mit. Bei
        /// <see cref="BrauchwasserWeg.Bestand"/> bleibt der Weg dieser Methode unverändert.
        /// </param>
        internal static BedarfsVorschau ProjektVorschau(BedarfsArt art, int idProjekt,
                                                       IReadOnlyList<string> namen,
                                                       ZapfprofilStand zapfprofil = null)
        {
            var ergebnis = new BedarfsVorschau { Art = art };

            if (art == BedarfsArt.Brauchwasser && idProjekt != 0)
            {
                ZapfprofilStand stand = zapfprofil ?? ZapfprofilCtrl.Lies(idProjekt);
                if (stand.Weg == BrauchwasserWeg.Generator) return ZapfprofilVorschau(ergebnis, idProjekt, stand);
            }

            if (namen == null) return ergebnis;

            var liste = new List<string>(namen);

            if (art == BedarfsArt.Stromverbraucher) return Strom(ergebnis, idProjekt, liste);

            var sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };

            if (art == BedarfsArt.Prozesswaerme)
            {
                sim.Prozesswaerme_berechnen(liste);

                // W9-O-3 (04.09.2026): Die Prozesssumme geht ueber die EINHEITENKLASSE in
                // die Einheit, die der Kern fuer Waermebedarf_Prozess fuehrt - MWh.
                sim.ProzesssummeUebernehmen();
                WPPlan.Core.BhkwPlan.MonatsSumme(sim.prozesswerte, sim.Waermebedarf_Prozess_Monat,
                                                 sim.mo_anfang, sim.mo_ende);
            }
            else
            {
                // W8-O-5b (07.09.2026): symmetrisch zum Prozesszweig darueber - die
                // Summe geht ueber die EINHEITENKLASSE in die Einheit, die der Kern
                // fuer Waermebedarf_Brauchwasser fuehrt (MWh). Vorher stand hier die
                // nackte Summe in kWh, waehrend der Lauf MWh auswies.
                sim.Brauchwasserwaerme_berechnen(liste);
                sim.BrauchwassersummeUebernehmen();
                WPPlan.Core.BhkwPlan.MonatsSumme(sim.brauchwasserwerte,
                                                 sim.Waermebedarf_Brauchwasser_Monat,
                                                 sim.mo_anfang, sim.mo_ende);
            }

            ergebnis.Waerme = sim;
            ergebnis.Erfolgreich = true;
            return ergebnis;
        }

        /// <summary>
        /// Die Vorschau des Zapfprofilwegs (2.2): Kalender der Klimaregion des Projekts, dann
        /// derselbe Generatoraufruf wie im Lauf. Kann der Generator für das Projekt nicht
        /// rechnen, bleibt die Vorschau ohne Ergebnis (<see cref="BedarfsVorschau.Erfolgreich"/>
        /// <c>false</c>) und nennt den Grund in <see cref="BedarfsVorschau.Meldung"/> — wie der
        /// Lauf, der dann abbricht. Abgelehnte Zonen tragen 0 wie im Lauf; die Meldung nennt sie
        /// (N8).
        ///
        /// <para><b>Immer der deterministische Pfad (5.1).</b> Die Vorschau rechnet live — sie zieht
        /// kein Jahresensemble, auch wenn der Stand die Jahresreihe stochastisch führt: Die
        /// stochastische Reihe entsteht erst im Lauf (im Dialog nebenläufig mit „Stochastisch
        /// rechnen"). Die Jahresmenge ist in beiden Wegen dieselbe (Energieprobe, 4.4).</para>
        /// </summary>
        private static BedarfsVorschau ZapfprofilVorschau(BedarfsVorschau ergebnis, int idProjekt, ZapfprofilStand stand)
        {
            ergebnis.Zapfprofilweg = true;
            if (stand.Projekt != null && stand.Projekt.JahresreiheStochastisch)
                stand = stand with { Projekt = stand.Projekt with { JahresreiheStochastisch = false } };
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(idProjekt);
            if (projekt.m_ID_Klimaregion <= 0)
            {
                ergebnis.Meldung = SimulationWaermebedarf.ZAPFPROFIL_PRAEFIX
                                   + "Das Projekt hat keine Klimaregion — ohne Kalender keine Vorschau.";
                return ergebnis;
            }

            var sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };
            sim.ZapfprofilKalenderLesen(projekt.m_ID_Klimaregion);
            bool ok = sim.BrauchwasserAusGenerator(stand);
            sim.BrauchwassersummeUebernehmen();

            ergebnis.Waerme = sim;
            ergebnis.Erfolgreich = ok;
            ergebnis.Meldung = !ok ? sim.Fehlertext
                : string.Join(Environment.NewLine, sim.Zapfprofil.Ablehnungen.Select(a =>
                      SimulationWaermebedarf.ZAPFPROFIL_PRAEFIX + SimulationWaermebedarf.ZapfAblehnungstext(a)));
            return ergebnis;
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
                // Form_Brauchwasser_Admin:82-85 stand OHNE Teiler da (Befund W14-B49) -
                // der Wert lag in kWh. Seit W8-O-5b uebernimmt der Kern die Summe in
                // MWh, wie im Prozesszweig und wie im Lauf; die angezeigte Zahl bleibt
                // dieselbe, weil die Anzeige die Einheit AM WERT liest.
                sim.Brauchwasserwaerme_berechnen(liste);
                sim.BrauchwassersummeUebernehmen();
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
        ///
        /// <para><b>Die sechs Zuweisungen stehen seit W8‑B‑3 im Kern</b>
        /// (<see cref="SimulationStrombedarf.ProfilbedarfUebernehmen"/>) — die
        /// Katalogvorschau hier und die Projektvorschau des Bedarfsprofildialogs teilen
        /// sich damit EINE Fassung. Die eingefrorenen Zahlen der Katalogvorschau
        /// (<c>BedarfVerwaltungTests</c>) bleiben davon unberührt: Die Rechnung ist
        /// dieselbe, nur steht sie nicht mehr zweimal da.</para>
        /// </summary>
        private static BedarfsVorschau Strom(BedarfsVorschau ergebnis, int idProjekt,
                                             List<string> liste)
        {
            var sim = new SimulationStrombedarf { m_ID_Projekt = idProjekt };

            double[] reihe = sim.Stromprofil_Strombedarf_berechnen(liste);
            if (reihe == null) return ergebnis;

            sim.ProfilbedarfUebernehmen(reihe);

            ergebnis.Strom = sim;
            ergebnis.Erfolgreich = true;
            return ergebnis;
        }
    }
}

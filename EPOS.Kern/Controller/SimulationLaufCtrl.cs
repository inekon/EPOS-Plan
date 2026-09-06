using System;
using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der SIMULATIONSLAUF als Kernvorgang: vorprüfen, Bedarf rechnen, bestücken,
    /// laufen lassen, Abbruchgrund auswerten, Ergebnis speichern (iU9-W11a.4).
    ///
    /// <para><b>Warum es diesen Controller gibt.</b> <c>Form_Simulation_Detail
    /// .btn_Simulation_Click</c> (:3419-3531) war der Lauf: Konfiguration lesen,
    /// Netzverluste prüfen, Klimaregion prüfen, Bedarf rechnen, <c>sim</c> bestücken,
    /// <c>Do_Simulation</c> rufen, Abbruch auswerten — 112 Zeilen Fachablauf in einem
    /// Klickhandler, jede Prüfung als <c>MessageBox</c>. Ein Fehler ist hier eine
    /// RÜCKGABE; ob daraus ein Dialog, ein Warnbanner oder ein Protokolleintrag wird,
    /// entscheidet die Oberfläche.</para>
    ///
    /// <para><b>Zwei Wege, ein Ablauf.</b> Denselben Ablauf führt
    /// <see cref="SimulationRunner.Simuliere"/> headless — er liest die Betriebsart aus
    /// der Konfiguration, die Maske aus ihren Bedienelementen. Das ist der einzige
    /// Unterschied, und er bleibt: Die Maske darf nicht ignorieren, was der Anwender
    /// gerade eingestellt hat. <see cref="Bestuecken"/> nimmt diese Größen deshalb als
    /// Parameter statt sie zu lesen.</para>
    ///
    /// <para><b>Datenbank und Faden.</b> <see cref="Vorpruefen"/>, <see cref="Bedarf"/>
    /// und <see cref="Bestuecken"/> LESEN die Datenbank und gehören auf den
    /// Oberflächenfaden. <see cref="Laufen"/> darf in <c>Task.Run</c> — Probe R-W10a-2
    /// hat gezeigt, dass der Datenzugriff keinen Fadenbezug hat (eigene Verbindung je
    /// Aufruf, nichts <c>[ThreadStatic]</c>). Vorgezogen werden muss deshalb NICHTS;
    /// die Aufteilung ist trotzdem die aus <c>Form_SpeicherOptimierung</c>, weil sie den
    /// Ablauf lesbar hält.</para>
    /// </summary>
    public static class SimulationLaufCtrl
    {
        /// <summary>
        /// Prüft, ob der Lauf überhaupt beginnen kann, und liefert den Fehlertext —
        /// <c>null</c>, wenn alles steht.
        ///
        /// <para>Drei Gründe, in der Reihenfolge des Vorläufers:</para>
        /// <list type="number">
        ///   <item>Es gibt keine Konfigurationszeile (<c>SIM_MSG_KONFIGURATION_FEHLT</c>).</item>
        ///   <item>Netzverluste über 100 %, gemessen NUR bei der Einheit „%"
        ///   (<c>SIM_MSG_NETZVERLUSTE_ZU_GROSS</c>).</item>
        ///   <item>Das Projekt führt keine Klimaregion
        ///   (<c>SIM_MSG_KLIMAREGION_WAEHLEN</c>).</item>
        /// </list>
        ///
        /// <para>Die Schemamigrationssperre steht bewusst NICHT hier: Sie gehört zum
        /// Programmzustand, nicht zum Projekt, und die Maske prüft sie schon vor dem
        /// Zurücksetzen der Anzeige (<c>SimulationBlockiert</c>). Der Rechenkern
        /// wiederholt sie ohnehin in <c>Do_Simulation</c>.</para>
        /// </summary>
        /// <param name="idProjekt">Das Projekt.</param>
        /// <param name="konfig">
        /// Die gelesene Konfiguration; <c>null</c> heißt „keine Zeile gefunden".
        /// </param>
        /// <param name="idKlimaregion">Die Klimaregion des Projekts; 0 heißt „keine".</param>
        public static string Vorpruefen(int idProjekt, KonfigurationModel konfig, int idKlimaregion)
        {
            // Der LESEMODUS steht VOR allem anderen (Welle iF30, Anwenderentscheid
            // 04.09.2026). Ein Lauf endet mit ErgebnisSpeichern, und der schriebe an der
            // Schreibnaht auf - nach Minuten Rechenzeit und mitten im Speichern. Die Frage
            // gehört deshalb an den Anfang: erst der Grund, dann gar kein Lauf.
            string lesemodus = LesemodusGrund();
            if (lesemodus != null) return lesemodus;

            if (konfig == null) return MyResource.Resource.SIM_MSG_KONFIGURATION_FEHLT;

            // Wörtlich aus Energiebedarf :3953: NUR bei der Einheit „%" und NUR über 100.
            if (konfig.m_szNetzverlusteEinheit == "%" && (int)konfig.m_Netzverluste > 100)
                return MyResource.Resource.SIM_MSG_NETZVERLUSTE_ZU_GROSS;

            if (idKlimaregion == 0) return MyResource.Resource.SIM_MSG_KLIMAREGION_WAEHLEN;

            return null;
        }

        /// <summary>
        /// Der Grund, warum im LESEMODUS nicht gerechnet werden darf — <c>null</c>, wenn
        /// die Lizenz Arbeitsergebnisse erlaubt (Welle iF30).
        /// </summary>
        /// <remarks>
        /// <para><b>Warum eine eigene Frage und nicht die Schreibnaht.</b> Die Naht wirft
        /// dort, wo die erste schreibende Anweisung steht — bei einem Simulationslauf ist
        /// das <c>ErgebnisCtrl.Save</c>, also NACH der ganzen Rechnung. Der Anwender sähe
        /// eine Meldung, nachdem er eine Minute gewartet hat. Die Frage steht deshalb ein
        /// zweites Mal, ganz vorn; die Naht bleibt der Riegel dahinter.</para>
        /// <para><b>Ansehen bleibt frei.</b> Diese Prüfung gehört zum LAUF und nicht zur
        /// Ergebnisansicht: Ein gespeichertes Ergebnis darf im Lesemodus geöffnet,
        /// betrachtet, berichtet und exportiert werden (Konzept § 6).</para>
        /// </remarks>
        public static string LesemodusGrund()
        {
            return Schreibnaht.DarfSchreiben() ? null : MyResource.Resource.SIM_MSG_LESEMODUS;
        }

        /// <summary>
        /// Rechnet Wärme- und Strombedarf des Projekts (Schritt 7 aus § 1c der
        /// Vermessung) und liefert den Fehlertext der Stromrechnung — <c>null</c> bei
        /// Erfolg.
        ///
        /// <para><b>Die beiden Bedarfsobjekte gehören dem Aufrufer</b> (Befund W11-B3):
        /// <c>Form_Start</c> reicht sie in die Detailansicht hinein und nutzt sie danach
        /// für seine Kachelbeschriftungen weiter. Sie werden deshalb hereingereicht und
        /// AN ORT UND STELLE gefüllt, nicht neu angelegt.</para>
        ///
        /// <para>Die Reihenfolge ist die des Vorläufers und die des Runners: Netzverluste
        /// setzen, Wärmebedarf rechnen, den Wochentagskalender der Wärmerechnung an die
        /// Stromrechnung geben (K1/F3 — sonst ermitteln beide je einen eigenen), Strom
        /// rechnen.</para>
        /// </summary>
        public static string Bedarf(int idProjekt, int idKlimaregion,
                                    double netzverluste, string netzverlusteEinheit,
                                    SimulationWaermebedarf waerme, SimulationStrombedarf strom)
        {
            waerme.Netzverluste = (int)netzverluste;
            waerme.Netzverluste_Einheit = netzverlusteEinheit;
            waerme.Waermebedarf_berechnen(idProjekt, idKlimaregion);

            strom.m_ID_Projekt = idProjekt;
            strom.WochentagJan1 = waerme.WochentagJan1;
            strom.Berechnung(idProjekt);

            return string.IsNullOrEmpty(strom.Fehlertext) ? null : strom.Fehlertext;
        }

        /// <summary>
        /// Bestückt den Lauf mit allem, was er braucht (Schritt 8 aus § 1c).
        ///
        /// <para><b>Der letzte Datenbankzugriff vor dem Lauf</b> —
        /// <c>PufferSpCtrl.PendelspeicherVolumenLiter</c> liest den Projekt-Puffer
        /// „BHKW-Pendelspeicher". Danach ist <see cref="Laufen"/> für die Oberfläche
        /// eine reine Rechnung.</para>
        ///
        /// <para><c>grenzleistungBhkw</c> und <c>modusBhkw</c> kommen als Parameter,
        /// weil Maske und Runner sie aus verschiedenen Quellen nehmen: die Maske aus
        /// ihren Bedienelementen (der Anwender hat sie eben eingestellt), der Runner aus
        /// der gespeicherten Konfiguration.</para>
        /// </summary>
        public static void Bestuecken(SimulationControl sim, int idProjekt, string[] tool,
                                      SimulationWaermebedarf waerme, SimulationStrombedarf strom,
                                      KonfigurationCtrl konfig,
                                      int grenzleistungBhkw, int modusBhkw)
        {
            sim.tool = tool;
            sim.Stundentemperatur = waerme.Stundentemperatur;
            sim.simulation_Waermebedarf = waerme;
            sim.simulation_Strombedarf = strom;
            sim.ctrl_konfig = konfig;
            sim.GrenzleistungBHKW = grenzleistungBhkw;
            sim.VolumenPendelspeicherBHKW = PufferSpCtrl.PendelspeicherVolumenLiter(idProjekt);
            sim.modeBHKW = modusBhkw;
        }

        /// <summary>
        /// Der Lauf selbst — der Teil, der in <c>Task.Run</c> gehört.
        ///
        /// <para>Meldet seine Phasen über <paramref name="fortschritt"/> und prüft
        /// <paramref name="abbruch"/> zwischen ihnen; ein Abbruch verlässt die Methode
        /// mit <see cref="OperationCanceledException"/>, und der angefangene Lauf ist
        /// zu verwerfen.</para>
        /// </summary>
        public static void Laufen(SimulationControl sim, int idProjekt,
                                  IProgress<LaufFortschritt> fortschritt = null,
                                  CancellationToken abbruch = default)
        {
            sim.Do_Simulation(idProjekt, fortschritt, abbruch);
        }

        /// <summary>
        /// Der ABBRUCHGRUND eines gelaufenen Laufs — <c>null</c>, wenn er durchgegangen
        /// ist (wörtlich aus <c>Form_Simulation_Detail.LaufAbgebrochen</c> :3551-3578).
        ///
        /// <para>Zwei Quellen, in dieser Reihenfolge: <c>sim.Sperrgrund</c> (der Lauf ist
        /// gar nicht erst angelaufen — Schemamigration) und <c>sim.Fehlertext</c> (ein
        /// Erzeugermodul hat abgebrochen). Angehängt werden die weiteren Fehlermeldungen
        /// desselben Laufs aus dem Protokoll — sie erreichten die Oberfläche vorher
        /// nicht, und ohne sie hat der Anwender keinen Ansatzpunkt.</para>
        /// </summary>
        public static string Abbruchgrund(SimulationControl sim)
        {
            if (sim == null) return null;

            string grund = !string.IsNullOrEmpty(sim.Sperrgrund) ? sim.Sperrgrund : sim.Fehlertext;
            if (string.IsNullOrEmpty(grund)) return null;

            string weitere = SimulationProtokoll.Aktuell.FehlertextFuerAnzeige(grund);
            if (string.IsNullOrEmpty(weitere)) return grund;

            return grund + Environment.NewLine + Environment.NewLine +
                   MyResource.Resource.SIM_MSG_WEITERE_FEHLERMELDUNGEN + Environment.NewLine + weitere;
        }

        /// <summary>
        /// Speichert das Ergebnis des Laufs nach <c>Tab_Ergebnis*</c> (wörtlich aus
        /// <c>SpeichereErgebnis</c> :3712-3760, ohne <c>Program.mainfrm</c>).
        ///
        /// <para><b>Das Auffrischen der Startmaske bleibt beim Aufrufer.</b> Der
        /// Vorläufer rief danach <c>Program.mainfrm.SetSPControl(...)</c> — im Kern ist
        /// <c>Program.*</c> verboten, und eine Oberflächenauffrischung ist auch keine
        /// Aufgabe des Speicherns.</para>
        ///
        /// <para>Gebaut wird über <see cref="SimulationRunner.BaueErgebnis"/> — dieselbe
        /// Quelle, aus der auch der headless-Lauf speichert.</para>
        /// </summary>
        /// <returns><c>true</c>, wenn geschrieben wurde (<c>ErgebnisCtrl.Save</c>
        /// liefert die Ergebnis-Kopf-Id, oder -1).</returns>
        public static bool ErgebnisSpeichern(int idProjekt,
                                             SimulationWaermebedarf waerme,
                                             SimulationStrombedarf strom,
                                             SimulationControl sim)
        {
            if (idProjekt <= 0) return false;

            ErgebnisModel m = SimulationRunner.BaueErgebnis(idProjekt, waerme, strom, sim);
            return new ErgebnisCtrl().Save(m) > 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
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

        // =================================================================
        // #190 - ERZEUGER OHNE KASKADENPLATZ (Abnahmeliste „PV mit Heizkessel")
        // =================================================================

        /// <summary>
        /// Sprachneutrale Kennung des Befunds „Waermeerzeuger angelegt, aber in keinem
        /// Kaskadenplatz <c>Tool_1..4</c>" (#190).
        /// </summary>
        public const string KRIT_ERZEUGER_OHNE_KASKADENPLATZ = "LAUF_W_ERZEUGER_OHNE_KASKADENPLATZ";

        /// <summary>
        /// Sprachneutrale Kennung des Befunds „Stromerzeuger bzw. Energiespeicher
        /// angelegt, aber nicht auf seinem Platz <c>Tool_5</c>/<c>Tool_6</c>" (#190).
        /// </summary>
        public const string KRIT_ERZEUGER_OHNE_STROMPLATZ = "LAUF_W_ERZEUGER_OHNE_STROMPLATZ";

        /// <summary>
        /// Die Erzeugeranlagen des Projekts, die in KEINEM Platz der Simulation stehen —
        /// die Vorpruefung zum Abnahmebefund „PV mit Heizkessel: die Simulation
        /// beruecksichtigt den Heizkessel nicht" (#190). Nie <c>null</c>; leer = alles
        /// Angelegte rechnet auch.
        ///
        /// <para><b>Der Befund.</b> Ein Waermeerzeuger rechnet ausschliesslich dann,
        /// wenn seine Technologie in einem der vier Kaskadenplaetze
        /// <c>Tab_Einstellungen.Tool_1..4</c> steht (<c>SimulationControl</c>,
        /// Erzeugerdurchlauf ueber <c>tool[]</c>); Photovoltaik und Stromspeicher
        /// haengen ebenso an ihren Plaetzen <c>Tool_5</c>/<c>Tool_6</c>. Einen Kessel im
        /// Assistenten oder im Erzeugerdialog ANZULEGEN legt aber keinen Platz an —
        /// <see cref="Kaskade.Aufnehmen"/> hat genau einen Aufrufer, das „+ aufnehmen"
        /// der verfuegbaren Karte. Bis hierher rechnete die Anlage deshalb STILL nicht:
        /// keine Meldung, kein Protokolleintrag, nur eine Null in der Ergebnisuebersicht.
        /// Genau so sieht das Referenzprojekt 1007 aus (Kessel angelegt,
        /// <c>Tool_1..4 = ('', Solarthermie, Waermepumpe, '')</c>).</para>
        ///
        /// <para><b>Sie MELDET, sie aendert nichts</b> — Anwenderentscheid <b>HK-E-1a</b>
        /// vom 11.09.2026: Erzeuger ohne Kaskadenplatz werden gemeldet, nicht automatisch
        /// aufgenommen; die Referenzbasis R7 bleibt. Kein Platz wird hier belegt, das
        /// waere eine Ergebnisaenderung an jedem Bestandsprojekt mit einer solchen
        /// Luecke. Der Weg zurueck steht im Text: die Simulationskonfiguration blendet
        /// ihre verfuegbaren Karten ein, „+ aufnehmen" bleibt der Handgriff des
        /// Anwenders.</para>
        ///
        /// <para>Gelesen wird ueber <see cref="StilleDb"/> — dialogfrei, weil derselbe
        /// Weg im unbeaufsichtigten Referenz- und CI-Lauf benutzt wird.</para>
        /// </summary>
        /// <param name="idProjekt">Das Projekt.</param>
        /// <param name="konfig">
        /// Die gelesene Konfiguration; <c>null</c> heisst „kein Platz belegt".
        /// </param>
        public static List<Warnbefund> ErzeugerOhneKaskadenplatz(int idProjekt,
                                                                 KonfigurationModel konfig)
        {
            List<string> plaetze = Kaskade.Lesen(konfig);
            plaetze.Add(Kaskade.StromWert(konfig, Kaskade.PLATZ_STROMERZEUGER));
            plaetze.Add(Kaskade.StromWert(konfig, Kaskade.PLATZ_ENERGIESPEICHER));

            return ErzeugerOhneKaskadenplatz(idProjekt, plaetze);
        }

        /// <summary>
        /// Dieselbe Pruefung gegen die Platzbelegung EINES LAUFS
        /// (<c>SimulationControl.tool</c>) statt gegen die gespeicherte Konfiguration —
        /// so meldet der Lauf, was er wirklich gerechnet hat (#190).
        /// </summary>
        /// <param name="plaetze">
        /// Die belegten Plaetze in beliebiger Reihenfolge; leere Eintraege und
        /// <c>null</c> werden uebergangen.
        /// </param>
        public static List<Warnbefund> ErzeugerOhneKaskadenplatz(int idProjekt,
                                                                 IList<string> plaetze)
        {
            List<Warnbefund> befunde = new List<Warnbefund>();
            if (idProjekt <= 0) return befunde;

            List<string> belegt = new List<string>();
            if (plaetze != null)
                foreach (string p in plaetze)
                    if (!string.IsNullOrEmpty(p) && !belegt.Contains(p)) belegt.Add(p);

            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, ID_Type, Bezeichner FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? ORDER BY ID_Type, ID",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));

            if (dt == null) return befunde;   // stiller Fehler - lieber nichts melden

            foreach (DataRow r in dt.Rows)
            {
                int idType = StilleDb.Zahl(StilleDb.Feld(r, "ID_Type"), -1);

                string dbWert = ErzeugerDbWert(idType);
                if (dbWert == null) continue;                 // Puffer, Referenzanlagen …
                if (belegt.Contains(dbWert)) continue;        // steht auf einem Platz

                string bezeichner = StilleDb.Text(StilleDb.Feld(r, "Bezeichner"));
                bool strom = idType == WizardItemClass.PV_TYP ||
                             idType == WizardItemClass.SP_TYP;

                befunde.Add(new Warnbefund
                {
                    Kriterium = strom ? KRIT_ERZEUGER_OHNE_STROMPLATZ
                                      : KRIT_ERZEUGER_OHNE_KASKADENPLATZ,
                    Hart = false,
                    ID_Anlage = StilleDb.Zahl(StilleDb.Feld(r, "ID"), 0),
                    Steuerwert = dbWert,
                    Text = string.Format(
                        strom ? MyResource.Resource.SIM_W_ERZEUGER_OHNE_STROMPLATZ
                              : MyResource.Resource.SIM_W_ERZEUGER_OHNE_KASKADENPLATZ,
                        ErzeugerAnzeige(idType), bezeichner)
                });
            }

            return befunde;
        }

        /// <summary>
        /// Der Steuerwert (<c>DbWerte.ERZEUGER_*</c>) zu einem
        /// <c>Tab_Energieanlagen.ID_Type</c>; <c>null</c> = kein Erzeuger mit eigenem
        /// Platz (Pufferspeicher, Referenzanlagen des Vergleichsfalls).
        /// </summary>
        private static string ErzeugerDbWert(int idType)
        {
            switch (idType)
            {
                case WizardItemClass.WP_TYP: return DbWerte.ERZEUGER_WAERMEPUMPE;
                case WizardItemClass.SOLAR_TYP: return DbWerte.ERZEUGER_SOLARTHERMIE;
                case WizardItemClass.PV_TYP: return DbWerte.ERZEUGER_PHOTOVOLTAIK;
                case WizardItemClass.SP_TYP: return DbWerte.ERZEUGER_STROMSPEICHER;
                case WizardItemClass.KESSEL_TYP: return DbWerte.ERZEUGER_HEIZKESSEL;
                case WizardItemClass.BHKW_TYP: return DbWerte.ERZEUGER_BHKW;
                default: return null;
            }
        }

        /// <summary>
        /// Der ANZEIGENAME der Erzeugerart — dieselben sechs Ressourcen, die
        /// <c>ErzeugerKatalog.Anzeige</c> der Oberflaeche liefert. Er steht hier ein
        /// zweites Mal, weil <c>ErzeugerKatalog</c> in der Windows-Anwendung liegt und
        /// der Kern sie nicht kennt.
        /// </summary>
        private static string ErzeugerAnzeige(int idType)
        {
            switch (idType)
            {
                case WizardItemClass.WP_TYP: return MyResource.Resource.KONFIG_WAERMEPUMPE;
                case WizardItemClass.SOLAR_TYP: return MyResource.Resource.KONFIG_SOLARTHERMIE;
                case WizardItemClass.PV_TYP: return MyResource.Resource.KONFIG_PHOTOVOLTAIK;
                case WizardItemClass.SP_TYP: return MyResource.Resource.KONFIG_STROMSPEICHER;
                case WizardItemClass.KESSEL_TYP: return MyResource.Resource.KONFIG_HEIZKESSEL;
                case WizardItemClass.BHKW_TYP: return MyResource.Resource.KONFIG_BHKW;
                default: return "";
            }
        }
    }
}

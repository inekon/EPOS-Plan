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
        /// Protokoll- und Fehlerkanal dieses Laufs (Paket 8, Konzept 13.4).
        ///
        /// <see cref="Simuliere"/> erzeugt ihn als ERSTES und legt ihn hier ab; er
        /// überlebt den Aufruf, damit der Aufrufer die HINWEISE abholen kann, ohne dass
        /// dafür eine Signatur wachsen musste. Bestehende Aufrufer bleiben deshalb
        /// unverändert übersetzbar:
        ///
        /// <code>
        /// var runner = new SimulationRunner();
        /// int id = runner.SimuliereUndSpeichere(idProjekt, out string fehler);
        /// //  … fehler    = Fehler und Warnungen (nur bei Abbruch belegt)
        /// //  … runner.Protokoll.Hinweise = die nicht abbrechenden Meldungen
        /// </code>
        /// </summary>
        public SimulationProtokoll Protokoll = SimulationProtokoll.Aktuell;

        /// <summary>
        /// true, wenn der letzte <see cref="Simuliere"/>-Aufruf die RECHNUNG vollständig
        /// durchgebracht hat — unabhängig davon, ob das anschließende Speichern gelang
        /// (Nacharbeit Paket 8, Befund N2).
        ///
        /// <see cref="SimuliereUndSpeichere"/> liefert -1 für beide Fälle. Der
        /// Berichtssammler braucht die Unterscheidung: Aus einem gerechneten, aber nicht
        /// gespeicherten Lauf lassen sich die Stundenreihen für die Ganglinien trotzdem
        /// abholen (<c>ZeitreihenExtraktor.AusLauf</c>).
        /// </summary>
        public bool LaufOk { get; private set; }

        /// <summary>
        /// Führt die komplette Simulation für ein Projekt aus (ohne UI).
        /// Rückgabe false + Fehlertext, wenn Konfiguration oder Klimaregion fehlen.
        ///
        /// PAKET 8 (Konzept 13.4): Der ganze Lauf steht im dialogfreien Modus von
        /// <see cref="DataRepository"/>, und alle Meldungen der Engine laufen über
        /// <see cref="Protokoll"/> zusammen. <paramref name="fehler"/> trägt am Ende
        /// den Text des Abbruchgrunds, ergänzt um die Warnungen des Laufs
        /// (<c>Protokoll.AlsText(nurFehlerUndWarnungen: true)</c>).
        /// </summary>
        public bool Simuliere(int idProjekt, out string fehler)
        {
            using (DataRepository.EngineModus())
            {
                LaufOk = false;
                bool ok = Simuliere_Intern(idProjekt, out fehler);
                LaufOk = ok;

                // Datenbankfehler, die im dialogfreien Modus aufgelaufen sind, gehören in
                // den Kanal - sonst wären sie zwar nicht mehr im Weg, aber auch nicht
                // mehr sichtbar.
                foreach (string meldung in DataRepository.StilleFehlerAbholen())
                    Protokoll.Warnung(string.Format(MyResource.Resource.SIMENG_DB_ZUGRIFF_WAEHREND_LAUF, meldung));

                if (!ok)
                {
                    // Der Abbruchgrund zuerst, dann die Warnungen desselben Laufs. Fehlt
                    // ein ausdrücklicher Grund (Modul hat nur abgebrochen), liefert der
                    // Kanal ihn.
                    string ausKanal = Protokoll.AlsText(true);
                    if (string.IsNullOrEmpty(fehler)) fehler = ausKanal;
                    else if (!string.IsNullOrEmpty(ausKanal) && ausKanal.IndexOf(fehler, StringComparison.Ordinal) < 0)
                        fehler = fehler + Environment.NewLine + ausKanal;
                }

                return ok;
            }
        }

        private bool Simuliere_Intern(int idProjekt, out string fehler)
        {
            fehler = null;

            // Paket 8: EIN Kanal je Lauf, angelegt VOR der Bedarfsrechnung - auch
            // SimulationWaermebedarf und SimulationStrombedarf melden dorthin.
            Protokoll = SimulationProtokoll.NeuStarten();

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
                fehler = string.Format(MyResource.Resource.SIMENG_KEINE_KONFIGURATION, idProjekt);
                return false;
            }

            // Netzverluste prüfen.
            int netzverluste = (int)ctrl.m_Netzverluste;
            if (ctrl.m_szNetzverlusteEinheit == "%" && netzverluste > 100)
            {
                fehler = MyResource.Resource.SIMENG_NETZVERLUSTE_UEBER_100;
                return false;
            }

            // Klimaregion aus dem Projekt.
            ProjektCtrl projektCtrl = new ProjektCtrl();
            projektCtrl.ReadSingle(idProjekt);
            int nKlimaregion = projektCtrl.m_ID_Klimaregion;
            if (nKlimaregion == 0)
            {
                fehler = string.Format(MyResource.Resource.SIMENG_KEINE_KLIMAREGION, idProjekt);
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
            // K1 (F3): denselben Klimadaten-Kalender wie die Wärmerechnung verwenden -
            // erspart der Stromrechnung die eigene Klimadaten-Lesung und schließt aus,
            // dass beide Bedarfsarten je einen anderen Wochentag ermitteln.
            simulation_Strombedarf.WochentagJan1 = simulation_Waermebedarf.WochentagJan1;
            simulation_Strombedarf.Berechnung(idProjekt);

            // PAKET 8 (Konzept 13.4): Bricht die Strombedarfsrechnung ab, war das bisher
            // eine MessageBox und der Lauf rechnete mit leerem Stromprofil weiter - ein
            // Ergebnis, das vollständig aussah und keines war. Jetzt bricht der Lauf ab.
            if (!string.IsNullOrEmpty(simulation_Strombedarf.Fehlertext))
            {
                fehler = simulation_Strombedarf.Fehlertext;
                return false;
            }

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

            // Paket-5-Nacharbeit, Befund N10: Der zweikanalige Weg meldet Abbrüche
            // dialogfrei über den Fehlerkanal statt über eine MessageBox (Konzept 13.4).
            // Ein solcher Lauf hat kein vollständiges Ergebnis und darf keines speichern.
            if (!string.IsNullOrEmpty(sim.Fehlertext))
            {
                fehler = sim.Fehlertext;
                return false;
            }

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
            // AP2b: bSimulationSSP steht seit dem Engine-Einbau für einen ECHTEN
            // Speicherlauf - bis dahin hieß es nur "Tool 6 war aktiv".
            m.Sim_Stromspeicher = sim.bSimulationSSP;

            // Detail: Waerme-/Strombedarf (immer vorhanden).
            m.Energiebedarf = new ErgebnisEnergiebedarfModel();
            m.Energiebedarf.Waermebedarf_Gesamt = simulation_Waermebedarf.Waermebedarf_Gesamt;
            m.Energiebedarf.Waermelast_Max = simulation_Waermebedarf.Waermebedarf_Max;
            m.Energiebedarf.Strombedarf_Gesamt = simulation_Strombedarf.Strombedarf_gesamt;
            m.Energiebedarf.Strombedarf_Max = simulation_Strombedarf.Strombedarf_Max;
            m.Energiebedarf.Waermerestbedarf = sim.Restwaerme;   // Restwärmebedarf nach allen Erzeugern
            m.Energiebedarf.Stromrestbedarf = sim.Reststrom;     // Reststrombedarf/Netzbezug

            // PAKET E1 (Konzept 4.4): der Jahresbedarf JE KANAL [MWh]. Quelle ist der
            // Kanalsatz, aus dem seit Paket K1 auch Waermebedarf_Gesamt gebildet wird
            // (die Kanäle sind die führende Größe, die Summe die abgeleitete) — es ist
            // die Aufschlüsselung desselben Werts, keine zweite Rechnung. Die Summe der
            // drei Spalten ist deshalb Waermebedarf_Gesamt bis auf die float-Rundung, mit
            // der Kanalsatz.Summe() den Summenvektor stundenweise bildet.
            m.Energiebedarf.Waermebedarf_Kanal = BedarfJeKanal(simulation_Waermebedarf);

            // Detail: Waermepumpe (nur wenn gerechnet), Werte wie in der WP-Ansicht (MWh).
            if (sim.bSimulationWP)
            {
                SimulationWaermepumpe wp = sim.simulation_wp;
                ErgebnisWaermepumpeModel w = new ErgebnisWaermepumpeModel();
                w.Waermebedarf = wp.Waermebedarf_gesamt / 1000.0;
                w.Waermeproduktion_WP = wp.WP_Waermeproduktion_gesamt / 1000.0;
                w.Stromverbrauch_WP = wp.WP_Strombedarf_gesamt / 1000.0;
                w.Stromverbrauch_Heizstab = wp.Heizstab_gesamt / 1000.0;
                // B0-7a: Vorbelegung aus der Stundenganglinie. Sie wird seit
                // Nutzerentscheidung 6-5 weiter unten ausnahmslos überschrieben
                // (Stufeneingang minus Eigenanteil) und steht hier nur noch als
                // definierter Ausgangswert.
                w.Restwaermebedarf = wp.waermerestbedarf_gesamt / 1000.0;
                // Paket 7 / Konzept 6.6: Kapazität kommt aus dem zugeordneten Speicher
                // (SimulationPufferspeicher.Q_max in kWh), nicht mehr aus dem Legacy-
                // Ausdruck Volumen · 1,16. Der alte Ausdruck rechnete ohne ΔT (also
                // implizit mit 1 K) und ohne /1000, nahm das Volumen zudem aus dem
                // WP-Datensatz statt aus dem Puffer - und widersprach damit der Anzeige.
                // DOKUMENTIERTE ERGEBNISÄNDERUNG in Projekten mit Puffer-Zuordnung.
                // Ohne zugeordneten Puffer wird 0 gespeichert (es gibt keine Kapazität).
                //
                // PAKET E1 (Konzept 6.3, Befund S-1): Nicht mehr der Alias puffer_wp (der
                // ERSTE Heizungspuffer), sondern die SUMME aller Senkenspeicher des
                // Laufs. Der Alias wies bei zwei Puffern je Kanal nur einen aus und bei
                // einem reinen Brauchwasser- oder Kombispeicher gar keinen (0), obwohl
                // der Lauf ihn bewirtschaftet hat. DOKUMENTIERTE ERGEBNISÄNDERUNG genau
                // in diesen Projekten; mit genau einem Heizungspuffer — dem Fall der
                // meisten Bestandsprojekte — ist der Wert unverändert. Begründung und
                // Abgrenzung gegen die Quellspeicher bei SenkenspeicherKapazitaet().
                w.Kapazitaet_Pufferspeicher = sim.SenkenspeicherKapazitaet();
                w.Vollbenutzungsstunden = (wp.wp_list.Count > 0) ? wp.WP_Laufzeit / wp.wp_list.Count : 0;
                w.Bivalenzpunkt = (wp.Bivalenzpunkt != -100) ? (double?)wp.Bivalenzpunkt : null;

                // Minimale Spitzenkesselleistung = max. stuendlicher Waermerestbedarf.
                double maxSpk = 0;
                for (int i = 0; i < wp.waermerestbedarf_stuendlich.Length; i++)
                    if (wp.waermerestbedarf_stuendlich[i] > maxSpk) maxSpk = wp.waermerestbedarf_stuendlich[i];
                w.Min_Spitzenkesselleistung = maxSpk;

                // EIGENANTEIL der Wärmepumpe [MWh] — Direktdeckung (Phase B) plus der ihr
                // zugerechnete Anteil an der bedarfsdeckenden Speicherentladung plus
                // Heizstab (er gehört zur WP, Tab_WP.Heizung je Modul).
                double wpEigen = (wp.Direktdeckung_gesamt + wp.Speicherentladung_Anteil +
                                  wp.Heizstab_gesamt) / 1000.0;

                // NUTZERENTSCHEIDUNG 6-5, entschieden am 15.08.2026 (Variante B):
                // Der RESTWÄRMEBEDARF der Wärmepumpe folgt jetzt derselben Regel wie
                // Solarthermie, Heizkessel und BHKW (Nutzerentscheidung 6-4):
                //
                //     Restwaermebedarf = Stufeneingang − EIGENANTEIL   (>= 0)
                //
                // Bisher meldete allein die Wärmepumpe den Rest NACH DER GANZEN
                // Speicherstufe (waermerestbedarf_gesamt, Kanalstand nach Phase F) —
                // Variante C aus 6-5. Mit genau EINEM Mitglied in der Stufe ist das
                // dieselbe Zahl; ab zwei Mitgliedern enthielt der Wert auch die Lieferung
                // von Heizkessel und BHKW, die beide ihre Deckung zusätzlich selbst
                // melden. Die Wärmepumpe wies dann einen KLEINEREN Rest aus als die
                // nachgelagerten Erzeuger (gemessen an 1024: 46,14 MWh gegen 348,84 des
                // Kessels und 229,85 des BHKW, bei identischem Stufeneingang 389,73).
                // Restbedarf und Deckung sind jetzt auch bei der WP zwei Seiten derselben
                // Rechnung; der Wert bleibt konstruktiv >= 0, weil Direktdeckung,
                // zugerechnete Entladung und Heizstab alle aus demselben Stufeneingang
                // stammen (die Klemmung ist Rundungsschutz).
                //
                // BEWUSST UNVERÄNDERT bleibt die GANGLINIE waermerestbedarf_stuendlich
                // (Export wp_restwaerme.csv) und mit ihr Min_Spitzenkesselleistung: Sie
                // führt den PROJEKTrest der Stunde, und genau der ist die Bezugsgröße für
                // die Auslegung eines Spitzenkessels — der muss decken, was nach ALLEN
                // Erzeugern offen bleibt, nicht den rechnerischen Anteil der Wärmepumpe.
                // Skalar und Ganglinie beantworten damit zwei verschiedene Fragen und
                // weichen ab zwei Stufenmitgliedern voneinander ab (1024: 246,91 MWh
                // Skalar gegen 46,14 MWh Gangliniensumme). Das ist der bewusst
                // dokumentierte Unterschied der Variante B; die Ganglinie mitzuziehen
                // (Variante C) wäre eine Änderung an Min_Spitzenkesselleistung und
                // gehört in ein eigenes Paket. Anders als beim BHKW (Befund N4) ist das
                // kein Widerspruch: Dort meldeten Skalar und Ganglinie DIESELBE Größe in
                // zwei Fassungen, hier sind es zwei verschiedene Größen.
                w.Restwaermebedarf = w.Waermebedarf - wpEigen;
                if (w.Restwaermebedarf < 0) w.Restwaermebedarf = 0;   // Rundungsschutz

                // B0-7b: Waermebedarfsdeckung (%) restbedarfsbasiert als EIGENANTEIL der
                // WP-Stufe: (Stufeneingang - Rest) / Gesamtbedarf. Bericht und
                // Wirtschaftlichkeit addieren die Erzeugeranteile zu 100 % — eine Differenz
                // gegen den Gesamtbedarf würde vorgelagerte Erzeuger doppelt zählen, wenn
                // die WP nicht an erster Kaskadenposition steht. Mit WP an erster Stelle
                // identisch zur Detailansicht; die alte produktionsbasierte Formel zählte
                // Speicherladung als Deckung.
                //
                // PAKET-5-NACHARBEIT, BEFUND N2: Mit MEHREREN Erzeugern in der
                // Speicherstufe ist "Stufeneingang − Rest nach der Stufe" kein
                // Eigenanteil mehr. Waermebedarf_stuendlich steht auf dem Eintritt in die
                // GANZE Stufe, waermerestbedarf_stuendlich auf dem Rest NACH ihr — die
                // Differenz enthält damit auch die Lieferung von Solarthermie und
                // Heizkessel, die ihre Deckung zusätzlich selbst melden. Gemessen an
                // einem präparierten 1023: Summe der ausgewiesenen Deckungen 85,71 % bei
                // tatsächlich 67,06 %.
                //
                // Der Eigenanteil wird deshalb aus den Größen gebildet, die die
                // Kaskadenschleife je Erzeuger führt:
                //   Direktdeckung (Phase B) + zugerechnete Speicherentladung + Heizstab.
                // Der Heizstab gehört zur Wärmepumpe (Tab_WP.Heizung je Modul); die
                // Zurechnung der Entladung folgt der Interimsregel "Vermischung im
                // Speicher" (siehe Kaskadenschleife). Mit genau EINEM Erzeuger in der
                // Stufe — dem Fall aller neun Referenzprojekte — ist das dieselbe Menge
                // wie bisher.
                double basis = simulation_Waermebedarf.Waermebedarf_Gesamt;
                if (basis > 0)
                {
                    // dieselbe Größe wie im Restbedarf (6-5)
                    double deckung = wpEigen / basis * 100.0;

                    if (deckung > 100) deckung = 100;
                    if (deckung < 0) deckung = 0;
                    w.Waermebedarfsdeckung = deckung;
                }

                // PAKET E1 (Konzept 4.4): dieselbe Deckung, aufgeschlüsselt auf die drei
                // Kanäle. Der Zähler ist derselbe Eigenanteil wie oben — nur eben aus den
                // KANALZEILEN, die die Module seit Paket K2 mitführen (Direktdeckung +
                // zugerechnete Speicherentladung + Heizstab). KEINE neue Verteilregel:
                // Der Kanal einer Buchung steht in der Engine fest, hier wird nur
                // umgerechnet und auf den führenden Skalar normiert.
                w.Deckung_Kanal = DeckungJeKanal(
                    Summiere(wp.Direktdeckung_Kanal, wp.Speicherentladung_Kanal, wp.Heizstab_Kanal),
                    basis, w.Waermebedarfsdeckung);

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

                // NACHARBEIT PAKET 6, BEFUND N8: Der Stufeneingang kommt aus der
                // double-Jahressumme des Moduls statt aus der Summe der float-Ganglinie.
                // Das ist dieselbe Größe, nur ohne die Summationsfehler von 8760
                // float-Additionen — und es bindet Waermebedarf_gesamt an, das bis dahin
                // nur geschrieben wurde.
                double waermebedarfMWh = bh.Waermebedarf_gesamt / 1000.0;
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

                // ETAPPE E2: die leistungsgewichteten ELEKTRISCHEN Vollbenutzungsstunden.
                // Sie sind die Bezugsgröße der KWKG-Deckelung; die beiden Zeilen darüber
                // führen THERMISCHE Vbh und können 8.760 h überschreiten. Rein additiv —
                // kein bestehender Wert dieser Zeile ändert sich dadurch.
                b.VbhElektrisch = bh.VbhElektrischGesamt;
                b.Waermebedarfsdeckung = (simulation_Waermebedarf.Waermebedarf_Gesamt > 0)
                    ? bh.Waermeproduktion_BHKW_MWh * 100.0 / simulation_Waermebedarf.Waermebedarf_Gesamt : 0;
                b.Strombedarfsdeckung = (simulation_Strombedarf.Strombedarf_gesamt > 0)
                    ? bh.Stromproduktion_BHKW_MWh * 100.0 / simulation_Strombedarf.Strombedarf_gesamt : 0;

                // PAKET 6 — Restbedarf und Deckungsgrad des BHKW, NUR im zweikanaligen Weg.
                //
                // (1) RESTBEDARF: Die Formel oben ist die Vektordifferenz
                //     „Bedarf − Produktion" — genau der Bilanzfehler aus Konzept 6.5, den
                //     auch SimulationControl bis Paket 5 gemacht hat. Sobald das BHKW einen
                //     Speicher lädt, gilt sie nicht mehr: Die geladene Wärme deckt noch
                //     keinen Bedarf.
                //
                //     NACHARBEIT PAKET 6, BEFUND N1 — ORCHESTRATOR-ENTSCHEIDUNG zur
                //     offenen Nutzerentscheidung 6-4, einheitlich für Solarthermie,
                //     Heizkessel und BHKW:
                //
                //         Restwaermebedarf = Stufeneingang − EIGENANTEIL   (>= 0)
                //         Eigenanteil      = Direktdeckung + zugerechnete Entladung
                //
                //     Die vorherige Fassung zog nur die DIREKTDECKUNG ab. Bei einer
                //     Puffer-HAUPTsenke ist die aber konstruktiv 0 (Doppelzählungs-
                //     Freibeweis, Konzept 6.3) — und genau das ist der Regelfall, den
                //     Migrationsregel R6 und ProjektPuffer.SQL_BHKW_AUF_PUFFER schreiben.
                //     Das BHKW meldete dort 100 % seines Stufeneingangs als Restbedarf und
                //     GLEICHZEITIG 84 % Deckung (gemessen an 1018: 141,45 statt 29 MWh).
                //     Mit dem Eigenanteil als Abzug sind Restbedarf und Deckung wieder
                //     zwei Seiten derselben Rechnung.
                //
                // (2) DECKUNGSGRAD: Bisher meldete das BHKW seine PRODUKTION als Deckung —
                //     der offene Punkt 4 aus der Paket-5-Nacharbeit (13.12). Jetzt ist es
                //     sein EIGENANTEIL: Direktdeckung plus der ihm zugerechnete Anteil an
                //     der bedarfsdeckenden Speicherentladung (Interimsregel „Vermischung im
                //     Speicher", siehe Kaskadenschleife). Damit geht die Summe der
                //     Erzeugerdeckungen auch mit dem BHKW als viertem Lader auf.
                //
                // PAKET A1: Der Block stand hinter „if (sim.KaskadeZweikanalig)". Die
                // Bedingung ist mit dem Altpfad entfallen; die Klammern bleiben und
                // halten die drei Hilfsgrößen beisammen.
                {
                    double bhkwDirekt = bh.Direktdeckung_gesamt / 1000.0;
                    double bhkwEigen = bhkwDirekt + bh.Speicherentladung_Anteil / 1000.0;

                    b.Restwaermebedarf = waermebedarfMWh - bhkwEigen;
                    if (b.Restwaermebedarf < 0) b.Restwaermebedarf = 0;   // Rundungsschutz

                    if (simulation_Waermebedarf.Waermebedarf_Gesamt > 0)
                    {
                        double deckungB = bhkwEigen * 100.0 / simulation_Waermebedarf.Waermebedarf_Gesamt;
                        if (deckungB > 100) deckungB = 100;
                        if (deckungB < 0) deckungB = 0;
                        b.Waermebedarfsdeckung = deckungB;
                    }

                    // PAKET E1: Aufschlüsselung derselben Deckung auf die drei Kanäle
                    // (Direktdeckung + zugerechnete Speicherentladung je Kanal).
                    b.Deckung_Kanal = DeckungJeKanal(
                        Summiere(bh.Direktdeckung_Kanal, bh.Speicherentladung_Kanal),
                        simulation_Waermebedarf.Waermebedarf_Gesamt, b.Waermebedarfsdeckung);
                }
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
                    // NICHT lokalisieren: mo.Modul wird nach Tab_ErgebnisBHKWModul.Modul
                    // GESCHRIEBEN und von der Referenzlauf-Suite als Skalar exportiert
                    // (Ergebnisexport, "…Modul[i]"). Ein übersetzter Ersatzname stünde
                    // damit in der Datenbank und ließe DE- und EN-Läufe auseinanderlaufen.
                    // Persistenzwert, immer deutsch (Drei-Schichten-Regel). Der gleichnamige
                    // Katalogschlüssel SIM_BHKW_MODUL_STANDARD gilt nur für die ANZEIGE in
                    // Form_Simulation_Detail.
                    mo.Modul = bh.bhkw_list_Namen[i] ?? "Standard BHKW";
                    mo.Waermeproduktion = bh.s_waerme_MWh[i];
                    mo.Stromproduktion = bh.s_strom_MWh[i];

                    // ETAPPE E2 (Leitentscheidung L6): die beiden Vollbenutzungsstunden-
                    // Größen je Modul. VbhThermisch ist SimulationBHKW.Laufzeiten[i] —
                    // bis hierher berechnet, aber nirgends gespeichert; VbhElektrisch ist
                    // die neue, für den KWK-Zuschlag maßgebliche Größe. Beide werden nur
                    // GESCHRIEBEN, kein Rechenweg liest sie.
                    if (i < SimulationBHKW.MAX_BHKW)
                    {
                        mo.VbhThermisch = bh.Laufzeiten[i];
                        mo.VbhElektrisch = bh.VbhElektrisch[i];
                    }

                    int cid = 0;
                    if (bh.bhkw_carrier != null && mo.Modul != null)
                        bh.bhkw_carrier.TryGetValue(mo.Modul.Trim(), out cid);
                    mo.CarrierId = cid;

                    b.Module.Add(mo);
                }

                m.BHKW = b;
            }

            // Detail: Heizkessel/Spitzenkessel (nur wenn gerechnet). Werte wie in der Kessel-Ansicht.
            if (sim.bSimulationKessel && sim.simulation_spk != null)
            {
                var spk = sim.simulation_spk;
                ErgebnisHeizkesselModel h = new ErgebnisHeizkesselModel();
                // PAKET-5-NACHARBEIT, BEFUND N1 — dieselbe Mitkorrektur wie bei der
                // Solarthermie (Konzept 6.4), die für den Kessel gefehlt hat:
                //
                // S_Waerme_spk ist die gesamte NUTZWÄRME des Kessels, seit Paket 5 also
                // Direktdeckung PLUS Speicherladung — und genau so gehört sie in
                // Waermeproduktion, denn der Brennstoffverbrauch und der
                // Jahresnutzungsgrad beziehen sich auf sie. Als BEDARFSDECKUNG taugt sie
                // nicht: Restwaermebedarf wurde negativ (gemessen an einem präparierten
                // 1018: −12,99 MWh) und die Summe der Deckungen überschritt 100 %.
                //
                // Bezugsgröße für Restbedarf und Deckung ist deshalb der EIGENANTEIL:
                // Direktdeckung plus zugerechneter Anteil an der Speicherentladung
                // (Befund N2). Ohne Puffer-Senke sind beide Zusatzgrößen exakt 0 — der
                // Ausdruck ist dann bitgleich der bisherige.
                //
                // NACHARBEIT PAKET 6, BEFUND N1: Der Restbedarf zog bis dahin nur die
                // DIREKTDECKUNG ab. Bei Puffer-Hauptsenke ist die konstruktiv 0, und der
                // Kessel meldete seinen vollen Stufeneingang als Rest — bei gleichzeitig
                // ausgewiesener Deckung. Restbedarf und Deckung folgen jetzt derselben
                // Größe. Einheitlich mit Solarthermie und BHKW (Nutzerentscheidung 6-4).
                double kesselDirekt = spk.S_Waerme_spk - spk.Speicherladung_gesamt / 1000.0;
                if (kesselDirekt < 0) kesselDirekt = 0;                      // Rundungsschutz
                double kesselEigen = kesselDirekt + spk.Speicherentladung_Anteil / 1000.0;

                h.Waermebedarf = spk.Waermebedarf_gesamt;
                h.Waermeproduktion = spk.S_Waerme_spk;
                h.Restwaermebedarf = spk.Waermebedarf_gesamt - kesselEigen;
                if (h.Restwaermebedarf < 0) h.Restwaermebedarf = 0;
                h.Strombedarf = spk.Strombedarf_gesamt / 1000.0;
                h.Reststrombedarf = spk.Strombedarf_gesamt / 1000.0 + spk.Stromverbrauch_Spk;
                h.Stromverbrauch = spk.Stromverbrauch_Spk;
                h.Waermebedarfsdeckung = 0;
                if (simulation_Waermebedarf.Waermebedarf_Gesamt > 0)
                {
                    double deckungK = kesselEigen * 100.0 / simulation_Waermebedarf.Waermebedarf_Gesamt;
                    if (deckungK > 100) deckungK = 100;
                    if (deckungK < 0) deckungK = 0;
                    h.Waermebedarfsdeckung = deckungK;
                }

                // PAKET E1: Aufschlüsselung derselben Deckung auf die drei Kanäle. Die
                // Kanalzeile der Direktdeckung summiert sich zu „Kesselabgabe −
                // Speicherladung", also genau zu kesselDirekt (siehe SimulationSPK).
                h.Deckung_Kanal = DeckungJeKanal(
                    Summiere(spk.Direktdeckung_Kanal, spk.Speicherentladung_Kanal),
                    simulation_Waermebedarf.Waermebedarf_Gesamt, h.Waermebedarfsdeckung);

                h.Maximale_Kesselleistung = spk.Maximale_Kesselleistung_Spk;
                h.Gasspitze = spk.Gasspitze_Spk;

                // ETAPPE D4: Quellwärme der Kaskade. Der Rechenkern führt sie in kWh
                // (Quellwaerme_gesamt summiert die Entladungen des Quellpuffers), die
                // Ergebniszeile in MWh - dieselbe Umrechnung wie beim Strombedarf zwei
                // Zeilen weiter oben. OHNE Quellbezug ist der Zähler exakt 0; die Spalte
                // wird trotzdem immer geschrieben, damit „keine Kaskade" und „Spalte
                // fehlt" unterscheidbar bleiben.
                h.Quellwaerme = spk.Quellwaerme_gesamt / 1000.0;
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

                    int cid = 0;
                    if (spk.spk_carrier != null && mo.Modul != null)
                        spk.spk_carrier.TryGetValue(mo.Modul.Trim(), out cid);
                    mo.CarrierId = cid;

                    h.Module.Add(mo);
                }

                m.Heizkessel = h;
            }

            // Detail: Solarthermie (nur wenn gerechnet). Werte wie in der Solarthermie-Ansicht.
            if (sim.bSimulationSolarthermie && sim.simulation_solarthermie != null)
            {
                var st = sim.simulation_solarthermie;
                ErgebnisSolarthermieModel stm = new ErgebnisSolarthermieModel();

                // Paket 5 / Konzept 6.4, ZWINGENDE MITKORREKTUR: Sobald die Solarthermie
                // zusätzlich einen Puffer lädt, wächst Waermeproduktion_gesamt über den
                // Momentanbedarf hinaus. Die alte Formel
                //   Restwaermebedarf = (Waermebedarf_gesamt − Waermeproduktion_gesamt)
                // wurde damit NEGATIV und die Deckung überschritt 100 % — beides landete
                // ungeprüft in Tab_ErgebnisSolarthermie und von dort in Variantenbericht
                // und Wirtschaftlichkeit.
                //
                // Bezugsgröße ist deshalb die DIREKTDECKUNG, also der Teil der Produktion,
                // der den Momentanbedarf gedeckt hat. Die gespeicherte Wärme deckt Bedarf
                // erst später und über den Speicher; sie einem Erzeuger zuzurechnen wäre
                // eine Doppelzählung, sobald zwei Erzeuger denselben Puffer laden. Die
                // Größe steht weiterhin vollständig in Waermeproduktion (und getrennt in
                // Speicherladung_gesamt).
                //
                // OHNE PUFFER-SENKE IST DIE KORREKTUR WIRKUNGSLOS: Dann lädt die
                // Solarthermie keinen Puffer, Speicherladung_gesamt ist exakt 0,0 und der
                // Ausdruck damit bitgleich der bisherige.
                double solarDirekt = st.Waermeproduktion_gesamt - st.Speicherladung_gesamt;
                // Rundungsschutz: Beide Summen entstehen getrennt; geht die gesamte
                // Produktion in den Speicher, kann die Differenz um wenige 1e-10 unter
                // null liegen. Ohne Puffer-Senke ist Speicherladung_gesamt exakt 0,0 und
                // Waermeproduktion_gesamt eine Summe nichtnegativer Werte — die Klemmung
                // greift dann nachweislich nie.
                if (solarDirekt < 0) solarDirekt = 0;

                // BEFUND N2 (Nacharbeit): Der DECKUNGSGRAD ist der Eigenanteil dieses
                // Erzeugers — Direktdeckung PLUS der Anteil an der Speicherentladung, die
                // Bedarf gedeckt hat (Zurechnung nach der Interimsregel "Vermischung im
                // Speicher", siehe Kaskadenschleife). Damit taucht keine kWh in zwei
                // Erzeuger-Deckungen auf, und ein Kollektorfeld, das ausschließlich einen
                // Puffer lädt, meldet nicht länger 0 % (offene Nutzerentscheidung 5-1).
                // NACHARBEIT PAKET 6, BEFUND N1: Auch der RESTBEDARF folgt jetzt dem
                // Eigenanteil, nicht mehr nur der Direktdeckung. Ein Kollektorfeld mit
                // Puffer-HAUPTsenke hat konstruktiv keine Direktdeckung (Doppelzählungs-
                // Freibeweis) und meldete deshalb seinen vollen Stufeneingang als Rest,
                // während es zugleich Deckung auswies. Beide Größen bilden jetzt dieselbe
                // Rechnung ab; "Restbedarf >= 0" bleibt erfüllt, weil Direktdeckung und
                // zugerechnete Entladung zusammen aus demselben Stufeneingang stammen
                // (Klemmung nur als Rundungsschutz). Ohne Puffer-Senke sind beide
                // Zusatzgrößen exakt 0, der Ausdruck also bitgleich der bisherige.
                double solarEigen = solarDirekt + st.Speicherentladung_Anteil;

                stm.Waermebedarf = st.Waermebedarf_gesamt / 1000.0;
                stm.Waermeproduktion = st.Waermeproduktion_gesamt / 1000.0;
                stm.Restwaermebedarf = (st.Waermebedarf_gesamt - solarEigen) / 1000.0;
                if (stm.Restwaermebedarf < 0) stm.Restwaermebedarf = 0;   // Rundungsschutz
                // PAKET E1 — BEFUND V0-O1 BEHOBEN (GEWOLLTE WERTÄNDERUNG genau dieser
                // einen Kennzahl):
                //
                // Der Nenner war bis hierher st.Waermebedarf_gesamt, also der
                // STUFENEINGANG der Solarthermie — der Bedarf, der bei ihr ankommt,
                // nachdem vorgelagerte Erzeuger der Kaskade bereits gedeckt haben.
                // Wärmepumpe, Heizkessel und BHKW teilen alle drei durch den
                // PROJEKTbedarf. Damit war die Solar-Deckung die einzige Größe, die sich
                // auf eine andere Bezugsmenge stützte: Steht die Solarthermie an zweiter
                // Kaskadenposition, ist ihr Stufeneingang kleiner als der Projektbedarf,
                // und ihr ausgewiesener Deckungsgrad fiel entsprechend ZU HOCH aus. Die
                // Summe der Erzeugerdeckungen ging dann über 100 % hinaus, obwohl genau
                // das die Eigenanteils-Logik (Befund N2) ausschließen soll — Bericht und
                // Wirtschaftlichkeit addieren die Anteile.
                //
                // Steht die Solarthermie allein bzw. an erster Position, sind beide
                // Nenner identisch und der Wert unverändert.
                //
                // Der RESTWÄRMEBEDARF oben bleibt bewusst auf dem Stufeneingang: Er ist
                // die Frage „was bleibt NACH diesem Erzeuger offen" und damit eine
                // Stufengröße — genauso wie bei Wärmepumpe, Kessel und BHKW.
                stm.Waermebedarfsdeckung = 0;
                if (simulation_Waermebedarf.Waermebedarf_Gesamt > 0)
                {
                    double deckungS = solarEigen / 1000.0 * 100.0
                                      / simulation_Waermebedarf.Waermebedarf_Gesamt;
                    if (deckungS > 100) deckungS = 100;
                    if (deckungS < 0) deckungS = 0;
                    stm.Waermebedarfsdeckung = deckungS;
                }

                // PAKET E1: Aufschlüsselung derselben Deckung auf die drei Kanäle.
                stm.Deckung_Kanal = DeckungJeKanal(
                    Summiere(st.Direktdeckung_Kanal, st.Speicherentladung_Kanal),
                    simulation_Waermebedarf.Waermebedarf_Gesamt, stm.Waermebedarfsdeckung);

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

                // V2 (PV-Konzept § 2.3, Etappe P1): In den Speicher geladene
                // PV-Energie ist KEINE Einspeisung — sie wirkt bereits als
                // vermiedener Netzbezug (Entladung senkt den Restbedarf). Die
                // Einspeisemenge ist deshalb max(0, Überschuss − Ladung) je
                // Viertelstunde; die Ladereihe (LadungAcKwh) hält die
                // SpeicherEngine genau dafür vor. Ohne Speicherlauf bleibt die
                // Formel der Bestand (Summe des Überschusses).
                if (sim.Speicherergebnis != null &&
                    sim.Speicherergebnis.LadungAcKwh != null &&
                    sim.Speicherergebnis.LadungAcKwh.Length == pvs.Ueberschuss_viertelstunde.Length)
                {
                    double[] ladungKwh = sim.Speicherergebnis.LadungAcKwh;
                    double einspKwh = 0;
                    for (int vi = 0; vi < ladungKwh.Length; vi++)
                        einspKwh += Math.Max(0,
                            pvs.Ueberschuss_viertelstunde[vi] * 0.25 - ladungKwh[vi]);
                    pvm.Ueberschuss = einspKwh / 1000.0;
                }
                else
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

            // Detail: Pufferspeicher (Konzept 6.6). Befüllt wird je Lauf der
            // Senken-Puffer (sim.puffer_wp) UND jeder Quellspeicher der WP-Module;
            // die Rolle steht in Verwendung. Quelle ist dieselbe Speicherliste, aus
            // der sich auch Navigator, CSV-Export und die Ergebnistabelle speisen.
            foreach (SimulationPufferspeicher sp in sim.AlleSpeicher())
            {
                var pz = new ErgebnisPufferspeicherModel
                {
                    ID_Pufferspeicher = sp.ID_Pufferspeicher,
                    Bezeichner = sp.Bezeichner ?? "",
                    Verwendung = sp.Verwendung ?? "",
                    Q_max = sp.Q_max,
                    Ladung_gesamt = sp.Ladung_gesamt,
                    Entladung_gesamt = sp.Entladung_gesamt,
                    Verluste_gesamt = sp.Verluste_gesamt,
                    SOC_Ende = sp.SOC,
                    SOC_Mittel = sp.SOC_Mittel,
                    SOC_Max = sp.SOC_Max,
                    Vollzyklen = sp.Vollzyklen,

                    // PAKET E1 (Schritt 52). ID_Anlage ist bei QUELLspeichern belegt und
                    // stellt die Zeile dem Serienschlüssel QUELLE_<AnlagenID> und den
                    // gleichnamigen Ganglinien-Dateien zur Seite; bei Senkenspeichern
                    // bleibt sie 0 und wird als NULL geschrieben.
                    ID_Anlage = sp.ID_Anlage,

                    // Die beiden Durchsatzsummen aus Befund N6 — bis Schritt 52 standen
                    // sie nur am Objekt („NICHT PERSISTIERT … vorgemerkte Erweiterung").
                    // Ohne Durchlass sind beide exakt 0.
                    Durchsatz_Geladen = sp.Durchsatz_Ladung_gesamt,
                    Durchsatz_Entladen = sp.Durchsatz_Entladung_gesamt,

                    // PAKET P1 (Befund E1-O5): Die beiden Temperaturspalten aus Schritt 52
                    // werden jetzt GEFÜLLT — Jahresmittel und Jahresminimum der obersten
                    // Schicht, gebildet aus derselben Stundenganglinie, die auch die
                    // Berichtsreihe PUFFER_<ID>_TOBEN trägt.
                    //
                    // Sie sind auch bei N = 1 belegt: Dort ist die eine Schichttemperatur
                    // die Ein-Zonen-Ersatztemperatur RL_eff + A/Q_max · (VL_eff − RL_eff)
                    // (Konzept 8.2) — eine reine Umrechnung des Füllstands, ohne
                    // Rückwirkung auf die Rechnung.
                    //
                    // QUELLSPEICHER bleiben NULL: Ihr Temperaturpaar ist ein Ersatzpaar
                    // aus der Anlagen-Spreizung und keine Speichertemperatur; eine
                    // Zustandsformel darauf wäre Scheinphysik (Konzept 8.2). NULL heißt in
                    // der Ergebniszeile „nicht erhoben" — genau das trifft dort zu.
                    T_oben_Mittel = sp.T_oben_Mittel,
                    T_oben_Min = sp.T_oben_Min
                };

                // Kanalaufteilung der Entladung — dieselbe Buchung wie Entladung_gesamt,
                // nur indiziert (SimulationPufferspeicher.Entladung_Kanal). KOPIERT statt
                // zugewiesen: Das Ergebnismodell darf nicht auf den Laufzustand des
                // Speicherobjekts zeigen (Aliasing-Regel B0-2).
                for (int k = 0; k < Kanal.ANZAHL; k++)
                    pz.Entladung_Kanal[k] = sp.Entladung_Kanal[k];

                m.Pufferspeicher.Add(pz);
            }

            // Detail: Stromspeicher (Fachkonzept Stromspeicher 7.1) - eine Zeile je
            // gerechneter Speichervariante. Bedingung ist derselbe Marker, der auch
            // m.Sim_Stromspeicher setzt: bSimulationSSP steht seit AP2b für einen
            // ECHTEN Engine-Lauf, und ErgebnisCtrl.Save schreibt den Block nur bei
            // gesetztem Flag - Kopf und Detail können so nicht widersprechen.
            //
            // Die Abbildung selbst liegt in StromspeicherSimCtrl (AP3b): Die
            // Ergebnisseite verwendet dieselbe Methode, damit Bildschirm und Datenbank
            // nicht auseinanderlaufen. Der Zusammenbau des ErgebnisModel bleibt wie bei
            // allen anderen Detailmodellen HIER.
            if (sim.bSimulationSSP && sim.Speicherergebnis != null)
            {
                m.Stromspeicher.Add(StromspeicherSimCtrl.AlsErgebnismodell(
                    sim.Speicherergebnis, sim.Speicherkontext));
            }

            return m;
        }

        // =====================================================================
        // PAKET E1 (Konzept 4.4) — Ergebnis je Kanal
        //
        //   Die drei Methoden hier sind die EINE Stelle, an der aus der
        //   kanalindizierten Buchführung der Engine (Paket K2) die gespeicherten
        //   Kanalgrößen werden. Sie sind bewusst public und static: Die
        //   Detailansicht zeigt dieselben Zahlen und muss dafür dieselbe Formel
        //   benutzen, nicht eine nachgebaute (Befund V0-7, „Dialog = Tab_Ergebnis").
        // =====================================================================

        /// <summary>
        /// Jahres-Wärmebedarf je Kanal [MWh] aus dem Kanalsatz des Laufs.
        ///
        /// <para>Es ist die AUFSCHLÜSSELUNG von
        /// <c>SimulationWaermebedarf.Waermebedarf_Gesamt</c>, keine zweite Rechnung:
        /// Dieselben Vektoren, aus denen seit Paket K1 der Summenvektor gebildet wird
        /// (die Kanäle sind die führende Größe), und dieselbe Umrechnung kWh → MWh.
        /// Die Summe der drei Werte ist der Gesamtbedarf bis auf die float-Rundung, mit
        /// der <c>Kanalsatz.Summe()</c> je Stunde addiert (Konzept 4.2, 1-ULP-Klasse) —
        /// bei den Größenordnungen des Rechenkerns liegt das um Zehnerpotenzen unter der
        /// kaufmännischen Rundung der Ergebniszeile.</para>
        /// </summary>
        public static double[] BedarfJeKanal(SimulationWaermebedarf bedarf)
        {
            double[] mwh = new double[Kanal.ANZAHL];
            if (bedarf == null) return mwh;

            Kanalsatz ks = bedarf.KanaeleDrei();
            for (int k = 0; k < Kanal.ANZAHL; k++)
            {
                float[] v = ks.Bedarf[k];
                double summe = 0;
                for (int h = 0; h < v.Length; h++) summe += v[h];
                mwh[k] = summe / 1000.0;
            }
            return mwh;
        }

        /// <summary>
        /// Elementweise Summe mehrerer Kanalzeilen [kWh] — der EIGENANTEIL eines
        /// Erzeugers je Kanal, zusammengesetzt aus genau den Summanden, aus denen der
        /// Runner auch seinen Skalar bildet (Direktdeckung + zugerechnete
        /// Speicherentladung, bei der Wärmepumpe zusätzlich der Heizstab).
        /// </summary>
        public static double[] Summiere(params double[][] zeilen)
        {
            double[] s = new double[Kanal.ANZAHL];
            if (zeilen == null) return s;

            foreach (double[] z in zeilen)
            {
                if (z == null) continue;
                for (int k = 0; k < Kanal.ANZAHL && k < z.Length; k++) s[k] += z[k];
            }
            return s;
        }

        /// <summary>
        /// Zerlegt die Wärmebedarfsdeckung eines Erzeugers [%] in ihre drei Kanalanteile.
        /// </summary>
        /// <param name="eigenanteilKanalKWh">Eigenanteil je Kanal [kWh] (siehe <see cref="Summiere"/>).</param>
        /// <param name="basisMWh">Wärmebedarf des PROJEKTS [MWh] — derselbe Nenner wie beim Skalar.</param>
        /// <param name="deckungGesamt">Der bereits gebildete (und geklemmte) Skalar [%].</param>
        /// <remarks>
        /// <para><b>Keine neue Verteilregel.</b> Welcher Kanal eine Deckung bekommt,
        /// entscheidet die Engine beim Abziehen vom Bedarf (<c>SenkeAbziehen</c>); hier
        /// wird nur derselbe Bruch je Kanal gebildet, mit demselben Nenner wie der
        /// Skalar.</para>
        ///
        /// <para><b>Normierung auf den führenden Skalar.</b> Die drei Rohwerte werden am
        /// Ende so skaliert, dass ihre Summe GENAU <paramref name="deckungGesamt"/>
        /// ergibt. Zwei Gründe: Der Skalar ist geklemmt (0..100), die Kanalwerte sind es
        /// nicht — ohne Normierung liefen beide im Klemmfall auseinander; und die
        /// Kanalakkumulatoren der Engine sind getrennte double-Ströme, deren Summe um
        /// eine Rundung neben dem Skalar liegen kann. Die zugesicherte Invariante
        /// „Σ Kanalwerte = Bestandsskalar" gilt damit ausnahmslos. Der Faktor liegt im
        /// Normalfall bei 1 ± 1e-12.</para>
        /// </remarks>
        public static double[] DeckungJeKanal(double[] eigenanteilKanalKWh, double basisMWh,
                                              double deckungGesamt)
        {
            double[] k = new double[Kanal.ANZAHL];
            if (eigenanteilKanalKWh == null || basisMWh <= 0) return k;

            double summe = 0;
            for (int i = 0; i < Kanal.ANZAHL && i < eigenanteilKanalKWh.Length; i++)
            {
                k[i] = eigenanteilKanalKWh[i] / 1000.0 / basisMWh * 100.0;
                summe += k[i];
            }

            if (summe > 0)
            {
                double faktor = deckungGesamt / summe;
                for (int i = 0; i < Kanal.ANZAHL; i++) k[i] *= faktor;
            }
            return k;
        }

        /// <summary>
        /// Führt die Simulation aus und schreibt das Ergebnis neu.
        /// Rückgabe: neue Ergebnis-Kopf-ID (&gt; 0) oder -1 bei Fehler (Fehlertext in 'fehler').
        ///
        /// PAKET 8 (Konzept 13.4): Auch das SPEICHERN steht im dialogfreien Modus.
        /// <c>ErgebnisCtrl.Save</c> und die von ihm gerufene Löschung des Vorgängerlaufs
        /// zeigten im Fehlerfall selbst eine MessageBox — an dieser Stelle wäre ein
        /// unbeaufsichtigter Lauf noch NACH der vollständigen Rechnung hängen geblieben.
        ///
        /// NACHARBEIT PAKET 8, BEFUND N4: Der dialogfreie Modus umschließt auch
        /// <see cref="BaueErgebnis"/>. Der beginnt mit <c>ProjektCtrl.ReadSingle</c> und
        /// liest anschließend Anlagen- und Speicherzeilen — jeder Datenbankfehler darin
        /// hätte im headless-Lauf eine MessageBox geöffnet, weil der Block bis dahin
        /// erst hinter dem Ergebnisaufbau begann.
        /// </summary>
        public int SimuliereUndSpeichere(int idProjekt, out string fehler)
        {
            if (!Simuliere(idProjekt, out fehler))
                return -1;

            int id;
            using (DataRepository.EngineModus())
            {
                ErgebnisModel m = BaueErgebnis(idProjekt, simulation_Waermebedarf, simulation_Strombedarf, sim);
                id = new ErgebnisCtrl().Save(m);

                foreach (string meldung in DataRepository.StilleFehlerAbholen())
                    Protokoll.Warnung(string.Format(MyResource.Resource.SIMENG_SPEICHERN_DES_ERGEBNISSES, meldung));
            }

            if (id <= 0)
            {
                fehler = MyResource.Resource.SIMENG_ERGEBNIS_NICHT_GESPEICHERT;
                string ausKanal = Protokoll.AlsText(true);
                if (!string.IsNullOrEmpty(ausKanal)) fehler = fehler + Environment.NewLine + ausKanal;
            }
            return id;
        }
    }
}

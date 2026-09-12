using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE der Ansicht „Stromspeicher-Auslegung" (Paket P3, Auftrag #192,
    /// Konzept „Stromspeicher-Dialoge" 2.1).
    ///
    /// <para><b>Warum es diesen Controller gibt.</b> Bis hierher standen die zwölf
    /// Delegaten der zwei Speicherdialoge in der WINDOWS-Hülle
    /// (<c>SimulationErgebnisHuelle.Flotte.cs</c> und <c>.Optimierung.cs</c>): Vorbelegung
    /// lesen, Stand und Profil schreiben, Flotte rechnen, Projektflotte aktivieren und
    /// deaktivieren, Größe übernehmen, Leistungspreis schreiben. Das ist Datenbank- und
    /// Rechenarbeit und gehört nach der Hausregel in den Kern — die Hülle behält nur, was
    /// die PLATTFORM beisteuert (Dateiwähler, <c>Task.Run</c>, Fensterbesitz).</para>
    ///
    /// <para><b>Der EINZELWEG ist mit #206 gefallen</b> (Anwenderentscheid SD‑E‑8 vom
    /// 11.09.2026): Die Ansicht kennt nur noch die Flottenrechnung, ein Einzelspeicher ist
    /// eine Flotte mit einer Einheit. Mit dem Modus fielen hier <c>EinzelVorbereiten</c>,
    /// <c>EinzelRechnen</c>, <c>Betriebsbild</c> und <c>RasterCsv</c> — und damit das
    /// gemerkte rohe Raster, an dem das Nachzeichnen des Betriebsbildes hing (W11b‑B‑25).
    /// <b>Der Optimierer selbst bleibt</b> (<see cref="SpeicherOptimierungCtrl"/>,
    /// <c>SpeicherOptimierer</c>): Er trägt das Betriebsbild des Berichts, die Vorbelegung
    /// in <c>SpeicherAuslegungCtrl</c> und die KI-Aktion „speicher_optimieren".</para>
    ///
    /// <para><b>Er ist eine INSTANZ, kein statischer Satz.</b> Anders als
    /// <see cref="SpeicherFlottenStudieCtrl"/> hält er den ZUSTAND eines Arbeitsgangs:
    /// das Projekt, den zugrunde liegenden Simulationslauf und die aktive
    /// Speichervariante.</para>
    ///
    /// <para><b>Woher der Simulationslauf kommt.</b> Jede EPOS-Zeitreihe
    /// (Last, PV, BHKW, Preise) stammt aus einem abgeschlossenen Lauf —
    /// <see cref="SpeicherAuslegungCtrl.Vorbereiten"/> verlangt ihn ausdrücklich. Zwei
    /// Wege führen dorthin: Die Ergebnisseite reicht IHREN Lauf herein
    /// (<see cref="LaufUebernehmen"/>), und wo keiner vorliegt, rechnet
    /// <see cref="Simulationslauf"/> ihn über <see cref="SimulationLaufCtrl"/> nach —
    /// derselbe Ablauf, den die Ergebnisseite fährt (Muster iU9‑W11a).</para>
    ///
    /// <para><b>Datenbank und Faden.</b> Lesen und Schreiben gehören auf den Bedienfaden;
    /// nur die reinen Rechnungen (<see cref="FlotteRechnen"/>,
    /// <see cref="PeakZielBestimmen"/>, <see cref="SimulationslaufRechnen"/>) dürfen in ein
    /// <c>Task.Run</c> der Hülle. Die Aufteilung ist dieselbe wie beim Simulationslauf.</para>
    /// </summary>
    public sealed class StromspeicherAuslegungCtrl
    {
        private readonly int _projektId;
        private readonly KonfigurationCtrl _konfig = new KonfigurationCtrl();
        private readonly ProjektCtrl _projekt = new ProjektCtrl();

        private SimulationControl _lauf;
        private StromspeicherVarianteModel _variante;

        /// <summary>Legt den Controller für ein Projekt an.</summary>
        /// <param name="projektId">Das Projekt; 0 oder kleiner ist kein Projekt.</param>
        public StromspeicherAuslegungCtrl(int projektId)
        {
            _projektId = projektId;
        }

        /// <summary>Das Projekt dieses Arbeitsgangs.</summary>
        public int ProjektId => _projektId;

        /// <summary>
        /// Der zugrunde liegende Simulationslauf; <c>null</c>, solange keiner vorliegt.
        /// </summary>
        public SimulationControl Lauf => _lauf;

        /// <summary>
        /// Liegt ein gerechneter Lauf vor? Ohne ihn stehen die EPOS-Zeitreihen nicht zur
        /// Verfügung, und jede Quelle „EPOS-Projektreihe" liefe ins Leere.
        /// </summary>
        public bool LaufVorhanden => _lauf != null && _lauf.simulation_Strombedarf != null;

        /// <summary>
        /// Die Ansicht wurde aus dem Simulationsergebnis heraus geöffnet: Sie übernimmt
        /// dessen Lauf, statt einen zweiten zu rechnen.
        /// </summary>
        /// <param name="sim">Der Lauf der Ergebnisseite; <c>null</c> setzt zurück.</param>
        public void LaufUebernehmen(SimulationControl sim)
        {
            _lauf = sim;
        }

        /// <summary>
        /// Wurde seit dem Öffnen an der Projektflotte oder am gespeicherten Stand etwas
        /// geändert? Die Ergebnisseite zieht daran ihr gespeichertes Ergebnis nach.
        /// </summary>
        public bool ProjektflotteGeaendert { get; private set; }

        // =================================================================
        //  Der Simulationslauf (Muster iU9-W11a)
        // =================================================================

        /// <summary>
        /// Bereitet einen eigenen Simulationslauf vor — Vorprüfung, Bedarfsrechnung,
        /// Bestückung. <b>Datenbank, deshalb Bedienfaden.</b>
        /// </summary>
        /// <param name="waerme">Das Wärmebedarfsobjekt des Aufrufers; es wird an Ort und Stelle gefüllt.</param>
        /// <param name="strom">Das Strombedarfsobjekt des Aufrufers.</param>
        /// <param name="neuerLauf">Der vorbereitete, noch nicht gerechnete Lauf.</param>
        /// <returns>Der Fehlertext; <c>null</c>, wenn der Lauf beginnen kann.</returns>
        public string SimulationslaufVorbereiten(SimulationWaermebedarf waerme,
                                                 SimulationStrombedarf strom,
                                                 out SimulationControl neuerLauf)
        {
            neuerLauf = null;
            if (_projektId <= 0) return MyResource.Resource.SIM_MSG_KEIN_PROJEKT;

            // Die Sperre einer unvollstaendigen Schemamigration steht NICHT hier: Sie
            // gehoert zum Programmzustand, nicht zum Projekt, und der Rechenkern
            // wiederholt sie ohnehin in Do_Simulation (SimulationControl.Sperrgrund →
            // SimulationLaufCtrl.Abbruchgrund).
            SimulationProtokoll.NeuStarten();

            _konfig.ProjektLesen(_projektId);
            if (_konfig.rows == 0) return MyResource.Resource.SIM_MSG_KONFIGURATION_FEHLT;

            _projekt.ReadSingle(_projektId);
            int idKlimaregion = _projekt.m_ID_Klimaregion;

            string fehler = SimulationLaufCtrl.Vorpruefen(_projektId, _konfig, idKlimaregion);
            if (fehler != null) return fehler;

            string bedarfsfehler = SimulationLaufCtrl.Bedarf(
                _projektId, idKlimaregion,
                _konfig.m_Netzverluste, _konfig.m_szNetzverlusteEinheit, waerme, strom);
            if (bedarfsfehler != null) return bedarfsfehler;

            var sim = new SimulationControl();
            SimulationLaufCtrl.Bestuecken(sim, _projektId, Tools(), waerme, strom, _konfig,
                _konfig.model != null ? _konfig.model.Leistungsgrenze
                                      : BhkwLeistungsgrenzeVorgabe.VORGABE_PROZENT,
                _konfig.model != null ? _konfig.model.Betriebsart : 0);
            neuerLauf = sim;
            return null;
        }

        /// <summary>
        /// Der Lauf selbst — der Teil, der in <c>Task.Run</c> gehört. Er wirft nicht:
        /// Jeder Ausgang steht in der Rückgabe.
        /// </summary>
        /// <param name="neuerLauf">Der vorbereitete Lauf aus <see cref="SimulationslaufVorbereiten"/>.</param>
        /// <param name="fortschritt">Phasenmeldung; <c>null</c> = keine.</param>
        /// <param name="token">Abbruchmarke.</param>
        /// <returns>Der Abbruchgrund; <c>null</c> bei Erfolg.</returns>
        public string SimulationslaufRechnen(SimulationControl neuerLauf,
                                             IProgress<LaufFortschritt> fortschritt,
                                             CancellationToken token)
        {
            if (neuerLauf == null) return MyResource.Resource.SIM_MSG_KEIN_VOLLSTAENDIGES_ERGEBNIS;
            try
            {
                SimulationLaufCtrl.Laufen(neuerLauf, _projektId, fortschritt, token);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { return ex.Message; }

            string abbruch = SimulationLaufCtrl.Abbruchgrund(neuerLauf);
            if (abbruch != null) return abbruch;

            LaufUebernehmen(neuerLauf);
            return null;
        }

        /// <summary>Die sechs Kaskadenplätze der Konfiguration (wörtlich wie die Ergebnishülle).</summary>
        private string[] Tools()
        {
            string[] tool = new string[6];
            if (_konfig.model == null) return tool;
            tool[0] = _konfig.model.m_Tool_1;
            tool[1] = _konfig.model.m_Tool_2;
            tool[2] = _konfig.model.m_Tool_3;
            tool[3] = _konfig.model.m_Tool_4;
            tool[4] = _konfig.model.m_Tool_5;
            tool[5] = _konfig.model.m_Tool_6;
            return tool;
        }

        // =================================================================
        //  Vorbelegung, Speichern, Profile
        // =================================================================

        /// <summary>
        /// Die höchste Bezugsleistung des gerechneten Strombedarfs [kW]; 0, solange kein
        /// Lauf vorliegt (Anwenderentscheid W11b‑E‑3).
        /// </summary>
        /// <remarks>
        /// Genommen wird der STROMBEDARF des Laufs und nicht der Netzbezug: Der Speicher
        /// soll die Spitze erst kappen, die Bezugsspitze ohne ihn ist die Bezugsgröße.
        /// </remarks>
        public double BezugsspitzeKw()
        {
            double[] werte = _lauf != null && _lauf.simulation_Strombedarf != null
                ? _lauf.simulation_Strombedarf.Strombedarf_viertelStundenwerte
                : null;
            if (werte == null) return 0.0;

            double spitze = 0.0;
            for (int i = 0; i < werte.Length; i++)
                if (werte[i] > spitze) spitze = werte[i];
            return spitze;
        }

        /// <summary>
        /// Vorbelegung des Suchraums samt aktueller Auslegung — <b>Datenbankzugriff</b>,
        /// deshalb auf dem Bedienfaden.
        /// </summary>
        public SpeicherOptimierungVorgaben Vorgaben()
        {
            // L_P kann im Parameterreiter unmittelbar geschrieben worden sein; vor der
            // Vorbelegung wird der aktive Variantensatz deshalb frisch gelesen.
            VarianteLesen();
            SpeicherOptimierungVorgaben vorgaben =
                SpeicherFlottenStudieCtrl.Vorbelegung(_projektId, BezugsspitzeKw(), _lauf);
            if (_variante != null)
                vorgaben.Eingaben.LeistungspreisEurProKwA = _variante.L_P;
            return vorgaben;
        }

        // =================================================================
        //  „Speicher hinzufügen" — die drei Quellen (Auftrag #239)
        // =================================================================

        /// <summary>
        /// Die Speicheranlagen DIESES Projekts als Kandidaten für die Flotte
        /// (Anwenderrückmeldung 12.09.2026). <b>Datenbankzugriff</b>, deshalb auf dem
        /// Bedienfaden.
        /// </summary>
        /// <remarks>
        /// Durchreiche auf <see cref="SpeicherFlottenStudieCtrl.Projektanlagenkandidaten"/>
        /// — die Ansicht kennt ihre Projekt-Id nicht und soll sie nicht kennen müssen.
        /// </remarks>
        public IReadOnlyList<SpeicherFlottenStudieCtrl.FlottenAnlagenkandidat> Projektanlagen()
            => SpeicherFlottenStudieCtrl.Projektanlagenkandidaten(_projektId);

        /// <summary>
        /// Eine Flotteneinheit aus EINER Speicheranlage dieses Projekts — derselbe Weg wie
        /// die Vorbelegung. <b>Datenbankzugriff.</b>
        /// </summary>
        /// <param name="anlageId">
        /// <c>Tab_Energieanlagen.ID</c> als Text, so wie
        /// <c>FlottenEinheit.AnlageId</c> und
        /// <see cref="SpeicherFlottenStudieCtrl.FlottenAnlagenkandidat.AnlageId"/> sie
        /// führen.
        /// </param>
        /// <returns>Die Einheit; <c>null</c> bei unbekannter oder unbrauchbarer Anlage.</returns>
        public FlottenEinheit EinheitAusProjektanlage(string anlageId)
        {
            if (!int.TryParse(anlageId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                return null;
            return SpeicherFlottenStudieCtrl.EinheitAusProjektanlage(_projektId, id);
        }

        /// <summary>Speichert den bearbeiteten Stand als projektgebundene Vorbelegung <c>@Aktuell</c>.</summary>
        /// <param name="eingaben">Der Arbeitsstand.</param>
        /// <returns>Leer bei Erfolg, sonst der Fehlertext.</returns>
        public string EinstellungenSpeichern(SpeicherOptimierungEingaben eingaben)
        {
            if (eingaben == null) return MyResource.Resource.FLOTTE_DLG_MSG_KEINE_ABLAGE;
            try
            {
                SpeicherOptimierungEingaben stand = eingaben.Kopie();
                stand.Auslegung.FlottenProjektbetriebDeaktiviert = false;
                SpeicherAuslegungCtrl.Speichern(_projektId, SpeicherAuslegungCtrl.Anlage(_projektId),
                    SpeicherAuslegungCtrl.AktuellerStand, stand);
                ProjektflotteGeaendert = true;
                return "";
            }
            catch (Exception ex) { return ex.Message; }
        }

        /// <summary>
        /// Speichert ein BENANNTES Profil und liest die Profilliste danach neu.
        /// </summary>
        /// <param name="eingaben">Der Arbeitsstand.</param>
        /// <param name="name">Der Profilname; ein führendes <c>@</c> ist für interne Stände reserviert.</param>
        /// <returns>Die aktualisierten Vorgaben.</returns>
        /// <exception cref="ArgumentException">Der Name beginnt mit <c>@</c>.</exception>
        public SpeicherOptimierungVorgaben ProfilSpeichern(SpeicherOptimierungEingaben eingaben, string name)
        {
            name = (name ?? "").Trim();
            if (name.StartsWith("@", StringComparison.Ordinal))
                throw new ArgumentException(MyResource.Resource.SPAUS_MSG_PROFILNAME_RESERVIERT);

            SpeicherAuslegungCtrl.Speichern(_projektId, SpeicherAuslegungCtrl.Anlage(_projektId),
                name, eingaben.Kopie());
            return Vorgaben();
        }

        // =================================================================
        //  Die Flottenstudie
        // =================================================================

        /// <summary>
        /// Beschafft Quellen und Kosten EINES Flottenlaufs. <b>Datenbank, Bedienfaden.</b>
        /// </summary>
        /// <param name="eingaben">Der Arbeitsstand.</param>
        /// <param name="meldung">Der Grund, warum nichts vorbereitet werden konnte.</param>
        /// <returns>Die Vorbereitung; <c>null</c>, wenn <paramref name="meldung"/> gesetzt ist.</returns>
        public StromspeicherOptimierungVorbereitung FlotteVorbereiten(
            SpeicherOptimierungEingaben eingaben, out string meldung)
        {
            meldung = "";
            try
            {
                StromspeicherOptimierungVorbereitung v =
                    SpeicherAuslegungCtrl.Vorbereiten(_lauf, _projektId, eingaben.Kopie());
                if (v == null)
                {
                    meldung = MyResource.Resource.SIMENG_SPEICHER_KEIN_SPEICHER;
                    return null;
                }
                meldung = EinstellungenSpeichern(v.Eingaben);
                if (!string.IsNullOrEmpty(meldung)) return null;
                return v;
            }
            catch (Exception ex)
            {
                meldung = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Rechnet die Flottenstudie. <b>Reine Rechnung</b> — sie gehört in
        /// <c>Task.Run</c> und wirft nur bei Abbruch.
        /// </summary>
        /// <param name="vorbereitung">Die Vorbereitung aus <see cref="FlotteVorbereiten"/>.</param>
        /// <param name="fortschritt">Kandidatenmeldung; <c>null</c> = keine.</param>
        /// <param name="token">Abbruchmarke.</param>
        public SpeicherFlottenErgebnis FlotteRechnen(StromspeicherOptimierungVorbereitung vorbereitung,
                                                     IProgress<FlottenFortschritt> fortschritt,
                                                     CancellationToken token)
        {
            if (vorbereitung == null)
                return new SpeicherFlottenErgebnis { Meldung = MyResource.Resource.SIMENG_SPEICHER_KEIN_SPEICHER };
            try
            {
                return SpeicherFlottenStudieCtrl.Rechnen(vorbereitung, Planer(), fortschritt, token);
            }
            catch (OperationCanceledException)
            {
                return new SpeicherFlottenErgebnis
                { Abgebrochen = true, Meldung = MyResource.Resource.FLOTTE_DLG_MSG_ABGEBROCHEN };
            }
            catch (Exception ex)
            {
                return new SpeicherFlottenErgebnis { Meldung = ex.Message };
            }
        }

        /// <summary>
        /// Der Fahrplan-Löser, sofern die Plattform einen registriert hat
        /// (<see cref="SpeicherFlottenProjektCtrl.PlanerFactory"/>); <c>null</c> sonst.
        /// </summary>
        /// <remarks>
        /// Der Kern kennt <c>SpeicherPlanung</c> NICHT (Hausregel: OR-Tools hängt nur an
        /// der Windows-Anwendung). Die zwei reaktiven Betriebsziele rechnen ohne Planer;
        /// für die drei planenden sperrt die Oberfläche den Lauf vorher
        /// (<see cref="FlottenPlanerLage"/>).
        /// </remarks>
        private static IFlottenPlaner Planer()
        {
            Func<IFlottenPlaner> fabrik = SpeicherFlottenProjektCtrl.PlanerFactory;
            if (fabrik == null) return null;
            try { return fabrik(); } catch (Exception) { return null; }
        }

        // =================================================================
        //  Vorpruefung, Peak-Ziel (P1: SD-Q3, SD-Q4, SD-Q5)
        // =================================================================

        /// <summary>
        /// Die VORPRÜFUNG vor dem Lauf (Konzept 2.4 Punkt 3) — ohne Diagnose, weil noch
        /// nichts gerechnet ist. <b>Datenbank, Bedienfaden.</b>
        /// </summary>
        /// <remarks>
        /// Sie ist die zweite Hälfte dessen, was der Studienlauf selbst tut
        /// (<c>SpeicherFlottenStudieCtrl.Rechnen</c> prüft erneut); hier steht sie, damit
        /// die Ansicht den Anwender warnen kann, BEVOR er einen Jahreslauf startet. Ein
        /// Fehlschlag der Vorbereitung ist KEIN Prüfbefund — dann gibt es schlicht nichts
        /// zu prüfen, und der Lauf meldet den Grund selbst.
        /// </remarks>
        /// <param name="eingaben">Der Arbeitsstand.</param>
        /// <returns>Die Hinweise; nie <c>null</c>.</returns>
        public List<FlottenHinweis> Vorpruefen(SpeicherOptimierungEingaben eingaben)
        {
            if (eingaben?.Auslegung?.Flotte == null) return new List<FlottenHinweis>();
            try
            {
                StromspeicherOptimierungVorbereitung v =
                    SpeicherAuslegungCtrl.Vorbereiten(_lauf, _projektId, eingaben.Kopie());
                if (v == null) return new List<FlottenHinweis>();
                return FlottenPlausibilitaet.Pruefe(SpeicherFlottenStudieCtrl.Eingang(v),
                    SpeicherFlottenStudieCtrl.Konfiguration(v.Eingaben),
                    v.Eingaben.Auslegung?.VerwendeteKosten);
            }
            catch (Exception) { return new List<FlottenHinweis>(); }
        }

        /// <summary>
        /// Der hergeleitete Vorschlag für das Peak-Ziel H₀ (Konzept 2.4 Punkt 1,
        /// Anwenderentscheid SD‑Q3). <b>Datenbank, Bedienfaden.</b>
        /// </summary>
        /// <remarks>
        /// Ohne beschaffbare Zeitreihe kommt der benannte RÜCKFALL aus der Bezugsspitze —
        /// <see cref="FlottenPeakZielVorschlag.AusReihe"/> sagt, welcher der beiden Fälle
        /// vorliegt, und die Ansicht zeigt die Herleitungszeile nur im ersten.
        /// </remarks>
        /// <param name="eingaben">Der Arbeitsstand.</param>
        public FlottenPeakZielVorschlag PeakZielVorschlag(SpeicherOptimierungEingaben eingaben)
        {
            FlottenStudieKonfiguration flotte = eingaben?.Auslegung?.Flotte;
            try
            {
                if (flotte != null && flotte.Einheiten.Count > 0)
                {
                    StromspeicherOptimierungVorbereitung v =
                        SpeicherAuslegungCtrl.Vorbereiten(_lauf, _projektId, eingaben.Kopie());
                    if (v != null)
                    {
                        FlottenEingang eingang = SpeicherFlottenStudieCtrl.Eingang(v);
                        if (eingang?.Istwerte != null && eingang.Istwerte.Count > 0)
                            return FlottenPeakZiel.Vorschlag(eingang.Istwerte,
                                SpeicherFlottenStudieCtrl.Konfiguration(v.Eingaben));
                    }
                }
            }
            catch (Exception) { /* ohne Reihe bleibt der Rueckfall */ }

            double entladeleistung = 0.0;
            if (flotte != null)
                foreach (FlottenEinheit e in flotte.Einheiten) entladeleistung += e.EntladeleistungKw;
            return FlottenPeakZiel.Rueckfall(BezugsspitzeKw(), entladeleistung);
        }

        /// <summary>
        /// Bestimmt das kleinste haltbare Peak-Ziel per Bisektion (Konzept 2.4 Punkt 2).
        /// <b>Bis zu zwölf Jahresläufe</b> — die Vorbereitung liest die Datenbank und
        /// bleibt auf dem Bedienfaden, die Suche gehört in <c>Task.Run</c>.
        /// </summary>
        /// <param name="vorbereitung">Die Vorbereitung aus <see cref="FlotteVorbereiten"/>.</param>
        /// <param name="fortschritt">Meldung je Jahreslauf; <c>null</c> = keine.</param>
        /// <param name="token">Abbruchmarke.</param>
        /// <exception cref="InvalidOperationException">
        /// Das Betriebsziel ist planend, die Flotte ist leer oder es fehlt die Zeitreihe.
        /// </exception>
        public FlottenPeakZielErgebnis PeakZielBestimmen(StromspeicherOptimierungVorbereitung vorbereitung,
                                                        IProgress<FlottenPeakZielFortschritt> fortschritt,
                                                        CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(vorbereitung);
            return FlottenPeakZiel.PeakZielBestimmen(
                SpeicherFlottenStudieCtrl.Eingang(vorbereitung),
                SpeicherFlottenStudieCtrl.Konfiguration(vorbereitung.Eingaben),
                Planer(), fortschritt, token);
        }

        // =================================================================
        //  Die Projektflotte
        // =================================================================

        /// <summary>Ist die Flotte dieses Projekts für den Projektlauf aktiviert?</summary>
        public bool ProjektflotteAktiv() => SpeicherFlottenProjektCtrl.IstAktiv(_projektId);

        /// <summary>
        /// Aktiviert den gerechneten Flottenstand für die Projektsimulation
        /// (<c>@Projektflotte</c>) und schreibt ihn zugleich als Arbeitsstand fort.
        /// </summary>
        /// <param name="ergebnis">Das Laufergebnis.</param>
        /// <returns>Leer bei Erfolg, sonst der Fehlertext.</returns>
        public string ProjektflotteAktivieren(SpeicherFlottenErgebnis ergebnis)
        {
            if (ergebnis == null) return MyResource.Resource.FLOTTE_DLG_KEIN_VERGLEICH;
            try
            {
                SpeicherFlottenProjektCtrl.Aktivieren(_projektId, ergebnis);
                SpeicherOptimierungEingaben aktuell = ergebnis.Eingaben.Kopie();
                aktuell.Auslegung.Flotte = SpeicherAuslegungKopie.Von(ergebnis.Konfiguration);
                aktuell.Auslegung.FlotteImProjektAktiv = false;
                aktuell.Auslegung.FlottenProjektbetriebDeaktiviert = false;
                SpeicherAuslegungCtrl.Speichern(_projektId, SpeicherAuslegungCtrl.Anlage(_projektId),
                    SpeicherAuslegungCtrl.AktuellerStand, aktuell);
                ProjektflotteGeaendert = true;
                return "";
            }
            catch (Exception ex) { return ex.Message; }
        }

        /// <summary>Nimmt die Flotte aus dem Projektlauf heraus.</summary>
        /// <returns>Leer bei Erfolg, sonst der Fehlertext.</returns>
        public string ProjektflotteDeaktivieren()
        {
            try
            {
                SpeicherOptimierungEingaben aktuell = Vorgaben().Eingaben.Kopie();
                aktuell.Auslegung.FlottenProjektbetriebDeaktiviert = true;
                SpeicherFlottenProjektCtrl.Deaktivieren(_projektId);
                SpeicherAuslegungCtrl.Speichern(_projektId, SpeicherAuslegungCtrl.Anlage(_projektId),
                    SpeicherAuslegungCtrl.AktuellerStand, aktuell);
                ProjektflotteGeaendert = true;
                return "";
            }
            catch (Exception ex) { return ex.Message; }
        }

        // =================================================================
        //  Die Groesse einer Einheit zurueck in die Projektanlage
        // =================================================================

        /// <summary>
        /// Schreibt Kapazität und Leistung EINER Speichereinheit in die Gerätedaten.
        /// </summary>
        /// <remarks>
        /// <para><b>Kein automatisches Nachrechnen</b> — wörtlich wie im Vorläufer: Die
        /// Simulation ist danach nicht mehr aktuell, und der Anwender entscheidet selbst,
        /// wann er den Lauf wiederholt.</para>
        /// <para><b>Er bleibt mit SD‑E‑8</b> (11.09.2026): Die Ansicht rechnet seither
        /// immer die Flotte, aber der PROJEKTLAUF führt weiter zwei Pfade (SD‑Q2) — ohne
        /// aktivierte Projektflotte rechnet er die Einzelanlage, und die ausgelegte Größe
        /// käme dort ohne diesen Weg nie an. Aufgerufen wird er aus Schritt 5 der Ansicht,
        /// für die eine Einheit mit Anlagenbezug.</para>
        /// </remarks>
        /// <param name="cNomKwh">Nennkapazität [kWh].</param>
        /// <param name="pKw">Leistung [kW].</param>
        /// <returns>Der Meldungstext und ob es geklappt hat.</returns>
        public (bool Erfolg, string Text) AuslegungUebernehmen(double cNomKwh, double pKw)
        {
            var ctrl = new StromspeicherSimCtrl();
            bool ok;
            try { ok = ctrl.UebernehmeAuslegung(_projektId, cNomKwh, pKw); }
            catch (Exception ex)
            { return (false, string.Format(MyResource.Resource.OPT_MSG_FEHLER, ex.Message)); }

            if (!ok)
                return (false, string.IsNullOrEmpty(ctrl.LetzterHinweis)
                    ? MyResource.Resource.OPT_MSG_UEBERNAHME_FEHLER
                    : ctrl.LetzterHinweis);

            return (true, MyResource.Resource.OPT_MSG_UEBERNOMMEN);
        }

        // =================================================================
        //  Der Leistungspreis (W11b-E-3)
        // =================================================================

        /// <summary>
        /// Schreibt den Leistungspreis L_P [€/(kW·a)] SOFORT in die aktive
        /// Speichervariante (Anwenderentscheid W11b‑E‑3).
        /// </summary>
        /// <remarks>
        /// Es ist dasselbe Feld <c>Tab_StromspeicherVariante.L_P</c>, das der Reiter
        /// „Parameter" und die Peak-Shaving-Maske pflegen — EINE Pflegestelle.
        /// <see cref="VarianteLesen"/> steht davor, weil die Ansicht geöffnet werden kann,
        /// ohne dass der Reiter „Stromspeicher" je gezeichnet wurde.
        /// </remarks>
        /// <param name="leistungspreisEurProKwA">Der Satz [€/(kW·a)].</param>
        /// <returns>Der Meldungstext und ob geschrieben wurde.</returns>
        public (bool Erfolg, string Text) LeistungspreisSchreiben(double leistungspreisEurProKwA)
        {
            VarianteLesen();
            if (_variante == null)
                return (false, MyResource.Resource.SP_PARAM_MSG_KEINE_VARIANTE);

            string fehler = SpeicherParameterPruefung.NichtNegativ(leistungspreisEurProKwA);
            if (fehler != null) return (false, fehler);

            try
            {
                _variante.L_P = leistungspreisEurProKwA;
                if (!new StromspeicherVarianteCtrl().Update(_variante))
                    return (false, MyResource.Resource.SP_PARAM_MSG_FEHLER);
            }
            catch (Exception)
            {
                return (false, MyResource.Resource.SP_PARAM_MSG_FEHLER);
            }

            return (true, MyResource.Resource.SP_PARAM_MSG_GESPEICHERT);
        }

        /// <summary>Liest die aktive Speichervariante des Projekts nach.</summary>
        private void VarianteLesen()
        {
            if (_projektId <= 0) { _variante = null; return; }
            try { _variante = new StromspeicherVarianteCtrl().AktiveVarianteSicherstellen(_projektId); }
            catch (Exception) { _variante = null; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;

namespace WindowsFormsApplication1
{
    public  class SimulationControl
    {
        // Simulationen Deklaration
        public SimulationWaermepumpe simulation_wp = new SimulationWaermepumpe();
        public SimulationSPK simulation_spk = new SimulationSPK();
        public SimulationSolarthermie simulation_solarthermie = new SimulationSolarthermie();
        public SimulationPV simulation_pv = new SimulationPV();
        public SimulationBHKW simulation_bhkw = new SimulationBHKW();

        private bool m_bError = false;

        // Eingangsparameter
        public SimulationWaermebedarf simulation_Waermebedarf;
        public SimulationStrombedarf simulation_Strombedarf;

        // ===================================================================
        // Speicher-Registry (Paket 4, Etappe 4a - Konzept 6.2)
        // ===================================================================

        /// <summary>
        /// ALLE im Lauf referenzierten Projekt-Pufferspeicher, Schlüssel ist
        /// <c>Tab_Pufferspeicher.ID</c> — je Speicher GENAU EIN Objekt (Konzept 6.2).
        ///
        /// Aufgebaut wird sie zu Beginn jedes Laufs aus
        ///   - der Alt-Zuordnung <c>Z_ProjektPufferSp</c> (der Senkenspeicher der
        ///     Wärmepumpe zuerst — er ist der Alias <see cref="puffer_wp"/>, Konzept 6.7),
        ///   - den Senken-Fremdschlüsseln der Projektanlagen (<c>WS_ID_Puffer</c>,
        ///     <c>WS_ID_Puffer2</c>),
        ///   - und den QUELLspeichern der WP-Module, die
        ///     <see cref="WaermequelleClass.Quellspeicher"/> beim Modulaufbau hier
        ///     einträgt (<c>WQ_ID_Puffer</c>).
        ///
        /// Damit löst die Registry die bisher parallele, modulweise Liste
        /// <c>wp_quellspeicher</c> ab (Konzept 6.2, Zusatz der Fassung 12): Die Instanzen
        /// dort und hier sind dieselben, es gibt keine zweite Speicherverwaltung mit
        /// eigener Bilanz mehr.
        ///
        /// IM EINKANALIGEN ALTPFAD wertet außer dem Alias <see cref="puffer_wp"/> kein
        /// Rechenpfad die Registry aus. Im zweikanaligen Weg ist sie die Menge, über die
        /// die aus der Kaskade gelöste Ladephase (6.3) und Phase G laufen — dann aber
        /// eingeschränkt auf die Einträge mit
        /// <see cref="SimulationPufferspeicher.ImRechenpfad"/>
        /// (siehe <see cref="RegistryFuerZweikanaligOeffnen"/>).
        /// </summary>
        public Dictionary<int, SimulationPufferspeicher> speicherRegistry =
            new Dictionary<int, SimulationPufferspeicher>();

        /// <summary>
        /// Aufnahmereihenfolge der Registry — <c>Dictionary</c> sichert keine
        /// Reihenfolge zu, <see cref="ErsterHeizpuffer"/> braucht aber genau die. Der
        /// Senkenspeicher der Wärmepumpe steht deshalb immer an erster Stelle.
        /// </summary>
        private readonly List<int> _speicherReihenfolge = new List<int>();

        /// <summary>
        /// Rückfallebene für den einen Fall, in dem der Registry-Schlüssel fehlt: eine
        /// Zuordnung auf eine Speicherzeile ohne gültige ID. Dann steht der Speicher
        /// nicht in der Registry, muss aber weiter der Alias sein — sonst rechnete das
        /// Projekt still ohne Puffer.
        /// </summary>
        private SimulationPufferspeicher _pufferOhneRegistrySchluessel = null;

        /// <summary>
        /// Speicher, die im Lauf rechnen, aber keinen freien Registry-Schlüssel bekommen
        /// haben — praktisch nur der Kurzschlussfall „derselbe Puffer als QUELLE und als
        /// SENKE" (siehe <see cref="QuellspeicherUebernehmen"/>).
        ///
        /// Sie gehören trotzdem in Phase G und in die Ergebnispersistenz: Ein Speicher,
        /// aus dem ein Modul entnimmt, muss seine Stunde abschließen (Bereitschafts-
        /// verluste, SOC-Ganglinie) und in der Bilanz auftauchen. Ohne diese Liste fiele
        /// er still heraus — die Registry ist ein <c>Dictionary</c> und kann einen
        /// Schlüssel nur einmal vergeben.
        /// </summary>
        private readonly List<SimulationPufferspeicher> _zusatzSpeicher =
            new List<SimulationPufferspeicher>();

        /// <summary>
        /// Die Zuordnungszeilen <c>Z_ProjektPufferSp</c> des Laufs, gelesen von
        /// <see cref="SpeicherRegistryAufbauen"/> und dort aufbewahrt.
        ///
        /// NACHARBEIT PAKET 6, BEFUND N2: Der Ersatz-Pendelspeicher entsteht erst nach
        /// dem Kontextaufbau und braucht dieselbe TEMPERATUR-VORRANGKETTE wie jeder
        /// andere Registry-Speicher (Projektkopie → Zuordnungszeile → Rückfall). Ohne
        /// diese Zwischenablage müsste er die Zeilen ein zweites Mal aus der Datenbank
        /// lesen — dieselbe Abfrage, doppelt, im Engine-Pfad.
        /// </summary>
        private Z_ProjektPufferSpCtrl _pspZuordnungen = null;

        /// <summary>
        /// Senkenzuordnungen aller Wärmeerzeuger des Projekts (Konzept 6.1), je Lauf
        /// neu geladen. Ausgewertet werden sie im zweikanaligen Weg
        /// (<c>Kaskadenkontext.SenkeJeModul</c>); der einkanalige Altpfad liest sie nicht.
        /// </summary>
        public List<Senkenzuordnung> senkenzuordnungen = new List<Senkenzuordnung>();

        /// <summary>
        /// EFFEKTIVER Rechenweg des Laufs: <c>true</c> = Speicherstufen-Mechanik
        /// (<see cref="Kaskade_Zweikanalig"/>) auf den beiden Bedarfskanälen mit
        /// herausgelöster Ladephase (Reihenfolge-Invariante 6.3), <c>false</c> = der
        /// einkanalige Altpfad.
        ///
        /// ZWEI QUELLEN speisen das Feld (siehe die Zuweisung in <c>Do_Simulation</c>):
        ///
        ///   1. das Feature-Flag <c>Tab_Einstellungen.Kaskade_Zweikanalig</c> des
        ///      Projekts (Konzept Kapitel 9) — der Schalter für Projekte OHNE BHKW,
        ///   2. seit PAKET BHKW-REGULÄR: die bloße ANWESENHEIT eines BHKW in
        ///      <c>Tool_1..4</c>. BHKW-Projekte rechnen ohne Rücksicht auf das Flag über
        ///      die Speicherstufe (Entscheidung des Anwenders 17.08.2026, revidiert 6-1).
        ///
        /// WARUM DAS FELD SELBST GESETZT WIRD und nicht nur die Weiche: Der Rechenweg ist
        /// nicht allein die Verzweigung in <c>Do_Simulation</c>. <see cref="AlleSpeicher"/>,
        /// der Registry-Aufbau und der <c>SimulationRunner</c> (Restbedarf und
        /// Deckungsgrade von Wärmepumpe und BHKW) lesen dasselbe Feld und müssen dieselbe
        /// Antwort bekommen — sonst rechnete ein BHKW-Projekt zweikanalig, während seine
        /// Ergebnisbildung noch die Altpfad-Formeln nähme.
        ///
        /// Der Schalter ändert Ergebnisse — bei Projekten mit Puffer-Senke deutlich. Was
        /// sich ändert und warum, steht im Umsetzungsprotokoll zu Paket 4, Teil 7.
        /// </summary>
        public bool KaskadeZweikanalig = false;

        /// <summary>
        /// <c>true</c>, wenn NICHT das Feature-Flag, sondern das BHKW in der Kaskade den
        /// Speicherstufen-Weg erzwungen hat (Paket BHKW-Regulär). Trägt allein den
        /// Protokollhinweis: Der Anwender soll den Grund des Rechenwegs im Lauf-Protokoll
        /// wiederfinden, auch wenn er den Schalter nie berührt hat.
        /// </summary>
        private bool _bhkwErzwingtSpeicherstufe = false;

        /// <summary>
        /// <c>true</c>, wenn ein PARALLELVERBUND im Projekt den Speicherstufen-Weg
        /// erzwungen hat (Paket Parallelverbund, Entscheidung des Anwenders 17.08.2026) —
        /// genau die Bauform von <see cref="_bhkwErzwingtSpeicherstufe"/> und aus demselben
        /// Grund: Der Anwender soll den Grund des Rechenwegs im Lauf-Protokoll
        /// wiederfinden, auch wenn er den Schalter nie berührt hat.
        ///
        /// WARUM DER VERBUND DEN WEG ERZWINGT. Der einkanalige Altpfad holt seinen einen
        /// Speicher aus der Alt-Zuordnung <c>Z_ProjektPufferSp</c> und kennt weder
        /// Ladeaufträge noch die Speicher-Registry als Rechenmenge. Ein Verbund ist aber
        /// genau eine Aussage über den LADEWEG („diese Anlage lädt einen gemeinsamen
        /// Vorrat aus mehreren Behältern"); ohne die Speicherstufe würde die aufsummierte
        /// Kapazität gespeichert, angezeigt — und nicht gerechnet. Das ist derselbe stille
        /// Wirkungsverlust, den Paket BHKW-Regulär für das BHKW beseitigt hat.
        /// </summary>
        private bool _verbundErzwingtSpeicherstufe = false;

        /// <summary>
        /// Die Verbünde des Projekts als <c>Leitspeicher-ID -&gt; zusätzliche Mitglieder</c>,
        /// EINMAL je Lauf gelesen (Paket Parallelverbund).
        ///
        /// Warum als Feld und nicht je Speicher nachgeschlagen: Der Registry-Aufbau läuft
        /// über zwei Blöcke und mehrere Rückfallwege; eine Abfrage je Kandidat wäre ein
        /// N+1 im Rechenpfad. Leeres Verzeichnis = kein Verbund = Bestandsverhalten.
        /// </summary>
        private Dictionary<int, List<int>> _verbuende = new Dictionary<int, List<int>>();

        /// <summary>
        /// Senkenspeicher der Wärmepumpe — Alias auf den ersten Heizungs-Puffer der
        /// Registry (Konzept 6.7), <c>null</c> wenn keiner zugeordnet ist.
        ///
        /// Bleibt als Eigenschaft bestehen, damit <c>NavigatorWaerme</c>,
        /// <c>Form_Simulation_Detail</c> (Anzeige und CSV-Export), der
        /// <c>ZeitreihenExtraktor</c> des Berichts und <c>SimulationRunner</c>
        /// unverändert weiterarbeiten. Sobald ein Projekt zwei Puffer hat, zeigen diese
        /// Stellen nur einen — das ist in Paket 7 aufzulösen.
        /// </summary>
        public SimulationPufferspeicher puffer_wp
        {
            get { return ErsterHeizpuffer(); }
        }

        public KonfigurationCtrl ctrl_konfig;
        public int m_ID_Projekt;
        public string[] tool;
        public float[] Stundentemperatur = new float[8760];
        public int modeBHKW;
        public int GrenzleistungBHKW;

        /// <summary>
        /// Volumen des BHKW-Pendelspeichers in LITERN (Etappe 3, 14.08.2026).
        ///
        /// Bis dahin stand hier der Alt-Parameter Tab_Einstellungen.Pendelspeicher in m³.
        /// Quelle ist jetzt ausschließlich der Projekt-Puffer "BHKW-Pendelspeicher"
        /// (PufferSpCtrl.PendelspeicherVolumenLiter); die Migration hat den Alt-Wert
        /// dorthin als m³ × 1000 übernommen.
        /// </summary>
        public int VolumenPendelspeicherBHKW;


        // Rückgabe
        public float Restwaerme;
        public float Reststrom;
        public float[] Rest_Waermebedarf_stuendlich = new float[8760];
        public float[] Rest_Strombedarf_viertelstuendlich = new float[8760 * 4];

        public bool bSimulationWP = false;
        public bool bSimulationKessel = false;
        public bool bSimulationSolarthermie = false;
        public bool bSimulationPV = false;

        /// <summary>
        /// true, wenn der Stromspeicher in diesem Lauf WIRKLICH gerechnet hat (AP2b).
        ///
        /// Bis AP2a bedeutete das Flag nur „Tool 6 war aktiv": Es wurde auch dann
        /// gesetzt, wenn der wirkungslose <c>SimulationSSP</c>-Stub Nullen lieferte.
        /// Seit dem Engine-Einbau setzt es ausschließlich ein erfolgreicher Lauf der
        /// <c>SpeicherEngine</c>; ein Projekt ohne Speicher, ohne Kapazität oder mit
        /// abgebrochener Rechnung lässt es auf false. Über
        /// <c>SimulationRunner.BaueErgebnis</c> geht es als
        /// <c>Tab_Ergebnis.Sim_Stromspeicher</c> in die Persistenz — der Lauf
        /// „behauptet" also keine Speicherrechnung mehr, die nicht stattgefunden hat.
        /// </summary>
        public bool bSimulationSSP = false;
        public bool bSimulationBHKW = false;

        /// <summary>
        /// Ergebnis des Stromspeicher-Laufs (AP2b) oder <c>null</c>, wenn nicht
        /// gerechnet wurde — die einzige Quelle für SoC-Ganglinie und
        /// Speicherkennzahlen. Belegt genau dann, wenn <see cref="bSimulationSSP"/>
        /// gesetzt ist.
        /// </summary>
        public SpeicherEngine.SpeicherErgebnis Speicherergebnis = null;

        /// <summary>
        /// Parametersatz, Variante und Anlagenbezug des Speicherlaufs (AP3b) —
        /// belegt zusammen mit <see cref="Speicherergebnis"/>.
        /// </summary>
        /// <remarks>
        /// Ergebnisseite und Ergebnispersistenz brauchen Größen, die das reine
        /// Engine-Ergebnis nicht führt (SoC-Band, Nutzungsdauer, N_zyk, Anlagenzeile).
        /// Sie hängen deshalb am Lauf statt aus der Datenbank nachgelesen zu werden —
        /// sonst gäbe es zwei Parametersätze für dieselbe Rechnung.
        /// </remarks>
        public StromspeicherLaufKontext Speicherkontext = null;

        /// <summary>
        /// Ladezustandsganglinie des Stromspeichers [kWh] im Viertelstundenraster
        /// (35.040) — Nullvektor, solange kein Speicher gerechnet hat.
        /// </summary>
        /// <remarks>
        /// Löst <c>SimulationPV.Speicherfuellstand_viertelstunde</c> ab (Fachkonzept
        /// 8.2, Rudiment 2). Die Reihe kommt <b>nativ</b> viertelstündlich aus der
        /// Engine; die frühere Interpolation stündlicher Werte
        /// (<c>Stundenwerte_zu_viertelstunden_Interpoliert</c>) entfällt damit — sie
        /// hatte nur die Treppenstufen der Stundenrechnung geglättet.
        /// </remarks>
        public float[] Speicherfuellstand_viertelstuendlich = new float[8760 * 4];

        /// <summary>
        /// Ladezustandsganglinie des Stromspeichers [kWh] stündlich (8.760), Mittel
        /// der vier Viertelstunden — für Berichte und Exporte im Stundenraster.
        /// </summary>
        public float[] Speicherfuellstand_stuendlich = new float[8760];

        /// <summary>
        /// Grund, aus dem der letzte Simulationsversuch gar nicht erst angelaufen ist
        /// (leer, wenn gerechnet wurde). Gefüllt von der Migrationsblockade, damit
        /// aufrufende Formulare den Anwender informieren können, statt Nullwerte
        /// anzuzeigen.
        /// </summary>
        public string Sperrgrund = "";

        /// <summary>
        /// Fehlertext eines Erzeugermoduls des ZWEIKANALIGEN Wegs (leer, wenn alles
        /// gerechnet hat). Konzept 13.4 verlangt eine dialogfreie Engine; der
        /// zweikanalige Weg meldet deshalb hierüber statt über eine MessageBox, und
        /// <c>SimulationRunner</c> reicht den Text an den Aufrufer weiter
        /// (Paket-5-Nacharbeit, Befund N10). Seit Paket 8 meldet auch der einkanalige
        /// Altpfad hierüber statt per MessageBox (Konzept 13.4).
        /// </summary>
        public string Fehlertext = "";

        /// <summary>
        /// Nimmt den Abbruchgrund eines Moduls auf — SAMMELND statt überschreibend
        /// (Nacharbeit Paket 8, Befund N11).
        ///
        /// Ein Lauf kann an mehreren Stellen scheitern: Wärmepumpe und Heizkessel melden
        /// aus verschiedenen Zweigen der Kaskade in dasselbe Feld. Bis zur Nacharbeit
        /// überschrieb der spätere Melder den früheren an zwei von drei Stellen — der
        /// Anwender sah dann den zweiten Grund und nicht den ersten. Doppelungen werden
        /// weggelassen: Dasselbe Modul meldet in beiden Rechenwegen denselben Text.
        ///
        /// ERGEBNISNEUTRAL: <see cref="Fehlertext"/> ist reiner Meldetext. Ob er belegt
        /// ist, entscheidet über den Abbruch des Speicherns — und belegt ist er in genau
        /// denselben Fällen wie zuvor.
        /// </summary>
        private void FehlertextAufnehmen(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (string.IsNullOrEmpty(Fehlertext)) { Fehlertext = text; return; }
            if (Fehlertext.IndexOf(text, StringComparison.Ordinal) >= 0) return;
            Fehlertext = Fehlertext + Environment.NewLine + text;
        }

        /// <summary>
        /// Protokoll- und Fehlerkanal dieses Laufs (Paket 8, Konzept 13.4).
        ///
        /// <see cref="Do_Simulation"/> zeigt hier auf den prozessweiten Kanal
        /// <see cref="SimulationProtokoll.Aktuell"/>. Der wird von den Einstiegspunkten
        /// eines Laufs erzeugt (<c>SimulationRunner.Simuliere</c>,
        /// <c>Form_Simulation_Detail.btn_Simulation_Click</c>) — und zwar VOR der
        /// Bedarfsrechnung, denn auch <see cref="SimulationWaermebedarf"/> und
        /// <see cref="SimulationStrombedarf"/> melden dorthin, und beide laufen vor der
        /// Kaskade. Wird <c>Do_Simulation</c> ohne einen solchen Einstieg gerufen, ist
        /// hier trotzdem ein gültiger (dann eben vorbelegter) Kanal.
        /// </summary>
        public SimulationProtokoll Protokoll = SimulationProtokoll.Aktuell;

        /// <summary>
        /// Führt einen kompletten Simulationslauf aus.
        ///
        /// PAKET 8 (Konzept 13.4): Der ganze Lauf steht im dialogfreien Modus von
        /// <see cref="DataRepository"/> — ein Datenbankfehler im Rechenpfad öffnet damit
        /// keine MessageBox mehr, sondern landet als Warnung im
        /// <see cref="Protokoll"/>. Für jede andere Nutzung von
        /// <c>DataRepository</c> ändert sich nichts (der Modus zählt und wird hier
        /// zuverlässig wieder freigegeben).
        /// </summary>
        public void Do_Simulation(int ID_Projekt)
        {
            using (DataRepository.EngineModus())
            {
                Do_Simulation_Intern(ID_Projekt);
                DbFehlerUebernehmen();
            }
        }

        /// <summary>Holt die im dialogfreien Modus aufgelaufenen DB-Meldungen ins Protokoll.</summary>
        private void DbFehlerUebernehmen()
        {
            foreach (string meldung in DataRepository.StilleFehlerAbholen())
                Protokoll.Warnung(string.Format(MyResource.Resource.SIMENG_DB_ZUGRIFF_WAEHREND_LAUF, meldung));
        }

        private void Do_Simulation_Intern(int ID_Projekt)
        {
            // Engine-Einstieg: Blockade bei nicht abgeschlossener Schema-Migration
            // (ADR-001, Aufgabe 6). Bewusst ohne MessageBox - die Engine bleibt
            // dialogfrei (Konzept 13.4); der Grund steht in Sperrgrund.
            Sperrgrund = "";
            Fehlertext = "";

            // Paket 8: an den Kanal dieses Laufs andocken. Ein eigener Kanal wird hier
            // BEWUSST nicht erzeugt - Wärme- und Strombedarf sind zu diesem Zeitpunkt
            // längst gerechnet und hätten ihre Meldungen sonst in ein verworfenes
            // Protokoll geschrieben.
            Protokoll = SimulationProtokoll.Aktuell;

            string sperrgrund;
            if (SchemaMigration.SimulationGesperrt(out sperrgrund))
            {
                Sperrgrund = sperrgrund;
                Protokoll.Fehlermeldung(string.Format(MyResource.Resource.SIMENG_SIMULATION_ABGEBROCHEN, sperrgrund));
                return;
            }

            float[] temp = new float[8760 * 4];
            float[] Eingang;
            float[] Ausgang;

            m_ID_Projekt = ID_Projekt;

            Array.Clear(Rest_Waermebedarf_stuendlich, 0, Rest_Waermebedarf_stuendlich.Length);
            Array.Clear(Rest_Strombedarf_viertelstuendlich, 0, Rest_Strombedarf_viertelstuendlich.Length);
            
            simulation_wp.Init();
            simulation_solarthermie.Init();
            simulation_spk.Init();
            simulation_pv.Init();

            // Neue Spalten (Prioritaet, Wärmequelle/-senke, Speicherregelung) sicherstellen,
            // bevor darauf zugegriffen wird - die Simulation kann ohne vorheriges Öffnen
            // des Konfigurationsdialogs gestartet werden.
            WaermequelleClass.SchemaSicherstellen();

            // Vorbelegung der Ladeprioritäten nachziehen (NULL -> 0, Konzept 3.4). Das
            // Gegenstück zum Schema: Migrationsregel R5 läuft genau einmal, über die
            // Oberfläche später angelegte Anlagen tragen wieder NULL. Rechnerisch ist das
            // gleichwertig - es hält den Bestand konsistent (siehe VorbelegungNachziehen).
            int nachgezogen = WaermesenkeClass.VorbelegungNachziehen(ID_Projekt);
            if (nachgezogen > 0)
                Protokoll.Hinweis(string.Format(
                    MyResource.Resource.SIMENG_LADEPRIO_VORBELEGUNG_NACHGEZOGEN, nachgezogen));

            // Feature-Flag der zweikanaligen Kaskade (Konzept Kapitel 9). Ab Etappe 4b
            // verzweigt es den Rechenweg: gesetzt -> Kaskade_Zweikanalig() nach der
            // Reihenfolge-Invariante 6.3, nicht gesetzt -> der unveränderte Altpfad.
            //
            // NACHARBEIT E-K1-2: Die Auswertung steht seit dieser Nacharbeit VOR dem
            // Registry-Aufbau. Sie muss dort schon feststehen, weil der Registry-Aufbau
            // die Verwendung des WP-Puffers festlegt und ein KOMBISPEICHER im Altpfad
            // ausdrücklich als Heizungspuffer zu führen ist (siehe
            // SpeicherRegistryAufbauen, Block 1). Der Protokollhinweis bleibt an seiner
            // bisherigen Stelle, damit die Reihenfolge im Protokollkanal unverändert ist.
            //
            // PAKET BHKW-REGULÄR (Entscheidung des Anwenders 17.08.2026, revidiert 6-1):
            // Steht ein BHKW in der Kaskade, gilt der Speicherstufen-Weg IMMER — auch
            // ohne Flag. Begründung des Anwenders: „es soll analog anderen Wärmeerzeugern
            // funktionieren, der Altpfad wird nicht benötigt." Der BHKW-Altpfad rechnete
            // seinen Speicher am Bedarf vorbei (Vektordifferenz Bedarf − Produktion,
            // Bilanzfehler aus Konzept 6.5) und ließ den Pufferspeicher unberücksichtigt;
            // dieser Weg ist mit diesem Paket ersatzlos entfallen.
            //
            // Ohne BHKW bleibt die Weiche Zeile für Zeile das, was sie war: Projekte mit
            // Wärmepumpe, Kessel und Solarthermie folgen weiterhin ausschließlich dem
            // Flag. Der Speicherstufen-Weg braucht dafür KEINE Kaskade und kein weiteres
            // Merkmal - ein BHKW allein in Tool_1 genügt.
            //
            // Die Anwesenheitsprüfung ist hier möglich, weil tool[] vor Do_Simulation
            // gesetzt wird (SimulationRunner: sim.tool = tool VOR sim.Do_Simulation).
            _bhkwErzwingtSpeicherstufe = KaskadeEnthaelt(DbWerte.ERZEUGER_BHKW);

            // PAKET PARALLELVERBUND (Entscheidung des Anwenders 17.08.2026): dasselbe
            // Oder-Glied, dieselbe Begründung. Führt mindestens eine Anlage des Projekts
            // zusätzliche Verbundmitglieder, rechnet der Lauf über die Speicherstufe —
            // sonst wäre die aufsummierte Kapazität gespeichert und angezeigt, aber nicht
            // gerechnet (Feldkommentar zu _verbundErzwingtSpeicherstufe).
            //
            // EIN Zugriff auf die Zuordnungstabelle; auf jeder Datenbank ohne Verbund
            // liefert er false, und die Weiche ist Zeile für Zeile die bisherige.
            _verbundErzwingtSpeicherstufe = AnlagePufferVerbundCtrl.ProjektHatVerbund(ID_Projekt);

            KaskadeZweikanalig = (ctrl_konfig != null && ctrl_konfig.model != null &&
                                  ctrl_konfig.model.Kaskade_Zweikanalig) ||
                                 _bhkwErzwingtSpeicherstufe ||
                                 _verbundErzwingtSpeicherstufe;

            // ***********************************************************************
            // Speicher-Registry aufbauen (Paket 4 - Konzept 6.2) und den Senkenspeicher
            // der Wärmepumpe daraus an das WP-Modul geben. Der zweikanalige Weg öffnet
            // die Registry danach noch für seine Speichermenge
            // (RegistryFuerZweikanaligOeffnen); der Altpfad rechnet mit genau diesem
            // einen Senkenspeicher weiter.
            // ***********************************************************************
            SpeicherRegistryAufbauen();
            simulation_wp.Pufferspeicher = puffer_wp;

            // PAKET 8 (Konzept 13.4): Die Extrapolationsrückfrage der WP-Kennlinie ist
            // zur Projekteinstellung geworden. Sie wird HIER an das Modul gegeben - vor
            // jedem der beiden Rechenwege, damit beide dieselbe Vorgabe sehen. Ohne
            // Konfigurationssatz gilt die Vorbelegung "erlaubt", also das bisherige
            // Verhalten (die Rückfrage wurde in jedem dokumentierten Lauf bejaht).
            simulation_wp.Extrapolation_Erlaubt =
                (ctrl_konfig == null || ctrl_konfig.model == null) || ctrl_konfig.model.Extrapolation_erlaubt;

            // Senkenzuordnungen des Projekts (Konzept 6.1) - ausgewertet nur im
            // zweikanaligen Weg.
            senkenzuordnungen = WaermesenkeClass.SenkenLaden(m_ID_Projekt);

            // Hinweis zum Rechenweg (die Auswertung selbst steht weiter oben, vor dem
            // Registry-Aufbau — siehe die Begründung dort). Der Grund wird MITGETEILT:
            // Bei einem BHKW-Projekt hat der Anwender den Schalter unter Umständen nie
            // berührt, und ein Rechenweg, den niemand angefordert hat, muss im Protokoll
            // erklärt sein (Paket BHKW-Regulär).
            if (_bhkwErzwingtSpeicherstufe)
                Protokoll.Hinweis("Das Projekt enthält ein BHKW - dieser Lauf rechnet " +
                                  "deshalb IMMER über die Speicherstufe mit herausgelöster " +
                                  "Ladephase (Konzept 6.3), unabhängig von der " +
                                  "Projekteinstellung Kaskade_Zweikanalig. Der einkanalige " +
                                  "BHKW-Altpfad ist entfallen (Paket BHKW-Regulär).");
            // Der Verbundhinweis steht NACH dem BHKW-Hinweis und als eigener Zweig: Bei
            // einem BHKW-Projekt mit Verbund ist der Rechenweg schon erklärt, und zwei
            // Sätze über dieselbe Weiche wären Rauschen. Ohne BHKW ist der Verbund der
            // Grund und muss genannt werden.
            else if (_verbundErzwingtSpeicherstufe)
                Protokoll.Hinweis("Mindestens ein Wärmeerzeuger des Projekts lädt einen " +
                                  "PARALLELVERBUND aus mehreren Pufferspeichern - dieser Lauf " +
                                  "rechnet deshalb IMMER über die Speicherstufe mit " +
                                  "herausgelöster Ladephase (Konzept 6.3), unabhängig von der " +
                                  "Projekteinstellung Kaskade_Zweikanalig. Der einkanalige " +
                                  "Altpfad kennt keine Ladeaufträge und würde die " +
                                  "aufsummierte Verbundkapazität nicht rechnen.");
            else if (KaskadeZweikanalig)
                Protokoll.Hinweis("Projekteinstellung Kaskade_Zweikanalig ist gesetzt - " +
                                  "dieser Lauf rechnet ZWEIKANALIG mit herausgelöster " +
                                  "Ladephase (Konzept 6.3).");

            Stundentemperatur = simulation_Waermebedarf.Stundentemperatur;
            Restwaerme = 0;
            Reststrom = simulation_Strombedarf.Strombedarf_gesamt; //MWh
            Rest_Strombedarf_viertelstuendlich = simulation_Strombedarf.Strombedarf_viertelStundenwerte;
            Rest_Waermebedarf_stuendlich = (float[])simulation_Waermebedarf.Waermebedarf.Clone();

            bSimulationWP = false;
            bSimulationKessel = false;
            bSimulationBHKW = false;
            bSimulationSolarthermie = false;
            bSimulationPV = false;
            bSimulationSSP = false;

            // Speicherergebnis des Vorlaufs verwerfen - sonst zeigten Chart und
            // Kennzahlen die Werte eines früheren Projekts an.
            Speicherergebnis = null;
            Array.Clear(Speicherfuellstand_viertelstuendlich, 0, Speicherfuellstand_viertelstuendlich.Length);
            Array.Clear(Speicherfuellstand_stuendlich, 0, Speicherfuellstand_stuendlich.Length);

            // Startpunkt der Simulation ist der Wärmebedarf
            Eingang = simulation_Waermebedarf.Waermebedarf;

            // ***********************************************************************
            // Etappe 4b: die zweikanalige Kaskade mit herausgelöster Ladephase
            // (Konzept 6.3) als EIGENER Rechenweg hinter dem Feature-Flag.
            //
            // Die Verzweigung steht bewusst VOR der bestehenden Schleife und nicht in
            // ihr: Der einkanalige Altpfad bleibt damit Zeile für Zeile unverändert und
            // ist als Rückfallebene durch Lesen nachweisbar, nicht erst durch Messen.
            //
            // PAKET BHKW-REGULÄR: Der Altpfad ist Rückfallebene nur noch für Projekte
            // OHNE BHKW. Sein BHKW-Zweig ist ersatzlos entfallen - er kann keines mehr
            // rechnen, und er bekommt seit der Erweiterung der Flag-Auswertung auch
            // keines mehr vorgesetzt.
            // ***********************************************************************

            // carrier ID, notwendig für die Berichtserzeugung holen (Brennstoff, Kosten, Emissionsberechnung)
            //
            // NULL-TOLERANT (Frage 21): Im Bestand stehen Anlagen ohne ID_Carrier bzw.
            // ohne Bezeichner (Projekt 1011); der direkte Cast brach den Lauf hier mit
            // einer InvalidCastException ab, bevor irgendein Ergebnis entstand. Eine
            // fehlende Zuordnung ist jetzt eine WARNUNG im Protokoll - gerechnet wird,
            // die Anlage steht im Bericht aber ohne Energieträger da (CarrierId 0, die
            // TryGetValue-Vorbelegung im SimulationRunner).
            EnergietraegerZuordnungLesen(WizardItemClass.BHKW_TYP, "BHKW", simulation_bhkw.bhkw_carrier);
            EnergietraegerZuordnungLesen(WizardItemClass.KESSEL_TYP, "Heizkessel", simulation_spk.spk_carrier);


            if (KaskadeZweikanalig)
            {
                Kaskade_Zweikanalig();
            }
            else
            {
                // ETAPPE D5a: Kombispeicher und Kessel-Quellbezug sind zweikanalige
                // Erweiterungen. Der Altpfad kennt weder zwei Kanäle noch eine
                // Speicherstufe mit mehreren Erzeugern - er rechnet unverändert weiter
                // und sagt, was das für diese Konfiguration bedeutet.
                AltpfadHinweiseD5a();

                for (int i = 0; i < 4; i++)
                {
                    if (tool[i] == DbWerte.ERZEUGER_WAERMEPUMPE)
                    {
                        Ausgang = Simulation_WP_Ctrl(Eingang, Viertelstunden_zu_Stundenwerte_Mittelwert(Rest_Strombedarf_viertelstuendlich), ctrl_konfig.model.m_WP_Heizstab);
     
                        if (m_bError) Ausgang = Eingang;
                        Restwaerme = 0;
                        for (int n = 0; n < 8760; n++) Restwaerme += Ausgang[n];
                        Rest_Waermebedarf_stuendlich = Ausgang;
                        Eingang = Ausgang;

                        Reststrom += (float)simulation_wp.WP_Strombedarf_gesamt / 1000f; // in MWh
                        Reststrom += (float)simulation_wp.Heizstab_gesamt / 1000f; // in MWh

                        temp = Stundenwerte_zu_viertelstunden(simulation_wp.WP_Strombedarf_stuendlich);
                        Rest_Strombedarf_viertelstuendlich = AddVectors(Rest_Strombedarf_viertelstuendlich, temp);
                        temp = Stundenwerte_zu_viertelstunden(simulation_wp.Heizstab_stuendlich);
                        Rest_Strombedarf_viertelstuendlich = AddVectors(Rest_Strombedarf_viertelstuendlich, temp);
                        bSimulationWP = true;
                    }
                    else if (tool[i] == DbWerte.ERZEUGER_HEIZKESSEL)
                    {
                        Ausgang = Simulation_SPK_Ctrl(Eingang, Viertelstunden_zu_Stundenwerte_Mittelwert(Rest_Strombedarf_viertelstuendlich), ctrl_konfig.model.m_Kessel_Betriebsbereitschaft);
                        Restwaerme = 0;
                        for (int n = 0; n < 8760; n++) Restwaerme += Ausgang[n];
                        Rest_Waermebedarf_stuendlich = Ausgang;
                        Eingang = Ausgang;
                   
                        temp = Stundenwerte_zu_viertelstunden(simulation_spk.Stromverbrauch_stuendlich);
                        Rest_Strombedarf_viertelstuendlich = AddVectors(Rest_Strombedarf_viertelstuendlich, temp);
                    
                        bSimulationKessel = true;
                    }
                    else if (tool[i] == DbWerte.ERZEUGER_SOLARTHERMIE)
                    {
                        Ausgang = Simulation_Solarthermie_Ctrl(Eingang);

                        Restwaerme = 0;
                        for (int n = 0; n < 8760; n++) Restwaerme += Ausgang[n];
                        Rest_Waermebedarf_stuendlich = Ausgang;
                        Eingang = Ausgang;

                        bSimulationSolarthermie = true;
                    }
                    // PAKET BHKW-REGULÄR: Hier stand der einkanalige BHKW-Zweig
                    // (Simulation_BHKW_Ctrl mit skalarem Pendelspeicher). Er ist
                    // entfallen - ein Projekt mit BHKW erreicht diese Schleife nicht
                    // mehr, weil die Weiche es ausnahmslos in die Speicherstufe schickt
                    // (Entscheidung des Anwenders 17.08.2026, revidiert 6-1). Ein
                    // toter else-if-Zweig für ERZEUGER_BHKW bliebe eine Attrappe: Er
                    // sähe wie eine Rückfallebene aus, wäre aber keine.
                }
            }

            // Photovoltaik abziehen
            if (tool[4] == DbWerte.ERZEUGER_PHOTOVOLTAIK)
            {
                var x = Rest_Strombedarf_viertelstuendlich.Sum() / 4000;
                temp = Simulation_Photovoltaik_Ctrl(Rest_Strombedarf_viertelstuendlich);
                Rest_Strombedarf_viertelstuendlich = SubVectors(Rest_Strombedarf_viertelstuendlich, temp);
                bSimulationPV = true;
            }

            // ***********************************************************************
            // Stromspeicher verrechnen (AP2b): die SpeicherEngine statt des
            // wirkungslosen SimulationSSP-Stubs.
            //
            // EINGEBETTET WIRD NUR DIE ENTLADUNG. Sie deckt Residuallast und senkt
            // damit den Netzbezug des Intervalls - genau die Größe, die dieser Vektor
            // führt. Die LADUNG speist sich aus dem Erzeugungsüberschuss und mindert
            // die Einspeisung, nicht den Netzbezug; sie darf hier deshalb NICHT
            // aufgeschlagen werden (der Überschuss steht nach dem PV-Block ohnehin
            // nicht mehr im Vektor - SubVectors klemmt bei 0).
            //
            // Die Stelle liegt hinter BEIDEN Rechenwegen (Altpfad und
            // Kaskade_Zweikanalig): Beide bauen denselben Lastvektor aus denselben
            // Reihen (WP-Strombedarf, Heizstab, Kesselstrom, BHKW-Erzeugung) und
            // setzen dieselben Modulflags, die der Controller beim Beschaffen der
            // Lastreihe auswertet.
            // ***********************************************************************
            if (tool[5] == DbWerte.ERZEUGER_STROMSPEICHER)
            {
                temp = Simulation_Stromspeicher_Ctrl(m_ID_Projekt);
                if (temp != null)
                {
                    Rest_Strombedarf_viertelstuendlich = SubVectors(Rest_Strombedarf_viertelstuendlich, temp);
                    bSimulationSSP = true;
                }
            }

            // Wärmebedarf von kWh in MWh umrechnen
            Restwaerme /= 1000f;

            // Reststrom mathematisch korrekt aus dem finalen Ergebnis-Vektor berechnen
            // Falls deine Quell-Vektoren stündliche kW-Mittelwerte/kWh enthalten:
            Reststrom = Rest_Strombedarf_viertelstuendlich.Sum() / 4000f;

            // ***********************************************************************
            // Nachlauf (Paket 7): Kennzahlen aller beteiligten Speicher aus ihren
            // Ganglinien bilden (SOC_Mittel/SOC_Max/Vollzyklen, Konzept 6.6) und die
            // Eingangsgrößen der VDI-4640-Auslegungsprüfung bereitstellen (13.1).
            // Beides ist reine Auswertung - es verändert kein Simulationsergebnis.
            // ***********************************************************************
            foreach (SimulationPufferspeicher sp in AlleSpeicher())
                sp.KennzahlenBerechnen();

            ErdreichAuswertung.AusLauf(this);
        }

        /// <summary>
        /// Liest je Anlage des Typs die Zuordnung Bezeichner → ID_Carrier aus
        /// <c>Tab_Energieanlagen</c> in <paramref name="ziel"/> - die Grundlage der
        /// Berichtserzeugung (Brennstoff, Kosten, Emissionen).
        ///
        /// NULL-TOLERANT nach dem Muster von <c>WErzeugerCtrl.Belegt</c> (Frage 21):
        /// Ein NULL in Bezeichner oder ID_Carrier ist ein Datenzustand, kein
        /// Absturzgrund. Projekt 1011 führt genau so eine Anlage und brach bis dahin
        /// mit einer InvalidCastException ab - ohne Protokoll, ohne Ergebnis. Solche
        /// Anlagen werden übersprungen und als Warnung gemeldet (Konzept 13.4: kein
        /// Ergebnis, das vollständig aussieht); im Bericht stehen sie wie jede Anlage
        /// ohne Zuordnung mit CarrierId 0 (SimulationRunner, TryGetValue-Vorbelegung).
        /// </summary>
        private void EnergietraegerZuordnungLesen(int idType, string gewerk, Dictionary<string, int> ziel)
        {
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + idType);
            while (rs.Next())
            {
                object bezeichner = rs.Read("Bezeichner");
                object carrier = rs.Read("ID_Carrier");

                if (bezeichner == null || bezeichner == DBNull.Value)
                {
                    Protokoll.Warnung("Energieträger-Zuordnung: Eine " + gewerk + "-Anlage des Projekts " +
                                      "(Tab_Energieanlagen ID " + rs.GetString("ID") + ") trägt keinen " +
                                      "Bezeichner - Brennstoff, Kosten und Emissionen dieser Anlage können " +
                                      "im Bericht keinem Energieträger zugeordnet werden.");
                    continue;
                }

                if (carrier == null || carrier == DBNull.Value)
                {
                    Protokoll.Warnung("Energieträger-Zuordnung: Der " + gewerk + "-Anlage „" + bezeichner +
                                      "\" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, " +
                                      "Kosten und Emissionen dieser Anlage können im Bericht nicht " +
                                      "ausgewiesen werden.");
                    continue;
                }

                ziel.TryAdd(bezeichner.ToString(), Convert.ToInt32(carrier));
            }
            rs.Close();
        }

        // ===================================================================
        // Zweikanalige Kaskade (Paket 4, Etappe 4b - Konzept 6.3)
        // ===================================================================

        /// <summary>
        /// Die zweikanalige Kaskade: derselbe Erzeugerdurchlauf tool[0..3] wie im
        /// Altpfad, aber auf den beiden Bedarfskanälen (Konzept 3.2) statt auf einem
        /// Summenvektor.
        ///
        /// ARBEITSTEILUNG seit Paket 5:
        ///
        ///   SPEICHERSTUFE — eine gemeinsame Stundenschleife (<see cref="Kaskadenschleife"/>)
        ///               mit der vollständigen Reihenfolge-Invariante A–G. In ihr rechnen
        ///               die WÄRMEPUMPE (immer, wenn sie in der Kaskade steht) sowie
        ///               SOLARTHERMIE und HEIZKESSEL, sobald mindestens eine ihrer
        ///               Anlagen einen Puffer als Senke führt. Nur so kann ein Speicher
        ///               von zwei Erzeugern bedient werden, und nur so läuft
        ///               <c>StundeAbschliessen</c> genau einmal je Stunde und Speicher.
        ///   VEKTORSTUFEN — Solarthermie, Heizkessel und (seit Paket 6) BHKW OHNE
        ///               Speicherbeteiligung rechnen zweikanalig, aber weiterhin als
        ///               eigene Jahresschleife an ihrer Kaskadenposition. Sie berühren
        ///               keinen Speicher; ihr Ergebnis hängt allein vom Kanalzustand an
        ///               dieser Position ab.
        ///
        /// SEIT PAKET 6 ist der Kompatibilitätsanker <c>Waermekanaele.Uebernehmen</c> in
        /// diesem Rechenweg vollständig aufgelöst: Auch das BHKW rechnet zweikanalig,
        /// wertet seine Senken aus (Vorgaberang 30) und wird Mitglied der Speicherstufe,
        /// sobald es einen Speicher hat — Puffer-Senke oder Pendelspeicher.
        ///
        /// ABGRENZUNG: Die Speicherstufe läuft an der Kaskadenposition ihres ERSTEN
        /// Mitglieds; weitere Mitglieder werden dort mitgerechnet, in ihrer
        /// Kaskadenreihenfolge (Phase B). Eine Stufe OHNE Speicherbeteiligung, die
        /// ZWISCHEN zwei Mitgliedern stünde, wird selbst Mitglied
        /// (<see cref="ZwischenstufenAufnehmen"/>) — damit gibt es keinen stillen
        /// Positionswechsel mehr, für keine der vier Erzeugerarten.
        /// </summary>
        private void Kaskade_Zweikanalig()
        {
            // Kanäle aus dem Bedarf bilden (Konzept 3.2): HEIZUNG als Residuum NACH dem
            // Netzverlust-Aufschlag, BRAUCHWASSER = brauchwasserwerte.
            Waermekanaele kanaele = simulation_Waermebedarf.Kanaele();

            // Datenkorrektur am Bedarf: gehört als WARNUNG in das Lauf-Protokoll
            // (Protokollkanal-Nachzug). Der Zähler ist bereits über das ganze Jahr
            // aggregiert - eine Meldung je Lauf, kein Meldungssturm.
            if (simulation_Waermebedarf.Kanal_Kappungen > 0)
                Protokoll.Warnung("Kanalbildung: in " + simulation_Waermebedarf.Kanal_Kappungen +
                                  " Stunden lag der Brauchwasserwert über dem Gesamtbedarf (" +
                                  simulation_Waermebedarf.Kanal_Kappung_kWh.ToString("0.###") +
                                  " kWh gekappt) - der Heizkanal wurde auf 0 gesetzt.");

            float[] temp;

            // Welche Erzeugerarten gehören in die gemeinsame Speicherstufe? Kriterium ist
            // die SENKENREFERENZ einer Anlage (WS_ID_Puffer / WS_ID_Puffer2 mit
            // Puffer-Ziel) — dieselbe Referenz, aus der Ladeordnung.Ladereihenfolge die
            // Ladeaufträge bildet. Die Wärmepumpe ist immer dabei: Sie führt Heizstab,
            // Quellspeicher und Bivalenzpunkt und rechnet seit Etappe 4b ohnehin in
            // dieser Schleife.
            _wpInSchleife = KaskadeEnthaelt(DbWerte.ERZEUGER_WAERMEPUMPE);
            _solarInSchleife = KaskadeEnthaelt(DbWerte.ERZEUGER_SOLARTHERMIE) &&
                               ErzeugerMitPufferSenke(ProjektPuffer.TYP_SOLARTHERMIE);
            // ETAPPE D5a: Auch eine Puffer-QUELLE macht den Kessel zum Schleifenmitglied.
            // Er entnimmt dann Wärme aus einem Speicher der Stufe (Kessel-Kaskade,
            // Konzept Anforderung 6) - als Vektorstufe außerhalb der Stundenschleife
            // hätte er darauf keinen Zugriff, und die Reihenfolge „nach seinem Puffer"
            // gäbe es dort nicht. Ohne Kessel mit WQ_Typ = Pufferspeicher - der gesamte
            // Bestand - ist die Bedingung unverändert.
            bool kesselPufferSenke = ErzeugerMitPufferSenke(ProjektPuffer.TYP_KESSEL);
            bool kesselPufferQuelle = ErzeugerMitPufferQuelle(ProjektPuffer.TYP_KESSEL);
            _kesselInSchleife = KaskadeEnthaelt(DbWerte.ERZEUGER_HEIZKESSEL) &&
                                (kesselPufferSenke || kesselPufferQuelle);

            // E-K2-4: Steht der Kessel ALLEIN wegen seiner Puffer-Quelle in der Schleife,
            // ist nach dem Aufbau der Quellbezüge zu melden, wenn gar keiner zustande
            // gekommen ist - sonst rechnet er still anders als vorher.
            _kesselNurWegenQuelle = _kesselInSchleife && !kesselPufferSenke && kesselPufferQuelle;

            // PAKET 6: Das BHKW ist Mitglied der SPEICHERSTUFE, sobald es einen Speicher
            // hat - entweder über eine Puffer-Senke (WS_ID_Puffer/WS_ID_Puffer2, derselbe
            // Test wie bei Solarthermie und Kessel) oder über seinen PENDELSPEICHER, der
            // im neuen Weg durch eine SimulationPufferspeicher-Instanz abgelöst wird
            // (Konzept 6.5, zweiter Punkt). Ohne beides hat es keine Speicherbeteiligung
            // und bleibt Vektorstufe an seiner Kaskadenposition - zweikanalig, aber ohne
            // die Phasen A, C, D, E und G.
            //
            // PAKET BHKW-REGULÄR: Diese Bedingung ist UNVERÄNDERT geblieben, und das ist
            // Absicht. Die Weiche entscheidet nur, dass ein BHKW-Projekt überhaupt
            // hierher kommt; ob das BHKW dann in der Stundenschleife oder als Vektorstufe
            // rechnet, bleibt eine Frage seines Speichers. Ein BHKW ohne jeden Puffer hat
            // in einer Speicherstufe nichts zu suchen - es würde dort nur die Bezugsgrößen
            // der übrigen Mitglieder verschieben, ohne selbst etwas zu gewinnen.
            _bhkwInSchleife = KaskadeEnthaelt(DbWerte.ERZEUGER_BHKW) &&
                              (ErzeugerMitPufferSenke(ProjektPuffer.TYP_BHKW) ||
                               VolumenPendelspeicherBHKW > 0);

            // Paket-5-Nacharbeit, Befund N4: Stufen, die ZWISCHEN zwei Mitgliedern
            // stünden, werden ebenfalls Mitglied - sonst rechneten sie stillschweigend
            // NACH der gesamten Speicherstufe.
            ZwischenstufenAufnehmen();

            bool schleifeGelaufen = false;

            for (int i = 0; i < 4; i++)
            {
                bool istSchleifenstufe = IstSchleifenstufe(i);

                if (istSchleifenstufe)
                {
                    if (schleifeGelaufen) continue;   // an ihrer ersten Position gerechnet
                    schleifeGelaufen = true;

                    // Rückfallebene: Bricht die Kennlinienauswertung ab, bleiben die
                    // Kanäle unverändert - dasselbe, was der Altpfad mit
                    // "Ausgang = Eingang" tut.
                    Waermekanaele vorher = kanaele.Clone();

                    Speicherstufe_Rechnen(kanaele,
                        Viertelstunden_zu_Stundenwerte_Mittelwert(Rest_Strombedarf_viertelstuendlich),
                        ctrl_konfig.model.m_WP_Heizstab,
                        ctrl_konfig.model.m_Kessel_Betriebsbereitschaft);

                    if (m_bError)
                    {
                        Array.Copy(vorher.Heiz, kanaele.Heiz, Waermekanaele.STUNDEN_JAHR);
                        Array.Copy(vorher.WW, kanaele.WW, Waermekanaele.STUNDEN_JAHR);
                    }

                    if (_wpInSchleife)
                    {
                        Reststrom += (float)simulation_wp.WP_Strombedarf_gesamt / 1000f; // in MWh
                        Reststrom += (float)simulation_wp.Heizstab_gesamt / 1000f;       // in MWh

                        temp = Stundenwerte_zu_viertelstunden(simulation_wp.WP_Strombedarf_stuendlich);
                        Rest_Strombedarf_viertelstuendlich = AddVectors(Rest_Strombedarf_viertelstuendlich, temp);
                        temp = Stundenwerte_zu_viertelstunden(simulation_wp.Heizstab_stuendlich);
                        Rest_Strombedarf_viertelstuendlich = AddVectors(Rest_Strombedarf_viertelstuendlich, temp);
                        bSimulationWP = true;
                    }

                    if (_kesselInSchleife)
                    {
                        // Paket-5-Nacharbeit, Befund N3 (zweiter Teil): BEZUGSPUNKT des
                        // Kessel-Strombedarfs. Im Altpfad wird der Kessel NACH der
                        // Wärmepumpe gerufen und sieht deshalb den Strombedarf nach
                        // deren Verbrauch. In der gemeinsamen Stundenschleife gibt es
                        // diese Reihenfolge nicht mehr — der Wert wird deshalb hier
                        // nachgezogen, sobald der WP-Strom feststeht, und zwar über
                        // exakt dieselbe Vektorkette wie im Altpfad. Steht der Kessel in
                        // der Kaskade VOR der Wärmepumpe, bleibt es beim Stufeneingang.
                        if (_wpInSchleife && KesselHinterWaermepumpe())
                        {
                            float[] stromNachWP =
                                Viertelstunden_zu_Stundenwerte_Mittelwert(Rest_Strombedarf_viertelstuendlich);
                            simulation_spk.Strombedarf_stuendlich = stromNachWP;
                            simulation_spk.Strombedarf_gesamt = stromNachWP.Sum();
                        }

                        temp = Stundenwerte_zu_viertelstunden(simulation_spk.Stromverbrauch_stuendlich);
                        Rest_Strombedarf_viertelstuendlich = AddVectors(Rest_Strombedarf_viertelstuendlich, temp);
                        bSimulationKessel = true;
                    }

                    if (_solarInSchleife) bSimulationSolarthermie = true;

                    if (_bhkwInSchleife)
                    {
                        // Die Stromerzeugung des BHKW senkt den Strombedarf - dieselbe
                        // Vektorkette wie im Altpfad, nur an der Position der Stufe.
                        float[] bhkwStromVs =
                            Stundenwerte_zu_viertelstunden(simulation_bhkw.stromproduktion);
                        Rest_Strombedarf_viertelstuendlich =
                            SubVectors(Rest_Strombedarf_viertelstuendlich, bhkwStromVs, false);
                        bSimulationBHKW = true;
                    }

                    continue;
                }

                if (tool[i] == DbWerte.ERZEUGER_HEIZKESSEL)
                {
                    // Vektorstufe: zweikanalig, aber ohne Speicherbeteiligung.
                    Simulation_SPK_Ctrl_Zweikanalig(kanaele,
                        Viertelstunden_zu_Stundenwerte_Mittelwert(Rest_Strombedarf_viertelstuendlich),
                        ctrl_konfig.model.m_Kessel_Betriebsbereitschaft);

                    temp = Stundenwerte_zu_viertelstunden(simulation_spk.Stromverbrauch_stuendlich);
                    Rest_Strombedarf_viertelstuendlich = AddVectors(Rest_Strombedarf_viertelstuendlich, temp);

                    bSimulationKessel = true;
                }
                else if (tool[i] == DbWerte.ERZEUGER_SOLARTHERMIE)
                {
                    // Vektorstufe: zweikanalig, aber ohne Speicherbeteiligung.
                    Simulation_Solarthermie_Ctrl_Zweikanalig(kanaele);

                    bSimulationSolarthermie = true;
                }
                else if (tool[i] == DbWerte.ERZEUGER_BHKW)
                {
                    // PAKET 6: Vektorstufe - zweikanalig, aber ohne Speicherbeteiligung.
                    // Der Kompatibilitätsanker Waermekanaele.Uebernehmen ist damit auch
                    // für das BHKW aufgelöst; der Warnzweig „BHKW zwischen zwei Mitgliedern
                    // rechnet danach" ist entfallen, weil das BHKW in genau diesem Fall
                    // jetzt selbst Mitglied wird (ZwischenstufenAufnehmen).
                    Simulation_BHKW_Ctrl_Zweikanalig(kanaele,
                        Viertelstunden_zu_Stundenwerte_Mittelwert(Rest_Strombedarf_viertelstuendlich));

                    float[] bhkwStromViertelstuendlich =
                        Stundenwerte_zu_viertelstunden(simulation_bhkw.stromproduktion);
                    Rest_Strombedarf_viertelstuendlich =
                        SubVectors(Rest_Strombedarf_viertelstuendlich, bhkwStromViertelstuendlich, false);

                    bSimulationBHKW = true;
                }
            }

            // Ergebnis der Wärmeseite: die Summe der beiden Restkanäle. Ein EIGENER
            // Vektor - im Altpfad zeigt Rest_Waermebedarf_stuendlich auf das
            // Ausgangsarray des letzten Moduls (Aliasing, B0-2).
            Rest_Waermebedarf_stuendlich = kanaele.Summe();

            Restwaerme = 0;
            for (int n = 0; n < 8760; n++) Restwaerme += Rest_Waermebedarf_stuendlich[n];
        }

        // NACHARBEIT PAKET 6, BEFUND N10: Hier stand „RestAufKanaeleZurueck" — die
        // proportionale Rückverteilung des Rests eines EINKANALIG rechnenden Erzeugers
        // über Waermekanaele.Uebernehmen. Seit das BHKW zweikanalig rechnet, hat der
        // Kompatibilitätsanker keinen Aufrufer mehr; die Methode ist entfallen.

        /// <summary>
        /// true, wenn der Heizkessel in der Kaskade HINTER der Wärmepumpe steht — die
        /// Bedingung dafür, dass er den Strombedarf NACH dem WP-Verbrauch sieht
        /// (Paket-5-Nacharbeit, Befund N3).
        /// </summary>
        private bool KesselHinterWaermepumpe()
        {
            if (tool == null) return false;

            int wp = -1, kessel = -1;
            for (int i = 0; i < 4 && i < tool.Length; i++)
            {
                if (tool[i] == DbWerte.ERZEUGER_WAERMEPUMPE && wp < 0) wp = i;
                if (tool[i] == DbWerte.ERZEUGER_HEIZKESSEL && kessel < 0) kessel = i;
            }

            return wp >= 0 && kessel > wp;
        }

        /// <summary>true, wenn <c>Tool_1..4</c> den Erzeuger enthält.</summary>
        private bool KaskadeEnthaelt(string erzeuger)
        {
            if (tool == null) return false;
            for (int i = 0; i < 4 && i < tool.Length; i++)
                if (tool[i] == erzeuger) return true;
            return false;
        }

        // NACHARBEIT PAKET 6, BEFUND N10: Hier stand „SchleifenstufeNach(position)" — die
        // Prüfung für den entfallenen Warnzweig „BHKW zwischen zwei Mitgliedern rechnet
        // DANACH". Seit ZwischenstufenAufnehmen auch das BHKW aufnimmt, hat sie keinen
        // Aufrufer mehr und ist entfallen.

        /// <summary>true, wenn die Stufe an dieser Kaskadenposition in der Speicherstufe rechnet.</summary>
        private bool IstSchleifenstufe(int position)
        {
            if (tool == null || position < 0 || position >= 4 || position >= tool.Length) return false;

            return (tool[position] == DbWerte.ERZEUGER_WAERMEPUMPE && _wpInSchleife) ||
                   (tool[position] == DbWerte.ERZEUGER_SOLARTHERMIE && _solarInSchleife) ||
                   (tool[position] == DbWerte.ERZEUGER_HEIZKESSEL && _kesselInSchleife) ||
                   (tool[position] == DbWerte.ERZEUGER_BHKW && _bhkwInSchleife);
        }

        /// <summary>
        /// Nimmt Solarthermie- und Kesselstufen, die in der Kaskade ZWISCHEN zwei
        /// Mitgliedern der Speicherstufe stehen, ebenfalls in die Schleife auf
        /// (Paket-5-Nacharbeit, Befund N4).
        ///
        /// DAS PROBLEM: Die Speicherstufe rechnet an der Kaskadenposition ihres ERSTEN
        /// Mitglieds. Eine Stufe ohne Puffer-Senke dazwischen wäre damit hinter die
        /// gesamte Stufe gerutscht — inklusive Nachentladung und Heizstab — und hätte
        /// stillschweigend ein anderes Ergebnis geliefert (gemessen: Solarproduktion in
        /// einem präparierten 1011 von 0,64 auf 0,28 MWh). Für das BHKW wurde dieser Fall
        /// bereits protokolliert, für Solarthermie und Heizkessel nicht.
        ///
        /// DIE LÖSUNG ist strukturell statt hinweisend: Beide Stufen KÖNNEN stundenweise
        /// rechnen (Paket 5 hat sie dafür zerlegt). Sie nehmen dann ohne Puffer-Senke als
        /// reine Heizkreis-Lieferanten an Phase B teil — an genau ihrer
        /// Kaskadenposition —, und der Positionswechsel verschwindet. Nur das BHKW bleibt
        /// bis Paket 6 draußen; für dieses eine verbleibende Vorkommen steht die Warnung
        /// weiterhin in <see cref="Kaskade_Zweikanalig"/>.
        ///
        /// EIN DURCHLAUF GENÜGT: Ein neu aufgenommenes Mitglied liegt selbst zwischen dem
        /// ersten und dem letzten - das Intervall wächst dadurch nicht.
        ///
        /// ABGRENZUNG: Stufen VOR dem ersten und NACH dem letzten Mitglied bleiben
        /// Vektorstufen. Ihre Kaskadenposition stimmt dort ohnehin (die Schleife als
        /// Ganzes steht zwischen ihnen), und sie in die Schleife zu ziehen würde die
        /// Bezugsgrößen der übrigen Stufen verschieben, ohne etwas zu gewinnen. Genau
        /// deshalb bleiben die neun Referenzprojekte unverändert: Keines hat heute mehr
        /// als EIN Mitglied.
        /// </summary>
        private void ZwischenstufenAufnehmen()
        {
            if (tool == null) return;

            int erste = -1, letzte = -1;
            for (int i = 0; i < 4 && i < tool.Length; i++)
                if (IstSchleifenstufe(i)) { if (erste < 0) erste = i; letzte = i; }

            if (erste < 0 || letzte <= erste + 1) return;

            for (int i = erste + 1; i < letzte; i++)
            {
                if (tool[i] == DbWerte.ERZEUGER_SOLARTHERMIE && !_solarInSchleife)
                {
                    _solarInSchleife = true;
                    Protokoll.Hinweis("Kaskade: Die Solarthermie steht zwischen zwei Erzeugern der " +
                                      "Speicherstufe. Sie rechnet deshalb als Mitglied der " +
                                      "Stundenschleife an ihrer Kaskadenposition mit (Phase B) - " +
                                      "ohne Puffer-Senke als reine Heizkreis-Stufe.");
                }
                else if (tool[i] == DbWerte.ERZEUGER_HEIZKESSEL && !_kesselInSchleife)
                {
                    _kesselInSchleife = true;
                    Protokoll.Hinweis("Kaskade: Der Heizkessel steht zwischen zwei Erzeugern der " +
                                      "Speicherstufe. Er rechnet deshalb als Mitglied der " +
                                      "Stundenschleife an seiner Kaskadenposition mit (Phase B) - " +
                                      "ohne Puffer-Senke als reine Heizkreis-Stufe.");
                }
                else if (tool[i] == DbWerte.ERZEUGER_BHKW && !_bhkwInSchleife)
                {
                    // PAKET 6: Seit das BHKW stundenweise rechnen kann, gilt für es
                    // dieselbe Regel wie für Solarthermie und Kessel. Der frühere
                    // Sonderfall - "BHKW zwischen zwei Mitgliedern rechnet DANACH" - ist
                    // damit entfallen, und mit ihm sein Warnzweig.
                    _bhkwInSchleife = true;
                    Protokoll.Hinweis("Kaskade: Das BHKW steht zwischen zwei Erzeugern der " +
                                      "Speicherstufe. Es rechnet deshalb als Mitglied der " +
                                      "Stundenschleife an seiner Kaskadenposition mit (Phase B) - " +
                                      "ohne Speicher als reine Heizkreis-Stufe.");
                }
            }
        }

        // Mitglieder der gemeinsamen Speicherstufe des laufenden Simulationslaufs
        // (Paket 5, um das BHKW erweitert in Paket 6). Sie werden in
        // Kaskade_Zweikanalig bestimmt und von LadeordnungAufbauen gelesen.
        private bool _wpInSchleife = false;
        private bool _solarInSchleife = false;
        private bool _kesselInSchleife = false;
        private bool _bhkwInSchleife = false;

        /// <summary>
        /// true, wenn der Heizkessel ALLEIN wegen einer Puffer-QUELLE in der
        /// Stundenschleife rechnet (Nacharbeit E-K2-4). Dann muss ein Quellbezug auch
        /// wirklich entstehen — sonst rechnet der Kessel anders als bisher, ohne dass
        /// die Kaskade greift, und genau das ist zu melden.
        /// </summary>
        private bool _kesselNurWegenQuelle = false;

        /// <summary>Hat die Wärmepumpe in diesem Lauf in der gemeinsamen Speicherstufe gerechnet?</summary>
        public bool WPInSpeicherstufe { get { return _wpInSchleife; } }

        /// <summary>Hat die Solarthermie in diesem Lauf in der gemeinsamen Speicherstufe gerechnet?</summary>
        public bool SolarInSpeicherstufe { get { return _solarInSchleife; } }

        /// <summary>Hat der Heizkessel in diesem Lauf in der gemeinsamen Speicherstufe gerechnet?</summary>
        public bool KesselInSpeicherstufe { get { return _kesselInSchleife; } }

        /// <summary>Hat das BHKW in diesem Lauf in der gemeinsamen Speicherstufe gerechnet? (Paket 6)</summary>
        public bool BHKWInSpeicherstufe { get { return _bhkwInSchleife; } }

        /// <summary>
        /// true, wenn mindestens eine Anlage dieser Erzeugerart im Projekt einen
        /// PUFFER als Haupt- oder Zweitsenke führt (Konzept 6.1).
        ///
        /// Geprüft wird gegen dieselbe Bedingung, mit der
        /// <see cref="Ladeordnung.Ladereihenfolge"/> eine ladende Anlage erkennt: gesetzte
        /// Puffer-ID UND Puffer-Ziel. Altdaten können eine <c>WS_ID_Puffer</c> tragen und
        /// trotzdem auf den Heizkreis zeigen; solche Reste dürfen keine Speicherstufe
        /// auslösen, denn es entstünde kein einziger Ladeauftrag daraus.
        ///
        /// Dialogfrei über <see cref="StilleDb"/> (Konzept 13.4).
        /// </summary>
        private bool ErzeugerMitPufferSenke(int idType)
        {
            DataTable dt = StilleDb.Tabelle(
                "SELECT WS_Ziel, WS_ID_Puffer, WS_Ziel2, WS_ID_Puffer2 FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND ID_Type = ?",
                StilleDb.Par("@proj", OleDbType.Integer, m_ID_Projekt),
                StilleDb.Par("@typ", OleDbType.Integer, idType));

            if (dt == null) return false;

            foreach (DataRow r in dt.Rows)
            {
                if (StilleDb.Zahl(StilleDb.Feld(r, "WS_ID_Puffer")) > 0 &&
                    WaermesenkeClass.IstPufferZiel(StilleDb.Text(StilleDb.Feld(r, "WS_Ziel"))))
                    return true;

                if (StilleDb.Zahl(StilleDb.Feld(r, "WS_ID_Puffer2")) > 0 &&
                    WaermesenkeClass.IstPufferZiel(StilleDb.Text(StilleDb.Feld(r, "WS_Ziel2"))))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// true, wenn mindestens eine Anlage dieser Art einen Pufferspeicher als
        /// WÄRMEQUELLE führt (<c>WQ_Typ = Pufferspeicher</c>) und dieser Puffer im
        /// Projekt auch WIRKLICH EXISTIERT — Etappe D5a, siehe
        /// <see cref="_kesselInSchleife"/>.
        ///
        /// <para>NACHARBEIT E-K2-4: Das ursprüngliche Prädikat prüfte
        /// <c>WQ_ID_Puffer IS NOT NULL</c>. Das ist breiter als der tatsächliche
        /// Quellbezug — eine 0 ist „not null", und ein Altdatenrest kann auf einen längst
        /// gelöschten Puffer zeigen. Beides zog den Kessel in die Stundenschleife (statt
        /// ihn als Vektorstufe zu rechnen) und änderte damit bei gesetztem Flag das
        /// Ergebnis, ohne dass ein Quellbezug entstand. Jetzt gilt: gesetzte ID
        /// <b>und</b> ein Puffer dieses Projekts. Die Fälle, die erst später auffallen
        /// (Puffer rechnet nicht mit, kein Temperaturpaar), meldet
        /// <see cref="KesselQuelleOhneWirkungMelden"/> nach dem Aufbau der Quellbezüge.</para>
        ///
        /// Dialogfrei über <see cref="StilleDb"/> (Konzept 13.4).
        /// </summary>
        private bool ErzeugerMitPufferQuelle(int idType)
        {
            DataTable dt = StilleDb.Tabelle(
                "SELECT WQ_ID_Puffer FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ? " +
                "AND WQ_Typ = ? AND WQ_ID_Puffer > 0",
                StilleDb.Par("@proj", OleDbType.Integer, m_ID_Projekt),
                StilleDb.Par("@typ", OleDbType.Integer, idType),
                StilleDb.Par("@wq", OleDbType.VarWChar, WaermequelleClass.TYP_PUFFER));

            if (dt == null) return false;

            foreach (DataRow r in dt.Rows)
            {
                int idPuffer = StilleDb.Zahl(StilleDb.Feld(r, "WQ_ID_Puffer"));
                if (idPuffer <= 0) continue;

                WaermesenkeClass.PufferInfo p = WaermesenkeClass.PufferLesen(idPuffer);
                if (p != null && p.ID_Projekt == m_ID_Projekt) return true;
            }

            return false;
        }

        /// <summary>
        /// Die gemeinsame SPEICHERSTUFE des zweikanaligen Wegs (Paket 5). Aufbau der
        /// Modullisten und der Eingangsgrößen wie in den einkanaligen Ctrl-Methoden;
        /// danach
        ///
        ///   1. Modulaufbau aller beteiligten Erzeuger samt Zusammenführung mehrfach
        ///      benutzter Quellspeicher,
        ///   2. Übernahme der Quellspeicher in die Registry (sie entstehen erst jetzt),
        ///   3. Öffnen der Registry für den Rechenpfad und Nachziehen der Felder, die
        ///      nur an der Projektkopie stehen,
        ///   4. Aufbau des <see cref="Kaskadenkontext"/> (Entlade- und Ladeordnung),
        ///   5. die gemeinsame Stundenschleife A–G (<see cref="Kaskadenschleife"/>).
        ///
        /// Die Kanäle werden dabei in place fortgeschrieben.
        /// </summary>
        private void Speicherstufe_Rechnen(Waermekanaele kanaele, float[] Strombedarf,
                                           bool bHeizstab, int nBereitschaft)
        {
            WaermequelleClass.SchemaSicherstellen();

            Kaskadenschleife schleife = new Kaskadenschleife();

            // Paket-5-Nacharbeit, Befund N3 (erster Teil): EIGENE KOPIE des
            // Stufeneingangs der Stromseite. Das Wärmepumpen-Modul übernimmt das
            // übergebene Array als sein AUSGABEARRAY (WP_Strombedarf_stuendlich) und
            // nullt es in Init(); wer danach daraus liest, bekommt Nullen. Genau das
            // hatte der Heizkessel getan — Tab_ErgebnisHeizkessel.Strombedarf und
            // .Reststrombedarf standen auf 0 (gemessen 1023: 133,35 -> 0 MWh).
            float[] stromStufeneingang = (float[])Strombedarf.Clone();

            // --- 1. Modulaufbau je beteiligter Erzeugerart ---------------------------
            if (_wpInSchleife)
            {
                WP_Liste_Laden();

                simulation_wp.Temperatur = Stundentemperatur;
                simulation_wp.PV_Ueberschuss_stuendlich = PV_Ueberschuss_Vorabberechnen();
                simulation_wp.WP_Strombedarf_stuendlich = Strombedarf;
                simulation_wp.Mit_Heizstab = bHeizstab;
                // Waermebedarf_stuendlich und Warmwasserbedarf_stuendlich setzt das Modul
                // selbst aus den Kanälen - im zweikanaligen Weg ist der Kanal die Wahrheit,
                // nicht ein vorab zugewiesener Summenvektor.

                m_bError = !simulation_wp.Vorbereiten_Zweikanalig();
                if (m_bError)
                {
                    // Paket 8: wie bei Kessel (N10) und BHKW (N8) dialogfrei melden.
                    // Nacharbeit N11: sammelnd, nicht überschreibend.
                    FehlertextAufnehmen(simulation_wp.Fehlertext);
                    return;
                }

                QuellspeicherUebernehmen(true);
                schleife.WP = simulation_wp;
            }

            if (_solarInSchleife)
            {
                Solar_Liste_Laden();
                if (!simulation_solarthermie.Vorbereiten_Zweikanalig(m_ID_Projekt, senkenzuordnungen))
                {
                    m_bError = true;
                    return;
                }
                schleife.Solar = simulation_solarthermie;
            }

            if (_kesselInSchleife)
            {
                SPK_Liste_Laden();
                // EIGENER Vektor aus der Kopie des Stufeneingangs (N3): Die Wärmepumpe
                // überschreibt den ihren stundenweise (WP_Strombedarf_stuendlich).
                simulation_spk.Strombedarf_stuendlich = (float[])stromStufeneingang.Clone();
                simulation_spk.Vorgabe_Betriebsbereitschaft = nBereitschaft;

                if (!simulation_spk.Vorbereiten_Zweikanalig(m_ID_Projekt, senkenzuordnungen))
                {
                    // N10: Der zweikanalige Weg meldet dialogfrei über den Fehlerkanal.
                    if (!string.IsNullOrEmpty(simulation_spk.Fehlertext))
                        Fehlertext = simulation_spk.Fehlertext;
                    m_bError = true;
                    return;
                }
                schleife.Kessel = simulation_spk;
            }

            if (_bhkwInSchleife)
            {
                BHKW_Liste_Laden();

                // Wie beim Kessel (N3): EIGENER Vektor aus der Kopie des Stufeneingangs.
                simulation_bhkw.strombedarf = (float[])stromStufeneingang.Clone();
                simulation_bhkw.bhkwGrenzleistungAllgemein = GrenzleistungBHKW;
                simulation_bhkw.modeBHKW = modeBHKW;
                // Der skalare Pendelspeicher ist im zweikanaligen Weg abgelöst - die
                // Kapazität kommt aus dem zugeordneten SimulationPufferspeicher
                // (Konzept 6.5, zweiter Punkt). Der Wert bleibt auf 0, damit ein
                // versehentlicher Rückgriff sofort auffiele.
                simulation_bhkw.kapazitaetPendelspeicher = 0f;

                if (!simulation_bhkw.Vorbereiten_Zweikanalig(m_ID_Projekt, senkenzuordnungen))
                {
                    if (!string.IsNullOrEmpty(simulation_bhkw.Fehlertext))
                        Fehlertext = simulation_bhkw.Fehlertext;
                    m_bError = true;
                    return;
                }
                schleife.BHKW = simulation_bhkw;
            }

            // --- 2./3. Registry öffnen ------------------------------------------------
            RegistryFuerZweikanaligOeffnen();

            // --- 4. Kontext (Entlade- und Ladeordnung) --------------------------------
            Kaskadenkontext kontext = KontextAufbauen();

            // PAKET 6: Ein BHKW ohne Puffer-Senke, aber mit Pendelspeichervolumen bekommt
            // seinen Ersatzspeicher - danach, weil erst jetzt feststeht, ob aus seinen
            // Senken ein Ladeauftrag entstanden ist.
            BhkwErsatzspeicherAufnehmen(kontext);

            // PAKET 8 (Konzept 13.4): Die Abgrenzungen des Kontextaufbaus gingen bisher
            // nur auf die Konsole - im Programm sah sie niemand. Sie setzen jetzt auf dem
            // HINWEIS-Kanal auf (der Console.WriteLine steckt in SimulationProtokoll und
            // bleibt damit erhalten) und erscheinen nach dem Lauf in der Detailansicht.
            foreach (string hinweis in kontext.Hinweise) Protokoll.Hinweis(hinweis);

            // Paket-5-Nacharbeit, Befund N5: Eine Puffer-Hauptsenke, aus der kein
            // Ladeauftrag entstanden ist, darf den Erzeuger nicht stillegen.
            PufferSenkenOhneAuftragZurueckfallen(kontext);

            // ETAPPE D5a: Quellbezüge auf Pufferspeicher — sie bestimmen die
            // Rechenreihenfolge (Rechenebenen der Kaskadenschleife) und beim Heizkessel
            // zusätzlich die Eintrittstemperatur.
            QuellbezuegeAufbauen(kontext);

            schleife.Kontext = kontext;
            schleife.Bedarfsreihenfolge = BedarfsreihenfolgeAufbauen();

            // --- 5. Stundenschleife A–G ------------------------------------------------
            m_bError = !schleife.Rechnen(kanaele);

            // D5a: Der Zyklus-Guard der Rechenebenen bricht dialogfrei ab; sein Text
            // gehört in denselben Fehlerkanal wie die übrigen Abbrüche.
            if (m_bError) FehlertextAufnehmen(schleife.Fehlertext);

            // Paket 8: Die Stundenschleife kann an der Kennlinie der Wärmepumpe
            // abbrechen (verbotene Extrapolation). Auch dieser Abbruch bekommt seinen
            // Text, statt nur als m_bError zu erscheinen.
            // Nacharbeit N11: sammelnd - derselbe Weg wie an den übrigen drei Stellen.
            if (m_bError) FehlertextAufnehmen(simulation_wp.Fehlertext);
        }

        /// <summary>
        /// SICHERHEITSNETZ gegen den stillen Totalausfall eines Erzeugers
        /// (Paket-5-Nacharbeit, Befund N5).
        ///
        /// Eine Anlage mit Puffer-HAUPTsenke deckt in Phase B nichts — sie lädt
        /// ausschließlich (Konzept 6.3, Doppelzählungs-Freibeweis). Entsteht aus ihrer
        /// Senkenreferenz aber kein Ladeauftrag, lädt sie auch nicht: Sie produziert das
        /// ganze Jahr nichts, und bis zur Nacharbeit ohne jeden Hinweis (gemessen an
        /// einem präparierten 1018: Kesselproduktion 34,27 -> 0 MWh). Ursachen sind
        /// Konfigurationsfehler, die die Oberfläche nicht verhindert: eine
        /// <c>WS_ID_Puffer</c>, die auf den Puffer eines FREMDEN Projekts zeigt, oder ein
        /// Puffer, den die Registry aus anderen Gründen nicht in den Rechenpfad nimmt.
        /// (Der zweite Fall — Puffer-Ziel ganz OHNE <c>WS_ID_Puffer</c> — wird schon eine
        /// Schicht früher abgefangen, in <c>WaermesenkeClass.Normalisieren</c>.)
        ///
        /// Die Rückfallebene ist der Heizkreis: Die Anlage deckt Bedarf wie eine Anlage
        /// ohne Puffer-Senke. Das ist die konservative Richtung — es entsteht keine
        /// Wärme, die niemand angefordert hat — und es wird protokolliert.
        ///
        /// Die Zuordnungsobjekte sind dieselben Instanzen, mit denen die Module rechnen
        /// (<c>senkenzuordnungen</c> ist die eine Quelle): Die Korrektur wirkt deshalb
        /// auch für Solarthermie und Heizkessel, deren Modulaufbau bereits gelaufen ist.
        /// </summary>
        private void PufferSenkenOhneAuftragZurueckfallen(Kaskadenkontext kontext)
        {
            if (kontext == null) return;

            if (_wpInSchleife)
                for (int i = 0; i < simulation_wp.wp_list.Count && i < kontext.SenkeJeModul.Count; i++)
                    SenkeAufHeizkreisZurueck(kontext, simulation_wp.wp_list[i],
                                             kontext.SenkeJeModul[i], "Wärmepumpe");

            if (_solarInSchleife)
                for (int f = 0; f < simulation_solarthermie.solar_anlagen_ids.Count; f++)
                    SenkeAufHeizkreisZurueck(kontext, simulation_solarthermie.solar_anlagen_ids[f],
                                             simulation_solarthermie.FeldSenke(f), "Solarthermie");

            if (_kesselInSchleife)
                for (int i = 0; i < simulation_spk.spk_anlagen_ids.Count; i++)
                    SenkeAufHeizkreisZurueck(kontext, simulation_spk.spk_anlagen_ids[i],
                                             simulation_spk.KesselSenke(i), "Heizkessel");

            if (_bhkwInSchleife)
                for (int i = 0; i < simulation_bhkw.bhkw_anlagen_ids.Count; i++)
                    SenkeAufHeizkreisZurueck(kontext, simulation_bhkw.bhkw_anlagen_ids[i],
                                             simulation_bhkw.BhkwSenke(i), "BHKW");
        }

        /// <summary>Eine Anlage ohne Ladeauftrag auf die Hauptsenke Heizkreis zurücksetzen.</summary>
        private static void SenkeAufHeizkreisZurueck(Kaskadenkontext kontext, int idAnlage,
                                                     Senkenzuordnung z, string art)
        {
            if (z == null || z.Haupt == Senke.Heizkreis) return;

            foreach (Ladeauftrag a in kontext.LadenOhnePV)
                if (a != null && !a.Zweitsenke && a.AnlagenID == idAnlage) return;

            // Protokollkanal-Nachzug: WARNUNG statt bloßer Konsolenzeile - der Rückfall
            // ist eine Ersatzannahme mit Ergebniswirkung (gemessen an einem präparierten
            // 1018: Kesselproduktion 34,27 -> 0 MWh ohne ihn). Der Schlüssel je Anlage
            // hält die Meldung eindeutig, auch wenn Haupt- und Zweitsenke beide fallen.
            SimulationProtokoll.Aktuell.WarnungEinmal(
                "senke-ohne-ladeauftrag-" + idAnlage,
                "Wärmesenke: Die Anlage " + idAnlage + " (" + art + ") ist als " +
                "Hauptsenke auf " + Senkenzuordnung.ZielAusSenke(z.Haupt) +
                " (Puffer " + z.IDPufferHaupt + ") konfiguriert, bekommt in diesem " +
                "Lauf aber KEINEN Ladeauftrag - der Puffer gehört zu einem anderen " +
                "Projekt oder rechnet nicht mit. Die Anlage deckt deshalb den " +
                "HEIZKREIS; ohne diesen Rückfall würde sie das ganze Jahr nichts " +
                "produzieren.");

            z.Haupt = Senke.Heizkreis;
        }

        /// <summary>
        /// Erzeugerarten der Phase B in KASKADENREIHENFOLGE — nur die Stufen, die in der
        /// gemeinsamen Stundenschleife rechnen (Konzept 6.3, Phase B).
        /// </summary>
        private List<int> BedarfsreihenfolgeAufbauen()
        {
            List<int> reihenfolge = new List<int>();
            if (tool == null) return reihenfolge;

            for (int i = 0; i < 4 && i < tool.Length; i++)
            {
                if (tool[i] == DbWerte.ERZEUGER_WAERMEPUMPE && _wpInSchleife) reihenfolge.Add(ProjektPuffer.TYP_WP);
                else if (tool[i] == DbWerte.ERZEUGER_SOLARTHERMIE && _solarInSchleife) reihenfolge.Add(ProjektPuffer.TYP_SOLARTHERMIE);
                else if (tool[i] == DbWerte.ERZEUGER_HEIZKESSEL && _kesselInSchleife) reihenfolge.Add(ProjektPuffer.TYP_KESSEL);
                else if (tool[i] == DbWerte.ERZEUGER_BHKW && _bhkwInSchleife) reihenfolge.Add(ProjektPuffer.TYP_BHKW);
            }

            return reihenfolge;
        }

        /// <summary>
        /// Anlagenliste der Wärmepumpen in Kaskadenreihenfolge (wie
        /// <see cref="Simulation_WP_Ctrl"/>).
        ///
        /// Paket-5-Nacharbeit, Befund N9: Diese drei Listen-Lader gehören zum NEUEN
        /// Rechenweg und laufen deshalb über den stillen, parametrisierten Zugriff
        /// (<see cref="StilleDb"/>) statt über den Altbestand <c>RecordSet</c>. Der
        /// schluckt SQL-Fehler stillschweigend — die Ursache der Bestandsbefunde
        /// B1-F1/B1-F2 —, und eine leere Modulliste sähe hier aus wie „das Projekt hat
        /// keine Anlagen dieser Art". Die Abfragen sind Wort für Wort dieselben; der
        /// Altpfad bleibt unverändert bei <c>RecordSet</c>, damit er byte-identisch
        /// rechnet.
        /// </summary>
        private void WP_Liste_Laden()
        {
            simulation_wp.wp_list.Clear();

            DataTable dt = StilleDb.Tabelle(
                "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ? " +
                "ORDER BY Prioritaet, ID",
                StilleDb.Par("@proj", OleDbType.Integer, m_ID_Projekt),
                StilleDb.Par("@typ", OleDbType.Integer, WizardItemClass.WP_TYP));

            if (dt == null)
            {
                Protokoll.Warnung("Speicherstufe: Die Wärmepumpen des Projekts " + m_ID_Projekt +
                                  " ließen sich nicht lesen - die Stufe rechnet ohne Module.");
                return;
            }

            foreach (DataRow r in dt.Rows)
                simulation_wp.wp_list.Add(StilleDb.Zahl(StilleDb.Feld(r, "ID")));
        }

        /// <summary>Kesselliste und Anlagen-IDs (wie <see cref="Simulation_SPK_Ctrl"/>); N9 wie oben.</summary>
        private void SPK_Liste_Laden()
        {
            simulation_spk.spk_list.Clear();
            simulation_spk.spk_anlagen_ids.Clear();

            DataTable dt = StilleDb.Tabelle(
                "SELECT Bezeichner, ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                StilleDb.Par("@proj", OleDbType.Integer, m_ID_Projekt),
                StilleDb.Par("@typ", OleDbType.Integer, WizardItemClass.KESSEL_TYP));

            if (dt == null)
            {
                Protokoll.Warnung("Speicherstufe: Die Heizkessel des Projekts " + m_ID_Projekt +
                                  " ließen sich nicht lesen - die Stufe rechnet ohne Module.");
                return;
            }

            foreach (DataRow r in dt.Rows)
            {
                simulation_spk.spk_list.Add(StilleDb.Text(StilleDb.Feld(r, "Bezeichner")));
                simulation_spk.spk_anlagen_ids.Add(StilleDb.Zahl(StilleDb.Feld(r, "ID")));
            }
        }

        /// <summary>Kollektorliste (wie <see cref="Simulation_Solarthermie_Ctrl"/>); N9 wie oben.</summary>
        private void Solar_Liste_Laden()
        {
            simulation_solarthermie.solarthermie_list.Clear();

            DataTable dt = StilleDb.Tabelle(
                "SELECT ID_SOLAR FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                StilleDb.Par("@proj", OleDbType.Integer, m_ID_Projekt),
                StilleDb.Par("@typ", OleDbType.Integer, WizardItemClass.SOLAR_TYP));

            if (dt == null)
            {
                Protokoll.Warnung("Speicherstufe: Die Kollektorfelder des Projekts " + m_ID_Projekt +
                                  " ließen sich nicht lesen - die Stufe rechnet ohne Module.");
                return;
            }

            foreach (DataRow r in dt.Rows)
                simulation_solarthermie.solarthermie_list.Add(StilleDb.Zahl(StilleDb.Feld(r, "ID_SOLAR")));
        }

        /// <summary>
        /// BHKW-Liste und Anlagen-IDs — seit PAKET BHKW-REGULÄR der EINZIGE Ladeweg der
        /// BHKW-Module. Er übernimmt die Abfrage des entfallenen einkanaligen
        /// <c>Simulation_BHKW_Ctrl</c>, nur dialogfrei und parametrisiert über
        /// <see cref="StilleDb"/> (Befund N9 der Paket-5-Nacharbeit) statt über
        /// <c>RecordSet</c>. Ohne <c>ORDER BY</c>, damit die Modulreihenfolge dieselbe
        /// bleibt wie bisher.
        ///
        /// <c>bhkwGrenzL</c> wird hier aus der ANLAGE vorbelegt (Prozentwert / 100);
        /// <c>SimulationBHKW.Moduldaten_Einlesen</c> überschreibt den Wert anschließend
        /// aus dem Katalog, sofern dort eine Grenzleistung hinterlegt ist. Der KATALOGWERT
        /// wird dort seit dem Einheiten-Fix dieses Pakets ebenfalls durch 100 geteilt -
        /// vorher trug er als Prozentzahl (z. B. 50) in eine Formel ein, die einen Faktor
        /// erwartet (0,5). Siehe die Begründung in <c>Moduldaten_Einlesen</c>.
        /// </summary>
        private void BHKW_Liste_Laden()
        {
            simulation_bhkw.bhkw_list.Clear();
            simulation_bhkw.bhkw_list_Namen.Clear();
            simulation_bhkw.bhkw_anlagen_ids.Clear();

            DataTable dt = StilleDb.Tabelle(
                "SELECT ID_BHKW, ID, Bezeichner, Grenzleistung FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND ID_Type = ?",
                StilleDb.Par("@proj", OleDbType.Integer, m_ID_Projekt),
                StilleDb.Par("@typ", OleDbType.Integer, WizardItemClass.BHKW_TYP));

            if (dt == null)
            {
                Protokoll.Warnung("Speicherstufe: Die BHKW des Projekts " + m_ID_Projekt +
                                  " ließen sich nicht lesen - die Stufe rechnet ohne Module.");
                return;
            }

            int i = 0;
            foreach (DataRow r in dt.Rows)
            {
                simulation_bhkw.bhkw_list.Add(StilleDb.Zahl(StilleDb.Feld(r, "ID_BHKW")));
                simulation_bhkw.bhkw_anlagen_ids.Add(StilleDb.Zahl(StilleDb.Feld(r, "ID")));
                simulation_bhkw.bhkw_list_Namen.Add(StilleDb.Text(StilleDb.Feld(r, "Bezeichner")));

                if (i < SimulationBHKW.MAX_BHKW)
                    simulation_bhkw.bhkwGrenzL[i] =
                        (float)(StilleDb.Kommazahl(StilleDb.Feld(r, "Grenzleistung")) / 100.0);
                i++;
            }
        }

        /// <summary>
        /// BHKW als zweikanalige VEKTORSTUFE (Paket 6): eigene Jahresschleife an der
        /// Kaskadenposition, ohne Speicherbeteiligung.
        ///
        /// Sie ersetzt den bisherigen Kompatibilitätsanker (einkanalig auf
        /// <c>Waermekanaele.Summe()</c> mit proportionaler Rückverteilung über
        /// <c>Uebernehmen()</c>). Zwei Dinge ändern sich damit auch ohne Speicher: Das
        /// BHKW deckt seinen Kanal nach <c>WS_Typ</c>, und der Restbedarf ist der
        /// tatsächliche Rest statt der Vektordifferenz <c>Bedarf − Produktion</c>
        /// (Bilanzfehler aus Konzept 6.5 / 2.2, Punkt 8).
        /// </summary>
        private void Simulation_BHKW_Ctrl_Zweikanalig(Waermekanaele kanaele, float[] Strombedarf)
        {
            BHKW_Liste_Laden();

            simulation_bhkw.strombedarf = Strombedarf;
            simulation_bhkw.bhkwGrenzleistungAllgemein = GrenzleistungBHKW;
            simulation_bhkw.modeBHKW = modeBHKW;
            simulation_bhkw.kapazitaetPendelspeicher = 0f;

            if (!simulation_bhkw.Berechnung_Zweikanalig(m_ID_Projekt, kanaele, senkenzuordnungen) &&
                !string.IsNullOrEmpty(simulation_bhkw.Fehlertext))
                Fehlertext = simulation_bhkw.Fehlertext;
        }

        /// <summary>
        /// ERSATZ-PENDELSPEICHER des BHKW (Paket 6, Konzept 6.5 zweiter Punkt).
        ///
        /// AUFLÖSUNGSKETTE des BHKW-Speichers im neuen Weg:
        ///
        ///   1. Puffer-Senke der BHKW-Anlage (<c>WS_Ziel</c>/<c>WS_ID_Puffer</c> bzw.
        ///      <c>WS_Ziel2</c>/<c>WS_ID_Puffer2</c>) → Registry-Speicher, Ladeauftrag über
        ///      die Ladeordnung (Vorgaberang 30). Das ist derselbe Weg wie bei Wärmepumpe,
        ///      Solarthermie und Heizkessel und der Regelfall auf migrierten Datenbanken:
        ///      Migrationsregel R6 legt zum Pendelspeicher IMMER auch die Senke an.
        ///   2. KEINE Puffer-Senke, aber ein Pendelspeichervolumen (die Puffer-Zeile
        ///      „BHKW-Pendelspeicher") → dieser Speicher wird HIER aufgenommen und bekommt
        ///      einen Ladeauftrag als ZWEITsenke. Damit arbeitet das BHKW wie bisher mit
        ///      seinem Pendelspeicher, aber über dieselbe Speicherphysik wie alle anderen
        ///      Erzeuger: Hysterese, Bereitschaftsverluste, Kapazität aus der
        ///      ΔT-Spreizung, Phase A/E-Entladung, Phase G und Herkunftsrechnung.
        ///   3. Weder noch → kein Speicher; das BHKW deckt nur den Momentanbedarf.
        ///
        /// Die Zweitsenken-Rolle ist die fachlich richtige: Der Pendelspeicher nimmt auf,
        /// was über den Momentanbedarf hinaus entsteht — genau die Definition der
        /// Zweitsenke aus Konzept E2.
        /// </summary>
        private void BhkwErsatzspeicherAufnehmen(Kaskadenkontext k)
        {
            if (!_bhkwInSchleife || k == null) return;
            if (VolumenPendelspeicherBHKW <= 0) return;

            foreach (Ladeauftrag vorhanden in k.LadenOhnePV)
                if (vorhanden != null && vorhanden.Erzeugerart == ProjektPuffer.TYP_BHKW) return;

            int idPuffer = PufferSpCtrl.PendelspeicherId(m_ID_Projekt);
            if (idPuffer <= 0)
            {
                // NACHARBEIT PAKET 6, BEFUND N8: Über den FEHLERKANAL statt als bloße
                // Konsolenzeile. Das Volumen stammt aus genau dieser Puffer-Zeile
                // (PufferSpCtrl.PendelspeicherVolumenLiter) — fehlt sie trotzdem, ist die
                // Datenlage in sich widersprüchlich, und das BHKW verlöre stillschweigend
                // seinen Speicher. Der Lauf bricht deshalb ab, statt ein plausibel
                // aussehendes, aber falsches Ergebnis zu speichern.
                // Der Bezeichner des Pendelspeichers ist ein PERSISTENZWERT und bleibt
                // als solcher in der Meldung stehen - er benennt die gesuchte Zeile.
                Fehlertext = string.Format(MyResource.Resource.SIMENG_PENDELSPEICHER_ZEILE_FEHLT,
                                           m_ID_Projekt, VolumenPendelspeicherBHKW,
                                           ProjektPuffer.BEZ_PENDELSPEICHER);
                // NACHARBEIT PAKET 8, BEFUND N9: über den Fehlerkanal (Konsolenzeile
                // bleibt, zusätzlich Lauf-Protokoll und Sammelanzeige).
                Protokoll.Fehlermeldung(Fehlertext);
                m_bError = true;
                return;
            }

            WaermesenkeClass.PufferInfo p = WaermesenkeClass.PufferLesen(idPuffer);
            if (p == null || p.ID_Projekt != m_ID_Projekt)
            {
                Fehlertext = string.Format(MyResource.Resource.SIMENG_PENDELSPEICHER_NICHT_LESBAR,
                                           idPuffer, m_ID_Projekt);
                // NACHARBEIT PAKET 8, BEFUND N9: siehe oben - Fehlerkanal statt blanker
                // Konsolenzeile.
                Protokoll.Fehlermeldung(Fehlertext);
                m_bError = true;
                return;
            }

            // TEMPERATUR-VORRANGKETTE wie für jeden anderen Registry-Speicher
            // (Nacharbeit Paket 6, Befund N2): Projektkopie zuerst, dann die
            // Zuordnungszeile Z_ProjektPufferSp, erst danach der Rückfall. Bis dahin
            // wertete dieser Weg NUR die Projektkopie aus — derselbe Puffer bekam über
            // die Registry ein anderes Q_max als hier (gemessen an 1018: 70/55 °C über
            // die Z-Zeile gegen den 10-K-Notnagel).
            int vorlauf = p.Vorlauf;
            int ruecklauf = p.Ruecklauf;
            if (vorlauf - ruecklauf <= 0)
            {
                int vZuordnung, rZuordnung;
                if (ZuordnungsTemperaturen(_pspZuordnungen, p.ID, p.Bezeichner,
                                           out vZuordnung, out rZuordnung))
                {
                    vorlauf = vZuordnung;
                    ruecklauf = rZuordnung;
                    Protokoll.HinweisEinmal("pendelspeicher-temp-zuordnung-" + p.ID,
                                      "BHKW-Pendelspeicher: Puffer " + p.ID + " (" + p.Bezeichner +
                                      ") hat kein Temperaturpaar in der Projektkopie - es gilt " +
                                      "die Zuordnungszeile (" + vorlauf + "/" + ruecklauf + " °C).");
                }
            }

            SimulationPufferspeicher sp;
            if (!speicherRegistry.TryGetValue(idPuffer, out sp) || sp == null)
            {
                sp = new SimulationPufferspeicher();
                sp.Bezeichner = p.Bezeichner;
                sp.Erzeuger = "BHKW";
                sp.ID_Pufferspeicher = p.ID;
                sp.ID_Projekt = p.ID_Projekt;
                sp.Verwendung = WaermesenkeClass.WirksameVerwendung(p);

                // RÜCKFALL 20 K statt 10 K (Befund N2): Die Altformel
                // „Liter · 20 / 860" hatte für den Pendelspeicher eine Spreizung von
                // 20 K fest verdrahtet. Bleibt sie ungepflegt, ist 20 K deshalb der
                // wertgleiche Ersatz (1,16 gegen 1,16279 Wh/(l·K), −0,24 %); der
                // generische 10-K-Notnagel würde die Kapazität ohne fachlichen Grund
                // halbieren. Für alle anderen Puffer bleibt es bei 10 K.
                sp.Init(p.Gesamtvolumen, vorlauf, ruecklauf, p.Bereitschaftsverluste, 20);
                RueckfallMelden(sp, p.ID, p.Bezeichner);

                sp.SchwelleEin = p.SchwelleEin / 100.0;
                sp.SchwelleAus = p.SchwelleAus / 100.0;
                sp.SchwelleAusNachrang = p.SchwelleAusNachrang / 100.0;
                sp.Entladeprio = p.Entladeprio;

                // PAKET BHKW-REGULÄR: Mindestfüllstand/Notreserve auch auf dem
                // Ersatz-Pendelspeicher-Weg. Er ist der Speicher, für den die Reserve
                // fachlich gedacht ist - ein Puffer, den ausschließlich das BHKW bedient.
                sp.SchwelleReserve = p.SchwelleReserve / 100.0;
                SpeicherAufnehmen(sp, true);
            }

            sp.ImRechenpfad = true;
            if (!k.AlleSpeicher.Contains(sp)) k.AlleSpeicher.Add(sp);

            // ENTLADEREIHENFOLGE (Befund N7): einsortieren statt anhängen. Ein Puffer mit
            // gepflegter Entladeprio gehört an seine Stelle in der Ordnung des Kanals
            // (Konzept 3.6) — dieselbe Reihenfolge, die die Pufferverwaltung anzeigt.
            //
            // NACHARBEIT I-K2-1: über BedientKanal, nicht über IstBrauchwasserkanal. Ein
            // KOMBISPEICHER als Ersatz-Pendelspeicher gehört in BEIDE Kanallisten; über
            // IstBrauchwasserkanal (für ihn false) landete er nur im Heizkanal, und die
            // Warmwasserhälfte fiel still aus. Für jeden anderen Speicher ist genau eine
            // der beiden Bedingungen wahr - dieselbe Einsortierung wie zuvor.
            if (sp.BedientKanal(false)) EntladeordnungEinsortieren(k.Entladeordnung(false), sp, false);
            if (sp.BedientKanal(true)) EntladeordnungEinsortieren(k.Entladeordnung(true), sp, true);

            Ladeauftrag a = new Ladeauftrag();
            a.Modulindex = 0;
            a.Erzeugerart = ProjektPuffer.TYP_BHKW;
            a.AnlagenID = simulation_bhkw.FuehrendeAnlage;
            a.Zweitsenke = true;
            a.Speicher = sp;
            a.Ladeprio = Ladeordnung.PRIO_BHKW;
            a.BMTyp = "";

            // OBERGRENZEN (Befund N7): über die Auflösungsregel 3.4 statt fest auf
            // Schwelle_Aus. Am Pendelspeicher lädt zwar nur das BHKW — dann ist es die
            // vorrangige Anlage und bekommt ohnehin Schwelle_Aus —, aber eine EIGENE
            // Ladegrenze (WS_Ladegrenze) galt bisher nicht, und in PV-Stunden wäre die
            // zweite Auflösung ausgefallen. Engine und Anzeige benutzen jetzt dieselbe
            // Quelle wie bei allen anderen Speichern.
            ObergrenzenFuerErsatzspeicher(a, sp);

            LadeauftragEinsortieren(k.LadenOhnePV, a);
            LadeauftragEinsortieren(k.LadenMitPV, a);

            Protokoll.Hinweis("BHKW-Pendelspeicher: Keine Puffer-Senke am BHKW - der Speicher „" +
                              sp.BezeichnerAnzeige() + "\" (" + p.Gesamtvolumen + " l, " +
                              vorlauf + "/" + ruecklauf + " °C, Q_max " +
                              sp.Q_max.ToString("0.###") + " kWh, Entladeprio " + sp.Entladeprio +
                              ", Obergrenze " + (a.Obergrenze * 100).ToString("0.#") + " % / mit PV " +
                              (a.ObergrenzePV * 100).ToString("0.#") + " %) rechnet als ZWEITSENKE " +
                              "mit. Der skalare Pendelspeicher des Altpfads ist damit abgelöst.");
        }

        /// <summary>
        /// Setzt einen Speicher an SEINE Stelle in der Entladereihenfolge des Kanals
        /// (Konzept 3.6, Nacharbeit Paket 6 Befund N7).
        ///
        /// Grundlage ist dieselbe Liste, aus der <see cref="EntladeordnungAufbauen"/> die
        /// Ordnung bildet — <c>Ladeordnung.Entladereihenfolge</c>. Steht der Speicher
        /// dort nicht (Projektzuordnung inkonsistent), kommt er ans Ende; das ist das
        /// Verhalten des Sicherheitsnetzes im Kontextaufbau.
        /// </summary>
        /// <param name="brauchwasser">
        /// Kanal, in den einsortiert wird (Nacharbeit I-K2-1). Er bestimmt zugleich, gegen
        /// welche SOLL-Ordnung die Position gesucht wird; beim Kombispeicher wird die
        /// Methode je Kanal einmal gerufen.
        /// </param>
        private void EntladeordnungEinsortieren(List<SimulationPufferspeicher> ordnung,
                                                SimulationPufferspeicher sp, bool brauchwasser)
        {
            if (ordnung == null || sp == null || ordnung.Contains(sp)) return;

            string verwendung = brauchwasser
                ? WaermesenkeClass.VERWENDUNG_BRAUCHWASSER : WaermesenkeClass.VERWENDUNG_HEIZUNG;

            List<Ladeordnung.EntladeEintrag> soll =
                Ladeordnung.Entladereihenfolge(m_ID_Projekt, verwendung);

            int platz = Ladeordnung.Position(soll, sp.ID_Pufferspeicher);
            if (platz <= 0)
            {
                Protokoll.HinweisEinmal("pendelspeicher-entladeordnung-" + verwendung + "-" +
                                  sp.ID_Pufferspeicher,
                                  "BHKW-Pendelspeicher: Der Speicher " + sp.ID_Pufferspeicher +
                                  " steht nicht in der Entladereihenfolge des Kanals " + verwendung +
                                  " - er wird ans Ende gestellt.");
                ordnung.Add(sp);
                return;
            }

            // Vor den ersten Speicher stellen, der in der SOLL-Ordnung hinter ihm steht.
            for (int i = 0; i < ordnung.Count; i++)
            {
                int p = Ladeordnung.Position(soll, ordnung[i].ID_Pufferspeicher);
                if (p > platz) { ordnung.Insert(i, sp); return; }
            }

            ordnung.Add(sp);
        }

        /// <summary>
        /// Löst die Ladeobergrenzen des Ersatz-Pendelspeichers nach Konzept 3.4/3.5 auf
        /// (Nacharbeit Paket 6, Befund N7) — mit derselben Funktion, die die
        /// Pufferverwaltung anzeigt und die <see cref="LadeordnungAufbauen"/> benutzt.
        ///
        /// Der Pendelspeicher hat oft gar keinen Ladeeintrag (er entsteht ja gerade,
        /// WEIL keine Senke auf ihn zeigt). Dann bleibt es bei <c>Schwelle_Aus</c> des
        /// Speichers — das ist derselbe Wert, den <c>ObergrenzenAufloesen</c> der
        /// vorrangigen Anlage zuweisen würde.
        /// </summary>
        private void ObergrenzenFuerErsatzspeicher(Ladeauftrag a, SimulationPufferspeicher sp)
        {
            a.Obergrenze = sp.SchwelleAus;
            a.ObergrenzePV = sp.SchwelleAus;

            List<Ladeordnung.LadeEintrag> eintraege =
                Ladeordnung.Ladereihenfolge(m_ID_Projekt, sp.ID_Pufferspeicher);
            if (eintraege == null || eintraege.Count == 0) return;

            Ladeordnung.LadeEintrag eigen = null;
            foreach (Ladeordnung.LadeEintrag e in eintraege)
                if (e.ID_Anlage == a.AnlagenID && e.Zweitsenke == a.Zweitsenke) { eigen = e; break; }
            if (eigen == null) return;

            // Ladereihenfolge hat die Obergrenzen ohne PV bereits aufgelöst.
            a.Obergrenze = eigen.Obergrenze / 100.0;
            a.Ladeprio = eigen.Ladeprio;

            // Zweite Auflösung für Stunden MIT PV-Überschuss (Konzept 3.5).
            Ladeordnung.ObergrenzenAufloesen(eintraege, sp.ID_Pufferspeicher,
                delegate (Ladeordnung.LadeEintrag e)
                {
                    return Ladeordnung.WirksameLadeprioPV(
                        e.ID_Type, e.Ladeprio, e.LadeprioPV,
                        BetriebsmodusDerAnlage(e.ID_Anlage), true);
                });
            a.ObergrenzePV = eigen.Obergrenze / 100.0;
        }

        /// <summary>Fügt einen Ladeauftrag an der Stelle seiner Ladepriorität ein (Konzept 3.4).</summary>
        private static void LadeauftragEinsortieren(List<Ladeauftrag> liste, Ladeauftrag a)
        {
            if (liste == null || a == null) return;

            for (int i = 0; i < liste.Count; i++)
                if (liste[i] != null && liste[i].Ladeprio > a.Ladeprio) { liste.Insert(i, a); return; }

            liste.Add(a);
        }

        /// <summary>
        /// Heizkessel als zweikanalige VEKTORSTUFE (Paket 5): eigene Jahresschleife an der
        /// Kaskadenposition, ohne Speicherbeteiligung.
        /// </summary>
        private void Simulation_SPK_Ctrl_Zweikanalig(Waermekanaele kanaele, float[] Strombedarf,
                                                     int nBereitschaft)
        {
            SPK_Liste_Laden();

            simulation_spk.Strombedarf_stuendlich = Strombedarf;
            simulation_spk.Vorgabe_Betriebsbereitschaft = nBereitschaft;

            if (!simulation_spk.Berechnung_Zweikanalig(m_ID_Projekt, kanaele, senkenzuordnungen) &&
                !string.IsNullOrEmpty(simulation_spk.Fehlertext))
                Fehlertext = simulation_spk.Fehlertext;   // N10: dialogfrei melden
        }

        /// <summary>
        /// Solarthermie als zweikanalige VEKTORSTUFE (Paket 5): eigene Jahresschleife an
        /// der Kaskadenposition, ohne Speicherbeteiligung.
        /// </summary>
        private void Simulation_Solarthermie_Ctrl_Zweikanalig(Waermekanaele kanaele)
        {
            Solar_Liste_Laden();

            simulation_solarthermie.Berechnung_Zweikanalig(m_ID_Projekt, kanaele, senkenzuordnungen);
        }

        /// <summary>
        /// Öffnet den Rechenpfad für die Speicher, die im zweikanaligen Weg wirklich
        /// arbeiten können, und zieht die Felder nach, die nur an der Projektkopie stehen.
        ///
        /// KRITERIUM (nachgeschärft in der Paket-4-Review, Befund B2-b): Es rechnet, was
        /// eine Anlage als SENKE führt (<c>WS_ID_Puffer</c>, <c>WS_ID_Puffer2</c> — die
        /// Referenzen, aus denen auch <c>Ladeordnung.Ladereihenfolge</c> die Ladeaufträge
        /// bildet) oder was QUELLE einer Wärmepumpe ist (<c>WQ_ID_Puffer</c>).
        ///
        /// Die erste Fassung öffnete stattdessen ALLE Registry-Einträge — mit der
        /// Begründung aus Konzept 6.7 („ab Etappe 4b rechnen alle Registry-Speicher
        /// mit"). Das ging zu weit: In die Registry kommt auch, was nur über die
        /// Alt-Zuordnung <c>Z_ProjektPufferSp</c> am Projekt hängt (Projekt 1007 aus einer
        /// Solarthermie-Zuordnung, 1011 aus „Gesamtsystem", 1018 aus einer
        /// BHKW-Zuordnung). Solche Speicher kann in diesem Rechenweg niemand laden — es
        /// gibt keinen Ladeauftrag für sie. Sie erschienen dann mit lauter Nullen in
        /// <c>Tab_ErgebnisPufferspeicher</c>, und über <see cref="ErsterHeizpuffer"/>
        /// meldete <c>puffer_wp</c> eine Speicherkapazität
        /// (<c>Kapazitaet_Pufferspeicher</c>), die kein Erzeuger benutzt. Mit dem engeren
        /// Kriterium verschwindet diese Ergebnisänderung: 1007 meldet mit gesetztem Flag
        /// wieder <c>PufferWP_vorhanden = False</c>, genau wie im Altpfad.
        ///
        /// <see cref="SimulationPufferspeicher.ImRechenpfad"/> bleibt damit auch im
        /// zweikanaligen Weg eine echte Unterscheidung — und der Altpfad, der es aus
        /// derselben Registry liest, bleibt unangetastet.
        ///
        /// Nachgezogen werden nur <c>Schwelle_Aus_Nachrang</c> und <c>Entladeprio</c>:
        /// Die Alt-Zuordnung <c>Z_ProjektPufferSp</c> kennt diese Spalten nicht, und ihr
        /// Speicher bekäme sonst die verhaltensneutrale Vorbelegung, obwohl am Puffer
        /// eine Reservezone gepflegt ist. Ein-/Abschaltschwelle bleiben ausdrücklich, wie
        /// die Registry sie aufgebaut hat — sie sind die Parameterquelle, mit der auch
        /// der Altpfad rechnet.
        /// </summary>
        private void RegistryFuerZweikanaligOeffnen()
        {
            List<int> senken = SenkenPufferDerAnlagen();

            // PAKET PARALLELVERBUND — das Sicherheitsnetz gegen DOPPELZÄHLUNG.
            //
            // Ein Verbundmitglied hat keinen eigenen Füllstand mehr: Seine Kapazität steckt
            // seit VerbundAufaddieren im Q_max des Leitspeichers. Käme es zusätzlich als
            // eigenes Rechenobjekt in den Rechenpfad, zählte dieselbe Kapazität zweimal,
            // und die Bilanz des Laufs wäre falsch, ohne dass es irgendwo auffiele.
            //
            // Im Regelfall ist das gar nicht möglich: Ein Mitglied steht in keiner
            // WS_ID_Puffer-Referenz und kommt schon über ReferenzierteSenkenPuffer nicht in
            // die Registry. Der Fall entsteht nur bei von HAND gepflegten Beständen (SQL
            // direkt in der Datenbank) - der Dialog verhindert ihn beim Speichern.
            // Als ANZEIGEOBJEKT (ImRechenpfad = false) bleibt der Speicher zulässig, genau
            // wie jeder andere referenzierte Puffer ohne Ladeauftrag.
            List<int> verbundMitglieder = AnlagePufferVerbundCtrl.MitgliederDesProjekts(m_ID_Projekt);

            foreach (int id in _speicherReihenfolge)
            {
                SimulationPufferspeicher sp;
                if (!speicherRegistry.TryGetValue(id, out sp) || sp == null) continue;

                if (sp.IstQuelle) { sp.ImRechenpfad = true; continue; }

                if (verbundMitglieder.Contains(id))
                {
                    Protokoll.WarnungEinmal("verbund-mitglied-eigenes-ziel-" + id,
                                      "Parallelverbund: Puffer " + id + " (" + sp.BezeichnerAnzeige() +
                                      ") ist Verbundmitglied UND wird von einer Anlage als " +
                                      "eigenständige Senke geführt. Seine Kapazität steckt " +
                                      "bereits im Leitspeicher des Verbunds; als eigenes " +
                                      "Rechenobjekt zählte sie doppelt. Er rechnet deshalb " +
                                      "NICHT eigenständig mit - bitte die Wärmesenke der " +
                                      "betreffenden Anlage berichtigen.");
                    sp.ImRechenpfad = false;
                    continue;
                }

                // Ausdrücklich ZUWEISEN, nicht nur setzen: Der Senkenspeicher aus der
                // Alt-Zuordnung trägt das Flag schon aus dem Registry-Aufbau. Ohne
                // Senkenreferenz gehört er im zweikanaligen Weg trotzdem nicht in den
                // Rechenpfad — ihn lädt hier niemand.
                bool referenziert = senken.Contains(id);
                if (sp.ImRechenpfad && !referenziert)
                    Protokoll.WarnungEinmal("registry-ohne-senkenreferenz-" + id,
                                      "Speicher-Registry: Puffer " + id + " (" + sp.BezeichnerAnzeige() +
                                      ") hat keine Senkenreferenz einer Anlage (WS_ID_Puffer/" +
                                      "WS_ID_Puffer2) - er rechnet im zweikanaligen Weg nicht mit.");
                sp.ImRechenpfad = referenziert;

                if (!referenziert) continue;

                WaermesenkeClass.PufferInfo p = WaermesenkeClass.PufferLesen(id);
                if (p == null) continue;

                if (p.SchwelleAusNachrang > 0) sp.SchwelleAusNachrang = p.SchwelleAusNachrang / 100.0;
                sp.Entladeprio = p.Entladeprio;
            }

            // Speicherzeile ohne gültige ID (Rückfallebene des Registry-Aufbaus): Sie
            // kann keine Senkenreferenz tragen - ein Fremdschlüssel zeigt nie auf 0 -,
            // ist aber der Puffer, mit dem der Altpfad dieses Projekt rechnet. Er bleibt
            // deshalb im Rechenpfad, damit der zweikanalige Weg dort nicht weniger
            // abbildet als der einkanalige.
            if (_pufferOhneRegistrySchluessel != null)
                _pufferOhneRegistrySchluessel.ImRechenpfad = true;
        }

        /// <summary>
        /// Baut den <see cref="Kaskadenkontext"/> des Laufs: Speichermenge für Phase G,
        /// Entladereihenfolge je Kanal (Konzept 3.6), kaskadenübergreifende Ladeordnung
        /// (3.4) in ihren beiden Ausprägungen (mit und ohne PV-Überschuss, 3.5) und die
        /// Senkenzuordnung je Modul (3.1).
        ///
        /// Gerechnet wird hier nichts, was <see cref="Ladeordnung"/> schon kann: Die
        /// Reihenfolgen und die aufgelösten Obergrenzen kommen von dort. Damit zeigen die
        /// Dialoge (4.2/4.3) und die Engine dieselbe Ordnung — genau das verlangt Konzept
        /// 3.4 („Die Anzeige ist die maßgebliche Kontrollinstanz").
        /// </summary>
        private Kaskadenkontext KontextAufbauen()
        {
            Kaskadenkontext k = new Kaskadenkontext();
            k.ID_Projekt = m_ID_Projekt;

            // --- 1. Speichermenge des Laufs (Phase G) --------------------------------
            k.AlleSpeicher.AddRange(RegistrySpeicher());

            // --- 2. Entladereihenfolge je Kanal (Konzept 3.6) ------------------------
            k.EntladenHeizung = EntladeordnungAufbauen(k, WaermesenkeClass.VERWENDUNG_HEIZUNG);
            k.EntladenBrauchwasser = EntladeordnungAufbauen(k, WaermesenkeClass.VERWENDUNG_BRAUCHWASSER);

            // --- 3. Senkenzuordnung je WP-Modul (Konzept 3.1) ------------------------
            for (int index = 0; index < simulation_wp.wp_list.Count; index++)
            {
                int idAnlage = simulation_wp.wp_list[index];
                Senkenzuordnung gefunden = null;
                foreach (Senkenzuordnung z in senkenzuordnungen)
                    if (z != null && z.AnlagenID == idAnlage) { gefunden = z; break; }

                // Ohne Zuordnungszeile gilt die Vorbelegung: Heizkreis, Bedarfsart Beides.
                if (gefunden == null) gefunden = new Senkenzuordnung { AnlagenID = idAnlage };
                k.SenkeJeModul.Add(gefunden);
            }

            // --- 4. Ladeaufträge (Konzept 3.4/3.5) -----------------------------------
            LadeordnungAufbauen(k);

            return k;
        }

        /// <summary>
        /// Registry-Speicher in Aufnahmereihenfolge, beschränkt auf die, die im Lauf
        /// tatsächlich rechnen.
        /// </summary>
        private List<SimulationPufferspeicher> RegistrySpeicher()
        {
            List<SimulationPufferspeicher> liste = new List<SimulationPufferspeicher>();

            foreach (int id in _speicherReihenfolge)
            {
                SimulationPufferspeicher sp;
                if (!speicherRegistry.TryGetValue(id, out sp) || sp == null) continue;
                if (!sp.ImRechenpfad) continue;
                if (!liste.Contains(sp)) liste.Add(sp);
            }

            if (_pufferOhneRegistrySchluessel != null &&
                _pufferOhneRegistrySchluessel.ImRechenpfad &&
                !liste.Contains(_pufferOhneRegistrySchluessel))
                liste.Add(_pufferOhneRegistrySchluessel);

            // Speicher ohne freien Registry-Schlüssel (Kurzschluss Quelle = Senke) —
            // sie rechnen mit und müssen deshalb auch in Phase G und in der Persistenz
            // auftauchen.
            foreach (SimulationPufferspeicher sp in _zusatzSpeicher)
                if (sp != null && sp.ImRechenpfad && !liste.Contains(sp)) liste.Add(sp);

            return liste;
        }

        /// <summary>
        /// Die Senkenspeicher EINES Kanals in Entladereihenfolge (Konzept 3.6).
        ///
        /// Grundlage ist <see cref="Ladeordnung.Entladereihenfolge"/> — dieselbe
        /// Reihenfolge, die die Pufferverwaltung anzeigt. Aufgenommen wird nur, was in
        /// der Registry steht und wirklich ein SENKENspeicher ist: Ein Quellspeicher
        /// trägt in der Projektkopie oft keine Verwendung und zählte damit als
        /// Heizungspuffer — er würde sonst seinen Vorrat an den Heizkreis abgeben,
        /// obwohl er die Wärmequelle der Wärmepumpe ist.
        /// </summary>
        private List<SimulationPufferspeicher> EntladeordnungAufbauen(Kaskadenkontext k, string verwendung)
        {
            List<SimulationPufferspeicher> liste = new List<SimulationPufferspeicher>();
            bool brauchwasser = string.Equals(verwendung, WaermesenkeClass.VERWENDUNG_BRAUCHWASSER,
                                              StringComparison.Ordinal);

            foreach (Ladeordnung.EntladeEintrag e in
                     Ladeordnung.Entladereihenfolge(m_ID_Projekt, verwendung))
            {
                SimulationPufferspeicher sp;
                if (!speicherRegistry.TryGetValue(e.ID_Puffer, out sp) || sp == null) continue;
                if (!sp.ImRechenpfad || sp.IstQuelle) continue;
                // D5a: BedientKanal statt IstBrauchwasserkanal - ein KOMBISPEICHER steht
                // in BEIDEN Entladereihenfolgen, je Kanal an der Stelle seiner
                // Entladepriorität. Für jeden anderen Speicher ist die Frage dieselbe wie
                // zuvor.
                if (!sp.BedientKanal(brauchwasser)) continue;
                if (!liste.Contains(sp)) liste.Add(sp);
            }

            // Sicherheitsnetz: ein Registry-Speicher dieses Kanals, den die
            // Entladereihenfolge nicht kennt (Projektzuordnung inkonsistent), fiele sonst
            // stillschweigend aus der Bilanz. Er kommt ans Ende - Reihenfolge der Aufnahme.
            foreach (SimulationPufferspeicher sp in k.AlleSpeicher)
            {
                if (sp == null || sp.IstQuelle) continue;
                if (!sp.BedientKanal(brauchwasser)) continue;      // D5a: Kombi in beiden
                if (liste.Contains(sp)) continue;

                Protokoll.HinweisEinmal("entladeordnung-nachtrag-" + verwendung + "-" +
                                  sp.ID_Pufferspeicher,
                                  "Speicher " + sp.ID_Pufferspeicher + " (" + sp.BezeichnerAnzeige() +
                                  ") steht nicht in der Entladereihenfolge des Kanals " + verwendung +
                                  " - er wird ans Ende gestellt.");
                liste.Add(sp);
            }

            return liste;
        }

        /// <summary>
        /// Kaskadenübergreifende Ladeordnung (Konzept 6.3 C/D) in ihren beiden
        /// Ausprägungen: für Stunden ohne und mit PV-Überschuss (3.5).
        ///
        /// SEIT PAKET 6 sind ALLE VIER Erzeugerarten aufnahmefähig — Wärmepumpe,
        /// Solarthermie, Heizkessel und BHKW. Ein Ladeauftrag entsteht, sobald die Anlage
        /// als Modul in der gemeinsamen Speicherstufe rechnet; steht ihre Art in diesem
        /// Lauf außerhalb der Stufe (Vektorstufe ohne Speicherbeteiligung), ruht die
        /// Senke und wird protokolliert. Das ist die konservative Richtung: Es entsteht
        /// keine Wärme, die niemand anfordert, und keine Doppelzählung.
        /// </summary>
        private void LadeordnungAufbauen(Kaskadenkontext k)
        {
            List<Ladeordnung.LadeEintrag> eintraege = new List<Ladeordnung.LadeEintrag>();
            Dictionary<Ladeordnung.LadeEintrag, Ladeauftrag> auftrag =
                new Dictionary<Ladeordnung.LadeEintrag, Ladeauftrag>();

            foreach (SimulationPufferspeicher sp in k.AlleSpeicher)
            {
                if (sp == null || sp.IstQuelle || sp.ID_Pufferspeicher <= 0) continue;

                List<Ladeordnung.LadeEintrag> proPuffer =
                    Ladeordnung.Ladereihenfolge(m_ID_Projekt, sp.ID_Pufferspeicher);
                List<Ladeordnung.LadeEintrag> gerechnet = new List<Ladeordnung.LadeEintrag>();

                foreach (Ladeordnung.LadeEintrag e in proPuffer)
                {
                    int modulindex = ModulindexDerAnlage(e.ID_Type, e.ID_Anlage);
                    if (modulindex < 0)
                    {
                        k.Hinweise.Add(string.Format(
                            MyResource.Resource.SIMENG_LADEORDNUNG_ART_NICHT_IN_SPEICHERSTUFE,
                            e.ID_Anlage, e.Erzeuger, sp.ID_Pufferspeicher, sp.BezeichnerAnzeige()));
                        continue;
                    }

                    Ladeauftrag a = new Ladeauftrag();
                    a.Modulindex = modulindex;
                    a.Erzeugerart = e.ID_Type;
                    a.AnlagenID = e.ID_Anlage;
                    a.Zweitsenke = e.Zweitsenke;
                    a.Speicher = sp;
                    // Ladeordnung führt die Obergrenze in PROZENT, der Speicher als Anteil.
                    // Sie ist nach ObergrenzenAufloesen immer > 0 (eigene Ladegrenze, sonst
                    // Schwelle_Aus bzw. Schwelle_Aus_Nachrang, beide mit Vorgabe 95 %) —
                    // der frühere Rückfall auf sp.SchwelleAus war unerreichbar und ist
                    // entfallen (Paket-4-Review).
                    a.Obergrenze = e.Obergrenze / 100.0;
                    a.Ladeprio = e.Ladeprio;
                    // Paket-5-Nacharbeit, Befund N7: über die ANLAGE auflösen, nicht über
                    // den Modulindex. Seit Paket 5 ist der Modulindex bei Solarthermie und
                    // Heizkessel ein Index in DEREN Modulliste — BetriebsmodusDesModuls
                    // hätte ihn gegen simulation_wp.Betriebsmodi aufgelöst und damit den
                    // Modus einer beliebigen Wärmepumpe geliefert. Es ist dieselbe
                    // Prioritätsfunktion wie zwei Zeilen weiter unten bei den Obergrenzen
                    // (Konzept 3.5/6.3: Reihenfolge und Obergrenze aus EINER Quelle).
                    a.BMTyp = BetriebsmodusDerAnlage(e.ID_Anlage);

                    eintraege.Add(e);
                    auftrag[e] = a;
                    gerechnet.Add(e);
                }

                // ZWEITE AUFLÖSUNG für Stunden MIT PV-Überschuss (Konzept 3.5). Der
                // Vorrang an einem Puffer bestimmt die Obergrenze; in PV-Stunden gilt
                // eine andere Priorität und damit womöglich ein anderer Vorrang. Ohne
                // diesen Schritt bekäme die Anlage, die WS_Ladeprio_PV gerade nach vorn
                // zieht, weiterhin die Nachrang-Reservezone — die Reihenfolge wäre
                // zeitabhängig, die Obergrenze nicht. Aufgelöst wird über ALLE Einträge
                // des Puffers, auch über die in 4b ruhenden: Der Vorrang entscheidet sich
                // an allen ladenden Anlagen, nicht nur an den Wärmepumpen.
                Ladeordnung.ObergrenzenAufloesen(proPuffer, sp.ID_Pufferspeicher,
                    delegate (Ladeordnung.LadeEintrag e)
                    {
                        return Ladeordnung.WirksameLadeprioPV(
                            e.ID_Type, e.Ladeprio, e.LadeprioPV,
                            BetriebsmodusDerAnlage(e.ID_Anlage), true);
                    });

                foreach (Ladeordnung.LadeEintrag e in gerechnet)
                    auftrag[e].ObergrenzePV = e.Obergrenze / 100.0;
            }

            // Ohne PV-Überschuss: die gespeicherte Ladepriorität (3.4).
            Ladeordnung.SortierenNachLadeprio(eintraege, delegate (Ladeordnung.LadeEintrag e)
            {
                return e.Ladeprio;
            });
            foreach (Ladeordnung.LadeEintrag e in eintraege) k.LadenOhnePV.Add(auftrag[e]);

            // Mit PV-Überschuss: WS_Ladeprio_PV übersteuert, aber nur bei Betriebsmodus PV
            // (3.5). Die Gleichstandskette dahinter ist dieselbe — und es ist DIESELBE
            // Prioritätsfunktion, mit der eben die Obergrenzen aufgelöst wurden.
            Ladeordnung.SortierenNachLadeprio(eintraege, delegate (Ladeordnung.LadeEintrag e)
            {
                return Ladeordnung.WirksameLadeprioPV(e.ID_Type, e.Ladeprio, e.LadeprioPV,
                                                      auftrag[e].BMTyp, true);
            });
            foreach (Ladeordnung.LadeEintrag e in eintraege) k.LadenMitPV.Add(auftrag[e]);
        }

        /// <summary>
        /// Index einer Anlage in der Modulliste IHRER Erzeugerart, sofern diese Art in
        /// diesem Lauf in der Speicherstufe rechnet; sonst −1 (Paket 5).
        /// </summary>
        private int ModulindexDerAnlage(int idType, int idAnlage)
        {
            if (idType == ProjektPuffer.TYP_WP)
                return _wpInSchleife ? simulation_wp.wp_list.IndexOf(idAnlage) : -1;

            if (idType == ProjektPuffer.TYP_SOLARTHERMIE)
                return _solarInSchleife ? simulation_solarthermie.solar_anlagen_ids.IndexOf(idAnlage) : -1;

            if (idType == ProjektPuffer.TYP_KESSEL)
                return _kesselInSchleife ? simulation_spk.spk_anlagen_ids.IndexOf(idAnlage) : -1;

            if (idType == ProjektPuffer.TYP_BHKW)
                return _bhkwInSchleife ? simulation_bhkw.bhkw_anlagen_ids.IndexOf(idAnlage) : -1;

            return -1;
        }

        /// <summary>Betriebsmodus (BM_Typ) eines WP-Moduls; leer, wenn unbekannt.</summary>
        private string BetriebsmodusDesModuls(int modulindex)
        {
            if (simulation_wp == null || simulation_wp.Betriebsmodi == null) return "";
            if (modulindex < 0 || modulindex >= simulation_wp.Betriebsmodi.Count) return "";
            return simulation_wp.Betriebsmodi[modulindex];
        }

        /// <summary>
        /// Betriebsmodus einer ANLAGE; leer, wenn sie in diesem Lauf kein WP-Modul ist.
        /// Nur Wärmepumpen kennen den Modus <c>PV</c> — für alle anderen Erzeuger ist die
        /// PV-Sonderpriorität damit ohnehin unwirksam (Konzept 3.5).
        /// </summary>
        private string BetriebsmodusDerAnlage(int idAnlage)
        {
            if (simulation_wp == null || simulation_wp.wp_list == null) return "";
            return BetriebsmodusDesModuls(simulation_wp.wp_list.IndexOf(idAnlage));
        }

        /// <summary>
        /// Alle am Lauf beteiligten Speicher in stabiler Reihenfolge: erst der
        /// Senkenspeicher der Wärmepumpe (Alias <see cref="puffer_wp"/>), danach die
        /// Quellspeicher der WP-Module in Modulreihenfolge.
        ///
        /// Das ist die EINE Quelle der Wahrheit für Ergebnis-Persistenz
        /// (Tab_ErgebnisPufferspeicher), Navigator-Serien, CSV-Export und die
        /// Ergebnistabelle der Detailansicht (Konzept 6.6/13.3).
        ///
        /// ETAPPE 4b — ZUSAMMENFÜHRUNG MIT DER REGISTRY (offener Punkt 6 aus 4a):
        /// Im zweikanaligen Weg IST die Registry diese eine Quelle. Sie liefert dieselben
        /// Objekte, die auch gerechnet haben — Ergebnis-Persistenz, Navigator und
        /// CSV-Export speisen sich damit aus derselben Menge, die die Stundenschleife
        /// bewegt hat. Das Kriterium ist unverändert „hat gerechnet", nur ist es jetzt
        /// über <see cref="SimulationPufferspeicher.ImRechenpfad"/> ausgedrückt: Ohne
        /// gelaufene Wärmepumpen-Stufe wird die Registry gar nicht erst geöffnet, und die
        /// Liste bleibt leer.
        ///
        /// IM ALTPFAD BLEIBT ALLES, WIE ES WAR: erst der Senkenspeicher der Wärmepumpe,
        /// dann die Quellspeicher der Module. Sonst kämen dort Speicher in die
        /// Ergebniszeilen, die in diesem Lauf nichts getan haben (der von Migrationsregel
        /// R6 angelegte „BHKW-Pendelspeicher" etwa), und die Regressionsbasis wäre
        /// hinfällig.
        /// </summary>
        public System.Collections.Generic.List<SimulationPufferspeicher> AlleSpeicher()
        {
            if (KaskadeZweikanalig) return RegistrySpeicher();

            var liste = new System.Collections.Generic.List<SimulationPufferspeicher>();
            if (puffer_wp != null) liste.Add(puffer_wp);

            if (simulation_wp != null && simulation_wp.Quellspeicher != null)
                foreach (SimulationPufferspeicher q in simulation_wp.Quellspeicher)
                    if (q != null && !liste.Contains(q)) liste.Add(q);

            return liste;
        }

        // ===================================================================
        // Speicher-Registry (Konzept 6.2) - Aufbau und Zugriff
        // ===================================================================

        /// <summary>
        /// Erster Heizungs-Puffer der Registry in Aufnahmereihenfolge, der im Lauf
        /// tatsächlich rechnet — die Auflösung des Alias <see cref="puffer_wp"/>
        /// (Konzept 6.7).
        ///
        /// Die Einschränkung auf <see cref="SimulationPufferspeicher.ImRechenpfad"/> ist
        /// zwingend und dort ausführlich begründet: Die Registry enthält auch Puffer, die
        /// in diesem Lauf niemand rechnet. Da der Senkenspeicher der Wärmepumpe als
        /// ERSTER aufgenommen wird, liefert die Methode im Altpfad genau den Speicher,
        /// den die bisherige Z-basierte Initialisierung geliefert hat.
        ///
        /// Im zweikanaligen Weg bleibt die Einschränkung bestehen, nur mit dem engeren
        /// Kriterium aus <see cref="RegistryFuerZweikanaligOeffnen"/> (Senken- oder
        /// Quellreferenz). OFFEN: die Reihenfolge auf die Entladepriorität umzustellen
        /// (Konzept 3.6) — sie ist hier noch die Aufnahmereihenfolge, und bei mehreren
        /// Heizungspuffern zeigt <c>puffer_wp</c> deshalb auf den zuerst aufgenommenen,
        /// nicht auf den zuerst entladenen.
        /// </summary>
        public SimulationPufferspeicher ErsterHeizpuffer()
        {
            foreach (int id in _speicherReihenfolge)
            {
                SimulationPufferspeicher sp;
                if (!speicherRegistry.TryGetValue(id, out sp) || sp == null) continue;
                if (!sp.ImRechenpfad) continue;
                if (string.Equals(sp.Verwendung, SimulationPufferspeicher.VERWENDUNG_HEIZUNG,
                                  StringComparison.Ordinal))
                    return sp;
            }

            return _pufferOhneRegistrySchluessel;
        }

        /// <summary>
        /// Nimmt einen Speicher unter seiner <c>Tab_Pufferspeicher.ID</c> in die Registry
        /// auf. Ein bereits vorhandener Schlüssel wird NICHT überschrieben — „je Speicher
        /// genau ein Objekt" heißt auch: das zuerst aufgebaute Objekt gewinnt, und das
        /// ist mit Absicht der Senkenspeicher der Wärmepumpe mit seinen Parametern aus
        /// der Alt-Zuordnung.
        /// </summary>
        /// <returns>true, wenn der Speicher neu aufgenommen wurde.</returns>
        private bool SpeicherAufnehmen(SimulationPufferspeicher sp, bool imRechenpfad)
        {
            if (sp == null) return false;

            int id = sp.ID_Pufferspeicher;
            if (id <= 0) return false;                       // ohne ID kein Schlüssel
            if (speicherRegistry.ContainsKey(id)) return false;

            sp.ImRechenpfad = imRechenpfad;
            speicherRegistry[id] = sp;
            _speicherReihenfolge.Add(id);
            return true;
        }

        /// <summary>
        /// Baut die Speicher-Registry des Laufs auf (Konzept 6.2).
        ///
        /// Reihenfolge — sie ist Teil des Vertrags, nicht Geschmackssache:
        ///
        ///   1. Der Senkenspeicher der WÄRMEPUMPE aus der Alt-Zuordnung
        ///      <c>Z_ProjektPufferSp</c>, mit unveränderter Parameterherkunft (Konzept
        ///      6.7: „die heutige Initialisierung bleibt die Quelle der Parameter").
        ///      Er steht an erster Stelle und ist beim Aufbau der einzige SENKEN-Eintrag
        ///      mit <see cref="SimulationPufferspeicher.ImRechenpfad"/> — daraus folgt,
        ///      dass <see cref="ErsterHeizpuffer"/> im Altpfad genau ihn liefert.
        ///   2. Alle übrigen von den Projektanlagen als SENKE referenzierten Puffer
        ///      (<c>WS_ID_Puffer</c>, <c>WS_ID_Puffer2</c>) und die Puffer der übrigen
        ///      Zuordnungszeilen, jeweils mit den Betriebsparametern der Projektkopie.
        ///
        /// Die QUELLspeicher (<c>WQ_ID_Puffer</c>) kommen NACH dem Modulaufbau dazu, über
        /// <see cref="QuellspeicherUebernehmen"/>. Grund: Ihre nutzbare Kapazität folgt
        /// nicht dem Temperaturpaar der Speicherzeile, sondern der Spreizung
        /// <c>WQ_Spreizung</c> der ANLAGE (dazu <c>WQ_Regeneration</c>); ein vorab aus der
        /// Speicherzeile gebautes Objekt trüge ein falsches Q_max. Übernommen werden
        /// deshalb die Instanzen, die das Modul tatsächlich rechnet.
        ///
        /// Nur REFERENZIERTE Puffer kommen hinein. Projekt 1023 der Referenzmenge zeigt,
        /// warum: Es trägt über 80 Puffer-Kopien aus wiederholtem „Projekt duplizieren",
        /// von denen genau einer benutzt wird.
        /// </summary>
        private void SpeicherRegistryAufbauen()
        {
            speicherRegistry.Clear();
            _speicherReihenfolge.Clear();
            _zusatzSpeicher.Clear();
            _pufferOhneRegistrySchluessel = null;

            // --- 0. Die Verbünde des Projekts, EINMAL ---------------------------------
            //
            // PAKET PARALLELVERBUND: Muss VOR beiden Blöcken stehen — die Aggregation
            // greift in Block 1 (WP-Alt-Zuordnung) genauso wie in Block 2. Auf einer
            // Datenbank ohne Verbund ist das Verzeichnis leer und alles Folgende ist Zeile
            // für Zeile das bisherige Verhalten.
            List<int> abweichendeZuschnitte;
            _verbuende = AnlagePufferVerbundCtrl.VerbuendeDesProjekts(m_ID_Projekt,
                                                                     out abweichendeZuschnitte);

            // FACHLICH UNMÖGLICHE KONSTELLATION, aber kein Absturzgrund: Zwei Anlagen
            // nennen für denselben Leitspeicher unterschiedliche Mitglieder. Hydraulisch
            // ist ein Behälter entweder Teil des Vorrats oder nicht — eine
            // erzeugerabhängige Kapazität desselben Speichers gibt es nicht. Gerechnet
            // wird deshalb die VEREINIGUNG (siehe VerbuendeDesProjekts), und der Anwender
            // erfährt es. Der Dialog verhindert diesen Fall beim Speichern; hier bleibt er
            // für von Hand gepflegte Bestände.
            foreach (int idLeit in abweichendeZuschnitte)
                Protokoll.WarnungEinmal("verbund-zuschnitt-abweichend-" + idLeit,
                                  "Parallelverbund: Für den Leitspeicher " + idLeit +
                                  " nennen mehrere Wärmeerzeuger UNTERSCHIEDLICHE " +
                                  "Verbundmitglieder. Gerechnet wird die Vereinigung aller " +
                                  "genannten Speicher als EIN Vorrat - ein Behälter ist " +
                                  "hydraulisch entweder Teil des Verbunds oder nicht. " +
                                  "Bitte die Wärmesenken der beteiligten Erzeuger vereinheitlichen.");

            // --- 1. Senkenspeicher der Wärmepumpe aus Z_ProjektPufferSp --------------
            //
            // Der Block ist gegenüber Paket 3 UNVERÄNDERT (nur das Ziel der Zuweisung
            // ist jetzt eine lokale Variable): Quelle der Parameter bleibt die
            // Zuordnungszeile mit dem Vorrang der Puffer-Betriebstemperaturen, damit
            // Q_max und die Schwellen exakt dieselben Zahlen ergeben wie bisher.
            Z_ProjektPufferSpCtrl pspZuordnung = new Z_ProjektPufferSpCtrl();
            pspZuordnung.ReadAll("ID_Projekt=" + m_ID_Projekt);
            _pspZuordnungen = pspZuordnung;          // N2: für den Ersatz-Pendelspeicher
            for (int n = 0; n < pspZuordnung.rows; n++)
            {
                if (pspZuordnung.items[n].Erzeuger != DbWerte.ERZEUGER_WAERMEPUMPE) continue;

                PufferSpCtrl psp = new PufferSpCtrl();
                psp.ReadAll("ID=" + pspZuordnung.items[n].ID_Pufferspeicher);
                if (psp.rows == 0 && !string.IsNullOrEmpty(pspZuordnung.items[n].PufferSp))
                {
                    // Fallback für Altdaten ohne ID: über Bezeichner im Projekt suchen
                    psp.ReadAll("Bezeichner='" + pspZuordnung.items[n].PufferSp.Replace("'", "''") +
                                "' AND ID_Projekt=" + m_ID_Projekt);
                }

                if (psp.rows > 0)
                {
                    // Etappe 4: Die PUFFER-Zeile ist die führende Ablage der
                    // Betriebstemperaturen (Konzept 5.1) - ein Speicher hat genau einen
                    // Betriebszustand, unabhängig davon, wie viele Anlagen ihn laden.
                    // Nur wenn dort kein vollständiges Paar steht (Alt-Datenbank, nie
                    // migriert, Werte gelöscht), gilt weiter die Zuordnungszeile.
                    //
                    // Regressionsneutral: Migration R1 hat genau die Werte DIESER
                    // Zuordnungszeile an den Puffer geschrieben - der Vorrang liefert
                    // auf migrierten Beständen dieselben Zahlen wie bisher.
                    int vorlauf = pspZuordnung.items[n].Vorlauf;
                    int ruecklauf = pspZuordnung.items[n].Ruecklauf;

                    int vPuffer, rPuffer;
                    if (PufferSpCtrl.TemperaturenLesen(psp.items[0].ID, out vPuffer, out rPuffer))
                    {
                        vorlauf = vPuffer;
                        ruecklauf = rPuffer;
                    }

                    SimulationPufferspeicher pufferWp = new SimulationPufferspeicher();
                    pufferWp.Bezeichner = psp.items[0].Name;
                    pufferWp.Erzeuger = "Wärmepumpe";
                    // Konzept 6.6: Rolle und Speicher-ID wandern in die Ergebniszeile
                    // und bilden den technischen Serienschlüssel der Anzeigen (13.3).
                    pufferWp.ID_Pufferspeicher = psp.items[0].ID;
                    pufferWp.ID_Projekt = m_ID_Projekt;

                    // ETAPPE D5a: Ein KOMBISPEICHER behält seine Verwendung — käme er über
                    // die Alt-Zuordnung als „Heizung" in die Registry, verlöre er seine
                    // zweite Kanalzugehörigkeit und der Warmwasserkanal bliebe ungedeckt.
                    // Jede ANDERE Verwendung bleibt ausdrücklich bei HEIZUNG: Diese Zeile
                    // ist die Rolle, mit der der Altpfad seinen Wärmepumpen-Puffer rechnet,
                    // und daran ändert D5a nichts (Regressionszusage).
                    //
                    // NACHARBEIT E-K1-2 — NUR IM ZWEIKANALIGEN WEG. Der Altpfad kennt den
                    // Kanalbegriff nicht; dort ist ein Kombispeicher ausdrücklich WIE EIN
                    // HEIZUNGSPUFFER zu führen (AltpfadHinweiseD5a sagt das dem Anwender).
                    // Stünde hier auch im Altpfad „Kombi", fände ErsterHeizpuffer() den
                    // Speicher nicht mehr — der Alias puffer_wp wäre null, die Wärmepumpe
                    // rechnete OHNE Speicher, und der Puffer fehlte in
                    // Tab_ErgebnisPufferspeicher, in der Erdreich-Auswertung und in den
                    // Zeitreihen des Berichts. Genau die Zusage „wie Heizung, mit Hinweis"
                    // hält diese Bedingung ein, und zwar für ALLE Altpfad-Leser: In der
                    // Registry steht dann durchgehend „Heizung".
                    pufferWp.Verwendung =
                        (KaskadeZweikanalig &&
                         WaermesenkeClass.IstKombiVerwendung(
                            WaermesenkeClass.WirksameVerwendung(
                                WaermesenkeClass.PufferLesen(psp.items[0].ID))))
                        ? SimulationPufferspeicher.VERWENDUNG_KOMBI
                        : SimulationPufferspeicher.VERWENDUNG_HEIZUNG;
                    pufferWp.Init(psp.items[0].Gesamtvolumen,
                                  vorlauf,
                                  ruecklauf,
                                  psp.items[0].Betriebsbereitschaftverlust);
                    RueckfallMelden(pufferWp, psp.items[0].ID, psp.items[0].Name);

                    // PAKET PARALLELVERBUND — NACH RueckfallMelden: Die Rückfallmeldung
                    // nennt die Kapazität, die aus dem ΔT-Notnagel des LEITSPEICHERS
                    // folgt. Stünde die Aggregation davor, meldete sie eine
                    // Verbundkapazität und wäre als Aussage über den einen Speicher ohne
                    // Temperaturpaar falsch.
                    VerbundAufaddieren(pufferWp);

                    // Konfigurierbare Schwellen der Speicherregelung [%]
                    object sEin = WaermequelleClass.WertLesenStill("Z_ProjektPufferSp", "Schwelle_Ein", pspZuordnung.items[n].ID);
                    object sAus = WaermequelleClass.WertLesenStill("Z_ProjektPufferSp", "Schwelle_Aus", pspZuordnung.items[n].ID);
                    if (sEin != null && Convert.ToDouble(sEin) > 0)
                        pufferWp.SchwelleEin = Convert.ToDouble(sEin) / 100.0;
                    if (sAus != null && Convert.ToDouble(sAus) > 0)
                        pufferWp.SchwelleAus = Convert.ToDouble(sAus) / 100.0;
                    // Zweite Stufe der Ladeobergrenze (Konzept 3.4): ohne eigenen Wert
                    // gleich der Abschaltschwelle und damit wirkungslos.
                    pufferWp.SchwelleAusNachrang = pufferWp.SchwelleAus;

                    pufferWp.ImRechenpfad = true;
                    if (!SpeicherAufnehmen(pufferWp, true))
                        _pufferOhneRegistrySchluessel = pufferWp;   // Speicherzeile ohne ID
                }
                break; // ReadAll sortiert nach Priorität -> erster Treffer gewinnt
            }

            // --- 2. Alle übrigen referenzierten Projekt-Puffer ------------------------
            foreach (int id in ReferenzierteSenkenPuffer(pspZuordnung))
            {
                if (id <= 0 || speicherRegistry.ContainsKey(id)) continue;

                WaermesenkeClass.PufferInfo p = WaermesenkeClass.PufferLesen(id);
                if (p == null)
                {
                    Protokoll.WarnungEinmal("registry-puffer-fehlt-" + id,
                                      "Speicher-Registry: Puffer " + id +
                                      " ist referenziert, existiert aber nicht mehr.");
                    continue;
                }
                if (p.ID_Projekt != m_ID_Projekt)
                {
                    Protokoll.WarnungEinmal("registry-puffer-fremdprojekt-" + id,
                                      "Speicher-Registry: Puffer " + id + " gehört zu Projekt " +
                                      p.ID_Projekt + ", nicht zu " + m_ID_Projekt +
                                      " - wird nicht aufgenommen.");
                    continue;
                }

                SimulationPufferspeicher sp = new SimulationPufferspeicher();
                sp.Bezeichner = p.Bezeichner;
                sp.ID_Pufferspeicher = p.ID;
                sp.ID_Projekt = p.ID_Projekt;
                sp.Verwendung = WaermesenkeClass.WirksameVerwendung(p);

                // TEMPERATUR-VORRANGKETTE wie in Block 1 (Paket 1d, in der Paket-4-Review
                // nachgezogen): Die Projektkopie ist die führende Ablage; steht dort kein
                // vollständiges Paar, gilt die ZUORDNUNGSZEILE - und erst danach der
                // 10-K-Notnagel aus SimulationPufferspeicher.Init. Ohne diese Stufe bekäme
                // ein Puffer aus einer Alt-Datenbank ohne Temperaturen ein Q_max nach
                // 10 K, obwohl in Z_ProjektPufferSp ein gepflegtes Paar steht - und damit
                // eine andere nutzbare Kapazität als derselbe Puffer in Block 1.
                int vorlauf = p.Vorlauf;
                int ruecklauf = p.Ruecklauf;
                if (vorlauf - ruecklauf <= 0)
                {
                    int vZuordnung, rZuordnung;
                    if (ZuordnungsTemperaturen(pspZuordnung, p.ID, p.Bezeichner,
                                               out vZuordnung, out rZuordnung))
                    {
                        vorlauf = vZuordnung;
                        ruecklauf = rZuordnung;
                        Protokoll.HinweisEinmal("registry-temp-zuordnung-" + p.ID,
                                          "Speicher-Registry: Puffer " + p.ID + " (" + p.Bezeichner +
                                          ") hat kein Temperaturpaar in der Projektkopie - es gilt " +
                                          "die Zuordnungszeile (" + vorlauf + "/" + ruecklauf + " °C).");
                    }
                }

                sp.Init(p.Gesamtvolumen, vorlauf, ruecklauf, p.Bereitschaftsverluste);
                RueckfallMelden(sp, p.ID, p.Bezeichner);

                // PAKET PARALLELVERBUND — dieselbe Stelle wie in Block 1, unmittelbar nach
                // der Rückfallmeldung des LEITSPEICHERS (Begründung dort).
                VerbundAufaddieren(sp);

                // PufferInfo führt die Schwellen in PROZENT (Ladeordnung-Vorgaben),
                // SimulationPufferspeicher als Anteil 0..1.
                sp.SchwelleEin = p.SchwelleEin / 100.0;
                sp.SchwelleAus = p.SchwelleAus / 100.0;
                sp.SchwelleAusNachrang = p.SchwelleAusNachrang / 100.0;
                sp.Entladeprio = p.Entladeprio;

                // PAKET BHKW-REGULÄR: Mindestfüllstand/Notreserve, dieselbe
                // Prozent-zu-Anteil-Umrechnung wie bei den drei Schwellen. WIRKSAM wird der
                // Wert erst, wenn die Kaskadenschleife den Speicher als Ziel eines
                // BHKW-Ladeauftrags erkennt (BhkwReserveGilt) - bis dahin ist er ein
                // getragener Parameter ohne Rechenwirkung.
                sp.SchwelleReserve = p.SchwelleReserve / 100.0;

                // Beim Aufbau nicht im Rechenpfad; der zweikanalige Weg öffnet ihn, wenn
                // eine Anlage ihn als Senke führt (siehe ImRechenpfad).
                SpeicherAufnehmen(sp, false);
            }
        }

        /// <summary>
        /// PAKET PARALLELVERBUND (Entscheidung des Anwenders 17.08.2026): Macht aus dem
        /// LEITSPEICHER das Rechenobjekt des GESAMTEN Verbunds — ein gemeinsamer
        /// Wärmevorrat aus mehreren parallel verschalteten Behältern.
        ///
        /// <b>WARUM HIER.</b> Das ist die minimal-invasive Stelle des ganzen Pakets. An
        /// dieser Zeile ist der Leitspeicher gerade fertig initialisiert, aber noch nicht
        /// in der Registry — alles, was danach kommt (Ladeordnung, Entladereihenfolge,
        /// Phase G, Persistenz nach <c>Tab_ErgebnisPufferspeicher</c>, Kennzahlen,
        /// Berichtszeitreihen), arbeitet bereits mit dem fertigen Objekt und braucht
        /// KEINE Verbundkenntnis. Ein Verbund ist damit für den gesamten Rechenweg genau
        /// ein Speicher mit größerer Kapazität — und exakt das ist die fachliche Zusage
        /// („Kapazitäten addiert, ein Füllstand, eine Schaltschwelle, EINE Ergebniszeile").
        /// Jede andere Stelle hätte den Verbundbegriff in die Kaskadenschleife tragen
        /// müssen.
        ///
        /// <b>Q_max WIRD SUMMIERT, NICHT DAS VOLUMEN.</b> Jeder Behälter bringt sein
        /// eigenes Temperaturpaar mit, und für jedes gilt die Vorrangkette bzw. der
        /// ΔT-Rückfall dieses Speichers — genau wie bisher für einen Einzelpuffer (Block 2
        /// oben, <c>WaermesenkeClass.PufferInfo.Q_max</c>). Zwei mal 1000 l bei 60/40 und
        /// 50/40 ergeben eben nicht 2000 l bei einer der beiden Spreizungen. Über die
        /// Einzelkapazitäten zu summieren ist die physikalisch richtige Rechnung und
        /// dieselbe Zahl, die der Dialog anzeigt
        /// (<c>WaermesenkeClass.VerbundKapazitaet</c>).
        ///
        /// <b>Was MITKOMMT:</b> <c>Q_max</c> und die <c>Bereitschaftsverluste</c> (jeder
        /// Behälter verliert für sich; der Verbund verliert die Summe).
        ///
        /// <b>Was NICHT mitkommt:</b> Schwellen (Ein/Aus/Nachrang), Notreserve,
        /// Entladepriorität, Verwendung und das angezeigte Temperaturpaar. Sie stammen
        /// AUSSCHLIESSLICH vom Leitspeicher, denn ein gemeinsamer Vorrat hat genau eine
        /// Regelung — zwei Abschaltschwellen an einem Füllstand wären keine Physik, sondern
        /// ein Widerspruch. Der Anwender pflegt sie am Leitspeicher, und die
        /// Pufferverwaltung zeigt genau dort, was gilt.
        ///
        /// <b>Ein Mitglied ohne gepflegtes Temperaturpaar</b> bekommt HIER den generischen
        /// 10-K-Rückfall aus <see cref="SimulationPufferspeicher.Init"/> — dieselbe
        /// Ersatzannahme, die es als Einzelspeicher bekäme — und die Meldung dazu, damit
        /// eine Verbundkapazität nie stillschweigend auf einer Annahme steht.
        ///
        /// <b>Kein Mitglied wird ein eigenes Rechenobjekt.</b> Mitglieder stehen in keiner
        /// <c>WS_ID_Puffer</c>-Referenz und kommen deshalb schon über
        /// <see cref="ReferenzierteSenkenPuffer"/> nicht in die Registry; das
        /// Sicherheitsnetz für von Hand gepflegte Bestände sitzt in
        /// <see cref="RegistryFuerZweikanaligOeffnen"/>.
        /// </summary>
        private void VerbundAufaddieren(SimulationPufferspeicher leit)
        {
            if (leit == null || _verbuende.Count == 0) return;

            List<int> mitglieder;
            if (!_verbuende.TryGetValue(leit.ID_Pufferspeicher, out mitglieder) ||
                mitglieder == null || mitglieder.Count == 0) return;

            double qLeit = leit.Q_max;
            double qSumme = qLeit;
            int gezaehlt = 0;

            // QUELLSPEICHER des Projekts — sie dürfen NIE in einen Senkenvorrat wandern.
            //
            // Ein Quellspeicher rechnet auf einem EIGENEN Weg: Seine Kapazität folgt der
            // Anlagen-Spreizung WQ_Spreizung, nicht dem Temperaturpaar der Speicherzeile
            // (QuellspeicherUebernehmen). Er ist damit bereits ein vollwertiges
            // Rechenobjekt; ihn zusätzlich aufzuaddieren zählte dieselbe Kapazität zweimal.
            //
            // Das Sicherheitsnetz in RegistryFuerZweikanaligOeffnen greift für diesen Fall
            // NICHT: Es lässt Quellspeicher ausdrücklich als Erstes durch
            // (sp.IstQuelle ⇒ ImRechenpfad = true, danach continue), weil sie ihren eigenen
            // Pfad haben. Die Abwehr muss deshalb HIER stehen — an der Stelle, an der die
            // Kapazität summiert wird.
            //
            // Aufgefallen im Wirkungsnachweis dieses Pakets: In Projekt 1021 war der als
            // Verbundmitglied eingetragene zweite Heizungspuffer zugleich Quellspeicher der
            // zweiten Wärmepumpe — seine Kapazität erschien im Verbund UND als eigenes
            // Quellobjekt in Tab_ErgebnisPufferspeicher.
            List<int> quellPuffer = AnlagePufferVerbundCtrl.QuellPufferDesProjekts(m_ID_Projekt);

            foreach (int idMitglied in mitglieder)
            {
                if (quellPuffer.Contains(idMitglied))
                {
                    Protokoll.WarnungEinmal("verbund-mitglied-ist-quelle-" + idMitglied,
                                      "Parallelverbund: Der Puffer " + idMitglied +
                                      " ist die WÄRMEQUELLE einer Anlage dieses Projekts und " +
                                      "kann deshalb nicht Teil des Wärmevorrats von Speicher " +
                                      leit.ID_Pufferspeicher + " (" + leit.BezeichnerAnzeige() +
                                      ") sein - ein Behälter liefert entweder die Wärme oder er " +
                                      "bildet den Vorrat, in den sie geladen wird. Seine " +
                                      "Kapazität geht NICHT in den Verbund ein; bitte die " +
                                      "Wärmesenke der ladenden Anlage berichtigen.");
                    continue;
                }

                WaermesenkeClass.PufferInfo m = WaermesenkeClass.PufferLesen(idMitglied);
                if (m == null)
                {
                    Protokoll.WarnungEinmal("verbund-mitglied-fehlt-" + idMitglied,
                                      "Parallelverbund: Das Verbundmitglied " + idMitglied +
                                      " des Leitspeichers " + leit.ID_Pufferspeicher + " (" +
                                      leit.BezeichnerAnzeige() + ") existiert nicht mehr - seine " +
                                      "Kapazität fehlt im gemeinsamen Vorrat.");
                    continue;
                }

                if (m.ID_Projekt != m_ID_Projekt)
                {
                    Protokoll.WarnungEinmal("verbund-mitglied-fremdprojekt-" + idMitglied,
                                      "Parallelverbund: Das Verbundmitglied " + idMitglied + " (" +
                                      m.Bezeichner + ") gehört zu Projekt " + m.ID_Projekt +
                                      ", nicht zu " + m_ID_Projekt +
                                      " - es wird nicht mitgerechnet.");
                    continue;
                }

                // Einzelkapazität über DENSELBEN Weg wie ein eigenständiger Speicher: ein
                // eigenes SimulationPufferspeicher-Objekt, das nur zum Rechnen von Q_max
                // und VerlustProStunde dient und die Registry nie sieht. So gilt für jedes
                // Mitglied die vertraute Init-Regel (1,16 Wh/(l·K), ΔT-Rückfall 10 K), und
                // es gibt keine zweite Kapazitätsformel im Haus.
                SimulationPufferspeicher hilf = new SimulationPufferspeicher();
                hilf.Init(m.Gesamtvolumen, m.Vorlauf, m.Ruecklauf, m.Bereitschaftsverluste);
                RueckfallMelden(hilf, m.ID, m.Bezeichner);

                qSumme += hilf.Q_max;
                leit.VerlustProStunde += hilf.VerlustProStunde;
                gezaehlt++;
            }

            leit.Q_max = qSumme;

            if (gezaehlt == 0) return;

            // Der NACHWEIS des Pakets: eine Protokollzeile, aus der die Summe
            // nachvollziehbar ist. Sie steht bewusst im HINWEIS-Kanal, nicht als Warnung -
            // der Verbund ist eine gewollte Konfiguration, keine Ersatzannahme.
            Protokoll.HinweisEinmal("verbund-aggregiert-" + leit.ID_Pufferspeicher,
                              "Parallelverbund: Speicher " + leit.ID_Pufferspeicher + " (" +
                              leit.BezeichnerAnzeige() + ") rechnet als EIN gemeinsamer Vorrat " +
                              "aus " + (gezaehlt + 1) + " Behältern - nutzbare Kapazität Q_max " +
                              qLeit.ToString("0.###") + " kWh (Leitspeicher) + " +
                              (qSumme - qLeit).ToString("0.###") + " kWh (" + gezaehlt +
                              " Mitglieder) = " + qSumme.ToString("0.###") + " kWh. Schwellen, " +
                              "Notreserve, Entladepriorität und Verwendung gelten aus dem " +
                              "Leitspeicher; es entsteht EINE Ergebniszeile unter seiner ID.");
        }

        /// <summary>
        /// Meldet einen ΔT-RÜCKFALL eines Speichers (Nacharbeit Paket 6, Befund N2).
        ///
        /// Ohne gepflegtes Temperaturpaar rechnet <c>SimulationPufferspeicher.Init</c>
        /// mit einem Ersatzwert — 10 K für gewöhnliche Puffer, 20 K für den
        /// BHKW-Pendelspeicher. Beides verändert die nutzbare Kapazität erheblich (bei
        /// einem 1000-l-Puffer 11,6 gegen 23,2 kWh), und beides geschah bis zur
        /// Nacharbeit stillschweigend. Projektgrundsatz: sichtbar falsch ist besser als
        /// still falsch.
        ///
        /// Die Meldung läuft in BEIDEN Rechenwegen — der Registry-Aufbau gehört zu
        /// keinem von beiden. Sie geht in kein Ergebnis und in keine CSV ein; der
        /// Altpfad rechnet unverändert.
        ///
        /// PROTOKOLLKANAL-NACHZUG: seit dem Folgepaket zu Paket 9 über den
        /// WARNUNGS-Kanal statt nur auf die Konsole. Der Ersatzwert bestimmt die
        /// nutzbare Kapazität und ist damit genau das, was Paket 8 unter „gerechnet,
        /// aber mit einer Ersatzannahme" führt. (Die Beispielliste im Klassenkopf von
        /// <see cref="SimulationProtokoll"/> nennt den ΔT-Rückfall unter den Hinweisen;
        /// maßgeblich ist die STUFENDEFINITION darüber — siehe Nachzug-Protokoll.)
        /// Die Konsolenzeile bleibt, sie steckt in <c>SimulationProtokoll.Eintragen</c>.
        /// </summary>
        private static void RueckfallMelden(SimulationPufferspeicher sp, int idPuffer, string bezeichner)
        {
            if (sp == null || sp.RueckfallDeltaT <= 0) return;

            SimulationProtokoll.Aktuell.WarnungEinmal("deltaT-rueckfall-" + idPuffer,
                              "Speicher-Registry: Puffer " + idPuffer + " (" +
                              (string.IsNullOrEmpty(bezeichner) ? "ohne Bezeichner" : bezeichner) +
                              ") hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = " +
                              sp.RueckfallDeltaT.ToString("0.#") + " K, nutzbare Kapazität Q_max " +
                              sp.Q_max.ToString("0.###") + " kWh. Ein gepflegtes Vorlauf-/" +
                              "Rücklaufpaar am Puffer ergäbe eine andere Kapazität.");
        }

        /// <summary>
        /// Vorlauf/Rücklauf einer Alt-Zuordnung <c>Z_ProjektPufferSp</c> zu einem Puffer —
        /// die mittlere Stufe der Temperatur-Vorrangkette (Paket 1d).
        ///
        /// Gesucht wird zuerst über die Puffer-ID, danach über den Bezeichner: Genau diese
        /// zwei Wege benutzt auch Block 1 des Registry-Aufbaus, und Altdaten ohne
        /// <c>ID_Pufferspeicher</c> hängen ausschließlich am Namen.
        /// </summary>
        /// <returns>true, wenn ein VOLLSTÄNDIGES Paar (Vorlauf &gt; Rücklauf) gefunden wurde.</returns>
        private bool ZuordnungsTemperaturen(Z_ProjektPufferSpCtrl zuordnungen, int idPuffer,
                                            string bezeichner, out int vorlauf, out int ruecklauf)
        {
            vorlauf = 0;
            ruecklauf = 0;
            if (zuordnungen == null) return false;

            for (int n = 0; n < zuordnungen.rows; n++)
            {
                bool trifft = (idPuffer > 0 && zuordnungen.items[n].ID_Pufferspeicher == idPuffer) ||
                              (!string.IsNullOrEmpty(bezeichner) &&
                               string.Equals(zuordnungen.items[n].PufferSp, bezeichner,
                                             StringComparison.Ordinal));
                if (!trifft) continue;

                if (zuordnungen.items[n].Vorlauf - zuordnungen.items[n].Ruecklauf <= 0) continue;

                vorlauf = zuordnungen.items[n].Vorlauf;
                ruecklauf = zuordnungen.items[n].Ruecklauf;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Übernimmt die Quellspeicher der WP-Module in die Registry (Konzept 6.2,
        /// Zusatz der Fassung 12: „Die Registry muss diese Instanzen übernehmen oder
        /// ablösen — sonst entstehen zwei parallele Speicherverwaltungen mit getrennter
        /// Bilanz").
        ///
        /// Übernommen werden die INSTANZEN selbst, keine Kopien: Was das Modul rechnet,
        /// ist danach dasselbe Objekt, das in der Registry steht.
        ///
        /// MEHRERE MODULE AM SELBEN QUELLPUFFER: Im einkanaligen Altpfad behält jedes
        /// Modul seine EIGENE Instanz, und nur die erste kommt in die Registry — ein
        /// Zusammenlegen wäre dort keine Aufräumarbeit, sondern eine Ergebnisänderung.
        /// Im zweikanaligen Weg hat <c>SimulationWaermepumpe.QuellspeicherZusammenfuehren</c>
        /// die Instanzen bereits vereinigt, bevor diese Methode läuft; die Schleife sieht
        /// dann je Puffer-ID nur noch ein Objekt.
        ///
        /// KURZSCHLUSS QUELLE = SENKE: Zeigt <c>WQ_ID_Puffer</c> auf einen Speicher, der
        /// im selben Projekt schon als SENKE in der Registry steht, ist der Schlüssel
        /// belegt und die Quell-Instanz käme nicht hinein — sie würde still aus Phase G
        /// (<c>StundeAbschliessen</c>) und aus der Ergebnispersistenz fallen, obwohl das
        /// WP-Modul aus ihr entnimmt. Genau das darf nicht passieren: Die Instanz wird
        /// deshalb ohne Registry-Schlüssel als ZUSATZSPEICHER geführt (siehe
        /// <see cref="_zusatzSpeicher"/>) und ausdrücklich protokolliert. Fachlich ist die
        /// Konfiguration ein Fehler — Konzept 4.6 blockiert sie beim Speichern —, Altdaten
        /// können sie aber tragen. Sichtbar falsch ist besser als still falsch.
        /// </summary>
        /// <param name="zweikanalig">
        /// true = Aufruf aus der gemeinsamen Speicherstufe. Nur dort greift die
        /// KASKADEN-Auflösung aus Etappe D5a (siehe unten); der Altpfad behält seine
        /// getrennten Instanzen und rechnet unverändert.
        /// </param>
        private void QuellspeicherUebernehmen(bool zweikanalig)
        {
            if (simulation_wp == null || simulation_wp.Quellspeicher == null) return;

            // Über eine Kopie laufen: Die Kaskaden-Auflösung tauscht Einträge der Liste
            // aus, über die hier iteriert wird.
            List<SimulationPufferspeicher> quellen =
                new List<SimulationPufferspeicher>(simulation_wp.Quellspeicher);

            foreach (SimulationPufferspeicher q in quellen)
            {
                if (q == null) continue;
                if (q.ID_Projekt <= 0) q.ID_Projekt = m_ID_Projekt;

                if (SpeicherAufnehmen(q, true)) continue;

                // Schon unter derselben Instanz aufgenommen (mehrere Module, zusammengeführt).
                if (q.ID_Pufferspeicher > 0 && speicherRegistry.ContainsKey(q.ID_Pufferspeicher) &&
                    ReferenceEquals(speicherRegistry[q.ID_Pufferspeicher], q)) continue;

                SimulationPufferspeicher belegt = null;
                if (q.ID_Pufferspeicher > 0) speicherRegistry.TryGetValue(q.ID_Pufferspeicher, out belegt);

                // ---------------------------------------------------------------
                // KASKADE (Etappe D5a): Der Schlüssel ist von einem SENKENspeicher
                // belegt, der NICHT die eigene Senke dieser Anlage ist — das ist kein
                // Kurzschluss, sondern die Booster-Konstellation des Konzepts: WP 1 lädt
                // Puffer 1, WP 2 bezieht daraus ihre Quellwärme.
                //
                // Beide Module müssen dann DIESELBE Instanz benutzen. Die eigens
                // aufgebaute Quell-Instanz startet voll (SOC = Q_max) und führte eine
                // zweite, getrennte Bilanz desselben Speichers — genau das, was Konzept
                // 6.2 ausschließt. Sie wird deshalb durch die Registry-Instanz ersetzt;
                // die Rechenreihenfolge (WP 2 nach Puffer 1) stellt die Kaskadenschleife
                // über die Rechenebenen her.
                // ---------------------------------------------------------------
                if (zweikanalig && belegt != null && !belegt.IstQuelle &&
                    !IstEigenerSenkenPuffer(q.ID_Anlage, q.ID_Pufferspeicher))
                {
                    int ersetzt = simulation_wp.QuellspeicherErsetzen(q, belegt);
                    Protokoll.HinweisEinmal("quelle-kaskade-" + q.ID_Pufferspeicher,
                                      "Kaskade: Puffer " + q.ID_Pufferspeicher + " (" +
                                      belegt.BezeichnerAnzeige() + ") ist WÄRMEQUELLE der Anlage " +
                                      q.ID_Anlage + " und zugleich Senke eines anderen Erzeugers. " +
                                      "Beide rechnen auf DERSELBEN Speicherinstanz (" + ersetzt +
                                      " Modulbezug umgestellt); die Anlage rechnet nach dem " +
                                      "Erzeuger, der den Puffer lädt.");
                    continue;
                }

                Protokoll.WarnungEinmal("registry-quelle-senke-kurzschluss-" + q.ID_Pufferspeicher,
                                  "Speicher-Registry: Puffer " + q.ID_Pufferspeicher + " ist QUELLE der " +
                                  "Anlage " + q.ID_Anlage + " und steht zugleich als " +
                                  ((belegt != null && !belegt.IstQuelle) ? "SENKE" : "weiterer Eintrag") +
                                  " in der Registry (Kurzschluss, Konzept 4.6). Die Quell-Instanz rechnet " +
                                  "mit und wird bilanziert, aber die Konfiguration ist zu prüfen.");

                q.ImRechenpfad = true;
                if (!_zusatzSpeicher.Contains(q)) _zusatzSpeicher.Add(q);
            }
        }

        // ==================================================================
        // QUELLBEZÜGE AUF PUFFERSPEICHER (Etappe D5a)
        // ==================================================================

        /// <summary>
        /// Sammelt die Quellbezüge auf Pufferspeicher und richtet die Kessel-Kaskade ein
        /// (Etappe D5a, Konzept_KonfigUI_Hydraulik Anforderung 6).
        ///
        /// Zwei Wirkungen:
        ///
        ///   1. <c>Kaskadenkontext.QuellpufferJeAnlage</c> — Grundlage der RECHENEBENEN
        ///      der Kaskadenschleife: Ein Erzeuger mit Quellpuffer rechnet nach dem
        ///      Erzeuger, der diesen Puffer lädt.
        ///   2. Beim HEIZKESSEL zusätzlich die Eintrittstemperatur: Der Puffer liefert
        ///      einen Teil des Temperaturhubs, und genau um diesen Anteil sinkt der
        ///      Brennstoffbedarf (Formel siehe <c>SimulationSPK</c>).
        ///
        /// Für die WÄRMEPUMPE ändert sich hier nichts an der Physik — ihr Quellbezug
        /// existiert seit Paket 2 (Verdampferwärme aus dem Quellspeicher). Neu ist allein
        /// die Reihenfolge: Bis D5a rechnete eine WP mit Quellpuffer VOR dessen Lader und
        /// sah deshalb den Füllstand der Vorstunde.
        ///
        /// <para><b>NUR WÄRMEPUMPE UND HEIZKESSEL</b> (Nacharbeit E-K2-2). Eine
        /// Rechenebene &gt; 0 darf nur bekommen, wessen Modul eine EBENENMASKE auswertet —
        /// und das sind allein diese beiden (<c>ModulEbenen</c>/<c>EbeneAktiv</c> in
        /// <c>SimulationWaermepumpe</c> und <c>SimulationSPK</c>). Stünde eine
        /// Solarthermie- oder BHKW-Anlage auf einer höheren Ebene, nähme
        /// <c>BedarfsordnungJeEbeneBilden</c> ihre ART auf beiden Ebenen auf, und
        /// <c>Stunde_Bedarf</c> liefe ZWEIMAL in derselben Stunde: beim BHKW eine echte
        /// Doppelproduktion (Fahrweise, Direktdeckung, Reservierung je zweimal). Das
        /// Konzept schränkt die Puffer-Quelle ohnehin auf Wärmepumpe und Heizkessel ein
        /// (Abschnitt 4, Anforderungen 5/6).</para>
        ///
        /// <para><b>KURZSCHLUSS QUELLE = EIGENE SENKE</b> (Nacharbeit E-K2-1). Konzept 4.6
        /// verbietet die Konstellation; Altdaten können sie tragen, und für den Kessel
        /// kann der Dialog sie heute nicht verhindern (die Quellen-Spalte kommt mit D5b).
        /// Der Zyklus-Guard der Ebenen greift hier ausdrücklich nicht
        /// (<c>EbenenRelaxieren</c> überspringt den Selbstbezug). Der Quellbezug wird
        /// deshalb gar nicht erst eingerichtet — dieselbe Wirkung wie bei der Wärmepumpe,
        /// die ihre eigene, getrennte Quellinstanz behält (siehe
        /// <see cref="QuellspeicherUebernehmen"/>).</para>
        ///
        /// Dialogfrei (Konzept 13.4).
        /// </summary>
        private void QuellbezuegeAufbauen(Kaskadenkontext kontext)
        {
            if (kontext == null) return;

            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, ID_Type, Bezeichner, WQ_Typ, WQ_ID_Puffer " +
                "FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND ID_Type IN (" + ProjektPuffer.WAERMEERZEUGER_TYPEN + ") " +
                "ORDER BY Prioritaet, ID",
                StilleDb.Par("@proj", OleDbType.Integer, m_ID_Projekt));
            if (dt == null) return;

            foreach (DataRow r in dt.Rows)
            {
                if (!string.Equals(StilleDb.Text(StilleDb.Feld(r, "WQ_Typ")),
                                   WaermequelleClass.TYP_PUFFER, StringComparison.Ordinal))
                    continue;

                int idAnlage = StilleDb.Zahl(StilleDb.Feld(r, "ID"));
                int idType = StilleDb.Zahl(StilleDb.Feld(r, "ID_Type"));
                int idPuffer = StilleDb.Zahl(StilleDb.Feld(r, "WQ_ID_Puffer"));
                if (idAnlage <= 0 || idPuffer <= 0) continue;

                // E-K2-2: Rechenebenen nur für Arten mit Modulmaske.
                if (idType != ProjektPuffer.TYP_WP && idType != ProjektPuffer.TYP_KESSEL)
                {
                    Protokoll.WarnungEinmal("quellpuffer-art-ohne-ebene-" + idAnlage,
                        "Wärmequelle Pufferspeicher: Die Anlage " + idAnlage + " ist weder " +
                        "Wärmepumpe noch Heizkessel, führt aber einen Pufferspeicher als " +
                        "Wärmequelle. Für diese Erzeugerart gibt es keinen Quellbezug " +
                        "(Konzept Abschnitt 4) - der Eintrag bleibt WIRKUNGSLOS und die " +
                        "Anlage rechnet unverändert an ihrer Kaskadenposition.");
                    continue;
                }

                // E-K2-1: Quelle = eigene Senke ist der Kurzschluss aus Konzept 4.6.
                if (IstEigenerSenkenPuffer(idAnlage, idPuffer))
                {
                    Protokoll.WarnungEinmal("quelle-gleich-eigene-senke-" + idAnlage,
                        "Wärmequelle Pufferspeicher: Die Anlage " + idAnlage + " bezieht ihre " +
                        "Wärme aus Puffer " + idPuffer + ", den sie selbst als Senke lädt " +
                        "(Kurzschluss, Konzept 4.6). Sie würde Wärme im Kreis pumpen; der " +
                        "Quellbezug bleibt deshalb WIRKUNGSLOS. Bitte die Wärmequelle oder " +
                        "die Wärmesenke dieser Anlage ändern.");
                    continue;
                }

                SimulationPufferspeicher sp = QuellspeicherInstanz(idPuffer, idAnlage);
                if (sp == null) continue;

                kontext.QuellpufferJeAnlage[idAnlage] = sp;

                if (idType == ProjektPuffer.TYP_KESSEL) KesselQuellbezugSetzen(idAnlage, sp);
            }

            KesselQuelleOhneWirkungMelden();
        }

        /// <summary>
        /// Meldet, wenn der Heizkessel ALLEIN wegen einer Puffer-Quelle in der
        /// Stundenschleife rechnet, aber kein einziger Quellbezug zustande gekommen ist
        /// (Nacharbeit E-K2-4).
        ///
        /// Die Gründe stehen einzeln schon im Protokoll (Puffer rechnet nicht mit, kein
        /// Temperaturpaar, Quelle zu kalt, Kurzschluss). Was ohne diese Zeile fehlte, ist
        /// die FOLGE: Der Kessel rechnet trotzdem in der Stundenschleife statt als
        /// Vektorstufe — ein Unterschied, den sonst nur ein Zahlenvergleich zeigt.
        /// </summary>
        private void KesselQuelleOhneWirkungMelden()
        {
            if (!_kesselNurWegenQuelle || simulation_spk == null) return;

            for (int i = 0; i < simulation_spk.KesselAnzahl; i++)
                if (simulation_spk.QuellAnteil(i) > 0) return;      // mindestens einer wirkt

            Protokoll.WarnungEinmal("kessel-quelle-ohne-wirkung",
                "Kessel-Kaskade: Die Heizkessel dieses Projekts führen einen Pufferspeicher " +
                "als Wärmequelle, aber KEIN Quellbezug ist zustande gekommen (Gründe siehe " +
                "die Meldungen darüber). Die Kessel rechnen deshalb ohne Kaskade - aber, " +
                "weil die Quelle konfiguriert ist, innerhalb der gemeinsamen " +
                "Speicherstufe statt als eigene Vektorstufe. Das kann die Zahlen gegenüber " +
                "einem Lauf ohne Quellenangabe verändern; die Wärmequelle ist zu " +
                "bereinigen oder zu vervollständigen.");
        }

        /// <summary>
        /// Die Speicherinstanz zu einer Quell-Puffer-ID, so wie sie in DIESEM Lauf
        /// rechnet: erst die Registry, dann die Zusatzspeicher (Kurzschluss-Fall), dann
        /// die eigens aufgebauten Quellinstanzen der WP-Module. <c>null</c>, wenn der
        /// Puffer im Lauf nicht mitrechnet.
        /// </summary>
        private SimulationPufferspeicher QuellspeicherInstanz(int idPuffer, int idAnlage)
        {
            SimulationPufferspeicher sp;
            if (speicherRegistry.TryGetValue(idPuffer, out sp) && sp != null && sp.ImRechenpfad)
                return sp;

            foreach (SimulationPufferspeicher z in _zusatzSpeicher)
                if (z != null && z.ID_Pufferspeicher == idPuffer && z.ID_Anlage == idAnlage) return z;

            if (simulation_wp != null && simulation_wp.Quellspeicher != null)
                foreach (SimulationPufferspeicher q in simulation_wp.Quellspeicher)
                    if (q != null && q.ID_Pufferspeicher == idPuffer) return q;

            return null;
        }

        /// <summary>
        /// Richtet den Quellbezug EINES Heizkessels ein: Anteil der Nutzwärme, den der
        /// Puffer über seinen Temperaturhub beisteuert (Etappe D5a).
        ///
        /// <code>
        ///   Anteil = (T_Quelle − T_Rücklauf) / (T_Vorlauf − T_Rücklauf)
        /// </code>
        ///
        /// <b>T_Quelle</b> ist die VORLAUFtemperatur des Quellpuffers — die Temperatur,
        /// mit der er liefert. Wie viel Wärme dahinter steht, begrenzt ohnehin sein
        /// <c>Q_max</c>, das aus derselben Spreizung gebildet ist; eine zweite Absenkung
        /// über die Mitteltemperatur wäre eine doppelte Vorsicht.
        ///
        /// <b>T_Vorlauf/T_Rücklauf</b> — das Temperaturpaar, über das der Kessel anheben
        /// muss — in einer VORRANGKETTE nach demselben Muster wie bei den Puffern
        /// (Konzept 5.1):
        ///
        ///   1. das Paar des Kessels selbst (<c>Tab_Heizkessel</c>),
        ///   2. das Paar seines SENKENpuffers (was er lädt, muss er auf dessen
        ///      Vorlauf bringen),
        ///   3. kein Paar → kein Quellbezug, mit Protokollhinweis. „Sichtbar falsch ist
        ///      besser als still falsch": Ohne Temperaturen ist der Hub nicht bestimmbar,
        ///      und ein geratener Anteil wäre eine Ergebnisänderung ohne Datengrundlage.
        /// </summary>
        private void KesselQuellbezugSetzen(int idAnlage, SimulationPufferspeicher quelle)
        {
            if (!_kesselInSchleife || simulation_spk == null) return;

            int index = simulation_spk.spk_anlagen_ids.IndexOf(idAnlage);
            if (index < 0 || index >= simulation_spk.KesselAnzahl) return;

            WaermesenkeClass.PufferInfo qp = WaermesenkeClass.PufferLesen(quelle.ID_Pufferspeicher);
            double tQuelle = (qp != null) ? qp.Vorlauf : 0;

            int vorlauf, ruecklauf;
            if (tQuelle <= 0 || !KesselTemperaturpaar(idAnlage, index, out vorlauf, out ruecklauf))
            {
                Protokoll.WarnungEinmal("kessel-quelle-ohne-temperaturen-" + idAnlage,
                    "Kessel-Kaskade: Die Anlage " + idAnlage + " bezieht ihre Wärme aus " +
                    "Puffer " + quelle.ID_Pufferspeicher + " (" + quelle.BezeichnerAnzeige() +
                    "), aber das Temperaturpaar für den Hub ist nicht bestimmbar " +
                    "(Puffer-Vorlauf " + tQuelle.ToString("0.#") + " °C, kein Vor-/Rücklauf " +
                    "am Kessel und an seiner Senke). Der Quellbezug bleibt WIRKUNGSLOS - " +
                    "der Kessel rechnet mit vollem Brennstoffbedarf.");
                return;
            }

            double anteil = (tQuelle - ruecklauf) / (double)(vorlauf - ruecklauf);
            if (anteil <= 0)
            {
                Protokoll.HinweisEinmal("kessel-quelle-zu-kalt-" + idAnlage,
                    "Kessel-Kaskade: Puffer " + quelle.ID_Pufferspeicher + " liefert " +
                    tQuelle.ToString("0.#") + " °C und damit nicht mehr als der " +
                    "Systemrücklauf (" + ruecklauf + " °C) des Kessels " + idAnlage +
                    ". Der Quellbezug bleibt wirkungslos.");
                return;
            }

            if (anteil > 1) anteil = 1;
            simulation_spk.QuellbezugSetzen(index, quelle, anteil);

            Protokoll.Hinweis("Kessel-Kaskade: Anlage " + idAnlage + " bezieht ihre " +
                              "Eintrittstemperatur aus Puffer " + quelle.ID_Pufferspeicher +
                              " (" + quelle.BezeichnerAnzeige() + ", " + tQuelle.ToString("0.#") +
                              " °C). Hub des Kessels " + ruecklauf + "/" + vorlauf + " °C; der " +
                              "Puffer trägt " + (anteil * 100).ToString("0.#") + " % der " +
                              "Nutzwärme, um genau diesen Anteil sinkt der Brennstoffbedarf. " +
                              "Der Kessel rechnet NACH dem Erzeuger, der den Puffer lädt.");
        }

        /// <summary>
        /// Temperaturpaar eines Kessels nach der Vorrangkette aus
        /// <see cref="KesselQuellbezugSetzen"/>.
        ///
        /// <para>NACHARBEIT I-K3: Stufe 1 verknüpft über die ID, nicht über den
        /// Bezeichner. <c>Tab_Energieanlagen.ID_Kessel</c> zeigt auf
        /// <c>Tab_Heizkessel.ID</c> — das ist die vorhandene Beziehung, und
        /// <c>CLAUDE.md</c> verlangt für neue Verknüpfungen ausdrücklich IDs. Über den
        /// Bezeichner hätten zwei gleichnamige Kessel desselben Projekts das
        /// Temperaturpaar des falschen geliefert und damit einen falschen Quellanteil.</para>
        /// </summary>
        private bool KesselTemperaturpaar(int idAnlage, int index, out int vorlauf, out int ruecklauf)
        {
            vorlauf = 0;
            ruecklauf = 0;

            // 1. Der Kessel selbst, über Tab_Energieanlagen.ID_Kessel. Still gelesen: Auf
            //    einem alten Schema kann die Spalte fehlen - dann greift Stufe 2, statt
            //    dass ein Dialog den Lauf anhält.
            int idKessel = StilleDb.Zahl(StilleDb.Scalar(
                "SELECT ID_Kessel FROM Tab_Energieanlagen WHERE ID = ?",
                StilleDb.Par("@id", OleDbType.Integer, idAnlage)));

            DataTable dt = (idKessel > 0)
                ? StilleDb.Tabelle("SELECT Vorlauf, Ruecklauf FROM Tab_Heizkessel WHERE ID = ?",
                                   StilleDb.Par("@id", OleDbType.Integer, idKessel))
                : null;

            if (dt != null && dt.Rows.Count > 0)
            {
                int v = StilleDb.Zahl(StilleDb.Feld(dt.Rows[0], "Vorlauf"));
                int r = StilleDb.Zahl(StilleDb.Feld(dt.Rows[0], "Ruecklauf"));
                if (ProjektPuffer.IstTemperaturpaar(v, r)) { vorlauf = v; ruecklauf = r; return true; }
            }

            // 2. Der SENKENpuffer des Kessels.
            Senkenzuordnung z = simulation_spk.KesselSenke(index);
            if (z != null)
            {
                foreach (int idPuffer in new[] { z.IDPufferHaupt, z.IDPufferZweit })
                {
                    if (idPuffer <= 0) continue;

                    WaermesenkeClass.PufferInfo p = WaermesenkeClass.PufferLesen(idPuffer);
                    if (p == null || !ProjektPuffer.IstTemperaturpaar(p.Vorlauf, p.Ruecklauf)) continue;

                    vorlauf = p.Vorlauf;
                    ruecklauf = p.Ruecklauf;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Was Kombispeicher und Kessel-Quellbezug im EINKANALIGEN Altpfad bedeuten
        /// (Etappe D5a) — beide sind zweikanalige Erweiterungen, und der Altpfad bleibt
        /// als Rückfallebene unverändert.
        ///
        /// <para><b>Kombispeicher:</b> Der Altpfad kennt nur EINEN Bedarfsvektor und
        /// damit keine zwei Kanäle, zwischen denen ein Vorrat aufzuteilen wäre. Ein
        /// Puffer mit Verwendung „Kombi" rechnet dort wie ein HEIZUNGS-Puffer — dieselbe
        /// Behandlung, die er über <c>IstBrauchwasserkanal = false</c> ohnehin schon
        /// bekäme. Das ist eine dokumentierte Vereinfachung, kein Rechenfehler: Auf einer
        /// Bedarfssumme ist „Heizung + Warmwasser aus einem Vorrat" genau das, was
        /// passiert.</para>
        ///
        /// <para><b>Kessel-Quellbezug:</b> unwirksam. Die Eintrittstemperatur aus einem
        /// Puffer verlangt eine gemeinsame Speicherstufe mit Rechenreihenfolge — beides
        /// gibt es nur im zweikanaligen Weg. Der Kessel rechnet mit vollem
        /// Brennstoffbedarf wie bisher.</para>
        ///
        /// <para><b>PAKET BHKW-REGULÄR — Geltungsbereich:</b> Diese Hinweise erreichen nur
        /// noch Projekte OHNE BHKW. Sobald ein BHKW in <c>Tool_1..4</c> steht, schickt die
        /// Weiche den Lauf ausnahmslos in die Speicherstufe, und der Altpfad wird gar
        /// nicht betreten. Ein BHKW-Hinweis fehlt hier deshalb nicht — er wäre
        /// unerreichbar.</para>
        /// </summary>
        private void AltpfadHinweiseD5a()
        {
            int kombi = StilleDb.Zahl(StilleDb.Scalar(
                "SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND Verwendung = ?",
                StilleDb.Par("@proj", OleDbType.Integer, m_ID_Projekt),
                StilleDb.Par("@verw", OleDbType.VarWChar, WaermesenkeClass.VERWENDUNG_KOMBI)));

            if (kombi > 0)
                Protokoll.HinweisEinmal("altpfad-kombispeicher",
                    "Kombispeicher: Das Projekt führt " + kombi + " Speicher mit Verwendung „" +
                    WaermesenkeClass.VERWENDUNG_KOMBI + "\". Dieser Lauf rechnet EINKANALIG " +
                    "(Kaskade_Zweikanalig ist nicht gesetzt) und kennt keine getrennten " +
                    "Kanäle - der Kombispeicher wird deshalb wie ein HEIZUNGSPUFFER " +
                    "behandelt. Für die gemeinsame Deckung von Heizung und Warmwasser aus " +
                    "einem Vorrat den zweikanaligen Rechenweg einschalten.");

            int kesselQuelle = StilleDb.Zahl(StilleDb.Scalar(
                "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ? " +
                "AND WQ_Typ = ? AND WQ_ID_Puffer IS NOT NULL",
                StilleDb.Par("@proj", OleDbType.Integer, m_ID_Projekt),
                StilleDb.Par("@typ", OleDbType.Integer, ProjektPuffer.TYP_KESSEL),
                StilleDb.Par("@wq", OleDbType.VarWChar, WaermequelleClass.TYP_PUFFER)));

            if (kesselQuelle > 0)
                Protokoll.HinweisEinmal("altpfad-kessel-quellpuffer",
                    "Kessel-Kaskade: " + kesselQuelle + " Heizkessel dieses Projekts haben " +
                    "einen Pufferspeicher als Wärmequelle. Dieser Lauf rechnet EINKANALIG " +
                    "(Kaskade_Zweikanalig ist nicht gesetzt); der Quellbezug bleibt dort " +
                    "WIRKUNGSLOS - die Kessel rechnen mit vollem Brennstoffbedarf. Für die " +
                    "Kaskade den zweikanaligen Rechenweg einschalten.");
        }

        /// <summary>
        /// true, wenn <paramref name="idPuffer"/> die eigene Senke der Anlage ist — der
        /// KURZSCHLUSS aus Konzept 4.6 (Quelle = Senke derselben Anlage), den der Dialog
        /// blockiert, Altdaten aber tragen können. Er bleibt vom Kaskadenweg der
        /// Etappe D5a ausgenommen.
        /// </summary>
        private bool IstEigenerSenkenPuffer(int idAnlage, int idPuffer)
        {
            if (idAnlage <= 0 || idPuffer <= 0) return false;

            DataTable dt = StilleDb.Tabelle(
                "SELECT WS_ID_Puffer, WS_ID_Puffer2 FROM Tab_Energieanlagen WHERE ID = ?",
                StilleDb.Par("@id", OleDbType.Integer, idAnlage));
            if (dt == null || dt.Rows.Count == 0) return false;

            return StilleDb.Zahl(StilleDb.Feld(dt.Rows[0], "WS_ID_Puffer")) == idPuffer ||
                   StilleDb.Zahl(StilleDb.Feld(dt.Rows[0], "WS_ID_Puffer2")) == idPuffer;
        }

        /// <summary>
        /// IDs aller Projekt-Puffer, die im Projekt als SENKE referenziert sind:
        /// <c>WS_ID_Puffer</c> und <c>WS_ID_Puffer2</c> der Anlagen (in
        /// Kaskadenreihenfolge) sowie <c>ID_Pufferspeicher</c> der Alt-Zuordnungen.
        /// Doppelte Nennungen sind unschädlich — der Aufrufer überspringt bekannte IDs.
        ///
        /// Dialogfrei über <see cref="StilleDb"/> (Konzept 13.4): Eine fehlende Spalte
        /// auf einem alten Schema liefert hier <c>null</c> statt einer MessageBox mitten
        /// im Rechenlauf.
        /// </summary>
        private List<int> ReferenzierteSenkenPuffer(Z_ProjektPufferSpCtrl zuordnungen)
        {
            List<int> ids = SenkenPufferDerAnlagen();

            if (zuordnungen != null)
                for (int n = 0; n < zuordnungen.rows; n++)
                    ids.Add(zuordnungen.items[n].ID_Pufferspeicher);

            return ids;
        }

        /// <summary>
        /// IDs der Puffer, die eine Projektanlage als SENKE führt — <c>WS_ID_Puffer</c>
        /// und <c>WS_ID_Puffer2</c>, in Kaskadenreihenfolge.
        ///
        /// Getrennt von <see cref="ReferenzierteSenkenPuffer"/>, weil die beiden Mengen
        /// verschiedene Fragen beantworten: Für die REGISTRY zählt „gehört zum Projekt"
        /// (dazu gehören auch die Alt-Zuordnungen aus <c>Z_ProjektPufferSp</c>), für den
        /// RECHENPFAD dagegen „kann ihn ein Erzeuger laden" — und das entscheidet allein
        /// die Senkenreferenz der Anlage (<see cref="RegistryFuerZweikanaligOeffnen"/>).
        /// </summary>
        private List<int> SenkenPufferDerAnlagen()
        {
            List<int> ids = new List<int>();

            DataTable dt = StilleDb.Tabelle(
                "SELECT WS_ID_Puffer, WS_ID_Puffer2 FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND ID_Type IN (" + ProjektPuffer.WAERMEERZEUGER_TYPEN + ") " +
                "ORDER BY Prioritaet, ID",
                StilleDb.Par("@proj", OleDbType.Integer, m_ID_Projekt));

            if (dt != null)
            {
                foreach (DataRow r in dt.Rows)
                {
                    ids.Add(StilleDb.Zahl(StilleDb.Feld(r, "WS_ID_Puffer")));
                    ids.Add(StilleDb.Zahl(StilleDb.Feld(r, "WS_ID_Puffer2")));
                }
            }

            return ids;
        }

        private float[] Simulation_WP_Ctrl(float[] Waermebedarf, float[] Strombedarf, bool bHeizstab)
        {
            RecordSet rs = new RecordSet();

            // Neue Spalten (Prioritaet, WQ_*) bei Bedarf anlegen und die WPs in
            // der eingestellten Prioritätsreihenfolge einsetzen (Kaskade).
            WaermequelleClass.SchemaSicherstellen();

            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.WP_TYP + " order by Prioritaet, ID");

            simulation_wp.wp_list.Clear();
            
            while (rs.Next())
            {
                simulation_wp.wp_list.Add((int)rs.Read("ID"));
            }
            rs.Close();

            simulation_wp.Temperatur = Stundentemperatur;
            simulation_wp.Waermebedarf_stuendlich = Waermebedarf;
            simulation_wp.PV_Ueberschuss_stuendlich = PV_Ueberschuss_Vorabberechnen();
            // Warmwasseranteil für die Wärmesenken-Zuordnung der Module
            simulation_wp.Warmwasserbedarf_stuendlich =
                simulation_Waermebedarf != null ? simulation_Waermebedarf.brauchwasserwerte : null;
            simulation_wp.WP_Strombedarf_stuendlich = Strombedarf;
            simulation_wp.Mit_Heizstab = bHeizstab;
            
            // Simulation starten
            m_bError = !simulation_wp.Berechnung();

            // PAKET 8 (Konzept 13.4): Der Grund eines Abbruchs geht dialogfrei über den
            // Fehlerkanal - fehlende Kennlinie zum gewählten Vorlauf oder verbotene
            // Extrapolation. Bis dahin zeigte das Modul dafür eine MessageBox und der
            // Aufrufer sah nur ein stilles m_bError.
            // Nacharbeit N11: sammelnd statt überschreibend - im Altpfad läuft nach der
            // Wärmepumpe noch der Kessel, und dessen Meldung hat die der WP verdrängt.
            if (m_bError) FehlertextAufnehmen(simulation_wp.Fehlertext);

            // Quellspeicher der Module in die Registry übernehmen (Konzept 6.2).
            // Erst JETZT, weil sie beim Modulaufbau entstehen - und mit denselben
            // Instanzen, nicht mit Kopien: Genau das ist die geforderte Ablösung der
            // parallelen Liste wp_quellspeicher.
            QuellspeicherUebernehmen(false);

            return  m_bError ? Waermebedarf : simulation_wp.waermerestbedarf_stuendlich;
        }

        /// <summary>
        /// Ermittelt den stündlichen PV-Überschuss [kW] für den Betriebsmodus
        /// "PV-optimiert" der Wärmepumpe. Da die Photovoltaik erst nach den
        /// Wärmeerzeugern gerechnet wird, wird ihr wetterabhängiges Potenzial
        /// hier vorab bestimmt und um den übrigen Strombedarf reduziert.
        /// Liefert null, wenn keine Wärmepumpe im PV-Modus betrieben wird.
        /// </summary>
        private float[] PV_Ueberschuss_Vorabberechnen()
        {
            // Läuft überhaupt eine Wärmepumpe im PV-Modus?
            bool pvModus = false;
            RecordSet rs = new RecordSet();
            rs.Open("select ID from Tab_Energieanlagen where ID_Projekt=" + m_ID_Projekt +
                    " and ID_Type=" + WizardItemClass.WP_TYP);
            while (rs.Next())
            {
                string modus = WaermequelleClass.WertLesen((int)rs.Read("ID"), "BM_Typ") as string;
                if (modus == WaermequelleClass.MODUS_PV) { pvModus = true; break; }
            }
            rs.Close();

            if (!pvModus || tool == null || tool.Length < 5 || tool[4] != DbWerte.ERZEUGER_PHOTOVOLTAIK) return null;

            try
            {
                // PV-Potenzial vorab bestimmen (wetterabhängig, unabhängig vom Bedarf)
                simulation_pv.m_ID_Projekt = m_ID_Projekt;
                simulation_pv.Strombedarf = (float[])Rest_Strombedarf_viertelstuendlich.Clone();
                simulation_pv.Berechnung(m_ID_Projekt);

                float[] potenzial = (float[])simulation_pv.pvPotentialGesamt_stuendlich.Clone();
                float[] bedarf = Viertelstunden_zu_Stundenwerte_Mittelwert(Rest_Strombedarf_viertelstuendlich);

                float[] ueberschuss = new float[8760];
                for (int i = 0; i < 8760; i++)
                {
                    float rest = potenzial[i] - (i < bedarf.Length ? bedarf[i] : 0);
                    ueberschuss[i] = rest > 0 ? rest : 0;
                }

                // Zustand der PV-Simulation zurücksetzen - sie wird später regulär gerechnet
                simulation_pv.Init();
                return ueberschuss;
            }
            catch (Exception ex)
            {
                // Protokollkanal-Nachzug: WARNUNG - die Wärmepumpe im Modus
                // „PV-optimiert" rechnet ohne den vorab bestimmten Überschuss weiter.
                Protokoll.Warnung("PV-Überschuss konnte nicht vorab bestimmt werden: " + ex.Message +
                                  " - die Wärmepumpe rechnet ohne PV-Vorrang.");
                simulation_pv.Init();
                return null;
            }
        }

        // PAKET BHKW-REGULÄR (Entscheidung des Anwenders 17.08.2026, revidiert 6-1):
        // Hier stand Simulation_BHKW_Ctrl - der einkanalige BHKW-Aufrufer des Altpfads.
        // Er ist ersatzlos entfallen; kein Lauf erreicht ihn mehr, weil BHKW-Projekte
        // ausnahmslos über die Speicherstufe rechnen (BHKW_Liste_Laden,
        // Simulation_BHKW_Ctrl_Zweikanalig, Speicherstufe_Rechnen).
        //
        // WAS MIT IHM VERSCHWINDET, und warum das gewollt ist:
        //
        //   - die RecordSet-Abfrage der BHKW-Anlagen (in neuem Code unerwünscht; der
        //     zweikanalige Weg liest sie parametrisiert über StilleDb),
        //   - die Bilanz „Restwärme = Bedarf − Produktion" als Vektordifferenz. Sobald
        //     das BHKW einen Speicher lädt, ist sie falsch: Geladene Wärme deckt noch
        //     keinen Bedarf (Bilanzfehler aus Konzept 6.5). Genau dieser Punkt war der
        //     Anlass des Pakets,
        //   - die Befüllung von kapazitaetPendelspeicher aus VolumenPendelspeicherBHKW
        //     (Liter · 20 / 860 [kWh], feste 20 K Spreizung). Der Speicherraum kommt
        //     jetzt aus einem SimulationPufferspeicher mit Hysterese,
        //     Bereitschaftsverlusten und ΔT-Spreizung aus den Puffer-Parametern
        //     (BhkwErsatzspeicherAufnehmen).
        //
        // Das FELD kapazitaetPendelspeicher in SimulationBHKW bleibt bestehen: Es ist
        // der generische Speicherraum-Skalar der gemeinsamen Motorläufe, über den der
        // zweikanalige Weg seinen Stufenspeicher spiegelt (Fahrweise_Stunde).

        private float[] Simulation_SPK_Ctrl(float[] Waermebedarf, float[] Strombedarf, int nBereitschaft)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.KESSEL_TYP);
   
            simulation_spk.spk_list.Clear();
            // Anlagen-IDs parallel zu spk_list (Konzept 6.2). spk_list bleibt die Liste
            // der BEZEICHNER: Sie ist zugleich der Modulname der Ergebniszeile
            // (SimulationRunner) und der Suchschlüssel der Kesseldaten - beides bleibt
            // unangetastet. Die ID ist der Schlüssel, den Etappe 4b für Senke,
            // Ladepriorität und Speicherzuordnung braucht.
            simulation_spk.spk_anlagen_ids.Clear();
            while (rs.Next())
            {
                simulation_spk.spk_list.Add((string)rs.Read("Bezeichner"));
                simulation_spk.spk_anlagen_ids.Add((int)rs.Read("ID"));
                //simulation_spk.spk_carrier.TryAdd((string)rs.Read("Bezeichner"), (int)rs.Read("ID_Carrier"));
            }
            rs.Close();

            simulation_spk.Waermebedarf = Waermebedarf;
            simulation_spk.Strombedarf_stuendlich = Strombedarf;
            simulation_spk.Vorgabe_Betriebsbereitschaft = nBereitschaft;
            
            // Simulation starten
            //
            // PAKET 8 (Konzept 13.4): Bricht das Modul ab (Kessel im Projekt nicht
            // hinterlegt, B0-3), meldete es das bisher als MessageBox - und der Altpfad
            // rechnete danach mit der GENULLTEN Restwärme weiter, weil Init() sie
            // geleert hat. Der Anwender sah einen vollständig aussehenden Lauf, in dem
            // der Wärmebedarf verschwunden war. Jetzt wird der Grund weitergereicht;
            // SimulationRunner speichert einen solchen Lauf nicht mehr, und die
            // Detailansicht zeigt den Text.
            // Nacharbeit N11: sammelnd statt überschreibend.
            if (!simulation_spk.Berechnung(m_ID_Projekt))
                FehlertextAufnehmen(simulation_spk.Fehlertext);

            return simulation_spk.Restwaerme;
        }

        private float[] Simulation_Solarthermie_Ctrl(float[] Waermebedarf)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SOLAR_TYP);

            simulation_solarthermie.solarthermie_list.Clear();
            while (rs.Next())
            {
                simulation_solarthermie.solarthermie_list.Add((int)rs.Read("ID_SOLAR"));
            }
            rs.Close();

            simulation_solarthermie.Waermebedarf = Array.ConvertAll<float, double>(Waermebedarf, x => (double)x);

            // Simulation starten
            simulation_solarthermie.Berechnung(m_ID_Projekt);

            return Array.ConvertAll<double, float>(simulation_solarthermie.Restwaerme, x => (float)x);
        }

        public float[] AddVectors(float[] array1, float[] array2)
        {
            if (array1.Length != array2.Length)
                throw new ArgumentException("Arrays must be of the same length.");

            float[] result = new float[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            {
                result[i] = array1[i] + array2[i];
            }
            return result;
        }

        public float[] SubVectors(float[] array1, float[] array2, bool korrigiert=true)
        {
            if (array1.Length != array2.Length)
                throw new ArgumentException("Arrays must be of the same length.");

            float[] result = new float[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            {
                if (korrigiert)
                {
                    if (array1[i] >= array2[i])
                        result[i] = array1[i] - array2[i];
                    else result[i] = 0;
                }
                else result[i] = array1[i] - array2[i];
            }
            return result;
        }

        public float[] Stundenwerte_zu_viertelstunden(float[] stundenwerte)
        {
            float[] viertelstundenwerte = new float[stundenwerte.Length * 4];
            for (int i = 0; i < stundenwerte.Length; i++)
            {
                viertelstundenwerte[i * 4] = stundenwerte[i];
                viertelstundenwerte[i * 4 + 1] = stundenwerte[i];
                viertelstundenwerte[i * 4 + 2] = stundenwerte[i];
                viertelstundenwerte[i * 4 + 3] = stundenwerte[i];
            }
            return viertelstundenwerte;
        }

        public float[] Viertelstunden_zu_Stundenwerte_Mittelwert(float[] viertelstundenwerte)
        {
            // Die Länge des neuen Arrays ist genau ein Viertel des Originals
            float[] stundenwerte = new float[viertelstundenwerte.Length / 4];

            for (int i = 0; i < stundenwerte.Length; i++)
            {
                // Die 4 Viertelstunden einer Stunde zusammenrechnen und den Durchschnitt bilden
                float summe = viertelstundenwerte[i * 4] +
                              viertelstundenwerte[i * 4 + 1] +
                              viertelstundenwerte[i * 4 + 2] +
                              viertelstundenwerte[i * 4 + 3];

                stundenwerte[i] = summe / 4f;
            }

            return stundenwerte;
        }

        private float[] Simulation_Photovoltaik_Ctrl(float[] Strombedarf)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.PV_TYP);

            simulation_pv.photovoltaik_list.Clear();
            while (rs.Next())
            {
                simulation_pv.photovoltaik_list.Add((int)rs.Read("ID_PV"));
            }
            rs.Close();

            simulation_pv.Strombedarf = Strombedarf;

            // Simulation starten
            float[] temp = simulation_pv.Berechnung(m_ID_Projekt);

            TestePVAnlage();

            return temp;
        }

        /// <summary>
        /// ENTWICKLER-SELBSTTEST der PV-Formel (Kategorie c des Protokollkanal-Nachzugs):
        /// feste Prüfparameter, kein Projektbezug, keine Anwenderaussage. Bleibt deshalb
        /// bewusst reine <c>Console.WriteLine</c>-Ausgabe und geht NICHT über
        /// <see cref="SimulationProtokoll"/>.
        ///
        /// ACHTUNG bei Änderungen an den Texten: <c>Referenzlauf/Protokoll.cs</c> zählt
        /// jede Kindprozesszeile mit dem Token „WARNUNG:" als Warnung. Der Zweig unten
        /// trägt es und schlägt im Fehlerfall im Laufprotokoll als Warnung durch — er
        /// greift nur, wenn die Formel wirklich falsch rechnet (in allen dokumentierten
        /// Referenzläufen bisher nie).
        /// </summary>
        public void TestePVAnlage()
        {
            // Test-Parameter für eine 10 kWp Anlage
            double testStrahlung = 1000.0; // W/m² (STC-Bedingung)
            double flaeche = 50.0;          // ca. 50m² für 10 kWp
            double wirkungsgrad = 0.20;     // 20% Wirkungsgrad
            double tempKoeff = -0.004;      // -0.4%/K
            double tAmb = 25.0;             // 25°C Luft
            double cosTheta = 1.0;          // Sonne steht perfekt senkrecht
            double strombedarf = 100.0;     // Hoher Bedarf, damit wir Produktion nicht kappen

            var ergebnis = simulation_pv.BerechnePV(strombedarf, testStrahlung, flaeche, wirkungsgrad, tempKoeff, tAmb, cosTheta);

            if (ergebnis.produktion < 8.0)
            {
                Console.WriteLine("WARNUNG: Der Wert ist zu niedrig für 10kWp bei 1000W/m²!");
                Console.WriteLine("Prüfe: Ist h0 wirklich 0.20? Wird flaeche korrekt übergeben?");
            }
        }

        /// <summary>
        /// Rechnet die aktive Speichervariante über die <c>SpeicherEngine</c> und
        /// liefert die ENTLADUNG je Viertelstunde als Leistung [kW] — oder
        /// <c>null</c>, wenn nicht gerechnet wurde.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Ersetzt den wirkungslosen <c>SimulationSSP</c>-Stub (AP2b, Fachkonzept 8.2,
        /// Rudiment 1). Gerechnet wird die Anlagenzeile der <b>aktiven Speichervariante</b>
        /// (AP9b, Fachkonzept 7.3) mit der Berechnungsart, die diese Variante vorgibt;
        /// nur ohne bestimmbare aktive Variante fällt der Lauf auf die Aggregation über
        /// alle <c>SP_TYP</c>-Anlagen zurück (Protokollhinweis). Die Reihen- und
        /// Parameterbeschaffung liegt vollständig in <see cref="StromspeicherSimCtrl"/>,
        /// die Formeln in der Engine.
        /// </para>
        /// <para>
        /// <b>Der Speicher darf den Lauf nicht kippen.</b> Jeder Fehler — fehlende
        /// Stammdaten, Rasterabweichung, Ausnahme aus der Engine — landet als Hinweis
        /// bzw. Warnung im Protokoll; die Kette rechnet dann ohne Speicherwirkung
        /// weiter, genau wie vor diesem Paket. Der Datenzugriff liegt im
        /// dialogfreien Modus (der ganze Lauf steht in
        /// <see cref="DataRepository.EngineModus"/>, Verschachtelung ist zulässig).
        /// </para>
        /// </remarks>
        private float[] Simulation_Stromspeicher_Ctrl(int ID_Projekt)
        {
            StromspeicherSimCtrl ctrl = new StromspeicherSimCtrl();
            SpeicherEngine.SpeicherErgebnis ergebnis;

            try
            {
                ergebnis = ctrl.RechneAktiveVariante(this, ID_Projekt);
            }
            catch (Exception ex)
            {
                Protokoll.Warnung(string.Format(MyResource.Resource.SIMENG_SPEICHER_FEHLGESCHLAGEN, ex.Message));
                return null;
            }

            // Hinweise des Controllers (kein Speicher, keine Kapazität, 1-C-Rückfall)
            // gehören in jedem Fall ins Protokoll - auch wenn gerechnet wurde.
            if (!string.IsNullOrEmpty(ctrl.LetzterHinweis)) Protokoll.Hinweis(ctrl.LetzterHinweis);

            if (ergebnis == null) return null;

            float[] entladung = StromspeicherSimCtrl.EntladungLeistungKw(ergebnis);
            if (entladung.Length != Rest_Strombedarf_viertelstuendlich.Length)
            {
                Protokoll.Warnung(string.Format(MyResource.Resource.SIMENG_SPEICHER_RASTER_ABWEICHUNG,
                                                entladung.Length, Rest_Strombedarf_viertelstuendlich.Length));
                return null;
            }

            Speicherergebnis = ergebnis;
            Speicherkontext = ctrl.LetzterKontext;
            Speicherfuellstand_viertelstuendlich = SpeicherEngine.RasterAdapter.ZuFloat(ergebnis.SoCKwh);
            Speicherfuellstand_stuendlich = Viertelstunden_zu_Stundenwerte_Mittelwert(Speicherfuellstand_viertelstuendlich);

            return entladung;
        }

    }
}

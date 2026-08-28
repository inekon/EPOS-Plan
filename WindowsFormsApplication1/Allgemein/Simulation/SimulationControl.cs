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
        ///   - den SENKEN der Projektanlagen — seit Paket S1 die geordneten Senkenlisten
        ///     (<c>Z_AnlageSenke</c>) samt der noch gespiegelten Altspalten
        ///     <c>WS_ID_Puffer</c>/<c>WS_ID_Puffer2</c>,
        ///   - und den QUELLspeichern der WP-Module, die
        ///     <see cref="WaermequelleClass.Quellspeicher"/> beim Modulaufbau hier
        ///     einträgt (<c>WQ_ID_Puffer</c>).
        ///
        /// Damit löst die Registry die bisher parallele, modulweise Liste
        /// <c>wp_quellspeicher</c> ab (Konzept 6.2, Zusatz der Fassung 12): Die Instanzen
        /// dort und hier sind dieselben, es gibt keine zweite Speicherverwaltung mit
        /// eigener Bilanz mehr.
        ///
        /// PAKET A1: Der frühere BLOCK 1 des Aufbaus — der Senkenspeicher der Wärmepumpe
        /// aus der Alt-Zuordnung <c>Z_ProjektPufferSp</c> — ist ersatzlos entfallen
        /// (Leitentscheidung L1, Schritt 51). Die Registry ist die Menge, über die die
        /// aus der Kaskade gelöste Ladephase (6.3) und Phase G laufen — eingeschränkt auf
        /// die Einträge mit <see cref="SimulationPufferspeicher.ImRechenpfad"/>
        /// (siehe <see cref="RegistryFuerZweikanaligOeffnen"/>).
        /// </summary>
        public Dictionary<int, SimulationPufferspeicher> speicherRegistry =
            new Dictionary<int, SimulationPufferspeicher>();

        /// <summary>
        /// Aufnahmereihenfolge der Registry — <c>Dictionary</c> sichert keine
        /// Reihenfolge zu, <see cref="ErsterHeizpuffer"/> braucht aber genau die. Sie
        /// folgt der Kaskadenreihenfolge der ladenden Anlagen und danach dem Rang der
        /// Senkenzeile.
        /// </summary>
        private readonly List<int> _speicherReihenfolge = new List<int>();

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
        /// Senkenzuordnungen aller Wärmeerzeuger des Projekts (Konzept 6.1), je Lauf
        /// neu geladen — die Fassung mit Haupt- und optionaler Zweitsenke.
        ///
        /// <b>SEIT PAKET S1 nur noch Übergangsbestand:</b> Der dreikanalige Weg rechnet
        /// mit den GEORDNETEN SENKENLISTEN (<see cref="Senkenlisten"/>). Diese Liste
        /// bleibt allein für das BHKW-Modul, das seinen Umbau auf n Senken je Stufe im
        /// eigenen Paket bekommt.
        /// </summary>
        public List<Senkenzuordnung> senkenzuordnungen = new List<Senkenzuordnung>();

        /// <summary>
        /// GEORDNETE SENKENLISTEN aller Wärmeerzeuger des Projekts (Paket S1,
        /// Konzept 5.1) — siehe <see cref="Senkenlisten"/>. <c>null</c> = in diesem Lauf
        /// noch nicht gelesen.
        /// </summary>
        private List<Senkenliste> _senkenlisten;

        /// <summary>
        /// Die geordneten Senkenlisten dieses Laufs, beim ERSTEN Zugriff gelesen und
        /// danach gehalten (Paket S1).
        ///
        /// <para><b>Warum verzögert.</b> Sie werden an drei Stellen gebraucht, deren
        /// früheste VOR dem bisherigen Ladepunkt liegt: Der Registry-Aufbau fragt schon
        /// nach den Senkenpuffern der Anlagen (<see cref="SenkenPufferDerAnlagen"/>), und
        /// ein Puffer, den erst eine Zeile mit Rang 3 lädt, käme sonst gar nicht erst in
        /// den Rechenpfad. Eine feste Ladezeile ganz am Anfang wäre die Alternative — sie
        /// verschöbe aber die Reihenfolge der Protokollmeldungen des Laufs.</para>
        ///
        /// <para>Zurückgesetzt wird das Feld zu Beginn jedes Laufs (dort, wo auch
        /// <see cref="senkenzuordnungen"/> neu gelesen wird); nie <c>null</c> als
        /// Rückgabe.</para>
        /// </summary>
        public List<Senkenliste> Senkenlisten()
        {
            if (_senkenlisten == null)
                _senkenlisten = WaermesenkeClass.SenkenlistenLaden(m_ID_Projekt);

            return _senkenlisten;
        }

        // ENTFALLEN MIT PAKET L (Aufraeumen, A1-O3): das Feld KaskadeZweikanalig.
        //
        // Bis Paket A1 war es der EFFEKTIVE Rechenweg des Laufs: true =
        // Speicherstufen-Mechanik mit herausgeloester Ladephase, false = der einkanalige
        // Altpfad. Der Altpfad ist mit A1 ersatzlos entfallen, das Feld stand seither
        // konstant auf true und hatte nur noch einen Leser - die Detailansicht. Mit
        // diesem Paket ist auch dort die Fallunterscheidung aufgeloest: Es gibt EINEN
        // Rechenweg ueber die Speicherstufe auf den drei Bedarfskanaelen.
        //
        // Die Projekteinstellung Tab_Einstellungen.Kaskade_Zweikanalig bleibt als
        // stillgelegte Spalte in der Datenbank stehen (Konzept Kapitel 15);
        // Migrationsschritt 51 hat sie im Bestand auf WAHR gesetzt.

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
        /// Rechenweg meldet deshalb hierüber statt über eine MessageBox, und
        /// <c>SimulationRunner</c> reicht den Text an den Aufrufer weiter
        /// (Paket-5-Nacharbeit, Befund N10; Konzept 13.4).
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

            // PAKET A1: Die beiden Vektorvariablen „Eingang"/„Ausgang" der einkanaligen
            // Modulschleife sind mit ihr entfallen.
            float[] temp = new float[8760 * 4];

            m_ID_Projekt = ID_Projekt;

            // PAKET S1: Die Senkenlisten des VORIGEN Laufs verwerfen. Gelesen werden sie
            // beim ersten Zugriff (siehe Senkenlisten()) - der liegt im Registry-Aufbau
            // und damit vor dem Ladepunkt der Senkenzuordnungen.
            _senkenlisten = null;

            // PAKET P1: dasselbe für die beiden projektbezogenen Lesecaches des
            // Schichtmodells - die Schichtparameter der Pufferzeilen und die
            // Einspeisehöhen der Senkenzeilen. Sie hängen am Projekt und dürfen einen
            // Laufwechsel nicht überleben.
            _schichtzeilen = null;
            _anschlusshoehen = null;

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

            // PAKET A1 (Leitentscheidung L1): Hier stand die WEICHE zwischen den beiden
            // Rechenwegen — das Feature-Flag Tab_Einstellungen.Kaskade_Zweikanalig,
            // oder-verknüpft mit „BHKW in der Kaskade" und „Parallelverbund im Projekt".
            // Sie ist ersatzlos entfallen: Jeder Lauf rechnet über die Speicherstufe mit
            // herausgelöster Ladephase (Reihenfolge-Invariante 6.3) auf den drei
            // Bedarfskanälen. Damit entfallen auch die drei Protokollhinweise, die den
            // gewählten Rechenweg begründet haben - es gibt keine Wahl mehr zu begründen.

            // ***********************************************************************
            // Speicher-Registry aufbauen (Paket 4 - Konzept 6.2) und den Senkenspeicher
            // der Wärmepumpe daraus an das WP-Modul geben. Die Registry wird danach für
            // die Speichermenge des Laufs geöffnet (RegistryFuerZweikanaligOeffnen).
            // ***********************************************************************
            SpeicherRegistryAufbauen();
            simulation_wp.Pufferspeicher = puffer_wp;

            // PAKET 8 (Konzept 13.4): Die Extrapolationsrückfrage der WP-Kennlinie ist
            // zur Projekteinstellung geworden. Ohne Konfigurationssatz gilt die
            // Vorbelegung "erlaubt", also das bisherige Verhalten (die Rückfrage wurde in
            // jedem dokumentierten Lauf bejaht).
            simulation_wp.Extrapolation_Erlaubt =
                (ctrl_konfig == null || ctrl_konfig.model == null) || ctrl_konfig.model.Extrapolation_erlaubt;

            // Senkenzuordnungen des Projekts (Konzept 6.1) - Haupt- und Zweitsenke.
            //
            // PAKET S1: Der dreikanalige Weg rechnet mit den GEORDNETEN SENKENLISTEN
            // (Senkenlisten(), Konzept 5.1). Diese Fassung bleibt für das BHKW-Modul, das
            // seinen Umbau auf n Senken je Stufe im eigenen Paket bekommt.
            senkenzuordnungen = WaermesenkeClass.SenkenLaden(m_ID_Projekt);

            // PAKET A1: Hier standen die drei Hinweise zum gewählten Rechenweg (BHKW
            // erzwingt / Parallelverbund erzwingt / Projekteinstellung gesetzt). Mit der
            // Weiche sind sie ersatzlos entfallen - es gibt nur noch EINEN Rechenweg,
            // und ein Hinweis, der keine Alternative mehr benennt, ist kein Hinweis.

            // PAKET S2 (Konzept 6.2, Entscheidung F6): Der Warnkriterienkatalog am
            // Laufstart.
            WarnkriterienMelden();

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

            // ***********************************************************************
            // PAKET A1 (Leitentscheidung L1): Der EINZIGE Rechenweg ist die
            // Speicherstufe mit herausgelöster Ladephase (Konzept 6.3) auf den drei
            // Bedarfskanälen. Der einkanalige Altpfad — die Modulschleife über tool[0..3]
            // auf EINEM Summenvektor mit ihren Aufrufern Simulation_WP_Ctrl,
            // Simulation_SPK_Ctrl und Simulation_Solarthermie_Ctrl — ist ersatzlos
            // entfallen, ebenso der Rechenweg-Hinweis AltpfadHinweiseD5a.
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


            Kaskade_Zweikanalig();

            // Photovoltaik abziehen
            if (tool[4] == DbWerte.ERZEUGER_PHOTOVOLTAIK)
            {
                var x = Rest_Strombedarf_viertelstuendlich.Sum() / 4000;
                temp = Simulation_Photovoltaik_Ctrl(Rest_Strombedarf_viertelstuendlich);
                Rest_Strombedarf_viertelstuendlich = SubVectors(Rest_Strombedarf_viertelstuendlich, temp);
                bSimulationPV = true;

                // V1 (PV-Konzept § 2.3, Etappe P1): BHKW-Überschuss läuft nicht mehr
                // als PV-Einspeisung, sondern getrennt — der Hinweis macht die
                // Korrektur im Laufprotokoll sichtbar (Abnahmekriterium P1).
                if (simulation_pv.BhkwUeberschuss_gesamt > 0.5f)
                {
                    string v1Text = null;
                    try { v1Text = MyResource.Resource.ResourceManager.GetString("SIM_PV_V1_BHKW_GETRENNT"); }
                    catch { }
                    if (string.IsNullOrEmpty(v1Text))
                        v1Text = "BHKW-Stromüberschuss von {0:N0} kWh getrennt von der PV-Einspeisung ausgewiesen.";
                    Protokoll.Hinweis(string.Format(v1Text, simulation_pv.BhkwUeberschuss_gesamt));
                }
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
            // Die Stelle liegt hinter der Speicherstufe: Sie baut den Lastvektor aus den
            // Reihen der Erzeuger (WP-Strombedarf, Heizstab, Kesselstrom,
            // BHKW-Erzeugung) und setzt die Modulflags, die der Controller beim
            // Beschaffen der Lastreihe auswertet.
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
            {
                sp.KennzahlenBerechnen();

                // PAKET P1 (Konzept § 11.3): SELBSTPRÜFUNG DER SCHICHT-INVARIANTE als
                // Protokollprobe am Laufende. Der Zähler steht bei einem gesunden Lauf
                // auf 0 - dann gibt es keine Zeile. Er zählt Stunden, in denen die Summe
                // der Schichtenergie von min(SOC, Q_max) über die Toleranz hinaus
                // abgewichen ist; nachgezogen wird sie in jedem Fall, gemeldet wird sie
                // hier, damit ein Auseinanderlaufen der beiden Zustandsebenen sichtbar
                // wird statt still ausgeglichen zu werden.
                if (sp.SchichtInvarianteVerletzungen > 0)
                    Protokoll.WarnungEinmal("schicht-invariante-" + sp.ID_Pufferspeicher,
                        string.Format(MyResource.Resource.SIMENG_SCHICHT_INVARIANTE,
                                      sp.ID_Pufferspeicher, sp.BezeichnerAnzeige(),
                                      sp.SchichtInvarianteVerletzungen,
                                      sp.SchichtInvarianteMaxAbweichung.ToString("0.######")));
            }

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
        // Speicherstufe mit herausgelöster Ladephase (Konzept 6.3) — seit Paket A1
        // der EINZIGE Rechenweg. Der Methodenname stammt aus Etappe 4b, als sie
        // hinter dem Feature-Flag Kaskade_Zweikanalig stand.
        // ===================================================================

        /// <summary>
        /// Der Erzeugerdurchlauf tool[0..3] auf den DREI Bedarfskanälen (Konzept 4.1)
        /// statt auf einem Summenvektor — seit Paket A1 der einzige Rechenweg.
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
            // Kanäle aus dem Bedarf holen (Konzept 4.2/4.1): Seit Paket K2 rechnet die
            // Kaskade auf DENSELBEN drei Kanälen, mit denen der Bedarf gebildet wurde -
            // die Übergangsabbildung Kanaele() (Heiz = HEIZUNG + PROZESS) ist damit
            // abgelöst und hat keinen Aufrufer mehr.
            //
            // Die frühere Kappungsmeldung entfällt mit den Kappungsfällen selbst: Es gibt
            // kein Residuum mehr, aus dem ein negativer Heizkanal entstehen könnte. An
            // ihre Stelle tritt die Energieprobe der Kanalbildung (Konzept 11.3), die
            // SimulationWaermebedarf selbst in das Lauf-Protokoll meldet.
            Kanalsatz kanaele = simulation_Waermebedarf.KanaeleDrei();

            // KNAPPHEITSREIHENFOLGE des Laufs (Konzept 4.3, F10) - EINMAL aufgelöst und
            // an beide Verbraucher gegeben: an die statischen Regeln, die die
            // Erzeugermodule und die Vektorstufen rufen (Kaskadenschleife.SenkeAbziehen,
            // DurchlassBuchen), und über den Kaskadenkontext an die Stundenschleife.
            // Die Auflösung steht VOR jeder Stufe: Auch eine Vektorstufe zieht über
            // dieselbe Regel ab.
            _knappheit = Kanal.KnappheitsReihenfolge(ctrl_konfig.model.Kanal_Knappheitsreihenfolge);
            Kaskadenschleife.KnappheitFuerLauf(_knappheit);

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
            // Absicht. Ob das BHKW in der Stundenschleife oder als Vektorstufe rechnet,
            // ist allein eine Frage seines Speichers. Ein BHKW ohne jeden Puffer hat
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
                    // Kanäle unverändert - die Stufe hat dann nichts gedeckt.
                    Kanalsatz vorher = kanaele.Clone();

                    Speicherstufe_Rechnen(kanaele,
                        Viertelstunden_zu_Stundenwerte_Mittelwert(Rest_Strombedarf_viertelstuendlich),
                        ctrl_konfig.model.m_WP_Heizstab,
                        ctrl_konfig.model.m_Kessel_Betriebsbereitschaft);

                    if (m_bError)
                        for (int k = 0; k < Kanal.ANZAHL; k++)
                            Array.Copy(vorher.Bedarf[k], kanaele.Bedarf[k], Kanalsatz.STUNDEN_JAHR);

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
                        // Kessel-Strombedarfs. In der Kaskadenreihenfolge steht der
                        // Kessel hinter der Wärmepumpe und sieht deshalb den Strombedarf
                        // nach deren Verbrauch. In der gemeinsamen Stundenschleife gibt
                        // es diese Reihenfolge nicht mehr — der Wert wird deshalb hier
                        // nachgezogen, sobald der WP-Strom feststeht. Steht der Kessel in
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
                        // Die Stromerzeugung des BHKW senkt den Strombedarf - an der
                        // Position der Stufe.
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

            // Ergebnis der Wärmeseite: die Summe der DREI Restkanäle (Paket K2). Ein
            // EIGENER Vektor - kein Alias auf das Ausgangsarray eines Moduls (B0-2).
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
        /// KNAPPHEITSREIHENFOLGE dieses Laufs (Konzept 4.3, F10) — aufgelöst zu Beginn
        /// von <see cref="Kaskade_Zweikanalig"/> aus der Projekteinstellung
        /// <c>Tab_Einstellungen.Kanal_Knappheitsreihenfolge</c> und von dort in den
        /// <see cref="Kaskadenkontext"/> gereicht. Vorbelegung {Brauchwasser, Prozess,
        /// Heizung}.
        /// </summary>
        private int[] _knappheit = Kanal.KnappheitVorgabe();

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
        /// PAKET S1: Maßgeblich sind die GEORDNETEN SENKENLISTEN — sie tragen alle Ränge,
        /// die beiden Altspalten nur die ersten zwei. Der Spaltenweg bleibt als zweite
        /// Prüfung darunter stehen: Er ist die Menge, mit der der Bestand diese Weiche
        /// heute stellt, und ein Projekt soll durch S1 nicht aus der Speicherstufe fallen.
        ///
        /// Dialogfrei über <see cref="StilleDb"/> (Konzept 13.4).
        /// </summary>
        private bool ErzeugerMitPufferSenke(int idType)
        {
            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, WS_Ziel, WS_ID_Puffer, WS_Ziel2, WS_ID_Puffer2 FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND ID_Type = ?",
                StilleDb.Par("@proj", OleDbType.Integer, m_ID_Projekt),
                StilleDb.Par("@typ", OleDbType.Integer, idType));

            if (dt == null) return false;

            // S1: erst über die Senkenlisten der Anlagen dieser Art.
            foreach (DataRow r in dt.Rows)
            {
                int idAnlage = StilleDb.Zahl(StilleDb.Feld(r, "ID"));
                if (idAnlage <= 0) continue;

                foreach (Senkenliste s in Senkenlisten())
                {
                    if (s == null || s.AnlagenID != idAnlage) continue;

                    foreach (Senkenzeile z in s.Zeilen)
                        if (z != null && z.IstPuffersenke && z.IDPuffer > 0) return true;
                }
            }

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
        private void Speicherstufe_Rechnen(Kanalsatz kanaele, float[] Strombedarf,
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

                QuellspeicherUebernehmen();
                schleife.WP = simulation_wp;
            }

            if (_solarInSchleife)
            {
                Solar_Liste_Laden();
                if (!simulation_solarthermie.Vorbereiten_Zweikanalig(m_ID_Projekt, Senkenlisten()))
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

                if (!simulation_spk.Vorbereiten_Zweikanalig(m_ID_Projekt, Senkenlisten()))
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

            // PAKET B1 (Konzept 8.2, L8): Temperaturkopplung der Wärmepumpen-Module.
            // MUSS hier stehen - nach QuellspeicherUebernehmen (erst dort wird die
            // eigene Quellinstanz durch die GETEILTE Registry-Instanz ersetzt, und genau
            // daran hängt die Unterscheidung geteilt/eigenständig) und vor
            // schleife.Rechnen. Der Kessel bekommt seine Kopplung eine Zeile darüber, in
            // KesselQuellbezugSetzen.
            BoosterKopplungVorbereiten();

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
        /// Eine Anlage OHNE Direktsenke deckt in Phase B nichts — sie lädt ausschließlich
        /// (Konzept 5.2). Entsteht aus ihrer erstrangigen Puffersenke aber kein
        /// Ladeauftrag, lädt sie auch nicht: Sie produziert das ganze Jahr nichts, und bis
        /// zur Nacharbeit ohne jeden Hinweis (gemessen an einem präparierten 1018:
        /// Kesselproduktion 34,27 -> 0 MWh). Ursachen sind Konfigurationsfehler, die die
        /// Oberfläche nicht verhindert: eine Puffer-ID, die auf den Puffer eines FREMDEN
        /// Projekts zeigt, oder ein Puffer, den die Registry aus anderen Gründen nicht in
        /// den Rechenpfad nimmt. (Der Fall „Puffer-Ziel ganz OHNE Puffer" wird schon eine
        /// Schicht früher abgefangen, in <c>WaermesenkeClass.SenkenlistenLaden</c>.)
        ///
        /// Die Rückfallebene ist der Heizkreis: Die Zeile wird zur Direktsenke, die Anlage
        /// deckt Bedarf wie eine Anlage ohne Puffer-Senke. Das ist die konservative
        /// Richtung — es entsteht keine Wärme, die niemand angefordert hat — und es wird
        /// protokolliert.
        ///
        /// <para><b>NUR RANG 1 wird zurückgestuft.</b> Eine höherrangige Puffersenke ohne
        /// Ladeauftrag ist wirkungslos (die Ladephase iteriert die AUFTRÄGE, nicht die
        /// Zeilen) und wird deshalb nur gemeldet. Sie zur Direktsenke zu machen wäre eine
        /// Verhaltensänderung: Die Anlage bekäme eine Bedarfsdeckung, die sie vorher nicht
        /// hatte.</para>
        ///
        /// Die Listenobjekte sind dieselben Instanzen, mit denen die Module rechnen
        /// (<see cref="Senkenlisten"/> ist die eine Quelle): Die Korrektur wirkt deshalb
        /// auch für Solarthermie und Heizkessel, deren Modulaufbau bereits gelaufen ist.
        /// </summary>
        private void PufferSenkenOhneAuftragZurueckfallen(Kaskadenkontext kontext)
        {
            if (kontext == null) return;

            if (_wpInSchleife)
                for (int i = 0; i < simulation_wp.wp_list.Count &&
                                i < kontext.SenkenlisteJeModul.Count; i++)
                    SenkeAufHeizkreisZurueck(kontext, simulation_wp.wp_list[i],
                                             kontext.SenkenlisteJeModul[i], "Wärmepumpe");

            if (_solarInSchleife)
                for (int f = 0; f < simulation_solarthermie.solar_anlagen_ids.Count; f++)
                    SenkeAufHeizkreisZurueck(kontext, simulation_solarthermie.solar_anlagen_ids[f],
                                             simulation_solarthermie.FeldSenke(f), "Solarthermie");

            if (_kesselInSchleife)
                for (int i = 0; i < simulation_spk.spk_anlagen_ids.Count; i++)
                    SenkeAufHeizkreisZurueck(kontext, simulation_spk.spk_anlagen_ids[i],
                                             simulation_spk.KesselSenke(i), "Heizkessel");

            // BHKW: Der Umbau seiner Stufe auf n Senken je Fahrweise ist ein eigenes
            // Paket (Konzept 5.2/F11). Bis dahin trägt es eine Senkenzuordnung statt
            // einer Senkenliste - dafür steht die Überladung darunter, und der Rückfall
            // wirkt unverändert.
            if (_bhkwInSchleife)
                for (int i = 0; i < simulation_bhkw.bhkw_anlagen_ids.Count; i++)
                    SenkeAufHeizkreisZurueck(kontext, simulation_bhkw.bhkw_anlagen_ids[i],
                                             simulation_bhkw.BhkwSenke(i), "BHKW");
        }

        /// <summary>
        /// ÜBERGANGSFASSUNG für Module, die noch eine <see cref="Senkenzuordnung"/> statt
        /// einer <see cref="Senkenliste"/> führen (BHKW bis zu seinem eigenen Paket).
        /// Wortgleich mit der Fassung vor Paket S1; sie entfällt mit dem BHKW-Umbau.
        /// </summary>
        private static void SenkeAufHeizkreisZurueck(Kaskadenkontext kontext, int idAnlage,
                                                     Senkenzuordnung z, string art)
        {
            if (z == null || z.Haupt == Senke.Heizkreis) return;
            if (AuftragVorhanden(kontext, idAnlage, 1)) return;

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
        /// Die ERSTRANGIGE Puffersenke einer Anlage auf den Heizkreis zurückstufen, wenn
        /// aus ihr kein Ladeauftrag entstanden ist (siehe
        /// <see cref="PufferSenkenOhneAuftragZurueckfallen"/>).
        /// </summary>
        private static void SenkeAufHeizkreisZurueck(Kaskadenkontext kontext, int idAnlage,
                                                     Senkenliste liste, string art)
        {
            if (liste == null) return;

            foreach (Senkenzeile z in liste.Zeilen)
            {
                if (z == null || !z.IstPuffersenke) continue;
                if (AuftragVorhanden(kontext, idAnlage, z.Rang)) continue;

                // Protokollkanal-Nachzug: WARNUNG statt bloßer Konsolenzeile - der
                // Rückfall ist eine Ersatzannahme mit Ergebniswirkung (gemessen an einem
                // präparierten 1018: Kesselproduktion 34,27 -> 0 MWh ohne ihn). Der
                // Schlüssel je Anlage UND Rang hält die Meldung eindeutig, auch wenn
                // mehrere Senken derselben Anlage fallen.
                SimulationProtokoll.Aktuell.WarnungEinmal(
                    "senke-ohne-ladeauftrag-" + idAnlage + "-" + z.Rang,
                    string.Format(MyResource.Resource.SIMENG_SENKE_OHNE_LADEAUFTRAG_RANG,
                                  idAnlage, art, z.Rang,
                                  Senkenzuordnung.ZielAusSenke(z.Ziel), z.IDPuffer,
                                  z.Rang == 1
                                      ? MyResource.Resource.SIMENG_SENKE_OHNE_LADEAUFTRAG_RANG1
                                      : MyResource.Resource.SIMENG_SENKE_OHNE_LADEAUFTRAG_NACHRANG));

                // Nur die erstrangige Zeile wird zur Direktsenke (Begründung am
                // Methodenkopf des Aufrufers).
                if (z.Rang != 1) continue;

                z.Ziel = Senke.Heizkreis;
                z.IDPuffer = 0;
                z.Ladeprio = 0;
                z.LadeprioPV = 0;
                z.LadegrenzeProzent = 0;
            }
        }

        /// <summary>true, wenn zu Anlage und Rang ein Ladeauftrag in diesem Lauf steht.</summary>
        private static bool AuftragVorhanden(Kaskadenkontext kontext, int idAnlage, int rang)
        {
            if (kontext == null || kontext.LadenOhnePV == null) return false;

            foreach (Ladeauftrag a in kontext.LadenOhnePV)
                if (a != null && a.AnlagenID == idAnlage && a.Rang == rang) return true;

            return false;
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
        /// keine Anlagen dieser Art". Die Abfragen sind Wort für Wort dieselben.
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
        private void Simulation_BHKW_Ctrl_Zweikanalig(Kanalsatz kanaele, float[] Strombedarf)
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

            // BETRIEBSTEMPERATUREN wie für jeden anderen Registry-Speicher: aus der
            // Projektkopie, sonst der ΔT-Rückfall (hier 20 K, siehe unten).
            //
            // PAKET A1: Die mittlere Stufe der Vorrangkette (Zuordnungszeile
            // Z_ProjektPufferSp, Nacharbeit Paket 6 / Befund N2) ist entfallen — die
            // Migration übernimmt diese Werte einmalig in die Projektkopie (Schritt 51).
            SimulationPufferspeicher sp;
            if (!speicherRegistry.TryGetValue(idPuffer, out sp) || sp == null)
            {
                sp = new SimulationPufferspeicher();
                sp.Bezeichner = p.Bezeichner;
                sp.Erzeuger = "BHKW";
                sp.ID_Pufferspeicher = p.ID;
                sp.ID_Projekt = p.ID_Projekt;
                sp.Verwendung = WaermesenkeClass.WirksameVerwendung(p);

                // PAKET K2: Klassen-Set aus der Projektkopie - wie im Registry-Aufbau.
                KlassenSetUebernehmen(sp);

                // RÜCKFALL 20 K statt 10 K (Befund N2): Die Altformel
                // „Liter · 20 / 860" hatte für den Pendelspeicher eine Spreizung von
                // 20 K fest verdrahtet. Bleibt sie ungepflegt, ist 20 K deshalb der
                // wertgleiche Ersatz (1,16 gegen 1,16279 Wh/(l·K), −0,24 %); der
                // generische 10-K-Notnagel würde die Kapazität ohne fachlichen Grund
                // halbieren. Für alle anderen Puffer bleibt es bei 10 K.
                sp.Init(p.Gesamtvolumen, p.Vorlauf, p.Ruecklauf, p.Bereitschaftsverluste, 20);
                RueckfallMelden(sp, p.ID, p.Bezeichner);

                // PAKET P1: dieselben Schichtparameter wie im Registry-Aufbau - der
                // Ersatz-Pendelspeicher ist eine Zeile derselben Projektkopie und darf
                // keine zweite Auslegung bekommen.
                SchichtparameterUebernehmen(sp);
                sp.SchichtenAufbauen();

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
            // NACHARBEIT I-K2-1, verallgemeinert in Paket K2: über das KLASSEN-SET, nicht
            // über IstBrauchwasserkanal. Ein Speicher mit mehrelementigem Klassen-Set
            // gehört in JEDE seiner Kanallisten; über IstBrauchwasserkanal (für einen
            // Kombispeicher false) landete er nur im Heizkanal, und die andere Hälfte fiel
            // still aus.
            for (int kanal = 0; kanal < Kanal.ANZAHL; kanal++)
                if (sp.BedientKanal(kanal))
                    EntladeordnungEinsortieren(k.Entladeordnung(kanal), sp, kanal);

            Ladeauftrag a = new Ladeauftrag();
            a.Modulindex = 0;
            a.Erzeugerart = ProjektPuffer.TYP_BHKW;
            a.AnlagenID = simulation_bhkw.FuehrendeAnlage;
            // PAKET S1: Rang 2 - der Ersatz-Pendelspeicher war und bleibt die
            // NACHRANGIGE Senke der BHKW-Stufe (bis K2 „Zweitsenke = true").
            a.Rang = 2;
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
                              p.Vorlauf + "/" + p.Ruecklauf + " °C, Q_max " +
                              sp.Q_max.ToString("0.###") + " kWh, Entladeprio " + sp.Entladeprio +
                              ", Obergrenze " + (a.Obergrenze * 100).ToString("0.#") + " % / mit PV " +
                              (a.ObergrenzePV * 100).ToString("0.#") + " %) rechnet als ZWEITSENKE " +
                              "mit. Der frühere skalare Pendelspeicher ist damit abgelöst.");
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
        /// <param name="kanal">
        /// Kanal, in den einsortiert wird (Nacharbeit I-K2-1, Paket K2 auf den Kanalindex
        /// umgestellt). Er bestimmt zugleich, gegen welche SOLL-Ordnung die Position
        /// gesucht wird; bei mehrelementigem Klassen-Set wird die Methode je Kanal einmal
        /// gerufen.
        /// </param>
        private void EntladeordnungEinsortieren(List<SimulationPufferspeicher> ordnung,
                                                SimulationPufferspeicher sp, int kanal)
        {
            if (ordnung == null || sp == null || ordnung.Contains(sp)) return;

            string kanalname = Kanal.Name(kanal);
            List<Ladeordnung.EntladeEintrag> soll = EntladeordnungQuelle(kanal);

            int platz = Ladeordnung.Position(soll, sp.ID_Pufferspeicher);
            if (platz <= 0)
            {
                Protokoll.HinweisEinmal("pendelspeicher-entladeordnung-" + kanalname + "-" +
                                  sp.ID_Pufferspeicher,
                                  string.Format(
                                      MyResource.Resource.SIMENG_PENDELSPEICHER_ENTLADEORDNUNG,
                                      sp.ID_Pufferspeicher, kanalname));
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
                Ladeordnung.Ladereihenfolge(m_ID_Projekt, sp.ID_Pufferspeicher, Senkenlisten());
            if (eintraege == null || eintraege.Count == 0) return;

            Ladeordnung.LadeEintrag eigen = null;
            foreach (Ladeordnung.LadeEintrag e in eintraege)
                if (e.ID_Anlage == a.AnlagenID && e.Rang == a.Rang) { eigen = e; break; }
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
        private void Simulation_SPK_Ctrl_Zweikanalig(Kanalsatz kanaele, float[] Strombedarf,
                                                     int nBereitschaft)
        {
            SPK_Liste_Laden();

            simulation_spk.Strombedarf_stuendlich = Strombedarf;
            simulation_spk.Vorgabe_Betriebsbereitschaft = nBereitschaft;

            if (!simulation_spk.Berechnung_Zweikanalig(m_ID_Projekt, kanaele, Senkenlisten()) &&
                !string.IsNullOrEmpty(simulation_spk.Fehlertext))
                Fehlertext = simulation_spk.Fehlertext;   // N10: dialogfrei melden
        }

        /// <summary>
        /// Solarthermie als zweikanalige VEKTORSTUFE (Paket 5): eigene Jahresschleife an
        /// der Kaskadenposition, ohne Speicherbeteiligung.
        /// </summary>
        private void Simulation_Solarthermie_Ctrl_Zweikanalig(Kanalsatz kanaele)
        {
            Solar_Liste_Laden();

            simulation_solarthermie.Berechnung_Zweikanalig(m_ID_Projekt, kanaele, Senkenlisten());
        }

        /// <summary>
        /// Öffnet den Rechenpfad für die Speicher, die im Lauf wirklich arbeiten können,
        /// und zieht die Felder nach, die nur an der Projektkopie stehen.
        ///
        /// KRITERIUM (nachgeschärft in der Paket-4-Review, Befund B2-b): Es rechnet, was
        /// eine Anlage als SENKE führt (die Senkenlisten und die gespiegelten Altspalten
        /// — dieselben Referenzen, aus denen auch <c>Ladeordnung.Ladereihenfolge</c> die
        /// Ladeaufträge bildet) oder was QUELLE einer Wärmepumpe ist
        /// (<c>WQ_ID_Puffer</c>).
        ///
        /// Die erste Fassung öffnete stattdessen ALLE Registry-Einträge — mit der
        /// Begründung aus Konzept 6.7 („ab Etappe 4b rechnen alle Registry-Speicher
        /// mit"). Das ging zu weit: Ein Speicher ohne Senkenreferenz hat keinen
        /// Ladeauftrag, erschiene aber mit lauter Nullen in
        /// <c>Tab_ErgebnisPufferspeicher</c>, und über <see cref="ErsterHeizpuffer"/>
        /// meldete <c>puffer_wp</c> eine Speicherkapazität
        /// (<c>Kapazitaet_Pufferspeicher</c>), die kein Erzeuger benutzt.
        /// <see cref="SimulationPufferspeicher.ImRechenpfad"/> bleibt damit eine echte
        /// Unterscheidung.
        ///
        /// Nachgezogen werden nur <c>Schwelle_Aus_Nachrang</c> und <c>Entladeprio</c>;
        /// Ein-/Abschaltschwelle bleiben ausdrücklich, wie die Registry sie aufgebaut hat.
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
            // Senkenreferenz und kommt schon über SenkenPufferDerAnlagen nicht in die
            // Registry. Der Fall entsteht nur bei von HAND gepflegten Beständen (SQL
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

                // Ausdrücklich ZUWEISEN, nicht nur setzen: Ohne Senkenreferenz gehört ein
                // Speicher nicht in den Rechenpfad — ihn lädt niemand.
                bool referenziert = senken.Contains(id);
                if (sp.ImRechenpfad && !referenziert)
                    Protokoll.WarnungEinmal("registry-ohne-senkenreferenz-" + id,
                                      "Speicher-Registry: Puffer " + id + " (" + sp.BezeichnerAnzeige() +
                                      ") wird von keiner Anlage dieses Projekts als Senke " +
                                      "geführt - er rechnet nicht mit.");
                sp.ImRechenpfad = referenziert;

                if (!referenziert) continue;

                WaermesenkeClass.PufferInfo p = WaermesenkeClass.PufferLesen(id);
                if (p == null) continue;

                if (p.SchwelleAusNachrang > 0) sp.SchwelleAusNachrang = p.SchwelleAusNachrang / 100.0;
                sp.Entladeprio = p.Entladeprio;
            }
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

            // Knappheitsreihenfolge des Laufs (4.3) - dieselbe Auflösung, die
            // Kaskade_Zweikanalig schon an die statischen Regeln gegeben hat.
            k.Knappheit = _knappheit;

            // --- 1. Speichermenge des Laufs (Phase G) --------------------------------
            k.AlleSpeicher.AddRange(RegistrySpeicher());

            // --- 2. Entladereihenfolge JE KANAL (Konzept 3.6, Paket K2) --------------
            for (int kanal = 0; kanal < Kanal.ANZAHL; kanal++)
                k.Entladen[kanal] = EntladeordnungAufbauen(k, kanal);

            // --- 3. Senkenliste je WP-Modul (Konzept 5.1, Paket S1) ------------------
            for (int index = 0; index < simulation_wp.wp_list.Count; index++)
            {
                int idAnlage = simulation_wp.wp_list[index];
                Senkenliste gefunden = null;
                foreach (Senkenliste s in Senkenlisten())
                    if (s != null && s.AnlagenID == idAnlage) { gefunden = s; break; }

                // Ohne Zeile gilt die Rang-1-Invariante: eine Direktsenke Heizkreis/Beides.
                if (gefunden == null) gefunden = Senkenliste.Vorbelegung(idAnlage);
                k.SenkenlisteJeModul.Add(gefunden);
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

            // PAKET A1: Hier stand die Sonderbehandlung von
            // _pufferOhneRegistrySchluessel — einer Speicherzeile ohne gültige ID, die
            // nur über die Alt-Zuordnung Z_ProjektPufferSp in den Lauf kam. Sie ist mit
            // Block 1 des Registry-Aufbaus entfallen.

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
        ///
        /// <para><b>ZUGEHÖRIGKEIT über das KLASSEN-SET</b>
        /// (<c>SimulationPufferspeicher.BedientKanal</c>). Sie ist dieselbe Frage, die das
        /// Durchsatzbudget stellt; beide MÜSSEN dieselbe Antwort bekommen, sonst
        /// verspräche die hydraulische Weiche einen Bedarf, den die Entladung nicht
        /// bedienen darf (oder umgekehrt). Bis Paket K2 lief sie über
        /// <c>Kaskadenschleife.EntladetKanal</c> und damit über die Interimsregel I2
        /// („Heizungspuffer bedient auch Prozess"); die ist mit S1 abgerissen — den
        /// Prozesskanal bedient nur noch ein Puffer mit <c>Nutzung_Prozess</c>.</para>
        ///
        /// <para><b>ORDNUNGSQUELLE des Prozesskanals.</b> <see cref="Ladeordnung"/> kennt
        /// nur die beiden persistierten Kanäle — eine Spalte „Verwendung = Prozess" gibt
        /// es nicht. Für den PROZESSkanal wird die Ordnung deshalb aus BEIDEN
        /// Kanalabfragen zusammengesetzt und nach derselben Regel sortiert
        /// (Entladepriorität, bei Gleichstand die Puffer-ID). Führt das Projekt keinen
        /// Speicher mit eigenem Prozess-Flag — jedes Bestandsprojekt —, ist das Ergebnis
        /// Element für Element die Heizungsordnung: genau die Liste, aus der die
        /// zweikanalige Rechnung den zusammengefassten Heiz-/Prozessbedarf gedeckt hat.
        /// </para>
        /// </summary>
        private List<SimulationPufferspeicher> EntladeordnungAufbauen(Kaskadenkontext k, int kanal)
        {
            List<SimulationPufferspeicher> liste = new List<SimulationPufferspeicher>();
            string kanalname = Kanal.Name(kanal);

            foreach (Ladeordnung.EntladeEintrag e in EntladeordnungQuelle(kanal))
            {
                SimulationPufferspeicher sp;
                if (!speicherRegistry.TryGetValue(e.ID_Puffer, out sp) || sp == null) continue;
                if (!sp.ImRechenpfad || sp.IstQuelle) continue;
                // Ein Speicher steht in der Entladeordnung JEDES Kanals seines
                // Klassen-Sets, je Kanal an der Stelle seiner Entladepriorität.
                if (!sp.BedientKanal(kanal)) continue;
                if (!liste.Contains(sp)) liste.Add(sp);
            }

            // Sicherheitsnetz: ein Registry-Speicher dieses Kanals, den die
            // Entladereihenfolge nicht kennt (Projektzuordnung inkonsistent), fiele sonst
            // stillschweigend aus der Bilanz. Er kommt ans Ende - Reihenfolge der Aufnahme.
            foreach (SimulationPufferspeicher sp in k.AlleSpeicher)
            {
                if (sp == null || sp.IstQuelle) continue;
                if (!sp.BedientKanal(kanal)) continue;
                if (liste.Contains(sp)) continue;

                Protokoll.HinweisEinmal("entladeordnung-nachtrag-" + kanalname + "-" +
                                  sp.ID_Pufferspeicher,
                                  string.Format(
                                      MyResource.Resource.SIMENG_ENTLADEORDNUNG_NACHTRAG,
                                      sp.ID_Pufferspeicher, sp.BezeichnerAnzeige(), kanalname));
                liste.Add(sp);
            }

            return liste;
        }

        /// <summary>
        /// SOLL-Ordnung eines Kanals aus <see cref="Ladeordnung"/> (siehe
        /// <see cref="EntladeordnungAufbauen"/>): für die beiden persistierten Kanäle die
        /// eine Abfrage, für den PROZESSkanal die sortierte Vereinigung beider.
        /// </summary>
        private List<Ladeordnung.EntladeEintrag> EntladeordnungQuelle(int kanal)
        {
            if (kanal == Kanal.BRAUCHWASSER)
                return Ladeordnung.Entladereihenfolge(m_ID_Projekt,
                                                      WaermesenkeClass.VERWENDUNG_BRAUCHWASSER,
                                                      Senkenlisten());

            List<Ladeordnung.EntladeEintrag> liste =
                Ladeordnung.Entladereihenfolge(m_ID_Projekt, WaermesenkeClass.VERWENDUNG_HEIZUNG,
                                               Senkenlisten());

            if (kanal != Kanal.PROZESS) return liste;

            // Prozesskanal: Speicher, deren PERSISTIERTES Set nur Brauchwasser und
            // Prozess führt, stehen nicht in der Heizungsordnung. Sie kommen dazu und die
            // Liste wird nach derselben Regel sortiert, die Ladeordnung selbst benutzt -
            // ohne solche Speicher (jedes Bestandsprojekt) ist die Sortierung ein No-op
            // auf einer bereits sortierten Liste.
            bool ergaenzt = false;
            foreach (Ladeordnung.EntladeEintrag e in
                     Ladeordnung.Entladereihenfolge(m_ID_Projekt,
                                                    WaermesenkeClass.VERWENDUNG_BRAUCHWASSER,
                                                    Senkenlisten()))
            {
                if (Ladeordnung.Position(liste, e.ID_Puffer) > 0) continue;
                liste.Add(e);
                ergaenzt = true;
            }

            if (ergaenzt)
                liste.Sort(delegate (Ladeordnung.EntladeEintrag a, Ladeordnung.EntladeEintrag b)
                {
                    int c = a.Prio.CompareTo(b.Prio);
                    if (c != 0) return c;
                    return a.ID_Puffer.CompareTo(b.ID_Puffer);
                });

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

                // PAKET S1: aus den GEORDNETEN SENKENLISTEN. Ein Ladeauftrag entsteht je
                // PUFFER-SENKENZEILE, nicht mehr je Spaltenpaar - damit sind mehr als
                // zwei Senken je Anlage abbildbar. Sortierung und Obergrenzen-Auflösung
                // sind dieselben wie zuvor (Ladeordnung 3.4).
                List<Ladeordnung.LadeEintrag> proPuffer =
                    Ladeordnung.Ladereihenfolge(m_ID_Projekt, sp.ID_Pufferspeicher, Senkenlisten());
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
                    // PAKET S1: der RANG der Senkenzeile steuert die Ladephase; das
                    // frühere Zweitsenken-Kennzeichen leitet sich daraus ab.
                    a.Rang = e.Rang;
                    a.Speicher = sp;
                    // PAKET P1 (Konzept § 7.4 Punkt 1): die EINSPEISEHÖHE dieser
                    // Senkenzeile. −1 = nicht gepflegt und damit oben - der Zustand
                    // JEDES heutigen Datensatzes (Schritt 50 hat die Spalte als
                    // P1-Vorgriff angelegt und bewusst leer gelassen).
                    a.Einspeisehoehe = Anschlusshoehe(e.ID_Anlage, e.Rang);
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
        /// EINSPEISEHÖHEN aller Senkenzeilen des Projekts, Schlüssel
        /// „<c>Anlage:Rang</c>" — EINMAL gelesen (Paket P1).
        /// </summary>
        private Dictionary<string, double> _anschlusshoehen;

        /// <summary>
        /// Einspeisehöhe einer Senkenzeile (<c>Z_AnlageSenke.Anschlusshoehe</c>, Konzept
        /// § 7.4 Punkt 1); <b>−1 = nicht gepflegt</b> und damit oben.
        ///
        /// <para>Gelesen wird hier und nicht in <c>WaermesenkeClass.SenkenlistenLaden</c>:
        /// Die Höhe ist eine reine ENGINE-Größe des Schichtmodells — kein Dialog, keine
        /// Projektkopie und keine der übrigen Auswertungen der Senkenliste fragt sie ab.
        /// <c>SELECT *</c> mit <c>StilleDb.Feld</c>, damit eine Datenbank ohne die Spalte
        /// (Schritt 50 nicht gelaufen) nicht die ganze Abfrage verliert.</para>
        /// </summary>
        private double Anschlusshoehe(int idAnlage, int rang)
        {
            if (_anschlusshoehen == null)
            {
                _anschlusshoehen = new Dictionary<string, double>();

                DataTable dt = StilleDb.Tabelle(
                    "SELECT s.ID_Anlage, s.Rang, s.Anschlusshoehe FROM " +
                    SchemaKatalog.Z_ANLAGESENKE + " s INNER JOIN " +
                    SchemaKatalog.TAB_ENERGIEANLAGEN + " a ON s.ID_Anlage = a.ID " +
                    "WHERE a.ID_Projekt = ?",
                    StilleDb.Par("@proj", OleDbType.Integer, m_ID_Projekt));

                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                    {
                        object o = StilleDb.Feld(r, SchemaKatalog.SPALTE_SENKE_ANSCHLUSSHOEHE);
                        if (o == null) continue;

                        double h = StilleDb.Kommazahl(o, -1);
                        if (h < 0 || h > 1) continue;

                        string schluessel = StilleDb.Zahl(StilleDb.Feld(r, SchemaKatalog.SPALTE_SENKE_ID_ANLAGE)) +
                                            ":" + StilleDb.Zahl(StilleDb.Feld(r, SchemaKatalog.SPALTE_SENKE_RANG));
                        _anschlusshoehen[schluessel] = h;
                    }
            }

            double hoehe;
            return _anschlusshoehen.TryGetValue(idAnlage + ":" + rang, out hoehe) ? hoehe : -1;
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
        /// Alle am Lauf beteiligten Speicher in stabiler Reihenfolge — die EINE Quelle
        /// der Wahrheit für Ergebnis-Persistenz (Tab_ErgebnisPufferspeicher),
        /// Navigator-Serien, CSV-Export und die Ergebnistabelle der Detailansicht
        /// (Konzept 6.6/13.3).
        ///
        /// ETAPPE 4b — ZUSAMMENFÜHRUNG MIT DER REGISTRY (offener Punkt 6 aus 4a):
        /// Die Registry IST diese eine Quelle. Sie liefert dieselben Objekte, die auch
        /// gerechnet haben — Ergebnis-Persistenz, Navigator und CSV-Export speisen sich
        /// damit aus derselben Menge, die die Stundenschleife bewegt hat. Das Kriterium
        /// ist „hat gerechnet", ausgedrückt über
        /// <see cref="SimulationPufferspeicher.ImRechenpfad"/>: Ohne gelaufene
        /// Wärmepumpen-Stufe wird die Registry gar nicht erst geöffnet, und die Liste
        /// bleibt leer.
        ///
        /// PAKET A1: Die zweite Fassung dieser Liste (Senkenspeicher der Wärmepumpe plus
        /// Quellspeicher der Module) gehörte zum einkanaligen Altpfad und ist mit ihm
        /// entfallen.
        /// </summary>
        public System.Collections.Generic.List<SimulationPufferspeicher> AlleSpeicher()
        {
            return RegistrySpeicher();
        }

        /// <summary>
        /// PAKET E1 (Konzept 6.3, Befund S-1): Nutzbare Kapazität ALLER Senkenspeicher
        /// des Laufs [kWh] — die Ablösung des Alias <see cref="puffer_wp"/> in der
        /// Ergebnisgröße <c>Tab_ErgebnisWaermepumpe.Kapazitaet_Pufferspeicher</c>.
        ///
        /// <para><b>Warum die Summe und nicht der erste Puffer.</b> <c>puffer_wp</c> ist
        /// der ERSTE Heizungs-Puffer in Aufnahmereihenfolge. In einem Projekt mit zwei
        /// Puffern je Kanal wies die Kennzahl damit die Kapazität EINES Behälters aus und
        /// verschwieg den zweiten; in einem Projekt, dessen einziger Speicher ein
        /// Brauchwasser- oder Kombispeicher ist, meldete sie 0, obwohl der Lauf einen
        /// Speicher bewirtschaftet hat. Beides ist keine Rundungsfrage, sondern ein
        /// falscher Wert — die Umstellung ist eine DOKUMENTIERTE Ergebnisänderung.</para>
        ///
        /// <para><b>Ohne die Quellspeicher.</b> Ein Quellpuffer ist Wärmequelle der
        /// Anlage, kein Vorrat für den Bedarf; seine Kapazität gehört nicht in eine
        /// Kennzahl, die die Pufferung der Wärmeversorgung beschreibt. Er steht mit
        /// eigener Zeile in <c>Tab_ErgebnisPufferspeicher</c> (Rolle „Quelle").</para>
        ///
        /// <para>Ein Parallelverbund zählt genau einmal: Sein Leitspeicher trägt bereits
        /// die aufsummierte Kapazität aller Mitglieder, und nur er steht in der
        /// Registry.</para>
        /// </summary>
        public double SenkenspeicherKapazitaet()
        {
            double summe = 0;
            foreach (SimulationPufferspeicher sp in AlleSpeicher())
            {
                if (sp == null) continue;
                if (string.Equals(sp.Verwendung, SimulationPufferspeicher.VERWENDUNG_QUELLE,
                                  StringComparison.Ordinal)) continue;
                if (sp.Q_max > 0) summe += sp.Q_max;
            }
            return summe;
        }

        // ===================================================================
        // PAKET E2 (Nachtrag zu Konzept 4.4) — KANALGANGLINIEN: Zugriffsweg und Probe
        // ===================================================================

        /// <summary>
        /// DECKUNG eines Erzeugers je Bedarfskanal und Stunde [kWh] (Paket E2) — die
        /// Stundenfassung des EIGENANTEILS, mit dem <c>SimulationRunner.Summiere</c> die
        /// Jahresdeckung je Kanal bildet: Direktdeckung plus die zugerechnete
        /// Speicherentladung.
        ///
        /// <para><b>Der Heizstab steht bewusst NICHT darin.</b> Er hat in der
        /// Ergebnisanzeige wie in der Kanalbuchführung seine eigene Zeile
        /// (<see cref="HeizstabKanalStuendlich"/>); nur der Runner rechnet ihn für die
        /// Deckungszahl der Wärmepumpe hinzu.</para>
        ///
        /// <para>Ein Gewerk, das im Lauf nicht gerechnet hat, liefert einen Nullvektor —
        /// nie <c>null</c> (dieselbe Zusage wie bei allen Ganglinien der Engine).</para>
        /// </summary>
        /// <param name="art">Erzeugerart, <c>ProjektPuffer.TYP_*</c>.</param>
        /// <param name="kanal">Bedarfskanal, <see cref="Kanal"/>.</param>
        public float[] DeckungKanalStuendlich(int art, int kanal)
        {
            if (art == ProjektPuffer.TYP_WP && simulation_wp != null)
                return Kanalganglinie.Deckung(kanal,
                    simulation_wp.Direktdeckung_KanalStuendlich,
                    simulation_wp.Speicherentladung_KanalStuendlich);

            if (art == ProjektPuffer.TYP_KESSEL && simulation_spk != null)
                return Kanalganglinie.Deckung(kanal,
                    simulation_spk.Direktdeckung_KanalStuendlich,
                    simulation_spk.Speicherentladung_KanalStuendlich);

            if (art == ProjektPuffer.TYP_SOLARTHERMIE && simulation_solarthermie != null)
                return Kanalganglinie.Deckung(kanal,
                    simulation_solarthermie.Direktdeckung_KanalStuendlich,
                    simulation_solarthermie.Speicherentladung_KanalStuendlich);

            if (art == ProjektPuffer.TYP_BHKW && simulation_bhkw != null)
                return Kanalganglinie.Deckung(kanal,
                    simulation_bhkw.Direktdeckung_KanalStuendlich,
                    simulation_bhkw.Speicherentladung_KanalStuendlich);

            return Kanalganglinie.Deckung(kanal);   // Nullvektor
        }

        /// <summary>
        /// HEIZSTABWÄRME je Bedarfskanal und Stunde [kWh] (Paket E2) — eigene Serie, wie
        /// in <c>NavigatorUebersicht</c> und im Ergebnis-Diagramm.
        /// </summary>
        public float[] HeizstabKanalStuendlich(int kanal)
        {
            return (simulation_wp != null)
                ? Kanalganglinie.Deckung(kanal, simulation_wp.Heizstab_KanalStuendlich)
                : Kanalganglinie.Deckung(kanal);
        }

        /// <summary>
        /// BEDARF je Kanal und Stunde [kWh] (Paket E2) — die Kanalvektoren des Laufs,
        /// netzverlust-inklusive; dieselben, aus deren Jahressummen
        /// <c>SimulationRunner.BedarfJeKanal</c> die drei Kennzahlen der Bedarfsseite
        /// bildet (Konsistenz Zahl ↔ Kurve).
        ///
        /// <para>Geliefert wird die KOPIE aus <c>KanaeleDrei()</c> — die Module
        /// überschreiben ihre Eingangsvektoren in-place (Regel B0-2).</para>
        /// </summary>
        public static float[] BedarfKanalStuendlich(SimulationWaermebedarf bedarf, int kanal)
        {
            if (bedarf == null || kanal < 0 || kanal >= Kanal.ANZAHL)
                return new float[Kanalsatz.STUNDEN_JAHR];
            return bedarf.KanaeleDrei().Bedarf[kanal];
        }

#if DEBUG

        /// <summary>
        /// PROBE der Kanalganglinien (Paket E2) — ausschließlich im Debug-Build, nach dem
        /// Muster von <c>Kanalsatz.Selbsttest</c> (kein Prüfcode im Release-Assembly).
        ///
        /// <para>Zugesichert wird für jede der neun Größen (vier Erzeuger × Direktdeckung
        /// und Speicherentladung, dazu der Heizstab) und für jeden Speicher: die
        /// JAHRESSUMME der Ganglinie je Kanal ist die Bestands-Jahressumme desselben
        /// Kanals. Beide entstehen aus derselben Buchung; der Rest ist allein die
        /// Assoziativität der double-Addition (8760 Akkumulatoren gegen einen). Maßstab
        /// ist <see cref="Kanalsatz.ErhaltungOk"/>.</para>
        ///
        /// <para>Zusätzlich die Summenzusage über die Kanäle: <c>Σ_k Σ_h Ganglinie</c>
        /// gegen den jeweiligen Bestandsskalar (<c>Direktdeckung_gesamt</c>,
        /// <c>Speicherentladung_Anteil</c>, <c>Heizstab_gesamt</c>,
        /// <c>Entladung_gesamt</c>).</para>
        /// </summary>
        public string KanalganglinienProbe()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int geprueft = 0, fehler = 0;
            double groessterRest = 0;

            Action<string, Kanalganglinie, double[], double> pruefen =
                delegate (string name, Kanalganglinie g, double[] jahr, double skalar)
                {
                    if (g == null || jahr == null) return;

                    double summeAllerKanaele = 0;
                    for (int k = 0; k < Kanal.ANZAHL; k++)
                    {
                        double ist = g.Jahressumme(k);
                        summeAllerKanaele += ist;
                        geprueft++;

                        double rest = Math.Abs(ist - jahr[k]);
                        if (rest > groessterRest) groessterRest = rest;
                        if (!Kanalsatz.ErhaltungOk(jahr[k], ist, Kanalsatz.ERHALTUNG_SCHRITTE_SUMME))
                        {
                            fehler++;
                            sb.AppendLine(string.Format(
                                "FEHLER {0} Kanal {1}: Ganglinie {2:G9} != Jahressumme {3:G9}",
                                name, k, ist, jahr[k]));
                        }
                    }

                    if (skalar > 0)
                    {
                        geprueft++;
                        double rest = Math.Abs(summeAllerKanaele - skalar);
                        if (rest > groessterRest) groessterRest = rest;
                        if (!Kanalsatz.ErhaltungOk(skalar, summeAllerKanaele,
                                                   Kanalsatz.ERHALTUNG_SCHRITTE_SUMME))
                        {
                            fehler++;
                            sb.AppendLine(string.Format(
                                "FEHLER {0}: Sigma Kanaele {1:G9} != Skalar {2:G9}",
                                name, summeAllerKanaele, skalar));
                        }
                    }
                };

            if (simulation_wp != null)
            {
                pruefen("WP.Direktdeckung", simulation_wp.Direktdeckung_KanalStuendlich,
                        simulation_wp.Direktdeckung_Kanal, simulation_wp.Direktdeckung_gesamt);
                pruefen("WP.Speicherentladung", simulation_wp.Speicherentladung_KanalStuendlich,
                        simulation_wp.Speicherentladung_Kanal, simulation_wp.Speicherentladung_Anteil);
                pruefen("WP.Heizstab", simulation_wp.Heizstab_KanalStuendlich,
                        simulation_wp.Heizstab_Kanal, simulation_wp.Heizstab_gesamt);
            }
            if (simulation_spk != null)
            {
                pruefen("Kessel.Direktdeckung", simulation_spk.Direktdeckung_KanalStuendlich,
                        simulation_spk.Direktdeckung_Kanal, 0);
                pruefen("Kessel.Speicherentladung", simulation_spk.Speicherentladung_KanalStuendlich,
                        simulation_spk.Speicherentladung_Kanal, simulation_spk.Speicherentladung_Anteil);
            }
            if (simulation_solarthermie != null)
            {
                pruefen("Solar.Direktdeckung", simulation_solarthermie.Direktdeckung_KanalStuendlich,
                        simulation_solarthermie.Direktdeckung_Kanal,
                        simulation_solarthermie.Direktdeckung_gesamt);
                pruefen("Solar.Speicherentladung", simulation_solarthermie.Speicherentladung_KanalStuendlich,
                        simulation_solarthermie.Speicherentladung_Kanal,
                        simulation_solarthermie.Speicherentladung_Anteil);
            }
            if (simulation_bhkw != null)
            {
                pruefen("BHKW.Direktdeckung", simulation_bhkw.Direktdeckung_KanalStuendlich,
                        simulation_bhkw.Direktdeckung_Kanal, simulation_bhkw.Direktdeckung_gesamt);
                pruefen("BHKW.Speicherentladung", simulation_bhkw.Speicherentladung_KanalStuendlich,
                        simulation_bhkw.Speicherentladung_Kanal, simulation_bhkw.Speicherentladung_Anteil);
            }

            foreach (SimulationPufferspeicher sp in AlleSpeicher())
            {
                if (sp == null) continue;
                pruefen("Puffer " + sp.ID_Pufferspeicher + ".Entladung",
                        sp.Entladung_KanalStuendlich, sp.Entladung_Kanal, sp.Entladung_gesamt);
            }

            sb.AppendLine(string.Format(
                "Kanalganglinien-Probe (Paket E2): {0} Zusagen geprueft, {1} FEHLER, groesster Rest {2:G4} kWh.",
                geprueft, fehler, groessterRest));
            return sb.ToString();
        }

#endif

        // ===================================================================
        // Speicher-Registry (Konzept 6.2) - Aufbau und Zugriff
        // ===================================================================

        /// <summary>
        /// Übernimmt das KLASSEN-SET eines Speichers aus der Projektkopie in das
        /// Rechenobjekt (Konzept 6.1, Schritt 49 — Paket K2) und zieht die Anzeige-/
        /// Ergebnisrolle <c>Verwendung</c> konsistent nach.
        ///
        /// <para><b>Warum die Rolle nachgezogen wird.</b> <c>Verwendung</c> ist seit
        /// Schritt 49 Lese-Altlast — die Wahrheit über die Kanäle steht im Set. Die Rolle
        /// wird trotzdem gebraucht (Ergebniszeile, Anzeige, Vollzyklen-Bezug); sie darf
        /// dann aber nicht etwas anderes behaupten als das Set. Abgeleitet wird sie über
        /// dieselbe eine Regel, mit der auch jeder Schreibweg der Puffertabelle arbeitet
        /// (<c>PufferSpCtrl.VerwendungAusKlassenSet</c>). Auf einem migrierten Bestand
        /// stimmen beide ohnehin überein, und die Zuweisung ist wirkungslos.</para>
        ///
        /// <para><b>PAKET A1:</b> Die frühere Ausnahme für Block 1 des Registry-Aufbaus
        /// (Set aus der gesetzten <c>Verwendung</c> ableiten statt aus den Flags, K2-O7)
        /// ist mit Block 1 selbst entfallen. Es gibt nur noch EINE Herkunft des Sets: die
        /// drei Flags der Projektkopie. Ebenso entfallen ist die Schranke „nur im
        /// zweikanaligen Weg" — es gibt nur noch diesen einen.</para>
        /// </summary>
        private void KlassenSetUebernehmen(SimulationPufferspeicher sp)
        {
            if (sp == null || sp.IstQuelle) return;

            PufferSpCtrl.KlassenSet set = (sp.ID_Pufferspeicher > 0)
                ? PufferSpCtrl.KlassenSetLesen(sp.ID_Pufferspeicher)
                : PufferSpCtrl.KlassenSetAusVerwendung(sp.Verwendung);

            sp.KlassenSetSetzen(set.Heizung, set.Brauchwasser, set.Prozess);

            string rolle = PufferSpCtrl.VerwendungAusKlassenSet(set.Heizung, set.Brauchwasser,
                                                                set.Prozess);
            if (string.Equals(rolle, sp.Verwendung, StringComparison.Ordinal)) return;

            Protokoll.HinweisEinmal("klassenset-rolle-" + sp.ID_Pufferspeicher,
                              string.Format(MyResource.Resource.SIMENG_KLASSENSET_ROLLE,
                                            sp.ID_Pufferspeicher, sp.BezeichnerAnzeige(),
                                            sp.KlassenSetText(), sp.Verwendung, rolle));
            sp.Verwendung = rolle;
        }

        /// <summary>
        /// Erster Heizungs-Puffer der Registry in Aufnahmereihenfolge, der im Lauf
        /// tatsächlich rechnet — die Auflösung des Alias <see cref="puffer_wp"/>
        /// (Konzept 6.7).
        ///
        /// Die Einschränkung auf <see cref="SimulationPufferspeicher.ImRechenpfad"/> ist
        /// zwingend und dort ausführlich begründet: Die Registry enthält auch Puffer, die
        /// in diesem Lauf niemand rechnet (Kriterium siehe
        /// <see cref="RegistryFuerZweikanaligOeffnen"/>: Senken- oder Quellreferenz).
        ///
        /// OFFEN: die Reihenfolge auf die Entladepriorität umzustellen (Konzept 3.6) —
        /// sie ist hier die Aufnahmereihenfolge, und bei mehreren Heizungspuffern zeigt
        /// <c>puffer_wp</c> deshalb auf den zuerst aufgenommenen, nicht auf den zuerst
        /// entladenen.
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

            // PAKET A1: Hier stand die Rückfallebene _pufferOhneRegistrySchluessel — eine
            // Speicherzeile ohne gültige ID, die nur über die Alt-Zuordnung
            // Z_ProjektPufferSp in den Lauf kam. Mit Block 1 des Registry-Aufbaus ist sie
            // entfallen; ein Puffer ohne ID kann keine Senkenreferenz tragen.
            return null;
        }

        /// <summary>
        /// Nimmt einen Speicher unter seiner <c>Tab_Pufferspeicher.ID</c> in die Registry
        /// auf. Ein bereits vorhandener Schlüssel wird NICHT überschrieben — „je Speicher
        /// genau ein Objekt" heißt auch: das zuerst aufgebaute Objekt gewinnt.
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
        /// Aufgenommen wird, was eine PROJEKTANLAGE ALS SENKE FÜHRT — die geordneten
        /// Senkenlisten (<c>Z_AnlageSenke</c>, Paket S1) samt der noch gespiegelten
        /// Altspalten <c>WS_ID_Puffer</c>/<c>WS_ID_Puffer2</c>, in Kaskadenreihenfolge
        /// der Anlagen und danach nach Rang; jeweils mit den Betriebsparametern der
        /// Projektkopie <c>Tab_Pufferspeicher</c>.
        ///
        /// <para><b>PAKET A1 (K2-O7):</b> Hier stand als BLOCK 1 der Senkenspeicher der
        /// WÄRMEPUMPE aus der Alt-Zuordnung <c>Z_ProjektPufferSp</c> — mit eigener
        /// Parameterherkunft, eigener Rollenfestlegung und einer Temperatur-Vorrangkette
        /// „Projektkopie → Zuordnungszeile". Er ist ersatzlos entfallen (Leitentscheidung
        /// L1, Schritt 51). Die Betriebstemperaturen der Zuordnungszeilen übernimmt die
        /// Migration einmalig als DML in die Projektkopie; ein Puffer ohne vollständiges
        /// Paar fällt wie jeder andere auf die ΔT-Regel in
        /// <see cref="SimulationPufferspeicher.Init"/> zurück (Meldung siehe
        /// <see cref="RueckfallMelden"/>).</para>
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

            // --- 0. Die Verbünde des Projekts, EINMAL ---------------------------------
            //
            // PAKET PARALLELVERBUND: Muss VOR dem Aufbau stehen — die Aggregation greift
            // an jedem aufgenommenen Leitspeicher. Auf einer Datenbank ohne Verbund ist
            // das Verzeichnis leer und alles Folgende ist Zeile für Zeile das bisherige
            // Verhalten.
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

            // --- 1. Alle von einer Projektanlage als SENKE geführten Puffer ----------
            //
            // PAKET A1: Hier stand davor als Block 1 der Senkenspeicher der Wärmepumpe
            // aus der Alt-Zuordnung Z_ProjektPufferSp (K2-O7). Er ist ersatzlos
            // entfallen; die Senkenmenge kommt vollständig aus den Senkenlisten der
            // Anlagen (Begründung im Methodenkopf).
            foreach (int id in SenkenPufferDerAnlagen())
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

                // PAKET K2: Klassen-Set aus den drei Flags der Projektkopie (Schritt 49).
                // Auf migriertem Bestand ist es die Ableitung aus genau der Verwendung,
                // die eine Zeile höher zugewiesen wurde - dann ändert der Aufruf nichts.
                KlassenSetUebernehmen(sp);

                // BETRIEBSTEMPERATUREN: ausschließlich aus der Projektkopie
                // Tab_Pufferspeicher. PAKET A1: Die mittlere Stufe der früheren
                // Vorrangkette (Zuordnungszeile Z_ProjektPufferSp) ist entfallen - die
                // Migration hat diese Werte einmalig in die Projektkopie übernommen
                // (Schritt 51). Ein Puffer ohne vollständiges Paar fällt wie bisher auf
                // die ΔT-Regel in SimulationPufferspeicher.Init zurück; RueckfallMelden
                // schreibt das in das Lauf-Protokoll.
                sp.Init(p.Gesamtvolumen, p.Vorlauf, p.Ruecklauf, p.Bereitschaftsverluste);
                RueckfallMelden(sp, p.ID, p.Bezeichner);

                // PAKET P1 (Konzept § 7.2): die Parameter der Schichtebene aus der
                // Projektkopie (Migrationsschritt 53). MUSS nach Init stehen — dort
                // entstehen Q_max, VL_eff/RL_eff und die Vorbelegung T_Nutz = RL_eff —
                // und VOR VerbundAufaddieren: Der Verbund-Guard (§ 6.3) prüft die
                // Schichtzahl und zwingt sie am Leitspeicher auf 1.
                SchichtparameterUebernehmen(sp);

                // PAKET PARALLELVERBUND — unmittelbar nach der Rückfallmeldung des
                // LEITSPEICHERS (Begründung dort).
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

                // PAKET P1: Schichtebene aus den fertigen Parametern aufbauen — erst
                // JETZT steht Q_max endgültig fest (der Verbund hat es womöglich
                // aufsummiert), und daran hängen Schichtenergie, Wärmekapazität und
                // Leitwert. Der Laufanfang (Reset) baut sie noch einmal auf.
                sp.SchichtenAufbauen();

                // Beim Aufbau nicht im Rechenpfad; geöffnet wird er, wenn eine Anlage ihn
                // als Senke führt (siehe ImRechenpfad).
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
        /// eigenes Temperaturpaar mit, und für jedes gilt der ΔT-Rückfall dieses
        /// Speichers — genau wie für einen Einzelpuffer
        /// (<c>WaermesenkeClass.PufferInfo.Q_max</c>). Zwei mal 1000 l bei 60/40 und
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

            // PAKET P1 (Konzept § 6.3, Entscheidung F8): VERBUND UND SCHICHTUNG SCHLIESSEN
            // SICH JE RECHENSPEICHER AUS — der Leitspeicher rechnet STETS mit N = 1.
            //
            // Gleich darunter wird sein Q_max zur AUFSUMMIERTEN Kapazität aller
            // Verbundmitglieder. Eine Schichtebene entsteht aber aus dem Volumen und der
            // Geometrie DIESES EINEN Behälters; auf einer fremden, größeren Kapazität
            // wären Schichtenergie, Wärmekapazität und Leitwert schlicht falsch. Die
            // harte Abweisung im Dialog (Warnkriterium W6) verhindert den Fall beim
            // Speichern; hier steht der Laufzeit-Riegel für von Hand gepflegte Bestände.
            if (leit.SchichtenAnzahl > 1)
            {
                Protokoll.WarnungEinmal("verbund-schichtung-" + leit.ID_Pufferspeicher,
                    string.Format(MyResource.Resource.SIMENG_VERBUND_SCHICHTUNG,
                                  leit.ID_Pufferspeicher, leit.BezeichnerAnzeige(),
                                  leit.SchichtenAnzahl));
                leit.SchichtenAnzahl = 1;
            }

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
        /// Die Meldung geht in kein Ergebnis und in keine CSV ein.
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

        // =====================================================================
        // PAKET P1 — Parameter des Schichtspeichermodells (Konzept § 7.2)
        // =====================================================================

        /// <summary>
        /// Die Zeilen von <c>Tab_Pufferspeicher</c> des laufenden Projekts, EINMAL
        /// gelesen — Schlüssel ist die Puffer-ID.
        ///
        /// <para>Der Registry-Aufbau holt die Stammfelder über
        /// <c>WaermesenkeClass.PufferLesen</c> (eine Abfrage je Puffer, gewachsene
        /// Struktur). Die neun Schichtfelder aus Schritt 53 kommen NICHT dort dazu:
        /// Diese Klasse ist der einzige Leser, der sie braucht, und mit einer
        /// Sammelabfrage je Projekt statt einer zweiten je Puffer bleibt es bei einem
        /// Zugriff.</para>
        /// </summary>
        private Dictionary<int, DataRow> _schichtzeilen;

        /// <summary>
        /// Liest die Schichtparameter des Projekts EINMAL und liefert die Zeile eines
        /// Puffers; <c>null</c>, wenn es sie nicht gibt oder die Tabelle nicht lesbar
        /// ist.
        ///
        /// <para><c>SELECT *</c> statt einer Spaltenliste — bewusst: Auf einer noch
        /// nicht migrierten Datenbank fehlen die neun Spalten, und eine ausformulierte
        /// Liste ließe die ganze Abfrage scheitern. So liefert
        /// <c>StilleDb.Feld(zeile, spalte)</c> für eine fehlende Spalte schlicht
        /// <c>null</c>, und jeder Parameter fällt auf seine Konzept-Vorgabe zurück
        /// (= das Verhalten vor Paket P1).</para>
        /// </summary>
        private DataRow Schichtzeile(int idPuffer)
        {
            if (_schichtzeilen == null)
            {
                _schichtzeilen = new Dictionary<int, DataRow>();

                DataTable dt = StilleDb.Tabelle(
                    "SELECT * FROM " + SchemaKatalog.TAB_PUFFERSPEICHER + " WHERE ID_Projekt = ?",
                    StilleDb.Par("@proj", OleDbType.Integer, m_ID_Projekt));

                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                    {
                        int id = StilleDb.Zahl(StilleDb.Feld(r, "ID"));
                        if (id > 0 && !_schichtzeilen.ContainsKey(id)) _schichtzeilen[id] = r;
                    }
            }

            DataRow zeile;
            return _schichtzeilen.TryGetValue(idPuffer, out zeile) ? zeile : null;
        }

        /// <summary>
        /// Überträgt die Parameter des SCHICHTSPEICHERMODELLS aus der Projektkopie auf
        /// das Rechenobjekt (Konzept § 7.2, Migrationsschritt 53).
        ///
        /// <para>Jeder Wert hat eine Vorgabe, die das Verhalten VOR Paket P1 ergibt:
        /// N = 1 (Ein-Zonen-Modell), Höhe aus dem Volumen, λ = 1,5 W/(m·K),
        /// T_Nutz = <c>RL_eff</c>, Entnahme oben (am Kombispeicher die Heizung in der
        /// Mitte, § 7.5) und beide Leistungsgrenzen unbegrenzt. Eine Datenbank ohne die
        /// Spalten rechnet damit exakt wie zuvor.</para>
        /// </summary>
        private void SchichtparameterUebernehmen(SimulationPufferspeicher sp)
        {
            if (sp == null) return;

            DataRow r = Schichtzeile(sp.ID_Pufferspeicher);
            if (r == null) return;

            // SCHICHTZAHL. Werte außerhalb 1..10 werden geklemmt — ein Wert von Hand in
            // der Datenbank darf keinen unmöglichen Zustand erzeugen.
            int n = StilleDb.Zahl(StilleDb.Feld(r, SchemaKatalog.SPALTE_PSP_SCHICHTEN_ANZAHL), 1);
            if (n < 1) n = 1;
            if (n > SimulationPufferspeicher.SCHICHTEN_MAX) n = SimulationPufferspeicher.SCHICHTEN_MAX;
            sp.SchichtenAnzahl = n;

            // GEOMETRIE und WÄRMELEITUNG. 0 bzw. NULL heißt „nicht gepflegt": Die Höhe
            // kommt dann aus dem Volumen über H/D = 2,5, λ_eff aus der Konzept-Vorgabe.
            sp.Hoehe = StilleDb.Kommazahl(StilleDb.Feld(r, SchemaKatalog.SPALTE_PSP_HOEHE), 0);
            if (sp.Hoehe < 0) sp.Hoehe = 0;

            sp.LambdaEff = StilleDb.Kommazahl(StilleDb.Feld(r, SchemaKatalog.SPALTE_PSP_LAMBDA_EFF),
                                              SimulationPufferspeicher.LAMBDA_EFF_DEFAULT);
            if (sp.LambdaEff <= 0) sp.LambdaEff = SimulationPufferspeicher.LAMBDA_EFF_DEFAULT;

            // MINDEST-NUTZTEMPERATUR des Brauchwasserkanals (F7: heute nur dieser Kanal).
            // Init hat alle drei Kanäle mit RL_eff vorbelegt; hier wird allein der
            // Brauchwasserkanal übersteuert, und nur bei gepflegtem Wert.
            object tNutz = StilleDb.Feld(r, SchemaKatalog.SPALTE_PSP_T_NUTZ_BW);
            if (tNutz != null)
            {
                double t = StilleDb.Kommazahl(tNutz, sp.RL_eff);

                // KLEMMUNG auf VL_eff mit Protokollwarnung (§ 7.2): Eine
                // Nutztemperatur über der Vorlauftemperatur könnte keine Schicht je
                // erreichen — der Kanal wäre still und dauerhaft abgeschaltet. Sichtbar
                // falsch ist besser als still falsch.
                if (t > sp.VL_eff)
                {
                    // {3} steht ZWEIMAL im Text (wirksamer Vorlauf und Ersatzwert) - beide
                    // Male derselbe Wert und dieselbe Formatierung wie bisher.
                    Protokoll.WarnungEinmal("tnutz-ueber-vorlauf-" + sp.ID_Pufferspeicher,
                        string.Format(MyResource.Resource.SIMENG_TNUTZ_UEBER_VORLAUF,
                                      sp.ID_Pufferspeicher, sp.BezeichnerAnzeige(),
                                      t.ToString("0.#"), sp.VL_eff.ToString("0.#")));
                    t = sp.VL_eff;
                }

                if (t < sp.RL_eff) t = sp.RL_eff;
                sp.TNutz[Kanal.BRAUCHWASSER] = t;
            }

            // ENTNAHMEHÖHEN. Die Vorgabe hängt am Klassen-Set (§ 7.5): Führt der
            // Speicher AUCH Brauchwasser, entnehmen Heizung und Prozesswärme in der
            // Mitte und lassen die Bereitschaftszone oben unangetastet; sonst entnehmen
            // alle Kanäle oben (§ 7.2, „Entnahme oben").
            double vorgabeUnten = sp.BedientKanal(Kanal.BRAUCHWASSER) ? 0.5 : 1.0;
            sp.Entnahmehoehe[Kanal.HEIZUNG] = Entnahmehoehe(r, SchemaKatalog.SPALTE_PSP_ENTNAHME_HEIZUNG,
                                                            vorgabeUnten);
            sp.Entnahmehoehe[Kanal.BRAUCHWASSER] = Entnahmehoehe(r, SchemaKatalog.SPALTE_PSP_ENTNAHME_BW,
                                                                 1.0);
            sp.Entnahmehoehe[Kanal.PROZESS] = Entnahmehoehe(r, SchemaKatalog.SPALTE_PSP_ENTNAHME_PROZESS,
                                                            vorgabeUnten);

            // LEISTUNGSGRENZEN [kW] — 0 = unbegrenzt (Befund K2-O6, § 6.3).
            sp.LadeleistungMax =
                StilleDb.Kommazahl(StilleDb.Feld(r, SchemaKatalog.SPALTE_PSP_LADELEISTUNG_MAX), 0);
            if (sp.LadeleistungMax < 0) sp.LadeleistungMax = 0;

            sp.EntladeleistungMax =
                StilleDb.Kommazahl(StilleDb.Feld(r, SchemaKatalog.SPALTE_PSP_ENTLADELEISTUNG_MAX), 0);
            if (sp.EntladeleistungMax < 0) sp.EntladeleistungMax = 0;
        }

        /// <summary>
        /// Eine Entnahmehöhe aus der Projektkopie, auf 0…1 geklemmt; NULL oder ein Wert
        /// außerhalb liefert <paramref name="vorgabe"/>.
        /// </summary>
        private static double Entnahmehoehe(DataRow r, string spalte, double vorgabe)
        {
            object o = StilleDb.Feld(r, spalte);
            if (o == null) return vorgabe;

            double h = StilleDb.Kommazahl(o, vorgabe);
            return (h >= 0 && h <= 1) ? h : vorgabe;
        }

        // PAKET A1: Hier stand „ZuordnungsTemperaturen" — die mittlere Stufe der
        // Temperatur-Vorrangkette (Vorlauf/Rücklauf aus der Alt-Zuordnung
        // Z_ProjektPufferSp). Sie ist mit der Stilllegung der Zuordnung entfallen; die
        // Werte übernimmt die Migration einmalig in die Projektkopie (Schritt 51).

        /// <summary>
        /// Übernimmt die Quellspeicher der WP-Module in die Registry (Konzept 6.2,
        /// Zusatz der Fassung 12: „Die Registry muss diese Instanzen übernehmen oder
        /// ablösen — sonst entstehen zwei parallele Speicherverwaltungen mit getrennter
        /// Bilanz").
        ///
        /// Übernommen werden die INSTANZEN selbst, keine Kopien: Was das Modul rechnet,
        /// ist danach dasselbe Objekt, das in der Registry steht.
        ///
        /// MEHRERE MODULE AM SELBEN QUELLPUFFER:
        /// <c>SimulationWaermepumpe.QuellspeicherZusammenfuehren</c> hat die Instanzen
        /// bereits vereinigt, bevor diese Methode läuft; die Schleife sieht dann je
        /// Puffer-ID nur noch ein Objekt.
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
        ///
        /// <para><b>PAKET A1:</b> Der Schalter <c>zweikanalig</c> ist entfallen. Er hielt
        /// die KASKADEN-Auflösung aus Etappe D5a (siehe unten) vom Altpfad fern, der
        /// seine getrennten Quellinstanzen behielt; es gibt nur noch den einen
        /// Rechenweg.</para>
        /// </summary>
        private void QuellspeicherUebernehmen()
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
                if (belegt != null && !belegt.IstQuelle &&
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
        /// PAKET B1 (Konzept 8.2, Leitentscheidung L8) — richtet die
        /// BOOSTER-TEMPERATURKOPPLUNG der Wärmepumpen-Module ein und protokolliert sie.
        ///
        /// <para>Gekoppelt wird ein Modul genau dann, wenn sein Quellpuffer ein
        /// GETEILTER Puffer ist: eine Speicherinstanz, die zugleich Senke eines anderen
        /// Erzeugers ist und deshalb von <see cref="QuellspeicherUebernehmen"/> aus der
        /// Registry eingesetzt wurde (<c>IstQuelle == false</c>). Eigenständige
        /// Quellspeicher — der Erdsonden-Ersatz mit <c>WQ_Spreizung</c>, Start voll —
        /// behalten die statische Jahres-Quelltemperatur (Konzept 8.2, letzter
        /// Absatz).</para>
        ///
        /// <para>Der Hinweis nennt Speicher und Temperaturband, weil die Kopplung eine
        /// ERGEBNISÄNDERUNG gegenüber jedem früheren Lauf desselben Projekts ist: Vorher
        /// rechnete dieselbe Anlage mit einer Konstante, jetzt mit einer Ganglinie.</para>
        /// </summary>
        private void BoosterKopplungVorbereiten()
        {
            if (simulation_wp == null || !_wpInSchleife) return;

            if (simulation_wp.BoosterKopplungVorbereiten() <= 0) return;

            IReadOnlyList<SimulationPufferspeicher> quellen = simulation_wp.Quellspeicher;
            for (int i = 0; i < quellen.Count; i++)
            {
                if (!simulation_wp.QuelleGekoppelt(i)) continue;

                SimulationPufferspeicher q = quellen[i];
                if (q == null) continue;

                // Die Anlagen-ID kommt aus der MODULLISTE, nicht aus dem Speicher: Die
                // geteilte Instanz ist ein SENKENspeicher der Registry und trägt deshalb
                // kein ID_Anlage (das führt nur eine eigene Quellinstanz).
                int idAnlage = (i < simulation_wp.wp_list.Count) ? simulation_wp.wp_list[i] : 0;

                Protokoll.Hinweis(string.Format(MyResource.Resource.SIMENG_BOOSTER_KOPPLUNG,
                                  idAnlage, q.ID_Pufferspeicher, q.BezeichnerAnzeige(),
                                  q.RL_eff.ToString("0.#"), q.VL_eff.ToString("0.#"),
                                  q.SchichtenWirksam,
                                  AnschlusshoeheText(simulation_wp.QuellAnschlusshoehe(i), q)));
            }
        }

        /// <summary>
        /// PAKET Q1: Zusatz zur Booster-Protokollzeile, der die QUELL-ENTNAHMEHÖHE nennt
        /// (<c>WQ_Anschlusshoehe</c>, Schema-Schritt 54).
        ///
        /// <para>Er erscheint NUR, wenn die Höhe etwas ändert — also bei einem
        /// geschichteten Speicher (N &gt; 1) und einer gepflegten Höhe unterhalb von
        /// „oben". Bei N = 1 hat ein Vorrat nur eine Zone, und bei „oben" steht die
        /// Vorgabe; in beiden Fällen wäre die Angabe eine Zeile Lärm um nichts, und die
        /// Meldungsmenge des Bestands bliebe nicht unverändert (Referenzlauf-Kriterium).</para>
        /// </summary>
        private static string AnschlusshoeheText(double hoehe, SimulationPufferspeicher q)
        {
            if (q == null || q.SchichtenWirksam <= 1) return "";
            if (hoehe >= SimulationPufferspeicher.HOEHE_OBEN) return "";

            return ", Entnahme auf Höhe " + hoehe.ToString("0.##") +
                   " (0 = unten, 1 = oben)";
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

            // PAKET B1: Ein TEMPERATURGEKOPPELTER Kessel zählt als wirksam, auch wenn sein
            // Anteil gerade 0 ist — er entsteht je Stunde neu, und beim Laufaufbau steht
            // der Puffer noch leer (Konzept 8.4). Ohne diese Zeile meldete ausgerechnet
            // der Booster „kein Quellbezug zustande gekommen".
            for (int i = 0; i < simulation_spk.KesselAnzahl; i++)
                if (simulation_spk.QuellAnteil(i) > 0 ||
                    simulation_spk.QuelleGekoppelt(i)) return;      // mindestens einer wirkt

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
        /// PAKET S2 — der WARNKRITERIENKATALOG (Konzept 6.2, Entscheidung F6) am
        /// Laufstart: <see cref="Warnkriterien.PruefeProjekt"/> und jeder Befund als
        /// Zeile im Protokollkanal.
        ///
        /// <para><b>NUR PROTOKOLL, kein Abbruch — auch bei harten Befunden.</b> Der
        /// Kurzschluss („Quelle = eigenes Ladeziel") und der Ring in der Kaskadenkette
        /// haben ihre Guards TIEFER: <see cref="QuellbezuegeAufbauen"/> laesst den
        /// Quellbezug bei Kurzschluss gar nicht erst entstehen (E-K2-1), und die
        /// Ebenen-Relaxation der <c>Kaskadenschleife</c> bricht bei einem Ring den Lauf
        /// mit eigenem Fehlertext ab. Ein zweiter Abbruch von hier aus wuerde dieselbe
        /// Sache zweimal melden und dem Anwender die genauere der beiden Meldungen
        /// nehmen. Der Katalog meldet deshalb VORAB und in derselben Sprache wie der
        /// Dialog — mehr nicht.</para>
        ///
        /// <para>Die Stelle ist bewusst NACH dem Registry-Aufbau und VOR dem Rechenweg
        /// gewaehlt: Der Katalog arbeitet auf der KONFIGURATION (Anlagen, Senkenlisten,
        /// Speicherzeilen) und braucht die Registry nicht — aber die Meldungen sollen im
        /// Protokoll vor allem stehen, was die Module melden.</para>
        ///
        /// <para><see cref="SimulationProtokoll.WarnungEinmal"/> mit dem Kriterium und
        /// den beteiligten IDs als Schluessel: Ein Befund, der aus zwei Richtungen
        /// entsteht (etwa ein Kurzschluss, den zwei Senkenzeilen derselben Anlage
        /// erzeugen), steht dann trotzdem nur einmal im Protokoll.</para>
        /// </summary>
        private void WarnkriterienMelden()
        {
            List<Warnbefund> befunde = Warnkriterien.PruefeProjekt(m_ID_Projekt);
            if (befunde == null || befunde.Count == 0) return;

            foreach (Warnbefund b in befunde)
            {
                if (b == null || string.IsNullOrEmpty(b.Text)) continue;

                Protokoll.WarnungEinmal(
                    "warnkriterium-" + b.Kriterium + "-" + b.ID_Anlage + "-" + b.ID_Puffer,
                    Zeilenumbruch.Einzeilig(b.Text));
            }
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
        /// <para><b>PAKET B1 (Konzept 8.4) — zwei Fälle statt einem.</b> Ist der
        /// Quellpuffer ein GETEILTER Puffer (zugleich Senke eines anderen Erzeugers,
        /// erkennbar an <c>!IstQuelle</c>), wird der Anteil nicht mehr einmalig gebildet,
        /// sondern folgt je Stunde dem Speicherzustand — dieselbe Regel und derselbe
        /// Lesezeitpunkt wie bei der Wärmepumpe (8.2). Diese Methode richtet dann nur den
        /// HUB ein (<c>SimulationSPK.QuellkopplungSetzen</c>); die Stundenabfrage macht
        /// die Kaskadenschleife. Ein EIGENSTÄNDIGER Quellspeicher behält den bisherigen
        /// statischen Weg über die Speicherzeile — sein Temperaturpaar sind keine
        /// Speichertemperaturen (7.6).</para>
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

            // PAKET B1 (Konzept 8.4): T_Quelle ist beim GETEILTEN Puffer nicht mehr die
            // Vorlauftemperatur der Speicherzeile, sondern der Speicherzustand. Für die
            // Einrichtung (Guard „zu kalt", Protokolltext) gilt dann das ERREICHBARE
            // MAXIMUM VL_eff — die höchste Temperatur, die eine Schicht je tragen kann.
            // Beim eigenständigen Quellspeicher bleibt es Zeichen für Zeichen beim
            // Bestandsweg über die Speicherzeile.
            bool gekoppelt = !quelle.IstQuelle && quelle.Q_max > 0 && quelle.VL_eff > quelle.RL_eff;

            WaermesenkeClass.PufferInfo qp = gekoppelt
                ? null : WaermesenkeClass.PufferLesen(quelle.ID_Pufferspeicher);
            double tQuelle = gekoppelt ? quelle.VL_eff : ((qp != null) ? qp.Vorlauf : 0);

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

            if (gekoppelt)
            {
                // PAKET B1: Der Anteil wird je Stunde neu gebildet (Konzept 8.4);
                // eingerichtet wird hier nur der HUB, gegen den er entsteht.
                // PAKET Q1: dazu die Quell-Entnahmehöhe der Anlage (Schritt 54,
                // WQ_Anschlusshoehe; NULL = oben und damit B1-Verhalten).
                double hoehe = SimulationWaermepumpe.AnschlusshoeheLesen(idAnlage);
                simulation_spk.QuellkopplungSetzen(index, quelle, vorlauf, ruecklauf, hoehe);

                Protokoll.Hinweis(string.Format(
                                  MyResource.Resource.SIMENG_KESSEL_BOOSTER_KOPPLUNG,
                                  idAnlage, quelle.ID_Pufferspeicher, quelle.BezeichnerAnzeige(),
                                  quelle.RL_eff.ToString("0.#"), quelle.VL_eff.ToString("0.#"),
                                  AnschlusshoeheText(hoehe, quelle),
                                  ruecklauf, vorlauf, (anteil * 100).ToString("0.#")));
                return;
            }

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

            // 2. Der SENKENpuffer des Kessels — seit Paket S1 über seine SENKENLISTE, in
            //    RANGFOLGE. Mit zwei Senken (jedes migrierte Bestandsprojekt) ist das
            //    Zeile für Zeile die bisherige Reihenfolge „Hauptsenke, dann Zweitsenke".
            Senkenliste senken = simulation_spk.KesselSenke(index);
            if (senken != null)
            {
                foreach (Senkenzeile z in senken.Zeilen)
                {
                    if (z == null || !z.IstPuffersenke || z.IDPuffer <= 0) continue;

                    WaermesenkeClass.PufferInfo p = WaermesenkeClass.PufferLesen(z.IDPuffer);
                    if (p == null || !ProjektPuffer.IstTemperaturpaar(p.Vorlauf, p.Ruecklauf)) continue;

                    vorlauf = p.Vorlauf;
                    ruecklauf = p.Ruecklauf;
                    return true;
                }
            }

            return false;
        }

        // PAKET A1: Hier stand „AltpfadHinweiseD5a" — die beiden Hinweise, was
        // Kombispeicher und Kessel-Quellbezug im einkanaligen Altpfad bedeuten
        // (Kombispeicher wie Heizungspuffer, Kessel-Quellbezug wirkungslos). Beide
        // Einschränkungen gibt es nicht mehr: Der Lauf rechnet immer dreikanalig, der
        // Kombispeicher bedient sein Klassen-Set, und der Kessel-Quellbezug wirkt.

        /// <summary>
        /// true, wenn <paramref name="idPuffer"/> die eigene Senke der Anlage ist — der
        /// KURZSCHLUSS aus Konzept 4.6 (Quelle = Senke derselben Anlage), den der Dialog
        /// blockiert, Altdaten aber tragen können. Er bleibt vom Kaskadenweg der
        /// Etappe D5a ausgenommen.
        ///
        /// <para><b>PAKET A1 — Befund S2-B1 geschlossen.</b> Bis dahin las die Prüfung
        /// ausschließlich die beiden Altspalten <c>WS_ID_Puffer</c>/<c>WS_ID_Puffer2</c>
        /// und übersah damit jeden Kurzschluss, der allein in der Senkentabelle steht:
        /// alles ab Rang 3 und alles, was programmatisch ohne Altspalten-Spiegelung
        /// geschrieben wurde (S2, Abschnitt 3; nachgewiesen in der Wirkprobe, Runde 2 —
        /// der Warnkriterienkatalog meldete den Kurzschluss, dieser Engine-Guard
        /// schwieg). Gefragt wird jetzt die GEORDNETE SENKENLISTE der Anlage, also
        /// dieselbe Quelle, aus der auch die Ladeaufträge entstehen. Sie ist im Lauf
        /// ohnehin geladen (<see cref="Senkenlisten"/>) — die Prüfung kostet keine
        /// zusätzliche Abfrage mehr, und sie fällt (über
        /// <c>WaermesenkeClass.SenkenlistenLaden</c>) auf einer noch nicht migrierten
        /// Datenbank auf genau die beiden Altspalten zurück.</para>
        /// </summary>
        private bool IstEigenerSenkenPuffer(int idAnlage, int idPuffer)
        {
            if (idAnlage <= 0 || idPuffer <= 0) return false;

            foreach (Senkenliste s in Senkenlisten())
            {
                if (s == null || s.AnlagenID != idAnlage) continue;

                foreach (Senkenzeile z in s.Zeilen)
                    if (z != null && z.IstPuffersenke && z.IDPuffer == idPuffer) return true;
            }

            return false;
        }

        /// <summary>
        /// IDs der Puffer, die eine Projektanlage als SENKE führt — in
        /// Kaskadenreihenfolge der Anlagen und danach nach Rang. Doppelte Nennungen sind
        /// unschädlich; der Aufrufer überspringt bekannte IDs.
        ///
        /// Dialogfrei über <see cref="StilleDb"/> (Konzept 13.4): Eine fehlende Spalte
        /// auf einem alten Schema liefert hier <c>null</c> statt einer MessageBox mitten
        /// im Rechenlauf.
        ///
        /// <para><b>PAKET A1:</b> Die frühere zweite Menge <c>ReferenzierteSenkenPuffer</c>
        /// (diese hier PLUS die Puffer der Alt-Zuordnungen <c>Z_ProjektPufferSp</c>) ist
        /// entfallen. Registry und Rechenpfad beantworten jetzt dieselbe Frage: „lädt ihn
        /// eine Anlage dieses Projekts".</para>
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

            // PAKET S1: dazu die Puffer der GEORDNETEN SENKENLISTEN - ein Speicher, den
            // erst eine Zeile mit Rang 3 lädt, steht in keiner der beiden Altspalten und
            // käme sonst weder in die Registry noch in den Rechenpfad.
            //
            // AUSDRÜCKLICH ERGÄNZEND, nicht ersetzend: Die Altspalten bleiben die Quelle
            // der bisherigen Menge, samt der Altdaten-Reste (eine WS_ID_Puffer, deren
            // WS_Ziel längst wieder auf den Heizkreis zeigt). Diese Reste sind heute in
            // der Registry und damit in Tab_ErgebnisPufferspeicher; sie hier
            // herauszufiltern wäre eine Ergebnisänderung ohne Auftrag. Auf migriertem
            // Bestand fügt die Schleife nichts hinzu, was nicht schon dastünde.
            //
            // PAKET A1: Der Altspalten-Zweig bleibt deshalb auch nach dem Altpfad-Abriss
            // stehen.
            //
            // PAKET L (A1-O4) — GEPRÜFT UND BEWUSST STEHEN GELASSEN, mit Begründung:
            //
            //   1. Er ist NICHT wirkungslos. Die WS_-Spiegelung ist mit A1 gefallen, die
            //      SPALTEN sind es nicht (Konzept Kapitel 15: „stillgelegt, Lese-Altlast
            //      nach Migration"). Was vor der Migration dort stand, steht dort weiter -
            //      einschliesslich der Altdaten-Reste, die keine Senkenzeile mehr hat.
            //      Auf der Referenzmenge sind das die zwei Puffer aus Befund V0-O6
            //      („PufferHeizung ohne WS_ID_Puffer", Gegenrichtung derselben Datenlage).
            //
            //   2. Sein Wegfall wäre ERGEBNISÄNDERND. Ein Puffer, der nur noch über die
            //      Altspalte in die Registry kommt, verschwände aus dem Rechenpfad und aus
            //      Tab_ErgebnisPufferspeicher - das ist keine Aufräumarbeit, sondern eine
            //      stille Verhaltensänderung an Bestandsprojekten.
            //
            //   3. Er ist HARMLOS, wo er nichts findet: Auf migriertem Bestand fügt die
            //      Schleife nichts hinzu, was die Senkenliste nicht ohnehin nennt.
            //
            // Er fällt erst mit den Spalten selbst - also mit einem Schema-Schritt, der
            // WS_Ziel/WS_ID_Puffer entfernt. Den gibt es bewusst nicht.
            foreach (Senkenliste s in Senkenlisten())
            {
                if (s == null) continue;

                foreach (Senkenzeile z in s.Zeilen)
                    if (z != null && z.IstPuffersenke && z.IDPuffer > 0 && !ids.Contains(z.IDPuffer))
                        ids.Add(z.IDPuffer);
            }

            return ids;
        }

        // PAKET A1: Hier stand „Simulation_WP_Ctrl" — der Aufrufer der einkanaligen
        // Wärmepumpen-Stundenschleife (SimulationWaermepumpe.Berechnung). Er ist mit dem
        // Altpfad entfallen; die Wärmepumpe rechnet ausschließlich in der Speicherstufe
        // (Speicherstufe_Rechnen → Kaskadenschleife).

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

        // PAKET A1: Hier standen „Simulation_SPK_Ctrl" und
        // „Simulation_Solarthermie_Ctrl" — die Aufrufer der einkanaligen Jahresschleifen
        // von Heizkessel (SimulationSPK.Berechnung) und Solarthermie
        // (SimulationSolarthermie.Berechnung). Beide sind mit dem Altpfad entfallen; die
        // Module rechnen als Mitglied der Speicherstufe oder als zweikanalige
        // Vektorstufe (Simulation_SPK_Ctrl_Zweikanalig,
        // Simulation_Solarthermie_Ctrl_Zweikanalig).

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

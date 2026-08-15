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
        public SimulationSSP simulation_ssp = new SimulationSSP();
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
        /// Feature-Flag der zweikanaligen Kaskade (Konzept Kapitel 9), gelesen aus
        /// <c>Tab_Einstellungen.Kaskade_Zweikanalig</c> des Projekts.
        ///
        /// GESETZT: <see cref="Kaskade_Zweikanalig"/> rechnet die Kaskade auf den beiden
        /// Bedarfskanälen mit herausgelöster Ladephase (Reihenfolge-Invariante 6.3).
        /// NICHT GESETZT: der unveränderte einkanalige Altpfad als Rückfallebene.
        ///
        /// Der Schalter ändert Ergebnisse — bei Projekten mit Puffer-Senke deutlich. Was
        /// sich ändert und warum, steht im Umsetzungsprotokoll zu Paket 4, Teil 7.
        /// </summary>
        public bool KaskadeZweikanalig = false;

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
        public bool bSimulationSSP = false;
        public bool bSimulationBHKW = false;

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
        /// (Paket-5-Nacharbeit, Befund N10). Der einkanalige Altpfad ist unberührt — er
        /// zeigt seine Meldungen unverändert als Dialog.
        /// </summary>
        public string Fehlertext = "";

        public void Do_Simulation(int ID_Projekt)
        {
            // Engine-Einstieg: Blockade bei nicht abgeschlossener Schema-Migration
            // (ADR-001, Aufgabe 6). Bewusst ohne MessageBox - die Engine bleibt
            // dialogfrei (Konzept 13.4); der Grund steht in Sperrgrund.
            Sperrgrund = "";
            Fehlertext = "";
            string sperrgrund;
            if (SchemaMigration.SimulationGesperrt(out sperrgrund))
            {
                Sperrgrund = sperrgrund;
                Console.WriteLine("Simulation abgebrochen: " + sperrgrund);
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
                Console.WriteLine("Ladeprioritäten: " + nachgezogen + " Feld(er) ohne Vorgabe auf 0 " +
                                  "gesetzt (Konzept 3.4, Vorbelegung wie Migrationsregel R5).");

            // ***********************************************************************
            // Speicher-Registry aufbauen (Paket 4 - Konzept 6.2) und den Senkenspeicher
            // der Wärmepumpe daraus an das WP-Modul geben. Der zweikanalige Weg öffnet
            // die Registry danach noch für seine Speichermenge
            // (RegistryFuerZweikanaligOeffnen); der Altpfad rechnet mit genau diesem
            // einen Senkenspeicher weiter.
            // ***********************************************************************
            SpeicherRegistryAufbauen();
            simulation_wp.Pufferspeicher = puffer_wp;

            // Senkenzuordnungen des Projekts (Konzept 6.1) - ausgewertet nur im
            // zweikanaligen Weg.
            senkenzuordnungen = WaermesenkeClass.SenkenLaden(m_ID_Projekt);

            // Feature-Flag der zweikanaligen Kaskade (Konzept Kapitel 9). Ab Etappe 4b
            // verzweigt es den Rechenweg: gesetzt -> Kaskade_Zweikanalig() nach der
            // Reihenfolge-Invariante 6.3, nicht gesetzt -> der unveränderte Altpfad.
            KaskadeZweikanalig = ctrl_konfig != null && ctrl_konfig.model != null &&
                                 ctrl_konfig.model.Kaskade_Zweikanalig;
            if (KaskadeZweikanalig)
                Console.WriteLine("Projekteinstellung Kaskade_Zweikanalig ist gesetzt - " +
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

            // Startpunkt der Simulation ist der Wärmebedarf
            Eingang = simulation_Waermebedarf.Waermebedarf;

            // ***********************************************************************
            // Etappe 4b: die zweikanalige Kaskade mit herausgelöster Ladephase
            // (Konzept 6.3) als EIGENER Rechenweg hinter dem Feature-Flag.
            //
            // Die Verzweigung steht bewusst VOR der bestehenden Schleife und nicht in
            // ihr: Der einkanalige Altpfad bleibt damit Zeile für Zeile unverändert und
            // ist als Rückfallebene durch Lesen nachweisbar, nicht erst durch Messen.
            // ***********************************************************************
            if (KaskadeZweikanalig)
            {
                Kaskade_Zweikanalig();
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    if (tool[i] == "Wärmepumpe")
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
                    else if (tool[i] == "Heizkessel")
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
                    else if (tool[i] == "Solarthermie")
                    {
                        Ausgang = Simulation_Solarthermie_Ctrl(Eingang);

                        Restwaerme = 0;
                        for (int n = 0; n < 8760; n++) Restwaerme += Ausgang[n];
                        Rest_Waermebedarf_stuendlich = Ausgang;
                        Eingang = Ausgang;

                        bSimulationSolarthermie = true;
                    }
                    else if (tool[i] == "BHKW")
                    {
                        Ausgang = Simulation_BHKW_Ctrl(Eingang, Viertelstunden_zu_Stundenwerte_Mittelwert(Rest_Strombedarf_viertelstuendlich));

                        Restwaerme = Ausgang.Sum();
                        Rest_Waermebedarf_stuendlich = Ausgang;
                        Eingang = Ausgang;

                        // Erzeugung holen und in Viertelstunden wandeln
                        float[] bhkwStromStuendlich = simulation_bhkw.stromproduktion;
                        float[] bhkwStromViertelstuendlich = Stundenwerte_zu_viertelstunden(bhkwStromStuendlich);

                        // Erzeugung vom Vektor abziehen
                        Rest_Strombedarf_viertelstuendlich = SubVectors(Rest_Strombedarf_viertelstuendlich, bhkwStromViertelstuendlich, false);
                        bSimulationBHKW = true;
                    }
                }
            }

            // Photovoltaik abziehen
            if (tool[4] == "Photovoltaik")
            {
                var x = Rest_Strombedarf_viertelstuendlich.Sum() / 4000;
                temp = Simulation_Photovoltaik_Ctrl(Rest_Strombedarf_viertelstuendlich);
                Rest_Strombedarf_viertelstuendlich = SubVectors(Rest_Strombedarf_viertelstuendlich, temp);
                bSimulationPV = true;
            }

            // Stromspeicher verrechnen
            if (tool[5] == "Stromspeicher")
            {
                temp = Simulation_Stromspeicher_Ctrl(Rest_Strombedarf_viertelstuendlich);
                Rest_Strombedarf_viertelstuendlich = SubVectors(Rest_Strombedarf_viertelstuendlich, temp);
                bSimulationSSP = true;
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

            if (simulation_Waermebedarf.Kanal_Kappungen > 0)
                Console.WriteLine("Kanalbildung: in " + simulation_Waermebedarf.Kanal_Kappungen +
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
            _wpInSchleife = KaskadeEnthaelt("Wärmepumpe");
            _solarInSchleife = KaskadeEnthaelt("Solarthermie") &&
                               ErzeugerMitPufferSenke(ProjektPuffer.TYP_SOLARTHERMIE);
            _kesselInSchleife = KaskadeEnthaelt("Heizkessel") &&
                                ErzeugerMitPufferSenke(ProjektPuffer.TYP_KESSEL);

            // PAKET 6: Das BHKW ist Kaskadenteilnehmer, sobald es einen Speicher hat -
            // entweder über eine Puffer-Senke (WS_ID_Puffer/WS_ID_Puffer2, derselbe Test
            // wie bei Solarthermie und Kessel) oder über seinen PENDELSPEICHER, der im
            // neuen Weg durch eine SimulationPufferspeicher-Instanz abgelöst wird
            // (Konzept 6.5, zweiter Punkt). Ohne beides hat es keine Speicherbeteiligung
            // und bleibt Vektorstufe an seiner Kaskadenposition - zweikanalig, aber ohne
            // die Phasen A, C, D, E und G.
            _bhkwInSchleife = KaskadeEnthaelt("BHKW") &&
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

                if (tool[i] == "Heizkessel")
                {
                    // Vektorstufe: zweikanalig, aber ohne Speicherbeteiligung.
                    Simulation_SPK_Ctrl_Zweikanalig(kanaele,
                        Viertelstunden_zu_Stundenwerte_Mittelwert(Rest_Strombedarf_viertelstuendlich),
                        ctrl_konfig.model.m_Kessel_Betriebsbereitschaft);

                    temp = Stundenwerte_zu_viertelstunden(simulation_spk.Stromverbrauch_stuendlich);
                    Rest_Strombedarf_viertelstuendlich = AddVectors(Rest_Strombedarf_viertelstuendlich, temp);

                    bSimulationKessel = true;
                }
                else if (tool[i] == "Solarthermie")
                {
                    // Vektorstufe: zweikanalig, aber ohne Speicherbeteiligung.
                    Simulation_Solarthermie_Ctrl_Zweikanalig(kanaele);

                    bSimulationSolarthermie = true;
                }
                else if (tool[i] == "BHKW")
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
                if (tool[i] == "Wärmepumpe" && wp < 0) wp = i;
                if (tool[i] == "Heizkessel" && kessel < 0) kessel = i;
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

            return (tool[position] == "Wärmepumpe" && _wpInSchleife) ||
                   (tool[position] == "Solarthermie" && _solarInSchleife) ||
                   (tool[position] == "Heizkessel" && _kesselInSchleife) ||
                   (tool[position] == "BHKW" && _bhkwInSchleife);
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
                if (tool[i] == "Solarthermie" && !_solarInSchleife)
                {
                    _solarInSchleife = true;
                    Console.WriteLine("Kaskade: Die Solarthermie steht zwischen zwei Erzeugern der " +
                                      "Speicherstufe. Sie rechnet deshalb als Mitglied der " +
                                      "Stundenschleife an ihrer Kaskadenposition mit (Phase B) - " +
                                      "ohne Puffer-Senke als reine Heizkreis-Stufe.");
                }
                else if (tool[i] == "Heizkessel" && !_kesselInSchleife)
                {
                    _kesselInSchleife = true;
                    Console.WriteLine("Kaskade: Der Heizkessel steht zwischen zwei Erzeugern der " +
                                      "Speicherstufe. Er rechnet deshalb als Mitglied der " +
                                      "Stundenschleife an seiner Kaskadenposition mit (Phase B) - " +
                                      "ohne Puffer-Senke als reine Heizkreis-Stufe.");
                }
                else if (tool[i] == "BHKW" && !_bhkwInSchleife)
                {
                    // PAKET 6: Seit das BHKW stundenweise rechnen kann, gilt für es
                    // dieselbe Regel wie für Solarthermie und Kessel. Der frühere
                    // Sonderfall - "BHKW zwischen zwei Mitgliedern rechnet DANACH" - ist
                    // damit entfallen, und mit ihm sein Warnzweig.
                    _bhkwInSchleife = true;
                    Console.WriteLine("Kaskade: Das BHKW steht zwischen zwei Erzeugern der " +
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
                if (m_bError) return;

                QuellspeicherUebernehmen();
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

            foreach (string hinweis in kontext.Hinweise) Console.WriteLine(hinweis);

            // Paket-5-Nacharbeit, Befund N5: Eine Puffer-Hauptsenke, aus der kein
            // Ladeauftrag entstanden ist, darf den Erzeuger nicht stillegen.
            PufferSenkenOhneAuftragZurueckfallen(kontext);

            schleife.Kontext = kontext;
            schleife.Bedarfsreihenfolge = BedarfsreihenfolgeAufbauen();

            // --- 5. Stundenschleife A–G ------------------------------------------------
            m_bError = !schleife.Rechnen(kanaele);
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

            Console.WriteLine("Wärmesenke: Die Anlage " + idAnlage + " (" + art + ") ist als " +
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
                if (tool[i] == "Wärmepumpe" && _wpInSchleife) reihenfolge.Add(ProjektPuffer.TYP_WP);
                else if (tool[i] == "Solarthermie" && _solarInSchleife) reihenfolge.Add(ProjektPuffer.TYP_SOLARTHERMIE);
                else if (tool[i] == "Heizkessel" && _kesselInSchleife) reihenfolge.Add(ProjektPuffer.TYP_KESSEL);
                else if (tool[i] == "BHKW" && _bhkwInSchleife) reihenfolge.Add(ProjektPuffer.TYP_BHKW);
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
                Console.WriteLine("Speicherstufe: Die Wärmepumpen des Projekts " + m_ID_Projekt +
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
                Console.WriteLine("Speicherstufe: Die Heizkessel des Projekts " + m_ID_Projekt +
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
                Console.WriteLine("Speicherstufe: Die Kollektorfelder des Projekts " + m_ID_Projekt +
                                  " ließen sich nicht lesen - die Stufe rechnet ohne Module.");
                return;
            }

            foreach (DataRow r in dt.Rows)
                simulation_solarthermie.solarthermie_list.Add(StilleDb.Zahl(StilleDb.Feld(r, "ID_SOLAR")));
        }

        /// <summary>
        /// BHKW-Liste und Anlagen-IDs des zweikanaligen Wegs (Paket 6) — dieselbe Abfrage
        /// wie <see cref="Simulation_BHKW_Ctrl"/>, nur dialogfrei und parametrisiert über
        /// <see cref="StilleDb"/> (Befund N9 der Paket-5-Nacharbeit). Ohne <c>ORDER BY</c>,
        /// damit die Modulreihenfolge dieselbe ist wie im Altpfad.
        ///
        /// <c>bhkwGrenzL</c> wird hier wie im Altpfad aus der ANLAGE vorbelegt;
        /// <c>SimulationBHKW.Moduldaten_Einlesen</c> überschreibt den Wert anschließend
        /// aus dem Katalog, sofern dort eine Grenzleistung hinterlegt ist. Dieses
        /// Bestandsverhalten bleibt unangetastet.
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
                Console.WriteLine("Speicherstufe: Die BHKW des Projekts " + m_ID_Projekt +
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
                Fehlertext = "BHKW-Pendelspeicher: Für Projekt " + m_ID_Projekt + " ist ein Volumen " +
                             "von " + VolumenPendelspeicherBHKW + " l bekannt, aber es gibt keine " +
                             "Puffer-Zeile „" + ProjektPuffer.BEZ_PENDELSPEICHER + "\". Der Lauf " +
                             "wurde abgebrochen, damit das BHKW nicht stillschweigend ohne " +
                             "Speicher rechnet.";
                Console.WriteLine(Fehlertext);
                m_bError = true;
                return;
            }

            WaermesenkeClass.PufferInfo p = WaermesenkeClass.PufferLesen(idPuffer);
            if (p == null || p.ID_Projekt != m_ID_Projekt)
            {
                Fehlertext = "BHKW-Pendelspeicher: Die Puffer-Zeile " + idPuffer + " des Projekts " +
                             m_ID_Projekt + " ließ sich nicht lesen oder gehört zu einem anderen " +
                             "Projekt. Der Lauf wurde abgebrochen, damit das BHKW nicht " +
                             "stillschweigend ohne Speicher rechnet.";
                Console.WriteLine(Fehlertext);
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
                    Console.WriteLine("BHKW-Pendelspeicher: Puffer " + p.ID + " (" + p.Bezeichner +
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
                SpeicherAufnehmen(sp, true);
            }

            sp.ImRechenpfad = true;
            if (!k.AlleSpeicher.Contains(sp)) k.AlleSpeicher.Add(sp);

            // ENTLADEREIHENFOLGE (Befund N7): einsortieren statt anhängen. Ein Puffer mit
            // gepflegter Entladeprio gehört an seine Stelle in der Ordnung des Kanals
            // (Konzept 3.6) — dieselbe Reihenfolge, die die Pufferverwaltung anzeigt.
            EntladeordnungEinsortieren(k.Entladeordnung(sp.IstBrauchwasserkanal), sp);

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

            Console.WriteLine("BHKW-Pendelspeicher: Keine Puffer-Senke am BHKW - der Speicher „" +
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
        private void EntladeordnungEinsortieren(List<SimulationPufferspeicher> ordnung,
                                                SimulationPufferspeicher sp)
        {
            if (ordnung == null || sp == null || ordnung.Contains(sp)) return;

            string verwendung = sp.IstBrauchwasserkanal
                ? WaermesenkeClass.VERWENDUNG_BRAUCHWASSER : WaermesenkeClass.VERWENDUNG_HEIZUNG;

            List<Ladeordnung.EntladeEintrag> soll =
                Ladeordnung.Entladereihenfolge(m_ID_Projekt, verwendung);

            int platz = Ladeordnung.Position(soll, sp.ID_Pufferspeicher);
            if (platz <= 0)
            {
                Console.WriteLine("BHKW-Pendelspeicher: Der Speicher " + sp.ID_Pufferspeicher +
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

            foreach (int id in _speicherReihenfolge)
            {
                SimulationPufferspeicher sp;
                if (!speicherRegistry.TryGetValue(id, out sp) || sp == null) continue;

                if (sp.IstQuelle) { sp.ImRechenpfad = true; continue; }

                // Ausdrücklich ZUWEISEN, nicht nur setzen: Der Senkenspeicher aus der
                // Alt-Zuordnung trägt das Flag schon aus dem Registry-Aufbau. Ohne
                // Senkenreferenz gehört er im zweikanaligen Weg trotzdem nicht in den
                // Rechenpfad — ihn lädt hier niemand.
                bool referenziert = senken.Contains(id);
                if (sp.ImRechenpfad && !referenziert)
                    Console.WriteLine("Speicher-Registry: Puffer " + id + " (" + sp.BezeichnerAnzeige() +
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
                if (sp.IstBrauchwasserkanal != brauchwasser) continue;
                if (!liste.Contains(sp)) liste.Add(sp);
            }

            // Sicherheitsnetz: ein Registry-Speicher dieses Kanals, den die
            // Entladereihenfolge nicht kennt (Projektzuordnung inkonsistent), fiele sonst
            // stillschweigend aus der Bilanz. Er kommt ans Ende - Reihenfolge der Aufnahme.
            foreach (SimulationPufferspeicher sp in k.AlleSpeicher)
            {
                if (sp == null || sp.IstQuelle) continue;
                if (sp.IstBrauchwasserkanal != brauchwasser) continue;
                if (liste.Contains(sp)) continue;

                Console.WriteLine("Speicher " + sp.ID_Pufferspeicher + " (" + sp.BezeichnerAnzeige() +
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
                        k.Hinweise.Add("Ladeordnung: Anlage " + e.ID_Anlage + " (" + e.Erzeuger +
                                       ") lädt laut Konfiguration den Speicher " + sp.ID_Pufferspeicher +
                                       " (" + sp.BezeichnerAnzeige() + "). Diese Erzeugerart rechnet " +
                                       "in diesem Lauf nicht in der Speicherstufe; die Anlage rechnet " +
                                       "als Vektorstufe wie eine Heizkreis-Anlage.");
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
                if (pspZuordnung.items[n].Erzeuger != "Wärmepumpe") continue;

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
                    pufferWp.Verwendung = SimulationPufferspeicher.VERWENDUNG_HEIZUNG;
                    pufferWp.Init(psp.items[0].Gesamtvolumen,
                                  vorlauf,
                                  ruecklauf,
                                  psp.items[0].Betriebsbereitschaftverlust);
                    RueckfallMelden(pufferWp, psp.items[0].ID, psp.items[0].Name);

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
                    Console.WriteLine("Speicher-Registry: Puffer " + id +
                                      " ist referenziert, existiert aber nicht mehr.");
                    continue;
                }
                if (p.ID_Projekt != m_ID_Projekt)
                {
                    Console.WriteLine("Speicher-Registry: Puffer " + id + " gehört zu Projekt " +
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
                        Console.WriteLine("Speicher-Registry: Puffer " + p.ID + " (" + p.Bezeichner +
                                          ") hat kein Temperaturpaar in der Projektkopie - es gilt " +
                                          "die Zuordnungszeile (" + vorlauf + "/" + ruecklauf + " °C).");
                    }
                }

                sp.Init(p.Gesamtvolumen, vorlauf, ruecklauf, p.Bereitschaftsverluste);
                RueckfallMelden(sp, p.ID, p.Bezeichner);

                // PufferInfo führt die Schwellen in PROZENT (Ladeordnung-Vorgaben),
                // SimulationPufferspeicher als Anteil 0..1.
                sp.SchwelleEin = p.SchwelleEin / 100.0;
                sp.SchwelleAus = p.SchwelleAus / 100.0;
                sp.SchwelleAusNachrang = p.SchwelleAusNachrang / 100.0;
                sp.Entladeprio = p.Entladeprio;

                // Beim Aufbau nicht im Rechenpfad; der zweikanalige Weg öffnet ihn, wenn
                // eine Anlage ihn als Senke führt (siehe ImRechenpfad).
                SpeicherAufnehmen(sp, false);
            }
        }

        /// <summary>
        /// Meldet einen ΔT-RÜCKFALL eines Speichers auf die Konsole (Nacharbeit Paket 6,
        /// Befund N2).
        ///
        /// Ohne gepflegtes Temperaturpaar rechnet <c>SimulationPufferspeicher.Init</c>
        /// mit einem Ersatzwert — 10 K für gewöhnliche Puffer, 20 K für den
        /// BHKW-Pendelspeicher. Beides verändert die nutzbare Kapazität erheblich (bei
        /// einem 1000-l-Puffer 11,6 gegen 23,2 kWh), und beides geschah bis zur
        /// Nacharbeit stillschweigend. Projektgrundsatz: sichtbar falsch ist besser als
        /// still falsch.
        ///
        /// Die Meldung läuft in BEIDEN Rechenwegen — der Registry-Aufbau gehört zu
        /// keinem von beiden. Sie ist reine Konsolenausgabe und geht in kein Ergebnis
        /// und in keine CSV ein; der Altpfad rechnet unverändert.
        /// </summary>
        private static void RueckfallMelden(SimulationPufferspeicher sp, int idPuffer, string bezeichner)
        {
            if (sp == null || sp.RueckfallDeltaT <= 0) return;

            Console.WriteLine("Speicher-Registry: Puffer " + idPuffer + " (" +
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
        private void QuellspeicherUebernehmen()
        {
            if (simulation_wp == null || simulation_wp.Quellspeicher == null) return;

            foreach (SimulationPufferspeicher q in simulation_wp.Quellspeicher)
            {
                if (q == null) continue;
                if (q.ID_Projekt <= 0) q.ID_Projekt = m_ID_Projekt;

                if (SpeicherAufnehmen(q, true)) continue;

                // Schon unter derselben Instanz aufgenommen (mehrere Module, zusammengeführt).
                if (q.ID_Pufferspeicher > 0 && speicherRegistry.ContainsKey(q.ID_Pufferspeicher) &&
                    ReferenceEquals(speicherRegistry[q.ID_Pufferspeicher], q)) continue;

                SimulationPufferspeicher belegt = null;
                if (q.ID_Pufferspeicher > 0) speicherRegistry.TryGetValue(q.ID_Pufferspeicher, out belegt);

                Console.WriteLine("Speicher-Registry: Puffer " + q.ID_Pufferspeicher + " ist QUELLE der " +
                                  "Anlage " + q.ID_Anlage + " und steht zugleich als " +
                                  ((belegt != null && !belegt.IstQuelle) ? "SENKE" : "weiterer Eintrag") +
                                  " in der Registry (Kurzschluss, Konzept 4.6). Die Quell-Instanz rechnet " +
                                  "mit und wird bilanziert, aber die Konfiguration ist zu prüfen.");

                q.ImRechenpfad = true;
                if (!_zusatzSpeicher.Contains(q)) _zusatzSpeicher.Add(q);
            }
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

            // Quellspeicher der Module in die Registry übernehmen (Konzept 6.2).
            // Erst JETZT, weil sie beim Modulaufbau entstehen - und mit denselben
            // Instanzen, nicht mit Kopien: Genau das ist die geforderte Ablösung der
            // parallelen Liste wp_quellspeicher.
            QuellspeicherUebernehmen();

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

            if (!pvModus || tool == null || tool.Length < 5 || tool[4] != "Photovoltaik") return null;

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
                Console.WriteLine("PV-Überschuss konnte nicht vorab bestimmt werden: " + ex.Message);
                simulation_pv.Init();
                return null;
            }
        }

        private float[] Simulation_BHKW_Ctrl(float[] Waermebedarf, float[] Strombedarf)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass. BHKW_TYP);

            simulation_bhkw.bhkw_list.Clear();
            simulation_bhkw.bhkw_list_Namen.Clear();
            // Anlagen-IDs parallel zu bhkw_list (Konzept 6.2). bhkw_list trägt die
            // ID_BHKW (Katalogzeile), nicht die Anlage - für Senken, Ladeprioritäten und
            // die Speicherzuordnung braucht Etappe 4b aber Tab_Energieanlagen.ID.
            simulation_bhkw.bhkw_anlagen_ids.Clear();

            int i = 0;
            while (rs.Next())
            {
                simulation_bhkw.bhkw_list.Add((int)rs.Read("ID_BHKW"));
                simulation_bhkw.bhkw_anlagen_ids.Add((int)rs.Read("ID"));
                simulation_bhkw.bhkw_list_Namen.Add((string)rs.Read("Bezeichner"));
                double Grenzleistung = (double)rs.Read("Grenzleistung") / 100;
                simulation_bhkw.bhkwGrenzL[i++] = (float)Grenzleistung;
            }
            rs.Close();

            simulation_bhkw.waermebedarf = Waermebedarf;
            simulation_bhkw.strombedarf = Strombedarf;
            simulation_bhkw.bhkwGrenzleistungAllgemein = GrenzleistungBHKW;
            // Kapazität des Pendelspeichers aus dem Volumen in LITERN (Etappe 3):
            //   Liter · 1,163 Wh/(l·K) · 20 K / 1000 = Liter · 20 / 860 [kWh]
            // Formelgleich zur Altfassung "m³ · 20000 / 860", weil die Migration den
            // Alt-Parameter mit dem Faktor 1000 in Liter überführt hat: 800 l ergeben
            // 16000/860 = 18,60 kWh, genau wie 0,8 · 20000/860. Die Zwischenprodukte
            // (Liter · 20 bzw. m³ · 20000) sind gleich groß und in float exakt
            // darstellbar, das Ergebnis ist damit bitgleich.
            //
            // ACHTUNG, bewusster Verhaltensunterschied: das Feld war IMMER int. Der
            // Alt-Parameter in m³ wurde deshalb auf ganze Kubikmeter abgeschnitten
            // ((int)0,8 = 0, (int)1,5 = 1) - die Nachkommastelle der Eingabe war
            // wirkungslos. In Litern verschwindet dieser Effekt; Projekte mit einem
            // Alt-Wert, der keine ganze Zahl war, rechnen jetzt mit dem eingegebenen
            // Volumen statt mit dem abgeschnittenen.
            simulation_bhkw.kapazitaetPendelspeicher = (float)VolumenPendelspeicherBHKW * 20 / 860;
            simulation_bhkw.modeBHKW = modeBHKW;

            // Simulation starten
            m_bError = !simulation_bhkw.Berechnung(m_ID_Projekt);

            float[] restwaerme = SubVectors(Waermebedarf, simulation_bhkw.waermeproduktion);
      
            return m_bError ? Waermebedarf : restwaerme;
        }

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
            }
            rs.Close();

            simulation_spk.Waermebedarf = Waermebedarf;
            simulation_spk.Strombedarf_stuendlich = Strombedarf;
            simulation_spk.Vorgabe_Betriebsbereitschaft = nBereitschaft;
            
            // Simulation starten
            simulation_spk.Berechnung(m_ID_Projekt);

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

            Console.WriteLine($"--- PV TESTLAUF ---");
            Console.WriteLine($"Potenzielle Produktion: {ergebnis.produktion} kW");

            if (ergebnis.produktion < 8.0)
            {
                Console.WriteLine("WARNUNG: Der Wert ist zu niedrig für 10kWp bei 1000W/m²!");
                Console.WriteLine("Prüfe: Ist h0 wirklich 0.20? Wird flaeche korrekt übergeben?");
            }
            else
            {
                Console.WriteLine("ERGEBNIS OK: Die Formel arbeitet physikalisch korrekt.");
            }
        }

        private float[] Simulation_Stromspeicher_Ctrl(float[] Strombedarf)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SP_TYP);

            simulation_ssp.stromspeicher_list.Clear();
            while (rs.Next())
            {
                simulation_ssp.stromspeicher_list.Add((int)rs.Read("ID_SP"));
            }
            rs.Close();

            simulation_ssp.Strombedarf = Strombedarf;

            // Simulation starten
            float[] temp = simulation_ssp.Berechnung(m_ID_Projekt);
            return temp;
        }

    }
}

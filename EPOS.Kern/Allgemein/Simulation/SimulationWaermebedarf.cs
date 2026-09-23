using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    public class SimulationWaermebedarf
    {
        public bool DBGelesen = false;
        public int Anzahl_Gebaeude = 0;
        public int Anzahl_Bewohner = 0;
        public double Wohnflaeche = 0;
        public int m_ID_Projekt = 0;

        // Wochenendtage des Klimakalenders (gemeinsamer Teil). Die Einstrahlung, die
        // Tagesmitteltemperatur und die Tagestypen stehen im Altweg-Teil des Kalenders.
        private bool[] WE = new bool[365];

        // Gebäudeprofil Wärmebedarf
        public double[] Waermebedarf = new double[8760];
        public double[] Waermebedarf_Gebaeude = new double[8760];
        public double[] Waermebedarf_Gebaeude_Monat = new double[12];
        public double[] Waermebedarf_sortiert = new double[8760];
        public double Waermebedarf_Max = 0;
        public double Waermebedarf_Gesamt = 0;
        public double Waermebedarf_Gebaeude_Gesamt = 0;


        // Brauchwasser Wärmeenergie 
        public double[] Waermebedarf_Brauchwasser_Monat = new double[12];
        public double Waermebedarf_Brauchwasser = 0;

        // Lastgang Gebäude
        public double[] Waermebedarf_Extern = new double[8760];
        public double Waermebedarf_Extern_Gesamt = 0;

        // Prozesswärme
        public double[] Waermebedarf_Prozess_Monat = new double[12];
        public double Waermebedarf_Prozess = 0;

        // Temperaturgang Klimaregion
        public double[] Stundentemperatur = new double[8760];

        public double[] Dauerlinie = new double[8760];
        public double[] Dauerlinie_nicht_sortiert = new double[8760];

        // Netzverluste
        public int Netzverluste = 0;
        public double Waermebedarf_Netzverluste = 0;
        public string Netzverluste_Einheit = "";

        // Monat-, Wochenwärme
        public int[] mo_anfang = new int[12];
        public int[] mo_ende = new int[12];

        /// <summary>
        /// PROZESSKANAL je Stunde [kWh]. Bis Paket K1 der reine Profilanteil; seit K1
        /// enthält der Vektor nach <see cref="Waermebedarf_berechnen"/> zusätzlich den
        /// anteiligen NETZVERLUST des Prozesskanals (Konzept 4.2/F2). Die ausgewiesene
        /// Energiemenge <see cref="Waermebedarf_Prozess"/> und die Monatswerte bleiben
        /// dagegen der reine Profilanteil — sie sind die Bedarfsmeldung des Anwenders,
        /// nicht die Kanalbilanz.
        /// </summary>
        public double[] prozesswerte = new double[8760];

        /// <summary>
        /// BRAUCHWASSERKANAL je Stunde [kWh] — dieselbe K1-Änderung wie bei
        /// <see cref="prozesswerte"/>: Der Vektor trägt nach
        /// <see cref="Waermebedarf_berechnen"/> den anteiligen Netzverlust mit.
        ///
        /// Das ist die GEWOLLTE Wirkung von F2 an den Altlesern: Die Wärmepumpe und die
        /// Detailansicht lesen hier den Warmwasseranteil des Bedarfs
        /// (<c>SimulationControl.Simulation_WP_Ctrl</c>,
        /// <c>Form_Simulation_Detail.WarmwasserAnteil</c>) — und der ist mit F2 eben nicht
        /// mehr netzverlustfrei. Die WW-Deckungsgrade ändern sich dadurch in jedem Projekt
        /// mit Brauchwasseranteil (dokumentierte Ergebnisänderung, Konzept 11.2).
        /// </summary>
        public double[] brauchwasserwerte = new double[8760];

        /// <summary>
        /// Wochentag des 1. Januar aus den Klimadaten (Montag = 0 … Sonntag = 6,
        /// Entscheidung F3). Wird in <see cref="Waermebedarf_berechnen"/> aus
        /// <c>Tab_Klimadaten.WE</c> abgeleitet und an die Profilroutine gegeben; vor dem
        /// ersten Lauf steht hier die Altkonvention (Sonntag).
        /// </summary>
        public int WochentagJan1 = ProfilBedarf.WOCHENTAG_ALTKONVENTION;

        /// <summary>
        /// Die drei Bedarfskanäle des Laufs (Konzept 4.1/4.2). Sie sind seit Paket K1 die
        /// FÜHRENDE Größe: <see cref="Waermebedarf"/> ist ihre Summe, nicht umgekehrt.
        /// Gelesen wird über <see cref="KanaeleDrei"/> (Kopie) — die Übergangsabbildung
        /// auf die zweikanalige Struktur ist mit Paket S1 gelöscht (K2-O3).
        /// </summary>
        private Kanalsatz _kanaele = new Kanalsatz();

        // =====================================================================
        //  Gebäudebedarfsrechnung: Fassade, Vorbereitung, Weiche (Stufe G1.0, E20,
        //  ADR-006). Die Fassade ruft den modellfreien Vorbereitungsschritt, die Weiche
        //  wählt je Gebäude GENAU EINEN Rechenweg hinter IGebaeudeRechenweg.
        // =====================================================================

        /// <summary>Der Klimakalender des Laufs, gefüllt in <see cref="KlimakalenderLesen"/>.</summary>
        private readonly Klimakalender _kalender;

        /// <summary>
        /// Der Tagesbilanz-Weg (Modul <c>Altweg/</c>) — aufgebaut je Lauf in
        /// <see cref="KlimakalenderLesen"/> mit dem Altweg-Teil des Kalenders.
        /// </summary>
        private Altweg.TagesbilanzRechenweg _altweg;

        /// <summary>
        /// Der VDI-6007-Weg (Modul <c>Gebaeude/</c>), angebunden in Stufe G1. Er hält keinen
        /// Zustand über ein Gebäude hinaus; sein Ergebnisträger wird je Lauf in
        /// <see cref="KlimakalenderLesen"/> geleert.
        /// </summary>
        private readonly Vdi6007Rechenweg _vdi6007;

        /// <summary>
        /// <b>Die NULL-Regel der Weiche — die eine, benannte Stelle.</b> Ein Gebäude ohne
        /// Angabe (<c>Gebaeude_Modell</c> NULL) rechnet <b>in dieser Welle</b> auf dem
        /// Tagesbilanz-Weg; so bleibt der Referenzlauf gegen die Basis byte-gleich, und allein
        /// ausdrücklich auf <see cref="DbWerte.GEBAEUDE_MODELL_VDI6007"/> gestellte Gebäude
        /// rechnen stündlich.
        /// <para><b>Schlusswelle G1+G2 schaltet NULL auf VDI6007</b> (E1): dann steht hier
        /// <see cref="DbWerte.GEBAEUDE_MODELL_VDI6007"/>, und die Basis wird neu eingefroren.</para>
        /// </summary>
        internal const string MODELL_OHNE_ANGABE = DbWerte.GEBAEUDE_MODELL_TAGESBILANZ;

        /// <summary>
        /// Die Ergebnisse der Gebäude des VDI-Wegs in diesem Lauf, je Merkplatz (Reihen und
        /// Kennzahlen, skaliert nach E8). Ein Gebäude auf dem Tagesbilanz-Weg hat keinen
        /// Eintrag. Geleert in <see cref="KlimakalenderLesen"/>.
        /// </summary>
        internal GebaeudeErgebnistraeger GebaeudeErgebnisse { get; } = new GebaeudeErgebnistraeger();

        /// <summary>Der Tagesbilanz-Weg dieses Laufs — Zugang für die Tests des Altwegs.</summary>
        internal Altweg.TagesbilanzRechenweg Tagesbilanzweg => _altweg;

        /// <summary>Der VDI-6007-Weg dieses Laufs — Zugang für Tests und die Messung zu U6.</summary>
        internal Vdi6007Rechenweg Vdi6007weg => _vdi6007;

        /// <summary>Der Klimakalender dieses Laufs — Zugang für die Tests.</summary>
        internal Klimakalender Kalender => _kalender;

        public SimulationWaermebedarf()
        {
            Classes.Simulation.Init init = new Classes.Simulation.Init();
            init.Monatswerte_berechnen(mo_anfang, mo_ende);

            _kalender = new Klimakalender(
                new KlimakalenderGemeinsam(WE, Stundentemperatur, mo_anfang, mo_ende),
                new KlimakalenderAltweg());
            _altweg = new Altweg.TagesbilanzRechenweg(_kalender.Altweg);
            _vdi6007 = new Vdi6007Rechenweg(GebaeudeErgebnisse);
        }

        public class Ergebnis
        {
            public Ergebnis()
            {
                Waermebedarf_Max = 0;
                Gesamt_Waermebedarf = 0;
            }
            public double Waermebedarf_Max;
            public double Gesamt_Waermebedarf;

        };

        public void Waermebedarf_berechnen(int ID_Projekt, int ID_Klimaregion)
        {
            Z_ProjektGebGanglinieCtrl waectrl;
            RecordSet rs;


            m_ID_Projekt = ID_Projekt;
            /*
            com.I_vector_init(ref Dauerlinie);
            com.I_vector_init(ref Dauerlinie_nicht_sortiert);
            com.I_vector_init(ref Waermebedarf_Extern);
            com.I_vector_init(ref Waermebedarf);
            com.I_vector_init(ref Waermebedarf_Gebaeude);
            com.I_vector_init(ref Waermebedarf_sortiert);
            com.I_vector_init(ref prozesswerte);
            com.I_vector_init(ref brauchwasserwerte);
            */

            WPPlan.Core.BhkwPlan.VectorInit(Dauerlinie);
            WPPlan.Core.BhkwPlan.VectorInit(Dauerlinie_nicht_sortiert);
            WPPlan.Core.BhkwPlan.VectorInit(Waermebedarf_Extern);
            WPPlan.Core.BhkwPlan.VectorInit(Waermebedarf);
            WPPlan.Core.BhkwPlan.VectorInit(Waermebedarf_Gebaeude);
            WPPlan.Core.BhkwPlan.VectorInit(Waermebedarf_sortiert);
            WPPlan.Core.BhkwPlan.VectorInit(prozesswerte);
            WPPlan.Core.BhkwPlan.VectorInit(brauchwasserwerte);

            // ---------------------------------------------------------------
            // PAKET K1 (Konzept 4.2): Kanalbildung OHNE Residuum.
            //
            // Die drei Bedarfsarten werden ab hier GETRENNT bis zum Schluss geführt:
            //   HEIZUNG      = Gebäudewärme + externe Lastgänge mit Kanal „Heizung"
            //   BRAUCHWASSER = Brauchwasserprofile + Lastgänge mit Kanal „Brauchwasser"
            //   PROZESS      = Prozessprofile     + Lastgänge mit Kanal „Prozesswaerme"
            // Danach werden die Netzverluste anteilig verteilt (F2), und erst daraus
            // entsteht der Summenvektor Waermebedarf.
            // ---------------------------------------------------------------
            _kanaele = new Kanalsatz();
            double[] kanalHeizung = _kanaele.Heizung;

            // ENERGIEPROBE (Konzept 11.3): eine UNABHÄNGIGE Summe aller Bedarfsanteile,
            // in double und ohne Kanalzuordnung mitgeführt. Sie ist der Gegenwert, an dem
            // am Ende die Kanalsumme gemessen wird - eine Kanalzuordnung, die einen
            // Anteil verschluckt oder doppelt bucht, fällt genau hier auf.
            double[] probe = new double[8760];

            //  if (!DBGelesen)
            KlimakalenderLesen(ID_Klimaregion);

            ProjektGebaeudeCtrl ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(ID_Projekt);

            // V0-1: Puffer für GENAU EIN Gebäude. BhkwPlan.StdWerte addiert auf den
            // vorhandenen Inhalt seines Zielvektors; bis hierher lief die Schleife auf dem
            // kumulierten Waermebedarf_Gebaeude und addierte diesen je Durchlauf erneut auf
            // Waermebedarf — bei N Gebäuden ging Gebäude 1 N-fach ein. Jetzt rechnet jedes
            // Gebäude in einen genullten Einzelpuffer, der danach genau einmal auf
            // Waermebedarf UND einmal auf Waermebedarf_Gebaeude geht. Waermebedarf_Gebaeude
            // bleibt damit wie bisher die Summe aller Gebäude. Bei einem Gebäude ist das
            // Ergebnis bitgleich zum bisherigen Verhalten.
            double[] Waermebedarf_EinGebaeude = new double[8760];

            for (int i = 0; i < ctrl.rows; i++)
            {
                // Der Rumpf je Gebäude ist die Fassade HeizwaermeEinesGebaeudes (Vorbereitung,
                // Weiche, genau ein Rechenweg), damit der Gebaeudedialog GENAU DIESE Rechnung
                // fuer EIN Gebaeude fahren kann. false = eine Vorbedingung des Rechenwegs
                // fehlt (Tagesbilanz: die Tagesverteilung), und der Abbruch der
                // Bedarfsrechnung bleibt an derselben Stelle wie bisher.
                if (!HeizwaermeEinesGebaeudes(ctrl.items[i], i, Waermebedarf_EinGebaeude)) return;

                //com.CSharp_I_vectoren_addieren(Waermebedarf_Gebaeude, Waermebedarf);
                // K1: Gebäudewärme geht in den HEIZKANAL statt in den Summenvektor.
                WPPlan.Core.BhkwPlan.VectorenAddieren(Waermebedarf_EinGebaeude, kanalHeizung);

                // Energieprobe: derselbe Betrag, unabhängig in double (noch in Watt).
                for (int h = 0; h < 8760; h++) probe[h] += Waermebedarf_EinGebaeude[h];

                // V0-1: Waermebedarf_Gebaeude bleibt die Summe ALLER Gebäude, wird aber
                // nicht mehr selbst als Rechenpuffer benutzt.
                WPPlan.Core.BhkwPlan.VectorenAddieren(Waermebedarf_EinGebaeude, Waermebedarf_Gebaeude);
            }

            Anzahl_Gebaeude = ctrl.rows;

            //com.I_Watt_To_Kw(ref Waermebedarf);
            // K1: Der Heizkanal trägt an dieser Stelle genau das, was bisher der
            // Summenvektor trug (nur Gebäudewärme) - die Umrechnung W -> kW bleibt
            // deshalb Anweisung für Anweisung dieselbe.
            WPPlan.Core.BhkwPlan.WattToKw(kanalHeizung);
            for (int h = 0; h < 8760; h++) probe[h] *= 0.001;


            // Wärmebedarf gesamt für alle Gebäude
            //Waermebedarf_Gebaeude_Gesamt = com.I_vector_summe(Waermebedarf);
            Waermebedarf_Gebaeude_Gesamt = kanalHeizung.Sum() / 1000;

            // Wärmebedarf extern 
            waectrl = new Z_ProjektGebGanglinieCtrl();
            waectrl.ReadAll("select * from Z_ProjektWaermebedarf where ID_Projekt=" + m_ID_Projekt);

            Waermebedarf_Extern_Gesamt = 0;
            rs = new RecordSet();

            // V0-5: Je Ganglinie ein eigener, genullter Puffer. Bis hierher lief der
            // Fülllauf direkt auf dem Klassenvektor Waermebedarf_Extern, der nur EINMAL vor
            // der Schleife genullt wurde: Reststunden einer längeren Vorgänger-Ganglinie
            // blieben stehen und gingen ein zweites Mal in die Summe ein. Der Rohpuffer ist
            // auf das Viertelstundenraster ausgelegt, damit auch 35.040 Werte hineinpassen.
            double[] ganglinie_roh = new double[8760 * 4];
            double[] ganglinie = new double[8760];

            for (int n = 0; n < waectrl.rows; n++)
            {
                // BEFUND B1 (S7): Die Spalten wurden bis 02.09.2026 ueber den Namen der
                // zugrunde liegenden TABELLE angesprochen (Tab_Waermebedarf.ID,
                // Tab_WaermebedarfDaten.ID). Jet loest das auf, SQLite nicht - eine Sicht hat
                // nur ihre eigenen Ausgabespalten ("no such column: Tab_Waermebedarf.ID").
                // Die Sicht heisst die zweite ID jetzt ID_Daten (002_views.sql).
                rs.Open("select * from Abfrage_ProjektGebaeudeGanglinie where ID=" + waectrl.items[n].m_ID_Ganglinie + " order by ID_Daten");

                int index = 0;
                double wert = 0;

                Array.Clear(ganglinie_roh, 0, ganglinie_roh.Length);

                while (rs.Next())
                {
                    wert = (double)rs.Read("Wert");
                    // V0-5 (c): Indexschutz. Eine zu lange Reihe (z. B. Minutenwerte) lief
                    // bisher ungefangen in eine IndexOutOfRangeException; gezählt wird
                    // weiter, damit die Rasterprüfung unten die wahre Wertzahl meldet.
                    if (index < ganglinie_roh.Length) ganglinie_roh[index] = (double)wert;
                    index++;
                }
                rs.Close();

                // V0-5 (b): Rasterprüfung nach dem Muster des Stromzweigs
                // (SimulationStrombedarf.Berechnung). Anders als dort trägt
                // Tab_Waermebedarf kein Feld "Zeitinterval" - das Raster ergibt sich
                // allein aus der Wertzahl: 8.760 Stunden- oder 35.040 Viertelstundenwerte.
                if (index != 8760 && index != 8760 * 4)
                {
                    SimulationProtokoll.Aktuell.Warnung(string.Format(
                        MyResource.Resource.SIMENG_WAERMEGANGLINIE_RASTER_PASST_NICHT,
                        waectrl.items[n].m_ID_Ganglinie, index));
                    continue;
                }

                if (index == 8760)
                {
                    Array.Copy(ganglinie_roh, ganglinie, 8760);
                }
                else
                {
                    // Viertelstundenleistung [kW] -> Stundenmittel, wie
                    // WirtschaftlichkeitCtrl.ViertelstundenZuStundenMittel. Der Rechenkern
                    // kennt nur das Stundenraster.
                    for (int h = 0; h < 8760; h++)
                        ganglinie[h] = (double)((ganglinie_roh[h * 4] + ganglinie_roh[h * 4 + 1]
                                              + ganglinie_roh[h * 4 + 2] + ganglinie_roh[h * 4 + 3]) / 4.0);
                }

                //com.CSharp_I_vectoren_addieren(Waermebedarf_Extern, Waermebedarf);
                // K1/F18: Jede Ganglinie geht in den Kanal, der an ihrer Zuordnung steht
                // (Z_ProjektWaermebedarf.Kanal). Leer, NULL und jeder unbekannte Wert
                // ergeben den Heizkanal - die altverhaltenserhaltende Vorbelegung, mit der
                // jede Bestandsganglinie unverändert im Heizbedarf mitläuft.
                int kanal = Kanal.AusText(waectrl.items[n].Kanal);
                WPPlan.Core.BhkwPlan.VectorenAddieren(ganglinie, _kanaele.Bedarf[kanal]);

                // Energieprobe: kanalneutral - die Ganglinie zählt einmal, egal wohin.
                for (int h = 0; h < 8760; h++) probe[h] += ganglinie[h];

                // V0-5 (a): Waermebedarf_Extern bleibt die Summe ALLER Ganglinien.
                WPPlan.Core.BhkwPlan.VectorenAddieren(ganglinie, Waermebedarf_Extern);

                //Waermebedarf_Extern_Gesamt += com.I_vector_summe(Waermebedarf_Extern);
                Waermebedarf_Extern_Gesamt += ganglinie.Sum() / 1000;
            }

            // Wärmebedarf Gebäude Monat
            //com.I_monats_summe(Waermebedarf, Waermebedarf_Gebaeude_Monat, mo_anfang, mo_ende);
            // K1: Ausgewiesen wird der HEIZKANAL (Gebäudewärme + Heizungs-Lastgänge) -
            // genau der Inhalt, den der Summenvektor an dieser Stelle bisher trug. Nur
            // eine Ganglinie, die der Anwender ausdrücklich auf Brauchwasser oder Prozess
            // stellt, zählt hier künftig nicht mehr mit; für jede Bestandsganglinie
            // (ohne Kanalangabe) ist der Wert unverändert.
            WPPlan.Core.BhkwPlan.MonatsSumme(kanalHeizung, Waermebedarf_Gebaeude_Monat, mo_anfang, mo_ende);

            // Prozesswärme
            Prozesswaerme_berechnen();
            //Waermebedarf_Prozess = com.I_vector_summe(prozesswerte);
            // AUSGEWIESEN wird der reine Profilanteil - vor der Netzverlustverteilung.
            Waermebedarf_Prozess = prozesswerte.Sum() / 1000;

            //com.CSharp_I_vectoren_addieren(prozesswerte, Waermebedarf);
            WPPlan.Core.BhkwPlan.VectorenAddieren(prozesswerte, _kanaele.Prozess);
            for (int h = 0; h < 8760; h++) probe[h] += prozesswerte[h];

            // Brauchwasserwärme
            Brauchwasserwaerme_berechnen();
            //Waermebedarf_Brauchwasser = com.I_vector_summe(brauchwasserwerte);
            // W8-O-5b (07.09.2026): EINE Zeile fuer beide Wege - der Lauf ruft
            // dieselbe Methode wie die Vorschau, damit das Feld nicht mehr je nach
            // Herkunft kWh oder MWh fuehrt. Begruendung an der Methode.
            BrauchwassersummeUebernehmen();
            //com.CSharp_I_vectoren_addieren(brauchwasserwerte, Waermebedarf);
            WPPlan.Core.BhkwPlan.VectorenAddieren(brauchwasserwerte, _kanaele.Brauchwasser);
            for (int h = 0; h < 8760; h++) probe[h] += brauchwasserwerte[h];

            // Netzverluste
            //Waermebedarf_Gesamt = com.I_vector_summe(Waermebedarf);
            // K1: Der Summenvektor ist ab hier eine ABGELEITETE Größe. Für den
            // Netzverlust-Betrag wird er - wie bisher - VOR dem Aufschlag gebildet.
            SummenvektorAusKanaelen();
            Waermebedarf_Gesamt = Waermebedarf.Sum() / 1000;


            double stundl_netzverluste = 0;
            if (Netzverluste_Einheit == "%")
            {
                stundl_netzverluste = (Waermebedarf_Gesamt * 1000 * Netzverluste) / (double)876000;
                Waermebedarf_Netzverluste = (Waermebedarf_Gesamt * Netzverluste) / 100;
            }
            else
            {
                stundl_netzverluste = (double)Netzverluste / (double)8760;

                // V0-8: Auch bei absoluter Einheit ("kWh/a") die tatsächlich
                // aufgeschlagene Jahresmenge ausweisen - in MWh, derselben Einheit wie im
                // Prozent-Zweig. Bisher blieb das Feld hier auf 0, obwohl NetzverlusteC die
                // Energie auf alle 8760 Stunden addierte: der Bilanzausweis war falsch.
                Waermebedarf_Netzverluste = (double)stundl_netzverluste * 8760 / 1000;
            }

            //com.I_netzverlustec(Waermebedarf, stundl_netzverluste);
            // F2 (entschieden 27.08.2026): Der konstante Stundenbetrag ist derselbe wie
            // bisher, er geht aber nicht mehr geschlossen in den (Heiz-)Summenvektor,
            // sondern je Stunde ANTEILIG auf die drei Kanäle. Bei Kanalsumme 0 vollständig
            // auf den Heizkanal - siehe Kanalsatz.NetzverlusteVerteilen.
            _kanaele.NetzverlusteVerteilen(stundl_netzverluste);
            for (int h = 0; h < 8760; h++) probe[h] += stundl_netzverluste;

            // Die beiden öffentlichen Bedarfsvektoren sind ab jetzt die KANÄLE inklusive
            // ihres Netzverlustanteils (gewollte F2-Wirkung, siehe Feldkommentare). Die
            // Monatswerte und die Jahresmengen oben bleiben der reine Profilanteil.
            Array.Copy(_kanaele.Brauchwasser, brauchwasserwerte, 8760);
            Array.Copy(_kanaele.Prozess, prozesswerte, 8760);

            // gesamter Wärmebedarf
            //Waermebedarf_Gesamt = com.I_vector_summe(Waermebedarf);
            SummenvektorAusKanaelen();
            Waermebedarf_Gesamt = Waermebedarf.Sum() / 1000;

            // Energieprobe je Stunde (Konzept 11.3): Kanalsumme gegen die unabhängig in
            // double geführte Summe aller Anteile.
            Energieprobe(probe);

            //com.CSharp_I_vectoren_addieren(Waermebedarf, Waermebedarf_sortiert);
            WPPlan.Core.BhkwPlan.VectorenAddieren(Waermebedarf, Waermebedarf_sortiert);

            //com.CSharp_I_vectoren_addieren(Waermebedarf, Dauerlinie_nicht_sortiert);
            WPPlan.Core.BhkwPlan.VectorenAddieren(Waermebedarf, Dauerlinie_nicht_sortiert);

            //Dauerlinie_nicht_sortiert = Waermebedarf;

            // Maximaler Stunden Wärmebedarf gesamt
            Waermebedarf_Max = Maximaler_Waermebedarf(Waermebedarf);

            // Normierung Ganglinie
            //com.I_normieren(Waermebedarf_sortiert, Waermebedarf_Max);
            WPPlan.Core.BhkwPlan.Normieren(Waermebedarf_sortiert, Waermebedarf_Max);
            //com.I_normieren(Dauerlinie_nicht_sortiert, Waermebedarf_Max);
            WPPlan.Core.BhkwPlan.Normieren(Dauerlinie_nicht_sortiert, Waermebedarf_Max);

            // absteigend sortieren
            //com.I_heapsort(Waermebedarf_sortiert, Dauerlinie); // absteigend sortiert
            WPPlan.Core.BhkwPlan.Heapsort(Waermebedarf_sortiert, Dauerlinie);

            Array.Reverse(Dauerlinie);
        }

        // ===================================================================
        // Kanalmodell (Paket K1 - Konzept 4.1/4.2)
        // ===================================================================

        /// <summary>
        /// Stunden, in denen die Energieprobe (Konzept 11.3) die Toleranz der
        /// 1-ULP-Klasse überschritten hat. Erwartungswert 0; ein Wert &gt; 0 ist ein
        /// Befund für die Verifikation, kein Betriebszustand.
        /// </summary>
        public int Energieprobe_Verletzungen = 0;

        /// <summary>Größte Abweichung der Energieprobe [kWh] (siehe <see cref="Energieprobe_Verletzungen"/>).</summary>
        public double Energieprobe_MaxAbweichung = 0;

        /// <summary>
        /// Schreibt die Kanalsumme in <see cref="Waermebedarf"/> — der Summenvektor ist
        /// seit Paket K1 eine ABGELEITETE Größe (Konzept 4.2).
        ///
        /// Kopiert bewusst IN das vorhandene Array, statt das Feld neu zu belegen: Der
        /// Vektor wird an mehreren Stellen als Referenz weitergereicht
        /// (<c>SimulationControl</c>, <c>Form_Simulation_Detail</c>), und eine
        /// Neubelegung würde dort auf einen veralteten Vektor zeigen lassen.
        /// </summary>
        private void SummenvektorAusKanaelen()
        {
            double[] summe = _kanaele.Summe();
            Array.Copy(summe, Waermebedarf, Kanalsatz.STUNDEN_JAHR);
        }

        /// <summary>
        /// Energieprobe je Stunde (Konzept 11.3): Die Kanalsumme muss der unabhängig in
        /// <c>double</c> geführten Summe aller Bedarfsanteile entsprechen — Gebäudewärme,
        /// externe Lastgänge, Prozess- und Brauchwasserprofile, Netzverluste.
        ///
        /// Der Maßstab ist die 1-ULP-Klasse (<see cref="Kanalsatz.ErhaltungOk"/>), gefasst
        /// über die <see cref="Kanalsatz.ERHALTUNG_SCHRITTE_SUMME"/> double-Speicherungen,
        /// die eine double-Referenzsumme von der Kanalsumme trennen; die verbleibende
        /// Abweichung ist allein diese Rundung. Gemeldet wird EINMAL je Lauf mit der Zahl
        /// der betroffenen Stunden — ein struktureller Fehler (ein verschluckter oder
        /// doppelt gebuchter Anteil) trifft sofort tausende Stunden und liegt um
        /// Größenordnungen über der Toleranz; er ist damit unverwechselbar.
        /// </summary>
        private void Energieprobe(double[] erwartet)
        {
            Energieprobe_Verletzungen = 0;
            Energieprobe_MaxAbweichung = 0;

            for (int h = 0; h < Kanalsatz.STUNDEN_JAHR; h++)
            {
                double abweichung = Math.Abs((double)Waermebedarf[h] - erwartet[h]);
                if (abweichung > Energieprobe_MaxAbweichung) Energieprobe_MaxAbweichung = abweichung;
                if (!Kanalsatz.ErhaltungOk(erwartet[h], Waermebedarf[h],
                                           Kanalsatz.ERHALTUNG_SCHRITTE_SUMME))
                    Energieprobe_Verletzungen++;
            }

            if (Energieprobe_Verletzungen > 0)
                SimulationProtokoll.Aktuell.WarnungEinmal("ENERGIEPROBE_KANAELE", string.Format(
                    MyResource.Resource.SIMENG_ENERGIEPROBE_KANAELE,
                    Energieprobe_Verletzungen,
                    Energieprobe_MaxAbweichung.ToString("G4")));
        }

        /// <summary>
        /// Die drei Bedarfskanäle des Projekts (Konzept 4.1) als KOPIE.
        ///
        /// Kopiert wird aus demselben Grund, aus dem <see cref="Kanalsatz.Summe"/> einen
        /// eigenen Vektor liefert: Die Erzeugermodule überschreiben ihre Eingangsvektoren
        /// in-place (Regel B0-2), und ein herausgegebenes Innenleben wäre damit eine
        /// Aliasing-Falle. Das ist die Schnittstelle, an der Paket K2 (dreikanalige
        /// Kaskade) andockt.
        /// </summary>
        public Kanalsatz KanaeleDrei()
        {
            return _kanaele.Clone();
        }

        // K2-O3: Die Übergangsabbildung Kanaele() (Heiz = HEIZUNG + PROZESS, WW =
        // BRAUCHWASSER) auf Waermekanaele hat mit Paket K2 ihren letzten Aufrufer
        // verloren — die Kaskade rechnet seither auf denselben drei Kanälen, mit denen
        // der Bedarf gebildet wird (KanaeleDrei). Mit Paket S1 ist sie gelöscht:
        // Prozesswärme ist ein eigener Kanal mit eigenen Senken, und eine Abbildung, die
        // sie wieder in den Heizkanal faltet, wäre ab hier schlicht falsch.

        /// <summary>
        /// <b>Der KLIMAKALENDER eines Laufs</b> (iU9-W9.8, Anwenderwunsch W9-E-2) — die
        /// 365 Tagessätze der Klimaregion, die 8 760 Stundentemperaturen und der daraus
        /// abgeleitete Wochentag des 1. Januar; Teil des modellfreien Vorbereitungsschritts.
        ///
        /// <para><b>Zwei Teile</b> (Stufe G1.0, F-Ü3): <c>WE</c>, Stundentemperatur,
        /// Wochentag und Monatsgrenzen bilden den gemeinsamen Teil
        /// (<see cref="KlimakalenderGemeinsam"/>), Einstrahlung, Tagesmitteltemperatur und
        /// Tagestypen den Altweg-Teil (<see cref="KlimakalenderAltweg"/>). Gelesen wird
        /// Anweisung für Anweisung wie zuvor, nur in die Felder des Kalenders. Danach wird
        /// der Tagesbilanz-Weg für diesen Lauf aufgebaut — mit dem Altweg-Teil und einem
        /// frischen Merkplatz.</para>
        ///
        /// <para>Der Gebäudedialog fährt dieselbe Vorbereitung wie der Lauf. Ohne sie
        /// stünden die Tagessätze auf 0, und jede Tagesheizlast käme als 0 heraus.</para>
        /// </summary>
        /// <param name="ID_Klimaregion">Die Klimaregion des Projekts.</param>
        internal void KlimakalenderLesen(int ID_Klimaregion)
        {
            KlimakalenderAltweg altweg = _kalender.Altweg;
            KlimadatenCtrl ctrl_klima = new KlimadatenCtrl();
            ctrl_klima.ReadAll(ID_Klimaregion);
            for (int i = 0; i < ctrl_klima.rows; i++)
            {
                altweg.Sol_N[i] = (double)ctrl_klima.items[i].m_Sol_Nord;
                altweg.Sol_w[i] = (double)ctrl_klima.items[i].m_Sol_West;
                altweg.Sol_O[i] = (double)ctrl_klima.items[i].m_Sol_Ost;
                altweg.Sol_S[i] = (double)ctrl_klima.items[i].m_Sol_Sued;
                altweg.A_Temp[i] = (double)ctrl_klima.items[i].m_nTemperatur;
                WE[i] = (bool)ctrl_klima.items[i].m_WE;
                altweg.TagTyp_W[i] = (int)ctrl_klima.items[i].m_TagTyp_W;
                altweg.TagTyp_NW[i] = (int)ctrl_klima.items[i].m_TagTyp_NW;
            }
            Stundentemperatur_aus_DB(ID_Klimaregion);
            DBGelesen = true;

            // F3 (Konzept 4.2): Der Klimadaten-Kalender ist ab Paket K1 für ALLE
            // Bedarfsarten führend. Die Profilkachelung startet damit mit dem
            // tatsächlichen Wochentag des 1. Januar statt fest mit Sonntag.
            WochentagJan1 = ProfilBedarf.WochentagJan1AusWE(WE);
            _kalender.Gemeinsam.WochentagJan1 = WochentagJan1;

            // Aufbau des Tagesbilanz-Wegs je Lauf (1.5): Er bekommt den Altweg-Teil des
            // Kalenders; der VDI-Weg bekäme allein den gemeinsamen Teil.
            _altweg = new Altweg.TagesbilanzRechenweg(altweg);

            // Stufe G1 — was der VDI-Weg zusätzlich aus dem gemeinsamen Teil liest
            // (Umsetzungskonzept 1.2): Koordinaten der Klimaregion und die Wochenendmaske
            // des Ortszeit-Kalenders mit ihrer Probe gegen Tab_Klimadaten.WE (U7, F-Ü8).
            // Die Solarreihe in Ortszeit hat Stundentemperatur_aus_DB bereits abgelegt.
            // Nichts davon erreicht den Tagesbilanz-Weg.
            KlimakalenderGemeinsam gemeinsam = _kalender.Gemeinsam;
            KlimaregionCtrl.Koordinaten(ID_Klimaregion, out double laengengrad, out double breitengrad);
            gemeinsam.Laengengrad = laengengrad;
            gemeinsam.Breitengrad = breitengrad;
            gemeinsam.Referenzjahr = SolardatenCtrl.Referenzjahr(m_ID_Projekt);
            gemeinsam.WochenendeOrtszeit = KlimakalenderGemeinsam.WochenendmaskeBilden(gemeinsam.Referenzjahr);
            gemeinsam.WochenendProbeAbweichungen = KlimakalenderGemeinsam.Abweichungen(gemeinsam.WochenendeOrtszeit, WE);
            GebaeudeErgebnisse.Leeren();
        }

        /// <summary>
        /// <b>Die HEIZWÄRME EINES Gebäudes — die Fassade der Gebäudebedarfsrechnung</b>
        /// (Stufe G1.0 der Gebäudesimulation, Entscheid E20, ADR-006). Sie ruft den
        /// modellfreien Vorbereitungsschritt (<see cref="GebaeudeVorbereitung"/>), die Weiche
        /// wählt den Rechenweg des Gebäudes (<see cref="RechenwegWaehlen"/>), und genau dieser
        /// eine Weg rechnet — hinter <see cref="IGebaeudeRechenweg"/>.
        ///
        /// <para><b>Warum es die Methode gibt.</b> Der Gebäudedialog zeigt seit dem
        /// Anwenderwunsch W9-E-2 den Wärmebedarf GENAU EINES Gebäudes. Diese Zahl muss
        /// dieselbe sein wie die des Laufs — also darf sie nicht ein zweites Mal
        /// gerechnet werden, sondern nur ein zweites Mal AUFGERUFEN. Der Lauf ruft sie in
        /// seiner Schleife, der Dialog für sein eines Gebäude.</para>
        ///
        /// <para><b>Die Verbrauchs-Rückrechnung (E8) führt die Fassade.</b> Ist die Einheit
        /// keine Fläche, rechnet der gewählte Weg zuerst auf der Katalogfläche
        /// (<c>Wohnflaeche_gesamt</c>) und liefert den Jahreswert <c>verbrauchAltKwh</c>; die
        /// Fläche wird im Verhältnis des angegebenen zum gerechneten Verbrauch
        /// zurückgerechnet, und der Weg rechnet ein zweites Mal auf dieser Fläche — die
        /// Reihenfolge des Bestands, weil der Tagesbilanz-Weg die Skalierung in der
        /// Tagesrechnung trägt.</para>
        ///
        /// <para><b>Der Index ist ein Merkplatz, kein Rang.</b> Eine Rechnung für EIN
        /// Gebäude nimmt deshalb 0 und bekommt bitgleich dasselbe Ergebnis wie dieses
        /// Gebäude im Lauf.</para>
        /// </summary>
        /// <param name="item">Die Zeile aus <c>Abfrage_Projektgebaeude</c>. Sie wird
        /// GESCHRIEBEN — <c>Bewohner</c> und <c>Z_AuswahlWohnflaeche</c> werden
        /// nachgerechnet, wie im Lauf.</param>
        /// <param name="index">Der Merkplatz des Gebäudes (ab 0).</param>
        /// <param name="ziel">Die 8 760 Stundenwerte in WATT; die Umrechnung nach kW
        /// macht der Aufrufer.</param>
        /// <returns><c>false</c>, wenn eine Vorbedingung des gewählten Rechenwegs fehlt
        /// (Tagesbilanz: keine Tagesverteilung zum Gebäudetyp) — der Lauf bricht dann ab,
        /// wie bisher.</returns>
        internal bool HeizwaermeEinesGebaeudes(ProjektGebaeudeModel item, int index, double[] ziel)
        {
            // 1. Der modellfreie Vorbereitungsschritt.
            GebaeudeVorbereitung vorbereitung = GebaeudeVorbereitung.Bilden(_kalender, item);

            // 2. Die Weiche: genau ein Rechenweg je Gebäude.
            IGebaeudeRechenweg weg = RechenwegWaehlen(item);
            KlimakalenderGemeinsam gemeinsam = vorbereitung.Klimakalender.Gemeinsam;
            double verbrauchAltKwh;

            // Der VDI-Weg läuft EINMAL; Rückrechnung und Skalierung sind eine
            // Nachmultiplikation hinter der Weiche (F-Ü2, Rechenschritte 8.3). Der
            // Tagesbilanz-Weg geht unverändert den Bestandsweg darunter (zwei Aufrufe).
            if (!ReferenceEquals(weg, _altweg))
                return EinLaufMitNachmultiplikation(weg, vorbereitung, item, index, ziel, gemeinsam);

            // wenn die Einheit nicht als "Wohnfläche [m²]" angegeben ist...Wohnfläche und Anzahl Bewohner berechnen
            if (vorbereitung.IstFlaeche)
            {
                item.Bewohner = item.Z_AuswahlWohnflaeche / item.Flaeche_Nutzer;
            }
            else
            {
                // 3. Verbrauchs-Rückrechnung: erster Lauf auf der Katalogfläche.
                item.Z_AuswahlWohnflaeche = vorbereitung.FlaecheAlt;

                if (!weg.Rechnen(item, index, ziel, gemeinsam, out verbrauchAltKwh)) return false;
                double FlaecheAlt = vorbereitung.FlaecheAlt;
                double FlaecheNeu = vorbereitung.VerbrauchNeu / verbrauchAltKwh * FlaecheAlt;
                item.Z_AuswahlWohnflaeche = FlaecheNeu;
                item.Bewohner = item.Z_AuswahlWohnflaeche / item.Flaeche_Nutzer;
            }
            Anzahl_Bewohner = (int)item.Bewohner;
            Wohnflaeche = item.Z_AuswahlWohnflaeche;

            // 4. Der Lauf auf der Bezugsfläche des Gebäudes.
            return weg.Rechnen(item, index, ziel, gemeinsam, out verbrauchAltKwh);
        }

        /// <summary>
        /// <b>Der Zweig des VDI-Wegs in der Fassade</b> (Rechenschritte 8.3, Umsetzungskonzept
        /// 1.5): ein Lauf auf dem Katalogbau, danach die Verhältnisrechnung nach E8 und die
        /// Nachmultiplikation der Reihe — in der Reihenfolge des Bestands, nachgebaut, nicht
        /// aus dem Tagesbilanz-Weg gerufen.
        /// <list type="number">
        /// <item>Flächenangabe: Faktor = <c>Z_AuswahlWohnflaeche / Nutzflaeche</c>,
        /// Bewohner = Fläche / Fläche je Nutzer.</item>
        /// <item>Verbrauchsangabe: <c>FlaecheNeu = VerbrauchNeu / VerbrauchAltKwh ·
        /// FlaecheAlt</c>, Faktor = <c>FlaecheNeu / FlaecheAlt</c>; <c>VerbrauchAltKwh = 0</c>
        /// wird benannt abgelehnt (<see cref="GebaeudeModellFehler.VerbrauchAltNull"/>) statt
        /// wie im Bestand durch null zu teilen.</item>
        /// </list>
        /// </summary>
        private bool EinLaufMitNachmultiplikation(IGebaeudeRechenweg weg, GebaeudeVorbereitung vorbereitung,
                                                  ProjektGebaeudeModel item, int index, double[] ziel,
                                                  KlimakalenderGemeinsam gemeinsam)
        {
            double verbrauchAltKwh;
            if (!weg.Rechnen(item, index, ziel, gemeinsam, out verbrauchAltKwh)) return false;

            double faktor;
            if (vorbereitung.IstFlaeche)
            {
                item.Bewohner = item.Z_AuswahlWohnflaeche / item.Flaeche_Nutzer;
                faktor = item.Z_AuswahlWohnflaeche / item.Nutzflaeche;
            }
            else
            {
                if (!(verbrauchAltKwh > 0.0) || double.IsInfinity(verbrauchAltKwh))
                {
                    SimulationProtokoll.Aktuell.Fehlermeldung(
                        "Gebäudemodell VDI 6007 [" + GebaeudeModellFehler.VerbrauchAltNull + "]: " +
                        (item.Gebaeudename ?? "") + " (" + item.ID_Gebaeude + ") — der Kataloglauf liefert keinen " +
                        "Heizbedarf; der angegebene Verbrauch lässt sich darauf nicht zurückrechnen.");
                    return false;
                }
                double FlaecheAlt = vorbereitung.FlaecheAlt;
                double FlaecheNeu = vorbereitung.VerbrauchNeu / verbrauchAltKwh * FlaecheAlt;
                item.Z_AuswahlWohnflaeche = FlaecheNeu;
                item.Bewohner = item.Z_AuswahlWohnflaeche / item.Flaeche_Nutzer;
                faktor = FlaecheNeu / FlaecheAlt;
            }

            if (double.IsNaN(faktor) || double.IsInfinity(faktor) || faktor < 0.0)
            {
                SimulationProtokoll.Aktuell.Fehlermeldung(
                    "Gebäudemodell VDI 6007 [" + GebaeudeModellFehler.PflichtgroesseFehlt + "]: " +
                    (item.Gebaeudename ?? "") + " (" + item.ID_Gebaeude + ") — der Skalierungsfaktor nach E8 ist " +
                    "nicht bestimmbar (Fläche oder Verbrauch fehlt).");
                return false;
            }

            for (int h = 0; h < 8760; h++) ziel[h] *= faktor;
            GebaeudeModellErgebnis ergebnis = GebaeudeErgebnisse.Ergebnis(index);
            if (ergebnis != null) GebaeudeErgebnisse.Setzen(index, ergebnis.Skaliert(faktor));

            Anzahl_Bewohner = (int)item.Bewohner;
            Wohnflaeche = item.Z_AuswahlWohnflaeche;
            return true;
        }

        /// <summary>
        /// <b>Die Weiche</b> (E20): liest den Rechenweg des Gebäudes
        /// (<c>Tab_Gebaeude.Gebaeude_Modell</c>) und wählt genau ein Modul.
        ///
        /// <para><b>Regel in dieser Welle (Stufe G1, Anbindung):</b>
        /// <see cref="DbWerte.GEBAEUDE_MODELL_VDI6007"/> führt auf den VDI-Weg,
        /// <see cref="DbWerte.GEBAEUDE_MODELL_TAGESBILANZ"/> auf den Tagesbilanz-Weg, und
        /// <c>NULL</c> folgt <see cref="MODELL_OHNE_ANGABE"/> — bis zur Schlusswelle G1+G2 der
        /// Tagesbilanz-Weg. Ein unbekannter Wert rechnet auf dem Tagesbilanz-Weg und wird als
        /// Warnung benannt.</para>
        /// </summary>
        internal IGebaeudeRechenweg RechenwegWaehlen(ProjektGebaeudeModel item)
        {
            string modell = item.Gebaeude_Modell ?? MODELL_OHNE_ANGABE;

            if (string.Equals(modell, DbWerte.GEBAEUDE_MODELL_VDI6007, StringComparison.Ordinal))
                return _vdi6007;

            if (!string.Equals(modell, DbWerte.GEBAEUDE_MODELL_TAGESBILANZ, StringComparison.Ordinal))
                SimulationProtokoll.Aktuell.WarnungEinmal("gebaeude-modell-unbekannt-" + modell,
                    "Gebäude " + (item.Gebaeudename ?? "") + ": Der Rechenweg '" + modell + "' ist unbekannt; " +
                    "gerechnet wird auf dem Tagesbilanz-Weg.");
            return _altweg;
        }

        private double Maximaler_Waermebedarf(double[] Waermebedarf)
        {
            double Waermebedarf_Max;

            Waermebedarf_Max = 0;
            for (int i = 0; i < 8760; i++)
            {
                if (Waermebedarf_Max < Waermebedarf[i]) Waermebedarf_Max = Waermebedarf[i];
            }

            return Waermebedarf_Max;
        }

        /// <summary>
        /// Die 8.760 Aussentemperaturen der Klimaregion.
        ///
        /// <para><b>B1 (Paket A): ueber den ORTSZEIT-Lesepfad.</b> Bis dahin las diese
        /// Methode <c>Tab_Solar</c> selbst und damit im UTC-Raster. Die Stundentemperatur
        /// speist den COP der Waermepumpe, die Erdreichrechnung und das Reporting — sie
        /// lag also gegenueber dem Bedarf 1 h (Winter) bzw. 2 h (Sommer) zu frueh. Die
        /// Jahres- und Monatsmittel bleiben davon unberuehrt, der Tagesgang nicht.</para>
        /// </summary>
        private void Stundentemperatur_aus_DB(int ID_Klimaregion)
        {
            SolardatenCtrl ctrldat = new SolardatenCtrl();
            ctrldat.ReadOrtszeit(ID_Klimaregion, m_ID_Projekt);

            int stunden = Math.Min(ctrldat.rows, Stundentemperatur.Length);
            for (int i = 0; i < stunden; i++)
                Stundentemperatur[i] = (double)ctrldat.items[i].Außen_Temp;

            // Stufe G1 (Umsetzungskonzept 1.2 Zeile 2): die ganze Zeilenliste bleibt für den
            // VDI-Weg stehen — Strahlung, Gegenstrahlung und die UTC-Herkunft je Zeile.
            _kalender.Gemeinsam.SolarOrtszeit = ctrldat.items.ToArray();
        }

        /// <summary>
        /// Prozesswärmeprofile des Projekts (bzw. des Katalogs) in
        /// <see cref="prozesswerte"/> und <see cref="Waermebedarf_Prozess_Monat"/>.
        ///
        /// PAKET K1: Der Algorithmus steht jetzt einmal in <see cref="ProfilBedarf"/> —
        /// zusammen mit dem Brauchwasser- und dem Stromzweig, die bis hierher je eine
        /// eigene, auseinandergelaufene Kopie hatten (Konzept 4.2). Diese Methode ist nur
        /// noch die Anbindung: Quellmodus setzen, Kalender wählen, Ergebnis melden.
        ///
        /// Der Quellmodus folgt weiterhin dem Parameter <paramref name="list"/> — die
        /// Vorschaudialoge übergeben ihre Auswahl, der Rechenweg nicht. Innerhalb der
        /// Profilroutine ist der Modus dagegen ein expliziter Parameter (V0-4).
        /// </summary>
        public void Prozesswaerme_berechnen(List<string> list = null)
        {
            try
            {
                //com.I_vector_init(ref prozesswerte);
                WPPlan.Core.BhkwPlan.VectorInit(prozesswerte);

                ProfilQuellmodus modus = ProfilBedarf.Vorschaumodus(list, m_ID_Projekt);

                // F3: Die Projektrechnung folgt dem Klimadaten-Kalender, die Katalog-
                // vorschau der Altkonvention - sie kennt kein Projekt und keine
                // Klimaregion, und ihre Kurven sollen zwischen zwei Katalogeinträgen
                // vergleichbar bleiben. Die Projektvorschau (W9-B-4/B-5) bleibt bei der
                // Altkonvention: WochentagJan1 entsteht erst IN Waermebedarf_berechnen
                // aus den geladenen Klimadaten, die der Dialog nie laedt - der Wert
                // waere hier ohnehin die Altkonvention. Damit ist der Kalender dieses
                // Weges unveraendert.
                int wochentag = (modus == ProfilQuellmodus.Projektrechnung)
                                ? WochentagJan1 : ProfilBedarf.WOCHENTAG_ALTKONVENTION;

                ProfilBedarf.Rechnen(ProfilQuelle.Prozesswaerme(modus), m_ID_Projekt, list,
                                     wochentag, mo_anfang, mo_ende,
                                     prozesswerte, Waermebedarf_Prozess_Monat);
            }
            // Protokollkanal-Nachzug: WARNUNG statt bloßer Konsolenzeile - der Bedarf ist
            // unvollständig und damit jedes Ergebnis darauf.
            catch (SystemException ex) { SimulationProtokoll.Aktuell.Warnung("Fehler bei der Prozesswärme-Berechnung (Ergebnis unvollständig): " + ex.Message); }
        }

        /// <summary>
        /// Weist die gerechnete Stundenreihe <see cref="prozesswerte"/> [kWh] als
        /// Energiemenge <see cref="Waermebedarf_Prozess"/> aus — <b>in MWh</b>, der
        /// Einheit, die dieses Feld führt (<see cref="Waermebedarf_berechnen"/> setzt es
        /// ebenso, und die Ergebnisanzeige liest es als MWh).
        ///
        /// <para><b>Warum es die Methode gibt.</b> Die Vorschauwege setzten das Feld
        /// bisher jeder für sich: Die Prozesswärme-Verwaltung mit einem nackten
        /// <c>/ 1000</c>, der Bedarfsprofildialog (W9) mit der blanken Summe — also in
        /// kWh, während die Anzeige MWh las. Dort stand „Wärmebedarf Prozess" um den
        /// Faktor 1000 zu groß (Entscheid W9‑O‑3 vom 04.09.2026). Die Umrechnung steht
        /// jetzt einmal, an dem Feld, das sie betrifft, und geht über
        /// <see cref="Energieeinheit"/> statt über einen Teiler im Aufrufer.</para>
        ///
        /// <para><b>Der Rechenweg bleibt unberührt.</b>
        /// <see cref="Waermebedarf_berechnen"/> rechnet weiter mit seiner eigenen Zeile;
        /// diese Methode bedient nur die Vorschauwege, und der Referenzlauf bleibt
        /// byte-gleich.</para>
        /// </summary>
        public void ProzesssummeUebernehmen()
        {
            Waermebedarf_Prozess = Energieeinheit.MWh.AusKWh(prozesswerte.Sum());
        }

        /// <summary>
        /// Brauchwasserprofile des Projekts (bzw. des Katalogs) in
        /// <see cref="brauchwasserwerte"/> und <see cref="Waermebedarf_Brauchwasser_Monat"/>.
        /// Aufbau und Begründung wie bei <see cref="Prozesswaerme_berechnen"/> — beide
        /// Zweige teilen sich seit Paket K1 dieselbe Routine (Konzept 4.2).
        /// </summary>
        public void Brauchwasserwaerme_berechnen(List<string> list = null)
        {
            try
            {
                //com.I_vector_init(ref brauchwasserwerte);
                WPPlan.Core.BhkwPlan.VectorInit(brauchwasserwerte);

                ProfilQuellmodus modus = ProfilBedarf.Vorschaumodus(list, m_ID_Projekt);

                int wochentag = (modus == ProfilQuellmodus.Projektrechnung)
                                ? WochentagJan1 : ProfilBedarf.WOCHENTAG_ALTKONVENTION;

                ProfilBedarf.Rechnen(ProfilQuelle.Brauchwasser(modus), m_ID_Projekt, list,
                                     wochentag, mo_anfang, mo_ende,
                                     brauchwasserwerte, Waermebedarf_Brauchwasser_Monat);
            }
            // Protokollkanal-Nachzug: WARNUNG, siehe Prozesswärme-Zweig.
            catch (SystemException ex) { SimulationProtokoll.Aktuell.Warnung("Fehler bei der Brauchwasserwärme-Berechnung (Ergebnis unvollständig): " + ex.Message); }
        }

        /// <summary>
        /// Weist die gerechnete Stundenreihe <see cref="brauchwasserwerte"/> [kWh] als
        /// Energiemenge <see cref="Waermebedarf_Brauchwasser"/> aus — <b>in MWh</b>, der
        /// Einheit, die dieses Feld führt.
        ///
        /// <para><b>Warum es die Methode gibt (Anwenderentscheid W8‑O‑5b vom
        /// 07.09.2026).</b> Das Feld trug bis hierher ZWEI Einheiten, je nachdem, wer es
        /// gefüllt hatte: <see cref="Waermebedarf_berechnen"/> teilte durch 1000 und wies
        /// MWh aus, die beiden Vorschauwege (<c>BedarfsVorschauCtrl</c>) übernahmen die
        /// nackte Summe und wiesen kWh aus. Die Ergebnisanzeige konnte nur EINE der
        /// beiden Angaben glauben; sie glaubte kWh und teilte deshalb den Wert des Laufs
        /// ein zweites Mal — „Wärmebedarf Brauchwasser" stand in
        /// <c>Simulation → Wärmebedarf-Details</c> um den Faktor 1000 zu klein. Die
        /// Umrechnung steht jetzt einmal, an dem Feld, das sie betrifft, und geht über
        /// <see cref="Energieeinheit"/> statt über einen Teiler im Aufrufer — dieselbe
        /// Bauform wie <see cref="ProzesssummeUebernehmen"/> (Entscheid W9‑O‑3).</para>
        ///
        /// <para><b>Der Rechenweg bleibt unberührt.</b> Das Feld ist eine reine ANZEIGE:
        /// Kein Erzeuger, keine Bilanz und keine Ergebnistabelle liest es — die
        /// Brauchwasserspalte von <c>Tab_ErgebnisEnergiebedarf</c> kommt aus dem
        /// Kanalvektor, nicht von hier. Der Referenzlauf bleibt byte-gleich.</para>
        /// </summary>
        public void BrauchwassersummeUebernehmen()
        {
            Waermebedarf_Brauchwasser = Energieeinheit.MWh.AusKWh(brauchwasserwerte.Sum());
        }
    }
}
